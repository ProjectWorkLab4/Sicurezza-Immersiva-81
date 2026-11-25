using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Definizione di uno scenario (asset SO_Mission_&lt;Titolo&gt;). Ogni team ne crea uno per scenario e lo aggiunge a
    /// SO_MissionCatalog: Home, Mission Brief, Statistiche, HUD, Pausa, Debrief e Score &amp; Replay lo leggono da qui.
    /// I testi (titolo, contesto, obiettivi, criteri) sono dati dello scenario, non stanno in SO_UIStrings_It.
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/UI/Mission Definition", fileName = "SO_Mission_Template")]
    public class MissionDefinition : ScriptableObject
    {
        /// <summary>Lunghezza massima consigliata del contesto (caratteri).</summary>
        public const int MAX_CONTEXT_LENGTH = 280;
        /// <summary>Numero massimo di obiettivi.</summary>
        public const int MAX_OBJECTIVES = 4;

        /// <summary>Numero dello scenario nel catalogo: 01..12 (training: 00).</summary>
        [Tooltip("Numero dello scenario nel catalogo: 01..12 (training: 00).")]
        public string Id = "01";
        /// <summary>Titolo dello scenario (es. Fire Starter). Appare come 01 · Fire Starter.</summary>
        [Tooltip("Titolo dello scenario (es. Fire Starter). Appare come 01 · Fire Starter.")]
        public string Title;
        /// <summary>Contesto e ruolo del giocatore, massimo 280 caratteri.</summary>
        [TextArea(3, 8)]
        [Tooltip("Contesto e ruolo del giocatore, massimo 280 caratteri.")]
        public string Context;
        /// <summary>Icona ICN_ della card (bianca, sulla miniatura).</summary>
        [Tooltip("Icona ICN_ della card (bianca, sulla miniatura).")]
        public Sprite Icon;
        /// <summary>Miniatura facoltativa; se vuota la card usa il gradiente.</summary>
        [Tooltip("Miniatura facoltativa; se vuota la card usa il gradiente.")]
        public Sprite Image;
        /// <summary>Colore iniziale del gradiente della miniatura.</summary>
        [Tooltip("Colore iniziale del gradiente della miniatura.")]
        public Color GradientTop = new Color(0.78f, 0.25f, 0.16f, 1f);
        /// <summary>Colore finale del gradiente della miniatura.</summary>
        [Tooltip("Colore finale del gradiente della miniatura.")]
        public Color GradientBottom = new Color(0.95f, 0.60f, 0.29f, 1f);
        /// <summary>Complessità da 1 a 5.</summary>
        [Range(1, 5)]
        [Tooltip("Complessità da 1 a 5.")]
        public int Complexity = 1;
        /// <summary>Nome della scena Unity dello scenario (SCN_&lt;Titolo>), presente nelle Build Settings.</summary>
        [Tooltip("Nome della scena Unity dello scenario (SCN_<Titolo>), presente nelle Build Settings.")]
        public string SceneName;
        /// <summary>Obiettivi in ordine, massimo 4; sul pannello da polso ~30 caratteri per riga.</summary>
        [Tooltip("Obiettivi in ordine, massimo 4; sul pannello da polso ~30 caratteri per riga.")]
        public List<string> Objectives = new List<string>();
        /// <summary>Criteri di completamento per esito (Positivo, Parziale, Negativo).</summary>
        [Tooltip("Criteri di completamento per esito (Positivo, Parziale, Negativo).")]
        public List<CompletionCriterion> Criteria = new List<CompletionCriterion>();
        /// <summary>Impostazioni per livello (se mancano, tutti i livelli sono sbloccati).</summary>
        [Tooltip("Impostazioni per livello (se mancano, tutti i livelli sono sbloccati).")]
        public List<MissionLevel> Levels = new List<MissionLevel>();
        /// <summary>true solo per il Training comune (non è uno scenario giocabile dalla griglia).</summary>
        [Tooltip("true solo per il Training comune (non è uno scenario giocabile dalla griglia).")]
        public bool IsTraining;
        /// <summary>Nota per i progettisti (non mostrata nel gioco). I contenuti segnaposto sono marcati (esempio).</summary>
        [TextArea(1, 3)]
        [Tooltip("Nota per i progettisti (non mostrata nel gioco). I contenuti segnaposto sono marcati (esempio).")]
        public string DesignerNote;

        /// <summary>
        /// Impostazioni dello scenario per un livello.
        /// </summary>
        /// <param name="l">Livello richiesto.</param>
        /// <returns>La voce di <see cref="Levels"/>; se manca, una voce nuova sbloccata e senza tempo personalizzato.</returns>
        public MissionLevel GetLevel(DifficultyLevel l)
        {
            if (Levels != null)
                for (int i = 0; i < Levels.Count; i++)
                    if (Levels[i] != null && Levels[i].Level == l) return Levels[i];
            return new MissionLevel { Level = l };
        }

        /// <summary>
        /// Tempo limite in secondi per un livello: quello dello scenario se impostato, altrimenti quello del livello comune
        /// (solo se il timer a tempo del livello è attivo).
        /// </summary>
        /// <param name="l">Livello richiesto.</param>
        /// <param name="cfg">Configurazione comune del livello (può essere null).</param>
        /// <returns>Secondi (0 = nessun limite, si mostra il tempo trascorso).</returns>
        public int GetTimeLimit(DifficultyLevel l, LevelConfig cfg)
        {
            MissionLevel level = GetLevel(l);
            if (level.TimeLimitSecondsOverride > 0) return level.TimeLimitSecondsOverride;
            if (cfg != null && cfg.TimerEnabled) return Mathf.Max(0, cfg.DefaultTimeLimitSeconds);
            return 0;
        }

        /// <summary>
        /// Testo del criterio di completamento per un esito.
        /// </summary>
        /// <param name="outcome">Esito.</param>
        /// <returns>Il testo, o stringa vuota se non definito.</returns>
        public string GetCriterionText(OutcomeType outcome)
        {
            if (Criteria != null)
                for (int i = 0; i < Criteria.Count; i++)
                    if (Criteria[i] != null && Criteria[i].Outcome == outcome) return Criteria[i].Text ?? string.Empty;
            return string.Empty;
        }

        /// <summary>
        /// Titolo completo "NN · Titolo" come nelle schermate.
        /// </summary>
        /// <returns>Testo del titolo con il numero.</returns>
        public string GetFullTitle()
        {
            return string.IsNullOrEmpty(Id) ? Title : Id + " · " + Title;
        }
    }
}
