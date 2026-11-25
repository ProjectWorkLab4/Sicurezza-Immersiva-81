using System;
using System.Collections.Generic;

namespace SI81.UI
{
    /// <summary>
    /// Convenzione PROVVISORIA dei prefissi delle righe di debrief (MissionResult.DebriefLines): lo scenario li scrive,
    /// PANEL_Debrief li legge per scegliere l'icona (decisione corretta, sbagliata, errore critico).
    /// </summary>
    public static class DebriefLinePrefix
    {
        /// <summary>Decisione corretta: icona CheckCircle verde. Es. "+ Hai dato l'allarme → i colleghi sono usciti".</summary>
        public const string GOOD = "+ ";
        /// <summary>Decisione sbagliata o errore: icona Cancel rossa.</summary>
        public const string BAD = "- ";
        /// <summary>Errore critico: icona Warning rossa con prefisso "Critico".</summary>
        public const string CRITICAL = "! ";
    }

    /// <summary>
    /// Risultato di una sessione di gioco (prodotto a fine scenario, salvato da IProgressService).
    /// Il punteggio sicurezza è l'indice 0-100 delle cinque dimensioni (brief p. 3); le stelle sono il rank (ScoreRules).
    /// </summary>
    [Serializable]
    public class MissionResult
    {
        /// <summary>Numero dello scenario (MissionDefinition.Id).</summary>
        public string MissionId;
        /// <summary>Livello giocato.</summary>
        public DifficultyLevel Level = DifficultyLevel.Guided;
        /// <summary>Esito della missione.</summary>
        public OutcomeType Outcome;
        /// <summary>Punteggio sicurezza, 0-100.</summary>
        public int SafetyScore;
        /// <summary>Stelle 0-5 (rank), da ScoreRules.StarsFromScore.</summary>
        public int Stars;
        /// <summary>Cinque dimensioni 0-100 nell'ordine di DimensionKind (Safety, Awareness, Decision, Execution, Time).</summary>
        public int[] Dimensions = new int[5];
        /// <summary>Durata della sessione in secondi.</summary>
        public float DurationSeconds;
        /// <summary>Istante di fine (secondi Unix UTC).</summary>
        public long TimestampUtc;
        /// <summary>Righe del debrief (decisioni ed errori, già in italiano).</summary>
        public List<string> DebriefLines = new List<string>();
    }
}
