using SI81.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.Rig
{
    /// <summary>
    /// Locomozione continua del Player Rig: stick sinistro relativo allo sguardo, snap turn da stick destro,
    /// gravità e contatto col pavimento tramite CharacterController. Muove solo la radice del rig, mai la camera.
    /// Si blocca quando il canale World è sospeso (menu/pausa), quando <see cref="MovementLock"/> è attivo o Time.timeScale è zero.
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
        private const string CORE_SCENE_NAME = "SCN_Core";

        [Header("Movimento")]
        [Tooltip("Se attivo, blocca movimento e rotazione e disabilita le relative InputAction (es. in SCN_Core).")]
        [SerializeField] private bool movementLock;
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
        private bool coreMovementLock;
        private bool managesCoreSceneLock;
        private bool appliedLocomotionLock;

        /// <summary>Se true, blocca movimento e rotazione a scatti e disabilita le relative InputAction.</summary>
        public bool MovementLock
        {
            get => movementLock;
            set
            {
                if (movementLock == value)
                {
                    SyncLocomotionInput();
                    return;
                }
                movementLock = value;
                if (!value) turnArmed = true;
                SyncLocomotionInput();
            }
        }

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
            managesCoreSceneLock = gameObject.scene.IsValid() && gameObject.scene.name == CORE_SCENE_NAME;
            coreMovementLock = movementLock;
        }

        /// <summary>
        /// Azzera la velocità verticale e allinea le InputAction di locomozione quando il rig viene riattivato.
        /// </summary>
        private void OnEnable()
        {
            verticalVelocity = 0f;
            turnArmed = true;
            if (managesCoreSceneLock)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                SceneManager.sceneUnloaded += HandleSceneUnloaded;
            }
            UpdateSceneMovementLock();
            SyncLocomotionInput();
        }

        /// <summary>
        /// Riallinea il blocco movimento dopo la registrazione del rig attivo.
        /// </summary>
        private void Start()
        {
            UpdateSceneMovementLock();
            SyncLocomotionInput();
        }

        /// <summary>
        /// Rimuove i listener di scena e rilascia l'eventuale blocco sulle InputAction.
        /// </summary>
        private void OnDisable()
        {
            if (managesCoreSceneLock)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            }
            if (appliedLocomotionLock && (RigRegistration.Instance == null || RigRegistration.Instance == rig))
            {
                appliedLocomotionLock = false;
                InputService service = InputService.Instance;
                if (service != null) service.SetLocomotionEnabled(true);
            }
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

            bool blocked = movementLock || service.IsSuppressed(InputChannel.World);
            if (!blocked && snapTurnEnabled) ApplySnapTurn(service.ReadTurn());

            Vector3 velocity = Vector3.zero;
            if (!blocked) velocity = ComputeMoveVelocity(service.ReadMove());
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
        /// Mantiene le soglie coerenti nell'inspector e aggiorna il lock in Play Mode.
        /// </summary>
        private void OnValidate()
        {
            deadzone = Mathf.Clamp(deadzone, 0f, 0.9f);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            snapTurnDegrees = Mathf.Max(0f, snapTurnDegrees);
            turnActivateThreshold = Mathf.Clamp(turnActivateThreshold, 0.1f, 1f);
            turnRearmThreshold = Mathf.Clamp(turnRearmThreshold, 0f, turnActivateThreshold - 0.01f);
            if (Application.isPlaying) SyncLocomotionInput();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateSceneMovementLock();
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            UpdateSceneMovementLock();
        }

        /// <summary>
        /// In SCN_Core: sblocca la locomozione quando è caricato uno scenario diverso da SCN_Core e la riblocca al ritorno.
        /// </summary>
        private void UpdateSceneMovementLock()
        {
            if (!managesCoreSceneLock) return;
            if (RigRegistration.Instance != null && rig != RigRegistration.Instance) return;
            MovementLock = HasLoadedScenarioScene() ? false : coreMovementLock;
        }

        /// <summary>
        /// Sincronizza lo stato di abilitazione delle InputAction Move/Turn con <see cref="movementLock"/>.
        /// </summary>
        private void SyncLocomotionInput()
        {
            if (!Application.isPlaying) return;
            if (RigRegistration.Instance != null && rig != RigRegistration.Instance) return;
            InputService service = InputService.Instance;
            if (service == null) return;

            bool shouldEnable = !isActiveAndEnabled || !movementLock;
            service.SetLocomotionEnabled(shouldEnable);
            appliedLocomotionLock = !shouldEnable;
        }

        private static bool HasLoadedScenarioScene()
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (scene.name == CORE_SCENE_NAME) continue;
                if (scene.name.StartsWith("InitTestScene", System.StringComparison.Ordinal)) continue;
                return true;
            }
            return false;
        }
    }
}
