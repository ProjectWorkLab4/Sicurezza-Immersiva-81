using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;

namespace SI81.UI
{
    /// <summary>
    /// Sceglie il controller XR "vero" di una mano. Su Quest con OpenXR possono esistere più dispositivi per la stessa mano
    /// (profili controller diversi): vince quello tracciato aggiornato più di recente; a parità, quello con la vibrazione.
    /// Il risultato resta in cache per il frame corrente, così raggio (UIPointer) e vibrazione (HapticsService) usano
    /// sempre lo stesso dispositivo senza ripetere la ricerca.
    /// </summary>
    public static class XRHandDevices
    {
        /// <summary>Nome del controllo della posizione "aim" sui controller OpenXR (stesso dei binding di InputBindings).</summary>
        public const string POINTER_POSITION = "pointerPosition";
        /// <summary>Nome del controllo della rotazione "aim" sui controller OpenXR.</summary>
        public const string POINTER_ROTATION = "pointerRotation";

        // Cache per frame: [0] sinistra, [1] destra.
        private static readonly XRController[] cached = new XRController[2];
        private static readonly int[] cachedFrame = { -1, -1 };

        /// <summary>
        /// Restituisce il controller della mano indicata (null se non ce n'è nessuno).
        /// </summary>
        /// <param name="left">true per la mano sinistra.</param>
        /// <returns>Il controller scelto per questo frame.</returns>
        public static XRController Get(bool left)
        {
            int slot = left ? 0 : 1;
            if (cachedFrame[slot] == Time.frameCount && cached[slot] != null && cached[slot].added) return cached[slot];

            InternedString usage = left ? CommonUsages.LeftHand : CommonUsages.RightHand;
            XRController best = null;
            double bestScore = double.MinValue;
            ReadOnlyArray<InputDevice> devices = InputSystem.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                if (!(devices[i] is XRController controller) || !controller.added || !HasUsage(controller, usage)) continue;
                // Tracciato prima di non tracciato, poi il più recente; piccolo vantaggio a chi ha la vibrazione.
                double score = controller.lastUpdateTime;
                if (!controller.isTracked.isPressed) score -= 100000.0;
                if (controller is XRControllerWithRumble) score += 0.001;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = controller;
                }
            }
            cached[slot] = best;
            cachedFrame[slot] = Time.frameCount;
            return best;
        }

        /// <summary>
        /// Legge la posa "aim" del controller (spazio di tracciamento); se il dispositivo non la ha usa la posa "grip".
        /// </summary>
        /// <param name="device">Controller.</param>
        /// <param name="position">Posizione locale.</param>
        /// <param name="rotation">Rotazione locale.</param>
        public static void ReadAimPose(XRController device, out Vector3 position, out Quaternion rotation)
        {
            if (!aimControls.TryGetValue(device.deviceId, out AimControls aim) || aim.Device != device)
            {
                // Cercati per nome una sola volta per dispositivo.
                aim = new AimControls
                {
                    Device = device,
                    Position = device.TryGetChildControl<Vector3Control>(POINTER_POSITION),
                    Rotation = device.TryGetChildControl<QuaternionControl>(POINTER_ROTATION)
                };
                aimControls[device.deviceId] = aim;
            }
            position = aim.Position != null ? aim.Position.ReadValue() : device.devicePosition.ReadValue();
            rotation = aim.Rotation != null ? aim.Rotation.ReadValue() : device.deviceRotation.ReadValue();
        }

        /// <summary>Controlli della posa "aim" di un dispositivo (null se mancano).</summary>
        private struct AimControls
        {
            /// <summary>Dispositivo a cui appartengono.</summary>
            public XRController Device;
            /// <summary>Posizione "aim".</summary>
            public Vector3Control Position;
            /// <summary>Rotazione "aim".</summary>
            public QuaternionControl Rotation;
        }

        // Controlli "aim" in cache per id del dispositivo.
        private static readonly System.Collections.Generic.Dictionary<int, AimControls> aimControls = new System.Collections.Generic.Dictionary<int, AimControls>();

        /// <summary>
        /// Indica se il dispositivo ha l'uso (mano) indicato.
        /// </summary>
        /// <param name="device">Dispositivo.</param>
        /// <param name="usage">Uso cercato.</param>
        /// <returns>true se lo ha.</returns>
        internal static bool HasUsage(InputDevice device, InternedString usage)
        {
            ReadOnlyArray<InternedString> usages = device.usages;
            for (int i = 0; i < usages.Count; i++) if (usages[i] == usage) return true;
            return false;
        }
    }
}
