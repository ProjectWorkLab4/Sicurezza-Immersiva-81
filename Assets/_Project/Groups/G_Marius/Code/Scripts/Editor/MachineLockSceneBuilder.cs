#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SI81.Rig;
using SI81.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Builds the P1 shell of the Machine Lock scenario: the mission asset values, the config asset,
    /// the scenario scene copied from the shared template, the scenario and spawn components and the
    /// Build Settings order. Safe to run again: it updates in place and never duplicates assets.
    /// Menu: SI81/MachineLock/Build Scene Shell. Batchmode entry point: BuildSceneShell.
    /// </summary>
    public static class MachineLockSceneBuilder
    {
        /// <summary>Existing mission asset, edited in place (GDD §14.6).</summary>
        public const string MISSION_PATH = "Assets/_Project/Home/UI/Data/Missions/SO_Mission_MachineLock.asset";
        /// <summary>Group config asset created by this builder.</summary>
        public const string CONFIG_PATH = "Assets/_Project/Groups/G_Marius/Level/Data/SO_MachineLockConfig.asset";
        /// <summary>Group scene created from the shared template.</summary>
        public const string SCENE_PATH = "Assets/_Project/Groups/G_Marius/Level/Scenes/SCN_MachineLock.unity";
        /// <summary>Shared scenario template.</summary>
        public const string TEMPLATE_PATH = "Assets/_Project/Shared/Level/ScenarioTemplate/SCN_ScenarioTemplate.unity";
        /// <summary>Shared core scene, first in the Build Settings.</summary>
        public const string CORE_SCENE_PATH = "Assets/_Project/Home/Level/Scenes/SCN_Core.unity";
        /// <summary>Shared mock scenario, second in the Build Settings.</summary>
        public const string MOCK_SCENE_PATH = "Assets/_Project/Home/Level/Scenes/Mock/SCN_MockScenario.unity";

        private const string SCENE_NAME = "SCN_MachineLock";
        private const string CONTEXT =
            "Ruolo: operatore di reparto. Entri nella sala di stampaggio: la pressa da 250 t lavora in modo anomalo, " +
            "l'olio idraulico a 120 °C perde da un tubo e una pozza calda circonda la macchina. " +
            "Metti in sicurezza la sala e avvisa il preposto.";
        private const string DESIGNER_NOTE =
            "Guided vertical slice — G_Marius. GDD: Docs/MachineLock/GDD_MachineLock.md.";

        private static readonly Vector3 SPAWN_POSITION = new Vector3(-5.2f, 0f, 0f);
        private static readonly Vector3 SPAWN_EULER = new Vector3(0f, 90f, 0f);

        /// <summary>
        /// Builds or updates the whole P1 shell. Menu item and batchmode entry point.
        /// </summary>
        [MenuItem("SI81/MachineLock/Build Scene Shell")]
        public static void BuildSceneShell()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("[MachineLock] Build Scene Shell cannot run in Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new InvalidOperationException("[MachineLock] Build Scene Shell cancelled: unsaved scenes.");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TEMPLATE_PATH) == null)
                throw new InvalidOperationException($"[MachineLock] Template scene not found: {TEMPLATE_PATH}");

            EnsureFolder(CONFIG_PATH);
            EnsureFolder(SCENE_PATH);

            UpdateMission();
            MachineLockConfig config = CreateOrUpdateConfig();
            CreateSceneFromTemplate();

            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            // Opening a scene in Single mode releases unreferenced assets: reload them by path.
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MISSION_PATH);
            config = AssetDatabase.LoadAssetAtPath<MachineLockConfig>(CONFIG_PATH);
            if (mission == null)
                throw new InvalidOperationException($"[MachineLock] Mission asset not found: {MISSION_PATH}");
            if (config == null)
                throw new InvalidOperationException($"[MachineLock] Config asset not found: {CONFIG_PATH}");

            RemoveTemplateExamples(scene);
            ConfigureScenario(scene, mission, config);
            ConfigureSpawnPoint(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SCENE_PATH))
                throw new InvalidOperationException($"[MachineLock] Could not save {SCENE_PATH}");
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();

            ApplyBuildSettings();
            Debug.Log($"[MachineLock] Scene shell ready: {SCENE_PATH}. Mission {MISSION_PATH}, config {CONFIG_PATH}.");
        }

        // ------------------------------------------------------------------ Mission asset

        private static void UpdateMission()
        {
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MISSION_PATH);
            if (mission == null)
                throw new InvalidOperationException($"[MachineLock] Mission asset not found: {MISSION_PATH}");

            mission.Id = "08";
            mission.Title = "Machine Lock";
            mission.Context = CONTEXT;
            mission.Objectives = new List<string>
            {
                "Ferma la pressa e allarma",
                "Isola l'energia e blocca",
                "Indossa i DPI e contiene",
                "Esci e avvisa il preposto"
            };
            mission.Criteria = new List<CompletionCriterion>
            {
                new CompletionCriterion { Outcome = OutcomeType.Positive, Text = "Procedura completa e area sicura" },
                new CompletionCriterion { Outcome = OutcomeType.Partial, Text = "Area non delimitata o DPI incompleti" },
                new CompletionCriterion { Outcome = OutcomeType.Negative, Text = "Macchina ancora attiva o errore grave" }
            };
            mission.Levels = new List<MissionLevel>
            {
                new MissionLevel { Level = DifficultyLevel.Guided, UnlockedByDefault = true, TimeLimitSecondsOverride = 0 },
                new MissionLevel { Level = DifficultyLevel.Operational, UnlockedByDefault = false, TimeLimitSecondsOverride = 0 },
                new MissionLevel { Level = DifficultyLevel.Crisis, UnlockedByDefault = false, TimeLimitSecondsOverride = 480 }
            };
            mission.Complexity = 4;
            mission.SceneName = SCENE_NAME;
            mission.IsTraining = false;
            mission.DesignerNote = DESIGNER_NOTE;

            // Icon, Image and the gradient colours are intentionally left untouched (GDD §14.6).
            EditorUtility.SetDirty(mission);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Config asset

        private static MachineLockConfig CreateOrUpdateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<MachineLockConfig>(CONFIG_PATH);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MachineLockConfig>();
                AssetDatabase.CreateAsset(config, CONFIG_PATH);
                Debug.Log($"[MachineLock] Created {CONFIG_PATH} with the GDD §14.4 defaults.");
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ Scene

        private static void CreateSceneFromTemplate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) != null) return;
            if (!AssetDatabase.CopyAsset(TEMPLATE_PATH, SCENE_PATH))
                throw new InvalidOperationException($"[MachineLock] Could not copy {TEMPLATE_PATH} to {SCENE_PATH}");
            AssetDatabase.Refresh();
            Debug.Log($"[MachineLock] Copied template scene to {SCENE_PATH}.");
        }

        private static void RemoveTemplateExamples(Scene scene)
        {
            string[] exampleNames = { "MESH_ExampleSign", "MESH_ExampleTool", "MESH_ExampleRisk", "MNG_DecisionTrigger" };
            foreach (string name in exampleNames)
            {
                GameObject go = FindRoot(scene, name);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void ConfigureScenario(Scene scene, MissionDefinition mission, MachineLockConfig config)
        {
            GameObject manager = FindRoot(scene, "MNG_Scenario");
            if (manager == null)
            {
                manager = new GameObject("MNG_Scenario");
                SceneManager.MoveGameObjectToScene(manager, scene);
            }

            // Replace the template base controller with the Machine Lock one.
            ScenarioController baseController = manager.GetComponent<ScenarioController>();
            if (baseController != null && !(baseController is MachineLockScenario))
                UnityEngine.Object.DestroyImmediate(baseController);

            var scenario = manager.GetComponent<MachineLockScenario>();
            if (scenario == null) scenario = manager.AddComponent<MachineLockScenario>();

            var so = new SerializedObject(scenario);
            SetObjectReference(so, "mission", mission);
            SetObjectReference(so, "config", config);
            // SpawnPoint on MNG_SpawnPoint owns the offset: the controller reference must stay empty.
            SetObjectReference(so, "spawnPoint", null);
            SerializedProperty level = so.FindProperty("editorLevel");
            if (level != null) level.intValue = (int)DifficultyLevel.Guided;
            SerializedProperty loadCore = so.FindProperty("loadCoreInEditor");
            if (loadCore != null) loadCore.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scenario);
        }

        private static void ConfigureSpawnPoint(Scene scene)
        {
            GameObject spawn = FindRoot(scene, "MNG_SpawnPoint");
            if (spawn == null)
            {
                spawn = new GameObject("MNG_SpawnPoint");
                SceneManager.MoveGameObjectToScene(spawn, scene);
            }
            if (spawn.GetComponent<SpawnPoint>() == null) spawn.AddComponent<SpawnPoint>();
            spawn.transform.SetPositionAndRotation(SPAWN_POSITION, Quaternion.Euler(SPAWN_EULER));
            EditorUtility.SetDirty(spawn);
        }

        // ------------------------------------------------------------------ Build Settings

        private static void ApplyBuildSettings()
        {
            var ordered = new List<EditorBuildSettingsScene>();
            var others = new List<EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
                if (entry.path != CORE_SCENE_PATH && entry.path != MOCK_SCENE_PATH && entry.path != SCENE_PATH)
                    others.Add(entry);

            AddScene(ordered, CORE_SCENE_PATH);
            AddScene(ordered, MOCK_SCENE_PATH);
            AddScene(ordered, SCENE_PATH);
            ordered.AddRange(others);
            EditorBuildSettings.scenes = ordered.ToArray();

            Debug.Log("[MachineLock] Build Settings: " + string.Join(", ", ordered.ConvertAll(s => s.path)) + ".");
        }

        private static void AddScene(List<EditorBuildSettingsScene> target, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning($"[MachineLock] Scene not found, skipped in Build Settings: {path}");
                return;
            }
            foreach (EditorBuildSettingsScene existing in target)
                if (existing.path == path) return;
            target.Add(new EditorBuildSettingsScene(path, true));
        }

        // ------------------------------------------------------------------ Utilities

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static void SetObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"[MachineLock] Serialized property not found: {propertyName}");
            property.objectReferenceValue = value;
        }

        private static void EnsureFolder(string assetPath)
        {
            int slash = assetPath.LastIndexOf('/');
            if (slash <= 0) return;
            string folder = assetPath.Substring(0, slash);
            if (AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
