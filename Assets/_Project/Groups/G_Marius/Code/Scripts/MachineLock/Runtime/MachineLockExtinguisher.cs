using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Water extinguisher of the Machine Lock scenario (GDD §9.9): the critical distractor on the north
    /// wall. Selecting it sprays water on the burning oil and triggers the steam, smoke and critical event.
    /// It stays visible and selectable: it is never hidden as a trap.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Extinguisher")]
    public sealed class MachineLockExtinguisher : MonoBehaviour
    {
        [Header("Scenario")]
        [Tooltip("Scenario that receives the critical event. Optional.")]
        [SerializeField] private MachineLockScenario scenario;

        [Header("Control")]
        [Tooltip("Water extinguisher wall mounted on the north wall.")]
        [SerializeField] private Interactable extinguisherInteractable;

        [Header("VFX")]
        [Tooltip("Water spray from the nozzle.")]
        [SerializeField] private ParticleSystem waterSprayVfx;
        [Tooltip("Steam burst where the water meets the hot oil.")]
        [SerializeField] private ParticleSystem steamBurstVfx;
        [Tooltip("Black smoke burst of the reaction.")]
        [SerializeField] private ParticleSystem smokeBurstVfx;

        [Header("Audio")]
        [Tooltip("One-shot source of the spray.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("Steam burst clip.")]
        [SerializeField] private AudioClip steamBurstClip;

        private float lastHoverTime = float.NegativeInfinity;

        private void OnEnable()
        {
            if (extinguisherInteractable != null)
            {
                extinguisherInteractable.SelectEntered += OnExtinguisherSelected;
                extinguisherInteractable.HoverEntered += OnExtinguisherHovered;
            }
        }

        private void OnDisable()
        {
            if (extinguisherInteractable != null)
            {
                extinguisherInteractable.SelectEntered -= OnExtinguisherSelected;
                extinguisherInteractable.HoverEntered -= OnExtinguisherHovered;
            }
        }

        private void Awake()
        {
            if (oneShotAudio != null) oneShotAudio.playOnAwake = false;
        }

        /// <summary>
        /// Sprays water: water, steam and smoke particles, the steam clip, the subtitled sound event and
        /// the <see cref="CriticalEvent.WaterExtinguisher"/> notification. Every effect is optional.
        /// </summary>
        public void TriggerSpray()
        {
            Play(waterSprayVfx);
            Play(steamBurstVfx);
            Play(smokeBurstVfx);

            if (oneShotAudio != null && steamBurstClip != null) oneShotAudio.PlayOneShot(steamBurstClip);
            if (HUDService.IsReady) HUDService.ShowSoundEvent(MachineLockTexts.SoundSteamBurst, true);
            if (scenario != null) scenario.NotifyCritical(CriticalEvent.WaterExtinguisher);
        }

        private static void Play(ParticleSystem system)
        {
            if (system == null) return;
            system.Play(true);
        }

        private void OnExtinguisherSelected(Hand hand)
        {
            TriggerSpray();
        }

        private void OnExtinguisherHovered(Hand hand)
        {
            float now = Time.unscaledTime;
            if (!float.IsNegativeInfinity(lastHoverTime) && now - lastHoverTime < 0.3f) return;
            lastHoverTime = now;
            if (HUDService.IsReady)
                HUDService.ShowActionFeedback(MachineLockTexts.HoverWaterExtinguisher, FeedbackKind.Negative);
        }
    }
}
