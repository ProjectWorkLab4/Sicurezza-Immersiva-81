using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "rig" del builder della HUD (Blocco C · C1): crea o aggiorna HUD_WristPanel, HUD_EventAlert, HUD_ActionFeedback,
    /// HUD_Subtitles e il prefab MNG_HUD che li contiene (con HUDRoot, HapticsService, UIAudioFeedback, AlertPresenter, MNG_WorldLabels
    /// e le istanze disattive di HUD_DecisionPanel / HUD_OutcomePanel). Misure da docs/FIGMA_SPECS.md §1b e dal tema; scala dei Canvas
    /// = 0,00126 × distanza (avvisi e sottotitoli a 1 m); pannello da polso: scala = larghezza fisica (12 cm) / larghezza del Canvas.
    /// </summary>
    internal static partial class HUDBuilder
    {
        /// <summary>Larghezza del Canvas del pannello da polso in unità. 12 cm / 340 = 0,000353 m per unità, come una lettura a ~28 cm con la formula 0,00126 × distanza.</summary>
        private const float RIG_WRIST_CANVAS_WIDTH = 340f;
        /// <summary>Dimensione del Canvas di un avviso (420 + margine per l'ombra, 102 + margine).</summary>
        private static readonly Vector2 RIG_ALERT_CANVAS = new Vector2(480f, 160f);
        /// <summary>Dimensione del Canvas dei sottotitoli (32 caratteri a 150% stanno in ~620).</summary>
        private static readonly Vector2 RIG_SUBTITLE_CANVAS = new Vector2(760f, 200f);
        /// <summary>Distanza degli avvisi dalla testa in metri (~1 m, CLAUDE.md §4.11).</summary>
        private const float RIG_ALERT_DISTANCE_M = 1f;
        /// <summary>Lato delle icone delle righe del pannello da polso.</summary>
        private const float RIG_WRIST_ICON = 20f;
        /// <summary>Lato delle icone degli avvisi.</summary>
        private const float RIG_ALERT_ICON = 32f;

        /// <summary>
        /// Crea o aggiorna HUD_WristPanel, HUD_EventAlert, HUD_ActionFeedback, HUD_Subtitles e MNG_HUD. Non fa nulla se mancano tema o testi.
        /// Richiede Build Components (LBL_ObjectiveItem, LBL_LevelBadge) e le icone.
        /// </summary>
        internal static void BuildRigHud()
        {
            if (!HudTryLoadContext(out UITheme theme, out UIStrings strings)) return;
            if (!ScreensBuilder.RequireKeys(strings, "HUD", "hud.wrist.timer", "hud.wrist.objective_progress", "hud.phase.scan", "hud.phase.decision",
                "hud.phase.action", "hud.phase.resolution", "hud.subtitles.speaker_line", "hud.subtitles.sound_event")) return;
            BaseBuilder.EnsureFolder(HUD_FOLDER);

            HUDIconLibrary icons = HudEnsureIconLibrary();
            Sprite card = HudEnsureRounded(theme, "HudCard", theme.RadiusCard);
            Sprite pill = HudEnsureRounded(theme, "HudPill", SpriteBuilder.GetPillRadius(theme));
            Sprite shadow = HudEnsureShadow(theme, "TEX_HudCardShadow", theme.RadiusCard, ShadowRoles.HUD_CARD);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var lines = new List<string>
            {
                RigBuildWristPanel(theme, strings, card, pill),
                RigBuildAlert("HUD_EventAlert", "CANVAS_EventAlert", true, theme, strings, card, pill, shadow),
                RigBuildAlert("HUD_ActionFeedback", "CANVAS_ActionFeedback", false, theme, strings, card, pill, shadow),
                RigBuildSubtitles(theme, strings, card),
                RigBuildManager(theme, strings, icons)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[SI81 UI] Build HUD · rig:\n" + string.Join("\n", lines));
        }

        // =====================================================================
        // HUD_WristPanel
        // =====================================================================

        /// <summary>
        /// HUD_WristPanel: radice (WristAttach + WristPanel) con CANVAS_WristPanel 12 × 7 cm: sfondo opaco surface.panel, obiettivo,
        /// riquadro timer, contatore, fase, badge del livello, suggerimento e ultimo avviso.
        /// </summary>
        private static string RigBuildWristPanel(UITheme theme, UIStrings strings, Sprite card, Sprite pill)
        {
            GameObject root = HudCreateOrLoadPrefab("HUD_WristPanel", out bool existed);
            try
            {
                Vector2 meters = theme.Hud.WristPanelSizeMeters;
                float scale = meters.x / RIG_WRIST_CANVAS_WIDTH;
                var size = new Vector2(RIG_WRIST_CANVAS_WIDTH, Mathf.Round(meters.y / scale));
                GameObject canvasGo = HudCreateWorldCanvas(root.transform, "CANVAS_WristPanel", size, scale);
                Transform canvas = canvasGo.transform;
                var group = canvasGo.GetComponent<CanvasGroup>();
                group.alpha = theme.Hud.WristIdleAlpha;
                group.interactable = false;
                group.blocksRaycasts = false;

                RoundedPanelGraphic background = HudAddRounded(canvas, "IMG_Background", card, theme.SurfacePanel);
                BaseBuilder.Stretch(background.rectTransform);
                background.transform.SetSiblingIndex(0);

                // Obiettivo (LBL_ObjectiveItem, modalità polso: avvisa oltre ~30 caratteri per riga).
                GameObject objectiveGo = ScreensBuilder.InstantiateComponent("LBL_ObjectiveItem", canvas, "LBL_ObjectiveItem");
                ObjectiveItem objective = objectiveGo != null ? objectiveGo.GetComponent<ObjectiveItem>() : null;
                if (objective != null)
                {
                    var serialized = new SerializedObject(objective);
                    serialized.FindProperty("wristMode").boolValue = true;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                // Timer, fase, contatore.
                RoundedPanelGraphic chip = HudAddRounded(canvas, "IMG_TimerChip", card, theme.Deny);
                chip.enabled = false;
                Image timerIcon = ComponentsBuilder.AddIcon(canvas, "ICN_Timer", theme, "ICN_Timer", RIG_WRIST_ICON, ColorTokens.TEXT_ON_DARK);
                TMP_Text timerText = HudAddText(canvas, "LBL_Timer", theme, strings, TextStyleIds.SUBTITLE_BOLD, ColorTokens.TEXT_ON_DARK, TextAlignmentOptions.MidlineLeft);
                RoundedPanelGraphic phasePill = HudAddRounded(canvas, "IMG_PhasePill", pill, theme.StateActive);
                TMP_Text phaseText = HudAddText(canvas, "LBL_Phase", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.Midline);
                TMP_Text counterText = HudAddText(canvas, "LBL_Counter", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_DARK, TextAlignmentOptions.MidlineRight);

                // Badge del livello.
                GameObject badgeGo = ScreensBuilder.InstantiateComponent("LBL_LevelBadge", canvas, "LBL_LevelBadge");
                LevelBadge badge = badgeGo != null ? badgeGo.GetComponent<LevelBadge>() : null;
                if (badge != null)
                {
                    badge.SetSurface(LabelSurface.Dark);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(badge);
                }

                // Suggerimento e ultimo avviso.
                RectTransform hintRow = BaseBuilder.GetOrCreateRectChild(canvas, "PANEL_Hint");
                Image hintIcon = ComponentsBuilder.AddIcon(hintRow, "ICN_Hint", theme, "ICN_Lightbulb", RIG_WRIST_ICON, ColorTokens.BRAND_LIGHT);
                TMP_Text hintText = HudAddText(hintRow, "LBL_Hint", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_DARK, TextAlignmentOptions.TopLeft);
                RectTransform alertRow = BaseBuilder.GetOrCreateRectChild(canvas, "PANEL_LastAlert");
                Image alertIcon = ComponentsBuilder.AddIcon(alertRow, "ICN_LastAlert", theme, "ICN_NotificationsActive", RIG_WRIST_ICON, ColorTokens.TEXT_ON_GREEN);
                TMP_Text alertText = HudAddText(alertRow, "LBL_LastAlert", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_GREEN, TextAlignmentOptions.MidlineLeft);
                hintRow.gameObject.SetActive(false);
                alertRow.gameObject.SetActive(false);

                // Ordine di disegno: sfondo, chip, contenuti.
                chip.transform.SetSiblingIndex(1);

                var attach = BaseBuilder.GetOrAdd<WristAttach>(root);
                attach.Configure(canvasGo);
                var panel = BaseBuilder.GetOrAdd<WristPanel>(root);
                panel.Configure(theme, strings, canvas, group, background, objective, counterText, chip, timerIcon, timerText,
                    ComponentsBuilder.LoadIcon("ICN_Timer"), ComponentsBuilder.LoadIcon("ICN_Warning_Filled"), phasePill, phaseText, badge,
                    hintRow, hintIcon, hintText, alertRow, alertIcon, alertText);
                EditorUtility.SetDirty(panel);
                EditorUtility.SetDirty(attach);
                return HudSavePrefab(root, "HUD_WristPanel", existed);
            }
            catch
            {
                RigDiscard(root, existed);
                throw;
            }
        }

        // =====================================================================
        // HUD_EventAlert / HUD_ActionFeedback
        // =====================================================================

        /// <summary>
        /// Un avviso breve: CANVAS_ con ombra hudCard, sfondo, icona 32, titolo e (solo evento) dettaglio. Opaco. Spento finché non serve.
        /// </summary>
        private static string RigBuildAlert(string prefabName, string canvasName, bool isEvent, UITheme theme, UIStrings strings, Sprite card, Sprite pill, Sprite shadow)
        {
            GameObject root = HudCreateOrLoadPrefab(prefabName, out bool existed);
            try
            {
                GameObject canvasGo = HudCreateWorldCanvas(root.transform, canvasName, RIG_ALERT_CANVAS, UITheme.GetCanvasScale(RIG_ALERT_DISTANCE_M));
                Transform canvas = canvasGo.transform;
                var group = canvasGo.GetComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;

                Image shadowImage = ComponentsBuilder.AddImage(canvas, "IMG_Shadow", shadow, theme.GetShadow(ShadowRoles.HUD_CARD)?.Color ?? Color.black, false);
                shadowImage.transform.SetSiblingIndex(0);
                RoundedPanelGraphic background = HudAddRounded(canvas, "IMG_Background", card, isEvent ? theme.Deny : theme.SurfaceCard);
                background.transform.SetSiblingIndex(1);

                string iconColor = isEvent ? ColorTokens.TEXT_PRIMARY : ColorTokens.CONFIRM;
                Image icon = ComponentsBuilder.AddIcon(background.transform, "ICN_Alert", theme, isEvent ? "ICN_NotificationsActive_Filled" : "ICN_CheckCircle_Filled", RIG_ALERT_ICON, iconColor);
                TMP_Text title = HudAddText(background.transform, isEvent ? "LBL_Title" : "LBL_Text", theme, strings,
                    isEvent ? TextStyleIds.SUBTITLE_BOLD : TextStyleIds.HUD_MIN, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.TopLeft);
                TMP_Text body = isEvent
                    ? HudAddText(background.transform, "LBL_Body", theme, strings, TextStyleIds.HUD_MIN, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.TopLeft)
                    : null;
                RigWrap(title);
                RigWrap(body);

                var view = BaseBuilder.GetOrAdd<AlertView>(root);
                view.Configure(theme, canvasGo, group, background, shadowImage, icon, title, body, card, pill, isEvent,
                    ComponentsBuilder.LoadIcon("ICN_NotificationsActive_Filled"), ComponentsBuilder.LoadIcon("ICN_CheckCircle_Filled"),
                    ComponentsBuilder.LoadIcon("ICN_Info"), ComponentsBuilder.LoadIcon("ICN_Cancel_Filled"));
                EditorUtility.SetDirty(view);
                canvasGo.SetActive(false);
                return HudSavePrefab(root, prefabName, existed);
            }
            catch
            {
                RigDiscard(root, existed);
                throw;
            }
        }

        // =====================================================================
        // HUD_Subtitles
        // =====================================================================

        /// <summary>
        /// HUD_Subtitles: radice con LazyFollow e SubtitlePresenter, CANVAS_Subtitles (1 m) con PANEL_Subtitle (dimensionato a runtime):
        /// sfondo subtitle.bg, icona evento, nome di chi parla e testo (max 32 caratteri × 2 righe). Spento finché non serve.
        /// </summary>
        private static string RigBuildSubtitles(UITheme theme, UIStrings strings, Sprite card)
        {
            GameObject root = HudCreateOrLoadPrefab("HUD_Subtitles", out bool existed);
            try
            {
                GameObject canvasGo = HudCreateWorldCanvas(root.transform, "CANVAS_Subtitles", RIG_SUBTITLE_CANVAS, UITheme.GetCanvasScale(UITheme.SUBTITLE_DISTANCE_M));
                var group = canvasGo.GetComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;

                RectTransform panel = BaseBuilder.GetOrCreateRectChild(canvasGo.transform, "PANEL_Subtitle");
                panel.sizeDelta = new Vector2(420f, 110f);
                RoundedPanelGraphic background = HudAddRounded(panel, "IMG_Background", card, theme.SubtitleBackground);
                BaseBuilder.Stretch(background.rectTransform);
                background.transform.SetSiblingIndex(0);
                Image eventIcon = ComponentsBuilder.AddIcon(panel, "ICN_Event", theme, "ICN_NotificationsActive", 24f, ColorTokens.SUBTITLE_EVENT);
                TMP_Text speaker = HudAddText(panel, "LBL_Speaker", theme, strings, TextStyleIds.SUBTITLE_BOLD, ColorTokens.SUBTITLE_SPEAKER, TextAlignmentOptions.TopLeft);
                TMP_Text body = HudAddText(panel, "LBL_Text", theme, strings, TextStyleIds.SUBTITLE, ColorTokens.SUBTITLE_TEXT, TextAlignmentOptions.TopLeft);

                var follow = BaseBuilder.GetOrAdd<LazyFollow>(root);
                follow.Configure(UITheme.SUBTITLE_DISTANCE_M, theme.Hud.SubtitlePitchMin, theme.Hud.SubtitlePitchMax, theme.Hud.MaxPitch);
                var presenter = BaseBuilder.GetOrAdd<SubtitlePresenter>(root);
                presenter.Configure(theme, strings, follow, canvasGo, group, panel, background, eventIcon, speaker, body);
                EditorUtility.SetDirty(follow);
                EditorUtility.SetDirty(presenter);
                canvasGo.SetActive(false);
                return HudSavePrefab(root, "HUD_Subtitles", existed);
            }
            catch
            {
                RigDiscard(root, existed);
                throw;
            }
        }

        // =====================================================================
        // MNG_HUD
        // =====================================================================

        /// <summary>
        /// MNG_HUD: HUDRoot + HapticsService + UIAudioFeedback + AlertPresenter, le istanze dei quattro prefab del rig, MNG_WorldLabels e
        /// le istanze disattive di HUD_DecisionPanel / HUD_OutcomePanel (se i prefab esistono).
        /// </summary>
        private static string RigBuildManager(UITheme theme, UIStrings strings, HUDIconLibrary icons)
        {
            GameObject root = HudCreateOrLoadPrefab("MNG_HUD", out bool existed);
            try
            {
                GameObject wristGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, HudPrefabPath("HUD_WristPanel"), "HUD_WristPanel");
                GameObject eventGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, HudPrefabPath("HUD_EventAlert"), "HUD_EventAlert");
                GameObject feedbackGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, HudPrefabPath("HUD_ActionFeedback"), "HUD_ActionFeedback");
                GameObject subtitlesGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, HudPrefabPath("HUD_Subtitles"), "HUD_Subtitles");

                var alerts = BaseBuilder.GetOrAdd<AlertPresenter>(root);
                alerts.Configure(theme, eventGo.GetComponent<AlertView>(), feedbackGo.GetComponent<AlertView>(), RIG_ALERT_DISTANCE_M);
                var haptics = BaseBuilder.GetOrAdd<HapticsService>(root);
                var audio = BaseBuilder.GetOrAdd<UIAudioFeedback>(root);
                audio.Configure(root.GetComponent<AudioSource>(), RigAudioSlots());
                var hudRoot = BaseBuilder.GetOrAdd<HUDRoot>(root);
                hudRoot.Configure(theme, strings, wristGo.GetComponent<WristPanel>(), wristGo.GetComponent<WristAttach>(), alerts,
                    subtitlesGo.GetComponent<SubtitlePresenter>(), icons);

                // Parti del Blocco C · C2 (se già costruite): etichette nel mondo, decisione, esito (istanze disattive).
                AddWorldLabelManager(root, theme);
                RigAddInactiveInstance(root.transform, "HUD_DecisionPanel");
                RigAddInactiveInstance(root.transform, "HUD_OutcomePanel");

                EditorUtility.SetDirty(alerts);
                EditorUtility.SetDirty(haptics);
                EditorUtility.SetDirty(audio);
                EditorUtility.SetDirty(hudRoot);
                return HudSavePrefab(root, "MNG_HUD", existed);
            }
            catch
            {
                RigDiscard(root, existed);
                throw;
            }
        }

        /// <summary>
        /// Aggiunge (o ritrova) un'istanza disattiva di un prefab della HUD sotto MNG_HUD, se il prefab esiste.
        /// </summary>
        /// <param name="parent">Radice di MNG_HUD.</param>
        /// <param name="prefabName">Nome del prefab e dell'istanza.</param>
        private static void RigAddInactiveInstance(Transform parent, string prefabName)
        {
            string path = HudPrefabPath(prefabName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[SI81 UI] MNG_HUD: prefab {prefabName} assente, istanza non aggiunta (eseguire Build HUD dopo aver costruito {prefabName}).");
                return;
            }
            GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(parent, path, prefabName);
            instance.SetActive(false);
        }

        /// <summary>
        /// Slot audio: un elemento per ogni id SFX_ previsto, con la clip se l'asset esiste già (altrimenti vuoto = silenzio).
        /// </summary>
        /// <returns>Elenco degli slot.</returns>
        private static List<UIAudioFeedback.Slot> RigAudioSlots()
        {
            var slots = new List<UIAudioFeedback.Slot>();
            foreach (string id in UIAudioFeedback.ALL_IDS)
            {
                AudioClip clip = AssetDatabase.FindAssets(id + " t:AudioClip")
                    .Select(guid => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)))
                    .FirstOrDefault(c => c != null && c.name == id);
                slots.Add(new UIAudioFeedback.Slot { Id = id, Clip = clip });
            }
            return slots;
        }

        /// <summary>
        /// Testo che va a capo, senza troncare (la forma dell'avviso si adatta al testo).
        /// </summary>
        /// <param name="text">Testo (può essere null).</param>
        private static void RigWrap(TMP_Text text)
        {
            if (text == null) return;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        /// <summary>
        /// Libera un contenuto di prefab rimasto aperto dopo un errore.
        /// </summary>
        /// <param name="root">Radice.</param>
        /// <param name="existed">true se caricato da un prefab esistente.</param>
        private static void RigDiscard(GameObject root, bool existed)
        {
            if (root == null) return;
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }
}
