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
            config.energyDecaySeconds = 0.5f;
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            yield return null;

            panel.Panel.InitializeState(true);
            Assert.That(panel.Panel.MainPowerOn, Is.True);
            Assert.That(panel.Panel.OilEnergized, Is.True);
            Assert.That(press.Press.IsPowered, Is.True);
            Assert.That(panel.Lockout.Selectable, Is.False, "The lockout point is locked while the panel is live.");
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
            Assert.That(cabinet.Cabinet.HasWrongPpeEquipped, Is.False, "The correct respirator replaced the surgical mask.");
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
        /// The exit is blocked while the press runs; after the E-stop a handover without the alarm and
        /// without containment is Partial and carries both missing-task penalties.
        /// </summary>
        [UnityTest]
        public IEnumerator Flow_ExitBlockedWhilePressRunning_MissingAlarmAndContainmentYieldPartial()
        {
            MachineLockScenario scenario = CreateScenario(DifficultyLevel.Guided);
            PressFixture press = CreatePress(scenario);
            PanelFixture panel = CreatePanel(scenario, press.Press);
            CabinetFixture cabinet = CreateCabinet(scenario, false);
            yield return null;

            scenario.NotifyExitReached();

            Assert.That(scenario.TaskExit, Is.False, "The exit is blocked while the press runs.");
            Assert.That(scenario.Spi.TaskExit, Is.False);
            Assert.That(scenario.HasEnded, Is.False);

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
            Assert.That(Vector3.Distance(spawn.transform.position, new Vector3(-5.2f, 0f, 0f)), Is.LessThan(Tolerance),
                "MNG_SpawnPoint must stay at (-5.2, 0, 0).");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(spawn.transform.eulerAngles.y, 90f)), Is.LessThan(0.1f),
                "MNG_SpawnPoint must face 90 degrees.");

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
            Assert.That(CountComponentsInScene<ScenarioTrigger>(scene), Is.GreaterThanOrEqualTo(1),
                "ZONE_Exit needs a ScenarioTrigger wired to NotifyExitReached.");

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

                Assert.That(Mathf.Abs(cabinet.position.x), Is.LessThanOrEqualTo(6f));
                Assert.That(Mathf.Abs(cabinet.position.z), Is.LessThanOrEqualTo(4f));
                Assert.That(Mathf.Abs(spillKit.position.x), Is.LessThanOrEqualTo(6f));
                Assert.That(Mathf.Abs(spillKit.position.z), Is.LessThanOrEqualTo(4f));
                Assert.That(Mathf.Abs(extinguisher.position.x), Is.LessThanOrEqualTo(6f));
                Assert.That(Mathf.Abs(extinguisher.position.z), Is.LessThanOrEqualTo(4f));

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

        /// <summary>Electrical panel under test with its lockout interactable.</summary>
        private sealed class PanelFixture
        {
            public MachineLockEnergyPanel Panel;
            public Interactable Lockout;
        }

        /// <summary>Alarm under test with its beacon light.</summary>
        private sealed class AlarmFixture
        {
            public MachineLockAlarm Alarm;
            public Light Beacon;
        }

        /// <summary>One PPE item with its interactable and its Italian display name.</summary>
        private sealed class PpeItemFixture
        {
            public MachineLockPpeItem Item;
            public Interactable Interactable;
            public string DisplayName;
        }

        /// <summary>PPE cabinet under test with the correct set and the wrong decoys.</summary>
        private sealed class CabinetFixture
        {
            public MachineLockPpeCabinet Cabinet;
            public PpeItemFixture[] CorrectItems;
            public PpeItemFixture HeatGloves;
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

        /// <summary>Free zones with their guided ghosts.</summary>
        private sealed class GhostFixture
        {
            public MachineLockSpillSystem Spill;
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

            var panel = root.AddComponent<MachineLockEnergyPanel>();
            WireObject(panel, "scenario", scenario);
            WireObject(panel, "config", config);
            WireObject(panel, "press", press);
            WireObject(panel, "mainSwitchInteractable", mainSwitch);
            WireObject(panel, "lockoutInteractable", lockout);
            WireObject(panel, "testerInteractable", tester);
            WireObject(panel, "switchHandle", switchObject.transform);
            root.SetActive(true);

            return new PanelFixture { Panel = panel, Lockout = lockout };
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
        /// Builds the PPE cabinet with the four correct items and, optionally, the latex gloves and the
        /// surgical mask decoys.
        /// </summary>
        private CabinetFixture CreateCabinet(MachineLockScenario scenario, bool includeWrongItems)
        {
            GameObject root = NewRoot("TEST_PpeCabinet");
            var correct = new List<PpeItemFixture>();

            correct.Add(AddPpeItem(root, "ITEM_HeatGloves", "Guanti termici"));
            correct.Add(AddPpeItem(root, "ITEM_Apron", "Grembiule"));
            correct.Add(AddPpeItem(root, "ITEM_FaceShield", "Visiera"));
            correct.Add(AddPpeItem(root, "ITEM_Respirator", "FFP3"));

            PpeItemFixture latex = null;
            PpeItemFixture mask = null;
            if (includeWrongItems)
            {
                latex = AddPpeItem(root, "ITEM_LatexGloves", "Guanti in lattice");
                mask = AddPpeItem(root, "ITEM_SurgicalMask", "Mascherina chirurgica");
            }

            var cabinet = root.AddComponent<MachineLockPpeCabinet>();
            WireObject(cabinet, "scenario", scenario);
            root.SetActive(true);

            ConfigureItem(correct[0], cabinet, PpeSlot.Gloves, true);
            ConfigureItem(correct[1], cabinet, PpeSlot.Body, true);
            ConfigureItem(correct[2], cabinet, PpeSlot.Face, true);
            ConfigureItem(correct[3], cabinet, PpeSlot.Respiratory, true);
            if (latex != null) ConfigureItem(latex, cabinet, PpeSlot.Gloves, false);
            if (mask != null) ConfigureItem(mask, cabinet, PpeSlot.Respiratory, false);

            return new CabinetFixture
            {
                Cabinet = cabinet,
                CorrectItems = correct.ToArray(),
                HeatGloves = correct[0],
                LatexGloves = latex,
                SurgicalMask = mask
            };
        }

        /// <summary>Adds one selectable PPE item child, before the cabinet collects its children.</summary>
        private static PpeItemFixture AddPpeItem(GameObject cabinetRoot, string name, string displayName)
        {
            GameObject itemObject = new GameObject(name);
            itemObject.transform.SetParent(cabinetRoot.transform, false);
            var interactable = itemObject.AddComponent<Interactable>();
            var item = itemObject.AddComponent<MachineLockPpeItem>();
            return new PpeItemFixture { Item = item, Interactable = interactable, DisplayName = displayName };
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
            MachineLockSnapZone.SnapKind kind, int quadrant, GameObject ghost)
        {
            GameObject zoneObject = new GameObject(name);
            zoneObject.transform.SetParent(parent.transform, false);
            zoneObject.transform.localPosition = position;
            var zone = zoneObject.AddComponent<MachineLockSnapZone>();
            zone.Configure(kind, null, ghost, quadrant);
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

        /// <summary>Builds a spill system whose zones own guided ghost objects.</summary>
        private GhostFixture CreateGhostZones(MachineLockScenario scenario)
        {
            GameObject root = NewRoot("TEST_GhostZones");

            GameObject coneGhostA = AddGhost(root, "VFX_SnapGhostCone1");
            GameObject coneGhostB = AddGhost(root, "VFX_SnapGhostCone2");
            GameObject absorbentGhost = AddGhost(root, "VFX_SnapGhostAbsorbent");

            AddZone(root, "ZONE_ConePoint1", new Vector3(3.8f, 0f, 2.8f),
                MachineLockSnapZone.SnapKind.Cone, -1, coneGhostA);
            AddZone(root, "ZONE_ConePoint2", new Vector3(-3.8f, 0f, 2.8f),
                MachineLockSnapZone.SnapKind.Cone, -1, coneGhostB);
            AddZone(root, "ZONE_Coverage1", new Vector3(1.75f, 0.02f, 1.25f),
                MachineLockSnapZone.SnapKind.Absorbent, 0, absorbentGhost);

            var spill = root.AddComponent<MachineLockSpillSystem>();
            WireObject(spill, "scenario", scenario);
            WireObject(spill, "config", config);
            root.SetActive(true);

            return new GhostFixture
            {
                Spill = spill,
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
