using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SI81.UI.Sandbox;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder del passo 2 (Base): genera o aggiorna i materiali del puntatore e dello sfondo, il prefab MNG_UI
    /// (UIScreenManager + EventSystem con InputSystemUIInputModule + CANVAS_Menu) e la scena SCN_UISandbox
    /// (rig minimo, schermate segnaposto, pulsanti di debug). Sicuro da rieseguire: trova gli oggetti per nome,
    /// aggiorna sul posto, non duplica nulla e mantiene i GUID di prefab, scena, materiali e texture.
    /// Richiede SO_UITheme e SO_UIStrings_It (menu SI81/UI/Build Theme).
    /// </summary>
    public static class BaseBuilder
    {
        // =====================================================================
        // Percorsi
        // =====================================================================

        /// <summary>Cartella dei prefab della UI comune.</summary>
        public const string PREFABS_FOLDER = ThemeBuilder.ROOT + "/UI/Prefabs";
        /// <summary>Cartella dei prefab di base (MNG_UI, PANEL_Menu, PANEL_Side, PANEL_BottomBar, BTN_BottomBarItem).</summary>
        public const string BASE_PREFABS_FOLDER = PREFABS_FOLDER + "/Base";
        /// <summary>Cartella dei materiali della UI comune.</summary>
        public const string MATERIALS_FOLDER = ThemeBuilder.ROOT + "/Art/Materials";
        /// <summary>Cartella delle texture generate.</summary>
        public const string TEXTURES_FOLDER = ThemeBuilder.ROOT + "/Art/Textures/UI";
        /// <summary>Cartella delle scene della UI comune.</summary>
        public const string SCENES_FOLDER = ThemeBuilder.ROOT + "/Level/Scenes";
        /// <summary>Prefab MNG_UI.</summary>
        public const string MNG_UI_PREFAB = BASE_PREFABS_FOLDER + "/MNG_UI.prefab";
        /// <summary>Materiale del raggio del puntatore.</summary>
        public const string POINTER_MATERIAL = MATERIALS_FOLDER + "/MAT_UIPointer.mat";
        /// <summary>Materiale del reticolo del puntatore.</summary>
        public const string RETICLE_MATERIAL = MATERIALS_FOLDER + "/MAT_UIPointerReticle.mat";
        /// <summary>Materiale dello sfondo segnaposto della sandbox.</summary>
        public const string BACKDROP_MATERIAL = MATERIALS_FOLDER + "/MAT_SandboxBackdrop.mat";
        /// <summary>Texture del gradiente dello sfondo segnaposto.</summary>
        public const string BACKDROP_TEXTURE = TEXTURES_FOLDER + "/TEX_SandboxBackdrop.asset";
        /// <summary>Icona usata come reticolo (anello con punto, bianca, colorata dal tema).</summary>
        public const string RETICLE_ICON = IconBuilder.ICONS_FOLDER + "/ICN_RadioButtonChecked.png";
        /// <summary>Icona del pulsante di debug "indietro".</summary>
        public const string BACK_ICON = IconBuilder.ICONS_FOLDER + "/ICN_ArrowBack.png";
        /// <summary>Scena di prova della UI.</summary>
        public const string SANDBOX_SCENE = SCENES_FOLDER + "/SCN_UISandbox.unity";

        // =====================================================================
        // Nomi dei GameObject (nome in scena = nome dell'asset)
        // =====================================================================

        /// <summary>Radice del prefab MNG_UI.</summary>
        public const string MNG_UI_NAME = "MNG_UI";
        /// <summary>GameObject dell'unico EventSystem.</summary>
        public const string EVENT_SYSTEM_NAME = "MNG_EventSystem";
        /// <summary>Vecchio nome dell'EventSystem (fino a v0.2.3), rinominato dalla migrazione.</summary>
        private const string EVENT_SYSTEM_LEGACY_NAME = "EventSystem";
        /// <summary>Canvas dei menu.</summary>
        public const string MENU_CANVAS_NAME = "CANVAS_Menu";
        /// <summary>Contenitore delle schermate.</summary>
        public const string SCREENS_NAME = "PANEL_Screens";
        /// <summary>Posto della bottom bar.</summary>
        public const string BOTTOM_BAR_SLOT_NAME = "PANEL_BottomBarSlot";

        // =====================================================================
        // Misure
        // =====================================================================

        /// <summary>Raggio della sfera di sfondo della sandbox, in metri.</summary>
        public const float BACKDROP_RADIUS_M = 30f;
        /// <summary>Pixel verticali della texture del gradiente.</summary>
        private const int BACKDROP_TEXTURE_HEIGHT = 64;
        /// <summary>Posizione del canvas di debug (a DESTRA, poco sotto gli occhi), in metri: a sinistra c'è PANEL_Side di Mission Brief.</summary>
        private static readonly Vector3 DEBUG_CANVAS_POSITION = new Vector3(1.2f, 1.35f, 1.1f);
        /// <summary>Rotazione Y del canvas di debug: rivolto verso il giocatore (fuori dal bordo destro di PANEL_Menu, x 0,72 m).</summary>
        private const float DEBUG_CANVAS_YAW = 45f;
        /// <summary>Larghezza del canvas di debug (unità Canvas).</summary>
        private const float DEBUG_CANVAS_WIDTH = 260f;
        /// <summary>Posizioni iniziali dei controller sul desktop (nessun tracciamento), in metri.</summary>
        private static readonly Vector3 LEFT_CONTROLLER_REST = new Vector3(-0.2f, 1.2f, 0.3f);
        private static readonly Vector3 RIGHT_CONTROLLER_REST = new Vector3(0.2f, 1.2f, 0.3f);

        /// <summary>Coda di rendering di raggio e reticolo: dopo la UI trasparente (3000).</summary>
        private const int POINTER_RENDER_QUEUE = 3100;

        /// <summary>Chiave del titolo di ogni schermata segnaposto (chiavi esistenti di SO_UIStrings_It).</summary>
        private static readonly Dictionary<UIScreenId, string> SCREEN_TITLE_KEYS = new Dictionary<UIScreenId, string>
        {
            { UIScreenId.Home, "ui.nav.home" },
            { UIScreenId.MissionBrief, "ui.brief.breadcrumb" },
            { UIScreenId.Stats, "ui.stats.title" },
            { UIScreenId.Profile, "ui.profile.title" },
            { UIScreenId.Settings, "ui.settings.title" },
            { UIScreenId.TrainingBrief, "ui.training.breadcrumb" },
            { UIScreenId.ComponentGallery, "ui.sandbox.gallery" },
        };
        /// <summary>
        /// Schermate di pausa e fine missione (Blocco C): hanno sempre il prefab reale, nessun segnaposto né pulsante di debug
        /// (si aprono dal flusso di missione). Posizionate in PANEL_Screens in ordine di UIScreenId.
        /// </summary>
        private static readonly UIScreenId[] FLOW_SCREEN_IDS = { UIScreenId.Pause, UIScreenId.Debrief, UIScreenId.ScoreReplay, UIScreenId.TrainingComplete };
        /// <summary>Parametri di PANEL_Menu in una schermata segnaposto della sandbox.</summary>
        private struct PanelSetup
        {
            /// <summary>Chiave del titolo.</summary>
            public string TitleKey;
            /// <summary>Chiave del percorso (vuota = nessuno).</summary>
            public string BreadcrumbKey;
            /// <summary>Mostra il posto di BTN_Back.</summary>
            public bool ShowBack;
            /// <summary>Token dell'altezza del pannello (NumberTokens).</summary>
            public string HeightToken;
        }

        /// <summary>
        /// PANEL_Menu di ogni schermata segnaposto. Home: "Scenari", senza indietro, 475. Mission Brief: percorso, indietro, 535
        /// (il titolo vero è il nome dello scenario da SO_Mission_*, qui un segnaposto). Le radici della bottom bar non hanno indietro.
        /// </summary>
        private static readonly Dictionary<UIScreenId, PanelSetup> SCREEN_PANELS = new Dictionary<UIScreenId, PanelSetup>
        {
            { UIScreenId.Home, new PanelSetup { TitleKey = "ui.home.title", BreadcrumbKey = "", ShowBack = false, HeightToken = NumberTokens.MENU_PANEL_HEIGHT } },
            { UIScreenId.MissionBrief, new PanelSetup { TitleKey = "ui.brief.description", BreadcrumbKey = "ui.brief.breadcrumb", ShowBack = true, HeightToken = NumberTokens.MENU_PANEL_HEIGHT_BRIEF } },
            { UIScreenId.Stats, new PanelSetup { TitleKey = "ui.stats.title", BreadcrumbKey = "", ShowBack = false, HeightToken = NumberTokens.MENU_PANEL_HEIGHT } },
            { UIScreenId.Profile, new PanelSetup { TitleKey = "ui.profile.title", BreadcrumbKey = "", ShowBack = false, HeightToken = NumberTokens.MENU_PANEL_HEIGHT } },
            { UIScreenId.Settings, new PanelSetup { TitleKey = "ui.settings.title", BreadcrumbKey = "", ShowBack = false, HeightToken = NumberTokens.MENU_PANEL_HEIGHT } },
            { UIScreenId.TrainingBrief, new PanelSetup { TitleKey = "ui.training.title", BreadcrumbKey = "ui.training.breadcrumb", ShowBack = true, HeightToken = NumberTokens.MENU_PANEL_HEIGHT } },
            // Galleria dei componenti (solo sandbox): altezza fissa di GalleryBuilder (il token qui è solo un segnaposto).
            { UIScreenId.ComponentGallery, new PanelSetup { TitleKey = "ui.sandbox.gallery", BreadcrumbKey = "", ShowBack = false, HeightToken = NumberTokens.MENU_PANEL_HEIGHT } },
        };
        /// <summary>Chiave del pulsante di debug "torna alla radice".</summary>
        private const string GO_HOME_KEY = "ui.button.back_to_home";

        // =====================================================================
        // Menu
        // =====================================================================

        /// <summary>
        /// Voce di menu: genera materiali, MNG_UI e SCN_UISandbox, poi apre e salva la sandbox.
        /// </summary>
        [MenuItem("SI81/UI/Build Base", priority = 20)]
        public static void BuildBaseMenu()
        {
            Debug.Log(BuildAll());
        }

        /// <summary>
        /// Esegue tutto il passo 2 e restituisce un riepilogo in italiano (usabile anche da CLI).
        /// </summary>
        /// <returns>Riepilogo delle operazioni.</returns>
        public static string BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "[SI81 UI] Build Base non eseguibile in Play Mode.";

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(ThemeBuilder.STRINGS_ASSET);
            if (theme == null || strings == null)
                return "[SI81 UI] Build Base interrotto: SO_UITheme o SO_UIStrings_It mancanti. Eseguire prima SI81/UI/Build Theme.";
            if (theme.Pointer == null || theme.Pointer.RayMaxLength <= 0f)
                return "[SI81 UI] Build Base interrotto: misure del puntatore assenti in SO_UITheme. Eseguire prima SI81/UI/Build Theme.";

            // La sandbox viene aperta in modalità singola: mai perdere modifiche non salvate di altre scene.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene open = SceneManager.GetSceneAt(i);
                if (open.isDirty && open.path != SANDBOX_SCENE)
                    return $"[SI81 UI] Build Base interrotto: la scena \"{open.name}\" ha modifiche non salvate. Salvarla o chiuderla e rieseguire.";
            }

            EnsureFolder(PREFABS_FOLDER);
            EnsureFolder(MATERIALS_FOLDER);
            EnsureFolder(TEXTURES_FOLDER);
            EnsureFolder(SCENES_FOLDER);

            var report = new StringBuilder("[SI81 UI] Build Base completato.\n");
            Materials materials = BuildMaterials(theme, report);
            AssetDatabase.SaveAssets();

            // Componenti del Blocco A (pulsanti, etichette, righe impostazioni, card): servono a MNG_UI e alla galleria della sandbox.
            ComponentsBuilder.BuildAll();
            report.AppendLine("Componenti: prefab in " + ComponentsBuilder.COMPONENTS_FOLDER + " (vedi Console per il dettaglio).");

            // Prima i componenti, poi MNG_UI che contiene PANEL_BottomBar (istanza annidata nel PANEL_BottomBarSlot).
            report.AppendLine(MenuPanelBuilder.BuildPrefab(theme, strings));
            report.AppendLine(SidePanelBuilder.BuildPrefab(theme, strings));
            report.AppendLine(BottomBarBuilder.BuildPrefabs(theme, strings));
            // Dati di scenario (catalogo): servono a UIServicesBootstrap di MNG_UI.
            report.AppendLine(ScreensBuilder.BuildDataAssets());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            report.AppendLine(BuildManagerPrefab(theme));
            // Schermate menu del Blocco B (prefab PANEL_* in Prefabs/Screens): sostituiscono i segnaposto nella sandbox.
            report.AppendLine(ScreensBuilder.BuildAll());
            // HUD in scenario e schermate di pausa/fine (Blocco C): prima della sandbox, che istanzia MNG_HUD e le schermate.
            HUDBuilder.BuildAll();
            report.AppendLine("HUD: prefab in " + HUDBuilder.HUD_FOLDER + " e schermate di pausa/fine in Prefabs/Screens (vedi Console).");
            report.AppendLine(BuildSandboxScene(theme, strings, materials));
            AssetDatabase.SaveAssets();
            return report.ToString().TrimEnd();
        }

        // =====================================================================
        // Materiali
        // =====================================================================

        /// <summary>Materiali generati dal builder.</summary>
        private struct Materials
        {
            /// <summary>Materiale del raggio del puntatore.</summary>
            public Material Pointer;
            /// <summary>Materiale del mirino sul punto colpito.</summary>
            public Material Reticle;
            /// <summary>Materiale dello sfondo della sandbox.</summary>
            public Material Backdrop;
        }

        /// <summary>
        /// Crea o aggiorna MAT_UIPointer, MAT_UIPointerReticle, TEX_SandboxBackdrop e MAT_SandboxBackdrop (URP Unlit).
        /// </summary>
        private static Materials BuildMaterials(UITheme theme, StringBuilder report)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null)
            {
                Debug.LogError("[SI81 UI] Shader \"Universal Render Pipeline/Unlit\" non trovato.");
                return default;
            }

            // Raggio: bianco trasparente, colore per renderer da MaterialPropertyBlock (_BaseColor).
            Material pointer = LoadOrCreateMaterial(POINTER_MATERIAL, unlit, out bool pointerCreated);
            SetupTransparent(pointer, null);

            // Reticolo: icona anello (alfa) su quad, visibile da entrambi i lati.
            Material reticle = LoadOrCreateMaterial(RETICLE_MATERIAL, unlit, out bool reticleCreated);
            SetupTransparent(reticle, AssetDatabase.LoadAssetAtPath<Texture2D>(RETICLE_ICON));

            // Sfondo: gradiente verticale scuro dai token color.backdrop.* su una sfera vista dall'interno.
            Texture2D gradient = BuildBackdropTexture(theme);
            Material backdrop = LoadOrCreateMaterial(BACKDROP_MATERIAL, unlit, out bool backdropCreated);
            SetupOpaque(backdrop, gradient);

            report.AppendLine($"Materiali: MAT_UIPointer {(pointerCreated ? "creato" : "aggiornato")}, " +
                              $"MAT_UIPointerReticle {(reticleCreated ? "creato" : "aggiornato")}, " +
                              $"MAT_SandboxBackdrop {(backdropCreated ? "creato" : "aggiornato")}, TEX_SandboxBackdrop.");
            return new Materials { Pointer = pointer, Reticle = reticle, Backdrop = backdrop };
        }

        /// <summary>
        /// Carica un materiale o lo crea con lo shader dato (percorso fisso: mai duplicati).
        /// </summary>
        private static Material LoadOrCreateMaterial(string path, Shader shader, out bool created)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            created = material == null;
            if (created)
            {
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }

        /// <summary>
        /// Configura un materiale URP Unlit trasparente (alpha blend, niente ZWrite, doppia faccia, dopo la UI).
        /// </summary>
        private static void SetupTransparent(Material material, Texture2D texture)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_AlphaClip", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", texture);
            material.renderQueue = POINTER_RENDER_QUEUE;
            EditorUtility.SetDirty(material);
        }

        /// <summary>
        /// Configura un materiale URP Unlit opaco a doppia faccia con la texture data.
        /// </summary>
        private static void SetupOpaque(Material material, Texture2D texture)
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", texture);
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
        }

        /// <summary>
        /// Crea o aggiorna sul posto la texture del gradiente verticale (basso → alto) dai token color.backdrop.*.
        /// </summary>
        private static Texture2D BuildBackdropTexture(UITheme theme)
        {
            Color bottom = theme.GetColor(ColorTokens.BACKDROP_BOTTOM);
            Color top = theme.GetColor(ColorTokens.BACKDROP_TOP);
            var pixels = new Color[2 * BACKDROP_TEXTURE_HEIGHT];
            for (int y = 0; y < BACKDROP_TEXTURE_HEIGHT; y++)
            {
                // Il colore scuro resta nella metà bassa, il gradiente sale verso l'alto (UV v della sfera: 0 in basso).
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, y / (float)(BACKDROP_TEXTURE_HEIGHT - 1)));
                Color c = Color.Lerp(bottom, top, t);
                pixels[y * 2] = c;
                pixels[y * 2 + 1] = c;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BACKDROP_TEXTURE);
            bool created = texture == null;
            if (created) texture = new Texture2D(2, BACKDROP_TEXTURE_HEIGHT, TextureFormat.RGBA32, false) { name = "TEX_SandboxBackdrop" };
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            if (created) AssetDatabase.CreateAsset(texture, BACKDROP_TEXTURE);
            else EditorUtility.SetDirty(texture);
            return texture;
        }

        // =====================================================================
        // Prefab MNG_UI
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna il prefab MNG_UI: UIScreenManager, EventSystem (InputSystemUIInputModule + UIInputSetup),
        /// CANVAS_Menu (World Space, 1,5 m davanti, scala dal tema) con i contenitori "PANEL_Screens" e "PANEL_BottomBarSlot".
        /// </summary>
        private static string BuildManagerPrefab(UITheme theme)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(MNG_UI_PREFAB) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(MNG_UI_PREFAB) : new GameObject(MNG_UI_NAME);
            try
            {
                root.name = MNG_UI_NAME;
                ResetLocal(root.transform);
                UIScreenManager manager = GetOrAdd<UIScreenManager>(root);
                // Flusso di missione (pausa, esito, Debrief, Score & Replay): un solo driver su MNG_UI, nessun parametro da configurare.
                GetOrAdd<MissionFlowDriver>(root);
                // Adattatore rig -> UI: presente nel prefab, i riferimenti si assegnano nell'istanza di SCN_Core.
                GetOrAdd<SI81.UI.Rig.RigUIBridge>(root);

                // --- EventSystem: l'unico dell'app. ---
                GameObject eventSystemGo = GetOrCreateChild(root.transform, EVENT_SYSTEM_NAME, EVENT_SYSTEM_LEGACY_NAME).gameObject;
                ResetLocal(eventSystemGo.transform);
                GetOrAdd<EventSystem>(eventSystemGo);
                InputSystemUIInputModule module = GetOrAdd<InputSystemUIInputModule>(eventSystemGo);
                // Nessun asset di azioni salvato: le azioni le crea UIInputSetup a runtime.
                module.UnassignActions();
                UIInputSetup inputSetup = GetOrAdd<UIInputSetup>(eventSystemGo);
                inputSetup.Configure(module, null);

                // --- CANVAS_Menu. ---
                RectTransform canvasRect = GetOrCreateRectChild(root.transform, MENU_CANVAS_NAME);
                // Posa di default nel prefab (UIScreenManager.PlaceInFront la ricalcola a runtime davanti alla testa).
                ConfigureWorldCanvas(canvasRect.gameObject, theme, theme.MenuCanvasSize,
                    new Vector3(0f, theme.MenuCenterHeight, theme.MenuDistance), Quaternion.identity,
                    UITheme.GetCanvasScale(theme.MenuDistance));
                GetOrAdd<CanvasGroup>(canvasRect.gameObject);

                RectTransform screens = GetOrCreateRectChild(canvasRect, SCREENS_NAME, "Screens");
                Stretch(screens);
                RectTransform bottomBarSlot = GetOrCreateRectChild(canvasRect, BOTTOM_BAR_SLOT_NAME, "BottomBarSlot");
                // Fascia in basso, larga quanto il canvas, altezza 0: PANEL_BottomBar decide la propria altezza.
                bottomBarSlot.anchorMin = new Vector2(0f, 0f);
                bottomBarSlot.anchorMax = new Vector2(1f, 0f);
                bottomBarSlot.pivot = new Vector2(0.5f, 0f);
                bottomBarSlot.anchoredPosition = Vector2.zero;
                bottomBarSlot.sizeDelta = Vector2.zero;
                bottomBarSlot.localScale = Vector3.one;
                bottomBarSlot.SetAsLastSibling();
                // PANEL_BottomBar (prefab annidato, posizione layoutHome; a runtime segue il PANEL_Menu corrente).
                BottomBarBuilder.PlaceInSlot(bottomBarSlot);

                manager.Configure(screens, bottomBarSlot, inputSetup, theme, canvasRect.gameObject);

                // Servizi comuni (catalogo, progressi, impostazioni, lanciatore). Nel prefab useMocks = false (gioco vero:
                // PlayerPrefs + caricamento scene); SCN_UISandbox lo imposta a true sulla propria istanza.
                var bootstrap = GetOrAdd<UIServicesBootstrap>(root);
                bootstrap.Configure(AssetDatabase.LoadAssetAtPath<MissionCatalog>(ScreensBuilder.CATALOG_ASSET), false, theme);

                PrefabUtility.SaveAsPrefabAsset(root, MNG_UI_PREFAB, out bool success);
                return success ? $"MNG_UI: {(exists ? "aggiornato" : "creato")} ({MNG_UI_PREFAB})." : "MNG_UI: ERRORE nel salvataggio del prefab.";
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Configura un Canvas World Space con UIWorldRaycaster (controller + mouse in Editor, rispetta raycastTarget e
        /// CanvasGroup) e WorldCanvasCamera (camera assegnata a runtime). Toglie TrackedDeviceRaycaster e il
        /// GraphicRaycaster semplice delle versioni ≤ 0.2.2: con un TrackedDeviceRaycaster in scena Input System
        /// non passerebbe i raggi dei controller a UIWorldRaycaster.
        /// </summary>
        private static void ConfigureWorldCanvas(GameObject go, UITheme theme, Vector2 size, Vector3 localPosition, Quaternion localRotation, float scale)
        {
            Canvas canvas = GetOrAdd<Canvas>(go);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localPosition = localPosition;
            rect.localRotation = localRotation;
            rect.localScale = Vector3.one * scale;
            foreach (TrackedDeviceRaycaster tracked in go.GetComponents<TrackedDeviceRaycaster>()) Object.DestroyImmediate(tracked);
            foreach (UnityEngine.UI.GraphicRaycaster plain in go.GetComponents<UnityEngine.UI.GraphicRaycaster>())
                if (plain.GetType() == typeof(UnityEngine.UI.GraphicRaycaster)) Object.DestroyImmediate(plain);
            GetOrAdd<UIWorldRaycaster>(go).Configure(theme.Pointer.RayMaxLength);
            GetOrAdd<WorldCanvasCamera>(go);
        }

        // =====================================================================
        // Scena SCN_UISandbox
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna SCN_UISandbox: sfondo scuro, rig minimo (camera + 2 controller con UIPointer), istanza di
        /// MNG_UI con 6 schermate segnaposto e canvas di debug. Lascia la scena aperta e salvata.
        /// </summary>
        private static string BuildSandboxScene(UITheme theme, UIStrings strings, Materials materials)
        {
            bool exists = File.Exists(Path.GetFullPath(SANDBOX_SCENE));
            Scene scene = exists
                ? EditorSceneManager.OpenScene(SANDBOX_SCENE, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Ambiente: niente skybox, colore scuro (CLAUDE.md §4.4: scena menu poco luminosa). ---
            Color backdropBottom = theme.GetColor(ColorTokens.BACKDROP_BOTTOM);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = backdropBottom;
            RenderSettings.fog = false;

            GameObject backdrop = BuildBackdrop(scene, materials.Backdrop, theme);

            // --- Rig minimo con componenti standard (Input System TrackedPoseDriver). ---
            GameObject origin = GetOrCreateRoot(scene, "MNG_SandboxRig", "XROrigin");
            ResetLocal(origin.transform);
            Transform cameraOffset = GetOrCreateChild(origin.transform, "MNG_RigOffset", "CameraOffset");
            ResetLocal(cameraOffset);

            GameObject cameraGo = GetOrCreateChild(cameraOffset, "MNG_RigHead", "CameraHead").gameObject;
            cameraGo.tag = "MainCamera";
            cameraGo.transform.localPosition = new Vector3(0f, theme.EyeHeight, 0f);
            cameraGo.transform.localRotation = Quaternion.identity;
            Camera camera = GetOrAdd<Camera>(cameraGo);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backdropBottom;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = BACKDROP_RADIUS_M * 3f;
            GetOrAdd<AudioListener>(cameraGo);
            ConfigurePoseDriver(GetOrAdd<TrackedPoseDriver>(cameraGo), "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");

            BuildController(cameraOffset, "MNG_RigLeftHand", UIPointerHand.Left, "{LeftHand}", LEFT_CONTROLLER_REST, theme, materials);
            BuildController(cameraOffset, "MNG_RigRightHand", UIPointerHand.Right, "{RightHand}", RIGHT_CONTROLLER_REST, theme, materials);
            Transform leftHandTransform = cameraOffset.Find("MNG_RigLeftHand");
            Transform rightHandTransform = cameraOffset.Find("MNG_RigRightHand");

            // --- Istanza di MNG_UI. ---
            GameObject managerInstance = GetOrCreatePrefabInstance(scene, MNG_UI_PREFAB, MNG_UI_NAME);
            // Rig -> MNG_UI: testa e origine del tracciamento serializzate (nessun ripiego su Camera.main, nessun avviso).
            UIInputSetup inputSetup = managerInstance.GetComponentInChildren<UIInputSetup>(true);
            var inputSo = new SerializedObject(inputSetup);
            inputSo.FindProperty("xrTrackingOrigin").objectReferenceValue = cameraOffset;
            inputSo.ApplyModifiedPropertiesWithoutUndo();
            var managerSo = new SerializedObject(managerInstance.GetComponent<UIScreenManager>());
            managerSo.FindProperty("head").objectReferenceValue = cameraGo.transform;
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            // --- Schermate segnaposto (oggetti aggiunti all'istanza, sotto "PANEL_Screens"). ---
            Transform screens = managerInstance.transform.Find(MENU_CANVAS_NAME + "/" + SCREENS_NAME);
            // Mocks nella sandbox: progressi finti, impostazioni in memoria, lanciatore finto.
            var bootstrapSo = new SerializedObject(managerInstance.GetComponent<UIServicesBootstrap>());
            bootstrapSo.FindProperty("useMocks").boolValue = true;
            bootstrapSo.FindProperty("seedMockProgress").boolValue = true;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
            foreach (KeyValuePair<UIScreenId, string> entry in SCREEN_TITLE_KEYS)
                PlaceScreen(screens, entry.Key, theme, strings);
            // Schermate di pausa e fine missione (Blocco C): solo prefab reali.
            foreach (UIScreenId flowId in FLOW_SCREEN_IDS)
                PlaceFlowScreen(screens, flowId);

            // --- MNG_HUD (HUD in scenario) e collegamento del rig di prova. ---
            string hudPath = HUDBuilder.HudPrefabPath("MNG_HUD");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(hudPath) != null)
            {
                GameObject hudInstance = GetOrCreatePrefabInstance(scene, hudPath, "MNG_HUD");
                GetOrAdd<UISandboxRig>(origin).Configure(cameraGo.transform, cameraOffset, leftHandTransform, rightHandTransform,
                    managerInstance.GetComponent<UIScreenManager>(), hudInstance.GetComponent<HUDRoot>());
            }
            else Debug.LogWarning("[SI81 UI] Sandbox: prefab MNG_HUD assente, HUD non aggiunta (eseguire SI81/UI/Build HUD).");

            // --- Canvas di debug. ---
            BuildDebugCanvas(scene, theme, strings);

            // Ordine in gerarchia stabile.
            backdrop.transform.SetSiblingIndex(0);
            origin.transform.SetSiblingIndex(1);
            managerInstance.transform.SetSiblingIndex(2);

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, SANDBOX_SCENE);
            return saved
                ? $"SCN_UISandbox: {(exists ? "aggiornata" : "creata")} e salvata ({SANDBOX_SCENE}); non aggiunta alle Build Settings."
                : "SCN_UISandbox: ERRORE nel salvataggio della scena.";
        }

        /// <summary>
        /// Crea o aggiorna la sfera di sfondo (vista dall'interno, materiale a doppia faccia, senza collider né ombre).
        /// </summary>
        private static GameObject BuildBackdrop(Scene scene, Material material, UITheme theme)
        {
            // Sfera di sfondo della sandbox: MESH_ (modello 3D, Nomenclatura).
            GameObject go = GetOrCreateRoot(scene, "MESH_SandboxBackdrop", "SandboxBackdrop");
            go.transform.SetPositionAndRotation(new Vector3(0f, theme.EyeHeight, 0f), Quaternion.identity);
            go.transform.localScale = Vector3.one * BACKDROP_RADIUS_M * 2f;
            MeshFilter filter = GetOrAdd<MeshFilter>(go);
            if (filter.sharedMesh == null) filter.sharedMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
            MeshRenderer renderer = GetOrAdd<MeshRenderer>(go);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        /// <summary>
        /// Crea o aggiorna un controller: TrackedPoseDriver (posa "aim" pointerPosition/pointerRotation, la stessa del modulo UI), LineRenderer, reticolo e UIPointer.
        /// </summary>
        private static void BuildController(Transform parent, string name, UIPointerHand hand, string usage, Vector3 restPosition, UITheme theme, Materials materials)
        {
            GameObject go = GetOrCreateChild(parent, name, name == "MNG_RigLeftHand" ? "LeftController" : "RightController").gameObject;
            go.transform.localPosition = restPosition;
            go.transform.localRotation = Quaternion.identity;
            ConfigurePoseDriver(GetOrAdd<TrackedPoseDriver>(go),
                $"<XRController>{usage}/pointerPosition", $"<XRController>{usage}/pointerRotation", $"<XRController>{usage}/trackingState");

            LineRenderer line = GetOrAdd<LineRenderer>(go);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPositions(new[] { Vector3.zero, Vector3.zero });
            line.startWidth = line.endWidth = theme.Pointer.RayWidth;
            line.sharedMaterial = materials.Pointer;
            line.numCapVertices = 0;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.enabled = false;

            GameObject reticleGo = GetOrCreateChild(go.transform, "VFX_PointerReticle", "Reticle").gameObject;
            reticleGo.transform.localPosition = Vector3.zero;
            reticleGo.transform.localRotation = Quaternion.identity;
            reticleGo.transform.localScale = Vector3.one * theme.Pointer.ReticleDiameter;
            MeshFilter filter = GetOrAdd<MeshFilter>(reticleGo);
            if (filter.sharedMesh == null) filter.sharedMesh = GetPrimitiveMesh(PrimitiveType.Quad);
            MeshRenderer reticle = GetOrAdd<MeshRenderer>(reticleGo);
            reticle.sharedMaterial = materials.Reticle;
            reticle.shadowCastingMode = ShadowCastingMode.Off;
            reticle.receiveShadows = false;
            reticle.lightProbeUsage = LightProbeUsage.Off;
            reticle.reflectionProbeUsage = ReflectionProbeUsage.Off;
            reticle.enabled = false;

            GetOrAdd<UIPointer>(go).Configure(hand, theme, line, reticle);
        }

        /// <summary>
        /// Imposta i binding di un TrackedPoseDriver; li riscrive solo se cambiano (scena stabile tra un'esecuzione e l'altra).
        /// </summary>
        internal static void ConfigurePoseDriver(TrackedPoseDriver driver, string positionPath, string rotationPath, string trackingStatePath)
        {
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            if (!HasSingleBinding(driver.positionInput, positionPath))
                driver.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, positionPath, expectedControlType: "Vector3"));
            if (!HasSingleBinding(driver.rotationInput, rotationPath))
                driver.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, rotationPath, expectedControlType: "Quaternion"));
            if (!HasSingleBinding(driver.trackingStateInput, trackingStatePath))
                driver.trackingStateInput = new InputActionProperty(new InputAction("TrackingState", InputActionType.Value, trackingStatePath, expectedControlType: "Integer"));
            EditorUtility.SetDirty(driver);
        }

        /// <summary>
        /// Indica se una InputActionProperty ha un'azione incorporata con un solo binding al percorso dato.
        /// </summary>
        internal static bool HasSingleBinding(InputActionProperty property, string path)
        {
            InputAction action = property.action;
            return action != null && property.reference == null && action.bindings.Count == 1 && action.bindings[0].path == path;
        }

        /// <summary>
        /// Nome del prefab reale di una schermata (Blocco B), o null se l'id non ha un prefab di prodotto (es. la galleria).
        /// </summary>
        private static string RealScreenPrefabName(UIScreenId id)
        {
            switch (id)
            {
                case UIScreenId.Home: return "PANEL_Home";
                case UIScreenId.MissionBrief: return "PANEL_MissionBrief";
                case UIScreenId.Stats: return "PANEL_Stats";
                case UIScreenId.Profile: return "PANEL_Profile";
                case UIScreenId.Settings: return "PANEL_Settings";
                case UIScreenId.TrainingBrief: return "PANEL_TrainingBrief";
                case UIScreenId.Pause: return "PANEL_Pause";
                case UIScreenId.Debrief: return "PANEL_Debrief";
                case UIScreenId.ScoreReplay: return "PANEL_ScoreReplay";
                case UIScreenId.TrainingComplete: return "PANEL_TrainingComplete";
                default: return null;
            }
        }

        /// <summary>
        /// Mette in PANEL_Screens la schermata reale (istanza del prefab di Prefabs/Screens) al posto del segnaposto; se il prefab
        /// non esiste (o l'id è la galleria) costruisce il segnaposto. Rieseguibile: toglie il segnaposto PANEL_Sandbox&lt;Id&gt; quando
        /// la schermata reale è disponibile. Ordine dei fratelli = valore di UIScreenId. Ogni schermata parte disattiva tranne Home.
        /// </summary>
        private static void PlaceScreen(Transform screens, UIScreenId id, UITheme theme, UIStrings strings)
        {
            string prefabName = RealScreenPrefabName(id);
            string path = prefabName == null ? null : ScreensBuilder.ScreenPrefabPath(prefabName);
            if (path == null || AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                BuildPlaceholderScreen(screens, id, theme, strings);
                return;
            }
            Transform placeholder = screens.Find("PANEL_Sandbox" + id);
            if (placeholder != null) Object.DestroyImmediate(placeholder.gameObject);
            GameObject instance = GetOrCreateChildPrefabInstance(screens, path, prefabName);
            instance.SetActive(id == UIScreenId.Home);
            instance.transform.SetSiblingIndex((int)id);
        }

        /// <summary>
        /// Crea o aggiorna una schermata segnaposto: UIScreen + CanvasGroup e un'istanza di PANEL_Menu (prefab annidato)
        /// con titolo, percorso, indietro e altezza da SCREEN_PANELS. Toglie il vecchio fondo Image e il vecchio LBL_Title (v0.2.0).
        /// </summary>
        private static void BuildPlaceholderScreen(Transform screens, UIScreenId id, UITheme theme, UIStrings strings)
        {
            PanelSetup setup = SCREEN_PANELS[id];
            foreach (string key in new[] { setup.TitleKey, setup.BreadcrumbKey })
                if (!string.IsNullOrEmpty(key) && !strings.Contains(key)) Debug.LogError($"[SI81 UI] Chiave \"{key}\" assente in SO_UIStrings_It.");

            RectTransform rect = GetOrCreateRectChild(screens, "PANEL_Sandbox" + id);
            Stretch(rect);
            UIScreen screen = GetOrAdd<UIScreen>(rect.gameObject);
            screen.Configure(id, theme);
            // Visibile solo Home all'avvio (lo decide comunque UIScreenManager a runtime).
            rect.gameObject.SetActive(id == UIScreenId.Home);
            rect.SetSiblingIndex((int)id);

            // Segnaposto v0.2.0: fondo a tinta unita e titolo propri, ora sostituiti da PANEL_Menu.
            var oldBackground = rect.GetComponent<UnityEngine.UI.Image>();
            if (oldBackground != null) Object.DestroyImmediate(oldBackground);
            Transform oldTitle = rect.Find("LBL_Title");
            if (oldTitle != null) Object.DestroyImmediate(oldTitle.gameObject);

            GameObject panelGo = GetOrCreateChildPrefabInstance(rect, MenuPanelBuilder.PREFAB_PATH, MenuPanelBuilder.ROOT_NAME);
            float panelHeight = id == UIScreenId.ComponentGallery ? GalleryBuilder.PANEL_HEIGHT : theme.GetNumber(setup.HeightToken);
            MenuPanelBuilder.ConfigureInstance(panelGo.GetComponent<MenuPanel>(), setup.TitleKey, setup.BreadcrumbKey, setup.ShowBack, panelHeight);
            panelGo.transform.SetSiblingIndex(1);
            // Schermate con "indietro": BTN_Back nel posto PANEL_BackSlot (il clic è gestito solo da BackButton, una sola Back).
            if (setup.ShowBack)
            {
                RectTransform backSlot = panelGo.GetComponent<MenuPanel>().BackSlot;
                var backPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ComponentsBuilder.COMPONENTS_FOLDER + "/BTN_Back.prefab");
                if (backSlot != null && backPrefab != null)
                {
                    GameObject backGo = GetOrCreateChildPrefabInstance(backSlot, ComponentsBuilder.COMPONENTS_FOLDER + "/BTN_Back.prefab", "BTN_Back");
                    var backRect = (RectTransform)backGo.transform;
                    backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0.5f, 0.5f);
                    backRect.anchoredPosition = Vector2.zero;
                    backRect.localScale = Vector3.one;
                    RecordInstanceOverrides(backGo);
                }
            }
            // Galleria: pagine con tutti i componenti del Blocco A (dati finti e stati a runtime, vedi GalleryBuilder).
            if (id == UIScreenId.ComponentGallery) GalleryBuilder.Populate(rect, panelGo.GetComponent<MenuPanel>(), theme, strings);

            // Mission Brief (layoutMissionBrief, frame 1184): PANEL_Side 24 a sinistra di PANEL_Menu, bordi superiori
            // allineati, PANEL_Menu resta centrato sul canvas (canvasHint Figma). Il pannello sporge a sinistra del
            // rettangolo 840 del canvas: in World Space nessun ritaglio, e i raycaster non controllano i bordi del canvas.
            if (id == UIScreenId.MissionBrief)
            {
                GameObject sideGo = GetOrCreateChildPrefabInstance(rect, SidePanelBuilder.PREFAB_PATH, SidePanelBuilder.ROOT_NAME);
                sideGo.transform.SetSiblingIndex(0);
                var side = sideGo.GetComponent<SidePanel>();
                BuildSandboxCriteria(side.ContentRoot, theme, strings);
                SidePanelBuilder.ConfigureInstance(side, SidePanel.DEFAULT_TITLE_KEY, -1f, (RectTransform)panelGo.transform);
            }
        }

        /// <summary>
        /// DATI FINTI SOLO PER LA SANDBOX: 3 righe segnaposto dei criteri di completamento sotto ContentRoot di PANEL_Side
        /// (icona 24 color.text.on-dark + etichetta Body color.text.on-green, gap 6, alta 24 come Figma 10:390).
        /// Testi da chiavi esistenti ("Positivo" / "Parziale" / "Negativo"): le frasi vere sono dati di SO_Mission_*
        /// e la riga vera è un componente del passo 3/4. Posizioni verticali calcolate da SidePanel.RefreshLayout.
        /// </summary>
        private static void BuildSandboxCriteria(RectTransform content, UITheme theme, UIStrings strings)
        {
            var rows = new[]
            {
                (Name: "LBL_SandboxCriterionPositive", Icon: "ICN_CheckCircle", Key: "ui.outcome.positive_short"),
                (Name: "LBL_SandboxCriterionPartial", Icon: "ICN_Contrast", Key: "ui.outcome.partial_short"),
                (Name: "LBL_SandboxCriterionNegative", Icon: "ICN_Cancel", Key: "ui.outcome.negative_short"),
            };
            float rowHeight = theme.GetNumber(NumberTokens.SPACE_XL); // 24 = icona 24 (Figma: righe alte 24)
            float iconGap = theme.GetNumber(NumberTokens.SIDE_PANEL_ROW_GAP);
            for (int i = 0; i < rows.Length; i++)
            {
                if (!strings.Contains(rows[i].Key)) Debug.LogError($"[SI81 UI] Chiave \"{rows[i].Key}\" assente in SO_UIStrings_It.");
                RectTransform row = GetOrCreateRectChild(content, rows[i].Name);
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.sizeDelta = new Vector2(0f, rowHeight);
                row.localScale = Vector3.one;
                row.localRotation = Quaternion.identity;
                row.SetSiblingIndex(i);

                RectTransform icon = GetOrCreateRectChild(row, rows[i].Icon);
                icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0f, 0.5f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(rowHeight, rowHeight);
                icon.localScale = Vector3.one;
                var image = GetOrAdd<UnityEngine.UI.Image>(icon.gameObject);
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconBuilder.ICONS_FOLDER + "/" + rows[i].Icon + ".png");
                if (image.sprite == null) Debug.LogError($"[SI81 UI] Icona assente: {rows[i].Icon}. Eseguire SI81/UI/Build Theme.");
                image.preserveAspect = true;
                image.color = theme.TextOnDark;
                image.raycastTarget = false;

                RectTransform label = GetOrCreateRectChild(row, "LBL_Text");
                label.anchorMin = Vector2.zero;
                label.anchorMax = Vector2.one;
                label.pivot = new Vector2(0f, 0.5f);
                label.offsetMin = new Vector2(rowHeight + iconGap, 0f);
                label.offsetMax = Vector2.zero;
                label.localScale = Vector3.one;
                TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(label.gameObject);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.raycastTarget = false;
                GetOrAdd<UIStringText>(label.gameObject).Configure(strings, rows[i].Key, theme, TextStyleIds.BODY, ColorTokens.TEXT_ON_GREEN);
                EditorUtility.SetDirty(tmp);
            }
        }

        /// <summary>
        /// Restituisce l'istanza di prefab figlia diretta con il nome dato, oppure la crea sotto il genitore.
        /// Segnala con un errore un figlio con lo stesso nome che non è un'istanza del prefab.
        /// </summary>
        internal static GameObject GetOrCreateChildPrefabInstance(Transform parent, string prefabPath, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            foreach (Transform child in parent)
            {
                if (child.name != name) continue;
                if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab && PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                    return child.gameObject;
                Debug.LogError($"[SI81 UI] \"{name}\" sotto \"{parent.name}\" non è un'istanza di {prefabPath}: rinominarlo o eliminarlo.");
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            return instance;
        }

        /// <summary>
        /// Mette in PANEL_Screens l'istanza del prefab di una schermata di pausa/fine missione (disattiva); avvisa se il prefab manca.
        /// </summary>
        private static void PlaceFlowScreen(Transform screens, UIScreenId id)
        {
            string prefabName = RealScreenPrefabName(id);
            string path = ScreensBuilder.ScreenPrefabPath(prefabName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[SI81 UI] Sandbox: prefab {prefabName} assente ({path}), schermata non aggiunta (eseguire SI81/UI/Build HUD).");
                return;
            }
            GameObject instance = GetOrCreateChildPrefabInstance(screens, path, prefabName);
            instance.SetActive(false);
            instance.transform.SetSiblingIndex((int)id);
        }

        /// <summary>
        /// Crea o aggiorna CANVAS_SandboxDebug: un pulsante per schermata, "indietro" e "radice", collegati a runtime
        /// da UISandboxDebugSwitcher.
        /// </summary>
        private static void BuildDebugCanvas(Scene scene, UITheme theme, UIStrings strings)
        {
            float buttonHeight = theme.TargetMinSize + theme.GetNumber(NumberTokens.SPACE_S);
            float gap = theme.GetNumber(NumberTokens.SPACE_S);
            float padding = theme.GetNumber(NumberTokens.SPACE_L);
            int buttonCount = SCREEN_TITLE_KEYS.Count + 2;
            float height = padding * 2f + buttonCount * buttonHeight + (buttonCount - 1) * gap;

            GameObject canvasGo = GetOrCreateRoot(scene, "CANVAS_SandboxDebug");
            if (canvasGo.GetComponent<RectTransform>() == null) canvasGo.AddComponent<RectTransform>();
            float distance = new Vector2(DEBUG_CANVAS_POSITION.x, DEBUG_CANVAS_POSITION.z).magnitude;
            ConfigureWorldCanvas(canvasGo, theme, new Vector2(DEBUG_CANVAS_WIDTH, height), DEBUG_CANVAS_POSITION,
                Quaternion.Euler(0f, DEBUG_CANVAS_YAW, 0f), UITheme.GetCanvasScale(distance));

            var panel = GetOrAdd<UnityEngine.UI.Image>(canvasGo);
            panel.color = theme.SurfacePanel;
            panel.raycastTarget = true;

            var screenButtons = new List<UISandboxDebugSwitcher.ScreenButton>();
            int index = 0;
            foreach (KeyValuePair<UIScreenId, string> entry in SCREEN_TITLE_KEYS)
            {
                UnityEngine.UI.Button button = BuildDebugButton(canvasGo.transform, "BTN_Debug" + entry.Key, index++, buttonHeight, gap, padding, theme);
                SetButtonLabel(button, entry.Value, theme, strings);
                screenButtons.Add(new UISandboxDebugSwitcher.ScreenButton { Button = button, Screen = entry.Key });
            }
            UnityEngine.UI.Button back = BuildDebugButton(canvasGo.transform, "BTN_DebugBack", index++, buttonHeight, gap, padding, theme);
            SetButtonIcon(back, AssetDatabase.LoadAssetAtPath<Sprite>(BACK_ICON), theme);
            UnityEngine.UI.Button home = BuildDebugButton(canvasGo.transform, "BTN_DebugGoHome", index, buttonHeight, gap, padding, theme);
            SetButtonLabel(home, GO_HOME_KEY, theme, strings);

            GetOrAdd<UISandboxDebugSwitcher>(canvasGo).Configure(screenButtons, back, home);
        }

        /// <summary>
        /// Crea o aggiorna un pulsante di debug in colonna (posizione manuale, nessun Layout Group).
        /// Colori della transizione dai token del tema.
        /// </summary>
        private static UnityEngine.UI.Button BuildDebugButton(Transform parent, string name, int index, float height, float gap, float padding, UITheme theme)
        {
            RectTransform rect = GetOrCreateRectChild(parent, name);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -padding - index * (height + gap));
            rect.sizeDelta = new Vector2(-2f * padding, height);
            rect.localScale = Vector3.one;
            rect.SetSiblingIndex(index);

            var image = GetOrAdd<UnityEngine.UI.Image>(rect.gameObject);
            image.color = theme.TextOnDark; // bianco: la tinta della transizione dà il colore finale
            image.raycastTarget = true;
            var button = GetOrAdd<UnityEngine.UI.Button>(rect.gameObject);
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = theme.TileNeutral;
            colors.highlightedColor = theme.StateActive;
            colors.pressedColor = theme.BrandLight;
            colors.selectedColor = theme.TileNeutral;
            colors.disabledColor = theme.ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = theme.Motion.HoverDuration;
            button.colors = colors;
            UnityEngine.UI.Navigation navigation = button.navigation;
            navigation.mode = UnityEngine.UI.Navigation.Mode.None;
            button.navigation = navigation;
            return button;
        }

        /// <summary>
        /// Crea o aggiorna l'etichetta LBL_DebugLabel di un pulsante di debug con il testo della chiave.
        /// </summary>
        private static void SetButtonLabel(UnityEngine.UI.Button button, string key, UITheme theme, UIStrings strings)
        {
            if (!strings.Contains(key)) Debug.LogError($"[SI81 UI] Chiave \"{key}\" assente in SO_UIStrings_It.");
            RectTransform label = GetOrCreateRectChild(button.transform, "LBL_DebugLabel", "LBL_Label");
            Stretch(label);
            float inset = theme.GetNumber(NumberTokens.SPACE_M);
            label.offsetMin = new Vector2(inset, 0f);
            label.offsetMax = new Vector2(-inset, 0f);
            TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(label.gameObject);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            GetOrAdd<UIStringText>(label.gameObject).Configure(strings, key, theme, TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY);
            EditorUtility.SetDirty(tmp);
        }

        /// <summary>
        /// Crea o aggiorna l'icona ICN_ di un pulsante di debug (sprite bianco colorato dal tema).
        /// </summary>
        private static void SetButtonIcon(UnityEngine.UI.Button button, Sprite sprite, UITheme theme)
        {
            if (sprite == null) Debug.LogError($"[SI81 UI] Icona assente: {BACK_ICON}. Eseguire SI81/UI/Build Theme.");
            RectTransform icon = GetOrCreateRectChild(button.transform, sprite != null ? sprite.name : "ICN_ArrowBack");
            float size = theme.TargetMinSize * 0.8f;
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = Vector2.zero;
            icon.sizeDelta = new Vector2(size, size);
            icon.localScale = Vector3.one;
            var image = GetOrAdd<UnityEngine.UI.Image>(icon.gameObject);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = theme.TextPrimary;
            image.raycastTarget = false;
        }

        // =====================================================================
        // Utilità
        // =====================================================================

        /// <summary>
        /// Restituisce l'istanza di prefab con il nome dato nella scena, oppure la crea.
        /// Segnala con un errore un oggetto con lo stesso nome che non è un'istanza del prefab.
        /// </summary>
        internal static GameObject GetOrCreatePrefabInstance(Scene scene, string prefabPath, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != name) continue;
                if (PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab) return root;
                Debug.LogError($"[SI81 UI] \"{name}\" nella scena non è un'istanza di {prefabPath}: rinominarlo o eliminarlo.");
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            return instance;
        }

        /// <summary>
        /// Registra come override dell'istanza di prefab le modifiche fatte da codice a tutti i GameObject e componenti
        /// sotto <paramref name="root"/>, così restano salvate nella scena.
        /// </summary>
        internal static void RecordInstanceOverrides(GameObject root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                foreach (Component c in t.GetComponents<Component>())
                    if (c != null) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
        }

        /// <summary>
        /// Restituisce l'oggetto radice con il nome dato nella scena, oppure lo crea.
        /// </summary>
        internal static GameObject GetOrCreateRoot(Scene scene, string name, string legacyName = null)
        {
            GameObject found = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (found != null) return found;
            // Migrazione: un oggetto con il vecchio nome viene rinominato, non duplicato.
            if (legacyName != null) found = scene.GetRootGameObjects().FirstOrDefault(g => g.name == legacyName);
            if (found != null)
            {
                found.name = name;
                return found;
            }
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        /// <summary>
        /// Restituisce il figlio diretto con il nome dato, oppure lo crea.
        /// </summary>
        internal static Transform GetOrCreateChild(Transform parent, string name, string legacyName = null)
        {
            Transform child = parent.Find(name);
            if (child == null && legacyName != null) child = RenameLegacyChild(parent, name, legacyName);
            if (child != null) return child;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>
        /// Migrazione dei nomi: se esiste un figlio con il vecchio nome lo rinomina (stesso oggetto, stesso fileID, nessun duplicato).
        /// </summary>
        /// <returns>Il figlio rinominato, o null se non c'era.</returns>
        private static Transform RenameLegacyChild(Transform parent, string name, string legacyName)
        {
            Transform legacy = parent.Find(legacyName);
            if (legacy == null) return null;
            legacy.name = name;
            return legacy;
        }

        /// <summary>
        /// Restituisce il figlio diretto UI (RectTransform) con il nome dato, oppure lo crea.
        /// </summary>
        internal static RectTransform GetOrCreateRectChild(Transform parent, string name, string legacyName = null)
        {
            Transform child = parent.Find(name);
            if (child == null && legacyName != null) child = RenameLegacyChild(parent, name, legacyName);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.layer = LayerMask.NameToLayer("UI");
                go.transform.SetParent(parent, false);
                return (RectTransform)go.transform;
            }
            if (child is RectTransform rect) return rect;
            return child.gameObject.AddComponent<RectTransform>();
        }

        /// <summary>
        /// Restituisce il componente del GameObject, aggiungendolo se manca (mai doppioni).
        /// </summary>
        internal static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        /// <summary>
        /// Allarga un RectTransform a tutto il genitore.
        /// </summary>
        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Azzera posizione e rotazione locali e porta la scala a 1.
        /// </summary>
        internal static void ResetLocal(Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        /// <summary>
        /// Restituisce la mesh integrata di una primitiva (sfera, quad) senza lasciare oggetti temporanei.
        /// </summary>
        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        /// <summary>
        /// Crea una cartella del progetto (e le cartelle madri) se non esiste.
        /// </summary>
        internal static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
