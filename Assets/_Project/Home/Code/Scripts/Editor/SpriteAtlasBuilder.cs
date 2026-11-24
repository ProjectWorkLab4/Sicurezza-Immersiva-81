using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder dello Sprite Atlas della UI comune: <c>TEX_AtlasUI.spriteatlasv2</c> (Sprite Atlas V2, integrato in Unity, nessun
    /// pacchetto) con TUTTI gli sprite TEX_ (cartella Sprites/) e ICN_ (cartella Icons/): pannelli, ombre, pillole e icone
    /// finiscono nella stessa texture, quindi ombra + vetro + icone di una schermata si disegnano senza cambi di materiale
    /// (meno draw call su Quest 3). Nomenclatura.md non ha un prefisso per gli atlanti: si usa TEX_ (è una texture), nome
    /// "TEX_AtlasUI" (scelta da confermare). Aggiungere sprite alle due cartelle basta: l'atlante le include da sole.
    /// Sicuro da rieseguire: il file viene riscritto solo se il contenuto cambia (GUID invariato, nessun duplicato).
    /// Incluso nella build (<c>includeInBuild</c>) e usato anche in Play Mode (modalità Sprite Packer = Sprite Atlas V2).
    /// Eseguito da SI81/UI/Build Theme (dopo icone e sprite) o da SI81/UI/Build Atlas.
    /// </summary>
    public static class SpriteAtlasBuilder
    {
        /// <summary>Percorso dell'atlante.</summary>
        public const string ATLAS_PATH = SpriteBuilder.SPRITES_FOLDER + "/TEX_AtlasUI.spriteatlasv2";
        /// <summary>Lato massimo della texture dell'atlante (le dimensioni reali dipendono dagli sprite).</summary>
        private const int MAX_TEXTURE_SIZE = 2048;
        /// <summary>Spazio tra gli sprite nell'atlante, in pixel (evita lo "sbavare" dei mipmap tra sprite vicini).</summary>
        private const int PADDING = 8;
        /// <summary>Piattaforma Android (Meta Quest 3).</summary>
        private const string ANDROID = "Android";
        /// <summary>Nome della piattaforma predefinita nelle impostazioni di texture.</summary>
        private const string DEFAULT_PLATFORM = "DefaultTexturePlatform";

        /// <summary>
        /// Voce di menu: crea o aggiorna l'atlante degli sprite della UI comune.
        /// </summary>
        [MenuItem("SI81/UI/Build Atlas", priority = 13)]
        public static void BuildAtlasMenu()
        {
            Debug.Log("[SI81 UI] " + BuildAll());
        }

        /// <summary>
        /// Crea o aggiorna l'atlante e imposta la modalità Sprite Packer su Sprite Atlas V2 (serve anche in Play Mode).
        /// </summary>
        /// <returns>Riepilogo in italiano.</returns>
        public static string BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Atlante TEX_AtlasUI: non eseguibile in Play Mode.";
            var spritesFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(SpriteBuilder.SPRITES_FOLDER);
            var iconsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(IconBuilder.ICONS_FOLDER);
            if (spritesFolder == null || iconsFolder == null)
                return "Atlante TEX_AtlasUI: interrotto, cartelle Sprites/ o Icons/ mancanti (eseguire SI81/UI/Build Theme).";

            // 1. File dell'atlante con le due cartelle come contenuto (le impostazioni stanno nell'importer, vedi 2).
            string status;
            if (!File.Exists(Path.GetFullPath(ATLAS_PATH)))
            {
                var asset = new SpriteAtlasAsset();
                asset.Add(new Object[] { spritesFolder, iconsFolder });
                SpriteAtlasAsset.Save(asset, ATLAS_PATH);
                AssetDatabase.ImportAsset(ATLAS_PATH, ImportAssetOptions.ForceUpdate);
                status = "creato";
            }
            else
            {
                SpriteAtlasAsset asset = SpriteAtlasAsset.Load(ATLAS_PATH);
                var missing = new List<Object>();
                var imported = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(ATLAS_PATH);
                var packables = new List<Object>(imported != null ? imported.GetPackables() : new Object[0]);
                if (!packables.Contains(spritesFolder)) missing.Add(spritesFolder);
                if (!packables.Contains(iconsFolder)) missing.Add(iconsFolder);
                if (missing.Count > 0)
                {
                    asset.Add(missing.ToArray());
                    SpriteAtlasAsset.Save(asset, ATLAS_PATH);
                    AssetDatabase.ImportAsset(ATLAS_PATH, ImportAssetOptions.ForceUpdate);
                    status = "aggiornato (cartelle)";
                }
                else status = "invariato";
            }

            // 2. Impostazioni (in Unity 6 vivono nell'importer, nel .meta): riscritte solo se cambiano.
            if (!(AssetImporter.GetAtPath(ATLAS_PATH) is SpriteAtlasImporter importer))
                return $"Atlante TEX_AtlasUI {status}, ma l'importer non è un SpriteAtlasImporter: impostazioni non applicate.";
            if (ApplySettings(importer)) status += ", impostazioni aggiornate";

            // 3. Sprite Packer: gli atlanti V2 vengono impacchettati anche in Play Mode (altrimenti solo in build).
            string mode = EnsureSpritePackerMode();
            return $"Atlante TEX_AtlasUI {status} ({ATLAS_PATH}): cartelle Sprites/ e Icons/, incluso nella build. {mode}";
        }

        /// <summary>
        /// Imposta sull'importer: incluso nella build, nessuna rotazione né impacchettamento stretto (gli sprite 9-slice e
        /// le icone UI restano rettangolari), mipmap, Android ASTC 4×4. Salva e reimporta solo se qualcosa cambia.
        /// </summary>
        /// <param name="importer">Importer dell'atlante.</param>
        /// <returns>true se ha cambiato qualcosa.</returns>
        private static bool ApplySettings(SpriteAtlasImporter importer)
        {
            bool changed = false;

            if (!importer.includeInBuild)
            {
                importer.includeInBuild = true;
                changed = true;
            }

            SpriteAtlasPackingSettings packing = importer.packingSettings;
            if (packing.blockOffset != 1 || packing.padding != PADDING || packing.enableRotation || packing.enableTightPacking || !packing.enableAlphaDilation)
            {
                packing.blockOffset = 1;
                packing.padding = PADDING;
                packing.enableRotation = false;
                packing.enableTightPacking = false;
                packing.enableAlphaDilation = true;
                importer.packingSettings = packing;
                changed = true;
            }

            SpriteAtlasTextureSettings texture = importer.textureSettings;
            if (texture.readable || !texture.generateMipMaps || !texture.sRGB || texture.filterMode != FilterMode.Bilinear || texture.anisoLevel != 1)
            {
                texture.readable = false;
                texture.generateMipMaps = true;
                texture.sRGB = true;
                texture.filterMode = FilterMode.Bilinear;
                texture.anisoLevel = 1;
                importer.textureSettings = texture;
                changed = true;
            }

            changed |= ApplyPlatform(importer, DEFAULT_PLATFORM, false, TextureImporterFormat.Automatic);
            changed |= ApplyPlatform(importer, ANDROID, true, TextureImporterFormat.ASTC_4x4);

            if (changed) importer.SaveAndReimport();
            return changed;
        }

        /// <summary>
        /// Imposta le impostazioni di texture di una piattaforma se differiscono da quelle volute.
        /// </summary>
        private static bool ApplyPlatform(SpriteAtlasImporter importer, string platform, bool overridden, TextureImporterFormat format)
        {
            TextureImporterPlatformSettings current = importer.GetPlatformSettings(platform);
            if (current.overridden == overridden && current.maxTextureSize == MAX_TEXTURE_SIZE && current.format == format
                && current.textureCompression == TextureImporterCompression.Compressed)
                return false;
            current.name = platform;
            current.overridden = overridden;
            current.maxTextureSize = MAX_TEXTURE_SIZE;
            current.format = format;
            current.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformSettings(current);
            return true;
        }

        /// <summary>
        /// Imposta Sprite Packer su "Sprite Atlas V2" se non lo è già (modifica di ProjectSettings/EditorSettings.asset,
        /// necessaria perché con la modalità Disabled gli atlanti non vengono usati in Play Mode).
        /// </summary>
        /// <returns>Frase di riepilogo.</returns>
        private static string EnsureSpritePackerMode()
        {
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
                return "Sprite Packer: già Sprite Atlas V2.";
            SpritePackerMode previous = EditorSettings.spritePackerMode;
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            return $"Sprite Packer: impostato su Sprite Atlas V2 (era {previous}; ProjectSettings/EditorSettings.asset).";
        }
    }
}

