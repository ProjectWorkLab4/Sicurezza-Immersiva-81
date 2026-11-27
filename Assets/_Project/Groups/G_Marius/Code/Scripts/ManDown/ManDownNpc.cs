using System;
using System.Collections.Generic;
using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// NPC a terra dello scenario Man Down. Gestisce la verifica della coscienza (GRIP su una spalla), espone
    /// la valutazione della respirazione richiamata da <see cref="ManDownBreathingZone"/> e, dopo la prima
    /// valutazione respiratoria, apre il popup decisionale comune per scegliere come posizionare la persona.
    ///
    /// La scelta logica è registrata da <see cref="ManDownScenario"/> anche se la posa visiva non esiste;
    /// <see cref="ManDownPoseController"/> applica la posa equivalente in modo istantaneo. Lo stato autorevole
    /// resta quello dello scenario: qui sono conservati solo i flag di presentazione e di ingresso VR, azzerati
    /// a ogni nuova missione. Non vengono creati UI, input, raycast, etichette o timer propri.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/NPC")]
    public sealed class ManDownNpc : MonoBehaviour
    {
        /// <summary>Alias del risultato registrato dal controller: l'NPC è incosciente in ogni partita.</summary>
        public const string CONSCIOUSNESS_RESULT = ManDownScenario.CONSCIOUSNESS_RESULT;
        /// <summary>Alias del risultato della respirazione normale registrato dal controller.</summary>
        public const string BREATHING_RESULT_NORMAL = ManDownScenario.BREATHING_RESULT_NORMAL;
        /// <summary>Alias del risultato della respirazione assente o anomala registrato dal controller.</summary>
        public const string BREATHING_RESULT_ABNORMAL = ManDownScenario.BREATHING_RESULT_ABNORMAL;

        /// <summary>Testo informativo dopo la verifica della coscienza.</summary>
        public const string CONSCIOUSNESS_INFO = "La persona è incosciente.";
        /// <summary>Testo informativo nel ramo con respirazione normale.</summary>
        public const string BREATHING_NORMAL_INFO = "La persona respira normalmente.";
        /// <summary>Testo informativo nel ramo con respirazione assente o anomala.</summary>
        public const string BREATHING_ABNORMAL_INFO = "La respirazione è assente o anomala.";
        /// <summary>Domanda del popup di posizionamento.</summary>
        public const string POSITION_QUESTION = "Come posizioni la persona?";
        /// <summary>Unica opzione di conferma dei popup informativi.</summary>
        public const string CONFIRM_OPTION = "OK";

        /// <summary>
        /// Cinque scelte di posizione, compresa quella di partenza (None = supina). Il popup ne mostra quattro
        /// in ordine casuale, escludendo sempre la posizione logica corrente.
        /// </summary>
        private static readonly ManDownPositionChoice[] BASE_POSITION_CHOICES =
        {
            ManDownPositionChoice.None,
            ManDownPositionChoice.SidePosition,
            ManDownPositionChoice.LegsRaised,
            ManDownPositionChoice.Prone,
            ManDownPositionChoice.Seated
        };

        [Header("Riferimenti di scena")]
        [Tooltip("Controller dello scenario su MNG_Scenario.")]
        [SerializeField] private ManDownScenario scenario;
        [Tooltip("Zona su una spalla dell'NPC, con Interactable configurato per il GRIP e FollowsHand disattivato.")]
        [SerializeField] private Interactable shoulderGrip;
        [Tooltip("Etichetta Interaction della spalla per la verifica della coscienza (stato 1).")]
        [SerializeField] private LabelAnchor shoulderLabel;
        [Tooltip("Etichetta Interaction della spalla per il cambio posizione (stato 3). Alternata con shoulderLabel: non sono mai visibili insieme.")]
        [SerializeField] private LabelAnchor positionLabel;
        [Tooltip("Controller delle pose statiche su questo stesso NPC. Opzionale: senza di esso la scelta logica viene comunque registrata.")]
        [SerializeField] private ManDownPoseController poseController;

        // Stato di presentazione e di ingresso, non di valutazione: non duplica i dati del controller.
        private bool gripArmed = true;
        private bool popupPending;
        private string pendingInfo;
        private ManDownPositionChoice[] displayedPositionChoices;
        private bool missingPanelWarned;

        // Stato dell'etichetta applicato e attesa che lo scenario azzeri i dati della partita precedente:
        // MissionSession.Started arriva prima dell'inizializzazione del controller.
        private int appliedLabelState = -1;
        private bool waitingForScenarioReset;

        /// <summary>Richiamato quando la posizione logica dell'NPC cambia effettivamente.</summary>
        public event Action PositionApplied;

        /// <summary>True se la valutazione della coscienza è già stata registrata in questa partita.</summary>
        public bool IsConsciousnessAssessed => scenario != null && scenario.IsConsciousnessChecked;

        /// <summary>Risultato della valutazione; è vuoto finché la verifica non è stata effettuata.</summary>
        public string VerifiedResult => IsConsciousnessAssessed ? CONSCIOUSNESS_RESULT : string.Empty;

        /// <summary>True se la valutazione della respirazione è già stata registrata in questa partita.</summary>
        public bool IsBreathingAssessed => scenario != null && scenario.IsBreathingChecked;

        /// <summary>Esito autorevole della respirazione di questa partita, noto prima della prima valutazione.</summary>
        public bool IsBreathingAbnormal => scenario != null && scenario.IsBreathingAbnormal;

        /// <summary>True se un popup decisionale è aperto (anche da altri componenti) o è stato richiesto da questo NPC.</summary>
        public bool IsPopupActive => IsPopupOpen();

        /// <summary>True se il pannello decisionale condiviso è disponibile per mostrare l'esito della valutazione.</summary>
        public bool IsDecisionPanelAvailable => FindFirstObjectByType<DecisionPanel>(FindObjectsInactive.Include) != null;

        /// <summary>Ramo rilevato: normale oppure assente/anomala. NotAssessed prima della verifica.</summary>
        public ManDownBreathingBranch BreathingBranch => scenario != null
            ? scenario.BreathingBranch
            : ManDownBreathingBranch.NotAssessed;

        /// <summary>Risultato della respirazione; vuoto finché la verifica non è stata effettuata.</summary>
        public string BreathingResult => IsBreathingAssessed ? scenario.BreathingResult : string.Empty;

        /// <summary>Secondi dall'inizio della missione della prima valutazione valida; -1 se non ancora effettuata.</summary>
        public float BreathingAssessedAt => scenario != null ? scenario.BreathingAssessedAt : -1f;

        /// <summary>Posizione logica corrente dell'NPC, registrata dallo scenario anche se la posa visiva manca.</summary>
        public ManDownPositionChoice CurrentPosition => scenario != null ? scenario.CurrentPosition : ManDownPositionChoice.None;

        /// <summary>Ultima posa visiva applicata al solo scheletro. Supine se non è stato applicato nulla.</summary>
        public ManDownPose CurrentVisualPose => poseController != null ? poseController.CurrentPose : ManDownPose.Supine;

        /// <summary>True se è in corso un popup di posizionamento aperto da questo componente.</summary>
        public bool IsPositionPopupOpen => popupPending && displayedPositionChoices != null;

        /// <summary>Sottoscrive GRIP, eventi di valutazione e inizio missione una sola volta all'attivazione.</summary>
        private void OnEnable()
        {
            if (shoulderGrip != null)
            {
                shoulderGrip.GrabEntered += OnShoulderGrip;
                shoulderGrip.GrabExited += OnShoulderGripExit;
            }
            if (scenario != null)
            {
                scenario.ConsciousnessAssessed += OnConsciousnessAssessed;
                scenario.BreathingAssessmentRecorded += OnBreathingAssessmentRecorded;
                scenario.MissionConcluded += OnMissionConcluded;
            }
            MissionSession.Started += OnMissionStarted;
            UpdateLabelState();
        }

        /// <summary>Rimuove le sottoscrizioni alla disattivazione, per non lasciare riferimenti alla partita precedente.</summary>
        private void OnDisable()
        {
            if (shoulderGrip != null)
            {
                shoulderGrip.GrabEntered -= OnShoulderGrip;
                shoulderGrip.GrabExited -= OnShoulderGripExit;
            }
            if (scenario != null)
            {
                scenario.ConsciousnessAssessed -= OnConsciousnessAssessed;
                scenario.BreathingAssessmentRecorded -= OnBreathingAssessmentRecorded;
                scenario.MissionConcluded -= OnMissionConcluded;
            }
            MissionSession.Started -= OnMissionStarted;
        }

        /// <summary>Mantiene la targhetta allineata allo stato autorevole dello scenario, non al solo grab.</summary>
        private void Update()
        {
            UpdateLabelState();
        }

        /// <summary>
        /// Valuta la coscienza solo se il pannello decisionale può presentare il messaggio: senza popup
        /// presentato correttamente la verifica non viene registrata.
        /// </summary>
        public void AssessConsciousness()
        {
            if (IsConsciousnessAssessed || scenario == null || !scenario.IsRunning) return;
            DecisionPanel panel = FindFirstObjectByType<DecisionPanel>(FindObjectsInactive.Include);
            if (panel == null || panel.IsOpen) return;
            scenario.VerifyConsciousness();
        }

        /// <summary>
        /// Valuta la respirazione chiamando il controller. Punto di ingresso di <see cref="ManDownBreathingZone"/>:
        /// ogni chiamata produce una voce nello storico e il popup informativo corrispondente.
        /// </summary>
        public void AssessBreathing()
        {
            if (!IsConsciousnessAssessed || scenario == null || !scenario.IsRunning) return;
            scenario.VerifyBreathing();
        }

        /// <summary>
        /// Registra la scelta logica e applica la posa visiva equivalente. Il problema di una posa non
        /// configurata non interrompe la missione: la scelta resta registrata e la scena visiva non cambia.
        /// </summary>
        /// <returns>true se la scelta logica è stata accettata.</returns>
        public bool SelectPosition(ManDownPositionChoice choice)
        {
            if (scenario == null || !scenario.IsRunning) return false;

            ManDownPositionChoiceRecord record = scenario.ChoosePosition(choice);
            if (record == null) return false;

            if (poseController != null)
            {
                ManDownPose pose;
                if (TryMapPose(choice, out pose)) poseController.ApplyPose(pose);
            }

            // La zona di respirazione deve azzerare il conteggio e attendere l'uscita del controller
            // prima di poter riavviare una valutazione nella nuova posa.
            PositionApplied?.Invoke();
            return true;
        }

        /// <summary>Associa una scelta logica alla posa visiva equivalente.</summary>
        private static bool TryMapPose(ManDownPositionChoice choice, out ManDownPose pose)
        {
            switch (choice)
            {
                case ManDownPositionChoice.SidePosition:
                    pose = ManDownPose.Recovery;
                    return true;
                case ManDownPositionChoice.LegsRaised:
                    pose = ManDownPose.LegsRaised;
                    return true;
                case ManDownPositionChoice.Prone:
                    pose = ManDownPose.Prone;
                    return true;
                case ManDownPositionChoice.Seated:
                    pose = ManDownPose.Seated;
                    return true;
                case ManDownPositionChoice.None:
                    pose = ManDownPose.Supine;
                    return true;
                default:
                    pose = ManDownPose.Supine;
                    return false;
            }
        }

        private static string GetPositionLabel(ManDownPositionChoice choice)
        {
            switch (choice)
            {
                case ManDownPositionChoice.None: return "Posizione iniziale (supina)";
                case ManDownPositionChoice.SidePosition: return "Posizione laterale";
                case ManDownPositionChoice.LegsRaised: return "Gambe sollevate";
                case ManDownPositionChoice.Prone: return "Posizione prona";
                case ManDownPositionChoice.Seated: return "Posizione seduta";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Un GRIP sulla spalla: prima della valutazione respiratoria verifica la coscienza come in TASK 03,
        /// dopo la valutazione apre il popup di posizionamento.Serve un nuovo GRIP effettivo (rilascio e
        /// nuova pressione) per ogni azione; se un popup è già aperto il GRIP non viene consumato.
        /// </summary>
        private void OnShoulderGrip(Hand hand)
        {
            ClearStalePopupState();
            if (!gripArmed || scenario == null || !scenario.IsRunning || IsPopupOpen()) return;

            gripArmed = false;
            if (!IsBreathingAssessed)
                AssessConsciousness();
            else
                OpenPositionPopup();
        }

        /// <summary>Riapre il GRIP dopo il rilascio, consentendo una nuova interazione o una correzione.</summary>
        private void OnShoulderGripExit(Hand hand)
        {
            gripArmed = true;
        }

        /// <summary>Apre il popup decisionale condiviso con le quattro opzioni in ordine casuale.</summary>
        private void OpenPositionPopup()
        {
            if (IsPopupOpen()) return;

            // Cinque posizioni in totale, quattro opzioni per popup: esclude solo la posizione logica corrente,
            // così la partenza (None) è sempre disponibile per tornare supini.
            var available = new List<ManDownPositionChoice>(BASE_POSITION_CHOICES.Length - 1);
            foreach (ManDownPositionChoice choice in BASE_POSITION_CHOICES)
                if (choice != scenario.CurrentPosition) available.Add(choice);

            ManDownPositionChoice[] order = available.ToArray();
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                ManDownPositionChoice swap = order[i];
                order[i] = order[j];
                order[j] = swap;
            }

            string[] labels = new string[order.Length];
            for (int i = 0; i < order.Length; i++) labels[i] = GetPositionLabel(order[i]);

            displayedPositionChoices = order;
            popupPending = true;
            scenario.SetDecisionPhase();

            if (!scenario.AskDecision(POSITION_QUESTION, labels, OnPositionChosen, 0f))
            {
                popupPending = false;
                displayedPositionChoices = null;
                scenario.RestoreActionPhase();
                if (!missingPanelWarned)
                {
                    missingPanelWarned = true;
                    Debug.LogWarning("[SI81 ManDown] HUD_DecisionPanel non disponibile: impossibile scegliere la posizione.", this);
                }
            }
        }

        /// <summary>
        /// Callback del popup di posizionamento: collega l'indice visualizzato alla scelta logica corrispondente.
        /// Non comunica al giocatore se la scelta è corretta o errata.
        /// </summary>
        private void OnPositionChosen(int index)
        {
            popupPending = false;
            scenario.RestoreActionPhase();

            ManDownPositionChoice[] order = displayedPositionChoices;
            displayedPositionChoices = null;

            if (index >= 0 && order != null && index < order.Length)
                SelectPosition(order[index]);

            FlushPendingInfo();
        }

        /// <summary>Dopo la prima verifica della coscienza nasconde l'etichetta e mostra il messaggio informativo.
        /// L'avanzamento degli obiettivi è gestito dallo scenario, che riceve già la verifica.</summary>
        private void OnConsciousnessAssessed()
        {
            if (shoulderLabel != null) shoulderLabel.Suppressed = true;
            ShowInfo(CONSCIOUSNESS_INFO);
        }

        /// <summary>
        /// Mostra l'esito della respirazione per ogni valutazione registrata, iniziale o successiva. Se un altro
        /// popup è aperto, il messaggio viene accodato e mostrato alla chiusura, senza sovrapposizioni.
        /// </summary>
        private void OnBreathingAssessmentRecorded(ManDownBreathingAssessmentRecord record)
        {
            ShowInfo(record.Abnormal ? BREATHING_ABNORMAL_INFO : BREATHING_NORMAL_INFO);
        }

        /// <summary>
        /// Recupera uno stato di popup rimasto attivo se il pannello condiviso è stato chiuso da un'altra
        /// interazione senza richiamare la callback di questo componente. Senza questo recupero il GRIP
        /// resterebbe bloccato per il resto della partita.
        /// </summary>
        private void ClearStalePopupState()
        {
            if (!popupPending && displayedPositionChoices == null) return;
            DecisionPanel panel = FindFirstObjectByType<DecisionPanel>(FindObjectsInactive.Include);
            if (panel != null && panel.IsOpen) return;

            popupPending = false;
            displayedPositionChoices = null;
            scenario?.RestoreActionPhase();
        }

        /// <summary>
        /// Tre stati derivati da `ManDownScenario`: 1 coscienza da verificare (GRIP/Afferra), 2 nessuna
        /// azione sulle spalle (targhette nascoste), 3 coscienza e respirazione registrate (Cambia posizione).
        /// Le due ancore vengono scritte insieme, quindi non possono essere visibili contemporaneamente.
        /// </summary>
        private void UpdateLabelState()
        {
            if (scenario == null) return;

            int state;
            if (waitingForScenarioReset)
            {
                // La partita precedente è ancora leggibile: mantieni lo stato iniziale finché non viene azzerata.
                if (scenario.IsConsciousnessChecked || scenario.IsBreathingChecked) state = 1;
                else { waitingForScenarioReset = false; state = 1; }
            }
            else if (!scenario.IsConsciousnessChecked) state = 1;
            else if (!scenario.IsBreathingChecked) state = 2;
            else state = 3;

            if (state == appliedLabelState) return;
            appliedLabelState = state;

            if (shoulderLabel != null) shoulderLabel.Suppressed = state != 1;
            if (positionLabel != null) positionLabel.Suppressed = state != 3;
        }

        /// <summary>true se il pannello decisionale condiviso è aperto o se questo componente ha appena richiesto un popup.</summary>
        private bool IsPopupOpen()
        {
            if (popupPending) return true;
            DecisionPanel panel = FindFirstObjectByType<DecisionPanel>(FindObjectsInactive.Include);
            return panel != null && panel.IsOpen;
        }

        /// <summary>
        /// Apre un popup informativo condiviso con una sola opzione di conferma. Se un popup è già aperto
        /// il testo viene accodato; se il pannello manca si ripiega sull'Alert condiviso.
        /// </summary>
        private void ShowInfo(string text)
        {
            if (scenario == null || !scenario.IsRunning || string.IsNullOrEmpty(text)) return;
            if (IsPopupOpen())
            {
                pendingInfo = text;
                return;
            }

            popupPending = true;
            if (!scenario.AskDecision(text, new[] { CONFIRM_OPTION }, OnInfoConfirmed, 0f))
            {
                popupPending = false;
                scenario.Alert(text);
            }
        }

        /// <summary>Chiusura di un popup informativo: mostra, se presente, il messaggio accodato.</summary>
        private void OnInfoConfirmed(int index)
        {
            popupPending = false;
            FlushPendingInfo();
        }

        private void FlushPendingInfo()
        {
            if (pendingInfo == null) return;
            string text = pendingInfo;
            pendingInfo = null;
            ShowInfo(text);
        }

        /// <summary>
        /// Alla conclusione della missione non devono restare popup o callback pendenti di questo NPC.
        /// La base chiude già il pannello decisionale condiviso; qui si azzerano i flag locali.
        /// </summary>
        private void OnMissionConcluded(ManDownScenarioResult result)
        {
            popupPending = false;
            displayedPositionChoices = null;
            pendingInfo = null;
            gripArmed = true;
        }

        /// <summary>
        /// A ogni nuova missione: ripristina la posa Supine, le interazioni e lo stato di presentazione.
        /// Non modifica i dati serializzati delle pose.
        /// </summary>
        private void OnMissionStarted()
        {
            waitingForScenarioReset = true;
            appliedLabelState = 1;
            if (shoulderLabel != null) shoulderLabel.Suppressed = false;
            if (positionLabel != null) positionLabel.Suppressed = true;
            gripArmed = true;
            popupPending = false;
            pendingInfo = null;
            displayedPositionChoices = null;
            missingPanelWarned = false;

            if (poseController != null)
            {
                if (!poseController.ApplyPose(ManDownPose.Supine)) poseController.RestoreInitialPose();
                poseController.ResetMissingPoseWarnings();
            }

            DecisionPanel panel = FindFirstObjectByType<DecisionPanel>(FindObjectsInactive.Include);
            if (panel != null && panel.IsOpen) panel.Close();
        }
    }
}
