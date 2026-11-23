using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Data;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Grows the spill visual and trigger until containment stops it.
    /// </summary>
    public sealed class Puddle : MonoBehaviour, IResettable
    {
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private SubstanceVariant currentVariant;
        [SerializeField] private Transform visual;
        [SerializeField, Range(0.01f, 1f)] private float initialRadiusFraction = 0.25f;

        private Vector3 initialRootScale;
        private float currentRadius;
        private bool growing;
        private bool contained;
        private bool covered;
        private bool secondSpillActive;
        private float activeGrowthMultiplier = 1f;
        private Collider puddleCollider;
        private Renderer visualRenderer;
        private MaterialPropertyBlock colorPropertyBlock;

        /// <summary>Raised when containment state changes.</summary>
        public event System.Action<bool> ContainedChanged;
        /// <summary>Gets whether the spill is contained.</summary>
        public bool Contained => contained;
        /// <summary>Gets whether absorbent-sheet coverage has hidden the spill.</summary>
        public bool Covered => covered;
        /// <summary>Gets the current radius.</summary>
        public float CurrentRadius => currentRadius;
        /// <summary>Gets the configured maximum radius.</summary>
        public float MaxRadius => currentVariant == null ? 1f : currentVariant.MaxRadius;
        /// <summary>Gets the configured growth rate.</summary>
        public float GrowthRate => currentVariant == null ? 0f : currentVariant.GrowthRate;
        /// <summary>Gets the current substance variant.</summary>
        public SubstanceVariant CurrentVariant => currentVariant;
        /// <summary>Gets whether the second-spill growth acceleration is active.</summary>
        public bool SecondSpillActive => secondSpillActive;

        private void Awake()
        {
            initialRootScale = transform.localScale;
            puddleCollider = GetComponent<Collider>();
            if (visual != null)
                visualRenderer = visual.GetComponent<Renderer>();
            ResetState();
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
            if (!growing || contained || GrowthRate <= 0f)
                return;

            AdvanceDebugTime(Time.deltaTime);
        }

        /// <summary>Advances growth without waiting for real time in debug checks.</summary>
        public void AdvanceDebugTime(float seconds)
        {
            if (!growing || contained || GrowthRate <= 0f || seconds <= 0f)
                return;

            currentRadius = Mathf.Min(MaxRadius,
                currentRadius + GrowthRate * activeGrowthMultiplier * seconds);
            ApplyScale();
        }

        /// <summary>Applies the replay-selected variant and resets the puddle tuning/visual.</summary>
        public void SetVariant(SubstanceVariant variant)
        {
            if (variant == null)
            {
                UnityEngine.Debug.LogError($"[CA] {name}: cannot assign a null SubstanceVariant.", this);
                return;
            }

            currentVariant = variant;
            ApplyVariantColor();
            ResetState();
        }

        /// <summary>Activates faster growth for the second spill unless the puddle is contained.</summary>
        public bool TriggerSecondSpill(float growthMultiplier)
        {
            if (secondSpillActive)
                return false;

            secondSpillActive = true;
            activeGrowthMultiplier = Mathf.Max(1f, growthMultiplier);
            if (!contained && !covered)
                growing = true;

            bool expanded = !contained && !covered;
            UnityEngine.Debug.Log($"[CA] Puddle second-spill growth: active={expanded}; multiplier={activeGrowthMultiplier:0.00}; radius={currentRadius:0.00}.");
            return expanded;
        }

        /// <summary>Marks the puddle contained and stops growth.</summary>
        public void SetContained(bool value)
        {
            if (contained == value)
                return;

            contained = value;
            if (contained)
                growing = false;
            ContainedChanged?.Invoke(contained);
            UnityEngine.Debug.Log($"[CA] Puddle contained={contained}; radius={currentRadius:0.00}");
        }

        /// <summary>Hides or reveals the spill after cleanup coverage.</summary>
        public void SetCovered(bool value)
        {
            if (covered == value)
                return;

            covered = value;
            if (covered)
                growing = false;
            if (puddleCollider != null)
                puddleCollider.enabled = !covered;
            if (visualRenderer != null)
                visualRenderer.enabled = !covered;
            UnityEngine.Debug.Log($"[CA] Puddle covered={covered}; radius={currentRadius:0.00}");
        }

        /// <summary>Resets radius, growth and containment state.</summary>
        public void ResetState()
        {
            contained = false;
            growing = false;
            covered = false;
            secondSpillActive = false;
            activeGrowthMultiplier = 1f;
            currentRadius = Mathf.Max(0.01f, MaxRadius * initialRadiusFraction);
            if (puddleCollider != null)
                puddleCollider.enabled = true;
            if (visualRenderer != null)
                visualRenderer.enabled = true;
            ApplyScale();
            ApplyVariantColor();
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
            else if (phase == ChemicalAlertDirector.Phase.Action && !contained)
                growing = true;
        }

        private void ApplyScale()
        {
            float factor = MaxRadius <= 0f ? 1f : Mathf.Clamp01(currentRadius / MaxRadius);
            transform.localScale = new Vector3(
                initialRootScale.x * factor,
                initialRootScale.y,
                initialRootScale.z * factor);
            if (visual != null)
                visual.localScale = Vector3.one;
        }

        private void ApplyVariantColor()
        {
            if (visualRenderer == null || currentVariant == null)
                return;

            if (colorPropertyBlock == null)
                colorPropertyBlock = new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(colorPropertyBlock);
            colorPropertyBlock.SetColor("_BaseColor", currentVariant.PuddleColor);
            colorPropertyBlock.SetColor("_Color", currentVariant.PuddleColor);
            visualRenderer.SetPropertyBlock(colorPropertyBlock);
        }

        private void OnValidate()
        {
            if (currentVariant == null)
                UnityEngine.Debug.LogWarning($"[CA] {name}: puddle SubstanceVariant is not assigned.", this);
        }
    }
}
