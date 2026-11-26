using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Manages placement of the single complete spill-containment ring.
    /// </summary>
    public sealed class PerimeterSlots : MonoBehaviour, IResettable
    {
        [SerializeField] private Puddle puddle;
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private SdsItem sdsItem;
        [SerializeField] private PlayerPPEState ppeState;
        [SerializeField] private PerimeterSlot[] slots = new PerimeterSlot[0];
        [SerializeField] private SpillSausage[] sausages = new SpillSausage[0];

        private bool orderErrorLogged;

        /// <summary>Gets whether every perimeter slot is occupied.</summary>
        public bool IsComplete => GetFilledCount() == slots.Length && slots.Length > 0;
        /// <summary>Gets the number of filled slots.</summary>
        public int FilledCount => GetFilledCount();
        /// <summary>Gets the total slot count.</summary>
        public int SlotCount => slots == null ? 0 : slots.Length;

        /// <summary>Tries to snap a released sausage to the nearest free slot.</summary>
        /// <param name="sausage">Sausage to place.</param>
        /// <param name="maxDistanceOverride">Optional snap range override; a negative value uses each slot's SnapDistance.</param>
        public bool TryPlace(SpillSausage sausage, float maxDistanceOverride = -1f)
        {
            if (sausage == null || slots == null || director != null && director.Outcome.HasValue)
                return false;

            PerimeterSlot nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (PerimeterSlot slot in slots)
            {
                if (slot == null || slot.IsOccupied)
                    continue;

                float distance = Vector3.Distance(sausage.transform.position, slot.transform.position);
                float snapDistance = maxDistanceOverride >= 0f ? maxDistanceOverride : slot.SnapDistance;
                if (distance <= snapDistance && distance < nearestDistance)
                {
                    nearest = slot;
                    nearestDistance = distance;
                }
            }

            if (nearest == null)
                return false;

            LogOrderIfNeeded();
            bool hasGloves = ppeState != null && ppeState.HasGloves;
            if (director != null)
            {
                if (hasGloves)
                    director.RecordAction("spill.contain", true, Dimension.Execution, "chemical.spill.contained");
                else
                    director.ReportError(
                        "spill.contain.without.gloves",
                        "chemical.error.gloves",
                        Dimension.Safety,
                        true);
            }

            if (director != null && director.Outcome.HasValue)
                return false;

            nearest.Place(sausage);
            UnityEngine.Debug.Log($"[CA] Perimeter slot filled: {FilledCount}/{SlotCount}");

            if (IsComplete)
            {
                if (puddle != null)
                    puddle.SetContained(true);
                UnityEngine.Debug.Log("[CA] Perimeter complete: puddle contained.");
            }

            return true;
        }

        /// <summary>Fills every slot for a deterministic debug check.</summary>
        public void DebugFillAll()
        {
            ResetState();
            int count = Mathf.Min(
                slots == null ? 0 : slots.Length,
                sausages == null ? 0 : sausages.Length);

            for (int i = 0; i < count; i++)
            {
                if (slots[i] == null || sausages[i] == null)
                    continue;
                LogOrderIfNeeded();
                slots[i].Place(sausages[i]);
            }

            if (IsComplete && puddle != null)
                puddle.SetContained(true);
            UnityEngine.Debug.Log($"[CA] Perimeter debug fill: {FilledCount}/{SlotCount}; complete={IsComplete}");
        }

        /// <summary>Clears all slots and restores the containment state.</summary>
        public void ResetState()
        {
            if (sausages != null)
            {
                foreach (SpillSausage sausage in sausages)
                    if (sausage != null) sausage.ResetState();
            }

            if (slots != null)
            {
                foreach (PerimeterSlot slot in slots)
                    if (slot != null) slot.Clear();
            }

            orderErrorLogged = false;
            if (puddle != null && puddle.Contained)
                puddle.SetContained(false);
        }

        /// <summary>Prints current perimeter state through the supplied logger.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger != null)
                logger($"[CA] Perimeter: filled={FilledCount}/{SlotCount}; complete={IsComplete}; contained={(puddle != null && puddle.Contained)}");
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

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }

        private int GetFilledCount()
        {
            int count = 0;
            if (slots == null) return count;
            foreach (PerimeterSlot slot in slots)
                if (slot != null && slot.IsOccupied) count++;
            return count;
        }

        private void LogOrderIfNeeded()
        {
            if (orderErrorLogged)
                return;

            bool sdsRead = sdsItem != null && sdsItem.HasBeenRead;
            bool ppeReady = ppeState != null && puddle != null &&
                ppeState.HasRequiredSet(puddle.CurrentVariant);
            bool ventilationOn = director != null && director.IsVentilationOn;

            if (!sdsRead || !ppeReady || !ventilationOn)
            {
                orderErrorLogged = true;
                if (director != null)
                    director.ReportError(
                        "spillkit.before-procedure",
                        "chemical.error.procedure_order",
                        Dimension.Decision,
                        false);
                UnityEngine.Debug.Log($"[CA] Recommended order error: ventilation={ventilationOn}; SDS={sdsRead}; PPE={ppeReady}; action continues.");
            }
        }
    }
}
