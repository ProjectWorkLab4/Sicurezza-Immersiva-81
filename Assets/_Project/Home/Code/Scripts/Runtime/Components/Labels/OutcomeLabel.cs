using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// LBL_Outcome: pillola dell'esito (positivo, parziale, negativo) con icona + testo (mai solo colore).
    /// - OnCard: colore pieno dell'esito, SOLO su card bianche (Positivo bianco su confirm, Parziale bianco su brand.dark,
    ///   Negativo nero su deny).
    /// - OnGlass: pillola bianca + icona colorata + testo nero, per le intestazioni vetro (CLAUDE.md 4.9).
    /// Dimensioni Figma: Small (h 32, icona 24, Body Bold) e Large (h 60, icona 32, Title Bold; HUD_OutcomePanel).
    /// La larghezza si adatta al testo. Non interattiva. Testi da ui.outcome.positive/partial/negative.
    /// Uso: <c>SetOutcome(OutcomeType.Partial)</c>; <c>SetPresentation(OutcomePresentation.OnGlass)</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("SI81/UI/Outcome Label")]
    public class OutcomeLabel : MonoBehaviour
    {
        /// <summary>Misure di una dimensione (Figma UI_OutcomeBadge).</summary>
        private struct Metrics
        {
            /// <summary>Altezza.</summary>
            public float Height;
            /// <summary>Margine sinistro.</summary>
            public float PaddingLeft;
            /// <summary>Margine destro.</summary>
            public float PaddingRight;
            /// <summary>Spazio tra icona e testo.</summary>
            public float Gap;
            /// <summary>Lato dell'icona.</summary>
            public float IconSize;
            /// <summary>Stile di testo del tema.</summary>
            public string TextStyle;
        }

        /// <summary>Misure della dimensione piccola.</summary>
        private static readonly Metrics SMALL = new Metrics { Height = 32f, PaddingLeft = 10f, PaddingRight = 14f, Gap = 6f, IconSize = 24f, TextStyle = TextStyleIds.BODY_BOLD };
        /// <summary>Misure della dimensione grande.</summary>
        private static readonly Metrics LARGE = new Metrics { Height = 60f, PaddingLeft = 16f, PaddingRight = 24f, Gap = 10f, IconSize = 32f, TextStyle = TextStyleIds.TITLE };

        [Header("Dati")]
        [Tooltip("Esito mostrato.")]
        [SerializeField] private OutcomeType outcome = OutcomeType.Positive;
        [Tooltip("Small = card e intestazioni, Large = HUD_OutcomePanel.")]
        [SerializeField] private LabelSize size = LabelSize.Small;
        [Tooltip("OnCard = colore pieno (solo card bianche). OnGlass = pillola bianca su intestazione vetro.")]
        [SerializeField] private OutcomePresentation presentation = OutcomePresentation.OnCard;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private RoundedPanelGraphic pill;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text text;
        [Tooltip("Icone nell'ordine di OutcomeType: Positive, Partial, Negative.")]
        [SerializeField] private Sprite[] icons = new Sprite[3];

        /// <summary>Esito corrente.</summary>
        public OutcomeType Outcome => outcome;
        /// <summary>Presentazione corrente.</summary>
        public OutcomePresentation Presentation => presentation;

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
        /// Imposta l'esito e aggiorna la pillola.
        /// </summary>
        /// <param name="newOutcome">Esito da mostrare.</param>
        public void SetOutcome(OutcomeType newOutcome)
        {
            outcome = newOutcome;
            Apply();
        }

        /// <summary>
        /// Imposta la presentazione (colore pieno su card / pillola bianca su vetro).
        /// </summary>
        /// <param name="newPresentation">Presentazione.</param>
        public void SetPresentation(OutcomePresentation newPresentation)
        {
            presentation = newPresentation;
            Apply();
        }

        /// <summary>
        /// Imposta la dimensione (Small / Large).
        /// </summary>
        /// <param name="newSize">Dimensione.</param>
        public void SetSize(LabelSize newSize)
        {
            size = newSize;
            Apply();
        }

        /// <summary>
        /// Chiave del testo di un esito.
        /// </summary>
        /// <param name="type">Esito.</param>
        /// <returns>Chiave di SO_UIStrings_It.</returns>
        public static string GetTextKey(OutcomeType type)
        {
            switch (type)
            {
                case OutcomeType.Positive: return "ui.outcome.positive";
                case OutcomeType.Partial: return "ui.outcome.partial";
                default: return "ui.outcome.negative";
            }
        }

        /// <summary>
        /// Aggiorna colori, icona, testo e larghezza dai dati correnti.
        /// </summary>
        public void Apply()
        {
            if (theme == null || strings == null || text == null) return;
            Metrics m = size == LabelSize.Large ? LARGE : SMALL;

            // Colori: pieno dell'esito su card; bianco + icona colorata + testo nero su vetro.
            Color outcomeColor = GetOutcomeColor(outcome);
            Color fillColor;
            Color iconColor;
            Color textColor;
            if (presentation == OutcomePresentation.OnGlass)
            {
                fillColor = theme.SurfaceCard;
                iconColor = outcomeColor;
                textColor = theme.TextPrimary;
            }
            else
            {
                fillColor = outcomeColor;
                // Solo Negativo (deny) ha testo nero; Positivo e Parziale bianco.
                textColor = outcome == OutcomeType.Negative ? theme.TextPrimary : theme.TextOnDark;
                iconColor = textColor;
            }

            LabelStyling.StyleText(text, theme, m.TextStyle, textColor);
            string label = strings.Get(GetTextKey(outcome));
            if (text.text != label) text.text = label;
            float textWidth = LabelStyling.TextWidth(text);
            float width = m.PaddingLeft + m.IconSize + m.Gap + textWidth + m.PaddingRight;

            if (pill != null)
            {
                LabelStyling.SetColor(pill, fillColor);
                PanelLayout.SetRect(pill.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }
            if (icon != null)
            {
                int index = (int)outcome;
                Sprite wanted = icons != null && index < icons.Length ? icons[index] : null;
                if (icon.sprite != wanted) icon.sprite = wanted;
                icon.raycastTarget = false;
                LabelStyling.SetColor(icon, iconColor);
                LabelStyling.Place(icon.rectTransform, m.PaddingLeft, (m.Height - m.IconSize) * 0.5f, m.IconSize, m.IconSize);
            }
            LabelStyling.Place(text.rectTransform, m.PaddingLeft + m.IconSize + m.Gap, 0f, textWidth, m.Height);
            LabelStyling.SetSize((RectTransform)transform, width, m.Height);
        }

        /// <summary>
        /// Colore pieno dell'esito dal tema.
        /// </summary>
        private Color GetOutcomeColor(OutcomeType type)
        {
            switch (type)
            {
                case OutcomeType.Positive: return theme.OutcomePositive;
                case OutcomeType.Partial: return theme.OutcomePartial;
                default: return theme.OutcomeNegative;
            }
        }

        /// <summary>
        /// Collega i riferimenti e applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, RoundedPanelGraphic newPill, Image newIcon, TMP_Text newText, Sprite[] newIcons)
        {
            theme = newTheme;
            strings = newStrings;
            pill = newPill;
            icon = newIcon;
            text = newText;
            icons = newIcons;
            Apply();
        }
    }
}
