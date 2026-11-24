using System;
using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>PB-01: pulsante di arresto di emergenza del generatore. Si preme con il grilletto (toccandolo o puntandolo).</summary>
    public class EmergencyStopButton : TempInteractable
    {
        [Tooltip("Parte del pulsante che si abbassa quando premuto.")]
        [SerializeField] private Transform cap;

        private bool pressed;

        /// <summary>Chiamato alla pressione (una sola volta).</summary>
        public event Action Pressed;
        /// <summary>true dopo la pressione.</summary>
        public bool IsPressed => pressed;

        /// <summary>Grilletto sul pulsante: arresto di emergenza.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseStart(TempHand hand) => Press();

        /// <summary>Preme il pulsante (anche da script o test).</summary>
        public void Press()
        {
            if (pressed) return;
            pressed = true;
            if (cap != null) cap.localPosition += Vector3.down * 0.02f;
            ElectricShockHud.Pulse(0.7f, 0.15f);
            Pressed?.Invoke();
        }
    }
}
