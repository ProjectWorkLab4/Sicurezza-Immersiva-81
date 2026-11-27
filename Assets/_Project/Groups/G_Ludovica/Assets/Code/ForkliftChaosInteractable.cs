using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>Azione specifica dello scenario 07 richiamata da un oggetto interattivo.</summary>
    public enum ForkliftChaosAction
    {
        // Osservazione: basta guardare l'oggetto (nessun pulsante, nessun pannello).
        ObserveMarco,
        ObserveForklift1,
        ObservePallet,
        ObserveBlindCorner,

        // Azioni fisiche: sguardo + grilletto/impugnatura sull'oggetto, oppure tasto funzione in Editor.
        StopForklift1,
        PickupRadio,
        Call112,
        CallForeman,
        PickupBarrier,
        PlaceBarrier,
        PlaceCones,
        KnockPallet,
        TalkToMarco,
        CheckBreathing,
        PressWound,
        CoverMarco,
        StartCPR,
        UseAED,
        MoveMarco,
        StopForklift2,

        // Azioni raggruppate: meno oggetti puntabili accanto a Marco, stessi effetti della sequenza clinica.
        AssessMarco,
        TreatWound,
        Resuscitate
    }

    /// <summary>
    /// Adattatore tra lo scenario Forklift Chaos e il layer di interazione condiviso (<see cref="Interactable"/> +
    /// <see cref="RayInteractor"/>). Non legge più direttamente i controller: si limita a tradurre select/grab/hover
    /// negli eventi dello scenario. In Editor restano disponibili i tasti funzione e lo sguardo prolungato, perché
    /// senza visore non esiste una posa valida dei controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    [AddComponentMenu("SI81/Scenari/Forklift Chaos/Interactable")]
    public sealed class ForkliftChaosInteractable : MonoBehaviour
    {
        [Header("Azione")]
        [SerializeField] private ForkliftChaosAction action;
        [SerializeField] private string label = "Interagisci";
        [SerializeField] private Key editorKey = Key.None;
        [SerializeField] private bool once = true;
        [SerializeField] private bool requiresRadio;
        [SerializeField] private bool lookOnly;
        [SerializeField] private bool useGrab;
        [SerializeField] private float dwellSeconds = 1.1f;

        // Un solo evento per frame: due adapter sullo stesso oggetto (osserva + spegni) non devono partire insieme.
        private static int lastUseFrame = -1;

        private Interactable interactable;
        private ForkliftChaosScenario scenario;
        private bool used;
        private bool hovered;
        private float lookTime;

        /// <summary>Azione richiesta dall'oggetto.</summary>
        public ForkliftChaosAction Action => action;
        /// <summary>Etichetta mostrata all'utente.</summary>
        public string Label => label;
        /// <summary>true se l'oggetto va afferrato con l'impugnatura invece che selezionato col grilletto.</summary>
        public bool UseGrab => useGrab;
        /// <summary>true se l'oggetto è attualmente tenuto in mano dal giocatore.</summary>
        public bool IsHeld => interactable != null && interactable.IsGrabbed;

        /// <summary>Configura un'azione da grilletto (select) o da tasto Editor.</summary>
        public void Configure(ForkliftChaosAction newAction, string newLabel, Key newKey,
            bool needsRadio = false, bool useGrab = false, bool keepAfterUse = false)
        {
            action = newAction;
            label = string.IsNullOrEmpty(newLabel) ? newAction.ToString() : newLabel;
            editorKey = newKey;
            requiresRadio = needsRadio;
            this.useGrab = useGrab;
            lookOnly = false;
            once = !keepAfterUse;
            ApplyInteractableFlags();
        }

        /// <summary>Configura un'osservazione: basta puntare l'oggetto per il tempo richiesto.</summary>
        public void ConfigureLook(ForkliftChaosAction newAction, string newLabel, float newDwell = 1.1f)
        {
            action = newAction;
            label = string.IsNullOrEmpty(newLabel) ? newAction.ToString() : newLabel;
            editorKey = Key.None;
            dwellSeconds = Mathf.Max(0.2f, newDwell);
            lookOnly = true;
            requiresRadio = false;
            useGrab = false;
            once = true;
            ApplyInteractableFlags();
        }

        /// <summary>Riporta l'oggetto allo stato iniziale (replay dello scenario).</summary>
        public void ResetUsed()
        {
            used = false;
            hovered = false;
            lookTime = 0f;
            ApplyInteractableFlags();
        }

        private void Awake()
        {
            interactable = GetComponent<Interactable>();
            ApplyInteractableFlags();
        }

        private void OnEnable()
        {
            if (interactable == null) interactable = GetComponent<Interactable>();
            if (interactable == null) return;
            interactable.SelectEntered += HandleSelectEntered;
            interactable.GrabEntered += HandleGrabEntered;
            interactable.GrabExited += HandleGrabExited;
            interactable.HoverEntered += HandleHoverEntered;
            interactable.HoverExited += HandleHoverExited;
        }

        private void OnDisable()
        {
            if (interactable == null) return;
            interactable.SelectEntered -= HandleSelectEntered;
            interactable.GrabEntered -= HandleGrabEntered;
            interactable.GrabExited -= HandleGrabExited;
            interactable.HoverEntered -= HandleHoverEntered;
            interactable.HoverExited -= HandleHoverExited;
            hovered = false;
            lookTime = 0f;
        }

        private void Update()
        {
            if (once && used) return;
            if (scenario == null) scenario = FindFirstObjectByType<ForkliftChaosScenario>();
            if (scenario == null || !scenario.IsRunning) return;

            if (lookOnly)
            {
                // VR: hover del raggio condiviso. Editor: sguardo della testa, perché i controller non sono tracciati.
                if (hovered || IsAimedAtByHead())
                {
                    lookTime += Time.deltaTime;
                    if (lookTime >= dwellSeconds) Fire();
                }
                else
                {
                    lookTime = 0f;
                }
                return;
            }

            if (editorKey != Key.None && Keyboard.current != null && Keyboard.current[editorKey].wasPressedThisFrame)
                Fire();
        }

        private void HandleSelectEntered(Hand hand)
        {
            if (lookOnly || useGrab) return;
            Fire();
        }

        private void HandleGrabEntered(Hand hand)
        {
            if (lookOnly || !useGrab) return;
            Fire();
        }

        /// <summary>
        /// Quando il giocatore rilascia l'impugnatura, radio e barriera restano dove sono state lasciate:
        /// la barriera vicino alla linea BL-01 viene posata automaticamente, la radio resta puntabile coi pulsanti.
        /// </summary>
        private void HandleGrabExited(Hand hand)
        {
            if (lookOnly || !useGrab) return;
            if (scenario == null) scenario = FindFirstObjectByType<ForkliftChaosScenario>();
            if (scenario == null) return;
            if (action == ForkliftChaosAction.PickupRadio || action == ForkliftChaosAction.PickupBarrier)
                scenario.HandleCarriedReleased(action);
        }

        private void HandleHoverEntered(Hand hand)
        {
            if (lookOnly) hovered = true;
        }

        private void HandleHoverExited(Hand hand)
        {
            if (lookOnly) hovered = false;
        }

        private void Fire()
        {
            if (once && used) return;
            if (scenario == null) scenario = FindFirstObjectByType<ForkliftChaosScenario>();
            if (scenario == null || !scenario.IsRunning) return;

            if (requiresRadio && !scenario.HasRadio)
            {
                scenario.Feedback("Prendi prima la radio dal rack RK-01.", FeedbackKind.Negative);
                return;
            }

            // Un solo evento per frame, anche con più adapter sullo stesso collider.
            if (lastUseFrame == Time.frameCount) return;
            lastUseFrame = Time.frameCount;

            used = true;
            scenario.HandleInteraction(action, this);
            if (once)
            {
                ApplyInteractableFlags();
                // Azione fatta: l'etichetta della UI comune ("TRIGGER Interagisci") non deve restare sopra il blocco.
                var prompt = GetComponent<LabelAnchor>();
                if (prompt != null && prompt.Kind == LabelKind.Interaction) prompt.enabled = false;
            }
        }

        /// <summary>Allinea select/grab dell'<see cref="Interactable"/> al tipo di azione.</summary>
        private void ApplyInteractableFlags()
        {
            if (interactable == null) interactable = GetComponent<Interactable>();
            if (interactable == null) return;

            bool active = !(once && used);
            if (lookOnly)
            {
                // Non toccare i flag condivisi: su uno stesso oggetto convivono spesso un adapter "guarda"
                // e un adapter "usa" (es. Muletto 1). Disattivarli qui renderebbe l'oggetto non cliccabile.
                // L'adapter lookOnly ignora comunque select e grab nei relativi gestori.
                return;
            }
            interactable.Selectable = active && !useGrab;
            interactable.Grabbable = active && useGrab;
            interactable.FollowsHand = useGrab;
        }

        /// <summary>Solo Editor: controlla che la testa stia puntando questo oggetto (senza controller tracciati).</summary>
        private bool IsAimedAtByHead()
        {
#if UNITY_EDITOR
            Transform head = Camera.main != null ? Camera.main.transform : null;
            if (head == null) return false;

            Ray ray = new Ray(head.position, head.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Collide)) return false;
            return hit.collider != null && hit.collider.GetComponentInParent<ForkliftChaosInteractable>() == this;
#else
            return false;
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.72f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, 0.18f);
        }
    }
}
