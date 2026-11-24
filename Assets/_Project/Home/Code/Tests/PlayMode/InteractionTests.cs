using System.Collections;
using NUnit.Framework;
using SI81.Input;
using SI81.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace SI81.Rig.Tests
{
    /// <summary>
    /// Seam PlayMode del layer di interazione: controller XR virtuale, due RayInteractor per mano, un cubo con
    /// Interactable e un muro occludente. Non carica il prefab Meta, non richiede OpenXR né la UI comune.
    /// Ogni cambio di input o di posa attende due frame: l'interattore gira dopo la coroutine di test, quindi uno
    /// solo non basta a osservare l'effetto.
    /// </summary>
    public sealed class InteractionTests : InputTestFixture
    {
        private VirtualXRController leftController;
        private VirtualXRController rightController;
        private InputService service;
        private GameObject root;
        private Transform trackingOrigin;
        private RayInteractor leftInteractor;
        private RayInteractor rightInteractor;
        private GameObject targetObject;
        private Interactable interactable;
        private GameObject wall;

        /// <summary>
        /// Prepara input virtuale, servizio, tracking origin e due interattori con un bersaglio a 2 m.
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
            SetAim(leftController, new Vector3(0f, 1.2f, 0f), Quaternion.identity);
            SetAim(rightController, new Vector3(0f, 1.2f, 0f), Quaternion.identity);

            InputService.ResetInstance();
            service = InputService.EnsureInstance();

            root = new GameObject("InteractionRoot");
            trackingOrigin = new GameObject("TrackingOrigin").transform;
            trackingOrigin.SetParent(root.transform, false);

            leftInteractor = new GameObject("LeftRayInteractor").AddComponent<RayInteractor>();
            leftInteractor.transform.SetParent(root.transform, false);
            leftInteractor.Configure(Hand.Left, trackingOrigin, root.transform);

            rightInteractor = new GameObject("RightRayInteractor").AddComponent<RayInteractor>();
            rightInteractor.transform.SetParent(root.transform, false);
            rightInteractor.Configure(Hand.Right, trackingOrigin, root.transform);

            targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = "Target";
            targetObject.transform.position = new Vector3(0f, 1.2f, 2f);
            interactable = targetObject.AddComponent<Interactable>();
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Distrugge scena di test e servizio e ripristina il tempo.
        /// </summary>
        [TearDown]
        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (wall != null) Object.DestroyImmediate(wall);
            if (root != null) Object.DestroyImmediate(root);
            if (targetObject != null) Object.DestroyImmediate(targetObject);
            InputService.ResetInstance();
            base.TearDown();
        }

        /// <summary>
        /// Hover: dopo l'iscrizione esce e rientra, riportando la mano che ha generato l'evento.
        /// </summary>
        [UnityTest]
        public IEnumerator Hover_EntersAndExits()
        {
            int leftEntered = 0;
            int leftExited = 0;
            Hand enteredHand = Hand.Any;
            interactable.HoverEntered += hand =>
            {
                if (hand != Hand.Left) return;
                leftEntered++;
                enteredHand = hand;
            };
            interactable.HoverExited += hand =>
            {
                if (hand == Hand.Left) leftExited++;
            };

            SetAim(leftController, new Vector3(0f, 1.2f, 0f), Quaternion.Euler(0f, 180f, 0f));
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.Null);
            int exitsBeforeReenter = leftExited;

            SetAim(leftController, new Vector3(0f, 1.2f, 0f), Quaternion.identity);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.SameAs(interactable));
            Assert.That(interactable.IsHoveredBy(Hand.Left), Is.True);
            Assert.That(leftEntered, Is.EqualTo(1));
            Assert.That(enteredHand, Is.EqualTo(Hand.Left));

            SetAim(leftController, new Vector3(0f, 1.2f, 0f), Quaternion.Euler(0f, 180f, 0f));
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.Null);
            Assert.That(interactable.IsHoveredBy(Hand.Left), Is.False);
            Assert.That(leftExited, Is.EqualTo(exitsBeforeReenter + 1));
        }

        /// <summary>
        /// Select: il grilletto sinistro seleziona solo la mano sinistra e il rilascio chiude la selezione.
        /// </summary>
        [UnityTest]
        public IEnumerator Select_IsPerHand_AndFollowsTrigger()
        {
            int leftEntered = 0;
            int leftExited = 0;
            interactable.SelectEntered += hand => { if (hand == Hand.Left) leftEntered++; };
            interactable.SelectExited += hand => { if (hand == Hand.Left) leftExited++; };

            yield return null;
            yield return null;
            Set(leftController, "triggerPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Selected, Is.SameAs(interactable));
            Assert.That(interactable.IsSelectedBy(Hand.Left), Is.True);
            Assert.That(interactable.IsSelectedBy(Hand.Right), Is.False);
            Assert.That(leftEntered, Is.EqualTo(1));

            Set(leftController, "triggerPressed", 0f);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Selected, Is.Null);
            Assert.That(interactable.IsSelected, Is.False);
            Assert.That(leftExited, Is.EqualTo(1));
        }

        /// <summary>
        /// Grab: l'oggetto segue la posizione della mano mantenendo l'offset e resta dov'è al rilascio.
        /// </summary>
        [UnityTest]
        public IEnumerator Grab_FollowsHand_AndStaysOnRelease()
        {
            int entered = 0;
            int exited = 0;
            interactable.GrabEntered += hand => { if (hand == Hand.Left) entered++; };
            interactable.GrabExited += hand => { if (hand == Hand.Left) exited++; };

            yield return null;
            yield return null;
            Set(leftController, "gripPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Grabbed, Is.SameAs(interactable));
            Assert.That(interactable.IsGrabbedBy(Hand.Left), Is.True);
            Assert.That(entered, Is.EqualTo(1));

            Set(leftController, "pointerPosition", new Vector3(0.5f, 1.2f, 0f));
            yield return null;
            yield return null;
            Assert.That(targetObject.transform.position.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(targetObject.transform.position.z, Is.EqualTo(2f).Within(0.01f));

            Set(leftController, "gripPressed", 0f);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Grabbed, Is.Null);
            Assert.That(interactable.IsGrabbed, Is.False);
            Assert.That(targetObject.transform.position.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(exited, Is.EqualTo(1));
        }

        /// <summary>
        /// Grab su un Rigidbody: kinematic durante la presa e ripristinato al rilascio.
        /// </summary>
        [UnityTest]
        public IEnumerator Grab_MakesRigidbodyKinematic_AndRestoresIt()
        {
            Rigidbody body = targetObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;

            yield return null;
            Set(leftController, "gripPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(body.isKinematic, Is.True);

            Set(leftController, "gripPressed", 0f);
            yield return null;
            yield return null;
            Assert.That(body.isKinematic, Is.False);
        }

        /// <summary>
        /// Un collider non interagibile davanti al bersaglio blocca il raggio; togliendolo l'hover torna.
        /// </summary>
        [UnityTest]
        public IEnumerator Hover_IsBlockedByNonInteractableCollider()
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.position = new Vector3(0f, 1.2f, 0.9f);
            Physics.SyncTransforms();

            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.Null);

            Object.DestroyImmediate(wall);
            wall = null;
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.SameAs(interactable));
        }

        /// <summary>
        /// Quando la mano punta la UI, l'hover si chiude e il Select non arriva al mondo (gate dell'Input Service).
        /// </summary>
        [UnityTest]
        public IEnumerator UIPointing_ClearsHover_AndBlocksSelect()
        {
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.SameAs(interactable));

            service.SetUIPointerOverUI(Hand.Left, true);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.Null);

            Set(leftController, "triggerPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Selected, Is.Null);
            Assert.That(interactable.IsSelectedBy(Hand.Left), Is.False);

            Set(leftController, "triggerPressed", 0f);
            service.SetUIPointerOverUI(Hand.Left, false);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.SameAs(interactable));

            Set(leftController, "triggerPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Selected, Is.SameAs(interactable));

            service.SetUIPointerOverUI(Hand.Left, true);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Selected, Is.Null);
            Assert.That(interactable.IsSelectedBy(Hand.Left), Is.False);
            Assert.That(leftInteractor.Hovered, Is.Null);

            Set(leftController, "triggerPressed", 0f);
            service.SetUIPointerOverUI(Hand.Left, false);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.SameAs(interactable));
        }

        /// <summary>
        /// World sospeso (menu/pausa) chiude hover, selezione e presa tenuti.
        /// </summary>
        [UnityTest]
        public IEnumerator WorldSuppression_ReleasesSelectionAndGrab()
        {
            yield return null;
            yield return null;
            Set(leftController, "triggerPressed", 1f);
            Set(leftController, "gripPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Selected, Is.SameAs(interactable));
            Assert.That(leftInteractor.Grabbed, Is.SameAs(interactable));

            InputContextHandle handle = service.PushContext(InputChannel.World, new object());
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Hovered, Is.Null);
            Assert.That(leftInteractor.Selected, Is.Null);
            Assert.That(leftInteractor.Grabbed, Is.Null);
            Assert.That(interactable.IsSelected, Is.False);
            Assert.That(interactable.IsGrabbed, Is.False);

            handle.Dispose();
            Set(leftController, "triggerPressed", 0f);
            Set(leftController, "gripPressed", 0f);
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Le due mani sono indipendenti: il grilletto destro non chiude la selezione sinistra e viceversa.
        /// </summary>
        [UnityTest]
        public IEnumerator TwoHands_SelectIndependently()
        {
            yield return null;
            yield return null;
            Assert.That(rightInteractor.Hovered, Is.SameAs(interactable));

            Set(leftController, "triggerPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(interactable.IsSelectedBy(Hand.Left), Is.True);
            Assert.That(interactable.IsSelectedBy(Hand.Right), Is.False);

            Set(rightController, "triggerPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(interactable.IsSelectedBy(Hand.Left), Is.True);
            Assert.That(interactable.IsSelectedBy(Hand.Right), Is.True);

            Set(rightController, "triggerPressed", 0f);
            yield return null;
            yield return null;
            Assert.That(interactable.IsSelectedBy(Hand.Right), Is.False);
            Assert.That(interactable.IsSelectedBy(Hand.Left), Is.True);

            Set(leftController, "triggerPressed", 0f);
            yield return null;
            yield return null;
            Assert.That(interactable.IsSelected, Is.False);
        }

        /// <summary>
        /// Grab disabilitato sull'oggetto: l'impugnatura non produce presa.
        /// </summary>
        [UnityTest]
        public IEnumerator Grab_RespectsGrabbableFlag()
        {
            interactable.Grabbable = false;
            yield return null;
            yield return null;
            Set(leftController, "gripPressed", 1f);
            yield return null;
            yield return null;
            Assert.That(leftInteractor.Grabbed, Is.Null);
            Assert.That(interactable.IsGrabbed, Is.False);
        }

        /// <summary>
        /// Imposta la posa aim di un controller virtuale.
        /// </summary>
        /// <param name="controller">Controller virtuale.</param>
        /// <param name="position">Posizione nello spazio di tracciamento.</param>
        /// <param name="rotation">Rotazione nello spazio di tracciamento.</param>
        private void SetAim(VirtualXRController controller, Vector3 position, Quaternion rotation)
        {
            Set(controller, "pointerPosition", position);
            Set(controller, "pointerRotation", rotation);
        }
    }
}
