# EVACUATION — Documentazione tecnica (E12)

Fonte di verità: `Docs/Agent/Evaquation/EV_HANDOFF.md` (E00–E11) + codice in `Code/Scripts/Evacuation/`.
Questo documento descrive **ciò che esiste**. Niente UI/animazioni: la UI futura si collegherà ai soli eventi C# e allo stato pubblico del Director.

## Convenzioni

- Cartella: `GROUP/` = `Assets/_Project/Groups/G_Tiziano/`. Tutti i percorsi sotto sono relativi a `GROUP/`.
- Namespace: `SI81.Tiziano.Evacuation` + `.Core`, `.Data`, `.Interaction`, `.Systems`, `.Integration`, `.Debug`.
- Stesso assembly di Chemical Alert (nessun asmdef): si **riusano** le classi CA, non le si duplica.
- Log prefisso `[EV]`. Chiavi testo prefisso `evac.`. Nessuna stringa per l'utente nel codice.
- Nomenclatura asset `SCN_ / SO_ / MAT_ / MESH_ / VFX_ / MNG_`; PascalCase, senza spazi né accenti.
- Ogni sistema che muta stato implementa `IResettable` (di CA) e si registra/unregistra dal Director.

## 1. Script (`Code/Scripts/Evacuation/`)

| Cartella | File | Ruolo |
|---|---|---|
| `Core/` | `EvacActionIds.cs` | ID stabili `evac.lg01`–`evac.lg11`, `evac.replay.start`; derivazione chiavi `.phrase`/`.hint`; severità (`EvacLogSeverity`). |
| `Core/` | `EvacuationEnums.cs` | `EvacDifficulty` (Guided/Operational/Crisis), `EvacExit` (None/Emergency/Internal), `EvacLogSeverity`. |
| `Core/` | `EvacPhaseEnums.cs` | `EvacPhase` (Briefing/Action/Resolution/ResultHandoff), `EvacDangerLevel` (Down/Mid/Up). |
| `Core/` | `EvacuationSnapshot.cs` | `EvacuationSnapshot` + `EvacScoringSettings` (numeri di punteggio tunabili, default dal BRIEF). |
| `Core/` | `EvacuationOutcomeEvaluator.cs` | `EvacuationOutcomeEvaluator`: `Evaluate`, `DetermineOutcome`, `CalculateScores`, `BuildActionLog`, `SelfCheck`. |
| `Data/` | `EvacuationConfig.cs` | ScriptableObject di tuning per livello; nessun numero di gameplay hardcoded. |
| `Interaction/` | `DoorInteractable.cs` | Porte istantanee: `EvacDoorLeaf`, `OnOpened`/`OnClosed`, collider di blocco disattivato da aperta. |
| `Interaction/` | `ElevatorButton.cs` | Pulsante di chiamata: `Pressed`, `OnPressed`, `WasPressed`. |
| `Interaction/` | `EvacNpc.cs` | `EvacNpcState` (Idle/Following/Evacuated), area trigger ~1 m, `FollowStarted`/`Stopped`/`Evacuated`, chiavi dialogo. |
| `Interaction/` | `EvacuationPlanInspectable.cs` | Piano: ispezione via `Activated` o sguardo fermo; `Inspected`, `OnInspected`, `TickGaze`. |
| `Interaction/` | `StairsExitZone.cs` | Zona scale: `EvacExit`, `Reached`, `OnReached`, `TryReach`, `ContainsPlayer`. |
| `Systems/` | `EvacuationDirector.cs` | Confine verso l'esterno: stato, eventi, report, reset, seed, valutazione finale. |
| `Systems/` | `AlarmSystem.cs` | Allarme da `Action`: emissione pulsante ≤3 Hz su `Renderer[]`, `AudioClip` opzionale, `AlarmStateChanged`. |
| `Systems/` | `NpcRegistry.cs` | Spawn dei 5 NPC sugli slot per seed, raccolta, fila, `LayoutSignature`. |
| `Systems/` | `NpcTrailFollower.cs` | Briciole di pane del percorso del giocatore; niente NavMesh. |
| `Systems/` | `EvacGuidedHighlights.cs` | Solo Guided: emissione `#E69F00` pulsante su piani, NPC idle, porta/cartello emergenza. |
| `Systems/` | `EvacFireManager.cs` | Solo Crisis: 5 focolai su 10 `FIRESLOT_*`, `ActiveFires`, `LayoutSignature`, `ValidateSeeds(200)`. |
| `Systems/` | `EvacFireSlot.cs` | Singolo focolaio: `SetActive(active,intensity,reduced)`, particle con tetto. |
| `Systems/` | `EvacFireLaneValidator.cs` | Verifica corsia libera ≥1.2 m sulle polilinee `ROUTE_*`. |
| `Systems/` | `EvacPlayerFireSensor.cs` | Corpo del giocatore (altezza testa 1.6 m) → contatto fuoco con cooldown. |
| `Integration/` | `EvacInteractableBridge.cs` | Adatta `SI81.Interaction.Interactable` del rig condiviso a `ScenarioInteractable` (hover/select/grab). |
| `Integration/` | `EvacuationScenarioController.cs` | Ponte con la UI comune (`ScenarioController`/HUDService): obiettivi, hint, feedback, esito. |
| `Debug/` | `EvacDebugCommands.cs` | Tutti i comandi tasto + `ContextMenu` `Evac/…`; `RunSmokeTest` (49 step). |
| `Debug/` | `EvacSelfCheckRunner.cs` | Runner storico E02 (evaluator + config), soppiantato da `RunSmokeTest`. |

