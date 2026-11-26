#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SI81.Interaction;
using SI81.Rig;
using SI81.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Builds the Machine Lock scenario: the mission asset values, the config asset, the damage profile,
    /// the blockout assets (materials, textures, prefabs) and the full scenario scene, including the
    /// runtime wiring of every interaction, VFX and audio source. Safe to run again: every asset is
    /// created or updated in place and the scene content roots are rebuilt, never duplicated.
    /// Menu: SI81/MachineLock/Build Scene Shell (P1 shell), SI81/MachineLock/Build Full Scene (P2–P8).
    /// Batchmode entry points: BuildSceneShell, BuildFullScene.
    /// </summary>
    public static partial class MachineLockSceneBuilder
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

        /// <summary>Group texture folder of the Machine Lock scenario.</summary>
        public const string TEXTURE_DIR = "Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock";
        /// <summary>Group material folder of the Machine Lock scenario.</summary>
        public const string MATERIAL_DIR = "Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock";
        /// <summary>Group prefab folder of the Machine Lock scenario.</summary>
        public const string PREFAB_DIR = "Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock";
        /// <summary>Group audio folder of the one-shot clips.</summary>
        public const string SOUND_DIR = "Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock";
        /// <summary>Group audio folder of the ambient loop.</summary>
        public const string MUSIC_DIR = "Assets/_Project/Groups/G_Marius/Audio/Music/MachineLock";
        /// <summary>Damage profile asset used by the URP damage volume (GDD §9.13).</summary>
        public const string DAMAGE_PROFILE_PATH = "Assets/_Project/Groups/G_Marius/Level/Data/SO_MachineLockDamageProfile.asset";

        /// <summary>The sixteen synthesized clips of the GDD §13.1 audio set.</summary>
        public static readonly string[] AUDIO_CLIPS =
        {
            "SFX_PressLoop.wav", "SFX_PressStop.wav", "SFX_EStopClack.wav", "SFX_AlarmButton.wav",
            "SFX_AlarmSiren.wav", "SFX_BreakerOff.wav", "SFX_Lockout.wav", "SFX_OilLeak.wav",
            "SFX_ValveTurn.wav", "SFX_TesterBeep.wav", "SFX_EquipPpe.wav", "SFX_Snap.wav",
            "SFX_SteamBurst.wav", "SFX_Shock.wav", "SFX_Burn.wav"
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


        /// <summary>The twelve production-named blockout prefabs (GDD §14.2).</summary>
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
        private const string CONTEXT =
            "Ruolo: operatore di reparto. Entri nella sala di stampaggio: la pressa da 250 t lavora in modo anomalo, " +
            "l'olio idraulico a 120 °C perde da un tubo e una pozza calda circonda la macchina. " +
            "Metti in sicurezza la sala e avvisa il preposto.";
        private const string DESIGNER_NOTE =
            "Guided vertical slice — G_Marius. GDD: Docs/MachineLock/GDD_MachineLock.md.";

        private const string SHARED_TEXTURE_DIR = "Assets/_Project/Shared/Art/Textures/";
        private const string SHARED_MATERIAL_DIR = "Assets/_Project/Shared/Art/Materials/";
        private const string SHARED_MODEL_DIR = "Assets/_Project/Shared/Art/Models/";
        private const string SHARED_PREFAB_DIR = "Assets/_Project/Shared/Level/Prefabs/";
        private const string SHARED_PROP_DIR = "Assets/_Project/Shared/Oggetti/";
        private const string SHARED_ROOM_DIR = "Assets/_Project/Shared/Level/Prefabs/Chemical_Alert_Room/";

        /// <summary>Spacious stamping bay spawn point inside PFB_OldIndustrialBuilding (ADR-0001).</summary>
        public static readonly Vector3 SPAWN_POSITION = new Vector3(-7.6f, 0f, 0f);
        /// <summary>Spawn yaw facing +X into the stamping bay.</summary>
        public static readonly Vector3 SPAWN_EULER = new Vector3(0f, 90f, 0f);

        /// <summary>Playable active stamping bay half-width along X (18.0 m total, ADR-0001).</summary>
        public const float ROOM_HALF_X = 9.0f;
        /// <summary>Playable active stamping bay half-depth along Z (11.0 m total, ADR-0001).</summary>
        public const float ROOM_HALF_Z = 5.5f;
        /// <summary>Active stamping bay ceiling fixture elevation inside the industrial hall.</summary>
        public const float ROOM_HEIGHT = 6.2f;
        /// <summary>Minimum Dry Perimeter margin on every side around the puddle (ADR-0001).</summary>
        public const float MIN_DRY_PERIMETER = 2.5f;
        /// <summary>Expected sparse World Label anchor count after collapsing electrical bay overlap (ADR-0002).</summary>
        public const int EXPECTED_LABEL_ANCHORS = 11;

        // ------------------------------------------------------------------ Materials cache

        private static Material matConcreteFloor;
        private static Material matWallConcrete;
        private static Material matCeilingConcrete;
        private static Material matBuildingWall;
        private static Material matBuildingFloor;
        private static Material matBuildingWindow;
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

            UpdateMission();
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
        /// Builds or updates the whole P2 blockout: textures, materials, signs, the twelve prefabs and
        /// the full scenario scene. Menu item and batchmode entry point.
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
            EnsureTextures();
            EnsureSignTextures();
            EnsureMaterials();
            MachineLockAudioClips clips = LoadAudioClips();
            EnsurePrefabs(clips);
            VolumeProfile damageProfile = CreateOrUpdateDamageProfile();

            UpdateMission();
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
            BuildExit(scene);
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
            Debug.Log($"[MachineLock] Full blockout ready: {SCENE_PATH}. " +
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

        // ------------------------------------------------------------------ Textures

        private static void EnsureAssetFolders()
        {
            EnsureFolder(CONFIG_PATH);
            EnsureFolder(SCENE_PATH);
            EnsureFolder(TEXTURE_DIR + "/TEX_Placeholder.png");
            EnsureFolder(MATERIAL_DIR + "/MAT_Placeholder.mat");
            EnsureFolder(PREFAB_DIR + "/MESH_Placeholder.prefab");
        }

        private static void EnsureTextures()
        {
            EnsurePbrAndBuildingTextures();
        }

        private static void CopyConcreteTexture(string source, string destinationName, TextureImporterType type,
            bool linear, int androidMaxSize = 1024)
        {
            if (!File.Exists(source))
                throw new InvalidOperationException($"[MachineLock] Shared texture not found: {source}");

            string destination = TEXTURE_DIR + "/" + destinationName;
            bool needsCopy = !File.Exists(destination);
            if (!needsCopy)
            {
                try
                {
                    needsCopy = new FileInfo(destination).Length != new FileInfo(source).Length;
                }
                catch (IOException)
                {
                    needsCopy = false;
                }
            }
            if (needsCopy)
            {
                try
                {
                    File.Copy(source, destination, true);
                }
                catch (IOException)
                {
                    // The Editor may still have the texture memory-mapped from a previous run.
                }
            }
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(destination) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"[MachineLock] TextureImporter missing for {destination}");

            importer.textureType = type;
            importer.sRGBTexture = !linear && type != TextureImporterType.NormalMap;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = androidMaxSize;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(android);

            importer.SaveAndReimport();
            Debug.Log($"[MachineLock] Configured texture {destination} ({type}, 2048 default / {androidMaxSize} ASTC 6x6 Android).");
        }

        private static Texture2D LoadGroupTexture(string fileName)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_DIR + "/" + fileName);
            if (texture == null)
                throw new InvalidOperationException($"[MachineLock] Texture not found: {TEXTURE_DIR}/{fileName}");
            return texture;
        }

        // ------------------------------------------------------------------ Sign textures

        private static void EnsureSignTextures()
        {
            BuildSignTexture("TEX_Sign_Stampaggio.png", "STAMPAGGIO",
                new Color32(20, 24, 27, 255), new Color32(255, 255, 255, 255), new Color32(245, 196, 0, 255));
            BuildSignTexture("TEX_Sign_QuadroGenerale.png", "QUADRO GENERALE",
                new Color32(245, 196, 0, 255), new Color32(20, 20, 20, 255), new Color32(20, 20, 20, 255));
            BuildSignTexture("TEX_Sign_Dpi.png", "DPI",
                new Color32(10, 60, 120, 255), new Color32(255, 255, 255, 255), new Color32(120, 200, 255, 255));
            BuildSignTexture("TEX_Sign_KitAntispandimento.png", "KIT ANTISPANDIMENTO",
                new Color32(190, 82, 12, 255), new Color32(255, 255, 255, 255), new Color32(30, 30, 30, 255));
            BuildSignTexture("TEX_Sign_Uscita.png", "USCITA",
                new Color32(0, 120, 55, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255));
            EnsureDiegeticPlaqueTextures();
        }

        /// <summary>
        /// Renders a crisp 1024×512 industrial sign with a procedural 5×7 bitmap font and imports it
        /// for Quest (1024 max, ASTC 4×4 on Android). No Canvas and no font asset are required.
        /// </summary>
        private static void BuildSignTexture(string fileName, string title, Color32 background, Color32 text, Color32 border)
        {
            const int width = 1024;
            const int height = 512;
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = background;

            const int borderWidth = 14;
            FillRect(pixels, width, height, 0, 0, width, borderWidth, border);
            FillRect(pixels, width, height, 0, height - borderWidth, width, borderWidth, border);
            FillRect(pixels, width, height, 0, 0, borderWidth, height, border);
            FillRect(pixels, width, height, width - borderWidth, 0, borderWidth, height, border);

            int scale = 1;
            for (int candidate = 12; candidate >= 1; candidate--)
            {
                int candidateWidth = title.Length * 6 * candidate - candidate;
                if (candidateWidth <= width - 140 && 7 * candidate <= height - 140)
                {
                    scale = candidate;
                    break;
                }
            }

            int textWidth = title.Length * 6 * scale - scale;
            int x0 = (width - textWidth) / 2;
            int y0 = (height - 7 * scale) / 2;
            for (int i = 0; i < title.Length; i++)
            {
                string[] glyph;
                if (!GLYPHS.TryGetValue(char.ToUpperInvariant(title[i]), out glyph)) glyph = GLYPHS[' '];
                int glyphX = x0 + i * 6 * scale;
                for (int row = 0; row < 7; row++)
                {
                    for (int column = 0; column < 5; column++)
                    {
                        if (glyph[row][column] != '1') continue;
                        int pixelY = y0 + (6 - row) * scale;
                        FillRect(pixels, width, height, glyphX + column * scale, pixelY, scale, scale, text);
                    }
                }
            }

            string path = TEXTURE_DIR + "/" + fileName;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"[MachineLock] TextureImporter missing for {path}");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.format = TextureImporterFormat.ASTC_4x4;
            android.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }

        private static void FillRect(Color32[] pixels, int width, int height, int x, int y, int w, int h, Color32 color)
        {
            int xMin = Mathf.Max(0, x);
            int xMax = Mathf.Min(width, x + w);
            int yMin = Mathf.Max(0, y);
            int yMax = Mathf.Min(height, y + h);
            for (int py = yMin; py < yMax; py++)
            {
                int row = py * width;
                for (int px = xMin; px < xMax; px++) pixels[row + px] = color;
            }
        }

        private static readonly Dictionary<char, string[]> GLYPHS = BuildGlyphs();

        private static Dictionary<char, string[]> BuildGlyphs()
        {
            var glyphs = new Dictionary<char, string[]>
            {
                { ' ', new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" } },
                { '-', new[] { "00000", "00000", "00000", "11111", "00000", "00000", "00000" } },
                { '.', new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00100" } },
                { 'A', new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" } },
                { 'B', new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" } },
                { 'C', new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" } },
                { 'D', new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" } },
                { 'E', new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" } },
                { 'F', new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" } },
                { 'G', new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01111" } },
                { 'H', new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" } },
                { 'I', new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" } },
                { 'J', new[] { "00111", "00010", "00010", "00010", "00010", "10010", "01100" } },
                { 'K', new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" } },
                { 'L', new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" } },
                { 'M', new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" } },
                { 'N', new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" } },
                { 'O', new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" } },
                { 'P', new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" } },
                { 'Q', new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" } },
                { 'R', new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" } },
                { 'S', new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" } },
                { 'T', new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" } },
                { 'U', new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" } },
                { 'V', new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" } },
                { 'W', new[] { "10001", "10001", "10001", "10101", "10101", "11011", "10001" } },
                { 'X', new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" } },
                { 'Y', new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" } },
                { 'Z', new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" } },
                { '0', new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" } },
                { '1', new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" } },
                { '2', new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" } },
                { '3', new[] { "11111", "00010", "00100", "00010", "00001", "10001", "01110" } },
                { '4', new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" } },
                { '5', new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" } },
                { '6', new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" } },
                { '7', new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" } },
                { '8', new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" } },
                { '9', new[] { "01110", "10001", "10001", "01111", "00001", "00010", "01100" } }
            };
            return glyphs;
        }

        // ------------------------------------------------------------------ Materials

        private static void EnsureMaterials()
        {
            matConcreteFloor = CopyMaterial(SHARED_MATERIAL_DIR + "Material.036.mat", "MAT_ConcreteFloorWorn");
            matConcreteFloor.SetTexture("_BaseMap", LoadGroupTexture("TEX_ConcreteFloorWorn_Albedo.jpg"));
            matConcreteFloor.SetTexture("_MainTex", LoadGroupTexture("TEX_ConcreteFloorWorn_Albedo.jpg"));
            matConcreteFloor.SetTexture("_BumpMap", LoadGroupTexture("TEX_ConcreteFloorWorn_Normal.exr"));
            matConcreteFloor.SetTexture("_MetallicGlossMap", LoadGroupTexture("TEX_ConcreteFloorWorn_Rough.jpg"));
            matConcreteFloor.SetTextureScale("_BaseMap", new Vector2(9f, 5.5f));
            matConcreteFloor.SetTextureScale("_BumpMap", new Vector2(9f, 5.5f));
            matConcreteFloor.SetTextureScale("_MetallicGlossMap", new Vector2(9f, 5.5f));
            matConcreteFloor.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(matConcreteFloor);

            // Dedicated scenario-owned URP Lit materials for PFB_OldIndustrialBuilding (ADR-0001).
            // Displacement map is deliberately omitted on Quest 3.
            matBuildingWall = LitMaterial("MAT_IndustrialBuilding_Wall", new Color(0.84f, 0.82f, 0.80f), 0.05f, 0.24f);
            AssignOptionalTexture(matBuildingWall, "_BaseMap", SHARED_BUILDING_TEX_DIR + "/buildingbase.jpg");
            AssignOptionalTexture(matBuildingWall, "_MainTex", SHARED_BUILDING_TEX_DIR + "/buildingbase.jpg");
            AssignOptionalTexture(matBuildingWall, "_BumpMap", SHARED_BUILDING_TEX_DIR + "/buildingNormalMap.jpg", true);
            AssignOptionalTexture(matBuildingWall, "_OcclusionMap", SHARED_BUILDING_TEX_DIR + "/buildingAmbientOcclusionMap.jpg");
            SetFloat(matBuildingWall, "_Cull", 0f); // Double-sided safeguard for interior visibility
            EditorUtility.SetDirty(matBuildingWall);

            matBuildingFloor = LitMaterial("MAT_IndustrialBuilding_Floor", new Color(0.55f, 0.54f, 0.52f), 0f, 0.18f);
            AssignOptionalTexture(matBuildingFloor, "_BaseMap", SHARED_BUILDING_TEX_DIR + "/TexturesCom_ConcreteBare0348_1_S.jpg");
            AssignOptionalTexture(matBuildingFloor, "_MainTex", SHARED_BUILDING_TEX_DIR + "/TexturesCom_ConcreteBare0348_1_S.jpg");
            EditorUtility.SetDirty(matBuildingFloor);

            matBuildingWindow = LitMaterial("MAT_IndustrialBuilding_Window", new Color(0.62f, 0.72f, 0.82f, 0.55f), 0.05f, 0.88f,
                new Color(0.32f, 0.38f, 0.44f));
            ConfigureTransparent(matBuildingWindow, 0.55f, new Color(0.32f, 0.38f, 0.44f));

            // Polyhaven CC0 PBR materials (green_metal_rust for press/machinery, concrete_wall_009 for bay concrete).
            matPressSteelGreen = LitMaterial("MAT_PressSteel_Green", new Color(0.82f, 0.88f, 0.82f), 0.35f, 0.42f);
            AssignOptionalTexture(matPressSteelGreen, "_BaseMap", TEXTURE_DIR + "/TEX_GreenMetalRust_Albedo.jpg");
            AssignOptionalTexture(matPressSteelGreen, "_MainTex", TEXTURE_DIR + "/TEX_GreenMetalRust_Albedo.jpg");
            AssignOptionalTexture(matPressSteelGreen, "_BumpMap", TEXTURE_DIR + "/TEX_GreenMetalRust_Normal.exr", true);
            AssignOptionalTexture(matPressSteelGreen, "_MetallicGlossMap", TEXTURE_DIR + "/TEX_GreenMetalRust_Rough.jpg");
            matPressSteelGreen.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
            matPressSteelGreen.SetTextureScale("_BumpMap", new Vector2(2f, 2f));
            EditorUtility.SetDirty(matPressSteelGreen);

            matConcreteWallPbr = LitMaterial("MAT_ConcreteWall_PBR", new Color(0.76f, 0.77f, 0.75f), 0f, 0.18f);
            AssignOptionalTexture(matConcreteWallPbr, "_BaseMap", TEXTURE_DIR + "/TEX_ConcreteWall009_Albedo.jpg");
            AssignOptionalTexture(matConcreteWallPbr, "_MainTex", TEXTURE_DIR + "/TEX_ConcreteWall009_Albedo.jpg");
            AssignOptionalTexture(matConcreteWallPbr, "_BumpMap", TEXTURE_DIR + "/TEX_ConcreteWall009_Normal.exr", true);
            matConcreteWallPbr.SetTextureScale("_BaseMap", new Vector2(2.5f, 1.5f));
            matConcreteWallPbr.SetTextureScale("_BumpMap", new Vector2(2.5f, 1.5f));
            EditorUtility.SetDirty(matConcreteWallPbr);

            matPaintedSteel = CopyMaterial(SHARED_MATERIAL_DIR + "MAT_OfficePropWhiteMetallic.mat", "MAT_PaintedSteel");
            matPolishedSteel = CopyMaterial(SHARED_MATERIAL_DIR + "MAT_OfficePropPolishedMetal.mat", "MAT_PolishedSteel");

            matWallConcrete = LitMaterial("MAT_WallConcrete", new Color(0.42f, 0.43f, 0.41f), 0f, 0.12f);
            matCeilingConcrete = LitMaterial("MAT_CeilingConcrete", new Color(0.22f, 0.24f, 0.26f), 0.1f, 0.14f);
            matDarkSlate = LitMaterial("MAT_DarkSlate", new Color(0.14f, 0.16f, 0.19f), 0.35f, 0.42f);
            matCyanAccent = LitMaterial("MAT_CyanAccent", new Color(0.06f, 0.54f, 0.84f), 0.25f, 0.55f);
            matOil = LitMaterial("MAT_Oil", new Color(0.05f, 0.03f, 0.02f), 0f, 0.92f);
            matEStopRed = LitMaterial("MAT_EmergencyStop_Red", new Color(0.78f, 0.05f, 0.04f), 0.05f, 0.55f);
            matEStopYellow = LitMaterial("MAT_EmergencyStop_Yellow", new Color(0.96f, 0.74f, 0.04f), 0.05f, 0.45f);
            matHmiScreen = LitMaterial("MAT_HMI_Screen", new Color(0.02f, 0.02f, 0.02f), 0f, 0.6f,
                new Color(0.95f, 0.14f, 0.04f) * 1.6f);
            matApronRubber = LitMaterial("MAT_Apron_Rubber", new Color(0.09f, 0.12f, 0.14f), 0.08f, 0.42f);
            matHeatGlove = LitMaterial("MAT_HeatGlove", new Color(0.92f, 0.34f, 0.05f), 0.10f, 0.28f);
            AssignOptionalTexture(matHeatGlove, "_BumpMap", TEXTURE_DIR + "/TEX_Glove_Normal.png", true);
            matSafetyBootLeather = LitMaterial("MAT_SafetyBoot_Leather", new Color(0.15f, 0.12f, 0.10f), 0.08f, 0.32f);
            matSafetyBootSole = LitMaterial("MAT_SafetyBoot_Sole", new Color(0.88f, 0.56f, 0.05f), 0.05f, 0.35f);
            matLatex = LitMaterial("MAT_Latex", new Color(0.94f, 0.96f, 0.92f), 0f, 0.88f);
            matSurgicalMaskBlue = LitMaterial("MAT_SurgicalMask_Blue", new Color(0.56f, 0.84f, 0.95f), 0f, 0.22f);
            matAbsorbentMat = LitMaterial("MAT_AbsorbentMat", new Color(0.85f, 0.83f, 0.72f), 0f, 0.15f);
            AssignOptionalTexture(matAbsorbentMat, "_BumpMap", TEXTURE_DIR + "/TEX_AbsorbentMat_Normal.png", true);
            matAbsorbentSock = LitMaterial("MAT_AbsorbentSock", new Color(0.88f, 0.86f, 0.78f), 0f, 0.18f);
            AssignOptionalTexture(matAbsorbentSock, "_BaseMap", TEXTURE_DIR + "/TEX_AbsorbentSock_Albedo.png");
            AssignOptionalTexture(matAbsorbentSock, "_MainTex", TEXTURE_DIR + "/TEX_AbsorbentSock_Albedo.png");

            matFloorMarking = LitMaterial("MAT_FloorSafetyMarking_Yellow", new Color(0.96f, 0.74f, 0.04f), 0f, 0.3f,
                new Color(0.42f, 0.30f, 0.02f));
            matBeaconRed = LitMaterial("MAT_BeaconRed", new Color(0.55f, 0.02f, 0.02f), 0f, 0.75f,
                new Color(0.9f, 0.05f, 0.03f) * 1.4f);
            matExitBeaconIdle = LitMaterial("MAT_ExitBeacon_Idle", new Color(0.08f, 0.26f, 0.14f), 0f, 0.5f,
                new Color(0.08f, 0.32f, 0.14f));
            matExitBeaconReady = LitMaterial("MAT_ExitBeacon_Ready", new Color(0.08f, 0.92f, 0.32f), 0f, 0.65f,
                new Color(0.15f, 1.85f, 0.45f));
            matLedRed = LitMaterial("MAT_LedRed", new Color(0.45f, 0.02f, 0.02f), 0f, 0.7f,
                new Color(1f, 0.05f, 0.03f) * 2f);
            matLedGreen = LitMaterial("MAT_LedGreen", new Color(0.02f, 0.35f, 0.08f), 0f, 0.7f,
                new Color(0.05f, 1f, 0.2f) * 2f);
            matLightFixture = LitMaterial("MAT_LightFixture", new Color(0.90f, 0.92f, 0.88f), 0f, 0.4f,
                new Color(1f, 0.97f, 0.9f) * 2.2f);
            matHmiOff = LitMaterial("MAT_HMI_Off", new Color(0.015f, 0.015f, 0.018f), 0f, 0.12f);
            matLedOff = LitMaterial("MAT_LedOff", new Color(0.07f, 0.07f, 0.075f), 0f, 0.35f);
            matParticleSoft = ParticleMaterial("MAT_ParticleSoft", new Color(0.66f, 0.66f, 0.64f, 0.5f));

            matFaceShieldVisor = LitMaterial("MAT_FaceShield_Visor", new Color(0.55f, 0.82f, 0.96f, 0.28f), 0f, 0.95f);
            ConfigureTransparent(matFaceShieldVisor, 0.28f);
            matGhostSnap = LitMaterial("MAT_GhostSnap", new Color(0.16f, 0.96f, 0.45f, 0.48f), 0f, 0.5f);
            ConfigureTransparent(matGhostSnap, 0.48f, new Color(0.12f, 0.95f, 0.35f));
            matBaselineZone = LitMaterial("MAT_BaselineZone", new Color(0.98f, 0.84f, 0.12f, 0.38f), 0f, 0.4f);
            ConfigureTransparent(matBaselineZone, 0.38f, new Color(0.68f, 0.58f, 0.06f));

            matDamageOverlay = EnsureDamageOverlayMaterial();

            EnsureSignMaterial("MAT_Sign_Stampaggio", "TEX_Sign_Stampaggio.png");
            EnsureSignMaterial("MAT_Sign_QuadroGenerale", "TEX_Sign_QuadroGenerale.png");
            EnsureSignMaterial("MAT_Sign_Dpi", "TEX_Sign_Dpi.png");
            EnsureSignMaterial("MAT_Sign_KitAntispandimento", "TEX_Sign_KitAntispandimento.png");
            EnsureSignMaterial("MAT_Sign_Uscita", "TEX_Sign_Uscita.png");
            EnsureDiegeticPlaqueMaterials();

            AssetDatabase.SaveAssets();
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

        /// <summary>
        /// Creates or updates the soft, transparent material shared by every particle system, so the VFX
        /// render on Quest with the URP Particle/Unlit shader instead of falling back to pink.
        /// </summary>
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

        private static void EnsureSignMaterial(string materialName, string textureFile, float emissionScale = 0.65f)
        {
            Texture2D texture = LoadGroupTexture(textureFile);
            var material = LitMaterial(materialName, Color.white, 0f, 0.25f, Color.white * emissionScale);
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

        // ------------------------------------------------------------------ Damage profile (GDD §9.13)

        /// <summary>
        /// Creates or updates the URP damage profile: a red Vignette plus a mild Color Adjustments
        /// override. Both volume components are real sub-assets of the profile asset.
        /// </summary>
        private static VolumeProfile CreateOrUpdateDamageProfile()
        {
            EnsureFolder(DAMAGE_PROFILE_PATH);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DAMAGE_PROFILE_PATH);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, DAMAGE_PROFILE_PATH);
                Debug.Log($"[MachineLock] Created {DAMAGE_PROFILE_PATH}.");
            }

            Vignette vignette;
            if (!profile.TryGet(out vignette) || vignette == null)
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

            ColorAdjustments adjustments;
            if (!profile.TryGet(out adjustments) || adjustments == null)
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

        // ------------------------------------------------------------------ Audio (GDD §13.1)

        /// <summary>The sixteen clips of the audio set, in the order of <see cref="AUDIO_CLIPS"/>.</summary>
        private sealed class MachineLockAudioClips
        {
            public AudioClip PressLoop;
            public AudioClip PressStop;
            public AudioClip EStopClack;
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
            public AudioClip Shock;
            public AudioClip Burn;
            public AudioClip AmbientFactory;
        }

        /// <summary>
        /// Loads the sixteen audio clips. A missing clip is a warning in batch mode and an error in an
        /// interactive full build, so the batch pipeline can report it through the validator.
        /// </summary>
        private static MachineLockAudioClips LoadAudioClips()
        {
            return new MachineLockAudioClips
            {
                PressLoop = LoadAudioClip(SOUND_DIR, "SFX_PressLoop.wav"),
                PressStop = LoadAudioClip(SOUND_DIR, "SFX_PressStop.wav"),
                EStopClack = LoadAudioClip(SOUND_DIR, "SFX_EStopClack.wav"),
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
                Shock = LoadAudioClip(SOUND_DIR, "SFX_Shock.wav"),
                Burn = LoadAudioClip(SOUND_DIR, "SFX_Burn.wav"),
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

        private static void SetText(UnityEngine.Object target, string propertyName, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"[MachineLock] Serialized property not found: {target.GetType().Name}.{propertyName}");
            property.stringValue = value;
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
            // An additive scene needs a saved host scene: batchmode starts on an untitled scene.
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
                BuildIndustrialDoorPrefab(temp);
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
            Debug.Log($"[MachineLock] Prefab saved: {path}");
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

        // -- MESH_Cone ---------------------------------------------------------

        private static void BuildConePrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_Cone");
            AddModelChild(root, "MESH_ConeBody", SHARED_MODEL_DIR + "MESH_Magazzino_Cono.fbx",
                new Vector3(0.3f, 0.5f, 0.3f), Vector3.zero, true, false, null);
            AddBoxCollider(root, new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f));

            Interactable interactable = AddInteractable(root, false, true, true);
            var cone = root.AddComponent<MachineLockCone>();
            AudioSource snapAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
            SetRef(cone, "interactable", interactable);
            SetRef(cone, "oneShotAudio", snapAudio);
            SetRef(cone, "snapClip", clips.Snap);
            SavePrefab(root);
        }

        // -- MESH_AbsorbentMat -------------------------------------------------

        private static void BuildAbsorbentMatPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_AbsorbentMat");
            AddModelChild(root, "MESH_AbsorbentMatBody", SHARED_MODEL_DIR + "MESH_PANNO_ASSORBENTE_TEAM.fbx",
                new Vector3(1.0f, 0.03f, 0.8f), new Vector3(0f, 0.015f, 0f), false, false, matAbsorbentMat);
            AddBoxCollider(root, new Vector3(0f, 0.03f, 0f), new Vector3(1.0f, 0.06f, 0.8f));

            Interactable interactable = AddInteractable(root, false, true, true);
            var absorbent = root.AddComponent<MachineLockAbsorbent>();
            AudioSource snapAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
            SetRef(absorbent, "interactable", interactable);
            SetRef(absorbent, "oneShotAudio", snapAudio);
            SetRef(absorbent, "snapClip", clips.Snap);
            SetInt(absorbent, "absorbentKind", (int)MachineLockAbsorbent.AbsorbentKind.Mat);
            SavePrefab(root);
        }

        // -- MESH_AbsorbentSock ------------------------------------------------

        private static void BuildAbsorbentSockPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_AbsorbentSock");
            AddModelChild(root, "MESH_AbsorbentSockBody", SHARED_MODEL_DIR + "MESH_SALSICCIOTTO_ONEDRIVE.fbx",
                new Vector3(1.2f, 0.16f, 0.22f), new Vector3(0f, 0.08f, 0f), true, false, matAbsorbentSock);
            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 0;
            collider.height = 1.2f;
            collider.radius = 0.10f;
            collider.center = new Vector3(0f, 0.08f, 0f);

            Interactable interactable = AddInteractable(root, false, true, true);
            var absorbent = root.AddComponent<MachineLockAbsorbent>();
            AudioSource snapAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
            SetRef(absorbent, "interactable", interactable);
            SetRef(absorbent, "oneShotAudio", snapAudio);
            SetRef(absorbent, "snapClip", clips.Snap);
            SetInt(absorbent, "absorbentKind", (int)MachineLockAbsorbent.AbsorbentKind.Sock);
            SavePrefab(root);
        }

        // -- MESH_Valve --------------------------------------------------------

        private static void BuildValvePrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_Valve");
            AddCylinder(root, "MESH_ValvePipe", 0.11f, 0.6f, Vector3.zero, new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddCylinder(root, "MESH_ValveFlangeW", 0.16f, 0.04f, new Vector3(-0.26f, 0f, 0f), new Vector3(0f, 0f, 90f), matDarkSlate, false);
            AddCylinder(root, "MESH_ValveFlangeE", 0.16f, 0.04f, new Vector3(0.26f, 0f, 0f), new Vector3(0f, 0f, 90f), matDarkSlate, false);
            AddCylinder(root, "MESH_ValveStem", 0.035f, 0.24f, new Vector3(0f, 0.14f, 0f), Vector3.zero, matPolishedSteel, false);

            // Bright unmistakable red handwheel with cross spokes and pulsing Guided indicator halo.
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

            // Flush Diegetic Plaque mounted cleanly on a vertical stanchion above the valve (double-sided N/S readable).
            AddBox(root, "MESH_ValveSignPost", new Vector3(0.03f, 0.42f, 0.03f), new Vector3(0f, 0.46f, -0.18f), matDarkSlate, false);
            AddBox(root, "MESH_ValvePlaqueBack", new Vector3(0.54f, 0.28f, 0.025f), new Vector3(0f, 0.68f, -0.18f), matDarkSlate, false);
            AddPlaque(root, "MESH_PlaqueValveFront", "MAT_Plaque_Valve", new Vector2(0.52f, 0.26f),
                new Vector3(0f, 0.68f, -0.165f), new Vector3(0f, 180f, 0f));
            AddPlaque(root, "MESH_PlaqueValveBack", "MAT_Plaque_Valve", new Vector2(0.52f, 0.26f),
                new Vector3(0f, 0.68f, -0.195f), Vector3.zero);

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

        // -- MESH_WaterExtinguisher -------------------------------------------

        private static void BuildWaterExtinguisherPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_WaterExtinguisher");
            AddModelChild(root, "MESH_ExtinguisherBody", SHARED_PROP_DIR + "Estintore/Estintore.fbx",
                new Vector3(0.18f, 0.66f, 0.18f), Vector3.zero, false, false, null);
            AddBox(root, "MESH_ExtinguisherBracket", new Vector3(0.24f, 0.48f, 0.03f),
                new Vector3(0f, 0.12f, -0.12f), matDarkSlate, false);
            AddBox(root, "MESH_ExtinguisherClampTop", new Vector3(0.20f, 0.03f, 0.09f),
                new Vector3(0f, 0.28f, -0.04f), matPaintedSteel, false);
            AddBox(root, "MESH_ExtinguisherClampBottom", new Vector3(0.20f, 0.03f, 0.09f),
                new Vector3(0f, -0.15f, -0.04f), matPaintedSteel, false);

            // Shared fire extinguisher wall sign + flush Diegetic Plaque explaining Class A water warning.
            AddModelChild(root, "MESH_ExtinguisherWallSign", SHARED_MODEL_DIR + "MESH_Folgorazione_CartelloEstintore.fbx",
                new Vector3(0.32f, 0.32f, 0.04f), new Vector3(0f, 0.72f, -0.10f), false, true, null);
            AddBox(root, "MESH_ExtinguisherPlaqueBacking", new Vector3(0.48f, 0.25f, 0.02f),
                new Vector3(0f, 0.44f, -0.11f), matDarkSlate, false);
            AddPlaque(root, "MESH_PlaqueExtinguisher", "MAT_Plaque_Extinguisher", new Vector2(0.46f, 0.23f),
                new Vector3(0f, 0.44f, -0.095f), new Vector3(0f, 180f, 0f));

            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.height = 0.75f;
            collider.radius = 0.14f;
            collider.center = new Vector3(0f, 0.04f, 0f);

            Interactable interactable = AddInteractable(root, true, false, false);
            var extinguisher = root.AddComponent<MachineLockExtinguisher>();
            ParticleSystem spray = AddParticles(root, "VFX_WaterSpray", new Vector3(0.16f, 0.55f, 0f), Vector3.zero,
                matParticleSoft, 100, false, 40f, new Color(0.72f, 0.8f, 0.9f, 0.55f), 0.12f, 2.5f, 1.2f, 0.05f,
                ParticleSystemShapeType.Cone, false);
            AudioSource sprayAudio = AddAudioSource(root, null, false, false, 1f, 0.85f);
            SetRef(extinguisher, "extinguisherInteractable", interactable);
            SetRef(extinguisher, "waterSprayVfx", spray);
            SetRef(extinguisher, "oneShotAudio", sprayAudio);
            SetRef(extinguisher, "steamBurstClip", clips.SteamBurst);
            SavePrefab(root);
        }

        // -- MESH_IndustrialDoor_RollUp ---------------------------------------

        private static void BuildIndustrialDoorPrefab(Scene temp)
        {
            var root = NewPrefabRoot(temp, "MESH_IndustrialDoor_RollUp");
            // Generous 2.0 x 2.5 m clear doorway; posts stay clear of the walk-through path.
            AddBox(root, "MESH_DoorPostLeft", new Vector3(0.16f, 2.6f, 0.16f), new Vector3(0f, 1.3f, -1.08f), matDarkSlate, true);
            AddBox(root, "MESH_DoorPostRight", new Vector3(0.16f, 2.6f, 0.16f), new Vector3(0f, 1.3f, 1.08f), matDarkSlate, true);
            AddBox(root, "MESH_DoorPostStripeLeft", new Vector3(0.17f, 2.4f, 0.04f), new Vector3(0f, 1.25f, -0.99f), matEStopYellow, false);
            AddBox(root, "MESH_DoorPostStripeRight", new Vector3(0.17f, 2.4f, 0.04f), new Vector3(0f, 1.25f, 0.99f), matEStopYellow, false);
            AddBox(root, "MESH_DoorHeader", new Vector3(0.22f, 0.24f, 2.36f), new Vector3(0f, 2.62f, 0f), matDarkSlate, false);
            GameObject rollHousing = AddModelChild(root, "MESH_DoorRollHousing", SHARED_ROOM_DIR + "MESH_PORTA_CAMION_TEAM.fbx",
                new Vector3(0.28f, 0.42f, 2.25f), new Vector3(0f, 2.80f, 0f), false, false, null);
            rollHousing.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Flush overhead Exit Diegetic Plaque facing into the room (+X).
            AddPlaque(root, "MESH_PlaqueExitHeader", "MAT_Plaque_ExitHeader", new Vector2(1.4f, 0.42f),
                new Vector3(0.13f, 2.62f, 0f), new Vector3(0f, -90f, 0f));
            SavePrefab(root);
        }

        // -- MESH_EStopPedestal -----------------------------------------------

        private static void BuildEStopPrefab(Scene temp)
        {
            var root = NewPrefabRoot(temp, "MESH_EStopPedestal");
            AddBox(root, "MESH_EStopPedestalFoot", new Vector3(0.52f, 0.08f, 0.52f), new Vector3(0f, 0.04f, 0f), matEStopYellow, true);
            AddBox(root, "MESH_EStopPedestalBase", new Vector3(0.42f, 1.12f, 0.40f), new Vector3(0f, 0.60f, 0f), matDarkSlate, true);
            // Yellow safety backplate on the +Z front face directly behind the buttons.
            AddBox(root, "MESH_EStopBackplate", new Vector3(0.44f, 0.62f, 0.05f), new Vector3(0f, 0.88f, 0.21f), matEStopYellow, false);
            AddModelChild(root, "MESH_EStopPushbuttonTrim", SHARED_MODEL_DIR + "MESH_PULSANTI_TEAM.fbx",
                new Vector3(0.28f, 0.22f, 0.04f), new Vector3(0f, 0.56f, 0.22f), false, true, null);

            GameObject stopButton = AddCylinder(root, "MESH_EmergencyStopButton", 0.08f, 0.08f,
                new Vector3(0f, 1.02f, 0.27f), new Vector3(90f, 0f, 0f), matEStopRed, false);
            AddCylinder(root, "MESH_EmergencyStopStem", 0.04f, 0.06f, new Vector3(0f, 1.02f, 0.22f), new Vector3(90f, 0f, 0f), matEStopYellow, false);
            GameObject startButton = AddCylinder(root, "MESH_TestStartButton", 0.055f, 0.06f,
                new Vector3(0f, 0.72f, 0.26f), new Vector3(90f, 0f, 0f), matLedGreen, false);

            // Flush angled top Diegetic Plaque explaining red E-stop vs green try-start.
            AddBox(root, "MESH_EStopPlaqueBack", new Vector3(0.48f, 0.26f, 0.025f), new Vector3(0f, 1.30f, 0.16f), matDarkSlate, false);
            AddPlaque(root, "MESH_PlaqueEStop", "MAT_Plaque_EStop", new Vector2(0.46f, 0.24f),
                new Vector3(0f, 1.30f, 0.18f), new Vector3(0f, 180f, 0f));

            AddWorldBoxCollider(stopButton, stopButton.transform.position, new Vector3(0.20f, 0.20f, 0.18f));
            AddInteractable(stopButton, true, false, false);
            AddWorldBoxCollider(startButton, startButton.transform.position, new Vector3(0.16f, 0.16f, 0.16f));
            AddInteractable(startButton, true, false, false);
            SavePrefab(root);
        }

        // -- MESH_AlarmButton -------------------------------------------------

        private static void BuildAlarmPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_AlarmButton");
            AddBox(root, "MESH_AlarmBox", new Vector3(0.44f, 0.56f, 0.16f), Vector3.zero, matEStopRed, true);
            AddBox(root, "MESH_AlarmTrim", new Vector3(0.36f, 0.44f, 0.04f), new Vector3(0f, 0f, 0.08f), matDarkSlate, false);
            GameObject pushButton = AddCylinder(root, "MESH_AlarmPushButton", 0.075f, 0.06f,
                new Vector3(0f, 0.04f, 0.12f), new Vector3(90f, 0f, 0f), matEStopRed, false);
            AddWorldBoxCollider(pushButton, pushButton.transform.position, new Vector3(0.22f, 0.22f, 0.18f));
            Interactable buttonInteractable = AddInteractable(pushButton, true, false, false);

            // Flush Diegetic Plaque below the alarm box.
            AddBox(root, "MESH_AlarmPlaqueBack", new Vector3(0.50f, 0.26f, 0.025f), new Vector3(0f, -0.44f, 0.06f), matDarkSlate, false);
            AddPlaque(root, "MESH_PlaqueAlarm", "MAT_Plaque_Alarm", new Vector2(0.48f, 0.24f),
                new Vector3(0f, -0.44f, 0.08f), new Vector3(0f, 180f, 0f));

            var beacon = NewChild(root, "VFX_AlarmBeacon", new Vector3(0f, 0.42f, 0f));
            GameObject dome = AddSphere(beacon, "MESH_AlarmBeaconDome", 0.16f, Vector3.zero, matBeaconRed, false);
            var light = beacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.1f, 0.05f);
            light.intensity = 2.2f;
            light.range = 10f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.enabled = false;

            AudioSource sirenAudio = AddAudioSource(root, clips.AlarmSiren, true, false, 1f, 0.85f);
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

        // -- MESH_ElectricalPanel (3 physically separated color-coded Sub-Stations) --

        private static void BuildElectricalPanelPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_ElectricalPanel");

            // Wide 2.5 m industrial wall-bay mounting backboard (facing -X into the stamping bay).
            AddBox(root, "MESH_ElectricalBayBackboard", new Vector3(0.08f, 1.50f, 2.50f), new Vector3(0.10f, 0.05f, 0f), matDarkSlate, true);
            AddBox(root, "MESH_ElectricalBayTopRail", new Vector3(0.12f, 0.07f, 2.54f), new Vector3(0.08f, 0.80f, 0f), matEStopYellow, false);
            AddBox(root, "MESH_ElectricalBayBottomRail", new Vector3(0.12f, 0.07f, 2.54f), new Vector3(0.08f, -0.70f, 0f), matEStopYellow, false);

            // ---- SUB-STATION 1 (Z = -0.88): MAIN DISCONNECT SWITCH (Red / Dark Slate) ----
            var subMain = NewChild(root, "SUB_MainDisconnect", new Vector3(0f, 0f, -0.88f));
            AddBox(subMain, "MESH_MainStationBack", new Vector3(0.14f, 0.92f, 0.66f), new Vector3(0.02f, 0.14f, 0f), matEStopRed, false);
            AddModelChild(subMain, "MESH_PanelCabinet", SHARED_PROP_DIR + "Quadro Elettrico/Quadro Elettrico.fbx",
                new Vector3(0.22f, 0.78f, 0.56f), new Vector3(-0.02f, 0.14f, 0f), false, false, null);
            GameObject disconnectSwitch = AddModelChild(subMain, "MESH_DisconnectSwitch", SHARED_PROP_DIR + "Manopola.fbx",
                new Vector3(0.24f, 0.24f, 0.24f), new Vector3(-0.18f, 0.14f, 0f), false, true, matEStopRed);
            AddBox(disconnectSwitch, "MESH_DisconnectHandleBar", new Vector3(0.04f, 0.26f, 0.05f),
                new Vector3(-0.04f, 0f, 0f), matEStopYellow, false);
            GameObject mainOnLed = AddSphere(subMain, "MESH_MainOnLed", 0.07f, new Vector3(-0.15f, 0.44f, -0.14f), matLedRed, false);
            GameObject mainOffLed = AddSphere(subMain, "MESH_MainOffLed", 0.07f, new Vector3(-0.15f, 0.44f, 0.14f), matLedOff, false);
            AddPlaque(subMain, "MESH_PlaqueMainSwitch", "MAT_Plaque_MainSwitch", new Vector2(0.60f, 0.30f),
                new Vector3(-0.06f, -0.46f, 0f), new Vector3(0f, 90f, 0f));

            AddWorldBoxCollider(disconnectSwitch, subMain.transform.TransformPoint(new Vector3(-0.14f, 0.14f, 0f)),
                new Vector3(0.26f, 0.72f, 0.58f));
            Interactable switchInteractable = AddInteractable(disconnectSwitch, true, false, false);

            // ---- SUB-STATION 2 (Z = 0.00): LOCKOUT STATION (Safety Yellow / Black LOTO) ----
            var subLock = NewChild(root, "SUB_LockoutStation", Vector3.zero);
            AddBox(subLock, "MESH_LockoutHousing", new Vector3(0.16f, 0.92f, 0.66f), new Vector3(0.01f, 0.14f, 0f), matEStopYellow, false);
            AddBox(subLock, "MESH_LockoutInnerPlate", new Vector3(0.04f, 0.76f, 0.54f), new Vector3(-0.08f, 0.14f, 0f), matDarkSlate, false);
            GameObject lockoutPoint = AddBox(subLock, "MESH_LockoutPoint", new Vector3(0.10f, 0.26f, 0.26f),
                new Vector3(-0.15f, 0.06f, 0f), matEStopYellow, false);
            AddModelChild(subLock, "MESH_LockoutButtons", SHARED_MODEL_DIR + "MESH_PULSANTI_TEAM.fbx",
                new Vector3(0.06f, 0.20f, 0.22f), new Vector3(-0.13f, 0.06f, 0f), false, true, null);
            GameObject lockoutLed = AddSphere(subLock, "MESH_LockoutLed", 0.09f, new Vector3(-0.14f, 0.38f, -0.13f), matLedOff, false);
            GameObject lockoutReadyLed = AddSphere(subLock, "MESH_LockoutReadyLed", 0.09f, new Vector3(-0.14f, 0.38f, 0.13f), matLedOff, false);
            AddPlaque(subLock, "MESH_PlaqueLockout", "MAT_Plaque_Lockout", new Vector2(0.60f, 0.30f),
                new Vector3(-0.06f, -0.46f, 0f), new Vector3(0f, 90f, 0f));

            AddWorldBoxCollider(lockoutPoint, subLock.transform.TransformPoint(new Vector3(-0.14f, 0.14f, 0f)),
                new Vector3(0.24f, 0.72f, 0.58f));
            Interactable lockoutInteractable = AddInteractable(lockoutPoint, true, false, false);

            // ---- SUB-STATION 3 (Z = +0.88): VOLTAGE TESTER (Cyan / Industrial Blue) ----
            var subTester = NewChild(root, "SUB_VoltageTester", new Vector3(0f, 0f, 0.88f));
            AddBox(subTester, "MESH_TesterHousing", new Vector3(0.16f, 0.92f, 0.66f), new Vector3(0.01f, 0.14f, 0f), matCyanAccent, false);
            AddBox(subTester, "MESH_TesterInnerPlate", new Vector3(0.04f, 0.76f, 0.54f), new Vector3(-0.08f, 0.14f, 0f), matDarkSlate, false);
            GameObject voltageTester = AddModelChild(subTester, "MESH_VoltageTester", SHARED_MODEL_DIR + "MESH_RilevatoreDiGas.fbx",
                new Vector3(0.14f, 0.38f, 0.28f), new Vector3(-0.14f, 0.06f, 0f), false, true, matCyanAccent);
            AddBox(subTester, "MESH_VoltageTesterScreen", new Vector3(0.03f, 0.14f, 0.20f),
                new Vector3(-0.14f, 0.34f, 0f), matHmiScreen, false);
            GameObject testerLedRed = AddSphere(subTester, "MESH_TesterLedRed", 0.085f, new Vector3(-0.15f, 0.48f, -0.14f), matLedOff, false);
            GameObject testerLedGreen = AddSphere(subTester, "MESH_TesterLedGreen", 0.085f, new Vector3(-0.15f, 0.48f, 0.14f), matLedOff, false);
            AddPlaque(subTester, "MESH_PlaqueTester", "MAT_Plaque_Tester", new Vector2(0.60f, 0.30f),
                new Vector3(-0.06f, -0.46f, 0f), new Vector3(0f, 90f, 0f));

            AddWorldBoxCollider(voltageTester, subTester.transform.TransformPoint(new Vector3(-0.14f, 0.14f, 0f)),
                new Vector3(0.24f, 0.72f, 0.58f));
            Interactable testerInteractable = AddInteractable(voltageTester, true, false, false);

            ParticleSystem arc = AddParticles(subTester, "VFX_ElectricalArc", new Vector3(-0.22f, 0.24f, 0f), Vector3.zero,
                matParticleSoft, 40, false, 30f, new Color(0.6f, 0.85f, 1f, 0.9f), 0.05f, 1.2f, 0.25f, 0.04f,
                ParticleSystemShapeType.Sphere, false);
            arc.gameObject.SetActive(false);

            AudioSource oneShot = AddAudioSource(root, null, false, false, 1f, 0.85f);
            var panel = root.AddComponent<MachineLockEnergyPanel>();
            SetRef(panel, "mainSwitchInteractable", switchInteractable);
            SetRef(panel, "lockoutInteractable", lockoutInteractable);
            SetRef(panel, "testerInteractable", testerInteractable);
            SetRef(panel, "switchHandle", disconnectSwitch.transform);
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

        // -- MESH_PpeCabinet (Wide shallow sliding-door industrial locker, 4 slots + 2 distractors) --

        private static void BuildPpeCabinetPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_PpeCabinet");
            // Wide, shallow industrial cabinet: 1.80 m wide x 2.05 m tall x 0.44 m deep.
            AddBox(root, "MESH_PpeCabinetBack", new Vector3(1.80f, 2.05f, 0.05f), new Vector3(0f, 1.025f, -0.20f), matDarkSlate, true);
            AddBox(root, "MESH_PpeCabinetSideLeft", new Vector3(0.05f, 2.05f, 0.44f), new Vector3(-0.875f, 1.025f, 0f), matDarkSlate, true);
            AddBox(root, "MESH_PpeCabinetSideRight", new Vector3(0.05f, 2.05f, 0.44f), new Vector3(0.875f, 1.025f, 0f), matDarkSlate, true);
            AddBox(root, "MESH_PpeCabinetTop", new Vector3(1.84f, 0.08f, 0.48f), new Vector3(0f, 2.01f, 0f), matCyanAccent, true);
            AddBox(root, "MESH_PpeCabinetBottom", new Vector3(1.80f, 0.06f, 0.44f), new Vector3(0f, 0.03f, 0f), matDarkSlate, true);
            AddBox(root, "MESH_PpeCabinetShelfLower", new Vector3(1.72f, 0.03f, 0.38f), new Vector3(0f, 0.44f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_PpeCabinetShelfUpper", new Vector3(1.72f, 0.03f, 0.38f), new Vector3(0f, 1.24f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_PpeCabinetDividerLeft", new Vector3(0.025f, 1.92f, 0.36f), new Vector3(-0.29f, 1.02f, -0.01f), matDarkSlate, false);
            AddBox(root, "MESH_PpeCabinetDividerRight", new Vector3(0.025f, 1.92f, 0.36f), new Vector3(0.29f, 1.02f, -0.01f), matDarkSlate, false);

            // Non-blocking compact sliding double doors on the +Z front face.
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

            // ---- TOP ROW (y = 1.56) ----
            // 1. Heat Gloves (Gloves, correct): thick safety-orange + aluminized cuffs
            var heatBay = CreatePpeBay(root, "MESH_HeatResistantGlove", new Vector3(-0.58f, 1.56f, 0.02f),
                "MAT_Plaque_Ppe_HeatGloves", "MAT_Plaque_Ppe_Equipped_HeatGloves", out GameObject heatVisual, out GameObject heatBadge);
            AddModelChild(heatVisual, "MESH_HeatGloveMesh", SHARED_MODEL_DIR + "GUANTO.fbx",
                new Vector3(0.28f, 0.34f, 0.14f), new Vector3(0f, 0.06f, 0f), false, true, matHeatGlove);
            AddCylinder(heatVisual, "MESH_HeatGloveCuff", 0.095f, 0.12f,
                new Vector3(0f, -0.10f, 0f), Vector3.zero, matPolishedSteel, false);

            // 2. Chemical Apron (Body, correct): heavy dark chemical rubber + high-vis yellow hazard trim
            var apronBay = CreatePpeBay(root, "MESH_ChemicalApron", new Vector3(0f, 1.56f, 0.02f),
                "MAT_Plaque_Ppe_Apron", "MAT_Plaque_Ppe_Equipped_Apron", out GameObject apronVisual, out GameObject apronBadge);
            AddBox(apronVisual, "MESH_ApronBody", new Vector3(0.38f, 0.44f, 0.05f), new Vector3(0f, -0.02f, 0f), matApronRubber, false);
            AddBox(apronVisual, "MESH_ApronTrimChest", new Vector3(0.39f, 0.055f, 0.06f), new Vector3(0f, 0.11f, 0f), matEStopYellow, false);
            AddBox(apronVisual, "MESH_ApronTrimHem", new Vector3(0.39f, 0.05f, 0.06f), new Vector3(0f, -0.19f, 0f), matEStopYellow, false);
            AddBox(apronVisual, "MESH_ApronStrapL", new Vector3(0.035f, 0.16f, 0.03f), new Vector3(-0.09f, 0.24f, 0f), matEStopYellow, false);
            AddBox(apronVisual, "MESH_ApronStrapR", new Vector3(0.035f, 0.16f, 0.03f), new Vector3(0.09f, 0.24f, 0f), matEStopYellow, false);

            // 3. Face Shield (Face, correct): bold safety-yellow browguard + transparent full visor + safety goggles
            var shieldBay = CreatePpeBay(root, "MESH_FaceShield", new Vector3(0.58f, 1.56f, 0.02f),
                "MAT_Plaque_Ppe_FaceShield", "MAT_Plaque_Ppe_Equipped_FaceShield", out GameObject shieldVisual, out GameObject shieldBadge);
            AddBox(shieldVisual, "MESH_FaceShieldBrowguard", new Vector3(0.34f, 0.08f, 0.22f), new Vector3(0f, 0.16f, 0f), matEStopYellow, false);
            AddModelChild(shieldVisual, "MESH_FaceShieldGlasses", SHARED_MODEL_DIR + "OCCHIALI_PROTETTIVI.fbx",
                new Vector3(0.24f, 0.12f, 0.16f), new Vector3(0f, 0.06f, 0.02f), false, true, null);
            AddBox(shieldVisual, "MESH_FaceShieldVisor", new Vector3(0.34f, 0.30f, 0.015f), new Vector3(0f, -0.02f, 0.11f), matFaceShieldVisor, false);

            // ---- BOTTOM ROW (y = 0.76) ----
            // 4. Safety Boots S3 (Feet, correct): pair of heavy steel-toe leather boots with thick yellow lug soles
            var bootsBay = CreatePpeBay(root, "MESH_SafetyBoots", new Vector3(-0.58f, 0.76f, 0.02f),
                "MAT_Plaque_Ppe_SafetyBoots", "MAT_Plaque_Ppe_Equipped_SafetyBoots", out GameObject bootsVisual, out GameObject bootsBadge);
            BuildSafetyBootMesh(bootsVisual, "BootLeft", new Vector3(-0.11f, -0.18f, 0f));
            BuildSafetyBootMesh(bootsVisual, "BootRight", new Vector3(0.11f, -0.18f, 0f));

            // 5. Latex Gloves (Gloves, distractor): thin pale glossy cream disposable glove
            var latexBay = CreatePpeBay(root, "MESH_LatexGlove", new Vector3(0f, 0.76f, 0.02f),
                "MAT_Plaque_Ppe_LatexGloves", "MAT_Plaque_Ppe_Equipped_LatexGloves", out GameObject latexVisual, out GameObject latexBadge);
            AddModelChild(latexVisual, "MESH_LatexGloveMesh", SHARED_MODEL_DIR + "GUANTO.fbx",
                new Vector3(0.22f, 0.26f, 0.08f), new Vector3(0f, -0.04f, 0.02f), false, true, matLatex);

            // 6. Surgical Mask (Face, distractor): clean pleated pale hospital-blue disposable mask with white ear loops
            // (replaces the 15,946-tri MASCHERINA.fbx so the full hall stays comfortably under the 100k Quest 3 budget).
            var maskBay = CreatePpeBay(root, "MESH_SurgicalMask", new Vector3(0.58f, 0.76f, 0.02f),
                "MAT_Plaque_Ppe_SurgicalMask", "MAT_Plaque_Ppe_Equipped_SurgicalMask", out GameObject maskVisual, out GameObject maskBadge);
            AddBox(maskVisual, "MESH_SurgicalMaskPleatTop", new Vector3(0.24f, 0.045f, 0.022f), new Vector3(0f, 0.02f, 0.03f), matSurgicalMaskBlue, false);
            AddBox(maskVisual, "MESH_SurgicalMaskPleatMid", new Vector3(0.24f, 0.045f, 0.028f), new Vector3(0f, -0.025f, 0.036f), matSurgicalMaskBlue, false);
            AddBox(maskVisual, "MESH_SurgicalMaskPleatBot", new Vector3(0.24f, 0.045f, 0.022f), new Vector3(0f, -0.07f, 0.03f), matSurgicalMaskBlue, false);
            AddBox(maskVisual, "MESH_SurgicalMaskNoseWire", new Vector3(0.22f, 0.012f, 0.026f), new Vector3(0f, 0.044f, 0.033f), matLatex, false);
            AddBox(maskVisual, "MESH_SurgicalMaskLoopL", new Vector3(0.012f, 0.11f, 0.08f), new Vector3(-0.125f, -0.025f, -0.01f), matLatex, false);
            AddBox(maskVisual, "MESH_SurgicalMaskLoopR", new Vector3(0.012f, 0.11f, 0.08f), new Vector3(0.125f, -0.025f, -0.01f), matLatex, false);

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
            string plaqueMaterialName, string equippedPlaqueMaterialName, out GameObject visualRoot, out GameObject equippedBadge)
        {
            var bay = NewChild(cabinetRoot, name, localPos);
            visualRoot = NewChild(bay, "Visual", Vector3.zero);

            // Flush high-contrast Diegetic Plaque mounted under the item on the shelf lip (facing +Z).
            AddPlaque(cabinetRoot, name + "_Plaque", plaqueMaterialName, new Vector2(0.50f, 0.22f),
                new Vector3(localPos.x, localPos.y - 0.34f, 0.195f), new Vector3(0f, 180f, 0f));

            // Per-item shelf equipped status badge ("<Nome DPI> INDOSSATO") revealed in the bay center when worn.
            equippedBadge = AddPlaque(bay, "EquippedBadge", equippedPlaqueMaterialName, new Vector2(0.46f, 0.22f),
                new Vector3(0f, 0.02f, 0.05f), new Vector3(0f, 180f, 0f));
            equippedBadge.SetActive(false);
            return bay;
        }

        private static void BuildSafetyBootMesh(GameObject parent, string prefix, Vector3 localBase)
        {
            AddBox(parent, prefix + "_Sole", new Vector3(0.11f, 0.045f, 0.28f), localBase + new Vector3(0f, 0.022f, 0.02f), matSafetyBootSole, false);
            AddBox(parent, prefix + "_Upper", new Vector3(0.105f, 0.10f, 0.25f), localBase + new Vector3(0f, 0.09f, 0.01f), matSafetyBootLeather, false);
            AddBox(parent, prefix + "_Shaft", new Vector3(0.105f, 0.18f, 0.12f), localBase + new Vector3(0f, 0.20f, -0.05f), matSafetyBootLeather, false);
            AddBox(parent, prefix + "_SteelToe", new Vector3(0.108f, 0.075f, 0.08f), localBase + new Vector3(0f, 0.08f, 0.11f), matPolishedSteel, false);
            AddBox(parent, prefix + "_Collar", new Vector3(0.112f, 0.035f, 0.128f), localBase + new Vector3(0f, 0.28f, -0.05f), matSafetyBootSole, false);
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

        // -- MESH_SpillKit (Shared openable industrial bin + clear Grip plaque) --

        private static void BuildSpillKitPrefab(Scene temp)
        {
            var root = NewPrefabRoot(temp, "MESH_SpillKit");
            AddBox(root, "MESH_SpillKitFloor", new Vector3(1.50f, 0.08f, 0.80f), new Vector3(0f, 0.08f, 0f), matEStopYellow, false);
            AddBox(root, "MESH_SpillKitFrame", new Vector3(1.46f, 0.34f, 0.76f), new Vector3(0f, 0.26f, 0f), matDarkSlate, false);
            AddModelChild(root, "MESH_SpillKitBin", SHARED_PREFAB_DIR + "MESH_SCATOLA_APRIBILE_ROSSA_TEAM.fbx",
                new Vector3(0.56f, 0.48f, 0.56f), new Vector3(-0.42f, 0.42f, -0.06f), true, false, null);

            AddCylinder(root, "MESH_SpillKitWheelNW", 0.07f, 0.05f, new Vector3(-0.62f, 0.07f, -0.32f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelNE", 0.07f, 0.05f, new Vector3(0.62f, 0.07f, -0.32f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelSW", 0.07f, 0.05f, new Vector3(-0.62f, 0.07f, 0.32f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelSE", 0.07f, 0.05f, new Vector3(0.62f, 0.07f, 0.32f), new Vector3(90f, 0f, 0f), matApronRubber, false);

            // Flush front Diegetic Plaque explaining Grip grab for cones & absorbents.
            AddBox(root, "MESH_SpillKitPlaqueBack", new Vector3(0.76f, 0.34f, 0.03f), new Vector3(0f, 0.25f, 0.40f), matDarkSlate, false);
            AddPlaque(root, "MESH_PlaqueSpillKit", "MAT_Plaque_SpillKit", new Vector2(0.72f, 0.30f),
                new Vector3(0f, 0.25f, 0.42f), new Vector3(0f, 180f, 0f));

            // 4 cones cleanly spaced along the left/center shelf; 2 mats + 2 socks cleanly separated on the right.
            AddNestedPrefab(root, "MESH_Cone", new Vector3(-0.56f, 0.44f, 0.16f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(-0.24f, 0.44f, 0.16f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(0.08f, 0.44f, 0.16f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(0.40f, 0.44f, 0.16f), Vector3.zero);
            AddNestedPrefab(root, "MESH_AbsorbentMat", new Vector3(0.28f, 0.45f, -0.18f), Vector3.zero);
            AddNestedPrefab(root, "MESH_AbsorbentMat", new Vector3(0.28f, 0.53f, -0.18f), new Vector3(0f, 8f, 0f));
            AddNestedPrefab(root, "MESH_AbsorbentSock", new Vector3(0.08f, 0.62f, -0.16f), new Vector3(0f, 12f, 0f));
            AddNestedPrefab(root, "MESH_AbsorbentSock", new Vector3(0.08f, 0.78f, -0.16f), new Vector3(0f, -10f, 0f));

            AddBoxCollider(root, new Vector3(0f, 0.22f, 0f), new Vector3(1.50f, 0.44f, 0.80f));
            SavePrefab(root);
        }

        // -- MESH_HydraulicPress (Upgraded multi-part press with Polyhaven green_metal_rust) --

        private static void BuildPressPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_HydraulicPress");
            // Heavy industrial bolster & base with Polyhaven CC0 green_metal_rust PBR steel + hazard skirting.
            AddBox(root, "MESH_PressBase", new Vector3(3.0f, 1.10f, 2.5f), new Vector3(0f, 0.55f, 0f), matPressSteelGreen, true);
            AddBox(root, "MESH_PressHazardSkirtN", new Vector3(3.04f, 0.14f, 0.04f), new Vector3(0f, 0.08f, 1.26f), matEStopYellow, false);
            AddBox(root, "MESH_PressHazardSkirtS", new Vector3(3.04f, 0.14f, 0.04f), new Vector3(0f, 0.08f, -1.26f), matEStopYellow, false);
            AddBox(root, "MESH_PressBolsterPlate", new Vector3(2.4f, 0.16f, 1.9f), new Vector3(0f, 1.18f, 0f), matDarkSlate, false);

            // Massive upper crown + twin hydraulic accumulator cylinders on top.
            AddBox(root, "MESH_PressCrown", new Vector3(3.0f, 1.15f, 2.5f), new Vector3(0f, 2.85f, 0f), matPressSteelGreen, true);
            AddCylinder(root, "MESH_PressHydraulicCylMain", 0.34f, 0.95f, new Vector3(0f, 3.85f, 0f), Vector3.zero, matDarkSlate, false);
            AddCylinder(root, "MESH_PressHydraulicCylAuxW", 0.18f, 0.70f, new Vector3(-0.85f, 3.75f, 0f), Vector3.zero, matPolishedSteel, false);
            AddCylinder(root, "MESH_PressHydraulicCylAuxE", 0.18f, 0.70f, new Vector3(0.85f, 3.75f, 0f), Vector3.zero, matPolishedSteel, false);

            // Four chrome tie-rod columns.
            AddCylinder(root, "MESH_PressColumnNW", 0.13f, 1.35f, new Vector3(-1.28f, 1.72f, -1.02f), Vector3.zero, matPolishedSteel, true);
            AddCylinder(root, "MESH_PressColumnNE", 0.13f, 1.35f, new Vector3(-1.28f, 1.72f, 1.02f), Vector3.zero, matPolishedSteel, true);
            AddCylinder(root, "MESH_PressColumnSW", 0.13f, 1.35f, new Vector3(1.28f, 1.72f, -1.02f), Vector3.zero, matPolishedSteel, true);
            AddCylinder(root, "MESH_PressColumnSE", 0.13f, 1.35f, new Vector3(1.28f, 1.72f, 1.02f), Vector3.zero, matPolishedSteel, true);

            // Moving hydraulic ram (preserved exact production name MESH_HydraulicPress_Ram).
            var ram = NewChild(root, "MESH_HydraulicPress_Ram", new Vector3(0f, 2.02f, 0f));
            AddBox(ram, "MESH_PressRamHead", new Vector3(1.45f, 0.42f, 1.35f), Vector3.zero, matPolishedSteel, true);
            AddBox(ram, "MESH_PressRamHazardEdge", new Vector3(1.48f, 0.08f, 1.38f), new Vector3(0f, -0.18f, 0f), matEStopYellow, false);
            AddCylinder(ram, "MESH_PressRamPiston", 0.22f, 0.75f, new Vector3(0f, 0.48f, 0f), Vector3.zero, matPolishedSteel, false);

            AddBox(root, "MESH_PressGuard", new Vector3(2.6f, 0.85f, 0.06f), new Vector3(0f, 0.78f, 1.28f), matDarkSlate, true);
            AddBox(root, "MESH_HydraulicPress_ControlPanel", new Vector3(0.32f, 0.80f, 0.62f), new Vector3(-1.62f, 1.58f, -0.95f), matDarkSlate, true);
            AddModelChild(root, "MESH_PressButtons", SHARED_MODEL_DIR + "MESH_PULSANTI_TEAM.fbx",
                new Vector3(0.06f, 0.22f, 0.24f), new Vector3(-1.79f, 1.28f, -0.95f), false, true, null);

            AddBox(root, "MESH_PressDisplayFrame", new Vector3(0.06f, 0.46f, 0.64f), new Vector3(-1.78f, 1.62f, -0.95f), matDarkSlate, false);
            GameObject display = AddBox(root, "MESH_PressDisplay", new Vector3(0.03f, 0.38f, 0.54f), new Vector3(-1.81f, 1.62f, -0.95f), matHmiScreen, false);
            NewChild(display, "VFX_DisplayFault", Vector3.zero);

            // Oversized high-contrast 120°C thermometer dial on the West face.
            AddCylinder(root, "MESH_Thermometer", 0.11f, 0.07f, new Vector3(-1.55f, 1.78f, 0.25f), new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddCylinder(root, "MESH_ThermometerFace", 0.09f, 0.025f, new Vector3(-1.60f, 1.78f, 0.25f), new Vector3(0f, 0f, 90f), matEStopRed, false);

            // Reinforced east leak manifold pipe feeding directly into the oil jet & valve pipe run.
            AddCylinder(root, "MESH_LeakPipe", 0.075f, 0.65f, new Vector3(1.65f, 0.95f, 0.55f), new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddBox(root, "MESH_LeakPipeElbow", new Vector3(0.16f, 0.16f, 0.16f), new Vector3(1.96f, 0.95f, 0.55f), matEStopRed, false);

            // Kinematic body plus trigger volume for player contact while ram moves (GDD §9.1).
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
            SetRef(press, "clackClip", clips.EStopClack);
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

        /// <summary>
        /// Adds a billboard particle system with a soft URP material. The caller decides whether it plays
        /// on awake, so the scene keeps at most four auto-playing systems (GDD §13.2).
        /// </summary>
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

        // ------------------------------------------------------------------ Collider helpers

        /// <summary>
        /// Adds a BoxCollider whose world size is independent of the local scale of the object, centred on
        /// the given world point. Used for rotated or heavily scaled primitive controls.
        /// </summary>
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

        /// <summary>
        /// Adds a BoxCollider that matches the local unit cube of a primitive, i.e. the local scale of the
        /// object becomes the collider size.
        /// </summary>
        private static BoxCollider AddLocalBoxCollider(GameObject target)
        {
            var collider = target.GetComponent<BoxCollider>();
            if (collider == null) collider = target.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            collider.isTrigger = false;
            return collider;
        }

        /// <summary>
        /// Adds a BoxCollider fitted to the renderer bounds of the object, with a small padding. Used for
        /// imported FBX props whose local scale is not a unit cube.
        /// </summary>
        private static BoxCollider FitBoxCollider(GameObject target, float padding)
        {
            var collider = target.GetComponent<BoxCollider>();
            if (collider == null) collider = target.AddComponent<BoxCollider>();
            Bounds bounds = RendererBounds(target);
            Vector3 scale = target.transform.lossyScale;
            Vector3 size = bounds.size + Vector3.one * padding;
            collider.size = new Vector3(
                SafeDivide(size.x, Mathf.Abs(scale.x)),
                SafeDivide(size.y, Mathf.Abs(scale.y)),
                SafeDivide(size.z, Mathf.Abs(scale.z)));
            collider.center = target.transform.InverseTransformPoint(bounds.center);
            collider.isTrigger = false;
            return collider;
        }

        private static float SafeDivide(float value, float divisor)
        {
            return divisor > 0.0001f ? value / divisor : value;
        }

        private static GameObject FindChild(GameObject parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
            throw new InvalidOperationException($"[MachineLock] Child not found: {parent.name}/{name}");
        }

        // ------------------------------------------------------------------ Scene helpers

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
        /// Instantiates a Shared FBX as a plain child, strips imported lights/cameras/colliders and
        /// fits it to the requested size. The mesh keeps referencing the Shared FBX: no Shared file is
        /// edited and the root name stays the production name.
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

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (Light light in instance.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
            foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera);
            foreach (AudioListener listener in instance.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(listener);

            // Imported FBX children carry importer names (Cube.081, ...): give them production names.
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

        // ------------------------------------------------------------------ Room

        private static void ConfigureRoomLighting(Scene scene)
        {
            GameObject lightRoot = FindRoot(scene, "MNG_Light");
            if (lightRoot == null)
            {
                lightRoot = new GameObject("MNG_Light");
                SceneManager.MoveGameObjectToScene(lightRoot, scene);
            }
            lightRoot.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightRoot.GetComponent<Light>();
            if (light == null) light = lightRoot.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;

            // Industrial hall ambient fill so PBR surfaces and plaques stay effortlessly readable on Quest 3.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.39f, 0.43f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;
        }

        private static void BuildRoom(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Room");

            // 1. Real shared industrial hall shell: PFB_OldIndustrialBuilding (ADR-0001).
            BuildIndustrialBuildingShell(env);

            // 2. Active stamping bay floor (18 x 11 m playable concrete floor, plus wide non-snagging collider).
            GameObject floor = AddBox(env, "MESH_Floor", new Vector3(ROOM_HALF_X * 2f, 0.1f, ROOM_HALF_Z * 2f),
                new Vector3(0f, -0.05f, 0f), matConcreteFloor, true);
            MarkStatic(floor);

            // Clean invisible locomotion boundary colliders aligned to the 18 x 11 m active bay perimeter
            // (with 2.1 m clear doorway opening on the West wall at Z in [-1.05, +1.05]).
            var bounds = NewChild(env, "COLLIDER_BayBounds", Vector3.zero);
            AddBoundaryCollider(bounds, "BOUND_North", new Vector3(0f, 2.5f, ROOM_HALF_Z + 0.15f), new Vector3(ROOM_HALF_X * 2f + 0.4f, 5f, 0.3f));
            AddBoundaryCollider(bounds, "BOUND_South", new Vector3(0f, 2.5f, -(ROOM_HALF_Z + 0.15f)), new Vector3(ROOM_HALF_X * 2f + 0.4f, 5f, 0.3f));
            AddBoundaryCollider(bounds, "BOUND_East", new Vector3(ROOM_HALF_X + 0.15f, 2.5f, 0f), new Vector3(0.3f, 5f, ROOM_HALF_Z * 2f + 0.4f));
            AddBoundaryCollider(bounds, "BOUND_WestNorth", new Vector3(-(ROOM_HALF_X + 0.15f), 2.5f, 3.3f), new Vector3(0.3f, 5f, 4.4f));
            AddBoundaryCollider(bounds, "BOUND_WestSouth", new Vector3(-(ROOM_HALF_X + 0.15f), 2.5f, -3.3f), new Vector3(0.3f, 5f, 4.4f));
            AddBoundaryCollider(bounds, "BOUND_WestBackstop", new Vector3(-(ROOM_HALF_X + 1.4f), 2.5f, 0f), new Vector3(0.3f, 5f, 3.0f));
            // Solid floor slab inside the open West doorway alcove so walking into a blocked exit never drops off MESH_Floor.
            AddBoundaryCollider(bounds, "BOUND_WestDoorwayFloor", new Vector3(-(ROOM_HALF_X + 0.75f), -0.05f, 0f), new Vector3(1.8f, 0.1f, 3.4f));

            // 3. Structural bay architectural elements (electrical backing wall, steel ceiling girders, West doorway).
            GameObject eastBackingWall = AddBox(env, "MESH_EastElectricalBayWall", new Vector3(0.24f, 3.1f, 3.2f),
                new Vector3(ROOM_HALF_X - 0.04f, 1.55f, 0f), matConcreteWallPbr, true);
            MarkStatic(eastBackingWall);

            // Overhead steel roof trusses framing the 6.2 m high stamping bay.
            var beams = NewChild(env, "MESH_CeilingBeams", Vector3.zero);
            for (int i = -2; i <= 2; i++)
            {
                GameObject girder = AddBox(beams, "MESH_RoofBeam0" + (i + 3), new Vector3(0.24f, 0.32f, ROOM_HALF_Z * 2f),
                    new Vector3(i * 4.2f, ROOM_HEIGHT, 0f), matDarkSlate, false);
                MarkStatic(girder);
            }

            // West exit doorway with overhead illuminated green exit beacon.
            var door = NewChild(env, "MESH_Door", new Vector3(-ROOM_HALF_X, 0f, 0f));
            InstantiatePrefabInScene(scene, "MESH_IndustrialDoor_RollUp", door.transform, Vector3.zero, Vector3.zero);
            var exitBeaconRoot = NewChild(door, "MESH_ExitBeaconRoot", new Vector3(0.24f, 3.12f, 0f));
            AddBox(exitBeaconRoot, "MESH_ExitBeaconFrame", new Vector3(0.18f, 0.44f, 1.44f), Vector3.zero, matDarkSlate, false);
            AddBox(exitBeaconRoot, "MESH_ExitBeacon", new Vector3(0.20f, 0.34f, 1.32f), new Vector3(0.02f, 0f, 0f), matExitBeaconIdle, false);

            // 4. Active bay boundary safety barriers & scenic industrial hall dressing (low-poly Shared FBXs).
            BuildBayBarriersAndDressing(env);

            // 5. Wall signs & proactive yellow safety floor lanes (Dry Perimeter + Hazard Boundary + station links).
            var signs = NewChild(env, "MESH_Signs", Vector3.zero);
            AddWallSign(signs, "MESH_SignStampaggio", "MAT_Sign_Stampaggio", new Vector2(3.2f, 1.2f),
                new Vector3(0f, 3.8f, ROOM_HALF_Z - 0.06f), Vector3.zero);
            AddWallSign(signs, "MESH_SignQuadroGenerale", "MAT_Sign_QuadroGenerale", new Vector2(2.4f, 0.9f),
                new Vector3(ROOM_HALF_X - 0.18f, 2.75f, 0f), new Vector3(0f, 90f, 0f));
            AddWallSign(signs, "MESH_SignDpi", "MAT_Sign_Dpi", new Vector2(1.6f, 0.7f),
                new Vector3(2.8f, 3.2f, -(ROOM_HALF_Z - 0.06f)), new Vector3(0f, 180f, 0f));
            AddWallSign(signs, "MESH_SignKitAntispandimento", "MAT_Sign_KitAntispandimento", new Vector2(2.0f, 0.8f),
                new Vector3(-5.2f, 3.1f, -(ROOM_HALF_Z - 0.06f)), new Vector3(0f, 180f, 0f));
            AddWallSign(signs, "MESH_SignUscita", "MAT_Sign_Uscita", new Vector2(1.4f, 0.55f),
                new Vector3(-(ROOM_HALF_X - 0.22f), 3.65f, 0f), new Vector3(0f, -90f, 0f));

            BuildYellowSafetyFloorLanes(signs);
            BuildCeilingFixtures(signs);
        }

        private static void BuildIndustrialBuildingShell(GameObject env)
        {
            string buildingPath = SHARED_MODEL_DIR + "PFB_OldIndustrialBuilding.fbx";
            var buildingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(buildingPath);
            if (buildingAsset == null)
                throw new InvalidOperationException($"[MachineLock] Shared industrial building not found: {buildingPath}");

            var shell = (GameObject)UnityEngine.Object.Instantiate(buildingAsset);
            shell.name = "PFB_OldIndustrialBuilding";
            shell.transform.SetParent(env.transform, false);
            // Rotate 90 deg Y so the 34.2 m hall length runs West-East (World X) and 11.4 m width runs South-North (World Z).
            // Scale local X by 1.14 so inner north/south masonry walls sit at +/-5.98 m (cleanly outside +/-5.5 m bay bounds).
            shell.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            shell.transform.localScale = new Vector3(1.14f, 1.0f, 1.04f);

            // Strip any imported colliders/lights/cameras/listeners and remove exterior rooftop above 11.4 m
            // so the Quest 3 triangle budget stays comfortably under 100,000 tris.
            foreach (Collider c in shell.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (Light l in shell.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(l);
            foreach (Camera cam in shell.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(cam);
            foreach (AudioListener al in shell.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(al);

            var toRemove = new List<GameObject>();
            foreach (Transform child in shell.GetComponentsInChildren<Transform>(true))
            {
                if (child == shell.transform) continue;
                string lower = child.name.Trim().ToLowerInvariant();
                if (lower == "building top")
                {
                    toRemove.Add(child.gameObject);
                    continue;
                }
                var mr = child.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                if (lower.Contains("window")) mr.sharedMaterial = matBuildingWindow;
                else if (lower.Contains("floor")) mr.sharedMaterial = matBuildingFloor;
                else mr.sharedMaterial = matBuildingWall;
                MarkStatic(child.gameObject);
            }
            foreach (GameObject dead in toRemove) UnityEngine.Object.DestroyImmediate(dead);

            // Align building center X/Z to (0, 0). The building's ground slab ('building floor') is 0.23 m thick
            // above b.min.y, so placing b.min.y at -0.245 m puts the slab top at y = -0.015 m (cleanly beneath
            // MESH_Floor at y = 0.00 without burying the oil puddle or yellow floor markings, while keeping the
            // second-story ceiling slab cleanly above our 6.2 m roof trusses).
            Bounds b = RendererBounds(shell);
            shell.transform.position += new Vector3(-b.center.x, -0.245f - b.min.y, -b.center.z);
            MarkStatic(shell);
        }

        private static void AddBoundaryCollider(GameObject parent, string name, Vector3 center, Vector3 size)
        {
            var go = NewChild(parent, name, center);
            AddBoxCollider(go, Vector3.zero, size);
        }

        private static void BuildBayBarriersAndDressing(GameObject env)
        {
            var dressing = NewChild(env, "ENV_IndustrialDressing", Vector3.zero);

            // Safety barriers cleanly demarcating the West & East active bay edges while keeping the hall open to view.
            Vector3[] barrierPositions =
            {
                new Vector3(-ROOM_HALF_X + 0.1f, 0f, -2.0f),
                new Vector3(-ROOM_HALF_X + 0.1f, 0f, -4.1f),
                new Vector3(-ROOM_HALF_X + 0.1f, 0f, 2.0f),
                new Vector3(-ROOM_HALF_X + 0.1f, 0f, 4.1f),
                new Vector3(ROOM_HALF_X - 0.1f, 0f, -2.6f),
                new Vector3(ROOM_HALF_X - 0.1f, 0f, -4.4f),
                new Vector3(ROOM_HALF_X - 0.1f, 0f, 2.6f),
                new Vector3(ROOM_HALF_X - 0.1f, 0f, 4.4f)
            };
            for (int i = 0; i < barrierPositions.Length; i++)
            {
                GameObject barrier = AddModelChild(dressing, "MESH_BayBarrier0" + (i + 1),
                    SHARED_MODEL_DIR + "MESH_Magazzino_Barriera.fbx",
                    new Vector3(1.65f, 1.05f, 0.14f), barrierPositions[i], true, false, null);
                barrier.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                MarkStatic(barrier);
            }

            // Scenic warehouse shelving, barrels, and pallets beyond the West/East active bay boundaries for depth.
            Vector3[] shelfPositions =
            {
                new Vector3(ROOM_HALF_X + 2.4f, 0f, -3.6f),
                new Vector3(ROOM_HALF_X + 2.4f, 0f, 3.6f),
                new Vector3(-ROOM_HALF_X - 2.6f, 0f, -3.8f),
                new Vector3(-ROOM_HALF_X - 2.6f, 0f, 3.8f)
            };
            for (int i = 0; i < shelfPositions.Length; i++)
            {
                GameObject shelf = AddModelChild(dressing, "MESH_HallShelf0" + (i + 1),
                    SHARED_MODEL_DIR + "MESH_Magazzino_Scaffale.fbx",
                    new Vector3(2.4f, 3.2f, 1.1f), shelfPositions[i], true, false, null);
                MarkStatic(shelf);
            }

            Vector3[] barrelPositions =
            {
                new Vector3(ROOM_HALF_X + 1.5f, 0f, -1.6f),
                new Vector3(ROOM_HALF_X + 1.5f, 0f, 1.8f),
                new Vector3(-ROOM_HALF_X - 1.9f, 0f, 2.4f)
            };
            for (int i = 0; i < barrelPositions.Length; i++)
            {
                GameObject barrel = AddModelChild(dressing, "MESH_HallBarrel0" + (i + 1),
                    SHARED_MODEL_DIR + "MESH_Folgorazione_Barile.fbx",
                    new Vector3(0.62f, 0.92f, 0.62f), barrelPositions[i], true, true, null);
                MarkStatic(barrel);
            }

            Vector3[] palletPositions =
            {
                new Vector3(ROOM_HALF_X + 4.4f, 0f, -1.5f),
                new Vector3(-ROOM_HALF_X - 4.2f, 0f, -2.2f)
            };
            for (int i = 0; i < palletPositions.Length; i++)
            {
                GameObject pallet = AddModelChild(dressing, "MESH_HallPallet0" + (i + 1),
                    SHARED_MODEL_DIR + "Pallet.fbx",
                    new Vector3(1.2f, 0.16f, 1.0f), palletPositions[i], true, false, null);
                MarkStatic(pallet);
            }

            // High-mounted industrial exhaust vent fan on the North hall wall.
            GameObject ventFan = AddModelChild(dressing, "MESH_HallVentFan",
                SHARED_PREFAB_DIR + "MESH_VENTOLA_TEAM.fbx",
                new Vector3(0.45f, 1.6f, 1.6f), new Vector3(-3.8f, 3.9f, ROOM_HALF_Z - 0.15f), false, true, null);
            ventFan.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            MarkStatic(ventFan);
        }

        private static void BuildYellowSafetyFloorLanes(GameObject signs)
        {
            // Inner hazard perimeter framing the 7 x 5 m oil spill & press danger zone.
            AddFloorMarking(signs, "MESH_FloorHazardNorth", new Vector2(8.4f, 0.14f), new Vector3(0f, 0.01f, 3.05f));
            AddFloorMarking(signs, "MESH_FloorHazardSouth", new Vector2(8.4f, 0.14f), new Vector3(0f, 0.01f, -3.05f));
            AddFloorMarking(signs, "MESH_FloorHazardEast", new Vector2(0.14f, 6.24f), new Vector3(4.15f, 0.01f, 0f));
            AddFloorMarking(signs, "MESH_FloorHazardWest", new Vector2(0.14f, 6.24f), new Vector3(-4.15f, 0.01f, 0f));

            // Outer Dry Perimeter safety walkway frame.
            AddFloorMarking(signs, "MESH_FloorWalkwayNorth", new Vector2(16.2f, 0.12f), new Vector3(0f, 0.01f, 4.75f));
            AddFloorMarking(signs, "MESH_FloorWalkwaySouth", new Vector2(16.2f, 0.12f), new Vector3(0f, 0.01f, -4.75f));
            AddFloorMarking(signs, "MESH_FloorWalkwayEast", new Vector2(0.12f, 9.62f), new Vector3(8.05f, 0.01f, 0f));
            AddFloorMarking(signs, "MESH_FloorWalkwayWest", new Vector2(0.12f, 9.62f), new Vector3(-8.05f, 0.01f, 0f));

            // Station wayfinding connector lanes (Spawn -> E-Stop, Electrical bay apron, Exit corridor).
            AddFloorMarking(signs, "MESH_FloorLaneEntry", new Vector2(2.6f, 0.12f), new Vector3(-7.0f, 0.01f, 0f));
            AddFloorMarking(signs, "MESH_FloorLaneElectrical", new Vector2(0.12f, 2.8f), new Vector3(7.55f, 0.01f, 0f));
            AddFloorMarking(signs, "MESH_FloorLaneAlarm", new Vector2(0.12f, 2.2f), new Vector3(-7.6f, 0.01f, 1.3f));
        }

        private static void AddWallSign(GameObject parent, string name, string materialName, Vector2 size, Vector3 position, Vector3 euler)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_DIR + "/" + materialName + ".mat");
            AddQuad(parent, name, size, position, euler, material);
        }

        private static void AddFloorMarking(GameObject parent, string name, Vector2 size, Vector3 position)
        {
            AddQuad(parent, name, size, position, new Vector3(90f, 0f, 0f), matFloorMarking);
        }

        private static void BuildCeilingFixtures(GameObject parent)
        {
            Vector3[] positions =
            {
                new Vector3(-5.0f, 5.45f, -2.0f),
                new Vector3(-5.0f, 5.45f, 2.0f),
                new Vector3(5.0f, 5.45f, -2.0f),
                new Vector3(5.0f, 5.45f, 2.0f)
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
                MarkStatic(model);
                AddQuad(fixture, "MESH_CeilingLight0" + (i + 1) + "Panel", new Vector2(1.1f, 0.32f),
                    new Vector3(0f, 0.06f, 0f), new Vector3(-90f, 0f, 0f), matLightFixture);
            }
        }

        // ------------------------------------------------------------------ Press area

        private static void BuildPressArea(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Press");
            InstantiatePrefabInScene(scene, "MESH_HydraulicPress", env.transform, Vector3.zero, Vector3.zero);

            // High-contrast steel oil pipe leading directly from the oil jet out to the bright red Valve
            // on the east edge of the spill (cleanly reachable from the Dry Perimeter without touching oil).
            AddCylinder(env, "MESH_PipeStraight01", 0.085f, 2.75f, new Vector3(3.38f, 0.95f, 0.55f), new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddBox(env, "MESH_PipeSupport01", new Vector3(0.10f, 0.92f, 0.10f), new Vector3(3.95f, 0.46f, 0.55f), matDarkSlate, false);
            AddBox(env, "MESH_PipeSupport02", new Vector3(0.10f, 0.92f, 0.10f), new Vector3(4.75f, 0.46f, 0.55f), matDarkSlate, false);

            InstantiatePrefabInScene(scene, "MESH_Valve", env.transform, new Vector3(4.75f, 0.95f, 0.55f), Vector3.zero);
        }

        // ------------------------------------------------------------------ Safety area

        private static void BuildSafetyArea(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Safety");
            // E-Stop pedestal faces West (-90° Y) directly toward the player at Spawn (-7.6, 0, 0).
            InstantiatePrefabInScene(scene, "MESH_EStopPedestal", env.transform, new Vector3(-5.6f, 0f, 0f), new Vector3(0f, -90f, 0f));
            // Department alarm mounted on a visible industrial post on the West-North side facing East.
            AddBox(env, "MESH_AlarmPost", new Vector3(0.16f, 2.2f, 0.16f), new Vector3(-8.65f, 1.1f, 2.4f), matDarkSlate, true);
            InstantiatePrefabInScene(scene, "MESH_AlarmButton", env.transform, new Vector3(-8.50f, 1.45f, 2.4f), new Vector3(0f, 90f, 0f));
            // 3 separated color-coded Electrical Sub-Stations centered on the East wall bay.
            InstantiatePrefabInScene(scene, "MESH_ElectricalPanel", env.transform, new Vector3(8.72f, 1.28f, 0f), Vector3.zero);
            // Default Pattern A positions for movable props (moved cleanly by ApplyLayoutPattern).
            InstantiatePrefabInScene(scene, "MESH_PpeCabinet", env.transform, new Vector3(2.8f, 0f, -5.12f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_SpillKit", env.transform, new Vector3(-5.2f, 0f, -4.35f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_WaterExtinguisher", env.transform, new Vector3(4.2f, 1.25f, 5.22f), new Vector3(0f, 180f, 0f));
        }

        // ------------------------------------------------------------------ Hazard area

        private static void BuildHazardArea(Scene scene, MachineLockAudioClips clips)
        {
            GameObject hazard = NewRoot(scene, "HAZARD_Oil");

            // Leak: dark oil jet directly under the press east manifold pipe (stopped when Valve closes).
            var jet = NewChild(hazard, "VFX_OilJet", new Vector3(2.05f, 0.95f, 0.55f));
            AddCylinder(jet, "VFX_OilJetStream", 0.022f, 0.95f, new Vector3(0f, -0.475f, 0f), Vector3.zero, matOil, false);
            AddParticles(jet, "VFX_OilParticles", new Vector3(0f, -0.1f, 0f), Vector3.zero,
                matParticleSoft, 80, true, 35f, new Color(0.09f, 0.06f, 0.03f, 0.9f), 0.05f, 0.4f, 1f, 0.03f,
                ParticleSystemShapeType.Cone);
            AddAudioSource(jet, clips.OilLeak, true, true, 1f, 0.7f);

            var puddle = NewChild(hazard, "VFX_OilPuddle", Vector3.zero);
            AddPuddleQuadrant(puddle, "MESH_PuddleQuadrantNE", 1f, 1f);
            AddPuddleQuadrant(puddle, "MESH_PuddleQuadrantNW", -1f, 1f);
            AddPuddleQuadrant(puddle, "MESH_PuddleQuadrantSE", 1f, -1f);
            AddPuddleQuadrant(puddle, "MESH_PuddleQuadrantSW", -1f, -1f);

            // Steam over the hot oil and inaction smoke above the press (GDD §9.12 and §13.2).
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

            // Three spacious puddle footprints leaving >= 2.9 m Dry Perimeter on ALL four sides of the 18 x 11 m bay.
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

        private static void AddPuddleQuadrant(GameObject puddle, string name, float signX, float signZ)
        {
            var quadrant = NewChild(puddle, name, Vector3.zero);
            AddBox(quadrant, name + "Outer", new Vector3(3.5f, 0.02f, 1.25f),
                new Vector3(1.75f * signX, 0.01f, 1.875f * signZ), matOil, false);
            AddBox(quadrant, name + "Inner", new Vector3(2.0f, 0.02f, 1.25f),
                new Vector3(2.5f * signX, 0.01f, 0.625f * signZ), matOil, false);
        }

        /// <summary>
        /// Proactive Guided baseline floor marker: subtle glowing floor ring for cone corners or quadrant
        /// outline frame for absorbent zones. Shown once full PPE is worn and hidden individually on Occupy().
        /// </summary>
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

        // ------------------------------------------------------------------ Delimitation and patterns

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

            // Three spacious Layout Patterns re-authored for the 18 x 11 m stamping bay (ADR-0001).
            // Every prop faces cleanly inward toward the Dry Perimeter with zero clipping.
            var a = NewChild(patterns, "PATTERN_A", Vector3.zero);
            NewMarker(a, "MarkerPpeCabinet", new Vector3(2.8f, 0f, -5.12f), Vector3.zero);
            NewMarker(a, "MarkerSpillKit", new Vector3(-5.2f, 0f, -4.35f), Vector3.zero);
            NewMarker(a, "MarkerExtinguisher", new Vector3(4.2f, 1.25f, 5.22f), new Vector3(0f, 180f, 0f));

            var b = NewChild(patterns, "PATTERN_B", Vector3.zero);
            NewMarker(b, "MarkerPpeCabinet", new Vector3(5.8f, 0f, -5.12f), Vector3.zero);
            NewMarker(b, "MarkerSpillKit", new Vector3(-5.2f, 0f, 4.35f), new Vector3(0f, 180f, 0f));
            NewMarker(b, "MarkerExtinguisher", new Vector3(0.0f, 1.25f, 5.22f), new Vector3(0f, 180f, 0f));

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

        private static void BuildExit(Scene scene)
        {
            var exit = NewRoot(scene, "ZONE_Exit");
            exit.transform.position = new Vector3(-ROOM_HALF_X, 0f, 0f);
            var trigger = exit.AddComponent<ScenarioTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("radius").floatValue = 1.15f;
            so.FindProperty("once").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);
            BindExitTrigger(scene);
        }

        /// <summary>
        /// Binds <see cref="MachineLockScenario.NotifyExitReached"/> to the persistent "onEntered" event of
        /// the exit trigger. The field is private to SI81.UI, so it is bound through reflection and the
        /// UnityEvent tool; the trigger component itself is not modified.
        /// </summary>
        private static void BindExitTrigger(Scene scene)
        {
            GameObject exit = FindRoot(scene, "ZONE_Exit");
            if (exit == null) throw new InvalidOperationException("[MachineLock] ZONE_Exit is missing.");
            var trigger = exit.GetComponent<ScenarioTrigger>();
            var scenario = FindRoot(scene, "MNG_Scenario").GetComponent<MachineLockScenario>();
            if (trigger == null || scenario == null)
                throw new InvalidOperationException("[MachineLock] Cannot bind the exit trigger.");

            FieldInfo field = typeof(ScenarioTrigger).GetField("onEntered", BindingFlags.Instance | BindingFlags.NonPublic);
            var onEntered = field != null ? field.GetValue(trigger) as UnityEvent : null;
            if (onEntered == null)
                throw new InvalidOperationException("[MachineLock] ScenarioTrigger.onEntered not found: the exit cannot be bound.");

            UnityEventTools.AddVoidPersistentListener(onEntered, scenario.NotifyExitReached);
            EditorUtility.SetDirty(trigger);
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

        /// <summary>
        /// Adds the damage feedback to MNG_Scenario: a global URP Volume with the red Vignette profile, the
        /// non-shadow-casting fallback light (disabled at rest) and the shared one-shot audio source.
        /// </summary>
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
            SetRef(feedback, "shockClip", clips.Shock);
            SetRef(feedback, "burnClip", clips.Burn);
            SetRef(feedback, "steamClip", clips.SteamBurst);
            var fbSo = new SerializedObject(feedback);
            fbSo.FindProperty("fadeInSeconds").floatValue = 0.15f;
            fbSo.FindProperty("holdSeconds").floatValue = 0.5f;
            fbSo.FindProperty("fadeOutSeconds").floatValue = 1.5f;
            fbSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);
        }

        // ------------------------------------------------------------------ Scene wiring (P3–P8)

        /// <summary>
        /// Cross-wires every runtime component of the built scene: the scenario, the press, the panel, the
        /// alarm, the cabinet, the valve, the extinguisher, the spill system with its eight snap zones and
        /// the six observation targets. Idempotent: it only assigns serialized references.
        /// </summary>
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

            // Press: scenario plus the two pedestal buttons.
            SetRef(press, "scenario", scenario);
            SetRef(press, "estopInteractable", FindInScene(scene, "MESH_EmergencyStopButton").GetComponent<Interactable>());
            SetRef(press, "testStartInteractable", FindInScene(scene, "MESH_TestStartButton").GetComponent<Interactable>());

            // Panel: scenario, config and the press it powers.
            SetRef(panel, "scenario", scenario);
            SetRef(panel, "config", config);
            SetRef(panel, "press", press);

            // Alarm, cabinet, valve and extinguisher.
            SetRef(alarm, "scenario", scenario);
            SetRef(alarm, "alarmInteractable", FindInScene(scene, "MESH_AlarmPushButton").GetComponent<Interactable>());
            SetRef(cabinet, "scenario", scenario);
            SetRef(valve, "scenario", scenario);
            SetRef(valve, "spillSystem", spill);

            GameObject steamObject = FindInScene(scene, "VFX_Steam");
            GameObject smokeObject = FindInScene(scene, "VFX_Smoke");
            SetRef(extinguisher, "scenario", scenario);
            SetRef(extinguisher, "steamBurstVfx", steamObject.GetComponent<ParticleSystem>());
            SetRef(extinguisher, "smokeBurstVfx", smokeObject.GetComponent<ParticleSystem>());

            // Spill system: hazards, leak, puddle meshes, trigger and the eight snap zones.
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

            // Spill kit items: they snap through the shared spill system.
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

            // Scenario: every reference used by the orchestrator.
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

        // ------------------------------------------------------------------ Labels (GDD §8.4, ADR-0002)

        private static void PlaceLabels(Scene scene)
        {
            // Strict <= 3 Risk labels pulled cleanly into open space (clear of walls & machinery).
            AddLabel(Find("MESH_HydraulicPress"), LabelKind.Risk, "Pressa in moto", "Non avvicinarti", new Vector3(-1.85f, 2.60f, 0f));
            AddLabel(Find("MESH_Thermometer"), LabelKind.Risk, "Olio a 120 °C", "Superficie bollente", new Vector3(-0.55f, 0.35f, 0f));
            AddLabel(Find("VFX_OilPuddle"), LabelKind.Risk, "Pozza d'olio bollente", "Non calpestare", new Vector3(0f, 0.55f, 2.35f));

            // Single clean electrical bay anchor (replaces 3 overlapping electrical anchors; detailed per-station
            // guidance lives on flush Diegetic Plaques + hover Action Feedback, ADR-0002).
            AddLabel(Find("MESH_ElectricalPanel"), LabelKind.ContextHelp, "Quadro elettrico", "Sezionatore · Lockout · Tester", new Vector3(-0.75f, 1.15f, 0f));

            // Sparse, non-overlapping open-space ContextHelp anchors.
            AddLabel(Find("MESH_EStopPedestal"), LabelKind.ContextHelp, "Fungo di emergenza", "Premi per fermare la pressa", new Vector3(0f, 1.72f, 0f));
            AddLabel(Find("MESH_AlarmButton"), LabelKind.ContextHelp, "Allarme reparto", "Premi per chiamare i soccorsi", new Vector3(0f, 0.72f, 0.45f));
            AddLabel(Find("MESH_PpeCabinet"), LabelKind.ContextHelp, "Armadio DPI", "Apri e indossa i 4 DPI", new Vector3(0f, 2.38f, 0.45f));
            AddLabel(Find("MESH_SpillKit"), LabelKind.ContextHelp, "Kit antispandimento", "Coni e assorbenti (GRIP)", new Vector3(0f, 1.15f, 0f));
            AddLabel(Find("MESH_Valve"), LabelKind.ContextHelp, "Valvola olio", "Chiudi per fermare la perdita", new Vector3(0f, 1.05f, 0f));
            AddLabel(Find("MESH_WaterExtinguisher"), LabelKind.ContextHelp, "Estintore ad acqua", "Non usare su olio o quadro", new Vector3(0f, 0.95f, 0.40f));

            AddLabel(Find("ZONE_Exit"), LabelKind.Interaction, "Uscita", string.Empty, new Vector3(0.65f, 1.85f, 0f));
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

        /// <summary>
        /// Removes every blockout root built by <see cref="BuildFullScene"/> so the content can be
        /// rebuilt in place without duplicating objects. The scenario, spawn and light roots stay.
        /// </summary>
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
