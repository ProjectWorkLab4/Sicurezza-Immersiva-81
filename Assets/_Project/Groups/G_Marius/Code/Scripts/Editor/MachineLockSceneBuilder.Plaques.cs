#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Quest 3 ASTC texture import configuration, UNI EN ISO 7010 / D.Lgs. 81/08 signage material
    /// binding, and brushed-aluminum Diegetic Plaque setup for <see cref="MachineLockSceneBuilder"/>
    /// (ADR-0002, ADR-0005, Issues #37-#40).
    /// </summary>
    public static partial class MachineLockSceneBuilder
    {
        /// <summary>All 14 UNI EN ISO 7010 / D.Lgs. 81/08 safety sign keys authored in G_Marius.</summary>
        public static readonly string[] ISO_SIGN_KEYS =
        {
            "W012", "W017", "W024", "W011",
            "P011",
            "M008", "M009", "M013", "M026", "PpeBoard",
            "F001", "F005",
            "E001", "E002"
        };

        /// <summary>The 6 engraved brushed-aluminum Diegetic Plaque keys (electrical Sub-Stations &amp; press controls).</summary>
        public static readonly string[] METAL_PLAQUE_KEYS =
        {
            "MainSwitch", "Lockout", "Tester",
            "EStop", "PressNameplate", "DeptHeader"
        };

        /// <summary>All scenario PBR texture filenames configured with Quest 3 Android ASTC overrides.</summary>
        public static readonly string[] PBR_TEXTURE_FILES =
        {
            "TEX_WorkshopFloor_Albedo.jpg", "TEX_WorkshopFloor_Normal.png", "TEX_WorkshopFloor_Rough.jpg",
            "TEX_FactoryWall_Albedo.jpg", "TEX_FactoryWall_Normal.png", "TEX_FactoryWall_Rough.jpg",
            "TEX_MetalPlate_Albedo.jpg", "TEX_MetalPlate_Normal.png", "TEX_MetalPlate_Rough.jpg",
            "TEX_CorrugatedIron_Albedo.jpg", "TEX_CorrugatedIron_Normal.png", "TEX_CorrugatedIron_Rough.jpg",
            "TEX_WorkbenchWood_Albedo.jpg", "TEX_WorkbenchWood_Normal.png", "TEX_WorkbenchWood_Rough.jpg",
            "TEX_GreenMetalRust_Albedo.jpg", "TEX_GreenMetalRust_Normal.exr", "TEX_GreenMetalRust_Rough.jpg",
            "TEX_ConcreteWall009_Albedo.jpg", "TEX_ConcreteWall009_Normal.exr", "TEX_ConcreteWall009_Rough.jpg",
            "TEX_AbsorbentMat_Albedo.png", "TEX_AbsorbentMat_QuiltedNormal.png",
            "TEX_AbsorbentSock_FabricAlbedo.png", "TEX_AbsorbentSock_FabricNormal.png",
            "TEX_OilRipple_Normal.png"
        };

        /// <summary>The 6 clean anti-aliased PPE shelf equipped-status badge keys.</summary>
        public static readonly string[] PPE_EQUIPPED_BADGE_KEYS =
        {
            "Ppe_Equipped_HeatGloves",
            "Ppe_Equipped_Apron",
            "Ppe_Equipped_FaceShield",
            "Ppe_Equipped_SafetyBoots",
            "Ppe_Equipped_LatexGloves",
            "Ppe_Equipped_SurgicalMask"
        };

        /// <summary>
        /// Configures Android ASTC overrides (1024 ASTC 6x6, zero displacement maps) across all
        /// PolyHaven CC0 workshop/machinery PBR sets and procedural absorbent/oil maps.
        /// Never writes to <c>Assets/_Project/Shared/</c>.
        /// </summary>
        private static void EnsurePbrWorkshopAndPropTextures()
        {
            // Shared glove normal copied into G_Marius (never binding group materials to Shared originals).
            CopySharedTextureToGroup(
                SHARED_TEXTURE_DIR + "NORMAL_GUANTO.png",
                "TEX_Glove_Normal.png",
                TextureImporterType.NormalMap, false, 1024);

            // PolyHaven CC0 PBR texture sets (1K, Android 1024 ASTC 6x6)
            ConfigurePbrSet("TEX_WorkshopFloor", "jpg", "png", "jpg");
            ConfigurePbrSet("TEX_FactoryWall", "jpg", "png", "jpg");
            ConfigurePbrSet("TEX_MetalPlate", "jpg", "png", "jpg");
            ConfigurePbrSet("TEX_CorrugatedIron", "jpg", "png", "jpg");
            ConfigurePbrSet("TEX_WorkbenchWood", "jpg", "png", "jpg");
            ConfigurePbrSet("TEX_GreenMetalRust", "jpg", "exr", "jpg");
            ConfigurePbrSet("TEX_ConcreteWall009", "jpg", "exr", "jpg");

            // Procedural spill-prop & oil-ripple PBR maps (zero sausage texture)
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_AbsorbentMat_Albedo.png", TextureImporterType.Default, false, 1024, 1024);
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_AbsorbentMat_QuiltedNormal.png", TextureImporterType.NormalMap, false, 1024, 1024);
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_AbsorbentSock_FabricAlbedo.png", TextureImporterType.Default, false, 1024, 1024);
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_AbsorbentSock_FabricNormal.png", TextureImporterType.NormalMap, false, 1024, 1024);
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_OilRipple_Normal.png", TextureImporterType.NormalMap, false, 1024, 1024);
        }

        private static void ConfigurePbrSet(string prefix, string albedoExt, string normalExt, string roughExt)
        {
            ConfigurePbrTexture($"{TEXTURE_DIR}/{prefix}_Albedo.{albedoExt}", TextureImporterType.Default, false, 1024, 1024);
            ConfigurePbrTexture($"{TEXTURE_DIR}/{prefix}_Normal.{normalExt}", TextureImporterType.NormalMap, false, 1024, 1024);
            ConfigurePbrTexture($"{TEXTURE_DIR}/{prefix}_Rough.{roughExt}", TextureImporterType.Default, true, 1024, 1024);
        }

        private static void ConfigurePbrTexture(string assetPath, TextureImporterType type, bool linear,
            int defaultMaxSize, int androidMaxSize)
        {
            if (!File.Exists(assetPath))
                throw new InvalidOperationException($"[MachineLock] Required PBR texture not found: {assetPath}");

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            }
            if (importer == null)
                throw new InvalidOperationException($"[MachineLock] TextureImporter missing for {assetPath}");

            bool dirty = false;
            if (importer.textureType != type) { importer.textureType = type; dirty = true; }
            bool wantSrgb = !linear && type != TextureImporterType.NormalMap;
            if (importer.sRGBTexture != wantSrgb) { importer.sRGBTexture = wantSrgb; dirty = true; }
            if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (importer.filterMode != FilterMode.Trilinear) { importer.filterMode = FilterMode.Trilinear; dirty = true; }
            if (importer.anisoLevel != 4) { importer.anisoLevel = 4; dirty = true; }
            if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; dirty = true; }
            if (importer.maxTextureSize != defaultMaxSize) { importer.maxTextureSize = defaultMaxSize; dirty = true; }

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden || android.maxTextureSize != androidMaxSize || android.format != TextureImporterFormat.ASTC_6x6)
            {
                android.name = "Android";
                android.overridden = true;
                android.maxTextureSize = androidMaxSize;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(android);
                dirty = true;
            }

            if (dirty) importer.SaveAndReimport();
        }

        /// <summary>
        /// Configures crisp anti-aliased UNI EN ISO 7010 signs, brushed-aluminum engraved plaques, and
        /// PPE shelf equipped badges with Quest 3 ASTC 4x4 compression, and cleans up legacy pixel-font
        /// / sausage textures and materials removed in Issue #36.
        /// </summary>
        private static void EnsureSignageAndPlaqueTextures()
        {
            CleanupLegacyAssets();

            foreach (string key in ISO_SIGN_KEYS)
            {
                int maxSize = key == "PpeBoard" || key == "E001" || key == "E002" ? 1024 : 512;
                ConfigureSignTexture($"{TEXTURE_DIR}/TEX_IsoSign_{key}.png", maxSize);
            }

            foreach (string key in METAL_PLAQUE_KEYS)
            {
                int maxSize = key == "DeptHeader" ? 1024 : 512;
                ConfigureSignTexture($"{TEXTURE_DIR}/TEX_Plaque_{key}.png", maxSize);
            }

            foreach (string key in PPE_EQUIPPED_BADGE_KEYS)
            {
                ConfigureSignTexture($"{TEXTURE_DIR}/TEX_Plaque_{key}.png", 512);
            }
        }

        private static void ConfigureSignTexture(string assetPath, int maxSize)
        {
            if (!File.Exists(assetPath))
                throw new InvalidOperationException($"[MachineLock] Required sign/plaque texture missing: {assetPath}");

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            }
            if (importer == null)
                throw new InvalidOperationException($"[MachineLock] TextureImporter missing for {assetPath}");

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
            if (!importer.sRGBTexture) { importer.sRGBTexture = true; dirty = true; }
            if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (importer.filterMode != FilterMode.Trilinear) { importer.filterMode = FilterMode.Trilinear; dirty = true; }
            if (importer.anisoLevel != 4) { importer.anisoLevel = 4; dirty = true; }
            if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; dirty = true; }
            if (importer.maxTextureSize != maxSize) { importer.maxTextureSize = maxSize; dirty = true; }

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden || android.maxTextureSize != maxSize || android.format != TextureImporterFormat.ASTC_4x4)
            {
                android.name = "Android";
                android.overridden = true;
                android.maxTextureSize = maxSize;
                android.format = TextureImporterFormat.ASTC_4x4;
                android.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(android);
                dirty = true;
            }

            if (dirty) importer.SaveAndReimport();
        }

        /// <summary>
        /// Creates or updates URP Lit materials for all 14 ISO 7010 safety signs, 6 brushed-metal
        /// plaques, and 6 PPE shelf equipped badges.
        /// </summary>
        private static void EnsureSignageAndPlaqueMaterials()
        {
            foreach (string key in ISO_SIGN_KEYS)
            {
                // Overhead illuminated exit box (E002) glows noticeably brighter than standard enameled wall signs.
                float emission = key == "E002" ? 0.85f : (key == "E001" ? 0.45f : 0.24f);
                EnsureSignMaterial("MAT_IsoSign_" + key, "TEX_IsoSign_" + key + ".png", emission, 0.05f, 0.35f);
            }

            foreach (string key in METAL_PLAQUE_KEYS)
            {
                EnsureSignMaterial("MAT_Plaque_" + key, "TEX_Plaque_" + key + ".png", 0.22f, 0.32f, 0.48f);
            }

            foreach (string key in PPE_EQUIPPED_BADGE_KEYS)
            {
                EnsureSignMaterial("MAT_Plaque_" + key, "TEX_Plaque_" + key + ".png", 0.42f, 0.05f, 0.28f);
            }
        }

        private static void CleanupLegacyAssets()
        {
            string[] legacyTextures =
            {
                "TEX_Sign_Dpi.png", "TEX_Sign_KitAntispandimento.png", "TEX_Sign_QuadroGenerale.png",
                "TEX_Sign_Stampaggio.png", "TEX_Sign_Uscita.png",
                "TEX_Plaque_Alarm.png", "TEX_Plaque_DeptStampaggio.png", "TEX_Plaque_ExitHeader.png",
                "TEX_Plaque_Extinguisher.png", "TEX_Plaque_SpillKit.png", "TEX_Plaque_Valve.png",
                "TEX_Plaque_Ppe_HeatGloves.png", "TEX_Plaque_Ppe_Apron.png",
                "TEX_Plaque_Ppe_FaceShield.png", "TEX_Plaque_Ppe_SafetyBoots.png",
                "TEX_Plaque_Ppe_LatexGloves.png", "TEX_Plaque_Ppe_SurgicalMask.png",
                "TEX_AbsorbentSock_Albedo.png", "TEX_AbsorbentMat_Normal.png"
            };
            foreach (string file in legacyTextures)
            {
                string path = TEXTURE_DIR + "/" + file;
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null || File.Exists(path))
                    AssetDatabase.DeleteAsset(path);
            }

            string[] legacyMaterials =
            {
                "MAT_Sign_Dpi.mat", "MAT_Sign_KitAntispandimento.mat", "MAT_Sign_QuadroGenerale.mat",
                "MAT_Sign_Stampaggio.mat", "MAT_Sign_Uscita.mat",
                "MAT_Plaque_Alarm.mat", "MAT_Plaque_DeptStampaggio.mat", "MAT_Plaque_ExitHeader.mat",
                "MAT_Plaque_Extinguisher.mat", "MAT_Plaque_SpillKit.mat", "MAT_Plaque_Valve.mat",
                "MAT_Plaque_Ppe_HeatGloves.mat", "MAT_Plaque_Ppe_Apron.mat",
                "MAT_Plaque_Ppe_FaceShield.mat", "MAT_Plaque_Ppe_SafetyBoots.mat",
                "MAT_Plaque_Ppe_LatexGloves.mat", "MAT_Plaque_Ppe_SurgicalMask.mat",
                "MAT_IndustrialBuilding_Wall.mat", "MAT_IndustrialBuilding_Floor.mat",
                "MAT_IndustrialBuilding_Window.mat", "MAT_Respirator.mat",
                "MAT_PanicBarGreen.mat", "MAT_TrafficCone_Orange.mat"
            };
            foreach (string file in legacyMaterials)
            {
                string path = MATERIAL_DIR + "/" + file;
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null || File.Exists(path))
                    AssetDatabase.DeleteAsset(path);
            }
        }

        private static GameObject AddPlaque(GameObject parent, string name, string materialName,
            Vector2 size, Vector3 localPos, Vector3 localEuler)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_DIR + "/" + materialName + ".mat");
            return AddQuad(parent, name, size, localPos, localEuler, mat);
        }

        /// <summary>
        /// Mounts a framed UNI EN ISO 7010 wall sign or engraved aluminum header with a sleek 1.8 cm
        /// dark-slate aluminum backing plate flush against the workshop wall. Pass
        /// <paramref name="markStatic"/> = false when parented under a pattern-movable prefab.
        /// </summary>
        private static GameObject AddFramedWallSign(GameObject parent, string name, string materialName,
            Vector2 size, Vector3 localPos, Vector3 localEuler, bool markStatic = true)
        {
            var holder = NewChild(parent, name, localPos);
            holder.transform.localEulerAngles = localEuler;
            AddBox(holder, name + "Back", new Vector3(size.x + 0.03f, size.y + 0.03f, 0.018f),
                new Vector3(0f, 0f, 0.009f), matDarkSlate, false);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_DIR + "/" + materialName + ".mat");
            AddQuad(holder, name + "Face", size, new Vector3(0f, 0f, -0.002f), Vector3.zero, mat);
            if (markStatic) MarkStatic(holder);
            return holder;
        }
    }
}
#endif
