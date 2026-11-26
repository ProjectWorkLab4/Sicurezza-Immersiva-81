#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SI81.UI;
using UnityEditor;
using UnityEngine;

namespace SI81.MachineLock.EditorTools
{
    /// <summary>
    /// Editor-only debug menu of the Machine Lock scenario (GDD Appendix D).
    /// It never drives gameplay input: the score tests are pure C# assertions over
    /// <see cref="MachineLockSpi"/>, and the Play Mode helpers call the public notify API of the
    /// scenario instead of synthesising input devices.
    /// </summary>
    public static class MachineLockDebugMenu
    {
        private const string LogPrefix = "[MachineLockDebug] ";
        private const float Tolerance = 0.0001f;

        private static readonly string[] ObservationIds =
        {
            "press", "thermometer", "display", "puddle", "panel", "ppe"
        };

        private static int passed;
        private static int failed;
        private static List<string> failures;

        // =====================================================================
        // Score tests
        // =====================================================================

        /// <summary>
        /// Runs the score and time cases of GDD §11 and §15 without entering Play Mode and logs
        /// one PASS/FAIL row per case plus an overall summary.
        /// </summary>
        [MenuItem("SI81/MachineLock/Run Score Tests")]
        public static void RunScoreTests()
        {
            passed = 0;
            failed = 0;
            failures = new List<string>();

            RunTimeModeChecks();
            RunWeightChecks();
            RunCriticalChecks();
            RunOutcomeChecks();
            RunDebriefChecks();
            RunScoreExampleChecks();

            if (failed == 0)
            {
                Debug.Log(LogPrefix + "PASS: " + passed + " score checks passed, 0 failed.");
                return;
            }

            Debug.LogError(LogPrefix + "FAIL: " + passed + " passed, " + failed + " failed. "
                           + string.Join(" | ", failures.ToArray()));
        }

        private static void RunTimeModeChecks()
        {
            MachineLockSpi guided = CreateSpi(SpiTimeMode.Guided, 0f);
            CheckFloat("Guided_TimeAtZero_IsFive", 5f, guided.TimePoints);

            guided.SetElapsed(450f);
            CheckFloat("Guided_TimeAtHalf_IsTwoPointFive", 2.5f, guided.TimePoints);

            guided.SetElapsed(900f);
            CheckFloat("Guided_TimeAtLimit_IsZero", 0f, guided.TimePoints);
            Check("Guided_TimeAtLimit_NoHardEnd", !guided.TimedOut);

            guided.SetElapsed(1000f);
            CheckFloat("Guided_TimeAfterLimit_IsZero", 0f, guided.TimePoints);
            Check("Guided_TimeAfterLimit_NoHardEnd", !guided.TimedOut);

            MachineLockSpi operational = CreateSpi(SpiTimeMode.Realistic, 600f);
            CheckFloat("Operational_TimeAtLimit_IsZero", 0f, operational.TimePoints);
            Check("Operational_TimeAtLimit_Ends", operational.TimedOut);

            MachineLockSpi crisis = CreateSpi(SpiTimeMode.Stress, 480f);
            CheckFloat("Crisis_TimeAtLimit_IsZero", 0f, crisis.TimePoints);
            Check("Crisis_TimeAtLimit_Ends", crisis.TimedOut);

            CheckFloat("Guided_TimeLimit_Is900", 900f,
                MachineLockSpi.DefaultTimeLimitFor(SpiTimeMode.Guided));
            CheckFloat("Operational_TimeLimit_Is600", 600f,
                MachineLockSpi.DefaultTimeLimitFor(SpiTimeMode.Realistic));
            CheckFloat("Crisis_TimeLimit_Is480", 480f,
                MachineLockSpi.DefaultTimeLimitFor(SpiTimeMode.Stress));
            Check("Guided_HasNoHardEnd", !MachineLockSpi.DefaultHardEndFor(SpiTimeMode.Guided));
        }

