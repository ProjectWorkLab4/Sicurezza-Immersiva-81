# GDD Section: Scoring System & Performance Metrics

## Scenario 03 — Man Down | Sicurezza Immersiva 81

**Stato:** modello di scoring locale aggiornato alle decisioni approvate dall'utente (2026-11-24/25). Le strutture e le soglie di esito sono approvate per il gameplay di Man Down; i singoli parametri didattici restano da validare. Non è una modifica al sistema condiviso né allo SPI comune.

Il sistema valuta la prestazione attraverso cinque dimensioni. Il punteggio ponderato di progetto ha un massimo di 100 punti. La priorità è riconoscere la situazione, attivare tempestivamente i soccorsi ed eseguire correttamente le azioni richieste dal ramo della missione. Pericolo ambientale, pozzanghera, danni all'operatore e primo soccorso aziendale sono esclusi.

> **SISTEMA IMPLEMENTATO IN TASK 09.1 — sostituisce i modelli di scoring precedenti.** Le vecchie soglie 20/20/30 non sono più progressione di gameplay e i vecchi pesi/cap non si applicano. Il calcolo vive in `ManDownScoring` (solo elaborazione dei dati registrati); l'SPI è `ScoreRules.ScoreFromDimensions`, cioè la media aritmetica delle cinque dimensioni, senza modificare la formula condivisa. L'integrazione con `EndMission`, esiti e debriefing resta al Task 09.2. I riferimenti a 20/20/30 più avanti nel documento sono storici.

## Sistema attuale — TASK 09.1

| Dimensione | Regola |
|---|---|
| **Safety** (0-100) | Base 100. Penalità una sola volta ciascuna: posizionamento inappropriato −30; contatto che interrompe l'analisi DAE −15; scarica non sicura (contatto) −50; scarica non autorizzata (pressione anticipata/non consigliata/elettrodi mancanti) −30; RCP con respirazione normale −100; elettrodi con respirazione normale −30. Una stessa pressione non conteggia in due categorie (unsafe ha priorità su unauthorized). |
| **Awareness** (0-100) | Coscienza verificata 30; respirazione verificata 70. Ripetizioni senza punti extra. |
| **Decision** (0-100) | Chiamata al 112: 70. Normali: laterale mantenuta alla conclusione 30. Anomali con DAE: RCP valida almeno una volta 20 + elettrodi applicati almeno una volta 10. Anomali senza DAE: RCP valida 30. La sola estrazione del DAE non dà punti. |
| **Execution — anomalo** (0-100) | `rcpNorm = rcpAccumulated / 90 * 100`. Senza DAE: `Clamp(rcpNorm)`. Con DAE: `min(rcpNorm, 60) + 5 (1° elettrodo) + 5 (2°) + 10 (analisi completata) + 20 (scarica valida)`. Proroga: compressioni/qualità non entrano nel valore RCP, ma i traguardi DAE restano validi. |
| **Execution — normale** (0-100) | Laterale mantenuta 100, altrimenti 0; −50 se è stata effettuata RCP inappropriata. Nessun punto per RCP/DAE in questo ramo. |
| **Time** (0-100) | 112: ≤20 s → 80; 20-60 s → lineare 80→30; >60 s → 30; omesso → 0. Valutazione iniziale: ≤20 s → 20; 20-60 s → lineare 20→0; oltre/omessa → 0. Tempo effettivo di missione, coerente con le pause. |

Tutti i valori (penalità, pesi, riferimento RCP 90, cap 60, soglie temporali 20/60) sono campi serializzati modificabili dall'Inspector su `ManDownScoring`. Nessun punteggio è mostrato durante la missione.

**Integrazione Task 09.2:** `ManDownScenario` calcola internamente lo SPI come media aritmetica delle cinque dimensioni con lo stesso arrotondamento della UI e passa `-1` a `EndMission`, perché la UI comune ricalcoli lo SPI. Esito: SPI ≥80 Positive, ≥50 Partial, <50 Negative; timeout generale o nessuna compressione valida nel ramo anomalo → Negative; 112 omesso o Execution <50 → al massimo Partial. La scadenza ordinaria della finestra 45 s e della proroga 15 s non è un timeout. Il debrief è generato da dati registrati con i prefissi `DebriefLinePrefix`, in italiano, senza numeri, formule o giudizi clinici; righe duplicate evitate e limite 8. Soglie 80/50 ed Execution 50 sono campi configurabili su `ManDownScenario`.

