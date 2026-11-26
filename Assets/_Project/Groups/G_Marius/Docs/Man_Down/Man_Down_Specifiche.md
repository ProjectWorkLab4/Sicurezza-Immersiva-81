# Sicurezza Immersiva 81 — Specifiche di sviluppo: Man Down

> **Documento di riferimento per OpenCode e per il gruppo di sviluppo.**  
> Scenario 2 — **Man Down (ID 3)** · Unity / C# · VR · OpenXR · target di test: Meta Quest 3.  
> **Stato:** specifica funzionale aggiornata alle decisioni approvate dall'utente (2026-11-24/25), **non** attestazione di funzionalità già implementate.

## 1. Fonti, ambito e priorità

Questo documento consolida tre documenti di progetto:

1. **`Man_Down_GDD.pdf`** — GDD specifico dello scenario, fonte primaria per le meccaniche e gli esiti di Man Down.
2. **`FlowChart_ManDown.pdf`** — diagramma dei percorsi decisionali, inclusi il pericolo ambientale e i quattro Bad Ending.
3. **`Project Work VR - GDD.pdf`** — GDD generale di *Sicurezza Immersiva 81*, fonte per difficoltà, interazioni VR, punteggi, accessibilità e sistemi comuni.

**Le decisioni approvate dall'utente e registrate in `TASK_PROGRESS.md` (§ "Decisioni approvate" e "Decisioni integrative definitive") prevalgono su tutte le fonti precedenti in caso di incongruenze.** In particolare: niente pericolo ambientale né primo soccorso aziendale; solo modalità Realistica da 180 s; soglie 80/50; ID semantici; RCP con movimento fisico dei controller; DAE con elettrodi provvisori; mancata chiamata limita l'esito a successo parziale senza arrivo simulato dei soccorsi.

Per le discrepanze residue non coperte dalle decisioni: **non inventare una soluzione e non modificare automaticamente il comportamento**: segnala il conflitto e chiedi conferma. Questo file è un riassunto operativo e non sostituisce i PDF originali.

**Ambito del gruppo:** implementare il solo scenario Man Down e le risorse ad esso necessarie. Menu globali, impostazioni XR, leaderboard, salvataggi e sistemi comuni devono essere **riutilizzati se esistenti**, non duplicati.

## 2. Progetto generale

*Sicurezza Immersiva 81* è un serious game educativo VR single-player per supportare la formazione sulla sicurezza sul lavoro. Il giocatore, in prima persona, esplora ambienti lavorativi, valuta rischi, usa strumenti e prende decisioni durante emergenze. Le azioni ricevono feedback visivo, sonoro e testuale, con esito finale di successo, successo parziale o fallimento.

- **Engine e linguaggio:** Unity e C#.
- **Piattaforma:** OpenXR, visori VR/MR; il gruppo intende testare su Meta Quest 3.
- **Interazione generale:** movimento 6DoF, locomozione lineare e/o teleport, grab/drop, utilizzo strumenti, interazioni UI. La digitazione numerica prevista dal GDD generale non è richiesta: la chiamata al 112 è simulata.
- **Vincolo di integrazione:** identificare prima i componenti XR, prefab, input e scoring già presenti nel progetto. Il GDD **non definisce una mappatura esatta dei pulsanti** dei controller.
- **Accessibilità prevista dal GDD generale:** mano dominante selezionabile; palette distinguibile anche per daltonismo (esempio Okabe–Ito); testi leggibili. Le informazioni critiche non dipendono solo dal colore o solo dall'audio.

## 3. Obiettivo e situazione iniziale

Il giocatore entra in un ufficio e trova un collega incosciente a terra vicino a una scrivania. Deve controllare la coscienza, chiamare il **112**, valutare la respirazione e seguire il percorso appropriato fino all'arrivo simulato dei soccorsi, quando la chiamata è stata effettuata.

All'avvio di una partita si determinano casualmente, in modo **indipendente**, e si mantengono stabili per tutta la partita:

- **Respirazione dell'NPC:** anormale **70%**, normale **30%**.
- **Disponibilità del DAE:** presente **75%**, assente **25%**.

Le probabilità sono **configurabili dall'Inspector**. Per i test deve essere possibile **forzare manualmente entrambe le condizioni**, così da coprire tutte le combinazioni senza dipendere dalla randomizzazione.

**Nessuna pozzanghera e nessun rischio ambientale:** l'opzione del GDD specifico è esclusa da decisione approvata. Non si generano danni all'operatore né penalità Safety collegate.

## 4. Ambientazione e oggetti

