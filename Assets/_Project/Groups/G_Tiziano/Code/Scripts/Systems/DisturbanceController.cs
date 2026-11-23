using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Drives optional Crisis light flicker and loop audio without authored animation assets.
    /// </summary>
    public sealed class DisturbanceController : MonoBehaviour
    {
        [SerializeField] private Light[] targetLights = new Light[0];
        [SerializeField] private AudioSource loopSource;
        [SerializeField, Range(0.1f, 3f)] private float frequencyHz = 2.5f;
        [SerializeField, Range(0f, 1f)] private float reducedEffectsMultiplier = 0.35f;

        private float[] originalIntensities = new float[0];
        private float originalAudioVolume;
        private bool disturbanceEnabled;
        private bool reducedEffects;
        private bool lightsOn = true;
        private float nextToggleTime;

        /// <summary>Gets whether reduced effects are enabled.</summary>
        public bool ReducedEffects => reducedEffects;
        /// <summary>Gets the effective flicker frequency, capped at 3 Hz.</summary>
        public float EffectiveFrequencyHz => Mathf.Min(frequencyHz, 3f) * (reducedEffects ? 0.5f : 1f);

        private void Awake()
        {
            frequencyHz = Mathf.Clamp(frequencyHz, 0.1f, 3f);
            CacheOriginalValues();
        }

        private void OnEnable()
        {
            CacheOriginalValues();
        }

        private void OnDisable()
        {
            RestoreOriginalValues();
        }

        private void Update()
        {
            if (!disturbanceEnabled || targetLights == null || targetLights.Length == 0)
                return;

            if (Time.unscaledTime < nextToggleTime)
                return;

            lightsOn = !lightsOn;
            float halfPeriod = 0.5f / Mathf.Max(0.1f, EffectiveFrequencyHz);
            nextToggleTime = Time.unscaledTime + halfPeriod;
            ApplyLightState();
        }

        /// <summary>Enables or disables the configured Crisis disturbance.</summary>
        public void SetDisturbanceEnabled(bool enabled)
        {
            disturbanceEnabled = enabled;
            lightsOn = true;
            nextToggleTime = Time.unscaledTime;
            if (enabled)
            {
                ApplyAudioState();
                ApplyLightState();
            }
            else
            {
                RestoreOriginalValues();
            }
        }

        /// <summary>Enables or disables reduced-intensity disturbance effects.</summary>
        public void SetReducedEffects(bool enabled)
        {
            reducedEffects = enabled;
            if (disturbanceEnabled)
            {
                ApplyAudioState();
                ApplyLightState();
            }
        }

        /// <summary>Sets the flicker frequency, clamped to the task limit of 3 Hz.</summary>
        public void SetFrequency(float value)
        {
            frequencyHz = Mathf.Clamp(value, 0.1f, 3f);
            nextToggleTime = Time.unscaledTime;
        }

        /// <summary>Prints disturbance state and effective frequency through the supplied logger.</summary>
        public void PrintState(System.Action<string> logger)
        {
            logger?.Invoke($"[CA] Disturbance: enabled={disturbanceEnabled}; reducedEffects={reducedEffects}; frequency={EffectiveFrequencyHz:0.00}Hz (max 3Hz); audio={(loopSource != null && loopSource.clip != null ? "assigned" : "none")}");
        }

        private void CacheOriginalValues()
        {
            if (targetLights == null)
                targetLights = new Light[0];

            originalIntensities = new float[targetLights.Length];
            for (int i = 0; i < targetLights.Length; i++)
                originalIntensities[i] = targetLights[i] == null ? 0f : targetLights[i].intensity;

            if (loopSource != null)
                originalAudioVolume = loopSource.volume;
        }

        private void ApplyLightState()
        {
            float lowFactor = reducedEffects ? 0.65f : 0.15f;
            float factor = lightsOn ? 1f : lowFactor;
            for (int i = 0; i < targetLights.Length; i++)
            {
                if (targetLights[i] != null && i < originalIntensities.Length)
                    targetLights[i].intensity = originalIntensities[i] * factor;
            }
        }

        private void ApplyAudioState()
        {
            if (loopSource == null || loopSource.clip == null)
                return;

            loopSource.loop = true;
            loopSource.volume = originalAudioVolume * (reducedEffects ? reducedEffectsMultiplier : 1f);
            if (!loopSource.isPlaying)
                loopSource.Play();
        }

        private void RestoreOriginalValues()
        {
            if (targetLights != null)
            {
                for (int i = 0; i < targetLights.Length && i < originalIntensities.Length; i++)
                {
                    if (targetLights[i] != null)
                        targetLights[i].intensity = originalIntensities[i];
                }
            }

            if (loopSource != null)
            {
                loopSource.Stop();
                loopSource.volume = originalAudioVolume;
            }
        }
    }
}
