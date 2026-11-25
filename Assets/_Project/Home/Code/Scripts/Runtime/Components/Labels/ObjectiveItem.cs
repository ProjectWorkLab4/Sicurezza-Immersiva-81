using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// LBL_ObjectiveItem: una riga di obiettivo con icona di stato + testo che va a capo (testo = dati della missione).
    /// Usata nella lista di PANEL_Pause e nel pannello da polso (HUD_WristPanel), su fondi scuri/vetro.
    /// - Done: ICN_CheckCircle_Filled + testo text.on-green barrato, nessuno sfondo.
    /// - Current: sfondo brand.light, ICN_RadioButtonChecked, testo Body Bold nero.
    /// - Todo: ICN_RadioButtonUnchecked + testo text.on-dark, nessuno sfondo.
    /// Misure Figma: larghezza 260, altezza adattata al testo (min 36), padding 8/6, spazio 8, icona 24, raggio button.
    /// Sul pannello da polso il testo non supera ~30 caratteri per riga (CLAUDE.md 4.10, SO_UITheme.Hud.WristObjectiveMaxChars):
    /// con <c>wristMode</c> attivo un testo più lungo scrive un avviso in Console. Non interattiva.
    /// Uso: <c>SetObjective("Spegni l'incendio", ObjectiveState.Current)</c> o <c>SetObjectiveKey("mission.fire.obj1", state)</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("SI81/UI/Objective Item")]
    public class ObjectiveItem : MonoBehaviour
    {
        /// <summary>Padding orizzontale (Figma 8).</summary>
        public const float PADDING_X = 8f;
        /// <summary>Padding verticale (Figma 6).</summary>
        public const float PADDING_Y = 6f;
        /// <summary>Spazio tra icona e testo (Figma 8).</summary>
        public const float GAP = 8f;
        /// <summary>Lato dell'icona (Figma 24).</summary>
        public const float ICON_SIZE = 24f;
        /// <summary>Altezza minima della riga (Figma 36).</summary>
        public const float MIN_HEIGHT = 36f;

        [Header("Dati")]
        [Tooltip("Testo dell'obiettivo (dati della missione; mai scritto nel prefab).")]
        [SerializeField] private string objectiveText;
        [Tooltip("Stato dell'obiettivo.")]
        [SerializeField] private ObjectiveState state = ObjectiveState.Todo;
        [Tooltip("Pannello da polso: avvisa in Console se una riga supera Hud.WristObjectiveMaxChars caratteri.")]
        [SerializeField] private bool wristMode;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private RoundedPanelGraphic background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text text;
        [Tooltip("Icone nell'ordine di ObjectiveState: Done, Current, Todo.")]
        [SerializeField] private Sprite[] icons = new Sprite[3];

        /// <summary>Stato corrente.</summary>
        public ObjectiveState State => state;
        /// <summary>Testo corrente.</summary>
        public string ObjectiveText => objectiveText;

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
        /// Imposta testo e stato dell'obiettivo.
        /// </summary>
        /// <param name="newText">Testo già localizzato (dati della missione).</param>
        /// <param name="newState">Stato dell'obiettivo.</param>
        public void SetObjective(string newText, ObjectiveState newState)
        {
            objectiveText = newText ?? string.Empty;
            state = newState;
            Apply();
        }

        /// <summary>
        /// Imposta il testo da una chiave di SO_UIStrings_It e lo stato.
        /// </summary>
        /// <param name="key">Chiave del testo.</param>
        /// <param name="newState">Stato dell'obiettivo.</param>
        public void SetObjectiveKey(string key, ObjectiveState newState)
        {
            SetObjective(strings != null ? strings.Get(key) : key, newState);
        }

        /// <summary>
        /// Cambia solo lo stato (testo invariato).
        /// </summary>
        /// <param name="newState">Nuovo stato.</param>
        public void SetState(ObjectiveState newState)
        {
            state = newState;
            Apply();
        }

        /// <summary>
        /// Aggiorna colori, icona, testo, altezza e sfondo dai dati correnti.
        /// </summary>
        public void Apply()
        {
            if (theme == null || text == null) return;

            // Colori per stato (Figma UI_ObjectiveItem): Done su verde, Current nero su brand.light, Todo bianco.
            Color contentColor;
            string style;
            switch (state)
            {
                case ObjectiveState.Done: contentColor = theme.GetColor(ColorTokens.TEXT_ON_GREEN); style = TextStyleIds.BODY; break;
                case ObjectiveState.Current: contentColor = theme.TextPrimary; style = TextStyleIds.BODY_BOLD; break;
                default: contentColor = theme.TextOnDark; style = TextStyleIds.BODY; break;
            }

            LabelStyling.StyleText(text, theme, style, contentColor);
            text.fontStyle = state == ObjectiveState.Done ? FontStyles.Strikethrough : FontStyles.Normal;
            if (text.text != objectiveText) text.text = objectiveText;

            // Altezza adattata al testo (calcolata una volta, nessun ContentSizeFitter).
            RectTransform root = (RectTransform)transform;
            float rootWidth = root.rect.width;
            float textWidth = Mathf.Max(0f, rootWidth - 2f * PADDING_X - ICON_SIZE - GAP);
            float textHeight = textWidth > 0f ? text.GetPreferredValues(objectiveText, textWidth, 0f).y : PanelLayout.LineHeight(theme.Body);
            float contentHeight = Mathf.Max(ICON_SIZE, Mathf.Ceil(textHeight));
            LabelStyling.SetSize(root, -1f, Mathf.Max(MIN_HEIGHT, contentHeight + 2f * PADDING_Y));

            LabelStyling.Place(icon != null ? icon.rectTransform : null, PADDING_X, PADDING_Y, ICON_SIZE, ICON_SIZE);
            LabelStyling.PlaceStretchX(text.rectTransform, PADDING_X + ICON_SIZE + GAP, PADDING_X, PADDING_Y, contentHeight);

            if (icon != null)
            {
                Sprite wanted = icons != null && (int)state < icons.Length ? icons[(int)state] : null;
                if (icon.sprite != wanted) icon.sprite = wanted;
                icon.raycastTarget = false;
                LabelStyling.SetColor(icon, contentColor);
            }
            if (background != null)
            {
                // Sfondo solo per l'obiettivo corrente.
                PanelLayout.SetActive(background.gameObject, state == ObjectiveState.Current);
                LabelStyling.SetColor(background, theme.BrandLight);
                PanelLayout.SetRect(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }
            if (wristMode) WarnIfTooLong();
        }

        // Testi già segnalati come troppo lunghi (un avviso per testo).
        private static readonly System.Collections.Generic.HashSet<string> WARNED_TEXTS = new System.Collections.Generic.HashSet<string>();

        /// <summary>
        /// Scrive un avviso se una riga del testo supera il limite del pannello da polso (~30 caratteri).
        /// </summary>
        private void WarnIfTooLong()
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(objectiveText)) return;
            int maxChars = theme.Hud != null ? theme.Hud.WristObjectiveMaxChars : 0;
            if (maxChars <= 0) return;
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            for (int i = 0; i < info.lineCount; i++)
            {
                if (info.lineInfo[i].characterCount <= maxChars) continue;
                // Un solo avviso per testo (il polso riapplica il layout a ogni cambio di mano/impostazione).
                if (!WARNED_TEXTS.Add(objectiveText)) return;
                Debug.LogWarning($"[LBL_ObjectiveItem] Riga di {info.lineInfo[i].characterCount} caratteri: sul pannello da polso il limite è ~{maxChars} per riga (\"{objectiveText}\").", this);
                return;
            }
        }

        /// <summary>
        /// Collega i riferimenti e applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, RoundedPanelGraphic newBackground, Image newIcon, TMP_Text newText, Sprite[] newIcons)
        {
            theme = newTheme;
            strings = newStrings;
            background = newBackground;
            icon = newIcon;
            text = newText;
            icons = newIcons;
            Apply();
        }
    }
}