        private static void RunWeightChecks()
        {
            int sum = MachineLockSpi.SAFETY_MAX
                      + MachineLockSpi.AWARENESS_MAX
                      + MachineLockSpi.DECISION_MAX
                      + MachineLockSpi.EXECUTION_MAX
                      + MachineLockSpi.TIME_MAX;
            Check("Spi_Weights_Sum100", sum == 100, "sum=" + sum);

            MachineLockSpi perfect = CreateSpi(SpiTimeMode.Guided, 0f);
            CompleteAllTasks(perfect);
            Check("Spi_PerfectRun_Is100", perfect.Spi == 100, "spi=" + perfect.Spi);
        }

        private static void RunCriticalChecks()
        {
            MachineLockSpi spi = CreateSpi(SpiTimeMode.Guided, 0f);
            bool hotOil = spi.ApplyCritical(CriticalEvent.HotOilBurn, 10f);
            Check("Spi_Critical_HotOilPenalties",
                hotOil && Nearly(spi.Safety, 10f) && Nearly(spi.Decision, 30f),
                "safety=" + spi.Safety + " decision=" + spi.Decision);

            bool cooldown = spi.ApplyCritical(CriticalEvent.LiveOilShock, 12f);
            Check("Spi_Critical_CooldownDiscardsSecondEvent",
                !cooldown && Nearly(spi.Safety, 10f) && Nearly(spi.Decision, 30f),
                "safety=" + spi.Safety + " decision=" + spi.Decision);

            bool afterCooldown = spi.ApplyCritical(CriticalEvent.LiveOilShock, 15f);
            Check("Spi_Critical_AfterCooldownApplies",
                afterCooldown && Nearly(spi.Safety, 0f) && Nearly(spi.Decision, 20f),
                "safety=" + spi.Safety + " decision=" + spi.Decision);

            bool clamp = spi.ApplyCritical(CriticalEvent.PressContact, 20f);
            Check("Spi_Critical_ClampsAtZero",
                clamp && Nearly(spi.Safety, 0f) && Nearly(spi.Decision, 10f),
                "safety=" + spi.Safety + " decision=" + spi.Decision);
        }

        private static void RunOutcomeChecks()
        {
            MachineLockSpi spi = CreateSpi(SpiTimeMode.Guided, 0f);

            Check("Spi_Outcome_PressRunningIsNegative",
                spi.EvaluateOutcome(Input(true, false, false)) == OutcomeType.Negative);
            Check("Spi_Outcome_TimeoutNotContainedIsNegative",
                spi.EvaluateOutcome(Input(false, false, false, false, true)) == OutcomeType.Negative);
            Check("Spi_Outcome_AllTasksIsPositive",
                spi.EvaluateOutcome(Input(false, true, true)) == OutcomeType.Positive);
            Check("Spi_Outcome_CriticalIsPartial",
                spi.EvaluateOutcome(Input(false, true, true, true)) == OutcomeType.Partial);
            Check("Spi_Outcome_MissingTasksIsPartial",
                spi.EvaluateOutcome(Input(false, true, false)) == OutcomeType.Partial);
            Check("Spi_Outcome_TimeoutContainedIsPartial",
                spi.EvaluateOutcome(Input(false, true, false, false, true)) == OutcomeType.Partial);
        }

        private static void RunDebriefChecks()
        {
            MachineLockSpi spi = CreateSpi(SpiTimeMode.Guided, 0f);
            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.Alarm);
            spi.ApplyEvent(SpiEvent.MainOff);
            spi.ApplyEvent(SpiEvent.Lockout);
            spi.ApplyEvent(SpiEvent.TesterZero);
            spi.ApplyEvent(SpiEvent.TryStartVerified);
            spi.ApplyEvent(SpiEvent.PpeFull);
            spi.ApplyEvent(SpiEvent.ContainmentComplete);

            spi.ApplyCritical(CriticalEvent.HotOilBurn, 0f);
            spi.ApplyCritical(CriticalEvent.LiveOilShock, 5f);
            spi.ApplyCritical(CriticalEvent.PressContact, 10f);
            spi.ApplyCritical(CriticalEvent.WrongPpeContact, 15f);
            spi.ApplyCritical(CriticalEvent.WaterExtinguisher, 20f);
            spi.ApplyCritical(CriticalEvent.TryStartUnisolated, 25f);
            spi.ApplyEvent(SpiEvent.MissingAlarm);

