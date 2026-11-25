using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Una scelta del selettore: valore salvato nello store (intero) e testo mostrato, da una chiave fissa
    /// (es. "ui.settings.comfort.locomotion_teleport") oppure da un formato con il valore (es. "ui.format.degrees" → "45°").
    /// </summary>
    [Serializable]
    public class SettingsOption
    {
        /// <summary>Valore intero salvato nello store (es. 0/1 per due modalità, 45 per gli angoli).</summary>
        [Tooltip("Valore intero salvato nello store (es. 0/1 per due modalità, 45 per gli angoli).")]
        public int Value;
        /// <summary>Chiave del testo mostrato (ha la precedenza sul formato).</summary>
        [Tooltip("Chiave del testo mostrato (ha la precedenza sul formato).")]
        public string LabelKey;
        /// <summary>Chiave di formato con il valore come {0}, es. ui.format.percent (usata se LabelKey è vuota).</summary>
        [Tooltip("Chiave di formato con il valore come {0}, es. ui.format.percent (usata se LabelKey è vuota).")]
        public string FormatKey;
    }

    /// <summary>
    /// Riga PANEL_SettingsSelector: etichetta + selettore a frecce "‹ valore ›" (nessun TMP_Dropdown, CLAUDE.md §3).
    /// Le scelte sono dati (<see cref="SettingsOption"/>): il valore salvato è il numero della scelta, il testo viene dalle chiavi.
    /// Frecce 40 × 40, valore Bold largo 130 (il testo lungo si rimpicciolisce fino a 14). Al limite la freccia si attenua e il clic
    /// richiede SFX_UIError (a meno che <c>wrap</c> sia attivo: allora si riparte dall'altro capo).
    /// </summary>
    public class SettingsSelectorRow : UISettingsRow
    {
        [Header("Selector")]
        [Tooltip("Valore predefinito (uno dei Value delle scelte).")]
        [SerializeField] private int defaultValue;
        [Tooltip("Scelte, nell'ordine in cui si scorrono con le frecce.")]
        [SerializeField] private List<SettingsOption> options = new List<SettingsOption>();
        [Tooltip("Se attivo, oltre l'ultima scelta si riparte dalla prima (e viceversa) e le frecce non si attenuano.")]
        [SerializeField] private bool wrap;
        [SerializeField] private SettingsArrowButton leftArrow;
        [SerializeField] private SettingsArrowButton rightArrow;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private UIStringText valueStyle;

        // Valore locale quando la riga non è collegata a uno store (parte dal predefinito alla prima lettura).
        private int localValue;
        private bool localInitialized;

        /// <summary>Lanciato quando la scelta cambia; parametro: il Value della nuova scelta.</summary>
        public event Action<int> ValueChanged;

        /// <summary>Scelte disponibili (lettura).</summary>
        public IReadOnlyList<SettingsOption> Options => options;
        /// <summary>Valore corrente (dallo store se collegato).</summary>
        public int Value => Store != null ? Store.GetInt(SettingId) : LocalValue;

        /// <summary>Valore locale: alla prima lettura parte dal predefinito (vale anche in Editor, dove Awake non gira).</summary>
        private int LocalValue
        {
            get
            {
                if (!localInitialized)
                {
                    localValue = defaultValue;
                    localInitialized = true;
                }
                return localValue;
            }
        }

        /// <summary>
        /// Si iscrive alle frecce, poi applica.
        /// </summary>
        protected override void Awake()
        {
            if (leftArrow != null) leftArrow.Clicked += HandleLeft;
            if (rightArrow != null) rightArrow.Clicked += HandleRight;
            base.Awake();
        }

        /// <summary>
        /// Si disiscrive dalle frecce.
        /// </summary>
        private void OnDestroy()
        {
            if (leftArrow != null) leftArrow.Clicked -= HandleLeft;
            if (rightArrow != null) rightArrow.Clicked -= HandleRight;
        }

        /// <summary>
        /// Imposta scelte, predefinito e wrap da una definizione a runtime (poi <see cref="UISettingsRow.SetRowData"/> e <see cref="UISettingsRow.Bind"/>).
        /// </summary>
        /// <param name="newOptions">Scelte nell'ordine delle frecce.</param>
        /// <param name="newDefault">Value della scelta predefinita.</param>
        /// <param name="newWrap">true = oltre l'ultima si riparte dalla prima.</param>
        public void SetSelectorData(List<SettingsOption> newOptions, int newDefault, bool newWrap = false)
        {
            options = newOptions ?? new List<SettingsOption>();
            defaultValue = newDefault;
            wrap = newWrap;
            localInitialized = false;
        }

        /// <summary>Freccia sinistra: scelta precedente.</summary>
        private void HandleLeft() { Step(-1); }
        /// <summary>Freccia destra: scelta successiva.</summary>
        private void HandleRight() { Step(1); }

        /// <summary>
        /// Sposta la scelta di una posizione. Al limite (senza wrap) non cambia nulla e richiede il suono di errore.
        /// </summary>
        /// <param name="direction">-1 = precedente, +1 = successiva.</param>
        public void Step(int direction)
        {
            if (!Interactable || options.Count == 0) return;
            int next = CurrentIndex() + direction;
            if (next < 0 || next >= options.Count)
            {
                if (!wrap)
                {
                    RaiseFeedback(FEEDBACK_ERROR);
                    return;
                }
                next = (next + options.Count) % options.Count;
            }
            SetValue(options[next].Value);
        }

        /// <summary>
        /// Imposta la scelta con il Value dato (se esiste tra le scelte), la scrive nello store e avvisa chi ascolta.
        /// </summary>
        /// <param name="value">Value di una delle scelte.</param>
        public void SetValue(int value)
        {
            if (IndexOf(value) < 0 || Value == value) return;
            if (Store != null) Store.SetInt(SettingId, value);
            else localValue = value;
            localInitialized = true;
            RefreshFromStore(true);
            ValueChanged?.Invoke(value);
        }

        /// <summary>
        /// Posizione nella lista del Value dato (-1 se assente).
        /// </summary>
        private int IndexOf(int value)
        {
            for (int i = 0; i < options.Count; i++)
                if (options[i] != null && options[i].Value == value) return i;
            return -1;
        }

        /// <summary>
        /// Posizione della scelta corrente; se il valore salvato non è tra le scelte, quella predefinita (o la prima).
        /// </summary>
        private int CurrentIndex()
        {
            int index = IndexOf(Value);
            if (index < 0) index = IndexOf(defaultValue);
            return Mathf.Max(0, index);
        }

        /// <inheritdoc/>
        protected override float GetControlsWidth()
        {
            if (theme == null) return 0f;
            float arrow = theme.GetNumber(SettingsTokens.SELECTOR_ARROW);
            float gap = theme.GetNumber(SettingsTokens.SELECTOR_GAP);
            return arrow + gap + theme.GetNumber(SettingsTokens.SELECTOR_VALUE_WIDTH) + gap + arrow;
        }

        /// <summary>
        /// Posiziona freccia destra (padding 8), valore (gap 4) e freccia sinistra, e colora le frecce.
        /// </summary>
        protected override void ApplyControls()
        {
            float padRight = theme.GetNumber(SettingsTokens.ROW_PADDING_RIGHT);
            float arrow = theme.GetNumber(SettingsTokens.SELECTOR_ARROW);
            float gap = theme.GetNumber(SettingsTokens.SELECTOR_GAP);
            float valueW = theme.GetNumber(SettingsTokens.SELECTOR_VALUE_WIDTH);
            var anchor = new Vector2(1f, 0.5f);
            if (rightArrow != null)
            {
                SetFixedRect(rightArrow.Rect, anchor, new Vector2(-padRight, 0f), new Vector2(arrow, arrow));
                rightArrow.ApplyTheme(theme);
            }
            if (valueText != null)
            {
                SetFixedRect(valueText.rectTransform, anchor, new Vector2(-(padRight + arrow + gap), 0f),
                    new Vector2(valueW, PanelLayout.LineHeight(theme.BodyBold)));
                if (valueStyle != null) valueStyle.SetStyle(TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY);
            }
            if (leftArrow != null)
            {
                SetFixedRect(leftArrow.Rect, anchor, new Vector2(-(padRight + arrow + gap + valueW + gap), 0f), new Vector2(arrow, arrow));
                leftArrow.ApplyTheme(theme);
            }
        }

        /// <summary>
        /// Mostra il testo della scelta corrente e attenua la freccia che è al limite.
        /// </summary>
        /// <param name="instant">Non usato (nessuna animazione).</param>
        protected override void RefreshFromStore(bool instant)
        {
            if (theme == null || options.Count == 0) return;
            int index = CurrentIndex();
            if (valueText != null) valueText.text = OptionText(options[index]);
            if (leftArrow != null) leftArrow.SetAtLimit(theme, !wrap && index <= 0);
            if (rightArrow != null) rightArrow.SetAtLimit(theme, !wrap && index >= options.Count - 1);
        }

        /// <summary>
        /// Testo di una scelta: dalla chiave, oppure dal formato con il valore, oppure il numero.
        /// </summary>
        /// <param name="option">Scelta.</param>
        /// <returns>Testo italiano.</returns>
        private string OptionText(SettingsOption option)
        {
            if (option == null || strings == null) return string.Empty;
            if (!string.IsNullOrEmpty(option.LabelKey)) return strings.Get(option.LabelKey);
            if (!string.IsNullOrEmpty(option.FormatKey)) return strings.Format(option.FormatKey, option.Value);
            return option.Value.ToString();
        }

        /// <inheritdoc/>
        protected override void RegisterDefaults(IUISettingsDefaults defaults)
        {
            defaults.RegisterDefaultInt(SettingId, defaultValue);
        }

        /// <summary>
        /// Imposta riferimenti e dati propri. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureSelector(SettingsArrowButton newLeft, SettingsArrowButton newRight, TMP_Text newValueText, UIStringText newValueStyle,
            int newDefault, List<SettingsOption> newOptions, bool newWrap)
        {
            leftArrow = newLeft;
            rightArrow = newRight;
            valueText = newValueText;
            valueStyle = newValueStyle;
            defaultValue = newDefault;
            options = newOptions;
            wrap = newWrap;
        }
    }
}
