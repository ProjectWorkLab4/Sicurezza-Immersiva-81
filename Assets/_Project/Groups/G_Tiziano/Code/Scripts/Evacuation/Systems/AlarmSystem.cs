using System;
using UnityEngine;
using UnityEngine.Events;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.Evacuation.Core;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Evacuation alarm. Active for the whole Action phase: loops an optional clip and pulses the
    /// emission of the assigned renderers through a MaterialPropertyBlock (no extra realtime lights).
    /// </summary>
    public sealed class AlarmSystem : MonoBehaviour, IResettable
    {
        private const float MAX_PULSE_HZ = 3f;
        private static readonly int EMISSION_ID = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private EvacuationDirector director;

        [Header("Sound (optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip alarmClip;

        [Header("Light pulse")]
        [SerializeField] private Renderer[] pulseRenderers = new Renderer[0];
        [SerializeField, ColorUsage(false, true)] private Color pulseColor = new Color(2.5f, 0.1f, 0.05f);
        [SerializeField, Range(0.1f, MAX_PULSE_HZ)] private float pulseHz = 1.5f;

        [Header("Hooks")]
        [SerializeField] private UnityEvent onAlarmStarted = new UnityEvent();
        [SerializeField] private UnityEvent onAlarmStopped = new UnityEvent();

        private MaterialPropertyBlock block;
        private bool isActive;
        private float startTime;

        /// <summary>Raised whenever the alarm turns on (true) or off (false).</summary>
        public event Action<bool> AlarmStateChanged;

        /// <summary>True while the alarm is sounding and pulsing.</summary>
        public bool IsActive => isActive;
        /// <summary>Pulse frequency in Hz, never above 3 Hz.</summary>
        public float PulseHz => Mathf.Min(pulseHz, MAX_PULSE_HZ);
        /// <summary>Hook invoked when the alarm starts.</summary>
        public UnityEvent OnAlarmStarted => onAlarmStarted;
        /// <summary>Hook invoked when the alarm stops.</summary>
        public UnityEvent OnAlarmStopped => onAlarmStopped;

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            ApplyEmission(Color.black);
        }

        private void OnEnable()
        {
            if (director == null)
            {
                UDebug.LogWarning("[EV] AlarmSystem: EvacuationDirector not assigned.", this);
                return;
            }

            director.PhaseChanged += HandlePhaseChanged;
            director.RegisterResettable(this);
        }

        private void Start()
        {
            if (director != null)
                SetAlarm(director.Phase == EvacPhase.Action);
        }

        private void OnDisable()
        {
            SetAlarm(false);
            if (director == null)
                return;

            director.PhaseChanged -= HandlePhaseChanged;
            director.UnregisterResettable(this);
        }

        private void Update()
        {
            if (!isActive)
                return;

            float wave = 0.5f - 0.5f * Mathf.Cos((Time.time - startTime) * PulseHz * Mathf.PI * 2f);
            ApplyEmission(pulseColor * wave);
        }

        /// <summary>Turns the alarm on or off (the Director phase drives it; debug commands may force it).</summary>
        public void SetAlarm(bool active)
        {
            if (isActive == active)
                return;

            isActive = active;
            if (active)
            {
                startTime = Time.time;
                PlayClip();
            }
            else
            {
                StopClip();
                ApplyEmission(Color.black);
            }

            UDebug.Log($"[EV] Alarm {(active ? "ON" : "OFF")}");
            AlarmStateChanged?.Invoke(active);
            if (director != null)
                director.ReportAlarmState(active);
            (active ? onAlarmStarted : onAlarmStopped).Invoke();
        }

        /// <summary>IResettable: silences the alarm and clears the emission.</summary>
        public void ResetState()
        {
            SetAlarm(false);
            ApplyEmission(Color.black);
        }

        private void HandlePhaseChanged(EvacPhase phase)
        {
            SetAlarm(phase == EvacPhase.Action);
        }

        private void PlayClip()
        {
            if (audioSource == null || alarmClip == null)
                return;

            audioSource.clip = alarmClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        private void StopClip()
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        private void ApplyEmission(Color color)
        {
            if (pulseRenderers == null)
                return;
            if (block == null)
                block = new MaterialPropertyBlock();

            for (int i = 0; i < pulseRenderers.Length; i++)
            {
                Renderer target = pulseRenderers[i];
                if (target == null)
                    continue;

                target.GetPropertyBlock(block);
                block.SetColor(EMISSION_ID, color);
                target.SetPropertyBlock(block);
            }
        }
    }
}
