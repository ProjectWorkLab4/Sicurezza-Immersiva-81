namespace SI81.Tiziano.ChemicalAlert.Core
{
    /// <summary>
    /// The five dimensions used to evaluate Chemical Alert performance.
    /// </summary>
    public enum Dimension
    {
        Safety,
        Awareness,
        Decision,
        Execution,
        Time
    }

    /// <summary>
    /// Final scenario outcome.
    /// </summary>
    public enum Outcome
    {
        Success,
        Partial,
        Failure
    }
}
