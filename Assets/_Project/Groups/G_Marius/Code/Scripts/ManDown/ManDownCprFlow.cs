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
        public bool suspendedByAed;
        public int compressionsBeforeShock;
        public int compressionsAfterShock;
        public int graceCompressions;
        public float accumulatedValue;
        public float graceQualitySum;
        public float suspensionSeconds;
        public bool instructionShown;
        public int poorRhythmAlerts;
        public float lastPoorRhythmAlertAt;
        public bool graceActive;
        public bool missionEnded;
    }

    /// <summary>
    /// Integra rilevamento fisico e valutazione ritmica della RCP nello scenario, senza soglie di
    /// progressione: la quantità e la qualità delle compressioni sono solo dati grezzi per il Task 09.
    /// Durante analisi/autorizzazione/scarica la registrazione delle compressioni valide è sospesa,
    /// ma la rilevazione del contatto delle mani resta attiva (gestita dai componenti dedicati).
    /// Nella proroga DAE le compressioni sono contate a parte e non alimentano il valore destinato allo scoring.
    /// Nessun timer visibile, metronomo, popup o calcolo di punteggio.
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
        [SerializeField] private ManDownAedDevice aedDevice;

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
        [Tooltip("Log solo su istruzione, avvisi, sospensioni, scarica e reset: non ogni frame.")]
        [SerializeField] private bool debugLog;
        [SerializeField] private ManDownCprFlowSnapshot debugSnapshot;

        private const string INSTRUCTION_PREFIX = "Esegui le compressioni toraciche mantenendo un intervallo di circa ";
        private const string INSTRUCTION_SUFFIX = " secondi tra una pressione e l'altra.";
        private const string POOR_RHYTHM_MESSAGE = "Attenzione: il ritmo delle compressioni non è adeguato.";

        private int compressionsBeforeShock;
        private int compressionsAfterShock;
        private int graceCompressions;
        private float accumulatedValue;
        private float graceQualitySum;
        private float firstCompressionAt = -1f;
        private float lastCompressionAt = -1f;
        private float suspensionStartAt = -1f;
        private float suspensionSeconds;
        private bool suspendedByAed;
        private bool instructionShown;
        private int poorRhythmAlertCount;
        private float lastPoorRhythmAlertAt = -1f;
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

        public float PoorRhythmQualityThreshold => poorRhythmQualityThreshold;
        public int MinEvaluatedIntervalsForAlert => minEvaluatedIntervalsForAlert;
        public float PoorRhythmAlertCooldownSeconds => poorRhythmAlertCooldownSeconds;
        public int CompressionsBeforeShock => compressionsBeforeShock;
        public int CompressionsAfterShock => compressionsAfterShock;
        public int GraceCompressions => graceCompressions;
        /// <summary>Somma delle qualità degli intervalli della finestra principale (dato grezzo, non punteggio).</summary>
        public float AccumulatedValue => accumulatedValue;
        public float GraceQualitySum => graceQualitySum;
        public float FirstCompressionAt => firstCompressionAt;
        public float LastCompressionAt => lastCompressionAt;
        public float SuspensionSeconds => suspensionSeconds;
        public bool IsSuspendedByAed => suspendedByAed;
        public bool InstructionShown => instructionShown;
        public int PoorRhythmAlertCount => poorRhythmAlertCount;
        public float LastPoorRhythmAlertAt => lastPoorRhythmAlertAt;
        public bool IsFlowDisabled => missionEnded;

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
            if (scenario != null)
            {
                scenario.MissionConcluded += OnMissionConcluded;
                scenario.AbnormalWindowElapsed += OnWindowElapsed;
            }
            if (aedDevice != null)
            {
                aedDevice.AnalysisStarted += OnAedAnalysisStarted;
                aedDevice.AnalysisInterrupted += OnAedAnalysisInterrupted;
                aedDevice.ShockCompleted += OnAedShockCompleted;
            }
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

        private void OnCompressionCompleted(float timestamp)
        {
            if (!CanOperate() || !IsAbnormalBranch) return;

            if (scenario.IsGraceActive)
            {
                // Proroga: il gesto resta valido e viene registrato, ma non entra nello scoring RCP.
                graceCompressions++;
                ReportSnapshot();
                return;
            }
            if (suspendedByAed) return;

            if (scenario.LastAedSnapshot.ShockCompleted) compressionsAfterShock++;
            else compressionsBeforeShock++;

            if (firstCompressionAt < 0f) firstCompressionAt = timestamp;
            lastCompressionAt = timestamp;
            ReportSnapshot();
        }

        private void OnIntervalEvaluated(float timestamp, float interval, float error, float quality,
            ManDownCprIntervalOutcome outcome)
        {
            if (!CanOperate() || !IsAbnormalBranch) return;

            if (scenario.IsGraceActive)
            {
                graceQualitySum += Mathf.Clamp01(quality);
                ReportSnapshot();
                return;
            }
            if (!suspendedByAed) accumulatedValue += Mathf.Clamp01(quality);

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

        /// <summary>Analisi DAE: interrompe la sequenza ritmica e sospende il conteggio delle compressioni.</summary>
        private void OnAedAnalysisStarted(float timestamp)
        {
            if (suspendedByAed || missionEnded) return;
            rhythm?.InterruptSequence();
            cpr?.InterruptForAed();
            suspendedByAed = true;
            suspensionStartAt = scenario != null ? scenario.MissionElapsedSeconds : timestamp;
            ReportSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown CPR] RCP sospesa per analisi DAE @ {suspensionStartAt:F2}s.", this);
        }

        /// <summary>Se l'analisi viene interrotta senza necessità di mani lontane, la RCP riprende subito.</summary>
        private void OnAedAnalysisInterrupted(string reason)
        {
            if (!suspendedByAed || aedDevice == null || aedDevice.NeedsHandsClear) return;
            ResumeCpr("analysis_interrupted");
        }

        private void OnAedShockCompleted(float timestamp, float quality)
        {
            if (suspendedByAed) ResumeCpr("shock_completed");
        }

        private void ResumeCpr(string reason)
        {
            suspendedByAed = false;
            if (suspensionStartAt >= 0f && scenario != null)
                suspensionSeconds += Mathf.Max(0f, scenario.MissionElapsedSeconds - suspensionStartAt);
            suspensionStartAt = -1f;
            // Nuova sequenza: la prima compressione successiva stabilisce un nuovo riferimento temporale.
            rhythm?.InterruptSequence();
            ReportSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown CPR] RCP ripristinata ({reason}); sospensione totale {suspensionSeconds:F2}s.", this);
        }

        private void OnWindowElapsed(float timestamp)
        {
            // Le statistiche della finestra principale sono state congelate dallo scenario in questo istante.
            ReportSnapshot();
        }

        private void OnCprAttemptInvalidated(float timestamp, string reason)
        {
            UpdateSnapshot();
        }

        private void OnNpcPositionApplied()
        {
            UpdateSnapshot();
        }

        /// <summary>Invia al controller lo stato grezzo corrente senza calcolare punteggi.</summary>
        private void ReportSnapshot()
        {
            if (scenario == null || rhythm == null || cpr == null) return;
            var list = cpr.CompletedCompressionTimestamps;
            float first = list.Count > 0 ? list[0] : firstCompressionAt;
            float last = list.Count > 0 ? list[list.Count - 1] : lastCompressionAt;
            var snapshot = new ManDownCprQualitySnapshot(
                scenario.CprStartedAt,
                compressionsBeforeShock, compressionsAfterShock, graceCompressions,
                cpr.CompletedCompressionCount, cpr.InvalidAttemptCount,
                rhythm.EvaluatedIntervalCount, rhythm.CorrectIntervalCount,
                rhythm.TooShortIntervalCount, rhythm.TooLongIntervalCount,
                rhythm.CorrectIntervalPercentage, rhythm.CumulativeTimingError,
                rhythm.AverageTimingError, rhythm.AverageInterval, rhythm.OverallRhythmQuality,
                // Inappropriata solo se la respirazione effettiva è normale: non dipende dal fatto
                // che il gesto sia riportato allo scenario.
                !IsAbnormalBranch,
                -1f, -1f, -1f,
                -1f,
                accumulatedValue, first, last, compressionsBeforeShock, compressionsAfterShock,
                graceCompressions, graceQualitySum, suspensionSeconds +
                (suspendedByAed && scenario != null && suspensionStartAt >= 0f
                    ? Mathf.Max(0f, scenario.MissionElapsedSeconds - suspensionStartAt) : 0f));
            scenario.ReportCprQualitySnapshot(snapshot);
        }

        private void OnMissionConcluded(ManDownScenarioResult result)
        {
            missionEnded = true;
            suspendedByAed = false;
            UnsubscribeGameplayEvents();
            UpdateSnapshot();
        }

        private void OnMissionStarted()
        {
            compressionsBeforeShock = 0;
            compressionsAfterShock = 0;
            graceCompressions = 0;
            accumulatedValue = 0f;
            graceQualitySum = 0f;
            firstCompressionAt = -1f;
            lastCompressionAt = -1f;
            suspensionStartAt = -1f;
            suspensionSeconds = 0f;
            suspendedByAed = false;
            instructionShown = false;
            poorRhythmAlertCount = 0;
            lastPoorRhythmAlertAt = -1f;
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
            if (scenario != null)
            {
                scenario.MissionConcluded -= OnMissionConcluded;
                scenario.AbnormalWindowElapsed -= OnWindowElapsed;
            }
            if (aedDevice != null)
            {
                aedDevice.AnalysisStarted -= OnAedAnalysisStarted;
                aedDevice.AnalysisInterrupted -= OnAedAnalysisInterrupted;
                aedDevice.ShockCompleted -= OnAedShockCompleted;
            }
        }

        private void UpdateSnapshot()
        {
            debugSnapshot = new ManDownCprFlowSnapshot
            {
                suspendedByAed = suspendedByAed,
                compressionsBeforeShock = compressionsBeforeShock,
                compressionsAfterShock = compressionsAfterShock,
                graceCompressions = graceCompressions,
                accumulatedValue = accumulatedValue,
                graceQualitySum = graceQualitySum,
                suspensionSeconds = suspensionSeconds,
                instructionShown = instructionShown,
                poorRhythmAlerts = poorRhythmAlertCount,
                lastPoorRhythmAlertAt = lastPoorRhythmAlertAt,
                graceActive = scenario != null && scenario.IsGraceActive,
                missionEnded = missionEnded
            };
        }
    }
}
