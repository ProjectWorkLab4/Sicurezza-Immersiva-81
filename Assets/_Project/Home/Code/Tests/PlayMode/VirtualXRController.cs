using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;

namespace SI81.Rig.Tests
{
    /// <summary>
    /// Controller XR virtuale per i test PlayMode: stessi nomi dei controlli del controller OpenXR
    /// (pointerPosition, pointerRotation, triggerPressed, gripPressed, secondaryButton, menuButton, primary2DAxis).
    /// Non richiede visore né runtime OpenXR.
    /// </summary>
    [InputControlLayout(displayName = "Virtual XR Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    public sealed class VirtualXRController : XRControllerWithRumble
    {
        /// <summary>Posa aim: posizione.</summary>
        [InputControl]
        public Vector3Control pointerPosition { get; private set; }
        /// <summary>Posa aim: rotazione.</summary>
        [InputControl]
        public QuaternionControl pointerRotation { get; private set; }
        /// <summary>Grilletto.</summary>
        [InputControl]
        public ButtonControl triggerPressed { get; private set; }
        /// <summary>Impugnatura.</summary>
        [InputControl]
        public ButtonControl gripPressed { get; private set; }
        /// <summary>Tasto secondario (B/Y).</summary>
        [InputControl]
        public ButtonControl secondaryButton { get; private set; }
        /// <summary>Tasto menu.</summary>
        [InputControl]
        public ButtonControl menuButton { get; private set; }
        /// <summary>Stick.</summary>
        [InputControl]
        public StickControl primary2DAxis { get; private set; }

        /// <summary>
        /// Collega i controlli dichiarati.
        /// </summary>
        protected override void FinishSetup()
        {
            base.FinishSetup();
            pointerPosition = GetChildControl<Vector3Control>("pointerPosition");
            pointerRotation = GetChildControl<QuaternionControl>("pointerRotation");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            menuButton = GetChildControl<ButtonControl>("menuButton");
            primary2DAxis = GetChildControl<StickControl>("primary2DAxis");
        }
    }
}
