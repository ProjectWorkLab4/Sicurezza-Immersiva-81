using System.Collections;
using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Electrical wall bay of the Machine Lock scenario (GDD §9.3 and §9.4, ADR-0002, Issue #43): three
    /// physically separated, unnumbered, color-coded Sub-Stations (Main Disconnect Switch, Lockout Station,
    /// Voltage Tester) whose horizontal slot positions shuffle across the 3! = 6 permutations per run.
    /// Power starts ON; opening the main switch cuts press power and unlocks the lockout station;
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

        /// <summary>Number of distinct left-to-right permutations of the three Sub-Stations (3! = 6).</summary>
        public const int SUB_STATION_PERMUTATION_COUNT = 6;

        /// <summary>Default minimum residual-energy discharge duration in seconds (Issue #45).</summary>
        public const float DEFAULT_DECAY_MIN_SECONDS = 15f;
        /// <summary>Default maximum residual-energy discharge duration in seconds (Issue #45).</summary>
        public const float DEFAULT_DECAY_MAX_SECONDS = 30f;

        private const float DEFAULT_ENERGIZED_PROBABILITY = 0.5f;
        private const float SWITCH_OFF_ANGLE = -80f;
        private const float ARC_PULSE_SECONDS = 0.35f;

        // Simulated industrial digital meter constants (Issue #45: clear visual feedback, no physics solver).
        private const float MAINS_NOMINAL_VOLTAGE = 230f;
        private const float RESIDUAL_PEAK_VOLTAGE = 222f;
        private const float RESIDUAL_VOLTAGE_EXPONENT = 1.35f;
        private const float RUNNING_LOAD_CURRENT = 12.40f;
        private const float STANDBY_LOAD_CURRENT = 0.85f;
        private const float NO_RESIDUAL_SETTLE_SECONDS = 0.5f;
        private const float DISPLAY_ACTIVE_REFRESH_SECONDS = 0.10f;
        private const float DISPLAY_STEADY_REFRESH_SECONDS = 0.25f;

        private static readonly WaitForSeconds ArcPulseWait = new WaitForSeconds(ARC_PULSE_SECONDS);
        private static readonly float[] DefaultSubStationSlotZ = { -0.88f, 0f, 0.88f };
        private static readonly int[][] SubStationPermutations =
        {
            new[] { 0, 1, 2 },
            new[] { 0, 2, 1 },
            new[] { 1, 0, 2 },
            new[] { 1, 2, 0 },
            new[] { 2, 0, 1 },
            new[] { 2, 1, 0 }
        };

        [Header("Scenario")]
        [Tooltip("Scenario that receives the panel events. Optional.")]
        [SerializeField] private MachineLockScenario scenario;
        [Tooltip("Tunable values. GDD defaults are used when empty.")]
        [SerializeField] private MachineLockConfig config;
        [Tooltip("Press that loses power when the main switch opens.")]
        [SerializeField] private MachineLockPress press;

        [Header("Sub-Station Layout (Randomized Slots)")]
        [Tooltip("Root transform of the Main Disconnect Switch Sub-Station (SUB_MainDisconnect).")]
        [SerializeField] private Transform mainSwitchStationRoot;
        [Tooltip("Root transform of the Lockout Station Sub-Station (SUB_LockoutStation).")]
        [SerializeField] private Transform lockoutStationRoot;
        [Tooltip("Root transform of the Voltage Tester Sub-Station (SUB_VoltageTester).")]
        [SerializeField] private Transform testerStationRoot;
        [Tooltip("Three symmetrical local Z slot offsets aligned with the bay rails and overhead conduit risers.")]
        [SerializeField] private float[] subStationSlotZ = { -0.88f, 0f, 0.88f };

        [Header("Sub-Station Controls")]
        [Tooltip("Main disconnect switch Interactable.")]
        [SerializeField] private Interactable mainSwitchInteractable;
        [Tooltip("Lockout station Interactable, selectable only after main power is off.")]
        [SerializeField] private Interactable lockoutInteractable;
        [Tooltip("Voltage tester Interactable.")]
        [SerializeField] private Interactable testerInteractable;
        [Tooltip("Diegetic 7-segment Digital Meter display mounted on the Voltage Tester Sub-Station.")]
        [SerializeField] private MachineLockDigitalMeterDisplay digitalMeterDisplay;
        [Tooltip("Rotary disconnect handle on the Main Disconnect Switch Sub-Station.")]
        [SerializeField] private Transform switchHandle;
        [Tooltip("Local rotation axis of the rotary disconnect handle (Z for legacy box, X for front-facing shaft).")]
        [SerializeField] private Vector3 switchRotationAxis = Vector3.forward;

        [Header("LEDs & Status Lamps")]
        [Tooltip("Optional ON (red) indicator on the Main Disconnect Switch Sub-Station.")]
        [SerializeField] private Renderer mainSwitchOnLedRenderer;
        [Tooltip("Optional OFF (green) indicator on the Main Disconnect Switch Sub-Station.")]
        [SerializeField] private Renderer mainSwitchOffLedRenderer;
        [Tooltip("Optional READY (green) indicator on the Lockout Station when unlocked after main off.")]
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

        private readonly MachineLockHoverFeedback hoverFeedback = new MachineLockHoverFeedback();
        private Quaternion switchOnRotation;
        private Coroutine arcRoutine;
        private ParticleSystem arcParticles;
        private float forcedDecaySecondsOverride = -1f;
        private float noResidualSettleRemaining;
        private float noResidualSettleStartVoltage;
        private float nextDisplayRefreshTime;

        /// <summary>true while the main circuit is closed (starts ON, GDD §9.3).</summary>
        public bool MainPowerOn { get; private set; } = true;
        /// <summary>true after the abstract lockout was applied.</summary>
        public bool LockedOut { get; private set; }
        /// <summary>true while residual energy remains in the oil.</summary>
        public bool OilEnergized => Charge01 > ENERGIZED_THRESHOLD;
        /// <summary>Normalized residual charge, 0 to 1.</summary>
        public float Charge01 { get; private set; }
        /// <summary>Active per-run residual energy discharge duration rolled in [15, 30] seconds (Issue #45).</summary>
        public float ActiveDecaySeconds { get; private set; }
        /// <summary>true after the tester confirmed ZERO.</summary>
        public bool TesterZero { get; private set; }
        /// <summary>true while the diegetic Digital Meter display is powered on.</summary>
        public bool TesterDisplayOn { get; private set; }
        /// <summary>Simulated voltage currently shown on the Digital Meter (0 while OFF).</summary>
        public float DisplayedVoltage { get; private set; }
        /// <summary>Simulated current in amperes currently shown on the Digital Meter (0 while OFF).</summary>
        public float DisplayedCurrent { get; private set; }
        /// <summary>Simulated power in watts currently shown on the Digital Meter (0 while OFF).</summary>
        public float DisplayedPower { get; private set; }
        /// <summary>Most recent hover Action Feedback string emitted by any of the three Sub-Stations.</summary>
        public string LastHoverFeedback => hoverFeedback.LastFeedback;
        /// <summary>Active Sub-Station slot permutation index, 0 to 5.</summary>
        public int ActiveSubStationOrderIndex { get; private set; }

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
            ActiveDecaySeconds = RollDecaySeconds();
            TesterDisplayOn = false;
            noResidualSettleRemaining = 0f;

            RefreshStatusLamps();
            ApplyLed(testerRedLedRenderer, ledOffMaterial);
            ApplyLed(testerGreenLedRenderer, ledOffMaterial);
            SetLockoutSelectable(false);
            SyncDisplayOff();
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
                mainSwitchInteractable.HoverExited += OnSubStationHoverExited;
            }
            if (lockoutInteractable != null)
            {
                lockoutInteractable.SelectEntered += OnLockoutSelected;
                lockoutInteractable.HoverEntered += OnLockoutHovered;
                lockoutInteractable.HoverExited += OnSubStationHoverExited;
            }
            if (testerInteractable != null)
            {
                testerInteractable.SelectEntered += OnTesterSelected;
                testerInteractable.HoverEntered += OnTesterHovered;
                testerInteractable.HoverExited += OnSubStationHoverExited;
            }
        }

        private void OnDisable()
        {
            if (mainSwitchInteractable != null)
            {
                mainSwitchInteractable.SelectEntered -= OnMainSwitchSelected;
                mainSwitchInteractable.HoverEntered -= OnMainSwitchHovered;
                mainSwitchInteractable.HoverExited -= OnSubStationHoverExited;
            }
            if (lockoutInteractable != null)
            {
                lockoutInteractable.SelectEntered -= OnLockoutSelected;
                lockoutInteractable.HoverEntered -= OnLockoutHovered;
                lockoutInteractable.HoverExited -= OnSubStationHoverExited;
            }
            if (testerInteractable != null)
            {
                testerInteractable.SelectEntered -= OnTesterSelected;
                testerInteractable.HoverEntered -= OnTesterHovered;
                testerInteractable.HoverExited -= OnSubStationHoverExited;
            }
            hoverFeedback.CancelHover();
            if (arcRoutine != null) StopCoroutine(arcRoutine);
            arcRoutine = null;
            if (arcParticles != null) arcParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (arcVfx != null) arcVfx.SetActive(false);
        }

        private void Update()
        {
            hoverFeedback.Tick();

            if (!MainPowerOn && Charge01 > 0f)
            {
                if (ActiveDecaySeconds <= 0f)
                {
                    Charge01 = 0f;
                }
                else
                {
                    Charge01 = Mathf.Max(0f, Charge01 - Time.deltaTime / ActiveDecaySeconds);
                }
            }

            if (noResidualSettleRemaining > 0f)
            {
                noResidualSettleRemaining = Mathf.Max(0f, noResidualSettleRemaining - Time.deltaTime);
            }

            if (!TesterDisplayOn) return;

            if (!MainPowerOn && !OilEnergized && !TesterZero && noResidualSettleRemaining <= 0f)
            {
                ConfirmZeroEnergyFromLiveDisplay();
                return;
            }

            if (Time.time >= nextDisplayRefreshTime)
            {
                UpdateLiveMeterDisplay(forceImmediate: false);
            }
        }

        // =====================================================================
        // Randomized Sub-Station slot permutation (Issue #43)
        // =====================================================================

        /// <summary>
        /// Places the three Sub-Stations (Main Disconnect, Lockout, Voltage Tester) into the three
        /// symmetrical bay slots according to <paramref name="permutationIndex"/> (0..5). Because every
        /// cabinet's plaque, LEDs, handle/hasp/multimeter, collider and arc VFX are parented under its
        /// Sub-Station root, each station moves atomically and aligns with the rails and overhead conduits.
        /// </summary>
        /// <param name="permutationIndex">Permutation index (normalized modulo 6).</param>
        public void ApplySubStationOrder(int permutationIndex)
        {
            int normalized = ((permutationIndex % SUB_STATION_PERMUTATION_COUNT) + SUB_STATION_PERMUTATION_COUNT)
                             % SUB_STATION_PERMUTATION_COUNT;
            ActiveSubStationOrderIndex = normalized;

            float[] slots = subStationSlotZ != null && subStationSlotZ.Length == 3
                ? subStationSlotZ
                : DefaultSubStationSlotZ;
            int[] permutation = SubStationPermutations[normalized];

            SetStationLocalZ(mainSwitchStationRoot, slots[permutation[0]]);
            SetStationLocalZ(lockoutStationRoot, slots[permutation[1]]);
            SetStationLocalZ(testerStationRoot, slots[permutation[2]]);
        }

        private static void SetStationLocalZ(Transform stationRoot, float localZ)
        {
            if (stationRoot == null) return;
            Vector3 pos = stationRoot.localPosition;
            pos.z = localZ;
            stationRoot.localPosition = pos;
        }

        // =====================================================================
        // Hover status text helpers
        // =====================================================================

        /// <summary>Current Italian hover Action Feedback for the Main Disconnect Switch Sub-Station.</summary>
        public string GetMainSwitchHoverText()
        {
            return MainPowerOn ? MachineLockTexts.HoverMainSwitchOn : MachineLockTexts.HoverMainSwitchOff;
        }

        /// <summary>Current Italian hover Action Feedback for the Lockout Station Sub-Station.</summary>
        public string GetLockoutHoverText()
        {
            if (LockedOut) return MachineLockTexts.HoverLockoutApplied;
            return MainPowerOn ? MachineLockTexts.HoverLockoutBlocked : MachineLockTexts.HoverLockoutReady;
        }

        /// <summary>Current Italian hover Action Feedback for the Voltage Tester Sub-Station.</summary>
        public string GetTesterHoverText()
        {
            if (TesterZero) return MachineLockTexts.HoverTesterZeroConfirmed;
            return MainPowerOn ? MachineLockTexts.HoverTesterLivePanel : MachineLockTexts.HoverTesterCheckResidual;
        }

        // =====================================================================
        // Explicit state (tests and debug menu)
        // =====================================================================

        /// <summary>
        /// Resets the panel to a known state: main power on, no lockout, meter display OFF, freshly
        /// rolled decay duration, and the charge passed in. Used by tests and the debug menu.
        /// </summary>
        public void InitializeState(bool energized)
        {
            MainPowerOn = true;
            LockedOut = false;
            Charge01 = energized ? 1f : 0f;
            ActiveDecaySeconds = RollDecaySeconds();
            TesterZero = false;
            TesterDisplayOn = false;
            noResidualSettleRemaining = 0f;

            ApplySwitchHandle(true);
            RefreshStatusLamps();
            ApplyLed(testerRedLedRenderer, ledOffMaterial);
            ApplyLed(testerGreenLedRenderer, ledOffMaterial);
            SetLockoutSelectable(false);
            if (arcVfx != null) arcVfx.SetActive(false);
            if (press != null) press.SetPower(true);
            SyncDisplayOff();
        }

        /// <summary>
        /// Overrides the rolled residual-energy decay duration for deterministic automated tests.
        /// Pass a negative value to restore normal random rolling from <see cref="MachineLockConfig"/>.
        /// </summary>
        /// <param name="seconds">Fixed decay duration in seconds, or negative to clear override.</param>
        public void SetDecayDurationForTest(float seconds)
        {
            forcedDecaySecondsOverride = seconds;
            ActiveDecaySeconds = RollDecaySeconds();
        }

        /// <summary>Forces the oil to full residual charge and invalidates a previous ZERO reading.</summary>
        public void ForceEnergized()
        {
            Charge01 = 1f;
            TesterZero = false;
            noResidualSettleRemaining = 0f;
            if (TesterDisplayOn)
            {
                ShowLive(false);
                UpdateLiveMeterDisplay(forceImmediate: true);
            }
        }

        /// <summary>Forces the oil to zero charge and invalidates a previous ZERO reading.</summary>
        public void ForceZeroEnergy()
        {
            Charge01 = 0f;
            TesterZero = false;
            noResidualSettleRemaining = 0f;
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
                noResidualSettleRemaining = 0f;
                ApplySwitchHandle(true);
                RefreshStatusLamps();
                SetLockoutSelectable(false);
                if (press != null) press.SetPower(true);
                if (TesterDisplayOn)
                {
                    ShowLive(false);
                    UpdateLiveMeterDisplay(forceImmediate: true);
                }
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

            if (TesterDisplayOn)
            {
                if (!OilEnergized && !TesterZero)
                {
                    noResidualSettleStartVoltage = Mathf.Max(DisplayedVoltage, MAINS_NOMINAL_VOLTAGE);
                    noResidualSettleRemaining = NO_RESIDUAL_SETTLE_SECONDS;
                }
                UpdateLiveMeterDisplay(forceImmediate: true);
            }
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
        /// Powers on (or keeps on) and reads the voltage tester without discharging the oil: LIVE with
        /// the main circuit closed or residual charge left, ZERO only below <see cref="ENERGIZED_THRESHOLD"/>.
        /// </summary>
        public string ReadTester()
        {
            TesterDisplayOn = true;
            noResidualSettleRemaining = 0f;
            PlayClip(testerBeepClip);

            if (MainPowerOn)
            {
                ShowLive(true);
                UpdateLiveMeterDisplay(forceImmediate: true);
                if (scenario != null) scenario.NotifyTesterRead(false, MachineLockTexts.TesterReadLivePanel);
                return MachineLockTexts.TesterReadLivePanel;
            }

            if (Charge01 > ENERGIZED_THRESHOLD)
            {
                ShowLive(false);
                UpdateLiveMeterDisplay(forceImmediate: true);
                if (scenario != null) scenario.NotifyTesterRead(false, MachineLockTexts.TesterReadLiveCharge);
                return MachineLockTexts.TesterReadLiveCharge;
            }

            ApplyLed(testerRedLedRenderer, ledOffMaterial);
            ApplyLed(testerGreenLedRenderer, ledGreenMaterial);
            TesterZero = true;
            UpdateLiveMeterDisplay(forceImmediate: true);
            if (scenario != null) scenario.NotifyTesterRead(true, MachineLockTexts.TesterReadZero);
            return MachineLockTexts.TesterReadZero;
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private float RollDecaySeconds()
        {
            if (forcedDecaySecondsOverride >= 0f) return forcedDecaySecondsOverride;
            if (config != null) return config.RollEnergyDecaySeconds();
            return UnityEngine.Random.Range(DEFAULT_DECAY_MIN_SECONDS, DEFAULT_DECAY_MAX_SECONDS);
        }

        private void ConfirmZeroEnergyFromLiveDisplay()
        {
            ApplyLed(testerRedLedRenderer, ledOffMaterial);
            ApplyLed(testerGreenLedRenderer, ledGreenMaterial);
            TesterZero = true;
            PlayClip(testerBeepClip);
            UpdateLiveMeterDisplay(forceImmediate: true);
            if (scenario != null) scenario.NotifyTesterRead(true, MachineLockTexts.TesterReadZero);
        }

        private void SyncDisplayOff()
        {
            DisplayedVoltage = 0f;
            DisplayedCurrent = 0f;
            DisplayedPower = 0f;
            if (digitalMeterDisplay != null) digitalMeterDisplay.SetPoweredOff();
        }

        private void UpdateLiveMeterDisplay(bool forceImmediate)
        {
            float t = Time.time;
            float jitter = Mathf.Sin(t * 7.3f) * 0.6f + Mathf.Cos(t * 13.1f) * 0.4f;

            float voltage;
            float current;
            float power;
            bool fastRefresh = false;

            if (MainPowerOn)
            {
                bool pressRunning = press == null || press.IsRunning;
                voltage = MAINS_NOMINAL_VOLTAGE + jitter * (pressRunning ? 1.2f : 1.0f);
                current = pressRunning
                    ? RUNNING_LOAD_CURRENT + jitter * 0.14f
                    : STANDBY_LOAD_CURRENT + jitter * 0.02f;
                power = voltage * current;
            }
            else if (noResidualSettleRemaining > 0f)
            {
                float ratio = Mathf.Clamp01(noResidualSettleRemaining / NO_RESIDUAL_SETTLE_SECONDS);
                voltage = noResidualSettleStartVoltage * (ratio * ratio);
                current = 0f;
                power = 0f;
                fastRefresh = true;
            }
            else if (Charge01 > ENERGIZED_THRESHOLD)
            {
                // Curved capacitor-bleed voltage profile snapping cleanly to 0 V once <= ENERGIZED_THRESHOLD.
                float baseVoltage = RESIDUAL_PEAK_VOLTAGE * Mathf.Pow(Mathf.Clamp01(Charge01), RESIDUAL_VOLTAGE_EXPONENT);
                float flicker = baseVoltage > 10f ? jitter * 0.8f : 0f;
                voltage = Mathf.Max(1f, baseVoltage + flicker);
                current = 0f;
                power = 0f;
                fastRefresh = true;
            }
            else
            {
                voltage = 0f;
                current = 0f;
                power = 0f;
            }

            DisplayedVoltage = voltage;
            DisplayedCurrent = current;
            DisplayedPower = power;

            if (digitalMeterDisplay != null)
                digitalMeterDisplay.SetPoweredOn(voltage, current, power);

            float interval = fastRefresh ? DISPLAY_ACTIVE_REFRESH_SECONDS : DISPLAY_STEADY_REFRESH_SECONDS;
            nextDisplayRefreshTime = forceImmediate ? t + interval : t + interval;
        }

        private void RefreshStatusLamps()
        {
            ApplyLed(mainSwitchOnLedRenderer, MainPowerOn ? ledRedMaterial : ledOffMaterial);
            ApplyLed(mainSwitchOffLedRenderer, MainPowerOn ? ledOffMaterial : ledGreenMaterial);
            ApplyLed(lockoutLedRenderer, LockedOut ? ledRedMaterial : ledOffMaterial);
            ApplyLed(lockoutReadyLedRenderer, (!MainPowerOn && !LockedOut) ? ledGreenMaterial : ledOffMaterial);
        }

        private void BeginSubStationHover(string text, FeedbackKind kind)
        {
            hoverFeedback.BeginHover(text, kind);
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
            Vector3 axis = switchRotationAxis.sqrMagnitude > 0.0001f ? switchRotationAxis.normalized : Vector3.forward;
            switchHandle.localRotation = on
                ? switchOnRotation
                : switchOnRotation * Quaternion.AngleAxis(SWITCH_OFF_ANGLE, axis);
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

        private void OnMainSwitchSelected(Hand hand)
        {
            hoverFeedback.CancelHover();
            OpenMainSwitch();
        }

        private void OnLockoutSelected(Hand hand)
        {
            hoverFeedback.CancelHover();
            ApplyLockout();
        }

        private void OnTesterSelected(Hand hand)
        {
            hoverFeedback.CancelHover();
            ReadTester();
        }

        private void OnMainSwitchHovered(Hand hand)
        {
            BeginSubStationHover(GetMainSwitchHoverText(), MainPowerOn ? FeedbackKind.Neutral : FeedbackKind.Positive);
        }

        private void OnLockoutHovered(Hand hand)
        {
            FeedbackKind kind = LockedOut ? FeedbackKind.Positive : (MainPowerOn ? FeedbackKind.Negative : FeedbackKind.Neutral);
            BeginSubStationHover(GetLockoutHoverText(), kind);
        }

        private void OnTesterHovered(Hand hand)
        {
            BeginSubStationHover(GetTesterHoverText(), TesterZero ? FeedbackKind.Positive : FeedbackKind.Neutral);
        }

        private void OnSubStationHoverExited(Hand hand)
        {
            hoverFeedback.CancelHover();
        }
    }
}
