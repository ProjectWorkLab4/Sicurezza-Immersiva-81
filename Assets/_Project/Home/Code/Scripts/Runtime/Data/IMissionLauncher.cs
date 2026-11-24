namespace SI81.UI
{
    /// <summary>
    /// Avvia le missioni e riporta al menu. Le schermate (Mission Brief, Training Brief, Score &amp; Replay, Pausa)
    /// lo usano da <see cref="UIServices.Launcher"/>, così non conoscono né scene né gestione del caricamento.
    /// </summary>
    public interface IMissionLauncher
    {
        /// <summary>Indica se lo scenario si può avviare davvero (la sua scena esiste nella build). Le schermate disattivano
        /// card e pulsanti di avvio quando è false, così nessun pulsante resta "morto".</summary>
        /// <param name="m">Scenario o Training.</param>
        /// <returns>true se Launch/LaunchTraining lo avvierebbero.</returns>
        bool CanLaunch(MissionDefinition m);
        /// <summary>Avvia uno scenario al livello scelto.</summary>
        /// <param name="m">Scenario.</param>
        /// <param name="level">Livello.</param>
        void Launch(MissionDefinition m, DifficultyLevel level);
        /// <summary>Avvia il Training comune.</summary>
        /// <param name="training">Definizione del Training (SO_Mission_Training).</param>
        void LaunchTraining(MissionDefinition training);
        /// <summary>Torna al menu: chiude la missione, mostra il menu e va a Home.</summary>
        void ReturnToHome();
    }
}
