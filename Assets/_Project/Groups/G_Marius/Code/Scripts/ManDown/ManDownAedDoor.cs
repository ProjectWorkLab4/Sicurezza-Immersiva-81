using System;
using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Apertura manuale dello sportello della scatola DAE. Usa solo il contratto condiviso: GRIP su un
    /// <see cref="Interactable"/> con <c>FollowsHand</c> disattivato e posa del controller letto tramite
    /// <see cref="RigRegistration.TryGetAimPose"/>. Nessun HingeJoint, Rigidbody, input locale o nuovo sistema
    /// di grab. Lo sportello ruota soltanto attorno all'asse della cerniera, entro un intervallo configurabile,
    /// e conserva l'angolo raggiunto al rilascio: un nuovo GRIP permette di aprire o chiudere di nuovo.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/AED Door")]
    public sealed class ManDownAedDoor : MonoBehaviour
    {
        /// <summary>Quadrato della distanza minima dal piano della cerniera, sotto la quale il movimento è ignorato.</summary>
        private const float MIN_REFERENCE_SQR = 0.0004f; // 2 cm

        [Header("Riferimenti di interazione")]
        [Tooltip("Interactable sulla maniglia. Deve avere FollowsHand disattivato e Selectable disattivato.")]
        [SerializeField] private Interactable handleInteraction;

        [Header("Cerniera e apertura")]
        [Tooltip("Asse locale attorno al quale ruota lo sportello (default Y = verticale).")]
        [SerializeField] private Vector3 hingeAxis = Vector3.up;
        [Tooltip("Se attivo inverte il verso di apertura, senza cambiare l'asse.")]
        [SerializeField] private bool invertOpeningDirection;
        [Tooltip("Angolo minimo della rotazione in gradi (default 0 = chiuso).")]
        [SerializeField] private float minAngle = 0f;
        [Tooltip("Angolo massimo della rotazione in gradi (default 110).")]
        [SerializeField] private float maxAngle = 110f;
        [Tooltip("Angolo minimo necessario per il futuro recupero del DAE in gradi (default 70).")]
        [SerializeField] private float retrievalAngle = 70f;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        /// <summary>Cambia stato quando lo sportello viene afferrato (true) o rilasciato (false).</summary>
        public event Action<bool> GrabStateChanged;

        private Hand grabbingHand = Hand.Any;
        private float currentAngle;
        private float grabStartAngle;
        private Vector3 grabReferenceDirection;
        private Vector3 hingeWorldPosition;
        private Vector3 hingeWorldAxis;
        private bool interactionEnabled = true;

        /// <summary>Angolo corrente dello sportello in gradi.</summary>
        public float CurrentAngle => currentAngle;
        /// <summary>true mentre una mano sta ruotando lo sportello.</summary>
        public bool IsGrabbed => grabbingHand.IsSingle();
        /// <summary>true quando l'apertura è sufficiente per il futuro recupero del dispositivo.</summary>
        public bool IsOpenEnoughForRetrieval => Mathf.Abs(currentAngle) >= Mathf.Abs(retrievalAngle);

        private void OnEnable()
        {
            if (handleInteraction != null)
            {
                handleInteraction.GrabEntered += OnGrabEntered;
                handleInteraction.GrabExited += OnGrabExited;
            }
            currentAngle = ClampAngle(minAngle);
            ApplyAngle();
        }

        private void OnDisable()
        {
            if (handleInteraction != null)
            {
                handleInteraction.GrabEntered -= OnGrabEntered;
                handleInteraction.GrabExited -= OnGrabExited;
            }
            ClearGrab();
        }

        private void Update()
        {
            if (!interactionEnabled || !grabbingHand.IsSingle()) return;

            RigRegistration rig = RigRegistration.Instance;
            if (rig == null || !rig.TryGetAimPose(grabbingHand, out Vector3 controllerPosition, out _)) return;

            // Il controller descrive un arco attorno alla cerniera: l'angolo cercato è la differenza
            // tra la direzione iniziale e quella attuale, proiettate sul piano perpendicolare all'asse.
            Vector3 projected = Vector3.ProjectOnPlane(controllerPosition - hingeWorldPosition, hingeWorldAxis);
            float delta = DeltaAngle(grabReferenceDirection, projected, hingeWorldAxis, invertOpeningDirection);
            if (Mathf.Approximately(delta, 0f)) return;
            SetAngle(grabStartAngle + delta);
        }

        /// <summary>
        /// Variazione angolare della maniglia, calcolata solo dalle direzioni proiettate sul piano della
        /// cerniera. Funzione pura: testabile senza visore e senza simulazioni fisiche.
        /// </summary>
        /// <param name="referenceDirection">Direzione dal perno al controller all'inizio della presa.</param>
        /// <param name="currentDirection">Direzione dal perno al controller nel frame corrente.</param>
        /// <param name="hingeAxisWorld">Asse della cerniera in spazio mondo.</param>
        /// <param name="invert">Se true inverte il verso di apertura.</param>
        /// <returns>Gradi di rotazione da sommare all'angolo di partenza; 0 con direzioni troppo corte.</returns>
        public static float DeltaAngle(Vector3 referenceDirection, Vector3 currentDirection, Vector3 hingeAxisWorld, bool invert)
        {
            if (referenceDirection.sqrMagnitude < MIN_REFERENCE_SQR || currentDirection.sqrMagnitude < MIN_REFERENCE_SQR) return 0f;
            float delta = Vector3.SignedAngle(referenceDirection.normalized, currentDirection.normalized, hingeAxisWorld.normalized);
            return invert ? -delta : delta;
        }

        /// <summary>Abilita o disabilita la presa sulla maniglia, senza muovere lo sportello.</summary>
        public void SetInteractionEnabled(bool value)
        {
            interactionEnabled = value;
            if (handleInteraction != null) handleInteraction.Grabbable = value;
            if (!value) ClearGrab();
        }

        /// <summary>Riporta lo sportello all'angolo minimo e interrompe un'eventuale presa in corso.</summary>
        [ContextMenu("SI81/AED Door/Reset")]
        public void ResetDoor()
        {
            ClearGrab();
            SetAngle(ClampAngle(minAngle));
            if (debugLog) Debug.Log($"[SI81 ManDown] Sportello DAE ripristinato a {currentAngle:F0}°.", this);
        }

        private void OnGrabEntered(Hand hand)
        {
            if (!interactionEnabled || !hand.IsSingle() || grabbingHand.IsSingle()) return;

            RigRegistration rig = RigRegistration.Instance;
            if (rig == null || !rig.TryGetAimPose(hand, out Vector3 controllerPosition, out _)) return;

            hingeWorldPosition = transform.position;
            hingeWorldAxis = transform.TransformDirection(hingeAxis.normalized).normalized;
            Vector3 projected = Vector3.ProjectOnPlane(controllerPosition - hingeWorldPosition, hingeWorldAxis);
            if (projected.sqrMagnitude < MIN_REFERENCE_SQR) return;

            grabReferenceDirection = projected.normalized;
            grabStartAngle = currentAngle;
            grabbingHand = hand;
            GrabStateChanged?.Invoke(true);
            if (debugLog) Debug.Log($"[SI81 ManDown] Sportello DAE afferrato da {hand}.", this);
        }

        private void OnGrabExited(Hand hand)
        {
            if (!hand.IsSingle() || grabbingHand != hand) return;
            ClearGrab();
            if (debugLog) Debug.Log($"[SI81 ManDown] Sportello DAE rilasciato a {currentAngle:F0}°.", this);
        }

        private void ClearGrab()
        {
            if (!grabbingHand.IsSingle()) return;
            grabbingHand = Hand.Any;
            GrabStateChanged?.Invoke(false);
        }

        private void SetAngle(float desired)
        {
            float clamped = ClampAngle(desired);
            if (Mathf.Approximately(clamped, currentAngle)) return;
            currentAngle = clamped;
            ApplyAngle();
        }

        private void ApplyAngle()
        {
            transform.localRotation = Quaternion.AngleAxis(currentAngle, hingeAxis.normalized);
        }

        private float ClampAngle(float angle)
        {
            float low = Mathf.Min(minAngle, maxAngle);
            float high = Mathf.Max(minAngle, maxAngle);
            return Mathf.Clamp(angle, low, high);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 axis = transform.TransformDirection(hingeAxis.normalized);
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawLine(transform.position - axis * 0.12f, transform.position + axis * 0.12f);
            Gizmos.DrawWireSphere(transform.position, 0.025f);
        }
#endif
    }
}
