using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// BTN_ScenarioCard (166 x 140): card di uno scenario nella Home. Immagine (miniatura o gradiente segnaposto) con icona
    /// dello scenario e pillola "miglior risultato" (stella + "4/5", oppure "Nuovo" se mai giocato, oppure lucchetto +
    /// "Non disponibile"); sotto titolo, complessità (5 pallini + "N/5") e livelli completati (icona a 3 barre).
    /// Mostra SOLO le stelle, mai il punteggio 0–100 (CLAUDE.md 4.6). Nessun bordo su nessuno stato (4.1): hover/pressed
    /// li gestisce <see cref="UIStateVisual"/>; il clic è un Button uGUI. Disabled = immagine, icona e barre al 40%, pillola
    /// con lucchetto, non cliccabile.
    /// Uso: <c>card.Set(new ScenarioCardData { Id = "01", Title = "...", Icon = ..., BestStars = 4, Complexity = 2, LevelsCompleted = 2 })</c>;
    /// iscriversi a <see cref="Clicked"/> (restituisce l'id dello scenario). Dati e testi arrivano sempre da Set e da SO_UIStrings_It.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(UIStateVisual))]
    [AddComponentMenu("SI81/UI/Scenario Card")]
    public class ScenarioCard : MonoBehaviour
    {
        /// <summary>Chiave del testo della stella ("{0}/5").</summary>
        public const string STARS_KEY = "ui.format.stars";
        /// <summary>Chiave del testo della complessità ("{0}/5").</summary>
        public const string COMPLEXITY_KEY = "ui.format.complexity";
        /// <summary>Chiave di "Nuovo".</summary>
        public const string NEW_KEY = "ui.home.new";
        /// <summary>Chiave di "Non disponibile".</summary>
        public const string UNAVAILABLE_KEY = "ui.home.unavailable";

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private DifficultyStyle difficultyStyle;
        [SerializeField] private Button button;
        [SerializeField] private UIStateVisual visual;
        [SerializeField] private CanvasGroup mediaGroup;
        [SerializeField] private CardImageGraphic thumbnail;
        [SerializeField] private Image scenarioIcon;
        [SerializeField] private RectTransform pill;
        [SerializeField] private RoundedPanelGraphic pillBackground;
        [SerializeField] private Image pillIcon;
        [SerializeField] private TMP_Text pillLabel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private CardDots complexityDots;
        [SerializeField] private TMP_Text complexityLabel;
        [SerializeField] private CanvasGroup levelsGroup;
        [SerializeField] private CardLevelBars levelBars;
        [Tooltip("Icona della stella piena nella pillola (ICN_Star_Filled).")]
        [SerializeField] private Sprite starSprite;
        [Tooltip("Icona del lucchetto nella pillola della variante Disabled (ICN_Lock).")]
        [SerializeField] private Sprite lockSprite;

        // Ultimi dati mostrati (per riapplicare tema/testi).
        private ScenarioCardData data;

        /// <summary>Evento: la card è stata cliccata (parametro: id dello scenario).</summary>
        public event Action<string> Clicked;

        /// <summary>Id dello scenario mostrato (vuoto se Set non è stato chiamato).</summary>
        public string Id => data != null ? data.Id : string.Empty;
        /// <summary>true se la card è cliccabile (scenario disponibile).</summary>
        public bool IsUnlocked => data == null || data.Unlocked;

        /// <summary>
        /// Collega il clic e applica gli ultimi dati.
        /// </summary>
        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
            Apply();
        }

        /// <summary>
        /// Rilascia il collegamento al pulsante.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>
        /// Mostra i dati di uno scenario (e li ricorda per riapplicarli).
        /// </summary>
        /// <param name="newData">Dati della card (null = ignorato).</param>
        public void Set(ScenarioCardData newData)
        {
            if (newData == null) return;
            data = newData;
            Apply();
        }

        /// <summary>
        /// Riscrive testi, immagini, colori e stato dai dati correnti. Nessun Layout Group: la pillola si misura una volta.
        /// </summary>
        public void Apply()
        {
            if (theme == null || strings == null || data == null) return;
            bool unlocked = data.Unlocked;
            float disabledAlpha = theme.GetNumber(CardsTokens.ALPHA_DISABLED);
            float mediaAlpha = unlocked ? 1f : disabledAlpha;

            // Interattività: stato visivo e pulsante.
            if (visual != null) visual.Interactable = unlocked;
            if (button != null) button.interactable = unlocked;

            // Immagine: miniatura o gradiente segnaposto (dati della missione, altrimenti verde brand).
            Color start = data.GradientStart ?? theme.BrandDark;
            Color end = data.GradientEnd ?? theme.GetColor(ColorTokens.LEVEL_BAR_ON_LIGHT);
            if (thumbnail != null) thumbnail.SetContent(data.Image, start, end);
            if (scenarioIcon != null)
            {
                scenarioIcon.sprite = data.Icon;
                scenarioIcon.enabled = data.Icon != null;
                scenarioIcon.color = theme.TextOnDark;
            }
            if (mediaGroup != null) mediaGroup.alpha = mediaAlpha;

            // Titolo.
            if (title != null)
            {
                CardTextUtil.Style(title, theme, TextStyleIds.BODY_BOLD, theme.TextPrimary);
                title.text = !string.IsNullOrEmpty(data.TitleKey) ? strings.Get(data.TitleKey) : data.Title;
            }

            // Complessità: pallini + "N/5".
            int complexity = Mathf.Clamp(data.Complexity, 1, CardDots.COUNT);
            if (complexityDots != null)
                complexityDots.Set(complexity, theme.GetColor(ColorTokens.LEVEL_BAR_ON_LIGHT), theme.GetColor(ColorTokens.LEVEL_BAR_OFF_LIGHT));
            if (complexityLabel != null)
            {
                CardTextUtil.Style(complexityLabel, theme, TextStyleIds.BODY, theme.TextSecondary);
                complexityLabel.text = strings.Format(COMPLEXITY_KEY, complexity);
            }

            // Livelli completati: barre piene (on-light) o solo contorno (off-light).
            if (levelBars != null)
            {
                float stroke = difficultyStyle != null ? difficultyStyle.BarStroke : theme.GetNumber(NumberTokens.STROKE_LEVEL_BAR);
                Color on = difficultyStyle != null ? difficultyStyle.GetBarColor(true, false) : theme.GetColor(ColorTokens.LEVEL_BAR_ON_LIGHT);
                Color off = difficultyStyle != null ? difficultyStyle.GetBarColor(false, false) : theme.GetColor(ColorTokens.LEVEL_BAR_OFF_LIGHT);
                levelBars.Set(Mathf.Clamp(data.LevelsCompleted, 0, DifficultyStyle.BAR_COUNT), on, off, stroke);
            }
            if (levelsGroup != null) levelsGroup.alpha = mediaAlpha;

            ApplyPill();
        }

        /// <summary>
        /// Pillola in alto a destra: stella + "N/5", "Nuovo" (senza stella) o lucchetto + "Non disponibile".
        /// La larghezza segue il testo (padding sx + icona + gap + testo + padding dx), calcolata una sola volta qui.
        /// </summary>
        private void ApplyPill()
        {
            if (pill == null || pillLabel == null) return;
            string text;
            Sprite icon;
            if (!data.Unlocked)
            {
                text = strings.Get(UNAVAILABLE_KEY);
                icon = lockSprite;
            }
            else if (data.BestStars.HasValue)
            {
                text = strings.Format(STARS_KEY, Mathf.Clamp(data.BestStars.Value, 0, 5));
                icon = starSprite;
            }
            else
            {
                text = strings.Get(NEW_KEY);
                icon = null;
            }

            CardTextUtil.Style(pillLabel, theme, TextStyleIds.BODY, theme.TextPrimary);
            pillLabel.text = text;
            if (pillBackground != null) pillBackground.color = theme.SurfaceCard;

            float padLeft = theme.GetNumber(CardsTokens.SCENARIO_PILL_PAD_LEFT);
            float padRight = theme.GetNumber(CardsTokens.SCENARIO_PILL_PAD_RIGHT);
            float padY = theme.GetNumber(CardsTokens.SCENARIO_PILL_PAD_Y);
            float gap = theme.GetNumber(CardsTokens.SCENARIO_PILL_GAP);
            float iconSize = theme.GetNumber(CardsTokens.SCENARIO_PILL_ICON);
            float height = iconSize + padY * 2f;
            float textWidth = CardTextUtil.MeasureWidth(pillLabel, text);

            float x = padLeft;
            if (pillIcon != null)
            {
                pillIcon.sprite = icon;
                pillIcon.enabled = icon != null;
                pillIcon.color = theme.TextPrimary;
                CardTextUtil.PlaceTopLeft(pillIcon.rectTransform, x, padY, iconSize, iconSize);
                if (icon != null) x += iconSize + gap;
            }
            // Il rettangolo del testo è più alto della riga Figma (Jaldi ha una riga naturale più alta): centrato sulla pillola.
            float labelHeight = Mathf.Max(height, pillLabel.preferredHeight);
            CardTextUtil.PlaceTopLeft(pillLabel.rectTransform, x, (height - labelHeight) * 0.5f, Mathf.Ceil(textWidth) + 1f, labelHeight);
            pill.sizeDelta = new Vector2(Mathf.Ceil(x + textWidth + padRight), height);
        }

        /// <summary>
        /// Rilancia il clic con l'id dello scenario.
        /// </summary>
        private void HandleClick()
        {
            if (data == null || !data.Unlocked) return;
            Clicked?.Invoke(data.Id);
        }

        /// <summary>
        /// Collega i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, DifficultyStyle newStyle, Button newButton, UIStateVisual newVisual,
            CanvasGroup newMediaGroup, CardImageGraphic newThumbnail, Image newScenarioIcon, RectTransform newPill, RoundedPanelGraphic newPillBackground,
            Image newPillIcon, TMP_Text newPillLabel, TMP_Text newTitle, CardDots newDots, TMP_Text newComplexityLabel,
            CanvasGroup newLevelsGroup, CardLevelBars newLevelBars, Sprite newStarSprite, Sprite newLockSprite)
        {
            theme = newTheme;
            strings = newStrings;
            difficultyStyle = newStyle;
            button = newButton;
            visual = newVisual;
            mediaGroup = newMediaGroup;
            thumbnail = newThumbnail;
            scenarioIcon = newScenarioIcon;
            pill = newPill;
            pillBackground = newPillBackground;
            pillIcon = newPillIcon;
            pillLabel = newPillLabel;
            title = newTitle;
            complexityDots = newDots;
            complexityLabel = newComplexityLabel;
            levelsGroup = newLevelsGroup;
            levelBars = newLevelBars;
            starSprite = newStarSprite;
            lockSprite = newLockSprite;
        }
    }
}
