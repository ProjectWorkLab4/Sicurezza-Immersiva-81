using System.Reflection;
using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using UnityEngine;

/// <summary>
/// Makes the oversized Gas Leak room playable: the shared RayInteractor reaches 10 m in this
/// scenario, but the imported room places the detector and the windows far from the spawn point.
///
/// This component listens to the same InputService actions as the shared interactor and casts a
/// second, longer ray only for targets beyond the shared range. Near targets are left to the
/// shared RayInteractor, so there is no duplicate hover/selection and no second rig, camera or UI.
/// For far objects marked FollowsHand it also owns the grab follow, because the shared interactor
/// never receives those targets. It lives on MNG_Scenario and does not read gameplay scripts.
/// </summary>
[DisallowMultipleComponent]
public sealed class GasLeakLongRangeInteraction : MonoBehaviour
{
    const int HIT_BUFFER_SIZE = 16;

    [Tooltip("Distanza coperta dal RayInteractor condiviso in questo scenario: sotto questa soglia non intervene.")]
    [SerializeField] float sharedRange = 10f;
    [Tooltip("Portata del raggio aggiuntivo per gli oggetti lontani dello scenario.")]
    [SerializeField] float maxDistance = 100f;

    readonly RaycastHit[] hits = new RaycastHit[HIT_BUFFER_SIZE];
    readonly VRInteractableButton[] heldByHand = new VRInteractableButton[2];
    readonly Interactable[] heldInteractable = new Interactable[2];
    readonly bool[] heldByGrab = new bool[2];

    readonly Transform[] followedTransform = new Transform[2];
    readonly Rigidbody[] followedBody = new Rigidbody[2];
    readonly Vector3[] grabOffsetPosition = new Vector3[2];
    readonly Quaternion[] grabOffsetRotation = new Quaternion[2];
    readonly bool[] bodyWasKinematic = new bool[2];
    readonly bool[] bodyUsedGravity = new bool[2];

    InputService subscribed;
    bool _rangeCondivisaApplicata;

    static readonly FieldInfo CAMPO_DISTANZA = typeof(RayInteractor).GetField(
        "maxDistance", BindingFlags.Instance | BindingFlags.NonPublic);

    void OnEnable()
    {
        Subscribe(InputService.Instance);
    }

    void Start()
    {
        ApplicaRangeCondivisa();

        if (subscribed == null)
        {
            Subscribe(InputService.Instance);
        }
    }

    /// <summary>Raddoppia la portata del raggio condiviso senza modificare i file comuni.</summary>
    void ApplicaRangeCondivisa()
    {
        if (_rangeCondivisaApplicata || CAMPO_DISTANZA == null)
        {
            return;
        }

        RayInteractor[] interattori = FindObjectsByType<RayInteractor>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (interattori.Length == 0)
        {
            return;
        }

        foreach (RayInteractor interattore in interattori)
        {
            CAMPO_DISTANZA.SetValue(interattore, sharedRange);
        }

        _rangeCondivisaApplicata = true;
    }

    /// <summary>Riprova finché il rig condiviso non è stato caricato.</summary>
    void Update()
    {
        if (!_rangeCondivisaApplicata)
        {
            ApplicaRangeCondivisa();
        }
    }

    void OnDisable()
    {
        ReleaseAll();
        Subscribe(null);
    }

    void Subscribe(InputService service)
    {
        if (subscribed == service)
        {
            return;
        }

        if (subscribed != null)
        {
            subscribed.SelectPerformed -= HandleSelectPerformed;
            subscribed.SelectCanceled -= HandleSelectCanceled;
            subscribed.GrabPerformed -= HandleGrabPerformed;
            subscribed.GrabCanceled -= HandleGrabCanceled;
        }

        subscribed = service;

        if (subscribed != null)
        {
            subscribed.SelectPerformed += HandleSelectPerformed;
            subscribed.SelectCanceled += HandleSelectCanceled;
            subscribed.GrabPerformed += HandleGrabPerformed;
            subscribed.GrabCanceled += HandleGrabCanceled;
        }
    }

    void HandleSelectPerformed(Hand hand) => TryActivate(hand, false);

    void HandleGrabPerformed(Hand hand) => TryActivate(hand, true);

    void HandleSelectCanceled(Hand hand) => TryRelease(hand, false);

    void HandleGrabCanceled(Hand hand) => TryRelease(hand, true);

    /// <summary>
    /// Keeps far grabbed objects attached to the hand. Mirrors the shared RayInteractor follow,
    /// which never runs for targets outside its 5 m range.
    /// </summary>
    void LateUpdate()
    {
        if (subscribed == null || subscribed.IsWorldSuppressed || Time.timeScale <= 0f)
        {
            ReleaseAll();
            return;
        }

        for (int index = 0; index < 2; index++)
        {
            Transform followed = followedTransform[index];
            if (followed == null)
            {
                continue;
            }

            if (!TryGetHandPose((Hand)index, out Vector3 handPosition, out Quaternion handRotation))
            {
                continue;
            }

            Vector3 position = handPosition + handRotation * grabOffsetPosition[index];
            Quaternion rotation = handRotation * grabOffsetRotation[index];
            Rigidbody body = followedBody[index];
            if (body != null)
            {
                body.position = position;
                body.rotation = rotation;
            }
            else
            {
                followed.SetPositionAndRotation(position, rotation);
            }
        }
    }

