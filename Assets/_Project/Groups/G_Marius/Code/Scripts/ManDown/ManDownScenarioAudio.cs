using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Audio 2D dello scenario: notifica della chiamata al 112 e sirena dell'ambulanza sul risultato finale.
    /// Due AudioSource indipendenti, nessun loop e nessun Play On Awake. La sirena ha fade out con tempo
    /// non scalato, così la conclusione della missione non lo interrompe. Nessuna modifica alla UI comune
    /// né ai suoni del DAE.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/Scenario Audio")]
    public sealed class ManDownScenarioAudio : MonoBehaviour
    {
        [Header("Riferimenti di scena")]
        [SerializeField] private ManDownScenario scenario;

        [Header("Clip")]
        [Tooltip("Notifica della chiamata ai soccorsi, riprodotta alla conferma del 112.")]
        [SerializeField] private AudioClip notificationClip;
        [Tooltip("Sirena dell'ambulanza, riprodotta a fine missione solo se il 112 è stato chiamato.")]
        [SerializeField] private AudioClip ambulanceClip;

        [Header("Volumi")]
        [SerializeField] private float notificationVolume = 0.9f;
        [SerializeField] private float ambulanceVolume = 0.8f;

        [Header("Fade out sirena")]
        [Tooltip("Durata del fade out in secondi (valore iniziale: 3).")]
        [Min(0f)]
        [SerializeField] private float fadeOutSeconds = 3f;

        private AudioSource notificationSource;
        private AudioSource ambulanceSource;
        private bool notificationPlayed;
        private bool ambulancePlayed;
        private bool missionConcluded;
        private float ambulanceStartedAt = -1f;
        private float fadeStartOffset;
        private bool fading;

        private void Awake()
        {
            notificationSource = CreateSource("Notifica112", notificationClip, notificationVolume);
            ambulanceSource = CreateSource("Ambulanza", ambulanceClip, ambulanceVolume);
        }

        private void OnEnable()
        {
            if (scenario == null) scenario = GetComponent<ManDownScenario>();
            if (scenario != null)
            {
                scenario.EmergencyCalled += OnEmergencyCalled;
                scenario.MissionConcluded += OnMissionConcluded;
            }
            MissionSession.Started += OnMissionStarted;
            MissionSession.Ended += OnSessionEnded;
        }

        private void OnDisable()
        {
            if (scenario != null)
            {
                scenario.EmergencyCalled -= OnEmergencyCalled;
                scenario.MissionConcluded -= OnMissionConcluded;
            }
            MissionSession.Started -= OnMissionStarted;
            MissionSession.Ended -= OnSessionEnded;
            StopAll();
        }

        private void OnDestroy()
        {
            StopAll();
        }

        private void Update()
        {
            if (!fading || ambulanceSource == null || ambulanceClip == null) return;

            float elapsed = Time.unscaledTime - ambulanceStartedAt;
            float fadeProgress = Mathf.Clamp01((elapsed - fadeStartOffset) / Mathf.Max(0.01f, fadeOutSeconds));
            ambulanceSource.volume = Mathf.Lerp(ambulanceVolume, 0f, fadeProgress);

            bool ended = elapsed >= ambulanceClip.length || (fadeStartOffset <= 0f && fadeProgress >= 1f);
            if (!ended) return;
            ambulanceSource.Stop();
            ambulanceSource.volume = ambulanceVolume;
            fading = false;
        }

        /// <summary>Chiamata registrata: suono una sola volta per missione, indipendentemente dal telefono.</summary>
        private void OnEmergencyCalled()
        {
            if (notificationPlayed || notificationSource == null || notificationClip == null) return;
            notificationPlayed = true;
            notificationSource.Stop();
            notificationSource.clip = notificationClip;
            notificationSource.volume = notificationVolume;
            notificationSource.Play();
        }

        private void OnMissionConcluded(ManDownScenarioResult result)
        {
            // Segnala che il risultato appartiene a questa missione; la riproduzione avviene con EndMission.
            missionConcluded = true;
        }

        /// <summary>
        /// La UI comune non espone un evento dedicato alla comparsa della schermata esito: il risultato
        /// viene pubblicato qui da EndMission, nello stesso istante in cui la UI avvia la sequenza finale.
        /// </summary>
        private void OnSessionEnded(MissionResult result)
        {
            if (!missionConcluded || ambulancePlayed || scenario == null || !scenario.IsCall112Made) return;
            if (ambulanceSource == null || ambulanceClip == null) return;

            ambulancePlayed = true;
            ambulanceSource.Stop();
            ambulanceSource.clip = ambulanceClip;
            ambulanceSource.volume = ambulanceVolume;
            ambulanceSource.Play();

            ambulanceStartedAt = Time.unscaledTime;
            fadeStartOffset = Mathf.Max(0f, ambulanceClip.length - Mathf.Max(0f, fadeOutSeconds));
            fading = true;
        }

        private void OnMissionStarted()
        {
            StopAll();
        }

        /// <summary>Ferma tutto e ripristina i volumi iniziali per la missione successiva o l'uscita.</summary>
        public void StopAll()
        {
            if (notificationSource != null) { notificationSource.Stop(); notificationSource.volume = notificationVolume; }
            if (ambulanceSource != null) { ambulanceSource.Stop(); ambulanceSource.volume = ambulanceVolume; }
            notificationPlayed = false;
            ambulancePlayed = false;
            missionConcluded = false;
            ambulanceStartedAt = -1f;
            fadeStartOffset = 0f;
            fading = false;
        }

        private AudioSource CreateSource(string name, AudioClip clip, float volume)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = volume;
            return source;
        }
    }
}
