#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// reflection and enforces all Issue #35 / ADR-0001..0004 architectural, Quest 3 budget, ASTC,
    /// building-shell, Dry Perimeter, non-overlapping electrical Sub-Station, 4-slot PPE, particle,
    /// light, audio, and damage-profile gates. Exits 0 only with zero errors and zero warnings.
    /// </summary>
    public static class MachineLockValidatorRunner
    {
        private const string MISSION_PATH = MachineLockSceneBuilder.MISSION_PATH;
        private const string SCENE_PATH = MachineLockSceneBuilder.SCENE_PATH;
        private const string CORE_SCENE_PATH = MachineLockSceneBuilder.CORE_SCENE_PATH;
        private const string MOCK_SCENE_PATH = MachineLockSceneBuilder.MOCK_SCENE_PATH;

        /// <summary>
        /// Validates SCN_MachineLock and exits with 0 when clean, 1 otherwise.
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
                CheckIndustrialBuildingShell(scene, ref errors);
                CheckTextureAstcAndDisplacement(scene, ref errors);
                CheckPrefabs(ref errors);
                CheckTriangleBudget(scene, ref errors);
                CheckLights(scene, ref errors);
                CheckLabelAnchors(scene, ref errors);
                CheckDryPerimeter(scene, ref errors);
                CheckElectricalAndPpeWiring(scene, ref errors);
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

        // ------------------------------------------------------------------ Shell, spawn, scenario, build settings

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

            float distance = Vector3.Distance(spawn.transform.position, MachineLockSceneBuilder.SPAWN_POSITION);
            if (distance > 0.001f)
                errors += Error($"MNG_SpawnPoint is at {spawn.transform.position} instead of {MachineLockSceneBuilder.SPAWN_POSITION}; offset {distance:0.000} m.");

            float yaw = Mathf.Abs(Mathf.DeltaAngle(spawn.transform.eulerAngles.y, MachineLockSceneBuilder.SPAWN_EULER.y));
            if (yaw > 0.1f)
                errors += Error($"MNG_SpawnPoint yaw is {spawn.transform.eulerAngles.y:0.000}° instead of {MachineLockSceneBuilder.SPAWN_EULER.y}°.");
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

        // ------------------------------------------------------------------ Industrial building & ASTC checks

        /// <summary>
        /// Verifies that the shared PFB_OldIndustrialBuilding shell is present in SCN_MachineLock
        /// with intact renderers and zero missing scripts (ADR-0001).
        /// </summary>
        private static void CheckIndustrialBuildingShell(Scene scene, ref int errors)
        {
            Transform building = FindDeep(scene, "PFB_OldIndustrialBuilding");
            if (building == null)
            {
                errors += Error("PFB_OldIndustrialBuilding shell is missing from SCN_MachineLock (ADR-0001).");
                return;
            }
            int renderers = building.GetComponentsInChildren<MeshRenderer>(true).Length;
            int missing = 0;
            foreach (Transform t in building.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            Debug.Log($"[MachineLockValidator] IndustrialBuilding=Present Renderers={renderers} MissingScripts={missing}");
            if (renderers < 2)
                errors += Error($"PFB_OldIndustrialBuilding has only {renderers} MeshRenderer(s).");
            if (missing > 0)
                errors += Error($"PFB_OldIndustrialBuilding has {missing} missing script(s).");
        }

        /// <summary>
        /// Validates Android ASTC overrides (&lt;= 2048) across all active building and PBR textures
        /// (albedo, normal, roughness/AO/spec) and confirms no rendered material references an 8K displacement map.
        /// </summary>
        private static void CheckTextureAstcAndDisplacement(Scene scene, ref int errors)
        {
            string[] buildingTexPaths =
            {
                MachineLockSceneBuilder.SHARED_BUILDING_TEX_DIR + "/buildingbase.jpg",
                MachineLockSceneBuilder.SHARED_BUILDING_TEX_DIR + "/buildingNormalMap.jpg",
                MachineLockSceneBuilder.SHARED_BUILDING_TEX_DIR + "/buildingAmbientOcclusionMap.jpg",
                MachineLockSceneBuilder.SHARED_BUILDING_TEX_DIR + "/buildingSpecularMap.jpg",
                MachineLockSceneBuilder.SHARED_BUILDING_TEX_DIR + "/TexturesCom_ConcreteBare0348_1_S.jpg",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_ConcreteFloorWorn_Albedo.jpg",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_ConcreteFloorWorn_Normal.exr",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_ConcreteFloorWorn_Rough.jpg",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_GreenMetalRust_Albedo.jpg",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_GreenMetalRust_Normal.exr",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_GreenMetalRust_Rough.jpg",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_ConcreteWall009_Albedo.jpg",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_ConcreteWall009_Normal.exr",
                MachineLockSceneBuilder.TEXTURE_DIR + "/TEX_ConcreteWall009_Rough.jpg"
            };

            int checkedCount = 0;
            foreach (string path in buildingTexPaths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    errors += Error($"Expected PBR/building texture missing: {path}");
                    continue;
                }
                checkedCount++;
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                if (!android.overridden || android.maxTextureSize > 2048)
                {
                    errors += Error($"Texture {path} Android override must be enabled and <= 2048 (actual overridden={android.overridden}, size={android.maxTextureSize}).");
                }
                if (!android.format.ToString().StartsWith("ASTC", StringComparison.Ordinal))
                {
                    errors += Error($"Texture {path} Android format must be ASTC (actual {android.format}).");
                }
            }

            // Ensure the 8K buildingDisplacementMap is NOT referenced by any material rendered in SCN_MachineLock.
            int displacementRefs = 0;
            var seenMaterials = new HashSet<Material>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        if (mat == null || !seenMaterials.Add(mat)) continue;
                        int propCount = mat.shader != null ? mat.shader.GetPropertyCount() : 0;
                        for (int i = 0; i < propCount; i++)
                        {
                            if (mat.shader.GetPropertyType(i) != ShaderPropertyType.Texture) continue;
                            Texture tex = mat.GetTexture(mat.shader.GetPropertyNameId(i));
                            if (tex != null && tex.name.IndexOf("Displacement", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                displacementRefs++;
                                errors += Error($"Material {mat.name} references displacement texture {tex.name} on Quest 3.");
                            }
                        }
                    }
                }
            }

            Debug.Log($"[MachineLockValidator] AstcTexturesChecked={checkedCount}/{buildingTexPaths.Length} DisplacementRefs={displacementRefs}");
        }

        // ------------------------------------------------------------------ Prefabs, budgets, labels, dry perimeter

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
            Debug.Log($"[MachineLockValidator] LabelAnchors={anchors} RiskLabels={risks} RiskCap=3");
            if (anchors != MachineLockSceneBuilder.EXPECTED_LABEL_ANCHORS)
                errors += Error($"Scene has {anchors} LabelAnchor components instead of the expected {MachineLockSceneBuilder.EXPECTED_LABEL_ANCHORS}.");
            if (risks > 3)
                errors += Error($"Scene has {risks} Risk labels; the cap is 3.");
        }

        private static void CheckDryPerimeter(Scene scene, ref int errors)
        {
            string[] names = { "PATTERN_PuddleA", "PATTERN_PuddleB", "PATTERN_PuddleC" };
            float halfBayX = MachineLockSceneBuilder.ROOM_HALF_X;
            float halfBayZ = MachineLockSceneBuilder.ROOM_HALF_Z;
            float minMargin = MachineLockSceneBuilder.MIN_DRY_PERIMETER;

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
                float west = position.x - halfX + halfBayX;
                float east = halfBayX - (position.x + halfX);
                float south = position.z - halfZ + halfBayZ;
                float north = halfBayZ - (position.z + halfZ);
                Debug.Log($"[MachineLockValidator] {name} DryMargin W={west:0.00} E={east:0.00} S={south:0.00} N={north:0.00} MinRequired={minMargin:0.00}");

                if (west < minMargin - 0.001f || east < minMargin - 0.001f || south < minMargin - 0.001f || north < minMargin - 0.001f)
                    errors += Error($"{name} leaves a dry perimeter below {minMargin:0.00} m " +
                                    $"(W={west:0.00} E={east:0.00} S={south:0.00} N={north:0.00}).");
            }
        }

        private static void CheckElectricalAndPpeWiring(Scene scene, ref int errors)
        {
            // Verify electrical Sub-Stations are physically separated (>= 0.55 m apart).
            Transform mainSub = FindDeep(scene, "SUB_MainDisconnect");
            Transform lockSub = FindDeep(scene, "SUB_LockoutStation");
            Transform testSub = FindDeep(scene, "SUB_VoltageTester");
            if (mainSub == null || lockSub == null || testSub == null)
            {
                errors += Error("Electrical bay is missing one or more Sub-Station roots (SUB_MainDisconnect, SUB_LockoutStation, SUB_VoltageTester).");
            }
            else
            {
                float d1 = Vector3.Distance(mainSub.position, lockSub.position);
                float d2 = Vector3.Distance(lockSub.position, testSub.position);
                Debug.Log($"[MachineLockValidator] ElectricalSubStations Separation Main-Lock={d1:0.00}m Lock-Tester={d2:0.00}m");
                if (d1 < 0.55f || d2 < 0.55f)
                    errors += Error($"Electrical Sub-Stations are too close (Main-Lock={d1:0.00}m, Lock-Tester={d2:0.00}m; minimum 0.55 m).");
            }

            // Verify 4-slot PPE wiring: Gloves, Body, Face, Feet (4 correct + 2 distractors on Gloves & Face).
            int correctCount = 0;
            int distractorCount = 0;
            bool hasFeet = false;
            bool hasLatexOnGloves = false;
            bool hasMaskOnFace = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MachineLockPpeItem item in root.GetComponentsInChildren<MachineLockPpeItem>(true))
                {
                    if (item.IsCorrect)
                    {
                        correctCount++;
                        if (item.Slot == PpeSlot.Feet) hasFeet = true;
                    }
                    else
                    {
                        distractorCount++;
                        if (item.Slot == PpeSlot.Gloves) hasLatexOnGloves = true;
                        if (item.Slot == PpeSlot.Face) hasMaskOnFace = true;
                    }
                }
            }
            Debug.Log($"[MachineLockValidator] PpeWiring Correct={correctCount} Distractors={distractorCount} HasFeet={hasFeet} DistractorsOnGlovesAndFace={hasLatexOnGloves && hasMaskOnFace}");
            if (correctCount != 4 || distractorCount != 2 || !hasFeet || !hasLatexOnGloves || !hasMaskOnFace)
                errors += Error("PPE locker wiring does not match ADR-0004 (4 correct slots Gloves/Body/Face/Feet + distractors on Gloves and Face).");
        }

        // ------------------------------------------------------------------ Runtime components, particles, audio, damage profile

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
                        errors += Error($"ParticleSystem {PathOf(system.transform)} has maxParticles={main.maxParticles}; the budget is 200.");
                    if (main.playOnAwake && system.gameObject.activeInHierarchy) playOnAwake++;
                }
            }

            Debug.Log($"[MachineLockValidator] ParticleSystems={systems} PlayOnAwake={playOnAwake} MaxParticlesBudget=200 PlayOnAwakeBudget=4");
            if (playOnAwake > 4)
                errors += Error($"{playOnAwake} particle systems play on awake; the budget is 4.");
        }

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
