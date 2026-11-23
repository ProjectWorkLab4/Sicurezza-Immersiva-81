# Chemical Alert — Manuale dei Comandi di Test e Controlli

Questo manuale documenta sia i **controlli del giocatore** (Meta Quest 3 / Meta XR Simulator) sia i **comandi di debug** da tastiera e `ContextMenu` (attivi solo in Editor e Development Build tramite `DebugCommands` su `OVRCameraRig`).

> **Nota importante per l'uso con Meta XR Simulator:** quando la finestra esterna del Meta XR Simulator ha il focus del sistema operativo, Unity disabilita `Keyboard.current` nella Game View (`PointersAndKeyboardsRespectGameViewFocus`) e intercetta i tasti `1/2/3/WASD`. Per usare i tasti da tastiera di `DebugCommands`, **cliccare prima sulla Game View di Unity**, oppure eseguire qualsiasi comando dal menu **`OVRCameraRig > DebugCommands > ContextMenu (⋮)`** nell'Inspector (che funziona sempre, anche senza focus sulla Game View).

---

## 1. Controlli del Giocatore (Meta Quest 3 / Meta XR Simulator)

Tutti gli input dei controller passano esclusivamente da `ScenarioInputBindings` (`LTouch` e `RTouch`):

| Azione Giocatore | Input Controller (`OVRInput`) | Comportamento in Scena |
|---|---|---|
| **Hover** | Avvicinare la mano (`LTouch` o `RTouch`) entro `0.12 m` da un oggetto | Emette `HoverEnter` / `HoverExit` sul `ScenarioInteractable` più vicino. |
| **Grab (Afferra)** | Premere e tenere premuto **Grip** (`Button.PrimaryHandTrigger`) | Afferra l'oggetto `Grabbable` (`SDSItem`, `PPEItemObject`, `SpillSausage`, `AbsorbentSheet`). |
| **Release (Rilascia)** | Rilasciare **Grip** (`Button.PrimaryHandTrigger`) | Rilascia l'oggetto; se vicino a uno slot compatibile (`PPEEquipZone`, `PerimeterSlot`, `Puddle`, `WasteBag`), esegue lo snap automatico. |
| **Activate (Attiva)** | Premere **Trigger** (`Button.PrimaryIndexTrigger`) a mano libera su un oggetto in hover | Attiva l'oggetto `Activatable` (`VentilationButton`, `PPECabinet`, `SpillKit`). |
| **Inspect (Ispeziona SDS)** | Portare l'oggetto afferrato (`SDSItem`) entro `0.35 m` davanti al visore (`CenterEyeAnchor`, `dot >= 0.5`) | Emette `Inspected` → `SdsRead(CurrentVariant)` e registra `sds.inspect` in `ActionLog`. |
| **Richiesta Hint (solo Guided)** | Premere il tasto **A** sul controller destro (`Button.One` su `RTouch`) in fase `Action` | Richiede manualmente il prossimo hint ordinato (`HintRequested(key)`). |

---

## 2. Tabella Completa dei Comandi di Debug (`DebugCommands`)

Ogni comando può essere invocato premendo il **tasto** corrispondente (con la Game View di Unity in focus) oppure tramite **`OVRCameraRig > DebugCommands > ContextMenu (⋮)`**.

### 2.1 Generali, Navigazione e Diagnostica
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `H` | `Print command list` | Elenca tutti i comandi registrati in `DebugCommands`. | `[CA] Debug commands: 0: ... \| 1: ...` |
| `T` | `Teleport rig to marker` | Teletrasporta `OVRCameraRig` in ciclo sui marker di scena (`SPAWN_Player`, `POS_Barrel`, `POS_Puddle`, `POS_VentButton`, `POS_Truck`, `POS_SDS`, `POS_PPECabinet`, `POS_SpillKit`, `POS_WasteBag`, `ZONE_ExitSafe`, `ZONE_SpillArea`). | `[CA] Debug teleport: OVRCameraRig -> <MarkerName>` |
| `P` | `Print state` | Stampa posizione del rig, marker corrente e conteggio comandi. | `[CA] Debug state: rig=...; pos=...` |
| `C` | `Print Chemical Alert configs` | Stampa i parametri dei 3 `SO_ChemicalAlertConfig` (`Guided`, `Operational`, `Crisis`). | `[CA] Config Guided: ...`, `[CA] Config Operational: ...`, `[CA] Config Crisis: ...` |
| `O` | `Run OutcomeEvaluator SelfCheck` | Esegue i 9 test deterministici dell'`OutcomeEvaluator`. | `[CA] T06 SelfCheck: 9/9 PASS` |
| `7` | `RunSmokeTest` | Esegue l'intera partita perfetta in 7 passi (`1/7` ventilazione, `2/7` SDS, `3/7` DPI, `4/7` perimetro, `5/7` fogli, `6/7` smaltimento, `7/7` uscita). | `[CA] SmokeTest 1/7..7/7: PASS` e `[CA] SmokeTest summary: 7/7 PASS` (`Outcome=Success`, `100/100/100/100/100`). |

