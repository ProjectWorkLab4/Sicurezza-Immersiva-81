using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.Evacuation.Core;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Data
{
    /// <summary>
    /// Difficulty-specific tuning for the Evacuation scenario. One asset per level
    /// (SO_EvacuationConfig_Guided / Operational / Crisis). No gameplay number lives in code.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_EvacuationConfig_Guided", menuName = "SI81/Evacuation/Config")]
    public sealed class EvacuationConfig : ScriptableObject
    {
        [Header("Level")]
        [SerializeField] private EvacDifficulty difficulty = EvacDifficulty.Guided;

        [Header("Time")]
        [Tooltip("Total time limit in seconds. 0 = infinite (stopwatch only).")]
        [SerializeField, Min(0f)] private float totalDurationSeconds;
        [SerializeField] private bool hasTimeLimit;

        [Header("Health and fire")]
        [SerializeField, Min(0)] private int hearts;
        [SerializeField] private bool fireEnabled;
        [Tooltip("Number of fire slots active per run (picked from the 10 FIRESLOT_* markers by seed).")]
        [SerializeField, Min(0)] private int activeFireCount = 5;
        [Tooltip("Fire contacts that kill the player (separate counter from hearts).")]
        [SerializeField, Min(1)] private int lethalContacts = 3;
        [SerializeField, Min(0f)] private float contactCooldownSeconds = 2f;

        [Header("Aids")]
        [SerializeField] private bool highlightEnabled;
        [SerializeField] private bool hintsEnabled;
        [SerializeField, Min(1f)] private float hintIdleSeconds = 20f;
        [Tooltip("Ordered Guided hints: plan, room1, room2, stairs, no-elevator.")]
        [SerializeField] private HintSO[] orderedHints = new HintSO[0];
        [Tooltip("Start the level with reduced visual effects (preparation for the Crisis fire).")]
        [SerializeField] private bool startWithReducedEffects;
        [SerializeField] private bool disturbancesEnabled;

        [Header("NPCs and route")]
        [SerializeField, Min(0)] private int npcCountRoom1 = 3;
        [SerializeField, Min(0)] private int npcCountRoom2 = 2;
        [SerializeField, Min(0.1f)] private float routeLaneWidth = 1.2f;

        [Header("Interaction")]
        [Tooltip("Gaze dwell time (seconds) to inspect an evacuation plan.")]
        [SerializeField, Min(0f)] private float interactionDwellSeconds = 0.75f;
        [Tooltip("Maximum distance (m) for gaze inspection.")]
        [SerializeField, Min(0.1f)] private float inspectMaxDistance = 1.2f;

        [Header("GDD points")]
        [SerializeField, Min(0)] private int successPoints;
        [SerializeField, Min(0)] private int partialPoints;
        [SerializeField, Min(0)] private int failurePoints;

        [Header("Scoring")]
        [SerializeField] private EvacScoringSettings scoring = new EvacScoringSettings();

        /// <summary>Gets the difficulty level this config describes.</summary>
        public EvacDifficulty Difficulty => difficulty;
        /// <summary>Gets the total duration in seconds (0 = infinite).</summary>
        public float TotalDurationSeconds => totalDurationSeconds;
        /// <summary>Gets whether running out of time ends the scenario (needs a positive duration).</summary>
        public bool HasTimeLimit => hasTimeLimit && totalDurationSeconds > 0f;
        /// <summary>Gets the heart count (0 = no damage possible).</summary>
        public int Hearts => hearts;
        /// <summary>Gets whether fire is spawned.</summary>
        public bool FireEnabled => fireEnabled;
        /// <summary>Gets the number of active fire slots per run.</summary>
        public int ActiveFireCount => activeFireCount;
        /// <summary>Gets the fire contacts that kill the player.</summary>
        public int LethalContacts => lethalContacts;
        /// <summary>Gets the cooldown between two contacts on the same entry, in seconds.</summary>
        public float ContactCooldownSeconds => contactCooldownSeconds;
        /// <summary>Gets whether guided highlights are enabled.</summary>
        public bool HighlightEnabled => highlightEnabled;
        /// <summary>Gets whether hints are enabled.</summary>
        public bool HintsEnabled => hintsEnabled;
        /// <summary>Gets the idle time before an automatic hint, in seconds.</summary>
        public float HintIdleSeconds => hintIdleSeconds;
        /// <summary>Gets the ordered Guided hint assets (stable localization keys).</summary>
        public IReadOnlyList<HintSO> OrderedHints => orderedHints;
        /// <summary>Gets whether the level starts with reduced visual effects.</summary>
        public bool StartWithReducedEffects => startWithReducedEffects;
        /// <summary>Gets whether disturbance effects (alarm/smoke pulses) are enabled.</summary>
        public bool DisturbancesEnabled => disturbancesEnabled;
        /// <summary>Gets the NPC count in room 1.</summary>
        public int NpcCountRoom1 => npcCountRoom1;
        /// <summary>Gets the NPC count in room 2.</summary>
        public int NpcCountRoom2 => npcCountRoom2;
        /// <summary>Gets the total NPC count.</summary>
        public int NpcCountTotal => npcCountRoom1 + npcCountRoom2;
        /// <summary>Gets the minimum free lane width on the ideal routes, in metres.</summary>
        public float RouteLaneWidth => routeLaneWidth;
        /// <summary>Gets the gaze dwell time to inspect a plan, in seconds.</summary>
        public float InteractionDwellSeconds => interactionDwellSeconds;
        /// <summary>Gets the maximum gaze inspection distance, in metres.</summary>
        public float InspectMaxDistance => inspectMaxDistance;
        /// <summary>Gets the GDD points for Success.</summary>
        public int SuccessPoints => successPoints;
        /// <summary>Gets the GDD points for Partial.</summary>
        public int PartialPoints => partialPoints;
        /// <summary>Gets the GDD points for Failure.</summary>
        public int FailurePoints => failurePoints;
        /// <summary>Gets the scoring numbers used by the evaluator.</summary>
        public EvacScoringSettings Scoring => scoring;

        /// <summary>Collects every configuration inconsistency as a readable message (empty list = valid).</summary>
        public void Validate(IList<string> warnings)
        {
            if (warnings == null)
                return;

            if (hasTimeLimit && totalDurationSeconds <= 0f)
                warnings.Add("hasTimeLimit is on but totalDurationSeconds is 0: the limit is ignored.");
            if (!hasTimeLimit && totalDurationSeconds > 0f)
                warnings.Add("totalDurationSeconds > 0 but hasTimeLimit is off: the limit is ignored.");
            if (difficulty == EvacDifficulty.Guided && hasTimeLimit)
                warnings.Add("Guided must not have a time limit (stopwatch only).");
            if (difficulty != EvacDifficulty.Guided && !hasTimeLimit)
                warnings.Add("Operational/Crisis need a time limit.");
            if (difficulty == EvacDifficulty.Guided)
            {
                if (!highlightEnabled || !hintsEnabled)
                    warnings.Add("Guided needs highlights and hints enabled.");
                if (orderedHints == null || orderedHints.Length != 5)
                    warnings.Add("Guided needs 5 ordered HintSO assets.");
                else
                {
                    for (int i = 0; i < orderedHints.Length; i++)
                    {
                        if (orderedHints[i] == null || string.IsNullOrEmpty(orderedHints[i].LocalizationKey))
                            warnings.Add($"Ordered hint {i + 1} is missing or has no localization key.");
                    }
                }
            }
            else if (highlightEnabled || hintsEnabled)
            {
                warnings.Add("Highlights and hints are Guided-only.");
            }
            if (fireEnabled && difficulty != EvacDifficulty.Crisis)
                warnings.Add("Fire is only expected in Crisis.");
            if (fireEnabled && (activeFireCount < 1 || activeFireCount > 10))
                warnings.Add("activeFireCount must be between 1 and 10 (10 FIRESLOT markers).");
            if (hearts == 0 && difficulty != EvacDifficulty.Guided)
                warnings.Add("hearts is 0 outside Guided: the player could not take damage.");
            if (NpcCountTotal != 5)
                warnings.Add("NPC total is not 5 (brief: 3 + 2).");
            if (routeLaneWidth < 1.2f)
                warnings.Add("routeLaneWidth below 1.2 m (brief requires >= 1.2 m free lane).");
            if (scoring == null)
            {
                warnings.Add("scoring settings missing.");
                return;
            }

            if (scoring.TimeFullScoreFraction <= 0f || scoring.TimeFullScoreFraction >= 1f)
                warnings.Add("scoring.TimeFullScoreFraction must be in (0,1).");
            if (scoring.GuidedMinScoreSeconds <= scoring.GuidedFullScoreSeconds)
                warnings.Add("scoring.GuidedMinScoreSeconds must be greater than GuidedFullScoreSeconds.");
            if (successPoints < partialPoints || partialPoints < failurePoints)
                warnings.Add("GDD points should satisfy success >= partial >= failure.");
        }

        private void OnValidate()
        {
            var warnings = new List<string>();
            Validate(warnings);
            for (int i = 0; i < warnings.Count; i++)
                UDebug.LogWarning($"[EV] {name}: {warnings[i]}", this);
        }
    }
}