            List<string> lines = spi.BuildDebriefLines(id => id.ToString());
            int good = CountPrefix(lines, DebriefLinePrefix.GOOD);
            int critical = CountPrefix(lines, DebriefLinePrefix.CRITICAL);
            int bad = CountPrefix(lines, DebriefLinePrefix.BAD);

            Check("Debrief_Cap5Good",
                good == MachineLockSpi.MAX_GOOD_LINES, "good=" + good);
            Check("Debrief_Cap4Critical",
                critical == MachineLockSpi.MAX_ERROR_LINES, "critical=" + critical);
            Check("Debrief_CriticalBudgetDisplacesBad", bad == 0, "bad=" + bad);
            Check("Debrief_TotalRows", lines.Count == 9, "count=" + lines.Count);
            Check("Debrief_KeepsChronologicalCriticalOrder",
                lines.Count == 9
                && lines[5] == DebriefLinePrefix.CRITICAL + DebriefLineId.CriticalHotOil
                && lines[8] == DebriefLinePrefix.CRITICAL + DebriefLineId.CriticalWrongPpe);

            MachineLockSpi containment = CreateSpi(SpiTimeMode.Guided, 0f);
            containment.ApplyEvent(SpiEvent.MissingCones);
            containment.ApplyEvent(SpiEvent.MissingAbsorbents);
            Check("Debrief_MissingContainment_CombinesOneLine",
                Nearly(containment.Decision, 31f)
                && CountLines(containment, DebriefLineId.BadMissingContainment.ToString()) == 1,
                "decision=" + containment.Decision);

            MachineLockSpi isolation = CreateSpi(SpiTimeMode.Guided, 0f);
            isolation.ApplyEvent(SpiEvent.MissingMainOff);
            isolation.ApplyEvent(SpiEvent.MissingLockout);
            Check("Debrief_MissingIsolation_CombinesOneLine",
                Nearly(isolation.Decision, 36f)
                && CountLines(isolation, DebriefLineId.BadMissingIsolation.ToString()) == 1,
                "decision=" + isolation.Decision);
        }

