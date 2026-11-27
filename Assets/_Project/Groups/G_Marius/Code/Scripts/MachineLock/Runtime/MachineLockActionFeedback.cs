using System.Collections.Generic;
using SI81.Input;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Centralized positive and negative audiovisual + haptic action feedback for Scenario 08 (Issue #46).
    /// Consolidates world-space emissive/tint pulses (via <see cref="MaterialPropertyBlock"/>, never mutating
    /// shared materials), positive chime / negative buzz one-shot audio, controller haptics, and optional
    /// HUD Action Feedback lines across containment, PPE, lockout, valve, and LOTO controls.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Action Feedback")]
    public sealed class MachineLockActionFeedback : MonoBehaviour
    {
        private const float FLASH_DURATION_SECONDS = 0.38f;
        private const float REDUCED_FLASH_DURATION_SECONDS = 0.22f;
        private const float POSITIVE_HAPTIC_AMPLITUDE = 0.28f;
        private const float POSITIVE_HAPTIC_DURATION = 0.08f;
        private const float NEGATIVE_HAPTIC_AMPLITUDE = 0.65f;
        private const float NEGATIVE_HAPTIC_DURATION = 0.16f;
        private const int SYNTH_SAMPLE_RATE = 44100;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color PositiveFlashEmission = new Color(0.14f, 0.90f, 0.38f, 1f);
        private static readonly Color NegativeFlashEmission = new Color(0.95f, 0.16f, 0.12f, 1f);
        private static readonly Color PositiveFlashTint = new Color(0.55f, 1.0f, 0.68f, 1f);
        private static readonly Color NegativeFlashTint = new Color(1.0f, 0.42f, 0.38f, 1f);

        private sealed class ActiveFlash
        {
            public Renderer[] Renderers;
            public Color[] OriginalBaseColors;
            public bool[] HasBaseColor;
            public Color FlashEmission;
            public Color FlashTint;
            public float Elapsed;
            public float Duration;
            public float IntensityScale;
        }

        [Header("Audio")]
        [Tooltip("2D/non-spatial audio source for positive and negative confirmation cues.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Ascending chime played on positive actions. Generated procedurally at runtime when unassigned.")]
        [SerializeField] private AudioClip positiveClip;
        [Tooltip("Muted low double-buzz played on negative or blocked actions. Generated procedurally at runtime when unassigned.")]
        [SerializeField] private AudioClip negativeClip;

        private readonly List<ActiveFlash> activeFlashes = new List<ActiveFlash>(8);
        private MaterialPropertyBlock sharedBlock;
        private AudioClip runtimePositiveClip;
        private AudioClip runtimeNegativeClip;

        /// <summary>Active scenario action feedback hub, or null when no scenario is loaded.</summary>
        public static MachineLockActionFeedback Instance { get; private set; }

        /// <summary>Total number of positive feedback cues emitted since initialization.</summary>
        public int PositiveCount { get; private set; }
        /// <summary>Total number of negative feedback cues emitted since initialization.</summary>
        public int NegativeCount { get; private set; }
        /// <summary>Most recent Action Feedback text passed to PlayPositive/PlayNegative.</summary>
        public string LastFeedbackMessage { get; private set; } = string.Empty;
        /// <summary>Most recent GameObject flashed visually.</summary>
        public GameObject LastFlashedTarget { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) return;
            Instance = this;
            sharedBlock = new MaterialPropertyBlock();
            EnsureAudioSource();
        }

        private void OnDisable()
        {
            ClearAllFlashes();
        }

        private void OnDestroy()
        {
            ClearAllFlashes();
            if (Instance == this) Instance = null;
            if (runtimePositiveClip != null) Destroy(runtimePositiveClip);
            if (runtimeNegativeClip != null) Destroy(runtimeNegativeClip);
        }

        private void Update()
        {
            if (activeFlashes.Count == 0) return;
            float dt = Time.deltaTime;
            for (int i = activeFlashes.Count - 1; i >= 0; i--)
            {
                ActiveFlash flash = activeFlashes[i];
                flash.Elapsed += dt;
                float t = Mathf.Clamp01(flash.Elapsed / Mathf.Max(0.001f, flash.Duration));
                if (t >= 1f)
                {
                    RestoreFlash(flash);
                    activeFlashes.RemoveAt(i);
                    continue;
                }
                // Crisp attack, smooth quadratic fade-out.
                float envelope = (1f - t) * (1f - t) * flash.IntensityScale;
                ApplyFlashEnvelope(flash, envelope);
            }
        }

        /// <summary>
        /// Triggers positive audiovisual and haptic feedback via the active scenario hub (null-safe).
        /// </summary>
        public static void PlayPositive(GameObject visualTarget, Hand hand = Hand.Any, string feedbackMessage = null)
        {
            if (Instance != null) Instance.EmitPositive(visualTarget, hand, feedbackMessage);
            else if (!string.IsNullOrEmpty(feedbackMessage) && HUDService.IsReady)
                HUDService.ShowActionFeedback(feedbackMessage, FeedbackKind.Positive);
        }

        /// <summary>
        /// Triggers negative audiovisual and haptic feedback via the active scenario hub (null-safe).
        /// </summary>
        public static void PlayNegative(GameObject visualTarget, Hand hand = Hand.Any, string feedbackMessage = null)
        {
            if (Instance != null) Instance.EmitNegative(visualTarget, hand, feedbackMessage);
            else if (!string.IsNullOrEmpty(feedbackMessage) && HUDService.IsReady)
                HUDService.ShowActionFeedback(feedbackMessage, FeedbackKind.Negative);
        }

        /// <summary>
        /// Emits a positive confirmation chime, brief green world flash, light haptic pulse, and optional HUD feedback.
        /// </summary>
        public void EmitPositive(GameObject visualTarget, Hand hand = Hand.Any, string feedbackMessage = null)
        {
            PositiveCount++;
            LastFlashedTarget = visualTarget;
            if (!string.IsNullOrEmpty(feedbackMessage)) LastFeedbackMessage = feedbackMessage;

            PlayClip(ResolvePositiveClip(), 0.72f);
            PulseHaptics(hand, POSITIVE_HAPTIC_AMPLITUDE, POSITIVE_HAPTIC_DURATION);
            if (visualTarget != null) BeginFlash(visualTarget, true);
            if (!string.IsNullOrEmpty(feedbackMessage) && HUDService.IsReady)
                HUDService.ShowActionFeedback(feedbackMessage, FeedbackKind.Positive);
        }

        /// <summary>
        /// Emits a negative error buzz, brief red world flash, firm haptic pulse, and optional HUD feedback.
        /// </summary>
        public void EmitNegative(GameObject visualTarget, Hand hand = Hand.Any, string feedbackMessage = null)
        {
            NegativeCount++;
            LastFlashedTarget = visualTarget;
            if (!string.IsNullOrEmpty(feedbackMessage)) LastFeedbackMessage = feedbackMessage;

            PlayClip(ResolveNegativeClip(), 0.85f);
            PulseHaptics(hand, NEGATIVE_HAPTIC_AMPLITUDE, NEGATIVE_HAPTIC_DURATION);
            if (visualTarget != null) BeginFlash(visualTarget, false);
            if (!string.IsNullOrEmpty(feedbackMessage) && HUDService.IsReady)
                HUDService.ShowActionFeedback(feedbackMessage, FeedbackKind.Negative);
        }

        /// <summary>Resets inspection counters (used by automated tests).</summary>
        public void ResetCounters()
        {
            PositiveCount = 0;
            NegativeCount = 0;
            LastFeedbackMessage = string.Empty;
            LastFlashedTarget = null;
        }

        private void BeginFlash(GameObject target, bool positive)
        {
            if (target == null) return;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(false);
            if (renderers == null || renderers.Length == 0) return;

            // Cancel any prior flash currently touching the same root so colors restore cleanly.
            for (int i = activeFlashes.Count - 1; i >= 0; i--)
            {
                if (activeFlashes[i].Renderers.Length > 0 && activeFlashes[i].Renderers[0] == renderers[0])
                {
                    RestoreFlash(activeFlashes[i]);
                    activeFlashes.RemoveAt(i);
                }
            }

            IUISettingsStore settings = UIServices.Settings;
            bool reducedFlashes = settings != null && settings.GetBool(SettingsCatalog.ACCESSIBILITY_REDUCE_FLASHES);

            var originals = new Color[renderers.Length];
            var hasBase = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r.sharedMaterial == null) continue;
                if (r.sharedMaterial.HasProperty(BaseColorId))
                {
                    originals[i] = r.sharedMaterial.GetColor(BaseColorId);
                    hasBase[i] = true;
                }
            }

            var flash = new ActiveFlash
            {
                Renderers = renderers,
                OriginalBaseColors = originals,
                HasBaseColor = hasBase,
                FlashEmission = positive ? PositiveFlashEmission : NegativeFlashEmission,
                FlashTint = positive ? PositiveFlashTint : NegativeFlashTint,
                Elapsed = 0f,
                Duration = reducedFlashes ? REDUCED_FLASH_DURATION_SECONDS : FLASH_DURATION_SECONDS,
                IntensityScale = reducedFlashes ? 0.45f : 1.0f
            };
            ApplyFlashEnvelope(flash, flash.IntensityScale);
            activeFlashes.Add(flash);
        }

        private void ApplyFlashEnvelope(ActiveFlash flash, float envelope)
        {
            if (sharedBlock == null) sharedBlock = new MaterialPropertyBlock();
            for (int i = 0; i < flash.Renderers.Length; i++)
            {
                Renderer r = flash.Renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(sharedBlock);
                sharedBlock.SetColor(EmissionColorId, flash.FlashEmission * (1.25f * envelope));
                if (flash.HasBaseColor[i])
                    sharedBlock.SetColor(BaseColorId, Color.Lerp(flash.OriginalBaseColors[i], flash.FlashTint, 0.55f * envelope));
                r.SetPropertyBlock(sharedBlock);
            }
        }

        private void RestoreFlash(ActiveFlash flash)
        {
            for (int i = 0; i < flash.Renderers.Length; i++)
            {
                Renderer r = flash.Renderers[i];
                if (r == null) continue;
                r.SetPropertyBlock(null);
            }
        }

        private void ClearAllFlashes()
        {
            for (int i = activeFlashes.Count - 1; i >= 0; i--)
                RestoreFlash(activeFlashes[i]);
            activeFlashes.Clear();
        }

        private void EnsureAudioSource()
        {
            if (oneShotAudio == null)
            {
                oneShotAudio = gameObject.AddComponent<AudioSource>();
                oneShotAudio.spatialBlend = 0f;
            }
            oneShotAudio.playOnAwake = false;
            oneShotAudio.loop = false;
        }

        private void PlayClip(AudioClip clip, float volumeScale)
        {
            if (clip == null) return;
            EnsureAudioSource();
            oneShotAudio.PlayOneShot(clip, volumeScale);
        }

        private static void PulseHaptics(Hand hand, float amplitude, float duration)
        {
            RigRegistration rig = RigRegistration.Instance;
            if (rig == null) return;
            if (hand == Hand.Left || hand == Hand.Right)
            {
                rig.Pulse(hand, amplitude, duration);
                return;
            }
            rig.Pulse(Hand.Left, amplitude, duration);
            rig.Pulse(Hand.Right, amplitude, duration);
        }

        private AudioClip ResolvePositiveClip()
        {
            if (positiveClip != null) return positiveClip;
            if (runtimePositiveClip == null)
                runtimePositiveClip = BuildRuntimeClip("SFX_FeedbackPositive_Runtime", BuildPositiveSamples());
            return runtimePositiveClip;
        }

        private AudioClip ResolveNegativeClip()
        {
            if (negativeClip != null) return negativeClip;
            if (runtimeNegativeClip == null)
                runtimeNegativeClip = BuildRuntimeClip("SFX_FeedbackNegative_Runtime", BuildNegativeSamples());
            return runtimeNegativeClip;
        }

        private static AudioClip BuildRuntimeClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SYNTH_SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Synthesizes a pleasant ascending two-note confirmation chime (D5 -> A5).</summary>
        public static float[] BuildPositiveSamples()
        {
            int total = (int)(SYNTH_SAMPLE_RATE * 0.24f);
            var data = new float[total];
            int split = (int)(SYNTH_SAMPLE_RATE * 0.085f);
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)SYNTH_SAMPLE_RATE;
                float sample;
                if (i < split)
                {
                    float env = Mathf.Sin(Mathf.PI * (i / (float)split));
                    sample = (Mathf.Sin(2f * Mathf.PI * 587.33f * t) * 0.65f
                              + Mathf.Sin(2f * Mathf.PI * 1174.66f * t) * 0.20f) * env * 0.50f;
                }
                else
                {
                    float localT = (i - split) / (float)SYNTH_SAMPLE_RATE;
                    float attack = Mathf.Clamp01(localT / 0.008f);
                    float decay = Mathf.Exp(-localT * 14f);
                    sample = (Mathf.Sin(2f * Mathf.PI * 880f * localT) * 0.70f
                              + Mathf.Sin(2f * Mathf.PI * 1760f * localT) * 0.18f) * attack * decay * 0.55f;
                }
                data[i] = Mathf.Clamp(sample, -0.95f, 0.95f);
            }
            return data;
        }

        /// <summary>Synthesizes a clear low muted double-buzz for wrong or blocked actions.</summary>
        public static float[] BuildNegativeSamples()
        {
            int total = (int)(SYNTH_SAMPLE_RATE * 0.28f);
            var data = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)SYNTH_SAMPLE_RATE;
                bool pulse1 = t >= 0f && t < 0.095f;
                bool pulse2 = t >= 0.140f && t < 0.265f;
                if (!pulse1 && !pulse2) continue;

                float localT = pulse1 ? t : (t - 0.140f);
                float dur = pulse1 ? 0.095f : 0.125f;
                float freq = pulse1 ? 185f : 148f;
                float env = Mathf.Clamp01(localT / 0.008f) * Mathf.Clamp01((dur - localT) / 0.025f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * localT) * 0.65f
                             + Mathf.Sin(2f * Mathf.PI * (freq * 2f) * localT) * 0.22f
                             + Mathf.Sin(2f * Mathf.PI * (freq * 3f) * localT) * 0.10f;
                data[i] = Mathf.Clamp(wave * env * 0.55f, -0.95f, 0.95f);
            }
            return data;
        }
    }
}
