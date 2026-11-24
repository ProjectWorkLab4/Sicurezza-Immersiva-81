using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace SI81.Ludovica.TempCore
{
    /// <summary>
    /// [TEMP] Rig del giocatore finché non arriva quello di SCN_Core (VR Interaction System).
    /// VR: testa e mani tracciate, levetta sinistra = camminare, levetta destra = rotazione a scatti,
    /// grip = afferra, grilletto = usa, A/X = ispeziona, B/Y = parla ("Fermi! Allontanatevi!"). Il tasto Menu sinistro è la Pausa
    /// della UI comune: il rig non lo usa. Desktop (Editor senza visore): WASD, tasto destro del mouse = guardare,
    /// E = afferra/lascia, clic sinistro = usa, R = ispeziona, Q = parla.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TempRig : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private TempHand leftHand;
        [SerializeField] private TempHand rightHand;
        [Tooltip("Velocità di camminata (m/s).")]
        [SerializeField] private float moveSpeed = 1.4f;
        [Tooltip("Gradi della rotazione a scatti.")]
        [SerializeField] private float snapTurnDegrees = 30f;
        [Tooltip("Altezza della testa in modalità desktop (m).")]
        [SerializeField] private float desktopEyeHeight = 1.65f;

        private CharacterController body;
        private bool desktop;
        private float pitch;
        private bool snapReady = true;
        private float verticalSpeed;

        private InputAction leftGrip, leftTrigger, leftPrimary, leftSecondary, leftStick;
        private InputAction rightGrip, rightTrigger, rightPrimary, rightSecondary, rightStick;

        /// <summary>Rig attivo (uno solo).</summary>
        public static TempRig Instance { get; private set; }
        /// <summary>Il giocatore ha parlato (B/Y o Q): posizione della testa e direzione dello sguardo.</summary>
        public static event Action<Vector3, Vector3> Shout;

        /// <summary>Testa (camera).</summary>
        public Transform Head => head;
        /// <summary>Mano sinistra.</summary>
        public TempHand LeftHand => leftHand;
        /// <summary>Mano destra.</summary>
        public TempHand RightHand => rightHand;
        /// <summary>true in modalità desktop (nessun visore attivo).</summary>
        public bool IsDesktop => desktop;
        /// <summary>Posizione dei piedi del giocatore (testa proiettata a terra).</summary>
        public Vector3 FeetPosition => new Vector3(head.position.x, transform.position.y, head.position.z);

        /// <summary>Prepara input, tracciamento e modalità (VR o desktop).</summary>
        private void Awake()
        {
            Instance = this;
            body = GetComponent<CharacterController>();
            desktop = !XRSettings.isDeviceActive;

            ConfigurePoseDriver(head, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");
            ConfigurePoseDriver(leftHand.transform, "<XRController>{LeftHand}/devicePosition", "<XRController>{LeftHand}/deviceRotation");
            ConfigurePoseDriver(rightHand.transform, "<XRController>{RightHand}/devicePosition", "<XRController>{RightHand}/deviceRotation");

            leftGrip = Button("LeftHand", "{GripButton}", "gripPressed");
            leftTrigger = Button("LeftHand", "{TriggerButton}", "triggerPressed");
            leftPrimary = Button("LeftHand", "{PrimaryButton}", "primaryButton");
            leftSecondary = Button("LeftHand", "{SecondaryButton}", "secondaryButton");
            leftStick = Stick("LeftHand");
            rightGrip = Button("RightHand", "{GripButton}", "gripPressed");
            rightTrigger = Button("RightHand", "{TriggerButton}", "triggerPressed");
            rightPrimary = Button("RightHand", "{PrimaryButton}", "primaryButton");
            rightSecondary = Button("RightHand", "{SecondaryButton}", "secondaryButton");
            rightStick = Stick("RightHand");

            if (desktop)
            {
                foreach (var tpd in GetComponentsInChildren<TrackedPoseDriver>()) tpd.enabled = false;
                head.localPosition = new Vector3(0f, desktopEyeHeight, 0f);
                head.localRotation = Quaternion.identity;
                leftHand.gameObject.SetActive(false);
                Debug.Log($"{TempHud.TAG} Rig desktop: WASD muovi, tasto destro guarda, E afferra/lascia, clic sinistro usa, R ispeziona, Q parla.");
            }
            else
            {
                SetFloorOrigin();
                head.localPosition = Vector3.zero;
            }
        }

        /// <summary>Libera il riferimento statico e gli input.</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var a in new[] { leftGrip, leftTrigger, leftPrimary, leftSecondary, leftStick, rightGrip, rightTrigger, rightPrimary, rightSecondary, rightStick })
                a?.Dispose();
        }

        /// <summary>Porta il giocatore in un punto (piedi) con una rotazione sul piano.</summary>
        /// <param name="feet">Posizione dei piedi.</param>
        /// <param name="yaw">Rotazione in gradi.</param>
        public void TeleportTo(Vector3 feet, float yaw)
        {
            body.enabled = false;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 headOffset = head.position - transform.position;
            headOffset.y = 0f;
            transform.position = feet - headOffset;
            body.enabled = true;
        }

        /// <summary>Legge input, muove il giocatore e aggiorna le mani.</summary>
        private void Update()
        {
            if (desktop) UpdateDesktop();
            else UpdateVr();
        }

        /// <summary>Aggiornamento in VR.</summary>
        private void UpdateVr()
        {
            Vector2 move = leftStick.ReadValue<Vector2>();
            Vector2 turn = rightStick.ReadValue<Vector2>();
            Move(move);

            if (Mathf.Abs(turn.x) > 0.7f && snapReady)
            {
                transform.RotateAround(head.position, Vector3.up, Mathf.Sign(turn.x) * snapTurnDegrees);
                snapReady = false;
            }
            else if (Mathf.Abs(turn.x) < 0.3f) snapReady = true;

            leftHand.Tick(ReadHand(leftGrip, leftTrigger, leftPrimary), null);
            rightHand.Tick(ReadHand(rightGrip, rightTrigger, rightPrimary), null);

            if (leftSecondary.WasPressedThisFrame() || rightSecondary.WasPressedThisFrame()) Shout?.Invoke(head.position, head.forward);
        }

        /// <summary>Aggiornamento in modalità desktop (tastiera e mouse).</summary>
        private void UpdateDesktop()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * 0.15f;
                transform.Rotate(0f, delta.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 move = Vector2.zero;
            if (kb.wKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed) move.y -= 1f;
            if (kb.dKey.isPressed) move.x += 1f;
            if (kb.aKey.isPressed) move.x -= 1f;
            Move(Vector2.ClampMagnitude(move, 1f));

            // La mano destra sta davanti alla camera e segue lo sguardo.
            rightHand.transform.SetPositionAndRotation(head.position + head.forward * 0.55f + head.right * 0.2f - head.up * 0.2f, head.rotation);
            var buttons = new HandButtons
            {
                GripDown = kb.eKey.wasPressedThisFrame,
                GripIsToggle = true,
                TriggerDown = mouse.leftButton.wasPressedThisFrame,
                TriggerUp = mouse.leftButton.wasReleasedThisFrame,
                PrimaryDown = kb.rKey.wasPressedThisFrame
            };
            rightHand.Tick(buttons, new Ray(head.position, head.forward));

            if (kb.qKey.wasPressedThisFrame) Shout?.Invoke(head.position, head.forward);
        }

        /// <summary>Movimento continuo relativo alla direzione dello sguardo, con gravità.</summary>
        private void Move(Vector2 input)
        {
            Vector3 fwd = head.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = head.right; right.y = 0f; right.Normalize();
            Vector3 velocity = (fwd * input.y + right * input.x) * moveSpeed;

            verticalSpeed = body.isGrounded ? -1f : verticalSpeed - 9.81f * Time.deltaTime;
            velocity.y = verticalSpeed;

            Vector3 local = head.localPosition;
            body.center = new Vector3(local.x, body.height * 0.5f + body.skinWidth, local.z);
            body.Move(velocity * Time.deltaTime);
        }

        /// <summary>Legge i tasti di una mano VR.</summary>
        private static HandButtons ReadHand(InputAction grip, InputAction trigger, InputAction primary)
        {
            return new HandButtons
            {
                GripDown = grip.WasPressedThisFrame(),
                GripUp = grip.WasReleasedThisFrame(),
                TriggerDown = trigger.WasPressedThisFrame(),
                TriggerUp = trigger.WasReleasedThisFrame(),
                PrimaryDown = primary.WasPressedThisFrame()
            };
        }

        /// <summary>Crea un'azione pulsante con binding per usage e per nome del controllo.</summary>
        private static InputAction Button(string hand, string usage, string control)
        {
            var a = new InputAction(type: InputActionType.Button);
            a.AddBinding($"<XRController>{{{hand}}}/{usage}");
            a.AddBinding($"<XRController>{{{hand}}}/{control}");
            a.Enable();
            return a;
        }

        /// <summary>Crea un'azione levetta.</summary>
        private static InputAction Stick(string hand)
        {
            var a = new InputAction(type: InputActionType.Value, expectedControlType: "Vector2");
            a.AddBinding($"<XRController>{{{hand}}}/{{Primary2DAxis}}");
            a.Enable();
            return a;
        }

        /// <summary>Configura un TrackedPoseDriver con posizione e rotazione dal dispositivo XR.</summary>
        private static void ConfigurePoseDriver(Transform target, string positionPath, string rotationPath)
        {
            var tpd = target.GetComponent<TrackedPoseDriver>();
            if (tpd == null) tpd = target.gameObject.AddComponent<TrackedPoseDriver>();
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.ignoreTrackingState = true;
            tpd.positionInput = new InputActionProperty(new InputAction(binding: positionPath, expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(new InputAction(binding: rotationPath, expectedControlType: "Quaternion"));
        }

        /// <summary>Imposta l'origine del tracciamento sul pavimento.</summary>
        private static void SetFloorOrigin()
        {
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var s in subsystems) s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
    }
}
