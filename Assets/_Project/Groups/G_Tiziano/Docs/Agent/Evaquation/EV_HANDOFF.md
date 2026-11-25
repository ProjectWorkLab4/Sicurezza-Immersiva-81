# HANDOFF — Evacuation

## Stato corrente (riscrivere a ogni task, max 15 righe)
- Ultima task completata: E13 (2026-11-25) — controllo prestazioni Quest: report + static batching e tetto particelle; scenario completato.
- Report: `Docs/EVAC_PERF_REPORT.md` (numeri prima/dopo, correzioni, verifica). Documenti E12 in `Docs/` (`EVAC_TECHNICAL_DOCS.md`, `EVAC_FLOWCHART.md`, `EVAC_DEBRIEF_TABLE.md`, `EVAC_TEST_COMMANDS_MANUAL.md`).
- Chemical Alert: congelato e completo. NON modificarlo. Riusati senza modifiche `ActionLog/ScenarioResult/Outcome/Dimension/IResettable`, `ScenarioInteractable`, `HintSO`.
- Decisioni confermate: scale giuste = emergenza; sinistra = interne; 5 NPC (3+2); Crisis 1 cuore, 3° contatto = morte; fuoco 5/10; ascensore letale; corsia ≥1.2 m; seed unico nel Director.
- REUSE_MAP.md: RIUSA core CA; COPIA pattern `PlayerHealth`/`ContactSensor`/`Disturbance` → sistemi `Evac*`; VFX fuoco placeholder in `GROUP/`.
- Path docs: `Docs/Agent/Evaquation/` (con la q). ⚠ `Debug` è un namespace: `using UDebug = UnityEngine.Debug;`.
- Tipi (`Code/Scripts/Evacuation/`): Core, Data, Interaction, Systems, Integration, Debug; Director in `Systems/EvacuationDirector.cs`.
- Director: 18 eventi C# + stato pubblico per la UI futura; elenco in `Docs/EVAC_TECHNICAL_DOCS.md`.
- `evac.replay.start` = entry meta a inizio run (NON un LG di debrief, non punteggiata), presente nel `ResultReady` via ActionLog; seed con contatore anti-collisioni per reset ravvicinati.
- E13: 111 MeshRenderer di `MODEL_Office` con `BatchingStatic` → 5 batch, 166 draw call, batches 376→229; `FireSlot` fireRate 26→18, smokeRate 10→8, smokeCap 60→40 → tetto 600→230 in Crisis.
- Draw call Editor: 377→320 (Guided), 385→329 (Crisis); 1 Directional Light, 0 trasparenze, 0 luci extra; 0 errori console.
- Scena `SCN_Evacuation`: `MODEL_Office` + MNG_* invariati; `Environment` con 55 BoxCollider e 0 renderer; prefab in `Level/Prefabs/Evacuation/`.
- Modello: `MESH_UFFICIO_COMPLETO.fbx` (111 mesh) in `Art/Models/`; pos (7.95,0,10.492), scala (1,1,-0.771); NPC restano i prefab `EvacNpc`.
- Tasti debug: H N M F R X P K T O Y F8 F9 L I G C J E Q V U Z F7 B F10 F3 F4 F5 F6 F11 F12 + menu `Evac/…`; elenco in `Docs/EVAC_TEST_COMMANDS_MANUAL.md`.
- `RunSmokeTest` 49/49 PASS, F5 200/200, F11 5/5, F12 10/10. Nessun git, nessun file fuori `G_Tiziano/`, nessuna UI/animazione.

## Log (solo append, max 8 righe per task)
### Exx — titolo — data
- Fatto:
- File creati/modificati (percorsi relativi a GROUP/):
- Decisioni/deviazioni dal brief:
- Comandi di test (tasto/menu → risultato atteso):
- Test col Meta XR Simulator (se serve):
- Richieste all'utente:
- Problemi aperti:

