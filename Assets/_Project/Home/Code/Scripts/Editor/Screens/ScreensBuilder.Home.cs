using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "Home" del builder delle schermate (Blocco B): genera o aggiorna il prefab PANEL_Home in
    /// Assets/_Project/Home/UI/Prefabs/Screens/. Struttura: PANEL_Menu (istanza, titolo "Scenari", senza indietro, 475) con nel
    /// contenuto PANEL_TrainingCard (712 x 56) e PANEL_ScenarioGrid (4 x 2 BTN_ScenarioCard 166 x 140, gap 16) e, nel posto a
    /// destra dell'intestazione, PANEL_Pager (due frecce 40 x 40 + "N/M"). Sicuro da rieseguire (figli cercati per nome, stessi GUID).
    /// Richiede SI81/UI/Build Theme, Build Base e Build Components. Contiene anche gli strumenti comuni "B2..." usati dal
    /// builder di Mission Brief (ScreensBuilder.MissionBrief.cs).
    /// </summary>
    internal static partial class ScreensBuilder
    {
        /// <summary>Cartella dei prefab delle schermate.</summary>
        private const string B2_SCREENS_FOLDER = BaseBuilder.PREFABS_FOLDER + "/Screens";
        /// <summary>Altezza dei rettangoli dei testi di una riga (la riga naturale di Jaldi è più alta di quella Figma: TMP non nasconde il testo).</summary>
        private const float B2_TEXT_BOX_HEIGHT = 32f;
        /// <summary>Nome del prefab della schermata Home.</summary>
        private const string HOME_NAME = "PANEL_Home";
        /// <summary>Colonne e righe della griglia degli scenari (Figma: 4 x 2).</summary>
        private const int HOME_COLUMNS = 4;
        private const int HOME_ROWS = 2;
        /// <summary>Larghezza del testo "N/M" tra le frecce di pagina.</summary>
        private const float HOME_PAGE_LABEL_WIDTH = 48f;

        /// <summary>Tema, testi e stile dei livelli già caricati.</summary>
        private struct B2Context
        {
            /// <summary>Tema comune SO_UITheme.</summary>
            public UITheme Theme;
            /// <summary>Tabella testi SO_UIStrings_It.</summary>
            public UIStrings Strings;
            /// <summary>Stili dei livelli SO_DifficultyStyle.</summary>
            public DifficultyStyle Difficulty;
        }

        /// <summary>
        /// Crea o aggiorna il prefab PANEL_Home.
        /// </summary>
        private static void BuildHome()
        {
            if (!B2TryLoadContext("Home", out B2Context c)) return;
            string menuPath = MenuPanelBuilder.PREFAB_PATH;
            string trainingPath = ComponentsBuilder.PrefabPath("PANEL_TrainingCard");
            string cardPath = ComponentsBuilder.PrefabPath("BTN_ScenarioCard");
            if (!B2PrefabsExist("Home", menuPath, trainingPath, cardPath)) return;
            if (!c.Strings.Contains(HomeScreen.PAGE_KEY))
                Debug.LogError($"[SI81 UI] Home: chiave \"{HomeScreen.PAGE_KEY}\" assente in SO_UIStrings_It (numero di pagina). Vedere NOTES_B2.md e rieseguire Build Theme.");

            BaseBuilder.EnsureFolder(B2_SCREENS_FOLDER);
            GameObject root = B2LoadRoot(HOME_NAME, out bool existed);
            try
            {
                UIScreen screen = B2ConfigureScreenRoot(root, UIScreenId.Home, c.Theme);

                // PANEL_Menu: "Scenari", nessun indietro, altezza 475.
                GameObject menuGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, menuPath, "PANEL_Menu");
                menuGo.transform.SetSiblingIndex(0);
                var menu = menuGo.GetComponent<MenuPanel>();
                MenuPanelBuilder.ConfigureInstance(menu, "ui.home.title", string.Empty, false, c.Theme.GetNumber(NumberTokens.MENU_PANEL_HEIGHT));
                RectTransform content = menu.ContentRoot;

                // Striscia del Training in cima al contenuto.
                float trainingHeight = c.Theme.GetNumber(CardsTokens.TRAINING_HEIGHT);
                GameObject trainingGo = BaseBuilder.GetOrCreateChildPrefabInstance(content, trainingPath, "PANEL_TrainingCard");
                CardTextUtil.PlaceTopLeft((RectTransform)trainingGo.transform, 0f, 0f, c.Theme.GetNumber(CardsTokens.TRAINING_WIDTH), trainingHeight);
                var training = trainingGo.GetComponent<TrainingCard>();

                // Griglia 4 x 2 (gap.grid) a space.m sotto il Training.
                float cardWidth = c.Theme.GetNumber(CardsTokens.SCENARIO_WIDTH);
                float cardHeight = c.Theme.GetNumber(CardsTokens.SCENARIO_HEIGHT);
                float gap = c.Theme.GapGrid;
                RectTransform grid = BaseBuilder.GetOrCreateRectChild(content, "PANEL_ScenarioGrid");
                CardTextUtil.PlaceTopLeft(grid, 0f, trainingHeight + c.Theme.GetNumber(NumberTokens.SPACE_M),
                    HOME_COLUMNS * cardWidth + (HOME_COLUMNS - 1) * gap, HOME_ROWS * cardHeight + (HOME_ROWS - 1) * gap);
                var cards = new ScenarioCard[HOME_COLUMNS * HOME_ROWS];
                for (int i = 0; i < cards.Length; i++)
                {
                    GameObject cardGo = BaseBuilder.GetOrCreateChildPrefabInstance(grid, cardPath, "BTN_ScenarioCard" + (i + 1));
                    CardTextUtil.PlaceTopLeft((RectTransform)cardGo.transform, (i % HOME_COLUMNS) * (cardWidth + gap), (i / HOME_COLUMNS) * (cardHeight + gap), cardWidth, cardHeight);
                    cardGo.transform.SetSiblingIndex(i);
                    cards[i] = cardGo.GetComponent<ScenarioCard>();
                }

                // Frecce di pagina nel posto a destra dell'intestazione.
                HomeBuildPager(menu.HeaderRightSlot, c, out GameObject pager, out Button previous, out Button next, out TMP_Text pageLabel);

                var home = BaseBuilder.GetOrAdd<HomeScreen>(root);
                home.Configure(c.Strings, screen, training, cards, pager, previous, next, pageLabel);
                EditorUtility.SetDirty(home);
                B2SaveRoot(root, HOME_NAME);
            }
            finally
            {
                B2ReleaseRoot(root, existed);
            }
        }

        // =====================================================================
        // Frecce di pagina
        // =====================================================================

        /// <summary>
        /// PANEL_Pager: [BTN_PagePrevious] LBL_Page [BTN_PageNext], ancorato a destra e centrato nell'intestazione.
        /// </summary>
        private static void HomeBuildPager(RectTransform slot, B2Context c, out GameObject pager, out Button previous, out Button next, out TMP_Text pageLabel)
        {
            float side = c.Theme.GetNumber(ButtonTokens.BACK);
            float gap = c.Theme.GetNumber(NumberTokens.SPACE_S);
            float width = side * 2f + HOME_PAGE_LABEL_WIDTH + gap * 2f;
            RectTransform pagerRect = BaseBuilder.GetOrCreateRectChild(slot, "PANEL_Pager");
            pagerRect.anchorMin = pagerRect.anchorMax = new Vector2(1f, 0.5f);
            pagerRect.pivot = new Vector2(1f, 0.5f);
            pagerRect.anchoredPosition = Vector2.zero;
            pagerRect.sizeDelta = new Vector2(width, side);
            pagerRect.localScale = Vector3.one;
            pager = pagerRect.gameObject;

            ComponentsBuilder.ShapeSprites shape = ComponentsBuilder.EnsureShapeSprites(c.Theme, "PageArrow", side * 0.5f);
            previous = HomeBuildArrow(pagerRect, "BTN_PagePrevious", "ICN_ChevronLeft", 0f, side, c.Theme, shape);
            UIStringText label = ComponentsBuilder.AddLabel(pagerRect, "LBL_Page", c.Theme, c.Strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_DARK,
                string.Empty, TextAlignmentOptions.Center);
            B2PlaceLine((RectTransform)label.transform, side + gap, (side - PanelLayout.LineHeight(c.Theme.Body)) * 0.5f, HOME_PAGE_LABEL_WIDTH, PanelLayout.LineHeight(c.Theme.Body));
            pageLabel = label.GetComponent<TMP_Text>();
            next = HomeBuildArrow(pagerRect, "BTN_PageNext", "ICN_ChevronRight", side + gap + HOME_PAGE_LABEL_WIDTH + gap, side, c.Theme, shape);
        }

        /// <summary>
        /// Freccia di pagina 40 x 40: cerchio tile.neutral con ICN_Chevron 24 (come le frecce di PANEL_SettingsSelector),
        /// hover scala 1,04 + ombra, pressed 0,98, nessun bordo (CLAUDE.md 4.1).
        /// </summary>
        private static Button HomeBuildArrow(RectTransform parent, string name, string iconName, float x, float side, UITheme theme, ComponentsBuilder.ShapeSprites shape)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            CardTextUtil.PlaceTopLeft(rect, x, 0f, side, side);
            ComponentsBuilder.AddStateVisual(rect.gameObject, theme, shape, side * 0.5f, false, false, false, false, false);
            Image fill = ComponentsBuilder.AddImage(rect, "IMG_Fill", shape.Rounded, theme.TileNeutral, true);
            BaseBuilder.Stretch(fill.rectTransform);
            Image icon = ComponentsBuilder.AddIcon(rect, "ICN_Chevron", theme, iconName, theme.GetNumber(ButtonTokens.ICON), ColorTokens.TEXT_PRIMARY);
            var button = BaseBuilder.GetOrAdd<Button>(rect.gameObject);
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            EditorUtility.SetDirty(icon);
            return button;
        }

        // =====================================================================
        // Strumenti comuni delle schermate (usati anche da Mission Brief)
        // =====================================================================

        /// <summary>
        /// Carica tema, testi e stile dei livelli; segnala in italiano se manca qualcosa.
        /// </summary>
        /// <param name="screenName">Nome della schermata (per i messaggi).</param>
        /// <param name="context">Dati caricati.</param>
        /// <returns>true se tutto è presente.</returns>
        private static bool B2TryLoadContext(string screenName, out B2Context context)
        {
            context = new B2Context
            {
                Theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET),
                Strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET),
                Difficulty = AssetDatabase.LoadAssetAtPath<DifficultyStyle>(ThemeBuilder.DIFFICULTY_ASSET)
            };
            if (context.Theme != null && context.Strings != null && context.Difficulty != null) return true;
            Debug.LogError($"[SI81 UI] {screenName}: interrotto, SO_UITheme / SO_UIStrings_It / SO_DifficultyStyle mancanti. Eseguire SI81/UI/Build Theme.");
            return false;
        }

        /// <summary>
        /// Controlla che i prefab annidati esistano; altrimenti scrive un errore in italiano.
        /// </summary>
        /// <param name="screenName">Nome della schermata (per i messaggi).</param>
        /// <param name="paths">Percorsi dei prefab richiesti.</param>
        /// <returns>true se esistono tutti.</returns>
        private static bool B2PrefabsExist(string screenName, params string[] paths)
        {
            bool ok = true;
            foreach (string path in paths)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
                Debug.LogError($"[SI81 UI] {screenName}: prefab mancante {path}. Eseguire prima SI81/UI/Build Base e SI81/UI/Build Components.");
                ok = false;
            }
            return ok;
        }

        /// <summary>
        /// Carica il contenuto del prefab della schermata (stesso GUID) o crea la radice di uno nuovo.
        /// </summary>
        /// <param name="prefabName">Nome del prefab (= nome della radice).</param>
        /// <param name="existed">true se il prefab esisteva già.</param>
        /// <returns>La radice da modificare; chiudere con <see cref="B2SaveRoot"/> e <see cref="B2ReleaseRoot"/>.</returns>
        private static GameObject B2LoadRoot(string prefabName, out bool existed)
        {
            string path = B2_SCREENS_FOLDER + "/" + prefabName + ".prefab";
            existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(prefabName, typeof(RectTransform));
            root.name = prefabName;
            root.layer = LayerMask.NameToLayer("UI");
            return root;
        }

        /// <summary>
        /// Salva la radice come prefab della schermata.
        /// </summary>
        /// <param name="root">Radice ottenuta da <see cref="B2LoadRoot"/>.</param>
        /// <param name="prefabName">Nome del prefab.</param>
        private static void B2SaveRoot(GameObject root, string prefabName)
        {
            string path = B2_SCREENS_FOLDER + "/" + prefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (success) Debug.Log($"[SI81 UI] {prefabName}: salvato ({path}).");
            else Debug.LogError($"[SI81 UI] {prefabName}: ERRORE nel salvataggio del prefab.");
        }

        /// <summary>
        /// Libera la memoria della radice (scarica il contenuto caricato, oppure distrugge la radice appena creata).
        /// </summary>
        /// <param name="root">Radice.</param>
        /// <param name="existed">Valore restituito da <see cref="B2LoadRoot"/>.</param>
        private static void B2ReleaseRoot(GameObject root, bool existed)
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Radice di una schermata: allargata al contenitore PANEL_Screens, CanvasGroup + UIScreen con id e tema.
        /// </summary>
        /// <param name="root">Radice del prefab.</param>
        /// <param name="id">Identificativo della schermata.</param>
        /// <param name="theme">Tema.</param>
        /// <returns>Il componente UIScreen.</returns>
        private static UIScreen B2ConfigureScreenRoot(GameObject root, UIScreenId id, UITheme theme)
        {
            BaseBuilder.Stretch((RectTransform)root.transform);
            BaseBuilder.GetOrAdd<CanvasGroup>(root);
            var screen = BaseBuilder.GetOrAdd<UIScreen>(root);
            screen.Configure(id, theme);
            EditorUtility.SetDirty(screen);
            return screen;
        }

        /// <summary>
        /// Crea o aggiorna un testo TMP decorativo (nessun raycast) con lo stile e il colore del tema; il testo vero lo scrive la schermata.
        /// </summary>
        /// <param name="parent">Genitore.</param>
        /// <param name="name">Nome (prefisso LBL_).</param>
        /// <param name="theme">Tema.</param>
        /// <param name="style">Stile di testo (TextStyleIds).</param>
        /// <param name="color">Colore.</param>
        /// <param name="alignment">Allineamento.</param>
        /// <param name="wrap">true = a capo.</param>
        /// <returns>Il testo.</returns>
        private static TextMeshProUGUI B2Text(Transform parent, string name, UITheme theme, string style, Color color, TextAlignmentOptions alignment, bool wrap)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var tmp = BaseBuilder.GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            tmp.alignment = alignment;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            CardTextUtil.Style(tmp, theme, style, color);
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        /// <summary>
        /// Posiziona un testo di una riga alta <paramref name="lineHeight"/> (altezza Figma) che parte a <paramref name="lineTop"/>:
        /// il rettangolo è più alto e centrato sulla riga.
        /// </summary>
        /// <param name="rect">Testo.</param>
        /// <param name="x">Distanza dal bordo sinistro.</param>
        /// <param name="lineTop">Distanza dal bordo superiore della riga.</param>
        /// <param name="width">Larghezza.</param>
        /// <param name="lineHeight">Altezza della riga.</param>
        private static void B2PlaceLine(RectTransform rect, float x, float lineTop, float width, float lineHeight)
        {
            CardTextUtil.PlaceTopLeft(rect, x, lineTop - (B2_TEXT_BOX_HEIGHT - lineHeight) * 0.5f, width, B2_TEXT_BOX_HEIGHT);
        }

        /// <summary>
        /// Carica lo sprite di un'icona ICN_ (errore in italiano se manca).
        /// </summary>
        /// <param name="iconName">Nome, es. ICN_CheckCircle.</param>
        /// <returns>Lo sprite o null.</returns>
        private static Sprite B2Icon(string iconName)
        {
            return ComponentsBuilder.LoadIcon(iconName);
        }
    }
}
