using System;
using System.Collections.Generic;
using SI81.UI;

namespace SI81.MachineLock
{
    /// <summary>
    /// Critical events of the Machine Lock scenario. Each one applies a fixed Safety and Decision penalty
    /// and produces one critical debrief line.
    /// </summary>
    public enum CriticalEvent
    {
        /// <summary>Contact with hot oil while the PPE set is incomplete.</summary>
        HotOilBurn,
        /// <summary>Contact with energised oil. PPE does not protect.</summary>
        LiveOilShock,
        /// <summary>Contact with the press while the ram is moving.</summary>
        PressContact,
        /// <summary>Contact with hot oil while wearing the wrong PPE.</summary>
        WrongPpeContact,
        /// <summary>Water extinguisher used on burning oil or on the electrical panel.</summary>
        WaterExtinguisher,
        /// <summary>Try-start performed before the machine was isolated.</summary>
        TryStartUnisolated
    }

    /// <summary>
    /// Scoring events of the Machine Lock scenario. Applying an event updates the SPI and may add a
    /// debrief line. Every event is idempotent unless the GDD states otherwise.
    /// </summary>
    public enum SpiEvent
    {
        /// <summary>Emergency stop pressed: Execution reward and inaction timer cancelled.</summary>
        EStop,
        /// <summary>Department alarm raised: Execution reward.</summary>
        Alarm,
        /// <summary>Main switch opened.</summary>
        MainOff,
        /// <summary>Lockout applied after the main switch was opened.</summary>
        Lockout,
        /// <summary>Voltage tester read zero residual energy.</summary>
        TesterZero,
        /// <summary>Try-start verified that the machine cannot restart.</summary>
        TryStartVerified,
        /// <summary>All four correct PPE items worn.</summary>
        PpeFull,
        /// <summary>Four cones and four absorbents placed (containment complete).</summary>
        ContainmentComplete,
        /// <summary>Player left the room and reported to the supervisor.</summary>
        Exit,
        /// <summary>Player approached the puddle danger zone before the E-stop.</summary>
        ApproachBeforeEStop,
        /// <summary>Player approached the live puddle danger zone before main power was off.</summary>
        ApproachLiveBeforeMainOff,
        /// <summary>Handover without the department alarm.</summary>
        MissingAlarm,
        /// <summary>Handover without the main switch open.</summary>
        MissingMainOff,
        /// <summary>Handover without the lockout applied.</summary>
        MissingLockout,
        /// <summary>Handover without all four cones.</summary>
        MissingCones,
        /// <summary>Handover without all four absorbents.</summary>
        MissingAbsorbents,
        /// <summary>Level time limit reached.</summary>
        Timeout,
        /// <summary>Bad Ending 2: the press kept running for the inaction limit.</summary>
        InactionBadEnding
    }

    /// <summary>
    /// Time modes defined by the scenario brief. The project levels map Guided to Guided,
    /// Operational to Realistic and Crisis to Stress.
    /// </summary>
    public enum SpiTimeMode
    {
        /// <summary>Guided: 900 s, soft limit, the mission continues.</summary>
        Guided,
        /// <summary>Realistic (project level Operational): 600 s, hard limit.</summary>
        Realistic,
        /// <summary>Stress (project level Crisis): 480 s, hard limit.</summary>
        Stress
    }

    /// <summary>
    /// Identifier of a Machine Lock debrief line. The SPI owns selection and capping;
    /// the Italian text lives in <c>MachineLockTexts</c>.
    /// </summary>
    public enum DebriefLineId
    {
        /// <summary>Press stopped and alarm raised.</summary>
        GoodEStopAndAlarm,
        /// <summary>Main power isolated and locked out.</summary>
        GoodIsolation,
        /// <summary>Zero residual energy verified with tester and try-start.</summary>
        GoodZeroEnergy,
        /// <summary>All four correct PPE items worn.</summary>
        GoodPpe,
        /// <summary>Spill delimited and contained.</summary>
        GoodContainment,
        /// <summary>Hot oil burn.</summary>
        CriticalHotOil,
        /// <summary>Live oil shock.</summary>
        CriticalShock,
        /// <summary>Water extinguisher misuse.</summary>
        CriticalWater,
        /// <summary>Try-start before isolation.</summary>
        CriticalTryStart,
        /// <summary>Press contact while running.</summary>
        CriticalPressContact,
        /// <summary>Contact with hot oil while wearing wrong PPE.</summary>
        CriticalWrongPpe,
        /// <summary>Bad Ending 2: the press kept working.</summary>
        CriticalInaction,
        /// <summary>Exit without the alarm.</summary>
        BadMissingAlarm,
        /// <summary>Exit without main off or lockout.</summary>
        BadMissingIsolation,
        /// <summary>Exit without delimitation or containment.</summary>
        BadMissingContainment,
        /// <summary>Level time limit exceeded.</summary>
        BadTimeout
    }

