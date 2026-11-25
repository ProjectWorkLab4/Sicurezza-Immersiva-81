using System;
using System.Collections;
using System.Collections.Generic;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Controller centrale dello scenario Man Down. Vive su MNG_Scenario e eredita da <see cref="ScenarioController"/>:
    /// la sessione, lo spawn, il timer condiviso e la chiusura della missione restano responsabilità della classe base.
    /// Questo componente possiede solo lo stato dello scenario: condizioni iniziali casuali o forzate, fasi, registro
    /// delle azioni, ricezione dei risultati RCP e classificazione dell'esito.
    ///
    /// Non gestisce input, camera, mani, interazioni o UI specifica. Le future classi ManDownNpc, ManDownPhone,
    /// ManDownCpr e ManDownAed comunicano tramite i metodi pubblici e gli eventi qui esposti.
    ///
    /// Modalità supportata: solo Realistica (180 s configurabili). Il timer è quello di HUDService: all'avvio viene
    /// riavviato con la durata dello scenario, senza creare un secondo contatore. Alla scadenza arriva TimerExpired,
    /// che chiude la missione con fallimento TIMEOUT.
    /// </summary>
    [AddComponentMenu("SI81/Scenari/Man Down Scenario")]
    public class ManDownScenario : ScenarioController
    {
        // =================================================================
        // Identificativi approvati (non mostrati al giocatore)
        // =================================================================

        /// <summary>Fallimento automatico: RCP omessa quando necessaria.</summary>
        public const string FAILURE_CPR_OMITTED = "CPR_OMITTED";
        /// <summary>Fallimento automatico: timer Realistico scaduto.</summary>
        public const string FAILURE_TIMEOUT = "TIMEOUT";
        /// <summary>Fallimento per punteggio sotto la soglia minima.</summary>
        public const string FAILURE_LOW_SCORE = "LOW_SCORE";

        /// <summary>Penalizzazione: 112 non chiamato.</summary>
        public const string PENALTY_EMERGENCY_NOT_CALLED = "EMERGENCY_NOT_CALLED";
        /// <summary>Penalizzazione: DAE presente e necessario ma mai usato.</summary>
        public const string PENALTY_AED_OMITTED = "AED_OMITTED";
        /// <summary>Penalizzazione: RCP eseguita male.</summary>
        public const string PENALTY_CPR_POOR_QUALITY = "CPR_POOR_QUALITY";
        /// <summary>Penalizzazione: DAE usato in modo scorretto (recuperabile).</summary>
        public const string PENALTY_AED_INCORRECT_USE = "AED_INCORRECT_USE";

        /// <summary>Punteggio minimo per il successo.</summary>
        public const int SUCCESS_SCORE_MIN = 80;
        /// <summary>Punteggio minimo per il successo parziale.</summary>
        public const int PARTIAL_SCORE_MIN = 50;

        // Identificativi delle azioni registrate.
        /// <summary>Azione: verifica della coscienza.</summary>
        public const string ACTION_CONSCIOUSNESS_CHECK = "CONSCIOUSNESS_CHECK";
        /// <summary>Azione: verifica della respirazione.</summary>
        public const string ACTION_BREATHING_CHECK = "BREATHING_CHECK";
        /// <summary>Azione: chiamata simulata al 112.</summary>
        public const string ACTION_CALL_112 = "CALL_112";
        /// <summary>Azione: avvio della RCP.</summary>
        public const string ACTION_CPR_START = "CPR_START";
        /// <summary>Azione: RCP iniziale con DAE completata.</summary>
        public const string ACTION_CPR_INITIAL_WITH_AED = "CPR_INITIAL_WITH_AED";
        /// <summary>Azione: RCP dopo defibrillazione completata.</summary>
        public const string ACTION_CPR_AFTER_DEFIBRILLATION = "CPR_AFTER_DEFIBRILLATION";
        /// <summary>Azione: RCP senza DAE completata.</summary>
        public const string ACTION_CPR_WITHOUT_AED = "CPR_WITHOUT_AED";
        /// <summary>Azione: problema di qualità della RCP.</summary>
        public const string ACTION_CPR_QUALITY_ISSUE = "CPR_QUALITY_ISSUE";
        /// <summary>Azione: attivazione del DAE.</summary>
        public const string ACTION_AED_ACTIVATE = "AED_ACTIVATE";
        /// <summary>Azione: elettrodo sinistro posizionato.</summary>
        public const string ACTION_AED_ELECTRODE_LEFT = "AED_ELECTRODE_LEFT";
        /// <summary>Azione: elettrodo destro posizionato.</summary>
        public const string ACTION_AED_ELECTRODE_RIGHT = "AED_ELECTRODE_RIGHT";
        /// <summary>Azione: istruzione del DAE confermata.</summary>
        public const string ACTION_AED_INSTRUCTION = "AED_INSTRUCTION";
        /// <summary>Azione: uso scorretto del DAE.</summary>
        public const string ACTION_AED_INCORRECT_USE = "AED_INCORRECT_USE";
        /// <summary>Azione: ripresa della RCP dopo il DAE.</summary>
        public const string ACTION_CPR_RESUME = "CPR_RESUME";
        /// <summary>Azione: posizione laterale.</summary>
        public const string ACTION_SIDE_POSITION = "SIDE_POSITION";
        /// <summary>Azione: monitoraggio completato.</summary>
        public const string ACTION_MONITORING = "MONITORING";
        /// <summary>Azione: inizio della fase finale di assistenza.</summary>
        public const string ACTION_FINAL_ASSISTANCE_START = "FINAL_ASSISTANCE_START";
        /// <summary>Azione: assistenza completata.</summary>
        public const string ACTION_ASSISTANCE_COMPLETED = "ASSISTANCE_COMPLETED";

        // =================================================================
        // Inspector
        // =================================================================

        [Header("Condizioni iniziali")]
        [Tooltip("Probabilità che la respirazione dell'NPC sia anormale (valore approvato: 0,70).")]
        [Range(0f, 1f)]
        [SerializeField] private float abnormalBreathingProbability = 0.7f;
        [Tooltip("Probabilità che il DAE sia presente (valore approvato: 0,75).")]
        [Range(0f, 1f)]
        [SerializeField] private float aedAvailableProbability = 0.75f;
        [Tooltip("Random oppure forzatura per i test. Il valore casuale viene mantenuto per tutta la partita.")]
        [SerializeField] private ManDownBreathingMode breathingMode = ManDownBreathingMode.Random;
        [Tooltip("Random oppure forzatura per i test. Indipendente dalla respirazione.")]
        [SerializeField] private ManDownAedAvailabilityMode aedMode = ManDownAedAvailabilityMode.Random;

        [Header("Tempi (solo modalità Realistica)")]
        [Tooltip("Durata massima della missione in secondi. Valore approvato: 180.")]
        [Min(1f)]
        [SerializeField] private float missionDurationSeconds = 180f;
        [Tooltip("Durata della breve fase finale di assistenza. 0 = conclusione esplicita (valore didattico da validare).")]
        [Min(0f)]
        [SerializeField] private float finalAssistanceDurationSeconds = 0f;

        [Header("RCP — ricezione risultati (nessuna validazione in questo task)")]
        [Tooltip("Compressioni valide richieste per la RCP iniziale quando il DAE è disponibile.")]
        [Min(1)]
        [SerializeField] private int initialWithAedCompressions = 20;
        [Tooltip("Compressioni valide richieste per la RCP dopo la defibrillazione.")]
        [Min(1)]
        [SerializeField] private int afterDefibrillationCompressions = 20;
        [Tooltip("Compressioni valide complessive richieste per la RCP senza DAE.")]
        [Min(1)]
        [SerializeField] private int withoutAedCompressions = 30;
        [Tooltip("Frequenza di riferimento minima (compressioni al minuto). Solo dato, non validata in questo task.")]
        [Min(0)]
        [SerializeField] private int referenceRateMin = 100;
        [Tooltip("Frequenza di riferimento massima (compressioni al minuto). Solo dato, non validata in questo task.")]
        [Min(0)]
        [SerializeField] private int referenceRateMax = 120;

        // =================================================================
        // Stato runtime
        // =================================================================

        private readonly List<ManDownActionRecord> actions = new List<ManDownActionRecord>();
        private readonly List<ManDownCprResult> cprResults = new List<ManDownCprResult>();
        private readonly List<string> failureCauses = new List<string>();
        private readonly List<string> penalties = new List<string>();
        private readonly HashSet<ManDownCprPhase> completedCprPhases = new HashSet<ManDownCprPhase>();
        private readonly HashSet<int> electrodesPlaced = new HashSet<int>();

        private bool initialized;
        private bool concluded;
        private bool timedOut;
        private bool breathingAbnormal;
        private bool aedAvailable;
        private bool consciousnessChecked;
        private bool breathingChecked;
        private bool call112Made;
        private bool cprStarted;
        private bool aedActivated;
        private bool aedInstructionConfirmed;
        private bool cprResumed;
        private bool sidePositionPlaced;
        private bool monitoringCompleted;
        private bool finalAssistanceStarted;
        private bool assistanceCompleted;
        private bool cprPoorQuality;
        private bool aedIncorrectUse;
        private bool incompatibleProcedure;
        private float missionStartTime;
        private ManDownPhase phase = ManDownPhase.NotStarted;
        private ManDownScenarioResult lastResult;
        private Coroutine finalAssistanceRoutine;

        // =================================================================
        // Eventi per le future classi
        // =================================================================

        /// <summary>Cambio di fase logica.</summary>
        public event Action<ManDownPhase> PhaseChanged;
        /// <summary>Verifica della coscienza completata.</summary>
        public event Action ConsciousnessAssessed;
        /// <summary>Verifica della respirazione completata (true = anormale).</summary>
        public event Action<bool> BreathingAssessed;
        /// <summary>Chiamata al 112 registrata.</summary>
        public event Action EmergencyCalled;
        /// <summary>RCP avviata.</summary>
        public event Action CprStarted;
        /// <summary>Risultato RCP ricevuto dalla futura meccanica.</summary>
        public event Action<ManDownCprResult> CprResultRegistered;
        /// <summary>Registrato un problema di qualità della RCP.</summary>
        public event Action CprQualityIssueReported;
        /// <summary>DAE attivato.</summary>
        public event Action AedActivated;
        /// <summary>Elettrodo posizionato (0 = sinistro, 1 = destro).</summary>
        public event Action<int> AedElectrodePlaced;
        /// <summary>Istruzione del DAE confermata.</summary>
        public event Action AedInstructionConfirmed;
        /// <summary>Uso scorretto del DAE registrato.</summary>
        public event Action AedIncorrectUseReported;
        /// <summary>RCP ripresa dopo il DAE.</summary>
        public event Action CprResumed;
        /// <summary>Posizione laterale eseguita.</summary>
        public event Action SidePositionPlaced;
        /// <summary>Monitoraggio completato.</summary>
        public event Action MonitoringCompleted;
        /// <summary>Fase finale di assistenza avviata.</summary>
        public event Action FinalAssistanceStarted;
        /// <summary>Ogni voce aggiunta al registro azioni.</summary>
        public event Action<ManDownActionRecord> ActionRecorded;
        /// <summary>Scenario concluso: risultato congelato (prima della chiusura della sessione comune).</summary>
        public event Action<ManDownScenarioResult> MissionConcluded;

        // =================================================================
        // Proprietà pubbliche
        // =================================================================

        /// <summary>Fase logica corrente.</summary>
        public ManDownPhase CurrentPhase => phase;
        /// <summary>true dopo l'inizializzazione e prima della conclusione.</summary>
        public bool IsInitialized => initialized;
        /// <summary>true se lo scenario accetta ancora azioni.</summary>
        public bool AcceptsActions => initialized && !concluded;
        /// <summary>true dopo la conclusione (una sola per istanza).</summary>
        public bool IsMissionConcluded => concluded;
        /// <summary>Respirazione iniziale dell'NPC (true = anormale).</summary>
        public bool IsBreathingAbnormal => breathingAbnormal;
        /// <summary>DAE disponibile nella partita.</summary>
        public bool IsAedAvailable => aedAvailable;
        /// <summary>Coscienza verificata.</summary>
        public bool IsConsciousnessChecked => consciousnessChecked;
        /// <summary>Respirazione verificata.</summary>
        public bool IsBreathingChecked => breathingChecked;
        /// <summary>112 chiamato.</summary>
        public bool IsCall112Made => call112Made;
        /// <summary>RCP avviata o risultato RCP ricevuto.</summary>
        public bool IsCprStarted => cprStarted;
        /// <summary>DAE attivato.</summary>
        public bool IsAedActivated => aedActivated;
        /// <summary>Entrambi gli elettrodi posizionati.</summary>
        public bool AreElectrodesPlaced => electrodesPlaced.Count >= 2;
        /// <summary>RCP ripresa dopo il DAE.</summary>
        public bool IsCprResumed => cprResumed;
        /// <summary>Posizione laterale eseguita.</summary>
        public bool IsSidePositionPlaced => sidePositionPlaced;
        /// <summary>Monitoraggio completato.</summary>
        public bool IsMonitoringCompleted => monitoringCompleted;
        /// <summary>Timer scaduto.</summary>
        public bool TimedOut => timedOut;
        /// <summary>Durata massima configurata in secondi.</summary>
        public float MissionDurationSeconds => missionDurationSeconds;
        /// <summary>Secondi dall'inizio effettivo della missione.</summary>
        public float MissionElapsedSeconds => initialized && !concluded ? Time.time - missionStartTime : lastResult != null ? lastResult.ElapsedSeconds : 0f;
        /// <summary>Registro azioni in sola lettura.</summary>
        public IReadOnlyList<ManDownActionRecord> Actions => actions;
        /// <summary>Risultati RCP ricevuti in sola lettura.</summary>
        public IReadOnlyList<ManDownCprResult> CprResults => cprResults;
        /// <summary>Cause di fallimento correnti (prima della conclusione possono essere vuote).</summary>
        public IReadOnlyList<string> FailureCauses => failureCauses;
        /// <summary>Penalizzazioni correnti (prima della conclusione possono essere vuote).</summary>
        public IReadOnlyList<string> Penalties => penalties;
        /// <summary>Ultimo risultato congelato (null finché la missione non finisce).</summary>
        public ManDownScenarioResult LastResult => lastResult;

        // =================================================================
        // Ciclo di vita
        // =================================================================

        /// <summary>
        /// Inizializza lo scenario all'avvio effettivo della missione. Idempotente: una seconda chiamata viene ignorata.
        /// </summary>
        protected override void OnScenarioStarted()
        {
            base.OnScenarioStarted();
            InitializeScenario();
        }

        /// <summary>
        /// Inizializza stato, condizioni iniziali, timer e registro azioni. Sicura da chiamare più volte.
        /// </summary>
        public void InitializeScenario()
        {
            if (initialized || concluded) return;

            initialized = true;
            ClearRuntimeData();
            phase = ManDownPhase.Initialization;
            missionStartTime = Time.time;

            DrawInitialConditions();

            if (MissionSession.Level != DifficultyLevel.Operational)
                Debug.LogWarning("[SI81 ManDown] È implementata solo la modalità Realistica (Operational). " +
                                 "Il timer e le soglie di questo scenario restano quelli configurati.", this);

            // Stesso timer della UI comune: si riavvia con la durata dello scenario, non se ne crea un secondo.
            HUDService.StartTimer(Mathf.Max(1, Mathf.RoundToInt(missionDurationSeconds)));
            SetPhase(PHASE_SCAN);
            ChangePhase(ManDownPhase.ConsciousnessAssessment);

            Debug.Log($"[SI81 ManDown] Scenario inizializzato: respirazione {(breathingAbnormal ? "anormale" : "normale")}, " +
                      $"DAE {(aedAvailable ? "presente" : "assente")}, durata {missionDurationSeconds:F0}s.", this);
        }

        /// <summary>
        /// Reset completo dei dati dello scenario. Se la missione è ancora in corso e il componente è attivo,
        /// reinizializza subito stati, condizioni iniziali e timer. Dopo la conclusione la sessione comune non può
        /// essere riaperta sulla stessa istanza: per una nuova partita va ricaricata la scena.
        /// </summary>
        public void ResetScenario()
        {
            bool restart = initialized && !concluded && isActiveAndEnabled;

            StopFinalAssistanceRoutine();
            ClearRuntimeData();
            initialized = false;
            phase = ManDownPhase.NotStarted;
            missionStartTime = 0f;
            lastResult = null;
            HUDService.StopTimer();

            Debug.Log("[SI81 ManDown] Reset dello scenario eseguito.", this);

            if (restart) InitializeScenario();
        }

        /// <summary>
        /// Ripulisce gli eventi prima della distruzione per non lasciare riferimenti alla sessione precedente.
        /// </summary>
        protected override void OnDestroy()
        {
            StopFinalAssistanceRoutine();
            ClearEvents();
            base.OnDestroy();
        }

        /// <summary>
        /// Timeout: fallimento automatico, con tutte le azioni già registrate conservate.
        /// </summary>
        protected override void OnTimerExpired()
        {
            if (!AcceptsActions) return;
            timedOut = true;
            Debug.Log("[SI81 ManDown] Timer scaduto: fallimento automatico TIMEOUT.", this);
            Conclude(CalculateProvisionalScore());
        }

        // =================================================================
        // Azioni dello scenario
        // =================================================================

        /// <summary>Verifica dello stato di coscienza.</summary>
        public void VerifyConsciousness()
        {
            if (!AcceptsActions || consciousnessChecked) return;

            consciousnessChecked = true;
            RecordAction(ACTION_CONSCIOUSNESS_CHECK, true, true, null);
            if (phase == ManDownPhase.Initialization || phase == ManDownPhase.ConsciousnessAssessment)
                ChangePhase(ManDownPhase.BreathingAssessment);
            ConsciousnessAssessed?.Invoke();
        }

        /// <summary>
        /// Verifica della respirazione: applica il risultato iniziale (casuale o forzato) e passa al ramo corretto.
        /// La verifica non cambia il risultato: le condizioni restano stabili per tutta la partita.
        /// </summary>
        public void VerifyBreathing()
        {
            if (!AcceptsActions || breathingChecked) return;

            breathingChecked = true;
            RecordAction(ACTION_BREATHING_CHECK, true, true, breathingAbnormal ? "abnormal" : "normal");

            if (breathingAbnormal && (sidePositionPlaced || monitoringCompleted)) incompatibleProcedure = true;
            if (!breathingAbnormal && cprStarted) incompatibleProcedure = true;

            if (phase == ManDownPhase.Initialization || phase == ManDownPhase.ConsciousnessAssessment ||
                phase == ManDownPhase.BreathingAssessment || phase == ManDownPhase.Assistance || phase == ManDownPhase.Cpr)
                ChangePhase(breathingAbnormal ? ManDownPhase.Cpr : ManDownPhase.Assistance);

            BreathingAssessed?.Invoke(breathingAbnormal);
        }

        /// <summary>Chiamata simulata al 112. Indipendente dalle altre azioni e non obbligatoria per proseguire.</summary>
        public void Call112()
        {
            if (!AcceptsActions || call112Made) return;

            call112Made = true;
            RecordAction(ACTION_CALL_112, true, true, null);
            EmergencyCalled?.Invoke();
        }

        /// <summary>
        /// Avvio della RCP. Consentito anche senza aver completato la verifica della respirazione e anche se il DAE
        /// è già stato avviato: il DAE non è bloccato dalle 20 compressioni iniziali.
        /// </summary>
        public void StartCpr()
        {
            if (!AcceptsActions) return;
            if (cprStarted)
            {
                if (phase != ManDownPhase.Aed && phase != ManDownPhase.CprResume) ChangePhase(ManDownPhase.Cpr);
                return;
            }

            cprStarted = true;
            if (breathingChecked && !breathingAbnormal) incompatibleProcedure = true;
            RecordAction(ACTION_CPR_START, true, true, null);
            if (phase != ManDownPhase.Aed && phase != ManDownPhase.CprResume) ChangePhase(ManDownPhase.Cpr);
            CprStarted?.Invoke();
        }

        /// <summary>
        /// Riceve e registra il risultato della futura meccanica RCP. Non rileva né valida le compressioni:
        /// salva i valori grezzi e considera completata la fase solo se il numero di valide raggiunge la soglia.
        /// </summary>
        /// <param name="result">Valori grezzi prodotti da ManDownCpr.</param>
        public void ReportCprResult(ManDownCprResult result)
        {
            if (!AcceptsActions) return;

            if (!cprStarted)
            {
                cprStarted = true;
                RecordAction(ACTION_CPR_START, true, true, "reported_by_cpr_result");
                CprStarted?.Invoke();
            }

            cprResults.Add(result);
            if (result.InvalidCompressions > 0) ReportCprPoorQuality();

            if (result.ValidCompressions >= GetRequiredCprCompressions(result.Phase) &&
                completedCprPhases.Add(result.Phase))
            {
                RecordAction(GetCprCompletionAction(result.Phase), true, true,
                    $"valid={result.ValidCompressions}, invalid={result.InvalidCompressions}, cpm={result.AverageCompressionsPerMinute:F0}");
            }

            ChangePhase(result.Phase == ManDownCprPhase.AfterDefibrillation ? ManDownPhase.CprResume : ManDownPhase.Cpr);
            CprResultRegistered?.Invoke(result);
        }

        /// <summary>Registra un problema di qualità della RCP (una sola volta).</summary>
        public void ReportCprPoorQuality()
        {
            if (!AcceptsActions || cprPoorQuality) return;

            cprPoorQuality = true;
            AddPenalty(PENALTY_CPR_POOR_QUALITY);
            RecordAction(ACTION_CPR_QUALITY_ISSUE, true, false, null);
            CprQualityIssueReported?.Invoke();
        }

        /// <summary>Attivazione del DAE. Ignorata se il DAE non è disponibile nella partita.</summary>
        public void ActivateAed()
        {
            if (!AcceptsActions || !aedAvailable || aedActivated) return;

            aedActivated = true;
            RecordAction(ACTION_AED_ACTIVATE, true, true, null);
            if (phase != ManDownPhase.CprResume) ChangePhase(ManDownPhase.Aed);
            AedActivated?.Invoke();
        }

        /// <summary>
        /// Posiziona un elettrodo (0 = sinistro, 1 = destro). Ogni elettrodo è registrato una sola volta.
        /// Ignorato se il DAE non è disponibile.
        /// </summary>
        public void PlaceElectrode(int electrodeIndex)
        {
            if (!AcceptsActions || !aedAvailable) return;
            if (electrodeIndex != 0 && electrodeIndex != 1) return;
            if (!electrodesPlaced.Add(electrodeIndex)) return;

            RecordAction(electrodeIndex == 0 ? ACTION_AED_ELECTRODE_LEFT : ACTION_AED_ELECTRODE_RIGHT, true, true, null);
            if (phase != ManDownPhase.CprResume) ChangePhase(ManDownPhase.Aed);
            AedElectrodePlaced?.Invoke(electrodeIndex);
        }

        /// <summary>Wrapper senza parametri per UnityEvent: elettrodo sinistro.</summary>
        public void PlaceLeftElectrode()
        {
            PlaceElectrode(0);
        }

        /// <summary>Wrapper senza parametri per UnityEvent: elettrodo destro.</summary>
        public void PlaceRightElectrode()
        {
            PlaceElectrode(1);
        }

        /// <summary>Conferma dell'istruzione del DAE (analisi simulata / defibrillazione quando indicata).</summary>
        public void ConfirmAedInstruction()
        {
            if (!AcceptsActions || !aedAvailable || aedInstructionConfirmed) return;

            aedInstructionConfirmed = true;
            RecordAction(ACTION_AED_INSTRUCTION, true, true, null);
            if (phase != ManDownPhase.CprResume) ChangePhase(ManDownPhase.Aed);
            AedInstructionConfirmed?.Invoke();
        }

        /// <summary>Registra un uso scorretto del DAE (recuperabile, una sola volta).</summary>
        public void ReportAedIncorrectUse()
        {
            if (!AcceptsActions || aedIncorrectUse) return;

            aedIncorrectUse = true;
            AddPenalty(PENALTY_AED_INCORRECT_USE);
            RecordAction(ACTION_AED_INCORRECT_USE, true, false, null);
            AedIncorrectUseReported?.Invoke();
        }

        /// <summary>Ripresa della RCP dopo il DAE.</summary>
        public void ResumeCpr()
        {
            if (!AcceptsActions || cprResumed) return;

            cprResumed = true;
            if (!cprStarted) cprStarted = true;
            RecordAction(ACTION_CPR_RESUME, true, true, null);
            ChangePhase(ManDownPhase.CprResume);
            CprResumed?.Invoke();
        }

        /// <summary>Posa laterale nel ramo con respirazione normale (recuperabile).</summary>
        public void PlaceSidePosition()
        {
            if (!AcceptsActions || sidePositionPlaced) return;

            sidePositionPlaced = true;
            bool appropriate = !(breathingChecked && breathingAbnormal);
            if (!appropriate) incompatibleProcedure = true;
            RecordAction(ACTION_SIDE_POSITION, true, appropriate, appropriate ? null : "breathing_abnormal");
            if (appropriate) ChangePhase(ManDownPhase.Assistance);
            SidePositionPlaced?.Invoke();
        }

        /// <summary>Monitoraggio completato nel ramo con respirazione normale (recuperabile).</summary>
        public void Monitor()
        {
            if (!AcceptsActions || monitoringCompleted) return;

            monitoringCompleted = true;
            bool appropriate = !(breathingChecked && breathingAbnormal);
            if (!appropriate) incompatibleProcedure = true;
            RecordAction(ACTION_MONITORING, true, appropriate, appropriate ? null : "breathing_abnormal");
            if (appropriate) ChangePhase(ManDownPhase.FinalAssistance);
            MonitoringCompleted?.Invoke();
        }

        /// <summary>
        /// Avvia la breve fase finale di assistenza. Con durata &gt; 0 la conclusione è automatica; con 0 resta
        /// esplicita tramite <see cref="CompleteFinalAssistance"/>.
        /// </summary>
        public void BeginFinalAssistance()
        {
            if (!AcceptsActions || finalAssistanceStarted) return;

            finalAssistanceStarted = true;
            RecordAction(ACTION_FINAL_ASSISTANCE_START, true, true, null);
            ChangePhase(ManDownPhase.FinalAssistance);
            FinalAssistanceStarted?.Invoke();

            if (Application.isPlaying && finalAssistanceDurationSeconds > 0f)
                finalAssistanceRoutine = StartCoroutine(FinalAssistanceDelay());
        }

        /// <summary>Completa l'assistenza e conclude la missione (una sola volta).</summary>
        public void CompleteFinalAssistance()
        {
            if (!AcceptsActions) return;

            finalAssistanceStarted = true;
            assistanceCompleted = true;
            RecordAction(ACTION_ASSISTANCE_COMPLETED, true, true, null);
            ChangePhase(ManDownPhase.FinalAssistance);
            FinishMission();
        }

        // =================================================================
        // Conclusione
        // =================================================================

        /// <summary>
        /// Conclude la missione usando il punteggio provvisorio calcolato dai dati registrati. La missione può essere
        /// conclusa una sola volta; le cause automatiche (TIMEOUT, CPR_OMITTED, LOW_SCORE) hanno la priorità.
        /// </summary>
        public void FinishMission()
        {
            if (!AcceptsActions) return;
            Conclude(CalculateProvisionalScore());
        }

        /// <summary>
        /// Conclude la missione con un punteggio esplicito (per test e per il futuro task di scoring).
        /// Applica comunque fallimenti automatici, penalizzazioni e limite dovuto alla mancata chiamata al 112.
        /// </summary>
        /// <param name="safetyScore">Punteggio sicurezza 0-100.</param>
        public void FinishMission(int safetyScore)
        {
            if (!AcceptsActions) return;
            Conclude(Mathf.Clamp(safetyScore, 0, 100));
        }

        /// <summary>
        /// Soglia di compressioni valide per una fase RCP, dai valori configurati nell'Inspector.
        /// </summary>
        /// <param name="cprPhase">Fase RCP.</param>
        /// <returns>Compressioni valide richieste.</returns>
        public int GetRequiredCprCompressions(ManDownCprPhase cprPhase)
        {
            switch (cprPhase)
            {
                case ManDownCprPhase.InitialWithAed: return initialWithAedCompressions;
                case ManDownCprPhase.AfterDefibrillation: return afterDefibrillationCompressions;
                case ManDownCprPhase.WithoutAed: return withoutAedCompressions;
                default: return initialWithAedCompressions;
            }
        }

        // =================================================================
        // Interni
        // =================================================================

        private void DrawInitialConditions()
        {
            switch (breathingMode)
            {
                case ManDownBreathingMode.ForceNormal:
                    breathingAbnormal = false;
                    break;
                case ManDownBreathingMode.ForceAbnormal:
                    breathingAbnormal = true;
                    break;
                default:
                    breathingAbnormal = UnityEngine.Random.value < Mathf.Clamp01(abnormalBreathingProbability);
                    break;
            }

            switch (aedMode)
            {
                case ManDownAedAvailabilityMode.ForceAvailable:
                    aedAvailable = true;
                    break;
                case ManDownAedAvailabilityMode.ForceUnavailable:
                    aedAvailable = false;
                    break;
                default:
                    aedAvailable = UnityEngine.Random.value < Mathf.Clamp01(aedAvailableProbability);
                    break;
            }
        }

        private void ChangePhase(ManDownPhase next)
        {
            if (concluded || phase == next) return;
            phase = next;
            SetPhase(PhaseKeyFor(next));
            PhaseChanged?.Invoke(next);
        }

        private static string PhaseKeyFor(ManDownPhase value)
        {
            switch (value)
            {
                case ManDownPhase.Assistance:
                case ManDownPhase.Cpr:
                case ManDownPhase.Aed:
                case ManDownPhase.CprResume:
                case ManDownPhase.FinalAssistance:
                    return PHASE_ACTION;
                case ManDownPhase.Concluded:
                    return PHASE_RESOLUTION;
                default:
                    return PHASE_SCAN;
            }
        }

        private void RecordAction(string actionId, bool completed, bool success, string note)
        {
            var record = new ManDownActionRecord(actionId, phase, Time.time - missionStartTime, completed, success, note);
            actions.Add(record);
            ActionRecorded?.Invoke(record);
        }

        private void AddPenalty(string penaltyId)
        {
            if (!penalties.Contains(penaltyId)) penalties.Add(penaltyId);
        }

        private void AddFailure(string failureId)
        {
            if (!failureCauses.Contains(failureId)) failureCauses.Add(failureId);
        }

        private static string GetCprCompletionAction(ManDownCprPhase cprPhase)
        {
            switch (cprPhase)
            {
                case ManDownCprPhase.AfterDefibrillation: return ACTION_CPR_AFTER_DEFIBRILLATION;
                case ManDownCprPhase.WithoutAed: return ACTION_CPR_WITHOUT_AED;
                default: return ACTION_CPR_INITIAL_WITH_AED;
            }
        }

        private void Conclude(int safetyScore)
        {
            if (concluded) return;
            safetyScore = Mathf.Clamp(safetyScore, 0, 100);

            failureCauses.Clear();
            penalties.Clear();

            bool cprOmitted = breathingAbnormal && !cprStarted && cprResults.Count == 0;

            if (timedOut) AddFailure(FAILURE_TIMEOUT);
            if (cprOmitted) AddFailure(FAILURE_CPR_OMITTED);
            if (safetyScore < PARTIAL_SCORE_MIN) AddFailure(FAILURE_LOW_SCORE);

            if (!call112Made) AddPenalty(PENALTY_EMERGENCY_NOT_CALLED);
            if (breathingAbnormal && aedAvailable && !aedActivated) AddPenalty(PENALTY_AED_OMITTED);
            if (cprPoorQuality) AddPenalty(PENALTY_CPR_POOR_QUALITY);
            if (aedIncorrectUse) AddPenalty(PENALTY_AED_INCORRECT_USE);

            bool automaticFailure = timedOut || cprOmitted || safetyScore < PARTIAL_SCORE_MIN;
            OutcomeType outcome;
            if (automaticFailure) outcome = OutcomeType.Negative;
            else if (safetyScore >= SUCCESS_SCORE_MIN) outcome = call112Made ? OutcomeType.Positive : OutcomeType.Partial;
            else outcome = OutcomeType.Partial;

            int[] dimensions = CalculateProvisionalDimensions(safetyScore);
            bool rescue = call112Made;
            float elapsed = Time.time - missionStartTime;

            var result = new ManDownScenarioResult(
                outcome, safetyScore, dimensions, failureCauses, penalties, rescue, elapsed,
                breathingAbnormal, aedAvailable, call112Made, timedOut);

            lastResult = result;
            concluded = true;
            timedOut = false;

            SetPhase(PHASE_RESOLUTION);
            phase = ManDownPhase.Concluded;
            PhaseChanged?.Invoke(ManDownPhase.Concluded);
            MissionConcluded?.Invoke(result);

            base.EndMission(outcome, safetyScore, dimensions, BuildDebriefLines(outcome, rescue));

            Debug.Log($"[SI81 ManDown] Missione conclusa: {outcome}, punteggio {safetyScore}, " +
                      $"cause=[{string.Join(", ", failureCauses)}], penalizzazioni=[{string.Join(", ", penalties)}], " +
                      $"soccorsi simulati={rescue}.", this);
        }

        /// <summary>
        /// Punteggio provvisorio basato sulle cinque dimensioni approvate, in attesa del task di scoring definitivo.
        /// Safety 5/5; Awareness 10; Decision 30 meno i malus; Execution solo dalle azioni registrate; Time non ancora
        /// calcolato. La somma ponderata è quella di <c>Man_Down_Scoring_System.md</c>.
        /// </summary>
        private int CalculateProvisionalScore()
        {
            int[] dims = CalculateProvisionalDimensions(-1);
            float weighted = 0.05f * dims[0] + 0.10f * dims[1] + 0.30f * dims[2] + 0.30f * dims[3] + 0.25f * dims[4];
            return Mathf.Clamp(Mathf.RoundToInt(weighted), 0, 100);
        }

        /// <summary>
        /// Costruisce le cinque dimensioni normalizzate 0-100. Se <paramref name="fixedScore"/> è &gt;= 0 il punteggio
        /// resta quello esplicito, ma le dimensioni continuano a descrivere lo stato registrato.
        /// </summary>
        private int[] CalculateProvisionalDimensions(int fixedScore)
        {
            int safety = 100;
            int awareness = (consciousnessChecked ? 50 : 0) + (breathingChecked ? 50 : 0);

            int decisionMalus = 0;
            if (!call112Made) decisionMalus += 15;
            if (breathingAbnormal && aedAvailable && !aedActivated) decisionMalus += 5;
            if (incompatibleProcedure) decisionMalus += 5;
            int decision = Mathf.Clamp(30 - decisionMalus, 0, 30) * 100 / 30;

            float executionPoints;
            if (breathingAbnormal)
            {
                float cprPoints = 0f;
                if (aedAvailable)
                {
                    float initial = GetCprProgress(ManDownCprPhase.InitialWithAed);
                    float after = GetCprProgress(ManDownCprPhase.AfterDefibrillation);
                    cprPoints = (initial + after) * 10f; // due fasi condividono il cap di 20 punti.
                }
                else
                {
                    cprPoints = GetCprProgress(ManDownCprPhase.WithoutAed) * 30f;
                }

                float aedPoints = 0f;
                if (aedAvailable)
                {
                    int steps = 0;
                    if (aedActivated) steps++;
                    if (electrodesPlaced.Count >= 2) steps++;
                    if (aedInstructionConfirmed) steps++;
                    aedPoints = 10f * steps / 3f;
                }

                executionPoints = cprPoints + aedPoints;
            }
            else
            {
                executionPoints = (sidePositionPlaced ? 15f : 0f) + (monitoringCompleted ? 15f : 0f);
            }

            int execution = Mathf.Clamp(Mathf.RoundToInt(executionPoints / 30f * 100f), 0, 100);
            int time = 0; // T_target/T_max non ancora forniti: il task di scoring li calcolerà.

            if (fixedScore >= 0)
                return new[] { safety, awareness, decision, execution, time };
            return new[] { safety, awareness, decision, execution, time };
        }

        private float GetCprProgress(ManDownCprPhase cprPhase)
        {
            int target = Mathf.Max(1, GetRequiredCprCompressions(cprPhase));
            int best = 0;
            for (int i = 0; i < cprResults.Count; i++)
                if (cprResults[i].Phase == cprPhase && cprResults[i].ValidCompressions > best)
                    best = cprResults[i].ValidCompressions;
            return Mathf.Clamp01((float)best / target);
        }

        private List<string> BuildDebriefLines(OutcomeType outcome, bool rescue)
        {
            var lines = new List<string>();

            switch (outcome)
            {
                case OutcomeType.Positive:
                    lines.Add(DebriefLinePrefix.GOOD + "Scenario completato: soccorsi attivati e procedure eseguite.");
                    break;
                case OutcomeType.Partial:
                    lines.Add(DebriefLinePrefix.BAD + "Esito parziale: procedure non completate o non corrette.");
                    break;
                default:
                    lines.Add(DebriefLinePrefix.CRITICAL + "Esito negativo: il collega non ha ricevuto l'assistenza necessaria.");
                    break;
            }

            if (failureCauses.Contains(FAILURE_TIMEOUT))
                lines.Add(DebriefLinePrefix.CRITICAL + "Tempo scaduto prima di completare le procedure.");
            if (failureCauses.Contains(FAILURE_CPR_OMITTED))
                lines.Add(DebriefLinePrefix.CRITICAL + "RCP non eseguita nonostante la respirazione anormale.");
            if (failureCauses.Contains(FAILURE_LOW_SCORE))
                lines.Add(DebriefLinePrefix.BAD + "Punteggio finale insufficiente.");

            if (penalties.Contains(PENALTY_EMERGENCY_NOT_CALLED))
                lines.Add(DebriefLinePrefix.BAD + "112 non chiamato: soccorsi non simulati e collega non dichiarato fuori pericolo.");
            if (penalties.Contains(PENALTY_AED_OMITTED))
                lines.Add(DebriefLinePrefix.BAD + "DAE presente ma non utilizzato.");
            if (penalties.Contains(PENALTY_CPR_POOR_QUALITY))
                lines.Add(DebriefLinePrefix.BAD + "RCP eseguita con qualità insufficiente.");
            if (penalties.Contains(PENALTY_AED_INCORRECT_USE))
                lines.Add(DebriefLinePrefix.BAD + "DAE utilizzato in modo scorretto.");

            if (rescue)
                lines.Add(DebriefLinePrefix.GOOD + "Soccorsi simulati in arrivo dopo la chiamata al 112.");

            return lines;
        }

        private IEnumerator FinalAssistanceDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, finalAssistanceDurationSeconds));
            finalAssistanceRoutine = null;
            CompleteFinalAssistance();
        }

        private void StopFinalAssistanceRoutine()
        {
            if (finalAssistanceRoutine != null)
            {
                StopCoroutine(finalAssistanceRoutine);
                finalAssistanceRoutine = null;
            }
        }

        private void ClearRuntimeData()
        {
            actions.Clear();
            cprResults.Clear();
            failureCauses.Clear();
            penalties.Clear();
            completedCprPhases.Clear();
            electrodesPlaced.Clear();

            concluded = false;
            timedOut = false;
            breathingAbnormal = false;
            aedAvailable = false;
            consciousnessChecked = false;
            breathingChecked = false;
            call112Made = false;
            cprStarted = false;
            aedActivated = false;
            aedInstructionConfirmed = false;
            cprResumed = false;
            sidePositionPlaced = false;
            monitoringCompleted = false;
            finalAssistanceStarted = false;
            assistanceCompleted = false;
            cprPoorQuality = false;
            aedIncorrectUse = false;
            incompatibleProcedure = false;
        }

        private void ClearEvents()
        {
            PhaseChanged = null;
            ConsciousnessAssessed = null;
            BreathingAssessed = null;
            EmergencyCalled = null;
            CprStarted = null;
            CprResultRegistered = null;
            CprQualityIssueReported = null;
            AedActivated = null;
            AedElectrodePlaced = null;
            AedInstructionConfirmed = null;
            AedIncorrectUseReported = null;
            CprResumed = null;
            SidePositionPlaced = null;
            MonitoringCompleted = null;
            FinalAssistanceStarted = null;
            ActionRecorded = null;
            MissionConcluded = null;
        }
    }
}
