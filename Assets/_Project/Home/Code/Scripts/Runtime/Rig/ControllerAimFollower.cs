using SI81.Input;
using UnityEngine;

namespace SI81.Rig
{
    /// <summary>
    /// Allinea il modello del controller alla posa "aim" letta dall'Input Service, convertita nello spazio mondo con
    /// l'origine del tracciamento del rig. Così il raggio e la mano disegnata partono dallo stesso punto.
    /// Il modello resta solo visivo: nessun input o interazione viene aggiunto da questo componente.
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Rig/Controller Aim Follower")]
    public sealed class ControllerAimFollower : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: dopo l'aggiornamento delle ancore del rig e della UI.</summary>
        public const int EXECUTION_ORDER = 10000;

        [Tooltip("Mano del controller rappresentato dal modello.")]
        [SerializeField] private Hand hand = Hand.Left;
        [Tooltip("Rig da cui leggere l'origine del tracciamento. Vuoto = RigRegistration.Instance.")]
        [SerializeField] private RigRegistration rig;

        /// <summary>
        /// Risolve il rig se non è assegnato nel prefab.
        /// </summary>
        private void Awake()
        {
            if (rig == null) rig = RigRegistration.Instance;
        }

        /// <summary>
        /// Sposta il modello sulla posa aim in spazio mondo.
        /// </summary>
        private void LateUpdate()
        {
            InputService service = InputService.Instance;
            if (service == null) return;
            if (rig == null) rig = RigRegistration.Instance;
            if (rig == null || rig.TrackingOrigin == null) return;
            if (!service.TryGetAimPose(hand, out Vector3 localPosition, out Quaternion localRotation)) return;
            Transform origin = rig.TrackingOrigin;
            transform.SetPositionAndRotation(origin.TransformPoint(localPosition), origin.rotation * localRotation);
        }

        /// <summary>
        /// Configura mano e rig. Usato dal builder del prefab.
        /// </summary>
        /// <param name="newHand">Mano del modello.</param>
        /// <param name="newRig">Rig del giocatore.</param>
        public void Configure(Hand newHand, RigRegistration newRig)
        {
            hand = newHand;
            rig = newRig;
        }
    }
}