    /// <summary>
    /// Tunable values of <see cref="MachineLockSpi"/>. Defaults mirror the GDD §14.4 config defaults;
    /// <see cref="MachineLockConfig"/> builds an instance for the current level.
    /// </summary>
    public sealed class MachineLockSpiOptions
    {
        /// <summary>Time limit of the running level, in seconds.</summary>
        public float TimeLimitSeconds = 900f;
        /// <summary>true when reaching the time limit ends the mission (Realistic and Stress).</summary>
        public bool HardEndAtLimit = false;
        /// <summary>Number of observation targets used to normalize Awareness.</summary>
        public int AwarenessTargets = 6;
        /// <summary>Maximum Awareness points.</summary>
        public int AwarenessMax = 20;
        /// <summary>Maximum Time points.</summary>
        public int TimeMax = 5;
        /// <summary>Seconds during which a second critical event is ignored.</summary>
        public float CriticalCooldownSeconds = 5f;
        /// <summary>Safety penalty of a critical contact.</summary>
        public int CriticalSafetyPenalty = 10;
        /// <summary>Safety penalty of the water extinguisher.</summary>
        public int WaterSafetyPenalty = 5;
        /// <summary>Safety penalty of a try-start before isolation.</summary>
        public int TryStartSafetyPenalty = 5;
        /// <summary>Decision penalty of a critical event.</summary>
        public int CriticalDecisionPenalty = 10;
        /// <summary>Decision penalty for a handover without the alarm.</summary>
        public int MissingAlarmPenalty = 8;
        /// <summary>Decision penalty for a handover without the main switch open.</summary>
        public int MissingMainPenalty = 2;
        /// <summary>Decision penalty for a handover without the lockout.</summary>
        public int MissingLockoutPenalty = 2;
        /// <summary>Decision penalty for a handover without all four cones.</summary>
        public int MissingConesPenalty = 4;
        /// <summary>Decision penalty for a handover without all four absorbents.</summary>
        public int MissingContainmentPenalty = 5;
        /// <summary>Decision penalty when the level time limit is reached.</summary>
        public int TimeoutPenalty = 5;
        /// <summary>Execution reward for the E-stop.</summary>
        public int ExecutionEStop = 2;
        /// <summary>Execution reward for the alarm.</summary>
        public int ExecutionAlarm = 2;
        /// <summary>Execution reward for main off plus lockout.</summary>
        public int ExecutionIsolation = 3;
        /// <summary>Execution reward for tester zero plus try-start verified.</summary>
        public int ExecutionZeroEnergy = 2;
        /// <summary>Execution reward for the full PPE set.</summary>
        public int ExecutionPpe = 3;
        /// <summary>Execution reward for containment complete.</summary>
        public int ExecutionContainment = 2;
        /// <summary>Execution reward for the exit and handover.</summary>
        public int ExecutionExit = 1;
    }

    /// <summary>
    /// Snapshot of the run state used by <see cref="MachineLockSpi.EvaluateOutcome"/>.
    /// </summary>
    public struct MachineLockOutcomeInput
    {
        /// <summary>true while the press is still moving.</summary>
        public bool PressRunning;
        /// <summary>true when delimitation and containment are complete.</summary>
        public bool Contained;
        /// <summary>true when every task flag is set, including the exit.</summary>
        public bool AllTasksComplete;
        /// <summary>true when at least one critical event happened.</summary>
        public bool AnyCritical;
        /// <summary>true when the level time limit was reached.</summary>
        public bool TimedOut;
    }

