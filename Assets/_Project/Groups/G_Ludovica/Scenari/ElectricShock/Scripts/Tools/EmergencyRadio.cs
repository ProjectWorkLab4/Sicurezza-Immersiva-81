using System;
using SI81.Input;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] RD-01: radio per chiamare il 112 (sostituto del prefab radio + dialogo dello scenario 07).
    /// Grilletto puntando la radio sul rack = chiamata al 112 (una volta).
    /// </summary>
    public class EmergencyRadio : ScenarioInteractable
    {
        /// <summary>true dopo la chiamata.</summary>
        public bool HasCalled { get; private set; }
        /// <summary>Chiamata al 112 effettuata.</summary>
        public event Action Called;

        /// <summary>Grilletto: chiama il 112.</summary>
        /// <param name="hand">Mano.</param>
        protected override void OnSelectStart(Hand hand)
        {
            if (!HasCalled) PlayerLocator.Pulse(hand, 0.4f, 0.1f);
            Call();
        }

        /// <summary>Chiama il 112 (anche da script o test).</summary>
        public void Call()
        {
            if (HasCalled) return;
            HasCalled = true;
            Called?.Invoke();
        }
    }
}
