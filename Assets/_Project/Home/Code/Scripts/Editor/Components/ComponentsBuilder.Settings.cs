using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "righe di Impostazioni" del builder dei componenti (Blocco A): crea o aggiorna i prefab
    /// PANEL_SettingsToggle, PANEL_SettingsSlider, PANEL_SettingsSelector e PANEL_SettingsAction in Prefabs/Components/.
    /// Chiamata da <c>ComponentsBuilder.BuildAll</c> (menu SI81/UI/Build Components). Sicuro da rieseguire: aggiorna i prefab sul posto
    /// (stessi GUID), trova i figli per nome, non duplica nulla. Richiede tema, testi, icone e sprite (SI81/UI/Build Theme).
    /// Sprite propri (TEX_SettingsRow*, TEX_Control*) generati con i generatori di SpriteBuilder.
    /// </summary>
    internal static partial class ComponentsBuilder
    {
        // =====================================================================
        // Percorsi e nomi
        // =====================================================================

        /// <summary>Cartella dei prefab dei componenti.</summary>
        private const string SETTINGS_PREFABS_FOLDER = BaseBuilder.PREFABS_FOLDER + "/Components";
        /// <summary>Prefab della riga toggle.</summary>
        private const string SETTINGS_TOGGLE_PATH = SETTINGS_PREFABS_FOLDER + "/PANEL_SettingsToggle.prefab";
        /// <summary>Prefab della riga slider.</summary>
        private const string SETTINGS_SLIDER_PATH = SETTINGS_PREFABS_FOLDER + "/PANEL_SettingsSlider.prefab";
        /// <summary>Prefab della riga selettore.</summary>
        private const string SETTINGS_SELECTOR_PATH = SETTINGS_PREFABS_FOLDER + "/PANEL_SettingsSelector.prefab";
        /// <summary>Prefab della riga azione.</summary>
        private const string SETTINGS_ACTION_PATH = SETTINGS_PREFABS_FOLDER + "/PANEL_SettingsAction.prefab";

        /// <summary>Fondo arrotondato della riga e del pulsante azione (radius.button): riusa TEX_ButtonRounded (identico, nessun duplicato).</summary>
        private const string SETTINGS_ROW_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_ButtonRounded.png";
        /// <summary>Ombra a riposo della riga (geometria dell'ombra card).</summary>
        private const string SETTINGS_SHADOW_REST_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_SettingsRowShadowRest.png";
        /// <summary>Ombra di hover della riga (geometria selected.lift).</summary>
        private const string SETTINGS_SHADOW_LIFT_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_SettingsRowShadowLift.png";
        /// <summary>Pillola alta 32 (traccia del toggle; anche manopola e maniglia, ridotte in proporzione, restano cerchi).</summary>
        private const string SETTINGS_PILL_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_ControlPill.png";
        /// <summary>Pillola alta 12 (traccia e riempimento dello slider).</summary>
        private const string SETTINGS_PILL_SMALL_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_ControlPillSmall.png";
        /// <summary>Cerchio di 40 (frecce del selettore).</summary>
        private const string SETTINGS_CIRCLE_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_ControlCircle.png";

        /// <summary>Sprite usati dalle quattro righe.</summary>
        private sealed class SettingsSprites
        {
            /// <summary>Sprite della riga.</summary>
            public Sprite Row;
            /// <summary>Sprite dell'ombra a riposo.</summary>
            public Sprite ShadowRest;
            /// <summary>Sprite dell'ombra sollevata.</summary>
            public Sprite ShadowLift;
            /// <summary>Sprite a pillola.</summary>
            public Sprite Pill;
            /// <summary>Sprite a pillola piccola.</summary>
            public Sprite PillSmall;
            /// <summary>Sprite circolare.</summary>
            public Sprite Circle;
        }

        /// <summary>Parti comuni di una riga: fondo, ombra, gruppo, etichetta e descrizione.</summary>
        private struct SettingsChrome
        {
            /// <summary>Sfondo arrotondato.</summary>
            public RoundedPanelGraphic Background;
            /// <summary>Immagine dell'ombra.</summary>
            public Image Shadow;
            /// <summary>CanvasGroup della riga.</summary>
            public CanvasGroup Group;
            /// <summary>Testo con chiave dalla tabella stringhe.</summary>
            public UIStringText Label;
            /// <summary>Testo della descrizione.</summary>
            public UIStringText Description;
        }

        // =====================================================================
        // Punto d'ingresso
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna gli sprite e i quattro prefab delle righe di Impostazioni e scrive il riepilogo in Console.
        /// </summary>
        private static void BuildSettingsRows()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] Righe Impostazioni: interrotto, SO_UITheme o SO_UIStrings_It mancanti (eseguire SI81/UI/Build Theme).");
                return;
            }
            if (theme.GetNumber(SettingsTokens.ROW_HEIGHT) <= 0f)
            {
                Debug.LogError("[SI81 UI] Righe Impostazioni: interrotto, token size.settings-row.* assenti in SO_UITheme (rieseguire SI81/UI/Build Theme).");
                return;
            }
            BaseBuilder.EnsureFolder(SETTINGS_PREFABS_FOLDER);
            SettingsSprites sprites = SettingsBuildSprites(theme);
            if (sprites == null) return;

            var report = new StringBuilder("[SI81 UI] Righe Impostazioni:\n");
            report.AppendLine(SettingsBuildToggle(theme, strings, sprites));
            report.AppendLine(SettingsBuildSlider(theme, strings, sprites));
            report.AppendLine(SettingsBuildSelector(theme, strings, sprites));
            report.AppendLine(SettingsBuildAction(theme, strings, sprites));
            Debug.Log(report.ToString().TrimEnd());
        }

        // =====================================================================
        // Sprite
        // =====================================================================

        /// <summary>
        /// Genera (se mancano o cambiano) gli sprite delle righe e li carica.
        /// </summary>
        private static SettingsSprites SettingsBuildSprites(UITheme theme)
        {
            UITheme.ShadowToken card = theme.GetShadow(ShadowRoles.CARD);
            UITheme.ShadowToken lift = theme.GetShadow(ShadowRoles.SELECTED_LIFT);
            if (card == null || lift == null)
            {
                Debug.LogError("[SI81 UI] Righe Impostazioni: interrotto, ombre \"card\" / \"selected.lift\" assenti in SO_UITheme.");
                return null;
            }
            BaseBuilder.EnsureFolder(SpriteBuilder.SPRITES_FOLDER);
            float rowHeight = theme.GetNumber(SettingsTokens.ROW_HEIGHT);
            SpriteBuilder.BuildRoundedSprite(SETTINGS_ROW_SPRITE, theme.RadiusButton);
            SpriteBuilder.BuildShadowSprite(SETTINGS_SHADOW_REST_SPRITE, theme.RadiusButton, card, rowHeight);
            SpriteBuilder.BuildShadowSprite(SETTINGS_SHADOW_LIFT_SPRITE, theme.RadiusButton, lift, rowHeight);
            SpriteBuilder.BuildRoundedSprite(SETTINGS_PILL_SPRITE, theme.GetNumber(SettingsTokens.TOGGLE_TRACK_HEIGHT) * 0.5f);
            SpriteBuilder.BuildRoundedSprite(SETTINGS_PILL_SMALL_SPRITE, theme.GetNumber(SettingsTokens.SLIDER_TRACK_HEIGHT) * 0.5f);
            SpriteBuilder.BuildRoundedSprite(SETTINGS_CIRCLE_SPRITE, theme.GetNumber(SettingsTokens.SELECTOR_ARROW) * 0.5f);
            AssetDatabase.SaveAssets();

            var sprites = new SettingsSprites
            {
                Row = AssetDatabase.LoadAssetAtPath<Sprite>(SETTINGS_ROW_SPRITE),
                ShadowRest = AssetDatabase.LoadAssetAtPath<Sprite>(SETTINGS_SHADOW_REST_SPRITE),
                ShadowLift = AssetDatabase.LoadAssetAtPath<Sprite>(SETTINGS_SHADOW_LIFT_SPRITE),
                Pill = AssetDatabase.LoadAssetAtPath<Sprite>(SETTINGS_PILL_SPRITE),
                PillSmall = AssetDatabase.LoadAssetAtPath<Sprite>(SETTINGS_PILL_SMALL_SPRITE),
                Circle = AssetDatabase.LoadAssetAtPath<Sprite>(SETTINGS_CIRCLE_SPRITE),
            };
            if (sprites.Row == null || sprites.ShadowRest == null || sprites.ShadowLift == null || sprites.Pill == null
                || sprites.PillSmall == null || sprites.Circle == null)
            {
                Debug.LogError("[SI81 UI] Righe Impostazioni: interrotto, sprite TEX_SettingsRow* / TEX_Control* non generati o non importati.");
                return null;
            }
            return sprites;
        }

        /// <summary>
        /// Carica lo sprite di un'icona ICN_ per nome (segnala un errore se manca).
        /// </summary>
        private static Sprite SettingsLoadIcon(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconBuilder.ICONS_FOLDER + "/" + name + ".png");
            if (sprite == null) Debug.LogError($"[SI81 UI] Icona {name} assente. Eseguire SI81/UI/Build Theme.");
            return sprite;
        }

        // =====================================================================
        // Parti comuni
        // =====================================================================

        /// <summary>
        /// Apre un prefab esistente (contenuto modificabile) o crea una radice UI nuova, con la misura della riga.
        /// </summary>
        private static GameObject SettingsOpenRoot(string path, string name, UITheme theme, out bool exists)
        {
            exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name, typeof(RectTransform));
            if (exists) RemoveMissingScripts(root);
            root.name = name;
            root.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(theme.GetNumber(SettingsTokens.ROW_WIDTH), theme.GetNumber(SettingsTokens.ROW_HEIGHT));
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return root;
        }

        /// <summary>
        /// Salva la radice come prefab, la chiude e restituisce il riepilogo.
        /// </summary>
        private static string SettingsSaveRoot(GameObject root, string path, string name, bool exists)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
            return success ? $"{name}: {(exists ? "aggiornato" : "creato")} ({path})." : $"{name}: ERRORE nel salvataggio del prefab.";
        }

        /// <summary>
        /// Chiude la radice senza salvare (dopo un errore): scarica il contenuto del prefab o distrugge l'oggetto temporaneo.
        /// </summary>
        private static void SettingsDiscardRoot(GameObject root, bool exists)
        {
            if (root == null) return;
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Crea o aggiorna fondo (IMG_Background, bersaglio dei raggi), ombra opzionale (IMG_Shadow), etichetta e descrizione
        /// (nascosta di default) della riga, nell'ordine di disegno corretto.
        /// </summary>
        private static SettingsChrome SettingsBuildChrome(RectTransform root, UITheme theme, UIStrings strings, SettingsSprites sprites, bool withShadow)
        {
            var chrome = new SettingsChrome { Group = BaseBuilder.GetOrAdd<CanvasGroup>(root.gameObject) };
            int index = 0;
            if (withShadow)
            {
                RectTransform shadowRect = BaseBuilder.GetOrCreateRectChild(root, "IMG_Shadow");
                chrome.Shadow = BaseBuilder.GetOrAdd<Image>(shadowRect.gameObject);
                chrome.Shadow.sprite = sprites.ShadowRest;
                chrome.Shadow.type = Image.Type.Sliced;
                chrome.Shadow.fillCenter = false;
                chrome.Shadow.pixelsPerUnitMultiplier = 1f;
                chrome.Shadow.raycastTarget = false;
                // Invisibile a riposo: il colore e l'alpha li imposta UIStateVisual (hover = ombra sollevata).
                Color shadowColor = theme.GetShadow(ShadowRoles.CARD).Color;
                shadowColor.a = 0f;
                chrome.Shadow.color = shadowColor;
                shadowRect.SetSiblingIndex(index++);
            }

            RectTransform backgroundRect = BaseBuilder.GetOrCreateRectChild(root, "IMG_Background");
            chrome.Background = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(backgroundRect.gameObject);
            chrome.Background.Sprite = sprites.Row;
            chrome.Background.DisableSheen();
            chrome.Background.color = theme.SurfaceCard;
            chrome.Background.raycastTarget = true;
            BaseBuilder.Stretch(backgroundRect);
            backgroundRect.SetSiblingIndex(index++);

            chrome.Label = SettingsBuildText(root, "LBL_Label", strings, theme, TextStyleIds.BODY, ColorTokens.TEXT_PRIMARY, TextAlignmentOptions.MidlineLeft);
            chrome.Label.transform.SetSiblingIndex(index++);
            chrome.Description = SettingsBuildText(root, "LBL_Description", strings, theme, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY, TextAlignmentOptions.MidlineLeft);
            chrome.Description.transform.SetSiblingIndex(index);
            return chrome;
        }

        /// <summary>
        /// Crea o aggiorna un'etichetta TMP (UIStringText) con l'allineamento dato.
        /// </summary>
        private static UIStringText SettingsBuildText(Transform parent, string name, UIStrings strings, UITheme theme, string style, string colorToken,
            TextAlignmentOptions alignment)
        {
            UIStringText text = MenuPanelBuilder.BuildLabel(parent, name, strings, theme, style, colorToken);
            var tmp = text.GetComponent<TextMeshProUGUI>();
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            return text;
        }

        /// <summary>
        /// Crea o aggiorna una grafica arrotondata decorativa o interattiva figlia di <paramref name="parent"/>.
        /// </summary>
        private static RoundedPanelGraphic SettingsBuildGraphic(Transform parent, string name, Sprite sprite, Color color, bool raycast)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var graphic = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(rect.gameObject);
            graphic.Sprite = sprite;
            graphic.DisableSheen();
            graphic.color = color;
            graphic.raycastTarget = raycast;
            return graphic;
        }

        /// <summary>
        /// Crea o aggiorna un'icona ICN_ decorativa (sprite bianco tinto dal tema) di 24.
        /// </summary>
        private static Image SettingsBuildIcon(Transform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var image = BaseBuilder.GetOrAdd<Image>(rect.gameObject);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Aggiunge o aggiorna UIStateVisual su un elemento della riga (scala hover/pressed; con ombra se data).
        /// </summary>
        private static UIStateVisual SettingsAddVisual(GameObject go, UITheme theme, SettingsSprites sprites, Image shadow, bool scaleOnHover)
        {
            var visual = BaseBuilder.GetOrAdd<UIStateVisual>(go);
            visual.Theme = theme;
            visual.ScaleTarget = (RectTransform)go.transform;
            visual.Shadow = shadow;
            visual.Glow = null;
            visual.CheckMark = null;
            visual.ScaleOnHover = scaleOnHover;
            visual.Selectable = false;
            // L'ombra della riga compare solo in hover (Figma: righe piatte a riposo).
            visual.ShadowAtRest = false;
            visual.ShadowRestSprite = sprites.ShadowRest;
            visual.ShadowLiftSprite = sprites.ShadowLift;
            visual.CornerRadius = theme.RadiusButton;
            EditorUtility.SetDirty(visual);
            return visual;
        }

        /// <summary>
        /// Controlla che le chiavi esistano in SO_UIStrings_It (segnala con un errore quelle assenti).
        /// </summary>
        private static void SettingsCheckKeys(UIStrings strings, params string[] keys)
        {
            foreach (string key in keys)
                if (!string.IsNullOrEmpty(key) && !strings.Contains(key)) Debug.LogError($"[SI81 UI] Chiave \"{key}\" assente in SO_UIStrings_It.");
        }

        // =====================================================================
        // PANEL_SettingsToggle
        // =====================================================================

        /// <summary>
        /// PANEL_SettingsToggle: riga con ombra di hover, etichetta, LBL_State (ON/OFF) e interruttore (IMG_Track + IMG_Knob).
        /// Dati di default: Comfort · Modalità seduto, spento.
        /// </summary>
        private static string SettingsBuildToggle(UITheme theme, UIStrings strings, SettingsSprites sprites)
        {
            const string NAME = "PANEL_SettingsToggle";
            const string LABEL_KEY = "ui.settings.comfort.seated_mode";
            SettingsCheckKeys(strings, LABEL_KEY, SettingsToggleRow.STATE_ON_KEY, SettingsToggleRow.STATE_OFF_KEY);
            GameObject root = SettingsOpenRoot(SETTINGS_TOGGLE_PATH, NAME, theme, out bool exists);
            try
            {
                var rect = (RectTransform)root.transform;
                SettingsChrome chrome = SettingsBuildChrome(rect, theme, strings, sprites, true);

                UIStringText state = SettingsBuildText(rect, "LBL_State", strings, theme, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    TextAlignmentOptions.MidlineRight);
                RoundedPanelGraphic track = SettingsBuildGraphic(rect, "IMG_Track", sprites.Pill, theme.TextSecondary, false);
                RoundedPanelGraphic knob = SettingsBuildGraphic(track.transform, "IMG_Knob", sprites.Pill, theme.TextOnDark, false);

                var row = BaseBuilder.GetOrAdd<SettingsToggleRow>(root);
                row.ConfigureBase(theme, strings, "comfort.seated_mode", LABEL_KEY, string.Empty, chrome.Background, chrome.Group, chrome.Label, chrome.Description);
                row.ConfigureToggle(track, knob, state, false);
                SettingsAddVisual(root, theme, sprites, chrome.Shadow, true);
                row.Apply();
                return SettingsSaveRoot(root, SETTINGS_TOGGLE_PATH, NAME, exists);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                SettingsDiscardRoot(root, exists);
                return $"{NAME}: ERRORE, vedere la Console.";
            }
        }

        // =====================================================================
        // PANEL_SettingsSlider
        // =====================================================================

        /// <summary>
        /// PANEL_SettingsSlider: etichetta, LBL_Value, traccia (IMG_Track) con riempimento (IMG_Fill), maniglia (IMG_Handle) e
        /// area di presa (IMG_GrabArea). Dati di default: Audio · Volume generale, 0–100, predefinito 80.
        /// </summary>
        private static string SettingsBuildSlider(UITheme theme, UIStrings strings, SettingsSprites sprites)
        {
            const string NAME = "PANEL_SettingsSlider";
            const string LABEL_KEY = "ui.settings.audio.master_volume";
            SettingsCheckKeys(strings, LABEL_KEY);
            GameObject root = SettingsOpenRoot(SETTINGS_SLIDER_PATH, NAME, theme, out bool exists);
            try
            {
                var rect = (RectTransform)root.transform;
                SettingsChrome chrome = SettingsBuildChrome(rect, theme, strings, sprites, false);

                UIStringText value = SettingsBuildText(rect, "LBL_Value", strings, theme, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    TextAlignmentOptions.MidlineRight);
                RoundedPanelGraphic track = SettingsBuildGraphic(rect, "IMG_Track", sprites.PillSmall, theme.TileNeutral, false);
                RoundedPanelGraphic fill = SettingsBuildGraphic(track.transform, "IMG_Fill", sprites.PillSmall, theme.BrandDark, false);
                RoundedPanelGraphic handle = SettingsBuildGraphic(track.transform, "IMG_Handle", sprites.Pill, theme.BrandDark, false);

                // Area di presa invisibile (raycastTarget = true) sopra la traccia.
                RectTransform grabRect = BaseBuilder.GetOrCreateRectChild(track.transform, "IMG_GrabArea");
                var hit = BaseBuilder.GetOrAdd<SettingsHitArea>(grabRect.gameObject);
                hit.raycastTarget = true;
                var grab = BaseBuilder.GetOrAdd<SettingsSliderGrab>(grabRect.gameObject);
                grabRect.SetAsLastSibling();

                var row = BaseBuilder.GetOrAdd<SettingsSliderRow>(root);
                row.ConfigureBase(theme, strings, "audio.master_volume", LABEL_KEY, string.Empty, chrome.Background, chrome.Group, chrome.Label, chrome.Description);
                row.ConfigureSlider(track, fill, handle, grab, value.GetComponent<TMP_Text>(), value, 80f, 0f, 100f, 1f, string.Empty);
                // Hover: la scala la fa la maniglia (32): la riga NON ha UIStateVisual (scalerebbe anche da premuta durante il trascinamento).
                row.Apply();
                return SettingsSaveRoot(root, SETTINGS_SLIDER_PATH, NAME, exists);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                SettingsDiscardRoot(root, exists);
                return $"{NAME}: ERRORE, vedere la Console.";
            }
        }

        // =====================================================================
        // PANEL_SettingsSelector
        // =====================================================================

        /// <summary>
        /// PANEL_SettingsSelector: etichetta, BTN_ArrowLeft, LBL_Value, BTN_ArrowRight. Dati di default: Comfort · Locomozione
        /// (Teletrasporto / Movimento continuo).
        /// </summary>
        private static string SettingsBuildSelector(UITheme theme, UIStrings strings, SettingsSprites sprites)
        {
            const string NAME = "PANEL_SettingsSelector";
            const string LABEL_KEY = "ui.settings.comfort.locomotion";
            const string OPTION_A = "ui.settings.comfort.locomotion_teleport";
            const string OPTION_B = "ui.settings.comfort.locomotion_continuous";
            SettingsCheckKeys(strings, LABEL_KEY, OPTION_A, OPTION_B);
            GameObject root = SettingsOpenRoot(SETTINGS_SELECTOR_PATH, NAME, theme, out bool exists);
            try
            {
                var rect = (RectTransform)root.transform;
                SettingsChrome chrome = SettingsBuildChrome(rect, theme, strings, sprites, false);

                SettingsArrowButton left = SettingsBuildArrow(rect, "BTN_ArrowLeft", "ICN_ChevronLeft", theme, sprites);
                UIStringText value = SettingsBuildText(rect, "LBL_Value", strings, theme, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    TextAlignmentOptions.Midline);
                var valueTmp = value.GetComponent<TextMeshProUGUI>();
                // Testi lunghi ("Movimento continuo") si rimpiccioliscono fino a 14 dentro i 130 del valore.
                valueTmp.enableAutoSizing = true;
                valueTmp.fontSizeMin = 14f;
                valueTmp.fontSizeMax = theme.BodyBold != null ? theme.BodyBold.Size : 18f;
                EditorUtility.SetDirty(valueTmp);
                SettingsArrowButton right = SettingsBuildArrow(rect, "BTN_ArrowRight", "ICN_ChevronRight", theme, sprites);

                var options = new List<SettingsOption>
                {
                    new SettingsOption { Value = 0, LabelKey = OPTION_A },
                    new SettingsOption { Value = 1, LabelKey = OPTION_B },
                };
                var row = BaseBuilder.GetOrAdd<SettingsSelectorRow>(root);
                row.ConfigureBase(theme, strings, "comfort.locomotion", LABEL_KEY, string.Empty, chrome.Background, chrome.Group, chrome.Label, chrome.Description);
                row.ConfigureSelector(left, right, valueTmp, value, 0, options, false);
                // Hover: scala solo sulle frecce (UIStateVisual proprio); la riga non ha UIStateVisual.
                row.Apply();
                return SettingsSaveRoot(root, SETTINGS_SELECTOR_PATH, NAME, exists);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                SettingsDiscardRoot(root, exists);
                return $"{NAME}: ERRORE, vedere la Console.";
            }
        }

        /// <summary>
        /// Crea o aggiorna una freccia del selettore: cerchio tile.neutral (IMG_Fill, bersaglio dei raggi) + icona chevron 24 +
        /// Button senza transizione + UIStateVisual (scala hover/pressed).
        /// </summary>
        private static SettingsArrowButton SettingsBuildArrow(RectTransform parent, string name, string iconName, UITheme theme, SettingsSprites sprites)
        {
            RectTransform arrowRect = BaseBuilder.GetOrCreateRectChild(parent, name);
            arrowRect.sizeDelta = Vector2.one * theme.GetNumber(SettingsTokens.SELECTOR_ARROW);
            RoundedPanelGraphic fill = SettingsBuildGraphic(arrowRect, "IMG_Fill", sprites.Circle, theme.TileNeutral, true);
            BaseBuilder.Stretch(fill.rectTransform);
            fill.transform.SetSiblingIndex(0);
            Image icon = SettingsBuildIcon(arrowRect, iconName, SettingsLoadIcon(iconName), theme.TextPrimary);
            float iconSize = theme.GetNumber(SettingsTokens.ROW_ICON);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = Vector2.zero;
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            icon.transform.SetSiblingIndex(1);

            var group = BaseBuilder.GetOrAdd<CanvasGroup>(arrowRect.gameObject);
            var button = BaseBuilder.GetOrAdd<Button>(arrowRect.gameObject);
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            var arrow = BaseBuilder.GetOrAdd<SettingsArrowButton>(arrowRect.gameObject);
            arrow.Configure(button, group, fill, icon);
            SettingsAddVisual(arrowRect.gameObject, theme, sprites, null, true);
            return arrow;
        }

        // =====================================================================
        // PANEL_SettingsAction
        // =====================================================================

        /// <summary>
        /// PANEL_SettingsAction: etichetta + BTN_Action (IMG_Fill, ICN_Action, LBL_ActionText) e, per il tipo Value, LBL_Value + ICN_Lock.
        /// Dati di default: Generale · Ripristina predefiniti (tipo Button, con conferma).
        /// </summary>
        private static string SettingsBuildAction(UITheme theme, UIStrings strings, SettingsSprites sprites)
        {
            const string NAME = "PANEL_SettingsAction";
            const string LABEL_KEY = "ui.settings.general.reset_defaults";
            const string ACTION_KEY = "ui.settings.general.reset";
            const string VALUE_KEY = "ui.settings.general.language_italian";
            SettingsCheckKeys(strings, LABEL_KEY, ACTION_KEY, VALUE_KEY);
            GameObject root = SettingsOpenRoot(SETTINGS_ACTION_PATH, NAME, theme, out bool exists);
            try
            {
                var rect = (RectTransform)root.transform;
                SettingsChrome chrome = SettingsBuildChrome(rect, theme, strings, sprites, false);

                // Pulsante azione.
                RectTransform buttonRect = BaseBuilder.GetOrCreateRectChild(rect, "BTN_Action");
                buttonRect.sizeDelta = new Vector2(theme.GetNumber(SettingsTokens.ACTION_HEIGHT) * 3f, theme.GetNumber(SettingsTokens.ACTION_HEIGHT));
                RoundedPanelGraphic fill = SettingsBuildGraphic(buttonRect, "IMG_Fill", sprites.Row, theme.TileNeutral, true);
                BaseBuilder.Stretch(fill.rectTransform);
                fill.transform.SetSiblingIndex(0);
                Image icon = SettingsBuildIcon(buttonRect, "ICN_Action", SettingsLoadIcon("ICN_SettingsBackupRestore"), theme.TextPrimary);
                icon.transform.SetSiblingIndex(1);
                UIStringText actionText = SettingsBuildText(buttonRect, "LBL_ActionText", strings, theme, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY,
                    TextAlignmentOptions.MidlineLeft);
                actionText.transform.SetSiblingIndex(2);
                var button = BaseBuilder.GetOrAdd<Button>(buttonRect.gameObject);
                button.targetGraphic = fill;
                button.transition = Selectable.Transition.None;
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                SettingsAddVisual(buttonRect.gameObject, theme, sprites, null, true);

                // Valore di sola lettura + lucchetto (tipo Value; nascosti nel tipo Button).
                UIStringText valueText = SettingsBuildText(rect, "LBL_Value", strings, theme, TextStyleIds.BODY, ColorTokens.TEXT_SECONDARY,
                    TextAlignmentOptions.MidlineRight);
                Image lockIcon = SettingsBuildIcon(rect, "ICN_Lock", SettingsLoadIcon("ICN_Lock"), theme.TextSecondary);

                var row = BaseBuilder.GetOrAdd<SettingsActionRow>(root);
                row.ConfigureBase(theme, strings, string.Empty, LABEL_KEY, string.Empty, chrome.Background, chrome.Group, chrome.Label, chrome.Description);
                row.ConfigureAction(SettingsActionType.Button, "reset_defaults", true, SettingsLoadIcon("ICN_SettingsBackupRestore"), ACTION_KEY, VALUE_KEY,
                    buttonRect.gameObject, button, fill, icon, actionText, valueText, lockIcon);
                // La riga non ha hover proprio (lo ha BTN_Action con il suo UIStateVisual).
                row.Apply();
                return SettingsSaveRoot(root, SETTINGS_ACTION_PATH, NAME, exists);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                SettingsDiscardRoot(root, exists);
                return $"{NAME}: ERRORE, vedere la Console.";
            }
        }
    }
}
