using System;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Ponte tra il telefono e il popup decisionale condiviso. Non costruisce UI: apre
    /// <c>HUD_DecisionPanel</c> tramite <see cref="ScenarioController.AskDecision"/> e instrada le
    /// risposte ai metodi già esistenti di <see cref="ManDownPhone"/> e <see cref="ManDownScenario"/>.
    /// Nessun timeout interno: con <c>timeoutSeconds=0</c> non c'è barra né scadenza del pannello;
    /// se la missione finisce mentre il popup è aperto, la UI comune lo chiude da sé senza callback.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/Phone UI")]
    public sealed class ManDownPhoneUi : MonoBehaviour
    {
        /// <summary>Domanda mostrata nel popup.</summary>
        public const string QUESTION = "Chiami il 112?";

        /// <summary>Opzioni mostrate nel popup: indice 0 = Sì, indice 1 = No.</summary>
        public static readonly string[] OPTIONS = { "Sì", "No" };

        [Header("Riferimenti di scena")]
        [Tooltip("Componente ManDownPhone sullo stesso telefono.")]
        [SerializeField] private ManDownPhone phone;
        [Tooltip("Controller della missione su MNG_Scenario.")]
        [SerializeField] private ManDownScenario scenario;
        [Tooltip("Etichetta Interaction del telefono, nascosta durante la presa e dopo la chiamata.")]
        [SerializeField] private LabelAnchor label;

        // true mentre il telefono è afferrato: l'etichetta resta nascosta anche dopo un "No".
        private bool isGrabbed;

        /// <summary>Si iscrive alla richiesta del telefono e all'avvio della missione.</summary>
        private void OnEnable()
        {
            if (phone != null) phone.InteractionRequested += OnInteractionRequested;
            if (scenario != null) scenario.EmergencyCalled += OnEmergencyCalled;
            MissionSession.Started += OnMissionStarted;
            ApplyLabelState();
        }

        /// <summary>Si scollega dagli eventi quando il componente si disattiva.</summary>
        private void OnDisable()
        {
            if (phone != null) phone.InteractionRequested -= OnInteractionRequested;
            if (scenario != null) scenario.EmergencyCalled -= OnEmergencyCalled;
            MissionSession.Started -= OnMissionStarted;
        }

        /// <summary>Apre il popup comune quando il telefono richiede l'interazione.</summary>
        private void OnInteractionRequested()
        {
            if (phone == null || scenario == null) return;

            // Stato globale: dopo un "Sì" accettato nessun telefono deve aprire il popup.
            if (phone.IsCallMade || scenario.IsCall112Made || !scenario.IsRunning)
            {
                phone.AnswerNo();
                return;
            }

            // Il pannello decisionale è condiviso: una seconda Show() sovrascriverebbe la callback del popup
            // già aperto (per esempio quello delle posizioni), lasciandone uno stato pendente per sempre.
            DecisionPanel openPanel = FindFirstObjectByType<DecisionPanel>(FindObjectsInactive.Include);
            if (openPanel != null && openPanel.IsOpen)
            {
                phone.AnswerNo();
                return;
            }

            if (!scenario.AskDecision(QUESTION, OPTIONS, OnDecision, 0f))
            {
                // Senza HUD_DecisionPanel la scelta non può essere presentata: si comporta come "No"
                // e lascia il telefono disponibile per un tentativo successivo.
                phone.AnswerNo();
            }
        }

        /// <summary>Instrada la scelta del popup: Sì registra la chiamata, No e timeout non registrano.</summary>
        /// <param name="index">0 = Sì, 1 = No, -1 = scadenza del pannello se ne fosse impostata una.</param>
        private void OnDecision(int index)
        {
            if (index == 0)
            {
                if (phone != null && phone.AnswerYes() && scenario != null) scenario.Call112();
            }
            else if (phone != null)
            {
                // "No" e DecisionPanel.TIMEOUT_INDEX: nessuna azione registrata, nuova interazione possibile.
                phone.AnswerNo();
            }

            ApplyLabelState();
        }

        /// <summary>Nasconde l'etichetta su tutti i telefoni quando la chiamata viene registrata.</summary>
        private void OnEmergencyCalled()
        {
            ApplyLabelState();
        }

        /// <summary>
        /// Ripristina l'etichetta all'inizio di ogni missione. Non legge lo stato dello scenario perché
        /// <see cref="MissionSession.Started"/> arriva prima dell'inizializzazione del controller: una nuova
        /// partita non ha mai una chiamata registrata.
        /// </summary>
        private void OnMissionStarted()
        {
            isGrabbed = false;
            if (label != null) label.Suppressed = false;
        }

        /// <summary>Nasconde l'etichetta mentre il telefono è afferrato.</summary>
        public void SuppressLabel()
        {
            isGrabbed = true;
            if (label != null) label.Suppressed = true;
        }

        /// <summary>Rimostra l'etichetta al rilascio, salvo dopo una chiamata già confermata.</summary>
        public void ShowLabel()
        {
            isGrabbed = false;
            ApplyLabelState();
        }

        /// <summary>
        /// Applica lo stato dell'etichetta: resta nascosta durante la presa e dopo il 112 chiamato,
        /// perché lo stato autorevole è `ManDownScenario.IsCall112Made` per tutti i telefoni.
        /// </summary>
        private void ApplyLabelState()
        {
            if (label == null) return;
            label.Suppressed = isGrabbed
                || (scenario != null && scenario.IsCall112Made)
                || (phone != null && phone.IsCallMade);
        }
    }
}
