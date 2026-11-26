using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SI81.MachineLock;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MachineLockSpi"/> (GDD §11 and §15).
    /// They cover the three time modes, the dimension weights, the critical penalties and cooldown,
    /// the outcome matrix, the capped debrief and the seven score examples of GDD §11.9.
    /// </summary>
    [TestFixture]
    public sealed class MachineLockSpiTests
    {
        private const float Tolerance = 0.0001f;

        private static readonly string[] ObservationIds =
        {
            "press", "thermometer", "display", "puddle", "panel", "ppe"
        };

        // =====================================================================
        // Time modes (GDD §11.6)
        // =====================================================================

        [Test]
        public void Guided_TimeAtZero_IsFive()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 0f);

            Assert.AreEqual(5f, spi.TimePoints, Tolerance);
        }

        [Test]
        public void Guided_TimeAtHalf_IsTwoPointFive()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 450f);

            Assert.AreEqual(2.5f, spi.TimePoints, Tolerance);
        }

        [Test]
        public void Guided_TimeAtLimit_IsZero_NoHardEnd()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 900f);

            Assert.AreEqual(0f, spi.TimePoints, Tolerance);
            Assert.IsFalse(spi.TimedOut);
        }

        [Test]
        public void Guided_TimeAfterLimit_IsZero_NoHardEnd()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 1000f);

            Assert.AreEqual(0f, spi.TimePoints, Tolerance);
            Assert.IsFalse(spi.TimedOut);
        }

        [Test]
        public void Operational_TimeAtLimit_IsZero_Ends()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Realistic, 600f);

            Assert.AreEqual(0f, spi.TimePoints, Tolerance);
            Assert.IsTrue(spi.TimedOut);
        }

        [Test]
        public void Crisis_TimeAtLimit_IsZero_Ends()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Stress, 480f);

            Assert.AreEqual(0f, spi.TimePoints, Tolerance);
            Assert.IsTrue(spi.TimedOut);
        }

        [Test]
        public void Operational_TimeLimit_Is600()
        {
            Assert.AreEqual(600f, MachineLockSpi.DefaultTimeLimitFor(SpiTimeMode.Realistic));
            Assert.IsTrue(MachineLockSpi.DefaultHardEndFor(SpiTimeMode.Realistic));
            Assert.AreEqual(SpiTimeMode.Realistic,
                MachineLockSpi.TimeModeFromLevel(DifficultyLevel.Operational));
        }

        [Test]
        public void Crisis_TimeLimit_Is480()
        {
            Assert.AreEqual(480f, MachineLockSpi.DefaultTimeLimitFor(SpiTimeMode.Stress));
            Assert.IsTrue(MachineLockSpi.DefaultHardEndFor(SpiTimeMode.Stress));
            Assert.AreEqual(SpiTimeMode.Stress,
                MachineLockSpi.TimeModeFromLevel(DifficultyLevel.Crisis));
        }

        [Test]
        public void Guided_TimeLimit_Is900()
        {
            Assert.AreEqual(900f, MachineLockSpi.DefaultTimeLimitFor(SpiTimeMode.Guided));
            Assert.IsFalse(MachineLockSpi.DefaultHardEndFor(SpiTimeMode.Guided));
            Assert.AreEqual(SpiTimeMode.Guided,
                MachineLockSpi.TimeModeFromLevel(DifficultyLevel.Guided));
        }

        [Test]
        public void Time_AfterLimitInHardMode_AlsoReportsTimedOut()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Realistic, 700f);

            Assert.AreEqual(0f, spi.TimePoints, Tolerance);
            Assert.IsTrue(spi.TimedOut);
        }

        [Test]
        public void SetElapsed_NegativeInput_ClampsToZero()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);
            spi.SetElapsed(-10f);

            Assert.AreEqual(0f, spi.Elapsed, Tolerance);
            Assert.AreEqual(5f, spi.TimePoints, Tolerance);
        }

        [Test]
        public void Timeout_AppliesPenaltyOnlyOnce()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Realistic, 600f);

            spi.ApplyEvent(SpiEvent.Timeout);
            spi.ApplyEvent(SpiEvent.Timeout);

            Assert.AreEqual(35f, spi.Decision, Tolerance);
            Assert.IsTrue(spi.TimedOut);
        }

        // =====================================================================
        // Weights (GDD §11.1)
        // =====================================================================

        [Test]
        public void Spi_Weights_Sum100()
        {
            int sum = MachineLockSpi.SAFETY_MAX
                      + MachineLockSpi.AWARENESS_MAX
                      + MachineLockSpi.DECISION_MAX
                      + MachineLockSpi.EXECUTION_MAX
                      + MachineLockSpi.TIME_MAX;

            Assert.AreEqual(100, sum);
        }

        [Test]
        public void Spi_PerfectRun_Is100()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 0f);
            CompleteAllTasks(spi);

            Assert.AreEqual(20f, spi.Safety, Tolerance);
            Assert.AreEqual(20f, spi.Awareness, Tolerance);
            Assert.AreEqual(40f, spi.Decision, Tolerance);
            Assert.AreEqual(15, spi.Execution);
            Assert.AreEqual(5f, spi.TimePoints, Tolerance);
            Assert.AreEqual(100, spi.Spi);
        }

        // =====================================================================
        // Critical events (GDD §11.2 and §11.4)
        // =====================================================================

        [Test]
        public void Spi_CriticalPenalties()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            Assert.IsTrue(spi.ApplyCritical(CriticalEvent.HotOilBurn, 10f));
            Assert.AreEqual(10f, spi.Safety, Tolerance);
            Assert.AreEqual(30f, spi.Decision, Tolerance);
            Assert.IsTrue(spi.CriticalOccurred);

            Assert.IsTrue(spi.ApplyCritical(CriticalEvent.WaterExtinguisher, 20f));
            Assert.AreEqual(5f, spi.Safety, Tolerance);
            Assert.AreEqual(20f, spi.Decision, Tolerance);

            Assert.IsTrue(spi.ApplyCritical(CriticalEvent.TryStartUnisolated, 30f));
            Assert.AreEqual(0f, spi.Safety, Tolerance);
            Assert.AreEqual(10f, spi.Decision, Tolerance);

            Assert.IsTrue(spi.ApplyCritical(CriticalEvent.LiveOilShock, 40f));
            Assert.AreEqual(0f, spi.Safety, Tolerance);
            Assert.AreEqual(0f, spi.Decision, Tolerance);
        }

        [Test]
        public void Critical_Cooldown_DeduplicatesAndClamps()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            Assert.IsTrue(spi.ApplyCritical(CriticalEvent.HotOilBurn, 10f));
            Assert.IsFalse(spi.ApplyCritical(CriticalEvent.LiveOilShock, 14.9f));
            Assert.AreEqual(10f, spi.Safety, Tolerance);
            Assert.AreEqual(30f, spi.Decision, Tolerance);

            Assert.IsTrue(spi.ApplyCritical(CriticalEvent.LiveOilShock, 15f));
            Assert.AreEqual(0f, spi.Safety, Tolerance);
            Assert.AreEqual(20f, spi.Decision, Tolerance);

            Assert.IsTrue(spi.ApplyCritical(CriticalEvent.PressContact, 20f));
            Assert.AreEqual(0f, spi.Safety, Tolerance);
            Assert.AreEqual(10f, spi.Decision, Tolerance);
        }

        [Test]
        public void Critical_InactionBadEnding_IsIdempotent()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            spi.ApplyEvent(SpiEvent.InactionBadEnding);
            spi.ApplyEvent(SpiEvent.InactionBadEnding);

            Assert.AreEqual(30f, spi.Decision, Tolerance);
            Assert.IsTrue(spi.CriticalOccurred);
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.CriticalInaction.ToString()));
        }

        // =====================================================================
        // Execution and Awareness idempotency (GDD §11.3 and §11.5)
        // =====================================================================

        [Test]
        public void Execution_Rewards_AreIdempotent()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.Alarm);
            spi.ApplyEvent(SpiEvent.Alarm);
            spi.ApplyEvent(SpiEvent.MainOff);
            spi.ApplyEvent(SpiEvent.Lockout);
            spi.ApplyEvent(SpiEvent.MainOff);
            spi.ApplyEvent(SpiEvent.Lockout);
            spi.ApplyEvent(SpiEvent.TesterZero);
            spi.ApplyEvent(SpiEvent.TryStartVerified);
            spi.ApplyEvent(SpiEvent.TesterZero);
            spi.ApplyEvent(SpiEvent.TryStartVerified);
            spi.ApplyEvent(SpiEvent.PpeFull);
            spi.ApplyEvent(SpiEvent.PpeFull);
            spi.ApplyEvent(SpiEvent.ContainmentComplete);
            spi.ApplyEvent(SpiEvent.ContainmentComplete);
            spi.ApplyEvent(SpiEvent.Exit);
            spi.ApplyEvent(SpiEvent.Exit);

            Assert.AreEqual(15, spi.Execution);
            Assert.AreEqual(5, CountLinesWithPrefix(spi, DebriefLinePrefix.GOOD));
        }

        [Test]
        public void Observation_Targets_AreDeduplicated()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            Assert.IsTrue(spi.Observe("press"));
            Assert.IsFalse(spi.Observe("press"));
            Assert.IsFalse(spi.Observe(null));
            Assert.IsFalse(spi.Observe(string.Empty));

            Assert.AreEqual(1, spi.ObservedTargets);
            Assert.AreEqual(20f / 6f, spi.Awareness, Tolerance);

            foreach (string id in ObservationIds) spi.Observe(id);

            Assert.AreEqual(ObservationIds.Length, spi.ObservedTargets);
            Assert.AreEqual(20f, spi.Awareness, Tolerance);
        }

        // =====================================================================
        // Outcome matrix (GDD §11.7)
        // =====================================================================

        [Test]
        public void Spi_OutcomeMatrix()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            Assert.AreEqual(OutcomeType.Negative,
                spi.EvaluateOutcome(Input(pressRunning: true, contained: false, allTasks: false)));
            Assert.AreEqual(OutcomeType.Negative,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: false, allTasks: false, timedOut: true)));
            Assert.AreEqual(OutcomeType.Positive,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: true)));
            Assert.AreEqual(OutcomeType.Partial,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: true, anyCritical: true)));
            Assert.AreEqual(OutcomeType.Partial,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: false)));
            Assert.AreEqual(OutcomeType.Partial,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: false, timedOut: true)));
        }

        // =====================================================================
        // Debrief cap (GDD §11.8)
        // =====================================================================

        [Test]
        public void Debrief_Cap5Good4Error()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

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

            List<string> lines = BuildLines(spi);

            Assert.AreEqual(5, CountLinesWithPrefix(spi, DebriefLinePrefix.GOOD));
            Assert.AreEqual(4, CountLinesWithPrefix(spi, DebriefLinePrefix.CRITICAL));
            Assert.AreEqual(0, CountLinesWithPrefix(spi, DebriefLinePrefix.BAD));
            Assert.AreEqual(9, lines.Count);

            Assert.AreEqual(DebriefLinePrefix.CRITICAL + DebriefLineId.CriticalHotOil,
                lines[5]);
            Assert.AreEqual(DebriefLinePrefix.CRITICAL + DebriefLineId.CriticalWrongPpe,
                lines[8]);

            // The score still includes every penalty even when the rows are trimmed.
            Assert.AreEqual(0f, spi.Decision, Tolerance);
            Assert.AreEqual(0f, spi.Safety, Tolerance);
        }

        [Test]
        public void Debrief_WithTwoCritical_ShowsTwoChronologicalBadLines()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            spi.ApplyCritical(CriticalEvent.HotOilBurn, 0f);
            spi.ApplyCritical(CriticalEvent.LiveOilShock, 10f);

            spi.ApplyEvent(SpiEvent.MissingAlarm);
            spi.ApplyEvent(SpiEvent.MissingMainOff);
            spi.ApplyEvent(SpiEvent.MissingLockout);
            spi.ApplyEvent(SpiEvent.Timeout);

            List<string> lines = BuildLines(spi);

            Assert.AreEqual(2, CountLinesWithPrefix(spi, DebriefLinePrefix.CRITICAL));
            Assert.AreEqual(2, CountLinesWithPrefix(spi, DebriefLinePrefix.BAD));
            Assert.AreEqual(4, lines.Count);
            Assert.AreEqual(DebriefLinePrefix.BAD + DebriefLineId.BadMissingIsolation, lines[3]);
        }

        [Test]
        public void Debrief_MissingContainment_AppliesBothPenaltiesAndOneLine()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            spi.ApplyEvent(SpiEvent.MissingCones);
            spi.ApplyEvent(SpiEvent.MissingAbsorbents);
            spi.ApplyEvent(SpiEvent.MissingCones);
            spi.ApplyEvent(SpiEvent.MissingAbsorbents);

            Assert.AreEqual(31f, spi.Decision, Tolerance);
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.BadMissingContainment.ToString()));
        }

        [Test]
        public void Debrief_MissingIsolation_AppliesBothPenaltiesAndOneLine()
        {
            MachineLockSpi spi = NewSpi(SpiTimeMode.Guided);

            spi.ApplyEvent(SpiEvent.MissingMainOff);
            spi.ApplyEvent(SpiEvent.MissingLockout);

            Assert.AreEqual(36f, spi.Decision, Tolerance);
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.BadMissingIsolation.ToString()));
        }

        // =====================================================================
        // Dimensions (GDD §11.1)
        // =====================================================================

        [Test]
        public void Dimensions100_NormalizesAllDimensions()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 240f);
            foreach (string id in ObservationIds.Take(3)) spi.Observe(id);
            spi.ApplyCritical(CriticalEvent.HotOilBurn, 30f);
            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.PpeFull);
            spi.ApplyEvent(SpiEvent.InactionBadEnding);

            int[] dimensions = spi.Dimensions100();

            Assert.AreEqual(50, dimensions[0]); // Safety 10 / 20
            Assert.AreEqual(50, dimensions[1]); // Awareness 10 / 20
            Assert.AreEqual(50, dimensions[2]); // Decision 20 / 40
            Assert.AreEqual(33, dimensions[3]); // Execution 5 / 15
            Assert.AreEqual(73, dimensions[4]); // Time 3.667 / 5
        }

        [Test]
        public void Dimensions100_PerfectRun_IsAll100()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 0f);
            CompleteAllTasks(spi);

            CollectionAssert.AreEqual(new[] { 100, 100, 100, 100, 100 }, spi.Dimensions100());
        }

        // =====================================================================
        // Config mapping (GDD §14.4)
        // =====================================================================

        [Test]
        public void Config_CreateSpiOptions_MapsAllLevels()
        {
            MachineLockConfig config = ScriptableObject.CreateInstance<MachineLockConfig>();
            try
            {
                MachineLockSpiOptions guided = config.CreateSpiOptions(DifficultyLevel.Guided);
                Assert.AreEqual(900f, guided.TimeLimitSeconds);
                Assert.IsFalse(guided.HardEndAtLimit);

                MachineLockSpiOptions operational = config.CreateSpiOptions(DifficultyLevel.Operational);
                Assert.AreEqual(600f, operational.TimeLimitSeconds);
                Assert.IsTrue(operational.HardEndAtLimit);

                config.timeLimitStress = 500f;
                config.missingAlarmPenalty = 11;
                config.executionExit = 4;
                config.awarenessTargets = 3;

                MachineLockSpiOptions crisis = config.CreateSpiOptions(DifficultyLevel.Crisis);
                Assert.AreEqual(500f, crisis.TimeLimitSeconds);
                Assert.IsTrue(crisis.HardEndAtLimit);
                Assert.AreEqual(11, crisis.MissingAlarmPenalty);
                Assert.AreEqual(4, crisis.ExecutionExit);
                Assert.AreEqual(3, crisis.AwarenessTargets);
                Assert.AreEqual(10, crisis.CriticalSafetyPenalty);
                Assert.AreEqual(5f, crisis.CriticalCooldownSeconds, Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        // =====================================================================
        // Score examples (GDD §11.9)
        // =====================================================================

        [Test]
        public void ScoreExample_Perfect5Minutes_Is98Positive()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 300f);
            CompleteAllTasks(spi);

            Assert.AreEqual(98, spi.Spi);
            Assert.AreEqual(OutcomeType.Positive,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: true)));
        }

        [Test]
        public void ScoreExample_Perfect12Minutes_Is96Positive()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 720f);
            CompleteAllTasks(spi);

            Assert.AreEqual(96, spi.Spi);
            Assert.AreEqual(OutcomeType.Positive,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: true)));
        }

        [Test]
        public void ScoreExample_MissingAlarm14Minutes_Is85Partial()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 840f);
            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.MainOff);
            spi.ApplyEvent(SpiEvent.Lockout);
            spi.ApplyEvent(SpiEvent.TesterZero);
            spi.ApplyEvent(SpiEvent.TryStartVerified);
            spi.ApplyEvent(SpiEvent.PpeFull);
            spi.ApplyEvent(SpiEvent.ContainmentComplete);
            spi.ApplyEvent(SpiEvent.Exit);
            ObserveAll(spi);
            spi.ApplyEvent(SpiEvent.MissingAlarm);

            Assert.AreEqual(32f, spi.Decision, Tolerance);
            Assert.AreEqual(13, spi.Execution);
            Assert.AreEqual(85, spi.Spi);
            Assert.AreEqual(OutcomeType.Partial,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: false)));
        }

        [Test]
        public void ScoreExample_MissingContainmentAndCones16Minutes_Is84Partial()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 960f);
            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.Alarm);
            spi.ApplyEvent(SpiEvent.MainOff);
            spi.ApplyEvent(SpiEvent.Lockout);
            spi.ApplyEvent(SpiEvent.TesterZero);
            spi.ApplyEvent(SpiEvent.TryStartVerified);
            spi.ApplyEvent(SpiEvent.PpeFull);
            spi.ApplyEvent(SpiEvent.Exit);
            ObserveAll(spi);
            spi.ApplyEvent(SpiEvent.MissingCones);
            spi.ApplyEvent(SpiEvent.MissingAbsorbents);

            Assert.AreEqual(0f, spi.TimePoints, Tolerance);
            Assert.AreEqual(31f, spi.Decision, Tolerance);
            Assert.AreEqual(13, spi.Execution);
            Assert.AreEqual(84, spi.Spi);
            Assert.AreEqual(OutcomeType.Partial,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: false, allTasks: false)));
        }

        [Test]
        public void ScoreExample_HotOilContact14Minutes_Is75Partial()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 840f);
            CompleteAllTasks(spi);
            spi.ApplyCritical(CriticalEvent.HotOilBurn, 120f);

            Assert.AreEqual(10f, spi.Safety, Tolerance);
            Assert.AreEqual(30f, spi.Decision, Tolerance);
            Assert.AreEqual(75, spi.Spi);
            Assert.AreEqual(OutcomeType.Partial,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: true, anyCritical: true)));
        }

        [Test]
        public void ScoreExample_WaterExtinguisher15Minutes_Is80Partial()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 900f);
            CompleteAllTasks(spi);
            spi.ApplyCritical(CriticalEvent.WaterExtinguisher, 100f);

            Assert.AreEqual(15f, spi.Safety, Tolerance);
            Assert.AreEqual(30f, spi.Decision, Tolerance);
            Assert.AreEqual(0f, spi.TimePoints, Tolerance);
            Assert.AreEqual(80, spi.Spi);
            Assert.AreEqual(OutcomeType.Partial,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: true, allTasks: true, anyCritical: true)));
        }

        [Test]
        public void ScoreExample_BadEnding2FourMinutes_Is49Negative()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 240f);
            foreach (string id in ObservationIds.Take(3)) spi.Observe(id);
            spi.ApplyCritical(CriticalEvent.HotOilBurn, 30f);
            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.PpeFull);
            spi.ApplyEvent(SpiEvent.InactionBadEnding);

            Assert.AreEqual(10f, spi.Safety, Tolerance);
            Assert.AreEqual(10f, spi.Awareness, Tolerance);
            Assert.AreEqual(20f, spi.Decision, Tolerance);
            Assert.AreEqual(5, spi.Execution);
            Assert.AreEqual(49, spi.Spi);
            Assert.AreEqual(OutcomeType.Negative,
                spi.EvaluateOutcome(Input(pressRunning: true, contained: false, allTasks: false)));
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static MachineLockSpi NewSpi(SpiTimeMode mode)
        {
            return new MachineLockSpi(new MachineLockSpiOptions
            {
                TimeLimitSeconds = MachineLockSpi.DefaultTimeLimitFor(mode),
                HardEndAtLimit = MachineLockSpi.DefaultHardEndFor(mode)
            });
        }

        private static MachineLockSpi StartedRun(SpiTimeMode mode, float elapsedSeconds)
        {
            MachineLockSpi spi = NewSpi(mode);
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

        private static List<string> BuildLines(MachineLockSpi spi)
        {
            return spi.BuildDebriefLines(id => id.ToString());
        }

        private static int CountLines(MachineLockSpi spi, string id)
        {
            return BuildLines(spi).Count(line => line.EndsWith(id, System.StringComparison.Ordinal));
        }

        private static int CountLinesWithPrefix(MachineLockSpi spi, string prefix)
        {
            return BuildLines(spi).Count(line => line.StartsWith(prefix, System.StringComparison.Ordinal));
        }
    }
}
