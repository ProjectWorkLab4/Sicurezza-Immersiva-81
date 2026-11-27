using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using NUnit.Framework;
using SI81.Input;
using SI81.Interaction;
using SI81.MachineLock;
using SI81.Rig;
using SI81.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SI81.MachineLock.Tests
{
    /// <summary>
    /// PlayMode integration and component tests for the Machine Lock scenario (issues #28-#34,
    /// phases P3-P6, P8 and P9). The suite builds clean GameObjects in Play Mode, so it needs no
    /// headset, no OpenXR session and no SCN_Core: the scenario is started through
    /// <see cref="MachineLockScenario.InitializeForTests"/>, which creates the SPI and the mission
    /// state without a HUD root or an OVR rig. One test loads the real SCN_MachineLock scene
    /// additively with an already active <see cref="MissionSession"/>, so the standalone-editor path
    /// that loads SCN_Core never runs.
    /// </summary>
    [TestFixture]
    public sealed class MachineLockFlowPlayModeTests
    {
        /// <summary>Real Machine Lock scene, loaded by the P9 scene verification test.</summary>
        private const string MachineLockScenePath =
            "Assets/_Project/Groups/G_Marius/Level/Scenes/SCN_MachineLock.unity";

        /// <summary>Mission asset used by the real scene, so the session and the scene agree.</summary>
        private const string MissionAssetPath =
            "Assets/_Project/Home/UI/Data/Missions/SO_Mission_MachineLock.asset";

        /// <summary>Tolerance of the float comparisons on SPI dimensions and transforms.</summary>
        private const float Tolerance = 0.001f;

        /// <summary>The six observation ids of GDD §9.11.</summary>
        private static readonly string[] ObservationIds =
        {
            "press", "thermometer", "display", "puddle", "panel", "ppe"
        };

        private readonly List<GameObject> roots = new List<GameObject>();
        private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

        private MachineLockConfig config;
        private MissionDefinition mission;

        /// <summary>
        /// Creates a fresh config and mission asset and starts a training session, so every test owns
        /// its SPI options and <see cref="MissionSession"/> state.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            MachineLockHoverFeedback.ResetGlobalCooldown();

            config = TrackAsset(ScriptableObject.CreateInstance<MachineLockConfig>());

            mission = TrackAsset(ScriptableObject.CreateInstance<MissionDefinition>());
            mission.Id = "TEST_MachineLock";
            mission.Title = "Machine Lock PlayMode test";
            mission.IsTraining = true;
            mission.Objectives.Clear();
            for (int i = 0; i < MachineLockScenario.MACRO_COUNT; i++)
                mission.Objectives.Add("Objective " + (i + 1));

            MissionSession.Clear();
            MissionSession.Begin(mission, DifficultyLevel.Guided, true);
        }

        /// <summary>
        /// Destroys every GameObject and ScriptableObject created by the test and clears the session,
        /// so no state leaks into the next test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            DestroyTrackedObjects();
            for (int i = assets.Count - 1; i >= 0; i--)
            {
                if (assets[i] != null) UnityEngine.Object.DestroyImmediate(assets[i]);
            }
            assets.Clear();
            UIServices.Clear();
            MissionSession.Clear();
        }

        // =====================================================================
        // P3 - Core interactions
        // =====================================================================

        /// <summary>
        /// The E-stop stops the ram where it is, keeps the press powered (the display stays on) and
        /// reports the task; opening the main circuit later turns the display dark.
        /// </summary>
        [UnityTest]
        public IEnumerator Press_EStopStopsRam_KeepsDisplayPowered()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            yield return null;

            Assert.That(press.Press.IsRunning, Is.True, "The press starts running.");
            Assert.That(press.Display.sharedMaterial, Is.SameAs(press.FaultMaterial),
                "The fault display is lit while the main circuit is closed.");

            yield return new WaitForSeconds(1.2f);
            float ramWhileRunning = press.Ram.localPosition.y;
            Assert.That(ramWhileRunning, Is.LessThan(0.99f), "The ram must move while the press runs.");

            press.Press.PressEStop();
            yield return null;

            Assert.That(press.Press.IsRunning, Is.False, "The E-stop stops the ram.");
            Assert.That(press.Press.IsPowered, Is.True, "The auxiliary circuit keeps the display powered.");
            Assert.That(press.Press.IsEStopPressed, Is.True);
            Assert.That(scenario.TaskEStop, Is.True);
            Assert.That(scenario.Spi.TaskEStop, Is.True);
            Assert.That(press.Display.sharedMaterial, Is.SameAs(press.FaultMaterial),
                "The display stays on after the E-stop: only the main switch darkens it.");

            Vector3 frozen = press.Ram.localPosition;
            yield return new WaitForSeconds(0.4f);
            Assert.That(Vector3.Distance(frozen, press.Ram.localPosition), Is.LessThan(0.0001f),
                "A stopped press must not move the ram.");

            press.Press.SetPower(false);
            yield return null;
            Assert.That(press.Press.IsPowered, Is.False);
            Assert.That(press.Display.sharedMaterial, Is.SameAs(press.OffMaterial),
                "Opening the main circuit turns the display dark.");
        }

        /// <summary>
        /// Opening the main switch stops and darkens the press, unlocks the lockout point, starts the
        /// residual charge decay and lets the lockout block any reclose.
        /// </summary>
        [UnityTest]
        public IEnumerator EnergyPanel_MainOffDarkensPress_UnlocksLockout_DecaysCharge()
        {
            config.energyDecayMinSeconds = 0.5f;
            config.energyDecayMaxSeconds = 0.5f;
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            yield return null;

            panel.Panel.InitializeState(true);
            Assert.That(panel.Panel.MainPowerOn, Is.True);
            Assert.That(panel.Panel.OilEnergized, Is.True);
            Assert.That(press.Press.IsPowered, Is.True);
            Assert.That(panel.Lockout.Selectable, Is.True, "The lockout point stays clickable while live so early clicks give negative feedback (Issue #46).");
            panel.Panel.ApplyLockout();
            Assert.That(panel.Panel.LockedOut, Is.False, "Applying lockout while main power is ON must stay blocked.");
            Assert.That(scenario.TaskLockout, Is.False);
            Assert.That(press.Display.sharedMaterial, Is.SameAs(press.FaultMaterial));

            panel.Panel.SetMainPower(false);

            Assert.That(panel.Panel.MainPowerOn, Is.False);
            Assert.That(press.Press.IsPowered, Is.False);
            Assert.That(press.Press.IsRunning, Is.False);
            Assert.That(scenario.TaskMainOff, Is.True);
            Assert.That(panel.Lockout.Selectable, Is.True, "Opening the main switch unlocks the lockout point.");
            Assert.That(press.Display.sharedMaterial, Is.SameAs(press.OffMaterial));

            float chargeBefore = panel.Panel.Charge01;
            yield return new WaitForSeconds(0.15f);
            Assert.That(panel.Panel.Charge01, Is.LessThan(chargeBefore), "The residual charge decays with the main switch open.");
            Assert.That(panel.Panel.Charge01, Is.GreaterThan(0f));

            panel.Panel.ApplyLockout();
            Assert.That(panel.Panel.LockedOut, Is.True);
            Assert.That(scenario.TaskLockout, Is.True);
            Assert.That(panel.Lockout.Selectable, Is.False, "The lockout point is consumed by a single lockout.");

            panel.Panel.SetMainPower(true);
            Assert.That(panel.Panel.MainPowerOn, Is.False, "A locked-out panel cannot be reclosed.");
            yield return null;
        }

        /// <summary>
        /// The voltage tester reads LIVE with the panel closed, LIVE with residual charge and ZERO only
        /// after the charge is gone; reading it never discharges the oil.
        /// </summary>
        [UnityTest]
        public IEnumerator EnergyPanel_TesterReadsAllThreeStates_WithoutDischarging()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            yield return null;

            panel.Panel.InitializeState(true);
            yield return null;

            float fullCharge = panel.Panel.Charge01;
            Assert.That(panel.Panel.ReadTester(), Is.EqualTo(MachineLockTexts.TesterReadLivePanel));
            Assert.That(panel.Panel.Charge01, Is.EqualTo(fullCharge), "Reading the tester must not discharge the oil.");
            Assert.That(panel.Panel.OilEnergized, Is.True);
            Assert.That(panel.Panel.TesterZero, Is.False);
            Assert.That(scenario.TaskTesterZero, Is.False);

            panel.Panel.SetMainPower(false);
            float residual = panel.Panel.Charge01;
            Assert.That(panel.Panel.ReadTester(), Is.EqualTo(MachineLockTexts.TesterReadLiveCharge));
            Assert.That(panel.Panel.Charge01, Is.EqualTo(residual), "A LIVE reading must not discharge the oil.");
            Assert.That(panel.Panel.OilEnergized, Is.True);
            Assert.That(scenario.TaskTesterZero, Is.False);

            panel.Panel.ForceZeroEnergy();
            Assert.That(panel.Panel.ReadTester(), Is.EqualTo(MachineLockTexts.TesterReadZero));
            Assert.That(panel.Panel.TesterZero, Is.True);
            Assert.That(panel.Panel.OilEnergized, Is.False);
            Assert.That(scenario.TaskTesterZero, Is.True);
            Assert.That(scenario.Spi.TaskTesterZero, Is.True);
            yield return null;
        }

        /// <summary>
        /// Diegetic Digital Meter starts powered OFF, powers ON continuously on select, reflects running
        /// vs E-stopped load while main power is ON, streams a curved residual voltage drop to 0 V after
        /// main power opens, auto-confirms TaskTesterZero without a second click, and stays ON on re-click (Issue #45).
        /// </summary>
        [UnityTest]
        public IEnumerator EnergyPanel_DigitalMeter_StartsOff_StreamsDrop_AndAutoConfirmsZero()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            yield return null;

            panel.Panel.InitializeState(true);
            panel.Panel.SetDecayDurationForTest(0.45f);
            yield return null;

            Assert.That(panel.Panel.TesterDisplayOn, Is.False, "Digital Meter must start powered OFF.");
            Assert.That(panel.Meter.IsPoweredOn, Is.False);

            // First click while main power is ON and press is running powers meter ON with active load.
            Assert.That(panel.Panel.ReadTester(), Is.EqualTo(MachineLockTexts.TesterReadLivePanel));
            Assert.That(panel.Panel.TesterDisplayOn, Is.True);
            Assert.That(panel.Meter.IsPoweredOn, Is.True);
            Assert.That(panel.Panel.DisplayedVoltage, Is.InRange(225f, 235f));
            Assert.That(panel.Panel.DisplayedCurrent, Is.GreaterThan(10f));
            Assert.That(panel.Panel.DisplayedPower, Is.GreaterThan(2000f));

            // E-Stop drops motor current to standby while mains voltage stays ~230 V.
            press.Press.PressEStop();
            yield return new WaitForSeconds(0.30f);
            Assert.That(panel.Panel.DisplayedVoltage, Is.InRange(225f, 235f));
            Assert.That(panel.Panel.DisplayedCurrent, Is.InRange(0.5f, 1.5f));
            Assert.That(panel.Panel.DisplayedPower, Is.InRange(100f, 350f));

            // Re-clicking while already ON keeps the meter ON (never toggles OFF).
            Assert.That(panel.Panel.ReadTester(), Is.EqualTo(MachineLockTexts.TesterReadLivePanel));
            Assert.That(panel.Panel.TesterDisplayOn, Is.True);
            Assert.That(panel.Meter.IsPoweredOn, Is.True);

            // Open Main Disconnect: current/power drop to 0 and voltage decays monotonically until auto-ZERO.
            panel.Panel.SetMainPower(false);
            float initialResidualV = panel.Panel.DisplayedVoltage;
            Assert.That(panel.Panel.DisplayedCurrent, Is.EqualTo(0f).Within(0.001f));
            Assert.That(panel.Panel.DisplayedPower, Is.EqualTo(0f).Within(0.001f));
            Assert.That(initialResidualV, Is.InRange(200f, 226f));
            Assert.That(panel.Panel.TesterZero, Is.False);

            yield return new WaitForSeconds(0.22f);
            float midResidualV = panel.Panel.DisplayedVoltage;
            Assert.That(midResidualV, Is.LessThan(initialResidualV - 30f),
                "Displayed voltage must visibly decrease while residual charge decays.");
            Assert.That(midResidualV, Is.GreaterThan(0f));

            // Wait until decay crosses ENERGIZED_THRESHOLD (0.05) -> auto-confirms ZERO without a second click.
            float deadline = Time.realtimeSinceStartup + 2.0f;
            while (!panel.Panel.TesterZero && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(panel.Panel.TesterZero, Is.True, "Active Digital Meter must auto-confirm TesterZero at safe zero.");
            Assert.That(scenario.TaskTesterZero, Is.True);
            Assert.That(scenario.Spi.TaskTesterZero, Is.True);
            Assert.That(panel.Panel.DisplayedVoltage, Is.EqualTo(0f).Within(0.001f));
            Assert.That(panel.Meter.FormattedVoltage, Is.EqualTo("0"));
            Assert.That(panel.Meter.FormattedCurrent, Is.EqualTo("0.00"));
            Assert.That(panel.Meter.FormattedPower, Is.EqualTo("0.0"));
        }

        /// <summary>
        /// In a 50% no-residual run, if the meter is already ON when the Main Disconnect opens, it settles
        /// smoothly to 0 V over ~0.5 s and then auto-confirms TaskTesterZero (Issue #45).
        /// </summary>
        [UnityTest]
        public IEnumerator EnergyPanel_DigitalMeter_NoResidualWhileAlreadyOn_SettlesAndAutoConfirmsZero()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            yield return null;

            panel.Panel.InitializeState(false);
            yield return null;

            Assert.That(panel.Panel.ReadTester(), Is.EqualTo(MachineLockTexts.TesterReadLivePanel));
            Assert.That(panel.Panel.TesterDisplayOn, Is.True);

            panel.Panel.SetMainPower(false);
            Assert.That(panel.Panel.TesterZero, Is.False, "No-residual settle should ramp briefly before confirming zero.");
            Assert.That(panel.Panel.DisplayedVoltage, Is.GreaterThan(100f));

            float deadline = Time.realtimeSinceStartup + 2.0f;
            while (!panel.Panel.TesterZero && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(panel.Panel.TesterZero, Is.True);
            Assert.That(scenario.TaskTesterZero, Is.True);
            Assert.That(panel.Panel.DisplayedVoltage, Is.EqualTo(0f).Within(0.001f));
            Assert.That(panel.Meter.FormattedVoltage, Is.EqualTo("0"));
        }

        /// <summary>
        /// Residual energy discharge duration rolls randomly within [15, 30] seconds per run (Issue #45).
        /// </summary>
        [UnityTest]
        public IEnumerator EnergyPanel_ResidualDecayDuration_RollsRandomlyWithin15To30Seconds()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PanelFixture panel = CreatePanel(scenario, null);
            yield return null;

            float minSeen = float.MaxValue;
            float maxSeen = float.MinValue;
            for (int i = 0; i < 20; i++)
            {
                panel.Panel.InitializeState(true);
                float rolled = panel.Panel.ActiveDecaySeconds;
                Assert.That(rolled, Is.InRange(15f, 30f));
                if (rolled < minSeen) minSeen = rolled;
                if (rolled > maxSeen) maxSeen = rolled;
            }
            Assert.That(maxSeen - minSeen, Is.GreaterThan(0.5f),
                "ActiveDecaySeconds must randomize across runs within [15, 30] seconds.");
        }

        /// <summary>
        /// Try-start with the main circuit still live jogs the ram and is a critical event; the same
        /// action on an isolated machine verifies the zero-energy isolation.
        /// </summary>
        [UnityTest]
        public IEnumerator Press_TryStartPoweredTriggersCritical_IsolatedVerifies()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            yield return null;

            press.Press.TryStart();

            Assert.That(scenario.Spi.CriticalOccurred, Is.True);
            Assert.That(scenario.Spi.Safety,
                Is.EqualTo((float)(MachineLockSpi.SAFETY_MAX - config.tryStartSafetyPenalty)).Within(Tolerance));
            Assert.That(CountDebrief(scenario, DebriefLineId.CriticalTryStart), Is.EqualTo(1));
            Assert.That(scenario.TaskTryStart, Is.False, "A machine that restarts is not isolated.");
            Assert.That(press.Press.IsRunning, Is.True);

            press.Press.SetPower(false);
            yield return null;
            Assert.That(press.Press.IsPowered, Is.False);

            press.Press.TryStart();

            Assert.That(scenario.TaskTryStart, Is.True);
            Assert.That(scenario.Spi.TaskTryStart, Is.True);
            Assert.That(scenario.Spi.TaskTesterZero, Is.False, "Try-start alone does not confirm zero energy.");
            Assert.That(CountDebrief(scenario, DebriefLineId.CriticalTryStart), Is.EqualTo(1),
                "The critical event is recorded once.");
            yield return null;
        }

        /// <summary>
        /// The department alarm raises the beacon and notifies the scenario exactly once.
        /// </summary>
        [UnityTest]
        public IEnumerator Alarm_RaisesBeaconAndNotifiesScenario()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            AlarmFixture alarm = CreateAlarm(scenario);
            yield return null;

            Assert.That(alarm.Alarm.Raised, Is.False);
            Assert.That(alarm.Beacon.enabled, Is.False);
            Assert.That(scenario.TaskAlarm, Is.False);

            alarm.Alarm.Raise();

            Assert.That(alarm.Alarm.Raised, Is.True);
            Assert.That(alarm.Beacon.enabled, Is.True);
            Assert.That(alarm.Beacon.intensity, Is.GreaterThan(0f));
            Assert.That(scenario.TaskAlarm, Is.True);
            Assert.That(scenario.Spi.TaskAlarm, Is.True);
            Assert.That(scenario.Spi.Execution, Is.EqualTo(config.executionAlarm));

            alarm.Alarm.Raise();
            Assert.That(scenario.Spi.Execution, Is.EqualTo(config.executionAlarm), "The alarm reward is paid once.");
            yield return null;
        }

        /// <summary>
        /// The water extinguisher on the burning oil is the water critical event, scored once inside
        /// the critical cooldown.
        /// </summary>
        [UnityTest]
        public IEnumerator Extinguisher_TriggersWaterCritical()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            MachineLockExtinguisher extinguisher = CreateExtinguisher(scenario);
            yield return null;

            extinguisher.TriggerSpray();

            Assert.That(scenario.Spi.CriticalOccurred, Is.True);
            Assert.That(scenario.Spi.Safety,
                Is.EqualTo((float)(MachineLockSpi.SAFETY_MAX - config.waterSafetyPenalty)).Within(Tolerance));
            Assert.That(CountDebrief(scenario, DebriefLineId.CriticalWater), Is.EqualTo(1));

            extinguisher.TriggerSpray();
            Assert.That(CountDebrief(scenario, DebriefLineId.CriticalWater), Is.EqualTo(1),
                "A second spray inside the critical cooldown is ignored.");
            yield return null;
        }

        /// <summary>
        /// Inaction escalates the smoke at 90 s and 150 s and ends with Bad Ending 2 at 240 s; the
        /// E-stop cancels the timer for good.
        /// </summary>
        [UnityTest]
        public IEnumerator Inaction_EscalatesSmokeAndTriggersBadEnding2At240s_CancelledByEStop()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            yield return null;

            Assert.That(press.Press.IsRunning, Is.True, "The inaction timer runs while the press works.");

            scenario.TickInactionForTests(89f);
            Assert.That(SmokeStage(scenario), Is.EqualTo(0), "No smoke before 90 s.");
            Assert.That(scenario.HasEnded, Is.False);

            scenario.TickInactionForTests(2f);
            Assert.That(SmokeStage(scenario), Is.GreaterThanOrEqualTo(1), "Smoke stage 1 at 90 s.");

            scenario.TickInactionForTests(60f);
            Assert.That(SmokeStage(scenario), Is.GreaterThanOrEqualTo(2), "Smoke stage 2 at 150 s.");
            Assert.That(scenario.HasEnded, Is.False);

            scenario.TickInactionForTests(100f);
            Assert.That(scenario.HasEnded, Is.True, "Bad Ending 2 at 240 s of press running.");
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Negative));
            Assert.That(scenario.Spi.CriticalOccurred, Is.True);
            Assert.That(CountDebrief(scenario, DebriefLineId.CriticalInaction), Is.EqualTo(1));
            yield return null;

            MachineLockScenario stopped = CreateScenario(DifficultyLevel.Guided);
            PressFixture stoppedPress = CreatePress(stopped);
            yield return null;
            stoppedPress.Press.PressEStop();
            int stageAfterStop = SmokeStage(stopped);

            stopped.TickInactionForTests(400f);

            Assert.That(stopped.HasEnded, Is.False, "The E-stop cancels the inaction timer.");
            Assert.That(SmokeStage(stopped), Is.EqualTo(stageAfterStop));
            yield return null;
        }

        // =====================================================================
        // P4 - PPE
        // =====================================================================

        /// <summary>
        /// The closed cabinet blocks equipping, the open one enables it, and a second item of the same
        /// slot replaces the first one, which returns to the shelf.
        /// </summary>
        [UnityTest]
        public IEnumerator PpeCabinet_ClosedBlocksEquip_OpenEnables_ReplacesSlot()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            CabinetFixture cabinet = CreateCabinet(scenario, true);
            yield return null;

            Assert.That(cabinet.Cabinet.IsOpen, Is.False);
            Assert.That(cabinet.Cabinet.Equip(cabinet.HeatGloves.Item), Is.False, "Items cannot be equipped while the door is shut.");
            Assert.That(cabinet.HeatGloves.Item.IsEquipped, Is.False);
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(0));

            cabinet.Cabinet.Open();

            Assert.That(cabinet.Cabinet.IsOpen, Is.True);
            Assert.That(cabinet.HeatGloves.Interactable.Selectable, Is.True);
            Assert.That(cabinet.Cabinet.Equip(cabinet.HeatGloves.Item), Is.True);
            Assert.That(cabinet.HeatGloves.Item.IsEquipped, Is.True);
            Assert.That(cabinet.HeatGloves.Interactable.Selectable, Is.False);
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(1));
            Assert.That(scenario.CorrectPpeCount, Is.EqualTo(1));

            Assert.That(cabinet.Cabinet.Equip(cabinet.LatexGloves.Item), Is.True);
            Assert.That(cabinet.HeatGloves.Item.IsEquipped, Is.False, "The replaced item returns to the shelf.");
            Assert.That(cabinet.HeatGloves.Interactable.Selectable, Is.True);
            Assert.That(cabinet.LatexGloves.Item.IsEquipped, Is.True);
            Assert.That(cabinet.Cabinet.GetEquipped(PpeSlot.Gloves), Is.SameAs(cabinet.LatexGloves.Item));
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(0), "A wrong item fills the slot without protecting.");
            Assert.That(cabinet.Cabinet.HasWrongPpeEquipped, Is.True);

            Assert.That(cabinet.Cabinet.Equip(cabinet.HeatGloves.Item), Is.True);
            Assert.That(cabinet.LatexGloves.Item.IsEquipped, Is.False);
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(1));
            Assert.That(cabinet.Cabinet.HasWrongPpeEquipped, Is.False);
            yield return null;
        }

        /// <summary>
        /// Wrong items never protect; the four correct items complete the PPE task and pay the reward
        /// once, even after cycling a slot back to 3/4 and to 4/4 again.
        /// </summary>
        [UnityTest]
        public IEnumerator PpeCabinet_WrongItemsDoNotProtect_FourCorrectGrantFullPpe()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            CabinetFixture cabinet = CreateCabinet(scenario, true);
            yield return null;

            cabinet.Cabinet.Open();

            Assert.That(cabinet.Cabinet.Equip(cabinet.SurgicalMask.Item), Is.True);
            Assert.That(cabinet.Cabinet.HasWrongPpeEquipped, Is.True);
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(0));
            Assert.That(cabinet.Cabinet.HasFullPpe, Is.False);
            Assert.That(scenario.TaskPpeFull, Is.False);

            for (int i = 0; i < cabinet.CorrectItems.Length; i++)
                Assert.That(cabinet.Cabinet.Equip(cabinet.CorrectItems[i].Item), Is.True,
                    "Equipping " + cabinet.CorrectItems[i].Item.DisplayName);

            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(4));
            Assert.That(cabinet.Cabinet.HasFullPpe, Is.True);
            Assert.That(cabinet.Cabinet.HasWrongPpeEquipped, Is.False, "The correct face shield replaced the surgical mask.");
            Assert.That(scenario.TaskPpeFull, Is.True);
            Assert.That(scenario.CorrectPpeCount, Is.EqualTo(4));
            Assert.That(scenario.Spi.TaskPpeFull, Is.True);
            Assert.That(scenario.Spi.Execution, Is.EqualTo(config.executionPpe));
            Assert.That(HasDebrief(scenario, DebriefLineId.GoodPpe), Is.True);

            Assert.That(cabinet.Cabinet.Equip(cabinet.LatexGloves.Item), Is.True);
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(3));
            Assert.That(cabinet.Cabinet.HasFullPpe, Is.False);
            Assert.That(scenario.TaskPpeFull, Is.False, "Removing a correct item breaks the full set.");

            Assert.That(cabinet.Cabinet.Equip(cabinet.HeatGloves.Item), Is.True);
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(4));
            Assert.That(scenario.TaskPpeFull, Is.True);
            Assert.That(scenario.Spi.Execution, Is.EqualTo(config.executionPpe),
                "Cycling the PPE set must not pay the reward twice.");
            yield return null;
        }

        // =====================================================================
        // P5 - Spill containment
        // =====================================================================

        /// <summary>
        /// Closing the valve turns the wheel, stops the jet, records the debrief line once and stays
        /// closed on further interactions.
        /// </summary>
        [UnityTest]
        public IEnumerator Valve_ClosesOnce_StopsJetAndRecordsDebrief()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            SpillFixture spill = CreateSpill(scenario, false);
            ValveFixture valve = CreateValve(scenario, spill.Spill);
            yield return null;

            Assert.That(spill.Jet.activeSelf, Is.True);
            Assert.That(HasDebrief(scenario, DebriefLineId.GoodValveClosed), Is.False);
            Assert.That(MachineLockTexts.DebriefLine(DebriefLineId.GoodValveClosed), Is.Not.Empty,
                "DebriefLineId.GoodValveClosed needs its Italian debrief text.");

            valve.Valve.Close();

            Assert.That(valve.Valve.IsClosed, Is.True);
            Assert.That(spill.Spill.ValveClosed, Is.True);
            Assert.That(spill.Jet.activeSelf, Is.False, "Closing the valve stops the leak jet.");
            Assert.That(scenario.ValveClosed, Is.True);
            Assert.That(Quaternion.Angle(Quaternion.identity, valve.Wheel.localRotation), Is.EqualTo(90f).Within(0.5f));
            Assert.That(CountDebrief(scenario, DebriefLineId.GoodValveClosed), Is.EqualTo(1));

            valve.Valve.Close();

            Assert.That(CountDebrief(scenario, DebriefLineId.GoodValveClosed), Is.EqualTo(1),
                "A second close must not duplicate the debrief line.");
            yield return null;
        }

        /// <summary>
        /// Items snap inside the snap radius and stay grabbable outside it; four cones plus four
        /// absorbents disable the puddle hazard and complete the containment task.
        /// </summary>
        [UnityTest]
        public IEnumerator ConesAndAbsorbents_SnapInsideRadius_StayGrabbableOutside_FourPlusFourDisablesHazard()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            SpillFixture spill = CreateSpill(scenario, true);
            yield return null;

            // Outside every zone the item stays where it was released and remains grabbable.
            ConeFixture farCone = CreateCone(scenario, spill.Spill, new Vector3(0f, 0f, 20f));
            yield return null;

            Assert.That(farCone.Cone.TrySnap(), Is.False);
            Assert.That(farCone.Cone.Snapped, Is.False);
            Assert.That(farCone.Interactable.Grabbable, Is.True, "A cone that did not snap must stay grabbable.");

            // The first cone is released 0.4 m off centre: still inside the 0.6 m snap radius.
            spill.Cones[0].Cone.transform.position = spill.ConeZones[0].SnapPoint.position + new Vector3(0.4f, 0f, 0f);
            for (int i = 0; i < spill.Cones.Length; i++)
            {
                ConeFixture cone = spill.Cones[i];
                Assert.That(cone.Cone.TrySnap(), Is.True, "Cone " + (i + 1) + " should snap inside the radius.");
                Assert.That(cone.Cone.Snapped, Is.True);
                Assert.That(cone.Interactable.Grabbable, Is.False, "A placed cone cannot be grabbed again.");
                Assert.That(spill.ConeZones[i].Occupied, Is.True);
                Assert.That(spill.Spill.ConesPlaced, Is.EqualTo(i + 1));
            }

            Assert.That(scenario.ConesPlaced, Is.EqualTo(4));
            Assert.That(scenario.TaskContainment, Is.False, "Cones alone do not contain the spill.");
            Assert.That(spill.Spill.ContainmentComplete, Is.False);

            for (int i = 0; i < spill.Absorbents.Length; i++)
            {
                AbsorbentFixture absorbent = spill.Absorbents[i];
                Assert.That(absorbent.Absorbent.TrySnap(), Is.True, "Absorbent " + (i + 1) + " should snap.");
                Assert.That(absorbent.Interactable.Grabbable, Is.False);
                Assert.That(spill.AbsorbentZones[i].Occupied, Is.True);
                Assert.That(spill.Spill.AbsorbentsPlaced, Is.EqualTo(i + 1));
            }

            Assert.That(spill.Spill.ContainmentComplete, Is.True);
            Assert.That(spill.HazardTrigger.enabled, Is.False, "Four absorbents remove the puddle hazard.");
            Assert.That(scenario.TaskContainment, Is.True);
            Assert.That(scenario.AbsorbentsPlaced, Is.EqualTo(4));
            Assert.That(scenario.Spi.TaskContainment, Is.True);
            Assert.That(HasDebrief(scenario, DebriefLineId.GoodContainment), Is.True);

            // A contained puddle no longer reports a contact.
            spill.Spill.NotifyPuddleContact();
            Assert.That(scenario.Spi.CriticalOccurred, Is.False);

            // Snapping again is idempotent and does not inflate the counters.
            Assert.That(spill.Cones[0].Cone.TrySnap(), Is.True);
            Assert.That(spill.Spill.ConesPlaced, Is.EqualTo(4));
            yield return null;
        }

        /// <summary>
        /// In Guided the free zones show their ghost while an item of that kind is held (with the
        /// two-hand count), hide it on release and never show it outside Guided.
        /// </summary>
        [UnityTest]
        public IEnumerator SpillSystem_GuidedGhostsShowWhileHeld_HideOnRelease()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            GhostFixture ghosts = CreateGhostZones(scenario);
            yield return null;

            Assert.That(ghosts.ConeGhosts[0].activeSelf, Is.False);
            Assert.That(ghosts.ConeGhosts[1].activeSelf, Is.False);

            ghosts.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, true);

            Assert.That(ghosts.ConeGhosts[0].activeSelf, Is.True, "Guided shows the ghost of a free cone zone.");
            Assert.That(ghosts.ConeGhosts[1].activeSelf, Is.True);
            Assert.That(ghosts.AbsorbentGhost.activeSelf, Is.False, "Only zones of the held kind show the ghost.");

            ghosts.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, false);

            Assert.That(ghosts.ConeGhosts[0].activeSelf, Is.False);
            Assert.That(ghosts.ConeGhosts[1].activeSelf, Is.False);

            // Two hands: releasing one cone keeps the hint of the cone still held.
            ghosts.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, true);
            ghosts.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, true);
            ghosts.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, false);
            Assert.That(ghosts.ConeGhosts[0].activeSelf, Is.True);
            ghosts.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, false);
            Assert.That(ghosts.ConeGhosts[0].activeSelf, Is.False);

            // Outside Guided the ghosts stay hidden.
            MissionSession.Begin(mission, DifficultyLevel.Operational, true);
            ghosts.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Absorbent, true);
            Assert.That(ghosts.AbsorbentGhost.activeSelf, Is.False, "Ghosts are a Guided hint only.");
            ghosts.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Absorbent, false);
            yield return null;
        }

        /// <summary>
        /// Puddle contact resolves the most severe hazard once: live oil (even with wrong PPE), then
        /// wrong PPE, then hot oil without protection; the full PPE set removes the burn.
        /// </summary>
        [UnityTest]
        public IEnumerator SpillSystem_HazardPriority_ShockOverWrongPpeOverHotOil()
        {
            HazardFixture shock = CreateHazardRig(HazardPpe.Wrong, true);
            yield return null;
            shock.Spill.NotifyPuddleContact();
            Assert.That(HasDebrief(shock.Scenario, DebriefLineId.CriticalShock), Is.True,
                "Live oil beats the wrong PPE severity.");
            Assert.That(HasDebrief(shock.Scenario, DebriefLineId.CriticalWrongPpe), Is.False);
            Assert.That(shock.Scenario.Spi.Safety,
                Is.EqualTo((float)(MachineLockSpi.SAFETY_MAX - config.criticalSafetyPenalty)).Within(Tolerance));
            DestroyTrackedObjects();

            HazardFixture wrongPpe = CreateHazardRig(HazardPpe.Wrong, false);
            yield return null;
            wrongPpe.Spill.NotifyPuddleContact();
            Assert.That(HasDebrief(wrongPpe.Scenario, DebriefLineId.CriticalWrongPpe), Is.True);
            Assert.That(HasDebrief(wrongPpe.Scenario, DebriefLineId.CriticalHotOil), Is.False);
            DestroyTrackedObjects();

            HazardFixture noPpe = CreateHazardRig(HazardPpe.None, false);
            yield return null;
            noPpe.Spill.NotifyPuddleContact();
            Assert.That(HasDebrief(noPpe.Scenario, DebriefLineId.CriticalHotOil), Is.True);
            DestroyTrackedObjects();

            HazardFixture fullPpe = CreateHazardRig(HazardPpe.Full, false);
            yield return null;
            fullPpe.Spill.NotifyPuddleContact();
            Assert.That(fullPpe.Scenario.Spi.CriticalOccurred, Is.False,
                "The full correct PPE set protects from the hot oil.");
            Assert.That(fullPpe.Scenario.Spi.Safety, Is.EqualTo((float)MachineLockSpi.SAFETY_MAX).Within(Tolerance));
            yield return null;
        }

        // =====================================================================
        // P6 - Observation and awareness
        // =====================================================================

        /// <summary>
        /// The gaze rule needs distance and angle inside the window for the full dwell, scores the
        /// target once and resets the dwell on a miss.
        /// </summary>
        [UnityTest]
        public IEnumerator ObservationTarget_GazeDwellAndMiss_AwardsAwarenessOnce()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            MachineLockObservationTarget target = CreateObservationTarget("press", new Vector3(0f, 0f, 0f));
            yield return null;

            Vector3 eye = new Vector3(0f, 0f, 2f);
            Vector3 forward = Vector3.back;

            Assert.That(target.TryObserve(eye, forward, ObservationMode.Gaze, 3.5f, 25f, 1.2f, 1.5f, 0.5f, 0.6f),
                Is.False, "Half of the dwell is not enough.");
            Assert.That(target.CurrentDwellSeconds, Is.EqualTo(0.6f).Within(Tolerance));
            Assert.That(target.TryObserve(eye, forward, ObservationMode.Gaze, 3.5f, 25f, 1.2f, 1.5f, 0.5f, 0.6f),
                Is.True, "The dwell completes the observation.");
            Assert.That(target.Observed, Is.True);
            Assert.That(scenario.ObservedTargets, Is.EqualTo(1));
            Assert.That(scenario.Spi.Awareness, Is.EqualTo(20f / 6f).Within(0.01f));

            Assert.That(target.TryObserve(eye, forward, ObservationMode.Gaze, 3.5f, 25f, 1.2f, 1.5f, 0.5f, 5f),
                Is.False, "A target scores once.");
            Assert.That(scenario.ObservedTargets, Is.EqualTo(1));

            MachineLockObservationTarget missed = CreateObservationTarget("thermometer", new Vector3(1f, 0f, 0f));
            Vector3 missEye = new Vector3(1f, 0f, 2f);

            // 180 degrees away: no dwell.
            Assert.That(missed.TryObserve(missEye, Vector3.forward, ObservationMode.Gaze, 3.5f, 25f, 1.2f, 1.5f, 0.5f, 0.5f),
                Is.False);
            Assert.That(missed.CurrentDwellSeconds, Is.EqualTo(0f).Within(Tolerance));

            // 40 degrees off the target: outside the 25 degree cone, the dwell resets every frame.
            Vector3 angled = Quaternion.Euler(0f, 40f, 0f) * Vector3.back;
            for (int i = 0; i < 4; i++)
                Assert.That(missed.TryObserve(missEye, angled, ObservationMode.Gaze, 3.5f, 25f, 1.2f, 1.5f, 0.5f, 0.5f),
                    Is.False);
            Assert.That(missed.CurrentDwellSeconds, Is.EqualTo(0f).Within(Tolerance));

            // Too far: no dwell either.
            for (int i = 0; i < 4; i++)
                Assert.That(missed.TryObserve(new Vector3(1f, 0f, 6f), Vector3.back, ObservationMode.Gaze, 3.5f, 25f, 1.2f, 1.5f, 0.5f, 0.5f),
                    Is.False);
            Assert.That(missed.Observed, Is.False);
            Assert.That(scenario.ObservedTargets, Is.EqualTo(1));
            yield return null;
        }

        /// <summary>
        /// The proximity fallback needs only the distance and dwell, scores once and ignores a head
        /// outside the proximity radius.
        /// </summary>
        [UnityTest]
        public IEnumerator ObservationTarget_ProximityFallback_AwardsAwarenessOnce()
        {
            config.observationMode = ObservationMode.Proximity;
            config.proximityDistance = 1.5f;
            config.proximityDwell = 0.5f;

            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            MachineLockObservationTarget target = CreateObservationTarget("puddle", Vector3.zero);

            GameObject headObject = new GameObject("TEST_Head");
            roots.Add(headObject);
            headObject.transform.position = new Vector3(0f, 0f, 1f);
            yield return null;

            Assert.That(target.TryObserve(headObject.transform, 0.3f), Is.False);
            Assert.That(target.TryObserve(headObject.transform, 0.3f), Is.True,
                "0.6 s of proximity completes the 0.5 s dwell.");
            Assert.That(target.Observed, Is.True);
            Assert.That(scenario.ObservedTargets, Is.EqualTo(1));
            Assert.That(scenario.Spi.Awareness, Is.EqualTo(20f / 6f).Within(0.01f));

            Assert.That(target.TryObserve(headObject.transform, 1f), Is.False, "A target scores once.");
            Assert.That(scenario.ObservedTargets, Is.EqualTo(1));

            MachineLockObservationTarget farTarget = CreateObservationTarget("panel", new Vector3(4f, 0f, 0f));
            headObject.transform.position = new Vector3(0f, 0f, 3f);
            for (int i = 0; i < 4; i++)
                Assert.That(farTarget.TryObserve(headObject.transform, 0.5f), Is.False,
                    "Outside the proximity radius nothing accumulates.");
            Assert.That(farTarget.Observed, Is.False);
            Assert.That(scenario.ObservedTargets, Is.EqualTo(1));
            yield return null;
        }

        // =====================================================================
        // P8 - Damage feedback
        // =====================================================================

        /// <summary>
        /// The damage feedback drives the URP vignette weight when the volume is usable, falls back to
        /// the red light when forced, and <see cref="MachineLockDamageFeedback.Clear"/> resets both paths.
        /// </summary>
        [UnityTest]
        public IEnumerator DamageFeedback_VolumePathAndFallbackPath_ClearResetsState()
        {
            // Volume path.
            GameObject volumeRoot = NewRoot("TEST_DamageVolume");
            Volume volume = volumeRoot.AddComponent<Volume>();
            volume.isGlobal = true;
            VolumeProfile profile = TrackAsset(ScriptableObject.CreateInstance<VolumeProfile>());
            profile.Add<Vignette>();
            volume.profile = profile;
            volume.weight = 0f;

            GameObject volumeLightObject = new GameObject("LIGHT_VolumeFallback");
            volumeLightObject.transform.SetParent(volumeRoot.transform, false);
            Light volumeLight = volumeLightObject.AddComponent<Light>();

            MachineLockDamageFeedback volumeFeedback = volumeRoot.AddComponent<MachineLockDamageFeedback>();
            WireObject(volumeFeedback, "damageVolume", volume);
            WireObject(volumeFeedback, "fallbackFlashLight", volumeLight);
            WireBool(volumeFeedback, "forceFallbackForTesting", false);
            volumeRoot.SetActive(true);
            yield return null;

            volumeFeedback.PlayCritical(CriticalEvent.HotOilBurn);
            yield return null;
            yield return null;

            Assert.That(volume.weight, Is.GreaterThan(0f), "The volume path must animate the Vignette weight.");
            Assert.That(volumeLight.intensity, Is.EqualTo(0f).Within(Tolerance),
                "The fallback light stays dark while the volume renders.");

            volumeFeedback.Clear();
            Assert.That(volume.weight, Is.EqualTo(0f).Within(Tolerance), "Clear resets the damage volume weight.");
            Assert.That(volumeLight.intensity, Is.EqualTo(0f).Within(Tolerance));

            // Fallback path.
            GameObject fallbackRoot = NewRoot("TEST_DamageFallback");
            GameObject fallbackLightObject = new GameObject("LIGHT_FallbackFlash");
            fallbackLightObject.transform.SetParent(fallbackRoot.transform, false);
            Light fallbackLight = fallbackLightObject.AddComponent<Light>();

            MachineLockDamageFeedback fallbackFeedback = fallbackRoot.AddComponent<MachineLockDamageFeedback>();
            WireObject(fallbackFeedback, "fallbackFlashLight", fallbackLight);
            WireBool(fallbackFeedback, "forceFallbackForTesting", true);
            fallbackRoot.SetActive(true);
            yield return null;

            fallbackFeedback.PlayCritical(CriticalEvent.LiveOilShock);
            yield return null;
            yield return null;

            Assert.That(fallbackLight.intensity, Is.GreaterThan(0f),
                "Without a usable volume the red light flashes instead.");

            fallbackFeedback.Clear();
            Assert.That(fallbackLight.intensity, Is.EqualTo(0f).Within(Tolerance),
                "Clear resets the fallback light.");
            yield return null;
        }

        // =====================================================================
        // Issue #35 - Collision safety, ergonomics, wayfinding, hover & overlay
        // =====================================================================

        /// <summary>
        /// Cones and absorbents ignore physical collisions with the player's CharacterController so
        /// moving them underneath the player never displaces or no-clips the rig, while Physics.Raycast
        /// still hits their non-trigger colliders for RayInteractor grabbing.
        /// </summary>
        [UnityTest]
        public IEnumerator Grabbables_IgnorePlayerCharacterControllerCollision_KeepRaycastHit()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "TEST_Floor";
            roots.Add(floor);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);

            GameObject player = new GameObject("TEST_PlayerRig");
            roots.Add(player);
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            GameObject coneObj = new GameObject("TEST_Cone");
            roots.Add(coneObj);
            coneObj.transform.position = new Vector3(0f, 0.25f, 2.5f);
            BoxCollider coneCollider = coneObj.AddComponent<BoxCollider>();
            coneCollider.size = new Vector3(0.3f, 0.5f, 0.3f);
            coneObj.AddComponent<Interactable>();
            MachineLockCone cone = coneObj.AddComponent<MachineLockCone>();

            GameObject matObj = new GameObject("TEST_AbsorbentMat");
            roots.Add(matObj);
            matObj.transform.position = new Vector3(1.5f, 0.1f, 2.5f);
            BoxCollider matCollider = matObj.AddComponent<BoxCollider>();
            matCollider.size = new Vector3(1.0f, 0.1f, 0.8f);
            matObj.AddComponent<Interactable>();
            MachineLockAbsorbent mat = matObj.AddComponent<MachineLockAbsorbent>();

            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(coneCollider.isTrigger, Is.False, "Cone collider must stay non-trigger for RayInteractor.");
            Assert.That(matCollider.isTrigger, Is.False, "Absorbent collider must stay non-trigger for RayInteractor.");
            Assert.That(Physics.GetIgnoreCollision(coneCollider, controller), Is.True,
                "Cone collider must ignore CharacterController collisions.");
            Assert.That(Physics.GetIgnoreCollision(matCollider, controller), Is.True,
                "Absorbent collider must ignore CharacterController collisions.");

            // RayInteractor uses Physics.Raycast(..., QueryTriggerInteraction.Ignore): both must still hit.
            bool coneHit = Physics.Raycast(new Vector3(0f, 0.25f, 4.5f), Vector3.back, out RaycastHit coneHitInfo,
                5f, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(coneHit && coneHitInfo.collider == coneCollider, Is.True,
                "Raycast must still hit the cone for grabbing.");

            bool matHit = Physics.Raycast(new Vector3(1.5f, 0.1f, 4.5f), Vector3.back, out RaycastHit matHitInfo,
                5f, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(matHit && matHitInfo.collider == matCollider, Is.True,
                "Raycast must still hit the absorbent for grabbing.");

            // Move the cone directly underneath the player's feet and step the CharacterController: no climb/no-clip.
            controller.Move(new Vector3(0f, -0.5f, 0f));
            yield return new WaitForFixedUpdate();
            float groundedY = player.transform.position.y;

            coneObj.transform.position = new Vector3(player.transform.position.x, groundedY + 0.15f, player.transform.position.z);
            yield return new WaitForFixedUpdate();
            controller.Move(new Vector3(0.05f, -0.2f, 0.05f));
            yield return new WaitForFixedUpdate();

            Assert.That(player.transform.position.y, Is.EqualTo(groundedY).Within(0.05f),
                "Moving a cone underneath the player must never lift or no-clip the CharacterController.");
        }

        /// <summary>
        /// Opening the PPE cabinet disables its door collider(s) immediately so the doors can never
        /// obstruct or trap the player's locomotion.
        /// </summary>
        [UnityTest]
        public IEnumerator PpeCabinet_OpenDisablesDoorColliders_PreventsTrapping()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            CabinetFixture cabinet = CreateCabinet(scenario, true);
            yield return null;

            Assert.That(cabinet.DoorCollider, Is.Not.Null);
            Assert.That(cabinet.DoorCollider.enabled, Is.True, "Door collider starts enabled while closed.");

            cabinet.Cabinet.Open();
            yield return null;

            Assert.That(cabinet.Cabinet.IsOpen, Is.True);
            Assert.That(cabinet.DoorCollider.enabled, Is.False,
                "Opening the PPE cabinet must disable its door collider(s) so locomotion is never blocked.");
        }

        /// <summary>
        /// Hovering a PPE item emits its Italian name and slot; equipping it flips hover feedback and
        /// shelf status to "&lt;Nome DPI&gt; indossato"; equipping Heat Gloves and Face Shield replaces
        /// Latex Gloves and Surgical Mask and clears HasWrongPpeEquipped with Safety Boots completing 4/4.
        /// </summary>
        [UnityTest]
        public IEnumerator PpeCabinet_HoverShowsNameAndSlot_EquipFlipsToIndossato_AndClearsWrongPpe()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            CabinetFixture cabinet = CreateCabinet(scenario, true);
            yield return null;

            cabinet.Cabinet.Open();

            // Brief ray graze (< dwell) followed by HoverExit must NOT emit spam feedback.
            InvokeInteractableHover(cabinet.SafetyBoots.Interactable, Hand.Right);
            yield return new WaitForSecondsRealtime(0.15f);
            InvokeInteractableHoverExit(cabinet.SafetyBoots.Interactable, Hand.Right);
            Assert.That(cabinet.SafetyBoots.Item.LastHoverFeedback, Is.Empty,
                "Brief hover graze below dwell threshold must not trigger ActionFeedback spam.");

            // Deliberate continuous dwell (>= 0.55 s) reports Italian name and slot.
            InvokeInteractableHover(cabinet.SafetyBoots.Interactable, Hand.Right);
            yield return new WaitForSecondsRealtime(MachineLockHoverFeedback.DEFAULT_DWELL_SECONDS + 0.08f);
            Assert.That(cabinet.SafetyBoots.Item.LastHoverFeedback, Does.Contain("Scarpe antinfortunistiche S3"));
            Assert.That(cabinet.SafetyBoots.Item.LastHoverFeedback, Does.Contain(MachineLockTexts.PpeSlotFeet));
            Assert.That(cabinet.SafetyBoots.Item.IsEquippedShelfStatusVisible, Is.False);

            // Global cooldown blocks immediate cross-item hover spam right after an emission.
            InvokeInteractableHover(cabinet.HeatGloves.Interactable, Hand.Right);
            yield return new WaitForSecondsRealtime(MachineLockHoverFeedback.DEFAULT_DWELL_SECONDS + 0.08f);
            Assert.That(cabinet.HeatGloves.Item.LastHoverFeedback, Is.Empty,
                "Global hover cooldown must suppress rapid cross-item hover toasts.");

            // Equip both distractors first.
            Assert.That(cabinet.Cabinet.Equip(cabinet.LatexGloves.Item), Is.True);
            Assert.That(cabinet.Cabinet.Equip(cabinet.SurgicalMask.Item), Is.True);
            Assert.That(cabinet.Cabinet.HasWrongPpeEquipped, Is.True);
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(0));

            // Replace distractors with Heat Gloves (Gloves) and Face Shield (Face), then equip Apron + Boots.
            Assert.That(cabinet.Cabinet.Equip(cabinet.HeatGloves.Item), Is.True);
            Assert.That(cabinet.Cabinet.Equip(cabinet.FaceShield.Item), Is.True);
            Assert.That(cabinet.LatexGloves.Item.IsEquipped, Is.False);
            Assert.That(cabinet.SurgicalMask.Item.IsEquipped, Is.False);
            Assert.That(cabinet.Cabinet.HasWrongPpeEquipped, Is.False,
                "Equipping Heat Gloves and Face Shield must clear HasWrongPpeEquipped.");

            Assert.That(cabinet.Cabinet.Equip(cabinet.CorrectItems[1].Item), Is.True); // Apron
            Assert.That(cabinet.Cabinet.Equip(cabinet.SafetyBoots.Item), Is.True);
            Assert.That(cabinet.Cabinet.HasFullPpe, Is.True);
            Assert.That(cabinet.Cabinet.CorrectCount, Is.EqualTo(4));

            // After cooldown elapses, deliberate hover on equipped Safety Boots flips to "<Nome DPI> indossato".
            MachineLockHoverFeedback.ResetGlobalCooldown();
            InvokeInteractableHover(cabinet.SafetyBoots.Interactable, Hand.Right);
            yield return new WaitForSecondsRealtime(MachineLockHoverFeedback.DEFAULT_DWELL_SECONDS + 0.08f);
            Assert.That(cabinet.SafetyBoots.Item.LastHoverFeedback, Is.EqualTo("Scarpe antinfortunistiche S3 indossato"));
            Assert.That(cabinet.SafetyBoots.Item.IsEquippedShelfStatusVisible, Is.True);
            Assert.That(cabinet.SafetyBoots.Interactable.Selectable, Is.False);
            yield return null;
        }

        /// <summary>
        /// In Guided mode, subtle Containment Zone baseline floor rings/outlines appear once full PPE is
        /// worn, brighten into full snap ghosts while holding matching gear, and hide individually once occupied.
        /// </summary>
        [UnityTest]
        public IEnumerator SpillSystem_ProactiveGuidedBaselineMarkers_AppearOnFullPpe_BrightenOnHold_HideOnOccupy()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            GhostFixture zones = CreateGhostZones(scenario);
            WireObject(scenario, "spillSystem", zones.Spill);
            yield return null;

            Assert.That(zones.ConeZones[0].IsBaselineVisible, Is.False, "Baseline markers stay hidden before full PPE.");
            Assert.That(zones.AbsorbentZone.IsBaselineVisible, Is.False);

            // Wearing 4/4 PPE proactively activates baseline markers across all free Containment Zones.
            scenario.NotifyPpeChanged(4, true);
            Assert.That(zones.ConeZones[0].IsBaselineVisible, Is.True, "Cone baseline ring appears on full PPE.");
            Assert.That(zones.ConeZones[1].IsBaselineVisible, Is.True);
            Assert.That(zones.AbsorbentZone.IsBaselineVisible, Is.True, "Absorbent quadrant outline appears on full PPE.");
            Assert.That(zones.ConeGhosts[0].activeSelf, Is.False, "Full snap ghost stays off until holding matching gear.");

            // Holding a cone brightens free cone zones into full snap ghosts.
            zones.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, true);
            Assert.That(zones.ConeGhosts[0].activeSelf, Is.True);
            Assert.That(zones.AbsorbentGhost.activeSelf, Is.False);

            // Occupying zone 0 hides both its baseline marker and its snap ghost, leaving zone 1 visible.
            Assert.That(zones.ConeZones[0].Occupy(), Is.True);
            Assert.That(zones.ConeZones[0].IsBaselineVisible, Is.False);
            Assert.That(zones.ConeZones[0].IsGhostVisible, Is.False);
            Assert.That(zones.ConeZones[1].IsBaselineVisible, Is.True);
            Assert.That(zones.ConeZones[1].IsGhostVisible, Is.True);

            zones.Spill.SetHeldSnapKind(MachineLockSnapZone.SnapKind.Cone, false);
            Assert.That(zones.ConeZones[1].IsGhostVisible, Is.False);
            Assert.That(zones.ConeZones[1].IsBaselineVisible, Is.True, "Unoccupied zone keeps baseline ring after release.");

            // Outside Guided, baseline markers stay hidden.
            MissionSession.Begin(mission, DifficultyLevel.Operational, true);
            zones.Spill.SetContainmentWayfindingActive(true);
            Assert.That(zones.ConeZones[1].IsBaselineVisible, Is.False);
            yield return null;
        }

        /// <summary>
        /// Hovering each of the three electrical Sub-Stations emits Italian status feedback reflecting
        /// live transitions (Main ON -> Main OFF -> Lockout Applied -> Tester ZERO).
        /// </summary>
        [UnityTest]
        public IEnumerator EnergyPanel_SubStationHoverReportsNameAndLiveState()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            yield return null;

            panel.Panel.InitializeState(false);

            float dwellWait = MachineLockHoverFeedback.DEFAULT_DWELL_SECONDS + 0.08f;

            // Brief graze does not emit.
            InvokeInteractableHover(panel.MainSwitch, Hand.Right);
            yield return new WaitForSecondsRealtime(0.15f);
            InvokeInteractableHoverExit(panel.MainSwitch, Hand.Right);
            Assert.That(panel.Panel.LastHoverFeedback, Is.Empty);

            // Deliberate dwell emits MainSwitch ON, then global cooldown suppresses immediate Lockout hover.
            InvokeInteractableHover(panel.MainSwitch, Hand.Right);
            yield return new WaitForSecondsRealtime(dwellWait);
            Assert.That(panel.Panel.LastHoverFeedback, Is.EqualTo(MachineLockTexts.HoverMainSwitchOn));

            InvokeInteractableHover(panel.Lockout, Hand.Right);
            yield return new WaitForSecondsRealtime(dwellWait);
            Assert.That(panel.Panel.LastHoverFeedback, Is.EqualTo(MachineLockTexts.HoverMainSwitchOn),
                "Global cooldown must prevent rapid Sub-Station hover spam.");

            // Verify live state hover texts across transitions once cooldown resets.
            MachineLockHoverFeedback.ResetGlobalCooldown();
            InvokeInteractableHover(panel.Lockout, Hand.Right);
            yield return new WaitForSecondsRealtime(dwellWait);
            Assert.That(panel.Panel.LastHoverFeedback, Is.EqualTo(MachineLockTexts.HoverLockoutBlocked));

            MachineLockHoverFeedback.ResetGlobalCooldown();
            InvokeInteractableHover(panel.Tester, Hand.Right);
            yield return new WaitForSecondsRealtime(dwellWait);
            Assert.That(panel.Panel.LastHoverFeedback, Is.EqualTo(MachineLockTexts.HoverTesterLivePanel));

            panel.Panel.SetMainPower(false);
            MachineLockHoverFeedback.ResetGlobalCooldown();
            InvokeInteractableHover(panel.MainSwitch, Hand.Right);
            yield return new WaitForSecondsRealtime(dwellWait);
            Assert.That(panel.Panel.LastHoverFeedback, Is.EqualTo(MachineLockTexts.HoverMainSwitchOff));

            MachineLockHoverFeedback.ResetGlobalCooldown();
            InvokeInteractableHover(panel.Lockout, Hand.Right);
            yield return new WaitForSecondsRealtime(dwellWait);
            Assert.That(panel.Panel.LastHoverFeedback, Is.EqualTo(MachineLockTexts.HoverLockoutReady));

            panel.Panel.ApplyLockout();
            MachineLockHoverFeedback.ResetGlobalCooldown();
            InvokeInteractableHover(panel.Lockout, Hand.Right);
            yield return new WaitForSecondsRealtime(dwellWait);
            Assert.That(panel.Panel.LastHoverFeedback, Is.EqualTo(MachineLockTexts.HoverLockoutApplied));

            panel.Panel.ReadTester();
            MachineLockHoverFeedback.ResetGlobalCooldown();
            InvokeInteractableHover(panel.Tester, Hand.Right);
            yield return new WaitForSecondsRealtime(dwellWait);
            Assert.That(panel.Panel.LastHoverFeedback, Is.EqualTo(MachineLockTexts.HoverTesterZeroConfirmed));
            yield return null;
        }

        /// <summary>
        /// When the active URP camera runs with renderPostProcessing = false (Quest 3 MNG_CoreRig setup),
        /// MachineLockDamageFeedback automatically drives the head-aligned peripheral red vignette overlay,
        /// fallback light pulse, and brief dark respawn blink, respects accessibility.reduce_flashes, and
        /// resets cleanly on Clear().
        /// </summary>
        [UnityTest]
        public IEnumerator DamageFeedback_PostProcessingDisabledCamera_DrivesScenarioOverlayAndRespawnBlink()
        {
            GameObject camObj = new GameObject("TEST_NoPostProcCamera");
            roots.Add(camObj);
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.enabled = true;
            UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = false;

            GameObject root = NewRoot("TEST_DamageOverlayFeedback");
            Volume volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            VolumeProfile profile = TrackAsset(ScriptableObject.CreateInstance<VolumeProfile>());
            profile.Add<Vignette>();
            volume.profile = profile;
            volume.weight = 0f;

            GameObject lightObj = new GameObject("LIGHT_Fallback");
            lightObj.transform.SetParent(root.transform, false);
            Light flashLight = lightObj.AddComponent<Light>();

            MachineLockDamageFeedback feedback = root.AddComponent<MachineLockDamageFeedback>();
            WireObject(feedback, "damageVolume", volume);
            WireObject(feedback, "fallbackFlashLight", flashLight);
            WireBool(feedback, "forceFallbackForTesting", false);
            root.SetActive(true);
            yield return null;

            // Normal settings: post-processing off engages scenario-owned overlay + fallback light, NOT Volume.
            feedback.PlayCritical(CriticalEvent.LiveOilShock);
            yield return new WaitForSeconds(0.18f);

            Assert.That(feedback.LastUsedScenarioOverlayPath, Is.True,
                "Disabled camera post-processing must select the scenario-owned visual path.");
            Assert.That(volume.weight, Is.EqualTo(0f).Within(Tolerance),
                "Invisible URP Volume must not be animated when camera post-processing is off.");
            Assert.That(feedback.IsScenarioOverlayActive, Is.True,
                "Head-aligned peripheral red vignette overlay must render.");
            Assert.That(feedback.CurrentOverlayIntensity, Is.GreaterThan(0.5f));
            Assert.That(flashLight.intensity, Is.GreaterThan(0f));

            float normalIntensity = feedback.CurrentOverlayIntensity;
            feedback.Clear();
            Assert.That(feedback.IsScenarioOverlayActive, Is.False, "Clear hides scenario overlay.");
            Assert.That(feedback.CurrentOverlayIntensity, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(flashLight.intensity, Is.EqualTo(0f).Within(Tolerance));

            // Accessibility: reduce_flashes softens peak overlay intensity and suppresses bright light flash.
            var settingsStub = new MemorySettingsStore();
            settingsStub.SetBool(SettingsCatalog.ACCESSIBILITY_REDUCE_FLASHES, true);
            UIServices.Configure(null, null, settingsStub, null);

            feedback.PlayCritical(CriticalEvent.HotOilBurn);
            yield return new WaitForSeconds(0.30f);
            Assert.That(feedback.IsScenarioOverlayActive, Is.True);
            Assert.That(feedback.CurrentOverlayIntensity, Is.LessThan(normalIntensity),
                "reduce_flashes must attenuate overlay peak intensity while keeping visual warning.");
            Assert.That(flashLight.intensity, Is.EqualTo(0f).Within(Tolerance),
                "reduce_flashes must suppress the bright point-light flash.");
            feedback.Clear();

            // Respawn blink: brief smooth dark fade invoking midpoint teleport callback, reset cleanly on Clear.
            bool teleported = false;
            feedback.PlayRespawnBlink(0.12f, () => teleported = true);
            Assert.That(feedback.IsRespawnBlinkActive, Is.True);
            yield return new WaitForSeconds(0.16f);
            Assert.That(teleported, Is.True, "Respawn blink must invoke the midpoint teleport callback.");
            yield return new WaitForSeconds(0.20f);
            Assert.That(feedback.IsRespawnBlinkActive, Is.False);
            Assert.That(feedback.IsScenarioOverlayActive, Is.False);
        }

        /// <summary>
        /// The overhead exit beacon shifts to its ready green material once both the press is stopped
        /// and spill containment is complete.
        /// </summary>
        [UnityTest]
        public IEnumerator Scenario_ExitBeaconTurnsReady_WhenPressStoppedAndContained()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            GameObject beaconObj = new GameObject("TEST_ExitBeacon");
            roots.Add(beaconObj);
            MeshRenderer beaconRenderer = beaconObj.AddComponent<MeshRenderer>();
            Material idleMat = CreateMaterial();
            Material readyMat = CreateMaterial();
            beaconRenderer.sharedMaterial = idleMat;

            WireObject(scenario, "exitBeaconRenderer", beaconRenderer);
            WireObject(scenario, "exitBeaconIdleMaterial", idleMat);
            WireObject(scenario, "exitBeaconReadyMaterial", readyMat);
            yield return null;

            scenario.NotifyEStopPressed();
            Assert.That(scenario.IsExitBeaconReady, Is.False);
            Assert.That(beaconRenderer.sharedMaterial, Is.SameAs(idleMat));

            scenario.NotifyConePlaced(4);
            scenario.NotifyAbsorbentPlaced(4);
            Assert.That(scenario.IsExitBeaconReady, Is.True);
            Assert.That(beaconRenderer.sharedMaterial, Is.SameAs(readyMat));
            yield return null;
        }

        // =====================================================================
        // P9 - End-to-end flows and real scene
        // =====================================================================

        /// <summary>
        /// Guided happy path: observe 6/6, E-stop, alarm, main off, lockout, tester zero, try-start,
        /// 4/4 PPE, valve closed, 4 cones and 4 absorbents and the exit. The mission ends Positive with
        /// four objectives and a near-perfect SPI.
        /// </summary>
        [UnityTest]
        public IEnumerator Flow_GuidedHappyPath_EndsPositiveWithHighSpi()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            AlarmFixture alarm = CreateAlarm(scenario);
            CabinetFixture cabinet = CreateCabinet(scenario, false);
            SpillFixture spill = CreateSpill(scenario, true);
            ValveFixture valve = CreateValve(scenario, spill.Spill);
            yield return null;

            Assert.That(scenario.Config, Is.SameAs(config), "InitializeForTests keeps the supplied config.");

            for (int i = 0; i < ObservationIds.Length; i++)
                scenario.NotifyObservation(ObservationIds[i]);
            Assert.That(scenario.ObservedTargets, Is.EqualTo(ObservationIds.Length));

            press.Press.PressEStop();
            alarm.Alarm.Raise();
            panel.Panel.SetMainPower(false);
            panel.Panel.ApplyLockout();
            panel.Panel.ForceZeroEnergy();
            Assert.That(panel.Panel.ReadTester(), Is.EqualTo(MachineLockTexts.TesterReadZero));
            press.Press.TryStart();

            cabinet.Cabinet.Open();
            for (int i = 0; i < cabinet.CorrectItems.Length; i++)
                Assert.That(cabinet.Cabinet.Equip(cabinet.CorrectItems[i].Item), Is.True);

            valve.Valve.Close();
            PlaceAllContainment(spill);

            Assert.That(scenario.TaskEStop, Is.True);
            Assert.That(scenario.TaskAlarm, Is.True);
            Assert.That(scenario.TaskMainOff, Is.True);
            Assert.That(scenario.TaskLockout, Is.True);
            Assert.That(scenario.TaskTesterZero, Is.True);
            Assert.That(scenario.TaskTryStart, Is.True);
            Assert.That(scenario.TaskPpeFull, Is.True);
            Assert.That(scenario.TaskContainment, Is.True);
            Assert.That(scenario.ValveClosed, Is.True);
            Assert.That(scenario.Spi.CriticalOccurred, Is.False, "The happy path has no critical event.");

            scenario.NotifyExitReached();
            Assert.That(scenario.TaskExit, Is.True);

            yield return new WaitForSeconds(2.7f);

            Assert.That(scenario.HasEnded, Is.True);
            Assert.That(scenario.ObjectivesCompleted, Is.EqualTo(MachineLockScenario.MACRO_COUNT));
            Assert.That(scenario.Spi.Spi, Is.GreaterThanOrEqualTo(95));
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Positive));
            Assert.That(MissionSession.LastResult.SafetyScore, Is.GreaterThanOrEqualTo(95));
            Assert.That(MissionSession.LastResult.Dimensions.Length, Is.EqualTo(5));
        }

        /// <summary>
        /// Early Exit while the press is still running is permitted (ADR-0006): it ends the mission
        /// Negative with the critical debrief row, severe Safety/Decision penalties, and zero score in
        /// Operational mode.
        /// </summary>
        [UnityTest]
        public IEnumerator Flow_EarlyExitWhilePressRunning_EndsNegativeWithCriticalDebrief()
        {
            // Guided Early Exit with press still running -> Negative + CriticalMissingEStop.
            MachineLockScenario guided = CreateScenario(DifficultyLevel.Guided);
            CreatePress(guided);
            yield return null;

            guided.NotifyExitReached();
            Assert.That(guided.TaskExit, Is.True, "Early Exit must be allowed even while the press runs.");
            Assert.That(guided.Spi.CriticalOccurred, Is.True);
            Assert.That(HasDebrief(guided, DebriefLineId.CriticalMissingEStop), Is.True);

            yield return new WaitForSeconds(2.7f);

            Assert.That(guided.HasEnded, Is.True);
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Negative));
            Assert.That(MissionSession.LastResult.SafetyScore, Is.EqualTo(0));
            DestroyTrackedObjects();

            // Operational Early Exit with partial progress (observed + alarm) still yields 0 Negative.
            MachineLockScenario operational = CreateScenario(DifficultyLevel.Operational);
            CreatePress(operational);
            AlarmFixture alarm = CreateAlarm(operational);
            yield return null;

            for (int i = 0; i < ObservationIds.Length; i++) operational.NotifyObservation(ObservationIds[i]);
            alarm.Alarm.Raise();
            operational.NotifyExitReached();

            yield return new WaitForSeconds(2.7f);

            Assert.That(operational.HasEnded, Is.True);
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Negative));
            Assert.That(MissionSession.LastResult.SafetyScore, Is.EqualTo(0));
        }

        /// <summary>
        /// After pressing E-stop, a handover without the alarm and without containment ends Partial
        /// and carries both missing-task penalties.
        /// </summary>
        [UnityTest]
        public IEnumerator Flow_ExitAfterEStopWithMissingAlarmAndContainment_YieldsPartialWithPenalties()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            CabinetFixture cabinet = CreateCabinet(scenario, false);
            yield return null;

            press.Press.PressEStop();
            panel.Panel.SetMainPower(false);
            panel.Panel.ApplyLockout();
            panel.Panel.ForceZeroEnergy();
            panel.Panel.ReadTester();
            press.Press.TryStart();

            cabinet.Cabinet.Open();
            for (int i = 0; i < cabinet.CorrectItems.Length; i++)
                Assert.That(cabinet.Cabinet.Equip(cabinet.CorrectItems[i].Item), Is.True);

            // The alarm is never raised and the spill is never contained.
            scenario.NotifyExitReached();
            Assert.That(scenario.TaskExit, Is.True);

            yield return new WaitForSeconds(2.7f);

            Assert.That(scenario.HasEnded, Is.True);
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Partial));
            Assert.That(scenario.TaskAlarm, Is.False);
            Assert.That(scenario.TaskContainment, Is.False);
            Assert.That(HasDebrief(scenario, DebriefLineId.BadMissingAlarm), Is.True);
            Assert.That(HasDebrief(scenario, DebriefLineId.BadMissingContainment), Is.True);
            Assert.That(MissionSession.LastResult.SafetyScore, Is.LessThan(95));
        }

        /// <summary>
        /// Activating the west exit door panic bar via Select (Trigger) or Grab (Grip) opens the shared
        /// HUD_DecisionPanel: selecting No keeps the door shut and the mission running; selecting Yes
        /// disables the door collider, swings the steel leaf open ~90 degrees, and completes the exit.
        /// </summary>
        [UnityTest]
        public IEnumerator ExitDoor_PanicBarSelectAndGrab_OpensDecisionPanel_NoStays_YesSwingsOpenAndEndsMission()
        {
#if UNITY_EDITOR
            GameObject decisionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Home/UI/Prefabs/HUD/HUD_DecisionPanel.prefab");
            Assert.That(decisionPrefab, Is.Not.Null, "HUD_DecisionPanel.prefab must exist in Home/UI.");

            GameObject decisionInstance = UnityEngine.Object.Instantiate(decisionPrefab);
            roots.Add(decisionInstance);
            DecisionPanel decisionPanel = decisionInstance.GetComponent<DecisionPanel>();
            Assert.That(decisionPanel, Is.Not.Null);

            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            CreatePress(scenario);

            GameObject doorRoot = NewRoot("TEST_ExitDoor");
            GameObject doorLeaf = new GameObject("MESH_SteelExitDoorLeaf");
            doorLeaf.transform.SetParent(doorRoot.transform, false);
            BoxCollider leafCollider = doorLeaf.AddComponent<BoxCollider>();

            GameObject panicBarObj = new GameObject("MESH_PanicBar");
            panicBarObj.transform.SetParent(doorLeaf.transform, false);
            panicBarObj.AddComponent<BoxCollider>();
            Interactable panicBar = panicBarObj.AddComponent<Interactable>();
            WireBool(panicBar, "canSelect", true);
            WireBool(panicBar, "canGrab", true);
            WireBool(panicBar, "followsHand", false);

            MachineLockExitDoor exitDoor = doorRoot.AddComponent<MachineLockExitDoor>();
            WireObject(exitDoor, "scenario", scenario);
            WireObject(exitDoor, "doorInteractable", panicBar);
            WireObject(exitDoor, "doorLeafTransform", doorLeaf.transform);
            WireObject(exitDoor, "doorLeafCollider", leafCollider);
            WireFloat(exitDoor, "openAngleDegrees", 90f);
            WireFloat(exitDoor, "openDurationSeconds", 0.15f);
            doorRoot.SetActive(true);
            yield return null;

            // Hovering the exit door panic bar must remain silent (relies on 3D LabelAnchor + E001/E002 signs).
            InvokeInteractableHover(panicBar, Hand.Right);
            Assert.That(exitDoor.IsConfirmationOpen, Is.False);

            // Select (Trigger) opens the shared DecisionPanel.
            MethodInfo selectEnter = typeof(Interactable).GetMethod("SelectEnter", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo grabEnter = typeof(Interactable).GetMethod("GrabEnter", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo chooseMethod = typeof(DecisionPanel).GetMethod("Choose", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(selectEnter, Is.Not.Null);
            Assert.That(grabEnter, Is.Not.Null);
            Assert.That(chooseMethod, Is.Not.Null);

            selectEnter.Invoke(panicBar, new object[] { Hand.Right });
            Assert.That(exitDoor.IsConfirmationOpen, Is.True, "Select on panic bar must open confirmation.");
            Assert.That(decisionPanel.IsOpen, Is.True, "Shared HUD_DecisionPanel must open.");

            // Option 1 ("No, resta nella stanza") cancels without opening the door or ending the mission.
            chooseMethod.Invoke(decisionPanel, new object[] { 1 });
            Assert.That(exitDoor.IsConfirmationOpen, Is.False);
            Assert.That(exitDoor.IsOpened, Is.False);
            Assert.That(leafCollider.enabled, Is.True);
            Assert.That(scenario.TaskExit, Is.False);
            Assert.That(scenario.HasEnded, Is.False);
            Assert.That(Quaternion.Angle(Quaternion.identity, doorLeaf.transform.localRotation), Is.LessThan(0.5f));

            // Grab (Grip) also opens the shared DecisionPanel cleanly (without FollowsHand).
            grabEnter.Invoke(panicBar, new object[] { Hand.Left });
            Assert.That(exitDoor.IsConfirmationOpen, Is.True, "Grab on panic bar must also open confirmation.");
            Assert.That(decisionPanel.IsOpen, Is.True);

            // Option 0 ("Sì, esci dal reparto") swings the steel leaf +90 deg outward (-X) and completes the exit.
            chooseMethod.Invoke(decisionPanel, new object[] { 0 });
            Assert.That(leafCollider.enabled, Is.False, "Door collider must disable while swinging open.");
            Assert.That(panicBar.Selectable, Is.False);
            Assert.That(panicBar.Grabbable, Is.False);

            yield return new WaitForSeconds(0.25f);
            Assert.That(exitDoor.IsOpened, Is.True);
            Assert.That(Quaternion.Angle(Quaternion.identity, doorLeaf.transform.localRotation), Is.EqualTo(90f).Within(1.0f));
            Vector3 swungFreeEdge = doorLeaf.transform.TransformPoint(new Vector3(0f, 1.2f, -1.88f));
            Assert.That(swungFreeEdge.x, Is.LessThan(doorLeaf.transform.position.x - 1.5f),
                "Hinged door leaf must swing outward along -X into the west vestibule, never inward.");
            Assert.That(scenario.TaskExit, Is.True);

            yield return new WaitForSeconds(2.7f);
            Assert.That(scenario.HasEnded, Is.True);
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Negative),
                "Early Exit through the panic bar with press running must yield Negative.");
#else
            Assert.Ignore("HUD_DecisionPanel prefab test requires the Editor AssetDatabase.");
            yield return null;
#endif
        }

        /// <summary>
        /// Completing the full safe LOTO + PPE + containment procedure and exiting through the physical
        /// panic bar -> HUD_DecisionPanel confirmation swings the door outward (-X) and ends Positive.
        /// </summary>
        [UnityTest]
        public IEnumerator ExitDoor_FullSafeProcedure_PanicBarConfirm_SwingsOutwardAndEndsPositive()
        {
#if UNITY_EDITOR
            GameObject decisionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Home/UI/Prefabs/HUD/HUD_DecisionPanel.prefab");
            Assert.That(decisionPrefab, Is.Not.Null);

            GameObject decisionInstance = UnityEngine.Object.Instantiate(decisionPrefab);
            roots.Add(decisionInstance);
            DecisionPanel decisionPanel = decisionInstance.GetComponent<DecisionPanel>();
            Assert.That(decisionPanel, Is.Not.Null);

            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            AlarmFixture alarm = CreateAlarm(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            CabinetFixture cabinet = CreateCabinet(scenario, false);
            SpillFixture spill = CreateSpill(scenario, true);
            ValveFixture valve = CreateValve(scenario, spill.Spill);

            GameObject doorRoot = NewRoot("TEST_ExitDoorPositive");
            GameObject doorLeaf = new GameObject("MESH_SteelExitDoorLeaf");
            doorLeaf.transform.SetParent(doorRoot.transform, false);
            BoxCollider leafCollider = doorLeaf.AddComponent<BoxCollider>();

            GameObject panicBarObj = new GameObject("MESH_PanicBar");
            panicBarObj.transform.SetParent(doorLeaf.transform, false);
            panicBarObj.AddComponent<BoxCollider>();
            Interactable panicBar = panicBarObj.AddComponent<Interactable>();
            WireBool(panicBar, "canSelect", true);
            WireBool(panicBar, "canGrab", true);
            WireBool(panicBar, "followsHand", false);

            MachineLockExitDoor exitDoor = doorRoot.AddComponent<MachineLockExitDoor>();
            WireObject(exitDoor, "scenario", scenario);
            WireObject(exitDoor, "doorInteractable", panicBar);
            WireObject(exitDoor, "doorLeafTransform", doorLeaf.transform);
            WireObject(exitDoor, "doorLeafCollider", leafCollider);
            WireFloat(exitDoor, "openAngleDegrees", 90f);
            WireFloat(exitDoor, "openDurationSeconds", 0.15f);
            doorRoot.SetActive(true);
            yield return null;

            for (int i = 0; i < ObservationIds.Length; i++) scenario.NotifyObservation(ObservationIds[i]);
            press.Press.PressEStop();
            alarm.Alarm.Raise();
            panel.Panel.SetMainPower(false);
            panel.Panel.ApplyLockout();
            panel.Panel.ForceZeroEnergy();
            panel.Panel.ReadTester();
            press.Press.TryStart();
            cabinet.Cabinet.Open();
            for (int i = 0; i < cabinet.CorrectItems.Length; i++) cabinet.Cabinet.Equip(cabinet.CorrectItems[i].Item);
            valve.Valve.Close();
            PlaceAllContainment(spill);

            MethodInfo selectEnter = typeof(Interactable).GetMethod("SelectEnter", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo chooseMethod = typeof(DecisionPanel).GetMethod("Choose", BindingFlags.Instance | BindingFlags.NonPublic);
            selectEnter.Invoke(panicBar, new object[] { Hand.Right });
            Assert.That(decisionPanel.IsOpen, Is.True);
            chooseMethod.Invoke(decisionPanel, new object[] { 0 });

            yield return new WaitForSeconds(0.25f);
            Assert.That(exitDoor.IsOpened, Is.True);
            Vector3 swungFreeEdge = doorLeaf.transform.TransformPoint(new Vector3(0f, 1.2f, -1.88f));
            Assert.That(swungFreeEdge.x, Is.LessThan(doorLeaf.transform.position.x - 1.5f),
                "Positive exit must swing the door leaf outward along -X.");

            yield return new WaitForSeconds(2.7f);
            Assert.That(scenario.HasEnded, Is.True);
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Positive));
            Assert.That(MissionSession.LastResult.SafetyScore, Is.GreaterThanOrEqualTo(95));
#else
            Assert.Ignore("HUD_DecisionPanel prefab test requires the Editor AssetDatabase.");
            yield return null;
#endif
        }

        /// <summary>
        /// Regression test for Issue #43: aiming anywhere on the steel door leaf (not just the panic bar)
        /// resolves to the exit Interactable; opening confirmation close to the West door clamps
        /// HUD_DecisionPanel strictly inside the bay and in front of the eyes; re-clicking recenters
        /// without deadlocking; walking beyond 3.5 m or external closure cleanly resets confirmation state.
        /// </summary>
        [UnityTest]
        public IEnumerator ExitDoor_WholeLeafHit_BayClampedDecisionPanel_RecenterAndWalkAwayAutoClose()
        {
#if UNITY_EDITOR
            GameObject decisionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Home/UI/Prefabs/HUD/HUD_DecisionPanel.prefab");
            Assert.That(decisionPrefab, Is.Not.Null);

            GameObject decisionInstance = UnityEngine.Object.Instantiate(decisionPrefab);
            roots.Add(decisionInstance);
            DecisionPanel decisionPanel = decisionInstance.GetComponent<DecisionPanel>();
            Assert.That(decisionPanel, Is.Not.Null);

            // Temporary camera acting as player head (Camera.main) standing right in front of the West Exit Door.
            GameObject headObj = new GameObject("TEST_MainCamera");
            headObj.tag = "MainCamera";
            roots.Add(headObj);
            headObj.AddComponent<Camera>();
            headObj.transform.position = new Vector3(-8.50f, 1.65f, 0f);
            headObj.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            decisionPanel.SetRig(headObj.transform);

            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            CreatePress(scenario);

            GameObject doorRoot = NewRoot("TEST_ExitDoorIssue43");
            doorRoot.transform.position = new Vector3(-9.0f, 0f, 0f);
            GameObject doorLeaf = new GameObject("MESH_SteelExitDoorLeaf");
            doorLeaf.transform.SetParent(doorRoot.transform, false);
            doorLeaf.transform.localPosition = new Vector3(-0.05f, 0f, 0.94f);
            BoxCollider leafCollider = doorLeaf.AddComponent<BoxCollider>();
            leafCollider.center = new Vector3(0f, 1.22f, -0.94f);
            leafCollider.size = new Vector3(0.10f, 2.42f, 1.88f);
            Interactable doorInteractable = doorLeaf.AddComponent<Interactable>();
            WireBool(doorInteractable, "canSelect", true);
            WireBool(doorInteractable, "canGrab", true);
            WireBool(doorInteractable, "followsHand", false);

            GameObject panicBarObj = new GameObject("MESH_PanicBar");
            panicBarObj.transform.SetParent(doorLeaf.transform, false);
            panicBarObj.transform.localPosition = new Vector3(0.036f, 1.05f, -0.94f);
            BoxCollider barCollider = panicBarObj.AddComponent<BoxCollider>();
            barCollider.center = new Vector3(0.09f, 0f, 0f);
            barCollider.size = new Vector3(0.24f, 0.30f, 1.44f);

            MachineLockExitDoor exitDoor = doorRoot.AddComponent<MachineLockExitDoor>();
            WireObject(exitDoor, "scenario", scenario);
            WireObject(exitDoor, "doorInteractable", doorInteractable);
            WireObject(exitDoor, "doorLeafTransform", doorLeaf.transform);
            WireObject(exitDoor, "doorLeafCollider", leafCollider);
            doorRoot.SetActive(true);
            yield return null;
            yield return new WaitForFixedUpdate();

            // 1. Raycast aimed at the upper door leaf (y = 1.65, well above the panic bar) must resolve Interactable.
            bool hitUpperLeaf = Physics.Raycast(new Vector3(-7.5f, 1.65f, 0f), Vector3.left, out RaycastHit leafHit,
                5f, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(hitUpperLeaf, Is.True, "Raycast must hit the upper door leaf.");
            Assert.That(leafHit.collider.GetComponentInParent<Interactable>(true), Is.EqualTo(doorInteractable),
                "Upper door leaf collider must resolve to the exit door Interactable.");

            // 2. Raycast aimed at the protruding panic bar must also resolve to the same Interactable.
            bool hitBar = Physics.Raycast(new Vector3(-7.5f, 1.05f, 0f), Vector3.left, out RaycastHit barHit,
                5f, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(hitBar, Is.True, "Raycast must hit the panic bar.");
            Assert.That(barHit.collider.GetComponentInParent<Interactable>(true), Is.EqualTo(doorInteractable),
                "Panic bar collider must resolve to the same exit door Interactable.");

            // 3. Standing 0.50 m from the West door facing West: HUD_DecisionPanel must stay inside bay (> -9.00) and ahead of eyes (< -8.50).
            Assert.That(exitDoor.RequestExitConfirmation(), Is.True);
            Assert.That(decisionPanel.IsOpen, Is.True);
            float clampedX = decisionPanel.transform.position.x;
            Assert.That(clampedX, Is.GreaterThan(-8.95f),
                "HUD_DecisionPanel must never clip behind the opaque West exit door at x = -9.00.");
            Assert.That(clampedX, Is.LessThan(headObj.transform.position.x - 0.15f),
                "HUD_DecisionPanel must stay strictly in front of the player's eyes.");
            float actualDist = Vector3.Distance(
                new Vector3(headObj.transform.position.x, decisionPanel.transform.position.y, headObj.transform.position.z),
                decisionPanel.transform.position);
            Assert.That(decisionPanel.transform.localScale.x, Is.EqualTo(UITheme.GetCanvasScale(actualDist)).Within(0.0001f),
                "HUD_DecisionPanel scale must match its clamped distance from the eyes.");

            // 3b. Oblique 60-deg NW gaze at x = -8.50: even the westernmost panel edge must stay inside x = -9.00.
            headObj.transform.position = new Vector3(-8.50f, 1.65f, 0f);
            headObj.transform.rotation = Quaternion.LookRotation(new Vector3(-0.5f, 0f, 0.866f), Vector3.up);
            Assert.That(exitDoor.RequestExitConfirmation(), Is.True);
            Vector3 obliqueCenter = decisionPanel.transform.position;
            float obliqueHalfWidth = 240f * decisionPanel.transform.localScale.x;
            float westEdgeX = obliqueCenter.x - Mathf.Abs(decisionPanel.transform.right.x) * obliqueHalfWidth;
            Assert.That(westEdgeX, Is.GreaterThan(-8.98f),
                "Outer west edge of HUD_DecisionPanel must never clip behind the West wall at oblique angles.");

            // 3c. Ultra-close roomscale lean (x = -8.85 facing West): panel still stays strictly inside x = -9.00 and ahead of eyes.
            headObj.transform.position = new Vector3(-8.85f, 1.65f, 0f);
            headObj.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            Assert.That(exitDoor.RequestExitConfirmation(), Is.True);
            Assert.That(decisionPanel.transform.position.x, Is.GreaterThan(-8.98f),
                "Even when leaning within 0.15 m of the closed door, panel must not clip behind x = -9.00.");
            Assert.That(decisionPanel.transform.position.x, Is.LessThan(headObj.transform.position.x - 0.04f),
                "Even at ultra-close lean, panel must stay in front of the player's eyes.");

            // 4. Stepping back and re-clicking while already open must recenter (never deadlock IsConfirmationOpen).
            headObj.transform.position = new Vector3(-7.20f, 1.65f, 0f);
            Assert.That(exitDoor.RequestExitConfirmation(), Is.True,
                "Re-activating the door while confirmation is open must recenter instead of deadlocking.");
            Assert.That(decisionPanel.transform.position.x, Is.EqualTo(-8.40f).Within(0.05f),
                "Recentered panel at x = -7.20 should sit at the full 1.20 m distance (-8.40).");

            // 5. Walking beyond AUTO_CLOSE_DISTANCE_METERS (> 3.5 m from x = -9.0) auto-closes the panel.
            headObj.transform.position = new Vector3(-4.50f, 1.65f, 0f);
            yield return null;
            Assert.That(exitDoor.IsConfirmationOpen, Is.False,
                "Walking > 3.5 m away from the exit door must auto-close confirmation.");
            Assert.That(decisionPanel.IsOpen, Is.False);

            // 6. External closure of DecisionPanel clears IsConfirmationOpen cleanly on the next frame.
            headObj.transform.position = new Vector3(-7.50f, 1.65f, 0f);
            Assert.That(exitDoor.RequestExitConfirmation(), Is.True);
            decisionPanel.Close();
            yield return null;
            Assert.That(exitDoor.IsConfirmationOpen, Is.False,
                "Closing HUD_DecisionPanel externally must clear IsConfirmationOpen.");
#else
            Assert.Ignore("HUD_DecisionPanel prefab test requires the Editor AssetDatabase.");
            yield return null;
#endif
        }

        /// <summary>
        /// Regression test for Issue #43: all 6 Sub-Station permutations place SUB_MainDisconnect,
        /// SUB_LockoutStation and SUB_VoltageTester into distinct symmetrical Z slots (-0.88, 0, +0.88)
        /// with full separation, and MachineLockScenario.SetForcedSubStationOrderIndex applies cleanly.
        /// </summary>
        [UnityTest]
        public IEnumerator EnergyPanel_AllSixSubStationPermutations_AreDistinctAndKeepFullSeparation()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PanelFixture fixture = CreatePanel(scenario, null);

            GameObject subMain = new GameObject("SUB_MainDisconnect");
            subMain.transform.SetParent(fixture.Panel.transform, false);
            subMain.transform.localPosition = new Vector3(0f, 0f, -0.88f);

            GameObject subLock = new GameObject("SUB_LockoutStation");
            subLock.transform.SetParent(fixture.Panel.transform, false);
            subLock.transform.localPosition = Vector3.zero;

            GameObject subTest = new GameObject("SUB_VoltageTester");
            subTest.transform.SetParent(fixture.Panel.transform, false);
            subTest.transform.localPosition = new Vector3(0f, 0f, 0.88f);

            WireObject(fixture.Panel, "mainSwitchStationRoot", subMain.transform);
            WireObject(fixture.Panel, "lockoutStationRoot", subLock.transform);
            WireObject(fixture.Panel, "testerStationRoot", subTest.transform);
            WireObject(scenario, "energyPanel", fixture.Panel);
            yield return null;

            var seenLayouts = new HashSet<string>();
            for (int perm = 0; perm < MachineLockEnergyPanel.SUB_STATION_PERMUTATION_COUNT; perm++)
            {
                scenario.SetForcedSubStationOrderIndex(perm);
                Assert.That(scenario.ActiveSubStationOrderIndex, Is.EqualTo(perm));

                float zMain = subMain.transform.localPosition.z;
                float zLock = subLock.transform.localPosition.z;
                float zTest = subTest.transform.localPosition.z;

                Assert.That(Mathf.Abs(zMain - zLock), Is.GreaterThanOrEqualTo(0.87f));
                Assert.That(Mathf.Abs(zLock - zTest), Is.GreaterThanOrEqualTo(0.87f));
                Assert.That(Mathf.Abs(zMain - zTest), Is.GreaterThanOrEqualTo(0.87f));

                string key = $"{zMain:F2}|{zLock:F2}|{zTest:F2}";
                Assert.That(seenLayouts.Add(key), Is.True, $"Duplicate Sub-Station layout at perm {perm}: {key}");
            }

            Assert.That(seenLayouts.Count, Is.EqualTo(MachineLockEnergyPanel.SUB_STATION_PERMUTATION_COUNT));
        }

        /// <summary>
        /// Regression test for Issue #43 exit transition: clicking Yes locks InputChannel.World
        /// (blocking RigLocomotion while leaving UI interactive), moves the rig to the front of the
        /// West exit door, swings the door open +90 deg, smoothly fades to pure black with the
        /// backdrop dropped strictly below Queue.Transparent (2950 < 3000), hides 3D world renderers
        /// while keeping the floor collider grounded, ends the mission, and cleanly unlocks input on unload.
        /// </summary>
        [UnityTest]
        public IEnumerator ExitDoor_ConfirmYes_MovesRigToDoor_LocksWorld_FadesBehindUi_AndUnlocksOnDestroy()
        {
#if UNITY_EDITOR
            InputService input = InputService.EnsureInstance();

            // Build a lightweight Player Rig with CharacterController + RigRegistration + RigLocomotion + Head camera.
            GameObject rigObj = new GameObject("TEST_PlayerRig");
            roots.Add(rigObj);
            rigObj.transform.position = new Vector3(-6.0f, 0f, 1.4f);
            rigObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            CharacterController cc = rigObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            GameObject headObj = new GameObject("TEST_RigHead");
            headObj.tag = "MainCamera";
            headObj.transform.SetParent(rigObj.transform, false);
            headObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            headObj.AddComponent<Camera>();
            GameObject leftHand = new GameObject("LeftHand");
            leftHand.transform.SetParent(rigObj.transform, false);
            GameObject rightHand = new GameObject("RightHand");
            rightHand.transform.SetParent(rigObj.transform, false);

            RigRegistration rig = rigObj.AddComponent<RigRegistration>();
            rig.Configure(headObj.transform, rigObj.transform, leftHand.transform, rightHand.transform);
            RigLocomotion locomotion = rigObj.AddComponent<RigLocomotion>();

            // Build a mock ENV_Room root with a visible wall renderer and a solid floor collider.
            GameObject envRoom = new GameObject("ENV_Room");
            roots.Add(envRoom);
            GameObject wallCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallCube.name = "MESH_TestWall";
            wallCube.transform.SetParent(envRoom.transform, false);
            wallCube.transform.position = new Vector3(-9.1f, 1.5f, 0f);
            Renderer wallRenderer = wallCube.GetComponent<Renderer>();

            GameObject floorCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorCube.name = "MESH_Floor";
            floorCube.transform.SetParent(envRoom.transform, false);
            floorCube.transform.position = new Vector3(0f, -0.5f, 0f);
            floorCube.transform.localScale = new Vector3(25f, 1f, 25f);
            Collider floorCollider = floorCube.GetComponent<Collider>();

            // Build scenario + damageFeedback + HUD_DecisionPanel + exit door.
            GameObject decisionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Home/UI/Prefabs/HUD/HUD_DecisionPanel.prefab");
            Assert.That(decisionPrefab, Is.Not.Null);
            GameObject decisionInstance = UnityEngine.Object.Instantiate(decisionPrefab);
            roots.Add(decisionInstance);
            DecisionPanel decisionPanel = decisionInstance.GetComponent<DecisionPanel>();
            decisionPanel.SetRig(headObj.transform);

            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            MachineLockDamageFeedback feedback = scenario.gameObject.AddComponent<MachineLockDamageFeedback>();
            WireBool(feedback, "forceFallbackForTesting", true);
            WireObject(scenario, "damageFeedback", feedback);
            CreatePress(scenario);

            GameObject doorRoot = NewRoot("TEST_ExitDoorTransition");
            doorRoot.transform.position = new Vector3(-9.0f, 0f, 0f);
            GameObject doorLeaf = new GameObject("MESH_SteelExitDoorLeaf");
            doorLeaf.transform.SetParent(doorRoot.transform, false);
            BoxCollider leafCollider = doorLeaf.AddComponent<BoxCollider>();
            Interactable doorInteractable = doorLeaf.AddComponent<Interactable>();
            WireBool(doorInteractable, "canSelect", true);
            WireBool(doorInteractable, "canGrab", true);
            WireBool(doorInteractable, "followsHand", false);

            MachineLockExitDoor exitDoor = doorRoot.AddComponent<MachineLockExitDoor>();
            WireObject(exitDoor, "scenario", scenario);
            WireObject(exitDoor, "doorInteractable", doorInteractable);
            WireObject(exitDoor, "doorLeafTransform", doorLeaf.transform);
            WireObject(exitDoor, "doorLeafCollider", leafCollider);
            WireFloat(exitDoor, "openAngleDegrees", 90f);
            WireFloat(exitDoor, "openDurationSeconds", 0.20f);
            WireFloat(exitDoor, "moveToDoorDurationSeconds", 0.15f);
            WireFloat(exitDoor, "fadeToBlackDurationSeconds", 0.20f);
            doorRoot.SetActive(true);
            yield return null;

            Assert.That(input.IsWorldSuppressed, Is.False);
            Assert.That(wallRenderer.enabled, Is.True);

            // Open confirmation and click Yes (Option 0).
            Assert.That(exitDoor.RequestExitConfirmation(), Is.True);
            MethodInfo chooseMethod = typeof(DecisionPanel).GetMethod("Choose", BindingFlags.Instance | BindingFlags.NonPublic);
            chooseMethod.Invoke(decisionPanel, new object[] { 0 });

            // Immediately on Yes: movement & world interaction are locked while UI channel stays active.
            Assert.That(scenario.IsMovementLockedForEndUi, Is.True);
            Assert.That(input.IsWorldSuppressed, Is.True, "World input/locomotion must lock immediately on Yes.");
            Assert.That(input.IsSuppressed(InputChannel.UI), Is.False, "UI channel must remain interactive.");

            // Wait for move-to-door + door-swing + fade-to-black to complete.
            yield return new WaitForSecondsRealtime(0.75f);

            // 1. Player moved cleanly to the front of the West Exit Door (~ -7.35, 0) facing West (-X).
            Vector3 finalHead = headObj.transform.position;
            Assert.That(finalHead.x, Is.EqualTo(-7.35f).Within(0.15f), "Player should move directly in front of the door.");
            Assert.That(finalHead.z, Is.EqualTo(0f).Within(0.15f), "Player should center on the doorway Z.");
            Assert.That(Vector3.Dot(Vector3.ProjectOnPlane(headObj.transform.forward, Vector3.up).normalized, Vector3.left),
                Is.GreaterThan(0.7f), "Player should face toward the opening West exit door.");

            // 2. Door swung open +90 deg and level faded to full black BEHIND UI (renderQueue < Transparent 3000).
            Assert.That(exitDoor.IsOpened, Is.True);
            Assert.That(feedback.IsExitFadeActive, Is.True);
            Assert.That(feedback.ExitFadeAlpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(feedback.ActiveOverlayRenderQueue, Is.EqualTo(MachineLockDamageFeedback.EXIT_BACKDROP_RENDER_QUEUE));
            Assert.That(feedback.ActiveOverlayRenderQueue, Is.LessThan(3000),
                "Black backdrop must render behind World Space UI (3000) and UIPointer (3000/3100).");

            // 3. 3D workshop visuals hidden (empty Z-buffer -> zero UI compenetration), floor collider still grounded!
            Assert.That(scenario.AreWorldVisualsHiddenForEndUi, Is.True);
            Assert.That(wallRenderer.enabled, Is.False, "3D scene renderers must hide behind the black end-of-level void.");
            Assert.That(floorCollider.enabled, Is.True, "Floor collider must stay enabled so RigLocomotion gravity keeps player grounded.");

            // 4. Mission ended while movement remains locked and UI stays interactive.
            Assert.That(scenario.HasEnded, Is.True);
            Assert.That(input.IsWorldSuppressed, Is.True);
            Assert.That(input.IsSuppressed(InputChannel.UI), Is.False);
            Assert.That(locomotion.enabled, Is.True);

            // 5. Unloading the scenario (Return to Home / Replay) cleanly unlocks InputChannel.World!
            UnityEngine.Object.DestroyImmediate(scenario.gameObject);
            Assert.That(input.IsWorldSuppressed, Is.False,
                "Destroying MachineLockScenario on unload must cleanly release the InputChannel.World lock.");
#else
            Assert.Ignore("HUD_DecisionPanel prefab test requires the Editor AssetDatabase.");
            yield return null;
#endif
        }

        /// <summary>
        /// The time limit ends the mission only outside Guided: a stopped and contained run is Partial,
        /// a run with the press still moving or the spill still open is Negative.
        /// </summary>
        [UnityTest]
        public IEnumerator Flow_Operational600AndCrisis480_TimerExpiryOutcomes()
        {
            config.timeLimitRealistic = 600f;
            config.timeLimitStress = 480f;

            // Operational: press stopped and spill contained -> Partial.
            MachineLockScenario operational = CreateScenario(DifficultyLevel.Operational);
            PressFixture operationalPress = CreatePress(operational);
            CreatePanel(operational, operationalPress.Press);
            SpillFixture operationalSpill = CreateSpill(operational, true);
            yield return null;

            operationalPress.Press.PressEStop();
            PlaceAllContainment(operationalSpill);
            Assert.That(operational.TaskContainment, Is.True);

            InvokeTimerExpiry(operational);

            Assert.That(operational.HasEnded, Is.True);
            Assert.That(operational.Spi.TimedOut, Is.True);
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Partial));
            Assert.That(HasDebrief(operational, DebriefLineId.BadTimeout), Is.True);
            DestroyTrackedObjects();

            // Crisis: the press is still running -> Negative.
            MachineLockScenario crisis = CreateScenario(DifficultyLevel.Crisis);
            CreatePress(crisis);
            CreateSpill(crisis, false);
            yield return null;

            InvokeTimerExpiry(crisis);

            Assert.That(crisis.HasEnded, Is.True);
            Assert.That(MissionSession.LastResult, Is.Not.Null);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Negative));
            DestroyTrackedObjects();

            // Operational: press stopped but the spill is still open -> Negative.
            MachineLockScenario openSpill = CreateScenario(DifficultyLevel.Operational);
            PressFixture openSpillPress = CreatePress(openSpill);
            CreateSpill(openSpill, false);
            yield return null;

            openSpillPress.Press.PressEStop();
            Assert.That(openSpill.TaskContainment, Is.False);

            InvokeTimerExpiry(openSpill);

            Assert.That(openSpill.HasEnded, Is.True);
            Assert.That(MissionSession.LastResult.Outcome, Is.EqualTo(OutcomeType.Negative));
            yield return null;
        }

        // =====================================================================
        // Issue #46 — Action Feedback & Exhaustive Scrollable Pause Checklist
        // =====================================================================

        /// <summary>
        /// Misplaced cones/absorbents, distractor PPE, and blocked Lockout clicks emit negative
        /// audiovisual/world-flash feedback without snapping or locking out; valid safety actions emit
        /// positive feedback and respect accessibility.reduce_flashes (Issue #46).
        /// </summary>
        [UnityTest]
        public IEnumerator ActionFeedback_WrongDrop_DistractorPpe_AndBlockedLockout_EmitNegative_ValidActionsEmitPositive()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            var actionFeedback = scenario.gameObject.AddComponent<MachineLockActionFeedback>();
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            CabinetFixture cabinet = CreateCabinet(scenario, true);
            SpillFixture spill = CreateSpill(scenario, true);
            ValveFixture valve = CreateValve(scenario, spill.Spill);

            Vector3 farConePos = new Vector3(0f, 0f, 20f);
            ConeFixture farCone = CreateCone(scenario, spill.Spill, farConePos);
            MeshRenderer farConeRenderer = farCone.Cone.gameObject.AddComponent<MeshRenderer>();
            farConeRenderer.sharedMaterial = CreateMaterial();

            GameObject farAbsObj = new GameObject("MESH_Absorbent_Loose");
            roots.Add(farAbsObj);
            farAbsObj.transform.position = new Vector3(0f, 0f, -20f);
            var farAbsInteractable = farAbsObj.AddComponent<Interactable>();
            var farAbs = farAbsObj.AddComponent<MachineLockAbsorbent>();
            WireObject(farAbs, "spillSystem", spill.Spill);
            WireObject(farAbs, "config", config);
            yield return null;

            // 1. Misplaced cone stays where dropped, remains grabbable, flashes red & emits negative cue.
            int negBefore = actionFeedback.NegativeCount;
            int posBefore = actionFeedback.PositiveCount;
            Assert.That(farCone.Cone.TrySnap(), Is.False);
            Assert.That(farCone.Cone.Snapped, Is.False);
            Assert.That(farCone.Interactable.Grabbable, Is.True);
            Assert.That(Vector3.Distance(farCone.Cone.transform.position, farConePos), Is.LessThan(Tolerance));
            Assert.That(actionFeedback.NegativeCount, Is.EqualTo(negBefore + 1));
            Assert.That(actionFeedback.PositiveCount, Is.EqualTo(posBefore));
            Assert.That(actionFeedback.LastFeedbackMessage, Is.EqualTo(MachineLockTexts.ConeWrongSpot));
            Assert.That(actionFeedback.LastFlashedTarget, Is.SameAs(farCone.Cone.gameObject));
            Assert.That(farConeRenderer.HasPropertyBlock(), Is.True, "World flash must apply via MaterialPropertyBlock.");

            // 2. Misplaced absorbent stays where dropped, remains grabbable, emits negative cue.
            negBefore = actionFeedback.NegativeCount;
            Assert.That(farAbs.TrySnap(), Is.False);
            Assert.That(farAbs.Snapped, Is.False);
            Assert.That(farAbsInteractable.Grabbable, Is.True);
            Assert.That(actionFeedback.NegativeCount, Is.EqualTo(negBefore + 1));
            Assert.That(actionFeedback.LastFeedbackMessage, Is.EqualTo(MachineLockTexts.AbsorbentWrongSpot));

            // 3. Valid cone & absorbent snaps emit positive cues.
            posBefore = actionFeedback.PositiveCount;
            Assert.That(spill.Cones[0].Cone.TrySnap(), Is.True);
            Assert.That(actionFeedback.PositiveCount, Is.EqualTo(posBefore + 1));
            posBefore = actionFeedback.PositiveCount;
            Assert.That(spill.Absorbents[0].Absorbent.TrySnap(), Is.True);
            Assert.That(actionFeedback.PositiveCount, Is.EqualTo(posBefore + 1));

            // 4. Distractor PPE emits negative cue; correct PPE emits positive cue.
            cabinet.Cabinet.Open();
            negBefore = actionFeedback.NegativeCount;
            posBefore = actionFeedback.PositiveCount;
            Assert.That(cabinet.Cabinet.Equip(cabinet.LatexGloves.Item), Is.True);
            Assert.That(actionFeedback.NegativeCount, Is.EqualTo(negBefore + 1));
            Assert.That(actionFeedback.PositiveCount, Is.EqualTo(posBefore));
            Assert.That(cabinet.Cabinet.Equip(cabinet.HeatGloves.Item), Is.True);
            Assert.That(actionFeedback.PositiveCount, Is.EqualTo(posBefore + 1));

            // 5. Blocked Lockout while Main Power is ON emits negative feedback and stays unlocked;
            //    Main OFF + valid Lockout + Valve close each emit positive feedback.
            panel.Panel.InitializeState(true);
            negBefore = actionFeedback.NegativeCount;
            panel.Panel.ApplyLockout();
            Assert.That(panel.Panel.LockedOut, Is.False);
            Assert.That(actionFeedback.NegativeCount, Is.EqualTo(negBefore + 1));
            Assert.That(actionFeedback.LastFeedbackMessage, Is.EqualTo(MachineLockTexts.LockoutBlockedFeedback));

            posBefore = actionFeedback.PositiveCount;
            panel.Panel.SetMainPower(false);
            Assert.That(actionFeedback.PositiveCount, Is.EqualTo(posBefore + 1));

            posBefore = actionFeedback.PositiveCount;
            panel.Panel.ApplyLockout();
            Assert.That(panel.Panel.LockedOut, Is.True);
            Assert.That(panel.Lockout.Selectable, Is.False);
            Assert.That(actionFeedback.PositiveCount, Is.EqualTo(posBefore + 1));

            posBefore = actionFeedback.PositiveCount;
            valve.Valve.Close();
            Assert.That(actionFeedback.PositiveCount, Is.EqualTo(posBefore + 1));

            // 6. Wait out the flash duration and confirm the MaterialPropertyBlock restores cleanly.
            yield return new WaitForSeconds(0.45f);
            Assert.That(farConeRenderer.HasPropertyBlock(), Is.False, "World flash must restore renderer MaterialPropertyBlock.");
        }

        /// <summary>
        /// MachineLockScenario publishes an 11-step live checklist to MissionFlow.PauseChecklist (including
        /// out-of-order tasks), PANEL_Pause renders it inside a clipped ScrollRect with an active vertical
        /// Scrollbar, and clearing/unloading restores the 4-item fallback with the scrollbar hidden (Issue #46).
        /// </summary>
        [UnityTest]
        public IEnumerator PauseChecklist_TracksAll11MachineLockSteps_AndScrollsInRealPauseScreen()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            SpillFixture spill = CreateSpill(scenario, false);
            ValveFixture valve = CreateValve(scenario, spill.Spill);
            yield return null;

            Assert.That(MissionFlow.PauseChecklist, Is.Not.Null);
            Assert.That(MissionFlow.PauseChecklist.Count, Is.EqualTo(11),
                "Machine Lock must publish all 11 concrete checklist tasks to the Pause screen.");
            Assert.That(MissionFlow.PauseChecklist[0].State, Is.EqualTo(ObjectiveState.Current));
            Assert.That(MissionFlow.PauseChecklist[0].Text, Does.Contain("(0/6)"));
            Assert.That(MissionFlow.PauseChecklist[1].State, Is.EqualTo(ObjectiveState.Todo));

            // Out-of-order completion: close valve and observe 1 target before E-stop.
            valve.Valve.Close();
            scenario.NotifyObservation("press");
            Assert.That(MissionFlow.PauseChecklist[8].State, Is.EqualTo(ObjectiveState.Done),
                "Closing the oil leak valve out of order must check off the valve step immediately.");
            Assert.That(MissionFlow.PauseChecklist[0].Text, Does.Contain("(1/6)"));
            Assert.That(MissionFlow.PauseChecklist[0].State, Is.EqualTo(ObjectiveState.Current));

            press.Press.PressEStop();
            Assert.That(MissionFlow.PauseChecklist[1].State, Is.EqualTo(ObjectiveState.Done));

