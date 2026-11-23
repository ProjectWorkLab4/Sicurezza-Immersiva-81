using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Systems;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Safe-zone trigger that resolves the scenario when the player exits.
    /// </summary>
    public sealed class ExitSafeZone : MonoBehaviour
    {
        [SerializeField] private ChemicalAlertDirector director;

        /// <summary>Raised when a player exit is detected.</summary>
        public event Action ExitedSafe;

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayerCollider(other))
                HandleExit();
        }

        /// <summary>Forces an exit for debug and automated checks.</summary>
        public void ForceExit()
        {
            HandleExit();
        }

        private void HandleExit()
        {
            ExitedSafe?.Invoke();
            if (director != null)
                director.HandleExitSafe();
            UnityEngine.Debug.Log($"[CA] ExitSafe reached: {name}");
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null)
                return false;

            if (other.GetComponentInParent<ScenarioHandInteractor>() != null)
                return true;

            return other.CompareTag("Player") ||
                other.name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                other.name.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
