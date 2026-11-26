using System;
using System.Collections.Generic;
using System.Linq;

namespace SI81.Tiziano.ChemicalAlert.Core
{
    /// <summary>
    /// Pure rules for converting scenario state and actions into an outcome.
    /// </summary>
    public sealed class OutcomeEvaluator
    {
        /// <summary>
        /// Evaluates the final outcome and five dimension sub-scores.
        /// Set <paramref name="evaluateHearts"/> to false for difficulty levels without a health system.
        /// </summary>
        public ScenarioResult Evaluate(
            ActionLog actionLog,
            int heartsRemaining,
            float timeRemainingSeconds,
            bool lossContained,
            bool cleanupComplete,
            bool ventilationFailed,
            float totalDurationSeconds = 180f,
            bool evaluateHearts = true)
        {
            ActionLog log = actionLog ?? new ActionLog();
            bool allActionsCorrect = log.Count > 0 && log.Entries.All(entry => entry.IsCorrect);
            Outcome outcome;

            bool heartsFailed = evaluateHearts && heartsRemaining <= 0;
            if (heartsFailed || timeRemainingSeconds <= 0f || ventilationFailed)
                outcome = Outcome.Failure;
            else if (allActionsCorrect && lossContained && cleanupComplete)
                outcome = Outcome.Success;
            else
                outcome = Outcome.Partial;

            var scores = CalculateScores(
                log,
                heartsRemaining,
                timeRemainingSeconds,
                lossContained,
                cleanupComplete,
                ventilationFailed,
                totalDurationSeconds,
                evaluateHearts);

            return new ScenarioResult(outcome, scores, log);
        }

        /// <summary>Runs deterministic evaluation cases and reports PASS/FAIL through the supplied logger.</summary>
        public bool SelfCheck(Action<string> output)
        {
            int passed = 0;
            int total = 0;

            CheckCase("success", Evaluate(
                CreateCorrectLog(),
                3,
                120f,
                true,
                true,
                false), Outcome.Success, output, ref passed, ref total);

            CheckCase("partial_loss_not_contained", Evaluate(
                CreateCorrectLog(),
                3,
                120f,
                false,
                true,
                false), Outcome.Partial, output, ref passed, ref total);

            CheckCase("partial_damage", Evaluate(
                CreateIncorrectSafetyLog(),
                2,
                120f,
                true,
                true,
                false), Outcome.Partial, output, ref passed, ref total);

            CheckCase("failure_hearts_zero", Evaluate(
                CreateIncorrectSafetyLog(),
                0,
                120f,
                true,
                true,
                false), Outcome.Failure, output, ref passed, ref total);

            CheckCase("guided_ignores_zero_hearts", Evaluate(
                CreateCorrectLog(),
                0,
                120f,
                true,
                true,
                false,
                180f,
                false), Outcome.Success, output, ref passed, ref total);

            CheckCase("failure_timeout", Evaluate(
                CreateCorrectLog(),
                3,
                0f,
                true,
                true,
                false), Outcome.Failure, output, ref passed, ref total);

            CheckCase("failure_ventilation_limit", Evaluate(
                CreateCorrectLog(),
                3,
                120f,
                true,
                true,
                true), Outcome.Failure, output, ref passed, ref total);

            CheckCase("partial_cleanup_incomplete", Evaluate(
                CreateCorrectLog(),
                3,
                120f,
                true,
                false,
                false), Outcome.Partial, output, ref passed, ref total);

            CheckCase("partial_wrong_decision", Evaluate(
                CreateMixedLog(),
                3,
                90f,
                true,
                true,
                false), Outcome.Partial, output, ref passed, ref total);

            CheckCase("partial_no_actions", Evaluate(
                new ActionLog(),
                3,
                120f,
                true,
                true,
                false), Outcome.Partial, output, ref passed, ref total);

            string summary = $"[CA] T06 SelfCheck: {passed}/{total} PASS";
            if (output != null)
                output(summary);
            return passed == total;
        }

        private static Dictionary<Dimension, int> CalculateScores(
            ActionLog log,
            int heartsRemaining,
            float timeRemainingSeconds,
            bool lossContained,
            bool cleanupComplete,
            bool ventilationFailed,
            float totalDurationSeconds,
            bool evaluateHearts)
        {
            var scores = new Dictionary<Dimension, int>();
            foreach (Dimension dimension in Enum.GetValues(typeof(Dimension)))
            {
                var dimensionEntries = log.Entries.Where(entry => entry.Dimension == dimension).ToList();
                int score = dimensionEntries.Count == 0
                    ? 0
                    : (int)Math.Round(100d * dimensionEntries.Count(entry => entry.IsCorrect) / dimensionEntries.Count);
                scores[dimension] = score;
            }

            if (evaluateHearts && heartsRemaining < 3)
                scores[Dimension.Safety] = Math.Min(scores[Dimension.Safety], Math.Max(0, heartsRemaining * 33));

            if (ventilationFailed)
                scores[Dimension.Awareness] = 0;

            if (!lossContained)
                scores[Dimension.Execution] = Math.Min(scores[Dimension.Execution], 30);

            if (!cleanupComplete)
                scores[Dimension.Execution] = Math.Min(scores[Dimension.Execution], 60);

            int timeScore = totalDurationSeconds <= 0f
                ? 0
                : (int)Math.Round(100f * Math.Max(0f, Math.Min(timeRemainingSeconds, totalDurationSeconds)) / totalDurationSeconds);
            scores[Dimension.Time] = timeScore;
            return scores;
        }

        private static void CheckCase(
            string name,
            ScenarioResult result,
            Outcome expected,
            Action<string> output,
            ref int passed,
            ref int total)
        {
            total++;
            bool pass = result.Outcome == expected;
            if (pass)
                passed++;
            if (output != null)
                output($"[CA] T06 SelfCheck {(pass ? "PASS" : "FAIL")} {name}: expected={expected}; actual={result.Outcome}");
        }

        private static ActionLog CreateCorrectLog()
        {
            var log = new ActionLog();
            log.Add("ventilation.activate", true, Dimension.Awareness, string.Empty);
            log.Add("sds.inspect", true, Dimension.Awareness, string.Empty);
            log.Add("ppe.equip.correct", true, Dimension.Safety, string.Empty);
            log.Add("spill.contain", true, Dimension.Execution, string.Empty);
            log.Add("cleanup.complete", true, Dimension.Execution, string.Empty);
            return log;
        }

        private static ActionLog CreateIncorrectSafetyLog()
        {
            var log = CreateCorrectLog();
            log.Add("spill.touch.without.ppe", false, Dimension.Safety, "chemical.burn");
            return log;
        }

        private static ActionLog CreateMixedLog()
        {
            var log = CreateCorrectLog();
            log.Add("ppe.equip.decoy", false, Dimension.Decision, "chemical.burn");
            return log;
        }
    }
}
