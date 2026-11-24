using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_TrainingComplete (Figma "Training · completato"): cerchio con spunta, "Training completato", messaggio, scheda bianca
    /// "cosa hai imparato" (4 voci fisse, chiavi ui.training.learned.*) e BTN_Primary "Torna alla Home". Testi e voci sono costruiti
    /// dal builder: lo script collega solo il pulsante. Il Training non ha punteggio: non si registra nulla.
    /// Uso: istanza di PANEL_TrainingComplete sotto PANEL_Screens (UIScreen id TrainingComplete); la apre MissionFlowDriver a fine Training.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Screens/Training Complete Screen")]
    public class TrainingCompleteScreen : MonoBehaviour
    {
        [Header("Riferimenti (assegnati dal builder)")]
        [Tooltip("BTN_Primary \"Torna alla Home\".")]
        [SerializeField] private UIButton homeButton;

        /// <summary>
        /// Collega il pulsante.
        /// </summary>
        private void Awake()
        {
            if (homeButton != null) homeButton.Clicked += HandleHome;
        }

        /// <summary>
        /// Scollega il pulsante.
        /// </summary>
        private void OnDestroy()
        {
            if (homeButton != null) homeButton.Clicked -= HandleHome;
        }

        /// <summary>
        /// "Torna alla Home": chiude la sessione del Training e mostra Home.
        /// </summary>
        private void HandleHome()
        {
            MissionFlow.ExitToHome();
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UIButton newHomeButton)
        {
            homeButton = newHomeButton;
        }
    }
}
