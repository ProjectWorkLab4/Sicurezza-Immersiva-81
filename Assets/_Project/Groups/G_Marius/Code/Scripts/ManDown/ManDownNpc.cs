using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// NPC a terra dello scenario Man Down. Gestisce la valutazione della coscienza (GRIP su una spalla) e
    /// espone la valutazione della respirazione: <see cref="AssessBreathing"/> è richiamato dalla zona fisica
    /// <see cref="ManDownBreathingZone"/>. Integra i popup informativi condivisi e l'obiettivo "Verifica
    /// coscienza e respiro" senza creare UI, input, raycast o timer propri.
    /// Lo stato autorevole di coscienza e respirazione resta quello di <see cref="ManDownScenario"/>: qui sono
    /// conservati solo i flag di presentazione (popup già mostrato, obiettivo già completato), azzerati a ogni
    /// nuova missione. L'NPC non viene mosso né scosso.
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
        /// <summary>Unica opzione di conferma dei popup informativi.</summary>
        public const string CONFIRM_OPTION = "OK";
        /// <summary>Indice dell'obiettivo "Verifica coscienza e respiro" in SO_Mission_ManDown.</summary>
        private const int NPC_ASSESSMENT_OBJECTIVE = 1;

        [Header("Riferimenti di scena")]
        [Tooltip("Controller dello scenario su MNG_Scenario.")]
        [SerializeField] private ManDownScenario scenario;
        [Tooltip("Zona su una spalla dell'NPC, con Interactable configurato per il GRIP e FollowsHand disattivato.")]
        [SerializeField] private Interactable shoulderGrip;
        [Tooltip("Etichetta Interaction della spalla, nascosta dopo la prima verifica valida.")]
        [SerializeField] private LabelAnchor shoulderLabel;

        // Stato di presentazione, non di valutazione: non duplica i dati del controller.
        private bool breathingPopupShown;
        private bool npcObjectiveCompleted;

        /// <summary>True se la valutazione della coscienza è già stata registrata in questa partita.</summary>
        public bool IsConsciousnessAssessed => scenario != null && scenario.IsConsciousnessChecked;

        /// <summary>Risultato della valutazione; è vuoto finché la verifica non è stata effettuata.</summary>
        public string VerifiedResult => IsConsciousnessAssessed ? CONSCIOUSNESS_RESULT : string.Empty;

        /// <summary>True se la valutazione della respirazione è già stata registrata in questa partita.</summary>
        public bool IsBreathingAssessed => scenario != null && scenario.IsBreathingChecked;

        /// <summary>
        /// Ramo rilevato: normale oppure assente/anomala. NotAssessed prima della verifica.
        /// Lo stato autorevole resta quello del controller.
        /// </summary>
        public ManDownBreathingBranch BreathingBranch => scenario != null
            ? scenario.BreathingBranch
            : ManDownBreathingBranch.NotAssessed;

        /// <summary>Risultato della respirazione; vuoto finché la verifica non è stata effettuata.</summary>
        public string BreathingResult => IsBreathingAssessed ? scenario.BreathingResult : string.Empty;

        /// <summary>
        /// Secondi dall'inizio della missione della prima valutazione valida della respirazione; -1 se non ancora effettuata.
        /// </summary>
        public float BreathingAssessedAt => scenario != null ? scenario.BreathingAssessedAt : -1f;

        /// <summary>Sottoscrive GRIP, eventi di valutazione e inizio missione una sola volta all'attivazione.</summary>
        private void OnEnable()
        {
            if (shoulderGrip != null) shoulderGrip.GrabEntered += OnShoulderGrip;
            if (scenario != null)
            {
                scenario.ConsciousnessAssessed += OnConsciousnessAssessed;
                scenario.BreathingAssessed += OnBreathingAssessed;
            }
            MissionSession.Started += OnMissionStarted;
        }

        /// <summary>Rimuove le sottoscrizioni alla disattivazione, per non lasciare riferimenti alla partita precedente.</summary>
        private void OnDisable()
        {
            if (shoulderGrip != null) shoulderGrip.GrabEntered -= OnShoulderGrip;
            if (scenario != null)
            {
                scenario.ConsciousnessAssessed -= OnConsciousnessAssessed;
                scenario.BreathingAssessed -= OnBreathingAssessed;
            }
            MissionSession.Started -= OnMissionStarted;
        }

        /// <summary>
        /// Valuta la coscienza chiamando il controller. Ignora duplicati, riferimenti mancanti e azioni fuori
        /// dalla missione attiva. ManDownScenario registra l'azione CONSCIOUSNESS_CHECK con timestamp e risultato.
        /// </summary>
        public void AssessConsciousness()
        {
            if (IsConsciousnessAssessed || scenario == null || !scenario.IsRunning) return;

            scenario.VerifyConsciousness();
        }

        /// <summary>
        /// Valuta la respirazione chiamando il controller. È il punto di ingresso pubblico richiamato da
        /// <see cref="ManDownBreathingZone"/>; non legge input né fa raycast. Non conserva stato di valutazione
        /// e può essere richiamato più volte: il controller registra una sola BREATHING_CHECK, mantiene il
        /// timestamp della prima valutazione valida e riemette BreathingAssessed. La valutazione non blocca
        /// le altre azioni del giocatore.
        /// </summary>
        public void AssessBreathing()
        {
            if (scenario == null || !scenario.IsRunning) return;

            scenario.VerifyBreathing();
        }

        /// <summary>
        /// Dopo la prima verifica valida della coscienza nasconde l'etichetta della spalla e mostra il messaggio
        /// informativo. Non suggerisce di controllare la respirazione e non avanza l'obiettivo da solo se la
        /// respirazione non è ancora stata valutata.
        /// </summary>
        private void OnConsciousnessAssessed()
        {
            if (shoulderLabel != null) shoulderLabel.Suppressed = true;
            ShowInfo(CONSCIOUSNESS_INFO);
            TryCompleteNpcObjective();
        }

        /// <summary>
        /// Mostra l'esito della respirazione una sola volta per missione: il controller riemette l'evento quando
        /// la valutazione viene ripetuta, ma non deve comparire un secondo popup. Non suggerisce interventi.
        /// </summary>
        /// <param name="abnormal">true se la respirazione è assente o anomala.</param>
        private void OnBreathingAssessed(bool abnormal)
        {
            if (!breathingPopupShown)
            {
                breathingPopupShown = true;
                ShowInfo(abnormal ? BREATHING_ABNORMAL_INFO : BREATHING_NORMAL_INFO);
            }
            TryCompleteNpcObjective();
        }

        /// <summary>
        /// Completa l'obiettivo "Verifica coscienza e respiro" quando entrambe le valutazioni sono registrate,
        /// indipendentemente dall'ordine in cui sono state eseguite. Non blocca nessuna azione successiva.
        /// </summary>
        private void TryCompleteNpcObjective()
        {
            if (npcObjectiveCompleted || scenario == null || !scenario.IsRunning) return;
            if (!scenario.IsConsciousnessChecked || !scenario.IsBreathingChecked) return;

            npcObjectiveCompleted = true;
            scenario.SetObjective(NPC_ASSESSMENT_OBJECTIVE);
            scenario.CompleteObjective();
        }

        /// <summary>
        /// Apre un popup informativo condiviso con una sola opzione di conferma. Usa solo AskDecision della
        /// classe base, senza UI, prefab o asset condivisi nuovi; se il pannello non è disponibile ripiega
        /// sull'Alert condiviso. Il timeout del pannello è 0, quindi il timer della missione non viene toccato.
        /// </summary>
        /// <param name="text">Testo informativo in italiano.</param>
        private void ShowInfo(string text)
        {
            if (scenario == null || !scenario.IsRunning) return;

            if (!scenario.AskDecision(text, new[] { CONFIRM_OPTION }, OnInfoConfirmed, 0f))
                scenario.Alert(text);
        }

        /// <summary>Conferma del popup informativo: non registra azioni né cambia lo stato dello scenario.</summary>
        /// <param name="index">Indice dell'opzione scelta (0 = OK).</param>
        private void OnInfoConfirmed(int index)
        {
            // Unico pulsante: la conferma chiude solo il pannello.
        }

        /// <summary>Ripristina etichetta e flag di presentazione all'inizio di ogni missione.</summary>
        private void OnMissionStarted()
        {
            if (shoulderLabel != null) shoulderLabel.Suppressed = false;
            breathingPopupShown = false;
            npcObjectiveCompleted = false;
        }

        /// <summary>Converte il GRIP sulla spalla in una valutazione della coscienza.</summary>
        /// <param name="hand">Mano che ha premuto il GRIP; non è rilevante per la valutazione.</param>
        private void OnShoulderGrip(Hand hand)
        {
            AssessConsciousness();
        }
    }
}
