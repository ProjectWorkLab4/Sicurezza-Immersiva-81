using UnityEngine;
using UnityEngine.Events;

namespace SI81.UI
{
    /// <summary>
    /// Radice di una schermata menu (PANEL_Home, PANEL_MissionBrief...). Va messo sul GameObject radice del prefab
    /// della schermata, figlio del contenitore "PANEL_Screens" di MNG_UI: <see cref="UIScreenManager"/> lo trova e lo registra.
    /// Mostra e nasconde la schermata con una dissolvenza del CanvasGroup (durata dal tema, tempo non scalato,
    /// quindi funziona anche in pausa; nessun Animator). A schermata nascosta il GameObject viene disattivato.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        [Tooltip("Identificativo della schermata (unico sotto lo stesso MNG_UI).")]
        [SerializeField] private UIScreenId id = UIScreenId.Home;
        [Tooltip("Tema comune SO_UITheme: fornisce la durata della dissolvenza (Motion.ScreenTransitionDuration).")]
        [SerializeField] private UITheme theme;
        [Tooltip("CanvasGroup della radice (assegnato dal builder; se vuoto viene cercato sullo stesso GameObject).")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Chiamato quando la schermata ha finito di comparire.")]
        [SerializeField] private UnityEvent onShown = new UnityEvent();
        [Tooltip("Chiamato quando la schermata ha finito di sparire (subito prima della disattivazione).")]
        [SerializeField] private UnityEvent onHidden = new UnityEvent();

        // Dissolvenza in corso (null se nessuna).
        private Coroutine fadeRoutine;
        // Stato logico: true da Show() fino al prossimo Hide().
        private bool isVisible;

        /// <summary>Identificativo della schermata.</summary>
        public UIScreenId Id => id;
        /// <summary>true se la schermata è mostrata (o sta comparendo).</summary>
        public bool IsVisible => isVisible;
        /// <summary>Evento: la schermata ha finito di comparire.</summary>
        public UnityEvent OnShown => onShown;
        /// <summary>Evento: la schermata ha finito di sparire.</summary>
        public UnityEvent OnHidden => onHidden;

        /// <summary>
        /// Recupera il CanvasGroup se non è stato assegnato.
        /// </summary>
        private void Awake()
        {
            EnsureCanvasGroup();
        }

        /// <summary>
        /// Mostra la schermata: attiva il GameObject e porta l'alpha a 1 con una dissolvenza.
        /// </summary>
        /// <param name="instant">true per mostrarla subito, senza dissolvenza.</param>
        public void Show(bool instant = false)
        {
            EnsureCanvasGroup();
            isVisible = true;
            gameObject.SetActive(true);
            StartFade(1f, instant);
        }

        /// <summary>
        /// Nasconde la schermata: blocca subito l'interazione, porta l'alpha a 0 e poi disattiva il GameObject.
        /// </summary>
        /// <param name="instant">true per nasconderla subito, senza dissolvenza.</param>
        public void Hide(bool instant = false)
        {
            EnsureCanvasGroup();
            isVisible = false;
            // Una schermata già disattivata non può avviare coroutine: va nascosta subito.
            StartFade(0f, instant || !gameObject.activeInHierarchy);
        }

        /// <summary>
        /// Avvia (o completa subito) la dissolvenza verso l'alpha indicata.
        /// </summary>
        /// <param name="targetAlpha">Alpha finale (0 o 1).</param>
        /// <param name="instant">true per applicare subito lo stato finale.</param>
        private void StartFade(float targetAlpha, bool instant)
        {
            // Ferma la dissolvenza precedente (se in corso).
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            // Interazione: attiva solo quando la schermata è visibile; disattivata subito quando sparisce.
            canvasGroup.interactable = targetAlpha > 0f;
            canvasGroup.blocksRaycasts = targetAlpha > 0f;

            float duration = theme != null ? theme.Motion.ScreenTransitionDuration : 0f;
            if (instant || duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                FinishFade(targetAlpha);
                return;
            }
            // Dissolvenza comune (UITween: tempo non scalato). Se il GameObject non è attivo non parte: stato finale subito.
            fadeRoutine = UITween.Play(this, null, UITween.Fade(canvasGroup, targetAlpha, duration, () =>
            {
                fadeRoutine = null;
                FinishFade(targetAlpha);
            }));
            if (fadeRoutine == null)
            {
                canvasGroup.alpha = targetAlpha;
                FinishFade(targetAlpha);
            }
        }

        /// <summary>
        /// Chiude la dissolvenza: lancia l'evento e, se nascosta, disattiva il GameObject (nessun costo di disegno).
        /// </summary>
        /// <param name="targetAlpha">Alpha raggiunta.</param>
        private void FinishFade(float targetAlpha)
        {
            if (targetAlpha > 0f)
            {
                onShown.Invoke();
                return;
            }
            onHidden.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Recupera il CanvasGroup dallo stesso GameObject se il riferimento è vuoto.
        /// </summary>
        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Imposta id e tema. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UIScreenId newId, UITheme newTheme)
        {
            id = newId;
            theme = newTheme;
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
