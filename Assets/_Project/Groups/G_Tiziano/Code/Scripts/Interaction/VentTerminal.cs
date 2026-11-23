using System;
using UnityEngine;
using UnityEngine.Events;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Interactable industrial ventilation terminal.
    /// </summary>
    public sealed class VentTerminal : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private UnityEvent onTurnedOn = new UnityEvent();

        private bool isOn;

        /// <summary>Raised once when the terminal turns on.</summary>
        public event Action TurnedOn;
        /// <summary>Gets whether this terminal is currently on.</summary>
        public bool IsOn => isOn;
        /// <summary>Gets the UnityEvent hook for future visual/audio work.</summary>
        public UnityEvent OnTurnedOn => onTurnedOn;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
        }

        private void OnEnable()
        {
            if (interactable != null)
                interactable.Activated += HandleActivated;
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.Activated -= HandleActivated;
        }

        /// <summary>Turns the terminal on without requiring controller input.</summary>
        public void TurnOn()
        {
            if (isOn)
                return;

            isOn = true;
            TurnedOn?.Invoke();
            onTurnedOn.Invoke();
            UnityEngine.Debug.Log($"[CA] Ventilation turned on: {name}");
        }

        /// <summary>Resets the terminal to its off state.</summary>
        public void ResetTerminal()
        {
            isOn = false;
        }

        private void HandleActivated(ScenarioInteractable source)
        {
            TurnOn();
        }
    }
}
