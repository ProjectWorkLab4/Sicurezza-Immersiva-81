# HANDOFF — Evacuation

## Stato corrente (riscrivere a ogni task, max 15 righe)
- Ultima task: E22 (2026-11-27) — SCN_Evacuation ricostruita con UFFICIO_STACCATO + 8 TAVOLO (passi 1–10 chiusi; restano test a mano nel Simulator, clip allarme, input hint).
- Prima: E20 (2026-11-26) — mappa: corridoio e pareti graybox resi visibili (il FBX non li contiene).
- E19: interazioni uffici sul muro del modello. E18: hit box NPC. E17: Home. E16: NPC non spingono. E15: missione 09.
- Causa: collider di interazione tutti trigger; il `RayInteractor` condiviso raycasta con `QueryTriggerInteraction.Ignore` → piani/NPC/pulsante non colpibili. Il bridge duplicava `Select → Activated` (doppia apertura porte).
- Fix: collider solidi di raycast su piani/porte/pulsante; `RaycastSurface` (sfera 0.55 @ y1.05) aggiunta a `EvacNpc.prefab`; area trigger NPC conservata.
- `EvacInteractableBridge`: solo `GrabEnter → RaiseActivated()`, `followsHand=false`; Hover/Select gestiti dal solo `ScenarioInteractable` → 1 evento per azione.
- Prefab aggiornati: EvacPlan, EvacElevatorButton, EvacDoor_Room/StairsEmergency/StairsInternal, EvacNpc; scena `SCN_Evacuation` salvata (`isDirty=false`).
- Script: `Integration/EvacInteractableBridge.cs` riscritto; `Debug/EvacDebugCommands.cs` + step smoke "hand interaction surfaces reachable by ray".
- Verifiche: raycast `Ignore` colpisce i 3 piani; `GrabEnter` → NPC Idle→Following, porta 1 toggle, piano ispezionato; `SelectEnter` porta 1 toggle. Y 50/50, F12 10/10, 0 errori console.
- Report/documenti E12–E13 in `Docs/` (`EVAC_TECHNICAL_DOCS.md`, `EVAC_FLOWCHART.md`, `EVAC_DEBRIEF_TABLE.md`, `EVAC_TEST_COMMANDS_MANUAL.md`, `EVAC_PERF_REPORT.md`).
- Chemical Alert: congelato e intoccato. Riuso senza modifiche CA core/`ScenarioInteractable`/`HintSO`; solo lettura di Home per il rig condiviso.
- Decisioni confermate: scale giuste = emergenza; sinistra = interne; 5 NPC (3+2); Crisis 1 cuore, 3° contatto = morte; fuoco 5/10; ascensore letale; corsia ≥1.2 m; seed unico nel Director.
- REUSE_MAP.md: RIUSA core CA; COPIA pattern `PlayerHealth`/`ContactSensor`/`Disturbance` → sistemi `Evac*`; VFX fuoco placeholder in `GROUP/`.
- Path docs: `Docs/Agent/Evaquation/` (con la q); `Debug` è un namespace: `using UDebug = UnityEngine.Debug;`. Scena senza rig locale (input da `SCN_Core`), tutto ancorato ai marker.
- Tasti debug: H N M F R X P K T O Y F8 F9 L I G C J E Q V U Z F7 B F10 F3 F4 F5 F6 F11 F12 + menu `Evac/…`.
- Nessun git, nessuna scrittura fuori `G_Tiziano/`, nessuna UI/animazione, Chemical Alert intoccato.

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

