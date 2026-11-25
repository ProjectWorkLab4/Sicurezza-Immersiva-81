using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stato3
{
    public static void Esegui()
    {
        var sb = new StringBuilder();

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != "OVRCameraRig")
            {
                continue;
            }

            var t = root.transform;
            sb.AppendLine($"[RIG] pos={t.position} rot={t.eulerAngles}");

            var mgr = root.GetComponent<OVRManager>();
            if (mgr != null)
            {
                sb.AppendLine($"  OVRManager trackingOriginType={mgr.trackingOriginType}");
            }

            foreach (var child in t.GetComponentsInChildren<Transform>(true))
            {
                if (child == t) continue;
                if (child.name != "TrackingSpace" && child.name != "CenterEyeAnchor" && child.name != "LeftEyeAnchor" && child.name != "RightEyeAnchor" && child.name != "Locomotor" && child.name != "PlayerController")
                {
                    continue;
                }

                sb.AppendLine($"  {child.name} local={child.localPosition} world={child.position}");

                var cc = child.GetComponent<CharacterController>();
                if (cc != null)
                {
                    sb.AppendLine($"    CC height={cc.height} center={cc.center} radius={cc.radius} skin={cc.skinWidth}");
                }

                var rb = child.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    sb.AppendLine($"    RB mass={rb.mass} kinematic={rb.isKinematic}");
                }
            }
        }

        var rig = Object.FindFirstObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            sb.AppendLine($"[OVRCameraRig] trackingSpace={(rig.trackingSpace == null ? "-" : rig.trackingSpace.name)} centerEye={(rig.centerEyeAnchor == null ? "-" : rig.centerEyeAnchor.name)}");
        }

        Debug.Log(sb.ToString());
    }
}
