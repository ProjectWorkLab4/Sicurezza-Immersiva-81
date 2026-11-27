namespace SI81.Tiziano.Evacuation.Core
{
    /// <summary>Ordered phases of the Evacuation scenario.</summary>
    public enum EvacPhase
    {
        /// <summary>Waiting to start: timer stopped, nothing happens until StartScenario.</summary>
        Briefing,
        /// <summary>Scenario running: alarm active, timer/stopwatch counting, reports accepted.</summary>
        Action,
        /// <summary>Scenario ended: outcome being evaluated, reports ignored.</summary>
        Resolution,
        /// <summary>Result published through ResultReady, waiting for retry/reset.</summary>
        ResultHandoff
    }

    /// <summary>Readable danger level for the future UI (Down = a serious error was just signalled).</summary>
    public enum EvacDangerLevel
    {
        /// <summary>Serious error signalled (e.g. elevator used in Guided).</summary>
        Down,
        /// <summary>Neutral state.</summary>
        Mid,
        /// <summary>Reserved for positive feedback (E08).</summary>
        Up
    }
}
