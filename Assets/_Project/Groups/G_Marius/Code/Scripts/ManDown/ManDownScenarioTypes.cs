using System;
using System.Collections.Generic;
using SI81.UI;

namespace SI81.ManDown
{
    /// <summary>
    /// Fase logica dello scenario Man Down. Le fasi non sono una sequenza obbligatoria: le azioni possono essere
    /// tentate in ordine diverso e corrette. Servono a esporre lo stato corrente alle future classi e alla UI comune.
    /// </summary>
    public enum ManDownPhase
    {
        /// <summary>Scenario non ancora inizializzato.</summary>
        NotStarted,
        /// <summary>Inizializzazione: condizioni casuali, timer e registro azioni.</summary>
        Initialization,
        /// <summary>Valutazione dello stato di coscienza.</summary>
        ConsciousnessAssessment,
        /// <summary>Valutazione della respirazione.</summary>
        BreathingAssessment,
        /// <summary>Assistenza nel ramo con respirazione normale.</summary>
        Assistance,
        /// <summary>RCP nel ramo con respirazione anormale.</summary>
        Cpr,
        /// <summary>Utilizzo del DAE, se disponibile.</summary>
        Aed,
        /// <summary>Ripresa della RCP dopo la defibrillazione.</summary>
        CprResume,
        /// <summary>Fase finale di assistenza prima della conclusione.</summary>
        FinalAssistance,
        /// <summary>Scenario concluso: dati congelati, nessuna nuova azione accettata.</summary>
        Concluded
    }

    /// <summary>
    /// Fase della RCP a cui si riferisce un risultato inviato dalla futura meccanica di rilevamento.
    /// Le soglie di compressioni valide sono 20 / 20 / 30, configurabili in <see cref="ManDownScenario"/>.
    /// </summary>
    public enum ManDownCprPhase
    {
        /// <summary>RCP iniziale con DAE disponibile (20 compressioni valide).</summary>
        InitialWithAed,
        /// <summary>RCP dopo la defibrillazione (20 compressioni valide).</summary>
        AfterDefibrillation,
        /// <summary>RCP senza DAE (30 compressioni valide complessive).</summary>
        WithoutAed
    }

    /// <summary>Come determinare la respirazione iniziale dell'NPC.</summary>
    public enum ManDownBreathingMode
    {
        /// <summary>Random con la probabilità configurata (70% anormale).</summary>
        Random,
        /// <summary>Forza respirazione normale (test).</summary>
        ForceNormal,
        /// <summary>Forza respirazione anormale (test).</summary>
        ForceAbnormal
    }

    /// <summary>
    /// Ramo della respirazione dopo la valutazione. Non duplica lo stato del controller: descrive solo
    /// la distinzione tra respirazione normale e respirazione assente o anomala, già presente in
    /// <see cref="ManDownScenario.IsBreathingAbnormal"/>. Prima della verifica il ramo è NotAssessed.
    /// </summary>
    public enum ManDownBreathingBranch
    {
        /// <summary>Respirazione non ancora valutata.</summary>
        NotAssessed,
        /// <summary>Ramo con respirazione normale (monitoraggio e posizione laterale).</summary>
        Normal,
        /// <summary>Ramo con respirazione assente o anomala (RCP).</summary>
        AbsentOrAbnormal
    }

    /// <summary>
    /// Posizione logica selezionata dal giocatore per l'NPC. È indipendente dalla posa visiva applicata
    /// da <see cref="ManDownPoseController"/>: una scelta può essere registrata anche se la posa non esiste.
    /// </summary>
    public enum ManDownPositionChoice
    {
        /// <summary>Nessuna scelta effettuata: posizione iniziale supina.</summary>
        None,
        /// <summary>Posizione laterale di sicurezza.</summary>
        SidePosition,
        /// <summary>Gambe sollevate.</summary>
        LegsRaised,
        /// <summary>Posizione prona.</summary>
        Prone,
        /// <summary>Posizione seduta.</summary>
        Seated
    }

    /// <summary>Esito decisionale di una scelta di posizione, valutato rispetto alla respirazione.</summary>
    public enum ManDownPositionOutcome
    {
        /// <summary>Laterale di sicurezza con respirazione normale: scelta corretta.</summary>
        Correct,
        /// <summary>Scelta errata oppure inappropriata per la respirazione rilevata.</summary>
        Incorrect
    }

