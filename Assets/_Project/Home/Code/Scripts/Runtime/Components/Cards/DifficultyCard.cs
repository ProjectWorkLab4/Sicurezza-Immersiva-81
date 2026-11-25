using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// BTN_DifficultyCard (226 x 117, CLAUDE.md 4.10): card di scelta del livello nel Mission Brief. Contiene un
    /// LBL_LevelBadge ("Livello N" Bold + nome) e la descrizione del livello (max ~55 caratteri, da SO_UIStrings_It).
    /// Selezionata (CLAUDE.md 4.1 e 4.7): sollevamento (ombra più profonda) + glow morbido brand.light + cerchio bianco 30 x 30
    /// in alto a destra con la spunta piena nel colore del livello (SO_DifficultyStyle). NESSUN bordo. Bloccata
    /// (livello non disponibile): contenuto al 40%, non cliccabile, non selezionabile.
    /// Uso: <c>card.Set(DifficultyLevel.Operational, "ui.level.operational_desc", false); card.Selected = true;</c>;
    /// iscriversi a <see cref="Clicked"/> (restituisce il livello). Chi la ospita (PANEL_MissionBrief) decide la selezione
    /// esclusiva tra le tre card.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(UIStateVisual))]
    [AddComponentMenu("SI81/UI/Difficulty Card")]
    public class DifficultyCard : MonoBehaviour
    {
        [Header("Dati")]
        [Tooltip("Livello mostrato.")]
        [SerializeField] private DifficultyLevel level = DifficultyLevel.Guided;
        [Tooltip("Chiave della descrizione. Vuota = quella del livello in SO_DifficultyStyle.")]
        [SerializeField] private string descriptionKey;
        [Tooltip("true = livello non disponibile.")]
        [SerializeField] private bool locked;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private UIStrings strings;
        [SerializeField] private DifficultyStyle difficultyStyle;
        [SerializeField] private Button button;
        [SerializeField] private UIStateVisual visual;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private LevelBadge levelBadge;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Image checkIcon;

        /// <summary>Evento: la card è stata cliccata (parametro: il suo livello). Non scatta se bloccata.</summary>
        public event Action<DifficultyLevel> Clicked;

        /// <summary>Livello mostrato.</summary>
        public DifficultyLevel Level => level;
        /// <summary>true se il livello è bloccato.</summary>
        public bool IsLocked => locked;

        /// <summary>
        /// true se la card è selezionata (sollevamento + glow + spunta). Una card bloccata non si seleziona.
        /// </summary>
        public bool Selected
        {
            get => visual != null && visual.Selected;
            set
            {
                if (visual == null) return;
                visual.Selected = value && !locked;
            }
        }

        /// <summary>
        /// Collega il clic e applica dati e stile.
        /// </summary>
        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
            Apply();
        }

        /// <summary>
        /// Rilascia il collegamento al pulsante.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
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
        /// Imposta livello, descrizione e blocco e aggiorna la card.
        /// </summary>
        /// <param name="newLevel">Livello (1 Guided, 2 Operational, 3 Crisis).</param>
        /// <param name="newDescriptionKey">Chiave della descrizione (es. "ui.level.guided_desc"). Null/vuota = quella di SO_DifficultyStyle.</param>
        /// <param name="isLocked">true = livello non disponibile (non cliccabile, al 40%).</param>
        public void Set(DifficultyLevel newLevel, string newDescriptionKey = null, bool isLocked = false)
        {
            level = newLevel;
            descriptionKey = newDescriptionKey;
            locked = isLocked;
            if (locked && visual != null) visual.Selected = false;
            Apply();
        }

        /// <summary>
        /// Riscrive badge, descrizione, colore della spunta e stato interattivo dai dati correnti.
        /// </summary>
        public void Apply()
        {
            if (theme == null || strings == null) return;
            DifficultyStyle.Entry entry = difficultyStyle != null ? difficultyStyle.Get(level) : null;

            if (levelBadge != null)
            {
                levelBadge.SetSurface(LabelSurface.Light);
                levelBadge.SetLevel(level);
            }

            string key = !string.IsNullOrEmpty(descriptionKey) ? descriptionKey : (entry != null ? entry.DescriptionKey : string.Empty);
            if (description != null)
            {
                CardTextUtil.Style(description, theme, TextStyleIds.BODY, theme.TextPrimary);
                description.text = string.IsNullOrEmpty(key) ? string.Empty : strings.Get(key);
            }

            // Spunta: icona piena nel colore del livello (guided / operational / crisis).
            if (checkIcon != null && entry != null) checkIcon.color = entry.Color;

            // Bloccata: non cliccabile, contenuto al 40%.
            if (contentGroup != null) contentGroup.alpha = locked ? theme.GetNumber(CardsTokens.ALPHA_DISABLED) : 1f;
            if (visual != null) visual.Interactable = !locked;
            if (button != null) button.interactable = !locked;
        }

        /// <summary>
        /// Rilancia il clic con il livello.
        /// </summary>
        private void HandleClick()
        {
            if (locked) return;
            Clicked?.Invoke(level);
        }

        /// <summary>
        /// Collega i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UIStrings newStrings, DifficultyStyle newStyle, Button newButton, UIStateVisual newVisual,
            CanvasGroup newContentGroup, LevelBadge newLevelBadge, TMP_Text newDescription, Image newCheckIcon)
        {
            theme = newTheme;
            strings = newStrings;
            difficultyStyle = newStyle;
            button = newButton;
            visual = newVisual;
            contentGroup = newContentGroup;
            levelBadge = newLevelBadge;
            description = newDescription;
            checkIcon = newCheckIcon;
        }
    }
}
