using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BlackoutDoorRotationRule
{
    [Tooltip("GameObject name in the scene (exact, or with a suffix).")]
    public string objectName;

    [Tooltip("Inspector local Z rotation used to identify this door.")]
    public float fromZ;

    [Tooltip("Inspector local Z rotation after blackout.")]
    public float toZ;

    [Tooltip("Optional direct reference. If set, name/from Z matching is skipped.")]
    public Transform target;
}

public class GameManager : MonoBehaviour
{
    [Header("Blackout")]
    [SerializeField] float blackoutDelay = 5f;
    [SerializeField] AudioClip blackoutSound;

    [Header("Blackout doors (local Z only)")]
    [SerializeField] BlackoutDoorRotationRule[] doorRotationRules =
    {
        new BlackoutDoorRotationRule { objectName = "porta_tagliafuoco_D3", fromZ = 90f, toZ = 0f },
        new BlackoutDoorRotationRule { objectName = "porta_tagliafuoco_D3", fromZ = 180f, toZ = 90f },
        new BlackoutDoorRotationRule { objectName = "porta_tagliafuoco_D12", fromZ = 0f, toZ = -90f },
        new BlackoutDoorRotationRule { objectName = "porta_tagliafuoco_D5", fromZ = 0f, toZ = 90f },
        new BlackoutDoorRotationRule { objectName = "porta_tagliafuoco_D6", fromZ = 90f, toZ = 180f },
        new BlackoutDoorRotationRule { objectName = "porta_tagliafuoco_D7", fromZ = 90f, toZ = 0f }
    };

    [SerializeField, Min(0.01f)] float doorRotationDuration = 0.6f;
    [SerializeField, Min(0f)] float doorRotationStartDelay = 0.1f;
    [SerializeField] bool debugDoorRotation = true;

    const float DoorAngleTolerance = 5f;

    public event Action OnBlackout;

    AudioSource audioSource;
    AudioClip placeholderAlarm;
    Coroutine blackoutRoutine;
    Coroutine doorRotationRoutine;


    void Start()
    {
        OnBlackout += DisableSceneLights;
        OnBlackout += PlayBlackoutSound;
        OnBlackout += ApplyBlackoutLook;
        OnBlackout += AnimateBlackoutDoors;
        blackoutRoutine = StartCoroutine(TriggerBlackoutAfter(blackoutDelay));
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
    }

    void OnDestroy()
    {
        OnBlackout -= DisableSceneLights;
        OnBlackout -= PlayBlackoutSound;
        OnBlackout -= ApplyBlackoutLook;
        OnBlackout -= AnimateBlackoutDoors;

        if (blackoutRoutine != null)
        {
            StopCoroutine(blackoutRoutine);
        }

        if (doorRotationRoutine != null)
        {
            StopCoroutine(doorRotationRoutine);
        }
    }

