using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "card" del builder dei componenti (Blocco A): genera o aggiorna i prefab BTN_ScenarioCard, PANEL_TrainingCard
    /// e BTN_DifficultyCard in Assets/_Project/Home/UI/Prefabs/Components/ e gli sprite TEX_Cards* (ombre e glow 9-slice).
    /// Sicuro da rieseguire: carica il prefab esistente, trova i figli per nome, non duplica nulla e mantiene i GUID.
    /// Ordine richiesto da <c>BuildAll</c>: prima BuildButtons e BuildLabels (BTN_Primary e LBL_LevelBadge vengono annidati
    /// se esistono), poi BuildCards. Richiede SI81/UI/Build Theme. Nessun bordo su nessuno stato (CLAUDE.md 4.1).
    /// </summary>
    internal static partial class ComponentsBuilder
    {
        /// <summary>Cartella dei prefab dei componenti (condivisa con le altre famiglie del Blocco A).</summary>
        private const string CARDS_PREFAB_FOLDER = BaseBuilder.PREFABS_FOLDER + "/Components";
        /// <summary>Sprite arrotondato con raggio radius.card (stesso file delle etichette: contenuto identico).</summary>
        private const string CARDS_ROUNDED_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_CardRounded.png";
        /// <summary>Ombra card a riposo (geometria del ruolo card).</summary>
        private const string CARDS_SHADOW_REST_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_CardShadowRest.png";
        /// <summary>Ombra di sollevamento (geometria del ruolo selected.lift).</summary>
        private const string CARDS_SHADOW_LIFT_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_CardShadowLift.png";
        /// <summary>Glow brand.light della selezione (geometria del ruolo selected.glow).</summary>
        private const string CARDS_GLOW_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_CardGlow.png";
        /// <summary>Ombra del cerchio bianco con la spunta (ruolo check.badge, forma circolare).</summary>
        private const string CARDS_CHECK_SHADOW_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_CardsCheckShadow.png";
        /// <summary>Prefab di BTN_Primary (annidato nel PANEL_TrainingCard se esiste).</summary>
        private const string CARDS_PRIMARY_PREFAB = CARDS_PREFAB_FOLDER + "/BTN_Primary.prefab";
        /// <summary>Prefab di LBL_LevelBadge (annidato in BTN_DifficultyCard se esiste).</summary>
        private const string CARDS_LEVEL_BADGE_PREFAB = CARDS_PREFAB_FOLDER + "/LBL_LevelBadge.prefab";
        /// <summary>Chiave dell'etichetta del pulsante del training.</summary>
        private const string CARDS_START_TRAINING_KEY = "ui.button.start_training";

        /// <summary>Dati comuni a tutti i prefab delle card.</summary>
        private struct CardsContext
        {
            /// <summary>Tema comune SO_UITheme.</summary>
            public UITheme Theme;
            /// <summary>Tabella testi SO_UIStrings_It.</summary>
            public UIStrings Strings;
            /// <summary>Stili dei livelli SO_DifficultyStyle.</summary>
            public DifficultyStyle Difficulty;
            /// <summary>Sprite a pillola.</summary>
            public Sprite Pill;
            /// <summary>Sprite della card.</summary>
            public Sprite Card;
            /// <summary>Sprite dell'ombra a riposo.</summary>
            public Sprite ShadowRest;
            /// <summary>Sprite dell'ombra sollevata.</summary>
            public Sprite ShadowLift;
            /// <summary>Sprite del bagliore.</summary>
            public Sprite Glow;
            /// <summary>Sprite dell'ombra della spunta.</summary>
            public Sprite CheckShadow;
        }

        /// <summary>Riferimenti dell'involucro comune di una card (ombra, glow, riempimento, stati).</summary>
        private struct CardsChrome
        {
            /// <summary>Contenitore visivo (scala, ombra, stati).</summary>
            public RectTransform Visual;
            /// <summary>Componente Button del prefab.</summary>
            public Button Button;
            /// <summary>Componente degli stati visivi.</summary>
            public UIStateVisual State;
        }

        /// <summary>
        /// Crea o aggiorna gli sprite TEX_Cards* e i 3 prefab delle card. Chiamato da <c>BuildAll</c> di ComponentsBuilder.
        /// </summary>
        private static void BuildCards()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultyStyle>(ThemeBuilder.DIFFICULTY_ASSET);
            if (theme == null || strings == null || difficulty == null)
            {
                Debug.LogError("[SI81 UI] Card: interrotto, SO_UITheme / SO_UIStrings_It / SO_DifficultyStyle mancanti. Eseguire SI81/UI/Build Theme.");
                return;
            }
            UITheme.ShadowToken rest = theme.GetShadow(ShadowRoles.CARD);
            UITheme.ShadowToken lift = theme.GetShadow(ShadowRoles.SELECTED_LIFT);
            UITheme.ShadowToken glow = theme.GetShadow(ShadowRoles.SELECTED_GLOW);
            UITheme.ShadowToken check = theme.GetShadow(ShadowRoles.CHECK_BADGE);
            if (rest == null || lift == null || glow == null || check == null)
            {
                Debug.LogError("[SI81 UI] Card: interrotto, ombre card / selected.lift / selected.glow / check.badge assenti in SO_UITheme.");
                return;
            }
            BaseBuilder.EnsureFolder(CARDS_PREFAB_FOLDER);

            // Sprite: card arrotondata e ombre 9-slice (il colore e l'alpha li dà Image.color dal tema).
            SpriteBuilder.BuildRoundedSprite(CARDS_ROUNDED_SPRITE, theme.RadiusCard);
            SpriteBuilder.BuildShadowSprite(CARDS_SHADOW_REST_SPRITE, theme.RadiusCard, rest);
            SpriteBuilder.BuildShadowSprite(CARDS_SHADOW_LIFT_SPRITE, theme.RadiusCard, lift);
            SpriteBuilder.BuildShadowSprite(CARDS_GLOW_SPRITE, theme.RadiusCard, glow);
            SpriteBuilder.BuildShadowSprite(CARDS_CHECK_SHADOW_SPRITE, theme.GetNumber(CardsTokens.CHECK_BADGE_SIZE) * 0.5f, check);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var context = new CardsContext
            {
                Theme = theme,
                Strings = strings,
                Difficulty = difficulty,
                Pill = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.PILL_ROUNDED),
                Card = AssetDatabase.LoadAssetAtPath<Sprite>(CARDS_ROUNDED_SPRITE),
                ShadowRest = AssetDatabase.LoadAssetAtPath<Sprite>(CARDS_SHADOW_REST_SPRITE),
                ShadowLift = AssetDatabase.LoadAssetAtPath<Sprite>(CARDS_SHADOW_LIFT_SPRITE),
                Glow = AssetDatabase.LoadAssetAtPath<Sprite>(CARDS_GLOW_SPRITE),
                CheckShadow = AssetDatabase.LoadAssetAtPath<Sprite>(CARDS_CHECK_SHADOW_SPRITE)
            };
            if (context.Pill == null || context.Card == null || context.ShadowRest == null || context.ShadowLift == null
                || context.Glow == null || context.CheckShadow == null)
            {
                Debug.LogError("[SI81 UI] Card: interrotto, sprite TEX_PillRounded / TEX_CardRounded / TEX_Cards* mancanti.");
                return;
            }

            CardsBuildPrefab("BTN_ScenarioCard", new Vector2(theme.GetNumber(CardsTokens.SCENARIO_WIDTH), theme.GetNumber(CardsTokens.SCENARIO_HEIGHT)),
                context, CardsBuildScenario);
            CardsBuildPrefab("PANEL_TrainingCard", new Vector2(theme.GetNumber(CardsTokens.TRAINING_WIDTH), theme.GetNumber(CardsTokens.TRAINING_HEIGHT)),
                context, CardsBuildTraining);
            CardsBuildPrefab("BTN_DifficultyCard", new Vector2(theme.GetNumber(CardsTokens.DIFFICULTY_WIDTH), theme.GetNumber(CardsTokens.DIFFICULTY_HEIGHT)),
                context, CardsBuildDifficulty);
            Debug.Log("[SI81 UI] Card: 3 prefab creati/aggiornati in " + CARDS_PREFAB_FOLDER + ".");
        }

        // =====================================================================
        // Scheletro del prefab e helper
        // =====================================================================

        /// <summary>
        /// Carica (o crea) il prefab, imposta nome, layer, ancore in alto a sinistra e dimensione della radice, chiama il
        /// costruttore del contenuto e salva.
        /// </summary>
        private static void CardsBuildPrefab(string name, Vector2 size, CardsContext context, Action<GameObject, CardsContext> buildContent)
        {
            string path = CARDS_PREFAB_FOLDER + "/" + name + ".prefab";
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name, typeof(RectTransform));
            if (exists) RemoveMissingScripts(root);
            try
            {
                root.name = name;
                root.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = size;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                buildContent(root, context);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success) Debug.LogError($"[SI81 UI] {name}: ERRORE nel salvataggio del prefab.");
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Involucro comune: Image trasparente (bersaglio dei raggi, non scala) + Button senza transizione + UIStateVisual,
        /// e PANEL_Visual (scalato) con IMG_Glow (opzionale), IMG_Shadow e IMG_Card. Niente bordi.
        /// </summary>
        private static CardsChrome CardsBuildChrome(GameObject root, CardsContext c, bool withGlow, bool selectable)
        {
            // Area cliccabile: alla radice, non scalata (così l'hover non "balla" sul bordo).
            var hit = BaseBuilder.GetOrAdd<Image>(root);
            hit.sprite = null;
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var button = BaseBuilder.GetOrAdd<Button>(root);
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            RectTransform visual = BaseBuilder.GetOrCreateRectChild(root.transform, "PANEL_Visual");
            BaseBuilder.Stretch(visual);
            visual.SetSiblingIndex(0);

            // Glow (solo selezionabili), ombra e riempimento della card, in quest'ordine dal fondo.
            Image glow = null;
            if (withGlow)
            {
                glow = CardsShadowImage(visual, "IMG_Glow", c.Glow, c.Theme.GetShadow(ShadowRoles.SELECTED_GLOW), c.Theme, true);
                glow.rectTransform.SetSiblingIndex(0);
            }
            Image shadow = CardsShadowImage(visual, "IMG_Shadow", c.ShadowRest, c.Theme.GetShadow(ShadowRoles.CARD), c.Theme, false);
            shadow.rectTransform.SetSiblingIndex(withGlow ? 1 : 0);
            RectTransform cardRect = BaseBuilder.GetOrCreateRectChild(visual, "IMG_Card");
            BaseBuilder.Stretch(cardRect);
            cardRect.SetSiblingIndex(withGlow ? 2 : 1);
            var card = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(cardRect.gameObject);
            card.Sprite = c.Card;
            card.DisableSheen();
            card.color = c.Theme.SurfaceCard;
            card.raycastTarget = false;

            var state = BaseBuilder.GetOrAdd<UIStateVisual>(root);
            state.Theme = c.Theme;
            state.ScaleTarget = visual;
            state.Shadow = shadow;
            state.Glow = glow;
            state.ScaleOnHover = true;
            state.Selectable = selectable;
            state.ShadowAtRest = true;
            state.ShadowRestSprite = c.ShadowRest;
            state.ShadowLiftSprite = c.ShadowLift;
            state.CornerRadius = c.Theme.RadiusCard;
            EditorUtility.SetDirty(state);
            return new CardsChrome { Visual = visual, Button = button, State = state };
        }

        /// <summary>
        /// Crea o aggiorna un'Image 9-slice per ombra/glow (decorativa, centro bucato), allargata del padding dello sprite.
        /// </summary>
        private static Image CardsShadowImage(Transform parent, string name, Sprite sprite, UITheme.ShadowToken token, UITheme theme, bool transparent)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var image = BaseBuilder.GetOrAdd<Image>(rect.gameObject);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.pixelsPerUnitMultiplier = 1f;
            image.raycastTarget = false;
            Color color = token.Color;
            // Il glow parte nascosto (alpha 0): lo mostra UIStateVisual da selezionato.
            if (transparent) color.a = 0f;
            image.color = color;
            float expand = PanelSprites.GetShadowLayout(theme.RadiusCard, token).PaddingUnits;
            PanelLayout.SetRect(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
            return image;
        }

        /// <summary>
        /// Crea o aggiorna un testo TMP decorativo (nessun raycast); lo stile e il colore li applica il componente dal tema.
        /// </summary>
        private static TextMeshProUGUI CardsText(Transform parent, string name, TextAlignmentOptions alignment, bool wrap, TextOverflowModes overflow)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var tmp = BaseBuilder.GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            tmp.alignment = alignment;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = overflow;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        /// <summary>
        /// Crea o aggiorna un'icona (Image decorativa; lo sprite lo assegna il builder o il componente).
        /// </summary>
        private static Image CardsImage(Transform parent, string name, Sprite sprite)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var image = BaseBuilder.GetOrAdd<Image>(rect.gameObject);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Crea o aggiorna una forma a mesh (pallino, barra, cerchio), decorativa.
        /// </summary>
        private static RoundedRectGraphic CardsShape(Transform parent, string name)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var graphic = BaseBuilder.GetOrAdd<RoundedRectGraphic>(rect.gameObject);
            graphic.raycastTarget = false;
            return graphic;
        }

        /// <summary>
        /// Carica lo sprite di un'icona ICN_ per nome; scrive un errore se manca.
        /// </summary>
        private static Sprite CardsIcon(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconBuilder.ICONS_FOLDER + "/" + name + ".png");
            if (sprite == null) Debug.LogError($"[SI81 UI] Icona {name} assente. Eseguire SI81/UI/Build Theme.");
            return sprite;
        }

        /// <summary>
        /// Centro verticale di un elemento alto <paramref name="height"/> dentro un contenitore alto <paramref name="containerHeight"/>.
        /// </summary>
        private static float CardsCenterY(float containerHeight, float height)
        {
            return (containerHeight - height) * 0.5f;
        }

        // =====================================================================
        // BTN_ScenarioCard
        // =====================================================================

        /// <summary>
        /// BTN_ScenarioCard: PANEL_Visual { IMG_Shadow, IMG_Card, PANEL_Media { IMG_Thumbnail, ICN_Scenario }, PANEL_BestPill
        /// { IMG_Pill, ICN_Star, LBL_Best }, PANEL_Body { LBL_Title, IMG_ComplexityDots, LBL_Complexity, IMG_LevelsCompleted } }.
        /// </summary>
        private static void CardsBuildScenario(GameObject root, CardsContext c)
        {
            UITheme theme = c.Theme;
            float width = theme.GetNumber(CardsTokens.SCENARIO_WIDTH);
            float height = theme.GetNumber(CardsTokens.SCENARIO_HEIGHT);
            float thumbHeight = theme.GetNumber(CardsTokens.SCENARIO_THUMBNAIL_HEIGHT);
            CardsChrome chrome = CardsBuildChrome(root, c, false, false);
            Transform visual = chrome.Visual;

            // Immagine: miniatura / gradiente (angoli alti arrotondati) + icona scenario bianca al centro. Gruppo per l'alpha Disabled.
            RectTransform media = BaseBuilder.GetOrCreateRectChild(visual, "PANEL_Media");
            CardTextUtil.PlaceTopLeft(media, 0f, 0f, width, thumbHeight);
            var mediaGroup = BaseBuilder.GetOrAdd<CanvasGroup>(media.gameObject);
            mediaGroup.blocksRaycasts = false;
            RectTransform thumbRect = BaseBuilder.GetOrCreateRectChild(media, "IMG_Thumbnail");
            BaseBuilder.Stretch(thumbRect);
            var thumbnail = BaseBuilder.GetOrAdd<CardImageGraphic>(thumbRect.gameObject);
            thumbnail.SetTopRadius(theme.RadiusCard);
            thumbnail.raycastTarget = false;
            float iconSize = theme.GetNumber(CardsTokens.SCENARIO_ICON);
            Image scenarioIcon = CardsImage(media, "ICN_Scenario", CardsIcon("ICN_LocalFireDepartment"));
            PanelLayout.SetRect(scenarioIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-iconSize * 0.5f, -iconSize * 0.5f), new Vector2(iconSize * 0.5f, iconSize * 0.5f));

            // Pillola "miglior risultato": angolo in alto a destra, larghezza calcolata dal componente.
            float inset = theme.GetNumber(CardsTokens.SCENARIO_PILL_INSET);
            RectTransform pill = BaseBuilder.GetOrCreateRectChild(visual, "PANEL_BestPill");
            pill.anchorMin = pill.anchorMax = new Vector2(1f, 1f);
            pill.pivot = new Vector2(1f, 1f);
            pill.anchoredPosition = new Vector2(-inset, -inset);
            pill.sizeDelta = new Vector2(60f, theme.GetNumber(CardsTokens.SCENARIO_PILL_ICON) + theme.GetNumber(CardsTokens.SCENARIO_PILL_PAD_Y) * 2f);
            RectTransform pillBackRect = BaseBuilder.GetOrCreateRectChild(pill, "IMG_Pill");
            BaseBuilder.Stretch(pillBackRect);
            var pillBackground = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(pillBackRect.gameObject);
            pillBackground.Sprite = c.Pill;
            pillBackground.DisableSheen();
            pillBackground.color = theme.SurfaceCard;
            pillBackground.raycastTarget = false;
            Sprite star = CardsIcon("ICN_Star_Filled");
            Sprite lockSprite = CardsIcon("ICN_Lock");
            Image pillIcon = CardsImage(pill, "ICN_Star", star);
            TextMeshProUGUI pillLabel = CardsText(pill, "LBL_Best", TextAlignmentOptions.MidlineLeft, false, TextOverflowModes.Overflow);

            // Corpo: titolo, riga complessità (pallini + "N/5") e livelli completati.
            float padX = theme.GetNumber(CardsTokens.SCENARIO_BODY_PAD_X);
            float padY = theme.GetNumber(CardsTokens.SCENARIO_BODY_PAD_Y);
            float bodyGap = theme.GetNumber(CardsTokens.SCENARIO_BODY_GAP);
            float rowGap = theme.GetNumber(CardsTokens.SCENARIO_ROW_GAP);
            float dot = theme.GetNumber(CardsTokens.SCENARIO_DOT);
            float dotGap = theme.GetNumber(CardsTokens.SCENARIO_DOT_GAP);
            float levelsIcon = theme.GetNumber(CardsTokens.SCENARIO_LEVELS_ICON);
            float titleLine = PanelLayout.LineHeight(theme.BodyBold);
            float rowLine = PanelLayout.LineHeight(theme.Body);
            float rowCenter = padY + titleLine + bodyGap + rowLine * 0.5f;
            // I rettangoli dei testi sono più alti della riga Figma (riga naturale di Jaldi): TMP non nasconde il testo.
            const float TEXT_BOX_HEIGHT = 32f;

            RectTransform body = BaseBuilder.GetOrCreateRectChild(visual, "PANEL_Body");
            CardTextUtil.PlaceTopLeft(body, 0f, thumbHeight, width, height - thumbHeight);
            TextMeshProUGUI title = CardsText(body, "LBL_Title", TextAlignmentOptions.MidlineLeft, false, TextOverflowModes.Ellipsis);
            CardTextUtil.PlaceTopLeft(title.rectTransform, padX, padY + CardsCenterY(titleLine, TEXT_BOX_HEIGHT), width - padX * 2f, TEXT_BOX_HEIGHT);

            // 5 pallini (52 x 8).
            RectTransform dotsRect = BaseBuilder.GetOrCreateRectChild(body, "IMG_ComplexityDots");
            float dotsWidth = CardDots.COUNT * dot + (CardDots.COUNT - 1) * dotGap;
            CardTextUtil.PlaceTopLeft(dotsRect, padX, rowCenter - dot * 0.5f, dotsWidth, dot);
            var dots = new RoundedRectGraphic[CardDots.COUNT];
            for (int i = 0; i < dots.Length; i++) dots[i] = CardsShape(dotsRect, "IMG_Dot" + (i + 1));
            var cardDots = BaseBuilder.GetOrAdd<CardDots>(dotsRect.gameObject);
            cardDots.Configure(dots);
            cardDots.Layout(dot, dotGap);
            EditorUtility.SetDirty(cardDots);

            TextMeshProUGUI complexityLabel = CardsText(body, "LBL_Complexity", TextAlignmentOptions.MidlineLeft, false, TextOverflowModes.Overflow);
            float complexityX = padX + dotsWidth + rowGap;
            float levelsX = width - padX - levelsIcon;
            CardTextUtil.PlaceTopLeft(complexityLabel.rectTransform, complexityX, rowCenter - TEXT_BOX_HEIGHT * 0.5f, levelsX - rowGap - complexityX, TEXT_BOX_HEIGHT);

            // Icona a 3 barre dei livelli completati (20 x 20, a destra).
            RectTransform levelsRect = BaseBuilder.GetOrCreateRectChild(body, "IMG_LevelsCompleted");
            CardTextUtil.PlaceTopLeft(levelsRect, levelsX, rowCenter - levelsIcon * 0.5f, levelsIcon, levelsIcon);
            var levelsGroup = BaseBuilder.GetOrAdd<CanvasGroup>(levelsRect.gameObject);
            levelsGroup.blocksRaycasts = false;
            var bars = new RoundedRectGraphic[DifficultyStyle.BAR_COUNT];
            for (int i = 0; i < bars.Length; i++) bars[i] = CardsShape(levelsRect, "IMG_Bar" + (i + 1));
            var levelBars = BaseBuilder.GetOrAdd<CardLevelBars>(levelsRect.gameObject);
            levelBars.Configure(bars);
            levelBars.Layout();
            EditorUtility.SetDirty(levelBars);

            var component = BaseBuilder.GetOrAdd<ScenarioCard>(root);
            component.Configure(theme, c.Strings, c.Difficulty, chrome.Button, chrome.State, mediaGroup, thumbnail, scenarioIcon, pill,
                pillBackground, pillIcon, pillLabel, title, cardDots, complexityLabel, levelsGroup, levelBars, star, lockSprite);
            // Aspetto di default nel prefab (colori e stili del tema); i dati veri arrivano da Set().
            CardsApplyDefaultLook(theme, title, complexityLabel, pillLabel);
            EditorUtility.SetDirty(component);
        }

        /// <summary>
        /// Applica agli elementi di testo lo stile del tema nel prefab (così l'anteprima non è magenta/bianca).
        /// </summary>
        private static void CardsApplyDefaultLook(UITheme theme, TMP_Text title, TMP_Text complexity, TMP_Text pill)
        {
            CardTextUtil.Style(title, theme, TextStyleIds.BODY_BOLD, theme.TextPrimary);
            CardTextUtil.Style(complexity, theme, TextStyleIds.BODY, theme.TextSecondary);
            CardTextUtil.Style(pill, theme, TextStyleIds.BODY, theme.TextPrimary);
        }

        // =====================================================================
        // PANEL_TrainingCard
        // =====================================================================

        /// <summary>
        /// PANEL_TrainingCard: PANEL_Visual { IMG_Shadow, IMG_Card, ICN_School, LBL_Title, LBL_Subtitle, BTN_Primary (annidato) }.
        /// </summary>
        private static void CardsBuildTraining(GameObject root, CardsContext c)
        {
            UITheme theme = c.Theme;
            CardsChrome chrome = CardsBuildChrome(root, c, false, false);
            // Hover 1,02 / pressed 0,99 (Figma): valori dal tema, copiati negli override di UIStateVisual.
            chrome.State.HoverScaleOverride = theme.GetNumber(CardsTokens.TRAINING_HOVER_SCALE);
            chrome.State.PressedScaleOverride = theme.GetNumber(CardsTokens.TRAINING_PRESSED_SCALE);

            Image icon = CardsImage(chrome.Visual, "ICN_School", CardsIcon("ICN_School"));
            TextMeshProUGUI title = CardsText(chrome.Visual, "LBL_Title", TextAlignmentOptions.MidlineLeft, false, TextOverflowModes.Overflow);
            TextMeshProUGUI subtitle = CardsText(chrome.Visual, "LBL_Subtitle", TextAlignmentOptions.MidlineLeft, false, TextOverflowModes.Ellipsis);

            // Pulsante "Avvia training": istanza annidata di BTN_Primary (Light) se il prefab esiste.
            Button startButton = null;
            GameObject primaryInstance = null;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CARDS_PRIMARY_PREFAB) != null)
            {
                GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(chrome.Visual, CARDS_PRIMARY_PREFAB, "BTN_Primary");
                primaryInstance = instance;
                startButton = instance.GetComponent<Button>();
                if (startButton == null) startButton = instance.GetComponentInChildren<Button>(true);
                // Superficie Light: il pulsante sta su una card bianca (bordo border.on-light, API di UIButton).
                UIButton primaryButton = instance.GetComponent<UIButton>();
                if (primaryButton != null)
                {
                    primaryButton.SetSurface(UIButtonSurface.Light);
                    EditorUtility.SetDirty(primaryButton);
                }
                // Etichetta: chiave del training (UIButton la riapplica a runtime, quindi si imposta sul componente, non sul testo).
                if (primaryButton != null)
                {
                    primaryButton.SetLabelKey(CARDS_START_TRAINING_KEY);
                    EditorUtility.SetDirty(primaryButton);
                }
            }
            else
            {
                Debug.LogWarning("[SI81 UI] PANEL_TrainingCard: BTN_Primary non trovato in " + CARDS_PRIMARY_PREFAB
                    + ". Eseguire BuildButtons prima di BuildCards: la striscia resta cliccabile ma senza pulsante.");
            }

            var component = BaseBuilder.GetOrAdd<TrainingCard>(root);
            component.Configure(theme, c.Strings, chrome.Button, chrome.State, icon, title, subtitle, startButton);
            // Apply posiziona anche il pulsante annidato: gli override dell'istanza si registrano dopo.
            component.Apply();
            if (primaryInstance != null) BaseBuilder.RecordInstanceOverrides(primaryInstance);
            EditorUtility.SetDirty(component);
        }

        // =====================================================================
        // BTN_DifficultyCard
        // =====================================================================

        /// <summary>
        /// BTN_DifficultyCard: PANEL_Visual { IMG_Glow, IMG_Shadow, IMG_Card, PANEL_Content { LBL_LevelBadge (annidato),
        /// LBL_Description }, IMG_CheckBadge { IMG_CheckShadow, IMG_CheckCircle, ICN_Check } }.
        /// </summary>
        private static void CardsBuildDifficulty(GameObject root, CardsContext c)
        {
            UITheme theme = c.Theme;
            float width = theme.GetNumber(CardsTokens.DIFFICULTY_WIDTH);
            float height = theme.GetNumber(CardsTokens.DIFFICULTY_HEIGHT);
            float padding = theme.GetNumber(CardsTokens.DIFFICULTY_PADDING);
            float gap = theme.GetNumber(CardsTokens.DIFFICULTY_GAP);
            CardsChrome chrome = CardsBuildChrome(root, c, true, true);

            // Contenuto (gruppo per l'alpha del livello bloccato).
            RectTransform content = BaseBuilder.GetOrCreateRectChild(chrome.Visual, "PANEL_Content");
            BaseBuilder.Stretch(content);
            var contentGroup = BaseBuilder.GetOrAdd<CanvasGroup>(content.gameObject);
            contentGroup.blocksRaycasts = false;

            // Badge livello: istanza annidata di LBL_LevelBadge (superficie Light) se il prefab esiste.
            LevelBadge badge = null;
            float badgeHeight = PanelLayout.LineHeight(theme.Body);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CARDS_LEVEL_BADGE_PREFAB) != null)
            {
                GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(content, CARDS_LEVEL_BADGE_PREFAB, "LBL_LevelBadge");
                badge = instance.GetComponent<LevelBadge>();
                var badgeRect = (RectTransform)instance.transform;
                badgeHeight = badgeRect.rect.height > 0f ? badgeRect.rect.height : badgeHeight;
                CardTextUtil.PlaceTopLeft(badgeRect, padding, padding, badgeRect.rect.width, badgeHeight);
                BaseBuilder.RecordInstanceOverrides(instance);
            }
            else
            {
                Debug.LogError("[SI81 UI] BTN_DifficultyCard: LBL_LevelBadge non trovato in " + CARDS_LEVEL_BADGE_PREFAB
                    + ". Eseguire BuildLabels prima di BuildCards.");
            }

            // Descrizione: Body 18 a capo, sotto il badge.
            float descriptionY = padding + badgeHeight + gap;
            TextMeshProUGUI description = CardsText(content, "LBL_Description", TextAlignmentOptions.TopLeft, true, TextOverflowModes.Overflow);
            CardTextUtil.PlaceTopLeft(description.rectTransform, padding, descriptionY, width - padding * 2f, height - descriptionY - padding);

            // Cerchio bianco 30 x 30 con la spunta piena nel colore del livello, sporgente dall'angolo in alto a destra.
            float badgeSize = theme.GetNumber(CardsTokens.CHECK_BADGE_SIZE);
            float outset = theme.GetNumber(CardsTokens.CHECK_BADGE_OUTSET);
            float checkIconSize = theme.GetNumber(CardsTokens.CHECK_BADGE_ICON);
            RectTransform checkBadge = BaseBuilder.GetOrCreateRectChild(chrome.Visual, "IMG_CheckBadge");
            checkBadge.anchorMin = checkBadge.anchorMax = new Vector2(1f, 1f);
            checkBadge.pivot = new Vector2(1f, 1f);
            checkBadge.anchoredPosition = new Vector2(outset, outset);
            checkBadge.sizeDelta = new Vector2(badgeSize, badgeSize);
            Image checkShadow = CardsShadowImage(checkBadge, "IMG_CheckShadow", c.CheckShadow, theme.GetShadow(ShadowRoles.CHECK_BADGE), theme, false);
            // L'ombra del cerchio ha un raggio diverso dalla card: si ricalcola l'allargamento con il raggio del cerchio.
            float checkExpand = PanelSprites.GetShadowLayout(badgeSize * 0.5f, theme.GetShadow(ShadowRoles.CHECK_BADGE)).PaddingUnits;
            PanelLayout.SetRect(checkShadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-checkExpand, -checkExpand), new Vector2(checkExpand, checkExpand));
            RoundedRectGraphic circle = CardsShape(checkBadge, "IMG_CheckCircle");
            BaseBuilder.Stretch(circle.rectTransform);
            circle.Radius = badgeSize * 0.5f;
            circle.Stroke = 0f;
            circle.color = theme.SurfaceCard;
            Image checkIcon = CardsImage(checkBadge, "ICN_Check", CardsIcon("ICN_CheckCircle_Filled"));
            PanelLayout.SetRect(checkIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-checkIconSize * 0.5f, -checkIconSize * 0.5f), new Vector2(checkIconSize * 0.5f, checkIconSize * 0.5f));
            // Visibile solo da selezionato: lo accende UIStateVisual.
            checkBadge.gameObject.SetActive(false);
            chrome.State.CheckMark = checkBadge.gameObject;
            EditorUtility.SetDirty(chrome.State);

            var component = BaseBuilder.GetOrAdd<DifficultyCard>(root);
            component.Configure(theme, c.Strings, c.Difficulty, chrome.Button, chrome.State, contentGroup, badge, description, checkIcon);
            CardTextUtil.Style(description, theme, TextStyleIds.BODY, theme.TextPrimary);
            component.Set(DifficultyLevel.Guided, null, false);
            EditorUtility.SetDirty(component);
        }
    }
}
