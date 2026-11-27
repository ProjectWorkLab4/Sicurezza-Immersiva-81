using System;
using System.Collections;
using SI81.Rig;
using SI81.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SI81.MachineLock
{
    /// <summary>
    /// Damage feedback and exit fade-to-black backdrop of the Machine Lock scenario (GDD §9.13, ADR-0003,
    /// ADR-0006). Drives either the URP damage Vignette or the scenario-owned head-aligned peripheral red
    /// overlay + dual-hand haptics + Guided respawn blink, and also provides a smooth full-screen fade to
    /// pure black on exit confirmation that drops its render queue strictly behind World Space UI
    /// (<see cref="EXIT_BACKDROP_RENDER_QUEUE"/> = 2950 &lt; 3000) once fully black so end-of-level UI
    /// (<c>HUD_OutcomePanel</c>, <c>PANEL_Debrief</c>, <c>UIPointer</c>) renders crisply over pure black
    /// with zero 3D world compenetration.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Damage Feedback")]
    public sealed class MachineLockDamageFeedback : MonoBehaviour
    {
        /// <summary>Render queue used while holding pure black behind World Space UI (UI = 3000, reticle = 3100).</summary>
        public const int EXIT_BACKDROP_RENDER_QUEUE = (int)RenderQueue.Transparent - 50;

        private const float MINOR_INTENSITY_SCALE = 0.35f;
        private const float OVERLAY_DISTANCE = 0.36f;
        private const float OVERLAY_SCALE = 2.40f;
        private const float DEFAULT_RESPAWN_BLINK_HALF_SECONDS = 0.15f;

        private static readonly Color DamageTint = new Color(1f, 0.72f, 0.68f, 1f);
        private static readonly Color OverlayRedColor = new Color(0.88f, 0.04f, 0.04f, 1f);
        private static readonly Color OverlayDarkColor = new Color(0.02f, 0.02f, 0.03f, 1f);
        private static readonly Color OverlayPureBlackColor = new Color(0f, 0f, 0f, 1f);

        private static readonly int ColorPropId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");
        private static readonly int IntensityPropId = Shader.PropertyToID("_Intensity");
        private static readonly int FullScreenFadePropId = Shader.PropertyToID("_FullScreenFade");

        [Header("Damage volume (URP)")]
        [Tooltip("Volume holding the red Vignette override. Driven when active camera post-processing is enabled.")]
        [SerializeField] private Volume damageVolume;

        [Tooltip("Red point light pulsed near the player's head in the scenario-owned visual path. Non-shadow-casting, intensity 0 at rest.")]
        [SerializeField] private Light fallbackFlashLight;

        [Header("Scenario-owned overlay (ADR-0003)")]
        [Tooltip("Optional pre-authored overlay material using SI81/MachineLock/DamageOverlay. Instantiated at runtime so shared assets are never mutated.")]
        [SerializeField] private Material overlayMaterialAsset;

        [Header("Audio")]
        [Tooltip("AudioSource used for the one-shot critical clips. 2D, Play On Awake off.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Clip played for CriticalEvent.LiveOilShock.")]
        [SerializeField] private AudioClip shockClip;
        [Tooltip("Clip played for hot oil, wrong PPE, press contact, and unisolated try-start.")]
        [SerializeField] private AudioClip burnClip;
        [Tooltip("Clip played for CriticalEvent.WaterExtinguisher.")]
        [SerializeField] private AudioClip steamClip;

        [Header("Envelope (seconds)")]
        [Tooltip("Fade-in of a critical pulse.")]
        [SerializeField] private float fadeInSeconds = 0.15f;
        [Tooltip("Hold of a critical pulse.")]
        [SerializeField] private float holdSeconds = 0.5f;
        [Tooltip("Fade-out of a critical pulse.")]
        [SerializeField] private float fadeOutSeconds = 1.5f;

        [Header("Intensity")]
        [Tooltip("Peak weight of the URP damage volume during a critical pulse.")]
        [SerializeField] private float maxVignetteIntensity = 0.55f;
        [Tooltip("Peak alpha/intensity of the scenario-owned peripheral red vignette overlay.")]
        [SerializeField] private float maxOverlayIntensity = 0.85f;
        [Tooltip("Peak intensity of the fallback red point-light pulse.")]
        [SerializeField] private float fallbackLightIntensity = 3.0f;

        [Header("Testing")]
        [Tooltip("Force the scenario-owned overlay + light path even when camera post-processing is enabled.")]
        [SerializeField] private bool forceFallbackForTesting;

        private Coroutine pulse;
        private Coroutine respawnBlinkRoutine;
        private Coroutine exitFadeRoutine;
        private ColorAdjustments colorAdjustments;
        private Color authoredFilter = Color.white;
        private bool tintCaptured;

        private GameObject overlayObject;
        private MeshRenderer overlayRenderer;
        private Material runtimeOverlayMaterial;
        private MaterialPropertyBlock overlayBlock;
        private float currentOverlayIntensity;
        private float currentBlinkIntensity;
        private float currentExitFadeIntensity;
        private bool usesCustomOverlayShader;

        /// <summary>true while the scenario-owned head-aligned overlay quad is actively rendering.</summary>
        public bool IsScenarioOverlayActive => overlayRenderer != null && overlayRenderer.enabled;

        /// <summary>Current normalized 0..1 intensity of the scenario-owned overlay (vignette, blink, or exit fade).</summary>
        public float CurrentOverlayIntensity => Mathf.Max(currentOverlayIntensity, Mathf.Max(currentBlinkIntensity, currentExitFadeIntensity));

        /// <summary>true when the last triggered feedback used the scenario-owned path instead of the URP Volume.</summary>
        public bool LastUsedScenarioOverlayPath { get; private set; }

        /// <summary>true while the brief dark respawn blink coroutine is running.</summary>
        public bool IsRespawnBlinkActive => respawnBlinkRoutine != null;

        /// <summary>true while the exit fade-to-black or its persistent pure-black UI backdrop is active.</summary>
        public bool IsExitFadeActive => exitFadeRoutine != null || currentExitFadeIntensity > 0.001f;

        /// <summary>Current 0..1 alpha of the exit fade-to-black backdrop.</summary>
        public float ExitFadeAlpha => currentExitFadeIntensity;

        /// <summary>Active render queue of the runtime overlay material, or -1 if not yet created.</summary>
        public int ActiveOverlayRenderQueue => runtimeOverlayMaterial != null ? runtimeOverlayMaterial.renderQueue : -1;

        private void Awake()
        {
            if (fallbackFlashLight != null)
            {
                fallbackFlashLight.shadows = LightShadows.None;
                fallbackFlashLight.intensity = 0f;
            }
            overlayBlock = new MaterialPropertyBlock();
            ResetVisuals();
        }

        private void LateUpdate()
        {
            if (overlayRenderer == null || !overlayRenderer.enabled) return;
            AlignOverlayToHead();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            Clear();
            if (runtimeOverlayMaterial != null)
            {
                Destroy(runtimeOverlayMaterial);
                runtimeOverlayMaterial = null;
            }
            if (overlayObject != null)
            {
                Destroy(overlayObject);
                overlayObject = null;
            }
        }

        // =====================================================================
        // Public API (GDD §14.3, ADR-0003, ADR-0006)
        // =====================================================================

        /// <summary>
        /// Critical damage: plays the matching one-shot clip, fires strong dual-hand haptics, and drives
        /// either the URP damage volume (when active camera post-processing is on) or the scenario-owned
        /// head-aligned peripheral red vignette overlay + red point-light pulse + Action Feedback.
        /// </summary>
        public void PlayCritical(CriticalEvent evt = CriticalEvent.HotOilBurn)
        {
            if (IsExitFadeActive) return;

            PlayClip(SelectClip(evt));
            if (HUDService.IsReady)
            {
                // Dual-hand strong haptic warning on both dominant and non-dominant controllers.
                HUDService.Pulse(0.90f, 0.40f, false);
                HUDService.Pulse(0.85f, 0.35f, true);
            }

            bool reducedFlashes = ReduceFlashes;
            if (ResolveVolumePath())
            {
                LastUsedScenarioOverlayPath = false;
                float peak = reducedFlashes ? maxVignetteIntensity * 0.65f : maxVignetteIntensity;
                StartPulse(fadeInSeconds, holdSeconds, fadeOutSeconds, Mathf.Clamp01(peak), false, reducedFlashes);
                return;
            }

            LastUsedScenarioOverlayPath = true;
            if (HUDService.IsReady) HUDService.ShowActionFeedback("Danno!", FeedbackKind.Negative);

            float overlayPeak = reducedFlashes ? maxOverlayIntensity * 0.48f : maxOverlayIntensity;
            float fadeIn = reducedFlashes ? Mathf.Max(fadeInSeconds, 0.28f) : fadeInSeconds;
            StartPulse(fadeIn, holdSeconds, fadeOutSeconds, Mathf.Clamp01(overlayPeak), true, reducedFlashes);
        }

        /// <summary>
        /// Minor damage: short mild vignette pulse and light haptic pulse without audio or HUD spam.
        /// </summary>
        public void PlayMinor()
        {
            if (IsExitFadeActive) return;
            if (HUDService.IsReady) HUDService.Pulse(0.4f, 0.15f);

            bool reducedFlashes = ReduceFlashes;
            if (ResolveVolumePath())
            {
                LastUsedScenarioOverlayPath = false;
                float peak = maxVignetteIntensity * MINOR_INTENSITY_SCALE;
                if (reducedFlashes) peak *= 0.65f;
                StartPulse(0.1f, 0.2f, 0.6f, Mathf.Clamp01(peak), false, reducedFlashes);
                return;
            }

            LastUsedScenarioOverlayPath = true;
            float overlayPeak = maxOverlayIntensity * MINOR_INTENSITY_SCALE * (reducedFlashes ? 0.5f : 1f);
            StartPulse(0.1f, 0.15f, 0.5f, Mathf.Clamp01(overlayPeak), true, reducedFlashes);
        }

        /// <summary>
        /// Plays a brief smooth dark fade-out / fade-in blink (default 0.3 s total: 0.15 s down, midpoint
        /// callback for teleport, 0.15 s back up) across Guided respawn so teleportation feels comfortable.
        /// </summary>
        public Coroutine PlayRespawnBlink(float halfDurationSeconds = DEFAULT_RESPAWN_BLINK_HALF_SECONDS,
            Action onMidpoint = null)
        {
            if (!isActiveAndEnabled)
            {
                onMidpoint?.Invoke();
                return null;
            }
            if (respawnBlinkRoutine != null)
            {
                StopCoroutine(respawnBlinkRoutine);
                respawnBlinkRoutine = null;
            }
            respawnBlinkRoutine = StartCoroutine(RespawnBlinkRoutine(Mathf.Max(0.05f, halfDurationSeconds), onMidpoint));
            return respawnBlinkRoutine;
        }

        /// <summary>
        /// Smoothly fades the entire workshop scene to pure black over <paramref name="durationSeconds"/>,
        /// then switches the black backdrop to <see cref="EXIT_BACKDROP_RENDER_QUEUE"/> (strictly behind
        /// World Space UI and <c>UIPointer</c>) and invokes <paramref name="onFullyBlack"/> so the end-of-level
        /// UI renders cleanly in a dark void until the scenario unloads.
        /// </summary>
        public Coroutine PlayExitFadeToBlack(float durationSeconds, Action onFullyBlack = null)
        {
            Clear();
            if (!isActiveAndEnabled)
            {
                onFullyBlack?.Invoke();
                return null;
            }

            EnsureOverlayMesh();
            SetOverlayRenderQueue((int)RenderQueue.Overlay);
            AlignOverlayToHead();

            if (durationSeconds <= 0.001f)
            {
                CompleteExitFadeToBackdrop(onFullyBlack);
                return null;
            }

            exitFadeRoutine = StartCoroutine(ExitFadeRoutine(durationSeconds, onFullyBlack));
            return exitFadeRoutine;
        }

        /// <summary>
        /// Stops every active pulse/blink/fade and resets the damage volume, scenario overlay mesh,
        /// fallback light, and authored colour filter cleanly.
        /// </summary>
        public void Clear()
        {
            if (pulse != null)
            {
                StopCoroutine(pulse);
                pulse = null;
            }
            if (respawnBlinkRoutine != null)
            {
                StopCoroutine(respawnBlinkRoutine);
                respawnBlinkRoutine = null;
            }
            if (exitFadeRoutine != null)
            {
                StopCoroutine(exitFadeRoutine);
                exitFadeRoutine = null;
            }
            ResetVisuals();
        }

        // =====================================================================
        // Pulse, Respawn Blink & Exit Fade-To-Black
        // =====================================================================

        private void StartPulse(float fadeIn, float hold, float fadeOut, float peak, bool scenarioOverlayPath,
            bool reducedFlashes)
        {
            if (pulse != null)
            {
                StopCoroutine(pulse);
                pulse = null;
            }
            ResetDamageVisualsOnly();
            if (!isActiveAndEnabled) return;

            if (scenarioOverlayPath)
            {
                EnsureOverlayMesh();
                SetOverlayRenderQueue((int)RenderQueue.Overlay);
                AlignOverlayToHead();
                if (!reducedFlashes) PlaceFallbackLight();
            }
            pulse = StartCoroutine(PulseRoutine(fadeIn, hold, fadeOut, peak, scenarioOverlayPath, reducedFlashes));
        }

        private IEnumerator PulseRoutine(float fadeIn, float hold, float fadeOut, float peak,
            bool scenarioOverlayPath, bool reducedFlashes)
        {
            float fadeInTime = Mathf.Max(0.001f, fadeIn);
            float holdTime = Mathf.Max(0f, hold);
            float fadeOutTime = Mathf.Max(0.001f, fadeOut);
            float elapsed = 0f;

            while (elapsed < fadeInTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInTime);
                ApplyPulseEnvelope(scenarioOverlayPath, t * peak, peak, t, reducedFlashes);
                yield return null;
            }

            ApplyPulseEnvelope(scenarioOverlayPath, peak, peak, 1f, reducedFlashes);
            if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

            elapsed = 0f;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float remaining = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
                ApplyPulseEnvelope(scenarioOverlayPath, remaining * peak, peak, remaining, reducedFlashes);
                yield return null;
            }

            ApplyPulseEnvelope(scenarioOverlayPath, 0f, peak, 0f, reducedFlashes);
            pulse = null;
        }

        private IEnumerator RespawnBlinkRoutine(float halfDuration, Action onMidpoint)
        {
            EnsureOverlayMesh();
            SetOverlayRenderQueue((int)RenderQueue.Overlay);
            AlignOverlayToHead();

            bool reduced = ReduceFlashes;
            float peak = reduced ? 0.65f : 0.94f;
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                currentBlinkIntensity = Mathf.Clamp01(elapsed / halfDuration) * peak;
                RefreshOverlayRender();
                yield return null;
            }

            currentBlinkIntensity = peak;
            RefreshOverlayRender();
            onMidpoint?.Invoke();
            AlignOverlayToHead();

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                currentBlinkIntensity = (1f - Mathf.Clamp01(elapsed / halfDuration)) * peak;
                RefreshOverlayRender();
                yield return null;
            }

            currentBlinkIntensity = 0f;
            RefreshOverlayRender();
            respawnBlinkRoutine = null;
        }

        private IEnumerator ExitFadeRoutine(float durationSeconds, Action onFullyBlack)
        {
            float duration = Mathf.Max(0.001f, durationSeconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smooth ease-in-out curve for comfortable VR blackout.
                currentExitFadeIntensity = t * t * (3f - 2f * t);
                RefreshOverlayRender();
                yield return null;
            }

            exitFadeRoutine = null;
            CompleteExitFadeToBackdrop(onFullyBlack);
        }

        private void CompleteExitFadeToBackdrop(Action onFullyBlack)
        {
            currentExitFadeIntensity = 1f;
            // Drop below Queue.Transparent (3000) so HUD_OutcomePanel, CANVAS_Menu and UIPointer render crisply on top.
            SetOverlayRenderQueue(EXIT_BACKDROP_RENDER_QUEUE);
            RefreshOverlayRender();
            onFullyBlack?.Invoke();
        }

        private void ApplyPulseEnvelope(bool scenarioOverlayPath, float amount, float peak, float normalized,
            bool reducedFlashes)
        {
            if (scenarioOverlayPath)
            {
                currentOverlayIntensity = Mathf.Clamp01(amount);
                RefreshOverlayRender();

                if (fallbackFlashLight != null)
                {
                    // Soften or skip the point-light flash under reduced flashes; overlay carries visual cue.
                    float lightAmount = reducedFlashes ? 0f : fallbackLightIntensity * Mathf.Pow(Mathf.Clamp01(normalized), 2f);
                    fallbackFlashLight.intensity = Mathf.Max(0f, lightAmount);
                    fallbackFlashLight.enabled = lightAmount > 0.01f;
                }
                return;
            }

            if (damageVolume != null) damageVolume.weight = Mathf.Clamp01(amount);
            if (colorAdjustments == null || !tintCaptured) return;
            float blend = peak > 0f ? Mathf.Clamp01(amount / peak) : 0f;
            colorAdjustments.colorFilter.value = Color.Lerp(authoredFilter, DamageTint, blend);
        }

        private void RefreshOverlayRender()
        {
            if (overlayRenderer == null) return;

            bool exitDominates = currentExitFadeIntensity > 0.001f;
            bool blinkDominates = !exitDominates && currentBlinkIntensity > 0.001f;
            float activeIntensity = exitDominates
                ? currentExitFadeIntensity
                : (blinkDominates ? currentBlinkIntensity : currentOverlayIntensity);
            bool visible = activeIntensity > 0.001f;

            overlayRenderer.enabled = visible;
            if (!visible) return;

            AlignOverlayToHead();
            Color tint = exitDominates
                ? OverlayPureBlackColor
                : (blinkDominates ? OverlayDarkColor : OverlayRedColor);
            float fullScreen = (exitDominates || blinkDominates) ? 1f : 0f;

            if (overlayBlock == null) overlayBlock = new MaterialPropertyBlock();
            overlayRenderer.GetPropertyBlock(overlayBlock);
            if (usesCustomOverlayShader)
            {
                overlayBlock.SetColor(ColorPropId, tint);
                overlayBlock.SetFloat(IntensityPropId, activeIntensity);
                overlayBlock.SetFloat(FullScreenFadePropId, fullScreen);
            }
            else
            {
                float alphaScale = exitDominates ? 1f : (blinkDominates ? 0.9f : 0.45f);
                Color fallbackTint = new Color(tint.r, tint.g, tint.b, activeIntensity * alphaScale);
                overlayBlock.SetColor(BaseColorPropId, fallbackTint);
                overlayBlock.SetColor(ColorPropId, fallbackTint);
            }
            overlayRenderer.SetPropertyBlock(overlayBlock);
        }

        private void ResetDamageVisualsOnly()
        {
            currentOverlayIntensity = 0f;
            if (damageVolume != null) damageVolume.weight = 0f;
            if (fallbackFlashLight != null)
            {
                fallbackFlashLight.intensity = 0f;
                fallbackFlashLight.enabled = false;
            }
            if (tintCaptured && colorAdjustments != null) colorAdjustments.colorFilter.value = authoredFilter;
            RefreshOverlayRender();
        }

        private void ResetVisuals()
        {
            currentBlinkIntensity = 0f;
            currentExitFadeIntensity = 0f;
            SetOverlayRenderQueue((int)RenderQueue.Overlay);
            ResetDamageVisualsOnly();
        }

        // =====================================================================
        // Overlay mesh & camera inspection helpers
        // =====================================================================

        private void EnsureOverlayMesh()
        {
            if (overlayObject != null && overlayRenderer != null) return;

            overlayObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlayObject.name = "VFX_ScenarioDamageOverlay";
            overlayObject.transform.SetParent(transform, false);
            overlayObject.transform.localScale = new Vector3(OVERLAY_SCALE, OVERLAY_SCALE, 1f);

            Collider col = overlayObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            overlayRenderer = overlayObject.GetComponent<MeshRenderer>();
            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            overlayRenderer.lightProbeUsage = LightProbeUsage.Off;
            overlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // Always instantiate a runtime material copy so renderQueue changes never mutate shared assets on disk.
            if (overlayMaterialAsset != null)
            {
                runtimeOverlayMaterial = new Material(overlayMaterialAsset);
                usesCustomOverlayShader = runtimeOverlayMaterial.HasProperty(IntensityPropId);
            }
            else
            {
                Shader shader = Shader.Find("SI81/MachineLock/DamageOverlay");
                if (shader != null)
                {
                    runtimeOverlayMaterial = new Material(shader);
                    usesCustomOverlayShader = true;
                }
                else
                {
                    Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    if (unlit == null) unlit = Shader.Find("Sprites/Default");
                    runtimeOverlayMaterial = new Material(unlit);
                    ConfigureTransparentFallback(runtimeOverlayMaterial);
                    usesCustomOverlayShader = false;
                }
            }
            overlayRenderer.sharedMaterial = runtimeOverlayMaterial;
            overlayRenderer.enabled = false;
        }

        private void SetOverlayRenderQueue(int queue)
        {
            if (runtimeOverlayMaterial != null)
                runtimeOverlayMaterial.renderQueue = queue;
        }

        private static void ConfigureTransparentFallback(Material mat)
        {
            if (mat == null) return;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)RenderQueue.Overlay;
        }

        private void AlignOverlayToHead()
        {
            if (overlayObject == null) return;
            Transform head = ResolveHeadTransform();
            if (head == null) return;
            overlayObject.transform.SetPositionAndRotation(
                head.position + head.forward * OVERLAY_DISTANCE,
                head.rotation);
        }

        /// <summary>
        /// Checks whether the URP global Volume can actually render on the active camera. Returns false
        /// whenever <see cref="UniversalAdditionalCameraData.renderPostProcessing"/> is false on the active
        /// camera (ADR-0003), engaging the scenario-owned visual overlay automatically.
        /// </summary>
        private bool ResolveVolumePath()
        {
            if (forceFallbackForTesting || damageVolume == null) return false;
            if (!damageVolume.enabled || !damageVolume.isActiveAndEnabled) return false;

            Camera activeCamera = ResolveActiveCamera();
            if (activeCamera != null)
            {
                UniversalAdditionalCameraData cameraData = activeCamera.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData == null || !cameraData.renderPostProcessing) return false;
            }

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

        private static Camera ResolveActiveCamera()
        {
            Transform head = RigRegistration.Instance != null ? RigRegistration.Instance.Head : null;
            if (head != null)
            {
                Camera headCam = head.GetComponent<Camera>();
                if (headCam == null) headCam = head.GetComponentInChildren<Camera>();
                if (headCam != null && headCam.enabled) return headCam;
            }
            return Camera.main;
        }

        private static Transform ResolveHeadTransform()
        {
            Transform head = RigRegistration.Instance != null ? RigRegistration.Instance.Head : null;
            if (head != null) return head;
            Camera main = Camera.main;
            return main != null ? main.transform : null;
        }

        private void PlaceFallbackLight()
        {
            if (fallbackFlashLight == null) return;
            fallbackFlashLight.enabled = true;
            Transform head = ResolveHeadTransform();
            if (head != null) fallbackFlashLight.transform.position = head.position;
        }

        private AudioClip SelectClip(CriticalEvent evt)
        {
            switch (evt)
            {
                case CriticalEvent.LiveOilShock: return shockClip;
                case CriticalEvent.WaterExtinguisher: return steamClip;
                default: return burnClip;
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || oneShotAudio == null) return;
            oneShotAudio.PlayOneShot(clip);
        }

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
