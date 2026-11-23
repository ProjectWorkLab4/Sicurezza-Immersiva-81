using TMPro;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Testo TMP collegato a una chiave di SO_UIStrings_It e a uno stile/colore di SO_UITheme.
    /// Nessun testo va scritto nei prefab: il testo arriva dalla chiave all'avvio (e in Editor quando si cambia un campo).
    /// Per testi dinamici (numeri, nomi scenario) usare <see cref="SetKey"/> o scrivere direttamente nel TMP da codice.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class UIStringText : MonoBehaviour
    {
        [Tooltip("Tabella testi SO_UIStrings_It.")]
        [SerializeField] private UIStrings strings;
        [Tooltip("Chiave del testo, es. ui.nav.home.")]
        [SerializeField] private string key;
        [Tooltip("Tema SO_UITheme (stile e colore). Vuoto = lo stile e il colore attuali del TMP restano.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Stile di testo del tema (vedi TextStyleIds), es. \"Title\".")]
        [SerializeField] private string textStyle = TextStyleIds.BODY;
        [Tooltip("Token colore del tema (vedi ColorTokens), es. \"color.text.on-dark\".")]
        [SerializeField] private string colorToken = ColorTokens.TEXT_ON_DARK;

        /// <summary>Chiave del testo.</summary>
        public string Key => key;

        /// <summary>
        /// Applica testo, stile e colore all'avvio.
        /// </summary>
        private void Awake()
        {
            Apply();
        }

        /// <summary>
        /// Cambia la chiave e aggiorna il testo.
        /// </summary>
        /// <param name="newKey">Nuova chiave di SO_UIStrings_It.</param>
        public void SetKey(string newKey)
        {
            key = newKey;
            Apply();
        }

        /// <summary>
        /// Cambia stile e colore del tema (es. stato attivo: Body Bold + brand.dark) e riapplica. Solo se cambiano.
        /// </summary>
        /// <param name="newStyle">Stile di testo del tema (vedi TextStyleIds).</param>
        /// <param name="newColorToken">Token colore del tema (vedi ColorTokens).</param>
        public void SetStyle(string newStyle, string newColorToken)
        {
            if (textStyle == newStyle && colorToken == newColorToken) return;
            textStyle = newStyle;
            colorToken = newColorToken;
            Apply();
        }

        /// <summary>Stile di testo del tema attualmente applicato.</summary>
        public string TextStyle => textStyle;
        /// <summary>Token colore del tema attualmente applicato.</summary>
        public string ColorToken => colorToken;

        /// <summary>
        /// Scrive nel TMP il testo della chiave e applica stile e colore del tema.
        /// </summary>
        public void Apply()
        {
            TMP_Text text = GetComponent<TMP_Text>();
            if (text == null) return;
            if (theme != null)
            {
                UITheme.TextStyle style = theme.GetTextStyle(textStyle);
                if (style != null) style.ApplyTo(text);
                text.color = theme.GetColor(colorToken);
            }
            if (strings != null && !string.IsNullOrEmpty(key)) text.text = strings.Get(key);
        }

        /// <summary>
        /// Imposta tutti i campi e applica. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UIStrings newStrings, string newKey, UITheme newTheme, string newStyle, string newColorToken)
        {
            strings = newStrings;
            key = newKey;
            theme = newTheme;
            textStyle = newStyle;
            colorToken = newColorToken;
            Apply();
        }
    }
}
