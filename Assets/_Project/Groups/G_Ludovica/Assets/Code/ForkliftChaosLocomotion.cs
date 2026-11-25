using SI81.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>
    /// Locomozione minima per lo scenario 07: muove l'origine del tracciamento del rig condiviso (MNG_CoreRig), senza
    /// toccare gli asset del Common Core. In Editor: WASD/frecce + Q/E (o mouse con tasto destro premuto), Shift per correre.
    /// In VR: stick sinistro per camminare, stick destro per svoltare a scatti. La testa del rig resta tracciata dal sistema XR.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Scenari/Forklift Chaos/Locomozione")]
    public sealed class ForkliftChaosLocomotion : MonoBehaviour
    {
        [Header("Velocità")]
        [SerializeField] private float walkSpeed = 2.1f;
        [SerializeField] private float sprintMultiplier = 1.9f;
        [SerializeField] private float keyboardTurnSpeed = 90f;
        [SerializeField] private float vrSpeed = 2.1f;
        [SerializeField] private float vrSnapDegrees = 30f;
        [SerializeField] private float stickDeadzone = 0.25f;

        private Transform origin;
        private Transform head;
        private InputAction moveAction;
        private InputAction turnAction;
        private bool snapArmed = true;

        private void OnEnable()
        {
            moveAction = new InputAction("FKC_Move", InputActionType.Value);
            moveAction.AddBinding("<XRController>{LeftHand}/primary2DAxis");
            moveAction.Enable();

            turnAction = new InputAction("FKC_Turn", InputActionType.Value);
            turnAction.AddBinding("<XRController>{RightHand}/primary2DAxis");
            turnAction.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null) { moveAction.Disable(); moveAction.Dispose(); moveAction = null; }
            if (turnAction != null) { turnAction.Disable(); turnAction.Dispose(); turnAction = null; }
        }

        private void Update()
        {
            if (origin == null)
            {
                origin = UIScreenManager.Instance != null ? UIScreenManager.Instance.TrackingOrigin : null;
                if (origin == null && Camera.main != null) origin = Camera.main.transform.parent;
            }
            if (origin == null) return;

            if (head == null)
                head = UIScreenManager.Instance != null ? UIScreenManager.Instance.Head : null;
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (head == null) head = origin;

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;

            ApplyVr(forward, right);
            ApplyKeyboard(forward, right);
        }

        private void ApplyVr(Vector3 forward, Vector3 right)
        {
            if (moveAction != null)
            {
                Vector2 axis = moveAction.ReadValue<Vector2>();
                if (axis.sqrMagnitude > stickDeadzone * stickDeadzone)
                {
                    Vector3 step = (forward * axis.y + right * axis.x) * (vrSpeed * Time.deltaTime);
                    origin.position += step;
                }
            }

            if (turnAction == null) return;
            float x = turnAction.ReadValue<Vector2>().x;
            if (Mathf.Abs(x) < stickDeadzone * 0.5f)
            {
                snapArmed = true;
                return;
            }
            if (!snapArmed) return;
            snapArmed = false;
            origin.Rotate(0f, x > 0f ? vrSnapDegrees : -vrSnapDegrees, 0f);
        }

        private void ApplyKeyboard(Vector3 forward, Vector3 right)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            Vector3 dir = Vector3.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir += forward;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir -= forward;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir += right;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir -= right;
            if (dir.sqrMagnitude > 0.0001f)
            {
                float speed = walkSpeed * (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed ? sprintMultiplier : 1f);
                origin.position += dir.normalized * (speed * Time.deltaTime);
            }

            if (kb.qKey.isPressed) origin.Rotate(0f, -keyboardTurnSpeed * Time.deltaTime, 0f);
            if (kb.eKey.isPressed) origin.Rotate(0f, keyboardTurnSpeed * Time.deltaTime, 0f);

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                origin.Rotate(0f, mouse.delta.ReadValue().x * 0.12f, 0f);
        }
    }
}
