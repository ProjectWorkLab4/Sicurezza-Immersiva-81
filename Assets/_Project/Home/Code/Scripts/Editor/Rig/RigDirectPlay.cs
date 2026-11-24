using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.Rig.EditorTools
{
    /// <summary>
    /// Direct-play Editor: premendo Play in una scena scenario (con MNG_SpawnPoint/SpawnPoint) carica SCN_Core in additivo
    /// se non è già presente, così il rig condiviso arriva senza passare dalla Home. La scena scenario resta quella attiva.
    /// Il menu della UI va nascosto dall'adattatore UI (ScenarioController/UIScreenManager): il rig non referenzia SI81.UI.
    /// </summary>
    [InitializeOnLoad]
    public static class RigDirectPlay
    {
        /// <summary>Percorso della scena core della UI comune.</summary>
        public const string CoreScenePath = "Assets/_Project/Home/Level/Scenes/SCN_Core.unity";

        /// <summary>
        /// Si iscrive al cambio di stato del Play Mode.
        /// </summary>
        static RigDirectPlay()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        /// <summary>
        /// All'ingresso in Play Mode carica SCN_Core additivamente se la scena attiva è uno scenario con spawn point.
        /// </summary>
        /// <param name="change">Nuovo stato del Play Mode.</param>
        private static void HandlePlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || active.path == CoreScenePath) return;
            if (SceneManager.GetSceneByPath(CoreScenePath).isLoaded) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CoreScenePath) == null) return;
            if (Object.FindFirstObjectByType<SpawnPoint>() == null && GameObject.Find("MNG_SpawnPoint") == null) return;

            EditorSceneManager.LoadSceneAsyncInPlayMode(CoreScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
        }
    }
}
