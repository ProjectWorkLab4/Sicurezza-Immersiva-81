using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// BTN_Back (Figma UI_BackButton): pulsante tondo-quadrato 40×40 con ICN_ArrowBack 24. Al clic torna alla schermata
    /// precedente con <see cref="UIScreenManager.Back"/> (cronologia gestita dal manager). Riempimento surface.card senza ombra
    /// a riposo; hover = tile.neutral + scala 1,04 + ombra; pressed = scala 0,98. Nascondere il pulsante nelle schermate radice
    /// (Home) è compito del pannello che lo ospita (PANEL_Menu / MenuPanel), non di questo componente.
    /// </summary>
    [AddComponentMenu("SI81/UI/Back Button")]
    public class BackButton : UIButton
    {
        [Tooltip("true = il pulsante sta dentro una scheda bianca: fondo tile.neutral (Figma Statistiche B).")]
        [SerializeField] private bool inCardLook;

        /// <summary>
        /// Colore di riempimento: dentro una scheda bianca (<c>inCardLook</c>) usa tile.neutral, altrimenti quello di UIButton.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        protected override Color GetFillColor(UIVisualState state)
        {
            if (inCardLook && state != UIVisualState.Disabled) return theme.TileNeutral;
            return base.GetFillColor(state);
        }

        /// <summary>
        /// Misure Figma: 40×40 (size.btn.back), icona 24 al centro, nessuna etichetta.
        /// </summary>
        /// <param name="state">Stato visivo.</param>
        protected override void LayoutContent(UIVisualState state)
        {
            float side = theme.GetNumber(ButtonTokens.BACK);
            float iconSize = theme.GetNumber(ButtonTokens.ICON);
            UIComponentUtil.SetSize((RectTransform)transform, side, side);
            if (icon != null) UIComponentUtil.SetCentered((RectTransform)icon.transform, Vector2.zero, new Vector2(iconSize, iconSize));
            if (fill != null) UIComponentUtil.SetInset(fill.rectTransform, 0f);
        }

        /// <summary>BTN_Back non ha bordo.</summary>
        /// <param name="state">Stato visivo.</param>
        protected override bool HasBorder(UIVisualState state)
        {
            return false;
        }

        /// <summary>
        /// Clic: torna alla schermata precedente (nessun effetto sulla radice, vedi UIScreenManager.Back). Il suono SFX_UIBack
        /// verrà collegato dal passo audio.
        /// </summary>
        protected override void OnClicked()
        {
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.Back();
        }
    }
}
