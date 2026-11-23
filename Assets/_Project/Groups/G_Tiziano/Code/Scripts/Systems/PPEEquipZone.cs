using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Slot zone that snaps a released compatible PPE item into place.
    /// </summary>
    public sealed class PPEEquipZone : MonoBehaviour, IResettable
    {
        [SerializeField] private PPEItem.Slot slot;
        [SerializeField, Min(0.01f)] private float snapDistance = 0.35f;
        [SerializeField] private ChemicalAlertDirector director;

        private PPEItemObject currentItem;

        /// <summary>Gets the slot accepted by this zone.</summary>
        public PPEItem.Slot Slot => slot;
        /// <summary>Gets the item currently equipped in this zone.</summary>
        public PPEItemObject CurrentItem => currentItem;
        /// <summary>Gets the configured snap distance.</summary>
        public float SnapDistance => snapDistance;

        /// <summary>Tries to equip a compatible item released near this zone.</summary>
        public bool TryEquip(PPEItemObject item)
        {
            if (item == null || item.ItemData == null ||
                item.ItemData.EquipmentSlot != slot ||
                Vector3.Distance(item.transform.position, transform.position) > snapDistance)
                return false;

            if (currentItem != null && currentItem != item)
                return false;

            currentItem = item;
            item.EquipToZone(this);

            bool isCorrectChoice = !item.IsDecoy;
            if (director != null)
            {
                director.RecordAction(
                    $"ppe.equip.{slot}",
                    isCorrectChoice,
                    Dimension.Decision,
                    isCorrectChoice ? "chemical.ppe.equipped" : "chemical.ppe.decoy");
                director.RecordAction(
                    $"ppe.slot.{slot}",
                    true,
                    Dimension.Safety,
                    "chemical.ppe.slot");
            }

            UnityEngine.Debug.Log($"[CA] PPE equipped: {item.ItemData.ItemId} -> {slot}; decoy={item.IsDecoy}");
            return true;
        }

        /// <summary>Equips a compatible item for editor/development debug checks.</summary>
        public bool DebugEquip(PPEItemObject item)
        {
            if (item == null || item.ItemData == null ||
                item.ItemData.EquipmentSlot != slot)
                return false;

            if (currentItem != null && currentItem != item)
                RemoveItem(currentItem);

            currentItem = item;
            item.EquipToZone(this);

            bool isCorrectChoice = !item.IsDecoy;
            if (director != null)
            {
                director.RecordAction(
                    $"ppe.equip.{slot}",
                    isCorrectChoice,
                    Dimension.Decision,
                    isCorrectChoice ? "chemical.ppe.equipped" : "chemical.ppe.decoy");
                director.RecordAction(
                    $"ppe.slot.{slot}",
                    true,
                    Dimension.Safety,
                    "chemical.ppe.slot");
            }

            UnityEngine.Debug.Log($"[CA] PPE debug equipped: {item.ItemData.ItemId} -> {slot}; decoy={item.IsDecoy}");
            return true;
        }

        /// <summary>Removes the current item from this zone.</summary>
        public void RemoveItem(PPEItemObject item)
        {
            if (item == null || currentItem != item)
                return;

            currentItem = null;
            item.ClearEquippedZone(this);
            item.transform.SetParent(null, true);

            Rigidbody body = item.GetComponent<Rigidbody>();
            if (body != null)
                body.isKinematic = false;

            UnityEngine.Debug.Log($"[CA] PPE unequipped: {item.ItemData.ItemId} from {slot}");
        }

        /// <summary>Clears the slot during scenario reset.</summary>
        public void ResetState()
        {
            if (currentItem != null)
            {
                PPEItemObject item = currentItem;
                currentItem = null;
                item.ClearEquippedZone(this);
            }
        }
    }
}
