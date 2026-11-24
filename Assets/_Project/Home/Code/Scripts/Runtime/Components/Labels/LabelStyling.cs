using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Funzioni condivise dalle etichette (stile testo del tema, posizionamento in alto a sinistra, larghezza del testo).
    /// Un solo posto per queste regole: nessun codice duplicato tra i componenti. Nessun Layout Group: il layout si calcola
    /// una volta nel codice.
    /// </summary>
    internal static class LabelStyling
    {
        /// <summary>
        /// Applica a un testo TMP lo stile del tema (font, dimensione) e il colore.
        /// </summary>
        /// <param name="text">Testo da configurare (può essere null).</param>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <param name="style">Nome dello stile (vedi <see cref="TextStyleIds"/>).</param>
        /// <param name="color">Colore del testo.</param>
        internal static void StyleText(TMP_Text text, UITheme theme, string style, Color color)
        {
            if (text == null || theme == null) return;
            UITheme.TextStyle textStyle = theme.GetTextStyle(style);
            if (textStyle != null) textStyle.ApplyTo(text);
            if (text.color != color) text.color = color;
        }

        /// <summary>
        /// Posiziona un RectTransform con origine in alto a sinistra del genitore (x verso destra, y verso il basso).
        /// </summary>
        /// <param name="rect">Elemento da posizionare.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="width">Larghezza.</param>
        /// <param name="height">Altezza.</param>
        internal static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null) return;
            PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x, -y - height), new Vector2(x + width, -y));
        }

        /// <summary>
        /// Posiziona un RectTransform largo quanto il genitore (con margini laterali) a una distanza dal bordo superiore.
        /// </summary>
        /// <param name="rect">Elemento da posizionare.</param>
        /// <param name="left">Margine sinistro.</param>
        /// <param name="right">Margine destro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="height">Altezza.</param>
        internal static void PlaceStretchX(RectTransform rect, float left, float right, float y, float height)
        {
            if (rect == null) return;
            PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(left, -y - height), new Vector2(-right, -y));
        }

        /// <summary>
        /// Imposta la dimensione di un RectTransform radice senza toccarne ancore e posizione.
        /// </summary>
        /// <param name="rect">Radice del componente.</param>
        /// <param name="width">Larghezza (negativa = invariata).</param>
        /// <param name="height">Altezza (negativa = invariata).</param>
        internal static void SetSize(RectTransform rect, float width, float height)
        {
            if (rect == null) return;
            if (width >= 0f && !Mathf.Approximately(rect.rect.width, width)) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (height >= 0f && !Mathf.Approximately(rect.rect.height, height)) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// Larghezza preferita di una riga di testo TMP (senza a capo), già con stile e testo applicati.
        /// </summary>
        /// <param name="text">Testo TMP.</param>
        /// <returns>Larghezza in unità Canvas (0 se il testo è null o vuoto).</returns>
        internal static float TextWidth(TMP_Text text)
        {
            if (text == null || string.IsNullOrEmpty(text.text)) return 0f;
            return Mathf.Ceil(text.GetPreferredValues(text.text).x);
        }

        /// <summary>
        /// Scrive il colore di una grafica solo se cambia (evita di sporcare la mesh).
        /// </summary>
        /// <param name="graphic">Grafica uGUI (può essere null).</param>
        /// <param name="color">Colore voluto.</param>
        internal static void SetColor(UnityEngine.UI.Graphic graphic, Color color)
        {
            if (graphic != null && graphic.color != color) graphic.color = color;
        }

        /// <summary>
        /// Attiva o disattiva un componente solo se lo stato cambia.
        /// </summary>
        /// <param name="behaviour">Componente (può essere null).</param>
        /// <param name="enabled">Stato voluto.</param>
        internal static void SetEnabled(Behaviour behaviour, bool enabled)
        {
            if (behaviour != null && behaviour.enabled != enabled) behaviour.enabled = enabled;
        }
    }
}
