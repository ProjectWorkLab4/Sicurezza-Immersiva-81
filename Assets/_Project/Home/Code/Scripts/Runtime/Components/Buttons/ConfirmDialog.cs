using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_ConfirmDialog (Figma UI_ConfirmDialog): dialogo di conferma MODALE 480×240 su vetro (surface.glass + riflesso, ombra dialog).
    /// Titolo (Title Bold, on-dark), messaggio (Body on-green, a capo) e due pulsanti allineati a destra:
    /// "Annulla" (BTN_Primary, ICN_Undo, FUOCO PREDEFINITO) e la conferma (BTN_Secondary Danger, ICN_Exit, testo nero).
    /// Usato da Pausa (Esci, Ricomincia) e Impostazioni (Ripristina predefiniti). Parametrico: testi per CHIAVE, nessun testo nel prefab.
    /// Modale: un velo scuro (<c>color.dialog.backdrop</c>) grande come il Canvas menu sta dietro il dialogo e BLOCCA i raggi sul resto
    /// della schermata; il dialogo va in fondo ai fratelli del Canvas (sopra tutto) a ogni <see cref="Show"/>. Nessun blur in tempo reale.
    /// Uso: <c>dialog.Show("ui.dialog.exit_mission.title", "ui.dialog.exit_mission.body", "ui.button.exit", "ui.button.cancel", OnExit, null);</c>
    /// </summary>
    [AddComponentMenu("SI81/UI/Confirm Dialog")]
    public class ConfirmDialog : MonoBehaviour
    {
        [Header("Dati")]
        [Tooltip("Tema comune SO_UITheme.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Dimensione del velo (di solito size.menu-canvas: tutto il Canvas menu).")]
        [SerializeField] private Vector2 backdropSize = new Vector2(840f, 627f);

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private Image backdrop;
        [SerializeField] private Image shadow;
        [SerializeField] private RoundedPanelGraphic glass;
        [SerializeField] private UIStringText title;
        [SerializeField] private UIStringText message;
        [SerializeField] private UIButton cancelButton;
        [SerializeField] private UIButton confirmButton;

        // Azioni dell'apertura corrente.
        private Action onConfirm;
        private Action onCancel;

        /// <summary>true se il dialogo è aperto.</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>
        /// Collega i due pulsanti e applica l'aspetto.
        /// </summary>
        private void Awake()
        {
            if (cancelButton != null) cancelButton.Clicked += HandleCancel;
            if (confirmButton != null) confirmButton.Clicked += HandleConfirm;
            Apply();
        }

        /// <summary>
        /// Rilascia i collegamenti.
        /// </summary>
        private void OnDestroy()
        {
            if (cancelButton != null) cancelButton.Clicked -= HandleCancel;
            if (confirmButton != null) confirmButton.Clicked -= HandleConfirm;
        }

        /// <summary>
        /// Apre il dialogo con i testi dati (chiavi di SO_UIStrings_It) e le azioni di conferma/annulla. Il dialogo si porta sopra
        /// gli altri elementi del Canvas e il focus va su "Annulla".
        /// </summary>
        /// <param name="titleKey">Chiave del titolo, es. ui.dialog.exit_mission.title.</param>
        /// <param name="messageKey">Chiave del messaggio, es. ui.dialog.exit_mission.body.</param>
        /// <param name="confirmKey">Chiave dell'etichetta di conferma, es. ui.button.exit.</param>
        /// <param name="cancelKey">Chiave dell'etichetta di annullamento, es. ui.button.cancel.</param>
        /// <param name="confirmAction">Chiamata alla conferma (dopo la chiusura). Può essere null.</param>
        /// <param name="cancelAction">Chiamata all'annullamento (dopo la chiusura). Può essere null.</param>
        /// <param name="confirmIcon">Icona del pulsante di conferma (null = quella del prefab, ICN_Exit).</param>
        public void Show(string titleKey, string messageKey, string confirmKey, string cancelKey, Action confirmAction, Action cancelAction, Sprite confirmIcon = null)
        {
            onConfirm = confirmAction;
            onCancel = cancelAction;
            if (title != null) title.SetKey(titleKey);
            if (message != null) message.SetKey(messageKey);
            if (confirmButton != null)
            {
                if (confirmIcon != null) confirmButton.SetIcon(confirmIcon);
                confirmButton.SetLabelKey(confirmKey);
            }
            if (cancelButton != null) cancelButton.SetLabelKey(cancelKey);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            LayoutActions();
            // Fuoco predefinito: "Annulla" (l'azione sicura).
            if (cancelButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
        }

        /// <summary>
        /// Chiude il dialogo senza chiamare nessuna azione.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Applica colori, ombra, riflesso vetro, velo e misure dal tema.
        /// </summary>
        public void Apply()
        {
            if (theme == null) return;
            if (glass != null) PanelLayout.ApplyGlass(glass, theme);
            if (shadow != null) PanelLayout.ApplyShadow(shadow, theme, ShadowRoles.DIALOG);
            if (backdrop != null)
            {
                Color color = theme.GetColor(ButtonTokens.DIALOG_BACKDROP);
                if (backdrop.color != color) backdrop.color = color;
                UIComponentUtil.SetCentered(backdrop.rectTransform, Vector2.zero, backdropSize);
            }
            var root = (RectTransform)transform;
            UIComponentUtil.SetSize(root, theme.GetNumber(ButtonTokens.DIALOG_WIDTH), theme.GetNumber(ButtonTokens.DIALOG_HEIGHT));
            LayoutTexts();
            LayoutActions();
        }

        /// <summary>
        /// Titolo e messaggio in alto: padding 24, larghezza di contenuto 432, gap 12 (space.m).
        /// </summary>
        private void LayoutTexts()
        {
            float padding = theme.PaddingPanel;
            float gap = theme.GetNumber(NumberTokens.SPACE_M);
            float contentWidth = theme.GetNumber(ButtonTokens.DIALOG_WIDTH) - padding * 2f;
            float titleHeight = PanelLayout.LineHeight(theme.Title);
            if (title != null)
                PanelLayout.SetRect((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(padding, -padding - titleHeight), new Vector2(padding + contentWidth, -padding));
            if (message != null)
            {
                float top = padding + titleHeight + gap;
                float messageHeight = theme.GetNumber(ButtonTokens.DIALOG_HEIGHT) - top - padding - theme.GetNumber(ButtonTokens.HEIGHT) - gap;
                PanelLayout.SetRect((RectTransform)message.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(padding, -top - messageHeight), new Vector2(padding + contentWidth, -top));
            }
        }

        /// <summary>
        /// Pulsanti in basso a destra, gap 12: conferma all'estremo destro, "Annulla" alla sua sinistra.
        /// </summary>
        private void LayoutActions()
        {
            if (theme == null) return;
            float padding = theme.PaddingPanel;
            float gap = theme.GetNumber(NumberTokens.SPACE_M);
            float x = padding;
            if (confirmButton != null) x = PlaceAction((RectTransform)confirmButton.transform, x, gap);
            if (cancelButton != null) PlaceAction((RectTransform)cancelButton.transform, x, gap);
        }

        /// <summary>
        /// Ancora un pulsante in basso a destra a <paramref name="fromRight"/> dal bordo destro e restituisce il nuovo scarto.
        /// </summary>
        private float PlaceAction(RectTransform rect, float fromRight, float gap)
        {
            float width = rect.rect.width;
            float height = rect.rect.height;
            float bottom = theme.PaddingPanel;
            PanelLayout.SetRect(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-fromRight - width, bottom), new Vector2(-fromRight, bottom + height));
            return fromRight + width + gap;
        }

        /// <summary>
        /// "Annulla" cliccato: chiude e chiama l'azione di annullamento.
        /// </summary>
        private void HandleCancel()
        {
            Action action = onCancel;
            ClearActions();
            Hide();
            action?.Invoke();
        }

        /// <summary>
        /// Conferma cliccata: chiude e chiama l'azione di conferma.
        /// </summary>
        private void HandleConfirm()
        {
            Action action = onConfirm;
            ClearActions();
            Hide();
            action?.Invoke();
        }

        /// <summary>
        /// Dimentica le azioni dell'apertura corrente.
        /// </summary>
        private void ClearActions()
        {
            onConfirm = null;
            onCancel = null;
        }

        /// <summary>
        /// Imposta tema, riferimenti e dimensione del velo. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UITheme newTheme, Vector2 newBackdropSize, Image newBackdrop, Image newShadow, RoundedPanelGraphic newGlass,
            UIStringText newTitle, UIStringText newMessage, UIButton newCancel, UIButton newConfirm)
        {
            theme = newTheme;
            backdropSize = newBackdropSize;
            backdrop = newBackdrop;
            shadow = newShadow;
            glass = newGlass;
            title = newTitle;
            message = newMessage;
            cancelButton = newCancel;
            confirmButton = newConfirm;
            Apply();
        }
    }
}
