using System.Collections.Generic;

namespace SI81.Tiziano.ChemicalAlert.Core
{
    /// <summary>
    /// Immutable evaluation result exposed to the scenario boundary.
    /// </summary>
    public sealed class ScenarioResult
    {
        private readonly Dictionary<Dimension, int> subScores;

        /// <summary>Creates a scenario result with one score per evaluation dimension.</summary>
        public ScenarioResult(Outcome outcome, IDictionary<Dimension, int> scores, ActionLog actionLog)
        {
            Outcome = outcome;
            subScores = new Dictionary<Dimension, int>();
            foreach (Dimension dimension in System.Enum.GetValues(typeof(Dimension)))
            {
                int score = 0;
                if (scores != null && scores.ContainsKey(dimension))
                    score = scores[dimension];
                subScores[dimension] = ClampScore(score);
            }

            ActionLog = actionLog;
        }

        /// <summary>Gets the final scenario outcome.</summary>
        public Outcome Outcome { get; }
        /// <summary>Gets the action log used for this result.</summary>
        public ActionLog ActionLog { get; }
        /// <summary>Gets all dimension sub-scores from 0 to 100.</summary>
        public IReadOnlyDictionary<Dimension, int> SubScores => subScores;

        /// <summary>Gets one dimension score from 0 to 100.</summary>
        public int GetSubScore(Dimension dimension)
        {
            return subScores[dimension];
        }

        private static int ClampScore(int score)
        {
            return score < 0 ? 0 : score > 100 ? 100 : score;
        }
    }
}