### E14 — Fix grab/interazione mano (rig condiviso) — 2026-11-26
- Fatto: diagnosticato con raycast in scena — `RayInteractor` con `QueryTriggerInteraction.Ignore` colpiva il muro, non il piano (`isTrigger=true`). Aggiunti collider solidi di raycast su 3 piani, 4 porte, pulsante ascensore e figlio `RaycastSurface` su `EvacNpc.prefab` (area trigger ~1 m conservata, verificata dallo smoke test).
- Bridge: rimossi Hover/Select duplicati (già inoltrati da `ScenarioInteractable`); `GrabEnter` → `RaiseActivated()`, `Grabbable` dal prefab, `FollowsHand=false` → la presa attiva senza trascinare oggetti. `grabbable=1` e `followsHand=0` serializzati in tutti i prefab/scena.
- File (rel. a GROUP/): modificati `Code/Scripts/Evacuation/Integration/EvacInteractableBridge.cs`, `Code/Scripts/Evacuation/Debug/EvacDebugCommands.cs` (nuovo step 3), 6 prefab in `Level/Prefabs/Evacuation/`, `Level/Scenes/SCN_Evacuation.unity`, `Docs/Agent/Evaquation/EV_HANDOFF.md`. Nessun git, nessun file fuori `G_Tiziano/`, nessuna UI/animazione, Chemical Alert intoccato.
- Decisioni/deviazioni: [PROPOSTA] grip = attivazione invece di trasporto, coerente con "nessuna animazione"; non modificati `Home/RayInteractor` (fuori gruppo) né CA. Se in futuro il raggio includerà i trigger, rimuovere i collider di raycast aggiunti.
- Comandi di test: Y → `SmokeTest summary 50/50 ALL PASS` (nuovo step "hand interaction surfaces reachable by ray"); F12 → `E10 reset stress: 10/10 PASS`; O → `SelfCheck TOTAL: ALL PASS`; J → "Test spot -> <nome>" davanti a piano/porta/NPC; R → reset pulito.
- Test col Simulator: J fino al piano → grip o trigger → `[EV] Grab -> Activate` / `[CA] Activated` + `[EV] Plan inspected`; J su un NPC → `[EV] NPC following`; grip su porta → `[EV] Door ... opened/closed` una sola volta; pulsante ascensore → LG06.
- Richieste all'utente: verificare in Simulator (e poi Quest) che grip e trigger funzionino su piano/NPC/porte/pulsante; confermare che la presa non debba far seguire l'oggetto alla mano.
- Problemi aperti: nessuno in scope; il rig resta in `SCN_Core` (fuori gruppo).

### E15 — Home/UI: missione Evacuation pronta — 2026-11-26
- Fatto: compilato `Home/UI/Data/Missions/SO_Mission_Evacuation.asset` (ID 09, contesto reale 216 char, 4 obiettivi ≤30, 3 criteri, complessità 3, DesignerNote senza segnaposto). Override tempo HUD: Guided 0, Operational 180, Crisis 120, allineati al Director.
- Build Settings: `SCN_Evacuation` aggiunta dopo `SCN_Core` (indice 1); catalogo e `MNG_Scenario` (missione + `MNG_SpawnPoint`) già collegati, non toccati.
- Verifiche: `ScenarioValidator.Validate(...)` → 0 issue; Play da `SCN_Core` → `CurrentScreen=Home`, card `LBL_Title=09 · Evacuation`, 9 missioni a catalogo, 0 errori console; editor riportato su `SCN_Core`.
- File: fuori `G_Tiziano/` solo per autorizzazione esplicita: `SO_Mission_Evacuation.asset`, `ProjectSettings/EditorBuildSettings.asset`; aggiornato `EV_HANDOFF.md`. Nessun git, nessuna UI creata, Chemical Alert intoccato.
- Decisioni/deviazioni: mantenute 8 missioni di esempio e mock (99); ID 09 scelto dall'utente; nessun menu `SI81/UI/Build…` eseguito.
- Comandi di test: `SI81/Scenari/Valida tutti gli scenari` → 0 errori per Evacuation; Play da `SCN_Core` → Home → card `09 · Evacuation` → Mission Brief → Avvia → `SCN_Evacuation`.
- Richieste all'utente: provare il flusso completo Home → Brief → missione → Debrief → Home nei 3 livelli (Simulator/Quest); decidere se togliere la missione mock (99) alla consegna.
- Problemi aperti: nessuno in scope.

