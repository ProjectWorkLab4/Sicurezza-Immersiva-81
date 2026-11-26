using System;
using System.Collections.Generic;
using SI81.Input;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Rilevamento fisico delle compressioni toraciche con entrambi i controller del rig condiviso.
    /// La zona è un singolo BoxCollider trigger figlio di un osso del torace: segue il corpo e le sue
    /// dimensioni/offset sono modificabili dall'Inspector. La presenza è verificata con le sole ancore
    /// <see cref="RigRegistration.LeftHand"/> e <see cref="RigRegistration.RightHand"/>, non con eventi
    /// trigger o collider dei controller: quindi controller con più collider non generano rilevamenti
    /// duplicati e non viene aggiunto alcun collider al rig condiviso.
    ///
    /// L'asse di compressione è <c>transform.up</c> della zona; le due soglie sono quote lungo quell'asse
    /// rispetto al centro della zona. Il punto medio tra i controller è la riferimento per l'escursione,
    /// ma anche la posizione individuale di entrambe le mani deve attraversare le soglie: una mano sola
    /// non può simulare una compressione. Nessun pulsante, raycast, animazione o forza sul torace.
    ///
    /// Il componente produce solo dati grezzi (timestamp di pressioni, compressioni complete e tentativi
    /// invalidi) ed eventi. Non calcola ritmo, qualità, punteggio o progressione RCP/DAE: alla prima
    /// pressione chiama <see cref="ManDownScenario.StartCpr"/> solo per registrare l'azione, anche se la
    /// respirazione è normale (RCP errata). Il feedback aptico usa <see cref="RigRegistration.Pulse"/>,
    /// quindi il gate condiviso dei setting UI, senza un secondo sistema di haptics.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("SI81/Man Down/CPR Compression Zone")]
    public sealed class ManDownCpr : MonoBehaviour
    {
        private enum CycleState
        {
            /// <summary>Nessun ciclo: servono entrambe le mani in condizione di rilascio.</summary>
            Idle,
            /// <summary>Rilascio verificato: una pressione completa produce un evento.</summary>
            Armed,
            /// <summary>Pressione rilevata: in attesa del rilascio per chiudere la compressione.</summary>
            Pressed
        }

        [Header("Riferimenti di scena")]
        [Tooltip("Controller dello scenario su MNG_Scenario: fonte autorevole di tempo, fase e registrazione azione.")]
        [SerializeField] private ManDownScenario scenario;
        [Tooltip("NPC a terra: la RCP è disponibile dopo la valutazione respiratoria e in posizione supina.")]
        [SerializeField] private ManDownNpc npc;

        [Header("Soglie (metri lungo l'asse della zona, dal suo centro)")]
        [Tooltip("Soglia inferiore di pressione: il punto medio e entrambe le mani devono scenderle.")]
        [Min(0f)]
        [SerializeField] private float pressureThreshold = 0.03f;
        [Tooltip("Soglia superiore di rilascio: il punto medio e entrambe le mani devono superarla. Escursione iniziale 5 cm.")]
        [Min(0f)]
        [SerializeField] private float releaseThreshold = 0.08f;
        [Tooltip("Distanza massima consentita tra i due controller, in metri.")]
        [Min(0.01f)]
        [SerializeField] private float maxControllerDistance = 0.25f;

        [Header("Feedback aptico")]
        [Tooltip("Ampiezza dell'impulso su entrambi i controller alla pressione rilevata (0 = disattivato).")]
        [Range(0f, 1f)]
        [SerializeField] private float hapticAmplitude = 0.5f;
        [Tooltip("Durata dell'impulso aptico in secondi.")]
        [Min(0f)]
        [SerializeField] private float hapticDuration = 0.12f;

        [Header("Debug")]
        [Tooltip("Se true, registra in Console solo pressioni, compressioni complete e tentativi invalidi (non ogni frame).")]
        [SerializeField] private bool debugLog;

        private readonly List<float> pressureTimestamps = new List<float>();
        private readonly List<float> completedCompressionTimestamps = new List<float>();
        private readonly List<float> invalidAttemptTimestamps = new List<float>();

        private BoxCollider zone;
        private CycleState state = CycleState.Idle;
        private bool attemptInProgress;

        /// <summary>Pressione valida rilevata (timestamp di missione).</summary>
        public event Action<float> PressureDetected;
        /// <summary>Compressione completa: pressione seguita da rilascio (timestamp di missione).</summary>
        public event Action<float> CompressionCompleted;
        /// <summary>Ciclo interrotto prima del completamento (timestamp e motivo tecnico).</summary>
        public event Action<float, string> AttemptInvalidated;

        /// <summary>Soglia inferiore di pressione, in metri lungo l'asse della zona.</summary>
        public float PressureThreshold => pressureThreshold;
        /// <summary>Soglia superiore di rilascio, in metri lungo l'asse della zona.</summary>
        public float ReleaseThreshold => releaseThreshold;
        /// <summary>Distanza massima tra i controller, in metri.</summary>
        public float MaxControllerDistance => maxControllerDistance;
        /// <summary>Ampiezza dell'impulso aptico di pressione.</summary>
        public float HapticAmplitude => hapticAmplitude;
        /// <summary>Durata dell'impulso aptico di pressione.</summary>
        public float HapticDuration => hapticDuration;

        /// <summary>Timestamp di missione di ogni pressione rilevata.</summary>
        public IReadOnlyList<float> PressureTimestamps => pressureTimestamps;
        /// <summary>Timestamp di missione di ogni compressione completata.</summary>
        public IReadOnlyList<float> CompletedCompressionTimestamps => completedCompressionTimestamps;
        /// <summary>Timestamp di missione di ogni tentativo incompleto o invalido.</summary>
        public IReadOnlyList<float> InvalidAttemptTimestamps => invalidAttemptTimestamps;
        /// <summary>Numero di compressioni complete.</summary>
        public int CompletedCompressionCount => completedCompressionTimestamps.Count;
        /// <summary>Numero di tentativi incompleti o invalidi.</summary>
        public int InvalidAttemptCount => invalidAttemptTimestamps.Count;

        /// <summary>True se entrambe le mani sono in condizione di rilascio e una pressione può essere contata.</summary>
        public bool IsArmed => state == CycleState.Armed;
        /// <summary>True dopo una pressione valida, finché non arriva il rilascio.</summary>
        public bool IsPressurePending => state == CycleState.Pressed;
        /// <summary>Stato leggibile del ciclo per il debug in Inspector.</summary>
        public string CycleStateLabel => state.ToString();

        /// <summary>Quota attuale del controller sinistro lungo l'asse di compressione.</summary>
        public float LeftHeight => zone != null ? HeightAlongAxis(ControllerPosition(Hand.Left)) : 0f;
        /// <summary>Quota attuale del controller destro lungo l'asse di compressione.</summary>
        public float RightHeight => zone != null ? HeightAlongAxis(ControllerPosition(Hand.Right)) : 0f;
        /// <summary>Quota del punto medio tra i controller.</summary>
        public float MidpointHeight => (LeftHeight + RightHeight) * 0.5f;
        /// <summary>Distanza attuale tra i due controller (0 se uno dei due non è tracciato).</summary>
        public float ControllerDistance
        {
            get
            {
                Vector3 left = ControllerPosition(Hand.Left);
                Vector3 right = ControllerPosition(Hand.Right);
                return left == Vector3.zero || right == Vector3.zero ? 0f : Vector3.Distance(left, right);
            }
        }

        /// <summary>
        /// Tolleranza individuale derivata dall'escursione configurata: metà della distanza tra le soglie.
        /// Le singole mani devono restare entro questa tolleranza dalla soglia raggiunta dal punto medio.
        /// </summary>
        public float IndividualTolerance => Mathf.Max(0.005f, (releaseThreshold - pressureThreshold) * 0.5f);

        private void Awake()
        {
            zone = GetComponent<BoxCollider>();
        }

        private void OnEnable()
        {
            if (npc != null) npc.PositionApplied += OnNpcPositionApplied;
            if (scenario != null) scenario.MissionConcluded += OnMissionConcluded;
            MissionSession.Started += OnMissionStarted;
            ResetCycle();
        }

        private void OnDisable()
        {
            if (npc != null) npc.PositionApplied -= OnNpcPositionApplied;
            if (scenario != null) scenario.MissionConcluded -= OnMissionConcluded;
            MissionSession.Started -= OnMissionStarted;
            ResetCycle();
        }

        private void OnValidate()
        {
            releaseThreshold = Mathf.Max(0f, releaseThreshold);
            pressureThreshold = Mathf.Clamp(pressureThreshold, 0f, Mathf.Max(0f, releaseThreshold - 0.005f));
            maxControllerDistance = Mathf.Max(0.01f, maxControllerDistance);
        }

        /// <summary>
        /// Aggiorna il ciclo con le sole pose delle mani del rig condiviso. Ogni frame verifica prima i
        /// requisiti di utilizzo e la validità di posizionamento: se vengono a mancare, il ciclo in corso
        /// viene invalidato una sola volta e riparte dallo stato Idle.
        /// </summary>
        private void Update()
        {
            if (!CanOperate())
            {
                Invalidate("not_available");
                return;
            }

            Vector3 left;
            Vector3 right;
            string invalidReason;
            if (!TryGetValidHands(out left, out right, out invalidReason))
            {
                Invalidate(invalidReason);
                return;
            }

            float leftHeight = HeightAlongAxis(left);
            float rightHeight = HeightAlongAxis(right);
            float midpointHeight = (leftHeight + rightHeight) * 0.5f;
            float tolerance = IndividualTolerance;

            bool midpointAtRelease = midpointHeight >= releaseThreshold;
            bool bothAtRelease = midpointAtRelease &&
                                 leftHeight >= releaseThreshold - tolerance &&
                                 rightHeight >= releaseThreshold - tolerance;
            bool midpointAtPressure = midpointHeight <= pressureThreshold;
            bool bothAtPressure = midpointAtPressure &&
                                  leftHeight <= pressureThreshold + tolerance &&
                                  rightHeight <= pressureThreshold + tolerance;

            switch (state)
            {
                case CycleState.Idle:
                    if (bothAtRelease) state = CycleState.Armed;
                    break;

                case CycleState.Armed:
                    if (bothAtPressure)
                    {
                        state = CycleState.Pressed;
                        attemptInProgress = false;
                        OnPressure();
                    }
                    else
                    {
                        // Un abbassamento iniziato ma non arrivato alla soglia non è una compressione:
                        // se le mani tornano in alto viene contato come tentativo incompleto.
                        if (midpointHeight < releaseThreshold - tolerance) attemptInProgress = true;
                        if (attemptInProgress && bothAtRelease)
                        {
                            Invalidate("insufficient_depth");
                        }
                    }
                    break;

                case CycleState.Pressed:
                    // Il completamento richiede il rilascio di entrambe le mani, non solo del punto medio:
                    // una mano rimasta abbassata non chiude la compressione.
                    if (bothAtRelease)
                    {
                        OnCompressionComplete();
                        state = CycleState.Armed;
                    }
                    break;
            }
        }

        /// <summary>
        /// La meccanica è disponibile solo a missione attiva, dopo la valutazione respiratoria e con
        /// l'NPC in posizione supina. Non richiede respirazione anomala: la RCP su persona che respira
        /// viene registrata dal controller come azione errata per il futuro scoring.
        /// </summary>
        private bool CanOperate()
        {
            return scenario != null && npc != null && zone != null && zone.enabled &&
                   scenario.AcceptsActions && scenario.IsRunning &&
                   npc.IsBreathingAssessed && npc.CurrentPosition == ManDownPositionChoice.None;
        }

        /// <summary>
        /// Ottiene le due ancore controller del rig condiviso e verifica che siano entrambe dentro il
        /// volume e sufficientemente vicine. Non usa collider né eventi trigger: controller con più
        /// collider sono quindi gestiti come una sola posa per mano.
        /// </summary>
        private bool TryGetValidHands(out Vector3 left, out Vector3 right, out string invalidReason)
        {
            left = Vector3.zero;
            right = Vector3.zero;
            invalidReason = string.Empty;

            RigRegistration rig = RigRegistration.Instance;
            if (rig == null)
            {
                invalidReason = "no_rig";
                return false;
            }

            Transform leftAnchor = rig.LeftHand;
            Transform rightAnchor = rig.RightHand;
            if (leftAnchor == null || rightAnchor == null)
            {
                invalidReason = "no_controller";
                return false;
            }

            left = leftAnchor.position;
            right = rightAnchor.position;
            if (!zone.bounds.Contains(left) || !zone.bounds.Contains(right))
            {
                invalidReason = "outside_zone";
                return false;
            }

            if (Vector3.Distance(left, right) > maxControllerDistance)
            {
                invalidReason = "too_far";
                return false;
            }

            return true;
        }

        /// <summary>Quota di un punto lungo l'asse di compressione della zona (il suo asse Y locale).</summary>
        private float HeightAlongAxis(Vector3 worldPoint)
        {
            Vector3 origin = zone.transform.TransformPoint(zone.center);
            return Vector3.Dot(worldPoint - origin, zone.transform.up);
        }

        /// <summary>Posizione mondo di un'ancora controller del rig condiviso, o zero se assente.</summary>
        private Vector3 ControllerPosition(Hand hand)
        {
            RigRegistration rig = RigRegistration.Instance;
            if (rig == null) return Vector3.zero;
            Transform anchor = hand == Hand.Left ? rig.LeftHand : hand == Hand.Right ? rig.RightHand : null;
            return anchor != null ? anchor.position : Vector3.zero;
        }

        /// <summary>Tempo coerente con lo scenario: secondi dall'inizio effettivo della missione.</summary>
        private float Timestamp()
        {
            return scenario != null ? scenario.MissionElapsedSeconds : -1f;
        }

        /// <summary>
        /// Pressione raggiunta da entrambe le mani: registra il timestamp, emette l'evento, invia
        /// l'impulso aptico su entrambi i controller e registra l'azione RCP nel controller (una sola
        /// volta), anche quando la respirazione è normale.
        /// </summary>
        private void OnPressure()
        {
            float timestamp = Timestamp();
            pressureTimestamps.Add(timestamp);
            PressureDetected?.Invoke(timestamp);

            if (scenario != null && !scenario.IsCprStarted) scenario.StartCpr();
            PulseBothControllers();

            if (debugLog) Debug.Log($"[SI81 ManDown CPR] Pressione rilevata @ {timestamp:F2}s", this);
        }

        /// <summary>Compressione completa: solo dopo pressione e rilascio validi di entrambe le mani.</summary>
        private void OnCompressionComplete()
        {
            float timestamp = Timestamp();
            completedCompressionTimestamps.Add(timestamp);
            CompressionCompleted?.Invoke(timestamp);
            if (debugLog) Debug.Log($"[SI81 ManDown CPR] Compressione completa #{CompletedCompressionCount} @ {timestamp:F2}s", this);
        }

        /// <summary>
        /// Interrompe un ciclo attivo, conta al massimo un tentativo invalido per ciclo e richiede una
        /// nuova condizione di rilascio. Non fa nulla se non c'era un ciclo in corso.
        /// </summary>
        private void Invalidate(string reason)
        {
            if (state != CycleState.Idle || attemptInProgress)
            {
                float timestamp = Timestamp();
                invalidAttemptTimestamps.Add(timestamp);
                AttemptInvalidated?.Invoke(timestamp, reason);
                if (debugLog) Debug.Log($"[SI81 ManDown CPR] Tentativo invalido ({reason}) @ {timestamp:F2}s", this);
            }
            ResetCycle();
        }

        /// <summary>Azzaera lo stato del ciclo senza contare un tentativo (nuova missione/disattivazione).</summary>
        private void ResetCycle()
        {
            state = CycleState.Idle;
            attemptInProgress = false;
        }

        /// <summary>Impulso aptico breve su entrambi i controller tramite il bridge condiviso del rig.</summary>
        private void PulseBothControllers()
        {
            if (hapticAmplitude <= 0f || hapticDuration <= 0f) return;
            RigRegistration rig = RigRegistration.Instance;
            if (rig == null) return;
            rig.Pulse(Hand.Left, hapticAmplitude, hapticDuration);
            rig.Pulse(Hand.Right, hapticAmplitude, hapticDuration);
        }

        private void OnNpcPositionApplied()
        {
            Invalidate("position_changed");
        }

        private void OnMissionConcluded(ManDownScenarioResult result)
        {
            Invalidate("mission_concluded");
        }

        private void OnMissionStarted()
        {
            ResetCycle();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Gizmos: volume della zona e due soglie come piani lungo l'asse di compressione, più le posi
        /// delle mani (verdi dentro la zona, rosse fuori). Nessun log per frame.
        /// </summary>
        private void OnDrawGizmos()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.85f, 0.4f, 0.5f);
            Gizmos.DrawWireCube(box.center, box.size);

            Vector3 flatSize = new Vector3(box.size.x, 0f, box.size.z);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(box.center.x, box.center.y + releaseThreshold, box.center.z), flatSize);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(new Vector3(box.center.x, box.center.y + pressureThreshold, box.center.z), flatSize);
            Gizmos.matrix = Matrix4x4.identity;

            if (!Application.isPlaying) return;
            RigRegistration rig = RigRegistration.Instance;
            if (rig == null) return;
            DrawHand(rig.LeftHand);
            DrawHand(rig.RightHand);
        }

        private void DrawHand(Transform anchor)
        {
            if (anchor == null) return;
            Gizmos.color = GetComponent<BoxCollider>().bounds.Contains(anchor.position) ? Color.green : Color.red;
            Gizmos.DrawSphere(anchor.position, 0.02f);
        }
#endif
    }
}
