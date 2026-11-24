using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "Profilo" del builder delle schermate (Blocco B · writer B3): crea o aggiorna il prefab
    /// <c>Prefabs/Screens/PANEL_Profile.prefab</c> (UIScreen + PANEL_Menu annidato + colonna identità + competenze + griglia dei badge).
    /// Usa gli strumenti <c>Stats…</c> definiti in <c>ScreensBuilder.Stats.cs</c> (stesso writer). Create-or-update, rieseguibile.
    /// </summary>
    internal static partial class ScreensBuilder
    {
        /// <summary>Sprite del cerchio dell'avatar (raggio = metà del lato 120).</summary>
        private const string PROFILE_AVATAR_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_AvatarRounded.png";

        /// <summary>
        /// Crea o aggiorna PANEL_Profile (Figma "Profilo", FIGMA_SPECS §2 + "SPI N/100" complessivo).
        /// </summary>
        static void BuildProfile()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] PANEL_Profile: interrotto, SO_UITheme / SO_UIStrings_It mancanti (eseguire SI81/UI/Build Theme).");
                return;
            }
            string[] needed = { "BTN_Badge", "BTN_Back", "LBL_StarRating", "LBL_DimensionBar" };
            foreach (string prefab in needed)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ComponentsBuilder.PrefabPath(prefab)) == null)
                {
                    Debug.LogError($"[SI81 UI] PANEL_Profile: interrotto, il prefab {prefab} manca (eseguire SI81/UI/Build Components).");
                    return;
                }

            GameObject root = StatsOpenPrefab("PANEL_Profile", out bool existed);
            try
            {
                var screen = BaseBuilder.GetOrAdd<UIScreen>(root);
                screen.Configure(UIScreenId.Profile, theme);

                GameObject panelGo = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, MenuPanelBuilder.PREFAB_PATH, MenuPanelBuilder.ROOT_NAME);
                panelGo.transform.SetSiblingIndex(0);
                var panel = panelGo.GetComponent<MenuPanel>();
                MenuPanelBuilder.ConfigureInstance(panel, "ui.profile.title", string.Empty, false, 0f);
                RectTransform content = panel.ContentRoot;
                StatsContentSize(theme, panel, out float contentW, out float contentH);

                const float IDENTITY_W = 240f;
                float gap = theme.GapGrid;
                float skillsW = contentW - IDENTITY_W - gap;
                float line = PanelLayout.LineHeight(theme.Body);

                // ================= Colonna sinistra: identità =================
                RectTransform identity = StatsRect(content, "PANEL_Identity", 0f, 0f, IDENTITY_W, contentH);
                const float AVATAR = 120f;
                SpriteBuilder.BuildRoundedSprite(PROFILE_AVATAR_SPRITE, AVATAR * 0.5f);
                Image avatar = ComponentsBuilder.AddImage(identity, "IMG_Avatar", ComponentsBuilder.LoadSprite(PROFILE_AVATAR_SPRITE), theme.BrandLight, false);
                StatsPlace((RectTransform)avatar.transform, (IDENTITY_W - AVATAR) * 0.5f, 0f, AVATAR, AVATAR);
                Image avatarIcon = ComponentsBuilder.AddIcon(avatar.transform, "ICN_Avatar", theme, "ICN_Engineering_Filled", 72f, ColorTokens.BRAND_DARK);
                avatarIcon.rectTransform.anchoredPosition = Vector2.zero;

                float y = AVATAR + 6f;
                float nameH = PanelLayout.LineHeight(theme.SubtitleBold);
                UIStringText name = StatsText(identity, "LBL_Name", theme, strings, TextStyleIds.SUBTITLE_BOLD, ColorTokens.TEXT_ON_DARK,
                    string.Empty, TextAlignmentOptions.Center, 0f, y, IDENTITY_W, nameH);
                y += nameH + 6f;
                UIStringText missions = StatsText(identity, "LBL_MissionsDone", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_GREEN,
                    string.Empty, TextAlignmentOptions.Center, 0f, y, IDENTITY_W, line);
                y += line + 12f;
                StatsText(identity, "LBL_RecentTitle", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_DARK,
                    "ui.profile.recent_results", TextAlignmentOptions.MidlineLeft, 0f, y, IDENTITY_W, line);
                y += line + 4f;

                // 3 righe: titolo (autoridotto) · stella piena brand.light · "N/5 · NN/100".
                const float ROW_H = 24f;
                const float STAR = 20f;
                const float VALUE_W = 92f;
                var recentRows = new ProfileScreen.RecentRow[ProfileScreen.RECENT_COUNT];
                for (int i = 0; i < recentRows.Length; i++)
                {
                    // Migrazione v0.4.1: il vecchio nome con underscore (PANEL_RecentRow_1) diventa PANEL_RecentRow1 (§2), senza doppioni.
                    Transform legacyRow = identity.Find("PANEL_RecentRow_" + (i + 1));
                    if (legacyRow != null) legacyRow.name = "PANEL_RecentRow" + (i + 1);
                    RectTransform row = StatsRect(identity, "PANEL_RecentRow" + (i + 1), 0f, y + i * (ROW_H + 2f), IDENTITY_W, ROW_H);
                    float titleW = IDENTITY_W - VALUE_W - STAR - 8f;
                    UIStringText title = StatsText(row, "LBL_Title", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_DARK,
                        string.Empty, TextAlignmentOptions.MidlineLeft, 0f, 0f, titleW, ROW_H);
                    ProfileAutoSize(title.GetComponent<TMP_Text>(), theme);
                    Image star = ComponentsBuilder.AddIcon(row, "ICN_Star", theme, "ICN_Star_Filled", STAR, ColorTokens.BRAND_LIGHT);
                    StatsPlace((RectTransform)star.transform, titleW + 4f, (ROW_H - STAR) * 0.5f, STAR, STAR);
                    UIStringText value = StatsText(row, "LBL_Value", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_GREEN,
                        string.Empty, TextAlignmentOptions.MidlineRight, IDENTITY_W - VALUE_W, 0f, VALUE_W, ROW_H);
                    ProfileAutoSize(value.GetComponent<TMP_Text>(), theme);
                    recentRows[i] = new ProfileScreen.RecentRow { Root = row.gameObject, Title = title.GetComponent<TMP_Text>(), Value = value.GetComponent<TMP_Text>() };
                }

                // Scheda bianca in fondo alla colonna: "SPI N/100" complessivo + stelle (= rank).
                const float CARD_PAD_X = 12f;
                const float CARD_PAD_Y = 8f;
                float cardH = CARD_PAD_Y * 2f + PanelLayout.LineHeight(theme.BodyBold) + 2f + 24f;
                RectTransform overall = StatsAddCard(identity, "PANEL_OverallScore", theme, 0f, contentH - cardH, IDENTITY_W, cardH);
                UIStringText overallScore = StatsText(overall, "LBL_OverallScore", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    string.Empty, TextAlignmentOptions.MidlineLeft, CARD_PAD_X, CARD_PAD_Y, IDENTITY_W - 2f * CARD_PAD_X, PanelLayout.LineHeight(theme.BodyBold));
                ProfileAutoSize(overallScore.GetComponent<TMP_Text>(), theme);
                GameObject overallStarsGo = StatsInstance(overall, "LBL_StarRating", "LBL_StarRating", CARD_PAD_X,
                    CARD_PAD_Y + PanelLayout.LineHeight(theme.BodyBold) + 2f, -1f, -1f);
                var overallStars = overallStarsGo.GetComponent<StarRating>();
                overallStars.SetStars(0, false);
                BaseBuilder.RecordInstanceOverrides(overallStarsGo);

                // ================= Colonna destra: competenze e badge =================
                RectTransform skills = StatsRect(content, "PANEL_Skills", IDENTITY_W + gap, 0f, skillsW, contentH);
                StatsText(skills, "LBL_SkillsTitle", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_DARK,
                    "ui.profile.skills", TextAlignmentOptions.MidlineLeft, 0f, 0f, skillsW, line);
                var bars = new DimensionBar[5];
                float barsTop = line + 6f;
                float barStep = line + 6f;
                for (int i = 0; i < bars.Length; i++)
                {
                    GameObject barGo = StatsInstance(skills, "LBL_DimensionBar", "LBL_DimensionBar_" + (DimensionKind)i, 0f, barsTop + i * barStep, skillsW, line);
                    bars[i] = barGo.GetComponent<DimensionBar>();
                    bars[i].SetSurface(LabelSurface.Dark);
                    bars[i].SetDimension((DimensionKind)i, 0);
                    BaseBuilder.RecordInstanceOverrides(barGo);
                }

                // Badge: titolo "Badge · N su M" e griglia 6 per riga (2 righe di BTN_Badge 64, gap 12).
                float badgesTop = barsTop + (bars.Length - 1) * barStep + line + 12f;
                UIStringText badgesTitle = StatsText(skills, "LBL_BadgesTitle", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_ON_DARK,
                    string.Empty, TextAlignmentOptions.MidlineLeft, 0f, badgesTop, skillsW, line);
                float gridTop = badgesTop + line + 8f;
                float badgeSize = theme.GetNumber(ButtonTokens.BADGE);
                float gridH = 2f * badgeSize + 12f;
                RectTransform grid = StatsRect(skills, "PANEL_BadgeGrid", 0f, gridTop, skillsW, gridH);
                BadgeButton badgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ComponentsBuilder.PrefabPath("BTN_Badge")).GetComponent<BadgeButton>();

                // Dettaglio badge (non progettato in Figma: scheda bianca semplice al posto della griglia).
                RectTransform detail = StatsAddCard(skills, "PANEL_BadgeDetail", theme, 0f, gridTop, skillsW, gridH);
                float backSize = theme.TargetMinSize;
                float detailPad = theme.GetNumber(NumberTokens.SPACE_M);
                GameObject backGo = StatsInstance(detail, "BTN_Back", "BTN_Back", detailPad, detailPad, backSize, backSize);
                StatsMarkInCardBack(backGo);
                const float STATE_W = 96f;
                UIStringText detailTitle = StatsText(detail, "LBL_BadgeTitle", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    string.Empty, TextAlignmentOptions.MidlineLeft, detailPad * 2f + backSize, detailPad,
                    skillsW - detailPad * 3f - backSize - STATE_W, backSize);
                detailTitle.GetComponent<TMP_Text>().overflowMode = TextOverflowModes.Ellipsis;
                UIStringText detailState = StatsText(detail, "LBL_BadgeState", theme, strings, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_SECONDARY,
                    string.Empty, TextAlignmentOptions.MidlineRight, skillsW - detailPad - STATE_W, detailPad, STATE_W, backSize);
                float descTop = detailPad * 2f + backSize;
                UIStringText detailDesc = StatsText(detail, "LBL_BadgeDescription", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY,
                    string.Empty, TextAlignmentOptions.TopLeft, detailPad, descTop, skillsW - 2f * detailPad, gridH - descTop - detailPad);
                detailDesc.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;
                detail.gameObject.SetActive(false);
                grid.gameObject.SetActive(true);

                // Componente della schermata.
                var profile = BaseBuilder.GetOrAdd<ProfileScreen>(root);
                profile.Configure(theme, strings, name.GetComponent<TMP_Text>(), missions.GetComponent<TMP_Text>(), recentRows,
                    overallScore.GetComponent<TMP_Text>(), overallStars, bars, badgesTitle.GetComponent<TMP_Text>(), badgePrefab, grid, detail.gameObject,
                    detailTitle.GetComponent<TMP_Text>(), detailState.GetComponent<TMP_Text>(), detailDesc.GetComponent<TMP_Text>(),
                    backGo.GetComponent<UIButton>());
                StatsWireOnShown(screen, profile, profile.Refresh);
                EditorUtility.SetDirty(profile);
                Debug.Log("[SI81 UI] " + StatsSavePrefab(root, "PANEL_Profile", existed));
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[SI81 UI] PANEL_Profile: errore, prefab non salvato. " + exception);
                StatsDiscard(root, existed);
            }
        }

        /// <summary>
        /// Riduzione automatica del testo (da 12 alla dimensione dello stile) per righe strette: titoli lunghi come "07 · Forklift Chaos"
        /// restano su una riga nei 240 della colonna, senza tagliarsi.
        /// </summary>
        /// <param name="text">Testo TMP.</param>
        /// <param name="theme">Tema (dimensione massima = Body).</param>
        private static void ProfileAutoSize(TMP_Text text, UITheme theme)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = theme.Body.Size;
            EditorUtility.SetDirty(text);
        }
    }
}