    /// <summary>
    /// Activates the first far interactable under the hand's aim ray. Near interactables are
    /// intentionally ignored: they already belong to the shared RayInteractor.
    /// </summary>
    void TryActivate(Hand hand, bool grab)
    {
        int index = hand.Index();

        // A trigger press followed by a grip press upgrades the same object to a carried grab
        // without a second activation (and without fighting the button cooldown).
        if (heldByHand[index] != null)
        {
            if (grab && !heldByGrab[index])
            {
                heldByGrab[index] = true;
                if (heldInteractable[index] != null &&
                    TryGetHandPose(hand, out Vector3 currentPosition, out Quaternion currentRotation))
                {
                    BeginFollow(index, heldInteractable[index], currentPosition, currentRotation);
                }
            }
            return;
        }

        if (!CanUseWorld(hand) || !TryGetHandPose(hand, out Vector3 origin, out Quaternion rotation))
        {
            return;
        }

        Vector3 direction = rotation * Vector3.forward;
        int count = Physics.RaycastNonAlloc(origin, direction, hits, maxDistance, ~0,
            QueryTriggerInteraction.Ignore);

        // Same occlusion rule as the shared RayInteractor: only the first non-ignored
        // collider counts. A wall stops the ray; a near object belongs to the shared interactor.
        int nearest = -1;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.distance >= nearestDistance)
            {
                continue;
            }

            if (IsIgnored(hit.collider.transform))
            {
                continue;
            }

            nearest = i;
            nearestDistance = hit.distance;
        }

        if (nearest < 0 || nearestDistance < sharedRange)
        {
            return;
        }

        RaycastHit targetHit = hits[nearest];
        Interactable interactable = targetHit.collider.GetComponentInParent<Interactable>(true);
        if (interactable == null ||
            !(grab ? interactable.CanGrab(hand) : interactable.CanSelect(hand)))
        {
            return;
        }

        VRInteractableButton button = interactable.GetComponentInParent<VRInteractableButton>();
        if (button == null || !button.Available)
        {
            return;
        }

        heldByHand[index] = button;
        heldInteractable[index] = interactable;
        heldByGrab[index] = grab;
        button.Press();

        if (grab && interactable.FollowsHand)
        {
            BeginFollow(index, interactable, origin, rotation);
        }
    }

    /// <summary>Releases the far interactable activated by this hand, if any.</summary>
    void TryRelease(Hand hand, bool grabRelease)
    {
        int index = hand.Index();

        if (grabRelease)
        {
            EndFollow(index);
        }
        else if (heldByGrab[index])
        {
            // Trigger released while the grip still holds the object: keep it active.
            return;
        }

        VRInteractableButton button = heldByHand[index];
        if (button == null)
        {
            return;
        }

        heldByHand[index] = null;
        heldInteractable[index] = null;
        heldByGrab[index] = false;
        button.Release();
    }

    /// <summary>Records the hand offset and disables physics exactly like RayInteractor.BeginGrab.</summary>
    void BeginFollow(int index, Interactable interactable, Vector3 handPosition, Quaternion handRotation)
    {
        Rigidbody body = interactable.GetComponentInParent<Rigidbody>();
        Transform followed = body != null ? body.transform : interactable.transform;
        Quaternion inverseHand = Quaternion.Inverse(handRotation);

        grabOffsetPosition[index] = inverseHand * (followed.position - handPosition);
        grabOffsetRotation[index] = inverseHand * followed.rotation;
        followedTransform[index] = followed;
        followedBody[index] = body;

        if (body != null)
        {
            bodyWasKinematic[index] = body.isKinematic;
            bodyUsedGravity[index] = body.useGravity;
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    /// <summary>Restores physics state after a far grab.</summary>
    void EndFollow(int index)
    {
        followedTransform[index] = null;
        Rigidbody body = followedBody[index];
        followedBody[index] = null;
        if (body == null)
        {
            return;
        }

        body.isKinematic = bodyWasKinematic[index];
        body.useGravity = bodyUsedGravity[index];
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    /// <summary>Releases everything on disable, pause or world suppression.</summary>
    void ReleaseAll()
    {
        for (int index = 0; index < 2; index++)
        {
            EndFollow(index);
            if (heldByHand[index] != null)
            {
                heldByHand[index].Release();
            }
            heldByHand[index] = null;
            heldInteractable[index] = null;
            heldByGrab[index] = false;
        }
    }

    /// <summary>Matches the shared RayInteractor gates: no world input while UI is targeted.</summary>
    bool CanUseWorld(Hand hand)
    {
        return hand.IsSingle() && subscribed != null && !subscribed.IsWorldSuppressed &&
               !subscribed.IsPointingAtUI(hand) && Time.timeScale > 0f;
    }

    /// <summary>Converts the hand's aim pose from tracking space to world space.</summary>
    bool TryGetHandPose(Hand hand, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = default;

        RigRegistration rig = RigRegistration.Instance;
        if (rig == null || rig.TrackingOrigin == null || subscribed == null)
        {
            return false;
        }

        if (!subscribed.TryGetAimPose(hand, out Vector3 localPosition, out Quaternion localRotation))
        {
            return false;
        }

        position = rig.TrackingOrigin.TransformPoint(localPosition);
        rotation = rig.TrackingOrigin.rotation * localRotation;
        return true;
    }

    /// <summary>Excludes the player rig from the raycast, exactly like the shared interactor.</summary>
    bool IsIgnored(Transform candidate)
    {
        RigRegistration rig = RigRegistration.Instance;
        if (rig == null || candidate == null)
        {
            return false;
        }

        Transform ignoreRoot = rig.transform.root;
        return candidate == ignoreRoot || candidate.IsChildOf(ignoreRoot);
    }
}
