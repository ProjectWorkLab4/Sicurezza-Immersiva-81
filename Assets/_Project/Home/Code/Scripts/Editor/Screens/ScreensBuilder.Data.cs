using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SI81.UI.EditorTools
{
    /// <summary>
    /// Parte "dati" di ScreensBuilder: crea o aggiorna gli asset SO_LevelConfig_*, gli 8 scenari di esempio, SO_Mission_Training,
    /// i 12 badge e SO_MissionCatalog in Assets/_Project/Home/UI/Data/Missions/. Sicuro da rieseguire: i GUID restano, gli scenari
    /// già modificati dai team (senza il marcatore "(esempio)" in DesignerNote) NON vengono sovrascritti e le voci aggiunte
    /// a mano al catalogo restano. Tutti i valori di LevelConfig e dei contenuti di esempio sono PROVVISORI.
    /// </summary>
    internal static partial class ScreensBuilder
    {
        /// <summary>Marcatore dei contenuti segnaposto in MissionDefinition.DesignerNote.</summary>
        internal const string SAMPLE_MARKER = "(esempio)";

        /// <summary>Descrizione compatta di uno scenario di esempio.</summary>
        private struct SampleMission
        {
            /// <summary>Identificativi, testi e colori (esadecimali) dello scenario di esempio.</summary>
            public string Id, Title, SceneName, IconName, TopHex, BottomHex, Context;
            /// <summary>Complessità 1-5.</summary>
            public int Complexity;
            /// <summary>Obiettivi (massimo 30 caratteri).</summary>
            public string[] Objectives;
            /// <summary>Criteri di completamento: Positivo, Parziale, Negativo.</summary>
            public string[] Criteria; // Positivo, Parziale, Negativo
        }

        /// <summary>Descrizione compatta di un badge.</summary>
        private struct SampleBadge
        {
            /// <summary>Identificativo, titolo, descrizione e icona del badge.</summary>
            public string Id, Title, Description, IconName;
            /// <summary>Regola di sblocco.</summary>
            public BadgeRule Rule;
        }

        // Scenari di esempio: ids 01,02,03,04,07,08,09,10 (FIGMA_SPECS, mock di Home). Testi segnaposto in italiano.
        /// <summary>Scenari di esempio per il catalogo (ids 01-10).</summary>
        private static readonly SampleMission[] SAMPLE_MISSIONS =
        {
            new SampleMission { Id = "01", Title = "Fire Starter", SceneName = "SCN_FireStarter", IconName = "ICN_LocalFireDepartment",
                TopHex = "#C8402A", BottomHex = "#F2994A", Complexity = 2,
                Context = "Ruolo: addetto antincendio di piano. Open space al 2° piano, pausa pranzo: una ciabatta multipresa sovraccarica sotto una scrivania inizia a fumare. Riconosci il principio d'incendio, dai l'allarme e intervieni in sicurezza prima che il fumo raggiunga le vie d'esodo.",
                Objectives = new[] { "Trova la fonte del fumo", "Dai l'allarme al coordinatore", "Stacca la multipresa", "Spegni con l'estintore" },
                Criteria = new[] { "Incendio spento e allarme dato in tempo", "Incendio spento, ma allarme in ritardo", "Il fumo raggiunge le vie d'esodo" } },
            new SampleMission { Id = "02", Title = "Blackout", SceneName = "SCN_Blackout", IconName = "ICN_FlashOff",
                TopHex = "#3E1C66", BottomHex = "#7B5BA6", Complexity = 3,
                Context = "Ruolo: tecnico di reparto. Un blackout improvviso lascia il magazzino al buio con i macchinari ancora in moto. Raggiungi il quadro in sicurezza, segnala il guasto e guida i colleghi verso l'uscita senza usare fiamme libere.",
                Objectives = new[] { "Attiva la luce d'emergenza", "Avvisa i colleghi vicini", "Raggiungi il quadro elettrico", "Segnala il guasto" },
                Criteria = new[] { "Tutti al sicuro e guasto segnalato", "Tutti al sicuro, segnalazione tardiva", "Un collega resta al buio in zona pericolosa" } },
            new SampleMission { Id = "03", Title = "Man Down", SceneName = "SCN_ManDown", IconName = "ICN_EcgHeart",
                TopHex = "#1C6E80", BottomHex = "#3FB8A0", Complexity = 2,
                Context = "Ruolo: addetto al primo soccorso. Un collega si accascia in reparto e non risponde. Valuta la scena, chiama i soccorsi, controlla il respiro e usa il defibrillatore seguendo le istruzioni.",
                Objectives = new[] { "Metti in sicurezza la scena", "Verifica coscienza e respiro", "Chiama il 112", "Usa il defibrillatore" },
                Criteria = new[] { "Soccorsi chiamati e DAE usato subito", "Soccorsi chiamati, DAE usato in ritardo", "Soccorsi non chiamati in tempo" } },
            new SampleMission { Id = "04", Title = "Chemical Alert", SceneName = "SCN_ChemicalAlert", IconName = "ICN_Science",
                TopHex = "#24805C", BottomHex = "#76C98A", Complexity = 4,
                Context = "Ruolo: operatore di laboratorio. Un contenitore perde un liquido sconosciuto sul banco. Leggi l'etichetta, indossa i DPI giusti, isola la zona e avvisa il responsabile prima di pulire.",
                Objectives = new[] { "Allontanati e avvisa tutti", "Leggi l'etichetta di pericolo", "Indossa i DPI corretti", "Isola l'area della fuoriuscita" },
                Criteria = new[] { "Area isolata con i DPI corretti", "Area isolata, DPI incompleti", "Contatto con la sostanza" } },
            new SampleMission { Id = "07", Title = "Forklift Chaos", SceneName = "SCN_ForkliftChaos", IconName = "ICN_Forklift",
                TopHex = "#2B4A86", BottomHex = "#5B86B5", Complexity = 3,
                Context = "Ruolo: operatore di magazzino. Un carrello elevatore percorre le corsie con un carico instabile. Segnala il pericolo, tieniti fuori dalla corsia di manovra e fai fermare il mezzo prima che il carico cada.",
                Objectives = new[] { "Resta fuori dalla corsia", "Segnala il carico instabile", "Fai fermare il mezzo", "Delimita la zona di rischio" },
                Criteria = new[] { "Mezzo fermo e zona delimitata", "Mezzo fermo, zona non delimitata", "Persona investita o carico caduto" } },
            new SampleMission { Id = "08", Title = "Machine Lock", SceneName = "SCN_MachineLock", IconName = "ICN_PrecisionManufacturing",
                TopHex = "#3F4A55", BottomHex = "#7D8C99", Complexity = 4,
                Context = "Ruolo: manutentore. Una pressa si è bloccata durante il ciclo. Prima di intervenire applica la procedura di blocco e segnalazione, verifica l'assenza di energia residua e solo dopo libera il pezzo.",
                Objectives = new[] { "Ferma la macchina", "Applica il lucchetto di blocco", "Verifica l'energia residua", "Libera il pezzo incastrato" },
                Criteria = new[] { "Procedura completa e pezzo liberato", "Procedura incompleta, nessun danno", "Intervento a macchina attiva" } },
            new SampleMission { Id = "09", Title = "Evacuation", SceneName = "SCN_Evacuation", IconName = "ICN_DoorOpen",
                TopHex = "#1B6E35", BottomHex = "#58B65E", Complexity = 3,
                Context = "Ruolo: addetto all'evacuazione. Suona l'allarme in un edificio di uffici. Guida i colleghi lungo le vie d'esodo, controlla le stanze, chiudi le porte e raggiungi il punto di raccolta senza usare l'ascensore.",
                Objectives = new[] { "Guida i colleghi alle uscite", "Controlla le stanze del piano", "Chiudi le porte dietro di te", "Raggiungi il punto di raccolta" },
                Criteria = new[] { "Tutti al punto di raccolta in tempo", "Evacuazione completa ma lenta", "Persone ancora nell'edificio" } },
            new SampleMission { Id = "10", Title = "Electric Shock", SceneName = "SCN_ElectricShock", IconName = "ICN_ElectricBolt",
                TopHex = "#8C7A12", BottomHex = "#D9C43A", Complexity = 4,
                Context = "Ruolo: addetto alla squadra di emergenza. Un collega resta a contatto con un cavo scoperto. Non toccarlo: togli corrente dal quadro, allontana il cavo con un oggetto isolante e presta i primi soccorsi.",
                Objectives = new[] { "Non toccare l'infortunato", "Togli corrente dal quadro", "Allontana il cavo isolato", "Presta i primi soccorsi" },
                Criteria = new[] { "Corrente tolta e soccorso prestato", "Corrente tolta, soccorso in ritardo", "Soccorritore folgorato" } }
        };

        // Badge del Profilo (12). Le prime 5 regole sono quelle sbloccate dai dati di esempio.
        /// <summary>Badge di esempio.</summary>
        private static readonly SampleBadge[] SAMPLE_BADGES =
        {
            new SampleBadge { Id = "first_mission", Title = "Prima missione", Description = "Concludi la tua prima missione.", IconName = "ICN_WorkspacePremium_Filled", Rule = BadgeRule.FirstMission },
            new SampleBadge { Id = "three_scenarios", Title = "Esploratore", Description = "Gioca 3 scenari diversi.", IconName = "ICN_LocalFireDepartment_Filled", Rule = BadgeRule.ThreeScenarios },
            new SampleBadge { Id = "four_stars", Title = "Ottimo risultato", Description = "Ottieni almeno 4 stelle in uno scenario.", IconName = "ICN_Star_Filled", Rule = BadgeRule.FourStars },
            new SampleBadge { Id = "five_positives", Title = "Esiti sicuri", Description = "Ottieni 5 esiti positivi.", IconName = "ICN_CheckCircle_Filled", Rule = BadgeRule.FivePositives },
            new SampleBadge { Id = "operational_completed", Title = "Operativo", Description = "Concludi uno scenario al livello Operational.", IconName = "ICN_Engineering_Filled", Rule = BadgeRule.OperationalCompleted },
            new SampleBadge { Id = "crisis_completed", Title = "Sangue freddo", Description = "Concludi uno scenario al livello Crisis.", IconName = "ICN_Warning_Filled", Rule = BadgeRule.CrisisCompleted },
            new SampleBadge { Id = "five_stars", Title = "Cinque stelle", Description = "Ottieni 5 stelle in uno scenario.", IconName = "ICN_Star_Filled", Rule = BadgeRule.FiveStars },
            new SampleBadge { Id = "fast_run", Title = "Allarme lampo", Description = "Concludi con esito positivo in meno di 3 minuti.", IconName = "ICN_NotificationsActive_Filled", Rule = BadgeRule.FastRun },
            new SampleBadge { Id = "ten_sessions", Title = "Veterano", Description = "Gioca 10 sessioni.", IconName = "ICN_Person_Filled", Rule = BadgeRule.TenSessions },
            new SampleBadge { Id = "all_levels", Title = "Tutti i livelli", Description = "Concludi i 3 livelli di uno stesso scenario.", IconName = "ICN_Tune_Filled", Rule = BadgeRule.AllLevelsOneScenario },
            new SampleBadge { Id = "perfect_dimension", Title = "Dimensione perfetta", Description = "Raggiungi 95 in una dimensione del punteggio.", IconName = "ICN_Bolt_Filled", Rule = BadgeRule.PerfectDimension },
            new SampleBadge { Id = "all_scenarios", Title = "Completista", Description = "Gioca tutti gli scenari del catalogo.", IconName = "ICN_BarChart_Filled", Rule = BadgeRule.AllScenarios }
        };

        /// <summary>
        /// Crea o aggiorna i dati di scenario: SO_LevelConfig_*, scenari di esempio, SO_Mission_Training, SO_MissionCatalog.
        /// </summary>
        /// <returns>Riepilogo in italiano.</returns>
        internal static string BuildDataAssets()
        {
            BaseBuilder.EnsureFolder(MISSIONS_FOLDER);
            var report = new StringBuilder("Dati scenari: ");

            // Configurazioni di livello (comuni: sempre riallineate ai valori del builder).
            LevelConfig guided = BuildLevelConfig(DifficultyLevel.Guided, "ui.level.guided", "ui.level.guided_desc", true, true, true, true, true, false, 0);
            LevelConfig operational = BuildLevelConfig(DifficultyLevel.Operational, "ui.level.operational", "ui.level.operational_desc", false, false, true, false, true, true, 600);
            LevelConfig crisis = BuildLevelConfig(DifficultyLevel.Crisis, "ui.level.crisis", "ui.level.crisis_desc", false, false, false, false, true, true, 300);

            // Catalogo (si crea prima, per tenere i riferimenti).
            var catalog = LoadOrCreateAsset<MissionCatalog>(CATALOG_ASSET, out bool catalogCreated);
            catalog.Guided = guided;
            catalog.Operational = operational;
            catalog.Crisis = crisis;

            // Scenari di esempio: le voci aggiunte a mano restano, quelle di esempio si riallineano.
            if (catalog.Missions == null) catalog.Missions = new List<MissionDefinition>();
            catalog.Missions.RemoveAll(m => m == null);
            int created = 0;
            foreach (SampleMission sample in SAMPLE_MISSIONS)
            {
                MissionDefinition mission = BuildSampleMission(sample, out bool wasCreated);
                if (wasCreated) created++;
                if (!catalog.Missions.Contains(mission)) catalog.Missions.Add(mission);
            }
            catalog.Missions.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            // Training comune.
            catalog.Training = BuildTrainingMission();

            // Badge.
            catalog.Badges = BuildBadges();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            report.Append($"catalogo {(catalogCreated ? "creato" : "aggiornato")} ({catalog.Missions.Count} scenari, {created} nuovi, Training, 3 livelli, {catalog.Badges.Count} badge).");
            return report.ToString();
        }

        /// <summary>
        /// Crea o aggiorna una configurazione di livello.
        /// </summary>
        private static LevelConfig BuildLevelConfig(DifficultyLevel level, string nameKey, string descKey, bool highlights, bool hints,
            bool riskLabels, bool contextHelp, bool objectives, bool timer, int timeLimit)
        {
            var cfg = LoadOrCreateAsset<LevelConfig>(MISSIONS_FOLDER + "/SO_LevelConfig_" + level + ".asset", out _);
            cfg.Level = level;
            cfg.NameKey = nameKey;
            cfg.DescriptionKey = descKey;
            cfg.ShowHighlights = highlights;
            cfg.ShowHints = hints;
            cfg.ShowRiskLabels = riskLabels;
            cfg.ShowContextHelp = contextHelp;
            cfg.ShowObjectivesOnWrist = objectives;
            cfg.TimerEnabled = timer;
            cfg.DefaultTimeLimitSeconds = timeLimit;
            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        /// <summary>
        /// Crea o aggiorna uno scenario di esempio. Se l'asset esiste e non è più marcato "(esempio)" resta com'è.
        /// </summary>
        private static MissionDefinition BuildSampleMission(SampleMission s, out bool created)
        {
            string assetName = "SO_Mission_" + s.Title.Replace(" ", string.Empty);
            var mission = LoadOrCreateAsset<MissionDefinition>(MISSIONS_FOLDER + "/" + assetName + ".asset", out created);
            if (!created && (mission.DesignerNote == null || !mission.DesignerNote.Contains(SAMPLE_MARKER))) return mission;

            mission.Id = s.Id;
            mission.Title = s.Title;
            mission.Context = s.Context;
            if (s.Context.Length > MissionDefinition.MAX_CONTEXT_LENGTH)
                Debug.LogWarning($"[SI81 UI] {assetName}: contesto di {s.Context.Length} caratteri (massimo {MissionDefinition.MAX_CONTEXT_LENGTH}).");
            mission.Icon = ComponentsBuilder.LoadIcon(s.IconName);
            mission.GradientTop = ParseColor(s.TopHex);
            mission.GradientBottom = ParseColor(s.BottomHex);
            mission.Complexity = s.Complexity;
            mission.SceneName = s.SceneName;
            mission.Objectives = new List<string>(s.Objectives);
            mission.Criteria = new List<CompletionCriterion>
            {
                new CompletionCriterion { Outcome = OutcomeType.Positive, Text = s.Criteria[0] },
                new CompletionCriterion { Outcome = OutcomeType.Partial, Text = s.Criteria[1] },
                new CompletionCriterion { Outcome = OutcomeType.Negative, Text = s.Criteria[2] }
            };
            mission.Levels = DefaultLevels();
            mission.IsTraining = false;
            mission.DesignerNote = SAMPLE_MARKER + " Contenuto segnaposto: il team proprietario lo sostituisce e toglie questo marcatore.";
            EditorUtility.SetDirty(mission);
            return mission;
        }

        /// <summary>
        /// Crea o aggiorna SO_Mission_Training (Id "00").
        /// </summary>
        private static MissionDefinition BuildTrainingMission()
        {
            var mission = LoadOrCreateAsset<MissionDefinition>(MISSIONS_FOLDER + "/SO_Mission_Training.asset", out bool created);
            if (!created && (mission.DesignerNote == null || !mission.DesignerNote.Contains(SAMPLE_MARKER))) return mission;
            mission.Id = "00";
            mission.Title = "Training";
            mission.Context = "Tutorial comune: impari a muoverti, afferrare gli oggetti, selezionare i pannelli con il raggio e aprire la Pausa. Nessun punteggio.";
            mission.Icon = ComponentsBuilder.LoadIcon("ICN_School");
            mission.GradientTop = ParseColor("#24805C");
            mission.GradientBottom = ParseColor("#76C98A");
            mission.Complexity = 1;
            mission.SceneName = "SCN_Training";
            mission.Objectives = new List<string> { "Muoviti e girati nella stanza", "Afferra e rilascia un oggetto", "Seleziona un pannello", "Apri la Pausa" };
            mission.Criteria = new List<CompletionCriterion>
            {
                new CompletionCriterion { Outcome = OutcomeType.Positive, Text = "Completi tutti e 4 i passi del tutorial." },
                new CompletionCriterion { Outcome = OutcomeType.Partial, Text = "Completi i passi ma chiedi aiuto più volte." },
                new CompletionCriterion { Outcome = OutcomeType.Negative, Text = "Esci dal tutorial prima della fine." }
            };
            mission.Levels = new List<MissionLevel> { new MissionLevel { Level = DifficultyLevel.Guided } };
            mission.IsTraining = true;
            mission.DesignerNote = SAMPLE_MARKER + " Training comune senza punteggio.";
            EditorUtility.SetDirty(mission);
            return mission;
        }

        /// <summary>
        /// Costruisce l'elenco dei 12 badge con le icone piene.
        /// </summary>
        private static List<BadgeDefinition> BuildBadges()
        {
            var list = new List<BadgeDefinition>();
            foreach (SampleBadge b in SAMPLE_BADGES)
                list.Add(new BadgeDefinition { Id = b.Id, Title = b.Title, Description = b.Description, Icon = ComponentsBuilder.LoadIcon(b.IconName), Rule = b.Rule });
            return list;
        }

        /// <summary>
        /// Le tre voci di livello, tutte sbloccate (nessun tempo personalizzato).
        /// </summary>
        internal static List<MissionLevel> DefaultLevels()
        {
            return new List<MissionLevel>
            {
                new MissionLevel { Level = DifficultyLevel.Guided },
                new MissionLevel { Level = DifficultyLevel.Operational },
                new MissionLevel { Level = DifficultyLevel.Crisis }
            };
        }

        /// <summary>
        /// Converte un colore esadecimale "#RRGGBB" (se non valido, magenta come segnale).
        /// </summary>
        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;
        }

        /// <summary>
        /// Carica un asset ScriptableObject dal percorso o lo crea (stesso GUID alle esecuzioni successive).
        /// </summary>
        /// <typeparam name="T">Tipo dell'asset.</typeparam>
        /// <param name="path">Percorso .asset.</param>
        /// <param name="created">true se è stato creato ora.</param>
        /// <returns>L'asset.</returns>
        internal static T LoadOrCreateAsset<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }
    }
}
