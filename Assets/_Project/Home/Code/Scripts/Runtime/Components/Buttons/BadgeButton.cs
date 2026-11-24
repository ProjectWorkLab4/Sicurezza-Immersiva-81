using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// BTN_Badge (Figma UI_Badge): badge del profilo, 64×64. Due varianti che differiscono per FORMA e TESTO, non solo per colore:
    /// - Sbloccato: cerchio brand.light (raggio 32) con icona PIENA 32 in brand.dark (es. ICN_WorkspacePremium_Filled);
    /// - Bloccato: quadrato tile.neutral (radius.card) con ICN_Lock 24 in text.secondary e la scritta "Bloccato" (ui.profile.badge_locked).
    /// Hover/pressed standard (scala + ombra) da <see cref="UIStateVisual"/>. La selezione apre una scheda di dettaglio non ancora
    /// progettata (FIGMA_SPECS punto aperto 9): chi usa il badge ascolta <see cref="UIButton.Clicked"/>.
    /// </summary>
    [AddComponentMenu("SI81/UI/Badge Button")]
    public class BadgeButton : UIButton
    {
        [Header("Badge")]
        [Tooltip("true = sbloccato (cerchio con icona piena), false = bloccato (quadrato con lucchetto e scritta).")]
        [SerializeField] private bool unlocked = true;
        [Tooltip("Icona del lucchetto (ICN_Lock) mostrata da bloccato.")]
        [SerializeField] private Sprite lockSprite;
        [Tooltip("Sprite del riempimento da sbloccato (cerchio, TEX_BadgeRounded).")]
        [SerializeField] private Sprite unlockedFill;
        [Tooltip("Sprite del riempimento da bloccato (quadrato arrotondato, TEX_CardRounded).")]
        [SerializeField] private Sprite lockedFill;
        [Tooltip("Ombra a riposo da sbloccato (cerchio).")]
        [SerializeField] private Sprite unlockedShadowRest;
        [Tooltip("Ombra di sollevamento da sbloccato (cerchio).")]
        [SerializeField] private Sprite unlockedShadowLift;
        [Tooltip("Ombra a riposo da bloccato (quadrato).")]
        [SerializeField] private Sprite lockedShadowRest;
        [Tooltip("Ombra di sollevamento da bloccato (quadrato).")]
        [SerializeField] private Sprite lockedShadowLift;

        /// <summary>true se il badge è sbloccato.</summary>
        public bool Unlocked => unlocked;

        /// <summary>
        /// Imposta sbloccato/bloccato e riapplica forma, icona e testo.
        /// </summary>
        /// <param name="isUnlocked">true = sbloccato.</param>
        public void SetUnlocked(bool isUnlocked)
        {
            unlocked = isUnlocked;
            Apply();
        }

        /// <summary>
        /// Imposta stato e icona piena del badge (es. ICN_LocalFireDepartment_Filled) in un colpo solo.
        /// </summary>
        /// <param name="isUnlocked">true = sbloccato.</param>
        /// <param name="filledIcon">Icona piena da mostrare da sbloccato (null = lascia quella attuale).</param>
        public void SetData(bool isUnlocked, Sprite filledIcon)
        {
            if (filledIcon != null) iconSprite = filledIcon;
            unlocked = isUnlocked;
            Apply();
        }

        /// <summary>Lucchetto da bloccato, icona del badge da sbloccato.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override Sprite GetIconSprite(UIVisualState state)
        {
            return unlocked ? iconSprite : lockSprite;
        }

        /// <summary>Icona sempre visibile (anche da disabilitato).</summary>
        /// <param name="state">Stato visivo.</param>
        protected override bool ShowIcon(UIVisualState state)
        {
            return true;
        }

        /// <summary>
        /// Etichetta "Bloccato" visibile solo da bloccato.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        protected override void ApplyLabel(UIVisualState state)
        {
            base.ApplyLabel(state);
            if (label != null) PanelLayout.SetActive(label.gameObject, !unlocked);
        }

        /// <summary>
        /// Forma (sprite di riempimento e ombre), icona e testo secondo lo stato sbloccato/bloccato; 64×64 in entrambi i casi.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        protected override void LayoutContent(UIVisualState state)
        {
            float side = theme.GetNumber(ButtonTokens.BADGE);
            UIComponentUtil.SetSize((RectTransform)transform, side, side);
            if (fill != null)
            {
                Sprite wanted = unlocked ? unlockedFill : lockedFill;
                if (wanted != null && fill.sprite != wanted) fill.sprite = wanted;
                UIComponentUtil.SetInset(fill.rectTransform, 0f);
            }
            if (stateVisual != null)
            {
                stateVisual.ShadowRestSprite = unlocked ? unlockedShadowRest : lockedShadowRest;
                stateVisual.ShadowLiftSprite = unlocked ? unlockedShadowLift : lockedShadowLift;
                stateVisual.CornerRadius = unlocked ? side * 0.5f : theme.RadiusCard;
            }

            if (unlocked)
            {
                float iconSize = theme.GetNumber(ButtonTokens.BADGE_ICON);
                if (icon != null) UIComponentUtil.SetCentered((RectTransform)icon.transform, Vector2.zero, new Vector2(iconSize, iconSize));
                return;
            }
            // Bloccato: lucchetto 24 sopra, scritta Body sotto, colonna centrata.
            float lockSize = theme.GetNumber(ButtonTokens.ICON);
            float lineHeight = PanelLayout.LineHeight(theme.Body);
            const float GAP = 2f;
            float total = lockSize + GAP + lineHeight;
            if (icon != null) UIComponentUtil.SetCentered((RectTransform)icon.transform, new Vector2(0f, total * 0.5f - lockSize * 0.5f), new Vector2(lockSize, lockSize));
            if (label != null)
            {
                TMP_Text tmp = label.GetComponent<TMP_Text>();
                float width = Mathf.Max(UIComponentUtil.MeasureWidth(tmp), side);
                UIComponentUtil.SetCentered((RectTransform)label.transform, new Vector2(0f, -(total * 0.5f - lineHeight * 0.5f)), new Vector2(width, lineHeight));
            }
        }

        /// <summary>Nessun bordo.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override bool HasBorder(UIVisualState state)
        {
            return false;
        }

        /// <summary>brand.light da sbloccato, tile.neutral da bloccato.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override Color GetFillColor(UIVisualState state)
        {
            if (state == UIVisualState.Disabled) return theme.ButtonDisabled;
            return unlocked ? theme.BrandLight : theme.TileNeutral;
        }

        /// <summary>Body (la scritta "Bloccato").</summary>
        /// <param name="state">Stato visivo.</param>
        protected override string GetLabelStyle(UIVisualState state)
        {
            return TextStyleIds.BODY;
        }

        /// <summary>
        /// Colori: icona brand.dark (sbloccato) o text.secondary (lucchetto); scritta text.primary.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        /// <param name="instant">true = senza dissolvenza.</param>
        protected override void ApplyLook(UIVisualState state, bool instant)
        {
            base.ApplyLook(state, instant);
            if (icon == null || theme == null || state == UIVisualState.Disabled) return;
            Color iconColor = theme.GetColor(unlocked ? ColorTokens.BRAND_DARK : ColorTokens.TEXT_SECONDARY);
            if (icon.color != iconColor) icon.color = iconColor;
        }

        /// <summary>
        /// Imposta sprite e dati. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureBadge(bool isUnlocked, Sprite badgeIcon, Sprite newLockSprite, Sprite newUnlockedFill, Sprite newLockedFill,
            Sprite newUnlockedShadowRest, Sprite newUnlockedShadowLift, Sprite newLockedShadowRest, Sprite newLockedShadowLift)
        {
            unlocked = isUnlocked;
            iconSprite = badgeIcon;
            lockSprite = newLockSprite;
            unlockedFill = newUnlockedFill;
            lockedFill = newLockedFill;
            unlockedShadowRest = newUnlockedShadowRest;
            unlockedShadowLift = newUnlockedShadowLift;
            lockedShadowRest = newLockedShadowRest;
            lockedShadowLift = newLockedShadowLift;
            Apply();
        }
    }
}
