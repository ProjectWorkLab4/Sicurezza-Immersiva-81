using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// =============================================================================================================
// HELPER CONDIVISI (Blocco B) che i file parziali ScreensBuilder.<Nome>.cs POSSONO usare (firme esatte):
//   internal const string SCREENS_FOLDER                          // "Assets/_Project/Home/UI/Prefabs/Screens"
//   internal const string MISSIONS_FOLDER                         // "Assets/_Project/Home/UI/Data/Missions"
//   internal const string CATALOG_ASSET                           // ".../Data/Missions/SO_MissionCatalog.asset"
//   internal static bool TryLoadContext(out UITheme theme, out UIStrings strings, out MissionCatalog catalog)
//   internal static string ScreenPrefabPath(string prefabName)
//   internal static GameObject CreateOrLoadScreenPrefab(string prefabName, UIScreenId id, UITheme theme, out bool existed)
//   internal static MenuPanel AddMenuPanel(GameObject root, UITheme theme, string titleKey, string breadcrumbKey, float height, bool showBack)
//   internal static SidePanel AddSidePanel(GameObject root, UITheme theme, string titleKey, MenuPanel attachTo)
//   internal static GameObject InstantiateComponent(string prefabName, Transform parent, string instanceName)
//   internal static bool RequireKeys(UIStrings strings, string context, params string[] keys)
//   internal static string SaveScreenPrefab(GameObject root, string prefabName, bool existed)
//   internal static T LoadOrCreateAsset<T>(string path, out bool created) where T : ScriptableObject   (in ScreensBuilder.Data.cs)
// Ogni schermata implementa nel proprio file parziale:  internal static string BuildHome() / BuildMissionBrief() / BuildStats() /
//   BuildProfile() / BuildSettings() / BuildTrainingBrief()   (ritorna il riepilogo in italiano; BuildAll le chiama in sequenza).
// =============================================================================================================

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder delle schermate menu (Blocco B), diviso in file parziali: questo file (punto di ingresso e strumenti comuni),
    /// .Data (SO_Mission_*, SO_LevelConfig_*, SO_MissionCatalog) e uno per schermata (.Home, .MissionBrief, .Stats, .Profile,
    /// .Settings, .TrainingBrief). Menu <c>SI81/UI/Build Screens</c> → <see cref="BuildAll"/>: crea o aggiorna i prefab PANEL_* in
    /// Assets/_Project/Home/UI/Prefabs/Screens/ (create-or-update sul posto, stessi GUID, figli cercati per nome, mai YAML a mano).
    /// Richiede tema, testi e i prefab dei componenti (SI81/UI/Build Theme, Build Base, Build Components).
    /// </summary>
    internal static partial class ScreensBuilder
    {
        /// <summary>Cartella dei prefab delle schermate.</summary>
        internal const string SCREENS_FOLDER = BaseBuilder.PREFABS_FOLDER + "/Screens";
        /// <summary>Cartella dei dati di scenario (SO_Mission_*, SO_LevelConfig_*, SO_MissionCatalog).</summary>
        internal const string MISSIONS_FOLDER = ThemeBuilder.DATA_FOLDER + "/Missions";
        /// <summary>Asset del catalogo scenari.</summary>
        internal const string CATALOG_ASSET = MISSIONS_FOLDER + "/SO_MissionCatalog.asset";

        /// <summary>
        /// Voce di menu: crea o aggiorna dati e prefab di tutte le schermate del Blocco B.
        /// </summary>
        [MenuItem("SI81/UI/Build Screens", priority = 26)]
        public static void BuildScreensMenu()
        {
            Debug.Log("[SI81 UI] " + BuildAll());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Esegue tutti i passi nell'ordine: dati, Home, Mission Brief, Statistiche, Profilo, Impostazioni, Training Brief.
        /// Un passo che fallisce non ferma gli altri (errore in Console).
        /// </summary>
        /// <returns>Riepilogo in italiano, una riga per passo.</returns>
        internal static string BuildAll()
        {
            var lines = new List<string>();
            RunStep(lines, "Dati", BuildDataAssets);
            // Gli asset dati devono esistere su disco prima dei prefab che li leggono.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BaseBuilder.EnsureFolder(SCREENS_FOLDER);
            RunStep(lines, "Home", () => { BuildHome(); return "Home: prefab PANEL_Home aggiornato (dettaglio in Console)."; });
            RunStep(lines, "Mission Brief", () => { BuildMissionBrief(); return "Mission Brief: prefab PANEL_MissionBrief aggiornato (dettaglio in Console)."; });
            RunStep(lines, "Statistiche", () => { BuildStats(); return "Statistiche: prefab PANEL_Stats aggiornato (dettaglio in Console)."; });
            RunStep(lines, "Profilo", () => { BuildProfile(); return "Profilo: prefab PANEL_Profile aggiornato (dettaglio in Console)."; });
            RunStep(lines, "Impostazioni", () => { BuildSettings(); return "Impostazioni: prefab PANEL_Settings aggiornato (dettaglio in Console)."; });
            RunStep(lines, "Training Brief", () => { BuildTrainingBrief(); return "Training Brief: prefab PANEL_TrainingBrief aggiornato (dettaglio in Console)."; });
            AssetDatabase.SaveAssets();
            return "Build Screens completato:\n" + string.Join("\n", lines);
        }

        /// <summary>
        /// Esegue un passo isolando le eccezioni, così gli altri passi girano comunque.
        /// </summary>
        /// <param name="lines">Righe di riepilogo (si aggiunge quella del passo).</param>
        /// <param name="stepName">Nome del passo, per i messaggi.</param>
        /// <param name="step">Metodo da eseguire (ritorna il riepilogo).</param>
        private static void RunStep(List<string> lines, string stepName, Func<string> step)
        {
            try
            {
                lines.Add(" - " + step());
            }
            catch (Exception exception)
            {
                lines.Add($" - {stepName}: ERRORE, passo interrotto.");
                Debug.LogError($"[SI81 UI] Build Screens · {stepName}: errore, passo interrotto. {exception}");
            }
        }

        // =====================================================================
        // Strumenti comuni per le schermate
        // =====================================================================

        /// <summary>
        /// Carica tema, testi e catalogo (se mancano, scrive un errore in Console e restituisce false).
        /// </summary>
        /// <param name="theme">SO_UITheme.</param>
        /// <param name="strings">SO_UIStrings_It.</param>
        /// <param name="catalog">SO_MissionCatalog.</param>
        /// <returns>true se i tre asset esistono.</returns>
        internal static bool TryLoadContext(out UITheme theme, out UIStrings strings, out MissionCatalog catalog)
        {
            theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CATALOG_ASSET);
            if (theme == null || strings == null)
            {
                Debug.LogError("[SI81 UI] Build Screens: SO_UITheme o SO_UIStrings_It mancanti (eseguire SI81/UI/Build Theme).");
                return false;
            }
            if (catalog == null)
            {
                Debug.LogError("[SI81 UI] Build Screens: SO_MissionCatalog mancante (si crea con Build Screens, passo Dati).");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Percorso del prefab di una schermata.
        /// </summary>
        /// <param name="prefabName">Nome del prefab, es. PANEL_Home.</param>
        /// <returns>Percorso sotto Prefabs/Screens.</returns>
        internal static string ScreenPrefabPath(string prefabName)
        {
            return SCREENS_FOLDER + "/" + prefabName + ".prefab";
        }

        /// <summary>
        /// Carica il contenuto del prefab della schermata (per aggiornarlo sul posto) oppure crea la radice di uno nuovo:
        /// RectTransform a tutto schermo (come le schermate di PANEL_Screens), CanvasGroup e UIScreen con l'id dato.
        /// Da chiudere SEMPRE con <see cref="SaveScreenPrefab"/>.
        /// </summary>
        /// <param name="prefabName">Nome del prefab (= nome della radice), es. PANEL_Home.</param>
        /// <param name="id">Id della schermata per UIScreenManager.</param>
        /// <param name="theme">Tema (durata della dissolvenza).</param>
        /// <param name="existed">true se il prefab esisteva già.</param>
        /// <returns>La radice da modificare.</returns>
        internal static GameObject CreateOrLoadScreenPrefab(string prefabName, UIScreenId id, UITheme theme, out bool existed)
        {
            BaseBuilder.EnsureFolder(SCREENS_FOLDER);
            string path = ScreenPrefabPath(prefabName);
            existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(prefabName, typeof(RectTransform));
            root.name = prefabName;
            root.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)root.transform;
            BaseBuilder.Stretch(rect);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            BaseBuilder.GetOrAdd<CanvasGroup>(root);
            BaseBuilder.GetOrAdd<UIScreen>(root).Configure(id, theme);
            return root;
        }

        /// <summary>
        /// Aggiunge (o ritrova) l'istanza di PANEL_Menu nella schermata e ne imposta titolo, percorso, indietro e altezza.
        /// </summary>
        /// <param name="root">Radice della schermata.</param>
        /// <param name="theme">Tema.</param>
        /// <param name="titleKey">Chiave del titolo (vuota = nessun titolo).</param>
        /// <param name="breadcrumbKey">Chiave del percorso sopra il titolo (vuota = nessuno).</param>
        /// <param name="height">Altezza in unità Canvas (0 = predefinita del tema, 475).</param>
        /// <param name="showBack">true per mostrare il posto di BTN_Back.</param>
        /// <returns>Il MenuPanel dell'istanza.</returns>
        internal static MenuPanel AddMenuPanel(GameObject root, UITheme theme, string titleKey, string breadcrumbKey, float height, bool showBack)
        {
            GameObject go = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, MenuPanelBuilder.PREFAB_PATH, MenuPanelBuilder.ROOT_NAME);
            var panel = go.GetComponent<MenuPanel>();
            MenuPanelBuilder.ConfigureInstance(panel, titleKey ?? string.Empty, breadcrumbKey ?? string.Empty, showBack, height);
            return panel;
        }

        /// <summary>
        /// Aggiunge (o ritrova) l'istanza di PANEL_Side, a sinistra del pannello dato, e ne imposta il titolo.
        /// </summary>
        /// <param name="root">Radice della schermata.</param>
        /// <param name="theme">Tema.</param>
        /// <param name="titleKey">Chiave del titolo (es. SidePanel.DEFAULT_TITLE_KEY).</param>
        /// <param name="attachTo">PANEL_Menu accanto a cui sta (tetto allineato, distanza dal tema).</param>
        /// <returns>Il SidePanel dell'istanza.</returns>
        internal static SidePanel AddSidePanel(GameObject root, UITheme theme, string titleKey, MenuPanel attachTo)
        {
            GameObject go = BaseBuilder.GetOrCreateChildPrefabInstance(root.transform, SidePanelBuilder.PREFAB_PATH, SidePanelBuilder.ROOT_NAME);
            go.transform.SetSiblingIndex(0);
            var side = go.GetComponent<SidePanel>();
            SidePanelBuilder.ConfigureInstance(side, titleKey, -1f, attachTo != null ? (RectTransform)attachTo.transform : null);
            return side;
        }

        /// <summary>
        /// Aggiunge (o ritrova) come figlio diretto l'istanza di un prefab dei componenti del Blocco A
        /// (Prefabs/Components/&lt;prefabName&gt;.prefab). Rieseguibile: lo stesso nome non crea doppioni.
        /// </summary>
        /// <param name="prefabName">Nome del prefab, es. BTN_ScenarioCard.</param>
        /// <param name="parent">Genitore.</param>
        /// <param name="instanceName">Nome dell'istanza (unico sotto il genitore), es. BTN_ScenarioCard_01.</param>
        /// <returns>L'istanza (null e un errore se il prefab manca).</returns>
        internal static GameObject InstantiateComponent(string prefabName, Transform parent, string instanceName)
        {
            string path = ComponentsBuilder.PrefabPath(prefabName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogError($"[SI81 UI] Prefab {prefabName} assente in {path} (eseguire SI81/UI/Build Components).");
                return null;
            }
            GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(parent, path, instanceName);
            instance.layer = LayerMask.NameToLayer("UI");
            return instance;
        }

        /// <summary>
        /// Controlla che le chiavi di testo esistano in SO_UIStrings_It e segnala con un errore quelle mancanti.
        /// </summary>
        /// <param name="strings">Tabella dei testi.</param>
        /// <param name="context">Nome della schermata, per il messaggio.</param>
        /// <param name="keys">Chiavi da controllare (le vuote vengono ignorate).</param>
        /// <returns>true se tutte esistono.</returns>
        internal static bool RequireKeys(UIStrings strings, string context, params string[] keys)
        {
            bool ok = true;
            foreach (string key in keys)
            {
                if (string.IsNullOrEmpty(key) || strings.Contains(key)) continue;
                Debug.LogError($"[SI81 UI] {context}: chiave \"{key}\" assente in SO_UIStrings_It.");
                ok = false;
            }
            return ok;
        }

        /// <summary>
        /// Salva la radice come prefab di schermata (stesso GUID se esisteva) e libera la memoria.
        /// </summary>
        /// <param name="root">Radice ottenuta da <see cref="CreateOrLoadScreenPrefab"/>.</param>
        /// <param name="prefabName">Nome del prefab.</param>
        /// <param name="existed">Valore restituito da <see cref="CreateOrLoadScreenPrefab"/>.</param>
        /// <returns>Riepilogo in italiano.</returns>
        internal static string SaveScreenPrefab(GameObject root, string prefabName, bool existed)
        {
            string path = ScreenPrefabPath(prefabName);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                return success ? $"{prefabName}: {(existed ? "aggiornato" : "creato")} ({path})." : $"{prefabName}: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (existed) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Ancora un RectTransform in alto a sinistra del genitore con posizione e misura date (y verso il basso).
        /// </summary>
        internal static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
