using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>Stile del pulsante (FIGMA_SPECS: BTN_Primary / BTN_Secondary Neutral e Danger).</summary>
    public enum UIButtonStyle
    {
        /// <summary>BTN_Primary: riempimento state.active, bordo di 2 unità (CLAUDE.md §4.2), testo nero.</summary>
        Primary,
        /// <summary>BTN_Secondary Neutral: riempimento surface.card (hover tile.neutral), nessun bordo.</summary>
        Secondary,
        /// <summary>BTN_Secondary Danger: riempimento deny, testo NERO (mai bianco), nessun bordo.</summary>
        Danger
    }

    /// <summary>Superficie su cui sta BTN_Primary: decide il colore del bordo (border.on-dark / border.on-light).</summary>
    public enum UIButtonSurface
    {
        /// <summary>Su pannello vetro scuro: bordo bianco.</summary>
        Dark,
        /// <summary>Su card bianca: bordo brand.dark.</summary>
        Light
    }

    /// <summary>
    /// Base di tutti i pulsanti con riempimento, etichetta e icona (BTN_Primary, BTN_Secondary, BTN_Back, e le sottoclassi
    /// BTN_Badge, BTN_SettingsTab, BTN_ListRow). Gestisce: chiave dell'etichetta (UIStringText, mai testo scritto nel prefab),
    /// icona iniziale opzionale (ICN_ tinta dal tema), stile, interattività, evento di clic e i COLORI per stato
    /// (riempimento con dissolvenza, testo, icona, bordo). Scala, ombra, glow e spunta li fa <see cref="UIStateVisual"/>
    /// sullo stesso GameObject. Nessun bordo su hover/selezione/pressed (CLAUDE.md §4.1); l'unico bordo è quello di BTN_Primary (§4.2).
    /// Le sottoclassi cambiano solo i metodi virtuali (colori, layout): nessun codice duplicato.
    /// Tutti i valori (colori, misure, stili di testo) vengono da SO_UITheme; i testi da SO_UIStrings_It. Nessun Layout Group.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(UIStateVisual))]
    [AddComponentMenu("SI81/UI/UI Button")]
    public class UIButton : MonoBehaviour
    {
        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] protected UITheme theme;
        [Tooltip("Stile: Primary, Secondary (Neutral) o Danger.")]
        [SerializeField] protected UIButtonStyle style = UIButtonStyle.Primary;
        [Tooltip("Superficie su cui sta il pulsante (solo per il bordo di BTN_Primary).")]
        [SerializeField] protected UIButtonSurface surface = UIButtonSurface.Dark;
        [Tooltip("Chiave dell'etichetta in SO_UIStrings_It, es. ui.button.start_scenario.")]
        [SerializeField] protected string labelKey;
        [Tooltip("Chiave dell'etichetta da disabilitato (BTN_Primary mostra il MOTIVO, es. ui.button.choose_level). Vuoto = resta l'etichetta normale.")]
        [SerializeField] protected string disabledLabelKey;
        [Tooltip("Icona iniziale (ICN_), es. ICN_PlayArrow. Vuoto = nessuna icona.")]
        [SerializeField] protected Sprite iconSprite;
        [Tooltip("Larghezza fissa in unità Canvas (BTN_Primary 200). 0 = larghezza secondo il contenuto.")]
        [SerializeField] protected float fixedWidth;

        /// <summary>Componente Button del prefab.</summary>
        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] protected Button button;
        /// <summary>Stati visivi del pulsante.</summary>
        [SerializeField] protected UIStateVisual stateVisual;
        /// <summary>Riempimento.</summary>
        [SerializeField] protected Image fill;
        /// <summary>Bordo.</summary>
        [SerializeField] protected Image border;
        /// <summary>Etichetta.</summary>
        [SerializeField] protected UIStringText label;
        /// <summary>Icona.</summary>
        [SerializeField] protected Image icon;

        // Animazioni di colore in corso.
        private Coroutine fillRoutine;
        // Testo libero (dato, non chiave) mostrato al posto della chiave.
        private string customText;

        /// <summary>Evento: il pulsante è stato cliccato (solo se interattivo).</summary>
        public event Action Clicked;

        /// <summary>UnityEvent del clic (Button.onClick), per collegare azioni dall'Inspector.</summary>
        public Button.ButtonClickedEvent OnClick => button != null ? button.onClick : null;
        /// <summary>Componente degli stati visivi (scala, ombra, glow, spunta).</summary>
        public UIStateVisual StateVisual => stateVisual;
        /// <summary>Chiave dell'etichetta attuale.</summary>
        public string LabelKey => labelKey;
        /// <summary>Stile del pulsante.</summary>
        public UIButtonStyle Style => style;

        /// <summary>
        /// true = pulsante cliccabile. false = aspetto Disabled (token button.disabled, testo nero; per BTN_Primary: niente
        /// icona, niente bordo, etichetta = motivo).
        /// </summary>
        public bool Interactable
        {
            get => stateVisual != null ? stateVisual.Interactable : (button == null || button.interactable);
            set
            {
                if (button != null) button.interactable = value;
                if (stateVisual != null) stateVisual.Interactable = value;
                Apply();
            }
        }

        /// <summary>
        /// Cambia la chiave dell'etichetta mostrata da disabilitato (il motivo, es. "ui.home.unavailable") e aggiorna il pulsante.
        /// </summary>
        /// <param name="key">Chiave di SO_UIStrings_It; vuota = etichetta normale.</param>
        public void SetDisabledLabelKey(string key)
        {
            disabledLabelKey = key;
            Apply();
        }

        /// <summary>
        /// Collega i pulsanti e applica l'aspetto iniziale.
        /// </summary>
        protected virtual void Awake()
        {
            ResolveReferences();
            if (stateVisual != null) stateVisual.StateChanged += OnStateChanged;
            if (button != null) button.onClick.AddListener(HandleClick);
            Apply();
        }

        /// <summary>
        /// All'attivazione riapplica l'aspetto senza transizioni.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (stateVisual != null) ApplyLook(stateVisual.State, true);
        }

        /// <summary>
        /// Rilascia i collegamenti.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (stateVisual != null) stateVisual.StateChanged -= OnStateChanged;
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>
        /// Recupera i riferimenti sullo stesso GameObject se il builder non li ha assegnati.
        /// </summary>
        private void ResolveReferences()
        {
            if (button == null) button = GetComponent<Button>();
            if (stateVisual == null) stateVisual = GetComponent<UIStateVisual>();
        }

        /// <summary>
        /// Cambia la chiave dell'etichetta e riapplica.
        /// </summary>
        /// <param name="newKey">Chiave in SO_UIStrings_It.</param>
        public void SetLabelKey(string newKey)
        {
            labelKey = newKey;
            customText = null;
            Apply();
        }

        /// <summary>
        /// Mostra un testo già pronto (dato, es. nome di scenario) al posto di una chiave.
        /// </summary>
        /// <param name="text">Testo da mostrare.</param>
        public void SetLabelText(string text)
        {
            customText = text;
            Apply();
        }

        /// <summary>
        /// Cambia l'icona iniziale (null = nessuna icona) e riapplica.
        /// </summary>
        /// <param name="sprite">Sprite ICN_ o null.</param>
        public void SetIcon(Sprite sprite)
        {
            iconSprite = sprite;
            Apply();
        }

        /// <summary>
        /// Cambia stile e superficie (es. BTN_Primary Light dentro una card bianca, BTN_Secondary Danger nel dialogo) e riapplica.
        /// </summary>
        /// <param name="newStyle">Primary, Secondary o Danger.</param>
        /// <param name="newSurface">Dark (su vetro) o Light (su card bianca): decide il bordo di BTN_Primary.</param>
        public void SetStyle(UIButtonStyle newStyle, UIButtonSurface newSurface)
        {
            style = newStyle;
            surface = newSurface;
            Apply();
        }

        /// <summary>
        /// Cambia solo la superficie (bordo di BTN_Primary: border.on-dark o border.on-light) e riapplica.
        /// </summary>
        /// <param name="newSurface">Dark o Light.</param>
        public void SetSurface(UIButtonSurface newSurface)
        {
            SetStyle(style, newSurface);
        }

        /// <summary>
        /// Applica tutto: etichetta, icona, misure e colori dello stato corrente (senza transizioni).
        /// </summary>
        public virtual void Apply()
        {
            if (theme == null) return;
            ResolveReferences();
            UIVisualState state = stateVisual != null ? stateVisual.State : UIVisualState.Normal;
            ApplyLabel(state);
            ApplyIcon(state);
            LayoutContent(state);
            ApplyLook(state, true);
        }

        /// <summary>
        /// Chiave, stile e colore dell'etichetta secondo lo stato.
        /// </summary>
        protected virtual void ApplyLabel(UIVisualState state)
        {
            if (label == null) return;
            TMP_Text tmp = label.GetComponent<TMP_Text>();
            if (!string.IsNullOrEmpty(customText))
            {
                if (label.Key != string.Empty) label.SetKey(string.Empty);
                if (tmp != null && tmp.text != customText) tmp.text = customText;
            }
            else
            {
                string key = state == UIVisualState.Disabled && !string.IsNullOrEmpty(disabledLabelKey) ? disabledLabelKey : labelKey;
                if (!string.IsNullOrEmpty(key) && label.Key != key) label.SetKey(key);
            }
            label.SetStyle(GetLabelStyle(state), GetLabelColorToken(state));
        }

        /// <summary>
        /// Sprite e visibilità dell'icona iniziale.
        /// </summary>
        protected virtual void ApplyIcon(UIVisualState state)
        {
            if (icon == null) return;
            Sprite sprite = GetIconSprite(state);
            if (icon.sprite != sprite) icon.sprite = sprite;
            PanelLayout.SetActive(icon.gameObject, sprite != null && ShowIcon(state));
        }

        /// <summary>Sprite dell'icona per lo stato (le sottoclassi possono scegliere contorno/piena).</summary>
        protected virtual Sprite GetIconSprite(UIVisualState state)
        {
            return iconSprite;
        }

        /// <summary>true se l'icona è visibile nello stato (BTN_Primary la nasconde da disabilitato).</summary>
        protected virtual bool ShowIcon(UIVisualState state)
        {
            return !(style == UIButtonStyle.Primary && state == UIVisualState.Disabled);
        }

        /// <summary>
        /// Misure Figma: altezza 48, padding-x 16, icona 24, gap 8, contenuto centrato; larghezza fissa (BTN_Primary 200) o
        /// secondo il contenuto (BTN_Secondary). Il bordo di BTN_Primary rientra il riempimento di border.primary.
        /// </summary>
        protected virtual void LayoutContent(UIVisualState state)
        {
            var root = (RectTransform)transform;
            float height = theme.GetNumber(ButtonTokens.HEIGHT);
            float padding = theme.GetNumber(ButtonTokens.PADDING_X);
            float gap = theme.GetNumber(ButtonTokens.GAP);
            float iconSize = theme.GetNumber(ButtonTokens.ICON);
            bool iconVisible = icon != null && icon.gameObject.activeSelf;
            TMP_Text tmp = label != null ? label.GetComponent<TMP_Text>() : null;
            float labelWidth = UIComponentUtil.MeasureWidth(tmp);

            float content = labelWidth + (iconVisible ? iconSize + gap : 0f);
            float width = fixedWidth > 0f ? fixedWidth : Mathf.Ceil(padding * 2f + content);
            UIComponentUtil.SetSize(root, width, height);

            // Contenuto (icona + gap + etichetta) centrato.
            float left = -content * 0.5f;
            if (iconVisible) UIComponentUtil.SetCentered((RectTransform)icon.transform, new Vector2(left + iconSize * 0.5f, 0f), new Vector2(iconSize, iconSize));
            if (label != null)
            {
                float labelLeft = left + (iconVisible ? iconSize + gap : 0f);
                UIComponentUtil.SetCentered((RectTransform)label.transform, new Vector2(labelLeft + labelWidth * 0.5f, 0f), new Vector2(labelWidth, height));
            }
            if (border != null) UIComponentUtil.SetInset(border.rectTransform, 0f);
            if (fill != null) UIComponentUtil.SetInset(fill.rectTransform, HasBorder(state) ? theme.GetNumber(NumberTokens.BORDER_PRIMARY) : 0f);
        }

        /// <summary>
        /// Colori dello stato: riempimento (con dissolvenza), bordo, testo e icona.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        /// <param name="instant">true = senza dissolvenza.</param>
        protected virtual void ApplyLook(UIVisualState state, bool instant)
        {
            if (theme == null) return;
            bool fade = !instant && stateVisual != null && !stateVisual.LastRefreshInstant;
            Color fillColor = GetFillColor(state);
            UIComponentUtil.SetColor(this, ref fillRoutine, fill, fillColor, theme.Motion.HoverDuration, !fade);

            if (border != null)
            {
                bool hasBorder = HasBorder(state);
                PanelLayout.SetActive(border.gameObject, hasBorder);
                if (hasBorder) border.color = theme.GetColor(surface == UIButtonSurface.Dark ? ColorTokens.BORDER_ON_DARK : ColorTokens.BORDER_ON_LIGHT);
                if (fill != null) UIComponentUtil.SetInset(fill.rectTransform, hasBorder ? theme.GetNumber(NumberTokens.BORDER_PRIMARY) : 0f);
            }

            // Etichetta e icona: stessa tinta (token del tema).
            Color textColor = theme.GetColor(GetLabelColorToken(state));
            if (icon != null && icon.color != textColor) icon.color = textColor;
            if (label != null) label.SetStyle(GetLabelStyle(state), GetLabelColorToken(state));
        }

        /// <summary>true se nello stato c'è il bordo (solo BTN_Primary non disabilitato, CLAUDE.md §4.2).</summary>
        protected virtual bool HasBorder(UIVisualState state)
        {
            return style == UIButtonStyle.Primary && state != UIVisualState.Disabled;
        }

        /// <summary>
        /// Colore di riempimento per stato: Primary = state.active; Secondary = surface.card (hover tile.neutral);
        /// Danger = deny; Disabled = button.disabled.
        /// </summary>
        protected virtual Color GetFillColor(UIVisualState state)
        {
            if (state == UIVisualState.Disabled) return theme.ButtonDisabled;
            switch (style)
            {
                case UIButtonStyle.Primary: return theme.StateActive;
                case UIButtonStyle.Danger: return theme.Deny;
                default: return state == UIVisualState.Hover ? theme.TileNeutral : theme.SurfaceCard;
            }
        }

        /// <summary>Stile di testo del tema per lo stato (Subtitle Bold 24).</summary>
        protected virtual string GetLabelStyle(UIVisualState state)
        {
            return TextStyleIds.SUBTITLE_BOLD;
        }

        /// <summary>Token colore del testo e dell'icona: nero (text.primary), mai bianco; da disabilitato button.disabled-text.</summary>
        protected virtual string GetLabelColorToken(UIVisualState state)
        {
            return state == UIVisualState.Disabled ? ColorTokens.BUTTON_DISABLED_TEXT : ColorTokens.TEXT_PRIMARY;
        }

        /// <summary>
        /// Lo stato visivo è cambiato: ricolora.
        /// </summary>
        private void OnStateChanged(UIVisualState state)
        {
            if (theme == null) return;
            // Il passaggio da/verso Disabled cambia etichetta, icona e misure.
            if (state == UIVisualState.Disabled || lastAppliedDisabled)
            {
                lastAppliedDisabled = state == UIVisualState.Disabled;
                Apply();
                return;
            }
            ApplyLook(state, false);
        }

        // true se l'ultimo stato applicato era Disabled (per rifare etichetta e misure all'uscita).
        private bool lastAppliedDisabled;

        /// <summary>
        /// Clic del Button uGUI: rilancia l'evento e chiama <see cref="OnClicked"/>.
        /// </summary>
        private void HandleClick()
        {
            if (!Interactable) return;
            OnClicked();
            Clicked?.Invoke();
        }

        /// <summary>
        /// Azione del pulsante (le sottoclassi la sovrascrivono: es. BTN_Back torna indietro).
        /// </summary>
        protected virtual void OnClicked()
        {
        }

#if UNITY_EDITOR
        /// <summary>
        /// In Editor riapplica quando si cambia un campo nell'Inspector (rimandato: in OnValidate non si possono cambiare i RectTransform).
        /// </summary>
        private void OnValidate()
        {
            UIEditorSupport.ApplyDeferred(this, Apply);
        }
#endif

        /// <summary>
        /// Imposta tema, riferimenti e dati e applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIButtonStyle newStyle, UIButtonSurface newSurface, string newLabelKey, string newDisabledKey,
            Sprite newIcon, float newFixedWidth, Button newButton, UIStateVisual newStateVisual, Image newFill, Image newBorder,
            UIStringText newLabel, Image newIconImage)
        {
            theme = newTheme;
            style = newStyle;
            surface = newSurface;
            labelKey = newLabelKey;
            disabledLabelKey = newDisabledKey;
            iconSprite = newIcon;
            fixedWidth = newFixedWidth;
            button = newButton;
            stateVisual = newStateVisual;
            fill = newFill;
            border = newBorder;
            label = newLabel;
            icon = newIconImage;
            Apply();
        }
    }
}
