using System.Collections.Generic;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>Esito di una scelta registrata (specifica §11).</summary>
    public enum ChoiceOutcome
    {
        /// <summary>Scelta corretta.</summary>
        Correct,
        /// <summary>Scelta rischiosa.</summary>
        Risky,
        /// <summary>Errore critico.</summary>
        CriticalError,
        /// <summary>Passo mancato.</summary>
        Missed
    }

    /// <summary>Le 5 dimensioni comuni del punteggio, nell'ordine di EndMission.</summary>
    public enum ScoreDimension
    {
        /// <summary>Sicurezza.</summary>
        Safety = 0,
        /// <summary>Consapevolezza.</summary>
        Awareness = 1,
        /// <summary>Decisione.</summary>
        Decision = 2,
        /// <summary>Esecuzione.</summary>
        Execution = 3,
        /// <summary>Tempo.</summary>
        Time = 4
    }

    /// <summary>Una riga del Registro delle scelte: tempo, ID azione, esito, dimensione e valori di contesto.</summary>
    public class ChoiceLogEntry
    {
        /// <summary>Secondi dall'inizio dello scenario.</summary>
        public float Time;
        /// <summary>ID azione (LG-01...).</summary>
        public string ActionId;
        /// <summary>Esito.</summary>
        public ChoiceOutcome Outcome;
        /// <summary>Dimensione dello score.</summary>
        public ScoreDimension Dimension;
        /// <summary>Valori per i segnaposto della frase ({s}, {tipo}, {scelta}, {esito}).</summary>
        public readonly Dictionary<string, string> Context = new Dictionary<string, string>();
    }

    /// <summary>
    /// [TEMP / fallback §11.3] Registro delle scelte dello scenario. Va sostituito dal Registro delle scelte
    /// dello Score Manager comune quando esiste: stesso formato dati (tempo, ID, esito, dimensione, contesto).
    /// </summary>
    public class ChoiceLog
    {
        /// <summary>Punteggio di partenza di ogni dimensione.</summary>
        public const int BASE_SCORE = 60;

        private readonly List<ChoiceLogEntry> entries = new List<ChoiceLogEntry>();
        private readonly HashSet<string> recordedOnce = new HashSet<string>();

        /// <summary>Tutte le righe registrate, in ordine di inserimento.</summary>
        public IReadOnlyList<ChoiceLogEntry> Entries => entries;

        /// <summary>Registra una scelta.</summary>
        /// <param name="time">Secondi dall'inizio.</param>
        /// <param name="actionId">ID azione.</param>
        /// <param name="outcome">Esito.</param>
        /// <param name="dimension">Dimensione.</param>
        /// <returns>La riga creata (per aggiungere contesto in seguito).</returns>
        public ChoiceLogEntry Record(float time, string actionId, ChoiceOutcome outcome, ScoreDimension dimension)
        {
            var e = new ChoiceLogEntry { Time = time, ActionId = actionId, Outcome = outcome, Dimension = dimension };
            entries.Add(e);
            Debug.Log($"[SI81 ElectricShock] Scelta {FormatTime(time)} {actionId} {outcome} ({dimension})");
            return e;
        }

        /// <summary>Registra una scelta solo la prima volta per quell'ID.</summary>
        /// <returns>La riga creata, o null se l'ID era già registrato.</returns>
        public ChoiceLogEntry RecordOnce(float time, string actionId, ChoiceOutcome outcome, ScoreDimension dimension)
        {
            if (!recordedOnce.Add(actionId)) return null;
            return Record(time, actionId, outcome, dimension);
        }

        /// <summary>true se l'ID è già stato registrato.</summary>
        /// <param name="actionId">ID azione.</param>
        public bool Has(string actionId)
        {
            foreach (var e in entries) if (e.ActionId == actionId) return true;
            return false;
        }

        /// <summary>true se c'è almeno un errore critico.</summary>
        public bool HasCriticalError()
        {
            foreach (var e in entries) if (e.Outcome == ChoiceOutcome.CriticalError) return true;
            return false;
        }

        /// <summary>
        /// Calcola le 5 dimensioni 0-100. [PROVVISORIO] Ogni dimensione parte da 60: corretto +20, rischioso -20,
        /// mancato -25, errore critico -60. I pesi definitivi sono del Game System comune.
        /// </summary>
        /// <returns>Safety, Awareness, Decision, Execution, Time.</returns>
        public int[] ComputeDimensions()
        {
            int[] dims = { BASE_SCORE, BASE_SCORE, BASE_SCORE, BASE_SCORE, BASE_SCORE };
            foreach (var e in entries)
            {
                int delta = e.Outcome switch
                {
                    ChoiceOutcome.Correct => 20,
                    ChoiceOutcome.Risky => -20,
                    ChoiceOutcome.Missed => -25,
                    _ => -60
                };
                dims[(int)e.Dimension] += delta;
            }
            for (int i = 0; i < dims.Length; i++) dims[i] = Mathf.Clamp(dims[i], 0, 100);
            return dims;
        }

        /// <summary>Tempo in formato m:ss.</summary>
        /// <param name="seconds">Secondi.</param>
        public static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
