using System.Collections;
using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Interactive hinged steel exit door for the Machine Lock scenario (ADR-0006, Issue #41).
    /// Replaces the old walk-into <c>ScenarioTrigger</c> with a physical panic bar that accepts both
    /// Select (Trigger) and Grab (Grip) without pulling the bar off the door. Activating the panic bar
    /// opens the shared <c>HUD_DecisionPanel</c> via <see cref="MachineLockScenario.RequestExitConfirmation"/>.
    /// Confirming exit plays the latch sound, disables the leaf collider, swings the door open 90 degrees
    /// into the west vestibule, and calls <see cref="MachineLockScenario.NotifyExitReached"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Exit Door")]
    public sealed class MachineLockExitDoor : MonoBehaviour
    {
        private const float DEFAULT_OPEN_ANGLE_DEGREES = 90f;
        private const float DEFAULT_OPEN_DURATION_SECONDS = 0.8f;

        [Header("Scenario")]
        [Tooltip("Scenario that displays the confirmation decision panel and resolves exit handover.")]
        [SerializeField] private MachineLockScenario scenario;

        [Header("Door and panic bar")]
        [Tooltip("Panic bar Interactable supporting both Select (Trigger) and Grab (Grip) with FollowsHand = false.")]
        [SerializeField] private Interactable panicBarInteractable;
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
        [Tooltip("One-shot audio source for the panic-bar latch release.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Mechanical latch release clip played when the door unlatches.")]
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
            EnsurePanicBarInteractable();
        }

        private void OnEnable()
        {
            EnsurePanicBarInteractable();
            WirePanicBar();
        }

        private void OnDisable()
        {
            UnwirePanicBar();
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }
            IsOpening = false;
            IsConfirmationOpen = false;
        }

        /// <summary>
        /// Opens the shared exit confirmation panel through <see cref="MachineLockScenario.RequestExitConfirmation"/>.
        /// Ignored when the confirmation is already open, the door is already opening/opened, or the scenario ended.
        /// </summary>
        /// <returns>true when the decision panel opened cleanly.</returns>
        public bool RequestExitConfirmation()
        {
            if (IsOpened || IsOpening || IsConfirmationOpen) return false;
            if (scenario == null || scenario.HasEnded) return false;

            IsConfirmationOpen = true;
            bool opened = scenario.RequestExitConfirmation(HandleExitDecision);
            if (!opened)
            {
                IsConfirmationOpen = false;
                return false;
            }
            return true;
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

            if (panicBarInteractable != null)
            {
                panicBarInteractable.Selectable = false;
                panicBarInteractable.Grabbable = false;
            }
            if (doorLeafCollider != null)
                doorLeafCollider.enabled = false;

            PlayLatchClip();

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

        private void EnsurePanicBarInteractable()
        {
            if (panicBarInteractable != null) return;
            panicBarInteractable = GetComponentInChildren<Interactable>(true);
        }

        private void WirePanicBar()
        {
            if (panicBarInteractable == null || wiredInteractable == panicBarInteractable) return;
            UnwirePanicBar();
            wiredInteractable = panicBarInteractable;
            wiredInteractable.SelectEntered += OnPanicBarActivated;
            wiredInteractable.GrabEntered += OnPanicBarActivated;
        }

        private void UnwirePanicBar()
        {
            if (wiredInteractable == null) return;
            wiredInteractable.SelectEntered -= OnPanicBarActivated;
            wiredInteractable.GrabEntered -= OnPanicBarActivated;
            wiredInteractable = null;
        }

        private void OnPanicBarActivated(Hand hand)
        {
            RequestExitConfirmation();
        }

        private void PlayLatchClip()
        {
            if (oneShotAudio == null || latchClip == null) return;
            oneShotAudio.PlayOneShot(latchClip);
        }
    }
}
