# TASK_PROGRESS — Man Down (G_Marius)

- **Data:** 2026-11-25
- **Stato:** otto decisioni aperte e decisioni integrative **approvate e documentate**; gameplay **non implementato**. Rig temporaneo **rinviato**: l'utente lo inserirà a breve.
- **Regole operative:** `Docs/Man_Down/AGENTS.md` §0, §0.1 e §0.2. Leggere `AGENTS.md` e questo file all'inizio di ogni task e dopo ogni interruzione/compattazione.
- **Priorità:** le decisioni approvate dall'utente prevalgono su GDD specifico, flowchart e GDD generale in caso di incongruenze.

---

## Roadmap dei task

| # | Task | Stato | Note |
|---:|---|---|---|
| 1 | Preparazione e documentazione | **Completata** | TASK 00, 00b, 00c, 00d, 00e; analisi, direttive e decisioni approvate registrate. |
| 2 | Player rig temporaneo | **Rinviato** | L'utente inserirà il rig a breve. OpenCode non lo ha creato e non deve crearlo ora. Resta fuori dalla scena finale e dal repository. |
| 3 | Controller della missione | Non avviato | `ManDownScenario` come `MonoBehaviour`: stato autorevole, random configurabile, timer 180 s, API pubblica ed eventi. |
| 4 | Controllo della coscienza | Non avviato | `VerifyConsciousness()` + evento; nessuna UI. |
| 5 | Telefono e chiamata al 112 | Non avviato | `Call112()`; omissione = `EMERGENCY_NOT_CALLED`, esito massimo successo parziale. |
| 6 | Controllo della respirazione | Non avviato | Anormale 70% / normale 30%, configurabile e forzabile; indizio visivo transform-based. |
| 7 | Posizione laterale e monitoraggio | Non avviato | `PlaceSidePosition()` e `Monitor()` nel ramo normale. |
| 8 | Meccanica RCP | Non avviato | Rilevamento controller separato dalla valutazione; 20 / 20 / 30 compressioni valide, 100–120 cpm. |
| 9 | Meccanica DAE | Non avviato | DAE 75% / 25%; elettrodi provvisori con snap; ripresa RCP; nessun cavo simulato. |
| 10 | Scoring ed esiti | Non avviato | Soglie 80 / 50; ID di fallimento ed errori; cinque dimensioni grezze; SPI comune invariato. |
| 11 | Test completo del gameplay | Non avviato | Play Mode senza visore; poi prova su Quest 3 con il rig inserito dall'utente. |
| 12 | Integrazione UI SI81 | Non avviato | Solo quando il kit sarà disponibile; nessuna UI prima del task dedicato. |
| 13 | Validazione finale | Non avviato | Verifica su visore e distinzione tra test eseguiti e test manuali. |

---

## Task corrente

- **Task 00e — Registrazione delle decisioni gameplay approvate** (2026-11-25).
- **Richiesta:** aggiornare `TASK_PROGRESS.md`, specifiche, scoring e `AGENTS.md` con le otto decisione approvate e con le decisioni integrative definitive; aggiornare il piano delle classi; non implementare ancora gli script.
- **Esito:** documentazione aggiornata; **nessun gameplay implementato**, nessuna scena/prefab/script/configurazione toccata.
- **Rig:** l'utente ha chiarito che `SharedPlayerRig.md` descrive il rig finale da implementare a fine progetto. Per ora il rig temporaneo è rinviato e sarà inserito dall'utente: non è stato creato alcun sistema alternativo e non è stata introdotta alcuna dipendenza permanente.
- **Perimetro rispettato:** sola modifica di file Markdown in `Docs/Man_Down/`.
- **Non toccati:** `SCN_Man_Down.unity`, prefab, materiali, texture, script, Build Settings, `Shared`, `Home`, altri gruppi, `Packages`, `ProjectSettings`, `.gitignore`.
- Le decisioni di gameplay elencate nel §5 non sono più aperte: sono state approvate e accommodate nei documenti.

---

## Operazioni completate

### Analisi documentale e ispezione reale (TASK 00 / TASK 01)

