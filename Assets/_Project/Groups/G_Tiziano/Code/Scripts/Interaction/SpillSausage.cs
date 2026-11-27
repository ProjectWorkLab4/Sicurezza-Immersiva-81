using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Systems;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Grabbable spill sausage that snaps to a nearby perimeter slot on release.
    /// </summary>
    public sealed class SpillSausage : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private PerimeterSlots perimeterSlots;
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private Transform visual;
        [SerializeField] private Collider puddleCollider;
        [SerializeField, Min(1f)] private float placedVisualScaleFactor = 9f;

        private Transform initialParent;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialVisualScale;
        private bool initialKinematic;
        private bool initialUseGravity;
        private PerimeterSlot placedSlot;

        /// <summary>Gets the current occupying slot.</summary>
        public PerimeterSlot PlacedSlot => placedSlot;
        /// <summary>Gets whether this sausage is placed.</summary>
        public bool IsPlaced => placedSlot != null;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
            if (visual == null)
                visual = transform.Find("Visual");

            initialParent = transform.parent;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialVisualScale = visual == null ? Vector3.one : visual.localScale;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                initialKinematic = body.isKinematic;
                initialUseGravity = body.useGravity;
            }
        }

        private void OnEnable()
        {
            if (interactable != null)
                interactable.Released += HandleReleased;
            if (director != null)
                director.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.Released -= HandleReleased;
            if (director != null)
                director.PhaseChanged -= HandlePhaseChanged;
        }

        /// <summary>Tries to place this sausage in a nearby free slot.</summary>
        /// <param name="ignoreSnapDistance">When true, the nearest free slot is used regardless of its SnapDistance.</param>
        public bool TryAutoPlace(bool ignoreSnapDistance = false)
        {
            if (perimeterSlots == null)
                return false;
            return ignoreSnapDistance
                ? perimeterSlots.TryPlace(this, float.MaxValue)
                : perimeterSlots.TryPlace(this);
        }

        /// <summary>Places the sausage when its collider touches the puddle collider.</summary>
        public bool TryAutoPlaceOnPuddleContact()
        {
            if (IsPlaced)
                return false;

            Collider sausageCollider = GetComponent<Collider>();
            if (sausageCollider == null || puddleCollider == null || !puddleCollider.enabled ||
                !sausageCollider.bounds.Intersects(puddleCollider.bounds))
                return false;

            bool placed = TryAutoPlace(true);
            if (placed)
                UnityEngine.Debug.Log($"[CA] Sausage auto-placed on puddle contact: {name}");
            return placed;
        }

        /// <summary>Attaches this sausage to a perimeter slot.</summary>
        public void AttachToSlot(PerimeterSlot slot)
        {
            placedSlot = slot;
            transform.SetParent(slot.transform, true);
            transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
            if (visual != null)
                visual.localScale = initialVisualScale * placedVisualScaleFactor;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
                body.isKinematic = true;
        }

        /// <summary>Clears the slot reference during removal/reset.</summary>
        public void ClearSlot(PerimeterSlot slot)
        {
            if (placedSlot == slot)
                placedSlot = null;
        }

        /// <summary>Restores this sausage to its kit position.</summary>
        public void ResetState()
        {
            if (placedSlot != null)
                placedSlot.Clear();

            transform.SetParent(initialParent, true);
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            if (visual != null)
                visual.localScale = initialVisualScale;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = initialKinematic;
                body.useGravity = initialUseGravity;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void HandleReleased(ScenarioInteractable source)
        {
            // Contact check first: the player only needs to bring the sausage near the puddle.
            if (TryAutoPlaceOnPuddleContact())
                return;

            TryAutoPlace();
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }
    }
}
