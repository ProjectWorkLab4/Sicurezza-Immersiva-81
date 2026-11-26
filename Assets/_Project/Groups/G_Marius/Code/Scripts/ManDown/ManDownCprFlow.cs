using System;
using System.Globalization;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>Snapshot serializzato del flusso RCP, aggiornato a ogni evento e leggibile in Play Mode.</summary>
    [Serializable]
    public struct ManDownCprFlowSnapshot
    {
        public string phase;
        public int initialCount;
        public int afterAedCount;
        public int withoutAedCount;
        public int initialTarget;
        public int afterAedTarget;
        public int withoutAedTarget;
        public bool initialComplete;
        public bool afterAedComplete;
        public bool withoutAedComplete;
        public bool instructionShown;
        public int poorRhythmAlerts;
        public float lastPoorRhythmAlertAt;
        public bool afterAedPhase;
        public bool missionEnded;
    }

    /// <summary>
    /// Integra il rilevamento fisico (<see cref="ManDownCpr"/>) e la valutazione ritmica
    /// (<see cref="ManDownCprRhythm"/>) nella progressione di <see cref="ManDownScenario"/>, usando
    /// solo la UI comune tramite <see cref="ScenarioController.Alert"/>: messaggio iniziale non bloccante
    /// e avviso di ritmo insufficiente. Non apre popup, non mostra contatori/percentuali e non tocca il
    /// punteggio: produce soltanto dati grezzi e registrazioni di fase per il futuro scoring.
    ///
    /// Una compressione fisicamente completa conta per la progressione indipendentemente dal ritmo;
    /// una compressione invalida non conta. La qualità ritmica resta separata dalle soglie di progressione.
    /// Il DAE non è implementato: <see cref="InterruptForAed"/> predispone solo l'interruzione esplicita
    /// della sequenza ritmica e il cambiamento di fase, senza simulare alcuna azione sul dispositivo.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/CPR Scenario Flow")]
    public sealed class ManDownCprFlow : MonoBehaviour
    {
        [Header("Riferimenti di scena")]
        [SerializeField] private ManDownCpr cpr;
        [SerializeField] private ManDownCprRhythm rhythm;
        [SerializeField] private ManDownScenario scenario;
        [SerializeField] private ManDownNpc npc;

        [Header("Feedback qualità ritmica")]
        [Tooltip("Qualità complessiva sotto la quale viene mostrato l'avviso (valore iniziale: 0,60).")]
        [Range(0f, 1f)]
        [SerializeField] private float poorRhythmQualityThreshold = 0.60f;
        [Tooltip("Numero minimo di intervalli valutati prima del primo avviso (valore iniziale: 5).")]
        [Min(1)]
        [SerializeField] private int minEvaluatedIntervalsForAlert = 5;
        [Tooltip("Secondi minimi tra due avvisi, misurati con il tempo di missione (valore iniziale: 5).")]
        [Min(0f)]
        [SerializeField] private float poorRhythmAlertCooldownSeconds = 5f;

        [Header("Debug")]
        [Tooltip("Se true, registra solo istruzione, avvisi, soglie raggiunte, interruzioni e reset.")]
        [SerializeField] private bool debugLog;
        [SerializeField] private ManDownCprFlowSnapshot debugSnapshot;

        private const string INSTRUCTION_PREFIX = "Esegui le compressioni toraciche mantenendo un intervallo di circa ";
        private const string INSTRUCTION_SUFFIX = " secondi tra una pressione e l'altra.";
        private const string POOR_RHYTHM_MESSAGE = "Attenzione: il ritmo delle compressioni non è adeguato.";
        private const string MONITORING_NOTE = "abnormal_breathing_cpr_completed";

        private int initialCount;
        private int afterAedCount;
        private int withoutAedCount;
        private bool initialComplete;
        private bool afterAedComplete;
        private bool withoutAedComplete;
        private bool afterAedPhase;
        private bool instructionShown;
        private int poorRhythmAlertCount;
        private float lastPoorRhythmAlertAt = -1f;
        private float interruptedForAedAt = -1f;
        private bool missionEnded;

        /// <summary>Testo dell'istruzione iniziale, con il valore configurato nel valutatore ritmico.</summary>
        public string InstructionText
        {
            get
            {
                string seconds = rhythm != null
                    ? rhythm.IdealInterval.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',')
                    : "0,55";
                return INSTRUCTION_PREFIX + seconds + INSTRUCTION_SUFFIX;
            }
        }

        /// <summary>Soglia di qualità sotto la quale viene mostrato l'avviso.</summary>
        public float PoorRhythmQualityThreshold => poorRhythmQualityThreshold;
        /// <summary>Intervalli minimi valutati prima del primo avviso.</summary>
        public int MinEvaluatedIntervalsForAlert => minEvaluatedIntervalsForAlert;
        /// <summary>Cooldown dell'avviso in secondi di missione.</summary>
        public float PoorRhythmAlertCooldownSeconds => poorRhythmAlertCooldownSeconds;

        /// <summary>Fase RCP a cui appartengono le compressioni correnti.</summary>
        public ManDownCprPhase CurrentCprPhase
        {
            get
            {
                if (scenario == null || !scenario.IsAedAvailable) return ManDownCprPhase.WithoutAed;
                if (afterAedPhase || scenario.IsCprResumed) return ManDownCprPhase.AfterDefibrillation;
                return ManDownCprPhase.InitialWithAed;
            }
        }

        /// <summary>Compressioni complete della fase corrente.</summary>
        public int CurrentPhaseCompressionCount
        {
            get
            {
                switch (CurrentCprPhase)
                {
                    case ManDownCprPhase.AfterDefibrillation: return afterAedCount;
                    case ManDownCprPhase.WithoutAed: return withoutAedCount;
                    default: return initialCount;
                }
            }
        }

        /// <summary>Soglia della fase corrente, letta dalle tre configurazioni dello scenario.</summary>
        public int CurrentPhaseTarget => scenario != null ? scenario.GetRequiredCprCompressions(CurrentCprPhase) : 0;

        /// <summary>Compressioni fisicamente complete della fase iniziale con DAE.</summary>
        public int InitialCount => initialCount;
        /// <summary>Compressioni fisicamente complete della fase dopo il DAE.</summary>
        public int AfterAedCount => afterAedCount;
        /// <summary>Compressioni fisicamente complete del percorso senza DAE.</summary>
        public int WithoutAedCount => withoutAedCount;
        /// <summary>True se la fase RCP iniziale con DAE ha raggiunto la soglia.</summary>
        public bool InitialPhaseComplete => initialComplete;
        /// <summary>True se la fase RCP dopo il DAE ha raggiunto la soglia.</summary>
        public bool AfterAedPhaseComplete => afterAedComplete;
        /// <summary>True se la RCP senza DAE ha raggiunto la soglia.</summary>
        public bool WithoutAedPhaseComplete => withoutAedComplete;
        /// <summary>True se l'istruzione iniziale è già stata mostrata in questa missione.</summary>
        public bool InstructionShown => instructionShown;
        /// <summary>Numero di avvisi di ritmo insufficiente mostrati nella missione.</summary>
        public int PoorRhythmAlertCount => poorRhythmAlertCount;
        /// <summary>Timestamp di missione dell'ultimo avviso di ritmo; -1 se mai mostrato.</summary>
        public float LastPoorRhythmAlertAt => lastPoorRhythmAlertAt;
        /// <summary>True dopo un'interruzione esplicita per il DAE.</summary>
        public bool IsAfterAedPhase => afterAedPhase;
        /// <summary>True se la missione è conclusa e il flusso RCP è disabilitato.</summary>
        public bool IsFlowDisabled => missionEnded;

        /// <summary>Soglia di fase raggiunta (fase e conteggio).</summary>
        public event Action<ManDownCprPhase, int> PhaseThresholdReached;
        /// <summary>Istruzione iniziale mostrata una sola volta.</summary>
        public event Action InstructionShownOnce;
        /// <summary>Avviso di ritmo insufficiente mostrato (timestamp di missione).</summary>
        public event Action<float> PoorRhythmAlertShown;
        /// <summary>Stato del flusso ripulito a nuova missione.</summary>
        public event Action FlowReset;

        private void OnEnable()
        {
            if (cpr != null)
            {
                cpr.CompressionCompleted += OnCompressionCompleted;
                cpr.AttemptInvalidated += OnCprAttemptInvalidated;
            }
            if (rhythm != null) rhythm.IntervalEvaluated += OnIntervalEvaluated;
            if (npc != null) npc.PositionApplied += OnNpcPositionApplied;
            if (scenario != null) scenario.MissionConcluded += OnMissionConcluded;
            MissionSession.Started += OnMissionStarted;
            UpdateSnapshot();
        }

        private void OnDisable()
        {
            UnsubscribeGameplayEvents();
            MissionSession.Started -= OnMissionStarted;
        }

        private void OnValidate()
        {
            minEvaluatedIntervalsForAlert = Mathf.Max(1, minEvaluatedIntervalsForAlert);
            poorRhythmAlertCooldownSeconds = Mathf.Max(0f, poorRhythmAlertCooldownSeconds);
            poorRhythmQualityThreshold = Mathf.Clamp01(poorRhythmQualityThreshold);
        }

        /// <summary>
        /// Mostra l'istruzione iniziale una sola volta, quando entrambe le mani raggiungono la
        /// condizione di rilascio nella zona torace. Solo nel ramo con respirazione anormale,
        /// dopo la valutazione respiratoria e in posizione supina.
        /// </summary>
        private void Update()
        {
            if (instructionShown || !CanOperate() || !IsAbnormalBranch) return;
            if (cpr == null || !cpr.IsArmed) return;

            instructionShown = true;
            scenario.Alert(InstructionText);
            InstructionShownOnce?.Invoke();
            UpdateSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown CPR] Istruzione iniziale mostrata: {InstructionText}", this);
        }

        private bool CanOperate()
        {
            return cpr != null && rhythm != null && scenario != null && npc != null &&
                   scenario.AcceptsActions && !missionEnded &&
                   npc.IsBreathingAssessed && npc.CurrentPosition == ManDownPositionChoice.None;
        }

        private bool IsAbnormalBranch => scenario != null && scenario.IsBreathingAbnormal;

        /// <summary>
        /// Una compressione fisicamente completa conta per la progressione anche se il ritmo è
        /// scorretto. La qualità ritmica viene registrata separatamente senza modificare il punteggio.
        /// </summary>
        private void OnCompressionCompleted(float timestamp)
        {
            if (!CanOperate()) return;

            if (!IsAbnormalBranch)
            {
                // RCP eseguita su persona che respira: StartCpr() l'ha già registrata come azione
                // inappropriata. Nessuna progressione, nessun DAE, nessuna conclusione automatica.
                ReportSnapshot(true);
                return;
            }

            switch (CurrentCprPhase)
            {
                case ManDownCprPhase.AfterDefibrillation: afterAedCount++; break;
                case ManDownCprPhase.WithoutAed: withoutAedCount++; break;
                default: initialCount++; break;
            }

            ManDownCprPhase phase = CurrentCprPhase;
            int count = CurrentPhaseCompressionCount;
            int target = scenario.GetRequiredCprCompressions(phase);

            if (count >= target && !IsPhaseComplete(phase))
            {
                MarkPhaseComplete(phase, timestamp);
                // InvalidCompressions = 0: i tentativi invalidi restano dati grezzi e non attivano
                // la penalità CPR_POOR_QUALITY, demandata al futuro task di scoring.
                scenario.ReportCprResult(new ManDownCprResult(phase, count, 0, 0f));
                PhaseThresholdReached?.Invoke(phase, count);
                if (phase == ManDownCprPhase.WithoutAed)
                    scenario.StartFinalMonitoring(MONITORING_NOTE);

                if (debugLog) Debug.Log($"[SI81 ManDown CPR] Soglia {phase} raggiunta: {count}/{target}.", this);
            }

            ReportSnapshot(false);
        }

        /// <summary>
        /// Avviso di ritmo insufficiente: solo dopo il numero minimo di intervalli valutati, solo
        /// durante la RCP, solo nel ramo anormale e non prima del cooldown. Non interrompe nulla.
        /// </summary>
        private void OnIntervalEvaluated(float timestamp, float interval, float error, float quality,
            ManDownCprIntervalOutcome outcome)
        {
            if (!CanOperate() || !IsAbnormalBranch) return;
            if (cpr != null && !cpr.IsArmed && !cpr.IsPressurePending) return;
            if (rhythm == null || rhythm.EvaluatedIntervalCount < minEvaluatedIntervalsForAlert) return;
            if (rhythm.OverallRhythmQuality >= poorRhythmQualityThreshold) return;

            float now = scenario.MissionElapsedSeconds;
            if (lastPoorRhythmAlertAt >= 0f && now - lastPoorRhythmAlertAt < poorRhythmAlertCooldownSeconds) return;

            lastPoorRhythmAlertAt = now;
            poorRhythmAlertCount++;
            scenario.Alert(POOR_RHYTHM_MESSAGE);
            PoorRhythmAlertShown?.Invoke(now);
            UpdateSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown CPR] Avviso ritmo insufficiente @ {now:F2}s.", this);
        }

        /// <summary>
        /// Predispone l'interruzione per il futuro DAE: azzera solo il riferimento ritmico, conserva
        /// le statistiche e fa contare le compressioni successive nella fase dopo defibrillazione.
        /// Non simula il dispositivo e non chiama API del DAE.
        /// </summary>
        public void InterruptForAed()
        {
            if (rhythm != null) rhythm.InterruptSequence();
            afterAedPhase = true;
            interruptedForAedAt = scenario != null ? scenario.MissionElapsedSeconds : -1f;
            UpdateSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown CPR] Interruzione per DAE @ {interruptedForAedAt:F2}s; statistiche conservate.", this);
        }

        /// <summary>
        /// Un ciclo fisico invalido non conta per la progressione; il detector l'ha già scartato.
        /// Qui non si azzerano le statistiche ritmiche deliberate del Task 07.2.
        /// </summary>
        private void OnCprAttemptInvalidated(float timestamp, string reason)
        {
            UpdateSnapshot();
        }

        /// <summary>
        /// Cambio di posa: il detector invalida la pressione in corso; il flusso non riavvia il
        /// monitoraggio né avanza la progressione, e non tocca le statistiche già raccolte.
        /// </summary>
        private void OnNpcPositionApplied()
        {
            UpdateSnapshot();
        }

        private bool IsPhaseComplete(ManDownCprPhase phase)
        {
            switch (phase)
            {
                case ManDownCprPhase.AfterDefibrillation: return afterAedComplete;
                case ManDownCprPhase.WithoutAed: return withoutAedComplete;
                default: return initialComplete;
            }
        }

        private void MarkPhaseComplete(ManDownCprPhase phase, float timestamp)
        {
            switch (phase)
            {
                case ManDownCprPhase.AfterDefibrillation:
                    afterAedComplete = true;
                    snapshotAfterAedAt = timestamp;
                    break;
                case ManDownCprPhase.WithoutAed:
                    withoutAedComplete = true;
                    snapshotWithoutAedAt = timestamp;
                    break;
                default:
                    initialComplete = true;
                    snapshotInitialAt = timestamp;
                    break;
            }
        }

        private float snapshotInitialAt = -1f;
        private float snapshotAfterAedAt = -1f;
        private float snapshotWithoutAedAt = -1f;

        /// <summary>Invia al controller lo stato grezzo corrente senza calcolare punteggi.</summary>
        private void ReportSnapshot(bool inappropriate)
        {
            if (scenario == null || rhythm == null || cpr == null) return;
            var snapshot = new ManDownCprQualitySnapshot(
                scenario.CprStartedAt,
                initialCount, afterAedCount, withoutAedCount,
                cpr.CompletedCompressionCount, cpr.InvalidAttemptCount,
                rhythm.EvaluatedIntervalCount, rhythm.CorrectIntervalCount,
                rhythm.TooShortIntervalCount, rhythm.TooLongIntervalCount,
                rhythm.CorrectIntervalPercentage, rhythm.CumulativeTimingError,
                rhythm.AverageTimingError, rhythm.AverageInterval, rhythm.OverallRhythmQuality,
                inappropriate || !IsAbnormalBranch,
                snapshotInitialAt, snapshotAfterAedAt, snapshotWithoutAedAt, interruptedForAedAt);
            scenario.ReportCprQualitySnapshot(snapshot);
        }

        private void OnMissionConcluded(ManDownScenarioResult result)
        {
            missionEnded = true;
            UnsubscribeGameplayEvents();
            UpdateSnapshot();
        }

        private void OnMissionStarted()
        {
            initialCount = 0;
            afterAedCount = 0;
            withoutAedCount = 0;
            initialComplete = false;
            afterAedComplete = false;
            withoutAedComplete = false;
            afterAedPhase = false;
            instructionShown = false;
            poorRhythmAlertCount = 0;
            lastPoorRhythmAlertAt = -1f;
            interruptedForAedAt = -1f;
            snapshotInitialAt = -1f;
            snapshotAfterAedAt = -1f;
            snapshotWithoutAedAt = -1f;
            missionEnded = false;
            FlowReset?.Invoke();
            UpdateSnapshot();
        }

        private void UnsubscribeGameplayEvents()
        {
            if (cpr != null)
            {
                cpr.CompressionCompleted -= OnCompressionCompleted;
                cpr.AttemptInvalidated -= OnCprAttemptInvalidated;
            }
            if (rhythm != null) rhythm.IntervalEvaluated -= OnIntervalEvaluated;
            if (npc != null) npc.PositionApplied -= OnNpcPositionApplied;
            if (scenario != null) scenario.MissionConcluded -= OnMissionConcluded;
        }

        private void UpdateSnapshot()
        {
            debugSnapshot = new ManDownCprFlowSnapshot
            {
                phase = CurrentCprPhase.ToString(),
                initialCount = initialCount,
                afterAedCount = afterAedCount,
                withoutAedCount = withoutAedCount,
                initialTarget = scenario != null ? scenario.GetRequiredCprCompressions(ManDownCprPhase.InitialWithAed) : 0,
                afterAedTarget = scenario != null ? scenario.GetRequiredCprCompressions(ManDownCprPhase.AfterDefibrillation) : 0,
                withoutAedTarget = scenario != null ? scenario.GetRequiredCprCompressions(ManDownCprPhase.WithoutAed) : 0,
                initialComplete = initialComplete,
                afterAedComplete = afterAedComplete,
                withoutAedComplete = withoutAedComplete,
                instructionShown = instructionShown,
                poorRhythmAlerts = poorRhythmAlertCount,
                lastPoorRhythmAlertAt = lastPoorRhythmAlertAt,
                afterAedPhase = afterAedPhase,
                missionEnded = missionEnded
            };
        }
    }
}
