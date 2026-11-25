using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "Impostazioni" del builder delle schermate (Blocco B): crea o aggiorna il prefab PANEL_Settings in Prefabs/Screens/.
    /// Struttura: UIScreen (id Settings) + SettingsScreen; PANEL_Menu (vetro, titolo ui.settings.title); sotto ContentRoot
    /// PANEL_Tabs (6 × BTN_SettingsTab 180×48, gap 4) e PANEL_Rows con UN gruppo di righe per scheda (PANEL_RowsComfort…), ogni riga
    /// è un'istanza di PANEL_SettingsToggle/Slider/Selector/Action configurata dal <see cref="SettingsCatalog"/>; PANEL_ConfirmDialog
    /// (istanza disattiva) per "Ripristina impostazioni". Sicuro da rieseguire: istanze cercate per nome, stessi GUID.
    /// </summary>
    internal static partial class ScreensBuilder
    {
        /// <summary>Percorso del prefab PANEL_Settings.</summary>
        private const string SETTINGS_SCREEN_PREFAB = BaseBuilder.PREFABS_FOLDER + "/Screens/PANEL_Settings.prefab";
        /// <summary>Nome della radice (nome in scena = nome dell'asset).</summary>
        private const string SETTINGS_SCREEN_NAME = "PANEL_Settings";

        /// <summary>
        /// Crea o aggiorna PANEL_Settings e scrive il riepilogo in Console.
        /// </summary>
        private static void BuildSettings()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] PANEL_Settings: interrotto, SO_UITheme o SO_UIStrings_It mancanti (eseguire SI81/UI/Build Theme).");
                return;
            }
            string[] required =
            {
                MenuPanelBuilder.PREFAB_PATH, ComponentsBuilder.PrefabPath("BTN_SettingsTab"), ComponentsBuilder.PrefabPath("PANEL_ConfirmDialog"),
                ComponentsBuilder.PrefabPath("PANEL_SettingsToggle"), ComponentsBuilder.PrefabPath("PANEL_SettingsSlider"),
                ComponentsBuilder.PrefabPath("PANEL_SettingsSelector"), ComponentsBuilder.PrefabPath("PANEL_SettingsAction"),
            };
            foreach (string path in required)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
                Debug.LogError($"[SI81 UI] PANEL_Settings: interrotto, prefab mancante {path} (eseguire SI81/UI/Build Menu Panel e Build Components).");
                return;
            }
            if (!SettingsCheckCatalog(strings)) return;

            BaseBuilder.EnsureFolder(BaseBuilder.PREFABS_FOLDER + "/Screens");
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(SETTINGS_SCREEN_PREFAB) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(SETTINGS_SCREEN_PREFAB) : new GameObject(SETTINGS_SCREEN_NAME, typeof(RectTransform));
            try
            {
                root.name = SETTINGS_SCREEN_NAME;
                root.layer = LayerMask.NameToLayer("UI");
                var rootRect = (RectTransform)root.transform;
                BaseBuilder.Stretch(rootRect);
                var screen = BaseBuilder.GetOrAdd<UIScreen>(root);
                screen.Configure(UIScreenId.Settings, theme);

                // Pannello vetro con titolo "Impostazioni" (nessun indietro: schermata di primo livello, altezza 475).
                GameObject menuGo = BaseBuilder.GetOrCreateChildPrefabInstance(rootRect, MenuPanelBuilder.PREFAB_PATH, MenuPanelBuilder.ROOT_NAME);
                menuGo.transform.SetSiblingIndex(0);
                var menu = menuGo.GetComponent<MenuPanel>();
                MenuPanelBuilder.ConfigureInstance(menu, "ui.settings.title", string.Empty, false, theme.GetNumber(NumberTokens.MENU_PANEL_HEIGHT));

                RectTransform content = menu.ContentRoot;
                float tabWidth = theme.GetNumber(ButtonTokens.TAB_WIDTH);
                float tabHeight = theme.GetNumber(ButtonTokens.TAB_HEIGHT);
                float stackGap = theme.GetNumber(NumberTokens.SPACE_XS);
                float columnGap = theme.GetNumber(NumberTokens.SPACE_L);
                float rowWidth = theme.GetNumber(SettingsTokens.ROW_WIDTH);
                float rowHeight = theme.GetNumber(SettingsTokens.ROW_HEIGHT);
                IReadOnlyList<SettingsTabDefinition> tabDefinitions = SettingsCatalog.Tabs;

                // Colonna schede.
                RectTransform tabsRect = BaseBuilder.GetOrCreateRectChild(content, "PANEL_Tabs");
                PlaceTopLeft(tabsRect, 0f, 0f, tabWidth, tabDefinitions.Count * tabHeight + (tabDefinitions.Count - 1) * stackGap);
                // Colonna righe.
                RectTransform rowsRect = BaseBuilder.GetOrCreateRectChild(content, "PANEL_Rows");
                PlaceTopLeft(rowsRect, tabWidth + columnGap, 0f, rowWidth, SettingsMaxStackHeight(tabDefinitions, rowHeight, stackGap));

                var tabs = new SettingsTabButton[tabDefinitions.Count];
                var groups = new RectTransform[tabDefinitions.Count];
                var report = new StringBuilder();
                for (int i = 0; i < tabDefinitions.Count; i++)
                {
                    SettingsTabDefinition tab = tabDefinitions[i];
                    tabs[i] = SettingsBuildTab(tabsRect, tab, i, tabWidth, tabHeight, stackGap);
                    groups[i] = SettingsBuildRowGroup(rowsRect, tab, i == 0, rowWidth, rowHeight, stackGap, report);
                }

                // Dialogo di conferma (istanza disattiva, centrata sullo schermo).
                GameObject dialogGo = BaseBuilder.GetOrCreateChildPrefabInstance(rootRect, ComponentsBuilder.PrefabPath("PANEL_ConfirmDialog"), "PANEL_ConfirmDialog");
                var dialogRect = (RectTransform)dialogGo.transform;
                dialogRect.anchorMin = dialogRect.anchorMax = dialogRect.pivot = new Vector2(0.5f, 0.5f);
                dialogRect.anchoredPosition = Vector2.zero;
                dialogRect.SetAsLastSibling();
                BaseBuilder.RecordInstanceOverrides(dialogGo);

                var settingsScreen = BaseBuilder.GetOrAdd<SettingsScreen>(root);
                settingsScreen.Configure(tabs, groups, dialogGo.GetComponent<ConfirmDialog>(), ComponentsBuilder.LoadIcon("ICN_SettingsBackupRestore"));
                EditorUtility.SetDirty(settingsScreen);

                // Le schermate partono nascoste: le mostra UIScreenManager.
                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, SETTINGS_SCREEN_PREFAB, out bool success);
                Debug.Log(success
                    ? $"[SI81 UI] PANEL_Settings: {(exists ? "aggiornato" : "creato")} ({SETTINGS_SCREEN_PREFAB}).{report}"
                    : "[SI81 UI] PANEL_Settings: ERRORE nel salvataggio del prefab.");
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
        /// Controlla il catalogo: massimo 7 righe per scheda, nessuna riga vietata (CLAUDE.md §8), chiavi presenti nei testi.
        /// </summary>
        /// <returns>true se il catalogo è valido.</returns>
        private static bool SettingsCheckCatalog(UIStrings strings)
        {
            bool valid = true;
            foreach (SettingsTabDefinition tab in SettingsCatalog.Tabs)
            {
                if (tab.Rows.Length > SettingsCatalog.MAX_ROWS_PER_TAB)
                {
                    Debug.LogError($"[SI81 UI] PANEL_Settings: la scheda {tab.Id} ha {tab.Rows.Length} righe (massimo {SettingsCatalog.MAX_ROWS_PER_TAB}).");
                    valid = false;
                }
                var keys = new List<string> { tab.LabelKey };
                foreach (SettingsRowDefinition row in tab.Rows)
                {
                    keys.Add(row.LabelKey);
                    keys.Add(row.ActionTextKey);
                    keys.Add(row.ValueKey);
                    foreach (SettingsOption option in row.Options)
                    {
                        keys.Add(option.LabelKey);
                        keys.Add(option.FormatKey);
                    }
                    if (!string.IsNullOrEmpty(row.FormatKey)) keys.Add(row.FormatKey);
                }
                keys.Add("ui.dialog.reset_defaults.title");
                keys.Add("ui.dialog.reset_defaults.body");
                foreach (string key in keys)
                {
                    if (string.IsNullOrEmpty(key) || strings.Contains(key)) continue;
                    Debug.LogError($"[SI81 UI] PANEL_Settings: chiave \"{key}\" assente in SO_UIStrings_It (scheda {tab.Id}).");
                    valid = false;
                }
            }
            return valid;
        }

        /// <summary>
        /// Altezza dello stack di righe più alto tra le schede (n × 48 + (n − 1) × 4).
        /// </summary>
        private static float SettingsMaxStackHeight(IReadOnlyList<SettingsTabDefinition> tabs, float rowHeight, float gap)
        {
            int max = 0;
            foreach (SettingsTabDefinition tab in tabs) max = Mathf.Max(max, tab.Rows.Length);
            return max * rowHeight + Mathf.Max(0, max - 1) * gap;
        }

        /// <summary>
        /// Crea o aggiorna una scheda (istanza di BTN_SettingsTab) con icone e chiave dal catalogo.
        /// </summary>
        private static SettingsTabButton SettingsBuildTab(RectTransform parent, SettingsTabDefinition tab, int index, float width, float height, float gap)
        {
            GameObject go = BaseBuilder.GetOrCreateChildPrefabInstance(parent, ComponentsBuilder.PrefabPath("BTN_SettingsTab"), "BTN_SettingsTab" + tab.Id);
            go.transform.SetSiblingIndex(index);
            PlaceTopLeft((RectTransform)go.transform, 0f, index * (height + gap), width, height);
            var button = go.GetComponent<SettingsTabButton>();
            button.SetData(ComponentsBuilder.LoadIcon(tab.IconOutlineName), ComponentsBuilder.LoadIcon(tab.IconFilledName), tab.LabelKey);
            BaseBuilder.RecordInstanceOverrides(go);
            return button;
        }

        /// <summary>
        /// Crea o aggiorna il gruppo di righe di una scheda (PANEL_Rows&lt;Scheda&gt;) con un'istanza di prefab per riga.
        /// </summary>
        private static RectTransform SettingsBuildRowGroup(RectTransform parent, SettingsTabDefinition tab, bool visible, float rowWidth, float rowHeight,
            float gap, StringBuilder report)
        {
            RectTransform group = BaseBuilder.GetOrCreateRectChild(parent, "PANEL_Rows" + tab.Id);
            PlaceTopLeft(group, 0f, 0f, rowWidth, tab.Rows.Length * rowHeight + Mathf.Max(0, tab.Rows.Length - 1) * gap);
            group.SetSiblingIndex((int)tab.Id);
            for (int i = 0; i < tab.Rows.Length; i++)
            {
                SettingsRowDefinition definition = tab.Rows[i];
                string prefabName = definition.Type == SettingsRowType.Toggle ? "PANEL_SettingsToggle"
                    : definition.Type == SettingsRowType.Slider ? "PANEL_SettingsSlider"
                    : definition.Type == SettingsRowType.Selector ? "PANEL_SettingsSelector"
                    : "PANEL_SettingsAction";
                GameObject go = BaseBuilder.GetOrCreateChildPrefabInstance(group, ComponentsBuilder.PrefabPath(prefabName), "PANEL_Settings" + SettingsPascal(definition.Id));
                go.transform.SetSiblingIndex(i);
                PlaceTopLeft((RectTransform)go.transform, 0f, i * (rowHeight + gap), rowWidth, rowHeight);
                var row = go.GetComponent<UISettingsRow>();
                Sprite icon = string.IsNullOrEmpty(definition.ActionIconName) ? null : ComponentsBuilder.LoadIcon(definition.ActionIconName);
                if (!SettingsCatalog.ConfigureRow(row, definition, icon))
                    report.Append($"\n  ERRORE: la riga {definition.Id} non è del tipo {definition.Type}.");
                BaseBuilder.RecordInstanceOverrides(go);
            }
            // Solo la prima scheda è visibile nel prefab; a runtime decide SettingsScreen.SelectTab.
            group.gameObject.SetActive(visible);
            return group;
        }

        /// <summary>
        /// Converte un id "comfort.turn_mode" in PascalCase senza separatori ("ComfortTurnMode") per il nome dell'istanza.
        /// </summary>
        private static string SettingsPascal(string id)
        {
            var builder = new StringBuilder();
            foreach (string part in id.Split('.', '_'))
            {
                if (part.Length == 0) continue;
                builder.Append(char.ToUpperInvariant(part[0])).Append(part, 1, part.Length - 1);
            }
            return builder.ToString();
        }
    }
}
