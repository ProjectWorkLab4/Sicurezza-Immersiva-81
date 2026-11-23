using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Gestisce HUD_EventAlert e HUD_ActionFeedback: UN avviso alla volta, in basso nella vista (~-15° sotto lo sguardo, ~1 m),
    /// 3 s, con suono e aptica; poi il testo passa all'ultima riga del pannello da polso. Posizione scelta SOLO quando l'avviso
    /// compare (dalla imbardata della testa): non è bloccato alla testa.
    /// Regole di coda (priorità Event &gt; Feedback):
    ///  - un Event interrompe subito un Feedback in corso (il Feedback interrotto non torna); un Event durante un altro Event si accoda (max 3, il più vecchio cade);
    ///  - un Feedback con un avviso in corso si accoda (max 2, il più vecchio cade); un Feedback su un altro Feedback lo sostituisce solo se quello è visibile da almeno 1 s;
    ///  - finito l'avviso parte prima la coda Event, poi la coda Feedback.
    /// Regole di livello (<see cref="ApplyLevel"/>): Guided evento Completo + ogni feedback; Operational solo titolo + solo feedback negativi; Crisis solo icona (+ sottotitolo) e nessun feedback.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Alert Presenter")]
    public class AlertPresenter : MonoBehaviour
    {
        /// <summary>Quante voci al massimo restano in coda per priorità Event.</summary>
        private const int MAX_EVENT_QUEUE = 3;
        /// <summary>Quante voci al massimo restano in coda per priorità Feedback.</summary>
        private const int MAX_FEEDBACK_QUEUE = 2;
        /// <summary>Tempo minimo di visibilità di un Feedback prima che un altro lo sostituisca, in secondi.</summary>
        private const float MIN_FEEDBACK_SECONDS = 1f;

        /// <summary>Un avviso in attesa o in corso.</summary>
        private struct Pending
        {
            /// <summary>Priorità dell'avviso (Evento o Feedback).</summary>
            public AlertPriority Priority;
            /// <summary>Titolo già risolto dalla tabella stringhe.</summary>
            public string Title;
            /// <summary>Testo secondario già risolto (può essere vuoto).</summary>
            public string Body;
            /// <summary>Nome dell'icona ICN_ da mostrare.</summary>
            public string IconName;
            /// <summary>Tipo di feedback (solo per priorità Feedback).</summary>
            public FeedbackKind Kind;
        }

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private AlertView eventView;
        [SerializeField] private AlertView feedbackView;
        [Tooltip("Distanza degli avvisi dalla testa in metri (~1 m).")]
        [SerializeField] private float distance = 1f;

        // Rig e servizi.
        private Transform head;
        private HUDIconLibrary icons;
        // Regole del livello.
        private EventAlertMode eventMode = EventAlertMode.Full;
        private DifficultyLevel level = DifficultyLevel.Guided;
        // Stato.
        private readonly List<Pending> eventQueue = new List<Pending>();
        private readonly List<Pending> feedbackQueue = new List<Pending>();
        private bool hasCurrent;
        private Pending current;
        private float currentStart;
        private float currentEnd;
        private bool hudVisible = true;
        private Coroutine eventFade;
        private Coroutine feedbackFade;

        /// <summary>Lanciato quando un avviso finisce (testo, icona): HUDRoot lo copia sul pannello da polso.</summary>
        public event Action<string, Sprite> AlertFinished;
        /// <summary>Lanciato quando compare un avviso evento in forma solo icona (testo da sottotitolare).</summary>
        public event Action<string> IconOnlyEventShown;

        /// <summary>Forma corrente degli avvisi evento.</summary>
        public EventAlertMode EventMode => eventMode;

        /// <summary>
        /// Imposta la testa e la libreria delle icone.
        /// </summary>
        /// <param name="newHead">Testa (camera) del giocatore.</param>
        /// <param name="newIcons">Libreria delle icone per nome.</param>
        public void Setup(Transform newHead, HUDIconLibrary newIcons)
        {
            head = newHead;
            icons = newIcons;
        }

        /// <summary>
        /// Spegne i Canvas all'avvio.
        /// </summary>
        private void Awake()
        {
            if (eventView != null) eventView.SetShown(false);
            if (feedbackView != null) feedbackView.SetShown(false);
        }

        /// <summary>
        /// Imposta le regole del livello.
        /// </summary>
        /// <param name="cfg">Configurazione del livello (null = Guided).</param>
        public void ApplyLevel(LevelConfig cfg)
        {
            level = cfg != null ? cfg.Level : DifficultyLevel.Guided;
            switch (level)
            {
                case DifficultyLevel.Operational: eventMode = EventAlertMode.TitleOnly; break;
                case DifficultyLevel.Crisis: eventMode = EventAlertMode.IconOnly; break;
                default: eventMode = EventAlertMode.Full; break;
            }
        }

        /// <summary>
        /// Mostra o nasconde la HUD degli avvisi; nascondendo toglie l'avviso in corso e svuota le code.
        /// </summary>
        /// <param name="visible">true per consentire gli avvisi.</param>
        public void SetVisible(bool visible)
        {
            hudVisible = visible;
            if (visible) return;
            ClearAll();
        }

        /// <summary>
        /// Toglie subito l'avviso in corso e svuota le code (senza copiarli sul polso).
        /// </summary>
        public void ClearAll()
        {
            eventQueue.Clear();
            feedbackQueue.Clear();
            hasCurrent = false;
            if (eventView != null) eventView.SetShown(false);
            if (feedbackView != null) feedbackView.SetShown(false);
        }

        /// <summary>
        /// Richiede un avviso evento (priorità alta).
        /// </summary>
        /// <param name="text">Testo: prima riga titolo, seconda riga dettaglio.</param>
        /// <param name="iconName">Nome icona ICN_ (vuoto = predefinita).</param>
        public void ShowEvent(string text, string iconName)
        {
            if (!hudVisible || string.IsNullOrEmpty(text)) return;
            SplitText(text, out string title, out string body);
            Enqueue(new Pending { Priority = AlertPriority.Event, Title = title, Body = body, IconName = iconName });
        }

        /// <summary>
        /// Richiede un feedback d'azione (priorità bassa), se il livello lo prevede.
        /// </summary>
        /// <param name="text">Testo del feedback.</param>
        /// <param name="kind">Tipo.</param>
        public void ShowFeedback(string text, FeedbackKind kind)
        {
            if (!hudVisible || string.IsNullOrEmpty(text)) return;
            if (level == DifficultyLevel.Crisis) return;
            if (level == DifficultyLevel.Operational && kind != FeedbackKind.Negative) return;
            Enqueue(new Pending { Priority = AlertPriority.Feedback, Title = text, Body = string.Empty, Kind = kind });
        }

        /// <summary>
        /// Applica le regole di coda a un nuovo avviso.
        /// </summary>
        /// <param name="item">Avviso richiesto.</param>
        private void Enqueue(Pending item)
        {
            float now = Time.unscaledTime;
            if (item.Priority == AlertPriority.Event)
            {
                if (hasCurrent && current.Priority == AlertPriority.Feedback)
                {
                    Finish(true);
                    Begin(item);
                    return;
                }
                if (!hasCurrent) { Begin(item); return; }
                AddToQueue(eventQueue, item, MAX_EVENT_QUEUE);
                return;
            }

            if (!hasCurrent) { Begin(item); return; }
            if (current.Priority == AlertPriority.Feedback && now - currentStart >= MIN_FEEDBACK_SECONDS && eventQueue.Count == 0)
            {
                Finish(true);
                Begin(item);
                return;
            }
            AddToQueue(feedbackQueue, item, MAX_FEEDBACK_QUEUE);
        }

        /// <summary>
        /// Aggiunge in coda; se è piena scarta il più vecchio.
        /// </summary>
        /// <param name="queue">Coda.</param>
        /// <param name="item">Avviso.</param>
        /// <param name="max">Capienza.</param>
        private static void AddToQueue(List<Pending> queue, Pending item, int max)
        {
            if (queue.Count >= max) queue.RemoveAt(0);
            queue.Add(item);
        }

        /// <summary>
        /// Mostra un avviso: posizione (una volta), contenuto, dissolvenza, suono, aptica.
        /// </summary>
        /// <param name="item">Avviso.</param>
        private void Begin(Pending item)
        {
            current = item;
            hasCurrent = true;
            currentStart = Time.unscaledTime;
            currentEnd = currentStart + theme.Hud.AlertDuration;
            bool isEvent = item.Priority == AlertPriority.Event;
            AlertView view = isEvent ? eventView : feedbackView;
            if (view == null) { hasCurrent = false; return; }

            Sprite sprite = icons != null ? icons.Find(item.IconName) : null;
            if (isEvent) view.ShowEvent(item.Title, item.Body, sprite, eventMode);
            else view.ShowFeedback(item.Title, item.Kind);
            HUDPlacement.PlaceAtGaze(view.CanvasTransform, head, distance, theme.Hud.AlertPitch, theme.Hud.MaxPitch);
            view.SetShown(true);
            FadeTo(view, 1f, true);

            HapticsService haptics = HapticsService.Instance;
            if (isEvent)
            {
                UIAudioFeedback.Play(UIAudioFeedback.SFX_HUD_ALERT);
                if (haptics != null) haptics.PulseBoth(haptics.EventAlert);
                if (eventMode == EventAlertMode.IconOnly) IconOnlyEventShown?.Invoke(item.Title);
            }
            else
            {
                UIAudioFeedback.Play(item.Kind == FeedbackKind.Negative ? UIAudioFeedback.SFX_HUD_FEEDBACK_NEGATIVE : UIAudioFeedback.SFX_HUD_FEEDBACK_POSITIVE);
                if (haptics != null) haptics.PulseNonDominant(haptics.ActionFeedback);
            }
        }

        /// <summary>
        /// Chiude l'avviso in corso: lo copia sul polso e avvia la dissolvenza d'uscita.
        /// </summary>
        /// <param name="immediate">true = spegne subito (sostituzione), false = dissolvenza.</param>
        private void Finish(bool immediate)
        {
            if (!hasCurrent) return;
            hasCurrent = false;
            bool isEvent = current.Priority == AlertPriority.Event;
            AlertView view = isEvent ? eventView : feedbackView;
            Sprite sprite = null;
            AlertFinished?.Invoke(current.Title, sprite);
            if (view == null) return;
            if (immediate) view.SetShown(false);
            else FadeTo(view, 0f, false);
        }

        /// <summary>
        /// Dissolvenza di una vista; in uscita spegne il Canvas alla fine.
        /// </summary>
        /// <param name="view">Vista.</param>
        /// <param name="alpha">Alpha finale.</param>
        /// <param name="fromZero">true = parte da 0 (ingresso).</param>
        private void FadeTo(AlertView view, float alpha, bool fromZero)
        {
            if (fromZero) view.Group.alpha = 0f;
            bool isEvent = view == eventView;
            float duration = theme.Motion.FadeDuration;
            System.Collections.IEnumerator routine = UITween.Fade(view.Group, alpha, duration, alpha <= 0f ? (Action)(() => { if (!hasCurrent || (current.Priority == AlertPriority.Event) != isEvent) view.SetShown(false); }) : null);
            Coroutine started = UITween.Play(this, isEvent ? eventFade : feedbackFade, routine);
            if (isEvent) eventFade = started; else feedbackFade = started;
            if (started == null)
            {
                view.Group.alpha = alpha;
                if (alpha <= 0f) view.SetShown(false);
            }
        }

        /// <summary>
        /// Fa scadere l'avviso in corso e passa al successivo (prima la coda Event, poi la coda Feedback).
        /// </summary>
        private void Update()
        {
            if (!hasCurrent) return;
            if (Time.unscaledTime < currentEnd) return;
            Finish(false);
            if (eventQueue.Count > 0) { Pending next = eventQueue[0]; eventQueue.RemoveAt(0); Begin(next); }
            else if (feedbackQueue.Count > 0) { Pending next = feedbackQueue[0]; feedbackQueue.RemoveAt(0); Begin(next); }
        }

        /// <summary>
        /// Divide un testo in titolo (prima riga) e dettaglio (resto).
        /// </summary>
        /// <param name="text">Testo.</param>
        /// <param name="title">Titolo.</param>
        /// <param name="body">Dettaglio (può essere vuoto).</param>
        private static void SplitText(string text, out string title, out string body)
        {
            int index = text.IndexOf('\n');
            if (index < 0) { title = text; body = string.Empty; return; }
            title = text.Substring(0, index).Trim();
            body = text.Substring(index + 1).Trim();
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, AlertView newEventView, AlertView newFeedbackView, float newDistance)
        {
            theme = newTheme;
            eventView = newEventView;
            feedbackView = newFeedbackView;
            distance = newDistance;
        }
    }
}
