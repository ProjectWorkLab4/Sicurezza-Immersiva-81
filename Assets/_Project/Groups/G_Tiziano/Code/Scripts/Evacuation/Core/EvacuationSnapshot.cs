using System;

namespace SI81.Tiziano.Evacuation.Core
{
    /// <summary>
    /// Tunable scoring numbers for the Evacuation evaluator. Serialized inside EvacuationConfig; defaults follow the BRIEF.
    /// </summary>
    [Serializable]
    public sealed class EvacScoringSettings
    {
        /// <summary>Safety penalty for each non-lethal fire contact.</summary>
        public int SafetyContactPenalty = 25;
        /// <summary>Awareness score when no evacuation plan was inspected.</summary>
        public int AwarenessNoPlanScore = 30;
        /// <summary>Decision penalty for reaching the emergency stairs with NPCs missing.</summary>
        public int DecisionMissingNpcPenalty = 30;
        /// <summary>Decision penalty for reaching the internal stairs.</summary>
        public int DecisionInternalStairsPenalty = 40;
        /// <summary>Decision penalty for using the elevator.</summary>
        public int DecisionElevatorPenalty = 60;
        /// <summary>Fraction of the time limit under which the Time score is 100 (timed levels).</summary>
        public float TimeFullScoreFraction = 0.6f;
        /// <summary>Time score reached at the limit (or at GuidedMinScoreSeconds).</summary>
        public int TimeMinScore = 40;
        /// <summary>Guided: seconds under which the Time score is 100.</summary>
        public float GuidedFullScoreSeconds = 90f;
        /// <summary>Guided: seconds at which the Time score drops to TimeMinScore.</summary>
        public float GuidedMinScoreSeconds = 240f;
        /// <summary>Time penalty when the player leaves the room doing nothing in the first seconds (impulsive).</summary>
        public int ImpulsivePenalty = 20;
        /// <summary>Seconds after the start in which leaving the room without any action counts as impulsive (used by the Director).</summary>
        public float ImpulsiveWindowSeconds = 5f;
    }

    /// <summary>
    /// Final-state snapshot handed to the evaluator. Pure data, no Unity dependency.
    /// </summary>
    public sealed class EvacuationSnapshot
    {
        /// <summary>Difficulty level of the run.</summary>
        public EvacDifficulty Difficulty = EvacDifficulty.Operational;
        /// <summary>True if at least one evacuation plan was inspected.</summary>
        public bool PlanInspected;
        /// <summary>NPCs of room 1 following the player at the exit.</summary>
        public int NpcCollectedRoom1;
        /// <summary>NPCs of room 2 following the player at the exit.</summary>
        public int NpcCollectedRoom2;
        /// <summary>NPCs present in room 1.</summary>
        public int NpcTotalRoom1 = 3;
        /// <summary>NPCs present in room 2.</summary>
        public int NpcTotalRoom2 = 2;
        /// <summary>Exit reached when the scenario ended.</summary>
        public EvacExit ExitReached = EvacExit.None;
        /// <summary>True if the player interacted with the elevator.</summary>
        public bool ElevatorUsed;
        /// <summary>Fire contacts accumulated (separate counter from hearts).</summary>
        public int FireContacts;
        /// <summary>Contacts that kill the player (config value).</summary>
        public int LethalContacts = 3;
        /// <summary>False if the player died (hearts at 0).</summary>
        public bool Alive = true;
        /// <summary>True if the timer expired (ignored in Guided).</summary>
        public bool TimedOut;
        /// <summary>Seconds elapsed since the start of the Action phase.</summary>
        public float ElapsedSeconds;
        /// <summary>Time limit in seconds (0 = none).</summary>
        public float TimeLimitSeconds;
        /// <summary>True if the player left the room doing nothing in the first seconds.</summary>
        public bool ImpulsiveExit;
        /// <summary>Optional timestamp of the first plan inspection (negative = use ElapsedSeconds).</summary>
        public float PlanInspectedAt = -1f;
        /// <summary>Optional timestamp when room 1 was fully collected (negative = use ElapsedSeconds).</summary>
        public float Room1CompletedAt = -1f;
        /// <summary>Optional timestamp when room 2 was fully collected (negative = use ElapsedSeconds).</summary>
        public float Room2CompletedAt = -1f;

        /// <summary>Gets the total NPCs collected.</summary>
        public int NpcCollectedTotal => Clamp(NpcCollectedRoom1, NpcTotalRoom1) + Clamp(NpcCollectedRoom2, NpcTotalRoom2);
        /// <summary>Gets the total NPCs in the scenario.</summary>
        public int NpcTotal => Math.Max(0, NpcTotalRoom1) + Math.Max(0, NpcTotalRoom2);
        /// <summary>Gets whether every NPC is following the player.</summary>
        public bool AllNpcCollected => NpcCollectedTotal >= NpcTotal;
        /// <summary>Gets whether the player is dead (explicit flag or lethal contacts reached).</summary>
        public bool IsDead => !Alive || (LethalContacts > 0 && FireContacts >= LethalContacts);
        /// <summary>Gets whether the timer failure applies (never in Guided).</summary>
        public bool EffectiveTimeout => TimedOut && Difficulty != EvacDifficulty.Guided;

        private static int Clamp(int value, int max)
        {
            return Math.Max(0, Math.Min(value, Math.Max(0, max)));
        }
    }
}
