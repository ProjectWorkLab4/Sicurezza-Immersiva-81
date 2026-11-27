using System.Collections;
using SI81.Input;
using SI81.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

namespace SI81.MachineLock
{
    /// <summary>
    /// Interactive hinged steel exit door for the Machine Lock scenario (ADR-0006, Issue #41, Issue #43).
    /// Accepts both Select (Trigger) and Grab (Grip) on the entire steel door leaf and protruding panic bar
    /// without pulling the door off its hinges. Activating the door opens (or recenters, if already open)
    /// the shared <c>HUD_DecisionPanel</c> safely inside the bay via
    /// <see cref="MachineLockScenario.RequestExitConfirmation"/>, plays a subtle mechanical click, and
    /// auto-closes the confirmation if the player walks away (&gt; 3.5 m) so confirmation state can never
    /// deadlock. Confirming exit plays the latch sound, disables the leaf collider, swings the door open
    /// 90 degrees outward (-X) into the west vestibule, and calls <see cref="MachineLockScenario.NotifyExitReached"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Exit Door")]
    public sealed class MachineLockExitDoor : MonoBehaviour
    {
        /// <summary>Horizontal distance in metres beyond which an unanswered exit confirmation auto-closes.</summary>
        public const float AUTO_CLOSE_DISTANCE_METERS = 3.5f;

        private const float DEFAULT_OPEN_ANGLE_DEGREES = 90f;
        private const float DEFAULT_OPEN_DURATION_SECONDS = 0.8f;

        [Header("Scenario")]
        [Tooltip("Scenario that displays the confirmation decision panel and resolves exit handover.")]
        [SerializeField] private MachineLockScenario scenario;

        [Header("Door and panic bar")]
        [Tooltip("Exit door Interactable (on MESH_SteelExitDoorLeaf covering both leaf and panic bar) supporting Select (Trigger) and Grab (Grip) with FollowsHand = false.")]
        [FormerlySerializedAs("panicBarInteractable")]
        [SerializeField] private Interactable doorInteractable;
        [Tooltip("Hinged steel door leaf transform rotated around its hinge pivot when opening.")]
        [SerializeField] private Transform doorLeafTransform;
        [Tooltip("Solid door leaf collider disabled once the player confirms exit.")]
        [SerializeField] private Collider doorLeafCollider;

        [Header("Motion")]
        [Tooltip("Local axis around which the door leaf swings open.")]
        [SerializeField] private Vector3 openAxis = Vector3.up;
        [Tooltip("Swing angle in degrees when opening (+90 swings outward along -X into the west vestibule).")]
        [SerializeField] private float openAngleDegrees = DEFAULT_OPEN_ANGLE_DEGREES;
        [Tooltip("Seconds taken to swing the door open; 0 snaps open immediately.")]
        [SerializeField] private float openDurationSeconds = DEFAULT_OPEN_DURATION_SECONDS;

        [Header("Audio")]
        [Tooltip("One-shot audio source for door press click and latch release.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Subtle mechanical click clip played when pressing the door/panic bar opens the exit prompt.")]
        [SerializeField] private AudioClip pressClickClip;
        [Tooltip("Mechanical latch release clip played when the door unlatches and swings open.")]
        [SerializeField] private AudioClip latchClip;

        private Quaternion closedLocalRotation = Quaternion.identity;
        private bool closedRotationCaptured;
        private Coroutine openRoutine;
        private Interactable wiredInteractable;

        /// <summary>true while the exit confirmation DecisionPanel is currently awaiting a player choice.</summary>
        public bool IsConfirmationOpen { get; private set; }
        /// <summary>true while the steel door leaf is swinging open.</summary>
        public bool IsOpening { get; private set; }
        /// <summary>true once the door has fully swung open and handed control to the scenario exit flow.</summary>
        public bool IsOpened { get; private set; }

        private void Awake()
        {
            CaptureClosedRotation();
            if (oneShotAudio != null) oneShotAudio.playOnAwake = false;
            EnsureDoorInteractable();
        }

        private void OnEnable()
        {
            EnsureDoorInteractable();
            WireDoorInteractable();
        }

        private void OnDisable()
        {
            UnwireDoorInteractable();
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }
            if (IsConfirmationOpen && scenario != null)
                scenario.CloseActiveExitConfirmation();
            IsOpening = false;
            IsConfirmationOpen = false;
        }

