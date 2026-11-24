using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.Ludovica.TempCore
{
    /// <summary>
    /// [TEMP] Sostituto del flusso Home → Mission Brief → Avvia di SCN_Core: nella build carica la scena dello scenario
    /// in additivo al livello scelto. Menu (VR) o Tab (desktop) riavviano lo scenario al livello successivo
    /// (Guided → Operational → Crisis → Guided). La vibrazione conferma il livello: 1, 2 o 3 impulsi.
    /// </summary>
    public class TempCoreBootstrap : MonoBehaviour
    {
        [Tooltip("Nome della scena dello scenario da caricare.")]
        [SerializeField] private string scenarioSceneName = "SCN_ElectricShock";
        [Tooltip("Percorso della scena (usato solo nell'Editor, dove la scena può non essere nella lista di build).")]
        [SerializeField] private string scenarioScenePath = "Assets/_Project/Groups/G_Ludovica/Scenari/ElectricShock/SCN_ElectricShock.unity";
        [Tooltip("Livello al primo avvio nella build.")]
        [SerializeField] private DifficultyLevel startLevel = DifficultyLevel.Guided;

        private bool busy;

        /// <summary>Si iscrive al riavvio.</summary>
        private void OnEnable() => TempRig.RestartRequested += HandleRestart;

        /// <summary>Si disiscrive dal riavvio.</summary>
        private void OnDisable() => TempRig.RestartRequested -= HandleRestart;

        /// <summary>Se lo scenario non è già caricato (avvio dalla build), lo carica.</summary>
        private IEnumerator Start()
        {
            yield return null;
            if (!SceneManager.GetSceneByName(scenarioSceneName).isLoaded)
                yield return LoadScenario(startLevel);
        }

        /// <summary>Riavvia lo scenario al livello successivo.</summary>
        private void HandleRestart()
        {
            if (busy) return;
            var next = MissionSession.Level == DifficultyLevel.Crisis ? DifficultyLevel.Guided : MissionSession.Level + 1;
            StartCoroutine(Restart(next));
        }

        /// <summary>Scarica e ricarica lo scenario al livello indicato.</summary>
        private IEnumerator Restart(DifficultyLevel level)
        {
            busy = true;
            var scene = SceneManager.GetSceneByName(scenarioSceneName);
            if (TempRig.Instance != null)
            {
                TempRig.Instance.LeftHand.Release();
                TempRig.Instance.RightHand.Release();
            }
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            MissionSession.Abort();
            yield return LoadScenario(level);
            busy = false;
        }

        /// <summary>Apre la sessione e carica lo scenario in additivo.</summary>
        private IEnumerator LoadScenario(DifficultyLevel level)
        {
            MissionSession.Begin(level);
            TempHud.Log("Avvio", $"{scenarioSceneName} al livello {level}");
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scenarioScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync(scenarioSceneName, LoadSceneMode.Additive);
#endif
            var scene = SceneManager.GetSceneByName(scenarioSceneName);
            if (scene.IsValid()) SceneManager.SetActiveScene(scene);
            // Le vibrazioni di conferma girano a parte: il riavvio successivo è subito disponibile.
            StartCoroutine(PulseLevel(level));
        }

        /// <summary>1, 2 o 3 vibrazioni per Guided, Operational, Crisis.</summary>
        private IEnumerator PulseLevel(DifficultyLevel level)
        {
            for (int i = 0; i < (int)level; i++)
            {
                TempHud.Pulse(0.8f, 0.15f);
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}
