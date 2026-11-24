using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Catalogo degli scenari (asset SO_MissionCatalog). Un team aggiunge qui il proprio SO_Mission_&lt;Titolo&gt;
    /// e la scena SCN_&lt;Titolo&gt; alle Build Settings: il resto della UI si aggiorna da solo.
    /// Contiene anche le tre configurazioni di livello comuni e le definizioni dei badge.
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/UI/Mission Catalog", fileName = "SO_MissionCatalog")]
    public class MissionCatalog : ScriptableObject
    {
        /// <summary>Scenari giocabili, in ordine di numero (griglia di Home, lista di Statistiche).</summary>
        [Tooltip("Scenari giocabili, in ordine di numero (griglia di Home, lista di Statistiche).")]
        public List<MissionDefinition> Missions = new List<MissionDefinition>();
        /// <summary>Training comune (SO_Mission_Training).</summary>
        [Tooltip("Training comune (SO_Mission_Training).")]
        public MissionDefinition Training;
        /// <summary>SO_LevelConfig_Guided.</summary>
        [Tooltip("SO_LevelConfig_Guided.")]
        public LevelConfig Guided;
        /// <summary>SO_LevelConfig_Operational.</summary>
        [Tooltip("SO_LevelConfig_Operational.")]
        public LevelConfig Operational;
        /// <summary>SO_LevelConfig_Crisis.</summary>
        [Tooltip("SO_LevelConfig_Crisis.")]
        public LevelConfig Crisis;
        /// <summary>Badge del Profilo (12 previsti; la regola di sblocco è in BadgeDefinition.Rule).</summary>
        [Tooltip("Badge del Profilo (12 previsti; la regola di sblocco è in BadgeDefinition.Rule).")]
        public List<BadgeDefinition> Badges = new List<BadgeDefinition>();

        /// <summary>
        /// Cerca uno scenario per numero (anche il Training, se coincide).
        /// </summary>
        /// <param name="id">Numero dello scenario, es. "01".</param>
        /// <returns>Lo scenario o null.</returns>
        public MissionDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Missions != null)
                for (int i = 0; i < Missions.Count; i++)
                    if (Missions[i] != null && Missions[i].Id == id) return Missions[i];
            if (Training != null && Training.Id == id) return Training;
            return null;
        }

        /// <summary>
        /// Configurazione comune di un livello.
        /// </summary>
        /// <param name="l">Livello.</param>
        /// <returns>SO_LevelConfig_* corrispondente (può essere null se non assegnato).</returns>
        public LevelConfig GetLevelConfig(DifficultyLevel l)
        {
            switch (l)
            {
                case DifficultyLevel.Operational: return Operational;
                case DifficultyLevel.Crisis: return Crisis;
                default: return Guided;
            }
        }

        /// <summary>
        /// Scenari giocabili: tutti quelli di <see cref="Missions"/> che non sono Training e non sono null.
        /// </summary>
        public IEnumerable<MissionDefinition> Playable
        {
            get
            {
                if (Missions == null) yield break;
                for (int i = 0; i < Missions.Count; i++)
                    if (Missions[i] != null && !Missions[i].IsTraining) yield return Missions[i];
            }
        }
    }
}
