using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Serializable UnityEvent carrying a float (seconds).</summary>
[Serializable]
public class FloatEvent : UnityEvent<float>
{
}

/// <summary>
/// Countdown timer for the VR experience.
///
/// While running it updates an optional label (a TMP text on the wall / panel)
/// and, when the time runs out, prints a message on the VR screen through
/// <see cref="VRMessageDisplay"/> and fires <see cref="onExpired"/>.
///
/// It can be started from the Inspector (autoStart), by code
/// (<see cref="StartTimer()"/>) or by poking one of the startButtons.
/// </summary>
[DisallowMultipleComponent]
public class CountdownTimer : MonoBehaviour
{
    [Header("Tempo")]
    [Tooltip("Durata in secondi usata da StartTimer() e all'avvio.")]
    [SerializeField] float durationSeconds = 120f;
    [SerializeField] bool autoStart = false;
    [SerializeField] bool loop = false;
    [Tooltip("Ritardo prima di far ripartire il timer quando loop e' attivo.")]
    [SerializeField] float loopDelay = 2f;

    [Header("Interazione")]
    [Tooltip("Pulsanti che avviano il timer (VRInteractableButton).")]
    [SerializeField] VRInteractableButton[] startButtons = new VRInteractableButton[0];
    [SerializeField] bool restartOnPress = false;

    [Header("Visualizzazione")]
    [Tooltip("Etichetta TMP che mostra il tempo rimanente. Opzionale.")]
    [SerializeField] TMP_Text countdownLabel;
    [Tooltip("Testo mostrato prima del tempo, es. 'Tempo rimasto: '.")]
    [SerializeField] string labelPrefix = "";
    [SerializeField] string finalLabel = "00:00";

    [Header("Schermo VR")]
    [SerializeField] bool showOnVRScreen = true;
    [SerializeField] VRMessageDisplay display;
    [SerializeField] string expiredTitle = "TEMPO SCADUTO";
    [Tooltip("Messaggio stampato sullo schermo VR allo scadere del tempo.")]
    [TextArea(2, 5)]
    [SerializeField] string expiredMessage = "Il tempo a disposizione e' terminato. Mettersi in sicurezza ed evacuare l'area.";
    [SerializeField] float expiredMessageDuration = 6f;

    [Header("Avviso (opzionale)")]
    [Tooltip("Secondi rimanenti ai quali mostrare un avviso. 0 = nessun avviso.")]
    [SerializeField] float warningAtSeconds = 10f;
    [SerializeField] string warningMessage = "Attenzione: il tempo sta per scadere.";

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip startClip;
    [SerializeField] AudioClip warningClip;
    [SerializeField] AudioClip expiredClip;

    [Header("Eventi")]
    [SerializeField] UnityEvent onStarted = new UnityEvent();
    [SerializeField] UnityEvent onExpired = new UnityEvent();
    [SerializeField] FloatEvent onTick = new FloatEvent();

    /// <summary>Fired when the countdown reaches zero.</summary>
    public event Action Expired;

    /// <summary>Fired when the countdown starts.</summary>
    public event Action Started;

    public float Remaining { get; private set; }
    public bool IsRunning { get; private set; }