### E00 — Ricognizione del riuso — 2026-11-25
- Fatto: letti BRIEF, CONVENTIONS, HANDOFF, sezione E00, ENV_REPORT e firme pubbliche CA; ispezionato in sola lettura il rig di `SCN_ChemicalAlert` (scena già aperta, non salvata/modificata); scritto REUSE_MAP.
- File creati/modificati (percorsi relativi a GROUP/): `Docs/Agent/Evaquation/REUSE_MAP.md` (nuovo, 40 righe); `Docs/Agent/Evaquation/EV_HANDOFF.md` (aggiornato).
- Decisioni/deviazioni dal brief: REUSE_MAP scritto in `Docs/Agent/Evaquation/` (cartella reale, con la q) invece di `Docs/Agent_Evacuation/`. Enum Difficulty/Phase/DangerLevel di CA sono annidati nel Director CA → copiarli nel Director Evac, non referenziarli.
- Comandi di test (tasto/menu → risultato atteso): nessuno, E00 è solo lettura (`EvacDebugCommands` nasce in E03). Verifica: aprire `REUSE_MAP.md` (≤50 righe) e controllare che la console non abbia errori nuovi.
- Test col Meta XR Simulator (se serve): non applicabile.
- Richieste all'utente: (1) rinominare la cartella `Evaquation`→`Evacuation`? (opzionale, rompe i path nei doc); (2) scegliere la locomozione per E01 (CA non ne ha: serve per percorrere il piano; da decidere se ne creo una provvisoria nel Simulator).
- Problemi aperti: emissione highlight da verificare sui materiali graybox (keyword `_EMISSION`); corpo giocatore per zone/fuoco da definire in E01; il log di `ScenarioInteractable` è fisso `[CA]` (sealed).

### E01 — Scena, rig, graybox e marker — 2026-11-25
- Fatto: AssetDatabase.CopyAsset di SCN_ChemicalAlert → SCN_Evacuation; rimossi tutti i root CA e, dal rig, DebugCommands/PlayerPPEState/ChemicalAlertUI/ToxicVignetteCanvas; graybox 1:1 (137 oggetti, 53 collider, 0 sovrapposizioni AABB); 49 marker; rig su SPAWN_Player. Play Mode: 0 errori/NullReference (solo log OpenXR/MetaXR/MCP); CA scena+meta SHA256 invariati.
- File creati (rel. a GROUP/): `Level/Scenes/SCN_Evacuation.unity`; `Art/Materials/Evacuation/MAT_EV_*.mat` (7); cartelle vuote `Code/Scripts/Evacuation/{Core,Interaction,Systems,Data,Data/Assets,Debug}`, `Level/Prefabs/Evacuation/`; aggiornato `Docs/Agent/Evaquation/EV_HANDOFF.md`. Nessuno script creato.
- Decisioni/deviazioni: porta ufficio 2 a x=11.0 e POS_Plan_R2 a x=10.2 (uffici "identici" = traslazione +6 m; il BRIEF dice porta≈10 e piano≈10.1, incoerenti tra loro); ROUTE solo varianti R1/R2 (9 punti, corsia libera lato ovest scrivanie); scrivanie 2×2 per stanza (riga nord con sedie a sud, riga sud con sedie a nord) lasciando libero il passaggio P0→porta; NPCSLOT_*_05/06 in piedi (7.4,11.7) e (6.4,9.2) (+6 in R2); ascensore = blocco pieno; scale = 4 gradini decorativi; ZONE_* solo marker.
- Comandi di test: nessun comando di debug (EvacDebugCommands nasce in E03). Verifica: aprire SCN_Evacuation → Play → console senza errori/NullReference; Hierarchy: `ScenarioMarkers` ha 49 figli, `OVRCameraRig` ha solo OVRCameraRig/OVRManager/OVRHeadsetEmulator/ScenarioInputBindings.
- Test col Meta XR Simulator: Play, camminare con l'headset simulato da SPAWN_Player (angolo NO ufficio 1) → porta ufficio 1 → corridoio → porta scale emergenza (x≈7.5, z=5); controllare ufficio 2, ascensore a destra, scale interne a sinistra; i muri non bloccano il rig (nessun collider sul rig).
- Richieste all'utente: (1) locomozione ancora non decisa: il Simulator basta per E01–E07? (2) confermare porta ufficio 2 a x=11 (vs ≈10 del BRIEF); (3) rinomina cartella `Evaquation`→`Evacuation` opzionale.
- Problemi aperti: scena CA ricaricata/aperta solo in lettura, mai salvata; emissione highlight non ancora verificata a runtime; nessun controllo visivo (screenshot) del graybox, solo verifica numerica (AABB, marker fuori dai collider, 0 missing scripts).

