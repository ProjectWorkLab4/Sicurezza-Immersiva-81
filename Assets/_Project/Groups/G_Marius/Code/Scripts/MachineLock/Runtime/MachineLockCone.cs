using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Delimitation cone of the Machine Lock scenario (GDD §8.2, §8.3 and §9.8). The cone is grabbed with
    /// the grip, follows the hand and, when released, snaps to the nearest free
    /// <see cref="MachineLockSnapZone"/> of kind <see cref="MachineLockSnapZone.SnapKind.Cone"/> within the
    /// configured radius. Collisions against the shared Player Rig's <see cref="CharacterController"/> are
    /// ignored via <see cref="Physics.IgnoreCollision(Collider, Collider, bool)"/> while keeping the collider
    /// non-trigger so <c>RayInteractor</c> raycasts always hit it and carrying a cone underneath the player
    /// never causes climbing or no-clipping.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Cone")]
    public sealed class MachineLockCone : MonoBehaviour
    {
        private const float DEFAULT_SNAP_RADIUS = 0.6f;
        private const int FREEZE_FRAMES = 2;
        private const float HOVER_THROTTLE_SECONDS = 0.35f;

        [Header("Wiring")]
        [Tooltip("Spill system that owns the zones, the ghost hint and the containment count.")]
        [SerializeField] private MachineLockSpillSystem spillSystem;
        [Tooltip("Tunable values (snap radius). Optional: the GDD default of 0.6 m is used when empty.")]
        [SerializeField] private MachineLockConfig config;
        [Tooltip("Interactable of the cone. Empty: found on this object or on a child.")]
        [SerializeField] private Interactable interactable;
        [Tooltip("World label of the cone. Hidden while the cone is held or placed.")]
        [SerializeField] private LabelAnchor labelAnchor;

        [Header("Audio")]
        [Tooltip("Source used for the snap click.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Soft click played when the cone snaps.")]
        [SerializeField] private AudioClip snapClip;

        private Interactable wiredInteractable;
        private Rigidbody body;
        private int freezeFrames;
        private float lastHoverTime = float.NegativeInfinity;

        /// <summary>true after the cone snapped to a zone. A placed cone cannot be grabbed again.</summary>
        public bool Snapped { get; private set; }
        /// <summary>Most recent hover Action Feedback string emitted for this cone.</summary>
        public string LastHoverFeedback { get; private set; } = string.Empty;

        /// <summary>
        /// Ensures physical collisions with the active player <see cref="CharacterController"/> are ignored
        /// while preserving non-trigger raycast hits.
        /// </summary>
        public bool RefreshPlayerCollisionIgnore()
        {
            return MachineLockCollisionSafety.IgnorePlayerCollisions(gameObject);
        }

        /// <summary>
        /// Tries to place the cone on the nearest free cone zone. Idempotent: a placed cone stays placed.
        /// </summary>
        /// <returns>true when the cone is on a zone, false when it stays where it was released.</returns>
        public bool TrySnap()
        {
            if (Snapped) return true;

            MachineLockSnapZone zone = FindZone();
            if (zone == null || !zone.Occupy()) return false;

            transform.SetPositionAndRotation(zone.SnapPoint.position, zone.SnapPoint.rotation);
            FreezeBody();

            if (interactable != null) interactable.Grabbable = false;
            Snapped = true;
            if (labelAnchor != null) labelAnchor.Suppressed = true;
            PlayOneShot(snapClip);

            if (spillSystem != null) spillSystem.RegisterCone(zone);
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
                spillSystem.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, false);
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
                return spillSystem.FindNearestZone(transform.position, MachineLockSnapZone.SnapKind.Cone, radius);

            return MachineLockSnapZone.FindNearestUnoccupied(transform.position, MachineLockSnapZone.SnapKind.Cone, radius);
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
            wiredInteractable.HoverEntered += OnHoverEntered;
            wiredInteractable.GrabEntered += OnGrabEntered;
            wiredInteractable.GrabExited += OnGrabExited;
        }

        private void Unwire()
        {
            if (wiredInteractable == null) return;
            wiredInteractable.HoverEntered -= OnHoverEntered;
            wiredInteractable.GrabEntered -= OnGrabEntered;
            wiredInteractable.GrabExited -= OnGrabExited;
            wiredInteractable = null;
        }

        private void OnHoverEntered(Hand hand)
        {
            if (Snapped) return;
            float now = Time.unscaledTime;
            if (!float.IsNegativeInfinity(lastHoverTime) && now - lastHoverTime < HOVER_THROTTLE_SECONDS) return;
            lastHoverTime = now;
            LastHoverFeedback = MachineLockTexts.HoverCone;
            if (HUDService.IsReady)
                HUDService.ShowActionFeedback(LastHoverFeedback, FeedbackKind.Neutral);
        }

        private void OnGrabEntered(Hand hand)
        {
            RefreshPlayerCollisionIgnore();
            if (labelAnchor != null) labelAnchor.Suppressed = true;
            if (spillSystem != null) spillSystem.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, true);
        }

        private void OnGrabExited(Hand hand)
        {
            RefreshPlayerCollisionIgnore();
            if (labelAnchor != null) labelAnchor.Suppressed = false;
            if (spillSystem != null) spillSystem.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, false);
            TrySnap();
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (oneShotAudio != null && clip != null) oneShotAudio.PlayOneShot(clip);
        }
    }
}
