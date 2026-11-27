# REUSE_MAP — Chemical Alert → Evacuation (E00, sola lettura)
CA = `SI81.Tiziano.ChemicalAlert.{Core,Data,Interaction,Systems,Debug}`; Evac = `SI81.Tiziano.Evacuation.*`. Stesso assembly, niente asmdef: non duplicare nomi di classe CA. Verdetti: RIUSA / COPIA (in `Evacuation/`, nome `Evac…`) / NON TOCCARE.
Env (ENV_REPORT): Unity 6000.3.14f1, URP 17.3.0, Input System 1.19 (legacy OFF), Meta XR Core 209.0.0, Quality PC/Mobile; nessun budget prestazionale documentato.

## Core (CA.Core)
- `ActionLog` / `ActionLogEntry`: `Add(id,isCorrect,Dimension,consequenceKey)` + overload con `double timestamp`; `Entries`, `Count`, `Clear()`. **RIUSA** (passare il tempo di gioco Evac come timestamp).
- enum `Dimension {Safety,Awareness,Decision,Execution,Time}`, `Outcome {Success,Partial,Failure}`; `IResettable { void ResetState(); }`. **RIUSA**.
- `ScenarioResult(Outcome, IDictionary<Dimension,int>, ActionLog)`: `Outcome`, `ActionLog`, `SubScores`, `GetSubScore(Dimension)`. **RIUSA**.
- `OutcomeEvaluator.Evaluate(log, hearts, timeRemaining, lossContained, cleanupComplete, ventilationFailed, totalDuration)` + `SelfCheck(Action<string>)`: regole solo CA. **NON TOCCARE**; E02 scrive `EvacEvaluator` nuovo, copiando il pattern SelfCheck.

## Data (CA.Data)
- `ChemicalAlertConfig` (SO): campi `[SerializeField]` + getter read-only (`DifficultyId`, `TotalDurationSeconds`, `Hearts`, `HintsEnabled`, `GuidedHighlights`, `OrderedHints`, `HintIdleSeconds`, `DefeatOnTimeout`, `Success/Partial/FailurePoints`…). **COPIA pattern** → `EvacuationConfig` + 3 SO `SO_EvacuationConfig_<Livello>`.
- `HintSO`: `StepId`, `LocalizationKey`. **RIUSA** (chiavi `evac.hint.*`; il menu CreateAsset dice Chemical Alert: creare le istanze via MCP/script, non da menu).

## Interaction (CA.Interaction)
- `ScenarioInteractable` (sealed): eventi C# `Grabbed/Released/Activated/Inspected/HoverEnter/HoverExit(ScenarioInteractable)` + UnityEvent omonimi; `Grabbable/Activatable/Inspectable`; `SetInteractionEnabled(bool)`; `SetGuidedHighlight(bool)`; `Raise<Evento>()` per ogni evento. **RIUSA**.
  - Nessun metodo `Simulate…` qui: `DebugCommands` simula chiamando `Raise*` (Grab/Release/Activate/Inspect). Il log interno è fisso `[CA]` (sealed, non cambiabile).
  - Highlight: `_EmissionColor` = #E69F00 × pulsazione (0.8 Hz) via `MaterialPropertyBlock` su tutti i `Renderer` figli. [DA VERIFICARE in E01/E04] i materiali graybox devono avere emissione abilitata (keyword `_EMISSION`), altrimenti non si vede.
