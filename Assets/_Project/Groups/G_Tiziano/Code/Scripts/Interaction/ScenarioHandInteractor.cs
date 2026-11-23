using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Provisional near-controller interactor using the centralized scenario bindings.
    /// </summary>
    public sealed class ScenarioHandInteractor : MonoBehaviour
    {
        [SerializeField] private ScenarioInputBindings inputBindings;
        [SerializeField] private Transform headAnchor;
        [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.LTouch;
        [SerializeField, Min(0.01f)] private float hoverRadius = 0.12f;
        [SerializeField, Min(0.01f)] private float inspectDistance = 0.35f;
        [SerializeField, Range(-1f, 1f)] private float inspectLookDot = 0.5f;

        private ScenarioInteractable hovered;
        private ScenarioInteractable held;
        private Transform heldOriginalParent;
        private Vector3 previousPosition;
        private Vector3 releaseVelocity;

        /// <summary>Gets the currently hovered object.</summary>
        public ScenarioInteractable Hovered => hovered;

        /// <summary>Gets the currently held object.</summary>
        public ScenarioInteractable Held => held;

        private void Update()
        {
            UpdateReleaseVelocity();
            UpdateHover();

            if (inputBindings == null) return;

            if (held == null && inputBindings.GetGripDown(controller))
                TryGrab();

            if (held != null && inputBindings.GetGripUp(controller))
                Release();

            if (held == null && inputBindings.GetTriggerDown(controller) && hovered != null)
                hovered.RaiseActivated();

            TryInspect();
        }

        private void UpdateReleaseVelocity()
        {
            releaseVelocity = (transform.position - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            previousPosition = transform.position;
        }

        private void UpdateHover()
        {
            ScenarioInteractable next = FindNearestInteractable();
            if (next == hovered) return;

            if (hovered != null)
                hovered.RaiseHoverExit();

            hovered = next;

            if (hovered != null)
                hovered.RaiseHoverEnter();
        }

        private ScenarioInteractable FindNearestInteractable()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, hoverRadius);
            ScenarioInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                ScenarioInteractable candidate = collider.GetComponentInParent<ScenarioInteractable>();
                if (candidate == null || !candidate.Grabbable && !candidate.Activatable && !candidate.Inspectable)
                    continue;

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void TryGrab()
        {
            if (hovered == null || !hovered.Grabbable) return;

            held = hovered;
            held.RaiseGrabbed();
            heldOriginalParent = held.transform.parent;
            Rigidbody body = held.GetComponent<Rigidbody>();
            if (body != null)
                body.isKinematic = true;
            held.transform.SetParent(transform, true);
        }

        private void Release()
        {
            held.transform.SetParent(heldOriginalParent, true);
            Rigidbody body = held.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.linearVelocity = releaseVelocity;
            }

            held.RaiseReleased();
            held = null;
            heldOriginalParent = null;
        }

        private void TryInspect()
        {
            if (held == null || headAnchor == null || !held.Inspectable) return;

            Vector3 toObject = held.transform.position - headAnchor.position;
            if (toObject.magnitude > inspectDistance) return;

            Vector3 direction = toObject.normalized;
            if (Vector3.Dot(headAnchor.forward, direction) >= inspectLookDot)
                held.RaiseInspected();
        }
    }
}
