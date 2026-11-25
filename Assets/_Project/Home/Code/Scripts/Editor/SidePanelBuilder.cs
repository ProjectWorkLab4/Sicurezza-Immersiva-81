using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder del prefab PANEL_Side (pannello vetro laterale, inclinato verso il giocatore): ombra compatta
    /// (TEX_PanelShadowCompact), vetro arrotondato con riflesso (stessi strati di PANEL_Menu), LBL_Title e PANEL_Content.
    /// Sicuro da rieseguire: aggiorna il prefab sul posto (stessi GUID e fileID), non duplica figli.
    /// Eseguito da SI81/UI/Build Base prima della sandbox; richiede tema e sprite (SI81/UI/Build Theme).
    /// </summary>
    public static class SidePanelBuilder
    {
        /// <summary>Prefab PANEL_Side.</summary>
        public const string PREFAB_PATH = BaseBuilder.BASE_PREFABS_FOLDER + "/PANEL_Side.prefab";
        /// <summary>Nome della radice (nome in scena = nome dell'asset).</summary>
        public const string ROOT_NAME = "PANEL_Side";

        /// <summary>
        /// Voce di menu: crea o aggiorna solo il prefab PANEL_Side.
        /// </summary>
        [MenuItem("SI81/UI/Build Side Panel", priority = 22)]
        public static void BuildSideMenu()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            Debug.Log("[SI81 UI] " + BuildPrefab(theme, strings));
        }

        /// <summary>
        /// Crea o aggiorna il prefab PANEL_Side e restituisce un riepilogo in italiano.
        /// </summary>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <param name="strings">Tabella testi SO_UIStrings_It.</param>
        /// <returns>Riepilogo.</returns>
        public static string BuildPrefab(UITheme theme, UIStrings strings)
        {
            if (theme == null || strings == null) return "PANEL_Side: interrotto, SO_UITheme o SO_UIStrings_It mancanti.";
            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.PANEL_ROUNDED);
            var shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.PANEL_SHADOW_COMPACT);
            if (rounded == null || shadowSprite == null)
                return "PANEL_Side: interrotto, sprite TEX_PanelRounded / TEX_PanelShadowCompact mancanti (eseguire SI81/UI/Build Theme).";
            if (!strings.Contains(SidePanel.DEFAULT_TITLE_KEY))
                Debug.LogError($"[SI81 UI] Chiave \"{SidePanel.DEFAULT_TITLE_KEY}\" assente in SO_UIStrings_It.");
            BaseBuilder.EnsureFolder(BaseBuilder.PREFABS_FOLDER);

            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(PREFAB_PATH) : new GameObject(ROOT_NAME, typeof(RectTransform));
            try
            {
                root.name = ROOT_NAME;
                root.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)root.transform;
                // Ancorato in alto al centro come PANEL_Menu; pivot in alto a destra (cerniera dell'inclinazione).
                // La posizione vera la decide SidePanel accanto ad AttachTo (istanza in scena).
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(0f, -theme.MarginScreen);
                rect.localScale = Vector3.one;

                // Ombra compatta (tinta col token sidePanel) e vetro arrotondato con riflesso: stessi strati di PANEL_Menu.
                MenuPanelBuilder.BuildGlassLayers(rect, shadowSprite, rounded, out UnityEngine.UI.Image shadow, out RoundedPanelGraphic glass);

                // Titolo (Body Bold, on-dark) e contenuti.
                UIStringText title = MenuPanelBuilder.BuildLabel(rect, "LBL_Title", strings, theme, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_DARK);
                title.transform.SetSiblingIndex(2);
                RectTransform content = BaseBuilder.GetOrCreateRectChild(rect, "PANEL_Content", "Content");
                content.SetSiblingIndex(3);

                SidePanel panel = BaseBuilder.GetOrAdd<SidePanel>(root);
                panel.Configure(theme, shadow, glass, title, content);
                // Valori del prefab: titolo "Criteri di completamento", inclinazione dal tema, nessun pannello accanto.
                panel.ConfigureParameters(SidePanel.DEFAULT_TITLE_KEY, -1f, null);

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out bool success);
                return success ? $"PANEL_Side: {(exists ? "aggiornato" : "creato")} ({PREFAB_PATH})." : "PANEL_Side: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Imposta i parametri di un'istanza di PANEL_Side in una scena (titolo, inclinazione, pannello accanto),
        /// ricalcola il layout e registra le modifiche come override dell'istanza.
        /// </summary>
        /// <param name="panel">SidePanel dell'istanza.</param>
        /// <param name="titleKey">Chiave del titolo.</param>
        /// <param name="tiltAngle">Gradi (negativo = token del tema).</param>
        /// <param name="attachTo">Fratello accanto a cui mettersi (es. PANEL_Menu) o null.</param>
        public static void ConfigureInstance(SidePanel panel, string titleKey, float tiltAngle, RectTransform attachTo)
        {
            panel.ConfigureParameters(titleKey, tiltAngle, attachTo);
            BaseBuilder.RecordInstanceOverrides(panel.gameObject);
        }
    }
}
