using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Radice del prefab MNG_HUD (rig condiviso di SCN_Core): contiene HUD_WristPanel, HUD_EventAlert, HUD_ActionFeedback e
    /// HUD_Subtitles e realizza <see cref="HUDService"/>. Si registra in Awake. Riceve testa e controller dal rig con
    /// <see cref="SetRig"/> (se mancano usa UIScreenManager/Camera.main e i UIPointer di scena, con UN avviso). Ascolta MissionSession:
    /// all'inizio di una missione applica il livello (LevelConfig), obiettivi e timer di MissionDefinition; a fine missione ferma il timer
    /// e nasconde la HUD. Parte NASCOSTA: la mostra l'inizio di una missione o HUDService.SetVisible(true).
    /// Timer: conto alla rovescia (secondi &gt; 0) o tempo trascorso (0); si ferma con Time.timeScale = 0 (pausa).
    /// Avvisi del timer (haptic + suono) a 60 s e 30 s; aspetto di avviso (riquadro deny) dagli ultimi 30 s.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("SI81/UI/HUD Root")]
    public class HUDRoot : MonoBehaviour
    {
        /// <summary>Intervallo in secondi tra i tentativi di trovare testa e controller.</summary>
        private const float RIG_RETRY_SECONDS = 0.5f;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private WristPanel wrist;
        [SerializeField] private WristAttach wristAttach;
        [SerializeField] private AlertPresenter alerts;
        [SerializeField] private SubtitlePresenter subtitles;
        [SerializeField] private HUDIconLibrary icons;

        // Rig.
        private Transform head;
        private Transform leftController;
        private Transform rightController;
        private bool rigAssignedByCaller;
        private bool rigApplied;
        private float nextRigTry;
        // Stato HUD.
        private bool visible;
        private string lastObjective;
        private bool objectiveInitialized;
        // Timer.
        private bool timerRunning;
        private bool countdown;
        private float remaining;
        private float elapsed;
        private bool warnedFirst;
        private bool warnedSecond;
        private int lastShownSecond = -1;
        // Sessione.
        // Impostazioni.
        private IUISettingsStore boundStore;

        /// <summary>Libreria delle icone ICN_ per nome.</summary>
        public HUDIconLibrary Icons => icons;
        /// <summary>Secondi rimasti (solo conto alla rovescia).</summary>
        public float RemainingSeconds => countdown ? remaining : 0f;
        /// <summary>Secondi trascorsi dall'avvio del timer.</summary>
        public float ElapsedSeconds => elapsed;

        // =====================================================================
        // Ciclo di vita
        // =====================================================================

        /// <summary>
        /// Registra la HUD, collega gli avvisi al polso e ai sottotitoli, parte nascosta.
        /// </summary>
        private void Awake()
        {
            if (alerts != null)
            {
                alerts.AlertFinished += HandleAlertFinished;
                alerts.IconOnlyEventShown += HandleIconOnlyEvent;
            }
            if (wrist != null) wrist.SetPhase("hud.phase.scan");
            ApplyVisibility(false);
            HUDService.Register(this);
        }

        /// <summary>
        /// Ascolta missione e impostazioni.
        /// </summary>
        private void OnEnable()
        {
            MissionSession.Ended += HandleMissionEnded;
            MissionSession.Started += BeginFromSession;
            UIServices.Ready += BindSettings;
            BindSettings();
        }

        /// <summary>
        /// Smette di ascoltare.
        /// </summary>
        private void OnDisable()
        {
            MissionSession.Ended -= HandleMissionEnded;
            MissionSession.Started -= BeginFromSession;
            UIServices.Ready -= BindSettings;
            if (boundStore != null) boundStore.Changed -= HandleSettingChanged;
            boundStore = null;
        }

        /// <summary>
        /// Deregistra la HUD e distrugge il pannello da polso (che può essere figlio di un controller).
        /// </summary>
        private void OnDestroy()
        {
            HUDService.Unregister(this);
            if (alerts != null)
            {
                alerts.AlertFinished -= HandleAlertFinished;
                alerts.IconOnlyEventShown -= HandleIconOnlyEvent;
            }
            if (wristAttach != null && wristAttach.transform.parent != transform) Destroy(wristAttach.gameObject);
        }

        /// <summary>
        /// Ogni fotogramma: ritrova il rig se manca, fa avanzare il timer.
        /// </summary>
        private void Update()
        {
            if (!rigApplied && Time.unscaledTime >= nextRigTry) ResolveRig();
            TickTimer();
        }

        // =====================================================================
        // Rig
        // =====================================================================

        /// <summary>
        /// Assegna testa e controller del rig condiviso (da chiamare appena il rig è pronto).
        /// </summary>
        /// <param name="newHead">Testa (camera).</param>
        /// <param name="left">Controller sinistro (ancora della mano).</param>
        /// <param name="right">Controller destro.</param>
        public void SetRig(Transform newHead, Transform left, Transform right)
        {
            head = newHead;
            leftController = left;
            rightController = right;
            rigAssignedByCaller = true;
            DistributeRig();
        }

        /// <summary>
        /// Cerca testa e controller se non assegnati: testa da UIScreenManager/Camera.main, controller dai UIPointer in scena.
        /// </summary>
        private void ResolveRig()
        {
            nextRigTry = Time.unscaledTime + RIG_RETRY_SECONDS;
            if (rigAssignedByCaller) { DistributeRig(); return; }
            Transform assigned = UIScreenManager.Instance != null ? UIScreenManager.Instance.Head : null;
            head = UIRigLocator.ResolveHead(assigned, "HUDRoot");
            UIPointer[] pointers = FindObjectsByType<UIPointer>(FindObjectsSortMode.None);
            for (int i = 0; i < pointers.Length; i++)
            {
                if (pointers[i].Hand == UIPointerHand.Left) leftController = pointers[i].transform;
                else rightController = pointers[i].transform;
            }
            DistributeRig();
        }

        /// <summary>
        /// Passa testa e controller ai componenti che li usano.
        /// </summary>
        private void DistributeRig()
        {
            if (head != null)
            {
                if (wrist != null) wrist.SetHead(head);
                if (alerts != null) alerts.Setup(head, icons);
                if (subtitles != null) subtitles.SetHead(head);
            }
            if (head != null)
            {
                WorldLabelManager labels = GetComponentInChildren<WorldLabelManager>(true);
                if (labels != null) labels.SetRig(head);
                DecisionPanel decision = GetComponentInChildren<DecisionPanel>(true);
                if (decision != null) decision.SetRig(head);
                OutcomePanel outcome = GetComponentInChildren<OutcomePanel>(true);
                if (outcome != null) outcome.SetRig(head);
            }
            if (wristAttach != null) wristAttach.SetAnchors(leftController, rightController);
            rigApplied = head != null && leftController != null && rightController != null;
        }

        // =====================================================================
        // Impostazioni
        // =====================================================================

        /// <summary>
        /// Collega lo store delle impostazioni: i cambi vengono inoltrati a <see cref="HUDService.SettingsChanged"/>.
        /// </summary>
        private void BindSettings()
        {
            if (boundStore == UIServices.Settings) return;
            if (boundStore != null) boundStore.Changed -= HandleSettingChanged;
            boundStore = UIServices.Settings;
            if (boundStore != null) boundStore.Changed += HandleSettingChanged;
            HandleSettingChanged(null);
        }

        /// <summary>
        /// Un'impostazione è cambiata: aggiorna "riduci lampeggi" e avvisa mano e sottotitoli.
        /// </summary>
        /// <param name="id">Id dell'impostazione (null = tutte).</param>
        private void HandleSettingChanged(string id)
        {
            if (wrist != null && boundStore != null && (string.IsNullOrEmpty(id) || id == SettingsCatalog.ACCESSIBILITY_REDUCE_FLASHES))
                wrist.SetPulseAllowed(!boundStore.GetBool(SettingsCatalog.ACCESSIBILITY_REDUCE_FLASHES));
            HUDService.RaiseSettingsChanged(id);
        }

        // =====================================================================
        // Missione
        // =====================================================================

        /// <summary>
        /// Prepara la HUD per la missione in corso: livello, obiettivi, fase, timer. Mostra la HUD.
        /// </summary>
        public void BeginFromSession()
        {
            MissionDefinition mission = MissionSession.Current;
            if (mission == null) return;
            LevelConfig cfg = UIServices.Catalog != null ? UIServices.Catalog.GetLevelConfig(MissionSession.Level) : null;
            ApplyLevel(cfg);
            if (cfg == null && wrist != null) wrist.ApplyLevel(null);
            if (alerts != null) alerts.ClearAll();
            if (subtitles != null) subtitles.Clear();
            ApplyVisibility(true);
            visible = true;

            objectiveInitialized = false;
            lastObjective = null;
            SetHint(string.Empty);
            SetCountdown(string.Empty);
            SetPhase("hud.phase.scan");
            if (mission.Objectives != null && mission.Objectives.Count > 0) SetObjectiveIndex(0);
            else { SetObjective(string.Empty); SetObjectiveProgress(0, 0); }
            StartTimer(mission.GetTimeLimit(MissionSession.Level, cfg));
        }

        /// <summary>
        /// Fine missione: ferma il timer e nasconde la HUD (l'esito lo mostra MissionFlow).
        /// </summary>
        /// <param name="result">Risultato (non usato).</param>
        private void HandleMissionEnded(MissionResult result)
        {
            StopTimer();
            SetVisible(false);
        }

        // =====================================================================
        // Operazioni di HUDService
        // =====================================================================

        /// <summary>Applica le regole del livello a polso e avvisi.</summary>
        /// <param name="cfg">Configurazione del livello.</param>
        public void ApplyLevel(LevelConfig cfg)
        {
            if (wrist != null) wrist.ApplyLevel(cfg);
            if (alerts != null) alerts.ApplyLevel(cfg);
            HUDService.SetCurrentLevelInternal(cfg);
        }

        /// <summary>Mostra o nasconde tutta la HUD.</summary>
        /// <param name="show">true per mostrare.</param>
        public void SetVisible(bool show)
        {
            visible = show;
            ApplyVisibility(show);
            if (!show)
            {
                if (alerts != null) alerts.SetVisible(false);
                if (subtitles != null) subtitles.Clear();
            }
            else if (alerts != null) alerts.SetVisible(true);
        }

        /// <summary>
        /// Accende o spegne il pannello da polso.
        /// </summary>
        /// <param name="show">true per mostrare.</param>
        private void ApplyVisibility(bool show)
        {
            if (wristAttach != null) wristAttach.SetHudVisible(show);
        }

        /// <summary>Imposta il testo dell'obiettivo; impulso aptico se cambia (non al primo).</summary>
        /// <param name="text">Testo.</param>
        public void SetObjective(string text)
        {
            bool changed = objectiveInitialized && text != lastObjective;
            lastObjective = text;
            objectiveInitialized = true;
            if (wrist != null) wrist.SetObjective(text);
            if (changed && visible)
            {
                UIAudioFeedback.Play(UIAudioFeedback.SFX_HUD_OBJECTIVE);
                HapticsService haptics = HapticsService.Instance;
                if (haptics != null) haptics.PulseNonDominant(haptics.ObjectiveChange);
            }
        }

        /// <summary>Imposta l'obiettivo dall'indice di MissionDefinition.Objectives, con il contatore.</summary>
        /// <param name="index">Indice da 0.</param>
        public void SetObjectiveIndex(int index)
        {
            MissionDefinition mission = MissionSession.Current;
            if (mission == null || mission.Objectives == null || index < 0 || index >= mission.Objectives.Count) return;
            MissionFlow.ObjectiveIndex = index;
            SetObjective(mission.Objectives[index]);
            SetObjectiveProgress(index + 1, mission.Objectives.Count);
        }

        /// <summary>Imposta il contatore degli obiettivi.</summary>
        /// <param name="current">Corrente (da 1).</param>
        /// <param name="total">Totale.</param>
        public void SetObjectiveProgress(int current, int total)
        {
            if (wrist != null) wrist.SetProgress(current, total);
        }

        /// <summary>Imposta la fase.</summary>
        /// <param name="phaseKey">Chiave hud.phase.*.</param>
        public void SetPhase(string phaseKey)
        {
            if (wrist != null) wrist.SetPhase(phaseKey);
        }

        /// <summary>Imposta il suggerimento.</summary>
        /// <param name="text">Testo.</param>
        public void SetHint(string text)
        {
            if (wrist != null) wrist.SetHint(text);
        }

        /// <summary>Imposta il conto alla rovescia di scenario sul polso (vuoto = nascosto).</summary>
        /// <param name="text">Testo.</param>
        public void SetCountdown(string text)
        {
            if (wrist != null) wrist.SetCountdown(text);
        }

        /// <summary>Mostra un avviso evento.</summary>
        /// <param name="text">Testo.</param>
        /// <param name="iconName">Icona.</param>
        public void ShowEventAlert(string text, string iconName)
        {
            if (alerts != null) alerts.ShowEvent(text, iconName);
        }

        /// <summary>Mostra un feedback d'azione.</summary>
        /// <param name="text">Testo.</param>
        /// <param name="kind">Tipo.</param>
        public void ShowActionFeedback(string text, FeedbackKind kind)
        {
            if (alerts != null) alerts.ShowFeedback(text, kind);
        }

        /// <summary>Accoda un sottotitolo.</summary>
        /// <param name="speaker">Chi parla.</param>
        /// <param name="text">Testo.</param>
        /// <param name="seconds">Durata (0 = auto).</param>
        /// <param name="isEvent">Evento sonoro.</param>
        /// <param name="critical">Sempre mostrato.</param>
        public void ShowSubtitle(string speaker, string text, float seconds, bool isEvent, bool critical)
        {
            if (visible && subtitles != null) subtitles.Show(speaker, text, seconds, isEvent, critical);
        }

        /// <summary>Toglie il sottotitolo.</summary>
        public void ClearSubtitle()
        {
            if (subtitles != null) subtitles.Clear();
        }

        /// <summary>Avviso concluso: il testo resta sull'ultima riga del pannello da polso.</summary>
        /// <param name="text">Testo.</param>
        /// <param name="sprite">Icona (può essere null).</param>
        private void HandleAlertFinished(string text, Sprite sprite)
        {
            if (wrist != null) wrist.SetLastAlert(text, sprite);
        }

        /// <summary>Avviso evento solo icona (Crisis): il testo va nei sottotitoli, sempre.</summary>
        /// <param name="text">Testo dell'avviso.</param>
        private void HandleIconOnlyEvent(string text)
        {
            if (subtitles != null) subtitles.Show(string.Empty, text, 0f, true, true);
        }

        // =====================================================================
        // Timer
        // =====================================================================

        /// <summary>
        /// Avvia il timer: secondi &gt; 0 = conto alla rovescia, 0 = tempo trascorso.
        /// </summary>
        /// <param name="seconds">Secondi.</param>
        public void StartTimer(int seconds)
        {
            countdown = seconds > 0;
            remaining = seconds;
            elapsed = 0f;
            warnedFirst = warnedSecond = false;
            timerRunning = true;
            lastShownSecond = -1;
            RefreshTimerView();
        }

        /// <summary>Ferma il timer.</summary>
        public void StopTimer()
        {
            timerRunning = false;
        }

        /// <summary>
        /// Fa avanzare il timer con il tempo di gioco (si ferma in pausa), avvisa a 60 s e 30 s e alla fine.
        /// </summary>
        private void TickTimer()
        {
            if (!timerRunning) return;
            float dt = Time.deltaTime;
            elapsed += dt;
            if (countdown)
            {
                remaining = Mathf.Max(0f, remaining - dt);
                UITheme.HudSettings hud = theme.Hud;
                if (!warnedFirst && remaining <= hud.TimerWarningFirst) { warnedFirst = true; WarnTimer(); }
                if (!warnedSecond && remaining <= hud.TimerWarningSecond) { warnedSecond = true; WarnTimer(); }
                if (remaining <= 0f)
                {
                    timerRunning = false;
                    RefreshTimerView();
                    HUDService.RaiseTimerExpired();
                    return;
                }
            }
            RefreshTimerView();
        }

        /// <summary>
        /// Impulso aptico e suono dell'avviso del timer.
        /// </summary>
        private void WarnTimer()
        {
            UIAudioFeedback.Play(UIAudioFeedback.SFX_HUD_TIMER_WARNING);
            HapticsService haptics = HapticsService.Instance;
            if (haptics != null) haptics.PulseNonDominant(haptics.TimerWarning);
        }

        /// <summary>
        /// Aggiorna il timer sul polso solo quando cambia il secondo mostrato.
        /// </summary>
        private void RefreshTimerView()
        {
            if (wrist == null) return;
            int seconds = countdown ? Mathf.CeilToInt(remaining) : Mathf.FloorToInt(elapsed);
            if (seconds == lastShownSecond) return;
            lastShownSecond = seconds;
            bool warning = countdown && remaining <= theme.Hud.TimerWarningSecond;
            wrist.SetTimer(seconds, warning);
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, WristPanel newWrist, WristAttach newAttach, AlertPresenter newAlerts,
            SubtitlePresenter newSubtitles, HUDIconLibrary newIcons)
        {
            theme = newTheme; strings = newStrings; wrist = newWrist; wristAttach = newAttach; alerts = newAlerts;
            subtitles = newSubtitles; icons = newIcons;
        }
    }
}
