using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace SI81.UI
{
    /// <summary>
    /// Configura a runtime l'unico EventSystem dell'app (figlio di MNG_UI): assegna a InputSystemUIInputModule le
    /// azioni create da <see cref="UIInputActions"/> (mouse in Editor + posizione/rotazione/grilletto dei due controller),
    /// imposta l'origine del tracciamento XR e gestisce l'azione "Back" (B/Y, Esc, Backspace) che espone con
    /// l'evento <see cref="BackPerformed"/>. Nessun XR Interaction Toolkit (CLAUDE.md §4.12).
    /// Gira prima del modulo (ordine di esecuzione molto basso), così il modulo non crea le sue azioni di default.
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InputSystemUIInputModule))]
    public class UIInputSetup : MonoBehaviour
    {
        /// <summary>Ordine di esecuzione: prima di InputSystemUIInputModule (0) e dopo UIScreenManager.</summary>
        public const int EXECUTION_ORDER = -10000;

        [Tooltip("Modulo di input UI dello stesso GameObject.")]
        [SerializeField] private InputSystemUIInputModule inputModule;
        [Tooltip("Origine del tracciamento del rig (il genitore di camera e controller, es. CameraOffset / TrackingSpace). " +
                 "Se vuoto viene usato il genitore di Camera.main (con un avviso: il rig deve chiamare SetTrackingOrigin).")]
        [SerializeField] private Transform xrTrackingOrigin;

        // Asset delle azioni creato in memoria (uno per EventSystem).
        private InputActionAsset actions;
        // Azione "indietro".
        private InputAction backAction;
        // true se "indietro" può scattare: lo decide UIScreenManager (solo con il menu visibile).
        private bool backEnabled = true;

        /// <summary>Evento: l'utente ha chiesto "indietro" (B/Y, Esc, Backspace).</summary>
        public event Action BackPerformed;

        /// <summary>Asset delle azioni UI creato a runtime (null prima di Awake).</summary>
        public InputActionAsset Actions => actions;
        /// <summary>Origine del tracciamento XR usata dal modulo.</summary>
        public Transform XrTrackingOrigin => xrTrackingOrigin;
        /// <summary>true se l'azione "indietro" è abilitata (vedi <see cref="SetBackEnabled"/>).</summary>
        public bool BackEnabled => backEnabled;

        /// <summary>
        /// Crea le azioni e le assegna al modulo prima che il modulo si abiliti.
        /// </summary>
        private void Awake()
        {
            if (inputModule == null) inputModule = GetComponent<InputSystemUIInputModule>();

            actions = UIInputActions.CreateAsset();
            InputActionMap pointer = actions.FindActionMap(UIInputActions.POINTER_MAP, true);

            // L'asset va assegnato prima dei singoli riferimenti: il modulo cerca le azioni nel suo asset.
            inputModule.actionsAsset = actions;
            inputModule.point = InputActionReference.Create(pointer.FindAction(UIInputActions.ACTION_POINT, true));
            inputModule.leftClick = InputActionReference.Create(pointer.FindAction(UIInputActions.ACTION_CLICK, true));
            inputModule.scrollWheel = InputActionReference.Create(pointer.FindAction(UIInputActions.ACTION_SCROLL, true));
            inputModule.trackedDevicePosition = InputActionReference.Create(pointer.FindAction(UIInputActions.ACTION_TRACKED_POSITION, true));
            inputModule.trackedDeviceOrientation = InputActionReference.Create(pointer.FindAction(UIInputActions.ACTION_TRACKED_ORIENTATION, true));
            // Navigazione da tastiera/gamepad non usata nei menu VR: nessuna azione move/submit/cancel.
            inputModule.move = null;
            inputModule.submit = null;
            inputModule.cancel = null;
            inputModule.rightClick = null;
            inputModule.middleClick = null;
            // Ogni puntatore resta separato: senza questo, un evento di un mouse o di un puntatore di sistema (Quest/Android)
            // cancella i puntatori dei controller ogni volta e i raggi non colpiscono più la UI.
            inputModule.pointerBehavior = UIPointerBehavior.AllPointersAsIs;
            // Righe "[SI81 Input]" nel log del visore (versione, controller, esito di ogni grilletto).
            if (GetComponent<UIInputLog>() == null) gameObject.AddComponent<UIInputLog>();

            ApplyTrackingOrigin();

            backAction = actions.FindActionMap(UIInputActions.NAVIGATION_MAP, true).FindAction(UIInputActions.ACTION_BACK, true);
        }

        /// <summary>
        /// Abilita l'azione "Back" e si iscrive al suo evento.
        /// </summary>
        private void OnEnable()
        {
            if (backAction == null) return;
            backAction.performed += HandleBack;
            RefreshBackAction();
        }

        /// <summary>
        /// Disabilita l'azione "Back".
        /// </summary>
        private void OnDisable()
        {
            if (backAction == null) return;
            backAction.performed -= HandleBack;
            backAction.Disable();
        }

        /// <summary>
        /// Distrugge l'asset delle azioni creato in memoria.
        /// </summary>
        private void OnDestroy()
        {
            if (actions != null) Destroy(actions);
        }

        /// <summary>
        /// Abilita o disabilita l'azione "indietro" (B/Y, Esc, Backspace). UIScreenManager la tiene attiva solo mentre
        /// il menu è visibile, così durante le missioni quei tasti restano liberi per lo scenario.
        /// </summary>
        /// <param name="value">true per abilitarla.</param>
        public void SetBackEnabled(bool value)
        {
            backEnabled = value;
            RefreshBackAction();
        }

        /// <summary>
        /// Allinea lo stato dell'azione "Back" a <see cref="backEnabled"/> e allo stato del componente.
        /// </summary>
        private void RefreshBackAction()
        {
            if (backAction == null) return;
            if (backEnabled && isActiveAndEnabled) backAction.Enable();
            else backAction.Disable();
        }

        /// <summary>
        /// Cambia l'origine del tracciamento XR (es. quando il rig viene caricato dopo MNG_UI).
        /// </summary>
        /// <param name="origin">Genitore di camera e controller nel rig.</param>
        public void SetTrackingOrigin(Transform origin)
        {
            xrTrackingOrigin = origin;
            ApplyTrackingOrigin();
        }

        /// <summary>
        /// Assegna l'origine del tracciamento al modulo; se manca usa il genitore di Camera.main (con un avviso).
        /// </summary>
        private void ApplyTrackingOrigin()
        {
            if (xrTrackingOrigin == null)
            {
                Transform head = UIRigLocator.ResolveHead(null, "UIInputSetup");
                if (head != null) xrTrackingOrigin = head.parent;
            }
            if (inputModule != null) inputModule.xrTrackingOrigin = xrTrackingOrigin;
        }

        /// <summary>
        /// Rilancia la pressione di "Back" agli iscritti (UIScreenManager).
        /// </summary>
        /// <param name="context">Contesto dell'azione (non usato).</param>
        private void HandleBack(InputAction.CallbackContext context)
        {
            BackPerformed?.Invoke();
        }

        /// <summary>
        /// Imposta modulo e origine del tracciamento. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(InputSystemUIInputModule module, Transform trackingOrigin)
        {
            inputModule = module;
            xrTrackingOrigin = trackingOrigin;
        }
    }
}