**Revisione TASK 10.1 (coerenza, pesi e formule invariati):** scoring, esito e debrief leggono un unico snapshot RCP autorevole — `ManDownScenario.ScoringCprSnapshot`, cioè quello congelato alla scadenza della finestra se la finestra è scaduta, altrimenti l'ultimo registrato. Le compressioni in proroga restano escluse dal valore RCP e dal conteggio usato per `CPR_OMITTED`, mentre i traguardi DAE in proroga restano validi. `ManDownScoreResult` espone `RcpPoints` e `RcpMaxPoints` (cap 60 con DAE, 100 senza) per giudicare la quantità RCP con lo stesso riferimento dello scoring: raggiunto il cap non viene segnalato come insufficiente. Il ritmo viene giudicato solo con almeno un intervallo valutabile. Il debrief distingue respirazione non valutata / normale / anomala: senza valutazione non attribuisce al giocatore azioni o omissioni del ramo, e i messaggi DAE compaiono solo con respirazione anomala valutata e DAE disponibile all'inizio; DAE ignorato, recuperato ma inutilizzato, parziale e completato restano messaggi distinti. `AED_OMITTED` è registrato solo se il DAE non è mai stato recuperato, senza penalità aggiuntive per chi lo ha recuperato senza completarlo. Il timeout generale è letto dal risultato congelato (non da un flag azzerato prima del debrief). La soglia di tempestività del messaggio 112 riutilizza il parametro Time. Nessun peso, cap, soglia o formula approvata è stata modificata.

## Panoramica delle dimensioni

| Dimensione | Peso | Punteggio iniziale | Meccanica | Descrizione |
|---|---:|---|---|---|
| Safety | 5% | 5/5 | Sottrattiva, attualmente neutra | Incolumità dell'operatore |
| Awareness | 10% | 0/10 | Additiva | Verifica di coscienza e respirazione |
| Decision | 30% | 30/30 | Sottrattiva contestuale | Chiamata ai soccorsi e scelta del ramo corretto |
| Execution | 30% | 0/30 | Additiva, skill check | Qualità della RCP, DAE o assistenza nel ramo normale |
| Time | 25% | 0–25 | Proporzionale | Tempestività delle azioni necessarie |
| **Totale** | **100%** | — | — | — |

## 1. Safety (Sicurezza fisica)

- Peso: 5/100 punti. Partenza: 5 punti.
- Meccanica: sottrattiva solo in presenza di danni all'operatore.
- Non essendo previsto alcun pericolo ambientale o pozzanghera, il valore resta **5/5** (100/100 normalizzato). Non inventare danni, rischi o penalità collegate. Non confondere la sicurezza dell'operatore con la qualità delle cure all'NPC.

## 2. Awareness (Consapevolezza della situazione)

- Peso: 10/100 punti. Partenza: 0 punti. Meccanica: additiva.
- Verifica della coscienza: +5 punti.
- Verifica della respirazione: +5 punti.
- Assegnare ogni bonus una sola volta, soltanto quando la relativa interazione è stata effettivamente completata. Non attribuire punti per la sola vicinanza all'NPC o per il semplice trascorrere del tempo. La correttezza della scelta successiva appartiene a Decision.

## 3. Decision (Correttezza procedurale & Flow)

- Peso: 30/100 punti. Partenza: 30 punti. Meccanica: sottrattiva, contestuale.
- I malus si applicano una sola volta per categoria, al verificarsi della condizione definitiva o di una scelta errata effettiva; mai per un'azione semplicemente non ancora compiuta.
- La RCP completamente omessa è un fallimento automatico (`CPR_OMITTED`) gestito a livello di esito, non un semplice malus; la RCP tentata male è `CPR_POOR_QUALITY` e ricade in Execution.