    Coroutine _restartRoutine;
    bool _warningPlayed;
    int _lastDisplayedSecond = -1;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        Remaining = Mathf.Max(0.1f, durationSeconds);
    }

    void Start()
    {
        UpdateLabel(force: true);

        if (autoStart)
        {
            StartTimer();
        }
    }

    void OnEnable()
    {
        if (startButtons == null)
        {
            return;
        }

        foreach (VRInteractableButton button in startButtons)
        {
            if (button != null)
            {
                button.Selected += HandleStartButton;
            }
        }
    }

    void OnDisable()
    {
        if (startButtons == null)
        {
            return;
        }

        foreach (VRInteractableButton button in startButtons)
        {
            if (button != null)
            {
                button.Selected -= HandleStartButton;
            }
        }
    }

    void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        float previous = Remaining;
        Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);

        if (Mathf.CeilToInt(previous) != Mathf.CeilToInt(Remaining))
        {
            UpdateLabel(force: false);
            onTick.Invoke(Remaining);

            if (!_warningPlayed && warningAtSeconds > 0f && Remaining <= warningAtSeconds && Remaining > 0f)
            {
                _warningPlayed = true;
                PlayClip(warningClip);
                ShowOnVRScreen(expiredTitle, warningMessage, 3f);
            }
        }

        if (Remaining <= 0f)
        {
            Expire();
        }
    }

    /// <summary>Starts (or restarts) the countdown with the configured duration.</summary>
    public void StartTimer()
    {
        StartTimer(durationSeconds);
    }

    /// <summary>Starts (or restarts) the countdown with an explicit duration.</summary>
    public void StartTimer(float seconds)
    {
        CancelRestart();

        durationSeconds = Mathf.Max(0.1f, seconds);
        Remaining = durationSeconds;
        IsRunning = true;
        _warningPlayed = false;
        _lastDisplayedSecond = -1;

        PlayClip(startClip);
        UpdateLabel(force: true);

        onStarted.Invoke();
        Started?.Invoke();
    }

    /// <summary>Pauses the countdown keeping the remaining time.</summary>
    public void Pause()
    {
        IsRunning = false;
    }

    /// <summary>Resumes a paused countdown.</summary>
    public void Resume()
    {
        if (Remaining > 0f)
        {
            IsRunning = true;
        }
    }

    /// <summary>Stops the countdown and resets the remaining time to zero.</summary>
    public void Stop()
    {
        CancelRestart();
        IsRunning = false;
        Remaining = 0f;
        UpdateLabel(force: true);
    }

    /// <summary>Stops the countdown and restores the configured duration.</summary>
    public void ResetTimer()
    {
        CancelRestart();
        IsRunning = false;
        Remaining = Mathf.Max(0.1f, durationSeconds);
        _warningPlayed = false;
        UpdateLabel(force: true);
    }

    /// <summary>Adds (or removes, with a negative value) seconds to the countdown.</summary>
    public void AddSeconds(float seconds)
    {
        Remaining = Mathf.Max(0f, Remaining + seconds);
        _warningPlayed = Remaining <= warningAtSeconds;
        UpdateLabel(force: true);

        if (Remaining <= 0f && IsRunning)
        {
            Expire();
        }
    }

    /// <summary>Returns the remaining time formatted as mm:ss.</summary>
    public string GetFormattedTime()
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(Remaining));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    void Expire()
    {
        if (!IsRunning && Remaining <= 0f && _lastDisplayedSecond == 0)
        {
            return;
        }

        IsRunning = false;
        Remaining = 0f;
        UpdateLabel(force: true);

        PlayClip(expiredClip);
        ShowOnVRScreen(expiredTitle, expiredMessage, expiredMessageDuration);

        onExpired.Invoke();
        Expired?.Invoke();

        if (loop)
        {
            _restartRoutine = StartCoroutine(RestartAfterDelay());
        }
    }

    IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(loopDelay);
        _restartRoutine = null;
        StartTimer();
    }

    void CancelRestart()
    {
        if (_restartRoutine != null)
        {
            StopCoroutine(_restartRoutine);
            _restartRoutine = null;
        }
    }

    void UpdateLabel(bool force)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(Remaining));

        if (!force && totalSeconds == _lastDisplayedSecond)
        {
            return;
        }

        _lastDisplayedSecond = totalSeconds;

        if (countdownLabel != null)
        {
            countdownLabel.text = totalSeconds <= 0 && !IsRunning
                ? finalLabel
                : labelPrefix + GetFormattedTime();
        }
    }

    void ShowOnVRScreen(string title, string body, float duration)
    {
        if (!showOnVRScreen || string.IsNullOrEmpty(body))
        {
            return;
        }

        VRMessageDisplay target = display != null ? display : VRMessageDisplay.GetOrCreate();
        target.Show(title, body, duration);
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void HandleStartButton(VRInteractableButton button)
    {
        if (restartOnPress || !IsRunning)
        {
            StartTimer();
        }
    }
}
