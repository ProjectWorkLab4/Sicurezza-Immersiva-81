using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// HUD_WristPanel: contenuto del pannello da polso (sola lettura). Righe: obiettivo (LBL_ObjectiveItem, ~30 caratteri per
    /// riga) + contatore, timer mm:ss con avviso (sfondo deny, testo nero) negli ultimi secondi, fase (pillola), LBL_LevelBadge,
    /// suggerimento (solo Guided), ultimo avviso. Il layout si ricalcola SOLO quando un contenuto cambia (nessun Layout Group,
    /// nessun lavoro per fotogramma oltre al controllo "il pannello è rivolto agli occhi?"). Dissolvenza 30% → 100% quando il
    /// pannello guarda gli occhi entro ~40 cm (soglie angolari con isteresi). Opaco (CLAUDE.md §4.5). Non interattivo: nessun raggio.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Wrist Panel")]
    public class WristPanel : MonoBehaviour
    {
        /// <summary>Margine interno del pannello.</summary>
        private const float PADDING = 8f;
        /// <summary>Spazio verticale tra le righe.</summary>
        private const float ROW_GAP = 5f;
        /// <summary>Altezza della riga timer / fase / contatore.</summary>
        private const float TIMER_ROW_HEIGHT = 32f;
        /// <summary>Lato delle icone delle righe.</summary>
        private const float ICON_SIZE = 20f;
        /// <summary>Altezza della pillola della fase.</summary>
        private const float PHASE_HEIGHT = 24f;
        /// <summary>Padding orizzontale della pillola e del riquadro timer.</summary>
        private const float PILL_PADDING = 10f;
        /// <summary>Id del Blackout: i suoi obiettivi sono provvisori e restano solo in Guided.</summary>
        const string ID_BLACKOUT = "02";

        [Header("Dati (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private Transform canvasTransform;
        [SerializeField] private CanvasGroup group;
        [Header("Sfondo")]
        [SerializeField] private RoundedPanelGraphic background;
        [Header("Obiettivo e contatore")]
        [SerializeField] private ObjectiveItem objectiveItem;
        [SerializeField] private TMP_Text counterText;
        [Header("Timer")]
        [SerializeField] private RoundedPanelGraphic timerChip;
        [SerializeField] private Image timerIcon;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Sprite timerSprite;
        [SerializeField] private Sprite warningSprite;
        [Header("Fase e livello")]
        [SerializeField] private RoundedPanelGraphic phasePill;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private LevelBadge levelBadge;
        [Header("Suggerimento e ultimo avviso")]
        [SerializeField] private RectTransform hintRow;
        [SerializeField] private Image hintIcon;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private RectTransform alertRow;
        [SerializeField] private Image alertIcon;
        [SerializeField] private TMP_Text alertText;
        [Header("Dissolvenza quando rivolto agli occhi (valori provvisori, da tarare)")]
        [Tooltip("Angolo massimo tra la normale del pannello e la direzione verso gli occhi per considerarlo rivolto, in gradi.")]
        [SerializeField] private float facingAngle = 50f;
        [Tooltip("Angolo massimo tra lo sguardo e il pannello per considerarlo guardato, in gradi.")]
        [SerializeField] private float gazeAngle = 45f;
        [Tooltip("Margine in gradi aggiunto alle soglie per USCIRE dallo stato (isteresi, evita lo sfarfallio).")]
        [SerializeField] private float hysteresisAngle = 10f;
        [Tooltip("Frequenza dell'impulso del timer in avviso, in Hz (massimo 1, CLAUDE.md §4.11).")]
        [SerializeField] private float warningPulseHz = 0.8f;

        // Stato mostrato.
        private string objectiveText = string.Empty;
        private int objectiveCurrent;
        private int objectiveTotal;
        private string phaseKey;
        private string hint = string.Empty;
        private string lastAlert = string.Empty;
        private int shownSeconds = -1;
        private bool timerWarning;
        private bool pulseWarning = true;
        // Regole di livello.
        private bool showObjective = true;
        private bool showHints;
        private bool showCounter = true;
        private DifficultyLevel level = DifficultyLevel.Guided;
        // Larghezze misurate una volta in ApplyTheme.
        private float chipWidth;
        private float phaseWidth;
        private float counterWidth;
        // Testa e dissolvenza.
        private Transform head;
        private bool focused;
        private float focusDistance;
        private float idleAlpha = 1f;
        private float fadeDuration;
        private Coroutine fadeRoutine;
        private bool themeApplied;

        /// <summary>Imposta la testa per il calcolo "rivolto agli occhi".</summary>
        /// <param name="newHead">Testa (camera) del giocatore.</param>
        public void SetHead(Transform newHead)
        {
            head = newHead;
        }

        /// <summary>
        /// Applica tema e stato iniziale.
        /// </summary>
        private void Awake()
        {
            ApplyTheme();
            Relayout();
        }

        /// <summary>
        /// Ricalcola colori e misure fisse dal tema (una volta) e porta il pannello alla dissolvenza di riposo.
        /// </summary>
        private void ApplyTheme()
        {
            if (theme == null || themeApplied) return;
            themeApplied = true;
            UITheme.HudSettings hud = theme.Hud;
            idleAlpha = hud.WristIdleAlpha;
            focusDistance = hud.WristFocusDistance;
            fadeDuration = theme.Motion.FadeDuration;
            if (group != null) group.alpha = idleAlpha;

            LabelStyling.SetColor(background, theme.SurfacePanel);
            LabelStyling.StyleText(timerText, theme, TextStyleIds.SUBTITLE_BOLD, theme.TextOnDark);
            LabelStyling.StyleText(counterText, theme, TextStyleIds.BODY_BOLD, theme.TextOnDark);
            LabelStyling.StyleText(phaseText, theme, TextStyleIds.BODY_BOLD, theme.TextPrimary);
            LabelStyling.StyleText(hintText, theme, TextStyleIds.BODY, theme.TextOnDark);
            LabelStyling.StyleText(alertText, theme, TextStyleIds.BODY, theme.TextOnGreen);
            LabelStyling.SetColor(phasePill, theme.StateActive);
            LabelStyling.SetColor(hintIcon, theme.BrandLight);
            LabelStyling.SetColor(alertIcon, theme.TextOnGreen);
            if (levelBadge != null) levelBadge.SetSurface(LabelSurface.Dark);

            // Larghezze fisse: il timer cambia ogni secondo ma "00:00" ha sempre la stessa larghezza.
            chipWidth = PILL_PADDING + ICON_SIZE + 6f + Mathf.Ceil(MeasureWidth(timerText, "00:00")) + PILL_PADDING;
            counterWidth = Mathf.Ceil(MeasureWidth(counterText, "00/00"));
            if (hintText != null) { hintText.textWrappingMode = TextWrappingModes.Normal; hintText.overflowMode = TextOverflowModes.Ellipsis; }
            if (alertText != null) { alertText.textWrappingMode = TextWrappingModes.NoWrap; alertText.overflowMode = TextOverflowModes.Ellipsis; }
            if (timerIcon != null && timerSprite != null) timerIcon.sprite = timerSprite;
            enabled = true;
        }

        /// <summary>
        /// Larghezza preferita di un testo TMP già stilizzato.
        /// </summary>
        /// <param name="text">Testo TMP.</param>
        /// <param name="sample">Testo di prova.</param>
        /// <returns>Larghezza in unità Canvas (0 se manca il TMP).</returns>
        private static float MeasureWidth(TMP_Text text, string sample)
        {
            return text != null ? text.GetPreferredValues(sample).x : 0f;
        }

        // =====================================================================
        // API
        // =====================================================================

        /// <summary>
        /// Imposta le regole del livello: obiettivi sul polso, suggerimenti, contatore (non in Crisis), badge del livello.
        /// Per Blackout gli obiettivi sono provvisori: solo in Guided, negli altri livelli spariscono riga e contatore.
        /// </summary>
        /// <param name="cfg">Configurazione del livello (null = tutto visibile tranne i suggerimenti).</param>
        public void ApplyLevel(LevelConfig cfg)
        {
            ApplyTheme();
            level = cfg != null ? cfg.Level : DifficultyLevel.Guided;
            bool nascondiBlackout = MissionSession.Current != null && MissionSession.Current.Id == ID_BLACKOUT && level != DifficultyLevel.Guided;
            showObjective = (cfg == null || cfg.ShowObjectivesOnWrist) && !nascondiBlackout;
            showHints = cfg != null && cfg.ShowHints;
            showCounter = level != DifficultyLevel.Crisis;
            if (levelBadge != null) levelBadge.SetLevel(level);
            Relayout();
        }

        /// <summary>
        /// Imposta il testo dell'obiettivo corrente.
        /// </summary>
        /// <param name="text">Testo (dati della missione).</param>
        public void SetObjective(string text)
        {
            ApplyTheme();
            objectiveText = text ?? string.Empty;
            if (objectiveItem != null) objectiveItem.SetObjective(objectiveText, ObjectiveState.Current);
            Relayout();
        }

        /// <summary>
        /// Imposta il contatore "corrente/totale" (totale 0 = nascosto).
        /// </summary>
        /// <param name="current">Obiettivo corrente (da 1).</param>
        /// <param name="total">Totale.</param>
        public void SetProgress(int current, int total)
        {
            objectiveCurrent = current;
            objectiveTotal = total;
            Relayout();
        }

        /// <summary>
        /// Imposta la fase (chiave hud.phase.*).
        /// </summary>
        /// <param name="key">Chiave del testo.</param>
        public void SetPhase(string key)
        {
            phaseKey = key;
            Relayout();
        }

        /// <summary>
        /// Imposta il suggerimento (mostrato solo se il livello lo prevede).
        /// </summary>
        /// <param name="text">Testo (max ~60 caratteri); vuoto = nascosto.</param>
        public void SetHint(string text)
        {
            hint = text ?? string.Empty;
            Relayout();
        }

        /// <summary>
        /// Copia l'ultimo avviso mostrato sul polso.
        /// </summary>
        /// <param name="text">Testo dell'avviso.</param>
        /// <param name="icon">Icona dell'avviso (può essere null).</param>
        public void SetLastAlert(string text, Sprite icon)
        {
            lastAlert = text ?? string.Empty;
            if (alertIcon != null && icon != null) alertIcon.sprite = icon;
            Relayout();
        }

        /// <summary>
        /// Aggiorna il timer; riscrive il testo SOLO se cambia il secondo mostrato.
        /// </summary>
        /// <param name="seconds">Secondi da mostrare (rimasti o trascorsi).</param>
        /// <param name="warning">true per lo stato di avviso (sfondo deny, testo nero).</param>
        public void SetTimer(int seconds, bool warning)
        {
            ApplyTheme();
            if (seconds < 0) seconds = 0;
            if (seconds != shownSeconds && timerText != null && strings != null)
            {
                shownSeconds = seconds;
                timerText.text = strings.Format("hud.wrist.timer", seconds / 60, seconds % 60);
            }
            if (warning != timerWarning)
            {
                timerWarning = warning;
                ApplyTimerColors();
            }
        }

        /// <summary>
        /// Imposta se l'impulso del timer in avviso è consentito (falso con "riduci lampeggi").
        /// </summary>
        /// <param name="allowed">true per lasciar pulsare il riquadro.</param>
        public void SetPulseAllowed(bool allowed)
        {
            pulseWarning = allowed;
            if (!allowed && timerChip != null && timerWarning && theme != null) LabelStyling.SetColor(timerChip, theme.Deny);
        }

        /// <summary>
        /// Colora timer e riquadro per lo stato normale o di avviso (Figma: riquadro deny, icona e testo neri).
        /// </summary>
        private void ApplyTimerColors()
        {
            if (theme == null) return;
            Color content = timerWarning ? theme.TextPrimary : theme.TextOnDark;
            LabelStyling.SetColor(timerText, content);
            LabelStyling.SetColor(timerIcon, content);
            if (timerIcon != null)
            {
                Sprite wanted = timerWarning && warningSprite != null ? warningSprite : timerSprite;
                if (wanted != null && timerIcon.sprite != wanted) timerIcon.sprite = wanted;
            }
            if (timerChip != null)
            {
                timerChip.enabled = timerWarning;
                LabelStyling.SetColor(timerChip, theme.Deny);
            }
        }

        // =====================================================================
        // Layout (solo quando cambia un contenuto)
        // =====================================================================

        /// <summary>
        /// Ricalcola posizioni e visibilità delle righe dall'alto verso il basso. Se il contenuto supera l'altezza del Canvas il
        /// suggerimento passa a una sola riga.
        /// </summary>
        private void Relayout()
        {
            if (theme == null || background == null) return;
            RectTransform panel = (RectTransform)background.transform;
            float width = panel.rect.width;
            float height = panel.rect.height;
            float contentWidth = width - 2f * PADDING;
            float y = PADDING;

            // Obiettivo.
            bool hasObjective = showObjective && objectiveItem != null && !string.IsNullOrEmpty(objectiveText);
            if (objectiveItem != null)
            {
                PanelLayout.SetActive(objectiveItem.gameObject, hasObjective);
                if (hasObjective)
                {
                    RectTransform rect = (RectTransform)objectiveItem.transform;
                    LabelStyling.Place(rect, PADDING, y, contentWidth, rect.rect.height);
                    objectiveItem.Apply();
                    y += rect.rect.height + ROW_GAP;
                }
            }

            // Timer + fase + contatore (stessa riga).
            LabelStyling.Place(timerChip != null ? timerChip.rectTransform : null, PADDING, y, chipWidth, TIMER_ROW_HEIGHT);
            LabelStyling.Place(timerIcon != null ? timerIcon.rectTransform : null, PADDING + PILL_PADDING, y + (TIMER_ROW_HEIGHT - ICON_SIZE) * 0.5f, ICON_SIZE, ICON_SIZE);
            LabelStyling.Place(timerText != null ? timerText.rectTransform : null, PADDING + PILL_PADDING + ICON_SIZE + 6f, y, chipWidth - PILL_PADDING * 2f - ICON_SIZE - 6f, TIMER_ROW_HEIGHT);

            float x = PADDING + chipWidth + 8f;
            bool hasPhase = !string.IsNullOrEmpty(phaseKey) && strings != null && phaseText != null;
            if (phasePill != null) PanelLayout.SetActive(phasePill.gameObject, hasPhase);
            if (hasPhase)
            {
                string phaseLabel = strings.Get(phaseKey);
                if (phaseText.text != phaseLabel) phaseText.text = phaseLabel;
                phaseWidth = Mathf.Ceil(MeasureWidth(phaseText, phaseLabel)) + PILL_PADDING * 2f;
                float pillY = y + (TIMER_ROW_HEIGHT - PHASE_HEIGHT) * 0.5f;
                LabelStyling.Place(phasePill.rectTransform, x, pillY, phaseWidth, PHASE_HEIGHT);
                LabelStyling.Place(phaseText.rectTransform, x + PILL_PADDING, pillY, phaseWidth - PILL_PADDING * 2f, PHASE_HEIGHT);
            }

            bool hasCounter = showCounter && showObjective && objectiveTotal > 0 && strings != null && counterText != null;
            if (counterText != null) PanelLayout.SetActive(counterText.gameObject, hasCounter);
            if (hasCounter)
            {
                string counter = strings.Format("hud.wrist.objective_progress", objectiveCurrent, objectiveTotal);
                if (counterText.text != counter) counterText.text = counter;
                counterText.alignment = TextAlignmentOptions.MidlineRight;
                LabelStyling.Place(counterText.rectTransform, width - PADDING - counterWidth, y, counterWidth, TIMER_ROW_HEIGHT);
            }
            y += TIMER_ROW_HEIGHT + ROW_GAP;

            // Badge del livello.
            if (levelBadge != null)
            {
                RectTransform rect = (RectTransform)levelBadge.transform;
                LabelStyling.Place(rect, PADDING, y, rect.rect.width, rect.rect.height);
                y += rect.rect.height + ROW_GAP;
            }

            // Suggerimento (solo livelli con ShowHints) e ultimo avviso.
            float lineHeight = PanelLayout.LineHeight(theme.Body);
            bool hasHint = showHints && !string.IsNullOrEmpty(hint) && hintRow != null;
            bool hasAlert = !string.IsNullOrEmpty(lastAlert) && alertRow != null;
            float alertHeight = hasAlert ? lineHeight + 2f : 0f;
            if (hasHint)
            {
                float textWidth = contentWidth - ICON_SIZE - 8f;
                if (hintText.text != hint) hintText.text = hint;
                float preferred = Mathf.Ceil(hintText.GetPreferredValues(hint, textWidth, 0f).y);
                float available = height - PADDING - y - alertHeight - (hasAlert ? ROW_GAP : 0f);
                float hintHeight = Mathf.Min(preferred, 2f * lineHeight);
                if (hintHeight > available) hintHeight = lineHeight;
                hintText.maxVisibleLines = hintHeight > lineHeight + 1f ? 2 : 1;
                LabelStyling.Place(hintRow, PADDING, y, contentWidth, hintHeight);
                LabelStyling.Place(hintIcon.rectTransform, 0f, 1f, ICON_SIZE, ICON_SIZE);
                LabelStyling.Place(hintText.rectTransform, ICON_SIZE + 8f, 0f, textWidth, hintHeight);
                y += hintHeight + ROW_GAP;
            }
            if (hintRow != null) PanelLayout.SetActive(hintRow.gameObject, hasHint);
            if (hasAlert)
            {
                if (alertText.text != lastAlert) alertText.text = lastAlert;
                LabelStyling.Place(alertRow, PADDING, y, contentWidth, alertHeight);
                LabelStyling.Place(alertIcon.rectTransform, 0f, 1f, ICON_SIZE, ICON_SIZE);
                LabelStyling.Place(alertText.rectTransform, ICON_SIZE + 8f, 0f, contentWidth - ICON_SIZE - 8f, alertHeight);
            }
            if (alertRow != null) PanelLayout.SetActive(alertRow.gameObject, hasAlert);
        }

        // =====================================================================
        // Dissolvenza e impulso del timer
        // =====================================================================

        /// <summary>
        /// Controlla se il pannello è rivolto agli occhi (distanza e angoli, con isteresi) e avvia la dissolvenza 30% ↔ 100%;
        /// in stato di avviso fa pulsare il riquadro del timer (max 1 Hz, solo se consentito).
        /// </summary>
        private void Update()
        {
            if (head != null && canvasTransform != null && group != null)
            {
                Vector3 toHead = head.position - canvasTransform.position;
                float distance = toHead.magnitude;
                bool wantFocus = false;
                if (distance > 0.0001f && distance <= focusDistance)
                {
                    Vector3 direction = toHead / distance;
                    float extra = focused ? hysteresisAngle : 0f;
                    // Il fronte leggibile del Canvas è dal lato -Z: la normale verso lo spettatore è -forward.
                    float facing = Vector3.Angle(-canvasTransform.forward, direction);
                    float gaze = Vector3.Angle(head.forward, -direction);
                    wantFocus = facing <= facingAngle + extra && gaze <= gazeAngle + extra;
                }
                if (wantFocus != focused)
                {
                    focused = wantFocus;
                    float target = focused ? 1f : idleAlpha;
                    fadeRoutine = UITween.Play(this, fadeRoutine, UITween.Fade(group, target, fadeDuration, () => fadeRoutine = null));
                    if (fadeRoutine == null) group.alpha = target;
                }
            }

            if (timerWarning && pulseWarning && timerChip != null)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * warningPulseHz * 2f * Mathf.PI);
                Color c = theme.Deny;
                c.a = Mathf.Lerp(0.55f, 1f, k);
                timerChip.color = c;
            }
        }

        /// <summary>
        /// Imposta tutti i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, Transform newCanvas, CanvasGroup newGroup, RoundedPanelGraphic newBackground,
            ObjectiveItem newObjective, TMP_Text newCounter, RoundedPanelGraphic newChip, Image newTimerIcon, TMP_Text newTimerText,
            Sprite newTimerSprite, Sprite newWarningSprite, RoundedPanelGraphic newPhasePill, TMP_Text newPhaseText, LevelBadge newBadge,
            RectTransform newHintRow, Image newHintIcon, TMP_Text newHintText, RectTransform newAlertRow, Image newAlertIcon, TMP_Text newAlertText)
        {
            theme = newTheme; strings = newStrings; canvasTransform = newCanvas; group = newGroup; background = newBackground;
            objectiveItem = newObjective; counterText = newCounter; timerChip = newChip; timerIcon = newTimerIcon; timerText = newTimerText;
            timerSprite = newTimerSprite; warningSprite = newWarningSprite; phasePill = newPhasePill; phaseText = newPhaseText; levelBadge = newBadge;
            hintRow = newHintRow; hintIcon = newHintIcon; hintText = newHintText; alertRow = newAlertRow; alertIcon = newAlertIcon; alertText = newAlertText;
        }
    }
}