### 2.2 Regia, Fasi, Difficoltà ed Esiti (`ChemicalAlertDirector`)
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `R` | `Reset ChemicalAlertDirector` | Resetta l'intero scenario a `Phase.Briefing` e prepara un nuovo replay. | `[CA] ReplayStart: ...` e `[CA] Director reset.` |
| `X` | `Reset ChemicalAlertDirector three times` | Esegue 3 reset consecutivi per verificare l'idempotenza del reset. | `[CA] Director reset x3.` + snapshot stato. |
| `N` | `Advance director phase` | Avanza di una fase (`Briefing` → `Scan` → `Action` → `Resolution`). | `[CA] Director state: phase=...` |
| `D` | `Cycle director difficulty` | Cicla `Guided` → `Operational` → `Crisis` e resetta lo scenario. | `[CA] Difficulty applied: <Difficulty>; highlights=...; distractions=...; disturbance=...` |
| `F` | `Force director time up` | Forza la scadenza del timer e la sconfitta per timeout. | `[CA] ResultReady: outcome=Failure; ...` |
| `S` | `Print director state` | Stampa lo stato corrente del Director. | `[CA] Director state: phase=...; difficulty=...; time=...; hearts=...; outcome=...` |
| `5` | `Force safe exit` | Simula l'ingresso del giocatore in `ZONE_ExitSafe`. | `[CA] ResultReady: outcome=Success\|Partial\|Failure; ...` |
| `6` | `Print result recap` | Stampa il recap dell'ultimo `ScenarioResult` e tutte le voci di `ActionLog`. | `[CA] Result recap: outcome=...; scores=...` + elenco `[CA] Result action: ...` |

### 2.3 Livelli di Difficoltà (T15) e Replay / Evento Dinamico (T16)
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `8` | `Request Guided hint` | Richiede manualmente il prossimo hint in modalità `Guided`. | `[CA] HintRequested: key=chemical.hint.<step>; step=...; source=manual.` |
| `9` | `Toggle reduced disturbance effects` | Attiva/disattiva il flag `ReducedEffects` per lo sfarfallio/audio in `Crisis`. | `[CA] Reduced effects: True\|False.` |
| `0` | `Print difficulty behavior` | Stampa stato difficoltà, highlight, hint emessi, pericolo e frequenza disturbo (`≤ 3 Hz`). | `[CA] Difficulty behavior: ...` e `[CA] Disturbance: ... frequency=...Hz (max 3Hz)` |
| `F6` | `Advance replay time 5s` | Avanza di `5 s` il timer dell'evento dinamico in fase `Action` fino a far scattare la seconda perdita (`20–40 s`). | `[CA] SecondSpillOccurred: actionLog=event.second_spill...` e `[CA] Replay state: ...` |
| `F8` | `Fix replay seed` | Fissa il seed al valore `replaySeedForDebug` (default `81081`) e riavvia lo scenario. | `[CA] Replay seed fixed: 81081; restarting scenario.` |
| `F9` | `Use random replay seed` | Sblocca il seed fisso tornando a seed casuali e riavvia lo scenario. | `[CA] Replay seed unlocked; starting a random replay.` |

