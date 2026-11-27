using UnityEngine;
using UnityEngine.Events;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Instantaneous open/closed state for the spill kit case.
    /// </summary>
    public sealed class SpillKit : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private UnityEvent onOpened = new UnityEvent();
        [SerializeField] private UnityEvent onClosed = new UnityEvent();

        private bool isOpen;

        /// <summary>Gets whether the kit is open.</summary>
        public bool IsOpen => isOpen;
        /// <summary>Gets the UnityEvent hook raised when opening.</summary>
        public UnityEvent OnOpened => onOpened;
        /// <summary>Gets the UnityEvent hook raised when closing.</summary>
        public UnityEvent OnClosed => onClosed;

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

        /// <summary>Toggles the spill kit immediately.</summary>
        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        /// <summary>Opens the kit immediately.</summary>
        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            onOpened.Invoke();
            UnityEngine.Debug.Log($"[CA] Spill kit opened: {name}");
        }

        /// <summary>Closes the kit immediately.</summary>
        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            onClosed.Invoke();
            UnityEngine.Debug.Log($"[CA] Spill kit closed: {name}");
        }

        /// <summary>Resets the kit to closed.</summary>
        public void ResetKit()
        {
            isOpen = false;
        }

        private void HandleActivated(ScenarioInteractable source)
        {
            Toggle();
        }
    }
}