#if UNITY_EDITOR
            const string pausePrefabPath = "Assets/_Project/Home/UI/Prefabs/Screens/PANEL_Pause.prefab";
            GameObject pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pausePrefabPath);
            Assert.That(pausePrefab, Is.Not.Null, "PANEL_Pause prefab must exist at " + pausePrefabPath);

            GameObject canvasRoot = new GameObject("TEST_PauseCanvas", typeof(RectTransform), typeof(Canvas));
            roots.Add(canvasRoot);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject pauseInstance = UnityEngine.Object.Instantiate(pausePrefab, canvasRoot.transform, false);
            pauseInstance.SetActive(true);
            var pauseScreen = pauseInstance.GetComponent<PauseScreen>();
            Assert.That(pauseScreen, Is.Not.Null);

            pauseScreen.Refresh();
            Canvas.ForceUpdateCanvases();
            yield return null;

            Assert.That(pauseScreen.VisibleObjectiveCount, Is.EqualTo(11));
            Assert.That(pauseScreen.ChecklistScrollRect, Is.Not.Null);
            Assert.That(pauseScreen.ChecklistScrollRect.enabled, Is.True,
                "ScrollRect must be enabled when 11 checklist entries overflow the viewport.");
            Assert.That(pauseScreen.ChecklistScrollbar, Is.Not.Null);
            Assert.That(pauseScreen.ChecklistScrollbar.gameObject.activeSelf, Is.True,
                "Vertical scrollbar must be visible for the 11-step Machine Lock checklist.");
            Assert.That(pauseScreen.ChecklistScrollRect.content.sizeDelta.y,
                Is.GreaterThan(pauseScreen.ChecklistScrollRect.viewport.rect.height),
                "Checklist content height must exceed viewport height so scrolling is meaningful.");

            // Unloading the scenario clears PauseChecklist and reverts PauseScreen to the 4 standard objectives.
            UnityEngine.Object.DestroyImmediate(scenario.gameObject);
            Assert.That(MissionFlow.PauseChecklist, Is.Null,
                "Destroying MachineLockScenario must clear MissionFlow.PauseChecklist.");

            pauseScreen.Refresh();
            Canvas.ForceUpdateCanvases();
            yield return null;

            Assert.That(pauseScreen.VisibleObjectiveCount, Is.EqualTo(MachineLockScenario.MACRO_COUNT));
            Assert.That(pauseScreen.ChecklistScrollbar.gameObject.activeSelf, Is.False,
                "Scrollbar must hide when falling back to standard 4 objectives.");
            Assert.That(pauseScreen.ChecklistScrollRect.enabled, Is.False);
