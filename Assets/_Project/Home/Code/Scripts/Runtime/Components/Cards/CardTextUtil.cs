using System;
using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Piccole utilità condivise dalle card (un solo posto, nessun codice duplicato).
    /// </summary>
    public static class CardTextUtil
    {
        /// <summary>Separatore dei titoli composti ("Training · Tutorial comune").</summary>
        public const string SEPARATOR = " · ";

        /// <summary>
        /// Mette in grassetto la parte prima del separatore " · " e lascia il resto Regular
        /// (Figma: "Training" Bold + " · Tutorial comune" Regular). Senza separatore il testo resta com'è.
        /// </summary>
        /// <param name="full">Testo completo, già formattato.</param>
        /// <returns>Testo con tag rich text &lt;b&gt;.</returns>
        public static string BoldBeforeSeparator(string full)
        {
            if (string.IsNullOrEmpty(full)) return string.Empty;
            int index = full.IndexOf(SEPARATOR, StringComparison.Ordinal);
            if (index <= 0) return full;
            return "<b>" + full.Substring(0, index) + "</b>" + full.Substring(index);
        }

        /// <summary>
        /// Larghezza preferita di un testo TMP su una riga (senza a capo), in unità Canvas.
        /// </summary>
        /// <param name="text">Testo TMP (stile già applicato).</param>
        /// <param name="content">Testo da misurare.</param>
        /// <returns>Larghezza in unità Canvas.</returns>
        public static float MeasureWidth(TMP_Text text, string content)
        {
            if (text == null || string.IsNullOrEmpty(content)) return 0f;
            return text.GetPreferredValues(content, 0f, 0f).x;
        }

        /// <summary>
        /// Posiziona un RectTransform con origine in alto a sinistra del genitore (x verso destra, y verso il basso).
        /// </summary>
        /// <param name="rect">Elemento da posizionare.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="y">Distanza dal bordo superiore.</param>
        /// <param name="width">Larghezza.</param>
        /// <param name="height">Altezza.</param>
        public static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Applica a un testo TMP lo stile del tema e il colore (solo se qualcosa cambia).
        /// </summary>
        /// <param name="text">Testo da configurare (può essere null).</param>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <param name="style">Nome dello stile (vedi <see cref="TextStyleIds"/>).</param>
        /// <param name="color">Colore del testo.</param>
        public static void Style(TMP_Text text, UITheme theme, string style, Color color)
        {
            if (text == null || theme == null) return;
            UITheme.TextStyle textStyle = theme.GetTextStyle(style);
            if (textStyle != null) textStyle.ApplyTo(text);
            text.color = color;
        }
    }
}
