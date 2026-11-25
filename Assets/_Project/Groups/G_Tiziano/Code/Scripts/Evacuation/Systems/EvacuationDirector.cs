using System;
using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Data;
using EvacOutcome = SI81.Tiziano.ChemicalAlert.Core.Outcome;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Single entry point of the Evacuation scenario towards the rest of the game: phases, timer/stopwatch, hearts,
    /// seed, reset registry, action log and result. Other systems talk to it only through the Report... methods;
    /// the outside world only reads its state and listens to its events.
    /// </summary>
    public sealed class EvacuationDirector : MonoBehaviour, IResettable
    {
        /// <summary>Damage source key emitted with DamageTaken for lethal fire contacts.</summary>
        public const string DamageSourceFire = "evac.damage.fire";

        [Header("Configuration")]
        [SerializeField] private EvacDifficulty difficulty = EvacDifficulty.Operational;
        [SerializeField] private EvacuationConfig[] configurations = new EvacuationConfig[0];

        [Header("Rig and markers")]
        [Tooltip("Root of the player rig (OVRCameraRig). Teleported to the spawn marker on every reset.")]
        [SerializeField] private Transform rigRoot;
        [Tooltip("SPAWN_Player marker.")]
        [SerializeField] private Transform spawnMarker;

        [Header("Flow")]
        [Tooltip("Start the scenario automatically after Play (honoured only in the Editor).")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField, Min(0f)] private float autoStartDelaySeconds = 2f;
        [Tooltip("Seconds spent in the Resolution phase before the result is published (0 = same frame).")]
        [SerializeField, Min(0f)] private float resolutionSeconds;

        [Header("Seed")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed = 81081;

        [Header("Debug")]
        [Tooltip("Editor/debug only. If > 0, overrides the config time limit for the current run (used by smoke tests).")]
        [SerializeField] private float debugTimeLimitSeconds = -1f;

        private readonly EvacuationOutcomeEvaluator evaluator = new EvacuationOutcomeEvaluator();
        private readonly ActionLog actionLog = new ActionLog();
        private readonly List<IResettable> resettables = new List<IResettable>();

        private EvacuationConfig config;
        private EvacPhase phase = EvacPhase.Briefing;
        private EvacDangerLevel danger = EvacDangerLevel.Mid;
        private float elapsed;
        private int hearts;
        private bool alive = true;
        private bool timedOut;
        private bool timeUpRaised;
        private bool planInspected;
        private bool alarmActive;
        private float planInspectedAt = -1f;
        private float room1CompletedAt = -1f;
        private float room2CompletedAt = -1f;
        private int npcRoom1;
        private int npcRoom2;
        private int fireContacts;
        private bool elevatorUsed;
        private EvacExit exitReached = EvacExit.None;
        private bool anyActionDone;
        private bool roomExitRecorded;
        private bool impulsiveExit;
        private string endReason = string.Empty;
        private EvacOutcome? outcome;
        private ScenarioResult lastResult;
        private bool autoStartPending;
        private float autoStartRemaining;
        private bool resolutionPending;
        private float resolutionRemaining;
        private System.Random rng;
        private int seed;
        private bool hasDebugTimeLimitOverride;
        private float debugTimeLimitOverrideSeconds;
        private float idleTimer;
        private bool idleHintIssued;
        private int hintIndex;
        private int hintRequests;
        private bool reducedEffects;
        private int seedDraws;
        private string replayNpcSlots = string.Empty;
        private string replayFireSlots = string.Empty;
        private bool replayStartLogged;

        /// <summary>Raised whenever the phase changes.</summary>
        public event Action<EvacPhase> PhaseChanged;
        /// <summary>Raised the first time an evacuation plan is inspected, with the plan id (may be empty).</summary>
        public event Action<string> PlanInspected;
        /// <summary>Raised when the evacuation alarm turns on (true) or off (false).</summary>
        public event Action<bool> AlarmStateChanged;
        /// <summary>Raised with a stable dialogue key when an NPC should speak.</summary>
        public event Action<string> NpcDialogueRequested;
        /// <summary>Raised when an NPC joins the line: (room 1 or 2, total NPCs collected).</summary>
        public event Action<int, int> NpcCollected;
        /// <summary>Raised after an exit is reported, with the reached exit kind.</summary>
        public event Action<EvacExit> ExitReported;
        /// <summary>Raised on every fire contact: (contact number starting at 1, intensity 0-1).</summary>
        public event Action<int, float> FireContact;
        /// <summary>Raised when hearts are lost: (damage source key, hearts remaining).</summary>
        public event Action<string, int> DamageTaken;
        /// <summary>Raised when the hearts value changes (and on reset).</summary>
        public event Action<int> HeartsChanged;
        /// <summary>Raised when the player dies.</summary>
        public event Action Died;
        /// <summary>Raised with a stable hint key (aids, Guided only).</summary>
        public event Action<string> HintRequested;
        /// <summary>Raised when the danger level changes.</summary>
        public event Action<EvacDangerLevel> DangerLevelChanged;
        /// <summary>Raised when the countdown reaches zero (never in Guided).</summary>
        public event Action TimeUp;
        /// <summary>Raised once per run with the final result.</summary>
        public event Action<ScenarioResult> ResultReady;
        /// <summary>Raised for every action log entry as it is recorded.</summary>
        public event Action<ActionLogEntry> ActionRecorded;
        /// <summary>Raised at the end of ResetScenario.</summary>
        public event Action ScenarioReset;
        /// <summary>Raised when the seed changes.</summary>
        public event Action<int> SeedChanged;
        /// <summary>Raised at run start with the seed and the chosen layout: (seed, NPC slot ids, active fire slot ids).</summary>
        public event Action<int, string, string> ReplayStarted;

        /// <summary>Gets the current phase.</summary>
        public EvacPhase Phase => phase;
        /// <summary>Gets the selected difficulty.</summary>
        public EvacDifficulty Difficulty => difficulty;
        /// <summary>Gets the active configuration (null if none is assigned for the difficulty).</summary>
        public EvacuationConfig Config => config;
        /// <summary>Gets the configurations assigned to the director.</summary>
        public IReadOnlyList<EvacuationConfig> Configurations => configurations;
        /// <summary>Gets whether the active level has a time limit (countdown) instead of a stopwatch only.</summary>
        public bool HasTimeLimit => hasDebugTimeLimitOverride ? debugTimeLimitOverrideSeconds > 0f : config != null && config.HasTimeLimit;
        /// <summary>Gets the effective time limit seconds (debug override wins).</summary>
        public float EffectiveTimeLimitSeconds => hasDebugTimeLimitOverride ? Mathf.Max(0f, debugTimeLimitOverrideSeconds) : (config != null && config.HasTimeLimit ? config.TotalDurationSeconds : 0f);
        /// <summary>Gets the seconds elapsed since the Action phase started.</summary>
        public float Elapsed => elapsed;
        /// <summary>Gets the countdown seconds left (0 when there is no time limit).</summary>
        public float TimeRemaining => HasTimeLimit ? Mathf.Max(0f, EffectiveTimeLimitSeconds - elapsed) : 0f;
        /// <summary>Gets the hearts left.</summary>
        public int Hearts => hearts;
        /// <summary>Gets whether the player is alive.</summary>
        public bool IsAlive => alive;
        /// <summary>Gets the final outcome once the result is ready.</summary>
        public EvacOutcome? Outcome => outcome;
        /// <summary>Gets the last published result.</summary>
        public ScenarioResult LastResult => lastResult;
        /// <summary>Gets the runtime action log.</summary>
        public ActionLog ActionLog => actionLog;
        /// <summary>Gets the seed of the current run.</summary>
        public int Seed => seed;
        /// <summary>Gets the single seeded random generator every random choice must use.</summary>
        public System.Random Rng => rng;
        /// <summary>Gets the current danger level.</summary>
        public EvacDangerLevel DangerLevel => danger;
        /// <summary>Gets whether guided highlights are active (Guided level only).</summary>
        public bool HighlightsEnabled => difficulty == EvacDifficulty.Guided && config != null && config.HighlightEnabled;
        /// <summary>Gets whether ordered hints are available (Guided level only).</summary>
        public bool HintsEnabled => difficulty == EvacDifficulty.Guided && config != null && config.HintsEnabled && config.OrderedHints != null && config.OrderedHints.Count > 0;
        /// <summary>Gets the stable key of the next hint for the current progress step (empty if unavailable).</summary>
        public string NextHintKey => HintsEnabled ? HintKeyAt(hintIndex) : string.Empty;
        /// <summary>Gets how many hints were requested in the current run.</summary>
        public int HintRequests => hintRequests;
        /// <summary>Gets whether visual effects are reduced (Crisis preparation for E09).</summary>
        public bool ReducedEffects => reducedEffects;
        /// <summary>Gets whether an evacuation plan was inspected.</summary>
        public bool PlanWasInspected => planInspected;
        /// <summary>True while the alarm system reports the alarm as active.</summary>
        public bool IsAlarmActive => alarmActive;
        /// <summary>Gets the NPCs collected in room 1.</summary>
        public int NpcCollectedRoom1 => npcRoom1;
        /// <summary>Gets the NPCs collected in room 2.</summary>
        public int NpcCollectedRoom2 => npcRoom2;
        /// <summary>Gets the fire contacts so far (separate from hearts).</summary>
        public int FireContacts => fireContacts;
        /// <summary>Gets whether the elevator was used.</summary>
        public bool ElevatorWasUsed => elevatorUsed;
        /// <summary>Gets the exit reached when the scenario ended.</summary>
        public EvacExit ExitReached => exitReached;
        /// <summary>Gets why the scenario ended (exit, timeout, elevator, death, debug).</summary>
        public string EndReason => endReason;
        /// <summary>Gets the number of registered resettable systems.</summary>
        public int ResettableCount => resettables.Count;
        /// <summary>Gets the rig root moved on reset (falls back to the shared rig at runtime).</summary>
        public Transform RigRoot => rigRoot != null ? rigRoot : SharedRigRoot;

        /// <summary>Assigns the shared rig root from the rig/UI integration layer.</summary>
        public void SetRigRoot(Transform value)
        {
            rigRoot = value;
        }

        private Transform SharedRigRoot
        {
            get
            {
                SI81.Rig.RigRegistration registration = SI81.Rig.RigRegistration.Instance;
                return registration != null ? registration.transform : null;
            }
        }
        /// <summary>Gets the spawn marker.</summary>
        public Transform SpawnMarker => spawnMarker;
        /// <summary>Gets whether the seed is fixed.</summary>
        public bool IsSeedFixed => useFixedSeed;
        /// <summary>Gets the NPC slot ids chosen for the current run (comma separated, empty until reported).</summary>
        public string ReplayNpcSlots => replayNpcSlots;
        /// <summary>Gets the active fire slot ids chosen for the current run (comma separated, empty until reported).</summary>
        public string ReplayFireSlots => replayFireSlots;
        /// <summary>Gets whether the ReplayStart entry was recorded for the current run.</summary>
        public bool ReplayStartLogged => replayStartLogged;

        /// <summary>Receives the NPC slot layout chosen for this run (called by NpcRegistry).</summary>
        public void ReportNpcLayout(string slotIds)
        {
            replayNpcSlots = slotIds ?? string.Empty;
        }

        /// <summary>Receives the active fire slot layout chosen for this run (called by EvacFireManager).</summary>
        public void ReportFireLayout(string slotIds)
        {
            replayFireSlots = slotIds ?? string.Empty;
        }

        private void Awake()
        {
            ResolveReferences();
            ResetScenario();
        }

        private void Start()
        {
            if (autoStartOnPlay && Application.isEditor && phase == EvacPhase.Briefing)
            {
                autoStartPending = true;
                autoStartRemaining = autoStartDelaySeconds;
            }
        }

        private void Update()
        {
            if (autoStartPending)
            {
                autoStartRemaining -= Time.deltaTime;
                if (autoStartRemaining <= 0f)
                    StartScenario();
            }

            if (phase == EvacPhase.Action)
            {
                AdvanceTime(Time.deltaTime);
                UpdateAids(Time.deltaTime);
            }
            else if (phase == EvacPhase.Resolution && resolutionPending)
            {
                resolutionRemaining -= Time.deltaTime;
                if (resolutionRemaining <= 0f)
                    PublishResult();
            }
        }

        /// <summary>Starts the scenario: Briefing -> Action, timer/stopwatch begins and the alarm is considered active.</summary>
        public void StartScenario()
        {
            autoStartPending = false;
            if (phase != EvacPhase.Briefing)
            {
                UDebug.LogWarning($"[EV] StartScenario ignored: phase is {phase} (reset first).");
                return;
            }

            elapsed = 0f;
            EnsureReplayStartLogged();
            SetPhase(EvacPhase.Action);
            UDebug.Log($"[EV] Scenario started: {difficulty}, {(HasTimeLimit ? "countdown " + EffectiveTimeLimitSeconds.ToString("0") + " s" : "stopwatch")}, hearts {hearts}, seed {seed}.");
        }

        /// <summary>Advances one phase: Briefing starts, Action ends the run as it stands (debug), Resolution publishes the result.</summary>
        public void AdvancePhase()
        {
            switch (phase)
            {
                case EvacPhase.Briefing:
                    StartScenario();
                    break;
                case EvacPhase.Action:
                    EndScenario("debug");
                    break;
                case EvacPhase.Resolution:
                    if (resolutionPending)
                        PublishResult();
                    break;
                default:
                    UDebug.Log("[EV] Already in ResultHandoff: use reset.");
                    break;
            }
        }

        /// <summary>Selects a difficulty, loads its config and performs a full reset.</summary>
        public void SetDifficulty(EvacDifficulty value)
        {
            difficulty = value;
            ResetScenario();
        }

        /// <summary>Returns the configuration assigned for a difficulty (null if missing).</summary>
        public EvacuationConfig ConfigFor(EvacDifficulty value)
        {
            if (configurations == null)
                return null;
            for (int i = 0; i < configurations.Length; i++)
            {
                if (configurations[i] != null && configurations[i].Difficulty == value)
                    return configurations[i];
            }

            return null;
        }

        /// <summary>Forces the countdown to expire. Ignored when the level has no time limit (Guided).</summary>
        public void ForceTimeUp()
        {
            if (phase != EvacPhase.Action)
            {
                UDebug.LogWarning($"[EV] ForceTimeUp ignored: phase is {phase}.");
                return;
            }

            if (!HasTimeLimit)
            {
                UDebug.Log($"[EV] ForceTimeUp ignored: {difficulty} has no time limit.");
                return;
            }

            elapsed = EffectiveTimeLimitSeconds;
            HandleTimeExpired();
        }

        /// <summary>Advances the scenario clock and the aid timers by the given seconds (debug/tests).</summary>
        public void AdvanceDebugTime(float seconds)
        {
            if (seconds <= 0f)
                return;
            AdvanceTime(seconds);
            UpdateAids(seconds);
        }

        /// <summary>Overrides the active time limit for the current run only (Editor/debug smoke tests).</summary>
        public void SetDebugTimeLimit(float seconds)
        {
            hasDebugTimeLimitOverride = seconds > 0f;
            debugTimeLimitOverrideSeconds = hasDebugTimeLimitOverride ? seconds : -1f;
        }

        /// <summary>Clears the debug time-limit override (used by smoke tests).</summary>
        public void ClearDebugTimeLimit()
        {
            hasDebugTimeLimitOverride = false;
            debugTimeLimitOverrideSeconds = -1f;
        }

        /// <summary>Resets the whole scenario: config, state, log, seed, registered systems, and teleports the rig to SPAWN_Player.</summary>
        public void ResetScenario()
        {
            autoStartPending = false;
            resolutionPending = false;

            config = ConfigFor(difficulty);
            if (config == null)
                UDebug.LogError($"[EV] No EvacuationConfig assigned for {difficulty}.", this);

            elapsed = 0f;
            hearts = config != null ? config.Hearts : 0;
            alive = true;
            timedOut = false;
            timeUpRaised = false;
            planInspected = false;
            planInspectedAt = -1f;
            room1CompletedAt = -1f;
            room2CompletedAt = -1f;
            npcRoom1 = 0;
            npcRoom2 = 0;
            fireContacts = 0;
            elevatorUsed = false;
            exitReached = EvacExit.None;
            anyActionDone = false;
            roomExitRecorded = false;
            impulsiveExit = false;
            endReason = string.Empty;
            outcome = null;
            lastResult = null;
            actionLog.Clear();
            idleTimer = 0f;
            idleHintIssued = false;
            hintIndex = 0;
            hintRequests = 0;
            reducedEffects = config != null && config.StartWithReducedEffects;
            replayNpcSlots = string.Empty;
            replayFireSlots = string.Empty;
            replayStartLogged = false;

            ApplySeed();
            ResetRegisteredSystems();
            TeleportRigTo(spawnMarker);

            SetPhase(EvacPhase.Briefing);
            SetDanger(EvacDangerLevel.Mid);
            HeartsChanged?.Invoke(hearts);
            ScenarioReset?.Invoke();
            UDebug.Log($"[EV] Reset: {difficulty}, hearts {hearts}, {(HasTimeLimit ? EffectiveTimeLimitSeconds.ToString("0") + " s" : "no limit")}, seed {seed}{(useFixedSeed ? " (fixed)" : string.Empty)}, resettables {resettables.Count}.");
        }

        /// <summary>IResettable entry point (the Director is not registered in its own list).</summary>
        public void ResetState()
        {
            ResetScenario();
        }

        /// <summary>Registers a system to be reset by ResetScenario. Duplicates are ignored.</summary>
        public void RegisterResettable(IResettable resettable)
        {
            if (resettable == null || ReferenceEquals(resettable, this) || resettables.Contains(resettable))
                return;
            resettables.Add(resettable);
        }

        /// <summary>Removes a system from the reset registry.</summary>
        public void UnregisterResettable(IResettable resettable)
        {
            if (resettable != null)
                resettables.Remove(resettable);
        }

        /// <summary>Teleports the rig root to a marker (position + yaw). Returns false if rig or marker is missing.</summary>
        public bool TeleportRigTo(Transform marker)
        {
            Transform root = RigRoot;
            if (root == null || marker == null)
                return false;

            root.SetPositionAndRotation(marker.position, Quaternion.Euler(0f, marker.eulerAngles.y, 0f));
            return true;
        }

        /// <summary>Fixes the seed (applied immediately and at every following reset).</summary>
        public void SetFixedSeed(int value)
        {
            useFixedSeed = true;
            fixedSeed = value;
            SetSeed(value);
        }

        /// <summary>Removes the fixed seed and draws a new random one immediately.</summary>
        public void ClearFixedSeed()
        {
            useFixedSeed = false;
            SetSeed(NewRandomSeed());
        }

        /// <summary>Reports that an evacuation plan was inspected. Returns true only the first time.</summary>
        public bool ReportPlanInspected(string planId = null)
        {
            if (phase != EvacPhase.Action || planInspected)
                return false;

            planInspected = true;
            planInspectedAt = elapsed;
            anyActionDone = true;
            Record(EvacActionIds.Lg01PlanInspected, true, Dimension.Awareness);
            SetDanger(EvacDangerLevel.Up);
            NoteProgress();
            PlanInspected?.Invoke(planId ?? string.Empty);
            return true;
        }

        /// <summary>Reports that an NPC of the given room (1 or 2) joined the line.</summary>
        public void ReportNpcCollected(int room)
        {
            if (phase != EvacPhase.Action || config == null)
                return;
            if (room != 1 && room != 2)
            {
                UDebug.LogWarning($"[EV] ReportNpcCollected: invalid room {room} (expected 1 or 2).", this);
                return;
            }

            int total = room == 1 ? config.NpcCountRoom1 : config.NpcCountRoom2;
            int current = room == 1 ? npcRoom1 : npcRoom2;
            if (current >= total)
            {
                UDebug.LogWarning($"[EV] ReportNpcCollected: room {room} already complete ({total}/{total}).", this);
                return;
            }

            current++;
            anyActionDone = true;
            if (room == 1)
                npcRoom1 = current;
            else
                npcRoom2 = current;

            SetDanger(EvacDangerLevel.Up);
            NoteProgress();
            NpcCollected?.Invoke(room, npcRoom1 + npcRoom2);
            if (current == total)
            {
                if (room == 1)
                {
                    room1CompletedAt = elapsed;
                    Record(EvacActionIds.Lg03Room1Done, true, Dimension.Execution);
                }
                else
                {
                    room2CompletedAt = elapsed;
                    Record(EvacActionIds.Lg04Room2Done, true, Dimension.Execution);
                }
            }
        }

        /// <summary>Reports that the player reached an exit zone. Ends the scenario.</summary>
        public void ReportExitReached(EvacExit kind)
        {
            if (phase != EvacPhase.Action)
                return;
            if (kind == EvacExit.None)
            {
                UDebug.LogWarning("[EV] ReportExitReached(None) ignored.", this);
                return;
            }

            exitReached = kind;
            SetDanger(kind == EvacExit.Emergency ? EvacDangerLevel.Up : EvacDangerLevel.Down);
            NoteProgress();
            if (!planInspected)
                Record(EvacActionIds.Lg02NoPlan, false, Dimension.Awareness);

            if (kind == EvacExit.Emergency)
            {
                if (AllNpcCollected())
                    Record(EvacActionIds.Lg09StairsAllNpcs, true, Dimension.Decision);
                else
                    Record(EvacActionIds.Lg05StairsMissingNpcs, false, Dimension.Decision);
            }
            else
            {
                Record(EvacActionIds.Lg11InternalStairs, false, Dimension.Decision);
            }

            EndScenario(kind == EvacExit.Emergency ? "exit.emergency" : "exit.internal");
            ExitReported?.Invoke(kind);
        }

        /// <summary>Reports that the player pressed an elevator call button. Failure, except in Guided (danger Down, scenario continues).</summary>
        public void ReportElevatorUsed()
        {
            if (phase != EvacPhase.Action || elevatorUsed)
                return;

            elevatorUsed = true;
            Record(EvacActionIds.Lg06Elevator, false, Dimension.Safety);
            SetDanger(EvacDangerLevel.Down);
            NoteProgress();
            if (difficulty != EvacDifficulty.Guided)
                EndScenario("elevator");
        }

        /// <summary>Reports a fire contact (the caller handles the 2 s cooldown). The 3rd contact (config) kills the player.</summary>
        public void ReportFireContact()
        {
            if (phase != EvacPhase.Action || config == null)
                return;
            if (config.Hearts <= 0)
            {
                UDebug.Log($"[EV] Fire contact ignored: {difficulty} takes no damage.");
                return;
            }

            fireContacts++;
            int lethal = Mathf.Max(1, config.LethalContacts);
            float intensity = Mathf.Clamp01((float)fireContacts / lethal);
            FireContact?.Invoke(fireContacts, intensity);

            if (fireContacts >= lethal)
            {
                Record(EvacActionIds.Lg08FireDeath, false, Dimension.Safety);
                hearts = 0;
                alive = false;
                HeartsChanged?.Invoke(hearts);
                DamageTaken?.Invoke(DamageSourceFire, hearts);
                Died?.Invoke();
                EndScenario("death");
            }
            else
            {
                Record(EvacActionIds.Lg07FireContact, false, Dimension.Safety);
            }
        }

        /// <summary>Reports that the player left the starting room (used to detect an impulsive exit in the first seconds).</summary>
        public void ReportRoomExited()
        {
            if (phase != EvacPhase.Action || roomExitRecorded || config == null)
                return;

            roomExitRecorded = true;
            if (!anyActionDone && elapsed <= config.Scoring.ImpulsiveWindowSeconds)
            {
                impulsiveExit = true;
                UDebug.Log($"[EV] Impulsive exit detected at {elapsed:0.0} s.");
            }
        }

        /// <summary>Reports the alarm state; raises <see cref="AlarmStateChanged"/> only on changes.</summary>
        public void ReportAlarmState(bool active)
        {
            if (alarmActive == active)
                return;

            alarmActive = active;
            AlarmStateChanged?.Invoke(active);
        }

        /// <summary>Relays an NPC dialogue request (stable key) to the future UI/audio.</summary>
        public void ReportNpcDialogue(string key)
        {
            if (!string.IsNullOrEmpty(key))
                NpcDialogueRequested?.Invoke(key);
        }

        /// <summary>Relays a hint request (stable key). Only the Guided level provides hints.</summary>
        public void RequestHint(string key)
        {
            if (string.IsNullOrEmpty(key) || !HintsEnabled)
                return;
            HintRequested?.Invoke(key);
        }

        /// <summary>Requests the next ordered hint for the current progress step (Guided only).</summary>
        public void RequestNextHint()
        {
            if (!HintsEnabled || phase != EvacPhase.Action)
                return;

            string key = HintKeyAt(hintIndex);
            if (string.IsNullOrEmpty(key))
                return;

            hintRequests++;
            idleTimer = 0f;
            idleHintIssued = true;
            RequestHint(key);
            hintIndex = CurrentHintStepIndex();
            UDebug.Log($"[EV] Hint requested: {key} (index {hintIndex}/{config.OrderedHints.Count}).");
        }

        /// <summary>Overrides the reduced-effects flag (E09 will consume it for fire/smoke).</summary>
        public void SetReducedEffects(bool value)
        {
            reducedEffects = value;
            UDebug.Log($"[EV] ReducedEffects {reducedEffects} ({difficulty}).");
        }

        /// <summary>Prints the behaviour of the active difficulty level through the logger.</summary>
        public void PrintLevelBehavior(Action<string> logger)
        {
            if (logger == null)
                return;

            string time = HasTimeLimit ? EffectiveTimeLimitSeconds.ToString("0") + " s countdown" : "stopwatch (no defeat)";
            string keys = HintsEnabled ? string.Join(", ", HintKeys()) : "-";
            string idle = config != null ? config.HintIdleSeconds.ToString("0.0") : "n/a";
            logger($"[EV] Level {difficulty}: time={time} hearts={hearts} highlights={HighlightsEnabled} hints={HintsEnabled} hintKeys=[{keys}] idle={idle}s reduced={reducedEffects} danger={danger} fire={config != null && config.FireEnabled}");
        }

        /// <summary>Builds the end-state snapshot consumed by the evaluator.</summary>
        public EvacuationSnapshot BuildSnapshot()
        {
            var s = new EvacuationSnapshot
            {
                Difficulty = difficulty,
                PlanInspected = planInspected,
                NpcCollectedRoom1 = npcRoom1,
                NpcCollectedRoom2 = npcRoom2,
                NpcTotalRoom1 = config != null ? config.NpcCountRoom1 : 0,
                NpcTotalRoom2 = config != null ? config.NpcCountRoom2 : 0,
                ExitReached = exitReached,
                ElevatorUsed = elevatorUsed,
                FireContacts = fireContacts,
                LethalContacts = config != null ? config.LethalContacts : 3,
                Alive = alive,
                TimedOut = timedOut,
                ElapsedSeconds = elapsed,
                TimeLimitSeconds = HasTimeLimit ? EffectiveTimeLimitSeconds : 0f,
                ImpulsiveExit = impulsiveExit,
                PlanInspectedAt = planInspectedAt,
                Room1CompletedAt = room1CompletedAt,
                Room2CompletedAt = room2CompletedAt
            };
            return s;
        }

        /// <summary>Prints the current state through the logger.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger == null)
                return;
            logger($"[EV] state: phase={phase} diff={difficulty} elapsed={elapsed:0.0}s remaining={(HasTimeLimit ? TimeRemaining.ToString("0.0") + "s" : "n/a")} hearts={hearts} alive={alive} contacts={fireContacts} plan={planInspected} npc={npcRoom1}+{npcRoom2}/{(config != null ? config.NpcCountTotal : 0)} elevator={elevatorUsed} exit={exitReached} danger={danger} seed={seed}{(useFixedSeed ? " (fixed)" : string.Empty)} outcome={(outcome.HasValue ? outcome.Value.ToString() : "-")} log={actionLog.Count} resettables={resettables.Count}");
        }

        /// <summary>Prints the result recap (outcome, sub-scores, GDD points, action log) through the logger.</summary>
        public void PrintResult(Action<string> logger)
        {
            if (logger == null)
                return;
            if (lastResult == null)
            {
                logger("[EV] result: not available yet.");
                return;
            }

            int points = outcome == EvacOutcome.Success ? config.SuccessPoints : outcome == EvacOutcome.Partial ? config.PartialPoints : config.FailurePoints;
            logger($"[EV] RESULT {lastResult.Outcome} ({difficulty}, reason {endReason}, {elapsed:0.0}s, seed {seed}) scores S={lastResult.GetSubScore(Dimension.Safety)} A={lastResult.GetSubScore(Dimension.Awareness)} D={lastResult.GetSubScore(Dimension.Decision)} E={lastResult.GetSubScore(Dimension.Execution)} T={lastResult.GetSubScore(Dimension.Time)} gddPoints={points}");
            for (int i = 0; i < lastResult.ActionLog.Entries.Count; i++)
            {
                ActionLogEntry e = lastResult.ActionLog.Entries[i];
                logger($"[EV]   log t={e.Timestamp:0.0} {e.ActionId} {EvacActionIds.GetSeverity(e.ActionId)} {e.Dimension} key={e.ConsequenceKey}");
            }
        }

        private void ResolveReferences()
        {
            if (rigRoot == null)
            {
                var rig = FindFirstObjectByType<OVRCameraRig>();
                if (rig != null)
                    rigRoot = rig.transform;
            }

            if (spawnMarker == null)
            {
                GameObject marker = GameObject.Find("SPAWN_Player");
                if (marker != null)
                    spawnMarker = marker.transform;
            }
        }

        private void AdvanceTime(float seconds)
        {
            if (phase != EvacPhase.Action || seconds <= 0f)
                return;

            elapsed += seconds;
            if (HasTimeLimit && elapsed >= EffectiveTimeLimitSeconds)
            {
                elapsed = EffectiveTimeLimitSeconds;
                HandleTimeExpired();
            }
        }

        private void HandleTimeExpired()
        {
            if (timeUpRaised || phase != EvacPhase.Action)
                return;

            timeUpRaised = true;
            timedOut = true;
            Record(EvacActionIds.Lg10Timeout, false, Dimension.Time);
            TimeUp?.Invoke();
            EndScenario("timeout");
        }

        private bool AllNpcCollected()
        {
            return config != null && npcRoom1 + npcRoom2 >= config.NpcCountTotal;
        }

        private void EndScenario(string reason)
        {
            if (phase != EvacPhase.Action)
                return;

            endReason = reason;
            SetPhase(EvacPhase.Resolution);
            lastResult = evaluator.Evaluate(BuildSnapshot(), config != null ? config.Scoring : null, actionLog);
            outcome = lastResult.Outcome;

            if (resolutionSeconds <= 0f)
            {
                PublishResult();
            }
            else
            {
                resolutionPending = true;
                resolutionRemaining = resolutionSeconds;
            }
        }

        private void PublishResult()
        {
            resolutionPending = false;
            SetPhase(EvacPhase.ResultHandoff);
            PrintResult(UDebug.Log);
            ResultReady?.Invoke(lastResult);
        }

        private void Record(string actionId, bool isCorrect, Dimension dimension)
        {
            ActionLogEntry entry = actionLog.Add(elapsed, actionId, isCorrect, dimension, EvacActionIds.PhraseKey(actionId));
            UDebug.Log($"[EV] Action {actionId} {EvacActionIds.GetSeverity(actionId)} {dimension} t={elapsed:0.0}s");
            ActionRecorded?.Invoke(entry);
        }

        private void SetPhase(EvacPhase next)
        {
            if (phase == next)
                return;

            EvacPhase previous = phase;
            phase = next;
            UDebug.Log($"[EV] Phase {previous} -> {next}");
            PhaseChanged?.Invoke(next);
        }

        private void SetDanger(EvacDangerLevel next)
        {
            if (danger == next)
                return;

            danger = next;
            DangerLevelChanged?.Invoke(next);
        }

        private void NoteProgress()
        {
            idleTimer = 0f;
            idleHintIssued = false;
            if (config != null)
                hintIndex = CurrentHintStepIndex();
        }

        private void UpdateAids(float seconds)
        {
            if (phase != EvacPhase.Action || seconds <= 0f || config == null)
                return;

            idleTimer += seconds;
            if (idleTimer >= config.HintIdleSeconds && danger != EvacDangerLevel.Down)
                SetDanger(EvacDangerLevel.Mid);

            if (!idleHintIssued && HintsEnabled && idleTimer >= config.HintIdleSeconds)
                RequestNextHint();
        }

        private int CurrentHintStepIndex()
        {
            if (!planInspected)
                return 0;
            if (config == null || npcRoom1 < config.NpcCountRoom1)
                return 1;
            if (npcRoom2 < config.NpcCountRoom2)
                return 2;
            if (exitReached == EvacExit.None)
                return hintIndex >= 3 ? 4 : 3;
            return 4;
        }

        private string HintKeyAt(int index)
        {
            if (!HintsEnabled)
                return string.Empty;
            int count = config.OrderedHints.Count;
            var hint = config.OrderedHints[Mathf.Clamp(index, 0, count - 1)];
            return hint != null ? hint.LocalizationKey ?? string.Empty : string.Empty;
        }

        private string[] HintKeys()
        {
            if (!HintsEnabled)
                return new string[0];
            var keys = new string[config.OrderedHints.Count];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = HintKeyAt(i);
            return keys;
        }

        private void EnsureReplayStartLogged()
        {
            if (replayStartLogged)
                return;

            replayStartLogged = true;
            Record(EvacActionIds.ReplayStart, true, Dimension.Execution);
            UDebug.Log($"[EV] ReplayStart seed={seed} npc=[{(string.IsNullOrEmpty(replayNpcSlots) ? "-" : replayNpcSlots)}] fire=[{(string.IsNullOrEmpty(replayFireSlots) ? "-" : replayFireSlots)}]");
            ReplayStarted?.Invoke(seed, replayNpcSlots, replayFireSlots);
        }

        private void ApplySeed()
        {
            SetSeed(useFixedSeed ? fixedSeed : NewRandomSeed());
        }

        private void SetSeed(int value)
        {
            seed = value;
            rng = new System.Random(value);
            UDebug.Log($"[EV] Seed {value}{(useFixedSeed ? " (fixed)" : string.Empty)}");
            SeedChanged?.Invoke(value);
        }

        /// <summary>Draws a fresh seed. The counter keeps rapid consecutive resets from repeating a time-based seed.</summary>
        private int NewRandomSeed()
        {
            seedDraws++;
            int mix = System.Environment.TickCount ^ unchecked((int)(seedDraws * 2654435761L));
            return new System.Random(mix).Next(1, int.MaxValue);
        }

        private void ResetRegisteredSystems()
        {
            IResettable[] snapshot = resettables.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                IResettable item = snapshot[i];
                bool destroyed = item is UnityEngine.Object unityObject && unityObject == null;
                if (item == null || destroyed)
                {
                    resettables.Remove(item);
                    continue;
                }

                try
                {
                    item.ResetState();
                }
                catch (Exception exception)
                {
                    UDebug.LogError($"[EV] Reset failed on {item.GetType().Name}: {exception.Message}", this);
                }
            }
        }
    }
}
