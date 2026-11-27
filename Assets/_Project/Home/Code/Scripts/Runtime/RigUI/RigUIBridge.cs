using SI81.Input;
using SI81.Rig;
using UnityEngine;

namespace SI81.UI.Rig
{
    /// <summary>
    /// Unico adattatore tra il Player Rig condiviso (<c>SI81.Rig</c>) e la UI comune (<c>SI81.UI</c>).
    /// Vive in un assembly separato così <c>SI81.UI</c> non referenzia <c>SI81.Rig</c>: il rig non conosce la UI e la UI
    /// non conosce il rig, tranne questo componente.
    /// <para>
    /// <see cref="RigRegistration"/> lo trova da solo una volta in <c>Start</c> oppure lo riceve nel campo
    /// <c>uiBridge</c>. Consegna testa, origine del tracciamento e mani a <see cref="UIScreenManager"/> e
    /// <see cref="HUDRoot"/>, espone il gate del puntatore UI per mano, inoltra l'aptica a
    /// <see cref="HapticsService"/> (che applica il gate <c>controls.ui_haptics</c>) e applica la modalità seduto
    /// (<see cref="SettingsCatalog.COMFORT_SEATED_MODE"/>) e la ricalibrazione (<see cref="SettingsScreen.RecalibrateRequested"/>)
    /// alzando il <c>TrackingOrigin</c> del rig così camera, mani e raggi restano allineati.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Rig UI Bridge")]
    public sealed class RigUIBridge : MonoBehaviour, IRigUIBridge
    {
        /// <summary>Target standing eye height (metres) used to calibrate seated mode from tracked HMD height.</summary>
        public const float TARGET_STANDING_EYE_HEIGHT = 1.60f;
        /// <summary>Fallback vertical offset (metres) applied when seated mode is toggled before HMD tracking is ready or while standing.</summary>
        public const float DEFAULT_SEATED_OFFSET = 0.40f;
        /// <summary>Minimum local HMD Y height (metres) considered a valid tracked head pose.</summary>
        public const float MIN_VALID_HEAD_HEIGHT = 0.40f;
        /// <summary>Maximum local HMD Y height (metres) auto-calibrated on toggle; higher postures use <see cref="DEFAULT_SEATED_OFFSET"/> until recalibrated.</summary>
        public const float MAX_AUTO_SEATED_HEAD_HEIGHT = 1.45f;
        /// <summary>Maximum vertical seated lift (metres) allowed so the player stays within room clearance.</summary>
        public const float MAX_SEATED_OFFSET = 0.75f;

        [SerializeField] private UIScreenManager screens;
        [SerializeField] private HUDRoot hud;
        [SerializeField] private UIPointer leftPointer;
        [SerializeField] private UIPointer rightPointer;
        [SerializeField] private HapticsService haptics;

        private Transform trackedHead;
        private Transform trackingOrigin;
        private float baseTrackingOriginLocalY;
        private bool hasBaseTrackingOriginY;
        private float appliedSeatedOffset;
        private bool pendingSeatedCalibration;
        private bool pendingExplicitRecalibrate;
        private IUISettingsStore boundStore;

        /// <summary>Current vertical seated-mode offset (metres) applied to the rig's <c>TrackingOrigin</c>.</summary>
        public float AppliedSeatedOffset => appliedSeatedOffset;

        private void OnEnable()
        {
            UIServices.Ready += BindSettings;
            SettingsScreen.RecalibrateRequested += HandleRecalibrateRequested;
            BindSettings();
        }

        private void OnDisable()
        {
            UIServices.Ready -= BindSettings;
            SettingsScreen.RecalibrateRequested -= HandleRecalibrateRequested;
            if (boundStore != null)
            {
                boundStore.Changed -= HandleSettingChanged;
                boundStore = null;
            }
            pendingSeatedCalibration = false;
            pendingExplicitRecalibrate = false;
            ApplySeatedOffset(0f);
        }

        private void OnDestroy()
        {
            ApplySeatedOffset(0f);
        }

        private void LateUpdate()
        {
            if (!pendingSeatedCalibration) return;
            if (trackedHead == null || trackingOrigin == null) return;
            if (trackedHead.localPosition.y < MIN_VALID_HEAD_HEIGHT) return;

            bool explicitRecalibrate = pendingExplicitRecalibrate;
            pendingSeatedCalibration = false;
            pendingExplicitRecalibrate = false;

            float offset = ComputeSeatedOffset(explicitRecalibrate, out _);
            ApplySeatedOffset(offset);
            if (screens != null && screens.IsMenuVisible) screens.Recenter();
        }

        /// <summary>
        /// Consegna il rig alla schermata corrente e alla HUD. Chiamato da <see cref="RigRegistration"/> appena i
        /// riferimenti sono pronti e ogni volta che il rig cambia.
        /// </summary>
        /// <param name="handoff">Testa, origine del tracciamento e ancore delle due mani.</param>
        public void ApplyRig(RigHandoff handoff)
        {
            if (handoff.TrackingOrigin != trackingOrigin)
            {
                // Restore previous rig's tracking origin before switching to a different rig instance.
                ApplySeatedOffset(0f);
                trackingOrigin = handoff.TrackingOrigin;
                hasBaseTrackingOriginY = trackingOrigin != null;
                baseTrackingOriginLocalY = hasBaseTrackingOriginY ? trackingOrigin.localPosition.y : 0f;
            }
            trackedHead = handoff.Head;

            if (screens != null)
            {
                screens.SetHead(handoff.Head);
                screens.SetTrackingOrigin(handoff.TrackingOrigin);
            }
            if (hud != null)
                hud.SetRig(handoff.Head, handoff.LeftHand, handoff.RightHand);

            if (boundStore == null && UIServices.Settings != null) BindSettings();
            else SyncSeatedMode(recenterMenu: false, explicitRecalibrate: false);
        }