#endif
        }

        /// <summary>
        /// Loads the real SCN_MachineLock additively with an active session (so the standalone-editor
        /// SCN_Core path never runs) and verifies the spawn marker, the absence of camera/UI
        /// infrastructure, every runtime component and the three layout patterns.
        /// </summary>
        [UnityTest]
        public IEnumerator RealScene_MachineLock_SpawnNoForbiddenObjectsAndAllComponentsWired()
        {
#if UNITY_EDITOR
            MissionDefinition sceneMission =
                AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);
            MissionSession.Clear();
            MissionSession.Begin(sceneMission != null ? sceneMission : mission, DifficultyLevel.Guided, true);

            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                MachineLockScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            while (load != null && !load.isDone)
                yield return null;

            Scene scene = SceneManager.GetSceneByPath(MachineLockScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = SceneManager.GetSceneByName("SCN_MachineLock");
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True,
                "SCN_MachineLock must load additively.");

            Exception failure = null;
            try
            {
                VerifyRealScene(scene);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            while (unload != null && !unload.isDone)
                yield return null;
            MissionSession.Clear();

            if (failure != null)
                ExceptionDispatchInfo.Capture(failure).Throw();
#else
            Assert.Ignore("The real scene verification needs the Editor scene loading API.");
            yield return null;
#endif
        }

        // =====================================================================
        // Real scene verification
        // =====================================================================

