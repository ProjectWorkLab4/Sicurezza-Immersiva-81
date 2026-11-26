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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Batchmode validator of the Machine Lock scenario. Runs the shared scenario validator through
    /// reflection (the editor assembly is not auto-referenced) and adds the P1 shell checks, the P2
    /// blockout checks and the P3–P8 content checks: every runtime component and its wiring, the particle
    /// budget, the sixteen audio clips, the damage profile and the light budget.
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
                CheckRuntimeComponents(scene, ref errors);
                CheckParticles(scene, ref errors);
                CheckAudioClips(ref errors);
                CheckDamageProfile(ref errors);
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

        // ------------------------------------------------------------------ P3–P8 content checks

        /// <summary>
        /// Every runtime component of the scenario must exist with the expected count, and neither the
        /// scene nor the prefabs may contain a missing script.
        /// </summary>
        private static void CheckRuntimeComponents(Scene scene, ref int errors)
        {
            int press = CountInScene<MachineLockPress>(scene);
            int alarm = CountInScene<MachineLockAlarm>(scene);
            int panel = CountInScene<MachineLockEnergyPanel>(scene);
            int extinguisher = CountInScene<MachineLockExtinguisher>(scene);
            int cabinet = CountInScene<MachineLockPpeCabinet>(scene);
            int ppeItems = CountInScene<MachineLockPpeItem>(scene);
            int valve = CountInScene<MachineLockValve>(scene);
            int spill = CountInScene<MachineLockSpillSystem>(scene);
            int cones = CountInScene<MachineLockCone>(scene);
            int absorbents = CountInScene<MachineLockAbsorbent>(scene);
            int snapZones = CountInScene<MachineLockSnapZone>(scene);
            int observations = CountInScene<MachineLockObservationTarget>(scene);
            int feedback = CountInScene<MachineLockDamageFeedback>(scene);

            Debug.Log($"[MachineLockValidator] Runtime Press={press} Alarm={alarm} Panel={panel} " +
                      $"Extinguisher={extinguisher} Cabinet={cabinet} PpeItems={ppeItems} Valve={valve} " +
                      $"Spill={spill} Cones={cones} Absorbents={absorbents} SnapZones={snapZones} " +
                      $"ObservationTargets={observations} DamageFeedback={feedback}");

            errors += Expect(press, 1, "MachineLockPress");
            errors += Expect(alarm, 1, "MachineLockAlarm");
            errors += Expect(panel, 1, "MachineLockEnergyPanel");
            errors += Expect(extinguisher, 1, "MachineLockExtinguisher");
            errors += Expect(cabinet, 1, "MachineLockPpeCabinet");
            errors += Expect(ppeItems, 6, "MachineLockPpeItem");
            errors += Expect(valve, 1, "MachineLockValve");
            errors += Expect(spill, 1, "MachineLockSpillSystem");
            errors += Expect(cones, 4, "MachineLockCone");
            errors += Expect(absorbents, 4, "MachineLockAbsorbent");
            errors += Expect(snapZones, 8, "MachineLockSnapZone");
            errors += Expect(observations, 6, "MachineLockObservationTarget");
            errors += Expect(feedback, 1, "MachineLockDamageFeedback");

            int missingScripts = CountMissingScripts(scene);
            Debug.Log($"[MachineLockValidator] SceneMissingScripts={missingScripts}");
            if (missingScripts > 0)
                errors += Error($"{missingScripts} missing script(s) found in the scenario scene.");
        }

        /// <summary>
        /// Particle budget of GDD §13.2: at most 200 particles per system and at most four systems that
        /// play on awake.
        /// </summary>
        private static void CheckParticles(Scene scene, ref int errors)
        {
            int systems = 0;
            int playOnAwake = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ParticleSystem system in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (system == null) continue;
                    systems++;
                    ParticleSystem.MainModule main = system.main;
                    if (main.maxParticles > 200)
                        errors += Error($"ParticleSystem {PathOf(system.transform)} has maxParticles={main.maxParticles}; " +
                                        "the budget is 200.");
                    if (main.playOnAwake && system.gameObject.activeInHierarchy) playOnAwake++;
                }
            }

            Debug.Log($"[MachineLockValidator] ParticleSystems={systems} PlayOnAwake={playOnAwake} " +
                      "MaxParticlesBudget=200 PlayOnAwakeBudget=4");
            if (playOnAwake > 4)
                errors += Error($"{playOnAwake} particle systems play on awake; the budget is 4.");
        }

        /// <summary>
        /// The sixteen clips of GDD §13.1 must exist and load as an <see cref="AudioClip"/>.
        /// </summary>
        private static void CheckAudioClips(ref int errors)
        {
            int expected = MachineLockSceneBuilder.AUDIO_CLIPS.Length + 1;
            int loaded = 0;
            foreach (string clip in MachineLockSceneBuilder.AUDIO_CLIPS)
                loaded += CheckAudioClip(MachineLockSceneBuilder.SOUND_DIR + "/" + clip, ref errors);
            loaded += CheckAudioClip(MachineLockSceneBuilder.MUSIC_DIR + "/" + MachineLockSceneBuilder.AMBIENT_CLIP, ref errors);

            Debug.Log($"[MachineLockValidator] AudioClips={loaded}/{expected}");
            if (loaded != expected)
                errors += Error($"{expected - loaded} audio clip(s) of the GDD §13.1 set are missing.");
        }

        private static int CheckAudioClip(string path, ref int errors)
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) != null) return 1;
            errors += Error($"Audio clip not found: {path}");
            return 0;
        }

        /// <summary>
        /// The damage profile asset must exist and contain both a Vignette and a Color Adjustments
        /// override (GDD §9.13).
        /// </summary>
        private static void CheckDamageProfile(ref int errors)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MachineLockSceneBuilder.DAMAGE_PROFILE_PATH);
            if (profile == null)
            {
                errors += Error($"Damage profile not found: {MachineLockSceneBuilder.DAMAGE_PROFILE_PATH}");
                return;
            }

            Vignette vignette;
            bool hasVignette = profile.TryGet(out vignette) && vignette != null;
            ColorAdjustments adjustments;
            bool hasAdjustments = profile.TryGet(out adjustments) && adjustments != null;

            Debug.Log($"[MachineLockValidator] DamageProfile Vignette={hasVignette} ColorAdjustments={hasAdjustments}");
            if (!hasVignette)
                errors += Error("SO_MachineLockDamageProfile has no Vignette override.");
            if (!hasAdjustments)
                errors += Error("SO_MachineLockDamageProfile has no Color Adjustments override.");
        }

        private static int CountInScene<T>(Scene scene) where T : Component
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<T>(true).Length;
            return count;
        }

        private static int CountMissingScripts(Scene scene)
        {
            int missing = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            return missing;
        }

        private static int Expect(int actual, int expected, string label)
        {
            if (actual == expected) return 0;
            Debug.LogError($"[MachineLockValidator] Error: {label} count is {actual} instead of {expected}.");
            return 1;
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
