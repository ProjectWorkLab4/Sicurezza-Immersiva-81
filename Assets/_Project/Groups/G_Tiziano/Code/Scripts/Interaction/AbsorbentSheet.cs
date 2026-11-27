using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Systems;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Grabbable absorbent sheet that is placed on and stains across the puddle.
    /// </summary>
    public sealed class AbsorbentSheet : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private CleanupSystem cleanupSystem;
        [SerializeField] private ChemicalAlertDirector director;

        private Transform initialParent;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool initialKinematic;
        private bool initialUseGravity;
        private bool placed;
        private bool absorbed;
        private bool initialized;
        private Renderer[] renderers = new Renderer[0];
        private MaterialPropertyBlock colorPropertyBlock;

        /// <summary>Gets whether this sheet is currently placed on the puddle.</summary>
        public bool IsPlaced => placed;
        /// <summary>Gets whether the sheet has completed absorption and changed color.</summary>
        public bool IsAbsorbed => absorbed;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Captures the scene home state. Reset can run before Awake on prefab instances, so initialize
        /// lazily to avoid restoring a zero transform and making the sheet impossible to reach.
        /// </summary>
        private void EnsureInitialized()
        {
            if (initialized)
                return;

            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();

            initialParent = transform.parent;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                initialKinematic = body.isKinematic;
                initialUseGravity = body.useGravity;
            }
            renderers = GetComponentsInChildren<Renderer>(true);
            initialized = true;
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

        /// <summary>Places this sheet over the puddle.</summary>
        public void AttachToPuddle(Transform puddleTransform, Vector3 localPosition)
        {
            placed = true;
            Vector3 worldPosition = puddleTransform == null
                ? transform.position
                : puddleTransform.TransformPoint(localPosition);
            Quaternion worldRotation = puddleTransform == null
                ? transform.rotation
                : puddleTransform.rotation;
            Transform stableParent = puddleTransform == null ? initialParent : puddleTransform.parent;
            transform.SetParent(stableParent, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
                body.isKinematic = true;
            if (interactable != null)
                interactable.SetInteractionEnabled(false);
        }

        /// <summary>Changes this sheet's visual color as the puddle is absorbed.</summary>
        public void SetAbsorbed(bool value)
        {
            absorbed = value;
            SetAbsorptionProgress(value ? 1f : 0f);
            Collider itemCollider = GetComponent<Collider>();
            if (itemCollider != null)
                itemCollider.enabled = !value;
            if (interactable != null)
                interactable.SetInteractionEnabled(!value);
        }

        /// <summary>Gradually stains a placed sheet during the absorption animation.</summary>
        public void SetAbsorptionProgress(float progress)
        {
            if (!placed)
                return;

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
            if (colorPropertyBlock == null)
                colorPropertyBlock = new MaterialPropertyBlock();

            Color color = Color.Lerp(Color.white, new Color32(144, 120, 44, 255), Mathf.Clamp01(progress));
            foreach (Renderer target in renderers)
            {
                if (target == null)
                    continue;
                target.GetPropertyBlock(colorPropertyBlock);
                colorPropertyBlock.SetColor("_BaseColor", color);
                colorPropertyBlock.SetColor("_Color", color);
                target.SetPropertyBlock(colorPropertyBlock);
            }
        }

        /// <summary>Restores this sheet to its original kit position.</summary>
        public void ResetState()
        {
            EnsureInitialized();
            placed = false;
            absorbed = false;
            ResetVisualColor();

            transform.SetParent(initialParent, true);
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            Collider itemCollider = GetComponent<Collider>();
            if (itemCollider != null)
                itemCollider.enabled = true;
            if (interactable != null)
                interactable.SetInteractionEnabled(true);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = initialKinematic;
                body.useGravity = initialUseGravity;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void ResetVisualColor()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
            if (colorPropertyBlock == null)
                colorPropertyBlock = new MaterialPropertyBlock();

            foreach (Renderer target in renderers)
            {
                if (target == null)
                    continue;
                target.GetPropertyBlock(colorPropertyBlock);
                colorPropertyBlock.SetColor("_BaseColor", Color.white);
                colorPropertyBlock.SetColor("_Color", Color.white);
                target.SetPropertyBlock(colorPropertyBlock);
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
