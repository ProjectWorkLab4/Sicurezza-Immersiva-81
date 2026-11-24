using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SI81.UI
{
    /// <summary>
    /// Riga PANEL_SettingsToggle: etichetta + testo ON/OFF (Bold, sempre scritto: lo stato non è solo colore) + interruttore
    /// (traccia 56 × 32 e manopola bianca di 24). Acceso: traccia state.active e manopola a destra; spento: traccia text.secondary
    /// e manopola a sinistra; disabilitato: aspetto spento con riga al 40%. Tutta la riga è il bersaglio del clic (raggio del controller).
    /// Hover/pressed/ombra: UIStateVisual sulla radice (scala 1,04 / 0,98 + ombra). Suono SFX_UIToggle richiesto con
    /// <see cref="UISettingsRow.FeedbackRequested"/>. Il valore vive nello store (<see cref="UISettingsRow.Bind"/>).
    /// </summary>
    public class SettingsToggleRow : UISettingsRow, IPointerClickHandler
    {
        /// <summary>Chiave del testo "ON".</summary>
        public const string STATE_ON_KEY = "ui.settings.toggle.on";
        /// <summary>Chiave del testo "OFF".</summary>
        public const string STATE_OFF_KEY = "ui.settings.toggle.off";

        [Header("Toggle")]
        [Tooltip("Valore predefinito dell'impostazione (usato anche da 'Ripristina predefiniti').")]
        [SerializeField] private bool defaultValue;
        [SerializeField] private RoundedPanelGraphic track;
        [SerializeField] private RoundedPanelGraphic knob;
        [SerializeField] private UIStringText stateLabel;

        // Valore locale quando la riga non è collegata a uno store (parte dal predefinito alla prima lettura).
        private bool localValue;
        private bool localInitialized;
        // Animazioni in corso.
        private Coroutine trackRoutine;
        private Coroutine knobRoutine;

        /// <summary>Lanciato quando l'utente (o il codice) cambia il valore; parametro: nuovo valore.</summary>
        public event Action<bool> ValueChanged;

        /// <summary>Valore corrente (dallo store se collegato).</summary>
        public bool Value => Store != null ? Store.GetBool(SettingId) : LocalValue;

        /// <summary>Valore locale: alla prima lettura parte dal predefinito (vale anche in Editor, dove Awake non gira).</summary>
        private bool LocalValue
        {
            get
            {
                if (!localInitialized)
                {
                    localValue = defaultValue;
                    localInitialized = true;
                }
                return localValue;
            }
        }

        /// <summary>
        /// Imposta il predefinito della riga da una definizione a runtime (poi <see cref="UISettingsRow.SetRowData"/> e <see cref="UISettingsRow.Bind"/>).
        /// </summary>
        /// <param name="newDefault">Valore predefinito.</param>
        public void SetToggleData(bool newDefault)
        {
            defaultValue = newDefault;
            localInitialized = false;
        }

        /// <summary>
        /// Scrive il nuovo valore (nello store se collegato) e avvisa chi ascolta. Nessun suono (lo chiede solo il clic).
        /// </summary>
        /// <param name="value">Nuovo valore.</param>
        public void SetValue(bool value)
        {
            if (Value == value) return;
            if (Store != null) Store.SetBool(SettingId, value);
            else localValue = value;
            localInitialized = true;
            RefreshFromStore(false);
            ValueChanged?.Invoke(value);
        }

        /// <summary>
        /// Inverte il valore (se la riga è interattiva) e richiede il suono SFX_UIToggle.
        /// </summary>
        public void Toggle()
        {
            if (!Interactable) return;
            SetValue(!Value);
            RaiseFeedback(FEEDBACK_TOGGLE);
        }

        /// <summary>
        /// Clic del puntatore (grilletto rilasciato sulla riga): inverte il valore.
        /// </summary>
        /// <param name="eventData">Dati del puntatore.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Toggle();
        }

        /// <inheritdoc/>
        protected override float GetControlsWidth()
        {
            if (theme == null) return 0f;
            return theme.GetNumber(SettingsTokens.TOGGLE_STATE_WIDTH) + theme.GetNumber(SettingsTokens.ROW_GAP)
                   + theme.GetNumber(SettingsTokens.TOGGLE_TRACK_WIDTH);
        }

        /// <summary>
        /// Posiziona traccia (a destra, padding 8), testo ON/OFF (a sinistra della traccia, gap 12) e manopola; colora la manopola e il testo.
        /// </summary>
        protected override void ApplyControls()
        {
            float padRight = theme.GetNumber(SettingsTokens.ROW_PADDING_RIGHT);
            float gap = theme.GetNumber(SettingsTokens.ROW_GAP);
            float trackW = theme.GetNumber(SettingsTokens.TOGGLE_TRACK_WIDTH);
            float trackH = theme.GetNumber(SettingsTokens.TOGGLE_TRACK_HEIGHT);
            float knobSize = theme.GetNumber(SettingsTokens.TOGGLE_KNOB);
            float stateW = theme.GetNumber(SettingsTokens.TOGGLE_STATE_WIDTH);

            if (track != null) SetFixedRect(track.rectTransform, new Vector2(1f, 0.5f), new Vector2(-padRight, 0f), new Vector2(trackW, trackH));
            if (knob != null)
            {
                SetFixedRect(knob.rectTransform, new Vector2(0f, 0.5f), new Vector2(KnobX(Value && Interactable), 0f), new Vector2(knobSize, knobSize));
                if (knob.color != theme.TextOnDark) knob.color = theme.TextOnDark;
            }
            if (stateLabel != null)
            {
                SetFixedRect((RectTransform)stateLabel.transform, new Vector2(1f, 0.5f), new Vector2(-(padRight + trackW + gap), 0f),
                    new Vector2(stateW, PanelLayout.LineHeight(theme.BodyBold)));
                stateLabel.SetStyle(TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY);
            }
        }

        /// <summary>
        /// Posizione X (bordo sinistro) della manopola dentro la traccia: a sinistra (spento) o a destra (acceso), con lo stesso margine.
        /// </summary>
        /// <param name="on">true = acceso.</param>
        /// <returns>Posizione in unità Canvas.</returns>
        private float KnobX(bool on)
        {
            float trackW = theme.GetNumber(SettingsTokens.TOGGLE_TRACK_WIDTH);
            float inset = (theme.GetNumber(SettingsTokens.TOGGLE_TRACK_HEIGHT) - theme.GetNumber(SettingsTokens.TOGGLE_KNOB)) * 0.5f;
            return on ? trackW - inset - theme.GetNumber(SettingsTokens.TOGGLE_KNOB) : inset;
        }

        /// <summary>
        /// Mostra il valore: colore della traccia, posizione della manopola, testo ON/OFF. Disabilitato = aspetto spento.
        /// </summary>
        /// <param name="instant">true = senza animazione.</param>
        protected override void RefreshFromStore(bool instant)
        {
            if (theme == null || track == null) return;
            bool shownOn = Value && Interactable;
            if (stateLabel != null)
            {
                string key = shownOn ? STATE_ON_KEY : STATE_OFF_KEY;
                if (stateLabel.Key != key) stateLabel.SetKey(key);
            }
            Color trackColor = shownOn ? theme.StateActive : theme.TextSecondary;
            float knobX = KnobX(shownOn);
            float duration = instant || !Application.isPlaying ? 0f : theme.Motion.SelectDuration;
            if (duration <= 0f || !isActiveAndEnabled)
            {
                trackRoutine = null;
                knobRoutine = null;
                if (track.color != trackColor) track.color = trackColor;
                if (knob != null) knob.rectTransform.anchoredPosition = new Vector2(knobX, 0f);
                return;
            }
            trackRoutine = UITween.Play(this, trackRoutine, UITween.ColorTo(track, trackColor, duration, () => trackRoutine = null));
            if (knob != null) knobRoutine = UITween.Play(this, knobRoutine, SlideKnob(knobX, duration));
        }

        /// <summary>
        /// Fa scorrere la manopola (anchoredPosition.x) verso il valore finale, a tempo non scalato (UITween non ha un movimento).
        /// </summary>
        /// <param name="targetX">Posizione X finale.</param>
        /// <param name="duration">Durata in secondi.</param>
        private IEnumerator SlideKnob(float targetX, float duration)
        {
            RectTransform rect = knob.rectTransform;
            float startX = rect.anchoredPosition.x;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, Mathf.Clamp01(elapsed / duration)), 0f);
                yield return null;
            }
            rect.anchoredPosition = new Vector2(targetX, 0f);
            knobRoutine = null;
        }

        /// <summary>
        /// Disabilitato = aspetto spento: rilegge il valore mostrato.
        /// </summary>
        protected override void OnInteractableChanged()
        {
            RefreshFromStore(true);
        }

        /// <inheritdoc/>
        protected override void RegisterDefaults(IUISettingsDefaults defaults)
        {
            defaults.RegisterDefaultBool(SettingId, defaultValue);
        }

        /// <summary>
        /// Imposta i riferimenti propri e il predefinito. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureToggle(RoundedPanelGraphic newTrack, RoundedPanelGraphic newKnob, UIStringText newStateLabel, bool newDefault)
        {
            track = newTrack;
            knob = newKnob;
            stateLabel = newStateLabel;
            defaultValue = newDefault;
        }
    }
}