- Fonti lette: `AGENTS.md`, specifiche, scoring, GDD specifico e generale, flowchart, guide UI, guida interazioni, `Docs/SharedPlayerRig.md`, `Packages/manifest.json`.
- Ispezione Unity MCP della scena attiva: un solo root `/prefab_scena`, nessuna Camera/rig/UI/script; collider solo su `MESH_OfficeStructure`.
- Pacchetti verificati: Meta XR Core `209.0.0`, Input System `1.19.0`, XR Management `4.7.0`, OpenXR `1.16.1`, URP `17.3.0`, Test Framework `1.6.0`; **XRI non installato**.
- Verificato che il rig documentato in `SharedPlayerRig.md` non è presente nel progetto (`MNG_CoreRig.prefab`, assembly `SI81.*`, `InputService`, `RigRegistration`, `Interactable`, `SharedInteraction.md`, `SCN_Core` assenti).
- Nessuna modifica alla scena o ad asset Unity in TASK 00 e TASK 01.

### Decisioni approvate (Task 00e)

1. **Probabilità iniziali.** All'avvio si randomizzano in modo indipendente respirazione dell'NPC e disponibilità del DAE: respirazione anormale **70%** / normale **30%**; DAE presente **75%** / assente **25%**. I valori sono configurabili dall'Inspector. È prevista la forzatura manuale di entrambe le condizioni per testare tutte le combinazioni senza dipendere dalla randomizzazione.
2. **Meccanica RCP.** Basata sul movimento fisico dei controller VR, non su pulsazioni ripetute di un pulsante. Zona dedicata sul torace; due soglie distinte per compressione e rilascio; nuova compressione solo dopo il rilascio della precedente; valutazione di posizione, ampiezza virtuale e frequenza; progressione da numero di compressioni valide + frequenza adeguata; registrazione delle compressioni errate e della qualità complessiva; parametri e soglie configurabili e sottoposti a validazione didattica. Il rilevamento fisico è separato dalla logica di valutazione. Non si simula la forza fisica sul torace.
   - **Integrazione definitiva:** RCP iniziale con DAE **20** compressioni valide; RCP dopo defibrillazione **20**; RCP senza DAE **30** complessive; frequenza di riferimento **100–120 cpm**; valutare anche il rilascio; **nessuna ventilazione**; il DAE può essere usato tempestivamente anche prima di completare le 20 iniziali. I numeri sono soglie di progressione gameplay, non criteri clinici di rianimazione.
