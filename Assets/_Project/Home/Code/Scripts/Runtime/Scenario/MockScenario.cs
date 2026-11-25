using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Scenario finto per provare tutto il percorso senza grafica: Home → Mission Brief → scena → esito → Debrief → Score &amp; Replay → Home.
    /// Dura circa 30 secondi: cambia obiettivo, mostra un avviso, un feedback, un sottotitolo, poi una decisione con 2 opzioni;
    /// l'esito dipende dalla scelta (1ª = Positivo, 2ª = Parziale, tempo scaduto = Negativo). Serve anche da esempio per i team:
    /// mostra come si usano i metodi di <see cref="ScenarioController"/>. I testi sono dati dello scenario (segnaposto "esempio").
    /// </summary>
    [AddComponentMenu("SI81/Scenari/Mock Scenario")]
    public class MockScenario : ScenarioController
    {
        [Header("Tempi della prova (secondi)")]
        [Tooltip("Pausa tra un passaggio e il successivo.")]
        [SerializeField] private float stepSeconds = 4f;
        [Tooltip("Tempo concesso per scegliere (0 = nessun limite).")]
        [SerializeField] private float decisionSeconds = 12f;

        /// <summary>
        /// Avvia la sequenza finta.
        /// </summary>
        protected override void OnScenarioStarted()
        {
            StartCoroutine(Run());
        }

        /// <summary>
        /// Sequenza: sottotitolo, feedback, avviso, decisione. Le attese sono a tempo di gioco (si fermano in pausa).
        /// </summary>
        private IEnumerator Run()
        {
            SetPhase(PHASE_SCAN);
            SetHint("Guarda attorno a te (esempio)");
            Subtitle("Caposquadra", "Benvenuto: questa è una prova (esempio).");
            yield return new WaitForSeconds(stepSeconds);

            Feedback("Area controllata (esempio)", FeedbackKind.Neutral);
            CompleteObjective();
            yield return new WaitForSeconds(stepSeconds);

            Alert("Fumo dal quadro elettrico\nAllontanati e valuta");
            Subtitle("Collega", "Sento odore di bruciato!");
            SetPhase(PHASE_DECISION);
            yield return new WaitForSeconds(stepSeconds);

            string[] options = { "Dai l'allarme", "Spegni da solo il quadro" };
            if (!AskDecision("Cosa fai?", options, OnDecision, decisionSeconds))
            {
                // Senza HUD_DecisionPanel la prova non può continuare: si chiude con esito parziale.
                OnDecision(1);
            }
        }

        /// <summary>
        /// Scelta fatta (o tempo scaduto = -1): feedback, attesa breve e fine missione con esito diverso.
        /// </summary>
        /// <param name="index">Opzione scelta, o -1 se il tempo è scaduto.</param>
        private void OnDecision(int index)
        {
            StartCoroutine(Finish(index));
        }

        /// <summary>
        /// Mostra il feedback della scelta e chiude la missione.
        /// </summary>
        private IEnumerator Finish(int index)
        {
            SetPhase(PHASE_ACTION);
            CompleteObjective();
            if (index == 0) Feedback("Allarme dato: ottimo (esempio)", FeedbackKind.Positive);
            else if (index == 1) Feedback("Intervento rischioso (esempio)", FeedbackKind.Negative);
            else Feedback("Nessuna decisione (esempio)", FeedbackKind.Negative);
            yield return new WaitForSeconds(stepSeconds * 0.75f);

            SetPhase(PHASE_RESOLUTION);
            var lines = new List<string>();
            if (index == 0)
            {
                lines.Add(DebriefLinePrefix.GOOD + "Hai dato l'allarme: i colleghi sono usciti (esempio).");
                lines.Add(DebriefLinePrefix.GOOD + "Hai controllato l'area prima di agire (esempio).");
                EndMission(OutcomeType.Positive, 92, new[] { 95, 90, 92, 88, 94 }, lines);
            }
            else if (index == 1)
            {
                lines.Add(DebriefLinePrefix.GOOD + "Hai controllato l'area prima di agire (esempio).");
                lines.Add(DebriefLinePrefix.BAD + "Hai spento il quadro da solo, senza dare l'allarme (esempio).");
                EndMission(OutcomeType.Partial, 55, new[] { 50, 70, 45, 60, 50 }, lines);
            }
            else
            {
                lines.Add(DebriefLinePrefix.BAD + "Non hai deciso in tempo (esempio).");
                lines.Add(DebriefLinePrefix.CRITICAL + "Nessun allarme dato (esempio).");
                EndMission(OutcomeType.Negative, 20, new[] { 20, 30, 10, 20, 20 }, lines);
            }
        }
    }
}