### E02 — Dati e valutatore (logica pura) — 2026-11-25
- Fatto: `EvacuationConfig` SO (+`OnValidate`/`Validate` con avvisi `[EV]`) e 3 istanze; `EvacuationOutcomeEvaluator` (esito, 5 sotto-punteggi, log LG01–LG11) con `SelfCheck` di 15 casi; compilato senza errori; SelfCheck 15/15 PASS + 3 config PASS. Nessuna scena/prefab modificati, nessun testo italiano nel codice.
- File creati (rel. a GROUP/): `Code/Scripts/Evacuation/Core/{EvacuationEnums,EvacActionIds,EvacuationSnapshot,EvacuationOutcomeEvaluator}.cs`; `Code/Scripts/Evacuation/Data/EvacuationConfig.cs`; `Code/Scripts/Evacuation/Debug/EvacSelfCheckRunner.cs`; `Code/Scripts/Evacuation/Data/Assets/SO_EvacuationConfig_{Guided,Operational,Crisis}.asset`; aggiornato `Docs/Agent/Evaquation/EV_HANDOFF.md`.
- Decisioni/deviazioni: numeri di punteggio (penalità, 60%, 40, 90/240 s, −20, finestra 5 s) in `EvacScoringSettings` dentro il SO, non hardcoded; severità Rischioso/Errore critico in `EvacActionIds.GetSeverity` (ActionLog ha solo `isCorrect`); LG02 solo se raggiunta un'uscita senza piano; ascensore Guided = Partial + LG06 + Safety 0; morte = `!Alive` o contatti ≥ letali; Time score Guided per timeout ignorato; Guided punti GDD 0/0/0; `disturbancesEnabled` solo Crisis [PROPOSTA].
- Comandi di test: nessun tasto (EvacDebugCommands nasce in E03). Aggiungere `EvacSelfCheckRunner` a un GameObject qualsiasi (assegnare i 3 SO nell'array `configs`, opzionale) → ContextMenu `Evac/Run Evaluator SelfCheck` → console: 15 righe `[EV] E02 SelfCheck PASS …`, `evaluator: 15/15 PASS`, 3 righe PASS config, `TOTAL: ALL PASS`.
- Test col Meta XR Simulator: non applicabile (nessuna interazione).
- Richieste all'utente: (1) confermare [PROPOSTA] 'nessuna uscita e nessun fallimento = Failure' e `disturbancesEnabled` solo in Crisis; (2) il runner va rimosso o assorbito da `EvacDebugCommands` in E03 (non l'ho messo in scena).
- Problemi aperti: HintSO `evac.hint.*` da creare in E08; E01 ancora senza controllo visivo (screenshot).

### E03 — EvacuationDirector e comandi di debug — 2026-11-25
- Fatto: Director (fasi, timer/cronometro, cuori, seed, registro IResettable, Report…, eventi, recap + `ResultReady` via evaluator) ed `EvacDebugCommands`; prefab `MNG_EvacuationDirector` in scena; corretto step 7 dello smoke test (mancavano i 2 NPC della stanza 2). Play Mode: SmokeTest 14/14 PASS, 0 eccezioni.
- File creati (rel. a GROUP/): `Code/Scripts/Evacuation/Core/EvacPhaseEnums.cs`, `Systems/EvacuationDirector.cs`, `Debug/EvacDebugCommands.cs`; `Level/Prefabs/Evacuation/MNG_EvacuationDirector.prefab`; modificati `Level/Scenes/SCN_Evacuation.unity` (+Director, rimosso GameObject residuo con `EvacSelfCheckRunner`) e questo HANDOFF.
- Decisioni: Guided ignora ForceTimeUp; ascensore Guided = danger Down + Partial; contatti fuoco ignorati se Hearts=0 (Guided); `ReportRoomExited` rileva l'uscita impulsiva; Director senza `IResettable` proprio nel registro; `EvacSelfCheckRunner` assorbito da tasto O.
- Comandi di test (Play, Game View con focus): H lista · Y smoke test (atteso `summary 14/14 ALL PASS`) · N Briefing→Action→fine run · M cambia livello · F timeout · R/X reset (x3) · P stato · K recap · T teletrasporto ciclico · F8/F9 seed · O SelfCheck.
- Test col Simulator: non applicabile (nessuna interazione).
- Richieste all'utente: (1) confermare autostart solo Editor (2 s); (2) cancellare `EvacSelfCheckRunner.cs` se d'accordo; (3) confermare che i contatti fuoco in Guided/Operational non danno danno.
- Problemi aperti: Director non ancora collegato a porte/NPC/piani (E04+); nessun sistema registrato come IResettable (`resettables 0`).

