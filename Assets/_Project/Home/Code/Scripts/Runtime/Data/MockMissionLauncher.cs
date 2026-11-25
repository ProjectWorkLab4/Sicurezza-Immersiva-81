using System.Collections;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Lanciatore finto per la sandbox: non carica scene. Scrive nel log, nasconde il menu e, se richiesto, dopo N secondi
    /// fabbrica un risultato plausibile e chiama <see cref="MissionSession.End"/> (così si provano Debrief e Score &amp; Replay
    /// senza uno scenario). ReturnToHome mostra di nuovo il menu e va a Home.
    /// </summary>
    public sealed class MockMissionLauncher : IMissionLauncher
    {
        // MonoBehaviour su cui girano le attese (UIServicesBootstrap).
        private readonly MonoBehaviour runner;
        // Secondi prima del risultato finto (<= 0 = nessun risultato automatico).
        private readonly float resultDelaySeconds;
        // Attesa in corso.
        private Coroutine pending;
        // Contatore per variare i risultati finti.
        private int counter;

        /// <summary>
        /// Crea il lanciatore finto.
        /// </summary>
        /// <param name="newRunner">MonoBehaviour per le attese (può essere null: nessun risultato automatico).</param>
        /// <param name="newResultDelaySeconds">Secondi prima del risultato finto (0 = non fabbricarlo).</param>
        public MockMissionLauncher(MonoBehaviour newRunner, float newResultDelaySeconds = 0f)
        {
            runner = newRunner;
            resultDelaySeconds = newResultDelaySeconds;
        }

        /// <inheritdoc/>
        public bool CanLaunch(MissionDefinition m)
        {
            // Finto: non carica scene, quindi ogni scenario con dati si può "avviare" (sandbox).
            return m != null;
        }

        /// <inheritdoc/>
        public void Launch(MissionDefinition m, DifficultyLevel level)
        {
            if (m == null) return;
            Debug.Log($"[SI81 UI] (mock) Avvio scenario {m.GetFullTitle()} · livello {(int)level} ({level}) · scena {m.SceneName}.");
            Begin(m, level, false);
        }

        /// <inheritdoc/>
        public void LaunchTraining(MissionDefinition training)
        {
            if (training == null) return;
            Debug.Log("[SI81 UI] (mock) Avvio Training.");
            Begin(training, DifficultyLevel.Guided, true);
        }

        /// <inheritdoc/>
        public void ReturnToHome()
        {
            Debug.Log("[SI81 UI] (mock) Ritorno a Home.");
            StopPending();
            Time.timeScale = 1f;
            MissionSession.Clear();
            UIScreenManager manager = UIScreenManager.Instance;
            if (manager == null) return;
            manager.ShowMenu();
            manager.GoHome();
        }

        /// <summary>
        /// Fabbrica subito un risultato per la missione in corso (per i pulsanti di debug della sandbox).
        /// </summary>
        /// <param name="outcome">Esito da simulare.</param>
        public void FabricateResult(OutcomeType outcome)
        {
            if (!MissionSession.IsActive) return;
            MissionSession.End(BuildResult(MissionSession.Current, MissionSession.Level, outcome));
        }

        /// <summary>
        /// Inizia la sessione, nasconde il menu e prepara il risultato automatico.
        /// </summary>
        private void Begin(MissionDefinition m, DifficultyLevel level, bool training)
        {
            StopPending();
            // Ricomincia / Riprova / Cambia livello funzionano anche senza scene: si riparte da tempo normale e sessione nuova.
            Time.timeScale = 1f;
            MissionSession.Begin(m, level, training);
            if (UIScreenManager.Instance != null) UIScreenManager.Instance.HideMenu();
            if (runner != null && resultDelaySeconds > 0f) pending = runner.StartCoroutine(FabricateAfterDelay());
        }

        /// <summary>
        /// Attende il tempo stabilito (non scalato) e poi chiude la sessione con un esito che ruota tra positivo, parziale, negativo.
        /// </summary>
        private IEnumerator FabricateAfterDelay()
        {
            yield return new WaitForSecondsRealtime(resultDelaySeconds);
            pending = null;
            var outcome = (OutcomeType)(counter % 3);
            counter++;
            FabricateResult(outcome);
        }

        /// <summary>
        /// Ferma l'attesa in corso, se c'è.
        /// </summary>
        private void StopPending()
        {
            if (pending != null && runner != null) runner.StopCoroutine(pending);
            pending = null;
        }

        /// <summary>
        /// Crea un risultato plausibile per esito e livello (valori di prova).
        /// </summary>
        private static MissionResult BuildResult(MissionDefinition m, DifficultyLevel level, OutcomeType outcome)
        {
            int baseScore = outcome == OutcomeType.Positive ? 82 : outcome == OutcomeType.Partial ? 66 : 38;
            baseScore -= ((int)level - 1) * 2;
            var dims = new[] { baseScore + 4, baseScore - 3, baseScore + 8, baseScore - 6, baseScore - 1 };
            var result = new MissionResult
            {
                MissionId = m.Id,
                Level = level,
                Outcome = outcome,
                Dimensions = dims,
                SafetyScore = ScoreRules.ScoreFromDimensions(dims),
                DurationSeconds = 240f + (int)level * 30f,
                TimestampUtc = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            result.DebriefLines.Add(DebriefLinePrefix.GOOD + "Hai dato l'allarme subito → i colleghi sono usciti in tempo");
            if (outcome != OutcomeType.Positive)
                result.DebriefLines.Add(DebriefLinePrefix.BAD + "Hai avvisato il coordinatore in ritardo → squadra arrivata tardi");
            if (outcome == OutcomeType.Negative)
                result.DebriefLines.Add(DebriefLinePrefix.CRITICAL + "Ti sei avvicinato al fumo senza abbassarti → esposizione al fumo");
            return result;
        }
    }
}
