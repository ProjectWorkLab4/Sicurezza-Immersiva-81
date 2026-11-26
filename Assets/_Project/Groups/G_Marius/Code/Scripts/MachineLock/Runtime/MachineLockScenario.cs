using System.Collections;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// High-level state of the Machine Lock scenario (GDD §10.1). Used for HUD phase and hints;
    /// task flags stay independent and may complete out of order.
    /// </summary>
    public enum MachineLockState
    {
        /// <summary>Start, until the intro subtitles end.</summary>
        Intro,
        /// <summary>Press running.</summary>
        Emergency,
        /// <summary>E-stop pressed.</summary>
        MachineStopped,
        /// <summary>Main power off and lockout applied.</summary>
        Isolated,
        /// <summary>Tester zero plus try-start verified.</summary>
        ZeroEnergy,
        /// <summary>Full correct PPE worn.</summary>
        Protected,
        /// <summary>Cones and absorbents complete.</summary>
        Contained,
        /// <summary>Every task complete, waiting for the exit trigger.</summary>
        HandoverReady,
        /// <summary>EndMission was called.</summary>
        Ended
    }

    /// <summary>
    /// Orchestrator of the Machine Lock scenario. Lives on MNG_Scenario in SCN_MachineLock.
    /// It owns the task flags, the derived macro-objectives, the HUD texts, the SPI and the single
    /// EndMission call. Content components (press, panel, PPE, spill, observation) report through the
    /// Notify methods; every method is null-safe and idempotent and ignores calls after the end.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Scenario")]
    public sealed class MachineLockScenario : ScenarioController
    {
        /// <summary>Number of derived macro-objectives (one per mission objective).</summary>
        public const int MACRO_COUNT = 4;

        [Header("Machine Lock")]
        [Tooltip("Tunable Machine Lock values (SO_MachineLockConfig).")]
        [SerializeField] private MachineLockConfig config;

        private MachineLockSpi spi;
        private MachineLockState state = MachineLockState.Intro;
        private bool ended;
        private int currentMacro;
        private int correctPpeCount;
        private int conesPlaced;
        private int absorbentsPlaced;
        private bool valveClosed;
        private bool actionStarted;
        private Coroutine playingLines;

        /// <summary>Current high-level state.</summary>
        public MachineLockState State => state;
        /// <summary>SPI of the current run; null before the scenario starts.</summary>
        public MachineLockSpi Spi => spi;
        /// <summary>true after <see cref="EndMissionOnce"/>.</summary>
        public bool HasEnded => ended;
        /// <summary>Task flag: E-stop pressed.</summary>
        public bool TaskEStop { get; private set; }
        /// <summary>Task flag: alarm raised.</summary>
        public bool TaskAlarm { get; private set; }
        /// <summary>Task flag: main switch open.</summary>
        public bool TaskMainOff { get; private set; }
        /// <summary>Task flag: lockout applied.</summary>
        public bool TaskLockout { get; private set; }
        /// <summary>Task flag: tester read zero.</summary>
        public bool TaskTesterZero { get; private set; }
        /// <summary>Task flag: try-start verified.</summary>
        public bool TaskTryStart { get; private set; }
        /// <summary>Task flag: full correct PPE worn.</summary>
        public bool TaskPpeFull { get; private set; }
        /// <summary>Task flag: containment complete.</summary>
        public bool TaskContainment { get; private set; }
        /// <summary>Task flag: exit reached and handover done.</summary>
        public bool TaskExit { get; private set; }
        /// <summary>Number of correct PPE items currently worn.</summary>
        public int CorrectPpeCount => correctPpeCount;
        /// <summary>Number of cones placed on the delimitation zones.</summary>
        public int ConesPlaced => conesPlaced;
        /// <summary>Number of absorbents placed on the coverage zones.</summary>
        public int AbsorbentsPlaced => absorbentsPlaced;
        /// <summary>true after the leak valve was closed.</summary>
        public bool ValveClosed => valveClosed;
        /// <summary>Number of distinct observation targets scored.</summary>
        public int ObservedTargets => spi != null ? spi.ObservedTargets : 0;
        /// <summary>Index of the current derived macro-objective, 0 to 4.</summary>
        public int CurrentMacroObjective => currentMacro;

        /// <summary>
        /// Starts the scenario: creates the SPI, sets the scan phase, plays the intro subtitles and
        /// shows the first hint. Called by the base controller once the HUD is ready.
        /// </summary>
        protected override void OnScenarioStarted()
        {
            ended = false;
            state = MachineLockState.Intro;
            currentMacro = 0;
            spi = new MachineLockSpi(ResolveConfig().CreateSpiOptions(Level));
            spi.SetElapsed(0f);

            SetPhase(PHASE_SCAN);
            SetHint(string.Format(MachineLockTexts.HintStartFormat, 0));
            RefreshObjectives();
            PlayLines(MachineLockTexts.Intro1, MachineLockTexts.Intro2);
        }

        /// <summary>
        /// Handles the level timer. Guided has no countdown and does not end here; Realistic and
        /// Stress end with Time = 0 and an outcome based on the current state.
        /// </summary>
        protected override void OnTimerExpired()
        {
            if (ended) return;
            if (Level == DifficultyLevel.Guided) return;
            if (spi != null)
            {
                spi.SetElapsed(ElapsedSeconds);
                spi.ApplyEvent(SpiEvent.Timeout);
            }
            RefreshObjectives();
            EndMissionOnce(EvaluateTimeoutOutcome());
        }

        // =====================================================================
        // Task events (GDD §10.5)
        // =====================================================================

        /// <summary>
        /// E-stop pressed. Stops motion only: the display stays on and energy is not isolated.
        /// </summary>
        public void NotifyEStopPressed()
        {
            if (ended || TaskEStop) return;
            TaskEStop = true;
            actionStarted = true;
            Apply(SpiEvent.EStop);
            SetPhase(PHASE_ACTION);
            PlayLines(MachineLockTexts.EStop, MachineLockTexts.EStop2);
            SetHint(MachineLockTexts.HintAlarm);
            RefreshObjectives();
        }

        /// <summary>
        /// Department alarm raised.
        /// </summary>
        public void NotifyAlarmPressed()
        {
            if (ended || TaskAlarm) return;
            TaskAlarm = true;
            actionStarted = true;
            Apply(SpiEvent.Alarm);
            PlayLines(MachineLockTexts.Alarm, MachineLockTexts.Alarm2);
            SetHint(MachineLockTexts.HintMainPower);
            RefreshObjectives();
        }

        /// <summary>
        /// Main switch opened: the display and the motor go off.
        /// </summary>
        public void NotifyMainPowerOff()
        {
            if (ended || TaskMainOff) return;
            TaskMainOff = true;
            actionStarted = true;
            Apply(SpiEvent.MainOff);
            PlayLines(MachineLockTexts.Panel, MachineLockTexts.Panel2);
            SetHint(MachineLockTexts.HintLockout);
            RefreshObjectives();
        }

        /// <summary>
        /// Lockout applied after the main switch was opened.
        /// </summary>
        public void NotifyLockoutApplied()
        {
            if (ended || TaskLockout) return;
            TaskLockout = true;
            actionStarted = true;
            Apply(SpiEvent.Lockout);
            PlayLines(MachineLockTexts.Lockout, MachineLockTexts.Lockout2);
            SetHint(MachineLockTexts.HintTester);
            RefreshObjectives();
        }

        /// <summary>
        /// Voltage tester read. Zero confirms that no residual energy is left.
        /// </summary>
        /// <param name="zero">true when the reading is ZERO.</param>
        public void NotifyTesterRead(bool zero)
        {
            if (ended) return;
            if (!zero)
            {
                Alert(MachineLockTexts.TesterLive + "\n" + MachineLockTexts.TesterLive2);
                return;
            }
            if (TaskTesterZero) return;
            TaskTesterZero = true;
            actionStarted = true;
            Apply(SpiEvent.TesterZero);
            Alert(MachineLockTexts.TesterZero);
            SetHint(MachineLockTexts.HintTryStart);
            RefreshObjectives();
        }

        /// <summary>
        /// Try-start performed and the machine did not move: isolation confirmed.
        /// </summary>
        public void NotifyTryStartVerified()
        {
            if (ended || TaskTryStart) return;
            TaskTryStart = true;
            actionStarted = true;
            Apply(SpiEvent.TryStartVerified);
            Feedback(MachineLockTexts.TryOk, FeedbackKind.Positive);
            SetHint(MachineLockTexts.HintPpe);
            RefreshObjectives();
        }

        /// <summary>
        /// PPE set changed. Full means the four correct items are worn.
        /// </summary>
        /// <param name="correctCount">Number of correct items worn, 0 to 4.</param>
        /// <param name="full">true when all four correct items are worn.</param>
        public void NotifyPpeChanged(int correctCount, bool full)
        {
            if (ended) return;
            correctPpeCount = Mathf.Clamp(correctCount, 0, 4);
            bool wasFull = TaskPpeFull;
            TaskPpeFull = full;
            actionStarted = true;

            if (full)
            {
                if (!wasFull) Apply(SpiEvent.PpeFull);
                Alert(MachineLockTexts.PpeFull);
                SetHint(MachineLockTexts.HintValveAndContainment);
            }
            else
            {
                SetHint(string.Format(MachineLockTexts.HintPpePartialFormat, correctPpeCount));
            }
            RefreshObjectives();
        }

        /// <summary>
        /// A cone snapped to a delimitation zone.
        /// </summary>
        /// <param name="count">Total number of placed cones, 0 to 4.</param>
        public void NotifyConePlaced(int count)
        {
            if (ended) return;
            conesPlaced = Mathf.Clamp(Mathf.Max(conesPlaced, count), 0, 4);
            actionStarted = true;
            Feedback(string.Format(MachineLockTexts.ConeFormat, conesPlaced), FeedbackKind.Positive);
            UpdateContainment();
        }

        /// <summary>
        /// An absorbent mat or sock snapped to a coverage zone.
        /// </summary>
        /// <param name="count">Total number of placed absorbents, 0 to 4.</param>
        public void NotifyAbsorbentPlaced(int count)
        {
            if (ended) return;
            absorbentsPlaced = Mathf.Clamp(Mathf.Max(absorbentsPlaced, count), 0, 4);
            actionStarted = true;
            Feedback(string.Format(MachineLockTexts.HintContainmentPartialFormat, conesPlaced, absorbentsPlaced), FeedbackKind.Positive);
            UpdateContainment();
        }

        /// <summary>
        /// Leak valve closed. No score, but a good-practice debrief line.
        /// </summary>
        public void NotifyValveClosed()
        {
            if (ended || valveClosed) return;
            valveClosed = true;
            actionStarted = true;
            Feedback(MachineLockTexts.Valve, FeedbackKind.Positive);
        }

        /// <summary>
        /// An observation target was looked at long enough. Each distinct id scores once.
        /// </summary>
        /// <param name="id">Stable target identifier (press, thermometer, display, puddle, panel, ppe).</param>
        public void NotifyObservation(string id)
        {
            if (ended || spi == null) return;
            if (!spi.Observe(id)) return;
            if (!actionStarted) SetHint(string.Format(MachineLockTexts.HintStartFormat, spi.ObservedTargets));
        }

        /// <summary>
        /// A critical hazard happened. Penalties are applied once per contact, honouring the
        /// critical cooldown, and the matching alert is shown. In Guided the run continues.
        /// </summary>
        /// <param name="evt">Critical event type.</param>
        public void NotifyCritical(CriticalEvent evt)
        {
            if (ended || spi == null) return;
            if (!spi.ApplyCritical(evt, ElapsedSeconds)) return;

            switch (evt)
            {
                case CriticalEvent.LiveOilShock:
                    Alert(MachineLockTexts.Shock + "\n" + MachineLockTexts.Shock2);
                    break;
                case CriticalEvent.WaterExtinguisher:
                    Alert(MachineLockTexts.Water + "\n" + MachineLockTexts.Water2);
                    break;
                case CriticalEvent.TryStartUnisolated:
                    Alert(MachineLockTexts.TryFail + "\n" + MachineLockTexts.TryFail2);
                    break;
                default:
                    Alert(MachineLockTexts.HotOil + "\n" + MachineLockTexts.HotOil2);
                    break;
            }
        }

        /// <summary>
        /// The player reached the exit. With the press still running the exit is blocked; otherwise
        /// missing non-critical tasks are penalised, the supervisor line plays and the mission ends once.
        /// </summary>
        public void NotifyExitReached()
        {
            if (ended) return;
            if (!TaskEStop)
            {
                Alert(MachineLockTexts.ExitBlocked + "\n" + MachineLockTexts.ExitBlocked2);
                return;
            }
            if (TaskExit) return;

            TaskExit = true;
            actionStarted = true;
            ApplyHandoverPenalties();
            Apply(SpiEvent.Exit);
            RefreshObjectives();

            OutcomeType outcome = spi.EvaluateOutcome(BuildOutcomeInput());
            PlayLines(outcome == OutcomeType.Positive ? MachineLockTexts.PrepostoSafe : MachineLockTexts.PrepostoUnsafe);
            StartCoroutine(EndAfterSupervisorLine(outcome));
        }

        // =====================================================================
        // End
        // =====================================================================

        /// <summary>
        /// Ends the mission at most once, passing the weighted SPI, the normalized dimensions and the
        /// capped debrief lines to the common controller.
        /// </summary>
        /// <param name="outcome">Mission outcome.</param>
        public void EndMissionOnce(OutcomeType outcome)
        {
            if (ended) return;
            ended = true;
            state = MachineLockState.Ended;
            if (playingLines != null)
            {
                StopCoroutine(playingLines);
                playingLines = null;
            }

            if (spi == null)
            {
                EndMission(outcome, -1, null, null);
                return;
            }

            spi.SetElapsed(ElapsedSeconds);
            EndMission(outcome, spi.Spi, spi.Dimensions100(), MachineLockTexts.BuildDebriefLines(spi));
        }

        private IEnumerator EndAfterSupervisorLine(OutcomeType outcome)
        {
            yield return new WaitForSeconds(2.5f);
            EndMissionOnce(outcome);
        }

        // =====================================================================
        // Objectives, state, helpers
        // =====================================================================

        /// <summary>
        /// Completes every macro-objective that is already satisfied and keeps the wrist panel on the
        /// current one (GDD §10.3). Safe to call after every task event.
        /// </summary>
        public void RefreshObjectives()
        {
            while (currentMacro < MACRO_COUNT && MacroComplete(currentMacro))
            {
                CompleteObjective();
                currentMacro++;
            }
            if (currentMacro < MACRO_COUNT) SetObjective(currentMacro);
            UpdateState();
        }

        private bool MacroComplete(int macro)
        {
            switch (macro)
            {
                case 0:
                    return TaskEStop && TaskAlarm;
                case 1:
                    return TaskMainOff && TaskLockout && TaskTesterZero && TaskTryStart;
                case 2:
                    return TaskPpeFull && TaskContainment;
                case 3:
                    return TaskExit;
                default:
                    return false;
            }
        }

        private void UpdateState()
        {
            if (ended)
            {
                state = MachineLockState.Ended;
                return;
            }
            if (TaskExit) state = MachineLockState.HandoverReady;
            else if (TaskContainment) state = MachineLockState.Contained;
            else if (TaskPpeFull) state = MachineLockState.Protected;
            else if (TaskTesterZero && TaskTryStart) state = MachineLockState.ZeroEnergy;
            else if (TaskMainOff && TaskLockout) state = MachineLockState.Isolated;
            else if (TaskEStop) state = MachineLockState.MachineStopped;
            else state = MachineLockState.Emergency;
        }

        private void UpdateContainment()
        {
            bool complete = conesPlaced >= 4 && absorbentsPlaced >= 4;
            if (complete)
            {
                if (!TaskContainment)
                {
                    TaskContainment = true;
                    Apply(SpiEvent.ContainmentComplete);
                    SetPhase(PHASE_RESOLUTION);
                    Alert(MachineLockTexts.Containment);
                }
                SetHint(MachineLockTexts.HintExit);
            }
            else
            {
                SetHint(string.Format(MachineLockTexts.HintContainmentPartialFormat, conesPlaced, absorbentsPlaced));
            }
            RefreshObjectives();
        }

        private void ApplyHandoverPenalties()
        {
            if (spi == null) return;
            if (!TaskAlarm) spi.ApplyEvent(SpiEvent.MissingAlarm);
            if (!TaskMainOff) spi.ApplyEvent(SpiEvent.MissingMainOff);
            if (!TaskLockout) spi.ApplyEvent(SpiEvent.MissingLockout);
            if (!TaskContainment)
            {
                if (conesPlaced < 4) spi.ApplyEvent(SpiEvent.MissingCones);
                if (absorbentsPlaced < 4) spi.ApplyEvent(SpiEvent.MissingAbsorbents);
            }
        }

        private MachineLockOutcomeInput BuildOutcomeInput()
        {
            return new MachineLockOutcomeInput
            {
                PressRunning = !TaskEStop,
                Contained = TaskContainment,
                AllTasksComplete = TaskEStop && TaskAlarm && TaskMainOff && TaskLockout
                                  && TaskTesterZero && TaskTryStart && TaskPpeFull && TaskContainment && TaskExit,
                AnyCritical = spi != null && spi.CriticalOccurred,
                TimedOut = spi != null && spi.TimedOut
            };
        }

        private OutcomeType EvaluateTimeoutOutcome()
        {
            return TaskEStop && TaskContainment ? OutcomeType.Partial : OutcomeType.Negative;
        }

        private void Apply(SpiEvent evt)
        {
            if (spi == null) return;
            spi.SetElapsed(ElapsedSeconds);
            spi.ApplyEvent(evt);
        }

        private MachineLockConfig ResolveConfig()
        {
            if (config != null) return config;
            Debug.LogWarning("[SI81 MachineLock] MachineLockScenario has no SO_MachineLockConfig: using the GDD defaults. " +
                             "Assign the config on MNG_Scenario.", this);
            config = ScriptableObject.CreateInstance<MachineLockConfig>();
            return config;
        }

        private void PlayLines(params string[] lines)
        {
            if (playingLines != null) StopCoroutine(playingLines);
            playingLines = StartCoroutine(PlayLinesRoutine(lines));
        }

        private IEnumerator PlayLinesRoutine(string[] lines)
        {
            foreach (string line in lines)
            {
                if (ended) yield break;
                Subtitle(null, line, 2.5f);
                yield return new WaitForSeconds(2.5f);
            }
            playingLines = null;
        }
    }
}