#if UNITY_EDITOR
        /// <summary>
        /// Checks the loaded scenario scene: spawn marker, empty controller spawn reference, forbidden
        /// objects, runtime components and layout patterns A/B/C.
        /// </summary>
        private void VerifyRealScene(Scene scene)
        {
            GameObject manager = FindRoot(scene, "MNG_Scenario");
            Assert.That(manager, Is.Not.Null, "MNG_Scenario is missing from SCN_MachineLock.");
            var scenario = manager.GetComponent<MachineLockScenario>();
            Assert.That(scenario, Is.Not.Null, "MNG_Scenario has no MachineLockScenario component.");
            Assert.That(scenario.Config, Is.Not.Null, "MNG_Scenario must keep its SO_MachineLockConfig.");

            // The scene starts its own HUD wait; initialise the test state explicitly so the layout
            // patterns can be exercised without waiting for SCN_Core.
            scenario.InitializeForTests(scenario.Config, DifficultyLevel.Guided);

            GameObject spawn = FindRoot(scene, "MNG_SpawnPoint");
            Assert.That(spawn, Is.Not.Null, "MNG_SpawnPoint is missing from SCN_MachineLock.");
            Assert.That(spawn.GetComponent<SpawnPoint>(), Is.Not.Null, "MNG_SpawnPoint has no SpawnPoint component.");
            Assert.That(Vector3.Distance(spawn.transform.position, new Vector3(-7.6f, 0f, 0f)), Is.LessThan(Tolerance),
                "MNG_SpawnPoint must stay at (-7.6, 0, 0) in the expanded stamping bay.");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(spawn.transform.eulerAngles.y, 90f)), Is.LessThan(0.1f),
                "MNG_SpawnPoint must face 90 degrees.");

            Transform building = FindInScene(scene, "MESH_WorkshopShell");
            Assert.That(building, Is.Not.Null, "MESH_WorkshopShell must be present in SCN_MachineLock (ADR-0005).");
            Assert.That(FindInScene(scene, "PFB_OldIndustrialBuilding"), Is.Null,
                "PFB_OldIndustrialBuilding must be replaced by MESH_WorkshopShell.");
            Assert.That(FindInScene(scene, "MESH_DoorRollHousing"), Is.Null,
                "MESH_DoorRollHousing must be removed from the west exit door.");

            var serializedScenario = new SerializedObject(scenario);
            SerializedProperty spawnReference = serializedScenario.FindProperty("spawnPoint");
            Assert.That(spawnReference, Is.Not.Null, "ScenarioController has no serialized spawnPoint field.");
            Assert.That(spawnReference.objectReferenceValue, Is.Null,
                "ScenarioController.spawnPoint must stay empty: MNG_SpawnPoint owns the offset.");

            int cameras = 0;
            int listeners = 0;
            int canvases = 0;
            int eventSystems = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    if (camera != null && camera.targetTexture == null) cameras++;
                listeners += root.GetComponentsInChildren<AudioListener>(true).Length;
                canvases += root.GetComponentsInChildren<Canvas>(true).Length;
                eventSystems += root.GetComponentsInChildren<EventSystem>(true).Length;
            }

            Assert.That(cameras, Is.EqualTo(0), "The scenario scene must not contain a camera: SCN_Core owns it.");
            Assert.That(listeners, Is.EqualTo(0), "The scenario scene must not contain an AudioListener.");
            Assert.That(canvases, Is.EqualTo(0), "The scenario scene must not contain a Canvas.");
            Assert.That(eventSystems, Is.EqualTo(0), "The scenario scene must not contain an EventSystem.");

            Assert.That(CountComponentsInScene<MachineLockPress>(scene), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountComponentsInScene<MachineLockEnergyPanel>(scene), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountComponentsInScene<MachineLockAlarm>(scene), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountComponentsInScene<MachineLockPpeCabinet>(scene), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountComponentsInScene<MachineLockPpeItem>(scene), Is.GreaterThanOrEqualTo(4));
            Assert.That(CountComponentsInScene<MachineLockExtinguisher>(scene), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountComponentsInScene<MachineLockValve>(scene), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountComponentsInScene<MachineLockSpillSystem>(scene), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountComponentsInScene<MachineLockCone>(scene), Is.GreaterThanOrEqualTo(4));
            Assert.That(CountComponentsInScene<MachineLockAbsorbent>(scene), Is.GreaterThanOrEqualTo(4));
            Assert.That(CountComponentsInScene<MachineLockSnapZone>(scene), Is.GreaterThanOrEqualTo(8));
            Assert.That(CountComponentsInScene<MachineLockObservationTarget>(scene), Is.EqualTo(ObservationIds.Length));
            Assert.That(CountComponentsInScene<MachineLockDamageFeedback>(scene), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountComponentsInScene<MachineLockActionFeedback>(scene), Is.EqualTo(1),
                "MNG_Scenario needs MachineLockActionFeedback wired with positive/negative cues (Issue #46).");
            Assert.That(CountComponentsInScene<MachineLockExitDoor>(scene), Is.EqualTo(1),
                "West exit door needs a MachineLockExitDoor component (ADR-0006).");
            Assert.That(CountComponentsInScene<ScenarioTrigger>(scene), Is.EqualTo(0),
                "Legacy walk-into ScenarioTrigger must be removed in favour of MachineLockExitDoor.");

            foreach (string qName in new[] { "MESH_PuddleQuadrantNE", "MESH_PuddleQuadrantNW", "MESH_PuddleQuadrantSE", "MESH_PuddleQuadrantSW" })
            {
                Transform q = FindInScene(scene, qName);
                Assert.That(q, Is.Not.Null, qName + " is missing from VFX_OilPuddle.");
                MeshRenderer mr = q.GetComponentInChildren<MeshRenderer>(true);
                Assert.That(mr, Is.Not.Null, qName + " must have a MeshRenderer.");
                Assert.That(mr.sharedMaterial != null && mr.sharedMaterial.name == "MAT_Oil", Is.True,
                    qName + " must render with MAT_Oil.");
            }

            Transform cabinet = FindInScene(scene, "MESH_PpeCabinet");
            Transform spillKit = FindInScene(scene, "MESH_SpillKit");
            Transform extinguisher = FindInScene(scene, "MESH_WaterExtinguisher");
            Assert.That(cabinet, Is.Not.Null, "MESH_PpeCabinet is missing from the scene.");
            Assert.That(spillKit, Is.Not.Null, "MESH_SpillKit is missing from the scene.");
            Assert.That(extinguisher, Is.Not.Null, "MESH_WaterExtinguisher is missing from the scene.");

            float previousCabinetX = float.NaN;
            for (int index = 0; index < 3; index++)
            {
                scenario.SetForcedPatternIndex(index);
                scenario.ApplyLayoutPattern(index);

                Assert.That(Convert.ToInt32(scenario.ActivePatternIndex), Is.EqualTo(index),
                    "ApplyLayoutPattern must activate the requested pattern.");

                char suffix = (char)('A' + index);
                Transform pattern = FindInScene(scene, "PATTERN_" + suffix);
                Assert.That(pattern, Is.Not.Null, "PATTERN_" + suffix + " is missing from MNG_LayoutPatterns.");

                AssertMarker(pattern, "MarkerPpeCabinet", cabinet);
                AssertMarker(pattern, "MarkerSpillKit", spillKit);
                AssertMarker(pattern, "MarkerExtinguisher", extinguisher);

                Assert.That(Mathf.Abs(cabinet.position.x), Is.LessThanOrEqualTo(9f));
                Assert.That(Mathf.Abs(cabinet.position.z), Is.LessThanOrEqualTo(5.5f));
                Assert.That(Mathf.Abs(spillKit.position.x), Is.LessThanOrEqualTo(9f));
                Assert.That(Mathf.Abs(spillKit.position.z), Is.LessThanOrEqualTo(5.5f));
                Assert.That(Mathf.Abs(extinguisher.position.x), Is.LessThanOrEqualTo(9f));
                Assert.That(Mathf.Abs(extinguisher.position.z), Is.LessThanOrEqualTo(5.5f));

                Assert.That(Mathf.Approximately(cabinet.position.x, previousCabinetX), Is.False,
                    "Every pattern must move the PPE cabinet.");
                previousCabinetX = cabinet.position.x;
            }
        }

        /// <summary>
        /// Asserts that a prop sits on the marker of the active pattern.
        /// </summary>
        private static void AssertMarker(Transform pattern, string markerName, Transform prop)
        {
            Transform marker = null;
            for (int i = 0; i < pattern.childCount; i++)
            {
                if (pattern.GetChild(i).name == markerName)
                {
                    marker = pattern.GetChild(i);
                    break;
                }
            }

            Assert.That(marker, Is.Not.Null, markerName + " is missing from " + pattern.name + ".");
            Assert.That(Vector3.Distance(marker.position, prop.position), Is.LessThan(0.01f),
                prop.name + " is not on " + markerName + " of " + pattern.name + ".");
        }
