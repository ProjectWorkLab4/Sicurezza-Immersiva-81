# HANDOFF — Chemical Alert

## Stato corrente (riscrivere a ogni task, max 15 righe)
- Ultima task completata: AGGIUSTAMENTO T17b — Asset modellatore (panno, salsicciotto, pulsante) in scena.
- T18: creati `Docs/TECHNICAL_DOCS.md` (architettura, eventi Director per la UI, chiavi localizzazione/ActionLog, flowchart Mermaid, 28 script, asset) e `Docs/TEST_COMMANDS_MANUAL.md` (controlli Meta Quest 3/Simulator + 34 comandi di debug).
- Scena composta SOLO da prefab in `Level/Prefabs/` (ogni root è un'istanza); riferimenti cross-object riparati dopo la conversione (erano rimasti asset→asset).
- 3 nuovi asset da `Shared/Art/Models/` istanziati senza duplicarli: `MESH_SALSICCIOTTO_ONEDRIVE` (anello, scala compensata per le catene slot/pozzanghera), `MESH_PANNO_ASSORBENTE_TEAM` (foglio piatto) e `MESH_PULSANTI_TEAM` (pannello 2 bottoni rosso/verde).
- Tavolino `SpillKit` (dietro il camion, Sud): fila di 6 salsicciotti-anello a Z=-3.65 + catasta di 6 panni a (0.55, -3.12); `SheetTray` rimosso perché i panni vi finivano dentro.
- `VentilationButton` sul muro Sud a sinistra del tavolino (`(1.02, 1.40, -4.855)`, pannello a filo muro); rimossa la colonnina provvisoria di fianco al camion; `POS_VentButton` spostato col pulsante.
- Colliders salsicciotti/panni ridimensionati per grab e snap; `ResetState` coerente con le nuove posizioni iniziali.
- In attesa dal modellatore: i 3 DPI (occhialini, maschera, guanti) → armadietto/scaffale a destra del tavolino.
- Play Mode: 3 `RunSmokeTest` consecutivi con reset → `Success` (`7/7 PASS`); 0 errori di gioco; validazione scena 0 issue; scena salvata.
- Prossima task: T19 — Controllo prestazioni (Quest).

## Log (solo append, max 8 righe per task)
### Txx — titolo — data
- Fatto:
- File creati/modificati (percorsi relativi a GROUP/):
- Decisioni/deviazioni dal brief:
- Comandi di test (tasto/menu → risultato atteso):
- Test col Meta XR Simulator (se serve):
- Problemi aperti:

### T00 — Ricognizione — 2026-11-19
- Fatto: rilevati Unity 6000.3.14f1, Input System New (1), URP 17.3.0 (`PC_RPAsset`/`Mobile_RPAsset`), Meta XR Core `209.0.0` (`OVRCameraRig.prefab`), `Shared/` (7 FBX in `Level/Prefabs/`), MCP `unityMCP` (47 tool, Editor non connesso), nessun budget prestazionale documentato.
- File creati/modificati (percorsi relativi a GROUP/): `Docs/Agent/ENV_REPORT.md`, `Docs/Agent/HANDOFF.md`
- Decisioni/deviazioni dal brief: nessuna
- Comandi di test (tasto/menu → risultato atteso): nessun comando di test in T00 (sola lettura; `DebugCommands` previsto in T03).
- Test col Meta XR Simulator (se serve): N/A (T00 sola lettura)
- Problemi aperti: Unity Editor non connesso al bridge MCP (`No Unity Editor instances found`); Build Target attivo non verificabile senza Editor aperto.

### T01 — Scena, rig e struttura — 2026-11-19
- Fatto: create le 5 sottocartelle di `Code/Scripts`; creata `SCN_ChemicalAlert.unity` con istanza `OVRCameraRig` (prefab del package, `_trackingOriginType: 1` = FloorLevel), Directional Light, `Floor` (12×0.2×12 a y=-0.1, grigio scuro) e `TestCube` (0,0.5,3, arancio #E69F00) con materiali URP Lit distinti. Nessuno script C#.
- File creati/modificati (percorsi relativi a GROUP/): `Level/Scenes/SCN_ChemicalAlert.unity`, `Art/Materials/MAT_CA_Floor.mat`, `Art/Materials/MAT_CA_TestCube.mat` (+ `.meta`), `Code/Scripts/{Core,Interaction,Systems,Data,Debug}/`, `Docs/Agent/HANDOFF.md`
- Decisioni/deviazioni dal brief: scena scritta come YAML testuale perché il bridge MCP non vede alcuna istanza Unity (server `mcp-for-unity` attivo, 0 Editor). Prefab NON duplicato/modificato: è un'istanza del prefab in `Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab` (GUID `126d619cf4daa52469682f85c1378b4a`). Nessun `Hello` creato (nessun codice in T01).
- Comandi di test (tasto/menu → risultato atteso): nessun tasto di gioco in T01; `Ctrl+R` in Editor → importa la scena; `File > Open Scene > SCN_ChemicalAlert` → gerarchia: Directional Light, Floor, TestCube, OVRCameraRig.
- Test col Meta XR Simulator (se serve): aprire la scena → Play con Meta XR Simulator attivo → si vede il pavimento grigio e il cubo arancione a 3 m; `WASD` = muoversi, tasto destro del mouse + mouse = guardarsi intorno, frecce = ruotare la vista; i controller simulati si muovono con i binding del pannello `Inputs > Input bindings`. In T01 NON c'è interazione: grab/activate arrivano in T02.
- Problemi aperti: bridge MCP non disponibile → nessuna verifica automatica di Play Mode né generazione `.meta` da parte dell'Editor.

### T02 — Interazione provvisoria (grab/activate/inspect) — 2026-11-19
- Fatto: `ScenarioInteractable` (eventi Grabbed/Released/Activated/Inspected/HoverEnter/HoverExit + flag grabbable/activatable/inspectable), `ScenarioHandInteractor` (hover con `OverlapSphereNonAlloc`, grip = afferra/rilascia con parenting + velocità, trigger = attiva, inspect < 0,35 m + dot vista), `ScenarioInputBindings` (UNICO file che legge `OVRInput`), `ScenarioRigBootstrap` (monta gli interattori sulle ancore mani del rig). TestCube con Rigidbody + ScenarioInteractable.
- File creati/modificati (percorsi relativi a GROUP/): `Code/Scripts/Interaction/{ScenarioInputBindings,ScenarioInteractable,ScenarioHandInteractor,ScenarioRigBootstrap}.cs` (+ `.meta`), `Level/Scenes/SCN_ChemicalAlert.unity`, `Docs/Agent/HANDOFF.md`
- Decisioni/deviazioni dal brief: interattori aggiunti a runtime dal bootstrap invece che in scena (YAML scritto a mano senza MCP); un solo lookup una-tantum del rig (`OVRManager.instance` → fallback `FindFirstObjectByType`). Binding provvisori: grip = `Primary/SecondaryHandTrigger`, trigger = `Primary/SecondaryIndexTrigger`.
- Comandi di test (tasto/menu → risultato atteso): grip sx/dx → `[CA] test.cube: grabbed (Left/Right)`; rilascio grip → `released` + il cubo cade con la velocità della mano; trigger su cubo in hover → `activated`; cubo tenuto a <0,35 m e guardato → `inspected`. Nessun comando da tastiera (DebugCommands in T03).
- Test col Meta XR Simulator (se serve): avvicinare il controller al cubo arancione → `hover enter`; premere grip → `grabbed`; rilasciare → `released`; premere trigger → `activated`; portare il cubo vicino al viso → `inspected`.
- Problemi aperti: bridge MCP assente → compilazione verificata solo con `csc` esterno, Play Mode non verificato.

### PULIZIA — Rimozione scaffolding di test — 2026-11-19
- Fatto: rimossi via Unity Editor (MCP) tutti gli artefatti di test di T01–T02; scena ridotta a `OVRCameraRig` + `Directional Light`. Non è stata creata alcuna scena di test: si passa allo scenario reale.
- Rimosso: GameObject `TestCube` (Rigidbody, ScenarioInteractable, `test.cube`), GameObject `Floor` + `MAT_CA_Floor.mat`, GameObject `ScenarioRig` + `ScenarioRigBootstrap.cs`, `MAT_CA_TestCube.mat` (asset cancellati via AssetDatabase → `.meta` rimossi).
- Mantenuto: `Code/Scripts/Interaction/{ScenarioInteractable,ScenarioHandInteractor,ScenarioInputBindings}.cs`; `Level/Scenes/SCN_ChemicalAlert.unity` (solo rig + luce); `Docs/Agent/*` (incluso `ENV_REPORT.md`); struttura cartelle `Art/ Audio/ Code/ Docs/ Level/ UI/` con `PLACEHOLDER.txt`.
- Verifica: compilazione OK (`scriptCompilationFailed = false`), `manage_scene validate` = 0 issue (0 missing script, 0 prefab rotti), Play Mode senza errori (solo warning del package Meta XR/OpenXR).
- Comandi di test (tasto/menu → risultato atteso): nessuno (scaffolding di test rimosso; `DebugCommands` resta in T03).
- Problemi aperti: interazione NON verificata nel Simulator; senza bootstrap non esiste alcun componente che monti `ScenarioHandInteractor` sulle ancore (da reintegrare in T05).

### T01 — conferma stato esistente — 2026-11-19
- Fatto: T01 risulta già completata; nessun asset modificato o ricreato.
- Comandi di test (Editor): `File > Open Scene > SCN_ChemicalAlert` → `OVRCameraRig` + `Directional Light`; `Play` → nessun errore.
- Test col Meta XR Simulator: N/A (T01 non introduce interazioni).
- Problemi aperti: nessun test eseguito in questa sessione.

### T01 — Scena, rig e struttura — 2026-11-19
- Fatto: aggiunti `Floor` e `TestCube`; rig prefab e cartelle richieste verificati, senza modificare il prefab.
- Verifica: scena valida (0 issue); Play Mode: 0 errori Console (restano warning Meta XR/OpenXR nell'Editor Windows).
- Comandi di test: Editor `File > Open Scene > SCN_ChemicalAlert`, poi `Play` → rig, luce, pavimento e cubo presenti; Console senza errori.
- Test col Meta XR Simulator: aprire la scena, verificare la vista e muovere testa/controller (verifica utente ancora pendente).
- Comandi debug tastiera: nessuno per T01.
- Richieste all'utente: completare la verifica visiva e del tracking nel Meta XR Simulator.

### T02 — Interazione provvisoria — 2026-11-19
- Fatto: creati i tre script Interaction; montati bindings sul rig e interattori sulle due ancore controller.
- Fatto: `TestCube` configurato come grabbable/activatable/inspectable con Rigidbody.
- Verifica Editor: compilazione OK, scena valida (0 issue), Play Mode con 0 errori.
- Comandi di test: Simulator, avvicina controller al cubo → `[CA] HoverEnter`; grip → `[CA] Grabbed`; rilascia → `[CA] Released`.
- Comandi di test: trigger mentre il cubo è in hover → `[CA] Activated`; porta il cubo a <0,35 m dalla testa guardandolo → `[CA] Inspected`.

### T03 — Registro comandi di debug — 2026-11-19
- Fatto: aggiunto `DebugCommands` e marker `DebugSpawnMarker`; H elenca i comandi, T teletrasporta, R è placeholder, P stampa lo stato.
- Verifica: compilazione OK, scena valida (0 issue), Play Mode con log `[CA]` per elenco/stato/teletrasporto.
- Comandi di test: Play Mode → `H` → elenco dei 4 comandi in Console.
- Comandi di test: Play Mode → `T` → `OVRCameraRig` va a `DebugSpawnMarker`; Console `[CA] Debug teleport`.
- Comandi di test: Play Mode → `P` → Console `[CA] Debug state` con rig, marker e conteggio.
- Comandi di test: Inspector `DebugCommands` → ContextMenu `Reset scene (placeholder)` → log placeholder.
- Test col Meta XR Simulator: avviare scena, premere H/T/P da tastiera Editor; nessuna interazione controller richiesta.
- Richieste all'utente: confermare opzionalmente i tasti nel Meta XR Simulator.
- Debug tastiera: nessuno in T02; sarà introdotto in T03.
- Richieste all'utente: confermare nel Meta XR Simulator grab/release/activate/inspect.

### T04 — Dati ScriptableObject — 2026-11-19
- Fatto: creati 3 config difficoltà, 9 PPE e 3 varianti sostanza sotto `Code/Scripts/Data/Assets`.
- Fatto: `OnValidate` avvisa su ID/chiavi/soglie/DPI mancanti; asset finali non producono warning `[CA]`.
- Verifica dati: Guided `0/0/0`, Operational `3/30/10`, Crisis `3/50/20`; ogni variante ha 3 DPI richiesti.
- Comando di test: Play Mode → `C` → stampa i 3 config con durata, soglie, jitter, vite e punti.
- Comando di test: Inspector `DebugCommands` → ContextMenu `Print Chemical Alert configs` → stessi 3 log.
- Test col Meta XR Simulator: N/A (T04 aggiunge solo dati e comando debug da tastiera).
- Richieste all'utente: nessuna.

### T05 — Ambiente provvisorio e marker — 2026-11-19
- Fatto: creato graybox 1:1 della stanza, garage/truck praticabile, armadietto, kit, ventilazione, scaffali, barrel/puddle e uscita laterale.
- Fatto: root con collider + figlio `Visual`; trigger per `ZONE_ExitSafe` e `ZONE_SpillArea`; materiali placeholder in `Art/Materials`.
- Fatto: creati tutti gli 11 marker richiesti e assegnati a `DebugCommands`.
- Verifica: struttura OK, scena valida (0 issue), Play Mode senza errori Tiziano.
- Comandi di test: Play Mode → premere `T` 11 volte → Console mostra SPAWN, POS_* e ZONE_* in sequenza.
- Comando di test: Inspector `DebugCommands` → ContextMenu `Teleport rig to marker` → ciclo marker.
- Test col Meta XR Simulator: usare `T` e verificare posizione in scala 1:1; camminare senza attraversare pareti.
- Richieste all'utente: nessuna; modelli `Shared/` non erano elencati, quindi il graybox resta provvisorio.

### T06 — ActionLog e OutcomeEvaluator — 2026-11-19
- Fatto: create classi pure Core per log azioni, risultato, enum e valutazione.
- Fatto: regole Success/Partial/Failure e sotto-punteggi sulle 5 dimensioni.
- Fatto: SelfCheck con 9 casi, incluso successo, perdita non contenuta, danni, cuori, timeout e ventilazione.
- Comando di test: Play Mode → `O` → stampa ogni caso e il riepilogo `9/9 PASS`.
- Comando di test: Inspector `DebugCommands` → ContextMenu `Run OutcomeEvaluator SelfCheck`.
- Verifica: compilazione OK, 0 errori Tiziano, tutti i casi PASS.
- Test col Meta XR Simulator: N/A (logica pura; nessuna interazione richiesta).
- Richieste all'utente: nessuna.

### T07 — ChemicalAlertDirector — 2026-11-19
- Fatto: creato `ChemicalAlertDirector` e prefab `MNG_ChemicalAlertDirector`, con i 3 config assegnati.
- Fatto: fasi, timer Action, timeout, outcome, reset e API eventi verso l'esterno.
- Fatto: `DebugCommands` esteso con fase, difficoltà, timeout, stato, reset x3 e simulazione interazioni.
- Comandi di test: `N` → fase successiva; `D` → cambia difficoltà e resetta; `S` → stampa stato.
- Comandi di test: `F` → forza timeout; `R` → reset; `X` → tre reset consecutivi.
- Comandi di test: `G/L/A/I` → Simulate Grab/Release/Activate/Inspect su `TestCube`.
- Verifica: fasi, timer, timeout, eventi, reset x3 e log interazioni verificati; scena 0 issue.
- Test col Meta XR Simulator: opzionale; T07 è verificabile interamente da tastiera/ContextMenu.

### T08 — Ventilazione ed escalation nascosta — 2026-11-19
- Fatto: creati `VentTerminal`, `VentilationSystem`, `VentilationEscalation`; collegati al Director e a `VentilationButton`.
- Fatto: soglie da config con jitter, eventi cough/toxic progress/loss, log tempi e stop su attivazione.
- Comandi di test: `N` due volte → Action; `E` cinque volte → Cough → Toxic → Loss; `Y` stampa stadio.
- Comandi di test: `V` attiva ventilazione; `M` imposta moltiplicatore x10; `R` resetta.
- Verifica ignorando: escalation completa; attivando dopo tosse: stop senza perdita; reset singolo/x3 OK.
- Test col Meta XR Simulator: in Action premere il pulsante ventilazione col trigger → `[CA] Ventilation turned on`.
- Verifica Editor: compilazione OK, scena 0 issue, 0 errori Tiziano.
- Richieste all'utente: nessuna.

### T09 — Camion e SDS — 2026-11-19
- Fatto: creato `SDSItem` afferrabile/ispezionabile in `POS_SDS`, con Variant A assegnata.
- Fatto: `SdsRead` emesso una sola volta e log `sds.inspect` su Awareness.
- Comando di test: `Q` → simula `Inspected` sullo stesso `ScenarioInteractable` dell'SDS.
- Comando di test: Inspector `DebugCommands` → `Simulate SDS inspection`.
- Verifica: evento/log `[CA] SdsRead`, variantA e reset posizione/stato verificati in Play Mode.
- Test col Meta XR Simulator: afferrare SDS nel camion, portarla a <0,35 m dalla testa e guardarla.
- Atteso: `[CA] SdsRead` una volta; reset Director la riporta a `POS_SDS`.
- Richieste all'utente: confermare il test grab + inspect nel Simulator.

### T10 — Armadietto DPI ed equipaggiamento — 2026-11-19
- Fatto: creati `PPECabinet`, 9 PPE afferrabili e zone Head/Torso/Hands sul rig.
- Fatto: equip su rilascio vicino allo slot, aggancio istantaneo, rimozione su grab e reset.
- Fatto: `PlayerPPEState.HasRequiredSet` verificato con i set corretti delle 3 varianti e con decoy.
- Comandi di test: `B` → apre/chiude armadietto; `K` → set corretto; `J` → set decoy.
- Comando di test: `W` → stampa slot, item equipaggiati e `requiredSet(variantA)`.
- Test col Meta XR Simulator: afferrare un DPI e rilasciarlo vicino alla zona dello slot; grab di nuovo per rimuoverlo.
- Verifica: equip/unequip, set A/B/C, decoy, reset e cabinet verificati; scena 0 issue.
- Richieste all'utente: confermare il rilascio vicino alle tre zone nel Simulator.

### T11 — Pozzanghera, contatto e salute — 2026-11-19
- Fatto: `Puddle` cresce con la variante corrente e si ferma con `Contained`.
- Fatto: `SubstanceContactSensor` collegato al trigger della pozzanghera; evento burn 5 s e Safety log.
- Fatto: `PlayerHealth` con cuori config, Guided danger-only, invulnerabilità e `Died`.
- Comandi di test: `Z` → forza contatto; `U` → stampa cuori/stato morte.
- Test: PPE corretto non danneggia; decoy/assenza set danneggiano; tre danni producono `Died`.
- Test: Guided emette `DangerDetected` senza danno; reset ripristina raggio e salute.
- Test col Meta XR Simulator: entrare nella pozzanghera con mani/corpo senza set DPI completo.
- Verifica Editor: compilazione OK, scena 0 issue, nessun errore Tiziano.

### T12 — Kit antisversamento e perimetro — 2026-11-19
- Fatto: creati `SpillKit`, 6 `SpillSausage`, `PerimeterSlots` e 6 `PerimeterSlot`.
- Fatto: aggancio per prossimità, contenimento completo e log errore d'ordine non bloccante.
- Comandi di test: `1` → riempie il perimetro; `2` → stampa filled/complete/contained.
- Comando di test: Inspector `DebugCommands` → `Fill spill perimeter` / `Print spill perimeter state`.
- Test: riempimento completo ferma la crescita; 3 slot riempiti non contengono.
- Test: reset Director svuota slot, riporta salsicciotti e riattiva la crescita.
- Test col Meta XR Simulator: afferrare e rilasciare ogni salsicciotto vicino allo slot corrispondente.
- Verifica Editor: compilazione OK, scena 0 issue, nessun errore Tiziano.

### T13 — Fogli assorbenti, copertura e smaltimento — 2026-11-19
- Fatto: creati 6 `AbsorbentSheet`, `WasteItem`, `WasteBag` e `CleanupSystem`.
- Fatto: fogli posabili, conteggio proporzionale al raggio, copertura e `CleanupComplete`.
- Fatto: WasteItem raccolto senza DPI corretto → percorso burn Safety; smaltimento loggato Execution.
- Comandi di test: `3` → completa la sequenza debug; `4` → stampa placed/required/disposed/complete.
- Comando di test: Inspector `DebugCommands` → `Complete cleanup debug` / `Print cleanup state`.
- Verifica: sequenza perimetro→fogli→smaltimento, fogli anticipati, burn e reset verificati.
- Test col Meta XR Simulator: afferrare foglio, posarlo sulla pozzanghera, poi portarlo al WasteBag.
- Verifica Editor: compilazione OK, scena 0 issue, nessun errore Tiziano.

### T14 — Esiti, uscita e retry — 2026-11-19
- Fatto: collegata `ZONE_ExitSafe` al Director tramite `ExitSafeZone`.
- Fatto: Success/Partial/Failure e recap `ResultReady(ScenarioResult)` + ActionLog in Console.
- Comandi di test: `5` → forza uscita safe; `6` → stampa esito e recap.
- Verifica partita Success: SDS/PPE + perimetro/fogli/smaltimento + uscita.
- Verifica partita Partial: uscita senza contenimento.
- Verifica partita Failure: `F`/timeout; retry con `R` verificato pulito.
- Log osservati: tre `ResultReady` con outcome corretti e recap.
- Test col Meta XR Simulator: uscire nella `ZONE_ExitSafe` e verificare il log dell'esito.

### DIAGNOSI — Input Simulator e RunSmokeTest — 2026-11-19
- Fatto: diagnosticati input/Simulator; corretti `case "C"` in `DebugCommands`, grab/unequip in `ScenarioHandInteractor`, log `ActionLog` in `PPEEquipZone.DebugEquip` e `ScenarioInteractable` duplicato su `PPEObject_ChemicalGloves`; aggiunto `RunSmokeTest`.
- File modificati (relativi a GROUP/): `Code/Scripts/Debug/DebugCommands.cs`, `Code/Scripts/Interaction/ScenarioHandInteractor.cs`, `Code/Scripts/Systems/PPEEquipZone.cs`, `Level/Scenes/SCN_ChemicalAlert.unity`, `Docs/Agent/HANDOFF.md`.
- Cosa non funzionava: con Meta XR Simulator in primo piano la Game View Unity perde il focus (`PointersAndKeyboardsRespectGameViewFocus`) e `Keyboard.current` non riceve i tasti (inoltre `1/2/3/WASD` sono usati dal Simulator); in più mancava `case "C"` in `WasPressed`, `PPEObject_ChemicalGloves` aveva due `ScenarioInteractable` e `TryGrab` su DPI equipaggiato veniva annullato da `RemoveItem`.
- Come lanciare lo smoke test: in Play Mode cliccare `OVRCameraRig > DebugCommands > ContextMenu (⋮) > RunSmokeTest` (funziona anche senza focus sulla Game View), oppure cliccare sulla **Game View** di Unity e premere `7` (dopo `R` per reset è rilanciabile).
- Comandi di test: `7` o ContextMenu `RunSmokeTest` → stampa `[CA] SmokeTest 1/7..7/7: PASS` e `[CA] SmokeTest summary: 7/7 PASS`; `R` → reset; `7` → secondo `7/7 PASS`.
- Test col Meta XR Simulator: usare i controller simulati (`LTouch`/`RTouch`, grip = `PrimaryHandTrigger`, trigger = `PrimaryIndexTrigger`) o lanciare `RunSmokeTest` da ContextMenu/Game View.
- Richieste all'utente: per usare i tasti da tastiera di `DebugCommands` mentre il Simulator è aperto, dare focus alla **Game View** di Unity (o usare il `ContextMenu` sull'Inspector di `DebugCommands`).

### T15 — Livelli di difficoltà — 2026-11-20
- Fatto: Guided hint/highlight/pericolo; Operational 3 DPI decoy; Crisis flicker e riduzione effetti.
- File: `Code/Scripts/{Data/ChemicalAlertConfig.cs,Data/HintSO.cs,Interaction/ScenarioInputBindings.cs,Interaction/ScenarioInteractable.cs,Systems/ChemicalAlertDirector.cs,Systems/DisturbanceController.cs,Debug/DebugCommands.cs}`, 5 SO hint, 3 SO config e `Level/Scenes/SCN_ChemicalAlert.unity`.
- Test Play Mode: `D` cicla i livelli; `8`/ContextMenu richiede hint Guided; `9` toggle riduzione effetti; `0` stampa comportamento; `N`×2 + `E`×3 → Mid, `E` → Up, `V` → Down.
- Risultato: compilazione OK, 0 errori console, scena valida; test difficulty, hint manuale/idle, distrattori, livelli pericolo e limite 3 Hz superati.
- Comandi di test: `D` → ciclo/reset; `8` → HintRequested; `9` → riduzione effetti; `0` → stato; `N`×2 + `E`×3 → Mid, `E` → Up, `V` → Down; `R` → reset.
- Test Simulator: Game View in focus; Guided → `N`×2 → premi A su RTouch e verifica `HintRequested`; Operational → verifica i 3 DPI decoy; Crisis → verifica flicker ≤3 Hz.
- Richieste all'utente: collegare le 5 chiavi `chemical.hint.*` al catalogo di localizzazione/UI futura.

### T16 — Replay ed evento dinamico — 2026-11-20
- Fatto: seed replay riproducibile; variante applicata a SDS/PPE/pozzanghera; shuffle 9 DPI; jitter ventilazione seedato; seconda perdita dinamica.
- Config: 3 varianti, pool DPI, finestra seconda perdita 20–40 s, crescita ×2 e clip audio opzionale in ogni `SO_ChemicalAlertConfig`.
- Evento: `SecondSpillOccurred` + `event.second_spill` / `chemical.spill.second` in `ActionLog` e `ScenarioResult`.
- Comandi: `F8` fissa seed Inspector (default 81081); `F9` seed casuale; `N`×2 avvia Action; `F6` avanza 5 s; `5` esito; `6` recap; `R` nuovo replay.
- Test Play Mode: 5 replay variati/coerenti, seed fisso ripetibile, jitter identico, evento a 38.58 s, espansione e recap verificati.
- Risultato: compilazione OK; 0 errori console; validazione scena 0 issue.
- Test Simulator: interagire con SDS e DPI; confrontare `ReplayStart` (variant/SDS/requiredPPE); in Action attendere o usare `F6` finché scatta la seconda perdita, poi verificare crescita e recap.
- Richieste all'utente: nessuna; il segnale audio resta opzionale e non assegnato.

### T17 — Integrazione asset del modellatore — 2026-11-20
- Fatto: integrati in `SCN_ChemicalAlert` tutti gli asset 3D da `Shared/Level/Prefabs/` senza spostarli/duplicarli; costruite scaffalature industriali a 3 ripiani (2 campate per lato) riempite su ogni livello con barili/scatole/soda, armadietto DPI a 3 ripiani, banco `SpillKit`, colonnina `VentilationButton`, 2 ventole e pallet di scarico.
- File: `Level/Scenes/SCN_ChemicalAlert.unity`, `Art/Materials/{MAT_BoxRed,MAT_BoxGreen,MAT_BoxCardboard,MAT_SteelDark,MAT_ShelfBeam,MAT_ShelfDeck,MAT_Hazard,MAT_Safety}.mat`, `Docs/Agent/HANDOFF.md`.
- Decisioni: nessun file `MESH_SCAFFALE*` presente in `Shared/Level/Prefabs/`, quindi create scaffalature industriali a 3 ripiani in scena popolandole con istanze 3D da `Shared/`; DPI lasciati placeholder sui 3 ripiani dell'armadietto.
- Comandi di test: `7` → `RunSmokeTest` (`7/7 PASS`, `Outcome=Success`); `R` → reset; `T` → ciclo marker 3D; `D` → highlight Guided sulle mesh 3D; `Q`/`B`/`K`/`1`/`3`/`5` → passi singoli.
- Test Simulator: verificare visual 3D di stanza, scaffalature a 3 ripiani piene, camion+SDS nel cassone, barile rovesciato, 2 ventole+colonnina pulsante, banco SpillKit, armadietto DPI e barile blu WasteBag.
- Richieste all'utente: se hai un FBX dedicato per la struttura dello scaffale (non presente in `Shared/Level/Prefabs/`) o i modelli 3D dei DPI, aggiungili in `Shared/Level/Prefabs/` per sostituire struttura scaffali e placeholder DPI.

### T18 — Documentazione tecnica e manuale dei comandi — 2026-11-20
- Fatto: redatti `Docs/TECHNICAL_DOCS.md` (architettura, API/eventi `ChemicalAlertDirector` per UI futura, chiavi localizzazione/`ActionLog`, flowchart Mermaid, 28 script, prefab, trigger, SO e asset 3D) e `Docs/TEST_COMMANDS_MANUAL.md` (controlli Meta Quest 3/Simulator, tabella dei 34 comandi di debug e procedure di verifica).
- File: `Docs/TECHNICAL_DOCS.md` (+ `.meta`), `Docs/TEST_COMMANDS_MANUAL.md` (+ `.meta`), `Docs/Agent/HANDOFF.md`.
- Decisioni: documentazione separata in due file Markdown sotto `GROUP/Docs/` allineati al 100% con nomi classi, eventi, chiavi e tasti del codice post-T15/T16/T17.
- Comandi di test: `H` → stampa lista comandi registrati; `7` → `RunSmokeTest` (`7/7 PASS`); `C` → config; `O` → `SelfCheck` (`9/9 PASS`); `0` → stato difficoltà; `6` → recap esito.
- Test Simulator: seguire la procedura in `Docs/TEST_COMMANDS_MANUAL.md` (Sezione 1 e 3).
- Richieste all'utente: nessuna.

### AGGIUSTAMENTO — Scaffale FBX + scena a prefab — 2026-11-20
- Fatto: scaffali ricostruiti con `MESH_SCAFFALE_DA_3_VUOTO_TEAM.fbx` (Ovest 4 unità, Est 3, `PPECabinet` a Sud) con ripiani popolati; convertiti in prefab tutti i root di scena e raggruppati muri/marker; riparati i riferimenti cross-object dopo la conversione (erano rimasti asset→asset).
- File: `Level/Scenes/SCN_ChemicalAlert.unity`, `Level/Prefabs/*.prefab` (nuovi: `Floor`, `Shelf_Left/Right`, `PPECabinet`, `SDSItem`, `Puddle`, `PPEObject`, `SpillSausage`, `AbsorbentSheet`, `CleanupSystem`, `ScenarioMarkers`, `Truck`, `WasteBag`, `VentilationButton`, `Barrel`, `SpillKit`, `ServiceDoor`, `ZONE_ExitSafe`, `TestCube`, `DirectionalLight`, `PerimeterSlots`), `Docs/Agent/HANDOFF.md`.
- Comandi di test: `7` → `RunSmokeTest` → `7/7 PASS` ×3 consecutivi con `R` (`Success`); `H` → lista comandi.
- Richieste all'utente: i modelli 3D dei DPI (9 `PPEObject_*` sono ancora placeholder visivi).

### AGGIUSTAMENTO T17b — Panno, salsicciotto, pulsante — 2026-11-20
- Fatto: montati `MESH_SALSICCIOTTO_ONEDRIVE` (anello) sui 6 `SpillSausage`, `MESH_PANNO_ASSORBENTE_TEAM` sui 6 `AbsorbentSheet` (catasta sul tavolino), `MESH_PULSANTI_TEAM` su `VentilationButton` (muro Sud a sinistra del tavolino); rimossa la colonnina provvisoria e il `SheetTray`.
- File: `Level/Scenes/SCN_ChemicalAlert.unity`, `Level/Prefabs/{SpillSausage,AbsorbentSheet,VentilationButton}.prefab` (override visual/collider/posizioni), `Docs/Agent/HANDOFF.md`.
- Decisioni: salsicciotto = anello arrotolato (scala compensata per catene slot 0.35 e pozzanghera); panni impilati a catasta (i 6 non stavano in fila sul tavolo); pulsante sul lato Est del tavolino (spazio libero tra tavolo e WasteBag).
- Comandi di test: `7` → `RunSmokeTest` → `7/7 PASS` ×3 con `R`; `T` → marker `POS_VentButton` sulla parete Sud.
- Test Simulator: prendere salsicciotti/panni dal tavolino (Grip), attivare il pannello al muro (Trigger), perimetro attorno alla pozzanghera.
- Richieste all'utente: in attesa dei 3 DPI; se il pulsante va dall'altro lato del tavolino, dirmelo (spostamento immediato).

### AGGIUSTAMENTO T17c — Riduzione salsicciotti sul tavolino — 2026-11-20
- Fatto: lasciati 2 salsicciotti sul piano del tavolino; spostati 4 sul ripiano inferiore del banco, mantenendo tutti e 6 attivi per gameplay/reset.
- File: `Level/Scenes/SCN_ChemicalAlert.unity`, `Docs/Agent/HANDOFF.md`.
- Comando di test: `7` → `RunSmokeTest` → `7/7 PASS`, `Outcome=Success`.
- Test Simulator: verificare 2 salsicciotti sul piano e 4 accessibili dal ripiano inferiore.
