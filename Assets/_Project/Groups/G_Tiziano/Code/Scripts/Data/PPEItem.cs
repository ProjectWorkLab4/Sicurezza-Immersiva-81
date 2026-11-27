using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Data
{
    /// <summary>
    /// Equipment data used by the Chemical Alert PPE cabinet.
    /// </summary>
    [CreateAssetMenu(fileName = "PPEItem", menuName = "SI81/Chemical Alert/PPE Item")]
    public sealed class PPEItem : ScriptableObject
    {
        /// <summary>Supported PPE equipment slots.</summary>
        public enum Slot
        {
            Head,
            Torso,
            Hands
        }

        [SerializeField] private string itemId;
        [SerializeField] private string textKey;
        [SerializeField] private Slot slot;
        [SerializeField] private bool isDecoy;

        /// <summary>Gets the stable item identifier.</summary>
        public string ItemId => itemId;
        /// <summary>Gets the localization key for this item.</summary>
        public string TextKey => textKey;
        /// <summary>Gets the equipment slot.</summary>
        public Slot EquipmentSlot => slot;
        /// <summary>Gets whether this item is a decoy.</summary>
        public bool IsDecoy => isDecoy;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
                UnityEngine.Debug.LogWarning($"[CA] {name}: itemId is empty.", this);

            if (string.IsNullOrWhiteSpace(textKey))
                UnityEngine.Debug.LogWarning($"[CA] {name}: textKey is empty.", this);
        }
    }
}
