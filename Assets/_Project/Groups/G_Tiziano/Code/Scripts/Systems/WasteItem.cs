using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// State carried by an absorbent sheet after it is used.
    /// </summary>
    public sealed class WasteItem : MonoBehaviour
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private PlayerPPEState ppeState;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Puddle puddle;
        [SerializeField] private WasteBag wasteBag;

        private bool isWaste;
        private bool disposed;

        /// <summary>Gets whether this item is a used waste sheet.</summary>
        public bool IsWaste => isWaste;
        /// <summary>Gets whether this item has been disposed in the bag.</summary>
        public bool IsDisposed => disposed;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.Grabbed += HandleGrabbed;
                interactable.Released += HandleReleased;
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Grabbed -= HandleGrabbed;
                interactable.Released -= HandleReleased;
            }
        }

        /// <summary>Marks the sheet as used waste.</summary>
        public void MarkWaste()
        {
            isWaste = true;
            disposed = false;
        }

        /// <summary>Marks this waste item as safely disposed.</summary>
        public void MarkDisposed()
        {
            disposed = true;
            if (director != null)
                director.RecordAction("waste.dispose", true, Dimension.Execution, "chemical.waste.disposed");
            UnityEngine.Debug.Log($"[CA] Waste disposed: {name}");
        }

        /// <summary>Clears waste and disposal state during reset.</summary>
        public void ResetState()
        {
            isWaste = false;
            disposed = false;
        }

        private void HandleGrabbed(ScenarioInteractable source)
        {
            if (!isWaste || disposed)
                return;

            bool safe = ppeState != null && puddle != null &&
                ppeState.HasRequiredSet(puddle.CurrentVariant);
            if (director != null)
                director.RecordAction(
                    "waste.collect",
                    safe,
                    Dimension.Execution,
                    safe ? "chemical.waste.collect" : "chemical.burn");

            if (!safe && playerHealth != null)
                playerHealth.TakeDamage("burn", 5f);
            UnityEngine.Debug.Log($"[CA] Waste grabbed: {name}; safe={safe}");
        }

        private void HandleReleased(ScenarioInteractable source)
        {
            if (isWaste && !disposed && wasteBag != null)
                wasteBag.TryDispose(this);
        }
    }
}
