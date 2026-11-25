using SI81.Input;
using UnityEngine;

namespace SI81.Interaction
{
    /// <summary>
    /// Interattore a raggio di una mano: legge la posa aim dall'Input Service, colpisce gli <see cref="Interactable"/>
    /// con un raycast fisico e consegna hover, select e presa alla mano. Un componente per mano, sulle ancore del rig.
    /// Il gate UI per mano e la sospensione World arrivano dall'Input Service: non esiste un secondo raggio UI.
    /// Non disegna nulla: il visuale del puntatore resta alla UI (vedi nota UI/world ray priority).
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Interaction/Ray Interactor")]
    public sealed class RayInteractor : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: dopo Input Service e locomozione, prima dei consumatori generici.</summary>
        public const int EXECUTION_ORDER = -800;

        private const int HIT_BUFFER_SIZE = 16;

        [Tooltip("Mano controllata da questo interattore.")]
        [SerializeField] private Hand hand = Hand.Right;
        [Tooltip("Origine del tracciamento (TrackingSpace): converte la posa aim in spazio mondo.")]
        [SerializeField] private Transform trackingOrigin;
        [Tooltip("Radice ignorata dal raycast (il rig stesso). Vuoto = radice della tracking origin.")]
        [SerializeField] private Transform ignoreRoot;
        [Tooltip("Livelli fisici su cui cercare oggetti interagibili.")]
        [SerializeField] private LayerMask interactionLayers = ~0;
        [Tooltip("Distanza massima del raggio in metri.")]
        [Min(0.1f)]
        [SerializeField] private float maxDistance = 5f;

        private readonly RaycastHit[] hits = new RaycastHit[HIT_BUFFER_SIZE];
        private InputService subscribed;
        private Interactable hovered;
        private Interactable selected;
        private Interactable grabbed;
        private bool pendingSelectPress;
        private bool pendingSelectRelease;
        private bool pendingGrabPress;
        private bool pendingGrabRelease;
        private Vector3 handPosition;
        private Quaternion handRotation;
        private bool hasHandPose;
        private Vector3 grabOffsetPosition;
        private Quaternion grabOffsetRotation;
        private Rigidbody grabbedBody;
        private bool grabbedBodyWasKinematic;
        private bool grabbedBodyUsedGravity;
        private bool warnedNoService;
        private bool warnedNoOrigin;

        /// <summary>Mano controllata dall'interattore.</summary>
        public Hand Hand => hand;
        /// <summary>Origine del tracciamento usata per la conversione in spazio mondo.</summary>
        public Transform TrackingOrigin => trackingOrigin;
        /// <summary>Radice esclusa dal raycast.</summary>
        public Transform IgnoreRoot => ignoreRoot;
        /// <summary>Distanza massima del raggio in metri.</summary>
        public float MaxDistance => maxDistance;
        /// <summary>Oggetto attualmente puntato dalla mano, o null.</summary>
        public Interactable Hovered => hovered;
        /// <summary>Oggetto selezionato con il grilletto, o null.</summary>
        public Interactable Selected => selected;
        /// <summary>Oggetto afferrato con l'impugnatura, o null.</summary>
        public Interactable Grabbed => grabbed;
        /// <summary>true se la posa aim della mano è valida in questo frame.</summary>
        public bool HasHandPose => hasHandPose;
        /// <summary>Posa mondo della mano usata dal raggio e dalla presa.</summary>
        public Vector3 HandPosition => handPosition;
        /// <summary>Rotazione mondo della mano usata dal raggio e dalla presa.</summary>
        public Quaternion HandRotation => handRotation;

        /// <summary>
        /// Configura mano e riferimenti. Usato dal builder del prefab e dai test.
        /// </summary>
        /// <param name="newHand">Mano controllata.</param>
        /// <param name="newTrackingOrigin">Origine del tracciamento.</param>
        /// <param name="newIgnoreRoot">Radice da ignorare nel raycast (null = radice della tracking origin).</param>
        public void Configure(Hand newHand, Transform newTrackingOrigin, Transform newIgnoreRoot)
        {
            hand = newHand;
            trackingOrigin = newTrackingOrigin;
            ignoreRoot = newIgnoreRoot != null
                ? newIgnoreRoot
                : newTrackingOrigin != null ? newTrackingOrigin.root : null;
        }

        /// <summary>
        /// Si iscrive all'Input Service.
        /// </summary>
        private void OnEnable()
        {
            Subscribe(InputService.Instance);
        }

        /// <summary>
        /// Riprova l'iscrizione se il servizio non era ancora pronto in OnEnable.
        /// </summary>
        private void Start()
        {
            if (subscribed == null) Subscribe(InputService.Instance);
        }

        /// <summary>
        /// Rilascia prese e selezioni e si disiscrive.
        /// </summary>
        private void OnDisable()
        {
            Subscribe(null);
            ReleaseAll();
        }

