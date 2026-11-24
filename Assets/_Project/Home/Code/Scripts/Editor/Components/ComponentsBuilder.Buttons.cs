using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "pulsanti" del builder dei componenti (Blocco A): crea o aggiorna i prefab BTN_Primary, BTN_Secondary, BTN_Back,
    /// BTN_Badge, BTN_SettingsTab, BTN_ListRow e PANEL_ConfirmDialog in Assets/_Project/Home/UI/Prefabs/Components/.
    /// Struttura comune dei pulsanti: IMG_Glow / IMG_Shadow (UIStateVisual), IMG_Border (solo BTN_Primary), IMG_Fill (area dei raggi),
    /// ICN_Leading, LBL_Label, IMG_Check (selezione). Sicuro da rieseguire (stessi GUID, figli per nome). Dati e colori dal tema.
    /// </summary>
    internal static partial class ComponentsBuilder
    {
        /// <summary>Prefab BTN_Primary.</summary>
        private const string PRIMARY_NAME = "BTN_Primary";
        /// <summary>Prefab BTN_Secondary.</summary>
        private const string SECONDARY_NAME = "BTN_Secondary";
        /// <summary>Prefab BTN_Back.</summary>
        private const string BACK_NAME = "BTN_Back";
        /// <summary>Prefab BTN_Badge.</summary>
        private const string BADGE_NAME = "BTN_Badge";
        /// <summary>Prefab BTN_SettingsTab.</summary>
        private const string TAB_NAME = "BTN_SettingsTab";
        /// <summary>Prefab BTN_ListRow.</summary>
        private const string LIST_ROW_NAME = "BTN_ListRow";
        /// <summary>Prefab PANEL_ConfirmDialog.</summary>
        private const string DIALOG_NAME = "PANEL_ConfirmDialog";

        /// <summary>Descrizione di un pulsante da costruire.</summary>
        private struct ButtonSpec
        {
            /// <summary>Stile (Primary/Secondary/Danger).</summary>
            public UIButtonStyle Style;
            /// <summary>Chiave dell'etichetta ("" = nessuna etichetta).</summary>
            public string LabelKey;
            /// <summary>Chiave dell'etichetta da disabilitato.</summary>
            public string DisabledKey;
            /// <summary>Nome dell'icona iniziale ("" = nessuna).</summary>
            public string IconName;
            /// <summary>Larghezza fissa (0 = secondo il contenuto).</summary>
            public float FixedWidth;
            /// <summary>Dimensione iniziale della radice.</summary>
            public Vector2 Size;
            /// <summary>true se ha l'etichetta LBL_Label.</summary>
            public bool HasLabel;
            /// <summary>true se ha IMG_Border (BTN_Primary).</summary>
            public bool HasBorder;
            /// <summary>Nome del figlio etichetta.</summary>
            public string LabelName;
            /// <summary>Allineamento dell'etichetta.</summary>
            public TextAlignmentOptions Alignment;
            /// <summary>Stile di testo iniziale.</summary>
            public string TextStyle;
            /// <summary>Ombra card anche a riposo.</summary>
            public bool ShadowAtRest;
            /// <summary>Supporta Selected.</summary>
            public bool Selectable;
            /// <summary>Crea IMG_Glow.</summary>
            public bool Glow;
            /// <summary>Crea il cerchio di selezione.</summary>
            public bool Check;
            /// <summary>Cerchio compatto.</summary>
            public bool CompactCheck;
            /// <summary>Raggio degli angoli.</summary>
            public float Radius;
        }

        /// <summary>Elementi comuni di un pulsante appena costruito.</summary>
        private struct ButtonParts
        {
            /// <summary>Componente Button del prefab.</summary>
            public Button Button;
            /// <summary>Contenitore visivo (scala, ombra, stati).</summary>
            public UIStateVisual Visual;
            /// <summary>Immagine di riempimento.</summary>
            public Image Fill;
            /// <summary>Immagine del bordo.</summary>
            public Image Border;
            /// <summary>Testo con chiave dalla tabella stringhe.</summary>
            public UIStringText Label;
            /// <summary>Immagine dell'icona.</summary>
            public Image Icon;
        }

        /// <summary>
        /// Crea o aggiorna i 7 prefab dei pulsanti e del dialogo. Chiamato da <c>BuildAll</c> di ComponentsBuilder (per primo: le card
        /// annidano BTN_Primary).
        /// </summary>
        private static void BuildButtons()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] Pulsanti: interrotto, SO_UITheme / SO_UIStrings_It mancanti. Eseguire SI81/UI/Build Theme.");
                return;
            }
            BaseBuilder.EnsureFolder(COMPONENTS_FOLDER);
            ShapeSprites button = EnsureShapeSprites(theme, "Button", theme.RadiusButton);

            var report = new System.Text.StringBuilder("[SI81 UI] Pulsanti:");
            report.Append("\n ").Append(ButtonsBuildPrimary(theme, strings, button));
            report.Append("\n ").Append(ButtonsBuildSecondary(theme, strings, button));
            report.Append("\n ").Append(ButtonsBuildBack(theme, strings, button));
            report.Append("\n ").Append(ButtonsBuildBadge(theme, strings));
            report.Append("\n ").Append(ButtonsBuildTab(theme, strings, button));
            report.Append("\n ").Append(ButtonsBuildListRow(theme, strings, button));
            report.Append("\n ").Append(ButtonsBuildDialog(theme, strings));
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Ricostruisce BTN_ListRow dopo le etichette, così può annidare un'istanza di LBL_StarRating nel posto delle stelle.
        /// </summary>
        private static void BuildListRowAfterLabels()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null) return;
            ShapeSprites button = EnsureShapeSprites(theme, "Button", theme.RadiusButton);
            Debug.Log("[SI81 UI] " + ButtonsBuildListRow(theme, strings, button));
        }

        // =====================================================================
        // Scheletro comune
        // =====================================================================

        /// <summary>
        /// Costruisce gli elementi comuni di un pulsante sotto <paramref name="root"/>: UIStateVisual (ombra/glow/spunta), bordo,
        /// riempimento, icona ed etichetta, Button uGUI senza transizioni né navigazione. Ordine dei fratelli: Glow, Shadow,
        /// Border, Fill, Icon, Label, Check.
        /// </summary>
        private static ButtonParts ButtonsBuildParts(GameObject root, ButtonSpec spec, UITheme theme, UIStrings strings, ShapeSprites shape)
        {
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = spec.Size;

            var parts = new ButtonParts();
            parts.Visual = AddStateVisual(root, theme, shape, spec.Radius, spec.Selectable, spec.ShadowAtRest, spec.Glow, spec.Check, spec.CompactCheck);
            if (spec.HasBorder) parts.Border = AddImage(root.transform, "IMG_Border", shape.Rounded, theme.GetColor(ColorTokens.BORDER_ON_DARK), false);
            parts.Fill = AddImage(root.transform, "IMG_Fill", shape.Rounded, theme.StateActive, true);
            if (!string.IsNullOrEmpty(spec.IconName)) parts.Icon = AddIcon(root.transform, "ICN_Leading", theme, spec.IconName, theme.GetNumber(ButtonTokens.ICON), ColorTokens.TEXT_PRIMARY);
            if (spec.HasLabel)
                parts.Label = AddLabel(root.transform, spec.LabelName, theme, strings, spec.TextStyle, ColorTokens.TEXT_PRIMARY, spec.LabelKey, spec.Alignment);

            parts.Button = BaseBuilder.GetOrAdd<Button>(root);
            parts.Button.targetGraphic = parts.Fill;
            parts.Button.transition = Selectable.Transition.None;
            Navigation navigation = parts.Button.navigation;
            navigation.mode = Navigation.Mode.None;
            parts.Button.navigation = navigation;

            // L'ordine dei fratelli: bordo, riempimento, icona, etichetta (dopo ombra/glow), spunta sempre ultima.
            int index = parts.Visual.Glow != null ? 2 : 1;
            if (parts.Border != null) parts.Border.transform.SetSiblingIndex(index++);
            parts.Fill.transform.SetSiblingIndex(index++);
            if (parts.Icon != null) parts.Icon.transform.SetSiblingIndex(index++);
            if (parts.Label != null) parts.Label.transform.SetSiblingIndex(index);
            if (parts.Visual.CheckMark != null) parts.Visual.CheckMark.transform.SetAsLastSibling();
            return parts;
        }

        /// <summary>
        /// Configura il componente UIButton (o una sottoclasse) con gli elementi costruiti.
        /// </summary>
        private static void ButtonsConfigure(UIButton component, ButtonSpec spec, UITheme theme, ButtonParts parts, UIButtonSurface surface)
        {
            Sprite icon = string.IsNullOrEmpty(spec.IconName) ? null : LoadIcon(spec.IconName);
            component.Configure(theme, spec.Style, surface, spec.LabelKey, spec.DisabledKey, icon, spec.FixedWidth,
                parts.Button, parts.Visual, parts.Fill, parts.Border, parts.Label, parts.Icon);
            EditorUtility.SetDirty(component);
        }

        /// <summary>Specifica di base dei pulsanti con etichetta centrata (Primary/Secondary).</summary>
        private static ButtonSpec ButtonsBaseSpec(UITheme theme)
        {
            return new ButtonSpec
            {
                HasLabel = true,
                LabelName = "LBL_Label",
                Alignment = TextAlignmentOptions.Center,
                TextStyle = TextStyleIds.SUBTITLE_BOLD,
                ShadowAtRest = true,
                Radius = theme.RadiusButton,
                Size = new Vector2(theme.GetNumber(ButtonTokens.PRIMARY_WIDTH), theme.GetNumber(ButtonTokens.HEIGHT))
            };
        }

        // =====================================================================
        // Prefab
        // =====================================================================

        /// <summary>
        /// BTN_Primary: 200×48, state.active, bordo 2 (Dark), ICN_PlayArrow + "Avvia scenario"; disabilitato = motivo (ui.button.choose_level).
        /// </summary>
        private static string ButtonsBuildPrimary(UITheme theme, UIStrings strings, ShapeSprites shape)
        {
            GameObject root = CreateOrLoadPrefab(PRIMARY_NAME, out bool existed);
            ButtonSpec spec = ButtonsBaseSpec(theme);
            spec.Style = UIButtonStyle.Primary;
            spec.LabelKey = "ui.button.start_scenario";
            spec.DisabledKey = "ui.button.choose_level";
            spec.IconName = "ICN_PlayArrow";
            spec.FixedWidth = theme.GetNumber(ButtonTokens.PRIMARY_WIDTH);
            spec.HasBorder = true;
            ButtonParts parts = ButtonsBuildParts(root, spec, theme, strings, shape);
            ButtonsConfigure(BaseBuilder.GetOrAdd<UIButton>(root), spec, theme, parts, UIButtonSurface.Dark);
            return SavePrefab(root, PRIMARY_NAME, existed);
        }

        /// <summary>
        /// BTN_Secondary: altezza 48, larghezza secondo il contenuto, surface.card (Neutral; Danger = SetStyle), ICN_RestartAlt + "Ricomincia".
        /// </summary>
        private static string ButtonsBuildSecondary(UITheme theme, UIStrings strings, ShapeSprites shape)
        {
            GameObject root = CreateOrLoadPrefab(SECONDARY_NAME, out bool existed);
            ButtonSpec spec = ButtonsBaseSpec(theme);
            spec.Style = UIButtonStyle.Secondary;
            spec.LabelKey = "ui.button.restart";
            spec.DisabledKey = string.Empty;
            spec.IconName = "ICN_RestartAlt";
            spec.FixedWidth = 0f;
            ButtonParts parts = ButtonsBuildParts(root, spec, theme, strings, shape);
            ButtonsConfigure(BaseBuilder.GetOrAdd<UIButton>(root), spec, theme, parts, UIButtonSurface.Dark);
            return SavePrefab(root, SECONDARY_NAME, existed);
        }

        /// <summary>
        /// BTN_Back: 40×40, surface.card senza ombra a riposo, ICN_ArrowBack 24; il clic chiama UIScreenManager.Back.
        /// </summary>
        private static string ButtonsBuildBack(UITheme theme, UIStrings strings, ShapeSprites shape)
        {
            GameObject root = CreateOrLoadPrefab(BACK_NAME, out bool existed);
            float side = theme.GetNumber(ButtonTokens.BACK);
            ButtonSpec spec = ButtonsBaseSpec(theme);
            spec.Style = UIButtonStyle.Secondary;
            spec.HasLabel = false;
            spec.LabelKey = string.Empty;
            spec.DisabledKey = string.Empty;
            spec.IconName = "ICN_ArrowBack";
            spec.ShadowAtRest = false;
            spec.Size = new Vector2(side, side);
            ButtonParts parts = ButtonsBuildParts(root, spec, theme, strings, shape);
            ButtonsConfigure(BaseBuilder.GetOrAdd<BackButton>(root), spec, theme, parts, UIButtonSurface.Dark);
            return SavePrefab(root, BACK_NAME, existed);
        }

        /// <summary>
        /// BTN_Badge: 64×64; sbloccato = cerchio brand.light + icona piena, bloccato = quadrato tile.neutral + lucchetto + "Bloccato".
        /// Sprite cerchio (TEX_BadgeRounded) e quadrato (TEX_BadgeLockedRounded) con le rispettive ombre.
        /// </summary>
        private static string ButtonsBuildBadge(UITheme theme, UIStrings strings)
        {
            float side = theme.GetNumber(ButtonTokens.BADGE);
            ShapeSprites round = EnsureShapeSprites(theme, "Badge", side * 0.5f);
            ShapeSprites square = EnsureShapeSprites(theme, "Card", theme.RadiusCard);
            GameObject root = CreateOrLoadPrefab(BADGE_NAME, out bool existed);
            ButtonSpec spec = ButtonsBaseSpec(theme);
            spec.Style = UIButtonStyle.Secondary;
            spec.LabelKey = "ui.profile.badge_locked";
            spec.DisabledKey = string.Empty;
            spec.IconName = "ICN_WorkspacePremium_Filled";
            spec.TextStyle = TextStyleIds.BODY;
            spec.Radius = side * 0.5f;
            spec.Size = new Vector2(side, side);
            spec.ShadowAtRest = true;
            ButtonParts parts = ButtonsBuildParts(root, spec, theme, strings, round);
            var badge = BaseBuilder.GetOrAdd<BadgeButton>(root);
            ButtonsConfigure(badge, spec, theme, parts, UIButtonSurface.Dark);
            badge.ConfigureBadge(true, LoadIcon("ICN_WorkspacePremium_Filled"), LoadIcon("ICN_Lock"), round.Rounded, square.Rounded,
                round.ShadowRest, round.ShadowLift, square.ShadowRest, square.ShadowLift);
            return SavePrefab(root, BADGE_NAME, existed);
        }

        /// <summary>
        /// BTN_SettingsTab: 180×48, icona 24 + etichetta Body; Active = Selected (fondo surface.card, icona piena, Bold brand.dark).
        /// </summary>
        private static string ButtonsBuildTab(UITheme theme, UIStrings strings, ShapeSprites shape)
        {
            GameObject root = CreateOrLoadPrefab(TAB_NAME, out bool existed);
            ButtonSpec spec = ButtonsBaseSpec(theme);
            spec.Style = UIButtonStyle.Secondary;
            spec.LabelKey = "ui.settings.tab.comfort";
            spec.DisabledKey = string.Empty;
            spec.IconName = "ICN_SelfImprovement";
            spec.Alignment = TextAlignmentOptions.MidlineLeft;
            spec.TextStyle = TextStyleIds.BODY;
            spec.ShadowAtRest = false;
            spec.Selectable = true;
            spec.Size = new Vector2(theme.GetNumber(ButtonTokens.TAB_WIDTH), theme.GetNumber(ButtonTokens.TAB_HEIGHT));
            ButtonParts parts = ButtonsBuildParts(root, spec, theme, strings, shape);
            var tab = BaseBuilder.GetOrAdd<SettingsTabButton>(root);
            ButtonsConfigure(tab, spec, theme, parts, UIButtonSurface.Dark);
            tab.SetData(LoadIcon("ICN_SelfImprovement"), LoadIcon("ICN_SelfImprovement_Filled"), "ui.settings.tab.comfort");
            return SavePrefab(root, TAB_NAME, existed);
        }

        /// <summary>
        /// BTN_ListRow: 440×40; LBL_Title, PANEL_StarsSlot (con LBL_StarRating annidato se il prefab esiste), LBL_Score; selezione =
        /// fondo brand.light + sollevamento + glow + spunta compatta. Testi vuoti: arrivano dai dati (SetData / SetNotPlayed).
        /// </summary>
        private static string ButtonsBuildListRow(UITheme theme, UIStrings strings, ShapeSprites shape)
        {
            GameObject root = CreateOrLoadPrefab(LIST_ROW_NAME, out bool existed);
            ButtonSpec spec = ButtonsBaseSpec(theme);
            spec.Style = UIButtonStyle.Secondary;
            spec.LabelKey = string.Empty;
            spec.DisabledKey = string.Empty;
            spec.IconName = string.Empty;
            spec.LabelName = "LBL_Title";
            spec.Alignment = TextAlignmentOptions.MidlineLeft;
            spec.TextStyle = TextStyleIds.BODY_BOLD;
            spec.ShadowAtRest = false;
            spec.Selectable = true;
            spec.Glow = true;
            spec.Check = true;
            spec.CompactCheck = true;
            spec.Size = new Vector2(theme.GetNumber(ButtonTokens.ROW_WIDTH), theme.GetNumber(ButtonTokens.ROW_HEIGHT));
            ButtonParts parts = ButtonsBuildParts(root, spec, theme, strings, shape);

            // Posto delle stelle e punteggio (dopo il titolo, prima della spunta).
            RectTransform slot = BaseBuilder.GetOrCreateRectChild(root.transform, "PANEL_StarsSlot");
            StarRating stars = null;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("LBL_StarRating")) != null)
            {
                GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(slot, PrefabPath("LBL_StarRating"), "LBL_StarRating");
                var instanceRect = (RectTransform)instance.transform;
                instanceRect.anchorMin = instanceRect.anchorMax = instanceRect.pivot = new Vector2(0.5f, 0.5f);
                instanceRect.anchoredPosition = Vector2.zero;
                stars = instance.GetComponent<StarRating>();
                BaseBuilder.RecordInstanceOverrides(instance);
            }
            UIStringText score = AddLabel(root.transform, "LBL_Score", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY, string.Empty, TextAlignmentOptions.MidlineRight);
            int after = parts.Label.transform.GetSiblingIndex() + 1;
            slot.SetSiblingIndex(after);
            score.transform.SetSiblingIndex(after + 1);
            if (parts.Visual.CheckMark != null) parts.Visual.CheckMark.transform.SetAsLastSibling();

            var row = BaseBuilder.GetOrAdd<ListRowButton>(root);
            ButtonsConfigure(row, spec, theme, parts, UIButtonSurface.Light);
            row.ConfigureRow(strings, slot, stars, score);
            // Si legge PRIMA di salvare: SavePrefab distrugge la radice e un riferimento distrutto risulterebbe "null".
            string starsNote = stars == null ? " (stelle non annidate: LBL_StarRating assente)" : " (stelle annidate)";
            return SavePrefab(root, LIST_ROW_NAME, existed) + starsNote;
        }

        /// <summary>
        /// PANEL_ConfirmDialog: 480×240 vetro; IMG_Backdrop (velo modale), IMG_Shadow, IMG_Glass, LBL_Title, LBL_Body e PANEL_Actions con
        /// BTN_Cancel (istanza di BTN_Primary Dark, ICN_Undo) e BTN_Confirm (istanza di BTN_Secondary Danger, ICN_Exit). Salvato DISATTIVO:
        /// lo apre ConfirmDialog.Show.
        /// </summary>
        private static string ButtonsBuildDialog(UITheme theme, UIStrings strings)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(PRIMARY_NAME)) == null || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(SECONDARY_NAME)) == null)
                return DIALOG_NAME + ": interrotto, BTN_Primary / BTN_Secondary mancanti.";
            var rounded = LoadSprite(SpriteBuilder.PANEL_ROUNDED);
            // Ombra del dialogo: stessa geometria dell'ombra menu (raggio panel), quindi si riusa TEX_PanelShadow (nessun duplicato).
            string shadowPath = SpriteBuilder.PANEL_SHADOW;
            if (rounded == null) return DIALOG_NAME + ": interrotto, TEX_PanelRounded mancante (eseguire SI81/UI/Build Theme).";

            GameObject root = CreateOrLoadPrefab(DIALOG_NAME, out bool existed);
            root.SetActive(true);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(theme.GetNumber(ButtonTokens.DIALOG_WIDTH), theme.GetNumber(ButtonTokens.DIALOG_HEIGHT));

            // Velo modale (blocca i raggi), ombra, vetro con riflesso.
            Image backdrop = AddImage(root.transform, "IMG_Backdrop", null, theme.GetColor(ButtonTokens.DIALOG_BACKDROP), true);
            Image shadow = AddImage(root.transform, "IMG_Shadow", LoadSprite(shadowPath), Color.white, false);
            shadow.fillCenter = false;
            RectTransform glassRect = BaseBuilder.GetOrCreateRectChild(root.transform, "IMG_Glass");
            RoundedPanelGraphic glass = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(glassRect.gameObject);
            glass.Sprite = rounded;
            glass.raycastTarget = true;
            UIComponentUtil.SetInset(glassRect, 0f);

            UIStringText title = AddLabel(root.transform, "LBL_Title", theme, strings, TextStyleIds.TITLE, ColorTokens.TEXT_ON_DARK,
                "ui.dialog.exit_mission.title", TextAlignmentOptions.MidlineLeft);
            UIStringText body = AddLabel(root.transform, "LBL_Body", theme, strings, TextStyleIds.BODY, ColorTokens.TEXT_ON_GREEN,
                "ui.dialog.exit_mission.body", TextAlignmentOptions.TopLeft);
            var bodyText = body.GetComponent<TextMeshProUGUI>();
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            EditorUtility.SetDirty(bodyText);

            RectTransform actions = BaseBuilder.GetOrCreateRectChild(root.transform, "PANEL_Actions");
            UIComponentUtil.SetInset(actions, 0f);
            GameObject cancelGo = BaseBuilder.GetOrCreateChildPrefabInstance(actions, PrefabPath(PRIMARY_NAME), "BTN_Cancel");
            GameObject confirmGo = BaseBuilder.GetOrCreateChildPrefabInstance(actions, PrefabPath(SECONDARY_NAME), "BTN_Confirm");
            var cancel = cancelGo.GetComponent<UIButton>();
            var confirm = confirmGo.GetComponent<UIButton>();
            cancel.SetStyle(UIButtonStyle.Primary, UIButtonSurface.Dark);
            cancel.SetIcon(LoadIcon("ICN_Undo"));
            cancel.SetLabelKey("ui.button.cancel");
            confirm.SetStyle(UIButtonStyle.Danger, UIButtonSurface.Dark);
            confirm.SetIcon(LoadIcon("ICN_Exit"));
            confirm.SetLabelKey("ui.button.exit");
            BaseBuilder.RecordInstanceOverrides(cancelGo);
            BaseBuilder.RecordInstanceOverrides(confirmGo);

            var dialog = BaseBuilder.GetOrAdd<ConfirmDialog>(root);
            dialog.Configure(theme, theme.MenuCanvasSize, backdrop, shadow, glass, title, body, cancel, confirm);
            backdrop.transform.SetSiblingIndex(0);
            shadow.transform.SetSiblingIndex(1);
            glassRect.SetSiblingIndex(2);
            EditorUtility.SetDirty(dialog);
            root.SetActive(false);
            return SavePrefab(root, DIALOG_NAME, existed);
        }
    }
}
