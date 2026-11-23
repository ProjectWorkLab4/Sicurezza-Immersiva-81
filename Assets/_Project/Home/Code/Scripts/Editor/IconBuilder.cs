using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder delle icone ICN_: converte gli SVG Material Symbols di Editor/Source/Icons~ in PNG 128×128
    /// bianchi (forma nel canale alfa) in Assets/_Project/Home/Art/Textures/Icons/. Sicuro da rieseguire: sovrascrive sul posto
    /// solo i PNG il cui contenuto cambia, mantiene .meta e GUID, non crea duplicati e non cancella nulla.
    /// </summary>
    public static class IconBuilder
    {
        /// <summary>Cartella degli SVG sorgente (il "~" finale la nasconde a Unity).</summary>
        public const string SOURCE_FOLDER = ThemeBuilder.ROOT + "/Code/Scripts/Editor/Source/Icons~";
        /// <summary>Cartella dei PNG generati.</summary>
        public const string ICONS_FOLDER = ThemeBuilder.ROOT + "/Art/Textures/Icons";
        /// <summary>Lato del PNG in pixel.</summary>
        public const int ICON_SIZE = 128;
        /// <summary>Campioni per lato di ogni pixel (8×8 = 64 campioni per pixel di anti-aliasing).</summary>
        private const int SUPERSAMPLE = 8;
        /// <summary>Copertura minima plausibile di un'icona (frazione di pixel non trasparenti).</summary>
        private const float MIN_COVERAGE = 0.02f;

        /// <summary>Nome file valido: ICN_ + PascalCase, variante opzionale come suffisso (es. ICN_Home_Filled).</summary>
        private static readonly Regex NAME_PATTERN = new Regex(@"^ICN_[A-Z][A-Za-z0-9]*(_[A-Z][A-Za-z0-9]*)?$");

        /// <summary>
        /// Opzioni di tessellazione in unità della scena SVG (48 unità = 128 px): curve approssimate
        /// con errore massimo ≈ 0,03 px, invisibile anche nei mipmap.
        /// </summary>
        private static readonly VectorUtils.TessellationOptions TESSELLATION = new VectorUtils.TessellationOptions
        {
            StepDistance = float.MaxValue,
            MaxCordDeviation = 0.01f,
            MaxTanAngleDeviation = 0.05f,
            SamplingStepSize = 0.01f
        };

        /// <summary>
        /// Voce di menu: genera o aggiorna tutte le icone ICN_.
        /// </summary>
        [MenuItem("SI81/UI/Build Icons", priority = 11)]
        public static void BuildIconsMenu()
        {
            Debug.Log("[SI81 UI] " + BuildAll());
        }

        /// <summary>
        /// Genera o aggiorna tutte le icone e restituisce un riepilogo in italiano (usabile anche da CLI
        /// e da <see cref="ThemeBuilder.BuildAll"/>).
        /// </summary>
        /// <returns>Riepilogo delle operazioni.</returns>
        public static string BuildAll()
        {
            string sourceFull = Path.GetFullPath(SOURCE_FOLDER);
            if (!Directory.Exists(sourceFull))
            {
                Debug.LogError($"[SI81 UI] Cartella sorgente delle icone mancante: {SOURCE_FOLDER}.");
                return "Icone ICN_: interrotto, sorgenti SVG mancanti.";
            }
            BaseBuilder.EnsureFolder(ICONS_FOLDER);

            // Elenco ordinato: stesso ordine (e stesso risultato) su ogni macchina.
            string[] svgFiles = Directory.GetFiles(sourceFull, "*.svg").OrderBy(f => f, StringComparer.Ordinal).ToArray();
            int created = 0, updated = 0, unchanged = 0, failed = 0;
            var writtenPaths = new List<string>();
            var expectedNames = new HashSet<string>();

            foreach (string svgFile in svgFiles)
            {
                string name = Path.GetFileNameWithoutExtension(svgFile);
                if (!NAME_PATTERN.IsMatch(name))
                {
                    Debug.LogError($"[SI81 UI] Nome icona non valido: \"{name}.svg\" (atteso ICN_NomePascalCase o ICN_Nome_Variante). File saltato.");
                    failed++;
                    continue;
                }
                expectedNames.Add(name);

                byte[] png = RasterizeToPng(svgFile, name);
                if (png == null)
                {
                    failed++;
                    continue;
                }

                // Scrive solo se il contenuto cambia: niente reimport inutili, GUID e .meta restano.
                string assetPath = ICONS_FOLDER + "/" + name + ".png";
                string fullPath = Path.GetFullPath(assetPath);
                bool exists = File.Exists(fullPath);
                if (exists && File.ReadAllBytes(fullPath).SequenceEqual(png))
                {
                    unchanged++;
                    continue;
                }
                File.WriteAllBytes(fullPath, png);
                writtenPaths.Add(assetPath);
                if (exists) updated++; else created++;
            }

            // Import dei PNG scritti (le impostazioni arrivano da IconImportSettings).
            if (writtenPaths.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string path in writtenPaths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }

            // Controllo impostazioni di import di tutte le icone (corregge eventuali modifiche manuali).
            int settingsFixed = 0;
            foreach (string name in expectedNames)
            {
                string assetPath = ICONS_FOLDER + "/" + name + ".png";
                if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
                {
                    Debug.LogError($"[SI81 UI] Icona non importata come texture: {assetPath}.");
                    failed++;
                    continue;
                }
                if (IconImportSettings.Apply(importer))
                {
                    importer.SaveAndReimport();
                    settingsFixed++;
                }
            }

            // PNG ICN_ senza SVG sorgente: solo segnalati, mai cancellati.
            string[] orphans = Directory.GetFiles(Path.GetFullPath(ICONS_FOLDER), "ICN_*.png")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !expectedNames.Contains(n))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
            if (orphans.Length > 0)
                Debug.LogWarning($"[SI81 UI] Icone senza SVG sorgente in {SOURCE_FOLDER} (non cancellate): {string.Join(", ", orphans)}.");

            var report = new StringBuilder();
            report.Append($"Icone ICN_: {svgFiles.Length} SVG → creati {created}, aggiornati {updated}, invariati {unchanged}");
            if (settingsFixed > 0) report.Append($", impostazioni di import corrette {settingsFixed}");
            if (failed > 0) report.Append($", errori {failed}");
            if (orphans.Length > 0) report.Append($", PNG senza sorgente {orphans.Length}");
            report.Append('.');
            return report.ToString();
        }

        // =====================================================================
        // Rasterizzazione
        // =====================================================================

        /// <summary>
        /// Legge un SVG, lo tessella con l'API Vector Graphics di Unity e lo rasterizza su CPU in un PNG
        /// ICON_SIZE×ICON_SIZE: RGB bianco, alfa = copertura della forma (anti-aliasing SUPERSAMPLE×SUPERSAMPLE).
        /// L'intero viewBox è mappato sul quadrato, quindi il margine interno di Material resta.
        /// </summary>
        /// <param name="svgFile">Percorso completo del file SVG.</param>
        /// <param name="name">Nome dell'icona (per i messaggi).</param>
        /// <returns>Byte del PNG, oppure null in caso di errore (già segnalato in Console).</returns>
        private static byte[] RasterizeToPng(string svgFile, string name)
        {
            SVGParser.SceneInfo scene;
            try
            {
                using (var reader = new StringReader(File.ReadAllText(svgFile, Encoding.UTF8)))
                    scene = SVGParser.ImportSVG(reader, ViewportOptions.PreserveViewport, 0f, 1f, 0, 0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SI81 UI] SVG non leggibile: {name}.svg ({e.Message}).");
                return null;
            }

            Rect viewport = scene.SceneViewport;
            if (viewport.width <= 0f || viewport.height <= 0f)
            {
                Debug.LogError($"[SI81 UI] SVG senza viewBox/dimensioni valide: {name}.svg.");
                return null;
            }

            List<VectorUtils.Geometry> geometries = VectorUtils.TessellateScene(scene.Scene, TESSELLATION, scene.NodeOpacity);
            float[] coverage = RasterizeCoverage(geometries, viewport);

            // Pixel: Texture2D parte dal basso, l'SVG dall'alto → riga capovolta.
            var pixels = new Color32[ICON_SIZE * ICON_SIZE];
            int nonZero = 0;
            for (int y = 0; y < ICON_SIZE; y++)
            {
                for (int x = 0; x < ICON_SIZE; x++)
                {
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(coverage[y * ICON_SIZE + x]) * 255f);
                    if (alpha > 0) nonZero++;
                    pixels[(ICON_SIZE - 1 - y) * ICON_SIZE + x] = new Color32(255, 255, 255, alpha);
                }
            }

            float fraction = nonZero / (float)(ICON_SIZE * ICON_SIZE);
            if (fraction < MIN_COVERAGE)
            {
                Debug.LogError($"[SI81 UI] Icona vuota o quasi vuota dopo la rasterizzazione: {name} ({fraction:P1} pixel visibili).");
                return null;
            }

            var texture = new Texture2D(ICON_SIZE, ICON_SIZE, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false);
                return texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Rasterizza i triangoli tessellati su una griglia di campioni e restituisce la copertura (0–1) di
        /// ogni pixel, riga 0 in alto. Un campione coperto da più triangoli conta una sola volta, quindi i lati
        /// condivisi non creano cuciture; il risultato non dipende dalla GPU (identico su ogni macchina).
        /// </summary>
        /// <param name="geometries">Geometrie tessellate dalla scena SVG.</param>
        /// <param name="viewport">viewBox della scena, mappato sull'intero quadrato.</param>
        /// <returns>Copertura per pixel, ICON_SIZE×ICON_SIZE.</returns>
        private static float[] RasterizeCoverage(List<VectorUtils.Geometry> geometries, Rect viewport)
        {
            int grid = ICON_SIZE * SUPERSAMPLE;
            var samples = new bool[grid * grid];
            float scaleX = grid / viewport.width;
            float scaleY = grid / viewport.height;

            foreach (VectorUtils.Geometry geometry in geometries)
            {
                if (geometry.Color.a <= 0f || geometry.Vertices == null || geometry.Indices == null) continue;

                // Vertici nello spazio dei campioni (asse Y verso il basso, come nell'SVG).
                var points = new Vector2[geometry.Vertices.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    Vector2 world = geometry.WorldTransform.MultiplyPoint(geometry.Vertices[i]);
                    points[i] = new Vector2((world.x - viewport.x) * scaleX, (world.y - viewport.y) * scaleY);
                }

                for (int t = 0; t + 2 < geometry.Indices.Length; t += 3)
                    FillTriangle(samples, grid, points[geometry.Indices[t]], points[geometry.Indices[t + 1]], points[geometry.Indices[t + 2]]);
            }

            // Media dei campioni di ogni pixel.
            var coverage = new float[ICON_SIZE * ICON_SIZE];
            float samplesPerPixel = SUPERSAMPLE * SUPERSAMPLE;
            for (int py = 0; py < ICON_SIZE; py++)
            {
                for (int px = 0; px < ICON_SIZE; px++)
                {
                    int count = 0;
                    for (int sy = 0; sy < SUPERSAMPLE; sy++)
                    {
                        int row = (py * SUPERSAMPLE + sy) * grid + px * SUPERSAMPLE;
                        for (int sx = 0; sx < SUPERSAMPLE; sx++)
                            if (samples[row + sx]) count++;
                    }
                    coverage[py * ICON_SIZE + px] = count / samplesPerPixel;
                }
            }
            return coverage;
        }

        /// <summary>
        /// Segna come coperti i campioni (centro della cella) che cadono dentro il triangolo, lati inclusi.
        /// Per ogni riga di campioni calcola l'intervallo orizzontale coperto (scanline): il costo dipende
        /// dall'area del triangolo, non dal suo rettangolo di ingombro (la tessellazione crea molti triangoli sottili).
        /// </summary>
        private static void FillTriangle(bool[] samples, int grid, Vector2 a, Vector2 b, Vector2 c)
        {
            // Triangoli degeneri: nessuna area, nessun campione.
            float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (Mathf.Abs(area) < 1e-9f) return;

            float minYf = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            float maxYf = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            // Righe il cui centro (y + 0,5) cade in [minY, maxY].
            int minY = Mathf.Max(0, Mathf.CeilToInt(minYf - 0.5f));
            int maxY = Mathf.Min(grid - 1, Mathf.FloorToInt(maxYf - 0.5f));

            for (int y = minY; y <= maxY; y++)
            {
                float centerY = y + 0.5f;
                float left = float.MaxValue, right = float.MinValue;
                Intersect(a, b, centerY, ref left, ref right);
                Intersect(b, c, centerY, ref left, ref right);
                Intersect(c, a, centerY, ref left, ref right);
                if (left > right) continue;

                // Colonne il cui centro (x + 0,5) cade in [left, right].
                int startX = Mathf.Max(0, Mathf.CeilToInt(left - 0.5f));
                int endX = Mathf.Min(grid - 1, Mathf.FloorToInt(right - 0.5f));
                int row = y * grid;
                for (int x = startX; x <= endX; x++) samples[row + x] = true;
            }
        }

        /// <summary>
        /// Interseca il lato p→q con la retta orizzontale y = centerY e allarga l'intervallo [left, right].
        /// Un lato orizzontale sulla retta contribuisce con entrambi gli estremi.
        /// </summary>
        private static void Intersect(Vector2 p, Vector2 q, float centerY, ref float left, ref float right)
        {
            float lowY = Mathf.Min(p.y, q.y), highY = Mathf.Max(p.y, q.y);
            if (centerY < lowY || centerY > highY) return;
            if (highY - lowY < 1e-9f)
            {
                left = Mathf.Min(left, Mathf.Min(p.x, q.x));
                right = Mathf.Max(right, Mathf.Max(p.x, q.x));
                return;
            }
            float x = p.x + (centerY - p.y) * (q.x - p.x) / (q.y - p.y);
            if (x < left) left = x;
            if (x > right) right = x;
        }

        // =====================================================================
        // Utilità
        // =====================================================================
    }
}
