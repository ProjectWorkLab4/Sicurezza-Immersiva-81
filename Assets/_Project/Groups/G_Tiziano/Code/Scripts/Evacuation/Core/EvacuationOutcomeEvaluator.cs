using System;
using System.Collections.Generic;
using System.Linq;
using SI81.Tiziano.ChemicalAlert.Core;

namespace SI81.Tiziano.Evacuation.Core
{
    /// <summary>
    /// Pure rules converting an Evacuation end-state snapshot into a ScenarioResult (outcome, 5 sub-scores, action log).
    /// Reuses the shared Outcome/Dimension/ActionLog/ScenarioResult types.
    /// </summary>
    public sealed class EvacuationOutcomeEvaluator
    {
        private static readonly Dimension[] DimensionOrder =
        {
            Dimension.Safety, Dimension.Awareness, Dimension.Decision, Dimension.Execution, Dimension.Time
        };

        /// <summary>
        /// Evaluates the snapshot. If actionLog is null the log (LG01-LG11) is derived from the snapshot;
        /// otherwise the supplied runtime log is attached unchanged.
        /// </summary>
        public ScenarioResult Evaluate(EvacuationSnapshot snapshot, EvacScoringSettings settings = null, ActionLog actionLog = null)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            settings = settings ?? new EvacScoringSettings();
            ActionLog log = actionLog ?? BuildActionLog(snapshot);
            return new ScenarioResult(DetermineOutcome(snapshot), CalculateScores(snapshot, settings), log);
        }

        /// <summary>Determines Success / Partial / Failure from the snapshot.</summary>
        public Outcome DetermineOutcome(EvacuationSnapshot s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            bool elevatorFailure = s.ElevatorUsed && s.Difficulty != EvacDifficulty.Guided;
            if (s.IsDead || elevatorFailure || s.EffectiveTimeout)
                return Outcome.Failure;

            // No exit and no failure condition: the scenario never finished, treated as Failure [PROPOSTA].
            if (s.ExitReached == EvacExit.None)
                return Outcome.Failure;

            if (s.ExitReached == EvacExit.Internal)
                return Outcome.Partial;

            bool elevatorCapsGuided = s.ElevatorUsed; // only reachable in Guided
            if (elevatorCapsGuided || !s.PlanInspected || !s.AllNpcCollected)
                return Outcome.Partial;

            return Outcome.Success;
        }

        /// <summary>Calculates the five 0-100 sub-scores.</summary>
        public Dictionary<Dimension, int> CalculateScores(EvacuationSnapshot s, EvacScoringSettings st)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            st = st ?? new EvacScoringSettings();