### E16 — NPC che seguono senza spingere — 2026-11-26
- Fatto: `EvacNpc.ApplyPlayerCollisionIgnore()` ignora le collisioni tra i collider solidi dell'NPC (la sfera `RaycastSurface` usata dal raggio della mano) e il `CharacterController` del rig condiviso; applicato in `OnEnable`, `Configure` e all'evento `RigRegistration.Registered`, con unsubscribe in `OnDisable`.
- Causa: il collider solido aggiunto in E14 per rendere l'NPC puntabile dal `RayInteractor` (che scarta i trigger) urtava il giocatore mentre la fila si avvicinava (lineSpacing 0.9 m). I trigger restano invariati e non spingono.
- File: modificato solo `Code/Scripts/Evacuation/Interaction/EvacNpc.cs`. Nessun git, nessuna UI/animazione, Chemical Alert intoccato; scena e prefab invariati.
- Verifiche: in Play con `SCN_Core` + `SCN_Evacuation`, `Physics.GetIgnoreCollision(surface, controller)` = true per 5/5 NPC; `SmokeTest summary 50/50 ALL PASS`; 5 NPC raccolti restano in `Following` con fila attiva.
- Comandi di test: U/Z → raccolta NPC; T → teletrasporto rig; F7 → stampa fila e distanze; Y → smoke test 50/50; Simulator: avvicinarsi a un NPC che segue → nessuna spinta, il raggio continua a puntarlo.
- Decisioni/deviazioni: nessun nuovo layer e nessuna modifica a ProjectSettings; la presa resta attivazione senza trascinamento (E14).
- Richieste all'utente: confermare in Simulator/Quest che la fila non spinga più e che l'NPC resti puntabile con grip/trigger.
- Problemi aperti: nessuno in scope.

### E17 — Home: Evacuation disponibile di nuovo — 2026-11-26
- Causa: la modifica alle Build Settings di E15 era rimasta solo in memoria: dopo il domain reload/Play Mode `EditorBuildSettings.scenes` era tornato alle 3 scene precedenti, quindi la Home filtrava la card. Missione, catalogo e `MNG_Scenario` erano intatti.
- Fatto: `EditorBuildSettings.scenes` riscritto con Core(0), Evacuation(1), Mock, ChemicalAlert e salvato con `File/Save Project`; verificato su `ProjectSettings/EditorBuildSettings.asset` anche dopo Play/Stop.
- Verifiche: Play da `SCN_Core` → `CurrentScreen=Home`, card `BTN_ScenarioCard7` con testo `09 · Evacuation`, `interactable=True`; click card → Mission Brief → "Avvia scenario" → `SCN_Evacuation` caricata in additivo, `MissionSession.IsActive=True`, Director in `Action`.
- File: solo `ProjectSettings/EditorBuildSettings.asset` (autorizzazione esplicita data in E15) e `EV_HANDOFF.md`. Nessun git, nessuna UI creata, Chemical Alert intoccato.
- Comandi di test: Play da `SCN_Core` → Home → card `09 · Evacuation` → Mission Brief → `Avvia scenario` → missione in Action; `SI81/Scenari/Valida tutti gli scenari` → 0 errori per Evacuation.
- Decisioni/deviazioni: mantenute mock (99) e Chemical Alert in Build Settings; nessun menu `SI81/UI/Build…` eseguito; la card non è stata creata a mano, arriva dal catalogo.
- Richieste all'utente: confermare che la card sia presente e avviabile anche dopo un riavvio dell'Editor (il file è ora salvato).
- Problemi aperti: nessuno in scope.