## 2. Prefab (`Level/Prefabs/Evacuation/`)

`MNG_EvacuationDirector`, `EvacAlarm`, `EvacPlan`, `EvacNpc`, `EvacDoor_Room`, `EvacDoor_StairsEmergency`,
`EvacDoor_StairsInternal`, `EvacElevatorButton`, `EvacExitSign`, `EvacStairsExitZone`, `FireSlot`.

Ogni prefab: **root** (logica + collider) + figlio `Visual` (graybox/prop del modello). Dove servirebbe un'animazione c'è uno stato istantaneo + `UnityEvent`.

## 3. Dati (`Code/Scripts/Evacuation/Data/Assets/`)

| Asset | Valori rilevanti |
|---|---|
| `SO_EvacuationConfig_Guided` | tempo illimitato (cronometro), 0 cuori, no fuoco, highlight+hint on, 5 hint ordinati, 0/0/0 punti. |
| `SO_EvacuationConfig_Operational` | 180 s, 3 cuori, no fuoco, no aiuti, 30/10/0 punti. |
| `SO_EvacuationConfig_Crisis` | 120 s, 1 cuore, fuoco on (5/10 focolai, 3 contatti letali, cooldown 2 s), effetti ridotti all'avvio, disturbi on, 50/20/0 punti. |

Comuni: 3 NPC in U1 + 2 in U2; corsia ≥1.2 m; sguardo piano 0.75 s entro 1.2 m; `EvacScoringSettings` default BRIEF.

Hint (`HintSO`, `StepId`→`LocalizationKey`): `SO_EvacHint_01_Plan` (`evac.step.plan`→`evac.hint.plan`), `02_Room1`, `03_Room2`, `04_Stairs`, `05_NoElevator` (chiavi `evac.hint.*` corrispondenti).

## 4. Scena, materiali, modelli, audio

- Scena: `Level/Scenes/SCN_Evacuation.unity`. Istanti `MNG_EvacuationDirector`, `MNG_Scenario`, `MNG_SpawnPoint`, `MNG_EvacNpcRegistry`, `MNG_FireManager`, `MNG_PlayerFireSensor`; `MODEL_Office`; `Environment` con 55 BoxCollider e 0 renderer (serve al validatore corsia).
- Marker under `ScenarioMarkers`: `SPAWN_Player`, `NPCSLOT_R1_01..06`, `NPCSLOT_R2_01..06`, `POS_Plan_R1/R2/Corridor`, `POS_Alarm`, `POS_Elevator`, `POS_ElevatorButton`, `ZONE_StairsEmergency`, `ZONE_StairsInternal`, `ROUTE_R1_01..09`, `ROUTE_R2_01..09`, `FIRESLOT_01..10`. Tutto il gameplay è ancorato ai marker: **nessuna coordinata nel codice**.
- Materiali `Art/Materials/Evacuation/MAT_EV_*` (15): tra cui `MAT_EV_Plan`, `MAT_EV_ExitSign`, `MAT_EV_DoorFire`, `MAT_EV_Alarm`, `MAT_EV_NpcGuided` (emissione abilitata per highlight).
- Modello: `Art/Models/MESH_UFFICIO_COMPLETO.fbx` (istanza a (7.95, 0, 10.492), scala Z −0.771). NPC non inclusi nel FBX: restano i prefab `EvacNpc`.
- Audio: `Audio/Sound/` contiene solo `PLACEHOLDER.txt` (nessun clip definitivo; `AudioClip` opzionale e vuoto = nessun suono).

## 5. Dipendenze da Chemical Alert e da Shared (sola lettura)

