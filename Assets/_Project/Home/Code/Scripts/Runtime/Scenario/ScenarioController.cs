using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace SI81.UI
{
    /// <summary>
    /// Componente base di ogni scenario (sta su MNG_Scenario in ogni SCN_&lt;Titolo&gt;). Collega lo scenario alla UI comune
    /// SENZA costruire UI: obiettivi, timer, avvisi, sottotitoli, decisioni e fine missione passano da HUDService,
    /// DecisionPanel e MissionSession. Il team scrive una classe che EREDITA da ScenarioController (o usa gli eventi
    /// UnityEvent di ScenarioTrigger) e chiama i metodi qui sotto.
    /// Avvio: se la scena è stata caricata dal lanciatore (Home → Mission Brief → Avvia) la sessione c'è già; se invece si preme
    /// Play direttamente nella scena dello scenario (solo Editor) il componente carica SCN_Core in additivo, inizia una sessione
    /// "Guided" con la missione assegnata e nasconde il menu: così ogni team prova la propria scena da sola.
    /// Fine: <see cref="EndMission"/> costruisce il MissionResult (stelle da ScoreRules) e chiama MissionSession.End; a tempo scaduto
    /// <see cref="OnTimerExpired"/> chiude con esito Negativo (sostituibile).
    /// Uso: 1) assegna SO_Mission_&lt;Titolo&gt;; 2) nei tuoi script chiama SetObjective / Alert / AskDecision; 3) chiudi con EndMission.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Scenari/Scenario Controller")]
    public class ScenarioController : MonoBehaviour
    {
        /// <summary>Fase "scansione" della HUD (hud.phase.scan).</summary>
        public const string PHASE_SCAN = "hud.phase.scan";
        /// <summary>Fase "decisione" della HUD (hud.phase.decision).</summary>
        public const string PHASE_DECISION = "hud.phase.decision";
        /// <summary>Fase "azione" della HUD (hud.phase.action).</summary>
        public const string PHASE_ACTION = "hud.phase.action";
        /// <summary>Fase "risoluzione" della HUD (hud.phase.resolution).</summary>
        public const string PHASE_RESOLUTION = "hud.phase.resolution";

        /// <summary>Punteggio dato di default quando il tempo scade (esito Negativo).</summary>
        public const int TIMEOUT_SCORE = 20;
        /// <summary>Secondi reali massimi di attesa di SCN_Core, MNG_UI e MNG_HUD all'avvio.</summary>
        private const float CORE_WAIT_SECONDS = 15f;
        /// <summary>Percorso di SCN_Core nel progetto (solo Editor, per la prova della scena da sola).</summary>
        private const string CORE_SCENE_PATH = "Assets/_Project/Home/Level/Scenes/SCN_Core.unity";

        [Header("Scenario")]
        [Tooltip("SO_Mission_<Titolo> di questo scenario (obiettivi, criteri, tempo). Obbligatorio.")]
        [SerializeField] protected MissionDefinition mission;

        [Tooltip("Punto di partenza del giocatore (MNG_SpawnPoint): all'avvio l'origine del rig viene portata qui (posizione e rotazione sul piano). Vuoto = il rig resta dove sta.")]
        [SerializeField] private Transform spawnPoint;

        [Header("Prova da Editor (solo se si preme Play in questa scena)")]
        [Tooltip("Livello usato quando la scena parte da sola, senza passare dalla Home.")]
        [SerializeField] private DifficultyLevel editorLevel = DifficultyLevel.Guided;
        [Tooltip("Se attivo, in Play Mode da sola la scena carica SCN_Core (rig, menu, HUD) in additivo.")]
        [SerializeField] private bool loadCoreInEditor = true;

        // true quando la scena è partita da sola (non dal lanciatore).
        private bool standalone = false;
        // true quando HUD e servizi sono pronti e lo scenario è partito.
        private bool running;
        // true dopo EndMission (una sola chiusura).
        private bool ended;
        // Obiettivo corrente (indice in mission.Objectives) e quanti sono completati.
        private int currentObjective;
        private int objectivesCompleted;
        // Istante di inizio (tempo di gioco, si ferma in pausa).
        private float startTime;
        // Pannello decisione trovato in scena (cache).
        private DecisionPanel decisionPanel;
        // Origine del rig spostata sul punto di partenza e posa originale da ripristinare alla chiusura.
        private Transform movedOrigin;
        private Vector3 originalOriginPosition;
        private Quaternion originalOriginRotation;

        /// <summary>Scenario assegnato.</summary>
        public MissionDefinition Mission => mission;
        /// <summary>Livello in corso.</summary>
        public DifficultyLevel Level => MissionSession.Level;
        /// <summary>true tra l'avvio e <see cref="EndMission"/>.</summary>
        public bool IsRunning => running && !ended;
        /// <summary>Indice dell'obiettivo corrente (0-based).</summary>
        public int CurrentObjectiveIndex => currentObjective;
        /// <summary>Numero di obiettivi completati.</summary>
        public int ObjectivesCompleted => objectivesCompleted;
        /// <summary>Numero di obiettivi dello scenario.</summary>
        public int ObjectiveCount => mission != null && mission.Objectives != null ? mission.Objectives.Count : 0;
        /// <summary>Secondi di gioco dall'avvio dello scenario.</summary>
        public float ElapsedSeconds => running ? Time.time - startTime : 0f;

        // =====================================================================
        // Avvio
        // =====================================================================

        /// <summary>
        /// Avvia lo scenario: prepara la sessione (da sola in Editor), attende MNG_UI e MNG_HUD e chiama <see cref="OnScenarioStarted"/>.
        /// </summary>
        private IEnumerator Start()
        {
            if (mission == null)
            {
                Debug.LogError("[SI81 Scenario] ScenarioController: manca SO_Mission_<Titolo>. Assegnalo nel componente su MNG_Scenario.", this);
                yield break;
            }

            bool launched = MissionSession.IsActive;
            if (!launched)
            {
#if UNITY_EDITOR
                standalone = true;
                if (loadCoreInEditor && UIScreenManager.Instance == null) yield return LoadCoreInEditor();
                MissionSession.Begin(mission, editorLevel, mission.IsTraining);
#else
                Debug.LogError("[SI81 Scenario] Nessuna sessione attiva: lo scenario va avviato dalla Home (SceneMissionLauncher).", this);
                yield break;
#endif
            }
            else if (MissionSession.Current != mission)
            {
                Debug.LogWarning($"[SI81 Scenario] La sessione in corso è \"{MissionSession.Current.name}\", ma questa scena usa \"{mission.name}\". Controlla SceneName della missione.", this);
            }

            // Attende servizi (MNG_UI) e HUD (MNG_HUD): nascono in SCN_Core.
            float waited = 0f;
            while ((!UIServices.IsReady || !HUDService.IsReady) && waited < CORE_WAIT_SECONDS)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!HUDService.IsReady)
                Debug.LogWarning("[SI81 Scenario] MNG_HUD non trovata: avvisi, sottotitoli e pannello da polso non compariranno. Apri la scena passando da SCN_Core.", this);

            if (standalone && UIScreenManager.Instance != null) UIScreenManager.Instance.HideMenu();

            MoveRigToSpawnPoint();

            // La HUD parte da sola al primo fotogramma dopo MissionSession.Begin: si lascia passare un fotogramma e si riallinea.
            yield return null;
            LevelConfig cfg = UIServices.Catalog != null ? UIServices.Catalog.GetLevelConfig(MissionSession.Level) : null;
            if (HUDService.IsReady && cfg != null) HUDService.ApplyLevel(cfg);
            MissionFlow.EnsureObjectiveState(ObjectiveCount);
            currentObjective = 0;
            objectivesCompleted = MissionFlow.CompletedObjectiveCount;
            if (ObjectiveCount > 0) SetObjective(0);
            SetPhase(PHASE_SCAN);

            HUDService.TimerExpired += HandleTimerExpired;
            startTime = Time.time;
            running = true;
            OnScenarioStarted();
        }

        /// <summary>
        /// Toglie l'iscrizione al timer.
        /// </summary>
        protected virtual void OnDestroy()
        {
            HUDService.TimerExpired -= HandleTimerExpired;
            if (decisionPanel != null && decisionPanel.IsOpen) decisionPanel.Close();
            if (movedOrigin != null)
            {
                movedOrigin.localPosition = originalOriginPosition;
                movedOrigin.localRotation = originalOriginRotation;
            }
        }

        /// <summary>
        /// Porta l'origine del tracciamento del rig sul punto di partenza (posizione e imbardata) solo se la scena non
        /// usa già <c>SI81.Rig.SpawnPoint</c>. Quando <c>SpawnPoint</c> è presente, è il componente del rig a spostare
        /// e ripristinare la radice del rig senza spostare il <c>TrackingOrigin</c> figlio (che altrimenti corromperebbe
        /// lo spawn degli scenari successivi e l'offset della modalità seduto).
        /// </summary>
        private void MoveRigToSpawnPoint()
        {
            if (spawnPoint == null || UIScreenManager.Instance == null) return;
            if (HasRigSpawnPoint()) return;
            Transform origin = UIScreenManager.Instance.TrackingOrigin;
            if (origin == null) return;
            movedOrigin = origin;
            originalOriginPosition = origin.localPosition;
            originalOriginRotation = origin.localRotation;
            origin.SetPositionAndRotation(spawnPoint.position, Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f));
        }

        /// <summary>
        /// True se lo spawnPoint assegnato o un oggetto della stessa scena monta già <c>SI81.Rig.SpawnPoint</c>.
        /// Controllo per nome di tipo per non introdurre una dipendenza da <c>SI81.UI</c> verso <c>SI81.Rig</c>.
        /// </summary>
        private bool HasRigSpawnPoint()
        {
            if (spawnPoint != null && HasRigSpawnPointComponent(spawnPoint.gameObject)) return true;
            UnityEngine.SceneManagement.Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) return false;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && HasRigSpawnPointComponent(roots[i])) return true;
            }
            return false;
        }

        private static bool HasRigSpawnPointComponent(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour b = behaviours[i];
                if (b != null && b.GetType().FullName == "SI81.Rig.SpawnPoint") return true;
            }
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Solo Editor: carica SCN_Core in additivo (rig, MNG_UI, MNG_HUD) e attende il caricamento.
        /// </summary>
        private IEnumerator LoadCoreInEditor()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CORE_SCENE_PATH) == null)
            {
                Debug.LogWarning("[SI81 Scenario] SCN_Core non trovata: esegui il menu SI81/Scenari/Crea kit scenario. La scena parte senza UI.", this);
                yield break;
            }
            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(CORE_SCENE_PATH, new LoadSceneParameters(LoadSceneMode.Additive));
            while (load != null && !load.isDone) yield return null;
        }
