using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Single runtime boundary for Chemical Alert state and events.
    /// </summary>
    public sealed class ChemicalAlertDirector : MonoBehaviour, IResettable
    {
        /// <summary>Available scenario difficulty configurations.</summary>
        public enum Difficulty
        {
            Guided,
            Operational,
            Crisis
        }

        /// <summary>Ordered scenario phases.</summary>
        public enum Phase
        {
            Briefing,
            Scan,
            Action,
            Resolution,
            ResultHandoff
        }

        /// <summary>Readable danger levels for the future UI and recap systems.</summary>
        public enum DangerLevel
        {
            Down,
            Mid,
            Up
        }

        [SerializeField] private Difficulty difficulty = Difficulty.Operational;
        [SerializeField] private ChemicalAlertConfig[] configurations = new ChemicalAlertConfig[0];
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private VentilationEscalation ventilationEscalation;
        [SerializeField] private CleanupSystem cleanupSystem;
        [SerializeField] private VentilationSystem ventilationSystem;
        [SerializeField] private Puddle puddle;
        [SerializeField] private SdsItem sdsItem;
        [SerializeField] private PlayerPPEState playerPPEState;
        [SerializeField] private ScenarioInputBindings inputBindings;
        [SerializeField] private SI81.Input.Hand hintController = SI81.Input.Hand.Right;
        [SerializeField] private ScenarioInteractable[] guidedHighlightTargets = new ScenarioInteractable[0];
        [SerializeField] private ScenarioInteractable[] operationalDistractors = new ScenarioInteractable[0];
        [SerializeField] private DisturbanceController disturbanceController;

        private readonly OutcomeEvaluator outcomeEvaluator = new OutcomeEvaluator();
        private ChemicalAlertConfig activeConfiguration;
        private Phase phase;
        private float timeRemaining;
        private int hearts;
        private Outcome? outcome;
        private ScenarioResult lastResult;
        private ActionLog actionLog = new ActionLog();
        private bool timeUpRaised;
        private bool ventilationFailed;
        private bool lossContained;
        private bool cleanupComplete;
        private bool secondSpillRegistered;
        private bool ventilationActivated;
        private bool sdsRead;
        private bool completionDelayPending;
        private bool sdsPanelOpen;
        private bool eyeProtectionWarningRaised;
        private float completionDelayRemaining;
        private DangerLevel dangerLevel = DangerLevel.Down;
        private float lastCorrectProgressTime;
        private int nextHintIndex;
        private float lastVignetteProgress = -1f;

        /// <summary>Raised whenever the scenario phase changes.</summary>
        public event Action<Phase> PhaseChanged;
        /// <summary>Raised once when the configured timer expires.</summary>
        public event Action TimeUp;
        /// <summary>Raised when a ScenarioResult is ready for the future UI/Game System.</summary>
        public event Action<ScenarioResult> ResultReady;
        /// <summary>Raised when the configured dynamic second spill occurs.</summary>
        public event Action SecondSpillOccurred;
        /// <summary>Raised when a Guided hint localization key is requested.</summary>
        public event Action<string> HintRequested;
        /// <summary>Raised when the readable ventilation danger level changes.</summary>
        public event Action<DangerLevel> DangerLevelChanged;
        /// <summary>Raised when the SDS is read so the SDS panel can be shown.</summary>
        public event Action<SubstanceVariant> SdsRead;
        /// <summary>Raised when a PPE type is acquired.</summary>
        public event Action<string> PpeAcquired;
        /// <summary>Raised whenever current hearts change.</summary>
        public event Action<int> HeartsChanged;
        /// <summary>Raised with a stable error localization key.</summary>
        public event Action<string> ErrorReported;
        /// <summary>Raised when the SDS panel should close.</summary>
        public event Action SdsPanelDismissRequested;
        /// <summary>Raised with vignette intensity (zero hides) and the current hazard color.</summary>
        public event Action<float, Color> ToxicVignetteChanged;
        /// <summary>Raised when a cough cue should be played.</summary>
        public event Action CoughRequested;

        /// <summary>Gets the current phase.</summary>
        public Phase CurrentPhase => phase;
        /// <summary>Gets the remaining action time in seconds.</summary>
        public float TimeRemaining => timeRemaining;
        /// <summary>Gets the current hearts.</summary>
        public int Hearts => hearts;
        /// <summary>Gets the selected difficulty.</summary>
        public Difficulty SelectedDifficulty => difficulty;
        /// <summary>Gets the active difficulty configuration.</summary>
        public ChemicalAlertConfig CurrentConfig => activeConfiguration;
        /// <summary>Gets the current outcome, if one is available.</summary>
        public Outcome? Outcome => outcome;
        /// <summary>Gets the current action log.</summary>
        public ActionLog ActionLog => actionLog;
        /// <summary>Gets the last emitted result, if one is available.</summary>
        public ScenarioResult LastResult => lastResult;
        /// <summary>Gets the current ventilation danger level.</summary>
        public DangerLevel CurrentDangerLevel => dangerLevel;
        /// <summary>Gets the number of ordered Guided hints already emitted.</summary>
        public int EmittedHintCount => nextHintIndex;
        /// <summary>Gets whether reduced Crisis effects are active.</summary>
        public bool ReducedEffects => disturbanceController != null && disturbanceController.ReducedEffects;
        /// <summary>Gets whether industrial ventilation is currently on.</summary>
        public bool IsVentilationOn => ventilationSystem != null && ventilationSystem.IsOn;
        /// <summary>Gets whether the SDS information panel is currently open.</summary>
        public bool IsSdsPanelOpen => sdsPanelOpen;

        /// <summary>Publishes current health to the UI/Game System boundary.</summary>
        public void NotifyHeartsChanged(int value)
        {
            hearts = Mathf.Max(0, value);
            HeartsChanged?.Invoke(hearts);
        }

        private void Awake()
        {
            if (ventilationSystem == null)
                ventilationSystem = GetComponent<VentilationSystem>();
            if (disturbanceController == null)
                disturbanceController = GetComponent<DisturbanceController>();
            ResetScenario();
        }

        private void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.Died += HandlePlayerDied;
            if (ventilationEscalation != null)
                ventilationEscalation.LossStarted += HandleVentilationLoss;
            if (cleanupSystem != null)
                cleanupSystem.CleanupComplete += HandleCleanupComplete;
            if (puddle != null)
                puddle.ContainedChanged += HandleContainedChanged;
            if (ventilationEscalation != null)
            {
                ventilationEscalation.CoughTriggered += HandleCoughTriggered;
                ventilationEscalation.VentilationStageChanged += HandleVentilationStageChanged;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.Died -= HandlePlayerDied;
            if (ventilationEscalation != null)
                ventilationEscalation.LossStarted -= HandleVentilationLoss;
            if (cleanupSystem != null)
                cleanupSystem.CleanupComplete -= HandleCleanupComplete;
            if (puddle != null)
                puddle.ContainedChanged -= HandleContainedChanged;
            if (ventilationEscalation != null)
            {
                ventilationEscalation.CoughTriggered -= HandleCoughTriggered;
                ventilationEscalation.VentilationStageChanged -= HandleVentilationStageChanged;
            }
        }

        private void Update()
        {
            UpdateDangerLevel();
            UpdateToxicVignette();

            if (phase != Phase.Action || outcome.HasValue)
                return;

            if (completionDelayPending)
            {
                completionDelayRemaining = Mathf.Max(0f, completionDelayRemaining - Time.deltaTime);
                if (completionDelayRemaining <= 0f)
                    CompleteAutomaticResult();
                return;
            }

            if (difficulty == Difficulty.Guided && activeConfiguration != null &&
                activeConfiguration.HintsEnabled)
            {
                if (inputBindings != null && inputBindings.GetHintDown(hintController))
                    RequestHint();

                if (Time.time - lastCorrectProgressTime >= activeConfiguration.HintIdleSeconds)
                    EmitNextHint("idle");
            }

            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            if (timeRemaining <= 0f && ActiveConfigDefeatsOnTimeout())
                ForceTimeUp();
        }

        /// <summary>Advances one phase in the documented scenario order.</summary>
        public void AdvancePhase()
        {
            if (outcome.HasValue || phase == Phase.ResultHandoff)
                return;

            switch (phase)
            {
                case Phase.Briefing:
                    SetPhase(Phase.Scan);
                    break;
                case Phase.Scan:
                    SetPhase(Phase.Action);
                    break;
                case Phase.Action:
                    SetPhase(Phase.Resolution);
                    break;
                case Phase.Resolution:
                    CompleteResult(Core.Outcome.Partial, false);
                    break;
            }
        }

        /// <summary>Changes difficulty and resets the scenario to its initial state.</summary>
        public void SetDifficulty(Difficulty value)
        {
            difficulty = value;
            ResetScenario();
        }

        /// <summary>Forces the timeout failure path for debug and automated checks.</summary>
        public void ForceTimeUp()
        {
            if (timeUpRaised || outcome.HasValue)
                return;

            timeRemaining = 0f;
            timeUpRaised = true;
            outcome = Core.Outcome.Failure;
            TimeUp?.Invoke();
            SetPhase(Phase.Resolution);
            CompleteResult(Core.Outcome.Failure, true);
        }

        /// <summary>Records a significant action at the scenario boundary.</summary>
        public ActionLogEntry RecordAction(string actionId, bool isCorrect, Dimension dimension, string consequenceKey)
        {
            if (isCorrect)
                lastCorrectProgressTime = Time.time;
            ActionLogEntry entry = actionLog.Add(actionId, isCorrect, dimension, consequenceKey);
            TryScheduleAutomaticResult();
            return entry;
        }

        /// <summary>Registers ventilation activation as a scenario objective.</summary>
        public void NotifyVentilationActivated()
        {
            if (ventilationActivated)
                return;

            ventilationActivated = true;
            RecordAction("ventilation.activate", true, Dimension.Awareness, "chemical.ventilation.on");
            UnityEngine.Debug.Log("[CA] Objective complete: ventilation.activate.");
            TryScheduleAutomaticResult();
        }

        /// <summary>Publishes the SDS content event and marks the SDS objective complete.</summary>
        public void NotifySdsRead(SubstanceVariant variant)
        {
            if (!sdsRead)
            {
                sdsRead = true;
                TryScheduleAutomaticResult();
            }

            sdsPanelOpen = true;
            SdsRead?.Invoke(variant);
            UnityEngine.Debug.Log($"[CA] SDS data exposed: variant={(variant == null ? "None" : variant.VariantId)}.");
        }

        /// <summary>Toggles the SDS panel when the already-read SDS is activated again.</summary>
        public void ToggleSdsPanel(SubstanceVariant variant)
        {
            if (sdsPanelOpen)
            {
                DismissSdsPanel();
                return;
            }

            NotifySdsRead(variant);
        }

        /// <summary>Requests that the SDS panel be dismissed by B or re-interaction.</summary>
        public void DismissSdsPanel()
        {
            if (!sdsPanelOpen)
                return;

            sdsPanelOpen = false;
            SdsPanelDismissRequested?.Invoke();
        }

        /// <summary>Requests the optional cough sound cue through the scenario boundary.</summary>
        public void RequestCoughCue()
        {
            CoughRequested?.Invoke();
        }

        /// <summary>Publishes PPE acquisition and rechecks automatic completion.</summary>
        public void NotifyPpeAcquired(PPEItem item)
        {
            if (item == null)
                return;

            PpeAcquired?.Invoke(item.ItemId);
            UnityEngine.Debug.Log($"[CA] Objective progress: PPE acquired={item.ItemId}.");
            TryScheduleAutomaticResult();
        }

        /// <summary>Logs and publishes a gameplay error, optionally charging one life.</summary>
        public void ReportError(string actionId, string errorKey, Dimension dimension, bool costsLife)
        {
            if (string.IsNullOrWhiteSpace(errorKey))
            {
                UnityEngine.Debug.LogError("[CA] Director: ReportError requires a stable error key.", this);
                return;
            }

            string resolvedActionId = string.IsNullOrWhiteSpace(actionId) ? "error.unspecified" : actionId;
            actionLog.Add(resolvedActionId, false, dimension, errorKey);
            ErrorReported?.Invoke(errorKey);
            UnityEngine.Debug.Log($"[CA] ErrorReported: key={errorKey}; action={resolvedActionId}; difficulty={difficulty}; costsLife={costsLife}.");

            if (costsLife && difficulty != Difficulty.Guided && playerHealth != null)
                playerHealth.TakeDamage("ppe.error", activeConfiguration == null ? 5f : activeConfiguration.PainDurationSeconds, true, false);
        }

        /// <summary>Advances the automatic completion delay for deterministic debug checks.</summary>
        public void AdvanceCompletionDebugTime(float seconds)
        {
            if (!completionDelayPending || seconds <= 0f)
                return;

            completionDelayRemaining = Mathf.Max(0f, completionDelayRemaining - seconds);
            if (completionDelayRemaining <= 0f)
                CompleteAutomaticResult();
        }

        /// <summary>Resets phase, timer, hearts, outcome, result and action log.</summary>
        public void ResetScenario()
        {
            if (sdsPanelOpen)
                SdsPanelDismissRequested?.Invoke();
            sdsPanelOpen = false;
            activeConfiguration = FindConfiguration(difficulty);
            phase = Phase.Briefing;
            timeRemaining = activeConfiguration == null ? 180f : activeConfiguration.TotalDurationSeconds;
            hearts = activeConfiguration == null ? 3 : activeConfiguration.Hearts;
            if (playerHealth != null)
                playerHealth.ResetState();
            outcome = null;
            lastResult = null;
            timeUpRaised = false;
            ventilationFailed = false;
            lossContained = false;
            cleanupComplete = false;
            secondSpillRegistered = false;
            ventilationActivated = false;
            sdsRead = false;
            completionDelayPending = false;
            completionDelayRemaining = 0f;
            sdsPanelOpen = false;
            eyeProtectionWarningRaised = false;
            lastVignetteProgress = -1f;
            lastCorrectProgressTime = Time.time;
            nextHintIndex = 0;
            actionLog.Clear();
            SetDangerLevel(DangerLevel.Down);
            ApplyDifficultyBehavior();
            PhaseChanged?.Invoke(phase);
            SetPhase(Phase.Action);
        }

        /// <summary>Records the dynamic second-spill event for listeners and the result recap.</summary>
        public bool RegisterSecondSpill()
        {
            if (secondSpillRegistered || phase != Phase.Action || outcome.HasValue)
                return false;

            secondSpillRegistered = true;
            actionLog.Add("event.second_spill", true, Dimension.Awareness, "chemical.spill.second");
            SecondSpillOccurred?.Invoke();
            UnityEngine.Debug.Log("[CA] SecondSpillOccurred: actionLog=event.second_spill; consequence=chemical.spill.second.");
            return true;
        }

        /// <summary>Emits the next configured Guided hint on a manual request.</summary>
        public bool RequestHint()
        {
            if (difficulty != Difficulty.Guided || activeConfiguration == null ||
                !activeConfiguration.HintsEnabled || outcome.HasValue)
                return false;

            return EmitNextHint("manual");
        }

        /// <summary>Sets the optional Crisis reduced-effects flag.</summary>
        public void SetReducedEffects(bool enabled)
        {
            if (disturbanceController == null)
            {
                UnityEngine.Debug.LogWarning("[CA] Director: DisturbanceController is not assigned.", this);
                return;
            }

            disturbanceController.SetReducedEffects(enabled);
            UnityEngine.Debug.Log($"[CA] Reduced effects: {enabled}.");
        }

        /// <summary>Prints active difficulty behaviors and disturbance limits.</summary>
        public void PrintDifficultyBehavior(Action<string> logger)
        {
            if (logger == null)
                return;

            logger($"[CA] Difficulty behavior: difficulty={difficulty}; highlights={activeConfiguration != null && activeConfiguration.GuidedHighlights}; hints={activeConfiguration != null && activeConfiguration.HintsEnabled}; distractions={activeConfiguration != null && activeConfiguration.DistractionsEnabled}; danger={dangerLevel}; hintsEmitted={nextHintIndex}");
            if (disturbanceController != null)
                disturbanceController.PrintState(logger);
        }

        /// <summary>Reapplies difficulty-specific object states after replay data is shuffled.</summary>
        public void ReapplyDifficultyBehavior()
        {
            ApplyDifficultyBehavior();
        }

        /// <summary>Implements the common reset contract.</summary>
        public void ResetState()
        {
            ResetScenario();
        }

        /// <summary>Prints a compact state snapshot through the supplied logger.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger == null)
                return;

            logger($"[CA] Director state: phase={phase}; difficulty={difficulty}; time={timeRemaining:0.00}; hearts={hearts}; outcome={(outcome.HasValue ? outcome.Value.ToString() : "None")}; contained={lossContained}; cleanup={cleanupComplete}; actions={actionLog.Count}");
        }

        /// <summary>Prints the last result and its action recap through the supplied logger.</summary>
        public void PrintResult(Action<string> logger)
        {
            if (logger == null)
                return;
            if (lastResult == null)
            {
                logger("[CA] Result: None.");
                return;
            }

            logger($"[CA] Result recap: outcome={lastResult.Outcome}; " +
                $"scores={lastResult.GetSubScore(Dimension.Safety)}/{lastResult.GetSubScore(Dimension.Awareness)}/" +
                $"{lastResult.GetSubScore(Dimension.Decision)}/{lastResult.GetSubScore(Dimension.Execution)}/" +
                $"{lastResult.GetSubScore(Dimension.Time)}; actions={lastResult.ActionLog.Count}");
            foreach (ActionLogEntry entry in lastResult.ActionLog.Entries)
                logger($"[CA] Result action: {entry.ActionId}; correct={entry.IsCorrect}; dimension={entry.Dimension}; consequence={entry.ConsequenceKey}");
        }

        private void SetPhase(Phase nextPhase)
        {
            if (phase == nextPhase)
                return;

            phase = nextPhase;
            PhaseChanged?.Invoke(phase);
        }

        private void CompleteResult(Core.Outcome forcedOutcome, bool ventilationFailed)
        {
            CompleteResult(forcedOutcome, ventilationFailed, null);
        }

        private void CompleteResult(Core.Outcome forcedOutcome, bool ventilationFailed, ScenarioResult evaluatedResult)
        {
            if (phase != Phase.Resolution)
                SetPhase(Phase.Resolution);

            outcome = forcedOutcome;
            lastResult = evaluatedResult ?? outcomeEvaluator.Evaluate(
                actionLog,
                hearts,
                timeRemaining,
                lossContained,
                cleanupComplete,
                ventilationFailed,
                activeConfiguration == null ? 180f : activeConfiguration.TotalDurationSeconds);

            SetPhase(Phase.ResultHandoff);
            ResultReady?.Invoke(lastResult);
            UnityEngine.Debug.Log($"[CA] ResultReady: outcome={lastResult.Outcome}; actions={lastResult.ActionLog.Count}; " +
                $"scores={lastResult.GetSubScore(Dimension.Safety)}/{lastResult.GetSubScore(Dimension.Awareness)}/" +
                $"{lastResult.GetSubScore(Dimension.Decision)}/{lastResult.GetSubScore(Dimension.Execution)}/" +
                $"{lastResult.GetSubScore(Dimension.Time)}");
            foreach (ActionLogEntry entry in lastResult.ActionLog.Entries)
                UnityEngine.Debug.Log($"[CA] ActionLog: t={entry.Timestamp:0.00}; id={entry.ActionId}; correct={entry.IsCorrect}; dimension={entry.Dimension}; consequence={entry.ConsequenceKey}");
        }

        private void HandlePlayerDied()
        {
            CompleteResult(Core.Outcome.Failure, false);
        }

        private void HandleVentilationLoss()
        {
            ventilationFailed = true;
            CompleteResult(Core.Outcome.Failure, true);
        }

        private void HandleCleanupComplete()
        {
            cleanupComplete = true;
            lossContained = puddle == null || puddle.Contained;
            UnityEngine.Debug.Log("[CA] Director: CleanupComplete received.");
            TryScheduleAutomaticResult();
        }

        private void HandleContainedChanged(bool isContained)
        {
            lossContained = isContained;
            TryScheduleAutomaticResult();
        }

        private void HandleCoughTriggered()
        {
            RequestCoughCue();
        }

        private void HandleVentilationStageChanged(float progress)
        {
            UpdateToxicVignette();
        }

        private void TryScheduleAutomaticResult()
        {
            if (completionDelayPending || outcome.HasValue || phase != Phase.Action ||
                !ventilationActivated || !sdsRead ||
                playerPPEState == null || !playerPPEState.HasAllRequiredProtection ||
                !lossContained || !cleanupComplete || puddle == null || !puddle.Covered)
                return;

            completionDelayPending = true;
            completionDelayRemaining = activeConfiguration == null
                ? 2f
                : activeConfiguration.SuccessDelaySeconds;
            UnityEngine.Debug.Log($"[CA] Objectives complete; resolving result in {completionDelayRemaining:0.00}s.");
            if (completionDelayRemaining <= 0f)
                CompleteAutomaticResult();
        }

        private void CompleteAutomaticResult()
        {
            if (!completionDelayPending || outcome.HasValue)
                return;

            completionDelayPending = false;
            ScenarioResult result = outcomeEvaluator.Evaluate(
                actionLog,
                hearts,
                timeRemaining,
                lossContained,
                cleanupComplete,
                ventilationFailed,
                activeConfiguration == null ? 180f : activeConfiguration.TotalDurationSeconds);
            CompleteResult(result.Outcome, ventilationFailed, result);
        }

        private void UpdateToxicVignette()
        {
            bool toxic = ventilationEscalation != null &&
                ventilationEscalation.CurrentStage == VentilationEscalation.Stage.Toxic &&
                (ventilationSystem == null || !ventilationSystem.IsOn);
            bool eyesProtected = playerPPEState != null && playerPPEState.HasEyeProtection;
            float intensity = toxic && !eyesProtected
                ? Mathf.Clamp01(ventilationEscalation.StageProgress)
                : 0f;
            Color tint = puddle != null && puddle.CurrentVariant != null
                ? puddle.CurrentVariant.PuddleColor
                : Color.yellow;

            if (toxic && !eyesProtected && !eyeProtectionWarningRaised)
            {
                eyeProtectionWarningRaised = true;
                ReportError(
                    "ventilation.toxic.without.eyes",
                    "chemical.error.eye_protection",
                    Dimension.Safety,
                    false);
            }

            if (Mathf.Abs(intensity - lastVignetteProgress) > 0.01f)
            {
                lastVignetteProgress = intensity;
                ToxicVignetteChanged?.Invoke(intensity, tint);
            }
        }

        private bool EmitNextHint(string source)
        {
            HintSO[] hints = activeConfiguration == null ? null : activeConfiguration.OrderedHints;
            if (hints == null || nextHintIndex >= hints.Length)
            {
                UnityEngine.Debug.Log("[CA] HintRequested: no configured hint remains.");
                lastCorrectProgressTime = Time.time;
                return false;
            }

            HintSO hint = hints[nextHintIndex++];
            if (hint == null || string.IsNullOrWhiteSpace(hint.LocalizationKey))
            {
                UnityEngine.Debug.LogError($"[CA] Director: invalid hint at index {nextHintIndex - 1}.", this);
                lastCorrectProgressTime = Time.time;
                return false;
            }

            lastCorrectProgressTime = Time.time;
            HintRequested?.Invoke(hint.LocalizationKey);
            UnityEngine.Debug.Log($"[CA] HintRequested: key={hint.LocalizationKey}; step={hint.StepId}; source={source}.");
            return true;
        }

        private void ApplyDifficultyBehavior()
        {
            bool guidedHighlightsEnabled = activeConfiguration != null &&
                difficulty == Difficulty.Guided && activeConfiguration.GuidedHighlights;
            if (guidedHighlightTargets != null)
            {
                foreach (ScenarioInteractable target in guidedHighlightTargets)
                {
                    if (target != null)
                        target.SetGuidedHighlight(guidedHighlightsEnabled);
                }
            }

            bool distractionsEnabled = activeConfiguration != null &&
                activeConfiguration.DistractionsEnabled;
            if (operationalDistractors != null)
            {
                foreach (ScenarioInteractable target in operationalDistractors)
                {
                    if (target == null)
                        continue;

                    PPEItemObject ppeItem = target.GetComponent<PPEItemObject>();
                    bool shouldBeActive = ppeItem == null || !ppeItem.IsDecoy || distractionsEnabled;
                    if (target.gameObject.activeSelf != shouldBeActive)
                        target.gameObject.SetActive(shouldBeActive);
                }
            }

            if (disturbanceController != null)
            {
                disturbanceController.SetFrequency(activeConfiguration == null
                    ? 2.5f
                    : activeConfiguration.DisturbanceFrequencyHz);
                disturbanceController.SetDisturbanceEnabled(
                    difficulty == Difficulty.Crisis &&
                    activeConfiguration != null &&
                    activeConfiguration.DisturbanceEnabled);
            }

            UnityEngine.Debug.Log($"[CA] Difficulty applied: {difficulty}; highlights={guidedHighlightsEnabled}; distractions={distractionsEnabled}; disturbance={difficulty == Difficulty.Crisis && activeConfiguration != null && activeConfiguration.DisturbanceEnabled}.");
        }

        private void UpdateDangerLevel()
        {
            DangerLevel next = DangerLevel.Down;
            if (ventilationSystem == null || !ventilationSystem.IsOn)
            {
                if (ventilationEscalation != null)
                {
                    switch (ventilationEscalation.CurrentStage)
                    {
                        case VentilationEscalation.Stage.Cough:
                            next = DangerLevel.Mid;
                            break;
                        case VentilationEscalation.Stage.Toxic:
                            next = ventilationEscalation.StageProgress >= 0.5f
                                ? DangerLevel.Up
                                : DangerLevel.Mid;
                            break;
                        case VentilationEscalation.Stage.Loss:
                            next = DangerLevel.Up;
                            break;
                    }
                }
            }

            SetDangerLevel(next);
        }

        private void SetDangerLevel(DangerLevel next)
        {
            if (dangerLevel == next)
                return;

            dangerLevel = next;
            DangerLevelChanged?.Invoke(dangerLevel);
            UnityEngine.Debug.Log($"[CA] DangerLevelChanged: {dangerLevel}.");
        }

        private ChemicalAlertConfig FindConfiguration(Difficulty requestedDifficulty)
        {
            string requestedId = requestedDifficulty.ToString();
            if (configurations != null)
            {
                foreach (ChemicalAlertConfig config in configurations)
                {
                    if (config != null && string.Equals(config.DifficultyId, requestedId, StringComparison.OrdinalIgnoreCase))
                        return config;
                }

                foreach (ChemicalAlertConfig config in configurations)
                {
                    if (config != null)
                    {
                        UnityEngine.Debug.LogError($"[CA] Director: config '{requestedId}' missing; using '{config.DifficultyId}'.", this);
                        return config;
                    }
                }
            }

            UnityEngine.Debug.LogError("[CA] Director: no ChemicalAlertConfig assigned; using safe fallback.", this);
            return null;
        }

        private bool ActiveConfigDefeatsOnTimeout()
        {
            return activeConfiguration == null || activeConfiguration.DefeatOnTimeout;
        }
    }
}
