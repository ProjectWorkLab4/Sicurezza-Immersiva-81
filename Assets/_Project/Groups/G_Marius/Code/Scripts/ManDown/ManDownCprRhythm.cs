using System;
using System.Collections.Generic;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>Classificazione di un intervallo tra due pressioni appartenenti a compressioni complete.</summary>
    public enum ManDownCprIntervalOutcome
    {
        /// <summary>Intervallo dentro la fascia corretta (errore zero).</summary>
        Correct,
        /// <summary>Intervallo più breve della soglia minima.</summary>
        TooShort,
        /// <summary>Intervallo più lungo della soglia massima.</summary>
        TooLong
    }

    /// <summary>Un intervallo valutato; i timestamp sono quelli della pressione corrente e di quella precedente.</summary>
    public readonly struct ManDownCprIntervalRecord
    {
        /// <summary>Timestamp di missione della pressione corrente.</summary>
        public float Timestamp { get; }
        /// <summary>Timestamp di missione della pressione precedente.</summary>
        public float PreviousTimestamp { get; }
        /// <summary>Differenza tra i due timestamp di pressione, in secondi.</summary>
        public float Interval { get; }
        /// <summary>Errore non negativo rispetto alla fascia corretta.</summary>
        public float Error { get; }
        /// <summary>Qualità dell'intervallo, compresa tra 0 e 1.</summary>
        public float Quality { get; }
        /// <summary>Esito dell'intervallo.</summary>
        public ManDownCprIntervalOutcome Outcome { get; }

        internal ManDownCprIntervalRecord(float timestamp, float previousTimestamp, float interval,
            float error, float quality, ManDownCprIntervalOutcome outcome)
        {
            Timestamp = timestamp;
            PreviousTimestamp = previousTimestamp;
            Interval = interval;
            Error = error;
            Quality = quality;
            Outcome = outcome;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Interval:F3}s err={Error:F3} q={Quality:F2} ({Outcome})";
        }
    }

    /// <summary>
    /// Snapshot serializzato delle statistiche: viene aggiornato a ogni evento e resta leggibile
    /// nell'Inspector durante il Play Mode, senza log per frame e senza dipendere dalla UI.
    /// </summary>
    [Serializable]
    public struct ManDownCprRhythmSnapshot
    {
        public int completedCompressions;
        public int evaluatedIntervals;
        public int correctIntervals;
        public int tooShortIntervals;
        public int tooLongIntervals;
        public float correctPercent;
        public float cumulativeError;
        public float averageError;
        public float averageInterval;
        public float rhythmQuality;
        public float lastInterval;
        public float lastError;
        public float lastQuality;
        public string sequenceState;
        public bool applicableForScoring;
    }

    /// <summary>
    /// Valutazione della qualità ritmica delle compressioni rilevate da <see cref="ManDownCpr"/>.
    /// Si iscrive agli eventi del detector: una pressione viene messa in attesa e diventa parte della
    /// sequenza solo quando la compressione viene completata dal rilascio. Gli intervalli usano quindi
    /// esclusivamente i timestamp delle pressioni di compressioni fisicamente complete, mai quelli
    /// dei rilasci. La prima compressione completa è il riferimento iniziale e non viene valutata.
    ///
    /// Il componente non modifica il rilevamento fisico, non chiama la UI, non implementa metronomi,
    /// soglie di progressione né scoring: espone solo dati grezzi per il futuro task di valutazione.
    /// L'interruzione esplicita (<see cref="InterruptSequence"/>) serve alla futura ripresa dopo il DAE:
    /// azzera il riferimento temporale ma conserva tutte le statistiche già accumulate.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/CPR Rhythm Evaluation")]
    public sealed class ManDownCprRhythm : MonoBehaviour
    {
        /// <summary>Tolleranza per il confronto con le soglie: assorbe l'arrotondamento dei float.</summary>
        private const float EPSILON = 0.0001f;

        [Header("Riferimenti di scena")]
        [Tooltip("Detector fisico sulla zona torace: fornisce pressioni e compressioni complete.")]
        [SerializeField] private ManDownCpr cpr;
        [Tooltip("Controller dello scenario: solo per sapere se la respirazione è anomala e per il tempo di missione.")]
        [SerializeField] private ManDownScenario scenario;

        [Header("Parametri di valutazione (secondi)")]
        [Tooltip("Intervallo di riferimento ideale: dato indicativo per la calibrazione, non entra nella formula dell'errore.")]
        [Min(0f)]
        [SerializeField] private float idealInterval = 0.55f;
        [Tooltip("Intervallo minimo considerato corretto (valore iniziale: 0,50 s).")]
        [Min(0f)]
        [SerializeField] private float minCorrectInterval = 0.50f;
        [Tooltip("Intervallo massimo considerato corretto (valore iniziale: 0,60 s).")]
        [Min(0f)]
        [SerializeField] private float maxCorrectInterval = 0.60f;
        [Tooltip("Tolleranza massima dell'errore: a errore uguale a questo valore la qualità è 0 (valore iniziale: 0,40 s).")]
        [Min(EPSILON)]
        [SerializeField] private float maxErrorTolerance = 0.40f;

        [Header("Debug")]
        [Tooltip("Se true, registra in Console solo intervalli valutati, interruzioni e reset (mai un log per frame).")]
        [SerializeField] private bool debugLog;
        [SerializeField] private ManDownCprRhythmSnapshot debugSnapshot;

        private readonly List<ManDownCprIntervalRecord> intervals = new List<ManDownCprIntervalRecord>();

        private float pendingPressureTimestamp = -1f;
        private float lastCompletedPressureTimestamp = -1f;
        private float sumIntervals;
        private float sumErrors;
        private float sumQualities;
        private int correctCount;
        private int tooShortCount;
        private int tooLongCount;
        private float lastInterval;
        private float lastError;
        private float lastQuality;
        private ManDownCprIntervalOutcome lastOutcome;
        private bool sequenceInterrupted;

        /// <summary>Intervallo ideale di riferimento, in secondi.</summary>
        public float IdealInterval => idealInterval;
        /// <summary>Limite inferiore della fascia corretta, in secondi.</summary>
        public float MinCorrectInterval => minCorrectInterval;
        /// <summary>Limite superiore della fascia corretta, in secondi.</summary>
        public float MaxCorrectInterval => maxCorrectInterval;
        /// <summary>Tolleranza massima dell'errore, in secondi.</summary>
        public float MaxErrorTolerance => maxErrorTolerance;

        /// <summary>Compressioni fisicamente complete rilevate dal detector.</summary>
        public int CompletedCompressionCount => cpr != null ? cpr.CompletedCompressionCount : 0;
        /// <summary>Tentativi fisicamente invalidi registrati dal detector nel Task 07.1.</summary>
        public int InvalidAttemptCount => cpr != null ? cpr.InvalidAttemptCount : 0;
        /// <summary>Numero di intervalli valutati.</summary>
        public int EvaluatedIntervalCount => intervals.Count;
        /// <summary>Numero di intervalli corretti.</summary>
        public int CorrectIntervalCount => correctCount;
        /// <summary>Numero di intervalli troppo brevi.</summary>
        public int TooShortIntervalCount => tooShortCount;
        /// <summary>Numero di intervalli troppo lunghi.</summary>
        public int TooLongIntervalCount => tooLongCount;
        /// <summary>Percentuale di intervalli corretti; 0 se non ci sono intervalli valutabili.</summary>
        public float CorrectIntervalPercentage => intervals.Count > 0 ? 100f * correctCount / intervals.Count : 0f;
        /// <summary>Somma degli errori temporali, in secondi.</summary>
        public float CumulativeTimingError => sumErrors;
        /// <summary>Errore temporale medio; 0 se non ci sono intervalli valutabili.</summary>
        public float AverageTimingError => intervals.Count > 0 ? sumErrors / intervals.Count : 0f;
        /// <summary>Intervallo medio; 0 se non ci sono intervalli valutabili.</summary>
        public float AverageInterval => intervals.Count > 0 ? sumIntervals / intervals.Count : 0f;
        /// <summary>Qualità ritmica complessiva (media delle qualità); 0 se non ci sono intervalli.</summary>
        public float OverallRhythmQuality => intervals.Count > 0 ? Mathf.Clamp01(sumQualities / intervals.Count) : 0f;
        /// <summary>True se esiste almeno un intervallo valutabile.</summary>
        public bool HasEvaluableIntervals => intervals.Count > 0;
        /// <summary>Ultimo intervallo valutato, in secondi.</summary>
        public float LastInterval => lastInterval;
        /// <summary>Ultimo errore valutato, in secondi.</summary>
        public float LastError => lastError;
        /// <summary>Ultima qualità valutata, compresa tra 0 e 1.</summary>
        public float LastQuality => lastQuality;
        /// <summary>Esito dell'ultimo intervallo valutato.</summary>
        public ManDownCprIntervalOutcome LastOutcome => lastOutcome;
        /// <summary>Storico degli intervalli valutati.</summary>
        public IReadOnlyList<ManDownCprIntervalRecord> Intervals => intervals;

        /// <summary>
        /// True solo quando la respirazione è stata valutata come anomala: le compressioni eseguite su
        /// una persona che respira normalmente restano registrate ma non devono contribuire
        /// positivamente al futuro scoring. Nessuna penalità viene applicata in questo task.
        /// </summary>
        public bool IsApplicableForScoring => scenario != null && scenario.IsBreathingChecked && scenario.IsBreathingAbnormal;

        /// <summary>True se una pressione è in attesa della compressione completa che la validi.</summary>
        public bool HasPendingPressure => pendingPressureTimestamp >= 0f;
        /// <summary>True se esiste un riferimento temporale per il prossimo intervallo.</summary>
        public bool HasRhythmReference => lastCompletedPressureTimestamp >= 0f;
        /// <summary>True dopo un'interruzione esplicita della sequenza.</summary>
        public bool IsSequenceInterrupted => sequenceInterrupted;

        /// <summary>Un intervallo è stato valutato (timestamp di missione, intervallo, errore, qualità, esito).</summary>
        public event Action<float, float, float, float, ManDownCprIntervalOutcome> IntervalEvaluated;
        /// <summary>La sequenza ritmica è stata interrotta esplicitamente senza perdere le statistiche.</summary>
        public event Action SequenceInterrupted;
        /// <summary>Tutte le statistiche sono state azzerate a nuova missione o su richiesta.</summary>
        public event Action StatisticsReset;

        private void OnEnable()
        {
            if (cpr != null)
            {
                cpr.PressureDetected += OnPressureDetected;
                cpr.CompressionCompleted += OnCompressionCompleted;
                cpr.AttemptInvalidated += OnAttemptInvalidated;
            }
            MissionSession.Started += OnMissionStarted;
            UpdateSnapshot();
        }

        private void OnDisable()
        {
            if (cpr != null)
            {
                cpr.PressureDetected -= OnPressureDetected;
                cpr.CompressionCompleted -= OnCompressionCompleted;
                cpr.AttemptInvalidated -= OnAttemptInvalidated;
            }
            MissionSession.Started -= OnMissionStarted;
        }

        private void OnValidate()
        {
            minCorrectInterval = Mathf.Max(0f, minCorrectInterval);
            maxCorrectInterval = Mathf.Max(minCorrectInterval, maxCorrectInterval);
            idealInterval = Mathf.Clamp(idealInterval, minCorrectInterval, maxCorrectInterval);
            maxErrorTolerance = Mathf.Max(EPSILON, maxErrorTolerance);
        }

        /// <summary>
        /// Una pressione resta in attesa: solo la compressione completata dal rilascio la rende
        /// parte della sequenza ritmica. Non viene creato alcun intervallo in questo momento.
        /// </summary>
        private void OnPressureDetected(float timestamp)
        {
            pendingPressureTimestamp = timestamp;
        }

        /// <summary>
        /// Consuma la pressione in attesa. La prima completa imposta il riferimento; le successive
        /// producono un intervallo. Il timestamp del rilascio non viene mai usato nel calcolo.
        /// </summary>
        private void OnCompressionCompleted(float completionTimestamp)
        {
            if (pendingPressureTimestamp < 0f) return;

            float currentPressure = pendingPressureTimestamp;
            pendingPressureTimestamp = -1f;

            if (lastCompletedPressureTimestamp < 0f)
            {
                lastCompletedPressureTimestamp = currentPressure;
                sequenceInterrupted = false;
                UpdateSnapshot();
                return;
            }

            float previousPressure = lastCompletedPressureTimestamp;
            float interval = currentPressure - previousPressure;
            lastCompletedPressureTimestamp = currentPressure;
            EvaluateInterval(currentPressure, previousPressure, interval);
        }

        /// <summary>
        /// Un tentativo fisico invalido può aver lasciato una pressione senza compressione completa:
        /// quella pressione non entra nella sequenza. Il riferimento dell'ultima compressione completa
        /// NON viene azzerato, così uscire dalla zona non può evitare le penalità ritmiche.
        /// </summary>
        private void OnAttemptInvalidated(float timestamp, string reason)
        {
            pendingPressureTimestamp = -1f;
        }

        /// <summary>
        /// Interrompe la sequenza ritmica (per esempio prima di un futuro utilizzo del DAE): il
        /// riferimento temporale viene azzerato, ma tutte le statistiche restano disponibili.
        /// </summary>
        [ContextMenu("SI81/Interrupt Rhythm Sequence")]
        public void InterruptSequence()
        {
            pendingPressureTimestamp = -1f;
            lastCompletedPressureTimestamp = -1f;
            sequenceInterrupted = true;
            SequenceInterrupted?.Invoke();
            UpdateSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown CPR] Sequenza ritmica interrotta; statistiche conservate.", this);
        }

        /// <summary>Azzera intervalli, contatori, somme e riferimento: usato a nuova missione.</summary>
        [ContextMenu("SI81/Reset Rhythm Statistics")]
        public void ResetStatistics()
        {
            intervals.Clear();
            pendingPressureTimestamp = -1f;
            lastCompletedPressureTimestamp = -1f;
            sumIntervals = 0f;
            sumErrors = 0f;
            sumQualities = 0f;
            correctCount = 0;
            tooShortCount = 0;
            tooLongCount = 0;
            lastInterval = 0f;
            lastError = 0f;
            lastQuality = 0f;
            lastOutcome = ManDownCprIntervalOutcome.Correct;
            sequenceInterrupted = false;
            StatisticsReset?.Invoke();
            UpdateSnapshot();
            if (debugLog) Debug.Log("[SI81 ManDown CPR] Statistiche ritmiche azzerate.", this);
        }

        /// <summary>
        /// errore = max(0, minimo − intervallo, intervallo − massimo); qualità = clamp01(1 − errore/tolleranza).
        /// Un errore troppo veloce non compensa mai un errore troppo lento: si sommano gli errori,
        /// non gli scostamenti con segno. La qualità media non viene mai data per buona se non ci sono
        /// intervalli: in quel caso resta 0.
        /// </summary>
        private void EvaluateInterval(float pressureTimestamp, float previousPressure, float interval)
        {
            ManDownCprIntervalOutcome outcome;
            float error;
            if (interval < minCorrectInterval - EPSILON)
            {
                outcome = ManDownCprIntervalOutcome.TooShort;
                error = Mathf.Max(0f, minCorrectInterval - interval);
            }
            else if (interval > maxCorrectInterval + EPSILON)
            {
                outcome = ManDownCprIntervalOutcome.TooLong;
                error = Mathf.Max(0f, interval - maxCorrectInterval);
            }
            else
            {
                outcome = ManDownCprIntervalOutcome.Correct;
                error = 0f;
            }
            if (error < EPSILON)
            {
                error = 0f;
                outcome = ManDownCprIntervalOutcome.Correct;
            }

            float quality = Mathf.Clamp01(1f - error / Mathf.Max(EPSILON, maxErrorTolerance));
            intervals.Add(new ManDownCprIntervalRecord(pressureTimestamp, previousPressure,
                interval, error, quality, outcome));
            sumIntervals += interval;
            sumErrors += error;
            sumQualities += quality;
            switch (outcome)
            {
                case ManDownCprIntervalOutcome.Correct: correctCount++; break;
                case ManDownCprIntervalOutcome.TooShort: tooShortCount++; break;
                case ManDownCprIntervalOutcome.TooLong: tooLongCount++; break;
            }
            lastInterval = interval;
            lastError = error;
            lastQuality = quality;
            lastOutcome = outcome;

            IntervalEvaluated?.Invoke(pressureTimestamp, interval, error, quality, outcome);
            UpdateSnapshot();
            if (debugLog) Debug.Log($"[SI81 ManDown CPR] Intervallo {interval:F3}s err={error:F3} q={quality:F2} ({outcome})", this);
        }

        private void OnMissionStarted()
        {
            ResetStatistics();
        }

        /// <summary>Aggiorna lo snapshot serializzato letto nell'Inspector durante il Play Mode.</summary>
        private void UpdateSnapshot()
        {
            debugSnapshot = new ManDownCprRhythmSnapshot
            {
                completedCompressions = CompletedCompressionCount,
                evaluatedIntervals = intervals.Count,
                correctIntervals = correctCount,
                tooShortIntervals = tooShortCount,
                tooLongIntervals = tooLongCount,
                correctPercent = CorrectIntervalPercentage,
                cumulativeError = sumErrors,
                averageError = AverageTimingError,
                averageInterval = AverageInterval,
                rhythmQuality = OverallRhythmQuality,
                lastInterval = lastInterval,
                lastError = lastError,
                lastQuality = lastQuality,
                sequenceState = sequenceInterrupted ? "Interrupted" : HasRhythmReference ? "Active" : "Waiting",
                applicableForScoring = IsApplicableForScoring
            };
        }
    }
}