### E04 — Allarme, piani e porte — 2026-11-25
- Fatto: ripreso lavoro interrotto, completato e verificato: `AlarmSystem` parte/stoppa con Action, 3 piani (trigger o dwell 0.75 s ≤1.2 m), 4 porte con collider/UnityEvent e reset, cartello uscita a emissione fissa; Play Mode SmokeTest 20/20 PASS, 0 errori, scena salvata.
- File (rel. a GROUP/): `Code/Scripts/Evacuation/{Systems/AlarmSystem.cs,Interaction/EvacuationPlanInspectable.cs,Interaction/DoorInteractable.cs}`; modificati `Systems/EvacuationDirector.cs`, `Data/EvacuationConfig.cs`, `Debug/EvacDebugCommands.cs`, `Art/Materials/Evacuation/MAT_EV_Alarm.mat`, `Level/Scenes/SCN_Evacuation.unity`; prefab `Level/Prefabs/Evacuation/Evac{Alarm,Plan,ExitSign,Door_Room,Door_StairsEmergency,Door_StairsInternal}.prefab`.
- Decisioni: emissione via MaterialPropertyBlock (1.5 Hz) + keyword `_EMISSION` aggiunta al graybox; porte stanza `startsOpen=true`, scale chiuse; trigger d'interazione su figlio pivot, blocker disattivato da aperta; LG01 solo prima ispezione, eventi per istanza.
- Comandi di test: H elenco · Y smoke test (`summary 20/20 ALL PASS`) · L allarme on/off · I ispeziona piano successivo (LG01 una volta) · G/C apri/chiudi tutte · J teletrasporto davanti a piano/porta · ContextMenu `Evac/…` equivalenti.
- Test col Simulator: Play → J davanti a un piano → hover mano + trigger: log `[EV] Plan inspected` e LG01; J davanti a una porta → hover + trigger: log `[EV] Door … opened/closed`, blocco rimosso da aperta; R ripristina tutto.
- Richieste all'utente: (1) assegnare `AudioClip` allarme in `AlarmSystem` (vuoto = silenzio); (2) confermare testo dei cartelli piani (solo chiave `evac.plan.route`).
- Problemi aperti: nessuno per E04; E05 collegherà ascensore/scale; non ancora test visivo in headset reale.

### E05 — Ascensore, scale e uscita — 2026-11-25
- Fatto: `ElevatorButton` su `Elevator_Buttons` → `ReportElevatorUsed`; 2 `StairsExitZone` trigger sui `ZONE_*` → `ReportExitReached`; LG02 senza piano; Play Mode: SmokeTest 23/23 PASS, 0 errori, comandi E/Q/V verificati, reset pulito.
- File (rel. a GROUP/): `Code/Scripts/Evacuation/Interaction/{ElevatorButton,StairsExitZone}.cs`; modificati `Debug/EvacDebugCommands.cs`, `Level/Scenes/SCN_Evacuation.unity`; nuovi prefab `Level/Prefabs/Evacuation/Evac{ElevatorButton,StairsExitZone}.prefab`; aggiornato HANDOFF.
- Decisioni/deviazioni: un solo interactable per i 2 pulsanti (stesso visual graybox); zone con BoxCollider trigger 2,2×2,4×2,2 m e bounds calcolati dal transform (physics sync inaffidabile dopo teletrasporto debug); rileva rig/head senza collider; Success "senza NPC" simula 5 `ReportNpcCollected` via Director (NPC veri in E06).
- Comandi di test (tasto/menu → risultato atteso): H elenco · Y `summary 23/23 ALL PASS` · E ascensore → Failure (Guided: danger Down, continua) · V scale interne → Partial · Q scale emergenza + piano + 5 NPC → Success · P stato · R/X reset puliti.
- Test col Simulator: Play → J/T davanti ai pulsanti → hover+trigger: LG06; entra in `ZONE_StairsInternal` → Partial (LG11); ispeziona un piano, raccogli i 5 NPC (E06), entra in `ZONE_StairsEmergency` → Success (LG09); R ripristina.
- Richieste all'utente: (1) confermare che un solo interactable copra entrambi i pulsanti; (2) colore/visual definitivo delle piastre zone col modello E11.
- Problemi aperti: NPC non ancora presenti (E06); fuoco/hint non implementati; nessun test in headset reale.

