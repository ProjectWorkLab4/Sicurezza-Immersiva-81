using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Absorbent mat or sock of the Machine Lock scenario (GDD §8.2, §8.3 and §9.8). Both kinds are
    /// grabbed with the grip, follow the hand and, when released, snap to the nearest free
    /// <see cref="MachineLockSnapZone"/> of kind <see cref="MachineLockSnapZone.SnapKind.Absorbent"/>
    /// within the configured radius. Collisions against the shared Player Rig's <see cref="CharacterController"/>
    /// are ignored via <see cref="Physics.IgnoreCollision(Collider, Collider, bool)"/> while keeping the
    /// collider non-trigger for <c>RayInteractor</c> targeting.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Absorbent")]
    public sealed class MachineLockAbsorbent : MonoBehaviour
    {
        /// <summary>Kind of absorbent item.</summary>
        public enum AbsorbentKind
        {
            /// <summary>Absorbent mat: feedback MachineLockTexts.Mat.</summary>
            Mat,
            /// <summary>Absorbent sock: feedback MachineLockTexts.Sock.</summary>
            Sock
        }

        private const float DEFAULT_SNAP_RADIUS = 0.6f;
        private const int FREEZE_FRAMES = 2;

        [Header("Item")]
        [Tooltip("Kind of absorbent: mat or sock.")]
        [SerializeField] private AbsorbentKind absorbentKind = AbsorbentKind.Mat;

        [Header("Wiring")]
        [Tooltip("Spill system that owns the zones, the ghost hint and the containment count.")]
        [SerializeField] private MachineLockSpillSystem spillSystem;
        [Tooltip("Tunable values (snap radius). Optional: the GDD default of 0.6 m is used when empty.")]
        [SerializeField] private MachineLockConfig config;
        [Tooltip("Interactable of the item. Empty: found on this object or on a child.")]
        [SerializeField] private Interactable interactable;
        [Tooltip("World label of the item. Hidden while the item is held or placed.")]
        [SerializeField] private LabelAnchor labelAnchor;

        [Header("Audio")]
        [Tooltip("Source used for the snap click.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Soft click played when the item snaps.")]
        [SerializeField] private AudioClip snapClip;

        private Interactable wiredInteractable;
        private Rigidbody body;
        private int freezeFrames;

        /// <summary>Mat or sock.</summary>
        public AbsorbentKind Kind => absorbentKind;

        /// <summary>true after the item snapped to a zone. A placed item cannot be grabbed again.</summary>
        public bool Snapped { get; private set; }

        /// <summary>
        /// Ensures physical collisions with the active player <see cref="CharacterController"/> are ignored
        /// while preserving non-trigger raycast hits.
        /// </summary>
        public bool RefreshPlayerCollisionIgnore()
        {
            return MachineLockCollisionSafety.IgnorePlayerCollisions(gameObject);
        }

        /// <summary>
        /// Tries to place the mat or sock on the nearest free absorbent zone. Idempotent: a placed item stays
        /// placed.
        /// </summary>
        /// <returns>true when the item is on a zone, false when it stays where it was released.</returns>
        public bool TrySnap()
        {
            return TrySnap(Hand.Any, emitNegativeOnMiss: true);
        }

        private bool TrySnap(Hand hand, bool emitNegativeOnMiss)
        {
            if (Snapped) return true;

            MachineLockSnapZone zone = FindZone();
            if (zone == null || !zone.Occupy())
            {
                if (emitNegativeOnMiss)
                    MachineLockActionFeedback.PlayNegative(gameObject, hand, MachineLockTexts.AbsorbentWrongSpot);
                return false;
            }

            transform.SetPositionAndRotation(zone.SnapPoint.position, zone.SnapPoint.rotation);
            FreezeBody();

            if (interactable != null) interactable.Grabbable = false;
            Snapped = true;
            if (labelAnchor != null) labelAnchor.Suppressed = true;
            PlayOneShot(snapClip);
            MachineLockActionFeedback.PlayPositive(gameObject, hand);

            if (spillSystem != null) spillSystem.RegisterAbsorbent(zone, absorbentKind);
            return true;
        }

        private void Awake()
        {
            ResolveReferences();
            body = GetComponentInParent<Rigidbody>();
            RefreshPlayerCollisionIgnore();
        }

        private void Start()
        {
            RefreshPlayerCollisionIgnore();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplyItemState();
            Wire();
            RigRegistration.Registered += OnRigRegistered;
            RefreshPlayerCollisionIgnore();
        }

        private void OnDisable()
        {
            RigRegistration.Registered -= OnRigRegistered;
            Unwire();
            if (!Snapped && spillSystem != null)
                spillSystem.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Absorbent, false);
        }

        private void LateUpdate()
        {
            if (freezeFrames <= 0) return;
            freezeFrames--;
            ApplyFrozenBody();
        }

        private void OnRigRegistered(RigRegistration rig)
        {
            RefreshPlayerCollisionIgnore();
        }

        private void ResolveReferences()
        {
            if (interactable == null) interactable = GetComponent<Interactable>();
            if (interactable == null) interactable = GetComponentInChildren<Interactable>(true);
            if (labelAnchor == null) labelAnchor = GetComponent<LabelAnchor>();
            if (labelAnchor == null) labelAnchor = GetComponentInChildren<LabelAnchor>(true);
            if (spillSystem == null) spillSystem = FindFirstObjectByType<MachineLockSpillSystem>();
        }

        private void ApplyItemState()
        {
            if (interactable == null) return;
            if (Snapped)
            {
                interactable.Grabbable = false;
                interactable.Selectable = false;
                return;
            }

            interactable.Grabbable = true;
            interactable.FollowsHand = true;
            interactable.Selectable = false;
        }

        private MachineLockSnapZone FindZone()
        {
            float radius = config != null ? config.snapRadius : DEFAULT_SNAP_RADIUS;
            if (spillSystem != null)
                return spillSystem.FindNearestZone(transform.position, MachineLockSnapZone.SnapKind.Absorbent, radius);

            return MachineLockSnapZone.FindNearestUnoccupied(transform.position, MachineLockSnapZone.SnapKind.Absorbent, radius);
        }

        private void FreezeBody()
        {
            if (body == null) body = GetComponentInParent<Rigidbody>();
            ApplyFrozenBody();
            freezeFrames = FREEZE_FRAMES;
        }

        private void ApplyFrozenBody()
        {
            if (body == null) return;
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void Wire()
        {
            if (interactable == null || wiredInteractable == interactable) return;
            Unwire();
            wiredInteractable = interactable;
            wiredInteractable.GrabEntered += OnGrabEntered;
            wiredInteractable.GrabExited += OnGrabExited;
        }

        private void Unwire()
        {
            if (wiredInteractable == null) return;
            wiredInteractable.GrabEntered -= OnGrabEntered;
            wiredInteractable.GrabExited -= OnGrabExited;
            wiredInteractable = null;
        }

        private void OnGrabEntered(Hand hand)
        {
            RefreshPlayerCollisionIgnore();
            if (labelAnchor != null) labelAnchor.Suppressed = true;
            if (spillSystem != null) spillSystem.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Absorbent, true);
        }

        private void OnGrabExited(Hand hand)
        {
            RefreshPlayerCollisionIgnore();
            if (labelAnchor != null) labelAnchor.Suppressed = false;
            if (spillSystem != null) spillSystem.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Absorbent, false);
            TrySnap(hand, emitNegativeOnMiss: true);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (oneShotAudio != null && clip != null) oneShotAudio.PlayOneShot(clip);
        }
    }
}
