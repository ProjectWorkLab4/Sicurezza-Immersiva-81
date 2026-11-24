using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================================================
// HELPER CONDIVISI (Blocco C · C1) che le altre parti di HUDBuilder POSSONO usare (firme esatte, tutti i nomi iniziano con "Hud"):
//   internal const string HUD_FOLDER                        // "Assets/_Project/Home/UI/Prefabs/HUD"
//   internal const string HUD_ICONS_ASSET                   // "Assets/_Project/Home/UI/Data/SO_HUDIcons.asset"
//   internal static bool HudTryLoadContext(out UITheme theme, out UIStrings strings)
//   internal static string HudPrefabPath(string prefabName)
//   internal static GameObject HudCreateOrLoadPrefab(string prefabName, out bool existed)
//   internal static string HudSavePrefab(GameObject root, string prefabName, bool existed)
//   internal static GameObject HudCreateWorldCanvas(Transform parent, string canvasName, Vector2 size, float scale)   // World Space + CanvasGroup + WorldCanvasCamera, SENZA raycaster
//   internal static Sprite HudEnsureRounded(UITheme theme, string family, float radius)                              // TEX_<family>Rounded
//   internal static Sprite HudEnsureShadow(UITheme theme, string spriteName, float radius, string shadowRole)       // TEX_<spriteName>.png (ruolo es. ShadowRoles.HUD_CARD)
//   internal static RoundedPanelGraphic HudAddRounded(Transform parent, string name, Sprite sprite, Color color)
//   internal static TMP_Text HudAddText(Transform parent, string name, UITheme theme, UIStrings strings, string style, string colorToken, TextAlignmentOptions alignment)
//   internal static HUDIconLibrary HudEnsureIconLibrary()
// =============================================================================================================

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder della HUD in scenario (Blocco C), diviso in file parziali: questo (punto di ingresso e strumenti comuni),
    /// .Rig (C1: HUD_WristPanel, HUD_EventAlert, HUD_ActionFeedback, HUD_Subtitles, MNG_HUD), .World (C2: etichette, decisione,
    /// esito) e .EndScreens (C3: Pausa, Debrief, Score &amp; Replay, Training completato). Menu <c>SI81/UI/Build HUD</c> →
    /// <see cref="BuildAll"/>. Create-or-update sul posto (stessi GUID, figli cercati per nome, sicuro da rieseguire, mai YAML a mano).
    /// Richiede Build Theme, Build Components e (per MNG_HUD) i prefab del Blocco C già costruiti: l'ordine di BuildAll lo garantisce.
    /// </summary>
    internal static partial class HUDBuilder
    {
        /// <summary>Cartella dei prefab della HUD (HUD_*, MNG_HUD).</summary>
        internal const string HUD_FOLDER = BaseBuilder.PREFABS_FOLDER + "/HUD";
        /// <summary>Asset della libreria delle icone per nome.</summary>
        internal const string HUD_ICONS_ASSET = ThemeBuilder.DATA_FOLDER + "/SO_HUDIcons.asset";

        /// <summary>
        /// Voce di menu: crea o aggiorna tutti i prefab della HUD.
        /// </summary>
        [MenuItem("SI81/UI/Build HUD", priority = 28)]
        public static void BuildHudMenu()
        {
            BuildAll();
        }

        /// <summary>
        /// Esegue i passi nell'ordine: etichette nel mondo e decisione/esito (C2) prima, perché MNG_HUD li annida; poi il rig
        /// HUD (C1: polso, avvisi, sottotitoli, MNG_HUD); poi le schermate di Pausa e fine (C3). Un passo che fallisce non
        /// ferma gli altri (errore in Console).
        /// </summary>
        internal static void BuildAll()
        {
            BaseBuilder.EnsureFolder(HUD_FOLDER);
            HudRunStep("Etichette nel mondo", () => BuildWorldLabels());
            HudRunStep("Decisione ed esito", () => BuildDecisionOutcome());
            HudRunStep("Rig HUD (polso, avvisi, sottotitoli, MNG_HUD)", () => BuildRigHud());
            HudRunStep("Schermate di Pausa e fine", () => BuildEndScreens());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Esegue un passo isolando le eccezioni.
        /// </summary>
        /// <param name="stepName">Nome del passo.</param>
        /// <param name="step">Azione.</param>
        private static void HudRunStep(string stepName, Action step)
        {
            try
            {
                step();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SI81 UI] Build HUD · {stepName}: errore, passo interrotto. {exception}");
            }
        }

        // =====================================================================
        // Contesto e prefab
        // =====================================================================

        /// <summary>
        /// Carica tema e testi; scrive un errore se mancano.
        /// </summary>
        /// <param name="theme">Tema.</param>
        /// <param name="strings">Tabella testi.</param>
        /// <returns>true se disponibili.</returns>
        internal static bool HudTryLoadContext(out UITheme theme, out UIStrings strings)
        {
            theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] Build HUD: SO_UITheme o SO_UIStrings_It mancanti (eseguire SI81/UI/Build Theme).");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Percorso di un prefab della HUD.
        /// </summary>
        /// <param name="prefabName">Nome, es. HUD_WristPanel.</param>
        /// <returns>Percorso sotto Prefabs/HUD.</returns>
        internal static string HudPrefabPath(string prefabName)
        {
            return HUD_FOLDER + "/" + prefabName + ".prefab";
        }

        /// <summary>
        /// Carica il contenuto del prefab esistente (aggiornamento sul posto) oppure crea la radice di uno nuovo. Da chiudere
        /// SEMPRE con <see cref="HudSavePrefab"/>.
        /// </summary>
        /// <param name="prefabName">Nome del prefab (= nome della radice).</param>
        /// <param name="existed">true se esisteva già.</param>
        /// <returns>La radice da modificare.</returns>
        internal static GameObject HudCreateOrLoadPrefab(string prefabName, out bool existed)
        {
            BaseBuilder.EnsureFolder(HUD_FOLDER);
            string path = HudPrefabPath(prefabName);
            existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(prefabName, typeof(RectTransform));
            if (existed) ComponentsBuilder.RemoveMissingScripts(root);
            root.name = prefabName;
            root.layer = LayerMask.NameToLayer("UI");
            return root;
        }

        /// <summary>
        /// Salva la radice come prefab (stesso GUID) e libera la memoria.
        /// </summary>
        /// <param name="root">Radice ottenuta da <see cref="HudCreateOrLoadPrefab"/>.</param>
        /// <param name="prefabName">Nome del prefab.</param>
        /// <param name="existed">Valore restituito da <see cref="HudCreateOrLoadPrefab"/>.</param>
        /// <returns>Riepilogo in italiano.</returns>
        internal static string HudSavePrefab(GameObject root, string prefabName, bool existed)
        {
            string path = HudPrefabPath(prefabName);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                return success ? $"{prefabName}: {(existed ? "aggiornato" : "creato")} ({path})." : $"{prefabName}: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (existed) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // =====================================================================
        // Figli comuni
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna un Canvas World Space con CanvasGroup e WorldCanvasCamera, SENZA raycaster (HUD di sola lettura:
        /// nessun raggio lo colpisce). Il Canvas è centrato nel genitore, scala uniforme data.
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="canvasName">Nome (CANVAS_...).</param>
        /// <param name="size">Dimensione in unità Canvas.</param>
        /// <param name="scale">Scala uniforme (metri per unità Canvas).</param>
        /// <returns>Il GameObject del Canvas.</returns>
        internal static GameObject HudCreateWorldCanvas(Transform parent, string canvasName, Vector2 size, float scale)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, canvasName);
            GameObject go = rect.gameObject;
            go.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = BaseBuilder.GetOrAdd<Canvas>(go);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            BaseBuilder.GetOrAdd<CanvasGroup>(go);
            BaseBuilder.GetOrAdd<WorldCanvasCamera>(go);
            foreach (GraphicRaycaster raycaster in go.GetComponents<GraphicRaycaster>()) UnityEngine.Object.DestroyImmediate(raycaster);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * scale;
            return go;
        }

        /// <summary>
        /// Crea o aggiorna lo sprite 9-slice arrotondato TEX_&lt;famiglia&gt;Rounded.
        /// </summary>
        /// <param name="theme">Tema (non usato per ora, per simmetria).</param>
        /// <param name="family">Famiglia, es. "HudCard".</param>
        /// <param name="radius">Raggio degli angoli in unità Canvas.</param>
        /// <returns>Lo sprite caricato.</returns>
        internal static Sprite HudEnsureRounded(UITheme theme, string family, float radius)
        {
            string path = $"{SpriteBuilder.SPRITES_FOLDER}/TEX_{family}Rounded.png";
            SpriteBuilder.BuildRoundedSprite(path, radius);
            return ComponentsBuilder.LoadSprite(path);
        }

        /// <summary>
        /// Crea o aggiorna uno sprite ombra 9-slice per un ruolo d'ombra del tema.
        /// </summary>
        /// <param name="theme">Tema.</param>
        /// <param name="spriteName">Nome senza estensione, es. "TEX_HudCardShadow".</param>
        /// <param name="radius">Raggio della forma che ombreggia.</param>
        /// <param name="shadowRole">Ruolo dell'ombra (vedi ShadowRoles).</param>
        /// <returns>Lo sprite caricato (null se manca il ruolo).</returns>
        internal static Sprite HudEnsureShadow(UITheme theme, string spriteName, float radius, string shadowRole)
        {
            UITheme.ShadowToken token = theme.GetShadow(shadowRole);
            if (token == null) return null;
            string path = $"{SpriteBuilder.SPRITES_FOLDER}/{spriteName}.png";
            SpriteBuilder.BuildShadowSprite(path, radius, token);
            return ComponentsBuilder.LoadSprite(path);
        }

        /// <summary>
        /// Crea o aggiorna una grafica arrotondata figlia (decorativa: nessun raggio).
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome (IMG_...).</param>
        /// <param name="sprite">Sprite 9-slice.</param>
        /// <param name="color">Colore.</param>
        /// <returns>La grafica.</returns>
        internal static RoundedPanelGraphic HudAddRounded(Transform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            rect.gameObject.layer = LayerMask.NameToLayer("UI");
            var graphic = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(rect.gameObject);
            graphic.Sprite = sprite;
            graphic.color = color;
            graphic.raycastTarget = false;
            return graphic;
        }

        /// <summary>
        /// Crea o aggiorna un testo TMP dinamico (stile e colore del tema, nessuna chiave: il testo lo scrive il codice).
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome (LBL_...).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="strings">Tabella testi.</param>
        /// <param name="style">Stile (TextStyleIds).</param>
        /// <param name="colorToken">Token colore (ColorTokens).</param>
        /// <param name="alignment">Allineamento.</param>
        /// <returns>Il testo TMP.</returns>
        internal static TMP_Text HudAddText(Transform parent, string name, UITheme theme, UIStrings strings, string style, string colorToken, TextAlignmentOptions alignment)
        {
            UIStringText label = ComponentsBuilder.AddLabel(parent, name, theme, strings, style, colorToken, string.Empty, alignment);
            label.gameObject.layer = LayerMask.NameToLayer("UI");
            return label.GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Crea o aggiorna SO_HUDIcons con tutte le icone ICN_ della cartella Icons.
        /// </summary>
        /// <returns>La libreria.</returns>
        internal static HUDIconLibrary HudEnsureIconLibrary()
        {
            var library = ScreensBuilder.LoadOrCreateAsset<HUDIconLibrary>(HUD_ICONS_ASSET, out _);
            var entries = new List<HUDIconLibrary.Entry>();
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { IconBuilder.ICONS_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) entries.Add(new HUDIconLibrary.Entry { Name = sprite.name, Sprite = sprite });
            }
            entries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            library.SetEntries(entries);
            EditorUtility.SetDirty(library);
            return library;
        }
    }
}
