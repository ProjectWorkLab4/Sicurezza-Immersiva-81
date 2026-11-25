using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Chi mostra l'esito della missione (HUD_OutcomePanel, Blocco C · C2). <see cref="MissionFlowDriver"/> lo chiama a fine missione:
    /// <see cref="ShowOutcome"/> all'inizio, <see cref="HideOutcome"/> dopo 3-5 secondi. Il pannello si registra con
    /// <see cref="MissionFlow.RegisterOutcomePresenter"/> (oppure basta che un MonoBehaviour della scena implementi l'interfaccia).
    /// </summary>
    public interface IOutcomePresenter
    {
        /// <summary>Mostra il pannello dell'esito (1,5 m al centro, nessun input).</summary>
        /// <param name="outcome">Esito della missione.</param>
        void ShowOutcome(OutcomeType outcome);

        /// <summary>Nasconde il pannello dell'esito.</summary>
        void HideOutcome();
    }

    /// <summary>
    /// Colla del flusso di missione (Blocco C · C3), stato statico + azioni: pausa (Time.timeScale 0/1, HUD nascosta, PANEL_Pause),
    /// ricomincia, esci alla Home, esito di fine missione e badge nuovi. Il "motore" che ascolta eventi e tasti è
    /// <see cref="MissionFlowDriver"/> su MNG_UI; qui stanno le regole, così le schermate (Pausa, Score &amp; Replay) le usano senza duplicarle.
    /// Regole: la pausa vale solo a missione in corso (<see cref="IsRunning"/>), mai mentre l'esito è mostrato e mai nel menu;
    /// "Esci alla Home" NON registra nulla (non chiama MissionSession.End), quindi nessun punteggio.
    /// </summary>
    public static class MissionFlow
    {
        /// <summary>Durata minima dell'esito, in secondi (brief: 3-5 s).</summary>
        public const float OUTCOME_MIN_SECONDS = 3f;
        /// <summary>Durata massima dell'esito, in secondi.</summary>
        public const float OUTCOME_MAX_SECONDS = 5f;

        // Pannello esito registrato (null = cercarne uno in scena).
        private static IOutcomePresenter outcomePresenter;
        // Id dei badge già sbloccati all'inizio della missione (null = nessuna istantanea).
        private static HashSet<string> badgesAtStart;

        /// <summary>true se il gioco è in pausa (Time.timeScale = 0).</summary>
        public static bool IsPaused { get; private set; }
        /// <summary>true mentre HUD_OutcomePanel è mostrato (pausa e input ignorati).</summary>
        public static bool IsOutcomeShowing { get; internal set; }
        /// <summary>true se una missione è in corso: sessione attiva e nessun risultato ancora consegnato.</summary>
        public static bool IsRunning => MissionSession.IsActive && MissionSession.LastResult == null;
        /// <summary>true se si può mettere in pausa adesso.</summary>
        public static bool CanPause => IsRunning && !IsPaused && !IsOutcomeShowing && UIScreenManager.Instance != null;

        /// <summary>
        /// Chiave della fase corrente (hud.phase.*), mostrata nella pillola "Fase: ..." della Pausa (vuota = nessuna pillola).
        /// La scrive chi imposta la fase: HUDService.SetPhase deve assegnarla qui (vedi NOTES_C3).
        /// </summary>
        public static string CurrentPhaseKey { get; set; }
        /// <summary>
        /// Indice (da 0) dell'obiettivo corrente di MissionDefinition.Objectives: i precedenti sono completati, i successivi da fare.
        /// Lo aggiorna chi gestisce gli obiettivi (HUDService.SetObjective o lo scenario).
        /// </summary>
        public static int ObjectiveIndex { get; set; }

        /// <summary>Lanciato quando si entra (true) o si esce (false) dalla pausa.</summary>
        public static event Action<bool> PausedChanged;

        /// <summary>
        /// Azzera lo stato all'avvio del Play Mode (utile con il "Domain Reload" disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            outcomePresenter = null;
            badgesAtStart = null;
            IsPaused = false;
            IsOutcomeShowing = false;
            CurrentPhaseKey = null;
            ObjectiveIndex = 0;
            PausedChanged = null;
        }

        // =====================================================================
        // Esito
        // =====================================================================

        /// <summary>
        /// Registra il pannello esito (chiamato da HUD_OutcomePanel in OnEnable).
        /// </summary>
        /// <param name="presenter">Pannello che mostra l'esito.</param>
        public static void RegisterOutcomePresenter(IOutcomePresenter presenter)
        {
            outcomePresenter = presenter;
        }

        /// <summary>
        /// Toglie la registrazione del pannello esito (chiamato in OnDisable), solo se è quello registrato.
        /// </summary>
        /// <param name="presenter">Pannello da togliere.</param>
        public static void UnregisterOutcomePresenter(IOutcomePresenter presenter)
        {
            if (outcomePresenter == presenter) outcomePresenter = null;
        }

        /// <summary>
        /// Restituisce il pannello esito: quello registrato oppure, se manca, il primo MonoBehaviour in scena (anche disattivo)
        /// che implementa <see cref="IOutcomePresenter"/>. La ricerca avviene una volta a fine missione, non a ogni frame.
        /// </summary>
        /// <returns>Il pannello, o null se non c'è.</returns>
        public static IOutcomePresenter FindOutcomePresenter()
        {
            if (outcomePresenter != null) return outcomePresenter;
            MonoBehaviour[] all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (MonoBehaviour behaviour in all)
            {
                if (behaviour is IOutcomePresenter found)
                {
                    outcomePresenter = found;
                    return found;
                }
            }
            // HUD_OutcomePanel (Blocco C · C2) non implementa l'interfaccia: lo si adatta qui.
            OutcomePanel panel = UnityEngine.Object.FindFirstObjectByType<OutcomePanel>(FindObjectsInactive.Include);
            if (panel != null) outcomePresenter = new OutcomePanelAdapter(panel);
            return outcomePresenter;
        }

        /// <summary>
        /// Adatta <see cref="OutcomePanel"/> a <see cref="IOutcomePresenter"/>: mostra l'ultimo risultato della sessione.
        /// </summary>
        private sealed class OutcomePanelAdapter : IOutcomePresenter
        {
            // Pannello adattato.
            private readonly OutcomePanel panel;

            /// <summary>Crea l'adattatore.</summary>
            /// <param name="newPanel">Pannello esito.</param>
            public OutcomePanelAdapter(OutcomePanel newPanel)
            {
                panel = newPanel;
            }

            /// <summary>Mostra il pannello esito con l'ultimo risultato della sessione.</summary>
            /// <param name="outcome">Esito della missione.</param>
            public void ShowOutcome(OutcomeType outcome)
            {
                if (panel != null) panel.Show(MissionSession.LastResult, null);
            }

            /// <summary>Nasconde il pannello esito.</summary>
            public void HideOutcome()
            {
                if (panel != null) panel.Hide();
            }
        }

        // =====================================================================
        // Inizio missione
        // =====================================================================

        /// <summary>
        /// Prepara una nuova missione: azzera pausa, fase, obiettivo e fotografa i badge già sbloccati (per i "Nuovi badge" di Score &amp; Replay),
        /// poi rende visibile la HUD (era stata nascosta a fine missione).
        /// </summary>
        public static void OnMissionStarted()
        {
            ClearPauseState();
            IsOutcomeShowing = false;
            CurrentPhaseKey = null;
            ObjectiveIndex = 0;
            CaptureBadgeSnapshot();
            SetHudVisible(true);
        }

        /// <summary>
        /// Memorizza gli id dei badge sbloccati in questo momento.
        /// </summary>
        private static void CaptureBadgeSnapshot()
        {
            badgesAtStart = null;
            if (UIServices.Progress == null) return;
            badgesAtStart = new HashSet<string>();
            foreach (BadgeInfo badge in UIServices.Progress.GetBadges())
                if (badge != null && badge.Unlocked) badgesAtStart.Add(badge.Id);
        }

        /// <summary>
        /// Badge sbloccati dopo l'inizio della missione (vuoto se non c'è l'istantanea).
        /// </summary>
        /// <param name="into">Elenco da riempire (viene svuotato prima).</param>
        public static void GetNewBadges(List<BadgeInfo> into)
        {
            into.Clear();
            if (badgesAtStart == null || UIServices.Progress == null) return;
            foreach (BadgeInfo badge in UIServices.Progress.GetBadges())
                if (badge != null && badge.Unlocked && !badgesAtStart.Contains(badge.Id)) into.Add(badge);
        }

        // =====================================================================
        // Pausa
        // =====================================================================

        /// <summary>
        /// Alterna la pausa (tasto menu sinistro / P in Editor): mette in pausa se possibile, altrimenti riprende.
        /// </summary>
        public static void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        /// <summary>
        /// Mette in pausa: Time.timeScale = 0, HUD nascosta e PANEL_Pause a 1,5 m al centro. Ignorata fuori missione o durante l'esito.
        /// </summary>
        public static void Pause()
        {
            if (!CanPause) return;
            IsPaused = true;
            Time.timeScale = 0f;
            SetHudVisible(false);
            UIScreenManager manager = UIScreenManager.Instance;
            // Prima la schermata (menu ancora nascosto: nessuna dissolvenza di Home), poi il menu davanti alla testa.
            manager.ShowRoot(UIScreenId.Pause);
            manager.ShowMenu();
            PausedChanged?.Invoke(true);
        }

        /// <summary>
        /// Riprende il gioco: Time.timeScale = 1, menu nascosto, HUD di nuovo visibile.
        /// </summary>
        public static void Resume()
        {
            if (!IsPaused) return;
            ClearPauseState();
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.HideMenu();
            SetHudVisible(true);
        }

        /// <summary>
        /// Esce dallo stato di pausa senza toccare menu e HUD (Time.timeScale = 1).
        /// </summary>
        internal static void ClearPauseState()
        {
            bool wasPaused = IsPaused;
            IsPaused = false;
            Time.timeScale = 1f;
            if (wasPaused) PausedChanged?.Invoke(false);
        }

        // =====================================================================
        // Ricomincia / Esci
        // =====================================================================

        /// <summary>
        /// Rilancia lo stesso scenario allo stesso livello (Training compreso) tramite <see cref="UIServices.Launcher"/>.
        /// Usata da "Ricomincia" (Pausa) e "Riprova" (Score &amp; Replay). Nasconde il menu, rimette la HUD e azzera la pausa.
        /// </summary>
        public static void RestartMission()
        {
            MissionDefinition mission = MissionSession.Current;
            if (mission == null || UIServices.Launcher == null)
            {
                Debug.LogWarning("[SI81 UI] Ricomincia: nessuna missione in corso o lanciatore non pronto.");
                return;
            }
            DifficultyLevel level = MissionSession.Level;
            bool training = MissionSession.IsTraining;
            ClearPauseState();
            IsOutcomeShowing = false;
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.HideMenu();
            SetHudVisible(true);
            if (training) UIServices.Launcher.LaunchTraining(mission);
            else UIServices.Launcher.Launch(mission, level);
        }

        /// <summary>
        /// Esce alla Home senza registrare nulla (nessuna chiamata a MissionSession.End): HUD nascosta, pausa chiusa,
        /// poi <see cref="IMissionLauncher.ReturnToHome"/> (scarica lo scenario, mostra il menu, va a Home).
        /// </summary>
        public static void ExitToHome()
        {
            ClearPauseState();
            IsOutcomeShowing = false;
            SetHudVisible(false);
            if (UIServices.Launcher != null)
            {
                UIServices.Launcher.ReturnToHome();
                return;
            }
            // Senza lanciatore: chiude la sessione e mostra Home da soli.
            MissionSession.Clear();
            UIScreenManager manager = UIScreenManager.Instance;
            if (manager == null) return;
            manager.ShowMenu();
            manager.GoHome();
        }

        /// <summary>
        /// Mostra o nasconde tutta la HUD, se il servizio è pronto.
        /// </summary>
        /// <param name="visible">true per mostrarla.</param>
        private static void SetHudVisible(bool visible)
        {
            if (HUDService.IsReady) HUDService.SetVisible(visible);
        }
    }
}