| Azione o omissione | ID | Malus | Condizione |
|---|---|---:|---|
| Mancata chiamata al 112 | `EMERGENCY_NOT_CALLED` | −15 | Chiusura o timeout senza chiamata registrata |
| DAE ignorato | `AED_OMITTED` | −5 | DAE presente e necessario, mai utilizzato alla chiusura |
| Scelta di procedura incompatibile con la respirazione | — | −5 | Azione errata effettiva, recuperabile se corretta |

Decision = `max(0, 30 − somma dei malus applicabili)`. Nessun malus per DAE assente, RCP non necessaria o azioni non applicabili al ramo. La chiamata tardiva ma effettuata non equivale a omissione: il ritardo è valutato in Time. Evitare duplicazioni della stessa penalità. L'avvio di una procedura riguarda Decision; la qualità della sua esecuzione riguarda Execution.

La mancata chiamata comporta anche il **limite di esito**: anche con punteggio alto non si può ottenere successo completo; l'esito massimo è successo parziale. Questo limite **non altera il punteggio**: non si sottraggono punti aggiuntivi e non si falsano le dimensioni.

**Dati preparati in TASK 06.2 (nessun calcolo di punteggio):** `ManDownScenario` conserva lo storico completo delle valutazioni respiratorie (`ManDownBreathingAssessmentRecord`: numero progressivo, timestamp, flag `IsInitial`, esito, posizione logica al momento del controllo, `FollowsCorrectPosition` e validità per la posizione corrente). Espone inoltre se il giocatore ha rivalutato dopo l'ultimo posizionamento corretto (`ReassessedAfterLastCorrectPosition`, `LastCorrectPositionAt`, `LastReassessmentAt`, `HasValidReassessmentAfterCorrectPosition`). Ogni cambio effettivo di posizione invalida le rivalutazioni precedenti. La mancata rivalutazione non blocca la conclusione: il dato è solo grezzo, da usare nel futuro task di scoring. Non vengono applicati malus né punteggi in questo task.

**Dati preparati in TASK 06.1 (nessun calcolo di punteggio):** `ManDownScenario` registra ogni scelta di posizione con `ManDownPositionChoiceRecord` (selezionata, precedente, timestamp, fase, esito `Correct`/`Incorrect`, flag di correzione e nota tecnica), conservando errori e correzioni in liste separate, la prima scelta corretta e la posizione logica corrente. La correttezza segue la condizione respiratoria autorevole: laterale di sicurezza corretta solo con respirazione normale, altre tre sempre errate, tutte inappropriate con respirazione assente o anomala. Per la futura timeline del Debrief sono già predisposti al massimo 8 dati nel formato previsto da `RecordChoice` (`ElapsedSeconds`, azione ≤ 60 caratteri, conseguenza ≤ 40 caratteri, tipo Good/Bad/Critical), senza chiamare l'API non ancora implementata. In questo task **non** viene applicato alcun malus Decision aggiuntivo: i dati grezzi sono pronti per il task di scoring.

## 4. Execution (Competenza tecnica e abilità)

- Peso: 30/100 punti. Partenza: 0 punti. Meccanica: additiva tramite skill check.
- La valutazione riguarda le azioni richieste dalla specifica partita, non tutti gli strumenti presenti nell'ufficio.

| Ramo | Skill check | Punti massimi |
|---|---|---:|
| Respirazione anormale, DAE presente | RCP (20 iniziali + 20 dopo defibrillazione) | 20 |
| Respirazione anormale, DAE presente | DAE | 10 |
| Respirazione anormale, DAE assente | RCP (30 complessive) | 30 |
| Respirazione normale | Posizione laterale, se appropriata, e monitoraggio | 30 complessivi |

Le due fasi RCP del ramo con DAE condividono lo stesso cap di 20 punti: la qualità complessiva tiene conto sia delle 20 compressioni iniziali sia delle 20 dopo la defibrillazione. Il DAE può essere avviato prima di completare le 20 iniziali senza perdere i punti DAE: le soglie di compressione sono progressione di gameplay, non prerequisito temporale assoluto.

