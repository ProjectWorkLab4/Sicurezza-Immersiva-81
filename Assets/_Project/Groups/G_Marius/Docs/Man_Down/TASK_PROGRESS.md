# TASK_PROGRESS — Man Down (G_Marius)

- **Data:** 2026-11-25
- **Stato:** otto decisioni aperte e decisioni integrative **approvate e documentate**; TASK 01 (rig condiviso), TASK 02 (`ManDownScenario`) e TASK 03 (sola verifica della coscienza dell'NPC) completati e verificati in Editor/Play Mode. **TASK 04.1 (analisi e predisposizione del telefono) completato in sola lettura:** nessuno script, input, UI, prefab, materiale o asset condiviso modificato; registrata la decisione di usare il popup comune `AskDecision("Chiami il 112?", ["Sì", "No"])` per la chiamata, compatibile con Operational. Con `No` il popup si chiude senza registrare nulla e il telefono resta interagibile; solo `Sì` registra la decisione chiamando `Call112()`. Il controller usa solo il timer comune e la coscienza resta sottoazione dell'obiettivo 2 senza `CompleteObjective()` né avanzamento da `PHASE_SCAN`. `ManDownNpc` esiste solo per la coscienza; **TASK 04.2 (`ManDownPhone`) implementato e compilato; TASK 04.3 (interazione VR) configurato; TASK 04.5 (popup UI) integrato; TASK 04.6 (validazione) in corso:** tutti e tre i telefoni hanno collider, `Interactable` GRIP, `ManDownPhone`, `Rigidbody`, `LabelAnchor` Interaction e il ponte `AskDecision`; `Sì` chiama `Call112()`, `No`/timeout non registrano e permettono di riprovare. Compilazione, riferimenti, Console e logica statica verificati; **i test runtime in Play Mode non sono stati eseguiti da OpenCode** (su indicazione dell'utente vanno fatti da lui): il Task 04 **non è ancora dichiarato completato**. `ManDownCpr`, `ManDownAed`, respirazione, scoring definitivo e UI dedicata restano da implementare. Validazione fisica su Meta Quest 3 ancora **manuale**. La transform di `MESH_ShoulderGrip` è confermata dall'utente e **non va toccata**; resta una sola modifica non documentata sui due materiali condivisi (vedi § "Problemi aperti"), non toccata in questo microtask.
- **Regole operative:** `Docs/Man_Down/AGENTS.md` §0, §0.1, §0.2 e §0.3 (quest'ultimo ha priorità su rig, UI e Build Settings). Leggere `AGENTS.md` e questo file all'inizio di ogni task e dopo ogni interruzione/compattazione.
- **Priorità:** le decisioni approvate dall'utente prevalgono su GDD specifico, flowchart e GDD generale in caso di incongruenze.

---

## Roadmap dei task

| # | Task | Stato | Note |
|---:|---|---|---|
| 1 | Preparazione e documentazione | **Completata** | TASK 00, 00b, 00c, 00d, 00e; analisi, direttive e decisioni approvate registrate. |
| 2 | Integrazione del player rig condiviso (TASK 01) | **Completata in Editor/Play Mode** | Nessun rig locale: la scena usa `SCN_Core`, `InputService`, `Interactable` e `RayInteractor` condivisi. Prova su Quest 3 ancora manuale. |
| 3 | Controller della missione (TASK 02) | **Completata** | `ManDownScenario` eredita da `ScenarioController`; stato, random 70/30 e 75/25 configurabili/forzabili, API ed eventi verificati. In TASK 03 rimossi avvio/riavvio timer duplicati: resta solo il timer comune. |
| 4 | Controllo della coscienza (TASK 03) | **Completata in Editor/Play Mode** | `ManDownNpc` su spalla con `Interactable`/`LabelAnchor` condivisi, GRIP → `VerifyConsciousness()`, azione unica `NPC incosciente`; nessun avanzamento HUD. Prova su Quest 3 ancora manuale. |
| 5 | Telefono e chiamata al 112 | **Validazione in corso (TASK 04.6)** | Popup, `Call112()`, etichetta, fisica e reset verificati staticamente/Editor; 8 test runtime in attesa di Play Mode da parte dell'utente. Task 04 non ancora chiuso. |
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

- **TASK 04.6 — Validazione della chiamata al 112** (2026-11-25), eseguito dopo TASK 04.5; conclude il filone Task 04 solo quando i test runtime saranno confermati.
- **Richiesta:** verificare i 8 comportamenti del flusso (popup, No, Sì con timestamp, assenza duplicati, omissione non bloccante, scadenza timer, reset, etichetta), compilazione/riferimenti/Console; correggere solo problemi riconducibili al Task 04; controllare i collider aggiunti dall'utente **senza modificarli**; controllare `git status`/`git diff`; non implementare scoring né toccare asset condivisi.
- **Esito:** compilazione `compilationFailed=false`, console **0 errori**; riferimenti Inspector validi su tutti e tre i telefoni; comportamento statico/Edit Mode coerente. I 8 test richiedono l'esecuzione in Play Mode, che OpenCode non avvia su indicazione dell'utente: risultano **"da verificare manualmente"**, non "superati". Il Task 04 resta aperto.
- **Collider esaminati (sola lettura, nessuna modifica):** 47 collider in scena, tutti non trigger; telefoni con collider + `Interactable` + `Rigidbody`; desk con 3 box ciascuno; pavimento/pareti/soffitto, porta, DAE, estintore, scrivanie, sedie, piante e NPC con collider. Dettagli e segnalazioni nel paragrafo dedicato.
- **Git:** modifiche presenti nei documenti `G_Marius`, nello scenario, nei prefab del gruppo (collider dell'utente) e in alcuni asset fuori dal gruppo (da non includere senza decisione). Elenco per il commit in "Prossimo passaggio".
- **Correzione etichetta dopo "Sì" (richiesta utente):** `ManDownPhoneUi` ora si sottoscrive anche a `ManDownScenario.EmergencyCalled` e `ApplyLabelState()` usa lo stato autorevole `ManDownScenario.IsCall112Made`: dopo la chiamata **tutti e tre** i telefoni nascondono l'etichetta, non solo quello usato. Verificato in Edit Mode (suppress su tutti, ripristino a nuova missione). Il comportamento di "No" resta invariato. Durante la ricompilazione intermedia erano comparsi due errori `CS0103` transienti (`OnEmergencyCalled` non ancora compilato) e alcuni assert `ShouldRunBehaviour()` causati dai test via `SendMessage` in Edit Mode: dopo la compilazione finale e la pulizia della Console, `compilationFailed=false` e **0 errori/warning**. Nessun errore runtime rimasto.
- **Raggio di interazione (decisione utente):** l'API condivisa espone un solo `maxDistance` (5 m) per hover/select/GRAB, non un limite dedicato al GRAB. L'utente ha scelto di **mantenere 5 m**: nessuna modifica a `SCN_Core` o ad altri asset condivisi.

- **TASK 04.5 — Integrazione UI della chiamata al 112** (2026-11-25), eseguito dopo TASK 04.3 e l'aggiornamento fisica/reset.
- **Richiesta:** aprire il popup decisionale condiviso all'interazione col telefono, con domanda "Chiami il 112?" e risposte "Sì"/"No"; collegare le risposte alla logica esistente; permettere una nuova interazione dopo "No", bloccarla dopo "Sì"; gestire la scadenza del timer come previsto dalla UI condivisa; configurare `LabelAnchor` coerente con GRIP; verificare Operational. Niente Canvas, popup/timer personalizzati, scoring o asset condivisi modificati.
- **Esito:** creato `ManDownPhoneUi.cs` (ponte) e aggiunto ai tre telefoni, con riferimenti a `ManDownPhone`, `ManDownScenario` e `LabelAnchor`; `AskDecision` viene chiamato con `timeoutSeconds=0`; callback 0 → `AnswerYes()` + `Call112()`, 1 e `-1` → `AnswerNo()`. Aggiunto `LabelAnchor` Interaction "Telefono"/"Chiama il 112" con chiavi vuote (predefiniti GRIP/Afferra); etichetta nascosta in presa e dopo la chiamata.
- **Verifiche:** ricompilazione 0 errori; rilettura dei componenti e dei persistent listener nella scena salvata; verifica Edit Mode dell'instradamento (No, -1, Sì, doppio Sì ignorato, etichetta e reset). Nessun Play Mode avviato da OpenCode: verifica del popup reale e della compatibilità Operational demandata all'utente.
- **Decisioni/limiti:** nessun timeout interno (`timeoutSeconds=0`), come da decisione precedente: se la missione scade mentre il popup è aperto, `ScenarioController.EndMission()` chiude il pannello con `Close()` senza callback e la penalità `EMERGENCY_NOT_CALLED` resta applicabile. Un eventuale timeout futuro (`-1`) è già trattato come "No". La scena presentava modifiche dell'utente ai prefab dei collider: salvate insieme alle nostre, nessun prefab modificato da OpenCode.

- **TASK 04.3 — Interazione VR con il telefono** (2026-11-25), eseguito dopo TASK 04.2 (`ManDownPhone`) e le precisazioni dell'utente su input e telefoni.
- **Richiesta:** rendere interagibili i telefoni in VR con il solo sistema condiviso SI81; configurare collider e componenti, collegare l'interazione a `ManDownPhone`, evitare interazioni ripetute o dopo chiamata già effettuata, mantenere modello e materiali. Decisioni ricevute: **GRIP/Grab**, **tutti e tre i telefoni**; dopo il 112 chiamato nessuno dei tre deve mostrare etichetta o scelta, ma resta prendibile/interagibile fisicamente.
- **Esito:** aggiunti a `ENV_Telefono_1/2/3` un `BoxCollider` non trigger, un `Interactable` condiviso (`canSelect=false`, `canGrab=true`, `followsHand=true`) e un `ManDownPhone`; `GrabEntered` ha un persistent listener `RuntimeOnly` verso il nuovo wrapper `RequestCallFromInteraction()`. Modello `cellulare` e materiale `Lit` invariati, transform dei telefoni invariata. Dettagli nella sezione "Operazioni completate".
- **Verifiche:** ricompilazione Unity completata con 0 errori (4 warning `CS0414` preesistenti); rilettura dei componenti nella scena salvata; verifica in Edit Mode del dispatcher serializzato attivando temporaneamente il listener (`EditorAndRuntime`) e ripristinando `RuntimeOnly`: 2 richieste, 1 `No`, 1 `Sì`, nessuna richiesta dopo la chiamata, doppio `Sì` ignorato, reset ripristina `NotMade`. Nessun Test Runner. **Da ora in poi i test in Play Mode sono eseguiti dall'utente** su richiesta del microtask.
- **Conferma sul grip NPC (utente):** è atteso che il GRIP sulla persona a terra non mostri effetto a schermo: `ManDownNpc` registra solo `CONSCIOUSNESS_CHECK` in `ManDownScenario.Actions`. La transform di `MESH_ShoulderGrip` non è stata toccata in questo task.
- **Limiti / demandati:** popup `AskDecision`, `Call112()`, obiettivo/HUD, etichette UI, scoring e prova su visore restano ai microtask successivi. La chiamata resta indipendente dalle altre azioni; senza 112 l'esito massimo resta successo parziale. TASK 01, 02, 03, 04.1 e 04.2 restano completati come registrato sotto.
- **Aggiornamento richiesto dall'utente dopo il test (2026-11-25, ancora dentro il filone TASK 04.3):** i telefoni restavano nella posizione di rilascio quando si usciva e si rientrava dalla Home, e non avevano gravità/collisioni. Aggiunti `Rigidbody` ai tre telefoni e `ResetPhone()` ripristina posa, rotazione, gravità e velocità agganciandosi a `MissionSession.Started`. Dettagli nel paragrafo dedicato sotto.
- **Raggio del raggio condiviso (verifica documentale):** `SharedInteraction.md` e `RayInteractor.cs` indicano **5 m** di default (`maxDistance`), non infinito. Il valore si cambia solo sui `RayInteractor` di `SCN_Core` (asset/scena condivisa): **non modificato**; serve autorizzazione esplicita/team per portarlo a 1 m.

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
- **Chiamata al 112 tramite popup decisionale comune (aggiornamento del 2026-11-25, indicazione del team UI, precisata dall'utente):** interagendo con il telefono il giocatore vede `AskDecision("Chiami il 112?", new[] { "Sì", "No" }, callback)`: indice 0 (`Sì`) → `Call112()`, unica via che registra la decisione (`CALL_112`); indice 1 (`No`) → il popup si chiude, **nessuna azione registrata** e il telefono resta interagibile per riaprirlo. Se la chiamata non viene mai effettuata, la penalità `EMERGENCY_NOT_CALLED` scatterà alla conclusione come da decisione esistente. Non è previsto un timeout del popup (`timeoutSeconds=0`); se in futuro ne venisse impostato uno, l'indice `-1` andrà trattato come `No`. Nessun tastierino numerico, Canvas aggiuntivo o UI personalizzata; `HUD_DecisionPanel` è compatibile con Operational (nessun filtro per livello in `ScenarioController`/`DecisionPanel`). Il timer del pannello, se impostato, è distinto dal timer della missione, che resta in corso. La scelta del telefono e del gesto di apertura è stata risolta in TASK 04.3: GRIP su tutti e tre i telefoni.
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
- **Non implementato in TASK 02:** rilevamento fisico RCP/DAE, componenti interattivi su NPC/telefoni/DAE, UI, audio, input XR e calcolo definitivo delle cinque dimensioni. TASK 03 ha poi aggiunto solo la coscienza dell'NPC.

### Verifica della coscienza (TASK 03, 2026-11-25)

- **File creati:** `Code/Scripts/ManDown/ManDownNpc.cs` (+ `.meta`); componente su `MESH_RiggedEmployee` con `[DisallowMultipleComponent]`.
- **File modificati:** `Code/Scripts/ManDown/ManDownScenario.cs` (costanti `ACTION_CONSCIOUSNESS_CHECK` e `CONSCIOUSNESS_RESULT`, rimozione avvio/riavvio timer); `Level/Scenes/SCN_ManDown.unity` (figlio `MESH_ShoulderGrip` e riferimenti); documenti `AGENTS.md`, `TASK_PROGRESS.md`, `Man_Down_Specifiche.md`.
- **Gesto approvato:** il giocatore avvicina il controller a una spalla e preme GRIP; nessuno spostamento fisico o scuotimento dell'NPC. `Interactable` ha `canSelect=false`, `canGrab=true`, `followsHand=false`; il `LabelAnchor` è di tipo `Interaction`, con titolo `Persona a terra`, dettaglio `Verifica la coscienza` e chiavi personalizzate vuote, quindi predefiniti comuni `GRIP` / `Afferra`.
- **Stato autorevole:** `ManDownNpc` non mantiene `assessed` locale; legge `scenario.IsConsciousnessChecked`. La sottoscrizione a `GrabEntered` avviene in `OnEnable()` e viene rimossa in `OnDisable()`, evitando duplicati e riferimenti alla partita precedente. `AssessConsciousness()` ignora duplicati, riferimenti nulli e azioni fuori missione.
- **Registrazione:** `VerifyConsciousness()` aggiunge una sola `ManDownActionRecord` con ID `CONSCIOUSNESS_CHECK`, `Completed=true`, `Success=true`, timestamp relativo all'inizio missione e `Note="NPC incosciente"`. L'alias `ManDownNpc.CONSCIOUSNESS_RESULT` punta alla costante del controller. Nessuna penalità numerica, `RecordChoice()` o calcolo definitivo.
- **Fase e HUD:** la fase interna passa a `BreathingAssessment`, ma `PhaseKeyFor(BreathingAssessment)` resta `PHASE_SCAN`; non vengono chiamati `CompleteObjective()` né avanzano obiettivo/counter HUD. La verifica resta una sottoazione dell'obiettivo 2.
- **Timer:** nessun `HUDService.StartTimer()`/`StopTimer()` nello scenario; `HUDRoot.BeginFromSession()` usa il timer comune con `mission.GetTimeLimit(...)`. `SO_Mission_ManDown` resta invariato (Operational 600 s, override 0); i 180 s approvati non sono stati applicati all'asset senza una decisione esplicita.
- **Scene e NPC:** `MESH_ShoulderGrip` è figlio di `MESH_RiggedEmployee` con posizione locale `(-0,700; 0,380; -0,450)` e `BoxCollider` non trigger di size `(0,320; 0,240; 0,340)`. L'NPC conserva 12 `SkinnedMeshRenderer`, posizione world `(-14,500; 0,321; 23,500)`, rotazione `270°` sull'asse X e bounds invariati.

### Analisi del telefono (TASK 04.1, 2026-11-25)

- **Fonti lette in questo microtask:** `AGENTS.md` (root e `Docs/Man_Down/`), `TASK_PROGRESS.md`, `Man_Down_Specifiche.md`, stato già registrato di `Man_Down_Scoring_System.md`, `README.md`, `Home/Docs/SharedInteraction.md`, `Home/Docs/SharedPlayerRig.md`, `Docs/UI_INTEGRATION_FOR_AI.md` del gruppo, `Docs/Guida_Design-Interactions.md`, `Code/Scripts/ManDown/ManDownScenario.cs`, stato già registrato di `Code/Scripts/ManDown/ManDownNpc.cs`, `Home/Code/Scripts/Runtime/Scenario/ScenarioController.cs` e `Home/UI/Data/Missions/SO_Mission_ManDown.asset`.
- **Stato di TASK 03 verificato:** il commit `98cde82` contiene `ManDownNpc.cs`, le costanti e `VerifyConsciousness()` in `ManDownScenario.cs`, il figlio `MESH_ShoulderGrip` in scena e i documenti aggiornati. Nessun altro file di codice o scenario risulta creato dopo quel commit. `git status` mostra però modifiche non documentate: vedi § "Problemi aperti".
- **Modello e gerarchia:** tre istanze `ENV_Telefono_1`, `ENV_Telefono_2`, `ENV_Telefono_3`, figlie dirette di `prefab_scena` (unico root). Sono istanze del modello condiviso `Assets/_Project/Shared/Art/Models/MESH_Telefono.fbx`: mesh `cellulare`, 1501 vertici / 982 tris, materiale `Lit` incorporato, scala locale 70 con rotazione base X 270°. In scena **non** è usato il prefab del gruppo `Level/Prefabs/Man_Down/MESH_Phone.prefab` (sorgente `Art/Models/Man_Down/MESH_Phone.fbx`, 364 tris, materiale `Salotto`): resta un'alternativa disponibile ma non utilizzata.
- **Posizioni e distanze (world, Editor):** `ENV_Telefono_1` `(-21,80; 0,77; 23,62)`, bounds `0,27 × 0,02 × 0,18 m`, distanza dall'NPC **7,318 m**; `ENV_Telefono_2` `(-19,40; 0,77; 20,69)`, bounds `0,28 × 0,02 × 0,20 m`, distanza **5,672 m** (il più vicino all'NPC); `ENV_Telefono_3` `(-21,77; 0,77; 20,65)`, bounds `0,26 × 0,02 × 0,15 m`, distanza **7,821 m**. L'NPC è a `(-14,50; 0,32; 23,50)`. I telefoni poggiano su una superficie intorno a y ≈ 0,73 m, più in basso del piano superiore dei desk `OfficeDeskCovered` (top ~1,225 m): da verificare visivamente prima di definire la zona di interazione.
- **Collider e componenti presenti:** ogni telefono ha solo `Transform`, `MeshFilter` e `MeshRenderer`. **Nessun Collider**, `Rigidbody`, `Interactable`, `LabelAnchor`, script o UnityEvent su nessuno dei tre. Al momento dell'analisi non esisteva `ManDownPhone.cs`: lo script è stato creato in TASK 04.2. Come già registrato, telefoni, DAE, scrivanie e prop non hanno collider di gameplay.
- **Come renderlo interagibile con il sistema SI81 condiviso:** il rig di `SCN_Core` fornisce `InputService` e i due `RayInteractor`; il telefono deve solo esporre collider e `Interactable` (`SharedInteraction.md`, § "Scenario usage"). Il pattern già verificato in TASK 03 su `MESH_ShoulderGrip` è: `BoxCollider` non trigger (il raycast usa `QueryTriggerInteraction.Ignore`), `Interactable` con `canGrab=true` e `followsHand=false` (e `canSelect` in base al gesto scelto), `LabelAnchor` di tipo `Interaction` con testi italiani, e un componente `ManDownPhone` che si sottoscrive a `SelectEntered` (trigger) o `GrabEntered` (grip) e **apre il popup comune**; la callback del popup chiama `ManDownScenario.Call112()` solo per `Sì`. Niente rig, input locale, raycast proprio o UI dedicata sul telefono. La scelta del telefono e del gesto è stata risolta in TASK 04.3: GRIP/Grab su tutti e tre i telefoni, coerentemente con la mappa condivisa (`Guida_Design-Interactions.md`) e con TASK 03.
- **Popup decisionale — verifica documentale (aggiornamento del 2026-11-25):** `GUIDA_INTEGRAZIONE_SCENARIO.md` (§4 tabella metodi e fig. § "Dove compare ogni cosa") e `UI_INTEGRATION_FOR_AI.md` (§5) documentano `AskDecision(domanda, opzioni[], callback, timeoutSeconds=0)`: pannello `HUD_DecisionPanel` world-locked a 1,2 m davanti alla testa, **2–4 opzioni** (~34 caratteri ciascuna, quindi "Sì"/"No" rientrano), scelta col raggio del controller, callback con indice 0-based o `-1` (`DecisionPanel.TIMEOUT_INDEX`) alla scadenza, ritorno `false` se il pannello manca in `MNG_HUD`. `DecisionPanel.cs` conferma che `Show()` non filtra per livello, che con `timeoutSeconds=0` non c'è barra né scaduta automatica e che il pannello resta fermo nel mondo; `HUDRoot.cs` collega solo la testa del rig. `ScenarioController.AskDecision()` non controlla `Level`: il popup è quindi **utilizzabile in Operational**, dove la tabella dei livelli (§6 guida) prevede conto alla rovescia 600 s e nessun suggerimento/ContextHelp. Il timer della missione non viene sospeso dall'apertura del popup (`FIGMA_SPECS.md`: "Timer keeps running only if mission says so"). La domanda e le due risposte sono testi italiani forniti dallo scenario, non chiavi di localizzazione nuove. Nessun tastierino numerico, Canvas, pannello o sistema UI del gruppo: si usa solo il kit condiviso. **Comportamento confermato dall'utente:** `No` chiude il popup senza registrare alcuna decisione e senza disabilitare il telefono, che può essere usato di nuovo; solo `Sì` registra la decisione tramite `Call112()`. Per ora `timeoutSeconds=0`, quindi non si verifica il caso `-1`.
- **Metodi già disponibili in `ManDownScenario` per la chiamata:**
  - `public const string ACTION_CALL_112 = "CALL_112"` (ID azione) e `public const string PENALTY_EMERGENCY_NOT_CALLED = "EMERGENCY_NOT_CALLED"`.
  - `public void Call112()`: se `AcceptsActions` (`initialized && !concluded`) e `!call112Made`, imposta il flag, registra `CALL_112` completata/riuscita con nota `null` e solleva `EmergencyCalled`; duplicati ignorati. **Non cambia fase, non chiama `CompleteObjective()`, non mostra UI, non simula l'arrivo dei soccorsi da sé**.
  - Stato ed eventi correlati: `IsCall112Made`, `EmergencyCalled`, `ActionRecorded`. In conclusione: se `!call112Made` aggiunge la penalità `EMERGENCY_NOT_CALLED`, il debrief include la riga dedicata, `rescue=false` (nessun arrivo simulato) e con punteggio ≥ 80 l'esito è comunque `Partial`; `CalculateProvisionalDimensions()` applica un malus di 15 alla dimensione Decision. I fallimenti automatici `TIMEOUT`, `CPR_OMITTED` e `LOW_SCORE` restano prioritari.
- **Obiettivi della missione relativi alla chiamata:** `SO_Mission_ManDown.asset` ha 4 obiettivi: indice 0 `Metti in sicurezza la scena`, 1 `Verifica coscienza e respiro`, 2 `Chiama il 112`, 3 `Usa il defibrillatore`. I tre criteri sono `Soccorsi chiamati e DAE usato subito`, `Soccorsi chiamati, DAE usato in ritardo` e `Soccorsi non chiamati in tempo`. Attenzione: `Call112()` **non** tocca l'HUD; `CompleteObjective()` nella classe base completa l'obiettivo **corrente** e passa al successivo (`ScenarioController.cs`), quindi per l'obiettivo 3 serve un collegamento esplicito (per esempio `SetObjective(2)` + `CompleteObjective()`) nel task UI, non in questo microtask.

### Script `ManDownPhone` (TASK 04.2, 2026-11-25)

- **File creato:** `Code/Scripts/ManDown/ManDownPhone.cs` (+ `.meta` generato da Unity), namespace `SI81.ManDown`, classe `sealed` con `[DisallowMultipleComponent]` e `[AddComponentMenu("SI81/Man Down/Phone")]`. Nessun altro file Unity creato o modificato; scena, prefab, materiali e script condivisi intatti.
- **Stato:** enum annidato `CallState { NotMade, Made }` con valore iniziale `NotMade`; proprietà `CurrentCallState` (sola lettura), `IsCallMade` e `IsAwaitingResponse`. Il flag di attesa impedisce richieste duplicate mentre una risposta è pendente.
- **Metodi pubblici:** `bool RequestInteraction()` emette l'evento `InteractionRequested` solo se non è già stata fatta la chiamata, non c'è una richiesta pendente ed esiste almeno un ascoltatore (finché il popup non è collegato la richiesta non viene emessa e lo stato resta invariato); `bool AnswerYes()` porta lo stato a `Made`, azzera l'attesa ed emette `CallConfirmed`; `bool AnswerNo()` azzera l'attesa ed emette `CallDeclined` senza cambiare stato, quindi il telefono resta riutilizzabile; `void ResetPhone()` riporta tutto a `NotMade` per la partita successiva. I tre metodi booleani restituiscono `false` quando l'azione non è applicabile (per esempio dopo una chiamata già registrata).
- **Eventi:** `InteractionRequested` (richiesta di aprire il popup), `CallConfirmed` (Sì), `CallDeclined` (No). `OnDestroy()` li azzera per non trattenere riferimenti a una partita precedente.
- **Indipendenza:** nessun `using` di `SI81.Input`, `SI81.UI` o `SI81.Interaction`; nessun riferimento a `AskDecision`, `Call112()`, `Interactable`, `LabelAnchor`, Canvas, scoring o alla scena. Il componente non sa chi aprirà il popup né chi registrerà la chiamata.
- **Non implementato di proposito:** popup, gesto di input, collider/componenti in scena, collegamento a `ManDownScenario`, aggiornamento obiettivo/HUD, audio e test funzionali: tutto demandato ai microtask successivi.

### Interazione VR sui telefoni (TASK 04.3, 2026-11-25)

- **Modifica a `ManDownPhone.cs`:** aggiunto il metodo pubblico `void RequestCallFromInteraction()` che delega a `RequestInteraction()`. Serve solo a essere collegato da un `UnityEvent` senza argomenti; non introduce dipendenze da `SI81.Input`, `SI81.UI` o `SI81.Interaction`.
- **Scena `SCN_ManDown.unity`:** su `ENV_Telefono_1`, `ENV_Telefono_2` e `ENV_Telefono_3` (figli di `prefab_scena`) aggiunti:
  - `BoxCollider` non trigger, dimensionato dalla bounds del renderer con margine 1,2× (world approx `0,42 × 0,03 × 0,36 m`, `0,47 × 0,03 × 0,43 m`, `0,34 × 0,03 × 0,22 m`). Non è trigger perché `RayInteractor` usa `QueryTriggerInteraction.Ignore`.
  - `Interactable` condiviso: `canSelect=false`, `canGrab=true`, `followsHand=true` (il telefono segue la mano durante la presa, come richiesto dall'utente: resta prendibile e interagibile fisicamente anche dopo la chiamata).
  - `ManDownPhone` con stato iniziale `NotMade`.
  - Persistent listener su `grabEntered` → `RequestCallFromInteraction`, in modalità `RuntimeOnly` (valore serializzato `m_CallState: 2`): non si attiva in Edit Mode, ma è attivo in Play Mode.
- **Input scelto:** GRIP/Grab, su indicazione esplicita dell'utente; il trigger resta riservato alla futura scelta nel popup. Nessun `OVRInput`, Input Action, raycast o interazione alternativa.
- **Invarianti:** mesh `cellulare` (1501 vertici / 982 tris) e materiale `Lit` invariati su tutti e tre; transform e scala dei telefoni invariate; transform di `MESH_ShoulderGrip` non toccata. Nessun `LabelAnchor` aggiunto.
- **Comportamento atteso dopo la chiamata:** `AnswerYes()` porta `ManDownPhone` a `Made`; `RequestInteraction()` rifiuta nuove richieste, quindi nessun telefono aprirà etichetta o scelta. I telefoni restano comunque afferrabili (`canGrab=true`, `followsHand=true`) perché non vengono disabilitati.
- **Non implementato:** popup `AskDecision`, `Call112()`, aggiornamento obiettivo/HUD, aggancio del reset al riavvio missione, scoring, audio e prova su visore.

### Fisica e reset dei telefoni (aggiornamento post-TASK 04.3, 2026-11-25)

- **`ManDownPhone.cs`:** ora dipende anche da `SI81.UI.MissionSession` per il solo evento `Started` (ciclo di vita della missione, non UI). In `Awake()`/`OnEnable()` cattura posa iniziale e `Rigidbody`; `OnEnable()` si sottoscrive a `MissionSession.Started`, `OnDisable()`/`OnDestroy()` si scollegano. `ResetPhone()` riporta stato a `NotMade`, ripristina posa/rotazione iniziali e, se c'è un `Rigidbody`, imposta `isKinematic=false`, `useGravity=true`, azzera velocità lineare/angolare.
- **Perché il reset serve anche senza ricaricare la scena:** `SceneMissionLauncher.ReturnToHome()` scarica lo scenario solo se `loadedMissionScene` è impostato (avvio dalla Home). Se si entra in Play direttamente da `SCN_ManDown` e si torna alla Home, `loadedMissionScene` resta vuoto e la scena non viene scaricata: i `Transform` modificati dalla presa resterebbero tali. L'evento statico `MissionSession.Started` viene emesso da `MissionSession.Begin()` a ogni avvio, quindi copre entrambi i percorsi.
- **Scena:** aggiunto un `Rigidbody` a ciascuno dei tre telefoni (`mass=0,2`, `useGravity=true`, `isKinematic=false`, interpolazione, collisioni discrete), sullo stesso GameObject del `BoxCollider`/`Interactable`, così `RayInteractor.BeginGrab()` lo rende cinematico e `ReleaseGrab()` ripristina gravità e velocità: al rilascio il telefono cade e rispetta i collider presenti.
- **Collider degli altri oggetti:** l'utente sta aggiungendo i collider alle scrivanie/arredi nei prefab del gruppo (`MESH_OfficeDeskCovered.prefab`, `MESH_OfficeDoor.prefab` modificati dall'utente): **non toccati** da OpenCode in questo aggiornamento.
- **Verifica Edit Mode:** spostando temporaneamente ogni telefono a `(0;10;0)` con rotazione e fisica alterate e chiamando `ResetPhone()`, posa/rotazione/gravità/velocità/stato tornano ai valori iniziali per tutti e tre. Nessun Play Mode avviato da OpenCode: la prova di caduta e urti resta all'utente.
- **Non eseguito:** modifica del raggio del raggio condiviso; vedi "Problemi aperti".

### Integrazione del popup decisionale (TASK 04.5, 2026-11-25)

- **File creato:** `Code/Scripts/ManDown/ManDownPhoneUi.cs` (+ `.meta`), namespace `SI81.ManDown`, classe `sealed` con `[DisallowMultipleComponent]` e `[AddComponentMenu("SI81/Man Down/Phone UI")]`. Costanti `QUESTION = "Chiami il 112?"` e `OPTIONS = { "Sì", "No" }`.
- **Dipendenze:** usa solo `ScenarioController.AskDecision`, `MissionSession.Started`, `LabelAnchor.Suppressed` e i metodi pubblici di `ManDownPhone`/`ManDownScenario`: nessun Canvas, popup, timer o input proprio, nessun scoring.
- **Scena:** su ciascun telefono aggiunti `ManDownPhoneUi` (riferimenti a `ManDownPhone`, `ManDownScenario` su `MNG_Scenario`, `LabelAnchor`) e `LabelAnchor` di tipo `Interaction` con titolo `Telefono`, dettaglio `Chiama il 112`, chiavi tasto/azione vuote → predefiniti comuni `GRIP` / `Afferra`, offset `(0; 0,25; 0)`.
- **Eventi serializzati:** `grabEntered` ha due listener (`ManDownPhone.RequestCallFromInteraction`, `ManDownPhoneUi.SuppressLabel`); `grabExited` ha `ManDownPhoneUi.ShowLabel`. Tutti `RuntimeOnly`, come da pratica del progetto.
- **Flusso:** interazione → `InteractionRequested` → `AskDecision(domanda, opzioni, callback, 0f)`; callback 0 → `AnswerYes()` e, solo se accettata, `Call112()`; callback 1 o `-1` → `AnswerNo()`. Se `AskDecision` restituisce `false` (pannello comune assente) si comporta come "No", così il telefono resta riutilizzabile. Dopo "Sì" l'etichetta resta soppressa e nuove richieste vengono ignorate da `ManDownPhone`.
- **Timer/Operational:** nessun timeout del pannello (`0f`): la UI comune non mostra la barra e non restituisce `-1`. Se scade il timer della missione, `ScenarioController.EndMission()` chiude il pannello tramite `DecisionPanel.Close()` senza callback: nessuna chiamata viene registrata e la penalità `EMERGENCY_NOT_CALLED` resta applicabile. `AskDecision` non ha filtri per livello (`ScenarioController.cs`, `DecisionPanel.cs`, `HUDRoot.cs`), quindi il popup è disponibile anche in **Operational**, dove la HUD mostra il conto alla rovescia e nessun suggerimento/ContextHelp.
- **Non implementato:** scoring, avanzamento obiettivo/HUD, audio, prova su Quest 3.

### Validazione Task 04 e controllo collider (TASK 04.6, 2026-11-25)

- **Stato dei 8 test richiesti:** nessuno è stato eseguito in Play Mode da OpenCode. Verifica statica/Edit Mode disponibile per tutti i punti, ma il risultato runtime resta **"da verificare manualmente"**: non vengono segnati come superati. Il Task 04 resta aperto finché l'utente non riporta l'esito dei test in Play Mode.
- **Controllo compilazione/riferimenti/Console:** `compilationFailed=false`, console `groundTruth.consoleErrors=0` (warning ambientali preesistenti). `MNG_Scenario` ha `SO_Mission_ManDown`; per i tre telefoni `ManDownPhoneUi` ha riferimenti validi a `ManDownPhone`, `ManDownScenario`, `LabelAnchor`, eventi `grabEntered` con `RequestCallFromInteraction` + `SuppressLabel` e `grabExited` con `ShowLabel`, etichetta `Interaction/Telefono` non soppressa e `ManDownPhone` in `NotMade` con `Rigidbody` presente.
- **Collider controllati (sola lettura):** 47 collider nella scena, tutti `isTrigger=false` e abilitati. Telefoni: un `BoxCollider` ciascuno (non duplicato) con `Interactable` + `Rigidbody`. Architettura: `Floor` con MeshCollider, pareti/soffitto con BoxCollider, porta, DAE, estintore, scrivanie (3 box ciascuna), sedie, piante, divano, armadio, scaffale, libreria, stampante, NPC e zona spalla. Nessun collider risulta mancante sui telefoni.
- **Segnalazioni sui collider (nessuna modifica applicata):**
  1. `Assets/_Project/Shared/Level/Prefabs/Office/MESH_Chair.prefab` risulta modificato: è un asset **Shared**, fuori dalla cartella autorizzata. Va confermato/risolto prima di qualunque commit.
  2. Alcuni collider aggiunti ai prefab hanno dimensioni locali molto grandi (per esempio `MESH_AED` `9,77 × 29,24 × 27,12` e `MESH_FireAlarm_Sprinkler` `1,66 × 3,98 × 4,04`) perché i modelli FBX usano scale d'import 100/0,02: in scena i bounds risultano plausibili, ma vanno verificati visivamente.
  3. `MESH_RiggedEmployee` ha ora un BoxCollider sul root: è non interattivo, quindi può bloccare il raggio verso un telefono se si frappone; da tenere presente nei test di puntamento.
  4. `TV_DA_UFFICIO_ONEDRIVE` e il vecchio `MESH_Sofa` non hanno collider: non serve al Task 04, ma segnalo l'assenza se prevedi interazioni fisiche anche su di essi.
- **Correzioni applicate in TASK 04.6:** nessuna. Le segnalazioni sopra riguardano scelte dell'utente o asset fuori dal Task 04.

### Aggiornamento etichetta e raggio (richiesta utente successiva al TASK 04.6)

- **Etichetta dopo il 112:** `ManDownPhoneUi` si iscrive a `ManDownScenario.EmergencyCalled` e `ApplyLabelState()` usa `scenario.IsCall112Made` (oltre a `phone.IsCallMade`). Dopo un "Sì" accettato, l'evento aggiorna i tre ponteggi e **nessuno dei tre telefoni mostra più l'etichetta**; il "No" resta invariato e il reset a nuova missione la ripristina. Verificato in Edit Mode: `Suppressed=true` sui tre dopo `EmergencyCalled`, `false` dopo `MissionSession.Started`.
- **Raggio di presa:** `RayInteractor.maxDistance` resta **5 m** per decisione esplicita dell'utente; nessun asset condiviso modificato. Non esiste un limite dedicato al solo GRAB nell'API condivisa.

---

## Operazioni rimanenti

1. **Attendere approvazione esplicita per TASK 04.6.** TASK 04.5 ha integrato il popup e le etichette; non iniziare autonomamente respirazione, RCP, DAE o scoring definitivo. La chiamata al 112 resta indipendente dalle altre azioni; la sua omissione comporta la penalità e limita l'esito a successo parziale.
2. **`ManDownNpc` (solo estensione futura):** oggi contiene la sola coscienza. Aggiungere poi respiro visivo e posa laterale via transform, zone configurabili per torace ed elettrodi, richiamando `VerifyBreathing()`, `PlaceSidePosition()` ed `Monitor()` senza modificare la logica del controller.
3. **`ManDownPhone` — completato nel TASK 04.5:** popup `AskDecision`, risposte, `Call112()`, `LabelAnchor`, reset e fisica sono collegati. Restano solo i test in Play Mode (popup reale, caduta, uscita/rientro, Operational) e l'aggiornamento obiettivo/HUD, che non fa parte di questo task. Niente tastierini, Canvas o UI personalizzata.
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
| `Code/Scripts/ManDown/ManDownScenario.cs` | Modificato: costanti `CONSCIOUSNESS_CHECK` / `NPC incosciente`, registrazione della verifica, rimozione di `StartTimer`/`StopTimer` duplicati | TASK 03 |
| `Code/Scripts/ManDown/ManDownNpc.cs` (+ `.meta`) | Creato: componente della sola coscienza, collegato all'`Interactable` della spalla e al controller | TASK 03 |
| `Level/Scenes/SCN_ManDown.unity` | Aggiornato: figlio `MESH_ShoulderGrip` con `BoxCollider`, `Interactable` e `LabelAnchor` condivisi; aggiunto e collegato `ManDownNpc` | TASK 03 |
| `Docs/Man_Down/Man_Down_Specifiche.md` | Aggiornata la riga della verifica coscienza con GRIP su spalla, `LabelAnchor` comune e risultato `NPC incosciente` | TASK 03 |
| `Docs/Man_Down/TASK_PROGRESS.md` | Aggiornato con l'analisi del telefono, i componenti individuati, i metodi/obiettivi collegati al 112, la decisione sul popup e le criticità; nessun asset Unity modificato | TASK 04.1 |
| `Docs/Man_Down/Man_Down_Specifiche.md` | Aggiornata la chiamata al 112 con il popup comune `AskDecision` ("Chiami il 112?" / "Sì" / "No"), la compatibilità Operational e il divieto di UI personalizzata | TASK 04.1 |
| `Code/Scripts/ManDown/ManDownPhone.cs` (+ `.meta`) | Creato: stato della chiamata, richiesta di interazione, risposte Sì/No e reset; nessuna UI, input o collegamento al controller | TASK 04.2 |
| `Docs/Man_Down/TASK_PROGRESS.md` | Aggiornato con metodi, eventi, verifiche di compilazione e operazioni rimandate | TASK 04.2 |
| `Code/Scripts/ManDown/ManDownPhone.cs` | Modificato: aggiunto il wrapper `RequestCallFromInteraction()` per il `UnityEvent` di `GrabEntered` | TASK 04.3 |
| `Level/Scenes/SCN_ManDown.unity` | Modificato: su `ENV_Telefono_1/2/3` aggiunti `BoxCollider`, `Interactable` GRIP, `ManDownPhone` e persistent listener verso la richiesta; modello/materiali/transform invariati | TASK 04.3 |
| `Docs/Man_Down/TASK_PROGRESS.md` | Aggiornato con configurazione VR, input usato, verifiche e checklist di test manuale | TASK 04.3 |
| `Code/Scripts/ManDown/ManDownPhone.cs` | Modificato: reset di posa/fisica/stato agganciato a `MissionSession.Started` | Aggiornamento post-TASK 04.3 |
| `Level/Scenes/SCN_ManDown.unity` | Modificato: `Rigidbody` sui tre telefoni per caduta e collisioni al rilascio; scena risalvata | Aggiornamento post-TASK 04.3 |
| `Docs/Man_Down/TASK_PROGRESS.md` | Aggiornato con fisica, reset e verifica documentale del raggio | Aggiornamento post-TASK 04.3 |
| `Code/Scripts/ManDown/ManDownPhoneUi.cs` (+ `.meta`) | Creato: ponte verso `AskDecision`, instradamento Sì/No/timeout e soppressione etichetta | TASK 04.5 |
| `Level/Scenes/SCN_ManDown.unity` | Modificato: `ManDownPhoneUi` e `LabelAnchor` Interaction sui tre telefoni, eventi di presa collegati; scena risalvata | TASK 04.5 |
| `Docs/Man_Down/TASK_PROGRESS.md` | Aggiornato con popup, etichetta, timer/Operational e verifiche | TASK 04.5 |

**Modifiche globali autorizzate:** solo `ProjectSettings/EditorBuildSettings.asset`. `SO_Mission_ManDown.asset` è stato riutilizzato senza modificarlo.
**Non modificati in TASK 02/TASK 03:** `Shared`, `Home`, altri gruppi, `Packages`, configurazioni globali, `SO_Mission_ManDown.asset` e `.gitignore`. Nessun commit o push. I documenti `!LLM_Migrazione_Rig_UI.md` e `SharedPlayerRig.md` (con i relativi `.meta`) restano intenzionalmente untracked.
**Nota risolta (storica):** Unity aveva riscritto automaticamente `Assets/_Project/Shared/Art/Materials/MAT_CabinetBody.mat` e `MAT_CoffeeTableBody.mat` durante il salvataggio; le modifiche non appartenevano al task e sono state ripristinate con `git checkout --` prima della chiusura di TASK 03.

**TASK 04.1:** nessun file Unity creato o modificato; aggiornati solo i documenti del gruppo. La transform di `MESH_ShoulderGrip` è stata **confermata dall'utente**: va conservata e non toccata. Restano in `git status` solo i due materiali condivisi riscritti da Unity, **non applicati né ripristinati** dal microtask e in attesa di una decisione esplicita.

**TASK 04.2:** creato solo `ManDownPhone.cs` (+ `.meta`); nessuna scena, prefab, materiale, script condiviso o impostazione globale modificato. La transform di `MESH_ShoulderGrip` non è stata toccata. Nessun commit o push.

**TASK 04.3:** modificati solo `ManDownPhone.cs` (wrapper void), `SCN_ManDown.unity` (tre telefoni) e questo documento. Nessun asset condiviso o di `Home` modificato intenzionalmente, nessun commit/push. La transform di `MESH_ShoulderGrip` non è stata toccata: il valore attuale di `m_LocalScale.z` (`0,6263806`) deriva dai test dell'utente e va conservato. Durante la sessione Play Mode avviata prima che l'utente richiedesse di delegargli i test, Unity ha riscritto `Assets/_Project/Home/Art/Materials/MAT_UIPointerReticle.mat` (`m_CustomRenderQueue` 3100 → 3000): file **non toccato** da questo microtask, da ripristinare solo con decisione esplicita.

**Aggiornamento post-TASK 04.3:** aggiunti solo `Rigidbody` ai tre telefoni in scena e il reset in `ManDownPhone.cs`; nessun asset condiviso, `Home`, prefab o impostazione globale modificato da OpenCode. L'utente stava modificando diversi prefab del gruppo per i collider degli arredi: file **non toccati** da OpenCode. Nessun commit o push.

**TASK 04.5:** creato solo `ManDownPhoneUi.cs` (+ `.meta`) e aggiunti componenti/riferimenti ai tre telefoni in `SCN_ManDown.unity`; nessun asset condiviso, `Home` o prefab modificato da OpenCode. L'utente risulta aver modificato anche `Assets/_Project/Shared/Level/Prefabs/Office/MESH_Chair.prefab`: fuori dalla cartella autorizzata e **non toccato** da OpenCode, da verificare prima di un commit.

---

## Decisioni tecniche

### Piano di implementazione aggiornato

Obiettivo: flusso completo e verificabile senza UI specifica, senza XRI alternativo, senza rig locale e senza input locale. La logica resta testabile da Inspector e da UnityEvent, anche senza visore; il player condiviso resta l'unico sistema di camera, locomozione e interazione.

```text
MNG_Scenario  →  ManDownScenario   (stato, random, validazione, esiti, ID, eventi; timer solo comune)
                   ├─ ManDownNpc    (TASK 03: sola coscienza via GRIP su spalla; respiro/posa e zone torace/elettrodi nel task dedicato)
                   ├─ ManDownPhone  (chiamata 112 simulata)
                   ├─ ManDownCpr    (solo rilevamento fisico delle compressioni)
                   └─ ManDownAed    (procedura DAE con elettrodi provvisori)
```

| Classe | Responsabilità | Non fa |
|---|---|---|
| `ManDownScenario` | Stato autorevole: coscienza, respirazione, disponibilità DAE, 112 chiamato, fasi RCP/DAE, tempi, cinque dimensioni grezze, ID di errore/fallimento, esito unico. Valida posizione/ampiezza/frequenza/rilascio e conteggi 20/20/30. | UI, input, fisica, animazioni complesse, rilevamento dei controller. |
| `ManDownNpc` | TASK 03: verifica della coscienza tramite GRIP su una spalla con `Interactable`/`LabelAnchor` condivisi. Da implementare: respiro visivo e posa laterale via transform; zone configurabili per torace e due elettrodi. | Decidere esito clinico, punteggio o validazione. |
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

- Niente Canvas/HUD/etichette/`LabelAnchor` né UI provvisoria prima del task dedicato. Eccezione autorizzata in TASK 03: solo il `LabelAnchor` comune di tipo `Interaction` su `MESH_ShoulderGrip`, con testi `Persona a terra` / `Verifica la coscienza` e chiavi predefinite `GRIP` / `Afferra`.
- Niente pericolo ambientale, pozzanghera, panno, danni o penalità Safety: Safety resta neutrale 5/5.
- Niente ragdoll: posa laterale e respiro visivo via transform.
- Solo modalità Realistica, 180 s; Guidata e Stress non implementate, parametri configurabili.
- RCP: 20 / 20 / 30 compressioni valide, 100–120 cpm, nessuna ventilazione, DAE non bloccato dalle 20 iniziali.
- DAE: elettrodi provvisori con snap, nessun cavo fisico; la logica non dipende dalla mesh.
- Mancata chiamata: nessun arrivo dei soccorsi, nessuna dichiarazione di fuori pericolo, esito massimo successo parziale.
- Chiamata al 112: popup comune `AskDecision("Chiami il 112?", ["Sì", "No"])`; niente tastierini numerici, Canvas o UI personalizzata; pannello disponibile anche in Operational, timer della missione non sospeso.
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
- **Nota TASK 03:** il timer attivo a 180 s e le due righe su `StartTimer`/`StopTimer` descrivono lo stato storico di TASK 02. In TASK 03 quei due avvii/fermi duplicati sono stati rimossi: resta solo il timer comune, con `SO_Mission_ManDown` invariato.

### TASK 03 — verifiche eseguite

- Ricompilazione Unity: `status=completed`, `failed=false`, `compilationFailed=false`, 0 errori e 4 warning `CS0414` preesistenti.
- Validatore scenario SI81: **0 errori, 9 avvisi** di naming preesistente; console dopo validazione `entries=[]`, `cursor=314`, `groundTruth.consoleErrors=0`.
- Play Mode da `SCN_ManDown` con harness temporaneo `si81_task03_playmode_test.cs`: **35/35 PASS**. Confermati un solo `ManDownScenario`, `ManDownNpc`, `Interactable`, `LabelAnchor` e `HUDRoot`; zero Canvas; etichetta e flag `Interactable` corretti; Scenario inizializzato e attivo.
- Azione unica `CONSCIOUSNESS_CHECK @ 35,63s` con `Completed=true`, `Success=true` e `Note="NPC incosciente"`. Duplicati ignorati; reset ripristina `IsConsciousnessChecked=false`, svuota le azioni e consente una nuova verifica.
- NPC invariato: 12 `SkinnedMeshRenderer`, world position `(-14,500; 0,321; 23,500)`, rotazione `270°` sull'asse X e bounds invariati. `MissionFlow.CurrentPhaseKey=hud.phase.scan`; obiettivo e contatore invariati, nessun `CompleteObjective()`.
- Console dopo Play Mode: `entries=[]`, `cursor=340`, `groundTruth.consoleErrors=0`. Play Mode fermato; `editor save_all` e `AssetDatabase.SaveAssets()` eseguiti.
- Timer comune in diagnostica direct Play Mode: `GetTimeLimit=0`, `timerRunning=False`, `countdown=False`, `remaining=0`, `elapsed=0`. È stato verificato solo che lo scenario non riavvii il timer, non l'avvio tramite Home.

### TASK 04.1 — verifiche eseguite (sola lettura)

- `git status --short`, `git diff --stat` e `git diff`: verificato il commit `98cde82` di TASK 03 e identificate le modifiche non documentate (dettagli in "Problemi aperti"). Nessun file è stato ripristinato.
- Ispezione Editor della scena `SCN_ManDown.unity` aperta e salvata (`isDirty:false`, un solo root `prefab_scena`): gerarchia, componenti, transform, bounds e distanze dei tre telefoni, dell'NPC, di `MESH_ShoulderGrip`, dei desk e dei marker; sorgente prefab dei telefoni verificata come modello condiviso `MESH_Telefono.fbx`.
- Lettura del codice: `ManDownScenario.cs` (metodi, costanti, eventi, penalità ed esiti legati al 112), `ScenarioController.cs` (`CompleteObjective()` completa l'obiettivo corrente), `SO_Mission_ManDown.asset` (obiettivi e criteri).
- Verifica documentale del popup (sola lettura): `GUIDA_INTEGRAZIONE_SCENARIO.md` §4/§5/§6, `UI_INTEGRATION_FOR_AI.md` §5/§6, `DecisionPanel.cs`, `HUDRoot.cs`, `MockScenario.cs` e `FIGMA_SPECS.md`. Confermato che `AskDecision` accetta 2 opzioni, restituisce indici 0/1 o `-1`, non è filtrato per livello e funziona in Operational; nessun test in Play Mode del popup è stato eseguito in TASK 04.1.
- **Non eseguiti in TASK 04.1:** compilazione, Play Mode, Test Runner e prova su Quest 3: nessuno script o asset è stato modificato. Nessuna modifica ad asset condivisi.

### TASK 04.2 — verifiche eseguite

- Ricompilazione Unity dopo la creazione di `ManDownPhone.cs`: `status=completed`, `failed=false`, `compilationFailed=false`, `consoleErrors=0`. I 4 warning `CS0414` sono preesistenti (3 in `ManDownScenario.cs`, 1 in `G_Ludovica/ForkliftChaosTestEnd.cs`) e non riguardano il nuovo script.
- Riflessione sul tipo compilato: `SI81.ManDown.ManDownPhone` deriva da `UnityEngine.MonoBehaviour`; esposti gli eventi `InteractionRequested`, `CallConfirmed`, `CallDeclined` e i metodi pubblici `RequestInteraction()`, `AnswerYes()`, `AnswerNo()`, `ResetPhone()` oltre alle proprietà di stato.
- `git status`: aggiunti solo `ManDownPhone.cs` e il relativo `.meta`; nessuna modifica ulteriore a scena, prefab, materiali o script condivisi.
- **Non eseguiti:** Play Mode, Test Runner, prova su Quest 3 e verifica funzionale della macchina a stati (richiesta → Sì/No → reset): il microtask richiedeva solo la compilazione e la logica non è ancora collegata a popup o scenario.

### TASK 04.3 — verifiche eseguite

- Ricompilazione Unity dopo il wrapper `RequestCallFromInteraction()`: `completed`, `failed=false`, `compilationFailed=false`, 0 errori; 4 warning `CS0414` preesistenti.
- Rilettura della scena salvata: per ogni telefono `BoxCollider` non trigger, `Interactable` (`select=false`, `grab=true`, `followsHand=true`), `ManDownPhone` in `NotMade`, un persistent listener `RequestCallFromInteraction` con `m_CallState=2` (`RuntimeOnly`), mesh `cellulare` e materiale `Lit` invariati.
- Verifica del dispatcher serializzato in **Edit Mode**: impostato temporaneamente `m_CallState=1` (`EditorAndRuntime`), invocato `grabEntered` con `Hand.Left` su ogni telefono con ascoltatori temporanei; risultati identici per i tre: 2 richieste ricevute, 1 `CallDeclined`, 1 `CallConfirmed`; dopo `AnswerYes()` nessuna ulteriore richiesta e doppio `AnswerYes()` rifiutato; `ResetPhone()` riporta `NotMade`. Stato ripristinato a `2` e scena risalvata.
- Console dopo la sessione: 0 errori; i warning MetaXR/OpenXR sono ambientali e preesistenti.
- **Non eseguiti da OpenCode:** Test Runner e prova su Meta Quest 3. Su indicazione dell'utente, da ora in poi i test in Play Mode vanno eseguiti dall'utente; la checklist è in "Prossimo passaggio".

### Aggiornamento post-TASK 04.3 — verifiche eseguite

- Ricompilazione dopo le modifiche a `ManDownPhone.cs`: `up_to_date`/`completed`, `compilationFailed=false`, console **0 errori**.
- Rilettura in Editor: i tre telefoni hanno `Rigidbody` (`mass=0,2`, `useGravity=true`, `isKinematic=false`) sullo stesso GameObject di `BoxCollider`/`Interactable`/`ManDownPhone`.
- Verifica Edit Mode del reset: dopo aver spostato ogni telefono a `(0;10;0)`, alterato rotazione, kinematicità, gravità e velocità, `ResetPhone()` ha ripristinato posa/rotazione iniziali, `isKinematic=false`, `useGravity=true`, velocità zero e `CallState.NotMade` per tutti e tre.
- Verifica documentale del raggio: `RayInteractor.maxDistance` è `5f` (non infinito) ed è esposto solo come getter pubblico `MaxDistance`; la modifica richiede l'Inspector dei due `RayInteractor` in `SCN_Core` (condiviso). Non eseguita.
- **Non eseguiti:** caduta/urti reali e uscita-rientro dalla Home in Play Mode: demandati all'utente.

### TASK 04.5 — verifiche eseguite

- Ricompilazione dopo la creazione di `ManDownPhoneUi.cs` e la configurazione della scena: `compilationFailed=false`, console **0 errori**; i warning restano ambientali/preesistenti.
- Rilettura della scena salvata: per ogni telefono `ManDownPhoneUi` con i tre riferimenti validi, `LabelAnchor` Interaction (`Telefono` / `Chiama il 112`, chiavi vuote, offset `(0;0,25;0)`), `grabEntered` con `RequestCallFromInteraction` e `SuppressLabel`, `grabExited` con `ShowLabel`, `callState=2` (`RuntimeOnly`).
- Verifica Edit Mode dell'instradamento (chiamata diretta al metodo privato `OnDecision` con 1, -1 e 0): `No` e `-1` lasciano `NotMade`, `Sì` porta a `Made` e sopprime l'etichetta, un secondo `AnswerYes()` viene rifiutato, `ResetPhone()` + `ShowLabel()` ripristinano tutto.
- Compatibilità Operational verificata dal codice: `AskDecision` non filtra per livello e `DecisionPanel` è disponibile a tutti i livelli; `timeoutSeconds=0` evita il `-1` e il timer missione non viene sospeso.
- **Non eseguiti:** apertura reale del popup in Play Mode, scelta con il raggio, scadenza del timer missione con popup aperto e prova su Quest 3: checklist per l'utente in "Prossimo passaggio".

### TASK 04.6 — verifiche eseguite

- Compilazione: `compilationFailed=false`, `consoleErrors=0`. Nessun nuovo errore dopo l'aggiunta dei collider da parte dell'utente e del ponte UI.
- Riferimenti Inspector riletti (vedi paragrafo "Validazione Task 04"): missione, telefoni, etichette, eventi e Rigidbody presenti e corretti.
- `git status --short` e `git diff --name-only` controllati: 15 file modificati e 6 file untracked rilevanti (vedi elenco per il commit in "Prossimo passaggio").
- **Tabella test TASK 04.6 (NON eseguiti in Play Mode da OpenCode):**
  | # | Test | Stato | Base della verifica disponibile |
  |---:|---|---|---|
  | 1 | Interazione telefono → popup | Da verificare manualmente | `GrabEntered` → `RequestCallFromInteraction` → `InteractionRequested` → `AskDecision`; Edit Mode verificato |
  | 2 | "No" permette nuova interazione | Da verificare manualmente | `OnDecision(1)` → `AnswerNo()` → `IsCallMade=false`; Edit Mode verificato |
  | 3 | "Sì" registra chiamata + timestamp | Da verificare manualmente | `Call112()` registra `CALL_112` con `Time.time - missionStartTime` (codice); `AnswerYes` verificato in Edit Mode |
  | 4 | Nessuna chiamata duplicata | Da verificare manualmente | guard `call112Made` in `Call112()` e `IsCallMade` in `RequestInteraction()`/`AnswerYes()`; doppio `AnswerYes` verificato in Edit Mode |
  | 5 | Omissione non blocca altre azioni | Da verificare manualmente | `Conclude()` applica solo penalità/limite d'esito, nessun ritorno anticipato (codice) |
  | 6 | Scadenza timer con popup aperto | Da verificare manualmente | `EndMission()` chiama `DecisionPanel.Close()` senza callback; nessun timeout interno (`timeoutSeconds=0`) |
  | 7 | Riavvio ripristina stato iniziale | Da verificare manualmente | `MissionSession.Started` → `ResetPhone()`; reset posa/fisica verificato in Edit Mode |
  | 8 | Etichetta coerente con l'interazione | Da verificare manualmente | `LabelAnchor` Interaction, GRIP predefinito, suppression in presa/dopo Sì verificati in Edit Mode |
- **Conclusione:** nessun test runtime è dichiarato superato; il Task 04 resta aperto in attesa dei risultati dell'utente.

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
4. `ManDownNpc` ha solo la coscienza (TASK 03); RCP e DAE non hanno ancora componenti interattivi: `ManDownCpr` e `ManDownAed` useranno `Interactable`/`RayInteractor` condivisi nei task dedicati. I telefoni sono invece interagibili dal TASK 04.3. **Conferma utente del 2026-11-25:** il GRIP sulla persona a terra non produce feedback visivo o sonoro ed è atteso che "non faccia niente" a schermo; internamente registra una sola `CONSCIOUSNESS_CHECK` in `ManDownScenario.Actions`, senza avanzare obiettivo o fase HUD.
5. Nessun audio per 112/DAE/feedback; la presentazione avverrà solo nel task UI.
6. NPC senza Animator/clip: il respiro visivo resta transform-based, come già deciso.
7. Il punteggio di TASK 02 è **provvisorio**: calcolato dalle azioni registrate, con la dimensione Time ancora a 0 in attesa dello scoring definitivo; `FinishMission(int)` esiste solo per test e per il futuro calcolo.
8. Modifiche auto-generate da Unity su due materiali condivisi (`MAT_CabinetBody.mat`, `MAT_CoffeeTableBody.mat`) erano state ripristinate con `git checkout --` prima della chiusura di TASK 03, ma **alla verifica di TASK 04.1 risultano di nuovo modificate** (stesse righe: keyword `_METALLICSPECGLOSSMAP`, passaggio `MOTIONVECTORS` e proprietà URP aggiunte). Non sono state toccate in questo microtask: ripristinarle richiede una decisione esplicita, perché il task vieta la modifica di asset condivisi.
9. **Transform di `MESH_ShoulderGrip` — risolta (conferma utente 2026-11-25):** nel commit `98cde82` aveva `m_LocalPosition (-0,700; 0,380; -0,450)` e `m_LocalScale (1;1;1)`; l'utente l'ha modificata durante i test e ha confermato di **non toccarla**. Valori attuali su disco: `m_LocalPosition (0,013; 1,415; 0,01)` e `m_LocalScale (2,0704; 1; 0,6263806)` (il componente Z era `0,48583` prima dei test), `BoxCollider` invariato (`size 0,320 × 0,240 × 0,340`, non trigger). Da conservare in TASK 04.5 e seguenti.
10. **Telefono — risolto (TASK 04.3):** i tre telefoni hanno collider, `Interactable` GRIP e `ManDownPhone`; la scelta del telefono e del gesto è confermata (tutti e tre, Grip). Resta da verificare sul visore l'inquadratura e la presa reale; il popup e `Call112()` sono stati integrati nel TASK 04.5.
12. **Materiale Home auto-modificato da Unity:** durante la sessione Play Mode sono cambiate le righe di `Assets/_Project/Home/Art/Materials/MAT_UIPointerReticle.mat` (`m_CustomRenderQueue` 3100 → 3000). Non è stato toccato da OpenCode e va ripristinato solo con decisione esplicita, perché fuori dalla cartella autorizzata.
13. **Raggio di presa — risolto (decisione utente 2026-11-25):** il raggio condiviso non è infinito ma ha `maxDistance=5 m` su ciascun `RayInteractor` di `SCN_Core` (`SharedInteraction.md`, `RayInteractor.cs`). L'API non ha un limite dedicato al GRAB. Dopo la verifica documentale l'utente ha scelto di **mantenere 5 m**: nessuna modifica a `SCN_Core` o ad asset condivisi.
14. **Collisioni della scena:** il telefono ha `BoxCollider` + `Rigidbody`, ma la caduta corretta dipende dai collider di pavimento/scrivanie/arredi che l'utente sta aggiungendo nei prefab del gruppo. Verificare che il pavimento (`MESH_OfficeStructure/Floor`, MeshCollider) e i desk abbiano collider prima di considerare valida la fisica.
15. **Popup e scadenza missione:** con `timeoutSeconds=0` il pannello decisionale non ha barra né callback `-1`; se il timer della missione scade mentre è aperto, `ScenarioController.EndMission()` lo chiude senza callback e nessuna chiamata viene registrata: comportamento previsto e già documentato, da verificare in Play Mode.
16. **Prefab condiviso modificato dall'utente:** `Assets/_Project/Shared/Level/Prefabs/Office/MESH_Chair.prefab` risulta modificato in `git status` insieme a diversi prefab del gruppo. OpenCode non l'ha toccato: va verificato/risolto prima di qualunque commit, perché le regole del progetto vietano modifiche unilaterali a `Shared`.
11. **Rifiuto esplicito della chiamata — risolto (conferma utente 2026-11-25):** con `No` (indice 1) il popup si chiude, **non viene registrata alcuna azione** e il telefono può essere usato di nuovo; solo `Sì` (indice 0) chiama `Call112()` e registra la decisione. Se la chiamata non avviene mai, la penalità `EMERGENCY_NOT_CALLED` resta quella già approvata e scatterà alla conclusione: non serve un ID aggiuntivo per il rifiuto. Nessun timeout del popup previsto per ora.

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

- **TASK 04.6 — Task 04 NON ancora completato.** Compilazione, riferimenti, Console, collider e logica statica sono verificati; gli 8 test runtime devono essere eseguiti e riportati dall'utente in Play Mode (checklist qui sotto). Dopo la conferma si potrà chiudere il Task 04 e passare al Task 05. Non avviare autonomamente respirazione, RCP, DAE, scoring o avanzamento obiettivo/HUD. La transform di `MESH_ShoulderGrip` non va toccata.
- **Checklist per l'utente (Play Mode):** (1) GRIP su un telefono → un solo popup "Chiami il 112?" con Sì/No ed etichetta GRIP/Afferra che sparisce in presa; (2) No → popup chiuso, nessuna azione registrata, nuova presa riapre il popup; (3) Sì → azione `CALL_112` con timestamp, nessun popup successivo, etichetta soppressa; (4) prova una seconda chiamata: nessun duplicato; (5) verifica che altre azioni restino eseguibili senza chiamata; (6) apri il popup e lascia scadere il timer missione → popup chiuso, penalità `EMERGENCY_NOT_CALLED`; (7) esci/rientra dalla Home → stato e posa iniziali, popup di nuovo disponibile; (8) ripeti in Operational e poi su Quest 3 per etichetta, raggio e presa reali.
- **File da includere nel prossimo commit (quando autorizzato):** nuovi `Code/Scripts/ManDown/ManDownPhone.cs` + `.meta`, `Code/Scripts/ManDown/ManDownPhoneUi.cs` + `.meta`; modificati `Docs/Man_Down/TASK_PROGRESS.md`, `Docs/Man_Down/Man_Down_Specifiche.md`, `Level/Scenes/SCN_ManDown.unity`, e i prefab del gruppo con i collider aggiunti dall'utente (`MESH_AED`, `MESH_Chair`, `MESH_Employee`, `MESH_FireAlarm_Sprinkler`, `MESH_FireExtinguisher`, `MESH_OfficeDeskCovered`, `MESH_OfficeDoor`, `MESH_RiggedEmployee`, tutti con i rispettivi `.meta` se generati). **Da NON includere senza decisione:** `Assets/_Project/Shared/Art/Materials/MAT_CabinetBody.mat`, `MAT_CoffeeTableBody.mat`, `Assets/_Project/Shared/Level/Prefabs/Office/MESH_Chair.prefab`, `Assets/_Project/Home/Art/Materials/MAT_UIPointerReticle.mat` (fuori dal gruppo/auto-generati). I documenti `!LLM_Migrazione_Rig_UI.md` e `SharedPlayerRig.md` restano intenzionalmente untracked. Nessun commit o push senza autorizzazione.
- **Test in Play Mode delegati all'utente per TASK 04.5:** (1) avvia `SCN_ManDown` e verifica un solo `SCN_Core`/rig/UI; (2) punta un telefono: deve comparire l'etichetta Interaction `Telefono` / `Chiama il 112` con predefiniti `GRIP` / `Afferra`, e sparire durante la presa; (3) premi GRIP: deve apparire **un solo** popup `Chiami il 112?` con `Sì` e `No`; (4) scegli `No`: il popup si chiude, nessuna azione viene registrata e una nuova presa riapre il popup; (5) scegli `Sì`: `CALL_112` viene registrata una sola volta, non compaiono più popup/etichette sui tre telefoni e i telefoni restano afferrabili; (6) ripeti in **Operational** (con conto alla rovescia): il popup deve funzionare e il timer della missione deve continuare; (7) se scade il timer mentre il popup è aperto, deve chiudersi senza chiamata e con penalità `EMERGENCY_NOT_CALLED`; (8) esci e rientra dalla Home: telefoni nella posa originale e popup disponibile di nuovo.
- **Test TASK 04.3 (storici, superati dal TASK 04.5):** i controlli su collider, GRIP, assenza di scelta e invariabilità del modello sono stati assorbiti dalla checklist TASK 04.5 sopra. OpenCode non avvia Play Mode: i test restano delegati all'utente.
- **Test fisica/reset delegati all'utente:** (1) afferrare un telefono, sollevarlo e rilasciarlo sopra il pavimento o una scrivania con collider: deve cadere e fermarsi senza attraversare oggetti; (2) ripetere su tutti e tre; (3) spostare i telefoni, uscire dalla missione verso la Home e rientrare: devono tornare alla posa originale e con `CallState.NotMade`; (4) verificare che dopo il rientro non restino velocità residue. Se la caduta non avviene, controlla i collider dei desk/pavimento che stai aggiungendo.
- **Raggio risolto:** l'utente ha confermato di mantenere il `maxDistance` condiviso a 5 m; non ci sono modifiche a `SCN_Core` da approvare.
- Completare la procedura manuale su Meta Quest 3 riportata sotto e segnalare l'esito prima di considerare validata l'integrazione hardware del rig (TASK 01).
- Quando richiesto, implementare un solo componente alla volta, collegandolo alle API già esistenti del controller senza modificarne la logica di stato; verificare compilazione, riferimenti e test prima di aggiornare i documenti.
- I dati didattici mancanti elencati sopra restano configurabili e non vanno inventati.
- **Nessun commit/push/merge** e **nessuna modifica a `.gitignore` senza nuova autorizzazione**; le modifiche auto-generate da Unity sui due materiali condivisi e su `MAT_UIPointerReticle` risultano ancora presenti in `git status` e non sono state toccate (vedi "Problemi aperti").

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
10. Verificare la coscienza (TASK 03): avvicinare il controller a una spalla dell'NPC, controllare l'etichetta comune `Persona a terra` / `Verifica la coscienza` con `GRIP` / `Afferra`, premere GRIP e confermare che l'azione `CONSCIOUSNESS_CHECK` venga registrata una sola volta con `NPC incosciente`, senza avanzare obiettivo o fase HUD. Ripetere il GRIP: non deve registrare duplicati. Dopo un reset, la verifica deve essere di nuovo disponibile.
11. Registrare esito, eventuali anomalie e data della prova in questo file: finché non viene eseguita, il rig e la coscienza risultano validati **solo in Editor/Play Mode**, non su hardware.
12. Il seriale `2G0YC5ZG1402NV` citato nei documenti è solo riferimento di connessione del dispositivo, **non** prova di avvenuta validazione.
