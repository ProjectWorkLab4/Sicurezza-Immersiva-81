using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Barra di navigazione dei menu (prefab PANEL_BottomBar): pillola 520×56 in vetro chiaro (color.surface.glass-light,
    /// nessuna sfocatura in tempo reale), ombra bottomBar, 4 voci BTN_BottomBarItem distribuite in modo uniforme dentro
    /// il padding (space-between). Va nel posto "PANEL_BottomBarSlot" di CANVAS_Menu (MNG_UI), UNA sola istanza.
    /// Navigazione: le voci sono di primo livello → <see cref="UIScreenManager.ShowRoot"/> (cronologia svuotata: da una
    /// scheda della barra il tasto ← non porta da nessuna parte; Home = GoHome()).
    /// Voce attiva: segue <see cref="UIScreenManager.ScreenChanged"/>; le schermate figlie usano la voce della propria
    /// sezione (<see cref="SectionAlias"/>: Mission Brief e Training brief → Home, come la descrizione Figma).
    /// Posizione: centrata sotto il PANEL_Menu della schermata corrente, a gap.bottom-bar.menu (16) dal suo bordo
    /// inferiore (Home: y 531 nel frame 840×627; Mission Brief, pannello alto 535: 60 più in basso). Ricalcolata solo
    /// al cambio di schermata. Nessun Layout Group: posizioni delle voci calcolate una volta in <see cref="Apply"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class BottomBar : MonoBehaviour
    {
        /// <summary>Schermata figlia → voce della sezione a cui appartiene.</summary>
        [Serializable]
        public class SectionAlias
        {
            /// <summary>Schermata figlia (es. MissionBrief).</summary>
            public UIScreenId Screen;
            /// <summary>Voce della barra da mostrare attiva (es. Home).</summary>
            public UIScreenId Section;
        }

        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Schermate figlie e voce della sezione da mostrare attiva. Schermate non elencate e senza voce: nessuna voce attiva.")]
        [SerializeField] private List<SectionAlias> sectionAliases = new List<SectionAlias>
        {
            new SectionAlias { Screen = UIScreenId.MissionBrief, Section = UIScreenId.Home },
            new SectionAlias { Screen = UIScreenId.TrainingBrief, Section = UIScreenId.Home }
        };
        [Tooltip("Segue il PANEL_Menu della schermata corrente (16 sotto il suo bordo inferiore). Disattivato = posizione del prefab/istanza.")]
        [SerializeField] private bool followMenuPanel = true;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private UnityEngine.UI.Image shadowImage;
        [SerializeField] private RoundedPanelGraphic glass;
        [SerializeField] private RectTransform itemsRoot;
        [SerializeField] private List<BottomBarItem> items = new List<BottomBarItem>();

        // Gestore a cui la barra è iscritta.
        private UIScreenManager manager;

        /// <summary>Voci della barra, nell'ordine.</summary>
        public IReadOnlyList<BottomBarItem> Items => items;

        /// <summary>Voce attiva (null se nessuna).</summary>
        public BottomBarItem ActiveItem
        {
            get
            {
                foreach (BottomBarItem item in items)
                    if (item != null && item.IsActive) return item;
                return null;
            }
        }

        /// <summary>
        /// Applica tema e layout e si collega ai clic delle voci.
        /// </summary>
        private void Awake()
        {
            Apply();
            foreach (BottomBarItem item in items)
                if (item != null) item.Clicked += HandleItemClicked;
        }

        /// <summary>
        /// Si iscrive al cambio di schermata e si allinea alla schermata corrente.
        /// </summary>
        private void OnEnable()
        {
            manager = UIScreenManager.Instance != null ? UIScreenManager.Instance : GetComponentInParent<UIScreenManager>();
            if (manager == null) return;
            manager.ScreenChanged += HandleScreenChanged;
            if (manager.CurrentScreen.HasValue) Refresh(manager.CurrentScreen.Value);
        }

        /// <summary>
        /// Si disiscrive dal cambio di schermata.
        /// </summary>
        private void OnDisable()
        {
            if (manager != null) manager.ScreenChanged -= HandleScreenChanged;
            manager = null;
        }

        /// <summary>
        /// Rilascia i collegamenti alle voci.
        /// </summary>
        private void OnDestroy()
        {
            foreach (BottomBarItem item in items)
                if (item != null) item.Clicked -= HandleItemClicked;
        }

        /// <summary>
        /// Applica tutto: vetro chiaro, ombra, misure della barra e posizioni delle voci (una tantum).
        /// </summary>
        public void Apply()
        {
            if (theme == null) return;
            var root = (RectTransform)transform;
            float width = theme.GetNumber(NumberTokens.BOTTOM_BAR_WIDTH);
            float height = theme.GetNumber(NumberTokens.BOTTOM_BAR_HEIGHT);
            float padding = theme.GetNumber(NumberTokens.BOTTOM_BAR_PADDING);
            if (Mathf.Abs(root.rect.width - width) > 0.001f) root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (Mathf.Abs(root.rect.height - height) > 0.001f) root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            // Vetro chiaro senza riflesso (Figma: nessun "sheen" sulla barra) e ombra bottomBar.
            if (glass != null)
            {
                if (glass.SheenEnabled) glass.DisableSheen();
                if (glass.color != theme.SurfaceGlassLight) glass.color = theme.SurfaceGlassLight;
                PanelLayout.SetRect(glass.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }
            PanelLayout.ApplyShadow(shadowImage, theme, ShadowRoles.BOTTOM_BAR);
            if (itemsRoot != null)
                PanelLayout.SetRect(itemsRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(padding, padding), new Vector2(-padding, -padding));

            LayoutItems(width - 2f * padding);
        }

        /// <summary>
        /// Distribuisce le voci attive nello spazio interno (space-between): prima a sinistra, ultima a destra,
        /// spazi uguali tra le altre (Figma: x 6 / 115 / 227 / 366, gap 8).
        /// </summary>
        /// <param name="innerWidth">Larghezza interna (barra − 2 × padding).</param>
        private void LayoutItems(float innerWidth)
        {
            float total = 0f;
            int count = 0;
            foreach (BottomBarItem item in items)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                item.Apply();
                total += item.GetPreferredWidth();
                count++;
            }
            float gap = count > 1 ? (innerWidth - total) / (count - 1) : 0f;
            float x = 0f;
            foreach (BottomBarItem item in items)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                float itemWidth = item.GetPreferredWidth();
                var rect = (RectTransform)item.transform;
                // Ancorata a sinistra e centrata in verticale; pivot al centro (la scala pressed resta centrata).
                PanelLayout.SetRect(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, -rect.rect.height * 0.5f), new Vector2(x + itemWidth, rect.rect.height * 0.5f));
                x += itemWidth + gap;
            }
        }

        /// <summary>
        /// Aggiorna voce attiva e posizione per la schermata data (chiamato da ScreenChanged; pubblico per le prove).
        /// </summary>
        /// <param name="screen">Schermata corrente.</param>
        public void Refresh(UIScreenId screen)
        {
            UIScreenId section = GetSection(screen);
            foreach (BottomBarItem item in items)
                if (item != null) item.SetActive(item.Screen == section);
            if (followMenuPanel) FollowMenuPanel(screen);
        }

        /// <summary>
        /// Voce della sezione di una schermata: la schermata stessa se ha una voce, altrimenti l'alias configurato.
        /// </summary>
        /// <param name="screen">Schermata corrente.</param>
        /// <returns>Id della voce da mostrare attiva (può non corrispondere a nessuna voce).</returns>
        public UIScreenId GetSection(UIScreenId screen)
        {
            foreach (SectionAlias alias in sectionAliases)
                if (alias != null && alias.Screen == screen) return alias.Section;
            return screen;
        }

        /// <summary>
        /// Mette la barra centrata sotto il PANEL_Menu della schermata, a gap.bottom-bar.menu dal suo bordo inferiore.
        /// Se la schermata non ha un PANEL_Menu la posizione resta quella attuale.
        /// </summary>
        /// <param name="screen">Schermata corrente.</param>
        private void FollowMenuPanel(UIScreenId screen)
        {
            if (manager == null || theme == null) return;
            UIScreen screenRoot = manager.GetScreen(screen);
            MenuPanel menu = screenRoot != null ? screenRoot.GetComponentInChildren<MenuPanel>(true) : null;
            var root = (RectTransform)transform;
            if (menu == null || root.parent == null) return;

            var menuRect = (RectTransform)menu.transform;
            Rect r = menuRect.rect;
            // Centro del bordo inferiore di PANEL_Menu, portato nello spazio del genitore della barra.
            Vector3 bottomCenter = root.parent.InverseTransformPoint(menuRect.TransformPoint(new Vector3(r.center.x, r.yMin, 0f)));
            // Pivot della barra in alto al centro (impostato dal builder): bordo superiore a 16 sotto il pannello.
            Rect bar = root.rect;
            Vector3 pivotOffset = new Vector3((root.pivot.x - 0.5f) * bar.width, (root.pivot.y - 1f) * bar.height, 0f);
            Vector3 target = new Vector3(bottomCenter.x, bottomCenter.y - theme.GetNumber(NumberTokens.BOTTOM_BAR_MENU_GAP), root.localPosition.z) + pivotOffset;
            if (root.localPosition != target) root.localPosition = target;
        }

        /// <summary>
        /// Clic di una voce: apre la sua schermata come radice (Home: torna alla radice).
        /// </summary>
        /// <param name="item">Voce cliccata.</param>
        private void HandleItemClicked(BottomBarItem item)
        {
            UIScreenManager target = manager != null ? manager : UIScreenManager.Instance;
            if (target == null || item == null) return;
            if (item.Screen == target.HomeScreen) target.GoHome();
            else target.ShowRoot(item.Screen);
        }

        /// <summary>
        /// Cambio di schermata: aggiorna voce attiva e posizione.
        /// </summary>
        private void HandleScreenChanged(UIScreenId? previous, UIScreenId next)
        {
            Refresh(next);
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
        /// Imposta tema, riferimenti e voci, poi applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, UnityEngine.UI.Image newShadow, RoundedPanelGraphic newGlass,
            RectTransform newItemsRoot, List<BottomBarItem> newItems)
        {
            theme = newTheme;
            shadowImage = newShadow;
            glass = newGlass;
            itemsRoot = newItemsRoot;
            items = newItems;
            Apply();
        }
    }
}
