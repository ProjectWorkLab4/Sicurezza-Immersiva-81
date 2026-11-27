#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SI81.Interaction;
using SI81.Rig;
using SI81.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Builds the Machine Lock scenario: the mission asset verification, config asset, damage profile,
    /// PBR materials, UNI EN ISO 7010 signage, brushed-aluminum plaques, 12 production prefabs, custom
    /// low-poly workshop shell, organic SPLAT oil puddle, and full runtime wiring (ADR-0002..0006,
    /// Issues #35-#42). Safe to re-run idempotently without duplicating scene roots or touching Shared/Home.
    /// </summary>
    public static partial class MachineLockSceneBuilder
    {
        /// <summary>Existing mission asset (verified without unnecessary edits).</summary>
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

        /// <summary>Group texture folder of the Machine Lock scenario.</summary>
        public const string TEXTURE_DIR = "Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock";
        /// <summary>Group material folder of the Machine Lock scenario.</summary>
        public const string MATERIAL_DIR = "Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock";
        /// <summary>Group Blender-authored model folder of the Machine Lock scenario.</summary>
        public const string GROUP_MODEL_DIR = "Assets/_Project/Groups/G_Marius/Art/Models/";
        /// <summary>Group prefab folder of the Machine Lock scenario.</summary>
        public const string PREFAB_DIR = "Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock";
        /// <summary>Group audio folder of the one-shot clips.</summary>
        public const string SOUND_DIR = "Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock";
        /// <summary>Group audio folder of the ambient loop.</summary>
        public const string MUSIC_DIR = "Assets/_Project/Groups/G_Marius/Audio/Music/MachineLock";
        /// <summary>Damage profile asset used by the URP damage volume (GDD §9.13).</summary>
        public const string DAMAGE_PROFILE_PATH = "Assets/_Project/Groups/G_Marius/Level/Data/SO_MachineLockDamageProfile.asset";

        /// <summary>The workshop SFX clips shipped in G_Marius (including shock, mechanical button, siren &amp; action feedback).</summary>
        public static readonly string[] AUDIO_CLIPS =
        {
            "SFX_PressLoop.wav", "SFX_PressStop.wav", "SFX_EStopClack.wav", "SFX_MechanicalButton.wav",
            "SFX_AlarmButton.wav", "SFX_AlarmSiren.wav", "SFX_BreakerOff.wav", "SFX_Lockout.wav",
            "SFX_OilLeak.wav", "SFX_ValveTurn.wav", "SFX_TesterBeep.wav", "SFX_EquipPpe.wav",
            "SFX_Snap.wav", "SFX_SteamBurst.wav", "SFX_Shock.wav", "SFX_ElectricShock.wav", "SFX_Burn.wav",
            "SFX_FeedbackPositive.wav", "SFX_FeedbackNegative.wav"
        };

        /// <summary>The ambient room tone that lives in the music folder of the group.</summary>
        public const string AMBIENT_CLIP = "SFX_AmbientFactory.wav";

        /// <summary>Stable identifiers of the six observation targets (GDD §9.11).</summary>
        public static readonly string[] OBSERVATION_NAMES =
        {
            "OBS_Press", "OBS_Thermometer", "OBS_Display", "OBS_Puddle", "OBS_Panel", "OBS_PpeCabinet"
        };

        /// <summary>Identifiers reported by the observation targets, in <see cref="OBSERVATION_NAMES"/> order.</summary>
        public static readonly string[] OBSERVATION_IDS =
        {
            "press", "thermometer", "display", "puddle", "panel", "ppe_cabinet"
        };

        /// <summary>The twelve production-named prefabs (GDD §14.2).</summary>
        public static readonly string[] PREFAB_NAMES =
        {
            "MESH_HydraulicPress",
            "MESH_EStopPedestal",
            "MESH_AlarmButton",
            "MESH_ElectricalPanel",
            "MESH_PpeCabinet",
            "MESH_SpillKit",
            "MESH_WaterExtinguisher",
            "MESH_Cone",
            "MESH_AbsorbentMat",
            "MESH_AbsorbentSock",
            "MESH_Valve",
            "MESH_IndustrialDoor_RollUp"
        };

        private const string SCENE_NAME = "SCN_MachineLock";
        private const string URP_LIT = "Universal Render Pipeline/Lit";
        private const string SHARED_TEXTURE_DIR = "Assets/_Project/Shared/Art/Textures/";
        private const string SHARED_MATERIAL_DIR = "Assets/_Project/Shared/Art/Materials/";
        private const string SHARED_MODEL_DIR = "Assets/_Project/Shared/Art/Models/";
        private const string SHARED_PREFAB_DIR = "Assets/_Project/Shared/Level/Prefabs/";

        /// <summary>Spacious stamping bay spawn point inside MESH_WorkshopShell (ADR-0005).</summary>
        public static readonly Vector3 SPAWN_POSITION = new Vector3(-7.6f, 0f, 0f);
        /// <summary>Spawn yaw facing +X into the stamping bay.</summary>
        public static readonly Vector3 SPAWN_EULER = new Vector3(0f, 90f, 0f);

        /// <summary>Playable active stamping bay half-width along X (18.0 m total).</summary>
        public const float ROOM_HALF_X = 9.0f;
        /// <summary>Playable active stamping bay half-depth along Z (11.0 m total).</summary>
        public const float ROOM_HALF_Z = 5.5f;
        /// <summary>Active stamping bay ceiling fixture elevation inside the workshop hall.</summary>
        public const float ROOM_HEIGHT = 6.2f;
        /// <summary>Minimum Dry Perimeter margin on every side around the puddle.</summary>
        public const float MIN_DRY_PERIMETER = 2.5f;
        /// <summary>Expected sparse World Label anchor count (ADR-0002).</summary>
        public const int EXPECTED_LABEL_ANCHORS = 11;

        // ------------------------------------------------------------------ Materials cache

        private static Dictionary<string, Material> heroMaterialMap;
        private static Material matWorkshopFloor;
        private static Material matWorkshopFloorBay;
        private static Material matFactoryWall;
        private static Material matWorkshopSteel;
        private static Material matCorrugatedRoof;
        private static Material matWorkshopWindow;
        private static Material matExitWarmGlow;
        private static Material matWorkbenchWood;
        private static Material matExitDoorSteel;
        private static Material matPanicBar;
        private static Material matTrafficConeBody;
        private static Material matConeReflective;
        private static Material matPressSteelGreen;
        private static Material matConcreteWallPbr;
        private static Material matPaintedSteel;
        private static Material matPolishedSteel;
        private static Material matDarkSlate;
        private static Material matCyanAccent;
        private static Material matOil;
        private static Material matEStopRed;
        private static Material matEStopYellow;
        private static Material matHmiScreen;
        private static Material matFaceShieldVisor;
        private static Material matApronRubber;
        private static Material matHeatGlove;
        private static Material matSafetyBootLeather;
        private static Material matSafetyBootSole;
        private static Material matLatex;
        private static Material matSurgicalMaskBlue;
        private static Material matFloorMarking;
        private static Material matBaselineZone;
        private static Material matBeaconRed;
        private static Material matExitBeaconIdle;
        private static Material matExitBeaconReady;
        private static Material matLedRed;
        private static Material matLedGreen;
        private static Material matGhostSnap;
        private static Material matLightFixture;
        private static Material matHmiOff;
        private static Material matLedOff;
        private static Material matParticleSoft;
        private static Material matDamageOverlay;
        private static Material matAbsorbentSock;
        private static Material matAbsorbentMat;

        // ------------------------------------------------------------------ Entry points

        /// <summary>
        /// Builds or updates the P1 shell only. Menu item and batchmode entry point.
        /// </summary>
        [MenuItem("SI81/MachineLock/Build Scene Shell")]
        public static void BuildSceneShell()
        {
            GuardPlayMode("Build Scene Shell");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new InvalidOperationException("[MachineLock] Build Scene Shell cancelled: unsaved scenes.");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TEMPLATE_PATH) == null)
                throw new InvalidOperationException($"[MachineLock] Template scene not found: {TEMPLATE_PATH}");

            EnsureFolder(CONFIG_PATH);
            EnsureFolder(SCENE_PATH);

            VerifyMissionAsset();
            MachineLockConfig config = CreateOrUpdateConfig();
            CreateSceneFromTemplate();

            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
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

        /// <summary>
        /// Builds or updates the complete Machine Lock scenario: ASTC PBR textures, ISO 7010 signage,
        /// brushed-metal plaques, 12 prefabs, custom workshop shell, organic SPLAT oil puddle, and full wiring.
        /// </summary>
        [MenuItem("SI81/MachineLock/Build Full Scene")]
        public static void BuildFullScene()
        {
            GuardPlayMode("Build Full Scene");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new InvalidOperationException("[MachineLock] Build Full Scene cancelled: unsaved scenes.");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TEMPLATE_PATH) == null)
                throw new InvalidOperationException($"[MachineLock] Template scene not found: {TEMPLATE_PATH}");

            EnsureAssetFolders();
            EnsurePbrWorkshopAndPropTextures();
            EnsureSignageAndPlaqueTextures();
            EnsureMaterials();
            EnsureFeedbackAudioClips();
            MachineLockAudioClips clips = LoadAudioClips();
            EnsurePrefabs(clips);
            VolumeProfile damageProfile = CreateOrUpdateDamageProfile();

            VerifyMissionAsset();
            MachineLockConfig config = CreateOrUpdateConfig();
            CreateSceneFromTemplate();

            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MISSION_PATH);
            config = AssetDatabase.LoadAssetAtPath<MachineLockConfig>(CONFIG_PATH);
            if (mission == null)
                throw new InvalidOperationException($"[MachineLock] Mission asset not found: {MISSION_PATH}");
            if (config == null)
                throw new InvalidOperationException($"[MachineLock] Config asset not found: {CONFIG_PATH}");

            RemoveTemplateExamples(scene);
            ConfigureScenario(scene, mission, config);
            ConfigureSpawnPoint(scene);
            RemoveBlockoutRoots(scene);
            ConfigureRoomLighting(scene);
            BuildRoom(scene);
            BuildPressArea(scene);
            BuildSafetyArea(scene);
            BuildHazardArea(scene, clips);
            BuildDelimitationArea(scene);
            BuildLayoutPatterns(scene);
            BuildObservationTargets(scene, config);
            EnsureMiscRoots(scene, clips);
            ConfigureDamageFeedback(scene, damageProfile, clips);
            WireScenarioContent(scene, config);
            PlaceLabels(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SCENE_PATH))
                throw new InvalidOperationException($"[MachineLock] Could not save {SCENE_PATH}");
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();

            ApplyBuildSettings();
            Debug.Log($"[MachineLock] Full workshop scene ready: {SCENE_PATH}. " +
                      $"{PREFAB_NAMES.Length} prefabs, " +
                      $"{AssetDatabase.FindAssets("t:Material", new[] { MATERIAL_DIR }).Length} materials, " +
                      $"{AssetDatabase.FindAssets("t:Texture2D", new[] { TEXTURE_DIR }).Length} textures, " +
                      $"{AUDIO_CLIPS.Length + 1} audio clips.");
        }

        private static void GuardPlayMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException($"[MachineLock] {operation} cannot run in Play Mode.");
        }

        // ------------------------------------------------------------------ Mission & Config assets

        private static void VerifyMissionAsset()
        {
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MISSION_PATH);
            if (mission == null)
                throw new InvalidOperationException($"[MachineLock] Mission asset not found: {MISSION_PATH}");
            if (mission.SceneName != SCENE_NAME)
                Debug.LogWarning($"[MachineLock] {MISSION_PATH} SceneName is '{mission.SceneName}' (expected '{SCENE_NAME}').");
        }

        private static MachineLockConfig CreateOrUpdateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<MachineLockConfig>(CONFIG_PATH);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MachineLockConfig>();
                AssetDatabase.CreateAsset(config, CONFIG_PATH);
                Debug.Log($"[MachineLock] Created {CONFIG_PATH} with the GDD §14.4 defaults.");
            }
            config.missingEStopSafetyPenalty = 20;
            config.missingEStopDecisionPenalty = 40;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ Scene shell

        private static void CreateSceneFromTemplate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) != null) return;
            if (!AssetDatabase.CopyAsset(TEMPLATE_PATH, SCENE_PATH))
                throw new InvalidOperationException($"[MachineLock] Could not copy {TEMPLATE_PATH} to {SCENE_PATH}");
            AssetDatabase.Refresh();
        }

        private static void RemoveTemplateExamples(Scene scene)
        {
            string[] exampleNames =
            {
                "MESH_ExampleSign", "MESH_ExampleTool", "MESH_ExampleRisk", "MNG_DecisionTrigger", "MESH_Floor"
            };
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

            ScenarioController baseController = manager.GetComponent<ScenarioController>();
            if (baseController != null && !(baseController is MachineLockScenario))
                UnityEngine.Object.DestroyImmediate(baseController);

            var scenario = manager.GetComponent<MachineLockScenario>();
            if (scenario == null) scenario = manager.AddComponent<MachineLockScenario>();

            var so = new SerializedObject(scenario);
            SetObjectReference(so, "mission", mission);
            SetObjectReference(so, "config", config);
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

        // ------------------------------------------------------------------ Textures

        private static void EnsureAssetFolders()
        {
            EnsureFolder(CONFIG_PATH);
            EnsureFolder(SCENE_PATH);
            EnsureFolder(TEXTURE_DIR + "/TEX_Placeholder.png");
            EnsureFolder(MATERIAL_DIR + "/MAT_Placeholder.mat");
            EnsureFolder(PREFAB_DIR + "/MESH_Placeholder.prefab");
        }

        private static void CopySharedTextureToGroup(string source, string destinationName, TextureImporterType type,
            bool linear, int androidMaxSize = 1024)
        {
            if (!File.Exists(source))
                throw new InvalidOperationException($"[MachineLock] Shared texture not found: {source}");

            string destination = TEXTURE_DIR + "/" + destinationName;
            bool needsCopy = !File.Exists(destination);
            if (!needsCopy)
            {
                try { needsCopy = new FileInfo(destination).Length != new FileInfo(source).Length; }
                catch (IOException) { needsCopy = false; }
            }
            if (needsCopy)
            {
                try { File.Copy(source, destination, true); }
                catch (IOException) { }
            }
            ConfigurePbrTexture(destination, type, linear, 1024, androidMaxSize);
        }

        private static Texture2D LoadGroupTexture(string fileName)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_DIR + "/" + fileName);
            if (texture == null)
                throw new InvalidOperationException($"[MachineLock] Texture not found: {TEXTURE_DIR}/{fileName}");
            return texture;
        }

        // ------------------------------------------------------------------ Materials

        private static void EnsureMaterials()
        {
            // Workshop Shell PBR Materials (PolyHaven CC0)
            matWorkshopFloor = PbrLitMaterial("MAT_WorkshopFloor", new Color(0.76f, 0.75f, 0.72f), 0.04f, 0.24f,
                "TEX_WorkshopFloor_Albedo.jpg", "TEX_WorkshopFloor_Normal.png", "TEX_WorkshopFloor_Rough.jpg",
                Vector2.one, 0.75f);

            matWorkshopFloorBay = PbrLitMaterial("MAT_WorkshopFloorBay", new Color(0.76f, 0.75f, 0.72f), 0.04f, 0.24f,
                "TEX_WorkshopFloor_Albedo.jpg", "TEX_WorkshopFloor_Normal.png", "TEX_WorkshopFloor_Rough.jpg",
                new Vector2(6f, 3.6f), 0.75f);

            matFactoryWall = PbrLitMaterial("MAT_FactoryWall", new Color(0.84f, 0.82f, 0.79f), 0.02f, 0.18f,
                "TEX_FactoryWall_Albedo.jpg", "TEX_FactoryWall_Normal.png", "TEX_FactoryWall_Rough.jpg",
                Vector2.one, 0.8f);

            matConcreteWallPbr = PbrLitMaterial("MAT_ConcreteWall_PBR", new Color(0.74f, 0.75f, 0.73f), 0f, 0.18f,
                "TEX_ConcreteWall009_Albedo.jpg", "TEX_ConcreteWall009_Normal.exr", "TEX_ConcreteWall009_Rough.jpg",
                Vector2.one, 0.85f);

            matWorkshopSteel = PbrLitMaterial("MAT_WorkshopSteel", new Color(0.32f, 0.35f, 0.39f), 0.55f, 0.42f,
                "TEX_MetalPlate_Albedo.jpg", "TEX_MetalPlate_Normal.png", "TEX_MetalPlate_Rough.jpg",
                Vector2.one, 0.65f);

            matCorrugatedRoof = PbrLitMaterial("MAT_CorrugatedRoof", new Color(0.46f, 0.48f, 0.50f), 0.38f, 0.30f,
                "TEX_CorrugatedIron_Albedo.jpg", "TEX_CorrugatedIron_Normal.png", "TEX_CorrugatedIron_Rough.jpg",
                new Vector2(1.4f, 1.4f), 0.75f);

            matWorkbenchWood = PbrLitMaterial("MAT_WorkbenchWood", new Color(0.76f, 0.62f, 0.46f), 0f, 0.28f,
                "TEX_WorkbenchWood_Albedo.jpg", "TEX_WorkbenchWood_Normal.png", "TEX_WorkbenchWood_Rough.jpg",
                Vector2.one, 0.7f);

            matPressSteelGreen = PbrLitMaterial("MAT_PressSteel_Green", new Color(0.74f, 0.84f, 0.76f), 0.35f, 0.44f,
                "TEX_GreenMetalRust_Albedo.jpg", "TEX_GreenMetalRust_Normal.exr", "TEX_GreenMetalRust_Rough.jpg",
                Vector2.one, 0.75f);

            matExitDoorSteel = LitMaterial("MAT_ExitDoorSteel", new Color(0.68f, 0.72f, 0.75f), 0.25f, 0.42f);
            matExitDoorSteel.SetTexture("_BaseMap", null);
            matExitDoorSteel.SetTexture("_MainTex", null);
            matExitDoorSteel.SetTexture("_BumpMap", null);
            matExitDoorSteel.SetTexture("_MetallicGlossMap", null);
            matExitDoorSteel.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(matExitDoorSteel);

            matWorkshopWindow = LitMaterial("MAT_WorkshopWindow", new Color(0.72f, 0.84f, 0.92f, 0.48f), 0.05f, 0.90f,
                new Color(0.46f, 0.56f, 0.64f));
            ConfigureTransparent(matWorkshopWindow, 0.48f, new Color(0.46f, 0.56f, 0.64f));

            matExitWarmGlow = LitMaterial("MAT_ExitWarmGlow", new Color(0.96f, 0.91f, 0.80f), 0f, 0.20f,
                new Color(0.72f, 0.64f, 0.48f));

            matPanicBar = LitMaterial("MAT_PanicBar", new Color(0.06f, 0.68f, 0.24f), 0.15f, 0.56f,
                new Color(0.02f, 0.14f, 0.05f));

            matTrafficConeBody = LitMaterial("MAT_TrafficCone_Body", new Color(0.96f, 0.30f, 0.03f), 0.02f, 0.48f);
            matConeReflective = LitMaterial("MAT_ConeReflective", new Color(0.95f, 0.96f, 0.98f), 0.10f, 0.78f,
                new Color(0.16f, 0.16f, 0.18f));

            // Metals, slate & safety accents
            matPaintedSteel = CopyMaterial(SHARED_MATERIAL_DIR + "MAT_OfficePropWhiteMetallic.mat", "MAT_PaintedSteel");
            matPolishedSteel = CopyMaterial(SHARED_MATERIAL_DIR + "MAT_OfficePropPolishedMetal.mat", "MAT_PolishedSteel");
            matDarkSlate = LitMaterial("MAT_DarkSlate", new Color(0.14f, 0.16f, 0.19f), 0.36f, 0.44f);
            matCyanAccent = LitMaterial("MAT_CyanAccent", new Color(0.06f, 0.48f, 0.78f), 0.22f, 0.52f);
            matEStopRed = LitMaterial("MAT_EmergencyStop_Red", new Color(0.78f, 0.05f, 0.04f), 0.06f, 0.55f);
            matEStopYellow = LitMaterial("MAT_EmergencyStop_Yellow", new Color(0.96f, 0.74f, 0.04f), 0.06f, 0.46f);
            matHmiScreen = LitMaterial("MAT_HMI_Screen", new Color(0.02f, 0.02f, 0.02f), 0f, 0.6f,
                new Color(0.95f, 0.14f, 0.04f) * 1.6f);

            // Glossy dark wet hydraulic oil with subtle ripple normal map for organic MESH_OilPuddleSplat puddle
            matOil = LitMaterial("MAT_Oil", new Color(0.014f, 0.010f, 0.007f), 0.02f, 0.82f);
            AssignOptionalTexture(matOil, "_BumpMap", TEXTURE_DIR + "/TEX_OilRipple_Normal.png", true);
            SetFloat(matOil, "_BumpScale", 0.38f);
            matOil.SetTextureScale("_BumpMap", new Vector2(2.5f, 2.5f));
            EditorUtility.SetDirty(matOil);

            // PPE & quilted absorbent mat / fabric-sleeved absorbent sock (zero sausage texture)
            matApronRubber = LitMaterial("MAT_Apron_Rubber", new Color(0.09f, 0.12f, 0.14f), 0.08f, 0.42f);
            matHeatGlove = LitMaterial("MAT_HeatGlove", new Color(0.92f, 0.34f, 0.05f), 0.10f, 0.28f);
            AssignOptionalTexture(matHeatGlove, "_BumpMap", TEXTURE_DIR + "/TEX_Glove_Normal.png", true);
            matSafetyBootLeather = LitMaterial("MAT_SafetyBoot_Leather", new Color(0.15f, 0.12f, 0.10f), 0.08f, 0.32f);
            matSafetyBootSole = LitMaterial("MAT_SafetyBoot_Sole", new Color(0.88f, 0.56f, 0.05f), 0.05f, 0.35f);
            matLatex = LitMaterial("MAT_Latex", new Color(0.84f, 0.93f, 0.99f), 0f, 0.86f);
            AssignOptionalTexture(matLatex, "_BumpMap", TEXTURE_DIR + "/TEX_Glove_Normal.png", true);
            SetFloat(matLatex, "_BumpScale", 0.65f);
            matSurgicalMaskBlue = LitMaterial("MAT_SurgicalMask_Blue", new Color(0.56f, 0.84f, 0.95f), 0f, 0.22f);

            matAbsorbentMat = LitMaterial("MAT_AbsorbentMat", new Color(0.94f, 0.93f, 0.88f), 0f, 0.14f);
            AssignOptionalTexture(matAbsorbentMat, "_BaseMap", TEXTURE_DIR + "/TEX_AbsorbentMat_Albedo.png");
            AssignOptionalTexture(matAbsorbentMat, "_MainTex", TEXTURE_DIR + "/TEX_AbsorbentMat_Albedo.png");
            AssignOptionalTexture(matAbsorbentMat, "_BumpMap", TEXTURE_DIR + "/TEX_AbsorbentMat_QuiltedNormal.png", true);
            SetFloat(matAbsorbentMat, "_BumpScale", 0.85f);
            EditorUtility.SetDirty(matAbsorbentMat);

            matAbsorbentSock = LitMaterial("MAT_AbsorbentSock", new Color(0.92f, 0.90f, 0.84f), 0f, 0.16f);
            AssignOptionalTexture(matAbsorbentSock, "_BaseMap", TEXTURE_DIR + "/TEX_AbsorbentSock_FabricAlbedo.png");
            AssignOptionalTexture(matAbsorbentSock, "_MainTex", TEXTURE_DIR + "/TEX_AbsorbentSock_FabricAlbedo.png");
            AssignOptionalTexture(matAbsorbentSock, "_BumpMap", TEXTURE_DIR + "/TEX_AbsorbentSock_FabricNormal.png", true);
            SetFloat(matAbsorbentSock, "_BumpScale", 0.85f);
            EditorUtility.SetDirty(matAbsorbentSock);

            // Indicators, markings & VFX
            matFloorMarking = LitMaterial("MAT_FloorSafetyMarking_Yellow", new Color(0.94f, 0.72f, 0.05f), 0f, 0.32f,
                new Color(0.28f, 0.20f, 0.01f));
            matBeaconRed = LitMaterial("MAT_BeaconRed", new Color(0.55f, 0.02f, 0.02f), 0f, 0.75f,
                new Color(0.9f, 0.05f, 0.03f) * 1.4f);
            matExitBeaconIdle = LitMaterial("MAT_ExitBeacon_Idle", new Color(0.08f, 0.28f, 0.14f), 0f, 0.5f,
                new Color(0.08f, 0.36f, 0.16f));
            matExitBeaconReady = LitMaterial("MAT_ExitBeacon_Ready", new Color(0.08f, 0.92f, 0.32f), 0f, 0.65f,
                new Color(0.15f, 1.85f, 0.45f));
            matLedRed = LitMaterial("MAT_LedRed", new Color(0.45f, 0.02f, 0.02f), 0f, 0.7f,
                new Color(1f, 0.05f, 0.03f) * 2.2f);
            matLedGreen = LitMaterial("MAT_LedGreen", new Color(0.02f, 0.35f, 0.08f), 0f, 0.7f,
                new Color(0.05f, 1f, 0.2f) * 2.2f);
            matLightFixture = LitMaterial("MAT_LightFixture", new Color(0.92f, 0.94f, 0.90f), 0f, 0.4f,
                new Color(1f, 0.97f, 0.9f) * 2.2f);
            matHmiOff = LitMaterial("MAT_HMI_Off", new Color(0.015f, 0.015f, 0.018f), 0f, 0.12f);
            matLedOff = LitMaterial("MAT_LedOff", new Color(0.08f, 0.08f, 0.09f), 0f, 0.35f);
            matParticleSoft = ParticleMaterial("MAT_ParticleSoft", new Color(0.66f, 0.66f, 0.64f, 0.5f));

            matFaceShieldVisor = LitMaterial("MAT_FaceShield_Visor", new Color(0.55f, 0.82f, 0.96f, 0.28f), 0f, 0.95f);
            ConfigureTransparent(matFaceShieldVisor, 0.28f);
            matGhostSnap = LitMaterial("MAT_GhostSnap", new Color(0.16f, 0.96f, 0.45f, 0.48f), 0f, 0.5f);
            ConfigureTransparent(matGhostSnap, 0.48f, new Color(0.12f, 0.95f, 0.35f));
            matBaselineZone = LitMaterial("MAT_BaselineZone", new Color(0.98f, 0.84f, 0.12f, 0.38f), 0f, 0.4f);
            ConfigureTransparent(matBaselineZone, 0.38f, new Color(0.68f, 0.58f, 0.06f));

            matDamageOverlay = EnsureDamageOverlayMaterial();

            EnsureSignageAndPlaqueMaterials();

            // Lookup table for binding Blender FBX material slots by name.
            heroMaterialMap = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase)
            {
                { "MAT_WorkshopFloor", matWorkshopFloor },
                { "MAT_ConcreteWall_PBR", matConcreteWallPbr },
                { "MAT_FactoryWall", matFactoryWall },
                { "MAT_WorkshopSteel", matWorkshopSteel },
                { "MAT_CorrugatedRoof", matCorrugatedRoof },
                { "MAT_WorkshopWindow", matWorkshopWindow },
                { "MAT_ExitWarmGlow", matExitWarmGlow },
                { "MAT_WorkbenchWood", matWorkbenchWood },
                { "MAT_ExitDoorSteel", matExitDoorSteel },
                { "MAT_PanicBar", matPanicBar },
                { "MAT_PanicBarGreen", matPanicBar },
                { "MAT_TrafficCone_Body", matTrafficConeBody },
                { "MAT_TrafficCone_Orange", matTrafficConeBody },
                { "MAT_ConeReflective", matConeReflective },
                { "MAT_PressSteel_Green", matPressSteelGreen },
                { "MAT_DarkSlate", matDarkSlate },
                { "MAT_PolishedSteel", matPolishedSteel },
                { "MAT_PaintedSteel", matPaintedSteel },
                { "MAT_CyanAccent", matCyanAccent },
                { "MAT_EmergencyStop_Red", matEStopRed },
                { "MAT_EmergencyStop_Yellow", matEStopYellow },
                { "MAT_Apron_Rubber", matApronRubber },
                { "MAT_HeatGlove", matHeatGlove },
                { "MAT_SafetyBoot_Leather", matSafetyBootLeather },
                { "MAT_SafetyBoot_Sole", matSafetyBootSole },
                { "MAT_FaceShield_Visor", matFaceShieldVisor },
                { "MAT_Latex", matLatex },
                { "MAT_SurgicalMask_Blue", matSurgicalMaskBlue },
                { "MAT_AbsorbentMat", matAbsorbentMat },
                { "MAT_AbsorbentSock", matAbsorbentSock },
                { "MAT_Oil", matOil },
                { "MAT_HMI_Off", matHmiOff }
            };

            AssetDatabase.SaveAssets();
        }

        private static Material PbrLitMaterial(string name, Color tint, float metallic, float smoothness,
            string albedoFile, string normalFile, string roughFile, Vector2 tiling, float bumpScale = 0.8f)
        {
            Material mat = LitMaterial(name, tint, metallic, smoothness);
            AssignOptionalTexture(mat, "_BaseMap", TEXTURE_DIR + "/" + albedoFile);
            AssignOptionalTexture(mat, "_MainTex", TEXTURE_DIR + "/" + albedoFile);
            AssignOptionalTexture(mat, "_BumpMap", TEXTURE_DIR + "/" + normalFile, true);
            AssignOptionalTexture(mat, "_MetallicGlossMap", TEXTURE_DIR + "/" + roughFile);
            SetFloat(mat, "_BumpScale", bumpScale);
            mat.SetTextureScale("_BaseMap", tiling);
            mat.SetTextureScale("_MainTex", tiling);
            mat.SetTextureScale("_BumpMap", tiling);
            mat.SetTextureScale("_MetallicGlossMap", tiling);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AssignOptionalTexture(Material material, string property, string assetPath, bool isNormal = false)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) return;
            material.SetTexture(property, tex);
            if (isNormal) material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
        }

        private static Material EnsureDamageOverlayMaterial()
        {
            string path = MATERIAL_DIR + "/MAT_DamageOverlay.mat";
            Shader shader = Shader.Find("SI81/MachineLock/DamageOverlay");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = "MAT_DamageOverlay" };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null)
            {
                mat.shader = shader;
            }
            SetColor(mat, "_Color", new Color(0.88f, 0.04f, 0.04f, 1f));
            SetFloat(mat, "_Intensity", 0f);
            SetFloat(mat, "_InnerRadius", 0.28f);
            SetFloat(mat, "_OuterRadius", 0.72f);
            SetFloat(mat, "_FullScreenFade", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CopyMaterial(string source, string name)
        {
            string destination = MATERIAL_DIR + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(destination) == null)
            {
                if (!AssetDatabase.CopyAsset(source, destination))
                    throw new InvalidOperationException($"[MachineLock] Could not copy {source} to {destination}");
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(destination);
            if (material == null)
                throw new InvalidOperationException($"[MachineLock] Material not found: {destination}");
            return material;
        }

        private static Material LitMaterial(string name, Color baseColor, float metallic, float smoothness, Color? emission = null)
        {
            string path = MATERIAL_DIR + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(URP_LIT)) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = Shader.Find(URP_LIT);
            }

            SetColor(material, "_BaseColor", baseColor);
            SetColor(material, "_Color", baseColor);
            SetFloat(material, "_Metallic", metallic);
            SetFloat(material, "_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                SetColor(material, "_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Creates or updates a transparent URP particle material configured for soft-alpha smoke, steam, and spray.</summary>
        private static Material ParticleMaterial(string name, Color baseColor)
        {
            string path = MATERIAL_DIR + "/" + name + ".mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find(URP_LIT);

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            SetColor(material, "_BaseColor", baseColor);
            SetColor(material, "_Color", baseColor);
            SetFloat(material, "_Surface", 1f);
            SetFloat(material, "_Blend", 0f);
            SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(material, "_ZWrite", 0f);
            SetFloat(material, "_SoftParticlesEnabled", 1f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (material.HasProperty("_SoftParticlesEnabled")) material.EnableKeyword("_SOFTPARTICLES_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTransparent(Material material, float alpha, Color? emission = null)
        {
            SetFloat(material, "_Surface", 1f);
            SetFloat(material, "_Blend", 0f);
            SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(material, "_ZWrite", 0f);
            SetFloat(material, "_AlphaClip", 0f);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                SetColor(material, "_EmissionColor", emission.Value);
            }
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
        }

        private static void EnsureSignMaterial(string materialName, string textureFile, float emissionScale = 0.25f,
            float metallic = 0f, float smoothness = 0.30f)
        {
            Texture2D texture = LoadGroupTexture(textureFile);
            var material = LitMaterial(materialName, Color.white, metallic, smoothness, Color.white * emissionScale);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_EmissionMap", texture);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
        }

        private static void SetColor(Material material, string property, Color color)
        {
            if (material.HasProperty(property)) material.SetColor(property, color);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }

        // ------------------------------------------------------------------ Damage profile

        /// <summary>Creates or updates the URP VolumeProfile holding the Vignette and ColorAdjustments sub-assets for critical damage feedback.</summary>
        private static VolumeProfile CreateOrUpdateDamageProfile()
        {
            EnsureFolder(DAMAGE_PROFILE_PATH);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DAMAGE_PROFILE_PATH);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, DAMAGE_PROFILE_PATH);
            }

            if (!profile.TryGet(out Vignette vignette) || vignette == null)
            {
                vignette = ScriptableObject.CreateInstance<Vignette>();
                vignette.name = "Vignette";
                vignette.hideFlags = HideFlags.HideInHierarchy;
                profile.components.Add(vignette);
                AssetDatabase.AddObjectToAsset(vignette, profile);
            }
            vignette.active = true;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.85f, 0.05f, 0.05f, 1f);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.55f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.4f;

            if (!profile.TryGet(out ColorAdjustments adjustments) || adjustments == null)
            {
                adjustments = ScriptableObject.CreateInstance<ColorAdjustments>();
                adjustments.name = "ColorAdjustments";
                adjustments.hideFlags = HideFlags.HideInHierarchy;
                profile.components.Add(adjustments);
                AssetDatabase.AddObjectToAsset(adjustments, profile);
            }
            adjustments.active = true;
            adjustments.colorFilter.overrideState = true;
            adjustments.colorFilter.value = Color.white;
            adjustments.saturation.overrideState = true;
            adjustments.saturation.value = -15f;

            EditorUtility.SetDirty(vignette);
            EditorUtility.SetDirty(adjustments);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // ------------------------------------------------------------------ Audio

        private sealed class MachineLockAudioClips
        {
            public AudioClip PressLoop;
            public AudioClip PressStop;
            public AudioClip EStopClack;
            public AudioClip MechanicalButton;
            public AudioClip AlarmButton;
            public AudioClip AlarmSiren;
            public AudioClip BreakerOff;
            public AudioClip Lockout;
            public AudioClip OilLeak;
            public AudioClip ValveTurn;
            public AudioClip TesterBeep;
            public AudioClip EquipPpe;
            public AudioClip Snap;
            public AudioClip SteamBurst;
            public AudioClip ElectricShock;
            public AudioClip Burn;
            public AudioClip FeedbackPositive;
            public AudioClip FeedbackNegative;
            public AudioClip AmbientFactory;
        }

        private static void EnsureFeedbackAudioClips()
        {
            EnsureSynthWavClip(SOUND_DIR + "/SFX_FeedbackPositive.wav", MachineLockActionFeedback.BuildPositiveSamples(), 44100);
            EnsureSynthWavClip(SOUND_DIR + "/SFX_FeedbackNegative.wav", MachineLockActionFeedback.BuildNegativeSamples(), 44100);
        }

        private static void EnsureSynthWavClip(string assetPath, float[] samples, int sampleRate)
        {
            EnsureFolder(assetPath);
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath) && AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath) != null)
                return;

            WriteMonoPcm16Wav(fullPath, samples, sampleRate);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void WriteMonoPcm16Wav(string fullPath, float[] samples, int sampleRate)
        {
            const short channels = 1;
            const short bitsPerSample = 16;
            int dataBytes = samples.Length * channels * (bitsPerSample / 8);
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            short blockAlign = (short)(channels * (bitsPerSample / 8));

            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                for (int i = 0; i < samples.Length; i++)
                {
                    short pcm = (short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                    writer.Write(pcm);
                }
            }
        }

        private static MachineLockAudioClips LoadAudioClips()
        {
            return new MachineLockAudioClips
            {
                PressLoop = LoadAudioClip(SOUND_DIR, "SFX_PressLoop.wav"),
                PressStop = LoadAudioClip(SOUND_DIR, "SFX_PressStop.wav"),
                EStopClack = LoadAudioClip(SOUND_DIR, "SFX_EStopClack.wav"),
                MechanicalButton = LoadAudioClip(SOUND_DIR, "SFX_MechanicalButton.wav"),
                AlarmButton = LoadAudioClip(SOUND_DIR, "SFX_AlarmButton.wav"),
                AlarmSiren = LoadAudioClip(SOUND_DIR, "SFX_AlarmSiren.wav"),
                BreakerOff = LoadAudioClip(SOUND_DIR, "SFX_BreakerOff.wav"),
                Lockout = LoadAudioClip(SOUND_DIR, "SFX_Lockout.wav"),
                OilLeak = LoadAudioClip(SOUND_DIR, "SFX_OilLeak.wav"),
                ValveTurn = LoadAudioClip(SOUND_DIR, "SFX_ValveTurn.wav"),
                TesterBeep = LoadAudioClip(SOUND_DIR, "SFX_TesterBeep.wav"),
                EquipPpe = LoadAudioClip(SOUND_DIR, "SFX_EquipPpe.wav"),
                Snap = LoadAudioClip(SOUND_DIR, "SFX_Snap.wav"),
                SteamBurst = LoadAudioClip(SOUND_DIR, "SFX_SteamBurst.wav"),
                ElectricShock = LoadAudioClip(SOUND_DIR, "SFX_ElectricShock.wav"),
                Burn = LoadAudioClip(SOUND_DIR, "SFX_Burn.wav"),
                FeedbackPositive = LoadAudioClip(SOUND_DIR, "SFX_FeedbackPositive.wav"),
                FeedbackNegative = LoadAudioClip(SOUND_DIR, "SFX_FeedbackNegative.wav"),
                AmbientFactory = LoadAudioClip(MUSIC_DIR, AMBIENT_CLIP)
            };
        }

        private static AudioClip LoadAudioClip(string directory, string fileName)
        {
            string path = directory + "/" + fileName;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null) return clip;

            string message = $"[MachineLock] Audio clip not found: {path}";
            if (Application.isBatchMode)
            {
                Debug.LogWarning(message);
                return null;
            }
            throw new InvalidOperationException(message);
        }

        // ------------------------------------------------------------------ Serialized wiring helpers

        private static void SetRef(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"[MachineLock] Serialized property not found: {target.GetType().Name}.{propertyName}");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetRefArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null || !property.isArray)
                throw new InvalidOperationException($"[MachineLock] Serialized array not found: {target.GetType().Name}.{propertyName}");
            property.arraySize = values == null ? 0 : values.Length;
            for (int i = 0; i < property.arraySize; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInt(UnityEngine.Object target, string propertyName, int value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"[MachineLock] Serialized property not found: {target.GetType().Name}.{propertyName}");
            property.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"[MachineLock] Serialized property not found: {target.GetType().Name}.{propertyName}");
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerializedFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"[MachineLock] Serialized property not found: {target.GetType().Name}.{propertyName}");
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetVector3(UnityEngine.Object target, string propertyName, Vector3 value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"[MachineLock] Serialized property not found: {target.GetType().Name}.{propertyName}");
            property.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        // ------------------------------------------------------------------ Prefabs

        private static void EnsurePrefabs(MachineLockAudioClips clips)
        {
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || string.IsNullOrEmpty(active.path))
            {
                CreateSceneFromTemplate();
                EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                BuildConePrefab(temp, clips);
                BuildAbsorbentMatPrefab(temp, clips);
                BuildAbsorbentSockPrefab(temp, clips);
                BuildValvePrefab(temp, clips);
                BuildWaterExtinguisherPrefab(temp, clips);
                BuildIndustrialDoorPrefab(temp, clips);
                BuildEStopPrefab(temp);
                BuildAlarmPrefab(temp, clips);
                BuildElectricalPanelPrefab(temp, clips);
                BuildPpeCabinetPrefab(temp, clips);
                BuildSpillKitPrefab(temp);
                BuildPressPrefab(temp, clips);
            }
            finally
            {
                EditorSceneManager.CloseScene(temp, true);
                AssetDatabase.SaveAssets();
            }
        }

        private static string PrefabPath(string name) => PREFAB_DIR + "/" + name + ".prefab";

        private static GameObject NewPrefabRoot(Scene temp, string name)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, temp);
            return root;
        }

        private static void SavePrefab(GameObject root)
        {
            string path = PrefabPath(root.name);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject InstantiatePrefabInScene(Scene scene, string prefabName, Transform parent, Vector3 position, Vector3 euler)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(prefabName));
            if (prefab == null)
                throw new InvalidOperationException($"[MachineLock] Prefab not found: {PrefabPath(prefabName)}");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localEulerAngles = euler;
            return instance;
        }

        private static GameObject AddNestedPrefab(GameObject parent, string prefabName, Vector3 localPosition, Vector3 euler)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(prefabName));
            if (prefab == null)
                throw new InvalidOperationException($"[MachineLock] Prefab not found: {PrefabPath(prefabName)}");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.scene);
            instance.transform.SetParent(parent.transform, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localEulerAngles = euler;
            return instance;
        }

        // -- MESH_Cone (uniform-scale Y-up hero model) -------------------------

        private static void BuildConePrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_Cone");
            AddHeroModelChild(root, "MESH_ConeBody", "MESH_TrafficConeHero.fbx", Vector3.zero, Vector3.zero);
            AddBoxCollider(root, new Vector3(0f, 0.26f, 0f), new Vector3(0.34f, 0.52f, 0.34f));

            Interactable interactable = AddInteractable(root, false, true, true);
            var cone = root.AddComponent<MachineLockCone>();
            AudioSource snapAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
            SetRef(cone, "interactable", interactable);
            SetRef(cone, "oneShotAudio", snapAudio);
            SetRef(cone, "snapClip", clips.Snap);
            SavePrefab(root);
        }

        // -- MESH_AbsorbentMat (quilted polypropylene pad) ---------------------

        private static void BuildAbsorbentMatPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_AbsorbentMat");
            AddHeroModelChild(root, "MESH_AbsorbentMatBody", "MESH_AbsorbentMatHero.fbx", Vector3.zero, Vector3.zero);
            AddBoxCollider(root, new Vector3(0f, 0.025f, 0f), new Vector3(0.96f, 0.05f, 0.76f));

            Interactable interactable = AddInteractable(root, false, true, true);
            var absorbent = root.AddComponent<MachineLockAbsorbent>();
            AudioSource snapAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
            SetRef(absorbent, "interactable", interactable);
            SetRef(absorbent, "oneShotAudio", snapAudio);
            SetRef(absorbent, "snapClip", clips.Snap);
            SetInt(absorbent, "absorbentKind", (int)MachineLockAbsorbent.AbsorbentKind.Mat);
            SavePrefab(root);
        }

        // -- MESH_AbsorbentSock (fabric-sleeved boom, zero sausages) -----------

        private static void BuildAbsorbentSockPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_AbsorbentSock");
            AddHeroModelChild(root, "MESH_AbsorbentSockBody", "MESH_AbsorbentSockHero.fbx", Vector3.zero, Vector3.zero);
            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 0;
            collider.height = 1.20f;
            collider.radius = 0.09f;
            collider.center = new Vector3(0f, 0.07f, 0f);

            Interactable interactable = AddInteractable(root, false, true, true);
            var absorbent = root.AddComponent<MachineLockAbsorbent>();
            AudioSource snapAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
            SetRef(absorbent, "interactable", interactable);
            SetRef(absorbent, "oneShotAudio", snapAudio);
            SetRef(absorbent, "snapClip", clips.Snap);
            SetInt(absorbent, "absorbentKind", (int)MachineLockAbsorbent.AbsorbentKind.Sock);
            SavePrefab(root);
        }

        // -- MESH_Valve (clean industrial red handwheel, no redundant plaque) --

        private static void BuildValvePrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_Valve");
            AddCylinder(root, "MESH_ValvePipe", 0.11f, 0.6f, Vector3.zero, new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddCylinder(root, "MESH_ValveFlangeW", 0.16f, 0.04f, new Vector3(-0.26f, 0f, 0f), new Vector3(0f, 0f, 90f), matDarkSlate, false);
            AddCylinder(root, "MESH_ValveFlangeE", 0.16f, 0.04f, new Vector3(0.26f, 0f, 0f), new Vector3(0f, 0f, 90f), matDarkSlate, false);
            AddCylinder(root, "MESH_ValveStem", 0.035f, 0.24f, new Vector3(0f, 0.14f, 0f), Vector3.zero, matPolishedSteel, false);

            var wheel = NewChild(root, "MESH_Valve_Wheel", new Vector3(0f, 0.28f, 0f));
            AddCylinder(wheel, "MESH_ValveWheelRim", 0.20f, 0.045f, Vector3.zero, Vector3.zero, matEStopRed, false);
            AddCylinder(wheel, "MESH_ValveWheelHub", 0.06f, 0.065f, Vector3.zero, Vector3.zero, matEStopYellow, false);
            for (int i = 0; i < 4; i++)
            {
                var spoke = AddBox(wheel, "MESH_ValveSpoke" + (i + 1), new Vector3(0.38f, 0.028f, 0.028f),
                    Vector3.zero, matEStopRed, false);
                spoke.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            }

            GameObject pulseRing = AddCylinder(root, "MESH_ValvePulseRing", 0.24f, 0.018f,
                new Vector3(0f, 0.28f, 0f), Vector3.zero, matBeaconRed, false);

            AddBoxCollider(root, new Vector3(0f, 0.18f, 0f), new Vector3(0.62f, 0.56f, 0.52f));

            Interactable interactable = AddInteractable(root, true, false, false);
            var valve = root.AddComponent<MachineLockValve>();
            AudioSource squeakAudio = AddAudioSource(root, null, false, false, 1f, 0.85f);
            SetRef(valve, "interactable", interactable);
            SetRef(valve, "wheelTransform", wheel.transform);
            SetRef(valve, "pulseIndicatorRenderer", pulseRing.GetComponent<Renderer>());
            SetVector3(valve, "rotationAxis", Vector3.up);
            SetRef(valve, "oneShotAudio", squeakAudio);
            SetRef(valve, "squeakClip", clips.ValveTurn);
            SavePrefab(root);
        }

        // -- MESH_WaterExtinguisher (uniform-scale Y-up hero + attached F001) --

        private static void BuildWaterExtinguisherPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_WaterExtinguisher");
            AddHeroModelChild(root, "MESH_ExtinguisherBody", "MESH_WaterExtinguisherHero.fbx", Vector3.zero, Vector3.zero);
            AddBox(root, "MESH_ExtinguisherBracket", new Vector3(0.22f, 0.44f, 0.03f),
                new Vector3(0f, 0.02f, -0.11f), matDarkSlate, false);
            AddBox(root, "MESH_ExtinguisherClampTop", new Vector3(0.19f, 0.03f, 0.08f),
                new Vector3(0f, 0.16f, -0.04f), matWorkshopSteel, false);
            AddBox(root, "MESH_ExtinguisherClampBottom", new Vector3(0.19f, 0.03f, 0.08f),
                new Vector3(0f, -0.18f, -0.04f), matWorkshopSteel, false);
            // Attached ISO 7010 F001 sign travels with the extinguisher across Layout Patterns A, B, and C.
            AddFramedWallSign(root, "MESH_IsoSign_F001", "MAT_IsoSign_F001",
                new Vector2(0.48f, 0.48f), new Vector3(0f, 1.23f, -0.22f), new Vector3(0f, 180f, 0f), markStatic: false);

            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.height = 0.72f;
            collider.radius = 0.14f;
            collider.center = Vector3.zero;

            Interactable interactable = AddInteractable(root, true, false, false);
            var extinguisher = root.AddComponent<MachineLockExtinguisher>();
            ParticleSystem spray = AddParticles(root, "VFX_WaterSpray", new Vector3(0.11f, -0.22f, 0.02f), Vector3.zero,
                matParticleSoft, 100, false, 40f, new Color(0.72f, 0.8f, 0.9f, 0.55f), 0.12f, 2.5f, 1.2f, 0.05f,
                ParticleSystemShapeType.Cone, false);
            AudioSource sprayAudio = AddAudioSource(root, null, false, false, 1f, 0.85f);
            SetRef(extinguisher, "extinguisherInteractable", interactable);
            SetRef(extinguisher, "waterSprayVfx", spray);
            SetRef(extinguisher, "oneShotAudio", sprayAudio);
            SetRef(extinguisher, "steamBurstClip", clips.SteamBurst);
            SavePrefab(root);
        }

        // -- MESH_IndustrialDoor_RollUp (Hinged Steel Exit Door + Panic Bar) ---

        private static void BuildIndustrialDoorPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_IndustrialDoor_RollUp");

            // Authentic Italian steel frame with solid jamb colliders (zero MESH_DoorRollHousing!).
            AddHeroModelChild(root, "MESH_SteelExitDoorFrame", "MESH_SteelExitDoorFrame.fbx", Vector3.zero, Vector3.zero);
            AddBoundaryCollider(root, "COLLIDER_DoorJambNorth", new Vector3(0f, 1.26f, 1.01f), new Vector3(0.24f, 2.52f, 0.14f));
            AddBoundaryCollider(root, "COLLIDER_DoorJambSouth", new Vector3(0f, 1.26f, -1.01f), new Vector3(0.24f, 2.52f, 0.14f));

            // Illuminated overhead ISO 7010 E002 sign + readiness light bar facing +X into the stamping bay.
            AddPlaque(root, "MESH_IsoSign_E002", "MAT_IsoSign_E002", new Vector2(0.98f, 0.40f),
                new Vector3(0.195f, 2.90f, 0f), new Vector3(0f, -90f, 0f));
            AddBox(root, "MESH_ExitBeacon", new Vector3(0.04f, 0.06f, 1.06f),
                new Vector3(0.18f, 2.63f, 0f), matExitBeaconIdle, false);

            // Hinged REI-120 steel door leaf pivoted on North hinge edge (-0.05, 0, +0.94); swings +90 deg outward (-X) into the west vestibule.
            // Placing Interactable on MESH_SteelExitDoorLeaf ensures aiming anywhere on the door leaf OR panic bar resolves cleanly via GetComponentInParent (Issue #43).
            var leafPivot = NewChild(root, "MESH_SteelExitDoorLeaf", new Vector3(-0.05f, 0f, 0.94f));
            AddHeroModelChild(leafPivot, "MESH_ExitDoorLeafBody", "MESH_SteelExitDoorLeaf.fbx", Vector3.zero, Vector3.zero);
            BoxCollider leafCollider = leafPivot.AddComponent<BoxCollider>();
            leafCollider.center = new Vector3(0f, 1.22f, -0.94f);
            leafCollider.size = new Vector3(0.10f, 2.42f, 1.88f);
            Interactable doorInteractable = AddInteractable(leafPivot, true, true, false);

            // Prominent green EN 1125 horizontal panic bar protruding from the door leaf (shares parent Interactable).
            var panicBarObj = NewChild(leafPivot, "MESH_PanicBar", new Vector3(0.036f, 1.05f, -0.94f));
            AddHeroModelChild(panicBarObj, "MESH_PanicBarBody", "MESH_PanicBar.fbx", Vector3.zero, Vector3.zero);
            AddBoxCollider(panicBarObj, new Vector3(0.09f, 0f, 0f), new Vector3(0.24f, 0.30f, 1.44f));

            AudioSource latchAudio = AddAudioSource(root, null, false, false, 1f, 0.85f);
            var exitDoor = root.AddComponent<MachineLockExitDoor>();
            SetRef(exitDoor, "doorInteractable", doorInteractable);
            SetRef(exitDoor, "doorLeafTransform", leafPivot.transform);
            SetRef(exitDoor, "doorLeafCollider", leafCollider);
            SetVector3(exitDoor, "openAxis", Vector3.up);
            SetSerializedFloat(exitDoor, "openAngleDegrees", 90f);
            SetSerializedFloat(exitDoor, "openDurationSeconds", 0.8f);
            SetVector3(exitDoor, "exitStandLocalOffset", new Vector3(1.65f, 0f, 0f));
            SetSerializedFloat(exitDoor, "moveToDoorDurationSeconds", 0.45f);
            SetSerializedFloat(exitDoor, "fadeToBlackDurationSeconds", 0.95f);
            SetRef(exitDoor, "oneShotAudio", latchAudio);
            SetRef(exitDoor, "pressClickClip", clips.MechanicalButton);
            SetRef(exitDoor, "latchClip", clips.Lockout);
            SavePrefab(root);
        }

        // -- MESH_EStopPedestal (with engraved brushed-metal plaque) -----------

        private static void BuildEStopPrefab(Scene temp)
        {
            var root = NewPrefabRoot(temp, "MESH_EStopPedestal");
            AddBox(root, "MESH_EStopPedestalFoot", new Vector3(0.52f, 0.08f, 0.52f), new Vector3(0f, 0.04f, 0f), matEStopYellow, true);
            AddBox(root, "MESH_EStopPedestalBase", new Vector3(0.42f, 1.12f, 0.40f), new Vector3(0f, 0.60f, 0f), matWorkshopSteel, true);
            AddBox(root, "MESH_EStopBackplate", new Vector3(0.44f, 0.62f, 0.05f), new Vector3(0f, 0.88f, 0.21f), matEStopYellow, false);
            AddModelChild(root, "MESH_EStopPushbuttonTrim", SHARED_MODEL_DIR + "MESH_PULSANTI_TEAM.fbx",
                new Vector3(0.28f, 0.22f, 0.04f), new Vector3(0f, 0.56f, 0.22f), false, true, null);

            GameObject stopButton = AddCylinder(root, "MESH_EmergencyStopButton", 0.08f, 0.08f,
                new Vector3(0f, 1.02f, 0.27f), new Vector3(90f, 0f, 0f), matEStopRed, false);
            AddCylinder(root, "MESH_EmergencyStopStem", 0.04f, 0.06f, new Vector3(0f, 1.02f, 0.22f), new Vector3(90f, 0f, 0f), matEStopYellow, false);
            GameObject startButton = AddCylinder(root, "MESH_TestStartButton", 0.055f, 0.06f,
                new Vector3(0f, 0.72f, 0.26f), new Vector3(90f, 0f, 0f), matLedGreen, false);

            // Engraved brushed-aluminum control plaque above the pedestal.
            AddBox(root, "MESH_EStopPlaqueBack", new Vector3(0.48f, 0.26f, 0.025f), new Vector3(0f, 1.30f, 0.16f), matDarkSlate, false);
            AddPlaque(root, "MESH_PlaqueEStop", "MAT_Plaque_EStop", new Vector2(0.46f, 0.24f),
                new Vector3(0f, 1.30f, 0.18f), new Vector3(0f, 180f, 0f));

            AddWorldBoxCollider(stopButton, stopButton.transform.position, new Vector3(0.20f, 0.20f, 0.18f));
            AddInteractable(stopButton, true, false, false);
            AddWorldBoxCollider(startButton, startButton.transform.position, new Vector3(0.16f, 0.16f, 0.16f));
            AddInteractable(startButton, true, false, false);
            SavePrefab(root);
        }

        // -- MESH_AlarmButton (tuned spatial siren rolloff, ISO F005 on wall) --

        private static void BuildAlarmPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_AlarmButton");
            AddBox(root, "MESH_AlarmBox", new Vector3(0.42f, 0.52f, 0.16f), Vector3.zero, matEStopRed, true);
            AddBox(root, "MESH_AlarmTrim", new Vector3(0.34f, 0.42f, 0.04f), new Vector3(0f, 0f, 0.08f), matDarkSlate, false);
            GameObject pushButton = AddCylinder(root, "MESH_AlarmPushButton", 0.075f, 0.06f,
                new Vector3(0f, 0.04f, 0.12f), new Vector3(90f, 0f, 0f), matEStopRed, false);
            AddWorldBoxCollider(pushButton, pushButton.transform.position, new Vector3(0.22f, 0.22f, 0.18f));
            Interactable buttonInteractable = AddInteractable(pushButton, true, false, false);

            var beacon = NewChild(root, "VFX_AlarmBeacon", new Vector3(0f, 0.38f, 0f));
            GameObject dome = AddSphere(beacon, "MESH_AlarmBeaconDome", 0.15f, Vector3.zero, matBeaconRed, false);
            var light = beacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.1f, 0.05f);
            light.intensity = 2.2f;
            light.range = 10f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.enabled = false;

            // Tuned looping spatial siren (Logarithmic rolloff across 28 m workshop hall).
            AudioSource sirenAudio = AddAudioSource(root, clips.AlarmSiren, true, false, 1f, 0.65f);
            sirenAudio.rolloffMode = AudioRolloffMode.Logarithmic;
            sirenAudio.minDistance = 2.5f;
            sirenAudio.maxDistance = 24f;
            AudioSource buttonAudio = AddAudioSource(root, clips.AlarmButton, false, false, 1f, 0.9f);

            var alarm = root.AddComponent<MachineLockAlarm>();
            SetRef(alarm, "alarmInteractable", buttonInteractable);
            SetRef(alarm, "beaconLight", light);
            SetRef(alarm, "beaconRotor", beacon.transform);
            SetRef(alarm, "beaconDomeRenderer", dome.GetComponent<Renderer>());
            SetRef(alarm, "sirenAudio", sirenAudio);
            SetRef(alarm, "buttonAudio", buttonAudio);
            SetRef(alarm, "buttonClip", clips.AlarmButton);
            SavePrefab(root);
        }

        // -- MESH_ElectricalPanel (3 IP65 Sub-Stations, compact LEDs, brushed plaques) --

        private static void BuildElectricalPanelPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_ElectricalPanel");

            // Clean industrial steel mounting rails behind the three IP65 Sub-Stations.
            AddBox(root, "MESH_ElectricalBayRailTop", new Vector3(0.06f, 0.06f, 2.52f), new Vector3(0.12f, 0.50f, 0f), matWorkshopSteel, false);
            AddBox(root, "MESH_ElectricalBayRailBot", new Vector3(0.06f, 0.06f, 2.52f), new Vector3(0.12f, -0.22f, 0f), matWorkshopSteel, false);

            // ---- SUB-STATION 1 (Z = -0.88): MAIN DISCONNECT SWITCH ----
            var subMain = NewChild(root, "SUB_MainDisconnect", new Vector3(0f, 0f, -0.88f));
            AddHeroModelChild(subMain, "MESH_SubDisconnectBody", "MESH_SubStationDisconnect.fbx", Vector3.zero, Vector3.zero);
            GameObject disconnectHandle = AddHeroModelChild(subMain, "MESH_DisconnectSwitch", "MESH_DisconnectHandle.fbx",
                new Vector3(-0.155f, 0.12f, 0f), Vector3.zero);
            // Compact recessed pilot-lamp cylinder discs (0.052 m diameter) seated inside chrome bezels.
            GameObject mainOnLed = AddCylinder(subMain, "MESH_MainOnLed", 0.026f, 0.018f,
                new Vector3(-0.148f, 0.40f, -0.12f), new Vector3(0f, 0f, 90f), matLedRed, false);
            GameObject mainOffLed = AddCylinder(subMain, "MESH_MainOffLed", 0.026f, 0.018f,
                new Vector3(-0.148f, 0.40f, 0.12f), new Vector3(0f, 0f, 90f), matLedOff, false);
            AddBox(subMain, "MESH_PlaqueMainBack", new Vector3(0.02f, 0.30f, 0.58f), new Vector3(0.08f, -0.49f, 0f), matDarkSlate, false);
            AddPlaque(subMain, "MESH_PlaqueMainSwitch", "MAT_Plaque_MainSwitch", new Vector2(0.56f, 0.28f),
                new Vector3(0.065f, -0.49f, 0f), new Vector3(0f, 90f, 0f));

            AddWorldBoxCollider(disconnectHandle, subMain.transform.TransformPoint(new Vector3(-0.08f, 0.14f, 0f)),
                new Vector3(0.28f, 0.86f, 0.62f));
            Interactable switchInteractable = AddInteractable(disconnectHandle, true, false, false);

            // ---- SUB-STATION 2 (Z = 0.00): LOCKOUT STATION ----
            var subLock = NewChild(root, "SUB_LockoutStation", Vector3.zero);
            AddHeroModelChild(subLock, "MESH_SubLockoutBody", "MESH_SubStationLockout.fbx", Vector3.zero, Vector3.zero);
            var lockoutPoint = NewChild(subLock, "MESH_LockoutPoint", new Vector3(-0.14f, 0.14f, 0f));
            GameObject lockoutLed = AddCylinder(subLock, "MESH_LockoutLed", 0.026f, 0.018f,
                new Vector3(-0.148f, 0.40f, -0.12f), new Vector3(0f, 0f, 90f), matLedOff, false);
            GameObject lockoutReadyLed = AddCylinder(subLock, "MESH_LockoutReadyLed", 0.026f, 0.018f,
                new Vector3(-0.148f, 0.40f, 0.12f), new Vector3(0f, 0f, 90f), matLedOff, false);
            AddBox(subLock, "MESH_PlaqueLockBack", new Vector3(0.02f, 0.30f, 0.58f), new Vector3(0.08f, -0.49f, 0f), matDarkSlate, false);
            AddPlaque(subLock, "MESH_PlaqueLockout", "MAT_Plaque_Lockout", new Vector2(0.56f, 0.28f),
                new Vector3(0.065f, -0.49f, 0f), new Vector3(0f, 90f, 0f));

            AddWorldBoxCollider(lockoutPoint, subLock.transform.TransformPoint(new Vector3(-0.08f, 0.14f, 0f)),
                new Vector3(0.28f, 0.86f, 0.62f));
            Interactable lockoutInteractable = AddInteractable(lockoutPoint, true, false, false);

            // ---- SUB-STATION 3 (Z = +0.88): VOLTAGE TESTER (diegetic 7-segment Digital Meter, Issue #45) ----
            var subTester = NewChild(root, "SUB_VoltageTester", new Vector3(0f, 0f, 0.88f));
            AddHeroModelChild(subTester, "MESH_SubTesterBody", "MESH_SubStationTester.fbx", Vector3.zero, Vector3.zero);
            var voltageTester = NewChild(subTester, "MESH_VoltageTester", new Vector3(-0.14f, 0.14f, 0f));
            // Panel-mount charcoal outer housing (0.25 m wide x 0.205 m tall x 0.024 m deep), seated cleanly above the yellow dial and below the pilot LEDs.
            AddBox(subTester, "MESH_VoltageTesterMeterHousing", new Vector3(0.024f, 0.205f, 0.25f),
                new Vector3(-0.212f, 0.220f, 0f), matDarkSlate, false);
            // Front diegetic Digital Meter faceplate + 7-segment LCD quad facing -X into the stamping bay (starts dark/unlit via matHmiOff).
            GameObject testerScreenQuad = AddQuad(subTester, "MESH_VoltageTesterScreen", new Vector2(0.244f, 0.199f),
                new Vector3(-0.225f, 0.220f, 0f), new Vector3(0f, 90f, 0f), matHmiOff);
            MachineLockDigitalMeterDisplay digitalMeter = testerScreenQuad.AddComponent<MachineLockDigitalMeterDisplay>();
            SetRef(digitalMeter, "screenRenderer", testerScreenQuad.GetComponent<Renderer>());
            GameObject testerLedRed = AddCylinder(subTester, "MESH_TesterLedRed", 0.026f, 0.018f,
                new Vector3(-0.148f, 0.42f, -0.12f), new Vector3(0f, 0f, 90f), matLedOff, false);
            GameObject testerLedGreen = AddCylinder(subTester, "MESH_TesterLedGreen", 0.026f, 0.018f,
                new Vector3(-0.148f, 0.42f, 0.12f), new Vector3(0f, 0f, 90f), matLedOff, false);
            AddBox(subTester, "MESH_PlaqueTestBack", new Vector3(0.02f, 0.30f, 0.58f), new Vector3(0.08f, -0.49f, 0f), matDarkSlate, false);
            AddPlaque(subTester, "MESH_PlaqueTester", "MAT_Plaque_Tester", new Vector2(0.56f, 0.28f),
                new Vector3(0.065f, -0.49f, 0f), new Vector3(0f, 90f, 0f));

            AddWorldBoxCollider(voltageTester, subTester.transform.TransformPoint(new Vector3(-0.08f, 0.14f, 0f)),
                new Vector3(0.28f, 0.86f, 0.62f));
            Interactable testerInteractable = AddInteractable(voltageTester, true, false, false);

            ParticleSystem arc = AddParticles(subTester, "VFX_ElectricalArc", new Vector3(-0.25f, 0.165f, 0f), Vector3.zero,
                matParticleSoft, 40, false, 30f, new Color(0.6f, 0.85f, 1f, 0.9f), 0.05f, 1.2f, 0.25f, 0.04f,
                ParticleSystemShapeType.Sphere, false);
            arc.gameObject.SetActive(false);

            AudioSource oneShot = AddAudioSource(root, null, false, false, 1f, 0.85f);
            var panel = root.AddComponent<MachineLockEnergyPanel>();
            SetRef(panel, "mainSwitchStationRoot", subMain.transform);
            SetRef(panel, "lockoutStationRoot", subLock.transform);
            SetRef(panel, "testerStationRoot", subTester.transform);
            SetRef(panel, "mainSwitchInteractable", switchInteractable);
            SetRef(panel, "lockoutInteractable", lockoutInteractable);
            SetRef(panel, "testerInteractable", testerInteractable);
            SetRef(panel, "digitalMeterDisplay", digitalMeter);
            SetRef(panel, "switchHandle", disconnectHandle.transform);
            SetVector3(panel, "switchRotationAxis", Vector3.right);
            SetRef(panel, "mainSwitchOnLedRenderer", mainOnLed.GetComponent<Renderer>());
            SetRef(panel, "mainSwitchOffLedRenderer", mainOffLed.GetComponent<Renderer>());
            SetRef(panel, "lockoutReadyLedRenderer", lockoutReadyLed.GetComponent<Renderer>());
            SetRef(panel, "lockoutLedRenderer", lockoutLed.GetComponent<Renderer>());
            SetRef(panel, "testerRedLedRenderer", testerLedRed.GetComponent<Renderer>());
            SetRef(panel, "testerGreenLedRenderer", testerLedGreen.GetComponent<Renderer>());
            SetRef(panel, "ledOffMaterial", matLedOff);
            SetRef(panel, "ledRedMaterial", matLedRed);
            SetRef(panel, "ledGreenMaterial", matLedGreen);
            SetRef(panel, "arcVfx", arc.gameObject);
            SetRef(panel, "oneShotAudio", oneShot);
            SetRef(panel, "breakerOffClip", clips.BreakerOff);
            SetRef(panel, "lockoutClip", clips.Lockout);
            SetRef(panel, "testerBeepClip", clips.TesterBeep);
            SavePrefab(root);
        }

        // -- MESH_PpeCabinet (4 slots + 2 distractors, clean equipped badges) --

        private static void BuildPpeCabinetPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_PpeCabinet");
            AddBox(root, "MESH_PpeCabinetBack", new Vector3(1.80f, 2.05f, 0.05f), new Vector3(0f, 1.025f, -0.20f), matPaintedSteel, true);
            AddBox(root, "MESH_PpeCabinetSideLeft", new Vector3(0.05f, 2.05f, 0.44f), new Vector3(-0.875f, 1.025f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_PpeCabinetSideRight", new Vector3(0.05f, 2.05f, 0.44f), new Vector3(0.875f, 1.025f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_PpeCabinetTop", new Vector3(1.84f, 0.08f, 0.48f), new Vector3(0f, 2.01f, 0f), matCyanAccent, true);
            AddBox(root, "MESH_PpeCabinetBottom", new Vector3(1.80f, 0.06f, 0.44f), new Vector3(0f, 0.03f, 0f), matDarkSlate, true);
            AddBox(root, "MESH_PpeCabinetShelfLower", new Vector3(1.72f, 0.03f, 0.38f), new Vector3(0f, 0.44f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_PpeCabinetShelfUpper", new Vector3(1.72f, 0.03f, 0.38f), new Vector3(0f, 1.24f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_PpeCabinetDividerLeft", new Vector3(0.025f, 1.92f, 0.36f), new Vector3(-0.29f, 1.02f, -0.01f), matPaintedSteel, false);
            AddBox(root, "MESH_PpeCabinetDividerRight", new Vector3(0.025f, 1.92f, 0.36f), new Vector3(0.29f, 1.02f, -0.01f), matPaintedSteel, false);

            var doorLeft = NewChild(root, "MESH_PpeCabinet_DoorLeft", new Vector3(-0.44f, 1.02f, 0.22f));
            GameObject doorLeftPanel = AddBox(doorLeft, "MESH_PpeCabinet_DoorLeftPanel", new Vector3(0.86f, 1.92f, 0.03f),
                Vector3.zero, matPaintedSteel, false);
            AddBox(doorLeft, "MESH_PpeCabinet_DoorLeftWindow", new Vector3(0.66f, 1.50f, 0.036f),
                Vector3.zero, matFaceShieldVisor, false);
            AddBox(doorLeft, "MESH_PpeCabinet_DoorLeftHandle", new Vector3(0.04f, 0.28f, 0.04f),
                new Vector3(0.36f, 0f, 0.035f), matEStopYellow, false);
            BoxCollider doorLeftCol = AddLocalBoxCollider(doorLeftPanel);
            Interactable doorLeftInteractable = AddInteractable(doorLeftPanel, true, false, false);

            var doorRight = NewChild(root, "MESH_PpeCabinet_DoorRight", new Vector3(0.44f, 1.02f, 0.22f));
            GameObject doorRightPanel = AddBox(doorRight, "MESH_PpeCabinet_DoorRightPanel", new Vector3(0.86f, 1.92f, 0.03f),
                Vector3.zero, matPaintedSteel, false);
            AddBox(doorRight, "MESH_PpeCabinet_DoorRightWindow", new Vector3(0.66f, 1.50f, 0.036f),
                Vector3.zero, matFaceShieldVisor, false);
            AddBox(doorRight, "MESH_PpeCabinet_DoorRightHandle", new Vector3(0.04f, 0.28f, 0.04f),
                new Vector3(-0.36f, 0f, 0.035f), matEStopYellow, false);
            BoxCollider doorRightCol = AddLocalBoxCollider(doorRightPanel);
            Interactable doorRightInteractable = AddInteractable(doorRightPanel, true, false, false);

            AudioSource cabinetAudio = AddAudioSource(root, null, false, false, 1f, 0.85f);
            var cabinet = root.AddComponent<MachineLockPpeCabinet>();
            SetRef(cabinet, "cabinetInteractable", doorLeftInteractable);
            SetRef(cabinet, "secondaryDoorInteractable", doorRightInteractable);
            SetRef(cabinet, "doorTransform", doorLeft.transform);
            SetRef(cabinet, "secondaryDoorTransform", doorRight.transform);
            SetBool(cabinet, "slideDoorsOpen", true);
            SetSerializedFloat(cabinet, "doorSlideDistance", 0.78f);
            SetRefArray(cabinet, "doorColliders", new UnityEngine.Object[] { doorLeftCol, doorRightCol });
            SetRef(cabinet, "oneShotAudio", cabinetAudio);
            SetRef(cabinet, "equipClip", clips.EquipPpe);
            SetRef(cabinet, "openClip", clips.EStopClack);

            // Top row (y = 1.56, shelf surface at local y = -0.305) — 3 high-detail Blender hero PPE models
            var heatBay = CreatePpeBay(root, "MESH_HeatResistantGlove", new Vector3(-0.58f, 1.56f, 0.02f),
                "MAT_Plaque_Ppe_Equipped_HeatGloves", out GameObject heatVisual, out GameObject heatBadge);
            AddHeroModelChild(heatVisual, "MESH_HeatGloveMesh", "MESH_HeatGlovesHero.fbx",
                new Vector3(0f, -0.305f, 0f), Vector3.zero);

            var apronBay = CreatePpeBay(root, "MESH_ChemicalApron", new Vector3(0f, 1.56f, 0.02f),
                "MAT_Plaque_Ppe_Equipped_Apron", out GameObject apronVisual, out GameObject apronBadge);
            AddHeroModelChild(apronVisual, "MESH_ChemicalApronMesh", "MESH_ChemicalApronHero.fbx",
                new Vector3(0f, -0.24f, 0f), Vector3.zero);

            var shieldBay = CreatePpeBay(root, "MESH_FaceShield", new Vector3(0.58f, 1.56f, 0.02f),
                "MAT_Plaque_Ppe_Equipped_FaceShield", out GameObject shieldVisual, out GameObject shieldBadge);
            AddHeroModelChild(shieldVisual, "MESH_FaceShieldMesh", "MESH_FaceShieldHero.fbx",
                new Vector3(0f, -0.305f, 0f), Vector3.zero);

            // Bottom row (y = 0.76, shelf surface at local y = -0.305) — Safety Boots S3 + 2 realistic Wrong DPI distractors
            var bootsBay = CreatePpeBay(root, "MESH_SafetyBoots", new Vector3(-0.58f, 0.76f, 0.02f),
                "MAT_Plaque_Ppe_Equipped_SafetyBoots", out GameObject bootsVisual, out GameObject bootsBadge);
            AddHeroModelChild(bootsVisual, "MESH_SafetyBootsMesh", "MESH_SafetyBootsHero.fbx",
                new Vector3(0f, -0.305f, 0f), Vector3.zero);

            var latexBay = CreatePpeBay(root, "MESH_LatexGlove", new Vector3(0f, 0.76f, 0.02f),
                "MAT_Plaque_Ppe_Equipped_LatexGloves", out GameObject latexVisual, out GameObject latexBadge);
            AddHeroModelChild(latexVisual, "MESH_LatexGloveMesh", "MESH_LatexGlovesHero.fbx",
                new Vector3(0f, -0.305f, 0f), Vector3.zero);

            var maskBay = CreatePpeBay(root, "MESH_SurgicalMask", new Vector3(0.58f, 0.76f, 0.02f),
                "MAT_Plaque_Ppe_Equipped_SurgicalMask", out GameObject maskVisual, out GameObject maskBadge);
            AddHeroModelChild(maskVisual, "MESH_SurgicalMaskMesh", "MESH_SurgicalMaskHero.fbx",
                new Vector3(0f, -0.305f, 0f), Vector3.zero);

            MachineLockPpeItem[] items = new MachineLockPpeItem[6];
            items[0] = BuildPpeItem(cabinet, heatBay, heatVisual, heatBadge, PpeSlot.Gloves, true, "Guanti anticalore EN 407");
            items[1] = BuildPpeItem(cabinet, apronBay, apronVisual, apronBadge, PpeSlot.Body, true, "Grembiule antischizzo");
            items[2] = BuildPpeItem(cabinet, shieldBay, shieldVisual, shieldBadge, PpeSlot.Face, true, "Visiera facciale EN 166");
            items[3] = BuildPpeItem(cabinet, bootsBay, bootsVisual, bootsBadge, PpeSlot.Feet, true, "Scarpe antinfortunistiche S3");
            items[4] = BuildPpeItem(cabinet, latexBay, latexVisual, latexBadge, PpeSlot.Gloves, false, "Guanti in lattice monouso");
            items[5] = BuildPpeItem(cabinet, maskBay, maskVisual, maskBadge, PpeSlot.Face, false, "Mascherina chirurgica");
            SetRefArray(cabinet, "items", new UnityEngine.Object[]
            {
                items[0], items[1], items[2], items[3], items[4], items[5]
            });
            SavePrefab(root);
        }

        private static GameObject CreatePpeBay(GameObject cabinetRoot, string name, Vector3 localPos,
            string equippedPlaqueMaterialName, out GameObject visualRoot, out GameObject equippedBadge)
        {
            var bay = NewChild(cabinetRoot, name, localPos);
            visualRoot = NewChild(bay, "Visual", Vector3.zero);
            equippedBadge = AddPlaque(bay, "EquippedBadge", equippedPlaqueMaterialName, new Vector2(0.48f, 0.24f),
                new Vector3(0f, 0.02f, 0.05f), new Vector3(0f, 180f, 0f));
            equippedBadge.SetActive(false);
            return bay;
        }

        private static MachineLockPpeItem BuildPpeItem(MachineLockPpeCabinet cabinet, GameObject item,
            GameObject visualRoot, GameObject equippedBadge, PpeSlot slot, bool correct, string displayName)
        {
            AddBoxCollider(item, new Vector3(0f, 0f, 0.04f), new Vector3(0.50f, 0.56f, 0.34f));
            Interactable interactable = AddInteractable(item, false, false, false);
            var ppeItem = item.AddComponent<MachineLockPpeItem>();
            ppeItem.Configure(cabinet, slot, correct, displayName);
            ppeItem.ConfigureShelfVisuals(visualRoot, equippedBadge);
            SetRef(ppeItem, "interactable", interactable);
            SetRef(ppeItem, "itemVisualRoot", visualRoot);
            SetRef(ppeItem, "equippedBadgeVisual", equippedBadge);
            return ppeItem;
        }

        // -- MESH_SpillKit (clean trolley with cones, quilted mats & fabric socks) --

        private static void BuildSpillKitPrefab(Scene temp)
        {
            var root = NewPrefabRoot(temp, "MESH_SpillKit");
            AddBox(root, "MESH_SpillKitFloor", new Vector3(1.64f, 0.08f, 0.90f), new Vector3(0f, 0.08f, 0f), matEStopYellow, false);
            AddBox(root, "MESH_SpillKitFrame", new Vector3(1.58f, 0.34f, 0.84f), new Vector3(0f, 0.26f, 0f), matWorkshopSteel, false);
            AddModelChild(root, "MESH_SpillKitBin", SHARED_PREFAB_DIR + "MESH_SCATOLA_APRIBILE_ROSSA_TEAM.fbx",
                new Vector3(0.40f, 0.38f, 0.40f), new Vector3(-0.56f, 0.43f, -0.22f), true, false, matEStopRed);

            AddCylinder(root, "MESH_SpillKitWheelNW", 0.07f, 0.05f, new Vector3(-0.68f, 0.07f, -0.36f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelNE", 0.07f, 0.05f, new Vector3(0.68f, 0.07f, -0.36f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelSW", 0.07f, 0.05f, new Vector3(-0.68f, 0.07f, 0.36f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelSE", 0.07f, 0.05f, new Vector3(0.68f, 0.07f, 0.36f), new Vector3(90f, 0f, 0f), matApronRubber, false);

            // 4 un-squashed cones spaced cleanly along front edge; 2 quilted mats + 2 fabric socks neatly stacked behind.
            AddNestedPrefab(root, "MESH_Cone", new Vector3(-0.56f, 0.43f, 0.24f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(-0.19f, 0.43f, 0.24f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(0.18f, 0.43f, 0.24f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(0.55f, 0.43f, 0.24f), Vector3.zero);

            AddNestedPrefab(root, "MESH_AbsorbentMat", new Vector3(0.26f, 0.435f, -0.16f), Vector3.zero);
            AddNestedPrefab(root, "MESH_AbsorbentMat", new Vector3(0.26f, 0.475f, -0.16f), new Vector3(0f, 4f, 0f));
            AddNestedPrefab(root, "MESH_AbsorbentSock", new Vector3(0.18f, 0.515f, -0.24f), Vector3.zero);
            AddNestedPrefab(root, "MESH_AbsorbentSock", new Vector3(0.18f, 0.515f, -0.06f), new Vector3(0f, 180f, 0f));

            AddBoxCollider(root, new Vector3(0f, 0.22f, 0f), new Vector3(1.64f, 0.44f, 0.90f));
            SavePrefab(root);
        }

        // -- MESH_HydraulicPress (Blender hero press + MechanicalButton SFX) ---

        private static void BuildPressPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_HydraulicPress");

            AddHeroModelChild(root, "MESH_PressBody", "MESH_HydraulicPressBody.fbx", Vector3.zero, Vector3.zero);
            AddBoundaryCollider(root, "COLLIDER_PressBase", new Vector3(0f, 0.64f, 0f), new Vector3(2.96f, 1.24f, 2.46f));
            AddBoundaryCollider(root, "COLLIDER_PressCrown", new Vector3(0f, 2.98f, 0f), new Vector3(2.94f, 1.04f, 2.40f));

            // Moving hydraulic ram (exact production child name MESH_HydraulicPress_Ram).
            var ram = NewChild(root, "MESH_HydraulicPress_Ram", new Vector3(0f, 2.02f, 0f));
            AddHeroModelChild(ram, "MESH_RamBody", "MESH_HydraulicPressRam.fbx", Vector3.zero, Vector3.zero);

            // West HMI fault display panel flush in the cantilevered console bezel.
            GameObject display = AddBox(root, "MESH_PressDisplay", new Vector3(0.018f, 0.39f, 0.54f),
                new Vector3(-1.815f, 1.62f, -0.95f), matHmiScreen, false);
            NewChild(display, "VFX_DisplayFault", Vector3.zero);

            // West thermometer anchor aligned to sculpted 120 C analog gauge on MESH_HydraulicPressBody.
            NewChild(root, "MESH_Thermometer", new Vector3(-1.55f, 1.78f, 0.25f));

            // Engraved brushed-aluminum press technical nameplate on West crown face.
            AddPlaque(root, "MESH_Plaque_PressNameplate", "MAT_Plaque_PressNameplate", new Vector2(0.76f, 0.38f),
                new Vector3(-1.505f, 2.98f, 0f), new Vector3(0f, 90f, 0f));

            // Kinematic body plus trigger volume for player contact while ram moves.
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var hazard = root.AddComponent<BoxCollider>();
            hazard.isTrigger = true;
            hazard.center = new Vector3(0f, 1.6f, 0f);
            hazard.size = new Vector3(3.1f, 3.2f, 2.6f);

            AudioSource loopAudio = AddAudioSource(root, clips.PressLoop, true, false, 1f, 0.8f);
            AudioSource oneShotAudio = AddAudioSource(root, null, false, false, 1f, 0.9f);
            var press = root.AddComponent<MachineLockPress>();
            SetRef(press, "ramTransform", ram.transform);
            SetVector3(press, "ramBaseLocalPosition", Vector3.zero);
            SetRef(press, "displayRenderer", display.GetComponent<Renderer>());
            SetRef(press, "displayFaultMaterial", matHmiScreen);
            SetRef(press, "displayOffMaterial", matHmiOff);
            SetRef(press, "pressHazardTrigger", hazard);
            SetRef(press, "loopAudio", loopAudio);
            SetRef(press, "oneShotAudio", oneShotAudio);
            SetRef(press, "stopClip", clips.PressStop);
            SetRef(press, "clackClip", clips.MechanicalButton);
            SavePrefab(root);
        }

        // ------------------------------------------------------------------ Runtime helpers

        private static Interactable AddInteractable(GameObject target, bool selectable, bool grabbable, bool followsHand)
        {
            var interactable = target.GetComponent<Interactable>();
            if (interactable == null) interactable = target.AddComponent<Interactable>();
            SetBool(interactable, "canSelect", selectable);
            SetBool(interactable, "canGrab", grabbable);
            SetBool(interactable, "followsHand", followsHand);
            return interactable;
        }

        private static AudioSource AddAudioSource(GameObject target, AudioClip clip, bool loop, bool playOnAwake,
            float spatialBlend, float volume)
        {
            var source = target.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = playOnAwake;
            source.spatialBlend = spatialBlend;
            source.volume = volume;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 15f;
            return source;
        }

        /// <summary>Creates a Quest 3 budget-safe world-space ParticleSystem (no shadow casting, capped maxParticles).</summary>
        private static ParticleSystem AddParticles(GameObject parent, string name, Vector3 localPosition, Vector3 localEuler,
            Material material, int maxParticles, bool playOnAwake, float rate, Color startColor, float startSize,
            float startSpeed, float startLifetime, float radius, ParticleSystemShapeType shapeType, bool loop = true)
        {
            GameObject systemObject = NewChild(parent, name, localPosition);
            systemObject.transform.localEulerAngles = localEuler;

            var system = systemObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = loop;
            main.playOnAwake = playOnAwake;
            main.maxParticles = maxParticles;
            main.startColor = startColor;
            main.startSize = startSize;
            main.startSpeed = startSpeed;
            main.startLifetime = startLifetime;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.02f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = shapeType;
            shape.radius = radius;
            if (shapeType == ParticleSystemShapeType.Cone) shape.angle = 18f;

            ParticleSystemRenderer systemRenderer = systemObject.GetComponent<ParticleSystemRenderer>();
            if (systemRenderer != null)
            {
                systemRenderer.sharedMaterial = material;
                systemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                systemRenderer.shadowCastingMode = ShadowCastingMode.Off;
                systemRenderer.receiveShadows = false;
            }
            return system;
        }

        // ------------------------------------------------------------------ Collider & primitive helpers

        /// <summary>Adds or updates a non-trigger BoxCollider sized in world metres regardless of non-uniform parent scale.</summary>
        private static BoxCollider AddWorldBoxCollider(GameObject target, Vector3 worldCenter, Vector3 worldSize)
        {
            var collider = target.GetComponent<BoxCollider>();
            if (collider == null) collider = target.AddComponent<BoxCollider>();
            Vector3 scale = target.transform.lossyScale;
            collider.size = new Vector3(
                SafeDivide(worldSize.x, Mathf.Abs(scale.x)),
                SafeDivide(worldSize.y, Mathf.Abs(scale.y)),
                SafeDivide(worldSize.z, Mathf.Abs(scale.z)));
            collider.center = target.transform.InverseTransformPoint(worldCenter);
            collider.isTrigger = false;
            return collider;
        }

        private static BoxCollider AddLocalBoxCollider(GameObject target)
        {
            var collider = target.GetComponent<BoxCollider>();
            if (collider == null) collider = target.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            collider.isTrigger = false;
            return collider;
        }

        private static float SafeDivide(float value, float divisor)
        {
            return divisor > 0.0001f ? value / divisor : value;
        }

        private static GameObject NewChild(GameObject parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = localPosition;
            return child;
        }

        private static GameObject AddBox(GameObject parent, string name, Vector3 size, Vector3 center, Material material, bool keepCollider)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = center;
            box.transform.localScale = size;
            ApplyMaterial(box, material);
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static GameObject AddCylinder(GameObject parent, string name, float radius, float height, Vector3 center, Vector3 euler, Material material, bool keepCollider)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent.transform, false);
            cylinder.transform.localPosition = center;
            cylinder.transform.localRotation = Quaternion.Euler(euler);
            cylinder.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            ApplyMaterial(cylinder, material);
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            return cylinder;
        }

        private static GameObject AddSphere(GameObject parent, string name, float diameter, Vector3 center, Material material, bool keepCollider)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent.transform, false);
            sphere.transform.localPosition = center;
            sphere.transform.localScale = Vector3.one * diameter;
            ApplyMaterial(sphere, material);
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            return sphere;
        }

        private static GameObject AddQuad(GameObject parent, string name, Vector2 size, Vector3 center, Vector3 euler, Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent.transform, false);
            quad.transform.localPosition = center;
            quad.transform.localRotation = Quaternion.Euler(euler);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            ApplyMaterial(quad, material);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            return quad;
        }

        private static void AddBoxCollider(GameObject go, Vector3 center, Vector3 size)
        {
            var collider = go.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private static void ApplyMaterial(GameObject go, Material material)
        {
            if (material == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Instantiates a Blender-authored G_Marius FBX at exact real-world uniform (1,1,1) scale,
        /// strips imported non-mesh components, and binds every material slot by name to the
        /// scenario's URP Lit material dictionary.
        /// </summary>
        private static GameObject AddHeroModelChild(GameObject parent, string name, string fbxFileName,
            Vector3 localPos, Vector3 localEuler, bool preserveChildNames = false)
        {
            string assetPath = GROUP_MODEL_DIR + fbxFileName;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
                throw new InvalidOperationException($"[MachineLock] Group hero FBX not found: {assetPath}");

            var instance = (GameObject)UnityEngine.Object.Instantiate(model);
            instance.name = name;
            instance.transform.SetParent(parent.transform, false);
            instance.transform.localPosition = localPos;
            instance.transform.localEulerAngles = localEuler;
            instance.transform.localScale = Vector3.one;

            StripImportedNonMeshComponents(instance);

            if (!preserveChildNames)
            {
                int part = 1;
                foreach (Transform descendant in instance.GetComponentsInChildren<Transform>(true))
                {
                    if (descendant == instance.transform) continue;
                    descendant.name = name + "Part" + part.ToString("00");
                    part++;
                }
            }

            BindHeroMaterials(instance);
            return instance;
        }

        private static void BindHeroMaterials(GameObject instance)
        {
            if (heroMaterialMap == null) return;
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = NormalizeSlotMaterialName(mats[i].name);
                    if (heroMaterialMap.TryGetValue(key, out Material mapped) && mapped != null)
                    {
                        mats[i] = mapped;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        private static string NormalizeSlotMaterialName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return string.Empty;
            string clean = rawName.Replace(" (Instance)", string.Empty).Trim();
            int dot = clean.IndexOf('.');
            if (dot > 0) clean = clean.Substring(0, dot);
            return clean;
        }

        private static void StripImportedNonMeshComponents(GameObject instance)
        {
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (Light light in instance.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
            foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera);
            foreach (AudioListener listener in instance.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(listener);
        }

        /// <summary>
        /// Instantiates a read-only Shared FBX as a plain child, strips imported non-mesh components,
        /// and fits it cleanly to the target dimensions without editing any Shared file.
        /// </summary>
        private static GameObject AddModelChild(GameObject parent, string name, string assetPath, Vector3 targetSize,
            Vector3 localCenter, bool alignBottom, bool uniform, Material overrideMaterial)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
                throw new InvalidOperationException($"[MachineLock] Shared model not found: {assetPath}");

            var instance = (GameObject)UnityEngine.Object.Instantiate(model);
            instance.name = name;
            instance.transform.SetParent(parent.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            StripImportedNonMeshComponents(instance);

            int part = 1;
            foreach (Transform descendant in instance.GetComponentsInChildren<Transform>(true))
            {
                if (descendant == instance.transform) continue;
                descendant.name = name + "Part" + part.ToString("00");
                part++;
            }

            if (overrideMaterial != null)
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = overrideMaterial;

            Bounds bounds = RendererBounds(instance);
            Vector3 size = bounds.size;
            Vector3 scale = new Vector3(
                size.x > 0.0001f ? targetSize.x / size.x : 1f,
                size.y > 0.0001f ? targetSize.y / size.y : 1f,
                size.z > 0.0001f ? targetSize.z / size.z : 1f);
            if (uniform)
            {
                float uniformScale = Mathf.Min(scale.x, Mathf.Min(scale.y, scale.z));
                scale = Vector3.one * uniformScale;
            }
            instance.transform.localScale = scale;

            bounds = RendererBounds(instance);
            Vector3 offset = alignBottom
                ? new Vector3(localCenter.x - bounds.center.x, localCenter.y - bounds.min.y, localCenter.z - bounds.center.z)
                : localCenter - bounds.center;
            instance.transform.localPosition += offset;
            return instance;
        }

        private static Bounds RendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        // ------------------------------------------------------------------ Room & Workshop Shell

        private static void ConfigureRoomLighting(Scene scene)
        {
            GameObject lightRoot = FindRoot(scene, "MNG_Light");
            if (lightRoot == null)
            {
                lightRoot = new GameObject("MNG_Light");
                SceneManager.MoveGameObjectToScene(lightRoot, scene);
            }
            lightRoot.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            var light = lightRoot.GetComponent<Light>();
            if (light == null) light = lightRoot.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.93f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.40f, 0.43f, 0.47f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.22f;
            RenderSettings.fog = false;
        }

        private static void BuildRoom(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Room");

            // 1. Custom Blender-authored low-poly Italian industrial workshop shell (ADR-0005, ~3,516 tris).
            GameObject shell = AddHeroModelChild(env, "MESH_WorkshopShell", "MESH_WorkshopShell.fbx", Vector3.zero, Vector3.zero);
            MarkStaticRecursive(shell);

            // 2. Active stamping bay concrete floor (18 x 11 m top at y = 0.000, matching shell slab seamlessly).
            GameObject floor = AddBox(env, "MESH_Floor", new Vector3(ROOM_HALF_X * 2f, 0.1f, ROOM_HALF_Z * 2f),
                new Vector3(0f, -0.05f, 0f), matWorkshopFloorBay, true);
            MarkStatic(floor);

            // Clean invisible locomotion boundary colliders aligned to the 18 x 11 m active bay perimeter.
            var bounds = NewChild(env, "COLLIDER_BayBounds", Vector3.zero);
            AddBoundaryCollider(bounds, "BOUND_North", new Vector3(0f, 2.5f, ROOM_HALF_Z + 0.15f), new Vector3(ROOM_HALF_X * 2f + 0.4f, 5f, 0.3f));
            AddBoundaryCollider(bounds, "BOUND_South", new Vector3(0f, 2.5f, -(ROOM_HALF_Z + 0.15f)), new Vector3(ROOM_HALF_X * 2f + 0.4f, 5f, 0.3f));
            AddBoundaryCollider(bounds, "BOUND_East", new Vector3(ROOM_HALF_X + 0.15f, 2.5f, 0f), new Vector3(0.3f, 5f, ROOM_HALF_Z * 2f + 0.4f));
            AddBoundaryCollider(bounds, "BOUND_WestNorth", new Vector3(-(ROOM_HALF_X + 0.15f), 2.5f, 3.3f), new Vector3(0.3f, 5f, 4.4f));
            AddBoundaryCollider(bounds, "BOUND_WestSouth", new Vector3(-(ROOM_HALF_X + 0.15f), 2.5f, -3.3f), new Vector3(0.3f, 5f, 4.4f));
            AddBoundaryCollider(bounds, "BOUND_WestBackstop", new Vector3(-(ROOM_HALF_X + 1.9f), 2.5f, 0f), new Vector3(0.3f, 5f, 3.4f));
            AddBoundaryCollider(bounds, "BOUND_WestDoorwayFloor", new Vector3(-(ROOM_HALF_X + 1.0f), -0.05f, 0f), new Vector3(2.4f, 0.1f, 3.4f));

            // 3. West hinged steel Exit Door with panic bar, E002 illuminated header & MachineLockExitDoor.
            var door = NewChild(env, "MESH_Door", new Vector3(-ROOM_HALF_X, 0f, 0f));
            InstantiatePrefabInScene(scene, "MESH_IndustrialDoor_RollUp", door.transform, Vector3.zero, Vector3.zero);

            // 4. Active bay & background workshop dressing + safety barriers.
            BuildBayBarriersAndDressing(env);

            // 5. UNI EN ISO 7010 / D.Lgs. 81/08 Safety Signs, yellow safety floor lanes & ceiling fixtures.
            var signs = NewChild(env, "MESH_Signs", Vector3.zero);
            BuildIso7010WallSignage(signs);
            BuildYellowSafetyFloorLanes(signs);
            BuildCeilingFixtures(signs);
        }

        private static void AddBoundaryCollider(GameObject parent, string name, Vector3 center, Vector3 size)
        {
            var go = NewChild(parent, name, center);
            AddBoxCollider(go, Vector3.zero, size);
        }

        private static void BuildIso7010WallSignage(GameObject signs)
        {
            // North wall (z = +5.46, facing South -Z, euler = 0,0,0), centered cleanly between H-beam columns at x = -4.5, 0, +4.5.
            AddFramedWallSign(signs, "MESH_Plaque_DeptHeader", "MAT_Plaque_DeptHeader",
                new Vector2(2.40f, 0.54f), new Vector3(-2.25f, 3.45f, 5.46f), Vector3.zero);
            AddFramedWallSign(signs, "MESH_IsoSign_W017", "MAT_IsoSign_W017",
                new Vector2(0.50f, 0.50f), new Vector3(-3.30f, 2.55f, 5.46f), Vector3.zero);
            AddFramedWallSign(signs, "MESH_IsoSign_W024", "MAT_IsoSign_W024",
                new Vector2(0.50f, 0.50f), new Vector3(-2.60f, 2.55f, 5.46f), Vector3.zero);
            AddFramedWallSign(signs, "MESH_IsoSign_P011", "MAT_IsoSign_P011",
                new Vector2(0.50f, 0.50f), new Vector3(-1.90f, 2.55f, 5.46f), Vector3.zero);
            AddFramedWallSign(signs, "MESH_IsoSign_W011", "MAT_IsoSign_W011",
                new Vector2(0.50f, 0.50f), new Vector3(-1.20f, 2.55f, 5.46f), Vector3.zero);

            // East electrical backing wall (x = +8.88, facing West -X, euler = 0,90,0), centered cleanly between z = -0.88 / 0 / +0.88 conduits.
            AddFramedWallSign(signs, "MESH_IsoSign_W012", "MAT_IsoSign_W012",
                new Vector2(0.48f, 0.48f), new Vector3(8.88f, 2.55f, -0.44f), new Vector3(0f, 90f, 0f));
            AddFramedWallSign(signs, "MESH_IsoSign_P011East", "MAT_IsoSign_P011",
                new Vector2(0.48f, 0.48f), new Vector3(8.88f, 2.55f, 0.44f), new Vector3(0f, 90f, 0f));

            // South wall (z = -5.46, facing North +Z, euler = 0,180,0)
            AddFramedWallSign(signs, "MESH_IsoSign_PpeBoard", "MAT_IsoSign_PpeBoard",
                new Vector2(0.74f, 0.92f), new Vector3(-1.20f, 2.55f, -5.46f), new Vector3(0f, 180f, 0f));
            AddFramedWallSign(signs, "MESH_IsoSign_M008", "MAT_IsoSign_M008",
                new Vector2(0.42f, 0.42f), new Vector3(2.05f, 2.55f, -5.46f), new Vector3(0f, 180f, 0f));
            AddFramedWallSign(signs, "MESH_IsoSign_M009", "MAT_IsoSign_M009",
                new Vector2(0.42f, 0.42f), new Vector3(2.55f, 2.55f, -5.46f), new Vector3(0f, 180f, 0f));
            AddFramedWallSign(signs, "MESH_IsoSign_M013", "MAT_IsoSign_M013",
                new Vector2(0.42f, 0.42f), new Vector3(3.05f, 2.55f, -5.46f), new Vector3(0f, 180f, 0f));
            AddFramedWallSign(signs, "MESH_IsoSign_M026", "MAT_IsoSign_M026",
                new Vector2(0.42f, 0.42f), new Vector3(3.55f, 2.55f, -5.46f), new Vector3(0f, 180f, 0f));

            // West partition wall (x = -8.90, facing East +X, euler = 0,-90,0); E001 sits North of doorway so left arrow points straight to the exit.
            AddFramedWallSign(signs, "MESH_IsoSign_F005", "MAT_IsoSign_F005",
                new Vector2(0.48f, 0.48f), new Vector3(-8.90f, 2.75f, 2.45f), new Vector3(0f, -90f, 0f));
            AddFramedWallSign(signs, "MESH_IsoSign_E001", "MAT_IsoSign_E001",
                new Vector2(0.72f, 0.36f), new Vector3(-8.90f, 2.75f, 1.55f), new Vector3(0f, -90f, 0f));
        }

        private static void BuildBayBarriersAndDressing(GameObject env)
        {
            var dressing = NewChild(env, "ENV_IndustrialDressing", Vector3.zero);

            // 1. Safety barriers flanking the East electrical wall opening (open view into East background bay).
            Vector3[] eastBarrierPositions =
            {
                new Vector3(ROOM_HALF_X - 0.08f, 0f, -2.65f),
                new Vector3(ROOM_HALF_X - 0.08f, 0f, -4.45f),
                new Vector3(ROOM_HALF_X - 0.08f, 0f, 2.65f),
                new Vector3(ROOM_HALF_X - 0.08f, 0f, 4.45f)
            };
            for (int i = 0; i < eastBarrierPositions.Length; i++)
            {
                GameObject barrier = AddModelChild(dressing, "MESH_BayBarrier0" + (i + 1),
                    SHARED_MODEL_DIR + "MESH_Magazzino_Barriera.fbx",
                    new Vector3(1.65f, 1.05f, 0.14f), eastBarrierPositions[i], true, false, null);
                barrier.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                MarkStaticRecursive(barrier);
            }

            // 2. Grounded active-bay perimeter workshop dressing (verified free across Patterns A, B, and C!).
            GameObject benchSouth = AddHeroModelChild(dressing, "MESH_WorkbenchSouth", "MESH_IndustrialWorkbench.fbx",
                new Vector3(-1.20f, 0f, -5.02f), Vector3.zero);
            AddBoxCollider(benchSouth, new Vector3(0f, 0.45f, 0f), new Vector3(1.80f, 0.90f, 0.68f));
            MarkStaticRecursive(benchSouth);

            GameObject benchNE = AddHeroModelChild(dressing, "MESH_WorkbenchNorthEast", "MESH_IndustrialWorkbench.fbx",
                new Vector3(7.05f, 0f, 5.02f), new Vector3(0f, 180f, 0f));
            AddBoxCollider(benchNE, new Vector3(0f, 0.45f, 0f), new Vector3(1.80f, 0.90f, 0.68f));
            MarkStaticRecursive(benchNE);

            GameObject cabSW = AddHeroModelChild(dressing, "MESH_ToolCabinetSW", "MESH_ToolCabinet.fbx",
                new Vector3(-8.00f, 0f, -5.06f), Vector3.zero);
            AddBoxCollider(cabSW, new Vector3(0f, 0.56f, 0f), new Vector3(0.96f, 1.12f, 0.52f));
            MarkStaticRecursive(cabSW);

            GameObject cabNW = AddHeroModelChild(dressing, "MESH_ToolCabinetNW", "MESH_ToolCabinet.fbx",
                new Vector3(-8.00f, 0f, 5.06f), new Vector3(0f, 180f, 0f));
            AddBoxCollider(cabNW, new Vector3(0f, 0.56f, 0f), new Vector3(0.96f, 1.12f, 0.52f));
            MarkStaticRecursive(cabNW);

            GameObject bundSE = AddHeroModelChild(dressing, "MESH_BundDrumsSE", "MESH_BundDrums.fbx",
                new Vector3(7.85f, 0f, -4.85f), Vector3.zero);
            AddBoxCollider(bundSE, new Vector3(0f, 0.54f, 0f), new Vector3(1.36f, 1.08f, 0.78f));
            MarkStaticRecursive(bundSE);

            GameObject sheetPalletNE = AddHeroModelChild(dressing, "MESH_SheetPalletNE", "MESH_SheetPallet.fbx",
                new Vector3(2.15f, 0f, 4.92f), new Vector3(0f, 180f, 0f));
            AddBoxCollider(sheetPalletNE, new Vector3(0f, 0.28f, 0f), new Vector3(1.20f, 0.56f, 0.85f));
            MarkStaticRecursive(sheetPalletNE);

            // Protective yellow/black steel bollards guarding the Electrical Bay & West Exit flanks.
            Vector3[] bollardPos =
            {
                new Vector3(8.35f, 0f, -1.68f),
                new Vector3(8.35f, 0f,  1.68f),
                new Vector3(-8.55f, 0f, -1.42f),
                new Vector3(-8.55f, 0f,  1.42f)
            };
            for (int i = 0; i < bollardPos.Length; i++)
            {
                GameObject bollard = AddHeroModelChild(dressing, "MESH_Bollard0" + (i + 1), "MESH_SteelBollard.fbx",
                    bollardPos[i], Vector3.zero);
                MarkStaticRecursive(bollard);
            }

            // Overhead armored cable tray & risers connecting East Electrical Bay directly to the Hydraulic Press.
            GameObject conduit = AddHeroModelChild(dressing, "MESH_OverheadConduit", "MESH_OverheadConduit.fbx",
                Vector3.zero, Vector3.zero);
            MarkStaticRecursive(conduit);

            // 3. Background Factory Bay Dressing beyond East (+X) & West (-X) bay limits for rich industrial depth.
            GameObject forklift = AddModelChild(dressing, "MESH_HallForklift",
                SHARED_MODEL_DIR + "Muletto.fbx",
                new Vector3(2.40f, 2.15f, 1.25f), new Vector3(11.8f, 0f, 2.85f), true, true, null);
            forklift.transform.localRotation = Quaternion.Euler(0f, -125f, 0f);
            MarkStaticRecursive(forklift);

            GameObject generator = AddModelChild(dressing, "MESH_HallGenerator",
                SHARED_MODEL_DIR + "MESH_Folgorazione_Generatore.fbx",
                new Vector3(2.20f, 1.65f, 1.15f), new Vector3(12.2f, 0f, -3.55f), true, true, null);
            generator.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            MarkStaticRecursive(generator);

            Vector3[] shelfPositions =
            {
                new Vector3(12.6f, 0f,  4.3f),
                new Vector3(10.5f, 0f, -4.6f),
                new Vector3(-12.4f, 0f, -4.1f),
                new Vector3(-12.4f, 0f,  4.1f)
            };
            for (int i = 0; i < shelfPositions.Length; i++)
            {
                GameObject shelf = AddModelChild(dressing, "MESH_HallShelf0" + (i + 1),
                    SHARED_MODEL_DIR + "MESH_Magazzino_Scaffale.fbx",
                    new Vector3(2.4f, 3.2f, 1.1f), shelfPositions[i], true, false, null);
                MarkStaticRecursive(shelf);
            }

            GameObject sheetEast = AddHeroModelChild(dressing, "MESH_SheetPalletEast", "MESH_SheetPallet.fbx",
                new Vector3(10.5f, 0f, 3.4f), new Vector3(0f, 12f, 0f));
            MarkStaticRecursive(sheetEast);

            GameObject bundWest = AddHeroModelChild(dressing, "MESH_BundDrumsWest", "MESH_BundDrums.fbx",
                new Vector3(-10.8f, 0f, 3.6f), new Vector3(0f, 90f, 0f));
            MarkStaticRecursive(bundWest);

            Vector3[] barrelPositions =
            {
                new Vector3(10.4f, 0f, -1.5f),
                new Vector3(10.4f, 0f, -2.3f),
                new Vector3(-10.8f, 0f, -3.4f)
            };
            for (int i = 0; i < barrelPositions.Length; i++)
            {
                GameObject barrel = AddModelChild(dressing, "MESH_HallBarrel0" + (i + 1),
                    SHARED_MODEL_DIR + "MESH_Folgorazione_Barile.fbx",
                    new Vector3(0.62f, 0.92f, 0.62f), barrelPositions[i], true, true, null);
                MarkStaticRecursive(barrel);
            }

            GameObject palletEast = AddModelChild(dressing, "MESH_HallPallet01",
                SHARED_MODEL_DIR + "Pallet.fbx",
                new Vector3(1.2f, 0.16f, 1.0f), new Vector3(11.6f, 0f, -1.0f), true, false, null);
            MarkStaticRecursive(palletEast);

            // High-mounted industrial exhaust vent fan on the North wall.
            GameObject ventFan = AddModelChild(dressing, "MESH_HallVentFan",
                SHARED_PREFAB_DIR + "MESH_VENTOLA_TEAM.fbx",
                new Vector3(0.45f, 1.55f, 1.55f), new Vector3(-4.5f, 3.35f, 5.42f), false, true, null);
            ventFan.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            MarkStaticRecursive(ventFan);
        }

        private static void BuildYellowSafetyFloorLanes(GameObject signs)
        {
            // Inner hazard perimeter framing the 7 x 5 m oil spill & press danger zone.
            AddFloorMarking(signs, "MESH_FloorHazardNorth", new Vector2(8.4f, 0.14f), new Vector3(0f, 0.005f, 3.05f));
            AddFloorMarking(signs, "MESH_FloorHazardSouth", new Vector2(8.4f, 0.14f), new Vector3(0f, 0.005f, -3.05f));
            AddFloorMarking(signs, "MESH_FloorHazardEast", new Vector2(0.14f, 6.24f), new Vector3(4.15f, 0.005f, 0f));
            AddFloorMarking(signs, "MESH_FloorHazardWest", new Vector2(0.14f, 6.24f), new Vector3(-4.15f, 0.005f, 0f));

            // Outer Dry Perimeter safety walkway frame.
            AddFloorMarking(signs, "MESH_FloorWalkwayNorth", new Vector2(16.2f, 0.12f), new Vector3(0f, 0.005f, 4.75f));
            AddFloorMarking(signs, "MESH_FloorWalkwaySouth", new Vector2(16.2f, 0.12f), new Vector3(0f, 0.005f, -4.75f));
            AddFloorMarking(signs, "MESH_FloorWalkwayEast", new Vector2(0.12f, 9.62f), new Vector3(8.05f, 0.005f, 0f));
            AddFloorMarking(signs, "MESH_FloorWalkwayWest", new Vector2(0.12f, 9.62f), new Vector3(-8.05f, 0.005f, 0f));

            // Station wayfinding connector lanes.
            AddFloorMarking(signs, "MESH_FloorLaneEntry", new Vector2(2.6f, 0.12f), new Vector3(-7.0f, 0.005f, 0f));
            AddFloorMarking(signs, "MESH_FloorLaneElectrical", new Vector2(0.12f, 2.8f), new Vector3(7.55f, 0.005f, 0f));
            AddFloorMarking(signs, "MESH_FloorLaneAlarm", new Vector2(0.12f, 2.2f), new Vector3(-7.6f, 0.005f, 1.3f));
        }

        private static void AddFloorMarking(GameObject parent, string name, Vector2 size, Vector3 position)
        {
            AddQuad(parent, name, size, position, new Vector3(90f, 0f, 0f), matFloorMarking);
        }

        private static void BuildCeilingFixtures(GameObject parent)
        {
            Vector3[] positions =
            {
                new Vector3(-5.0f, 5.75f, -2.0f),
                new Vector3(-5.0f, 5.75f,  2.0f),
                new Vector3( 5.0f, 5.75f, -2.0f),
                new Vector3( 5.0f, 5.75f,  2.0f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                var fixture = NewChild(parent, "MESH_CeilingLight0" + (i + 1), positions[i]);
                var light = fixture.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.96f, 0.88f);
                light.intensity = 1.4f;
                light.range = 10f;
                light.shadows = LightShadows.None;
                light.lightmapBakeType = LightmapBakeType.Baked;

                GameObject model = AddModelChild(fixture, "MESH_CeilingLightFixture0" + (i + 1),
                    SHARED_MODEL_DIR + "MESH_Magazzino_LuceDaSoffitto.fbx",
                    new Vector3(1.4f, 0.35f, 0.55f), new Vector3(0f, 0.25f, 0f), false, false, null);
                MarkStaticRecursive(model);
                AddQuad(fixture, "MESH_CeilingLight0" + (i + 1) + "Panel", new Vector2(1.1f, 0.32f),
                    new Vector3(0f, 0.06f, 0f), new Vector3(-90f, 0f, 0f), matLightFixture);
            }
        }

        // ------------------------------------------------------------------ Press & Safety areas

        private static void BuildPressArea(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Press");
            InstantiatePrefabInScene(scene, "MESH_HydraulicPress", env.transform, Vector3.zero, Vector3.zero);

            AddCylinder(env, "MESH_PipeStraight01", 0.085f, 2.75f, new Vector3(3.38f, 0.95f, 0.55f), new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddBox(env, "MESH_PipeSupport01", new Vector3(0.10f, 0.92f, 0.10f), new Vector3(3.95f, 0.46f, 0.55f), matDarkSlate, false);
            AddBox(env, "MESH_PipeSupport02", new Vector3(0.10f, 0.92f, 0.10f), new Vector3(4.75f, 0.46f, 0.55f), matDarkSlate, false);

            InstantiatePrefabInScene(scene, "MESH_Valve", env.transform, new Vector3(4.75f, 0.95f, 0.55f), Vector3.zero);
        }

        private static void BuildSafetyArea(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Safety");
            InstantiatePrefabInScene(scene, "MESH_EStopPedestal", env.transform, new Vector3(-5.6f, 0f, 0f), new Vector3(0f, -90f, 0f));
            AddBox(env, "MESH_AlarmPost", new Vector3(0.16f, 2.2f, 0.16f), new Vector3(-8.65f, 1.1f, 2.4f), matDarkSlate, true);
            InstantiatePrefabInScene(scene, "MESH_AlarmButton", env.transform, new Vector3(-8.50f, 1.45f, 2.4f), new Vector3(0f, 90f, 0f));
            InstantiatePrefabInScene(scene, "MESH_ElectricalPanel", env.transform, new Vector3(8.72f, 1.28f, 0f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_PpeCabinet", env.transform, new Vector3(2.8f, 0f, -5.12f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_SpillKit", env.transform, new Vector3(-5.2f, 0f, -4.35f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_WaterExtinguisher", env.transform, new Vector3(3.75f, 1.25f, 5.22f), new Vector3(0f, 180f, 0f));
        }

        // ------------------------------------------------------------------ Hazard area (organic 4-quadrant SPLAT oil puddle)

        private static void BuildHazardArea(Scene scene, MachineLockAudioClips clips)
        {
            GameObject hazard = NewRoot(scene, "HAZARD_Oil");

            var jet = NewChild(hazard, "VFX_OilJet", new Vector3(2.05f, 0.95f, 0.55f));
            AddCylinder(jet, "VFX_OilJetStream", 0.022f, 0.95f, new Vector3(0f, -0.475f, 0f), Vector3.zero, matOil, false);
            AddParticles(jet, "VFX_OilParticles", new Vector3(0f, -0.1f, 0f), Vector3.zero,
                matParticleSoft, 80, true, 35f, new Color(0.09f, 0.06f, 0.03f, 0.9f), 0.05f, 0.4f, 1f, 0.03f,
                ParticleSystemShapeType.Cone);
            AddAudioSource(jet, clips.OilLeak, true, true, 1f, 0.7f);

            // Organic 4-quadrant SPLAT hot-oil puddle derived from MESH_SPLAT.glb (exact 7.0 x 5.0 m base).
            AddHeroModelChild(hazard, "VFX_OilPuddle", "MESH_OilPuddleSplat.fbx", Vector3.zero, Vector3.zero, preserveChildNames: true);

            AddParticles(hazard, "VFX_Steam", new Vector3(0f, 0.35f, 1.8f), Vector3.zero,
                matParticleSoft, 60, true, 12f, new Color(0.85f, 0.86f, 0.85f, 0.35f), 0.35f, 0.35f, 3f, 1.4f,
                ParticleSystemShapeType.Sphere);
            AddParticles(hazard, "VFX_Smoke", new Vector3(0f, 2.8f, 0f), Vector3.zero,
                matParticleSoft, 120, false, 0f, new Color(0.16f, 0.16f, 0.16f, 0.75f), 0.9f, 0.3f, 5f, 1.2f,
                ParticleSystemShapeType.Sphere);

            var zone = NewChild(hazard, "ZONE_PuddleHazard", new Vector3(0f, 0.25f, 0f));
            var zoneCollider = zone.AddComponent<BoxCollider>();
            zoneCollider.center = Vector3.zero;
            zoneCollider.size = new Vector3(7f, 0.5f, 5f);
            zoneCollider.isTrigger = true;

            for (int i = 0; i < 4; i++) BuildCoverageZone(hazard, i);

            var patternA = NewChild(hazard, "PATTERN_PuddleA", Vector3.zero);
            patternA.transform.localScale = new Vector3(7.0f, 1f, 5.0f);
            var patternB = NewChild(hazard, "PATTERN_PuddleB", new Vector3(0.5f, 0f, 0.2f));
            patternB.transform.localScale = new Vector3(6.6f, 1f, 4.8f);
            var patternC = NewChild(hazard, "PATTERN_PuddleC", new Vector3(-0.4f, 0f, -0.25f));
            patternC.transform.localScale = new Vector3(6.8f, 1f, 4.7f);

            hazard.AddComponent<MachineLockSpillSystem>();
        }

        private static void BuildCoverageZone(GameObject hazard, int quadrant)
        {
            float signX = quadrant == 1 || quadrant == 3 ? -1f : 1f;
            float signZ = quadrant >= 2 ? -1f : 1f;
            GameObject zone = NewChild(hazard, "ZONE_Coverage" + (quadrant + 1),
                new Vector3(2.0f * signX, 0.02f, 1.4f * signZ));

            GameObject baseline = BuildBaselineOutline(zone, new Vector3(2.4f, 0.02f, 1.1f), false);
            GameObject ghost = BuildGhost(zone, new Vector3(2.4f, 0.025f, 1.1f), false);
            var snapZone = zone.AddComponent<MachineLockSnapZone>();
            snapZone.Configure(MachineLockSnapZone.SnapKind.Absorbent, zone.transform, ghost, quadrant, baseline);
        }

        private static GameObject BuildBaselineOutline(GameObject zone, Vector3 size, bool cone)
        {
            var baseline = NewChild(zone, "VFX_BaselineMarker", Vector3.zero);
            if (cone)
            {
                AddBox(baseline, "MESH_BaseRingN", new Vector3(0.56f, 0.012f, 0.07f), new Vector3(0f, 0.014f, 0.25f), matBaselineZone, false);
                AddBox(baseline, "MESH_BaseRingS", new Vector3(0.56f, 0.012f, 0.07f), new Vector3(0f, 0.014f, -0.25f), matBaselineZone, false);
                AddBox(baseline, "MESH_BaseRingE", new Vector3(0.07f, 0.012f, 0.56f), new Vector3(0.25f, 0.014f, 0f), matBaselineZone, false);
                AddBox(baseline, "MESH_BaseRingW", new Vector3(0.07f, 0.012f, 0.56f), new Vector3(-0.25f, 0.014f, 0f), matBaselineZone, false);
            }
            else
            {
                float hw = size.x * 0.5f;
                float hd = size.z * 0.5f;
                AddBox(baseline, "MESH_BaseFrameN", new Vector3(size.x, 0.012f, 0.08f), new Vector3(0f, 0.016f, hd), matBaselineZone, false);
                AddBox(baseline, "MESH_BaseFrameS", new Vector3(size.x, 0.012f, 0.08f), new Vector3(0f, 0.016f, -hd), matBaselineZone, false);
                AddBox(baseline, "MESH_BaseFrameE", new Vector3(0.08f, 0.012f, size.z), new Vector3(hw, 0.016f, 0f), matBaselineZone, false);
                AddBox(baseline, "MESH_BaseFrameW", new Vector3(0.08f, 0.012f, size.z), new Vector3(-hw, 0.016f, 0f), matBaselineZone, false);
            }
            baseline.SetActive(false);
            return baseline;
        }

        private static GameObject BuildGhost(GameObject zone, Vector3 size, bool cone)
        {
            var ghost = NewChild(zone, "VFX_SnapGhost", Vector3.zero);
            if (cone)
            {
                AddCylinder(ghost, "MESH_GhostConeBody", 0.16f, 0.52f, new Vector3(0f, 0.26f, 0f), Vector3.zero, matGhostSnap, false);
            }
            else
            {
                AddBox(ghost, "MESH_GhostCoverage", new Vector3(size.x, size.y, size.z),
                    new Vector3(0f, 0.018f, 0f), matGhostSnap, false);
            }
            ghost.SetActive(false);
            return ghost;
        }

        // ------------------------------------------------------------------ Delimitation, patterns & observation

        private static void BuildDelimitationArea(Scene scene)
        {
            GameObject delimitation = NewRoot(scene, "ZONE_Delimitation");
            Vector3[] points =
            {
                new Vector3(3.9f, 0f, 2.85f),
                new Vector3(-3.9f, 0f, 2.85f),
                new Vector3(3.9f, 0f, -2.85f),
                new Vector3(-3.9f, 0f, -2.85f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject point = NewChild(delimitation, "ZONE_ConePoint" + (i + 1), points[i]);
                GameObject baseline = BuildBaselineOutline(point, Vector3.zero, true);
                GameObject ghost = BuildGhost(point, Vector3.zero, true);
                var snapZone = point.AddComponent<MachineLockSnapZone>();
                snapZone.Configure(MachineLockSnapZone.SnapKind.Cone, point.transform, ghost, -1, baseline);
            }
        }

        private static void BuildLayoutPatterns(Scene scene)
        {
            GameObject patterns = NewRoot(scene, "MNG_LayoutPatterns");

            var a = NewChild(patterns, "PATTERN_A", Vector3.zero);
            NewMarker(a, "MarkerPpeCabinet", new Vector3(2.8f, 0f, -5.12f), Vector3.zero);
            NewMarker(a, "MarkerSpillKit", new Vector3(-5.2f, 0f, -4.35f), Vector3.zero);
            NewMarker(a, "MarkerExtinguisher", new Vector3(3.75f, 1.25f, 5.22f), new Vector3(0f, 180f, 0f));

            var b = NewChild(patterns, "PATTERN_B", Vector3.zero);
            NewMarker(b, "MarkerPpeCabinet", new Vector3(5.8f, 0f, -5.12f), Vector3.zero);
            NewMarker(b, "MarkerSpillKit", new Vector3(-5.2f, 0f, 4.35f), new Vector3(0f, 180f, 0f));
            NewMarker(b, "MarkerExtinguisher", new Vector3(0.9f, 1.25f, 5.22f), new Vector3(0f, 180f, 0f));

            var c = NewChild(patterns, "PATTERN_C", Vector3.zero);
            NewMarker(c, "MarkerPpeCabinet", new Vector3(-2.5f, 0f, 5.12f), new Vector3(0f, 180f, 0f));
            NewMarker(c, "MarkerSpillKit", new Vector3(4.8f, 0f, -4.35f), Vector3.zero);
            NewMarker(c, "MarkerExtinguisher", new Vector3(-6.2f, 1.25f, -5.22f), Vector3.zero);
        }

        private static GameObject NewMarker(GameObject parent, string name, Vector3 localPos, Vector3 localEuler)
        {
            var marker = NewChild(parent, name, localPos);
            marker.transform.localEulerAngles = localEuler;
            return marker;
        }

        private static void BuildObservationTargets(Scene scene, MachineLockConfig config)
        {
            GameObject targets = NewRoot(scene, "MNG_ObservationTargets");
            var scenario = FindRoot(scene, "MNG_Scenario");
            if (scenario == null)
                throw new InvalidOperationException("[MachineLock] MNG_Scenario is missing before the observation targets are built.");

            Vector3[] positions =
            {
                new Vector3(0f, 1.7f, 0f),
                new Vector3(-1.6f, 1.78f, 0.25f),
                new Vector3(-1.8f, 1.62f, -0.95f),
                new Vector3(0f, 0.1f, 1.85f),
                new Vector3(8.5f, 1.3f, 0f),
                new Vector3(2.8f, 1.1f, -5.12f)
            };

            for (int i = 0; i < OBSERVATION_NAMES.Length; i++)
            {
                GameObject target = NewChild(targets, OBSERVATION_NAMES[i], positions[i]);
                var observation = target.AddComponent<MachineLockObservationTarget>();
                observation.Configure(OBSERVATION_IDS[i], scenario.GetComponent<MachineLockScenario>(), config);
            }
        }

        private static void EnsureMiscRoots(Scene scene, MachineLockAudioClips clips)
        {
            GameObject audioRoot = EnsureEmptyRoot(scene, "MNG_Audio");
            AddAudioSource(audioRoot, clips.AmbientFactory, true, true, 0f, 0.25f);
            EnsureEmptyRoot(scene, "MNG_Debug");
        }

        private static GameObject EnsureEmptyRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root == null) root = NewRoot(scene, name);
            return root;
        }

        /// <summary>Wires the global URP damage Volume, stereo fallback overlay material, flash light, and critical SFX on MNG_Scenario (ADR-0003).</summary>
        private static void ConfigureDamageFeedback(Scene scene, VolumeProfile profile, MachineLockAudioClips clips)
        {
            GameObject manager = FindRoot(scene, "MNG_Scenario");
            if (manager == null) throw new InvalidOperationException("[MachineLock] MNG_Scenario is missing.");

            GameObject vignetteObject = EnsureChild(manager, "VFX_DamageVignette");
            var volume = EnsureComponent<Volume>(vignetteObject);
            volume.isGlobal = true;
            volume.weight = 0f;
            volume.priority = 0f;
            volume.sharedProfile = profile;

            GameObject lightObject = EnsureChild(manager, "LIGHT_DamageFlash");
            var flash = EnsureComponent<Light>(lightObject);
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.08f, 0.05f);
            flash.intensity = 0f;
            flash.range = 3f;
            flash.shadows = LightShadows.None;
            flash.lightmapBakeType = LightmapBakeType.Realtime;
            flash.enabled = false;
            EditorUtility.SetDirty(flash);

            var oneShot = EnsureComponent<AudioSource>(manager);
            oneShot.playOnAwake = false;
            oneShot.loop = false;
            oneShot.spatialBlend = 0f;
            oneShot.volume = 0.8f;
            EditorUtility.SetDirty(oneShot);

            var feedback = EnsureComponent<MachineLockDamageFeedback>(manager);
            SetRef(feedback, "damageVolume", volume);
            SetRef(feedback, "fallbackFlashLight", flash);
            SetRef(feedback, "overlayMaterialAsset", matDamageOverlay);
            SetRef(feedback, "oneShotAudio", oneShot);
            SetRef(feedback, "shockClip", clips.ElectricShock);
            SetRef(feedback, "burnClip", clips.Burn);
            SetRef(feedback, "steamClip", clips.SteamBurst);
            var fbSo = new SerializedObject(feedback);
            fbSo.FindProperty("fadeInSeconds").floatValue = 0.15f;
            fbSo.FindProperty("holdSeconds").floatValue = 0.5f;
            fbSo.FindProperty("fadeOutSeconds").floatValue = 1.5f;
            fbSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);

            var actionFeedback = EnsureComponent<MachineLockActionFeedback>(manager);
            SetRef(actionFeedback, "oneShotAudio", oneShot);
            SetRef(actionFeedback, "positiveClip", clips.FeedbackPositive);
            SetRef(actionFeedback, "negativeClip", clips.FeedbackNegative);
            EditorUtility.SetDirty(actionFeedback);
        }

        // ------------------------------------------------------------------ Scene wiring

        private static void WireScenarioContent(Scene scene, MachineLockConfig config)
        {
            var scenario = FindInScene(scene, "MNG_Scenario").GetComponent<MachineLockScenario>();
            var press = FindInScene(scene, "MESH_HydraulicPress").GetComponent<MachineLockPress>();
            var panel = FindInScene(scene, "MESH_ElectricalPanel").GetComponent<MachineLockEnergyPanel>();
            var alarm = FindInScene(scene, "MESH_AlarmButton").GetComponent<MachineLockAlarm>();
            var cabinet = FindInScene(scene, "MESH_PpeCabinet").GetComponent<MachineLockPpeCabinet>();
            var spill = FindInScene(scene, "HAZARD_Oil").GetComponent<MachineLockSpillSystem>();
            var valve = FindInScene(scene, "MESH_Valve").GetComponent<MachineLockValve>();
            var extinguisher = FindInScene(scene, "MESH_WaterExtinguisher").GetComponent<MachineLockExtinguisher>();
            var feedback = FindInScene(scene, "MNG_Scenario").GetComponent<MachineLockDamageFeedback>();
            var exitDoor = FindInScene(scene, "MESH_IndustrialDoor_RollUp").GetComponent<MachineLockExitDoor>();

            SetRef(press, "scenario", scenario);
            SetRef(press, "estopInteractable", FindInScene(scene, "MESH_EmergencyStopButton").GetComponent<Interactable>());
            SetRef(press, "testStartInteractable", FindInScene(scene, "MESH_TestStartButton").GetComponent<Interactable>());

            SetRef(panel, "scenario", scenario);
            SetRef(panel, "config", config);
            SetRef(panel, "press", press);

            SetRef(alarm, "scenario", scenario);
            SetRef(alarm, "alarmInteractable", FindInScene(scene, "MESH_AlarmPushButton").GetComponent<Interactable>());
            SetRef(cabinet, "scenario", scenario);
            SetRef(valve, "scenario", scenario);
            SetRef(valve, "spillSystem", spill);
            SetRef(exitDoor, "scenario", scenario);

            GameObject steamObject = FindInScene(scene, "VFX_Steam");
            GameObject smokeObject = FindInScene(scene, "VFX_Smoke");
            SetRef(extinguisher, "scenario", scenario);
            SetRef(extinguisher, "steamBurstVfx", steamObject.GetComponent<ParticleSystem>());
            SetRef(extinguisher, "smokeBurstVfx", smokeObject.GetComponent<ParticleSystem>());

            SetRef(spill, "scenario", scenario);
            SetRef(spill, "config", config);
            SetRef(spill, "energyPanel", panel);
            SetRef(spill, "ppeCabinet", cabinet);
            SetRef(spill, "oilJetVfx", FindInScene(scene, "VFX_OilJet"));
            SetRef(spill, "oilLeakAudio", FindInScene(scene, "VFX_OilJet").GetComponent<AudioSource>());
            SetRef(spill, "puddleHazardTrigger", FindInScene(scene, "ZONE_PuddleHazard").GetComponent<Collider>());
            SetRefArray(spill, "puddleQuadrants", new UnityEngine.Object[]
            {
                FindInScene(scene, "MESH_PuddleQuadrantNE"),
                FindInScene(scene, "MESH_PuddleQuadrantNW"),
                FindInScene(scene, "MESH_PuddleQuadrantSE"),
                FindInScene(scene, "MESH_PuddleQuadrantSW")
            });

            var snapZones = new UnityEngine.Object[8];
            for (int i = 0; i < 4; i++)
                snapZones[i] = FindInScene(scene, "ZONE_Coverage" + (i + 1)).GetComponent<MachineLockSnapZone>();
            for (int i = 0; i < 4; i++)
                snapZones[4 + i] = FindInScene(scene, "ZONE_ConePoint" + (i + 1)).GetComponent<MachineLockSnapZone>();
            SetRefArray(spill, "snapZones", snapZones);

            GameObject spillKit = FindInScene(scene, "MESH_SpillKit");
            foreach (MachineLockCone cone in spillKit.GetComponentsInChildren<MachineLockCone>(true))
            {
                SetRef(cone, "spillSystem", spill);
                SetRef(cone, "config", config);
            }
            foreach (MachineLockAbsorbent absorbent in spillKit.GetComponentsInChildren<MachineLockAbsorbent>(true))
            {
                SetRef(absorbent, "spillSystem", spill);
                SetRef(absorbent, "config", config);
            }

            GameObject manager = FindInScene(scene, "MNG_Scenario");
            SetRef(scenario, "spawnPointTransform", FindInScene(scene, "MNG_SpawnPoint").transform);
            SetRef(scenario, "press", press);
            SetRef(scenario, "alarm", alarm);
            SetRef(scenario, "energyPanel", panel);
            SetRef(scenario, "ppeCabinet", cabinet);
            SetRef(scenario, "spillSystem", spill);
            SetRef(scenario, "extinguisher", extinguisher);
            SetRef(scenario, "damageFeedback", feedback);
            SetRefArray(scenario, "layoutPatterns", new UnityEngine.Object[]
            {
                FindInScene(scene, "PATTERN_A").transform,
                FindInScene(scene, "PATTERN_B").transform,
                FindInScene(scene, "PATTERN_C").transform
            });
            SetRefArray(scenario, "puddlePatterns", new UnityEngine.Object[]
            {
                FindInScene(scene, "PATTERN_PuddleA").transform,
                FindInScene(scene, "PATTERN_PuddleB").transform,
                FindInScene(scene, "PATTERN_PuddleC").transform
            });
            SetRef(scenario, "ppeCabinetTransform", FindInScene(scene, "MESH_PpeCabinet").transform);
            SetRef(scenario, "spillKitTransform", spillKit.transform);
            SetRef(scenario, "extinguisherTransform", FindInScene(scene, "MESH_WaterExtinguisher").transform);
            SetRef(scenario, "obsPpeCabinetTransform", FindInScene(scene, "OBS_PpeCabinet").transform);
            SetRef(scenario, "puddleVisualRoot", FindInScene(scene, "VFX_OilPuddle").transform);
            SetRef(scenario, "smokeVfx", smokeObject.GetComponent<ParticleSystem>());
            SetRef(scenario, "steamVfx", steamObject.GetComponent<ParticleSystem>());
            SetRef(scenario, "ambientAudio", FindInScene(scene, "MNG_Audio").GetComponent<AudioSource>());
            SetRef(scenario, "exitBeaconRenderer", FindInScene(scene, "MESH_ExitBeacon").GetComponent<Renderer>());
            SetRef(scenario, "exitBeaconIdleMaterial", matExitBeaconIdle);
            SetRef(scenario, "exitBeaconReadyMaterial", matExitBeaconReady);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(scenario);
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.name == name) return transform.gameObject;
            throw new InvalidOperationException($"[MachineLock] Scene object not found: {name}");
        }

        private static GameObject EnsureChild(GameObject parent, string name)
        {
            Transform existing = parent.transform.Find(name);
            if (existing != null) return existing.gameObject;
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void MarkStatic(GameObject target)
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target)
                                      | StaticEditorFlags.ContributeGI
                                      | StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(target, flags);
        }

        private static void MarkStaticRecursive(GameObject target)
        {
            foreach (Transform t in target.GetComponentsInChildren<Transform>(true))
                MarkStatic(t.gameObject);
        }

        // ------------------------------------------------------------------ Labels (ADR-0002, ADR-0006)

        private static void PlaceLabels(Scene scene)
        {
            // Strict <= 3 Risk labels pulled cleanly into open space.
            AddLabel(Find("MESH_HydraulicPress"), LabelKind.Risk, "Pressa in moto", "Non avvicinarti", new Vector3(-1.85f, 2.60f, 0f));
            AddLabel(Find("MESH_Thermometer"), LabelKind.Risk, "Olio a 120 °C", "Superficie bollente", new Vector3(-0.55f, 0.35f, 0f));
            AddLabel(Find("VFX_OilPuddle"), LabelKind.Risk, "Pozza d'olio bollente", "Non calpestare", new Vector3(0f, 0.55f, 2.35f));

            // Single order-neutral electrical bay anchor (unnumbered plaques + randomized slots, Issue #43).
            AddLabel(Find("MESH_ElectricalPanel"), LabelKind.ContextHelp, "Quadro elettrico", "Isolamento elettrico LOTO", new Vector3(-0.75f, 1.15f, 0f));

            // Sparse ContextHelp anchors; Exit uses ContextHelp to avoid the shared Interaction "GRIP — Afferra" default.
            AddLabel(Find("MESH_EStopPedestal"), LabelKind.ContextHelp, "Fungo di emergenza", "Premi per fermare la pressa", new Vector3(0f, 1.72f, 0f));
            AddLabel(Find("MESH_AlarmButton"), LabelKind.ContextHelp, "Allarme reparto", "Premi per chiamare i soccorsi", new Vector3(0f, 0.72f, 0.45f));
            AddLabel(Find("MESH_PpeCabinet"), LabelKind.ContextHelp, "Armadio DPI", "Apri e indossa i 4 DPI", new Vector3(0f, 2.38f, 0.45f));
            AddLabel(Find("MESH_SpillKit"), LabelKind.ContextHelp, "Kit antispandimento", "Coni e assorbenti (GRIP)", new Vector3(0f, 1.15f, 0f));
            AddLabel(Find("MESH_Valve"), LabelKind.ContextHelp, "Valvola olio", "Chiudi per fermare la perdita", new Vector3(0f, 1.05f, 0f));
            AddLabel(Find("MESH_WaterExtinguisher"), LabelKind.ContextHelp, "Estintore ad acqua", "Non usare su olio o quadro", new Vector3(0f, 0.95f, 0.40f));
            AddLabel(Find("MESH_Door"), LabelKind.ContextHelp, "Uscita di emergenza", "Punta la porta o il maniglione (TRIGGER o GRIP)", new Vector3(0.35f, 1.95f, 0.85f));
        }

        private static GameObject Find(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.name == name) return transform.gameObject;
            throw new InvalidOperationException($"[MachineLock] Scene object not found for a label: {name}");
        }

        private static void AddLabel(GameObject target, LabelKind kind, string title, string detail, Vector3 offset)
        {
            var anchor = target.GetComponent<LabelAnchor>();
            if (anchor == null) anchor = target.AddComponent<LabelAnchor>();
            var so = new SerializedObject(anchor);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("requiresHighlight").boolValue = false;
            so.FindProperty("title").stringValue = title;
            so.FindProperty("titleKey").stringValue = string.Empty;
            so.FindProperty("detail").stringValue = detail;
            so.FindProperty("detailKey").stringValue = string.Empty;
            so.FindProperty("buttonKey").stringValue = string.Empty;
            so.FindProperty("actionKey").stringValue = string.Empty;
            so.FindProperty("offset").vector3Value = offset;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anchor);
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
        }

        private static void AddScene(List<EditorBuildSettingsScene> target, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;
            foreach (EditorBuildSettingsScene existing in target)
                if (existing.path == path) return;
            target.Add(new EditorBuildSettingsScene(path, true));
        }

        // ------------------------------------------------------------------ Utilities

        private static GameObject NewRoot(Scene scene, string name)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static void DestroyRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }

        /// <summary>Destroys previously generated scenario root GameObjects so Full Scene rebuilds remain idempotent.</summary>
        private static void RemoveBlockoutRoots(Scene scene)
        {
            string[] roots =
            {
                "ENV_Room", "ENV_Press", "ENV_Safety", "HAZARD_Oil", "ZONE_Delimitation",
                "MNG_LayoutPatterns", "MNG_ObservationTargets", "ZONE_Exit", "MNG_Audio", "MNG_Debug"
            };
            foreach (string root in roots) DestroyRoot(scene, root);
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
