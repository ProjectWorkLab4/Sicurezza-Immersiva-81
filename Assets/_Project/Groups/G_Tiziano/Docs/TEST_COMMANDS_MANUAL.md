# Chemical Alert — Manuale dei Comandi di Test e Controlli

Questo manuale documenta i **controlli del giocatore** (Meta Quest 3 / Meta XR Simulator) e i **comandi di debug** da tastiera e `ContextMenu` (attivi solo in Editor e Development Build tramite `DebugCommands` su `OVRCameraRig`).

**Ultimo aggiornamento:** 2026-11-24 — allineato alle regole a ordine libero (3 DPI × 2 copie, anello singolo, 3 fogli, esito automatico) e ai 38 comandi registrati.

> **Nota importante per l'uso con Meta XR Simulator:** quando la finestra esterna del Meta XR Simulator ha il focus del sistema operativo, Unity disabilita `Keyboard.current` nella Game View (`PointersAndKeyboardsRespectGameViewFocus`) e intercetta i tasti `1/2/3/WASD`. Per usare i tasti da tastiera di `DebugCommands`, **cliccare prima sulla Game View di Unity**, oppure eseguire qualsiasi comando dal menu **`OVRCameraRig > DebugCommands > ContextMenu (⋮)`** nell'Inspector (che funziona sempre, anche senza focus sulla Game View).

---

## 1. Controlli del Giocatore (Meta Quest 3 / Meta XR Simulator)

Tutti gli input dei controller passano esclusivamente da `ScenarioInputBindings` (`LTouch` e `RTouch`).

| Azione Giocatore | Input Controller (`OVRInput`) | Comportamento in Scena |
|---|---|---|
| **Hover** | Avvicinare la mano (`LTouch` o `RTouch`) entro `0.12 m` da un oggetto | Emette `HoverEnter` / `HoverExit` sul `ScenarioInteractable` più vicino. |
| **Grab (Afferra)** | Premere e tenere premuto **Grip** (`Button.PrimaryHandTrigger`) | Afferra `SDSItem`, `PPEItemObject`, `SpillSausage`, `AbsorbentSheet`. **I DPI vengono acquisiti al Grab**, non al rilascio. |
| **Release (Rilascia)** | Rilasciare **Grip** (`Button.PrimaryHandTrigger`) | Rilascia l'oggetto; l'anello si aggancia al `PerimeterSlot` entro `0.35 m`, il foglio si posa sulla pozzanghera entro `0.75 m`. |
| **Activate (Attiva)** | Premere **Trigger** (`Button.PrimaryIndexTrigger`) a mano libera su un oggetto in hover | Attiva `VentilationButton` (`VentTerminal`), `PPECabinet`, `SpillKit`; su SDS già letta apre/chiude il pannello. |
| **Inspect (Ispeziona SDS)** | Portare l'SDS afferrata entro `0.35 m` davanti al visore (`CenterEyeAnchor`, `dot >= 0.5`) | Emette `SdsRead` e registra `sds.inspect`. |
| **Chiudi pannello SDS** | Premere **B** sul controller destro (`RawButton.B` su `RTouch`) | `SdsPanelDismissRequested` → il pannello viene nascosto. In alternativa, riattivare l'SDS. |
| **Richiesta Hint (solo Guided)** | Premere **A** sul controller destro (`Button.One` su `RTouch`) in fase `Action` | Richiede il prossimo hint ordinato (`HintRequested(key)`). |

**DPI obbligatori:** guanti chimici (`chemical.gloves`), respiratore (`respirator`), visiera (`face.shield`). Il mobiletto contiene 2 copie corrette per tipo; raccoglierne una disabilita la gemella.

---

## 2. Tabella Completa dei Comandi di Debug (`DebugCommands`)

Ogni comando può essere invocato premendo il **tasto** (Game View in focus) oppure tramite **`OVRCameraRig > DebugCommands > ContextMenu (⋮)`**. Il tasto `5` (vecchia uscita safe) **non esiste più**: non produce alcun log.

