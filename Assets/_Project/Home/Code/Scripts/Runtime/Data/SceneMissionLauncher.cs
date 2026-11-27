using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.UI
{
    /// <summary>
    /// Lanciatore reale: carica la scena dello scenario (<c>MissionDefinition.SceneName</c>), inizia la sessione e nasconde il menu.
    /// Il rig e MNG_UI stanno in SCN_Core (sempre caricata): le scene di scenario sono caricate in ADDITIVO e scaricate al ritorno.
    /// Con <c>additive</c> = false usa il caricamento singolo (MNG_UI deve allora essere presente anche nella scena di menu).
    /// </summary>
    public sealed class SceneMissionLauncher : IMissionLauncher
    {
        // Nome della scena di menu da ricaricare al ritorno (vuoto = il menu è già in SCN_Core, si scarica solo lo scenario).
        private readonly string menuSceneName;
        // true = scenari in additivo sopra SCN_Core.
        private readonly bool additive;
        // Scena di scenario caricata in additivo, da scaricare al ritorno.
        private string loadedMissionScene;
        // Contatore monotono per annullare callback di scaricamento obsolete se parte un nuovo lancio o un ritorno alla Home.
        private int transitionGeneration;

        /// <summary>
        /// Crea il lanciatore.
        /// </summary>
        /// <param name="newMenuSceneName">Scena di menu da caricare al ritorno (es. SCN_MainMenu); vuota se il menu sta in SCN_Core.</param>
        /// <param name="newAdditive">true per caricare gli scenari in additivo (consigliato).</param>
        public SceneMissionLauncher(string newMenuSceneName, bool newAdditive = true)
        {
            menuSceneName = newMenuSceneName;
            additive = newAdditive;
        }

        /// <inheritdoc/>
        public bool CanLaunch(MissionDefinition m)
        {
            if (m == null || string.IsNullOrEmpty(m.SceneName)) return false;
#if UNITY_EDITOR
            // In Editor la scena deve essere nelle Build Settings e attiva, come nella build (stesso errore evitato).
            foreach (UnityEditor.EditorBuildSettingsScene scene in UnityEditor.EditorBuildSettings.scenes)
                if (scene.enabled && System.IO.Path.GetFileNameWithoutExtension(scene.path) == m.SceneName) return true;
            return false;
#else
            return Application.CanStreamedLevelBeLoaded(m.SceneName);
#endif
        }

        /// <inheritdoc/>
        public void Launch(MissionDefinition m, DifficultyLevel level)
        {
            if (!CanLaunch(m))
            {
                Debug.LogWarning($"[SI81 UI] Scenario {(m != null ? m.name : "null")}: scena non disponibile nella build, avvio annullato.");
                return;
            }
            StartScene(m, level, false);
        }

        /// <inheritdoc/>
        public void LaunchTraining(MissionDefinition training)
        {
            if (!CanLaunch(training))
            {
                Debug.LogWarning("[SI81 UI] Training: scena non disponibile nella build, avvio annullato.");
                return;
            }
            StartScene(training, DifficultyLevel.Guided, true);
        }

        /// <inheritdoc/>
        public void ReturnToHome()
        {
            int generation = ++transitionGeneration;
            Time.timeScale = 1f;
            MissionSession.Clear();
            if (additive && !string.IsNullOrEmpty(loadedMissionScene) && SceneManager.GetSceneByName(loadedMissionScene).isLoaded)
            {
                // Scarica lo scenario (la scena core torna attiva); il menu è già in SCN_Core.
                SetCoreSceneActive(SceneManager.GetSceneByName(loadedMissionScene));
                AsyncOperation unload = SceneManager.UnloadSceneAsync(loadedMissionScene);
                loadedMissionScene = null;
                if (unload != null) unload.completed += _ => { if (transitionGeneration == generation) ShowHome(); };
                else ShowHome();
                return;
            }
            loadedMissionScene = null;
            if (!string.IsNullOrEmpty(menuSceneName) && SceneManager.GetActiveScene().name != menuSceneName)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(menuSceneName, LoadSceneMode.Single);
                if (load != null)
                {
                    load.completed += _ => { if (transitionGeneration == generation) ShowHome(); };
                    return;
                }
            }
            ShowHome();
        }

        /// <summary>
        /// Carica la scena dello scenario e inizia la sessione.
        /// </summary>
        /// <param name="m">Scenario.</param>
        /// <param name="level">Livello.</param>
        /// <param name="training">true per il Training.</param>
        private void StartScene(MissionDefinition m, DifficultyLevel level, bool training)
        {
            if (string.IsNullOrEmpty(m.SceneName))
            {
                Debug.LogError($"[SI81 UI] Lo scenario \"{m.name}\" non ha SceneName: impossibile avviarlo.", m);
                return;
            }
            int generation = ++transitionGeneration;
            Time.timeScale = 1f;
            // Ricomincia / Riprova / Cambia livello: se uno scenario è ancora caricato lo scarica, altrimenti la scena si duplica.
            // La nuova sessione e la nuova scena partono solo a scaricamento finito: così OnDisable (SpawnPoint) e OnDestroy
            // dello scenario vecchio completano prima che parta il nuovo scenario e non possono sovrascrivere posa, HUD o checklist.
            AsyncOperation unload = UnloadLoadedScene();
            if (unload != null && !unload.isDone)
            {
                if (UIScreenManager.Instance != null) UIScreenManager.Instance.HideMenu();
                unload.completed += _ =>
                {
                    if (transitionGeneration != generation) return;
                    LoadMissionScene(m, level, training, generation);
                };
                return;
            }
            LoadMissionScene(m, level, training, generation);
        }

        /// <summary>Avvia la sessione, carica la scena dello scenario (additiva o singola) e la rende attiva a caricamento finito.</summary>
        /// <param name="m">Scenario.</param>
        /// <param name="level">Livello.</param>
        /// <param name="training">true per il Training.</param>
        /// <param name="generation">Generazione della transizione per ignorare callback obsolete.</param>
        private void LoadMissionScene(MissionDefinition m, DifficultyLevel level, bool training, int generation)
        {
            MissionSession.Begin(m, level, training);
            AsyncOperation op = SceneManager.LoadSceneAsync(m.SceneName, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SI81 UI] Scena \"{m.SceneName}\" non trovata nelle Build Settings.", m);
                MissionSession.Clear();
                return;
            }
            if (additive)
            {
                loadedMissionScene = m.SceneName;
                // A caricamento finito la scena dello scenario diventa quella attiva (luci, NavMesh, istanziazioni).
                string sceneName = m.SceneName;
                op.completed += _ =>
                {
                    if (transitionGeneration != generation) return;
                    Scene loaded = SceneManager.GetSceneByName(sceneName);
                    if (loaded.isLoaded && loadedMissionScene == sceneName) SceneManager.SetActiveScene(loaded);
                };
            }
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.HideMenu();
        }

        /// <summary>
        /// Scarica la scena di scenario ancora caricata (se c'è) e rimette attiva la scena di base.
        /// </summary>
        /// <returns>L'operazione di scaricamento, o null se non c'era nulla da scaricare.</returns>
        private AsyncOperation UnloadLoadedScene()
        {
            if (!additive || string.IsNullOrEmpty(loadedMissionScene)) return null;
            Scene scene = SceneManager.GetSceneByName(loadedMissionScene);
            loadedMissionScene = null;
            if (!scene.isLoaded) return null;
            SetCoreSceneActive(scene);
            return SceneManager.UnloadSceneAsync(scene);
        }

        /// <summary>
        /// Rimette attiva la scena che contiene MNG_UI (la scena core), prima di scaricare quella dello scenario.
        /// </summary>
        /// <param name="exclude">Scena da non scegliere (quella dello scenario).</param>
        private static void SetCoreSceneActive(Scene exclude)
        {
            Scene core = UIScreenManager.Instance != null ? UIScreenManager.Instance.gameObject.scene : default;
            if (core.IsValid() && core.isLoaded && core != exclude) SceneManager.SetActiveScene(core);
        }

        /// <summary>
        /// Mostra il menu e va a Home (dopo il caricamento della scena).
        /// </summary>
        private static void ShowHome()
        {
            UIScreenManager manager = UIScreenManager.Instance;
            if (manager == null) return;
            manager.ShowMenu();
            manager.GoHome();
            // L'origine del rig può essere tornata dal punto di partenza dello scenario a quella originale: il menu va rimesso davanti alla testa.
            manager.Recenter();
        }
    }
}
