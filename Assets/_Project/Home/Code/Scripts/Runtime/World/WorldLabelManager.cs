using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Gestore delle etichette nel mondo (uno per scena/rig, vive in MNG_HUD). Tiene tre pool di istanze di HUD_RiskLabel,
    /// HUD_InteractionPrompt e HUD_ContextHelp e, circa 10 volte al secondo, sceglie per ogni <see cref="LabelAnchor"/> se
    /// mostrarlo: distanza 0,75–4 m dalla testa (UITheme.Hud), tipo consentito dal livello (HUDService.CurrentLevel:
    /// ShowRiskLabels, ShowContextHelp, ShowHighlights; gli inviti all'azione sempre), al massimo 3 etichette di rischio
    /// (le più vicine). Le etichette si rigirano verso la testa solo quando appaiono o se l'angolo si discosta oltre la soglia;
    /// a ogni frame si aggiornano solo posizione e scala delle poche etichette attive. Nessuna allocazione a runtime.
    /// Uso: nessuna configurazione in scena, basta MNG_HUD; gli scenari aggiungono solo <see cref="LabelAnchor"/> agli oggetti.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/World Label Manager")]
    public class WorldLabelManager : MonoBehaviour
    {
        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Testa del giocatore (assegnata dal rig con SetRig; se vuota si usa quella di MNG_UI o Camera.main).")]
        [SerializeField] private Transform head;

        [Header("Prefab (assegnati dal builder)")]
        [Tooltip("Prefab HUD_RiskLabel.")]
        [SerializeField] private WorldLabel riskPrefab;
        [Tooltip("Prefab HUD_InteractionPrompt.")]
        [SerializeField] private WorldLabel interactionPrefab;
        [Tooltip("Prefab HUD_ContextHelp.")]
        [SerializeField] private WorldLabel contextPrefab;

        // Istanza unica per scena (per HUDService.SetVisible e per gli scenari).
        private static WorldLabelManager instance;

        // Pool per tipo (indice = LabelKind).
        private readonly WorldLabel[][] pools = new WorldLabel[3][];
        // Ancore scelte a ogni passaggio per tipo, e relative distanze (riusate: nessuna allocazione).
        private readonly LabelAnchor[][] picked = new LabelAnchor[3][];
        private readonly float[][] pickedDistance = new float[3][];
        private readonly int[] pickedCount = new int[3];

        // Parametri letti dal tema una volta.
        private float tickInterval;
        private float yawThreshold;
        // Tempo trascorso dall'ultimo passaggio (tempo non scalato: funziona anche in pausa).
        private float sinceTick;
        // Testa risolta.
        private Transform resolvedHead;
        // false = tutte le etichette nascoste (HUD nascosta: pausa, esito).
        private bool visible = true;
        // true dopo la creazione dei pool.
        private bool built;

        /// <summary>Gestore attivo (null se non c'è MNG_HUD in scena).</summary>
        public static WorldLabelManager Instance => instance;

        /// <summary>true se le etichette possono comparire.</summary>
        public bool Visible => visible;

        /// <summary>
        /// Registra l'istanza e crea i pool.
        /// </summary>
        private void Awake()
        {
            instance = this;
            BuildPools();
        }

        /// <summary>
        /// Libera l'istanza statica e le etichette create.
        /// </summary>
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            for (int kind = 0; kind < pools.Length; kind++)
            {
                if (pools[kind] == null) continue;
                for (int i = 0; i < pools[kind].Length; i++)
                    if (pools[kind][i] != null) Destroy(pools[kind][i].gameObject);
            }
        }

        /// <summary>
        /// Azzera l'istanza statica a ogni avvio (Domain Reload disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            instance = null;
        }

        /// <summary>
        /// Indica la testa del giocatore (dal rig condiviso).
        /// </summary>
        /// <param name="newHead">Transform della camera del rig.</param>
        public void SetRig(Transform newHead)
        {
            head = newHead;
            resolvedHead = null;
        }

        /// <summary>
        /// Mostra o nasconde tutte le etichette nel mondo (HUDService.SetVisible lo chiama quando nasconde la HUD).
        /// </summary>
        /// <param name="show">false = nasconde subito tutte le etichette attive e non ne mostra altre.</param>
        public void SetVisible(bool show)
        {
            visible = show;
            if (show) return;
            for (int kind = 0; kind < pools.Length; kind++)
            {
                if (pools[kind] == null) continue;
                for (int i = 0; i < pools[kind].Length; i++)
                    if (pools[kind][i] != null && !pools[kind][i].IsFree) pools[kind][i].Hide();
            }
        }

        /// <summary>
        /// Crea i tre pool di istanze (dimensioni da UITheme.Hud e dai token WorldTokens) e legge i parametri del tema.
        /// </summary>
        private void BuildPools()
        {
            if (built || theme == null) return;
            built = true;
            tickInterval = Mathf.Max(0.02f, theme.GetNumber(WorldTokens.LABEL_TICK));
            yawThreshold = theme.GetNumber(WorldTokens.LABEL_YAW_THRESHOLD);
            BuildPool(LabelKind.Risk, riskPrefab, theme.Hud.MaxRiskLabels);
            BuildPool(LabelKind.Interaction, interactionPrefab, Mathf.RoundToInt(theme.GetNumber(WorldTokens.MAX_INTERACTION_PROMPTS)));
            BuildPool(LabelKind.ContextHelp, contextPrefab, Mathf.RoundToInt(theme.GetNumber(WorldTokens.MAX_CONTEXT_HELPS)));
        }

        /// <summary>
        /// Crea il pool di un tipo: istanze spente, libere, fuori dalla gerarchia del rig (restano ferme nel mondo).
        /// </summary>
        private void BuildPool(LabelKind kind, WorldLabel prefab, int size)
        {
            int index = (int)kind;
            size = Mathf.Max(0, size);
            pools[index] = new WorldLabel[size];
            picked[index] = new LabelAnchor[size];
            pickedDistance[index] = new float[size];
            if (prefab == null) return;
            for (int i = 0; i < size; i++)
            {
                WorldLabel label = Instantiate(prefab);
                label.name = prefab.name + "_" + (i + 1).ToString("00");
                label.gameObject.SetActive(false);
                pools[index][i] = label;
            }
        }

        /// <summary>
        /// A ogni frame aggiorna posizione e scala delle etichette attive; circa 10 volte al secondo rifà la scelta.
        /// </summary>
        private void Update()
        {
            if (!built) BuildPools();
            if (resolvedHead == null) resolvedHead = WorldPlacement.ResolveHead(head, "WorldLabelManager");
            if (resolvedHead == null) return;

            sinceTick += Time.unscaledDeltaTime;
            if (sinceTick >= tickInterval)
            {
                sinceTick = 0f;
                if (visible) Tick();
                FaceActive(false);
            }
            FollowActive();
        }

        /// <summary>
        /// Aggiorna posizione e scala di tutte le etichette attive.
        /// </summary>
        private void FollowActive()
        {
            for (int kind = 0; kind < pools.Length; kind++)
            {
                WorldLabel[] pool = pools[kind];
                if (pool == null) continue;
                for (int i = 0; i < pool.Length; i++)
                {
                    WorldLabel label = pool[i];
                    if (label != null && label.Anchor != null) label.Follow(resolvedHead);
                }
            }
        }

        /// <summary>
        /// Rigira verso la testa le etichette attive il cui angolo si è discostato oltre la soglia.
        /// </summary>
        /// <param name="force">true = le rigira tutte.</param>
        private void FaceActive(bool force)
        {
            for (int kind = 0; kind < pools.Length; kind++)
            {
                WorldLabel[] pool = pools[kind];
                if (pool == null) continue;
                for (int i = 0; i < pool.Length; i++)
                {
                    WorldLabel label = pool[i];
                    if (label != null && label.Anchor != null) label.Face(resolvedHead, yawThreshold, force);
                }
            }
        }

        /// <summary>
        /// Passaggio a 10 Hz: sceglie le ancore da mostrare per ogni tipo e aggiorna i pool.
        /// </summary>
        private void Tick()
        {
            LevelConfig level = HUDService.CurrentLevel;
            for (int kind = 0; kind < 3; kind++)
            {
                if (pools[kind] == null) continue;
                Collect((LabelKind)kind, level);
                Apply((LabelKind)kind);
            }
        }

        /// <summary>
        /// Sceglie le ancore più vicine di un tipo che soddisfano distanza e regole del livello (inserimento ordinato in un
        /// array di dimensione massima fissa).
        /// </summary>
        private void Collect(LabelKind kind, LevelConfig level)
        {
            int index = (int)kind;
            LabelAnchor[] best = picked[index];
            float[] bestDistance = pickedDistance[index];
            int count = 0;
            if (best.Length > 0 && KindAllowed(kind, level))
            {
                float min = theme.Hud.WorldLabelMinDistance;
                float max = theme.Hud.WorldLabelMaxDistance;
                var anchors = LabelAnchor.All;
                Vector3 headPosition = resolvedHead.position;
                for (int i = 0; i < anchors.Count; i++)
                {
                    LabelAnchor a = anchors[i];
                    if (a == null || a.Kind != kind || a.Suppressed) continue;
                    if (a.RequiresHighlight && (level == null || !level.ShowHighlights)) continue;
                    float distance = Vector3.Distance(headPosition, a.WorldPoint);
                    if (distance < min || distance > max) continue;
                    count = InsertSorted(best, bestDistance, count, a, distance);
                }
            }
            pickedCount[index] = count;
        }

        /// <summary>
        /// true se il livello consente il tipo: rischio e aiuto contestuale seguono LevelConfig, gli inviti all'azione sempre.
        /// Senza livello (HUD non ancora pronta) rischio e aiuto restano nascosti.
        /// </summary>
        private static bool KindAllowed(LabelKind kind, LevelConfig level)
        {
            switch (kind)
            {
                case LabelKind.Risk: return level != null && level.ShowRiskLabels;
                case LabelKind.ContextHelp: return level != null && level.ShowContextHelp;
                default: return true;
            }
        }

        /// <summary>
        /// Inserisce un'ancora nell'array ordinato per distanza crescente, scartando la più lontana se è pieno.
        /// </summary>
        /// <returns>Il nuovo numero di elementi.</returns>
        private static int InsertSorted(LabelAnchor[] list, float[] distances, int count, LabelAnchor anchor, float distance)
        {
            int capacity = list.Length;
            if (count == capacity && distance >= distances[count - 1]) return count;
            int position = count < capacity ? count : count - 1;
            while (position > 0 && distances[position - 1] > distance)
            {
                list[position] = list[position - 1];
                distances[position] = distances[position - 1];
                position--;
            }
            list[position] = anchor;
            distances[position] = distance;
            return count < capacity ? count + 1 : count;
        }

        /// <summary>
        /// Allinea il pool alla scelta: nasconde le etichette le cui ancore non sono più scelte, mostra quelle nuove.
        /// </summary>
        private void Apply(LabelKind kind)
        {
            int index = (int)kind;
            WorldLabel[] pool = pools[index];
            LabelAnchor[] chosen = picked[index];
            int count = pickedCount[index];

            // 1) Etichette attive la cui ancora non è più tra quelle scelte (o è stata distrutta): via.
            for (int i = 0; i < pool.Length; i++)
            {
                WorldLabel label = pool[i];
                if (label == null || label.IsFree || label.Anchor == null) continue;
                if (!Contains(chosen, count, label.Anchor)) label.Hide();
            }

            // 2) Ancore scelte senza etichetta: ne prende una libera. Se il pool è vuoto riprova al prossimo passaggio.
            for (int c = 0; c < count; c++)
            {
                LabelAnchor anchor = chosen[c];
                if (IsShown(pool, anchor)) continue;
                WorldLabel free = FindFree(pool);
                if (free == null) break;
                free.Bind(anchor);
                free.Follow(resolvedHead);
                free.Face(resolvedHead, yawThreshold, true);
                free.Show();
            }
        }

        /// <summary>
        /// true se l'ancora è nell'elenco delle scelte.
        /// </summary>
        private static bool Contains(LabelAnchor[] list, int count, LabelAnchor anchor)
        {
            for (int i = 0; i < count; i++)
                if (list[i] == anchor) return true;
            return false;
        }

        /// <summary>
        /// true se una delle etichette del pool è già legata all'ancora.
        /// </summary>
        private static bool IsShown(WorldLabel[] pool, LabelAnchor anchor)
        {
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].Anchor == anchor) return true;
            return false;
        }

        /// <summary>
        /// Prima etichetta libera del pool (null se sono tutte in uso).
        /// </summary>
        private static WorldLabel FindFree(WorldLabel[] pool)
        {
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].IsFree) return pool[i];
            return null;
        }

        /// <summary>
        /// Collega tema e prefab. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, WorldLabel risk, WorldLabel interaction, WorldLabel context)
        {
            theme = newTheme;
            riskPrefab = risk;
            interactionPrefab = interaction;
            contextPrefab = context;
        }
    }
}
