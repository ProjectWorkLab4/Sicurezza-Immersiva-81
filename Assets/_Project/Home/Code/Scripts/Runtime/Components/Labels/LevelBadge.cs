using System.Globalization;
using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// LBL_LevelBadge: icona a 3 barre (piene = numero del livello) + "Livello N" in Bold + " · Nome" in Regular
    /// (CLAUDE.md 4.7). Unico modo di mostrare il livello. Non interattiva (nessun raycast). Colori delle barre da
    /// SO_DifficultyStyle (token level.bar.*), colore del testo dal tema, testi da SO_UIStrings_It (ui.level.badge,
    /// ui.level.guided/operational/crisis). La larghezza si adatta al testo (hug), calcolata una volta a ogni modifica.
    /// Uso: <c>SetLevel(2)</c> oppure <c>SetLevel(2, "ui.level.operational")</c>; <c>SetSurface(LabelSurface.Dark)</c> su fondo scuro.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("SI81/UI/Level Badge")]
    public class LevelBadge : MonoBehaviour
    {
        /// <summary>Lato dell'icona livello (Figma 20×20).</summary>
        public const float ICON_SIZE = 20f;
        /// <summary>Spazio tra icona e testo.</summary>
        public const float GAP = 6f;

        /// <summary>Barre dell'icona (x, y dall'alto, larghezza, altezza) nel riquadro 20×20 (Figma UI_LevelBadge).</summary>
        private static readonly Rect[] BAR_RECTS =
        {
            new Rect(2f, 12f, 4f, 7f),
            new Rect(8f, 7f, 4f, 12f),
            new Rect(14f, 2f, 4f, 17f)
        };
        /// <summary>Raggio degli angoli delle barre (Figma).</summary>
        private const float BAR_RADIUS = 1f;
        /// <summary>Separatore del badge, tolto dal testo Bold (punto mediano).</summary>
        private const char SEPARATOR = '·';
        /// <summary>Segnaposto del nome del livello in ui.level.badge.</summary>
        private const string NAME_PLACEHOLDER = "{1}";

        [Header("Dati")]
        [Tooltip("Livello mostrato (1 Guided, 2 Operational, 3 Crisis).")]
        [SerializeField] private DifficultyLevel level = DifficultyLevel.Guided;
        [Tooltip("Chiave del nome. Vuota = quella del livello in SO_DifficultyStyle.")]
        [SerializeField] private string nameKey;
        [Tooltip("Fondo su cui sta il badge (colori delle barre e del testo).")]
        [SerializeField] private LabelSurface surface = LabelSurface.Light;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private DifficultyStyle difficultyStyle;
        [SerializeField] private RoundedRectGraphic[] bars = new RoundedRectGraphic[DifficultyStyle.BAR_COUNT];
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text nameText;

        /// <summary>Livello mostrato.</summary>
        public DifficultyLevel Level => level;
        /// <summary>Fondo corrente.</summary>
        public LabelSurface Surface => surface;

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
        /// Imposta il livello (e, se serve, la chiave del suo nome) e aggiorna l'etichetta.
        /// </summary>
        /// <param name="newLevel">Numero del livello, 1–3 (valori fuori scala vengono limitati).</param>
        /// <param name="newNameKey">Chiave del nome (es. "ui.level.guided"). Null/vuota = quella di SO_DifficultyStyle.</param>
        public void SetLevel(int newLevel, string newNameKey = null)
        {
            level = (DifficultyLevel)Mathf.Clamp(newLevel, (int)DifficultyLevel.Guided, (int)DifficultyLevel.Crisis);
            nameKey = newNameKey;
            Apply();
        }

        /// <summary>
        /// Imposta il livello dall'enum comune.
        /// </summary>
        /// <param name="newLevel">Livello.</param>
        public void SetLevel(DifficultyLevel newLevel)
        {
            SetLevel((int)newLevel);
        }

        /// <summary>
        /// Cambia il fondo (chiaro / scuro) e aggiorna i colori.
        /// </summary>
        /// <param name="newSurface">Fondo su cui sta il badge.</param>
        public void SetSurface(LabelSurface newSurface)
        {
            surface = newSurface;
            Apply();
        }

        /// <summary>
        /// Aggiorna barre, testi, colori e larghezza dai dati correnti.
        /// </summary>
        public void Apply()
        {
            if (theme == null || difficultyStyle == null || strings == null || levelText == null || nameText == null) return;
            bool dark = surface == LabelSurface.Dark;
            DifficultyStyle.Entry entry = difficultyStyle.Get(level);
            int filled = entry != null ? entry.BarsFilled : (int)level;

            // Barre: piene = riempimento, vuote = solo contorno (nessuna ombra, nessun bordo di stato).
            for (int i = 0; i < bars.Length && i < BAR_RECTS.Length; i++)
            {
                if (bars[i] == null) continue;
                bool isFilled = i < filled;
                bars[i].Radius = BAR_RADIUS;
                bars[i].Stroke = isFilled ? 0f : difficultyStyle.BarStroke;
                LabelStyling.SetColor(bars[i], difficultyStyle.GetBarColor(isFilled, dark));
                LabelStyling.Place(bars[i].rectTransform, BAR_RECTS[i].x, BAR_RECTS[i].y, BAR_RECTS[i].width, BAR_RECTS[i].height);
            }

            // Testo: "Livello N" Bold + " · Nome" Regular, ricavati da ui.level.badge senza duplicare la frase.
            string resolvedNameKey = !string.IsNullOrEmpty(nameKey) ? nameKey : (entry != null ? entry.LabelKey : string.Empty);
            string levelName = string.IsNullOrEmpty(resolvedNameKey) ? string.Empty : strings.Get(resolvedNameKey);
            SplitBadge(strings.Get(DifficultyStyle.BADGE_FORMAT_KEY), (int)level, levelName, out string boldPart, out string regularPart);

            Color textColor = dark ? theme.TextOnDark : theme.TextPrimary;
            LabelStyling.StyleText(levelText, theme, TextStyleIds.BODY_BOLD, textColor);
            LabelStyling.StyleText(nameText, theme, TextStyleIds.BODY, textColor);
            if (levelText.text != boldPart) levelText.text = boldPart;
            if (nameText.text != regularPart) nameText.text = regularPart;

            // Hug: icona + spazio + testo Bold + testo Regular.
            float height = Mathf.Max(ICON_SIZE, PanelLayout.LineHeight(theme.Body));
            float boldWidth = LabelStyling.TextWidth(levelText);
            float regularWidth = LabelStyling.TextWidth(nameText);
            float textX = ICON_SIZE + GAP;
            LabelStyling.Place(levelText.rectTransform, textX, 0f, boldWidth, height);
            LabelStyling.Place(nameText.rectTransform, textX + boldWidth, 0f, regularWidth, height);
            LabelStyling.SetSize((RectTransform)transform, textX + boldWidth + regularWidth, height);
        }

        /// <summary>
        /// Divide il testo del badge in parte Bold ("Livello 2") e parte Regular (" · Operational") usando il formato
        /// di SO_UIStrings_It. Se il formato non contiene il segnaposto del nome, tutto il testo va in Bold.
        /// </summary>
        private static void SplitBadge(string pattern, int levelNumber, string levelName, out string boldPart, out string regularPart)
        {
            int nameIndex = pattern.IndexOf(NAME_PLACEHOLDER, System.StringComparison.Ordinal);
            if (nameIndex < 0)
            {
                boldPart = string.Format(CultureInfo.InvariantCulture, pattern, levelNumber, levelName);
                regularPart = string.Empty;
                return;
            }
            string head = pattern.Substring(0, nameIndex);
            string boldPattern = head.TrimEnd(' ', SEPARATOR);
            string tail = pattern.Substring(boldPattern.Length);
            boldPart = boldPattern.Replace("{0}", levelNumber.ToString(CultureInfo.InvariantCulture));
            regularPart = tail.Replace("{0}", levelNumber.ToString(CultureInfo.InvariantCulture)).Replace(NAME_PLACEHOLDER, levelName);
        }

        /// <summary>
        /// Collega i riferimenti e applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, DifficultyStyle newStyle, RoundedRectGraphic[] newBars, TMP_Text newLevelText, TMP_Text newNameText)
        {
            theme = newTheme;
            strings = newStrings;
            difficultyStyle = newStyle;
            bars = newBars;
            levelText = newLevelText;
            nameText = newNameText;
            Apply();
        }
    }
}
