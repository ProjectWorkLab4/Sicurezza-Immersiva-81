using System;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>Miglior risultato di uno scenario (calcolato dai risultati salvati, non serializzato).</summary>
    [Serializable]
    public class MissionBest
    {
        /// <summary>Numero dello scenario.</summary>
        public string MissionId;
        /// <summary>Migliori stelle (null = mai giocato).</summary>
        public int? BestStars;
        /// <summary>Miglior punteggio sicurezza 0-100 (null = mai giocato).</summary>
        public int? BestScore;
        /// <summary>Livelli conclusi con esito non negativo, 0-3.</summary>
        public int LevelsCompleted;
        /// <summary>Ultimo livello giocato (null = mai giocato).</summary>
        public DifficultyLevel? LastPlayedLevel;
    }

    /// <summary>Totali per la scheda Riepilogo di Statistiche e per il Profilo.</summary>
    [Serializable]
    public class ProgressTotals
    {
        /// <summary>Sessioni con esito positivo.</summary>
        public int Positive;
        /// <summary>Sessioni con esito parziale.</summary>
        public int Partial;
        /// <summary>Sessioni con esito negativo.</summary>
        public int Negative;
        /// <summary>Sessioni totali.</summary>
        public int Sessions;
        /// <summary>Media dei migliori punteggi degli scenari giocati (0-100).</summary>
        public int AverageScore;
        /// <summary>Scenari giocati almeno una volta.</summary>
        public int ScenariosPlayed;
        /// <summary>Scenari giocabili del catalogo.</summary>
        public int ScenariosTotal;
    }

    /// <summary>Stato di un badge per il Profilo (BTN_Badge).</summary>
    [Serializable]
    public class BadgeInfo
    {
        /// <summary>Identificativo del badge.</summary>
        public string Id;
        /// <summary>Titolo mostrato.</summary>
        public string Title;
        /// <summary>Icona piena (usata quando sbloccato).</summary>
        public Sprite Icon;
        /// <summary>true se sbloccato.</summary>
        public bool Unlocked;
        /// <summary>Come si ottiene.</summary>
        public string Description;
    }
}
