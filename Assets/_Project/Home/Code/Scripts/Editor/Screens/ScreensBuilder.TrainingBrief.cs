using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "Training brief" del builder delle schermate (Blocco B): crea o aggiorna il prefab PANEL_TrainingBrief in Prefabs/Screens/.
    /// Struttura: UIScreen (id TrainingBrief) + TrainingBriefScreen; PANEL_Menu (vetro, percorso ui.training.breadcrumb, titolo
    /// ui.training.title) con BTN_Back nel posto dell'indietro; PANEL_Learn (scheda bianca: titolo "Cosa imparerai" + 5 voci 336×40 su
    /// 2 colonne, che sono anche la panoramica dei comandi); nota "nessun punteggio" e BTN_Primary "Avvia training" in basso a destra.
    /// Sicuro da rieseguire: figli cercati per nome, stessi GUID.
    /// </summary>
    internal static partial class ScreensBuilder
    {
        /// <summary>Percorso del prefab PANEL_TrainingBrief.</summary>
        private const string TRAINING_SCREEN_PREFAB = BaseBuilder.PREFABS_FOLDER + "/Screens/PANEL_TrainingBrief.prefab";
        /// <summary>Nome della radice.</summary>
        private const string TRAINING_SCREEN_NAME = "PANEL_TrainingBrief";
        /// <summary>Cerchio 40 (TEX_ControlCircle, generato dalle righe di Impostazioni) per lo sfondo delle icone delle voci.</summary>
        private const string TRAINING_CIRCLE_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_ControlCircle.png";
        /// <summary>Sprite del fondo delle card (TEX_CardRounded, Blocco A · Card).</summary>
        private const string TRAINING_CARD_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_CardRounded.png";
        /// <summary>Sprite dell'ombra a riposo delle card (TEX_CardShadowRest, Blocco A · Card).</summary>
        private const string TRAINING_CARD_SHADOW_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_CardShadowRest.png";
        /// <summary>Lato delle voci (Figma: 336 × 40, icona tonda 40).</summary>
        private const float TRAINING_ITEM_WIDTH = 336f;
        /// <summary>Altezza delle voci.</summary>
        private const float TRAINING_ITEM_HEIGHT = 40f;
        /// <summary>Spazio verticale tra le righe di voci (Figma 10).</summary>
        private const float TRAINING_ROW_GAP = 10f;

        /// <summary>Una voce di "Cosa imparerai": nome del figlio, icona e chiave comune (ui.training.&lt;key&gt;.title/desc).</summary>
        private struct TrainingItem
        {
            /// <summary>Nome in PascalCase per il figlio (PANEL_LearnItem&lt;Name&gt;).</summary>
            public string Name;
            /// <summary>Icona ICN_.</summary>
            public string Icon;
            /// <summary>Parte centrale della chiave (locomotion, turn, grab, select, pause).</summary>
            public string Key;
        }

        /// <summary>Le 5 voci in ordine di lettura (sinistra → destra, poi riga successiva).</summary>
        private static readonly TrainingItem[] TRAINING_ITEMS =
        {
            new TrainingItem { Name = "Locomotion", Icon = "ICN_DirectionsWalk", Key = "locomotion" },
            new TrainingItem { Name = "Turn", Icon = "ICN_RotateRight", Key = "turn" },
            new TrainingItem { Name = "Grab", Icon = "ICN_BackHand", Key = "grab" },
            new TrainingItem { Name = "Select", Icon = "ICN_AdsClick", Key = "select" },
            new TrainingItem { Name = "Pause", Icon = "ICN_PauseCircle", Key = "pause" },
        };

        /// <summary>
        /// Crea o aggiorna PANEL_TrainingBrief e scrive il riepilogo in Console.
        /// </summary>
        private static void BuildTrainingBrief()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] PANEL_TrainingBrief: interrotto, SO_UITheme o SO_UIStrings_It mancanti (eseguire SI81/UI/Build Theme).");
                return;
            }
            string[] required =
            {
                MenuPanelBuilder.PREFAB_PATH, ComponentsBuilder.PrefabPath("BTN_Back"), ComponentsBuilder.PrefabPath("BTN_Primary"), TRAINING_CIRCLE_SPRITE,
                TRAINING_CARD_SPRITE, TRAINING_CARD_SHADOW_SPRITE,
            };
            foreach (string path in required)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) continue;
                Debug.LogError($"[SI81 UI] PANEL_TrainingBrief: interrotto, asset mancante {path} (eseguire SI81/UI/Build Menu Panel e Build Components).");
                return;
            }

            BaseBuilder.EnsureFolder(BaseBuilder.PREFABS_FOLDER + "/Screens");
            // Scheda bianca: riusa gli sprite delle card di Blocco A (nessun duplicato).
            var card = new ComponentsBuilder.ShapeSprites
            {
                Rounded = ComponentsBuilder.LoadSprite(TRAINING_CARD_SPRITE),
                ShadowRest = ComponentsBuilder.LoadSprite(TRAINING_CARD_SHADOW_SPRITE),
            };

            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(TRAINING_SCREEN_PREFAB) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(TRAINING_SCREEN_PREFAB) : new GameObject(TRAINING_SCREEN_NAME, typeof(RectTransform));
            try
            {
                root.name = TRAINING_SCREEN_NAME;
                root.layer = LayerMask.NameToLayer("UI");
                var rootRect = (RectTransform)root.transform;
                BaseBuilder.Stretch(rootRect);
                var screen = BaseBuilder.GetOrAdd<UIScreen>(root);
                screen.Configure(UIScreenId.TrainingBrief, theme);

                // Pannello vetro con percorso + titolo e BTN_Back (altezza di default 475).
                GameObject menuGo = BaseBuilder.GetOrCreateChildPrefabInstance(rootRect, MenuPanelBuilder.PREFAB_PATH, MenuPanelBuilder.ROOT_NAME);
                menuGo.transform.SetSiblingIndex(0);
                var menu = menuGo.GetComponent<MenuPanel>();
                MenuPanelBuilder.ConfigureInstance(menu, "ui.training.title", "ui.training.breadcrumb", true, theme.GetNumber(NumberTokens.MENU_PANEL_HEIGHT));
                TrainingBuildBackButton(menu);

                RectTransform content = menu.ContentRoot;
                float contentWidth = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - 2f * theme.PaddingPanel;
                float bodyLine = PanelLayout.LineHeight(theme.Body);
                float cardBottom = TrainingBuildLearnCard(content, theme, strings, card, contentWidth);

                // Nota "nessun punteggio" (Body on-green, a capo, 2 righe) sotto la scheda, gap space.m.
                float gap = theme.GetNumber(NumberTokens.SPACE_M);
                UIStringText note = ComponentsBuilder.AddLabel(content, "LBL_NoScoreNote", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_GREEN,
                    "ui.training.no_score_note", TextAlignmentOptions.TopLeft);
                var noteText = note.GetComponent<TextMeshProUGUI>();
                noteText.textWrappingMode = TextWrappingModes.Normal;
                EditorUtility.SetDirty(noteText);
                PlaceTopLeft((RectTransform)note.transform, 0f, cardBottom + gap, contentWidth, 2f * bodyLine);

                // BTN_Primary Dark "Avvia training", in basso a destra.
                GameObject startGo = BaseBuilder.GetOrCreateChildPrefabInstance(content, ComponentsBuilder.PrefabPath("BTN_Primary"), "BTN_StartTraining");
                var startRect = (RectTransform)startGo.transform;
                startRect.anchorMin = startRect.anchorMax = startRect.pivot = new Vector2(1f, 0f);
                startRect.anchoredPosition = Vector2.zero;
                startRect.localScale = Vector3.one;
                var startButton = startGo.GetComponent<UIButton>();
                startButton.SetStyle(UIButtonStyle.Primary, UIButtonSurface.Dark);
                startButton.SetIcon(ComponentsBuilder.LoadIcon("ICN_PlayArrow"));
                startButton.SetLabelKey("ui.button.start_training");
                BaseBuilder.RecordInstanceOverrides(startGo);

                var trainingScreen = BaseBuilder.GetOrAdd<TrainingBriefScreen>(root);
                trainingScreen.Configure(startButton);
                EditorUtility.SetDirty(trainingScreen);

                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, TRAINING_SCREEN_PREFAB, out bool success);
                Debug.Log(success
                    ? $"[SI81 UI] PANEL_TrainingBrief: {(exists ? "aggiornato" : "creato")} ({TRAINING_SCREEN_PREFAB})."
                    : "[SI81 UI] PANEL_TrainingBrief: ERRORE nel salvataggio del prefab.");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Mette BTN_Back (istanza di BTN_Back) nel posto dell'indietro di PANEL_Menu, centrato 40 × 40.
        /// </summary>
        private static void TrainingBuildBackButton(MenuPanel menu)
        {
            if (menu.BackSlot == null) return;
            GameObject backGo = BaseBuilder.GetOrCreateChildPrefabInstance(menu.BackSlot, ComponentsBuilder.PrefabPath("BTN_Back"), "BTN_Back");
            var rect = (RectTransform)backGo.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            BaseBuilder.RecordInstanceOverrides(backGo);
        }

        /// <summary>
        /// Crea o aggiorna PANEL_Learn: scheda bianca (ombra card) con titolo e 5 voci su 2 colonne.
        /// </summary>
        /// <returns>Coordinata y (verso il basso, nel contenuto) del bordo inferiore della scheda.</returns>
        private static float TrainingBuildLearnCard(RectTransform content, UITheme theme, UIStrings strings, ComponentsBuilder.ShapeSprites sprites, float contentWidth)
        {
            float padding = theme.GetNumber(NumberTokens.SPACE_M);
            float columnGap = theme.GetNumber(NumberTokens.GAP_GRID);
            float titleLine = PanelLayout.LineHeight(theme.Subtitle);
            float gridTop = padding + titleLine + TRAINING_ROW_GAP;
            int rows = (TRAINING_ITEMS.Length + 1) / 2;
            float cardHeight = gridTop + rows * TRAINING_ITEM_HEIGHT + (rows - 1) * TRAINING_ROW_GAP + padding;

            RectTransform card = BaseBuilder.GetOrCreateRectChild(content, "PANEL_Learn");
            PlaceTopLeft(card, 0f, 0f, contentWidth, cardHeight);
            card.SetSiblingIndex(0);

            // Ombra card (sprite espanso del padding dell'ombra) e fondo bianco.
            UITheme.ShadowToken shadow = theme.GetShadow(ShadowRoles.CARD);
            Image shadowImage = ComponentsBuilder.AddImage(card, "IMG_Shadow", sprites.ShadowRest, shadow.Color, false);
            shadowImage.fillCenter = false;
            float expand = PanelSprites.GetShadowLayout(theme.RadiusCard, shadow).PaddingUnits;
            PanelLayout.SetRect((RectTransform)shadowImage.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
            shadowImage.transform.SetSiblingIndex(0);

            RectTransform fillRect = BaseBuilder.GetOrCreateRectChild(card, "IMG_Fill");
            var fill = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(fillRect.gameObject);
            fill.Sprite = sprites.Rounded;
            fill.DisableSheen();
            fill.color = theme.SurfaceCard;
            fill.raycastTarget = false;
            BaseBuilder.Stretch(fillRect);
            fillRect.SetSiblingIndex(1);

            UIStringText title = ComponentsBuilder.AddLabel(card, "LBL_LearnTitle", theme, strings, TextStyleIds.SUBTITLE, ColorTokens.TEXT_PRIMARY,
                "ui.training.what_you_learn", TextAlignmentOptions.MidlineLeft);
            PlaceTopLeft((RectTransform)title.transform, padding, padding, contentWidth - 2f * padding, titleLine);

            for (int i = 0; i < TRAINING_ITEMS.Length; i++)
            {
                float x = padding + (i % 2) * (TRAINING_ITEM_WIDTH + columnGap);
                float y = gridTop + (i / 2) * (TRAINING_ITEM_HEIGHT + TRAINING_ROW_GAP);
                TrainingBuildLearnItem(card, theme, strings, TRAINING_ITEMS[i], x, y);
            }
            return cardHeight;
        }

        /// <summary>
        /// Crea o aggiorna una voce: cerchio brand.light 40 con icona brand.dark 24 e due righe (titolo Bold + descrizione secondaria).
        /// </summary>
        private static void TrainingBuildLearnItem(RectTransform card, UITheme theme, UIStrings strings, TrainingItem item, float x, float y)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(card, "PANEL_LearnItem" + item.Name);
            PlaceTopLeft(rect, x, y, TRAINING_ITEM_WIDTH, TRAINING_ITEM_HEIGHT);

            RectTransform circleRect = BaseBuilder.GetOrCreateRectChild(rect, "IMG_Icon");
            var circle = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(circleRect.gameObject);
            circle.Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TRAINING_CIRCLE_SPRITE);
            circle.DisableSheen();
            circle.color = theme.BrandLight;
            circle.raycastTarget = false;
            PlaceTopLeft(circleRect, 0f, 0f, TRAINING_ITEM_HEIGHT, TRAINING_ITEM_HEIGHT);
            Image icon = ComponentsBuilder.AddIcon(circleRect, "ICN_" + item.Name, theme, item.Icon, 24f, ColorTokens.BRAND_DARK);
            var iconRect = (RectTransform)icon.transform;
            iconRect.anchoredPosition = Vector2.zero;

            // Testi: due righe Body (23 + 23 = 46 > 40) centrate in verticale sull'icona; larghezza = voce − icona − gap.
            float line = PanelLayout.LineHeight(theme.Body);
            float textX = TRAINING_ITEM_HEIGHT + theme.GetNumber(NumberTokens.SPACE_M);
            float textWidth = TRAINING_ITEM_WIDTH - textX;
            float top = (TRAINING_ITEM_HEIGHT - 2f * line) * 0.5f;
            UIStringText itemTitle = ComponentsBuilder.AddLabel(rect, "LBL_Title", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                $"ui.training.{item.Key}.title", TextAlignmentOptions.MidlineLeft);
            PlaceTopLeft((RectTransform)itemTitle.transform, textX, top, textWidth, line);
            UIStringText itemDesc = ComponentsBuilder.AddLabel(rect, "LBL_Desc", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY,
                $"ui.training.{item.Key}.desc", TextAlignmentOptions.MidlineLeft);
            PlaceTopLeft((RectTransform)itemDesc.transform, textX, top + line, textWidth, line);
        }
    }
}
