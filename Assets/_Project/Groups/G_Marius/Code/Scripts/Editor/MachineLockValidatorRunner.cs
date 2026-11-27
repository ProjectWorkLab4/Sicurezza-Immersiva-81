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
    /// Validator of the Machine Lock scenario (batchmode and interactive Editor). Runs the shared
    /// ScenarioValidator through reflection and enforces all ADR-0002..0006 &amp; Issue #36-#42 gates:
    /// low-poly WorkshopShell (&lt;= 12k tris), total triangle budget (&lt;= 100k), ASTC overrides,
    /// zero displacement maps, zero DoorRollHousing/sausage/gas-detector legacy artifacts, uniform
    /// extinguisher/cone scales, SPLAT oil puddle quadrants, ISO 7010 signage, MachineLockExitDoor
    /// without ScenarioTrigger, Dry Perimeter &gt;= 2.5 m, electrical Sub-Station separation, 4-slot PPE,
    /// updated audio clips, particles, lights, and damage feedback serialization.
    /// </summary>
    public static class MachineLockValidatorRunner
    {
        private const string MISSION_PATH = MachineLockSceneBuilder.MISSION_PATH;
        private const string SCENE_PATH = MachineLockSceneBuilder.SCENE_PATH;
        private const string CORE_SCENE_PATH = MachineLockSceneBuilder.CORE_SCENE_PATH;
        private const string MOCK_SCENE_PATH = MachineLockSceneBuilder.MOCK_SCENE_PATH;
        private const int WORKSHOP_SHELL_TRI_BUDGET = 12000;
        private const int SCENE_TRI_BUDGET = 100000;

        /// <summary>
        /// Runs validation inside a live Editor session without calling <see cref="EditorApplication.Exit"/>.
        /// </summary>
        [MenuItem("SI81/MachineLock/Validate Scene (In Editor)")]
        public static void ValidateSceneInteractive()
        {
            ValidateSceneInEditor(out int errors, out int warnings);
            Debug.Log($"[MachineLockValidator] Interactive run complete: Errors={errors} Warnings={warnings}");
        }

        /// <summary>
        /// Runs every validation gate against SCN_MachineLock and returns the error/warning counts
        /// without terminating the Unity Editor process.
        /// </summary>
        public static bool ValidateSceneInEditor(out int errors, out int warnings)
        {
            errors = 0;
            warnings = 0;
            try
            {
                Validate(ref errors, ref warnings);
            }
            catch (Exception exception)
            {
                errors += Error("Unexpected failure: " + exception);
            }

            Debug.Log($"[MachineLockValidator] Errors={errors} Warnings={warnings}");
            return errors == 0 && warnings == 0;
        }

        /// <summary>
        /// Batchmode entry point: validates SCN_MachineLock and exits with 0 when clean, 1 otherwise.
        /// </summary>
        public static void ValidateScene()
        {
            bool clean = ValidateSceneInEditor(out _, out _);
            if (Application.isBatchMode)
                EditorApplication.Exit(clean ? 0 : 1);
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
                CheckWorkshopShellAndLegacyCleanup(scene, ref errors);
                CheckHeroPropScaleAndSplatPuddle(scene, ref errors);
                CheckIsoSignagePresence(scene, ref errors);
                CheckTextureAstcAndDisplacement(scene, ref errors);
                CheckPrefabs(ref errors);
                CheckTriangleBudget(scene, ref errors);
                CheckLights(scene, ref errors);
                CheckLabelAnchors(scene, ref errors);
                CheckDryPerimeter(scene, ref errors);
                CheckWalkableFloorContinuity(scene, ref errors);
                CheckElectricalAndPpeWiring(scene, ref errors);
                CheckRuntimeComponents(scene, ref errors);
                CheckParticles(scene, ref errors);
                CheckAudioClips(scene, ref errors);
                CheckDamageProfile(ref errors);
                CheckDamageFeedbackSerialization(scene, ref errors);
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

        // ------------------------------------------------------------------ Spawn, scenario, build settings

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
                errors += Error($"MNG_SpawnPoint yaw is {spawn.transform.eulerAngles.y:0.000} deg instead of {MachineLockSceneBuilder.SPAWN_EULER.y} deg.");
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

        // ------------------------------------------------------------------ Workshop shell, hero assets, signage & ASTC

        /// <summary>
        /// Enforces ADR-0005 workshop shell triangle budget (&lt;= 12,000 tris) and confirms legacy
        /// high-poly building, roll-up housing, and pixel-font banner/plaque artifacts are absent.
        /// </summary>
        private static void CheckWorkshopShellAndLegacyCleanup(Scene scene, ref int errors)
        {
            Transform shell = FindDeep(scene, "MESH_WorkshopShell");
            if (shell == null)
            {
                errors += Error("MESH_WorkshopShell is missing from SCN_MachineLock (ADR-0005).");
                return;
            }

            long shellTris = 0;
            int renderers = 0;
            foreach (MeshFilter mf in shell.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                renderers++;
                shellTris += mf.sharedMesh.triangles.Length / 3;
            }

            Debug.Log($"[MachineLockValidator] WorkshopShell Renderers={renderers} Tris={shellTris} Budget={WORKSHOP_SHELL_TRI_BUDGET}");
            if (renderers < 1)
                errors += Error("MESH_WorkshopShell has no rendered meshes.");
            if (shellTris > WORKSHOP_SHELL_TRI_BUDGET)
                errors += Error($"MESH_WorkshopShell triangle count is {shellTris}, above the {WORKSHOP_SHELL_TRI_BUDGET} budget.");

            // Legacy artifacts removed in Issue #36 must be absent from the scene.
            string[] forbiddenLegacyNames =
            {
                "PFB_OldIndustrialBuilding",
                "MESH_DoorRollHousing",
                "MESH_SignStampaggio",
                "MESH_SignQuadroGenerale",
                "MESH_SignDpi",
                "MESH_SignKitAntispandimento",
                "MESH_SignUscita",
                "MESH_PlaqueAlarm",
                "MESH_PlaqueValveFront",
                "MESH_PlaqueSpillKit",
                "MESH_PlaqueExtinguisher",
                "MESH_PlaqueExitHeader"
            };
            foreach (string legacy in forbiddenLegacyNames)
            {
                if (FindDeep(scene, legacy) != null)
                    errors += Error($"Legacy object '{legacy}' must be removed in Issue #36.");
            }
        }

        /// <summary>
        /// Verifies uniform non-squashed local scales on the water extinguisher and all four cones,
        /// and checks that the organic four-quadrant oil puddle binds <c>MAT_Oil</c>.
        /// </summary>
        private static void CheckHeroPropScaleAndSplatPuddle(Scene scene, ref int errors)
        {
            // Verify Water Extinguisher & Traffic Cones have uniform non-squashed local scale.
            Transform ext = FindDeep(scene, "MESH_ExtinguisherBody");
            if (ext == null)
                errors += Error("MESH_ExtinguisherBody not found inside MESH_WaterExtinguisher.");
            else if (!IsNearlyUniform(ext.localScale))
                errors += Error($"MESH_ExtinguisherBody has squashed non-uniform scale {ext.localScale}.");

            int conesChecked = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MachineLockCone cone in root.GetComponentsInChildren<MachineLockCone>(true))
                {
                    Transform body = cone.transform.Find("MESH_ConeBody");
                    if (body == null)
                    {
                        errors += Error($"Cone {cone.name} is missing MESH_ConeBody.");
                        continue;
                    }
                    conesChecked++;
                    if (!IsNearlyUniform(body.localScale))
                        errors += Error($"Cone {cone.name}/MESH_ConeBody has squashed non-uniform scale {body.localScale}.");
                }
            }
            if (conesChecked != 4)
                errors += Error($"Expected 4 MESH_ConeBody checks, found {conesChecked}.");

            // Verify organic oil puddle quadrants exist and bind glossy MAT_Oil.
            string[] quadNames = { "MESH_PuddleQuadrantNE", "MESH_PuddleQuadrantNW", "MESH_PuddleQuadrantSE", "MESH_PuddleQuadrantSW" };
            long splatTris = 0;
            foreach (string qName in quadNames)
            {
                Transform q = FindDeep(scene, qName);
                if (q == null)
                {
                    errors += Error($"Missing organic oil puddle quadrant: {qName}.");
                    continue;
                }
                MeshFilter mf = q.GetComponentInChildren<MeshFilter>(true);
                MeshRenderer mr = q.GetComponentInChildren<MeshRenderer>(true);
                if (mf == null || mf.sharedMesh == null || mr == null)
                {
                    errors += Error($"Oil puddle quadrant {qName} has no rendered mesh.");
                    continue;
                }
                splatTris += mf.sharedMesh.triangles.Length / 3;
                if (mr.sharedMaterial == null || mr.sharedMaterial.name != "MAT_Oil")
                    errors += Error($"Oil puddle quadrant {qName} must use MAT_Oil (actual: {mr.sharedMaterial?.name ?? "null"}).");
            }
            Debug.Log($"[MachineLockValidator] SplatOilPuddle Quadrants=4 Tris={splatTris}");
            if (splatTris < 200)
                errors += Error($"Oil puddle triangle count ({splatTris}) looks like legacy boxes instead of MESH_OilPuddleSplat.");
        }

        /// <summary>
        /// Verifies that every UNI EN ISO 7010 safety sign defined in
        /// <see cref="MachineLockSceneBuilder.ISO_SIGN_KEYS"/> is placed in the scene.
        /// </summary>
        private static void CheckIsoSignagePresence(Scene scene, ref int errors)
        {
            int found = 0;
            foreach (string key in MachineLockSceneBuilder.ISO_SIGN_KEYS)
            {
                string signObjectName = "MESH_IsoSign_" + key;
                if (FindDeep(scene, signObjectName) != null) found++;
                else errors += Error($"Missing UNI EN ISO 7010 safety sign in SCN_MachineLock: {signObjectName}.");
            }
            Debug.Log($"[MachineLockValidator] Iso7010Signs={found}/{MachineLockSceneBuilder.ISO_SIGN_KEYS.Length}");
        }

        /// <summary>
        /// Enforces Quest 3 Android ASTC overrides (&lt;= 2048) across all scenario PBR, signage, and
        /// plaque textures, and guarantees no scene material binds a displacement or legacy sausage map.
        /// </summary>
        private static void CheckTextureAstcAndDisplacement(Scene scene, ref int errors)
        {
            var requiredTextures = new List<string>(MachineLockSceneBuilder.PBR_TEXTURE_FILES.Length
                + MachineLockSceneBuilder.ISO_SIGN_KEYS.Length
                + MachineLockSceneBuilder.METAL_PLAQUE_KEYS.Length);
            foreach (string file in MachineLockSceneBuilder.PBR_TEXTURE_FILES)
                requiredTextures.Add($"{MachineLockSceneBuilder.TEXTURE_DIR}/{file}");
            foreach (string key in MachineLockSceneBuilder.ISO_SIGN_KEYS)
                requiredTextures.Add($"{MachineLockSceneBuilder.TEXTURE_DIR}/TEX_IsoSign_{key}.png");
            foreach (string key in MachineLockSceneBuilder.METAL_PLAQUE_KEYS)
                requiredTextures.Add($"{MachineLockSceneBuilder.TEXTURE_DIR}/TEX_Plaque_{key}.png");

            int checkedCount = 0;
            foreach (string path in requiredTextures)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    errors += Error($"Expected PBR/signage texture missing: {path}");
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

            // Ensure no rendered material references a displacement or sausage texture.
            int badTextureRefs = 0;
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
                            if (tex == null) continue;
                            if (tex.name.IndexOf("Displacement", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                badTextureRefs++;
                                errors += Error($"Material {mat.name} references displacement texture {tex.name} on Quest 3.");
                            }
                            if (tex.name.IndexOf("SAUSAGE", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                badTextureRefs++;
                                errors += Error($"Material {mat.name} still references legacy sausage texture {tex.name}.");
                            }
                        }
                    }
                }
            }

            Debug.Log($"[MachineLockValidator] AstcTexturesChecked={checkedCount}/{requiredTextures.Count} BadTextureRefs={badTextureRefs}");
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
            Debug.Log($"[MachineLockValidator] Triangles={triangles} Budget={SCENE_TRI_BUDGET}");
            if (triangles > SCENE_TRI_BUDGET)
                errors += Error($"Scene triangle count is {triangles}, above the {SCENE_TRI_BUDGET} budget.");
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
                float d3 = Vector3.Distance(mainSub.position, testSub.position);
                Debug.Log($"[MachineLockValidator] ElectricalSubStations Separation Main-Lock={d1:0.00}m Lock-Tester={d2:0.00}m Main-Tester={d3:0.00}m");
                if (d1 < 0.55f || d2 < 0.55f || d3 < 0.55f)
                    errors += Error($"Electrical Sub-Stations are too close (Main-Lock={d1:0.00}m, Lock-Tester={d2:0.00}m, Main-Tester={d3:0.00}m; minimum 0.55 m).");
            }

            Transform panelTransform = FindDeep(scene, "MESH_ElectricalPanel");
            MachineLockEnergyPanel energyPanel = panelTransform != null ? panelTransform.GetComponent<MachineLockEnergyPanel>() : null;
            if (energyPanel != null)
            {
                var panelSo = new SerializedObject(energyPanel);
                if (panelSo.FindProperty("mainSwitchStationRoot").objectReferenceValue == null
                    || panelSo.FindProperty("lockoutStationRoot").objectReferenceValue == null
                    || panelSo.FindProperty("testerStationRoot").objectReferenceValue == null)
                {
                    errors += Error("MachineLockEnergyPanel must wire mainSwitchStationRoot, lockoutStationRoot, and testerStationRoot for randomized slot order (Issue #43).");
                }
                if (panelSo.FindProperty("digitalMeterDisplay").objectReferenceValue == null)
                {
                    errors += Error("MachineLockEnergyPanel must wire digitalMeterDisplay on SUB_VoltageTester (Issue #45).");
                }
            }

            if (testSub != null && testSub.GetComponentInChildren<MachineLockDigitalMeterDisplay>(true) == null)
                errors += Error("SUB_VoltageTester must contain a MachineLockDigitalMeterDisplay component (Issue #45).");

            Transform doorLeafTransform = FindDeep(scene, "MESH_SteelExitDoorLeaf");
            if (doorLeafTransform == null || doorLeafTransform.GetComponent<SI81.Interaction.Interactable>() == null)
                errors += Error("MESH_SteelExitDoorLeaf must carry the exit Interactable so both full door leaf and panic bar respond cleanly (Issue #43).");

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
            int exitDoors = CountInScene<MachineLockExitDoor>(scene);
            int legacyTriggers = CountInScene<ScenarioTrigger>(scene);

            Debug.Log($"[MachineLockValidator] Runtime Press={press} Alarm={alarm} Panel={panel} " +
                      $"Extinguisher={extinguisher} Cabinet={cabinet} PpeItems={ppeItems} Valve={valve} " +
                      $"Spill={spill} Cones={cones} Absorbents={absorbents} SnapZones={snapZones} " +
                      $"ObservationTargets={observations} DamageFeedback={feedback} ExitDoors={exitDoors} LegacyTriggers={legacyTriggers}");

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
            errors += Expect(exitDoors, 1, "MachineLockExitDoor");
            errors += Expect(legacyTriggers, 0, "Legacy ScenarioTrigger");

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

        private static void CheckAudioClips(Scene scene, ref int errors)
        {
            int expected = MachineLockSceneBuilder.AUDIO_CLIPS.Length + 1;
            int loaded = 0;
            foreach (string clip in MachineLockSceneBuilder.AUDIO_CLIPS)
                loaded += CheckAudioClip(MachineLockSceneBuilder.SOUND_DIR + "/" + clip, ref errors);
            loaded += CheckAudioClip(MachineLockSceneBuilder.MUSIC_DIR + "/" + MachineLockSceneBuilder.AMBIENT_CLIP, ref errors);

            // Verify scenario audio clip wiring: SFX_ElectricShock on DamageFeedback, SFX_MechanicalButton on Press, SFX_AlarmSiren on Alarm.
            GameObject manager = FindRoot(scene, "MNG_Scenario");
            MachineLockDamageFeedback feedback = manager != null ? manager.GetComponent<MachineLockDamageFeedback>() : null;
            if (feedback != null)
            {
                var shockClip = new SerializedObject(feedback).FindProperty("shockClip").objectReferenceValue as AudioClip;
                if (shockClip == null || shockClip.name != "SFX_ElectricShock")
                    errors += Error($"MachineLockDamageFeedback.shockClip must be SFX_ElectricShock (actual: {shockClip?.name ?? "null"}).");
            }

            Transform pressTransform = FindDeep(scene, "MESH_HydraulicPress");
            MachineLockPress press = pressTransform != null ? pressTransform.GetComponent<MachineLockPress>() : null;
            if (press != null)
            {
                var clackClip = new SerializedObject(press).FindProperty("clackClip").objectReferenceValue as AudioClip;
                if (clackClip == null || clackClip.name != "SFX_MechanicalButton")
                    errors += Error($"MachineLockPress.clackClip must be SFX_MechanicalButton (actual: {clackClip?.name ?? "null"}).");
            }

            Transform alarmTransform = FindDeep(scene, "MESH_AlarmButton");
            MachineLockAlarm alarm = alarmTransform != null ? alarmTransform.GetComponent<MachineLockAlarm>() : null;
            if (alarm != null)
            {
                var sirenSrc = new SerializedObject(alarm).FindProperty("sirenAudio").objectReferenceValue as AudioSource;
                if (sirenSrc == null || sirenSrc.clip == null || sirenSrc.clip.name != "SFX_AlarmSiren" || !sirenSrc.loop)
                    errors += Error("MachineLockAlarm.sirenAudio must loop SFX_AlarmSiren.");
            }

            Debug.Log($"[MachineLockValidator] AudioClips={loaded}/{expected} AudioWiringVerified=True");
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

            bool hasVignette = profile.TryGet(out Vignette vignette) && vignette != null;
            bool hasAdjustments = profile.TryGet(out ColorAdjustments adjustments) && adjustments != null;

            Debug.Log($"[MachineLockValidator] DamageProfile Vignette={hasVignette} ColorAdjustments={hasAdjustments}");
            if (!hasVignette)
                errors += Error("SO_MachineLockDamageProfile has no Vignette override.");
            if (!hasAdjustments)
                errors += Error("SO_MachineLockDamageProfile has no Color Adjustments override.");
        }

        /// <summary>
        /// Probes key walking coordinates across the active bay and exit vestibule to ensure solid
        /// non-trigger floor colliders cover the full locomotion surface without step-down gaps.
        /// </summary>
        private static void CheckWalkableFloorContinuity(Scene scene, ref int errors)
        {
            var floorColliders = new List<Collider>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
                {
                    if (col == null || !col.enabled || col.isTrigger) continue;
                    Bounds b = col.bounds;
                    if (b.max.y >= -0.15f && b.max.y <= 0.15f && b.size.x >= 1.0f && b.size.z >= 1.0f)
                        floorColliders.Add(col);
                }
            }

            Vector3[] probePoints =
            {
                MachineLockSceneBuilder.SPAWN_POSITION,
                new Vector3(0f, 0f, MachineLockSceneBuilder.ROOM_HALF_Z - 0.5f),
                new Vector3(0f, 0f, -(MachineLockSceneBuilder.ROOM_HALF_Z - 0.5f)),
                new Vector3(MachineLockSceneBuilder.ROOM_HALF_X - 0.5f, 0f, 0f),
                new Vector3(-MachineLockSceneBuilder.ROOM_HALF_X, 0f, 0f),
                new Vector3(-(MachineLockSceneBuilder.ROOM_HALF_X + 0.5f), 0f, 0f),
                new Vector3(-(MachineLockSceneBuilder.ROOM_HALF_X + 1.0f), 0f, 0f)
            };

            int covered = 0;
            foreach (Vector3 pt in probePoints)
            {
                bool hit = false;
                foreach (Collider col in floorColliders)
                {
                    Bounds b = col.bounds;
                    if (pt.x >= b.min.x - 0.01f && pt.x <= b.max.x + 0.01f &&
                        pt.z >= b.min.z - 0.01f && pt.z <= b.max.z + 0.01f)
                    {
                        hit = true;
                        break;
                    }
                }
                if (hit) covered++;
                else errors += Error($"No walkable floor collider covers probe point {pt}.");
            }

            Debug.Log($"[MachineLockValidator] FloorContinuity Probes={covered}/{probePoints.Length} FloorColliders={floorColliders.Count}");
        }

        /// <summary>
        /// Verifies that <see cref="MachineLockDamageFeedback"/> serializes the ADR-0003 stereo overlay
        /// material and exact fade-in/hold/fade-out timings (0.15 / 0.50 / 1.50 s).
        /// </summary>
        private static void CheckDamageFeedbackSerialization(Scene scene, ref int errors)
        {
            GameObject manager = FindRoot(scene, "MNG_Scenario");
            MachineLockDamageFeedback feedback = manager != null ? manager.GetComponent<MachineLockDamageFeedback>() : null;
            if (feedback == null)
            {
                errors += Error("MachineLockDamageFeedback is missing on MNG_Scenario.");
                return;
            }

            var so = new SerializedObject(feedback);
            float fadeIn = so.FindProperty("fadeInSeconds").floatValue;
            float hold = so.FindProperty("holdSeconds").floatValue;
            float fadeOut = so.FindProperty("fadeOutSeconds").floatValue;
            UnityEngine.Object overlayMat = so.FindProperty("overlayMaterialAsset").objectReferenceValue;

            Debug.Log($"[MachineLockValidator] DamageFeedback FadeIn={fadeIn:0.00}s Hold={hold:0.00}s FadeOut={fadeOut:0.00}s OverlayMat={(overlayMat != null ? overlayMat.name : "NULL")}");
            if (overlayMat == null)
                errors += Error("MachineLockDamageFeedback.overlayMaterialAsset is not assigned.");
            if (Mathf.Abs(fadeIn - 0.15f) > 0.02f || Mathf.Abs(hold - 0.50f) > 0.05f || Mathf.Abs(fadeOut - 1.50f) > 0.05f)
                errors += Error($"MachineLockDamageFeedback timings ({fadeIn}/{hold}/{fadeOut}) diverge from spec (0.15/0.50/1.50).");
        }

        private static bool IsNearlyUniform(Vector3 scale)
        {
            float sx = Mathf.Abs(scale.x);
            float sy = Mathf.Abs(scale.y);
            float sz = Mathf.Abs(scale.z);
            float max = Mathf.Max(sx, Mathf.Max(sy, sz));
            float min = Mathf.Min(sx, Mathf.Min(sy, sz));
            return max > 0.001f && (max - min) / max < 0.05f;
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