### 2.4 Ventilazione, SDS, DPI, Contatto Sostanza e Contenimento
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `V` | `Activate ventilation` | Accende `VentilationButton` e ferma l'escalation nascosta. | `[CA] Ventilation turned on: VentilationButton` e `[CA] VentilationSystem: On; escalation stopped.` |
| `E` | `Advance ventilation escalation 20s` | Avanza di `20 s` l'escalation ventilazione (richiede fase `Action` e ventilazione spenta). | A `~40s`: `[CA] Ventilation cough...`; a `~60s`: `[CA] Ventilation toxic stage...`; a `~80s`: `[CA] Ventilation loss...` |
| `M` | `Set ventilation debug multiplier x10` | Imposta il moltiplicatore di velocità dell'escalation ventilazione a `×10`. | `[CA] Ventilation escalation: ...` |
| `Y` | `Print ventilation stage` | Stampa stadio, tempo trascorso, progresso tossico e soglie della ventilazione. | `[CA] Ventilation escalation: stage=...; elapsed=...; thresholds=...` |
| `Q` | `Simulate SDS inspection` | Simula la lettura della `SDSItem` per la variante chimica corrente. | `[CA] SdsRead: SDSItem; variant=variantA\|B\|C` |
| `B` | `Toggle PPE cabinet` | Apre/chiude l'armadietto DPI (`PPECabinet`). | `[CA] PPE cabinet opened\|closed: PPECabinet` |
| `K` | `Equip correct PPE set` | Equipaggia automaticamente i 3 DPI corretti richiesti dalla `SubstanceVariant` attiva. | `[CA] PPE debug equipped: ...` e `requiredSet=True` |
| `J` | `Equip decoy PPE set` | Equipaggia il set DPI decoy/errato. | `[CA] PPE debug equipped: ... decoy=True` e `requiredSet=False` |
| `W` | `Print PPE state` | Stampa i DPI attualmente equipaggiati nei 3 slot e se il set richiesto è completo. | `[CA] PPE state: Head=...; Torso=...; Hands=...; requiredSet=True\|False` |
| `Z` | `Force substance contact` | Simula il contatto diretto con la sostanza chimica nella pozzanghera. | Senza DPI in Operational/Crisis: `[CA] DamageTaken: kind=burn; duration=5.00s; hearts=...`; in Guided: `[CA] DangerDetected...`; con DPI: `[CA] Substance contact blocked by required PPE.` |
| `U` | `Print health state` | Stampa i cuori rimanenti e lo stato di salute del giocatore. | `[CA] PlayerHealth: hearts=...; dead=False\|True` |
| `1` | `Fill spill perimeter` | Posiziona tutti e 6 i `SpillSausage` nei 6 `PerimeterSlot` attorno alla pozzanghera. | `[CA] Puddle contained=True; radius=...` |
| `2` | `Print spill perimeter state` | Stampa quanti slot del perimetro sono occupati (`0..6/6`). | `[CA] PerimeterSlots: 6/6; complete=True` |
| `3` | `Complete cleanup debug` | Completa perimetro, copertura con fogli assorbenti e smaltimento nel `WasteBag`. | `[CA] Puddle covered=True...`, `[CA] CleanupSystem: CleanupComplete.` |
| `4` | `Print cleanup state` | Stampa fogli posati, fogli smaltiti e stato di completamento bonifica. | `[CA] CleanupSystem: placed=3/3; disposed=3/3; complete=True` |
| `G/L/A/I` | `Simulate Grab/Release/Activate/Inspect TestCube` | Verifica unitaria degli eventi di `ScenarioInteractable` su `TestCube`. | `[CA] Grabbed/Released/Activated/Inspected: TestCube` |

---

## 3. Procedure Rapide di Verifica

1. **Smoke Test Completo Automatico (Partita Success `100/100/100/100/100`):**
   - Avviare Play Mode → premere `7` (o `ContextMenu > RunSmokeTest`) → verificare `[CA] SmokeTest summary: 7/7 PASS` → premere `R` → premere `7` per verificare la ripetibilità dopo il reset.
2. **Verifica Partita Partial (Uscita senza contenimento o con errore):**
   - Premere `R` → `N` → `N` (entra in `Action`) → `5` (forza uscita safe) → `6` (recap) → verificare `outcome=Partial`.
3. **Verifica Partita Failure (Timeout o Escalation Ventilazione):**
   - Premere `R` → `N` → `N` → `E` × 5 (supera `~80s` senza ventilazione) oppure `F` (timeout) → verificare `outcome=Failure`.
4. **Verifica Livelli di Difficoltà (T15):**
   - Premere `D` fino a `Guided` → verificare highlight `#E69F00` sugli oggetti interagibili → `N` × 2 → `8` (emette `HintRequested`) → `Z` (emette `DangerDetected` senza perdere cuori).
   - Premere `D` per `Operational` (`3` decoy attivi, nessun aiuto) e ancora `D` per `Crisis` (sfarfallio luci `2.50 Hz ≤ 3 Hz`; premere `9` e `0` per verificare `reducedEffects=True` a `1.25 Hz`).
5. **Verifica Replay ed Evento Dinamico (T16):**
   - Premere `F8` per fissare il seed (`81081`) e verificare che `R` riproduca stessa variante, stesso ordine armadietto DPI, stesse soglie ventilazione e stesso istante `SecondSpill`.
   - Premere `F9` per tornare al seed casuale → `N` × 2 → premere `F6` ripetutamente fino a `[CA] SecondSpillOccurred` → `5` → `6` per verificare `event.second_spill` nel recap.