    /// <summary>
    /// Authoritative Safety Performance Index of the Machine Lock scenario (GDD §11).
    /// Pure C#: no MonoBehaviour, no scene and no UnityEngine dependency, so it can be unit-tested.
    /// The SPI owns the five dimensions, their weights, the time modes, the penalties, the outcome
    /// evaluation and the capped debrief selection. Player-facing debrief text is resolved by the
    /// caller through <see cref="BuildDebriefLines"/>.
    /// </summary>
    public sealed class MachineLockSpi
    {
        /// <summary>Maximum Safety points.</summary>
        public const int SAFETY_MAX = 20;
        /// <summary>Maximum Awareness points.</summary>
        public const int AWARENESS_MAX = 20;
        /// <summary>Maximum Decision points.</summary>
        public const int DECISION_MAX = 40;
        /// <summary>Maximum Execution points.</summary>
        public const int EXECUTION_MAX = 15;
        /// <summary>Maximum Time points.</summary>
        public const int TIME_MAX = 5;
        /// <summary>Maximum SPI value.</summary>
        public const int SPI_MAX = 100;
        /// <summary>Maximum number of positive debrief rows.</summary>
        public const int MAX_GOOD_LINES = 5;
        /// <summary>Maximum number of error and critical debrief rows.</summary>
        public const int MAX_ERROR_LINES = 4;

        private sealed class DebriefEntry
        {
            public DebriefLineId Id;
            public bool Good;
            public bool Critical;
        }

        private readonly MachineLockSpiOptions options;
        private readonly List<DebriefEntry> debrief = new List<DebriefEntry>();
        private readonly HashSet<DebriefLineId> addedLines = new HashSet<DebriefLineId>();
        private readonly HashSet<string> observedIds = new HashSet<string>();

        private bool taskEStop;
        private bool taskAlarm;
        private bool taskMainOff;
        private bool taskLockout;
        private bool taskTesterZero;
        private bool taskTryStart;
        private bool taskPpeFull;
        private bool taskContainment;
        private bool taskExit;
        private bool isolationRewarded;
        private bool zeroEnergyRewarded;
        private bool approachBeforeEStopApplied;
        private bool approachLiveApplied;
        private bool timeoutApplied;

        private float lastCriticalSeconds = float.NegativeInfinity;

        /// <summary>
        /// Creates an SPI with the given options, or with GDD defaults when options is null.
        /// </summary>
        /// <param name="options">Tunable values; null uses a fresh default set.</param>
        public MachineLockSpi(MachineLockSpiOptions options = null)
        {
            this.options = options != null ? options : new MachineLockSpiOptions();
            Safety = SAFETY_MAX;
            Decision = DECISION_MAX;
        }

        /// <summary>Options in use.</summary>
        public MachineLockSpiOptions Options => options;
        /// <summary>Safety dimension, 0 to 20.</summary>
        public float Safety { get; private set; }
        /// <summary>Decision dimension, 0 to 40.</summary>
        public float Decision { get; private set; }
        /// <summary>Execution dimension, 0 to 15.</summary>
        public int Execution { get; private set; }
        /// <summary>Elapsed game time in seconds.</summary>
        public float Elapsed { get; private set; }
        /// <summary>Number of distinct observation targets already scored.</summary>
        public int ObservedTargets => observedIds.Count;
        /// <summary>true when at least one critical event happened.</summary>
        public bool CriticalOccurred { get; private set; }
        /// <summary>true when the hard time limit of the level was reached.</summary>
        public bool TimedOut { get; private set; }

        /// <summary>Awareness dimension, 0 to 20. Each observation target is worth 20 / targets.</summary>
        public float Awareness => options.AwarenessTargets <= 0
            ? 0f
            : options.AwarenessMax * Math.Min(observedIds.Count, options.AwarenessTargets) / (float)options.AwarenessTargets;

        /// <summary>Time dimension, 0 to 5. Proportional to the unused part of the level budget.</summary>
        public float TimePoints
        {
            get
            {
                if (options.TimeLimitSeconds <= 0f) return options.TimeMax;
                float remaining = (options.TimeLimitSeconds - Elapsed) / options.TimeLimitSeconds;
                return options.TimeMax * Clamp01(remaining);
            }
        }

        /// <summary>Weighted SPI, rounded and clamped to 0..100.</summary>
        public int Spi => Clamp(RoundToInt(Safety + Awareness + Decision + Execution + TimePoints), 0, SPI_MAX);

