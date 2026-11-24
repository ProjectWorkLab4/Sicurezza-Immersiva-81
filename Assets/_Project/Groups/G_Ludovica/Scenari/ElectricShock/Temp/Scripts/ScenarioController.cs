using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.Ludovica.TempCore
{
    /// <summary>
    /// [TEMP] Sostituto locale di SI81.UI.ScenarioController con la stessa API pubblica e protetta
    /// (SetObjective, CompleteObjective, SetPhase, SetHint, Alert, Feedback, Subtitle, AskDecision, EndMission,
    /// OnScenarioStarted, OnTimerExpired, Level, ElapsedSeconds...). Tutte le chiamate alla UI finiscono in Console.
    /// Quando la UI comune è nel progetto: elimina la cartella Temp, cambia lo using in SI81.UI e assegna SO_Mission_ElectricShock
    /// (i dati della missione sono in Docs/SO_Mission_ElectricShock_dati.md).
    /// </summary>
    [DisallowMultipleComponent]
    public class ScenarioController : MonoBehaviour
    {
        /// <summary>Fase "scansione" della HUD.</summary>
        public const string PHASE_SCAN = "hud.phase.scan";
        /// <summary>Fase "decisione" della HUD.</summary>
        public const string PHASE_DECISION = "hud.phase.decision";
        /// <summary>Fase "azione" della HUD.</summary>
        public const string PHASE_ACTION = "hud.phase.action";
        /// <summary>Fase "risoluzione" della HUD.</summary>
        public const string PHASE_RESOLUTION = "hud.phase.resolution";
        /// <summary>Punteggio dato di default quando il tempo scade.</summary>
        public const int TIMEOUT_SCORE = 20;
        /// <summary>Percorso della scena core temporanea (solo Editor).</summary>
        public const string TEMP_CORE_SCENE_PATH = "Assets/_Project/Groups/G_Ludovica/Scenari/ElectricShock/Temp/SCN_ElectricShock_TempCore.unity";
        /// <summary>Secondi massimi di attesa del rig temporaneo.</summary>
        private const float CORE_WAIT_SECONDS = 15f;

        [Header("[TEMP] Dati missione (poi in SO_Mission_ElectricShock)")]
        [Tooltip("Id a due cifre della missione.")]
        [SerializeField] protected string missionId = "10";
        [Tooltip("Titolo della missione.")]
        [SerializeField] protected string missionTitle = "Electric Shock";
        [Tooltip("Obiettivi (1-4 righe, max 30 caratteri).")]
        [SerializeField] protected List<string> objectives = new List<string>();
        [Tooltip("Tempo limite per Guided, Operational, Crisis (0 = tempo del livello: nessuno / 600 / 300).")]
        [SerializeField] protected int[] timeLimitSecondsOverride = { 0, 240, 180 };

        [Header("Scenario")]
        [Tooltip("Punto di partenza del giocatore (MNG_SpawnPoint).")]
        [SerializeField] private Transform spawnPoint;

        [Header("Prova da Editor")]
        [Tooltip("Livello usato quando la scena parte da sola.")]
        [SerializeField] private DifficultyLevel editorLevel = DifficultyLevel.Guided;
        [Tooltip("Se attivo, in Play Mode da sola la scena carica la scena core temporanea in additivo.")]
        [SerializeField] private bool loadCoreInEditor = true;

        private bool running;
        private bool ended;
        private int currentObjective;
        private int objectivesCompleted;
        private float startTime;
        private float timeLimit;
        private bool timerExpired;

        /// <summary>Livello in corso.</summary>
        public DifficultyLevel Level => MissionSession.Level;
        /// <summary>true tra l'avvio e EndMission.</summary>
        public bool IsRunning => running && !ended;
        /// <summary>Indice dell'obiettivo corrente (0-based).</summary>
        public int CurrentObjectiveIndex => currentObjective;
        /// <summary>Numero di obiettivi completati.</summary>
        public int ObjectivesCompleted => objectivesCompleted;
        /// <summary>Numero di obiettivi dello scenario.</summary>
        public int ObjectiveCount => objectives != null ? objectives.Count : 0;
        /// <summary>Secondi di gioco dall'avvio dello scenario.</summary>
        public float ElapsedSeconds => running ? Time.time - startTime : 0f;

        /// <summary>Avvia lo scenario: sessione (da sola in Editor), rig temporaneo, spawn, poi OnScenarioStarted.</summary>
        private IEnumerator Start()
        {
            if (!MissionSession.IsActive)
            {
#if UNITY_EDITOR
                if (loadCoreInEditor && TempRig.Instance == null && !IsSceneLoaded(TEMP_CORE_SCENE_PATH))
                {
                    var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                        TEMP_CORE_SCENE_PATH, new LoadSceneParameters(LoadSceneMode.Additive));
                    while (op != null && !op.isDone) yield return null;
                }
                MissionSession.Begin(editorLevel);
#else
                Debug.LogError("[SI81 Scenario] Nessuna sessione attiva: la scena va caricata dalla scena core temporanea.", this);
                yield break;
#endif
            }

            float waited = 0f;
            while (TempRig.Instance == null && waited < CORE_WAIT_SECONDS)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (TempRig.Instance == null) Debug.LogWarning("[SI81 Scenario] Rig temporaneo non trovato: lo scenario parte senza giocatore.", this);
            else if (spawnPoint != null) TempRig.Instance.TeleportTo(spawnPoint.position, spawnPoint.eulerAngles.y);

            int levelIndex = Mathf.Clamp((int)Level - 1, 0, 2);
            int defaultLimit = Level == DifficultyLevel.Guided ? 0 : (Level == DifficultyLevel.Operational ? 600 : 300);
            int overrideLimit = timeLimitSecondsOverride != null && timeLimitSecondsOverride.Length > levelIndex ? timeLimitSecondsOverride[levelIndex] : 0;
            timeLimit = overrideLimit > 0 ? overrideLimit : defaultLimit;

            running = true;
            startTime = Time.time;
            TempHud.Log("Missione", $"{missionId} · {missionTitle} · livello {Level}" + (timeLimit > 0 ? $" · tempo {timeLimit:0} s" : " · senza tempo limite"));
            if (ObjectiveCount > 0) SetObjective(0);
            OnScenarioStarted();
            if (timeLimit > 0f) StartCoroutine(TimerRoutine());
        }

        /// <summary>Controlla il tempo limite del livello (coroutine: la classe figlia resta libera di usare Update).</summary>
        private IEnumerator TimerRoutine()
        {
            while (IsRunning && !timerExpired)
            {
                if (ElapsedSeconds >= timeLimit)
                {
                    timerExpired = true;
                    TempHud.Log("Timer", "tempo scaduto");
                    OnTimerExpired();
                }
                yield return null;
            }
        }

        /// <summary>Pulizia alla distruzione (come nella UI comune).</summary>
        protected virtual void OnDestroy() { }

        /// <summary>Sessione, HUD e primo obiettivo pronti: qui parte la logica dello scenario.</summary>
        protected virtual void OnScenarioStarted() { }

        /// <summary>Tempo scaduto: di default chiude con esito Negativo.</summary>
        protected virtual void OnTimerExpired()
        {
            EndMission(OutcomeType.Negative, TIMEOUT_SCORE, null, new List<string> { DebriefLinePrefix.BAD + "Tempo scaduto prima di completare la missione." });
        }

        /// <summary>Imposta l'obiettivo corrente (0-based).</summary>
        /// <param name="index">Indice dell'obiettivo.</param>
        public void SetObjective(int index)
        {
            if (ObjectiveCount == 0) return;
            currentObjective = Mathf.Clamp(index, 0, ObjectiveCount - 1);
            TempHud.Log("Obiettivo", $"{currentObjective + 1}/{ObjectiveCount} {objectives[currentObjective]}");
        }

        /// <summary>Completa l'obiettivo corrente e passa al successivo.</summary>
        public void CompleteObjective()
        {
            objectivesCompleted++;
            if (currentObjective < ObjectiveCount - 1) SetObjective(currentObjective + 1);
        }

        /// <summary>Imposta la fase della HUD (PHASE_*).</summary>
        /// <param name="phaseKey">Chiave della fase.</param>
        public void SetPhase(string phaseKey) => TempHud.Log("Fase", phaseKey);

        /// <summary>Suggerimento (la UI comune lo mostra solo in Guided).</summary>
        /// <param name="text">Testo, circa 60 caratteri.</param>
        public void SetHint(string text) => TempHud.Log("Suggerimento", text);

        /// <summary>Avviso evento (3 s, suono e vibrazione).</summary>
        /// <param name="text">"Titolo\nDettaglio".</param>
        /// <param name="iconName">Nome icona (ignorato qui).</param>
        public void Alert(string text, string iconName = null)
        {
            TempHud.Log("Avviso", text.Replace("\n", " · "));
            TempHud.Pulse(0.6f, 0.25f);
        }

        /// <summary>Feedback di azione.</summary>
        /// <param name="text">Testo.</param>
        /// <param name="kind">Positive, Neutral, Negative.</param>
        public void Feedback(string text, FeedbackKind kind) => TempHud.Log($"Feedback {kind}", text);

        /// <summary>Sottotitolo.</summary>
        /// <param name="speaker">Chi parla.</param>
        /// <param name="text">Testo.</param>
        /// <param name="seconds">Durata (0 = automatica).</param>
        public void Subtitle(string speaker, string text, float seconds = 0f) => TempHud.Log("Sottotitolo", string.IsNullOrEmpty(speaker) ? text : $"{speaker}: {text}");

        /// <summary>Pannello decisione: qui non esiste, quindi restituisce false come la UI comune senza pannello.</summary>
        /// <param name="question">Domanda.</param>
        /// <param name="options">Opzioni.</param>
        /// <param name="onChosen">Callback con indice scelto.</param>
        /// <param name="timeoutSeconds">0 = nessun limite.</param>
        /// <returns>Sempre false (pannello assente).</returns>
        public bool AskDecision(string question, string[] options, Action<int> onChosen, float timeoutSeconds = 0f)
        {
            TempHud.Log("Decisione (pannello assente)", question);
            return false;
        }

        /// <summary>Chiude la missione una sola volta: scrive esito, punteggio, dimensioni e debrief in Console.</summary>
        /// <param name="outcome">Esito.</param>
        /// <param name="safetyScore">Punteggio 0-100; se negativo media delle dimensioni.</param>
        /// <param name="dimensions">Cinque valori 0-100 (Safety, Awareness, Decision, Execution, Time).</param>
        /// <param name="debriefLines">Righe di debrief con prefisso.</param>
        public void EndMission(OutcomeType outcome, int safetyScore, int[] dimensions, List<string> debriefLines)
        {
            if (ended) return;
            ended = true;

            int[] dims = new int[5];
            if (dimensions != null)
                for (int i = 0; i < dims.Length && i < dimensions.Length; i++) dims[i] = Mathf.Clamp(dimensions[i], 0, 100);
            int score;
            if (safetyScore >= 0) score = Mathf.Clamp(safetyScore, 0, 100);
            else
            {
                int sum = 0;
                foreach (int d in dims) sum += d;
                score = Mathf.RoundToInt(sum / 5f);
            }
            if (dimensions == null) for (int i = 0; i < dims.Length; i++) dims[i] = score;

            var result = new MissionResultShim
            {
                MissionId = missionId,
                Level = Level,
                Outcome = outcome,
                SafetyScore = score,
                Dimensions = dims,
                DurationSeconds = ElapsedSeconds,
                DebriefLines = debriefLines != null ? new List<string>(debriefLines) : new List<string>()
            };

            var sb = new StringBuilder();
            sb.AppendLine($"{TempHud.TAG} FINE MISSIONE {missionId} · {missionTitle} · {Level}");
            sb.AppendLine($"Esito: {outcome} · Punteggio sicurezza {score}/100 · Durata {result.DurationSeconds:0} s");
            sb.AppendLine($"Safety {dims[0]} · Awareness {dims[1]} · Decision {dims[2]} · Execution {dims[3]} · Time {dims[4]}");
            foreach (var line in result.DebriefLines) sb.AppendLine(line);
            Debug.Log(sb.ToString());

            MissionSession.End(result);
        }

        /// <summary>true se la scena con quel percorso è già caricata.</summary>
        private static bool IsSceneLoaded(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).path == path) return true;
            return false;
        }
    }
}
