using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// LBL_StarRating: da 0 a 5 stelle (ICN_Star_Filled piena, ICN_Star vuota) + valore "N/5" (ui.format.stars).
    /// Le stelle sono il grado (rank) del punteggio sicurezza (CLAUDE.md 4.6). Solo su superfici chiare (testo nero).
    /// Non interattiva (nessun raycast). Misure Figma: stelle 24 con spazio 2, valore Body a destra (153×24 con valore).
    /// Uso: <c>SetStars(4)</c>; <c>SetStars(4, false)</c> per nascondere "4/5".
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("SI81/UI/Star Rating")]
    public class StarRating : MonoBehaviour
    {
        /// <summary>Numero massimo di stelle.</summary>
        public const int MAX_STARS = 5;
        /// <summary>Lato di una stella (Figma 24).</summary>
        public const float STAR_SIZE = 24f;
        /// <summary>Spazio tra le stelle (Figma 2).</summary>
        public const float STAR_GAP = 2f;
        /// <summary>Chiave del valore, es. "4/5".</summary>
        public const string VALUE_KEY = "ui.format.stars";

        [Header("Dati")]
        [Tooltip("Stelle piene, da 0 a 5.")]
        [Range(0, MAX_STARS)]
        [SerializeField] private int stars;
        [Tooltip("Mostra il valore \"N/5\" a destra delle stelle.")]
        [SerializeField] private bool showValue = true;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private Image[] starImages = new Image[MAX_STARS];
        [SerializeField] private Sprite starOutline;
        [SerializeField] private Sprite starFilled;
        [SerializeField] private TMP_Text valueText;

        /// <summary>Stelle piene mostrate.</summary>
        public int Stars => stars;

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
        /// Imposta le stelle piene (limitate a 0–5) e aggiorna.
        /// </summary>
        /// <param name="newStars">Numero di stelle piene.</param>
        public void SetStars(int newStars)
        {
            stars = Mathf.Clamp(newStars, 0, MAX_STARS);
            Apply();
        }

        /// <summary>
        /// Imposta le stelle piene e se mostrare il valore "N/5".
        /// </summary>
        /// <param name="newStars">Numero di stelle piene.</param>
        /// <param name="withValue">true per mostrare il valore a destra.</param>
        public void SetStars(int newStars, bool withValue)
        {
            showValue = withValue;
            SetStars(newStars);
        }

        /// <summary>
        /// Aggiorna sprite delle stelle, colori, valore e larghezza dai dati correnti.
        /// </summary>
        public void Apply()
        {
            if (theme == null || strings == null) return;
            Color color = theme.TextPrimary;
            for (int i = 0; i < starImages.Length && i < MAX_STARS; i++)
            {
                Image star = starImages[i];
                if (star == null) continue;
                Sprite wanted = i < stars ? starFilled : starOutline;
                if (star.sprite != wanted) star.sprite = wanted;
                star.raycastTarget = false;
                LabelStyling.SetColor(star, color);
                LabelStyling.Place(star.rectTransform, i * (STAR_SIZE + STAR_GAP), 0f, STAR_SIZE, STAR_SIZE);
            }

            float width = MAX_STARS * STAR_SIZE + (MAX_STARS - 1) * STAR_GAP;
            if (valueText != null)
            {
                PanelLayout.SetActive(valueText.gameObject, showValue);
                if (showValue)
                {
                    LabelStyling.StyleText(valueText, theme, TextStyleIds.BODY, color);
                    string text = strings.Format(VALUE_KEY, stars);
                    if (valueText.text != text) valueText.text = text;
                    float valueWidth = LabelStyling.TextWidth(valueText);
                    LabelStyling.Place(valueText.rectTransform, width + STAR_GAP, 0f, valueWidth, STAR_SIZE);
                    width += STAR_GAP + valueWidth;
                }
            }
            LabelStyling.SetSize((RectTransform)transform, width, STAR_SIZE);
        }

        /// <summary>
        /// Collega i riferimenti e applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, Image[] newStars, Sprite outline, Sprite filled, TMP_Text newValueText)
        {
            theme = newTheme;
            strings = newStrings;
            starImages = newStars;
            starOutline = outline;
            starFilled = filled;
            valueText = newValueText;
            Apply();
        }
    }
}
