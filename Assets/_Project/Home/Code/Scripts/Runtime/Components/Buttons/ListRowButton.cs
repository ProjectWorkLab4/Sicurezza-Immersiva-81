using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// BTN_ListRow (Figma UI_ListRow): riga di un elenco (Statistiche: una riga per scenario), 440×40, padding-x 12, gap 12.
    /// Colonne: titolo Body Bold (flex, "01 · Fire Starter"), posto delle stelle (153×24, istanza di LBL_StarRating) e punteggio
    /// Body "86/100" (56, allineato a destra). Stati:
    /// - Default: fondo surface.card; Hover: tile.neutral + scala 1,04 + ombra;
    /// - Selected: fondo brand.light (Figma) PIÙ sollevamento, glow brand.light e spunta (CLAUDE.md §4.1: scelta di progetto,
    ///   il Figma ha solo il fondo); nessun bordo;
    /// - NotPlayed: fondo surface.card, titolo Regular text.secondary e scritta "Non giocato" (ui.stats.not_played) al posto di
    ///   stelle e punteggio.
    /// La spunta è il cerchio compatto (size.state-check.compact) per stare in una riga alta 40.
    /// Dati con <see cref="SetData"/> / <see cref="SetNotPlayed"/>; la selezione con <see cref="Selected"/>.
    /// </summary>
    [AddComponentMenu("SI81/UI/List Row Button")]
    public class ListRowButton : UIButton
    {
        [Header("Riga")]
        [Tooltip("Tabella testi SO_UIStrings_It (per il punteggio e per \"Non giocato\").")]
        [SerializeField] private UIStrings strings;
        [Tooltip("true = scenario mai giocato: niente stelle né punteggio, scritta \"Non giocato\".")]
        [SerializeField] private bool notPlayed;
        [Tooltip("Posto delle stelle (153×24): vi sta un'istanza di LBL_StarRating.")]
        [SerializeField] private RectTransform starsSlot;
        [Tooltip("Componente stelle dentro starsSlot (opzionale: se manca si lascia il solo posto vuoto).")]
        [SerializeField] private StarRating starRating;
        [Tooltip("Testo a destra: punteggio N/100 oppure \"Non giocato\".")]
        [SerializeField] private UIStringText score;

        /// <summary>true se la riga è quella selezionata (fondo brand.light, sollevamento, glow, spunta).</summary>
        public bool Selected
        {
            get => stateVisual != null && stateVisual.Selected;
            set
            {
                if (stateVisual != null) stateVisual.Selected = value;
            }
        }

        /// <summary>Posto delle stelle (per inserirvi LBL_StarRating).</summary>
        public RectTransform StarsSlot => starsSlot;

        /// <summary>
        /// Imposta i dati di una riga giocata.
        /// </summary>
        /// <param name="title">Titolo già composto (ui.format.scenario_title: "01 · Fire Starter").</param>
        /// <param name="stars">Stelle 0–5.</param>
        /// <param name="scoreValue">Punteggio sicurezza 0–100.</param>
        public void SetData(string title, int stars, int scoreValue)
        {
            notPlayed = false;
            SetLabelText(title);
            if (starRating != null) starRating.SetStars(stars);
            if (score != null)
            {
                TMP_Text tmp = score.GetComponent<TMP_Text>();
                score.SetKey(string.Empty);
                if (tmp != null && strings != null) tmp.text = strings.Format("ui.format.score_of_100", scoreValue);
            }
            Apply();
        }

        /// <summary>
        /// Imposta i dati di uno scenario mai giocato (titolo + "Non giocato", senza stelle né punteggio).
        /// </summary>
        /// <param name="title">Titolo già composto.</param>
        public void SetNotPlayed(string title)
        {
            notPlayed = true;
            SetLabelText(title);
            if (score != null) score.SetKey("ui.stats.not_played");
            Apply();
        }

        /// <summary>
        /// Misure Figma: 440×40, padding 12, gap 12; da destra punteggio (56), stelle (153), titolo a riempimento.
        /// Da "non giocato" la scritta occupa il posto di stelle + punteggio e le stelle si nascondono.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        protected override void LayoutContent(UIVisualState state)
        {
            float width = theme.GetNumber(ButtonTokens.ROW_WIDTH);
            float height = theme.GetNumber(ButtonTokens.ROW_HEIGHT);
            float padding = theme.GetNumber(ButtonTokens.ROW_PADDING);
            float gap = theme.GetNumber(ButtonTokens.ROW_GAP);
            float starsWidth = theme.GetNumber(ButtonTokens.ROW_STARS_WIDTH);
            float scoreWidth = theme.GetNumber(ButtonTokens.ROW_SCORE_WIDTH);
            float starsHeight = StarRating.STAR_SIZE;
            UIComponentUtil.SetSize((RectTransform)transform, width, height);
            if (fill != null) UIComponentUtil.SetInset(fill.rectTransform, 0f);

            float right = width * 0.5f - padding;
            // Testo a destra (punteggio, oppure "Non giocato" sul posto di stelle + punteggio).
            float rightWidth = notPlayed ? scoreWidth + gap + starsWidth : scoreWidth;
            if (score != null) UIComponentUtil.SetCentered((RectTransform)score.transform, new Vector2(right - rightWidth * 0.5f, 0f), new Vector2(rightWidth, height));
            right -= rightWidth + gap;
            // Stelle.
            if (starsSlot != null)
            {
                PanelLayout.SetActive(starsSlot.gameObject, !notPlayed);
                UIComponentUtil.SetCentered(starsSlot, new Vector2(right - starsWidth * 0.5f, 0f), new Vector2(starsWidth, starsHeight));
            }
            if (!notPlayed) right -= starsWidth + gap;
            // Titolo: dal padding sinistro fino alle colonne di destra.
            float left = -width * 0.5f + padding;
            float titleWidth = Mathf.Max(0f, right - left);
            if (label != null) UIComponentUtil.SetCentered((RectTransform)label.transform, new Vector2(left + titleWidth * 0.5f, 0f), new Vector2(titleWidth, height));
        }

        /// <summary>Nessun bordo.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override bool HasBorder(UIVisualState state)
        {
            return false;
        }

        /// <summary>
        /// Fondo: surface.card; hover tile.neutral; selezionata brand.light (anche premuta o in hover).
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        protected override Color GetFillColor(UIVisualState state)
        {
            if (state == UIVisualState.Disabled) return theme.ButtonDisabled;
            if (Selected) return theme.BrandLight;
            return state == UIVisualState.Hover || state == UIVisualState.Pressed ? theme.TileNeutral : theme.SurfaceCard;
        }

        /// <summary>Body Bold; da non giocato Body Regular.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override string GetLabelStyle(UIVisualState state)
        {
            return notPlayed ? TextStyleIds.BODY : TextStyleIds.BODY_BOLD;
        }

        /// <summary>text.primary; da non giocato text.secondary.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override string GetLabelColorToken(UIVisualState state)
        {
            if (state == UIVisualState.Disabled) return ColorTokens.BUTTON_DISABLED_TEXT;
            return notPlayed && !Selected ? ColorTokens.TEXT_SECONDARY : ColorTokens.TEXT_PRIMARY;
        }

        /// <summary>
        /// Colora anche il testo di destra: stesso token del titolo da non giocato (text.secondary), text.primary altrimenti.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        /// <param name="instant">true = senza dissolvenza.</param>
        protected override void ApplyLook(UIVisualState state, bool instant)
        {
            base.ApplyLook(state, instant);
            if (score != null) score.SetStyle(TextStyleIds.BODY, GetLabelColorToken(state));
        }

        /// <summary>
        /// Imposta riferimenti propri della riga. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureRow(UIStrings newStrings, RectTransform newStarsSlot, StarRating newStarRating, UIStringText newScore)
        {
            strings = newStrings;
            starsSlot = newStarsSlot;
            starRating = newStarRating;
            score = newScore;
            Apply();
        }
    }
}