    /// <summary>
    /// Tipo di una voce pronta per la futura timeline delle scelte del Debrief. I valori ricalcano
    /// le icone delle righe di debrief; la conversione a ChoiceKind avverrà quando l'API comune sarà
    /// disponibile. Non viene chiamato alcun metodo UI in questo task.
    /// </summary>
    public enum ManDownChoiceKind
    {
        /// <summary>Scelta corretta.</summary>
        Good,
        /// <summary>Scelta errata recuperabile.</summary>
        Bad,
        /// <summary>Scelta inappropriata con respirazione assente o anomala.</summary>
        Critical
    }

    /// <summary>
    /// Una scelta di posizione registrata dal controller. Conserva la posizione selezionata e quella
    /// precedente, il timestamp e la valutazione decisionale; non modifica la rappresentazione visiva.
    /// </summary>
    public sealed class ManDownPositionChoiceRecord
    {
        /// <summary>Posizione scelta dal giocatore.</summary>
        public ManDownPositionChoice SelectedPosition { get; }
        /// <summary>Posizione logica dell'NPC prima della scelta.</summary>
        public ManDownPositionChoice PreviousPosition { get; }
        /// <summary>Secondi dall'inizio della missione.</summary>
        public float Timestamp { get; }
        /// <summary>Fase logica al momento della scelta.</summary>
        public ManDownPhase Phase { get; }
        /// <summary>Correttezza della scelta rispetto alla respirazione conosciuta.</summary>
        public ManDownPositionOutcome Outcome { get; }
        /// <summary>True se la scelta corretta corregge un errore precedente ancora registrato.</summary>
        public bool IsCorrection { get; }
        /// <summary>Nota tecnica, non mostrata al giocatore.</summary>
        public string Note { get; }
        /// <summary>Azione in seconda persona per la futura timeline del Debrief (al massimo 60 caratteri).</summary>
        public string ActionText { get; }
        /// <summary>Conseguenza breve per la futura timeline del Debrief (al massimo 40 caratteri).</summary>
        public string ConsequenceText { get; }
        /// <summary>Tipo previsto per la futura timeline del Debrief.</summary>
        public ManDownChoiceKind Kind { get; }

        internal ManDownPositionChoiceRecord(
            ManDownPositionChoice selectedPosition,
            ManDownPositionChoice previousPosition,
            float timestamp,
            ManDownPhase phase,
            ManDownPositionOutcome outcome,
            bool isCorrection,
            string note,
            string actionText,
            string consequenceText,
            ManDownChoiceKind kind)
        {
            SelectedPosition = selectedPosition;
            PreviousPosition = previousPosition;
            Timestamp = timestamp;
            Phase = phase;
            Outcome = outcome;
            IsCorrection = isCorrection;
            Note = note;
            ActionText = actionText;
            ConsequenceText = consequenceText;
            Kind = kind;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{SelectedPosition} @ {Timestamp:F2}s (from={PreviousPosition}, outcome={Outcome}, correction={IsCorrection})";
        }
    }

    /// <summary>
    /// Una valutazione della respirazione. La prima è l'iniziale; le successive sono rivalutazioni.
    /// Ogni voce conserva il timestamp e la posizione logica dell'NPC al momento della verifica.
    /// </summary>
    public sealed class ManDownBreathingAssessmentRecord
    {
        /// <summary>Numero progressivo della valutazione nella partita (1 = iniziale).</summary>
        public int Sequence { get; }
        /// <summary>Secondi dall'inizio della missione.</summary>
        public float Timestamp { get; }
        /// <summary>true solo per la prima valutazione della partita.</summary>
        public bool IsInitial => Sequence == 1;
        /// <summary>true se la respirazione era assente o anomala.</summary>
        public bool Abnormal { get; }
        /// <summary>Posizione logica dell'NPC al momento della valutazione.</summary>
        public ManDownPositionChoice PositionAtAssessment { get; }
        /// <summary>true se la valutazione è avvenuta dopo un posizionamento corretto.</summary>
        public bool FollowsCorrectPosition { get; }
        /// <summary>
        /// true finché non viene effettuato un nuovo posizionamento: una rivalutazione non resta valida
        /// per una posizione diversa da quella in cui è stata fatta.
        /// </summary>
        public bool ValidForCurrentPosition { get; internal set; }