        private static void RunScoreExampleChecks()
        {
            MachineLockSpi perfect5 = CreateSpi(SpiTimeMode.Guided, 300f);
            CompleteAllTasks(perfect5);
            Check("ScoreExample_Perfect5Minutes_Is98Positive",
                perfect5.Spi == 98
                && perfect5.EvaluateOutcome(Input(false, true, true)) == OutcomeType.Positive,
                "spi=" + perfect5.Spi);

            MachineLockSpi perfect12 = CreateSpi(SpiTimeMode.Guided, 720f);
            CompleteAllTasks(perfect12);
            Check("ScoreExample_Perfect12Minutes_Is96Positive",
                perfect12.Spi == 96
                && perfect12.EvaluateOutcome(Input(false, true, true)) == OutcomeType.Positive,
                "spi=" + perfect12.Spi);

            MachineLockSpi missingAlarm = CreateSpi(SpiTimeMode.Guided, 840f);
            missingAlarm.ApplyEvent(SpiEvent.EStop);
            missingAlarm.ApplyEvent(SpiEvent.MainOff);
            missingAlarm.ApplyEvent(SpiEvent.Lockout);
            missingAlarm.ApplyEvent(SpiEvent.TesterZero);
            missingAlarm.ApplyEvent(SpiEvent.TryStartVerified);
            missingAlarm.ApplyEvent(SpiEvent.PpeFull);
            missingAlarm.ApplyEvent(SpiEvent.ContainmentComplete);
            missingAlarm.ApplyEvent(SpiEvent.Exit);
            ObserveAll(missingAlarm);
            missingAlarm.ApplyEvent(SpiEvent.MissingAlarm);
            Check("ScoreExample_MissingAlarm14Minutes_Is85Partial",
                missingAlarm.Spi == 85
                && missingAlarm.EvaluateOutcome(Input(false, true, false)) == OutcomeType.Partial,
                "spi=" + missingAlarm.Spi);

            MachineLockSpi missingContainment = CreateSpi(SpiTimeMode.Guided, 960f);
            missingContainment.ApplyEvent(SpiEvent.EStop);
            missingContainment.ApplyEvent(SpiEvent.Alarm);
            missingContainment.ApplyEvent(SpiEvent.MainOff);
            missingContainment.ApplyEvent(SpiEvent.Lockout);
            missingContainment.ApplyEvent(SpiEvent.TesterZero);
            missingContainment.ApplyEvent(SpiEvent.TryStartVerified);
            missingContainment.ApplyEvent(SpiEvent.PpeFull);
            missingContainment.ApplyEvent(SpiEvent.Exit);
            ObserveAll(missingContainment);
            missingContainment.ApplyEvent(SpiEvent.MissingCones);
            missingContainment.ApplyEvent(SpiEvent.MissingAbsorbents);
            Check("ScoreExample_MissingContainmentAndCones16Minutes_Is84Partial",
                missingContainment.Spi == 84
                && missingContainment.EvaluateOutcome(Input(false, false, false)) == OutcomeType.Partial,
                "spi=" + missingContainment.Spi);

            MachineLockSpi hotOil = CreateSpi(SpiTimeMode.Guided, 840f);
            CompleteAllTasks(hotOil);
            hotOil.ApplyCritical(CriticalEvent.HotOilBurn, 120f);
            Check("ScoreExample_HotOilContact14Minutes_Is75Partial",
                hotOil.Spi == 75
                && hotOil.EvaluateOutcome(Input(false, true, true, true)) == OutcomeType.Partial,
                "spi=" + hotOil.Spi);

            MachineLockSpi water = CreateSpi(SpiTimeMode.Guided, 900f);
            CompleteAllTasks(water);
            water.ApplyCritical(CriticalEvent.WaterExtinguisher, 100f);
            Check("ScoreExample_WaterExtinguisher15Minutes_Is80Partial",
                water.Spi == 80
                && water.EvaluateOutcome(Input(false, true, true, true)) == OutcomeType.Partial,
                "spi=" + water.Spi);

            MachineLockSpi badEnding = CreateSpi(SpiTimeMode.Guided, 240f);
            foreach (string id in ObservationIds)
            {
                if (badEnding.ObservedTargets < 3) badEnding.Observe(id);
            }
            badEnding.ApplyCritical(CriticalEvent.HotOilBurn, 30f);
            badEnding.ApplyEvent(SpiEvent.EStop);
            badEnding.ApplyEvent(SpiEvent.PpeFull);
            badEnding.ApplyEvent(SpiEvent.InactionBadEnding);
            Check("ScoreExample_BadEnding2FourMinutes_Is49Negative",
                badEnding.Spi == 49
                && badEnding.EvaluateOutcome(Input(true, false, false)) == OutcomeType.Negative,
                "spi=" + badEnding.Spi);
        }

        // =====================================================================
        // State and Play Mode helpers
        // =====================================================================

        /// <summary>
        /// Logs the task flags, the normalized dimensions, the SPI, the elapsed time, the state and the
        /// outcome of the Machine Lock scenario in the open scene.
        /// </summary>
        [MenuItem("SI81/MachineLock/Log State")]
        private static void LogState()
        {
            MachineLockScenario scenario = FindScenario();
            if (scenario == null) return;

            Debug.Log(LogPrefix + "State=" + scenario.State
                      + " Level=" + scenario.Level
                      + " Elapsed=" + scenario.ElapsedSeconds.ToString("0.0") + "s"
                      + " Macro=" + scenario.CurrentMacroObjective);

            Debug.Log(LogPrefix + "Tasks: EStop=" + scenario.TaskEStop
                      + " Alarm=" + scenario.TaskAlarm
                      + " MainOff=" + scenario.TaskMainOff
                      + " Lockout=" + scenario.TaskLockout
                      + " TesterZero=" + scenario.TaskTesterZero
                      + " TryStart=" + scenario.TaskTryStart
                      + " PpeFull=" + scenario.TaskPpeFull
                      + " Containment=" + scenario.TaskContainment
                      + " Exit=" + scenario.TaskExit
                      + " Cones=" + scenario.ConesPlaced + "/4"
                      + " Absorbents=" + scenario.AbsorbentsPlaced + "/4");

            MachineLockSpi spi = scenario.Spi;
            if (spi == null)
            {
                Debug.Log(LogPrefix + "SPI not available: start the mission first (Play Mode).");
                return;
            }

            int[] dimensions = spi.Dimensions100();
            Debug.Log(LogPrefix + "Dimensions100: Safety=" + dimensions[0]
                      + " Awareness=" + dimensions[1]
                      + " Decision=" + dimensions[2]
                      + " Execution=" + dimensions[3]
                      + " Time=" + dimensions[4]
                      + " | SPI=" + spi.Spi
                      + " | Observed=" + spi.ObservedTargets
                      + " | Critical=" + spi.CriticalOccurred
                      + " | TimedOut=" + spi.TimedOut);

            OutcomeType outcome = spi.EvaluateOutcome(BuildOutcomeInput(scenario, spi));
            Debug.Log(LogPrefix + "Outcome=" + outcome);
        }