3. **Meccanica DAE.** Procedura interattiva: recupero, attivazione, posizionamento manuale dei due elettrodi nelle zone dedicate, snap automatico degli elettrodi corretti, analisi simulata e defibrillazione quando indicata, ripresa della RCP. Gli errori sono recuperabili ma registrati per lo scoring. Non esiste una mesh con elettrodi separati: si userà il modello disponibile più due elettrodi provvisori realizzati con primitive Unity appiattite e materiali semplici. La sostituzione futura delle mesh non deve richiedere modifiche alla logica. Nessun cavo fisico, simulazione di cavi o animazione complessa.
4. **Timeout e arrivo dei soccorsi.** Sistema ibrido: arrivo simulato dopo il completamento delle procedure fondamentali e di una breve fase finale di assistenza, senza attese passive prolungate. Percorsi: respirazione normale → assistenza e monitoraggio; anormale senza DAE → RCP; anormale con DAE → RCP, DAE, ripresa RCP. **Solo modalità Realistica**, timeout **180 s**; Guidata e Stress non si implementano ora, ma i parametri restano configurabili. Scadenza = fallimento automatico `TIMEOUT`, ma tutte le azioni eseguite vengono comunque valutate. Non è obbligatorio completare tutte le azioni corrette per concludere la missione. La mancata chiamata al 112 non impedisce la conclusione: non si simula un arrivo attribuito a una chiamata mai effettuata e non si dichiara l'NPC fuori pericolo; l'esito massimo è successo parziale.
5. **Scoring ed esiti.** Successo `>= 80`; successo parziale `>= 50` e `< 80`; fallimento `< 50` (`LOW_SCORE`). Fallimenti automatici, indipendenti dal punteggio: RCP completamente omessa quando necessaria (`CPR_OMITTED`) e scadenza del timer (`TIMEOUT`). Penalizzazioni senza fallimento automatico: `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE`. RCP omessa e RCP tentata male restano distinte. Si mantengono le cinque dimensioni: Safety 5%, Awareness 10%, Decision 30%, Execution 30%, Time 25%; Safety resta neutra 5/5 fino a rischi ambientali valutabili (oggi esclusi). Non si modifica lo scoring condiviso né lo SPI comune. La scelta finale tra soglie sul punteggio ponderato di Man Down o sullo SPI comune è rinviata all'integrazione finale. La mancata chiamata limita l'esito a successo parziale senza alterare artificialmente il punteggio; i fallimenti automatici mantengono la priorità.
6. **Primo soccorso aziendale.** Meccanica esclusa completamente. Unica comunicazione richiesta: chiamata simulata al 112. Nessuna chiamata aggiuntiva agli addetti aziendali, nessuna attivazione automatica, nessun NPC dedicato, nessuna penalità relativa. La decisione sostituisce il GDD specifico.
7. **Sorgente dei trigger.** La logica dello scenario resta indipendente dal player rig e dal sistema di interazione VR. Gli script espongono metodi pubblici ed eventi utilizzabili da componenti intermedi. Si separano: rilevamento fisico delle interazioni, logica dello scenario, validazione delle azioni, scoring, presentazione e feedback UI. La logica deve essere testabile anche senza visore. Nessuna dipendenza permanente dal rig temporaneo.
8. **Identificativi interni.** Fallimento: `CPR_OMITTED`, `TIMEOUT`, `LOW_SCORE`. Errori/omissioni con penalizzazione: `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE`. Tutte le cause di fallimento presenti vengono registrate, ma la missione viene conclusa una sola volta. Gli ID sono indipendenti dalle descrizioni mostrate al giocatore, che saranno in italiano e gestite in fase di integrazione UI.

### Decisioni integrative definitive

- Si sviluppa **esclusivamente la modalità Realistica** con timeout 180 s; Guidata e Stress non vanno implementate ora, ma i parametri restano configurabili per future estensioni.
- RCP: 20 compressioni valide iniziali con DAE, 20 dopo defibrillazione, 30 senza DAE; 100–120 cpm; posizione, ampiezza, frequenza e rilascio valutati; nessuna ventilazione; il DAE non viene bloccato dalle 20 iniziali; tutto configurabile dall'Inspector.
- Mancata chiamata al 112: penalizzazione già definita, impossibilità di successo completo, esito massimo successo parziale, nessun arrivo simulato dei soccorsi, nessuna dichiarazione che l'NPC sia fuori pericolo, nessuna alterazione artificiale del punteggio; i fallimenti automatici conservano la priorità.
- Queste decisioni sostituiscono qualunque indicazione precedente incompatibile nei PDF e nei documenti del gruppo.

---

## Operazioni rimanenti

