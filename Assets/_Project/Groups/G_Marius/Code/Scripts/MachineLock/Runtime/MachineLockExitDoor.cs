using System.Collections;
using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
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
    /// auto-closes the confirmation if the player walks away (&gt; 3.5 m).
    /// Confirming exit locks player movement and world interaction immediately (keeping UI interactive),
    /// moves the player cleanly in front of the door, unlatches and swings the steel leaf open 90 degrees
    /// outward (-X), fades the level smoothly to pure black (hiding 3D scene visuals behind the UI so
    /// end-of-level panels never compenetrate with geometry), and ends the mission.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Exit Door")]
    public sealed class MachineLockExitDoor : MonoBehaviour
    {
        /// <summary>Horizontal distance in metres beyond which an unanswered exit confirmation auto-closes.</summary>
        public const float AUTO_CLOSE_DISTANCE_METERS = 3.5f;

        private const float DEFAULT_OPEN_ANGLE_DEGREES = 90f;
        private const float DEFAULT_OPEN_DURATION_SECONDS = 0.8f;
        private const float DEFAULT_MOVE_TO_DOOR_SECONDS = 0.45f;
        private const float DEFAULT_FADE_TO_BLACK_SECONDS = 0.95f;
        private const float YAW_ALIGN_THRESHOLD_DEGREES = 45f;

        [Header("Scenario")]
        [Tooltip("Scenario that displays the confirmation decision panel, locks movement, fades to black, and resolves exit handover.")]
        [SerializeField] private MachineLockScenario scenario;

        [Header("Door and panic bar")]
        [Tooltip("Exit door Interactable (on MESH_SteelExitDoorLeaf covering both leaf and panic bar) supporting Select (Trigger) and Grab (Grip) with FollowsHand = false.")]
        [FormerlySerializedAs("panicBarInteractable")]
        [SerializeField] private Interactable doorInteractable;
        [Tooltip("Hinged steel door leaf transform rotated around its hinge pivot when opening.")]
        [SerializeField] private Transform doorLeafTransform;
        [Tooltip("Solid door leaf collider disabled once the player confirms exit.")]
        [SerializeField] private Collider doorLeafCollider;

        [Header("Motion & Exit Sequence")]
        [Tooltip("Local axis around which the door leaf swings open.")]
        [SerializeField] private Vector3 openAxis = Vector3.up;
        [Tooltip("Swing angle in degrees when opening (+90 swings outward along -X into the west vestibule).")]
        [SerializeField] private float openAngleDegrees = DEFAULT_OPEN_ANGLE_DEGREES;
        [Tooltip("Seconds taken to swing the door open; 0 snaps open immediately.")]
        [SerializeField] private float openDurationSeconds = DEFAULT_OPEN_DURATION_SECONDS;
        [Tooltip("Local offset in front of the door where the player is positioned on exit confirmation (1.65 m east of door center).")]
        [SerializeField] private Vector3 exitStandLocalOffset = new Vector3(1.65f, 0f, 0f);
        [Tooltip("Seconds taken to glide the player smoothly in front of the door while movement is locked.")]
        [SerializeField] private float moveToDoorDurationSeconds = DEFAULT_MOVE_TO_DOOR_SECONDS;
        [Tooltip("Seconds taken to fade the room smoothly to pure black as the door opens (0 skips fade).")]
        [SerializeField] private float fadeToBlackDurationSeconds = DEFAULT_FADE_TO_BLACK_SECONDS;

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
        /// <summary>true while the exit sequence (move to door, swing open, fade to black) is running.</summary>
        public bool IsOpening { get; private set; }
        /// <summary>true once the door has fully swung open and handed control to the scenario exit flow.</summary>
        public bool IsOpened { get; private set; }

        /// <summary>World-space standing point directly in front of the exit door used during the locked exit sequence.</summary>
        public Vector3 ExitStandWorldPosition => transform.TransformPoint(exitStandLocalOffset);

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
        /// Callback passed to <c>HUD_DecisionPanel</c>. Option 0 confirms exit and starts the locked
        /// exit sequence; any other option (1 = stay, or timeout) cancels without ending the mission.
        /// </summary>
        /// <param name="optionIndex">0 = confirm exit, 1 = stay in room, -1 = timeout.</param>
        public void HandleExitDecision(int optionIndex)
        {
            IsConfirmationOpen = false;
            if (optionIndex == 0)
                OpenAndCompleteExit();
        }

        /// <summary>
        /// Locks movement and world interaction immediately (keeping UI interactive), disables the door
        /// interactable and leaf collider, moves the player in front of the door, unlatches and swings
        /// the steel leaf open by <see cref="openAngleDegrees"/>, fades the level to black, hides 3D
        /// workshop visuals behind the UI backdrop, and completes the mission exit.
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

            // Lock movement and world interaction immediately while leaving the UI channel active.
            if (scenario != null)
                scenario.BeginExitSequenceLock();

            if (Application.isPlaying && openDurationSeconds > 0.001f && doorLeafTransform != null)
            {
                if (openRoutine != null) StopCoroutine(openRoutine);
                openRoutine = StartCoroutine(OpenDoorAndFadeSequence());
            }
            else
            {
                PlayClip(latchClip);
                ApplyDoorAngle(openAngleDegrees);
                FinishOpen(false);
            }
        }

        private IEnumerator OpenDoorAndFadeSequence()
        {
            // 1. Move the locked player smoothly in front of the West Exit Door so they watch it open cleanly.
            yield return MovePlayerToDoorFrontRoutine();

            // 2. Unlatch and smoothly swing the steel exit door outward (+90 deg along -X).
            PlayClip(latchClip);
            bool runFade = scenario != null && scenario.HasExitFadeFeedback && fadeToBlackDurationSeconds > 0.001f;
            float openDuration = Mathf.Max(0.001f, openDurationSeconds);
            float fadeTriggerTime = openDuration * 0.5f;
            bool fadeStarted = false;
            bool fadeCompleted = false;

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                ApplyDoorAngle(openAngleDegrees * eased);

                if (runFade && !fadeStarted && elapsed >= fadeTriggerTime)
                {
                    fadeStarted = true;
                    scenario.StartExitFadeToBlack(fadeToBlackDurationSeconds, () => fadeCompleted = true);
                }
                yield return null;
            }

            ApplyDoorAngle(openAngleDegrees);

            // 3. Finish fading the room to pure black while movement stays locked, then clear 3D visuals and end level.
            if (runFade)
            {
                if (!fadeStarted)
                    scenario.StartExitFadeToBlack(fadeToBlackDurationSeconds, () => fadeCompleted = true);

                while (!fadeCompleted)
                    yield return null;

                scenario.HideScenarioWorldVisualsForEndUi();
                openRoutine = null;
                FinishOpen(endImmediately: true);
                yield break;
            }

            openRoutine = null;
            FinishOpen(endImmediately: false);
        }

        private IEnumerator MovePlayerToDoorFrontRoutine()
        {
            RigRegistration rig = RigRegistration.Instance;
            if (rig == null) yield break;

            Transform rigTransform = rig.transform;
            Transform head = rig.Head;
            Vector3 targetEyeWorld = ExitStandWorldPosition;
            Vector3 doorDirection = (transform.position - targetEyeWorld);
            doorDirection.y = 0f;
            if (doorDirection.sqrMagnitude < 0.0001f) doorDirection = Vector3.left;
            doorDirection.Normalize();

            // If the player turned well away from the door before clicking Yes, snap-turn once to face it comfortably.
            Quaternion targetRigRotation = rigTransform.rotation;
            if (head != null)
            {
                Vector3 headForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (headForward.sqrMagnitude > 0.0001f
                    && Vector3.Angle(headForward.normalized, doorDirection) > YAW_ALIGN_THRESHOLD_DEGREES)
                {
                    float yawDelta = Vector3.SignedAngle(headForward.normalized, doorDirection, Vector3.up);
                    targetRigRotation = Quaternion.AngleAxis(yawDelta, Vector3.up) * rigTransform.rotation;
                    Vector3 rotatedOffset = Quaternion.AngleAxis(yawDelta, Vector3.up)
                        * new Vector3(head.position.x - rigTransform.position.x, 0f, head.position.z - rigTransform.position.z);
                    Vector3 pivotRigPos = new Vector3(head.position.x - rotatedOffset.x, rigTransform.position.y, head.position.z - rotatedOffset.z);
                    rig.TeleportTo(pivotRigPos, targetRigRotation);
                }
            }

            Vector3 headOffset = head != null
                ? new Vector3(head.position.x - rigTransform.position.x, 0f, head.position.z - rigTransform.position.z)
                : Vector3.zero;
            Vector3 startPos = rigTransform.position;
            Vector3 endPos = new Vector3(targetEyeWorld.x - headOffset.x, startPos.y, targetEyeWorld.z - headOffset.z);

            float horizontalDistance = Vector2.Distance(new Vector2(startPos.x, startPos.z), new Vector2(endPos.x, endPos.z));
            if (horizontalDistance <= 0.05f || moveToDoorDurationSeconds <= 0.001f)
            {
                rig.TeleportTo(endPos, targetRigRotation);
                yield break;
            }

            float duration = Mathf.Max(0.05f, moveToDoorDurationSeconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 pos = Vector3.Lerp(startPos, endPos, eased);
                rig.TeleportTo(pos, targetRigRotation);
                yield return null;
            }

            rig.TeleportTo(endPos, targetRigRotation);
        }

        private void FinishOpen(bool endImmediately)
        {
            IsOpening = false;
            IsOpened = true;
            if (scenario != null) scenario.NotifyExitReached(endImmediately);
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
