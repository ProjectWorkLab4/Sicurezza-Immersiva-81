using System.Collections;
using SI81.Rig;
using SI81.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SI81.MachineLock
{
    /// <summary>
    /// Damage feedback of the Machine Lock scenario (GDD §9.13): a red URP Vignette pulse that fades in,
    /// holds and fades out, a one-shot clip, a haptic pulse on the dominant hand and, when the damage
    /// volume cannot render, a red light flash plus the "Danno!" HUD feedback. The component owns the
    /// damage <see cref="Volume"/> (its <c>weight</c> is animated here) and the fallback light; it never
    /// creates a camera, an audio listener or a canvas, it does not attach anything to the head.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Damage Feedback")]
    public sealed class MachineLockDamageFeedback : MonoBehaviour
    {
        /// <summary>Intensity of <see cref="PlayMinor"/> relative to the critical pulse.</summary>
        private const float MINOR_INTENSITY_SCALE = 0.35f;

        /// <summary>Colour the optional Color Adjustments override is pulled to during a critical pulse.</summary>
        private static readonly Color DamageTint = new Color(1f, 0.72f, 0.68f, 1f);

        [Header("Damage volume (URP)")]
        [Tooltip("Volume that holds the red Vignette override (and optionally a mild Color Adjustments override). " +
                 "This component animates its weight; keep the weight at 0 when no damage is shown.")]
        [SerializeField] private Volume damageVolume;

        [Tooltip("Red point light used when the damage volume cannot render. Keep it non-shadow-casting, " +
                 "with intensity 0; this component flashes it near the player's head.")]
        [SerializeField] private Light fallbackFlashLight;

        [Header("Audio")]
        [Tooltip("AudioSource used for the one-shot critical clips. 2D, Play On Awake off.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Clip played for CriticalEvent.LiveOilShock.")]
        [SerializeField] private AudioClip shockClip;
        [Tooltip("Clip played for every critical event that is not a shock or a steam event (hot oil, press contact).")]
        [SerializeField] private AudioClip burnClip;
        [Tooltip("Clip played for CriticalEvent.WaterExtinguisher.")]
        [SerializeField] private AudioClip steamClip;

        [Header("Envelope (seconds)")]
        [Tooltip("Fade-in of a critical pulse.")]
        [SerializeField] private float fadeInSeconds = 0.15f;
        [Tooltip("Hold of a critical pulse.")]
        [SerializeField] private float holdSeconds = 0.5f;
        [Tooltip("Fade-out of a critical pulse.")]
        [SerializeField] private float fadeOutSeconds = 2.0f;

        [Header("Intensity")]
        [Tooltip("Peak weight of the damage volume during a critical pulse.")]
        [SerializeField] private float maxVignetteIntensity = 0.55f;
        [Tooltip("Peak intensity of the fallback light flash (a critical pulse only lasts a few tenths of a second).")]
        [SerializeField] private float fallbackLightIntensity = 3.0f;

        [Header("Testing")]
        [Tooltip("Force the light and HUD fallback even when the damage volume is ready. Editor and QA use only.")]
        [SerializeField] private bool forceFallbackForTesting;

        // Active pulse coroutine (null when nothing is playing).
        private Coroutine pulse;
        // Optional Color Adjustments override of the damage profile.
        private ColorAdjustments colorAdjustments;
        // Authored colour filter of the Color Adjustments override, restored when the pulse ends.
        private Color authoredFilter = Color.white;
        // true when authoredFilter was captured from the profile.
        private bool tintCaptured;

        /// <summary>
        /// Clears the visual state and enforces the fallback light defaults (no shadows, intensity 0).
        /// </summary>
        private void Awake()
        {
            if (fallbackFlashLight != null)
            {
                fallbackFlashLight.shadows = LightShadows.None;
                fallbackFlashLight.intensity = 0f;
            }
            ResetVisuals();
        }

        /// <summary>
        /// Stops the pulse when the component or its GameObject is disabled.
        /// </summary>
        private void OnDisable()
        {
            Clear();
        }

        /// <summary>
        /// Stops the pulse when the component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Clear();
        }

        // =====================================================================
        // Public API (GDD §14.3)
        // =====================================================================

        /// <summary>
        /// Critical damage: plays the matching one-shot clip, pulses the dominant hand, then fades the
        /// damage vignette in, holds it and fades it out. When the volume is missing, has no profile, has
        /// no Vignette override, is disabled or <see cref="forceFallbackForTesting"/> is set, the fallback
        /// runs instead: red light flash near the head plus the "Danno!" action feedback.
        /// </summary>
        /// <param name="evt">Critical event that decides which clip plays.</param>
        public void PlayCritical(CriticalEvent evt = CriticalEvent.HotOilBurn)
        {
            PlayClip(SelectClip(evt));
            if (HUDService.IsReady) HUDService.Pulse(0.85f, 0.35f, false);

            bool reducedFlashes = ReduceFlashes;
            if (ResolveVolumePath())
            {
                // A smooth fade is not a flash: the peak is only lowered when the player asked for fewer flashes.
                float peak = reducedFlashes ? maxVignetteIntensity * 0.75f : maxVignetteIntensity;
                StartPulse(fadeInSeconds, holdSeconds, fadeOutSeconds, Mathf.Clamp01(peak), false);
                return;
            }

            // Fallback: text and haptic always carry the event, the light flash is the only bright part.
            if (HUDService.IsReady) HUDService.ShowActionFeedback("Danno!", FeedbackKind.Negative);
            if (reducedFlashes) return;
            StartPulse(0.05f, 0f, 0.3f, fallbackLightIntensity, true);
        }

        /// <summary>
        /// Minor damage: a short mild vignette pulse (0.1 s in, 0.2 s hold, 0.6 s out) and a light haptic
        /// pulse. No audio and no HUD feedback, so repeated contacts do not spam the wrist panel.
        /// </summary>
        public void PlayMinor()
        {
            if (HUDService.IsReady) HUDService.Pulse(0.4f, 0.15f);

            bool reducedFlashes = ReduceFlashes;
            if (ResolveVolumePath())
            {
                float peak = maxVignetteIntensity * MINOR_INTENSITY_SCALE;
                if (reducedFlashes) peak *= 0.75f;
                StartPulse(0.1f, 0.2f, 0.6f, Mathf.Clamp01(peak), false);
                return;
            }
            if (reducedFlashes) return;
            StartPulse(0.04f, 0f, 0.2f, fallbackLightIntensity * MINOR_INTENSITY_SCALE, true);
        }

        /// <summary>
        /// Stops the active pulse and resets every visual: the damage volume weight, the fallback light
        /// intensity and the authored colour filter of the profile.
        /// </summary>
        public void Clear()
        {
            if (pulse != null)
            {
                StopCoroutine(pulse);
                pulse = null;
            }
            ResetVisuals();
        }

        // =====================================================================
        // Pulse
        // =====================================================================

        /// <summary>
        /// Starts a fade-in, hold and fade-out pulse, restarting any pulse already running.
        /// </summary>
        /// <param name="fadeIn">Fade-in duration in seconds.</param>
        /// <param name="hold">Hold duration in seconds.</param>
        /// <param name="fadeOut">Fade-out duration in seconds.</param>
        /// <param name="peak">Peak amount (volume weight, or light intensity when <paramref name="lightPath"/>).</param>
        /// <param name="lightPath">true to drive the fallback light instead of the damage volume.</param>
        private void StartPulse(float fadeIn, float hold, float fadeOut, float peak, bool lightPath)
        {
            if (pulse != null)
            {
                StopCoroutine(pulse);
                pulse = null;
            }
            // Clear both visual paths first, so switching between volume and light never leaves a stale value.
            ResetVisuals();
            if (!isActiveAndEnabled)
            {
                return;
            }
            if (lightPath) PlaceFallbackLight();
            pulse = StartCoroutine(PulseRoutine(fadeIn, hold, fadeOut, peak, lightPath));
        }

        /// <summary>
        /// Frame-rate independent envelope: fade-in, hold, fade-out, then back to zero.
        /// </summary>
        private IEnumerator PulseRoutine(float fadeIn, float hold, float fadeOut, float peak, bool lightPath)
        {
            float fadeInTime = Mathf.Max(0f, fadeIn);
            float holdTime = Mathf.Max(0f, hold);
            float fadeOutTime = Mathf.Max(0f, fadeOut);
            float elapsed = 0f;

            while (elapsed < fadeInTime)
            {
                elapsed += Time.deltaTime;
                SetEnvelope(lightPath, Mathf.Clamp01(elapsed / fadeInTime) * peak, peak);
                yield return null;
            }

            SetEnvelope(lightPath, peak, peak);
            if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

            elapsed = 0f;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                SetEnvelope(lightPath, (1f - Mathf.Clamp01(elapsed / fadeOutTime)) * peak, peak);
                yield return null;
            }

            SetEnvelope(lightPath, 0f, peak);
            pulse = null;
        }

        /// <summary>
        /// Applies one envelope step to the damage volume (with the optional colour tint) or to the light.
        /// </summary>
        /// <param name="lightPath">true to drive the fallback light.</param>
        /// <param name="amount">Current envelope amount.</param>
        /// <param name="peak">Peak amount of the running pulse.</param>
        private void SetEnvelope(bool lightPath, float amount, float peak)
        {
            if (lightPath)
            {
                if (fallbackFlashLight != null)
                {
                    fallbackFlashLight.intensity = Mathf.Max(0f, amount);
                    // Keep the light out of the realtime budget again as soon as the pulse is over.
                    fallbackFlashLight.enabled = amount > 0f;
                }
                return;
            }

            if (damageVolume != null) damageVolume.weight = Mathf.Clamp01(amount);
            if (colorAdjustments == null || !tintCaptured) return;
            float blend = peak > 0f ? Mathf.Clamp01(amount / peak) : 0f;
            colorAdjustments.colorFilter.value = Color.Lerp(authoredFilter, DamageTint, blend);
        }

        /// <summary>
        /// Puts every visual back to its idle state and restores the authored colour filter.
        /// </summary>
        private void ResetVisuals()
        {
            if (damageVolume != null) damageVolume.weight = 0f;
            if (fallbackFlashLight != null)
            {
                fallbackFlashLight.intensity = 0f;
                // An idle fallback light must not count against the two-realtime-light budget.
                fallbackFlashLight.enabled = false;
            }
            if (tintCaptured && colorAdjustments != null) colorAdjustments.colorFilter.value = authoredFilter;
        }

        // =====================================================================
        // Resolution helpers
        // =====================================================================

        /// <summary>
        /// Resolves the damage profile overrides and says whether the volume path can run. The profile
        /// must exist and contain a Vignette; the Color Adjustments override is optional and its authored
        /// colour filter is captured so the pulse can restore it.
        /// </summary>
        /// <returns>true when the damage volume can show the pulse.</returns>
        private bool ResolveVolumePath()
        {
            if (forceFallbackForTesting || damageVolume == null) return false;
            if (!damageVolume.enabled || !damageVolume.isActiveAndEnabled) return false;

            VolumeProfile profile = damageVolume.profile;
            if (profile == null) return false;

            Vignette foundVignette;
            if (!profile.TryGet(out foundVignette) || foundVignette == null) return false;

            ColorAdjustments foundAdjustments;
            if (profile.TryGet(out foundAdjustments) && foundAdjustments != null)
            {
                if (colorAdjustments != foundAdjustments)
                {
                    colorAdjustments = foundAdjustments;
                    authoredFilter = foundAdjustments.colorFilter.value;
                }
                tintCaptured = true;
            }
            else
            {
                colorAdjustments = null;
                tintCaptured = false;
            }
            return true;
        }

        /// <summary>
        /// Moves the fallback light next to the player's head (the shared rig, or the main camera when a
        /// scene has no rig). Never creates a camera: it only reads the one already in the scene.
        /// </summary>
        private void PlaceFallbackLight()
        {
            if (fallbackFlashLight == null) return;

            fallbackFlashLight.enabled = true;
            Transform head = RigRegistration.Instance != null ? RigRegistration.Instance.Head : null;
            if (head != null)
            {
                fallbackFlashLight.transform.position = head.position;
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null) fallbackFlashLight.transform.position = mainCamera.transform.position;
        }

        /// <summary>
        /// Picks the clip of a critical event: shock, steam, or burn for everything else.
        /// </summary>
        /// <param name="evt">Critical event.</param>
        /// <returns>Clip to play, or null when none is assigned.</returns>
        private AudioClip SelectClip(CriticalEvent evt)
        {
            switch (evt)
            {
                case CriticalEvent.LiveOilShock:
                    return shockClip;
                case CriticalEvent.WaterExtinguisher:
                    return steamClip;
                default:
                    return burnClip;
            }
        }

        /// <summary>
        /// Plays one clip on the shared one-shot source, if both the clip and the source exist.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        private void PlayClip(AudioClip clip)
        {
            if (clip == null || oneShotAudio == null) return;
            oneShotAudio.PlayOneShot(clip);
        }

        /// <summary>
        /// true when the accessibility setting "reduce flashes" is on. Without a settings store the
        /// feedback stays at its default (flashes allowed).
        /// </summary>
        private static bool ReduceFlashes
        {
            get
            {
                IUISettingsStore settings = UIServices.Settings;
                return settings != null && settings.GetBool(SettingsCatalog.ACCESSIBILITY_REDUCE_FLASHES);
            }
        }
    }
}
