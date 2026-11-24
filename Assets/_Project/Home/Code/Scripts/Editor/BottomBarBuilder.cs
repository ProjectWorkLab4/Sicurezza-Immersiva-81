using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder dei prefab BTN_BottomBarItem (voce: pulsante + pillola hover + icona + etichetta) e PANEL_BottomBar
    /// (ombra, vetro chiaro e 4 istanze annidate di BTN_BottomBarItem configurate dai dati in <see cref="ITEMS"/>).
    /// Sicuro da rieseguire: aggiorna i prefab sul posto (stessi GUID e fileID), non duplica figli né istanze.
    /// Eseguito da SI81/UI/Build Base prima di MNG_UI; richiede tema, testi, icone e sprite (SI81/UI/Build Theme).
    /// </summary>
    public static class BottomBarBuilder
    {
        /// <summary>Prefab BTN_BottomBarItem.</summary>
        public const string ITEM_PREFAB_PATH = BaseBuilder.BASE_PREFABS_FOLDER + "/BTN_BottomBarItem.prefab";
        /// <summary>Prefab PANEL_BottomBar.</summary>
        public const string BAR_PREFAB_PATH = BaseBuilder.BASE_PREFABS_FOLDER + "/PANEL_BottomBar.prefab";
        /// <summary>Nome della radice della voce (nome in scena = nome dell'asset).</summary>
        public const string ITEM_ROOT_NAME = "BTN_BottomBarItem";
        /// <summary>Nome della radice della barra.</summary>
        public const string BAR_ROOT_NAME = "PANEL_BottomBar";

        /// <summary>Dati di una voce della barra.</summary>
        private struct ItemData
        {
            /// <summary>Suffisso del nome dell'istanza (BTN_BottomBarItem_&lt;Suffix&gt;).</summary>
            public string Suffix;
            /// <summary>Schermata aperta.</summary>
            public UIScreenId Screen;
            /// <summary>Nome dell'icona contorno (la piena ha il suffisso _Filled).</summary>
            public string Icon;
            /// <summary>Chiave dell'etichetta.</summary>
            public string LabelKey;
        }

        /// <summary>Voci nell'ordine Figma (UI_BottomBar 7:232): Home, Profilo, Statistiche, Impostazioni.</summary>
        private static readonly ItemData[] ITEMS =
        {
            new ItemData { Suffix = "Home", Screen = UIScreenId.Home, Icon = "ICN_Home", LabelKey = "ui.nav.home" },
            new ItemData { Suffix = "Profile", Screen = UIScreenId.Profile, Icon = "ICN_Person", LabelKey = "ui.nav.profile" },
            new ItemData { Suffix = "Stats", Screen = UIScreenId.Stats, Icon = "ICN_BarChart", LabelKey = "ui.nav.stats" },
            new ItemData { Suffix = "Settings", Screen = UIScreenId.Settings, Icon = "ICN_Settings", LabelKey = "ui.nav.settings" },
        };

        /// <summary>
        /// Voce di menu: crea o aggiorna solo i prefab BTN_BottomBarItem e PANEL_BottomBar.
        /// </summary>
        [MenuItem("SI81/UI/Build Bottom Bar", priority = 23)]
        public static void BuildBottomBarMenu()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            Debug.Log("[SI81 UI] " + BuildPrefabs(theme, strings));
        }

        /// <summary>
        /// Crea o aggiorna i due prefab (prima la voce, poi la barra che la contiene) e restituisce un riepilogo.
        /// </summary>
        /// <param name="theme">Tema SO_UITheme.</param>
        /// <param name="strings">Tabella testi SO_UIStrings_It.</param>
        /// <returns>Riepilogo in italiano.</returns>
        public static string BuildPrefabs(UITheme theme, UIStrings strings)
        {
            if (theme == null || strings == null) return "PANEL_BottomBar: interrotto, SO_UITheme o SO_UIStrings_It mancanti.";
            var pill = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.PILL_ROUNDED);
            var shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.BOTTOM_BAR_SHADOW);
            if (pill == null || shadowSprite == null)
                return "PANEL_BottomBar: interrotto, sprite TEX_PillRounded / TEX_BottomBarShadow mancanti (eseguire SI81/UI/Build Theme).";
            foreach (ItemData item in ITEMS)
            {
                if (!strings.Contains(item.LabelKey)) Debug.LogError($"[SI81 UI] Chiave \"{item.LabelKey}\" assente in SO_UIStrings_It.");
                if (ComponentsBuilder.LoadIcon(item.Icon) == null || ComponentsBuilder.LoadIcon(item.Icon + "_Filled") == null)
                    Debug.LogError($"[SI81 UI] Icona {item.Icon} / {item.Icon}_Filled assente. Eseguire SI81/UI/Build Theme.");
            }
            BaseBuilder.EnsureFolder(BaseBuilder.PREFABS_FOLDER);
            string itemReport = BuildItemPrefab(theme, strings, pill);
            string barReport = BuildBarPrefab(theme, pill, shadowSprite);
            return itemReport + "\n" + barReport;
        }

        /// <summary>
        /// Crea o aggiorna BTN_BottomBarItem: Button (nessuna transizione, stati gestiti da BottomBarItem), IMG_Hover
        /// (pillola, bersaglio dei raggi), ICN_ItemIcon e LBL_ItemLabel (decorativi, raycastTarget = false). Dati di default: Home.
        /// </summary>
        private static string BuildItemPrefab(UITheme theme, UIStrings strings, Sprite pill)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(ITEM_PREFAB_PATH) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(ITEM_PREFAB_PATH) : new GameObject(ITEM_ROOT_NAME, typeof(RectTransform));
            try
            {
                root.name = ITEM_ROOT_NAME;
                root.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;

                // Pillola hover: unico bersaglio dei raggi della voce (alpha 0 a riposo, colore dal tema a runtime).
                RectTransform hoverRect = BaseBuilder.GetOrCreateRectChild(rect, "IMG_Hover");
                hoverRect.SetSiblingIndex(0);
                RoundedPanelGraphic hover = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(hoverRect.gameObject);
                hover.Sprite = pill;
                hover.DisableSheen();
                hover.raycastTarget = true;

                // Icona (decorativa).
                RectTransform iconRect = BaseBuilder.GetOrCreateRectChild(rect, "ICN_ItemIcon", "ICN_Icon");
                iconRect.SetSiblingIndex(1);
                var icon = BaseBuilder.GetOrAdd<UnityEngine.UI.Image>(iconRect.gameObject);
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                // Etichetta (decorativa): una riga, allineata a sinistra e centrata in verticale.
                UIStringText label = MenuPanelBuilder.BuildLabel(rect, "LBL_ItemLabel", "LBL_Label", strings, theme, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY);
                label.transform.SetSiblingIndex(2);
                var tmp = label.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                EditorUtility.SetDirty(tmp);

                var button = BaseBuilder.GetOrAdd<UnityEngine.UI.Button>(root);
                button.targetGraphic = hover;
                button.transition = UnityEngine.UI.Selectable.Transition.None;
                UnityEngine.UI.Navigation navigation = button.navigation;
                navigation.mode = UnityEngine.UI.Navigation.Mode.None;
                button.navigation = navigation;

                BottomBarItem item = BaseBuilder.GetOrAdd<BottomBarItem>(root);
                item.Configure(theme, button, hover, icon, label);
                ItemData home = ITEMS[0];
                item.ConfigureData(home.Screen, ComponentsBuilder.LoadIcon(home.Icon), ComponentsBuilder.LoadIcon(home.Icon + "_Filled"), home.LabelKey);

                PrefabUtility.SaveAsPrefabAsset(root, ITEM_PREFAB_PATH, out bool success);
                return success ? $"BTN_BottomBarItem: {(exists ? "aggiornato" : "creato")} ({ITEM_PREFAB_PATH})." : "BTN_BottomBarItem: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Crea o aggiorna PANEL_BottomBar: IMG_Shadow (TEX_BottomBarShadow, decorativa), IMG_Glass (pillola vetro chiaro,
        /// blocca i raggi sulla barra), PANEL_Items con le 4 istanze annidate BTN_BottomBarItem_&lt;Voce&gt;.
        /// Posizione di default = layoutHome: ancorata in basso al centro del genitore (PANEL_BottomBarSlot, in fondo al frame
        /// 840×627), bordo superiore a margin.screen + altezza (96) dal fondo → y 531 dall'alto.
        /// </summary>
        private static string BuildBarPrefab(UITheme theme, Sprite pill, Sprite shadowSprite)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(BAR_PREFAB_PATH) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(BAR_PREFAB_PATH) : new GameObject(BAR_ROOT_NAME, typeof(RectTransform));
            try
            {
                root.name = BAR_ROOT_NAME;
                root.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, theme.MarginScreen + theme.GetNumber(NumberTokens.BOTTOM_BAR_HEIGHT));
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;

                // Ombra (decorativa) e vetro chiaro: stessi strati dei pannelli vetro, sprite della pillola.
                MenuPanelBuilder.BuildGlassLayers(rect, shadowSprite, pill, out UnityEngine.UI.Image shadow, out RoundedPanelGraphic glass);
                glass.DisableSheen();

                RectTransform itemsRoot = BaseBuilder.GetOrCreateRectChild(rect, "PANEL_Items", "Items");
                itemsRoot.SetSiblingIndex(2);
                var items = new List<BottomBarItem>();
                for (int i = 0; i < ITEMS.Length; i++)
                {
                    ItemData data = ITEMS[i];
                    GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(itemsRoot, ITEM_PREFAB_PATH, ITEM_ROOT_NAME + "_" + data.Suffix);
                    instance.transform.SetSiblingIndex(i);
                    var item = instance.GetComponent<BottomBarItem>();
                    item.ConfigureData(data.Screen, ComponentsBuilder.LoadIcon(data.Icon), ComponentsBuilder.LoadIcon(data.Icon + "_Filled"), data.LabelKey);
                    items.Add(item);
                }

                BottomBar bar = BaseBuilder.GetOrAdd<BottomBar>(root);
                bar.Configure(theme, shadow, glass, itemsRoot, items);
                foreach (BottomBarItem item in items) BaseBuilder.RecordInstanceOverrides(item.gameObject);

                PrefabUtility.SaveAsPrefabAsset(root, BAR_PREFAB_PATH, out bool success);
                return success ? $"PANEL_BottomBar: {(exists ? "aggiornato" : "creato")} ({BAR_PREFAB_PATH}), 4 voci." : "PANEL_BottomBar: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Crea o aggiorna l'istanza di PANEL_BottomBar nel posto PANEL_BottomBarSlot di MNG_UI (prefab annidato, posizione
        /// del prefab = layoutHome). Usato da BaseBuilder mentre costruisce MNG_UI.
        /// </summary>
        /// <param name="bottomBarSlot">Posto della bottom bar in CANVAS_Menu.</param>
        internal static void PlaceInSlot(RectTransform bottomBarSlot)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BAR_PREFAB_PATH) == null)
            {
                Debug.LogError("[SI81 UI] PANEL_BottomBar mancante: eseguire SI81/UI/Build Bottom Bar (o Build Base).");
                return;
            }
            BaseBuilder.GetOrCreateChildPrefabInstance(bottomBarSlot, BAR_PREFAB_PATH, BAR_ROOT_NAME);
        }
    }
}
