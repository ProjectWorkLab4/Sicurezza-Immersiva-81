using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Systems;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Debug
{
    /// <summary>
    /// Editor/development-only registry for keyboard and context-menu scenario checks.
    /// </summary>
    public sealed class DebugCommands : MonoBehaviour
    {
        [SerializeField] private Transform rigRoot;
        [SerializeField] private Transform teleportMarker;
        [SerializeField] private Transform[] teleportMarkers = new Transform[0];
        [SerializeField] private string markerName = "DebugSpawnMarker";
        [SerializeField] private ChemicalAlertConfig[] chemicalAlertConfigs = new ChemicalAlertConfig[0];
        [SerializeField] private int replaySeedForDebug = 81081;

        private readonly Dictionary<string, RegisteredCommand> commands =
            new Dictionary<string, RegisteredCommand>(StringComparer.OrdinalIgnoreCase);
        private int nextMarkerIndex;

        private sealed class RegisteredCommand
        {
            public string Description;
            public Action Action;
        }

        /// <summary>Registers or replaces a debug command.</summary>
        public void Register(string key, string description, Action action)
        {
            if (string.IsNullOrWhiteSpace(key) || action == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: invalid command registration.");
                return;
            }

            commands[key.Trim()] = new RegisteredCommand
            {
                Description = description ?? string.Empty,
                Action = action
            };
        }

        /// <summary>Removes a registered debug command.</summary>
        public void Unregister(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
                commands.Remove(key.Trim());
        }

        private void Awake()
        {
            ResolveReferences();
            Register("H", "elenca i comandi di debug", PrintCommandList);
            Register("T", "teletrasporta il rig al marker", TeleportRigToMarker);
            Register("R", "reset ChemicalAlertDirector", ResetDirector);
            Register("P", "stampa lo stato di debug", PrintState);
            Register("C", "stampa i 3 config Chemical Alert", PrintChemicalAlertConfigs);
            Register("O", "esegue il SelfCheck OutcomeEvaluator", RunOutcomeEvaluatorSelfCheck);
            Register("N", "avanza la fase del director", AdvanceDirectorPhase);
            Register("D", "cambia difficoltà e resetta", CycleDirectorDifficulty);
            Register("F", "forza fine tempo", ForceDirectorTimeUp);
            Register("X", "esegue 3 reset consecutivi", ResetDirectorThreeTimes);
            Register("S", "stampa stato director", PrintDirectorState);
            Register("G", "simula Grab su TestCube", SimulateGrabTestCube);
            Register("L", "simula Release su TestCube", SimulateReleaseTestCube);
            Register("A", "simula Activate su TestCube", SimulateActivateTestCube);
            Register("I", "simula Inspect su TestCube", SimulateInspectTestCube);
            Register("V", "attiva la ventilazione", ActivateVentilation);
            Register("E", "avanza escalation ventilazione di 20s", AdvanceVentilationDebugTime);
            Register("M", "imposta moltiplicatore ventilazione x10", SetVentilationDebugMultiplier);
            Register("Y", "stampa stadio ventilazione", PrintVentilationStage);
            Register("Q", "simula lettura SDS", SimulateSdsInspection);
            Register("B", "apre/chiude armadietto DPI", TogglePPECabinet);
            Register("K", "equipaggia set DPI corretto", EquipCorrectPPESet);
            Register("J", "equipaggia set DPI decoy", EquipDecoyPPESet);
            Register("W", "stampa stato DPI", PrintPPEState);
            Register("Z", "forza contatto sostanza", ForceSubstanceContact);
            Register("U", "stampa cuori salute", PrintHealthState);
            Register("1", "riempi perimetro spill", FillSpillPerimeter);
            Register("2", "stampa stato perimetro", PrintSpillPerimeterState);
            Register("3", "completa cleanup debug", CompleteCleanupDebug);
            Register("4", "stampa stato cleanup", PrintCleanupState);
            Register("5", "forza uscita safe", ForceExitSafe);
            Register("6", "stampa esito e recap", PrintResultRecap);
            Register("7", "esegue lo smoke test completo", RunSmokeTest);
            Register("8", "richiede un hint Guided", RequestDirectorHint);
            Register("9", "attiva/disattiva effetti ridotti", ToggleReducedEffects);
            Register("0", "stampa comportamento difficoltà", PrintDifficultyBehavior);
            Register("F6", "avanza replay di 5s per la seconda perdita", AdvanceSecondSpillDebugTime);
            Register("F8", "fissa seed replay (valore Inspector)", FixReplaySeed);
            Register("F9", "rimuove seed fisso replay", ClearReplaySeed);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current == null)
                return;

            foreach (KeyValuePair<string, RegisteredCommand> command in commands)
            {
                if (WasPressed(command.Key))
                {
                    command.Value.Action.Invoke();
                    break;
                }
            }
#endif
        }

        private bool WasPressed(string key)
        {
            switch (key.ToUpperInvariant())
            {
                case "H": return Keyboard.current.hKey.wasPressedThisFrame;
                case "T": return Keyboard.current.tKey.wasPressedThisFrame;
                case "R": return Keyboard.current.rKey.wasPressedThisFrame;
                case "P": return Keyboard.current.pKey.wasPressedThisFrame;
                case "C": return Keyboard.current.cKey.wasPressedThisFrame;
                case "O": return Keyboard.current.oKey.wasPressedThisFrame;
                case "N": return Keyboard.current.nKey.wasPressedThisFrame;
                case "D": return Keyboard.current.dKey.wasPressedThisFrame;
                case "F": return Keyboard.current.fKey.wasPressedThisFrame;
                case "X": return Keyboard.current.xKey.wasPressedThisFrame;
                case "S": return Keyboard.current.sKey.wasPressedThisFrame;
                case "G": return Keyboard.current.gKey.wasPressedThisFrame;
                case "L": return Keyboard.current.lKey.wasPressedThisFrame;
                case "A": return Keyboard.current.aKey.wasPressedThisFrame;
                case "I": return Keyboard.current.iKey.wasPressedThisFrame;
                case "V": return Keyboard.current.vKey.wasPressedThisFrame;
                case "E": return Keyboard.current.eKey.wasPressedThisFrame;
                case "M": return Keyboard.current.mKey.wasPressedThisFrame;
                case "Y": return Keyboard.current.yKey.wasPressedThisFrame;
                case "Q": return Keyboard.current.qKey.wasPressedThisFrame;
                case "B": return Keyboard.current.bKey.wasPressedThisFrame;
                case "K": return Keyboard.current.kKey.wasPressedThisFrame;
                case "J": return Keyboard.current.jKey.wasPressedThisFrame;
                case "W": return Keyboard.current.wKey.wasPressedThisFrame;
                case "Z": return Keyboard.current.zKey.wasPressedThisFrame;
                case "U": return Keyboard.current.uKey.wasPressedThisFrame;
                case "1": return Keyboard.current.digit1Key.wasPressedThisFrame;
                case "2": return Keyboard.current.digit2Key.wasPressedThisFrame;
                case "3": return Keyboard.current.digit3Key.wasPressedThisFrame;
                case "4": return Keyboard.current.digit4Key.wasPressedThisFrame;
                case "5": return Keyboard.current.digit5Key.wasPressedThisFrame;
                case "6": return Keyboard.current.digit6Key.wasPressedThisFrame;
                case "7": return Keyboard.current.digit7Key.wasPressedThisFrame;
                case "8": return Keyboard.current.digit8Key.wasPressedThisFrame;
                case "9": return Keyboard.current.digit9Key.wasPressedThisFrame;
                case "0": return Keyboard.current.digit0Key.wasPressedThisFrame;
                case "F6": return Keyboard.current.f6Key.wasPressedThisFrame;
                case "F8": return Keyboard.current.f8Key.wasPressedThisFrame;
                case "F9": return Keyboard.current.f9Key.wasPressedThisFrame;
                default: return false;
            }
        }

        private void ResolveReferences()
        {
            if (rigRoot == null)
                rigRoot = transform.root;

            if (teleportMarker == null && !string.IsNullOrWhiteSpace(markerName))
            {
                GameObject marker = GameObject.Find(markerName);
                if (marker != null)
                    teleportMarker = marker.transform;
            }

            if (teleportMarker == null)
                UnityEngine.Debug.LogError("[CA] DebugCommands: marker non assegnato.");

            if (teleportMarkers == null || teleportMarkers.Length == 0)
            {
                string[] requiredMarkerNames =
                {
                    "SPAWN_Player", "POS_Barrel", "POS_Puddle", "POS_VentButton", "POS_Truck",
                    "POS_SDS", "POS_PPECabinet", "POS_SpillKit", "POS_WasteBag",
                    "ZONE_ExitSafe", "ZONE_SpillArea"
                };
                List<Transform> foundMarkers = new List<Transform>();
                foreach (string requiredMarkerName in requiredMarkerNames)
                {
                    GameObject marker = GameObject.Find(requiredMarkerName);
                    if (marker != null)
                        foundMarkers.Add(marker.transform);
                }
                teleportMarkers = foundMarkers.ToArray();
            }
        }

        /// <summary>Prints every registered debug command to the Unity console.</summary>
        [ContextMenu("Print command list")]
        public void PrintCommandList()
        {
            string list = string.Join(" | ",
                commands.OrderBy(pair => pair.Key)
                    .Select(pair => $"{pair.Key}: {pair.Value.Description}"));
            UnityEngine.Debug.Log($"[CA] Debug commands: {list}");
        }

        /// <summary>Teleports the configured rig root to the configured marker.</summary>
        [ContextMenu("Teleport rig to marker")]
        public void TeleportRigToMarker()
        {
            ResolveReferences();
            if (rigRoot == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: teletrasporto impossibile, riferimenti mancanti.");
                return;
            }

            Transform target = null;
            if (teleportMarkers != null && teleportMarkers.Length > 0)
            {
                target = teleportMarkers[nextMarkerIndex % teleportMarkers.Length];
                nextMarkerIndex++;
            }
            else
            {
                target = teleportMarker;
            }

            if (target == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: nessun marker disponibile.");
                return;
            }

            rigRoot.SetPositionAndRotation(target.position, target.rotation);
            UnityEngine.Debug.Log($"[CA] Debug teleport: {rigRoot.name} -> {target.name}");
        }

        /// <summary>Logs the reset hook without changing scenario gameplay state.</summary>
        [ContextMenu("Reset scene (placeholder)")]
        public void ResetScenePlaceholder()
        {
            UnityEngine.Debug.Log("[CA] Debug reset scene: placeholder, nessun reset gameplay ancora registrato.");
        }

        /// <summary>Resets the Chemical Alert director once.</summary>
        [ContextMenu("Reset ChemicalAlertDirector")]
        public void ResetDirector()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director == null) return;
            director.ResetScenario();
            UnityEngine.Debug.Log("[CA] Director reset.");
        }

        /// <summary>Runs three consecutive director resets for idempotency checks.</summary>
        [ContextMenu("Reset ChemicalAlertDirector three times")]
        public void ResetDirectorThreeTimes()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director == null) return;
            for (int i = 0; i < 3; i++)
                director.ResetScenario();
            UnityEngine.Debug.Log("[CA] Director reset x3.");
            director.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Advances the Chemical Alert director by one phase.</summary>
        [ContextMenu("Advance director phase")]
        public void AdvanceDirectorPhase()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director == null) return;
            director.AdvancePhase();
            director.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Cycles the director through Guided, Operational and Crisis.</summary>
        [ContextMenu("Cycle director difficulty")]
        public void CycleDirectorDifficulty()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director == null) return;
            int next = ((int)director.SelectedDifficulty + 1) %
                Enum.GetValues(typeof(ChemicalAlertDirector.Difficulty)).Length;
            director.SetDifficulty((ChemicalAlertDirector.Difficulty)next);
            director.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Forces the director timeout path.</summary>
        [ContextMenu("Force director time up")]
        public void ForceDirectorTimeUp()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director == null) return;
            director.ForceTimeUp();
            director.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Prints the director state through the debug console.</summary>
        [ContextMenu("Print director state")]
        public void PrintDirectorState()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director != null)
                director.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Requests the next configured Guided hint.</summary>
        [ContextMenu("Request Guided hint")]
        public void RequestDirectorHint()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director != null && !director.RequestHint())
                UnityEngine.Debug.Log("[CA] Hint request ignored (not Guided, disabled, or exhausted).");
        }

        /// <summary>Toggles reduced effects for the Crisis disturbance.</summary>
        [ContextMenu("Toggle reduced disturbance effects")]
        public void ToggleReducedEffects()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director != null)
                director.SetReducedEffects(!director.ReducedEffects);
        }

        /// <summary>Prints current difficulty behavior and disturbance state.</summary>
        [ContextMenu("Print difficulty behavior")]
        public void PrintDifficultyBehavior()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director != null)
                director.PrintDifficultyBehavior(UnityEngine.Debug.Log);
        }

        /// <summary>Advances replay time by five debug seconds toward the scheduled second spill.</summary>
        [ContextMenu("Advance replay time 5s")]
        public void AdvanceSecondSpillDebugTime()
        {
            ScenarioReplayController replay = FindFirstObjectByType<ScenarioReplayController>();
            if (replay == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: ScenarioReplayController non trovato.");
                return;
            }

            replay.AdvanceDebugTime(5f);
            replay.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Fixes the replay seed to the Inspector value and restarts the scenario.</summary>
        [ContextMenu("Fix replay seed")]
        public void FixReplaySeed()
        {
            ScenarioReplayController replay = FindFirstObjectByType<ScenarioReplayController>();
            if (replay == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: ScenarioReplayController non trovato.");
                return;
            }

            replay.SetFixedSeed(replaySeedForDebug);
        }

        /// <summary>Clears the fixed replay seed and restarts with a new random seed.</summary>
        [ContextMenu("Use random replay seed")]
        public void ClearReplaySeed()
        {
            ScenarioReplayController replay = FindFirstObjectByType<ScenarioReplayController>();
            if (replay == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: ScenarioReplayController non trovato.");
                return;
            }

            replay.ClearFixedSeed();
        }

        /// <summary>Simulates a grab event on a named ScenarioInteractable.</summary>
        public void SimulateGrab(string objectName)
        {
            ScenarioInteractable interactable = FindInteractable(objectName);
            if (interactable != null) interactable.RaiseGrabbed();
        }

        /// <summary>Simulates a release event on a named ScenarioInteractable.</summary>
        public void SimulateRelease(string objectName)
        {
            ScenarioInteractable interactable = FindInteractable(objectName);
            if (interactable != null) interactable.RaiseReleased();
        }

        /// <summary>Simulates an activation event on a named ScenarioInteractable.</summary>
        public void SimulateActivate(string objectName)
        {
            ScenarioInteractable interactable = FindInteractable(objectName);
            if (interactable != null) interactable.RaiseActivated();
        }

        /// <summary>Simulates an inspection event on a named ScenarioInteractable.</summary>
        public void SimulateInspect(string objectName)
        {
            ScenarioInteractable interactable = FindInteractable(objectName);
            if (interactable != null) interactable.RaiseInspected();
        }

        /// <summary>Simulates grab on the existing TestCube.</summary>
        [ContextMenu("Simulate Grab TestCube")]
        public void SimulateGrabTestCube() => SimulateGrab("TestCube");

        /// <summary>Simulates release on the existing TestCube.</summary>
        [ContextMenu("Simulate Release TestCube")]
        public void SimulateReleaseTestCube() => SimulateRelease("TestCube");

        /// <summary>Simulates activation on the existing TestCube.</summary>
        [ContextMenu("Simulate Activate TestCube")]
        public void SimulateActivateTestCube() => SimulateActivate("TestCube");

        /// <summary>Simulates inspection on the existing TestCube.</summary>
        [ContextMenu("Simulate Inspect TestCube")]
        public void SimulateInspectTestCube() => SimulateInspect("TestCube");

        /// <summary>Activates the configured ventilation terminal.</summary>
        [ContextMenu("Activate ventilation")]
        public void ActivateVentilation()
        {
            VentTerminal terminal = FindFirstObjectByType<VentTerminal>();
            if (terminal == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: VentTerminal non trovato.");
                return;
            }
            terminal.TurnOn();
        }

        /// <summary>Advances ventilation escalation by twenty debug seconds.</summary>
        [ContextMenu("Advance ventilation escalation 20s")]
        public void AdvanceVentilationDebugTime()
        {
            VentilationEscalation escalation = FindFirstObjectByType<VentilationEscalation>();
            if (escalation == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: VentilationEscalation non trovata.");
                return;
            }
            escalation.AdvanceDebugTime(20f);
            escalation.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Sets the ventilation Update-loop debug multiplier to ten.</summary>
        [ContextMenu("Set ventilation debug multiplier x10")]
        public void SetVentilationDebugMultiplier()
        {
            VentilationEscalation escalation = FindFirstObjectByType<VentilationEscalation>();
            if (escalation == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: VentilationEscalation non trovata.");
                return;
            }
            escalation.SetDebugTimeMultiplier(10f);
            UnityEngine.Debug.Log("[CA] Ventilation debug multiplier: x10.");
        }

        /// <summary>Prints the current ventilation escalation stage.</summary>
        [ContextMenu("Print ventilation stage")]
        public void PrintVentilationStage()
        {
            VentilationEscalation escalation = FindFirstObjectByType<VentilationEscalation>();
            if (escalation != null)
                escalation.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Simulates the same Inspected event used by the SDS interaction.</summary>
        [ContextMenu("Simulate SDS inspection")]
        public void SimulateSdsInspection()
        {
            SdsItem sds = FindFirstObjectByType<SdsItem>();
            if (sds == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: SdsItem non trovato.");
                return;
            }

            ScenarioInteractable interactable = sds.GetComponent<ScenarioInteractable>();
            if (interactable == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: ScenarioInteractable SDS non trovato.");
                return;
            }

            interactable.RaiseInspected();
        }

        /// <summary>Toggles the PPE cabinet through its interaction surface.</summary>
        [ContextMenu("Toggle PPE cabinet")]
        public void TogglePPECabinet()
        {
            PPECabinet cabinet = FindFirstObjectByType<PPECabinet>();
            if (cabinet == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: PPECabinet non trovato.");
                return;
            }
            cabinet.Toggle();
        }

        /// <summary>Equips the configured correct PPE debug set.</summary>
        [ContextMenu("Equip correct PPE set")]
        public void EquipCorrectPPESet()
        {
            PlayerPPEState state = FindFirstObjectByType<PlayerPPEState>();
            if (state == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: PlayerPPEState non trovato.");
                return;
            }
            state.DebugEquipCorrectSet();
            state.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Equips the configured decoy PPE debug set.</summary>
        [ContextMenu("Equip decoy PPE set")]
        public void EquipDecoyPPESet()
        {
            PlayerPPEState state = FindFirstObjectByType<PlayerPPEState>();
            if (state == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: PlayerPPEState non trovato.");
                return;
            }
            state.DebugEquipDecoySet();
            state.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Prints the current PPE slots and required-set result.</summary>
        [ContextMenu("Print PPE state")]
        public void PrintPPEState()
        {
            PlayerPPEState state = FindFirstObjectByType<PlayerPPEState>();
            if (state != null)
                state.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Forces one substance-contact evaluation.</summary>
        [ContextMenu("Force substance contact")]
        public void ForceSubstanceContact()
        {
            SubstanceContactSensor sensor = FindFirstObjectByType<SubstanceContactSensor>();
            if (sensor == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: SubstanceContactSensor non trovato.");
                return;
            }
            sensor.ForceContact();
        }

        /// <summary>Prints current player hearts and death state.</summary>
        [ContextMenu("Print health state")]
        public void PrintHealthState()
        {
            PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
            if (health != null)
                health.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Fills every configured spill perimeter slot for a debug check.</summary>
        [ContextMenu("Fill spill perimeter")]
        public void FillSpillPerimeter()
        {
            PerimeterSlots perimeter = FindFirstObjectByType<PerimeterSlots>();
            if (perimeter == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: PerimeterSlots non trovato.");
                return;
            }
            perimeter.DebugFillAll();
        }

        /// <summary>Prints the current spill perimeter state.</summary>
        [ContextMenu("Print spill perimeter state")]
        public void PrintSpillPerimeterState()
        {
            PerimeterSlots perimeter = FindFirstObjectByType<PerimeterSlots>();
            if (perimeter != null)
                perimeter.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Runs the complete perimeter, coverage and disposal debug sequence.</summary>
        [ContextMenu("Complete cleanup debug")]
        public void CompleteCleanupDebug()
        {
            CleanupSystem cleanup = FindFirstObjectByType<CleanupSystem>();
            if (cleanup == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: CleanupSystem non trovato.");
                return;
            }
            cleanup.DebugCompleteCleanup();
        }

        /// <summary>Prints current cleanup coverage and disposal state.</summary>
        [ContextMenu("Print cleanup state")]
        public void PrintCleanupState()
        {
            CleanupSystem cleanup = FindFirstObjectByType<CleanupSystem>();
            if (cleanup != null)
                cleanup.PrintState(UnityEngine.Debug.Log);
        }

        /// <summary>Forces the safe-zone exit resolution.</summary>
        [ContextMenu("Force safe exit")]
        public void ForceExitSafe()
        {
            ExitSafeZone exit = FindFirstObjectByType<ExitSafeZone>();
            if (exit == null)
            {
                UnityEngine.Debug.LogError("[CA] DebugCommands: ExitSafeZone non trovato.");
                return;
            }
            exit.ForceExit();
        }

        /// <summary>Prints the Director result and ActionLog recap.</summary>
        [ContextMenu("Print result recap")]
        public void PrintResultRecap()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director != null)
                director.PrintResult(UnityEngine.Debug.Log);
        }

        /// <summary>
        /// Runs the complete Chemical Alert scenario in sequence using Simulate* commands
        /// and logs PASS/FAIL for each step (ventilation, SDS, PPE, perimeter, sheets, disposal, exit).
        /// Can be re-run after a scenario reset.
        /// </summary>
        [ContextMenu("RunSmokeTest")]
        public void RunSmokeTest()
        {
            ChemicalAlertDirector director = FindDirector();
            if (director != null)
            {
                if (director.CurrentPhase == ChemicalAlertDirector.Phase.Briefing)
                {
                    director.AdvancePhase();
                    director.AdvancePhase();
                }
                else if (director.CurrentPhase == ChemicalAlertDirector.Phase.Scan)
                {
                    director.AdvancePhase();
                }
            }

            // 1. Ventilazione
            VentTerminal ventTerminal = FindFirstObjectByType<VentTerminal>();
            VentilationSystem ventilationSystem = FindFirstObjectByType<VentilationSystem>();
            if (ventTerminal != null)
                SimulateActivate(ventTerminal.gameObject.name);
            bool ventPass = ventTerminal != null && ventTerminal.IsOn &&
                (ventilationSystem == null || ventilationSystem.IsOn);
            UnityEngine.Debug.Log($"[CA] SmokeTest 1/7 ventilazione: {(ventPass ? "PASS" : "FAIL")}");

            // 2. SDS
            SdsItem sds = FindFirstObjectByType<SdsItem>();
            if (sds != null)
            {
                SimulateGrab(sds.gameObject.name);
                SimulateInspect(sds.gameObject.name);
                SimulateRelease(sds.gameObject.name);
            }
            bool sdsPass = sds != null && sds.HasBeenRead;
            UnityEngine.Debug.Log($"[CA] SmokeTest 2/7 SDS: {(sdsPass ? "PASS" : "FAIL")}");

            // 3. DPI
            PPECabinet cabinet = FindFirstObjectByType<PPECabinet>();
            if (cabinet != null && !cabinet.IsOpen)
                SimulateActivate(cabinet.gameObject.name);

            PlayerPPEState ppeState = FindFirstObjectByType<PlayerPPEState>();
            Puddle puddle = FindFirstObjectByType<Puddle>();
            SubstanceVariant variant = puddle != null && puddle.CurrentVariant != null
                ? puddle.CurrentVariant
                : (sds != null ? sds.CurrentVariant : null);
            PPEItemObject[] ppeObjects = FindObjectsByType<PPEItemObject>(FindObjectsSortMode.None);
            PPEEquipZone[] equipZones = ppeState != null && ppeState.Zones != null && ppeState.Zones.Length > 0
                ? ppeState.Zones
                : FindObjectsByType<PPEEquipZone>(FindObjectsSortMode.None);

            if (variant != null && variant.RequiredPpe != null)
            {
                foreach (PPEItem required in variant.RequiredPpe)
                {
                    if (required == null)
                        continue;

                    PPEItemObject itemObj = ppeObjects.FirstOrDefault(item => item != null && item.ItemData == required);
                    PPEEquipZone zone = equipZones.FirstOrDefault(z => z != null && z.Slot == required.EquipmentSlot);
                    if (itemObj != null && zone != null)
                    {
                        SimulateGrab(itemObj.gameObject.name);
                        itemObj.transform.position = zone.transform.position;
                        SimulateRelease(itemObj.gameObject.name);
                    }
                }
            }
            bool ppePass = ppeState != null && variant != null && ppeState.HasRequiredSet(variant);
            UnityEngine.Debug.Log($"[CA] SmokeTest 3/7 DPI: {(ppePass ? "PASS" : "FAIL")}");

            // 4. Perimetro
            PerimeterSlots perimeter = FindFirstObjectByType<PerimeterSlots>();
            PerimeterSlot[] slots = FindObjectsByType<PerimeterSlot>(FindObjectsSortMode.None)
                .OrderBy(s => s.name)
                .ToArray();
            SpillSausage[] sausages = FindObjectsByType<SpillSausage>(FindObjectsSortMode.None)
                .OrderBy(s => s.name)
                .ToArray();
            int pairCount = Mathf.Min(slots.Length, sausages.Length);
            for (int i = 0; i < pairCount; i++)
            {
                SimulateGrab(sausages[i].gameObject.name);
                sausages[i].transform.position = slots[i].transform.position;
                SimulateRelease(sausages[i].gameObject.name);
            }
            bool perimeterPass = perimeter != null && perimeter.IsComplete &&
                puddle != null && puddle.Contained;
            UnityEngine.Debug.Log($"[CA] SmokeTest 4/7 perimetro: {(perimeterPass ? "PASS" : "FAIL")}");

            // 5. Fogli
            CleanupSystem cleanup = FindFirstObjectByType<CleanupSystem>();
            AbsorbentSheet[] sheets = FindObjectsByType<AbsorbentSheet>(FindObjectsSortMode.None)
                .OrderBy(s => s.name)
                .ToArray();
            int requiredSheets = cleanup != null ? Mathf.Max(1, cleanup.RequiredSheetCount) : sheets.Length;
            int sheetsToPlace = Mathf.Min(requiredSheets, sheets.Length);
            for (int i = 0; i < sheetsToPlace; i++)
            {
                if (puddle != null)
                {
                    SimulateGrab(sheets[i].gameObject.name);
                    sheets[i].transform.position = puddle.transform.position;
                    SimulateRelease(sheets[i].gameObject.name);
                }
            }
            bool sheetsPass = cleanup != null && cleanup.RequiredSheetCount > 0 &&
                cleanup.PlacedCount >= cleanup.RequiredSheetCount;
            UnityEngine.Debug.Log($"[CA] SmokeTest 5/7 fogli: {(sheetsPass ? "PASS" : "FAIL")}");

            // 6. Smaltimento
            WasteBag wasteBag = FindFirstObjectByType<WasteBag>();
            for (int i = 0; i < sheets.Length; i++)
            {
                if (sheets[i] != null && sheets[i].IsPlaced && wasteBag != null)
                {
                    SimulateGrab(sheets[i].gameObject.name);
                    sheets[i].transform.position = wasteBag.transform.position;
                    SimulateRelease(sheets[i].gameObject.name);
                }
            }
            bool disposalPass = cleanup != null && cleanup.DisposedCount >= cleanup.RequiredSheetCount &&
                cleanup.IsCleanupComplete && puddle != null && puddle.Covered;
            UnityEngine.Debug.Log($"[CA] SmokeTest 6/7 smaltimento: {(disposalPass ? "PASS" : "FAIL")}");

            // 7. Uscita
            ForceExitSafe();
            bool exitPass = director != null && director.Outcome == Outcome.Success &&
                director.LastResult != null && director.LastResult.Outcome == Outcome.Success;
            UnityEngine.Debug.Log($"[CA] SmokeTest 7/7 uscita: {(exitPass ? "PASS" : "FAIL")}");

            int passedSteps = (ventPass ? 1 : 0) + (sdsPass ? 1 : 0) + (ppePass ? 1 : 0) +
                (perimeterPass ? 1 : 0) + (sheetsPass ? 1 : 0) + (disposalPass ? 1 : 0) + (exitPass ? 1 : 0);
            UnityEngine.Debug.Log($"[CA] SmokeTest summary: {passedSteps}/7 {(passedSteps == 7 ? "PASS" : "FAIL")}");
        }

        private ChemicalAlertDirector FindDirector()
        {
            ChemicalAlertDirector director = FindFirstObjectByType<ChemicalAlertDirector>();
            if (director == null)
                UnityEngine.Debug.LogError("[CA] DebugCommands: ChemicalAlertDirector non trovato.");
            return director;
        }

        private ScenarioInteractable FindInteractable(string objectName)
        {
            GameObject target = string.IsNullOrWhiteSpace(objectName) ? null : GameObject.Find(objectName);
            ScenarioInteractable interactable = target == null
                ? null
                : target.GetComponentInParent<ScenarioInteractable>();
            if (interactable == null)
                UnityEngine.Debug.LogError($"[CA] DebugCommands: ScenarioInteractable non trovato: {objectName}");
            return interactable;
        }

        /// <summary>Prints the current debug target state to the Unity console.</summary>
        [ContextMenu("Print state")]
        public void PrintState()
        {
            string rig = rigRoot == null ? "null" : rigRoot.position.ToString();
            string marker = teleportMarker == null ? "null" : teleportMarker.position.ToString();
            int markerCount = teleportMarkers == null ? 0 : teleportMarkers.Length;
            UnityEngine.Debug.Log($"[CA] Debug state: rig={rig}; marker={marker}; markers={markerCount}; commands={commands.Count}");
        }

        /// <summary>Prints the configured Chemical Alert difficulty assets.</summary>
        [ContextMenu("Print Chemical Alert configs")]
        public void PrintChemicalAlertConfigs()
        {
            if (chemicalAlertConfigs == null || chemicalAlertConfigs.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[CA] Chemical Alert configs: nessun asset assegnato.", this);
                return;
            }

            foreach (ChemicalAlertConfig config in chemicalAlertConfigs)
            {
                if (config == null)
                {
                    UnityEngine.Debug.LogWarning("[CA] Chemical Alert configs: riferimento nullo.", this);
                    continue;
                }

                UnityEngine.Debug.Log(
                    $"[CA] Config {config.DifficultyId}: duration={config.TotalDurationSeconds}s; " +
                    $"ventilation={config.VentilationCoughThresholdSeconds}/{config.VentilationToxicStageDelaySeconds}/" +
                    $"{config.VentilationFailureDelaySeconds}s; jitter={config.VentilationJitterSeconds}s; " +
                    $"hearts={config.Hearts}; points={config.SuccessPoints}/{config.PartialPoints}/{config.FailurePoints}");
            }
        }

        /// <summary>Runs the pure OutcomeEvaluator self-check and logs every case.</summary>
        [ContextMenu("Run OutcomeEvaluator SelfCheck")]
        public void RunOutcomeEvaluatorSelfCheck()
        {
            bool passed = new OutcomeEvaluator().SelfCheck(UnityEngine.Debug.Log);
            UnityEngine.Debug.Log($"[CA] T06 SelfCheck result: {(passed ? "PASS" : "FAIL")}");
        }
    }
}
