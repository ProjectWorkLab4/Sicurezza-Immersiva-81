using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Suoni comuni della UI e della HUD: un solo AudioSource 2D e una tabella id -> AudioClip (id = nome dell'asset SFX_).
    /// Null-safe: uno slot senza clip non fa nulla (gli SFX_ non sono ancora stati creati). Ascolta
    /// <see cref="UISettingsRow.FeedbackRequested"/> (SFX_UIToggle, SFX_UIError, HAPTIC_Tick). Volume = master × (effetti o allarmi).
    /// Gli altri componenti chiedono un suono con <c>UIAudioFeedback.Play(UIAudioFeedback.SFX_UI_SELECT)</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("SI81/UI/UI Audio Feedback")]
    public class UIAudioFeedback : MonoBehaviour
    {
        /// <summary>Id suono: passaggio del puntatore.</summary>
        public const string SFX_UI_HOVER = "SFX_UIHover";
        /// <summary>Id suono: selezione.</summary>
        public const string SFX_UI_SELECT = "SFX_UISelect";
        /// <summary>Id suono: conferma.</summary>
        public const string SFX_UI_CONFIRM = "SFX_UIConfirm";
        /// <summary>Id suono: indietro.</summary>
        public const string SFX_UI_BACK = "SFX_UIBack";
        /// <summary>Id suono: interruttore.</summary>
        public const string SFX_UI_TOGGLE = "SFX_UIToggle";
        /// <summary>Id suono: errore.</summary>
        public const string SFX_UI_ERROR = "SFX_UIError";
        /// <summary>Id suono: avviso evento (gruppo allarmi).</summary>
        public const string SFX_HUD_ALERT = "SFX_HUDAlert";
        /// <summary>Id suono: feedback positivo.</summary>
        public const string SFX_HUD_FEEDBACK_POSITIVE = "SFX_HUDFeedbackPositive";
        /// <summary>Id suono: feedback negativo.</summary>
        public const string SFX_HUD_FEEDBACK_NEGATIVE = "SFX_HUDFeedbackNegative";
        /// <summary>Id suono: avviso del timer (gruppo allarmi).</summary>
        public const string SFX_HUD_TIMER_WARNING = "SFX_HUDTimerWarning";
        /// <summary>Id suono: cambio di obiettivo.</summary>
        public const string SFX_HUD_OBJECTIVE = "SFX_HUDObjective";

        /// <summary>Tutti gli id previsti, nell'ordine in cui il builder crea gli slot.</summary>
        public static readonly string[] ALL_IDS =
        {
            SFX_UI_HOVER, SFX_UI_SELECT, SFX_UI_CONFIRM, SFX_UI_BACK, SFX_UI_TOGGLE, SFX_UI_ERROR,
            SFX_HUD_ALERT, SFX_HUD_FEEDBACK_POSITIVE, SFX_HUD_FEEDBACK_NEGATIVE, SFX_HUD_TIMER_WARNING, SFX_HUD_OBJECTIVE
        };

        /// <summary>Uno slot: id (nome dell'asset SFX_) e clip (può essere vuota).</summary>
        [Serializable]
        public class Slot
        {
            /// <summary>Nome dell'asset, es. SFX_UIHover.</summary>
            public string Id;
            /// <summary>Clip audio (vuota finché l'asset non esiste).</summary>
            public AudioClip Clip;
        }

        [Header("Slot SFX_ (clip vuote = silenzio)")]
        [SerializeField] private List<Slot> slots = new List<Slot>();
        [Header("Riferimenti")]
        [SerializeField] private AudioSource source;

        // Tabella id -> clip.
        private Dictionary<string, AudioClip> lookup;

        /// <summary>Istanza unica (null se MNG_HUD non è in scena).</summary>
        public static UIAudioFeedback Instance { get; private set; }

        /// <summary>
        /// Registra l'istanza, prepara l'AudioSource 2D e ascolta le richieste delle righe di Impostazioni.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            if (source == null) source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            BuildLookup();
        }

        /// <summary>
        /// Si iscrive agli eventi di feedback.
        /// </summary>
        private void OnEnable()
        {
            UISettingsRow.FeedbackRequested += HandleFeedbackRequested;
        }

        /// <summary>
        /// Si disiscrive.
        /// </summary>
        private void OnDisable()
        {
            UISettingsRow.FeedbackRequested -= HandleFeedbackRequested;
        }

        /// <summary>
        /// Toglie l'istanza.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Azzera l'istanza statica all'avvio del Play Mode (Domain Reload disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Instance = null;
        }

        /// <summary>
        /// Suona il clip di un id. Null-safe: senza istanza, slot o clip non fa nulla.
        /// </summary>
        /// <param name="sfxId">Id (nome SFX_ dell'asset).</param>
        public static void Play(string sfxId)
        {
            if (Instance != null) Instance.PlayId(sfxId);
        }

        /// <summary>
        /// Suona il clip di un id con volume da impostazioni (allarmi per SFX_HUDAlert e SFX_HUDTimerWarning, altrimenti effetti).
        /// </summary>
        /// <param name="sfxId">Id dello slot.</param>
        public void PlayId(string sfxId)
        {
            if (lookup == null) BuildLookup();
            if (string.IsNullOrEmpty(sfxId) || !lookup.TryGetValue(sfxId, out AudioClip clip) || clip == null || source == null) return;
            bool alarm = sfxId == SFX_HUD_ALERT || sfxId == SFX_HUD_TIMER_WARNING;
            source.PlayOneShot(clip, GetVolume(alarm));
        }

        /// <summary>
        /// Volume 0..1: master × gruppo (allarmi o effetti), dalle impostazioni (predefinito 100% se mancano).
        /// </summary>
        /// <param name="alarm">true per il gruppo allarmi.</param>
        /// <returns>Volume lineare.</returns>
        private float GetVolume(bool alarm)
        {
            IUISettingsStore store = UIServices.Settings;
            if (store == null) return 1f;
            float master = store.GetFloat(SettingsCatalog.AUDIO_MASTER_VOLUME) / 100f;
            float group = store.GetFloat(alarm ? SettingsCatalog.AUDIO_ALARMS : SettingsCatalog.AUDIO_EFFECTS) / 100f;
            return Mathf.Clamp01(master * group);
        }

        /// <summary>
        /// Costruisce la tabella id -> clip dagli slot.
        /// </summary>
        private void BuildLookup()
        {
            lookup = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] != null && !string.IsNullOrEmpty(slots[i].Id)) lookup[slots[i].Id] = slots[i].Clip;
        }

        /// <summary>
        /// Richiesta di una riga di Impostazioni: suono SFX_ oppure tick aptico.
        /// </summary>
        /// <param name="feedbackId">Id (SFX_UIToggle, SFX_UIError, HAPTIC_Tick).</param>
        private void HandleFeedbackRequested(string feedbackId)
        {
            if (feedbackId == UISettingsRow.FEEDBACK_TICK)
            {
                if (HapticsService.Instance != null) HapticsService.Instance.PulseTick();
                return;
            }
            PlayId(feedbackId);
        }

        /// <summary>
        /// Imposta slot e AudioSource. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(AudioSource newSource, List<Slot> newSlots)
        {
            source = newSource;
            slots = newSlots;
        }
    }
}
