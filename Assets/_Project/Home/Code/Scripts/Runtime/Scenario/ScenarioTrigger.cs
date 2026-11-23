using UnityEngine;
using UnityEngine.Events;

namespace SI81.UI
{
    /// <summary>
    /// Innesco di esempio per gli scenari, senza fisica: scatta quando la testa del giocatore entra nel raggio indicato.
    /// Collega il UnityEvent a un metodo pubblico di ScenarioController (es. SetObjective, CompleteObjective, Subtitle) oppure del
    /// tuo script di scenario. La testa è quella di MNG_UI (UIScreenManager.Head) o Camera.main. Un controllo ogni 0,1 s.
    /// Uso: 1) aggiungilo a un oggetto vuoto nel punto voluto; 2) imposta il raggio; 3) aggiungi le azioni a "Alla entrata".
    /// </summary>
    [AddComponentMenu("SI81/Scenari/Scenario Trigger")]
    public class ScenarioTrigger : MonoBehaviour
    {
        /// <summary>Intervallo tra due controlli della distanza, in secondi.</summary>
        private const float CHECK_INTERVAL = 0.1f;

        [Tooltip("Distanza in metri dalla testa entro cui l'innesco scatta.")]
        [SerializeField] private float radius = 1.5f;
        [Tooltip("true = scatta una sola volta.")]
        [SerializeField] private bool once = true;
        [Tooltip("Azioni da eseguire quando il giocatore entra nel raggio.")]
        [SerializeField] private UnityEvent onEntered = new UnityEvent();

        /// <summary>Evento di ingresso (usato dai builder Editor per collegare azioni di esempio).</summary>
        internal UnityEvent OnEntered => onEntered;

        // Tempo al prossimo controllo e stato.
        private float nextCheck;
        private bool inside;
        private bool fired;

        /// <summary>
        /// Controlla a intervalli la distanza dalla testa e lancia l'evento all'ingresso nel raggio.
        /// </summary>
        private void Update()
        {
            if (Time.unscaledTime < nextCheck || (once && fired)) return;
            nextCheck = Time.unscaledTime + CHECK_INTERVAL;

            Transform head = UIScreenManager.Instance != null ? UIScreenManager.Instance.Head : null;
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (head == null) return;

            bool now = (head.position - transform.position).sqrMagnitude <= radius * radius;
            if (now && !inside)
            {
                fired = true;
                onEntered.Invoke();
            }
            inside = now;
        }

        /// <summary>
        /// Disegna il raggio nella Scene view.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.29f, 0.87f, 0.5f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