        /// <summary>
        /// Aggiorna hover e consegna le azioni della mano in questo frame. Nessuna allocazione.
        /// </summary>
        private void Update()
        {
            InputService current = InputService.Instance;
            if (current == null)
            {
                if (!warnedNoService)
                {
                    warnedNoService = true;
                    Debug.LogWarning("[SI81 Interaction] InputService assente: il raggio resta inattivo.", this);
                }
                return;
            }
            warnedNoService = false;
            if (subscribed != current) Subscribe(current);
            if (!hand.IsSingle()) return;

            if (trackingOrigin == null)
            {
                if (!warnedNoOrigin)
                {
                    warnedNoOrigin = true;
                    Debug.LogError("[SI81 Interaction] RayInteractor senza tracking origin: impossibile convertire la posa aim.", this);
                }
                ReleaseAll();
                return;
            }

            if (current.IsWorldSuppressed || Time.timeScale <= 0f)
            {
                ReleaseAll();
                return;
            }

            ValidateState();
            hasHandPose = ReadHandPose(current);

            Interactable target = null;
            if (hasHandPose && !current.IsPointingAtUI(hand)) target = FindTarget();
            SetHover(target);
            ProcessPending();
        }

        /// <summary>
        /// Fa seguire all'oggetto afferrato la posa della mano, mantenendo l'offset di presa.
        /// </summary>
        private void LateUpdate()
        {
            if (grabbed == null || !grabbed.FollowsHand || !hasHandPose) return;
            Vector3 position = handPosition + handRotation * grabOffsetPosition;
            Quaternion rotation = handRotation * grabOffsetRotation;
            if (grabbedBody != null)
            {
                grabbedBody.position = position;
                grabbedBody.rotation = rotation;
            }
            else
            {
                grabbed.transform.SetPositionAndRotation(position, rotation);
            }
        }

        /// <summary>
        /// Rilascia oggetti distrutti o disattivati senza aspettare il prossimo input.
        /// </summary>
        private void ValidateState()
        {
            // Un riferimento distrutto è "null" per Unity ma non per il campo: azzeralo davvero.
            if (selected == null) selected = null;
            else if (!selected.isActiveAndEnabled) ReleaseSelect();

            if (grabbed == null)
            {
                grabbed = null;
                grabbedBody = null;
                grabOffsetPosition = default;
                grabOffsetRotation = Quaternion.identity;
            }
            else if (!grabbed.isActiveAndEnabled)
            {
                ReleaseGrab();
            }
        }

        /// <summary>
        /// Legge la posa aim della mano e la converte in spazio mondo con la tracking origin.
        /// </summary>
        /// <param name="service">Servizio di input corrente.</param>
        /// <returns>true se il controller è tracciato.</returns>
        private bool ReadHandPose(InputService service)
        {
            if (!service.TryGetAimPose(hand, out Vector3 localPosition, out Quaternion localRotation)) return false;
            handPosition = trackingOrigin.TransformPoint(localPosition);
            handRotation = trackingOrigin.rotation * localRotation;
            return true;
        }