### 2.1 Generali, Navigazione e Diagnostica
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `H` | `Print command list` | Elenca i 38 comandi registrati. | `[CA] Debug commands: ...` |
| `T` | `Teleport rig to marker` | Teletrasporta il rig in ciclo sui marker: `SPAWN_Player`, `POS_Barrel`, `POS_Puddle`, `POS_VentButton`, `POS_Truck`, `POS_SDS`, `POS_PPECabinet`, `POS_SpillKit`, `ZONE_SpillArea`. | `[CA] Debug teleport: OVRCameraRig -> <MarkerName>` |
| `P` | `Print state` | Stampa posizione rig, marker corrente, numero marker e numero comandi. | `[CA] Debug state: rig=...; marker=...; markers=9; commands=38` |
| `C` | `Print Chemical Alert configs` | Stampa durata, soglie ventilazione, jitter, cuori e punti dei 3 config. | `[CA] Config Guided: ...`, `[CA] Config Operational: ...`, `[CA] Config Crisis: ...` |
| `O` | `Run OutcomeEvaluator SelfCheck` | Esegue i 9 test deterministici dell'`OutcomeEvaluator`. | `[CA] T06 SelfCheck: 9/9 PASS` e `[CA] T06 SelfCheck result: PASS` |
| `7` | `RunSmokeTest` | Esegue la partita perfetta in 7 passi: `1/7` ventilazione, `2/7` SDS, `3/7` DPI, `4/7` perimetro (anello singolo), `5/7` fogli, `6/7` assorbimento `4.5 s`, `7/7` esito automatico. | `[CA] SmokeTest 1/7..7/7: PASS` e `[CA] SmokeTest summary: 7/7 PASS` (`Success`, `100/100/0/100/100`) |
| `X` | `Reset ChemicalAlertDirector three times` | Esegue 3 reset consecutivi per verificare l'idempotenza. | `[CA] Director reset x3.` + `[CA] Director state: ...` |
| `R` | `Reset ChemicalAlertDirector` | Resetta lo scenario e avvia un nuovo replay direttamente in `Action`. | `[CA] ReplayStart: ...` e `[CA] Director reset.` |

### 2.2 Regia, Fasi, Difficoltà ed Esiti (`ChemicalAlertDirector`)
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `N` | `Advance director phase` | Avanza di una fase. **Attenzione:** dopo `R` la fase è già `Action`; un `N` porta a `Resolution`, il successivo chiude con `Partial`. | `[CA] Director state: phase=...` |
| `D` | `Cycle director difficulty` | Cicla `Guided` → `Operational` → `Crisis` e resetta lo scenario. | `[CA] Difficulty applied: <Difficulty>; highlights=...; distractions=...; disturbance=...` |
| `F` | `Force director time up` | Forza la scadenza del timer e la sconfitta per timeout (funziona in tutte le difficoltà come comando esplicito). | `[CA] ResultReady: outcome=Failure; ...` |
| `S` | `Print director state` | Stampa fase, difficoltà, tempo, cuori, esito e numero azioni. | `[CA] Director state: phase=...; difficulty=...; time=...; hearts=...; outcome=...; contained=...; cleanup=...; actions=...` |
| `6` | `Print result recap` | Stampa l'ultimo `ScenarioResult` e tutte le voci di `ActionLog`. | `[CA] Result recap: outcome=...; scores=...` + `[CA] Result action: ...` |
| `8` | `Request Guided hint` | Richiede manualmente il prossimo hint in `Guided`. | `[CA] HintRequested: key=chemical.hint.<step>; step=...; source=manual.` |
| `9` | `Toggle reduced disturbance effects` | Attiva/disattiva `ReducedEffects` per lo sfarfallio/audio in `Crisis`. | `[CA] Reduced effects: True\|False.` |
| `0` | `Print difficulty behavior` | Stampa difficoltà, highlight, hint, distrattori, pericolo e disturbo. | `[CA] Difficulty behavior: ...` e `[CA] Disturbance: ...` |

### 2.3 Replay ed Evento Dinamico (T16)
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `F6` | `Advance replay time 5s` | Avanza di `5 s` il timer della seconda perdita (solo in `Action`, finché non scatta). | `[CA] SecondSpillOccurred: ...` e `[CA] Replay state: ...` |
| `F8` | `Fix replay seed` | Fissa il seed a `replaySeedForDebug` (default `81081`) e riavvia lo scenario. | `[CA] Replay seed fixed: 81081; restarting scenario.` |
| `F9` | `Use random replay seed` | Torna a seed casuali e riavvia lo scenario. | `[CA] Replay seed unlocked; starting a random replay.` |

**Log atteso di `ReplayStart`:** `seed=<int>; fixed=<True|False>; variant=variantA|B|C; SDS=chemical.sds.variantX.title; requiredPPE=chemical.gloves,respirator,face.shield; secondSpillWindow=20.00-40.00s; scheduledAt=<20.00-40.00>s.`
**Validazione armadietto:** `[CA] PPE cabinet validated: 3 required types, 2 interactive copies each; no decoys.`