### RCP

Il rilevamento fisico dei controller resta separato dalla valutazione: il detector produce valori grezzi (posizione nella zona torace, ampiezza virtuale, intervalli tra compressioni, rilascio). La validazione decide quali compressioni sono valide e la qualità complessiva.

Definire:

- `N` = numero di compressioni valide richieste dalla fase (20 o 30, configurabile).
- `V` = compressioni valide distinte, registrate solo dopo un rilascio che riarma il detector.
- `I` = compressioni errate rilevanti (`CPR_POOR_QUALITY`), comprese quelle fuori zona, con ampiezza non valida, senza rilascio o con frequenza inadeguata.

Qualità RCP proposta:

```text
quality = clamp01(V / N) × V / max(1, V + I) × frequencyFactor
punti RCP = cap del ramo × quality
```

`frequencyFactor` è 1 quando la frequenza media rientra nell'intervallo di riferimento **100–120 cpm**, altrimenti applica un fattore ridotto configurabile. La posizione, l'ampiezza virtuale e il rilascio determinano se una compressione entra in `V` o in `I`; non si simula la forza fisica applicata al torace e non si valutano le ventilazioni (escluse). Se `N` non è configurato o `V = 0`, non assegnare punti RCP. Contare compressioni distinte, non frame di contatto; non premiare spam né azioni oltre il completamento.

**Nota:** i valori di ampiezza, soglie di rilascio e tolleranza di frequenza non sono stati forniti: i campi devono essere esposti nell'Inspector **senza valori clinici inventati** e validati dal referente didattico prima del collaudo.

**Dati preparati in TASK 07.2 (nessun punteggio applicato):** `ManDownCprRhythm` separa la valutazione ritmica dal rilevamento fisico e produce, per il futuro `frequencyFactor`, i seguenti valori grezzi: compressioni fisicamente complete, intervalli valutati fra pressioni di compressioni complete (mai dai rilasci; la prima completa è solo riferimento), intervalli corretti/troppo brevi/troppo lunghi, relativa percentuale, errore temporale cumulativo e medio, intervallo medio e qualità ritmica complessiva. Parametri configurabili e valori iniziali **approvati per il gameplay**: intervallo ideale `0,55 s`, fascia corretta `0,50–0,60 s`, tolleranza massima dell'errore `0,40 s`. Formule: `errore = Max(0, min − intervallo, intervallo − max)` (sempre non negativo, nessuna compensazione tra troppo veloce e troppo lento) e `qualita = Clamp01(1 − errore / tolleranza)`, con media delle qualità come qualità complessiva. Se non ci sono intervalli, qualità e medie restano 0. `InterruptSequence()` conserva le statistiche e azzera solo il riferimento temporale, per la futura ripresa dopo il DAE; il reset di missione azzera tutto. Le compressioni su persona che respira normalmente sono registrate ma marcate `IsApplicableForScoring = false`: non devono contribuire positivamente. Questi valori sono soglie di gameplay, non una scala clinicamente validata: il passaggio a `frequencyFactor`, alle penalità e al punteggio finale resta al task di scoring.

**Aggiornamento TASK 07.3 (solo dati, nessun punteggio):** `ManDownCprFlow` invia a `ManDownScenario` uno snapshot grezzo per fase (compressioni complete iniziali/dopo DAE/senza DAE, tentativi invalidi, intervalli e qualità ritmica, timestamp di avvio, completamento di fase e interruzione per DAE, eventuale esecuzione inappropriata con respirazione normale). Lo snapshot è esposto come `CprStartedAt`, `LastCprQualitySnapshot` e `CprQuality` nel risultato congelato, pronto per il futuro `frequencyFactor` e per il debrief. La progressione usa solo le compressioni fisicamente complete e le tre soglie 20/20/30 già configurate, indipendentemente dalla qualità ritmica; l'omissione resta distinta dalla cattiva esecuzione. Non sono state applicate penalità o soglie di progressione aggiuntive: in particolare i tentativi invalidi non attivano `CPR_POOR_QUALITY` in questo task.