        /// <summary>
        /// Cerca il primo collider non ignorato lungo il raggio: se non è un <see cref="Interactable"/> non c'è hover
        /// (un muro blocca il raggio, non lo attraversa).
        /// </summary>
        /// <returns>Oggetto interagibile più vicino, o null.</returns>
        private Interactable FindTarget()
        {
            Vector3 direction = handRotation * Vector3.forward;
            int count = Physics.RaycastNonAlloc(handPosition, direction, hits, maxDistance, interactionLayers,
                QueryTriggerInteraction.Ignore);
            Interactable nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null) continue;
                if (IsIgnored(hit.collider.transform)) continue;
                if (hit.distance >= nearestDistance) continue;
                nearestDistance = hit.distance;
                nearest = hit.collider.GetComponentInParent<Interactable>(true);
            }
            return nearest != null && nearest.CanHover(hand) ? nearest : null;
        }

        /// <summary>
        /// true se il transform appartiene al rig del giocatore e va escluso dal raggio.
        /// </summary>
        /// <param name="candidate">Transform del collider colpito.</param>
        private bool IsIgnored(Transform candidate)
        {
            if (ignoreRoot == null || candidate == null) return false;
            return candidate == ignoreRoot || candidate.IsChildOf(ignoreRoot);
        }

        /// <summary>
        /// Cambia l'oggetto puntato e notifica ingresso/uscita.
        /// </summary>
        /// <param name="next">Nuovo oggetto puntato, o null.</param>
        private void SetHover(Interactable next)
        {
            if (hovered == next) return;
            if (hovered != null) hovered.HoverExit(hand);
            hovered = next;
            if (hovered != null) hovered.HoverEnter(hand);
        }

        /// <summary>
        /// Applica le azioni arrivate dall'Input Service dopo il raycast del frame. Prima le uscite, poi gli ingressi.
        /// </summary>
        private void ProcessPending()
        {
            if (pendingSelectRelease)
            {
                pendingSelectRelease = false;
                ReleaseSelect();
            }
            if (pendingGrabRelease)
            {
                pendingGrabRelease = false;
                ReleaseGrab();
            }
            if (pendingSelectPress)
            {
                pendingSelectPress = false;
                if (hovered != null && selected == null && hovered.CanSelect(hand))
                {
                    selected = hovered;
                    selected.SelectEnter(hand);
                }
            }
            if (pendingGrabPress)
            {
                pendingGrabPress = false;
                if (hovered != null && grabbed == null && hovered.CanGrab(hand)) BeginGrab(hovered);
            }
        }

        /// <summary>
        /// Inizia la presa e registra l'offset rispetto alla mano. Un solo oggetto per mano.
        /// </summary>
        /// <param name="target">Oggetto da afferrare.</param>
        private void BeginGrab(Interactable target)
        {
            grabbed = target;
            if (target.FollowsHand)
            {
                grabbedBody = target.GetComponentInParent<Rigidbody>();
                Transform followed = grabbedBody != null ? grabbedBody.transform : target.transform;
                Quaternion inverseHand = Quaternion.Inverse(handRotation);
                grabOffsetPosition = inverseHand * (followed.position - handPosition);
                grabOffsetRotation = inverseHand * followed.rotation;
                if (grabbedBody != null)
                {
                    grabbedBodyWasKinematic = grabbedBody.isKinematic;
                    grabbedBodyUsedGravity = grabbedBody.useGravity;
                    grabbedBody.isKinematic = true;
                    grabbedBody.useGravity = false;
                }
            }
            target.GrabEnter(hand);
        }

        /// <summary>
        /// Chiude la selezione corrente.
        /// </summary>
        private void ReleaseSelect()
        {
            if (selected == null) return;
            Interactable target = selected;
            selected = null;
            target.SelectExit(hand);
        }

        /// <summary>
        /// Chiude la presa corrente e ripristina il Rigidbody (kinematic, gravità, velocità).
        /// </summary>
        private void ReleaseGrab()
        {
            Interactable target = grabbed;
            Rigidbody body = grabbedBody;
            grabbed = null;
            grabbedBody = null;
            grabOffsetPosition = default;
            grabOffsetRotation = Quaternion.identity;
            if (target != null) target.GrabExit(hand);
            if (body == null) return;
            body.isKinematic = grabbedBodyWasKinematic;
            body.useGravity = grabbedBodyUsedGravity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Rilascia tutto e azzera gli input in attesa (pausa, menu, World sospeso, disattivazione).
        /// </summary>
        private void ReleaseAll()
        {
            pendingSelectPress = false;
            pendingSelectRelease = false;
            pendingGrabPress = false;
            pendingGrabRelease = false;
            ReleaseSelect();
            ReleaseGrab();
            SetHover(null);
            hasHandPose = false;
        }

        /// <summary>
        /// Cambia il servizio ascoltato, scollegando il precedente.
        /// </summary>
        /// <param name="next">Nuovo servizio, o null per scollegare.</param>
        private void Subscribe(InputService next)
        {
            if (subscribed == next) return;
            if (subscribed != null)
            {
                subscribed.SelectPerformed -= HandleSelectPerformed;
                subscribed.SelectCanceled -= HandleSelectCanceled;
                subscribed.GrabPerformed -= HandleGrabPerformed;
                subscribed.GrabCanceled -= HandleGrabCanceled;
            }
            subscribed = next;
            if (subscribed != null)
            {
                subscribed.SelectPerformed += HandleSelectPerformed;
                subscribed.SelectCanceled += HandleSelectCanceled;
                subscribed.GrabPerformed += HandleGrabPerformed;
                subscribed.GrabCanceled += HandleGrabCanceled;
            }
        }

        /// <summary>
        /// Grilletto premuto sulla mano dell'interattore: rimanda al momento del raycast.
        /// </summary>
        /// <param name="eventHand">Mano che ha premuto.</param>
        private void HandleSelectPerformed(Hand eventHand)
        {
            if (eventHand == hand) pendingSelectPress = true;
        }

        /// <summary>
        /// Grilletto rilasciato o selezione annullata (UI/contesto): rimanda al momento del raycast.
        /// </summary>
        /// <param name="eventHand">Mano che ha rilasciato.</param>
        private void HandleSelectCanceled(Hand eventHand)
        {
            if (eventHand == hand) pendingSelectRelease = true;
        }

        /// <summary>
        /// Impugnatura premuta sulla mano dell'interattore: rimanda al momento del raycast.
        /// </summary>
        /// <param name="eventHand">Mano che ha premuto.</param>
        private void HandleGrabPerformed(Hand eventHand)
        {
            if (eventHand == hand) pendingGrabPress = true;
        }

        /// <summary>
        /// Impugnatura rilasciata o presa annullata (contesto): rimanda al momento del raycast.
        /// </summary>
        /// <param name="eventHand">Mano che ha rilasciato.</param>
        private void HandleGrabCanceled(Hand eventHand)
        {
            if (eventHand == hand) pendingGrabRelease = true;
        }

        /// <summary>
        /// Mantiene la distanza massima positiva nell'inspector.
        /// </summary>
        private void OnValidate()
        {
            maxDistance = Mathf.Max(0.1f, maxDistance);
        }
    }
}
