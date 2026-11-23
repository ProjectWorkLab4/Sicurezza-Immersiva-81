using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Systems;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Grabbable absorbent sheet that can be placed on the puddle.
    /// </summary>
    public sealed class AbsorbentSheet : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private CleanupSystem cleanupSystem;
        [SerializeField] private WasteItem wasteItem;
        [SerializeField] private ChemicalAlertDirector director;

        private Transform initialParent;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool placed;

        /// <summary>Gets whether this sheet is currently placed on the puddle.</summary>
        public bool IsPlaced => placed;
        /// <summary>Gets the used waste state.</summary>
        public WasteItem Waste => wasteItem;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
            if (wasteItem == null)
                wasteItem = GetComponent<WasteItem>();

            initialParent = transform.parent;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
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

        /// <summary>Attaches this sheet to the puddle as used waste.</summary>
        public void AttachToPuddle(Transform puddleTransform, Vector3 localPosition)
        {
            placed = true;
            transform.SetParent(puddleTransform, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
                body.isKinematic = true;
            if (wasteItem != null)
                wasteItem.MarkWaste();
        }

        /// <summary>Restores this sheet to its original kit position.</summary>
        public void ResetState()
        {
            placed = false;
            if (wasteItem != null)
                wasteItem.ResetState();

            transform.SetParent(initialParent, true);
            transform.SetPositionAndRotation(initialPosition, initialRotation);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void HandleReleased(ScenarioInteractable source)
        {
            if (cleanupSystem != null)
                cleanupSystem.TryPlaceSheet(this);
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }
    }
}
