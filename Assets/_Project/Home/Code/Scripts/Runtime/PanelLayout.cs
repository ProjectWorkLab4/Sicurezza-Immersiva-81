using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Funzioni di layout comuni ai pannelli vetro (PANEL_Menu, PANEL_Side, ...): vetro con riflesso, ombra 9-slice
    /// allargata del padding dello sprite, RectTransform scritti solo quando cambiano, altezze di riga del tema.
    /// Un solo posto per queste regole: nessun codice duplicato tra i pannelli (brief p. 8). Nessun Layout Group.
    /// </summary>
    public static class PanelLayout
    {
        /// <summary>
        /// Colora il vetro con surface.glass e attiva il riflesso diagonale del tema.
        /// </summary>
        /// <param name="glass">Grafica arrotondata del pannello (può essere null).</param>
        /// <param name="theme">Tema SO_UITheme.</param>
        public static void ApplyGlass(RoundedPanelGraphic glass, UITheme theme)
        {
            if (glass == null || theme == null) return;
            glass.color = theme.SurfaceGlass;
            glass.SetSheen(theme.Sheen);
        }

        /// <summary>
        /// Applica un'ombra del tema a un'Image 9-slice figlia del pannello: colore e alpha del token, RectTransform
        /// allargato di <see cref="PanelSprites.ShadowLayout.PaddingUnits"/> per lato (lo spostamento è già nello sprite).
        /// </summary>
        /// <param name="shadowImage">Image con lo sprite TEX_PanelShadow* (può essere null).</param>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <param name="role">Ruolo dell'ombra (vedi <see cref="ShadowRoles"/>).</param>
        public static void ApplyShadow(UnityEngine.UI.Image shadowImage, UITheme theme, string role)
        {
            if (shadowImage == null || theme == null) return;
            UITheme.ShadowToken shadow = theme.GetShadow(role);
            if (shadow != null && shadowImage.color != shadow.Color) shadowImage.color = shadow.Color;
            float expand = shadow != null ? PanelSprites.GetShadowLayout(theme.RadiusPanel, shadow).PaddingUnits : 0f;
            SetRect((RectTransform)shadowImage.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-expand, -expand), new Vector2(expand, expand));
        }

        /// <summary>
        /// Altezza di riga di uno stile di testo (dimensione × interlinea Figma, arrotondata per eccesso: Body 18 → 23).
        /// </summary>
        /// <param name="style">Stile del tema (può essere null).</param>
        /// <returns>Altezza in unità Canvas.</returns>
        public static float LineHeight(UITheme.TextStyle style)
        {
            return style != null ? Mathf.Ceil(style.Size * style.LineHeightPercent / 100f) : 0f;
        }

        /// <summary>
        /// Posiziona un RectTransform con l'angolo in alto a sinistra a (x, y) dall'angolo in alto a sinistra del genitore
        /// (y verso il basso), pivot al centro (la scala dell'hover parte dal centro).
        /// </summary>
        /// <param name="rect">Elemento da posizionare.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="width">Larghezza.</param>
        /// <param name="height">Altezza.</param>
        public static void PlaceTopLeftCentered(RectTransform rect, float x, float y, float width, float height)
        {
            SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(x, -(y + height)), new Vector2(x + width, -y));
        }

        /// <summary>
        /// Imposta ancore, pivot e offset di un RectTransform, scrivendo solo i valori che cambiano.
        /// </summary>
        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect.anchorMin != anchorMin) rect.anchorMin = anchorMin;
            if (rect.anchorMax != anchorMax) rect.anchorMax = anchorMax;
            if (rect.pivot != pivot) rect.pivot = pivot;
            if (rect.offsetMin != offsetMin) rect.offsetMin = offsetMin;
            if (rect.offsetMax != offsetMax) rect.offsetMax = offsetMax;
            if (rect.localScale != Vector3.one) rect.localScale = Vector3.one;
        }

        /// <summary>
        /// Attiva o disattiva un GameObject solo se lo stato cambia.
        /// </summary>
        public static void SetActive(GameObject go, bool active)
        {
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