        internal ManDownBreathingAssessmentRecord(
            int sequence, float timestamp, bool abnormal, ManDownPositionChoice positionAtAssessment,
            bool followsCorrectPosition)
        {
            Sequence = sequence;
            Timestamp = timestamp;
            Abnormal = abnormal;
            PositionAtAssessment = positionAtAssessment;
            FollowsCorrectPosition = followsCorrectPosition;
            ValidForCurrentPosition = true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"#{Sequence} @ {Timestamp:F2}s (initial={IsInitial}, abnormal={Abnormal}, " +
                   $"position={PositionAtAssessment}, afterCorrect={FollowsCorrectPosition}, valid={ValidForCurrentPosition})";
        }
    }

    /// <summary>
    /// Dato grezzo già nel formato previsto da <c>RecordChoice</c>, da passare alla UI comune quando
    /// l'API sarà implementata. Non viene usato adesso per calcolare punteggi.
    /// </summary>
    public readonly struct ManDownChoiceData
    {
        /// <summary>Secondi dall'inizio della missione.</summary>
        public float ElapsedSeconds { get; }
        /// <summary>Azione in seconda persona, al massimo 60 caratteri.</summary>
        public string Action { get; }
        /// <summary>Conseguenza breve, al massimo 40 caratteri.</summary>
        public string Consequence { get; }
        /// <summary>Tipo della scelta per la timeline del Debrief.</summary>
        public ManDownChoiceKind Kind { get; }

        public ManDownChoiceData(float elapsedSeconds, string action, string consequence, ManDownChoiceKind kind)
        {
            ElapsedSeconds = elapsedSeconds;
            Action = action;
            Consequence = consequence;
            Kind = kind;
        }
    }

    /// <summary>Come determinare la disponibilità iniziale del DAE.</summary>
    public enum ManDownAedAvailabilityMode
    {
        /// <summary>Random con la probabilità configurata (75% presente).</summary>
        Random,
        /// <summary>Forza DAE presente (test).</summary>
        ForceAvailable,
        /// <summary>Forza DAE assente (test).</summary>
        ForceUnavailable
    }

    /// <summary>
    /// Una voce del registro azioni: cosa è stato tentato o completato e quando.
    /// </summary>
    public sealed class ManDownActionRecord
    {
        /// <summary>Identificativo tecnico dell'azione (costante di <see cref="ManDownScenario"/>).</summary>
        public string ActionId { get; }
        /// <summary>Fase dello scenario al momento della registrazione.</summary>
        public ManDownPhase Phase { get; }
        /// <summary>Secondi dall'inizio effettivo della missione.</summary>
        public float Timestamp { get; }
        /// <summary>true se l'azione è stata completata; false se solo tentata.</summary>
        public bool Completed { get; }
        /// <summary>true se l'azione è corretta/riuscita; false se errata o parziale.</summary>
        public bool Success { get; }
        /// <summary>Nota tecnica facoltativa (non mostrata al giocatore).</summary>
        public string Note { get; }

        internal ManDownActionRecord(string actionId, ManDownPhase phase, float timestamp, bool completed, bool success, string note)
        {
            ActionId = actionId;
            Phase = phase;
            Timestamp = timestamp;
            Completed = completed;
            Success = success;
            Note = note;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{ActionId} @ {Timestamp:F2}s (phase={Phase}, completed={Completed}, success={Success})";
        }
    }

    /// <summary>
    /// Risultato grezzo inviato dalla futura meccanica RCP. Il controller non rileva né valida le compressioni:
    /// registra i valori e li userà nel task di scoring.
    /// </summary>
    public readonly struct ManDownCprResult
    {
        /// <summary>Fase RCP a cui appartiene il risultato.</summary>
        public ManDownCprPhase Phase { get; }
        /// <summary>Compressioni considerate valide dalla futura meccanica.</summary>
        public int ValidCompressions { get; }
        /// <summary>Compressioni errate rilevanti (qualità).</summary>
        public int InvalidCompressions { get; }
        /// <summary>Frequenza media in compressioni al minuto (0 = non disponibile).</summary>
        public float AverageCompressionsPerMinute { get; }

        /// <summary>Crea un risultato RCP grezzo.</summary>
        public ManDownCprResult(ManDownCprPhase phase, int validCompressions, int invalidCompressions, float averageCompressionsPerMinute)
        {
            Phase = phase;
            ValidCompressions = Math.Max(0, validCompressions);
            InvalidCompressions = Math.Max(0, invalidCompressions);
            AverageCompressionsPerMinute = Math.Max(0f, averageCompressionsPerMinute);
        }
    }

