using System;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Telefono della chiamata simulata al 112. Si occupa soltanto dello stato della chiamata e della
    /// sequenza richiesta/risposta: non conosce input VR, popup, UI, scenario, scoring né la scena.
    /// Una richiesta solleva <see cref="InteractionRequested"/>: un componente futuro aprirà il popup
    /// comune e richiamerà <see cref="AnswerYes"/> oppure <see cref="AnswerNo"/>. Solo "Sì" porta allo
    /// stato <see cref="CallState.Made"/>; "No" lascia il telefono disponibile per una nuova interazione.
    /// A ogni nuova partita <see cref="ResetPhone"/> ripristina stato, posa e fisica: si aggancia
    /// all'evento comune <see cref="MissionSession.Started"/>, quindi funziona anche quando la scena
    /// di scenario non viene scaricata al ritorno dalla Home.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/Phone")]
    public sealed class ManDownPhone : MonoBehaviour
    {
        /// <summary>Stato della chiamata: non ancora effettuata oppure già registrata.</summary>
        public enum CallState
        {
            /// <summary>Nessuna chiamata registrata: il telefono è ancora utilizzabile.</summary>
            NotMade,
            /// <summary>Chiamata già registrata: ulteriori richieste e risposte vengono ignorate.</summary>
            Made
        }

        /// <summary>Richiesta di interazione: il ricevente aprirà il popup decisionale.</summary>
        public event Action InteractionRequested;

        /// <summary>Risposta "Sì": la chiamata è stata registrata.</summary>
        public event Action CallConfirmed;

        /// <summary>Risposta "No": nessuna registrazione, il telefono resta utilizzabile.</summary>
        public event Action CallDeclined;

        /// <summary>Stato corrente della chiamata.</summary>
        public CallState CurrentCallState { get; private set; }

        /// <summary>True se la chiamata è già stata registrata.</summary>
        public bool IsCallMade => CurrentCallState == CallState.Made;

        /// <summary>True se è stata emessa una richiesta in attesa di risposta.</summary>
        public bool IsAwaitingResponse { get; private set; }

        // Posa originale catturata al caricamento della scena e ripristinata a ogni nuova missione.
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Rigidbody body;
        private bool poseCaptured;

        /// <summary>
        /// Chiede di interagire con il telefono. Se non ci sono ascoltatori la richiesta non viene emessa:
        /// prima del collegamento del popup lo stato resta inalterato e si può riprovare.
        /// </summary>
        /// <returns>True se la richiesta è stata emessa.</returns>
        public bool RequestInteraction()
        {
            if (IsCallMade || IsAwaitingResponse || InteractionRequested == null) return false;

            IsAwaitingResponse = true;
            InteractionRequested.Invoke();
            return true;
        }

        /// <summary>
        /// Registra la risposta "Sì". Porta lo stato a <see cref="CallState.Made"/> una sola volta:
        /// ulteriori richieste o risposte positive vengono ignorate.
        /// </summary>
        /// <returns>True se la chiamata è stata registrata da questa chiamata.</returns>
        public bool AnswerYes()
        {
            if (IsCallMade) return false;

            IsAwaitingResponse = false;
            CurrentCallState = CallState.Made;
            CallConfirmed?.Invoke();
            return true;
        }

        /// <summary>
        /// Registra la risposta "No": chiude la richiesta senza cambiare lo stato, così il telefono
        /// può essere usato di nuovo in un secondo momento.
        /// </summary>
        /// <returns>True se la risposta è stata accettata.</returns>
        public bool AnswerNo()
        {
            if (IsCallMade) return false;

            IsAwaitingResponse = false;
            CallDeclined?.Invoke();
            return true;
        }

        /// <summary>
        /// Wrapper senza parametri e senza valore di ritorno, collegabile all'evento `GrabEntered`
        /// di un `Interactable` dall'Inspector. Delega a <see cref="RequestInteraction"/>.
        /// </summary>
        public void RequestCallFromInteraction()
        {
            RequestInteraction();
        }

        /// <summary>Cattura la posa originale e il Rigidbody prima di qualunque interazione.</summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            poseCaptured = true;
        }

        /// <summary>Si aggancia all'avvio di ogni missione per ripristinare posa, fisica e stato.</summary>
        private void OnEnable()
        {
            if (!poseCaptured || body == null)
            {
                body = GetComponent<Rigidbody>();
                initialPosition = transform.position;
                initialRotation = transform.rotation;
                poseCaptured = true;
            }
            MissionSession.Started += OnMissionStarted;
        }

        /// <summary>Si scollega dall'evento di sessione quando il componente si disattiva.</summary>
        private void OnDisable()
        {
            MissionSession.Started -= OnMissionStarted;
        }

        /// <summary>Ripristina stato, posa e fisica iniziali per una nuova partita.</summary>
        public void ResetPhone()
        {
            if (!poseCaptured || body == null)
            {
                body = GetComponent<Rigidbody>();
                initialPosition = transform.position;
                initialRotation = transform.rotation;
                poseCaptured = true;
            }

            CurrentCallState = CallState.NotMade;
            IsAwaitingResponse = false;
            transform.SetPositionAndRotation(initialPosition, initialRotation);

            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>Ripristina il telefono quando comincia una nuova missione.</summary>
        private void OnMissionStarted()
        {
            ResetPhone();
        }

        /// <summary>Rilascia gli ascoltatori per non trattenere riferimenti alla partita precedente.</summary>
        private void OnDestroy()
        {
            MissionSession.Started -= OnMissionStarted;
            InteractionRequested = null;
            CallConfirmed = null;
            CallDeclined = null;
        }
    }
}
