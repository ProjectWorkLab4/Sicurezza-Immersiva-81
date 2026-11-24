using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Componente di MNG_UI che crea i servizi comuni in Awake e li pubblica in <see cref="UIServices"/>.
    /// Con <c>useMocks</c> (sandbox) usa dati e lanciatore finti; altrimenti progressi e impostazioni su PlayerPrefs
    /// e lanciatore che carica le scene. Parte prima di UIScreenManager, così le schermate trovano i servizi già pronti.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class UIServicesBootstrap : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: prima di UIScreenManager (-10001).</summary>
        public const int EXECUTION_ORDER = -10010;

        [Tooltip("Catalogo scenari SO_MissionCatalog.")]
        [SerializeField] private MissionCatalog catalog;
        [Tooltip("Tema comune SO_UITheme: soglie del punteggio (score.stars.N.min) e del badge rapido (badge.fast_run.seconds).")]
        [SerializeField] private UITheme theme;
        [Tooltip("true = dati e lanciatore finti (sandbox); false = PlayerPrefs e caricamento scene reali.")]
        [SerializeField] private bool useMocks = true;
        [Tooltip("Solo con i finti: parte con i risultati di esempio (true) o con un profilo vuoto (false).")]
        [SerializeField] private bool seedMockProgress = true;
        [Tooltip("Solo con i finti: secondi dopo i quali un risultato finto chiude la missione avviata (0 = mai, si usano i pulsanti di debug).")]
        [SerializeField] private float mockResultDelaySeconds = 0f;
        [Tooltip("Solo senza finti: scena di menu da caricare al ritorno (vuota se il menu sta in SCN_Core).")]
        [SerializeField] private string menuSceneName = string.Empty;

        /// <summary>Catalogo assegnato.</summary>
        public MissionCatalog Catalog => catalog;
        /// <summary>true se usa i servizi finti.</summary>
        public bool UseMocks => useMocks;

        /// <summary>
        /// Crea e pubblica i servizi.
        /// </summary>
        private void Awake()
        {
            if (catalog == null) Debug.LogError("[MNG_UI] UIServicesBootstrap: SO_MissionCatalog non assegnato.", this);
            // Soglie stelle e badge rapido dai token del tema (una sola fonte, niente costanti duplicate).
            ScoreRules.UseTheme(theme);
            if (useMocks)
            {
                UIServices.Configure(catalog, new MockProgressService(catalog, seedMockProgress), new MockSettingsStore(),
                    new MockMissionLauncher(this, mockResultDelaySeconds));
            }
            else
            {
                UIServices.Configure(catalog, new PlayerPrefsProgressService(catalog), new PlayerPrefsSettingsStore(),
                    new SceneMissionLauncher(menuSceneName));
            }
            // Predefiniti delle impostazioni subito (non solo quando si apre PANEL_Settings): HUD, sottotitoli e polso li leggono dall'inizio.
            SettingsCatalog.RegisterDefaults(UIServices.Settings);
            UINavigationContext.Reset();
        }

        /// <summary>
        /// Toglie i servizi quando MNG_UI viene distrutto, se sono ancora quelli creati qui.
        /// </summary>
        private void OnDestroy()
        {
            UIServices.Clear();
        }

        /// <summary>
        /// Imposta catalogo, modalità e tema. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(MissionCatalog newCatalog, bool newUseMocks, UITheme newTheme)
        {
            theme = newTheme;
            catalog = newCatalog;
            useMocks = newUseMocks;
        }
    }
}