#endif

        // =====================================================================
        // Ganci da sovrascrivere
        // =====================================================================

        /// <summary>
        /// Chiamato quando sessione, HUD e primo obiettivo sono pronti: qui parte la logica dello scenario.
        /// </summary>
        protected virtual void OnScenarioStarted()
        {
        }

        /// <summary>
        /// Chiamato quando il tempo finisce. Il predefinito chiude la missione con esito Negativo (punteggio basso e riga di debrief).
        /// Sovrascrivere per un altro comportamento (es. esito Parziale se l'obiettivo principale è già raggiunto).
        /// </summary>
        protected virtual void OnTimerExpired()
        {
            var lines = new List<string> { DebriefLinePrefix.BAD + "Tempo scaduto prima di mettere in sicurezza l'area." };
            EndMission(OutcomeType.Negative, TIMEOUT_SCORE, null, lines);
        }

        /// <summary>
        /// Inoltra la scadenza del timer se la missione è ancora in corso.
        /// </summary>
        private void HandleTimerExpired()
        {
            if (!IsRunning) return;
            OnTimerExpired();
        }

        // =====================================================================
        // Obiettivi e fase
        // =====================================================================

        /// <summary>
        /// Imposta l'obiettivo corrente (indice in SO_Mission.Objectives): aggiorna il pannello da polso con contatore "n/N".
        /// </summary>
        /// <param name="index">Indice 0-based; viene limitato al numero di obiettivi.</param>
        public void SetObjective(int index)
        {
            int count = ObjectiveCount;
            if (count <= 0) return;
            currentObjective = Mathf.Clamp(index, 0, count - 1);
            MissionFlow.ObjectiveIndex = currentObjective;
            HUDService.SetObjectiveIndex(currentObjective);
        }

        /// <summary>
        /// Segna completato l'obiettivo corrente e passa al successivo (se c'è; l'ultimo resta mostrato).
        /// </summary>
        public void CompleteObjective()
        {
            if (!IsRunning) return;
            if (MissionFlow.HasObjectiveCompletionState)
                MissionFlow.SetObjectiveCompleted(currentObjective, true);
            objectivesCompleted = MissionFlow.HasObjectiveCompletionState
                ? MissionFlow.CompletedObjectiveCount
                : Mathf.Min(objectivesCompleted + 1, ObjectiveCount);
            if (currentObjective + 1 < ObjectiveCount) SetObjective(currentObjective + 1);
        }

        /// <summary>
        /// Segna completato un obiettivo specifico e porta il polso al primo obiettivo ancora da fare.
        /// Utile negli scenari in cui le azioni possono avvenire in ordine libero.
        /// </summary>
        /// <param name="index">Indice 0-based in MissionDefinition.Objectives.</param>
        public void SetObjectiveCompleted(int index)
        {
            if (!IsRunning || index < 0 || index >= ObjectiveCount)
                return;

            MissionFlow.EnsureObjectiveState(ObjectiveCount);
            MissionFlow.SetObjectiveCompleted(index, true);
            objectivesCompleted = MissionFlow.CompletedObjectiveCount;
            int next = MissionFlow.FirstIncompleteObjectiveIndex();
            SetObjective(next >= 0 ? next : ObjectiveCount - 1);
        }

        /// <summary>
        /// Imposta la fase mostrata sul polso e nella Pausa.
        /// </summary>
        /// <param name="phaseKey">Una delle costanti PHASE_* (hud.phase.scan, decision, action, resolution).</param>
        public void SetPhase(string phaseKey)
        {
            MissionFlow.CurrentPhaseKey = phaseKey;
            HUDService.SetPhase(phaseKey);
        }

        /// <summary>
        /// Imposta il suggerimento del livello Guided (vuoto = nasconde). Max ~60 caratteri.
        /// </summary>
        /// <param name="text">Testo del suggerimento.</param>
        public void SetHint(string text)
        {
            HUDService.SetHint(text);
        }

        // =====================================================================
        // Avvisi, feedback, sottotitoli
        // =====================================================================

        /// <summary>
        /// Avviso evento in basso nella vista (3 s, suono e aptica). Prima riga = titolo, seconda (dopo "\n") = dettaglio.
        /// </summary>
        /// <param name="text">Testo dell'avviso.</param>
        /// <param name="iconName">Nome icona ICN_ facoltativo (es. "ICN_Warning_Filled").</param>
        public void Alert(string text, string iconName = null)
        {
            HUDService.ShowEventAlert(text, iconName);
        }

        /// <summary>
        /// Feedback d'azione (priorità bassa, 3 s). Guided: sempre; Operational: solo errori; Crisis: mai.
        /// </summary>
        /// <param name="text">Testo breve (max ~60 caratteri).</param>
        /// <param name="kind">Positivo, neutro o negativo.</param>
        public void Feedback(string text, FeedbackKind kind)
        {
            HUDService.ShowActionFeedback(text, kind);
        }

        /// <summary>
        /// Sottotitolo con nome di chi parla (max 32 caratteri x 2 righe: i testi lunghi vanno in più pagine).
        /// </summary>
        /// <param name="speaker">Chi parla (vuoto = nessuno).</param>
        /// <param name="text">Testo.</param>
        /// <param name="seconds">Durata totale; 0 = automatica.</param>
        public void Subtitle(string speaker, string text, float seconds = 0f)
        {
            HUDService.ShowSubtitle(speaker, text, seconds);
        }

        // =====================================================================
        // Decisione
        // =====================================================================

        /// <summary>
        /// Apre HUD_DecisionPanel a 1,2 m davanti al giocatore con 2-4 opzioni; si sceglie col raggio.
        /// </summary>
        /// <param name="question">Domanda.</param>
        /// <param name="options">Testi delle opzioni (2-4, una riga ~34 caratteri).</param>
        /// <param name="onChosen">Chiamata con l'indice scelto (0-based) o -1 (DecisionPanel.TIMEOUT_INDEX) se il tempo scade.</param>
        /// <param name="timeoutSeconds">0 = nessun limite.</param>
        /// <returns>true se il pannello è stato aperto; false se MNG_HUD non ha HUD_DecisionPanel (errore in Console).</returns>
        public bool AskDecision(string question, string[] options, Action<int> onChosen, float timeoutSeconds = 0f)
        {
            if (decisionPanel == null) decisionPanel = FindFirstObjectByType<DecisionPanel>(FindObjectsInactive.Include);
            if (decisionPanel == null)
            {
                Debug.LogError("[SI81 Scenario] HUD_DecisionPanel non trovato: serve MNG_HUD in SCN_Core (apri la scena dal kit scenario).", this);
                return false;
            }
            decisionPanel.Show(question, options, onChosen, timeoutSeconds);
            return true;
        }

        // =====================================================================
        // Fine missione
        // =====================================================================

        /// <summary>
        /// Chiude la missione: costruisce il MissionResult (punteggio, stelle da ScoreRules, dimensioni, righe di debrief) e chiama
        /// MissionSession.End. Da quel momento la UI comune mostra esito, Debrief e Score &amp; Replay. Una sola chiamata è valida.
        /// </summary>
        /// <param name="outcome">Esito: Positive, Partial, Negative.</param>
        /// <param name="safetyScore">Punteggio sicurezza 0-100; se negativo si calcola come media delle dimensioni.</param>
        /// <param name="dimensions">Cinque valori 0-100 (Safety, Awareness, Decision, Execution, Time); null = tutti uguali al punteggio.</param>
        /// <param name="debriefLines">Righe del debrief in italiano, con prefisso DebriefLinePrefix (GOOD "+ ", BAD "- ", CRITICAL "! ").</param>
        public void EndMission(OutcomeType outcome, int safetyScore, int[] dimensions, List<string> debriefLines)
        {
            if (ended) return;
            ended = true;
            HUDService.StopTimer();
            if (decisionPanel != null && decisionPanel.IsOpen) decisionPanel.Close();

            int[] dims = new int[ScoreRules.DIMENSION_COUNT];
            if (dimensions != null)
                for (int i = 0; i < dims.Length && i < dimensions.Length; i++) dims[i] = Mathf.Clamp(dimensions[i], 0, 100);
            int score = safetyScore >= 0 ? Mathf.Clamp(safetyScore, 0, 100) : ScoreRules.ScoreFromDimensions(dims);
            if (dimensions == null)
                for (int i = 0; i < dims.Length; i++) dims[i] = score;

            var result = new MissionResult
            {
                MissionId = mission != null ? mission.Id : null,
                Level = MissionSession.Level,
                Outcome = outcome,
                SafetyScore = score,
                Stars = ScoreRules.StarsFromScore(score),
                Dimensions = dims,
                DurationSeconds = ElapsedSeconds,
                DebriefLines = debriefLines != null ? new List<string>(debriefLines) : new List<string>()
            };
            MissionSession.End(result);
        }
    }
}