| Elemento | Ruolo previsto |
|---|---|
| NPC incosciente a terra | Verifica di coscienza e respirazione; RCP; eventuale cambio posa; zone configurabili per torace ed elettrodi |
| Scrivania, computer | Arredamento e contesto dell'ufficio |
| Telefono | Chiamata simulata al 112 |
| DAE a parete | Strumento utilizzabile **solo se presente** nella partita; due elettrodi provvisori |
| Arredamento da ufficio | Ambientazione e navigazione |

Non esiste una mesh del DAE con elettrodi separati e afferrabili: si usa il modello disponibile più **due primitive Unity appiattite** con materiali semplici. La logica non dipende dalla mesh-specifica degli elettrodi, così una futura sostituzione non richiede modifiche al codice. Non si implementano cavi fisici, simulazioni dei cavi o animazioni complesse.

Gli oggetti decorativi non richiedono automaticamente sistemi di interazione o prefab personalizzati. Riutilizzare asset e prefab condivisi come **istanze**, senza modificare gli originali.

## 5. Sequenza funzionale principale

### 5.1 Verifica della sicurezza

L'ambiente è considerato sicuro per direttiva approvata: non esistono pericoli ambientali da gestire, non c'è pozzanghera e non c'è un Bad Ending per esposizione al pericolo. Non si generano penalità Safety: la dimensione resta neutrale 5/5.

### 5.2 Coscienza e soccorsi

1. Il giocatore interagisce con l'NPC per verificarne lo stato di coscienza con GRIP su una spalla. TASK 05.2: l'etichetta `GRIP` / `Afferra` resta visibile fino alla prima verifica valida, poi viene nascosta e compare il popup informativo condiviso "La persona è incosciente." con un solo pulsante "OK". Il messaggio non suggerisce di controllare la respirazione.
2. Deve attivare i soccorsi con la **chiamata simulata al 112**. Interagendo con il telefono si apre il **popup decisionale comune** tramite `AskDecision("Chiami il 112?", new[] { "Sì", "No" }, callback, 0f)`: `Sì` (indice 0) chiama `Call112()` ed è l'unica scelta che registra la decisione; `No` (indice 1) chiude il popup **senza registrare alcuna azione** e lascia il telefono interagibile per un nuovo tentativo. Lo stato della chiamata è globale e autorevole in `ManDownScenario.IsCall112Made`: dopo un `Sì` accettato da qualsiasi telefono nessun altro telefono apre il popup, ma tutti restano fisicamente afferrabili. Non è previsto un timeout del popup. Se la chiamata non viene mai effettuata, la penalità `EMERGENCY_NOT_CALLED` resta applicabile alla conclusione. **Nessun tastierino numerico, Canvas aggiuntivo o sistema UI personalizzato**: si usa solo `HUD_DecisionPanel` del kit condiviso, compatibile con Operational.
3. Il **primo soccorso aziendale è escluso completamente**: nessuna chiamata aggiuntiva, nessuna attivazione automatica, nessun NPC dedicato, nessuna penalità relativa.
4. La mancata chiamata è registrata come `EMERGENCY_NOT_CALLED`: comporta la penalizzazione prevista e impedisce il successo completo, ma **non impedisce la conclusione della missione**. In quel caso non si simula l'arrivo dei soccorsi e non si dichiara che l'NPC sia fuori pericolo; l'esito massimo è successo parziale.
5. Il giocatore controlla la respirazione dell'NPC.

### 5.3 Percorso A — respirazione normale

1. TASK 05.2: avvicinando un controller davanti alla bocca e mantenendolo fermo per **2,5 s** (durata configurabile) si completa la verifica; il sistema comunica l'esito con il popup informativo condiviso "La persona respira normalmente." (un solo pulsante "OK"). Non compaiono etichette o indicatori di avanzamento per la zona.
2. Il giocatore monitora l'NPC e, se appropriato, può metterlo in posizione laterale: sostituzione/cambio posa semplificato, senza ragdoll.
3. Dopo le procedure fondamentali si svolge una **breve fase finale di assistenza**, senza attesa passiva prolungata; la durata è configurabile e va validata.
4. **Se il 112 è stato chiamato**, arriva simulato il soccorso e si valuta l'esito. **Se non è stato chiamato**, si applica la conclusione alternativa del §5.2: nessun arrivo, nessuna dichiarazione di fuori pericolo, esito massimo successo parziale.

#### 5.3.1 Sistema di pose e scelta della posizione (TASK 06.1)

