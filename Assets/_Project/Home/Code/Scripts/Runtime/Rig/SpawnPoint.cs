using UnityEngine;

namespace SI81.Rig
{
    /// <summary>
    /// Punto di partenza del giocatore in una scena scenario (MNG_SpawnPoint). All'attivazione porta la radice del rig
    /// qui con posizione e yaw del marker; alla disattivazione (scaricamento della scena) ripristina la posa precedente.
    /// La scena scenario resta senza rig: questo componente è solo un marker di contenuto.
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Rig/Spawn Point (MNG_SpawnPoint)")]
    public sealed class SpawnPoint : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: dopo RigRegistration.</summary>
        public const int EXECUTION_ORDER = -40;

        [Tooltip("Porta il rig qui all'attivazione del marker.")]
        [SerializeField] private bool applyOnEnable = true;

        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private RigRegistration appliedRig;
        private bool applied;

        /// <summary>
        /// Applica lo spawn quando la scena scenario viene caricata/additivata.
        /// </summary>
        private void OnEnable()
        {
            RigRegistration.Registered += HandleRigRegistered;
            if (applyOnEnable) Apply();
        }

        /// <summary>
        /// Riprova in Start se il rig non esisteva ancora in OnEnable.
        /// </summary>
        private void Start()
        {
            if (applyOnEnable && !applied) Apply();
        }

        /// <summary>
        /// Ripristina la posa precedente quando la scena scenario viene scaricata.
        /// </summary>
        private void OnDisable()
        {
            RigRegistration.Registered -= HandleRigRegistered;
            if (!applied) return;
            RigRegistration rig = appliedRig;
            applied = false;
            appliedRig = null;
            if (rig != null && RigRegistration.Instance == rig) rig.TeleportTo(previousPosition, previousRotation);
        }

        /// <summary>
        /// Il rig è comparso dopo lo scenario (SCN_Core additivo): applica lo spawn ora.
        /// </summary>
        /// <param name="rig">Rig registrato.</param>
        private void HandleRigRegistered(RigRegistration rig)
        {
            if (applyOnEnable) Apply();
        }

        /// <summary>
        /// Porta il rig alla posizione del marker e al suo yaw. Ripetibile: salva la posa precedente una sola volta.
        /// </summary>
        public void Apply()
        {
            RigRegistration rig = RigRegistration.Instance;
            if (rig == null) return;
            if (!applied || appliedRig != rig)
            {
                previousPosition = rig.transform.position;
                previousRotation = rig.transform.rotation;
                appliedRig = rig;
                applied = true;
            }
            Quaternion yaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            rig.TeleportTo(transform.position, yaw);
        }
    }
}