    /// <summary>
    /// Fotografia congelata della conclusione: esito, punteggio provvisorio, cause, penalizzazioni e condizioni iniziali.
    /// Le future classi e i test leggono da qui senza dipendere dallo stato vivo del componente.
    /// </summary>
    public sealed class ManDownScenarioResult
    {
        /// <summary>Esito finale.</summary>
        public OutcomeType Outcome { get; }
        /// <summary>Punteggio sicurezza 0-100 usato per l'esito (provvisorio finché non arriva lo scoring definitivo).</summary>
        public int Score { get; }
        /// <summary>Cinque dimensioni grezze 0-100 (Safety, Awareness, Decision, Execution, Time).</summary>
        public int[] Dimensions { get; }
        /// <summary>Cause di fallimento registrate (anche più di una).</summary>
        public IReadOnlyList<string> FailureCauses { get; }
        /// <summary>Errori e omissioni con penalizzazione.</summary>
        public IReadOnlyList<string> Penalties { get; }
        /// <summary>true solo se il 112 è stato chiamato: i soccorsi possono essere simulati.</summary>
        public bool RescueSimulated { get; }
        /// <summary>Secondi dall'inizio effettivo della missione.</summary>
        public float ElapsedSeconds { get; }
        /// <summary>Respirazione iniziale dell'NPC (true = anormale).</summary>
        public bool BreathingAbnormal { get; }
        /// <summary>DAE disponibile nella partita.</summary>
        public bool AedAvailable { get; }
        /// <summary>Chiamata al 112 registrata.</summary>
        public bool EmergencyCalled { get; }
        /// <summary>true se il timer è scaduto.</summary>
        public bool TimedOut { get; }
        /// <summary>true se il monitoraggio finale è stato completato (ramo con respirazione normale).</summary>
        public bool FinalMonitoringCompleted { get; }
        /// <summary>Numero di valutazioni della respirazione registrate nella partita.</summary>
        public int BreathingAssessmentCount { get; }
        /// <summary>Timestamp dell'ultimo posizionamento corretto; -1 se assente.</summary>
        public float LastCorrectPositionAt { get; }
        /// <summary>
        /// true se era prevista una rivalutazione dopo il posizionamento corretto ma non è stata effettuata
        /// (dato grezzo per il futuro scoring; non modifica il punteggio provvisorio).
        /// </summary>
        public bool ReassessmentAfterCorrectPositionMissing { get; }

        internal ManDownScenarioResult(
            OutcomeType outcome,
            int score,
            int[] dimensions,
            List<string> failureCauses,
            List<string> penalties,
            bool rescueSimulated,
            float elapsedSeconds,
            bool breathingAbnormal,
            bool aedAvailable,
            bool emergencyCalled,
            bool timedOut,
            bool finalMonitoringCompleted,
            int breathingAssessmentCount,
            float lastCorrectPositionAt,
            bool reassessmentAfterCorrectPositionMissing)
        {
            Outcome = outcome;
            Score = score;
            Dimensions = dimensions;
            FailureCauses = failureCauses.ToArray();
            Penalties = penalties.ToArray();
            RescueSimulated = rescueSimulated;
            ElapsedSeconds = elapsedSeconds;
            BreathingAbnormal = breathingAbnormal;
            AedAvailable = aedAvailable;
            EmergencyCalled = emergencyCalled;
            TimedOut = timedOut;
            FinalMonitoringCompleted = finalMonitoringCompleted;
            BreathingAssessmentCount = breathingAssessmentCount;
            LastCorrectPositionAt = lastCorrectPositionAt;
            ReassessmentAfterCorrectPositionMissing = reassessmentAfterCorrectPositionMissing;
        }
    }
}
