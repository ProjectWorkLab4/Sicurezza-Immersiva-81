namespace SI81.UI
{
    /// <summary>Esito di una missione (brief p. 3): positivo, parziale, negativo.</summary>
    public enum OutcomeType
    {
        /// <summary>Esito positivo (color.outcome.positive).</summary>
        Positive,
        /// <summary>Esito parziale (color.outcome.partial, solo su card bianca).</summary>
        Partial,
        /// <summary>Esito negativo (color.outcome.negative, solo testo nero).</summary>
        Negative
    }

    /// <summary>Superficie su cui sta un'etichetta: determina i colori (token on-light / on-dark).</summary>
    public enum LabelSurface
    {
        /// <summary>Fondo chiaro (card bianca): testo nero, barre verdi.</summary>
        Light,
        /// <summary>Fondo scuro (vetro / pannello verde): testo bianco, barre verde chiaro.</summary>
        Dark
    }

    /// <summary>Le cinque dimensioni del punteggio sicurezza (brief p. 3).</summary>
    public enum DimensionKind
    {
        /// <summary>Safety.</summary>
        Safety,
        /// <summary>Awareness.</summary>
        Awareness,
        /// <summary>Decision.</summary>
        Decision,
        /// <summary>Execution.</summary>
        Execution,
        /// <summary>Time.</summary>
        Time
    }

    /// <summary>Accento di PANEL_StatTile: colore, icona e testo dipendono da questo valore.</summary>
    public enum StatAccent
    {
        /// <summary>Positivi (confirm).</summary>
        Positive,
        /// <summary>Parziali (brand.dark).</summary>
        Partial,
        /// <summary>Negativi (deny).</summary>
        Negative,
        /// <summary>Sessioni (tile.neutral).</summary>
        Neutral
    }

    /// <summary>Stato di un obiettivo (LBL_ObjectiveItem).</summary>
    public enum ObjectiveState
    {
        /// <summary>Completato: spunta piena, testo barrato.</summary>
        Done,
        /// <summary>Obiettivo corrente: sfondo brand.light, testo Bold.</summary>
        Current,
        /// <summary>Da fare: cerchio vuoto.</summary>
        Todo
    }

    /// <summary>Dimensione di LBL_Outcome: Small (intestazioni, card) o Large (HUD_OutcomePanel).</summary>
    public enum LabelSize
    {
        /// <summary>Alta 32, icona 24, testo Body Bold.</summary>
        Small,
        /// <summary>Alta 60, icona 32, testo Title Bold.</summary>
        Large
    }

    /// <summary>Come si presenta LBL_Outcome: a colori pieni (card bianca) o pillola bianca su intestazione vetro (CLAUDE.md 4.9).</summary>
    public enum OutcomePresentation
    {
        /// <summary>Colore pieno dell'esito: solo su card bianche.</summary>
        OnCard,
        /// <summary>Pillola bianca + icona colorata + testo nero: su intestazioni vetro.</summary>
        OnGlass
    }
}
