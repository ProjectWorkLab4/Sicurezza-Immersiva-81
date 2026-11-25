using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Freccia circolare 40 × 40 del selettore (BTN_ArrowLeft / BTN_ArrowRight): fondo tile.neutral, icona chevron, pulsante uGUI
    /// senza transizione (hover/pressed li dà UIStateVisual). Al limite si attenua (alpha 0,4) ma resta cliccabile: il clic produce
    /// il suono di errore invece di muovere la scelta.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(CanvasGroup))]
    public class SettingsArrowButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RoundedPanelGraphic fill;
        [SerializeField] private Image icon;

        /// <summary>Lanciato al clic della freccia.</summary>
        public event Action Clicked;

        /// <summary>RectTransform della freccia.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>
        /// Collega il clic del pulsante all'evento <see cref="Clicked"/>.
        /// </summary>
        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        /// <summary>
        /// Scollega il clic.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>
        /// Inoltra il clic.
        /// </summary>
        private void HandleClick()
        {
            Clicked?.Invoke();
        }

        /// <summary>
        /// Colori dal tema: fondo tile.neutral, icona text.primary.
        /// </summary>
        /// <param name="theme">Tema SO_UITheme.</param>
        public void ApplyTheme(UITheme theme)
        {
            if (theme == null) return;
            if (fill != null && fill.color != theme.TileNeutral) fill.color = theme.TileNeutral;
            if (icon != null && icon.color != theme.TextPrimary) icon.color = theme.TextPrimary;
        }

        /// <summary>
        /// Attenua la freccia quando la scelta è al limite (opacità del tema, 0,4).
        /// </summary>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <param name="atLimit">true se non si può andare oltre in questa direzione.</param>
        public void SetAtLimit(UITheme theme, bool atLimit)
        {
            if (group == null || theme == null) return;
            float alpha = atLimit ? theme.GetNumber(SettingsTokens.ROW_DISABLED_OPACITY) : 1f;
            if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(Button newButton, CanvasGroup newGroup, RoundedPanelGraphic newFill, Image newIcon)
        {
            button = newButton;
            group = newGroup;
            fill = newFill;
            icon = newIcon;
        }
    }
}
