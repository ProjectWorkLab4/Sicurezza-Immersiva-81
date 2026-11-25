using SI81.Input;
using UnityEngine;

namespace SI81.Rig.Tests
{
    /// <summary>
    /// Adattatore UI finto per i test: registra l'hand-off, lo stato del raggio UI per mano e gli impulsi aptici.
    /// Non referenzia la UI comune né il Meta SDK.
    /// </summary>
    public sealed class StubRigUIBridge : MonoBehaviour, IRigUIBridge
    {
        /// <summary>Ultimo hand-off ricevuto.</summary>
        public RigHandoff Handoff { get; private set; }
        /// <summary>Numero di hand-off ricevuti.</summary>
        public int ApplyCount { get; private set; }
        /// <summary>Stato "punta la UI" per mano (0 = sinistra, 1 = destra).</summary>
        public readonly bool[] Pointing = new bool[2];
        /// <summary>Numero di impulsi ricevuti.</summary>
        public int PulseCount { get; private set; }
        /// <summary>Ultima mano dell'impulso.</summary>
        public Hand LastPulseHand { get; private set; }
        /// <summary>Ultima ampiezza dell'impulso.</summary>
        public float LastPulseAmplitude { get; private set; }
        /// <summary>Ultima durata dell'impulso.</summary>
        public float LastPulseDuration { get; private set; }

        /// <summary>Registra l'hand-off.</summary>
        /// <param name="handoff">Riferimenti del rig.</param>
        public void ApplyRig(RigHandoff handoff)
        {
            Handoff = handoff;
            ApplyCount++;
        }

        /// <summary>Riporta lo stato impostato dal test.</summary>
        /// <param name="hand">Mano singola.</param>
        public bool IsPointingAtUI(Hand hand)
        {
            return hand == Hand.Left || hand == Hand.Right
                ? Pointing[hand == Hand.Left ? 0 : 1]
                : false;
        }

        /// <summary>Registra l'impulso.</summary>
        /// <param name="hand">Mano.</param>
        /// <param name="amplitude">Ampiezza.</param>
        /// <param name="duration">Durata.</param>
        public void Pulse(Hand hand, float amplitude, float duration)
        {
            PulseCount++;
            LastPulseHand = hand;
            LastPulseAmplitude = amplitude;
            LastPulseDuration = duration;
        }
    }
}