        /// <summary>Task flag: E-stop pressed.</summary>
        public bool TaskEStop => taskEStop;
        /// <summary>Task flag: alarm raised.</summary>
        public bool TaskAlarm => taskAlarm;
        /// <summary>Task flag: main switch open.</summary>
        public bool TaskMainOff => taskMainOff;
        /// <summary>Task flag: lockout applied.</summary>
        public bool TaskLockout => taskLockout;
        /// <summary>Task flag: tester read zero.</summary>
        public bool TaskTesterZero => taskTesterZero;
        /// <summary>Task flag: try-start verified.</summary>
        public bool TaskTryStart => taskTryStart;
        /// <summary>Task flag: full correct PPE worn.</summary>
        public bool TaskPpeFull => taskPpeFull;
        /// <summary>Task flag: containment complete.</summary>
        public bool TaskContainment => taskContainment;
        /// <summary>Task flag: exit reached and handover done.</summary>
        public bool TaskExit => taskExit;

        /// <summary>
        /// Maps a project difficulty level to the SPI time mode.
        /// </summary>
        /// <param name="level">Project level.</param>
        /// <returns>Guided, Realistic (Operational) or Stress (Crisis).</returns>
        public static SpiTimeMode TimeModeFromLevel(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Operational:
                    return SpiTimeMode.Realistic;
                case DifficultyLevel.Crisis:
                    return SpiTimeMode.Stress;
                default:
                    return SpiTimeMode.Guided;
            }
        }

        /// <summary>
        /// Time limit of a time mode, in seconds.
        /// </summary>
        /// <param name="mode">Time mode.</param>
        /// <returns>900, 600 or 480 seconds.</returns>
        public static float DefaultTimeLimitFor(SpiTimeMode mode)
        {
            switch (mode)
            {
                case SpiTimeMode.Realistic:
                    return 600f;
                case SpiTimeMode.Stress:
                    return 480f;
                default:
                    return 900f;
            }
        }

        /// <summary>
        /// true when reaching the time limit ends the mission for this mode.
        /// </summary>
        /// <param name="mode">Time mode.</param>
        /// <returns>false for Guided, true for Realistic and Stress.</returns>
        public static bool DefaultHardEndFor(SpiTimeMode mode)
        {
            return mode != SpiTimeMode.Guided;
        }

        /// <summary>
        /// Sets the elapsed game time and updates the hard-time state.
        /// </summary>
        /// <param name="seconds">Elapsed seconds since the scenario started.</param>
        public void SetElapsed(float seconds)
        {
            Elapsed = Math.Max(0f, seconds);
            TimedOut = options.HardEndAtLimit && options.TimeLimitSeconds > 0f && Elapsed >= options.TimeLimitSeconds;
        }

        /// <summary>
        /// Applies a scoring event. Safe to call twice: rewards and debrief lines are added once per run.
        /// </summary>
        /// <param name="evt">Event to apply.</param>
        public void ApplyEvent(SpiEvent evt)
        {
            switch (evt)
            {
                case SpiEvent.EStop:
                    if (taskEStop) return;
                    taskEStop = true;
                    AddExecution(options.ExecutionEStop);
                    TryAddGoodEStopAndAlarm();
                    break;

                case SpiEvent.Alarm:
                    if (taskAlarm) return;
                    taskAlarm = true;
                    AddExecution(options.ExecutionAlarm);
                    TryAddGoodEStopAndAlarm();
                    break;

                case SpiEvent.MainOff:
                    if (taskMainOff) return;
                    taskMainOff = true;
                    TryRewardIsolation();
                    break;

                case SpiEvent.Lockout:
                    if (taskLockout) return;
                    taskLockout = true;
                    TryRewardIsolation();
                    break;

                case SpiEvent.TesterZero:
                    if (taskTesterZero) return;
                    taskTesterZero = true;
                    TryRewardZeroEnergy();
                    break;

                case SpiEvent.TryStartVerified:
                    if (taskTryStart) return;
                    taskTryStart = true;
                    TryRewardZeroEnergy();
                    break;

                case SpiEvent.PpeFull:
                    if (taskPpeFull) return;
                    taskPpeFull = true;
                    AddExecution(options.ExecutionPpe);
                    AddLine(DebriefLineId.GoodPpe, true, false);
                    break;

                case SpiEvent.ContainmentComplete:
                    if (taskContainment) return;
                    taskContainment = true;
                    AddExecution(options.ExecutionContainment);
                    AddLine(DebriefLineId.GoodContainment, true, false);
                    break;

                case SpiEvent.Exit:
                    if (taskExit) return;
                    taskExit = true;
                    AddExecution(options.ExecutionExit);
                    break;

                case SpiEvent.ApproachBeforeEStop:
                    if (approachBeforeEStopApplied) return;
                    approachBeforeEStopApplied = true;
                    AddDecision(-options.CriticalDecisionPenalty);
                    break;

                case SpiEvent.ApproachLiveBeforeMainOff:
                    if (approachLiveApplied) return;
                    approachLiveApplied = true;
                    AddDecision(-options.CriticalDecisionPenalty);
                    break;

                case SpiEvent.MissingAlarm:
                    AddDecision(-options.MissingAlarmPenalty);
                    AddLine(DebriefLineId.BadMissingAlarm, false, false);
                    break;

                case SpiEvent.MissingMainOff:
                    AddDecision(-options.MissingMainPenalty);
                    AddLine(DebriefLineId.BadMissingIsolation, false, false);
                    break;

                case SpiEvent.MissingLockout:
                    AddDecision(-options.MissingLockoutPenalty);
                    AddLine(DebriefLineId.BadMissingIsolation, false, false);
                    break;

                case SpiEvent.MissingCones:
                    AddDecision(-options.MissingConesPenalty);
                    AddLine(DebriefLineId.BadMissingContainment, false, false);
                    break;

                case SpiEvent.MissingAbsorbents:
                    AddDecision(-options.MissingContainmentPenalty);
                    AddLine(DebriefLineId.BadMissingContainment, false, false);
                    break;

                case SpiEvent.Timeout:
                    TimedOut = true;
                    if (timeoutApplied) break;
                    timeoutApplied = true;
                    AddDecision(-options.TimeoutPenalty);
                    AddLine(DebriefLineId.BadTimeout, false, false);
                    break;

                case SpiEvent.InactionBadEnding:
                    AddDecision(-options.CriticalDecisionPenalty);
                    AddLine(DebriefLineId.CriticalInaction, false, true);
                    CriticalOccurred = true;
                    break;
            }
        }

