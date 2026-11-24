using System;

namespace SI81.UI
{
    /// <summary>
    /// Impostazioni di uno scenario per un livello di difficoltà (elemento di MissionDefinition.Levels).
    /// </summary>
    [Serializable]
    public class MissionLevel
    {
        /// <summary>Livello a cui si riferiscono le impostazioni.</summary>
        public DifficultyLevel Level = DifficultyLevel.Guided;
        /// <summary>true se il livello è selezionabile dall'inizio (false = bloccato finché non si sblocca).</summary>
        public bool UnlockedByDefault = true;
        /// <summary>Tempo limite in secondi che sostituisce quello di LevelConfig (0 = usa quello del livello).</summary>
        public int TimeLimitSecondsOverride = 0;
    }
}
