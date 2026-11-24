using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// BTN_SettingsTab (Figma UI_SettingsTab): scheda di PANEL_Settings, 180×48, icona 24 + etichetta Body, sul pannello vetro
    /// (6 schede: Comfort, Audio, Sottotitoli, Controlli, Accessibilità, Generale). Stati:
    /// - Default: nessun fondo, icona contorno + testo text.on-dark;
    /// - Hover: fondo brand.dark (Figma) + scala 1,04 + ombra (standard §4.1: nel Figma il solo fondo brand.dark sul vetro brand.dark
    ///   è quasi invisibile, quindi si aggiungono scala e ombra);
    /// - Active (= Selected di <see cref="UIStateVisual"/>): fondo surface.card, icona PIENA brand.dark, etichetta Body Bold brand.dark,
    ///   nessun bordo; senza spunta né glow (la scheda attiva si riconosce da fondo e grassetto).
    /// Il cambio di scheda lo decide il pannello che ospita le schede: ascolta <see cref="UIButton.Clicked"/> e imposta <see cref="Active"/>.
    /// </summary>
    [AddComponentMenu("SI81/UI/Settings Tab Button")]
    public class SettingsTabButton : UIButton
    {
        [Header("Icone della scheda")]
        [Tooltip("Icona contorno (stato normale), es. ICN_SelfImprovement.")]
        [SerializeField] private Sprite iconOutline;
        [Tooltip("Icona piena (scheda attiva), es. ICN_SelfImprovement_Filled.")]
        [SerializeField] private Sprite iconFilled;

        /// <summary>true se la scheda è quella attiva (fondo bianco, icona piena, etichetta Bold).</summary>
        public bool Active
        {
            get => stateVisual != null && stateVisual.Selected;
            set
            {
                if (stateVisual != null) stateVisual.Selected = value;
            }
        }

        /// <summary>Icona piena quando attiva, contorno altrimenti.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override Sprite GetIconSprite(UIVisualState state)
        {
            return state == UIVisualState.Selected && iconFilled != null ? iconFilled : iconOutline;
        }

        /// <summary>
        /// Misure Figma: 180×48, padding sinistro 12, icona 24, gap 8, etichetta a sinistra dopo l'icona.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        protected override void LayoutContent(UIVisualState state)
        {
            float width = theme.GetNumber(ButtonTokens.TAB_WIDTH);
            float height = theme.GetNumber(ButtonTokens.TAB_HEIGHT);
            float padding = theme.GetNumber(ButtonTokens.TAB_PADDING);
            float gap = theme.GetNumber(ButtonTokens.TAB_GAP);
            float iconSize = theme.GetNumber(ButtonTokens.ICON);
            UIComponentUtil.SetSize((RectTransform)transform, width, height);
            if (icon != null) UIComponentUtil.SetCentered((RectTransform)icon.transform, new Vector2(-width * 0.5f + padding + iconSize * 0.5f, 0f), new Vector2(iconSize, iconSize));
            if (label != null)
            {
                float left = padding + iconSize + gap;
                PanelLayout.SetRect((RectTransform)label.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(left, 0f), new Vector2(-padding, 0f));
            }
            if (fill != null) UIComponentUtil.SetInset(fill.rectTransform, 0f);
        }

        /// <summary>Nessun bordo su nessuno stato.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override bool HasBorder(UIVisualState state)
        {
            return false;
        }

        /// <summary>
        /// Fondo: trasparente a riposo, brand.dark in hover/pressed, surface.card se attiva.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        protected override Color GetFillColor(UIVisualState state)
        {
            switch (state)
            {
                case UIVisualState.Selected: return theme.SurfaceCard;
                case UIVisualState.Hover:
                case UIVisualState.Pressed: return theme.BrandDark;
                case UIVisualState.Disabled: return theme.ButtonDisabled;
                default: return UIComponentUtil.WithAlpha(theme.BrandDark, 0f);
            }
        }

        /// <summary>Body Bold se attiva, Body altrimenti.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override string GetLabelStyle(UIVisualState state)
        {
            return state == UIVisualState.Selected ? TextStyleIds.BODY_BOLD : TextStyleIds.BODY;
        }

        /// <summary>brand.dark se attiva, text.on-dark altrimenti.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override string GetLabelColorToken(UIVisualState state)
        {
            if (state == UIVisualState.Disabled) return ColorTokens.BUTTON_DISABLED_TEXT;
            return state == UIVisualState.Selected ? ColorTokens.BRAND_DARK : ColorTokens.TEXT_ON_DARK;
        }

        /// <summary>
        /// Imposta icone e chiave da codice (le 6 schede di PANEL_Settings sono istanze di questo prefab con dati diversi).
        /// </summary>
        /// <param name="outline">Icona contorno.</param>
        /// <param name="filled">Icona piena.</param>
        /// <param name="newLabelKey">Chiave dell'etichetta, es. ui.settings.tab.audio.</param>
        public void SetData(Sprite outline, Sprite filled, string newLabelKey)
        {
            iconOutline = outline;
            iconFilled = filled;
            iconSprite = outline;
            labelKey = newLabelKey;
            Apply();
        }
    }
}
