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
        private const string SHARED_PROP_DIR = "Assets/_Project/Shared/Oggetti/";
        private const string SHARED_ROOM_DIR = "Assets/_Project/Shared/Level/Prefabs/Chemical_Alert_Room/";

        private static readonly Vector3 SPAWN_POSITION = new Vector3(-5.2f, 0f, 0f);
        private static readonly Vector3 SPAWN_EULER = new Vector3(0f, 90f, 0f);

        // Room bounds used by the layout and by the validator (GDD §7.1).
        private const float ROOM_HALF_X = 6f;
        private const float ROOM_HALF_Z = 4f;
        private const float ROOM_HEIGHT = 4f;

        // ------------------------------------------------------------------ Materials cache

        private static Material matConcreteFloor;
        private static Material matWallConcrete;
        private static Material matCeilingConcrete;
        private static Material matPaintedSteel;
        private static Material matPolishedSteel;
        private static Material matOil;
        private static Material matEStopRed;
        private static Material matEStopYellow;
        private static Material matHmiScreen;
        private static Material matFaceShieldVisor;
        private static Material matApronRubber;
        private static Material matHeatGlove;
        private static Material matLatex;
        private static Material matRespirator;
        private static Material matFloorMarking;
        private static Material matBeaconRed;
        private static Material matLedRed;
        private static Material matLedGreen;
        private static Material matGhostSnap;
        private static Material matLightFixture;
        private static Material matHmiOff;
        private static Material matLedOff;
        private static Material matParticleSoft;

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
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "concrete_floor_worn_001_diff_4k.jpg",
                "TEX_ConcreteFloorWorn_Albedo.jpg",
                TextureImporterType.Default, false);
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "concrete_floor_worn_001_nor_gl_4k.exr",
                "TEX_ConcreteFloorWorn_Normal.exr",
                TextureImporterType.NormalMap, false);
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "concrete_floor_worn_001_rough_4k.jpg",
                "TEX_ConcreteFloorWorn_Rough.jpg",
                TextureImporterType.Default, true);
        }

        private static void CopyConcreteTexture(string source, string destinationName, TextureImporterType type, bool linear)
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
                    // The existing group copy is identical and stays valid.
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
            android.maxTextureSize = 1024;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(android);

            importer.SaveAndReimport();
            Debug.Log($"[MachineLock] Configured texture {destination} ({type}, 2048 default / 1024 ASTC 6x6 Android).");
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
            matConcreteFloor.SetTextureScale("_BaseMap", new Vector2(6f, 4f));
            matConcreteFloor.SetTextureScale("_BumpMap", new Vector2(6f, 4f));
            matConcreteFloor.SetTextureScale("_MetallicGlossMap", new Vector2(6f, 4f));
            matConcreteFloor.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(matConcreteFloor);

            matPaintedSteel = CopyMaterial(SHARED_MATERIAL_DIR + "MAT_OfficePropWhiteMetallic.mat", "MAT_PaintedSteel");
            matPolishedSteel = CopyMaterial(SHARED_MATERIAL_DIR + "MAT_OfficePropPolishedMetal.mat", "MAT_PolishedSteel");

            matWallConcrete = LitMaterial("MAT_WallConcrete", new Color(0.42f, 0.43f, 0.41f), 0f, 0.12f);
            matCeilingConcrete = LitMaterial("MAT_CeilingConcrete", new Color(0.30f, 0.31f, 0.30f), 0f, 0.08f);
            matOil = LitMaterial("MAT_Oil", new Color(0.05f, 0.03f, 0.02f), 0f, 0.9f);
            matEStopRed = LitMaterial("MAT_EmergencyStop_Red", new Color(0.72f, 0.04f, 0.03f), 0f, 0.55f);
            matEStopYellow = LitMaterial("MAT_EmergencyStop_Yellow", new Color(0.95f, 0.72f, 0.05f), 0f, 0.45f);
            matHmiScreen = LitMaterial("MAT_HMI_Screen", new Color(0.02f, 0.02f, 0.02f), 0f, 0.6f,
                new Color(0.95f, 0.14f, 0.04f) * 1.6f);
            matApronRubber = LitMaterial("MAT_Apron_Rubber", new Color(0.10f, 0.13f, 0.11f), 0f, 0.35f);
            matHeatGlove = LitMaterial("MAT_HeatGlove", new Color(0.62f, 0.52f, 0.36f), 0.05f, 0.3f);
            matLatex = LitMaterial("MAT_Latex", new Color(0.93f, 0.93f, 0.88f), 0f, 0.85f);
            matRespirator = LitMaterial("MAT_Respirator", new Color(0.88f, 0.88f, 0.85f), 0f, 0.55f);
            matFloorMarking = LitMaterial("MAT_FloorSafetyMarking_Yellow", new Color(0.95f, 0.72f, 0.05f), 0f, 0.3f,
                new Color(0.35f, 0.26f, 0.02f));
            matBeaconRed = LitMaterial("MAT_BeaconRed", new Color(0.55f, 0.02f, 0.02f), 0f, 0.75f,
                new Color(0.9f, 0.05f, 0.03f) * 1.4f);
            matLedRed = LitMaterial("MAT_LedRed", new Color(0.45f, 0.02f, 0.02f), 0f, 0.7f,
                new Color(1f, 0.05f, 0.03f) * 2f);
            matLedGreen = LitMaterial("MAT_LedGreen", new Color(0.02f, 0.35f, 0.08f), 0f, 0.7f,
                new Color(0.05f, 1f, 0.2f) * 2f);
            matLightFixture = LitMaterial("MAT_LightFixture", new Color(0.85f, 0.87f, 0.85f), 0f, 0.4f,
                new Color(1f, 0.97f, 0.9f) * 2.2f);
            matHmiOff = LitMaterial("MAT_HMI_Off", new Color(0.015f, 0.015f, 0.018f), 0f, 0.12f);
            matLedOff = LitMaterial("MAT_LedOff", new Color(0.07f, 0.07f, 0.075f), 0f, 0.35f);
            matParticleSoft = ParticleMaterial("MAT_ParticleSoft", new Color(0.66f, 0.66f, 0.64f, 0.5f));

            matFaceShieldVisor = LitMaterial("MAT_FaceShield_Visor", new Color(0.55f, 0.78f, 0.9f, 0.18f), 0f, 0.95f);
            ConfigureTransparent(matFaceShieldVisor, 0.18f);
            matGhostSnap = LitMaterial("MAT_GhostSnap", new Color(0.2f, 0.95f, 0.45f, 0.35f), 0f, 0.5f);
            ConfigureTransparent(matGhostSnap, 0.35f, new Color(0.1f, 0.9f, 0.3f));

            EnsureSignMaterial("MAT_Sign_Stampaggio", "TEX_Sign_Stampaggio.png");
            EnsureSignMaterial("MAT_Sign_QuadroGenerale", "TEX_Sign_QuadroGenerale.png");
            EnsureSignMaterial("MAT_Sign_Dpi", "TEX_Sign_Dpi.png");
            EnsureSignMaterial("MAT_Sign_KitAntispandimento", "TEX_Sign_KitAntispandimento.png");
            EnsureSignMaterial("MAT_Sign_Uscita", "TEX_Sign_Uscita.png");

            AssetDatabase.SaveAssets();
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

        private static void EnsureSignMaterial(string materialName, string textureFile)
        {
            Texture2D texture = LoadGroupTexture(textureFile);
            var material = LitMaterial(materialName, Color.white, 0f, 0.25f, Color.white);
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
                new Vector3(1.0f, 0.02f, 0.8f), new Vector3(0f, 0.01f, 0f), false, false, null);
            AddBoxCollider(root, new Vector3(0f, 0.01f, 0f), new Vector3(1.0f, 0.02f, 0.8f));

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
            var body = AddCylinder(root, "MESH_AbsorbentSockBody", 0.075f, 1.2f,
                new Vector3(0f, 0.075f, 0f), new Vector3(0f, 0f, 90f), matLatex, false);
            body.name = "MESH_AbsorbentSockBody";
            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 0;
            collider.height = 1.2f;
            collider.radius = 0.075f;
            collider.center = new Vector3(0f, 0.075f, 0f);

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
            AddCylinder(root, "MESH_ValvePipe", 0.09f, 0.5f, Vector3.zero, new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            GameObject wheel = AddCylinder(root, "MESH_Valve_Wheel", 0.15f, 0.05f, new Vector3(0f, 0.16f, 0f), Vector3.zero, matEStopRed, false);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 60f;
                var spoke = AddBox(root, "MESH_ValveSpoke" + (i + 1), new Vector3(0.27f, 0.02f, 0.02f),
                    new Vector3(0f, 0.16f, 0f), matPolishedSteel, false);
                spoke.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
            AddBoxCollider(root, new Vector3(0f, 0.08f, 0f), new Vector3(0.5f, 0.4f, 0.4f));

            Interactable interactable = AddInteractable(root, true, false, false);
            var valve = root.AddComponent<MachineLockValve>();
            AudioSource squeakAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
            SetRef(valve, "interactable", interactable);
            SetRef(valve, "wheelTransform", wheel.transform);
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
                new Vector3(0.15f, 0.6f, 0.15f), Vector3.zero, false, false, null);
            AddBox(root, "MESH_ExtinguisherBracket", new Vector3(0.2f, 0.42f, 0.03f),
                new Vector3(0f, 0.12f, 0.1f), matPaintedSteel, false);
            AddBox(root, "MESH_ExtinguisherClampTop", new Vector3(0.18f, 0.03f, 0.08f),
                new Vector3(0f, 0.28f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_ExtinguisherClampBottom", new Vector3(0.18f, 0.03f, 0.08f),
                new Vector3(0f, -0.15f, 0f), matPaintedSteel, false);
            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.height = 0.6f;
            collider.radius = 0.08f;
            collider.center = Vector3.zero;

            Interactable interactable = AddInteractable(root, true, false, false);
            var extinguisher = root.AddComponent<MachineLockExtinguisher>();
            ParticleSystem spray = AddParticles(root, "VFX_WaterSpray", new Vector3(0.16f, 0.55f, 0f), Vector3.zero,
                matParticleSoft, 100, false, 40f, new Color(0.72f, 0.8f, 0.9f, 0.55f), 0.12f, 2.5f, 1.2f, 0.05f,
                ParticleSystemShapeType.Cone, false);
            AudioSource sprayAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
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
            AddBox(root, "MESH_DoorPostLeft", new Vector3(0.12f, 2.3f, 0.12f), new Vector3(0f, 1.15f, -0.62f), matPaintedSteel, true);
            AddBox(root, "MESH_DoorPostRight", new Vector3(0.12f, 2.3f, 0.12f), new Vector3(0f, 1.15f, 0.62f), matPaintedSteel, true);
            AddBox(root, "MESH_DoorHeader", new Vector3(1.5f, 0.18f, 0.16f), new Vector3(0f, 2.35f, 0f), matPaintedSteel, false);
            AddModelChild(root, "MESH_DoorRollHousing", SHARED_ROOM_DIR + "MESH_PORTA_CAMION_TEAM.fbx",
                new Vector3(1.45f, 0.34f, 0.22f), new Vector3(0f, 2.42f, 0f), false, false, null);
            SavePrefab(root);
        }

        // -- MESH_EStopPedestal -----------------------------------------------

        private static void BuildEStopPrefab(Scene temp)
        {
            var root = NewPrefabRoot(temp, "MESH_EStopPedestal");
            AddBox(root, "MESH_EStopPedestalBase", new Vector3(0.4f, 1.1f, 0.4f), new Vector3(0f, 0.55f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_EStopBackplate", new Vector3(0.44f, 0.5f, 0.06f), new Vector3(0f, 1.0f, -0.2f), matEStopYellow, false);
            GameObject stopButton = AddCylinder(root, "MESH_EmergencyStopButton", 0.06f, 0.07f,
                new Vector3(0f, 0.95f, 0.24f), new Vector3(90f, 0f, 0f), matEStopRed, false);
            AddCylinder(root, "MESH_EmergencyStopStem", 0.03f, 0.06f, new Vector3(0f, 0.95f, 0.19f), new Vector3(90f, 0f, 0f), matEStopYellow, false);
            GameObject startButton = AddCylinder(root, "MESH_TestStartButton", 0.04f, 0.05f,
                new Vector3(0f, 0.55f, 0.22f), new Vector3(90f, 0f, 0f), matLedGreen, false);

            // The two buttons need their own collider, proud of the pedestal, so the VR ray hits them
            // directly instead of the pedestal body.
            AddWorldBoxCollider(stopButton, stopButton.transform.position, new Vector3(0.16f, 0.16f, 0.16f));
            AddInteractable(stopButton, true, false, false);
            AddWorldBoxCollider(startButton, startButton.transform.position, new Vector3(0.12f, 0.12f, 0.12f));
            AddInteractable(startButton, true, false, false);
            SavePrefab(root);
        }

        // -- MESH_AlarmButton -------------------------------------------------

        private static void BuildAlarmPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_AlarmButton");
            AddBox(root, "MESH_AlarmBox", new Vector3(0.36f, 0.46f, 0.14f), Vector3.zero, matPaintedSteel, true);
            GameObject pushButton = AddCylinder(root, "MESH_AlarmPushButton", 0.06f, 0.05f,
                new Vector3(0f, 0.05f, 0.1f), new Vector3(90f, 0f, 0f), matEStopRed, false);
            AddWorldBoxCollider(pushButton, pushButton.transform.position, new Vector3(0.16f, 0.16f, 0.16f));
            Interactable buttonInteractable = AddInteractable(pushButton, true, false, false);

            var beacon = NewChild(root, "VFX_AlarmBeacon", new Vector3(0f, 0.34f, 0f));
            GameObject dome = AddSphere(beacon, "MESH_AlarmBeaconDome", 0.12f, Vector3.zero, matBeaconRed, false);
            var light = beacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.1f, 0.05f);
            light.intensity = 2f;
            light.range = 8f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.enabled = false;

            AudioSource sirenAudio = AddAudioSource(root, clips.AlarmSiren, true, false, 1f, 0.8f);
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

        // -- MESH_ElectricalPanel ---------------------------------------------

        private static void BuildElectricalPanelPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_ElectricalPanel");
            AddModelChild(root, "MESH_PanelCabinet", SHARED_PROP_DIR + "Quadro Elettrico/Quadro Elettrico.fbx",
                new Vector3(0.25f, 1.2f, 1.0f), Vector3.zero, false, false, null);
            GameObject disconnectSwitch = AddModelChild(root, "MESH_DisconnectSwitch", SHARED_PROP_DIR + "Manopola.fbx",
                new Vector3(0.22f, 0.22f, 0.22f), new Vector3(-0.16f, 0.3f, -0.3f), false, true, matEStopRed);

            AddBox(root, "MESH_LockoutPoint", new Vector3(0.03f, 0.18f, 0.18f), new Vector3(-0.15f, 0.05f, 0.3f), matEStopYellow, false);
            AddBox(root, "MESH_LockoutLed", new Vector3(0.02f, 0.06f, 0.06f), new Vector3(-0.17f, 0.05f, 0.3f), matLedRed, false);

            GameObject voltageTester = AddBox(root, "MESH_VoltageTester", new Vector3(0.09f, 0.32f, 0.22f),
                new Vector3(-0.16f, 0.3f, 0.25f), matPaintedSteel, false);
            AddBox(root, "MESH_VoltageTesterScreen", new Vector3(0.02f, 0.1f, 0.11f), new Vector3(-0.21f, 0.37f, 0.25f), matHmiScreen, false);
            GameObject testerLedRed = AddBox(root, "MESH_TesterLedRed", new Vector3(0.02f, 0.035f, 0.035f),
                new Vector3(-0.21f, 0.24f, 0.3f), matLedRed, false);
            GameObject testerLedGreen = AddBox(root, "MESH_TesterLedGreen", new Vector3(0.02f, 0.035f, 0.035f),
                new Vector3(-0.21f, 0.24f, 0.2f), matLedGreen, false);

            // The body collider sits behind the front face: the switch, the lockout point and the tester
            // get their own collider so the VR ray hits each control directly.
            AddBoxCollider(root, new Vector3(0.02f, 0f, 0f), new Vector3(0.24f, 1.2f, 1.0f));
            FitBoxCollider(disconnectSwitch, 0.02f);
            Interactable switchInteractable = AddInteractable(disconnectSwitch, true, false, false);

            GameObject lockoutPoint = FindChild(root, "MESH_LockoutPoint");
            AddWorldBoxCollider(lockoutPoint, lockoutPoint.transform.position, new Vector3(0.09f, 0.24f, 0.24f));
            Interactable lockoutInteractable = AddInteractable(lockoutPoint, true, false, false);

            AddWorldBoxCollider(voltageTester, voltageTester.transform.position, new Vector3(0.12f, 0.34f, 0.24f));
            Interactable testerInteractable = AddInteractable(voltageTester, true, false, false);

            ParticleSystem arc = AddParticles(root, "VFX_ElectricalArc", new Vector3(-0.22f, 0.3f, 0.0f), Vector3.zero,
                matParticleSoft, 40, false, 30f, new Color(0.6f, 0.85f, 1f, 0.9f), 0.05f, 1.2f, 0.25f, 0.04f,
                ParticleSystemShapeType.Sphere, false);
            arc.gameObject.SetActive(false);

            AudioSource oneShot = AddAudioSource(root, null, false, false, 1f, 0.8f);
            var panel = root.AddComponent<MachineLockEnergyPanel>();
            SetRef(panel, "mainSwitchInteractable", switchInteractable);
            SetRef(panel, "lockoutInteractable", lockoutInteractable);
            SetRef(panel, "testerInteractable", testerInteractable);
            SetRef(panel, "switchHandle", disconnectSwitch.transform);
            SetRef(panel, "lockoutLedRenderer", FindChild(root, "MESH_LockoutLed").GetComponent<Renderer>());
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

        // -- MESH_PpeCabinet ---------------------------------------------------

        private static void BuildPpeCabinetPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_PpeCabinet");
            AddBox(root, "MESH_PpeCabinetBack", new Vector3(1.2f, 2.0f, 0.06f), new Vector3(0f, 1.0f, -0.27f), matPaintedSteel, true);
            AddBox(root, "MESH_PpeCabinetSideLeft", new Vector3(0.06f, 2.0f, 0.6f), new Vector3(-0.57f, 1.0f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_PpeCabinetSideRight", new Vector3(0.06f, 2.0f, 0.6f), new Vector3(0.57f, 1.0f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_PpeCabinetTop", new Vector3(1.2f, 0.06f, 0.6f), new Vector3(0f, 1.97f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_PpeCabinetBottom", new Vector3(1.2f, 0.06f, 0.6f), new Vector3(0f, 0.03f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_PpeCabinetShelf1", new Vector3(1.08f, 0.03f, 0.5f), new Vector3(0f, 0.5f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_PpeCabinetShelf2", new Vector3(1.08f, 0.03f, 0.5f), new Vector3(0f, 1.0f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_PpeCabinetShelf3", new Vector3(1.08f, 0.03f, 0.5f), new Vector3(0f, 1.5f, 0f), matPaintedSteel, false);

            // The door starts CLOSED (GDD §9.6): identity rotation, hinge on the left edge.
            var door = NewChild(root, "MESH_PpeCabinet_Door", new Vector3(-0.6f, 1.0f, 0.3f));
            door.transform.localRotation = Quaternion.identity;
            GameObject doorPanel = AddBox(door, "MESH_PpeCabinet_DoorPanel", new Vector3(1.2f, 1.8f, 0.04f),
                new Vector3(0.6f, 0f, 0f), matPaintedSteel, false);
            AddBox(door, "MESH_PpeCabinet_DoorHandle", new Vector3(0.04f, 0.2f, 0.04f), new Vector3(1.08f, 0f, -0.05f), matPolishedSteel, false);
            AddLocalBoxCollider(doorPanel);
            Interactable doorInteractable = AddInteractable(doorPanel, true, false, false);

            // Correct PPE: heat glove, chemical apron, face shield, respirator.
            GameObject heatGlove = AddModelChild(root, "MESH_HeatResistantGlove", SHARED_MODEL_DIR + "GUANTO.fbx",
                new Vector3(0.28f, 0.34f, 0.12f), new Vector3(-0.3f, 1.66f, 0.05f), false, true, matHeatGlove);

            var apron = NewChild(root, "MESH_ChemicalApron", new Vector3(0.3f, 1.28f, 0.06f));
            AddBox(apron, "MESH_ApronBody", new Vector3(0.42f, 0.76f, 0.04f), Vector3.zero, matApronRubber, false);
            AddBox(apron, "MESH_ApronStrap", new Vector3(0.05f, 0.3f, 0.03f), new Vector3(0f, 0.5f, 0f), matApronRubber, false);

            var shield = NewChild(root, "MESH_FaceShield", new Vector3(-0.3f, 0.95f, 0.1f));
            AddModelChild(shield, "MESH_FaceShieldGlasses", SHARED_MODEL_DIR + "OCCHIALI_PROTETTIVI.fbx",
                new Vector3(0.24f, 0.16f, 0.16f), Vector3.zero, false, true, null);
            AddQuad(shield, "MESH_FaceShieldVisor", new Vector3(0.34f, 0.3f), new Vector3(0f, 0f, 0.12f),
                new Vector3(0f, 180f, 0f), matFaceShieldVisor);

            var respirator = NewChild(root, "MESH_Respirator", new Vector3(0.3f, 0.72f, 0.06f));
            AddModelChild(respirator, "MESH_RespiratorMask", SHARED_MODEL_DIR + "MASCHERINA.fbx",
                new Vector3(0.26f, 0.18f, 0.2f), Vector3.zero, false, true, matRespirator);
            AddCylinder(respirator, "MESH_RespiratorFilterLeft", 0.045f, 0.08f,
                new Vector3(-0.1f, -0.03f, -0.1f), new Vector3(90f, 0f, 0f), matRespirator, false);
            AddCylinder(respirator, "MESH_RespiratorFilterRight", 0.045f, 0.08f,
                new Vector3(0.1f, -0.03f, -0.1f), new Vector3(90f, 0f, 0f), matRespirator, false);

            // Wrong PPE: latex gloves and surgical mask.
            GameObject latexGlove = AddModelChild(root, "MESH_LatexGlove", SHARED_MODEL_DIR + "GUANTO.fbx",
                new Vector3(0.26f, 0.3f, 0.1f), new Vector3(-0.3f, 0.2f, 0.08f), false, true, matLatex);
            GameObject surgicalMask = AddModelChild(root, "MESH_SurgicalMask", SHARED_MODEL_DIR + "MASCHERINA.fbx",
                new Vector3(0.22f, 0.16f, 0.16f), new Vector3(0.3f, 0.18f, 0.08f), false, true, null);

            AudioSource cabinetAudio = AddAudioSource(root, null, false, false, 1f, 0.8f);
            var cabinet = root.AddComponent<MachineLockPpeCabinet>();
            SetRef(cabinet, "cabinetInteractable", doorInteractable);
            SetRef(cabinet, "doorTransform", door.transform);
            SetSerializedFloat(cabinet, "openDoorAngleY", -105f);
            SetRef(cabinet, "oneShotAudio", cabinetAudio);
            SetRef(cabinet, "equipClip", clips.EquipPpe);
            SetRef(cabinet, "openClip", clips.EStopClack);

            // The six items: four correct types plus the two distractors (GDD §9.6).
            MachineLockPpeItem[] items = new MachineLockPpeItem[6];
            items[0] = BuildPpeItem(cabinet, heatGlove, PpeSlot.Gloves, true, "Guanti anticalore EN 407");
            items[1] = BuildPpeItem(cabinet, apron, PpeSlot.Body, true, "Grembiule antischizzo");
            items[2] = BuildPpeItem(cabinet, shield, PpeSlot.Face, true, "Visiera facciale EN 166");
            items[3] = BuildPpeItem(cabinet, respirator, PpeSlot.Respiratory, true, "Respiratore FFP3");
            items[4] = BuildPpeItem(cabinet, latexGlove, PpeSlot.Gloves, false, "Guanti in lattice monouso");
            items[5] = BuildPpeItem(cabinet, surgicalMask, PpeSlot.Respiratory, false, "Mascherina chirurgica");
            SetRefArray(cabinet, "items", new UnityEngine.Object[]
            {
                items[0], items[1], items[2], items[3], items[4], items[5]
            });
            SavePrefab(root);
        }

        /// <summary>
        /// Adds a selectable PPE item to a cabinet: its own collider, a non-selectable Interactable (the
        /// cabinet drives the selectable flag) and a <see cref="MachineLockPpeItem"/> with the GDD §9.6 data.
        /// </summary>
        private static MachineLockPpeItem BuildPpeItem(MachineLockPpeCabinet cabinet, GameObject item, PpeSlot slot,
            bool correct, string displayName)
        {
            FitBoxCollider(item, 0.01f);
            Interactable interactable = AddInteractable(item, false, false, false);
            var ppeItem = item.AddComponent<MachineLockPpeItem>();
            ppeItem.Configure(cabinet, slot, correct, displayName);
            SetRef(ppeItem, "interactable", interactable);
            return ppeItem;
        }

        // -- MESH_SpillKit -----------------------------------------------------

        private static void BuildSpillKitPrefab(Scene temp)
        {
            var root = NewPrefabRoot(temp, "MESH_SpillKit");
            AddBox(root, "MESH_SpillKitFloor", new Vector3(1.0f, 0.06f, 0.6f), new Vector3(0f, 0.06f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_SpillKitSideNorth", new Vector3(1.0f, 0.34f, 0.04f), new Vector3(0f, 0.23f, 0.28f), matPaintedSteel, false);
            AddBox(root, "MESH_SpillKitSideSouth", new Vector3(1.0f, 0.34f, 0.04f), new Vector3(0f, 0.23f, -0.28f), matPaintedSteel, false);
            AddBox(root, "MESH_SpillKitSideEast", new Vector3(0.04f, 0.34f, 0.6f), new Vector3(0.48f, 0.23f, 0f), matPaintedSteel, false);
            AddBox(root, "MESH_SpillKitSideWest", new Vector3(0.04f, 0.34f, 0.6f), new Vector3(-0.48f, 0.23f, 0f), matPaintedSteel, false);
            AddCylinder(root, "MESH_SpillKitWheelNW", 0.06f, 0.04f, new Vector3(-0.42f, 0.06f, -0.24f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelNE", 0.06f, 0.04f, new Vector3(0.42f, 0.06f, -0.24f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelSW", 0.06f, 0.04f, new Vector3(-0.42f, 0.06f, 0.24f), new Vector3(90f, 0f, 0f), matApronRubber, false);
            AddCylinder(root, "MESH_SpillKitWheelSE", 0.06f, 0.04f, new Vector3(0.42f, 0.06f, 0.24f), new Vector3(90f, 0f, 0f), matApronRubber, false);

            AddNestedPrefab(root, "MESH_Cone", new Vector3(-0.36f, 0.4f, 0f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(-0.12f, 0.4f, 0f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(0.12f, 0.4f, 0f), Vector3.zero);
            AddNestedPrefab(root, "MESH_Cone", new Vector3(0.36f, 0.4f, 0f), Vector3.zero);
            AddNestedPrefab(root, "MESH_AbsorbentMat", new Vector3(0.2f, 0.43f, 0.18f), Vector3.zero);
            AddNestedPrefab(root, "MESH_AbsorbentMat", new Vector3(0.2f, 0.46f, 0.18f), Vector3.zero);
            AddNestedPrefab(root, "MESH_AbsorbentSock", new Vector3(-0.2f, 0.45f, 0.16f), new Vector3(0f, 20f, 0f));
            AddNestedPrefab(root, "MESH_AbsorbentSock", new Vector3(-0.2f, 0.48f, 0.2f), new Vector3(0f, -15f, 0f));

            AddBoxCollider(root, new Vector3(0f, 0.2f, 0f), new Vector3(1.0f, 0.4f, 0.6f));
            SavePrefab(root);
        }

        // -- MESH_HydraulicPress -----------------------------------------------

        private static void BuildPressPrefab(Scene temp, MachineLockAudioClips clips)
        {
            var root = NewPrefabRoot(temp, "MESH_HydraulicPress");
            AddBox(root, "MESH_PressBase", new Vector3(3.0f, 1.2f, 2.5f), new Vector3(0f, 0.6f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_PressCrown", new Vector3(3.0f, 1.2f, 2.5f), new Vector3(0f, 2.8f, 0f), matPaintedSteel, true);
            AddBox(root, "MESH_PressColumnNW", new Vector3(0.2f, 1.2f, 0.2f), new Vector3(-1.3f, 1.7f, -1.05f), matPolishedSteel, true);
            AddBox(root, "MESH_PressColumnNE", new Vector3(0.2f, 1.2f, 0.2f), new Vector3(-1.3f, 1.7f, 1.05f), matPolishedSteel, true);
            AddBox(root, "MESH_PressColumnSW", new Vector3(0.2f, 1.2f, 0.2f), new Vector3(1.3f, 1.7f, -1.05f), matPolishedSteel, true);
            AddBox(root, "MESH_PressColumnSE", new Vector3(0.2f, 1.2f, 0.2f), new Vector3(1.3f, 1.7f, 1.05f), matPolishedSteel, true);
            GameObject ram = AddBox(root, "MESH_HydraulicPress_Ram", new Vector3(1.2f, 0.4f, 1.2f), new Vector3(0f, 2.0f, 0f), matPolishedSteel, true);
            AddBox(root, "MESH_PressGuard", new Vector3(2.6f, 0.9f, 0.06f), new Vector3(0f, 0.75f, 1.28f), matPaintedSteel, true);
            AddBox(root, "MESH_HydraulicPress_ControlPanel", new Vector3(0.3f, 0.75f, 0.55f), new Vector3(-1.6f, 1.6f, -1.1f), matPaintedSteel, true);

            AddBox(root, "MESH_PressDisplayFrame", new Vector3(0.06f, 0.44f, 0.6f), new Vector3(-1.52f, 1.5f, -0.8f), matPaintedSteel, false);
            GameObject display = AddBox(root, "MESH_PressDisplay", new Vector3(0.03f, 0.36f, 0.5f), new Vector3(-1.56f, 1.5f, -0.8f), matHmiScreen, false);
            NewChild(display, "VFX_DisplayFault", Vector3.zero);

            AddCylinder(root, "MESH_Thermometer", 0.07f, 0.06f, new Vector3(-1.56f, 1.7f, 0.2f), new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddCylinder(root, "MESH_ThermometerFace", 0.055f, 0.02f, new Vector3(-1.6f, 1.7f, 0.2f), new Vector3(0f, 0f, 90f), matEStopRed, false);

            AddCylinder(root, "MESH_LeakPipe", 0.05f, 0.5f, new Vector3(1.6f, 0.95f, 0.5f), new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddBox(root, "MESH_LeakPipeElbow", new Vector3(0.12f, 0.12f, 0.12f), new Vector3(1.85f, 0.95f, 0.5f), matPolishedSteel, false);

            // Kinematic body plus a trigger volume for the player contact (GDD §9.1).
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
            light.intensity = 0.9f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;

            // A dark enclosed room without baked lightmaps needs ambient fill to stay readable.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.26f, 0.28f, 0.31f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;
        }

        private static void BuildRoom(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Room");

            GameObject floor = AddBox(env, "MESH_Floor", new Vector3(12f, 0.1f, 8f), new Vector3(0f, -0.05f, 0f), matConcreteFloor, true);

            var walls = NewChild(env, "MESH_Walls", Vector3.zero);
            AddBox(walls, "MESH_WallNorth", new Vector3(12.2f, 4f, 0.1f), new Vector3(0f, 2f, 4.05f), matWallConcrete, true);
            AddBox(walls, "MESH_WallSouth", new Vector3(12.2f, 4f, 0.1f), new Vector3(0f, 2f, -4.05f), matWallConcrete, true);
            AddBox(walls, "MESH_WallEast", new Vector3(0.1f, 4f, 8.2f), new Vector3(6.05f, 2f, 0f), matWallConcrete, true);
            AddBox(walls, "MESH_WallWestSouth", new Vector3(0.1f, 4f, 3.5f), new Vector3(-6.05f, 2f, -2.35f), matWallConcrete, true);
            AddBox(walls, "MESH_WallWestNorth", new Vector3(0.1f, 4f, 3.5f), new Vector3(-6.05f, 2f, 2.35f), matWallConcrete, true);
            AddBox(walls, "MESH_WallWestLintel", new Vector3(0.1f, 1.8f, 1.2f), new Vector3(-6.05f, 3.1f, 0f), matWallConcrete, true);

            GameObject ceiling = AddBox(env, "MESH_Ceiling", new Vector3(12f, 0.1f, 8f), new Vector3(0f, 4.05f, 0f), matCeilingConcrete, false);

            // Baked GI and static batching on the shell: the room never moves (GDD §7.7).
            MarkStatic(floor);
            MarkStatic(ceiling);
            foreach (Transform wall in walls.transform) MarkStatic(wall.gameObject);

            var door = NewChild(env, "MESH_Door", new Vector3(-6f, 0f, 0f));
            InstantiatePrefabInScene(scene, "MESH_IndustrialDoor_RollUp", door.transform, Vector3.zero, Vector3.zero);

            var signs = NewChild(env, "MESH_Signs", Vector3.zero);
            AddWallSign(signs, "MESH_SignStampaggio", "MAT_Sign_Stampaggio", new Vector2(2.4f, 1.0f),
                new Vector3(0f, 2.9f, 3.96f), new Vector3(0f, 0f, 0f));
            AddWallSign(signs, "MESH_SignQuadroGenerale", "MAT_Sign_QuadroGenerale", new Vector2(1.6f, 0.8f),
                new Vector3(5.96f, 2.55f, 0f), new Vector3(0f, 90f, 0f));
            AddWallSign(signs, "MESH_SignDpi", "MAT_Sign_Dpi", new Vector2(1.2f, 0.6f),
                new Vector3(2.5f, 2.6f, -3.96f), new Vector3(0f, 180f, 0f));
            AddWallSign(signs, "MESH_SignKitAntispandimento", "MAT_Sign_KitAntispandimento", new Vector2(1.7f, 0.85f),
                new Vector3(-4.5f, 2.5f, -3.96f), new Vector3(0f, 180f, 0f));
            AddWallSign(signs, "MESH_SignUscita", "MAT_Sign_Uscita", new Vector2(1.0f, 0.5f),
                new Vector3(-5.96f, 2.75f, 0f), new Vector3(0f, -90f, 0f));

            AddFloorMarking(signs, "MESH_FloorMarkingNorth", new Vector2(7.8f, 0.12f), new Vector3(0f, 0.01f, 2.9f));
            AddFloorMarking(signs, "MESH_FloorMarkingSouth", new Vector2(7.8f, 0.12f), new Vector3(0f, 0.01f, -2.9f));
            AddFloorMarking(signs, "MESH_FloorMarkingEast", new Vector2(0.12f, 5.8f), new Vector3(3.9f, 0.01f, 0f));
            AddFloorMarking(signs, "MESH_FloorMarkingWest", new Vector2(0.12f, 5.8f), new Vector3(-3.9f, 0.01f, 0f));

            BuildCeilingFixtures(signs);
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
                new Vector3(-3.5f, 3.45f, -1.5f),
                new Vector3(0f, 3.45f, 1.5f),
                new Vector3(3.5f, 3.45f, -1.5f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                var fixture = NewChild(parent, "MESH_CeilingLight0" + (i + 1), positions[i]);
                var light = fixture.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.95f, 0.86f);
                light.intensity = 1.1f;
                light.range = 7f;
                light.shadows = LightShadows.None;
                light.lightmapBakeType = LightmapBakeType.Baked;
                AddQuad(fixture, "MESH_CeilingLight0" + (i + 1) + "Panel", new Vector2(1.2f, 0.35f),
                    new Vector3(0f, 0.5f, 0f), new Vector3(-90f, 0f, 0f), matLightFixture);
            }
        }

        // ------------------------------------------------------------------ Press area

        private static void BuildPressArea(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Press");
            InstantiatePrefabInScene(scene, "MESH_HydraulicPress", env.transform, Vector3.zero, Vector3.zero);

            // Pipe run from the press east leak to the valve near the east wall (GDD §7.2).
            AddCylinder(env, "MESH_PipeStraight01", 0.09f, 3.55f, new Vector3(3.45f, 0.95f, 0.5f), new Vector3(0f, 0f, 90f), matPolishedSteel, false);
            AddSphere(env, "MESH_PipeElbow01", 0.2f, new Vector3(5.2f, 0.95f, 0.5f), matPolishedSteel, false);
            AddCylinder(env, "MESH_PipeStraight02", 0.09f, 1.0f, new Vector3(5.2f, 0.95f, 0f), new Vector3(90f, 0f, 0f), matPolishedSteel, false);
            AddBox(env, "MESH_PipeSupport01", new Vector3(0.08f, 0.9f, 0.08f), new Vector3(4.4f, 0.45f, 0.5f), matPaintedSteel, false);

            InstantiatePrefabInScene(scene, "MESH_Valve", env.transform, new Vector3(5.2f, 1.0f, -0.5f), Vector3.zero);
        }

        // ------------------------------------------------------------------ Safety area

        private static void BuildSafetyArea(Scene scene)
        {
            GameObject env = NewRoot(scene, "ENV_Safety");
            InstantiatePrefabInScene(scene, "MESH_EStopPedestal", env.transform, new Vector3(-4.2f, 0f, 0f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_AlarmButton", env.transform, new Vector3(-5.9f, 1.4f, 1.8f), new Vector3(0f, 90f, 0f));
            InstantiatePrefabInScene(scene, "MESH_ElectricalPanel", env.transform, new Vector3(5.9f, 1.2f, 0f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_PpeCabinet", env.transform, new Vector3(2.5f, 0f, -3.9f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_SpillKit", env.transform, new Vector3(-4.5f, 0f, -3.2f), Vector3.zero);
            InstantiatePrefabInScene(scene, "MESH_WaterExtinguisher", env.transform, new Vector3(3.5f, 1.2f, 3.9f), Vector3.zero);
        }

        // ------------------------------------------------------------------ Hazard area

        private static void BuildHazardArea(Scene scene, MachineLockAudioClips clips)
        {
            GameObject hazard = NewRoot(scene, "HAZARD_Oil");

            // Leak: thin dark jet plus the looping hiss (GDD §13.1 and §13.2).
            var jet = NewChild(hazard, "VFX_OilJet", new Vector3(1.95f, 0.9f, 0.5f));
            AddCylinder(jet, "VFX_OilJetStream", 0.012f, 0.9f, new Vector3(0f, -0.45f, 0f), Vector3.zero, matOil, false);
            AddParticles(jet, "VFX_OilParticles", new Vector3(0f, -0.1f, 0f), Vector3.zero,
                matParticleSoft, 80, true, 35f, new Color(0.09f, 0.06f, 0.03f, 0.9f), 0.05f, 0.4f, 1f, 0.03f,
                ParticleSystemShapeType.Cone);
            AddAudioSource(jet, clips.OilLeak, true, true, 1f, 0.7f);

            var puddle = NewChild(hazard, "VFX_OilPuddle", Vector3.zero);
            AddPuddleQuadrant(puddle, "MESH_PuddleQuadrantNE", 1f, 1f);
            AddPuddleQuadrant(puddle, "MESH_PuddleQuadrantNW", -1f, 1f);
            AddPuddleQuadrant(puddle, "MESH_PuddleQuadrantSE", 1f, -1f);
            AddPuddleQuadrant(puddle, "MESH_PuddleQuadrantSW", -1f, -1f);

            // Steam over the hot oil and the inaction smoke over the press (GDD §9.12 and §13.2).
            AddParticles(hazard, "VFX_Steam", new Vector3(0f, 0.35f, 1.8f), Vector3.zero,
                matParticleSoft, 60, true, 12f, new Color(0.85f, 0.86f, 0.85f, 0.35f), 0.35f, 0.35f, 3f, 1.4f,
                ParticleSystemShapeType.Sphere);
            AddParticles(hazard, "VFX_Smoke", new Vector3(0f, 2.6f, 0f), Vector3.zero,
                matParticleSoft, 120, false, 0f, new Color(0.16f, 0.16f, 0.16f, 0.75f), 0.9f, 0.3f, 5f, 1.2f,
                ParticleSystemShapeType.Sphere);

            var zone = NewChild(hazard, "ZONE_PuddleHazard", new Vector3(0f, 0.25f, 0f));
            var zoneCollider = zone.AddComponent<BoxCollider>();
            zoneCollider.center = Vector3.zero;
            zoneCollider.size = new Vector3(7f, 0.5f, 5f);
            zoneCollider.isTrigger = true;

            for (int i = 0; i < 4; i++) BuildCoverageZone(hazard, i);

            var patternA = NewChild(hazard, "PATTERN_PuddleA", Vector3.zero);
            patternA.transform.localScale = new Vector3(7f, 1f, 5f);
            var patternB = NewChild(hazard, "PATTERN_PuddleB", new Vector3(0.5f, 0f, 0f));
            patternB.transform.localScale = new Vector3(6.5f, 1f, 5f);
            var patternC = NewChild(hazard, "PATTERN_PuddleC", new Vector3(0f, 0f, -0.25f));
            patternC.transform.localScale = new Vector3(7f, 1f, 4.5f);

            hazard.AddComponent<MachineLockSpillSystem>();
        }

        /// <summary>
        /// Builds one absorbent coverage zone with its Guided ghost, hidden until the player holds an item.
        /// Quadrant order matches the puddle meshes: 0 NE, 1 NW, 2 SE, 3 SW.
        /// </summary>
        private static void BuildCoverageZone(GameObject hazard, int quadrant)
        {
            float signX = quadrant == 1 || quadrant == 3 ? -1f : 1f;
            float signZ = quadrant >= 2 ? -1f : 1f;
            GameObject zone = NewChild(hazard, "ZONE_Coverage" + (quadrant + 1),
                new Vector3(1.75f * signX, 0.02f, 1.25f * signZ));

            GameObject ghost = BuildGhost(zone, new Vector3(2.6f, 0.02f, 1.0f), false);
            var snapZone = zone.AddComponent<MachineLockSnapZone>();
            snapZone.Configure(MachineLockSnapZone.SnapKind.Absorbent, zone.transform, ghost, quadrant);
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
        /// Guided snap ghost of one zone: a flat mat or a small cone in the transparent highlight
        /// material, inactive until the spill system shows it.
        /// </summary>
        private static GameObject BuildGhost(GameObject zone, Vector3 size, bool cone)
        {
            var ghost = NewChild(zone, "VFX_SnapGhost", Vector3.zero);
            if (cone)
            {
                AddCylinder(ghost, "MESH_GhostConeBody", 0.15f, 0.5f, new Vector3(0f, 0.25f, 0f), Vector3.zero, matGhostSnap, false);
            }
            else
            {
                AddBox(ghost, "MESH_GhostCoverage", new Vector3(size.x, size.y, size.z),
                    new Vector3(0f, 0.01f, 0f), matGhostSnap, false);
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
                new Vector3(3.8f, 0f, 2.8f),
                new Vector3(-3.8f, 0f, 2.8f),
                new Vector3(3.8f, 0f, -2.8f),
                new Vector3(-3.8f, 0f, -2.8f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject point = NewChild(delimitation, "ZONE_ConePoint" + (i + 1), points[i]);
                GameObject ghost = BuildGhost(point, Vector3.zero, true);
                var snapZone = point.AddComponent<MachineLockSnapZone>();
                snapZone.Configure(MachineLockSnapZone.SnapKind.Cone, point.transform, ghost, -1);
            }
        }

        private static void BuildLayoutPatterns(Scene scene)
        {
            GameObject patterns = NewRoot(scene, "MNG_LayoutPatterns");

            // GDD §7.6: one of three randomised layouts.
            var a = NewChild(patterns, "PATTERN_A", Vector3.zero);
            NewChild(a, "MarkerPpeCabinet", new Vector3(2.5f, 0f, -3.9f));
            NewChild(a, "MarkerSpillKit", new Vector3(-4.5f, 0f, -3.2f));
            NewChild(a, "MarkerExtinguisher", new Vector3(3.5f, 1.2f, 3.9f));

            var b = NewChild(patterns, "PATTERN_B", Vector3.zero);
            NewChild(b, "MarkerPpeCabinet", new Vector3(4.5f, 0f, -3.9f));
            NewChild(b, "MarkerSpillKit", new Vector3(-4.5f, 0f, 3.2f));
            NewChild(b, "MarkerExtinguisher", new Vector3(5.7f, 1.2f, 2.5f));

            var c = NewChild(patterns, "PATTERN_C", Vector3.zero);
            NewChild(c, "MarkerPpeCabinet", new Vector3(-5.7f, 0f, -2.5f));
            NewChild(c, "MarkerSpillKit", new Vector3(3.5f, 0f, -3.2f));
            NewChild(c, "MarkerExtinguisher", new Vector3(5.7f, 1.2f, 3.2f));
        }

        private static void BuildObservationTargets(Scene scene, MachineLockConfig config)
        {
            GameObject targets = NewRoot(scene, "MNG_ObservationTargets");
            var scenario = FindRoot(scene, "MNG_Scenario");
            if (scenario == null)
                throw new InvalidOperationException("[MachineLock] MNG_Scenario is missing before the observation targets are built.");

            Vector3[] positions =
            {
                new Vector3(0f, 1.6f, 0f),
                new Vector3(-1.5f, 1.7f, 0.2f),
                new Vector3(-1.5f, 1.5f, -0.8f),
                new Vector3(0f, 0.05f, 1.8f),
                new Vector3(5.9f, 1.2f, 0f),
                new Vector3(2.5f, 1.0f, -3.9f)
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
            exit.transform.position = new Vector3(-6f, 0f, 0f);
            var trigger = exit.AddComponent<ScenarioTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("radius").floatValue = 1f;
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
            SetRef(feedback, "oneShotAudio", oneShot);
            SetRef(feedback, "shockClip", clips.Shock);
            SetRef(feedback, "burnClip", clips.Burn);
            SetRef(feedback, "steamClip", clips.SteamBurst);
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

        // ------------------------------------------------------------------ Labels (GDD §8.4)

        private static void PlaceLabels(Scene scene)
        {
            AddLabel(Find("MESH_HydraulicPress"), LabelKind.Risk, "Pressa in moto", "Non avvicinarti", new Vector3(0f, 2.2f, 0f));
            AddLabel(Find("MESH_Thermometer"), LabelKind.Risk, "Olio a 120 °C", "Superficie bollente", new Vector3(0f, 0.15f, 0f));
            AddLabel(Find("VFX_OilPuddle"), LabelKind.Risk, "Pozza d'olio bollente", "Non calpestare", new Vector3(0f, 0.3f, 1.9f));

            AddLabel(Find("MESH_EStopPedestal"), LabelKind.ContextHelp, "Fungo di emergenza", "Premi per fermare la pressa", new Vector3(0f, 1.35f, 0f));
            AddLabel(Find("MESH_AlarmButton"), LabelKind.ContextHelp, "Allarme reparto", "Premi per chiamare i soccorsi", new Vector3(0f, 0.45f, 0f));
            AddLabel(Find("MESH_ElectricalPanel"), LabelKind.ContextHelp, "Quadro generale", "Apri l'interruttore principale", new Vector3(0f, 0.7f, 0f));
            AddLabel(Find("MESH_LockoutPoint"), LabelKind.ContextHelp, "Punto di lockout", "Blocca il quadro dopo lo stacco", new Vector3(0f, 0.2f, 0f));
            AddLabel(Find("MESH_VoltageTester"), LabelKind.ContextHelp, "Tester di tensione", "Controlla l'energia residua", new Vector3(0f, 0.25f, 0f));
            AddLabel(Find("MESH_PpeCabinet"), LabelKind.ContextHelp, "Armadio DPI", "Apri e indossa i DPI", new Vector3(0f, 2.1f, 0f));
            AddLabel(Find("MESH_SpillKit"), LabelKind.ContextHelp, "Kit assorbente", "Prendi coni, tappeti e sacchi", new Vector3(0f, 0.7f, 0f));
            AddLabel(Find("MESH_Valve"), LabelKind.ContextHelp, "Valvola", "Chiudi per fermare la perdita", new Vector3(0f, 0.35f, 0f));
            AddLabel(Find("MESH_WaterExtinguisher"), LabelKind.ContextHelp, "Estintore ad acqua", "Non usare su olio o quadro", new Vector3(0f, 0.5f, 0f));

            AddLabel(Find("ZONE_Exit"), LabelKind.Interaction, "Uscita", string.Empty, new Vector3(0f, 0.5f, 0f));
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
