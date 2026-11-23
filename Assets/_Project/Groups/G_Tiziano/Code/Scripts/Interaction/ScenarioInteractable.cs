using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Minimal event-driven interaction surface for scenario objects.
    /// </summary>
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

        private void Update()
        {
            if (guidedHighlightEnabled)
                ApplyGuidedHighlight();
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
