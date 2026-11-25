# TASK_PROGRESS — Man Down (G_Marius)

- **Data:** 2026-11-25
- **Stato:** otto decisioni aperte e decisioni integrative **approvate e documentate**; TASK 01 (rig condiviso) e TASK 02 (`ManDownScenario`) completati e verificati in Editor/Play Mode. Il controller non crea UI, input o interazioni: `ManDownNpc`, `ManDownPhone`, `ManDownCpr`, `ManDownAed` e scoring definitivo restano da implementare. Validazione fisica su Meta Quest 3 ancora **manuale**.
- **Regole operative:** `Docs/Man_Down/AGENTS.md` §0, §0.1, §0.2 e §0.3 (quest'ultimo ha priorità su rig, UI e Build Settings). Leggere `AGENTS.md` e questo file all'inizio di ogni task e dopo ogni interruzione/compattazione.
- **Priorità:** le decisioni approvate dall'utente prevalgono su GDD specifico, flowchart e GDD generale in caso di incongruenze.

---

## Roadmap dei task

| # | Task | Stato | Note |
|---:|---|---|---|
| 1 | Preparazione e documentazione | **Completata** | TASK 00, 00b, 00c, 00d, 00e; analisi, direttive e decisioni approvate registrate. |
| 2 | Integrazione del player rig condiviso (TASK 01) | **Completata in Editor/Play Mode** | Nessun rig locale: la scena usa `SCN_Core`, `InputService`, `Interactable` e `RayInteractor` condivisi. Prova su Quest 3 ancora manuale. |
| 3 | Controller della missione (TASK 02) | **Completata** | `ManDownScenario` eredita da `ScenarioController`; stato, random 70/30 e 75/25 configurabili/forzabili, timer condiviso 180 s, API ed eventi verificati. |
| 4 | Controllo della coscienza | **Logica pronta (TASK 02)** | `VerifyConsciousness()` + evento; manca il componente che lo richiama. |
| 5 | Telefono e chiamata al 112 | **Logica pronta (TASK 02)** | `Call112()` e penalità `EMERGENCY_NOT_CALLED`; manca `ManDownPhone` e l'interazione. |
| 6 | Controllo della respirazione | **Logica pronta (TASK 02)** | Esito 70/30 configurabile/forzabile e evento; manca l'indizio visivo in `ManDownNpc`. |
| 7 | Posizione laterale e monitoraggio | **Logica pronta (TASK 02)** | `PlaceSidePosition()` e `Monitor()` nel ramo normale; manca la posa in `ManDownNpc`. |
| 8 | Meccanica RCP | Non avviato | `ManDownCpr` deve produrre risultati grezzi; la registrazione in `ManDownScenario` è pronta (20/20/30, 100–120 cpm come dati). |
| 9 | Meccanica DAE | Non avviato | DAE 75% / 25%; `ManDownAed` con elettrodi snap; la registrazione degli step è pronta in TASK 02. |
| 10 | Scoring ed esiti | **Parziale (TASK 02)** | Soglie 80/50, ID ed esito unico implementati; mancano le cinque dimensioni definitive e l'integrazione con lo SPI comune. |
| 11 | Test completo del gameplay | Non avviato | Play Mode senza visore; poi prova su Quest 3 con il rig condiviso di `SCN_Core`. |
| 12 | Integrazione UI SI81 | Non avviato | Task separato e autorizzato: il kit è disponibile, ma nessuna UI specifica prima del task dedicato. |
| 13 | Validazione finale | Non avviato | Verifica su visore e distinzione tra test eseguiti e test manuali. |

---

## Task corrente

- **TASK 02 — Controller logico centrale `ManDownScenario`** (2026-11-25).
- **Richiesta:** completare esclusivamente il controller logico compatibile con `ScenarioController` condiviso: inizializzazione all'inizio effettivo, condizioni indipendenti configurabili/forzabili, fasi non rigide, registro azioni e timestamp, ricezione dei risultati RCP grezzi, timeout 180 s, esiti e ID approvati, API/eventi per le classi future. Nessuna UI, input XR, interazione o gameplay dei componenti dedicati.
- **Esito:** due script creati in `Code/Scripts/ManDown/`; su `MNG_Scenario` il componente base `SI81.UI.ScenarioController` è stato **sostituito** da `SI81.ManDown.ManDownScenario` (non aggiunto accanto: la base ha `[DisallowMultipleComponent]`). Missione e spawn point preservati.
- **Timer:** `HUDRoot.BeginFromSession()` avvia già il timer condiviso; `ManDownScenario` riavvia lo stesso `HUDService.StartTimer(int)` con 180 s. Nessun contatore parallelo; alla scadenza `TimerExpired` conclude con `TIMEOUT`.
- **Verifiche:** ricompilazione riuscita con 0 errori; harness temporaneo isolato **49/49 verifiche superate, 0 fallimenti**; Play Mode con timer condiviso attivo e scadenza forzata; console runtime 0 errori.
- **Limiti:** `ManDownNpc`, `ManDownPhone`, `ManDownCpr`, `ManDownAed`, UI e scoring definitivo non sono stati implementati. Nessuna prova su visore: movimento/rotazione reali, pose controller, haptics e presa/selezione restano test manuali. TASK 01 resta completato e verificato come registrato sotto.

---

## Operazioni completate

### Analisi documentale e ispezione reale (TASK 00 / TASK 01)

- Fonti lette: `AGENTS.md`, specifiche, scoring, GDD specifico e generale, flowchart, guide UI, guida interazioni, `Docs/SharedPlayerRig.md`, `Packages/manifest.json`.
- Ispezione Unity MCP della scena attiva: un solo root `/prefab_scena`, nessuna Camera/rig/UI/script; collider solo su `MESH_OfficeStructure`.
- Pacchetti verificati: Meta XR Core `209.0.0`, Input System `1.19.0`, XR Management `4.7.0`, OpenXR `1.16.1`, URP `17.3.0`, Test Framework `1.6.0`; **XRI non installato**.
- Verificato in fase storica (2026-11-24) che il rig documentato in `SharedPlayerRig.md` non era ancora presente nel progetto (`MNG_CoreRig.prefab`, assembly `SI81.*`, `InputService`, `RigRegistration`, `Interactable`, `SharedInteraction.md`, `SCN_Core` assenti). **Stato superato:** dopo il merge con `origin/main` il sistema condiviso è presente e integrato; vedere § "Integrazione del rig condiviso" e `AGENTS.md` §0.3.
- Nessuna modifica alla scena o ad asset Unity nelle fasi documentali TASK 00/00b/00c/00d/00e e nella prima registrazione di TASK 01.

### Integrazione del rig condiviso (TASK 01, 2026-11-25)

- Creato il branch `migration/group_marius`, unito `origin/main` e risolti i conflitti sui percorsi condivisi mantenendo la versione di `main`; commit locali `080a834` (backup prima dello sviluppo) e `ef1bbae` (merge). **Nessun push**.
- Scena rinominata da `SCN_Man_Down.unity` a `SCN_ManDown.unity`, preservando il GUID `3b4441978213bad42a806f3c23963e12`. Build Settings aggiornati con `SCN_Core.unity` indice 0, `SCN_MockScenario.unity` indice 1 e `SCN_ManDown.unity` indice 2.
- Riutilizzato `SO_Mission_ManDown.asset` esistente (Id `03`, `SceneName: SCN_ManDown`) con il `ScenarioController` base: non è stata creata una missione duplicata.
- Aggiunti come figli di `prefab_scena`: `MNG_SpawnPoint` con `SI81.Rig.SpawnPoint` a `(-17.240, 0.253, 26.600)`, yaw `138`; `MNG_Scenario` con `SI81.UI.ScenarioController`, missione collegata, spawn point collegato, `editorLevel=Guided` e `loadCoreInEditor=false`. La scena conserva un solo root.
- `loadCoreInEditor=false` è necessario perché `RigDirectPlay` carica già `SCN_Core` all'ingresso in Play Mode quando trova `MNG_SpawnPoint`. Con `true` comparivano due `SCN_Core`, due `RigRegistration` e due `MNG_UI`.
- La scena Man Down non contiene Camera, rig, Canvas, HUD, EventSystem, input locale, `OVRInput`, XRI, raycast o selezione duplicati.
- Durante le verifiche Unity ha riscritto automaticamente due materiali di `Shared` e `ProjectSettings/ShaderGraphSettings.asset`: le modifiche non intenzionali sono state ripristinate. È stata conservata solo la modifica voluta a `ProjectSettings/EditorBuildSettings.asset`.
- Dopo un primo salvataggio i due marker non risultavano ancora scritti su disco: sono stati ricreati, la scena è stata salvata con `SaveScene` e la loro persistenza è stata verificata rileggendo il file `.unity` (`editorLevel: 1`, `loadCoreInEditor: 0`).

### Decisioni approvate (Task 00e)

1. **Probabilità iniziali.** All'avvio si randomizzano in modo indipendente respirazione dell'NPC e disponibilità del DAE: respirazione anormale **70%** / normale **30%**; DAE presente **75%** / assente **25%**. I valori sono configurabili dall'Inspector. È prevista la forzatura manuale di entrambe le condizioni per testare tutte le combinazioni senza dipendere dalla randomizzazione.
2. **Meccanica RCP.** Basata sul movimento fisico dei controller VR, non su pulsazioni ripetute di un pulsante. Zona dedicata sul torace; due soglie distinte per compressione e rilascio; nuova compressione solo dopo il rilascio della precedente; valutazione di posizione, ampiezza virtuale e frequenza; progressione da numero di compressioni valide + frequenza adeguata; registrazione delle compressioni errate e della qualità complessiva; parametri e soglie configurabili e sottoposti a validazione didattica. Il rilevamento fisico è separato dalla logica di valutazione. Non si simula la forza fisica sul torace.
   - **Integrazione definitiva:** RCP iniziale con DAE **20** compressioni valide; RCP dopo defibrillazione **20**; RCP senza DAE **30** complessive; frequenza di riferimento **100–120 cpm**; valutare anche il rilascio; **nessuna ventilazione**; il DAE può essere usato tempestivamente anche prima di completare le 20 iniziali. I numeri sono soglie di progressione gameplay, non criteri clinici di rianimazione.
3. **Meccanica DAE.** Procedura interattiva: recupero, attivazione, posizionamento manuale dei due elettrodi nelle zone dedicate, snap automatico degli elettrodi corretti, analisi simulata e defibrillazione quando indicata, ripresa della RCP. Gli errori sono recuperabili ma registrati per lo scoring. Non esiste una mesh con elettrodi separati: si userà il modello disponibile più due elettrodi provvisori realizzati con primitive Unity appiattite e materiali semplici. La sostituzione futura delle mesh non deve richiedere modifiche alla logica. Nessun cavo fisico, simulazione di cavi o animazione complessa.
4. **Timeout e arrivo dei soccorsi.** Sistema ibrido: arrivo simulato dopo il completamento delle procedure fondamentali e di una breve fase finale di assistenza, senza attese passive prolungate. Percorsi: respirazione normale → assistenza e monitoraggio; anormale senza DAE → RCP; anormale con DAE → RCP, DAE, ripresa RCP. **Solo modalità Realistica**, timeout **180 s**; Guidata e Stress non si implementano ora, ma i parametri restano configurabili. Scadenza = fallimento automatico `TIMEOUT`, ma tutte le azioni eseguite vengono comunque valutate. Non è obbligatorio completare tutte le azioni corrette per concludere la missione. La mancata chiamata al 112 non impedisce la conclusione: non si simula un arrivo attribuito a una chiamata mai effettuata e non si dichiara l'NPC fuori pericolo; l'esito massimo è successo parziale.
5. **Scoring ed esiti.** Successo `>= 80`; successo parziale `>= 50` e `< 80`; fallimento `< 50` (`LOW_SCORE`). Fallimenti automatici, indipendenti dal punteggio: RCP completamente omessa quando necessaria (`CPR_OMITTED`) e scadenza del timer (`TIMEOUT`). Penalizzazioni senza fallimento automatico: `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE`. RCP omessa e RCP tentata male restano distinte. Si mantengono le cinque dimensioni: Safety 5%, Awareness 10%, Decision 30%, Execution 30%, Time 25%; Safety resta neutra 5/5 fino a rischi ambientali valutabili (oggi esclusi). Non si modifica lo scoring condiviso né lo SPI comune. La scelta finale tra soglie sul punteggio ponderato di Man Down o sullo SPI comune è rinviata all'integrazione finale. La mancata chiamata limita l'esito a successo parziale senza alterare artificialmente il punteggio; i fallimenti automatici mantengono la priorità.
6. **Primo soccorso aziendale.** Meccanica esclusa completamente. Unica comunicazione richiesta: chiamata simulata al 112. Nessuna chiamata aggiuntiva agli addetti aziendali, nessuna attivazione automatica, nessun NPC dedicato, nessuna penalità relativa. La decisione sostituisce il GDD specifico.
7. **Sorgente dei trigger.** La logica dello scenario resta indipendente dal player rig e dal sistema di interazione VR. Gli script espongono metodi pubblici ed eventi utilizzabili da componenti intermedi. Si separano: rilevamento fisico delle interazioni, logica dello scenario, validazione delle azioni, scoring, presentazione e feedback UI. La logica deve essere testabile anche senza visore. Nessuna dipendenza da un rig locale: l'integrazione usa il rig condiviso di `SCN_Core` e le API `SI81.*`.
8. **Identificativi interni.** Fallimento: `CPR_OMITTED`, `TIMEOUT`, `LOW_SCORE`. Errori/omissioni con penalizzazione: `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE`. Tutte le cause di fallimento presenti vengono registrate, ma la missione viene conclusa una sola volta. Gli ID sono indipendenti dalle descrizioni mostrate al giocatore, che saranno in italiano e gestite in fase di integrazione UI.

### Decisioni integrative definitive

- Si sviluppa **esclusivamente la modalità Realistica** con timeout 180 s; Guidata e Stress non vanno implementate ora, ma i parametri restano configurabili per future estensioni.
- RCP: 20 compressioni valide iniziali con DAE, 20 dopo defibrillazione, 30 senza DAE; 100–120 cpm; posizione, ampiezza, frequenza e rilascio valutati; nessuna ventilazione; il DAE non viene bloccato dalle 20 iniziali; tutto configurabile dall'Inspector.
- Mancata chiamata al 112: penalizzazione già definita, impossibilità di successo completo, esito massimo successo parziale, nessun arrivo simulato dei soccorsi, nessuna dichiarazione che l'NPC sia fuori pericolo, nessuna alterazione artificiale del punteggio; i fallimenti automatici conservano la priorità.
- Queste decisioni sostituiscono qualunque indicazione precedente incompatibile nei PDF e nei documenti del gruppo.

### Controller logico centrale (TASK 02, 2026-11-25)

- **File creati:** `Code/Scripts/ManDown/ManDownScenario.cs` e `Code/Scripts/ManDown/ManDownScenarioTypes.cs` (con i relativi `.meta` generati da Unity), namespace `SI81.ManDown`.
- **`ManDownScenario` eredita da `SI81.UI.ScenarioController`**: sessione, spawn e chiusura missione restano alla classe base. Non duplica timer, UI o input.
- **Inizializzazione:** `OnScenarioStarted()` richiama `InitializeScenario()`, idempotente; azzera azioni, risultati RCP, cause, penalità, timestamp e flag; estrae le condizioni iniziali e avvia il timer. `ResetScenario()` azzera tutto e reinizializza solo se la missione è ancora attiva; dopo la conclusione serve ricaricare la scena. `ClearEvents()` resta solo in `OnDestroy()` per non perdere le sottoscrizioni delle future classi.
- **Condizioni iniziali:** probabilità 70/30 e 75/25 configurabili dall'Inspector; modalità `Random`, `ForceNormal`, `ForceAbnormal`, `ForceAvailable`, `ForceUnavailable` per test deterministici; indipendenti e stabili per tutta la partita.
- **Fasi non rigide:** `NotStarted`, `Initialization`, `ConsciousnessAssessment`, `BreathingAssessment`, `Assistance`, `Cpr`, `Aed`, `CprResume`, `FinalAssistance`, `Concluded`. Le azioni possono essere tentate in ordine diverso e corrette; errori e procedure incompatibili vengono registrati.
- **Registro e RCP:** ogni azione produce un `ManDownActionRecord` con ID, fase, timestamp, completamento, esito e nota; i duplicati delle azioni uniche sono ignorati. `ReportCprResult(ManDownCprResult)` conserva solo i valori grezzi e la soglia di completamento (20/20/30), senza rilevare compressioni, validare qualità o posizionare elettrodi.
- **Conclusione:** `FinishMission()` usa il punteggio provvisorio; `FinishMission(int)` consente un valore esplicito per test e per il futuro scoring, applicando comunque timeout, `CPR_OMITTED`, `LOW_SCORE`, penalità e limite a parziale senza 112. Una sola conclusione per istanza.
- **Esiti:** `>= 80` successo, `>= 50` e `< 80` parziale, `< 50` fallimento; fallimenti automatici e penalizzazioni con tutti gli ID approvati, registrabili in più cause. Senza chiamata al 112 non c'è arrivo simulato né dichiarazione di fuori pericolo e l'esito è al massimo parziale, salvo fallimenti automatici.
- **Scena:** su `MNG_Scenario` il componente `SI81.UI.ScenarioController` è stato rimosso e sostituito da `SI81.ManDown.ManDownScenario`; missione `SO_Mission_ManDown` e spawn point preservati, `editorLevel=Guided`, `loadCoreInEditor=false`, un solo componente derivato da `ScenarioController`.
- **Non implementato:** rilevamento fisico RCP/DAE, componenti interattivi su NPC/telefoni/DAE, UI, audio, input XR e calcolo definitivo delle cinque dimensioni.

---

## Operazioni rimanenti

1. **Attendere approvazione esplicita prima del task successivo.** TASK 02 è chiuso in Editor/Play Mode; non iniziare autonomamente i componenti dedicati.
2. **`ManDownNpc`:** rappresentazione NPC con respiro e posa laterale via transform, zone configurabili per torace ed elettrodi; richiama `VerifyBreathing()`, `PlaceSidePosition()` ed `Monitor()`.
3. **`ManDownPhone`:** interazione con i telefoni esistenti e chiamata simulata tramite `Call112()`.
4. **`ManDownCpr`:** rilevamento fisico del movimento dei controller nella zona torace, due soglie, riarmo al rilascio, ampiezza/frequenza; produce `ManDownCprResult` grezzi senza calcolare punteggi.
5. **`ManDownAed`:** recupero, attivazione, elettrodi provvisori con snap, analisi simulata, defibrillazione e ripresa RCP tramite le API del controller.
6. **Scoring definitivo:** cinque dimensioni 0–100 con i pesi approvati, soglie 80/50 e scelta finale con lo SPI comune; il controller espone già gli ID, il risultato congelato e il tempo trascorso.
7. **Test completi e UI:** Play Mode senza visore, poi integrazione UI SI81 in task separato e validazione su Meta Quest 3.
8. **Dati didattici mancanti:** ampiezza/soglie RCP, durata della fase finale, `T_target`/`T_max`: campi configurabili, da validare, non inventare.

---

## File creati o modificati

| File | Operazione | Fase |
|---|---|---|
| `Docs/Man_Down/TASK_PROGRESS.md` (e relativo `.meta` generato da Unity) | Creato, poi aggiornato in Task 00b, 00c, 00d, TASK 01 e Task 00e; aggiornato con l'esito della migrazione | TASK 00 / 00b / 00c / 00d / 01 / 00e / migrazione TASK 01 |
| `Docs/Man_Down/AGENTS.md` | Modificato: §0, §0.1, §0.2, §3, §6, §7, §10; aggiornato con l'esito della migrazione | TASK 00 / 00b / 00c / 00d / 01 / 00e / migrazione TASK 01 |
| `Docs/Man_Down/Man_Down_Specifiche.md` | Riscritto per coerenza con le decisioni approvate; rig temporaneo sostituito dal rig condiviso | Task 00e / migrazione TASK 01 |
| `Docs/Man_Down/Man_Down_Scoring_System.md` | Aggiornato: soglie, ID, RCP/DAE, esiti, limiti di integrazione e rig condiviso | Task 00e / migrazione TASK 01 |
| `Level/Scenes/SCN_ManDown.unity` (+ `.meta`) | Creato dal rename di `SCN_Man_Down.unity`, GUID preservato; aggiunti `MNG_SpawnPoint` e `MNG_Scenario` sotto `prefab_scena` | Migrazione TASK 01 |
| `Level/Scenes/SCN_Man_Down.unity` (+ `.meta`) | Vecchio percorso risultato `D` dopo il rename | Migrazione TASK 01 |
| `ProjectSettings/EditorBuildSettings.asset` | Modificato con autorizzazione: aggiunta/ordinamento delle tre scene (`SCN_Core`, `SCN_MockScenario`, `SCN_ManDown`) | Migrazione TASK 01 |
| `Code/Scripts/ManDown/ManDownScenario.cs` (+ `.meta`) | Creato: controller `SI81.ManDown.ManDownScenario` derivato da `ScenarioController` | TASK 02 |
| `Code/Scripts/ManDown/ManDownScenarioTypes.cs` (+ `.meta`) | Creato: enum di condizioni/fasi, record azione, risultato RCP e risultato scenario | TASK 02 |
| `Level/Scenes/SCN_ManDown.unity` | Aggiornato: `MNG_Scenario` usa `ManDownScenario` invece del componente base; missione e spawn point preservati | TASK 02 |

**Modifiche globali autorizzate:** solo `ProjectSettings/EditorBuildSettings.asset`. `SO_Mission_ManDown.asset` è stato riutilizzato senza modificarlo.
**Non modificati in TASK 02:** `Shared`, `Home`, altri gruppi, `Packages`, configurazioni globali, `SO_Mission_ManDown.asset` e `.gitignore`. Nessun commit o push. I documenti `!LLM_Migrazione_Rig_UI.md` e `SharedPlayerRig.md` (con i relativi `.meta`) restano intenzionalmente untracked.
**Nota:** `git status` mostra ancora modifiche auto-generate da Unity su due materiali condivisi (`MAT_CabinetBody.mat`, `MAT_CoffeeTableBody.mat`): non sono state introdotte da TASK 02 e vanno verificate/ripristinate prima di un eventuale commit.

---

## Decisioni tecniche

### Piano di implementazione aggiornato

Obiettivo: flusso completo e verificabile senza UI specifica, senza XRI alternativo, senza rig locale e senza input locale. La logica resta testabile da Inspector e da UnityEvent, anche senza visore; il player condiviso resta l'unico sistema di camera, locomozione e interazione.

```text
MNG_Scenario  →  ManDownScenario   (stato, random, tempi, validazione, esiti, ID, eventi)
                   ├─ ManDownNpc    (rappresentazione NPC, zone torace/elettrodi, respiro e posa via transform)
                   ├─ ManDownPhone  (chiamata 112 simulata)
                   ├─ ManDownCpr    (solo rilevamento fisico delle compressioni)
                   └─ ManDownAed    (procedura DAE con elettrodi provvisori)
```

| Classe | Responsabilità | Non fa |
|---|---|---|
| `ManDownScenario` | Stato autorevole: coscienza, respirazione, disponibilità DAE, 112 chiamato, fasi RCP/DAE, tempi, cinque dimensioni grezze, ID di errore/fallimento, esito unico. Valida posizione/ampiezza/frequenza/rilascio e conteggi 20/20/30. | UI, input, fisica, animazioni complesse, rilevamento dei controller. |
| `ManDownNpc` | Rappresentazione: respiro visivo e posa laterale via transform; espone transform/zone configurabili per torace e due elettrodi. | Decidere esito clinico, punteggio o validazione. |
| `ManDownPhone` | Rappresenta il telefono e chiama `Call112()` quando avviene l'interazione. | Aprire UI telefonica (inesistente) o chiamare i soccorsi da sé. |
| `ManDownCpr` | Rilevamento fisico: zona torace, due soglie compressione/rilascio, riarmo, ampiezza virtuale e intervalli; emite valori grezzi/eventi. | Calcolare punteggio, qualità, esito o mostrare feedback. |
| `ManDownAed` | Recupero, attivazione, elettrodi con snap, analisi simulata, defibrillazione e ripresa RCP; usa zone/transform configurabili e primitive provvisorie. | Dipendere dalla mesh specifica, simulare cavi o bloccare il ramo senza DAE. |

**API pubblica reale di `ManDownScenario`** (TASK 02, richiamabile da UnityEvent o da componenti intermedi): `InitializeScenario()`, `ResetScenario()`, `VerifyConsciousness()`, `VerifyBreathing()`, `Call112()`, `StartCpr()`, `ReportCprResult(ManDownCprResult)`, `ReportCprPoorQuality()`, `ActivateAed()`, `PlaceElectrode(int)`, `PlaceLeftElectrode()`, `PlaceRightElectrode()`, `ConfirmAedInstruction()`, `ReportAedIncorrectUse()`, `ResumeCpr()`, `PlaceSidePosition()`, `Monitor()`, `BeginFinalAssistance()`, `CompleteFinalAssistance()`, `FinishMission()`, `FinishMission(int)`, `GetRequiredCprCompressions(ManDownCprPhase)`. I due wrapper senza parametri per gli elettrodi sono già presenti per UnityEvent.

Eventi C# pubblici: `PhaseChanged`, `ConsciousnessAssessed`, `BreathingAssessed`, `EmergencyCalled`, `CprStarted`, `CprResultRegistered`, `CprQualityIssueReported`, `AedActivated`, `AedElectrodePlaced`, `AedInstructionConfirmed`, `AedIncorrectUseReported`, `CprResumed`, `SidePositionPlaced`, `MonitoringCompleted`, `FinalAssistanceStarted`, `ActionRecorded`, `MissionConcluded`.

Proprietà in sola lettura per lo stato: `CurrentPhase`, `IsInitialized`, `AcceptsActions`, `IsMissionConcluded`, `IsBreathingAbnormal`, `IsAedAvailable`, flag delle azioni registrate, `MissionElapsedSeconds`, `Actions`, `CprResults`, `FailureCauses`, `Penalties`, `LastResult`.

La scena è collegata tramite `MNG_Scenario` a `ManDownScenario`, che eredita dal `ScenarioController` condiviso: sessione, spawn, timer e UI restano quelli della classe base e del kit comune, senza duplicazioni.

### Rig e interazione condivisi (verificati in TASK 01)

- Il rig vive solo in `SCN_Core`: `RigRegistration`, camera abilitata `CenterEyeAnchor`, `InputService`, `RayInteractor`, `RigUIBridge` ed `EventSystem` sono quelli del sistema comune.
- La scena Man Down contiene solo i marker `MNG_SpawnPoint` e `MNG_Scenario` sotto `prefab_scena`; il caricamento di `SCN_Core` in Editor è gestito da `RigDirectPlay`, quindi `loadCoreInEditor` resta `false`.
- API controller disponibili per le future meccaniche: `WasSelectPressed`, `IsGrabHeld`, `WasMenuPressed`, `WasBackPressed`, `ReadMove`, `ReadTurn`, `TryGetAimPose`, `PushContext`, `IsPointingAtUI`; eventi `SelectPerformed`, `SelectCanceled`, `GrabPerformed`, `GrabCanceled`, `MenuPerformed`, `BackPerformed`, `MoveChanged`, `TurnChanged`.
- Il gameplay Man Down dovrà chiamare queste API e gli eventi del rig condiviso, senza introdurre `OVRInput`, Input Actions proprie, XRI o un secondo raycast/selezione/grab.

### Regole ora fissate

- Niente Canvas/HUD/etichette/`LabelAnchor` né UI provvisoria prima del task dedicato.
- Niente pericolo ambientale, pozzanghera, panno, danni o penalità Safety: Safety resta neutrale 5/5.
- Niente ragdoll: posa laterale e respiro visivo via transform.
- Solo modalità Realistica, 180 s; Guidata e Stress non implementate, parametri configurabili.
- RCP: 20 / 20 / 30 compressioni valide, 100–120 cpm, nessuna ventilazione, DAE non bloccato dalle 20 iniziali.
- DAE: elettrodi provvisori con snap, nessun cavo fisico; la logica non dipende dalla mesh.
- Mancata chiamata: nessun arrivo dei soccorsi, nessuna dichiarazione di fuori pericolo, esito massimo successo parziale.
- Soglie 80 / 50; ID `CPR_OMITTED`, `TIMEOUT`, `LOW_SCORE`, `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE`.
- Punteggi GDD generale 30/10/0 e modalità Guidata/Stress sono superati dalle regole approvate.
- Il rig è quello condiviso di `SCN_Core`: non creare un rig locale, una camera, un EventSystem o un sistema di interazione alternativo.
- Il punteggio ponderato di `Man_Down_Scoring_System.md` resta un modello locale: conservare i cinque valori grezzi 0–100 senza falsarli per la UI comune.

### Regole di lavoro

Sintesi in cima a questo file; elenco completo in `AGENTS.md` §0.1, §0.2 e §0.3. In particolare: un solo task alla volta, solo file di `G_Marius` salvo autorizzazioni esplicite, riutilizzo dell'esistente, Unity MCP per la scena, aggiornamento dei documenti a ogni fase, nessun commit/push senza autorizzazione.

---

## Test effettuati

**Realmente eseguiti (Editor / Play Mode / Test Runner):**

- Compilazione Unity completata dopo il merge e l'aggiunta dei marker; Editor `ready`, `compilationFailed: false`.
- Rilettura su disco della scena `SCN_ManDown.unity`: un solo root `prefab_scena`, `MNG_SpawnPoint` e `MNG_Scenario` persistiti, `editorLevel: 1` (Guided), `loadCoreInEditor: 0`; `isDirty: false`.
- Rilettura su disco di `ProjectSettings/EditorBuildSettings.asset`: `SCN_Core` indice 0, `SCN_MockScenario` indice 1, `SCN_ManDown` indice 2 con il GUID preservato.
- Validatore scenario SI81: **0 errori, 9 avvisi**. Tutti gli avvisi sono di naming preesistente (`MESH_Chair_0..3`, `MESH_TablePrinter (1)..(3)`, `MESH_Lamp_2`, `MESH_Sofa_1`); non riguardano il rig o la configurazione appena aggiunta.
- Play Mode avviato dalla scena salvata su disco: una sola `SCN_Core`, un solo `RigRegistration`, una sola camera abilitata (`CenterEyeAnchor`), un solo `EventSystem`, due `RayInteractor` condivisi, un solo `RigUIBridge`, un solo `InputService`.
- Sessione attiva con `SO_Mission_ManDown`; `TrackingOrigin` e `Head` a `(-17.240, 0.253, 26.600)` con yaw `138`.
- Stabilità del rig: `RIG_POS_1=(-17.2400, 0.3332, 26.6000)` e `RIG_POS_2=(-17.2400, 0.3332, 26.6000)` dopo circa 65 s; nessuna caduta continua.
- Console runtime: **0 errori**. I warning osservati sono ambientali e preesistenti (MetaXR/OpenXR senza dispositivo, Unity AI account, `UIInputSetup` al primo `Awake`, OVRHaptics senza controller); nessun warning `Nessun IRigUIBridge`.
- Test PlayMode del rig/interazione condivisi: `unity command test run --mode playmode --async-tests true --filter SI81.Rig.Tests --timeout 600` → **23/23 superati, 0 falliti**.
### TASK 02 — verifiche eseguite

- Ricompilazione Unity dopo i nuovi script: `recompile_status=completed`, `failed=false`, `compilationFailed=false`; console con **0 errori**.
- Harness temporaneo isolato (`man_down_functional_harness.cs`, eseguito via Unity CLI): **49/49 verifiche superate, `TOTALE FAIL=0`**. Copertura: A1–A28 (init idempotente, forzature, flag e azioni uniche, 19/20 compressioni registrate come grezze, DAE prima delle 20, penalità multiple, eventi, conclusione unica, reset con eventi conservati, soccorsi solo con 112, cinque dimensioni con Time ancora a 0), B1–B5 (`CPR_OMITTED`, penalità 112, fallimenti e debrief), C1–C3 (`TIMEOUT`), D1–D6 (soglie 20 e 30, valori grezzi e penalità), E1–E3 (DAE prima della RCP), F1–F4 (`80` → successo, `50` → parziale, `49` → fallimento, `100` senza 112 → parziale).
- Play Mode dalla scena salvata: `scene=SCN_ManDown`, `scenarioCount=1`, `hudRootCount=1`, `initialized/running/accepts/sessionActive=True`, `duration=180`, `hudTimerRunning/countdown=True`, `timerSubscribers=1`: timer condiviso attivo, nessun contatore parallelo.
- Scadenza forzata del timer condiviso (`HUDService.RaiseTimerExpired`): `timedOut=True`, `outcome=Negative`, `failureCauses=[TIMEOUT,CPR_OMITTED,LOW_SCORE]`, `penalties=[EMERGENCY_NOT_CALLED]`, `hudTimerRunning=False`; una seconda chiamata produce lo stesso risultato (conclusione unica).
- Console runtime dopo Play Mode: `cursor:262`, **0 errori**. Play Mode fermato; `editor save_all` eseguito e scena con `isDirty:false`.
- Scena riletta su disco dopo la sostituzione del componente: `Assembly-CSharp::SI81.ManDown.ManDownScenario`, `editorLevel: 1`, `loadCoreInEditor: 0`, un solo componente derivato da `ScenarioController`.

**Non eseguiti / ancora da effettuare:**

- Prova fisica su Meta Quest 3: movimento/rotazione con controller reali, pose e modelli dei controller, haptics, selezione UI e presa degli oggetti.
- Rilevamento fisico delle compressioni e procedure DAE reali: `ManDownCpr` e `ManDownAed` non esistono ancora; il controller riceve solo risultati grezzi.
- Scoring definitivo e integrazione UI: le cinque dimensioni non sono ancora calcolate e la UI specifica non è stata collegata.
- Conferma visiva dell'utente della posa iniziale e dell'inquadratura sul visore.

---

## Problemi aperti

### Impedimenti verificati (non sono decisioni di gameplay)

1. Nessuna validazione hardware del rig: Editor riporta `No XR device is available ... Running without VR`, quindi movimento, rotazione, pose controller, haptics e presa/selezione reali richiedono la prova su Quest 3.
2. Nove avvisi di naming preesistente nel validatore di scenario: non bloccano il rig e non sono stati rinominati in questo task per evitare modifiche non richieste.
3. Warning ambientali MetaXR/Unity AI/UI all'avvio: presenti anche senza il nuovo contenuto di scena; non sono errori e non dipendono da Man Down.
4. Il controller logico è pronto, ma NPC, telefoni e DAE non hanno ancora componenti interattivi: `ManDownNpc`, `ManDownPhone`, `ManDownCpr` e `ManDownAed` useranno `Interactable`/`RayInteractor` condivisi nei task dedicati.
5. Nessun audio per 112/DAE/feedback; la presentazione avverrà solo nel task UI.
6. NPC senza Animator/clip: il respiro visivo resta transform-based, come già deciso.
7. Il punteggio di TASK 02 è **provvisorio**: calcolato dalle azioni registrate, con la dimensione Time ancora a 0 in attesa dello scoring definitivo; `FinishMission(int)` esiste solo per test e per il futuro calcolo.
8. Modifiche auto-generate da Unity su due materiali condivisi (`MAT_CabinetBody.mat`, `MAT_CoffeeTableBody.mat`) risultano ancora in `git status`: non appartengono a TASK 02 e vanno ripristinate prima di un commit.

### Dati ancora necessari prima del collaudo didattico (non ancora forniti)

- Valori numerici di ampiezza virtuale, soglie di rilascio e tolleranza di frequenza per la RCP: i campi saranno configurabili, **senza valori inventati**, e andranno validati dal referente didattico.
- Durata esatta della "breve fase finale di assistenza": campo configurabile, valore da validare.
- Soglie `T_target` / `T_max` per il punteggio Time: da playtest, non derivate da criteri clinici.
- Applicazione finale delle soglie 80/50 al punteggio ponderato di Man Down oppure allo SPI comune: da concordare durante l'integrazione finale.
- Descrizioni italiane mostrate al giocatore per gli ID: demandate alla UI finale.
- Solo se si vuole gestire un esito "shock non indicato": va definito. Altrimenti la defibrillazione resta uno step scriptato del solo ramo anormale con DAE.

**Nessuna delle otto decisioni aperte resta senza risposta:** probabilità, RCP, DAE, timeout/soccorsi, soglie/esiti, primo soccorso aziendale, trigger e ID sono approvati.

---

## Prossimo passaggio

- **Attendere approvazione esplicita prima del task successivo.** TASK 02 è completato in Editor/Play Mode; non avviare autonomamente `ManDownNpc`, `ManDownPhone`, `ManDownCpr`, `ManDownAed`, UI o scoring definitivo.
- Completare la procedura manuale su Meta Quest 3 riportata sotto e segnalare l'esito prima di considerare validata l'integrazione hardware del rig (TASK 01).
- Quando richiesto, implementare un solo componente alla volta, collegandolo alle API già esistenti del controller senza modificarne la logica di stato; verificare compilazione, riferimenti e test prima di aggiornare i documenti.
- I dati didattici mancanti elencati sopra restano configurabili e non vanno inventati.
- **Nessun commit/push** e **nessuna modifica a `.gitignore` senza nuova autorizzazione**; verificare e ripristinare le modifiche auto-generate da Unity sui due materiali condivisi prima di un eventuale commit.

### Procedura manuale su Meta Quest 3 (non ancora eseguita)

1. Connettere il Quest 3 in modalità sviluppatore (via Quest Link/Air Link per il Play Mode oppure con una build installata) e confermare che il dispositivo sia riconosciuto.
2. Avviare `SCN_ManDown` (indice 2 delle Build Settings). Verificare che venga caricato anche il player condiviso di `SCN_Core` e che non compaiano due `SCN_Core`, due `RigRegistration` o due `MNG_UI`.
3. Controllare che in scena ci siano **una sola camera abilitata** (`CenterEyeAnchor`), **un solo EventSystem**, **un solo InputService**, **due RayInteractor condivisi** e **un solo RigUIBridge**.
4. Verificare la posa iniziale: testa/tracking origin a `(-17.240, 0.253, 26.600)` con yaw `138`; rig a terra sul pavimento, nessuna caduta o deriva dopo alcuni secondi di riposo.
5. Provare movimento e rotazione reali con gli stick: il movimento deve seguire la direzione dello sguardo, la rotazione deve essere fluida o a scatti secondo la configurazione condivisa, senza deriva a riposo.
6. Provare menu/back, pausa e ripresa: il movimento deve fermarsi in pausa e i pulsanti devono agire sulla UI comune senza attivare azioni di scenario.
7. Provare trigger e grip: selezione UI senza selezionare l'oggetto di mondo dietro; presa/selezione degli oggetti solo tramite `Interactable` e `RayInteractor` condivisi.
8. Verificare haptics su menu, avvisi e interazioni, inclusa disattivazione dalle impostazioni comuni.
9. Raccogliere i log Meta Quest Developer Hub / logcat: nessun errore, nessun duplicato di `InputService`/`RigRegistration`/`MNG_UI`, nessun bridge mancante; verificare che i log `[SI81 Input]` riportino controller, pose del raggio e oggetto colpito.
10. Registrare esito, eventuali anomalie e data della prova in questo file: finché non viene eseguita, il rig risulta validato **solo in Editor/Play Mode**, non su hardware.
11. Il seriale `2G0YC5ZG1402NV` citato nei documenti è solo riferimento di connessione del dispositivo, **non** prova di avvenuta validazione.
