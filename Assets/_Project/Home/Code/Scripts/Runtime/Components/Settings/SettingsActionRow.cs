using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>Tipo di riga azione (Figma UI_SettingsRow_Action, variante Type).</summary>
    public enum SettingsActionType
    {
        /// <summary>Etichetta + pulsante azione (icona + testo Bold), es. "Ripristina".</summary>
        Button,
        /// <summary>Etichetta + valore di sola lettura + lucchetto, es. "Lingua: Italiano".</summary>
        Value
    }

    /// <summary>
    /// Riga PANEL_SettingsAction. Tipo Button: etichetta + BTN_Action (alto 40, fondo tile.neutral, icona 24 + testo Body Bold;
    /// larghezza = 12 + 24 + 6 + testo + 16); il clic lancia <see cref="Triggered"/>. Se <see cref="RequiresConfirmation"/> è
    /// attivo la schermata PANEL_Settings apre PANEL_ConfirmDialog prima di eseguire (es. "Ripristina predefiniti", poi
    /// <see cref="IUISettingsDefaults.ResetToDefaults"/> sullo store). Tipo Value: valore in text.secondary + lucchetto (non interattivo).
    /// Questa riga non scrive nello store: l'azione la decide chi ascolta l'evento.
    /// </summary>
    public class SettingsActionRow : UISettingsRow
    {
        [Header("Action")]
        [SerializeField] private SettingsActionType actionType = SettingsActionType.Button;
        [Tooltip("Identificativo dell'azione per chi ascolta l'evento, es. reset_defaults, recalibrate, replay_training, open_info.")]
        [SerializeField] private string actionId;
        [Tooltip("Se attivo, la schermata deve chiedere conferma (PANEL_ConfirmDialog) prima di eseguire l'azione.")]
        [SerializeField] private bool requiresConfirmation;
        [Tooltip("Icona ICN_ del pulsante (es. ICN_SettingsBackupRestore, ICN_Refresh, ICN_PlayArrow, ICN_Info).")]
        [SerializeField] private Sprite actionIcon;
        [Tooltip("Chiave del testo del pulsante, es. ui.settings.general.reset.")]
        [SerializeField] private string actionTextKey;
        [Tooltip("Chiave del valore di sola lettura (tipo Value), es. ui.settings.general.language_italian.")]
        [SerializeField] private string valueKey;

        [Header("Pulsante (tipo Button)")]
        [SerializeField] private GameObject buttonRoot;
        [SerializeField] private Button actionButton;
        [SerializeField] private RoundedPanelGraphic actionFill;
        [SerializeField] private Image actionIconImage;
        [SerializeField] private UIStringText actionLabel;

        [Header("Valore (tipo Value)")]
        [SerializeField] private UIStringText valueLabel;
        [SerializeField] private Image lockIcon;

        // Larghezza calcolata dei controlli (pulsante, oppure valore + lucchetto).
        private float controlsWidth;

        /// <summary>Lanciato quando l'utente preme il pulsante azione (parametro: questa riga).</summary>
        public event Action<SettingsActionRow> Triggered;

        /// <summary>Identificativo dell'azione.</summary>
        public string ActionId => actionId;
        /// <summary>true se la schermata deve chiedere conferma prima di eseguire.</summary>
        public bool RequiresConfirmation => requiresConfirmation;
        /// <summary>Tipo di riga.</summary>
        public SettingsActionType ActionType => actionType;

        /// <summary>
        /// Collega il clic del pulsante, poi applica.
        /// </summary>
        protected override void Awake()
        {
            if (actionButton != null) actionButton.onClick.AddListener(PerformAction);
            base.Awake();
        }

        /// <summary>
        /// Scollega il clic del pulsante.
        /// </summary>
        private void OnDestroy()
        {
            if (actionButton != null) actionButton.onClick.RemoveListener(PerformAction);
        }

        /// <summary>
        /// Imposta tipo, azione, icona e testi da una definizione a runtime (poi <see cref="UISettingsRow.SetRowData"/>, che ricalcola il layout).
        /// </summary>
        /// <param name="newType">Button oppure Value (sola lettura).</param>
        /// <param name="newActionId">Identificativo dell'azione (es. reset_defaults).</param>
        /// <param name="newRequiresConfirmation">true = la schermata chiede conferma prima di eseguire.</param>
        /// <param name="newIcon">Icona ICN_ del pulsante (null = quella del prefab).</param>
        /// <param name="newTextKey">Chiave del testo del pulsante.</param>
        /// <param name="newValueKey">Chiave del valore (tipo Value).</param>
        public void SetActionData(SettingsActionType newType, string newActionId, bool newRequiresConfirmation, Sprite newIcon, string newTextKey, string newValueKey = "")
        {
            actionType = newType;
            actionId = newActionId;
            requiresConfirmation = newRequiresConfirmation;
            if (newIcon != null) actionIcon = newIcon;
            actionTextKey = newTextKey;
            valueKey = newValueKey ?? string.Empty;
        }

        /// <summary>
        /// Esegue l'azione: lancia <see cref="Triggered"/> (solo tipo Button, riga interattiva).
        /// </summary>
        public void PerformAction()
        {
            if (!Interactable || actionType != SettingsActionType.Button) return;
            Triggered?.Invoke(this);
        }

        /// <summary>
        /// Scrive un valore dinamico (tipo Value) senza passare da una chiave (es. nome letto da un dato).
        /// </summary>
        /// <param name="text">Testo già in italiano.</param>
        public void SetValueText(string text)
        {
            if (valueLabel == null) return;
            valueLabel.GetComponent<TMP_Text>().text = text;
            // La larghezza del testo è cambiata: ricalcola le posizioni.
            Apply();
        }

        /// <summary>
        /// Testi del pulsante e del valore dalle chiavi; mostra solo i controlli del tipo scelto; calcola la larghezza dei controlli.
        /// </summary>
        private void ApplyActionTexts()
        {
            bool isButton = actionType == SettingsActionType.Button;
            if (buttonRoot != null) PanelLayout.SetActive(buttonRoot, isButton);
            if (valueLabel != null) PanelLayout.SetActive(valueLabel.gameObject, !isButton);
            if (lockIcon != null) PanelLayout.SetActive(lockIcon.gameObject, !isButton);

            float iconSize = theme.GetNumber(SettingsTokens.ROW_ICON);
            if (isButton)
            {
                if (actionLabel != null)
                {
                    actionLabel.SetStyle(TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY);
                    if (!string.IsNullOrEmpty(actionTextKey) && actionLabel.Key != actionTextKey) actionLabel.SetKey(actionTextKey);
                }
                controlsWidth = theme.GetNumber(SettingsTokens.ACTION_PADDING_LEFT) + iconSize + theme.GetNumber(SettingsTokens.ACTION_GAP)
                                + Measure(actionLabel, theme.BodyBold) + theme.GetNumber(SettingsTokens.ACTION_PADDING_RIGHT);
            }
            else
            {
                if (valueLabel != null)
                {
                    valueLabel.SetStyle(TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY);
                    if (!string.IsNullOrEmpty(valueKey) && valueLabel.Key != valueKey) valueLabel.SetKey(valueKey);
                }
                controlsWidth = Measure(valueLabel, theme.Body) + theme.GetNumber(SettingsTokens.ACTION_GAP) + iconSize;
            }
        }

        /// <summary>
        /// Larghezza del testo con lo stile dato, arrotondata per eccesso (0 se manca qualcosa).
        /// </summary>
        /// <param name="text">Testo (con UIStringText).</param>
        /// <param name="style">Stile del tema con cui si misura.</param>
        /// <returns>Larghezza in unità Canvas.</returns>
        private static float Measure(UIStringText text, UITheme.TextStyle style)
        {
            if (text == null || style == null || style.Font == null) return 0f;
            TMP_Text tmp = text.GetComponent<TMP_Text>();
            if (tmp == null) return 0f;
            TMP_FontAsset shownFont = tmp.font;
            float shownSize = tmp.fontSize;
            tmp.font = style.Font;
            tmp.fontSize = style.Size;
            float width = tmp.GetPreferredValues(tmp.text, float.PositiveInfinity, float.PositiveInfinity).x;
            tmp.font = shownFont;
            tmp.fontSize = shownSize;
            return Mathf.Ceil(width);
        }

        /// <inheritdoc/>
        protected override float GetControlsWidth()
        {
            if (theme == null) return 0f;
            ApplyActionTexts();
            return controlsWidth;
        }

        /// <summary>
        /// Posiziona e colora pulsante (icona + testo) oppure valore + lucchetto.
        /// </summary>
        protected override void ApplyControls()
        {
            float padRight = theme.GetNumber(SettingsTokens.ROW_PADDING_RIGHT);
            float iconSize = theme.GetNumber(SettingsTokens.ROW_ICON);
            var anchor = new Vector2(1f, 0.5f);
            if (actionType == SettingsActionType.Button)
            {
                float padLeft = theme.GetNumber(SettingsTokens.ACTION_PADDING_LEFT);
                float padEnd = theme.GetNumber(SettingsTokens.ACTION_PADDING_RIGHT);
                float gap = theme.GetNumber(SettingsTokens.ACTION_GAP);
                if (buttonRoot != null)
                    SetFixedRect((RectTransform)buttonRoot.transform, anchor, new Vector2(-padRight, 0f),
                        new Vector2(controlsWidth, theme.GetNumber(SettingsTokens.ACTION_HEIGHT)));
                if (actionFill != null)
                {
                    PanelLayout.SetRect(actionFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    if (actionFill.color != theme.TileNeutral) actionFill.color = theme.TileNeutral;
                }
                if (actionIconImage != null)
                {
                    if (actionIcon != null && actionIconImage.sprite != actionIcon) actionIconImage.sprite = actionIcon;
                    if (actionIconImage.color != theme.TextPrimary) actionIconImage.color = theme.TextPrimary;
                    PanelLayout.SetRect(actionIconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(padLeft, -iconSize * 0.5f), new Vector2(padLeft + iconSize, iconSize * 0.5f));
                }
                if (actionLabel != null)
                    PanelLayout.SetRect((RectTransform)actionLabel.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
                        new Vector2(padLeft + iconSize + gap, 0f), new Vector2(-padEnd, 0f));
            }
            else
            {
                if (lockIcon != null)
                {
                    if (lockIcon.color != theme.TextSecondary) lockIcon.color = theme.TextSecondary;
                    SetFixedRect(lockIcon.rectTransform, anchor, new Vector2(-padRight, 0f), new Vector2(iconSize, iconSize));
                }
                if (valueLabel != null)
                {
                    float textWidth = Mathf.Max(0f, controlsWidth - iconSize - theme.GetNumber(SettingsTokens.ACTION_GAP));
                    SetFixedRect((RectTransform)valueLabel.transform, anchor,
                        new Vector2(-(padRight + iconSize + theme.GetNumber(SettingsTokens.ACTION_GAP)), 0f),
                        new Vector2(textWidth, PanelLayout.LineHeight(theme.Body)));
                }
            }
        }

        /// <summary>L'azione non ha un valore nello store: niente da mostrare.</summary>
        protected override void RefreshFromStore(bool instant) { }

        /// <summary>L'azione non ha un predefinito.</summary>
        protected override void RegisterDefaults(IUISettingsDefaults defaults) { }

        /// <summary>
        /// Imposta riferimenti e dati propri. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureAction(SettingsActionType newType, string newActionId, bool newRequiresConfirmation, Sprite newIcon, string newTextKey,
            string newValueKey, GameObject newButtonRoot, Button newButton, RoundedPanelGraphic newFill, Image newIconImage, UIStringText newLabel,
            UIStringText newValueLabel, Image newLock)
        {
            actionType = newType;
            actionId = newActionId;
            requiresConfirmation = newRequiresConfirmation;
            actionIcon = newIcon;
            actionTextKey = newTextKey;
            valueKey = newValueKey;
            buttonRoot = newButtonRoot;
            actionButton = newButton;
            actionFill = newFill;
            actionIconImage = newIconImage;
            actionLabel = newLabel;
            valueLabel = newValueLabel;
            lockIcon = newLock;
        }
    }
}