        /// <summary>
        /// Applies a critical event, honouring the critical cooldown. Returns false when the event is
        /// ignored because another critical event happened inside the cooldown window.
        /// </summary>
        /// <param name="evt">Critical event.</param>
        /// <param name="nowSeconds">Current elapsed game time in seconds.</param>
        /// <returns>true when the penalties were applied.</returns>
        public bool ApplyCritical(CriticalEvent evt, float nowSeconds)
        {
            if (!float.IsNegativeInfinity(lastCriticalSeconds)
                && nowSeconds - lastCriticalSeconds < options.CriticalCooldownSeconds)
                return false;
            lastCriticalSeconds = nowSeconds;

            switch (evt)
            {
                case CriticalEvent.HotOilBurn:
                    ApplyCriticalPenalties(options.CriticalSafetyPenalty);
                    AddLine(DebriefLineId.CriticalHotOil, false, true);
                    break;
                case CriticalEvent.LiveOilShock:
                    ApplyCriticalPenalties(options.CriticalSafetyPenalty);
                    AddLine(DebriefLineId.CriticalShock, false, true);
                    break;
                case CriticalEvent.PressContact:
                    ApplyCriticalPenalties(options.CriticalSafetyPenalty);
                    AddLine(DebriefLineId.CriticalPressContact, false, true);
                    break;
                case CriticalEvent.WrongPpeContact:
                    ApplyCriticalPenalties(options.CriticalSafetyPenalty);
                    AddLine(DebriefLineId.CriticalWrongPpe, false, true);
                    break;
                case CriticalEvent.WaterExtinguisher:
                    ApplyCriticalPenalties(options.WaterSafetyPenalty);
                    AddLine(DebriefLineId.CriticalWater, false, true);
                    break;
                case CriticalEvent.TryStartUnisolated:
                    ApplyCriticalPenalties(options.TryStartSafetyPenalty);
                    AddLine(DebriefLineId.CriticalTryStart, false, true);
                    break;
            }

            CriticalOccurred = true;
            return true;
        }

        /// <summary>
        /// Records an observation target. Each distinct id scores once.
        /// </summary>
        /// <param name="targetId">Stable target identifier (for example "press").</param>
        /// <returns>true when the target was new and scored.</returns>
        public bool Observe(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return false;
            return observedIds.Add(targetId);
        }

        /// <summary>
        /// Evaluates the mission outcome from the GDD §11.7 matrix.
        /// </summary>
        /// <param name="input">Current run state snapshot.</param>
        /// <returns>Positive, Partial or Negative.</returns>
        public OutcomeType EvaluateOutcome(MachineLockOutcomeInput input)
        {
            if (input.PressRunning) return OutcomeType.Negative;
            if (input.TimedOut && !input.Contained) return OutcomeType.Negative;
            if (input.AllTasksComplete && !input.AnyCritical && !input.TimedOut) return OutcomeType.Positive;
            return OutcomeType.Partial;
        }