| Classe | Verdetto | Uso in Evacuation |
|---|---|---|
| `SI81.Tiziano.ChemicalAlert.Core.ActionLog` / `ActionLogEntry` | **RIUSA** | `Add(double timestamp, string id, bool isCorrect, Dimension dimension, string consequenceKey)`; `ActionRecorded`. |
| `Dimension`, `Outcome`, `IResettable` | **RIUSA** | Sotto-punteggi, esito, reset di ogni sistema. |
| `ScenarioResult` | **RIUSA** | `Outcome`, `SubScores`, `GetSubScore(Dimension)`, `ActionLog`. |
| `OutcomeEvaluator` | **NON TOCCARE** | Regole CA; al suo posto `EvacuationOutcomeEvaluator` (pattern `SelfCheck` copiato). |
| `SI81.Tiziano.ChemicalAlert.Interaction.ScenarioInteractable` | **RIUSA** | Eventi `Grabbed/Released/Activated/Inspected/HoverEnter/HoverExit` + UnityEvent omonimi; `Raise*` usati dal debug. |
| `ScenarioHandInteractor`, `ScenarioInputBindings` | **RIUSA** | Unico lettore `OVRInput`; hover senza layer mask. |
| `HintSO` (`CA.Data`) | **RIUSA** | 5 asset hint Evac con chiavi `evac.hint.*`. |
| `ChemicalAlertConfig`, `ChemicalAlertDirector`, `PlayerHealth`, `DisturbanceController`, `SubstanceContactSensor`, UI CA | **NON TOCCARE / NON RIUSARE** | Pattern copiato nei corrispondenti `Evac*`; scena e prefab CA congelati. |
| `SI81.Rig`, `SI81.UI`, `SI81.Input` (Shared) | SOLA LETTURA | Rig condiviso da `SCN_Core`, ponte HUD in `EvacuationScenarioController`, `Interactable` del RayInteractor. |

⚠ `Debug` è un namespace: usare `using UDebug = UnityEngine.Debug;` dove serve `Debug.Log`.

## 6. Director: stato ed eventi per la UI futura

`SI81.Tiziano.Evacuation.Systems.EvacuationDirector` (prefab `MNG_EvacuationDirector`).

**Stato leggibile** (property): `Phase`, `Difficulty`, `Config`, `Configurations`, `HasTimeLimit`, `EffectiveTimeLimitSeconds`, `Elapsed`, `TimeRemaining`, `Hearts`, `IsAlive`, `Outcome`, `LastResult`, `ActionLog`, `Seed`, `Rng`, `DangerLevel`, `HighlightsEnabled`, `HintsEnabled`, `NextHintKey`, `HintRequests`, `ReducedEffects`, `PlanWasInspected`, `IsAlarmActive`, `NpcCollectedRoom1/2`, `FireContacts`, `ElevatorWasUsed`, `ExitReached`, `EndReason`, `ResettableCount`, `RigRoot`, `SpawnMarker`, `IsSeedFixed`, `ReplayNpcSlots`, `ReplayFireSlots`, `ReplayStartLogged`.

**Eventi C#** (unsubscribe in `OnDisable` già fatto dai sistemi):

| Evento | Firma | Quando |
|---|---|---|
| `PhaseChanged` | `EvacPhase` | Ogni cambio di fase. |
| `PlanInspected` | `string planId` | Prima ispezione di un piano. |
| `AlarmStateChanged` | `bool` | Allarme acceso/spento. |
| `NpcDialogueRequested` | `string key` | `evac.npc.dialogue.follow` / `.already`. |
| `NpcCollected` | `(int room, int total)` | Un NPC entra nella fila. |
| `ExitReported` | `EvacExit` | Uscita raggiunta (`exit.emergency` / `exit.internal`). |
| `FireContact` | `(int n, float intensity)` | Contatto fuoco (Crisis). |
| `DamageTaken` | `(string source, int hearts)` | Sorgente `evac.damage.fire`. |
| `HeartsChanged` | `int` | Variazione cuori e reset. |
| `Died` | — | Cuori a 0. |
| `HintRequested` | `string key` | Hint ordinato/idle (Guided). |
| `DangerLevelChanged` | `EvacDangerLevel` | Down/Mid/Up. |
| `TimeUp` | — | Timer a zero (mai in Guided). |
| `ResultReady` | `ScenarioResult` | Una volta per run, con esito + ActionLog + sotto-punteggi. |
| `ActionRecorded` | `ActionLogEntry` | Ogni entry del log. |
| `ScenarioReset` | — | Fine di `ResetScenario()`. |
| `SeedChanged` | `int` | Nuovo seed. |
| `ReplayStarted` | `(int seed, string npcSlots, string fireSlots)` | Inizio run, con layout. |

