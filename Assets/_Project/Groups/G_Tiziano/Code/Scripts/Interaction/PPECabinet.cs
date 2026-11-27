using UnityEngine;
using UnityEngine.Events;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Instantaneous open/closed state for the PPE cabinet.
    /// </summary>
    public sealed class PPECabinet : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private UnityEvent onOpened = new UnityEvent();
        [SerializeField] private UnityEvent onClosed = new UnityEvent();

        private bool isOpen;

        /// <summary>Gets whether the cabinet is open.</summary>
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

        /// <summary>Toggles the cabinet state immediately.</summary>
        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        /// <summary>Opens the cabinet immediately.</summary>
        public void Open()
        {
            if (isOpen)
                return;

            isOpen = true;
            onOpened.Invoke();
            UnityEngine.Debug.Log($"[CA] PPE cabinet opened: {name}");
        }

        /// <summary>Closes the cabinet immediately.</summary>
        public void Close()
        {
            if (!isOpen)
                return;

            isOpen = false;
            onClosed.Invoke();
            UnityEngine.Debug.Log($"[CA] PPE cabinet closed: {name}");
        }

        /// <summary>Resets the cabinet to closed.</summary>
        public void ResetCabinet()
        {
            isOpen = false;
        }

        private void HandleActivated(ScenarioInteractable source)
        {
            Toggle();
        }
    }
}
