namespace SI81.UI
{
    /// <summary>
    /// Stato di navigazione condiviso tra le schermate del menu: chi apre una schermata imposta qui che cosa mostrare
    /// (es. Home imposta <see cref="SelectedMission"/> e poi chiama UIScreenManager.Show(MissionBrief)).
    /// Statico e senza logica: si azzera all'avvio dei servizi.
    /// </summary>
    public static class UINavigationContext
    {
        /// <summary>Scenario scelto (letto da Mission Brief; null = nessuno).</summary>
        public static MissionDefinition SelectedMission;
        /// <summary>Livello scelto in Mission Brief (null = nessuno, si usa l'ultimo giocato o Guided).</summary>
        public static DifficultyLevel? SelectedLevel;
        /// <summary>Riga selezionata in Statistiche (-1 = nessuna, si mostra il Riepilogo).</summary>
        public static int StatsSelectedIndex = -1;
        /// <summary>Scheda attiva di Impostazioni (0 = Comfort ... 5 = Generale).</summary>
        public static int SettingsTab;

        /// <summary>
        /// Rimette tutto ai valori iniziali.
        /// </summary>
        public static void Reset()
        {
            SelectedMission = null;
            SelectedLevel = null;
            StatsSelectedIndex = -1;
            SettingsTab = 0;
        }
    }
}
