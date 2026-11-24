using System;
using UnityEngine.InputSystem;

namespace SI81.UI
{
    /// <summary>
    /// Ingresso della pausa: tasto menu del controller SINISTRO (Quest 3) e tasto P (solo Editor e Development Build).
    /// Piccola azione dell'Input System indipendente da UIInputActions (che serve il menu): i percorsi sono costanti qui sotto,
    /// per cambiare un tasto basta modificare SOLO queste. Nessun XR Interaction Toolkit (CLAUDE.md §4.12).
    /// Uso: <c>var input = new PauseInput(); input.Pressed += ...; input.Enable();</c> e <c>Dispose()</c> a fine vita.
    /// </summary>
    public sealed class PauseInput : IDisposable
    {
        /// <summary>Tasto menu del controller sinistro.</summary>
        public const string LEFT_MENU = "<XRController>{LeftHand}/menuButton";
        /// <summary>Tasto P della tastiera (solo Editor / Development Build).</summary>
        public const string KEYBOARD_PAUSE = "<Keyboard>/p";

        // Azione dell'Input System (pulsante).
        private readonly InputAction action;

        /// <summary>Lanciato quando il tasto di pausa viene premuto.</summary>
        public event Action Pressed;

        /// <summary>
        /// Crea l'azione con i suoi binding.
        /// </summary>
        /// <param name="includeKeyboard">true per aggiungere anche il tasto P (ignorato nelle build finali).</param>
        public PauseInput(bool includeKeyboard = true)
        {
            action = new InputAction("Pause", InputActionType.Button);
            action.AddBinding(LEFT_MENU);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (includeKeyboard) action.AddBinding(KEYBOARD_PAUSE);
#endif
            action.performed += HandlePerformed;
        }

        /// <summary>
        /// Abilita l'ascolto del tasto.
        /// </summary>
        public void Enable()
        {
            action.Enable();
        }

        /// <summary>
        /// Disabilita l'ascolto del tasto.
        /// </summary>
        public void Disable()
        {
            action.Disable();
        }

        /// <summary>
        /// Rilascia l'azione.
        /// </summary>
        public void Dispose()
        {
            action.performed -= HandlePerformed;
            action.Dispose();
        }

        /// <summary>
        /// Il tasto è stato premuto: avvisa chi ascolta.
        /// </summary>
        private void HandlePerformed(InputAction.CallbackContext context)
        {
            Pressed?.Invoke();
        }
    }
}