### DAE

Suddividere l'utilizzo simulato in tre skill check, ciascuno vale 1/3 del cap DAE (10 punti):

1. Recupero e attivazione del dispositivo.
2. Posizionamento manuale dei due elettrodi nelle zone dedicate, con snap automatico se corretti.
3. Analisi simulata, defibrillazione quando indicata e conferma della ripresa della RCP.

Assegnare punti solo per passaggi validi, una volta ciascuno. L'avvio del dispositivo non equivale alla procedura completata. L'uso scorretto è registrato come `AED_INCORRECT_USE` ed è recuperabile; il DAE mai utilizzato quando necessario e disponibile è `AED_OMITTED` in Decision. La procedura non deve bloccare il ramo senza DAE e non dipende dalla mesh specifica degli elettrodi.

### Respirazione normale

Attribuire 15 punti alla posa laterale quando appropriata e 15 al monitoraggio completato, compresa la breve fase finale di assistenza. Se la posa non è appropriata, attribuire tutti i 30 punti al monitoraggio. Non richiedere RCP o DAE in questo ramo. La durata della fase finale è configurabile e va validata: non inventare un valore clinico.

I criteri tecnici, il numero di interazioni e le condizioni di appropriatezza devono essere configurabili e validati dal referente del primo soccorso. Si tratta di parametri di simulazione, non di istruzioni mediche.

## 5. Time (Tempestività dell'intervento)

- Peso: 25/100 punti. Meccanica: proporzionale ai tempi delle azioni essenziali.
- Non valutare esclusivamente la durata totale della partita: una conclusione rapida ottenuta saltando procedure non deve essere premiata.
- Si implementa solo la modalità **Realistica**, con limite **180 s**. Guidata e Stress non si implementano ora; i parametri restano configurabili (`Guided` senza countdown, `Stress` 120 s) per future estensioni.

| Ramo | Azioni temporizzate e ripartizione dei 25 punti |
|---|---|
| Anormale, DAE presente | Chiamata 112: 10; avvio RCP: 10; avvio utilizzo DAE: 5 |
| Anormale, DAE assente | Chiamata 112: 12,5; avvio RCP: 12,5 |
| Normale | Chiamata 112: 15; avvio assistenza appropriata: 10 |

Per ogni azione applicabile, registrare il ritardo `t` dal relativo momento di riferimento: avvio missione per la chiamata; verifica respiratoria per RCP/assistenza; avvio RCP per DAE. Registrare anche il tempo assoluto, per individuare ritardi nelle verifiche preliminari durante i test. Una partenza anticipata ma non valida non viene premiata.

Definire due soglie configurabili per azione: `T_target` (punteggio pieno) e `T_max` (punteggio nullo), con `T_max > T_target >= 0`. Per un'azione valida eseguita: `fattore = clamp01((T_max − t) / (T_max − T_target))`. Per un'azione omessa: `fattore = 0`. Time = somma dei punti massimi di ciascuna azione moltiplicati per il suo fattore. Non premiare la velocità delle singole compressioni né l'interruzione anticipata del monitoraggio/RCP.

I target saranno stabiliti tramite playtest, non derivati da soglie cliniche. In fase di test usare un solo orologio locale; nella versione integrata usare il tempo della sessione comune, senza duplicare il timer.

## 6. Esiti e regole trasversali

- **Successo:** punteggio `>= 80`.
- **Successo parziale:** punteggio `>= 50` e `< 80`.
- **Fallimento:** punteggio `< 50`, registrato come `LOW_SCORE`.

**Fallimenti automatici, indipendenti dal punteggio:**

| ID | Condizione |
|---|---|
| `CPR_OMITTED` | RCP completamente omessa quando necessaria |
| `TIMEOUT` | Scadenza del timer Realistico (180 s) |

**Penalizzazioni senza fallimento automatico:** `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE`. RCP omessa e RCP eseguita male restano distinte; DAE ignorato e DAE usato male restano distinti.

