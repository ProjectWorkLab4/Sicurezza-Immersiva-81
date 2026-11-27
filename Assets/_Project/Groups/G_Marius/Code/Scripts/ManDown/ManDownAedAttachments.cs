using System;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Zone magnetiche sul torace dell'NPC per i due elettrodi del DAE. Le trasformazioni delle zone
    /// sono figlie dello scheletro, quindi seguono automaticamente il sistema di pose esistente senza
    /// modificarlo. La logica è puramente locale: nessun collider trigger, nessun evento di physics,
    /// nessuna UI e nessun cambiamento di posa. Due elettrodi sono intercambiabili e una zona può
    /// ospitare al massimo un elettrodo.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/AED Attachment Zones")]
    public sealed class ManDownAedAttachments : MonoBehaviour
    {
        [Header("Riferimenti di scena")]
        [Tooltip("NPC: la posa supina è necessaria per agganciare.")]
        [SerializeField] private ManDownNpc npc;
        [Tooltip("Zona A: torace alto a destra, sotto la clavicola (anatomia dell'NPC).")]
        [SerializeField] private Transform zoneA;
        [Tooltip("Zona B: torace laterale a sinistra, sotto l'ascella (anatomia dell'NPC).")]
        [SerializeField] private Transform zoneB;

        [Header("Aggancio")]
        [Tooltip("Distanza massima tra elettrodo e zona per consentire l'aggancio, in metri.")]
        [Min(0.01f)]
        [SerializeField] private float snapDistance = 0.10f;

        /// <summary>Cambiata posa visiva dell'NPC: utile alla futura logica di analisi.</summary>
        public event Action<ManDownPose> PoseChanged;

        private readonly ManDownAedElectrode[] occupied = new ManDownAedElectrode[2];
        private ManDownPose lastPose = ManDownPose.Supine;

        /// <summary>Numero di zone occupate (0, 1 o 2).</summary>
        public int AppliedCount => (occupied[0] != null ? 1 : 0) + (occupied[1] != null ? 1 : 0);
        /// <summary>true se entrambe le zone sono occupate.</summary>
        public bool BothOccupied => occupied[0] != null && occupied[1] != null;
        /// <summary>Posa visiva corrente dell'NPC.</summary>
        public ManDownPose CurrentPose => npc != null ? npc.CurrentVisualPose : ManDownPose.Supine;
        /// <summary>true solo se l'NPC è supino e il riferimento è collegato.</summary>
        public bool IsSupine => npc != null && npc.CurrentVisualPose == ManDownPose.Supine;
        /// <summary>Tolleranza di aggancio in metri (sola lettura, per i log diagnostici).</summary>
        public float SnapDistance => snapDistance;

        private void OnEnable()
        {
            MissionSession.Started += OnMissionStarted;
            lastPose = CurrentPose;
        }

        private void OnDisable()
        {
            MissionSession.Started -= OnMissionStarted;
        }

        private void OnDestroy()
        {
            PoseChanged = null;
        }

        private void Update()
        {
            ManDownPose pose = CurrentPose;
            if (pose == lastPose) return;
            lastPose = pose;
            PoseChanged?.Invoke(pose);
        }

        private void OnMissionStarted()
        {
            ResetZones();
            lastPose = CurrentPose;
        }

        /// <summary>
        /// Tenta l'aggancio sul rilascio: serve NPC supino e una zona libera entro la distanza
        /// configurata; se entrambe sono valide viene scelta la più vicina. L'orientamento
        /// dell'elettrodo non è un requisito.
        /// </summary>
        /// <returns>Indice della zona occupata, o -1 se non è stato possibile agganciare.</returns>
        public int TryAttach(ManDownAedElectrode electrode)
        {
            if (electrode == null || !IsSupine) return -1;

            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < 2; i++)
            {
                Transform zone = Zone(i);
                if (zone == null || (occupied[i] != null && occupied[i] != electrode)) continue;

                float distance = Vector3.Distance(electrode.transform.position, zone.position);
                if (distance > snapDistance || distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            if (best < 0) return -1;

            occupied[best] = electrode;
            Transform target = Zone(best);
            electrode.transform.SetPositionAndRotation(target.position, target.rotation);
            return best;
        }

        /// <summary>Libera la zona occupata dall'elettrodo indicato, se presente.</summary>
        public void Detach(ManDownAedElectrode electrode)
        {
            if (electrode == null) return;
            for (int i = 0; i < 2; i++)
            {
                if (occupied[i] == electrode) occupied[i] = null;
            }
        }

        /// <summary>Svuota entrambe le zone senza muovere gli elettrodi (usato dal reset).</summary>
        public void ResetZones()
        {
            occupied[0] = null;
            occupied[1] = null;
            lastPose = CurrentPose;
        }

        /// <summary>Trasformazione finale della zona 0 (A) o 1 (B).</summary>
        public Transform Zone(int index)
        {
            return index == 0 ? zoneA : zoneB;
        }

        public bool IsOccupied(int index)
        {
            return index >= 0 && index < 2 && occupied[index] != null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < 2; i++)
            {
                Transform zone = Zone(i);
                if (zone == null) continue;
                Gizmos.color = i == 0 ? new Color(0.2f, 0.6f, 1f) : new Color(0.2f, 0.9f, 0.7f);
                Gizmos.DrawWireCube(zone.position, new Vector3(0.10f, 0.02f, 0.15f));
                Gizmos.DrawWireSphere(zone.position, snapDistance);
                Gizmos.DrawLine(zone.position, zone.position + zone.up * 0.12f);
            }
        }
#endif
    }
}
