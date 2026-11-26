using System.Collections;
using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Electrical wall bay of the Machine Lock scenario (GDD §9.3 and §9.4, ADR-0002): three physically
    /// separated, high-contrast color-coded Sub-Stations (Main Disconnect Switch, Lockout Station, Voltage
    /// Tester). Power starts ON; opening the main switch cuts press power and unlocks the lockout station;
    /// residual charge decays linearly only while the main switch is open. The tester never discharges the
    /// oil, it only reads it. Hovering any Sub-Station emits throttled Action Feedback with its name and
    /// live state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Energy Panel")]
    public sealed class MachineLockEnergyPanel : MonoBehaviour
    {
        /// <summary>Charge above which the oil still counts as energised (GDD §9.4).</summary>
        public const float ENERGIZED_THRESHOLD = 0.05f;

        private const float DEFAULT_DECAY_SECONDS = 90f;
        private const float DEFAULT_ENERGIZED_PROBABILITY = 0.5f;
        private const float SWITCH_OFF_ANGLE = -80f;
        private const float ARC_PULSE_SECONDS = 0.35f;
        private const float HOVER_THROTTLE_SECONDS = 0.25f;

        private static readonly WaitForSeconds ArcPulseWait = new WaitForSeconds(ARC_PULSE_SECONDS);

        [Header("Scenario")]
        [Tooltip("Scenario that receives the panel events. Optional.")]
        [SerializeField] private MachineLockScenario scenario;
        [Tooltip("Tunable values. GDD defaults are used when empty.")]
        [SerializeField] private MachineLockConfig config;
        [Tooltip("Press that loses power when the main switch opens.")]
        [SerializeField] private MachineLockPress press;

        [Header("Sub-Station Controls")]
        [Tooltip("Sub-Station 1: Main disconnect switch.")]
        [SerializeField] private Interactable mainSwitchInteractable;
        [Tooltip("Sub-Station 2: Lockout station, selectable only after main power is off.")]
        [SerializeField] private Interactable lockoutInteractable;
        [Tooltip("Sub-Station 3: Voltage tester.")]
        [SerializeField] private Interactable testerInteractable;
        [Tooltip("Rotary disconnect handle on the Main Disconnect Switch Sub-Station.")]
        [SerializeField] private Transform switchHandle;

        [Header("LEDs & Status Lamps")]
        [Tooltip("Optional ON (red) indicator on the Main Disconnect Switch Sub-Station.")]
        [SerializeField] private Renderer mainSwitchOnLedRenderer;
        [Tooltip("Optional OFF (green) indicator on the Main Disconnect Switch Sub-Station.")]
        [SerializeField] private Renderer mainSwitchOffLedRenderer;
        [Tooltip("Optional READY (yellow/green) indicator on the Lockout Station when unlocked after main off.")]
        [SerializeField] private Renderer lockoutReadyLedRenderer;
        [Tooltip("Red LOCK lamp of the Lockout Station.")]
        [SerializeField] private Renderer lockoutLedRenderer;
        [Tooltip("Red LIVE lamp of the Voltage Tester.")]
        [SerializeField] private Renderer testerRedLedRenderer;
        [Tooltip("Green ZERO lamp of the Voltage Tester.")]
        [SerializeField] private Renderer testerGreenLedRenderer;
        [Tooltip("Material of a lamp that is off.")]
        [SerializeField] private Material ledOffMaterial;
        [Tooltip("Material of a lamp that is on and red.")]
        [SerializeField] private Material ledRedMaterial;
        [Tooltip("Material of a lamp that is on and green.")]
        [SerializeField] private Material ledGreenMaterial;

        [Header("VFX and audio")]
        [Tooltip("Electric arc shown by the tester while the main circuit is live.")]
        [SerializeField] private GameObject arcVfx;
        [Tooltip("One-shot source for the panel clips.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Clip of the main breaker opening.")]
        [SerializeField] private AudioClip breakerOffClip;
        [Tooltip("Clip of the lockout being applied.")]
        [SerializeField] private AudioClip lockoutClip;
        [Tooltip("Beep of the voltage tester.")]
        [SerializeField] private AudioClip testerBeepClip;

        private Quaternion switchOnRotation;
        private Coroutine arcRoutine;
        private ParticleSystem arcParticles;
        private float lastHoverTime = float.NegativeInfinity;

        /// <summary>true while the main circuit is closed (starts ON, GDD §9.3).</summary>
        public bool MainPowerOn { get; private set; } = true;
        /// <summary>true after the abstract lockout was applied.</summary>
        public bool LockedOut { get; private set; }
        /// <summary>true while residual energy remains in the oil.</summary>
        public bool OilEnergized => Charge01 > ENERGIZED_THRESHOLD;
        /// <summary>Normalized residual charge, 0 to 1.</summary>
        public float Charge01 { get; private set; }
        /// <summary>true after the tester confirmed ZERO.</summary>
        public bool TesterZero { get; private set; }
        /// <summary>Most recent hover Action Feedback string emitted by any of the three Sub-Stations.</summary>
        public string LastHoverFeedback { get; private set; } = string.Empty;

        private void Awake()
        {
            if (switchHandle != null) switchOnRotation = switchHandle.localRotation;
            if (oneShotAudio != null) oneShotAudio.playOnAwake = false;
            if (arcVfx != null)
            {
                arcParticles = arcVfx.GetComponentInChildren<ParticleSystem>(true);
                arcVfx.SetActive(false);
            }

            float probability = config != null ? config.energizedProbability : DEFAULT_ENERGIZED_PROBABILITY;
            Charge01 = UnityEngine.Random.value < probability ? 1f : 0f;

            RefreshStatusLamps();
            ApplyLed(testerRedLedRenderer, ledOffMaterial);
            ApplyLed(testerGreenLedRenderer, ledOffMaterial);
            SetLockoutSelectable(false);
        }

        private void Start()
        {
            if (press != null) press.SetPower(MainPowerOn);
        }

        private void OnEnable()
        {
            if (mainSwitchInteractable != null)
            {
                mainSwitchInteractable.SelectEntered += OnMainSwitchSelected;
                mainSwitchInteractable.HoverEntered += OnMainSwitchHovered;
            }
            if (lockoutInteractable != null)
            {
                lockoutInteractable.SelectEntered += OnLockoutSelected;
                lockoutInteractable.HoverEntered += OnLockoutHovered;
            }
            if (testerInteractable != null)
            {
                testerInteractable.SelectEntered += OnTesterSelected;
                testerInteractable.HoverEntered += OnTesterHovered;
            }
        }

        private void OnDisable()
        {
            if (mainSwitchInteractable != null)
            {
                mainSwitchInteractable.SelectEntered -= OnMainSwitchSelected;
                mainSwitchInteractable.HoverEntered -= OnMainSwitchHovered;
            }
            if (lockoutInteractable != null)
            {
                lockoutInteractable.SelectEntered -= OnLockoutSelected;
                lockoutInteractable.HoverEntered -= OnLockoutHovered;
            }
            if (testerInteractable != null)
            {
                testerInteractable.SelectEntered -= OnTesterSelected;
                testerInteractable.HoverEntered -= OnTesterHovered;
            }
            if (arcRoutine != null) StopCoroutine(arcRoutine);
            arcRoutine = null;
            if (arcParticles != null) arcParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (arcVfx != null) arcVfx.SetActive(false);
        }

        private void Update()
        {
            if (MainPowerOn || Charge01 <= 0f) return;
            float decaySeconds = config != null ? config.energyDecaySeconds : DEFAULT_DECAY_SECONDS;
            if (decaySeconds <= 0f)
            {
                Charge01 = 0f;
                return;
            }
            Charge01 = Mathf.Max(0f, Charge01 - Time.deltaTime / decaySeconds);
        }

        // =====================================================================
        // Hover status text helpers
        // =====================================================================

        /// <summary>Current Italian hover Action Feedback for Sub-Station 1 (Main Disconnect Switch).</summary>
        public string GetMainSwitchHoverText()
        {
            return MainPowerOn ? MachineLockTexts.HoverMainSwitchOn : MachineLockTexts.HoverMainSwitchOff;
        }

        /// <summary>Current Italian hover Action Feedback for Sub-Station 2 (Lockout Station).</summary>
        public string GetLockoutHoverText()
        {
            if (LockedOut) return MachineLockTexts.HoverLockoutApplied;
            return MainPowerOn ? MachineLockTexts.HoverLockoutBlocked : MachineLockTexts.HoverLockoutReady;
        }

        /// <summary>Current Italian hover Action Feedback for Sub-Station 3 (Voltage Tester).</summary>
        public string GetTesterHoverText()
        {
            if (TesterZero) return MachineLockTexts.HoverTesterZeroConfirmed;
            return MainPowerOn ? MachineLockTexts.HoverTesterLivePanel : MachineLockTexts.HoverTesterCheckResidual;
        }

        // =====================================================================
        // Explicit state (tests and debug menu)
        // =====================================================================

        /// <summary>
        /// Resets the panel to a known state: main power on, no lockout and the charge passed in.
        /// Used by the tests and the debug menu; the tester must still be read to confirm ZERO.
        /// </summary>
        public void InitializeState(bool energized)
        {
            MainPowerOn = true;
            LockedOut = false;
            Charge01 = energized ? 1f : 0f;
            TesterZero = false;

            ApplySwitchHandle(true);
            RefreshStatusLamps();
            ApplyLed(testerRedLedRenderer, ledOffMaterial);
            ApplyLed(testerGreenLedRenderer, ledOffMaterial);
            SetLockoutSelectable(false);
            if (arcVfx != null) arcVfx.SetActive(false);
            if (press != null) press.SetPower(true);
        }

        /// <summary>Forces the oil to full residual charge and invalidates a previous ZERO reading.</summary>
        public void ForceEnergized()
        {
            Charge01 = 1f;
            TesterZero = false;
        }

        /// <summary>Forces the oil to zero charge and invalidates a previous ZERO reading.</summary>
        public void ForceZeroEnergy()
        {
            Charge01 = 0f;
            TesterZero = false;
        }

        // =====================================================================
        // Main switch, lockout, tester
        // =====================================================================

        /// <summary>
        /// Opens or closes the main switch. Opening it turns the display and the motor off, enables the
        /// lockout station and notifies the scenario; it is ignored when the lockout is already applied.
        /// </summary>
        public void SetMainPower(bool on)
        {
            if (on)
            {
                if (MainPowerOn || LockedOut) return;
                MainPowerOn = true;
                ApplySwitchHandle(true);
                RefreshStatusLamps();
                SetLockoutSelectable(false);
                if (press != null) press.SetPower(true);
                return;
            }

            if (!MainPowerOn) return;
            MainPowerOn = false;
            ApplySwitchHandle(false);
            RefreshStatusLamps();
            SetLockoutSelectable(!LockedOut);
            if (press != null) press.SetPower(false);
            PlayClip(breakerOffClip);
            if (scenario != null) scenario.NotifyMainPowerOff();
        }

        /// <summary>Opens the main switch. Bound to the switch <see cref="Interactable"/>.</summary>
        public void OpenMainSwitch()
        {
            SetMainPower(false);
        }

        /// <summary>
        /// Applies the abstract lockout. Only valid with main power off; lights the red LOCK lamp,
        /// disables further lockout selection and notifies the scenario.
        /// </summary>
        public void ApplyLockout()
        {
            if (MainPowerOn || LockedOut) return;
            LockedOut = true;
            RefreshStatusLamps();
            SetLockoutSelectable(false);
            PlayClip(lockoutClip);
            if (scenario != null) scenario.NotifyLockoutApplied();
        }

        /// <summary>
        /// Reads the voltage tester without discharging the oil: LIVE with the main circuit closed or
        /// residual charge left, ZERO only below <see cref="ENERGIZED_THRESHOLD"/>.
        /// </summary>
        public string ReadTester()
        {
            PlayClip(testerBeepClip);

            if (MainPowerOn)
            {
                ShowLive(true);
                if (scenario != null) scenario.NotifyTesterRead(false, MachineLockTexts.TesterReadLivePanel);
                return MachineLockTexts.TesterReadLivePanel;
            }

            if (Charge01 > ENERGIZED_THRESHOLD)
            {
                ShowLive(false);
                if (scenario != null) scenario.NotifyTesterRead(false, MachineLockTexts.TesterReadLiveCharge);
                return MachineLockTexts.TesterReadLiveCharge;
            }

            ApplyLed(testerRedLedRenderer, ledOffMaterial);
            ApplyLed(testerGreenLedRenderer, ledGreenMaterial);
            TesterZero = true;
            if (scenario != null) scenario.NotifyTesterRead(true, MachineLockTexts.TesterReadZero);
            return MachineLockTexts.TesterReadZero;
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private void RefreshStatusLamps()
        {
            ApplyLed(mainSwitchOnLedRenderer, MainPowerOn ? ledRedMaterial : ledOffMaterial);
            ApplyLed(mainSwitchOffLedRenderer, MainPowerOn ? ledOffMaterial : ledGreenMaterial);
            ApplyLed(lockoutLedRenderer, LockedOut ? ledRedMaterial : ledOffMaterial);
            ApplyLed(lockoutReadyLedRenderer, (!MainPowerOn && !LockedOut) ? ledGreenMaterial : ledOffMaterial);
        }

        private void EmitHoverFeedback(string text, FeedbackKind kind)
        {
            float now = Time.unscaledTime;
            if (!float.IsNegativeInfinity(lastHoverTime) && now - lastHoverTime < HOVER_THROTTLE_SECONDS && LastHoverFeedback == text)
                return;
            lastHoverTime = now;
            LastHoverFeedback = text;
            if (HUDService.IsReady) HUDService.ShowActionFeedback(text, kind);
        }

        private void ShowLive(bool pulseArc)
        {
            ApplyLed(testerRedLedRenderer, ledRedMaterial);
            ApplyLed(testerGreenLedRenderer, ledOffMaterial);
            if (pulseArc) PulseArc();
        }

        private void PulseArc()
        {
            if (arcVfx == null) return;
            if (arcRoutine != null) StopCoroutine(arcRoutine);
            arcRoutine = StartCoroutine(PulseArcRoutine());
        }

        private IEnumerator PulseArcRoutine()
        {
            arcVfx.SetActive(true);
            if (arcParticles != null) arcParticles.Play(true);
            yield return ArcPulseWait;
            if (arcParticles != null) arcParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (arcVfx != null) arcVfx.SetActive(false);
            arcRoutine = null;
        }

        private void ApplySwitchHandle(bool on)
        {
            if (switchHandle == null) return;
            switchHandle.localRotation = on
                ? switchOnRotation
                : switchOnRotation * Quaternion.Euler(0f, 0f, SWITCH_OFF_ANGLE);
        }

        private void SetLockoutSelectable(bool selectable)
        {
            if (lockoutInteractable != null) lockoutInteractable.Selectable = selectable;
        }

        private static void ApplyLed(Renderer renderer, Material material)
        {
            if (renderer == null) return;
            renderer.sharedMaterial = material;
        }

        private void PlayClip(AudioClip clip)
        {
            if (oneShotAudio == null || clip == null) return;
            oneShotAudio.PlayOneShot(clip);
        }

        private void OnMainSwitchSelected(Hand hand) => OpenMainSwitch();
        private void OnLockoutSelected(Hand hand) => ApplyLockout();
        private void OnTesterSelected(Hand hand) => ReadTester();

        private void OnMainSwitchHovered(Hand hand)
        {
            EmitHoverFeedback(GetMainSwitchHoverText(), MainPowerOn ? FeedbackKind.Neutral : FeedbackKind.Positive);
        }

        private void OnLockoutHovered(Hand hand)
        {
            FeedbackKind kind = LockedOut ? FeedbackKind.Positive : (MainPowerOn ? FeedbackKind.Negative : FeedbackKind.Neutral);
            EmitHoverFeedback(GetLockoutHoverText(), kind);
        }

        private void OnTesterHovered(Hand hand)
        {
            EmitHoverFeedback(GetTesterHoverText(), TesterZero ? FeedbackKind.Positive : FeedbackKind.Neutral);
        }
    }
}
