using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Istanza di un'etichetta nel mondo: HUD_RiskLabel, HUD_InteractionPrompt o HUD_ContextHelp (stesso componente, il tipo
    /// decide quali riferimenti sono usati). Opaca (CLAUDE.md §4.5), Canvas World Space con perno nel punto di aggancio:
    /// il <see cref="WorldLabelManager"/> la prende da un pool, la lega a un <see cref="LabelAnchor"/>, la sposta, la scala
    /// (scala = 0,00126 × distanza, quindi dimensione apparente costante) e la rigira verso la testa solo quando serve.
    /// Solo HUD_ContextHelp ha un raycaster (si apre puntando e premendo il grilletto); le altre non intercettano il raggio.
    /// Testi aggiornati solo quando cambiano; nessuna allocazione per frame.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("SI81/UI/World Label")]
    public class WorldLabel : MonoBehaviour
    {
        // Distanza minima tra la direzione attuale e quella voluta sotto la quale non serve rigirare (evita tremolii).
        private const float MIN_FORWARD_SQR = 0.0001f;

        [Header("Dati")]
        [Tooltip("Tipo di etichetta (decide cosa mostrano i riferimenti).")]
        [SerializeField] private LabelKind kind = LabelKind.Risk;
        [Tooltip("Tema comune SO_UITheme (durata della dissolvenza, distanze).")]
        [SerializeField] private UITheme theme;
        [Tooltip("Tabella testi SO_UIStrings_It (tasto e verbo dell'invito all'azione, chiavi dell'ancora).")]
        [SerializeField] private UIStrings strings;
        [Tooltip("Icona predefinita del tipo, usata se l'ancora non ne indica una (avviso, mano, aiuto).")]
        [SerializeField] private Sprite defaultIcon;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private CanvasGroup group;
        [Tooltip("Titolo (Body Bold).")]
        [SerializeField] private TMP_Text titleText;
        [Tooltip("Dettaglio: istruzione (Risk), aiuto (ContextHelp) o verbo dell'azione (Interaction).")]
        [SerializeField] private TMP_Text detailText;
        [Tooltip("Icona nella scheda (avviso, oggetto, ...). Opzionale.")]
        [SerializeField] private Image iconImage;
        [Tooltip("Interaction: testo del tasto dentro la pillola (es. GRIP).")]
        [SerializeField] private TMP_Text keyText;
        [Tooltip("Interaction: pillola scura del tasto (si allarga al testo).")]
        [SerializeField] private RectTransform keyPill;
        [Tooltip("ContextHelp: scheda con titolo e testo, mostrata solo da espansa.")]
        [SerializeField] private GameObject expandedCard;
        [Tooltip("ContextHelp: pallino (unico elemento che riceve il raggio) che apre e chiude la scheda.")]
        [SerializeField] private Button toggleButton;

        // Ancora a cui l'etichetta è legata (null = libera nel pool).
        private LabelAnchor anchor;
        // Dissolvenza in corso.
        private Coroutine fadeRoutine;
        // true = ContextHelp aperta.
        private bool expanded;
        // Ultimo valore di scala applicato (evita scritture inutili).
        private float lastScale = -1f;
        // Tipo di ultima icona applicata.
        private Sprite lastIcon;

        /// <summary>Tipo di etichetta.</summary>
        public LabelKind Kind => kind;
        /// <summary>Ancora attuale (null se libera o in uscita).</summary>
        public LabelAnchor Anchor => anchor;
        /// <summary>true se il GameObject è libero nel pool (non mostrata e non in dissolvenza di uscita).</summary>
        public bool IsFree => !gameObject.activeSelf;
        /// <summary>true se la ContextHelp è aperta.</summary>
        public bool IsExpanded => expanded;

        /// <summary>
        /// Collega il pulsante del pallino (solo ContextHelp).
        /// </summary>
        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (toggleButton != null) toggleButton.onClick.AddListener(ToggleExpanded);
        }

        /// <summary>
        /// Rilascia il collegamento del pulsante.
        /// </summary>
        private void OnDestroy()
        {
            if (toggleButton != null) toggleButton.onClick.RemoveListener(ToggleExpanded);
        }

        /// <summary>
        /// Lega l'etichetta a un'ancora e aggiorna contenuti (testi e icona scritti solo se cambiano). Non la mostra.
        /// </summary>
        /// <param name="newAnchor">Ancora di scena.</param>
        public void Bind(LabelAnchor newAnchor)
        {
            if (anchor != newAnchor) SetExpanded(false);
            anchor = newAnchor;
            if (anchor == null) return;

            SetText(titleText, anchor.ResolveTitle(strings));
            switch (kind)
            {
                case LabelKind.Interaction:
                    BindInteraction();
                    break;
                default:
                    SetText(detailText, anchor.ResolveDetail(strings));
                    break;
            }

            Sprite wanted = anchor.Icon != null ? anchor.Icon : defaultIcon;
            if (iconImage != null && wanted != lastIcon)
            {
                iconImage.sprite = wanted;
                iconImage.enabled = wanted != null;
                lastIcon = wanted;
            }
        }

        /// <summary>
        /// Invito all'azione: pillola del tasto (larga come il testo) e verbo accanto.
        /// </summary>
        private void BindInteraction()
        {
            string buttonKeyText = strings != null ? strings.Get(string.IsNullOrEmpty(anchor.ButtonKey) ? "hud.prompt.button.grip" : anchor.ButtonKey) : string.Empty;
            string verb = strings != null ? strings.Get(string.IsNullOrEmpty(anchor.ActionKey) ? "hud.prompt.action.grab" : anchor.ActionKey) : string.Empty;
            SetText(keyText, buttonKeyText);
            SetText(detailText, verb);
            if (keyPill == null || keyText == null || detailText == null) return;

            // Padding orizzontale della pillola e spazio col verbo: dal tema (space.s = 8, space.xs = 4 + 2).
            float padX = theme != null ? theme.GetNumber(NumberTokens.SPACE_S) : 8f;
            float gap = theme != null ? theme.GetNumber(NumberTokens.SPACE_S) - 2f : 6f;
            float pillWidth = UIComponentUtil.MeasureWidth(keyText) + padX * 2f;
            UIComponentUtil.SetSize(keyPill, pillWidth, keyPill.rect.height);
            var verbRect = (RectTransform)detailText.transform;
            Vector2 position = verbRect.anchoredPosition;
            position.x = keyPill.anchoredPosition.x + pillWidth + gap;
            verbRect.anchoredPosition = position;
        }

        /// <summary>
        /// Scrive un testo TMP solo se è diverso (nessun rebuild inutile).
        /// </summary>
        private static void SetText(TMP_Text target, string value)
        {
            if (target == null || target.text == value) return;
            target.text = value;
        }

        /// <summary>
        /// Mostra l'etichetta con una dissolvenza in entrata (durata dal tema).
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            group.alpha = 0f;
            group.blocksRaycasts = kind == LabelKind.ContextHelp;
            lastScale = -1f;
            float duration = theme != null ? theme.Motion.FadeDuration : 0f;
            if (duration <= 0f)
            {
                group.alpha = 1f;
                return;
            }
            fadeRoutine = UITween.Play(this, fadeRoutine, UITween.Fade(group, 1f, duration, () => fadeRoutine = null));
        }

        /// <summary>
        /// Nasconde l'etichetta con una dissolvenza in uscita, poi la libera nel pool. L'ancora viene sciolta subito.
        /// </summary>
        public void Hide()
        {
            anchor = null;
            if (!gameObject.activeSelf) return;
            group.blocksRaycasts = false;
            float duration = theme != null ? theme.Motion.FadeDuration : 0f;
            if (duration <= 0f || !isActiveAndEnabled)
            {
                gameObject.SetActive(false);
                return;
            }
            fadeRoutine = UITween.Play(this, fadeRoutine, UITween.Fade(group, 0f, duration, Release));
        }

        /// <summary>
        /// Fine della dissolvenza in uscita: spegne il GameObject (torna libero).
        /// </summary>
        private void Release()
        {
            fadeRoutine = null;
            SetExpanded(false);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sposta l'etichetta sull'ancora e ne regola la scala sulla distanza dalla testa (dimensione apparente costante).
        /// Chiamata a ogni frame solo per le etichette attive (al massimo una decina): nessuna allocazione.
        /// </summary>
        /// <param name="head">Testa del giocatore.</param>
        public void Follow(Transform head)
        {
            if (anchor == null || head == null) return;
            Vector3 point = anchor.WorldPoint;
            transform.position = point;
            float distance = Vector3.Distance(head.position, point);
            if (theme != null) distance = Mathf.Clamp(distance, theme.Hud.WorldLabelMinDistance, theme.Hud.WorldLabelMaxDistance);
            float scale = UITheme.GetCanvasScale(distance);
            if (Mathf.Abs(scale - lastScale) > 0.000001f)
            {
                transform.localScale = Vector3.one * scale;
                lastScale = scale;
            }
        }

        /// <summary>
        /// Rigira l'etichetta verso la testa (solo imbardata, resta dritta). Con <paramref name="force"/> false lo fa solo se
        /// la direzione attuale si discosta più di <paramref name="thresholdDegrees"/> da quella voluta.
        /// </summary>
        /// <param name="head">Testa del giocatore.</param>
        /// <param name="thresholdDegrees">Scostamento massimo tollerato in gradi.</param>
        /// <param name="force">true = rigira comunque (appena mostrata).</param>
        public void Face(Transform head, float thresholdDegrees, bool force)
        {
            if (head == null) return;
            Vector3 away = transform.position - head.position;
            away.y = 0f;
            if (away.sqrMagnitude < MIN_FORWARD_SQR) return;
            away.Normalize();
            if (!force)
            {
                Vector3 current = transform.forward;
                current.y = 0f;
                if (current.sqrMagnitude > MIN_FORWARD_SQR && Vector3.Angle(current, away) <= thresholdDegrees) return;
            }
            // Il Canvas guarda lungo +Z: il suo fronte è rivolto a chi sta dietro, cioè alla testa.
            transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        /// <summary>
        /// Apre o chiude la scheda di HUD_ContextHelp (clic sul pallino).
        /// </summary>
        public void ToggleExpanded()
        {
            SetExpanded(!expanded);
        }

        /// <summary>
        /// Imposta la scheda di HUD_ContextHelp aperta o chiusa.
        /// </summary>
        /// <param name="open">true = aperta.</param>
        public void SetExpanded(bool open)
        {
            expanded = open;
            if (expandedCard != null && expandedCard.activeSelf != open) expandedCard.SetActive(open);
        }

        /// <summary>
        /// Collega tema, testi e riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(LabelKind newKind, UITheme newTheme, UIStrings newStrings, Sprite newDefaultIcon, CanvasGroup newGroup,
            TMP_Text newTitle, TMP_Text newDetail, Image newIcon, TMP_Text newKeyText, RectTransform newKeyPill, GameObject newExpandedCard, Button newToggle)
        {
            kind = newKind;
            theme = newTheme;
            strings = newStrings;
            defaultIcon = newDefaultIcon;
            group = newGroup;
            titleText = newTitle;
            detailText = newDetail;
            iconImage = newIcon;
            keyText = newKeyText;
            keyPill = newKeyPill;
            expandedCard = newExpandedCard;
            toggleButton = newToggle;
        }
    }
}
