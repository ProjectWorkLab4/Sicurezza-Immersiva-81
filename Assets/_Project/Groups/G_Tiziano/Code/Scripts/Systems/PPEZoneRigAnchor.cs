using SI81.Rig;
using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Reparents a PPE equip zone to the shared Player Rig root so the zone follows the player exactly
    /// like the old OVRCameraRig children did. Works also when SCN_Core is loaded after the scenario.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PPEZoneRigAnchor : MonoBehaviour
    {
        private void OnEnable()
        {
            RigRegistration.Registered += HandleRegistered;
        }

        private void OnDisable()
        {
            RigRegistration.Registered -= HandleRegistered;
        }

        private void Start()
        {
            HandleRegistered(RigRegistration.Instance);
        }

        private void HandleRegistered(RigRegistration rig)
        {
            if (rig == null) return;
            transform.SetParent(rig.transform, false);
        }
    }
}
