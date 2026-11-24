using SI81.Input;
using UnityEngine;

namespace SI81.Rig
{
    /// <summary>
    /// Locomozione continua del Player Rig: stick sinistro relativo allo sguardo, snap turn da stick destro,
    /// gravità e contatto col pavimento tramite CharacterController. Muove solo la radice del rig, mai la camera.
    /// Si blocca quando il canale World è sospeso (menu/pausa) o Time.timeScale è zero.
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(RigRegistration))]
    [AddComponentMenu("SI81/Rig/Rig Locomotion")]
    public sealed class RigLocomotion : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: dopo l'Input Service, prima dei consumatori generici.</summary>
        public const int EXECUTION_ORDER = -900;

        [Header("Movimento")]
        [Tooltip("Zona morta radiale dello stick sinistro (0..0.9).")]
        [Range(0f, 0.9f)]
        [SerializeField] private float deadzone = 0.2f;
        [Tooltip("Velocità massima di movimento in m/s.")]
        [Min(0f)]
        [SerializeField] private float moveSpeed = 1.8f;

        [Header("Rotazione a scatti")]
        [Tooltip("Snap turn abilitato (45° di default).")]
        [SerializeField] private bool snapTurnEnabled = true;
        [Tooltip("Gradi per scatto.")]
        [Min(0f)]
        [SerializeField] private float snapTurnDegrees = 45f;
        [Tooltip("Soglia dello stick destro per far scattare la rotazione.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float turnActivateThreshold = 0.7f;
        [Tooltip("Soglia sotto la quale lo stick si riarma per il prossimo scatto.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float turnRearmThreshold = 0.3f;

        [Header("Gravità")]
        [Tooltip("Applica gravità e contatto col pavimento tramite CharacterController.")]
        [SerializeField] private bool gravityEnabled = true;
        [Tooltip("Accelerazione di gravità in m/s².")]
        [SerializeField] private float gravity = -9.81f;

        private CharacterController controller;
        private RigRegistration rig;
        private float verticalVelocity;
        private bool turnArmed = true;
        private bool warnedNoService;

        /// <summary>Zona morta radiale dello stick sinistro.</summary>
        public float Deadzone
        {
            get => deadzone;
            set => deadzone = Mathf.Clamp(value, 0f, 0.9f);
        }

        /// <summary>Velocità massima di movimento in m/s.</summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        /// <summary>Snap turn abilitato.</summary>
        public bool SnapTurnEnabled
        {
            get => snapTurnEnabled;
            set
            {
                snapTurnEnabled = value;
                if (value) turnArmed = true;
            }
        }

        /// <summary>Gradi per scatto.</summary>
        public float SnapTurnDegrees
        {
            get => snapTurnDegrees;
            set => snapTurnDegrees = Mathf.Max(0f, value);
        }

        /// <summary>Gravità e contatto col pavimento abilitati.</summary>
        public bool GravityEnabled
        {
            get => gravityEnabled;
            set => gravityEnabled = value;
        }

        /// <summary>
        /// Cache di CharacterController e RigRegistration.
        /// </summary>
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            rig = GetComponent<RigRegistration>();
        }

        /// <summary>
        /// Azzera la velocità verticale quando il rig viene riattivato.
        /// </summary>
        private void OnEnable()
        {
            verticalVelocity = 0f;
            turnArmed = true;
        }

        /// <summary>
        /// Muove e ruota la radice del rig una volta per frame.
        /// </summary>
        private void Update()
        {
            if (controller == null) return;
            if (rig == null || rig != RigRegistration.Instance) return;
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            InputService service = InputService.Instance;
            if (service == null)
            {
                if (!warnedNoService)
                {
                    warnedNoService = true;
                    Debug.LogWarning("[SI81 Rig] InputService assente: la locomozione resta ferma.", this);
                }
                return;
            }

            bool suppressed = service.IsSuppressed(InputChannel.World);
            if (!suppressed && snapTurnEnabled) ApplySnapTurn(service.ReadTurn());

            Vector3 velocity = Vector3.zero;
            if (!suppressed) velocity = ComputeMoveVelocity(service.ReadMove());
            ApplyGravity(deltaTime);
            controller.Move(velocity * deltaTime + Vector3.up * (verticalVelocity * deltaTime));
        }

        /// <summary>
        /// Converte lo stick sinistro in velocità mondo relativa allo yaw della testa.
        /// </summary>
        /// <param name="stick">Valore grezzo dello stick.</param>
        /// <returns>Velocità orizzontale in m/s.</returns>
        private Vector3 ComputeMoveVelocity(Vector2 stick)
        {
            stick = Vector2.ClampMagnitude(stick, 1f);
            float magnitude = stick.magnitude;
            if (magnitude <= deadzone) return Vector3.zero;
            float scaled = Mathf.Clamp01((magnitude - deadzone) / (1f - deadzone));
            Vector2 direction = stick / magnitude;

            Vector3 forward = rig != null && rig.Head != null ? rig.Head.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
                forward.y = 0f;
            }
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 worldDirection = right * direction.x + forward * direction.y;
            return worldDirection * (scaled * moveSpeed);
        }

        /// <summary>
        /// Applica uno scatto di 45° intorno alla testa quando lo stick destro supera la soglia; si riarma al rientro.
        /// </summary>
        /// <param name="turn">Valore grezzo dello stick destro.</param>
        private void ApplySnapTurn(Vector2 turn)
        {
            float amount = turn.x;
            if (turnArmed)
            {
                if (Mathf.Abs(amount) < turnActivateThreshold) return;
                Vector3 pivot = rig != null && rig.Head != null
                    ? rig.Head.position
                    : transform.position + Vector3.up * (controller.height * 0.5f);
                transform.RotateAround(pivot, Vector3.up, Mathf.Sign(amount) * snapTurnDegrees);
                turnArmed = false;
                return;
            }
            if (Mathf.Abs(amount) <= turnRearmThreshold) turnArmed = true;
        }

        /// <summary>
        /// Aggiorna la velocità verticale con la gravità e il contatto col pavimento.
        /// </summary>
        /// <param name="deltaTime">Tempo del frame.</param>
        private void ApplyGravity(float deltaTime)
        {
            if (!gravityEnabled)
            {
                verticalVelocity = 0f;
                return;
            }
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * deltaTime;
        }

        /// <summary>
        /// Azzera la velocità verticale (dopo un teleport/spawn).
        /// </summary>
        public void ResetVerticalVelocity()
        {
            verticalVelocity = 0f;
        }

        /// <summary>
        /// Mantiene le soglie coerenti nell'inspector.
        /// </summary>
        private void OnValidate()
        {
            deadzone = Mathf.Clamp(deadzone, 0f, 0.9f);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            snapTurnDegrees = Mathf.Max(0f, snapTurnDegrees);
            turnActivateThreshold = Mathf.Clamp(turnActivateThreshold, 0.1f, 1f);
            turnRearmThreshold = Mathf.Clamp(turnRearmThreshold, 0f, turnActivateThreshold - 0.01f);
        }
    }
}
