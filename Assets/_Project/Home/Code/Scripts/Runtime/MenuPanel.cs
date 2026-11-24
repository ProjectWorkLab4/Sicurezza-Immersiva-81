using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Pannello vetro dei menu (prefab PANEL_Menu): ombra morbida, vetro arrotondato con riflesso diagonale,
    /// intestazione (posto per BTN_Back, percorso facoltativo, titolo, posto a destra) e area contenuti.
    /// Colori e misure arrivano solo da SO_UITheme, i testi da SO_UIStrings_It (tramite UIStringText). Il layout è calcolato da codice
    /// (nessun Layout Group) quando cambiano i parametri, non a ogni frame.
    /// Uso: istanziare PANEL_Menu dentro la schermata (UIScreen), impostare chiave del titolo, percorso, ShowBack
    /// e altezza; mettere i contenuti sotto <see cref="ContentRoot"/> e gli elementi a destra sotto <see cref="HeaderRightSlot"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MenuPanel : MonoBehaviour
    {
        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] private UITheme theme;

        [Header("Parametri")]
        [Tooltip("Chiave del titolo (stile Title, color.text.on-dark), es. ui.home.title.")]
        [SerializeField] private string titleKey;
        [Tooltip("Chiave del percorso sopra il titolo (stile Body, color.text.on-green), es. ui.brief.breadcrumb. Vuota = nessun percorso.")]
        [SerializeField] private string breadcrumbKey;
        [Tooltip("Mostra il posto di BTN_Back a sinistra del titolo (nascosto su Home). Il clic è gestito da BTN_Back (BackButton), il pannello mostra/nasconde solo il posto.")]
        [SerializeField] private bool showBack;
        [Tooltip("Altezza del pannello in unità Canvas. 0 = default del tema (size.menu-panel.height, 475). Mission Brief: 535 (punto aperto).")]
        [SerializeField] private float height;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UnityEngine.UI.Image shadowImage;
        [SerializeField] private RoundedPanelGraphic glass;
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform backSlot;
        [SerializeField] private RectTransform titleColumn;
        [SerializeField] private UIStringText breadcrumbText;
        [SerializeField] private UIStringText titleText;
        [SerializeField] private RectTransform headerRightSlot;
        [SerializeField] private RectTransform contentRoot;

        /// <summary>Contenitore dei contenuti della schermata (sotto l'intestazione, dentro il padding).</summary>
        public RectTransform ContentRoot => contentRoot;
        /// <summary>Posto a destra nell'intestazione (es. pillola "Miglior risultato"); ancorato al bordo destro.</summary>
        public RectTransform HeaderRightSlot => headerRightSlot;
        /// <summary>Posto 40×40 di BTN_Back (passo 3).</summary>
        public RectTransform BackSlot => backSlot;
        /// <summary>Chiave del titolo.</summary>
        public string TitleKey => titleKey;
        /// <summary>Chiave del percorso (vuota = nessun percorso).</summary>
        public string BreadcrumbKey => breadcrumbKey;

        /// <summary>Mostra o nasconde il posto di BTN_Back e riposiziona l'intestazione.</summary>
        public bool ShowBack
        {
            get => showBack;
            set
            {
                showBack = value;
                ApplyLayout();
            }
        }

        /// <summary>Altezza effettiva del pannello (parametro, o default del tema se 0).</summary>
        public float Height => height > 0f ? height : (theme != null ? theme.GetNumber(NumberTokens.MENU_PANEL_HEIGHT) : 0f);

        /// <summary>
        /// Applica tema, testi e layout.
        /// </summary>
        private void Awake()
        {
            Apply();
        }

        /// <summary>
        /// Cambia l'altezza del pannello e ricalcola il layout.
        /// </summary>
        /// <param name="newHeight">Altezza in unità Canvas (0 = default del tema).</param>
        public void SetHeight(float newHeight)
        {
            height = newHeight;
            ApplyLayout();
        }

        /// <summary>
        /// Cambia la chiave del titolo.
        /// </summary>
        /// <param name="key">Chiave di SO_UIStrings_It.</param>
        public void SetTitleKey(string key)
        {
            titleKey = key;
            if (titleText != null) titleText.SetKey(key);
        }

        /// <summary>
        /// Scrive un titolo dinamico (es. titolo dello scenario da SO_Mission_*) senza chiave.
        /// </summary>
        /// <param name="text">Testo già localizzato.</param>
        public void SetTitleText(string text)
        {
            titleKey = string.Empty;
            if (titleText == null) return;
            titleText.SetKey(string.Empty);
            TMP_Text tmp = titleText.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = text;
        }

        /// <summary>
        /// Cambia la chiave del percorso (vuota = nasconde il percorso) e ricalcola il layout.
        /// </summary>
        /// <param name="key">Chiave di SO_UIStrings_It o stringa vuota.</param>
        public void SetBreadcrumbKey(string key)
        {
            breadcrumbKey = key;
            ApplyTexts();
            ApplyLayout();
        }

        /// <summary>
        /// Applica tutto: colori del tema, testi e layout.
        /// </summary>
        public void Apply()
        {
            ApplyTheme();
            ApplyTexts();
            ApplyLayout();
        }

        /// <summary>
        /// Colori del tema: vetro (surface.glass) con riflesso, ombra menuPanel (colore e alpha del token).
        /// </summary>
        private void ApplyTheme()
        {
            PanelLayout.ApplyGlass(glass, theme);
        }

        /// <summary>
        /// Chiavi dei testi: titolo e percorso (il percorso si nasconde se la chiave è vuota).
        /// </summary>
        private void ApplyTexts()
        {
            if (titleText != null && !string.IsNullOrEmpty(titleKey) && titleText.Key != titleKey) titleText.SetKey(titleKey);
            if (breadcrumbText != null)
            {
                bool hasBreadcrumb = !string.IsNullOrEmpty(breadcrumbKey);
                SetActive(breadcrumbText.gameObject, hasBreadcrumb);
                if (hasBreadcrumb && breadcrumbText.Key != breadcrumbKey) breadcrumbText.SetKey(breadcrumbKey);
            }
        }

        /// <summary>
        /// Posiziona ombra, vetro, intestazione e contenuti (misure Figma: padding.panel 24, space.m 12, titolo 40,
        /// percorso 23, BTN_Back 40). Home: contenuti da y 76; con percorso: da y 99.
        /// </summary>
        private void ApplyLayout()
        {
            if (theme == null) return;
            var root = (RectTransform)transform;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, theme.GetNumber(NumberTokens.MENU_PANEL_WIDTH));
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);

            float padding = theme.PaddingPanel;
            float gap = theme.GetNumber(NumberTokens.SPACE_M);
            float backSize = theme.TargetMinSize;
            float titleLine = LineHeight(theme.Title);
            float breadcrumbLine = LineHeight(theme.Body);
            bool hasBreadcrumb = !string.IsNullOrEmpty(breadcrumbKey);
            float headerHeight = Mathf.Max(hasBreadcrumb ? breadcrumbLine + titleLine : titleLine, showBack ? backSize : 0f);

            // Ombra menuPanel (colore del token, sprite più grande del pannello di PaddingUnits per lato).
            PanelLayout.ApplyShadow(shadowImage, theme, ShadowRoles.MENU_PANEL);
            if (glass != null) SetRect(glass.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Intestazione: fascia in alto dentro il padding.
            if (header != null)
                SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(padding, -padding - headerHeight), new Vector2(-padding, -padding));

            // Posto di BTN_Back: 40×40 a sinistra, centrato in verticale.
            if (backSlot != null)
            {
                SetActive(backSlot.gameObject, showBack);
                SetRect(backSlot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, -backSize * 0.5f), new Vector2(backSize, backSize * 0.5f));
            }

            // Colonna titolo: a destra del posto indietro (+ space.m), tutta l'altezza dell'intestazione.
            float columnLeft = showBack ? backSize + gap : 0f;
            if (titleColumn != null)
                SetRect(titleColumn, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(columnLeft, 0f), Vector2.zero);

            // Percorso (riga alta 23) e titolo (riga alta 40) impilati, centrati in verticale nella colonna.
            float stackHeight = hasBreadcrumb ? breadcrumbLine + titleLine : titleLine;
            float stackTop = -(headerHeight - stackHeight) * 0.5f;
            if (breadcrumbText != null)
                SetRect((RectTransform)breadcrumbText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, stackTop - breadcrumbLine), new Vector2(0f, stackTop));
            if (titleText != null)
            {
                float titleTop = stackTop - (hasBreadcrumb ? breadcrumbLine : 0f);
                SetRect((RectTransform)titleText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, titleTop - titleLine), new Vector2(0f, titleTop));
            }

            // Posto a destra: ancorato al bordo destro, alto quanto l'intestazione, larghezza decisa dai figli.
            if (headerRightSlot != null)
                SetRect(headerRightSlot, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);

            // Contenuti: sotto l'intestazione (+ space.m), dentro il padding.
            if (contentRoot != null)
                SetRect(contentRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 1f),
                    new Vector2(padding, padding), new Vector2(-padding, -(padding + headerHeight + gap)));
        }

        /// <summary>Alias locale di <see cref="PanelLayout.LineHeight"/>.</summary>
        private static float LineHeight(UITheme.TextStyle style) => PanelLayout.LineHeight(style);

        /// <summary>Alias locale di <see cref="PanelLayout.SetRect"/>.</summary>
        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
            => PanelLayout.SetRect(rect, anchorMin, anchorMax, pivot, offsetMin, offsetMax);

        /// <summary>Alias locale di <see cref="PanelLayout.SetActive"/>.</summary>
        private static void SetActive(GameObject go, bool active) => PanelLayout.SetActive(go, active);