### E18 — Hit box delle interazioni mano — 2026-11-26
- Fatto: in `EvacNpc.prefab` la sfera solida `RaycastSurface` (r 0.55, centro y 1.05, più alta e più larga del modello) è stata sostituita da un `BoxCollider` non-trigger centro (0, 0.85, 0), size (0.42, 1.70, 0.42): copre l'NPC dal pavimento all'altezza della testa, larghezza pari al visual capsula (0.32) + margine.
- Verificato invariato: piani (box 0.60×0.45×0.08), porte (Blocker + Hinge), pulsante ascensore (0.10×0.30×0.20) — le hit box corrispondono già ai visual; l'area trigger NPC (sfera r 1) resta per le regole di gioco.
- Verifiche: in Play, raggio da 0.8 m in avanti a 1.5 m di altezza colpisce `RaycastSurface` per 5/5 NPC; `Physics.GetIgnoreCollision(box, CharacterController)=true` per 5/5 (nessuna spinta); `SmokeTest summary 50/50 ALL PASS`.
- File: modificato solo `Level/Prefabs/Evacuation/EvacNpc.prefab` e `EV_HANDOFF.md`. Nessun git, nessuna UI/animazione, Chemical Alert intoccato; Build Settings rimaste salvate.
- Comandi di test: J → davanti a NPC; grip/trigger → `[EV] NPC following`; U/Z → raccolta; Y → 50/50; avvicinarsi alla fila → nessuna spinta.
- Decisioni/deviazioni: il box è più alto del visual capsula (1 m) per consentire il raggio all'altezza della testa del giocatore; nessun nuovo layer, nessuna modifica al rig condiviso.
- Richieste all'utente: confermare in Simulator/Quest che puntare il collega sia facile e che la fila non spinga.
- Problemi aperti: nessuno in scope.

### E19 — Interazioni allineate a MESH_UFFICIO_COMPLETO — 2026-11-26
- Riferimento: FBX `Art/Models/MESH_UFFICIO_COMPLETO.fbx` già montato in `MODEL_Office` (111 mesh, pos 7.95/0/10.49, scala 1/1/−0.771). Muro sud uffici nel modello: z≈8.76 (cube `Cube.087`), contro z=8 dei collider graybox e z=8.12 dei piani.
- Fatto: spostati a z=8.76 i 5 collider `Wall_Offices_South_*` e le porte `Door_R1/R2`; spostati a z=8.92 i piani `Plan_R1/R2` e i marker `POS_Plan_R1/R2`, cioè davanti alla faccia del muro (8.86) perché lo sguardo e il raggio arrivano dall'interno ufficio.
- Non toccati: piano corridoio, porte scale interne/emergenza e pulsante ascensore (il FBX non ha una superficie equivalente) e area trigger NPC; `MODEL_Office` invariato.
- Verifiche: raggio da 0.8 m lato ufficio colpisce `Plan_R1/R2` e `Door_R1/R2` (porte da chiuse); `SmokeTest summary 50/50 ALL PASS` dopo l'allineamento; scena salvata (`isDirty=false`), editor riportato su `SCN_Core`.
- File: modificata solo `Level/Scenes/SCN_Evacuation.unity` e `EV_HANDOFF.md`. Nessun git, nessuna UI/animazione, Chemical Alert intoccato, Build Settings rimaste salvate.
- Comandi di test: `J` davanti a piano/porta → grip/trigger → `[EV] Plan inspected` / `[EV] Door ...`; `I` simula ispezione; `Y` → 50/50; Simulator: controllare che i piani siano attaccati al muro visibile.
- Decisioni/deviazioni: i piani sono 0.16 m davanti al centro muro per non essere occlusi dal collider del muro durante lo sguardo; porte centrate nel muro con apertura immediata; nessun collider aggiunto al FBX (resta visual-only).
- Richieste all'utente: confermare in Simulator/Quest che piani e porte siano dove si vede il muro del modello; se vuoi allineare anche corridoio/ascensore, indicare la superficie del FBX di riferimento.
- Problemi aperti: piano corridoio, ascensore e scale restano ancorati ai marker graybox.

