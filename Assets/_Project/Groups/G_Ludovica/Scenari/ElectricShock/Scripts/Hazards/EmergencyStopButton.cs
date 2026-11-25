using System;
using System.Collections;
using SI81.Ludovica.TempCore;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// PB-01: arresto di emergenza del generatore. Nel modello MESH_Folgorazione_Generatore è la "Chiave (Interagibile)":
    /// col grilletto la chiave gira di 90° (come la sua animazione) e il generatore si ferma.
    /// Senza chiave (segnaposto) la parte <see cref="cap"/> si abbassa come un pulsante.
    /// </summary>
    public class EmergencyStopButton : TempInteractable
    {
        [Tooltip("Parte che si muove: la chiave del generatore o il fungo del pulsante segnaposto.")]
        [SerializeField] private Transform cap;
        [Tooltip("true = la parte è una chiave che gira fino alla rotazione locale di arrivo; false = pulsante che si abbassa.")]
        [SerializeField] private bool turnKey;
        [Tooltip("Rotazione locale finale della chiave (dall'animazione del modello: identità).")]
        [SerializeField] private Vector3 keyTurnedEuler = Vector3.zero;
        [Tooltip("Durata della rotazione della chiave (s), come l'animazione del modello.")]
        [SerializeField] private float turnSeconds = 0.25f;

        private bool pressed;

        /// <summary>Chiamato alla pressione (una sola volta).</summary>
        public event Action Pressed;
        /// <summary>true dopo la pressione.</summary>
        public bool IsPressed => pressed;

        /// <summary>Grilletto sulla chiave o sul pulsante: arresto di emergenza.</summary>
        /// <param name="hand">Mano.</param>
        public override void OnUseStart(TempHand hand) => Press();

        /// <summary>Gira la chiave o preme il pulsante (anche da script o test).</summary>
        public void Press()
        {
            if (pressed) return;
            pressed = true;
            if (cap != null)
            {
                if (turnKey && isActiveAndEnabled) StartCoroutine(TurnKey());
                else if (turnKey) cap.localRotation = Quaternion.Euler(keyTurnedEuler);
                else cap.localPosition += Vector3.down * 0.02f;
            }
            ElectricShockHud.Pulse(0.7f, 0.15f);
            Pressed?.Invoke();
        }

        /// <summary>Rotazione della chiave dalla posa di riposo a quella di arresto.</summary>
        private IEnumerator TurnKey()
        {
            Quaternion from = cap.localRotation;
            Quaternion to = Quaternion.Euler(keyTurnedEuler);
            for (float t = 0f; t < turnSeconds; t += Time.deltaTime)
            {
                cap.localRotation = Quaternion.Slerp(from, to, t / turnSeconds);
                yield return null;
            }
            cap.localRotation = to;
        }
    }
}
