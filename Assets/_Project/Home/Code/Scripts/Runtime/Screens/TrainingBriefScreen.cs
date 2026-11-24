using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// PANEL_TrainingBrief (Figma Training · brief): intestazione con BTN_Back, scheda "Cosa imparerai" (5 voci),
    /// panoramica dei comandi e BTN_Primary "Avvia training". Testi e voci sono fissi (chiavi ui.training.*), costruiti dal builder:
    /// lo script collega solo il pulsante. Il clic chiama <c>UIServices.Launcher.LaunchTraining(UIServices.Catalog.Training)</c>.
    /// Se la scena del training non è nella build il pulsante è disattivato con il motivo "Non disponibile" (OnEnable).
    /// Uso: istanza di PANEL_TrainingBrief sotto PANEL_Screens (UIScreen con id TrainingBrief), raggiunta da PANEL_TrainingCard o da Impostazioni.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Training Brief Screen")]
    public class TrainingBriefScreen : MonoBehaviour
    {
        [Header("Riferimenti (assegnati dal builder)")]
        [Tooltip("BTN_Primary \"Avvia training\".")]
        [SerializeField] private UIButton startButton;

        /// <summary>
        /// Collega il clic del pulsante.
        /// </summary>
        private void Awake()
        {
            if (startButton != null) startButton.Clicked += HandleStart;
        }

        /// <summary>
        /// A ogni apertura: "Avvia training" è attivo solo se la scena del training esiste nella build;
        /// altrimenti è disattivato con il motivo "Non disponibile" (niente pulsante che non fa nulla).
        /// </summary>
        private void OnEnable()
        {
            if (startButton == null) return;
            MissionDefinition training = UIServices.Catalog != null ? UIServices.Catalog.Training : null;
            bool canStart = UIServices.Launcher != null && UIServices.Launcher.CanLaunch(training);
            startButton.SetDisabledLabelKey(UNAVAILABLE_KEY);
            startButton.Interactable = canStart;
        }

        /// <summary>Chiave del motivo mostrato sul pulsante quando il training non si può avviare.</summary>
        private const string UNAVAILABLE_KEY = "ui.home.unavailable";

        /// <summary>
        /// Scollega il clic.
        /// </summary>
        private void OnDestroy()
        {
            if (startButton != null) startButton.Clicked -= HandleStart;
        }

        /// <summary>
        /// "Avvia training": lancia il training comune tramite il lanciatore di missioni.
        /// </summary>
        private void HandleStart()
        {
            if (!UIServices.IsReady || UIServices.Launcher == null || UIServices.Catalog == null) return;
            MissionDefinition training = UIServices.Catalog.Training;
            if (training == null)
            {
                Debug.LogWarning("[SI81 UI] Avvia training: SO_MissionCatalog non ha il training (campo Training vuoto).", this);
                return;
            }
            UIServices.Launcher.LaunchTraining(training);
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UIButton newStartButton)
        {
            startButton = newStartButton;
        }
    }
}
