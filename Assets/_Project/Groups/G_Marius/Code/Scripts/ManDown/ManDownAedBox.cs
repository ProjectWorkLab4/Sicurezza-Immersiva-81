using System;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Stato della scatola DAE: disponibilità, posizione iniziale del dispositivo, etichetta comune e
    /// disattivazione delle interazioni a missione conclusa. Non duplica la randomizzazione: l'unica fonte
    /// autorevole è <see cref="ManDownScenario.IsAedAvailable"/>. I figli visivi sono separati dalla logica
    /// e possono essere sostituiti senza toccare i riferimenti di interazione.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/AED Box")]
    public sealed class ManDownAedBox : MonoBehaviour
    {
        [Header("Riferimenti di scena")]
        [Tooltip("Controller della missione su MNG_Scenario (fonte della disponibilità del DAE).")]
        [SerializeField] private ManDownScenario scenario;
        [Tooltip("Sportello con apertura manuale sulla maniglia.")]
        [SerializeField] private ManDownAedDoor door;
        [Tooltip("Radice di tutti gli elementi visivi: disattivata quando il DAE è assente.")]
        [SerializeField] private GameObject visuals;
        [Tooltip("Placeholder del dispositivo, riposto nel punto iniziale a ogni reset.")]
        [SerializeField] private GameObject aedDevice;
        [Tooltip("Punto di posizionamento iniziale del DAE, dentro la scatola.")]
        [SerializeField] private Transform aedSlot;
        [Tooltip("Dispositivo DAE: la scatola decide quando può essere afferrato, in base allo sportello.")]
        [SerializeField] private ManDownAedDevice device;
        [Tooltip("Etichetta Interaction comune sulla maniglia.")]
        [SerializeField] private LabelAnchor label;

        private bool doorGrabbed;
        private bool lastDeviceGate;

        private void OnEnable()
        {
            if (door != null) door.GrabStateChanged += OnDoorGrabStateChanged;
            if (scenario != null)
            {
                scenario.PhaseChanged += OnPhaseChanged;
                scenario.MissionConcluded += OnMissionConcluded;
            }
            MissionSession.Started += OnMissionStarted;

            ResetToInitial();
            // In Play Mode il componente può attivarsi dopo l'inizializzazione dello scenario.
            if (scenario != null && scenario.IsInitialized) ApplyAvailability();
            RefreshDeviceGate();
        }

        /// <summary>
        /// Aggiorna la possibilità di afferrare il dispositivo quando lo sportello supera la soglia
        /// di apertura. Dopo il recupero il dispositivo resta comunque indipendente dallo sportello.
        /// </summary>
        private void Update()
        {
            RefreshDeviceGate();
        }

        private void OnDisable()
        {
            if (door != null) door.GrabStateChanged -= OnDoorGrabStateChanged;
            if (scenario != null)
            {
                scenario.PhaseChanged -= OnPhaseChanged;
                scenario.MissionConcluded -= OnMissionConcluded;
            }
            MissionSession.Started -= OnMissionStarted;
        }

        /// <summary>
        /// Nuova missione: scatola chiusa, dispositivo dentro, interazioni disabilitate finché lo scenario
        /// non ha estratto la disponibilità. Non legge <c>IsAedAvailable</c> qui perché l'evento arriva
        /// prima dell'inizializzazione del controller.
        /// </summary>
        private void OnMissionStarted()
        {
            ResetToInitial();
        }

        /// <summary>
        /// L'inizializzazione dello scenario termina sempre con questa fase: solo a quel punto la
        /// randomizzazione 75/25 è già stata stabilita. Valido anche dopo un <c>ResetScenario()</c>.
        /// </summary>
        private void OnPhaseChanged(ManDownPhase next)
        {
            if (next == ManDownPhase.ConsciousnessAssessment) ApplyAvailability();
        }

        private void OnMissionConcluded(ManDownScenarioResult result)
        {
            if (door != null) door.SetInteractionEnabled(false);
            if (device != null) device.SetInteractionsDisabled();
            RefreshLabel();
        }

        private void OnDoorGrabStateChanged(bool grabbed)
        {
            doorGrabbed = grabbed;
            RefreshLabel();
        }

        /// <summary>Ripristina la configurazione iniziale, indipendentemente dall'esito della partita precedente.</summary>
        [ContextMenu("SI81/AED Box/Reset")]
        public void ResetToInitial()
        {
            // In Edit Mode la scatola deve restare visibile e modificabile: la rimozione visiva
            // avviene solo a runtime, quando la disponibilità è nota.
            bool playing = Application.isPlaying;
            doorGrabbed = false;
            if (door != null)
            {
                door.SetInteractionEnabled(!playing);
                door.ResetDoor();
            }
            PlaceDeviceAtSlot();
            if (device != null) device.ResetDevice();
            if (aedDevice != null) aedDevice.SetActive(!playing);
            if (visuals != null) visuals.SetActive(!playing);
            lastDeviceGate = false;
            RefreshLabel();
        }

        /// <summary>
        /// Applica la disponibilità già stabilita dallo scenario: con DAE assente la scatola, il
        /// dispositivo e le interazioni restano disattivati; con DAE presente tutto è attivo e chiuso.
        /// </summary>
        private void ApplyAvailability()
        {
            if (scenario == null) return;

            bool available = scenario.IsAedAvailable && !scenario.IsMissionConcluded;
            PlaceDeviceAtSlot();
            if (device != null) device.ResetDevice();
            if (aedDevice != null) aedDevice.SetActive(available);
            if (visuals != null) visuals.SetActive(available);
            if (door != null)
            {
                door.ResetDoor();
                door.SetInteractionEnabled(available);
            }
            RefreshDeviceGate();
            RefreshLabel();
        }

        private void PlaceDeviceAtSlot()
        {
            if (aedDevice == null || aedSlot == null) return;
            Transform t = aedDevice.transform;
            t.SetParent(aedSlot, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        private void RefreshDeviceGate()
        {
            if (device == null) return;
            bool available = scenario != null && scenario.IsAedAvailable && !scenario.IsMissionConcluded;
            bool gate = available && door != null && door.IsOpenEnoughForRetrieval;
            if (gate == lastDeviceGate) return;
            lastDeviceGate = gate;
            device.SetInteractionAllowed(gate);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            bool available = scenario != null && scenario.IsAedAvailable && !scenario.IsMissionConcluded;
            bool doorOpen = door != null && door.IsOpenEnoughForRetrieval;
            bool deviceReady = device != null && device.IsRetrieved;
            // Con lo sportello aperto precede l'etichetta del dispositivo; dopo il recupero
            // l'etichetta della scatola non torna a comparire.
            label.Suppressed = !available || doorGrabbed || doorOpen || deviceReady;
        }
    }
}