            var scores = new Dictionary<Dimension, int>();
            scores[Dimension.Safety] = CalculateSafety(s, st);
            scores[Dimension.Awareness] = s.PlanInspected ? 100 : Clamp100(st.AwarenessNoPlanScore);
            scores[Dimension.Decision] = CalculateDecision(s, st);
            scores[Dimension.Execution] = s.NpcTotal <= 0
                ? 100
                : Clamp100(RoundToInt(100d * s.NpcCollectedTotal / s.NpcTotal));
            scores[Dimension.Time] = CalculateTime(s, st);
            return scores;
        }

        /// <summary>Builds the action log (LG01-LG11) implied by the snapshot.</summary>
        public ActionLog BuildActionLog(EvacuationSnapshot s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            var log = new ActionLog();
            double end = s.ElapsedSeconds;

            if (s.PlanInspected)
                Add(log, Stamp(s.PlanInspectedAt, end), EvacActionIds.Lg01PlanInspected, true, Dimension.Awareness);
            if (s.NpcTotalRoom1 > 0 && s.NpcCollectedRoom1 >= s.NpcTotalRoom1)
                Add(log, Stamp(s.Room1CompletedAt, end), EvacActionIds.Lg03Room1Done, true, Dimension.Execution);
            if (s.NpcTotalRoom2 > 0 && s.NpcCollectedRoom2 >= s.NpcTotalRoom2)
                Add(log, Stamp(s.Room2CompletedAt, end), EvacActionIds.Lg04Room2Done, true, Dimension.Execution);

            int nonLethal = s.LethalContacts > 0 ? Math.Min(s.FireContacts, s.LethalContacts - 1) : s.FireContacts;
            for (int i = 0; i < nonLethal; i++)
                Add(log, end, EvacActionIds.Lg07FireContact, false, Dimension.Safety);
            if (s.LethalContacts > 0 && s.FireContacts >= s.LethalContacts)
                Add(log, end, EvacActionIds.Lg08FireDeath, false, Dimension.Safety);

            if (s.ElevatorUsed)
                Add(log, end, EvacActionIds.Lg06Elevator, false, Dimension.Safety);
            if (s.EffectiveTimeout)
                Add(log, end, EvacActionIds.Lg10Timeout, false, Dimension.Time);

            if (s.ExitReached != EvacExit.None && !s.PlanInspected)
                Add(log, end, EvacActionIds.Lg02NoPlan, false, Dimension.Awareness);

            if (s.ExitReached == EvacExit.Emergency)
            {
                if (s.AllNpcCollected)
                    Add(log, end, EvacActionIds.Lg09StairsAllNpcs, true, Dimension.Decision);
                else
                    Add(log, end, EvacActionIds.Lg05StairsMissingNpcs, false, Dimension.Decision);
            }
            else if (s.ExitReached == EvacExit.Internal)
            {
                Add(log, end, EvacActionIds.Lg11InternalStairs, false, Dimension.Decision);
            }

            return log;
        }

        /// <summary>Runs the built-in cases, prints PASS/FAIL per case through output and returns true if all pass.</summary>
        public bool SelfCheck(Action<string> output)
        {
            int passed = 0;
            int total = 0;
            var st = new EvacScoringSettings();

            // Expected scores order: Safety, Awareness, Decision, Execution, Time (-1 = not checked).
            Run("success_operational", Make(s => { s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 90f; s.TimeLimitSeconds = 180f; }),
                Outcome.Success, new[] { 100, 100, 100, 100, 100 }, new[] { "evac.lg01", "evac.lg03", "evac.lg04", "evac.lg09" });

            Run("partial_missing_npc", Make(s => { s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 1; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 100f; s.TimeLimitSeconds = 180f; }),
                Outcome.Partial, new[] { 100, 100, 70, 80, 100 }, new[] { "evac.lg01", "evac.lg03", "evac.lg05" });

            Run("partial_no_plan", Make(s => { s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 100f; s.TimeLimitSeconds = 180f; }),
                Outcome.Partial, new[] { 100, 30, 100, 100, 100 }, new[] { "evac.lg02", "evac.lg03", "evac.lg04", "evac.lg09" });

            Run("partial_internal_stairs", Make(s => { s.PlanInspected = true; s.NpcCollectedRoom1 = 1; s.ExitReached = EvacExit.Internal; s.ElapsedSeconds = 60f; s.TimeLimitSeconds = 180f; }),
                Outcome.Partial, new[] { 100, 100, 60, 20, 100 }, new[] { "evac.lg01", "evac.lg11" });

            Run("fail_elevator_operational", Make(s => { s.ElevatorUsed = true; s.ElapsedSeconds = 20f; s.TimeLimitSeconds = 180f; }),
                Outcome.Failure, new[] { 0, 30, 40, 0, 100 }, new[] { "evac.lg06" });

            Run("fail_timeout", Make(s => { s.PlanInspected = true; s.TimedOut = true; s.ElapsedSeconds = 180f; s.TimeLimitSeconds = 180f; }),
                Outcome.Failure, new[] { 100, 100, 100, 0, 0 }, new[] { "evac.lg01", "evac.lg10" });

            Run("fail_fire_death_crisis", Make(s => { s.Difficulty = EvacDifficulty.Crisis; s.PlanInspected = true; s.FireContacts = 3; s.Alive = false; s.ElapsedSeconds = 40f; s.TimeLimitSeconds = 120f; }),
                Outcome.Failure, new[] { 0, 100, 100, 0, 100 }, new[] { "evac.lg01", "evac.lg07", "evac.lg07", "evac.lg08" });

            Run("guided_elevator_partial", Make(s => { s.Difficulty = EvacDifficulty.Guided; s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.ElevatorUsed = true; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 120f; }),
                Outcome.Partial, new[] { 0, 100, 40, 100, 88 }, new[] { "evac.lg01", "evac.lg03", "evac.lg04", "evac.lg06", "evac.lg09" });

            Run("guided_no_limit_success", Make(s => { s.Difficulty = EvacDifficulty.Guided; s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 400f; }),
                Outcome.Success, new[] { 100, 100, 100, 100, 40 }, new[] { "evac.lg01", "evac.lg03", "evac.lg04", "evac.lg09" });

            Run("guided_timeout_flag_ignored", Make(s => { s.Difficulty = EvacDifficulty.Guided; s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.ExitReached = EvacExit.Emergency; s.TimedOut = true; s.ElapsedSeconds = 60f; }),
                Outcome.Success, new[] { 100, 100, 100, 100, 100 }, new[] { "evac.lg01", "evac.lg03", "evac.lg04", "evac.lg09" });

            Run("crisis_one_contact_success", Make(s => { s.Difficulty = EvacDifficulty.Crisis; s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.FireContacts = 1; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 60f; s.TimeLimitSeconds = 120f; }),
                Outcome.Success, new[] { 75, 100, 100, 100, 100 }, new[] { "evac.lg01", "evac.lg03", "evac.lg04", "evac.lg07", "evac.lg09" });

            Run("crisis_two_contacts_success", Make(s => { s.Difficulty = EvacDifficulty.Crisis; s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.FireContacts = 2; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 60f; s.TimeLimitSeconds = 120f; }),
                Outcome.Success, new[] { 50, 100, 100, 100, 100 }, new[] { "evac.lg01", "evac.lg03", "evac.lg04", "evac.lg07", "evac.lg07", "evac.lg09" });

            Run("time_linear_midpoint", Make(s => { s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 144f; s.TimeLimitSeconds = 180f; }),
                Outcome.Success, new[] { 100, 100, 100, 100, 70 }, new[] { "evac.lg01", "evac.lg03", "evac.lg04", "evac.lg09" });

            Run("impulsive_exit_penalty", Make(s => { s.PlanInspected = true; s.NpcCollectedRoom1 = 3; s.NpcCollectedRoom2 = 2; s.ExitReached = EvacExit.Emergency; s.ElapsedSeconds = 50f; s.TimeLimitSeconds = 180f; s.ImpulsiveExit = true; }),
                Outcome.Success, new[] { 100, 100, 100, 100, 80 }, new[] { "evac.lg01", "evac.lg03", "evac.lg04", "evac.lg09" });

            Run("incomplete_no_exit", Make(s => { s.Difficulty = EvacDifficulty.Guided; s.ElapsedSeconds = 30f; }),
                Outcome.Failure, new[] { 100, 30, 100, 0, 100 }, new string[0]);

            string summary = $"[EV] E02 SelfCheck evaluator: {passed}/{total} PASS";
            if (output != null)
                output(summary);
            return passed == total;

            void Run(string name, EvacuationSnapshot snap, Outcome expectedOutcome, int[] expectedScores, string[] expectedLog)
            {
                total++;
                ScenarioResult result = Evaluate(snap, st);
                var problems = new List<string>();
                if (result.Outcome != expectedOutcome)
                    problems.Add($"outcome expected={expectedOutcome} actual={result.Outcome}");
                for (int i = 0; i < DimensionOrder.Length; i++)
                {
                    if (expectedScores[i] >= 0 && result.GetSubScore(DimensionOrder[i]) != expectedScores[i])
                        problems.Add($"{DimensionOrder[i]} expected={expectedScores[i]} actual={result.GetSubScore(DimensionOrder[i])}");
                }

                string expectedIds = string.Join(",", expectedLog.OrderBy(x => x, StringComparer.Ordinal));
                string actualIds = string.Join(",", result.ActionLog.Entries.Select(e => e.ActionId).OrderBy(x => x, StringComparer.Ordinal));
                if (expectedIds != actualIds)
                    problems.Add($"log expected=[{expectedIds}] actual=[{actualIds}]");

                bool pass = problems.Count == 0;
                if (pass)
                    passed++;
                if (output != null)
                    output($"[EV] E02 SelfCheck {(pass ? "PASS" : "FAIL")} {name}: outcome={result.Outcome}" + (pass ? string.Empty : " | " + string.Join("; ", problems)));
            }
        }

        private static EvacuationSnapshot Make(Action<EvacuationSnapshot> configure)
        {
            var snapshot = new EvacuationSnapshot();
            configure(snapshot);
            return snapshot;
        }

        private static int CalculateSafety(EvacuationSnapshot s, EvacScoringSettings st)
        {
            if (s.ElevatorUsed || s.IsDead)
                return 0;
            int counted = s.LethalContacts > 0 ? Math.Min(s.FireContacts, s.LethalContacts - 1) : s.FireContacts;
            return Clamp100(100 - st.SafetyContactPenalty * Math.Max(0, counted));
        }

        private static int CalculateDecision(EvacuationSnapshot s, EvacScoringSettings st)
        {
            int score = 100;
            if (s.ExitReached == EvacExit.Emergency && !s.AllNpcCollected)
                score -= st.DecisionMissingNpcPenalty;
            if (s.ExitReached == EvacExit.Internal)
                score -= st.DecisionInternalStairsPenalty;
            if (s.ElevatorUsed)
                score -= st.DecisionElevatorPenalty;
            return Clamp100(score);
        }

        private static int CalculateTime(EvacuationSnapshot s, EvacScoringSettings st)
        {
            if (s.EffectiveTimeout)
                return 0;

            float elapsed = Math.Max(0f, s.ElapsedSeconds);
            bool guidedCurve = s.Difficulty == EvacDifficulty.Guided || s.TimeLimitSeconds <= 0f;
            float full = guidedCurve ? st.GuidedFullScoreSeconds : st.TimeFullScoreFraction * s.TimeLimitSeconds;
            float min = guidedCurve ? st.GuidedMinScoreSeconds : s.TimeLimitSeconds;

            double score;
            if (elapsed <= full)
                score = 100d;
            else if (elapsed >= min || min <= full)
                score = st.TimeMinScore;
            else
                score = 100d - (100d - st.TimeMinScore) * (elapsed - full) / (min - full);

            int result = RoundToInt(score);
            if (s.ImpulsiveExit)
                result -= st.ImpulsivePenalty;
            return Clamp100(result);
        }

        private static void Add(ActionLog log, double timestamp, string id, bool correct, Dimension dimension)
        {
            log.Add(timestamp, id, correct, dimension, EvacActionIds.PhraseKey(id));
        }

        private static double Stamp(float optional, double fallback)
        {
            return optional >= 0f ? optional : fallback;
        }

        private static int RoundToInt(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static int Clamp100(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }
}
