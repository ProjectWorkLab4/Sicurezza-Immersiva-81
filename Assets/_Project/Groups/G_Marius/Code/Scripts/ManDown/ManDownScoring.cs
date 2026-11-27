using System.Collections.Generic;
using SI81.UI;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>Risultato grezzo del scoring locale: cinque dimensioni 0-100 e SPI come media aritmetica.</summary>
    [System.Serializable]
    public struct ManDownScoreResult
    {
        public int Safety;
        public int Awareness;
        public int Decision;
        public int Execution;
        public int Time;
        public int Overall;
        public bool AbnormalBranch;
        public bool AedAvailable;
        public bool TimedOut;
        public string ConclusionReason;
        public float Call112At;
        public float AssessmentAt;
        public float Elapsed;
        public float RcpAccumulatedValue;
        /// <summary>Punti RCP normalizzati confluiti in Execution (solo ramo anomalo).</summary>
        public int RcpPoints;
        /// <summary>Massimo punteggio RCP applicabile nel ramo: cap con DAE, 100 senza DAE.</summary>
        public int RcpMaxPoints;
        public int AppliedElectrodesEver;
        public bool ShockCompleted;
        public string SafetyPenalties;

        /// <summary>Dimensioni nell'ordine condiviso: Safety, Awareness, Decision, Execution, Time.</summary>
        public int[] Dimensions => new[] { Safety, Awareness, Decision, Execution, Time };

        public string Summary =>
            $"SPI={Overall} | Safety={Safety} Awareness={Awareness} Decision={Decision} Execution={Execution} Time={Time}" +
            $" | RCPgrezzo={RcpAccumulatedValue:F2} elettrodi={AppliedElectrodesEver} scarica={ShockCompleted}" +
            $" | 112@{(Call112At >= 0f ? Call112At.ToString("F1") : "—")}s respiro@{(AssessmentAt >= 0f ? AssessmentAt.ToString("F1") : "—")}s" +
            $" | penalty=[{SafetyPenalties}]";
    }

    /// <summary>
    /// Calcolo locale del punteggio di Man Down. Non modifica gameplay, UI o esiti: legge soltanto
    /// stato, azioni e statistiche già registrate dallo scenario e usa <see cref="ScoreRules"/> per
    /// l'SPI complessivo (media aritmetica delle cinque dimensioni), senza alterare la formula condivisa.
    /// L'integrazione con EndMission/debriefing è demandata al Task 09.2.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/Scenario Scoring")]
    public sealed class ManDownScoring : MonoBehaviour
    {
        [Header("Safety — penalità una sola volta ciascuna")]
        [SerializeField] private int positionPenalty = 30;
        [SerializeField] private int analysisContactPenalty = 15;
        [SerializeField] private int unsafeShockPenalty = 50;
        [SerializeField] private int unauthorizedShockPenalty = 30;
        [SerializeField] private int inappropriateCprPenalty = 100;
        [SerializeField] private int inappropriateElectrodesPenalty = 30;

        [Header("Awareness")]
        [SerializeField] private int consciousnessPoints = 30;
        [SerializeField] private int breathingPoints = 70;

        [Header("Decision")]
        [SerializeField] private int call112Points = 70;
        [SerializeField] private int normalPositionDecisionPoints = 30;
        [SerializeField] private int abnormalCprWithAedPoints = 20;
        [SerializeField] private int abnormalElectrodesPoints = 10;
        [SerializeField] private int abnormalCprWithoutAedPoints = 30;

        [Header("Execution — respirazione anomala")]
        [Tooltip("Valore RCP grezzo di riferimento: 90 punti cumulativi corrispondono a 100%.")]
        [Min(0.01f)]
        [SerializeField] private float rcpReferenceValue = 90f;
        [SerializeField] private float rcpScoreCapWithAed = 60f;
        [SerializeField] private float firstElectrodePoints = 5f;
        [SerializeField] private float secondElectrodePoints = 5f;
        [SerializeField] private float analysisCompletedPoints = 10f;
        [SerializeField] private float shockPoints = 20f;

        [Header("Execution — respirazione normale")]
        [SerializeField] private float normalPositionExecutionPoints = 100f;
        [SerializeField] private int inappropriateCprExecutionPenalty = 50;

        [Header("Time — chiamata 112")]
        [SerializeField] private float callFullScoreUntil = 20f;
        [SerializeField] private float callMinimumScoreFrom = 60f;
        [SerializeField] private float callFullPoints = 80f;
        [SerializeField] private float callMinimumPoints = 30f;

        [Header("Time — valutazione iniziale")]
        [SerializeField] private float assessmentFullScoreUntil = 20f;
        [SerializeField] private float assessmentZeroScoreFrom = 60f;
        [SerializeField] private float assessmentFullPoints = 20f;

        /// <summary>Soglia di tempestività della chiamata 112 usata in Time, riutilizzata dal debrief.</summary>
        public float CallFullScoreUntil => callFullScoreUntil;

        /// <summary>Calcola il scoring leggendo lo scenario. Non modifica alcuno stato.</summary>
        public ManDownScoreResult Calculate(ManDownScenario scenario)
        {
            var result = new ManDownScoreResult();
            if (scenario == null) return result;

            bool abnormal = scenario.IsBreathingAbnormal;
            bool aed = scenario.IsAedAvailable;
            float elapsed = scenario.MissionElapsedSeconds;
            result.AbnormalBranch = abnormal;
            result.AedAvailable = aed;
            result.TimedOut = scenario.TimedOut;
            result.ConclusionReason = scenario.ConclusionReason;
            result.Elapsed = elapsed;

            // Unico snapshot RCP per scoring, esito e debrief: se la finestra principale è scaduta,
            // si usa quello congelato a quel momento, così le compressioni della proroga non entrano
            // né nel valore RCP né nel conteggio usato per "RCP omessa".
            ManDownCprQualitySnapshot cprSnapshot = scenario.ScoringCprSnapshot;
            int electrodeCount = CountDistinctElectrodes(scenario);
            float rcpAccumulated = cprSnapshot.RcpAccumulatedValue;
            bool cprEverValid = cprSnapshot.TotalCompleted > 0;
            bool shockCompleted = scenario.LastAedSnapshot.ShockCompleted;
            result.AppliedElectrodesEver = electrodeCount;
            result.RcpAccumulatedValue = rcpAccumulated;
            result.ShockCompleted = shockCompleted;

            // --- Safety: ogni categoria al massimo una volta. ---
            var penalties = new List<string>();
            if (HasIncorrectPosition(scenario)) { AddPenalty(penalties, "position", positionPenalty); }
            if (HasNote(scenario, ManDownScenario.ACTION_AED_INVALID_ATTEMPT, "analysis_contact"))
                AddPenalty(penalties, "analysis_contact", analysisContactPenalty);
            if (HasAnyNote(scenario, ManDownScenario.ACTION_AED_INVALID_ATTEMPT,
                    "shock_with_contact", "contact_after_authorization"))
                AddPenalty(penalties, "unsafe_shock", unsafeShockPenalty);
            else if (HasAnyNote(scenario, ManDownScenario.ACTION_AED_INVALID_ATTEMPT,
                    "early_press", "not_advised", "missing_electrodes"))
                AddPenalty(penalties, "unauthorized_shock", unauthorizedShockPenalty);
            if (!abnormal && scenario.IsCprStarted) AddPenalty(penalties, "inappropriate_cpr", inappropriateCprPenalty);
            if (!abnormal && HasAction(scenario, ManDownScenario.ACTION_AED_INCORRECT_USE))
                AddPenalty(penalties, "inappropriate_electrodes", inappropriateElectrodesPenalty);

            int safety = 100;
            foreach (string p in penalties) safety -= ParsePenaltyValue(p);
            result.Safety = Mathf.Clamp(safety, 0, 100);
            result.SafetyPenalties = penalties.Count > 0 ? string.Join(",", penalties) : "none";

            // --- Awareness: due sole valutazioni, ripetute senza punti aggiuntivi. ---
            result.Awareness = ClampInt((scenario.IsConsciousnessChecked ? consciousnessPoints : 0) +
                                        (scenario.IsBreathingChecked ? breathingPoints : 0));

            // --- Decision: 112 + condizioni del ramo. ---
            float decision = scenario.IsCall112Made ? call112Points : 0f;
            if (!abnormal)
            {
                if (scenario.CurrentPosition == ManDownPositionChoice.SidePosition &&
                    scenario.LastCorrectPositionAt >= 0f)
                    decision += normalPositionDecisionPoints;
            }
            else
            {
                if (cprEverValid) decision += aed ? abnormalCprWithAedPoints : abnormalCprWithoutAedPoints;
                if (aed && electrodeCount >= 2) decision += abnormalElectrodesPoints;
            }
            result.Decision = ClampInt(decision);

            // --- Execution. ---
            float execution;
            if (abnormal)
            {
                float normalized = rcpReferenceValue > 0f ? rcpAccumulated / rcpReferenceValue * 100f : 0f;
                float rcpScore = aed ? Mathf.Min(rcpScoreCapWithAed, normalized) : Mathf.Clamp(normalized, 0f, 100f);
                result.RcpPoints = Mathf.Clamp(Mathf.RoundToInt(rcpScore), 0, 100);
                result.RcpMaxPoints = Mathf.Clamp(Mathf.RoundToInt(aed ? rcpScoreCapWithAed : 100f), 0, 100);
                execution = rcpScore;
                if (aed)
                {
                    float aedScore = 0f;
                    if (electrodeCount >= 1) aedScore += firstElectrodePoints;
                    if (electrodeCount >= 2) aedScore += secondElectrodePoints;
                    if (scenario.IsAedActivated) aedScore += analysisCompletedPoints;
                    if (shockCompleted) aedScore += shockPoints;
                    execution += aedScore;
                }
            }
            else
            {
                execution = (scenario.CurrentPosition == ManDownPositionChoice.SidePosition &&
                             scenario.LastCorrectPositionAt >= 0f) ? normalPositionExecutionPoints : 0f;
                if (scenario.IsCprStarted) execution -= inappropriateCprExecutionPenalty;
            }
            result.Execution = Mathf.Clamp(Mathf.RoundToInt(execution), 0, 100);

            // --- Time: tempo effettivo di missione, con pausa già inclusa in MissionElapsedSeconds. ---
            result.Call112At = FirstActionTimestamp(scenario, ManDownScenario.ACTION_CALL_112);
            result.AssessmentAt = scenario.BreathingAssessedAt;
            float callScore = result.Call112At < 0f ? 0f :
                LinearDecrease(result.Call112At, callFullScoreUntil, callMinimumScoreFrom, callFullPoints, callMinimumPoints);
            float assessmentScore = result.AssessmentAt < 0f ? 0f :
                LinearDecrease(result.AssessmentAt, assessmentFullScoreUntil, assessmentZeroScoreFrom, assessmentFullPoints, 0f);
            result.Time = Mathf.Clamp(Mathf.RoundToInt(callScore + assessmentScore), 0, 100);

            var dims = result.Dimensions;
            result.Overall = ScoreRules.ScoreFromDimensions(dims);
            return result;
        }

        private static float LinearDecrease(float t, float fullUntil, float zeroFrom, float fullPoints, float minPoints)
        {
            if (t <= fullUntil) return fullPoints;
            if (t >= zeroFrom) return minPoints;
            float span = Mathf.Max(0.001f, zeroFrom - fullUntil);
            return Mathf.Lerp(fullPoints, minPoints, (t - fullUntil) / span);
        }

        private static int ClampInt(float value)
        {
            return Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
        }

        private static void AddPenalty(List<string> penalties, string id, int value)
        {
            penalties.Add(id + ":" + Mathf.Max(0, value));
        }

        private static int ParsePenaltyValue(string entry)
        {
            int sep = entry.LastIndexOf(':');
            return sep >= 0 && int.TryParse(entry.Substring(sep + 1), out int value) ? value : 0;
        }

        private static bool HasAction(ManDownScenario scenario, string id)
        {
            for (int i = 0; i < scenario.Actions.Count; i++)
                if (scenario.Actions[i].ActionId == id) return true;
            return false;
        }

        private static bool HasIncorrectPosition(ManDownScenario scenario)
        {
            for (int i = 0; i < scenario.Actions.Count; i++)
                if (scenario.Actions[i].ActionId == ManDownScenario.ACTION_POSITION_CHOICE &&
                    !scenario.Actions[i].Success) return true;
            return false;
        }

        private static bool HasNote(ManDownScenario scenario, string id, string note)
        {
            for (int i = 0; i < scenario.Actions.Count; i++)
                if (scenario.Actions[i].ActionId == id && scenario.Actions[i].Note == note) return true;
            return false;
        }

        private static bool HasAnyNote(ManDownScenario scenario, string id, params string[] notes)
        {
            for (int i = 0; i < scenario.Actions.Count; i++)
            {
                if (scenario.Actions[i].ActionId != id) continue;
                for (int j = 0; j < notes.Length; j++)
                    if (scenario.Actions[i].Note == notes[j]) return true;
            }
            return false;
        }

        private static float FirstActionTimestamp(ManDownScenario scenario, string id)
        {
            for (int i = 0; i < scenario.Actions.Count; i++)
                if (scenario.Actions[i].ActionId == id) return scenario.Actions[i].Timestamp;
            return -1f;
        }

        private static int CountDistinctElectrodes(ManDownScenario scenario)
        {
            bool left = false, right = false;
            for (int i = 0; i < scenario.Actions.Count; i++)
            {
                string id = scenario.Actions[i].ActionId;
                if (id == ManDownScenario.ACTION_AED_ELECTRODE_LEFT) left = true;
                else if (id == ManDownScenario.ACTION_AED_ELECTRODE_RIGHT) right = true;
            }
            return (left ? 1 : 0) + (right ? 1 : 0);
        }
    }
}
