using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Elemento della scena che il giocatore deve osservare (cavo, pozza, persone): è "osservato" quando resta
    /// nel campo visivo centrale, in vista diretta, per un breve tempo.
    /// </summary>
    public class ObservableHazard : MonoBehaviour
    {
        [Tooltip("Identificativo usato dallo scenario (cable, pool, people).")]
        [SerializeField] private string hazardId = "pool";
        [Tooltip("Distanza massima di osservazione (m).")]
        [SerializeField] private float maxDistance = 12f;
        [Tooltip("Mezzo angolo del cono visivo (gradi).")]
        [SerializeField] private float viewAngle = 25f;
        [Tooltip("Secondi di sguardo necessari.")]
        [SerializeField] private float dwellSeconds = 0.75f;

        private float dwell;

        /// <summary>Identificativo dell'elemento.</summary>
        public string HazardId => hazardId;
        /// <summary>true dopo la prima osservazione.</summary>
        public bool IsObserved { get; private set; }
        /// <summary>Secondi di scenario della prima osservazione.</summary>
        public float ObservedAt { get; private set; } = -1f;

        /// <summary>Aggiorna l'osservazione con la posa della testa.</summary>
        /// <param name="head">Testa del giocatore.</param>
        /// <param name="scenarioTime">Secondi dall'inizio dello scenario.</param>
        /// <returns>true nel frame in cui diventa osservato.</returns>
        public bool Tick(Transform head, float scenarioTime)
        {
            if (IsObserved || head == null) return false;
            Vector3 to = transform.position - head.position;
            bool visible = to.magnitude <= maxDistance && Vector3.Angle(head.forward, to) <= viewAngle && HasLineOfSight(head.position);
            dwell = visible ? dwell + Time.deltaTime : 0f;
            if (dwell < dwellSeconds) return false;
            IsObserved = true;
            ObservedAt = scenarioTime;
            return true;
        }

        /// <summary>true se nulla di estraneo si trova tra la testa e l'elemento.</summary>
        private bool HasLineOfSight(Vector3 eye)
        {
            if (!Physics.Linecast(eye, transform.position, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform.IsChildOf(transform) || (hit.point - transform.position).sqrMagnitude < 0.25f;
        }
    }
}
