using System;
using System.Collections.Generic;

namespace SI81.Tiziano.ChemicalAlert.Core
{
    /// <summary>
    /// One significant player action recorded by Chemical Alert.
    /// </summary>
    public sealed class ActionLogEntry
    {
        /// <summary>Creates an action log entry.</summary>
        public ActionLogEntry(double timestamp, string actionId, bool isCorrect, Dimension dimension, string consequenceKey)
        {
            Timestamp = timestamp;
            ActionId = actionId;
            IsCorrect = isCorrect;
            Dimension = dimension;
            ConsequenceKey = consequenceKey ?? string.Empty;
        }

        /// <summary>Gets the action timestamp in scenario seconds.</summary>
        public double Timestamp { get; }
        /// <summary>Gets the stable action identifier.</summary>
        public string ActionId { get; }
        /// <summary>Gets whether the action was correct.</summary>
        public bool IsCorrect { get; }
        /// <summary>Gets the evaluation dimension.</summary>
        public Dimension Dimension { get; }
        /// <summary>Gets the stable consequence key.</summary>
        public string ConsequenceKey { get; }
    }

    /// <summary>
    /// Ordered collection of significant scenario actions.
    /// </summary>
    public sealed class ActionLog
    {
        private readonly List<ActionLogEntry> entries = new List<ActionLogEntry>();
        private double nextTimestamp;

        /// <summary>Gets the recorded entries in chronological insertion order.</summary>
        public IReadOnlyList<ActionLogEntry> Entries => entries;

        /// <summary>Gets the number of recorded actions.</summary>
        public int Count => entries.Count;

        /// <summary>Adds an action using the next deterministic scenario timestamp.</summary>
        public ActionLogEntry Add(string actionId, bool isCorrect, Dimension dimension, string consequenceKey)
        {
            return Add(nextTimestamp++, actionId, isCorrect, dimension, consequenceKey);
        }

        /// <summary>Adds an action with an explicit scenario timestamp.</summary>
        public ActionLogEntry Add(double timestamp, string actionId, bool isCorrect, Dimension dimension, string consequenceKey)
        {
            var entry = new ActionLogEntry(timestamp, actionId, isCorrect, dimension, consequenceKey);
            entries.Add(entry);
            nextTimestamp = Math.Max(nextTimestamp, timestamp + 1d);
            return entry;
        }

        /// <summary>Removes every recorded action and resets the generated timestamp.</summary>
        public void Clear()
        {
            entries.Clear();
            nextTimestamp = 0d;
        }
    }
}
