#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Procedural high-contrast Diegetic Plaque renderer and Android ASTC texture importer helpers
    /// for <see cref="MachineLockSceneBuilder"/> (ADR-0001, ADR-0002).
    /// </summary>
    public static partial class MachineLockSceneBuilder
    {
        /// <summary>Directory containing the shared OldIndustrialBuilding textures.</summary>
        public const string SHARED_BUILDING_TEX_DIR = "Assets/_Project/Shared/Art/Textures/TEX_OldIndustrialBuilding";

        private static readonly Dictionary<char, string[]> EXTENDED_GLYPHS = BuildExtendedGlyphs();

        /// <summary>
        /// Configures Android ASTC overrides on all supplied TEX_OldIndustrialBuilding textures and the
        /// downloaded Polyhaven CC0 PBR texture sets (1024/2048 ASTC on Quest 3; displacement map kept
        /// unreferenced and capped small).
        /// </summary>
        private static void EnsurePbrAndBuildingTextures()
        {
            // Shared concrete floor copy (G_Marius)
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "concrete_floor_worn_001_diff_4k.jpg",
                "TEX_ConcreteFloorWorn_Albedo.jpg",
                TextureImporterType.Default, false, 1024);
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "concrete_floor_worn_001_nor_gl_4k.exr",
                "TEX_ConcreteFloorWorn_Normal.exr",
                TextureImporterType.NormalMap, false, 1024);
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "concrete_floor_worn_001_rough_4k.jpg",
                "TEX_ConcreteFloorWorn_Rough.jpg",
                TextureImporterType.Default, true, 1024);

            // Shared prop companion textures copied into G_Marius (never binding group materials into Shared originals)
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "NORMAL_GUANTO.png",
                "TEX_Glove_Normal.png",
                TextureImporterType.NormalMap, false, 1024);
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "MAPPA_NORMALE_PANNO.png",
                "TEX_AbsorbentMat_Normal.png",
                TextureImporterType.NormalMap, false, 1024);
            CopyConcreteTexture(
                SHARED_TEXTURE_DIR + "TEXTURE_SAUSAGES.png",
                "TEX_AbsorbentSock_Albedo.png",
                TextureImporterType.Default, false, 1024);

            // Polyhaven CC0 green_metal_rust (painted press/machinery steel)
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_GreenMetalRust_Albedo.jpg", TextureImporterType.Default, false, 1024, 1024);
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_GreenMetalRust_Normal.exr", TextureImporterType.NormalMap, false, 1024, 1024);
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_GreenMetalRust_Rough.jpg", TextureImporterType.Default, true, 1024, 1024);

            // Polyhaven CC0 concrete_wall_009 (bay concrete piers & surrounds)
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_ConcreteWall009_Albedo.jpg", TextureImporterType.Default, false, 1024, 1024);
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_ConcreteWall009_Normal.exr", TextureImporterType.NormalMap, false, 1024, 1024);
            ConfigurePbrTexture(TEXTURE_DIR + "/TEX_ConcreteWall009_Rough.jpg", TextureImporterType.Default, true, 1024, 1024);

            // Shared TEX_OldIndustrialBuilding (newly added Shared drop — configure Quest 3 ASTC overrides)
            ConfigurePbrTexture(SHARED_BUILDING_TEX_DIR + "/buildingbase.jpg", TextureImporterType.Default, false, 2048, 2048);
            ConfigurePbrTexture(SHARED_BUILDING_TEX_DIR + "/buildingNormalMap.jpg", TextureImporterType.NormalMap, false, 2048, 1024);
            ConfigurePbrTexture(SHARED_BUILDING_TEX_DIR + "/buildingAmbientOcclusionMap.jpg", TextureImporterType.Default, true, 1024, 1024);
            ConfigurePbrTexture(SHARED_BUILDING_TEX_DIR + "/buildingSpecularMap.jpg", TextureImporterType.Default, true, 1024, 1024);
            ConfigurePbrTexture(SHARED_BUILDING_TEX_DIR + "/TexturesCom_ConcreteBare0348_1_S.jpg", TextureImporterType.Default, false, 1024, 1024);
            // Displacement map is omitted from Quest 3 materials; cap its import size so it stays lightweight if touched.
            ConfigurePbrTexture(SHARED_BUILDING_TEX_DIR + "/buildingDisplacementMap.jpg", TextureImporterType.Default, true, 512, 256);
        }

        private static void ConfigurePbrTexture(string assetPath, TextureImporterType type, bool linear,
            int defaultMaxSize, int androidMaxSize)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"[MachineLock] Optional PBR texture not found: {assetPath}");
                return;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

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
        /// Generates all wall signs and high-contrast multi-line Diegetic Plaques (ADR-0002) for
        /// electrical Sub-Stations, PPE locker bays, Spill Kit, Valve, E-Stop, Alarm, Extinguisher, and Exit.
        /// </summary>
        private static void EnsureDiegeticPlaqueTextures()
        {
            Color32 darkBody = new Color32(18, 22, 27, 255);
            Color32 white = new Color32(250, 252, 255, 255);
            Color32 softWhite = new Color32(228, 234, 242, 255);
            Color32 black = new Color32(15, 16, 18, 255);

            Color32 red = new Color32(198, 28, 24, 255);
            Color32 yellow = new Color32(242, 192, 12, 255);
            Color32 cyan = new Color32(16, 134, 198, 255);
            Color32 blue = new Color32(14, 78, 152, 255);
            Color32 orange = new Color32(214, 96, 14, 255);
            Color32 green = new Color32(14, 146, 68, 255);
            Color32 slate = new Color32(74, 82, 92, 255);

            // Electrical Sub-Station plaques
            BuildPlaqueTexture("TEX_Plaque_MainSwitch.png", "1. QUADRO GENERALE", "TOGLIE TENSIONE", "PREMI TRIGGER",
                red, white, darkBody, softWhite, red);
            BuildPlaqueTexture("TEX_Plaque_Lockout.png", "2. LOCKOUT (LOTO)", "BLOCCA IL QUADRO", "DOPO LO STACCO",
                yellow, black, darkBody, softWhite, yellow);
            BuildPlaqueTexture("TEX_Plaque_Tester.png", "3. TESTER TENSIONE", "VERIFICA ENERGIA", "RESIDUA (LIVE/ZERO)",
                cyan, white, darkBody, softWhite, cyan);

            // Core station plaques
            BuildPlaqueTexture("TEX_Plaque_EStop.png", "ARRESTO EMERGENZA", "ROSSO: FERMA PRESSA", "VERDE: PROVA RIAVVIO",
                red, white, darkBody, softWhite, yellow);
            BuildPlaqueTexture("TEX_Plaque_Alarm.png", "ALLARME REPARTO", "CHIAMA I SOCCORSI", "PREMI TRIGGER",
                red, white, darkBody, softWhite, red);
            BuildPlaqueTexture("TEX_Plaque_Valve.png", "VALVOLA OLIO", "FERMA LA PERDITA", "CHIUDI CON TRIGGER",
                red, white, darkBody, softWhite, yellow);
            BuildPlaqueTexture("TEX_Plaque_SpillKit.png", "KIT ANTISPANDIMENTO", "CONI + TAPPETI + SACCHI", "AFFERRA CON GRIP",
                orange, white, darkBody, softWhite, orange);
            BuildPlaqueTexture("TEX_Plaque_Extinguisher.png", "ESTINTORE AD ACQUA", "SOLO CLASSE A", "VIETATO SU OLIO!",
                red, white, darkBody, softWhite, white);
            BuildPlaqueTexture("TEX_Plaque_ExitHeader.png", "USCITA DI SICUREZZA", "CONSEGNA AL PREPOSTO", "A MESSA IN SICUREZZA",
                green, white, darkBody, softWhite, green);

            // PPE locker item plaques (4 correct + 2 distractors)
            BuildPlaqueTexture("TEX_Plaque_Ppe_HeatGloves.png", "GUANTI ANTICALORE", "EN 407 - MANI", "CALORE 120C",
                blue, white, darkBody, softWhite, orange);
            BuildPlaqueTexture("TEX_Plaque_Ppe_Apron.png", "GREMBIULE", "ANTISCHIZZO - CORPO", "GOMMA CHIMICA",
                blue, white, darkBody, softWhite, yellow);
            BuildPlaqueTexture("TEX_Plaque_Ppe_FaceShield.png", "VISIERA FACCIALE", "EN 166 - VISO", "SCHERMO INTEGRALE",
                blue, white, darkBody, softWhite, cyan);
            BuildPlaqueTexture("TEX_Plaque_Ppe_SafetyBoots.png", "SCARPE S3", "ANTINFORTUNISTICHE", "PIEDI - PUNTALE ACCIAIO",
                blue, white, darkBody, softWhite, yellow);
            BuildPlaqueTexture("TEX_Plaque_Ppe_LatexGloves.png", "GUANTI IN LATTICE", "MONOUSO - MANI", "SOTTILI NON TERMICI",
                slate, white, darkBody, softWhite, slate);
            BuildPlaqueTexture("TEX_Plaque_Ppe_SurgicalMask.png", "MASCHERINA", "CHIRURGICA - VISO", "TESSUTO MONOUSO",
                slate, white, darkBody, softWhite, slate);

            // Per-item shelf equipped status plaques ("<Nome DPI> indossato" — green for correct, amber/red for distractors)
            Color32 okBg = new Color32(12, 42, 22, 255);
            Color32 warnBg = new Color32(54, 18, 14, 255);
            BuildPlaqueTexture("TEX_Plaque_Ppe_Equipped_HeatGloves.png", "GUANTI ANTICALORE", "INDOSSATO", "PROTEZIONE MANI OK",
                green, white, okBg, white, green);
            BuildPlaqueTexture("TEX_Plaque_Ppe_Equipped_Apron.png", "GREMBIULE", "INDOSSATO", "PROTEZIONE CORPO OK",
                green, white, okBg, white, green);
            BuildPlaqueTexture("TEX_Plaque_Ppe_Equipped_FaceShield.png", "VISIERA FACCIALE", "INDOSSATO", "PROTEZIONE VISO OK",
                green, white, okBg, white, green);
            BuildPlaqueTexture("TEX_Plaque_Ppe_Equipped_SafetyBoots.png", "SCARPE S3", "INDOSSATO", "PROTEZIONE PIEDI OK",
                green, white, okBg, white, green);
            BuildPlaqueTexture("TEX_Plaque_Ppe_Equipped_LatexGloves.png", "GUANTI IN LATTICE", "INDOSSATO", "NON IDONEO A 120C!",
                orange, white, warnBg, softWhite, red);
            BuildPlaqueTexture("TEX_Plaque_Ppe_Equipped_SurgicalMask.png", "MASCHERINA", "INDOSSATO", "NON IDONEA A 120C!",
                orange, white, warnBg, softWhite, red);
        }

        /// <summary>
        /// Creates or updates URP Lit emissive materials for every generated Diegetic Plaque texture.
        /// </summary>
        private static void EnsureDiegeticPlaqueMaterials()
        {
            string[] names =
            {
                "MainSwitch", "Lockout", "Tester",
                "EStop", "Alarm", "Valve", "SpillKit", "Extinguisher", "ExitHeader",
                "Ppe_HeatGloves", "Ppe_Apron", "Ppe_FaceShield", "Ppe_SafetyBoots",
                "Ppe_LatexGloves", "Ppe_SurgicalMask",
                "Ppe_Equipped_HeatGloves", "Ppe_Equipped_Apron", "Ppe_Equipped_FaceShield",
                "Ppe_Equipped_SafetyBoots", "Ppe_Equipped_LatexGloves", "Ppe_Equipped_SurgicalMask"
            };

            foreach (string key in names)
            {
                EnsureSignMaterial("MAT_Plaque_" + key, "TEX_Plaque_" + key + ".png", 0.55f);
            }
        }

        /// <summary>
        /// Renders a crisp 512×256 multi-line industrial Diegetic Plaque (colored title banner +
        /// two high-contrast body lines + outer frame) with Quest 3 ASTC 4×4 compression.
        /// </summary>
        private static void BuildPlaqueTexture(string fileName, string headerText, string bodyLine1, string bodyLine2,
            Color32 headerBg, Color32 headerFg, Color32 bodyBg, Color32 bodyFg, Color32 borderColor)
        {
            const int width = 512;
            const int height = 256;
            const int border = 8;
            const int headerHeight = 96;

            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bodyBg;

            // Header band at top (high Y in Texture2D space)
            FillRect(pixels, width, height, 0, height - headerHeight, width, headerHeight, headerBg);
            // Divider line
            FillRect(pixels, width, height, 0, height - headerHeight - 4, width, 4, borderColor);
            // Outer border
            FillRect(pixels, width, height, 0, 0, width, border, borderColor);
            FillRect(pixels, width, height, 0, height - border, width, border, borderColor);
            FillRect(pixels, width, height, 0, 0, border, height, borderColor);
            FillRect(pixels, width, height, width - border, 0, border, height, borderColor);

            DrawCenteredLine(pixels, width, height, headerText, height - headerHeight / 2 - 2, width - 36, 5, headerFg);
            DrawCenteredLine(pixels, width, height, bodyLine1, 98, width - 36, 4, bodyFg);
            DrawCenteredLine(pixels, width, height, bodyLine2, 42, width - 36, 3, bodyFg);

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
            importer.anisoLevel = 4;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 512;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 512;
            android.format = TextureImporterFormat.ASTC_4x4;
            android.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }

        private static void DrawCenteredLine(Color32[] pixels, int width, int height, string text,
            int centerY, int maxWidth, int maxScale, Color32 color)
        {
            if (string.IsNullOrEmpty(text)) return;
            string upper = NormalizePlaqueText(text);
            int scale = 1;
            for (int candidate = maxScale; candidate >= 1; candidate--)
            {
                int w = upper.Length * 6 * candidate - candidate;
                if (w <= maxWidth)
                {
                    scale = candidate;
                    break;
                }
            }

            int textW = upper.Length * 6 * scale - scale;
            int x0 = (width - textW) / 2;
            int y0 = centerY - (7 * scale) / 2;

            for (int i = 0; i < upper.Length; i++)
            {
                if (!EXTENDED_GLYPHS.TryGetValue(upper[i], out string[] glyph))
                    glyph = EXTENDED_GLYPHS[' '];
                int glyphX = x0 + i * 6 * scale;
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if (glyph[row][col] != '1') continue;
                        int py = y0 + (6 - row) * scale;
                        FillRect(pixels, width, height, glyphX + col * scale, py, scale, scale, color);
                    }
                }
            }
        }

        private static string NormalizePlaqueText(string input)
        {
            return input.ToUpperInvariant()
                .Replace('À', 'A').Replace('È', 'E').Replace('É', 'E')
                .Replace('Ì', 'I').Replace('Ò', 'O').Replace('Ù', 'U')
                .Replace('—', '-').Replace('·', '.');
        }

        private static GameObject AddPlaque(GameObject parent, string name, string materialName,
            Vector2 size, Vector3 localPos, Vector3 localEuler)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_DIR + "/" + materialName + ".mat");
            return AddQuad(parent, name, size, localPos, localEuler, mat);
        }

        private static Dictionary<char, string[]> BuildExtendedGlyphs()
        {
            var g = new Dictionary<char, string[]>(BuildGlyphs())
            {
                { ':', new[] { "00000", "00100", "00100", "00000", "00100", "00100", "00000" } },
                { '/', new[] { "00001", "00010", "00010", "00100", "01000", "01000", "10000" } },
                { '(', new[] { "00010", "00100", "01000", "01000", "01000", "00100", "00010" } },
                { ')', new[] { "01000", "00100", "00010", "00010", "00010", "00100", "01000" } },
                { '!', new[] { "00100", "00100", "00100", "00100", "00100", "00000", "00100" } },
                { '+', new[] { "00000", "00100", "00100", "11111", "00100", "00100", "00000" } },
                { '\'', new[] { "00100", "00100", "01000", "00000", "00000", "00000", "00000" } },
                { '°', new[] { "01100", "10010", "01100", "00000", "00000", "00000", "00000" } }
            };
            return g;
        }
    }
}
#endif
