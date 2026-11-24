using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Data
{
    /// <summary>
    /// Difficulty-specific tuning for the Chemical Alert scenario.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ChemicalAlertConfig_Guided", menuName = "SI81/Chemical Alert/Config")]
    public sealed class ChemicalAlertConfig : ScriptableObject
    {
        [SerializeField] private string difficultyId;
        [SerializeField, Min(1f)] private float totalDurationSeconds = 180f;
        [SerializeField, Min(0f)] private float ventilationCoughThresholdSeconds = 40f;
        [SerializeField, Min(0f)] private float ventilationToxicStageDelaySeconds = 20f;
        [SerializeField, Min(0f)] private float ventilationFailureDelaySeconds = 20f;
        [SerializeField, Min(0f)] private float ventilationJitterSeconds;
        [SerializeField, Min(0)] private int hearts = 3;
        [SerializeField, Min(0f)] private float painDurationSeconds = 5f;
        [SerializeField] private bool hintsEnabled;
        [SerializeField] private bool distractionsEnabled;
        [SerializeField] private bool guidedHighlights;
        [SerializeField] private HintSO[] orderedHints = new HintSO[0];
        [SerializeField, Min(1f)] private float hintIdleSeconds = 20f;
        [SerializeField] private bool disturbanceEnabled;
        [SerializeField, Range(0.1f, 3f)] private float disturbanceFrequencyHz = 2.5f;
        [SerializeField] private SubstanceVariant[] substanceVariants = new SubstanceVariant[0];
        [SerializeField] private PPEItem[] ppeShufflePool = new PPEItem[0];
        [SerializeField] private bool secondSpillEnabled;
        [SerializeField, Min(0f)] private float secondSpillWindowStartSeconds = 20f;
        [SerializeField, Min(0f)] private float secondSpillWindowEndSeconds = 40f;
        [SerializeField, Min(1f)] private float secondSpillGrowthMultiplier = 2f;
        [SerializeField] private AudioClip secondSpillAudio;
        [SerializeField, Min(1)] private int requiredAbsorbentSheetCount = 3;
        [SerializeField, Min(0.1f)] private float absorptionDurationSeconds = 4.5f;
        [SerializeField, Min(0f)] private float successDelaySeconds = 2f;
        [SerializeField] private bool defeatOnTimeout = true;
        [SerializeField, Min(0)] private int successPoints;
        [SerializeField, Min(0)] private int partialPoints;
        [SerializeField, Min(0)] private int failurePoints;

        /// <summary>Gets the stable difficulty identifier.</summary>
        public string DifficultyId => difficultyId;
        /// <summary>Gets the total scenario duration in seconds.</summary>
        public float TotalDurationSeconds => totalDurationSeconds;
        /// <summary>Gets the ventilation cough threshold in seconds.</summary>
        public float VentilationCoughThresholdSeconds => ventilationCoughThresholdSeconds;
        /// <summary>Gets the delay from cough to toxic stage in seconds.</summary>
        public float VentilationToxicStageDelaySeconds => ventilationToxicStageDelaySeconds;
        /// <summary>Gets the delay from toxic stage to failure in seconds.</summary>
        public float VentilationFailureDelaySeconds => ventilationFailureDelaySeconds;
        /// <summary>Gets the configured ventilation timing jitter in seconds.</summary>
        public float VentilationJitterSeconds => ventilationJitterSeconds;
        /// <summary>Gets the configured heart count.</summary>
        public int Hearts => hearts;
        /// <summary>Gets the pain duration in seconds.</summary>
        public float PainDurationSeconds => painDurationSeconds;
        /// <summary>Gets whether hints are enabled.</summary>
        public bool HintsEnabled => hintsEnabled;
        /// <summary>Gets whether distractions are enabled.</summary>
        public bool DistractionsEnabled => distractionsEnabled;
        /// <summary>Gets whether Guided highlights are enabled.</summary>
        public bool GuidedHighlights => guidedHighlights;
        /// <summary>Gets the ordered, data-driven Guided hints.</summary>
        public HintSO[] OrderedHints => orderedHints;
        /// <summary>Gets the idle duration before an automatic hint is emitted.</summary>
        public float HintIdleSeconds => hintIdleSeconds;
        /// <summary>Gets whether Crisis disturbances are enabled.</summary>
        public bool DisturbanceEnabled => disturbanceEnabled;
        /// <summary>Gets the configured disturbance flicker frequency in hertz.</summary>
        public float DisturbanceFrequencyHz => disturbanceFrequencyHz;
        /// <summary>Gets the substance variants available to this replay.</summary>
        public SubstanceVariant[] SubstanceVariants => substanceVariants;
        /// <summary>Gets the shuffled cabinet PPE pool, including decoys.</summary>
        public PPEItem[] PpeShufflePool => ppeShufflePool;
        /// <summary>Gets whether the dynamic second spill is enabled.</summary>
        public bool SecondSpillEnabled => secondSpillEnabled;
        /// <summary>Gets the earliest second-spill time after Action begins.</summary>
        public float SecondSpillWindowStartSeconds => secondSpillWindowStartSeconds;
        /// <summary>Gets the latest second-spill time after Action begins.</summary>
        public float SecondSpillWindowEndSeconds => secondSpillWindowEndSeconds;
        /// <summary>Gets the secondary puddle growth multiplier.</summary>
        public float SecondSpillGrowthMultiplier => secondSpillGrowthMultiplier;
        /// <summary>Gets the optional second-spill audio signal.</summary>
        public AudioClip SecondSpillAudio => secondSpillAudio;
        /// <summary>Gets the number of sheets to place on the puddle for absorption.</summary>
        public int RequiredAbsorbentSheetCount => requiredAbsorbentSheetCount;
        /// <summary>Gets the time used for the puddle absorption scale-down.</summary>
        public float AbsorptionDurationSeconds => absorptionDurationSeconds;
        /// <summary>Gets the delay between completing gameplay objectives and resolving the result.</summary>
        public float SuccessDelaySeconds => successDelaySeconds;
        /// <summary>Gets whether timeout causes defeat.</summary>
        public bool DefeatOnTimeout => defeatOnTimeout;
        /// <summary>Gets success points.</summary>
        public int SuccessPoints => successPoints;
        /// <summary>Gets partial-result points.</summary>
        public int PartialPoints => partialPoints;
        /// <summary>Gets failure points.</summary>
        public int FailurePoints => failurePoints;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(difficultyId))
                UnityEngine.Debug.LogWarning($"[CA] {name}: difficultyId is empty.", this);

            if (ventilationCoughThresholdSeconds + ventilationToxicStageDelaySeconds +
                ventilationFailureDelaySeconds > totalDurationSeconds)
                UnityEngine.Debug.LogWarning($"[CA] {name}: ventilation thresholds exceed total duration.", this);

            if (hearts == 0 && defeatOnTimeout && !hintsEnabled)
                UnityEngine.Debug.LogWarning($"[CA] {name}: zero hearts with timeout defeat and no hints; verify Guided tuning.", this);

            disturbanceFrequencyHz = Mathf.Clamp(disturbanceFrequencyHz, 0.1f, 3f);
            secondSpillWindowStartSeconds = Mathf.Max(0f, secondSpillWindowStartSeconds);
            secondSpillWindowEndSeconds = Mathf.Max(secondSpillWindowStartSeconds, secondSpillWindowEndSeconds);
            secondSpillGrowthMultiplier = Mathf.Max(1f, secondSpillGrowthMultiplier);
            requiredAbsorbentSheetCount = Mathf.Clamp(requiredAbsorbentSheetCount, 1, 6);
            absorptionDurationSeconds = Mathf.Max(0.1f, absorptionDurationSeconds);
            successDelaySeconds = Mathf.Max(0f, successDelaySeconds);
        }
    }
}
