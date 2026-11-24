using SI81.Input;
using UnityEngine;

namespace SI81.Rig
{
    /// <summary>
    /// Porta all'Input Service lo stato "il raggio punta la UI" di ciascuna mano. Gira dopo UIPointer (ordine alto),
    /// così il Select dello stesso frame è già filtrato. Nessuna allocazione per frame.
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RigRegistration))]
    [AddComponentMenu("SI81/Rig/Rig UI Pointer Feed")]
    public sealed class RigUIPointerFeed : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: dopo i LateUpdate della UI.</summary>
        public const int EXECUTION_ORDER = 10000;

        private RigRegistration rig;

        /// <summary>
        /// Trova il rig sullo stesso GameObject.
        /// </summary>
        private void Awake()
        {
            rig = GetComponent<RigRegistration>();
        }

        /// <summary>
        /// Aggiorna il gate UI per mano.
        /// </summary>
        private void LateUpdate()
        {
            InputService service = InputService.Instance;
            if (service == null || rig == null || rig != RigRegistration.Instance) return;
            IRigUIBridge bridge = rig.Bridge;
            if (bridge == null)
            {
                service.SetUIPointerOverUI(Hand.Left, false);
                service.SetUIPointerOverUI(Hand.Right, false);
                return;
            }
            service.SetUIPointerOverUI(Hand.Left, bridge.IsPointingAtUI(Hand.Left));
            service.SetUIPointerOverUI(Hand.Right, bridge.IsPointingAtUI(Hand.Right));
        }
    }
}
