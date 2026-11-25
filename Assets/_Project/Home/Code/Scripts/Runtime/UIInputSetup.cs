using System.Collections.Generic;
using SI81.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace SI81.UI
{
    /// <summary>
    /// Configura l'unico EventSystem dell'app (figlio di MNG_UI) collegando <see cref="InputSystemUIInputModule"/>
    /// alle azioni UI di <see cref="InputService"/> (mappa <c>UI</c>): mouse in Editor e posa/grilletto dei due controller.
    /// Non crea un asset di azioni proprio: l'unico proprietario delle azioni è l'Input Service, che gestisce anche
    /// Menu e Back. Imposta l'origine del tracciamento XR e aggiunge la diagnostica <see cref="UIInputLog"/>.
    /// Nessun XR Interaction Toolkit.
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

        // Riferimenti creati qui sopra le azioni dell'Input Service: vanno distrutti con il componente.
        private readonly List<InputActionReference> createdReferences = new List<InputActionReference>(5);

        /// <summary>Origine del tracciamento XR usata dal modulo.</summary>
        public Transform XrTrackingOrigin => xrTrackingOrigin;

        /// <summary>
        /// Collega il modulo alle azioni UI dell'Input Service prima che il modulo si abiliti.
        /// </summary>
        private void Awake()
        {
            if (inputModule == null) inputModule = GetComponent<InputSystemUIInputModule>();

            InputService service = InputService.EnsureInstance();
            InputActionMap ui = service.UIMap;

            // L'asset va assegnato prima dei singoli riferimenti: il modulo cerca le azioni nel suo asset.
            inputModule.actionsAsset = service.Actions;
            inputModule.point = Reference(ui, InputBindings.Point);
            inputModule.leftClick = Reference(ui, InputBindings.Click);
            inputModule.scrollWheel = Reference(ui, InputBindings.Scroll);
            inputModule.trackedDevicePosition = Reference(ui, InputBindings.TrackedDevicePosition);
            inputModule.trackedDeviceOrientation = Reference(ui, InputBindings.TrackedDeviceOrientation);
            // Navigazione da tastiera/gamepad non usata nei menu VR: nessuna azione move/submit/cancel.
            // Back resta di InputService: UIScreenManager la abilita solo a menu visibile.
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
        }

        /// <summary>
        /// Distrugge i riferimenti creati a runtime.
        /// </summary>
        private void OnDestroy()
        {
            for (int i = 0; i < createdReferences.Count; i++)
                if (createdReferences[i] != null) Destroy(createdReferences[i]);
            createdReferences.Clear();
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
        /// Crea un riferimento all'azione indicata e lo ricorda per la distruzione.
        /// </summary>
        /// <param name="map">Mappa UI dell'Input Service.</param>
        /// <param name="actionName">Nome dell'azione (costante di <see cref="InputBindings"/>).</param>
        /// <returns>Riferimento da assegnare al modulo.</returns>
        private InputActionReference Reference(InputActionMap map, string actionName)
        {
            InputActionReference reference = InputActionReference.Create(map.FindAction(actionName, true));
            createdReferences.Add(reference);
            return reference;
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
        /// Imposta modulo e origine del tracciamento. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(InputSystemUIInputModule module, Transform trackingOrigin)
        {
            inputModule = module;
            xrTrackingOrigin = trackingOrigin;
        }
    }
}
