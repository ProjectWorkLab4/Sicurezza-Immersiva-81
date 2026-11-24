using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SI81.UI.Sandbox
{
    /// <summary>
    /// SOLO per SCN_UISandbox: cambia schermata con i tasti 1–6 (ordine di <see cref="UIScreenId"/>) e con i pulsanti
    /// di debug nel mondo. Il tasto ← (Esc/Backspace, B/Y) è già gestito da UIScreenManager tramite UIInputSetup.
    /// Non usare nelle scene di scenario né in SCN_Core.
    /// </summary>
    [DisallowMultipleComponent]
    public class UISandboxDebugSwitcher : MonoBehaviour
    {
        /// <summary>Collegamento tra un pulsante di debug e la schermata che apre.</summary>
        [Serializable]
        public class ScreenButton
        {
            /// <summary>Pulsante nel mondo.</summary>
            public Button Button;
            /// <summary>Schermata da mostrare al click.</summary>
            public UIScreenId Screen;
        }

        /// <summary>Tasti numerici: il tasto N apre la schermata con valore N−1.</summary>
        private static readonly Key[] DIGIT_KEYS = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

        /// <summary>Numero di valori di <see cref="UIScreenId"/>, letto UNA volta (Enum.GetValues alloca un array a ogni chiamata).</summary>
        private static readonly int SCREEN_COUNT = Enum.GetValues(typeof(UIScreenId)).Length;

        [Tooltip("Pulsanti che aprono una schermata.")]
        [SerializeField] private List<ScreenButton> screenButtons = new List<ScreenButton>();
        [Tooltip("Pulsante \"indietro\" (UIScreenManager.Back).")]
        [SerializeField] private Button backButton;
        [Tooltip("Pulsante \"radice\" (UIScreenManager.GoHome). Facoltativo.")]
        [SerializeField] private Button homeButton;

        /// <summary>
        /// Collega i pulsanti ai metodi di UIScreenManager (a runtime: nessun listener salvato nella scena).
        /// </summary>
        private void Awake()
        {
            foreach (ScreenButton entry in screenButtons)
            {
                if (entry == null || entry.Button == null) continue;
                UIScreenId id = entry.Screen;
                entry.Button.onClick.AddListener(() => ShowScreen(id));
            }
            if (backButton != null) backButton.onClick.AddListener(Back);
            if (homeButton != null) homeButton.onClick.AddListener(GoHome);
        }

        /// <summary>
        /// Legge i tasti 1–9 dalla tastiera (se presente).
        /// </summary>
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            for (int i = 0; i < DIGIT_KEYS.Length && i < SCREEN_COUNT; i++)
            {
                if (keyboard[DIGIT_KEYS[i]].wasPressedThisFrame && UIScreenManager.Instance != null && UIScreenManager.Instance.GetScreen((UIScreenId)i) != null) ShowScreen((UIScreenId)i);
            }
        }

        /// <summary>
        /// Mostra una schermata tramite UIScreenManager.
        /// </summary>
        /// <param name="id">Schermata da mostrare.</param>
        public void ShowScreen(UIScreenId id)
        {
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.Show(id);
        }

        /// <summary>
        /// Torna alla schermata precedente tramite UIScreenManager.
        /// </summary>
        public void Back()
        {
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.Back();
        }

        /// <summary>
        /// Torna alla radice tramite UIScreenManager.
        /// </summary>
        public void GoHome()
        {
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.GoHome();
        }

        /// <summary>
        /// Imposta i pulsanti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(List<ScreenButton> buttons, Button back, Button home)
        {
            screenButtons = buttons;
            backButton = back;
            homeButton = home;
        }
    }
}