### E06 — NPC colleghi (raccolta e fila indiana) — 2026-11-25
- Fatto: `EvacNpc` (Idle/Following/Evacuated, area trigger ~1 m, ScenarioInteractable, UnityEvent) + `NpcRegistry` (spawn 3+2 su `NPCSLOT_*` seedati) + `NpcTrailFollower` (briciole ~0.25 m da testa, gap 0.9 m, ≤1.6 m/s, Y fissa, no NavMesh). Evacuazione su exit emergency; LG03/LG04/LG05/LG09 via Director.
- File (rel. a GROUP/): nuovi `Code/Scripts/Evacuation/Interaction/EvacNpc.cs`, `Systems/{NpcRegistry,NpcTrailFollower}.cs`; modificati `Systems/EvacuationDirector.cs` (+`ExitReported`), `Debug/EvacDebugCommands.cs` (U/Z/F7, smoke 27 step); scena `Level/Scenes/SCN_Evacuation.unity`; prefab `Level/Prefabs/Evacuation/EvacNpc.prefab`.
- Decisioni/deviazioni: NPC spawnati a runtime dal prefab (no 5 istanze salvate in scena) per reset/seed puliti; trail resetta a teletrasporto >2.5 m; `SimulateExit` fa già teletrasporto+reach; verifica “Success senza NPC” ora usa NPC reali nei test E06.
- Comandi di test (tasto/menu → risultato atteso): H elenco · Y `summary 27/27 ALL PASS` · U raccoglie prossimo NPC (dialogo follow/already) · Z raccoglie tutti · F7 stampa trail/fila · V con 3/5 → Partial · Q con piano+5/5 → Success, NPC Evacuated · R/X reset puliti (Idle, no briciole).
- Test col Simulator: Play → J/T vicino a NPC → hover+trigger: entra in fila indiana; cammina: segue il percorso, si ferma col player; raccogli 5/5 + piano + Q → Success; R ripristina.
- Richieste all'utente: nessuna.
- Problemi aperti: fuoco/hint/Crisis non implementati (E08/E09); nessun test visivo in headset reale.

### E07 — Vertical slice Operational end-to-end — 2026-11-25
- Fatto: esteso `EvacDebugCommands.RunSmokeTest` a 32 step con 5 casi E07 in Operational: Success, Partial NPC mancanti, Partial scale interne, Failure ascensore, Failure timeout con soglia ridotta. Ogni caso verifica `ResultReady` una sola volta, recap/log coerenti (`ActionLog` non vuoto, reason/exit/outcome attesi, allarme spento); timeout verifica anche `TimeUp` una sola volta e `LG10`. Smoke 32/32 PASS ×3 consecutivi, 0 errori/warning console.
- File (rel. a GROUP/): `Code/Scripts/Evacuation/Debug/EvacDebugCommands.cs` (step E07 + helper `ExpectEnd/CollectNpcs`); `Code/Scripts/Evacuation/Systems/EvacuationDirector.cs` (debug override `SetDebugTimeLimit/ClearDebugTimeLimit`, `EffectiveTimeLimitSeconds` usato da timer/ForceTimeUp/snapshot/log); `Docs/Agent/Evaquation/EV_HANDOFF.md`.
- Decisioni/deviazioni: nessuna nuova feature di gameplay; override tempo solo debug, pulito a fine smoke/reset flow dei test; non modifica i SO. Nessun file fuori `G_Tiziano/`, nessun git.
- Comandi di test (tasto/menu → risultato atteso): Y / ContextMenu `Evac/Run Smoke Test (Y)` → `SmokeTest summary 32/32 ALL PASS`; H elenco; P/K stato/recap; R/X reset. Verifica E07: step 28–32 PASS (Success, Partial NPC mancanti, Partial scale interne, Failure ascensore, Failure timeout ridotto).
- Test col Simulator: Play in `SCN_Evacuation` (Operational). Success: I ispeziona piano, Z raccoglie tutti, Q scale emergenza → Result Success, `LG01/03/04/09`. Partial NPC: I, U×3, Q → Partial + `LG05`. Partial interne: I, Z, V → Partial + `LG11`. Failure ascensore: E → Failure + `LG06`. Failure timeout: F (o attendere 180 s) → Failure + `LG10`.
- Richieste all'utente: verifica manuale nel Simulator del vertical slice Operational (milestone E07).
- Problemi aperti: hint/Guided UX completa e incendio Crisis restano per E08/E09; nessun test in headset reale.

