using System;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Stato statico della missione in corso (usato da HUD, Pausa, Debrief e Score &amp; Replay). Lo imposta il lanciatore
    /// (<see cref="IMissionLauncher"/>) con <see cref="Begin"/>; a fine scenario lo scenario chiama <see cref="End"/>.
    /// <see cref="End"/> registra il risultato nei progressi (tranne per il Training): gli scenari NON devono chiamare
    /// IProgressService.RecordResult da soli, o il risultato sarebbe contato due volte.
    /// </summary>
    public static class MissionSession
    {
        /// <summary>Scenario in corso (null se nessuno).</summary>
        public static MissionDefinition Current { get; private set; }
        /// <summary>Livello in corso.</summary>
        public static DifficultyLevel Level { get; private set; } = DifficultyLevel.Guided;
        /// <summary>Ultimo risultato (null finché la missione non finisce o dopo un nuovo Begin).</summary>
        public static MissionResult LastResult { get; private set; }
        /// <summary>true se la sessione in corso è il Training (nessun punteggio).</summary>
        public static bool IsTraining { get; private set; }
        /// <summary>true se una missione è in corso.</summary>
        public static bool IsActive => Current != null;

        /// <summary>Lanciato a fine missione, dopo il salvataggio del risultato; i pannelli di fine (Debrief) si agganciano qui.</summary>
        public static event Action<MissionResult> Ended;
        /// <summary>Lanciato da <see cref="Begin"/> a sessione impostata (HUD e flusso di missione si agganciano qui, senza polling).</summary>
        public static event Action Started;

        /// <summary>
        /// Inizia una sessione.
        /// </summary>
        /// <param name="m">Scenario.</param>
        /// <param name="l">Livello.</param>
        /// <param name="training">true se è il Training.</param>
        public static void Begin(MissionDefinition m, DifficultyLevel l, bool training)
        {
            Current = m;
            Level = l;
            IsTraining = training;
            LastResult = null;
            Started?.Invoke();
        }

        /// <summary>
        /// Chiude la sessione con un risultato: lo normalizza, lo salva nei progressi (non per il Training) e lancia <see cref="Ended"/>.
        /// </summary>
        /// <param name="r">Risultato della missione.</param>
        public static void End(MissionResult r)
        {
            // Una sola chiusura per sessione: una seconda chiamata registrerebbe il risultato due volte.
            if (LastResult != null && r != null)
            {
                Debug.LogWarning("[SI81 UI] MissionSession.End chiamata due volte per la stessa sessione: ignorata.");
                return;
            }
            if (r != null)
            {
                if (string.IsNullOrEmpty(r.MissionId) && Current != null) r.MissionId = Current.Id;
                ScoreRules.Normalize(r);
                if (!IsTraining && UIServices.Progress != null) UIServices.Progress.RecordResult(r);
            }
            LastResult = r;
            Ended?.Invoke(r);
        }

        /// <summary>
        /// Azzera lo stato (si torna al menu). Lasciare <see cref="LastResult"/> fino al ritorno a Home permette a Score &amp; Replay di leggerlo.
        /// </summary>
        public static void Clear()
        {
            Current = null;
            IsTraining = false;
            LastResult = null;
        }
    }
}
