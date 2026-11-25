using SI81.Input;
using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Adaptor over the shared InputService. Reads no device directly: the rig owns all input.
    /// A/X are intentionally unbound in the common UI, so hint requests come from HUD/automatic aids.
    /// </summary>
    public sealed class ScenarioInputBindings : MonoBehaviour
    {
        private bool previousGrabLeft;
        private bool previousGrabRight;

        private static InputService Service => InputService.Instance;

        private void LateUpdate()
        {
            InputService service = Service;
            previousGrabLeft = service != null && service.IsGrabHeld(Hand.Left);
            previousGrabRight = service != null && service.IsGrabHeld(Hand.Right);
        }

        /// <summary>True on the frame the grip is pressed.</summary>
        public bool GetGripDown(Hand hand)
        {
            InputService service = Service;
            if (service == null)
                return false;
            bool current = service.IsGrabHeld(hand);
            return current && !(hand == Hand.Left ? previousGrabLeft : previousGrabRight);
        }

        /// <summary>True on the frame the grip is released.</summary>
        public bool GetGripUp(Hand hand)
        {
            InputService service = Service;
            if (service == null)
                return false;
            bool current = service.IsGrabHeld(hand);
            return !current && (hand == Hand.Left ? previousGrabLeft : previousGrabRight);
        }

        /// <summary>True on the frame the trigger is pressed.</summary>
        public bool GetTriggerDown(Hand hand)
        {
            InputService service = Service;
            return service != null && service.WasSelectPressed(hand);
        }

        /// <summary>Unbound in the common rig: hints are automatic or requested from the HUD.</summary>
        public bool GetHintDown(Hand hand)
        {
            return false;
        }

        /// <summary>B/Y back button, used only while a panel is open.</summary>
        public bool GetDismissDown(Hand hand)
        {
            InputService service = Service;
            return service != null && service.WasBackPressed();
        }
    }
}