### E08 — Guided/Operational/Crisis, hint e highlight — 2026-11-25
- Fatto: Guided ha cronometro senza sconfitta, highlight #E69F00 pulsante (0.8 Hz, MPB) su piani/NPC Idle/porta+cartello scale, 5 hint ordinati automatici dopo `hintIdleSeconds` e su richiesta, danger Up (progresso) / Mid (fermo) / Down (errore). Operational 180 s e Crisis 120 s senza aiuti; `ReducedEffects` impostato in Crisis per E09.
- File (rel. a GROUP/): nuovi `Systems/EvacGuidedHighlights.cs`, `Interaction/EvacHintInput.cs`, 5 asset `Data/Assets/SO_EvacHint_0*.asset`, `Art/Materials/Evacuation/MAT_EV_NpcGuided.mat`; modificati `Data/EvacuationConfig.cs`, `Systems/EvacuationDirector.cs`, `Debug/EvacDebugCommands.cs` (39 step), `Level/Scenes/SCN_Evacuation.unity`, prefab `EvacNpc.prefab`, materiali Evacuation, HANDOFF.
- Decisioni/deviazioni: hint riusano `HintSO` CA senza modificarlo; controller A/X via `ScenarioInputBindings.GetHintDown`; cartello evidenziato con MPB dedicato perché non ha `ScenarioInteractable`; tasto B/F10 evitano 0–9/WASD del Meta XR Simulator. Nessun git, nessuna scrittura fuori `G_Tiziano/`.
- Comandi di test (tasto/menu → risultato atteso): Y / `Evac/Run Smoke Test (Y)` → `SmokeTest summary 39/39 ALL PASS` (×3); M o menu `Evac/Set Difficulty *` → 3 livelli con tempi 0/180/120 s; B o `Evac/Request Hint (B)` → `evac.hint.*` ordinato; F10 → riga `Level … highlights/hints/reduced`; F/E → Guided resta in Action con danger Down.
- Test col Simulator: Play → Guided: highlight arancione su piani/NPC/porta+cartello, B o A-X mostra hint, idle 20 s → hint automatico e danger Mid, ascensore E → Down senza sconfitta, timeout F ignorato; passa a Operational/Crisis con M → nessun highlight e countdown attivo.
- Richieste all'utente: verifica visiva nel Simulator del pulse #E69F00 e del flag effetti ridotti in attesa di E09.
- Problemi aperti: E09 deve consumare `ReducedEffects` per fuoco/fumo; nessun test in headset reale.

### E09 — Crisis: incendio, fumo, contatti e corsia libera — 2026-11-25
- Fatto: 10 `FIRESLOT` con prefab `FireSlot` (trigger + VFX fuoco/fumo placeholder), `EvacFireManager` attiva 5 slot solo in Crisis col seed del Director e ripete l'estrazione finché la corsia è libera (max 50 tentativi); `EvacPlayerFireSensor` segue la testa e segnala i contatti con cooldown 2 s; 1°/2° contatto = LG07, 3° = LG08 + cuori 0 + Failure. `ReducedEffects` dimezza le emissioni.
- File (rel. a GROUP/): nuovi `Code/Scripts/Evacuation/Systems/{EvacFireSlot,EvacFireManager,EvacFireLaneValidator,EvacPlayerFireSensor}.cs`, `Level/Prefabs/Evacuation/FireSlot.prefab`; modificati `Debug/EvacDebugCommands.cs` (44 step, comandi F3–F6), `Level/Scenes/SCN_Evacuation.unity`, HANDOFF. Nessun file fuori `G_Tiziano/`, nessun git, nessuna UI/animazione.
- Decisioni/deviazioni: VFX particellari con tetto (≤60 particelle/sistema, nessuna luce realtime) invece di mesh definitive; il validatore ignora gli stipiti delle porte apribili (altrimenti la larghezza 1.2 m non passa dai vani da 0.9 m) e verifica muri + fuochi attivi sulle polilinee ROUTE; 200 seed validati senza scarti.
- Comandi di test (tasto/menu → risultato atteso): Y → `SmokeTest summary 44/44 ALL PASS` (×3); F3 / `Evac/Force Fire Contact` → contatto (1/2 LG07, 3 LG08 + Failure); F4 → stampa 5 slot attivi in Crisis, 0 negli altri livelli; F5 → `200/200 seed validi`; F6 → gizmo fuochi; M → cambia livello; R/X → reset puliti.
- Test col Simulator: Play → M (Crisis) → F4 verifica i 5 focolai → cammina dentro una zona fuoco, esci e rientra dopo 2 s per 3 contatti → Failure con LG07×2 + LG08; F5 stampa 0 scartati; R ripristina focolai e contatori.
- Richieste all'utente: verifica visiva nel Simulator del fuoco/fumo e del flag effetti ridotti; sostituire i VFX placeholder con quelli definitivi in E11.
- Problemi aperti: il modello finale (E11) dovrà allineare i FIRESLOT alla geometria reale; nessun test in headset reale.

