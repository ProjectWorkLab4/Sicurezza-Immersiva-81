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
        [SerializeField] private PPEEquipZone[] equipZones = new PPEEquipZone[0];

        private Transform initialParent;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private PPEEquipZone equippedZone;

        /// <summary>Gets the data asset represented by this object.</summary>
        public PPEItem ItemData => itemData;
        /// <summary>Gets whether this PPE is marked as a decoy.</summary>
        public bool IsDecoy => itemData != null && itemData.IsDecoy;
        /// <summary>Gets the currently equipped zone, if any.</summary>
        public PPEEquipZone EquippedZone => equippedZone;
        /// <summary>Gets whether this item is currently equipped.</summary>
        public bool IsEquipped => equippedZone != null;

        /// <summary>Assigns the PPE data item placed in this shuffled cabinet slot.</summary>
        public void SetItemData(PPEItem value)
        {
            if (value == null)
            {
                UnityEngine.Debug.LogError($"[CA] {name}: cannot assign null PPE data.", this);
                return;
            }

            if (equippedZone != null)
                equippedZone.RemoveItem(this);

            itemData = value;
            UnityEngine.Debug.Log($"[CA] PPE cabinet slot shuffled: {name} -> {itemData.ItemId}; decoy={itemData.IsDecoy}.");
        }

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();

            initialParent = transform.parent;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
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

        /// <summary>Tries to equip this item in the first compatible nearby zone.</summary>
        public bool TryAutoEquip()
        {
            if (equipZones == null)
                return false;

            foreach (PPEEquipZone zone in equipZones)
            {
                if (zone != null && zone.TryEquip(this))
                    return true;
            }

            return false;
        }

        /// <summary>Attaches this item to a compatible zone.</summary>
        public void EquipToZone(PPEEquipZone zone)
        {
            if (zone == null || itemData == null)
                return;

            if (equippedZone != null && equippedZone != zone)
                equippedZone.RemoveItem(this);

            equippedZone = zone;
            transform.SetParent(zone.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
                body.isKinematic = true;
        }

        /// <summary>Removes this item from its current zone.</summary>
        public void Unequip()
        {
            if (equippedZone != null)
                equippedZone.RemoveItem(this);
        }

        /// <summary>Restores this item to its original scene position.</summary>
        public void ResetState()
        {
            if (equippedZone != null)
                equippedZone.RemoveItem(this);

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

        /// <summary>Clears the owning zone reference during unequip/reset.</summary>
        public void ClearEquippedZone(PPEEquipZone zone)
        {
            if (equippedZone == zone)
                equippedZone = null;
        }

        private void HandleReleased(ScenarioInteractable source)
        {
            TryAutoEquip();
        }

        private void HandleGrabbed(ScenarioInteractable source)
        {
            if (equippedZone != null)
                equippedZone.RemoveItem(this);
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
