using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Data;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Debug
{
    /// <summary>
    /// Temporary runner for the E02 checks (evaluator SelfCheck + config sanity). Superseded by EvacDebugCommands.RunSmokeTest in E03.
    /// Add to any GameObject, assign the three configs (optional) and use the ContextMenu.
    /// </summary>
    public sealed class EvacSelfCheckRunner : MonoBehaviour
    {
        [SerializeField] private EvacuationConfig[] configs = new EvacuationConfig[0];

        /// <summary>Runs the evaluator SelfCheck and the config checks; logs PASS/FAIL per case with the [EV] prefix.</summary>
        [ContextMenu("Evac/Run Evaluator SelfCheck")]
        public void RunSelfCheck()
        {
            bool evaluatorOk = new EvacuationOutcomeEvaluator().SelfCheck(Log);
            bool configsOk = CheckConfigs();
            UDebug.Log($"[EV] E02 SelfCheck TOTAL: {(evaluatorOk && configsOk ? "ALL PASS" : "FAILURES")}");
        }

        private bool CheckConfigs()
        {
            if (configs == null || configs.Length == 0)
            {
                Log("[EV] E02 SelfCheck configs: SKIP (no configs assigned)");
                return true;
            }

            bool all = true;
            for (int i = 0; i < configs.Length; i++)
            {
                EvacuationConfig c = configs[i];
                if (c == null)
                {
                    Log($"[EV] E02 SelfCheck FAIL config[{i}]: null reference");
                    all = false;
                    continue;
                }

                var problems = new List<string>();
                c.Validate(problems);
                CheckExpected(c, problems);
                bool pass = problems.Count == 0;
                all &= pass;
                Log($"[EV] E02 SelfCheck {(pass ? "PASS" : "FAIL")} config {c.name} ({c.Difficulty})" + (pass ? string.Empty : " | " + string.Join("; ", problems)));
            }

            return all;
        }

        private static void CheckExpected(EvacuationConfig c, List<string> problems)
        {
            switch (c.Difficulty)
            {
                case EvacDifficulty.Guided:
                    Expect(!c.HasTimeLimit && c.TotalDurationSeconds == 0f, "Guided: no time limit", problems);
                    Expect(c.Hearts == 0 && !c.FireEnabled, "Guided: hearts 0, no fire", problems);
                    Expect(c.HighlightEnabled && c.HintsEnabled, "Guided: highlight+hints on", problems);
                    break;
                case EvacDifficulty.Operational:
                    Expect(c.HasTimeLimit && Mathf.Approximately(c.TotalDurationSeconds, 180f), "Operational: 180 s", problems);
                    Expect(c.Hearts == 3 && !c.FireEnabled, "Operational: hearts 3, no fire", problems);
                    Expect(!c.HighlightEnabled && !c.HintsEnabled, "Operational: no aids", problems);
                    Expect(c.SuccessPoints == 30 && c.PartialPoints == 10 && c.FailurePoints == 0, "Operational: points 30/10/0", problems);
                    break;
                case EvacDifficulty.Crisis:
                    Expect(c.HasTimeLimit && Mathf.Approximately(c.TotalDurationSeconds, 120f), "Crisis: 120 s", problems);
                    Expect(c.Hearts == 1 && c.FireEnabled && c.ActiveFireCount == 5 && c.LethalContacts == 3, "Crisis: 1 heart, fire 5, lethal 3", problems);
                    Expect(!c.HighlightEnabled && !c.HintsEnabled, "Crisis: no aids", problems);
                    Expect(c.SuccessPoints == 50 && c.PartialPoints == 20 && c.FailurePoints == 0, "Crisis: points 50/20/0", problems);
                    break;
            }

            Expect(c.NpcCountRoom1 == 3 && c.NpcCountRoom2 == 2, "NPC 3+2", problems);
            Expect(Mathf.Approximately(c.RouteLaneWidth, 1.2f) && Mathf.Approximately(c.ContactCooldownSeconds, 2f), "lane 1.2 m, cooldown 2 s", problems);
        }

        private static void Expect(bool condition, string what, List<string> problems)
        {
            if (!condition)
                problems.Add(what);
        }

        private static void Log(string message)
        {
            UDebug.Log(message);
        }
    }
}
