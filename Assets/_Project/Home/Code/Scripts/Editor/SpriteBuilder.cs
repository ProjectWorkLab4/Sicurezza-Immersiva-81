using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder degli sprite generati TEX_ in Assets/_Project/Home/Art/Textures/UI/: rettangoli arrotondati 9-slice bianchi e ombre
    /// morbide 9-slice pre-sfocate (niente componenti Shadow/Outline, niente sfocatura in tempo reale). I generatori
    /// sono riusabili con raggio e ombra come parametri (card, pulsanti e bottom bar li useranno con i loro token).
    /// Sicuro da rieseguire: riscrive solo i PNG il cui contenuto cambia, mantiene .meta e GUID, ricontrolla le
    /// impostazioni di import. Eseguito anche da SI81/UI/Build Theme.
    /// </summary>
    public static class SpriteBuilder
    {
        /// <summary>Cartella degli sprite generati.</summary>
        public const string SPRITES_FOLDER = ThemeBuilder.ROOT + "/Art/Textures/UI";
        /// <summary>Rettangolo arrotondato dei pannelli (radius.panel).</summary>
        public const string PANEL_ROUNDED = SPRITES_FOLDER + "/TEX_PanelRounded.png";
        /// <summary>Ombra dei pannelli menu (radius.panel + ombra menuPanel), bucata sotto il pannello.</summary>
        public const string PANEL_SHADOW = SPRITES_FOLDER + "/TEX_PanelShadow.png";
        /// <summary>Ombra compatta dei pannelli bassi (PANEL_Side): bordi 9-slice che stanno in pannelli alti ≥ size.shadow.compact-min-height.</summary>
        public const string PANEL_SHADOW_COMPACT = SPRITES_FOLDER + "/TEX_PanelShadowCompact.png";
        /// <summary>Pillola bianca 9-slice (raggio = metà altezza della bottom bar): PANEL_BottomBar e pillola hover di BTN_BottomBarItem.</summary>
        public const string PILL_ROUNDED = SPRITES_FOLDER + "/TEX_PillRounded.png";
        /// <summary>Ombra compatta di PANEL_BottomBar (ombra bottomBar 0/10/30, alta esattamente quanto la barra + espansione).</summary>
        public const string BOTTOM_BAR_SHADOW = SPRITES_FOLDER + "/TEX_BottomBarShadow.png";

        /// <summary>Campioni per lato di ogni pixel per l'anti-aliasing delle forme (4×4).</summary>
        private const int SUPERSAMPLE = 4;
        /// <summary>Piattaforma Android (Meta Quest 3).</summary>
        private const string ANDROID = "Android";

        /// <summary>
        /// Voce di menu: genera o aggiorna tutti gli sprite TEX_ dal tema.
        /// </summary>
        [MenuItem("SI81/UI/Build Sprites", priority = 12)]
        public static void BuildSpritesMenu()
        {
            Debug.Log("[SI81 UI] " + BuildAll(AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET)));
        }

        /// <summary>
        /// Genera o aggiorna tutti gli sprite e restituisce un riepilogo in italiano (usabile anche da CLI e da ThemeBuilder).
        /// </summary>
        /// <param name="theme">Tema SO_UITheme già generato.</param>
        /// <returns>Riepilogo delle operazioni.</returns>
        public static string BuildAll(UITheme theme)
        {
            if (theme == null) return "Sprite TEX_: interrotto, SO_UITheme mancante (eseguire SI81/UI/Build Theme).";
            UITheme.ShadowToken menuShadow = theme.GetShadow(ShadowRoles.MENU_PANEL);
            if (menuShadow == null) return "Sprite TEX_: interrotto, ombra \"menuPanel\" assente in SO_UITheme.";
            BaseBuilder.EnsureFolder(SPRITES_FOLDER);

            var report = new StringBuilder("Sprite TEX_: ");
            report.Append("TEX_PanelRounded ").Append(BuildRoundedSprite(PANEL_ROUNDED, theme.RadiusPanel)).Append(", ");
            report.Append("TEX_PanelShadow ").Append(BuildShadowSprite(PANEL_SHADOW, theme.RadiusPanel, menuShadow)).Append(", ");

            // Variante compatta per pannelli bassi (PANEL_Side): geometria dell'ombra sidePanel (il colore lo dà Image.color).
            UITheme.ShadowToken sideShadow = theme.GetShadow(ShadowRoles.SIDE_PANEL);
            float compactMin = theme.GetNumber(NumberTokens.SHADOW_COMPACT_MIN_HEIGHT);
            if (sideShadow == null || compactMin <= 0f)
                return report.Append("TEX_PanelShadowCompact interrotto (ombra \"sidePanel\" o size.shadow.compact-min-height assenti).").ToString();
            report.Append("TEX_PanelShadowCompact ").Append(BuildShadowSprite(PANEL_SHADOW_COMPACT, theme.RadiusPanel, sideShadow, compactMin)).Append(", ");

            // Pillola (radius.pill = completamente arrotondata): raggio = metà dell'altezza della bottom bar (28), così lo
            // sprite copre senza compressione la barra (56) e, ridotto in proporzione, la pillola hover delle voci (44).
            UITheme.ShadowToken barShadow = theme.GetShadow(ShadowRoles.BOTTOM_BAR);
            float barHeight = theme.GetNumber(NumberTokens.BOTTOM_BAR_HEIGHT);
            if (barShadow == null || barHeight <= 0f)
                return report.Append("TEX_PillRounded / TEX_BottomBarShadow interrotti (ombra \"bottomBar\" o size.bottom-bar.height assenti).").ToString();
            report.Append("TEX_PillRounded ").Append(BuildRoundedSprite(PILL_ROUNDED, GetPillRadius(theme))).Append(", ");
            report.Append("TEX_BottomBarShadow ").Append(BuildShadowSprite(BOTTOM_BAR_SHADOW, GetBottomBarShadowRadius(theme), barShadow, barHeight)).Append('.');
            return report.ToString();
        }

        /// <summary>
        /// Raggio della pillola: metà dell'altezza della bottom bar (radius.pill 999 = completamente arrotondato).
        /// </summary>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <returns>Raggio in unità Canvas (28).</returns>
        public static float GetPillRadius(UITheme theme)
        {
            return Mathf.Min(theme.RadiusPill, theme.GetNumber(NumberTokens.BOTTOM_BAR_HEIGHT) * 0.5f);
        }

        /// <summary>
        /// Raggio della forma dell'ombra della bottom bar: metà altezza meno 1 pixel di texture (26), perché la variante
        /// compatta richiede una forma alta almeno 2 raggi + 2 pixel; così la texture è alta quanto il rettangolo
        /// dell'ombra (56 + 2 × 48) e i bordi 9-slice non vengono compressi. Differenza invisibile dopo la sfocatura.
        /// </summary>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <returns>Raggio in unità Canvas.</returns>
        public static float GetBottomBarShadowRadius(UITheme theme)
        {
            return GetPillRadius(theme) - 1f / PanelSprites.SHADOW_PIXELS_PER_UNIT;
        }

        // =====================================================================
        // Generatori riusabili
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna uno sprite 9-slice bianco con angoli arrotondati (forma nell'alfa, colore dal componente).
        /// Densità <see cref="PanelSprites.ROUNDED_PIXELS_PER_UNIT"/> px per unità Canvas; bordo 9-slice = raggio.
        /// </summary>
        /// <param name="assetPath">Percorso del PNG (TEX_*.png in Sprites/).</param>
        /// <param name="radius">Raggio degli angoli in unità Canvas (es. radius.panel 24, radius.card 12).</param>
        /// <returns>"creato", "aggiornato" o "invariato".</returns>
        public static string BuildRoundedSprite(string assetPath, float radius)
        {
            int size = PanelSprites.GetRoundedTextureSize(radius, out int border);
            float radiusPx = radius * PanelSprites.ROUNDED_PIXELS_PER_UNIT;
            float[] alpha = RasterizeRoundedRect(size, size, new Rect(0f, 0f, size, size), radiusPx);
            string status = WritePng(assetPath, size, size, alpha);
            ApplyImportSettings(assetPath, PanelSprites.ROUNDED_PIXELS_PER_UNIT, new Vector4(border, border, border, border),
                size, TextureImporterFormat.ASTC_4x4);
            return status;
        }

        /// <summary>
        /// Crea o aggiorna uno sprite ombra 9-slice: forma arrotondata spostata dell'offset del token, sfocata con una
        /// gaussiana (σ = blur / 2) e bucata sotto la forma originale (come Figma: ombra non visibile dietro un riempimento
        /// semitrasparente). Bianco con alfa = intensità 0–1: il colore e l'alpha del token si danno con Image.color.
        /// Il RectTransform dell'Image va allargato di <see cref="PanelSprites.ShadowLayout.PaddingUnits"/> per lato.
        /// </summary>
        /// <param name="assetPath">Percorso del PNG (TEX_*.png in Sprites/).</param>
        /// <param name="radius">Raggio degli angoli della forma in unità Canvas.</param>
        /// <param name="shadow">Ombra del tema (offset, blur).</param>
        /// <returns>"creato", "aggiornato" o "invariato".</returns>
        /// <param name="minShapeHeight">0 = sprite normale; &gt; 0 = variante compatta per pannelli alti almeno questo valore
        /// (vedi <see cref="PanelSprites.GetShadowLayout(float, UITheme.ShadowToken, float)"/>).</param>
        public static string BuildShadowSprite(string assetPath, float radius, UITheme.ShadowToken shadow, float minShapeHeight = 0f)
        {
            PanelSprites.ShadowLayout layout = PanelSprites.GetShadowLayout(radius, shadow, minShapeHeight);
            float ppu = PanelSprites.SHADOW_PIXELS_PER_UNIT;
            int w = layout.Width, h = layout.Height;
            float radiusPx = radius * ppu;
            var shape = new Rect(layout.PaddingPx, layout.PaddingPx, w - 2f * layout.PaddingPx, h - 2f * layout.PaddingPx);

            // 1. Forma spostata (Y della texture verso l'alto, come l'offset del token), 2. sfocatura gaussiana separabile.
            Rect moved = shape;
            moved.position += shadow.Offset * ppu;
            float[] mask = RasterizeRoundedRect(w, h, moved, radiusPx);
            float[] blurred = GaussianBlur(mask, w, h, PanelSprites.BlurToSigma(shadow.Blur) * ppu);

            // 3. Bucatura sotto la forma originale, leggermente rientrata perché il bordo del pannello copra la transizione.
            float inset = PanelSprites.SHADOW_KNOCKOUT_INSET * ppu;
            Rect knock = new Rect(shape.x + inset, shape.y + inset, shape.width - 2f * inset, shape.height - 2f * inset);
            float[] hole = RasterizeRoundedRect(w, h, knock, Mathf.Max(0f, radiusPx - inset));
            for (int i = 0; i < blurred.Length; i++) blurred[i] *= 1f - hole[i];

            string status = WritePng(assetPath, w, h, blurred);
            ApplyImportSettings(assetPath, ppu, new Vector4(layout.Left, layout.Bottom, layout.Right, layout.Top),
                Mathf.Max(w, h), TextureImporterFormat.ASTC_6x6);
            return status;
        }

        // =====================================================================
        // Rasterizzazione
        // =====================================================================

        /// <summary>
        /// Copertura (0–1) di un rettangolo arrotondato per ogni pixel, riga 0 in basso, con SUPERSAMPLE×SUPERSAMPLE
        /// campioni per pixel (risultato identico su ogni macchina).
        /// </summary>
        private static float[] RasterizeRoundedRect(int width, int height, Rect shape, float radius)
        {
            var coverage = new float[width * height];
            float r = Mathf.Min(radius, Mathf.Min(shape.width, shape.height) * 0.5f);
            Vector2 center = shape.center;
            Vector2 half = shape.size * 0.5f - new Vector2(r, r);
            float step = 1f / SUPERSAMPLE;
            float perSample = 1f / (SUPERSAMPLE * SUPERSAMPLE);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < SUPERSAMPLE; sy++)
                    {
                        float py = y + (sy + 0.5f) * step;
                        for (int sx = 0; sx < SUPERSAMPLE; sx++)
                        {
                            float px = x + (sx + 0.5f) * step;
                            // Distanza con segno dal rettangolo arrotondato.
                            float qx = Mathf.Abs(px - center.x) - half.x;
                            float qy = Mathf.Abs(py - center.y) - half.y;
                            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                            float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                            if (distance <= 0f) inside++;
                        }
                    }
                    coverage[y * width + x] = inside * perSample;
                }
            }
            return coverage;
        }

        /// <summary>
        /// Sfocatura gaussiana separabile (orizzontale poi verticale), fuori dalla texture vale 0.
        /// </summary>
        private static float[] GaussianBlur(float[] source, int width, int height, float sigma)
        {
            if (sigma <= 0f) return (float[])source.Clone();
            int radius = Mathf.CeilToInt(sigma * 3f);
            var kernel = new float[radius * 2 + 1];
            float sum = 0f;
            for (int i = -radius; i <= radius; i++)
            {
                kernel[i + radius] = Mathf.Exp(-(i * i) / (2f * sigma * sigma));
                sum += kernel[i + radius];
            }
            for (int i = 0; i < kernel.Length; i++) kernel[i] /= sum;

            var temp = new float[source.Length];
            var result = new float[source.Length];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float acc = 0f;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sx = x + k;
                        if (sx >= 0 && sx < width) acc += source[y * width + sx] * kernel[k + radius];
                    }
                    temp[y * width + x] = acc;
                }
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float acc = 0f;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sy = y + k;
                        if (sy >= 0 && sy < height) acc += temp[sy * width + x] * kernel[k + radius];
                    }
                    result[y * width + x] = acc;
                }
            return result;
        }

        // =====================================================================
        // File e import
        // =====================================================================

        /// <summary>
        /// Scrive il PNG (RGB bianco, alfa dai valori) solo se il contenuto cambia: niente reimport inutili, GUID invariato.
        /// </summary>
        /// <returns>"creato", "aggiornato" o "invariato".</returns>
        private static string WritePng(string assetPath, int width, int height, float[] alpha)
        {
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha[i]) * 255f));

            byte[] png;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false);
                png = texture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            string fullPath = Path.GetFullPath(assetPath);
            bool exists = File.Exists(fullPath);
            if (exists && File.ReadAllBytes(fullPath).SequenceEqual(png)) return "invariato";
            File.WriteAllBytes(fullPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return exists ? "aggiornato" : "creato";
        }

        /// <summary>
        /// Impostazioni di import comuni degli sprite TEX_: Sprite singolo, mesh FullRect, bordi 9-slice, px per unità
        /// (rispetto a Canvas.referencePixelsPerUnit 100), alfa come trasparenza, Clamp, Bilinear, non compresso e senza
        /// mipmap (compressione ASTC e mipmap li applica l'atlante TEX_AtlasUI), niente lettura/scrittura. Reimporta solo se qualcosa cambia.
        /// </summary>
        private static void ApplyImportSettings(string assetPath, float pixelsPerCanvasUnit, Vector4 border, int maxSide, TextureImporterFormat androidFormat)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
            {
                Debug.LogError($"[SI81 UI] Sprite non importato come texture: {assetPath}.");
                return;
            }
            bool changed = false;
            changed |= Set(importer.textureType, TextureImporterType.Sprite, v => importer.textureType = v);
            changed |= Set(importer.spriteImportMode, SpriteImportMode.Single, v => importer.spriteImportMode = v);
            changed |= Set(importer.spritePixelsPerUnit, pixelsPerCanvasUnit * PanelSprites.CANVAS_REFERENCE_PIXELS_PER_UNIT, v => importer.spritePixelsPerUnit = v);
            changed |= Set(importer.spriteBorder, border, v => importer.spriteBorder = v);
            changed |= Set(importer.alphaSource, TextureImporterAlphaSource.FromInput, v => importer.alphaSource = v);
            changed |= Set(importer.alphaIsTransparency, true, v => importer.alphaIsTransparency = v);
            changed |= Set(importer.sRGBTexture, true, v => importer.sRGBTexture = v);
            changed |= Set(importer.wrapMode, TextureWrapMode.Clamp, v => importer.wrapMode = v);
            changed |= Set(importer.filterMode, FilterMode.Bilinear, v => importer.filterMode = v);
            // Originale non compresso e senza mipmap: compressione e mipmap li fa l'atlante TEX_AtlasUI.
            changed |= Set(importer.mipmapEnabled, false, v => importer.mipmapEnabled = v);
            changed |= Set(importer.isReadable, false, v => importer.isReadable = v);
            changed |= Set(importer.npotScale, TextureImporterNPOTScale.None, v => importer.npotScale = v);
            int maxSize = Mathf.NextPowerOfTwo(maxSide);
            changed |= Set(importer.maxTextureSize, maxSize, v => importer.maxTextureSize = v);
            changed |= Set(importer.textureCompression, TextureImporterCompression.Uncompressed, v => importer.textureCompression = v);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool spriteChanged = false;
            spriteChanged |= Set(settings.spriteMeshType, SpriteMeshType.FullRect, v => settings.spriteMeshType = v);
            spriteChanged |= Set(settings.spriteGenerateFallbackPhysicsShape, false, v => settings.spriteGenerateFallbackPhysicsShape = v);
            if (spriteChanged)
            {
                importer.SetTextureSettings(settings);
                changed = true;
            }

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings(ANDROID);
            bool androidChanged = false;
            androidChanged |= Set(android.overridden, false, v => android.overridden = v);
            if (androidChanged)
            {
                importer.SetPlatformTextureSettings(android);
                changed = true;
            }

            if (changed) importer.SaveAndReimport();
        }

        /// <summary>
        /// Assegna un valore solo se diverso da quello attuale.
        /// </summary>
        /// <returns>true se il valore è stato cambiato.</returns>
        private static bool Set<T>(T current, T wanted, System.Action<T> assign)
        {
            if (Equals(current, wanted)) return false;
            assign(wanted);
            return true;
        }
    }
}