### E10 — Replay, seed e robustezza del reset — 2026-11-25
- Fatto: seed fresco a ogni `ResetScenario()` (contatore anti-collisioni), fissabile con F8/F9; NPC e fuochi variano col seed; `ReplayStart` (seed + layout) nel log e in `ResultReady` via `ReplayStarted`; 5 partite con esiti differenti, 10 reset senza residui né doppie sottoscrizioni; `NpcTrailFollower.Head` risolto lazy.
- File creati/modificati (rel. a GROUP/): `Code/Scripts/Evacuation/Core/EvacActionIds.cs`, `Systems/{EvacuationDirector,NpcRegistry,EvacFireManager,NpcTrailFollower}.cs`, `Debug/EvacDebugCommands.cs`; `Docs/Agent/Evaquation/EV_HANDOFF.md`. Nessuna scena/prefab toccato.
- Decisioni/deviazioni: `evac.replay.start` è entry meta (non LG01–LG11, non punteggiata) loggata a inizio run così finisce nel `ScenarioResult`; il seed non usa più `new Random()` senza seme (reset ravvicinati potevano ripetersi). Fix `Head` fuori dal replay stretto ma necessario per "riferimenti nulli dopo reset".
- Comandi di test (tasto/menu → risultato atteso): Y → `SmokeTest summary 49/49 ALL PASS` (×3); F11 → `E10 replay games: 5/5 PASS`; F12 → `E10 reset stress: 10/10 PASS`; menu `Evac/Print Replay Layout` → seed + slot NPC/fuoco; F8/F9 → seed fisso/nuovo; R/X → reset puliti.
- Test col Meta XR Simulator: Play → M (Crisis) → F11 verifica 5 finali diversi senza NPC/briciole/fuochi residui; F12 verifica 10 reset; Print Replay Layout; F8 con seed 4242 e 2 reset → stessa disposizione, F9 → diversa.
- Richieste all'utente: verifica visiva in Simulator che NPC/fuochi cambino tra partite e che i VFX si spengano al reset.
- Problemi aperti: E11 dovrà riallineare i marker al modello finale; nessun test in headset reale.