1. **Task 2 — Player rig temporaneo:** rinviato; l'utente lo inserirà a breve. OpenCode non deve crearlo autonomamente né introdurre dipendenze permanenti dal gameplay.
2. **Task 3 — Controller della missione:** creare `ManDownScenario` con stato autorevole, randomizzazione configurabile/forzabile, timer 180 s, API pubblica ed eventi; senza UI, input o XR.
3. **Task 4 — Controllo della coscienza:** `VerifyConsciousness()` + evento; registrazione dello stato.
4. **Task 5 — Telefono e chiamata al 112:** `Call112()`; omissione registrata come `EMERGENCY_NOT_CALLED`; nessun arrivo simulato se non è stata chiamata.
5. **Task 6 — Controllo della respirazione:** `VerifyBreathing()` con esito 70/30 configurabile e forzabile; indizio visivo transform-based.
6. **Task 7 — Posizione laterale e monitoraggio:** `PlaceSidePosition()` e `Monitor()` per il ramo normale; fase finale di assistenza breve e configurabile.
7. **Task 8 — Meccanica RCP:** rilevamento fisico in `ManDownCpr`; validazione in `ManDownScenario`; 20 / 20 / 30 compressioni, 100–120 cpm, due soglie, riarmo al rilascio, errori e qualità.
8. **Task 9 — Meccanica DAE:** `ManDownAed` con elettrodi provvisori, snap, analisi simulata, defibrillazione e ripresa RCP; errori recuperabili ma registrati.
9. **Task 10 — Scoring ed esiti:** cinque dimensioni grezze 0–100; soglie 80/50; ID di fallimento ed errori; missione conclusa una sola volta; SPI comune invariato.
10. **Task 11 — Test completo del gameplay:** compilazione, riferimenti Inspector, Play Mode senza visore; poi prova su Meta Quest 3 con il rig inserito dall'utente.
11. **Task 12 — Integrazione UI SI81:** sostituire la base di `ManDownScenario` con `ScenarioController`, aggiungere le chiamate UI una sola volta, senza riscrivere le interazioni.
12. **Task 13 — Validazione finale:** verifiche su visore e distinzione documentata tra test eseguiti e test manuali residui.
13. **Prime operazioni concrete del Task 3:** creare i cinque script in `Code/Scripts/ManDown/` (solo `MonoBehaviour`, nessun `using SI81.UI`); aggiungere i componenti agli oggetti esistenti sotto `prefab_scena` senza creare nuovi root; verificare il flusso in Play Mode tramite UnityEvent di test nell'Inspector.

---

## File creati o modificati

| File | Operazione | Fase |
|---|---|---|
| `Docs/Man_Down/TASK_PROGRESS.md` (e relativo `.meta` generato da Unity) | Creato, poi aggiornato in Task 00b, 00c, 00d, TASK 01 e Task 00e | TASK 00 / 00b / 00c / 00d / 01 / 00e |
| `Docs/Man_Down/AGENTS.md` | Modificato: §0, §0.1, §0.2, §3.9, §7, §10 | TASK 00 / 00b / 00c / 00d / 01 / 00e |
| `Docs/Man_Down/Man_Down_Specifiche.md` | Riscritto per coerenza con le decisioni approvate | Task 00e |
| `Docs/Man_Down/Man_Down_Scoring_System.md` | Aggiornato: soglie, ID, RCP/DAE, esiti e limiti di integrazione | Task 00e |

**Nessuna modifica a:** `Level/Scenes/SCN_Man_Down.unity`, prefab, materiali, texture, script, Build Settings, `Shared`, `Home`, altri gruppi, `Packages`, `ProjectSettings` o configurazioni XR. Nessun commit o push.

---

## Decisioni tecniche

### Piano di implementazione aggiornato

Obiettivo: flusso completo e verificabile senza UI, senza XRI, senza rig e senza input locali. La logica resta testabile da Inspector e da UnityEvent, anche senza visore.

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

**API pubblica di `ManDownScenario`** (metodi richiamabili da UnityEvent o da un futuro `ScenarioTrigger`; nomi adattati alle convenzioni del progetto): `BeginMission()`, `VerifyConsciousness()`, `Call112()`, `VerifyBreathing()`, `StartCpr()`, `RegisterCprCompression()`, `RegisterCprFault()`, `ActivateAed()`, `PlaceElectrode(int electrodeIndex)`, `ConfirmAedInstruction()`, `PlaceSidePosition()`, `Monitor()`, `FinishMission()`. Se l'integrazione richiederà metodi senza parametri, si aggiungeranno wrapper dedicati per i due elettrodi senza modificare la logica. Eventi C# pubblici corrispondono a ogni azione registrata (coscienza, chiamata, respirazione, compressione, errore RCP, attivazione DAE, elettrodo, istruzione DAE, posa, monitoraggio, fine missione).

In integrazione cambierà solo la classe base in `ScenarioController` e verranno aggiunte lì le chiamate a `SetObjective`, `CompleteObjective`, `SetPhase`, `Alert`, `Feedback`, `Subtitle`, `AskDecision` e `EndMission` (una sola volta, con `-1` e le cinque dimensioni normalizzate). I componenti di interazione resteranno invariati.

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
- Il rig temporaneo sarà inserito dall'utente: nessuna dipendenza permanente e nessun rig creato da OpenCode.
- Il punteggio ponderato di `Man_Down_Scoring_System.md` resta un modello locale: conservare i cinque valori grezzi 0–100 senza falsarli per la UI comune.

