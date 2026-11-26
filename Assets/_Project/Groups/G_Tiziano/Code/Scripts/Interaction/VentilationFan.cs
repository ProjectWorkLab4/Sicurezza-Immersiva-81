using System;
using UnityEngine;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Rotates the fan blades while the ventilation terminal is on.
    /// Code-driven visual: no Animator, animation clip or tween.
    /// </summary>
    public sealed class VentilationFan : MonoBehaviour
    {
        [SerializeField] private VentTerminal terminal;
        [SerializeField] private Transform blades;
        [SerializeField, Min(0f)] private float degreesPerSecond = 180f;

        private Quaternion initialBladeRotation;
        private bool spinning;

        /// <summary>Gets whether the blades are currently spinning.</summary>
        public bool IsSpinning => spinning;

        private void Awake()
        {
            if (terminal == null)
                terminal = FindFirstObjectByType<VentTerminal>();
            if (blades == null && transform.childCount > 0)
                blades = transform.GetChild(0);
            if (blades != null)
                initialBladeRotation = blades.localRotation;
        }

        private void Update()
        {
            bool shouldSpin = terminal != null && terminal.IsOn && blades != null;
            if (!shouldSpin)
            {
                if (spinning)
                {
                    blades.localRotation = initialBladeRotation;
                    spinning = false;
                    UnityEngine.Debug.Log($"[CA] Ventilation fan stopped: {name}");
                }
                return;
            }

            if (!spinning)
            {
                spinning = true;
                UnityEngine.Debug.Log($"[CA] Ventilation fan started: {name}");
            }

            blades.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.Self);
        }

        /// <summary>Prints the current fan state through the supplied logger.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger == null)
                return;

            logger($"[CA] Ventilation fan: {name}; spinning={spinning}; " +
                   $"terminalOn={(terminal != null && terminal.IsOn)}; speed={degreesPerSecond:0} deg/s.");
        }
    }
}
