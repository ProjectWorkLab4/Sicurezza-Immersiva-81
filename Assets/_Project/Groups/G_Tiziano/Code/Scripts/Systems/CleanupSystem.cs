using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Counts absorbent coverage and waste disposal for the cleanup sequence.
    /// </summary>
    public sealed class CleanupSystem : MonoBehaviour, IResettable
    {
        [SerializeField] private Puddle puddle;
        [SerializeField] private PerimeterSlots perimeter;
        [SerializeField] private WasteBag wasteBag;
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private AbsorbentSheet[] sheets = new AbsorbentSheet[0];
        [SerializeField, Min(0.01f)] private float placementDistance = 0.75f;

        private bool cleanupComplete;

        /// <summary>Raised once when all required sheets are disposed.</summary>
        public event Action CleanupComplete;
        /// <summary>Gets whether cleanup is complete.</summary>
        public bool IsCleanupComplete => cleanupComplete;
        /// <summary>Gets the number of placed sheets.</summary>
        public int PlacedCount => CountPlaced();
        /// <summary>Gets the number of disposed sheets.</summary>
        public int DisposedCount => CountDisposed();
        /// <summary>Gets the current required sheet count.</summary>
        public int RequiredSheetCount => CalculateRequiredSheetCount();

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

        private void Update()
        {
            EvaluateCoverage();
        }

        /// <summary>Places a released sheet when it is close enough to the puddle.</summary>
        public bool TryPlaceSheet(AbsorbentSheet sheet)
        {
            if (sheet == null || sheet.IsPlaced || puddle == null ||
                Vector3.Distance(sheet.transform.position, puddle.transform.position) > placementDistance)
                return false;

            if (director != null)
                director.RecordAction("sheet.place", true, Dimension.Execution, "chemical.sheet.place");

            Vector3 localPosition = new Vector3(
                (PlacedCount % 3 - 1) * 0.25f,
                0.08f,
                (PlacedCount / 3) * 0.2f - 0.1f);
            sheet.AttachToPuddle(puddle.transform, localPosition);
            UnityEngine.Debug.Log($"[CA] Absorbent sheet placed: {PlacedCount}/{RequiredSheetCount}");
            EvaluateCoverage();
            return true;
        }

        /// <summary>Registers a waste item disposed in the waste bag.</summary>
        public void RegisterDisposed(WasteItem item)
        {
            if (item == null || cleanupComplete)
                return;

            UnityEngine.Debug.Log($"[CA] Cleanup disposal: {DisposedCount}/{RequiredSheetCount}");
            EvaluateCoverage();
        }

        /// <summary>Completes the sequence for deterministic debug checks.</summary>
        public void DebugCompleteCleanup()
        {
            if (perimeter != null && !perimeter.IsComplete)
                perimeter.DebugFillAll();

            if (sheets != null)
            {
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null) TryPlaceSheetDebug(sheet);
            }

            if (wasteBag != null && sheets != null)
            {
                foreach (AbsorbentSheet sheet in sheets)
                {
                    if (sheet == null || sheet.Waste == null || !sheet.Waste.IsWaste)
                        continue;
                    sheet.transform.position = wasteBag.transform.position;
                    wasteBag.TryDispose(sheet.Waste);
                }
            }

            EvaluateCoverage();
        }

        /// <summary>Prints coverage and disposal state through the supplied logger.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger != null)
                logger($"[CA] Cleanup: placed={PlacedCount}/{RequiredSheetCount}; disposed={DisposedCount}; contained={(puddle != null && puddle.Contained)}; complete={cleanupComplete}");
        }

        /// <summary>Resets sheets, coverage, disposal and puddle visibility.</summary>
        public void ResetState()
        {
            cleanupComplete = false;
            if (sheets != null)
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null) sheet.ResetState();
            if (puddle != null && puddle.Covered)
                puddle.SetCovered(false);
        }

        private void TryPlaceSheetDebug(AbsorbentSheet sheet)
        {
            if (sheet == null || sheet.IsPlaced)
                return;

            sheet.transform.position = puddle.transform.position;
            TryPlaceSheet(sheet);
        }

        private void EvaluateCoverage()
        {
            if (cleanupComplete || puddle == null || !puddle.Contained)
                return;

            if (PlacedCount < RequiredSheetCount || DisposedCount < RequiredSheetCount)
                return;

            cleanupComplete = true;
            puddle.SetCovered(true);
            CleanupComplete?.Invoke();
            UnityEngine.Debug.Log("[CA] CleanupComplete.");
        }

        private int CalculateRequiredSheetCount()
        {
            int total = sheets == null ? 0 : sheets.Length;
            if (total == 0 || puddle == null || puddle.MaxRadius <= 0f)
                return 0;

            float ratio = Mathf.Clamp01(puddle.CurrentRadius / puddle.MaxRadius);
            return Mathf.Clamp(Mathf.CeilToInt(total * ratio), 1, total);
        }

        private int CountPlaced()
        {
            int count = 0;
            if (sheets != null)
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null && sheet.IsPlaced) count++;
            return count;
        }

        private int CountDisposed()
        {
            int count = 0;
            if (sheets != null)
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null && sheet.Waste != null && sheet.Waste.IsDisposed) count++;
            return count;
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }
    }
}
