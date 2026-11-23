using System;

namespace SI81.UI
{
    /// <summary>
    /// Criterio di completamento di uno scenario: che cosa significa chiudere con un certo esito
    /// (mostrato nel pannello laterale "Criteri di completamento" di Mission Brief). Il testo è un dato dello scenario.
    /// </summary>
    [Serializable]
    public class CompletionCriterion
    {
        /// <summary>Esito a cui si riferisce il criterio.</summary>
        public OutcomeType Outcome;
        /// <summary>Testo del criterio, in italiano (breve, una riga nel pannello laterale).</summary>
        public string Text;
    }
}
