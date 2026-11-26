using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Department alarm of the Machine Lock scenario (GDD §9.2): a wall button that raises a rotating,
    /// flashing red beacon and a looping siren, and subtitles the sound event.
    /// It never stops a hazard. The button is a plain <see cref="Interactable"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Alarm")]
    public sealed class MachineLockAlarm : MonoBehaviour
    {
        private const float ROTOR_DEGREES_PER_SECOND = 210f;
        private const float FLASH_SPEED = 7.5f;
        private const float FALLBACK_LIGHT_INTENSITY = 2f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color DefaultDomeEmission = new Color(1f, 0.08f, 0.04f);

        [Header("Scenario")]
        [Tooltip("Scenario that receives the alarm event. Optional.")]
        [SerializeField] private MachineLockScenario scenario;

        [Header("Control")]
        [Tooltip("Wall alarm button.")]
        [SerializeField] private Interactable alarmInteractable;

        [Header("Beacon")]
        [Tooltip("Point light of the rotating beacon.")]
        [SerializeField] private Light beaconLight;
        [Tooltip("Rotating part of the beacon.")]
        [SerializeField] private Transform beaconRotor;
        [Tooltip("Dome of the beacon, flashed through its emission.")]
        [SerializeField] private Renderer beaconDomeRenderer;

        [Header("Audio")]
        [Tooltip("Looping siren source.")]
        [SerializeField] private AudioSource sirenAudio;
        [Tooltip("One-shot source for the button click.")]
        [SerializeField] private AudioSource buttonAudio;
        [Tooltip("Click of the alarm button.")]
        [SerializeField] private AudioClip buttonClip;

        private bool raised;
        private float beaconBaseIntensity = FALLBACK_LIGHT_INTENSITY;
        private Color domeEmission = DefaultDomeEmission;
        private MaterialPropertyBlock propertyBlock;
        private float lastHoverTime = float.NegativeInfinity;

        /// <summary>true after the alarm was raised once.</summary>
        public bool Raised => raised;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();

            if (beaconLight != null)
            {
                beaconBaseIntensity = beaconLight.intensity > 0f ? beaconLight.intensity : FALLBACK_LIGHT_INTENSITY;
                beaconLight.enabled = false;
            }

            if (beaconDomeRenderer != null && beaconDomeRenderer.sharedMaterial != null &&
                beaconDomeRenderer.sharedMaterial.HasProperty(EmissionColorId))
            {
                domeEmission = beaconDomeRenderer.sharedMaterial.GetColor(EmissionColorId);
            }

            if (sirenAudio != null)
            {
                sirenAudio.loop = true;
                sirenAudio.playOnAwake = false;
            }
            if (buttonAudio != null) buttonAudio.playOnAwake = false;
        }

        private void OnEnable()
        {
            if (alarmInteractable != null)
            {
                alarmInteractable.SelectEntered += OnAlarmSelected;
                alarmInteractable.HoverEntered += OnAlarmHovered;
            }
        }

        private void OnDisable()
        {
            if (alarmInteractable != null)
            {
                alarmInteractable.SelectEntered -= OnAlarmSelected;
                alarmInteractable.HoverEntered -= OnAlarmHovered;
            }
            if (sirenAudio != null) sirenAudio.Stop();
        }

        private void Update()
        {
            if (!raised) return;

            float deltaTime = Time.deltaTime;
            if (beaconRotor != null) beaconRotor.Rotate(0f, ROTOR_DEGREES_PER_SECOND * deltaTime, 0f, Space.Self);

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * FLASH_SPEED);
            if (beaconLight != null)
            {
                beaconLight.intensity = Mathf.Lerp(beaconBaseIntensity * 0.25f, beaconBaseIntensity * 1.6f, pulse);
            }

            if (beaconDomeRenderer != null && propertyBlock != null)
            {
                beaconDomeRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(EmissionColorId, domeEmission * (0.35f + 1.5f * pulse));
                beaconDomeRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        /// <summary>
        /// Raises the alarm: beacon on, siren playing, subtitled sound event and scenario notification.
        /// Idempotent, and it does not stop any hazard.
        /// </summary>
        public void Raise()
        {
            if (raised) return;
            raised = true;

            if (beaconLight != null)
            {
                beaconLight.enabled = true;
                beaconLight.intensity = beaconBaseIntensity;
            }

            if (buttonAudio != null && buttonClip != null) buttonAudio.PlayOneShot(buttonClip);
            if (sirenAudio != null) sirenAudio.Play();
            if (HUDService.IsReady) HUDService.ShowSoundEvent(MachineLockTexts.SoundAlarmSiren, true);
            if (scenario != null) scenario.NotifyAlarmPressed();
        }

        private void OnAlarmSelected(Hand hand)
        {
            Raise();
        }

        private void OnAlarmHovered(Hand hand)
        {
            float now = Time.unscaledTime;
            if (!float.IsNegativeInfinity(lastHoverTime) && now - lastHoverTime < 0.3f) return;
            lastHoverTime = now;
            if (HUDService.IsReady)
                HUDService.ShowActionFeedback(raised ? MachineLockTexts.Alarm : MachineLockTexts.HoverAlarm,
                    raised ? FeedbackKind.Positive : FeedbackKind.Neutral);
        }
    }
}
