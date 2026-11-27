using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Tracks acquired PPE protections and validates required substance protection.
    /// </summary>
    public sealed class PlayerPPEState : MonoBehaviour
    {
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private PPEItemObject[] debugCorrectSet = new PPEItemObject[0];
        [SerializeField] private PPEItemObject[] debugDecoySet = new PPEItemObject[0];
        [SerializeField] private SubstanceVariant debugVariant;
        [SerializeField] private PPEItemObject[] cabinetItems = new PPEItemObject[0];
        [SerializeField] private PPEItem maskItem;
        [SerializeField] private PPEItem glovesItem;
        [SerializeField] private PPEItem eyeProtectionItem;

        private readonly HashSet<PPEItem> acquiredItems = new HashSet<PPEItem>();

        /// <summary>Gets the number of distinct PPE types acquired in the current scenario.</summary>
        public int AcquiredCount => acquiredItems.Count;
        /// <summary>Gets whether the respiratory mask has been acquired.</summary>
        public bool HasMask => HasItem(maskItem);
        /// <summary>Gets whether protective gloves have been acquired.</summary>
        public bool HasGloves => HasItem(glovesItem);
        /// <summary>Gets whether eye protection has been acquired.</summary>
        public bool HasEyeProtection => HasItem(eyeProtectionItem);
        /// <summary>Gets whether all three required PPE types have been acquired.</summary>
        public bool HasAllRequiredProtection => HasMask && HasGloves && HasEyeProtection;
        /// <summary>Compatibility property for debug tooling; counts acquired PPE items.</summary>
        public int EquippedCount => AcquiredCount;

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
                if (required == null || !acquiredItems.Contains(required))
                    return false;
            }

            return true;
        }

        /// <summary>Acquires one PPE type and disables interaction on its duplicate cabinet copy.</summary>
        public bool TryAcquire(PPEItemObject source)
        {
            if (source == null || source.ItemData == null)
                return false;

            if (!acquiredItems.Add(source.ItemData))
            {
                source.DisableInteractionAsDuplicate();
                return false;
            }

            source.MarkAcquired();
            if (cabinetItems != null)
            {
                foreach (PPEItemObject duplicate in cabinetItems)
                {
                    if (duplicate != null && duplicate != source && duplicate.ItemData == source.ItemData)
                        duplicate.DisableInteractionAsDuplicate();
                }
            }

            if (director != null)
            {
                director.RecordAction(
                    $"ppe.acquire.{source.ItemData.ItemId}",
                    true,
                    Dimension.Safety,
                    "chemical.ppe.acquired");
                director.NotifyPpeAcquired(source.ItemData);
            }

            UnityEngine.Debug.Log($"[CA] PPE acquired: {source.ItemData.ItemId}; count={AcquiredCount}/3.");
            return true;
        }

        /// <summary>Returns whether the specified PPE data asset has been acquired.</summary>
        public bool HasItem(PPEItem item)
        {
            return item != null && acquiredItems.Contains(item);
        }

        /// <summary>Clears acquisition state and restores all cabinet copies for a retry.</summary>
        public void ResetState()
        {
            acquiredItems.Clear();

            if (cabinetItems != null)
            {
                foreach (PPEItemObject item in cabinetItems)
                    if (item != null) item.ResetState();
            }
        }

        /// <summary>Acquires the required PPE types without physical controller input for debug checks.</summary>
        public void DebugEquipCorrectSet()
        {
            if (debugVariant == null)
            {
                UnityEngine.Debug.LogError("[CA] PPE debug: no SubstanceVariant assigned.");
                return;
            }

            PPEItem[] required = debugVariant.RequiredPpe;
            foreach (PPEItem itemData in required)
            {
                PPEItemObject item = FindCabinetItem(itemData);
                if (item != null)
                    TryAcquire(item);
            }
        }

        /// <summary>Reports that decoy PPE is no longer part of the Chemical Alert cabinet.</summary>
        public void DebugEquipDecoySet()
        {
            UnityEngine.Debug.LogWarning("[CA] PPE debug: no decoy set is configured in the current gameplay.");
        }

        /// <summary>Prints the acquired PPE protections through the supplied logger.</summary>
        public void PrintState(System.Action<string> logger)
        {
            if (logger == null)
                return;

            bool requiredSet = debugVariant != null && HasRequiredSet(debugVariant);
            logger($"[CA] PPE state: acquired={AcquiredCount}/3; mask={HasMask}; gloves={HasGloves}; eyes={HasEyeProtection}; requiredSet({(debugVariant == null ? "None" : debugVariant.VariantId)})={requiredSet}");
        }

        private PPEItemObject FindCabinetItem(PPEItem itemData)
        {
            if (itemData == null || cabinetItems == null)
                return null;

            foreach (PPEItemObject item in cabinetItems)
                if (item != null && item.ItemData == itemData && !HasItem(itemData))
                    return item;
            return null;
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }
    }
}
