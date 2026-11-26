using System.Collections;
using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Hydraulic press of the Machine Lock scenario (GDD §9.1): irregular ram cycle, blinking fault
    /// display, E-stop, main power cut and the maintenance try-start.
    /// No Animator and no Meta SDK: the ram moves from <see cref="Update"/> and the two controls are plain
    /// <see cref="Interactable"/> components. Every scenario call is null-safe and idempotent.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Press")]
    public sealed class MachineLockPress : MonoBehaviour
    {
        private const float MIN_PERIOD_SECONDS = 6f;
        private const float MAX_PERIOD_SECONDS = 8f;
        private const float DOWN_END = 0.40f;
        private const float DWELL_END = 0.50f;
        private const float UP_END = 0.90f;
        private const float BLINK_SPEED = 3.1f;
        private const float JOG_SECONDS = 1.4f;
        private const uint RANDOM_SEED = 0x9E3779B9u;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color DefaultFaultEmission = new Color(1f, 0.15f, 0.05f);

        [Header("Scenario")]
        [Tooltip("Scenario that receives the press events. Optional: the press still works without it.")]
        [SerializeField] private MachineLockScenario scenario;

        [Header("Ram")]
        [Tooltip("Moving ram of the press.")]
        [SerializeField] private Transform ramTransform;
        [Tooltip("Local rest position of the ram, before the downward travel.")]
        [SerializeField] private Vector3 ramBaseLocalPosition;
        [Tooltip("Downward travel of the ram, in metres (GDD: 0.6 m).")]
        [SerializeField] private float ramTravel = 0.6f;

        [Header("Fault display")]
        [Tooltip("Renderer of the HMI screen that blinks ALLARME 04 — PRESSIONE ANOMALA.")]
        [SerializeField] private Renderer displayRenderer;
        [Tooltip("Material of the display while the main power is on.")]
        [SerializeField] private Material displayFaultMaterial;
        [Tooltip("Material of the display while the main power is off.")]
        [SerializeField] private Material displayOffMaterial;

        [Header("Controls")]
        [Tooltip("Emergency stop mushroom.")]
        [SerializeField] private Interactable estopInteractable;
        [Tooltip("Maintenance test start button on the E-stop pedestal.")]
        [SerializeField] private Interactable testStartInteractable;
        [Tooltip("Hazard trigger of the press body. Kept for scene wiring: the contact is reported by OnTriggerEnter.")]
        [SerializeField] private Collider pressHazardTrigger;

        [Header("Audio")]
        [Tooltip("Looping hydraulic and motor audio.")]
        [SerializeField] private AudioSource loopAudio;
        [Tooltip("One-shot source for the clack and stop clips.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Clip played when the press stops.")]
        [SerializeField] private AudioClip stopClip;
        [Tooltip("Clip played on the E-stop and on the try-start.")]
        [SerializeField] private AudioClip clackClip;

        private bool running = true;
        private bool powered = true;
        private bool eStopPressed;
        private bool jogging;
        private bool displayPowered;
        private bool pauseConsumed;

        private float ramOffset;
        private float cycleElapsed;
        private float cyclePeriod = MIN_PERIOD_SECONDS;
        private float pauseWindowStart;
        private float pauseDuration;
        private float pauseRemaining;

        private uint randomState = RANDOM_SEED;
        private Vector3 resolvedBaseLocalPosition;
        private Color faultEmission = DefaultFaultEmission;
        private Color faultBaseColor = Color.white;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine jogRoutine;

        /// <summary>true while the ram is cycling, i.e. until the E-stop or a power cut.</summary>
        public bool IsRunning => running;
        /// <summary>true while the main circuit feeds the press.</summary>
        public bool IsPowered => powered;
        /// <summary>true after the E-stop mushroom was pressed once.</summary>
        public bool IsEStopPressed => eStopPressed;

        private void Awake()
        {
            resolvedBaseLocalPosition = ramBaseLocalPosition;
            if (resolvedBaseLocalPosition == Vector3.zero && ramTransform != null)
                resolvedBaseLocalPosition = ramTransform.localPosition;

            propertyBlock = new MaterialPropertyBlock();
            ReadDisplayColors();
            ConfigureAudio();
            BeginCycle();

            if (pressHazardTrigger != null && !pressHazardTrigger.isTrigger)
            {
                Debug.LogWarning("[SI81 MachineLock] MachineLockPress: the press hazard collider is not a trigger, " +
                                 "so press contact cannot be reported.", this);
            }
        }

        private void OnEnable()
        {
            if (estopInteractable != null) estopInteractable.SelectEntered += OnEStopSelected;
            if (testStartInteractable != null) testStartInteractable.SelectEntered += OnTestStartSelected;
            if (running && powered) PlayLoop();
        }

        private void OnDisable()
        {
            if (estopInteractable != null) estopInteractable.SelectEntered -= OnEStopSelected;
            if (testStartInteractable != null) testStartInteractable.SelectEntered -= OnTestStartSelected;
            if (jogRoutine != null) StopCoroutine(jogRoutine);
            jogRoutine = null;
            jogging = false;
            StopLoop();
        }

        private void Start()
        {
            if (running && powered) PlayLoop();
        }

        private void Update()
        {
            UpdateDisplay();
            if (!powered || jogging || !running) return;
            UpdateCycle(Time.deltaTime);
        }

        // =====================================================================
        // Public controls
        // =====================================================================

        /// <summary>
        /// Presses the emergency stop: the ram stops where it is, the loop audio stops and the display
        /// stays on because the auxiliary circuit is still live. Idempotent.
        /// </summary>
        public void PressEStop()
        {
            if (eStopPressed) return;
            eStopPressed = true;
            running = false;
            StopLoop();
            PlayOneShot(clackClip);
            PlayOneShot(stopClip);
            if (scenario != null) scenario.NotifyEStopPressed();
        }

        /// <summary>
        /// Applies the state of the main circuit. Opening the circuit stops the ram, silences the motor
        /// and turns the display dark; closing it restores the display and the cycle unless the E-stop is latched.
        /// </summary>
        /// <param name="on">true when the main circuit is live.</param>
        public void SetPower(bool on)
        {
            if (!on)
            {
                powered = false;
                running = false;
                StopLoop();
                ApplyDisplayState(false);
                return;
            }

            if (powered) return;
            powered = true;
            ApplyDisplayState(true);
            if (eStopPressed) return;
            running = true;
            PlayLoop();
        }

        /// <summary>
        /// Maintenance try-start. It bypasses the E-stop: with the main circuit still live the ram jogs
        /// and the scenario records a critical event; with the circuit open nothing moves and the
        /// isolation is verified.
        /// </summary>
        public void TryStart()
        {
            if (!powered)
            {
                if (scenario != null) scenario.NotifyTryStartVerified();
                return;
            }

            if (jogRoutine != null) return;
            jogRoutine = StartCoroutine(JogRoutine());
        }

        /// <summary>
        /// Reports a player contact with the hazard trigger while the ram is moving.
        /// </summary>
        /// <param name="other">Collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (!running || other == null) return;
            if (!IsPlayerCollider(other)) return;
            if (scenario != null) scenario.NotifyCritical(CriticalEvent.PressContact);
        }

        // =====================================================================
        // Movement
        // =====================================================================

        private IEnumerator JogRoutine()
        {
            if (scenario != null) scenario.NotifyCritical(CriticalEvent.TryStartUnisolated);
            jogging = true;
            PlayLoop();
            PlayOneShot(clackClip);

            float elapsed = 0f;
            while (elapsed < JOG_SECONDS)
            {
                elapsed += Time.deltaTime;
                float phase = Mathf.Clamp01(elapsed / JOG_SECONDS);
                float amount = phase < 0.5f
                    ? SmoothStep01(phase * 2f)
                    : 1f - SmoothStep01((phase - 0.5f) * 2f);
                ApplyRamOffset(amount);
                yield return null;
            }

            ApplyRamOffset(ramOffset);
            jogging = false;
            jogRoutine = null;
            if (!running) StopLoop();
        }

        private void UpdateCycle(float deltaTime)
        {
            if (pauseRemaining > 0f)
            {
                pauseRemaining = Mathf.Max(0f, pauseRemaining - deltaTime);
                ApplyRamOffset(ramOffset);
                return;
            }

            cycleElapsed += deltaTime;
            float phase = cyclePeriod > 0f ? cycleElapsed / cyclePeriod : 0f;
            if (phase >= 1f)
            {
                BeginCycle();
                phase = 0f;
            }

            if (!pauseConsumed && pauseDuration > 0f && phase >= pauseWindowStart)
            {
                pauseConsumed = true;
                pauseRemaining = pauseDuration;
            }

            ramOffset = OffsetForPhase(phase);
            ApplyRamOffset(ramOffset);
        }

        private void BeginCycle()
        {
            cyclePeriod = MIN_PERIOD_SECONDS + Next01() * (MAX_PERIOD_SECONDS - MIN_PERIOD_SECONDS);
            pauseWindowStart = 0.15f + Next01() * 0.55f;
            pauseDuration = Next01() < 0.45f ? 0.35f + Next01() * 0.65f : 0f;
            pauseConsumed = false;
            pauseRemaining = 0f;
            cycleElapsed = 0f;
        }

        private static float OffsetForPhase(float phase)
        {
            if (phase < DOWN_END) return SmoothStep01(phase / DOWN_END);
            if (phase < DWELL_END) return 1f;
            if (phase < UP_END) return 1f - SmoothStep01((phase - DWELL_END) / (UP_END - DWELL_END));
            return 0f;
        }

        private void ApplyRamOffset(float amount)
        {
            if (ramTransform == null) return;
            ramTransform.localPosition = resolvedBaseLocalPosition + Vector3.down * (ramTravel * Mathf.Clamp01(amount));
        }

        /// <summary>
        /// Deterministic linear congruential generator: the same seed always produces the same pauses,
        /// so the press behaves identically in a test replay.
        /// </summary>
        private float Next01()
        {
            randomState = randomState * 1664525u + 1013904223u;
            return (randomState >> 8) / 16777216f;
        }

        private static float SmoothStep01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        // =====================================================================
        // Display, audio, helpers
        // =====================================================================

        private void UpdateDisplay()
        {
            ApplyDisplayState(powered);
            if (!powered || displayRenderer == null || propertyBlock == null) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * BLINK_SPEED);
            float blink = 0.2f + 0.8f * pulse;
            displayRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, faultEmission * blink);
            propertyBlock.SetColor(BaseColorId, faultBaseColor * (0.4f + 0.6f * blink));
            displayRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ApplyDisplayState(bool on)
        {
            if (displayRenderer == null || displayPowered == on) return;
            displayPowered = on;
            if (on)
            {
                if (displayFaultMaterial != null) displayRenderer.sharedMaterial = displayFaultMaterial;
            }
            else if (displayOffMaterial != null)
            {
                displayRenderer.sharedMaterial = displayOffMaterial;
            }
            displayRenderer.SetPropertyBlock(null);
        }

        private void ReadDisplayColors()
        {
            faultEmission = DefaultFaultEmission;
            faultBaseColor = Color.white;
            if (displayFaultMaterial == null) return;
            if (displayFaultMaterial.HasProperty(EmissionColorId)) faultEmission = displayFaultMaterial.GetColor(EmissionColorId);
            if (displayFaultMaterial.HasProperty(BaseColorId)) faultBaseColor = displayFaultMaterial.GetColor(BaseColorId);
        }

        private void ConfigureAudio()
        {
            if (loopAudio != null)
            {
                loopAudio.loop = true;
                loopAudio.playOnAwake = false;
            }
            if (oneShotAudio != null) oneShotAudio.playOnAwake = false;
        }

        private void PlayLoop()
        {
            if (loopAudio == null || loopAudio.isPlaying) return;
            loopAudio.Play();
        }

        private void StopLoop()
        {
            if (loopAudio == null) return;
            loopAudio.Stop();
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (oneShotAudio == null || clip == null) return;
            oneShotAudio.PlayOneShot(clip);
        }

        private void OnEStopSelected(Hand hand)
        {
            PressEStop();
        }

        private void OnTestStartSelected(Hand hand)
        {
            TryStart();
        }

        /// <summary>
        /// true when the collider belongs to the player rig: a CharacterController, the Player tag or a
        /// root that carries the shared <see cref="RigRegistration"/>.
        /// </summary>
        private static bool IsPlayerCollider(Collider other)
        {
            if (other.GetComponentInParent<CharacterController>() != null) return true;
            if (other.CompareTag("Player")) return true;
            if (other.GetComponentInParent<RigRegistration>() != null) return true;
            return other.transform.root.GetComponentInChildren<RigRegistration>(true) != null;
        }
    }
}