#if UNITY_EDITOR
        /// <summary>
        /// In Editor riapplica tutto quando si cambia un campo nell'Inspector. Rimandato a fine aggiornamento perché
        /// in OnValidate Unity non permette di attivare oggetti o cambiare RectTransform; ignorato sugli asset prefab non aperti.
        /// </summary>
        private void OnValidate()
        {
            UIEditorSupport.ApplyDeferred(this, Apply);
        }
#endif

        /// <summary>
        /// Imposta dati, riferimenti e parametri, poi applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UnityEngine.UI.Image newShadow, RoundedPanelGraphic newGlass,
            RectTransform newHeader, RectTransform newBackSlot, RectTransform newTitleColumn, UIStringText newBreadcrumb,
            UIStringText newTitle, RectTransform newRightSlot, RectTransform newContent)
        {
            theme = newTheme;
            shadowImage = newShadow;
            glass = newGlass;
            header = newHeader;
            backSlot = newBackSlot;
            titleColumn = newTitleColumn;
            breadcrumbText = newBreadcrumb;
            titleText = newTitle;
            headerRightSlot = newRightSlot;
            contentRoot = newContent;
            Apply();
        }

        /// <summary>
        /// Imposta i parametri della singola istanza (titolo, percorso, indietro, altezza), poi applica. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureParameters(string newTitleKey, string newBreadcrumbKey, bool newShowBack, float newHeight)
        {
            titleKey = newTitleKey;
            breadcrumbKey = newBreadcrumbKey;
            showBack = newShowBack;
            height = newHeight;
            Apply();
        }
    }
}
