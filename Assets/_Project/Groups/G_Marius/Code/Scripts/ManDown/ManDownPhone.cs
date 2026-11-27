using System;
using SI81.Rig;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Telefono della chiamata simulata al 112. Lo stato della chiamata è globale e autorevole in
    /// <see cref="ManDownScenario.IsCall112Made"/>: tutti i telefoni lo leggono, quindi dopo un "Sì" nessuno
    /// apre più il popup. Il componente si occupa solo della sequenza richiesta/risposta: non conosce input VR,
    /// popup, UI, scoring né la scena. Una richiesta solleva <see cref="InteractionRequested"/>: il ponte
    /// <see cref="ManDownPhoneUi"/> apre il popup comune e richiama <see cref="AnswerYes"/> oppure <see cref="AnswerNo"/>.
    /// Solo "Sì" porta allo stato <see cref="CallState.Made"/>; "No" lascia tutti i telefoni disponibili per un
    /// nuovo tentativo. A ogni nuova partita <see cref="ResetPhone"/> ripristina stato, posa e fisica: si aggancia
    /// all'evento comune <see cref="MissionSession.Started"/>, quindi funziona anche quando la scena
    /// di scenario non viene scaricata al ritorno dalla Home. Ignora selettivamente le sole collisioni fisiche
    /// con i collider del corpo del rig condiviso, senza toccare la collision matrix globale.
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

        [Header("Riferimenti di scena")]
        [Tooltip("Controller della missione: è la fonte autorevole dello stato globale della chiamata.")]
        [SerializeField] private ManDownScenario scenario;

        /// <summary>Richiesta di interazione: il ricevente aprirà il popup decisionale.</summary>
        public event Action InteractionRequested;

        /// <summary>Risposta "Sì": la chiamata è stata registrata.</summary>
        public event Action CallConfirmed;

        /// <summary>Risposta "No": nessuna registrazione, il telefono resta utilizzabile.</summary>
        public event Action CallDeclined;

        /// <summary>
        /// Stato corrente della chiamata, derivato dallo stato globale di <see cref="ManDownScenario"/>.
        /// Tutti i telefoni condividono quindi lo stesso esito: dopo un "Sì" nessuno è più disponibile.
        /// </summary>
        public CallState CurrentCallState => IsCallMade ? CallState.Made : CallState.NotMade;

        /// <summary>
        /// True se la chiamata è già stata registrata. Lo stato autorevole è <see cref="ManDownScenario.IsCall112Made"/>;
        /// il flag locale resta solo come ripiego quando il riferimento allo scenario non è collegato.
        /// </summary>
        public bool IsCallMade => (scenario != null && scenario.IsCall112Made) || callMadeLocally;

        /// <summary>True se è stata emessa una richiesta in attesa di risposta.</summary>
        public bool IsAwaitingResponse { get; private set; }

        // Posa originale catturata al caricamento della scena e ripristinata a ogni nuova missione.
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Rigidbody body;
        private bool poseCaptured;

        // Ripiego locale: usato solo se il riferimento allo scenario non è collegato.
        private bool callMadeLocally;

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
            callMadeLocally = true;
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
            RigRegistration.Registered += OnRigRegistered;
            IgnorePlayerBodyCollisions();
        }

        /// <summary>Si scollega dall'evento di sessione quando il componente si disattiva.</summary>
        private void OnDisable()
        {
            MissionSession.Started -= OnMissionStarted;
            RigRegistration.Registered -= OnRigRegistered;
        }

        /// <summary>
        /// Applica l'ignore selettivo se il rig condiviso si registra dopo il telefono (scene caricate in additivo).
        /// </summary>
        private void OnRigRegistered(RigRegistration rig)
        {
            if (rig != null) IgnorePlayerBodyCollisions(rig);
        }

        /// <summary>
        /// Ignora solo le collisioni fisiche tra i collider di questo telefono e quelli del corpo del giocatore.
        /// Raycast, GRIP, rilascio, gravità e collisioni con ambiente/oggetti restano invariati; non si tocca la
        /// collision matrix globale, quindi la soluzione è locale allo scenario e compatibile con il rig condiviso.
        /// </summary>
        private void IgnorePlayerBodyCollisions()
        {
            IgnorePlayerBodyCollisions(RigRegistration.Instance);
        }

        /// <summary>Versione con rig esplicito, usata anche al momento della registrazione tardiva del rig.</summary>
        private void IgnorePlayerBodyCollisions(RigRegistration rig)
        {
            if (rig == null) return;

            Collider[] playerColliders = rig.GetComponentsInChildren<Collider>(true);
            if (playerColliders.Length == 0) return;

            Collider[] phoneColliders = GetComponents<Collider>();
            for (int i = 0; i < phoneColliders.Length; i++)
            {
                if (phoneColliders[i] == null) continue;
                for (int j = 0; j < playerColliders.Length; j++)
                {
                    if (playerColliders[j] != null)
                        Physics.IgnoreCollision(phoneColliders[i], playerColliders[j], true);
                }
            }
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

            callMadeLocally = false;
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
