using SI81.Input;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Zona fisica davanti alla bocca dell'NPC per la valutazione della respirazione. Il BoxCollider trigger
    /// definisce il volume: la presenza è rilevata confrontando la posa delle sole due mani del rig condiviso
    /// con i bounds del collider, senza raycast, senza pulsanti e senza considerare altri oggetti.
    /// È disponibile solo dopo la verifica della coscienza. Richiede che lo stesso controller resti dentro
    /// per un tempo configurabile; uscire, cambiare mano, aprire un popup o cambiare posa azzera il conteggio.
    /// Dopo la chiusura del popup un controller ancora dentro avvia un nuovo conteggio completo senza dover
    /// uscire e rientrare. Al completamento chiama <see cref="ManDownNpc.AssessBreathing"/>: logica, storico e
    /// registrazione restano quelli di <see cref="ManDownScenario"/>. Nessuna etichetta, suggerimento o UI propria.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("SI81/Man Down/Breathing Zone")]
    public sealed class ManDownBreathingZone : MonoBehaviour
    {
        [Header("Riferimenti di scena")]
        [Tooltip("Componente ManDownNpc sull'NPC a terra (chiama AssessBreathing al completamento).")]
        [SerializeField] private ManDownNpc npc;

        [Header("Rilevamento")]
        [Tooltip("Permanenza continua dello stesso controller dentro la zona, in secondi (valore richiesto: 2,5).")]
        [Min(0.1f)]
        [SerializeField] private float requiredDwellSeconds = 2.5f;

        private BoxCollider zone;
        private Hand activeHand = Hand.Any;
        private float dwellSeconds;
        private bool completed;

        /// <summary>Tempo richiesto, in secondi, per completare la valutazione.</summary>
        public float RequiredDwellSeconds => requiredDwellSeconds;

        /// <summary>Mano attualmente dentro la zona; Any se la zona è vuota o occupata da entrambe.</summary>
        public Hand ActiveHand => activeHand;

        /// <summary>Secondi di permanenza accumulati per la mano corrente.</summary>
        public float DwellSeconds => dwellSeconds;

        /// <summary>True dopo il completamento, fino a quando la mano non esce dalla zona.</summary>
        public bool HasCompleted => completed;

        /// <summary>Ottiene il collider trigger e si iscrive all'inizio di ogni missione.</summary>
        private void Awake()
        {
            zone = GetComponent<BoxCollider>();
        }

        /// <summary>Azzera il rilevamento all'attivazione e a ogni nuova missione.</summary>
        private void OnEnable()
        {
            MissionSession.Started += OnMissionStarted;
            if (npc != null) npc.PositionApplied += OnPositionApplied;
            ResetDwell();
        }

        /// <summary>Si scollega dall'evento di missione alla disattivazione.</summary>
        private void OnDisable()
        {
            MissionSession.Started -= OnMissionStarted;
            if (npc != null) npc.PositionApplied -= OnPositionApplied;
        }

        /// <summary>
        /// Controlla ogni frame le sole posi delle due mani del rig condiviso. Una sola mano dentro la zona
        /// accumula tempo; se esce o entra l'altra mano il conteggio riparte da zero. Non usa raycast né bottoni.
        /// </summary>
        private void Update()
        {
            if (npc == null || zone == null || !zone.enabled || !npc.IsConsciousnessAssessed)
            {
                ResetDwell();
                return;
            }

            // Il pannello decisionale è condiviso: con qualsiasi popup aperto il tempo non deve accumularsi,
            // altrimenti la valutazione finisce durante un'altra scelta e le callback si sovrappongono.
            // Senza pannello non si completa: non si registra una valutazione il cui esito non può essere mostrato.
            if (npc.IsPopupActive || !npc.IsDecisionPanelAvailable)
            {
                ResetDwell();
                return;
            }

            bool leftInside = IsControllerInside(Hand.Left);
            bool rightInside = IsControllerInside(Hand.Right);

            // Nessuna mano oppure entrambe: non c'è un singolo controller da conteggiare e il tempo non corre.
            if (leftInside == rightInside)
            {
                ResetDwell();
                return;
            }

            Hand inside = leftInside ? Hand.Left : Hand.Right;
            if (inside != activeHand)
            {
                activeHand = inside;
                dwellSeconds = 0f;
                completed = false;
            }

            // Dopo un completamento il popup si chiude e il conteggio riparte da zero con lo stesso controller
            // ancora dentro: non serve uscire e rientrare per una nuova valutazione.
            if (completed)
            {
                ResetDwell();
                return;
            }

            dwellSeconds += Time.deltaTime;
            if (dwellSeconds < Mathf.Max(0.1f, requiredDwellSeconds)) return;

            completed = true;
            npc.AssessBreathing();
        }

        /// <summary>
        /// Vero se l'ancora del controller richiesto è dentro il volume del BoxCollider. Le due ancore
        /// <see cref="RigRegistration.LeftHand"/> e <see cref="RigRegistration.RightHand"/> sono le pose fisiche
        /// dei controller del rig condiviso: non è un raycast, non servono pulsanti e non vengono considerati
        /// altri oggetti della scena.
        /// </summary>
        /// <param name="hand">Mano da verificare.</param>
        private bool IsControllerInside(Hand hand)
        {
            RigRegistration rig = RigRegistration.Instance;
            if (rig == null) return false;

            Transform controller = hand == Hand.Left ? rig.LeftHand : hand == Hand.Right ? rig.RightHand : null;
            return controller != null && zone.bounds.Contains(controller.position);
        }

        /// <summary>Azzera mano attiva, tempo e completamento, riarmando una nuova valutazione.</summary>
        private void ResetDwell()
        {
            activeHand = Hand.Any;
            dwellSeconds = 0f;
            completed = false;
        }

        /// <summary>
        /// Cambio di posa dell'NPC: il trigger si è spostato con lo scheletro, quindi il conteggio in corso
        /// viene invalidato e riparte da zero senza richiedere l'uscita del controller.
        /// </summary>
        private void OnPositionApplied()
        {
            ResetDwell();
        }

        /// <summary>Riarmatura a ogni nuova missione, senza toccare lo scenario.</summary>
        private void OnMissionStarted()
        {
            ResetDwell();
        }
    }
}
