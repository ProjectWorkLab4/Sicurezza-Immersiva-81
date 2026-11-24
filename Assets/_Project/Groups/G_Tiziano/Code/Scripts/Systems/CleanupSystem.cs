using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Counts absorbent sheets, runs the absorption visual and completes cleanup.
    /// </summary>
    public sealed class CleanupSystem : MonoBehaviour, IResettable
    {
        [SerializeField] private Puddle puddle;
        [SerializeField] private PerimeterSlots perimeter;
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private PlayerPPEState ppeState;
        [SerializeField] private AbsorbentSheet[] sheets = new AbsorbentSheet[0];
        [SerializeField, Min(0.01f)] private float placementDistance = 0.75f;

        private bool cleanupComplete;
        private bool absorptionStarted;

        /// <summary>Raised once when all required sheets are disposed.</summary>
        public event Action CleanupComplete;
        /// <summary>Gets whether cleanup is complete.</summary>
        public bool IsCleanupComplete => cleanupComplete;
        /// <summary>Gets the number of placed sheets.</summary>
        public int PlacedCount => CountPlaced();
        /// <summary>Gets the number of sheets saturated by the completed absorption.</summary>
        public int DisposedCount => CountAbsorbed();
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
            if (puddle != null)
                puddle.ContainedChanged += HandleContainedChanged;
            if (puddle != null)
                puddle.AbsorptionCompleted += HandleAbsorptionCompleted;
        }

        private void OnDisable()
        {
            if (director != null)
                director.PhaseChanged -= HandlePhaseChanged;
            if (puddle != null)
                puddle.ContainedChanged -= HandleContainedChanged;
            if (puddle != null)
                puddle.AbsorptionCompleted -= HandleAbsorptionCompleted;
        }

        private void Update()
        {
            EvaluateCoverage();
            if (absorptionStarted && puddle != null)
            {
                float progress = puddle.AbsorptionProgress;
                if (sheets != null)
                {
                    foreach (AbsorbentSheet sheet in sheets)
                        if (sheet != null && sheet.IsPlaced)
                            sheet.SetAbsorptionProgress(progress);
                }
            }
        }

        /// <summary>Places a released sheet when it is close enough to the puddle.</summary>
        public bool TryPlaceSheet(AbsorbentSheet sheet)
        {
            if (sheet == null || sheet.IsPlaced || puddle == null ||
                director != null && director.Outcome.HasValue ||
                Vector3.Distance(sheet.transform.position, puddle.transform.position) > placementDistance)
                return false;

            bool hasGloves = ppeState != null && ppeState.HasGloves;
            if (director != null)
            {
                if (hasGloves)
                    director.RecordAction("sheet.place", true, Dimension.Execution, "chemical.sheet.place");
                else
                    director.ReportError(
                        "sheet.place.without.gloves",
                        "chemical.error.gloves",
                        Dimension.Safety,
                        true);
            }

            if (director != null && director.Outcome.HasValue)
                return false;

            Vector3 localPosition = new Vector3((PlacedCount - 1) * 0.18f, 0.08f, 0f);
            sheet.AttachToPuddle(puddle.transform, localPosition);
            UnityEngine.Debug.Log($"[CA] Absorbent sheet placed: {PlacedCount}/{RequiredSheetCount}");
            EvaluateCoverage();
            return true;
        }

        /// <summary>Places the required sheets and advances absorption for deterministic debug checks.</summary>
        public void DebugCompleteCleanup()
        {
            if (perimeter != null && !perimeter.IsComplete)
                perimeter.DebugFillAll();

            if (sheets != null)
            {
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null) TryPlaceSheetDebug(sheet);
            }

            EvaluateCoverage();
            if (puddle != null && director != null && director.CurrentConfig != null)
                AdvanceDebugTime(director.CurrentConfig.AbsorptionDurationSeconds);
        }

        /// <summary>Advances the absorption timer without waiting for real time.</summary>
        public void AdvanceDebugTime(float seconds)
        {
            if (puddle != null && absorptionStarted)
                puddle.AdvanceAbsorptionDebugTime(seconds);
        }

        /// <summary>Prints placement and absorption state through the supplied logger.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger != null)
                logger($"[CA] Cleanup: placed={PlacedCount}/{RequiredSheetCount}; absorbed={DisposedCount}; contained={(puddle != null && puddle.Contained)}; absorptionStarted={absorptionStarted}; complete={cleanupComplete}");
        }

        /// <summary>Resets sheets, absorption progress and puddle visibility.</summary>
        public void ResetState()
        {
            cleanupComplete = false;
            absorptionStarted = false;
            if (sheets != null)
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null) sheet.ResetState();
            if (puddle != null)
                puddle.ResetState();
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
            if (cleanupComplete || absorptionStarted || puddle == null)
                return;

            if (PlacedCount < RequiredSheetCount || !puddle.Contained)
                return;

            ChemicalAlertConfig config = director == null ? null : director.CurrentConfig;
            float duration = config == null ? 4.5f : config.AbsorptionDurationSeconds;
            absorptionStarted = puddle.BeginAbsorption(duration);
            if (absorptionStarted)
                UnityEngine.Debug.Log($"[CA] Absorption started: sheets={PlacedCount}; duration={duration:0.00}s.");
        }

        private int CalculateRequiredSheetCount()
        {
            int total = sheets == null ? 0 : sheets.Length;
            if (total == 0)
                return 0;
            int configured = director == null || director.CurrentConfig == null
                ? 3
                : director.CurrentConfig.RequiredAbsorbentSheetCount;
            return Mathf.Clamp(configured, 1, total);
        }

        private int CountPlaced()
        {
            int count = 0;
            if (sheets != null)
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null && sheet.IsPlaced) count++;
            return count;
        }

        private int CountAbsorbed()
        {
            int count = 0;
            if (sheets != null)
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null && sheet.IsAbsorbed) count++;
            return count;
        }

        private void HandleContainedChanged(bool isContained)
        {
            if (isContained)
                EvaluateCoverage();
        }

        private void HandleAbsorptionCompleted()
        {
            if (cleanupComplete)
                return;

            cleanupComplete = true;
            absorptionStarted = false;
            if (sheets != null)
            {
                foreach (AbsorbentSheet sheet in sheets)
                    if (sheet != null && sheet.IsPlaced)
                        sheet.SetAbsorbed(true);
            }

            CleanupComplete?.Invoke();
            UnityEngine.Debug.Log($"[CA] CleanupComplete: absorbed={CountAbsorbed()}/{RequiredSheetCount}.");
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }
    }
}
