using System;
using System.Collections;
using System.Collections.Generic;
using SI81.Input;
using SI81.Interaction;
using UnityEngine;

/// <summary>
/// Resets a blackout door to its pre-blackout local Z rotation when the player
/// selects or grabs it with the shared <see cref="Interactable"/> events.
///
/// The doors come from <see cref="GameManager.DoorRotationRules"/>, so the list is
/// still edited in one place. Original angles are captured in <c>Awake</c>, before
/// GameManager's delayed blackout animation can change them. Only the local Z
/// rotation is touched, exactly as in GameManager; X/Y and the transform position
/// are left alone.
///
/// If a door has no <see cref="Interactable"/> in its hierarchy, one is added at
/// runtime with <c>FollowsHand = false</c> so the shared ray interactor can hit its
/// existing collider without the grab fighting this script's animation.
/// </summary>
[DisallowMultipleComponent]
public class DoorResetOnInteract : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("GameManager with the door rules. Auto-found in the scene if empty.")]
    [SerializeField] GameManager gameManager;

    BlackoutScenario scenario;

    [Header("Comportamento")]
    [Tooltip("Seconds for the smooth reset animation. 0 = instant.")]
    [SerializeField, Min(0f)] float resetDuration = 0.4f;
    [SerializeField] bool debugReset = true;

    [Header("Porte a rotazione su selezione")]
    [Tooltip("Door pairs that toggle between two local Z angles when selected.")]
    [SerializeField] DoorToggleRule[] toggleRules = new[]
    {
        new DoorToggleRule { objectName = "porta_D15", closedZ = 0f, openZ = 90f },
        new DoorToggleRule { objectName = "porta_D16", closedZ = 0f, openZ = 90f },
        new DoorToggleRule { objectName = "porta_D10", closedZ = 0f, openZ = -90f, oneShot = true },
        new DoorToggleRule { objectName = "porta_tagliafuoco_D1", closedZ = 90f, openZ = 0f, oneShot = true }
    };
    [Tooltip("Seconds for the smooth toggle animation. 0 = instant.")]
    [SerializeField, Min(0f)] float toggleDuration = 0.6f;

    [Header("Azioni errate")]
    [Tooltip("Doors whose selection counts as a GDD wrong action. No Z rotation is changed for these.")]
    [SerializeField] string[] wrongActionDoorNames = new[]
    {
        "porta_tagliafuoco_D12",
        "porta_tagliafuoco_D11",
        "porta_tagliafuoco_D14",
        "porta_D16",
        "porta_D15",
        "porta_D2",
        "porta_tagliafuoco_D13",
        "porta_tagliafuoco_D6",
        "porta_D9",
        "porta_tagliafuoco_D7",
        "porta_tagliafuoco_D8"
    };
    [SerializeField] string wrongActionDescription = "Non hai seguito la segnaletica";

    [Serializable]
    public class DoorToggleRule
    {
        [Tooltip("Direct reference. If empty, the name below is used.")]
        public Transform target;
        public string objectName;
        public float closedZ;
        public float openZ;
        [Tooltip("If true, the rule rotates once and then stops listening for selection.")]
        public bool oneShot;
    }

    const float DoorAngleTolerance = 5f;

    class DoorBinding
    {
        public Transform Door;
        public float OriginalZ;
        public Coroutine Routine;
    }

    class ToggleBinding
    {
        public Transform Door;
        public float ClosedZ;
        public float OpenZ;
        public bool IsOpen;
        public bool OneShot;
        public Coroutine Routine;
    }

    readonly List<DoorBinding> bindings = new List<DoorBinding>();
    readonly Dictionary<Interactable, Action<Hand>> handlers = new Dictionary<Interactable, Action<Hand>>();
    readonly List<ToggleBinding> toggleBindings = new List<ToggleBinding>();
    readonly Dictionary<Interactable, Action<Hand>> toggleHandlers = new Dictionary<Interactable, Action<Hand>>();
    readonly List<Transform> wrongActionDoors = new List<Transform>();
    readonly Dictionary<Interactable, Action<Hand>> wrongActionHandlers = new Dictionary<Interactable, Action<Hand>>();

    void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        scenario = FindFirstObjectByType<BlackoutScenario>();

        // Must happen before GameManager's delayed OnBlackout animation moves the doors.
        BuildBindings();
        BuildToggleBindings();
        BuildWrongActionBindings();
    }

    void OnEnable()
    {
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    void BuildBindings()
    {
        bindings.Clear();

        if (gameManager == null)
        {
            if (debugReset)
                Debug.LogWarning("[DoorReset] No GameManager found; no doors will be tracked.");
            return;
        }

        IReadOnlyList<BlackoutDoorRotationRule> rules = gameManager.DoorRotationRules;
        if (rules == null || rules.Count == 0)
        {
            if (debugReset)
                Debug.LogWarning("[DoorReset] GameManager has no door rotation rules.");
            return;
        }

        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (BlackoutDoorRotationRule rule in rules)
        {
            Transform door = ResolveDoor(rule, sceneTransforms);

            if (door == null)
            {
                if (debugReset)
                    Debug.LogWarning($"[DoorReset] No door found for '{rule.objectName}' at Z={rule.fromZ:0.##}.");
                continue;
            }

            // Same wrap-safe adjustment used by GameManager: keeps the exact Inspector angle.
            float currentZ = door.localEulerAngles.z;
            float originalZ = rule.fromZ + Mathf.DeltaAngle(rule.fromZ, currentZ);
            bindings.Add(new DoorBinding { Door = door, OriginalZ = originalZ });
        }

        if (debugReset)
            Debug.Log($"[DoorReset] Tracking {bindings.Count} door(s) for reset on interaction.");
    }

    void BuildToggleBindings()
    {
        toggleBindings.Clear();

        if (toggleRules == null || toggleRules.Length == 0)
            return;

        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (DoorToggleRule rule in toggleRules)
        {
            if (rule == null)
                continue;

            Transform door = rule.target;

            if (door == null && !string.IsNullOrEmpty(rule.objectName))
            {
                Transform best = null;
                float bestDelta = float.MaxValue;

                foreach (Transform candidate in sceneTransforms)
                {
                    if (candidate == null || !GameManager.DoorNameMatches(candidate.name, rule.objectName))
                        continue;

                    float delta = Mathf.Abs(Mathf.DeltaAngle(candidate.localEulerAngles.z, rule.closedZ));
                    if (delta < bestDelta)
                    {
                        best = candidate;
                        bestDelta = delta;
                    }
                }

                if (bestDelta <= DoorAngleTolerance)
                    door = best;
            }

            if (door == null)
            {
                if (debugReset)
                    Debug.LogWarning($"[DoorReset] No door found for toggle rule '{rule.objectName}' at Z={rule.closedZ:0.##}.");
                continue;
            }

            float closedZ = rule.closedZ + Mathf.DeltaAngle(rule.closedZ, door.localEulerAngles.z);
            float openZ = closedZ + Mathf.DeltaAngle(closedZ, rule.openZ);
            toggleBindings.Add(new ToggleBinding { Door = door, ClosedZ = closedZ, OpenZ = openZ, OneShot = rule.oneShot });
        }

        if (debugReset)
            Debug.Log($"[DoorReset] Tracking {toggleBindings.Count} door(s) for toggle on selection.");
    }

    void BuildWrongActionBindings()
    {
        wrongActionDoors.Clear();

        if (wrongActionDoorNames == null || wrongActionDoorNames.Length == 0)
            return;

        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (string doorName in wrongActionDoorNames)
        {
            if (string.IsNullOrEmpty(doorName))
                continue;

            Transform door = null;

            foreach (Transform candidate in sceneTransforms)
            {
                if (candidate != null && GameManager.DoorNameMatches(candidate.name, doorName))
                {
                    door = candidate;
                    break;
                }
            }

            if (door == null)
            {
                if (debugReset)
                    Debug.LogWarning($"[DoorReset] No door found for wrong action '{doorName}'.");
                continue;
            }

            wrongActionDoors.Add(door);
        }

        if (debugReset)
            Debug.Log($"[DoorReset] Tracking {wrongActionDoors.Count} door(s) for wrong actions on selection.");
    }

    static Transform ResolveDoor(BlackoutDoorRotationRule rule, Transform[] sceneTransforms)
    {
        if (rule.target != null)
            return rule.target;

        if (string.IsNullOrEmpty(rule.objectName))
            return null;

        // Several rules can share a name (e.g. two D3 doors), so prefer the one
        // whose current Z matches the rule's fromZ, like GameManager does.
        Transform best = null;
        float bestDelta = float.MaxValue;

        foreach (Transform candidate in sceneTransforms)
        {
            if (candidate == null || !GameManager.DoorNameMatches(candidate.name, rule.objectName))
                continue;

            float delta = Mathf.Abs(Mathf.DeltaAngle(candidate.localEulerAngles.z, rule.fromZ));
            if (delta < bestDelta)
            {
                best = candidate;
                bestDelta = delta;
            }
        }

        return bestDelta <= DoorAngleTolerance ? best : null;
    }

    void Subscribe()
    {
        foreach (DoorBinding binding in bindings)
        {
            if (binding.Door == null)
                continue;

            Interactable[] targets = binding.Door.GetComponentsInChildren<Interactable>(true);

            if (targets.Length == 0)
            {
                Interactable created = binding.Door.gameObject.AddComponent<Interactable>();
                created.FollowsHand = false;
                targets = new[] { created };
            }

            foreach (Interactable target in targets)
            {
                if (target == null || handlers.ContainsKey(target))
                    continue;

                Action<Hand> handler = hand => ResetDoor(binding);
                target.SelectEntered += handler;
                target.GrabEntered += handler;
                handlers[target] = handler;
            }
        }

        foreach (ToggleBinding binding in toggleBindings)
        {
            if (binding.Door == null)
                continue;

            Interactable[] targets = binding.Door.GetComponentsInChildren<Interactable>(true);

            if (targets.Length == 0)
            {
                Interactable created = binding.Door.gameObject.AddComponent<Interactable>();
                created.FollowsHand = false;
                targets = new[] { created };
            }

            foreach (Interactable target in targets)
            {
                if (target == null || toggleHandlers.ContainsKey(target))
                    continue;

                Action<Hand> handler = hand => ToggleDoor(binding);
                target.SelectEntered += handler;
                toggleHandlers[target] = handler;
            }
        }

        foreach (Transform door in wrongActionDoors)
        {
            if (door == null)
                continue;

            Interactable[] targets = door.GetComponentsInChildren<Interactable>(true);

            if (targets.Length == 0)
            {
                Interactable created = door.gameObject.AddComponent<Interactable>();
                created.FollowsHand = false;
                targets = new[] { created };
            }

            foreach (Interactable target in targets)
            {
                if (target == null || wrongActionHandlers.ContainsKey(target))
                    continue;

                Action<Hand> handler = hand => SegnalaAzioneErrata();
                target.SelectEntered += handler;
                wrongActionHandlers[target] = handler;
            }
        }
    }

    void Unsubscribe()
    {
        foreach (KeyValuePair<Interactable, Action<Hand>> pair in handlers)
        {
            if (pair.Key == null)
                continue;

            pair.Key.SelectEntered -= pair.Value;
            pair.Key.GrabEntered -= pair.Value;
        }

        handlers.Clear();

        foreach (KeyValuePair<Interactable, Action<Hand>> pair in toggleHandlers)
        {
            if (pair.Key == null)
                continue;

            pair.Key.SelectEntered -= pair.Value;
        }

        toggleHandlers.Clear();

        foreach (KeyValuePair<Interactable, Action<Hand>> pair in wrongActionHandlers)
        {
            if (pair.Key == null)
                continue;

            pair.Key.SelectEntered -= pair.Value;
        }

        wrongActionHandlers.Clear();
    }

    void SegnalaAzioneErrata()
    {
        if (scenario == null)
        {
            scenario = FindFirstObjectByType<BlackoutScenario>();
        }

        if (scenario != null)
        {
            scenario.SegnalaAzioneErrata(wrongActionDescription);
        }
    }

    void ResetDoor(DoorBinding binding)
    {
        if (binding.Door == null)
            return;

        if (binding.Routine != null)
            StopCoroutine(binding.Routine);

        binding.Routine = StartCoroutine(AnimateReset(binding));
    }

    void ToggleDoor(ToggleBinding binding)
    {
        if (binding.Door == null)
            return;

        if (binding.Routine != null)
            StopCoroutine(binding.Routine);

        binding.Routine = StartCoroutine(AnimateToggle(binding));
    }

    IEnumerator AnimateToggle(ToggleBinding binding)
    {
        float targetZ = binding.IsOpen ? binding.ClosedZ : binding.OpenZ;
        binding.IsOpen = !binding.IsOpen;

        if (toggleDuration <= 0f)
        {
            ApplyZ(binding.Door, targetZ);
            binding.Routine = null;

            if (binding.OneShot)
            {
                RimuoviHandlerToggle(binding);
            }

            yield break;
        }

        float startZ = binding.Door.localEulerAngles.z;
        float delta = Mathf.DeltaAngle(startZ, targetZ);

        if (debugReset)
            Debug.Log($"[DoorReset] Toggling '{binding.Door.name}' from Z={startZ:0.##} to Z={targetZ:0.##}.");

        for (float elapsed = 0f; elapsed < toggleDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / toggleDuration));
            ApplyZ(binding.Door, startZ + delta * t);
            yield return null;
        }

        ApplyZ(binding.Door, targetZ);
        binding.Routine = null;

        if (binding.OneShot)
        {
            RimuoviHandlerToggle(binding);
        }
    }

    void RimuoviHandlerToggle(ToggleBinding binding)
    {
        if (binding.Door == null)
            return;

        List<Interactable> daRimuovere = new List<Interactable>();

        foreach (KeyValuePair<Interactable, Action<Hand>> pair in toggleHandlers)
        {
            if (pair.Key == null)
            {
                daRimuovere.Add(pair.Key);
                continue;
            }

            if (pair.Key.transform == binding.Door || pair.Key.transform.IsChildOf(binding.Door))
            {
                pair.Key.SelectEntered -= pair.Value;
                daRimuovere.Add(pair.Key);
            }
        }

        foreach (Interactable key in daRimuovere)
        {
            toggleHandlers.Remove(key);
        }
    }

    IEnumerator AnimateReset(DoorBinding binding)
    {
        if (resetDuration <= 0f)
        {
            ApplyZ(binding, binding.OriginalZ);
            binding.Routine = null;
            yield break;
        }

        float startZ = binding.Door.localEulerAngles.z;
        float delta = Mathf.DeltaAngle(startZ, binding.OriginalZ);

        if (debugReset)
            Debug.Log($"[DoorReset] Resetting '{binding.Door.name}' from Z={startZ:0.##} to Z={binding.OriginalZ:0.##}.");

        for (float elapsed = 0f; elapsed < resetDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / resetDuration));
            ApplyZ(binding, startZ + delta * t);
            yield return null;
        }

        ApplyZ(binding, binding.OriginalZ);

        // If the player interacts while the blackout animation is still running,
        // keep winning the frame until GameManager stops writing to the same doors.
        while (gameManager != null && gameManager.IsDoorRotationActive)
        {
            ApplyZ(binding, binding.OriginalZ);
            yield return null;
        }

        ApplyZ(binding, binding.OriginalZ);
        binding.Routine = null;
    }

    static void ApplyZ(DoorBinding binding, float z)
    {
        ApplyZ(binding.Door, z);
    }

    static void ApplyZ(Transform door, float z)
    {
        Vector3 euler = door.localEulerAngles;
        euler.z = z;
        door.localEulerAngles = euler;
    }
}
