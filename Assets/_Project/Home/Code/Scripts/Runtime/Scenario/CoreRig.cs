using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Passaggio di consegne del rig condiviso di SCN_Core: all'avvio dà testa, origine del tracciamento e controller a MNG_UI
    /// (UIScreenManager.SetHead / SetTrackingOrigin) e a MNG_HUD (HUDRoot.SetRig). Sta sulla radice del rig segnaposto di SCN_Core;
    /// quando arriva il rig vero (VR Interaction System) si mette questo componente sul suo prefab e si assegnano gli stessi quattro
    /// riferimenti, senza altro codice. Nessuna ricerca per nome.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("SI81/Scenari/Core Rig")]
    public class CoreRig : MonoBehaviour
    {
        [Tooltip("Testa del giocatore (camera).")]
        [SerializeField] private Transform head;
        [Tooltip("Origine del tracciamento (genitore di testa e controller, es. CameraOffset / TrackingSpace).")]
        [SerializeField] private Transform trackingOrigin;
        [Tooltip("Controller sinistro (ancora del polso).")]
        [SerializeField] private Transform leftHand;
        [Tooltip("Controller destro (ancora del polso).")]
        [SerializeField] private Transform rightHand;

        /// <summary>
        /// Consegna il rig a MNG_UI e MNG_HUD (se esistono già; gli altri ripiegano da soli su Camera.main).
        /// </summary>
        private void Start()
        {
            Apply();
        }

        /// <summary>
        /// Assegna testa e origine alla UI e testa e controller alla HUD. Richiamabile se il rig cambia a runtime.
        /// </summary>
        public void Apply()
        {
            UIScreenManager manager = UIScreenManager.Instance;
            if (manager != null)
            {
                if (head != null) manager.SetHead(head);
                if (trackingOrigin != null) manager.SetTrackingOrigin(trackingOrigin);
            }
            HUDRoot hud = FindFirstObjectByType<HUDRoot>(FindObjectsInactive.Include);
            if (hud != null && head != null) hud.SetRig(head, leftHand, rightHand);
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(Transform newHead, Transform newOrigin, Transform newLeft, Transform newRight)
        {
            head = newHead;
            trackingOrigin = newOrigin;
            leftHand = newLeft;
            rightHand = newRight;
        }
    }
}
