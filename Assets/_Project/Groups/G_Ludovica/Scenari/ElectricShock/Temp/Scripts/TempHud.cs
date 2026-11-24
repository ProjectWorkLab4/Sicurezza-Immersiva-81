using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SI81.Ludovica.TempCore
{
    /// <summary>
    /// [TEMP] Sostituto di HUDService finché la UI comune non è nel progetto: scrive tutto in Console
    /// (sul visore si legge con adb logcat) e gestisce la vibrazione dei controller.
    /// </summary>
    public static class TempHud
    {
        /// <summary>Prefisso di tutte le righe in Console, per filtrarle in logcat.</summary>
        public const string TAG = "[SI81 TempHUD]";

        /// <summary>Scrive una riga in Console con il tag comune.</summary>
        /// <param name="kind">Tipo di messaggio (Obiettivo, Avviso, Sottotitolo...).</param>
        /// <param name="text">Testo.</param>
        public static void Log(string kind, string text)
        {
            Debug.Log($"{TAG} {kind}: {text}");
        }

        /// <summary>Vibrazione dei controller (entrambi o solo il non dominante, il sinistro).</summary>
        /// <param name="amplitude">Intensità 0-1.</param>
        /// <param name="duration">Durata in secondi.</param>
        /// <param name="nonDominantOnly">true = solo controller sinistro.</param>
        public static void Pulse(float amplitude, float duration, bool nonDominantOnly = false)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
            foreach (var device in devices)
            {
                bool isLeft = (device.characteristics & InputDeviceCharacteristics.Left) != 0;
                if (nonDominantOnly && !isLeft) continue;
                if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
            }
        }
    }
}