### E11 — Modello 3D reale al posto del graybox — 2026-11-25
- Fatto: duplicato `MESH_UFFICIO_COMPLETO.fbx` (Shared → `Art/Models/`, 115 MB); istanza `MODEL_Office` a (7.95, 0, 10.492) con scala Z -0.771 (uffici a nord, corridoio a sud, ascensore a est); guscio/soffitto del modello disattivato; graybox `Environment` attivo con soli collider (55 Box, 0 renderer) per il validatore; Visual di Door_R1/R2/StairsEmergency sostituiti con prop del modello. Scena salvata.
- Marker spostati (nomi invariati): FIRESLOT_06 (5.9,11.7)→(3.0,9.4) e FIRESLOT_08 (11.9,11.7)→(13.0,9.4) erano dentro scrivanie; FIRESLOT_07 (7.4,8.6)→(7.0,9.6) e FIRESLOT_09 (13.4,8.6)→(9.0,9.6) erano dentro il muro uffici/corridoio. Altri 45 marker verificati e lasciati.
- File (rel. a GROUP/): nuovi `Art/Models/MESH_UFFICIO_COMPLETO.fbx(.meta)`; modificati `Level/Scenes/SCN_Evacuation.unity`, `Docs/Agent/Evaquation/EV_HANDOFF.md`. Nessun git, nessun file fuori `G_Tiziano/`, nessuna UI/animazione, Chemical Alert intoccato.
- Decisioni/deviazioni: il FBX ha Z invertito rispetto al BRIEF → specchiato in Z (stanze non rimappate); NPC non inclusi nel modello (li carica l'utente) → restano i prefab `EvacNpc`; piani e cartello restano i marker interattivi esistenti (nessun prop equivalente identificabile nel FBX). Le aperture porte del modello coincidono coi collider graybox.
- Comandi di test (tasto/menu → risultato atteso): Y / `Evac/Run Smoke Test` → `SmokeTest summary 49/49 ALL PASS` (×3 consecutivi); F5 / `Evac/Validate Fire Lanes` → `Fire lane: 200/200 seed validi, 0 scartati`; F4 → 5 focolai in Crisis sui nuovi slot; M → ciclo difficoltà; R/X → reset puliti. 0 errori console.
- Test col Simulator: Play → verifica scala 1:1 e passaggio dalle porte R1/R2 e dalla porta scale emergenza senza compenetrazioni; F5 stampa 0 scartati; M (Crisis) → F4 controlla i focolai lontani dai muri; R ripristina.
- Richieste all'utente: caricare i NPC nel modello definitivo; confermare visivamente in headset soffitto (guscio disattivato per leggibilità) e allineamento X/Z.
- Problemi aperti: il FBX non include scale interne né piani dedicati → restano marker/prefab esistenti; warning import su poligoni auto-intersecanti di `Cube.009` (non errori).

### E12 — Documentazione e tabella frasi del debrief — 2026-11-25
- Fatto: `Docs/EVAC_TECHNICAL_DOCS.md` (script/prefab/SO/asset, dipendenze CA, 18 eventi + stato del Director, chiavi), `EVAC_FLOWCHART.md` (Mermaid: DP1–DP4, conseguenze, esiti, 3 livelli), `EVAC_DEBRIEF_TABLE.md` (LG01–LG11 con `{t}`, "cosa potevi fare", dimensione, chiavi; LG11 scritta come le altre), `EVAC_TEST_COMMANDS_MANUAL.md` (32 tasti + menu, Simulator, procedure). Nomi/eventi/chiavi/tasti verificati sul codice.
- File (rel. a GROUP/): nuovi `Docs/EVAC_*.md` (+ `.meta` via refresh Unity); modificato `Docs/Agent/Evaquation/EV_HANDOFF.md`. Nessun git, nessun file fuori `G_Tiziano/`, nessuna UI/animazione, Chemical Alert intoccato.
- Decisioni/deviazioni: frase LG11 "Al {t} hai preso le scale interne invece di quelle di emergenza. Conseguenza: vittoria parziale. Cosa potevi fare: seguire il cartello illuminato verso le scale di emergenza."; `evac.replay.start` escluso dalla tabella debrief (entry meta non punteggiata); valori SO copiati dagli asset reali.
- Comandi di test (tasto/menu → risultato atteso): Y → `SmokeTest summary 49/49 ALL PASS`; O → `E02 SelfCheck evaluator: N/N PASS` + `SelfCheck TOTAL: ALL PASS`; F5 → `Fire lane: 200/200 seed validi, 0 scartati`; F11 → `E10 replay games: 5/5 PASS`; F12 → `E10 reset stress: 10/10 PASS`; H → elenco comandi; P/K/F10 → stato/risultato/comportamento.
- Test col Simulator: task solo documentale; procedure di verifica giocatore (porte, piante, NPC, ascensore, fuoco, reset) documentate nel manuale.
- Richieste all'utente: rivedere i 4 documenti, confermare la frase LG11 ed eseguire la verifica Simulator dal manuale.
- Problemi aperti: E13 (controllo prestazioni Quest) resta opzionale; nessun test in headset reale.

### E13 — Controllo prestazioni (Quest) — 2026-11-25
- Fatto: inventario (415 GO, 207 mesh/112 uniche/574.890 tri, 80 materiali opachi, 0 trasparenze, 1 Directional Light, 20 PS) e misure UnityStats prima/dopo in `Docs/EVAC_PERF_REPORT.md`. Draw call 377→320 (Guided), 385→329 (Crisis); batches 376→229; setPass 40→43/44.
- Correzioni: `BatchingStatic` su 111 MeshRenderer di `MODEL_Office` (5 static batch, 166 draw call); `FireSlot.prefab` + 10 istanze con fireRate 26→18, smokeRate 10→8, smokeCap 60→40 → tetto particelle 600→230 in Crisis (RiduciEffetti). Scene salvata, `isDirty=false`.
- File (rel. a GROUP/): nuovi `Docs/EVAC_PERF_REPORT.md` (+ `.meta`); modificati `Level/Scenes/SCN_Evacuation.unity`, `Level/Prefabs/Evacuation/FireSlot.prefab`, `Docs/Agent/Evaquation/EV_HANDOFF.md`. Nessun git, nessun file fuori `G_Tiziano/`, nessuna UI/animazione, Chemical Alert intoccato.
- Decisioni/deviazioni: esclusi dal batching porte/NPC/piani/allarme/cartello (dinamici o con `MaterialPropertyBlock` Guided); nessun tocco a ProjectSettings/luci/shader/package. Misure in Editor Windows, da confermare in headset.
- Comandi di test (tasto/menu → risultato atteso): Y → `SmokeTest summary 49/49 ALL PASS`; O → `SelfCheck TOTAL: ALL PASS`; F5 → `Fire lane: 200/200 seed validi, 0 scartati`; F11 → `E10 replay games: 5/5 PASS`; F12 → `E10 reset stress: 10/10 PASS`; F4 → 5 focolai con cap ridotto; F10 → comportamento livelli. 0 errori console.
- Test col Simulator: M (Crisis) → F4/F3 verifica fuoco leggibile coi VFX ridotti; R/X/F12 reset puliti e VFX spenti.
- Richieste all'utente: profilare su Quest reale (draw call/frame time) e confermare che il tetto particelle ridotto non penalizzi la leggibilità del fuoco.
- Problemi aperti: nessuno in scope; la valutazione Quest resta fuori dal gruppo.
