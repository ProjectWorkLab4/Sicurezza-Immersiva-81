using System.Collections;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Motore del flusso di missione (componente di MNG_UI, aggiunto dal builder): ascolta il tasto di pausa e
    /// <see cref="MissionSession.Ended"/> e applica le regole di <see cref="MissionFlow"/>.
    /// Fine missione: HUD nascosta, <see cref="IOutcomePresenter"/> (HUD_OutcomePanel) per 3-5 s, poi menu davanti alla testa
    /// con PANEL_Debrief (Training: PANEL_TrainingComplete). Pausa: tasto menu sinistro / P (Editor), solo a missione in corso.
    /// Rileva anche l'inizio di una missione (evento MissionSession.Started) per azzerare lo stato e mostrare la HUD.
    /// La barra in basso resta nascosta finché una missione è attiva (Pausa, Debrief, Score &amp; Replay non la hanno).
    /// Uso: un solo componente su MNG_UI (accanto a UIScreenManager); nessuna configurazione richiesta.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Mission Flow Driver")]
    public class MissionFlowDriver : MonoBehaviour
    {
        [Header("Fine missione")]
        [Tooltip("Durata di HUD_OutcomePanel prima del Debrief, in secondi (limitata a 3-5 come da brief).")]
        [Range(MissionFlow.OUTCOME_MIN_SECONDS, MissionFlow.OUTCOME_MAX_SECONDS)]
        [SerializeField] private float outcomeSeconds = 4f;
        [Tooltip("Attesa prima di PANEL_TrainingComplete (il Training non ha esito), in secondi.")]
        [SerializeField] private float trainingEndDelay = 0.5f;

        [Header("Pausa")]
        [Tooltip("Abilita anche il tasto P (solo Editor e Development Build).")]
        [SerializeField] private bool keyboardPause = true;

        // Ingresso della pausa.
        private PauseInput pauseInput;
        // Sequenza di fine missione in corso (null se nessuna).
        private Coroutine endRoutine;
        // Gestore a cui siamo iscritti.
        private UIScreenManager subscribedManager;
        // true dopo il primo avviso di pannello esito mancante.
        private bool warnedNoPresenter;

        /// <summary>
        /// Si iscrive al tasto di pausa e alla fine della missione.
        /// </summary>
        private void OnEnable()
        {
            pauseInput = new PauseInput(keyboardPause);
            pauseInput.Pressed += HandlePausePressed;
            pauseInput.Enable();
            MissionSession.Ended += HandleEnded;
            MissionSession.Started += HandleStarted;
        }

        /// <summary>
        /// Si collega al gestore delle schermate (la barra in basso segue visibilità del menu e schermata).
        /// </summary>
        private void Start()
        {
            Subscribe(UIScreenManager.Instance);
            UpdateBottomBar();
        }

        /// <summary>
        /// Rilascia tasto ed eventi e rimette il tempo a velocità normale.
        /// </summary>
        private void OnDisable()
        {
            if (pauseInput != null)
            {
                pauseInput.Pressed -= HandlePausePressed;
                pauseInput.Dispose();
                pauseInput = null;
            }
            MissionSession.Ended -= HandleEnded;
            MissionSession.Started -= HandleStarted;
            Subscribe(null);
            if (endRoutine != null)
            {
                StopCoroutine(endRoutine);
                endRoutine = null;
            }
            MissionFlow.IsOutcomeShowing = false;
            if (MissionFlow.IsPaused) MissionFlow.ClearPauseState();
        }

        /// <summary>
        /// Inizio missione (evento <see cref="MissionSession.Started"/>): azzera lo stato di pausa e mostra la HUD.
        /// </summary>
        private void HandleStarted()
        {
            MissionFlow.OnMissionStarted();
            UpdateBottomBar();
        }

        /// <summary>
        /// Tasto di pausa premuto: alterna la pausa (ignorato fuori missione o durante l'esito).
        /// </summary>
        private void HandlePausePressed()
        {
            MissionFlow.TogglePause();
        }

        /// <summary>
        /// La missione è finita: avvia la sequenza esito → Debrief (o Training completato).
        /// </summary>
        /// <param name="result">Risultato consegnato dallo scenario.</param>
        private void HandleEnded(MissionResult result)
        {
            if (endRoutine != null) StopCoroutine(endRoutine);
            endRoutine = StartCoroutine(EndSequence(result));
        }

        /// <summary>
        /// Sequenza di fine missione: HUD nascosta → esito 3-5 s (tempo reale) → menu con Debrief / Training completato.
        /// Un risultato null viene trattato come missione interrotta (si torna a Home senza Debrief).
        /// </summary>
        private IEnumerator EndSequence(MissionResult result)
        {
            MissionFlow.ClearPauseState();
            MissionFlow.IsOutcomeShowing = true;
            if (HUDService.IsReady) HUDService.SetVisible(false);

            bool training = MissionSession.IsTraining;
            IOutcomePresenter presenter = null;
            if (result != null && !training)
            {
                presenter = MissionFlow.FindOutcomePresenter();
                if (presenter == null && !warnedNoPresenter)
                {
                    warnedNoPresenter = true;
                    Debug.LogWarning("[SI81 UI] Fine missione: nessun HUD_OutcomePanel (IOutcomePresenter) in scena, l'esito non viene mostrato.", this);
                }
                presenter?.ShowOutcome(result.Outcome);
                yield return new WaitForSecondsRealtime(Mathf.Clamp(outcomeSeconds, MissionFlow.OUTCOME_MIN_SECONDS, MissionFlow.OUTCOME_MAX_SECONDS));
                presenter?.HideOutcome();
            }
            else
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, trainingEndDelay));
            }

            MissionFlow.IsOutcomeShowing = false;
            endRoutine = null;
            if (result == null)
            {
                Debug.LogWarning("[SI81 UI] MissionSession.End(null): missione trattata come interrotta, ritorno a Home.", this);
                MissionFlow.ExitToHome();
                yield break;
            }

            UIScreenManager manager = UIScreenManager.Instance;
            if (manager == null) yield break;
            // Prima la schermata (menu nascosto: nessuna dissolvenza di quella precedente), poi il menu davanti alla testa.
            manager.ShowRoot(training ? UIScreenId.TrainingComplete : UIScreenId.Debrief);
            manager.ShowMenu();
        }

        /// <summary>
        /// Si iscrive (o si disiscrive, con null) agli eventi del gestore delle schermate.
        /// </summary>
        /// <param name="manager">Gestore, o null per disiscriversi.</param>
        private void Subscribe(UIScreenManager manager)
        {
            if (subscribedManager != null)
            {
                subscribedManager.ScreenChanged -= HandleScreenChanged;
                subscribedManager.MenuVisibilityChanged -= HandleMenuVisibilityChanged;
            }
            subscribedManager = manager;
            if (subscribedManager == null) return;
            subscribedManager.ScreenChanged += HandleScreenChanged;
            subscribedManager.MenuVisibilityChanged += HandleMenuVisibilityChanged;
        }

        /// <summary>
        /// Cambio di schermata: aggiorna la barra in basso.
        /// </summary>
        private void HandleScreenChanged(UIScreenId? previous, UIScreenId next)
        {
            UpdateBottomBar();
        }

        /// <summary>
        /// Menu mostrato o nascosto: aggiorna la barra in basso.
        /// </summary>
        private void HandleMenuVisibilityChanged(bool visible)
        {
            UpdateBottomBar();
        }

        /// <summary>
        /// La barra in basso (PANEL_BottomBar) c'è solo fuori dalle missioni: con una sessione attiva (anche in Pausa, Debrief,
        /// Score &amp; Replay e Impostazioni rapide) il suo posto viene disattivato.
        /// </summary>
        private void UpdateBottomBar()
        {
            UIScreenManager manager = UIScreenManager.Instance;
            if (manager == null || manager.BottomBarSlot == null) return;
            bool visible = !MissionSession.IsActive;
            if (manager.BottomBarSlot.gameObject.activeSelf != visible) manager.BottomBarSlot.gameObject.SetActive(visible);
        }
    }
}
