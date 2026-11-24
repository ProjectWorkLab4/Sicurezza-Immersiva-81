using System;
using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] RD-01: radio per chiamare il 112 (sostituto del prefab radio + dialogo dello scenario 07).
    /// Si prende dal rack RK-01; grilletto = chiama il 112 (una volta).
    /// </summary>
    public class EmergencyRadio : TempInteractable
    {
        /// <summary>true dopo la chiamata.</summary>
        public bool HasCalled { get; private set; }
        /// <summary>Chiamata al 112 effettuata.</summary>
        public event Action Called;

        /// <summary>true: si può afferrare.</summary>
        public override bool IsGrabbable => true;

        /// <summary>Grilletto: chiama il 112.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseStart(TempHand hand) => Call();

        /// <summary>Lasciata: si appoggia a terra.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnRelease(TempHand hand)
        {
            base.OnRelease(hand);
            DropUpright();
        }

        /// <summary>Posa nella mano.</summary>
        public override void GetHoldPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = new Vector3(0f, -0.1f, 0f);
            localRotation = Quaternion.identity;
        }

        /// <summary>Chiama il 112 (anche da script o test).</summary>
        public void Call()
        {
            if (HasCalled) return;
            HasCalled = true;
            ElectricShockHud.Pulse(0.4f, 0.1f);
            Called?.Invoke();
        }
    }
}