    IEnumerator TriggerBlackoutAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        OnBlackout?.Invoke();
    }

    void DisableSceneLights()
    {
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            light.enabled = false;
    }

    void AnimateBlackoutDoors()
    {
        if (doorRotationRoutine != null)
            return;

        doorRotationRoutine = StartCoroutine(AnimateDoors(doorRotationDuration));
    }

    IEnumerator AnimateDoors(float duration)
    {
        if (doorRotationRules == null)
        {
            doorRotationRoutine = null;
            yield break;
        }

        if (doorRotationStartDelay > 0f)
            yield return new WaitForSecondsRealtime(doorRotationStartDelay);

        var targets = new List<BlackoutDoorTarget>();
        float safeDuration = Mathf.Max(0.01f, duration);
        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (BlackoutDoorRotationRule rule in doorRotationRules)
        {
            if (rule.target != null)
            {
                float directZ = rule.target.localEulerAngles.z;
                if (Mathf.Abs(Mathf.DeltaAngle(directZ, rule.fromZ)) <= DoorAngleTolerance)
                    directZ = rule.fromZ + Mathf.DeltaAngle(rule.fromZ, directZ);

                targets.Add(new BlackoutDoorTarget(rule.target, directZ, rule.toZ));
                continue;
            }

            if (string.IsNullOrEmpty(rule.objectName))
                continue;

            bool matched = false;

            foreach (Transform candidate in sceneTransforms)
            {
                if (candidate == null || !DoorNameMatches(candidate.name, rule.objectName))
                    continue;

                float currentZ = candidate.localEulerAngles.z;
                if (Mathf.Abs(Mathf.DeltaAngle(currentZ, rule.fromZ)) > DoorAngleTolerance)
                {
                    if (debugDoorRotation)
                        Debug.LogWarning($"[BlackoutDoors] Skipped '{candidate.name}' at Z={currentZ:0.##}; expected near {rule.fromZ:0.##}.");
                    continue;
                }

                float startZ = rule.fromZ + Mathf.DeltaAngle(rule.fromZ, currentZ);
                targets.Add(new BlackoutDoorTarget(candidate, startZ, rule.toZ));
                matched = true;
            }

            if (!matched && debugDoorRotation)
                Debug.LogWarning($"[BlackoutDoors] No match for '{rule.objectName}' starting at Z={rule.fromZ:0.##}. {DescribeDoorCandidates(sceneTransforms)}");
        }

        if (targets.Count == 0)
        {
            doorRotationRoutine = null;
            yield break;
        }

        if (debugDoorRotation)
            Debug.Log($"[BlackoutDoors] Animating {targets.Count} door(s) over {safeDuration:0.##}s.");

        for (float elapsed = 0f; elapsed < safeDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));

            foreach (BlackoutDoorTarget target in targets)
                ApplyDoorRotation(target, Mathf.LerpUnclamped(target.FromZ, target.ToZ, t));

            yield return null;
        }

        foreach (BlackoutDoorTarget target in targets)
            ApplyDoorRotation(target, target.ToZ);

        doorRotationRoutine = null;
    }

    static bool DoorNameMatches(string candidateName, string ruleName)
    {
        if (string.Equals(candidateName, ruleName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!candidateName.StartsWith(ruleName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (candidateName.Length == ruleName.Length)
            return true;

        char next = candidateName[ruleName.Length];
        return next == ' ' || next == '.' || next == '(' || next == '_' || next == '-';
    }

    static string DescribeDoorCandidates(Transform[] sceneTransforms)
    {
        var found = new List<string>();

        foreach (Transform candidate in sceneTransforms)
        {
            if (candidate == null || candidate.name.IndexOf("porta_tagliafuoco", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            found.Add($"{candidate.name} (Z={candidate.localEulerAngles.z:0.##})");
        }

        return found.Count == 0
            ? "No 'porta_tagliafuoco' transforms found."
            : $"Found: {string.Join(", ", found.ToArray())}";
    }

    static void ApplyDoorRotation(BlackoutDoorTarget target, float z)
    {
        Vector3 euler = target.Target.localEulerAngles;
        euler.z = z;
        target.Target.localEulerAngles = euler;
    }

    class BlackoutDoorTarget
    {
        public readonly Transform Target;
        public readonly float FromZ;
        public readonly float ToZ;

        public BlackoutDoorTarget(Transform target, float fromZ, float toZ)
        {
            Target = target;
            FromZ = fromZ;
            ToZ = toZ;
        }
    }

    void ApplyBlackoutLook()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.skybox = null;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.02f;

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }
    }

    void PlayBlackoutSound()
    {
        AudioClip clip = blackoutSound;
        if (clip == null)
        {
            if (placeholderAlarm == null)
                placeholderAlarm = CreateFireAlarmPlaceholder();

            clip = placeholderAlarm;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    static AudioClip CreateFireAlarmPlaceholder()
    {
        const int frequency = 44100;
        const float clipDuration = 2f;
        const float toneDuration = 0.25f;
        const float fadeDuration = 0.01f;
        int sampleCount = Mathf.CeilToInt(frequency * clipDuration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)frequency;
            int toneIndex = Mathf.FloorToInt(t / toneDuration);
            float localTime = t - toneIndex * toneDuration;
            float hz = toneIndex % 2 == 0 ? 800f : 600f;
            float envelope = Mathf.Min(1f, Mathf.Min(localTime / fadeDuration, (toneDuration - localTime) / fadeDuration));

            samples[i] = Mathf.Sin(2f * Mathf.PI * hz * localTime) * 0.12f * Mathf.Clamp01(envelope);
        }

        AudioClip clip = AudioClip.Create("FireAlarmPlaceholder", sampleCount, 1, frequency, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
