using System;
using System.Collections.Generic;

namespace SI81.UI
{
    /// <summary>
    /// Servizio dei progressi in memoria per la sandbox e le prove: nessun salvataggio, parte da dati di esempio
    /// coerenti con il mock di Figma (scenari 01, 02, 07, 09 giocati; 03, 04, 08, 10 nuovi: 9 sessioni, 5 positive,
    /// 2 parziali, 2 negative, media 70/100, 5 badge su 12). Se <c>seed</c> è false parte vuoto.
    /// </summary>
    public sealed class MockProgressService : ProgressServiceBase
    {
        // Secondi in un giorno (per sfalsare gli istanti dei risultati di esempio).
        private const long DAY = 86400L;

        /// <summary>
        /// Crea il servizio con i dati di esempio.
        /// </summary>
        /// <param name="catalog">Catalogo con i badge.</param>
        /// <param name="seed">true per partire con i risultati di esempio, false per partire vuoto (primo avvio).</param>
        public MockProgressService(MissionCatalog catalog, bool seed = true) : base(catalog)
        {
            if (seed) SetResults(BuildSampleResults());
        }

        /// <summary>
        /// Azzera i risultati e rimette i dati di esempio (utile ai pulsanti di debug della sandbox).
        /// </summary>
        public void ResetToSample()
        {
            SetResults(BuildSampleResults());
            RaiseChanged();
        }

        /// <summary>
        /// Elimina tutti i risultati (simula un profilo nuovo).
        /// </summary>
        public void Clear()
        {
            SetResults(null);
            RaiseChanged();
        }

        /// <summary>
        /// Costruisce i 9 risultati di esempio, dal più vecchio al più recente.
        /// </summary>
        /// <returns>Elenco di risultati.</returns>
        private static List<MissionResult> BuildSampleResults()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var list = new List<MissionResult>
            {
                // 01 Fire Starter: 4 tentativi, livello massimo 2, ultimo tentativo con le dimensioni di Figma (82/74/90/68/71).
                Make("01", DifficultyLevel.Guided, OutcomeType.Partial, new[] { 66, 60, 62, 58, 59 }, 310f, now - 12 * DAY,
                    "- Hai avvisato il coordinatore dopo 2 min → squadra arrivata tardi", "! Ti sei avvicinato al fumo senza abbassarti → esposizione al fumo"),
                Make("01", DifficultyLevel.Guided, OutcomeType.Positive, new[] { 76, 70, 74, 72, 78 }, 260f, now - 10 * DAY,
                    "+ Hai dato l'allarme subito → i colleghi sono usciti in tempo"),
                Make("01", DifficultyLevel.Operational, OutcomeType.Positive, new[] { 90, 82, 88, 84, 86 }, 245f, now - 7 * DAY,
                    "+ Hai scelto l'estintore CO2 → nessun rischio elettrico", "+ Hai dato l'allarme subito → i colleghi sono usciti in tempo"),
                Make("01", DifficultyLevel.Operational, OutcomeType.Positive, new[] { 82, 74, 90, 68, 71 }, 288f, now - 1 * DAY,
                    "+ Hai scelto l'estintore CO2 → nessun rischio elettrico", "- Non hai chiuso la porta dell'ufficio → il fumo è arrivato in corridoio"),
                // 02 Blackout: un fallimento e un esito positivo (3 stelle).
                Make("02", DifficultyLevel.Guided, OutcomeType.Negative, new[] { 48, 42, 40, 44, 46 }, 400f, now - 9 * DAY,
                    "! Hai usato la fiamma libera al buio → rischio incendio"),
                Make("02", DifficultyLevel.Guided, OutcomeType.Positive, new[] { 76, 70, 72, 68, 74 }, 330f, now - 5 * DAY,
                    "+ Hai usato la torcia d'emergenza → percorso sicuro"),
                // 07 Forklift Chaos: negativo e poi parziale (3 stelle, 64).
                Make("07", DifficultyLevel.Guided, OutcomeType.Negative, new[] { 42, 38, 40, 44, 36 }, 380f, now - 6 * DAY,
                    "! Hai attraversato la corsia del carrello → rischio investimento"),
                Make("07", DifficultyLevel.Guided, OutcomeType.Partial, new[] { 66, 60, 64, 68, 62 }, 350f, now - 3 * DAY,
                    "- Hai segnalato il pericolo in ritardo → area isolata tardi"),
                // 09 Evacuation: un solo esito positivo ma con punteggio basso (2 stelle, 58).
                Make("09", DifficultyLevel.Guided, OutcomeType.Positive, new[] { 62, 56, 58, 60, 54 }, 420f, now - 2 * DAY,
                    "- Hai dato l'allarme dopo un minuto → evacuazione lenta")
            };
            return list;
        }

        /// <summary>
        /// Crea un risultato di esempio (punteggio = media delle dimensioni).
        /// </summary>
        /// <param name="missionId">Scenario.</param>
        /// <param name="level">Livello.</param>
        /// <param name="outcome">Esito.</param>
        /// <param name="dimensions">Cinque dimensioni.</param>
        /// <param name="duration">Durata in secondi.</param>
        /// <param name="timestamp">Istante di fine (Unix UTC).</param>
        /// <param name="lines">Righe di debrief con prefisso (<see cref="DebriefLinePrefix"/>).</param>
        /// <returns>Il risultato.</returns>
        private static MissionResult Make(string missionId, DifficultyLevel level, OutcomeType outcome, int[] dimensions,
            float duration, long timestamp, params string[] lines)
        {
            return new MissionResult
            {
                MissionId = missionId,
                Level = level,
                Outcome = outcome,
                SafetyScore = ScoreRules.ScoreFromDimensions(dimensions),
                Dimensions = dimensions,
                DurationSeconds = duration,
                TimestampUtc = timestamp,
                DebriefLines = new List<string>(lines)
            };
        }
    }
}
