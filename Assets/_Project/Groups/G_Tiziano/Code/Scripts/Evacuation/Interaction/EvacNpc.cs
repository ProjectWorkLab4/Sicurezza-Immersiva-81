using System;
using UnityEngine;
using UnityEngine.Events;
using SI81.Tiziano.ChemicalAlert.Interaction;
using SI81.Tiziano.Evacuation.Systems;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Interaction
{
    /// <summary>Lifecycle state of a colleague NPC.</summary>
    public enum EvacNpcState
    {
        /// <summary>Still sitting at the desk, available for collection.</summary>
        Idle,
        /// <summary>Following the player along the breadcrumb trail.</summary>
        Following,
        /// <summary>Reached the emergency exit with the player.</summary>
        Evacuated
    }

    /// <summary>
    /// Colleague NPC: a ~1 m trigger area for hand interaction and a graybox Visual child.
    /// Once collected it follows a breadcrumb trail; no NavMesh and no animation.
    /// </summary>
    [RequireComponent(typeof(ScenarioInteractable))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class EvacNpc : MonoBehaviour
    {
        /// <summary>Stable dialogue key raised when the colleague joins the line.</summary>
        public const string DialogueFollow = "evac.npc.dialogue.follow";
        /// <summary>Stable dialogue key raised when the colleague is already in the line.</summary>
        public const string DialogueAlready = "evac.npc.dialogue.already";

        [SerializeField] private NpcRegistry registry;
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private SphereCollider interactionArea;
        [SerializeField] private Transform visual;
        [SerializeField] private int room = 1;
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.6f;

        [Header("Hooks")]
        [SerializeField] private UnityEvent onFollowStarted = new UnityEvent();
        [SerializeField] private UnityEvent onStopped = new UnityEvent();
        [SerializeField] private UnityEvent onEvacuated = new UnityEvent();

        private EvacNpcState state = EvacNpcState.Idle;
        private int order = -1;
        private float fixedY;
        private bool haltedReported;

        /// <summary>Raised when this NPC starts following.</summary>
        public event Action<EvacNpc> FollowStarted;
        /// <summary>Raised when this NPC reaches its slot and halts.</summary>
        public event Action<EvacNpc> Stopped;
        /// <summary>Raised when this NPC reaches the emergency exit.</summary>
        public event Action<EvacNpc> Evacuated;

        /// <summary>Gets the current state.</summary>
        public EvacNpcState State => state;
        /// <summary>Gets the zero-based position in the line (-1 when idle).</summary>
        public int Order => order;
        /// <summary>Gets the office this NPC belongs to (1 or 2).</summary>
        public int Room => room;
        /// <summary>Gets the hand interaction surface.</summary>
        public ScenarioInteractable Interactable => interactable;
        /// <summary>Gets the interaction trigger area.</summary>
        public SphereCollider InteractionArea => interactionArea;
        /// <summary>Gets the visual child (graybox capsule).</summary>
        public Transform Visual => visual;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();
            if (interactionArea == null)
                interactionArea = GetComponent<SphereCollider>();
            if (visual == null)
            {
                Transform child = transform.Find("Visual");
                if (child != null)
                    visual = child;
            }
            if (registry == null)
                registry = FindFirstObjectByType<NpcRegistry>();
        }

        private void OnEnable()
        {
            if (interactable != null)
                interactable.Activated += HandleActivated;
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.Activated -= HandleActivated;
        }

        /// <summary>Initialises the NPC after a runtime spawn. The position sets its fixed Y.</summary>
        public void Configure(NpcRegistry owner, int roomNumber, string id)
        {
            registry = owner;
            room = roomNumber;
            name = id;
            state = EvacNpcState.Idle;
            order = -1;
            fixedY = transform.position.y;
            haltedReported = false;
            if (interactable != null)
                interactable.SetInteractionEnabled(true);
        }

        /// <summary>Starts following at the given position in the line. Returns false unless Idle.</summary>
        public bool BeginFollowing(int lineOrder)
        {
            if (state != EvacNpcState.Idle)
                return false;

            state = EvacNpcState.Following;
            order = lineOrder;
            haltedReported = false;
            UDebug.Log($"[EV] NPC following: {name} room {room} order {order}.");
            onFollowStarted.Invoke();
            FollowStarted?.Invoke(this);
            return true;
        }

        /// <summary>Marks this NPC as evacuated at the emergency exit.</summary>
        public void MarkEvacuated()
        {
            if (state == EvacNpcState.Evacuated)
                return;

            state = EvacNpcState.Evacuated;
            if (interactable != null)
                interactable.SetInteractionEnabled(false);
            UDebug.Log($"[EV] NPC evacuated: {name} room {room}.");
            onStopped.Invoke();
            Stopped?.Invoke(this);
            onEvacuated.Invoke();
            Evacuated?.Invoke(this);
        }

        /// <summary>Moves towards a trail target at the configured speed, keeping a fixed height.</summary>
        public void Follow(Vector3 target, float deltaTime)
        {
            if (state != EvacNpcState.Following || deltaTime <= 0f)
                return;

            Vector3 current = new Vector3(transform.position.x, fixedY, transform.position.z);
            Vector3 desired = new Vector3(target.x, fixedY, target.z);
            Vector3 delta = desired - current;
            float distance = delta.magnitude;
            if (distance < 0.02f)
            {
                if (!haltedReported)
                {
                    haltedReported = true;
                    onStopped.Invoke();
                    Stopped?.Invoke(this);
                }
                return;
            }

            haltedReported = false;
            float step = Mathf.Min(moveSpeed * deltaTime, distance);
            transform.position = current + delta * (step / distance);
        }

        private void HandleActivated(ScenarioInteractable source)
        {
            if (registry != null)
                registry.Collect(this, "trigger");
        }
    }
}
