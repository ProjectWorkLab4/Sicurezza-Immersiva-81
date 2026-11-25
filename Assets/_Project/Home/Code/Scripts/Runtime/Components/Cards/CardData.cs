using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Dati di una BTN_ScenarioCard. Li compila chi mostra le card (Home) a partire da SO_Mission_* e dai progressi del
    /// giocatore; la card non legge mai altre fonti. Il punteggio 0–100 NON è qui: le card della Home mostrano solo le
    /// stelle (CLAUDE.md 4.6).
    /// </summary>
    public class ScenarioCardData
    {
        /// <summary>Identificativo dello scenario (es. "01"): viene restituito dall'evento di clic.</summary>
        public string Id;
        /// <summary>Titolo già formattato (es. "01 · Fire Starter", da ui.format.scenario_title). Ignorato se <see cref="TitleKey"/> è valorizzata.</summary>
        public string Title;
        /// <summary>Chiave di SO_UIStrings_It del titolo (opzionale, ha la precedenza su <see cref="Title"/>).</summary>
        public string TitleKey;
        /// <summary>Icona dello scenario (ICN_*, bianca, 44 sull'immagine).</summary>
        public Sprite Icon;
        /// <summary>Miniatura dello scenario. Null = segnaposto a gradiente.</summary>
        public Sprite Image;
        /// <summary>Colore iniziale del gradiente segnaposto (dato della missione). Null = colore di default del tema.</summary>
        public Color? GradientStart;
        /// <summary>Colore finale del gradiente segnaposto (dato della missione). Null = colore di default del tema.</summary>
        public Color? GradientEnd;
        /// <summary>Migliori stelle ottenute (1–5). Null = mai giocato: la pillola mostra "Nuovo".</summary>
        public int? BestStars;
        /// <summary>Complessità dello scenario, da 1 a 5.</summary>
        public int Complexity = 1;
        /// <summary>Livelli completati, da 0 a 3 (barre piene nell'icona dei livelli).</summary>
        public int LevelsCompleted;
        /// <summary>false = scenario non disponibile (variante Disabled: lucchetto, non cliccabile).</summary>
        public bool Unlocked = true;
    }
}
