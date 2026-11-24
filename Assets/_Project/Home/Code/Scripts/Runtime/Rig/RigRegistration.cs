using System;
using SI81.Input;
using UnityEngine;

namespace SI81.Rig
{
    /// <summary>
    /// Registrazione del Player Rig: legge testa, origine del tracciamento e le due mani, le consegna all'unico
    /// <see cref="IRigUIBridge"/> e fornisce la posa aim in spazio mondo. Non cerca riferimenti per nome e non referenzia
    /// l'assembly UI: l'adattatore della UI si registra con <see cref="RegisterBridge"/> o viene trovato una volta in Start.
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Rig/Rig Registration")]
    public sealed class RigRegistration : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: prima di UIScreenManager e HUD.</summary>
        public const int EXECUTION_ORDER = -50;

        /// <summary>Istanza corrente del rig (null se non c'è un rig in scena).</summary>
        public static RigRegistration Instance { get; private set; }

        /// <summary>Lanciato quando il primo rig registra la sua istanza (serve allo spawn point caricato prima).</summary>
        public static event Action<RigRegistration> Registered;

        [Tooltip("Testa del giocatore (CenterEyeAnchor / camera).")]
        [SerializeField] private Transform head;
        [Tooltip("Origine del tracciamento (TrackingSpace / CameraOffset).")]
        [SerializeField] private Transform trackingOrigin;
        [Tooltip("Ancora del controller sinistro.")]
        [SerializeField] private Transform leftHand;
        [Tooltip("Ancora del controller destro.")]
        [SerializeField] private Transform rightHand;
        [Tooltip("Adattatore UI che implementa IRigUIBridge. Vuoto = viene cercato una volta in Start.")]
        [SerializeField] private MonoBehaviour uiBridge;

        private IRigUIBridge bridge;
        private MonoBehaviour bridgeBehaviour;
        private bool warnedIncomplete;
        private bool warnedNoBridge;

        /// <summary>Testa del giocatore.</summary>
        public Transform Head => head;
        /// <summary>Origine del tracciamento.</summary>
        public Transform TrackingOrigin => trackingOrigin;
        /// <summary>Mano sinistra.</summary>
        public Transform LeftHand => leftHand;
        /// <summary>Mano destra.</summary>
        public Transform RightHand => rightHand;
        /// <summary>Hand-off corrente (struct, nessuna allocazione).</summary>
        public RigHandoff Handoff => new RigHandoff(head, trackingOrigin, leftHand, rightHand);
        /// <summary>Adattatore UI registrato, o null.</summary>
        public IRigUIBridge Bridge => bridgeBehaviour != null ? bridge : null;

