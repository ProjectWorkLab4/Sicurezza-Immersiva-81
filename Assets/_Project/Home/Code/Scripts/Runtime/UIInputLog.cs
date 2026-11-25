using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;

namespace SI81.UI
{
    /// <summary>
    /// Righe di diagnosi dell'input UI nel log del visore (logcat / Meta Quest Developer Hub), tutte con il prefisso
    /// "[SI81 Input]": all'avvio versione e impostazioni del modulo, a ogni controller collegato il suo tipo, e a ogni
    /// pressione del grilletto mano, posa del raggio e cosa colpisce. Serve a capire sul Quest perché un clic non arriva.
    /// Sta su MNG_UI (lo aggiunge UIInputSetup a runtime); costa solo quando si preme il grilletto.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIInputLog : MonoBehaviour
    {
        /// <summary>Versione della UI comune scritta nel log (allineata al CHANGELOG).</summary>
        public const string UI_VERSION = "0.7.5";
        /// <summary>Prefisso di tutte le righe, per filtrare il log.</summary>
        private const string TAG = "[SI81 Input] ";
        /// <summary>Nome del controllo del grilletto sui controller XR.</summary>
        private const string TRIGGER = "triggerPressed";

        // Modulo UI di questo MNG_UI.
        private InputSystemUIInputModule module;
        // Ultimo stato del grilletto per dispositivo (id): una riga per ogni nuova pressione, a prescindere dai frame.
        private readonly System.Collections.Generic.Dictionary<int, bool> lastTrigger = new System.Collections.Generic.Dictionary<int, bool>();

        /// <summary>
        /// Collega il modulo e si iscrive ai cambi di dispositivo.
        /// </summary>
        private void OnEnable()
        {
            module = GetComponentInChildren<InputSystemUIInputModule>(true);
            InputSystem.onDeviceChange += HandleDeviceChange;
        }

        /// <summary>
        /// Si disiscrive dai cambi di dispositivo.
        /// </summary>
        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
        }

        /// <summary>
        /// Scrive versione, impostazioni del modulo e dispositivi già presenti.
        /// </summary>
        private void Start()
        {
            var sb = new StringBuilder(TAG);
            sb.Append("UI comune v").Append(UI_VERSION);
            if (module != null)
            {
                sb.Append(" · modulo: pointerBehavior=").Append(module.pointerBehavior);
                sb.Append(", origine=").Append(module.xrTrackingOrigin != null ? module.xrTrackingOrigin.name : "NESSUNA");
                sb.Append(", posizione=").Append(DescribeBindings(module.trackedDevicePosition));
                sb.Append(", click=").Append(DescribeBindings(module.leftClick));
            }
            else sb.Append(" · ATTENZIONE: nessun InputSystemUIInputModule");
            sb.Append(" · EventSystem=").Append(EventSystem.current != null ? EventSystem.current.name : "NESSUNO");
            Debug.Log(sb.ToString());
            foreach (InputDevice device in InputSystem.devices) LogDevice("presente", device);
        }

        /// <summary>
        /// A ogni pressione del grilletto di un controller XR scrive mano, posa e risultato del raycast del modulo.
        /// </summary>
        private void Update()
        {
            var devices = InputSystem.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                if (!(devices[i] is XRController controller)) continue;
                var trigger = controller.TryGetChildControl<ButtonControl>(TRIGGER);
                if (trigger == null) continue;
                bool pressed = trigger.isPressed;
                lastTrigger.TryGetValue(controller.deviceId, out bool wasPressed);
                lastTrigger[controller.deviceId] = pressed;
                if (!pressed || wasPressed) continue;

                XRHandDevices.ReadAimPose(controller, out Vector3 position, out Quaternion rotation);
                RaycastResult hit = module != null ? module.GetLastRaycastResult(controller.deviceId) : default;
                bool chosen = XRHandDevices.Get(XRHandDevices.HasUsage(controller, CommonUsages.LeftHand)) == controller;
                Debug.Log(TAG + $"grilletto {controller.name} [{string.Join(",", controller.usages)}] tracciato={controller.isTracked.isPressed} " +
                          $"scelto={chosen} posa={position:F2} dir={(rotation * Vector3.forward):F2} " +
                          (hit.isValid && hit.gameObject != null ? $"colpito={hit.gameObject.name} a {hit.distance:F2} m" : "colpito=NIENTE") +
                          $" schermata={(UIScreenManager.Instance != null ? UIScreenManager.Instance.CurrentScreen.ToString() : "-")}");
            }
        }

        /// <summary>
        /// Scrive i dispositivi collegati o scollegati durante la partita.
        /// </summary>
        /// <param name="device">Dispositivo.</param>
        /// <param name="change">Tipo di cambio.</param>
        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed) LogDevice(change == InputDeviceChange.Added ? "collegato" : "scollegato", device);
        }

        /// <summary>
        /// Scrive una riga con nome, layout e usi di un dispositivo (solo dispositivi XR e puntatori).
        /// </summary>
        /// <param name="what">Evento (presente, collegato, scollegato).</param>
        /// <param name="device">Dispositivo.</param>
        private static void LogDevice(string what, InputDevice device)
        {
            if (!(device is TrackedDevice) && !(device is Pointer)) return;
            Debug.Log(TAG + $"dispositivo {what}: {device.name} layout={device.layout} usi=[{string.Join(",", device.usages)}]");
        }

        /// <summary>
        /// Elenca i controlli effettivamente legati a un'azione del modulo (quanti e quali).
        /// </summary>
        /// <param name="reference">Riferimento all'azione.</param>
        /// <returns>Testo breve.</returns>
        private static string DescribeBindings(InputActionReference reference)
        {
            if (reference == null || reference.action == null) return "nessuna azione";
            InputAction action = reference.action;
            var sb = new StringBuilder(action.enabled ? "on " : "OFF ");
            sb.Append(action.controls.Count).Append(" controlli");
            return sb.ToString();
        }
    }
}
