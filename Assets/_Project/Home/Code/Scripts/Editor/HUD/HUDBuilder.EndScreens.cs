using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "schermate di fine" del builder della HUD (Blocco C · C3): crea o aggiorna i prefab PANEL_Pause, PANEL_Debrief,
    /// PANEL_ScoreReplay e PANEL_TrainingComplete in Assets/_Project/Home/UI/Prefabs/Screens/ (<see cref="BuildEndScreens"/>).
    /// Usa gli helper comuni di <c>ScreensBuilder</c> (radice schermata, PANEL_Menu, PANEL_Side, istanze dei componenti, salvataggio);
    /// i suoi helper privati iniziano con "End". Create-or-update sul posto (stessi GUID, figli cercati per nome), nessun YAML a mano.
    /// Per l'inserimento sotto MNG_UI/CANVAS_Menu/PANEL_Screens vedi NOTES_C3.md (BaseBuilder.RealScreenPrefabName + MissionFlowDriver).
    /// </summary>
    internal static partial class HUDBuilder
    {
        /// <summary>Sprite del cerchio (TEX_ControlCircle, generato dalle righe di Impostazioni).</summary>
        private const string END_CIRCLE_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_ControlCircle.png";
        /// <summary>Sprite 9-slice della pillola (raggio radius.pill).</summary>
        private const string END_PILL_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_PillRounded.png";
        /// <summary>Larghezza del velo scuro della Pausa (copre la vista a 1,5 m).</summary>
        private const float END_DIM_WIDTH = 4000f;
        /// <summary>Altezza del velo scuro della Pausa.</summary>
        private const float END_DIM_HEIGHT = 3000f;
        /// <summary>Altezza di PANEL_Menu in Pausa e Training completato (Figma 400).</summary>
        private const float END_SHORT_PANEL_HEIGHT = 400f;
        /// <summary>Larghezza fissa dei pulsanti della colonna di Pausa (Figma 240).</summary>
        private const float END_PAUSE_BUTTON_WIDTH = 240f;
        /// <summary>Numero di righe preparate per la scheda Decisioni.</summary>
        private const int END_DECISION_ROWS = 5;
        /// <summary>Numero di righe preparate per la scheda Errori.</summary>
        private const int END_ERROR_ROWS = 4;
        /// <summary>Numero massimo di passi del percorso corretto (Figma 6).</summary>
        private const int END_STEPS = 6;

        /// <summary>
        /// Crea o aggiorna i quattro prefab delle schermate di fine/pausa. Un prefab che fallisce non ferma gli altri.
        /// </summary>
        internal static void BuildEndScreens()
        {
            if (!ScreensBuilder.TryLoadContext(out UITheme theme, out UIStrings strings, out MissionCatalog _)) return;
            if (!ScreensBuilder.RequireKeys(strings, "Schermate di fine",
                "ui.pause.title", "ui.pause.phase", "ui.pause.objectives", "ui.dialog.restart.title", "ui.dialog.restart.body",
                "ui.dialog.exit_mission.title", "ui.dialog.exit_mission.body", "ui.debrief.title", "ui.debrief.correct_path",
                "ui.debrief.decisions", "ui.debrief.errors", "ui.debrief.critical_prefix", "ui.score.title", "ui.score.rank",
                "ui.score.new_badges", "ui.score.dimensions_vs_previous", "ui.score.safety_label", "ui.score.out_of_100",
                "ui.training.complete_title", "ui.training.complete_body", "ui.training.learned.move", "ui.training.learned.grab",
                "ui.training.learned.select", "ui.training.learned.pause", "ui.button.back_to_home", "ui.button.next",
                "ui.button.retry", "ui.button.change_level", "ui.nav.home", "ui.button.resume", "ui.button.restart",
                "ui.button.quick_settings", "ui.button.exit_to_home"))
            {
                Debug.LogError("[SI81 UI] Schermate di fine: chiavi mancanti, aggiungerle a strings_it.json (vedi NOTES_C3.md) e rieseguire Build Theme.");
                return;
            }
            BaseBuilder.EnsureFolder(ScreensBuilder.SCREENS_FOLDER);
            // TEX_PillRounded è condiviso (Build Theme, raggio = metà della bottom bar): qui non va rigenerato con radius.pill (999 → 4096 px).
            if (AssetDatabase.LoadAssetAtPath<Sprite>(END_PILL_SPRITE) == null)
            {
                Debug.LogError("[SI81 UI] Schermate di fine: sprite TEX_PillRounded mancante (eseguire SI81/UI/Build Theme).");
                return;
            }
            AssetDatabase.Refresh();

            Func<UITheme, UIStrings, string>[] steps = { EndBuildPause, EndBuildDebrief, EndBuildScoreReplay, EndBuildTrainingComplete };
            string[] names = { "PANEL_Pause", "PANEL_Debrief", "PANEL_ScoreReplay", "PANEL_TrainingComplete" };
            for (int i = 0; i < steps.Length; i++)
            {
                try
                {
                    Debug.Log("[SI81 UI] " + steps[i](theme, strings));
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[SI81 UI] {names[i]}: errore, passo interrotto. {exception}");
                }
            }
        }

        // =====================================================================
        // PANEL_Pause
        // =====================================================================

        /// <summary>
        /// PANEL_Pause: velo IMG_Dim, PANEL_Menu (400) con badge del livello, pillola fase, obiettivi (4) e quattro pulsanti a destra, dialogo di conferma.
        /// </summary>
        private static string EndBuildPause(UITheme theme, UIStrings strings)
        {
            GameObject root = ScreensBuilder.CreateOrLoadScreenPrefab("PANEL_Pause", UIScreenId.Pause, theme, out bool existed);
            try
            {
                // Velo scuro (nero 40%) dietro tutto: non riceve raggi.
                Image dim = ComponentsBuilder.AddImage(root.transform, "IMG_Dim", null, theme.GetColor(ButtonTokens.DIALOG_BACKDROP), false);
                PanelLayout.SetRect(dim.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-END_DIM_WIDTH * 0.5f, -END_DIM_HEIGHT * 0.5f), new Vector2(END_DIM_WIDTH * 0.5f, END_DIM_HEIGHT * 0.5f));
                dim.rectTransform.SetSiblingIndex(0);

                MenuPanel menu = ScreensBuilder.AddMenuPanel(root, theme, "ui.pause.title", string.Empty, END_SHORT_PANEL_HEIGHT, false);
                RectTransform content = menu.ContentRoot;
                float gap = theme.GetNumber(NumberTokens.SPACE_M);
                float contentWidth = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - 2f * theme.PaddingPanel;
                float columnX = contentWidth - END_PAUSE_BUTTON_WIDTH;
                float objectivesWidth = columnX - 16f;

                // Intestazione: badge del livello (Dark) a sinistra, pillola della fase a destra.
                GameObject badgeGo = EndInstance(content, "LBL_LevelBadge", "LBL_LevelBadge", 0f, 0f, -1f, -1f);
                var badge = badgeGo.GetComponent<LevelBadge>();
                badge.SetSurface(LabelSurface.Dark);
                BaseBuilder.RecordInstanceOverrides(badgeGo);

                RectTransform pill = BaseBuilder.GetOrCreateRectChild(content, "PANEL_Phase");
                EndPlace(pill, contentWidth - 200f, 0f, 200f, 28f);
                Image pillImage = ComponentsBuilder.AddImage(pill, "IMG_Pill", ComponentsBuilder.LoadSprite(END_PILL_SPRITE), theme.StateActive, false);
                PanelLayout.SetRect(pillImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                UIStringText phase = EndText(pill, "LBL_Phase", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY, string.Empty,
                    TextAlignmentOptions.Midline, 0f, 0f, 200f, 28f);

                // Obiettivi (titolo + 4 voci).
                float line = PanelLayout.LineHeight(theme.Body);
                float top = 28f + gap + 8f;
                EndText(content, "LBL_ObjTitle", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_DARK, "ui.pause.objectives",
                    TextAlignmentOptions.MidlineLeft, 0f, top, objectivesWidth, line);
                RectTransform objectives = BaseBuilder.GetOrCreateRectChild(content, "PANEL_Objectives");
                EndPlace(objectives, 0f, top + line + 4f, objectivesWidth, 200f);
                var items = new ObjectiveItem[MissionDefinition.MAX_OBJECTIVES];
                for (int i = 0; i < items.Length; i++)
                {
                    GameObject itemGo = EndInstance(objectives, "LBL_ObjectiveItem", "LBL_ObjectiveItem" + (i + 1), 0f, i * 40f, objectivesWidth, 36f);
                    items[i] = itemGo.GetComponent<ObjectiveItem>();
                }

                // Pulsanti a destra: Riprendi (Primary Dark) + tre Secondary neutri (Esci bianco come in Figma).
                float y = top;
                float step = theme.GetNumber(ButtonTokens.HEIGHT) + 8f;
                UIButton resume = EndButton(content, "BTN_Primary", "BTN_Resume", columnX, y, END_PAUSE_BUTTON_WIDTH, UIButtonStyle.Primary, "ICN_PlayArrow", "ui.button.resume");
                UIButton restart = EndButton(content, "BTN_Secondary", "BTN_Restart", columnX, y + step, END_PAUSE_BUTTON_WIDTH, UIButtonStyle.Secondary, "ICN_RestartAlt", "ui.button.restart");
                UIButton quick = EndButton(content, "BTN_Secondary", "BTN_QuickSettings", columnX, y + 2f * step, END_PAUSE_BUTTON_WIDTH, UIButtonStyle.Secondary, "ICN_Tune", "ui.button.quick_settings");
                UIButton exit = EndButton(content, "BTN_Secondary", "BTN_ExitToHome", columnX, y + 3f * step, END_PAUSE_BUTTON_WIDTH, UIButtonStyle.Secondary, "ICN_Exit", "ui.button.exit_to_home");

                ConfirmDialog dialog = EndAddDialog(root);
                var screen = BaseBuilder.GetOrAdd<PauseScreen>(root);
                screen.Configure(strings, objectivesWidth, 4f, menu, badge, pill.gameObject, phase.GetComponent<TMP_Text>(), objectives, items,
                    resume, restart, quick, exit, dialog, ComponentsBuilder.LoadIcon("ICN_RestartAlt"));
                EditorUtility.SetDirty(screen);
                root.SetActive(false);
            }
            catch
            {
                EndDiscard(root, existed);
                throw;
            }
            return ScreensBuilder.SaveScreenPrefab(root, "PANEL_Pause", existed);
        }

        // =====================================================================
        // PANEL_Debrief
        // =====================================================================

        /// <summary>
        /// PANEL_Debrief: PANEL_Side "Percorso corretto" (6 passi), PANEL_Menu con esito su vetro, schede Decisioni ed Errori (righe a pool) e "Avanti".
        /// </summary>
        private static string EndBuildDebrief(UITheme theme, UIStrings strings)
        {
            GameObject root = ScreensBuilder.CreateOrLoadScreenPrefab("PANEL_Debrief", UIScreenId.Debrief, theme, out bool existed);
            try
            {
                MenuPanel menu = ScreensBuilder.AddMenuPanel(root, theme, "ui.debrief.title", string.Empty, 0f, false);
                SidePanel side = ScreensBuilder.AddSidePanel(root, theme, "ui.debrief.correct_path", menu);
                ResultHeader header = EndAddResultHeader(root, theme, strings, menu, "ui.debrief.title");

                // Percorso corretto: 6 righe (cerchio con numero + testo a capo). L'altezza di ogni riga la scrive la schermata.
                float sideWidth = theme.GetNumber(NumberTokens.SIDE_PANEL_WIDTH) - 2f * theme.PaddingPanel;
                float stepTextWidth = sideWidth - 26f - 12f;
                Sprite circle = ComponentsBuilder.LoadSprite(END_CIRCLE_SPRITE);
                var steps = new DebriefScreen.StepRow[END_STEPS];
                for (int i = 0; i < END_STEPS; i++)
                {
                    RectTransform row = BaseBuilder.GetOrCreateRectChild(side.ContentRoot, "PANEL_Step" + (i + 1));
                    row.sizeDelta = new Vector2(sideWidth, 26f);
                    row.SetSiblingIndex(i);
                    Image number = ComponentsBuilder.AddImage(row, "IMG_StepNum", circle, theme.BrandLight, false);
                    EndPlace(number.rectTransform, 0f, 0f, 26f, 26f);
                    UIStringText numberText = EndText(row, "LBL_StepNum", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY, string.Empty,
                        TextAlignmentOptions.Midline, 0f, 0f, 26f, 26f);
                    UIStringText stepText = EndText(row, "LBL_Step", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_DARK, string.Empty,
                        TextAlignmentOptions.TopLeft, 0f, 0f, 10f, 26f);
                    EndWrap(stepText);
                    PanelLayout.SetRect((RectTransform)stepText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                        new Vector2(38f, -26f), new Vector2(0f, 0f));
                    steps[i] = new DebriefScreen.StepRow { Root = row, Number = numberText.GetComponent<TMP_Text>(), Text = stepText.GetComponent<TMP_Text>() };
                }
                side.RefreshLayout();

                // Schede.
                RectTransform content = menu.ContentRoot;
                float contentWidth = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - 2f * theme.PaddingPanel;
                EndContentSize(theme, menu, out _, out float contentHeight);
                float gap = theme.GetNumber(NumberTokens.SPACE_M);

                Sprite good = ComponentsBuilder.LoadIcon("ICN_CheckCircle_Filled");
                Sprite bad = ComponentsBuilder.LoadIcon("ICN_Cancel_Filled");
                Sprite critical = ComponentsBuilder.LoadIcon("ICN_Warning_Filled");
                Sprite neutral = ComponentsBuilder.LoadIcon("ICN_Info");

                RectTransform decisions = EndAddCard(content, "PANEL_Decisions", theme, 0f, 0f, contentWidth, 100f);
                EndText(decisions, "LBL_DecisionsTitle", theme, strings, TextStyleIds.SUBTITLE, ColorTokens.TEXT_PRIMARY, "ui.debrief.decisions",
                    TextAlignmentOptions.MidlineLeft, 12f, 12f, contentWidth - 24f, PanelLayout.LineHeight(theme.Subtitle));
                DebriefScreen.Row[] decisionRows = EndBuildRows(decisions, theme, strings, END_DECISION_ROWS);

                RectTransform errors = EndAddCard(content, "PANEL_Errors", theme, 0f, 120f, contentWidth, 100f);
                EndText(errors, "LBL_ErrorsTitle", theme, strings, TextStyleIds.SUBTITLE, ColorTokens.TEXT_PRIMARY, "ui.debrief.errors",
                    TextAlignmentOptions.MidlineLeft, 12f, 12f, contentWidth - 24f, PanelLayout.LineHeight(theme.Subtitle));
                DebriefScreen.Row[] errorRows = EndBuildRows(errors, theme, strings, END_ERROR_ROWS);

                UIButton next = EndButton(content, "BTN_Primary", "BTN_Next", 0f, 0f, 200f, UIButtonStyle.Primary, "ICN_ArrowForward", "ui.button.next");
                EndAnchorBottomRight(next, 0f);

                float cardsHeight = contentHeight - ((RectTransform)next.transform).rect.height - gap;
                var screen = BaseBuilder.GetOrAdd<DebriefScreen>(root);
                screen.Configure(theme, strings, cardsHeight, contentWidth, stepTextWidth, header, side, steps, decisions, errors,
                    decisionRows, errorRows, new[] { good, bad, critical, neutral }, next);
                EditorUtility.SetDirty(screen);
                root.SetActive(false);
            }
            catch
            {
                EndDiscard(root, existed);
                throw;
            }
            return ScreensBuilder.SaveScreenPrefab(root, "PANEL_Debrief", existed);
        }

        /// <summary>
        /// Crea le righe a pool di una scheda (radice, icona 24, testo a capo). Posizione e altezza le decide la schermata.
        /// </summary>
        private static DebriefScreen.Row[] EndBuildRows(RectTransform card, UITheme theme, UIStrings strings, int count)
        {
            var rows = new DebriefScreen.Row[count];
            for (int i = 0; i < count; i++)
            {
                RectTransform root = BaseBuilder.GetOrCreateRectChild(card, "PANEL_Row" + (i + 1));
                root.sizeDelta = new Vector2(688f, 24f);
                Image icon = ComponentsBuilder.AddImage(root, "ICN_Row", null, theme.Confirm, false);
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                EndPlace(icon.rectTransform, 0f, 0f, 24f, 24f);
                UIStringText text = EndText(root, "LBL_Row", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY, string.Empty,
                    TextAlignmentOptions.TopLeft, 32f, 0f, 10f, 24f);
                EndWrap(text);
                PanelLayout.SetRect((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(32f, 0f), new Vector2(0f, 0f));
                rows[i] = new DebriefScreen.Row { Root = root, Icon = icon, Text = text.GetComponent<TMP_Text>() };
                root.gameObject.SetActive(false);
            }
            return rows;
        }

        // =====================================================================
        // PANEL_ScoreReplay
        // =====================================================================

        /// <summary>
        /// PANEL_ScoreReplay: scheda bianca del punteggio (numero, rank a stelle, nuovi badge), cinque barre con differenza e tre azioni.
        /// </summary>
        private static string EndBuildScoreReplay(UITheme theme, UIStrings strings)
        {
            GameObject root = ScreensBuilder.CreateOrLoadScreenPrefab("PANEL_ScoreReplay", UIScreenId.ScoreReplay, theme, out bool existed);
            try
            {
                MenuPanel menu = ScreensBuilder.AddMenuPanel(root, theme, "ui.score.title", string.Empty, 0f, false);
                ResultHeader header = EndAddResultHeader(root, theme, strings, menu, "ui.score.title");
                RectTransform content = menu.ContentRoot;
                EndContentSize(theme, menu, out float contentWidth, out float contentHeight);

                // --- Scheda punteggio (300 × tutta l'altezza) ---
                const float cardWidth = 300f;
                float labelLine = PanelLayout.LineHeight(theme.Body);
                float displayLine = PanelLayout.LineHeight(theme.Display);
                RectTransform card = EndAddCard(content, "PANEL_Score", theme, 0f, 0f, cardWidth, contentHeight);
                float y = 16f;
                EndText(card, "LBL_ScoreLabel", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY, "ui.score.safety_label",
                    TextAlignmentOptions.MidlineLeft, 16f, y, cardWidth - 32f, labelLine);
                y += labelLine + 4f;
                UIStringText number = EndText(card, "LBL_ScoreNumber", theme, strings, TextStyleIds.DISPLAY, ColorTokens.TEXT_PRIMARY, string.Empty,
                    TextAlignmentOptions.BottomLeft, 16f, y, cardWidth - 32f, displayLine);
                var numberTmp = number.GetComponent<TextMeshProUGUI>();
                numberTmp.richText = true;
                EditorUtility.SetDirty(numberTmp);
                y += displayLine + 4f;
                EndText(card, "LBL_RankLabel", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY, "ui.score.rank",
                    TextAlignmentOptions.MidlineLeft, 16f, y, cardWidth - 32f, labelLine);
                y += labelLine + 4f;
                GameObject starsGo = EndInstance(card, "LBL_StarRating", "LBL_StarRating", 16f, y, -1f, -1f);
                var stars = starsGo.GetComponent<StarRating>();
                y += StarRating.STAR_SIZE + 12f;

                // Nuovi badge: titolo + BTN_Badge + nome (blocco nascosto se non ce ne sono).
                RectTransform badges = BaseBuilder.GetOrCreateRectChild(card, "PANEL_NewBadges");
                EndPlace(badges, 16f, y, cardWidth - 32f, labelLine + 8f + 64f);
                EndText(badges, "LBL_NewBadges", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY, "ui.score.new_badges",
                    TextAlignmentOptions.MidlineLeft, 0f, 0f, cardWidth - 32f, labelLine);
                GameObject badgeGo = EndInstance(badges, "BTN_Badge", "BTN_Badge", 0f, labelLine + 8f, -1f, -1f);
                var badgeButton = badgeGo.GetComponent<BadgeButton>();
                UIStringText badgeName = EndText(badges, "LBL_BadgeName", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY, string.Empty,
                    TextAlignmentOptions.MidlineLeft, 76f, labelLine + 8f + (64f - labelLine) * 0.5f, cardWidth - 32f - 76f, labelLine);

                // --- Dimensioni ---
                float columnX = cardWidth + 16f;
                float columnWidth = contentWidth - columnX;
                EndText(content, "LBL_DimensionsTitle", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_DARK, "ui.score.dimensions_vs_previous",
                    TextAlignmentOptions.MidlineLeft, columnX, 0f, columnWidth, labelLine);
                var bars = new DimensionBar[5];
                var deltas = new TMP_Text[5];
                float deltaWidth = 36f;
                float barWidth = columnWidth - deltaWidth - 8f;
                float rowY = labelLine + 8f;
                for (int i = 0; i < 5; i++)
                {
                    GameObject barGo = EndInstance(content, "LBL_DimensionBar", "LBL_DimensionBar" + (DimensionKind)i, columnX, rowY, -1f, -1f);
                    var barRect = (RectTransform)barGo.transform;
                    float barHeight = barRect.rect.height;
                    EndPlace(barRect, columnX, rowY, barWidth, barHeight);
                    bars[i] = barGo.GetComponent<DimensionBar>();
                    bars[i].SetSurface(LabelSurface.Dark);
                    bars[i].SetDimension((DimensionKind)i, 0);
                    BaseBuilder.RecordInstanceOverrides(barGo);
                    UIStringText delta = EndText(content, "LBL_Delta" + (DimensionKind)i, theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_GREEN, string.Empty,
                        TextAlignmentOptions.MidlineRight, columnX + barWidth + 8f, rowY, deltaWidth, barHeight);
                    deltas[i] = delta.GetComponent<TMP_Text>();
                    rowY += barHeight + 8f;
                }

                // --- Azioni (in basso a destra: Riprova all'estremo, poi Cambia livello, poi Home) ---
                UIButton retry = EndButton(content, "BTN_Primary", "BTN_Retry", 0f, 0f, 200f, UIButtonStyle.Primary, "ICN_Replay", "ui.button.retry");
                float x = EndAnchorBottomRight(retry, 0f);
                UIButton change = EndButton(content, "BTN_Secondary", "BTN_ChangeLevel", 0f, 0f, 0f, UIButtonStyle.Secondary, "ICN_Tune", "ui.button.change_level");
                x = EndAnchorBottomRight(change, x);
                UIButton home = EndButton(content, "BTN_Secondary", "BTN_Home", 0f, 0f, 0f, UIButtonStyle.Secondary, "ICN_Home", "ui.nav.home");
                EndAnchorBottomRight(home, x);
                // La scheda del punteggio finisce sopra la riga dei pulsanti (altrimenti "Home" la copre).
                float buttonRowHeight = ((RectTransform)retry.transform).rect.height;
                EndPlace(card, 0f, 0f, cardWidth, contentHeight - buttonRowHeight - 12f);

                var screen = BaseBuilder.GetOrAdd<ScoreReplayScreen>(root);
                screen.Configure(theme, strings, header, numberTmp, stars, badges.gameObject, badgeButton, badgeName.GetComponent<TMP_Text>(), bars, deltas,
                    home, change, retry);
                EditorUtility.SetDirty(screen);
                root.SetActive(false);
            }
            catch
            {
                EndDiscard(root, existed);
                throw;
            }
            return ScreensBuilder.SaveScreenPrefab(root, "PANEL_ScoreReplay", existed);
        }

        // =====================================================================
        // PANEL_TrainingComplete
        // =====================================================================

        /// <summary>
        /// PANEL_TrainingComplete: cerchio con spunta, messaggio, scheda "cosa hai imparato" (4 voci) e "Torna alla Home".
        /// </summary>
        private static string EndBuildTrainingComplete(UITheme theme, UIStrings strings)
        {
            GameObject root = ScreensBuilder.CreateOrLoadScreenPrefab("PANEL_TrainingComplete", UIScreenId.TrainingComplete, theme, out bool existed);
            try
            {
                MenuPanel menu = ScreensBuilder.AddMenuPanel(root, theme, "ui.training.complete_title", string.Empty, END_SHORT_PANEL_HEIGHT, false);
                RectTransform content = menu.ContentRoot;
                float contentWidth = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - 2f * theme.PaddingPanel;

                // Cerchio brand.light 60 con spunta brand.dark 48, centrato.
                Image circle = ComponentsBuilder.AddImage(content, "IMG_Check", ComponentsBuilder.LoadSprite(END_CIRCLE_SPRITE), theme.BrandLight, false);
                EndPlace(circle.rectTransform, (contentWidth - 60f) * 0.5f, 0f, 60f, 60f);
                Image check = ComponentsBuilder.AddIcon(circle.rectTransform, "ICN_Check", theme, "ICN_Check", 48f, ColorTokens.BRAND_DARK);
                check.rectTransform.anchoredPosition = Vector2.zero;

                float line = PanelLayout.LineHeight(theme.Body);
                float y = 68f;
                EndText(content, "LBL_Body", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_GREEN, "ui.training.complete_body",
                    TextAlignmentOptions.Midline, 0f, y, contentWidth, line);
                y += line + 8f;

                // Scheda bianca: 4 voci con spunta piena.
                string[] keys = { "ui.training.learned.move", "ui.training.learned.grab", "ui.training.learned.select", "ui.training.learned.pause" };
                float rowHeight = 24f;
                float cardHeight = 12f + keys.Length * rowHeight + (keys.Length - 1) * 4f + 12f;
                float cardWidth = 560f;
                RectTransform card = EndAddCard(content, "PANEL_Learned", theme, (contentWidth - cardWidth) * 0.5f, y, cardWidth, cardHeight);
                for (int i = 0; i < keys.Length; i++)
                {
                    float rowY = 12f + i * (rowHeight + 4f);
                    Image icon = ComponentsBuilder.AddImage(card, "ICN_Learned" + (i + 1), ComponentsBuilder.LoadIcon("ICN_CheckCircle_Filled"), theme.Confirm, false);
                    icon.type = Image.Type.Simple;
                    icon.preserveAspect = true;
                    EndPlace(icon.rectTransform, 12f, rowY, 24f, 24f);
                    EndText(card, "LBL_Learned" + (i + 1), theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY, keys[i],
                        TextAlignmentOptions.MidlineLeft, 44f, rowY, cardWidth - 56f, rowHeight);
                }

                UIButton home = EndButton(content, "BTN_Primary", "BTN_BackToHome", 0f, 0f, 240f, UIButtonStyle.Primary, "ICN_Home", "ui.button.back_to_home");
                var homeRect = (RectTransform)home.transform;
                homeRect.anchorMin = homeRect.anchorMax = homeRect.pivot = new Vector2(0.5f, 0f);
                homeRect.anchoredPosition = Vector2.zero;
                BaseBuilder.RecordInstanceOverrides(home.gameObject);

                var screen = BaseBuilder.GetOrAdd<TrainingCompleteScreen>(root);
                screen.Configure(home);
                EditorUtility.SetDirty(screen);
                root.SetActive(false);
            }
            catch
            {
                EndDiscard(root, existed);
                throw;
            }
            return ScreensBuilder.SaveScreenPrefab(root, "PANEL_TrainingComplete", existed);
        }

        // =====================================================================
        // Helper (prefisso End)
        // =====================================================================

        /// <summary>
        /// Libera la radice senza salvare (dopo un errore).
        /// </summary>
        private static void EndDiscard(GameObject root, bool existed)
        {
            if (root == null) return;
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Dimensioni dell'area contenuti di PANEL_Menu: larghezza pannello − 2 × padding; altezza pannello − 2 × padding − riga del titolo − space.m.
        /// </summary>
        private static void EndContentSize(UITheme theme, MenuPanel menu, out float width, out float height)
        {
            float padding = theme.PaddingPanel;
            width = theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH) - 2f * padding;
            height = menu.Height - 2f * padding - PanelLayout.LineHeight(theme.Title) - theme.GetNumber(NumberTokens.SPACE_M);
        }

        /// <summary>
        /// Posiziona un elemento con l'angolo in alto a sinistra a (x, y); con larghezza o altezza negative lascia la dimensione al componente.
        /// </summary>
        private static void EndPlace(RectTransform rect, float x, float y, float width, float height)
        {
            if (width < 0f || height < 0f)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, -y);
                rect.localScale = Vector3.one;
                return;
            }
            PanelLayout.SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(x, -(y + height)), new Vector2(x + width, -y));
        }

        /// <summary>
        /// Crea o aggiorna un'etichetta (stile e colore del tema, chiave opzionale) e la posiziona.
        /// </summary>
        private static UIStringText EndText(Transform parent, string name, UITheme theme, UIStrings strings, string style, string colorToken,
            string key, TextAlignmentOptions alignment, float x, float y, float width, float height)
        {
            UIStringText label = ComponentsBuilder.AddLabel(parent, name, theme, strings, style, colorToken, key, alignment);
            EndPlace((RectTransform)label.transform, x, y, width, height);
            return label;
        }

        /// <summary>
        /// Attiva l'a capo automatico di un'etichetta.
        /// </summary>
        private static void EndWrap(UIStringText label)
        {
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            EditorUtility.SetDirty(tmp);
        }

        /// <summary>
        /// Crea o recupera un'istanza di un prefab di Prefabs/Components e la posiziona (vedi <see cref="EndPlace"/>).
        /// </summary>
        private static GameObject EndInstance(Transform parent, string prefabName, string instanceName, float x, float y, float width, float height)
        {
            GameObject instance = ScreensBuilder.InstantiateComponent(prefabName, parent, instanceName);
            if (instance == null) throw new InvalidOperationException($"Prefab {prefabName} mancante: eseguire SI81/UI/Build Components.");
            EndPlace((RectTransform)instance.transform, x, y, width, height);
            BaseBuilder.RecordInstanceOverrides(instance);
            return instance;
        }

        /// <summary>
        /// Istanza di BTN_Primary / BTN_Secondary con stile, icona, etichetta e larghezza fissa (0 = secondo il contenuto), posizionata in alto a sinistra.
        /// </summary>
        private static UIButton EndButton(Transform parent, string prefabName, string instanceName, float x, float y, float fixedWidth,
            UIButtonStyle style, string iconName, string labelKey)
        {
            GameObject go = EndInstance(parent, prefabName, instanceName, x, y, -1f, -1f);
            var button = go.GetComponent<UIButton>();
            button.SetStyle(style, UIButtonSurface.Dark);
            button.SetIcon(ComponentsBuilder.LoadIcon(iconName));
            button.SetLabelKey(labelKey);
            if (fixedWidth > 0f)
            {
                var serialized = new SerializedObject(button);
                SerializedProperty property = serialized.FindProperty("fixedWidth");
                if (property != null) property.floatValue = fixedWidth;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            button.Apply();
            var rect = (RectTransform)go.transform;
            EndPlace(rect, x, y, rect.rect.width, rect.rect.height);
            BaseBuilder.RecordInstanceOverrides(go);
            return button;
        }

        /// <summary>
        /// Ancora un pulsante in basso a destra a <paramref name="fromRight"/> dal bordo destro.
        /// </summary>
        /// <returns>Scarto per il prossimo pulsante a sinistra (larghezza + space.m).</returns>
        private static float EndAnchorBottomRight(UIButton button, float fromRight)
        {
            var rect = (RectTransform)button.transform;
            float width = rect.rect.width;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-fromRight, 0f);
            BaseBuilder.RecordInstanceOverrides(button.gameObject);
            return fromRight + width + 12f;
        }

        /// <summary>
        /// Scheda bianca (surface.card, radius.card, ombra card): IMG_Shadow + IMG_Background senza raycast; i contenuti vanno nel rettangolo restituito.
        /// </summary>
        private static RectTransform EndAddCard(Transform parent, string name, UITheme theme, float x, float y, float width, float height)
        {
            RectTransform card = BaseBuilder.GetOrCreateRectChild(parent, name);
            EndPlace(card, x, y, width, height);
            ComponentsBuilder.ShapeSprites shape = ComponentsBuilder.EnsureShapeSprites(theme, "Card", theme.RadiusCard);
            UITheme.ShadowToken token = theme.GetShadow(ShadowRoles.CARD);
            Image shadow = ComponentsBuilder.AddImage(card, "IMG_Shadow", shape.ShadowRest, token != null ? token.Color : Color.clear, false);
            shadow.fillCenter = false;
            float expand = PanelSprites.GetShadowLayout(theme.RadiusCard, token).PaddingUnits;
            PanelLayout.SetRect(shadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-expand, -expand), new Vector2(expand, expand));
            shadow.transform.SetSiblingIndex(0);
            Image background = ComponentsBuilder.AddImage(card, "IMG_Background", shape.Rounded, theme.SurfaceCard, false);
            PanelLayout.SetRect(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            background.transform.SetSiblingIndex(1);
            return card;
        }

        /// <summary>
        /// Istanza di PANEL_ConfirmDialog centrata nella schermata (disattiva, ultima tra i fratelli).
        /// </summary>
        private static ConfirmDialog EndAddDialog(GameObject root)
        {
            GameObject go = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, ComponentsBuilder.PrefabPath("PANEL_ConfirmDialog"), "PANEL_ConfirmDialog");
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.SetAsLastSibling();
            go.SetActive(false);
            BaseBuilder.RecordInstanceOverrides(go);
            return go.GetComponent<ConfirmDialog>();
        }

        /// <summary>
        /// Aggiunge a una schermata l'intestazione comune (badge del livello + esito su vetro nel posto a destra di PANEL_Menu).
        /// </summary>
        private static ResultHeader EndAddResultHeader(GameObject root, UITheme theme, UIStrings strings, MenuPanel menu, string titleKey)
        {
            GameObject badgeGo = EndInstance(menu.HeaderRightSlot, "LBL_LevelBadge", "LBL_LevelBadge", 0f, 0f, -1f, -1f);
            var badge = badgeGo.GetComponent<LevelBadge>();
            badge.SetSurface(LabelSurface.Dark);
            BaseBuilder.RecordInstanceOverrides(badgeGo);
            GameObject outcomeGo = EndInstance(menu.HeaderRightSlot, "LBL_Outcome", "LBL_Outcome", 0f, 0f, -1f, -1f);
            var outcome = outcomeGo.GetComponent<OutcomeLabel>();
            outcome.SetSize(LabelSize.Small);
            outcome.SetPresentation(OutcomePresentation.OnGlass);
            BaseBuilder.RecordInstanceOverrides(outcomeGo);
            var header = BaseBuilder.GetOrAdd<ResultHeader>(root);
            header.Configure(strings, titleKey, theme.GetNumber(NumberTokens.SPACE_M), menu, badge, outcome);
            EditorUtility.SetDirty(header);
            return header;
        }
    }
}
