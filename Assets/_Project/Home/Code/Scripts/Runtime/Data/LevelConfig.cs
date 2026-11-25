using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Configurazione di un livello di difficoltà comune a tutti gli scenari (asset SO_LevelConfig_Guided / _Operational / _Crisis).
    /// Dice quali aiuti dell'HUD sono attivi e se il timer è a tempo limite. Letta da HUD_WristPanel, HUD_RiskLabel,
    /// HUD_ContextHelp, HUD_ActionFeedback e dagli scenari (CLAUDE.md §4.11). I valori iniziali sono PROVVISORI.
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/UI/Level Config", fileName = "SO_LevelConfig_Guided")]
    public class LevelConfig : ScriptableObject
    {
        /// <summary>Livello a cui si riferisce la configurazione.</summary>
        [Tooltip("Livello a cui si riferisce la configurazione.")]
        public DifficultyLevel Level = DifficultyLevel.Guided;
        /// <summary>Chiave SO_UIStrings_It del nome del livello (es. ui.level.guided).</summary>
        [Tooltip("Chiave SO_UIStrings_It del nome del livello (es. ui.level.guided).")]
        public string NameKey;
        /// <summary>Chiave SO_UIStrings_It della descrizione del livello (es. ui.level.guided_desc).</summary>
        [Tooltip("Chiave SO_UIStrings_It della descrizione del livello (es. ui.level.guided_desc).")]
        public string DescriptionKey;
        /// <summary>Evidenzia gli oggetti interattivi importanti.</summary>
        [Tooltip("Evidenzia gli oggetti interattivi importanti.")]
        public bool ShowHighlights;
        /// <summary>Mostra il suggerimento sul pannello da polso.</summary>
        [Tooltip("Mostra il suggerimento sul pannello da polso.")]
        public bool ShowHints;
        /// <summary>Mostra le etichette di rischio (HUD_RiskLabel, max 3).</summary>
        [Tooltip("Mostra le etichette di rischio (HUD_RiskLabel, max 3).")]
        public bool ShowRiskLabels;
        /// <summary>Mostra l'aiuto contestuale (HUD_ContextHelp).</summary>
        [Tooltip("Mostra l'aiuto contestuale (HUD_ContextHelp).")]
        public bool ShowContextHelp;
        /// <summary>Mostra gli obiettivi sul pannello da polso (HUD_WristPanel).</summary>
        [Tooltip("Mostra gli obiettivi sul pannello da polso (HUD_WristPanel).")]
        public bool ShowObjectivesOnWrist = true;
        /// <summary>true = timer a tempo limite (con avvisi a 60 s e 30 s); false = si mostra solo il tempo trascorso.</summary>
        [Tooltip("true = timer a tempo limite (con avvisi a 60 s e 30 s); false = si mostra solo il tempo trascorso.")]
        public bool TimerEnabled;
        /// <summary>Tempo limite predefinito in secondi (0 = nessun limite). Lo scenario può cambiarlo in MissionLevel.TimeLimitSecondsOverride.</summary>
        [Tooltip("Tempo limite predefinito in secondi (0 = nessun limite). Lo scenario può cambiarlo in MissionLevel.TimeLimitSecondsOverride.")]
        public int DefaultTimeLimitSeconds;
    }
}