### 2.4 Ventilazione, SDS e DPI
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `V` | `Activate ventilation` | Accende `VentilationButton` e ferma l'escalation nascosta. | `[CA] Ventilation turned on: VentilationButton`, `[CA] VentilationSystem: On; escalation stopped.` |
| `E` | `Advance ventilation escalation 20s` | Avanza di `20 s` l'escalation (richiede `Action` e ventilazione spenta). | A `~40 s`: `Ventilation cough`; a `~60 s`: `Ventilation toxic stage`; a `~80 s`: `Ventilation loss` |
| `M` | `Set ventilation debug multiplier x10` | Imposta il moltiplicatore dell'escalation a `×10`. | `[CA] Ventilation debug multiplier: x10.` |
| `Y` | `Print ventilation stage` | Stampa stadio, tempo, progresso e soglie. | `[CA] Ventilation escalation: stage=...; thresholds=...` |
| `Q` | `Simulate SDS inspection` | Simula la lettura della SDS per la variante corrente. | `[CA] SdsRead: SDSItem; variant=variantA\|B\|C` |
| `B` | `Toggle PPE cabinet` | Apre/chiude l'armadietto DPI. | `[CA] PPE cabinet opened\|closed: PPECabinet` |
| `K` | `Equip correct PPE set` | Acquisisce i 3 DPI corretti della variante attiva. | `[CA] PPE acquired: <id>; count=N/3.` e `[CA] PPE state: acquired=3/3; ... requiredSet(...)=True` |
| `J` | `Equip decoy PPE set` | Non più supportato: segnala che non esistono decoy. | `[CA] PPE debug: no decoy set is configured in the current gameplay.` |
| `W` | `Print PPE state` | Stampa DPI acquisiti e completezza del set. | `[CA] PPE state: acquired=.../3; mask=...; gloves=...; eyes=...; requiredSet(...)=...` |
| `Z` | `Force substance contact` | Forza una valutazione di contatto con la perdita. | Senza guanti: `[CA] ErrorReported: key=chemical.error.gloves; ...`; con guanti: `[CA] Substance approach safe: gloves acquired.`; se la perdita è già contenuta: nessun log. |
| `U` | `Print health state` | Stampa cuori e stato di morte. | `[CA] Health: hearts=...; dead=False\|True; invulnerable=...` |

### 2.5 Contenimento, Fogli e Bonifica
| Tasto | Voce ContextMenu | Cosa fa | Risultato / Log atteso in Console `[CA]` |
|---|---|---|---|
| `1` | `Fill spill perimeter` | Posiziona l'unico `SpillSausage_0` nell'unico `PerimeterSlot_0`. | `[CA] Perimeter slot filled: 1/1`, `[CA] Puddle contained=True; radius=...`, `[CA] Perimeter complete: puddle contained.` |
| `2` | `Print spill perimeter state` | Stampa slot occupati e stato di contenimento. | `[CA] Perimeter: filled=1/1; complete=True; contained=True` |
| `3` | `Complete cleanup debug` | Completa perimetro (se serve), posa i 3 fogli e avanza l'assorbimento di `4.5 s`. | `[CA] Absorption started: sheets=3; duration=4.50s.`, `[CA] Puddle absorption completed: radius=0.`, `[CA] CleanupComplete: absorbed=3/3.` |
| `4` | `Print cleanup state` | Stampa fogli posati, assorbiti, contenimento e completamento. | `[CA] Cleanup: placed=.../3; absorbed=...; contained=...; absorptionStarted=...; complete=...` |

### 2.6 Comandi Legacy su `TestCube` (non più in scena)
`TestCube` è stato rimosso: questi comandi restano registrati ma producono un errore di riferimento. Servono solo come diagnostica dello strato `ScenarioInteractable`.

| Tasto | Voce ContextMenu | Esito atteso |
|---|---|---|
| `G` | `Simulate Grab TestCube` | `[CA] DebugCommands: ScenarioInteractable non trovato: TestCube` |
| `L` | `Simulate Release TestCube` | idem |
| `A` | `Simulate Activate TestCube` | idem |
| `I` | `Simulate Inspect TestCube` | idem |

---

## 3. Procedure Rapide di Verifica

