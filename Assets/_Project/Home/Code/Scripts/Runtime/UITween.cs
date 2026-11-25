using System;
using System.Collections;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Animazioni comuni della UI (dissolvenza di un CanvasGroup, scala, colore) come coroutine a TEMPO NON SCALATO
    /// (funzionano anche con Time.timeScale = 0, es. in pausa). Un solo posto per questo codice: UIScreen e
    /// BottomBarItem lo usano, i prossimi componenti (card, pulsanti) devono fare altrettanto. Nessun Animator.
    /// Le durate vengono dal tema (<see cref="UITheme.MotionSettings"/>): chi chiama passa la durata già letta da lì.
    /// Uso tipico: <c>fadeRoutine = UITween.Play(this, fadeRoutine, UITween.Fade(group, 1f, theme.Motion.FadeDuration, () =&gt; fadeRoutine = null));</c>
    /// </summary>
    public static class UITween
    {
        /// <summary>
        /// Ferma la coroutine precedente (se c'è) e avvia la nuova. Restituisce null se il proprietario non è attivo
        /// (una coroutine non può partire su un GameObject disattivato): in quel caso chi chiama applica lo stato finale.
        /// </summary>
        /// <param name="owner">Componente che ospita la coroutine.</param>
        /// <param name="previous">Coroutine in corso da fermare (può essere null).</param>
        /// <param name="routine">Animazione da avviare (es. <see cref="Fade"/>).</param>
        /// <returns>La nuova coroutine, o null se non è partita.</returns>
        public static Coroutine Play(MonoBehaviour owner, Coroutine previous, IEnumerator routine)
        {
            if (owner == null) return null;
            if (previous != null) owner.StopCoroutine(previous);
            return owner.isActiveAndEnabled ? owner.StartCoroutine(routine) : null;
        }

        /// <summary>
        /// Dissolvenza dell'alpha di un CanvasGroup verso il valore finale (interpolazione lineare).
        /// </summary>
        /// <param name="group">CanvasGroup da animare.</param>
        /// <param name="targetAlpha">Alpha finale.</param>
        /// <param name="duration">Durata in secondi (tempo non scalato).</param>
        /// <param name="onDone">Chiamata a fine animazione (può essere null).</param>
        public static IEnumerator Fade(CanvasGroup group, float targetAlpha, float duration, Action onDone = null)
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = targetAlpha;
            onDone?.Invoke();
        }

        /// <summary>
        /// Scala uniforme di un Transform verso il valore finale (interpolazione lineare).
        /// </summary>
        /// <param name="target">Transform da scalare.</param>
        /// <param name="targetScale">Scala finale (uguale sui tre assi).</param>
        /// <param name="duration">Durata in secondi (tempo non scalato).</param>
        /// <param name="onDone">Chiamata a fine animazione (può essere null).</param>
        public static IEnumerator Scale(Transform target, float targetScale, float duration, Action onDone = null)
        {
            float start = target.localScale.x;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.one * Mathf.Lerp(start, targetScale, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            target.localScale = Vector3.one * targetScale;
            onDone?.Invoke();
        }

        /// <summary>
        /// Colore (alpha compreso) di una grafica uGUI verso il valore finale (interpolazione lineare).
        /// </summary>
        /// <param name="graphic">Grafica da colorare.</param>
        /// <param name="targetColor">Colore finale.</param>
        /// <param name="duration">Durata in secondi (tempo non scalato).</param>
        /// <param name="onDone">Chiamata a fine animazione (può essere null).</param>
        public static IEnumerator ColorTo(UnityEngine.UI.Graphic graphic, Color targetColor, float duration, Action onDone = null)
        {
            Color start = graphic.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                graphic.color = Color.Lerp(start, targetColor, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            graphic.color = targetColor;
            onDone?.Invoke();
        }
    }
}
