using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SI81.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Builder del kit scenario (Blocco D). Menu SI81/Scenari/Crea kit scenario: crea o aggiorna SCN_Core (rig segnaposto condiviso +
    /// MNG_UI reale + MNG_HUD), SCN_ScenarioTemplate con SO_Mission_Template (da copiare), lo scenario finto SCN_MockScenario con
    /// SO_Mission_MockScenario (aggiunto al catalogo, per provare il percorso intero senza grafica) e le Build Settings
    /// (SCN_Core per prima). Menu SI81/Scenari/Nuovo scenario...: un team crea il proprio SCN_&lt;Titolo&gt; + SO_Mission_&lt;Titolo&gt; da copia del
    /// template, nella cartella del gruppo, già nel catalogo e nelle Build Settings. Sicuro da rieseguire: GUID di scene e asset restano.
    /// </summary>
    internal static class ScenarioKitBuilder
    {
        // =====================================================================
        // Percorsi e costanti
        // =====================================================================

        /// <summary>Cartella del kit scenario.</summary>
        internal const string SCENARIO_FOLDER = "Assets/_Project/Shared/Level/ScenarioTemplate";
        /// <summary>Cartella dello scenario finto.</summary>
        internal const string MOCK_FOLDER = BaseBuilder.SCENES_FOLDER + "/Mock";
        /// <summary>Scena persistente con rig, MNG_UI e MNG_HUD.</summary>
        internal const string CORE_SCENE = BaseBuilder.SCENES_FOLDER + "/SCN_Core.unity";
        /// <summary>Scena modello da copiare.</summary>
        internal const string TEMPLATE_SCENE = SCENARIO_FOLDER + "/SCN_ScenarioTemplate.unity";
        /// <summary>Missione modello (non nel catalogo).</summary>
        internal const string TEMPLATE_MISSION = SCENARIO_FOLDER + "/SO_Mission_Template.asset";
        /// <summary>Scena dello scenario finto.</summary>
        internal const string MOCK_SCENE = MOCK_FOLDER + "/SCN_MockScenario.unity";
        /// <summary>Missione dello scenario finto.</summary>
        internal const string MOCK_MISSION = MOCK_FOLDER + "/SO_Mission_MockScenario.asset";
        /// <summary>Prefab MNG_HUD del Blocco C.</summary>
        internal const string HUD_PREFAB = BaseBuilder.PREFABS_FOLDER + "/HUD/MNG_HUD.prefab";
        /// <summary>Cartella dei gruppi di lavoro.</summary>
        internal const string GROUPS_FOLDER = "Assets/_Project/Groups";
        /// <summary>Nome di SCN_Core.</summary>
        internal const string CORE_NAME = "SCN_Core";
        /// <summary>Marcatore dei campi da compilare nel template (il validatore lo cerca).</summary>
        internal const string TODO_MARKER = "[DA COMPILARE]";

        /// <summary>Rig condiviso di SCN_Core (Meta XR Core SDK).</summary>
        private const string OVR_CAMERA_RIG_PREFAB = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";
        /// <summary>Modelli dei controller Meta (OVRControllerHelper sceglie il modello giusto per il visore).</summary>
        private const string OVR_CONTROLLER_PREFAB = "Packages/com.meta.xr.sdk.core/Prefabs/OVRControllerPrefab.prefab";
        /// <summary>OVRInput.Controller.LTouch.</summary>
        private const int OVR_CONTROLLER_LTOUCH = 1;
        /// <summary>OVRInput.Controller.RTouch.</summary>
        private const int OVR_CONTROLLER_RTOUCH = 2;

        // =====================================================================
        // Menu
        // =====================================================================

        /// <summary>
        /// Menu SI81/Scenari/Crea kit scenario: esegue <see cref="BuildKit"/> e scrive il riepilogo in Console.
        /// </summary>
        [MenuItem("SI81/Scenari/Crea kit scenario", false, 100)]
        private static void BuildKitMenu()
        {
            Debug.Log(BuildKit());
        }

        /// <summary>
        /// Apre la finestra "Nuovo scenario".
        /// </summary>
        [MenuItem("SI81/Scenari/Nuovo scenario...", false, 101)]
        private static void NewScenarioMenu()
        {
            NewScenarioWindow.Open();
        }

        // =====================================================================
        // Kit
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna tutto il kit: asset missione, SCN_Core, template, scenario finto, catalogo e Build Settings.
        /// </summary>
        /// <returns>Riepilogo in italiano (usabile anche da CLI).</returns>
        internal static string BuildKit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "[SI81 Scenari] Crea kit scenario non eseguibile in Play Mode.";
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            var catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(ScreensBuilder.CATALOG_ASSET);
            if (theme == null || catalog == null)
                return "[SI81 Scenari] Kit interrotto: mancano SO_UITheme o SO_MissionCatalog. Esegui prima SI81/UI/Build Base.";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return "[SI81 Scenari] Kit interrotto: scene con modifiche non salvate.";

            BaseBuilder.EnsureFolder(SCENARIO_FOLDER);
            BaseBuilder.EnsureFolder(MOCK_FOLDER);
            var report = new StringBuilder("[SI81 Scenari] Kit scenario completato.\n");

            // --- Missioni (prima delle scene: le scene le referenziano). ---
            MissionDefinition template = BuildTemplateMission();
            MissionDefinition mock = BuildMockMission();
            catalog.Missions.RemoveAll(m => m == null);
            if (!catalog.Missions.Contains(mock)) catalog.Missions.Add(mock);
            catalog.Missions.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            report.AppendLine("SO_Mission_Template (Id 00, fuori dal catalogo) e SO_Mission_MockScenario (Id 99, nel catalogo: compare in Home, pagina 2 oltre 8 scenari).");

            // --- Scene. ---
            report.AppendLine(BuildCoreScene(theme, catalog));
            report.AppendLine(BuildScenarioScene(TEMPLATE_SCENE, typeof(ScenarioController), AssetDatabase.LoadAssetAtPath<MissionDefinition>(TEMPLATE_MISSION), true));
            report.AppendLine(BuildScenarioScene(MOCK_SCENE, typeof(MockScenario), AssetDatabase.LoadAssetAtPath<MissionDefinition>(MOCK_MISSION), false));

            // --- Build Settings. ---
            report.AppendLine(UpdateBuildSettings(CORE_SCENE, MOCK_SCENE));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return report.ToString().TrimEnd();
        }

        // =====================================================================
        // Missioni
        // =====================================================================

        /// <summary>
        /// Crea o riallinea SO_Mission_Template: tutti i campi con istruzioni segnaposto in italiano, marcate "[DA COMPILARE]".
        /// </summary>
        private static MissionDefinition BuildTemplateMission()
        {
            var m = ScreensBuilder.LoadOrCreateAsset<MissionDefinition>(TEMPLATE_MISSION, out _);
            m.Id = "00";
            m.Title = "Titolo scenario";
            m.Context = TODO_MARKER + " Ruolo del giocatore e situazione iniziale, massimo 280 caratteri. Scrivi chi è il giocatore, dove si trova e che cosa sta succedendo.";
            m.Icon = ComponentsBuilder.LoadIcon("ICN_School");
            m.Image = null;
            m.GradientTop = new Color(0.20f, 0.45f, 0.35f, 1f);
            m.GradientBottom = new Color(0.45f, 0.75f, 0.55f, 1f);
            m.Complexity = 1;
            m.SceneName = "SCN_ScenarioTemplate";
            m.Objectives = new List<string>
            {
                TODO_MARKER + " Obiettivo 1",
                TODO_MARKER + " Obiettivo 2",
                TODO_MARKER + " Obiettivo 3"
            };
            m.Criteria = new List<CompletionCriterion>
            {
                new CompletionCriterion { Outcome = OutcomeType.Positive, Text = TODO_MARKER + " Cosa deve fare il giocatore per l'esito Positivo." },
                new CompletionCriterion { Outcome = OutcomeType.Partial, Text = TODO_MARKER + " Cosa porta a un esito Parziale." },
                new CompletionCriterion { Outcome = OutcomeType.Negative, Text = TODO_MARKER + " Cosa porta a un esito Negativo." }
            };
            m.Levels = ScreensBuilder.DefaultLevels();
            m.IsTraining = false;
            m.DesignerNote = "Modello da copiare con SI81/Scenari/Nuovo scenario... Non va nel catalogo. Sostituisci ogni campo " + TODO_MARKER + ".";
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Crea o riallinea SO_Mission_MockScenario (Id 99): scenario finto di circa 30 secondi con contenuti segnaposto.
        /// </summary>
        private static MissionDefinition BuildMockMission()
        {
            var m = ScreensBuilder.LoadOrCreateAsset<MissionDefinition>(MOCK_MISSION, out _);
            m.Id = "99";
            m.Title = "Mock Scenario";
            m.Context = "Ruolo: collega in un reparto. Un quadro elettrico emette fumo: devi decidere in pochi secondi. Scenario di prova (esempio), senza grafica, per provare il percorso completo della UI.";
            m.Icon = ComponentsBuilder.LoadIcon("ICN_FireExtinguisher");
            m.GradientTop = new Color(0.35f, 0.35f, 0.55f, 1f);
            m.GradientBottom = new Color(0.60f, 0.65f, 0.85f, 1f);
            m.Complexity = 1;
            m.SceneName = "SCN_MockScenario";
            m.Objectives = new List<string> { "Controlla l'area", "Decidi come intervenire", "Metti in sicurezza" };
            m.Criteria = new List<CompletionCriterion>
            {
                new CompletionCriterion { Outcome = OutcomeType.Positive, Text = "Dai l'allarme prima di agire (esempio)." },
                new CompletionCriterion { Outcome = OutcomeType.Partial, Text = "Spegni il quadro da solo, senza allarme (esempio)." },
                new CompletionCriterion { Outcome = OutcomeType.Negative, Text = "Non decidi in tempo (esempio)." }
            };
            m.Levels = ScreensBuilder.DefaultLevels();
            m.IsTraining = false;
            m.DesignerNote = ScreensBuilder.SAMPLE_MARKER + " Scenario finto per provare il flusso: va tolto dal catalogo prima della consegna.";
            EditorUtility.SetDirty(m);
            return m;
        }

        // =====================================================================
        // SCN_Core
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna SCN_Core: rig segnaposto (MNG_CoreRig con testa, due controller e puntatori), istanza di MNG_UI con servizi
        /// reali (useMocks = false) e tutte le schermate disponibili, istanza di MNG_HUD (se il prefab del Blocco C esiste).
        /// </summary>
        private static string BuildCoreScene(UITheme theme, MissionCatalog catalog)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BaseBuilder.MNG_UI_PREFAB) == null)
                return "SCN_Core: NON creata, manca il prefab MNG_UI. Esegui prima SI81/UI/Build Base.";
            var pointerMaterial = AssetDatabase.LoadAssetAtPath<Material>(BaseBuilder.POINTER_MATERIAL);
            var reticleMaterial = AssetDatabase.LoadAssetAtPath<Material>(BaseBuilder.RETICLE_MATERIAL);
            if (pointerMaterial == null || reticleMaterial == null)
                return "SCN_Core: NON creata, mancano MAT_UIPointer / MAT_UIPointerReticle. Esegui prima SI81/UI/Build Base.";

            bool exists = File.Exists(Path.GetFullPath(CORE_SCENE));
            Scene scene = exists ? EditorSceneManager.OpenScene(CORE_SCENE, OpenSceneMode.Single) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // L'apertura Single può scaricare gli asset non referenziati: li ricarichiamo dal percorso.
            theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.THEME_ASSET);
            catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(ScreensBuilder.CATALOG_ASSET);
            pointerMaterial = AssetDatabase.LoadAssetAtPath<Material>(BaseBuilder.POINTER_MATERIAL);
            reticleMaterial = AssetDatabase.LoadAssetAtPath<Material>(BaseBuilder.RETICLE_MATERIAL);
            var report = new StringBuilder();

            // Ambiente scuro: la scena del menu deve restare poco luminosa (CLAUDE.md 4.4).
            Color backdrop = theme.GetColor(ColorTokens.BACKDROP_BOTTOM);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = backdrop;
            RenderSettings.fog = false;

            // --- Rig condiviso: OVRCameraRig di Meta XR Core SDK (testa e controller tracciati, modelli dei controller). ---
            // È l'UNICO rig dell'app: le scene di scenario non devono averne un altro (CLAUDE.md 4.11).
            GameObject rig = BaseBuilder.GetOrCreateRoot(scene, "MNG_CoreRig");
            BaseBuilder.ResetLocal(rig.transform);
            Transform oldOffset = rig.transform.Find("MNG_RigOffset");
            if (oldOffset != null) Object.DestroyImmediate(oldOffset.gameObject); // rig segnaposto delle versioni precedenti
            var ovrRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OVR_CAMERA_RIG_PREFAB);
            if (ovrRigPrefab == null)
                return $"SCN_Core: NON creata, manca {OVR_CAMERA_RIG_PREFAB} (Meta XR Core SDK).";
            GameObject ovrRig = BaseBuilder.GetOrCreateChildPrefabInstance(rig.transform, OVR_CAMERA_RIG_PREFAB, "OVRCameraRig");
            BaseBuilder.ResetLocal(ovrRig.transform);
            SetEnumByName(ovrRig, "OVRManager", "_trackingOriginType", "FloorLevel");
            Transform origin = FindDeep(ovrRig.transform, "TrackingSpace");
            Transform head = FindDeep(ovrRig.transform, "CenterEyeAnchor");
            Transform left = FindDeep(ovrRig.transform, "LeftControllerAnchor");
            Transform right = FindDeep(ovrRig.transform, "RightControllerAnchor");
            if (origin == null || head == null || left == null || right == null)
                return "SCN_Core: NON creata, OVRCameraRig senza TrackingSpace / CenterEyeAnchor / Left-RightControllerAnchor.";
            Camera camera = head.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backdrop;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            BuildControllerModel(left, OVR_CONTROLLER_LTOUCH);
            BuildControllerModel(right, OVR_CONTROLLER_RTOUCH);
            BuildPointer(left.gameObject, UIPointerHand.Left, theme, pointerMaterial, reticleMaterial);
            BuildPointer(right.gameObject, UIPointerHand.Right, theme, pointerMaterial, reticleMaterial);
            BaseBuilder.GetOrAdd<CoreRig>(rig).Configure(head, origin, left, right);
            Transform offset = origin;

            // --- MNG_UI con servizi reali. ---
            GameObject ui = BaseBuilder.GetOrCreatePrefabInstance(scene, BaseBuilder.MNG_UI_PREFAB, BaseBuilder.MNG_UI_NAME);
            var inputSo = new SerializedObject(ui.GetComponentInChildren<UIInputSetup>(true));
            inputSo.FindProperty("xrTrackingOrigin").objectReferenceValue = offset;
            inputSo.ApplyModifiedPropertiesWithoutUndo();
            var managerSo = new SerializedObject(ui.GetComponent<UIScreenManager>());
            managerSo.FindProperty("head").objectReferenceValue = head;
            managerSo.ApplyModifiedPropertiesWithoutUndo();
            var bootstrapSo = new SerializedObject(ui.GetComponent<UIServicesBootstrap>());
            bootstrapSo.FindProperty("useMocks").boolValue = false;
            bootstrapSo.FindProperty("menuSceneName").stringValue = string.Empty;
            if (bootstrapSo.FindProperty("catalog").objectReferenceValue == null) bootstrapSo.FindProperty("catalog").objectReferenceValue = catalog;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine(PlaceScreens(ui));

            // --- MNG_HUD (Blocco C). ---
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB) != null)
            {
                BaseBuilder.GetOrCreatePrefabInstance(scene, HUD_PREFAB, "MNG_HUD");
            }
            else
            {
                Debug.LogError($"[SI81 Scenari] Prefab MNG_HUD non trovato in {HUD_PREFAB}: SCN_Core è stata creata SENZA HUD. Integra il Blocco C ed esegui di nuovo SI81/Scenari/Crea kit scenario.");
                report.AppendLine("ATTENZIONE: MNG_HUD assente (Blocco C non integrato): SCN_Core senza HUD.");
            }

            rig.transform.SetSiblingIndex(0);
            ui.transform.SetSiblingIndex(1);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, CORE_SCENE);
            report.Append(saved ? $"SCN_Core: {(exists ? "aggiornata" : "creata")} ({CORE_SCENE})." : "SCN_Core: ERRORE nel salvataggio.");
            return report.ToString().TrimEnd();
        }

        /// <summary>
        /// Mette sotto PANEL_Screens di MNG_UI un'istanza di ogni schermata disponibile in Prefabs/Screens (ordine fratelli = valore di
        /// UIScreenId, solo Home attiva). Le schermate mancanti vengono elencate (le 4 del Blocco C compaiono quando il loro builder è stato eseguito).
        /// </summary>
        private static string PlaceScreens(GameObject ui)
        {
            Transform screens = ui.transform.Find(BaseBuilder.MENU_CANVAS_NAME + "/" + BaseBuilder.SCREENS_NAME);
            if (screens == null) return "ATTENZIONE: PANEL_Screens non trovato in MNG_UI.";
            var names = new Dictionary<UIScreenId, string>
            {
                { UIScreenId.Home, "PANEL_Home" },
                { UIScreenId.MissionBrief, "PANEL_MissionBrief" },
                { UIScreenId.Stats, "PANEL_Stats" },
                { UIScreenId.Profile, "PANEL_Profile" },
                { UIScreenId.Settings, "PANEL_Settings" },
                { UIScreenId.TrainingBrief, "PANEL_TrainingBrief" },
                { UIScreenId.Pause, "PANEL_Pause" },
                { UIScreenId.Debrief, "PANEL_Debrief" },
                { UIScreenId.ScoreReplay, "PANEL_ScoreReplay" },
                { UIScreenId.TrainingComplete, "PANEL_TrainingComplete" }
            };
            var missing = new List<string>();
            foreach (KeyValuePair<UIScreenId, string> entry in names)
            {
                string path = ScreensBuilder.ScreenPrefabPath(entry.Value);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    missing.Add(entry.Value);
                    continue;
                }
                GameObject instance = BaseBuilder.GetOrCreateChildPrefabInstance(screens, path, entry.Value);
                instance.SetActive(entry.Key == UIScreenId.Home);
                instance.transform.SetSiblingIndex(Mathf.Min((int)entry.Key, screens.childCount - 1));
            }
            return missing.Count == 0
                ? $"Schermate in SCN_Core: {names.Count}/{names.Count}."
                : "ATTENZIONE: schermate mancanti in SCN_Core (builder non eseguito): " + string.Join(", ", missing) + ".";
        }

        /// <summary>
        /// Cerca un discendente per nome (solo per le parti note del prefab OVRCameraRig di Meta).
        /// </summary>
        /// <param name="root">Radice da cui cercare.</param>
        /// <param name="name">Nome esatto del figlio.</param>
        /// <returns>Il Transform trovato, o null.</returns>
        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>
        /// Imposta un campo enum serializzato di un componente (cercato per nome del tipo) usando il nome del valore,
        /// così il builder non dipende dall'assembly di Meta.
        /// </summary>
        /// <param name="go">GameObject con il componente.</param>
        /// <param name="typeName">Nome del tipo del componente.</param>
        /// <param name="field">Nome del campo serializzato.</param>
        /// <param name="valueName">Nome del valore enum.</param>
        private static void SetEnumByName(GameObject go, string typeName, string field, string valueName)
        {
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null || c.GetType().Name != typeName) continue;
                var so = new SerializedObject(c);
                SerializedProperty p = so.FindProperty(field);
                int index = p != null ? System.Array.IndexOf(p.enumNames, valueName) : -1;
                if (index < 0) { Debug.LogWarning($"[SI81 Scenari] {typeName}.{field} = {valueName} non impostabile."); return; }
                if (p.enumValueIndex != index) { p.enumValueIndex = index; so.ApplyModifiedPropertiesWithoutUndo(); }
                return;
            }
        }

        /// <summary>
        /// Mette sotto l'ancora del controller il modello del controller Meta (OVRControllerPrefab) per la mano indicata:
        /// il giocatore vede i controller in mano.
        /// </summary>
        /// <param name="anchor">LeftControllerAnchor o RightControllerAnchor.</param>
        /// <param name="controller">Valore OVRInput.Controller (LTouch = 1, RTouch = 2).</param>
        private static void BuildControllerModel(Transform anchor, int controller)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OVR_CONTROLLER_PREFAB) == null)
            {
                Debug.LogWarning($"[SI81 Scenari] {OVR_CONTROLLER_PREFAB} non trovato: niente modelli dei controller.");
                return;
            }
            GameObject model = BaseBuilder.GetOrCreateChildPrefabInstance(anchor, OVR_CONTROLLER_PREFAB, "MESH_Controller");
            BaseBuilder.ResetLocal(model.transform);
            foreach (Component c in model.GetComponents<Component>())
            {
                if (c == null || c.GetType().Name != "OVRControllerHelper") continue;
                var so = new SerializedObject(c);
                SerializedProperty p = so.FindProperty("m_controller");
                if (p != null && p.intValue != controller) { p.intValue = controller; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        /// <summary>
        /// Crea o aggiorna il puntatore UI su un'ancora controller: LineRenderer, reticolo e UIPointer.
        /// </summary>
        private static void BuildPointer(GameObject go, UIPointerHand hand, UITheme theme, Material pointerMaterial, Material reticleMaterial)
        {
            LineRenderer line = BaseBuilder.GetOrAdd<LineRenderer>(go);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPositions(new[] { Vector3.zero, Vector3.zero });
            line.startWidth = line.endWidth = theme.Pointer.RayWidth;
            line.sharedMaterial = pointerMaterial;
            line.numCapVertices = 0;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.enabled = false;

            GameObject reticleGo = BaseBuilder.GetOrCreateChild(go.transform, "VFX_PointerReticle").gameObject;
            reticleGo.transform.localPosition = Vector3.zero;
            reticleGo.transform.localRotation = Quaternion.identity;
            reticleGo.transform.localScale = Vector3.one * theme.Pointer.ReticleDiameter;
            MeshFilter filter = BaseBuilder.GetOrAdd<MeshFilter>(reticleGo);
            if (filter.sharedMesh == null) filter.sharedMesh = GetPrimitiveMesh(PrimitiveType.Quad);
            MeshRenderer reticle = BaseBuilder.GetOrAdd<MeshRenderer>(reticleGo);
            reticle.sharedMaterial = reticleMaterial;
            reticle.shadowCastingMode = ShadowCastingMode.Off;
            reticle.receiveShadows = false;
            reticle.lightProbeUsage = LightProbeUsage.Off;
            reticle.reflectionProbeUsage = ReflectionProbeUsage.Off;
            reticle.enabled = false;

            BaseBuilder.GetOrAdd<UIPointer>(go).Configure(hand, theme, line, reticle);
        }

        /// <summary>
        /// Restituisce la mesh integrata di una primitiva senza lasciare oggetti temporanei.
        /// </summary>
        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        // =====================================================================
        // Scene di scenario (template e finto)
        // =====================================================================

        /// <summary>
        /// Crea o aggiorna una scena di scenario: luce, pavimento, MNG_SpawnPoint, MNG_Scenario (componente + missione), tre LabelAnchor
        /// di esempio e, se richiesto, un innesco di decisione. Mai UI costruita a mano. Gli oggetti che esistono già non vengono toccati.
        /// </summary>
        /// <param name="path">Percorso della scena.</param>
        /// <param name="controllerType">ScenarioController (template) o MockScenario.</param>
        /// <param name="mission">Missione da assegnare.</param>
        /// <param name="withTrigger">true per aggiungere MNG_DecisionTrigger (il finto non lo usa).</param>
        private static string BuildScenarioScene(string path, System.Type controllerType, MissionDefinition mission, bool withTrigger)
        {
            bool exists = File.Exists(Path.GetFullPath(path));
            // Aprire una scena in modalità Single scarica gli asset non referenziati: si ricarica la missione dal percorso dopo l'apertura.
            string missionPath = AssetDatabase.GetAssetPath(mission);
            if (string.IsNullOrEmpty(missionPath)) return System.IO.Path.GetFileNameWithoutExtension(path) + ": ERRORE, missione non valida.";
            Scene scene = exists ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(missionPath);
            PopulateScenarioScene(scene, controllerType, mission, withTrigger);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, path);
            return saved ? $"{Path.GetFileNameWithoutExtension(path)}: {(exists ? "aggiornata" : "creata")} ({path})." : $"{Path.GetFileNameWithoutExtension(path)}: ERRORE nel salvataggio.";
        }

        /// <summary>
        /// Riempie la scena aperta con il contenuto del modello (vedi <see cref="BuildScenarioScene"/>).
        /// </summary>
        private static void PopulateScenarioScene(Scene scene, System.Type controllerType, MissionDefinition mission, bool withTrigger)
        {
            // Luce direzionale (segnaposto di ambiente: il team la sostituisce con la propria illuminazione).
            GameObject light = BaseBuilder.GetOrCreateRoot(scene, "MNG_Light");
            if (light.GetComponent<Light>() == null)
            {
                Light l = light.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Pavimento segnaposto 40 x 40 m, origine = pavimento del rig (altezza 0).
            GameObject floor = GetOrCreatePrimitiveRoot(scene, "MESH_Floor", PrimitiveType.Plane);
            floor.transform.localScale = new Vector3(4f, 1f, 4f);

            // Punto di partenza del giocatore.
            GameObject spawn = BaseBuilder.GetOrCreateRoot(scene, "MNG_SpawnPoint");
            if (!HasBeenPlaced(spawn)) spawn.transform.SetPositionAndRotation(new Vector3(0f, 0f, -2f), Quaternion.identity);

            // Logica dello scenario.
            GameObject manager = BaseBuilder.GetOrCreateRoot(scene, "MNG_Scenario");
            Component controller = manager.GetComponent(controllerType);
            if (controller == null) controller = manager.AddComponent(controllerType);
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("mission").objectReferenceValue = mission;
            controllerSo.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            // Tre etichette di esempio: rischio, invito all'azione, aiuto contestuale.
            AddAnchorExample(scene, "MESH_ExampleRisk", PrimitiveType.Cube, new Vector3(-1.5f, 0.25f, 1.5f), new Vector3(0.5f, 0.5f, 0.5f),
                LabelKind.Risk, "Ciabatta sovraccarica (esempio)", "Staccala dalla presa");
            AddAnchorExample(scene, "MESH_ExampleTool", PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 2f), new Vector3(0.25f, 0.5f, 0.25f),
                LabelKind.Interaction, "Estintore CO2 (esempio)", string.Empty);
            AddAnchorExample(scene, "MESH_ExampleSign", PrimitiveType.Cube, new Vector3(1.5f, 0.5f, 1.5f), new Vector3(0.6f, 1f, 0.1f),
                LabelKind.ContextHelp, "Estintore CO2 (esempio)", "Adatto ai fuochi elettrici,\nnon usare acqua");

            // Innesco di decisione di esempio: avvicinandosi, completa l'obiettivo corrente.
            if (withTrigger)
            {
                GameObject trigger = BaseBuilder.GetOrCreateRoot(scene, "MNG_DecisionTrigger");
                if (!HasBeenPlaced(trigger)) trigger.transform.position = new Vector3(0f, 0f, 0.5f);
                ScenarioTrigger t = BaseBuilder.GetOrAdd<ScenarioTrigger>(trigger);
                if (t.OnEntered.GetPersistentEventCount() == 0 && controller is ScenarioController sc)
                    UnityEventTools.AddVoidPersistentListener(t.OnEntered, sc.CompleteObjective);
                EditorUtility.SetDirty(t);
            }
        }

        /// <summary>
        /// Crea (o ritrova) un oggetto primitivo radice in una scena.
        /// </summary>
        private static GameObject GetOrCreatePrimitiveRoot(Scene scene, string name, PrimitiveType type)
        {
            GameObject found = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (found != null) return found;
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        /// <summary>
        /// true se l'oggetto è stato già spostato dal punto zero (non lo si riposiziona a ogni esecuzione del builder).
        /// </summary>
        private static bool HasBeenPlaced(GameObject go)
        {
            return go.transform.position != Vector3.zero || go.transform.rotation != Quaternion.identity;
        }

        /// <summary>
        /// Crea o ritrova un oggetto di prova con un LabelAnchor del tipo dato. Se esiste già non cambia posizione né testi.
        /// </summary>
        private static void AddAnchorExample(Scene scene, string name, PrimitiveType shape, Vector3 position, Vector3 scale, LabelKind kind, string title, string detail)
        {
            bool existed = scene.GetRootGameObjects().Any(g => g.name == name);
            GameObject go = GetOrCreatePrimitiveRoot(scene, name, shape);
            LabelAnchor anchor = BaseBuilder.GetOrAdd<LabelAnchor>(go);
            if (existed) return;
            go.transform.position = position;
            go.transform.localScale = scale;
            var so = new SerializedObject(anchor);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("title").stringValue = title;
            so.FindProperty("detail").stringValue = detail;
            so.FindProperty("offset").vector3Value = new Vector3(0f, 0.5f / Mathf.Max(0.1f, scale.y) + 0.3f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // Build Settings e catalogo
        // =====================================================================

        /// <summary>
        /// Aggiorna le Build Settings: toglie le voci SampleScene che non esistono più, mette SCN_Core per prima e aggiunge le scene date
        /// (se mancano), lasciando le altre voci com'erano.
        /// </summary>
        /// <param name="scenePaths">Percorsi delle scene da garantire; il primo è SCN_Core (indice 0).</param>
        /// <returns>Riepilogo in italiano.</returns>
        internal static string UpdateBuildSettings(params string[] scenePaths)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int removed = list.RemoveAll(s => s.path.EndsWith("SampleScene.unity") && !File.Exists(Path.GetFullPath(s.path)));

            var added = new List<string>();
            for (int i = 0; i < scenePaths.Length; i++)
            {
                string path = scenePaths[i];
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                int index = list.FindIndex(s => s.path == path);
                if (index < 0)
                {
                    list.Add(new EditorBuildSettingsScene(path, true));
                    added.Add(Path.GetFileNameWithoutExtension(path));
                }
            }
            // SCN_Core sempre per prima.
            int coreIndex = list.FindIndex(s => s.path == CORE_SCENE);
            if (coreIndex > 0)
            {
                EditorBuildSettingsScene core = list[coreIndex];
                list.RemoveAt(coreIndex);
                list.Insert(0, core);
            }
            EditorBuildSettings.scenes = list.ToArray();
            return $"Build Settings: {list.Count} scene, SCN_Core per prima; aggiunte: {(added.Count == 0 ? "nessuna" : string.Join(", ", added))}; " +
                   $"voci SampleScene mancanti rimosse: {removed}.";
        }

        // =====================================================================
        // Nuovo scenario
        // =====================================================================

        /// <summary>
        /// Crea lo scenario di un team: copia SCN_ScenarioTemplate e SO_Mission_Template nella cartella del gruppo
        /// (Assets/_Project/Groups/&lt;gruppo&gt;/Scenari/&lt;Titolo&gt;/), collega la missione, la aggiunge al catalogo e la scena alle Build Settings.
        /// </summary>
        /// <param name="rawTitle">Titolo come lo scrive il team (es. "Fire Starter"); l'asset usa il PascalCase senza spazi.</param>
        /// <param name="id">Numero dello scenario "01".."12".</param>
        /// <param name="groupFolderName">Nome della cartella del gruppo (es. G_Marius).</param>
        /// <param name="error">Messaggio d'errore in italiano se la creazione non è possibile.</param>
        /// <returns>true se creato.</returns>
        internal static bool CreateScenario(string rawTitle, string id, string groupFolderName, out string error)
        {
            error = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { error = "Non disponibile in Play Mode."; return false; }
            if (string.IsNullOrWhiteSpace(rawTitle)) { error = "Scrivi il titolo dello scenario (es. Fire Starter)."; return false; }
            string pascal = ToPascalCase(rawTitle);
            if (string.IsNullOrEmpty(pascal)) { error = "Il titolo deve contenere almeno una lettera o cifra."; return false; }
            if (pascal.Any(c => !((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))))
            { error = "Il titolo può contenere solo lettere inglesi (senza accenti), cifre e spazi: i nomi di scena e asset non ammettono accenti, apostrofi, '-', '/' o '&'."; return false; }
            if (!int.TryParse(id, out int number) || number < 1 || number > 12) { error = "Il numero dello scenario deve essere tra 01 e 12."; return false; }
            id = number.ToString("00");
            if (string.IsNullOrEmpty(groupFolderName) || !AssetDatabase.IsValidFolder(GROUPS_FOLDER + "/" + groupFolderName))
            { error = "Scegli la cartella del tuo gruppo (Assets/_Project/Groups/...)."; return false; }

            var catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(ScreensBuilder.CATALOG_ASSET);
            if (catalog == null) { error = "SO_MissionCatalog non trovato: esegui prima SI81/UI/Build Base."; return false; }
            if (AssetDatabase.LoadAssetAtPath<MissionDefinition>(TEMPLATE_MISSION) == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(TEMPLATE_SCENE) == null)
            { error = "Il kit non esiste ancora: esegui prima SI81/Scenari/Crea kit scenario."; return false; }
            MissionDefinition used = catalog.GetById(id);
            if (used != null) { error = $"Il numero {id} è già usato dallo scenario \"{used.name}\"."; return false; }

            string folder = GROUPS_FOLDER + "/" + groupFolderName + "/Scenari/" + pascal;
            string scenePath = folder + "/SCN_" + pascal + ".unity";
            string missionPath = folder + "/SO_Mission_" + pascal + ".asset";
            if (File.Exists(Path.GetFullPath(scenePath)) || File.Exists(Path.GetFullPath(missionPath)))
            { error = $"Esistono già SCN_{pascal} o SO_Mission_{pascal} in {folder}."; return false; }
            if (catalog.Missions.Any(m => m != null && m.SceneName == "SCN_" + pascal))
            { error = $"Il catalogo contiene già uno scenario con scena SCN_{pascal}."; return false; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { error = "Scene con modifiche non salvate."; return false; }

            BaseBuilder.EnsureFolder(folder);
            if (!AssetDatabase.CopyAsset(TEMPLATE_MISSION, missionPath) || !AssetDatabase.CopyAsset(TEMPLATE_SCENE, scenePath))
            { error = "Copia del modello non riuscita: controlla la Console."; return false; }
            AssetDatabase.Refresh();

            // La missione copiata: stessi campi del modello, ma con numero, titolo e scena del team.
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(missionPath);
            mission.Id = id;
            mission.Title = rawTitle.Trim();
            mission.SceneName = "SCN_" + pascal;
            mission.DesignerNote = "Creato da SI81/Scenari/Nuovo scenario... Sostituisci i campi " + TODO_MARKER + ".";
            EditorUtility.SetDirty(mission);

            // La scena copiata punta ancora a SO_Mission_Template: si ricollega a quella nuova.
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ScenarioController controller = root.GetComponent<ScenarioController>();
                if (controller == null) continue;
                var so = new SerializedObject(controller);
                so.FindProperty("mission").objectReferenceValue = mission;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // Catalogo e Build Settings.
            catalog.Missions.RemoveAll(m => m == null);
            catalog.Missions.Add(mission);
            catalog.Missions.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SI81 Scenari] Creato SCN_{pascal} e SO_Mission_{pascal} in {folder}. {UpdateBuildSettings(CORE_SCENE, scenePath)}\n" +
                      "Prossimo passo: compila i campi " + TODO_MARKER + " della missione, poi SI81/Scenari/Valida scenario.");
            Selection.activeObject = mission;
            EditorGUIUtility.PingObject(mission);
            return true;
        }

        /// <summary>
        /// Titolo in PascalCase senza spazi ("fire starter" -> "FireStarter"); gli altri caratteri restano per la convalida.
        /// </summary>
        /// <param name="title">Titolo scritto dal team.</param>
        /// <returns>Nome adatto a SCN_ e SO_Mission_.</returns>
        internal static string ToPascalCase(string title)
        {
            var sb = new StringBuilder();
            foreach (string word in title.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1) sb.Append(word.Substring(1));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Primo numero libero tra 01 e 12 nel catalogo (per precompilare la finestra).
        /// </summary>
        /// <returns>Numero a due cifre, o "01" se il catalogo non c'è.</returns>
        internal static string NextFreeId()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(ScreensBuilder.CATALOG_ASSET);
            for (int i = 1; i <= 12; i++)
            {
                string id = i.ToString("00");
                if (catalog == null || catalog.GetById(id) == null) return id;
            }
            return "01";
        }

        /// <summary>
        /// Finestra "Nuovo scenario": titolo, numero e gruppo, poi crea scena e missione dal modello.
        /// </summary>
        private sealed class NewScenarioWindow : EditorWindow
        {
            // Valori dei campi.
            private string titleField = string.Empty;
            private string idField = "01";
            private string[] groups = new string[0];
            private int groupIndex;
            private string message;
            private MessageType messageType = MessageType.Info;

            /// <summary>
            /// Apre la finestra e carica l'elenco dei gruppi.
            /// </summary>
            internal static void Open()
            {
                var window = GetWindow<NewScenarioWindow>(true, "Nuovo scenario", true);
                window.minSize = new Vector2(380f, 220f);
                window.idField = NextFreeId();
                window.RefreshGroups();
                window.Show();
            }

            /// <summary>
            /// Rilegge le cartelle di Assets/_Project/Groups.
            /// </summary>
            private void RefreshGroups()
            {
                groups = AssetDatabase.IsValidFolder(GROUPS_FOLDER)
                    ? AssetDatabase.GetSubFolders(GROUPS_FOLDER).Select(Path.GetFileName).ToArray()
                    : new string[0];
                groupIndex = Mathf.Clamp(groupIndex, 0, Mathf.Max(0, groups.Length - 1));
            }

            /// <summary>
            /// Disegna i campi e il pulsante di creazione.
            /// </summary>
            private void OnGUI()
            {
                EditorGUILayout.HelpBox("Crea SCN_<Titolo> e SO_Mission_<Titolo> da copia del modello, nella cartella del tuo gruppo, " +
                                        "e li aggiunge al catalogo e alle Build Settings.", MessageType.Info);
                titleField = EditorGUILayout.TextField(new GUIContent("Titolo", "Es. Fire Starter. Solo lettere inglesi, cifre e spazi."), titleField);
                idField = EditorGUILayout.TextField(new GUIContent("Numero (01-12)", "Numero dello scenario nel catalogo."), idField);
                if (groups.Length == 0)
                    EditorGUILayout.HelpBox("Nessuna cartella in " + GROUPS_FOLDER + ".", MessageType.Warning);
                else
                    groupIndex = EditorGUILayout.Popup("Gruppo", groupIndex, groups);

                using (new EditorGUI.DisabledScope(groups.Length == 0))
                {
                    if (GUILayout.Button("Crea scenario", GUILayout.Height(28f)))
                    {
                        if (CreateScenario(titleField, idField, groups[groupIndex], out string error))
                        {
                            Close();
                            return;
                        }
                        message = error;
                        messageType = MessageType.Error;
                    }
                }
                if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
            }
        }
    }
}
