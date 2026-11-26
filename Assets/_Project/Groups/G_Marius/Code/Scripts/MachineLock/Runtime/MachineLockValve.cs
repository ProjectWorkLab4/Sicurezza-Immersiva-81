using SI81.Input;
using SI81.Interaction;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Leak valve of the Machine Lock scenario (GDD §8.2 and §9.7). Selecting it turns the wheel 90°,
    /// plays a metal squeak, stops the jet and the puddle growth and reports the closed valve to the spill
    /// system and to the scenario. No score: the GDD only asks for a good-practice debrief line.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Valve")]
    public sealed class MachineLockValve : MonoBehaviour
    {
        [Header("Scenario")]
        [Tooltip("Scenario that receives the closed valve event. Optional.")]
        [SerializeField] private MachineLockScenario scenario;
        [Tooltip("Spill system that owns the jet, the puddle and the leak audio.")]
        [SerializeField] private MachineLockSpillSystem spillSystem;

        [Header("Interaction")]
        [Tooltip("Interactable on the valve. Selecting it closes the valve once.")]
        [SerializeField] private Interactable interactable;

        [Header("Wheel")]
        [Tooltip("Wheel that turns when the valve closes. Empty: the MESH_Valve_Wheel child.")]
        [SerializeField] private Transform wheelTransform;
        [Tooltip("Axis the wheel turns around, in the local space of the wheel.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;
        [Tooltip("Rotation of the wheel when the valve is closed, in degrees.")]
        [SerializeField] private float closedRotationAngle = 90f;
        [Tooltip("Seconds the wheel takes to reach the closed angle. 0 turns it at once.")]
        [SerializeField] private float rotationSeconds = 0.25f;

        [Header("Audio")]
        [Tooltip("Source used for the metal squeak.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Metal squeak played when the valve closes.")]
        [SerializeField] private AudioClip squeakClip;

        private Interactable wiredInteractable;
        private Quaternion rotationStart;
        private Quaternion rotationTarget;
        private float rotationElapsed;
        private bool rotating;

        /// <summary>true after the valve was closed. Closing is idempotent.</summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Closes the valve: turns the wheel, disables selection, plays the squeak and reports the event.
        /// The first call wins; later calls do nothing.
        /// </summary>
        public void Close()
        {
            if (IsClosed) return;
            IsClosed = true;

            BeginRotation();
            if (interactable != null) interactable.Selectable = false;
            PlayOneShot(squeakClip);

            if (spillSystem != null) spillSystem.CloseValve();
            if (scenario != null) scenario.NotifyValveClosed();
        }

        private void Awake()
        {
            if (interactable == null) interactable = GetComponent<Interactable>();
            if (interactable == null) interactable = GetComponentInChildren<Interactable>(true);
            if (wheelTransform == null) wheelTransform = transform.Find("MESH_Valve_Wheel");
            if (rotationAxis.sqrMagnitude < 0.0001f) rotationAxis = Vector3.forward;
        }

        private void OnEnable()
        {
            Wire();
        }

        private void OnDisable()
        {
            Unwire();
        }

        private void Update()
        {
            if (!rotating || wheelTransform == null) return;

            rotationElapsed += Time.deltaTime;
            float duration = Mathf.Max(0.0001f, rotationSeconds);
            float phase = Mathf.Clamp01(rotationElapsed / duration);
            float smooth = phase * phase * (3f - 2f * phase);
            wheelTransform.localRotation = Quaternion.Slerp(rotationStart, rotationTarget, smooth);
            if (phase >= 1f) rotating = false;
        }

        private void BeginRotation()
        {
            if (wheelTransform == null) return;
            rotationStart = wheelTransform.localRotation;
            rotationTarget = rotationStart * Quaternion.AngleAxis(closedRotationAngle, rotationAxis.normalized);
            rotationElapsed = 0f;
            rotating = true;

            if (rotationSeconds <= 0f)
            {
                wheelTransform.localRotation = rotationTarget;
                rotating = false;
            }
        }

        private void Wire()
        {
            if (interactable == null || wiredInteractable == interactable) return;
            Unwire();
            wiredInteractable = interactable;
            wiredInteractable.SelectEntered += OnSelected;
        }

        private void Unwire()
        {
            if (wiredInteractable == null) return;
            wiredInteractable.SelectEntered -= OnSelected;
            wiredInteractable = null;
        }

        private void OnSelected(Hand hand)
        {
            Close();
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (oneShotAudio != null && clip != null) oneShotAudio.PlayOneShot(clip);
        }
    }
}