#endif

        // =====================================================================
        // Fixtures and builders
        // =====================================================================

        /// <summary>Press under test with its display renderer, materials and ram.</summary>
        private sealed class PressFixture
        {
            public MachineLockPress Press;
            public Renderer Display;
            public Material FaultMaterial;
            public Material OffMaterial;
            public Transform Ram;
        }

        /// <summary>Electrical panel under test with its three Sub-Station interactables and digital meter.</summary>
        private sealed class PanelFixture
        {
            public MachineLockEnergyPanel Panel;
            public Interactable MainSwitch;
            public Interactable Lockout;
            public Interactable Tester;
            public MachineLockDigitalMeterDisplay Meter;
        }

        /// <summary>Alarm under test with its beacon light.</summary>
        private sealed class AlarmFixture
        {
            public MachineLockAlarm Alarm;
            public Light Beacon;
        }

        /// <summary>One PPE item with its interactable, equipped badge and Italian display name.</summary>
        private sealed class PpeItemFixture
        {
            public MachineLockPpeItem Item;
            public Interactable Interactable;
            public GameObject EquippedBadge;
            public string DisplayName;
        }

        /// <summary>PPE cabinet under test with the 4 correct items, 2 distractors and door collider.</summary>
        private sealed class CabinetFixture
        {
            public MachineLockPpeCabinet Cabinet;
            public Collider DoorCollider;
            public PpeItemFixture[] CorrectItems;
            public PpeItemFixture HeatGloves;
            public PpeItemFixture FaceShield;
            public PpeItemFixture SafetyBoots;
            public PpeItemFixture LatexGloves;
            public PpeItemFixture SurgicalMask;
        }

        /// <summary>One cone with its interactable.</summary>
        private sealed class ConeFixture
        {
            public MachineLockCone Cone;
            public Interactable Interactable;
        }

        /// <summary>One absorbent with its interactable.</summary>
        private sealed class AbsorbentFixture
        {
            public MachineLockAbsorbent Absorbent;
            public Interactable Interactable;
        }

        /// <summary>Spill system under test with its zones, kit and hazard trigger.</summary>
        private sealed class SpillFixture
        {
            public MachineLockSpillSystem Spill;
            public GameObject Jet;
            public BoxCollider HazardTrigger;
            public MachineLockSnapZone[] ConeZones;
            public MachineLockSnapZone[] AbsorbentZones;
            public ConeFixture[] Cones;
            public AbsorbentFixture[] Absorbents;
        }

        /// <summary>Valve under test with its wheel.</summary>
        private sealed class ValveFixture
        {
            public MachineLockValve Valve;
            public Transform Wheel;
        }

        /// <summary>Free zones with their guided ghosts and proactive baseline markers.</summary>
        private sealed class GhostFixture
        {
            public MachineLockSpillSystem Spill;
            public MachineLockSnapZone[] ConeZones;
            public MachineLockSnapZone AbsorbentZone;
            public GameObject[] ConeGhosts;
            public GameObject AbsorbentGhost;
        }

        /// <summary>PPE setup of the hazard priority test.</summary>
        private enum HazardPpe
        {
            None,
            Wrong,
            Full
        }

        /// <summary>Spill system plus the energy panel and the PPE cabinet that decide the hazard.</summary>
        private sealed class HazardFixture
        {
            public MachineLockScenario Scenario;
            public MachineLockSpillSystem Spill;
            public MachineLockEnergyPanel Panel;
            public MachineLockPpeCabinet Cabinet;
        }

        /// <summary>
        /// Creates a scenario component with a clean test session. The standalone-editor core load is
        /// disabled before activation, so a failed session setup can never pull SCN_Core into the test.
        /// </summary>
        private MachineLockScenario CreateScenario(DifficultyLevel level)
        {
            MissionSession.Begin(mission, level, true);

            GameObject root = NewRoot("TEST_Scenario");
            var scenario = root.AddComponent<MachineLockScenario>();
            WireObject(scenario, "mission", mission);
            WireBool(scenario, "loadCoreInEditor", false);
            WireInt(scenario, "editorLevel", (int)level);
            root.SetActive(true);
            scenario.InitializeForTests(config, level);
            return scenario;
        }

        /// <summary>Builds a press with a moving ram and a fault display with two materials.</summary>
        private PressFixture CreatePress(MachineLockScenario scenario)
        {
            GameObject root = NewRoot("TEST_Press");

            GameObject ramObject = new GameObject("MESH_HydraulicPress_Ram");
            ramObject.transform.SetParent(root.transform, false);
            ramObject.transform.localPosition = new Vector3(0f, 1f, 0f);

            GameObject displayObject = new GameObject("MESH_PressDisplay");
            displayObject.transform.SetParent(root.transform, false);
            var display = displayObject.AddComponent<MeshRenderer>();

            Material faultMaterial = CreateMaterial();
            Material offMaterial = CreateMaterial();

            var press = root.AddComponent<MachineLockPress>();
            WireObject(press, "scenario", scenario);
            WireObject(press, "ramTransform", ramObject.transform);
            WireObject(press, "displayRenderer", display);
            WireObject(press, "displayFaultMaterial", faultMaterial);
            WireObject(press, "displayOffMaterial", offMaterial);
            root.SetActive(true);

            return new PressFixture
            {
                Press = press,
                Display = display,
                FaultMaterial = faultMaterial,
                OffMaterial = offMaterial,
                Ram = ramObject.transform
            };
        }

        /// <summary>Builds an electrical panel with its three interactables and its handle.</summary>
        private PanelFixture CreatePanel(MachineLockScenario scenario, MachineLockPress press)
        {
            GameObject root = NewRoot("TEST_ElectricalPanel");

            GameObject switchObject = new GameObject("MESH_DisconnectSwitch");
            switchObject.transform.SetParent(root.transform, false);
            var mainSwitch = switchObject.AddComponent<Interactable>();

            GameObject lockoutObject = new GameObject("MESH_LockoutPoint");
            lockoutObject.transform.SetParent(root.transform, false);
            var lockout = lockoutObject.AddComponent<Interactable>();

            GameObject testerObject = new GameObject("MESH_VoltageTester");
            testerObject.transform.SetParent(root.transform, false);
            var tester = testerObject.AddComponent<Interactable>();

            GameObject meterQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            meterQuad.name = "MESH_VoltageTesterScreen";
            meterQuad.transform.SetParent(root.transform, false);
            var meter = meterQuad.AddComponent<MachineLockDigitalMeterDisplay>();
            WireObject(meter, "screenRenderer", meterQuad.GetComponent<Renderer>());

            var panel = root.AddComponent<MachineLockEnergyPanel>();
            WireObject(panel, "scenario", scenario);
            WireObject(panel, "config", config);
            WireObject(panel, "press", press);
            WireObject(panel, "mainSwitchInteractable", mainSwitch);
            WireObject(panel, "lockoutInteractable", lockout);
            WireObject(panel, "testerInteractable", tester);
            WireObject(panel, "digitalMeterDisplay", meter);
            WireObject(panel, "switchHandle", switchObject.transform);
            root.SetActive(true);

            return new PanelFixture
            {
                Panel = panel,
                MainSwitch = mainSwitch,
                Lockout = lockout,
                Tester = tester,
                Meter = meter
            };
        }

        /// <summary>Builds the department alarm with its beacon light.</summary>
        private AlarmFixture CreateAlarm(MachineLockScenario scenario)
        {
            GameObject root = NewRoot("TEST_AlarmButton");

            GameObject beaconObject = new GameObject("VFX_AlarmBeacon");
            beaconObject.transform.SetParent(root.transform, false);
            var beacon = beaconObject.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.intensity = 2f;

            var alarm = root.AddComponent<MachineLockAlarm>();
            WireObject(alarm, "scenario", scenario);
            WireObject(alarm, "beaconLight", beacon);
            root.SetActive(true);

            return new AlarmFixture { Alarm = alarm, Beacon = beacon };
        }

        /// <summary>Builds the water extinguisher.</summary>
        private MachineLockExtinguisher CreateExtinguisher(MachineLockScenario scenario)
        {
            GameObject root = NewRoot("TEST_WaterExtinguisher");
            var extinguisher = root.AddComponent<MachineLockExtinguisher>();
            WireObject(extinguisher, "scenario", scenario);
            root.SetActive(true);
            return extinguisher;
        }

        /// <summary>
        /// Builds the PPE cabinet with the four correct items (Gloves, Body, Face, Feet), a non-blocking
        /// door collider, and optionally the latex gloves (Gloves) and surgical mask (Face) distractors.
        /// </summary>
        private CabinetFixture CreateCabinet(MachineLockScenario scenario, bool includeWrongItems)
        {
            GameObject root = NewRoot("TEST_PpeCabinet");
            GameObject doorObj = new GameObject("MESH_PpeCabinet_Door");
            doorObj.transform.SetParent(root.transform, false);
            BoxCollider doorCollider = doorObj.AddComponent<BoxCollider>();
            Interactable doorInteractable = doorObj.AddComponent<Interactable>();

            var correct = new List<PpeItemFixture>();
            correct.Add(AddPpeItem(root, "ITEM_HeatGloves", "Guanti anticalore EN 407"));
            correct.Add(AddPpeItem(root, "ITEM_Apron", "Grembiule antischizzo"));
            correct.Add(AddPpeItem(root, "ITEM_FaceShield", "Visiera facciale EN 166"));
            correct.Add(AddPpeItem(root, "ITEM_SafetyBoots", "Scarpe antinfortunistiche S3"));

            PpeItemFixture latex = null;
            PpeItemFixture mask = null;
            if (includeWrongItems)
            {
                latex = AddPpeItem(root, "ITEM_LatexGloves", "Guanti in lattice monouso");
                mask = AddPpeItem(root, "ITEM_SurgicalMask", "Mascherina chirurgica");
            }

            var cabinet = root.AddComponent<MachineLockPpeCabinet>();
            WireObject(cabinet, "scenario", scenario);
            WireObject(cabinet, "cabinetInteractable", doorInteractable);
            WireObject(cabinet, "doorTransform", doorObj.transform);
            root.SetActive(true);

            ConfigureItem(correct[0], cabinet, PpeSlot.Gloves, true);
            ConfigureItem(correct[1], cabinet, PpeSlot.Body, true);
            ConfigureItem(correct[2], cabinet, PpeSlot.Face, true);
            ConfigureItem(correct[3], cabinet, PpeSlot.Feet, true);
            if (latex != null) ConfigureItem(latex, cabinet, PpeSlot.Gloves, false);
            if (mask != null) ConfigureItem(mask, cabinet, PpeSlot.Face, false);

            return new CabinetFixture
            {
                Cabinet = cabinet,
                DoorCollider = doorCollider,
                CorrectItems = correct.ToArray(),
                HeatGloves = correct[0],
                FaceShield = correct[2],
                SafetyBoots = correct[3],
                LatexGloves = latex,
                SurgicalMask = mask
            };
        }

        /// <summary>Adds one selectable PPE item child with a shelf equipped badge.</summary>
        private static PpeItemFixture AddPpeItem(GameObject cabinetRoot, string name, string displayName)
        {
            GameObject itemObject = new GameObject(name);
            itemObject.transform.SetParent(cabinetRoot.transform, false);
            itemObject.AddComponent<BoxCollider>();
            var interactable = itemObject.AddComponent<Interactable>();
            GameObject visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(itemObject.transform, false);
            GameObject badge = new GameObject("EquippedBadge");
            badge.transform.SetParent(itemObject.transform, false);
            badge.SetActive(false);
            var item = itemObject.AddComponent<MachineLockPpeItem>();
            item.ConfigureShelfVisuals(visualRoot, badge);
            return new PpeItemFixture { Item = item, Interactable = interactable, EquippedBadge = badge, DisplayName = displayName };
        }

        /// <summary>Assigns slot, correctness and display name after the cabinet exists.</summary>
        private static void ConfigureItem(PpeItemFixture fixture, MachineLockPpeCabinet cabinet,
            PpeSlot slot, bool correct)
        {
            fixture.Item.Configure(cabinet, slot, correct, fixture.DisplayName);
        }

        /// <summary>
        /// Builds the spill system with eight zones, the puddle trigger and, when asked, the four cones
        /// and four absorbents of the spill kit.
        /// </summary>
        private SpillFixture CreateSpill(MachineLockScenario scenario, bool withKit)
        {
            GameObject root = NewRoot("TEST_HazardOil");

            Vector3[] conePositions =
            {
                new Vector3(3.8f, 0f, 2.8f),
                new Vector3(-3.8f, 0f, 2.8f),
                new Vector3(3.8f, 0f, -2.8f),
                new Vector3(-3.8f, 0f, -2.8f)
            };
            Vector3[] absorbentPositions =
            {
                new Vector3(1.75f, 0.02f, 1.25f),
                new Vector3(-1.75f, 0.02f, 1.25f),
                new Vector3(1.75f, 0.02f, -1.25f),
                new Vector3(-1.75f, 0.02f, -1.25f)
            };

            var coneZones = new MachineLockSnapZone[4];
            var absorbentZones = new MachineLockSnapZone[4];
            for (int i = 0; i < 4; i++)
            {
                coneZones[i] = AddZone(root, "ZONE_ConePoint" + (i + 1), conePositions[i],
                    MachineLockSnapZone.SnapKind.Cone, -1, null);
                absorbentZones[i] = AddZone(root, "ZONE_Coverage" + (i + 1), absorbentPositions[i],
                    MachineLockSnapZone.SnapKind.Absorbent, i, null);
            }

            GameObject hazardObject = new GameObject("ZONE_PuddleHazard");
            hazardObject.transform.SetParent(root.transform, false);
            hazardObject.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var hazardTrigger = hazardObject.AddComponent<BoxCollider>();
            hazardTrigger.size = new Vector3(7f, 0.5f, 5f);
            hazardTrigger.isTrigger = true;

            GameObject jet = new GameObject("VFX_OilJet");
            jet.transform.SetParent(root.transform, false);
            jet.transform.localPosition = new Vector3(1.95f, 0.9f, 0.5f);

            var cones = new List<ConeFixture>();
            var absorbents = new List<AbsorbentFixture>();
            if (withKit)
            {
                for (int i = 0; i < 4; i++)
                {
                    ConeFixture cone = AddCone(root, "MESH_Cone" + (i + 1), conePositions[i]);
                    cones.Add(cone);
                    AbsorbentFixture absorbent = AddAbsorbent(root, "MESH_Absorbent" + (i + 1),
                        absorbentPositions[i], i % 2 == 0 ? MachineLockAbsorbent.AbsorbentKind.Mat
                            : MachineLockAbsorbent.AbsorbentKind.Sock);
                    absorbents.Add(absorbent);
                }
            }

            var spill = root.AddComponent<MachineLockSpillSystem>();
            WireObject(spill, "scenario", scenario);
            WireObject(spill, "config", config);
            WireObject(spill, "oilJetVfx", jet);
            WireObject(spill, "puddleHazardTrigger", hazardTrigger);
            root.SetActive(true);

            // The kit items live under the same root: wire the spill system explicitly after activation.
            for (int i = 0; i < cones.Count; i++)
            {
                WireObject(cones[i].Cone, "spillSystem", spill);
                WireObject(cones[i].Cone, "config", config);
            }
            for (int i = 0; i < absorbents.Count; i++)
            {
                WireObject(absorbents[i].Absorbent, "spillSystem", spill);
                WireObject(absorbents[i].Absorbent, "config", config);
            }

            return new SpillFixture
            {
                Spill = spill,
                Jet = jet,
                HazardTrigger = hazardTrigger,
                ConeZones = coneZones,
                AbsorbentZones = absorbentZones,
                Cones = cones.ToArray(),
                Absorbents = absorbents.ToArray()
            };
        }

        /// <summary>Adds one snap zone child to the hazard root.</summary>
        private static MachineLockSnapZone AddZone(GameObject parent, string name, Vector3 position,
            MachineLockSnapZone.SnapKind kind, int quadrant, GameObject ghost, GameObject baseline = null)
        {
            GameObject zoneObject = new GameObject(name);
            zoneObject.transform.SetParent(parent.transform, false);
            zoneObject.transform.localPosition = position;
            var zone = zoneObject.AddComponent<MachineLockSnapZone>();
            zone.Configure(kind, null, ghost, quadrant, baseline);
            return zone;
        }

        /// <summary>Adds one cone item child to the hazard root.</summary>
        private static ConeFixture AddCone(GameObject parent, string name, Vector3 position)
        {
            GameObject coneObject = new GameObject(name);
            coneObject.transform.SetParent(parent.transform, false);
            coneObject.transform.localPosition = position;
            var interactable = coneObject.AddComponent<Interactable>();
            var cone = coneObject.AddComponent<MachineLockCone>();
            return new ConeFixture { Cone = cone, Interactable = interactable };
        }

        /// <summary>Adds one absorbent item child to the hazard root.</summary>
        private static AbsorbentFixture AddAbsorbent(GameObject parent, string name, Vector3 position,
            MachineLockAbsorbent.AbsorbentKind kind)
        {
            GameObject absorbentObject = new GameObject(name);
            absorbentObject.transform.SetParent(parent.transform, false);
            absorbentObject.transform.localPosition = position;
            var interactable = absorbentObject.AddComponent<Interactable>();
            var absorbent = absorbentObject.AddComponent<MachineLockAbsorbent>();
            WireEnum(absorbent, "absorbentKind", (int)kind);
            return new AbsorbentFixture { Absorbent = absorbent, Interactable = interactable };
        }

        /// <summary>Builds a cone released outside every zone, for the stay-grabbable check.</summary>
        private ConeFixture CreateCone(MachineLockScenario scenario, MachineLockSpillSystem spill, Vector3 position)
        {
            GameObject coneObject = new GameObject("MESH_Cone_Loose");
            roots.Add(coneObject);
            coneObject.transform.position = position;
            var interactable = coneObject.AddComponent<Interactable>();
            var cone = coneObject.AddComponent<MachineLockCone>();
            WireObject(cone, "spillSystem", spill);
            WireObject(cone, "config", config);
            return new ConeFixture { Cone = cone, Interactable = interactable };
        }

        /// <summary>Builds the leak valve with its wheel, turning instantly for the test.</summary>
        private ValveFixture CreateValve(MachineLockScenario scenario, MachineLockSpillSystem spill)
        {
            GameObject root = NewRoot("TEST_Valve");
            root.transform.position = new Vector3(5.2f, 1f, -0.5f);

            GameObject wheelObject = new GameObject("MESH_Valve_Wheel");
            wheelObject.transform.SetParent(root.transform, false);

            var valve = root.AddComponent<MachineLockValve>();
            WireObject(valve, "scenario", scenario);
            WireObject(valve, "spillSystem", spill);
            WireObject(valve, "wheelTransform", wheelObject.transform);
            WireFloat(valve, "rotationSeconds", 0f);
            root.SetActive(true);

            return new ValveFixture { Valve = valve, Wheel = wheelObject.transform };
        }

        /// <summary>Builds a spill system whose zones own guided ghost objects and proactive baseline rings.</summary>
        private GhostFixture CreateGhostZones(MachineLockScenario scenario)
        {
            GameObject root = NewRoot("TEST_GhostZones");

            GameObject coneGhostA = AddGhost(root, "VFX_SnapGhostCone1");
            GameObject coneGhostB = AddGhost(root, "VFX_SnapGhostCone2");
            GameObject absorbentGhost = AddGhost(root, "VFX_SnapGhostAbsorbent");
            GameObject coneBaseA = AddGhost(root, "VFX_BaselineCone1");
            GameObject coneBaseB = AddGhost(root, "VFX_BaselineCone2");
            GameObject absorbentBase = AddGhost(root, "VFX_BaselineAbsorbent");

            MachineLockSnapZone coneZoneA = AddZone(root, "ZONE_ConePoint1", new Vector3(3.8f, 0f, 2.8f),
                MachineLockSnapZone.SnapKind.Cone, -1, coneGhostA, coneBaseA);
            MachineLockSnapZone coneZoneB = AddZone(root, "ZONE_ConePoint2", new Vector3(-3.8f, 0f, 2.8f),
                MachineLockSnapZone.SnapKind.Cone, -1, coneGhostB, coneBaseB);
            MachineLockSnapZone absorbentZone = AddZone(root, "ZONE_Coverage1", new Vector3(1.75f, 0.02f, 1.25f),
                MachineLockSnapZone.SnapKind.Absorbent, 0, absorbentGhost, absorbentBase);

            var spill = root.AddComponent<MachineLockSpillSystem>();
            WireObject(spill, "scenario", scenario);
            WireObject(spill, "config", config);
            root.SetActive(true);

            return new GhostFixture
            {
                Spill = spill,
                ConeZones = new[] { coneZoneA, coneZoneB },
                AbsorbentZone = absorbentZone,
                ConeGhosts = new[] { coneGhostA, coneGhostB },
                AbsorbentGhost = absorbentGhost
            };
        }

        /// <summary>Adds an invisible ghost object, hidden by the zone at start.</summary>
        private static GameObject AddGhost(GameObject parent, string name)
        {
            GameObject ghost = new GameObject(name);
            ghost.transform.SetParent(parent.transform, false);
            return ghost;
        }

        /// <summary>
        /// Builds a scenario, an energy panel, a PPE cabinet and a spill system wired together, which is
        /// what the puddle contact severity depends on.
        /// </summary>
        private HazardFixture CreateHazardRig(HazardPpe ppe, bool energized)
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PanelFixture panel = CreatePanel(scenario, null);
            CabinetFixture cabinet = CreateCabinet(scenario, true);
            SpillFixture spill = CreateSpill(scenario, false);

            WireObject(spill.Spill, "energyPanel", panel.Panel);
            WireObject(spill.Spill, "ppeCabinet", cabinet.Cabinet);

            panel.Panel.InitializeState(energized);

            if (ppe != HazardPpe.None)
            {
                cabinet.Cabinet.Open();
                if (ppe == HazardPpe.Wrong)
                {
                    cabinet.Cabinet.Equip(cabinet.LatexGloves.Item);
                }
                else
                {
                    for (int i = 0; i < cabinet.CorrectItems.Length; i++)
                        cabinet.Cabinet.Equip(cabinet.CorrectItems[i].Item);
                }
            }

            return new HazardFixture
            {
                Scenario = scenario,
                Spill = spill.Spill,
                Panel = panel.Panel,
                Cabinet = cabinet.Cabinet
            };
        }

        /// <summary>Snaps the four cones and the four absorbents of the kit on their zones.</summary>
        private static void PlaceAllContainment(SpillFixture spill)
        {
            for (int i = 0; i < spill.Cones.Length; i++)
                Assert.That(spill.Cones[i].Cone.TrySnap(), Is.True, "Cone " + (i + 1) + " must snap.");
            for (int i = 0; i < spill.Absorbents.Length; i++)
                Assert.That(spill.Absorbents[i].Absorbent.TrySnap(), Is.True, "Absorbent " + (i + 1) + " must snap.");
        }

        /// <summary>Builds one observation target and configures it against the scenario.</summary>
        private MachineLockObservationTarget CreateObservationTarget(string id, Vector3 position)
        {
            GameObject targetObject = new GameObject("OBS_" + id);
            roots.Add(targetObject);
            targetObject.transform.position = position;
            var target = targetObject.AddComponent<MachineLockObservationTarget>();
            target.Configure(id, FindScenario(), config);
            return target;
        }

        /// <summary>Finds the single scenario under test.</summary>
        private MachineLockScenario FindScenario()
        {
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == null) continue;
                var scenario = roots[i].GetComponent<MachineLockScenario>();
                if (scenario != null) return scenario;
            }

            Assert.Fail("No MachineLockScenario under test.");
            return null;
        }

        /// <summary>Invokes the internal HoverEnter/HoverExit transitions on a shared Interactable.</summary>
        private static void InvokeInteractableHover(Interactable interactable, Hand hand)
        {
            Assert.That(interactable, Is.Not.Null);
            MethodInfo exit = typeof(Interactable).GetMethod("HoverExit", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo enter = typeof(Interactable).GetMethod("HoverEnter", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(enter, Is.Not.Null, "Interactable.HoverEnter was not found.");
            if (exit != null) exit.Invoke(interactable, new object[] { hand });
            enter.Invoke(interactable, new object[] { hand });
        }

        /// <summary>Invokes the internal HoverExit transition on a shared Interactable.</summary>
        private static void InvokeInteractableHoverExit(Interactable interactable, Hand hand)
        {
            Assert.That(interactable, Is.Not.Null);
            MethodInfo exit = typeof(Interactable).GetMethod("HoverExit", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(exit, Is.Not.Null, "Interactable.HoverExit was not found.");
            exit.Invoke(interactable, new object[] { hand });
        }

        /// <summary>Minimal in-memory IUISettingsStore for testing accessibility.reduce_flashes.</summary>
        private sealed class MemorySettingsStore : IUISettingsStore
        {
            private readonly Dictionary<string, bool> bools = new Dictionary<string, bool>();
            private readonly Dictionary<string, float> floats = new Dictionary<string, float>();
            private readonly Dictionary<string, int> ints = new Dictionary<string, int>();

            public event Action<string> Changed;

            public bool GetBool(string id) => bools.TryGetValue(id, out bool v) && v;
            public void SetBool(string id, bool value) { bools[id] = value; Changed?.Invoke(id); }
            public float GetFloat(string id) => floats.TryGetValue(id, out float v) ? v : 0f;
            public void SetFloat(string id, float value) { floats[id] = value; Changed?.Invoke(id); }
            public int GetInt(string id) => ints.TryGetValue(id, out int v) ? v : 0;
            public void SetInt(string id, int value) { ints[id] = value; Changed?.Invoke(id); }
        }

        /// <summary>Invokes the protected timer callback of the base scenario controller.</summary>
        private static void InvokeTimerExpiry(MachineLockScenario scenario)
        {
            MethodInfo timerExpired = typeof(ScenarioController).GetMethod(
                "OnTimerExpired", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(timerExpired, Is.Not.Null, "ScenarioController.OnTimerExpired was not found.");
            timerExpired.Invoke(scenario, null);
        }

        // =====================================================================
        // Debrief helpers
        // =====================================================================

        /// <summary>true when the capped debrief of the run contains the resolved line of an id.</summary>
        private static bool HasDebrief(MachineLockScenario scenario, DebriefLineId id)
        {
            return CountDebrief(scenario, id) > 0;
        }

        /// <summary>Counts how many times a debrief id appears in the capped list of the run.</summary>
        private static int CountDebrief(MachineLockScenario scenario, DebriefLineId id)
        {
            if (scenario == null || scenario.Spi == null) return 0;
            string text = MachineLockTexts.DebriefLine(id);
            if (string.IsNullOrEmpty(text)) return 0;

            List<string> lines = scenario.Spi.BuildDebriefLines(MachineLockTexts.DebriefLine);
            int count = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]) && lines[i].EndsWith(text, StringComparison.Ordinal))
                    count++;
            }
            return count;
        }

        /// <summary>Smoke stage as an integer, whatever the underlying numeric type is.</summary>
        private static int SmokeStage(MachineLockScenario scenario)
        {
            return Convert.ToInt32(scenario.SmokeStage);
        }

        // =====================================================================
        // Scene helpers
        // =====================================================================

        /// <summary>Finds a root object by exact name in a loaded scene.</summary>
        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        /// <summary>Finds the first transform with an exact name anywhere in a loaded scene.</summary>
        private static Transform FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                    if (transforms[i].name == name) return transforms[i];
            }
            return null;
        }

        /// <summary>Counts the components of a type, including inactive objects, in a loaded scene.</summary>
        private static int CountComponentsInScene<T>(Scene scene) where T : Component
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<T>(true).Length;
            return count;
        }

        // =====================================================================
        // Object and wiring helpers
        // =====================================================================

        /// <summary>Creates an inactive root, so Awake runs after the test wires its references.</summary>
        private GameObject NewRoot(string name)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            roots.Add(root);
            return root;
        }

        /// <summary>Tracks a ScriptableObject or a Material so TearDown can destroy it.</summary>
        private T TrackAsset<T>(T value) where T : UnityEngine.Object
        {
            assets.Add(value);
            return value;
        }

        /// <summary>Creates a material on a shader that exists in the editor and in a player build.</summary>
        private Material CreateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null, "No shader available for the press display materials.");
            return TrackAsset(new Material(shader));
        }

        /// <summary>Destroys every GameObject created by the test and clears the list.</summary>
        private void DestroyTrackedObjects()
        {
            for (int i = roots.Count - 1; i >= 0; i--)
            {
                if (roots[i] != null) UnityEngine.Object.DestroyImmediate(roots[i]);
            }
            roots.Clear();
        }

        /// <summary>Assigns a serialized object reference, through SerializedObject in the Editor.</summary>
        private static void WireObject(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
#if UNITY_EDITOR
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null,
                "Missing serialized field '" + fieldName + "' on " + target.GetType().Name + ".");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#else
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.That(field, Is.Not.Null,
                "Missing field '" + fieldName + "' on " + target.GetType().Name + ".");
            field.SetValue(target, value);