### E20 — Mappa: corridoi e muri visibili — 2026-11-26
- Causa: `MODEL_Office` (`MESH_UFFICIO_COMPLETO.fbx`) contiene uffici/scrivanie ma non il corridoio; `Environment` ha i collider del corridoio ma 0 renderer, quindi corridoio e alcune pareti non si vedevano.
- Fatto: aggiunti `MeshFilter`+`MeshRenderer` (cube) ai collider esistenti `Floor_Corridor`, `Wall_Corridor_South_Seg0`, `Wall_Corridor_South_Seg1`, `Wall_StairsInt_West_Seg0`, `Wall_Elevator_East_Seg0`; materiali riusati `MAT_EV_Floor` e `MAT_EV_Wall`, scala = size del BoxCollider, `BatchingStatic`.
- Non duplicati i muri già presenti nel FBX (muro sud uffici a z 8.76, pareti x 2/14, scale emergenza). Nessun collider nuovo: solo visual sui collider già presenti.
- Verifiche: screenshot top-down mostra il corridoio completo (pavimento + muro sud + pareti laterali); `SmokeTest summary 50/50 ALL PASS`; 0 errori console; scena salvata, editor riportato su `SCN_Core`.
- File: modificata solo `Level/Scenes/SCN_Evacuation.unity` e `EV_HANDOFF.md`. Nessun git, nessuna UI/animazione, Chemical Alert intoccato.
- Comandi di test: guarda la scena dall'alto (`MODEL_Office`) → corridoio visibile; Play da `SCN_Core` → Home → missione → percorri il corridoio; `Y` → 50/50.
- Decisioni/deviazioni: il corridoio resta graybox con materiali del gruppo, non parte del FBX; per averlo nel modello serve un aggiornamento dell'FBX da parte del modellatore (fuori scope).
- Richieste all'utente: confermare che corridoio e muri mancanti ora ci siano; se vuoi il corridoio dentro `MESH_UFFICIO_COMPLETO`, passa un FBX aggiornato.
- Problemi aperti: il modello FBX non contiene il corridoio; i visual aggiunti sono graybox.

