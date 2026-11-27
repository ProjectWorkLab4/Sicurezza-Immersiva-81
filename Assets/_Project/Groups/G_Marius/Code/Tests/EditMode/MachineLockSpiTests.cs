using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SI81.MachineLock;
using SI81.UI;
using UnityEngine;
using Object = UnityEngine.Object;

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
        // 4-slot PPE model & Italian localization (ADR-0004, Issue #35)
        // =====================================================================

        [Test]
        public void PpeSlots_ContainExactlyFourExpectedSlots_WithFeetReplacingRespiratory()
        {
            string[] names = Enum.GetNames(typeof(PpeSlot));

            Assert.AreEqual(4, names.Length);
            CollectionAssert.AreEquivalent(new[] { "Gloves", "Body", "Face", "Feet" }, names);
            Assert.IsFalse(names.Contains("Respiratory"), "Respiratory slot was replaced by Feet (Safety Boots).");
        }

        [Test]
        public void PpeTexts_EquippedFormat_MatchesNomeDpiIndossato()
        {
            Assert.AreEqual("Guanti anticalore EN 407 indossato",
                string.Format(MachineLockTexts.PpeEquippedFormat, "Guanti anticalore EN 407"));
            Assert.AreEqual("Grembiule antischizzo indossato",
                string.Format(MachineLockTexts.PpeEquippedFormat, "Grembiule antischizzo"));
            Assert.AreEqual("Visiera facciale EN 166 indossato",
                string.Format(MachineLockTexts.PpeEquippedFormat, "Visiera facciale EN 166"));
            Assert.AreEqual("Scarpe antinfortunistiche S3 indossato",
                string.Format(MachineLockTexts.PpeEquippedFormat, "Scarpe antinfortunistiche S3"));
        }

        [Test]
        public void PpeCabinet_DistractorReplacement_ClearsHasWrongPpeEquipped_WithSafetyBoots()
        {
            GameObject root = new GameObject("TEST_EditModeCabinet");
            try
            {
                var cabinet = root.AddComponent<MachineLockPpeCabinet>();
                MachineLockPpeItem heatGloves = AddEditModeItem(root, cabinet, PpeSlot.Gloves, true, "Guanti anticalore EN 407");
                MachineLockPpeItem apron = AddEditModeItem(root, cabinet, PpeSlot.Body, true, "Grembiule antischizzo");
                MachineLockPpeItem faceShield = AddEditModeItem(root, cabinet, PpeSlot.Face, true, "Visiera facciale EN 166");
                MachineLockPpeItem safetyBoots = AddEditModeItem(root, cabinet, PpeSlot.Feet, true, "Scarpe antinfortunistiche S3");
                MachineLockPpeItem latexGloves = AddEditModeItem(root, cabinet, PpeSlot.Gloves, false, "Guanti in lattice monouso");
                MachineLockPpeItem surgicalMask = AddEditModeItem(root, cabinet, PpeSlot.Face, false, "Mascherina chirurgica");

                cabinet.Open();

                Assert.IsTrue(cabinet.Equip(latexGloves));
                Assert.IsTrue(cabinet.Equip(surgicalMask));
                Assert.IsTrue(cabinet.HasWrongPpeEquipped);
                Assert.AreEqual(0, cabinet.CorrectCount);

                Assert.IsTrue(cabinet.Equip(heatGloves));
                Assert.IsFalse(latexGloves.IsEquipped);
                Assert.IsTrue(cabinet.HasWrongPpeEquipped, "Surgical mask still occupies Face slot.");

                Assert.IsTrue(cabinet.Equip(faceShield));
                Assert.IsFalse(surgicalMask.IsEquipped);
                Assert.IsFalse(cabinet.HasWrongPpeEquipped, "Replacing both distractors clears HasWrongPpeEquipped.");

                Assert.IsTrue(cabinet.Equip(apron));
                Assert.IsTrue(cabinet.Equip(safetyBoots));
                Assert.IsTrue(cabinet.HasFullPpe);
                Assert.AreEqual(4, cabinet.CorrectCount);
                Assert.AreEqual("Scarpe antinfortunistiche S3 indossato", safetyBoots.GetHoverFeedbackText());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // =====================================================================
        // Early Exit & Persistent Pattern Rotation (ADR-0006, Issue #41)
        // =====================================================================

        [Test]
        public void EarlyExit_MissingEStop_AppliesCriticalPenaltiesDebriefAndNegativeOutcome()
        {
            MachineLockSpi guided = StartedRun(SpiTimeMode.Guided, 15f);
            guided.ApplyEvent(SpiEvent.MissingEStop);
            guided.ApplyEvent(SpiEvent.MissingEStop); // Idempotent

            Assert.AreEqual(0f, guided.Safety, Tolerance);
            Assert.AreEqual(0f, guided.Decision, Tolerance);
            Assert.AreEqual(0f, guided.TimePoints, Tolerance, "Abandoning a running press forfeits the speed bonus.");
            Assert.AreEqual(0, guided.Spi, "Immediate Early Exit with press running yields SPI 0.");
            Assert.IsTrue(guided.CriticalOccurred);
            Assert.AreEqual(1, CountLines(guided, DebriefLineId.CriticalMissingEStop.ToString()));
            Assert.AreEqual(OutcomeType.Negative,
                guided.EvaluateOutcome(Input(pressRunning: true, contained: false, allTasks: false, anyCritical: true)));
            Assert.That(MachineLockTexts.DebriefLine(DebriefLineId.CriticalMissingEStop),
                Does.Contain("pressa ancora in funzione"));
        }

        [Test]
        public void EarlyExit_MissingEStop_OperationalAndCrisis_ZeroesScoreEvenWithPartialProgress()
        {
            foreach (SpiTimeMode mode in new[] { SpiTimeMode.Realistic, SpiTimeMode.Stress })
            {
                MachineLockSpi spi = StartedRun(mode, 60f);
                ObserveAll(spi);
                spi.ApplyEvent(SpiEvent.Alarm);
                spi.ApplyEvent(SpiEvent.MissingEStop);

                Assert.AreEqual(0, spi.Spi, mode + " Early Exit with press running must yield zero score.");
                Assert.AreEqual(OutcomeType.Negative,
                    spi.EvaluateOutcome(Input(pressRunning: true, contained: false, allTasks: false, anyCritical: true)));
            }
        }

        [Test]
        public void EarlyExit_PressStoppedWithMissingNonCriticalTasks_YieldsPartialWithPenalties()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 120f);
            spi.ApplyEvent(SpiEvent.EStop);
            spi.ApplyEvent(SpiEvent.MissingAlarm);
            spi.ApplyEvent(SpiEvent.MissingMainOff);
            spi.ApplyEvent(SpiEvent.MissingLockout);
            spi.ApplyEvent(SpiEvent.MissingCones);
            spi.ApplyEvent(SpiEvent.MissingAbsorbents);
            spi.ApplyEvent(SpiEvent.Exit);

            Assert.AreEqual(OutcomeType.Partial,
                spi.EvaluateOutcome(Input(pressRunning: false, contained: false, allTasks: false)));
            Assert.AreEqual(19f, spi.Decision, Tolerance);
            Assert.AreEqual(3, spi.Execution);
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.BadMissingAlarm.ToString()));
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.BadMissingIsolation.ToString()));
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.BadMissingContainment.ToString()));
        }

        [TestCase(3, 10, 2, 12345)]
        [TestCase(MachineLockEnergyPanel.SUB_STATION_PERMUTATION_COUNT, 5, 4, 67890)]
        public void PatternSelector_NeverRepeatsBackToBack_CyclesAll_AndPersistsAcrossReload(
            int patternCount, int cycles, int forcedIndex, int seed)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "ml_pattern_test_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var rng = new System.Random(seed);
                Func<int, int> nextInt = max => rng.Next(max);

                int previous = -1;
                for (int cycle = 0; cycle < cycles; cycle++)
                {
                    var seenInCycle = new HashSet<int>();
                    for (int step = 0; step < patternCount; step++)
                    {
                        // Reload from disk every single run to prove JSON persistence across sessions.
                        var selector = new MachineLockPatternSelector(tempPath, patternCount, nextInt);
                        int chosen = selector.NextPattern();

                        Assert.That(chosen, Is.InRange(0, patternCount - 1));
                        Assert.AreNotEqual(previous, chosen,
                            $"Back-to-back repeat at cycle {cycle} step {step}: {chosen}");
                        seenInCycle.Add(chosen);
                        previous = chosen;
                    }
                    Assert.AreEqual(patternCount, seenInCycle.Count,
                        $"Every {patternCount}-run cycle must visit all {patternCount} entries (cycle {cycle}).");
                }

                // Forced pattern override must return the forced index without corrupting saved state.
                var forcedSelector = new MachineLockPatternSelector(tempPath, patternCount, nextInt);
                int beforeForced = forcedSelector.LastPatternIndex;
                Assert.AreEqual(forcedIndex, forcedSelector.NextPattern(forcedPatternIndex: forcedIndex));
                Assert.AreEqual(beforeForced, forcedSelector.LastPatternIndex);

                // Corrupt JSON file must recover gracefully without throwing.
                File.WriteAllText(tempPath, "{ not valid json !!!");
                var recovered = new MachineLockPatternSelector(tempPath, patternCount, nextInt);
                Assert.That(recovered.NextPattern(), Is.InRange(0, patternCount - 1));
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        [Test]
        public void Hints_UseLandmarksOnly_WithZeroCardinalDirections()
        {
            string[] hints =
            {
                string.Format(MachineLockTexts.HintStartFormat, 0),
                MachineLockTexts.HintEStop,
                MachineLockTexts.HintAlarm,
                MachineLockTexts.HintMainPower,
                MachineLockTexts.HintLockout,
                MachineLockTexts.HintTester,
                MachineLockTexts.HintTryStart,
                MachineLockTexts.HintPpe,
                string.Format(MachineLockTexts.HintPpePartialFormat, 2),
                MachineLockTexts.HintValveAndContainment,
                string.Format(MachineLockTexts.HintContainmentPartialFormat, 2, 1),
                MachineLockTexts.HintExit
            };

            string[] forbiddenCardinals = { "nord", "sud", "est", "ovest" };
            foreach (string hint in hints)
            {
                Assert.That(hint.Length, Is.LessThanOrEqualTo(60), "Hint exceeds 60 chars: " + hint);
                string[] tokens = hint.ToLowerInvariant().Split(
                    new[] { ' ', '\'', '’', ',', '.', ':', ';', '!', '?', '-', '—', '·', '(', ')' },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (string cardinal in forbiddenCardinals)
                {
                    Assert.IsFalse(tokens.Contains(cardinal),
                        $"Hint '{hint}' must not contain cardinal direction '{cardinal}' (Issue #46).");
                }
            }

            string[] wristObjectives =
            {
                MachineLockTexts.WristEStop,
                MachineLockTexts.WristAlarm,
                MachineLockTexts.WristMainOff,
                MachineLockTexts.WristLockout,
                MachineLockTexts.WristTesterZero,
                MachineLockTexts.WristTryStart,
                string.Format(MachineLockTexts.WristPpeFormat, 3),
                MachineLockTexts.WristContainment,
                string.Format(MachineLockTexts.WristContainmentPartialFormat, 4, 4),
                MachineLockTexts.WristExit
            };
            foreach (string wrist in wristObjectives)
            {
                Assert.That(wrist.Length, Is.LessThanOrEqualTo(30),
                    $"Wrist objective '{wrist}' exceeds 30-char wrist line limit.");
            }
        }

        [Test]
        public void SequencePenalties_ApplyMinusFiveDecisionOnceEach_AndAllowFullExecutionCredit()
        {
            MachineLockSpi spi = StartedRun(SpiTimeMode.Guided, 60f);

            spi.ApplyEvent(SpiEvent.SequenceMainOffBeforeEStop);
            spi.ApplyEvent(SpiEvent.SequenceMainOffBeforeEStop); // Idempotent
            Assert.AreEqual(35f, spi.Decision, Tolerance);

            spi.ApplyEvent(SpiEvent.SequenceTryStartBeforeTesterZero);
            spi.ApplyEvent(SpiEvent.SequenceTryStartBeforeTesterZero); // Idempotent
            Assert.AreEqual(30f, spi.Decision, Tolerance);

            spi.ApplyEvent(SpiEvent.SequenceContainmentBeforeReady);
            spi.ApplyEvent(SpiEvent.SequenceContainmentBeforeReady); // Idempotent
            Assert.AreEqual(25f, spi.Decision, Tolerance);

            // Completing every task afterward still awards full Execution credit and avoids CriticalOccurred.
            CompleteAllTasks(spi);
            Assert.AreEqual(15, spi.Execution);
            Assert.IsFalse(spi.CriticalOccurred);
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.BadSequenceMainOffBeforeEStop.ToString()));
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.BadSequenceTryStartBeforeTesterZero.ToString()));
            Assert.AreEqual(1, CountLines(spi, DebriefLineId.BadSequenceContainmentBeforeReady.ToString()));
        }

        private static MachineLockPpeItem AddEditModeItem(GameObject parent, MachineLockPpeCabinet cabinet,
            PpeSlot slot, bool correct, string displayName)
        {
            var go = new GameObject("ITEM_" + displayName);
            go.transform.SetParent(parent.transform, false);
            var item = go.AddComponent<MachineLockPpeItem>();
            item.Configure(cabinet, slot, correct, displayName);
            return item;
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

        [Test]
        public void Config_EnergyDecayDefaults_Are15To30Seconds_AndRollWithinRange()
        {
            var cfg = UnityEngine.ScriptableObject.CreateInstance<MachineLockConfig>();
            try
            {
                Assert.That(cfg.energyDecayMinSeconds, Is.EqualTo(15f));
                Assert.That(cfg.energyDecayMaxSeconds, Is.EqualTo(30f));

                float minSeen = float.MaxValue;
                float maxSeen = float.MinValue;
                for (int i = 0; i < 40; i++)
                {
                    float rolled = cfg.RollEnergyDecaySeconds();
                    Assert.That(rolled, Is.InRange(15f, 30f));
                    if (rolled < minSeen) minSeen = rolled;
                    if (rolled > maxSeen) maxSeen = rolled;
                }
                Assert.That(maxSeen - minSeen, Is.GreaterThan(0.5f),
                    "RollEnergyDecaySeconds should produce varied values across runs.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cfg);
            }
        }

        private static int CountLinesWithPrefix(MachineLockSpi spi, string prefix)
        {
            return BuildLines(spi).Count(line => line.StartsWith(prefix, System.StringComparison.Ordinal));
        }
    }
}
