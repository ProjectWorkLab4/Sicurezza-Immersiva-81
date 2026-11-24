using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_TrainingCard (712 x 56): striscia del Training in cima alla Home. Icona ICN_School, titolo "Training" Bold +
    /// " · Tutorial comune" Regular, sottotitolo secondario e pulsante "Avvia training" (istanza annidata di BTN_Primary
    /// Light). Tutta la striscia è cliccabile (Button uGUI) e apre PANEL_TrainingBrief: <see cref="Clicked"/> scatta sia dal
    /// clic sulla striscia sia da quello sul pulsante. Stile diverso dalle card scenario: hover 1,02 e pressed 0,99
    /// (Figma, resta nei 712 di contenuto) tramite gli override di <see cref="UIStateVisual"/>; nessun bordo.
    /// Il layout (titolo, sottotitolo che si accorcia con "…", pulsante a destra) è calcolato una volta in <see cref="Apply"/>.
    /// Nota: per il prefisso Nomenclatura sarebbe BTN_TrainingCard (è cliccabile); il nome PANEL_ è quello di CLAUDE.md §2 (punto aperto).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(UIStateVisual))]
    [AddComponentMenu("SI81/UI/Training Card")]
    public class TrainingCard : MonoBehaviour
    {
        /// <summary>Chiave del titolo ("Training · Tutorial comune").</summary>
        public const string TITLE_KEY = "ui.training.title";
        /// <summary>Chiave del sottotitolo.</summary>
        public const string SUBTITLE_KEY = "ui.training.card_subtitle";

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private Button button;
        [SerializeField] private UIStateVisual visual;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [Tooltip("Pulsante \"Avvia training\" (istanza annidata di BTN_Primary). Vuoto = nessun pulsante, resta la striscia.")]
        [SerializeField] private Button startButton;

        /// <summary>Evento: la striscia (o il suo pulsante) è stata cliccata. Apre PANEL_TrainingBrief.</summary>
        public event Action Clicked;

        /// <summary>
        /// Collega i clic e applica testi e layout.
        /// </summary>
        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
            if (startButton != null) startButton.onClick.AddListener(HandleClick);
            Apply();
        }

        /// <summary>
        /// Rilascia i collegamenti.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            if (startButton != null) startButton.onClick.RemoveListener(HandleClick);
        }

        /// <summary>
        /// Applica testi, colori e layout: icona a sinistra, titolo, sottotitolo nello spazio rimasto, pulsante a destra.
        /// </summary>
        public void Apply()
        {
            if (theme == null || strings == null) return;
            float height = theme.GetNumber(CardsTokens.TRAINING_HEIGHT);
            float width = theme.GetNumber(CardsTokens.TRAINING_WIDTH);
            float padLeft = theme.GetNumber(CardsTokens.TRAINING_PAD_LEFT);
            float padRight = theme.GetNumber(CardsTokens.TRAINING_PAD_RIGHT);
            float gap = theme.GetNumber(CardsTokens.TRAINING_GAP);
            float iconSize = theme.GetNumber(CardsTokens.TRAINING_ICON);

            // Icona.
            float x = padLeft;
            if (icon != null)
            {
                icon.color = theme.BrandDark;
                PlaceCentered(icon.rectTransform, x, iconSize, iconSize, height);
            }
            x += iconSize + gap;

            // Titolo rich text: "Training" Bold + " · Tutorial comune" Regular, una riga.
            float titleWidth = 0f;
            if (title != null)
            {
                CardTextUtil.Style(title, theme, TextStyleIds.BODY, theme.TextPrimary);
                string text = CardTextUtil.BoldBeforeSeparator(strings.Get(TITLE_KEY));
                title.text = text;
                titleWidth = Mathf.Ceil(CardTextUtil.MeasureWidth(title, text)) + 1f;
                PlaceCentered(title.rectTransform, x, titleWidth, Mathf.Max(height, title.preferredHeight), height);
            }
            x += titleWidth + gap;

            // Pulsante a destra, centrato in verticale (larghezza = quella dell'istanza annidata).
            float buttonWidth = 0f;
            if (startButton != null)
            {
                var buttonRect = (RectTransform)startButton.transform;
                buttonWidth = buttonRect.rect.width;
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0.5f);
                buttonRect.pivot = new Vector2(1f, 0.5f);
                buttonRect.anchoredPosition = new Vector2(-padRight, 0f);
            }

            // Sottotitolo: dal fondo del titolo al pulsante (meno un gap), "…" se troppo lungo.
            if (subtitle != null)
            {
                CardTextUtil.Style(subtitle, theme, TextStyleIds.BODY, theme.TextSecondary);
                subtitle.textWrappingMode = TextWrappingModes.NoWrap;
                subtitle.overflowMode = TextOverflowModes.Ellipsis;
                subtitle.text = strings.Get(SUBTITLE_KEY);
                float available = width - padRight - (buttonWidth > 0f ? buttonWidth + gap : 0f) - x;
                PlaceCentered(subtitle.rectTransform, x, Mathf.Max(0f, available), Mathf.Max(height, subtitle.preferredHeight), height);
            }

            // Hover 1,02 / pressed 0,99 dal tema (token scale.training-card.*), al posto di 1,04 / 0,98.
            if (visual != null)
            {
                visual.HoverScaleOverride = theme.GetNumber(CardsTokens.TRAINING_HOVER_SCALE);
                visual.PressedScaleOverride = theme.GetNumber(CardsTokens.TRAINING_PRESSED_SCALE);
            }
        }

        /// <summary>
        /// Posiziona un elemento a sinistra (x), centrato in verticale nella striscia alta <paramref name="containerHeight"/>.
        /// </summary>
        private static void PlaceCentered(RectTransform rect, float x, float width, float height, float containerHeight)
        {
            CardTextUtil.PlaceTopLeft(rect, x, (containerHeight - height) * 0.5f, width, height);
        }

        /// <summary>
        /// Rilancia il clic.
        /// </summary>
        private void HandleClick()
        {
            Clicked?.Invoke();
        }

        /// <summary>
        /// Collega i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, Button newButton, UIStateVisual newVisual, Image newIcon,
            TMP_Text newTitle, TMP_Text newSubtitle, Button newStartButton)
        {
            theme = newTheme;
            strings = newStrings;
            button = newButton;
            visual = newVisual;
            icon = newIcon;
            title = newTitle;
            subtitle = newSubtitle;
            startButton = newStartButton;
        }
    }
}
