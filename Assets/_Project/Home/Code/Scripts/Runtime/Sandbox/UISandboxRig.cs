using UnityEngine;

namespace SI81.UI.Sandbox
{
    /// <summary>
    /// SOLO per SCN_UISandbox: simula il rig condiviso di SCN_Core. All'avvio passa testa e controller (sinistro/destro)
    /// a <see cref="HUDRoot.SetRig"/> e rende coerenti <see cref="UIScreenManager.SetHead"/> e
    /// <see cref="UIScreenManager.SetTrackingOrigin"/>. In SCN_Core questo lavoro lo fa il rig reale (OVRCameraRig).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Sandbox Rig")]
    public class UISandboxRig : MonoBehaviour
    {
        [Tooltip("Testa (camera) del rig di prova.")]
        [SerializeField] private Transform head;
        [Tooltip("Origine del tracciamento (offset della camera).")]
        [SerializeField] private Transform trackingOrigin;
        [Tooltip("Controller sinistro.")]
        [SerializeField] private Transform leftHand;
        [Tooltip("Controller destro.")]
        [SerializeField] private Transform rightHand;
        [Tooltip("MNG_UI (se vuoto si cerca UIScreenManager in scena).")]
        [SerializeField] private UIScreenManager screenManager;
        [Tooltip("MNG_HUD (se vuoto si cerca HUDRoot in scena).")]
        [SerializeField] private HUDRoot hudRoot;

        /// <summary>
        /// Assegna i riferimenti (usato dal builder).
        /// </summary>
        /// <param name="newHead">Testa.</param>
        /// <param name="newOrigin">Origine del tracciamento.</param>
        /// <param name="left">Controller sinistro.</param>
        /// <param name="right">Controller destro.</param>
        /// <param name="manager">Gestore schermate.</param>
        /// <param name="hud">Radice HUD.</param>
        public void Configure(Transform newHead, Transform newOrigin, Transform left, Transform right, UIScreenManager manager, HUDRoot hud)
        {
            head = newHead;
            trackingOrigin = newOrigin;
            leftHand = left;
            rightHand = right;
            screenManager = manager;
            hudRoot = hud;
        }

        /// <summary>
        /// Inoltra il rig a MNG_UI e a MNG_HUD.
        /// </summary>
        private void Start()
        {
            UIScreenManager manager = screenManager != null ? screenManager : FindFirstObjectByType<UIScreenManager>();
            if (manager != null)
            {
                manager.SetHead(head);
                manager.SetTrackingOrigin(trackingOrigin);
            }
            HUDRoot hud = hudRoot != null ? hudRoot : FindFirstObjectByType<HUDRoot>(FindObjectsInactive.Include);
            if (hud != null) hud.SetRig(head, leftHand, rightHand);
        }
    }
}
