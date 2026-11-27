using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Shared hover Action Feedback gate with deliberate dwell timing and a generous global cooldown
    /// across all interactive stations. Prevents controller ray sweeps and edge jitter from spamming
    /// <see cref="HUDService.ShowActionFeedback"/> banners, chimes, and non-dominant controller haptics
    /// while still surfacing live status readouts when the player deliberately aims at a PPE item or
    /// electrical Sub-Station.
    /// </summary>
    public sealed class MachineLockHoverFeedback
    {
        /// <summary>Continuous hover dwell required before emitting a tooltip toast, in unscaled seconds.</summary>
        public const float DEFAULT_DWELL_SECONDS = 0.55f;
        /// <summary>Global cooldown between any two hover tooltips across all scenario objects, in unscaled seconds.</summary>
        public const float GLOBAL_COOLDOWN_SECONDS = 4.0f;
        /// <summary>Minimum interval before repeating the exact same hover tooltip text, in unscaled seconds.</summary>
        public const float SAME_TEXT_COOLDOWN_SECONDS = 6.0f;

        private static float lastGlobalEmitTime = float.NegativeInfinity;
        private static string lastGlobalFeedback = string.Empty;

        private readonly float dwellSeconds;
        private readonly float globalCooldownSeconds;
        private readonly float sameTextCooldownSeconds;

        private bool pendingActive;
        private float pendingStartTime;
        private string pendingText = string.Empty;
        private FeedbackKind pendingKind = FeedbackKind.Neutral;

        /// <summary>
        /// Creates a hover feedback gate with deliberate dwell and global/same-text cooldowns.
        /// </summary>
        public MachineLockHoverFeedback(
            float dwellSeconds = DEFAULT_DWELL_SECONDS,
            float globalCooldownSeconds = GLOBAL_COOLDOWN_SECONDS,
            float sameTextCooldownSeconds = SAME_TEXT_COOLDOWN_SECONDS)
        {
            this.dwellSeconds = Mathf.Max(0f, dwellSeconds);
            this.globalCooldownSeconds = Mathf.Max(0f, globalCooldownSeconds);
            this.sameTextCooldownSeconds = Mathf.Max(0f, sameTextCooldownSeconds);
        }

        /// <summary>Most recently emitted hover feedback string from this instance.</summary>
        public string LastFeedback { get; private set; } = string.Empty;

        /// <summary>true while a hover target is currently dwelling toward emission.</summary>
        public bool IsPending => pendingActive;

        /// <summary>Configured continuous hover dwell delay, in unscaled seconds.</summary>
        public float DwellSeconds => dwellSeconds;

        /// <summary>Resets static global cooldown state cleanly when Domain Reload is disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetGlobalCooldown()
        {
            lastGlobalEmitTime = float.NegativeInfinity;
            lastGlobalFeedback = string.Empty;
        }

        /// <summary>
        /// Starts a dwell timer for <paramref name="text"/>. Must be ticked via <see cref="Tick"/>
        /// and cancelled via <see cref="CancelHover"/> when the ray exits before the dwell elapses.
        /// </summary>
        public void BeginHover(string text, FeedbackKind kind)
        {
            if (string.IsNullOrEmpty(text))
            {
                CancelHover();
                return;
            }

            pendingActive = true;
            pendingStartTime = Time.unscaledTime;
            pendingText = text;
            pendingKind = kind;

            if (dwellSeconds <= 0f)
            {
                pendingActive = false;
                TryEmitNow(text, kind);
            }
        }

        /// <summary>Cancels any pending hover dwell (called on HoverExited, SelectEntered, or OnDisable).</summary>
        public void CancelHover()
        {
            pendingActive = false;
            pendingText = string.Empty;
        }

        /// <summary>
        /// Advances the dwell timer. Call from <c>Update()</c> while active; emits once when the
        /// continuous hover duration reaches <see cref="DwellSeconds"/>.
        /// </summary>
        public bool Tick()
        {
            if (!pendingActive) return false;
            if (Time.unscaledTime - pendingStartTime < dwellSeconds) return false;

            string text = pendingText;
            FeedbackKind kind = pendingKind;
            pendingActive = false;
            pendingText = string.Empty;
            return TryEmitNow(text, kind);
        }

        /// <summary>
        /// Attempts to emit <paramref name="text"/> immediately, subject to the global cross-object
        /// cooldown and same-text repeat cooldown.
        /// </summary>
        public bool TryEmitNow(string text, FeedbackKind kind)
        {
            if (string.IsNullOrEmpty(text)) return false;

            float now = Time.unscaledTime;
            if (!float.IsNegativeInfinity(lastGlobalEmitTime))
            {
                float elapsed = now - lastGlobalEmitTime;
                if (elapsed < globalCooldownSeconds) return false;
                if (lastGlobalFeedback == text && elapsed < sameTextCooldownSeconds) return false;
            }

            lastGlobalEmitTime = now;
            lastGlobalFeedback = text;
            LastFeedback = text;
            if (HUDService.IsReady) HUDService.ShowActionFeedback(text, kind);
            return true;
        }
    }
}
