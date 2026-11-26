#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using SI81.Rig;
using SI81.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Batchmode validator of the Machine Lock P1 shell. Runs the shared scenario validator through
    /// reflection (the editor assembly is not auto-referenced) and adds the P1 specific checks:
    /// spawn transform, scenario wiring, absence of camera and UI infrastructure, Build Settings order.
    /// Entry point: ValidateScene. Exits 0 only with zero errors and zero warnings.
    /// </summary>
    public static class MachineLockValidatorRunner
    {
        private const string MISSION_PATH = MachineLockSceneBuilder.MISSION_PATH;
        private const string SCENE_PATH = MachineLockSceneBuilder.SCENE_PATH;
        private const string CORE_SCENE_PATH = MachineLockSceneBuilder.CORE_SCENE_PATH;
        private const string MOCK_SCENE_PATH = MachineLockSceneBuilder.MOCK_SCENE_PATH;
        private static readonly Vector3 SPAWN_POSITION = new Vector3(-5.2f, 0f, 0f);

        /// <summary>
        /// Validates the P1 shell and exits with 0 when clean, 1 otherwise.
        /// </summary>
        public static void ValidateScene()
        {
            int errors = 0;
            int warnings = 0;
            try
            {
                Validate(ref errors, ref warnings);
            }
            catch (Exception exception)
            {
                errors += Error("Unexpected failure: " + exception);
            }

            Debug.Log($"[MachineLockValidator] Errors={errors} Warnings={warnings}");
            EditorApplication.Exit(errors > 0 || warnings > 0 ? 1 : 0);
        }

        private static void Validate(ref int errors, ref int warnings)
        {
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MISSION_PATH);
            if (mission == null)
            {
                errors += Error($"Mission asset not found: {MISSION_PATH}");
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);
            if (sceneAsset == null)
            {
                errors += Error($"Scene not found: {SCENE_PATH}");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(SCENE_PATH);
            bool openedHere = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Additive);
                openedHere = true;
            }

            try
            {
                RunSharedValidator(mission, sceneAsset, ref errors, ref warnings);
                CheckSpawnPoint(scene, ref errors);
                CheckScenario(scene, mission, ref errors);
                CheckForbiddenComponents(scene, ref errors);
                CheckBuildSettings(ref errors);
                CheckPrefabs(ref errors);
                CheckTriangleBudget(scene, ref errors);
                CheckLights(scene, ref errors);
                CheckLabelAnchors(scene, ref errors);
                CheckDryPerimeter(scene, ref errors);
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ------------------------------------------------------------------ Shared validator

        private static void RunSharedValidator(MissionDefinition mission, SceneAsset sceneAsset, ref int errors, ref int warnings)
        {
            Type validatorType = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                validatorType = assembly.GetType("SI81.UI.EditorTools.ScenarioValidator", false);
                if (validatorType != null) break;
            }
            if (validatorType == null)
            {
                errors += Error("Shared validator SI81.UI.EditorTools.ScenarioValidator not found in the loaded assemblies.");
                return;
            }

            MethodInfo validate = validatorType.GetMethod(
                "Validate",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(MissionDefinition), typeof(SceneAsset), typeof(bool) },
                null);
            if (validate == null)
            {
                errors += Error("Shared validator Validate(MissionDefinition, SceneAsset, bool) not found.");
                return;
            }

            object result;
            try
            {
                result = validate.Invoke(null, new object[] { mission, sceneAsset, true });
            }
            catch (TargetInvocationException exception)
            {
                errors += Error("Shared validator threw: " + exception.InnerException);
                return;
            }
            if (!(result is IEnumerable issues)) return;

            int count = 0;
            foreach (object issue in issues)
            {
                count++;
                Type type = issue.GetType();
                FieldInfo severityField = type.GetField("Severity");
                FieldInfo messageField = type.GetField("Message");
                int severity = severityField != null ? Convert.ToInt32(severityField.GetValue(issue)) : 0;
                string message = messageField != null ? messageField.GetValue(issue) as string : null;
                if (severity == 2)
                {
                    errors++;
                    Debug.LogError($"[MachineLockValidator] Error: {message}");
                }
                else if (severity == 1)
                {
                    warnings++;
                    Debug.LogWarning($"[MachineLockValidator] Warning: {message}");
                }
                else
                {
                    Debug.Log($"[MachineLockValidator] Info: {message}");
                }
            }
            Debug.Log($"[MachineLockValidator] Shared scenario validator returned {count} issue(s).");
        }

        // ------------------------------------------------------------------ P1 checks

        private static void CheckSpawnPoint(Scene scene, ref int errors)
        {
            GameObject spawn = FindRoot(scene, "MNG_SpawnPoint");
            if (spawn == null)
            {
                errors += Error("MNG_SpawnPoint is missing.");
                return;
            }
            if (spawn.GetComponent<SpawnPoint>() == null)
            {
                errors += Error("MNG_SpawnPoint has no SI81.Rig.SpawnPoint component.");
                return;
            }

            float distance = Vector3.Distance(spawn.transform.position, SPAWN_POSITION);
            if (distance > 0.001f)
                errors += Error($"MNG_SpawnPoint is at {spawn.transform.position} instead of (-5.2, 0, 0); offset {distance:0.000} m.");

            float yaw = Mathf.Abs(Mathf.DeltaAngle(spawn.transform.eulerAngles.y, 90f));
            if (yaw > 0.1f)
                errors += Error($"MNG_SpawnPoint yaw is {spawn.transform.eulerAngles.y:0.000}° instead of 90°.");
        }

        private static void CheckScenario(Scene scene, MissionDefinition mission, ref int errors)
        {
            GameObject manager = FindRoot(scene, "MNG_Scenario");
            if (manager == null)
            {
                errors += Error("MNG_Scenario is missing.");
                return;
            }

            var scenario = manager.GetComponent<MachineLockScenario>();
            if (scenario == null)
            {
                errors += Error("MNG_Scenario has no MachineLockScenario component.");
                return;
            }
            if (scenario.Mission != mission)
                errors += Error($"MNG_Scenario mission is {(scenario.Mission != null ? scenario.Mission.name : "null")} instead of {mission.name}.");

            var so = new SerializedObject(scenario);
            SerializedProperty spawnReference = so.FindProperty("spawnPoint");
            if (spawnReference == null)
                errors += Error("MachineLockScenario has no serialized spawnPoint field.");
            else if (spawnReference.objectReferenceValue != null)
                errors += Error("ScenarioController.spawnPoint must stay empty to avoid a double spawn offset.");

            SerializedProperty editorLevel = so.FindProperty("editorLevel");
            if (editorLevel != null && editorLevel.intValue != (int)DifficultyLevel.Guided)
                errors += Error($"MNG_Scenario editorLevel is {(DifficultyLevel)editorLevel.intValue} instead of Guided.");

            SerializedProperty loadCore = so.FindProperty("loadCoreInEditor");
            if (loadCore != null && !loadCore.boolValue)
                errors += Error("MNG_Scenario loadCoreInEditor must be true for the in-editor standalone test.");
        }

        private static void CheckForbiddenComponents(Scene scene, ref int errors)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    if (camera != null && camera.targetTexture == null)
                        errors += Error($"Camera in the scenario scene: {PathOf(camera.transform)}.");
                foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
                    if (listener != null)
                        errors += Error($"AudioListener in the scenario scene: {PathOf(listener.transform)}.");
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    if (canvas != null)
                        errors += Error($"Canvas in the scenario scene: {PathOf(canvas.transform)}.");
                foreach (EventSystem eventSystem in root.GetComponentsInChildren<EventSystem>(true))
                    if (eventSystem != null)
                        errors += Error($"EventSystem in the scenario scene: {PathOf(eventSystem.transform)}.");
            }
        }

        private static void CheckBuildSettings(ref int errors)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            CheckSceneEntry(scenes, 0, CORE_SCENE_PATH, ref errors);
            CheckSceneEntry(scenes, 1, MOCK_SCENE_PATH, ref errors);
            CheckSceneEntry(scenes, 2, SCENE_PATH, ref errors);
            errors += CountDuplicates(scenes, CORE_SCENE_PATH);
            errors += CountDuplicates(scenes, MOCK_SCENE_PATH);
            errors += CountDuplicates(scenes, SCENE_PATH);
        }

        private static void CheckSceneEntry(EditorBuildSettingsScene[] scenes, int index, string path, ref int errors)
        {
            if (index >= scenes.Length)
            {
                errors += Error($"Build Settings index {index} is empty; expected {path}.");
                return;
            }
            EditorBuildSettingsScene entry = scenes[index];
            if (entry.path != path)
            {
                errors += Error($"Build Settings index {index} is {entry.path} instead of {path}.");
                return;
            }
            if (!entry.enabled) errors += Error($"Build Settings scene {path} is disabled.");
        }

        private static int CountDuplicates(EditorBuildSettingsScene[] scenes, string path)
        {
            int count = 0;
            foreach (EditorBuildSettingsScene entry in scenes)
                if (entry.path == path) count++;
            if (count <= 1) return 0;
            Debug.LogError($"[MachineLockValidator] Error: Build Settings contains {count} entries for {path}.");
            return count - 1;
        }

        // ------------------------------------------------------------------ P2 checks

        /// <summary>
        /// Checks that the twelve production-named prefabs exist and that none of them has a missing
        /// MonoBehaviour or a broken reference (GDD §14.2).
        /// </summary>
        private static void CheckPrefabs(ref int errors)
        {
            int missingScripts = 0;
            foreach (string name in MachineLockSceneBuilder.PREFAB_NAMES)
            {
                string path = MachineLockSceneBuilder.PREFAB_DIR + "/" + name + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    errors += Error($"Prefab not found: {path}");
                    continue;
                }

                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                    if (component == null) missingScripts++;
            }
            Debug.Log($"[MachineLockValidator] Prefabs={MachineLockSceneBuilder.PREFAB_NAMES.Length} MissingScripts={missingScripts}");
            if (missingScripts > 0)
                errors += Error($"{missingScripts} missing script(s) found inside the Machine Lock prefabs.");
        }

        /// <summary>
        /// Triangle budget of the whole scene, including every MeshFilter under every root.
        /// </summary>
        private static void CheckTriangleBudget(Scene scene, ref int errors)
        {
            long triangles = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    Mesh mesh = filter.sharedMesh;
                    if (mesh != null) triangles += mesh.triangles.Length / 3;
                }
            }
            Debug.Log($"[MachineLockValidator] Triangles={triangles} Budget=100000");
            if (triangles > 100000)
                errors += Error($"Scene triangle count is {triangles}, above the 100000 budget.");
        }

        /// <summary>
        /// Dynamic light budget: at most two realtime/mixed lights and at most one shadow caster.
        /// Baked ceiling fixtures do not count (GDD §7.7).
        /// </summary>
        private static void CheckLights(Scene scene, ref int errors)
        {
            int realtime = 0;
            int shadowCasters = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light == null || !light.enabled || !light.gameObject.activeInHierarchy) continue;
                    if (light.lightmapBakeType != LightmapBakeType.Baked) realtime++;
                    if (light.shadows != LightShadows.None) shadowCasters++;
                }
            }
            Debug.Log($"[MachineLockValidator] RealtimeLights={realtime} ShadowLights={shadowCasters}");
            if (realtime > 2)
                errors += Error($"Scene has {realtime} active realtime/mixed lights; the budget is 2.");
            if (shadowCasters > 1)
                errors += Error($"Scene has {shadowCasters} shadow-casting lights; the budget is 1.");
        }

        /// <summary>
        /// Exactly thirteen world labels and exactly three Risk labels (GDD §8.4).
        /// </summary>
        private static void CheckLabelAnchors(Scene scene, ref int errors)
        {
            int anchors = 0;
            int risks = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (LabelAnchor anchor in root.GetComponentsInChildren<LabelAnchor>(true))
                {
                    if (anchor == null) continue;
                    anchors++;
                    if (anchor.Kind == LabelKind.Risk) risks++;
                }
            }
            Debug.Log($"[MachineLockValidator] LabelAnchors={anchors} RiskLabels={risks}");
            if (anchors != 13)
                errors += Error($"Scene has {anchors} LabelAnchor components instead of the expected 13.");
            if (risks != 3)
                errors += Error($"Scene has {risks} Risk labels instead of the expected 3.");
        }

        /// <summary>
        /// The three puddle variants must leave at least 1.5 m of dry perimeter on every side
        /// (GDD §7.6): pattern extents are read from PATTERN_PuddleA/B/C in HAZARD_Oil.
        /// </summary>
        private static void CheckDryPerimeter(Scene scene, ref int errors)
        {
            string[] names = { "PATTERN_PuddleA", "PATTERN_PuddleB", "PATTERN_PuddleC" };
            foreach (string name in names)
            {
                Transform pattern = FindDeep(scene, name);
                if (pattern == null)
                {
                    errors += Error($"{name} is missing from HAZARD_Oil.");
                    continue;
                }

                Vector3 position = pattern.position;
                float halfX = Mathf.Abs(pattern.lossyScale.x) * 0.5f;
                float halfZ = Mathf.Abs(pattern.lossyScale.z) * 0.5f;
                float west = position.x - halfX + 6f;
                float east = 6f - (position.x + halfX);
                float south = position.z - halfZ + 4f;
                float north = 4f - (position.z + halfZ);
                Debug.Log($"[MachineLockValidator] {name} DryMargin W={west:0.00} E={east:0.00} S={south:0.00} N={north:0.00}");

                if (west < 1.5f - 0.001f || east < 1.5f - 0.001f || south < 1.5f - 0.001f || north < 1.5f - 0.001f)
                    errors += Error($"{name} leaves a dry perimeter below 1.5 m " +
                                    $"(W={west:0.00} E={east:0.00} S={south:0.00} N={north:0.00}).");
            }
        }

        // ------------------------------------------------------------------ Utilities

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static Transform FindDeep(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.name == name) return transform;
            return null;
        }

        private static string PathOf(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }

        private static int Error(string message)
        {
            Debug.LogError("[MachineLockValidator] Error: " + message);
            return 1;
        }
    }
}
#endif
