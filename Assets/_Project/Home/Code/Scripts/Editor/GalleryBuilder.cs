using System.Collections.Generic;
using SI81.UI.Sandbox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder della galleria dei componenti di SCN_UISandbox (schermata <see cref="UIScreenId.ComponentGallery"/>, tasto 7):
    /// su PANEL_Menu crea 11 pagine con tutte le istanze dei 20 prefab del Blocco A, ognuna in una cella con la didascalia dello
    /// stato; dati finti e stati sono applicati a runtime da <see cref="UISandboxGallery"/>. Chiamato da BaseBuilder
    /// (BuildPlaceholderScreen). Rieseguibile: celle, istanze e didascalie si trovano per nome, nessun duplicato.
    /// Solo sandbox: i testi delle didascalie (nomi degli stati) sono di debug e non entrano in SO_UIStrings_It.
    /// </summary>
    internal static class GalleryBuilder
    {
        /// <summary>Altezza di PANEL_Menu nella galleria (solo sandbox: più alto della Home per contenere le pagine).</summary>
        internal const float PANEL_HEIGHT = 580f;
        /// <summary>Altezza della fascia di navigazione (pagina + pulsante "Avanti") in fondo al contenuto.</summary>
        private const float NAV_HEIGHT = 52f;
        /// <summary>Altezza della didascalia sotto ogni componente.</summary>
        private const float CAPTION_HEIGHT = 16f;
        /// <summary>Spazio orizzontale tra due celle.</summary>
        private const float CELL_GAP_X = 14f;
        /// <summary>Spazio verticale tra due righe.</summary>
        private const float ROW_GAP_Y = 8f;
        /// <summary>Margine del fondo bianco dietro i componenti per superficie chiara.</summary>
        private const float BACKING_PADDING = 6f;
        /// <summary>Nome della cartella dei prefab dei componenti.</summary>
        private const string COMPONENTS = BaseBuilder.PREFABS_FOLDER + "/Components";

        /// <summary>Descrizione di una cella della galleria.</summary>
        private struct Cell
        {
            /// <summary>Nome del prefab da istanziare.</summary>
            public string Prefab;
            /// <summary>Ruolo della cella nella galleria.</summary>
            public GalleryRole Role;
            /// <summary>Indice della variante del prefab.</summary>
            public int Variant;
            /// <summary>Vero se la cella sta su fondo scuro.</summary>
            public bool Dark;
            /// <summary>Componente degli stati visivi.</summary>
            public UIVisualState State;
            /// <summary>Didascalia di debug (solo sandbox).</summary>
            public string Caption;
        }

        /// <summary>Descrizione di una pagina: titolo e righe di celle.</summary>
        private struct Page
        {
            /// <summary>Titolo.</summary>
            public string Title;
            /// <summary>Righe di celle della pagina.</summary>
            public Cell[][] Rows;
        }

        /// <summary>
        /// Crea una cella della galleria.
        /// </summary>
        private static Cell C(string prefab, GalleryRole role, UIVisualState state, string caption, int variant = 0, bool dark = false)
        {
            return new Cell { Prefab = prefab, Role = role, State = state, Caption = caption, Variant = variant, Dark = dark };
        }

        /// <summary>
        /// Le pagine della galleria (11): tutti i 20 prefab con i loro stati.
        /// </summary>
        private static Page[] BuildPageDefinitions()
        {
            const UIVisualState N = UIVisualState.Normal, H = UIVisualState.Hover, P = UIVisualState.Pressed, S = UIVisualState.Selected, D = UIVisualState.Disabled;
            return new[]
            {
                new Page { Title = "Pulsanti primario, indietro, secondario", Rows = new[]
                {
                    new[] { C("BTN_Primary", GalleryRole.Primary, N, "Primary Normal"), C("BTN_Primary", GalleryRole.Primary, H, "Primary Hover"), C("BTN_Primary", GalleryRole.Primary, P, "Primary Pressed") },
                    new[] { C("BTN_Primary", GalleryRole.Primary, D, "Primary Disabled"), C("BTN_Back", GalleryRole.Back, N, "Back N"), C("BTN_Back", GalleryRole.Back, H, "Back H"), C("BTN_Back", GalleryRole.Back, P, "Back P") },
                    new[] { C("BTN_Secondary", GalleryRole.Secondary, N, "Secondary N"), C("BTN_Secondary", GalleryRole.Secondary, H, "Secondary H"), C("BTN_Secondary", GalleryRole.Secondary, P, "Secondary P"), C("BTN_Secondary", GalleryRole.Secondary, D, "Secondary Dis.") },
                } },
                new Page { Title = "Danger, badge, schede", Rows = new[]
                {
                    new[] { C("BTN_Secondary", GalleryRole.Danger, N, "Danger N"), C("BTN_Secondary", GalleryRole.Danger, H, "Danger H"), C("BTN_Secondary", GalleryRole.Danger, P, "Danger P"), C("BTN_Secondary", GalleryRole.Danger, D, "Danger Dis.") },
                    new[] { C("BTN_Badge", GalleryRole.Badge, N, "Sbloccato N"), C("BTN_Badge", GalleryRole.Badge, H, "Sbloccato H"), C("BTN_Badge", GalleryRole.Badge, P, "Sbloccato P"), C("BTN_Badge", GalleryRole.Badge, N, "Bloccato N", 1), C("BTN_Badge", GalleryRole.Badge, H, "Bloccato H", 1) },
                    new[] { C("BTN_SettingsTab", GalleryRole.SettingsTab, N, "Tab Default"), C("BTN_SettingsTab", GalleryRole.SettingsTab, H, "Tab Hover"), C("BTN_SettingsTab", GalleryRole.SettingsTab, S, "Tab Active") },
                    new[] { C("BTN_SettingsTab", GalleryRole.SettingsTab, D, "Tab Disabled") },
                } },
                new Page { Title = "Righe elenco e dialogo", Rows = new[]
                {
                    new[] { C("BTN_ListRow", GalleryRole.ListRow, N, "ListRow Default") },
                    new[] { C("BTN_ListRow", GalleryRole.ListRow, H, "ListRow Hover") },
                    new[] { C("BTN_ListRow", GalleryRole.ListRow, S, "ListRow Selected") },
                    new[] { C("BTN_ListRow", GalleryRole.ListRow, N, "ListRow Non giocato", 1) },
                } },
                new Page { Title = "Etichette: livello, stelle, dimensioni", Rows = new[]
                {
                    new[] { C("LBL_LevelBadge", GalleryRole.LevelBadge, N, "Livello 1 Dark", 1, true), C("LBL_LevelBadge", GalleryRole.LevelBadge, N, "Livello 2 Dark", 2, true), C("LBL_LevelBadge", GalleryRole.LevelBadge, N, "Livello 3 Dark", 3, true) },
                    new[] { C("LBL_LevelBadge", GalleryRole.LevelBadge, N, "Livello 1 Light", 1), C("LBL_LevelBadge", GalleryRole.LevelBadge, N, "Livello 2 Light", 2), C("LBL_LevelBadge", GalleryRole.LevelBadge, N, "Livello 3 Light", 3) },
                    new[] { C("LBL_StarRating", GalleryRole.StarRating, N, "Stelle 1", 1), C("LBL_StarRating", GalleryRole.StarRating, N, "Stelle 3", 3), C("LBL_StarRating", GalleryRole.StarRating, N, "Stelle 5", 5) },
                    new[] { C("LBL_DimensionBar", GalleryRole.DimensionBar, N, "Dimensione Dark", 1, true), C("LBL_DimensionBar", GalleryRole.DimensionBar, N, "Dimensione Light", 2) },
                } },
                new Page { Title = "Esiti e statistiche", Rows = new[]
                {
                    new[] { C("LBL_Outcome", GalleryRole.Outcome, N, "Esito su card +", 0), C("LBL_Outcome", GalleryRole.Outcome, N, "su card ~", 1), C("LBL_Outcome", GalleryRole.Outcome, N, "su card -", 2) },
                    new[] { C("LBL_Outcome", GalleryRole.Outcome, N, "Esito su vetro +", 0, true), C("LBL_Outcome", GalleryRole.Outcome, N, "su vetro ~", 1, true), C("LBL_Outcome", GalleryRole.Outcome, N, "su vetro -", 2, true) },
                    new[] { C("PANEL_StatTile", GalleryRole.StatTile, N, "Positivi", 0), C("PANEL_StatTile", GalleryRole.StatTile, N, "Parziali", 1), C("PANEL_StatTile", GalleryRole.StatTile, N, "Negativi", 2), C("PANEL_StatTile", GalleryRole.StatTile, N, "Sessioni", 3) },
                } },
                new Page { Title = "Obiettivi", Rows = new[]
                {
                    new[] { C("LBL_ObjectiveItem", GalleryRole.Objective, N, "Obiettivo Done", 0), C("LBL_ObjectiveItem", GalleryRole.Objective, N, "Obiettivo Current", 1) },
                    new[] { C("LBL_ObjectiveItem", GalleryRole.Objective, N, "Obiettivo Todo", 2) },
                } },
                new Page { Title = "Impostazioni: righe attive", Rows = new[]
                {
                    new[] { C("PANEL_SettingsToggle", GalleryRole.SettingsToggle, N, "Toggle (valore dallo store)") },
                    new[] { C("PANEL_SettingsToggle", GalleryRole.SettingsToggle, H, "Toggle Hover") },
                    new[] { C("PANEL_SettingsSlider", GalleryRole.SettingsSlider, N, "Slider") },
                    new[] { C("PANEL_SettingsSelector", GalleryRole.SettingsSelector, N, "Selector") },
                    new[] { C("PANEL_SettingsAction", GalleryRole.SettingsAction, N, "Action (Ripristina: apre il dialogo)") },
                } },
                new Page { Title = "Impostazioni: valore e disabilitate", Rows = new[]
                {
                    new[] { C("PANEL_SettingsAction", GalleryRole.SettingsAction, N, "Action Value", 1) },
                    new[] { C("PANEL_SettingsToggle", GalleryRole.SettingsToggle, D, "Toggle Disabled") },
                    new[] { C("PANEL_SettingsSlider", GalleryRole.SettingsSlider, D, "Slider Disabled") },
                    new[] { C("PANEL_SettingsSelector", GalleryRole.SettingsSelector, D, "Selector Disabled") },
                } },
                new Page { Title = "Card scenario e training", Rows = new[]
                {
                    new[] { C("BTN_ScenarioCard", GalleryRole.ScenarioCard, N, "Scenario N"), C("BTN_ScenarioCard", GalleryRole.ScenarioCard, N, "Scenario Nuovo", 1), C("BTN_ScenarioCard", GalleryRole.ScenarioCard, H, "Scenario Hover"), C("BTN_ScenarioCard", GalleryRole.ScenarioCard, P, "Scenario Pressed") },
                    new[] { C("PANEL_TrainingCard", GalleryRole.TrainingCard, N, "Training N") },
                    new[] { C("PANEL_TrainingCard", GalleryRole.TrainingCard, H, "Training Hover") },
                } },
                new Page { Title = "Card livello", Rows = new[]
                {
                    new[] { C("BTN_DifficultyCard", GalleryRole.DifficultyCard, N, "Guided N", 1), C("BTN_DifficultyCard", GalleryRole.DifficultyCard, H, "Operational Hover", 2), C("BTN_DifficultyCard", GalleryRole.DifficultyCard, P, "Crisis Pressed", 3) },
                    new[] { C("BTN_DifficultyCard", GalleryRole.DifficultyCard, S, "Guided Selected", 1), C("BTN_DifficultyCard", GalleryRole.DifficultyCard, S, "Operational Selected", 2), C("BTN_DifficultyCard", GalleryRole.DifficultyCard, D, "Crisis Locked", 3) },
                } },
                new Page { Title = "Card: non disponibile e pressed", Rows = new[]
                {
                    new[] { C("BTN_ScenarioCard", GalleryRole.ScenarioCard, D, "Scenario Disabled") },
                    new[] { C("PANEL_TrainingCard", GalleryRole.TrainingCard, P, "Training Pressed") },
                } },
            };
        }

        /// <summary>
        /// Costruisce la galleria dentro PANEL_Menu della schermata: pagine, celle, navigazione, dialogo e componente runtime.
        /// </summary>
        /// <param name="screen">Radice della schermata (UIScreen).</param>
        /// <param name="panel">Istanza di PANEL_Menu della schermata.</param>
        /// <param name="theme">Tema.</param>
        /// <param name="strings">Tabella testi.</param>
        internal static void Populate(RectTransform screen, MenuPanel panel, UITheme theme, UIStrings strings)
        {
            panel.Apply();
            RectTransform content = panel.ContentRoot;
            if (content == null)
            {
                Debug.LogError("[SI81 UI] Galleria: PANEL_Menu senza ContentRoot.");
                return;
            }
            Vector2 area = content.rect.size;
            if (area.x < 100f || area.y < 100f) area = new Vector2(712f, PANEL_HEIGHT - 150f);
            float pageHeight = area.y - NAV_HEIGHT;

            Page[] definitions = BuildPageDefinitions();
            var pages = new GameObject[definitions.Length];
            var titles = new string[definitions.Length];
            var items = new List<GalleryItem>();
            Sprite card = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBuilder.SPRITES_FOLDER + "/TEX_CardRounded.png");

            for (int p = 0; p < definitions.Length; p++)
            {
                RectTransform page = BaseBuilder.GetOrCreateRectChild(content, "PANEL_GalleryPage" + p);
                BaseBuilder.Stretch(page);
                page.offsetMin = new Vector2(0f, NAV_HEIGHT);
                page.gameObject.SetActive(p == 0);
                pages[p] = page.gameObject;
                titles[p] = definitions[p].Title;
                float used = BuildPage(page, p, definitions[p], theme, card, items);
                if (used > pageHeight)
                    Debug.LogWarning($"[SI81 UI] Galleria: la pagina {p + 1} ({definitions[p].Title}) è alta {used:0} su {pageHeight:0} disponibili: spostare celle in un'altra pagina.");
            }

            // Navigazione in fondo al contenuto: testo di pagina a sinistra, BTN_Secondary "Avanti" a destra.
            RectTransform nav = BaseBuilder.GetOrCreateRectChild(content, "PANEL_GalleryNav");
            nav.anchorMin = new Vector2(0f, 0f);
            nav.anchorMax = new Vector2(1f, 0f);
            nav.pivot = new Vector2(0.5f, 0f);
            nav.anchoredPosition = Vector2.zero;
            nav.sizeDelta = new Vector2(0f, NAV_HEIGHT);
            nav.localScale = Vector3.one;
            TextMeshProUGUI pageText = AddCaption(nav, "LBL_GalleryPage", theme, "Pagina", 16f);
            var pageRect = (RectTransform)pageText.transform;
            pageRect.anchorMin = new Vector2(0f, 0f);
            pageRect.anchorMax = new Vector2(1f, 1f);
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = new Vector2(-190f, 0f);
            pageText.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject next = BaseBuilder.GetOrCreateChildPrefabInstance(nav, COMPONENTS + "/BTN_Secondary.prefab", "BTN_Secondary");
            var nextRect = (RectTransform)next.transform;
            nextRect.anchorMin = nextRect.anchorMax = new Vector2(1f, 0.5f);
            nextRect.pivot = new Vector2(1f, 0.5f);
            nextRect.anchoredPosition = new Vector2(-4f, 0f);
            UIButton nextButton = next.GetComponent<UIButton>();
            nextButton.SetLabelKey("ui.button.next");
            BaseBuilder.RecordInstanceOverrides(next);

            // Apri dialogo: BTN_Secondary nella pagina "Righe elenco e dialogo" (indice 2), a destra delle righe (che sono larghe 440).
            RectTransform openCell = BaseBuilder.GetOrCreateRectChild(pages[2].transform, "PANEL_GalleryOpenDialog");
            openCell.anchorMin = openCell.anchorMax = openCell.pivot = new Vector2(0f, 1f);
            openCell.sizeDelta = new Vector2(180f, 52f);
            openCell.anchoredPosition = new Vector2(480f, -8f);
            GameObject open = BaseBuilder.GetOrCreateChildPrefabInstance(openCell, COMPONENTS + "/BTN_Secondary.prefab", "BTN_Secondary");
            var openRect = (RectTransform)open.transform;
            openRect.anchorMin = openRect.anchorMax = new Vector2(0.5f, 0.5f);
            openRect.pivot = new Vector2(0.5f, 0.5f);
            openRect.anchoredPosition = Vector2.zero;
            UIButton openButton = open.GetComponent<UIButton>();
            openButton.SetLabelKey("ui.button.restart");
            BaseBuilder.RecordInstanceOverrides(open);

            // PANEL_ConfirmDialog: figlio della schermata (sopra il pannello), salvato disattivo nel prefab.
            GameObject dialogGo = BaseBuilder.GetOrCreateChildPrefabInstance(screen, COMPONENTS + "/PANEL_ConfirmDialog.prefab", "PANEL_ConfirmDialog");
            dialogGo.transform.SetAsLastSibling();
            dialogGo.SetActive(false);

            var gallery = BaseBuilder.GetOrAdd<UISandboxGallery>(screen.gameObject);
            gallery.Configure(pages, titles, pageText, nextButton, items, dialogGo.GetComponent<ConfirmDialog>(), openButton,
                new[]
                {
                    ComponentsBuilder.LoadIcon("ICN_WorkspacePremium_Filled"), ComponentsBuilder.LoadIcon("ICN_School"), ComponentsBuilder.LoadIcon("ICN_LocalFireDepartment_Filled"), ComponentsBuilder.LoadIcon("ICN_Settings_Filled")
                },
                new[] { ComponentsBuilder.LoadIcon("ICN_Tune"), ComponentsBuilder.LoadIcon("ICN_Tune_Filled"), ComponentsBuilder.LoadIcon("ICN_SettingsBackupRestore") });
            EditorUtility.SetDirty(gallery);
        }

        /// <summary>
        /// Costruisce le celle di una pagina e registra le voci.
        /// </summary>
        /// <returns>Altezza usata dalla pagina.</returns>
        private static float BuildPage(RectTransform page, int pageIndex, Page definition, UITheme theme, Sprite card, List<GalleryItem> items)
        {
            // Migrazione v0.3.1: le celle con il vecchio nome (PANEL_GalleryCell0_1, con underscore) vengono eliminate e ricreate.
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                Transform old = page.GetChild(i);
                if (System.Text.RegularExpressions.Regex.IsMatch(old.name, @"^PANEL_GalleryCell\d+_\d+$")) Object.DestroyImmediate(old.gameObject);
            }
            float y = 0f;
            int cellIndex = 0;
            foreach (Cell[] row in definition.Rows)
            {
                float x = 0f;
                float rowHeight = 0f;
                foreach (Cell cell in row)
                {
                    string prefabPath = COMPONENTS + "/" + cell.Prefab + ".prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        Debug.LogError($"[SI81 UI] Galleria: prefab mancante {prefabPath}. Eseguire SI81/UI/Build Components.");
                        continue;
                    }
                    Vector2 size = ((RectTransform)prefab.transform).sizeDelta;
                    float width = Mathf.Max(size.x, 70f);
                    float height = size.y + CAPTION_HEIGHT + 4f;
                    // Il testo "Livello N" è in Body Bold: le etichette si allargano un po' per le didascalie lunghe.
                    width = Mathf.Max(width, 90f);

                    RectTransform cellRect = BaseBuilder.GetOrCreateRectChild(page, $"PANEL_GalleryCellP{pageIndex}N{cellIndex++}");
                    cellRect.anchorMin = cellRect.anchorMax = cellRect.pivot = new Vector2(0f, 1f);
                    cellRect.sizeDelta = new Vector2(width, height);
                    cellRect.anchoredPosition = new Vector2(x, -y);
                    cellRect.localScale = Vector3.one;

                    // Fondo bianco per le etichette su superficie chiara (le altre stanno sul vetro scuro del pannello).
                    bool light = !cell.Dark && (cell.Role == GalleryRole.LevelBadge || cell.Role == GalleryRole.StarRating
                        || cell.Role == GalleryRole.DimensionBar || cell.Role == GalleryRole.Outcome);
                    Image backing = ComponentsBuilder.AddImage(cellRect, "IMG_GalleryBacking", card, theme.GetColor(ColorTokens.SURFACE_CARD), false);
                    backing.gameObject.SetActive(light);
                    var backingRect = (RectTransform)backing.transform;
                    backingRect.anchorMin = backingRect.anchorMax = backingRect.pivot = new Vector2(0.5f, 1f);
                    backingRect.sizeDelta = new Vector2(width + BACKING_PADDING, size.y + BACKING_PADDING);
                    backingRect.anchoredPosition = new Vector2(0f, BACKING_PADDING * 0.5f);
                    backingRect.SetSiblingIndex(0);

                    GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(cellRect, prefabPath, cell.Prefab);
                    var rect = (RectTransform)instance.transform;
                    // Centro dell'istanza = centro della zona alta della cella, qualunque sia il pivot del prefab.
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(-(0.5f - rect.pivot.x) * size.x, -size.y * 0.5f - (0.5f - rect.pivot.y) * size.y);
                    rect.localScale = Vector3.one;
                    instance.SetActive(true);
                    BaseBuilder.RecordInstanceOverrides(instance);

                    TextMeshProUGUI caption = AddCaption(cellRect, "LBL_GalleryCaption", theme, cell.Caption, 12f);
                    var captionRect = (RectTransform)caption.transform;
                    captionRect.anchorMin = new Vector2(0f, 0f);
                    captionRect.anchorMax = new Vector2(1f, 0f);
                    captionRect.pivot = new Vector2(0.5f, 0f);
                    captionRect.sizeDelta = new Vector2(CELL_GAP_X, CAPTION_HEIGHT);
                    captionRect.anchoredPosition = Vector2.zero;
                    caption.alignment = TextAlignmentOptions.Center;

                    items.Add(new GalleryItem { Target = instance, Role = cell.Role, Variant = cell.Variant, Dark = cell.Dark, State = cell.State, Page = pageIndex });
                    x += width + CELL_GAP_X;
                    rowHeight = Mathf.Max(rowHeight, height);
                }
                y += rowHeight + ROW_GAP_Y;
            }
            return y;
        }

        /// <summary>
        /// Crea o aggiorna una didascalia di debug (TMP semplice, stile Body del tema, colore text.on-green).
        /// </summary>
        private static TextMeshProUGUI AddCaption(Transform parent, string name, UITheme theme, string text, float fontSize)
        {
            RectTransform rect = BaseBuilder.GetOrCreateRectChild(parent, name);
            var tmp = BaseBuilder.GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            UITheme.TextStyle style = theme.GetTextStyle(TextStyleIds.BODY);
            if (style != null) style.ApplyTo(tmp);
            tmp.fontSize = fontSize;
            tmp.color = theme.GetColor(ColorTokens.TEXT_ON_GREEN);
            tmp.text = text;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            return tmp;
        }
    }
}
