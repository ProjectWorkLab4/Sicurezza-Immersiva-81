using System;
using System.Collections.Generic;
using UnityEngine;
using SI81.Rig;
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
        [SerializeField] private AudioClip coughSfx;

        private readonly HashSet<int> playerCollidersInside = new HashSet<int>();
        private bool maskWarningRaised;
        private bool gloveContactRaised;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayerCollider(other) || !playerCollidersInside.Add(other.GetInstanceID()))
                return;

            EvaluateContact();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            playerCollidersInside.Remove(other.GetInstanceID());
            if (playerCollidersInside.Count == 0)
            {
                maskWarningRaised = false;
                gloveContactRaised = false;
            }
        }

        /// <summary>Forces one contact evaluation for debug and automated checks.</summary>
        public void ForceContact()
        {
            EvaluateContact();
        }

        /// <summary>Resets sensor-local state; health reset is owned by PlayerHealth.</summary>
        public void ResetState()
        {
            playerCollidersInside.Clear();
            maskWarningRaised = false;
            gloveContactRaised = false;
        }

        private void EvaluateContact()
        {
            if (puddle != null && puddle.Contained)
                return;

            bool ventilationOn = director != null && director.IsVentilationOn;
            if (!ventilationOn && (ppeState == null || !ppeState.HasMask) && !maskWarningRaised)
            {
                maskWarningRaised = true;
                if (director != null)
                {
                    director.ReportError(
                        "substance.approach.without.mask",
                        "chemical.error.no_mask",
                        Dimension.Safety,
                        false);
                    director.RequestCoughCue();
                }

                if (coughSfx != null)
                    AudioSource.PlayClipAtPoint(coughSfx, transform.position);
            }

            if (ppeState == null || !ppeState.HasGloves)
            {
                if (!gloveContactRaised && director != null)
                {
                    gloveContactRaised = true;
                    director.ReportError(
                        "substance.contact.without.gloves",
                        "chemical.error.gloves",
                        Dimension.Safety,
                        true);
                }
                return;
            }

            if (director != null)
                director.RecordAction("substance.contact.safe", true, Dimension.Safety, "chemical.contact.safe");
            UnityEngine.Debug.Log("[CA] Substance approach safe: gloves acquired.");
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null)
                return false;

            RigRegistration rig = RigRegistration.Instance;
            if (rig != null &&
                ((rig.LeftHand != null && other.transform.IsChildOf(rig.LeftHand)) ||
                 (rig.RightHand != null && other.transform.IsChildOf(rig.RightHand)) ||
                 (rig.Head != null && other.transform.IsChildOf(rig.Head))))
                return true;

            return other.CompareTag("Player") ||
                other.name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                other.name.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
