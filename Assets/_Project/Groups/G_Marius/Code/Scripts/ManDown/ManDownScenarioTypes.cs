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
            bool timedOut)
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
        }
    }
}
