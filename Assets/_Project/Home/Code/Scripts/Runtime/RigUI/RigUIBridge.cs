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
    /// <see cref="HUDRoot"/>, espone il gate del puntatore UI per mano e inoltra l'aptica a
    /// <see cref="HapticsService"/> (che applica il gate <c>controls.ui_haptics</c>).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Rig UI Bridge")]
    public sealed class RigUIBridge : MonoBehaviour, IRigUIBridge
    {
        [SerializeField] private UIScreenManager screens;
        [SerializeField] private HUDRoot hud;
        [SerializeField] private UIPointer leftPointer;
        [SerializeField] private UIPointer rightPointer;
        [SerializeField] private HapticsService haptics;

        /// <summary>
        /// Consegna il rig alla schermata corrente e alla HUD. Chiamato da <see cref="RigRegistration"/> appena i
        /// riferimenti sono pronti e ogni volta che il rig cambia.
        /// </summary>
        /// <param name="handoff">Testa, origine del tracciamento e ancore delle due mani.</param>
        public void ApplyRig(RigHandoff handoff)
        {
            if (screens != null)
            {
                screens.SetHead(handoff.Head);
                screens.SetTrackingOrigin(handoff.TrackingOrigin);
            }
            if (hud != null)
                hud.SetRig(handoff.Head, handoff.LeftHand, handoff.RightHand);
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
        }
    }
}
