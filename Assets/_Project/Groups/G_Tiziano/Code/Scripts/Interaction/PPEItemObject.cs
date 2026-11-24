using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Systems;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Scene object representing one grabbable PPEItem asset.
    /// </summary>
    public sealed class PPEItemObject : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private PPEItem itemData;
        [SerializeField] private PlayerPPEState ppeState;

        private Transform initialParent;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Renderer[] visualRenderers = new Renderer[0];
        private Collider[] colliders = new Collider[0];
        private bool[] originalRendererStates = new bool[0];
        private bool[] originalColliderStates = new bool[0];
        private bool acquired;

        /// <summary>Gets the data asset represented by this object.</summary>
        public PPEItem ItemData => itemData;
        /// <summary>Gets whether this PPE is marked as a decoy.</summary>
        public bool IsDecoy => itemData != null && itemData.IsDecoy;
        /// <summary>Gets whether this physical copy has already been acquired.</summary>
        public bool IsAcquired => acquired;

        /// <summary>Assigns the PPE data represented by this physical cabinet copy.</summary>
        public void SetItemData(PPEItem value)
        {
            if (value == null)
            {
                UnityEngine.Debug.LogError($"[CA] {name}: cannot assign null PPE data.", this);
                return;
            }

            itemData = value;
            UnityEngine.Debug.Log($"[CA] PPE cabinet copy set: {name} -> {itemData.ItemId}.");
        }

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();

            initialParent = transform.parent;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
            visualRenderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            originalRendererStates = new bool[visualRenderers.Length];
            for (int i = 0; i < visualRenderers.Length; i++)
                originalRendererStates[i] = visualRenderers[i] != null && visualRenderers[i].enabled;
            originalColliderStates = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
                originalColliderStates[i] = colliders[i] != null && colliders[i].enabled;
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.Grabbed += HandleGrabbed;
                interactable.Released += HandleReleased;
            }
            if (director != null)
                director.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Grabbed -= HandleGrabbed;
                interactable.Released -= HandleReleased;
            }
            if (director != null)
                director.PhaseChanged -= HandlePhaseChanged;
        }

        /// <summary>Restores this cabinet copy's original position, visual and interaction for a retry.</summary>
        public void ResetState()
        {
            acquired = false;
            transform.SetParent(initialParent, true);
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            SetAcquiredVisual(false);
            if (interactable != null)
                interactable.SetInteractionEnabled(true);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (interactable != null)
                interactable.SetInteractionEnabled(true);
        }

        private void HandleReleased(ScenarioInteractable source)
        {
            if (!acquired)
                return;

            transform.SetParent(initialParent, true);
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void HandleGrabbed(ScenarioInteractable source)
        {
            if (acquired)
                return;

            if (ppeState == null)
            {
                UnityEngine.Debug.LogError($"[CA] {name}: PlayerPPEState reference is missing.", this);
                return;
            }

            ppeState.TryAcquire(this);
        }

        /// <summary>Marks this copy as collected and hides its model/collider.</summary>
        public void MarkAcquired()
        {
            acquired = true;
            SetAcquiredVisual(true);
        }

        /// <summary>Hides or restores this physical copy's visual and colliders after acquisition/reset.</summary>
        public void SetAcquiredVisual(bool hidden)
        {
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                if (visualRenderers[i] != null)
                    visualRenderers[i].enabled = hidden ? false : (i < originalRendererStates.Length && originalRendererStates[i]);
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = hidden ? false : (i < originalColliderStates.Length && originalColliderStates[i]);
            }

            if (interactable != null)
                interactable.SetInteractionEnabled(!hidden);
        }

        /// <summary>Disables this duplicate copy while keeping its shelf visual visible.</summary>
        public void DisableInteractionAsDuplicate()
        {
            if (interactable != null)
                interactable.SetInteractionEnabled(false);
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }

        private void OnValidate()
        {
            if (itemData == null)
                UnityEngine.Debug.LogWarning($"[CA] {name}: PPEItem asset is not assigned.", this);
        }
    }
}
