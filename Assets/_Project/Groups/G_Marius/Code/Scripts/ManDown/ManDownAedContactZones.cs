using SI81.Rig;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Zone di sicurezza del DAE sul corpo dell'NPC. Sono trigger locali figli delle ossa, quindi
    /// seguono le pose; non generano collisioni fisiche e non interferiscono con le altre interazioni.
    /// Il contatto si calcola dalle sole ancore delle mani del rig condiviso, con un test sul box
    /// orientato di ogni zona (non sull'AABB): una mano può essere in più zone, ma conta una sola volta.
    /// Nessun collider sul rig, nessun raycast e nessun oggetto scenico (DAE/elettrodi/cavi) considerato.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/AED Contact Zones")]
    public sealed class ManDownAedContactZones : MonoBehaviour
    {
        [Tooltip("Trigger locali, uno per parte del corpo. Posizione/rotazione/dimensioni regolabili nell'Inspector.")]
        [SerializeField] private BoxCollider[] zones;
        [Tooltip("Piccola tolleranza applicata ai bordi di ogni zona, in metri. 0 = aderente alla mesh.")]
        [Min(0f)]
        [SerializeField] private float margin;

        private readonly bool[] activeZones = new bool[8];

        /// <summary>Indica se almeno una mano è in contatto con una zona.</summary>
        public bool HasContact { get; private set; }

        /// <summary>Mano sinistra in contatto con una qualsiasi zona.</summary>
        public bool LeftContacting { get; private set; }
        /// <summary>Mano destra in contatto con una qualsiasi zona.</summary>
        public bool RightContacting { get; private set; }
        /// <summary>Nome della zona che ha determinato l'ultimo contatto (per i log diagnostici).</summary>
        public string LastZoneName { get; private set; } = string.Empty;
        /// <summary>Nome dell'ancora della mano che ha determinato l'ultimo contatto.</summary>
        public string LastHandName { get; private set; } = string.Empty;

        /// <summary>
        /// Ricalcola lo stato da zero a ogni chiamata: l'uscita da una zona mentre la mano resta in
        /// un'altra non lascia riferimenti pendenti e il contatto cessa solo con entrambe le mani fuori.
        /// </summary>
        public void Refresh()
        {
            LeftContacting = false;
            RightContacting = false;
            LastZoneName = string.Empty;
            LastHandName = string.Empty;
            if (zones == null) { HasContact = false; return; }
            for (int i = 0; i < zones.Length && i < activeZones.Length; i++) activeZones[i] = false;

            RigRegistration rig = RigRegistration.Instance;
            if (rig == null) { HasContact = false; return; }

            bool left = false;
            bool right = false;
            CheckHand(rig.LeftHand, "Left", ref left);
            CheckHand(rig.RightHand, "Right", ref right);
            LeftContacting = left;
            RightContacting = right;
            HasContact = left || right;
        }

        private void CheckHand(Transform anchor, string handName, ref bool contacting)
        {
            if (anchor == null) return;
            Vector3 hand = anchor.position;
            for (int i = 0; i < zones.Length && i < activeZones.Length; i++)
            {
                BoxCollider zone = zones[i];
                if (zone == null || !zone.enabled) continue;
                if (!ContainsLocal(zone, hand)) continue;

                contacting = true;
                activeZones[i] = true;
                if (string.IsNullOrEmpty(LastZoneName))
                {
                    LastZoneName = zone.name;
                    LastHandName = anchor.name + " (" + handName + ")";
                }
            }
        }

        /// <summary>Test nel sistema locale del trigger: esatto anche quando l'NPC cambia posa.</summary>
        private bool ContainsLocal(BoxCollider zone, Vector3 worldPoint)
        {
            Vector3 local = zone.transform.InverseTransformPoint(worldPoint);
            Vector3 center = zone.center;
            Vector3 half = zone.size * 0.5f + Vector3.one * margin;
            return local.x >= center.x - half.x && local.x <= center.x + half.x &&
                   local.y >= center.y - half.y && local.y <= center.y + half.y &&
                   local.z >= center.z - half.z && local.z <= center.z + half.z;
        }

        public void ResetContact()
        {
            HasContact = false;
            LeftContacting = false;
            RightContacting = false;
            LastZoneName = string.Empty;
            LastHandName = string.Empty;
            for (int i = 0; i < activeZones.Length; i++) activeZones[i] = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawZones(false);
        }

        private void OnDrawGizmos()
        {
            // I Gizmos esistono solo nell'Editor: mai nel gameplay VR.
            if (Application.isPlaying && HasContact) DrawZones(true);
        }

        private void DrawZones(bool contacted)
        {
            if (zones == null) return;
            for (int i = 0; i < zones.Length && i < activeZones.Length; i++)
            {
                BoxCollider zone = zones[i];
                if (zone == null) continue;
                bool active = Application.isPlaying && activeZones[i];
                Gizmos.color = contacted && active ? new Color(1f, 0.15f, 0.15f) :
                    new Color(0.85f, 0.25f, 0.95f);
                Gizmos.matrix = zone.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(zone.center, zone.size + Vector3.one * margin * 2f);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