- **Cinque pose statiche** su un solo NPC, gestite da `ManDownPoseController` sul GameObject `MESH_RiggedEmployee`: `Supine` (posizione iniziale), `Recovery` (laterale di sicurezza), `LegsRaised`, `Prone` e `Seated`. Ogni posa memorizza solo posizione e rotazione **locali** delle ossa dello scheletro esistente; la scala non viene mai salvata né applicata. L'applicazione è immediata, senza clip, Animator, interpolation o ragdoll. I dati sono serializzati nel componente e persistono nella scena.
- **Individuazione delle ossa:** vengono considerate solo le 73 ossa referenziate dagli `SkinnedMeshRenderer` e discendenti da `Hips`. Mesh, trigger, collider e oggetti estranei (per esempio `MESH_MouthZone` e `MESH_ShoulderGrip`) non rientrano nella memorizzazione. Non esiste un Animator sull'NPC che possa sovrascrivere le trasformazioni delle ossa.
- **Custom Inspector:** per ogni posa mostra lo stato *Configurata / Non configurata* e i pulsanti **Save Pose** (con conferma prima di sovrascrivere e blocco su scheletro non valido) e **Apply Pose**; permette di trovare `Hips`, catturare la posa corrente come `Supine` iniziale senza modificarla e ripristinare integralmente la posa iniziale. Ogni operazione è un gruppo Undo autonomo e viene salvata nella scena o nel prefab aperto.
- **Quattro scelte decisionali:** laterale di sicurezza, gambe sollevate, prona e seduta. `ManDownScenario.ChoosePosition()` è l'autorità: registra sempre la scelta logica (posizione selezionata, posizione precedente, timestamp, esito, correzioni) anche se la posa visiva non è stata configurata. `ManDownNpc.SelectPosition()` chiama lo scenario e poi applica la posa equivalente tramite `ManDownPoseController`.
- **Correttezza:** la laterale di sicurezza è corretta quando la respirazione è normale; le altre tre sono sempre errate; con respirazione assente o anomala tutte e quattro sono inappropriate. La valutazione usa la condizione respiratoria autorevole stabilita all'inizio della partita, indipendentemente dal momento in cui il giocatore l'ha verificata.
- **Posa mancante:** nessuna eccezione e nessuna interruzione della missione; la scena visiva resta invariata, la scelta logica viene comunque registrata e il problema è segnalato in Console una sola volta per posa per partita. La posizione logica dello scenario e la posa visiva applicata sono quindi stati distinti e consultabili separatamente.
- **Correzioni:** una nuova scelta con la stessa posizione di quella corrente viene ignorata come duplicato della stessa interazione; gli errori già registrati restano sempre presenti e una scelta corretta successiva viene segnata come correzione.
- Limiti del TASK 06.1: le quattro pose alternative **non** sono state create (l'utente le configurerà con l'Editor); non esistono ancora popup decisionale, secondo grab, riposizionamento trigger, rivalutazione respiratoria, timer finale, arrivo soccorsi, `EndMission`, RCP o DAE.

#### 5.3.2 Interazione VR e rivalutazione (TASK 06.2)