**Metodi di report usati dai sistemi**: `ReportPlanInspected`, `ReportNpcCollected`, `ReportExitReached`, `ReportElevatorUsed`, `ReportFireContact`, `ReportRoomExited`, `ReportAlarmState`, `ReportNpcDialogue`, `RequestHint`, `RequestNextHint`, `ReportNpcLayout`, `ReportFireLayout`.
**Controllo**: `StartScenario`, `SetDifficulty`, `AdvancePhase`, `ForceTimeUp`, `AdvanceDebugTime`, `ResetScenario`, `SetFixedSeed`, `ClearFixedSeed`, `SetReducedEffects`, `TeleportRigTo`, `PrintState`, `PrintResult`, `PrintLevelBehavior`, `BuildSnapshot`.

`EndReason` assume i valori `exit.emergency`, `exit.internal`, `elevator`, `timeout`.

## 7. Interazioni e trigger

| Componente | Trigger | Stato / consequences |
|---|---|---|
| `EvacuationPlanInspectable` | `ScenarioInteractable.Activated` (hover+trigger) **oppure** sguardo da fronte ≤1.2 m per 0.75 s | `PlanInspected(planId)` una volta; `evac.lg01`/`evac.lg02`; `OnInspected`. |
| `EvacNpc` | `SphereCollider` ~1 m → `Activated` | Idle→Following (fila, briciole)→Evacuated; `NpcDialogueRequested`, `NpcCollected`, `evac.lg03`/`lg04`. |
| `DoorInteractable` | `Activated` | Aperta/Chiusa istantanea; collider disattivato da aperta; `OnOpened`/`OnClosed`. |
| `ElevatorButton` | `Activated` | `Pressed`, `OnPressed`; `evac.lg06`; Failure (Guided: Partial + Danger Down). |
| `StairsExitZone` | Corpo del giocatore dentro `BoxCollider` | `Reached`, `OnReached`; `exit.emergency` → esito, `exit.internal` → Partial `evac.lg11`. |
| `EvacPlayerFireSensor` / `EvacFireSlot` | Ingresso nel collider focolaio, cooldown 2 s | `FireContact(n,intensity)`, `DamageTaken`, `evac.lg07`/`lg08`; 3° contatto = morte. |

## 8. Chiavi di localizzazione (solo chiavi nel codice)

| Gruppo | Chiavi |
|---|---|
| Piani | `evac.plan.route` (testo delle 3 planimetrie) |
| Dialogo NPC | `evac.npc.dialogue.follow`, `evac.npc.dialogue.already` |
| Hint Guided | `evac.hint.plan`, `evac.hint.room1`, `evac.hint.room2`, `evac.hint.stairs`, `evac.hint.noelevator` |
| Debrief | `evac.log.lg01.phrase` … `evac.log.lg11.phrase`; `evac.log.lg01.hint` … `evac.log.lg11.hint` (derivate da `EvacActionIds.PhraseKey/HintKey`; vedi `EVAC_DEBRIEF_TABLE.md`) |
| Meta replay | `evac.replay.start` (entry di log, **non** un LG di debrief, non punteggiata) |
| Danno | `evac.damage.fire` (sorgente di `DamageTaken`) |

## 9. Esiti e punteggi

- **Failure**: morte (cuori 0 / 3° contatto fuoco), ascensore (non Guided), timeout (non Guided), nessuna uscita.
- **Partial**: scale interne; scale di emergenza con NPC mancanti o piano non ispezionato; ascensore in Guided.
- **Success**: piano ispezionato + 5/5 NPC + scale di emergenza + vivo + entro il tempo + ascensore mai usato.
- Sotto-punteggi 0–100 in `Dimension`: `Safety`, `Awareness`, `Decision`, `Execution`, `Time` (regole e numeri in `EvacuationOutcomeEvaluator` + `EvacScoringSettings`).
- Ogni azione è una `ActionLogEntry` con timestamp = tempo di gioco; `ConsequenceKey` = chiave `.phrase`. `ResultReady` consegna il `ScenarioResult` completo.
- `ResetScenario()` = reset completo di tutti gli `IResettable` + teletrasporto rig a `SPAWN_Player`; seed fresco (anti-collisioni) salvo fissaggio con F8/F9.

## 10. Documenti collegati

- Flowchart decisioni/conseguenze/esiti: `EVAC_FLOWCHART.md`
- Frasi debrief LG01–LG11: `EVAC_DEBRIEF_TABLE.md`
- Comandi di test e procedura Simulator: `EVAC_TEST_COMMANDS_MANUAL.md`
