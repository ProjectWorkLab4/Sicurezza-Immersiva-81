using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>Mano del controller a cui appartiene un <see cref="UIPointer"/>.</summary>
    public enum UIPointerHand
    {
        /// <summary>Controller sinistro.</summary>
        Left = 0,
        /// <summary>Controller destro.</summary>
        Right = 1
    }

    /// <summary>
    /// Raggio di puntamento UI sul controller (componente SI81, nessun XR Interaction Toolkit). Non fa raycast propri:
    /// legge l'ultimo risultato che InputSystemUIInputModule ha calcolato per quel controller, così il raggio disegnato
    /// coincide sempre con ciò che riceve gli eventi. Disegna una linea (LineRenderer) fino all'elemento UI colpito e un
    /// piccolo reticolo; colore color.pointer.valid sugli elementi interattivi, color.pointer.invalid sugli altri;
    /// si nasconde quando non punta la UI o oltre la lunghezza massima del tema.
    /// Va sul GameObject del controller (quello con TrackedPoseDriver), con la stessa origine di tracciamento del modulo.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIPointer : MonoBehaviour
    {
        /// <summary>Proprietà colore del materiale URP Unlit (impostata con MaterialPropertyBlock, materiale condiviso).</summary>
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

        [Tooltip("Mano del controller: decide quale dispositivo XR leggere.")]
        [SerializeField] private UIPointerHand hand = UIPointerHand.Right;
        [Tooltip("Tema SO_UITheme: colori color.pointer.* e misure Pointer (spessore, lunghezza, reticolo).")]
        [SerializeField] private UITheme theme;
        [Tooltip("Linea del raggio (materiale MAT_UIPointer).")]
        [SerializeField] private LineRenderer line;
        [Tooltip("Reticolo nel punto colpito (quad con materiale MAT_UIPointerReticle).")]
        [SerializeField] private Renderer reticle;

        // Blocco proprietà riusato (nessuna allocazione per frame).
        private MaterialPropertyBlock propertyBlock;
        // Modulo di input UI corrente (cercato dall'EventSystem).
        private InputSystemUIInputModule inputModule;
        // Visibilità corrente di linea e reticolo.
        private bool shown = true;

        /// <summary>Mano del controller.</summary>
        public UIPointerHand Hand => hand;
        /// <summary>true se in questo frame il raggio punta un elemento UI (il raggio corto a riposo non conta).</summary>
        public bool IsPointingAtUI { get; private set; }

        /// <summary>
        /// Prepara linea e reticolo e li nasconde finché non c'è un bersaglio.
        /// </summary>
        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (line != null)
            {
                line.useWorldSpace = true;
                line.positionCount = 2;
                if (theme != null) line.startWidth = line.endWidth = theme.Pointer.RayWidth;
            }
            if (reticle != null && theme != null)
                reticle.transform.localScale = Vector3.one * theme.Pointer.ReticleDiameter;
            SetShown(false);
        }

        /// <summary>
        /// Dopo l'aggiornamento dell'EventSystem legge il risultato del raycast del controller e aggiorna il raggio.
        /// </summary>
        private void LateUpdate()
        {
            InputSystemUIInputModule module = GetModule();
            // Stesso controller che usano vibrazione e (per la mano) il modulo UI: il tracciato aggiornato più di recente.
            XRController device = XRHandDevices.Get(hand == UIPointerHand.Left);
            RaycastResult hit = device != null && module != null ? module.GetLastRaycastResult(device.deviceId) : default;
            if (device == null || theme == null)
            {
                SetShown(false);
                return;
            }

            // Stessa posa che usa il modulo UI (posa "aim" nell'origine di tracciamento): raggio disegnato = raggio degli eventi.
            GetDeviceRay(device, module, out Vector3 origin, out Vector3 forward);

            float distance = hit.isValid && hit.gameObject != null ? Vector3.Distance(origin, hit.worldPosition) : 0f;
            if (distance <= Mathf.Epsilon || distance > theme.Pointer.RayMaxLength)
            {
                ShowIdleRay(origin, forward);
                return;
            }

            Vector3 direction = (hit.worldPosition - origin) / distance;
            Color color = IsInteractive(hit.gameObject) ? theme.PointerValid : theme.PointerInvalid;
            propertyBlock.SetColor(BASE_COLOR_ID, color);

            if (line != null)
            {
                line.SetPosition(0, origin);
                line.SetPosition(1, hit.worldPosition);
                line.SetPropertyBlock(propertyBlock);
            }
            if (reticle != null)
            {
                Transform target = hit.gameObject.transform;
                reticle.transform.SetPositionAndRotation(hit.worldPosition - direction * theme.Pointer.ReticleSurfaceOffset,
                    Quaternion.LookRotation(target.forward, target.up));
                reticle.SetPropertyBlock(propertyBlock);
                reticle.enabled = true;
            }
            IsPointingAtUI = true;
            SetShown(true);
        }

        /// <summary>
        /// Calcola origine e direzione del raggio dalla posa del controller letta dall'Input System, convertita nello spazio mondo
        /// con l'origine di tracciamento del modulo UI; se il modulo non ha un'origine usa il Transform di questo GameObject.
        /// </summary>
        /// <param name="device">Controller XR della mano.</param>
        /// <param name="module">Modulo UI (può essere null).</param>
        /// <param name="origin">Origine del raggio nello spazio mondo.</param>
        /// <param name="forward">Direzione del raggio nello spazio mondo.</param>
        private void GetDeviceRay(XRController device, InputSystemUIInputModule module, out Vector3 origin, out Vector3 forward)
        {
            Transform space = module != null ? module.xrTrackingOrigin : null;
            if (space == null || !device.isTracked.isPressed)
            {
                origin = transform.position;
                forward = transform.forward;
                return;
            }
            // Posa "aim" (pointerPosition/pointerRotation) come i binding del modulo; se il dispositivo non la ha, posa "grip".
            XRHandDevices.ReadAimPose(device, out Vector3 localPosition, out Quaternion localRotation);
            origin = space.TransformPoint(localPosition);
            forward = space.rotation * localRotation * Vector3.forward;
        }

        /// <summary>
        /// Mostra un raggio corto (theme.Pointer.IdleRayLength) senza reticolo quando il controller non punta la UI,
        /// così il giocatore vede sempre dove sta puntando.
        /// </summary>
        /// <param name="origin">Origine del raggio.</param>
        /// <param name="forward">Direzione del raggio.</param>
        private void ShowIdleRay(Vector3 origin, Vector3 forward)
        {
            IsPointingAtUI = false;
            if (theme.Pointer.IdleRayLength <= 0f)
            {
                SetShown(false);
                return;
            }
            propertyBlock.SetColor(BASE_COLOR_ID, theme.PointerValid);
            if (line != null)
            {
                line.SetPosition(0, origin);
                line.SetPosition(1, origin + forward * theme.Pointer.IdleRayLength);
                line.SetPropertyBlock(propertyBlock);
            }
            SetShown(true);
            if (reticle != null) reticle.enabled = false;
        }

        /// <summary>
        /// Nasconde il raggio quando il componente viene disattivato.
        /// </summary>
        private void OnDisable()
        {
            SetShown(false);
        }

        /// <summary>
        /// Restituisce il modulo di input dell'EventSystem corrente (in cache finché resta valido).
        /// </summary>
        /// <returns>Il modulo, o null se non c'è un EventSystem con InputSystemUIInputModule.</returns>
        private InputSystemUIInputModule GetModule()
        {
            if (inputModule != null && inputModule.isActiveAndEnabled) return inputModule;
            EventSystem system = EventSystem.current;
            inputModule = system != null ? system.currentInputModule as InputSystemUIInputModule : null;
            return inputModule;
        }

        /// <summary>
        /// Indica se l'oggetto colpito (o un suo genitore) è un elemento interattivo abilitato.
        /// </summary>
        /// <param name="target">GameObject colpito dal raycast.</param>
        /// <returns>true se è un Selectable interattivo.</returns>
        private static bool IsInteractive(GameObject target)
        {
            Selectable selectable = target.GetComponentInParent<Selectable>();
            return selectable != null && selectable.IsInteractable();
        }

        /// <summary>
        /// Mostra o nasconde linea e reticolo (solo quando lo stato cambia).
        /// </summary>
        /// <param name="value">true per mostrare.</param>
        private void SetShown(bool value)
        {
            if (!value) IsPointingAtUI = false;
            if (shown == value) return;
            shown = value;
            if (line != null) line.enabled = value;
            if (reticle != null) reticle.enabled = value;
        }

        /// <summary>
        /// Imposta mano, tema, linea e reticolo. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UIPointerHand newHand, UITheme newTheme, LineRenderer newLine, Renderer newReticle)
        {
            hand = newHand;
            theme = newTheme;
            line = newLine;
            reticle = newReticle;
        }
    }
}
