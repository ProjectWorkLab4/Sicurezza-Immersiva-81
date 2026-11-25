using System.Collections;
using NUnit.Framework;
using SI81.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace SI81.Rig.Tests
{
    /// <summary>
    /// Seam PlayMode unico del Player Rig: controller XR virtuale, gerarchia minima testa/mani, RigRegistration,
    /// locomozione e bridge UI finto. Non carica il prefab Meta né richiede OpenXR/visore.
    /// </summary>
    public sealed class RigIntegrationTests : InputTestFixture
    {
        private VirtualXRController leftController;
        private VirtualXRController rightController;
        private InputService service;
        private RigRegistration rig;
        private RigLocomotion locomotion;
        private StubRigUIBridge bridge;
        private GameObject root;
        private Transform head;
        private Transform trackingOrigin;
        private Transform leftHand;
        private Transform rightHand;

        /// <summary>
        /// Prepara Input System virtuale, servizio, rig minimo e bridge finto. Gira DOPO il reset di InputTestFixture:
        /// UnitySetUp girerebbe prima e i dispositivi verrebbero cancellati dal reset.
        /// </summary>
        [SetUp]
        public override void Setup()
        {
            base.Setup();

            InputSystem.RegisterLayout<VirtualXRController>();
            leftController = InputSystem.AddDevice<VirtualXRController>("LeftController");
            InputSystem.SetDeviceUsage(leftController, CommonUsages.LeftHand);
            rightController = InputSystem.AddDevice<VirtualXRController>("RightController");
            InputSystem.SetDeviceUsage(rightController, CommonUsages.RightHand);
            Set(leftController, "isTracked", 1f);
            Set(rightController, "isTracked", 1f);

            InputService.ResetInstance();
            service = InputService.EnsureInstance();
            service.SetBackEnabled(false);

            root = new GameObject("RigRoot");
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            rig = root.AddComponent<RigRegistration>();
            locomotion = root.AddComponent<RigLocomotion>();
            locomotion.GravityEnabled = false;
            root.AddComponent<RigUIPointerFeed>();

            trackingOrigin = new GameObject("TrackingOrigin").transform;
            trackingOrigin.SetParent(root.transform, false);
            head = new GameObject("Head").transform;
            head.SetParent(trackingOrigin, false);
            head.localPosition = new Vector3(0f, 1.6f, 0f);
            leftHand = new GameObject("LeftHand").transform;
            leftHand.SetParent(trackingOrigin, false);
            leftHand.localPosition = new Vector3(-0.25f, 1.2f, 0.2f);
            rightHand = new GameObject("RightHand").transform;
            rightHand.SetParent(trackingOrigin, false);
            rightHand.localPosition = new Vector3(0.25f, 1.2f, 0.2f);

            bridge = new GameObject("StubBridge").AddComponent<StubRigUIBridge>();
            rig.Configure(head, trackingOrigin, leftHand, rightHand);
            rig.RegisterBridge(bridge);
        }

        /// <summary>
        /// Distrugge rig, bridge e servizio e ripristina il tempo, prima del TearDown del fixture.
        /// </summary>
        [TearDown]
        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (root != null) Object.DestroyImmediate(root);
            if (bridge != null) Object.DestroyImmediate(bridge.gameObject);
            InputService.ResetInstance();
            base.TearDown();
        }

        /// <summary>
        /// Il bridge riceve i quattro riferimenti esatti del rig.
        /// </summary>
        [UnityTest]
        public IEnumerator RigHandoff_IsAppliedToBridge()
        {
            yield return null;
            Assert.That(bridge.ApplyCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(bridge.Handoff.Head, Is.SameAs(head));
            Assert.That(bridge.Handoff.TrackingOrigin, Is.SameAs(trackingOrigin));
            Assert.That(bridge.Handoff.LeftHand, Is.SameAs(leftHand));
            Assert.That(bridge.Handoff.RightHand, Is.SameAs(rightHand));
            Assert.That(RigRegistration.Instance, Is.SameAs(rig));
        }

        /// <summary>
        /// Select segue il grilletto della mano e produce performed/canceled con la mano corretta.
        /// </summary>
        [UnityTest]
        public IEnumerator Select_DeliversPerHandPerformedAndCanceled()
        {
            Hand performed = Hand.Any;
            Hand canceled = Hand.Any;
            int performedCount = 0;
            int canceledCount = 0;
            service.SelectPerformed += hand => { performed = hand; performedCount++; };
            service.SelectCanceled += hand => { canceled = hand; canceledCount++; };

            Set(leftController, "triggerPressed", 1f);
            yield return null;
            Assert.That(performedCount, Is.EqualTo(1));
            Assert.That(performed, Is.EqualTo(Hand.Left));

            Set(leftController, "triggerPressed", 0f);
            yield return null;
            Assert.That(canceledCount, Is.EqualTo(1));
            Assert.That(canceled, Is.EqualTo(Hand.Left));
        }

        /// <summary>
        /// Le due mani sono indipendenti: la seconda pressione arriva anche mentre la prima è tenuta.
        /// </summary>
        [UnityTest]
        public IEnumerator Select_DeliversBothHandsIndependently()
        {
            bool leftPerformed = false;
            bool rightPerformed = false;
            bool leftCanceled = false;
            bool rightCanceled = false;
            service.SelectPerformed += hand =>
            {
                if (hand == Hand.Left) leftPerformed = true;
                if (hand == Hand.Right) rightPerformed = true;
            };
            service.SelectCanceled += hand =>
            {
                if (hand == Hand.Left) leftCanceled = true;
                if (hand == Hand.Right) rightCanceled = true;
            };

            Set(leftController, "triggerPressed", 1f);
            yield return null;
            Assert.That(leftPerformed, Is.True);
            Assert.That(rightPerformed, Is.False);

            Set(rightController, "triggerPressed", 1f);
            yield return null;
            Assert.That(leftPerformed, Is.True);
            Assert.That(rightPerformed, Is.True);

            Set(rightController, "triggerPressed", 0f);
            yield return null;
            Assert.That(rightCanceled, Is.True);
            Assert.That(leftCanceled, Is.False);

            Set(leftController, "triggerPressed", 0f);
            yield return null;
            Assert.That(leftCanceled, Is.True);
        }

        /// <summary>
        /// Grab resta tenuto e viene annullato quando World si sospende; dopo il rilascio una nuova pressione riparte.
        /// </summary>
        [UnityTest]
        public IEnumerator Grab_IsCanceledWhenWorldIsSuppressed()
        {
            int canceledCount = 0;
            service.GrabCanceled += _ => canceledCount++;

            Set(leftController, "gripPressed", 1f);
            yield return null;
            Assert.That(service.IsGrabHeld(Hand.Left), Is.True);

            InputContextHandle handle = service.PushContext(InputChannel.World, new object());
            Assert.That(service.IsWorldSuppressed, Is.True);
            Assert.That(service.IsGrabHeld(Hand.Left), Is.False);
            Assert.That(canceledCount, Is.EqualTo(1));

            handle.Dispose();
            Assert.That(service.IsWorldSuppressed, Is.False);
            Set(leftController, "gripPressed", 0f);
            yield return null;
            Set(leftController, "gripPressed", 1f);
            yield return null;
            Assert.That(service.IsGrabHeld(Hand.Left), Is.True);
        }

        /// <summary>
        /// Due owner possono sospendere World: il primo rilascio non riattiva l'input finché resta il secondo.
        /// </summary>
        [UnityTest]
        public IEnumerator Contexts_AreOwnerScopedAndIdempotent()
        {
            object firstOwner = new object();
            object secondOwner = new object();
            InputContextHandle first = service.PushContext(InputChannel.World, firstOwner);
            InputContextHandle second = service.PushContext(InputChannel.World, secondOwner);
            Assert.That(service.IsWorldSuppressed, Is.True);

            first.Dispose();
            first.Dispose();
            Assert.That(service.IsWorldSuppressed, Is.True);

            second.Dispose();
            Assert.That(service.IsWorldSuppressed, Is.False);
            yield return null;
        }

        /// <summary>
        /// Il raggio UI sopprime Select solo per la mano che punta la UI.
        /// </summary>
        [UnityTest]
        public IEnumerator UIPointing_SuppressesSelectForThatHandOnly()
        {
            Hand last = Hand.Any;
            int count = 0;
            service.SelectPerformed += hand => { last = hand; count++; };

            bridge.Pointing[0] = true;
            yield return null;
            Set(leftController, "triggerPressed", 1f);
            yield return null;
            Assert.That(count, Is.EqualTo(0));
            Set(leftController, "triggerPressed", 0f);
            yield return null;

            bridge.Pointing[0] = false;
            yield return null;
            Set(rightController, "triggerPressed", 1f);
            yield return null;
            Assert.That(count, Is.EqualTo(1));
            Assert.That(last, Is.EqualTo(Hand.Right));
            Set(rightController, "triggerPressed", 0f);
            yield return null;

            Set(leftController, "triggerPressed", 1f);
            yield return null;
            Assert.That(count, Is.EqualTo(2));
            Assert.That(last, Is.EqualTo(Hand.Left));
        }

        /// <summary>
        /// Back è attivo solo dopo SetBackEnabled; il tasto menu produce sempre MenuPerformed.
        /// </summary>
        [UnityTest]
        public IEnumerator Back_RequiresEnable_AndMenuIsDelivered()
        {
            int backCount = 0;
            int menuCount = 0;
            service.BackPerformed += _ => backCount++;
            service.MenuPerformed += _ => menuCount++;

            Set(leftController, "secondaryButton", 1f);
            yield return null;
            Assert.That(backCount, Is.EqualTo(0));
            Set(leftController, "secondaryButton", 0f);
            yield return null;

            service.SetBackEnabled(true);
            Set(leftController, "secondaryButton", 1f);
            yield return null;
            Assert.That(backCount, Is.EqualTo(1));
            Set(leftController, "secondaryButton", 0f);
            yield return null;

            Set(leftController, "menuButton", 1f);
            yield return null;
            Assert.That(menuCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Move/Turn riportano i valori degli stick e la mano associata.
        /// </summary>
        [UnityTest]
        public IEnumerator MoveAndTurn_ReportStickValuesAndHands()
        {
            Vector2 move = default;
            Vector2 turn = default;
            Hand moveHand = Hand.Any;
            Hand turnHand = Hand.Any;
            service.MoveChanged += (hand, value) => { moveHand = hand; move = value; };
            service.TurnChanged += (hand, value) => { turnHand = hand; turn = value; };

            Set(leftController, "primary2DAxis", new Vector2(0.25f, 0.75f));
            Set(rightController, "primary2DAxis", new Vector2(-0.5f, 0.5f));
            yield return null;

            Assert.That(service.ReadMove(), Is.EqualTo(new Vector2(0.25f, 0.75f)));
            Assert.That(service.ReadTurn(), Is.EqualTo(new Vector2(-0.5f, 0.5f)));
            Assert.That(move, Is.EqualTo(new Vector2(0.25f, 0.75f)));
            Assert.That(turn, Is.EqualTo(new Vector2(-0.5f, 0.5f)));
            Assert.That(moveHand, Is.EqualTo(Hand.Left));
            Assert.That(turnHand, Is.EqualTo(Hand.Right));
        }

        /// <summary>
        /// Lo stick sinistro muove la radice del rig lungo lo yaw della testa.
        /// </summary>
        [UnityTest]
        public IEnumerator Locomotion_MovesRootRelativeToHeadYaw()
        {
            head.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Vector3 start = root.transform.position;

            Set(leftController, "primary2DAxis", new Vector2(0f, 1f));
            float deadline = Time.realtimeSinceStartup + 10f;
            while (root.transform.position.x - start.x <= 0.05f && Time.realtimeSinceStartup < deadline)
                yield return null;

            Vector3 delta = root.transform.position - start;
            Assert.That(delta.x, Is.GreaterThan(0.05f));
            Assert.That(Mathf.Abs(delta.z), Is.LessThan(0.01f));

            Set(leftController, "primary2DAxis", Vector2.zero);
            yield return null;
        }

        /// <summary>
        /// World sospeso o Time.timeScale zero fermano movimento e rotazione.
        /// </summary>
        [UnityTest]
        public IEnumerator Locomotion_StopsWhenSuppressedOrPaused()
        {
            InputContextHandle handle = service.PushContext(InputChannel.World, new object());
            Vector3 start = root.transform.position;
            Set(leftController, "primary2DAxis", new Vector2(0f, 1f));
            yield return null;
            yield return null;
            Assert.That(root.transform.position, Is.EqualTo(start));
            handle.Dispose();
            Set(leftController, "primary2DAxis", Vector2.zero);
            yield return null;

            Quaternion rotation = root.transform.rotation;
            Time.timeScale = 0f;
            Set(rightController, "primary2DAxis", new Vector2(1f, 0f));
            yield return null;
            yield return null;
            Assert.That(root.transform.rotation, Is.EqualTo(rotation));
            Time.timeScale = 1f;
            Set(rightController, "primary2DAxis", Vector2.zero);
            yield return null;
        }

        /// <summary>
        /// Lo stick destro produce scatti da 45° e si riarma al rientro.
        /// </summary>
        [UnityTest]
        public IEnumerator SnapTurn_RotatesInFortyFiveDegreeSteps()
        {
            Quaternion start = root.transform.rotation;

            Set(rightController, "primary2DAxis", new Vector2(1f, 0f));
            yield return null;
            Assert.That(Quaternion.Angle(start, root.transform.rotation), Is.EqualTo(45f).Within(1f));

            Set(rightController, "primary2DAxis", Vector2.zero);
            yield return null;
            Set(rightController, "primary2DAxis", new Vector2(1f, 0f));
            yield return null;
            Assert.That(Quaternion.Angle(start, root.transform.rotation), Is.EqualTo(90f).Within(1f));
        }

        /// <summary>
        /// L'impulso arriva al bridge con mano, ampiezza e durata.
        /// </summary>
        [UnityTest]
        public IEnumerator Pulse_ReachesBridge()
        {
            rig.Pulse(Hand.Left, 0.8f, 0.2f);
            Assert.That(bridge.PulseCount, Is.EqualTo(1));
            Assert.That(bridge.LastPulseHand, Is.EqualTo(Hand.Left));
            Assert.That(bridge.LastPulseAmplitude, Is.EqualTo(0.8f));
            Assert.That(bridge.LastPulseDuration, Is.EqualTo(0.2f));
            yield return null;
        }

        /// <summary>
        /// La posa aim è in spazio di tracciamento e il rig la converte in spazio mondo.
        /// </summary>
        [UnityTest]
        public IEnumerator AimPose_IsTrackingSpace_AndRigConvertsToWorld()
        {
            Vector3 localPosition = new Vector3(0.1f, 1.2f, 0.3f);
            Quaternion localRotation = Quaternion.Euler(10f, 20f, 30f);
            Set(leftController, "pointerPosition", localPosition);
            Set(leftController, "pointerRotation", localRotation);
            Set(leftController, "isTracked", 1f);
            yield return null;

            Assert.That(service.TryGetAimPose(Hand.Left, out Vector3 position, out Quaternion rotation), Is.True);
            Assert.That(position, Is.EqualTo(localPosition));
            Assert.That(Quaternion.Angle(rotation, localRotation), Is.LessThan(0.01f));

            trackingOrigin.position = new Vector3(1f, 0f, 2f);
            trackingOrigin.rotation = Quaternion.Euler(0f, 90f, 0f);
            Assert.That(rig.TryGetAimPose(Hand.Left, out Vector3 worldPosition, out Quaternion worldRotation), Is.True);
            Assert.That(worldPosition, Is.EqualTo(trackingOrigin.TransformPoint(localPosition)));
            Assert.That(Quaternion.Angle(worldRotation, trackingOrigin.rotation * localRotation), Is.LessThan(0.01f));
        }

        /// <summary>
        /// Un solo InputService resta vivo e ResetInstance lo azzera davvero.
        /// </summary>
        [UnityTest]
        public IEnumerator InputService_IsSingular_AndResetClearsInstance()
        {
            InputService first = InputService.Instance;
            GameObject duplicateHost = new GameObject("DuplicateInputService");
            InputService duplicate = duplicateHost.AddComponent<InputService>();
            yield return null;

            Assert.That(InputService.Instance, Is.SameAs(first));
            Assert.That(duplicate == null || !duplicate.enabled, Is.True);

            InputService.ResetInstance();
            Assert.That(InputService.Instance, Is.Null);

            InputService second = InputService.EnsureInstance();
            Assert.That(second, Is.Not.Null);
            Assert.That(InputService.Instance, Is.SameAs(second));

            Object.Destroy(duplicateHost);
            yield return null;
        }

        /// <summary>
        /// MovementLock blocca movimento/rotazione a scatti e disabilita le InputAction Move e Turn.
        /// </summary>
        [UnityTest]
        public IEnumerator MovementLock_BlocksLocomotionAndDisablesMoveAndTurnActions()
        {
            InputAction moveAction = service.WorldMap.FindAction(InputBindings.Move, true);
            InputAction turnAction = service.WorldMap.FindAction(InputBindings.Turn, true);

            yield return null;
            Assert.That(locomotion.MovementLock, Is.False);
            Assert.That(service.IsLocomotionEnabled, Is.True);
            Assert.That(moveAction.enabled, Is.True);
            Assert.That(turnAction.enabled, Is.True);

            locomotion.MovementLock = true;
            Assert.That(service.IsLocomotionEnabled, Is.False);
            Assert.That(moveAction.enabled, Is.False);
            Assert.That(turnAction.enabled, Is.False);

            Vector3 startPos = root.transform.position;
            Quaternion startRot = root.transform.rotation;
            Set(leftController, "primary2DAxis", new Vector2(0f, 1f));
            Set(rightController, "primary2DAxis", new Vector2(1f, 0f));
            yield return null;
            yield return null;
            Assert.That(service.ReadMove(), Is.EqualTo(Vector2.zero));
            Assert.That(service.ReadTurn(), Is.EqualTo(Vector2.zero));
            Assert.That(root.transform.position, Is.EqualTo(startPos));
            Assert.That(root.transform.rotation, Is.EqualTo(startRot));

            Set(leftController, "primary2DAxis", Vector2.zero);
            Set(rightController, "primary2DAxis", Vector2.zero);
            yield return null;

            locomotion.MovementLock = false;
            Assert.That(service.IsLocomotionEnabled, Is.True);
            Assert.That(moveAction.enabled, Is.True);
            Assert.That(turnAction.enabled, Is.True);

            Set(rightController, "primary2DAxis", new Vector2(1f, 0f));
            yield return null;
            Assert.That(Quaternion.Angle(startRot, root.transform.rotation), Is.EqualTo(45f).Within(1f));
        }

        /// <summary>
        /// SpawnPoint sblocca MovementLock all'attivazione e lo ripristina alla disattivazione.
        /// </summary>
        [UnityTest]
        public IEnumerator SpawnPoint_UnlocksMovementOnEnable_AndRestoresOnDisable()
        {
            locomotion.MovementLock = true;
            yield return null;
            Assert.That(service.IsLocomotionEnabled, Is.False);

            GameObject spawnObject = new GameObject("MNG_SpawnPoint");
            spawnObject.transform.position = new Vector3(2f, 0f, -3f);
            spawnObject.AddComponent<SpawnPoint>();
            yield return null;

            Assert.That(locomotion.MovementLock, Is.False);
            Assert.That(service.IsLocomotionEnabled, Is.True);
            Assert.That(Vector3.Distance(root.transform.position, spawnObject.transform.position), Is.LessThan(0.01f));

            spawnObject.SetActive(false);
            yield return null;
            Assert.That(locomotion.MovementLock, Is.True);
            Assert.That(service.IsLocomotionEnabled, Is.False);

            Object.DestroyImmediate(spawnObject);
        }
    }
}
