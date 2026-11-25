using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Genera i font asset TMP (SDF statici) FNT_JaldiRegular_SDF e FNT_JaldiBold_SDF dai TTF in Fonts/.
    /// Sicuro da rieseguire: se l'asset è già aggiornato non lo tocca; se va rigenerato aggiorna sul posto font, atlas e
    /// materiale, mantenendo il GUID del file e i fileID locali dei sotto-asset (i riferimenti non si rompono).
    /// Impostazioni PROVVISORIE (guideline §3.3 non disponibile): campionamento 90 pt, padding 9, atlas 1024.
    /// </summary>
    public static class FontBuilder
    {
        /// <summary>Cartella dei font della UI comune.</summary>
        public const string FONTS_FOLDER = ThemeBuilder.ROOT + "/UI/Fonts";
        /// <summary>TTF Jaldi Regular (OFL).</summary>
        public const string REGULAR_TTF = FONTS_FOLDER + "/FNT_JaldiRegular.ttf";
        /// <summary>TTF Jaldi Bold (OFL).</summary>
        public const string BOLD_TTF = FONTS_FOLDER + "/FNT_JaldiBold.ttf";
        /// <summary>Font asset TMP Regular.</summary>
        public const string REGULAR_SDF = FONTS_FOLDER + "/FNT_JaldiRegular_SDF.asset";
        /// <summary>Font asset TMP Bold.</summary>
        public const string BOLD_SDF = FONTS_FOLDER + "/FNT_JaldiBold_SDF.asset";

        /// <summary>Dimensione di campionamento in punti. PROVVISORIO.</summary>
        public const int SAMPLING_POINT_SIZE = 90;
        /// <summary>Padding attorno ai glifi nell'atlas. PROVVISORIO.</summary>
        public const int ATLAS_PADDING = 9;
        /// <summary>Lato dell'atlas iniziale. PROVVISORIO.</summary>
        public const int ATLAS_SIZE = 1024;
        /// <summary>Lato massimo dell'atlas se i caratteri non entrano in quello iniziale.</summary>
        public const int MAX_ATLAS_SIZE = 2048;
        /// <summary>Modalità di rendering: SDF con antialiasing.</summary>
        public const GlyphRenderMode RENDER_MODE = GlyphRenderMode.SDFAA;

        /// <summary>Caratteri extra oltre a ASCII e Latin-1 (simboli usati nei testi Figma).</summary>
        public const string EXTRA_CHARACTERS = "·→−°–—’‘“”…€✓★◐≤≈±";

        /// <summary>Risultato della generazione di un font asset.</summary>
        public class FontResult
        {
            /// <summary>Percorso dell'asset.</summary>
            public string AssetPath;
            /// <summary>Font asset generato o esistente.</summary>
            public TMP_FontAsset Asset;
            /// <summary>Lato dell'atlas usato.</summary>
            public int AtlasSize;
            /// <summary>Caratteri richiesti ma assenti nel font sorgente.</summary>
            public string MissingInFont = string.Empty;
            /// <summary>Esito: "creato", "rigenerato" o "invariato".</summary>
            public string Status;
        }

        /// <summary>
        /// Voce di menu: genera entrambi i font asset.
        /// </summary>
        [MenuItem("SI81/UI/Build Fonts", priority = 11)]
        public static void BuildFontsMenu()
        {
            if (!TmpEssentials.EnsureImported(BuildFontsMenu)) return;
            foreach (FontResult r in BuildAll()) Debug.Log(Describe(r));
        }

        /// <summary>
        /// Genera (o verifica) i due font asset Jaldi.
        /// </summary>
        /// <param name="force">true per rigenerare anche se gli asset sono già aggiornati (atlas e materiale aggiornati sul posto).</param>
        /// <returns>Un risultato per font.</returns>
        public static List<FontResult> BuildAll(bool force = false)
        {
            return new List<FontResult>
            {
                Build(REGULAR_TTF, REGULAR_SDF, force),
                Build(BOLD_TTF, BOLD_SDF, force)
            };
        }

        /// <summary>
        /// Descrizione breve in italiano di un risultato, per la Console.
        /// </summary>
        /// <param name="r">Risultato.</param>
        /// <returns>Testo descrittivo.</returns>
        public static string Describe(FontResult r)
        {
            string missing = string.IsNullOrEmpty(r.MissingInFont) ? "nessuno" : string.Join(" ", r.MissingInFont.Select(c => $"{c}(U+{(int)c:X4})"));
            return $"[SI81 Fonts] {Path.GetFileName(r.AssetPath)}: {r.Status}, atlas {r.AtlasSize}x{r.AtlasSize}, " +
                   $"{(r.Asset != null ? r.Asset.characterTable.Count : 0)} caratteri. Glifi assenti nel font: {missing}.";
        }

        /// <summary>
        /// Insieme di caratteri richiesto: ASCII 32–126, Latin-1 160–255 e <see cref="EXTRA_CHARACTERS"/>.
        /// </summary>
        /// <returns>Stringa con ogni carattere una sola volta, in ordine.</returns>
        public static string GetCharacterSet()
        {
            var sb = new StringBuilder();
            for (int c = 32; c <= 126; c++) sb.Append((char)c);
            for (int c = 160; c <= 255; c++) sb.Append((char)c);
            sb.Append(EXTRA_CHARACTERS);
            return new string(sb.ToString().Distinct().ToArray());
        }

        /// <summary>
        /// Genera o aggiorna un singolo font asset statico.
        /// </summary>
        /// <param name="ttfPath">Percorso del TTF sorgente.</param>
        /// <param name="assetPath">Percorso del font asset TMP.</param>
        /// <param name="force">true per rigenerare anche se l'asset è già aggiornato.</param>
        /// <returns>Risultato della generazione.</returns>
        public static FontResult Build(string ttfPath, string assetPath, bool force = false)
        {
            var result = new FontResult { AssetPath = assetPath };
            Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"[SI81 Fonts] TTF non trovato: {ttfPath}.");
                result.Status = "errore";
                return result;
            }

            // 1. Quali caratteri esistono davvero nel font sorgente.
            string requested = GetCharacterSet();
            string present = FilterPresent(font, requested, out string missing);
            result.MissingInFont = missing;

            // 2. Se l'asset esiste ed è già aggiornato non si tocca (seconda esecuzione = nessuna modifica).
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null && !force && IsUpToDate(existing, present))
            {
                result.Asset = existing;
                result.AtlasSize = existing.atlasWidth;
                result.Status = "invariato";
                return result;
            }

            // 3. Genera in memoria con atlas 1024, poi 2048 se i glifi non entrano.
            TMP_FontAsset fresh = null;
            int size = ATLAS_SIZE;
            while (size <= MAX_ATLAS_SIZE)
            {
                fresh = CreateStatic(font, present, size, out bool fits);
                if (fits) break;
                Object.DestroyImmediate(fresh.atlasTextures[0]);
                Object.DestroyImmediate(fresh.material);
                Object.DestroyImmediate(fresh);
                fresh = null;
                size *= 2;
            }
            if (fresh == null)
            {
                Debug.LogError($"[SI81 Fonts] I caratteri non entrano in un atlas {MAX_ATLAS_SIZE}x{MAX_ATLAS_SIZE}: {assetPath}.");
                result.Status = "errore";
                return result;
            }
            result.AtlasSize = size;

            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            fresh.atlasTextures[0].name = baseName + " Atlas";
            fresh.material.name = baseName + " Material";

            if (existing == null)
            {
                // Nuovo asset: font + atlas + materiale come sotto-asset (come fa il Font Asset Creator di TMP).
                AssetDatabase.CreateAsset(fresh, assetPath);
                AssetDatabase.AddObjectToAsset(fresh.atlasTextures[0], fresh);
                AssetDatabase.AddObjectToAsset(fresh.material, fresh);
                result.Asset = fresh;
                result.Status = "creato";
            }
            else
            {
                UpdateInPlace(existing, fresh, assetPath, baseName);
                result.Asset = existing;
                result.Status = "rigenerato";
            }

            EditorUtility.SetDirty(result.Asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            result.Asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            return result;
        }

        /// <summary>
        /// Copia i dati di un font asset generato in memoria dentro l'asset esistente SENZA ricreare i sotto-asset:
        /// l'atlas Texture2D e il materiale esistenti ricevono i nuovi dati e impostazioni, così GUID e fileID locali
        /// restano identici e i riferimenti (prefab, materiali, scene) non si rompono. Alla fine distrugge l'asset temporaneo.
        /// </summary>
        /// <param name="existing">Font asset salvato su disco da aggiornare.</param>
        /// <param name="fresh">Font asset appena generato in memoria (non salvato).</param>
        /// <param name="assetPath">Percorso del font asset.</param>
        /// <param name="baseName">Nome base (es. FNT_JaldiRegular_SDF) per atlas e materiale.</param>
        private static void UpdateInPlace(TMP_FontAsset existing, TMP_FontAsset fresh, string assetPath, string baseName)
        {
            Texture2D newAtlas = fresh.atlasTextures[0];
            Material newMaterial = fresh.material;

            // 1. Sotto-asset da riusare: preferibilmente quelli già referenziati dal font, altrimenti i primi trovati nel file.
            Texture2D oldAtlas = existing.atlasTextures != null && existing.atlasTextures.Length > 0 && IsSubAssetOf(existing.atlasTextures[0], assetPath)
                ? existing.atlasTextures[0] : null;
            Material oldMaterial = IsSubAssetOf(existing.material, assetPath) ? existing.material : null;
            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (Object sub in subAssets)
            {
                if (oldAtlas == null && sub is Texture2D t) oldAtlas = t;
                else if (oldMaterial == null && sub is Material m) oldMaterial = m;
            }

            // Eventuali sotto-asset in più (residui di vecchie versioni del builder) vengono rimossi.
            foreach (Object sub in subAssets)
            {
                if ((sub is Texture2D && sub != oldAtlas) || (sub is Material && sub != oldMaterial))
                {
                    AssetDatabase.RemoveObjectFromAsset(sub);
                    Object.DestroyImmediate(sub, true);
                }
            }

            // 2. Atlas: dati e impostazioni copiati nella texture esistente (stesso oggetto, stesso fileID).
            bool adoptedAtlas = false;
            if (oldAtlas != null)
            {
                EditorUtility.CopySerialized(newAtlas, oldAtlas);
                oldAtlas.name = baseName + " Atlas";
                if (oldAtlas.width != newAtlas.width || oldAtlas.height != newAtlas.height)
                    Debug.LogError($"[SI81 Fonts] Copia dell'atlas non riuscita in {assetPath}: {oldAtlas.width}x{oldAtlas.height} invece di {newAtlas.width}x{newAtlas.height}.");
            }
            else
            {
                // Nessun atlas da riusare (file incompleto): si aggiunge quello nuovo.
                AssetDatabase.AddObjectToAsset(newAtlas, existing);
                oldAtlas = newAtlas;
                adoptedAtlas = true;
            }

            // 3. Materiale: proprietà copiate nel materiale esistente, texture riportata sull'atlas esistente.
            bool adoptedMaterial = false;
            if (oldMaterial != null)
            {
                EditorUtility.CopySerialized(newMaterial, oldMaterial);
                oldMaterial.name = baseName + " Material";
                foreach (int id in oldMaterial.GetTexturePropertyNameIDs())
                    if (oldMaterial.GetTexture(id) == newAtlas) oldMaterial.SetTexture(id, oldAtlas);
                oldMaterial.SetTexture(ShaderUtilities.ID_MainTex, oldAtlas);
            }
            else
            {
                AssetDatabase.AddObjectToAsset(newMaterial, existing);
                newMaterial.SetTexture(ShaderUtilities.ID_MainTex, oldAtlas);
                oldMaterial = newMaterial;
                adoptedMaterial = true;
            }

            // 4. Font asset: dati copiati, poi riferimenti riportati sui sotto-asset esistenti.
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = baseName;
            existing.atlasTextures = new[] { oldAtlas };
            existing.material = oldMaterial;
            existing.ReadFontAssetDefinition();
            EditorUtility.SetDirty(oldAtlas);
            EditorUtility.SetDirty(oldMaterial);

            // 5. Pulizia dell'asset temporaneo. TMP_FontAsset.OnDestroy distrugge atlas e materiale: si staccano prima.
            fresh.atlasTextures = new Texture2D[0];
            fresh.material = null;
            Object.DestroyImmediate(fresh);
            if (!adoptedAtlas) Object.DestroyImmediate(newAtlas);
            if (!adoptedMaterial) Object.DestroyImmediate(newMaterial);
        }

        /// <summary>
        /// Indica se un oggetto è salvato dentro il file dell'asset dato.
        /// </summary>
        /// <param name="obj">Oggetto da controllare (può essere null).</param>
        /// <param name="assetPath">Percorso del file asset.</param>
        /// <returns>true se l'oggetto è persistente e appartiene a quel file.</returns>
        private static bool IsSubAssetOf(Object obj, string assetPath)
        {
            return obj != null && EditorUtility.IsPersistent(obj) && AssetDatabase.GetAssetPath(obj) == assetPath;
        }

        /// <summary>
        /// Tiene solo i caratteri presenti nel font sorgente.
        /// </summary>
        /// <param name="font">Font sorgente.</param>
        /// <param name="requested">Caratteri richiesti.</param>
        /// <param name="missing">Caratteri richiesti ma assenti nel font.</param>
        /// <returns>Caratteri presenti.</returns>
        private static string FilterPresent(Font font, string requested, out string missing)
        {
            var present = new StringBuilder();
            var absent = new StringBuilder();
            FontEngine.LoadFontFace(font, SAMPLING_POINT_SIZE);
            foreach (char c in requested)
            {
                // Lo spazio non ha contorno ma ha un glifo: TryGetGlyphIndex lo trova comunque.
                if (FontEngine.TryGetGlyphIndex(c, out uint index) && (index != 0 || c == ' ')) present.Append(c);
                else absent.Append(c);
            }
            missing = absent.ToString();
            return present.ToString();
        }

        /// <summary>
        /// Crea in memoria un font asset con i caratteri dati e lo rende statico.
        /// </summary>
        /// <param name="font">Font sorgente.</param>
        /// <param name="characters">Caratteri presenti da inserire.</param>
        /// <param name="size">Lato dell'atlas.</param>
        /// <param name="fits">false se qualche glifo non è entrato nell'atlas.</param>
        /// <returns>Il font asset (non ancora salvato).</returns>
        private static TMP_FontAsset CreateStatic(Font font, string characters, int size, out bool fits)
        {
            // Atlas singolo (multi-atlas disattivato) così si capisce se i glifi non entrano.
            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, SAMPLING_POINT_SIZE, ATLAS_PADDING, RENDER_MODE,
                size, size, AtlasPopulationMode.Dynamic, false);
            fa.TryAddCharacters(characters, out string notAdded);
            fits = string.IsNullOrEmpty(notAdded);

            // Impostazioni di creazione visibili nell'Inspector TMP (utile per rigenerare a mano).
            FontAssetCreationSettings cs = fa.creationSettings;
            cs.sourceFontFileName = font.name;
            cs.sourceFontFileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font));
            cs.pointSize = SAMPLING_POINT_SIZE;
            cs.pointSizeSamplingMode = 1;
            cs.padding = ATLAS_PADDING;
            cs.paddingMode = 2;
            cs.packingMode = 0;
            cs.atlasWidth = size;
            cs.atlasHeight = size;
            cs.characterSetSelectionMode = 7; // Caratteri personalizzati
            cs.characterSequence = characters;
            cs.renderMode = (int)RENDER_MODE;
            cs.includeFontFeatures = false;
            fa.creationSettings = cs;

            // Statico: nessuna generazione di glifi a runtime su Quest 3.
            // (clearDynamicDataOnBuild non serve: TMP lo applica solo agli asset dinamici.)
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            return fa;
        }

        /// <summary>
        /// Controlla se un font asset esistente ha già impostazioni e caratteri richiesti.
        /// </summary>
        /// <param name="fa">Font asset esistente.</param>
        /// <param name="present">Caratteri che il font deve contenere.</param>
        /// <returns>true se non serve rigenerarlo.</returns>
        private static bool IsUpToDate(TMP_FontAsset fa, string present)
        {
            if (fa.atlasPopulationMode != AtlasPopulationMode.Static) return false;
            if (fa.atlasPadding != ATLAS_PADDING || fa.atlasRenderMode != RENDER_MODE) return false;
            if ((int)fa.faceInfo.pointSize != SAMPLING_POINT_SIZE) return false;
            if (fa.atlasWidth != ATLAS_SIZE && fa.atlasWidth != MAX_ATLAS_SIZE) return false;
            if (fa.atlasTextures == null || fa.atlasTextures.Length != 1 || fa.atlasTextures[0] == null) return false;
            if (fa.material == null) return false;
            var have = new HashSet<uint>(fa.characterTable.Select(c => c.unicode));
            var want = new HashSet<uint>(present.Select(c => (uint)c));
            return have.SetEquals(want);
        }
    }
}
