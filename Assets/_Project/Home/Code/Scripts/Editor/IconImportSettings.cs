using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Impostazioni di import delle icone ICN_ (PNG 128×128 bianchi in Assets/_Project/Home/Art/Textures/Icons/).
    /// Applicate automaticamente a ogni import da questo AssetPostprocessor e ricontrollate da
    /// <see cref="IconBuilder"/>, così restano uguali per tutti i team anche se qualcuno le cambia a mano.
    /// </summary>
    public class IconImportSettings : AssetPostprocessor
    {
        /// <summary>Cartella delle icone generate (con la barra finale, per il confronto dei percorsi).</summary>
        public const string ICONS_FOLDER_PREFIX = IconBuilder.ICONS_FOLDER + "/";
        /// <summary>Prefisso obbligatorio dei file icona.</summary>
        public const string ICON_PREFIX = "ICN_";
        /// <summary>Dimensione massima della texture (= dimensione del PNG).</summary>
        public const int MAX_SIZE = IconBuilder.ICON_SIZE;
        /// <summary>Piattaforma Android (Meta Quest 3).</summary>
        public const string ANDROID = "Android";
        /// <summary>
        /// Formato Android: ASTC 4×4 (8 bit/pixel, il blocco ASTC più fine) per bordi alfa puliti;
        /// 128×128 con mipmap ≈ 21 KB per icona.
        /// </summary>
        public const TextureImporterFormat ANDROID_FORMAT = TextureImporterFormat.ASTC_4x4;

        /// <summary>
        /// Versione del postprocessor: incrementarla quando cambiano le impostazioni, così Unity reimporta le icone.
        /// </summary>
        /// <returns>Numero di versione.</returns>
        public override uint GetVersion()
        {
            return 2;
        }

        /// <summary>
        /// Prima dell'import di una texture: se è un'icona ICN_ della cartella Icons, applica le impostazioni comuni.
        /// </summary>
        private void OnPreprocessTexture()
        {
            if (!IsIconPath(assetPath)) return;
            Apply((TextureImporter)assetImporter);
        }

        /// <summary>
        /// Indica se il percorso è un PNG ICN_ direttamente dentro Assets/_Project/Home/Art/Textures/Icons/.
        /// </summary>
        /// <param name="path">Percorso dell'asset (con barre "/").</param>
        /// <returns>true se il file è un'icona gestita da questo progetto.</returns>
        public static bool IsIconPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(ICONS_FOLDER_PREFIX)) return false;
            string fileName = path.Substring(ICONS_FOLDER_PREFIX.Length);
            return !fileName.Contains("/") && fileName.StartsWith(ICON_PREFIX) && fileName.EndsWith(".png");
        }

        /// <summary>
        /// Applica le impostazioni comuni delle icone all'importer e dice se qualcosa è cambiato.
        /// Sprite singolo, alfa come trasparenza, sRGB, Clamp, Bilinear, mipmap attive (UI world-space
        /// rimpicciolita a distanza: niente sfarfallio), niente lettura/scrittura, ASTC 4×4 su Android.
        /// </summary>
        /// <param name="importer">Importer della texture icona.</param>
        /// <returns>true se almeno un'impostazione è stata modificata.</returns>
        public static bool Apply(TextureImporter importer)
        {
            bool changed = false;

            // Tipo e forma dello sprite.
            changed |= Set(importer.textureType, TextureImporterType.Sprite, v => importer.textureType = v);
            changed |= Set(importer.spriteImportMode, SpriteImportMode.Single, v => importer.spriteImportMode = v);

            // Alfa e colore: il PNG è bianco con la forma nel canale alfa.
            changed |= Set(importer.alphaSource, TextureImporterAlphaSource.FromInput, v => importer.alphaSource = v);
            changed |= Set(importer.alphaIsTransparency, true, v => importer.alphaIsTransparency = v);
            changed |= Set(importer.sRGBTexture, true, v => importer.sRGBTexture = v);

            // Campionamento.
            changed |= Set(importer.wrapMode, TextureWrapMode.Clamp, v => importer.wrapMode = v);
            changed |= Set(importer.filterMode, FilterMode.Bilinear, v => importer.filterMode = v);
            // Niente mipmap sull'originale: in uso c'è l'atlante TEX_AtlasUI, che genera i propri mipmap.
            changed |= Set(importer.mipmapEnabled, false, v => importer.mipmapEnabled = v);
            changed |= Set(importer.isReadable, false, v => importer.isReadable = v);
            changed |= Set(importer.npotScale, TextureImporterNPOTScale.None, v => importer.npotScale = v);

            // Dimensione e compressione: l'originale resta NON compresso (Unity lo richiede per impacchettarlo nell'atlante
            // TEX_AtlasUI senza perdere dettaglio); la compressione (ASTC 4x4 su Android) la applica l'atlante.
            changed |= Set(importer.maxTextureSize, MAX_SIZE, v => importer.maxTextureSize = v);
            changed |= Set(importer.textureCompression, TextureImporterCompression.Uncompressed, v => importer.textureCompression = v);

            // Sprite: rettangolo pieno (uGUI Image non usa la mesh stretta), nessuna forma fisica.
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

            // Nessun override Android sull'originale (la compressione Android è nell'atlante).
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings(ANDROID);
            bool androidChanged = false;
            androidChanged |= Set(android.overridden, false, v => android.overridden = v);
            if (androidChanged)
            {
                importer.SetPlatformTextureSettings(android);
                changed = true;
            }

            return changed;
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
