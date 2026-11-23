using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Data;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Hidden ventilation escalation driven by the Action phase.
    /// </summary>
    public sealed class VentilationEscalation : MonoBehaviour
    {
        /// <summary>Escalation stages from safe air to active loss.</summary>
        public enum Stage
        {
            Inactive,
            Cough,
            Toxic,
            Loss
        }

        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private VentilationSystem ventilationSystem;
        [SerializeField, Min(1f)] private float debugTimeMultiplier = 1f;

        private Stage stage;
        private float elapsedSeconds;
        private float coughThresholdSeconds;
        private float toxicThresholdSeconds;
        private float lossThresholdSeconds;
        private float stageProgress;
        private bool tracking;
        private bool coughRaised;
        private bool lossRaised;
        private System.Random seededRandom;

        /// <summary>Raised once at the cough threshold.</summary>
        public event Action CoughTriggered;
        /// <summary>Raised when toxic progression changes, with progress from 0 to 1.</summary>
        public event Action<float> VentilationStageChanged;
        /// <summary>Raised once when the loss threshold is reached.</summary>
        public event Action LossStarted;

        /// <summary>Gets the current escalation stage.</summary>
        public Stage CurrentStage => stage;
        /// <summary>Gets the elapsed escalation time in seconds.</summary>
        public float ElapsedSeconds => elapsedSeconds;
        /// <summary>Gets toxic-stage progress from 0 to 1.</summary>
        public float StageProgress => stageProgress;
        /// <summary>Gets the resolved cough threshold.</summary>
        public float CoughThresholdSeconds => coughThresholdSeconds;
        /// <summary>Gets the resolved toxic threshold.</summary>
        public float ToxicThresholdSeconds => toxicThresholdSeconds;
        /// <summary>Gets the resolved loss threshold.</summary>
        public float LossThresholdSeconds => lossThresholdSeconds;
        /// <summary>Gets the debug time multiplier.</summary>
        public float DebugTimeMultiplier => debugTimeMultiplier;

        /// <summary>Sets the deterministic random stream used for ventilation jitter this run.</summary>
        public void SetRandomSeed(int seed)
        {
            seededRandom = new System.Random(seed);
        }

        private void Awake()
        {
            if (director == null)
                director = GetComponent<ChemicalAlertDirector>();
            if (ventilationSystem == null)
                ventilationSystem = GetComponent<VentilationSystem>();
        }

        private void OnEnable()
        {
            if (director != null)
                director.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (director != null)
                director.PhaseChanged -= HandlePhaseChanged;
        }

        private void Update()
        {
            if (tracking && ventilationSystem != null && !ventilationSystem.IsOn)
                AdvanceDebugTime(Time.deltaTime * debugTimeMultiplier);
        }

        /// <summary>Sets the debug multiplier used by the normal Update loop.</summary>
        public void SetDebugTimeMultiplier(float multiplier)
        {
            debugTimeMultiplier = Mathf.Max(1f, multiplier);
        }

        /// <summary>Advances escalation without waiting for real time.</summary>
        public void AdvanceDebugTime(float seconds)
        {
            if (!tracking || ventilationSystem == null || ventilationSystem.IsOn || seconds <= 0f)
                return;

            elapsedSeconds += seconds;
            EvaluateThresholds();
        }

        /// <summary>Stops escalation because ventilation was activated.</summary>
        public void StopEscalation()
        {
            if (!tracking)
                return;

            tracking = false;
            UnityEngine.Debug.Log($"[CA] Ventilation escalation stopped at {elapsedSeconds:0.00}s; cough={coughRaised}.");
        }

        /// <summary>Resets escalation and threshold state.</summary>
        public void ResetState()
        {
            tracking = false;
            stage = Stage.Inactive;
            elapsedSeconds = 0f;
            stageProgress = 0f;
            coughThresholdSeconds = 0f;
            toxicThresholdSeconds = 0f;
            lossThresholdSeconds = 0f;
            coughRaised = false;
            lossRaised = false;
        }

        /// <summary>Prints the current escalation state through the supplied logger.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger == null)
                return;

            logger($"[CA] Ventilation escalation: stage={stage}; elapsed={elapsedSeconds:0.00}; progress={stageProgress:0.00}; thresholds={coughThresholdSeconds:0.00}/{toxicThresholdSeconds:0.00}/{lossThresholdSeconds:0.00}; tracking={tracking}");
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
            {
                ResetState();
                return;
            }

            if (phase == ChemicalAlertDirector.Phase.Action &&
                ventilationSystem != null && !ventilationSystem.IsOn)
                BeginActionEscalation();
        }

        private void BeginActionEscalation()
        {
            ChemicalAlertConfig config = director == null ? null : director.CurrentConfig;
            float jitter = 0f;
            if (config != null && config.VentilationJitterSeconds > 0f)
            {
                double sample = seededRandom == null
                    ? UnityEngine.Random.value
                    : seededRandom.NextDouble();
                jitter = (float)((sample * 2d - 1d) * config.VentilationJitterSeconds);
            }
            float cough = config == null ? 40f : config.VentilationCoughThresholdSeconds;
            float toxicDelay = config == null ? 20f : config.VentilationToxicStageDelaySeconds;
            float lossDelay = config == null ? 20f : config.VentilationFailureDelaySeconds;

            coughThresholdSeconds = Mathf.Max(0.1f, cough + jitter);
            toxicThresholdSeconds = coughThresholdSeconds + Mathf.Max(0f, toxicDelay);
            lossThresholdSeconds = toxicThresholdSeconds + Mathf.Max(0f, lossDelay);
            tracking = true;
            stage = Stage.Inactive;
            elapsedSeconds = 0f;
            stageProgress = 0f;
            coughRaised = false;
            lossRaised = false;
            UnityEngine.Debug.Log($"[CA] Ventilation escalation started: thresholds={coughThresholdSeconds:0.00}/{toxicThresholdSeconds:0.00}/{lossThresholdSeconds:0.00}s.");
        }

        private void EvaluateThresholds()
        {
            if (!coughRaised && elapsedSeconds >= coughThresholdSeconds)
            {
                coughRaised = true;
                stage = Stage.Cough;
                CoughTriggered?.Invoke();
                UnityEngine.Debug.Log($"[CA] Ventilation cough at {elapsedSeconds:0.00}s.");
            }

            if (coughRaised && stage == Stage.Cough && elapsedSeconds >= toxicThresholdSeconds)
            {
                stage = Stage.Toxic;
                stageProgress = 0f;
                VentilationStageChanged?.Invoke(stageProgress);
                UnityEngine.Debug.Log($"[CA] Ventilation toxic stage at {elapsedSeconds:0.00}s.");
            }

            if (stage == Stage.Toxic)
            {
                float toxicDuration = Mathf.Max(0.001f, lossThresholdSeconds - toxicThresholdSeconds);
                float nextProgress = Mathf.Clamp01((elapsedSeconds - toxicThresholdSeconds) / toxicDuration);
                if (nextProgress > stageProgress)
                {
                    stageProgress = nextProgress;
                    VentilationStageChanged?.Invoke(stageProgress);
                }
            }

            if (!lossRaised && elapsedSeconds >= lossThresholdSeconds)
            {
                lossRaised = true;
                stage = Stage.Loss;
                stageProgress = 1f;
                VentilationStageChanged?.Invoke(stageProgress);
                LossStarted?.Invoke();
                UnityEngine.Debug.Log($"[CA] Ventilation loss at {elapsedSeconds:0.00}s.");
            }
        }
    }
}
