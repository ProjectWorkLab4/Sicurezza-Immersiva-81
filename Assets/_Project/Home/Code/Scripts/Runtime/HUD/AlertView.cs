using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Vista di un avviso breve: la usano HUD_EventAlert (sfondo deny, testo nero, forma Completa / solo titolo / solo icona) e
    /// HUD_ActionFeedback (card bianca, icona colorata dal tipo). Opaca, con ombra hudCard. Il layout si calcola UNA volta a ogni
    /// avviso mostrato (nessun Layout Group). La posizione nel mondo e i tempi li decide <see cref="AlertPresenter"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Alert View")]
    public class AlertView : MonoBehaviour
    {
        /// <summary>Margine interno orizzontale.</summary>
        private const float PADDING_X = 12f;
        /// <summary>Margine interno verticale.</summary>
        private const float PADDING_Y = 10f;
        /// <summary>Spazio tra icona e testo.</summary>
        private const float GAP = 10f;
        /// <summary>Lato dell'icona (Figma 32).</summary>
        private const float ICON_SIZE = 32f;
        /// <summary>Altezza minima della forma "solo titolo" e del feedback (Figma 52).</summary>
        private const float MIN_HEIGHT = 52f;
        /// <summary>Lato della forma "solo icona" (Figma 56).</summary>
        private const float ICON_ONLY_SIZE = 56f;
        /// <summary>Larghezza di avvisi e feedback (Figma 420).</summary>
        private const float WIDTH = 420f;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [Tooltip("Oggetto CANVAS_ del prefab: si accende solo mentre l'avviso è visibile.")]
        [SerializeField] private GameObject content;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RoundedPanelGraphic background;
        [SerializeField] private Image shadow;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [Tooltip("Sprite arrotondato (raggio card) e a pillola (forma solo icona).")]
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite pillSprite;
        [Tooltip("true = HUD_EventAlert (deny, testo nero); false = HUD_ActionFeedback (card bianca).")]
        [SerializeField] private bool isEvent = true;
        [Tooltip("Icone predefinite: avviso evento, positivo, neutro, negativo.")]
        [SerializeField] private Sprite eventIcon;
        [SerializeField] private Sprite positiveIcon;
        [SerializeField] private Sprite neutralIcon;
        [SerializeField] private Sprite negativeIcon;

        /// <summary>Gruppo per la dissolvenza.</summary>
        public CanvasGroup Group => group;
        /// <summary>Transform del Canvas da posizionare nel mondo.</summary>
        public Transform CanvasTransform => content != null ? content.transform : transform;
        /// <summary>true se è la vista degli avvisi evento.</summary>
        public bool IsEvent => isEvent;
        /// <summary>Icona predefinita degli avvisi evento.</summary>
        public Sprite DefaultEventIcon => eventIcon;

        /// <summary>
        /// Accende o spegne il Canvas.
        /// </summary>
        /// <param name="shown">true per mostrare.</param>
        public void SetShown(bool shown)
        {
            if (content != null && content.activeSelf != shown) content.SetActive(shown);
        }

        /// <summary>
        /// Scrive un avviso evento nella forma richiesta.
        /// </summary>
        /// <param name="titleText">Titolo (prima riga).</param>
        /// <param name="bodyText">Dettaglio (può essere vuoto; usato solo nella forma Completa).</param>
        /// <param name="sprite">Icona (null = predefinita).</param>
        /// <param name="mode">Forma.</param>
        public void ShowEvent(string titleText, string bodyText, Sprite sprite, EventAlertMode mode)
        {
            Color ink = theme.TextPrimary;
            Style(theme.Deny, ink, mode == EventAlertMode.IconOnly ? pillSprite : cardSprite);
            SetIcon(sprite != null ? sprite : eventIcon, ink);
            if (mode == EventAlertMode.IconOnly)
            {
                title.gameObject.SetActive(false);
                body.gameObject.SetActive(false);
                Resize(ICON_ONLY_SIZE, ICON_ONLY_SIZE);
                LabelStyling.Place(icon.rectTransform, (ICON_ONLY_SIZE - ICON_SIZE) * 0.5f, (ICON_ONLY_SIZE - ICON_SIZE) * 0.5f, ICON_SIZE, ICON_SIZE);
                return;
            }

            float textWidth = WIDTH - PADDING_X * 2f - ICON_SIZE - GAP;
            LabelStyling.StyleText(title, theme, TextStyleIds.SUBTITLE_BOLD, ink);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.text = titleText;
            float titleHeight = Mathf.Ceil(title.GetPreferredValues(titleText, textWidth, 0f).y);
            bool hasBody = mode == EventAlertMode.Full && !string.IsNullOrEmpty(bodyText);
            float bodyHeight = 0f;
            if (hasBody)
            {
                LabelStyling.StyleText(body, theme, TextStyleIds.HUD_MIN, ink);
                body.textWrappingMode = TextWrappingModes.Normal;
                body.text = bodyText;
                bodyHeight = Mathf.Ceil(body.GetPreferredValues(bodyText, textWidth, 0f).y);
            }
            title.gameObject.SetActive(true);
            body.gameObject.SetActive(hasBody);
            float height = Mathf.Max(MIN_HEIGHT, PADDING_Y * 2f + titleHeight + (hasBody ? 2f + bodyHeight : 0f));
            Resize(WIDTH, height);
            float textX = PADDING_X + ICON_SIZE + GAP;
            LabelStyling.Place(icon.rectTransform, PADDING_X, PADDING_Y, ICON_SIZE, ICON_SIZE);
            LabelStyling.Place(title.rectTransform, textX, PADDING_Y, textWidth, titleHeight);
            if (hasBody) LabelStyling.Place(body.rectTransform, textX, PADDING_Y + titleHeight + 2f, textWidth, bodyHeight);
        }

        /// <summary>
        /// Scrive un feedback d'azione.
        /// </summary>
        /// <param name="text">Testo (max ~60 caratteri).</param>
        /// <param name="kind">Tipo (icona e colore).</param>
        public void ShowFeedback(string text, FeedbackKind kind)
        {
            Color iconColor;
            Sprite sprite;
            switch (kind)
            {
                case FeedbackKind.Positive: iconColor = theme.Confirm; sprite = positiveIcon; break;
                case FeedbackKind.Negative: iconColor = theme.Deny; sprite = negativeIcon; break;
                default: iconColor = theme.BrandDark; sprite = neutralIcon; break;
            }
            Style(theme.SurfaceCard, theme.TextPrimary, cardSprite);
            SetIcon(sprite, iconColor);
            float textWidth = WIDTH - PADDING_X - 16f - ICON_SIZE - GAP;
            LabelStyling.StyleText(title, theme, TextStyleIds.HUD_MIN, theme.TextPrimary);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.text = text;
            title.gameObject.SetActive(true);
            if (body != null) body.gameObject.SetActive(false);
            float textHeight = Mathf.Ceil(title.GetPreferredValues(text, textWidth, 0f).y);
            float height = Mathf.Max(MIN_HEIGHT, PADDING_Y * 2f + textHeight);
            Resize(WIDTH, height);
            LabelStyling.Place(icon.rectTransform, PADDING_X, (height - ICON_SIZE) * 0.5f, ICON_SIZE, ICON_SIZE);
            LabelStyling.Place(title.rectTransform, PADDING_X + ICON_SIZE + GAP, (height - textHeight) * 0.5f, textWidth, textHeight);
        }

        /// <summary>
        /// Colori e sprite dello sfondo.
        /// </summary>
        /// <param name="fill">Colore dello sfondo.</param>
        /// <param name="ink">Colore del testo.</param>
        /// <param name="sprite">Sprite dello sfondo.</param>
        private void Style(Color fill, Color ink, Sprite sprite)
        {
            if (background.Sprite != sprite) background.Sprite = sprite;
            LabelStyling.SetColor(background, fill);
            LabelStyling.SetColor(title, ink);
            if (body != null) LabelStyling.SetColor(body, ink);
        }

        /// <summary>
        /// Imposta sprite e colore dell'icona.
        /// </summary>
        /// <param name="sprite">Sprite.</param>
        /// <param name="color">Colore.</param>
        private void SetIcon(Sprite sprite, Color color)
        {
            if (icon.sprite != sprite) icon.sprite = sprite;
            icon.enabled = sprite != null;
            LabelStyling.SetColor(icon, color);
        }

        /// <summary>
        /// Dimensiona sfondo e ombra (al centro del Canvas); l'ombra è allargata del padding del suo sprite.
        /// </summary>
        /// <param name="width">Larghezza.</param>
        /// <param name="height">Altezza.</param>
        private void Resize(float width, float height)
        {
            RectTransform rect = background.rectTransform;
            PanelLayout.SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-width * 0.5f, -height * 0.5f), new Vector2(width * 0.5f, height * 0.5f));
            if (shadow == null) return;
            UITheme.ShadowToken token = theme.GetShadow(ShadowRoles.HUD_CARD);
            if (token == null) return;
            if (shadow.color != token.Color) shadow.color = token.Color;
            float expand = PanelSprites.GetShadowLayout(theme.RadiusCard, token).PaddingUnits;
            PanelLayout.SetRect(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-width * 0.5f - expand, -height * 0.5f - expand), new Vector2(width * 0.5f + expand, height * 0.5f + expand));
        }

        /// <summary>
        /// Imposta tutti i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, GameObject newContent, CanvasGroup newGroup, RoundedPanelGraphic newBackground, Image newShadow,
            Image newIcon, TMP_Text newTitle, TMP_Text newBody, Sprite newCard, Sprite newPill, bool newIsEvent,
            Sprite newEventIcon, Sprite newPositive, Sprite newNeutral, Sprite newNegative)
        {
            theme = newTheme; content = newContent; group = newGroup; background = newBackground; shadow = newShadow; icon = newIcon;
            title = newTitle; body = newBody; cardSprite = newCard; pillSprite = newPill; isEvent = newIsEvent;
            eventIcon = newEventIcon; positiveIcon = newPositive; neutralIcon = newNeutral; negativeIcon = newNegative;
        }
    }
}