        /// <summary>
        /// Recalibrates the seated height offset against the current head pose when seated mode is enabled,
        /// and recenters the active menu if visible.
        /// </summary>
        public void RecalibrateSeatedHeight()
        {
            if (boundStore == null && UIServices.Settings != null) BindSettings();
            SyncSeatedMode(recenterMenu: true, explicitRecalibrate: true);
        }

        /// <summary>
        /// true se il raggio di quella mano sta puntando un elemento UI. Il rig lo usa per non consegnare il Select
        /// al mondo mentre la UI lo consuma.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        /// <returns>true se il puntatore della mano è sopra la UI.</returns>
        public bool IsPointingAtUI(Hand hand)
        {
            UIPointer pointer = hand == Hand.Left ? leftPointer : rightPointer;
            return pointer != null && pointer.IsPointingAtUI;
        }

        /// <summary>
        /// Impulso aptico sulla mano richiesta. Il gate delle impostazioni resta dentro <see cref="HapticsService"/>.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        /// <param name="amplitude">Ampiezza 0..1.</param>
        /// <param name="duration">Durata in secondi.</param>
        public void Pulse(Hand hand, float amplitude, float duration)
        {
            if (haptics == null) return;
            haptics.Pulse(hand == Hand.Left ? HUDHand.Left : HUDHand.Right, amplitude, duration);
        }

        /// <summary>
        /// Assegna i riferimenti della UI. Usato dai builder Editor e dall'integrazione di SCN_Core; a runtime i
        /// riferimenti sono serializzati nel prefab/istanza.
        /// </summary>
        /// <param name="newScreens">Gestore delle schermate.</param>
        /// <param name="newHud">HUD comune.</param>
        /// <param name="left">Puntatore della mano sinistra.</param>
        /// <param name="right">Puntatore della mano destra.</param>
        /// <param name="newHaptics">Servizio aptico.</param>
        public void Configure(UIScreenManager newScreens, HUDRoot newHud, UIPointer left, UIPointer right, HapticsService newHaptics)
        {
            screens = newScreens;
            hud = newHud;
            leftPointer = left;
            rightPointer = right;
            haptics = newHaptics;
            if (screens != null) screens.SeatedHeightOffset = appliedSeatedOffset;
        }

        private void BindSettings()
        {
            IUISettingsStore store = UIServices.Settings;
            if (boundStore == store)
            {
                SyncSeatedMode(recenterMenu: false, explicitRecalibrate: false);
                return;
            }
            if (boundStore != null) boundStore.Changed -= HandleSettingChanged;
            boundStore = store;
            if (boundStore != null) boundStore.Changed += HandleSettingChanged;
            SyncSeatedMode(recenterMenu: false, explicitRecalibrate: false);
        }

        private void HandleSettingChanged(string id)
        {
            if (!string.IsNullOrEmpty(id) && id != SettingsCatalog.COMFORT_SEATED_MODE) return;
            SyncSeatedMode(recenterMenu: false, explicitRecalibrate: false);
        }

        private void HandleRecalibrateRequested()
        {
            RecalibrateSeatedHeight();
        }

        private void SyncSeatedMode(bool recenterMenu, bool explicitRecalibrate)
        {
            bool seated = boundStore != null && boundStore.GetBool(SettingsCatalog.COMFORT_SEATED_MODE);
            if (!seated)
            {
                pendingSeatedCalibration = false;
                pendingExplicitRecalibrate = false;
                ApplySeatedOffset(0f);
                return;
            }

            float offset = ComputeSeatedOffset(explicitRecalibrate, out bool needsDeferredCalibration);
            pendingSeatedCalibration = needsDeferredCalibration;
            pendingExplicitRecalibrate = needsDeferredCalibration && explicitRecalibrate;
            ApplySeatedOffset(offset);

            if (recenterMenu && screens != null && screens.IsMenuVisible)
                screens.Recenter();
        }

        private float ComputeSeatedOffset(bool explicitRecalibrate, out bool needsDeferredCalibration)
        {
            if (trackedHead == null || trackedHead.localPosition.y < MIN_VALID_HEAD_HEIGHT)
            {
                needsDeferredCalibration = true;
                return DEFAULT_SEATED_OFFSET;
            }

            needsDeferredCalibration = false;
            float headLocalY = trackedHead.localPosition.y;
            if (!explicitRecalibrate && headLocalY > MAX_AUTO_SEATED_HEAD_HEIGHT)
                return DEFAULT_SEATED_OFFSET;

            return Mathf.Clamp(TARGET_STANDING_EYE_HEIGHT - headLocalY, 0f, MAX_SEATED_OFFSET);
        }

        private void ApplySeatedOffset(float offset)
        {
            appliedSeatedOffset = Mathf.Clamp(offset, 0f, MAX_SEATED_OFFSET);
            if (trackingOrigin != null && hasBaseTrackingOriginY)
            {
                Vector3 localPosition = trackingOrigin.localPosition;
                localPosition.y = baseTrackingOriginLocalY + appliedSeatedOffset;
                trackingOrigin.localPosition = localPosition;
            }
            if (screens != null)
                screens.SeatedHeightOffset = appliedSeatedOffset;
        }
    }
}
