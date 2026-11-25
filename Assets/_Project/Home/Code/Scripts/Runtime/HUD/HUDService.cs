using System;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Punto di accesso statico alla HUD in scenario (pannello da polso, avvisi, sottotitoli, aptica). Gli scenari e i
    /// pannelli comuni chiamano SOLO questa classe: la realizzazione sta in <see cref="HUDRoot"/> (prefab MNG_HUD nel rig
    /// condiviso di SCN_Core). Prima che HUDRoot esista, obiettivo, fase e suggerimento vengono ricordati e applicati
    /// appena compare; gli avvisi arrivati prima vengono scartati. Testi di scenario (obiettivi, avvisi, sottotitoli) =
    /// dati della missione, non chiavi di SO_UIStrings_It; solo la fase usa una chiave (hud.phase.*).
    /// </summary>
    public static class HUDService
    {
        // Realizzazione corrente (null se MNG_HUD non è in scena).
        private static HUDRoot root;
        // Stato ricordato finché HUDRoot non esiste.
        private static string pendingObjective;
        private static string pendingPhase;
        private static string pendingHint;
        private static LevelConfig pendingLevel;
        private static bool warnedNotReady;

        /// <summary>true quando MNG_HUD è in scena e registrato.</summary>
        public static bool IsReady => root != null;
        /// <summary>Configurazione del livello applicata (null se nessuna).</summary>
        public static LevelConfig CurrentLevel { get; private set; }
        /// <summary>Secondi rimasti del conto alla rovescia (0 se il timer è a tempo trascorso o fermo).</summary>
        public static float RemainingSeconds => root != null ? root.RemainingSeconds : 0f;
        /// <summary>Secondi trascorsi dall'avvio del timer.</summary>
        public static float ElapsedSeconds => root != null ? root.ElapsedSeconds : 0f;
        /// <summary>Libreria delle icone ICN_ per nome (null finché la HUD non è pronta).</summary>
        public static HUDIconLibrary Icons => root != null ? root.Icons : null;

        /// <summary>Lanciato quando HUDRoot si registra.</summary>
        public static event Action Ready;
        /// <summary>Lanciato quando il conto alla rovescia arriva a zero.</summary>
        public static event Action TimerExpired;
        /// <summary>Lanciato quando cambia un'impostazione (id; null = qualsiasi). Inoltrato da HUDRoot per pannello polso e sottotitoli.</summary>
        public static event Action<string> SettingsChanged;

        // ---------------------------------------------------------------- registrazione

        /// <summary>
        /// Registra la realizzazione e applica lo stato ricordato. Chiamato da HUDRoot.
        /// </summary>
        /// <param name="newRoot">HUDRoot del prefab MNG_HUD.</param>
        internal static void Register(HUDRoot newRoot)
        {
            root = newRoot;
            if (pendingLevel != null) root.ApplyLevel(pendingLevel);
            if (pendingObjective != null) root.SetObjective(pendingObjective);
            if (pendingPhase != null) root.SetPhase(pendingPhase);
            if (pendingHint != null) root.SetHint(pendingHint);
            Ready?.Invoke();
        }

        /// <summary>
        /// Toglie la realizzazione (HUDRoot distrutto).
        /// </summary>
        /// <param name="oldRoot">HUDRoot che si deregistra.</param>
        internal static void Unregister(HUDRoot oldRoot)
        {
            if (root == oldRoot) root = null;
        }

        /// <summary>
        /// Memorizza la configurazione di livello applicata (chiamato da HUDRoot).
        /// </summary>
        /// <param name="cfg">Configurazione.</param>
        internal static void SetCurrentLevelInternal(LevelConfig cfg)
        {
            CurrentLevel = cfg;
        }

        /// <summary>
        /// Lancia <see cref="TimerExpired"/> (chiamato da HUDRoot).
        /// </summary>
        internal static void RaiseTimerExpired()
        {
            TimerExpired?.Invoke();
        }

        /// <summary>
        /// Lancia <see cref="SettingsChanged"/> (chiamato da HUDRoot).
        /// </summary>
        /// <param name="id">Id dell'impostazione cambiata.</param>
        internal static void RaiseSettingsChanged(string id)
        {
            SettingsChanged?.Invoke(id);
        }

        /// <summary>
        /// Azzera lo stato statico all'avvio del Play Mode (Domain Reload disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            root = null;
            CurrentLevel = null;
            pendingObjective = pendingPhase = pendingHint = null;
            pendingLevel = null;
            warnedNotReady = false;
            Ready = null;
            TimerExpired = null;
            SettingsChanged = null;
        }

        // ---------------------------------------------------------------- pannello da polso

        /// <summary>
        /// Imposta l'obiettivo mostrato sul polso (max ~30 caratteri per riga). Impulso aptico se cambia.
        /// </summary>
        /// <param name="text">Testo dell'obiettivo (dati della missione).</param>
        public static void SetObjective(string text)
        {
            pendingObjective = text;
            if (root != null) root.SetObjective(text);
        }

        /// <summary>
        /// Imposta l'obiettivo corrente dall'indice in MissionDefinition.Objectives (con contatore "n/N").
        /// </summary>
        /// <param name="index">Indice 0..N-1 dell'obiettivo corrente.</param>
        public static void SetObjectiveIndex(int index)
        {
            MissionFlow.ObjectiveIndex = index;
            if (root != null) root.SetObjectiveIndex(index);
        }

        /// <summary>
        /// Imposta il contatore degli obiettivi "corrente/totale" (0 = nascosto).
        /// </summary>
        /// <param name="current">Obiettivo corrente (da 1).</param>
        /// <param name="total">Numero totale di obiettivi.</param>
        public static void SetObjectiveProgress(int current, int total)
        {
            if (root != null) root.SetObjectiveProgress(current, total);
        }

        /// <summary>
        /// Imposta la fase della missione.
        /// </summary>
        /// <param name="phaseKey">Chiave hud.phase.* (scan, decision, action, resolution).</param>
        public static void SetPhase(string phaseKey)
        {
            pendingPhase = phaseKey;
            MissionFlow.CurrentPhaseKey = phaseKey;
            if (root != null) root.SetPhase(phaseKey);
        }

        /// <summary>
        /// Imposta il suggerimento (solo livello Guided: se LevelConfig.ShowHints è falso non viene mostrato). Vuoto = nasconde.
        /// </summary>
        /// <param name="text">Testo del suggerimento (max ~60 caratteri).</param>
        public static void SetHint(string text)
        {
            pendingHint = text;
            if (root != null) root.SetHint(text);
        }

        /// <summary>
        /// Avvia il timer: con secondi &gt; 0 è un conto alla rovescia (avvisi a 60 s e 30 s, poi <see cref="TimerExpired"/>);
        /// con 0 mostra il tempo trascorso (nessun avviso).
        /// </summary>
        /// <param name="seconds">Secondi del conto alla rovescia, o 0 per il tempo trascorso.</param>
        public static void StartTimer(int seconds)
        {
            if (root != null) root.StartTimer(seconds);
        }

        /// <summary>
        /// Ferma il timer (il valore resta mostrato).
        /// </summary>
        public static void StopTimer()
        {
            if (root != null) root.StopTimer();
        }

        // ---------------------------------------------------------------- avvisi e sottotitoli

        /// <summary>
        /// Mostra un avviso evento (priorità alta, 3 s, suono + aptica). Testo: prima riga = titolo, eventuale seconda riga
        /// (separata da "\n") = dettaglio. La forma dipende dal livello (Completo / solo titolo / solo icona).
        /// </summary>
        /// <param name="text">Testo dell'avviso (dati della missione).</param>
        /// <param name="iconName">Nome icona ICN_ (es. "ICN_Warning_Filled"); vuoto = predefinita.</param>
        public static void ShowEventAlert(string text, string iconName = null)
        {
            if (!CheckReady("ShowEventAlert")) return;
            root.ShowEventAlert(text, iconName);
        }

        /// <summary>
        /// Mostra un feedback d'azione (priorità bassa, 3 s). Regole di livello: Guided ogni azione, Operational solo
        /// errori (Negative), Crisis mai.
        /// </summary>
        /// <param name="text">Testo del feedback (max ~60 caratteri, dati della missione).</param>
        /// <param name="kind">Tipo di feedback.</param>
        public static void ShowActionFeedback(string text, FeedbackKind kind)
        {
            if (!CheckReady("ShowActionFeedback")) return;
            root.ShowActionFeedback(text, kind);
        }

        /// <summary>
        /// Mostra un sottotitolo (max 32 caratteri x 2 righe: testi lunghi vengono divisi in pagine in sequenza).
        /// </summary>
        /// <param name="speaker">Nome di chi parla (vuoto = nessuno).</param>
        /// <param name="text">Testo del sottotitolo.</param>
        /// <param name="seconds">Durata totale in secondi; 0 = automatica dalla lunghezza.</param>
        public static void ShowSubtitle(string speaker, string text, float seconds = 0f)
        {
            if (!CheckReady("ShowSubtitle")) return;
            root.ShowSubtitle(speaker, text, seconds, false, false);
        }

        /// <summary>
        /// Mostra un sottotitolo di evento sonoro "[testo]" (es. allarme antincendio). Se critico viene mostrato anche con i sottotitoli disattivati.
        /// </summary>
        /// <param name="soundEvent">Descrizione del suono, senza parentesi.</param>
        /// <param name="critical">true per un allarme critico (sempre sottotitolato).</param>
        public static void ShowSoundEvent(string soundEvent, bool critical = true)
        {
            if (!CheckReady("ShowSoundEvent")) return;
            root.ShowSubtitle(string.Empty, soundEvent, 0f, true, critical);
        }

        /// <summary>
        /// Toglie subito il sottotitolo e svuota la coda.
        /// </summary>
        public static void ClearSubtitle()
        {
            if (root != null) root.ClearSubtitle();
        }

        // ---------------------------------------------------------------- aptica, livello, visibilità

        /// <summary>
        /// Impulso aptico su un controller.
        /// </summary>
        /// <param name="amplitude">Ampiezza 0..1.</param>
        /// <param name="duration">Durata in secondi.</param>
        /// <param name="nonDominantHand">true = mano non dominante (quella del pannello polso); false = mano dominante.</param>
        public static void Pulse(float amplitude, float duration, bool nonDominantHand = true)
        {
            HUDHand nonDominant = GetNonDominantHand();
            HUDHand hand = nonDominantHand ? nonDominant : (nonDominant == HUDHand.Left ? HUDHand.Right : HUDHand.Left);
            if (HapticsService.Instance != null) HapticsService.Instance.Pulse(hand, amplitude, duration);
        }

        /// <summary>
        /// Applica le regole di visibilità di un livello (SO_LevelConfig_*): suggerimenti, obiettivi sul polso, forma degli avvisi, ecc.
        /// </summary>
        /// <param name="cfg">Configurazione del livello.</param>
        public static void ApplyLevel(LevelConfig cfg)
        {
            CurrentLevel = cfg;
            pendingLevel = cfg;
            if (root != null) root.ApplyLevel(cfg);
        }

        /// <summary>
        /// Mostra o nasconde tutta la HUD (es. in pausa o durante l'esito). Avvisi e sottotitoli in corso vengono tolti.
        /// </summary>
        /// <param name="visible">true per mostrare.</param>
        public static void SetVisible(bool visible)
        {
            if (root != null) root.SetVisible(visible);
            if (WorldLabelManager.Instance != null) WorldLabelManager.Instance.SetVisible(visible);
        }

        /// <summary>
        /// Rilegge MissionSession (scenario, livello, obiettivi, tempo) e riparte da capo: da chiamare dopo un riavvio
        /// ("Ricomincia") se MissionSession.Begin non è stato richiamato.
        /// </summary>
        public static void RefreshFromSession()
        {
            if (root != null) root.BeginFromSession();
        }

        /// <summary>
        /// Mano NON dominante secondo l'impostazione controls.dominant_hand (0 = destra dominante, quindi polso a sinistra).
        /// </summary>
        /// <returns>La mano del pannello da polso.</returns>
        public static HUDHand GetNonDominantHand()
        {
            int dominant = UIServices.Settings != null ? UIServices.Settings.GetInt(SettingsCatalog.DOMINANT_HAND) : SettingsCatalog.HAND_RIGHT;
            return dominant == SettingsCatalog.HAND_LEFT ? HUDHand.Right : HUDHand.Left;
        }

        /// <summary>
        /// Controlla che la HUD sia pronta; altrimenti scrive UN avviso e scarta la chiamata.
        /// </summary>
        /// <param name="who">Nome della chiamata.</param>
        /// <returns>true se pronta.</returns>
        private static bool CheckReady(string who)
        {
            if (root != null) return true;
            if (!warnedNotReady)
            {
                warnedNotReady = true;
                Debug.LogWarning($"[MNG_HUD] HUDService.{who}: la HUD non è in scena (manca il prefab MNG_HUD nel rig condiviso). Chiamata ignorata.");
            }
            return false;
        }
    }
}