- **GRIP sulle spalle:** prima della valutazione respiratoria il GRIP continua a verificare la coscienza come in TASK 03; dopo la valutazione respiratoria un **nuovo GRIP effettivo** apre il popup di posizionamento. Serve il rilascio (`GrabExited`) per riarmare l'interazione: tenere premuto il pulsante non genera attivazioni ripetute. Se un popup è già aperto il GRIP non viene consumato né duplicato. Non sono state aggiunte etichette o suggerimenti.
- **Popup decisionale condiviso:** `AskDecision("Come posizioni la persona?", [...], callback, 0f)` con le quattro opzioni *Posizione laterale*, *Gambe sollevate*, *Posizione prona*, *Posizione seduta* mostrate in **ordine casuale** a ogni apertura. L'indice scelto viene mappato alla scelta logica corrispondente e la posa visiva cambia istantaneamente. Non viene comunicato al giocatore se la scelta è corretta o errata; errori e correzioni restano nel registro per scoring e debrief. Durante il popup la HUD passa alla fase `PHASE_DECISION` e torna alla fase logica corrente alla chiusura; non vengono avanzati obiettivi né conclusa la missione.
- **Trigger box:** `MESH_MouthZone` è figlia dell'osso `Head` e `MESH_ShoulderGrip` di `outfit_top`, quindi seguono automaticamente lo scheletro durante i cambi di posa. Dimensioni, offset, collider, `Interactable`, `LabelAnchor` e riferimenti sono stati preservati: non sono stati creati duplicati né modificati i dati delle pose configurate manualmente.
- **Rivalutazione respiratoria:** la zona davanti alla bocca funziona come in TASK 05.2 (presenza fisica di un controller, permanenza continua configurabile di 2,5 s, nessun pulsante, nessuna etichetta, popup informativo con l'esito corrispondente). Ogni valutazione ha una voce propria nello storico con timestamp e posizione logica al momento del controllo; la prima è iniziale, le successive sono rivalutazioni. Dopo un posizionamento corretto, una rivalutazione viene segnalata come tale; se la posizione viene cambiata effettivamente, la rivalutazione precedente non è più valida per la nuova posizione e il flag viene azzerato fino a una nuova verifica. La mancata rivalutazione non blocca la conclusione: il dato resta grezzo per il futuro scoring.
- **Spostamento della zona:** quando la posa cambia, il conteggio di permanenza viene azzerato e il rilevamento resta sospeso finché i controller non escono dal volume, così il trigger che segue lo scheletro non provoca valutazioni involontarie né conserva conteggi precedenti.
- **Reset:** a nuova missione la posa visiva torna a `Supine`, il GRIP viene riarmato, popup e messaggi in attesa vengono chiusi/reimpostati e lo scenario ripristina il suo stato iniziale. La chiamata al 112 e il timer comune restano indipendenti e disponibili come prima.
- Limiti del TASK 06.2: non esistono ancora timer conclusivo di 15 s, arrivo simulato dei soccorsi, conclusione della missione, calcolo dei punteggi, RCP o DAE.

#### 5.3.3 Monitoraggio finale del ramo normale (TASK 06.3)

- **Avvio:** il monitoraggio parte quando la respirazione è normale e il giocatore seleziona correttamente la posizione laterale. La durata è di **15 secondi**, configurabile dall'Inspector (`MonitoringDurationSeconds`). Non parte nel ramo con respirazione assente/anomala, nemmeno selezionando la laterale. L'API `StartFinalMonitoring()` è pubblica e potrà essere riutilizzata dal futuro ramo RCP/DAE.
- **Fase UI:** durante il monitoraggio la HUD condivisa usa `PHASE_RESOLUTION`. Non viene avviato un secondo timer visibile: resta solo il timer generale della missione, la cui scadenza ha la precedenza e conclude con `TIMEOUT`.
- **Durante il monitoraggio:** il giocatore può chiamare il 112, rivalutare la respirazione, cambiare posa e continuare a muoversi/interagire. La rivalutazione non è obbligatoria e la sua omissione viene registrata come dato grezzo (`ReassessmentAfterCorrectPositionMissing`) per il futuro scoring, senza influenzare il punteggio provvisorio. Chiamare il 112 o rivalutare non azzera il monitoraggio.
- **Cambio di posa:** una posizione diversa da quella laterale corretta interrompe il monitoraggio; ripristinare la laterale avvia un nuovo monitoraggio completo. Selezionare di nuovo la laterale senza un cambio effettivo non riavvia nulla.
- **Conclusione:** allo scadere, con 112 chiamato viene comunicato l'arrivo simulato dei soccorsi con `Alert` condiviso e la missione si conclude; senza 112 la missione si conclude comunque, senza arrivo simulato e senza dichiarare che la persona è al sicuro, con l'omissione registrata ed esito massimo `Partial`. Non vengono creati modelli, animazioni o NPC per i soccorsi. La missione si conclude una sola volta e vengono conservati azioni, errori e timestamp.
- **Soluzione provvisoria:** la conclusione usa le API già presenti (`FinishMission()`/`Conclude()`) con il punteggio provvisorio delle cinque dimensioni grezze: non sono stati inventati nuovi criteri di scoring. La sostituzione definitiva è prevista al Task 09.
- **Stato e reset:** `ManDownScenarioResult` espone monitoraggio completato, numero di valutazioni, timestamp dell'ultimo posizionamento corretto e omissione della rivalutazione. Dopo la conclusione non restano popup o callback pendenti; il reset annulla il monitoraggio e ripristina posa Supine, interazioni e flag iniziali.

### 5.4 Percorso B — respirazione anormale

1. TASK 05.2: il sistema comunica l'esito con il popup informativo condiviso "La respirazione è assente o anomala." (un solo pulsante "OK"), senza suggerire l'intervento. Il giocatore avvia poi la RCP tramite movimento fisico dei controller nella zona torace dedicata.
2. La progressione usa compressioni valide distinte e frequenza adeguata:
   - con DAE disponibile: **20** compressioni valide prima dell'eventuale DAE;
   - dopo la defibrillazione: **20** compressioni valide;
   - senza DAE: **30** compressioni valide complessive.
   - frequenza di riferimento **100–120 cpm**; si valutano posizione, ampiezza virtuale, frequenza e rilascio; **nessuna ventilazione**.
3. Se il **DAE è presente**, il giocatore lo recupera, lo attiva, posiziona manualmente i due elettrodi nelle zone dedicate (snap automatico se corretti), segue l'analisi simulata e la defibrillazione quando indicata, poi riprende la RCP. Il DAE può essere usato tempestivamente anche prima di completare le 20 compressioni iniziali.
4. Se il **DAE è assente**, continua la RCP senza bloccarsi.
5. Dopo le procedure fondamentali e la breve fase finale, **se il 112 è stato chiamato** arriva il soccorso simulato; altrimenti vale la conclusione alternativa del §5.2.

**Nota didattica:** numeri e procedure sono soglie di gameplay e simulazione, non istruzioni cliniche complete né certificazione BLSD. I parametri numerici devono essere configurabili e validati da un formatore qualificato prima di presentarli come formazione sanitaria. Il timer di gioco non deve suggerire che sia appropriato interrompere la rianimazione dopo 2–3 minuti nel mondo reale.

## 6. Flowchart logico normalizzato

```mermaid
flowchart TD
    A[Ingresso nell'ufficio] --> C[Verifica coscienza]
    C --> G[Chiamata simulata al 112 o omissione]
    G --> H[Verifica respirazione]
    H --> I{Respira normalmente?}
    I -->|Sì| J[Monitoraggio e posizione laterale se appropriata]
    J --> F{112 chiamato?}
    I -->|No| K[RCP: 20 o 30 compressioni valide]
    K --> L{DAE disponibile?}
    L -->|No| M[RCP fino a 30 compressioni]
    L -->|Sì| N[DAE: attivazione, elettrodi, analisi, defibrillazione]
    N --> O[Ripresa RCP: 20 compressioni]
    M --> F
    O --> F
    F -->|Sì| Z[Breve assistenza finale e arrivo simulato dei soccorsi]
    F -->|No| Y[Conclusione alternativa: nessun arrivo, nessuna dichiarazione di fuori pericolo, massimo successo parziale]
    T{Timer 180 s scaduto?} -->|Sì| X[Fallimento TIMEOUT, azioni comunque valutate]
```

Il diagramma rappresenta il percorso corretto e le conclusioni approvate. Il timeout e le omissioni sono gestiti come regole trasversali, non come nodi già visualizzati all'utente. L'ambiente è sempre sicuro per direttiva approvata.

## 7. Interazioni richieste (versione semplificata)

| Interazione | Comportamento atteso | Vincoli |
|---|---|---|
| Verifica coscienza | TASK 03: GRIP su `MESH_ShoulderGrip` con `Interactable`/`LabelAnchor` comuni (`Persona a terra` / `Verifica la coscienza`, `GRIP` / `Afferra`); `VerifyConsciousness()` registra una sola `CONSCIOUSNESS_CHECK` con `NPC incosciente`. TASK 05.2: etichetta iniziale mantenuta e nascosta dopo la verifica, popup "La persona è incosciente." con solo "OK", obiettivo "Verifica coscienza e respiro" aggiornato quando anche la respirazione è valutata | Nessun ragdoll, scuotimento o spostamento fisico; nessun suggerimento sulla respirazione; reset ripristina etichetta e popup |
| Chiamata 112 | Targhetta Interaction di prossimità "Telefono: Chiama i soccorsi" con GRIP/Afferra (come l'NPC), nascosta durante il grab e ripristinata al rilascio, rimossa su tutti i telefoni dopo il Sì e ripristinata a nuova missione; interazione → popup comune `AskDecision("Chiami il 112?", ["Sì", "No"])`; `Sì` → `Call112()` e registrazione della decisione; `No` → popup chiuso, nessuna registrazione, telefono riutilizzabile | Nessuna digitazione, tastierino numerico, Canvas o UI personalizzata; nessun primo soccorso aziendale; popup disponibile anche in Operational; penalità `EMERGENCY_NOT_CALLED` solo se la chiamata non avviene mai |
| Verifica respirazione | TASK 05.1: `ManDownScenario.VerifyBreathing()` applica l'esito iniziale e conserva registro/timestamp della prima valutazione. TASK 05.2: `ManDownBreathingZone` rileva la presenza fisica di una sola ancora controller davanti alla bocca per 2,5 s configurabili e richiama `ManDownNpc.AssessBreathing()`; popup condiviso sull'esito, ripetibile senza duplicati. Indicio visivo transform-based ancora da implementare | 70/30 configurabile e forzabile; la randomizzazione resta solo nel controller; nessun pulsante, raycast, etichetta o indicatore di avanzamento per la zona; la coscienza non è obbligatoria prima della respirazione |
| Posizione laterale e altre scelte | TASK 06.1: `ManDownNpc.SelectPosition()` registra la scelta in `ManDownScenario.ChoosePosition()` e applica la posa equivalente con `ManDownPoseController`; quattro scelte (laterale, gambe sollevate, prona, seduta), pose configurate dall'utente con il Custom Inspector. TASK 06.2: dopo la valutazione respiratoria un nuovo GRIP su `outfit_top` apre `AskDecision("Come posizioni la persona?")` con le quattro opzioni in ordine casuale; la scelta cambia la posa subito, senza feedback di correttezza | Nessun clip, Animator, ragdoll o modifica alle scale; posa mancante = scelta logica registrata e scena visiva invariata; nuovo GRIP richiesto per ogni azione, nessun popup duplicato, nessuna etichetta o suggerimento aggiunto |
| RCP | Movimento fisico dei controller su zona torace; due soglie, riarmo, ampiezza, frequenza | Rilevamento separato dalla valutazione; nessuna forza simulata; nessuna ventilazione |
| DAE | Recupero, attivazione, elettrodi provvisori con snap, analisi simulata, defibrillazione, ripresa RCP | Nessun cavo fisico; errori recuperabili ma registrati |
| Fine scenario | TASK 06.3: nel ramo normale la posizione laterale corretta avvia 15 s di monitoraggio in `PHASE_RESOLUTION`; poi arrivo simulato con `Alert` solo se il 112 è stato chiamato, altrimenti conclusione senza soccorsi ed esito massimo Partial | Un solo timer generale, timeout prioritario, conclusione unica, nessun popup pendente, nessun modello/animazione per i soccorsi; scoring definitivo al Task 09 |

La logica dello scenario espone metodi pubblici ed eventi: non dipende dal player rig né dal sistema di interazione VR. La presentazione e il feedback UI sono uno strato separato, da integrare solo nel task dedicato.

## 8. Modalità, tempo ed esiti

| Modalità | Stato | Tempo | Note |
|---|---|---:|---|
| Realistica | **Da implementare** | **180 s** | Unica modalità prevista ora. |
| Guidata | Non implementata | Nessun timeout | Parametri configurabili per future estensioni. |
| Stress | Non implementata | 120 s | Parametri configurabili per future estensioni. |

La scadenza del timer in modalità Realistica è un fallimento automatico (`TIMEOUT`), ma tutte le azioni effettuate vengono comunque valutate. Non è obbligatorio completare tutte le azioni corrette per concludere la missione: il giocatore può commettere omissioni e ricevere penalizzazioni.

Esiti configurabili:

- **Successo:** punteggio `>= 80`.
- **Successo parziale:** punteggio `>= 50` e `< 80`.
- **Fallimento:** punteggio `< 50` (`LOW_SCORE`).

I vecchi valori del GDD generale (Realistico 30/10/0, Stress 50/20/0) e la durata Guidata non definita sono superati dalle decisioni approvate. I punteggi mostrati al giocatore e l'applicazione finale delle soglie al punteggio ponderato di Man Down oppure allo SPI comune saranno concordati durante l'integrazione finale.

## 9. Errori, Bad Ending e identificativi interni

Non usare numeri dei Bad Ending dei PDF: usare identificativi semantici interni, indipendenti dalle descrizioni mostrate al giocatore.

**Fallimento (cause registrate; missione conclusa una sola volta):**

| ID | Condizione |
|---|---|
| `CPR_OMITTED` | RCP completamente omessa quando necessaria |
| `TIMEOUT` | Scadenza del timer Realistico |
| `LOW_SCORE` | Punteggio finale `< 50` |

**Penalizzazioni senza fallimento automatico:**

| ID | Condizione |
|---|---|
| `EMERGENCY_NOT_CALLED` | Chiamata al 112 omessa; esito massimo successo parziale, nessun arrivo simulato |
| `AED_OMITTED` | DAE necessario e disponibile, mai utilizzato |
| `CPR_POOR_QUALITY` | RCP tentata ma inadeguata; distinta da `CPR_OMITTED` |
| `AED_INCORRECT_USE` | Utilizzo scorretto del DAE; distinto da `AED_OMITTED` |

Tutte le cause di fallimento presenti vanno registrate, garantendo una sola conclusione della missione. Le descrizioni in italiano saranno gestite durante l'integrazione UI, non ora. Non esistono più un Bad Ending per pericolo ambientale né per mancata attivazione del primo soccorso aziendale: entrambe le meccaniche sono escluse.

## 10. Feedback

- **Visivo:** indizio di respirazione transform-based; indicatore della zona torace per la RCP; cambio posa; snap degli elettrodi; feedback su azioni corrette/errate; schermate di esito nella UI finale.
- **Audio:** non ci sono asset audio nel gruppo: ambiente, chiamata simulata, istruzioni DAE e conferme non sono ancora disponibili. Non inventarli in questo task.
- **Testo:** istruzioni essenziali e riepilogo finale saranno aggiunti con la UI comune; la logica espone eventi e ID. La domanda sul 112 usa il pannello decisionale comune `HUD_DecisionPanel` (`AskDecision`), non una UI del gruppo. TASK 05.2 usa lo stesso pannello per tre messaggi informativi con una sola opzione "OK": "La persona è incosciente.", "La persona respira normalmente." e "La respirazione è assente o anomala.". L'uso con una sola opzione è tecnicamente supportato ma non previsto dal contratto documentato (2–4 opzioni), in attesa di conferma del team UI; senza pannello si ripiega su `Alert`.
- **Accessibilità:** non affidare informazioni critiche solo al colore o solo all'audio; mantenere leggibilità in VR.

## 11. Integrazione tecnica e confini del gruppo

Queste sono **direttive operative per OpenCode**, non descrizioni di sistemi già verificati:

1. All'inizio di ogni task, leggere **`AGENTS.md`**, `TASK_PROGRESS.md` e questo documento, quindi verificare lo stato reale di scene, prefab e script.
2. Lavorare soltanto nella directory del gruppo `Assets/_Project/Groups/G_Marius/` e relative sottocartelle.
3. **Non modificare** `Shared`, cartelle di altri gruppi, `ProjectSettings`, `Packages`, configurazioni XR o scene globali senza autorizzazione.
4. I prefab condivisi possono essere **istanziati** nelle scene del gruppo; non applicare modifiche agli asset sorgente.
5. Non duplicare XR Origin, locomozione, input, scoring o UI se già esistenti.
6. Non introdurre nuovi pacchetti, sistemi complessi o dipendenze non necessarie.
7. Mantenere separati: rilevamento fisico, logica dello scenario, validazione, scoring, presentazione UI.
8. La logica deve funzionare e poter essere testata anche senza visore, via Inspector/UnityEvent.
9. Il rig è quello condiviso di `SCN_Core` (integrato nel 2026-11-25): non creare un rig locale, un altro input o un'altra interazione. La vecchia indicazione di un rig temporaneo inserito dall'utente è superata.
10. Alla fine di ogni task, registrare in `AGENTS.md` **solo** file realmente creati/modificati, comportamento implementato, verifiche eseguite e problemi aperti. **Non segnare come completato ciò che non è stato testato.**
11. Per ogni task indicare prima file e prefab coinvolti; chiedere conferma per modifiche con impatto esterno alla directory autorizzata.
12. Per la chiamata al 112 usare `AskDecision` della classe base: il pannello comune è disponibile in tutti i livelli, **Operational incluso** (nessun filtro per livello nel codice di `ScenarioController`/`DecisionPanel`). Per Man Down si usa `timeoutSeconds=0`: nessun limite e nessuna barra; solo `Sì` registra tramite `Call112()`, mentre `No` chiude il popup senza registrare e consente una nuova interazione col telefono. Un eventuale timeout futuro (`-1`) andrebbe trattato come `No`. Il timer del pannello è distinto dal timer della missione, che continua a scorrere. Non creare tastierini numerici, Canvas o UI personalizzata. Per i tre popup informativi dell'NPC (TASK 05.2) si usa lo stesso `AskDecision` con una sola opzione "OK", senza modifiche condivise: uso tecnicamente supportato dal pannello reale ma non previsto dal contratto documentato (2–4 opzioni), in attesa di conferma del team UI; se il pannello manca si ripiega su `Alert`. La zona di respirazione usa il BoxCollider trigger e le sole ancore controller del rig condiviso, senza raycast, pulsanti o etichette aggiuntive.

### Nomenclatura richiesta dal gruppo

- Materiali: `MAT_NomeOggetto`, ad esempio `MAT_OfficeChair_Fabric`.
- Prefab: `MESH_NomeOggetto`, compresi i nuovi prefab del gruppo.
- Texture: `TEX_NomeOggetto_Tipo` per i nuovi asset, senza rinominare file importati se rischia di rompere riferimenti.
- Scene: `SCN_NomeScena`.
- Script/classi C#: `PascalCase`; metodi e proprietà pubbliche `PascalCase`; campi privati/protetti `camelCase`; costanti `UPPERCASE`.

## 12. Checklist funzionale per test e accettazione

- [ ] La scena si apre senza errori e utilizza l'infrastruttura XR prevista dal progetto.
- [ ] All'avvio sono selezionati respirazione (70/30) e disponibilità DAE (75/25) in modo indipendente, stabili per tutta la partita.
- [ ] Le due condizioni possono essere forzate manualmente per coprire tutte le combinazioni.
- [ ] Si può verificare lo stato di coscienza dell'NPC.
- [ ] È possibile effettuare la chiamata simulata al 112 tramite il popup comune `AskDecision` ("Chiami il 112?" / "Sì" / "No"); l'omissione è registrata senza bloccare la conclusione.
- [ ] La verifica della respirazione porta al ramo corretto.
- [ ] Nel ramo normale funzionano monitoraggio, posizione laterale se appropriata e breve assistenza finale.
- [ ] Nel ramo anormale la RCP usa movimento fisico dei controller, due soglie, riarmo e 20 / 20 / 30 compressioni valide a 100–120 cpm; nessuna ventilazione.
- [ ] Se il DAE è presente, la procedura con elettrodi provvisori, snap, analisi, defibrillazione e ripresa RCP è utilizzabile.
- [ ] Se il DAE è assente, la procedura permette di continuare la RCP senza bloccarsi.
- [ ] Il DAE può essere usato anche prima di completare le 20 compressioni iniziali.
- [ ] Gli errori sono recuperabili ma registrati con gli ID approvati.
- [ ] Il timer Realistico di 180 s provoca `TIMEOUT` con valutazione delle azioni già eseguite.
- [ ] Se il 112 non è stato chiamato, non c'è arrivo simulato né dichiarazione di fuori pericolo e l'esito massimo è successo parziale.
- [ ] Le soglie 80/50 e gli ID `CPR_OMITTED`, `TIMEOUT`, `LOW_SCORE`, `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE` sono rispettate e la missione si conclude una sola volta.
- [ ] Safety resta 5/5 e nessun pericolo ambientale o pozzanghera è stato introdotto.
- [ ] Nessun asset Shared, di altri gruppi o impostazione globale è stato modificato.
- [ ] La scena è stata provata in Play Mode e, quando possibile, su Meta Quest 3.

## 13. Decisioni risolte e priorità

**Risolte:** probabilità iniziali, meccanica RCP, meccanica DAE, timeout e arrivo soccorsi, soglie ed esiti, primo soccorso aziendale, sorgente dei trigger, identificativi interni, modalità da implementare, limiti alla mancata chiamata e utilizzo del popup decisionale comune per la chiamata al 112 (indicazione del team UI del 2026-11-25). Vedi `TASK_PROGRESS.md`.

**Ancora necessari per la validazione, non per la struttura del codice:** valori didattici delle soglie RCP (ampiezza, rilascio, tolleranza di frequenza), durata della fase finale di assistenza, `T_target`/`T_max` di Time, scelta finale tra punteggio ponderato e SPI comune in integrazione. I campi saranno configurabili: non inventare valori clinici.

**Priorità consigliata di sviluppo (proposta tecnica):**

1. `ManDownScenario` con randomizzazione, force per i test, timer e API/eventi.
2. `ManDownNpc` con respiro visivo, posa laterale e zone configurabili.
3. Verifica coscienza e `ManDownPhone` con chiamata 112 simulata.
4. Verifica respirazione e diramazione dei due percorsi.
5. `ManDownCpr` come rilevamento fisico e validazione nel controller.
6. `ManDownAed` con elettrodi provvisori e ripresa RCP.
7. Scoring, ID ed esito unico.
8. Play Mode senza visore, poi integrazione con il rig condiviso di `SCN_Core` (già collegato alla scena; resta da validare su visore).
9. Integrazione UI SI81 e validazione finale su visore.

---

**Istruzione per OpenCode:** prima di scrivere codice, confronta queste specifiche con il progetto Unity reale e con i PDF originali. Riporta eventuali discrepanze non coperte dalle decisioni approvate, proponi un singolo task circoscritto e attendi conferma quando una scelta non è definita. Questo documento **non prova** che una funzionalità esista già.
