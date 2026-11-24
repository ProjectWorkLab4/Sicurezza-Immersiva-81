using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// HUD_OutcomePanel: esito della missione (fase Resolution). Card bianca opaca a 1,5 m al centro, piazzata una volta
    /// all'altezza degli occhi, con LBL_Outcome Large a colore pieno (solo su card bianca, CLAUDE.md §4.9) e una riga col
    /// motivo. Resta 3–5 secondi (token duration.outcome, nessun input), con un jingle per esito, poi sfuma e chiama
    /// <c>onDone</c> e l'evento <see cref="Completed"/> (MissionFlow passa al Debrief).
    /// Uso: <c>outcomePanel.Show(result, () =&gt; ...);</c> Il motivo è, nell'ordine, quello passato, il criterio di
    /// completamento dello scenario per quell'esito, la prima riga del debrief.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("SI81/UI/Outcome Panel")]
    public class OutcomePanel : MonoBehaviour
    {
        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Testa del giocatore (se vuota: quella di MNG_UI o Camera.main).")]
        [SerializeField] private Transform head;
        [Tooltip("Jingle per esito, nell'ordine Positive, Partial, Negative (asset SFX_, vuoti = nessun suono).")]
        [SerializeField] private AudioClip[] jingles = new AudioClip[3];

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private CanvasGroup group;
        [Tooltip("Istanza di LBL_Outcome Large.")]
        [SerializeField] private OutcomeLabel outcomeLabel;
        [Tooltip("Motivo dell'esito, una riga (Subtitle Regular, nero, centrato).")]
        [SerializeField] private TMP_Text reasonText;

        // Sequenza in corso (null se chiuso).
        private Coroutine sequence;
        // Dissolvenza in corso.
        private Coroutine fadeRoutine;

        /// <summary>Evento: il pannello ha finito di mostrarsi ed è sparito.</summary>
        public event Action Completed;

        /// <summary>true se il pannello è aperto.</summary>
        public bool IsShowing => sequence != null;

        /// <summary>
        /// Prepara il gruppo di dissolvenza. Il prefab è salvato disattivo: si apre solo con <see cref="Show"/>.
        /// </summary>
        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Indica la testa del giocatore (dal rig condiviso).
        /// </summary>
        /// <param name="newHead">Transform della camera del rig.</param>
        public void SetRig(Transform newHead)
        {
            head = newHead;
        }

        /// <summary>
        /// Mostra l'esito per 3–5 secondi, poi sfuma e richiama <paramref name="onDone"/> e <see cref="Completed"/>.
        /// Se è già aperto riparte da capo.
        /// </summary>
        /// <param name="result">Risultato della missione (esito e righe di debrief).</param>
        /// <param name="onDone">Chiamata a fine (può essere null).</param>
        /// <param name="reason">Motivo in una riga; null = criterio dello scenario per quell'esito, poi prima riga del debrief.</param>
        public void Show(MissionResult result, Action onDone, string reason = null)
        {
            if (theme == null || result == null)
            {
                onDone?.Invoke();
                Completed?.Invoke();
                return;
            }
            if (sequence != null) StopCoroutine(sequence);
            gameObject.SetActive(true);

            outcomeLabel.SetOutcome(result.Outcome);
            string text = reason ?? ResolveReason(result);
            if (reasonText.text != text) reasonText.text = text;
            LayoutContent();

            Transform headTransform = WorldPlacement.ResolveHead(head, "OutcomePanel");
            WorldPlacement.PlaceInFront(transform, headTransform, UITheme.OUTCOME_DISTANCE_M, 0f, theme.Hud.MaxPitch);
            PlayJingle(result.Outcome);

            group.blocksRaycasts = false;
            group.alpha = 0f;
            sequence = StartCoroutine(RunSequence(onDone));
        }

        /// <summary>
        /// Chiude subito il pannello senza richiamare nulla (es. si esce alla Home durante l'esito).
        /// </summary>
        public void Hide()
        {
            if (sequence != null) StopCoroutine(sequence);
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            sequence = null;
            fadeRoutine = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sequenza: dissolvenza in entrata, attesa (tempo non scalato), dissolvenza in uscita, richiami.
        /// </summary>
        private IEnumerator RunSequence(Action onDone)
        {
            float fade = theme.Motion.FadeDuration;
            float seconds = Mathf.Clamp(theme.GetNumber(WorldTokens.OUTCOME_DURATION),
                theme.GetNumber(WorldTokens.OUTCOME_DURATION_MIN), theme.GetNumber(WorldTokens.OUTCOME_DURATION_MAX));
            yield return UITween.Fade(group, 1f, fade);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds - fade * 2f));
            yield return UITween.Fade(group, 0f, fade);
            sequence = null;
            gameObject.SetActive(false);
            onDone?.Invoke();
            Completed?.Invoke();
        }

        /// <summary>
        /// Motivo dell'esito: criterio di completamento dello scenario corrente, altrimenti prima riga del debrief (senza prefisso).
        /// </summary>
        private static string ResolveReason(MissionResult result)
        {
            string text = MissionSession.Current != null ? MissionSession.Current.GetCriterionText(result.Outcome) : string.Empty;
            if (!string.IsNullOrEmpty(text)) return text;
            if (result.DebriefLines != null && result.DebriefLines.Count > 0)
            {
                text = result.DebriefLines[0] ?? string.Empty;
                if (text.Length > 2 && text[1] == ' ' && (text[0] == '+' || text[0] == '-' || text[0] == '!')) text = text.Substring(2);
                return text;
            }
            return string.Empty;
        }

        /// <summary>
        /// Suona il jingle dell'esito nel punto del pannello (nessun suono se manca la clip).
        /// </summary>
        private void PlayJingle(OutcomeType outcome)
        {
            int index = (int)outcome;
            if (jingles == null || index >= jingles.Length || jingles[index] == null) return;
            AudioSource.PlayClipAtPoint(jingles[index], transform.position);
        }

        /// <summary>
        /// Dispone pillola e motivo (padding e spazi dal tema) e adatta l'altezza della card al testo, mai sotto il minimo.
        /// </summary>
        private void LayoutContent()
        {
            float width = theme.GetNumber(WorldTokens.OUTCOME_WIDTH);
            float pad = theme.GetNumber(WorldTokens.OUTCOME_PADDING);
            float gap = theme.GetNumber(WorldTokens.OUTCOME_GAP);
            float minHeight = theme.GetNumber(WorldTokens.OUTCOME_MIN_HEIGHT);
            float textWidth = width - pad * 2f;

            var pillRect = (RectTransform)outcomeLabel.transform;
            float pillHeight = pillRect.rect.height;
            pillRect.anchoredPosition = new Vector2(0f, -pad);

            var reasonRect = (RectTransform)reasonText.transform;
            float textHeight = Mathf.Ceil(reasonText.GetPreferredValues(reasonText.text, textWidth, float.PositiveInfinity).y);
            UIComponentUtil.SetSize(reasonRect, textWidth, textHeight);
            reasonRect.anchoredPosition = new Vector2(0f, -(pad + pillHeight + gap));

            float height = Mathf.Max(minHeight, pad + pillHeight + gap + textHeight + pad);
            UIComponentUtil.SetSize((RectTransform)transform, width, height);
        }

        /// <summary>
        /// Collega tema e riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, CanvasGroup newGroup, OutcomeLabel newOutcomeLabel, TMP_Text newReasonText)
        {
            theme = newTheme;
            group = newGroup;
            outcomeLabel = newOutcomeLabel;
            reasonText = newReasonText;
        }
    }
}
