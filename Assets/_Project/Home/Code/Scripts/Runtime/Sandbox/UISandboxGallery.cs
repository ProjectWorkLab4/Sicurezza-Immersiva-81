using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SI81.UI.Sandbox
{
    /// <summary>Tipo di componente mostrato da una voce della galleria (decide quali dati finti applicare).</summary>
    public enum GalleryRole
    {
        /// <summary>BTN_Primary.</summary>
        Primary,
        /// <summary>BTN_Secondary in stile Secondary.</summary>
        Secondary,
        /// <summary>BTN_Secondary in stile Danger.</summary>
        Danger,
        /// <summary>BTN_Back.</summary>
        Back,
        /// <summary>BTN_Badge (Variant 0 = sbloccato, 1 = bloccato).</summary>
        Badge,
        /// <summary>BTN_SettingsTab (attiva se lo stato è Selected).</summary>
        SettingsTab,
        /// <summary>BTN_ListRow (Variant 0 = giocata, 1 = non giocata).</summary>
        ListRow,
        /// <summary>LBL_LevelBadge (Variant = livello 1-3, Dark = su fondo scuro).</summary>
        LevelBadge,
        /// <summary>LBL_StarRating (Variant = stelle 0-5).</summary>
        StarRating,
        /// <summary>LBL_DimensionBar (Variant = DimensionKind, Dark = su fondo scuro).</summary>
        DimensionBar,
        /// <summary>PANEL_StatTile (Variant = StatAccent).</summary>
        StatTile,
        /// <summary>LBL_Outcome (Variant = OutcomeType, Dark = su vetro, altrimenti su card).</summary>
        Outcome,
        /// <summary>LBL_ObjectiveItem (Variant = ObjectiveState).</summary>
        Objective,
        /// <summary>PANEL_SettingsToggle.</summary>
        SettingsToggle,
        /// <summary>PANEL_SettingsSlider.</summary>
        SettingsSlider,
        /// <summary>PANEL_SettingsSelector.</summary>
        SettingsSelector,
        /// <summary>PANEL_SettingsAction (Variant 0 = pulsante con conferma, 1 = valore di sola lettura).</summary>
        SettingsAction,
        /// <summary>BTN_ScenarioCard (Variant 0 = con stelle, 1 = nuovo; Disabled = non disponibile).</summary>
        ScenarioCard,
        /// <summary>PANEL_TrainingCard.</summary>
        TrainingCard,
        /// <summary>BTN_DifficultyCard (Variant = livello 1-3; Disabled = bloccata; Selected = selezionata).</summary>
        DifficultyCard
    }

    /// <summary>
    /// Una voce della galleria: istanza del prefab, ruolo, variante dei dati finti e stato visivo da forzare.
    /// </summary>
    [Serializable]
    public class GalleryItem
    {
        /// <summary>Istanza del prefab nella galleria.</summary>
        public GameObject Target;
        /// <summary>Ruolo (decide i dati finti).</summary>
        public GalleryRole Role;
        /// <summary>Variante dei dati (significato per ruolo, vedi <see cref="GalleryRole"/>).</summary>
        public int Variant;
        /// <summary>true = componente su superficie scura (vetro) invece che su card bianca.</summary>
        public bool Dark;
        /// <summary>Stato visivo mostrato.</summary>
        public UIVisualState State;
        /// <summary>Indice della pagina che contiene la voce.</summary>
        public int Page;
    }

    /// <summary>
    /// SOLO per SCN_UISandbox: galleria di tutti i componenti del Blocco A su pagine, con dati finti e ogni stato forzato
    /// (<see cref="UIStateVisual.ForceState"/>) fianco a fianco. Le righe di Impostazioni sono collegate a un
    /// <see cref="MockSettingsStore"/>; PANEL_ConfirmDialog si apre da un pulsante o dall'azione "Ripristina".
    /// I dati finti vivono qui (non nei prefab): gli scenari non usano questo componente.
    /// </summary>
    [DisallowMultipleComponent]
    public class UISandboxGallery : MonoBehaviour
    {
        [Header("Pagine")]
        [Tooltip("Radice di ogni pagina (si mostra una pagina alla volta).")]
        [SerializeField] private GameObject[] pages = new GameObject[0];
        [Tooltip("Testo con il numero della pagina (solo sandbox).")]
        [SerializeField] private TMP_Text pageCaption;
        [Tooltip("Pulsante che passa alla pagina successiva (BTN_Secondary).")]
        [SerializeField] private UIButton nextButton;
        [Tooltip("Titoli delle pagine (solo sandbox).")]
        [SerializeField] private string[] pageTitles = new string[0];

        [Header("Voci")]
        [Tooltip("Tutti i componenti della galleria.")]
        [SerializeField] private List<GalleryItem> items = new List<GalleryItem>();

        [Header("Dialogo e icone")]
        [Tooltip("PANEL_ConfirmDialog (salvato disattivo nel prefab).")]
        [SerializeField] private ConfirmDialog dialog;
        [Tooltip("Pulsante della galleria che apre il dialogo.")]
        [SerializeField] private UIButton openDialogButton;
        [Tooltip("Icone piene per badge e card: [0] premio, [1] scuola, [2] fuoco, [3] impostazioni.")]
        [SerializeField] private Sprite[] icons = new Sprite[0];
        [Tooltip("Icone contorno/piene della scheda impostazioni: [0] contorno, [1] piena, [2] ripristina.")]
        [SerializeField] private Sprite[] tabIcons = new Sprite[0];

        // Store in memoria delle righe di Impostazioni (si azzera a ogni avvio).
        private MockSettingsStore store;
        // Pagina mostrata.
        private int currentPage;

        /// <summary>Store usato dalle righe (per le prove: lettura e scrittura dei valori).</summary>
        public MockSettingsStore Store => store;
        /// <summary>Dialogo di conferma della galleria.</summary>
        public ConfirmDialog Dialog => dialog;
        /// <summary>Numero di pagine.</summary>
        public int PageCount => pages.Length;
        /// <summary>Pagina mostrata (0 = prima).</summary>
        public int CurrentPage => currentPage;
        /// <summary>Voci della galleria.</summary>
        public IReadOnlyList<GalleryItem> Items => items;

        /// <summary>
        /// Crea lo store, applica i dati finti e gli stati a tutte le voci e collega i pulsanti.
        /// </summary>
        private void Start()
        {
            store = new MockSettingsStore();
            // Tutte le pagine attive durante la preparazione: i componenti ricevono Awake/OnEnable prima dei dati finti.
            foreach (GameObject page in pages)
                if (page != null) page.SetActive(true);
            foreach (GalleryItem item in items)
            {
                if (item == null || item.Target == null) continue;
                try
                {
                    Populate(item);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[SI81 UI] Galleria: errore su {item.Target.name} ({item.Role}): {exception}", item.Target);
                }
            }
            if (nextButton != null) nextButton.Clicked += NextPage;
            if (openDialogButton != null) openDialogButton.Clicked += OpenDialog;
            ShowPage(0);
        }

        /// <summary>
        /// Scollega i pulsanti.
        /// </summary>
        private void OnDestroy()
        {
            if (nextButton != null) nextButton.Clicked -= NextPage;
            if (openDialogButton != null) openDialogButton.Clicked -= OpenDialog;
        }

        /// <summary>
        /// Passa alla pagina successiva (torna alla prima dopo l'ultima).
        /// </summary>
        public void NextPage()
        {
            if (pages.Length == 0) return;
            ShowPage((currentPage + 1) % pages.Length);
        }

        /// <summary>
        /// Mostra una pagina e nasconde le altre; aggiorna il testo di pagina.
        /// </summary>
        /// <param name="index">Indice della pagina (0 = prima).</param>
        public void ShowPage(int index)
        {
            if (pages.Length == 0) return;
            currentPage = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null) pages[i].SetActive(i == currentPage);
            // Disattivare una pagina azzera hover e pressione dei suoi componenti (UIStateVisual.OnDisable): si rimostrano ora.
            foreach (GalleryItem item in items)
            {
                if (item == null || item.Target == null || item.Page != currentPage) continue;
                if (item.State == UIVisualState.Hover || item.State == UIVisualState.Pressed) ApplyState(item);
            }
            if (pageCaption != null)
            {
                string title = currentPage < pageTitles.Length ? pageTitles[currentPage] : string.Empty;
                pageCaption.text = $"{currentPage + 1}/{pages.Length} · {title}";
            }
        }

        /// <summary>
        /// Apre PANEL_ConfirmDialog con i testi "Riavvia" (esempio) e callback che scrivono in Console.
        /// </summary>
        public void OpenDialog()
        {
            ShowDialog("ui.dialog.restart.title", "ui.dialog.restart.body", "ui.button.restart", "ui.button.cancel", null, null);
        }

        /// <summary>
        /// Apre il dialogo con i testi e le callback dati.
        /// </summary>
        /// <param name="titleKey">Chiave del titolo.</param>
        /// <param name="messageKey">Chiave del messaggio.</param>
        /// <param name="confirmKey">Chiave del pulsante di conferma.</param>
        /// <param name="cancelKey">Chiave del pulsante di annullamento.</param>
        /// <param name="onConfirm">Callback di conferma (facoltativa).</param>
        /// <param name="onCancel">Callback di annullamento (facoltativa).</param>
        public void ShowDialog(string titleKey, string messageKey, string confirmKey, string cancelKey, Action onConfirm, Action onCancel)
        {
            if (dialog == null) return;
            dialog.Show(titleKey, messageKey, confirmKey, cancelKey, onConfirm, onCancel);
        }

        /// <summary>
        /// Applica a una voce i dati finti del suo ruolo e poi lo stato visivo forzato.
        /// </summary>
        /// <param name="item">Voce della galleria.</param>
        private void Populate(GalleryItem item)
        {
            GameObject go = item.Target;
            int v = item.Variant;
            LabelSurface surface = item.Dark ? LabelSurface.Dark : LabelSurface.Light;
            switch (item.Role)
            {
                case GalleryRole.Secondary:
                    go.GetComponent<UIButton>().SetStyle(UIButtonStyle.Secondary, UIButtonSurface.Dark);
                    break;
                case GalleryRole.Danger:
                    go.GetComponent<UIButton>().SetStyle(UIButtonStyle.Danger, UIButtonSurface.Dark);
                    break;
                case GalleryRole.Badge:
                    go.GetComponent<BadgeButton>().SetData(v == 0, Icon(0));
                    break;
                case GalleryRole.SettingsTab:
                    var tab = go.GetComponent<SettingsTabButton>();
                    tab.SetData(TabIcon(0), TabIcon(1), "ui.settings.tab.comfort");
                    tab.Active = item.State == UIVisualState.Selected;
                    break;
                case GalleryRole.ListRow:
                    var row = go.GetComponent<ListRowButton>();
                    if (v == 0) row.SetData("Incendio in magazzino", 4, 86);
                    else row.SetNotPlayed("Fuga di gas");
                    row.Selected = item.State == UIVisualState.Selected;
                    break;
                case GalleryRole.LevelBadge:
                    var badge = go.GetComponent<LevelBadge>();
                    badge.SetLevel(v);
                    badge.SetSurface(surface);
                    break;
                case GalleryRole.StarRating:
                    go.GetComponent<StarRating>().SetStars(v);
                    break;
                case GalleryRole.DimensionBar:
                    var bar = go.GetComponent<DimensionBar>();
                    bar.SetSurface(surface);
                    bar.SetDimension((DimensionKind)v, 70 + v * 6);
                    break;
                case GalleryRole.StatTile:
                    go.GetComponent<StatTile>().SetStat((StatAccent)v, 5 - v);
                    break;
                case GalleryRole.Outcome:
                    var outcome = go.GetComponent<OutcomeLabel>();
                    outcome.SetOutcome((OutcomeType)v);
                    outcome.SetPresentation(item.Dark ? OutcomePresentation.OnGlass : OutcomePresentation.OnCard);
                    break;
                case GalleryRole.Objective:
                    go.GetComponent<ObjectiveItem>().SetObjective("Raggiungi l'uscita di emergenza", (ObjectiveState)v);
                    break;
                case GalleryRole.SettingsToggle:
                    var toggle = go.GetComponent<SettingsToggleRow>();
                    toggle.SetRowData("gallery.subtitles", "ui.settings.subtitles.enabled");
                    toggle.SetToggleData(v == 0);
                    toggle.Bind(store);
                    break;
                case GalleryRole.SettingsSlider:
                    var slider = go.GetComponent<SettingsSliderRow>();
                    slider.SetRowData("gallery.volume", "ui.settings.audio.master_volume");
                    slider.SetSliderData(0f, 100f, 5f, 80f, "ui.format.percent");
                    slider.Bind(store);
                    break;
                case GalleryRole.SettingsSelector:
                    var selector = go.GetComponent<SettingsSelectorRow>();
                    selector.SetRowData("gallery.turn", "ui.settings.comfort.turn");
                    selector.SetSelectorData(new List<SettingsOption>
                    {
                        new SettingsOption { Value = 0, LabelKey = "ui.settings.comfort.turn_snap" },
                        new SettingsOption { Value = 1, LabelKey = "ui.settings.comfort.turn_continuous" }
                    }, 0);
                    selector.Bind(store);
                    break;
                case GalleryRole.SettingsAction:
                    var action = go.GetComponent<SettingsActionRow>();
                    if (v == 0)
                    {
                        action.SetRowData("gallery.reset", "ui.settings.general.reset_defaults");
                        action.SetActionData(SettingsActionType.Button, "reset_defaults", true, TabIcon(2), "ui.settings.general.reset");
                        action.Triggered += OnSettingsAction;
                    }
                    else
                    {
                        action.SetRowData("gallery.language", "ui.settings.general.language");
                        action.SetActionData(SettingsActionType.Value, "language", false, null, string.Empty, "ui.settings.general.language_italian");
                    }
                    action.Bind(store);
                    break;
                case GalleryRole.ScenarioCard:
                    go.GetComponent<ScenarioCard>().Set(new ScenarioCardData
                    {
                        Id = "gallery." + v,
                        Title = v == 0 ? "Incendio in magazzino" : "Fuga di gas",
                        Icon = Icon(2),
                        BestStars = v == 0 ? 4 : (int?)null,
                        Complexity = 3,
                        LevelsCompleted = v == 0 ? 2 : 0,
                        Unlocked = item.State != UIVisualState.Disabled
                    });
                    break;
                case GalleryRole.DifficultyCard:
                    var difficulty = go.GetComponent<DifficultyCard>();
                    string[] descriptions = { "ui.level.guided_desc", "ui.level.operational_desc", "ui.level.crisis_desc" };
                    difficulty.Set((DifficultyLevel)Mathf.Clamp(v, 1, 3), descriptions[Mathf.Clamp(v, 1, 3) - 1], item.State == UIVisualState.Disabled);
                    difficulty.Selected = item.State == UIVisualState.Selected;
                    break;
            }
            ApplyState(item);
        }

        /// <summary>
        /// Mostra lo stato visivo della voce: i componenti con dati propri (disabilitato/bloccato) lo ricevono dall'API, gli altri
        /// da <see cref="UIStateVisual.ForceState"/>.
        /// </summary>
        /// <param name="item">Voce della galleria.</param>
        private void ApplyState(GalleryItem item)
        {
            GameObject go = item.Target;
            if (item.State == UIVisualState.Disabled)
            {
                var button = go.GetComponent<UIButton>();
                var settingsRow = go.GetComponent<UISettingsRow>();
                if (button != null) button.Interactable = false;
                else if (settingsRow != null) settingsRow.Interactable = false;
                return;
            }
            // Componenti senza UIStateVisual (etichette) o con stato dato solo da dati (Selected già impostato sopra).
            UIStateVisual visual = go.GetComponentInChildren<UIStateVisual>(true);
            if (visual == null) return;
            if (item.State == UIVisualState.Normal && item.Role != GalleryRole.ScenarioCard) return;
            visual.ForceState(item.State);
        }

        /// <summary>
        /// Azione "Ripristina" di Impostazioni: chiede conferma e ripristina i predefiniti dello store.
        /// </summary>
        /// <param name="row">Riga che ha lanciato l'azione.</param>
        private void OnSettingsAction(SettingsActionRow row)
        {
            if (!row.RequiresConfirmation) return;
            ShowDialog("ui.dialog.reset_defaults.title", "ui.dialog.reset_defaults.body", "ui.settings.general.reset", "ui.button.cancel",
                () => store.ResetToDefaults(), null);
        }

        /// <summary>
        /// Icona piena dell'elenco serializzato (null se assente).
        /// </summary>
        /// <param name="index">Indice in icons.</param>
        /// <returns>Sprite o null.</returns>
        private Sprite Icon(int index)
        {
            return index < icons.Length ? icons[index] : null;
        }

        /// <summary>
        /// Icona della scheda impostazioni / ripristina (null se assente).
        /// </summary>
        /// <param name="index">Indice in tabIcons.</param>
        /// <returns>Sprite o null.</returns>
        private Sprite TabIcon(int index)
        {
            return index < tabIcons.Length ? tabIcons[index] : null;
        }

        /// <summary>
        /// Imposta pagine, voci, dialogo, pulsanti e icone. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(GameObject[] newPages, string[] newTitles, TMP_Text newCaption, UIButton newNext, List<GalleryItem> newItems,
            ConfirmDialog newDialog, UIButton newOpenDialog, Sprite[] newIcons, Sprite[] newTabIcons)
        {
            pages = newPages;
            pageTitles = newTitles;
            pageCaption = newCaption;
            nextButton = newNext;
            items = newItems;
            dialog = newDialog;
            openDialogButton = newOpenDialog;
            icons = newIcons;
            tabIcons = newTabIcons;
        }
    }
}
