using UnityEngine;

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>
    /// Variabili di rigiocabilità del scenario 07. I valori vengono letti al primo avvio della missione: l'orario del
    /// Muletto 2, la posizione del secondo pallet, il numero di colleghi, lo stato di Marco e la posizione del rack.
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/Scenari/Forklift Chaos/Replay Config", fileName = "SO_Replay_ForkliftChaos")]
    public sealed class ForkliftChaosReplayConfig : ScriptableObject
    {
        [Header("Semplicità dei replay")]
        [Tooltip("Seed base; viene combinato con il numero di tentativi per variare le partite.")]
        public int seed = 7;
        [Tooltip("Se true la configurazione qui sotto vale per tutte le partite, senza casualità.")]
        public bool fixedReplay;

        [Header("Muletto 2")]
        public Vector2 m2DelayAfterCallGuided = new Vector2(60f, 120f);
        public Vector2 m2DelayAfterCallOperational = new Vector2(30f, 90f);
        [Tooltip("Secondi dall'urlo se non si chiama il 112.")]
        public float m2DelayWithoutCall = 150f;
        [Tooltip("Preavviso del clacson prima che il mezzo si muova.")]
        public float hornLeadSeconds = 6f;

        [Header("Colleghi")]
        public Vector2 colleagueArrivalAfterShout = new Vector2(60f, 90f);
        [Tooltip("Numero minimo di colleghi curiosi.")]
        public int colleagueCountMin = 2;
        [Tooltip("Numero massimo di colleghi curiosi.")]
        public int colleagueCountMax = 2;

        [Header("Marco")]
        [Tooltip("Se true Marco è sempre in apnea; se false la probabilità dipende dal livello.")]
        public bool forceApnea;
        [Range(0f, 1f)] public float apneaChanceGuided = 0f;
        [Range(0f, 1f)] public float apneaChanceOperational = 0.33f;
        [Range(0f, 1f)] public float apneaChanceCrisis = 0.5f;
        [Tooltip("Secondi dall'inizio in cui compare l'apnea.")]
        public float apneaAtSeconds = 150f;
        [Tooltip("Secondi entro cui iniziare RCP/DAE dopo l'apnea.")]
        public float cprDeadlineSeconds = 120f;

        [Header("Oggetti")]
        [Tooltip("Se true PA-02 è nella posizione alta (variante replay y=16,5).")]
        public bool pa02High;
        [Tooltip("Se true il rack RK-01 è a est dell'accesso.")]
        public bool rackEast;
    }
}