        /// <summary>
        /// Normalized dimension values for the common Score screen, each 0 to 100:
        /// Safety/20, Awareness/20, Decision/40, Execution/15, Time/5, multiplied by 100 and rounded.
        /// </summary>
        /// <returns>Five integers in Safety, Awareness, Decision, Execution, Time order.</returns>
        public int[] Dimensions100()
        {
            return new[]
            {
                Normalize(Safety, SAFETY_MAX),
                Normalize(Awareness, options.AwarenessMax),
                Normalize(Decision, DECISION_MAX),
                Normalize(Execution, EXECUTION_MAX),
                Normalize(TimePoints, options.TimeMax)
            };
        }

        /// <summary>
        /// Builds the capped debrief list: at most five positive rows and at most four error rows,
        /// critical rows first. The score already includes every applied penalty; only the visible
        /// rows are trimmed.
        /// </summary>
        /// <param name="resolve">Maps an id to its Italian text, without prefix.</param>
        /// <returns>Lines with the shared prefix ("+ ", "- ", "! ").</returns>
        public List<string> BuildDebriefLines(Func<DebriefLineId, string> resolve)
        {
            var lines = new List<string>();
            var good = new List<DebriefEntry>();
            var critical = new List<DebriefEntry>();
            var bad = new List<DebriefEntry>();

            foreach (DebriefEntry entry in debrief)
            {
                if (entry.Critical) critical.Add(entry);
                else if (entry.Good) good.Add(entry);
                else bad.Add(entry);
            }

            if (good.Count > MAX_GOOD_LINES) good.RemoveRange(MAX_GOOD_LINES, good.Count - MAX_GOOD_LINES);
            if (critical.Count > MAX_ERROR_LINES)
                critical.RemoveRange(0, critical.Count - MAX_ERROR_LINES);
            int badBudget = MAX_ERROR_LINES - critical.Count;
            if (bad.Count > badBudget) bad.RemoveRange(badBudget, bad.Count - badBudget);

            Append(lines, good, DebriefLinePrefix.GOOD, resolve);
            Append(lines, critical, DebriefLinePrefix.CRITICAL, resolve);
            Append(lines, bad, DebriefLinePrefix.BAD, resolve);
            return lines;
        }

        private static void Append(List<string> target, List<DebriefEntry> source, string prefix, Func<DebriefLineId, string> resolve)
        {
            if (resolve == null) return;
            foreach (DebriefEntry entry in source)
            {
                string text = resolve(entry.Id);
                if (string.IsNullOrEmpty(text)) continue;
                target.Add(prefix + text);
            }
        }

        private void TryAddGoodEStopAndAlarm()
        {
            if (taskEStop && taskAlarm) AddLine(DebriefLineId.GoodEStopAndAlarm, true, false);
        }

        private void TryRewardIsolation()
        {
            if (!taskMainOff || !taskLockout || isolationRewarded) return;
            isolationRewarded = true;
            AddExecution(options.ExecutionIsolation);
            AddLine(DebriefLineId.GoodIsolation, true, false);
        }

        private void TryRewardZeroEnergy()
        {
            if (!taskTesterZero || !taskTryStart || zeroEnergyRewarded) return;
            zeroEnergyRewarded = true;
            AddExecution(options.ExecutionZeroEnergy);
            AddLine(DebriefLineId.GoodZeroEnergy, true, false);
        }

        private void ApplyCriticalPenalties(int safetyPenalty)
        {
            Safety = Math.Max(0f, Safety - safetyPenalty);
            Decision = Math.Max(0f, Decision - options.CriticalDecisionPenalty);
        }

        private void AddDecision(int delta)
        {
            Decision = Clamp(Decision + delta, 0f, DECISION_MAX);
        }

        private void AddExecution(int delta)
        {
            Execution = Clamp(Execution + delta, 0, EXECUTION_MAX);
        }

        private void AddLine(DebriefLineId id, bool good, bool critical)
        {
            if (!addedLines.Add(id)) return;
            debrief.Add(new DebriefEntry { Id = id, Good = good, Critical = critical });
        }

        private static int Normalize(float value, float max)
        {
            if (max <= 0f) return 0;
            return RoundToInt(Clamp01(value / max) * 100f);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private static int RoundToInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
