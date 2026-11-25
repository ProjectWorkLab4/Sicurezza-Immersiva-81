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
    /// Richiede che lo stesso controller resti dentro per un tempo configurabile; uscire o cambiare mano
    /// azzera il conteggio. Al completamento chiama <see cref="ManDownNpc.AssessBreathing"/>: la logica e la
    /// registrazione restano quelle di <see cref="ManDownScenario"/>. Non aggiunge etichette, suggerimenti o
    /// indicatori di avanzamento e non dipende da input locale, rig locale o UI personalizzata.
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
            ResetDwell();
        }

        /// <summary>Si scollega dall'evento di missione alla disattivazione.</summary>
        private void OnDisable()
        {
            MissionSession.Started -= OnMissionStarted;
        }

        /// <summary>
        /// Controlla ogni frame le sole posi delle due mani del rig condiviso. Una sola mano dentro la zona
        /// accumula tempo; se esce o entra l'altra mano il conteggio riparte da zero. Non usa raycast né bottoni.
        /// </summary>
        private void Update()
        {
            if (npc == null || zone == null || !zone.enabled)
            {
                ResetDwell();
                return;
            }

            bool leftInside = IsControllerInside(Hand.Left);
            bool rightInside = IsControllerInside(Hand.Right);

            // Nessuna mano oppure entrambe: non c'è un singolo controller da conteggiare.
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

            if (completed) return;

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

        /// <summary>Riarmatura a ogni nuova missione, senza toccare lo scenario.</summary>
        private void OnMissionStarted()
        {
            ResetDwell();
        }
    }
}
