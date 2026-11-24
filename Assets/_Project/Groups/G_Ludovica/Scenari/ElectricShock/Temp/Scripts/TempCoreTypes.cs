// [TEMP] Copie locali dei tipi della UI comune (SI81.UI), solo finché SI81.UI non è nel progetto.
// Quando arriva la UI comune: elimina la cartella Temp e sostituisci "using SI81.Ludovica.TempCore;" con "using SI81.UI;".
namespace SI81.Ludovica.TempCore
{
    /// <summary>Esito della missione (copia di SI81.UI.OutcomeType).</summary>
    public enum OutcomeType
    {
        /// <summary>Esito positivo.</summary>
        Positive,
        /// <summary>Esito parziale.</summary>
        Partial,
        /// <summary>Esito negativo.</summary>
        Negative
    }

    /// <summary>Tipo di feedback azione (copia di SI81.UI.FeedbackKind).</summary>
    public enum FeedbackKind
    {
        /// <summary>Azione corretta.</summary>
        Positive,
        /// <summary>Informazione neutra.</summary>
        Neutral,
        /// <summary>Azione sbagliata o rischiosa.</summary>
        Negative
    }

    /// <summary>Livello di difficoltà (copia di SI81.UI.DifficultyLevel).</summary>
    public enum DifficultyLevel
    {
        /// <summary>Livello 1 · Guided.</summary>
        Guided = 1,
        /// <summary>Livello 2 · Operational.</summary>
        Operational = 2,
        /// <summary>Livello 3 · Crisis.</summary>
        Crisis = 3
    }

    /// <summary>Prefissi delle righe di debrief (copia di SI81.UI.DebriefLinePrefix).</summary>
    public static class DebriefLinePrefix
    {
        /// <summary>Decisione corretta.</summary>
        public const string GOOD = "+ ";
        /// <summary>Decisione sbagliata o errore.</summary>
        public const string BAD = "- ";
        /// <summary>Errore critico.</summary>
        public const string CRITICAL = "! ";
    }
}
