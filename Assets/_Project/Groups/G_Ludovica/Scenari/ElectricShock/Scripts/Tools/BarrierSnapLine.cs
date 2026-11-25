using System;
using System.Collections;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] BL-10: linea di barriera sul varco DR-03 con due slot (sostituto della snap zone dello scenario 07).
    /// La zona è isolata quando tutti gli slot sono occupati.
    /// </summary>
    public class BarrierSnapLine : MonoBehaviour
    {
        [Tooltip("Slot della linea (posizione e rotazione della barriera agganciata).")]
        [SerializeField] private Transform[] slots;
        [Tooltip("Barriere che possono agganciarsi.")]
        [SerializeField] private BarrierPanel[] barriers;
        [Tooltip("Distanza massima di aggancio (m).")]
        [SerializeField] private float snapDistance = 1.2f;
        [Tooltip("Indicatori visivi degli slot liberi (facoltativi).")]
        [SerializeField] private GameObject[] slotMarkers;
        [Tooltip("Secondi in cui le barriere restano aperte per far passare un NPC autorizzato.")]
        [SerializeField] private float passageOpenSeconds = 3f;
        [Tooltip("Mezza larghezza del varco lungo la linea (m).")]
        [SerializeField] private float passageHalfWidth = 2.3f;
        [Tooltip("Mezza larghezza di una barriera (m): la cerniera dell'apertura sta all'estremo (MESH_Magazzino_Barriera = 0,75 m).")]
        [SerializeField] private float panelHalfWidth = 0.375f;

        private BarrierPanel[] occupants;
        private float passageOpenUntil = -1f;
        private bool passageFullyOpen;

        /// <summary>true se tutti gli slot sono occupati.</summary>
        public bool IsClosed
        {
            get
            {
                if (occupants == null || occupants.Length == 0) return false;
                foreach (var o in occupants) if (o == null) return false;
                return true;
            }
        }

        /// <summary>Chiamato quando la linea cambia stato (chiusa / aperta).</summary>
        public event Action<bool> ClosedChanged;

        /// <summary>Collega le barriere alla linea.</summary>
        private void Awake()
        {
            occupants = new BarrierPanel[slots != null ? slots.Length : 0];
            if (barriers != null) foreach (var b in barriers) if (b != null) b.Line = this;
            RefreshMarkers();
        }

        /// <summary>Aggancia la barriera allo slot libero più vicino, se abbastanza vicino.</summary>
        /// <param name="barrier">Barriera lasciata.</param>
        /// <returns>true se agganciata.</returns>
        public bool TrySnap(BarrierPanel barrier)
        {
            int best = -1;
            float bestDist = snapDistance;
            Vector3 p = barrier.transform.position; p.y = 0f;
            for (int i = 0; i < slots.Length; i++)
            {
                if (occupants[i] != null) continue;
                Vector3 s = slots[i].position; s.y = 0f;
                float d = Vector3.Distance(p, s);
                if (d <= bestDist) { bestDist = d; best = i; }
            }
            if (best < 0) return false;
            Place(barrier, best);
            return true;
        }

        /// <summary>Aggancia la barriera a uno slot preciso (anche da script o test).</summary>
        /// <param name="barrier">Barriera.</param>
        /// <param name="slotIndex">Indice dello slot.</param>
        public void Place(BarrierPanel barrier, int slotIndex)
        {
            bool wasClosed = IsClosed;
            Detach(barrier, false);
            occupants[slotIndex] = barrier;
            barrier.SlotIndex = slotIndex;
            barrier.transform.SetPositionAndRotation(slots[slotIndex].position, slots[slotIndex].rotation);
            RefreshMarkers();
            if (IsClosed != wasClosed) ClosedChanged?.Invoke(IsClosed);
        }

        /// <summary>Libera lo slot occupato dalla barriera.</summary>
        /// <param name="barrier">Barriera.</param>
        public void Detach(BarrierPanel barrier) => Detach(barrier, true);

        /// <summary>Libera lo slot e, se richiesto, notifica il cambio di stato.</summary>
        private void Detach(BarrierPanel barrier, bool notify)
        {
            if (barrier.SlotIndex < 0) return;
            bool wasClosed = IsClosed;
            occupants[barrier.SlotIndex] = null;
            barrier.SlotIndex = -1;
            RefreshMarkers();
            if (notify && IsClosed != wasClosed) ClosedChanged?.Invoke(IsClosed);
        }

        /// <summary>true se il segmento orizzontale da "from" a "to" attraversa la linea mentre è chiusa.</summary>
        /// <param name="from">Punto di partenza.</param>
        /// <param name="to">Punto di arrivo.</param>
        public bool Blocks(Vector3 from, Vector3 to)
        {
            if (!IsClosed) return false;
            float x = transform.position.x;
            return (from.x - x) * (to.x - x) <= 0f && from.x != to.x;
        }

        /// <summary>
        /// Controllo di passo per gli NPC autorizzati (colleghi che escono, PM-2 quando la zona è sicura): se il passo arriva
        /// sulla linea chiusa, le barriere si aprono per qualche secondo e l'NPC aspetta che siano aperte.
        /// La linea resta "chiusa" per la logica (IsClosed non cambia).
        /// </summary>
        /// <param name="from">Posizione attuale.</param>
        /// <param name="to">Posizione dopo il passo.</param>
        /// <returns>true se il passo è permesso.</returns>
        public bool AllowPass(Vector3 from, Vector3 to)
        {
            if (!IsClosed) return true;
            Vector3 c = transform.position;
            bool onLine = Mathf.Abs(to.x - c.x) < 0.5f && Mathf.Abs(to.z - c.z) < passageHalfWidth;
            if (!onLine) return true;
            if (passageFullyOpen && Time.time < passageOpenUntil) return true;
            RequestPassage();
            return false;
        }

        /// <summary>Apre le barriere per qualche secondo (gli NPC autorizzati passano), poi le richiude.</summary>
        public void RequestPassage()
        {
            if (Time.time < passageOpenUntil) return;
            passageOpenUntil = Time.time + passageOpenSeconds + 1f;
            Debug.Log("[SI81 ElectricShock] Barriere aperte per far passare un NPC autorizzato.");
            StartCoroutine(PassageRoutine());
        }

        /// <summary>Animazione di apertura e chiusura: ogni barriera ruota di 80° attorno al suo estremo esterno.</summary>
        private IEnumerator PassageRoutine()
        {
            const float swing = 0.5f;
            for (float t = 0f; t < swing; t += Time.deltaTime) { ApplyOpening(t / swing); yield return null; }
            ApplyOpening(1f);
            passageFullyOpen = true;
            yield return new WaitForSeconds(passageOpenSeconds);
            passageFullyOpen = false;
            for (float t = 0f; t < swing; t += Time.deltaTime) { ApplyOpening(1f - t / swing); yield return null; }
            ApplyOpening(0f);
        }

        /// <summary>Posa delle barriere agganciate con apertura 0-1.</summary>
        private void ApplyOpening(float k)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var b = occupants[i];
                if (b == null || b.HeldBy != null) continue;
                Vector3 center = slots[i].position;
                Vector3 end = center + slots[i].right * panelHalfWidth;
                Vector3 other = center - slots[i].right * panelHalfWidth;
                Vector3 pivot = (end - transform.position).sqrMagnitude > (other - transform.position).sqrMagnitude ? end : other;
                Vector3 v = center - pivot;
                var rot = Quaternion.Euler(0f, 80f * k * Mathf.Sign(v.z == 0f ? 1f : v.z), 0f);
                b.transform.SetPositionAndRotation(pivot + rot * v, rot * slots[i].rotation);
            }
        }

        /// <summary>Mostra gli indicatori sugli slot liberi.</summary>
        private void RefreshMarkers()
        {
            if (slotMarkers == null || occupants == null) return;
            for (int i = 0; i < slotMarkers.Length && i < occupants.Length; i++)
                if (slotMarkers[i] != null) slotMarkers[i].SetActive(occupants[i] == null);
        }
    }
}
