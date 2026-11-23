using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Aggregates equipped PPE and validates a required substance set.
    /// </summary>
    public sealed class PlayerPPEState : MonoBehaviour
    {
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private PPEEquipZone[] zones = new PPEEquipZone[0];
        [SerializeField] private PPEItemObject[] debugCorrectSet = new PPEItemObject[0];
        [SerializeField] private PPEItemObject[] debugDecoySet = new PPEItemObject[0];
        [SerializeField] private SubstanceVariant debugVariant;

        /// <summary>Gets all configured equipment zones.</summary>
        public PPEEquipZone[] Zones => zones;
        /// <summary>Gets the number of currently equipped items.</summary>
        public int EquippedCount
        {
            get
            {
                int count = 0;
                if (zones == null) return count;
                foreach (PPEEquipZone zone in zones)
                    if (zone != null && zone.CurrentItem != null) count++;
                return count;
            }
        }

        private void Awake()
        {
            if (director == null)
                director = GetComponent<ChemicalAlertDirector>();
        }

        private void OnEnable()
        {
            if (director != null)
                director.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (director != null)
                director.PhaseChanged -= HandlePhaseChanged;
        }

        /// <summary>Returns true only when every required PPE asset is equipped.</summary>
        public bool HasRequiredSet(SubstanceVariant variant)
        {
            if (variant == null || variant.RequiredPpe == null || variant.RequiredPpe.Length == 0)
                return false;

            foreach (PPEItem required in variant.RequiredPpe)
            {
                bool found = false;
                if (zones != null)
                {
                    foreach (PPEEquipZone zone in zones)
                    {
                        if (zone != null && zone.CurrentItem != null &&
                            zone.CurrentItem.ItemData == required)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        /// <summary>Clears every equipped zone.</summary>
        public void ResetState()
        {
            if (zones == null)
                return;

            foreach (PPEEquipZone zone in zones)
                if (zone != null) zone.ResetState();
        }

        /// <summary>Equips the configured correct debug set without distance checks.</summary>
        public void DebugEquipCorrectSet()
        {
            DebugEquipSet(debugCorrectSet);
        }

        /// <summary>Equips the configured decoy debug set without distance checks.</summary>
        public void DebugEquipDecoySet()
        {
            DebugEquipSet(debugDecoySet);
        }

        /// <summary>Prints equipped slot and item state through the supplied logger.</summary>
        public void PrintState(System.Action<string> logger)
        {
            if (logger == null)
                return;

            string state = string.Empty;
            if (zones != null)
            {
                foreach (PPEEquipZone zone in zones)
                {
                    if (zone == null) continue;
                    string item = zone.CurrentItem == null ? "Empty" : zone.CurrentItem.ItemData.ItemId;
                    state += $"{zone.Slot}={item}; ";
                }
            }
            bool requiredSet = debugVariant != null && HasRequiredSet(debugVariant);
            logger($"[CA] PPE state: equipped={EquippedCount}; requiredSet({(debugVariant == null ? "None" : debugVariant.VariantId)})={requiredSet}; {state}");
        }

        private void DebugEquipSet(PPEItemObject[] items)
        {
            ResetState();
            if (items == null) return;

            foreach (PPEItemObject item in items)
            {
                if (item == null || zones == null) continue;
                foreach (PPEEquipZone zone in zones)
                {
                    if (zone != null && zone.DebugEquip(item))
                        break;
                }
            }
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }
    }
}
