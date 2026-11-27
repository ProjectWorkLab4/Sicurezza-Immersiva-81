using System;
using System.Collections;
using System.IO;
using SI81.Input;
using SI81.Rig;
using UnityEngine.SceneManagement;
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
    /// It owns the task flags, the derived macro-objectives, the HUD texts, the SPI, the randomised
    /// layout, the inaction timer and the single EndMission call. Content components (press, panel, PPE,
    /// spill, observation) report through the Notify methods; every method is null-safe and idempotent
    /// and ignores calls after the end.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Scenario")]
    public sealed class MachineLockScenario : ScenarioController
    {
        /// <summary>Number of derived macro-objectives (one per mission objective).</summary>
        public const int MACRO_COUNT = 4;

        /// <summary>Footprint the puddle quadrant meshes were authored for (GDD §7.6).</summary>
        private const float PUDDLE_BASE_WIDTH = 7f;
        /// <summary>Depth the puddle quadrant meshes were authored for (GDD §7.6).</summary>
        private const float PUDDLE_BASE_LENGTH = 5f;
        /// <summary>Smoke emission rate of inaction stage 1, in particles per second.</summary>
        private const float SMOKE_STAGE1_RATE = 25f;
        /// <summary>Smoke emission rate of inaction stage 2, in particles per second.</summary>
        private const float SMOKE_STAGE2_RATE = 60f;
        /// <summary>Seconds each subtitle stays on screen.</summary>
        private const float LINE_SECONDS = 2.5f;
        /// <summary>Persistent shuffle-bag filename for the six electrical Sub-Station slot permutations.</summary>
        private const string PANEL_ORDER_STATE_FILE = "machinelock_panel_order_state.json";
        /// <summary>Half-width of the active stamping bay along X (West door leaf sits at -9.00 m).</summary>
        private const float BAY_HALF_X = 9.0f;
        /// <summary>Half-depth of the active stamping bay along Z.</summary>
        private const float BAY_HALF_Z = 5.5f;
        /// <summary>Preferred clearance kept between the outer panel edges and bay walls/exit door.</summary>
        private const float DECISION_WALL_CLEARANCE_M = 0.22f;
        /// <summary>Minimum clearance kept between panel edges and walls when standing very close to the door.</summary>
        private const float DECISION_MIN_WALL_CLEARANCE_M = 0.12f;
        /// <summary>Emergency clearance when the player leans directly against the closed door leaf.</summary>
        private const float DECISION_EMERGENCY_WALL_CLEARANCE_M = 0.04f;
        /// <summary>Preferred minimum eye-to-panel distance before tightening wall clearance.</summary>
        private const float DECISION_MIN_EYE_DISTANCE_M = 0.35f;
        /// <summary>Half-width of HUD_DecisionPanel (480 Canvas units * 0.5 * 0.00126) per metre of distance, slightly padded.</summary>
        private const float DECISION_HALF_WIDTH_PER_METER = 0.31f;
        /// <summary>Minimum world Y height for the clamped exit decision panel.</summary>
        private const float DECISION_MIN_Y = 1.05f;
        /// <summary>Maximum world Y height for the clamped exit decision panel.</summary>
        private const float DECISION_MAX_Y = 2.15f;

        [Header("Machine Lock")]
        [Tooltip("Tunable Machine Lock values (SO_MachineLockConfig).")]
        [SerializeField] private MachineLockConfig config;

        [Header("Scene wiring")]
        [Tooltip("Player spawn point used by the Guided respawn after a critical contact.")]
        [SerializeField] private Transform spawnPointTransform;
        [Tooltip("Hydraulic press of ENV_Press.")]
        [SerializeField] private MachineLockPress press;
        [Tooltip("Department alarm of ENV_Safety.")]
        [SerializeField] private MachineLockAlarm alarm;
        [Tooltip("Electrical panel of ENV_Safety.")]
        [SerializeField] private MachineLockEnergyPanel energyPanel;
        [Tooltip("PPE cabinet of ENV_Safety.")]
        [SerializeField] private MachineLockPpeCabinet ppeCabinet;
        [Tooltip("Hot oil spill system of HAZARD_Oil.")]
        [SerializeField] private MachineLockSpillSystem spillSystem;
        [Tooltip("Water extinguisher of ENV_Safety.")]
        [SerializeField] private MachineLockExtinguisher extinguisher;
        [Tooltip("Damage feedback (URP vignette plus fallback) of MNG_Scenario.")]
        [SerializeField] private MachineLockDamageFeedback damageFeedback;

        [Header("Layout patterns (GDD §7.6)")]
        [Tooltip("Three prop layouts under MNG_LayoutPatterns: PATTERN_A, PATTERN_B, PATTERN_C.")]
        [SerializeField] private Transform[] layoutPatterns = new Transform[3];
        [Tooltip("Three puddle footprints under HAZARD_Oil: PATTERN_PuddleA, PATTERN_PuddleB, PATTERN_PuddleC.")]
        [SerializeField] private Transform[] puddlePatterns = new Transform[3];

        [Header("Moved scene roots")]
        [Tooltip("PPE cabinet root moved by the layout pattern.")]
        [SerializeField] private Transform ppeCabinetTransform;
        [Tooltip("Spill kit root moved by the layout pattern.")]
        [SerializeField] private Transform spillKitTransform;
        [Tooltip("Water extinguisher root moved by the layout pattern.")]
        [SerializeField] private Transform extinguisherTransform;
        [Tooltip("OBS_PpeCabinet target, kept above the cabinet whatever the layout.")]
        [SerializeField] private Transform obsPpeCabinetTransform;
        [Tooltip("VFX_OilPuddle root scaled by the selected puddle pattern.")]
        [SerializeField] private Transform puddleVisualRoot;

        [Header("VFX and audio")]
        [Tooltip("Inaction smoke above the press.")]
        [SerializeField] private ParticleSystem smokeVfx;
        [Tooltip("Steam over the hot oil puddle.")]
        [SerializeField] private ParticleSystem steamVfx;
        [Tooltip("Looping factory room tone of MNG_Audio.")]
        [SerializeField] private AudioSource ambientAudio;

        [Header("Guided wayfinding")]
        [Tooltip("Overhead illuminated exit sign/beacon above the west doorway.")]
        [SerializeField] private Renderer exitBeaconRenderer;
        [Tooltip("Idle material for the overhead exit beacon before containment is complete.")]
        [SerializeField] private Material exitBeaconIdleMaterial;
        [Tooltip("Bright green illuminated material for the overhead exit beacon once press is stopped and spill contained.")]
        [SerializeField] private Material exitBeaconReadyMaterial;

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
        private Coroutine respawnRoutine;
        private MachineLockPatternSelector patternSelector;
        private MachineLockPatternSelector panelOrderSelector;
        private int forcedPatternIndex = -1;
        private int forcedSubStationOrderIndex = -1;
        private DecisionPanel activeExitDecisionPanel;
        private InputContextHandle? worldLockHandle;
        private bool worldVisualsHidden;
        private DifficultyLevel? forcedLevel;

        /// <summary>Current high-level state.</summary>
        public MachineLockState State => state;
        /// <summary>SPI of the current run; null before the scenario starts.</summary>
        public MachineLockSpi Spi => spi;
        /// <summary>Config in use, resolved to the GDD defaults when the asset is missing.</summary>
        public MachineLockConfig Config => ResolveConfig();
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
        /// Number of completed macro-objectives, 0 to <see cref="MACRO_COUNT"/>. Mirrors the derived macro
        /// list even when the shared controller has not finished its HUD wait, which keeps the mission
        /// state readable in tests.
        /// </summary>
        public new int ObjectivesCompleted => currentMacro;
        /// <summary>Index of the applied layout pattern, 0 to 2; -1 before the first application.</summary>
        public int ActivePatternIndex { get; private set; } = -1;
        /// <summary>Index of the applied electrical Sub-Station permutation, 0 to 5; -1 without panel.</summary>
        public int ActiveSubStationOrderIndex => energyPanel != null ? energyPanel.ActiveSubStationOrderIndex : -1;
        /// <summary>true while the shared HUD_DecisionPanel opened by the exit door is still active.</summary>
        public bool IsExitConfirmationPanelOpen => activeExitDecisionPanel != null && activeExitDecisionPanel.IsOpen;
        /// <summary>true when the scenario has locked locomotion and world interaction (keeping UI interactive).</summary>
        public bool IsMovementLockedForEndUi => worldLockHandle.HasValue;
        /// <summary>true when <see cref="damageFeedback"/> is wired to drive the smooth exit fade-to-black backdrop.</summary>
        public bool HasExitFadeFeedback => damageFeedback != null && damageFeedback.isActiveAndEnabled;
        /// <summary>true after 3D workshop visuals have been hidden behind the black end-of-level UI backdrop.</summary>
        public bool AreWorldVisualsHiddenForEndUi => worldVisualsHidden;
        /// <summary>Seconds the press kept running without an E-stop (inaction timer, GDD §9.12).</summary>
        public float InactionElapsedSeconds { get; private set; }
        /// <summary>Smoke stage reached by the inaction timer: 0 none, 1 stage 1, 2 stage 2.</summary>
        public int SmokeStage { get; private set; }

        /// <summary>true when press is stopped (E-stop or main disconnect) and spill containment is complete (exit beacon bright green).</summary>
        public bool IsExitBeaconReady => (TaskEStop || TaskMainOff) && TaskContainment;
        /// <summary>true while the press has neither been E-stopped nor isolated by the main disconnect.</summary>
        public bool IsPressRunning => !TaskEStop && !TaskMainOff;
        /// <summary>Current concise concrete objective text pushed to HUD_WristPanel (synced 1:1 with PauseChecklist).</summary>
        public string CurrentWristObjectiveText { get; private set; } = string.Empty;
        /// <summary>Number of required concrete safety steps currently completed (0 to 9).</summary>
        public int CompletedRequiredSteps { get; private set; }
        /// <summary>Most recent state-derived Guided wrist hint text.</summary>
        public string CurrentHintText { get; private set; } = string.Empty;
        /// <summary>Most recent state-derived HUD phase key.</summary>
        public string CurrentPhaseKey { get; private set; } = PHASE_SCAN;

        /// <summary>Level used by the run: the forced test level, or the session level.</summary>
        private DifficultyLevel EffectiveLevel => forcedLevel ?? Level;

        /// <summary>true once full LOTO isolation and 4/4 correct PPE are active before starting containment.</summary>
        private bool IsReadyForContainment => TaskMainOff && TaskLockout && TaskTesterZero && TaskTryStart && TaskPpeFull;

        /// <summary>
        /// Starts the scenario: creates the SPI, applies the layout pattern via the persistent shuffle-bag,
        /// starts the room tone, sets the scan phase, plays the intro subtitles and shows the first hint.
        /// Called by the base controller once the HUD is ready.
        /// </summary>
        protected override void OnScenarioStarted()
        {
            ended = false;
            state = MachineLockState.Intro;
            currentMacro = 0;
            InactionElapsedSeconds = 0f;
            SmokeStage = 0;
            CurrentPhaseKey = string.Empty;
            spi = new MachineLockSpi(ResolveConfig().CreateSpiOptions(EffectiveLevel));
            spi.SetElapsed(0f);

            int pattern = ResolvePatternSelector().NextPattern(forcedPatternIndex);
            ApplyLayoutPattern(pattern);
            int panelOrder = ResolvePanelOrderSelector().NextPattern(forcedSubStationOrderIndex);
            ApplySubStationOrder(panelOrder);
            StartAmbientAudio();

            RefreshObjectives();
            PlayLines(MachineLockTexts.SpeakerSystem, MachineLockTexts.Intro1, MachineLockTexts.Intro2);
        }

        /// <summary>
        /// Handles the level timer. Guided has no countdown and does not end here; Realistic and
        /// Stress end with Time = 0 and an outcome based on the current state.
        /// </summary>
        protected override void OnTimerExpired()
        {
            if (ended) return;
            if (EffectiveLevel == DifficultyLevel.Guided) return;
            if (spi != null)
            {
                spi.SetElapsed(ElapsedSeconds);
                spi.ApplyEvent(SpiEvent.Timeout);
            }
            RefreshObjectives();
            EndMissionOnce(EvaluateTimeoutOutcome());
        }

        /// <summary>
        /// Drives the inaction timer while the press keeps running (GDD §9.12). The timer is stopped by
        /// any E-stop, opening the main disconnect, or once exit handover begins; it is independent of
        /// the level time limit.
        /// </summary>
        protected override void OnDestroy()
        {
            ReleaseExitSequenceLock();
            MissionFlow.PauseChecklist = null;
            if (damageFeedback != null) damageFeedback.Clear();
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsRunning || ended || TaskEStop || TaskMainOff || TaskExit || worldLockHandle.HasValue) return;
            AdvanceInaction(Time.deltaTime);
        }

        // =====================================================================
        // Task events (GDD §10.5)
        // =====================================================================

        /// <summary>
        /// E-stop pressed. Stops motion only: the display stays on and energy is not isolated.
        /// Cancels the inaction timer and clears the smoke.
        /// </summary>
        public void NotifyEStopPressed()
        {
            if (ended || TaskEStop) return;
            TaskEStop = true;
            actionStarted = true;
            CancelInaction();
            Apply(SpiEvent.EStop);
            PlayLines(MachineLockTexts.SpeakerSystem, MachineLockTexts.EStop, MachineLockTexts.EStop2);
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
            PlayLines(MachineLockTexts.SpeakerSystem, MachineLockTexts.Alarm, MachineLockTexts.Alarm2);
            RefreshObjectives();
        }

        /// <summary>
        /// Main switch opened: the display and the motor go off, cancelling the inaction timer.
        /// Opening it before pressing E-stop applies a one-time procedural Decision penalty while
        /// keeping E-stop completable afterward.
        /// </summary>
        public void NotifyMainPowerOff()
        {
            if (ended || TaskMainOff) return;
            if (!TaskEStop) Apply(SpiEvent.SequenceMainOffBeforeEStop);
            TaskMainOff = true;
            actionStarted = true;
            CancelInaction();
            Apply(SpiEvent.MainOff);
            PlayLines(MachineLockTexts.SpeakerSystem, MachineLockTexts.Panel, MachineLockTexts.Panel2);
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
            PlayLines(MachineLockTexts.SpeakerSystem, MachineLockTexts.Lockout, MachineLockTexts.Lockout2);
            RefreshObjectives();
        }

        /// <summary>
        /// Voltage tester read. Zero confirms that no residual energy is left.
        /// </summary>
        /// <param name="zero">true when the reading is ZERO.</param>
        public void NotifyTesterRead(bool zero)
        {
            NotifyTesterRead(zero, null);
        }

        /// <summary>
        /// Voltage tester read with the exact display readout (GDD §9.4). The HUD alert reports the same
        /// text the tester shows, so LIVE and ZERO are never ambiguous.
        /// </summary>
        /// <param name="zero">true when the reading is ZERO.</param>
        /// <param name="readout">Exact tester readout; null or empty falls back to the generic text.</param>
        public void NotifyTesterRead(bool zero, string readout)
        {
            if (ended) return;
            if (!zero)
            {
                string live = !string.IsNullOrEmpty(readout)
                    ? readout + "\n" + MachineLockTexts.TesterLive2
                    : MachineLockTexts.TesterLive + "\n" + MachineLockTexts.TesterLive2;
                Alert(live);
                return;
            }

            if (TaskTesterZero) return;
            TaskTesterZero = true;
            actionStarted = true;
            Apply(SpiEvent.TesterZero);
            Alert(!string.IsNullOrEmpty(readout) ? readout : MachineLockTexts.TesterZero);
            RefreshObjectives();
        }

        /// <summary>
        /// Try-start performed and the machine did not move: isolation confirmed. Executing try-start
        /// before the voltage tester confirms ZERO applies a one-time procedural Decision penalty while
        /// still marking try-start completed.
        /// </summary>
        public void NotifyTryStartVerified()
        {
            if (ended || TaskTryStart) return;
            if (!TaskTesterZero) Apply(SpiEvent.SequenceTryStartBeforeTesterZero);
            TaskTryStart = true;
            actionStarted = true;
            Apply(SpiEvent.TryStartVerified);
            Feedback(MachineLockTexts.TryOk, FeedbackKind.Positive);
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
            }
            if (spillSystem != null) spillSystem.SetContainmentWayfindingActive(full);
            RefreshObjectives();
        }

        /// <summary>
        /// A cone snapped to a delimitation zone.
        /// </summary>
        /// <param name="count">Total number of placed cones, 0 to 4.</param>
        public void NotifyConePlaced(int count)
        {
            if (ended) return;
            if (!IsReadyForContainment) Apply(SpiEvent.SequenceContainmentBeforeReady);
            conesPlaced = Mathf.Clamp(Mathf.Max(conesPlaced, count), 0, 4);
            actionStarted = true;
            Feedback(string.Format(MachineLockTexts.ConeFormat, conesPlaced), FeedbackKind.Positive);
            UpdateContainment();
        }

        /// <summary>
        /// An absorbent mat or sock snapped to a coverage zone. The Mat/Sock feedback is emitted once by
        /// <see cref="MachineLockSpillSystem.RegisterAbsorbent"/>; this method only updates the counters
        /// and the containment state.
        /// </summary>
        /// <param name="count">Total number of placed absorbents, 0 to 4.</param>
        public void NotifyAbsorbentPlaced(int count)
        {
            if (ended) return;
            if (!IsReadyForContainment) Apply(SpiEvent.SequenceContainmentBeforeReady);
            absorbentsPlaced = Mathf.Clamp(Mathf.Max(absorbentsPlaced, count), 0, 4);
            actionStarted = true;
            UpdateContainment();
        }

        /// <summary>
        /// Leak valve closed. No score, but a good-practice debrief line (GDD §9.7).
        /// </summary>
        public void NotifyValveClosed()
        {
            if (ended || valveClosed) return;
            valveClosed = true;
            actionStarted = true;
            Apply(SpiEvent.ValveClosed);
            Feedback(MachineLockTexts.Valve, FeedbackKind.Positive);
            RefreshObjectives();
        }

        /// <summary>
        /// An observation target was looked at long enough. Each distinct id scores once.
        /// </summary>
        /// <param name="id">Stable target identifier (press, thermometer, display, puddle, panel, ppe_cabinet).</param>
        public void NotifyObservation(string id)
        {
            if (ended || spi == null) return;
            if (!spi.Observe(id)) return;
            RefreshObjectives();
        }

        /// <summary>
        /// A critical hazard happened. Penalties are applied once per contact, honouring the
        /// critical cooldown, and the matching alert is shown. Damage feedback always plays; in Guided the
        /// player is teleported back to the spawn point after the config delay.
        /// </summary>
        /// <param name="evt">Critical event type.</param>
        public void NotifyCritical(CriticalEvent evt)
        {
            if (ended || spi == null) return;
            if (!spi.ApplyCritical(evt, ElapsedSeconds)) return;

            if (damageFeedback != null) damageFeedback.PlayCritical(evt);

            switch (evt)
            {
                case CriticalEvent.LiveOilShock:
                    Alert(MachineLockTexts.Shock + "\n" + MachineLockTexts.Shock2);
                    if (HUDService.IsReady) HUDService.ShowSoundEvent(MachineLockTexts.SoundShock, true);
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

            if (EffectiveLevel == DifficultyLevel.Guided) StartRespawn();
        }

        /// <summary>
        /// Opens (or recenters) the shared <c>HUD_DecisionPanel</c> safely inside the stamping bay to ask
        /// the player whether they want to leave the room now (ADR-0006, Issue #43).
        /// </summary>
        /// <param name="onChosen">Callback invoked with 0 (confirm exit), 1 (stay), or -1 (timeout).</param>
        /// <returns>true when the shared decision panel opened cleanly.</returns>
        public bool RequestExitConfirmation(Action<int> onChosen)
        {
            if (ended || TaskExit) return false;
            bool opened = AskDecision(
                MachineLockTexts.ExitConfirmQuestion,
                new[] { MachineLockTexts.ExitConfirmYes, MachineLockTexts.ExitConfirmNo },
                onChosen,
                0f);
            if (!opened) return false;

            activeExitDecisionPanel = FindFirstObjectByType<DecisionPanel>(FindObjectsInactive.Include);
            if (activeExitDecisionPanel != null)
                PositionDecisionPanelSafely(activeExitDecisionPanel.transform, ResolvePlayerHead());
            return true;
        }

        /// <summary>
        /// Closes the shared <c>HUD_DecisionPanel</c> if it is currently open for an exit prompt.
        /// </summary>
        public void CloseActiveExitConfirmation()
        {
            if (activeExitDecisionPanel != null && activeExitDecisionPanel.IsOpen)
                activeExitDecisionPanel.Close();
            activeExitDecisionPanel = null;
        }

        /// <summary>
        /// Resolves the active player head transform (shared rig first, then UI/Camera fallback).
        /// </summary>
        public Transform ResolvePlayerHead()
        {
            if (RigRegistration.Instance != null && RigRegistration.Instance.Head != null)
                return RigRegistration.Instance.Head;
            return WorldPlacement.ResolveHead(null, nameof(MachineLockScenario));
        }

        /// <summary>
        /// Clamps <paramref name="panelTransform"/> (accounting for its full horizontal width at oblique
        /// gaze angles and close leans) strictly inside the stamping bay walls and strictly in front of the
        /// player's eyes, scaling by the actual 3D eye-to-panel distance via
        /// <see cref="UITheme.GetCanvasScale"/> (Issue #43).
        /// </summary>
        public static void PositionDecisionPanelSafely(Transform panelTransform, Transform head)
        {
            if (panelTransform == null || head == null) return;

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(head.forward.y < 0f ? head.up : -head.up, Vector3.up);
            forward = forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 headPos = head.position;
            float preferredDist = ComputeMaxPanelDistance(headPos, forward, right, DECISION_WALL_CLEARANCE_M);
            float finalDist;
            if (preferredDist >= DECISION_MIN_EYE_DISTANCE_M)
            {
                finalDist = preferredDist;
            }
            else
            {
                float tightDist = ComputeMaxPanelDistance(headPos, forward, right, DECISION_MIN_WALL_CLEARANCE_M);
                if (tightDist >= 0.15f)
                    finalDist = tightDist;
                else
                    finalDist = Mathf.Max(0.05f, ComputeMaxPanelDistance(headPos, forward, right, DECISION_EMERGENCY_WALL_CLEARANCE_M));
            }

            Vector3 position = headPos + forward * finalDist;
            position.y = Mathf.Clamp(headPos.y, DECISION_MIN_Y, DECISION_MAX_Y);
            float actualEyeDistance = Mathf.Max(0.05f, Vector3.Distance(headPos, position));
            panelTransform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            panelTransform.localScale = Vector3.one * UITheme.GetCanvasScale(actualEyeDistance);
        }

        private static float ComputeMaxPanelDistance(Vector3 headPos, Vector3 forward, Vector3 right, float wallClearance)
        {
            float maxX = BAY_HALF_X - wallClearance;
            float maxZ = BAY_HALF_Z - wallClearance;
            float extentX = Mathf.Abs(right.x) * DECISION_HALF_WIDTH_PER_METER;
            float extentZ = Mathf.Abs(right.z) * DECISION_HALF_WIDTH_PER_METER;

            float maxDist = UITheme.DECISION_DISTANCE_M;
            float westSlope = forward.x - extentX;
            if (westSlope < -0.0001f)
                maxDist = Mathf.Min(maxDist, (-maxX - headPos.x) / westSlope);
            float eastSlope = forward.x + extentX;
            if (eastSlope > 0.0001f)
                maxDist = Mathf.Min(maxDist, (maxX - headPos.x) / eastSlope);
            float southSlope = forward.z - extentZ;
            if (southSlope < -0.0001f)
                maxDist = Mathf.Min(maxDist, (-maxZ - headPos.z) / southSlope);
            float northSlope = forward.z + extentZ;
            if (northSlope > 0.0001f)
                maxDist = Mathf.Min(maxDist, (maxZ - headPos.z) / northSlope);

            return Mathf.Max(0f, maxDist);
        }

        /// <summary>
        /// Locks player locomotion, snap-turn, and world ray interactions via
        /// <see cref="InputChannel.World"/> while leaving <see cref="InputChannel.UI"/> fully interactive.
        /// Released automatically when the scenario unloads (<see cref="OnDestroy"/>).
        /// </summary>
        public void BeginExitSequenceLock()
        {
            if (worldLockHandle.HasValue) return;
            InputService service = InputService.Instance;
            if (service != null)
                worldLockHandle = service.PushContext(InputChannel.World, this);
        }

        /// <summary>
        /// Releases the <see cref="InputChannel.World"/> lock if active.
        /// </summary>
        public void ReleaseExitSequenceLock()
        {
            if (!worldLockHandle.HasValue) return;
            worldLockHandle.Value.Dispose();
            worldLockHandle = null;
        }

        /// <summary>
        /// Starts the smooth full-screen fade to pure black on <see cref="damageFeedback"/> and fades out
        /// scenario room audio. Invokes <paramref name="onFullyBlack"/> once the backdrop is solid black
        /// behind the UI render queue.
        /// </summary>
        public void StartExitFadeToBlack(float durationSeconds, Action onFullyBlack)
        {
            if (Application.isPlaying && ambientAudio != null && ambientAudio.isPlaying)
                StartCoroutine(FadeOutAudioRoutine(ambientAudio, durationSeconds));

            if (damageFeedback != null && damageFeedback.isActiveAndEnabled)
            {
                damageFeedback.PlayExitFadeToBlack(durationSeconds, onFullyBlack);
                return;
            }
            onFullyBlack?.Invoke();
        }

        /// <summary>
        /// Disables 3D Renderers, Lights, ParticleSystems, and looping audio under the scenario's world
        /// roots once faded to black, leaving floor colliders intact (so gravity never drops the rig) and
        /// leaving an empty Z-buffer so <c>HUD_OutcomePanel</c> and <c>CANVAS_Menu</c> never compenetrate
        /// with 3D geometry.
        /// </summary>
        public void HideScenarioWorldVisualsForEndUi()
        {
            if (worldVisualsHidden) return;
            worldVisualsHidden = true;

            Scene scenarioScene = gameObject.scene;
            if (!scenarioScene.IsValid() || !scenarioScene.isLoaded) return;

            GameObject[] roots = scenarioScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || root == gameObject) continue;
                bool isScenarioWorldRoot =
                    root.name.StartsWith("ENV_", StringComparison.Ordinal)
                    || root.name.StartsWith("HAZARD_", StringComparison.Ordinal)
                    || root.name.StartsWith("ZONE_", StringComparison.Ordinal);
                if (!isScenarioWorldRoot) continue;

                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                    if (r != null) r.enabled = false;
                foreach (Light l in root.GetComponentsInChildren<Light>(true))
                    if (l != null) l.enabled = false;
                foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
                    if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                foreach (AudioSource audio in root.GetComponentsInChildren<AudioSource>(true))
                    if (audio != null && audio.loop) audio.Stop();
            }

            if (ambientAudio != null) ambientAudio.Stop();
        }

        /// <summary>
        /// The player confirmed leaving the room through the west exit door (ADR-0006). Early Exit is
        /// always allowed, even while the press is still running: missing tasks (including an active press)
        /// are penalized at handover, the supervisor closing line plays, and the mission ends once.
        /// </summary>
        public void NotifyExitReached()
        {
            NotifyExitReached(false);
        }

        /// <summary>
        /// Resolves exit handover and ends the mission. When <paramref name="endImmediately"/> is true
        /// (after the locked door-open + fade-to-black sequence completes), ends immediately without an
        /// extra subtitle wait so <c>HUD_OutcomePanel</c> appears right away over the black backdrop.
        /// </summary>
        public void NotifyExitReached(bool endImmediately)
        {
            if (ended || TaskExit || spi == null) return;

            TaskExit = true;
            actionStarted = true;
            BeginExitSequenceLock();
            ApplyHandoverPenalties();
            if (TaskEStop || TaskMainOff) Apply(SpiEvent.Exit);
            RefreshObjectives();

            OutcomeType outcome = spi.EvaluateOutcome(BuildOutcomeInput());
            if (endImmediately || !Application.isPlaying)
            {
                EndMissionOnce(outcome);
                return;
            }

            PlayLines(MachineLockTexts.SpeakerSupervisor,
                outcome == OutcomeType.Positive ? MachineLockTexts.PrepostoSafe : MachineLockTexts.PrepostoUnsafe);
            StartCoroutine(EndAfterSupervisorLine(outcome));
        }

        private static IEnumerator FadeOutAudioRoutine(AudioSource source, float durationSeconds)
        {
            if (source == null) yield break;
            float startVolume = source.volume;
            float duration = Mathf.Max(0.05f, durationSeconds);
            float elapsed = 0f;
            while (elapsed < duration && source != null)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (source != null)
            {
                source.volume = 0f;
                source.Stop();
            }
        }

        // =====================================================================
        // Layout (GDD §7.6)
        // =====================================================================

        /// <summary>
        /// Forces the layout pattern used at the next start. 0, 1 and 2 force PATTERN_A, PATTERN_B and
        /// PATTERN_C and apply them immediately; a negative value randomises again at start.
        /// </summary>
        /// <param name="index">0 to 2 to force a pattern, any negative value to randomise.</param>
        public void SetForcedPatternIndex(int index)
        {
            forcedPatternIndex = index;
            if (index >= 0) ApplyLayoutPattern(index);
        }

        /// <summary>
        /// Forces the electrical Sub-Station permutation used at the next start (0 to 5) and applies it
        /// immediately; a negative value resumes the 6-state persistent shuffle-bag (Issue #43).
        /// </summary>
        /// <param name="index">0 to 5 to force a permutation, any negative value to shuffle.</param>
        public void SetForcedSubStationOrderIndex(int index)
        {
            forcedSubStationOrderIndex = index;
            if (index >= 0) ApplySubStationOrder(index);
        }

        /// <summary>
        /// Applies one of the six Sub-Station slot permutations to the electrical panel.
        /// </summary>
        /// <param name="index">Permutation index, normalized modulo 6.</param>
        public void ApplySubStationOrder(int index)
        {
            if (energyPanel == null) return;
            energyPanel.ApplySubStationOrder(index);
        }

        /// <summary>
        /// Applies one of the three prop layouts: moves the PPE cabinet, the spill kit and the water
        /// extinguisher onto the matching markers and rescales the puddle visual. Keeps the observation
        /// target above the cabinet and the press cutout centred on the machine.
        /// </summary>
        /// <param name="index">Pattern index, clamped to the configured patterns.</param>
        public void ApplyLayoutPattern(int index)
        {
            if (layoutPatterns == null || layoutPatterns.Length == 0) return;

            int clamped = Mathf.Clamp(index, 0, layoutPatterns.Length - 1);
            Transform pattern = layoutPatterns[clamped];
            if (pattern == null) return;

            ActivePatternIndex = clamped;
            MoveToMarker(pattern, "MarkerPpeCabinet", ppeCabinetTransform);
            MoveToMarker(pattern, "MarkerSpillKit", spillKitTransform);
            MoveToMarker(pattern, "MarkerExtinguisher", extinguisherTransform);

            if (obsPpeCabinetTransform != null && ppeCabinetTransform != null)
                obsPpeCabinetTransform.position = ppeCabinetTransform.position + Vector3.up;

            ApplyPuddlePattern(clamped);
        }

        private static void MoveToMarker(Transform pattern, string markerName, Transform target)
        {
            if (pattern == null || target == null) return;
            Transform marker = pattern.Find(markerName);
            if (marker == null) return;
            target.SetPositionAndRotation(marker.position, marker.rotation);
        }

        private void ApplyPuddlePattern(int index)
        {
            if (puddleVisualRoot == null || puddlePatterns == null || puddlePatterns.Length == 0) return;

            Transform pattern = puddlePatterns[Mathf.Clamp(index, 0, puddlePatterns.Length - 1)];
            if (pattern == null) return;

            // The quadrant meshes are authored for a 7 x 5 m footprint. Scaling the root by the ratio of
            // the pattern extents keeps the press cutout centred on the machine and the dry perimeter
            // aligned with PATTERN_PuddleA/B/C, without moving any quadrant child by hand.
            Vector3 extents = pattern.lossyScale;
            float scaleX = extents.x > 0.0001f ? extents.x / PUDDLE_BASE_WIDTH : 1f;
            float scaleZ = extents.z > 0.0001f ? extents.z / PUDDLE_BASE_LENGTH : 1f;
            puddleVisualRoot.SetPositionAndRotation(pattern.position, pattern.rotation);
            puddleVisualRoot.localScale = new Vector3(scaleX, 1f, scaleZ);
        }

        // =====================================================================
        // Tests and debug
        // =====================================================================

        /// <summary>
        /// Initialises the run state without SCN_Core, so the orchestrator can be exercised in EditMode
        /// tests: creates the SPI, clears the task flags, the counters and the inaction timer and applies
        /// no HUD call.
        /// </summary>
        /// <param name="overrideConfig">Config to use, or null to keep the assigned one.</param>
        /// <param name="overrideLevel">Level used by the run, also for the time limit and respawn rule.</param>
        public void InitializeForTests(MachineLockConfig overrideConfig, DifficultyLevel overrideLevel)
        {
            if (overrideConfig != null) config = overrideConfig;
            forcedLevel = overrideLevel;

            ended = false;
            state = MachineLockState.Intro;
            currentMacro = 0;
            correctPpeCount = 0;
            conesPlaced = 0;
            absorbentsPlaced = 0;
            valveClosed = false;
            actionStarted = false;
            ActivePatternIndex = -1;
            InactionElapsedSeconds = 0f;
            SmokeStage = 0;
            CurrentWristObjectiveText = string.Empty;
            CompletedRequiredSteps = 0;
            CurrentHintText = string.Empty;
            CurrentPhaseKey = string.Empty;
            ResetTaskFlags();

            spi = new MachineLockSpi(ResolveConfig().CreateSpiOptions(overrideLevel));
            spi.SetElapsed(0f);
            RefreshObjectives();
        }

        /// <summary>
        /// Advances the inaction timer deterministically, without waiting for real time. Used by the
        /// EditMode tests of the smoke stages and of Bad Ending 2 (GDD §9.12).
        /// </summary>
        /// <param name="deltaSeconds">Seconds to add to the inaction timer.</param>
        public void TickInactionForTests(float deltaSeconds)
        {
            AdvanceInaction(deltaSeconds);
        }

        // =====================================================================
        // Inaction (GDD §9.12)
        // =====================================================================

        private void AdvanceInaction(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || ended || TaskEStop || TaskMainOff) return;

            InactionElapsedSeconds += deltaSeconds;
            MachineLockConfig resolved = ResolveConfig();

            if (SmokeStage < 1 && InactionElapsedSeconds >= resolved.inactionSmokeSeconds)
            {
                SmokeStage = 1;
                PlaySmoke(SMOKE_STAGE1_RATE);
                Alert(MachineLockTexts.SmokeStage1 + "\n" + MachineLockTexts.SmokeStage1Detail);
            }

            if (SmokeStage < 2 && InactionElapsedSeconds >= resolved.inactionSmokeStage2Seconds)
            {
                SmokeStage = 2;
                PlaySmoke(SMOKE_STAGE2_RATE);
                Alert(MachineLockTexts.SmokeStage2 + "\n" + MachineLockTexts.SmokeStage2Detail);
            }

            if (InactionElapsedSeconds < resolved.inactionBadEndingSeconds) return;
            Apply(SpiEvent.InactionBadEnding);
            EndMissionOnce(OutcomeType.Negative);
        }

        private void PlaySmoke(float rate)
        {
            if (smokeVfx == null) return;
            if (!smokeVfx.gameObject.activeSelf) smokeVfx.gameObject.SetActive(true);
            ParticleSystem.EmissionModule emission = smokeVfx.emission;
            emission.rateOverTime = rate;
            smokeVfx.Play(true);
        }

        private void CancelInaction()
        {
            InactionElapsedSeconds = 0f;
            SmokeStage = 0;
            if (smokeVfx == null) return;
            smokeVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void StartAmbientAudio()
        {
            if (ambientAudio == null) return;
            ambientAudio.loop = true;
            if (!ambientAudio.isPlaying) ambientAudio.Play();
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
            activeExitDecisionPanel = null;
            MissionFlow.PauseChecklist = null;
            BeginExitSequenceLock();
            state = MachineLockState.Ended;
            if (playingLines != null)
            {
                StopCoroutine(playingLines);
                playingLines = null;
            }
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }

            if (spi == null)
            {
                EndMission(outcome, -1, null, null);
                return;
            }

            // Outside a live run (tests, or a mission ended before the HUD was ready) the SPI already holds
            // the elapsed time: rewriting it would clear a TimedOut flag set by the timeout rule.
            if (IsRunning) spi.SetElapsed(ElapsedSeconds);
            EndMission(outcome, spi.Spi, spi.Dimensions100(), MachineLockTexts.BuildDebriefLines(spi));
        }

        private IEnumerator EndAfterSupervisorLine(OutcomeType outcome)
        {
            yield return new WaitForSeconds(LINE_SECONDS);
            EndMissionOnce(outcome);
        }

        private void StartRespawn()
        {
            if (!Application.isPlaying) return;
            if (respawnRoutine != null) StopCoroutine(respawnRoutine);
            respawnRoutine = StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            const float blinkHalfSeconds = 0.15f;
            float delay = Mathf.Max(0f, ResolveConfig().respawnDelaySeconds - blinkHalfSeconds);
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (ended)
            {
                respawnRoutine = null;
                yield break;
            }

            if (damageFeedback != null)
            {
                yield return damageFeedback.PlayRespawnBlink(blinkHalfSeconds, TeleportRigToSpawn);
            }
            else
            {
                yield return new WaitForSeconds(blinkHalfSeconds);
                if (!ended) TeleportRigToSpawn();
            }
            respawnRoutine = null;
        }

        private void TeleportRigToSpawn()
        {
            if (ended) return;
            RigRegistration rig = RigRegistration.Instance;
            Transform target = spawnPointTransform;
            if (rig == null || target == null) return;
            rig.TeleportTo(target.position, Quaternion.Euler(0f, target.eulerAngles.y, 0f));
        }

        // =====================================================================
        // Objectives, state, helpers
        // =====================================================================

        /// <summary>
        /// Recomputes derived macro progress, state, exit beacon, Pause checklist, Wrist Panel objective/counter,
        /// Guided wrist hint, and monotonic HUD phase from live task flags. Safe to call after any task event
        /// regardless of completion order.
        /// </summary>
        public void RefreshObjectives()
        {
            int firstIncompleteMacro = 0;
            while (firstIncompleteMacro < MACRO_COUNT && MacroComplete(firstIncompleteMacro))
                firstIncompleteMacro++;
            currentMacro = firstIncompleteMacro;
            MissionFlow.ObjectiveIndex = Mathf.Clamp(currentMacro, 0, MACRO_COUNT - 1);

            UpdateState();
            RefreshExitBeacon();
            SyncPauseChecklist();
        }

        /// <summary>
        /// Single source of truth for both <see cref="MissionFlow.PauseChecklist"/> (9 required safety tasks +
        /// 2 non-blocking awareness/valve rows) and <c>HUD_WristPanel</c> (active concrete step, completed
        /// count out of 9, state-derived Guided hint, and monotonic phase).
        /// </summary>
        public void SyncPauseChecklist()
        {
            int observed = spi != null ? spi.ObservedTargets : 0;
            int observedCap = config != null ? config.awarenessTargets : 6;
            bool observedDone = observed >= observedCap;

            string ppeHint = correctPpeCount > 0
                ? string.Format(MachineLockTexts.HintPpePartialFormat, correctPpeCount)
                : MachineLockTexts.HintPpe;
            bool containmentStarted = conesPlaced > 0 || absorbentsPlaced > 0;
            string containmentWrist = containmentStarted
                ? string.Format(MachineLockTexts.WristContainmentPartialFormat, conesPlaced, absorbentsPlaced)
                : MachineLockTexts.WristContainment;
            string containmentHint = containmentStarted || valveClosed
                ? string.Format(MachineLockTexts.HintContainmentPartialFormat, conesPlaced, absorbentsPlaced)
                : MachineLockTexts.HintValveAndContainment;

            var requiredSteps = new (string PauseText, string WristText, string HintText, bool Done)[]
            {
                (MachineLockTexts.ChecklistEStop, MachineLockTexts.WristEStop, MachineLockTexts.HintEStop, TaskEStop),
                (MachineLockTexts.ChecklistAlarm, MachineLockTexts.WristAlarm, MachineLockTexts.HintAlarm, TaskAlarm),
                (MachineLockTexts.ChecklistMainOff, MachineLockTexts.WristMainOff, MachineLockTexts.HintMainPower, TaskMainOff),
                (MachineLockTexts.ChecklistLockout, MachineLockTexts.WristLockout, MachineLockTexts.HintLockout, TaskLockout),
                (MachineLockTexts.ChecklistTesterZero, MachineLockTexts.WristTesterZero, MachineLockTexts.HintTester, TaskTesterZero),
                (MachineLockTexts.ChecklistTryStart, MachineLockTexts.WristTryStart, MachineLockTexts.HintTryStart, TaskTryStart),
                (string.Format(MachineLockTexts.ChecklistPpeFormat, correctPpeCount),
                    string.Format(MachineLockTexts.WristPpeFormat, correctPpeCount), ppeHint, TaskPpeFull),
                (string.Format(MachineLockTexts.ChecklistContainmentFormat, conesPlaced, absorbentsPlaced),
                    containmentWrist, containmentHint, TaskContainment),
                (MachineLockTexts.ChecklistExit, MachineLockTexts.WristExit, MachineLockTexts.HintExit, TaskExit)
            };

            var entries = new PauseChecklistEntry[requiredSteps.Length + 2];
            int completedRequired = 0;
            int activeIndex = -1;
            for (int i = 0; i < requiredSteps.Length; i++)
            {
                ObjectiveState stepState;
                if (requiredSteps[i].Done)
                {
                    stepState = ObjectiveState.Done;
                    completedRequired++;
                }
                else if (activeIndex < 0)
                {
                    stepState = ObjectiveState.Current;
                    activeIndex = i;
                }
                else
                {
                    stepState = ObjectiveState.Todo;
                }
                entries[i] = new PauseChecklistEntry(requiredSteps[i].PauseText, stepState);
            }

            // Non-blocking Awareness and optional Valve rows stay visible with live checkmarks
            // without ever stealing Current focus from required safety tasks.
            entries[requiredSteps.Length] = new PauseChecklistEntry(
                string.Format(MachineLockTexts.ChecklistObserveFormat, observed),
                observedDone ? ObjectiveState.Done : ObjectiveState.Todo);
            entries[requiredSteps.Length + 1] = new PauseChecklistEntry(
                MachineLockTexts.ChecklistValve,
                valveClosed ? ObjectiveState.Done : ObjectiveState.Todo);

            MissionFlow.PauseChecklist = entries;

            int wristIndex = activeIndex >= 0 ? activeIndex : requiredSteps.Length - 1;
            CurrentWristObjectiveText = requiredSteps[wristIndex].WristText;
            CompletedRequiredSteps = completedRequired;
            HUDService.SetObjective(CurrentWristObjectiveText);
            HUDService.SetObjectiveProgress(completedRequired, requiredSteps.Length);

            string derivedHint = (!actionStarted && !TaskEStop && !observedDone)
                ? string.Format(MachineLockTexts.HintStartFormat, observed)
                : requiredSteps[wristIndex].HintText;
            CurrentHintText = derivedHint;
            SetHint(derivedHint);

            string derivedPhase = ResolvePhaseKey();
            if (CurrentPhaseKey != derivedPhase || MissionFlow.CurrentPhaseKey != derivedPhase)
            {
                CurrentPhaseKey = derivedPhase;
                SetPhase(derivedPhase);
            }
        }

        private string ResolvePhaseKey()
        {
            if (TaskContainment || TaskExit) return PHASE_RESOLUTION;
            if (actionStarted || TaskEStop || TaskAlarm || TaskMainOff || TaskLockout
                || TaskTesterZero || TaskTryStart || TaskPpeFull || valveClosed
                || conesPlaced > 0 || absorbentsPlaced > 0)
                return PHASE_ACTION;
            return PHASE_SCAN;
        }

        private void RefreshExitBeacon()
        {
            if (exitBeaconRenderer == null) return;
            Material targetMaterial = IsExitBeaconReady ? exitBeaconReadyMaterial : exitBeaconIdleMaterial;
            if (targetMaterial != null) exitBeaconRenderer.sharedMaterial = targetMaterial;
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
            else if (TaskEStop || TaskMainOff) state = MachineLockState.MachineStopped;
            else state = MachineLockState.Emergency;
        }

        private void UpdateContainment()
        {
            bool complete = conesPlaced >= 4 && absorbentsPlaced >= 4;
            if (complete && !TaskContainment)
            {
                TaskContainment = true;
                Apply(SpiEvent.ContainmentComplete);
                Alert(MachineLockTexts.Containment);
            }
            RefreshObjectives();
        }

        private void ApplyHandoverPenalties()
        {
            if (spi == null) return;
            if (!TaskEStop && !TaskMainOff) spi.ApplyEvent(SpiEvent.MissingEStop);
            if (!TaskAlarm) spi.ApplyEvent(SpiEvent.MissingAlarm);
            if (!TaskMainOff) spi.ApplyEvent(SpiEvent.MissingMainOff);
            if (!TaskLockout) spi.ApplyEvent(SpiEvent.MissingLockout);
            if (!TaskContainment)
            {
                if (conesPlaced < 4) spi.ApplyEvent(SpiEvent.MissingCones);
                if (absorbentsPlaced < 4) spi.ApplyEvent(SpiEvent.MissingAbsorbents);
            }
        }

        private MachineLockPatternSelector ResolvePatternSelector()
        {
            int count = layoutPatterns != null && layoutPatterns.Length > 0
                ? layoutPatterns.Length
                : MachineLockPatternSelector.DEFAULT_PATTERN_COUNT;
            return patternSelector ?? (patternSelector = CreatePersistentSelector(MachineLockPatternSelector.DEFAULT_FILE_NAME, count));
        }

        private MachineLockPatternSelector ResolvePanelOrderSelector()
        {
            return panelOrderSelector ?? (panelOrderSelector = CreatePersistentSelector(
                PANEL_ORDER_STATE_FILE,
                MachineLockEnergyPanel.SUB_STATION_PERMUTATION_COUNT));
        }

        private static MachineLockPatternSelector CreatePersistentSelector(string fileName, int count)
        {
            return new MachineLockPatternSelector(Path.Combine(Application.persistentDataPath, fileName), count);
        }

        private MachineLockOutcomeInput BuildOutcomeInput()
        {
            return new MachineLockOutcomeInput
            {
                PressRunning = !TaskEStop && !TaskMainOff,
                Contained = TaskContainment,
                AllTasksComplete = TaskEStop && TaskAlarm && TaskMainOff && TaskLockout
                                  && TaskTesterZero && TaskTryStart && TaskPpeFull && TaskContainment && TaskExit,
                AnyCritical = spi != null && spi.CriticalOccurred,
                TimedOut = spi != null && spi.TimedOut
            };
        }

        private OutcomeType EvaluateTimeoutOutcome()
        {
            return (TaskEStop || TaskMainOff) && TaskContainment ? OutcomeType.Partial : OutcomeType.Negative;
        }

        private void Apply(SpiEvent evt)
        {
            if (spi == null) return;
            spi.SetElapsed(ElapsedSeconds);
            spi.ApplyEvent(evt);
        }

        private void ResetTaskFlags()
        {
            TaskEStop = false;
            TaskAlarm = false;
            TaskMainOff = false;
            TaskLockout = false;
            TaskTesterZero = false;
            TaskTryStart = false;
            TaskPpeFull = false;
            TaskContainment = false;
            TaskExit = false;
        }

        private MachineLockConfig ResolveConfig()
        {
            if (config != null) return config;
            Debug.LogWarning("[SI81 MachineLock] MachineLockScenario has no SO_MachineLockConfig: using the GDD defaults. " +
                             "Assign the config on MNG_Scenario.", this);
            config = ScriptableObject.CreateInstance<MachineLockConfig>();
            return config;
        }

        private void PlayLines(string speaker, params string[] lines)
        {
            // Subtitle timing only exists in Play Mode: EditMode tests drive the notify API directly.
            if (!Application.isPlaying || lines == null || lines.Length == 0) return;
            if (playingLines != null) StopCoroutine(playingLines);
            playingLines = StartCoroutine(PlayLinesRoutine(speaker, lines));
        }

        private IEnumerator PlayLinesRoutine(string speaker, string[] lines)
        {
            foreach (string line in lines)
            {
                if (ended) yield break;
                Subtitle(speaker, line, LINE_SECONDS);
                yield return new WaitForSeconds(LINE_SECONDS);
            }
            playingLines = null;
        }
    }
}
