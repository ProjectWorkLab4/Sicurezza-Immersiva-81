using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// HUD_DecisionPanel: pannello opaco della decisione (fase Decision), world-locked a 1,2 m davanti alla testa. Viene piazzato
    /// UNA volta all'apertura, all'altezza degli occhi (mai oltre +10°), e poi resta fermo nel mondo (nessun head-lock).
    /// Mostra una domanda e 2–4 opzioni (istanze di BTN_Secondary, testi dati dallo scenario) e, se richiesto, una barra del
    /// tempo (Crisis). Si usa con il raggio del controller (UIWorldRaycaster sul suo Canvas). La scelta dà un impulso aptico.
    /// Uso: <c>decisionPanel.Show("Cosa dici al collega?", new[] { "Usciamo subito", "Prendi l'estintore" }, i => ..., 10f);</c>
    /// Il testo delle opzioni va tenuto sotto circa 34 caratteri (una riga). Allo scadere del tempo si richiama con indice -1.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("SI81/UI/Decision Panel")]
    public class DecisionPanel : MonoBehaviour
    {
        /// <summary>Indice passato a <c>onChosen</c> quando il tempo scade senza una scelta.</summary>
        public const int TIMEOUT_INDEX = -1;

        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Testa del giocatore (se vuota: quella di MNG_UI o Camera.main).")]
        [SerializeField] private Transform head;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private CanvasGroup group;
        [Tooltip("Riga della domanda: icona + testo.")]
        [SerializeField] private RectTransform questionRow;
        [SerializeField] private TMP_Text questionText;
        [Tooltip("Icona della domanda a sinistra del testo.")]
        [SerializeField] private RectTransform questionIcon;
        [Tooltip("Pulsanti delle opzioni (2–4), dal primo all'ultimo.")]
        [SerializeField] private UIButton[] optionButtons = new UIButton[0];
        [Tooltip("Binario della barra del tempo (visibile solo se c'è un tempo limite).")]
        [SerializeField] private RectTransform timerTrack;
        [Tooltip("Riempimento della barra del tempo (si accorcia da destra).")]
        [SerializeField] private RectTransform timerFill;

        // Azione da chiamare alla scelta (null = pannello chiuso).
        private Action<int> chosenCallback;
        // Tempo limite e istante di apertura (tempo non scalato).
        private float timeout;
        private float openedAt;
        // Aggiornamento della barra a intervalli (non a ogni frame).
        private float timerTick;
        private float sinceTimerTick;
        // true da Show() fino alla scelta o alla chiusura.
        private bool isOpen;
        private Coroutine fadeRoutine;
        private bool wired;

        /// <summary>true se il pannello è aperto e aspetta una scelta.</summary>
        public bool IsOpen => isOpen;

        /// <summary>
        /// Collega i pulsanti (una volta). Il prefab è salvato disattivo: si apre solo con <see cref="Show"/>.
        /// </summary>
        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            WireButtons();
        }

        /// <summary>
        /// Collega il clic di ogni pulsante al proprio indice (una sola volta: le lambda si creano qui, non a ogni apertura).
        /// </summary>
        private void WireButtons()
        {
            if (wired) return;
            wired = true;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null) continue;
                int index = i;
                optionButtons[i].Clicked += () => Choose(index);
            }
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
        /// Apre il pannello: lo piazza davanti alla testa a 1,2 m, scrive domanda e opzioni, mostra (se serve) la barra del tempo.
        /// Se era già aperto lo sostituisce (la scelta precedente non viene richiamata).
        /// </summary>
        /// <param name="question">Domanda (dato dello scenario, in italiano).</param>
        /// <param name="options">Testi delle opzioni: da 2 a 4 (le eccedenti sono ignorate).</param>
        /// <param name="onChosen">Chiamata con l'indice scelto (0-based), o <see cref="TIMEOUT_INDEX"/> se il tempo scade.</param>
        /// <param name="timeoutSeconds">0 = nessun limite; &gt; 0 = barra del tempo, allo scadere si chiude e si richiama con -1.</param>
        /// <param name="optionIcons">Icone ICN_ opzionali, parallele alle opzioni (può essere null).</param>
        public void Show(string question, string[] options, Action<int> onChosen, float timeoutSeconds = 0f, Sprite[] optionIcons = null)
        {
            if (options == null || options.Length == 0 || theme == null) return;
            WireButtons();
            gameObject.SetActive(true);

            chosenCallback = onChosen;
            timeout = Mathf.Max(0f, timeoutSeconds);
            openedAt = Time.unscaledTime;
            sinceTimerTick = 0f;
            timerTick = Mathf.Max(0.02f, theme.GetNumber(WorldTokens.DECISION_TIMER_TICK));
            isOpen = true;

            if (questionText != null && questionText.text != question) questionText.text = question ?? string.Empty;
            int count = Mathf.Min(options.Length, optionButtons.Length);
            LayoutContent(count, timeout > 0f);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                UIButton button = optionButtons[i];
                if (button == null) continue;
                bool used = i < count;
                if (button.gameObject.activeSelf != used) button.gameObject.SetActive(used);
                if (!used) continue;
                button.SetLabelText(options[i]);
                button.SetIcon(optionIcons != null && i < optionIcons.Length ? optionIcons[i] : null);
            }
            SetTimerFill(1f);

            // Piazzato una volta, all'altezza degli occhi (angolo 0, mai oltre il massimo della HUD).
            Transform headTransform = WorldPlacement.ResolveHead(head, "DecisionPanel");
            WorldPlacement.PlaceInFront(transform, headTransform, UITheme.DECISION_DISTANCE_M, 0f, theme.Hud.MaxPitch);

            group.blocksRaycasts = true;
            group.interactable = true;
            group.alpha = 0f;
            fadeRoutine = UITween.Play(this, fadeRoutine, UITween.Fade(group, 1f, theme.Motion.FadeDuration, () => fadeRoutine = null));
        }

        /// <summary>
        /// Chiude il pannello senza richiamare la scelta (es. lo scenario cambia fase).
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            chosenCallback = null;
            FadeOut();
        }

        /// <summary>
        /// Controlla il tempo limite a intervalli (non a ogni frame): aggiorna la barra e, scaduto, chiude con -1.
        /// </summary>
        private void Update()
        {
            if (!isOpen || timeout <= 0f) return;
            sinceTimerTick += Time.unscaledDeltaTime;
            if (sinceTimerTick < timerTick) return;
            sinceTimerTick = 0f;
            float remaining = 1f - (Time.unscaledTime - openedAt) / timeout;
            if (remaining <= 0f)
            {
                Choose(TIMEOUT_INDEX);
                return;
            }
            SetTimerFill(remaining);
        }

        /// <summary>
        /// Registra la scelta: impulso aptico (solo per una scelta vera), chiusura e richiamo.
        /// </summary>
        /// <param name="index">Indice dell'opzione, o -1 per tempo scaduto.</param>
        private void Choose(int index)
        {
            if (!isOpen) return;
            isOpen = false;
            Action<int> callback = chosenCallback;
            chosenCallback = null;
            group.blocksRaycasts = false;
            if (index >= 0 && theme != null)
                HUDService.Pulse(theme.GetNumber(WorldTokens.DECISION_HAPTIC_AMPLITUDE), theme.GetNumber(WorldTokens.DECISION_HAPTIC_DURATION), false);
            FadeOut();
            callback?.Invoke(index);
        }

        /// <summary>
        /// Dissolvenza in uscita e disattivazione.
        /// </summary>
        private void FadeOut()
        {
            group.blocksRaycasts = false;
            if (!isActiveAndEnabled)
            {
                gameObject.SetActive(false);
                return;
            }
            fadeRoutine = UITween.Play(this, fadeRoutine, UITween.Fade(group, 0f, theme.Motion.FadeDuration, () =>
            {
                fadeRoutine = null;
                if (!isOpen) gameObject.SetActive(false);
            }));
        }

        /// <summary>
        /// Imposta la lunghezza della barra del tempo (1 = piena). Scrive solo se cambia.
        /// </summary>
        private void SetTimerFill(float fraction)
        {
            if (timerFill == null || timerTrack == null) return;
            bool visible = timeout > 0f;
            if (timerTrack.gameObject.activeSelf != visible) timerTrack.gameObject.SetActive(visible);
            Vector2 max = timerFill.anchorMax;
            float clamped = Mathf.Clamp01(fraction);
            if (Mathf.Abs(max.x - clamped) < 0.0005f) return;
            max.x = clamped;
            timerFill.anchorMax = max;
        }

        /// <summary>
        /// Dispone domanda, opzioni e barra dall'alto in basso e adatta l'altezza dello sfondo (una volta per apertura, senza Layout Group).
        /// </summary>
        /// <param name="optionCount">Numero di opzioni mostrate.</param>
        /// <param name="withTimer">true se c'è la barra del tempo.</param>
        private void LayoutContent(int optionCount, bool withTimer)
        {
            float width = theme.GetNumber(WorldTokens.DECISION_WIDTH);
            float padX = theme.GetNumber(WorldTokens.DECISION_PADDING_X);
            float padY = theme.GetNumber(WorldTokens.DECISION_PADDING_Y);
            float gap = theme.GetNumber(WorldTokens.DECISION_GAP);
            float icon = theme.GetNumber(WorldTokens.DECISION_ICON);
            float buttonHeight = theme.GetNumber(ButtonTokens.HEIGHT);
            float timerHeight = theme.GetNumber(WorldTokens.DECISION_TIMER_HEIGHT);

            // Domanda: il testo va a capo accanto all'icona; altezza = quella del testo (almeno l'icona).
            float textWidth = width - padX * 2f - icon - theme.GetNumber(NumberTokens.SPACE_S);
            var textRect = (RectTransform)questionText.transform;
            UIComponentUtil.SetSize(textRect, textWidth, textRect.rect.height);
            float textHeight = questionText.GetPreferredValues(questionText.text, textWidth, float.PositiveInfinity).y;
            float rowHeight = Mathf.Max(icon, Mathf.Ceil(textHeight));
            UIComponentUtil.SetSize(textRect, textWidth, rowHeight);
            UIComponentUtil.SetSize(questionRow, width - padX * 2f, rowHeight);
            questionRow.anchoredPosition = new Vector2(0f, -padY);
            if (questionIcon != null) questionIcon.anchoredPosition = new Vector2(0f, 0f);

            float y = padY + rowHeight + gap;
            for (int i = 0; i < optionCount; i++)
            {
                var buttonRect = (RectTransform)optionButtons[i].transform;
                buttonRect.anchoredPosition = new Vector2(0f, -y);
                y += buttonHeight + gap;
            }
            if (withTimer && timerTrack != null)
            {
                timerTrack.anchoredPosition = new Vector2(0f, -y);
                UIComponentUtil.SetSize(timerTrack, width - padX * 2f, timerHeight);
                y += timerHeight + gap;
            }
            float height = y - gap + padY;

            var root = (RectTransform)transform;
            UIComponentUtil.SetSize(root, width, height);
        }

        /// <summary>
        /// Collega tema e riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, CanvasGroup newGroup, RectTransform newQuestionRow, TMP_Text newQuestionText,
            RectTransform newQuestionIcon, UIButton[] newButtons, RectTransform newTimerTrack, RectTransform newTimerFill)
        {
            theme = newTheme;
            group = newGroup;
            questionRow = newQuestionRow;
            questionText = newQuestionText;
            questionIcon = newQuestionIcon;
            optionButtons = newButtons;
            timerTrack = newTimerTrack;
            timerFill = newTimerFill;
        }
    }
}
