using System;
using SI81.UI;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Parametri dello scenario 10 per livello (specifica §5, §7, §9). Solo configurazione: la logica legge
    /// questi valori, la UI comune decide da sola cosa mostrare a ogni livello.
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/G_Ludovica/Electric Shock Settings", fileName = "SO_ElectricShockSettings")]
    public class ElectricShockSettings : ScriptableObject
    {
        /// <summary>Valori di un livello.</summary>
        [Serializable]
        public class LevelSettings
        {
            [Tooltip("Livello a cui si applicano i valori.")]
            public DifficultyLevel Level = DifficultyLevel.Guided;
            [Tooltip("Secondi dall'accensione della pozza all'esplosione (min, max).")]
            public Vector2 ExplosionDelay = new Vector2(60f, 90f);
            [Tooltip("Evidenziazione di PB-01 e degli estintori corretti.")]
            public bool HighlightObjects;
            [Tooltip("Etichetta dell'estintore letta automaticamente quando lo si afferra.")]
            public bool AutoReadLabel;
            [Tooltip("Rack degli estintori in ordine casuale.")]
            public bool RandomizeRack;
            [Tooltip("Gonfiore del serbatoio durante l'avviso di esplosione.")]
            public bool TankSwelling = true;
            [Tooltip("Anello di 7 m durante l'avviso di esplosione.")]
            public bool BlastRing;
            [Tooltip("Numero di colleghi (2 o 3; con 3 il terzo compare a caso).")]
            public int Bystanders = 2;
            [Tooltip("Numero di distrattori (colleghi che chiamano, allarme di fondo).")]
            public int Distractors;
            [Tooltip("Allarme di fondo e fumo che copre la pozza (Crisis).")]
            public bool BackgroundNoise;
            [Tooltip("Suggerimenti massimi (0 = nessuno, -1 = illimitati).")]
            public int MaxHints;
            [Tooltip("Secondi prima del primo suggerimento.")]
            public float FirstHintDelay = 20f;
        }

        [Header("Tempi comuni (s)")]
        [Tooltip("Accensione della pozza se il generatore non viene fermato (min, max dall'inizio).")]
        public Vector2 IgnitionTime = new Vector2(45f, 75f);
        [Tooltip("Ritardo tra l'arresto del generatore e la scintilla sulla pozza.")]
        public float IgnitionAfterStop = 2f;
        [Tooltip("Anticipo dell'avviso di esplosione.")]
        public float ExplosionWarning = 12f;
        [Tooltip("Raggio della vampata (m).")]
        public float BlastRadius = 7f;
        [Tooltip("Momento in cui i colleghi tentano di aiutare (min, max dall'inizio).")]
        public Vector2 BystanderAttemptTime = new Vector2(40f, 70f);
        [Tooltip("Esitazione dei colleghi dal grido alla camminata.")]
        public float BystanderHesitation = 8f;
        [Tooltip("Arrivo di PM-2 dopo la chiamata al 112 (min, max).")]
        public Vector2 ParamedicAfterCall = new Vector2(30f, 45f);
        [Tooltip("Arrivo di PM-2 senza chiamata.")]
        public float ParamedicWithoutCall = 90f;
        [Tooltip("Esposizione al fumo prima della penalità.")]
        public float HarmfulAirSeconds = 20f;
        [Tooltip("Tempo vicino al collega per 'restare con lui'.")]
        public float StayWithVictimSeconds = 10f;
        [Tooltip("Distanza massima per 'restare con lui' (m).")]
        public float StayDistance = 2f;
        [Tooltip("Tempo entro cui osservare la pozza (LG-02).")]
        public float ObservePoolDeadline = 30f;
        [Tooltip("Arresto 'impulsivo': prima di questi secondi senza aver osservato (LG-21).")]
        public float ImpulsiveStopTime = 5f;
        [Tooltip("Arresto entro questi secondi dopo aver osservato (LG-09).")]
        public float TimelyStopAfterObserve = 30f;
        [Tooltip("Distanza a cui il grido raggiunge le persone (m).")]
        public float ShoutRange = 15f;

        [Header("Livelli")]
        public LevelSettings[] Levels =
        {
            new LevelSettings { Level = DifficultyLevel.Guided, ExplosionDelay = new Vector2(90f, 120f), HighlightObjects = true, AutoReadLabel = true, BlastRing = true, Bystanders = 2, Distractors = 0, MaxHints = -1, FirstHintDelay = 20f },
            new LevelSettings { Level = DifficultyLevel.Operational, ExplosionDelay = new Vector2(60f, 90f), Bystanders = 2, Distractors = 1, MaxHints = 3, FirstHintDelay = 45f },
            new LevelSettings { Level = DifficultyLevel.Crisis, ExplosionDelay = new Vector2(45f, 70f), RandomizeRack = true, TankSwelling = false, Bystanders = 3, Distractors = 2, BackgroundNoise = true, MaxHints = 0, FirstHintDelay = 0f }
        };

        /// <summary>Valori del livello richiesto (Guided se mancano).</summary>
        /// <param name="level">Livello.</param>
        public LevelSettings For(DifficultyLevel level)
        {
            foreach (var l in Levels) if (l.Level == level) return l;
            return Levels.Length > 0 ? Levels[0] : new LevelSettings();
        }
    }
}
