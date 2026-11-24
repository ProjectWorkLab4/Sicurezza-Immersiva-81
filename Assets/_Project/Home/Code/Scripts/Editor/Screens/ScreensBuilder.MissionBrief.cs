using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "Mission Brief" del builder delle schermate (Blocco B): genera o aggiorna il prefab PANEL_MissionBrief in
    /// Assets/_Project/Home/UI/Prefabs/Screens/. Struttura (FIGMA_SPECS, Mission Brief A/B): PANEL_Side (criteri) a sinistra di
    /// PANEL_Menu 760 x 535 con BTN_Back, percorso, titolo, complessità, pillola "Miglior risultato", PANEL_Description (contesto +
    /// 4 obiettivi su 2 colonne), PANEL_LevelPicker (3 BTN_DifficultyCard 226 x 117) e PANEL_Actions (BTN_Primary 200 x 48 a destra).
    /// Altezze: 24 + 63 + 12 + 185 + 12 + 155 + 12 + 48 + 24 = 535. Sicuro da rieseguire. Usa gli strumenti "B2..." di
    /// ScreensBuilder.Home.cs. Richiede Build Theme, Build Base e Build Components.
    /// </summary>
    internal static partial class ScreensBuilder
    {
        /// <summary>Nome del prefab della schermata Mission Brief.</summary>
        private const string BRIEF_NAME = "PANEL_MissionBrief";
        /// <summary>Padding interno e distanza tra i blocchi della scheda bianca (Figma: 12 e 6).</summary>
        private const float BRIEF_CARD_PADDING = 12f;
        private const float BRIEF_CARD_GAP = 6f;
        /// <summary>Righe di contesto (Figma: 3) e gap tra le righe degli obiettivi (Figma: 4).</summary>
        private const int BRIEF_CONTEXT_LINES = 3;
        private const float BRIEF_OBJECTIVE_ROW_GAP = 4f;
        /// <summary>Gap tra le due colonne degli obiettivi (Figma: 16).</summary>
        private const float BRIEF_OBJECTIVE_COLUMN_GAP = 16f;
        /// <summary>Gap tra il titolo "Scegli la difficoltà" e le card (Figma: 8).</summary>
        private const float BRIEF_PICKER_GAP = 8f;
        /// <summary>Pallini della complessità nell'intestazione (Figma: 10 x 10, gap 4).</summary>
        private const float BRIEF_DOT_SIZE = 10f;
        private const float BRIEF_DOT_GAP = 4f;
        /// <summary>Larghezza del testo "N/5" della complessità.</summary>
        private const float BRIEF_COMPLEXITY_LABEL_WIDTH = 40f;
        /// <summary>Lato dell'icona e gap delle righe dei criteri (Figma: 24 e 6).</summary>
        private const float BRIEF_CRITERION_ICON = 24f;
        private const float BRIEF_CRITERION_GAP = 6f;

        /// <summary>
        /// Crea o aggiorna il prefab PANEL_MissionBrief.
        /// </summary>
        private static void BuildMissionBrief()
        {
            if (!B2TryLoadContext("Mission Brief", out B2Context c)) return;
            string menuPath = MenuPanelBuilder.PREFAB_PATH;
            string sidePath = SidePanelBuilder.PREFAB_PATH;
            string backPath = ComponentsBuilder.PrefabPath("BTN_Back");
            string starsPath = ComponentsBuilder.PrefabPath("LBL_StarRating");
            string levelPath = ComponentsBuilder.PrefabPath("BTN_DifficultyCard");
            string primaryPath = ComponentsBuilder.PrefabPath("BTN_Primary");
            if (!B2PrefabsExist("Mission Brief", menuPath, sidePath, backPath, starsPath, levelPath, primaryPath)) return;
            foreach (string key in new[] { "ui.brief.objective_item", ScenarioCard.COMPLEXITY_KEY, "ui.brief.criterion", "ui.brief.best_result",
                "ui.button.start_scenario", "ui.button.choose_level", "ui.brief.description", "ui.brief.choose_difficulty",
                "ui.outcome.positive_short", "ui.outcome.partial_short", "ui.outcome.negative_short" })
                if (!c.Strings.Contains(key)) Debug.LogError($"[SI81 UI] Mission Brief: chiave \"{key}\" assente in SO_UIStrings_It. Vedere NOTES_B2.md e rieseguire Build Theme.");

            UITheme theme = c.Theme;
            BaseBuilder.EnsureFolder(B2_SCREENS_FOLDER);
            // Sprite della scheda bianca e della pillola (raggio card, famiglia "Card" come BTN_Badge Locked).
            ComponentsBuilder.ShapeSprites card = ComponentsBuilder.EnsureShapeSprites(theme, "Card", theme.RadiusCard);

            GameObject root = B2LoadRoot(BRIEF_NAME, out bool existed);
            try
            {
                UIScreen screen = B2ConfigureScreenRoot(root, UIScreenId.MissionBrief, theme);

                // PANEL_Side (criteri) e PANEL_Menu (535, indietro, percorso): il titolo vero è il nome dello scenario, scritto a runtime.
                GameObject sideGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, sidePath, "PANEL_Side");
                sideGo.transform.SetSiblingIndex(0);
                GameObject menuGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, menuPath, "PANEL_Menu");
                menuGo.transform.SetSiblingIndex(1);
                var menu = menuGo.GetComponent<MenuPanel>();
                MenuPanelBuilder.ConfigureInstance(menu, string.Empty, "ui.brief.breadcrumb", true, theme.GetNumber(NumberTokens.MENU_PANEL_HEIGHT_BRIEF));
                var side = sideGo.GetComponent<SidePanel>();

                // Intestazione.
                BriefBuildBack(menu, backPath);
                BriefBuildComplexity(menu, c, out RectTransform complexityRoot, out CardDots dots, out TMP_Text complexityLabel, out TMP_Text breadcrumb);
                BriefBuildBestPill(menu, c, card, starsPath, out RectTransform pill, out TMP_Text bestLabel, out StarRating bestStars);

                // Contenuti: descrizione, livelli, azioni.
                RectTransform content = menu.ContentRoot;
                float y = 0f;
                BriefBuildDescription(content, c, card, ref y, out TMP_Text context, out TMP_Text[] objectives);
                y += theme.GetNumber(NumberTokens.SPACE_M);
                DifficultyCard[] levelCards = BriefBuildLevelPicker(content, c, levelPath, ref y);
                y += theme.GetNumber(NumberTokens.SPACE_M);
                UIButton start = BriefBuildActions(content, c, primaryPath, y);

                // PANEL_Side: titolo, accanto al menu, con le righe dei criteri.
                BriefBuildCriteria(side, c, out RectTransform[] rows, out TMP_Text[] rowTexts);
                SidePanelBuilder.ConfigureInstance(side, SidePanel.DEFAULT_TITLE_KEY, -1f, (RectTransform)menuGo.transform);

                var brief = BaseBuilder.GetOrAdd<MissionBriefScreen>(root);
                brief.Configure(theme, c.Strings, screen, menu, side, breadcrumb, complexityRoot, dots, complexityLabel, pill, bestLabel,
                    bestStars, context, objectives, levelCards, start, rows, rowTexts);
                EditorUtility.SetDirty(brief);
                B2SaveRoot(root, BRIEF_NAME);
            }
            finally
            {
                B2ReleaseRoot(root, existed);
            }
        }

        // =====================================================================
        // Intestazione
        // =====================================================================

        /// <summary>
        /// BTN_Back nel posto a sinistra dell'intestazione di PANEL_Menu (centrato, 40 x 40).
        /// </summary>
        private static void BriefBuildBack(MenuPanel menu, string backPath)
        {
            GameObject back = BaseBuilder.GetOrCreateChildPrefabInstance(menu.BackSlot, backPath, "BTN_Back");
            var rect = (RectTransform)back.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// PANEL_Complexity (5 pallini 10 x 10 + "N/5") nella colonna del titolo, sulla riga del percorso; la posizione a destra
        /// del percorso la calcola la schermata (dipende dalla larghezza del testo).
        /// </summary>
        private static void BriefBuildComplexity(MenuPanel menu, B2Context c, out RectTransform root, out CardDots dots,
            out TMP_Text label, out TMP_Text breadcrumb)
        {
            UITheme theme = c.Theme;
            Transform column = menu.transform.Find("PANEL_Header/PANEL_TitleColumn");
            if (column == null) Debug.LogError("[SI81 UI] Mission Brief: PANEL_Header/PANEL_TitleColumn non trovato in PANEL_Menu. Eseguire SI81/UI/Build Base.");
            Transform breadcrumbTransform = column != null ? column.Find("LBL_Breadcrumb") : null;
            breadcrumb = breadcrumbTransform != null ? breadcrumbTransform.GetComponent<TMP_Text>() : null;

            float line = PanelLayout.LineHeight(theme.Body);
            float dotsWidth = CardDots.COUNT * BRIEF_DOT_SIZE + (CardDots.COUNT - 1) * BRIEF_DOT_GAP;
            float gap = theme.GetNumber(NumberTokens.SPACE_S);
            root = BaseBuilder.GetOrCreateRectChild(column != null ? column : menu.transform, "PANEL_Complexity");
            CardTextUtil.PlaceTopLeft(root, 0f, 0f, dotsWidth + gap + BRIEF_COMPLEXITY_LABEL_WIDTH, line);

            RectTransform dotsRect = BaseBuilder.GetOrCreateRectChild(root, "IMG_ComplexityDots");
            CardTextUtil.PlaceTopLeft(dotsRect, 0f, (line - BRIEF_DOT_SIZE) * 0.5f, dotsWidth, BRIEF_DOT_SIZE);
            var shapes = new RoundedRectGraphic[CardDots.COUNT];
            for (int i = 0; i < shapes.Length; i++)
            {
                RectTransform dotRect = BaseBuilder.GetOrCreateRectChild(dotsRect, "IMG_Dot" + (i + 1));
                shapes[i] = BaseBuilder.GetOrAdd<RoundedRectGraphic>(dotRect.gameObject);
                shapes[i].raycastTarget = false;
            }
            dots = BaseBuilder.GetOrAdd<CardDots>(dotsRect.gameObject);
            dots.Configure(shapes);
            dots.Layout(BRIEF_DOT_SIZE, BRIEF_DOT_GAP);
            dots.Set(2, theme.GetColor(ColorTokens.LEVEL_BAR_ON_DARK), theme.GetColor(ColorTokens.LEVEL_BAR_OFF_DARK));
            EditorUtility.SetDirty(dots);

            TextMeshProUGUI complexity = B2Text(root, "LBL_Complexity", theme, TextStyleIds.BODY, theme.TextOnDark, TextAlignmentOptions.MidlineLeft, false);
            B2PlaceLine(complexity.rectTransform, dotsWidth + gap, 0f, BRIEF_COMPLEXITY_LABEL_WIDTH, line);
            label = complexity;
        }

        /// <summary>
        /// PANEL_BestPill nel posto a destra: pillola brand.light (raggio card, padding 12/6, gap 8) con "Miglior risultato" Body Bold e
        /// LBL_StarRating. Dimensioni e visibilità le decide la schermata (nascosta se lo scenario non è mai stato giocato).
        /// </summary>
        private static void BriefBuildBestPill(MenuPanel menu, B2Context c, ComponentsBuilder.ShapeSprites card, string starsPath,
            out RectTransform pill, out TMP_Text label, out StarRating stars)
        {
            UITheme theme = c.Theme;
            pill = BaseBuilder.GetOrCreateRectChild(menu.HeaderRightSlot, "PANEL_BestPill");
            pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(1f, 0.5f);
            pill.anchoredPosition = Vector2.zero;
            pill.sizeDelta = new Vector2(280f, 36f);
            pill.localScale = Vector3.one;

            RectTransform backRect = BaseBuilder.GetOrCreateRectChild(pill, "IMG_Pill");
            BaseBuilder.Stretch(backRect);
            backRect.SetSiblingIndex(0);
            var back = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(backRect.gameObject);
            back.Sprite = card.Rounded;
            back.DisableSheen();
            back.color = theme.BrandLight;
            back.raycastTarget = false;

            TextMeshProUGUI best = B2Text(pill, "LBL_Best", theme, TextStyleIds.BODY_BOLD, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, false);
            best.text = c.Strings.Get("ui.brief.best_result");
            label = best;

            GameObject starsGo = BaseBuilder.GetOrCreateChildPrefabInstance(pill, starsPath, "LBL_StarRating");
            stars = starsGo.GetComponent<StarRating>();
            BaseBuilder.RecordInstanceOverrides(starsGo);
        }

        // =====================================================================
        // Contenuti
        // =====================================================================

        /// <summary>
        /// PANEL_Description: scheda bianca (radius.card, shadow.card, padding 12, gap 6) con "Descrizione", il contesto (3 righe)
        /// e 4 obiettivi su due colonne (1 e 3 a sinistra, 2 e 4 a destra).
        /// </summary>
        private static void BriefBuildDescription(RectTransform content, B2Context c, ComponentsBuilder.ShapeSprites shape, ref float y,
            out TMP_Text context, out TMP_Text[] objectives)
        {
            UITheme theme = c.Theme;
            float width = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - theme.PaddingPanel * 2f;
            float titleLine = PanelLayout.LineHeight(theme.Subtitle);
            float bodyLine = PanelLayout.LineHeight(theme.Body);
            float contextHeight = BRIEF_CONTEXT_LINES * bodyLine;
            float height = BRIEF_CARD_PADDING * 2f + titleLine + BRIEF_CARD_GAP + contextHeight + BRIEF_CARD_GAP + bodyLine * 2f + BRIEF_OBJECTIVE_ROW_GAP;

            RectTransform root = BaseBuilder.GetOrCreateRectChild(content, "PANEL_Description");
            CardTextUtil.PlaceTopLeft(root, 0f, y, width, height);
            BriefBuildWhiteCard(root, theme, shape);

            UIStringText title = ComponentsBuilder.AddLabel(root, "LBL_DescTitle", theme, c.Strings, TextStyleIds.SUBTITLE, ColorTokens.TEXT_PRIMARY,
                "ui.brief.description", TextAlignmentOptions.MidlineLeft);
            B2PlaceLine((RectTransform)title.transform, BRIEF_CARD_PADDING, BRIEF_CARD_PADDING, width - BRIEF_CARD_PADDING * 2f, titleLine);

            float contextY = BRIEF_CARD_PADDING + titleLine + BRIEF_CARD_GAP;
            TextMeshProUGUI contextText = B2Text(root, "LBL_Context", theme, TextStyleIds.BODY, theme.TextPrimary, TextAlignmentOptions.TopLeft, true);
            CardTextUtil.PlaceTopLeft(contextText.rectTransform, BRIEF_CARD_PADDING, contextY, width - BRIEF_CARD_PADDING * 2f, contextHeight);
            context = contextText;

            // Obiettivi: due colonne (336 + 16 + 336), righe alte quanto una riga Body, gap 4.
            float columnWidth = (width - BRIEF_CARD_PADDING * 2f - BRIEF_OBJECTIVE_COLUMN_GAP) * 0.5f;
            float objectivesY = contextY + contextHeight + BRIEF_CARD_GAP;
            objectives = new TMP_Text[MissionBriefScreen.MAX_OBJECTIVES];
            for (int i = 0; i < objectives.Length; i++)
            {
                TextMeshProUGUI text = B2Text(root, "LBL_Objective" + (i + 1), theme, TextStyleIds.BODY, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, false);
                int column = i % 2;
                int row = i / 2;
                B2PlaceLine(text.rectTransform, BRIEF_CARD_PADDING + column * (columnWidth + BRIEF_OBJECTIVE_COLUMN_GAP),
                    objectivesY + row * (bodyLine + BRIEF_OBJECTIVE_ROW_GAP), columnWidth, bodyLine);
                objectives[i] = text;
            }
            y += height;
        }

        /// <summary>
        /// Fondo di una scheda bianca: IMG_Shadow (shadow.card, 9-slice, centro bucato) e IMG_Card (surface.card, radius.card), nessun bordo.
        /// </summary>
        private static void BriefBuildWhiteCard(RectTransform root, UITheme theme, ComponentsBuilder.ShapeSprites shape)
        {
            UITheme.ShadowToken token = theme.GetShadow(ShadowRoles.CARD);
            Image shadow = ComponentsBuilder.AddImage(root, "IMG_Shadow", shape.ShadowRest, token != null ? token.Color : Color.clear, false);
            shadow.fillCenter = false;
            float expand = token != null ? PanelSprites.GetShadowLayout(theme.RadiusCard, token).PaddingUnits : 0f;
            PanelLayout.SetRect(shadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
            shadow.transform.SetSiblingIndex(0);

            RectTransform cardRect = BaseBuilder.GetOrCreateRectChild(root, "IMG_Card");
            BaseBuilder.Stretch(cardRect);
            cardRect.SetSiblingIndex(1);
            var card = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(cardRect.gameObject);
            card.Sprite = shape.Rounded;
            card.DisableSheen();
            card.color = theme.SurfaceCard;
            card.raycastTarget = false;
        }

        /// <summary>
        /// PANEL_LevelPicker: "Scegli la difficoltà" (Subtitle Regular on-dark), gap 8 e tre BTN_DifficultyCard 226 x 117 con gap 16.
        /// </summary>
        private static DifficultyCard[] BriefBuildLevelPicker(RectTransform content, B2Context c, string levelPath, ref float y)
        {
            UITheme theme = c.Theme;
            float cardWidth = theme.GetNumber(CardsTokens.DIFFICULTY_WIDTH);
            float cardHeight = theme.GetNumber(CardsTokens.DIFFICULTY_HEIGHT);
            float gap = theme.GapGrid;
            float titleLine = PanelLayout.LineHeight(theme.Subtitle);
            float width = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - theme.PaddingPanel * 2f;
            float height = titleLine + BRIEF_PICKER_GAP + cardHeight;

            RectTransform root = BaseBuilder.GetOrCreateRectChild(content, "PANEL_LevelPicker");
            CardTextUtil.PlaceTopLeft(root, 0f, y, width, height);
            UIStringText title = ComponentsBuilder.AddLabel(root, "LBL_ChooseLevel", theme, c.Strings, TextStyleIds.SUBTITLE, ColorTokens.TEXT_ON_DARK,
                "ui.brief.choose_difficulty", TextAlignmentOptions.MidlineLeft);
            B2PlaceLine((RectTransform)title.transform, 0f, 0f, width, titleLine);

            DifficultyLevel[] levels = { DifficultyLevel.Guided, DifficultyLevel.Operational, DifficultyLevel.Crisis };
            var cards = new DifficultyCard[levels.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                GameObject go = BaseBuilder.GetOrCreateChildPrefabInstance(root, levelPath, "BTN_DifficultyCard_" + levels[i]);
                CardTextUtil.PlaceTopLeft((RectTransform)go.transform, i * (cardWidth + gap), titleLine + BRIEF_PICKER_GAP, cardWidth, cardHeight);
                cards[i] = go.GetComponent<DifficultyCard>();
                cards[i].Set(levels[i], null, false);
                BaseBuilder.RecordInstanceOverrides(go);
            }
            y += height;
            return cards;
        }

        /// <summary>
        /// PANEL_Actions: BTN_Primary Dark 200 x 48 allineato a destra ("Avvia scenario" con ICN_PlayArrow; disabilitato mostra
        /// "Scegli un livello", stato A di Figma).
        /// </summary>
        private static UIButton BriefBuildActions(RectTransform content, B2Context c, string primaryPath, float y)
        {
            UITheme theme = c.Theme;
            float width = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - theme.PaddingPanel * 2f;
            float height = theme.GetNumber(ButtonTokens.HEIGHT);
            RectTransform root = BaseBuilder.GetOrCreateRectChild(content, "PANEL_Actions");
            CardTextUtil.PlaceTopLeft(root, 0f, y, width, height);

            GameObject go = BaseBuilder.GetOrCreateChildPrefabInstance(root, primaryPath, "BTN_Primary");
            var button = go.GetComponent<UIButton>();
            // Chiavi esplicite: etichetta normale e motivo da disabilitato (il campo del motivo non ha setter pubblico).
            var serialized = new SerializedObject(button);
            SerializedProperty disabledKey = serialized.FindProperty("disabledLabelKey");
            if (disabledKey != null) disabledKey.stringValue = "ui.button.choose_level";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            button.SetStyle(UIButtonStyle.Primary, UIButtonSurface.Dark);
            button.SetLabelKey("ui.button.start_scenario");
            button.SetIcon(B2Icon("ICN_PlayArrow"));
            var rect = (RectTransform)go.transform;
            CardTextUtil.PlaceTopLeft(rect, width - rect.rect.width, 0f, rect.rect.width, height);
            BaseBuilder.RecordInstanceOverrides(go);
            return button;
        }

        // =====================================================================
        // PANEL_Side
        // =====================================================================

        /// <summary>
        /// Tre righe dei criteri nel contenuto di PANEL_Side (una per esito, in ordine Positivo, Parziale, Negativo): icona 24
        /// (ICN_CheckCircle / ICN_Contrast / ICN_Cancel, text.on-dark) + testo Body on-green a capo, gap 6. Il testo lo scrive la schermata.
        /// </summary>
        private static void BriefBuildCriteria(SidePanel side, B2Context c, out RectTransform[] rows, out TMP_Text[] texts)
        {
            UITheme theme = c.Theme;
            string[] icons = { "ICN_CheckCircle", "ICN_Contrast", "ICN_Cancel" };
            string[] names = { "PANEL_CriterionPositive", "PANEL_CriterionPartial", "PANEL_CriterionNegative" };
            rows = new RectTransform[names.Length];
            texts = new TMP_Text[names.Length];
            float rowHeight = theme.GetNumber(NumberTokens.SPACE_XL);
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform row = BaseBuilder.GetOrCreateRectChild(side.ContentRoot, names[i]);
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.sizeDelta = new Vector2(0f, rowHeight);
                row.localScale = Vector3.one;
                row.SetSiblingIndex(i);
                rows[i] = row;

                Image icon = ComponentsBuilder.AddIcon(row, "ICN_Outcome", theme, icons[i], BRIEF_CRITERION_ICON, ColorTokens.TEXT_ON_DARK);
                CardTextUtil.PlaceTopLeft(icon.rectTransform, 0f, 0f, BRIEF_CRITERION_ICON, BRIEF_CRITERION_ICON);

                TextMeshProUGUI text = B2Text(row, "LBL_Criterion", theme, TextStyleIds.BODY, theme.TextOnGreen, TextAlignmentOptions.MidlineLeft, true);
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.pivot = new Vector2(0f, 0.5f);
                text.rectTransform.offsetMin = new Vector2(BRIEF_CRITERION_ICON + BRIEF_CRITERION_GAP, 0f);
                text.rectTransform.offsetMax = Vector2.zero;
                texts[i] = text;
            }
        }
    }
}
