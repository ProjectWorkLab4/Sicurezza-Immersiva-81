using System.Globalization;
using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// LBL_DimensionBar: una delle cinque dimensioni del punteggio sicurezza (Safety, Awareness, Decision, Execution, Time):
    /// nome (chiave ui.dimension.*) + barra 0–100 + valore (senza %).
    /// Misure Figma: 272×23 con nome 88, valore 32, spazio 8, pista alta 10 (contorno 1, pillola) e riempimento pillola.
    /// La larghezza della pista segue quella del componente (ancore, nessun Layout Group). Non interattiva.
    /// Uso: <c>SetDimension(DimensionKind.Awareness, 86)</c>; <c>SetSurface(LabelSurface.Light)</c> su card bianca.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("SI81/UI/Dimension Bar")]
    public class DimensionBar : MonoBehaviour
    {
        /// <summary>Larghezza della colonna del nome (Figma 88).</summary>
        public const float NAME_WIDTH = 88f;
        /// <summary>Larghezza della colonna del valore (Figma 32).</summary>
        public const float VALUE_WIDTH = 32f;
        /// <summary>Spazio tra le colonne (Figma 8).</summary>
        public const float GAP = 8f;
        /// <summary>Altezza della pista (Figma 10).</summary>
        public const float TRACK_HEIGHT = 10f;
        /// <summary>Spessore del contorno della pista (Figma 1).</summary>
        public const float TRACK_STROKE = 1f;
        /// <summary>Valore massimo.</summary>
        public const int MAX_VALUE = 100;
        /// <summary>Prefisso delle chiavi dei nomi: ui.dimension.safety ecc.</summary>
        public const string NAME_KEY_PREFIX = "ui.dimension.";

        [Header("Dati")]
        [Tooltip("Dimensione mostrata (la chiave del nome è ui.dimension.<nome>).")]
        [SerializeField] private DimensionKind dimension = DimensionKind.Safety;
        [Tooltip("Valore da 0 a 100.")]
        [Range(0, MAX_VALUE)]
        [SerializeField] private int value;
        [Tooltip("Fondo su cui sta la barra (colori di nome, pista, riempimento e valore).")]
        [SerializeField] private LabelSurface surface = LabelSurface.Dark;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private RectTransform track;
        [SerializeField] private RoundedRectGraphic trackStroke;
        [SerializeField] private RoundedPanelGraphic fill;
        [SerializeField] private TMP_Text valueText;

        /// <summary>Dimensione mostrata.</summary>
        public DimensionKind Dimension => dimension;
        /// <summary>Valore mostrato (0–100).</summary>
        public int Value => value;

        /// <summary>
        /// Applica dati e stile all'avvio.
        /// </summary>
        private void Awake()
        {
            Apply();
        }

#if UNITY_EDITOR
        /// <summary>
        /// In Editor riapplica quando si cambia un campo nell'Inspector (rimandato a fine aggiornamento).
        /// </summary>
        private void OnValidate()
        {
            UIEditorSupport.ApplyDeferred(this, Apply);
        }
#endif

        /// <summary>
        /// Imposta il valore (limitato a 0–100) e aggiorna barra e testo.
        /// </summary>
        /// <param name="newValue">Valore 0–100.</param>
        public void SetValue(int newValue)
        {
            value = Mathf.Clamp(newValue, 0, MAX_VALUE);
            Apply();
        }

        /// <summary>
        /// Imposta dimensione e valore insieme.
        /// </summary>
        /// <param name="newDimension">Dimensione da mostrare.</param>
        /// <param name="newValue">Valore 0–100.</param>
        public void SetDimension(DimensionKind newDimension, int newValue)
        {
            dimension = newDimension;
            SetValue(newValue);
        }

        /// <summary>
        /// Cambia il fondo (scuro / chiaro) e aggiorna i colori.
        /// </summary>
        /// <param name="newSurface">Fondo su cui sta la barra.</param>
        public void SetSurface(LabelSurface newSurface)
        {
            surface = newSurface;
            Apply();
        }

        /// <summary>
        /// Aggiorna layout, colori, testi e riempimento dai dati correnti.
        /// </summary>
        public void Apply()
        {
            if (theme == null || strings == null || nameText == null || valueText == null || track == null) return;
            bool dark = surface == LabelSurface.Dark;
            Color nameColor = dark ? theme.GetColor(ColorTokens.TEXT_ON_GREEN) : theme.TextPrimary;
            Color valueColor = dark ? theme.TextOnDark : theme.TextPrimary;
            Color trackColor = theme.GetColor(dark ? ColorTokens.LEVEL_BAR_OFF_DARK : ColorTokens.LEVEL_BAR_OFF_LIGHT);
            Color fillColor = dark ? theme.BrandLight : theme.BrandDark;

            // Layout con le ancore: nome a sinistra, valore a destra, pista nel mezzo (nessun Layout Group).
            float height = PanelLayout.LineHeight(theme.Body);
            LabelStyling.SetSize((RectTransform)transform, -1f, height);
            PanelLayout.SetRect(nameText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(NAME_WIDTH, 0f));
            PanelLayout.SetRect(valueText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-VALUE_WIDTH, 0f), new Vector2(0f, 0f));
            PanelLayout.SetRect(track, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(NAME_WIDTH + GAP, -TRACK_HEIGHT * 0.5f), new Vector2(-(VALUE_WIDTH + GAP), TRACK_HEIGHT * 0.5f));

            // Pista (solo contorno) e riempimento = frazione della larghezza della pista.
            if (trackStroke != null)
            {
                trackStroke.Radius = theme.RadiusPill;
                trackStroke.Stroke = TRACK_STROKE;
                LabelStyling.SetColor(trackStroke, trackColor);
                PanelLayout.SetRect(trackStroke.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }
            if (fill != null)
            {
                LabelStyling.SetColor(fill, fillColor);
                PanelLayout.SetRect(fill.rectTransform, Vector2.zero, new Vector2(value / (float)MAX_VALUE, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                PanelLayout.SetActive(fill.gameObject, value > 0);
            }

            // Testi: nome da ui.dimension.<nome>, valore intero senza %.
            string nameKey = NAME_KEY_PREFIX + dimension.ToString().ToLowerInvariant();
            LabelStyling.StyleText(nameText, theme, TextStyleIds.BODY, nameColor);
            LabelStyling.StyleText(valueText, theme, TextStyleIds.BODY, valueColor);
            string label = strings.Get(nameKey);
            if (nameText.text != label) nameText.text = label;
            string number = value.ToString(CultureInfo.InvariantCulture);
            if (valueText.text != number) valueText.text = number;
        }

        /// <summary>
        /// Collega i riferimenti e applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, TMP_Text newNameText, RectTransform newTrack,
            RoundedRectGraphic newTrackStroke, RoundedPanelGraphic newFill, TMP_Text newValueText)
        {
            theme = newTheme;
            strings = newStrings;
            nameText = newNameText;
            track = newTrack;
            trackStroke = newTrackStroke;
            fill = newFill;
            valueText = newValueText;
            Apply();
        }
    }
}
