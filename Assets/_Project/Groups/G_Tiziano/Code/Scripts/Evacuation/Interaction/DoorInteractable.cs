using System;
using UnityEngine;
using UnityEngine.Events;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Interaction;
using SI81.Tiziano.Evacuation.Systems;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Interaction
{
    /// <summary>A rotating part of a door. Closed pose is identity; open pose is <c>openAngle</c> around local Y.</summary>
    [Serializable]
    public sealed class EvacDoorLeaf
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private float openAngle = 90f;

        /// <summary>Applies the open or closed pose instantly.</summary>
        public void Apply(bool open)
        {
            if (pivot != null)
                pivot.localRotation = Quaternion.Euler(0f, open ? openAngle : 0f, 0f);
        }
    }

    /// <summary>
    /// Placeholder door: Activated toggles it instantly. Blocking colliders are disabled while open;
    /// the interaction trigger (child of a pivot) stays active so the door can be closed again.
    /// </summary>
    [RequireComponent(typeof(ScenarioInteractable))]
    public sealed class DoorInteractable : MonoBehaviour, IResettable
    {
        [SerializeField] private EvacuationDirector director;
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private string doorId = "evac.door";
        [SerializeField] private bool startsOpen;

        [Header("Moving parts")]
        [SerializeField] private EvacDoorLeaf[] leaves = new EvacDoorLeaf[0];
        [SerializeField] private Collider[] blockingColliders = new Collider[0];

        [Header("Hooks")]
        [SerializeField] private UnityEvent onOpened = new UnityEvent();
        [SerializeField] private UnityEvent onClosed = new UnityEvent();

        private bool isOpen;

        /// <summary>Raised after every state change with the new open state.</summary>
        public event Action<DoorInteractable, bool> StateChanged;

        /// <summary>Stable identifier of the door.</summary>
        public string DoorId => doorId;
        /// <summary>True while the door is open.</summary>
        public bool IsOpen => isOpen;
        /// <summary>State restored on reset.</summary>
        public bool StartsOpen => startsOpen;
        /// <summary>The interactable that drives the door.</summary>
        public ScenarioInteractable Interactable => interactable;
        /// <summary>Hook invoked when the door opens.</summary>
        public UnityEvent OnOpened => onOpened;
        /// <summary>Hook invoked when the door closes.</summary>
        public UnityEvent OnClosed => onClosed;

        /// <summary>True if at least one blocking collider is enabled.</summary>
        public bool IsBlocking
        {
            get
            {
                for (int i = 0; i < blockingColliders.Length; i++)
                {
                    if (blockingColliders[i] != null && blockingColliders[i].enabled)
                        return true;
                }

                return false;
            }
        }

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            isOpen = startsOpen;
            ApplyState();
        }

        private void OnEnable()
        {
            if (interactable != null)
                interactable.Activated += HandleActivated;
            if (director != null)
                director.RegisterResettable(this);
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.Activated -= HandleActivated;
            if (director != null)
                director.UnregisterResettable(this);
        }

        /// <summary>Opens the door (no effect if already open).</summary>
        public void Open()
        {
            SetOpen(true);
        }

        /// <summary>Closes the door (no effect if already closed).</summary>
        public void Close()
        {
            SetOpen(false);
        }

        /// <summary>Switches between open and closed.</summary>
        public void Toggle()
        {
            SetOpen(!isOpen);
        }

        /// <summary>Sets the door state instantly and raises the hooks when it changes.</summary>
        public void SetOpen(bool open)
        {
            if (isOpen == open)
                return;

            isOpen = open;
            ApplyState();
            UDebug.Log($"[EV] Door {doorId} {(open ? "opened" : "closed")}");
            (open ? onOpened : onClosed).Invoke();
            StateChanged?.Invoke(this, open);
        }

        /// <summary>IResettable: restores the initial state.</summary>
        public void ResetState()
        {
            SetOpen(startsOpen);
            ApplyState();
        }

        private void HandleActivated(ScenarioInteractable source)
        {
            Toggle();
        }

        private void ApplyState()
        {
            for (int i = 0; i < leaves.Length; i++)
                leaves[i]?.Apply(isOpen);

            for (int i = 0; i < blockingColliders.Length; i++)
            {
                if (blockingColliders[i] != null)
                    blockingColliders[i].enabled = !isOpen;
            }
        }
    }
}