### 3.1 Smoke test completo (ripetibile)
1. Avviare il Play Mode e svuotare la Console.
2. Premere `R` (reset) → premere `7` (o `ContextMenu > RunSmokeTest`).
3. Verificare `[CA] SmokeTest summary: 7/7 PASS` e `[CA] ResultReady: outcome=Success; actions=9; scores=100/100/0/100/100`.
4. Ripetere `R` → `7` altre due volte: ogni run deve dare `7/7 PASS`, `Success`, 3 cuori, 3 DPI, pozzanghera contenuta/coperta, cleanup completo.

### 3.2 Determinismo del replay
1. Premere `F8`: la Console mostra `Replay seed fixed: 81081` e un nuovo `ReplayStart` con `fixed=True`.
2. Premere `R` (o `F8` di nuovo): variante, `scheduledAt` e jitter devono restare identici.
3. Premere `F9`: `Replay seed unlocked` e un nuovo `ReplayStart` con `fixed=False` e seed diverso.
4. Ripetere `F9` più volte: il seed cambia e, in modalità casuale, la variante non si ripete consecutivamente.

### 3.3 Seconda perdita T16 con anello singolo
1. `R` → `F8` (seed `81081`, `scheduledAt ≈ 29.85 s`).
2. Premere `F6` finché compare `[CA] SecondSpillOccurred` (con `81081` servono 6 pressioni: 30 s).
3. Verificare `[CA] SecondSpill: seed=81081; elapsed=30.00s; puddleExpanded=True; variant=variantB.` e `[CA] Puddle second-spill growth: active=True; multiplier=2.00`.
4. Premere `1`: l'anello singolo contiene la pozzanghera (`1/1`, `contained=True`).
5. Premere `2` e attendere/avanzare: il raggio non cresce più (`delta 0`).
6. Premere `3`: assorbimento `4.5 s`, `CleanupComplete`; attendere `~2 s` e premere `6` per il recap: `event.second_spill` deve comparire tra le azioni.

### 3.4 Difficoltà
- `D` fino a `Guided`: highlight `#E69F00` sugli oggetti, `8` emette `HintRequested`, `Z` emette `DangerDetected` senza perdere cuori; `F` forza comunque il timeout.
- `D` fino a `Operational`: cuori `3`, nessun hint/highlight, nessun decoy.
- `D` fino a `Crisis`: cuori **`1`**, flicker luci `2.50 Hz ≤ 3 Hz`; `9` → `reducedEffects=True` a `1.25 Hz`.

### 3.5 Errori e conseguenze
- **Senza guanti:** `R` → `1` (anello) oppure `3` (fogli) → `chemical.error.gloves` e un cuore in meno per tentativo (Operational/Crisis). In Guided il danno è soppresso.
- **Senza maschera:** `R` → `Z` senza DPI e con ventilazione spenta → `chemical.error.no_mask` + cue di tosse, nessun danno.
- **Ordine non consigliato:** `R` → `1` prima di ventilazione/SDS/DPI → `chemical.error.procedure_order`, non bloccante.
- **Aria tossica senza occhi:** `R` → `E` × 3 senza ventilazione → vignetta + `chemical.error.eye_protection`, non bloccante.

### 3.6 Esiti Partial e Failure
- **Partial:** `R` → `N` (passa a `Resolution`) → `N` (chiude con `Partial`) → `6`.
- **Failure timeout:** `R` → `F` → `6`.
- **Failure ventilazione:** `R` → `E` × 5 senza attivare la ventilazione → `LossStarted` → `Failure` (copre il jitter massimo di Crisis).
- **Failure cuori (Crisis):** `D` fino a `Crisis` → `Z` senza guanti → cuori a `0` → `Died` → `Failure`.

### 3.7 Reset e idempotenza
- `X` esegue 3 reset consecutivi e stampa lo stato: dopo il comando la partita è in `Phase.Action`, `outcome=None`, `actions=0`, timer a `180 s`.

---

## 4. Rumore di Console Atteso (non è un errore di gameplay)
In Play Mode e alla chiusura della sessione XR possono comparire:
- `GetHmdColorDesc: Failed to get Hmd color description`
- `ErrorFunctionUnsupported: xrGetInstanceProcAddr ...`
- `Failed to set multimodal hands and controllers mode!`
- `XR: Error setting active audio output driver`
- `[OVRPlugin] [xrDestroySession] ... has undestroyed spaces`

Sono messaggi OVR/XR benigni: non indicano errori di scenario. Gli errori di gameplay sono sempre prefissati `[CA]` e contengono `ErrorReported` o `DebugCommands`.
