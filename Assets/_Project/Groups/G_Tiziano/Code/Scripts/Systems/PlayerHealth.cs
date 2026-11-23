using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Heart-based player health with Guided danger-only behavior.
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour, IResettable
    {
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField, Min(0)] private int fallbackHearts = 3;
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 1f;

        private int hearts;
        private bool died;
        private float invulnerableUntil;

        /// <summary>Raised when damage is applied, with kind and feedback duration.</summary>
        public event Action<string, float> DamageTaken;
        /// <summary>Raised when Guided mode receives a danger contact without damage.</summary>
        public event Action<string, float> DangerDetected;
        /// <summary>Raised once when hearts reach zero.</summary>
        public event Action Died;

        /// <summary>Gets the current heart count.</summary>
        public int Hearts => hearts;
        /// <summary>Gets whether the player has died.</summary>
        public bool IsDead => died;

        private void Awake()
        {
            if (director == null)
                director = GetComponent<ChemicalAlertDirector>();
            ResetState();
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

        /// <summary>Applies one damage event unless Guided mode suppresses damage.</summary>
        public bool TakeDamage(string kind, float durationSeconds)
        {
            if (died || Time.time < invulnerableUntil)
                return false;

            if (director != null &&
                director.SelectedDifficulty == ChemicalAlertDirector.Difficulty.Guided)
            {
                ReportDanger(kind, durationSeconds);
                return false;
            }

            hearts = Mathf.Max(0, hearts - 1);
            invulnerableUntil = Time.time + invulnerabilitySeconds;
            DamageTaken?.Invoke(kind, durationSeconds);
            if (director != null)
                director.RecordAction("health.damage", false, Dimension.Safety, "chemical.burn");
            UnityEngine.Debug.Log($"[CA] DamageTaken: kind={kind}; duration={durationSeconds:0.00}s; hearts={hearts}");

            if (hearts == 0)
            {
                died = true;
                Died?.Invoke();
                UnityEngine.Debug.Log("[CA] Died.");
            }

            return true;
        }

        /// <summary>Applies one damage event while bypassing invulnerability for debug checks.</summary>
        public bool DebugForceDamage(string kind, float durationSeconds)
        {
            invulnerableUntil = 0f;
            return TakeDamage(kind, durationSeconds);
        }

        /// <summary>Raises a danger event without reducing hearts.</summary>
        public void ReportDanger(string kind, float durationSeconds)
        {
            DangerDetected?.Invoke(kind, durationSeconds);
            UnityEngine.Debug.Log($"[CA] DangerDetected: kind={kind}; duration={durationSeconds:0.00}s; hearts={hearts}");
        }

        /// <summary>Resets hearts and invulnerability from the active config.</summary>
        public void ResetState()
        {
            hearts = director == null || director.CurrentConfig == null
                ? fallbackHearts
                : director.CurrentConfig.Hearts;
            died = false;
            invulnerableUntil = 0f;
        }

        /// <summary>Prints current health through the supplied logger.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger != null)
                logger($"[CA] Health: hearts={hearts}; dead={died}; invulnerable={(Time.time < invulnerableUntil)}");
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }
    }
}
