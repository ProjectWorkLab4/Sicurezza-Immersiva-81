using System.Collections.Generic;
using SI81.UI;
using SI81.Tiziano.ChemicalAlert.Core;
using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Collega il gameplay Chemical Alert alla sessione, alla HUD e alla chiusura missione condivise.
    /// </summary>
    public sealed class ChemicalAlertScenarioController : ScenarioController
    {
        [SerializeField] private ChemicalAlertDirector director;

        /// <summary>Indica se il livello corrente consente i suggerimenti manuali.</summary>
        public bool CanRequestHint =>
            director != null &&
            Level == DifficultyLevel.Guided &&
            director.CurrentConfig != null &&
            director.CurrentConfig.HintsEnabled;

        /// <summary>Collega gli eventi del Director alla HUD comune.</summary>
        private void OnEnable()
        {
            if (director == null)
                return;

            director.ResultReady += HandleResultReady;
            director.HintRequested += HandleHintRequested;
            director.ErrorReported += HandleErrorReported;
            director.DangerLevelChanged += HandleDangerLevelChanged;
        }

        /// <summary>Rimuove le sottoscrizioni agli eventi del Director.</summary>
        private void OnDisable()
        {
            if (director == null)
                return;

            director.ResultReady -= HandleResultReady;
            director.HintRequested -= HandleHintRequested;
            director.ErrorReported -= HandleErrorReported;
            director.DangerLevelChanged -= HandleDangerLevelChanged;
        }

        /// <summary>Segna i quattro obiettivi della missione quando il gameplay li raggiunge.</summary>
        private void Update()
        {
            if (!IsRunning || director == null)
                return;

            SyncObjective(0, director.VentilationActivated);
            SyncObjective(1, director.SdsWasRead);
            SyncObjective(2, director.RequiredPpeAcquired);
            SyncObjective(3, director.SpillSecured);
        }

        /// <summary>Allinea uno specifico obiettivo a una condizione del Director.</summary>
        private void SyncObjective(int index, bool done)
        {
            bool wasDone = MissionFlow.IsObjectiveCompleted(index);
            if (done == wasDone)
                return;

            MissionFlow.SetObjectiveCompleted(index, done);
            int next = MissionFlow.FirstIncompleteObjectiveIndex();
            SetObjective(next >= 0 ? next : ObjectiveCount - 1);
            if (done && CanRequestHint)
                ShowObjectiveAlert();
        }

        /// <summary>Allinea difficoltà e timer locali alla sessione della UI condivisa.</summary>
        protected override void OnScenarioStarted()
        {
            if (director == null)
            {
                UnityEngine.Debug.LogError("[CA] ChemicalAlertScenarioController: Director non assegnato.", this);
                return;
            }

            ChemicalAlertDirector.Difficulty difficulty = Level switch
            {
                DifficultyLevel.Guided => ChemicalAlertDirector.Difficulty.Guided,
                DifficultyLevel.Crisis => ChemicalAlertDirector.Difficulty.Crisis,
                _ => ChemicalAlertDirector.Difficulty.Operational
            };

            bool timerEnabled = Level != DifficultyLevel.Guided;
            float durationSeconds = Level == DifficultyLevel.Operational
                ? 600f
                : Level == DifficultyLevel.Crisis ? 300f : 180f;

            director.ConfigureForMission(difficulty, timerEnabled, durationSeconds);
            // Lasciamo che SetObjective(int) della base mostri il testo e il contatore dell'asset missione:
            // la Pausa e il polso devono seguire gli stessi obiettivi, con i check segnati dal gameplay.
            SetPhase(PHASE_ACTION);
        }

        /// <summary>Trasferisce la scadenza del timer comune al percorso di esito del gameplay.</summary>
        protected override void OnTimerExpired()
        {
            if (director != null)
            {
                director.ForceTimeUp();
                return;
            }

            EndMission(
                OutcomeType.Negative,
                TIMEOUT_SCORE,
                null,
                new List<string> { DebriefLinePrefix.BAD + "Tempo scaduto prima di mettere in sicurezza l'area." });
        }

        /// <summary>Richiede al Director il prossimo suggerimento Guided dal pulsante 3D.</summary>
        public bool RequestHint()
        {
            return CanRequestHint && director.RequestHint();
        }

        /// <summary>Testo dettagliato del prossimo obiettivo, usato dal popup della HUD.</summary>
        public string GetNextObjectiveHintText()
        {
            int next = MissionFlow.FirstIncompleteObjectiveIndex();
            if (next < 0 || Mission == null || Mission.Objectives == null || next >= Mission.Objectives.Count)
                return "Obiettivi completati: area messa in sicurezza.";

            string detail = next switch
            {
                0 => "Premi il pulsante della ventilazione per ripulire l'aria.",
                1 => "Avvicinati all'SDS sul banco e attivalo per leggere la sostanza.",
                2 => "Apri l'armadio e prendi maschera, guanti e protezione occhi corretti.",
                _ => "Delimita la perdita con il kit, poi posiziona 2 fogli assorbenti sulla pozza."
            };
            return "Prossimo obiettivo: " + Mission.Objectives[next] + "\n" + detail;
        }

        /// <summary>Mostra l'obiettivo corrente con il popup evento della HUD condivisa.</summary>
        private void ShowObjectiveAlert()
        {
            HUDService.ShowEventAlert(GetNextObjectiveHintText(), "ICN_Lightbulb");
        }

        /// <summary>Mostra il testo italiano corrispondente alla chiave di suggerimento.</summary>
        private void HandleHintRequested(string key)
        {
            switch (key)
            {
                case "chemical.hint.ppe":
                    SetHint("Indossa maschera, guanti e protezione per gli occhi.");
                    break;
                case "chemical.hint.sds":
                    SetHint("Leggi la scheda di sicurezza sul banco.");
                    break;
                case "chemical.hint.ventilation":
                    SetHint("Attiva la ventilazione prima di intervenire.");
                    break;
                case "chemical.hint.perimeter":
                    SetHint("Delimita la zona con il kit per sversamenti.");
                    break;
                case "chemical.hint.cleanup":
                    SetHint("Assorbi e raccogli il liquido contenuto.");
                    break;
                default:
                    SetHint("Controlla i pericoli e proteggiti prima di intervenire.");
                    break;
            }

            ShowObjectiveAlert();
        }

        /// <summary>Converte gli errori di gameplay in feedback della HUD comune.</summary>
        private void HandleErrorReported(string key)
        {
            string message;
            switch (key)
            {
                case "chemical.error.gloves":
                    message = "Indossa i guanti chimici prima di usare gli strumenti.";
                    break;
                case "chemical.error.no_mask":
                    message = "Avvicinarsi alla perdita senza mascherina provoca tosse.";
                    break;
                case "chemical.error.eye_protection":
                    message = "Proteggi gli occhi con occhiali a tenuta o visiera.";
                    break;
                case "chemical.error.procedure_order":
                    message = "Procedura non rispettata: l'esito potrebbe essere parziale.";
                    break;
                default:
                    message = "Azione non corretta: verifica la procedura e i DPI.";
                    break;
            }

            Feedback(message, FeedbackKind.Negative);
            Alert("Attenzione\n" + message, "ICN_Warning_Filled");
        }

        /// <summary>Comunica alla HUD condivisa l'aumento del pericolo; la vignetta è gestita nell'istanza UI Chemical Alert.</summary>
        private void HandleDangerLevelChanged(ChemicalAlertDirector.DangerLevel level)
        {
            if (level == ChemicalAlertDirector.DangerLevel.Mid)
                Alert("Esposizione in aumento\nAttiva la ventilazione e allontanati dalla perdita.", "ICN_Warning_Filled");
            else if (level == ChemicalAlertDirector.DangerLevel.Up)
                Alert("Aria pericolosa\nAllontanati e completa la messa in sicurezza.", "ICN_Warning_Filled");
        }

        /// <summary>Conclude la sessione condivisa usando esito e punteggi del valutatore Chemical Alert.</summary>
        private void HandleResultReady(ScenarioResult result)
        {
            if (result == null)
                return;

            OutcomeType outcome = result.Outcome switch
            {
                Core.Outcome.Success => OutcomeType.Positive,
                Core.Outcome.Partial => OutcomeType.Partial,
                _ => OutcomeType.Negative
            };

            int[] dimensions =
            {
                result.GetSubScore(Dimension.Safety),
                result.GetSubScore(Dimension.Awareness),
                result.GetSubScore(Dimension.Decision),
                result.GetSubScore(Dimension.Execution),
                result.GetSubScore(Dimension.Time)
            };

            string summary = result.Outcome switch
            {
                Core.Outcome.Success => DebriefLinePrefix.GOOD + "Area contenuta e bonificata con la protezione prevista.",
                Core.Outcome.Partial => DebriefLinePrefix.BAD + "Messa in sicurezza incompleta: verifica contenimento, DPI e bonifica.",
                _ => DebriefLinePrefix.CRITICAL + "La sicurezza dell'operatore o dell'area è stata compromessa."
            };

            EndMission(outcome, -1, dimensions, new List<string> { summary });
        }
    }
}
