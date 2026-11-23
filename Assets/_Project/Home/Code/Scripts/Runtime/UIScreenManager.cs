using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.UI
{
    /// <summary>
    /// Gestore delle schermate menu (script del prefab MNG_UI). Registra le <see cref="UIScreen"/> figlie del contenitore
    /// "PANEL_Screens", ne mostra una alla volta e tiene la cronologia per il tasto ← (BTN_Back, B/Y, Esc, Backspace).
    /// Show(id) mette la schermata corrente nella cronologia; Back() torna alla precedente (nessun effetto sulla radice);
    /// GoHome() svuota la cronologia. Ne esiste UNO solo: un secondo MNG_UI viene segnalato e distrutto.
    /// Visibilità del menu: <see cref="ShowMenu"/> / <see cref="HideMenu"/> mostrano o nascondono CANVAS_Menu; l'azione
    /// "indietro" (B/Y, Esc, Backspace) è attiva SOLO a menu visibile, quindi durante le missioni non scatta.
    /// Posizionamento: <see cref="PlaceInFront"/> / <see cref="Recenter"/> mettono il menu a 1,5 m davanti alla testa
    /// (solo imbardata, rivolto verso la testa); il rig condiviso assegna testa e origine con <see cref="SetHead"/> e
    /// <see cref="SetTrackingOrigin"/> (se mancano si usa Camera.main, con un avviso).
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    public class UIScreenManager : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: prima di tutto il resto della UI, così il controllo del duplicato avviene subito.</summary>
        public const int EXECUTION_ORDER = -10001;

        [Tooltip("Contenitore delle schermate (figlio \"PANEL_Screens\" di CANVAS_Menu): ogni UIScreen figlio viene registrato.")]
        [SerializeField] private RectTransform screensContainer;
        [Tooltip("Posto della bottom bar (figlio \"PANEL_BottomBarSlot\" di CANVAS_Menu): qui va l'istanza di PANEL_BottomBar.")]
        [SerializeField] private RectTransform bottomBarSlot;
        [Tooltip("Configurazione dell'input UI (sul MNG_EventSystem figlio): fornisce l'azione \"Back\".")]
        [SerializeField] private UIInputSetup inputSetup;
        [Tooltip("Schermata radice, mostrata all'avvio e da GoHome().")]
        [SerializeField] private UIScreenId homeScreen = UIScreenId.Home;

        [Header("Menu: visibilità e posizione")]
        [Tooltip("Tema comune SO_UITheme: distanza e altezza del menu (distance.menu, height.menu-center).")]
        [SerializeField] private UITheme theme;
        [Tooltip("CANVAS_Menu: il GameObject che ShowMenu()/HideMenu() attivano e PlaceInFront() sposta.")]
        [SerializeField] private GameObject menuRoot;
        [Tooltip("Testa del giocatore (camera del rig). Se vuota si usa Camera.main con un avviso. Il rig condiviso la assegna con SetHead().")]
        [SerializeField] private Transform head;
        [Tooltip("Mostra il menu (e abilita \"indietro\") all'avvio. Disattivare nelle scene che partono già in missione.")]
        [SerializeField] private bool showMenuOnStart = true;
        [Tooltip("Rimette il menu davanti alla testa ogni volta che viene riaperto con ShowMenu().")]
        [SerializeField] private bool recenterOnShow = true;

        // Schermate registrate, per id.
        private readonly Dictionary<UIScreenId, UIScreen> screens = new Dictionary<UIScreenId, UIScreen>();
        // Cronologia per il tasto ←: in cima la schermata precedente.
        private readonly Stack<UIScreenId> history = new Stack<UIScreenId>();
        // Schermata corrente (null finché non ne è stata mostrata una).
        private UIScreen current;
        // true se il menu (CANVAS_Menu) è visibile.
        private bool menuVisible;
        // true finché il posizionamento iniziale non è stato rifatto dopo il primo frame (a Start la testa può non essere ancora tracciata).
        private bool recenterPending;
        // Istante (tempo reale) dell'avvio, per il limite di attesa della prima posa della testa.
        private float startTime;
        /// <summary>Secondi massimi di attesa della prima posa vera della testa prima di centrare comunque il menu.</summary>
        private const float RECENTER_WAIT_SECONDS = 3f;
        // true dopo Start (prima non va ripetuto Show della schermata corrente).
        private bool initialized;

        /// <summary>L'unico gestore attivo (null se non esiste o è stato distrutto).</summary>
        public static UIScreenManager Instance { get; private set; }

        /// <summary>
        /// Evento: la schermata corrente è cambiata. Parametri: schermata precedente (null all'avvio) e nuova schermata.
        /// </summary>
        public event Action<UIScreenId?, UIScreenId> ScreenChanged;

        /// <summary>Evento: il menu è stato mostrato (true) o nascosto (false).</summary>
        public event Action<bool> MenuVisibilityChanged;

        /// <summary>Id della schermata corrente (null prima dell'avvio).</summary>
        public UIScreenId? CurrentScreen => current != null ? current.Id : (UIScreenId?)null;
        /// <summary>Numero di schermate nella cronologia (0 = sulla radice, Back() non fa nulla).</summary>
        public int HistoryCount => history.Count;
        /// <summary>Schermata radice.</summary>
        public UIScreenId HomeScreen => homeScreen;
        /// <summary>Contenitore delle schermate.</summary>
        public RectTransform ScreensContainer => screensContainer;
        /// <summary>Posto della bottom bar.</summary>
        public RectTransform BottomBarSlot => bottomBarSlot;
        /// <summary>true se il menu è visibile (solo allora "indietro" è attivo).</summary>
        public bool IsMenuVisible => menuVisible;
        /// <summary>Canvas del menu (CANVAS_Menu), null se non assegnato.</summary>
        public GameObject MenuRoot => menuRoot;
        /// <summary>Testa assegnata con <see cref="SetHead"/> (null se non assegnata).</summary>
        public Transform Head => head;
        /// <summary>Origine del tracciamento assegnata con <see cref="SetTrackingOrigin"/> (null se non assegnata).</summary>
        public Transform TrackingOrigin => inputSetup != null ? inputSetup.XrTrackingOrigin : null;
        /// <summary>Camera della testa assegnata (cercata anche nei figli), null se non c'è.</summary>
        public Camera HeadCamera => head != null ? (head.GetComponent<Camera>() ?? head.GetComponentInChildren<Camera>()) : null;

        /// <summary>
        /// Controlla che non esista già un altro MNG_UI e registra le schermate.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[MNG_UI] Esiste già un MNG_UI attivo (\"{Instance.gameObject.scene.name}\"): " +
                                 $"il duplicato nella scena \"{gameObject.scene.name}\" viene distrutto. " +
                                 "MNG_UI va messo una sola volta, in SCN_Core.", this);
                // Disattivato subito, così il suo EventSystem non entra mai in funzione.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RegisterScreens();
        }

        /// <summary>
        /// Si iscrive all'azione "Back" e al caricamento delle scene (controllo dei conflitti di input).
        /// </summary>
        private void OnEnable()
        {
            if (Instance != this) return;
            if (inputSetup != null) inputSetup.BackPerformed += Back;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        /// <summary>
        /// Si disiscrive dall'azione "Back" e dal caricamento delle scene.
        /// </summary>
        private void OnDisable()
        {
            if (inputSetup != null) inputSetup.BackPerformed -= Back;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>
        /// All'avvio nasconde tutte le schermate e mostra la radice (senza dissolvenza), poi mostra o nasconde il menu
        /// (e con esso l'azione "indietro"), lo mette davanti alla testa e controlla i conflitti di input.
        /// </summary>
        private void Start()
        {
            if (Instance != this) return;
            foreach (UIScreen screen in screens.Values) screen.Hide(true);
            if (screens.ContainsKey(homeScreen)) SwitchTo(homeScreen, true);
            else if (screens.Count > 0) Debug.LogWarning($"[MNG_UI] Schermata radice {homeScreen} non registrata.", this);

            // Posizione iniziale; ripetuta dopo il primo frame (a Start la testa può non essere ancora tracciata).
            recenterPending = true;
            startTime = Time.unscaledTime;
            Recenter();
            if (showMenuOnStart) ShowMenu();
            else HideMenu();
            initialized = true;
            UIInputDiagnostics.WarnOnce();
        }

        /// <summary>
        /// Dopo il primo frame rifà il posizionamento iniziale del menu (se la testa non c'è ancora riprova ad ogni frame).
        /// </summary>
        private void LateUpdate()
        {
            if (!recenterPending) return;
            Transform headTransform = head != null ? head : (Camera.main != null ? Camera.main.transform : null);
            if (headTransform == null) return;
            // Con il visore attivo i primi frame danno ancora la posa nulla: aspetta la prima posa vera (al massimo
            // RECENTER_WAIT_SECONDS), altrimenti il menu finisce davanti all'origine del rig invece che al giocatore.
            bool xrActive = UnityEngine.XR.XRSettings.isDeviceActive;
            bool headTracked = headTransform.localPosition.sqrMagnitude > 0.0001f;
            if (xrActive && !headTracked && Time.unscaledTime - startTime < RECENTER_WAIT_SECONDS) return;
            recenterPending = false;
            Recenter();
        }

        /// <summary>
        /// Azzera il riferimento statico quando il gestore viene distrutto.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Dopo il caricamento di una scena controlla che non sia arrivato un TrackedDeviceRaycaster o un secondo EventSystem.
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UIInputDiagnostics.WarnOnce();
        }

        /// <summary>
        /// Registra tutte le UIScreen figlie del contenitore (anche disattivate). Le registrazioni precedenti restano.
        /// </summary>
        public void RegisterScreens()
        {
            if (screensContainer == null)
            {
                Debug.LogWarning("[MNG_UI] Contenitore \"PANEL_Screens\" non assegnato.", this);
                return;
            }
            foreach (UIScreen screen in screensContainer.GetComponentsInChildren<UIScreen>(true)) RegisterScreen(screen);
        }

        /// <summary>
        /// Registra una schermata (es. creata a runtime). Se l'id è già usato da un'altra schermata scrive un avviso e la ignora.
        /// </summary>
        /// <param name="screen">Schermata da registrare.</param>
        public void RegisterScreen(UIScreen screen)
        {
            if (screen == null) return;
            if (screens.TryGetValue(screen.Id, out UIScreen existing) && existing != screen && existing != null)
            {
                Debug.LogWarning($"[MNG_UI] Id {screen.Id} già usato da \"{existing.name}\": \"{screen.name}\" ignorata.", screen);
                return;
            }
            screens[screen.Id] = screen;
        }

        /// <summary>
        /// Indica se una schermata è registrata.
        /// </summary>
        /// <param name="id">Id della schermata.</param>
        /// <returns>true se registrata.</returns>
        public bool HasScreen(UIScreenId id)
        {
            return screens.ContainsKey(id);
        }

        /// <summary>
        /// Mostra una schermata e mette la corrente nella cronologia. Nessun effetto se è già la corrente.
        /// </summary>
        /// <param name="id">Schermata da mostrare.</param>
        public void Show(UIScreenId id)
        {
            if (current != null && current.Id == id) return;
            if (!screens.ContainsKey(id))
            {
                Debug.LogWarning($"[MNG_UI] Schermata {id} non registrata.", this);
                return;
            }
            if (current != null) history.Push(current.Id);
            SwitchTo(id, false);
        }

        /// <summary>
        /// Versione per UnityEvent (l'Inspector non accetta enum): mostra la schermata con il valore numerico dato.
        /// </summary>
        /// <param name="id">Valore numerico di <see cref="UIScreenId"/>.</param>
        public void ShowById(int id)
        {
            Show((UIScreenId)id);
        }

        /// <summary>
        /// Torna alla schermata precedente (BTN_Back, B/Y, Esc, Backspace). Nessun effetto sulla radice né a menu nascosto
        /// (durante le missioni "indietro" non deve scattare).
        /// </summary>
        public void Back()
        {
            if (!menuVisible) return;
            // Ignora gli id non più registrati (es. schermata rimossa).
            while (history.Count > 0)
            {
                UIScreenId previous = history.Pop();
                if (!screens.ContainsKey(previous)) continue;
                SwitchTo(previous, false);
                return;
            }
        }

        /// <summary>
        /// Torna alla schermata radice e svuota la cronologia.
        /// </summary>
        public void GoHome()
        {
            ShowRoot(homeScreen);
        }

        /// <summary>
        /// Mostra una schermata di primo livello (voci di PANEL_BottomBar): svuota la cronologia, quindi da lì il
        /// tasto ← non porta da nessuna parte. Nessun cambio se è già la corrente (la cronologia viene comunque svuotata).
        /// </summary>
        /// <param name="id">Schermata da mostrare come radice.</param>
        public void ShowRoot(UIScreenId id)
        {
            if (!screens.ContainsKey(id))
            {
                Debug.LogWarning($"[MNG_UI] Schermata {id} non registrata.", this);
                return;
            }
            history.Clear();
            if (current == null || current.Id != id) SwitchTo(id, false);
        }

        /// <summary>
        /// Restituisce la schermata registrata con l'id dato (es. per leggere il suo PANEL_Menu).
        /// </summary>
        /// <param name="id">Id della schermata.</param>
        /// <returns>La schermata, o null se non registrata.</returns>
        public UIScreen GetScreen(UIScreenId id)
        {
            return screens.TryGetValue(id, out UIScreen screen) ? screen : null;
        }

        /// <summary>
        /// Nasconde la schermata corrente, mostra la nuova e lancia <see cref="ScreenChanged"/>. Non tocca la cronologia.
        /// </summary>
        /// <param name="id">Schermata da mostrare.</param>
        /// <param name="instant">true per cambiare senza dissolvenza.</param>
        private void SwitchTo(UIScreenId id, bool instant)
        {
            if (!screens.TryGetValue(id, out UIScreen next) || next == null) return;
            UIScreenId? previous = CurrentScreen;
            if (current != null && current != next) current.Hide(instant);
            current = next;
            current.Show(instant);
            ScreenChanged?.Invoke(previous, id);
        }

        // =====================================================================
        // Visibilità del menu
        // =====================================================================

        /// <summary>
        /// Mostra il menu (CANVAS_Menu), abilita "indietro" e, se <c>recenterOnShow</c>, lo rimette davanti alla testa.
        /// La schermata corrente riappare senza dissolvenza. Da chiamare a fine missione (Debrief, Score &amp; Replay) e per la pausa.
        /// </summary>
        public void ShowMenu()
        {
            bool changed = !menuVisible;
            menuVisible = true;
            if (menuRoot != null && !menuRoot.activeSelf) menuRoot.SetActive(true);
            if (inputSetup != null) inputSetup.SetBackEnabled(true);
            if (!changed) return;
            if (recenterOnShow) Recenter();
            // La dissolvenza in corso è stata interrotta dalla disattivazione: riporta la schermata a stato pieno.
            if (initialized && current != null) current.Show(true);
            MenuVisibilityChanged?.Invoke(true);
        }

        /// <summary>
        /// Nasconde il menu (CANVAS_Menu) e disabilita "indietro": da chiamare all'avvio di una missione.
        /// La cronologia resta (alla riapertura si ritrova la schermata corrente).
        /// </summary>
        public void HideMenu()
        {
            bool changed = menuVisible;
            menuVisible = false;
            if (inputSetup != null) inputSetup.SetBackEnabled(false);
            if (menuRoot != null && menuRoot.activeSelf) menuRoot.SetActive(false);
            if (changed) MenuVisibilityChanged?.Invoke(false);
        }

        // =====================================================================
        // Rig e posizionamento
        // =====================================================================

        /// <summary>
        /// Assegna la testa del giocatore (camera del rig condiviso) e la dà ai canvas World Space come camera degli eventi.
        /// </summary>
        /// <param name="newHead">Transform della camera principale del rig.</param>
        public void SetHead(Transform newHead)
        {
            head = newHead;
            foreach (WorldCanvasCamera canvasCamera in GetComponentsInChildren<WorldCanvasCamera>(true)) canvasCamera.Refresh();
            recenterPending = true;
        }

        /// <summary>
        /// Assegna l'origine del tracciamento del rig (genitore di camera e controller) al modulo di input e al calcolo dell'altezza.
        /// </summary>
        /// <param name="origin">Origine del tracciamento (es. CameraOffset / TrackingSpace).</param>
        public void SetTrackingOrigin(Transform origin)
        {
            if (inputSetup != null) inputSetup.SetTrackingOrigin(origin);
        }

        /// <summary>
        /// Rimette il menu davanti alla testa assegnata (o Camera.main, con un avviso). Nessun effetto se non c'è nessuna testa.
        /// </summary>
        public void Recenter()
        {
            PlaceInFront(UIRigLocator.ResolveHead(head, "UIScreenManager"));
        }

        /// <summary>
        /// Mette CANVAS_Menu davanti alla testa: a <c>distance.menu</c> (1,5 m) lungo la direzione dello sguardo
        /// proiettata sul piano orizzontale (solo imbardata, il menu resta dritto), con il centro all'altezza
        /// <c>height.menu-center</c> sopra l'origine del tracciamento (terra), rivolto verso la testa.
        /// </summary>
        /// <param name="headTransform">Testa del giocatore (camera). Se null non fa nulla.</param>
        public void PlaceInFront(Transform headTransform)
        {
            if (headTransform == null || menuRoot == null || theme == null) return;

            // Direzione orizzontale dello sguardo; se guarda quasi in verticale usa la direzione "su" della testa.
            Vector3 forward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(headTransform.forward.y < 0f ? headTransform.up : -headTransform.up, Vector3.up);
            forward = forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;

            Transform origin = TrackingOrigin;
            float floorY = origin != null ? origin.position.y : 0f;
            Vector3 position = headTransform.position + forward * theme.MenuDistance;
            position.y = floorY + theme.MenuCenterHeight;
            // Il Canvas guarda lungo +Z: orientato come lo sguardo, il suo fronte è rivolto alla testa.
            menuRoot.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        }

        /// <summary>
        /// Imposta i riferimenti interni. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(RectTransform screens, RectTransform bottomBar, UIInputSetup input, UITheme newTheme, GameObject newMenuRoot)
        {
            screensContainer = screens;
            bottomBarSlot = bottomBar;
            inputSetup = input;
            theme = newTheme;
            menuRoot = newMenuRoot;
        }

        /// <summary>
        /// Assegna la testa serializzata (es. alla camera del rig della sandbox). Usato solo dai builder Editor.
        /// </summary>
        internal void ConfigureHead(Transform newHead)
        {
            head = newHead;
        }
    }
}
