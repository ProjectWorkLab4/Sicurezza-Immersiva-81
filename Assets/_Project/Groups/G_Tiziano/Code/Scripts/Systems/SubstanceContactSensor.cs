using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Detects player contact with an uncontained puddle and routes damage or danger.
    /// </summary>
    public sealed class SubstanceContactSensor : MonoBehaviour, IResettable
    {
        [SerializeField] private Puddle puddle;
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private PlayerPPEState ppeState;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField, Min(0.1f)] private float burnDurationSeconds = 5f;
        [SerializeField] private AudioClip burnSfx;

        /// <summary>Raised when an unsafe contact applies damage.</summary>
        public event Action<string, float> DamageTaken;
        /// <summary>Raised when Guided mode detects unsafe contact without damage.</summary>
        public event Action<string, float> DangerDetected;
        /// <summary>Gets the optional burn SFX.</summary>
        public AudioClip BurnSfx => burnSfx;

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayerCollider(other))
                EvaluateContact();
        }

        /// <summary>Forces one contact evaluation for debug and automated checks.</summary>
        public void ForceContact()
        {
            EvaluateContact();
        }

        /// <summary>Resets sensor-local state; health reset is owned by PlayerHealth.</summary>
        public void ResetState()
        {
        }

        private void EvaluateContact()
        {
            if (puddle != null && puddle.Contained)
                return;

            bool hasRequiredPpe = ppeState != null &&
                puddle != null &&
                ppeState.HasRequiredSet(puddle.CurrentVariant);

            if (hasRequiredPpe)
            {
                if (director != null)
                    director.RecordAction("substance.contact.safe", true, Dimension.Safety, "chemical.contact.safe");
                UnityEngine.Debug.Log("[CA] Substance contact safe: required PPE set equipped.");
                return;
            }

            if (director != null)
                director.RecordAction("substance.contact", false, Dimension.Safety, "chemical.burn");

            if (director != null &&
                director.SelectedDifficulty == ChemicalAlertDirector.Difficulty.Guided)
            {
                DangerDetected?.Invoke("burn", burnDurationSeconds);
                if (playerHealth != null)
                    playerHealth.ReportDanger("burn", burnDurationSeconds);
                return;
            }

            bool applied = playerHealth != null &&
                playerHealth.TakeDamage("burn", burnDurationSeconds);
            if (applied)
                DamageTaken?.Invoke("burn", burnDurationSeconds);
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null)
                return false;

            if (other.GetComponentInParent<Interaction.ScenarioHandInteractor>() != null)
                return true;

            return other.CompareTag("Player") ||
                other.name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                other.name.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
