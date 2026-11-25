using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Blackout")]
    [SerializeField] float blackoutDelay = 5f;
    [SerializeField] AudioClip blackoutSound;

    public event Action OnBlackout;

    AudioSource audioSource;
    AudioClip placeholderAlarm;
    Coroutine blackoutRoutine;


    void Start()
    {
        OnBlackout += DisableSceneLights;
        OnBlackout += PlayBlackoutSound;
        OnBlackout += ApplyBlackoutLook;
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

        if (blackoutRoutine != null)
        {
            StopCoroutine(blackoutRoutine);
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
