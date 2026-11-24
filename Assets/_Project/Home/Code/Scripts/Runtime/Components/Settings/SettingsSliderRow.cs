using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Riga PANEL_SettingsSlider: etichetta + valore numerico Bold + slider (traccia 200 × 12 tile.neutral, riempimento brand.dark,
    /// maniglia circolare brand.dark di 28, 32 in hover o trascinamento). Intervallo min/max/passo configurabili da dati; il valore
    /// mostrato si formatta con una chiave testo opzionale (es. "ui.format.percent" → "92%"; vuota = numero semplice).
    /// Il puntatore del controller (grilletto) trascina la maniglia sull'area di presa alta 40 (<see cref="SettingsSliderGrab"/>).
    /// Impulso aptico (FEEDBACK_TICK) ogni <see cref="SettingsTokens.SLIDER_TICK_STEP"/> unità del valore (10% su 0–100).
    /// La maniglia resta sempre dentro la traccia: il suo centro corre sulla traccia accorciata di mezza maniglia per lato.
    /// </summary>
    public class SettingsSliderRow : UISettingsRow
    {
        [Header("Slider")]
        [Tooltip("Valore predefinito (usato anche da 'Ripristina predefiniti').")]
        [SerializeField] private float defaultValue = 80f;
        [Tooltip("Valore minimo.")]
        [SerializeField] private float minValue;
        [Tooltip("Valore massimo.")]
        [SerializeField] private float maxValue = 100f;
        [Tooltip("Passo: il valore si arrotonda a multipli del passo a partire dal minimo (0 = continuo).")]
        [SerializeField] private float step = 1f;
        [Tooltip("Chiave del formato del valore mostrato, es. ui.format.percent (vuota = numero semplice).")]
        [SerializeField] private string valueFormatKey;
        [SerializeField] private RoundedPanelGraphic track;
        [SerializeField] private RoundedPanelGraphic fillBar;
        [SerializeField] private RoundedPanelGraphic handle;
        [SerializeField] private SettingsSliderGrab grab;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private UIStringText valueStyle;

        // Valore locale quando la riga non è collegata a uno store (parte dal predefinito alla prima lettura).
        private float localValue;
        private bool localInitialized;
        // Stato del puntatore sulla maniglia.
        private bool hovered;
        private bool dragging;
        // Animazione della maniglia in corso.
        private Coroutine handleRoutine;

        /// <summary>Lanciato quando il valore cambia; parametro: nuovo valore (già arrotondato al passo).</summary>
        public event Action<float> ValueChanged;

        /// <summary>Valore minimo.</summary>
        public float MinValue => minValue;
        /// <summary>Valore massimo.</summary>
        public float MaxValue => maxValue;
        /// <summary>Valore corrente (dallo store se collegato).</summary>
        public float Value => Store != null ? Store.GetFloat(SettingId) : LocalValue;

        /// <summary>Valore locale: alla prima lettura parte dal predefinito (vale anche in Editor, dove Awake non gira).</summary>
        private float LocalValue
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
        /// Azzera lo stato del puntatore quando la riga si disattiva (niente maniglia grande rimasta appesa).
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            hovered = false;
            dragging = false;
            handleRoutine = null;
            if (handle != null) handle.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Imposta intervallo, passo, predefinito e formato da una definizione a runtime (poi <see cref="UISettingsRow.SetRowData"/> e <see cref="UISettingsRow.Bind"/>).
        /// </summary>
        /// <param name="newMin">Valore minimo.</param>
        /// <param name="newMax">Valore massimo.</param>
        /// <param name="newStep">Passo (0 = continuo).</param>
        /// <param name="newDefault">Valore predefinito.</param>
        /// <param name="newFormatKey">Chiave del formato del valore mostrato (vuota = numero semplice).</param>
        public void SetSliderData(float newMin, float newMax, float newStep, float newDefault, string newFormatKey = "")
        {
            minValue = newMin;
            maxValue = newMax;
            step = newStep;
            defaultValue = newDefault;
            valueFormatKey = newFormatKey ?? string.Empty;
            localInitialized = false;
        }

        /// <summary>
        /// Imposta il valore (arrotondato al passo e limitato all'intervallo), lo scrive nello store e avvisa chi ascolta.
        /// </summary>
        /// <param name="value">Nuovo valore.</param>
        public void SetValue(float value)
        {
            ChangeValue(value, false);
        }

        /// <summary>
        /// Cambia il valore; con <paramref name="byUser"/> richiede l'impulso aptico quando si attraversa un multiplo del passo-tick.
        /// </summary>
        /// <param name="value">Nuovo valore.</param>
        /// <param name="byUser">true se il cambio viene dal puntatore.</param>
        private void ChangeValue(float value, bool byUser)
        {
            value = Snap(value);
            float current = Value;
            if (Mathf.Approximately(current, value)) return;
            if (byUser && theme != null)
            {
                float tick = theme.GetNumber(SettingsTokens.SLIDER_TICK_STEP);
                if (tick > 0f && Mathf.FloorToInt(current / tick) != Mathf.FloorToInt(value / tick)) RaiseFeedback(FEEDBACK_TICK);
            }
            if (Store != null) Store.SetFloat(SettingId, value);
            else localValue = value;
            localInitialized = true;
            RefreshFromStore(true);
            ValueChanged?.Invoke(value);
        }

        /// <summary>
        /// Arrotonda al passo (a partire dal minimo) e limita a [min, max].
        /// </summary>
        /// <param name="value">Valore grezzo.</param>
        /// <returns>Valore valido.</returns>
        private float Snap(float value)
        {
            if (step > 0f) value = minValue + Mathf.Round((value - minValue) / step) * step;
            return Mathf.Clamp(value, minValue, maxValue);
        }

        /// <summary>
        /// Valore normalizzato 0–1 (0 se l'intervallo è nullo).
        /// </summary>
        private float Normalized(float value)
        {
            return maxValue > minValue ? Mathf.Clamp01((value - minValue) / (maxValue - minValue)) : 0f;
        }

        // =====================================================================
        // Puntatore (chiamato da SettingsSliderGrab)
        // =====================================================================

        /// <summary>
        /// Il puntatore ha premuto o trascinato sull'area di presa: porta il valore sulla posizione del raggio.
        /// </summary>
        /// <param name="eventData">Dati del puntatore.</param>
        internal void SetFromPointer(PointerEventData eventData)
        {
            if (!Interactable || track == null || theme == null) return;
            if (!TryGetTrackX(eventData, out float x)) return;
            float handleSize = theme.GetNumber(SettingsTokens.SLIDER_HANDLE);
            float travel = Mathf.Max(1f, theme.GetNumber(SettingsTokens.SLIDER_TRACK_WIDTH) - handleSize);
            float t = Mathf.Clamp01((x - handleSize * 0.5f) / travel);
            ChangeValue(Mathf.Lerp(minValue, maxValue, t), true);
        }

        /// <summary>
        /// Distanza orizzontale del raggio dal bordo sinistro della traccia, in unità Canvas. Usa il punto colpito nel mondo
        /// (raggio dei controller) e, se manca, la posizione sullo schermo (mouse in Editor).
        /// </summary>
        private bool TryGetTrackX(PointerEventData eventData, out float x)
        {
            RectTransform rect = track.rectTransform;
            x = 0f;
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                Vector3 local = rect.InverseTransformPoint(eventData.pointerCurrentRaycast.worldPosition);
                x = local.x - rect.rect.xMin;
                return true;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 point))
            {
                x = point.x - rect.rect.xMin;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Il puntatore entra/esce dall'area di presa o inizia/finisce il trascinamento: la maniglia passa a 32 (o torna a 28).
        /// </summary>
        /// <param name="isHovered">true se il puntatore è sull'area di presa.</param>
        /// <param name="isDragging">true se si sta trascinando.</param>
        internal void SetPointerState(bool isHovered, bool isDragging)
        {
            if (!Interactable) isHovered = isDragging = false;
            hovered = isHovered;
            dragging = isDragging;
            ApplyHandleSize(false);
        }

        /// <summary>
        /// Porta la scala della maniglia a 32/28 (hover o trascinamento) o a 1, in Motion.HoverDuration (UITween.Scale).
        /// </summary>
        /// <param name="instant">true = senza animazione.</param>
        private void ApplyHandleSize(bool instant)
        {
            if (handle == null || theme == null) return;
            float size = theme.GetNumber(SettingsTokens.SLIDER_HANDLE);
            float target = hovered || dragging ? theme.GetNumber(SettingsTokens.SLIDER_HANDLE_HOVER) / Mathf.Max(1f, size) : 1f;
            float duration = instant || !Application.isPlaying ? 0f : theme.Motion.HoverDuration;
            if (duration <= 0f || !isActiveAndEnabled)
            {
                handleRoutine = null;
                handle.transform.localScale = Vector3.one * target;
                return;
            }
            handleRoutine = UITween.Play(this, handleRoutine, UITween.Scale(handle.transform, target, duration, () => handleRoutine = null));
        }

        // =====================================================================
        // Layout e valore mostrato
        // =====================================================================

        /// <inheritdoc/>
        protected override float GetControlsWidth()
        {
            if (theme == null) return 0f;
            return theme.GetNumber(SettingsTokens.SLIDER_VALUE_WIDTH) + theme.GetNumber(SettingsTokens.ROW_GAP)
                   + theme.GetNumber(SettingsTokens.SLIDER_TRACK_WIDTH);
        }

        /// <summary>
        /// Posiziona traccia (a destra), area di presa, riempimento, maniglia e valore; colora traccia, riempimento e maniglia.
        /// </summary>
        protected override void ApplyControls()
        {
            float padRight = theme.GetNumber(SettingsTokens.ROW_PADDING_RIGHT);
            float gap = theme.GetNumber(SettingsTokens.ROW_GAP);
            float trackW = theme.GetNumber(SettingsTokens.SLIDER_TRACK_WIDTH);
            float trackH = theme.GetNumber(SettingsTokens.SLIDER_TRACK_HEIGHT);
            float handleSize = theme.GetNumber(SettingsTokens.SLIDER_HANDLE);
            float valueW = theme.GetNumber(SettingsTokens.SLIDER_VALUE_WIDTH);

            if (track != null)
            {
                SetFixedRect(track.rectTransform, new Vector2(1f, 0.5f), new Vector2(-padRight, 0f), new Vector2(trackW, trackH));
                if (track.color != theme.TileNeutral) track.color = theme.TileNeutral;
            }
            if (fillBar != null)
            {
                if (fillBar.color != theme.BrandDark) fillBar.color = theme.BrandDark;
                fillBar.rectTransform.anchorMin = fillBar.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                fillBar.rectTransform.pivot = new Vector2(0f, 0.5f);
                fillBar.rectTransform.anchoredPosition = Vector2.zero;
            }
            if (handle != null)
            {
                if (handle.color != theme.BrandDark) handle.color = theme.BrandDark;
                handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                handle.rectTransform.sizeDelta = new Vector2(handleSize, handleSize);
            }
            if (grab != null)
            {
                // Area di presa: larga come la traccia, alta 40 (bersaglio minimo), centrata sulla traccia.
                var grabRect = (RectTransform)grab.transform;
                grabRect.anchorMin = new Vector2(0f, 0.5f);
                grabRect.anchorMax = new Vector2(1f, 0.5f);
                grabRect.pivot = new Vector2(0.5f, 0.5f);
                grabRect.anchoredPosition = Vector2.zero;
                grabRect.sizeDelta = new Vector2(0f, theme.GetNumber(SettingsTokens.SLIDER_GRAB));
                grabRect.localScale = Vector3.one;
            }
            if (valueText != null)
            {
                SetFixedRect(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-(padRight + trackW + gap), 0f),
                    new Vector2(valueW, PanelLayout.LineHeight(theme.BodyBold)));
                if (valueStyle != null) valueStyle.SetStyle(TextStyleIds.BODY_BOLD, ColorTokens.TEXT_PRIMARY);
            }
        }

        /// <summary>
        /// Mostra il valore: larghezza del riempimento, posizione della maniglia e testo formattato.
        /// </summary>
        /// <param name="instant">Non usato (il valore si muove sempre subito, come il dito).</param>
        protected override void RefreshFromStore(bool instant)
        {
            if (theme == null || track == null) return;
            float value = Snap(Value);
            float trackH = theme.GetNumber(SettingsTokens.SLIDER_TRACK_HEIGHT);
            float handleSize = theme.GetNumber(SettingsTokens.SLIDER_HANDLE);
            float travel = Mathf.Max(0f, theme.GetNumber(SettingsTokens.SLIDER_TRACK_WIDTH) - handleSize);
            float centerX = handleSize * 0.5f + Normalized(value) * travel;
            if (fillBar != null) fillBar.rectTransform.sizeDelta = new Vector2(centerX, trackH);
            if (handle != null) handle.rectTransform.anchoredPosition = new Vector2(centerX, 0f);
            if (valueText != null) valueText.text = FormatValue(value);
        }

        /// <summary>
        /// Testo del valore: con la chiave di formato (es. "{0}%") o come numero semplice.
        /// </summary>
        /// <param name="value">Valore da mostrare.</param>
        /// <returns>Testo italiano.</returns>
        private string FormatValue(float value)
        {
            bool whole = Mathf.Approximately(value, Mathf.Round(value));
            if (string.IsNullOrEmpty(valueFormatKey) || strings == null)
                return value.ToString(whole ? "0" : "0.##", CultureInfo.InvariantCulture);
            return whole ? strings.Format(valueFormatKey, Mathf.RoundToInt(value)) : strings.Format(valueFormatKey, value);
        }

        /// <summary>
        /// Disabilitato: niente maniglia grande rimasta appesa.
        /// </summary>
        protected override void OnInteractableChanged()
        {
            if (!Interactable) SetPointerState(false, false);
        }

        /// <inheritdoc/>
        protected override void RegisterDefaults(IUISettingsDefaults defaults)
        {
            defaults.RegisterDefaultFloat(SettingId, defaultValue);
        }

        /// <summary>
        /// Imposta riferimenti e dati propri. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureSlider(RoundedPanelGraphic newTrack, RoundedPanelGraphic newFill, RoundedPanelGraphic newHandle, SettingsSliderGrab newGrab,
            TMP_Text newValueText, UIStringText newValueStyle, float newDefault, float newMin, float newMax, float newStep, string newFormatKey)
        {
            track = newTrack;
            fillBar = newFill;
            handle = newHandle;
            grab = newGrab;
            valueText = newValueText;
            valueStyle = newValueStyle;
            defaultValue = newDefault;
            minValue = newMin;
            maxValue = newMax;
            step = newStep;
            valueFormatKey = newFormatKey;
            grab.Configure(this);
        }
    }

}