- La mancata chiamata al 112 comporta il malus Decision, impedisce il successo completo e limita l'esito massimo a successo parziale, **senza alterare artificialmente il punteggio**.
- Quando non è stata effettuata la chiamata, non si simula l'arrivo dei soccorsi e non si dichiara che l'NPC sia fuori pericolo: si usa la conclusione alternativa descritta nelle specifiche.
- La scadenza del timer causa il fallimento, ma tutte le azioni effettuate devono comunque essere valutate e conservate.
- Registrare **tutte** le cause di fallimento eventualmente presenti, garantendo che la missione venga conclusa **una sola volta**.
- Congelare risultati una sola volta a fine missione; reset completo a ogni nuova partita.

Il vecchio modello "Negative per omissione critica (112, RCP necessaria, DAE necessario e disponibile ignorato) o timeout" è superato: solo `CPR_OMITTED` e `TIMEOUT` sono automatici; `LOW_SCORE` dipende dal punteggio.

## 7. Integrazione con la UI comune SI81

Calcolare e conservare i cinque valori grezzi normalizzati da 0 a 100 (arrotondati solo alla consegna), nell'ordine Safety, Awareness, Decision, Execution, Time. Il punteggio ponderato locale è `0,05*S + 0,10*A + 0,30*D + 0,30*E + 0,25*T`, equivalente alla somma dei contributi in punti sopra descritti.

**Da concordare in integrazione finale:** applicare le soglie 80/50 al punteggio ponderato di Man Down oppure allo SPI comune. La guida UI v0.6 calcola lo SPI come media semplice delle cinque dimensioni (pesi 20% ciascuno): i pesi 5/10/30/30/25 non sono riproducibili con quell'API senza cambiare il significato dei valori. Non passare i contributi ponderati al posto delle dimensioni normalizzate, non falsare i valori e non passare uno score esplicito contro la guida. Fino alla decisione finale, conservare i dati grezzi e usare l'eventuale media comune per lo SPI, senza modificare il sistema condiviso.

Registrare fino a 8 decisioni rilevanti con tempo, azione e conseguenza; debrief in italiano con prefissi `+`, `-` o `!`, demandato all'integrazione UI. Non chiamare `RecordChoice` se non disponibile. Nessuna UI o modifica a sistemi condivisi durante lo sviluppo del gameplay.

## 8. Test minimi e note operative

Verificare:

- successo nei tre rami con punteggio massimo raggiungibile;
- combinazioni forzate di respirazione e disponibilità DAE;
- chiamata omessa: malus, nessun arrivo, nessuna dichiarazione di fuori pericolo, esito massimo successo parziale e punteggio non alterato artificialmente;
- RCP omessa (`CPR_OMITTED`) e RCP tentata male (`CPR_POOR_QUALITY`) come condizioni distinte;
- conteggi 20 / 20 / 30 e frequenza 100–120 cpm, con riarmo al rilascio;
- DAE omesso (`AED_OMITTED`) e DAE usato male (`AED_INCORRECT_USE`), con errori recuperabili;
- DAE avviato prima delle 20 compressioni iniziali;
- ritardo senza omissione e timeout (`TIMEOUT`) con valutazione delle azioni già compiute;
- presenza simultanea di più cause di fallimento, con una sola conclusione missione;
- reset completo e assenza di malus per azioni non applicabili;
- valori dimensionali sempre entro 0–100 ed eventi duplicati senza incrementi di punteggio o malus.

Il rig condiviso di `SCN_Core` è integrato nella scena (2026-11-25): non creare un rig locale né dipendenze da un rig di test. La vecchia indicazione di un rig temporaneo inserito dall'utente è superata. La logica di scoring va testata anche senza visore.

Fonti di progetto: `scoring_system.pdf` (modello strutturale), `Man_Down_GDD.pdf`, `FlowChart_ManDown.pdf`, `Project Work VR - GDD.pdf` e `GUIDA_INTEGRAZIONE_SCENARIO.pdf` (v0.6, sezioni 4, 6 e 10). Le decisioni approvate dall'utente prevalgono sulle fonti in caso di conflitto.
