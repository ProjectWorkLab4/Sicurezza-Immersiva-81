using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Systems;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.Tiziano.Evacuation.Integration
{
    /// <summary>
    /// Ponte tra EvacuationDirector e la UI comune: obiettivi, avvisi, suggerimenti ed esito passano
    /// da ScenarioController/HUDService senza creare Canvas, camera o input locale nella scena.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EvacuationScenarioController : ScenarioController
    {
        [Tooltip("Director dello scenario Evacuation (MNG_EvacuationDirector).")]
        [SerializeField] private EvacuationDirector director;

        private bool subscribed;
        private bool missionEnded;

        private const string CoreScenePath = "Assets/_Project/Home/Level/Scenes/SCN_Core.unity";

        /// <summary>
        /// Evita che la prova diretta in Editor caricati un secondo SCN_Core quando è già aperto:
        /// due rig duplicati generano il MissingReferenceException di OVRManager.
        /// </summary>
        private void Awake()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).path != CoreScenePath)
                    continue;
                FieldInfo field = typeof(ScenarioController).GetField("loadCoreInEditor",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                    field.SetValue(this, false);
                return;
            }
        }

        /// <summary>Director usato dal ponte (assegnato anche a runtime se manca).</summary>
        public EvacuationDirector Director => director;

        /// <summary>Prepara il livello e avvia il Director quando la UI comune è pronta.</summary>
        protected override void OnScenarioStarted()
        {
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (director == null)
            {
                UnityEngine.Debug.LogError("[EV] EvacuationScenarioController: EvacuationDirector mancante.", this);
                return;
            }

            ApplyLevel();
            LinkSharedRig();
            Subscribe();
            director.ResetScenario();
            director.StartScenario();

            SetPhase(PHASE_SCAN);
            SetObjective(0);
            SetHint("Controlla la pianta, poi porta i colleghi alle scale di emergenza.");
        }

        /// <summary>Allinea il livello della UI comune con quello del Director.</summary>
        private void ApplyLevel()
        {
            EvacDifficulty level = Level == DifficultyLevel.Crisis
                ? EvacDifficulty.Crisis
                : Level == DifficultyLevel.Operational ? EvacDifficulty.Operational : EvacDifficulty.Guided;
            director.SetDifficulty(level);
        }

        /// <summary>Collega la radice del rig condiviso al Director (il rig arriva da SCN_Core).</summary>
        private void LinkSharedRig()
        {
            RigRegistration.Registered += HandleRigRegistered;
            RigRegistration rig = RigRegistration.Instance;
            if (rig != null)
                director.SetRigRoot(rig.transform);
        }

        /// <summary>Reagisce al rig registrato dopo il caricamento additivo di SCN_Core.</summary>
        private void HandleRigRegistered(RigRegistration rig)
        {
            if (director != null && rig != null)
                director.SetRigRoot(rig.transform);
        }

        /// <summary>Sottoscrive gli eventi del Director una sola volta.</summary>
        private void Subscribe()
        {
            if (subscribed)
                return;
            subscribed = true;
            director.PlanInspected += HandlePlanInspected;
            director.NpcCollected += HandleNpcCollected;
            director.ExitReported += HandleExitReported;
            director.HintRequested += HandleHintRequested;
            director.DangerLevelChanged += HandleDanger;
            director.ActionRecorded += HandleAction;
            director.ResultReady += HandleResult;
        }

        /// <summary>Rimuove le sottoscrizioni quando il componente viene distrutto.</summary>
        protected override void OnDestroy()
        {
            RigRegistration.Registered -= HandleRigRegistered;
            if (subscribed && director != null)
            {
                director.PlanInspected -= HandlePlanInspected;
                director.NpcCollected -= HandleNpcCollected;
                director.ExitReported -= HandleExitReported;
                director.HintRequested -= HandleHintRequested;
                director.DangerLevelChanged -= HandleDanger;
                director.ActionRecorded -= HandleAction;
                director.ResultReady -= HandleResult;
                subscribed = false;
            }
            base.OnDestroy();
        }

        /// <summary>Alla scadenza del timer della UI chiede al Director di chiudere la corsa.</summary>
        protected override void OnTimerExpired()
        {
            if (director != null && director.Phase == EvacPhase.Action)
                director.ForceTimeUp();
            else
                base.OnTimerExpired();
        }

        private void HandlePlanInspected(string planId)
        {
            SetObjective(1);
            Subtitle("Addetto", "Piano letto: uso le scale di emergenza.");
        }

        private void HandleNpcCollected(int room, int total)
        {
            Feedback("Collega unito alla fila", FeedbackKind.Positive);
            if (total >= 5)
                SetObjective(2);
        }

        private void HandleExitReported(EvacExit exit)
        {
            SetObjective(3);
            string text = exit == EvacExit.Emergency
                ? "Uscita di emergenza raggiunta"
                : "Hai usato le scale interne";
            Feedback(text, exit == EvacExit.Emergency ? FeedbackKind.Positive : FeedbackKind.Negative);
        }

        private void HandleHintRequested(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            switch (key)
            {
                case "evac.hint.plan": SetHint("Controlla la pianta sul muro."); break;
                case "evac.hint.room1": SetHint("Raccogli i 3 colleghi dell'ufficio 1."); break;
                case "evac.hint.room2": SetHint("Raccogli i 2 colleghi dell'ufficio 2."); break;
                case "evac.hint.stairs": SetHint("Usa la porta delle scale di emergenza."); break;
                case "evac.hint.noelevator": SetHint("Mai l'ascensore durante un'evacuazione."); break;
            }
        }

        private void HandleDanger(EvacDangerLevel level)
        {
            if (level == EvacDangerLevel.Down)
                Alert("Pericolo\nTorna sul percorso corretto", "ICN_Warning_Filled");
        }

        private void HandleAction(ActionLogEntry entry)
        {
            if (entry == null)
                return;
            switch (entry.ActionId)
            {
                case EvacActionIds.Lg06Elevator:
                    Alert("Ascensore\nUsa le scale di emergenza", "ICN_Warning_Filled");
                    break;
                case EvacActionIds.Lg10Timeout:
                    Alert("Tempo scaduto\nAffrettati verso l'uscita", "ICN_Warning_Filled");
                    break;
                case EvacActionIds.Lg03Room1Done:
                case EvacActionIds.Lg04Room2Done:
                    Feedback("Stanza controllata", FeedbackKind.Positive);
                    break;
            }
        }

        /// <summary>Chiude la missione una sola volta con punteggi e righe di debrief.</summary>
        private void HandleResult(ScenarioResult result)
        {
            if (missionEnded || result == null)
                return;
            missionEnded = true;

            OutcomeType outcome = result.Outcome == Outcome.Success
                ? OutcomeType.Positive
                : result.Outcome == Outcome.Partial ? OutcomeType.Partial : OutcomeType.Negative;

            int[] dimensions =
            {
                result.GetSubScore(Dimension.Safety),
                result.GetSubScore(Dimension.Awareness),
                result.GetSubScore(Dimension.Decision),
                result.GetSubScore(Dimension.Execution),
                result.GetSubScore(Dimension.Time)
            };

            var lines = new List<string>
            {
                outcome == OutcomeType.Positive
                    ? DebriefLinePrefix.GOOD + "Tutti i colleghi hanno raggiunto il punto di raccolta."
                    : outcome == OutcomeType.Partial
                        ? DebriefLinePrefix.BAD + "Evacuazione incompleta: mancano colleghi o il percorso non era corretto."
                        : DebriefLinePrefix.CRITICAL + "Percorso o ascensore sbagliato: riesamina la pianta."
            };

            EndMission(outcome, -1, dimensions, lines);
        }
    }
}
