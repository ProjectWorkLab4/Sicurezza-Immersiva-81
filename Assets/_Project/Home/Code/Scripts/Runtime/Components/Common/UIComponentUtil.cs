using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Funzioni comuni dei componenti del Blocco A (pulsanti, etichette, card, righe): colore con dissolvenza, posizionamento
    /// di un RectTransform al centro o a filo con un'inset, misura del testo. Un solo posto per questo codice (brief p. 8).
    /// Nessun Layout Group: ogni componente calcola le misure una volta con queste funzioni. Si appoggia a
    /// <see cref="PanelLayout"/> (scrittura dei RectTransform solo se cambiano) e a <see cref="UITween"/>.
    /// </summary>
    public static class UIComponentUtil
    {
        /// <summary>
        /// Porta il colore di una grafica al valore dato: con dissolvenza (tempo non scalato) se siamo in Play Mode
        /// e <paramref name="instant"/> è false, altrimenti subito. Scrive solo se il colore cambia.
        /// </summary>
        /// <param name="owner">Componente che ospita la coroutine.</param>
        /// <param name="routine">Coroutine in corso (aggiornata).</param>
        /// <param name="graphic">Grafica da colorare (può essere null).</param>
        /// <param name="target">Colore finale (alpha compreso).</param>
        /// <param name="duration">Durata in secondi.</param>
        /// <param name="instant">true = senza transizione.</param>
        public static void SetColor(MonoBehaviour owner, ref Coroutine routine, Graphic graphic, Color target, float duration, bool instant)
        {
            if (graphic == null) return;
            if (instant || duration <= 0f || owner == null || !owner.isActiveAndEnabled || !Application.isPlaying)
            {
                if (routine != null && owner != null) owner.StopCoroutine(routine);
                routine = null;
                if (graphic.color != target) graphic.color = target;
                return;
            }
            if (graphic.color == target && routine == null) return;
            routine = UITween.Play(owner, routine, UITween.ColorTo(graphic, target, duration));
        }

        /// <summary>
        /// Posiziona un RectTransform con ancore e pivot al centro del genitore, dato il centro e la dimensione.
        /// </summary>
        /// <param name="rect">RectTransform da posizionare (può essere null).</param>
        /// <param name="center">Centro rispetto al centro del genitore, in unità Canvas (Y verso l'alto).</param>
        /// <param name="size">Larghezza e altezza.</param>
        public static void SetCentered(RectTransform rect, Vector2 center, Vector2 size)
        {
            if (rect == null) return;
            var half = size * 0.5f;
            PanelLayout.SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), center - half, center + half);
        }

        /// <summary>
        /// Allarga un RectTransform a tutto il genitore, rientrato di <paramref name="inset"/> su ogni lato.
        /// </summary>
        /// <param name="rect">RectTransform da allargare (può essere null).</param>
        /// <param name="inset">Rientro per lato, in unità Canvas.</param>
        public static void SetInset(RectTransform rect, float inset)
        {
            if (rect == null) return;
            PanelLayout.SetRect(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(inset, inset), new Vector2(-inset, -inset));
        }

        /// <summary>
        /// Imposta la dimensione di un RectTransform (ancore invariate), scrivendo solo se cambia.
        /// </summary>
        /// <param name="rect">RectTransform della radice del componente.</param>
        /// <param name="width">Larghezza in unità Canvas.</param>
        /// <param name="height">Altezza in unità Canvas.</param>
        public static void SetSize(RectTransform rect, float width, float height)
        {
            if (rect == null) return;
            if (Mathf.Abs(rect.rect.width - width) > 0.001f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (Mathf.Abs(rect.rect.height - height) > 0.001f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// Larghezza di un testo TMP su una riga con il suo stile attuale, arrotondata per eccesso.
        /// </summary>
        /// <param name="text">Testo TMP (può essere null).</param>
        /// <returns>Larghezza in unità Canvas (0 se manca).</returns>
        public static float MeasureWidth(TMP_Text text)
        {
            if (text == null) return 0f;
            return Mathf.Ceil(text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity).x);
        }

        /// <summary>
        /// Colore di un token con alpha forzato (es. trasparente per nascondere una grafica senza cambiare tinta).
        /// </summary>
        /// <param name="color">Colore di partenza.</param>
        /// <param name="alpha">Nuovo alpha 0–1.</param>
        /// <returns>Il colore con il nuovo alpha.</returns>
        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
