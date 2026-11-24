using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// I tre livelli di difficoltà comuni a tutti gli scenari (brief: Guided, Operational, Crisis).
    /// Il valore numerico coincide con "Livello N".
    /// </summary>
    public enum DifficultyLevel
    {
        /// <summary>Livello 1 · Guided.</summary>
        Guided = 1,
        /// <summary>Livello 2 · Operational.</summary>
        Operational = 2,
        /// <summary>Livello 3 · Crisis.</summary>
        Crisis = 3
    }

    /// <summary>
    /// Stile visivo dei livelli di difficoltà (asset SO_DifficultyStyle): colori, chiavi di testo,
    /// barre dell'icona livello. Usato da LBL_LevelBadge, BTN_DifficultyCard, HUD_WristPanel.
    /// Generato da "SI81/UI/Build Theme" da Editor/Source/tokens_figma.json (unica fonte di verità; le icone assegnate restano).
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/UI/Difficulty Style", fileName = "SO_DifficultyStyle")]
    public class DifficultyStyle : ScriptableObject
    {
        /// <summary>Numero di barre dell'icona livello (3 barre "segnale").</summary>
        public const int BAR_COUNT = 3;
        /// <summary>Chiave del testo del badge: "Livello {0} · {1}".</summary>
        public const string BADGE_FORMAT_KEY = "ui.level.badge";

        /// <summary>Dati di un livello di difficoltà.</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>Livello (1, 2, 3).</summary>
            public DifficultyLevel Level = DifficultyLevel.Guided;
            /// <summary>Identificativo testuale inglese ("Guided", "Operational", "Crisis").</summary>
            public string Id;
            /// <summary>Chiave SO_UIStrings_It del nome del livello (es. "ui.level.guided").</summary>
            public string LabelKey;
            /// <summary>Chiave SO_UIStrings_It della descrizione (es. "ui.level.guided_desc").</summary>
            public string DescriptionKey;
            /// <summary>Colore del livello (spunta di selezione, accenti). Iniziale = UITheme.MISSING_COLOR finché il builder non lo scrive.</summary>
            public Color Color = UITheme.MISSING_COLOR;
            /// <summary>Colore del testo appoggiato sul colore del livello (Crisis: SOLO nero). Iniziale = UITheme.MISSING_COLOR.</summary>
            public Color TextOnColor = UITheme.MISSING_COLOR;
            /// <summary>Barre piene nell'icona livello (= numero del livello).</summary>
            public int BarsFilled = 1;
            /// <summary>Icona opzionale del livello. Vuota per ora: l'icona standard sono le 3 barre.</summary>
            public Sprite Icon;

            /// <summary>Numero del livello (1–3), come in "Livello N".</summary>
            public int Number => (int)Level;
        }

        [SerializeField] private List<Entry> levels = new List<Entry>();

        // I valori iniziali sono UITheme.MISSING_COLOR / 0: i dati veri arrivano SOLO da SI81/UI/Build Theme
        // (token color.level.bar.* e stroke.level-bar di tokens_figma.json). Modifiche nell'Inspector perse a ogni build.
        [Header("Barre dell'icona livello (stesso verde per tutti i livelli; da tokens_figma.json)")]
        [Tooltip("Barre piene su fondo chiaro (token color.level.bar.on-light).")]
        [SerializeField] private Color barOnLight = UITheme.MISSING_COLOR;
        [Tooltip("Barre vuote su fondo chiaro, solo contorno (token color.level.bar.off-light).")]
        [SerializeField] private Color barOffLight = UITheme.MISSING_COLOR;
        [Tooltip("Barre piene su fondo scuro (token color.level.bar.on-dark).")]
        [SerializeField] private Color barOnDark = UITheme.MISSING_COLOR;
        [Tooltip("Barre vuote su fondo scuro, solo contorno (token color.level.bar.off-dark).")]
        [SerializeField] private Color barOffDark = UITheme.MISSING_COLOR;
        [Tooltip("Spessore del contorno delle barre vuote (token stroke.level-bar).")]
        [SerializeField] private float barStroke;

        /// <summary>Tutti i livelli, in ordine 1–3.</summary>
        public IReadOnlyList<Entry> Levels => levels;
        /// <summary>Colore barre piene su fondo chiaro.</summary>
        public Color BarOnLight => barOnLight;
        /// <summary>Colore barre vuote su fondo chiaro.</summary>
        public Color BarOffLight => barOffLight;
        /// <summary>Colore barre piene su fondo scuro.</summary>
        public Color BarOnDark => barOnDark;
        /// <summary>Colore barre vuote su fondo scuro.</summary>
        public Color BarOffDark => barOffDark;
        /// <summary>Spessore contorno barre vuote.</summary>
        public float BarStroke => barStroke;

        /// <summary>
        /// Restituisce i dati di un livello. Null e un avviso se manca.
        /// </summary>
        /// <param name="level">Livello cercato.</param>
        /// <returns>I dati del livello, o null.</returns>
        public Entry Get(DifficultyLevel level)
        {
            foreach (Entry e in levels)
                if (e != null && e.Level == level) return e;
            Debug.LogWarning($"[SO_DifficultyStyle] Livello mancante: {level}.", this);
            return null;
        }

        /// <summary>
        /// Restituisce i dati di un livello dal suo identificativo ("Guided", "Operational", "Crisis").
        /// </summary>
        /// <param name="id">Identificativo inglese del livello (maiuscole ignorate).</param>
        /// <returns>I dati del livello, o null.</returns>
        public Entry Get(string id)
        {
            foreach (Entry e in levels)
                if (e != null && string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)) return e;
            Debug.LogWarning($"[SO_DifficultyStyle] Livello mancante: \"{id}\".", this);
            return null;
        }

        /// <summary>
        /// Colore di una barra dell'icona livello.
        /// </summary>
        /// <param name="filled">true se la barra è piena.</param>
        /// <param name="onDark">true se l'icona è su fondo scuro.</param>
        /// <returns>Il colore da usare.</returns>
        public Color GetBarColor(bool filled, bool onDark)
        {
            if (onDark) return filled ? barOnDark : barOffDark;
            return filled ? barOnLight : barOffLight;
        }

        /// <summary>
        /// Testo completo del badge, es. "Livello 2 · Operational", letto da SO_UIStrings_It.
        /// </summary>
        /// <param name="strings">Tabella dei testi.</param>
        /// <param name="level">Livello.</param>
        /// <returns>Testo del badge.</returns>
        public string GetBadgeText(UIStrings strings, DifficultyLevel level)
        {
            Entry e = Get(level);
            if (strings == null || e == null) return string.Empty;
            return strings.Format(BADGE_FORMAT_KEY, e.Number, strings.Get(e.LabelKey));
        }

        /// <summary>
        /// Sostituisce tutti i dati. Usato solo dal builder "SI81/UI/Build Theme".
        /// </summary>
        internal void SetData(List<Entry> newLevels, Color onLight, Color offLight, Color onDark, Color offDark, float stroke)
        {
            levels = newLevels;
            barOnLight = onLight;
            barOffLight = offLight;
            barOnDark = onDark;
            barOffDark = offDark;
            barStroke = stroke;
        }
    }
}
