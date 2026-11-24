using System;
using System.Collections.Generic;
using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using UnityEngine;
using UnityEngine.Events;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Minimal event-driven interaction surface for scenario objects. It is an adapter over the shared
    /// <see cref="Interactable"/>: the rig's <see cref="RayInteractor"/> raises hover/select/grab, and this
    /// component forwards them to the scenario's own events.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class ScenarioInteractable : MonoBehaviour
    {
        [SerializeField] private bool grabbable = true;
        [SerializeField] private bool activatable = true;
        [SerializeField] private bool inspectable = true;
        [SerializeField] private UnityEvent onGrabbed = new UnityEvent();
        [SerializeField] private UnityEvent onReleased = new UnityEvent();
        [SerializeField] private UnityEvent onActivated = new UnityEvent();
        [SerializeField] private UnityEvent onInspected = new UnityEvent();
        [SerializeField] private UnityEvent onHoverEnter = new UnityEvent();
        [SerializeField] private UnityEvent onHoverExit = new UnityEvent();
        [SerializeField, Min(0.1f)] private float highlightPulseHz = 0.8f;

        private readonly Dictionary<Renderer, MaterialPropertyBlock> originalPropertyBlocks =
            new Dictionary<Renderer, MaterialPropertyBlock>();
        private Renderer[] highlightRenderers;
        private bool guidedHighlightEnabled;
        private Interactable interactable;
        private bool inspectedThisGrab;

        /// <summary>Raised when this object is grabbed.</summary>
        public event Action<ScenarioInteractable> Grabbed;
        /// <summary>Raised when this object is released.</summary>
        public event Action<ScenarioInteractable> Released;
        /// <summary>Raised when this object is activated.</summary>
        public event Action<ScenarioInteractable> Activated;
        /// <summary>Raised when this object is inspected.</summary>
        public event Action<ScenarioInteractable> Inspected;
        /// <summary>Raised when hover starts.</summary>
        public event Action<ScenarioInteractable> HoverEnter;
        /// <summary>Raised when hover ends.</summary>
        public event Action<ScenarioInteractable> HoverExit;

        /// <summary>Gets whether the object can be grabbed.</summary>
        public bool Grabbable => grabbable;
        /// <summary>Gets whether the object can be activated.</summary>
        public bool Activatable => activatable;
        /// <summary>Gets whether the object can be inspected.</summary>
        public bool Inspectable => inspectable;

        /// <summary>
        /// Gets the shared interactable and aligns the flags used by the ray.
        /// </summary>
        private void Awake()
        {
            interactable = GetComponent<Interactable>();
            if (interactable == null) return;
            interactable.Grabbable = grabbable;
            interactable.Selectable = activatable;
        }

        /// <summary>
        /// Subscribes to the shared interaction events.
        /// </summary>
        private void OnEnable()
        {
            if (interactable == null) interactable = GetComponent<Interactable>();
            if (interactable == null) return;
            interactable.HoverEntered += HandleHoverEntered;
            interactable.HoverExited += HandleHoverExited;
            interactable.SelectEntered += HandleSelectEntered;
            interactable.GrabEntered += HandleGrabEntered;
            interactable.GrabExited += HandleGrabExited;
        }

        /// <summary>
        /// Unsubscribes from the shared interaction events.
        /// </summary>
        private void OnDisable()
        {
            if (interactable == null) return;
            interactable.HoverEntered -= HandleHoverEntered;
            interactable.HoverExited -= HandleHoverExited;
            interactable.SelectEntered -= HandleSelectEntered;
            interactable.GrabEntered -= HandleGrabEntered;
            interactable.GrabExited -= HandleGrabExited;
        }

        /// <summary>
        /// Guided highlight plus the "inspect while holding" rule that the shared Interactable does not model.
        /// </summary>
        private void Update()
        {
            if (guidedHighlightEnabled)
                ApplyGuidedHighlight();

            if (!inspectable || inspectedThisGrab || interactable == null || !interactable.IsGrabbed) return;
            RigRegistration rig = RigRegistration.Instance;
            Transform head = rig != null ? rig.Head : null;
            if (head == null) return;
            Vector3 toObject = transform.position - head.position;
            if (toObject.magnitude > 0.35f) return;
            if (Vector3.Dot(head.forward, toObject.normalized) < 0.5f) return;
            inspectedThisGrab = true;
            RaiseInspected();
        }

        private void HandleHoverEntered(Hand hand) => RaiseHoverEnter();
        private void HandleHoverExited(Hand hand) => RaiseHoverExit();
        private void HandleSelectEntered(Hand hand) => RaiseActivated();

        private void HandleGrabEntered(Hand hand)
        {
            inspectedThisGrab = false;
            RaiseGrabbed();
        }

        private void HandleGrabExited(Hand hand)
        {
            inspectedThisGrab = false;
            RaiseReleased();
        }

        /// <summary>Enables or restores this interactable's Guided emission highlight.</summary>
        public void SetGuidedHighlight(bool enabled)
        {
            if (guidedHighlightEnabled == enabled)
                return;

            guidedHighlightEnabled = enabled;
            if (enabled)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(true);
                originalPropertyBlocks.Clear();
                foreach (Renderer target in highlightRenderers)
                {
                    if (target == null)
                        continue;

                    MaterialPropertyBlock original = new MaterialPropertyBlock();
                    target.GetPropertyBlock(original);
                    originalPropertyBlocks[target] = original;
                }
                ApplyGuidedHighlight();
                return;
            }

            foreach (KeyValuePair<Renderer, MaterialPropertyBlock> item in originalPropertyBlocks)
            {
                if (item.Key != null)
                    item.Key.SetPropertyBlock(item.Value);
            }
            originalPropertyBlocks.Clear();
            highlightRenderers = null;
        }

        /// <summary>Raises the grab events.</summary>
        public void RaiseGrabbed()
        {
            Grabbed?.Invoke(this);
            onGrabbed.Invoke();
            UnityEngine.Debug.Log($"[CA] Grabbed: {name}");
        }

        /// <summary>Raises the release events.</summary>
        public void RaiseReleased()
        {
            Released?.Invoke(this);
            onReleased.Invoke();
            UnityEngine.Debug.Log($"[CA] Released: {name}");
        }

        /// <summary>Raises the activation events.</summary>
        public void RaiseActivated()
        {
            if (!activatable) return;
            Activated?.Invoke(this);
            onActivated.Invoke();
            UnityEngine.Debug.Log($"[CA] Activated: {name}");
        }

        /// <summary>Raises the inspection events.</summary>
        public void RaiseInspected()
        {
            if (!inspectable) return;
            Inspected?.Invoke(this);
            onInspected.Invoke();
            UnityEngine.Debug.Log($"[CA] Inspected: {name}");
        }

        /// <summary>Raises the hover-enter events.</summary>
        public void RaiseHoverEnter()
        {
            HoverEnter?.Invoke(this);
            onHoverEnter.Invoke();
            UnityEngine.Debug.Log($"[CA] HoverEnter: {name}");
        }

        /// <summary>Raises the hover-exit events.</summary>
        public void RaiseHoverExit()
        {
            HoverExit?.Invoke(this);
            onHoverExit.Invoke();
            UnityEngine.Debug.Log($"[CA] HoverExit: {name}");
        }

        private void ApplyGuidedHighlight()
        {
            if (highlightRenderers == null)
                return;

            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * highlightPulseHz * Mathf.PI * 2f);
            Color emission = (Color)new Color32(230, 159, 0, 255) * pulse;
            foreach (Renderer target in highlightRenderers)
            {
                if (target == null)
                    continue;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                target.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", emission);
                target.SetPropertyBlock(block);
            }
        }
    }
}
