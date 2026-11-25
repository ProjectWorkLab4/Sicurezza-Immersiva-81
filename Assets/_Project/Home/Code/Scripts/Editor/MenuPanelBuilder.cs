using TMPro;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder del prefab PANEL_Menu (pannello vetro dei menu): ombra (TEX_PanelShadow), vetro arrotondato con riflesso
    /// (RoundedPanelGraphic su TEX_PanelRounded), intestazione (PANEL_BackSlot, LBL_Breadcrumb, LBL_Title, PANEL_HeaderRightSlot) e
    /// PANEL_Content. Sicuro da rieseguire: aggiorna il prefab sul posto (stessi GUID e fileID), non duplica figli.
    /// Eseguito da SI81/UI/Build Base prima della sandbox; richiede tema e sprite (SI81/UI/Build Theme).
    /// </summary>
    public static class MenuPanelBuilder
    {
        /// <summary>Prefab PANEL_Menu.</summary>
        public const string PREFAB_PATH = BaseBuilder.BASE_PREFABS_FOLDER + "/PANEL_Menu.prefab";
        /// <summary>Nome della radice (nome in scena = nome dell'asset).</summary>
        public const string ROOT_NAME = "PANEL_Menu";

        /// <summary>
        /// Voce di menu: crea o aggiorna solo il prefab PANEL_Menu.
        /// </summary>
        [MenuItem("SI81/UI/Build Menu Panel", priority = 21)]
        public static void BuildMenu()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            Debug.Log("[SI81 UI] " + BuildPrefab(theme, strings));
        }

        /// <summary>
        /// Crea o aggiorna il prefab PANEL_Menu e restituisce un riepilogo in italiano.
        /// </summary>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <param name="strings">Tabella testi SO_UIStrings_It.</param>
        /// <returns>Riepilogo.</returns>
        public static string BuildPrefab(UITheme theme, UIStrings strings)
        {
            if (theme == null || strings == null) return "PANEL_Menu: interrotto, SO_UITheme o SO_UIStrings_It mancanti.";
            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.PANEL_ROUNDED);
            var shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.PANEL_SHADOW);
            if (rounded == null || shadowSprite == null)
                return "PANEL_Menu: interrotto, sprite TEX_PanelRounded / TEX_PanelShadow mancanti (eseguire SI81/UI/Build Theme).";
            BaseBuilder.EnsureFolder(BaseBuilder.PREFABS_FOLDER);

            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(PREFAB_PATH) : new GameObject(ROOT_NAME, typeof(RectTransform));
            try
            {
                root.name = ROOT_NAME;
                root.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)root.transform;
                // Ancorato in alto al centro del frame 840×627, a margin.screen (40) dal bordo superiore: x = (840-760)/2 = 40.
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -theme.MarginScreen);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;

                // Ombra (dietro, tinta col token menuPanel) e vetro arrotondato con riflesso.
                BuildGlassLayers(rect, shadowSprite, rounded, out UnityEngine.UI.Image shadow, out RoundedPanelGraphic glass);

                // Intestazione.
                RectTransform header = BaseBuilder.GetOrCreateRectChild(rect, "PANEL_Header", "Header");
                header.SetSiblingIndex(2);
                RectTransform backSlot = BaseBuilder.GetOrCreateRectChild(header, "PANEL_BackSlot", "BackSlot");
                RectTransform titleColumn = BaseBuilder.GetOrCreateRectChild(header, "PANEL_TitleColumn", "TitleColumn");
                UIStringText breadcrumb = BuildLabel(titleColumn, "LBL_Breadcrumb", strings, theme, TextStyleIds.BODY, ColorTokens.TEXT_ON_GREEN);
                UIStringText title = BuildLabel(titleColumn, "LBL_Title", strings, theme, TextStyleIds.TITLE, ColorTokens.TEXT_ON_DARK);
                RectTransform rightSlot = BaseBuilder.GetOrCreateRectChild(header, "PANEL_HeaderRightSlot", "HeaderRightSlot");

                // Contenuti.
                RectTransform content = BaseBuilder.GetOrCreateRectChild(rect, "PANEL_Content", "Content");
                content.SetSiblingIndex(3);

                MenuPanel panel = BaseBuilder.GetOrAdd<MenuPanel>(root);
                panel.Configure(theme, shadow, glass, header, backSlot, titleColumn, breadcrumb, title, rightSlot, content);
                // Valori del prefab: nessun titolo, nessun percorso, nessun indietro, altezza di default del tema (Home).
                panel.ConfigureParameters(string.Empty, string.Empty, false, 0f);

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out bool success);
                return success ? $"PANEL_Menu: {(exists ? "aggiornato" : "creato")} ({PREFAB_PATH})." : "PANEL_Menu: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Imposta i parametri di un'istanza di PANEL_Menu in una scena (titolo, percorso, indietro, altezza) e registra
        /// le modifiche come override dell'istanza, così restano salvate nella scena.
        /// </summary>
        /// <param name="panel">MenuPanel dell'istanza.</param>
        /// <param name="titleKey">Chiave del titolo.</param>
        /// <param name="breadcrumbKey">Chiave del percorso o stringa vuota.</param>
        /// <param name="showBack">true per mostrare il posto di BTN_Back.</param>
        /// <param name="height">Altezza in unità Canvas (0 = default del tema).</param>
        public static void ConfigureInstance(MenuPanel panel, string titleKey, string breadcrumbKey, bool showBack, float height)
        {
            panel.ConfigureParameters(titleKey, breadcrumbKey, showBack, height);
            BaseBuilder.RecordInstanceOverrides(panel.gameObject);
        }

        /// <summary>
        /// Crea o aggiorna i due strati comuni dei pannelli vetro (usato anche da SidePanelBuilder): IMG_Shadow (indice 0,
        /// Image 9-slice con lo sprite ombra dato, non riceve raggi) e IMG_Glass (indice 1, RoundedPanelGraphic su
        /// TEX_PanelRounded, riceve i raggi e blocca i clic dietro il pannello).
        /// </summary>
        internal static void BuildGlassLayers(RectTransform panel, Sprite shadowSprite, Sprite rounded,
            out UnityEngine.UI.Image shadow, out RoundedPanelGraphic glass)
        {
            RectTransform shadowRect = BaseBuilder.GetOrCreateRectChild(panel, "IMG_Shadow");
            shadow = BaseBuilder.GetOrAdd<UnityEngine.UI.Image>(shadowRect.gameObject);
            shadow.sprite = shadowSprite;
            shadow.type = UnityEngine.UI.Image.Type.Sliced;
            // Il centro dell'ombra è trasparente (bucato sotto il pannello): niente mesh per il riquadro centrale.
            shadow.fillCenter = false;
            shadow.pixelsPerUnitMultiplier = 1f;
            shadow.raycastTarget = false;
            shadowRect.SetSiblingIndex(0);

            RectTransform glassRect = BaseBuilder.GetOrCreateRectChild(panel, "IMG_Glass");
            glass = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(glassRect.gameObject);
            glass.Sprite = rounded;
            glass.raycastTarget = true;
            glassRect.SetSiblingIndex(1);
        }

        /// <summary>
        /// Crea o aggiorna un'etichetta TMP con UIStringText (stile e colore del tema, una riga, senza a capo).
        /// Usata anche da SidePanelBuilder.
        /// </summary>
        internal static UIStringText BuildLabel(Transform parent, string name, UIStrings strings, UITheme theme, string style, string colorToken)
        {
            return BuildLabel(parent, name, null, strings, theme, style, colorToken);
        }

        /// <summary>
        /// Come <see cref="BuildLabel(Transform, string, UIStrings, UITheme, string, string)"/>, ma rinomina un figlio con il
        /// vecchio nome (<paramref name="legacyName"/>) invece di crearne un secondo (migrazione dei prefab delle versioni precedenti).
        /// </summary>
        internal static UIStringText BuildLabel(Transform parent, string name, string legacyName, UIStrings strings, UITheme theme, string style, string colorToken)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name, legacyName);
            TextMeshProUGUI tmp = BaseBuilder.GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            // Overflow, non Ellipsis: la riga Figma (40 / 23) è più bassa della riga naturale di Jaldi (~1,69 em) e con
            // Ellipsis/Truncate TMP nasconderebbe tutto il testo. I titoli sono brevi (nomi scenario, titoli schermata).
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            tmp.text = string.Empty;
            UIStringText label = BaseBuilder.GetOrAdd<UIStringText>(rect.gameObject);
            label.Configure(strings, string.Empty, theme, style, colorToken);
            EditorUtility.SetDirty(tmp);
            return label;
        }
    }
}
