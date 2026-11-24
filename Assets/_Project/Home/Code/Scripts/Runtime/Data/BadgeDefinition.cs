using System;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>Regola di sblocco di un badge, valutata da IProgressService sui risultati salvati (valori PROVVISORI).</summary>
    public enum BadgeRule
    {
        /// <summary>Almeno una missione conclusa.</summary>
        FirstMission = 0,
        /// <summary>Almeno 3 scenari diversi giocati.</summary>
        ThreeScenarios = 1,
        /// <summary>Almeno un risultato da 4 stelle o più.</summary>
        FourStars = 2,
        /// <summary>Almeno 5 esiti positivi.</summary>
        FivePositives = 3,
        /// <summary>Un esito non negativo al livello Operational.</summary>
        OperationalCompleted = 4,
        /// <summary>Un esito non negativo al livello Crisis.</summary>
        CrisisCompleted = 5,
        /// <summary>Un risultato da 5 stelle.</summary>
        FiveStars = 6,
        /// <summary>Esito positivo in meno di 180 secondi.</summary>
        FastRun = 7,
        /// <summary>Almeno 10 sessioni giocate.</summary>
        TenSessions = 8,
        /// <summary>Tutti e 3 i livelli conclusi in uno stesso scenario.</summary>
        AllLevelsOneScenario = 9,
        /// <summary>Una dimensione del punteggio a 95 o più.</summary>
        PerfectDimension = 10,
        /// <summary>Tutti gli scenari del catalogo giocati almeno una volta.</summary>
        AllScenarios = 11
    }

    /// <summary>
    /// Definizione di un badge (dato del catalogo): titolo, descrizione, icona piena e regola di sblocco.
    /// </summary>
    [Serializable]
    public class BadgeDefinition
    {
        /// <summary>Identificativo unico (snake_case inglese).</summary>
        public string Id;
        /// <summary>Titolo mostrato (italiano).</summary>
        public string Title;
        /// <summary>Descrizione di come si ottiene (italiano).</summary>
        public string Description;
        /// <summary>Icona piena (ICN_*_Filled) mostrata da BTN_Badge sbloccato.</summary>
        public Sprite Icon;
        /// <summary>Regola di sblocco.</summary>
        public BadgeRule Rule;
    }
}
