using System;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Punto di accesso statico ai servizi comuni della UI: catalogo scenari, progressi, impostazioni, lanciatore.
    /// Li imposta <see cref="UIServicesBootstrap"/> (su MNG_UI) in Awake. Le schermate, se <see cref="IsReady"/> è false,
    /// si iscrivono a <see cref="Ready"/> e si popolano appena i servizi esistono.
    /// </summary>
    public static class UIServices
    {
        /// <summary>Catalogo degli scenari (SO_MissionCatalog).</summary>
        public static MissionCatalog Catalog { get; private set; }
        /// <summary>Progressi del giocatore.</summary>
        public static IProgressService Progress { get; private set; }
        /// <summary>Impostazioni (store comune con le righe di PANEL_Settings*).</summary>
        public static IUISettingsStore Settings { get; private set; }
        /// <summary>Lanciatore delle missioni.</summary>
        public static IMissionLauncher Launcher { get; private set; }
        /// <summary>true quando i servizi sono stati impostati.</summary>
        public static bool IsReady { get; private set; }

        /// <summary>Lanciato una volta quando i servizi diventano disponibili (e di nuovo se vengono sostituiti).</summary>
        public static event Action Ready;

        /// <summary>
        /// Imposta i servizi e lancia <see cref="Ready"/>.
        /// </summary>
        /// <param name="catalog">Catalogo scenari.</param>
        /// <param name="progress">Servizio dei progressi.</param>
        /// <param name="settings">Store delle impostazioni.</param>
        /// <param name="launcher">Lanciatore delle missioni.</param>
        public static void Configure(MissionCatalog catalog, IProgressService progress, IUISettingsStore settings, IMissionLauncher launcher)
        {
            Catalog = catalog;
            Progress = progress;
            Settings = settings;
            Launcher = launcher;
            IsReady = true;
            Ready?.Invoke();
        }

        /// <summary>
        /// Azzera i servizi (uscita dal gioco o prove).
        /// </summary>
        public static void Clear()
        {
            Catalog = null;
            Progress = null;
            Settings = null;
            Launcher = null;
            IsReady = false;
        }

        /// <summary>
        /// Azzera lo stato statico all'avvio del Play Mode (utile con il "Domain Reload" disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Clear();
            Ready = null;
        }
    }
}