        /// <summary>
        /// Play Mode helper: forces the residual energy of the machine to be live.
        /// </summary>
        [MenuItem("SI81/MachineLock/Force Energized")]
        private static void ForceEnergized()
        {
            MachineLockEnergyPanel panel = FindEnergyPanel();
            if (panel == null) return;
            panel.ForceEnergized();
            Debug.Log(LogPrefix + "MachineLockEnergyPanel.ForceEnergized() invoked.");
        }

        /// <summary>
        /// Play Mode helper: forces the residual energy to zero.
        /// </summary>
        [MenuItem("SI81/MachineLock/Force Zero Energy")]
        private static void ForceZeroEnergy()
        {
            MachineLockEnergyPanel panel = FindEnergyPanel();
            if (panel == null) return;
            panel.ForceZeroEnergy();
            Debug.Log(LogPrefix + "MachineLockEnergyPanel.ForceZeroEnergy() invoked.");
        }

        /// <summary>
        /// Play Mode QA helper: advances the next incomplete task via the public notify API of the
        /// scenario, from the E-stop through the exit handover.
        /// </summary>
        [MenuItem("SI81/MachineLock/Complete Current Objective")]
        private static void CompleteCurrentObjective()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(LogPrefix + "Complete Current Objective is available in Play Mode only.");
                return;
            }

            MachineLockScenario scenario = FindScenario();
            if (scenario == null) return;

            if (!scenario.TaskEStop)
            {
                scenario.NotifyEStopPressed();
                Debug.Log(LogPrefix + "Completed step: E-stop.");
                return;
            }

            if (!scenario.TaskAlarm)
            {
                scenario.NotifyAlarmPressed();
                Debug.Log(LogPrefix + "Completed step: alarm.");
                return;
            }

            if (!scenario.TaskMainOff)
            {
                scenario.NotifyMainPowerOff();
                Debug.Log(LogPrefix + "Completed step: main power off.");
                return;
            }

            if (!scenario.TaskLockout)
            {
                scenario.NotifyLockoutApplied();
                Debug.Log(LogPrefix + "Completed step: lockout.");
                return;
            }

            if (!scenario.TaskTesterZero)
            {
                scenario.NotifyTesterRead(true);
                Debug.Log(LogPrefix + "Completed step: tester zero.");
                return;
            }

            if (!scenario.TaskTryStart)
            {
                scenario.NotifyTryStartVerified();
                Debug.Log(LogPrefix + "Completed step: try-start verified.");
                return;
            }

            if (!scenario.TaskPpeFull)
            {
                scenario.NotifyPpeChanged(4, true);
                Debug.Log(LogPrefix + "Completed step: full PPE.");
                return;
            }

            if (scenario.ConesPlaced < 4)
            {
                scenario.NotifyConePlaced(scenario.ConesPlaced + 1);
                Debug.Log(LogPrefix + "Completed step: cone " + scenario.ConesPlaced + "/4.");
                return;
            }

            if (scenario.AbsorbentsPlaced < 4)
            {
                scenario.NotifyAbsorbentPlaced(scenario.AbsorbentsPlaced + 1);
                Debug.Log(LogPrefix + "Completed step: absorbent " + scenario.AbsorbentsPlaced + "/4.");
                return;
            }

            if (!scenario.TaskExit)
            {
                scenario.NotifyExitReached();
                Debug.Log(LogPrefix + "Completed step: exit handover.");
                return;
            }

            Debug.Log(LogPrefix + "Every task is already complete.");
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static MachineLockScenario FindScenario()
        {
            MachineLockScenario scenario = UnityEngine.Object.FindFirstObjectByType<MachineLockScenario>(
                FindObjectsInactive.Include);
            if (scenario == null)
                Debug.LogWarning(LogPrefix + "No MachineLockScenario in the open scene.");
            return scenario;
        }

        /// <summary>
        /// Finds the Machine Lock energy panel in the open scene, warning when it is absent.
        /// </summary>
        private static MachineLockEnergyPanel FindEnergyPanel()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(LogPrefix + "Residual energy helpers are available in Play Mode only.");
                return null;
            }

            MachineLockEnergyPanel panel = UnityEngine.Object.FindFirstObjectByType<MachineLockEnergyPanel>(
                FindObjectsInactive.Include);
            if (panel == null)
                Debug.LogWarning(LogPrefix + "No MachineLockEnergyPanel in the open scene.");
            return panel;
        }

        private static MachineLockOutcomeInput BuildOutcomeInput(MachineLockScenario scenario, MachineLockSpi spi)
        {
            return new MachineLockOutcomeInput
            {
                PressRunning = !scenario.TaskEStop,
                Contained = scenario.TaskContainment,
                AllTasksComplete = scenario.TaskEStop && scenario.TaskAlarm && scenario.TaskMainOff
                                   && scenario.TaskLockout && scenario.TaskTesterZero
                                   && scenario.TaskTryStart && scenario.TaskPpeFull
                                   && scenario.TaskContainment && scenario.TaskExit,
                AnyCritical = spi.CriticalOccurred,
                TimedOut = spi.TimedOut
            };
        }

        private static MachineLockSpi CreateSpi(SpiTimeMode mode, float elapsedSeconds)
        {
            var options = new MachineLockSpiOptions
            {
                TimeLimitSeconds = MachineLockSpi.DefaultTimeLimitFor(mode),
                HardEndAtLimit = MachineLockSpi.DefaultHardEndFor(mode)
            };
            var spi = new MachineLockSpi(options);
            spi.SetElapsed(elapsedSeconds);
            return spi;
        }

        private static void CompleteAllTasks(MachineLockSpi spi)
        {
            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.Alarm);
            spi.ApplyEvent(SpiEvent.MainOff);
            spi.ApplyEvent(SpiEvent.Lockout);
            spi.ApplyEvent(SpiEvent.TesterZero);
            spi.ApplyEvent(SpiEvent.TryStartVerified);
            spi.ApplyEvent(SpiEvent.PpeFull);
            spi.ApplyEvent(SpiEvent.ContainmentComplete);
            spi.ApplyEvent(SpiEvent.Exit);
            ObserveAll(spi);
        }

        private static void ObserveAll(MachineLockSpi spi)
        {
            foreach (string id in ObservationIds) spi.Observe(id);
        }

        private static MachineLockOutcomeInput Input(
            bool pressRunning,
            bool contained,
            bool allTasks,
            bool anyCritical = false,
            bool timedOut = false)
        {
            return new MachineLockOutcomeInput
            {
                PressRunning = pressRunning,
                Contained = contained,
                AllTasksComplete = allTasks,
                AnyCritical = anyCritical,
                TimedOut = timedOut
            };
        }

        private static int CountPrefix(List<string> lines, string prefix)
        {
            int count = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal)) count++;
            }
            return count;
        }

        private static int CountLines(MachineLockSpi spi, string id)
        {
            int count = 0;
            foreach (string line in spi.BuildDebriefLines(lineId => lineId.ToString()))
            {
                if (line.EndsWith(id, StringComparison.Ordinal)) count++;
            }
            return count;
        }

        private static void Check(string name, bool condition, string detail = null)
        {
            if (condition)
            {
                passed++;
                Debug.Log(LogPrefix + "PASS " + name);
                return;
            }

            failed++;
            string message = name + (string.IsNullOrEmpty(detail) ? string.Empty : " (" + detail + ")");
            failures.Add(message);
            Debug.LogError(LogPrefix + "FAIL " + message);
        }

        private static void CheckFloat(string name, float expected, float actual)
        {
            Check(name, Nearly(actual, expected), "expected=" + expected + " actual=" + actual);
        }

        private static bool Nearly(float actual, float expected)
        {
            return Math.Abs(actual - expected) <= Tolerance;
        }
    }
}
#endif
