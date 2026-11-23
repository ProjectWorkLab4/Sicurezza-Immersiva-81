using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Trigger-like disposal target for used waste sheets.
    /// </summary>
    public sealed class WasteBag : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private CleanupSystem cleanupSystem;
        [SerializeField, Min(0.01f)] private float snapDistance = 0.5f;

        /// <summary>Gets the configured disposal snap distance.</summary>
        public float SnapDistance => snapDistance;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
        }

        /// <summary>Disposes a used waste item released near this bag.</summary>
        public bool TryDispose(WasteItem item)
        {
            if (item == null || !item.IsWaste || item.IsDisposed ||
                Vector3.Distance(item.transform.position, transform.position) > snapDistance)
                return false;

            item.transform.SetParent(transform, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            Rigidbody body = item.GetComponent<Rigidbody>();
            if (body != null)
                body.isKinematic = true;

            item.MarkDisposed();
            if (cleanupSystem != null)
                cleanupSystem.RegisterDisposed(item);
            return true;
        }

        /// <summary>Resets the disposal target state.</summary>
        public void ResetBag()
        {
        }
    }
}