- `ScenarioHandInteractor` (sealed): `Hovered`, `Held`; campi: inputBindings, headAnchor, controller, hoverRadius 0.12, inspectDistance 0.35, inspectLookDot 0.5. **RIUSA**.
  - Hover = `Physics.OverlapSphere(anchor.position, 0.12)` SENZA layer mask (tutti i layer, trigger inclusi: `queriesHitTriggers=true`), `GetComponentInParent<ScenarioInteractable>`, il più vicino. Trigger → `RaiseActivated()` sull'hovered; grip → grab con reparent.
  - `Inspected` scatta SOLO per oggetto TENUTO vicino alla testa → NON va bene per piani/NPC: servono `EvacInspectDetector` (sguardo ≤1.2 m, 0.75 s) e area NPC ~1 m come sfera trigger su un figlio (l'OverlapSphere la rileva).
- `ScenarioInputBindings`: `GetGripDown/GetGripUp/GetTriggerDown/GetHintDown(A,X)/GetDismissDown(B)` su `OVRInput`. **RIUSA** (unico lettore OVRInput).

## Systems (CA.Systems)
- `ChemicalAlertDirector` (IResettable): enum ANNIDATI `Difficulty{Guided,Operational,Crisis}`, `Phase{Briefing,Scan,Action,Resolution,ResultHandoff}`, `DangerLevel{Down,Mid,Up}`; eventi `PhaseChanged`, `TimeUp`, `ResultReady`, `HintRequested(string)`, `DangerLevelChanged`, `HeartsChanged`; `RecordAction`, `ResetScenario`, `SetDifficulty`, `ForceTimeUp`, `PrintState`. **NON TOCCARE**; **COPIA pattern** in `EvacuationDirector` con enum propri annidati (nessun riferimento al Director CA).
- `PlayerHealth` (IResettable): `Hearts`, `IsDead`, `TakeDamage(kind,sec[,bypass,record])`, eventi `DamageTaken(string,float)`, `Died`, `DangerDetected`; legato al Director CA. **COPIA** → `EvacPlayerHealth` (contatore contatti fuoco separato dai cuori).
- `ScenarioReplayController`: `CurrentSeed`, `SetFixedSeed(int)`, `ClearFixedSeed()`, `AdvanceDebugTime`. Specifico CA. **COPIA pattern** (seed unico → `System.Random` del Director).
- `SubstanceContactSensor` (IResettable, `ForceContact()`): pattern contatto→danno. **COPIA pattern** → `FireContactSensor` (E09).
- `DisturbanceController`: `ReducedEffects`, `EffectiveFrequencyHz` (cap 3 Hz, ×0.5 se ridotto), `SetDisturbanceEnabled/SetReducedEffects/SetFrequency`. **COPIA pattern** per pulsazione allarme ≤3 Hz e flag riduci-effetti.
- `PlayerPPEState`, `PerimeterSlots/Slot`, `Puddle`, `CleanupSystem`, `Ventilation*`, `Sds*`, `PPE*`, `AbsorbentSheet`, `SpillKit`, `SpillSausage`, `VentTerminal`, `ChemicalAlertPanelUI` (UI, vietata): solo CA. **NON TOCCARE**, non riusare.

## Debug (CA.Debug)
- `DebugCommands` (807 righe): `Register(key,description,Action)` / `Unregister(key)`; `Update` solo `#if UNITY_EDITOR || DEVELOPMENT_BUILD` con `Keyboard.current.<tasto>.wasPressedThisFrame` (switch sul tasto maiuscolo); ogni comando ha anche `[ContextMenu]`. **NON TOCCARE**; **COPIA pattern** → `EvacDebugCommands` (E03).
  - Tasti CA già usati: H T R P C O N D F X S G L A I V E M Y Q B K J W Z U · 1–4 6–9 0 · F6 F8 F9. In Evac evitare 0–9 e WASD (Simulator).
  - `RunSmokeTest` (`[ContextMenu]`, public): `FindFirstObjectByType` dei sistemi, simula via `Raise*`, per step `Log(SmokeTest n/N <nome>: PASS|FAIL)`, riepilogo `summary k/N`. Replicare con prefisso `[EV]` ed estendere a ogni task.

## Rig in `SCN_ChemicalAlert` (letto dalla scena aperta, nessuna modifica)
- Root `OVRCameraRig` (Default, pos 0,0,0): `OVRCameraRig, OVRManager, OVRHeadsetEmulator, ScenarioInputBindings, DebugCommands, PlayerPPEState`. Nessun Collider/Rigidbody/CharacterController sul rig e nessuna locomozione.
- `TrackingSpace` → `CenterEyeAnchor` (Camera+AudioListener; figli UI `ChemicalAlertUI`, `ToxicVignetteCanvas`: NON portare), `Left/RightEyeAnchor`, `Left/RightHandAnchor` → `Left/RightControllerAnchor` con `ScenarioHandInteractor` (inputBindings=root, headAnchor=CenterEyeAnchor, L/R Touch, hoverRadius 0.12).
- Hover: nessun layer dedicato (tutto Default) e nessun collider sulle mani. Implicazione E01: per zone/fuoco serve un corpo giocatore (collider trigger + Rigidbody kinematic sul rig, o check di posizione); la locomozione per percorrere il piano non esiste in CA.
