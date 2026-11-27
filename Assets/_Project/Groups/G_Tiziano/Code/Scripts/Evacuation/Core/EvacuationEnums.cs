namespace SI81.Tiziano.Evacuation.Core
{
    /// <summary>Difficulty levels of the Evacuation scenario (the documents' "Stress" level is Crisis).</summary>
    public enum EvacDifficulty
    {
        /// <summary>Simple evacuation, stopwatch only, hints and highlights, no defeat on time or damage.</summary>
        Guided,
        /// <summary>Simple evacuation, 180 s limit, 3 hearts.</summary>
        Operational,
        /// <summary>Active fire, 120 s limit, 1 heart.</summary>
        Crisis
    }

    /// <summary>Which exit the player reached when the scenario ended.</summary>
    public enum EvacExit
    {
        /// <summary>No exit reached.</summary>
        None,
        /// <summary>Emergency stairs (correct exit).</summary>
        Emergency,
        /// <summary>Internal stairs (wrong exit).</summary>
        Internal
    }

    /// <summary>Severity label of an action log entry (the shared ActionLog only stores a correct/incorrect flag).</summary>
    public enum EvacLogSeverity
    {
        /// <summary>Correct action.</summary>
        Correct,
        /// <summary>Missed action.</summary>
        Missed,
        /// <summary>Risky action.</summary>
        Risky,
        /// <summary>Critical error.</summary>
        Critical
    }
}
