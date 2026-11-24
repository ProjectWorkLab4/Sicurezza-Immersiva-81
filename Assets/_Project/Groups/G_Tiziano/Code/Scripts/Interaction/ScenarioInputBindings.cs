using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Centralized provisional controller bindings for the Chemical Alert scenario.
    /// </summary>
    public sealed class ScenarioInputBindings : MonoBehaviour
    {
        /// <summary>
        /// Returns whether the grip input was pressed this frame for the specified controller.
        /// </summary>
        public bool GetGripDown(OVRInput.Controller controller) =>
            OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller);

        /// <summary>
        /// Returns whether the grip input was released this frame for the specified controller.
        /// </summary>
        public bool GetGripUp(OVRInput.Controller controller) =>
            OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller);

        /// <summary>
        /// Returns whether the trigger input was pressed this frame for the specified controller.
        /// </summary>
        public bool GetTriggerDown(OVRInput.Controller controller) =>
            OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller);

        /// <summary>
        /// Returns whether the hand-agnostic primary face button (A/X) was pressed.
        /// </summary>
        public bool GetHintDown(OVRInput.Controller controller) =>
            OVRInput.GetDown(OVRInput.Button.One, controller);

        /// <summary>Returns whether the physical B button was pressed to dismiss a modal panel.</summary>
        public bool GetDismissDown(OVRInput.Controller controller) =>
            OVRInput.GetDown(OVRInput.RawButton.B, controller);

    }
}
