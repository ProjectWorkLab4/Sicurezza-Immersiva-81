using System;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Base delle quattro righe di Impostazioni (PANEL_SettingsToggle / Slider / Selector / Action): riga 516 × 48 con fondo
    /// surface.card (radius.button; l'ombra hover la gestisce UIStateVisual), etichetta (chiave testo) con descrizione opzionale, binding a uno <see cref="IUISettingsStore"/>
    /// tramite un id, stato disabilitato (riga al 40%) e feedback (suono/aptica) richiesti con un evento statico.
    /// Le sottoclassi disegnano i propri controlli. Nessun Layout Group: le posizioni si calcolano una volta in <see cref="Apply"/>.
    /// Quante righe per scheda (max 7, CLAUDE.md §4.8) lo decide la schermata PANEL_Settings.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public abstract class UISettingsRow : MonoBehaviour
    {
        /// <summary>Id feedback: suono SFX_UIToggle (cambio di un toggle).</summary>
        public const string FEEDBACK_TOGGLE = "SFX_UIToggle";
        /// <summary>Id feedback: suono SFX_UIError (freccia al limite).</summary>
        public const string FEEDBACK_ERROR = "SFX_UIError";
        /// <summary>Id feedback: impulso aptico leggero (tick dello slider ogni 10%).</summary>
        public const string FEEDBACK_TICK = "HAPTIC_Tick";

        /// <summary>
        /// Evento globale: una riga chiede un suono o un impulso aptico (parametro: uno degli id FEEDBACK_*).
        /// Lo ascolta il sistema audio/aptico comune; senza ascoltatori non succede nulla.
        /// </summary>
        public static event Action<string> FeedbackRequested;

        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] protected UITheme theme;
        [Tooltip("Tabella testi SO_UIStrings_It.")]
        [SerializeField] protected UIStrings strings;
        [Tooltip("Id dell'impostazione nello store, es. audio.master_volume.")]
        [SerializeField] private string settingId;
        [Tooltip("Chiave dell'etichetta in SO_UIStrings_It, es. ui.settings.audio.master_volume.")]
        [SerializeField] private string labelKey;
        [Tooltip("Chiave di una descrizione opzionale sotto l'etichetta (vuoto = nessuna). Occupa una seconda riga nei 48 della riga.")]
        [SerializeField] private string descriptionKey;

        /// <summary>Gruppo CanvasGroup della riga.</summary>
        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] protected RoundedPanelGraphic fill;
        /// <summary>CanvasGroup della riga.</summary>
        [SerializeField] protected CanvasGroup group;
        /// <summary>Etichetta.</summary>
        [SerializeField] protected UIStringText label;
        /// <summary>Descrizione.</summary>
        [SerializeField] protected UIStringText description;

        // Store collegato (null = la riga usa il valore locale).
        private IUISettingsStore store;
        // Stato interattivo.
        private bool interactable = true;

        /// <summary>Id dell'impostazione nello store.</summary>
        public string SettingId => settingId;
        /// <summary>Chiave dell'etichetta.</summary>
        public string LabelKey => labelKey;
        /// <summary>Store collegato (null se la riga non è collegata).</summary>
        protected IUISettingsStore Store => store;

        /// <summary>
        /// Stato interattivo: false = riga al 40% (look Disabled), nessuna interazione né hover.
        /// </summary>
        public bool Interactable
        {
            get => interactable;
            set
            {
                if (interactable == value) return;
                interactable = value;
                ApplyInteractable();
            }
        }

        // =====================================================================
        // Ciclo di vita
        // =====================================================================

        /// <summary>
        /// Applica tema, testi, misure e valore all'avvio.
        /// </summary>
        protected virtual void Awake()
        {
            Apply();
        }

        /// <summary>
        /// Si iscrive allo store (se collegato) e rilegge il valore.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (store != null)
            {
                store.Changed -= HandleStoreChanged;
                store.Changed += HandleStoreChanged;
                RefreshFromStore(true);
            }
        }

        /// <summary>
        /// Si disiscrive dallo store.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (store != null) store.Changed -= HandleStoreChanged;
        }

        /// <summary>
        /// Collega la riga a uno store: registra il predefinito della riga (se lo store lo supporta), si iscrive a Changed
        /// e mostra il valore corrente. Si può chiamare più volte (cambia store).
        /// </summary>
        /// <param name="newStore">Store da usare (PlayerPrefsSettingsStore, MockSettingsStore o uno proprio); null = scollega.</param>
        public void Bind(IUISettingsStore newStore)
        {
            if (store != null) store.Changed -= HandleStoreChanged;
            store = newStore;
            if (store == null) return;
            if (store is IUISettingsDefaults defaults) RegisterDefaults(defaults);
            if (isActiveAndEnabled) store.Changed += HandleStoreChanged;
            RefreshFromStore(true);
        }

        /// <summary>
        /// Imposta i dati comuni della riga (id, chiave etichetta, chiave descrizione) da una definizione a runtime. Chiamare PRIMA di
        /// <see cref="Bind"/> (il predefinito si registra nello store con l'id corrente) e dopo i dati specifici del tipo.
        /// </summary>
        /// <param name="newSettingId">Id dell'impostazione nello store, es. audio.master_volume.</param>
        /// <param name="newLabelKey">Chiave dell'etichetta in SO_UIStrings_It.</param>
        /// <param name="newDescriptionKey">Chiave della descrizione (vuota = nessuna).</param>
        public void SetRowData(string newSettingId, string newLabelKey, string newDescriptionKey = "")
        {
            settingId = newSettingId;
            labelKey = newLabelKey;
            descriptionKey = newDescriptionKey ?? string.Empty;
            Apply();
        }

        /// <summary>
        /// Scollega la riga dallo store (la riga resta con il valore mostrato).
        /// </summary>
        public void Unbind()
        {
            Bind(null);
        }

        /// <summary>
        /// Una impostazione dello store è cambiata: se è la nostra (o "tutte") rilegge il valore.
        /// </summary>
        /// <param name="id">Id modificato (vuoto = tutte).</param>
        private void HandleStoreChanged(string id)
        {
            if (string.IsNullOrEmpty(id) || id == settingId) RefreshFromStore(false);
        }

        // =====================================================================
        // Applicazione di tema, testi e misure
        // =====================================================================

        /// <summary>
        /// Applica tutto: colori, testi (etichetta, descrizione), misure della riga e dei controlli, valore e stato interattivo.
        /// </summary>
        public void Apply()
        {
            if (theme == null) return;
            if (fill != null && fill.color != theme.SurfaceCard) fill.color = theme.SurfaceCard;
            ApplyTexts();
            ApplyBaseLayout();
            ApplyControls();
            RefreshFromStore(true);
            ApplyInteractable();
        }

        /// <summary>
        /// Etichetta e descrizione: chiave, stile Body, colori text.primary / text.secondary. La descrizione si vede solo con la chiave.
        /// </summary>
        private void ApplyTexts()
        {
            if (label != null)
            {
                label.SetStyle(TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY);
                if (!string.IsNullOrEmpty(labelKey) && label.Key != labelKey) label.SetKey(labelKey);
            }
            if (description != null)
            {
                bool hasDescription = !string.IsNullOrEmpty(descriptionKey);
                PanelLayout.SetActive(description.gameObject, hasDescription);
                description.SetStyle(TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY);
                if (hasDescription && description.Key != descriptionKey) description.SetKey(descriptionKey);
            }
        }

        /// <summary>
        /// Misure comuni: riga 516 × 48 (la larghezza solo se il RectTransform non è allargato dalle ancore), fondo e ombra
        /// a tutta riga, etichetta a sinistra (padding 16) fino ai controlli (che occupano <see cref="GetControlsWidth"/>).
        /// Con una descrizione l'etichetta sale e la descrizione scende (due righe Body nei 48).
        /// </summary>
        private void ApplyBaseLayout()
        {
            var root = (RectTransform)transform;
            float height = theme.GetNumber(SettingsTokens.ROW_HEIGHT);
            if (Mathf.Abs(root.rect.height - height) > 0.001f && root.anchorMin.y == root.anchorMax.y)
                root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (root.anchorMin.x == root.anchorMax.x)
            {
                float width = theme.GetNumber(SettingsTokens.ROW_WIDTH);
                if (Mathf.Abs(root.rect.width - width) > 0.001f) root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            if (fill != null)
                PanelLayout.SetRect(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            // L'ombra (IMG_Shadow) la dimensiona UIStateVisual (CornerRadius + sprite TEX_SettingsRowShadow*): qui non si tocca.

            float left = theme.GetNumber(SettingsTokens.ROW_PADDING_LEFT);
            float right = theme.GetNumber(SettingsTokens.ROW_PADDING_RIGHT);
            float controls = GetControlsWidth();
            float labelRight = right + (controls > 0f ? controls + theme.GetNumber(SettingsTokens.ROW_GAP) : 0f);
            bool hasDescription = description != null && !string.IsNullOrEmpty(descriptionKey);
            if (label != null)
            {
                if (hasDescription)
                {
                    float line = PanelLayout.LineHeight(theme.Body);
                    float inset = Mathf.Max(0f, (height - 2f * line) * 0.5f);
                    PanelLayout.SetRect((RectTransform)label.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                        new Vector2(left, -(inset + line)), new Vector2(-labelRight, -inset));
                    PanelLayout.SetRect((RectTransform)description.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                        new Vector2(left, -(inset + 2f * line)), new Vector2(-labelRight, -(inset + line)));
                }
                else
                {
                    PanelLayout.SetRect((RectTransform)label.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
                        new Vector2(left, 0f), new Vector2(-labelRight, 0f));
                }
            }
        }

        /// <summary>
        /// Stato disabilitato: riga all'opacità del tema (0,4), nessuna interazione; propaga a ogni UIStateVisual figlio (niente hover).
        /// </summary>
        private void ApplyInteractable()
        {
            if (group != null)
            {
                float alpha = interactable || theme == null ? 1f : theme.GetNumber(SettingsTokens.ROW_DISABLED_OPACITY);
                if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
                if (group.interactable != interactable) group.interactable = interactable;
            }
            foreach (UIStateVisual visual in GetComponentsInChildren<UIStateVisual>(true))
                if (visual.Interactable != interactable) visual.Interactable = interactable;
            OnInteractableChanged();
        }

        /// <summary>
        /// Richiede un suono o un impulso aptico (evento statico <see cref="FeedbackRequested"/>).
        /// </summary>
        /// <param name="feedbackId">Uno degli id FEEDBACK_*.</param>
        protected static void RaiseFeedback(string feedbackId)
        {
            FeedbackRequested?.Invoke(feedbackId);
        }

        /// <summary>
        /// Imposta ancore, pivot, posizione e dimensione di un elemento di dimensione fissa SENZA toccare la scala
        /// (gli elementi con hover cambiano scala a runtime: <see cref="PanelLayout.SetRect"/> la azzererebbe).
        /// </summary>
        /// <param name="rect">Elemento da posizionare.</param>
        /// <param name="anchor">Ancora (e pivot) normalizzata nella riga.</param>
        /// <param name="anchoredPosition">Posizione del pivot rispetto all'ancora.</param>
        /// <param name="size">Dimensione in unità Canvas.</param>
        protected static void SetFixedRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null) return;
            if (rect.anchorMin != anchor) rect.anchorMin = anchor;
            if (rect.anchorMax != anchor) rect.anchorMax = anchor;
            if (rect.pivot != anchor) rect.pivot = anchor;
            if (rect.anchoredPosition != anchoredPosition) rect.anchoredPosition = anchoredPosition;
            if (rect.sizeDelta != size) rect.sizeDelta = size;
        }

        // =====================================================================
        // Parti specifiche delle sottoclassi
        // =====================================================================

        /// <summary>Larghezza occupata dai controlli a destra (senza padding destro né gap dall'etichetta), in unità Canvas.</summary>
        protected abstract float GetControlsWidth();
        /// <summary>Colori e posizioni dei controlli propri della riga.</summary>
        protected abstract void ApplyControls();
        /// <summary>Mostra il valore corrente (dallo store, o locale se non collegata).</summary>
        /// <param name="instant">true = senza animazioni.</param>
        protected abstract void RefreshFromStore(bool instant);
        /// <summary>Registra il predefinito della riga nello store.</summary>
        /// <param name="defaults">Parte "predefiniti" dello store.</param>
        protected abstract void RegisterDefaults(IUISettingsDefaults defaults);
        /// <summary>Chiamato quando cambia lo stato interattivo (le sottoclassi possono aggiornare altro).</summary>
        protected virtual void OnInteractableChanged() { }

#if UNITY_EDITOR
        /// <summary>
        /// In Editor riapplica tutto quando si cambia un campo nell'Inspector (rimandato: in OnValidate Unity non permette di
        /// cambiare RectTransform); ignorato sugli asset prefab non aperti.
        /// </summary>
        protected virtual void OnValidate()
        {
            UIEditorSupport.ApplyDeferred(this, Apply);
        }
#endif

        /// <summary>
        /// Imposta dati e riferimenti comuni. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureBase(UITheme newTheme, UIStrings newStrings, string newSettingId, string newLabelKey, string newDescriptionKey,
            RoundedPanelGraphic newFill, CanvasGroup newGroup, UIStringText newLabel, UIStringText newDescription)
        {
            theme = newTheme;
            strings = newStrings;
            settingId = newSettingId;
            labelKey = newLabelKey;
            descriptionKey = newDescriptionKey;
            fill = newFill;
            group = newGroup;
            label = newLabel;
            description = newDescription;
        }
    }
}