### Regole di lavoro

Sintesi in cima a questo file; elenco completo in `AGENTS.md` §0.1 e §0.2. In particolare: un solo task alla volta, solo file di `G_Marius`, riutilizzo dell'esistente, Unity MCP per la scena, aggiornamento dei documenti a ogni fase, nessun commit/push senza autorizzazione.

---

## Test effettuati

**Realmente eseguiti (solo documentali / Editor):**

- Rilettura integrale di `AGENTS.md`, `TASK_PROGRESS.md`, specifiche e scoring prima di applicare le decisioni.
- Verifica del percorso effettivo dei quattro file Markdown aggiornati.
- Ricerca delle residue indicazioni incompatibili dopo l'aggiornamento (primo soccorso aziendale, 50/50, modalità multiple, vecchi ID, vecchie soglie).
- Nessun Test Runner o Play Mode avviato: nessuno script di gameplay creato.

**Non eseguiti / ancora da effettuare:**

- Compilazione dei futuri script e controllo dei riferimenti nell'Inspector.
- Play Mode, prove funzionali della logica senza visore.
- Prova su Meta Quest 3 con il rig che l'utente inserirà.

---

## Problemi aperti

### Impedimenti verificati (non sono decisioni di gameplay)

1. Kit `SI81.UI` assente: impossibile compilare/ereditare `ScenarioController` o aggiungere `LabelAnchor` ora.
2. Nessun sistema di interazione condiviso approvato: NPC, telefoni e DAE non hanno collider né componenti interattivi; `ScenarioTrigger` non è disponibile.
3. Rig temporaneo non ancora inserito: l'utente lo porterà a breve; la logica può essere sviluppata e testata senza di esso.
4. Nessun audio per 112/DAE/feedback; la presentazione avverrà solo nel task UI.
5. NPC senza Animator/clip: il respiro visivo resta transform-based, come già deciso.

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

- **Attendere il prossimo task.** Non avviare autonomamente i task della roadmap.
- L'utente inserirà a breve il rig temporaneo: OpenCode non lo crea e non deve modificarlo senza richiesta esplicita.
- Se verrà richiesto il Task 3 (Controller della missione): creare `ManDownScenario` e gli altri componenti solo se pertinenti, senza UI/input/XR; verificare compilazione e riferimenti, aggiornare poi questo file e `AGENTS.md` distinguendo test eseguiti e test manuali.
- I dati didattici mancanti elencati sopra restano configurabili e non vanno inventati: serviranno per la validazione, non per scrivere la struttura delle classi.
- **Nessun commit/push** e **nessuna modifica a `.gitignore`**.

### Come testare sul Quest 3 (procedura manuale, non ancora eseguita)

Vale quando il rig sarà stato inserito dall'utente e la scena di test sarà disponibile. Non è una prova già effettuata.

1. Verificare che ci sia **esattamente un rig e una camera** nella scena che carica il rig e nessun rig nella scena di scenario.
2. Controllare altezza del pavimento, tracciamento di testa e controller, modelli dei controller e raggio di mira.
3. Stick sinistro: movimento relativo alla direzione dello sguardo; stick destro: snap turn di 45°; nessuna deriva a riposo e velocità limitata.
4. Menu sinistro: apre/chiude la pausa e il movimento si ferma; B/Y inerti durante la missione e attivi solo nel menu.
5. Trigger: selezione UI senza selezionare l'oggetto di mondo dietro; grip per afferrare; verificare l'interazione sugli oggetti di Man Down.
6. Haptics: impulsi su menu/pause/avvisi/interazione e disattivazione con `controls.ui_haptics`.
7. Controllare i log con Meta Quest Developer Hub / logcat: niente duplicati di `InputService`, bridge mancanti o errori del rig; le righe `[SI81 Input]` devono riportare controller, pose del raggio e oggetto colpito.
8. Il seriale `2G0YC5ZG1402NV` citato nel documento è solo prova di connessione del dispositivo, **non** di avvenuta validazione.
