using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_StatTile: tessera statistica con icona 24, etichetta Body e valore Title Bold (Positivi, Parziali, Negativi,
    /// Sessioni). Sempre icona + etichetta + valore (mai solo colore). Misure Figma: 112×96, padding 12/8, raggio card.
    /// Non interattiva (nessun raycast). Colori dal tema (confirm, brand.dark, deny, tile.neutral), testi da
    /// SO_UIStrings_It (ui.stats.positives/partials/negatives/sessions), icone ICN_ assegnate dal builder.
    /// Uso: <c>SetStat(StatAccent.Positive, 5)</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("SI81/UI/Stat Tile")]
    public class StatTile : MonoBehaviour
    {
        /// <summary>Padding orizzontale (Figma 12).</summary>
        public const float PADDING_X = 12f;
        /// <summary>Padding verticale (Figma 8).</summary>
        public const float PADDING_Y = 8f;
        /// <summary>Lato dell'icona (Figma 24).</summary>
        public const float ICON_SIZE = 24f;

        [Header("Dati")]
        [Tooltip("Accento: colore, icona ed etichetta.")]
        [SerializeField] private StatAccent accent = StatAccent.Positive;
        [Tooltip("Valore mostrato (numero intero).")]
        [SerializeField] private int value;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private RoundedPanelGraphic background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [Tooltip("Icone nell'ordine di StatAccent: Positive, Partial, Negative, Neutral.")]
        [SerializeField] private Sprite[] icons = new Sprite[4];

        /// <summary>Accento corrente.</summary>
        public StatAccent Accent => accent;
        /// <summary>Valore corrente.</summary>
        public int Value => value;

        /// <summary>
        /// Applica dati e stile all'avvio.
        /// </summary>
        private void Awake()
        {
            Apply();
        }

#if UNITY_EDITOR
        /// <summary>
        /// In Editor riapplica quando si cambia un campo nell'Inspector (rimandato a fine aggiornamento).
        /// </summary>
        private void OnValidate()
        {
            UIEditorSupport.ApplyDeferred(this, Apply);
        }
#endif

        /// <summary>
        /// Imposta accento e valore insieme.
        /// </summary>
        /// <param name="newAccent">Accento della tessera.</param>
        /// <param name="newValue">Valore (conteggio).</param>
        public void SetStat(StatAccent newAccent, int newValue)
        {
            accent = newAccent;
            value = Mathf.Max(0, newValue);
            Apply();
        }

        /// <summary>
        /// Imposta solo il valore.
        /// </summary>
        /// <param name="newValue">Valore (conteggio).</param>
        public void SetValue(int newValue)
        {
            SetStat(accent, newValue);
        }

        /// <summary>
        /// Chiave dell'etichetta di un accento.
        /// </summary>
        /// <param name="stat">Accento.</param>
        /// <returns>Chiave di SO_UIStrings_It.</returns>
        public static string GetLabelKey(StatAccent stat)
        {
            switch (stat)
            {
                case StatAccent.Positive: return "ui.stats.positives";
                case StatAccent.Partial: return "ui.stats.partials";
                case StatAccent.Negative: return "ui.stats.negatives";
                default: return "ui.stats.sessions";
            }
        }

        /// <summary>
        /// Aggiorna colori, icona, testi e posizioni dai dati correnti.
        /// </summary>
        public void Apply()
        {
            if (theme == null || strings == null || labelText == null || valueText == null) return;

            // Sfondo e colore di testo/icona per accento (testo nero su deny e tile.neutral, bianco su confirm e brand.dark).
            Color fillColor;
            Color contentColor;
            switch (accent)
            {
                case StatAccent.Positive: fillColor = theme.Confirm; contentColor = theme.TextOnDark; break;
                case StatAccent.Partial: fillColor = theme.BrandDark; contentColor = theme.TextOnDark; break;
                case StatAccent.Negative: fillColor = theme.Deny; contentColor = theme.TextPrimary; break;
                default: fillColor = theme.TileNeutral; contentColor = theme.TextPrimary; break;
            }
            if (background != null)
            {
                LabelStyling.SetColor(background, fillColor);
                PanelLayout.SetRect(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            int index = (int)accent;
            if (icon != null)
            {
                Sprite wanted = icons != null && index < icons.Length ? icons[index] : null;
                if (icon.sprite != wanted) icon.sprite = wanted;
                icon.raycastTarget = false;
                LabelStyling.SetColor(icon, contentColor);
                LabelStyling.Place(icon.rectTransform, PADDING_X, PADDING_Y, ICON_SIZE, ICON_SIZE);
            }

            // Pila verticale: icona, etichetta Body, valore Title Bold (stile "Title" = Bold 32).
            float labelY = PADDING_Y + ICON_SIZE;
            float labelHeight = PanelLayout.LineHeight(theme.Body);
            float valueHeight = PanelLayout.LineHeight(theme.Title);
            LabelStyling.StyleText(labelText, theme, TextStyleIds.BODY, contentColor);
            LabelStyling.StyleText(valueText, theme, TextStyleIds.TITLE, contentColor);
            LabelStyling.PlaceStretchX(labelText.rectTransform, PADDING_X, PADDING_X, labelY, labelHeight);
            LabelStyling.PlaceStretchX(valueText.rectTransform, PADDING_X, PADDING_X, labelY + labelHeight, valueHeight);
            string label = strings.Get(GetLabelKey(accent));
            if (labelText.text != label) labelText.text = label;
            string number = value.ToString(CultureInfo.InvariantCulture);
            if (valueText.text != number) valueText.text = number;
        }

        /// <summary>
        /// Collega i riferimenti e applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, RoundedPanelGraphic newBackground, Image newIcon,
            TMP_Text newLabel, TMP_Text newValue, Sprite[] newIcons)
        {
            theme = newTheme;
            strings = newStrings;
            background = newBackground;
            icon = newIcon;
            labelText = newLabel;
            valueText = newValue;
            icons = newIcons;
            Apply();
        }
    }
}
