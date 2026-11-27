using System;
using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Elettrodo provvisorio del DAE: grab condiviso come i telefoni, rimozione magnetica dalle zone
    /// dell'NPC e rilascio libero quando non aggancia. Lo stato locale (recuperato, zona occupata)
    /// è notificato al dispositivo, che lo rielabora nel controller dello scenario. Nessun cavo
    /// simulato qui (i cavi sono gestiti da <see cref="ManDownAedLeads"/>), nessuna analisi, scarica,
    /// punteggio o UI personalizzata.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/AED Electrode")]
    public sealed class ManDownAedElectrode : MonoBehaviour
    {
        [Header("Riferimenti di scena")]
        [Tooltip("Dispositivo DAE di appartenenza: fonte di disponibilità e accensione.")]
        [SerializeField] private ManDownAedDevice device;
        [Tooltip("Zone magnetiche sull'NPC.")]
        [SerializeField] private ManDownAedAttachments attachments;
        [Tooltip("Interactable sulla radice dell'elettrodo.")]
        [SerializeField] private Interactable interaction;
        [Tooltip("Etichetta Interaction comune dell'elettrodo.")]
        [SerializeField] private LabelAnchor label;
        [Tooltip("Slot iniziale sul DAE: è la casa dell'elettrodo quando non è agganciato.")]
        [SerializeField] private Transform homeSlot;
        [Tooltip("Indice dello slot iniziale sul DAE (0 = A, 1 = B).")]
        [SerializeField] private int slotIndex;

        [Header("Feedback visivo")]
        [Tooltip("Renderer della pad: vira al colore sottostante quando l'elettrodo è agganciato.")]
        [SerializeField] private Renderer padRenderer;
        [Tooltip("Colore della pad quando l'elettrodo snap sulla zona dell'NPC.")]
        [SerializeField] private Color snappedColor = new Color(0.10f, 0.90f, 0.25f, 1f);

        [Header("Diagnostica")]
        [Tooltip("Log mirato solo al momento del rilascio: distanze, tolleranza, posa e esito. Nessun log per frame.")]
        [SerializeField] private bool debugRelease = true;

        /// <summary>Cambiato stato di recupero o di aggancio dell'elettrodo.</summary>
        public event Action StateChanged;

        private Rigidbody body;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private Vector3 initialLocalScale;
        private bool poseCaptured;

        private bool retrieved;
        private int attachedZone = -1;
        private bool grabbed;
        private bool interactionsDisabled;
        private MaterialPropertyBlock padBlock;

        /// <summary>true dopo il primo GRIP valido.</summary>
        public bool IsRetrieved => retrieved;
        /// <summary>Indice della zona occupata, o -1 se l'elettrodo è libero.</summary>
        public int AttachedZone => attachedZone;
        /// <summary>true se l'elettrodo è agganciato a una zona.</summary>
        public bool IsAttached => attachedZone >= 0;
        /// <summary>Indice dello slot iniziale sul DAE.</summary>
        public int SlotIndex => slotIndex;

        private void Awake()
        {
            CapturePose();
            body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            CapturePose();
            if (body == null) body = GetComponent<Rigidbody>();
            if (interaction != null)
            {
                interaction.GrabEntered += OnGrabEntered;
                interaction.GrabExited += OnGrabExited;
            }
            if (device != null)
            {
                device.Retrieved += OnDeviceStateChanged;
                device.PoweredOn += OnDeviceStateChanged;
            }
            MissionSession.Started += OnMissionStarted;
            RigRegistration.Registered += OnRigRegistered;
            IgnorePlayerBodyCollisions();
            RefreshAvailability();
            ApplyVisualState();
        }

        private void OnDisable()
        {
            if (interaction != null)
            {
                interaction.GrabEntered -= OnGrabEntered;
                interaction.GrabExited -= OnGrabExited;
            }
            if (device != null)
            {
                device.Retrieved -= OnDeviceStateChanged;
                device.PoweredOn -= OnDeviceStateChanged;
            }
            MissionSession.Started -= OnMissionStarted;
            RigRegistration.Registered -= OnRigRegistered;
        }

        private void OnDestroy()
        {
            StateChanged = null;
        }

        private void CapturePose()
        {
            if (poseCaptured) return;
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            initialLocalScale = transform.localScale;
            poseCaptured = true;
        }

        /// <summary>
        /// Riassesta la fisica DOPO che il RayInteractor ha ripristinato lo stato catturato al GRIP.
        /// <see cref="RayInteractor.ReleaseGrab"/> chiama <c>GrabExit</c> e subito dopo riscrive
        /// <c>isKinematic</c>/<c>useGravity</c>: per questo l'aggancio va consolidato in LateUpdate,
        /// non nel callback di rilascio. Nessun log per frame.
        /// </summary>
        private void LateUpdate()
        {
            if (!retrieved || interactionsDisabled) return;
            if (interaction != null && interaction.IsGrabbed) return;
            if (body == null) return;

            if (attachedZone >= 0)
            {
                Transform zone = attachments != null ? attachments.Zone(attachedZone) : null;
                if (zone != null && (body.position - zone.position).sqrMagnitude > 1e-8f)
                {
                    body.position = zone.position;
                    body.rotation = zone.rotation;
                }
                if (body.isKinematic && !body.useGravity) return;
                // Azzera le velocità prima di rendere il corpo cinematico: PhysX non accetta
                // l'assegnazione di velocità su un body già cinematico.
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.useGravity = false;
                return;
            }

            if (!body.isKinematic) return;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        /// <summary>Ricalcola se l'elettrodo può essere afferrato in base al DAE acceso.</summary>
        public void RefreshAvailability()
        {
            RefreshInteraction();
            RefreshLabel();
        }

        /// <summary>Disattiva presa ed etichetta a missione conclusa.</summary>
        public void SetInteractionsDisabled()
        {
            interactionsDisabled = true;
            RefreshInteraction();
            RefreshLabel();
        }

        /// <summary>
        /// Ripristina lo slot iniziale sul DAE, la fisica e gli stati. Non ricalcola la posa iniziale
        /// se già catturata: un elettrodo spostato non diventa la nuova posa di reset.
        /// </summary>
        [ContextMenu("SI81/AED Electrode/Reset")]
        public void ResetElectrode()
        {
            if (attachments != null) attachments.Detach(this);
            retrieved = false;
            attachedZone = -1;
            grabbed = false;
            interactionsDisabled = false;
            // La posa iniziale è relativa allo slot sul DAE: ripristina prima la gerarchia.
            if (homeSlot != null) transform.SetParent(homeSlot, false);
            if (!poseCaptured) CapturePose();

            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            transform.localScale = initialLocalScale;

            if (body == null) body = GetComponent<Rigidbody>();
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.useGravity = false;
            }

            ApplyVisualState();
            RefreshInteraction();
            RefreshLabel();
            StateChanged?.Invoke();
        }

        private void OnMissionStarted()
        {
            ResetElectrode();
        }

        private void OnDeviceStateChanged()
        {
            RefreshAvailability();
        }

        private void OnGrabEntered(Hand hand)
        {
            if (interactionsDisabled || !CanGrabNow()) return;

            grabbed = true;
            if (!retrieved) retrieved = true;
            if (attachedZone >= 0)
            {
                if (attachments != null) attachments.Detach(this);
                attachedZone = -1;
                // Torna sotto il DAE mantenendo la posa mondo: così il grab non genera salti.
                if (homeSlot != null) transform.SetParent(homeSlot, true);
            }
            ApplyVisualState();
            RefreshLabel();
            StateChanged?.Invoke();
        }

        private void OnGrabExited(Hand hand)
        {
            grabbed = false;

            float distanceA = attachments != null && attachments.Zone(0) != null
                ? Vector3.Distance(transform.position, attachments.Zone(0).position) : float.NaN;
            float distanceB = attachments != null && attachments.Zone(1) != null
                ? Vector3.Distance(transform.position, attachments.Zone(1).position) : float.NaN;
            float tolerance = attachments != null ? attachments.SnapDistance : -1f;
            string reason = "aggancio riuscito";
            if (attachments == null || device == null) reason = "riferimenti mancanti";
            else if (!device.IsPoweredOn) reason = "DAE non acceso";
            else if (!attachments.IsSupine) reason = "NPC non supino (" + attachments.CurrentPose + ")";
            else if (attachments.IsOccupied(0) && attachments.IsOccupied(1)) reason = "entrambe le zone occupate";
            else if (attachedZone >= 0) reason = "elettrodo già applicato";
            else if (distanceA > tolerance && distanceB > tolerance) reason = "fuori tolleranza";

            int attached = -1;
            // Non tenta un secondo aggancio mentre è ancora applicato.
            if (attachedZone < 0 && attachments != null && device != null && device.IsPoweredOn)
            {
                attached = attachments.TryAttach(this);
                if (attached >= 0)
                {
                    attachedZone = attached;
                    // Figlio della zona: segue scheletro e pose dell'NPC senza modificare il sistema di pose.
                    Transform zoneTransform = attachments.Zone(attached);
                    if (zoneTransform != null)
                    {
                        transform.SetParent(zoneTransform, true);
                        transform.localPosition = Vector3.zero;
                        transform.localRotation = Quaternion.identity;
                    }
                    // La fisica viene consolidata in LateUpdate: qui verrebbe sovrascritta da ReleaseGrab.
                    ApplyVisualState();
                    StateChanged?.Invoke();
                }
            }

            if (debugRelease)
            {
                Debug.Log($"[SI81 ManDown][AED] Rilascio {name} ({hand}): " +
                          $"dA={distanceA:F3} m, dB={distanceB:F3} m, tolleranza={tolerance:F3} m, " +
                          $"posa={(attachments != null ? attachments.CurrentPose.ToString() : "n/d")}, " +
                          $"supino={(attachments != null && attachments.IsSupine)}, " +
                          $"zone libere A={!attachments?.IsOccupied(0)} B={!attachments?.IsOccupied(1)}, " +
                          $"risultato={(attached >= 0 ? "agganciato zona " + (attached == 0 ? "A" : "B") : "non agganciato")}, " +
                          $"motivo={reason}.", this);
            }
            RefreshLabel();
        }

        private bool CanGrabNow()
        {
            if (device == null || attachments == null) return false;
            if (!device.IsAvailable || !device.IsRetrieved || !device.IsPoweredOn) return false;
            return true;
        }

        private void RefreshInteraction()
        {
            if (interaction == null) return;
            interaction.Grabbable = !interactionsDisabled && CanGrabNow();
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            bool available = device != null && device.IsAvailable;
            label.Suppressed = !available || interactionsDisabled || grabbed || !CanGrabNow();
        }

        /// <summary>
        /// La pad diventa verde quando l'elettrodo è agganciato; quando è libero si rimuove
        /// l'override in modo che torni il colore del materiale condiviso, senza modificarlo.
        /// </summary>
        private void ApplyVisualState()
        {
            if (padRenderer == null) return;
            if (attachedZone < 0)
            {
                padRenderer.SetPropertyBlock(null);
                return;
            }
            if (padBlock == null) padBlock = new MaterialPropertyBlock();
            padRenderer.GetPropertyBlock(padBlock);
            padBlock.SetColor("_BaseColor", snappedColor);
            padRenderer.SetPropertyBlock(padBlock);
        }

        private void IgnorePlayerBodyCollisions()
        {
            IgnorePlayerBodyCollisions(RigRegistration.Instance);
        }

        private void OnRigRegistered(RigRegistration rig)
        {
            if (rig != null) IgnorePlayerBodyCollisions(rig);
        }

        private void IgnorePlayerBodyCollisions(RigRegistration rig)
        {
            if (rig == null) return;
            Collider[] playerColliders = rig.GetComponentsInChildren<Collider>(true);
            if (playerColliders.Length == 0) return;
            // Stessa soluzione dei telefoni, estesa ai figli per eventuali collider visivi.
            Collider[] mine = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < mine.Length; i++)
            {
                if (mine[i] == null) continue;
                for (int j = 0; j < playerColliders.Length; j++)
                {
                    if (playerColliders[j] != null)
                        Physics.IgnoreCollision(mine[i], playerColliders[j], true);
                }
            }
        }

        /// <summary>Notifica al dispositivo il cambio di stato (aggancio, rimozione, recupero).</summary>
        public void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
