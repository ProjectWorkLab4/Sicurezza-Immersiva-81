using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Pannello vetro laterale (prefab PANEL_Side): stesso vetro, riflesso e raggio di PANEL_Menu, ombra sidePanel
    /// (sprite compatto TEX_PanelShadowCompact), titolo da chiave (default "Criteri di completamento") e area contenuti.
    /// Larghezza fissa dal tema, altezza calcolata dal contenuto: le righe sotto <see cref="ContentRoot"/> vengono
    /// impilate dall'alto (gap dal tema) solo quando si chiama <see cref="RefreshLayout"/>, mai a ogni frame
    /// (nessun Layout Group, nessun ContentSizeFitter).
    /// Inclinato verso il giocatore attorno al bordo DESTRO (pivot in alto a destra, vicino a PANEL_Menu): il bordo
    /// sinistro viene avanti. Se <see cref="AttachTo"/> è impostato (fratello nello stesso genitore, es. PANEL_Menu),
    /// il pannello si mette alla sua sinistra a gap.side-panel con i bordi superiori allineati.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SidePanel : MonoBehaviour
    {
        /// <summary>Chiave del titolo di default (Mission Brief, CLAUDE.md §4.7).</summary>
        public const string DEFAULT_TITLE_KEY = "ui.brief.completion_criteria";

        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] private UITheme theme;

        [Header("Parametri")]
        [Tooltip("Chiave del titolo (stile Body Bold, color.text.on-dark). Vuota = nessun titolo.")]
        [SerializeField] private string titleKey = DEFAULT_TITLE_KEY;
        [Tooltip("Inclinazione verso il giocatore in gradi. Negativo = token del tema angle.side-panel.tilt (20).")]
        [SerializeField] private float tiltAngle = -1f;
        [Tooltip("Pannello accanto a cui mettersi (fratello nello stesso genitore, es. PANEL_Menu). Vuoto = posizione libera.")]
        [SerializeField] private RectTransform attachTo;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UnityEngine.UI.Image shadowImage;
        [SerializeField] private RoundedPanelGraphic glass;
        [SerializeField] private UIStringText titleText;
        [SerializeField] private RectTransform contentRoot;

        /// <summary>Contenitore delle righe (impilate dall'alto da <see cref="RefreshLayout"/>).</summary>
        public RectTransform ContentRoot => contentRoot;
        /// <summary>Chiave del titolo.</summary>
        public string TitleKey => titleKey;
        /// <summary>Pannello accanto a cui si posiziona (può essere null).</summary>
        public RectTransform AttachTo => attachTo;
        /// <summary>Inclinazione effettiva in gradi (parametro, o token del tema se negativo).</summary>
        public float TiltAngle => tiltAngle >= 0f ? tiltAngle : (theme != null ? theme.GetNumber(NumberTokens.SIDE_PANEL_TILT) : 0f);
        /// <summary>Altezza attuale del pannello in unità Canvas (calcolata dal contenuto).</summary>
        public float Height => ((RectTransform)transform).rect.height;

        /// <summary>
        /// Applica tema, testo e layout all'avvio.
        /// </summary>
        private void Awake()
        {
            Apply();
        }

        /// <summary>
        /// Cambia la chiave del titolo e ricalcola il layout.
        /// </summary>
        /// <param name="key">Chiave di SO_UIStrings_It (vuota = nessun titolo).</param>
        public void SetTitleKey(string key)
        {
            titleKey = key;
            ApplyTexts();
            RefreshLayout();
        }

        /// <summary>
        /// Cambia l'inclinazione (gradi; negativo = token del tema) e la riapplica.
        /// </summary>
        /// <param name="degrees">Angolo verso il giocatore.</param>
        public void SetTiltAngle(float degrees)
        {
            tiltAngle = degrees;
            RefreshLayout();
        }

        /// <summary>
        /// Imposta il pannello accanto a cui mettersi e ricalcola la posizione.
        /// </summary>
        /// <param name="target">Fratello nello stesso genitore (es. PANEL_Menu) o null.</param>
        public void SetAttachTo(RectTransform target)
        {
            attachTo = target;
            RefreshLayout();
        }

        /// <summary>
        /// Applica tutto: colori del tema, testo e layout.
        /// </summary>
        public void Apply()
        {
            PanelLayout.ApplyGlass(glass, theme);
            ApplyTexts();
            RefreshLayout();
        }

        /// <summary>
        /// Ricalcolo una tantum: impila le righe attive di ContentRoot, adatta l'altezza al contenuto (minimo =
        /// titolo + padding), riposiziona ombra, accanto ad AttachTo e inclinazione. Da chiamare dopo aver aggiunto,
        /// tolto o ridimensionato righe.
        /// </summary>
        public void RefreshLayout()
        {
            if (theme == null) return;
            var root = (RectTransform)transform;
            float padding = theme.PaddingPanel;
            float rowGap = theme.GetNumber(NumberTokens.SIDE_PANEL_ROW_GAP);
            bool hasTitle = titleText != null && !string.IsNullOrEmpty(titleKey);
            float titleLine = hasTitle ? PanelLayout.LineHeight(theme.BodyBold) : 0f;

            // Righe: impilate dall'alto, larghe quanto il contenuto, altezza propria di ogni riga.
            float contentHeight = 0f;
            int rows = 0;
            if (contentRoot != null)
            {
                foreach (Transform child in contentRoot)
                {
                    if (!child.gameObject.activeSelf || !(child is RectTransform row)) continue;
                    float rowHeight = row.rect.height;
                    if (rows > 0) contentHeight += rowGap;
                    PanelLayout.SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -contentHeight - rowHeight), new Vector2(0f, -contentHeight));
                    contentHeight += rowHeight;
                    rows++;
                }
            }
            float gapAfterTitle = hasTitle && rows > 0 ? rowGap : 0f;
            float height = padding * 2f + titleLine + gapAfterTitle + contentHeight;

            // Radice: pivot in alto a destra (cerniera dell'inclinazione, vicino a PANEL_Menu).
            if (root.pivot != Vector2.one) root.pivot = Vector2.one;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, theme.GetNumber(NumberTokens.SIDE_PANEL_WIDTH));
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            PanelLayout.ApplyShadow(shadowImage, theme, ShadowRoles.SIDE_PANEL);
            if (glass != null)
                PanelLayout.SetRect(glass.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            if (titleText != null)
            {
                PanelLayout.SetActive(titleText.gameObject, hasTitle);
                PanelLayout.SetRect((RectTransform)titleText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(padding, -padding - titleLine), new Vector2(-padding, -padding));
            }
            if (contentRoot != null)
            {
                float top = padding + titleLine + gapAfterTitle;
                PanelLayout.SetRect(contentRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(padding, -top - contentHeight), new Vector2(-padding, -top));
            }

            PlaceNextToTarget(root);

            // Inclinazione: rotazione attorno all'asse verticale passante per il bordo destro. Il fronte della UI guarda
            // -Z locale del Canvas (verso il giocatore): un angolo negativo porta avanti il bordo sinistro.
            Quaternion tilt = Quaternion.Euler(0f, -TiltAngle, 0f);
            if (root.localRotation != tilt) root.localRotation = tilt;
        }

        /// <summary>
        /// Se AttachTo è un fratello, copia le sue ancore e mette il bordo destro di questo pannello a gap.side-panel
        /// dal suo bordo sinistro, bordi superiori allineati (layoutMissionBrief).
        /// </summary>
        private void PlaceNextToTarget(RectTransform root)
        {
            if (attachTo == null || attachTo == root || attachTo.parent != root.parent) return;
            if (root.anchorMin != attachTo.anchorMin) root.anchorMin = attachTo.anchorMin;
            if (root.anchorMax != attachTo.anchorMax) root.anchorMax = attachTo.anchorMax;
            Rect target = attachTo.rect;
            Vector3 topLeft = attachTo.localPosition + new Vector3(target.xMin, target.yMax, 0f);
            Vector3 position = topLeft + new Vector3(-theme.GetNumber(NumberTokens.SIDE_PANEL_GAP), 0f, 0f);
            if (root.localPosition != position) root.localPosition = position;
        }

        /// <summary>
        /// Scrive la chiave del titolo nell'etichetta (se diversa).
        /// </summary>
        private void ApplyTexts()
        {
            if (titleText != null && !string.IsNullOrEmpty(titleKey) && titleText.Key != titleKey) titleText.SetKey(titleKey);
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
        /// Imposta dati e riferimenti, poi applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UnityEngine.UI.Image newShadow, RoundedPanelGraphic newGlass,
            UIStringText newTitle, RectTransform newContent)
        {
            theme = newTheme;
            shadowImage = newShadow;
            glass = newGlass;
            titleText = newTitle;
            contentRoot = newContent;
            Apply();
        }

        /// <summary>
        /// Imposta i parametri della singola istanza (titolo, inclinazione, pannello accanto), poi applica. Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureParameters(string newTitleKey, float newTiltAngle, RectTransform newAttachTo)
        {
            titleKey = newTitleKey;
            tiltAngle = newTiltAngle;
            attachTo = newAttachTo;
            Apply();
        }
    }
}