### E21 — Colleghi con modello TIZIO + nuovo ufficio — 2026-11-26
- Fatto: in `EvacNpc.prefab` il vecchio Visual graybox (cube) è stato sostituito da un'istanza annidata di `Art/Models/TIZIO/TIZIO.fbx` sotto il figlio `Visual`. Invariati `RaycastSurface` (BoxCollider non-trigger), sfera trigger, bridge `EvacNpc`, `LabelAnchor` e script di interazione: nessuna logica di gameplay toccata.
- Fatto: riallineati i 12 `NPCSLOT_*` in `SCN_Evacuation.unity`. I bounds Unity reali di `UFFICIO_ASSEMBLATO_UNITO` sono x ∈ [-6.0, 8.1], z ∈ [11.62, 23.46]: solo due scrivanie del modello cadono dentro l'ufficio 1 graybox (x ≈ 2.0 / 4.7, z ≈ 13.7). I 6 marker R1 sono quindi posizionati accanto a quelle scrivanie e nelle zone libere del locale; i 6 marker R2 mantengono le posizioni validate di partenza (ufficio 1 + 6 m) perché il nuovo FBX non ha arredi in quell'area. Tutti i marker guardano a nord (+Z).
- File (rel. a GROUP/): modificati `Level/Prefabs/Evacuation/EvacNpc.prefab`, `Level/Scenes/SCN_Evacuation.unity`, `Docs/Agent/Evaquation/EV_HANDOFF.md`. Nessun codice C# modificato, nessun file fuori `G_Tiziano/`.
- Verifiche: prefab e scena riserializzati e salvati da Unity; 0 errori di compilazione. Smoke test eseguito in Play Mode: step NPC `25/50 NPC spawn 3+2 by seed...` e `43/50 E09 free lane on 200 seeds` PASS. Il suite completo riporta `38/50` per reference preesistenti non assegnate (`alarm`, 3 piani, 4 porte) già assenti nella scena HEAD prima di questa modifica: non dipendono dal cambio NPC.
- Decisioni/deviazioni: non è stato spostato o riscalato il modello ufficio dell'utente, per non alterare pareti/collider e lane già validati; gli NPC restano spawnati a runtime da `NpcRegistry`, non salvati come istanze di scena.
- Comandi di test: Play da `SCN_Evacuation` (il controller carica `SCN_Core` da solo) → `Y` → verificare i due step NPC su 50; `F11` → 5 replay; `F12` → 10 reset; `F5` → 200/200 lane libere.
- Richieste all'utente: verifica visiva in Simulator/Quest che i 5 colleghi TIZIO siano leggibili accanto alle scrivanie del nuovo ufficio; se vuoi che l'FBX copra anche l'ufficio 2, fornisci il modello con arredi in quell'area oppure autorizza uno spostamento/scala del modello.
- Problemi aperti: il nuovo FBX non è allineato al pavimento graybox (copre solo in parte l'ufficio 1); il smoke test completo resta 38/50 per i reference mancanti preesistenti.

### E22 — Ricostruzione SCN_Evacuation con UFFICIO_STACCATO + TAVOLO — 2026-11-27
- Fatto: passi 1–10 di `EV_REBUILD_STACCATO_PLAN.md`. Struttura da `UFFICIO_STACCATO.fbx` (referenziato da Shared), 8 `EvacDesk`, collider `Environment` a soli box, 49 marker + 10 `FIRESLOT` riposizionati, porte/piani/allarme/pulsante/cartello con modelli nuovi, riferimenti ricollegati, fix Hinge.
- File (rel. a GROUP/): `Level/Scenes/SCN_Evacuation.unity` (+ backup `SCN_Evacuation_PRE_STACCATO.unity`), `Level/Prefabs/Evacuation/EvacDesk.prefab`, materiali `MAT_EV_AlarmModel_Red/ExitSignModel_*`, `Code/Scripts/Evacuation/Interaction/DoorInteractable.cs` (**unico C# modificato**: `IgnoreCollision` rig↔collider non-blocker, come E16), `Docs/EVAC_PERF_REPORT.md`, `Docs/Agent/Evaquation/EV_REBUILD_STACCATO_PLAN.md`.
- Verifiche: Y `50/50 ALL PASS`; `ValidateSeeds(200)` → 0 scartati; O/F11/F12 eseguiti senza errori `[EV]` (riepiloghi non riletti); ≈387k tri statici. Pulizia: `SEGNALEVERDE` eliminato, `UFFICIO_ASSEMBLATO_UNITO` **disattivato** (non eliminato).
- Decisioni/assunzioni: annesso est = scale interne (mai confermato dall'utente); ascensore a ovest del corridoio; scale senza gradini percorribili (solo pianerottolo → `ExitZone_*`); ID `evac.plan.*` e `evac.door.stairs.internal` assegnati da me (validati dallo smoke test).
- Richieste all'utente: (1) test a mano nel Simulator: passaggio da tutte le porte aperte, scambio visivo porta emergenza, tavoli/NPC, tempi 180/120 s da ritarare; (2) clip audio per `AlarmSystem.alarmClip` (vuoto); (3) input A/X per gli hint: `EvacHintInput` non esiste più (rig ora `SI81.Rig`), resta `Director.RequestNextHint()` e tasto B; (4) confermare annesso est = scale interne.
- Problemi aperti: draw call/frame time su Quest non misurati; `Door_StairsEmergency` scambio visual non testato in Play; `LabelAnchor` su `Door_StairsInternal` assente.
