using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_Settings (Figma Impostazioni): 6 schede a sinistra (BTN_SettingsTab) e a destra le righe della scheda attiva
    /// (max 7, PANEL_Settings{Toggle,Slider,Selector,Action}). Le righe sono GIÀ costruite dal builder, una famiglia per scheda sotto
    /// <c>PANEL_Rows/PANEL_Rows{Scheda}</c>: cambiare scheda mostra un gruppo e nasconde gli altri (nessun Layout Group a runtime).
    /// Le righe si collegano a <see cref="UIServices.Settings"/> (<see cref="IUISettingsStore"/>) con gli id del <see cref="SettingsCatalog"/>.
    /// La scheda scelta si ricorda in <see cref="UINavigationContext.SettingsTab"/>. "Ripristina impostazioni" apre PANEL_ConfirmDialog
    /// (chiavi ui.dialog.reset_defaults.*) e poi chiama <see cref="IUISettingsDefaults.ResetToDefaults"/> sullo store.
    /// Uso: istanza di PANEL_Settings sotto PANEL_Screens (UIScreen con id Settings); nessuna configurazione a mano.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Settings Screen")]
    public class SettingsScreen : MonoBehaviour
    {
        [Header("Riferimenti (assegnati dal builder)")]
        [Tooltip("Schede, nell'ordine di SettingsTabId (Comfort, Audio, Sottotitoli, Controlli, Accessibilità, Generale).")]
        [SerializeField] private SettingsTabButton[] tabButtons = new SettingsTabButton[0];
        [Tooltip("Gruppi di righe, uno per scheda, nello stesso ordine delle schede.")]
        [SerializeField] private RectTransform[] rowGroups = new RectTransform[0];
        [Tooltip("Dialogo di conferma (istanza di PANEL_ConfirmDialog, salvata disattiva).")]
        [SerializeField] private ConfirmDialog confirmDialog;
        [Tooltip("Icona del pulsante di conferma del ripristino (ICN_SettingsBackupRestore).")]
        [SerializeField] private Sprite resetIcon;

        /// <summary>Lanciato dalla riga "Altezza utente → Ricalibra": il rig condiviso può rimisurare l'altezza.</summary>
        public static event Action RecalibrateRequested;
        /// <summary>Lanciato dalla riga "Informazioni → Apri" (contenuto non ancora progettato: punto aperto).</summary>
        public static event Action InfoRequested;

        // Righe di tutte le schede (raccolte una volta).
        private readonly List<UISettingsRow> allRows = new List<UISettingsRow>();
        // Righe azione già collegate (per scollegarsi).
        private readonly List<SettingsActionRow> actionRows = new List<SettingsActionRow>();
        // true dopo la prima raccolta dei riferimenti.
        private bool collected;
        // Scheda mostrata (-1 = nessuna).
        private int activeTab = -1;
        // true se in attesa di UIServices.Ready.
        private bool waitingForServices;

        /// <summary>Indice della scheda attiva (-1 se nessuna).</summary>
        public int ActiveTab => activeTab;

        /// <summary>
        /// Collega i clic delle schede e delle righe azione (una volta sola).
        /// </summary>
        private void Awake()
        {
            CollectRows();
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                int index = i;
                tabButtons[i].Clicked += () => SelectTab(index);
            }
        }

        /// <summary>
        /// Alla comparsa: collega le righe allo store (o attende i servizi) e mostra l'ultima scheda usata.
        /// </summary>
        private void OnEnable()
        {
            if (UIServices.IsReady) BindStore();
            else if (!waitingForServices)
            {
                waitingForServices = true;
                UIServices.Ready += HandleServicesReady;
            }
            SelectTab(UINavigationContext.SettingsTab);
        }

        /// <summary>
        /// Alla scomparsa: chiude il dialogo e smette di attendere i servizi.
        /// </summary>
        private void OnDisable()
        {
            if (waitingForServices)
            {
                waitingForServices = false;
                UIServices.Ready -= HandleServicesReady;
            }
            if (confirmDialog != null) confirmDialog.Hide();
        }

        /// <summary>
        /// Scollega le righe azione.
        /// </summary>
        private void OnDestroy()
        {
            foreach (SettingsActionRow row in actionRows)
                if (row != null) row.Triggered -= HandleActionTriggered;
            actionRows.Clear();
        }

        /// <summary>
        /// I servizi sono pronti: collega lo store.
        /// </summary>
        private void HandleServicesReady()
        {
            if (waitingForServices)
            {
                waitingForServices = false;
                UIServices.Ready -= HandleServicesReady;
            }
            BindStore();
        }

        /// <summary>
        /// Raccoglie le righe dei gruppi e si iscrive agli eventi delle righe azione.
        /// </summary>
        private void CollectRows()
        {
            if (collected) return;
            collected = true;
            foreach (RectTransform group in rowGroups)
            {
                if (group == null) continue;
                foreach (UISettingsRow row in group.GetComponentsInChildren<UISettingsRow>(true))
                {
                    allRows.Add(row);
                    if (row is SettingsActionRow action)
                    {
                        action.Triggered += HandleActionTriggered;
                        actionRows.Add(action);
                    }
                }
            }
        }

        /// <summary>
        /// Registra i predefiniti di tutte le impostazioni nello store e collega ogni riga (anche delle schede nascoste).
        /// </summary>
        private void BindStore()
        {
            IUISettingsStore store = UIServices.Settings;
            if (store == null) return;
            SettingsCatalog.RegisterDefaults(store);
            foreach (UISettingsRow row in allRows)
                if (row != null) row.Bind(store);
        }

        /// <summary>
        /// Mostra la scheda indicata (gruppo di righe visibile, scheda attiva evidenziata) e la ricorda per la prossima apertura.
        /// </summary>
        /// <param name="index">Indice della scheda (0-5).</param>
        public void SelectTab(int index)
        {
            if (rowGroups.Length == 0) return;
            index = Mathf.Clamp(index, 0, rowGroups.Length - 1);
            activeTab = index;
            UINavigationContext.SettingsTab = index;
            for (int i = 0; i < rowGroups.Length; i++)
                if (rowGroups[i] != null) PanelLayout.SetActive(rowGroups[i].gameObject, i == index);
            for (int i = 0; i < tabButtons.Length; i++)
                if (tabButtons[i] != null) tabButtons[i].Active = i == index;
        }

        /// <summary>
        /// Una riga azione è stata premuta: esegue l'azione (o chiede prima conferma).
        /// </summary>
        /// <param name="row">Riga che ha lanciato l'evento.</param>
        private void HandleActionTriggered(SettingsActionRow row)
        {
            if (row == null) return;
            if (row.RequiresConfirmation && row.ActionId == SettingsCatalog.ACTION_RESET_DEFAULTS && confirmDialog != null)
            {
                // Fuoco su "Annulla": ripristinare è distruttivo. Conferma = icona e testo "Ripristina".
                confirmDialog.Show("ui.dialog.reset_defaults.title", "ui.dialog.reset_defaults.body", "ui.settings.general.reset", "ui.button.cancel",
                    ResetDefaults, null, resetIcon);
                return;
            }
            RunAction(row.ActionId);
        }

        /// <summary>
        /// Esegue un'azione per id.
        /// </summary>
        /// <param name="actionId">Id dell'azione (vedi SettingsCatalog.ACTION_*).</param>
        private void RunAction(string actionId)
        {
            switch (actionId)
            {
                case SettingsCatalog.ACTION_RECALIBRATE:
                    // Riporta il menu davanti alla testa e avvisa il rig (rimisura altezza).
                    if (UIScreenManager.Instance != null) UIScreenManager.Instance.Recenter();
                    RecalibrateRequested?.Invoke();
                    break;
                case SettingsCatalog.ACTION_REPLAY_TRAINING:
                    // Il flusso "Avvia" del training non è progettato: si apre il brief del training (con il pulsante Avvia training).
                    if (UIScreenManager.Instance != null) UIScreenManager.Instance.Show(UIScreenId.TrainingBrief);
                    break;
                case SettingsCatalog.ACTION_INFO:
                    InfoRequested?.Invoke();
                    break;
                case SettingsCatalog.ACTION_RESET_DEFAULTS:
                    ResetDefaults();
                    break;
            }
        }

        /// <summary>
        /// Riporta tutte le impostazioni ai predefiniti (le righe si rileggono da sole: lo store lancia Changed con id vuoto).
        /// </summary>
        public void ResetDefaults()
        {
            if (UIServices.Settings is IUISettingsDefaults defaults) defaults.ResetToDefaults();
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(SettingsTabButton[] newTabs, RectTransform[] newGroups, ConfirmDialog newDialog, Sprite newResetIcon)
        {
            tabButtons = newTabs;
            rowGroups = newGroups;
            confirmDialog = newDialog;
            resetIcon = newResetIcon;
        }
    }
}
