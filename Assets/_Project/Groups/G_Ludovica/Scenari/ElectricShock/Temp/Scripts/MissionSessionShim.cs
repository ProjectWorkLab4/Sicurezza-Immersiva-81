using System;
using System.Collections.Generic;

namespace SI81.Ludovica.TempCore
{
    /// <summary>[TEMP] Risultato della missione (sottoinsieme di SI81.UI.MissionResult).</summary>
    public class MissionResultShim
    {
        /// <summary>Id della missione ("10").</summary>
        public string MissionId;
        /// <summary>Livello giocato.</summary>
        public DifficultyLevel Level;
        /// <summary>Esito.</summary>
        public OutcomeType Outcome;
        /// <summary>Punteggio sicurezza 0-100.</summary>
        public int SafetyScore;
        /// <summary>Dimensioni Safety, Awareness, Decision, Execution, Time.</summary>
        public int[] Dimensions;
        /// <summary>Durata in secondi.</summary>
        public float DurationSeconds;
        /// <summary>Righe di debrief con prefisso.</summary>
        public List<string> DebriefLines;
    }

    /// <summary>
    /// [TEMP] Sostituto di SI81.UI.MissionSession: tiene il livello della sessione in corso.
    /// </summary>
    public static class MissionSession
    {
        /// <summary>true tra Begin e End.</summary>
        public static bool IsActive { get; private set; }
        /// <summary>Livello della sessione.</summary>
        public static DifficultyLevel Level { get; private set; } = DifficultyLevel.Guided;
        /// <summary>Ultimo risultato chiuso.</summary>
        public static MissionResultShim LastResult { get; private set; }
        /// <summary>Chiamato alla chiusura della missione.</summary>
        public static event Action<MissionResultShim> Ended;

        /// <summary>Apre una sessione al livello indicato.</summary>
        /// <param name="level">Livello.</param>
        public static void Begin(DifficultyLevel level)
        {
            Level = level;
            IsActive = true;
            LastResult = null;
        }

        /// <summary>Chiude la sessione con il risultato.</summary>
        /// <param name="result">Risultato della missione.</param>
        public static void End(MissionResultShim result)
        {
            LastResult = result;
            IsActive = false;
            Ended?.Invoke(result);
        }

        /// <summary>Chiude la sessione senza risultato (riavvio).</summary>
        public static void Abort()
        {
            IsActive = false;
        }
    }
}
