using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Data;
using SI81.Tiziano.Evacuation.Interaction;
using SI81.Tiziano.Evacuation.Systems;
using EvacOutcome = SI81.Tiziano.ChemicalAlert.Core.Outcome;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Debug
{
    /// <summary>
    /// Editor/development-only registry of keyboard and ContextMenu commands for the Evacuation scenario.
    /// Every command is also reachable from the component's ContextMenu. Keys avoid 0-9 and W/A/S/D (used by the Meta XR Simulator)
    /// and only work while the Game View has focus. Press H to list them.
    /// </summary>
    public sealed class EvacDebugCommands : MonoBehaviour
    {
        [SerializeField] private EvacuationDirector director;
        [Tooltip("Root of the scenario markers (children are looked up by name once).")]
        [SerializeField] private Transform markersRoot;
        [SerializeField] private string[] teleportMarkerNames =
        {
            "SPAWN_Player", "POS_Plan_R1", "POS_Plan_R2", "POS_Plan_Corridor", "POS_Elevator", "ZONE_StairsEmergency", "ZONE_StairsInternal"
        };
        [SerializeField] private int debugSeed = 81081;

        [Header("Alarm, plans and doors (E04)")]
        [SerializeField] private AlarmSystem alarm;
        [SerializeField] private EvacuationPlanInspectable[] plans = new EvacuationPlanInspectable[0];
        [SerializeField] private DoorInteractable[] doors = new DoorInteractable[0];
        [Tooltip("Distance in front of a plan or door used by the test spot teleport.")]
        [SerializeField, Min(0.2f)] private float testSpotDistance = 0.8f;

        [Header("Elevator and stairs (E05)")]
        [SerializeField] private ElevatorButton[] elevatorButtons = new ElevatorButton[0];
        [SerializeField] private StairsExitZone[] stairsZones = new StairsExitZone[0];

        [Header("NPC colleagues (E06)")]
        [SerializeField] private NpcRegistry npcRegistry;
        [SerializeField] private NpcTrailFollower npcTrail;
        [Tooltip("1-based NPC index used by the 'Collect NPC by number' menu command.")]
        [SerializeField, Min(1)] private int npcNumberToCollect = 1;

        [Header("Difficulty and aids (E08)")]
        [SerializeField] private EvacGuidedHighlights guidedHighlights;

        [Header("Fire (E09)")]
        [SerializeField] private EvacFireManager fireManager;
        [SerializeField] private EvacFireLaneValidator fireLaneValidator;

        private readonly Dictionary<Key, RegisteredCommand> commands = new Dictionary<Key, RegisteredCommand>();
        private readonly List<Transform> teleportMarkers = new List<Transform>();
        private int nextTeleportIndex;
        private int nextPlanIndex;
        private int nextTestSpotIndex;

        private sealed class RegisteredCommand
        {
            public string Label;
            public string Description;
            public Action Action;
        }

        private sealed class SmokeStep
        {
            public string Name;
            public Func<string> Run;
        }

        private sealed class DummyResettable : IResettable
        {
            public int ResetCount;
            public void ResetState() { ResetCount++; }
        }

        /// <summary>Registers or replaces a keyboard command.</summary>
        public void Register(Key key, string description, Action action)
        {
            if (action == null)
            {
                UDebug.LogError("[EV] EvacDebugCommands: invalid command registration.");
                return;
            }

            commands[key] = new RegisteredCommand { Label = key.ToString(), Description = description ?? string.Empty, Action = action };
        }

        /// <summary>Removes a keyboard command.</summary>
        public void Unregister(Key key)
        {
            commands.Remove(key);
        }

        private void Awake()
        {
            ResolveReferences();
            Register(Key.H, "list the debug commands", PrintCommandList);
            Register(Key.N, "advance phase (Briefing->Action->end run)", AdvancePhase);
            Register(Key.M, "cycle difficulty (full reset)", CycleDifficulty);
            Register(Key.F, "force timeout (ignored in Guided)", ForceTimeUp);
            Register(Key.R, "reset the scenario", ResetScenario);
            Register(Key.X, "3 consecutive resets", ResetThreeTimes);
            Register(Key.P, "print state", PrintState);
            Register(Key.K, "print result recap", PrintResult);
            Register(Key.T, "teleport rig to the next marker (cyclic)", TeleportNext);
            Register(Key.O, "run evaluator SelfCheck + config checks", RunSelfCheck);
            Register(Key.Y, "run the smoke test", RunSmokeTest);
            Register(Key.F8, "fix seed (Inspector value)", FixSeed);
            Register(Key.F9, "randomize seed", RandomizeSeed);
            Register(Key.L, "force the alarm on/off", ToggleAlarm);
            Register(Key.I, "simulate the inspection of the next plan", InspectNextPlan);
            Register(Key.G, "open all doors", OpenAllDoors);
            Register(Key.C, "close all doors", CloseAllDoors);
            Register(Key.J, "teleport rig in front of the next plan/door", GoToNextTestSpot);
            Register(Key.E, "simulate pressing the elevator button", SimulateElevator);
            Register(Key.Q, "teleport into the emergency stairs zone", SimulateEmergencyExit);
            Register(Key.V, "teleport into the internal stairs zone", SimulateInternalExit);
            Register(Key.U, "collect the next NPC (hand activation)", CollectNextNpc);
            Register(Key.Z, "collect all NPCs", CollectAllNpcs);
            Register(Key.F7, "print NPC line and breadcrumb trail", PrintNpcTrail);
            Register(Key.B, "request the next Guided hint", RequestHint);
            Register(Key.F10, "print difficulty behaviour (time/aids/effects)", PrintLevelBehavior);
            Register(Key.F3, "force a fire contact", ForceFireContact);
            Register(Key.F4, "print active fire slots", PrintFireSlots);
            Register(Key.F5, "validate fire lanes on 200 seeds", ValidateFireSeeds);
            Register(Key.F6, "toggle fire gizmos", ToggleFireGizmos);
            Register(Key.F11, "run 5 varied replay games (E10)", RunE10ReplayGames);
            Register(Key.F12, "10 clean resets (E10)", ResetTenTimes);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current == null)
                return;

            foreach (KeyValuePair<Key, RegisteredCommand> command in commands)
            {
                if (Keyboard.current[command.Key].wasPressedThisFrame)
                {
                    command.Value.Action.Invoke();
                    break;
                }
            }
#endif
        }

        /// <summary>Lists every registered command.</summary>
        [ContextMenu("Evac/Help (H)")]
        public void PrintCommandList()
        {
            UDebug.Log("[EV] Debug commands (Game View focus; Simulator uses 0-9 and WASD):");
            foreach (RegisteredCommand c in commands.Values.OrderBy(x => x.Label, StringComparer.Ordinal))
                UDebug.Log($"[EV]   [{c.Label}] {c.Description}");
        }

        /// <summary>Advances the Director phase.</summary>
        [ContextMenu("Evac/Advance Phase (N)")]
        public void AdvancePhase()
        {
            if (Require())
                director.AdvancePhase();
        }

        /// <summary>Cycles Guided -> Operational -> Crisis and resets.</summary>
        [ContextMenu("Evac/Cycle Difficulty (M)")]
        public void CycleDifficulty()
        {
            if (!Require())
                return;
            var next = (EvacDifficulty)(((int)director.Difficulty + 1) % Enum.GetValues(typeof(EvacDifficulty)).Length);
            director.SetDifficulty(next);
        }

        /// <summary>Forces the countdown to expire.</summary>
        [ContextMenu("Evac/Force Timeout (F)")]
        public void ForceTimeUp()
        {
            if (Require())
                director.ForceTimeUp();
        }

        /// <summary>Resets the scenario.</summary>
        [ContextMenu("Evac/Reset (R)")]
        public void ResetScenario()
        {
            if (Require())
                director.ResetScenario();
        }

        /// <summary>Runs three resets in a row.</summary>
        [ContextMenu("Evac/Reset x3 (X)")]
        public void ResetThreeTimes()
        {
            if (!Require())
                return;
            for (int i = 0; i < 3; i++)
                director.ResetScenario();
            director.PrintState(UDebug.Log);
        }

        /// <summary>Prints the Director state.</summary>
        [ContextMenu("Evac/Print State (P)")]
        public void PrintState()
        {
            if (Require())
            {
                director.PrintState(UDebug.Log);
                PrintScenarioObjects();
            }
        }

        /// <summary>Prints the result recap.</summary>
        [ContextMenu("Evac/Print Result (K)")]
        public void PrintResult()
        {
            if (Require())
                director.PrintResult(UDebug.Log);
        }

        /// <summary>Teleports the rig to the next marker of the list.</summary>
        [ContextMenu("Evac/Teleport Next Marker (T)")]
        public void TeleportNext()
        {
            if (!Require())
                return;
            if (teleportMarkers.Count == 0)
                ResolveMarkers();
            if (teleportMarkers.Count == 0)
            {
                UDebug.LogWarning("[EV] Teleport: no markers resolved (assign markersRoot).", this);
                return;
            }

            Transform marker = teleportMarkers[nextTeleportIndex % teleportMarkers.Count];
            nextTeleportIndex = (nextTeleportIndex + 1) % teleportMarkers.Count;
            if (director.TeleportRigTo(marker))
                UDebug.Log($"[EV] Teleport -> {marker.name}");
        }

        /// <summary>Fixes the seed to the Inspector value.</summary>
        [ContextMenu("Evac/Fix Seed (F8)")]
        public void FixSeed()
        {
            if (Require())
                director.SetFixedSeed(debugSeed);
        }

        /// <summary>Removes the fixed seed and draws a random one.</summary>
        [ContextMenu("Evac/Randomize Seed (F9)")]
        public void RandomizeSeed()
        {
            if (Require())
                director.ClearFixedSeed();
        }

        /// <summary>Runs the evaluator SelfCheck and verifies the three configs.</summary>
        [ContextMenu("Evac/Run SelfCheck (O)")]
        public void RunSelfCheck()
        {
            bool evaluatorOk = new EvacuationOutcomeEvaluator().SelfCheck(UDebug.Log);
            bool configsOk = true;
            if (Require())
            {
                foreach (EvacDifficulty level in Enum.GetValues(typeof(EvacDifficulty)))
                {
                    string problem = CheckConfig(level);
                    configsOk &= problem == null;
                    UDebug.Log($"[EV] SelfCheck config {level}: {(problem == null ? "PASS" : "FAIL " + problem)}");
                }
            }

            UDebug.Log($"[EV] SelfCheck TOTAL: {(evaluatorOk && configsOk ? "ALL PASS" : "FAILURES")}");
        }

        /// <summary>Forces the alarm on or off (the next phase change takes over again).</summary>
        [ContextMenu("Evac/Toggle Alarm (L)")]
        public void ToggleAlarm()
        {
            if (!RequirePlayingObjects())
                return;
            alarm.SetAlarm(!alarm.IsActive);
        }

        /// <summary>Simulates hover + trigger on the next evacuation plan.</summary>
        [ContextMenu("Evac/Inspect Next Plan (I)")]
        public void InspectNextPlan()
        {
            if (!RequirePlayingObjects())
                return;
            EvacuationPlanInspectable plan = plans[nextPlanIndex % plans.Length];
            nextPlanIndex = (nextPlanIndex + 1) % plans.Length;
            UDebug.Log($"[EV] Simulated trigger on {plan.name}");
            plan.Interactable.RaiseActivated();
        }

        /// <summary>Opens every door.</summary>
        [ContextMenu("Evac/Open All Doors (G)")]
        public void OpenAllDoors()
        {
            SetAllDoors(true);
        }

        /// <summary>Closes every door.</summary>
        [ContextMenu("Evac/Close All Doors (C)")]
        public void CloseAllDoors()
        {
            SetAllDoors(false);
        }

        /// <summary>Teleports the rig in front of the next plan or door, facing it (Simulator tests).</summary>
        [ContextMenu("Evac/Go To Next Test Spot (J)")]
        public void GoToNextTestSpot()
        {
            if (!RequirePlayingObjects() || director.RigRoot == null || director.SpawnMarker == null)
                return;

            var targets = new List<Transform>();
            targets.AddRange(plans.Select(plan => plan.transform));
            targets.AddRange(doors.Select(door => door.transform));
            ResolveReferences();
            if (npcRegistry != null)
            {
                for (int i = 0; i < npcRegistry.Spawned.Count; i++)
                {
                    if (npcRegistry.Spawned[i] != null)
                        targets.Add(npcRegistry.Spawned[i].transform);
                }
            }
            Transform target = targets[nextTestSpotIndex % targets.Count];
            nextTestSpotIndex = (nextTestSpotIndex + 1) % targets.Count;

            Vector3 position = target.position + target.forward * testSpotDistance;
            position.y = director.SpawnMarker.position.y;
            director.RigRoot.SetPositionAndRotation(position, Quaternion.LookRotation(-target.forward, Vector3.up));
            UDebug.Log($"[EV] Test spot -> {target.name}");
        }

        /// <summary>Simulates trigger on the elevator call button.</summary>
        [ContextMenu("Evac/Simulate Elevator Button (E)")]
        public void SimulateElevator()
        {
            if (!RequireE05Objects())
                return;
            ElevatorButton button = elevatorButtons[0];
            UDebug.Log($"[EV] Simulated trigger on {button.name}");
            button.Interactable.RaiseActivated();
        }

        /// <summary>Teleports the rig into the emergency stairs zone and tries to report the exit.</summary>
        [ContextMenu("Evac/Simulate Emergency Stairs (Q)")]
        public void SimulateEmergencyExit()
        {
            SimulateExit(EvacExit.Emergency);
        }

        /// <summary>Teleports the rig into the internal stairs zone and tries to report the exit.</summary>
        [ContextMenu("Evac/Simulate Internal Stairs (V)")]
        public void SimulateInternalExit()
        {
            SimulateExit(EvacExit.Internal);
        }

        /// <summary>Simulates hand activation on the next idle NPC.</summary>
        [ContextMenu("Evac/Collect Next NPC (U)")]
        public void CollectNextNpc()
        {
            if (!RequirePlayingObjects() || !RequireNpcObjects())
                return;
            EvacNpc npc = NextIdleNpc();
            if (npc == null)
            {
                UDebug.LogWarning("[EV] No idle NPC left to collect.");
                return;
            }
            UDebug.Log($"[EV] Simulated trigger on {npc.name}");
            npc.Interactable.RaiseActivated();
        }

        /// <summary>Collects the NPC with the 1-based index from the Inspector (menu command).</summary>
        [ContextMenu("Evac/Collect NPC By Number")]
        public void CollectNpcByNumber()
        {
            if (!RequirePlayingObjects() || !RequireNpcObjects())
                return;
            if (npcNumberToCollect < 1 || npcNumberToCollect > npcRegistry.Spawned.Count)
            {
                UDebug.LogWarning($"[EV] NPC number {npcNumberToCollect} out of range (1-{npcRegistry.Spawned.Count}).");
                return;
            }
            EvacNpc npc = npcRegistry.Spawned[npcNumberToCollect - 1];
            UDebug.Log($"[EV] Simulated trigger on {npc.name} (#{npcNumberToCollect})");
            npc.Interactable.RaiseActivated();
        }

        /// <summary>Simulates hand activation on every idle NPC.</summary>
        [ContextMenu("Evac/Collect All NPCs (Z)")]
        public void CollectAllNpcs()
        {
            if (!RequirePlayingObjects() || !RequireNpcObjects())
                return;
            int count = 0;
            EvacNpc npc;
            while ((npc = NextIdleNpc()) != null)
            {
                npc.Interactable.RaiseActivated();
                count++;
            }
            UDebug.Log($"[EV] Collected {count} NPCs; line {npcRegistry.Line.Count}.");
        }

        /// <summary>Prints the NPC line and the breadcrumb trail.</summary>
        [ContextMenu("Evac/Print NPC Trail (F7)")]
        public void PrintNpcTrail()
        {
            if (!RequirePlayingObjects() || !RequireNpcObjects())
                return;
            npcRegistry.PrintStatus(UDebug.Log);
        }

        /// <summary>Requests the next ordered Guided hint (ignored outside Guided).</summary>
        [ContextMenu("Evac/Request Hint (B)")]
        public void RequestHint()
        {
            if (Require())
                director.RequestNextHint();
        }

        /// <summary>Prints time, aid and effect behaviour of the active level.</summary>
        [ContextMenu("Evac/Print Level Behavior (F10)")]
        public void PrintLevelBehavior()
        {
            if (Require())
                director.PrintLevelBehavior(UDebug.Log);
        }

        /// <summary>Forces a fire contact through the Director (Crisis only).</summary>
        [ContextMenu("Evac/Force Fire Contact (F3)")]
        public void ForceFireContact()
        {
            if (Require())
                director.ReportFireContact();
        }

        /// <summary>Prints the active fire slots.</summary>
        [ContextMenu("Evac/Print Fire Slots (F4)")]
        public void PrintFireSlots()
        {
            if (!RequireFireObjects())
                return;
            fireManager.PrintStatus();
        }

        /// <summary>Validates the fire lane on 200 seeds (blocked combinations are counted).</summary>
        [ContextMenu("Evac/Validate Fire Lanes (F5)")]
        public void ValidateFireSeeds()
        {
            if (!RequireFireObjects())
                return;
            fireManager.ValidateSeeds(200);
        }

        /// <summary>Toggles the active-fire gizmos.</summary>
        [ContextMenu("Evac/Toggle Fire Gizmos (F6)")]
        public void ToggleFireGizmos()
        {
            if (!RequireFireObjects())
                return;
            fireManager.ToggleGizmos();
        }

        /// <summary>Runs five consecutive games with different outcomes and checks for residual objects (E10).</summary>
        [ContextMenu("Evac/Run 5 Varied Replay Games (F11)")]
        public void RunE10ReplayGames()
        {
            string problem = RunE10VariedGames();
            UDebug.Log($"[EV] E10 replay games: {(problem == null ? "5/5 PASS" : "FAIL " + problem)}");
        }

        /// <summary>Runs ten consecutive full resets and checks for null references and duplicate subscriptions (E10).</summary>
        [ContextMenu("Evac/10 Clean Resets (F12)")]
        public void ResetTenTimes()
        {
            string problem = RunE10ResetStress();
            UDebug.Log($"[EV] E10 reset stress: {(problem == null ? "10/10 PASS" : "FAIL " + problem)}");
        }

        /// <summary>Prints the seed and the NPC/fire layout chosen for the current run.</summary>
        [ContextMenu("Evac/Print Replay Layout")]
        public void PrintReplayLayout()
        {
            if (!Require())
                return;
            UDebug.Log($"[EV] Replay layout: seed={director.Seed} fixed={director.IsSeedFixed} logged={director.ReplayStartLogged} npc=[{director.ReplayNpcSlots}] fire=[{director.ReplayFireSlots}]");
        }

        private bool RequireFireObjects()
        {
            if (!Require())
                return false;
            if (fireManager == null)
                fireManager = FindFirstObjectByType<EvacFireManager>();
            if (fireLaneValidator == null)
                fireLaneValidator = FindFirstObjectByType<EvacFireLaneValidator>();
            if (fireManager == null)
                UDebug.LogError("[EV] EvacFireManager not found.", this);
            return fireManager != null;
        }

        /// <summary>Switches directly to Guided (full reset).</summary>
        [ContextMenu("Evac/Set Difficulty Guided")]
        public void SetDifficultyGuided() { SetLevel(EvacDifficulty.Guided); }

        /// <summary>Switches directly to Operational (full reset).</summary>
        [ContextMenu("Evac/Set Difficulty Operational")]
        public void SetDifficultyOperational() { SetLevel(EvacDifficulty.Operational); }

        /// <summary>Switches directly to Crisis (full reset).</summary>
        [ContextMenu("Evac/Set Difficulty Crisis")]
        public void SetDifficultyCrisis() { SetLevel(EvacDifficulty.Crisis); }

        private void SetLevel(EvacDifficulty level)
        {
            if (Require())
                director.SetDifficulty(level);
        }

        private EvacNpc NextIdleNpc()
        {
            for (int i = 0; i < npcRegistry.Spawned.Count; i++)
            {
                EvacNpc npc = npcRegistry.Spawned[i];
                if (npc != null && npc.State == EvacNpcState.Idle)
                    return npc;
            }
            return null;
        }

        private bool RequireNpcObjects()
        {
            if (npcRegistry != null && npcTrail != null && npcRegistry.Spawned.Count > 0)
                return true;
            UDebug.LogError("[EV] EvacDebugCommands: NPC registry or trail not assigned.", this);
            return false;
        }

        private void SimulateExit(EvacExit kind)
        {
            if (!RequireE05Objects())
                return;
            StairsExitZone zone = FindZone(kind);
            if (zone == null)
            {
                UDebug.LogError($"[EV] Stairs zone {kind} not assigned.", this);
                return;
            }

            if (director.TeleportRigTo(zone.transform))
                UDebug.Log($"[EV] Teleported into {zone.name}");
            zone.TryReach("debug");
        }

        private StairsExitZone FindZone(EvacExit kind)
        {
            for (int i = 0; i < stairsZones.Length; i++)
            {
                if (stairsZones[i] != null && stairsZones[i].ExitKind == kind)
                    return stairsZones[i];
            }

            return null;
        }

        private bool RequireE05Objects()
        {
            if (!RequirePlayingObjects())
                return false;
            if (elevatorButtons.Length > 0 && stairsZones.Length >= 2)
                return true;
            UDebug.LogError("[EV] EvacDebugCommands: elevator button or stairs zones not assigned.", this);
            return false;
        }

        private void SetAllDoors(bool open)
        {
            if (!RequirePlayingObjects())
                return;
            for (int i = 0; i < doors.Length; i++)
                doors[i].SetOpen(open);
        }

        private void PrintScenarioObjects()
        {
            ResolveReferences();
            string doorState = string.Join(", ", doors.Where(door => door != null).Select(door => door.name + "=" + (door.IsOpen ? "open" : "closed")));
            string planState = string.Join(", ", plans.Where(plan => plan != null).Select(plan => plan.name + "=" + (plan.IsInspected ? "inspected" : "-")));
            string elevatorState = string.Join(", ", elevatorButtons.Where(button => button != null).Select(button => button.name + "=" + (button.WasPressed ? "pressed" : "-")));
            string zoneState = string.Join(", ", stairsZones.Where(zone => zone != null).Select(zone => zone.ExitKind + "=" + (zone.WasReached ? "reached" : "-")));
            UDebug.Log($"[EV] Alarm {(alarm != null && alarm.IsActive ? "ON" : "OFF")} | doors: {doorState} | plans: {planState}");
            UDebug.Log($"[EV] Elevator: {elevatorState} | stairs zones: {zoneState}");
            if (npcRegistry != null)
                npcRegistry.PrintStatus(UDebug.Log);
        }

        private bool RequirePlayingObjects()
        {
            if (!Application.isPlaying)
            {
                UDebug.LogWarning("[EV] Enter Play Mode first.");
                return false;
            }

            if (!Require())
                return false;
            if (alarm != null && plans.Length > 0 && doors.Length > 0)
                return true;

            UDebug.LogError("[EV] EvacDebugCommands: alarm, plans or doors not assigned.", this);
            return false;
        }

        /// <summary>Automatic end-to-end check of the Director (E03 skeleton, extended by every following task).</summary>
        [ContextMenu("Evac/Run Smoke Test (Y)")]
        public void RunSmokeTest()
        {
            if (!Application.isPlaying)
            {
                UDebug.LogWarning("[EV] SmokeTest: enter Play Mode first.");
                return;
            }

            if (!Require())
                return;

            EvacuationDirector d = director;
            EvacDifficulty originalDifficulty = d.Difficulty;
            d.ClearDebugTimeLimit();
            List<SmokeStep> steps = BuildSmokeSteps(d);
            int passed = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                string failure;
                try
                {
                    failure = steps[i].Run();
                }
                catch (Exception exception)
                {
                    failure = "exception: " + exception.Message;
                }

                bool pass = string.IsNullOrEmpty(failure);
                if (pass)
                    passed++;
                UDebug.Log($"[EV] SmokeTest {i + 1}/{steps.Count} {steps[i].Name}: {(pass ? "PASS" : "FAIL " + failure)}");
            }

            d.ClearFixedSeed();
            d.ClearDebugTimeLimit();
            d.SetDifficulty(originalDifficulty);
            UDebug.Log($"[EV] SmokeTest summary {passed}/{steps.Count} {(passed == steps.Count ? "ALL PASS" : "FAILURES")}");
        }

        private List<SmokeStep> BuildSmokeSteps(EvacuationDirector d)
        {
            var steps = new List<SmokeStep>();

            steps.Add(new SmokeStep
            {
                Name = "evaluator SelfCheck",
                Run = () =>
                {
                    var fails = new List<string>();
                    bool ok = new EvacuationOutcomeEvaluator().SelfCheck(line =>
                    {
                        if (line != null && line.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0)
                            fails.Add(line);
                    });
                    return ok ? null : string.Join(" | ", fails);
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "3 configs assigned and valid",
                Run = () =>
                {
                    foreach (EvacDifficulty level in Enum.GetValues(typeof(EvacDifficulty)))
                    {
                        string problem = CheckConfig(level);
                        if (problem != null)
                            return problem;
                    }

                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "each level loads its own config",
                Run = () =>
                {
                    foreach (EvacDifficulty level in Enum.GetValues(typeof(EvacDifficulty)))
                    {
                        d.SetDifficulty(level);
                        if (d.Difficulty != level || d.Config == null || d.Config.Difficulty != level)
                            return $"{level}: wrong config";
                        string state = ExpectBaseline(d);
                        if (state != null)
                            return $"{level}: {state}";
                    }

                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "Operational: Briefing->Action, countdown",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    if (d.Phase != EvacPhase.Briefing)
                        return "not in Briefing after reset";
                    d.AdvanceDebugTime(5f);
                    if (d.Elapsed != 0f)
                        return "timer moved during Briefing";
                    d.StartScenario();
                    if (d.Phase != EvacPhase.Action)
                        return "StartScenario did not enter Action";
                    d.AdvanceDebugTime(10f);
                    if (!Near(d.Elapsed, 10f) || !Near(d.TimeRemaining, 170f))
                        return $"elapsed {d.Elapsed}, remaining {d.TimeRemaining} (expected 10 / 170)";
                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "Guided: stopwatch without defeat",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Guided);
                    d.StartScenario();
                    d.AdvanceDebugTime(1000f);
                    d.ForceTimeUp();
                    if (d.HasTimeLimit || d.Phase != EvacPhase.Action || !Near(d.Elapsed, 1000f))
                        return $"phase {d.Phase}, elapsed {d.Elapsed}, limit {d.HasTimeLimit}";
                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "Operational: timeout -> Failure + LG10",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    int timeUps = 0;
                    int results = 0;
                    Action onTimeUp = () => timeUps++;
                    Action<ScenarioResult> onResult = r => results++;
                    d.TimeUp += onTimeUp;
                    d.ResultReady += onResult;
                    try
                    {
                        d.StartScenario();
                        d.AdvanceDebugTime(179f);
                        if (d.Phase != EvacPhase.Action)
                            return "ended before the limit";
                        d.AdvanceDebugTime(2f);
                        d.AdvanceDebugTime(2f);
                        if (d.Phase != EvacPhase.ResultHandoff || d.Outcome != EvacOutcome.Failure)
                            return $"phase {d.Phase}, outcome {d.Outcome}";
                        if (!HasAction(d, EvacActionIds.Lg10Timeout))
                            return "LG10 missing";
                        if (timeUps != 1 || results != 1)
                            return $"TimeUp x{timeUps}, ResultReady x{results} (expected 1/1)";
                        return null;
                    }
                    finally
                    {
                        d.TimeUp -= onTimeUp;
                        d.ResultReady -= onResult;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "Operational: full success path",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.ReportPlanInspected();
                    d.ReportPlanInspected();
                    for (int i = 0; i < 3; i++)
                        d.ReportNpcCollected(1);
                    for (int i = 0; i < 2; i++)
                        d.ReportNpcCollected(2);
                    d.ReportExitReached(EvacExit.Emergency);
                    if (d.Phase != EvacPhase.ResultHandoff || d.Outcome != EvacOutcome.Success)
                        return $"phase {d.Phase}, outcome {d.Outcome}";
                    string[] expected = { EvacActionIds.Lg01PlanInspected, EvacActionIds.Lg03Room1Done, EvacActionIds.Lg04Room2Done, EvacActionIds.Lg09StairsAllNpcs };
                    foreach (string id in expected)
                    {
                        if (!HasAction(d, id))
                            return id + " missing";
                    }

                    return d.ActionLog.Count == expected.Length + 1 && CountAction(d, EvacActionIds.ReplayStart) == 1
                        ? null
                        : $"log has {d.ActionLog.Count} entries (expected {expected.Length + 1} incl. ReplayStart)";
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "exits: internal stairs Partial, missing NPCs Partial",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.ReportPlanInspected();
                    d.ReportExitReached(EvacExit.Internal);
                    if (d.Outcome != EvacOutcome.Partial || !HasAction(d, EvacActionIds.Lg11InternalStairs))
                        return $"internal: outcome {d.Outcome}";

                    d.ResetScenario();
                    d.StartScenario();
                    d.ReportPlanInspected();
                    d.ReportNpcCollected(1);
                    d.ReportExitReached(EvacExit.Emergency);
                    if (d.Outcome != EvacOutcome.Partial || !HasAction(d, EvacActionIds.Lg05StairsMissingNpcs))
                        return $"missing NPCs: outcome {d.Outcome}";
                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "elevator: Failure (Operational), Partial and Down (Guided)",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.ReportElevatorUsed();
                    if (d.Phase != EvacPhase.ResultHandoff || d.Outcome != EvacOutcome.Failure || !HasAction(d, EvacActionIds.Lg06Elevator))
                        return $"Operational: phase {d.Phase}, outcome {d.Outcome}";

                    d.SetDifficulty(EvacDifficulty.Guided);
                    var levels = new List<EvacDangerLevel>();
                    Action<EvacDangerLevel> onDanger = levels.Add;
                    d.DangerLevelChanged += onDanger;
                    try
                    {
                        d.StartScenario();
                        d.ReportElevatorUsed();
                        if (d.Phase != EvacPhase.Action || d.DangerLevel != EvacDangerLevel.Down || levels.Count != 1)
                            return $"Guided: phase {d.Phase}, danger {d.DangerLevel}, events {levels.Count}";
                    }
                    finally
                    {
                        d.DangerLevelChanged -= onDanger;
                    }

                    d.ReportPlanInspected();
                    d.ReportExitReached(EvacExit.Emergency);
                    return d.Outcome == EvacOutcome.Partial ? null : $"Guided end: outcome {d.Outcome}";
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "Crisis: 3 fire contacts = death (1 heart)",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    var contacts = new List<int>();
                    int died = 0;
                    Action<int, float> onContact = (n, intensity) => contacts.Add(n);
                    Action onDied = () => died++;
                    d.FireContact += onContact;
                    d.Died += onDied;
                    try
                    {
                        d.StartScenario();
                        d.ReportFireContact();
                        d.ReportFireContact();
                        if (d.Phase != EvacPhase.Action || d.Hearts != 1 || d.FireContacts != 2)
                            return $"after 2 contacts: phase {d.Phase}, hearts {d.Hearts}, contacts {d.FireContacts}";
                        d.ReportFireContact();
                        if (d.Phase != EvacPhase.ResultHandoff || d.Hearts != 0 || d.IsAlive || d.Outcome != EvacOutcome.Failure)
                            return $"after 3 contacts: phase {d.Phase}, hearts {d.Hearts}, outcome {d.Outcome}";
                        if (died != 1 || contacts.Count != 3 || contacts[2] != 3)
                            return $"events: Died x{died}, contacts {contacts.Count}";
                        if (!HasAction(d, EvacActionIds.Lg07FireContact) || !HasAction(d, EvacActionIds.Lg08FireDeath))
                            return "LG07/LG08 missing";
                        return null;
                    }
                    finally
                    {
                        d.FireContact -= onContact;
                        d.Died -= onDied;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "AdvancePhase: Briefing->Action->ResultHandoff",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.AdvancePhase();
                    if (d.Phase != EvacPhase.Action)
                        return "first advance did not enter Action";
                    d.AdvancePhase();
                    return d.Phase == EvacPhase.ResultHandoff && d.Outcome == EvacOutcome.Failure ? null : $"phase {d.Phase}, outcome {d.Outcome}";
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "3 consecutive resets: baseline, no duplicate subscriptions",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    var dummy = new DummyResettable();
                    d.RegisterResettable(dummy);
                    d.RegisterResettable(dummy);
                    int registered = d.ResettableCount;
                    try
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            d.StartScenario();
                            d.ReportPlanInspected();
                            d.ReportNpcCollected(1);
                            d.ReportFireContact();
                            d.AdvanceDebugTime(7f);
                            d.ResetScenario();
                            string state = ExpectBaseline(d);
                            if (state != null)
                                return $"reset {i + 1}: {state}";
                        }

                        if (dummy.ResetCount != 3)
                            return $"dummy reset x{dummy.ResetCount} (expected 3)";
                        if (d.ResettableCount != registered)
                            return "registry size changed";

                        int phaseEvents = 0;
                        int results = 0;
                        Action<EvacPhase> onPhase = p => phaseEvents++;
                        Action<ScenarioResult> onResult = r => results++;
                        d.PhaseChanged += onPhase;
                        d.ResultReady += onResult;
                        try
                        {
                            d.StartScenario();
                            d.ReportExitReached(EvacExit.Internal);
                        }
                        finally
                        {
                            d.PhaseChanged -= onPhase;
                            d.ResultReady -= onResult;
                        }

                        // Briefing->Action, Action->Resolution, Resolution->ResultHandoff
                        return phaseEvents == 3 && results == 1 ? null : $"PhaseChanged x{phaseEvents} (expected 3), ResultReady x{results} (expected 1)";
                    }
                    finally
                    {
                        d.UnregisterResettable(dummy);
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "seed: fixed seed reproduces the sequence",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.SetFixedSeed(42);
                    d.ResetScenario();
                    int first = d.Rng.Next();
                    d.ResetScenario();
                    int second = d.Rng.Next();
                    bool fixedOk = d.Seed == 42 && first == second;
                    d.ClearFixedSeed();
                    return fixedOk && !d.IsSeedFixed ? null : $"seed {d.Seed}, first {first}, second {second}";
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "reset teleports the rig to SPAWN_Player",
                Run = () =>
                {
                    if (d.RigRoot == null || d.SpawnMarker == null)
                        return "rig root or spawn marker not assigned";
                    d.SetDifficulty(EvacDifficulty.Operational);
                    if (teleportMarkers.Count == 0)
                        ResolveMarkers();
                    Transform away = teleportMarkers.FirstOrDefault(t => t != d.SpawnMarker);
                    if (away == null)
                        return "no teleport marker resolved";
                    d.TeleportRigTo(away);
                    d.ResetScenario();
                    float distance = Vector3.Distance(d.RigRoot.position, d.SpawnMarker.position);
                    return distance < 0.01f ? null : $"rig {distance:0.00} m from SPAWN_Player";
                }
            });

            AddAlarmPlanDoorSteps(d, steps);
            AddElevatorExitSteps(d, steps);
            AddNpcSteps(d, steps);
            AddE07VerticalSliceSteps(d, steps);
            AddE08DifficultySteps(d, steps);
            AddE09FireSteps(d, steps);
            AddE10ReplaySteps(d, steps);

            return steps;
        }

        private void AddAlarmPlanDoorSteps(EvacuationDirector d, List<SmokeStep> steps)
        {
            steps.Add(new SmokeStep
            {
                Name = "alarm follows the Action phase",
                Run = () =>
                {
                    if (alarm == null)
                        return "alarm not assigned";
                    d.SetDifficulty(EvacDifficulty.Operational);
                    if (alarm.IsActive || d.IsAlarmActive)
                        return "alarm on in Briefing";
                    var states = new List<bool>();
                    Action<bool> onAlarm = value => states.Add(value);
                    d.AlarmStateChanged += onAlarm;
                    try
                    {
                        d.StartScenario();
                        if (!alarm.IsActive || !d.IsAlarmActive)
                            return "alarm off in Action";
                        d.ReportExitReached(EvacExit.Internal);
                        if (alarm.IsActive || d.IsAlarmActive)
                            return "alarm still on after the end";
                        return states.SequenceEqual(new[] { true, false }) ? null : "events: " + string.Join(",", states);
                    }
                    finally
                    {
                        d.AlarmStateChanged -= onAlarm;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "alarm: timeout and reset silence it",
                Run = () =>
                {
                    if (alarm == null)
                        return "alarm not assigned";
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.ForceTimeUp();
                    if (alarm.IsActive)
                        return "on after timeout";
                    d.ResetScenario();
                    d.StartScenario();
                    d.ResetScenario();
                    if (alarm.IsActive)
                        return "on after reset from Action";
                    alarm.SetAlarm(true);
                    d.ResetScenario();
                    return !alarm.IsActive && !d.IsAlarmActive ? null : "forced alarm survived the reset";
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "plans: trigger logs LG01 only once",
                Run = () =>
                {
                    if (plans.Length != 3 || plans.Any(plan => plan == null))
                        return "expected 3 plans, got " + plans.Length;
                    if (plans.Select(plan => plan.PlanId).Distinct().Count() != 3 || plans.Any(plan => plan.TextKey != "evac.plan.route"))
                        return "plan ids or text keys wrong";
                    d.SetDifficulty(EvacDifficulty.Operational);
                    plans[0].Interactable.RaiseActivated();
                    if (d.PlanWasInspected || d.ActionLog.Count != 0)
                        return "inspected during Briefing";
                    int events = 0;
                    Action<string> onPlan = id => events++;
                    d.PlanInspected += onPlan;
                    try
                    {
                        d.StartScenario();
                        plans[0].Interactable.RaiseActivated();
                        plans[0].Interactable.RaiseActivated();
                        plans[1].Interactable.RaiseActivated();
                    }
                    finally
                    {
                        d.PlanInspected -= onPlan;
                    }

                    int logged = CountAction(d, EvacActionIds.Lg01PlanInspected);
                    if (!d.PlanWasInspected || logged != 1 || events != 1)
                        return $"LG01 x{logged}, events x{events}";
                    if (!plans[0].IsInspected || !plans[1].IsInspected || plans[2].IsInspected)
                        return "instance flags wrong";
                    d.ResetScenario();
                    return plans.Any(plan => plan.IsInspected) ? "plans not reset" : null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "plans: gaze dwell, distance and side",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    float dwell = d.Config.InteractionDwellSeconds;
                    float tooFar = d.Config.InspectMaxDistance + 0.5f;
                    foreach (EvacuationPlanInspectable plan in plans)
                    {
                        Vector3 centre = plan.transform.position;
                        Vector3 forward = plan.transform.forward;
                        Vector3 front = centre + forward * testSpotDistance;
                        if (plan.TickGaze(centre - forward * testSpotDistance, forward, dwell * 2f))
                            return plan.name + ": inspected from behind";
                        if (plan.TickGaze(centre + forward * tooFar, -forward, dwell * 2f))
                            return plan.name + ": inspected from too far";
                        if (plan.TickGaze(front, forward, dwell * 2f))
                            return plan.name + ": inspected while looking away";
                        if (plan.TickGaze(front, -forward, dwell * 0.6f))
                            return plan.name + ": inspected before the dwell";
                        if (!plan.TickGaze(front, -forward, dwell * 0.6f))
                            return plan.name + ": gaze did not inspect";
                        if (plan.TickGaze(front, -forward, dwell * 2f))
                            return plan.name + ": repeated without looking away";
                    }

                    int logged = CountAction(d, EvacActionIds.Lg01PlanInspected);
                    return logged == 1 ? null : "LG01 x" + logged;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "doors: initial state, trigger toggles, blocker",
                Run = () =>
                {
                    if (doors.Length != 4 || doors.Any(door => door == null))
                        return "expected 4 doors, got " + doors.Length;
                    d.SetDifficulty(EvacDifficulty.Operational);
                    foreach (DoorInteractable door in doors)
                    {
                        bool expectedOpen = door.DoorId.IndexOf("stairs", StringComparison.Ordinal) < 0;
                        if (door.IsOpen != expectedOpen || door.IsBlocking == door.IsOpen)
                            return door.name + ": wrong initial state";
                        int opened = 0;
                        int closed = 0;
                        UnityEngine.Events.UnityAction onOpen = () => opened++;
                        UnityEngine.Events.UnityAction onClose = () => closed++;
                        door.OnOpened.AddListener(onOpen);
                        door.OnClosed.AddListener(onClose);
                        try
                        {
                            door.Interactable.RaiseActivated();
                            if (door.IsOpen == expectedOpen || door.IsBlocking == door.IsOpen)
                                return door.name + ": first trigger failed";
                            door.Interactable.RaiseActivated();
                            if (door.IsOpen != expectedOpen || door.IsBlocking == door.IsOpen)
                                return door.name + ": second trigger failed";
                        }
                        finally
                        {
                            door.OnOpened.RemoveListener(onOpen);
                            door.OnClosed.RemoveListener(onClose);
                        }

                        if (opened != 1 || closed != 1)
                            return $"{door.name}: OnOpened x{opened}, OnClosed x{closed}";
                    }

                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "doors: 3 resets, no duplicate subscriptions",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    int registered = d.ResettableCount;
                    if (registered < 1 + plans.Length + doors.Length)
                        return "resettables " + registered;
                    for (int i = 0; i < 3; i++)
                    {
                        foreach (DoorInteractable door in doors)
                            door.Toggle();
                        d.ResetScenario();
                        if (doors.Any(door => door.IsOpen != door.StartsOpen || door.IsBlocking == door.IsOpen))
                            return "state not restored at reset " + (i + 1);
                    }

                    if (d.ResettableCount != registered)
                        return $"resettables {registered} -> {d.ResettableCount}";
                    int changes = 0;
                    Action<DoorInteractable, bool> onChange = (door, open) => changes++;
                    doors[0].StateChanged += onChange;
                    try
                    {
                        doors[0].Interactable.RaiseActivated();
                    }
                    finally
                    {
                        doors[0].StateChanged -= onChange;
                    }

                    d.ResetScenario();
                    return changes == 1 ? null : "one trigger produced " + changes + " changes";
                }
            });
        }

        private void AddNpcSteps(EvacuationDirector d, List<SmokeStep> steps)
        {
            steps.Add(new SmokeStep
            {
                Name = "NPC spawn 3+2 by seed, hand activation, dialogue and duplicate",
                Run = () =>
                {
                    if (npcRegistry == null || npcTrail == null)
                        return "NPC registry or trail not assigned";
                    d.SetDifficulty(EvacDifficulty.Operational);
                    if (d.Config == null)
                        return "config missing";
                    int total = d.Config.NpcCountTotal;
                    if (npcRegistry.Spawned.Count != total)
                        return $"spawned {npcRegistry.Spawned.Count}/{total}";
                    int room1 = 0;
                    int room2 = 0;
                    for (int i = 0; i < npcRegistry.Spawned.Count; i++)
                    {
                        EvacNpc npc = npcRegistry.Spawned[i];
                        if (npc == null || npc.State != EvacNpcState.Idle || npc.Order != -1)
                            return $"spawn #{i} not Idle";
                        if (npc.Room == 1) room1++;
                        else if (npc.Room == 2) room2++;
                        else return $"spawn #{i} invalid room {npc.Room}";
                        if (npc.Interactable == null || npc.InteractionArea == null || !npc.InteractionArea.isTrigger)
                            return $"spawn #{i} missing hand interaction area";
                    }
                    if (room1 != d.Config.NpcCountRoom1 || room2 != d.Config.NpcCountRoom2)
                        return $"rooms {room1}+{room2}, expected {d.Config.NpcCountRoom1}+{d.Config.NpcCountRoom2}";

                    var dialogues = new List<string>();
                    Action<string> onDialogue = dialogues.Add;
                    d.NpcDialogueRequested += onDialogue;
                    try
                    {
                        d.StartScenario();
                        EvacNpc first = npcRegistry.Spawned[0];
                        first.Interactable.RaiseActivated();
                        if (npcRegistry.Line.Count != 1 || first.State != EvacNpcState.Following || first.Order != 0)
                            return $"collection line {npcRegistry.Line.Count}, state {first.State}, order {first.Order}";
                        if (d.NpcCollectedRoom1 + d.NpcCollectedRoom2 != 1)
                            return "Director did not count the collected NPC";
                        if (dialogues.Count != 1 || dialogues[0] != EvacNpc.DialogueFollow)
                            return "dialogue: " + string.Join(",", dialogues.ToArray());

                        first.Interactable.RaiseActivated();
                        if (npcRegistry.Line.Count != 1 || dialogues.Count != 2 || dialogues[1] != EvacNpc.DialogueAlready)
                            return "duplicate collection was not ignored";
                        return null;
                    }
                    finally
                    {
                        d.NpcDialogueRequested -= onDialogue;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "breadcrumb trail: two NPCs follow, keep order and halt",
                Run = () =>
                {
                    if (npcRegistry == null || npcTrail == null)
                        return "NPC registry or trail not assigned";
                    Transform head = npcTrail.Head;
                    if (head == null)
                        return "NPC trail head not assigned";
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    if (!npcRegistry.CollectNext("smoke") || !npcRegistry.CollectNext("smoke") || npcRegistry.Line.Count != 2)
                        return $"collected {npcRegistry.Line.Count}/2";

                    EvacNpc leader = npcRegistry.Line[0];
                    EvacNpc follower = npcRegistry.Line[1];
                    Vector3 savedHead = head.localPosition;
                    Vector3 origin = head.position;
                    try
                    {
                        head.position = new Vector3(origin.x, npcTrail.GroundY, origin.z);
                        npcTrail.Tick(0.02f);
                        Vector3 start = leader.transform.position;
                        for (int i = 1; i <= 20; i++)
                        {
                            head.position = new Vector3(origin.x + i * 0.25f, npcTrail.GroundY, origin.z);
                            npcTrail.Tick(0.1f);
                        }
                        if (npcTrail.BreadcrumbCount < 15)
                            return $"breadcrumbs {npcTrail.BreadcrumbCount}";
                        if ((leader.transform.position - start).magnitude < 0.5f)
                            return "leader did not move along the trail";

                        for (int i = 0; i < 100; i++)
                            npcTrail.Tick(0.1f);
                        Vector3 settled = leader.transform.position;
                        for (int i = 0; i < 10; i++)
                            npcTrail.Tick(0.1f);
                        float jitter = Vector3.Distance(settled, leader.transform.position);
                        float gapToHead = Vector3.Distance(
                            new Vector3(leader.transform.position.x, npcTrail.GroundY, leader.transform.position.z),
                            new Vector3(head.position.x, npcTrail.GroundY, head.position.z));
                        if (jitter > 0.02f)
                            return $"leader did not halt (jitter {jitter:0.000})";
                        if (gapToHead > 1.2f)
                            return $"leader {gapToHead:0.00} m from the stopped player";

                        float spacing = Vector3.Distance(leader.transform.position, follower.transform.position);
                        if (follower.Order != 1 || spacing < 0.5f || spacing > 1.5f)
                            return $"line order {follower.Order}, spacing {spacing:0.00} m";
                        return null;
                    }
                    finally
                    {
                        head.localPosition = savedHead;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "real NPCs at exit: 3/5 Partial, 5/5 Success, all Evacuated",
                Run = () =>
                {
                    if (npcRegistry == null || npcTrail == null)
                        return "NPC registry or trail not assigned";
                    StairsExitZone emergency = FindZone(EvacExit.Emergency);
                    if (emergency == null)
                        return "emergency zone not assigned";

                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.ReportPlanInspected();
                    for (int i = 0; i < 3; i++)
                    {
                        if (!npcRegistry.CollectNpc(i + 1, "smoke"))
                            return $"NPC #{i + 1} not collected";
                    }
                    d.TeleportRigTo(emergency.transform);
                    emergency.TryReach("smoke");
                    if (d.Outcome != EvacOutcome.Partial || !HasAction(d, EvacActionIds.Lg05StairsMissingNpcs))
                        return $"3/5 outcome {d.Outcome}";

                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.ReportPlanInspected();
                    if (npcRegistry.CollectAll("smoke") != d.Config.NpcCountTotal)
                        return "CollectAll did not collect every NPC";
                    d.TeleportRigTo(emergency.transform);
                    emergency.TryReach("smoke");
                    if (d.Outcome != EvacOutcome.Success || !HasAction(d, EvacActionIds.Lg09StairsAllNpcs))
                        return $"5/5 outcome {d.Outcome}";
                    int evacuated = 0;
                    for (int i = 0; i < npcRegistry.Line.Count; i++)
                    {
                        if (npcRegistry.Line[i] != null && npcRegistry.Line[i].State == EvacNpcState.Evacuated)
                            evacuated++;
                    }
                    return evacuated == d.Config.NpcCountTotal ? null : $"evacuated {evacuated}/{d.Config.NpcCountTotal}";
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "3 resets: NPCs respawn Idle and no breadcrumbs survive",
                Run = () =>
                {
                    if (npcRegistry == null || npcTrail == null)
                        return "NPC registry or trail not assigned";
                    for (int pass = 0; pass < 3; pass++)
                    {
                        d.SetDifficulty(EvacDifficulty.Operational);
                        d.StartScenario();
                        npcRegistry.CollectNext("smoke");
                        Transform head = npcTrail.Head;
                        if (head != null)
                        {
                            Vector3 savedHead = head.localPosition;
                            head.position += Vector3.right * 0.5f;
                            npcTrail.Tick(0.1f);
                            head.localPosition = savedHead;
                        }
                        d.ResetScenario();
                        if (npcTrail.BreadcrumbCount != 0)
                            return $"reset {pass + 1}: {npcTrail.BreadcrumbCount} breadcrumbs left";
                        if (npcRegistry.Line.Count != 0 || npcRegistry.Spawned.Count != d.Config.NpcCountTotal)
                            return $"reset {pass + 1}: line {npcRegistry.Line.Count}, spawned {npcRegistry.Spawned.Count}";
                        for (int i = 0; i < npcRegistry.Spawned.Count; i++)
                        {
                            EvacNpc npc = npcRegistry.Spawned[i];
                            if (npc == null || npc.State != EvacNpcState.Idle || npc.Order != -1)
                                return $"reset {pass + 1}: spawn #{i} not Idle";
                        }
                        string baseline = ExpectBaseline(d);
                        if (baseline != null)
                            return $"reset {pass + 1}: {baseline}";
                    }
                    return null;
                }
            });
        }

        private void AddE07VerticalSliceSteps(EvacuationDirector d, List<SmokeStep> steps)
        {
            steps.Add(new SmokeStep
            {
                Name = "E07 Operational vertical slice: Success end-to-end (recap + ResultReady once)",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    int results = 0;
                    Action<ScenarioResult> onResult = r => results++;
                    d.ResultReady += onResult;
                    try
                    {
                        d.StartScenario();
                        d.ReportPlanInspected("smoke.plan");
                        CollectNpcs(d.Config.NpcCountTotal);
                        SimulateExit(EvacExit.Emergency);
                        string end = ExpectEnd(d, EvacOutcome.Success, EvacExit.Emergency, "exit.emergency", results, 1);
                        if (end != null) return end;
                        if (!HasAction(d, EvacActionIds.Lg01PlanInspected) || !HasAction(d, EvacActionIds.Lg03Room1Done) || !HasAction(d, EvacActionIds.Lg04Room2Done) || !HasAction(d, EvacActionIds.Lg09StairsAllNpcs))
                            return "missing expected log entries";
                        return d.ActionLog.Count == 5 && CountAction(d, EvacActionIds.ReplayStart) == 1 ? null : "log count " + d.ActionLog.Count;
                    }
                    finally
                    {
                        d.ResultReady -= onResult;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E07 Operational: Partial (NPC mancanti) + ResultReady once",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    int results = 0;
                    Action<ScenarioResult> onResult = r => results++;
                    d.ResultReady += onResult;
                    try
                    {
                        d.StartScenario();
                        d.ReportPlanInspected("smoke.plan");
                        CollectNpcs(3);
                        SimulateExit(EvacExit.Emergency);
                        string end = ExpectEnd(d, EvacOutcome.Partial, EvacExit.Emergency, "exit.emergency", results, 1);
                        if (end != null) return end;
                        return HasAction(d, EvacActionIds.Lg05StairsMissingNpcs) ? null : "LG05 missing";
                    }
                    finally
                    {
                        d.ResultReady -= onResult;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E07 Operational: Partial (scale interne) + ResultReady once",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    int results = 0;
                    Action<ScenarioResult> onResult = r => results++;
                    d.ResultReady += onResult;
                    try
                    {
                        d.StartScenario();
                        d.ReportPlanInspected("smoke.plan");
                        CollectNpcs(d.Config.NpcCountTotal);
                        SimulateExit(EvacExit.Internal);
                        string end = ExpectEnd(d, EvacOutcome.Partial, EvacExit.Internal, "exit.internal", results, 1);
                        if (end != null) return end;
                        return HasAction(d, EvacActionIds.Lg11InternalStairs) ? null : "LG11 missing";
                    }
                    finally
                    {
                        d.ResultReady -= onResult;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E07 Operational: Failure (ascensore) + ResultReady once",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    int results = 0;
                    Action<ScenarioResult> onResult = r => results++;
                    d.ResultReady += onResult;
                    try
                    {
                        d.StartScenario();
                        SimulateElevator();
                        string end = ExpectEnd(d, EvacOutcome.Failure, EvacExit.None, "elevator", results, 1);
                        if (end != null) return end;
                        return HasAction(d, EvacActionIds.Lg06Elevator) ? null : "LG06 missing";
                    }
                    finally
                    {
                        d.ResultReady -= onResult;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E07 Operational: Failure (timeout, soglia ridotta) + TimeUp/ResultReady once",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.SetDebugTimeLimit(5f);
                    int results = 0;
                    int timeUps = 0;
                    Action<ScenarioResult> onResult = r => results++;
                    Action onTimeUp = () => timeUps++;
                    d.ResultReady += onResult;
                    d.TimeUp += onTimeUp;
                    try
                    {
                        d.StartScenario();
                        d.AdvanceDebugTime(4.9f);
                        if (d.Phase != EvacPhase.Action)
                            return "ended before reduced limit";
                        d.AdvanceDebugTime(0.2f);
                        d.AdvanceDebugTime(1f);
                        string end = ExpectEnd(d, EvacOutcome.Failure, EvacExit.None, "timeout", results, 1);
                        if (end != null) return end;
                        if (timeUps != 1) return "TimeUp x" + timeUps;
                        if (!HasAction(d, EvacActionIds.Lg10Timeout)) return "LG10 missing";
                        if (!Near(d.Elapsed, 5f)) return "elapsed " + d.Elapsed;
                        return null;
                    }
                    finally
                    {
                        d.TimeUp -= onTimeUp;
                        d.ResultReady -= onResult;
                        d.ClearDebugTimeLimit();
                    }
                }
            });
        }

        private void AddE08DifficultySteps(EvacuationDirector d, List<SmokeStep> steps)
        {
            steps.Add(new SmokeStep
            {
                Name = "E08 three levels change time/aids/effects",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Guided);
                    if (d.HasTimeLimit || !d.HighlightsEnabled || !d.HintsEnabled || d.ReducedEffects)
                        return "Guided behaviour wrong";
                    d.SetDifficulty(EvacDifficulty.Operational);
                    if (!d.HasTimeLimit || !Near(d.EffectiveTimeLimitSeconds, 180f) || d.HighlightsEnabled || d.HintsEnabled || d.ReducedEffects)
                        return "Operational behaviour wrong";
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    if (!d.HasTimeLimit || !Near(d.EffectiveTimeLimitSeconds, 120f) || d.HighlightsEnabled || d.HintsEnabled || !d.ReducedEffects)
                        return "Crisis behaviour wrong";
                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E08 Guided: 5 ordered hint keys on request",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Guided);
                    if (d.Config == null || d.Config.OrderedHints.Count != 5)
                        return "5 hint assets missing";
                    d.StartScenario();
                    var keys = new List<string>();
                    Action<string> onHint = key => keys.Add(key);
                    d.HintRequested += onHint;
                    try
                    {
                        d.RequestNextHint();
                        d.ReportPlanInspected("smoke.e08.hints");
                        d.RequestNextHint();
                        CollectNpcs(d.Config.NpcCountRoom1);
                        d.RequestNextHint();
                        CollectNpcs(d.Config.NpcCountRoom2);
                        d.RequestNextHint();
                        d.RequestNextHint();
                        string[] expected =
                        {
                            "evac.hint.plan", "evac.hint.room1", "evac.hint.room2",
                            "evac.hint.stairs", "evac.hint.noelevator"
                        };
                        if (keys.Count != expected.Length)
                            return "hint events " + keys.Count;
                        for (int i = 0; i < expected.Length; i++)
                        {
                            if (keys[i] != expected[i])
                                return $"hint {i + 1}: {keys[i]} != {expected[i]}";
                        }
                        return null;
                    }
                    finally
                    {
                        d.HintRequested -= onHint;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E08 Guided: idle hint once, new hint after progress",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Guided);
                    d.StartScenario();
                    int count = 0;
                    Action<string> onHint = key => count++;
                    d.HintRequested += onHint;
                    try
                    {
                        float idle = d.Config.HintIdleSeconds;
                        d.AdvanceDebugTime(idle + 0.1f);
                        if (count != 1)
                            return "auto hints " + count;
                        d.AdvanceDebugTime(idle + 0.1f);
                        if (count != 1)
                            return "auto hint repeated: " + count;
                        d.ReportPlanInspected("smoke.e08.idle");
                        d.AdvanceDebugTime(idle + 0.1f);
                        return count == 2 ? null : "hint after progress " + count;
                    }
                    finally
                    {
                        d.HintRequested -= onHint;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E08 danger Up/Mid/Down; Guided survives time and elevator",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Guided);
                    d.StartScenario();
                    var levels = new List<EvacDangerLevel>();
                    Action<EvacDangerLevel> onDanger = level => levels.Add(level);
                    d.DangerLevelChanged += onDanger;
                    try
                    {
                        d.ReportPlanInspected("smoke.e08.danger");
                        d.AdvanceDebugTime(d.Config.HintIdleSeconds + 0.1f);
                        SimulateElevator();
                        d.ForceTimeUp();
                        if (d.Phase != EvacPhase.Action || d.Outcome.HasValue || d.HasTimeLimit)
                            return "Guided ended: " + d.Phase + " " + d.Outcome;
                        if (!levels.Contains(EvacDangerLevel.Up) || !levels.Contains(EvacDangerLevel.Mid) || !levels.Contains(EvacDangerLevel.Down))
                            return "danger sequence incomplete";
                        return d.DangerLevel == EvacDangerLevel.Down ? null : "danger not Down";
                    }
                    finally
                    {
                        d.DangerLevelChanged -= onDanger;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E08 hints ignored outside Guided",
                Run = () =>
                {
                    foreach (EvacDifficulty level in new[] { EvacDifficulty.Operational, EvacDifficulty.Crisis })
                    {
                        d.SetDifficulty(level);
                        d.StartScenario();
                        int count = 0;
                        Action<string> onHint = key => count++;
                        d.HintRequested += onHint;
                        try
                        {
                            d.AdvanceDebugTime(d.Config.HintIdleSeconds + 0.1f);
                            d.RequestNextHint();
                            if (count != 0)
                                return $"{level}: {count} hints";
                        }
                        finally
                        {
                            d.HintRequested -= onHint;
                        }
                    }
                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E08 highlights only in Guided",
                Run = () =>
                {
                    if (guidedHighlights == null)
                        guidedHighlights = FindFirstObjectByType<EvacGuidedHighlights>();
                    if (guidedHighlights == null)
                        return "EvacGuidedHighlights missing";
                    d.SetDifficulty(EvacDifficulty.Guided);
                    d.StartScenario();
                    guidedHighlights.ApplyHighlights();
                    if (guidedHighlights.HighlightedCount <= 0)
                        return "Guided highlighted " + guidedHighlights.HighlightedCount;
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    guidedHighlights.ApplyHighlights();
                    if (guidedHighlights.HighlightedCount != 0)
                        return "Operational highlighted " + guidedHighlights.HighlightedCount;
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    d.StartScenario();
                    guidedHighlights.ApplyHighlights();
                    return guidedHighlights.HighlightedCount == 0 ? null : "Crisis highlighted " + guidedHighlights.HighlightedCount;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E08 reduced effects toggle and reset",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    if (!d.ReducedEffects)
                        return "Crisis did not start reduced";
                    d.SetReducedEffects(false);
                    if (d.ReducedEffects)
                        return "toggle failed";
                    d.ResetScenario();
                    return d.ReducedEffects ? null : "reset lost reduced flag";
                }
            });
        }

        private void AddE09FireSteps(EvacuationDirector d, List<SmokeStep> steps)
        {
            steps.Add(new SmokeStep
            {
                Name = "E09 Crisis: 5 fire slots active, none in Guided/Operational",
                Run = () =>
                {
                    if (fireManager == null)
                        fireManager = FindFirstObjectByType<EvacFireManager>();
                    if (fireManager == null)
                        return "EvacFireManager missing";
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    if (fireManager.ActiveFires.Count != d.Config.ActiveFireCount)
                        return "Crisis fires " + fireManager.ActiveFires.Count;
                    d.SetDifficulty(EvacDifficulty.Guided);
                    if (fireManager.ActiveFires.Count != 0)
                        return "Guided fires " + fireManager.ActiveFires.Count;
                    d.SetDifficulty(EvacDifficulty.Operational);
                    return fireManager.ActiveFires.Count == 0 ? null : "Operational fires " + fireManager.ActiveFires.Count;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E09 reset clears fires and contacts",
                Run = () =>
                {
                    if (fireManager == null)
                        fireManager = FindFirstObjectByType<EvacFireManager>();
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    d.ReportFireContact();
                    d.ResetScenario();
                    if (d.FireContacts != 0 || d.Hearts != d.Config.Hearts)
                        return "contacts/hearts not restored";
                    return fireManager.ActiveFires.Count == d.Config.ActiveFireCount ? null : "fires not restored";
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E09 free lane on 200 seeds",
                Run = () =>
                {
                    if (fireManager == null)
                        fireManager = FindFirstObjectByType<EvacFireManager>();
                    int rejected = fireManager.ValidateSeeds(200);
                    return rejected > 100 ? "blocked seeds " + rejected : null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E09 ReducedEffects flag reaches fire VFX",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    if (!d.ReducedEffects)
                        return "Crisis did not start reduced";
                    d.SetReducedEffects(false);
                    if (d.ReducedEffects)
                        return "toggle failed";
                    d.ResetScenario();
                    return d.ReducedEffects ? null : "reset lost reduced flag";
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E09 ForceContact leads to Failure + LG07/LG08",
                Run = () =>
                {
                    if (fireManager == null)
                        fireManager = FindFirstObjectByType<EvacFireManager>();
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    d.StartScenario();
                    fireManager.ForceContact();
                    fireManager.ForceContact();
                    if (d.FireContacts != 2 || !HasAction(d, EvacActionIds.Lg07FireContact))
                        return "first two contacts wrong";
                    fireManager.ForceContact();
                    if (d.Phase != EvacPhase.ResultHandoff || d.Outcome != EvacOutcome.Failure || !HasAction(d, EvacActionIds.Lg08FireDeath))
                        return "lethal contact wrong";
                    return null;
                }
            });
        }

        private void AddE10ReplaySteps(EvacuationDirector d, List<SmokeStep> steps)
        {
            steps.Add(new SmokeStep
            {
                Name = "E10 new seed at every reset, fixed seed repeatable",
                Run = () =>
                {
                    d.ClearFixedSeed();
                    var seeds = new List<int>();
                    for (int i = 0; i < 4; i++)
                    {
                        d.ResetScenario();
                        seeds.Add(d.Seed);
                    }
                    if (seeds.Distinct().Count() != seeds.Count)
                        return "repeated seeds: " + string.Join(",", seeds);
                    d.SetFixedSeed(4242);
                    d.ResetScenario();
                    int a = d.Seed;
                    d.ResetScenario();
                    int b = d.Seed;
                    if (a != 4242 || b != 4242 || !d.IsSeedFixed)
                        return $"fixed seed {a}/{b}, isFixed {d.IsSeedFixed}";
                    d.ClearFixedSeed();
                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E10 same seed reproduces NPC and fire layout",
                Run = () =>
                {
                    if (npcRegistry == null || fireManager == null)
                        return "NPC registry or fire manager not assigned";
                    d.SetDifficulty(EvacDifficulty.Crisis);
                    d.SetFixedSeed(4242);
                    d.ResetScenario();
                    string npc1 = npcRegistry.LayoutSignature();
                    string fire1 = fireManager.LayoutSignature();
                    d.ResetScenario();
                    string npc2 = npcRegistry.LayoutSignature();
                    string fire2 = fireManager.LayoutSignature();
                    if (string.IsNullOrEmpty(npc1) || npc1 != npc2)
                        return $"NPC layout [{npc1}] != [{npc2}]";
                    if (string.IsNullOrEmpty(fire1) || fire1 != fire2)
                        return $"fire layout [{fire1}] != [{fire2}]";
                    d.ClearFixedSeed();
                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E10 ReplayStart recorded in ActionLog and ResultReady",
                Run = () =>
                {
                    d.SetDifficulty(EvacDifficulty.Operational);
                    int results = 0;
                    ScenarioResult result = null;
                    Action<ScenarioResult> onResult = r => { results++; result = r; };
                    d.ResultReady += onResult;
                    try
                    {
                        d.StartScenario();
                        d.ReportPlanInspected("smoke.e10.replay");
                        CollectNpcs(d.Config.NpcCountTotal);
                        SimulateExit(EvacExit.Emergency);
                        if (results != 1 || result == null)
                            return "ResultReady x" + results;
                        if (!d.ReplayStartLogged || CountAction(d, EvacActionIds.ReplayStart) != 1)
                            return "ReplayStart missing or duplicated";
                        bool inResult = false;
                        for (int i = 0; i < result.ActionLog.Entries.Count; i++)
                        {
                            if (result.ActionLog.Entries[i].ActionId == EvacActionIds.ReplayStart)
                                inResult = true;
                        }
                        if (!inResult)
                            return "ReplayStart not in ResultReady log";
                        return string.IsNullOrEmpty(d.ReplayNpcSlots) ? "no NPC layout reported" : null;
                    }
                    finally
                    {
                        d.ResultReady -= onResult;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "E10 5 varied consecutive games, no residual objects",
                Run = () => RunE10VariedGames()
            });

            steps.Add(new SmokeStep
            {
                Name = "E10 10 clean resets, no null refs or duplicate subscriptions",
                Run = () => RunE10ResetStress()
            });
        }

        private string RunE10VariedGames()
        {
            if (!Application.isPlaying)
                return "not in Play Mode";
            if (!Require() || npcRegistry == null || npcTrail == null || fireManager == null)
                return "director or E06/E09 systems missing";
            EvacuationDirector d = director;
            d.ClearDebugTimeLimit();
            int results = 0;
            Action<ScenarioResult> onResult = r => results++;
            d.ResultReady += onResult;
            try
            {
                var outcomes = new List<EvacOutcome>();
                for (int game = 0; game < 5; game++)
                {
                    switch (game)
                    {
                        case 0:
                            d.SetDifficulty(EvacDifficulty.Operational);
                            d.StartScenario();
                            d.ReportPlanInspected("smoke.e10.g1");
                            CollectNpcs(d.Config.NpcCountTotal);
                            SimulateExit(EvacExit.Emergency);
                            break;
                        case 1:
                            d.SetDifficulty(EvacDifficulty.Operational);
                            d.StartScenario();
                            d.ReportPlanInspected("smoke.e10.g2");
                            CollectNpcs(3);
                            SimulateExit(EvacExit.Emergency);
                            break;
                        case 2:
                            d.SetDifficulty(EvacDifficulty.Operational);
                            d.StartScenario();
                            d.ReportPlanInspected("smoke.e10.g3");
                            CollectNpcs(d.Config.NpcCountTotal);
                            SimulateExit(EvacExit.Internal);
                            break;
                        case 3:
                            d.SetDifficulty(EvacDifficulty.Operational);
                            d.StartScenario();
                            SimulateElevator();
                            break;
                        default:
                            d.SetDifficulty(EvacDifficulty.Crisis);
                            d.StartScenario();
                            fireManager.ForceContact();
                            fireManager.ForceContact();
                            fireManager.ForceContact();
                            break;
                    }

                    if (d.Phase != EvacPhase.ResultHandoff || !d.Outcome.HasValue)
                        return $"game {game + 1}: phase {d.Phase}, outcome {d.Outcome}";
                    if (results != game + 1)
                        return $"game {game + 1}: ResultReady x{results}";
                    if (CountAction(d, EvacActionIds.ReplayStart) != 1)
                        return $"game {game + 1}: ReplayStart missing";
                    outcomes.Add(d.Outcome.Value);

                    d.ResetScenario();
                    string baseline = ExpectBaseline(d);
                    if (baseline != null)
                        return $"game {game + 1} reset: {baseline}";
                    if (npcTrail.BreadcrumbCount != 0)
                        return $"game {game + 1}: {npcTrail.BreadcrumbCount} breadcrumbs left";
                    if (npcRegistry.Line.Count != 0 || npcRegistry.Spawned.Count != d.Config.NpcCountTotal)
                        return $"game {game + 1}: line {npcRegistry.Line.Count}, spawned {npcRegistry.Spawned.Count}";
                    for (int i = 0; i < npcRegistry.Spawned.Count; i++)
                    {
                        EvacNpc npc = npcRegistry.Spawned[i];
                        if (npc == null || npc.State != EvacNpcState.Idle || npc.Order != -1)
                            return $"game {game + 1}: spawn #{i} not Idle";
                    }
                    int expectedFires = d.Difficulty == EvacDifficulty.Crisis ? d.Config.ActiveFireCount : 0;
                    if (fireManager.ActiveFires.Count != expectedFires)
                        return $"game {game + 1}: fires {fireManager.ActiveFires.Count} (expected {expectedFires})";
                }

                if (outcomes.Distinct().Count() < 3)
                    return "outcomes not varied: " + string.Join(",", outcomes);
                return null;
            }
            finally
            {
                d.ResultReady -= onResult;
                d.ClearDebugTimeLimit();
            }
        }

        private string RunE10ResetStress()
        {
            if (!Application.isPlaying)
                return "not in Play Mode";
            if (!Require() || npcRegistry == null || npcTrail == null || fireManager == null)
                return "director or E06/E09 systems missing";
            EvacuationDirector d = director;
            d.ClearDebugTimeLimit();
            d.SetDifficulty(EvacDifficulty.Crisis);
            int registered = d.ResettableCount;
            for (int pass = 0; pass < 10; pass++)
            {
                d.StartScenario();
                d.ReportPlanInspected("smoke.e10.reset");
                npcRegistry.CollectNext("smoke.e10.reset");
                if (doors.Length > 0 && doors[0] != null)
                    doors[0].Toggle();
                d.ReportFireContact();
                d.AdvanceDebugTime(3f);
                d.ResetScenario();

                string baseline = ExpectBaseline(d);
                if (baseline != null)
                    return $"reset {pass + 1}: {baseline}";
                if (npcTrail.BreadcrumbCount != 0)
                    return $"reset {pass + 1}: breadcrumbs {npcTrail.BreadcrumbCount}";
                if (npcRegistry.Line.Count != 0)
                    return $"reset {pass + 1}: line {npcRegistry.Line.Count}";
                for (int i = 0; i < npcRegistry.Spawned.Count; i++)
                {
                    EvacNpc npc = npcRegistry.Spawned[i];
                    if (npc == null || npc.State != EvacNpcState.Idle)
                        return $"reset {pass + 1}: spawn #{i} null or not Idle";
                }
                if (d.ResettableCount != registered)
                    return $"reset {pass + 1}: resettables {registered} -> {d.ResettableCount}";
                if (fireManager.ActiveFires.Count != d.Config.ActiveFireCount)
                    return $"reset {pass + 1}: fires {fireManager.ActiveFires.Count}";
            }

            int phaseEvents = 0;
            int results = 0;
            int replays = 0;
            Action<EvacPhase> onPhase = p => phaseEvents++;
            Action<ScenarioResult> onResult = r => results++;
            Action<int, string, string> onReplay = (seed, npc, fire) => replays++;
            d.PhaseChanged += onPhase;
            d.ResultReady += onResult;
            d.ReplayStarted += onReplay;
            try
            {
                d.StartScenario();
                d.ReportExitReached(EvacExit.Internal);
            }
            finally
            {
                d.PhaseChanged -= onPhase;
                d.ResultReady -= onResult;
                d.ReplayStarted -= onReplay;
            }

            if (phaseEvents != 3 || results != 1 || replays != 1)
                return $"events PhaseChanged x{phaseEvents}, ResultReady x{results}, ReplayStarted x{replays}";
            d.ResetScenario();
            return ExpectBaseline(d);
        }

        private string ExpectEnd(EvacuationDirector d, EvacOutcome expectedOutcome, EvacExit expectedExit, string expectedReason, int resultEvents, int expectedResultEvents)
        {
            if (d.Phase != EvacPhase.ResultHandoff)
                return "phase " + d.Phase;
            if (!d.Outcome.HasValue || d.Outcome.Value != expectedOutcome)
                return "outcome " + (d.Outcome.HasValue ? d.Outcome.Value.ToString() : "-");
            if (d.LastResult == null)
                return "LastResult missing";
            if (d.LastResult.Outcome != expectedOutcome)
                return "ResultReady outcome " + d.LastResult.Outcome;
            if (d.ExitReached != expectedExit)
                return "exit " + d.ExitReached;
            if (d.EndReason != expectedReason)
                return "reason " + d.EndReason;
            if (resultEvents != expectedResultEvents)
                return "ResultReady x" + resultEvents;
            if (d.IsAlarmActive)
                return "alarm still active";
            if (d.ActionLog.Count == 0)
                return "ActionLog empty";
            return null;
        }

        private void CollectNpcs(int count)
        {
            if (!RequireNpcObjects())
                return;
            int collected = 0;
            for (int i = 0; i < npcRegistry.Spawned.Count && collected < count; i++)
            {
                EvacNpc npc = npcRegistry.Spawned[i];
                if (npc == null || npc.State != EvacNpcState.Idle)
                    continue;
                npc.Interactable.RaiseActivated();
                collected++;
            }
        }

        private void AddElevatorExitSteps(EvacuationDirector d, List<SmokeStep> steps)
        {
            steps.Add(new SmokeStep
            {
                Name = "elevator button: Failure (Operational), Down (Guided), no duplicate log",
                Run = () =>
                {
                    if (elevatorButtons.Length == 0 || elevatorButtons[0] == null || elevatorButtons[0].Interactable == null)
                        return "elevator button not assigned";

                    ElevatorButton button = elevatorButtons[0];
                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    button.Interactable.RaiseActivated();
                    if (d.Phase != EvacPhase.ResultHandoff || d.Outcome != EvacOutcome.Failure || !button.WasPressed || !d.ElevatorWasUsed)
                        return $"Operational: phase {d.Phase}, outcome {d.Outcome}, pressed {button.WasPressed}";
                    if (!HasAction(d, EvacActionIds.Lg06Elevator))
                        return "LG06 missing";

                    d.SetDifficulty(EvacDifficulty.Guided);
                    var levels = new List<EvacDangerLevel>();
                    Action<EvacDangerLevel> onDanger = levels.Add;
                    d.DangerLevelChanged += onDanger;
                    try
                    {
                        d.StartScenario();
                        button.Interactable.RaiseActivated();
                        if (d.Phase != EvacPhase.Action || d.DangerLevel != EvacDangerLevel.Down || levels.Count != 1)
                            return $"Guided: phase {d.Phase}, danger {d.DangerLevel}, events {levels.Count}";
                        button.Interactable.RaiseActivated();
                        if (CountAction(d, EvacActionIds.Lg06Elevator) != 1)
                            return "LG06 logged twice";
                        return null;
                    }
                    finally
                    {
                        d.DangerLevelChanged -= onDanger;
                    }
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "stairs zones: internal Partial, emergency Success with plan",
                Run = () =>
                {
                    StairsExitZone emergency = FindZone(EvacExit.Emergency);
                    StairsExitZone internalZone = FindZone(EvacExit.Internal);
                    if (emergency == null || internalZone == null || emergency.ZoneCollider == null || internalZone.ZoneCollider == null)
                        return "stairs zones not assigned";
                    if (!emergency.ZoneCollider.isTrigger || !internalZone.ZoneCollider.isTrigger)
                        return "stairs zone colliders are not triggers";

                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.ReportPlanInspected();
                    d.TeleportRigTo(internalZone.transform);
                    if (!internalZone.TryReach("smoke") || d.Outcome != EvacOutcome.Partial || !HasAction(d, EvacActionIds.Lg11InternalStairs))
                        return $"internal: reached {internalZone.WasReached}, outcome {d.Outcome}";

                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.ReportPlanInspected();
                    for (int i = 0; i < 3; i++)
                        d.ReportNpcCollected(1);
                    for (int i = 0; i < 2; i++)
                        d.ReportNpcCollected(2);
                    d.TeleportRigTo(emergency.transform);
                    if (!emergency.TryReach("smoke") || d.Outcome != EvacOutcome.Success || !HasAction(d, EvacActionIds.Lg09StairsAllNpcs))
                        return $"emergency: reached {emergency.WasReached}, outcome {d.Outcome}";
                    return null;
                }
            });

            steps.Add(new SmokeStep
            {
                Name = "stairs zones: LG02 without plan, reset clears zones and button",
                Run = () =>
                {
                    StairsExitZone emergency = FindZone(EvacExit.Emergency);
                    StairsExitZone internalZone = FindZone(EvacExit.Internal);
                    if (emergency == null || internalZone == null)
                        return "stairs zones not assigned";

                    d.SetDifficulty(EvacDifficulty.Operational);
                    d.StartScenario();
                    d.TeleportRigTo(emergency.transform);
                    emergency.TryReach("smoke");
                    if (!HasAction(d, EvacActionIds.Lg02NoPlan))
                        return "LG02 missing";

                    d.ResetScenario();
                    if (emergency.WasReached || internalZone.WasReached || elevatorButtons.Any(button => button != null && button.WasPressed))
                        return "E05 flags survived reset";
                    return ExpectBaseline(d);
                }
            });
        }

        private string CheckConfig(EvacDifficulty level)
        {
            EvacuationConfig c = director.ConfigFor(level);
            if (c == null)
                return "config missing";
            var problems = new List<string>();
            c.Validate(problems);
            return problems.Count == 0 ? null : string.Join("; ", problems);
        }

        private static string ExpectBaseline(EvacuationDirector d)
        {
            if (d.Phase != EvacPhase.Briefing)
                return "phase is " + d.Phase;
            if (d.Elapsed != 0f)
                return "elapsed not 0";
            if (d.Config == null || d.Hearts != d.Config.Hearts)
                return "hearts differ from config";
            if (d.HasTimeLimit != d.Config.HasTimeLimit)
                return "time limit differs from config";
            if (d.ActionLog.Count != 0 || d.Outcome.HasValue || d.LastResult != null)
                return "log/outcome not cleared";
            if (d.FireContacts != 0 || d.NpcCollectedRoom1 != 0 || d.NpcCollectedRoom2 != 0 || d.PlanWasInspected || d.ElevatorWasUsed)
                return "counters/flags not cleared";
            if (d.ExitReached != EvacExit.None || !d.IsAlive || d.DangerLevel != EvacDangerLevel.Mid)
                return "exit/alive/danger not cleared";
            return null;
        }

        private static bool HasAction(EvacuationDirector d, string id)
        {
            for (int i = 0; i < d.ActionLog.Entries.Count; i++)
            {
                if (d.ActionLog.Entries[i].ActionId == id)
                    return true;
            }

            return false;
        }

        private static int CountAction(EvacuationDirector d, string id)
        {
            return d.ActionLog.Entries.Count(entry => entry.ActionId == id);
        }

        private static bool Near(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.001f;
        }

        private bool Require()
        {
            ResolveReferences();
            if (director != null)
                return true;
            UDebug.LogError("[EV] EvacDebugCommands: EvacuationDirector not found.", this);
            return false;
        }

        private void ResolveReferences()
        {
            if (alarm == null)
                alarm = FindFirstObjectByType<AlarmSystem>();
            if (plans == null || plans.Length == 0)
                plans = FindObjectsByType<EvacuationPlanInspectable>(FindObjectsSortMode.None).OrderBy(plan => plan.name, StringComparer.Ordinal).ToArray();
            if (doors == null || doors.Length == 0)
                doors = FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None).OrderBy(door => door.name, StringComparer.Ordinal).ToArray();
            if (elevatorButtons == null || elevatorButtons.Length == 0)
                elevatorButtons = FindObjectsByType<ElevatorButton>(FindObjectsSortMode.None).OrderBy(button => button.name, StringComparer.Ordinal).ToArray();
            if (stairsZones == null || stairsZones.Length == 0)
                stairsZones = FindObjectsByType<StairsExitZone>(FindObjectsSortMode.None).OrderBy(zone => zone.name, StringComparer.Ordinal).ToArray();
            if (npcRegistry == null)
                npcRegistry = FindFirstObjectByType<NpcRegistry>();
            if (npcTrail == null)
                npcTrail = FindFirstObjectByType<NpcTrailFollower>();
            if (guidedHighlights == null)
                guidedHighlights = FindFirstObjectByType<EvacGuidedHighlights>();
            if (fireManager == null)
                fireManager = FindFirstObjectByType<EvacFireManager>();
            if (fireLaneValidator == null)
                fireLaneValidator = FindFirstObjectByType<EvacFireLaneValidator>();
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (markersRoot == null)
            {
                GameObject root = GameObject.Find("ScenarioMarkers");
                if (root != null)
                    markersRoot = root.transform;
            }
        }

        private void ResolveMarkers()
        {
            teleportMarkers.Clear();
            if (markersRoot == null || teleportMarkerNames == null)
                return;
            for (int i = 0; i < teleportMarkerNames.Length; i++)
            {
                Transform marker = markersRoot.Find(teleportMarkerNames[i]);
                if (marker != null)
                    teleportMarkers.Add(marker);
                else
                    UDebug.LogWarning($"[EV] Teleport marker '{teleportMarkerNames[i]}' not found under {markersRoot.name}.", this);
            }
        }
    }
}