        private void Update()
        {
            if (!IsConfirmationOpen) return;

            if (scenario == null || scenario.HasEnded || !scenario.IsExitConfirmationPanelOpen)
            {
                IsConfirmationOpen = false;
                return;
            }

            Transform head = scenario.ResolvePlayerHead();
            if (head == null) return;

            Vector3 delta = head.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > AUTO_CLOSE_DISTANCE_METERS * AUTO_CLOSE_DISTANCE_METERS)
                CancelExitConfirmation();
        }

        /// <summary>
        /// Opens (or safely recenters if already open) the shared exit confirmation panel through
        /// <see cref="MachineLockScenario.RequestExitConfirmation"/> and plays a subtle click cue.
        /// Ignored only when the door is already opening/opened or the scenario has ended.
        /// </summary>
        /// <returns>true when the decision panel opened or recentered cleanly.</returns>
        public bool RequestExitConfirmation()
        {
            if (IsOpened || IsOpening) return false;
            if (scenario == null || scenario.HasEnded) return false;

            IsConfirmationOpen = true;
            bool opened = scenario.RequestExitConfirmation(HandleExitDecision);
            if (!opened)
            {
                IsConfirmationOpen = false;
                return false;
            }

            PlayClip(pressClickClip);
            return true;
        }

        /// <summary>
        /// Cancels an active exit confirmation prompt and closes the shared DecisionPanel without
        /// ending the mission. Called automatically when the player walks away from the exit door.
        /// </summary>
        public void CancelExitConfirmation()
        {
            if (!IsConfirmationOpen) return;
            IsConfirmationOpen = false;
            if (scenario != null) scenario.CloseActiveExitConfirmation();
        }

        /// <summary>
        /// Callback passed to <c>HUD_DecisionPanel</c>. Option 0 confirms exit and opens the door;
        /// any other option (1 = stay, or timeout) cancels without ending the mission.
        /// </summary>
        /// <param name="optionIndex">0 = confirm exit, 1 = stay in room, -1 = timeout.</param>
        public void HandleExitDecision(int optionIndex)
        {
            IsConfirmationOpen = false;
            if (optionIndex == 0)
                OpenAndCompleteExit();
        }

        /// <summary>
        /// Unlatches the panic bar, disables the door leaf collider, swings the steel leaf open by
        /// <see cref="openAngleDegrees"/>, and notifies <see cref="MachineLockScenario.NotifyExitReached"/>.
        /// </summary>
        public void OpenAndCompleteExit()
        {
            if (IsOpened || IsOpening) return;
            CaptureClosedRotation();

            IsOpening = true;
            IsConfirmationOpen = false;

            if (doorInteractable != null)
            {
                doorInteractable.Selectable = false;
                doorInteractable.Grabbable = false;
            }
            if (doorLeafCollider != null)
                doorLeafCollider.enabled = false;

            PlayClip(latchClip);

            if (Application.isPlaying && openDurationSeconds > 0.001f && doorLeafTransform != null)
            {
                if (openRoutine != null) StopCoroutine(openRoutine);
                openRoutine = StartCoroutine(OpenDoorRoutine());
            }
            else
            {
                ApplyDoorAngle(openAngleDegrees);
                FinishOpen();
            }
        }

        private IEnumerator OpenDoorRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.001f, openDurationSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smooth cubic ease-out for a weighted industrial steel door swing.
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                ApplyDoorAngle(openAngleDegrees * eased);
                yield return null;
            }

            ApplyDoorAngle(openAngleDegrees);
            openRoutine = null;
            FinishOpen();
        }

        private void FinishOpen()
        {
            IsOpening = false;
            IsOpened = true;
            if (scenario != null) scenario.NotifyExitReached();
        }

        private void ApplyDoorAngle(float degrees)
        {
            if (doorLeafTransform == null) return;
            Vector3 axis = openAxis.sqrMagnitude > 0.0001f ? openAxis.normalized : Vector3.up;
            doorLeafTransform.localRotation = closedLocalRotation * Quaternion.AngleAxis(degrees, axis);
        }

        private void CaptureClosedRotation()
        {
            if (closedRotationCaptured || doorLeafTransform == null) return;
            closedLocalRotation = doorLeafTransform.localRotation;
            closedRotationCaptured = true;
        }

        private void EnsureDoorInteractable()
        {
            if (doorInteractable != null) return;
            doorInteractable = GetComponentInChildren<Interactable>(true);
        }

        private void WireDoorInteractable()
        {
            if (doorInteractable == null || wiredInteractable == doorInteractable) return;
            UnwireDoorInteractable();
            wiredInteractable = doorInteractable;
            wiredInteractable.SelectEntered += OnDoorActivated;
            wiredInteractable.GrabEntered += OnDoorActivated;
        }

        private void UnwireDoorInteractable()
        {
            if (wiredInteractable == null) return;
            wiredInteractable.SelectEntered -= OnDoorActivated;
            wiredInteractable.GrabEntered -= OnDoorActivated;
            wiredInteractable = null;
        }

        private void OnDoorActivated(Hand hand)
        {
            RequestExitConfirmation();
        }

        private void PlayClip(AudioClip clip)
        {
            if (oneShotAudio == null || clip == null) return;
            oneShotAudio.PlayOneShot(clip);
        }
    }
}
