namespace SI81.Tiziano.ChemicalAlert.Core
{
    /// <summary>
    /// Contract for scenario systems that can restore their initial state.
    /// </summary>
    public interface IResettable
    {
        /// <summary>Restores the system to its initial state.</summary>
        void ResetState();
    }
}
