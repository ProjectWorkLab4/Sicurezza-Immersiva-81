using System;
using UnityEngine;
using UnityEngine.Events;

namespace SI81.Input
{
    /// <summary>
    /// Bridge opzionale per l'inspector: inoltra gli eventi dell'Input Service a UnityEvent, senza polling né lavoro
    /// per frame. Gli eventi senza argomento coprono Select/Grab/Back/Menu; Move/Turn portano il valore Vector2.
    /// Se il servizio non esiste all'OnEnable, riprova in Start.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Input/Input Unity Event Bridge")]
    public sealed class InputUnityEventBridge : MonoBehaviour
    {
        [Tooltip("Servizio da ascoltare; vuoto = InputService.Instance.")]
        [SerializeField] private InputService service;
        [Tooltip("Select premuto (una qualsiasi mano).")]
        [SerializeField] private UnityEvent selectPerformed = new UnityEvent();
        [Tooltip("Select rilasciato/annullato (una qualsiasi mano).")]
        [SerializeField] private UnityEvent selectCanceled = new UnityEvent();
        [Tooltip("Grab premuto (una qualsiasi mano).")]
        [SerializeField] private UnityEvent grabPerformed = new UnityEvent();
        [Tooltip("Grab rilasciato/annullato (una qualsiasi mano).")]
        [SerializeField] private UnityEvent grabCanceled = new UnityEvent();
        [Tooltip("Tasto indietro premuto.")]
        [SerializeField] private UnityEvent backPerformed = new UnityEvent();
        [Tooltip("Tasto menu premuto.")]
        [SerializeField] private UnityEvent menuPerformed = new UnityEvent();
        [Tooltip("Stick sinistro cambiato.")]
        [SerializeField] private Vector2Event moveChanged = new Vector2Event();
        [Tooltip("Stick destro cambiato.")]
        [SerializeField] private Vector2Event turnChanged = new Vector2Event();

        private InputService subscribed;

        /// <summary>
        /// Si iscrive al servizio (esplicito o l'istanza statica).
        /// </summary>
        private void OnEnable()
        {
            Subscribe(service != null ? service : InputService.Instance);
        }

        /// <summary>
        /// Riprova l'iscrizione se il servizio non era ancora pronto in OnEnable.
        /// </summary>
        private void Start()
        {
            if (subscribed == null) Subscribe(service != null ? service : InputService.Instance);
        }

        /// <summary>
        /// Si disiscrive.
        /// </summary>
        private void OnDisable()
        {
            Subscribe(null);
        }

        /// <summary>
        /// Cambia il servizio ascoltato, scollegando il precedente.
        /// </summary>
        /// <param name="next">Nuovo servizio, o null per scollegare.</param>
        private void Subscribe(InputService next)
        {
            if (subscribed == next) return;
            if (subscribed != null)
            {
                subscribed.SelectPerformedAnyHand -= HandleSelectPerformed;
                subscribed.SelectCanceledAnyHand -= HandleSelectCanceled;
                subscribed.GrabPerformedAnyHand -= HandleGrabPerformed;
                subscribed.GrabCanceledAnyHand -= HandleGrabCanceled;
                subscribed.BackPerformed -= HandleBackPerformed;
                subscribed.MenuPerformed -= HandleMenuPerformed;
                subscribed.MoveChanged -= HandleMoveChanged;
                subscribed.TurnChanged -= HandleTurnChanged;
            }

            subscribed = next;
            if (subscribed == null) return;
            subscribed.SelectPerformedAnyHand += HandleSelectPerformed;
            subscribed.SelectCanceledAnyHand += HandleSelectCanceled;
            subscribed.GrabPerformedAnyHand += HandleGrabPerformed;
            subscribed.GrabCanceledAnyHand += HandleGrabCanceled;
            subscribed.BackPerformed += HandleBackPerformed;
            subscribed.MenuPerformed += HandleMenuPerformed;
            subscribed.MoveChanged += HandleMoveChanged;
            subscribed.TurnChanged += HandleTurnChanged;
        }

        /// <summary>Inoltra Select premuto.</summary>
        private void HandleSelectPerformed() => selectPerformed.Invoke();
        /// <summary>Inoltra Select annullato.</summary>
        private void HandleSelectCanceled() => selectCanceled.Invoke();
        /// <summary>Inoltra Grab premuto.</summary>
        private void HandleGrabPerformed() => grabPerformed.Invoke();
        /// <summary>Inoltra Grab annullato.</summary>
        private void HandleGrabCanceled() => grabCanceled.Invoke();
        /// <summary>Inoltra Back premuto.</summary>
        private void HandleBackPerformed(Hand hand) => backPerformed.Invoke();
        /// <summary>Inoltra Menu premuto.</summary>
        private void HandleMenuPerformed(Hand hand) => menuPerformed.Invoke();
        /// <summary>Inoltra Move cambiato.</summary>
        private void HandleMoveChanged(Hand hand, Vector2 value) => moveChanged.Invoke(value);
        /// <summary>Inoltra Turn cambiato.</summary>
        private void HandleTurnChanged(Hand hand, Vector2 value) => turnChanged.Invoke(value);

        /// <summary>
        /// UnityEvent con un Vector2, serializzabile nell'inspector.
        /// </summary>
        [Serializable]
        public sealed class Vector2Event : UnityEvent<Vector2>
        {
        }
    }
}
