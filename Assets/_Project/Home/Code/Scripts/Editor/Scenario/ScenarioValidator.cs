using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SI81.UI.EditorTools
{
    /// <summary>Gravità di un problema trovato dal validatore degli scenari.</summary>
    internal enum ValidationSeverity
    {
        /// <summary>Informazione: nessuna azione richiesta.</summary>
        Info = 0,
        /// <summary>Avviso: lo scenario funziona ma non rispetta una regola consigliata.</summary>
        Warning = 1,
        /// <summary>Errore: lo scenario non si integra correttamente (da correggere prima della consegna).</summary>
        Error = 2
    }

    /// <summary>Un problema trovato dal validatore: gravità, messaggio italiano (cosa non va + come correggere) e oggetto da selezionare.</summary>
    internal sealed class ValidationIssue
    {
        /// <summary>Gravità.</summary>
        public ValidationSeverity Severity;
        /// <summary>Messaggio in italiano: cosa non va e come correggerlo.</summary>
        public string Message;
        /// <summary>Oggetto da selezionare/evidenziare al clic (asset, GameObject di scena o null).</summary>
        public UnityEngine.Object Context;

        /// <summary>Crea un problema.</summary>
        public ValidationIssue(ValidationSeverity severity, string message, UnityEngine.Object context = null)
        {
            Severity = severity;
            Message = message;
            Context = context;
        }
    }

    /// <summary>
    /// Validatore degli scenari (CLAUDE.md §5, §4.11, §2): controlla l'asset SO_Mission_&lt;Titolo&gt;, la scena SCN_&lt;Titolo&gt;
    /// e che la scena NON contenga UI fatta a mano (Canvas, EventSystem, MNG_UI, HUD_*, PANEL_*). I team lo eseguono da
    /// "SI81/Scenari/Valida scenario" (scena aperta) o "SI81/Scenari/Valida tutti gli scenari" (tutto il catalogo).
    /// Il nucleo <see cref="Validate(MissionDefinition, SceneAsset)"/> non apre finestre: è richiamabile da test e da CI.
    /// </summary>
    internal static class ScenarioValidator
    {
        /// <summary>Limite di caratteri per riga di obiettivo sul pannello da polso (CLAUDE.md §4.10).</summary>
        private const int MAX_OBJECTIVE_CHARS = 30;
        /// <summary>Numero massimo di etichette di rischio visibili insieme (CLAUDE.md §4.11).</summary>
        private const int MAX_RISK_LABELS = 3;
        /// <summary>Raggio (metri) entro cui le etichette di rischio si vedono insieme (distanza massima di visibilità).</summary>
        private const float RISK_CLUSTER_RADIUS = 4f;

        // Nome di asset/scena: PascalCase (lettere e cifre, iniziale maiuscola).
        private static readonly Regex PASCAL = new Regex(@"^[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant);
        // Prefissi di CLAUDE.md §2 (riconosce i nomi "alla SI81").
        private static readonly Regex PREFIXED = new Regex(@"^(CANVAS|BTN|IMG|PANEL|LBL|ICN|HUD|TEX|MAT|MESH|SFX|VFX|SCN|SO|FNT|ANIM|MNG)_", RegexOptions.CultureInvariant);
        // PREFISSO_PascalCase con eventuale variante _PascalCase (BTN_Primary_Hover).
        private static readonly Regex PREFIXED_OK = new Regex(@"^[A-Z]{2,6}_[A-Z][A-Za-z0-9]*(_[A-Z][A-Za-z0-9]*)?$", RegexOptions.CultureInvariant);
        // Componenti comuni che non devono stare in una scena di scenario (vivono in SCN_Core).
        /// <summary>Tipi che vivono solo in SCN_Core e non vanno negli scenari.</summary>
        private static readonly HashSet<string> CORE_ONLY_TYPES = new HashSet<string>
        {
            "UIScreenManager", "HUDService", "HUDRoot", "WorldLabelManager", "WristPanel", "WristAttach", "AlertPresenter",
            "SubtitlePresenter", "DecisionPanel", "OutcomePanel", "UIPointer", "UIWorldRaycaster", "MissionFlowDriver"
        };

        // ------------------------------------------------------------------ Menu

        /// <summary>
        /// Voce di menu: valida la scena aperta e la sua missione (cercata nel ScenarioController o per nome di scena).
        /// </summary>
        [MenuItem("SI81/Scenari/Valida scenario", priority = 60)]
        public static void ValidateOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            var issues = new List<ValidationIssue>();
            MissionDefinition mission = FindMissionForScene(scene);
            if (mission == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error,
                    $"Nessuna missione trovata per la scena aperta \"{scene.name}\". Aggiungi un ScenarioController (oggetto MNG_Scenario) che punti al tuo SO_Mission_<Titolo>, oppure imposta SceneName della missione a \"{scene.name}\"."));
            }
            else
            {
                issues.AddRange(ValidateMissionAsset(mission, LoadCatalog()));
                issues.AddRange(ValidateSceneContent(mission, scene));
            }
            Report($"Scena \"{scene.name}\"", issues, false);
        }

        /// <summary>
        /// Voce di menu: valida ogni missione di SO_MissionCatalog (asset, scena, Build Settings; la scena viene aperta in
        /// additivo e richiusa se non è già aperta).
        /// </summary>
        [MenuItem("SI81/Scenari/Valida tutti gli scenari", priority = 61)]
        public static void ValidateAll()
        {
            Report("Catalogo scenari", ValidateCatalog(true), false);
        }

        /// <summary>
        /// Valida tutto il catalogo senza aprire scene (solo asset, nomi, Build Settings): pensato per l'hook di build.
        /// Scrive tutto come avviso e NON interrompe la build.
        /// </summary>
        /// <returns>true se non ci sono errori.</returns>
        public static bool ValidateAllForBuild()
        {
            return Report("Catalogo scenari (build)", ValidateCatalog(false), true);
        }

        // ------------------------------------------------------------------ Nucleo

        /// <summary>
        /// Nucleo testabile: valida una missione e, se data, la sua scena (contenuto compreso: la scena viene aperta in
        /// additivo se non lo è già e poi richiusa).
        /// </summary>
        /// <param name="m">Missione da controllare.</param>
        /// <param name="scene">SceneAsset della scena dello scenario (può essere null: viene cercata dal nome SceneName).</param>
        /// <returns>Elenco dei problemi (vuoto = tutto a posto).</returns>
        public static List<ValidationIssue> Validate(MissionDefinition m, SceneAsset scene)
        {
            return Validate(m, scene, true);
        }

        /// <summary>
        /// Come <see cref="Validate(MissionDefinition, SceneAsset)"/> ma con la scelta di controllare o no il contenuto della scena.
        /// </summary>
        /// <param name="m">Missione da controllare.</param>
        /// <param name="scene">SceneAsset (può essere null).</param>
        /// <param name="includeSceneContent">false = salta l'apertura della scena (solo nome, esistenza, Build Settings).</param>
        /// <returns>Elenco dei problemi.</returns>
        public static List<ValidationIssue> Validate(MissionDefinition m, SceneAsset scene, bool includeSceneContent)
        {
            var issues = new List<ValidationIssue>();
            if (m == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, "Missione mancante (riferimento vuoto). Assegna un asset SO_Mission_<Titolo>."));
                return issues;
            }
            issues.AddRange(ValidateMissionAsset(m, LoadCatalog()));

            string scenePath = scene != null ? AssetDatabase.GetAssetPath(scene) : FindScenePath(m.SceneName);
            if (string.IsNullOrEmpty(scenePath))
            {
                string noScene = $"[{m.name}] La scena \"{m.SceneName}\" non esiste nel progetto. Crea la scena copiando SCN_ScenarioTemplate e rinominala SCN_{ToPascal(m.Title)}.";
                // Le missioni di esempio ("(esempio)" in DesignerNote) non hanno ancora la scena del team: solo avviso.
                issues.Add(IsSample(m) ? Warn(noScene + " (missione di esempio: la scena la crea il team proprietario.)", m) : Err(noScene, m));
                return issues;
            }

            Scene open = default;
            bool openedHere = false;
            if (includeSceneContent && !Application.isPlaying)
            {
                open = SceneManager.GetSceneByPath(scenePath);
                if (!open.IsValid() || !open.isLoaded)
                {
                    open = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    openedHere = true;
                }
            }
            try
            {
                if (open.IsValid()) issues.AddRange(ValidateSceneContent(m, open));
                else issues.AddRange(ValidateSceneFile(m, scenePath));
            }
            finally
            {
                if (openedHere && open.IsValid()) EditorSceneManager.CloseScene(open, true);
            }
            return issues;
        }

        /// <summary>
        /// Vero se la missione è un esempio segnaposto (DesignerNote contiene "(esempio)"): la sua scena può mancare.
        /// </summary>
        /// <param name="m">Missione da controllare.</param>
        private static bool IsSample(MissionDefinition m)
        {
            return m != null && !string.IsNullOrEmpty(m.DesignerNote) && m.DesignerNote.Contains("(esempio)");
        }

        // ------------------------------------------------------------------ Catalogo

        /// <summary>
        /// Valida tutte le missioni del catalogo (Training compreso).
        /// </summary>
        private static List<ValidationIssue> ValidateCatalog(bool includeSceneContent)
        {
            var issues = new List<ValidationIssue>();
            MissionCatalog catalog = LoadCatalog();
            if (catalog == null)
            {
                issues.Add(Err("SO_MissionCatalog non trovato. Esegui SI81/UI/Build Screens oppure crea l'asset da Create > SI81 > UI > Mission Catalog."));
                return issues;
            }
            var all = new List<MissionDefinition>();
            if (catalog.Missions != null) all.AddRange(catalog.Missions);
            if (catalog.Training != null && !all.Contains(catalog.Training)) all.Add(catalog.Training);
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null)
                {
                    issues.Add(Warn($"SO_MissionCatalog contiene una voce vuota (posizione {i}). Rimuovila dalla lista Missions.", catalog));
                    continue;
                }
                issues.AddRange(Validate(all[i], null, includeSceneContent));
            }
            return issues;
        }

        // ------------------------------------------------------------------ Asset missione

        /// <summary>
        /// Controlli sull'asset della missione: nome, Id, testi, obiettivi, criteri, livelli, complessità, icona, scena, catalogo.
        /// </summary>
        private static List<ValidationIssue> ValidateMissionAsset(MissionDefinition m, MissionCatalog catalog)
        {
            var issues = new List<ValidationIssue>();
            string tag = $"[{m.name}] ";
            string titlePascal = ToPascal(m.Title);

            // Nome dell'asset: SO_Mission_<Titolo> in PascalCase.
            if (!m.name.StartsWith("SO_Mission_", StringComparison.Ordinal))
                issues.Add(Err(tag + $"Il nome dell'asset deve iniziare con \"SO_Mission_\" (es. SO_Mission_{(titlePascal.Length > 0 ? titlePascal : "FireStarter")}). Rinominalo nel Project.", m));
            else
            {
                string suffix = m.name.Substring("SO_Mission_".Length);
                if (!PASCAL.IsMatch(suffix))
                    issues.Add(Err(tag + "Dopo \"SO_Mission_\" il titolo deve essere in PascalCase inglese, senza spazi, trattini, underscore o accenti (es. SO_Mission_FireStarter).", m));
                else if (titlePascal.Length > 0 && suffix != titlePascal)
                    issues.Add(Warn(tag + $"Il nome dell'asset (\"{suffix}\") non corrisponde al Titolo (\"{m.Title}\" = \"{titlePascal}\"). Rinomina l'asset o correggi il Titolo.", m));
            }

            if (string.IsNullOrWhiteSpace(m.Title))
                issues.Add(Err(tag + "Il campo Title è vuoto. Scrivi il titolo dello scenario (es. \"Fire Starter\").", m));

            // Id: due cifre e unico nel catalogo.
            if (string.IsNullOrEmpty(m.Id) || !Regex.IsMatch(m.Id, @"^\d{2}$"))
                issues.Add(Err(tag + $"L'Id \"{m.Id}\" non è di due cifre. Usa 01..12 (Training: 00).", m));
            else if (catalog != null)
            {
                int same = 0;
                if (catalog.Missions != null)
                    foreach (MissionDefinition o in catalog.Missions)
                        if (o != null && o != m && o.Id == m.Id) same++;
                if (catalog.Training != null && catalog.Training != m && catalog.Training.Id == m.Id) same++;
                if (same > 0)
                    issues.Add(Err(tag + $"L'Id \"{m.Id}\" è già usato da un'altra missione del catalogo. Scegli un numero libero concordato con gli altri team.", m));
            }

            // Contesto.
            if (string.IsNullOrWhiteSpace(m.Context))
                issues.Add(Err(tag + "Il Contesto è vuoto. Scrivi in 280 caratteri il ruolo del giocatore e la situazione.", m));
            else if (m.Context.Length > MissionDefinition.MAX_CONTEXT_LENGTH)
                issues.Add(Err(tag + $"Il Contesto è lungo {m.Context.Length} caratteri (massimo {MissionDefinition.MAX_CONTEXT_LENGTH}). Accorcialo.", m));

            // Obiettivi.
            int objectives = m.Objectives == null ? 0 : m.Objectives.Count;
            if (objectives < 1 || objectives > MissionDefinition.MAX_OBJECTIVES)
                issues.Add(Err(tag + $"Gli obiettivi sono {objectives}: servono da 1 a {MissionDefinition.MAX_OBJECTIVES}. Modifica la lista Objectives.", m));
            for (int i = 0; i < objectives; i++)
            {
                string o = m.Objectives[i];
                if (string.IsNullOrWhiteSpace(o))
                    issues.Add(Err(tag + $"L'obiettivo {i + 1} è vuoto. Scrivi il testo o rimuovi la voce.", m));
                else if (o.Length > MAX_OBJECTIVE_CHARS)
                    issues.Add(Warn(tag + $"L'obiettivo {i + 1} è lungo {o.Length} caratteri: sul pannello da polso ne stanno ~{MAX_OBJECTIVE_CHARS} per riga. Accorcia: \"{o}\".", m));
            }

            // Criteri di completamento.
            if (m.Criteria == null || m.Criteria.Count == 0)
                issues.Add(Err(tag + "Mancano i Criteri di completamento. Scrivi un testo per Positivo, Parziale e Negativo.", m));
            else
                foreach (OutcomeType outcome in Enum.GetValues(typeof(OutcomeType)))
                    if (string.IsNullOrWhiteSpace(m.GetCriterionText(outcome)))
                        issues.Add(Warn(tag + $"Manca il criterio per l'esito {outcome}. Aggiungilo in Criteria (appare nel pannello di Mission Brief).", m));

            // Livelli.
            // Il Training usa solo il livello Guided: nessun avviso sugli altri.
            foreach (DifficultyLevel level in Enum.GetValues(typeof(DifficultyLevel)))
            {
                if (m.IsTraining) break;
                bool found = false;
                if (m.Levels != null)
                    foreach (MissionLevel l in m.Levels) if (l != null && l.Level == level) found = true;
                if (!found)
                    issues.Add(Warn(tag + $"In Levels manca il livello {level}. Senza voce il livello è sbloccato e senza tempo personalizzato: aggiungila se vuoi controllarlo.", m));
            }

            if (m.Complexity < 1 || m.Complexity > 5)
                issues.Add(Err(tag + $"La Complessità è {m.Complexity}: deve essere da 1 a 5.", m));
            if (m.Icon == null)
                issues.Add(Err(tag + "Manca l'Icona (ICN_). Assegna un'icona bianca dalla cartella Icons: appare sulla card dello scenario.", m));

            // Scena dichiarata.
            string expectedScene = titlePascal.Length > 0 ? "SCN_" + titlePascal : null;
            if (string.IsNullOrEmpty(m.SceneName))
                issues.Add(Err(tag + $"SceneName è vuoto. Imposta \"{expectedScene ?? "SCN_<Titolo>"}\".", m));
            else if (expectedScene != null && m.SceneName != expectedScene)
                issues.Add(Err(tag + $"SceneName è \"{m.SceneName}\" ma dal Titolo ci si aspetta \"{expectedScene}\". Allinea scena, SceneName e Titolo.", m));

            // Presenza nel catalogo.
            if (catalog != null && !m.IsTraining && (catalog.Missions == null || !catalog.Missions.Contains(m)))
                issues.Add(Err(tag + "La missione non è in SO_MissionCatalog. Aggiungila alla lista Missions: Home, Brief e Statistiche la mostrano da lì.", m));

            // Testi della missione (per il controllo dei segnaposto qui sotto).
            var texts = new List<string> { m.Title, m.Context };
            if (m.Objectives != null) texts.AddRange(m.Objectives);
            if (m.Criteria != null) foreach (CompletionCriterion c in m.Criteria) if (c != null) texts.Add(c.Text);
            // Segnaposto del modello (SO_Mission_Template): non è un errore, ma va compilato prima della consegna.
            bool placeholders = false;
            foreach (string t in texts)
                if (!string.IsNullOrEmpty(t) && t.Contains(ScenarioKitBuilder.TODO_MARKER)) placeholders = true;
            if (placeholders)
                issues.Add(Warn(tag + $"Ci sono ancora testi \"{ScenarioKitBuilder.TODO_MARKER}\" (titolo, contesto, obiettivi o criteri). Sostituiscili prima della consegna.", m));

            // Il modello SO_Mission_Template non è uno scenario vero: ogni problema diventa un avviso.
            if (m.name == "SO_Mission_Template")
                foreach (ValidationIssue issue in issues)
                    if (issue.Severity == ValidationSeverity.Error) issue.Severity = ValidationSeverity.Warning;
            return issues;
        }

        /// <summary>
        /// Indica se un tipo è ScenarioController o una sua sottoclasse (confronto per nome lungo la catena di ereditarietà).
        /// </summary>
        /// <param name="type">Tipo del componente.</param>
        private static bool IsScenarioController(Type type)
        {
            for (Type t = type; t != null; t = t.BaseType)
                if (t.Name == "ScenarioController") return true;
            return false;
        }

        // ------------------------------------------------------------------ Scena

        /// <summary>
        /// Controlli sulla scena come file: nome SCN_&lt;Titolo&gt; e presenza attiva nelle Build Settings.
        /// </summary>
        private static List<ValidationIssue> ValidateSceneFile(MissionDefinition m, string scenePath)
        {
            var issues = new List<ValidationIssue>();
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (!sceneName.StartsWith("SCN_", StringComparison.Ordinal) || !PASCAL.IsMatch(sceneName.Substring(4)))
                issues.Add(Err($"[{sceneName}] Il nome della scena deve essere SCN_<Titolo> in PascalCase senza spazi (es. SCN_FireStarter). Rinominala nel Project.", asset));
            else if (!string.IsNullOrEmpty(m.SceneName) && sceneName != m.SceneName)
                issues.Add(Err($"[{sceneName}] Il nome della scena non coincide con SceneName della missione (\"{m.SceneName}\"). Rinomina la scena o correggi SceneName.", asset));

            bool inBuild = false, enabled = false;
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.path == scenePath) { inBuild = true; enabled = s.enabled; }
            if (!inBuild)
            {
                string msg = $"[{sceneName}] La scena non è nelle Build Settings. Apri File > Build Profiles > Scene List e aggiungila (SCN_Core deve restare la prima).";
                // Il modello da copiare non va in build: solo avviso.
                issues.Add(sceneName == "SCN_ScenarioTemplate" ? Warn(msg, asset) : Err(msg, asset));
            }
            else if (!enabled)
                issues.Add(Err($"[{sceneName}] La scena è nelle Build Settings ma disattivata. Spunta la casella accanto al suo nome.", asset));
            return issues;
        }

        /// <summary>
        /// Controlli di file e di contenuto su una scena aperta.
        /// </summary>
        private static List<ValidationIssue> ValidateSceneContent(MissionDefinition m, Scene scene)
        {
            var issues = ValidateSceneFile(m, scene.path);
            string tag = $"[{scene.name}] ";
            UIStrings strings = LoadStrings();

            // Tutti i GameObject della scena (anche disattivati).
            var all = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) all.Add(t.gameObject);

            // ScenarioController: riconosciuto per nome di tipo (indipendente dall'ordine di compilazione).
            var controllers = new List<Component>();
            foreach (GameObject go in all)
                foreach (Component c in go.GetComponents<Component>())
                    if (c != null && IsScenarioController(c.GetType())) controllers.Add(c);
            if (controllers.Count == 0)
                issues.Add(Err(tag + "Manca il ScenarioController. Copia l'oggetto MNG_Scenario da SCN_ScenarioTemplate e assegna la tua SO_Mission_<Titolo>."));
            else if (controllers.Count > 1)
                issues.Add(Err(tag + $"Ci sono {controllers.Count} ScenarioController: ne serve uno solo. Elimina quelli in più.", controllers[1].gameObject));
            if (controllers.Count >= 1)
            {
                MissionDefinition referenced = ReadMission(controllers[0], out bool hasField);
                if (!hasField)
                    issues.Add(Warn(tag + "Il ScenarioController non espone un campo di tipo MissionDefinition: impossibile verificare il collegamento alla missione.", controllers[0].gameObject));
                else if (referenced == null)
                    issues.Add(Err(tag + "Il ScenarioController non ha la missione assegnata. Trascina la tua SO_Mission_<Titolo> nel campo Mission.", controllers[0].gameObject));
                else if (referenced != m)
                    issues.Add(Err(tag + $"Il ScenarioController punta a \"{referenced.name}\" invece che a \"{m.name}\". Assegna la missione corretta.", controllers[0].gameObject));
            }

            var risks = new List<LabelAnchor>();
            foreach (GameObject go in all)
            {
                string path = HierarchyPath(go);

                // Nessuna UI fatta a mano e nessun sistema di SCN_Core (CLAUDE.md §5, §4.11).
                if (go.GetComponent<Canvas>() != null)
                    issues.Add(Err(tag + $"Canvas in scena (\"{path}\"): negli scenari non c'è UI fatta a mano. Elimina il Canvas e usa HUDService, LabelAnchor e DecisionPanel.Show.", go));
                if (go.GetComponent<EventSystem>() != null)
                    issues.Add(Err(tag + $"EventSystem in scena (\"{path}\"): ce n'è uno solo, in MNG_UI di SCN_Core. Eliminalo.", go));
                foreach (BaseInputModule module in go.GetComponents<BaseInputModule>())
                    if (module != null)
                        issues.Add(Err(tag + $"Modulo di input {module.GetType().Name} in scena (\"{path}\"): l'input UI sta in SCN_Core. Eliminalo.", go));
                if (go.GetComponent<TrackedDeviceRaycaster>() != null)
                    issues.Add(Err(tag + $"TrackedDeviceRaycaster in \"{path}\": non è ammesso (ruba i raggi a UIWorldRaycaster). Rimuovi il componente.", go));
                // Le camere che disegnano solo su una RenderTexture (monitor, specchi) sono ammesse: non toccano il visore.
                Camera sceneCamera = go.GetComponent<Camera>();
                if (sceneCamera != null && sceneCamera.targetTexture == null)
                    issues.Add(Err(tag + $"Camera in \"{path}\": la camera e il rig (OVRCameraRig) appartengono a SCN_Core. Un secondo rig rompe testa, controller e puntatori: elimina la camera/il rig dalla scena dello scenario.", go));
                if (go.GetComponent<AudioListener>() != null)
                    issues.Add(Warn(tag + $"AudioListener in \"{path}\": ne serve uno solo, nel rig di SCN_Core. Rimuovilo.", go));

                string n = go.name;
                if (n == "MNG_UI" || n == "MNG_HUD" || n.StartsWith("HUD_", StringComparison.Ordinal) || n.StartsWith("PANEL_", StringComparison.Ordinal)
                    || n.StartsWith("CANVAS_", StringComparison.Ordinal))
                    issues.Add(Err(tag + $"Oggetto \"{path}\": gli oggetti {n.Split('_')[0]}_* vivono in SCN_Core (prefab comuni), non negli scenari. Eliminalo: compare da solo a runtime.", go));

                foreach (Component c in go.GetComponents<Component>())
                    if (c != null && CORE_ONLY_TYPES.Contains(c.GetType().Name))
                        issues.Add(Err(tag + $"Componente comune {c.GetType().Name} in \"{path}\": appartiene a SCN_Core, non allo scenario. Rimuovilo.", go));

                // Script mancanti.
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing > 0)
                    issues.Add(Err(tag + $"{missing} script mancante/i su \"{path}\". Rimuovi il componente (menu del componente > Remove) o ripristina lo script.", go));

                // Nomi con prefisso SI81: PREFISSO_PascalCase.
                if (PREFIXED.IsMatch(n) && !PREFIXED_OK.IsMatch(n))
                    issues.Add(Warn(tag + $"Il nome \"{n}\" ha un prefisso SI81 ma non è PREFISSO_PascalCase (niente spazi, accenti, trattini o underscore oltre al primo e alla variante finale). Es. BTN_StartMission.", go));

                // Ancore delle etichette e chiavi di testo.
                LabelAnchor anchor = go.GetComponent<LabelAnchor>();
                if (anchor != null)
                {
                    if (anchor.Kind == LabelKind.Risk) risks.Add(anchor);
                    if (string.IsNullOrWhiteSpace(anchor.Title) && string.IsNullOrWhiteSpace(anchor.TitleKey))
                        issues.Add(Warn(tag + $"LabelAnchor su \"{path}\" senza titolo: scrivi il testo (Title) o una chiave (TitleKey).", go));
                    CheckKey(issues, strings, tag, anchor.TitleKey, path, go);
                    CheckKey(issues, strings, tag, anchor.DetailKey, path, go);
                    CheckKey(issues, strings, tag, anchor.ButtonKey, path, go);
                    CheckKey(issues, strings, tag, anchor.ActionKey, path, go);
                }
                foreach (UIStringText st in go.GetComponents<UIStringText>())
                    if (st != null) CheckKey(issues, strings, tag, st.Key, path, go);
            }

            // Massimo 3 etichette di rischio visibili insieme.
            foreach (LabelAnchor a in risks)
            {
                int near = 0;
                foreach (LabelAnchor b in risks)
                    if (Vector3.Distance(a.WorldPoint, b.WorldPoint) <= RISK_CLUSTER_RADIUS) near++;
                if (near > MAX_RISK_LABELS)
                {
                    issues.Add(Warn(tag + $"{near} etichette di rischio entro {RISK_CLUSTER_RADIUS:0} m da \"{HierarchyPath(a.gameObject)}\": ne restano visibili al massimo {MAX_RISK_LABELS}. Distanziale o cambia il tipo di alcune.", a.gameObject));
                    break;
                }
            }
            return issues;
        }

        /// <summary>
        /// Controlla che una chiave di testo (se presente) abbia formato valido ed esista in SO_UIStrings_It.
        /// </summary>
        private static void CheckKey(List<ValidationIssue> issues, UIStrings strings, string tag, string key, string path, GameObject go)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!UIStrings.IsValidKey(key))
                issues.Add(Err(tag + $"Chiave \"{key}\" su \"{path}\" non valida: usa minuscolo, snake_case, separato da punti (es. hud.alert.fire_spreading).", go));
            else if (strings != null && !strings.Contains(key))
                issues.Add(Err(tag + $"La chiave \"{key}\" su \"{path}\" non esiste in SO_UIStrings_It. Aggiungila a strings_it.json (poi SI81/UI/Build Theme) o scrivi il testo direttamente nel campo Title/Detail.", go));
        }

        // ------------------------------------------------------------------ Utilità

        /// <summary>
        /// Cerca la missione di una scena: dal ScenarioController, altrimenti dal catalogo per SceneName.
        /// </summary>
        private static MissionDefinition FindMissionForScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Component c in root.GetComponentsInChildren<Component>(true))
                    if (c != null && c.GetType().Name == "ScenarioController")
                    {
                        MissionDefinition m = ReadMission(c, out _);
                        if (m != null) return m;
                    }
            MissionCatalog catalog = LoadCatalog();
            if (catalog != null)
            {
                if (catalog.Missions != null)
                    foreach (MissionDefinition m in catalog.Missions)
                        if (m != null && m.SceneName == scene.name) return m;
                if (catalog.Training != null && catalog.Training.SceneName == scene.name) return catalog.Training;
            }
            return null;
        }

        /// <summary>
        /// Legge dal ScenarioController il primo campo di tipo MissionDefinition (pubblico o privato, anche nelle classi base).
        /// </summary>
        private static MissionDefinition ReadMission(Component controller, out bool hasField)
        {
            hasField = false;
            for (Type t = controller.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (typeof(MissionDefinition).IsAssignableFrom(f.FieldType))
                    {
                        hasField = true;
                        return f.GetValue(controller) as MissionDefinition;
                    }
            return null;
        }

        /// <summary>
        /// Percorso nella gerarchia ("Padre/Figlio") di un GameObject.
        /// </summary>
        private static string HierarchyPath(GameObject go)
        {
            var sb = new StringBuilder(go.name);
            for (Transform t = go.transform.parent; t != null; t = t.parent) sb.Insert(0, t.name + "/");
            return sb.ToString();
        }

        /// <summary>
        /// Converte un titolo in PascalCase ASCII ("Fire Starter" diventa "FireStarter"; accenti e simboli eliminati).
        /// </summary>
        /// <param name="title">Titolo scritto a mano.</param>
        /// <returns>Titolo in PascalCase (vuoto se il titolo è vuoto).</returns>
        internal static string ToPascal(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            string decomposed = title.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            bool upperNext = true;
            foreach (char ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (ch < 128 && char.IsLetterOrDigit(ch)) { sb.Append(upperNext ? char.ToUpperInvariant(ch) : ch); upperNext = false; }
                else upperNext = true;
            }
            return sb.ToString();
        }

        /// <summary>Cerca nel progetto il SO_MissionCatalog.</summary>
        private static MissionCatalog LoadCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:MissionCatalog");
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<MissionCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>Cerca nel progetto il SO_UIStrings_It.</summary>
        private static UIStrings LoadStrings()
        {
            string[] guids = AssetDatabase.FindAssets("t:UIStrings");
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<UIStrings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>Cerca il percorso di una scena per nome.</summary>
        private static string FindScenePath(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            foreach (string guid in AssetDatabase.FindAssets("t:SceneAsset " + sceneName))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == sceneName) return p;
            }
            return null;
        }

        /// <summary>Crea un errore.</summary>
        private static ValidationIssue Err(string message, UnityEngine.Object context = null) => new ValidationIssue(ValidationSeverity.Error, message, context);

        /// <summary>Crea un avviso.</summary>
        private static ValidationIssue Warn(string message, UnityEngine.Object context = null) => new ValidationIssue(ValidationSeverity.Warning, message, context);

        /// <summary>
        /// Scrive i problemi in Console (errori come LogError, così la CI li vede) e, se non è la build, mostra la finestra.
        /// </summary>
        /// <param name="title">Titolo del rapporto.</param>
        /// <param name="issues">Problemi trovati.</param>
        /// <param name="buildMode">true = tutto come avviso e nessuna finestra (hook di build: non blocca mai).</param>
        /// <returns>true se non ci sono errori.</returns>
        private static bool Report(string title, List<ValidationIssue> issues, bool buildMode)
        {
            int errors = 0, warnings = 0;
            foreach (ValidationIssue i in issues)
            {
                string text = "[SI81 Scenari] " + i.Message;
                if (i.Severity == ValidationSeverity.Error)
                {
                    errors++;
                    if (buildMode) Debug.LogWarning(text, i.Context); else Debug.LogError(text, i.Context);
                }
                else if (i.Severity == ValidationSeverity.Warning) { warnings++; Debug.LogWarning(text, i.Context); }
                else Debug.Log(text, i.Context);
            }
            if (errors == 0 && warnings == 0)
                issues.Add(new ValidationIssue(ValidationSeverity.Info, "Tutto a posto: nessun problema trovato."));
            Debug.Log($"[SI81 Scenari] {title}: {errors} errori, {warnings} avvisi.");
            if (!buildMode) ValidationWindow.ShowResults(title, issues);
            return errors == 0;
        }
    }

    /// <summary>
    /// Hook di build: prima di ogni build controlla il catalogo e scrive gli esiti in Console. Per ora SOLO avvisa
    /// (non interrompe mai la build); per bloccarla, lanciare un <see cref="BuildFailedException"/> se ValidateAllForBuild() è false.
    /// </summary>
    internal sealed class ScenarioBuildCheck : IPreprocessBuildWithReport
    {
        /// <summary>Ordine di esecuzione tra gli hook di build.</summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Esegue la validazione del catalogo prima della build (solo avvisi).
        /// </summary>
        /// <param name="report">Rapporto di build (non usato).</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            ScenarioValidator.ValidateAllForBuild();
        }
    }
}
