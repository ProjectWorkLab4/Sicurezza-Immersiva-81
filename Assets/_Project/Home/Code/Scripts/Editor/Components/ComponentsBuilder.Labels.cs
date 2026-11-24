using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "etichette" del builder dei componenti (Blocco A): genera o aggiorna i prefab LBL_LevelBadge, LBL_StarRating,
    /// LBL_DimensionBar, PANEL_StatTile, LBL_Outcome e LBL_ObjectiveItem in Assets/_Project/Home/UI/Prefabs/Components/.
    /// Sicuro da rieseguire: carica il prefab esistente, trova i figli per nome, non duplica nulla e mantiene i GUID.
    /// Le etichette non sono interattive: nessun raycastTarget e nessun UIStateVisual. Richiede SI81/UI/Build Theme.
    /// </summary>
    internal static partial class ComponentsBuilder
    {
        /// <summary>Cartella dei prefab dei componenti (condivisa con le altre famiglie del Blocco A).</summary>
        private const string LABELS_PREFAB_FOLDER = BaseBuilder.PREFABS_FOLDER + "/Components";
        /// <summary>Sprite arrotondato con raggio radius.card (sfondo di PANEL_StatTile).</summary>
        private const string LABELS_CARD_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_CardRounded.png";
        /// <summary>Sprite arrotondato con raggio radius.button (sfondo di LBL_ObjectiveItem).</summary>
        private const string LABELS_BUTTON_SPRITE = SpriteBuilder.SPRITES_FOLDER + "/TEX_ButtonRounded.png";

        /// <summary>Dati comuni a tutti i prefab delle etichette.</summary>
        private struct LabelsContext
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
            /// <summary>Componente Button del prefab.</summary>
            public Sprite Button;
        }

        /// <summary>
        /// Crea o aggiorna i 6 prefab delle etichette. Chiamato da <c>BuildAll</c> di ComponentsBuilder.
        /// </summary>
        private static void BuildLabels()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultyStyle>(ThemeBuilder.DIFFICULTY_ASSET);
            if (theme == null || strings == null || difficulty == null)
            {
                Debug.LogError("[SI81 UI] Etichette: interrotto, SO_UITheme / SO_UIStrings_It / SO_DifficultyStyle mancanti. Eseguire SI81/UI/Build Theme.");
                return;
            }
            BaseBuilder.EnsureFolder(LABELS_PREFAB_FOLDER);

            // Sprite 9-slice: la pillola esiste già; card e button con il raggio del tema (rieseguibile, "invariato" se uguale).
            SpriteBuilder.BuildRoundedSprite(LABELS_CARD_SPRITE, theme.RadiusCard);
            SpriteBuilder.BuildRoundedSprite(LABELS_BUTTON_SPRITE, theme.RadiusButton);
            var context = new LabelsContext
            {
                Theme = theme,
                Strings = strings,
                Difficulty = difficulty,
                Pill = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.PILL_ROUNDED),
                Card = AssetDatabase.LoadAssetAtPath<Sprite>(LABELS_CARD_SPRITE),
                Button = AssetDatabase.LoadAssetAtPath<Sprite>(LABELS_BUTTON_SPRITE)
            };
            if (context.Pill == null || context.Card == null || context.Button == null)
            {
                Debug.LogError("[SI81 UI] Etichette: interrotto, sprite TEX_PillRounded / TEX_CardRounded / TEX_ButtonRounded mancanti.");
                return;
            }

            LabelsBuildPrefab("LBL_LevelBadge", new Vector2(146f, 23f), context, LabelsBuildLevelBadge);
            LabelsBuildPrefab("LBL_StarRating", new Vector2(153f, 24f), context, LabelsBuildStarRating);
            LabelsBuildPrefab("LBL_DimensionBar", new Vector2(272f, 23f), context, LabelsBuildDimensionBar);
            LabelsBuildPrefab("PANEL_StatTile", new Vector2(112f, 96f), context, LabelsBuildStatTile);
            LabelsBuildPrefab("LBL_Outcome", new Vector2(146f, 32f), context, LabelsBuildOutcome);
            LabelsBuildPrefab("LBL_ObjectiveItem", new Vector2(260f, 36f), context, LabelsBuildObjectiveItem);
            Debug.Log("[SI81 UI] Etichette: 6 prefab creati/aggiornati in " + LABELS_PREFAB_FOLDER + ".");
        }

        // =====================================================================
        // Scheletro del prefab
        // =====================================================================

        /// <summary>
        /// Carica (o crea) il prefab, imposta nome, layer e dimensione della radice, chiama il costruttore del contenuto e salva.
        /// </summary>
        /// <param name="name">Nome del prefab = nome della radice.</param>
        /// <param name="size">Dimensione iniziale della radice (unità Canvas, da Figma).</param>
        /// <param name="context">Tema, testi e sprite.</param>
        /// <param name="buildContent">Costruisce i figli e configura il componente; il secondo parametro è true alla prima creazione.</param>
        private static void LabelsBuildPrefab(string name, Vector2 size, LabelsContext context, Action<GameObject, LabelsContext, bool> buildContent)
        {
            string path = LABELS_PREFAB_FOLDER + "/" + name + ".prefab";
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name, typeof(RectTransform));
            if (exists) RemoveMissingScripts(root);
            try
            {
                root.name = name;
                root.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = size;
                buildContent(root, context, !exists);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success) Debug.LogError($"[SI81 UI] {name}: ERRORE nel salvataggio del prefab.");
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // =====================================================================
        // Helper per i figli
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna un testo TMP decorativo (nessun raycast, senza stile: lo applica il componente dal tema).
        /// </summary>
        private static TextMeshProUGUI LabelsText(Transform parent, string name, TextAlignmentOptions alignment, bool wrap)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var tmp = BaseBuilder.GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            tmp.alignment = alignment;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        /// <summary>
        /// Crea o aggiorna un'icona (Image decorativa, sprite assegnato dal componente).
        /// </summary>
        private static Image LabelsImage(Transform parent, string name)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var image = BaseBuilder.GetOrAdd<Image>(rect.gameObject);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Crea o aggiorna una forma arrotondata a sprite 9-slice (decorativa, senza riflesso).
        /// </summary>
        private static RoundedPanelGraphic LabelsRounded(Transform parent, string name, Sprite sprite)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var graphic = BaseBuilder.GetOrAdd<RoundedPanelGraphic>(rect.gameObject);
            graphic.Sprite = sprite;
            graphic.DisableSheen();
            graphic.raycastTarget = false;
            return graphic;
        }

        /// <summary>
        /// Crea o aggiorna una forma a mesh (barra livello, contorno pista), decorativa.
        /// </summary>
        private static RoundedRectGraphic LabelsShape(Transform parent, string name)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var graphic = BaseBuilder.GetOrAdd<RoundedRectGraphic>(rect.gameObject);
            graphic.raycastTarget = false;
            return graphic;
        }

        /// <summary>
        /// Carica lo sprite di un'icona ICN_ per nome; scrive un errore se manca.
        /// </summary>
        private static Sprite LabelsIcon(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconBuilder.ICONS_FOLDER + "/" + name + ".png");
            if (sprite == null) Debug.LogError($"[SI81 UI] Icona {name} assente. Eseguire SI81/UI/Build Theme.");
            return sprite;
        }

        // =====================================================================
        // Prefab
        // =====================================================================

        /// <summary>
        /// LBL_LevelBadge: IMG_LevelBars (3 barre a mesh) + LBL_Level (Bold) + LBL_Name (Regular).
        /// </summary>
        private static void LabelsBuildLevelBadge(GameObject root, LabelsContext context, bool isNew)
        {
            RectTransform barsRoot = BaseBuilder.GetOrCreateRectChild(root.transform, "IMG_LevelBars");
            LabelStyling.Place(barsRoot, 0f, 0f, LevelBadge.ICON_SIZE, LevelBadge.ICON_SIZE);
            var bars = new RoundedRectGraphic[DifficultyStyle.BAR_COUNT];
            for (int i = 0; i < bars.Length; i++) bars[i] = LabelsShape(barsRoot, "IMG_Bar" + (i + 1));
            TextMeshProUGUI level = LabelsText(root.transform, "LBL_Level", TextAlignmentOptions.MidlineLeft, false);
            TextMeshProUGUI levelName = LabelsText(root.transform, "LBL_Name", TextAlignmentOptions.MidlineLeft, false);

            LevelBadge badge = BaseBuilder.GetOrAdd<LevelBadge>(root);
            badge.Configure(context.Theme, context.Strings, context.Difficulty, bars, level, levelName);
            if (isNew) badge.SetLevel(DifficultyLevel.Guided);
            EditorUtility.SetDirty(badge);
        }

        /// <summary>
        /// LBL_StarRating: 5 IMG_Star + LBL_Value.
        /// </summary>
        private static void LabelsBuildStarRating(GameObject root, LabelsContext context, bool isNew)
        {
            var stars = new Image[StarRating.MAX_STARS];
            for (int i = 0; i < stars.Length; i++) stars[i] = LabelsImage(root.transform, "ICN_Star" + (i + 1));
            TextMeshProUGUI value = LabelsText(root.transform, "LBL_Value", TextAlignmentOptions.MidlineLeft, false);

            StarRating rating = BaseBuilder.GetOrAdd<StarRating>(root);
            rating.Configure(context.Theme, context.Strings, stars, LabelsIcon("ICN_Star"), LabelsIcon("ICN_Star_Filled"), value);
            if (isNew) rating.SetStars(4);
            EditorUtility.SetDirty(rating);
        }

        /// <summary>
        /// LBL_DimensionBar: LBL_Name + IMG_Track (IMG_TrackStroke contorno + IMG_Fill riempimento) + LBL_Value.
        /// </summary>
        private static void LabelsBuildDimensionBar(GameObject root, LabelsContext context, bool isNew)
        {
            TextMeshProUGUI nameText = LabelsText(root.transform, "LBL_Name", TextAlignmentOptions.MidlineLeft, false);
            RectTransform track = BaseBuilder.GetOrCreateRectChild(root.transform, "IMG_Track");
            RoundedRectGraphic stroke = LabelsShape(track, "IMG_TrackStroke");
            RoundedPanelGraphic fill = LabelsRounded(track, "IMG_Fill", context.Pill);
            stroke.transform.SetSiblingIndex(0);
            fill.transform.SetSiblingIndex(1);
            TextMeshProUGUI value = LabelsText(root.transform, "LBL_Value", TextAlignmentOptions.MidlineRight, false);

            DimensionBar bar = BaseBuilder.GetOrAdd<DimensionBar>(root);
            bar.Configure(context.Theme, context.Strings, nameText, track, stroke, fill, value);
            if (isNew) bar.SetDimension(DimensionKind.Awareness, 86);
            EditorUtility.SetDirty(bar);
        }

        /// <summary>
        /// PANEL_StatTile: IMG_Background + ICN_Stat + LBL_Label + LBL_Value.
        /// </summary>
        private static void LabelsBuildStatTile(GameObject root, LabelsContext context, bool isNew)
        {
            RoundedPanelGraphic background = LabelsRounded(root.transform, "IMG_Background", context.Card);
            background.transform.SetSiblingIndex(0);
            Image icon = LabelsImage(root.transform, "ICN_Stat");
            TextMeshProUGUI label = LabelsText(root.transform, "LBL_Label", TextAlignmentOptions.MidlineLeft, false);
            TextMeshProUGUI value = LabelsText(root.transform, "LBL_Value", TextAlignmentOptions.MidlineLeft, false);

            // Ordine di StatAccent: Positive, Partial, Negative, Neutral.
            Sprite[] icons = { LabelsIcon("ICN_CheckCircle"), LabelsIcon("ICN_Contrast"), LabelsIcon("ICN_Cancel"), LabelsIcon("ICN_History") };
            StatTile tile = BaseBuilder.GetOrAdd<StatTile>(root);
            tile.Configure(context.Theme, context.Strings, background, icon, label, value, icons);
            if (isNew) tile.SetStat(StatAccent.Positive, 5);
            EditorUtility.SetDirty(tile);
        }

        /// <summary>
        /// LBL_Outcome: IMG_Pill + ICN_Outcome + LBL_Text.
        /// </summary>
        private static void LabelsBuildOutcome(GameObject root, LabelsContext context, bool isNew)
        {
            RoundedPanelGraphic pill = LabelsRounded(root.transform, "IMG_Pill", context.Pill);
            pill.transform.SetSiblingIndex(0);
            Image icon = LabelsImage(root.transform, "ICN_Outcome");
            TextMeshProUGUI text = LabelsText(root.transform, "LBL_Text", TextAlignmentOptions.MidlineLeft, false);

            // Ordine di OutcomeType: Positive, Partial, Negative.
            Sprite[] icons = { LabelsIcon("ICN_CheckCircle"), LabelsIcon("ICN_Contrast"), LabelsIcon("ICN_Cancel") };
            OutcomeLabel label = BaseBuilder.GetOrAdd<OutcomeLabel>(root);
            label.Configure(context.Theme, context.Strings, pill, icon, text, icons);
            if (isNew) label.SetOutcome(OutcomeType.Positive);
            EditorUtility.SetDirty(label);
        }

        /// <summary>
        /// LBL_ObjectiveItem: IMG_Background (solo obiettivo corrente) + ICN_State + LBL_Text (a capo).
        /// </summary>
        private static void LabelsBuildObjectiveItem(GameObject root, LabelsContext context, bool isNew)
        {
            RoundedPanelGraphic background = LabelsRounded(root.transform, "IMG_Background", context.Button);
            background.transform.SetSiblingIndex(0);
            Image icon = LabelsImage(root.transform, "ICN_State");
            TextMeshProUGUI text = LabelsText(root.transform, "LBL_Text", TextAlignmentOptions.TopLeft, true);

            // Ordine di ObjectiveState: Done, Current, Todo.
            Sprite[] icons = { LabelsIcon("ICN_CheckCircle_Filled"), LabelsIcon("ICN_RadioButtonChecked"), LabelsIcon("ICN_RadioButtonUnchecked") };
            ObjectiveItem item = BaseBuilder.GetOrAdd<ObjectiveItem>(root);
            item.Configure(context.Theme, context.Strings, background, icon, text, icons);
            if (isNew) item.SetState(ObjectiveState.Todo);
            EditorUtility.SetDirty(item);
        }
    }
}