#endif
        }

        /// <summary>Assigns a serialized bool.</summary>
        private static void WireBool(UnityEngine.Object target, string fieldName, bool value)
        {
#if UNITY_EDITOR
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null,
                "Missing serialized field '" + fieldName + "' on " + target.GetType().Name + ".");
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#else
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.That(field, Is.Not.Null,
                "Missing field '" + fieldName + "' on " + target.GetType().Name + ".");
            field.SetValue(target, value);
#endif
        }

        /// <summary>Assigns a serialized int.</summary>
        private static void WireInt(UnityEngine.Object target, string fieldName, int value)
        {
#if UNITY_EDITOR
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null,
                "Missing serialized field '" + fieldName + "' on " + target.GetType().Name + ".");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#else
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.That(field, Is.Not.Null,
                "Missing field '" + fieldName + "' on " + target.GetType().Name + ".");
            field.SetValue(target, field.FieldType.IsEnum ? Enum.ToObject(field.FieldType, value) : value);
#endif
        }

        /// <summary>Assigns a serialized float.</summary>
        private static void WireFloat(UnityEngine.Object target, string fieldName, float value)
        {
#if UNITY_EDITOR
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null,
                "Missing serialized field '" + fieldName + "' on " + target.GetType().Name + ".");
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#else
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.That(field, Is.Not.Null,
                "Missing field '" + fieldName + "' on " + target.GetType().Name + ".");
            field.SetValue(target, Convert.ChangeType(value, field.FieldType));
#endif
        }

        /// <summary>Assigns a serialized enum from its underlying integer value.</summary>
        private static void WireEnum(UnityEngine.Object target, string fieldName, int value)
        {
#if UNITY_EDITOR
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null,
                "Missing serialized field '" + fieldName + "' on " + target.GetType().Name + ".");
            property.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#else
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.That(field, Is.Not.Null,
                "Missing field '" + fieldName + "' on " + target.GetType().Name + ".");
            field.SetValue(target, Enum.ToObject(field.FieldType, value));
#endif
        }

        /// <summary>Finds a private field on a type or on one of its base types.</summary>
        private static FieldInfo FindField(Type type, string fieldName)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            return null;
        }
    }
}