        /// <summary>
        /// Registra l'istanza, rifiuta i duplicati e risolve l'adattatore UI.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SI81 Rig] Secondo RigRegistration rifiutato: il rig condiviso è uno solo (SCN_Core). " +
                                 "Il GameObject duplicato viene disattivato.", this);
                gameObject.SetActive(false);
                return;
            }
            Instance = this;
            if (uiBridge != null) RegisterBridge(uiBridge as IRigUIBridge);
            Registered?.Invoke(this);
        }

        /// <summary>
        /// Azzera l'istanza e gli iscritti quando parte il Play Mode con Domain Reload disattivato.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            Registered = null;
        }

        /// <summary>
        /// Consegna il rig alla UI appena la scena è pronta.
        /// </summary>
        private void Start()
        {
            if (bridgeBehaviour == null) ResolveBridge();
            Apply();
        }

        /// <summary>
        /// Toglie l'istanza statica.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Registra l'adattatore UI (componente della UI). Chiamabile prima o dopo Start; applica subito l'hand-off.
        /// </summary>
        /// <param name="newBridge">Adattatore, o null per scollegare.</param>
        public void RegisterBridge(IRigUIBridge newBridge)
        {
            if (newBridge != null)
            {
                bridgeBehaviour = newBridge as MonoBehaviour;
                if (bridgeBehaviour == null)
                {
                    Debug.LogError("[SI81 Rig] IRigUIBridge deve essere un MonoBehaviour per poterne controllare il ciclo di vita.", this);
                    bridge = null;
                    return;
                }
                bridge = newBridge;
                warnedNoBridge = false;
            }
            else
            {
                bridge = null;
                bridgeBehaviour = null;
            }
            if (Application.isPlaying && isActiveAndEnabled) Apply();
        }

        /// <summary>
        /// Consegna di nuovo l'hand-off alla UI (rig cambiato o UI caricata dopo).
        /// </summary>
        public void Apply()
        {
            if (!Handoff.IsComplete)
            {
                if (!warnedIncomplete)
                {
                    warnedIncomplete = true;
                    Debug.LogError("[SI81 Rig] RigRegistration incompleto: assegna testa, tracking origin e le due mani.", this);
                }
                return;
            }
            if (bridgeBehaviour == null)
            {
                if (!warnedNoBridge)
                {
                    warnedNoBridge = true;
                    Debug.LogWarning("[SI81 Rig] Nessun IRigUIBridge registrato: la UI non riceve l'hand-off del rig.", this);
                }
                return;
            }
            bridge.ApplyRig(Handoff);
        }

        /// <summary>
        /// Converte la posa aim del controller dallo spazio di tracciamento allo spazio mondo.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        /// <param name="worldPosition">Posizione mondo.</param>
        /// <param name="worldRotation">Rotazione mondo.</param>
        /// <returns>true se il controller è tracciato e il rig ha l'origine.</returns>
        public bool TryGetAimPose(Hand hand, out Vector3 worldPosition, out Quaternion worldRotation)
        {
            worldPosition = default;
            worldRotation = default;
            InputService service = InputService.Instance;
            if (service == null || trackingOrigin == null) return false;
            if (!service.TryGetAimPose(hand, out Vector3 localPosition, out Quaternion localRotation)) return false;
            worldPosition = trackingOrigin.TransformPoint(localPosition);
            worldRotation = trackingOrigin.rotation * localRotation;
            return true;
        }

        /// <summary>
        /// Impulso aptico inoltrato all'adattatore UI (che applica il gate delle impostazioni).
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        /// <param name="amplitude">Ampiezza 0..1.</param>
        /// <param name="duration">Durata in secondi.</param>
        public void Pulse(Hand hand, float amplitude, float duration)
        {
            if (bridgeBehaviour == null) return;
            bridge.Pulse(hand, Mathf.Clamp01(amplitude), Mathf.Max(0f, duration));
        }

        /// <summary>
        /// Porta il rig in una posa (spawn point). La locomozione azzera la velocità verticale.
        /// </summary>
        /// <param name="position">Posizione della radice.</param>
        /// <param name="rotation">Rotazione della radice (yaw).</param>
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            RigLocomotion locomotion = GetComponent<RigLocomotion>();
            if (locomotion != null) locomotion.ResetVerticalVelocity();
        }

        /// <summary>
        /// Configura i quattro riferimenti. Usato dal builder del prefab e dai test.
        /// </summary>
        /// <param name="newHead">Testa.</param>
        /// <param name="newTrackingOrigin">Origine del tracciamento.</param>
        /// <param name="newLeftHand">Mano sinistra.</param>
        /// <param name="newRightHand">Mano destra.</param>
        public void Configure(Transform newHead, Transform newTrackingOrigin, Transform newLeftHand, Transform newRightHand)
        {
            head = newHead;
            trackingOrigin = newTrackingOrigin;
            leftHand = newLeftHand;
            rightHand = newRightHand;
            if (Application.isPlaying && isActiveAndEnabled && bridgeBehaviour != null) Apply();
        }

        /// <summary>
        /// Cerca una volta l'adattatore UI tra i MonoBehaviour della scena, senza ricerche per frame.
        /// </summary>
        private void ResolveBridge()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IRigUIBridge found)
                {
                    RegisterBridge(found);
                    return;
                }
            }
        }
    }
}
