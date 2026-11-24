using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SI81.UI
{
    /// <summary>
    /// Voce della bottom bar (prefab BTN_BottomBarItem): icona 32 + etichetta Body, alta 44, configurata da dati
    /// (schermata di destinazione, coppia di icone contorno/piena, chiave dell'etichetta).
    /// Stati (Figma UI_BottomBarItem + CLAUDE.md §4.3):
    /// - Default: icona contorno + etichetta Regular in color.text.primary;
    /// - Hover: pillola color.tile.neutral dietro la voce (nessuna scala, nessuna ombra), anche sulla voce attiva;
    /// - Pressed: scala Motion.PressedScale (0,98);
    /// - Active: icona piena + etichetta Bold in color.brand.dark, nessun fondo/anello/linea.
    /// Transizioni con le durate del tema, a tempo non scalato (nessun Animator). La larghezza dipende dall'etichetta
    /// Regular (10 + 32 + 6 + testo + 10) e non cambia con lo stato attivo (niente salti della barra).
    /// Il clic viene rilanciato con l'evento <see cref="Clicked"/>: la navigazione la decide <see cref="BottomBar"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class BottomBarItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Schermata di primo livello aperta dalla voce.")]
        [SerializeField] private UIScreenId screen = UIScreenId.Home;
        [Tooltip("Icona contorno (stato normale), es. ICN_Home.")]
        [SerializeField] private Sprite iconOutline;
        [Tooltip("Icona piena (stato attivo), es. ICN_Home_Filled.")]
        [SerializeField] private Sprite iconFilled;
        [Tooltip("Chiave dell'etichetta in SO_UIStrings_It, es. ui.nav.home.")]
        [SerializeField] private string labelKey;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private RoundedPanelGraphic hoverBackground;
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private UIStringText label;

        // Stato logico.
        private bool isActive;
        private bool isHovered;
        private bool isPressed;
        // Animazioni in corso (null se nessuna).
        private Coroutine scaleRoutine;
        private Coroutine hoverRoutine;

        /// <summary>Evento: la voce è stata cliccata (parametro: la voce stessa).</summary>
        public event Action<BottomBarItem> Clicked;

        /// <summary>Schermata aperta dalla voce.</summary>
        public UIScreenId Screen => screen;
        /// <summary>Chiave dell'etichetta.</summary>
        public string LabelKey => labelKey;
        /// <summary>true se la voce è attiva (sezione corrente).</summary>
        public bool IsActive => isActive;
        /// <summary>true se il puntatore è sopra la voce.</summary>
        public bool IsHovered => isHovered;
        /// <summary>true se la voce è premuta.</summary>
        public bool IsPressed => isPressed;
        /// <summary>Pulsante uGUI della voce.</summary>
        public UnityEngine.UI.Button Button => button;

        /// <summary>
        /// Applica tema, testo e larghezza; collega il pulsante; parte nello stato Default senza animazioni.
        /// </summary>
        private void Awake()
        {
            Apply();
            if (button != null) button.onClick.AddListener(HandleClick);
            ApplyHover(true);
        }

        /// <summary>
        /// Alla disattivazione azzera hover e pressione (nessuno stato rimasto appeso).
        /// </summary>
        private void OnDisable()
        {
            isHovered = false;
            isPressed = false;
            scaleRoutine = null;
            hoverRoutine = null;
            transform.localScale = Vector3.one;
            ApplyHover(true);
        }

        /// <summary>
        /// Rilascia il collegamento al pulsante.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>
        /// Applica tutto: colori, icona, etichetta (chiave, stile, colore) e misure. Nessun Layout Group.
        /// </summary>
        public void Apply()
        {
            if (theme == null) return;
            if (label != null && !string.IsNullOrEmpty(labelKey) && label.Key != labelKey) label.SetKey(labelKey);
            if (hoverBackground != null && hoverRoutine == null)
            {
                Color hover = HoverColor(isHovered);
                if (hoverBackground.color != hover) hoverBackground.color = hover;
            }
            ApplyActiveLook();
            ApplyLayout();
        }

        /// <summary>
        /// Imposta lo stato attivo (sezione corrente): icona piena + etichetta Bold in brand.dark, oppure Default.
        /// </summary>
        /// <param name="active">true per lo stato attivo.</param>
        public void SetActive(bool active)
        {
            if (isActive == active) return;
            isActive = active;
            ApplyActiveLook();
        }

        /// <summary>
        /// Larghezza della voce: padding + icona + gap + etichetta (stile Body Regular, anche quando è attiva) + padding,
        /// arrotondata per eccesso.
        /// </summary>
        /// <returns>Larghezza in unità Canvas.</returns>
        public float GetPreferredWidth()
        {
            if (theme == null) return 0f;
            float padding = theme.GetNumber(NumberTokens.BOTTOM_BAR_ITEM_PADDING);
            float iconSize = theme.GetNumber(NumberTokens.BOTTOM_BAR_ITEM_ICON);
            float gap = theme.GetNumber(NumberTokens.BOTTOM_BAR_ITEM_GAP);
            return Mathf.Ceil(padding + iconSize + gap + MeasureRegularLabel() + padding);
        }

        /// <summary>
        /// Misura il testo dell'etichetta con lo stile Body Regular (la larghezza non cambia con lo stato attivo).
        /// </summary>
        /// <returns>Larghezza del testo in unità Canvas.</returns>
        private float MeasureRegularLabel()
        {
            TMP_Text tmp = label != null ? label.GetComponent<TMP_Text>() : null;
            UITheme.TextStyle body = theme.Body;
            if (tmp == null || body == null || body.Font == null) return 0f;
            // Misura con il font Regular anche se l'etichetta mostra il Bold: stessi parametri dello stile Body.
            TMP_FontAsset shown = tmp.font;
            float shownSize = tmp.fontSize;
            tmp.font = body.Font;
            tmp.fontSize = body.Size;
            float width = tmp.GetPreferredValues(tmp.text, float.PositiveInfinity, float.PositiveInfinity).x;
            tmp.font = shown;
            tmp.fontSize = shownSize;
            return width;
        }

        /// <summary>
        /// Misure Figma: alta 44, icona 32 a 10 dal bordo sinistro centrata in verticale, etichetta 6 a destra
        /// dell'icona (allineata a sinistra, così l'icona non si sposta quando l'etichetta passa al Bold),
        /// fondo hover su tutto il rettangolo.
        /// </summary>
        private void ApplyLayout()
        {
            var root = (RectTransform)transform;
            float height = theme.GetNumber(NumberTokens.BOTTOM_BAR_ITEM_HEIGHT);
            float padding = theme.GetNumber(NumberTokens.BOTTOM_BAR_ITEM_PADDING);
            float iconSize = theme.GetNumber(NumberTokens.BOTTOM_BAR_ITEM_ICON);
            float gap = theme.GetNumber(NumberTokens.BOTTOM_BAR_ITEM_GAP);
            float width = GetPreferredWidth();
            if (Mathf.Abs(root.rect.width - width) > 0.001f) root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (Mathf.Abs(root.rect.height - height) > 0.001f) root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            if (hoverBackground != null)
                PanelLayout.SetRect(hoverBackground.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            if (icon != null)
                PanelLayout.SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(padding, -iconSize * 0.5f), new Vector2(padding + iconSize, iconSize * 0.5f));
            if (label != null)
                PanelLayout.SetRect((RectTransform)label.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
                    new Vector2(padding + iconSize + gap, 0f), new Vector2(-padding, 0f));
        }

        /// <summary>
        /// Icona (contorno/piena), colore dell'icona e stile dell'etichetta secondo lo stato attivo.
        /// </summary>
        private void ApplyActiveLook()
        {
            if (theme == null) return;
            string colorToken = isActive ? ColorTokens.BRAND_DARK : ColorTokens.TEXT_PRIMARY;
            if (icon != null)
            {
                Sprite sprite = isActive && iconFilled != null ? iconFilled : iconOutline;
                if (icon.sprite != sprite) icon.sprite = sprite;
                Color iconColor = theme.GetColor(colorToken);
                if (icon.color != iconColor) icon.color = iconColor;
            }
            if (label != null) label.SetStyle(isActive ? TextStyleIds.BODY_BOLD : TextStyleIds.BODY, colorToken);
        }

        /// <summary>
        /// Mostra o nasconde la pillola hover: colore color.tile.neutral con alpha 0 → alpha del token, in
        /// Motion.HoverDuration (tempo non scalato). A riposo l'alpha è 0, quindi nel prefab la pillola non si vede;
        /// la grafica resta comunque il bersaglio dei raggi della voce.
        /// </summary>
        /// <param name="instant">true per applicare subito.</param>
        private void ApplyHover(bool instant)
        {
            if (hoverBackground == null || theme == null) return;
            if (hoverRoutine != null) StopCoroutine(hoverRoutine);
            hoverRoutine = null;
            Color target = HoverColor(isHovered);
            float duration = instant ? 0f : theme.Motion.HoverDuration;
            if (duration <= 0f || !isActiveAndEnabled || !Application.isPlaying)
            {
                if (hoverBackground.color != target) hoverBackground.color = target;
                return;
            }
            // Dissolvenza di colore comune (UITween, tempo non scalato).
            hoverRoutine = UITween.Play(this, null, UITween.ColorTo(hoverBackground, target, duration, () => hoverRoutine = null));
        }

        /// <summary>
        /// Colore della pillola hover: color.tile.neutral, trasparente quando non c'è hover.
        /// </summary>
        /// <param name="hovered">true per il colore pieno.</param>
        private Color HoverColor(bool hovered)
        {
            Color color = theme.TileNeutral;
            if (!hovered) color.a = 0f;
            return color;
        }

        /// <summary>
        /// Porta la scala a pressed (0,98) o a 1 in Motion.PressDuration, a tempo non scalato.
        /// </summary>
        private void ApplyPressedScale()
        {
            if (theme == null) return;
            float target = isPressed ? theme.Motion.PressedScale : 1f;
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            scaleRoutine = null;
            if (!isActiveAndEnabled || theme.Motion.PressDuration <= 0f)
            {
                transform.localScale = Vector3.one * target;
                return;
            }
            // Scala comune (UITween, tempo non scalato).
            scaleRoutine = UITween.Play(this, null, UITween.Scale(transform, target, theme.Motion.PressDuration, () => scaleRoutine = null));
        }

        /// <summary>
        /// Puntatore sopra la voce: mostra la pillola hover.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            isHovered = true;
            ApplyHover(false);
        }

        /// <summary>
        /// Puntatore fuori dalla voce: nasconde la pillola hover e annulla la pressione.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            ApplyHover(false);
            if (isPressed)
            {
                isPressed = false;
                ApplyPressedScale();
            }
        }

        /// <summary>
        /// Grilletto/clic premuto: scala pressed.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable() || eventData.button != PointerEventData.InputButton.Left) return;
            isPressed = true;
            ApplyPressedScale();
        }

        /// <summary>
        /// Grilletto/clic rilasciato: torna alla scala normale.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !isPressed) return;
            isPressed = false;
            ApplyPressedScale();
        }

        /// <summary>
        /// Indica se la voce accetta input (pulsante interattivo, gruppi compresi).
        /// </summary>
        private bool IsInteractable()
        {
            return button == null || button.IsInteractable();
        }

        /// <summary>
        /// Clic del pulsante: rilancia l'evento <see cref="Clicked"/>.
        /// </summary>
        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }

#if UNITY_EDITOR
        /// <summary>
        /// In Editor riapplica tutto quando si cambia un campo nell'Inspector (rimandato: in OnValidate Unity non
        /// permette di cambiare RectTransform); ignorato sugli asset prefab non aperti.
        /// </summary>
        private void OnValidate()
        {
            UIEditorSupport.ApplyDeferred(this, Apply);
        }
#endif

        /// <summary>
        /// Imposta tema e riferimenti, poi applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UnityEngine.UI.Button newButton, RoundedPanelGraphic newHover,
            UnityEngine.UI.Image newIcon, UIStringText newLabel)
        {
            theme = newTheme;
            button = newButton;
            hoverBackground = newHover;
            icon = newIcon;
            label = newLabel;
            Apply();
        }

        /// <summary>
        /// Imposta i dati della singola voce (schermata, icone, chiave), poi applica. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureData(UIScreenId newScreen, Sprite newOutline, Sprite newFilled, string newLabelKey)
        {
            screen = newScreen;
            iconOutline = newOutline;
            iconFilled = newFilled;
            labelKey = newLabelKey;
            Apply();
        }
    }
}
