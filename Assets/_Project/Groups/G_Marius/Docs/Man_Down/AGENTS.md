# AGENTS.md — Man Down (G_Marius)

> **Documento operativo per gli agenti che lavorano sullo scenario Man Down.**
> Va letto all'inizio di **ogni** task, insieme a `Man_Down_Specifiche.md`.
> Contiene solo informazioni **verificate** sul progetto reale, più le regole di lavoro del gruppo.
> Se una cosa non è segnata come verificata, non darla per esistente.

- **Gruppo:** G_Marius
- **Scenario:** Man Down (ID 3)
- **Cartella autorizzata:** `Assets/_Project/Groups/G_Marius/`
- **Ultimo aggiornamento:** 2026-11-26
- **Stato:** analisi iniziale completata; materiali e prefab dei modelli sistemati; ufficio open space 14×10 ricostruito e rifinito (Task 16–18). Documentazione aggiornata in TASK 00/00b/00c/00d/01 e Task 00e: le otto decisioni aperte e le decisioni integrative sono state **approvate e registrate**. TASK 01 (rig condiviso), **TASK 02 (`ManDownScenario`)** e **TASK 03 (sola verifica della coscienza dell'NPC)** sono completati e verificati in Editor/Play Mode: nessun rig locale, nessuna UI Canvas/TMP specifica e nessun input/raycast/grab duplicati. Il controller registra stato, condizioni iniziali, azioni, risultati RCP grezzi, esiti e ID approvati; il timer resta solo quello comune (`SO_Mission_ManDown` invariato, Operational 600 s) e la verifica della coscienza resta sottoazione dell'obiettivo 2 senza `CompleteObjective()` né avanzamento da `PHASE_SCAN`. Il filone TASK 04 (telefono/112) risulta implementato e committato (`da95c74`), ma resta aperto in attesa dei test runtime dell'utente. **TASK 05.1 (logica della valutazione respiratoria) completato e verificato in compilazione/Edit Mode:** la randomizzazione 70/30 e le forzature restano in `ManDownScenario`; `ManDownNpc.AssessBreathing()` è il metodo pubblico richiamato dalla zona fisica del TASK 05.2, con registrazione una sola volta, timestamp della prima valutazione e ramo normale/assente-anomalo esposti senza stato locale. **TASK 05.2 (interazione respiratoria e popup informativi) completato e verificato in compilazione/Edit Mode:** `ManDownBreathingZone` usa il BoxCollider trigger davanti alla bocca e le sole ancore controller del rig condiviso con permanenza configurabile di 2,5 s; `ManDownNpc` nasconde l'etichetta della spalla, mostra i tre popup informativi con `AskDecision` a un'opzione "OK" (uso tecnicamente supportato ma fuori dal contratto documentato 2–4 opzioni, in attesa di conferma del team UI; fallback `Alert`) e completa l'obiettivo "Verifica coscienza e respiro" in qualsiasi ordine. **TASK 06.1 (controller/Editor delle pose e logica decisionale) completato e verificato in compilazione/Edit Mode:** `ManDownPoseController` gestisce cinque pose statiche con le sole trasformazioni locali delle 73 ossa dello scheletro, senza clip/Animator/ragdoll e senza toccare le scale; il Custom Inspector configurato con Save/Apply/Ripristino e Undo; `Supine` e la posa iniziale sono state catturate senza alterare l'NPC; `ManDownScenario.ChoosePosition()` registra le quattro scelte logiche con errori, correzioni e dati per il futuro scoring, e `ManDownNpc.SelectPosition()` applica la posa visiva solo se disponibile. Le quattro pose alternative sono state configurate manualmente dall'utente e non sono state modificate. **TASK 06.2 (interazione VR e rivalutazione) completato e verificato in compilazione/Edit Mode:** il GRIP post-respirazione apre `AskDecision("Come posizioni la persona?")` con quattro opzioni in ordine casuale e applica la posa senza feedback di correttezza; serve un nuovo GRIP per ogni azione; le trigger box figlie di `Head`/`outfit_top` seguono lo scheletro; lo storico delle valutazioni respiratorie distingue la prima verifica dalle rivalutazioni e ne registra la validità dopo il posizionamento. **TASK 06.3 (monitoraggio finale del ramo normale) completato e verificato in compilazione/Edit Mode:** 15 s configurabili in `PHASE_RESOLUTION` senza secondo timer, avvio con laterale corretta, interruzione su posizione errata e riavvio completo, 112 e rivalutazione innocui, conclusione con arrivo simulato solo se il 112 è stato chiamato e limite a Partial in caso contrario, timeout prioritario e reset completo; i dati del monitoraggio e dell'omissione della rivalutazione sono nel risultato congelato. `ManDownCpr`, `ManDownAed`, indicio visivo del respiro, scoring definitivo e UI dedicata restano da implementare. La validazione fisica su Meta Quest 3 resta manuale. **TASK 07.1 (rilevamento fisico RCP) completato in compilazione/Edit Mode:** `ManDownCpr` con zona toracica dedicata su `Spine1`, due soglie (5 cm), controllo del punto medio e delle singole mani, vibrazione su entrambi i controller e dati grezzi; restano da implementare ritmo/scoring/progressione e la prova su visore. **TASK 07.2 (valutazione ritmica) completato in compilazione/Edit Mode:** `ManDownCprRhythm` calcola intervalli, errori e qualità dalle pressioni delle compressioni complete, con parametri configurabili, statistiche grezze e interruzione esplicita che conserva i dati; nessun punteggio, UI o metronomo. **TASK 07.3 (integrazione RCP) completato in compilazione/Edit Mode:** `ManDownCprFlow` collega detector e ritmo allo scenario con istruzione e avviso tramite `Alert` comune, progressione 20/20/30, monitoraggio finale senza DAE e snapshot grezzo nel risultato; nessun DAE, penalità o scoring. **TASK 08.1 (scatola DAE e apertura manuale) completato in compilazione/Edit Mode e verificato in Play Mode senza visore:** prefab `MESH_AEDBox` e `MESH_AEDPlaceholder`, sportello a cerniera aperto con GRIP e movimento del controller tramite le sole API condivise (`Interactable` con `followsHand=false` + `RigRegistration.TryGetAimPose`), angolo 0–110° e soglia di recupero 70° configurabili, disponibilità letta da `ManDownScenario.IsAedAvailable`; modello/texture originali preservati ma non più in scena. **TASK 08.2 (recupero, trasporto e accensione automatica) completato in compilazione/Edit Mode e verificato in Play Mode senza visore:** `ManDownAedDevice` usa il grab condiviso come i telefoni, la scatola abilita la presa solo oltre l'angolo di recupero, primo GRIP → recupero+accensione registrati una sola volta nello scenario con timestamp ed eventi, indicatore grigio→verde via `MaterialPropertyBlock`; dopo il rilascio il Rigidbody diventa dinamico con gravità. **TASK 08.3 (elettrodi e aggancio magnetico) completato in compilazione/Edit Mode e verificato in Play Mode senza visore:** prefab `MESH_AedElectrode`, due zone figlie di `Spine2`, aggancio/rimozione con GRIP e stato centrale, più due cavi `LineRenderer` (decisione dell'utente: la sezione inserita sui cavi prevale sul precedente divieto). **TASK 08.4 (analisi, scarica e QTE) completato in compilazione/Edit Mode:** macchina a stati su `ManDownAedDevice`, contatto mani via trigger figlio di `Hips`, pulsante con `Select` condiviso (scelta dell'utente), beep `DAE.mp3` su `AudioSource` 3D, qualità temporale con grace 1,5 s e zero a 8 s, snapshot grezzo nello scenario; nessuna progressione RCP, scoring o difficulty. Nessun commit o push.

> **AGGIORNAMENTO TASK 08.5 (2026-11-26) — prevale sulle specifiche storiche:** nel ramo respiratorio anomalo non esistono più le soglie 20/20/30 compressioni né il monitoraggio aggiuntivo di 15 s. La missione usa una finestra temporale di **45 s** che parte alla prima valutazione anomala, con proroga **una sola volta** di **15 s** se il DAE è disponibile, estratto e senza scarica completata. La RCP produce un valore grezzo cumulativo (somma delle qualità degli intervalli); pesi, cap e normalizzazione saranno definiti nel Task 09. I riferimenti a 20/20/30 nei paragrafi storici restano come documentazione del vecchio comportamento, non come regola operativa. Task 09.1 (calcolo scoring), 09.2 (integrazione EndMission/esiti/debriefing), 09.3 (audio notifica 112/sirena) e 10.1 (revisione coerenza di scoring, esiti e debriefing) completati e verificati in Edit Mode; gestione contestuale dei label spostata alla Task 11.1; Task 10 (test completi) e Task 13 (tre difficoltà) da fare, quest'ultimo ultimissimo.

---

## 0. Direttive TASK 00 — priorità massima (2026-11-24)

Queste sono le regole storiche di TASK 00 e **prevalgono sulle istruzioni precedenti**, ma sui punti rig/UI/Build Settings sono aggiornate dal §0.3 (integrazione del rig condiviso, 2026-11-25), che ha priorità massima.

1. **Logica prima, UI dopo, ma vincoli UI fin da ora.** Completare e verificare l'intero flusso di gioco Man Down senza UI; integrare la UI comune SI81 solo alla fine. I vincoli di architettura, naming e API di `Docs/GUIDA_INTEGRAZIONE_SCENARIO.md` e `Docs/UI_INTEGRATION_FOR_AI.md` vanno però rispettati fin dal primo script, per evitare riscritture. Il percorso reale dei due file è la cartella del gruppo, **non** `Assets/_Project/Docs/` come scritto nella guida.
2. **Nessuna UI specifica Man Down ora.** Il kit `SI81.UI` è disponibile e `MNG_Scenario` usa `SI81.ManDown.ManDownScenario`, derivato dal `ScenarioController` condiviso, per sessione, spawn e timer; non creare Canvas, HUD, pannelli, etichette o `LabelAnchor` propri, né chiamate UI specifiche, fino al task dedicato. L'EventSystem resta quello di `SCN_Core`.
3. **Nessun nuovo sistema XR.** Non aggiungere XR Interaction Toolkit, rig, player, controller, locomozione, Input Actions, Raycaster o collider/interactable locali. Il player condiviso di `SCN_Core` è già integrato: non creare un rig locale, nemmeno temporaneo, salvo una nuova autorizzazione esplicita che non duplichi il sistema comune.
4. **Controller stabile.** `ManDownScenario` è stato implementato in TASK 02 come `MonoBehaviour` derivato da `ScenarioController`, con API pubblica stabile, metodi senza parametri e wrapper per gli elettrodi. Non duplica sessione, spawn o timer; le chiamate UI verranno aggiunte una sola volta nel task dedicato e i componenti di interazione dovranno usare solo i metodi/eventi già esposti, senza riscrivere la logica di stato.
5. **Esclusione totale di pericolo ambientale e pozzanghera.** Non implementare pozzanghera, panno, rischio di scivolamento, danni all'operatore o penalità Safety collegate. Questa decisione **sostituisce** il §7.4 punto 1 e il Bad Ending "esposizione al pericolo" del flowchart. Safety resta neutra (5/5).
6. **Nessuna modifica in TASK 00.** Il task corrente è solo analisi e documentazione: non toccare `SCN_Man_Down.unity`, prefab, materiali, script o Build Settings. Ogni lavoro successivo resta dentro `Assets/_Project/Groups/G_Marius/`.
7. **Stato documentale.** Inventario, architettura proposta, impedimenti e decisioni aperte sono in `Docs/Man_Down/TASK_PROGRESS.md`.
8. **Direttive operative.** Le regole di esecuzione riviste del 2026-11-24 sono nel §0.1 e prevalgono su qualsiasi formulazione precedente.

### 0.1 Direttive operative riviste (2026-11-24)

1. Prima di iniziare, leggere `AGENTS.md` e `TASK_PROGRESS.md` in `Docs/Man_Down/`, verificandone il percorso effettivo.
2. Leggere **solo** le sezioni della documentazione pertinenti al task assegnato: non ripetere analisi complete del progetto.
3. Eseguire **esclusivamente** il task richiesto; non iniziare autonomamente quelli successivi.
4. Modificare esclusivamente i file autorizzati del gruppo `G_Marius`; riutilizzare script, modelli e componenti già esistenti.
5. Non modificare `Shared`, `Home`, altri gruppi, `Packages` o `ProjectSettings`, salvo autorizzazione esplicita: in TASK 01 è stata autorizzata solo la modifica di `ProjectSettings/EditorBuildSettings.asset`.
6. Non creare UI, Canvas o pannelli personalizzati: la UI comune SI81 si integra solo nel task dedicato.
7. Mantenere la logica indipendente dal player rig e dalla rappresentazione grafica; non creare né modificare sistemi XR. Il rig ora è quello condiviso di `SCN_Core`, non un rig temporaneo locale.
8. Non implementare il pericolo ambientale.
9. Rispettare le convenzioni di naming e documentazione C# della guida SI81; privilegiare soluzioni semplici e compatibili con Meta Quest 3.
10. Non modificare materiali, illuminazione o disposizione dell'ambiente se non strettamente necessario al task.
11. Usare Unity MCP per ispezionare e configurare la scena quando necessario.
12. Aggiornare `TASK_PROGRESS.md` dopo ogni fase significativa, indicando modifiche effettuate, verifiche, problemi e prossimo passaggio.
13. Al termine verificare la compilazione, controllare i riferimenti nell'Inspector e provare la funzionalità quando possibile.
14. Aggiornare `AGENTS.md` e `TASK_PROGRESS.md` distinguendo i test **realmente eseguiti** da quelli **ancora da effettuare manualmente**.
15. Se il contesto viene compattato o la sessione interrotta, rileggere `AGENTS.md` e `TASK_PROGRESS.md` e riprendere dalla prima operazione incompleta.
16. Non eseguire commit o push senza autorizzazione.
17. Fornire un riepilogo sintetico e attendere il prossimo task.

### 0.2 Decisioni gameplay approvate (2026-11-24/25) — priorità massima

Queste decisioni dell'utente prevalgono su GDD specifico, flowchart, GDD generale e su qualsiasi formulazione precedente incompatibile. Il dettaglio è in `Docs/Man_Down/TASK_PROGRESS.md` e nei due documenti di specifiche/scoring.

1. **Probabilità:** respirazione anormale 70% / normale 30%; DAE presente 75% / assente 25%; indipendenti, configurabili dall'Inspector e forzabili manualmente per i test.
2. **RCP:** movimento fisico dei controller nella zona torace, due soglie compressione/rilascio, riarmo al rilascio, valutazione di posizione/ampiezza virtuale/frequenza, errori e qualità registrati; rilevamento fisico separato dalla valutazione; nessuna forza simulata. Soglie: 20 compressioni iniziali con DAE, 20 dopo defibrillazione, 30 senza DAE; 100–120 cpm; nessuna ventilazione; parametri configurabili e da validare.
3. **DAE:** recupero, attivazione, elettrodi posizionati manualmente con snap, analisi simulata, defibrillazione quando indicata, ripresa RCP. Elettrodi provvisori con primitive appiattite e materiali semplici; nessun cavo fisico né animazione complessa; futura sostituzione mesh senza cambiare logica.
4. **Timeout e soccorsi:** solo modalità Realistica, 180 s; Guidata e Stress non implementate ma configurabili. Arrivo simulato dopo procedure fondamentali e breve assistenza finale, solo se il 112 è stato chiamato. Timeout = fallimento `TIMEOUT` con valutazione di tutte le azioni. Mancata chiamata non blocca la conclusione: nessun arrivo attribuito a una chiamata mai fatta, nessuna dichiarazione di fuori pericolo, esito massimo successo parziale.
5. **Scoring ed esiti:** successo `>= 80`, parziale `>= 50` e `< 80`, fallimento `< 50` (`LOW_SCORE`). Fallimenti automatici solo `CPR_OMITTED` e `TIMEOUT`. Penalizzazioni: `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE`. Cinque dimensioni invariate (Safety 5%, Awareness 10%, Decision 30%, Execution 30%, Time 25%); Safety resta 5/5. Non modificare scoring condiviso o SPI comune; applicazione finale delle soglie da concordare in integrazione.
6. **Primo soccorso aziendale:** escluso del tutto. Solo chiamata simulata al 112; nessuna chiamata aggiuntiva, attivazione automatica, NPC dedicato o penalità relativa.
7. **Trigger:** logica indipendente da rig e interazione VR; metodi pubblici ed eventi per componenti intermedi; separazione tra rilevamento fisico, logica scenario, validazione, scoring e UI; testabile senza visore; nessuna dipendenza da un rig locale: l'integrazione usa il rig condiviso di `SCN_Core` e le API `SI81.*`.
8. **ID interni:** `CPR_OMITTED`, `TIMEOUT`, `LOW_SCORE` per il fallimento; `EMERGENCY_NOT_CALLED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE` per penalizzazioni. Registrare tutte le cause, concludere la missione una sola volta; descrizioni italiane demandate alla UI.

### 0.3 Integrazione del player rig condiviso (2026-11-25) — priorità massima

Questa sezione aggiorna le formulazioni di §0/§0.1 che davano rig, kit UI o Build Settings come non ancora disponibili.

1. **Usare solo il player condiviso.** Man Down non contiene rig, camera, EventSystem, `InputService`, `OVRInput`, XRI, Input Actions, raycast/selezione/grab o UI propri. Tutto arriva da `SCN_Core` e dal kit `SI81.*` condiviso.
2. **Scene e Build Settings autorizzate.** `SCN_Man_Down.unity` è stata rinominata `SCN_ManDown.unity` preservando il GUID `3b4441978213bad42a806f3c23963e12`. Le Build Settings sono `SCN_Core` indice 0, `SCN_MockScenario` indice 1, `SCN_ManDown` indice 2. Nessuna altra modifica globale è stata conservata.
3. **Marker nella scena.** `MNG_SpawnPoint` (`SI81.Rig.SpawnPoint`) e `MNG_Scenario` (`SI81.ManDown.ManDownScenario`, derivato da `SI81.UI.ScenarioController`) sono figli di `prefab_scena`; la scena conserva un solo root. Lo spawn è `(-17.240, 0.253, 26.600)`, yaw `138`; la missione è `SO_Mission_ManDown` riutilizzata, con `editorLevel=Guided` e `loadCoreInEditor=false`. In TASK 02 il componente base su `MNG_Scenario` è stato **sostituito**, non affiancato: la classe base ha `[DisallowMultipleComponent]`.
4. **Caricamento del core.** `RigDirectPlay` carica già `SCN_Core` in Play Mode quando trova `MNG_SpawnPoint`: per questo `loadCoreInEditor` resta `false`. Con `true` comparivano due `SCN_Core`, due `RigRegistration` e due `MNG_UI`.
5. **Verifiche reali.** Compilazione completata; scena riletta su disco con i marker persistiti; validatore scenario `0 errori, 9 avvisi` di naming preesistente; Play Mode con una sola `SCN_Core`, un solo `RigRegistration`, una camera abilitata (`CenterEyeAnchor`), un solo `EventSystem`, due `RayInteractor`, un solo `RigUIBridge`, un solo `InputService`; console runtime con `0 errori`; test PlayMode `SI81.Rig.Tests` **23/23 superati**.
6. **Limite della validazione.** L'Editor riporta `No XR device is available ... Running without VR`: movimento/rotazione reali, pose controller, haptics e presa/selezione su Meta Quest 3 restano **test manuali** e non vanno dichiarati eseguiti.
7. **Prossimo task.** TASK 02 e TASK 03 sono completati; `ManDownNpc` esiste solo per la coscienza e usa `Interactable`/`LabelAnchor` condivisi. Non avviare telefono, respirazione, RCP, DAE, UI dedicata o scoring definitivo senza approvazione esplicita; non eseguire commit/push senza nuova autorizzazione.

---

## 1. Perimetro e regole non negoziabili

### 1.1 Dove si lavora

Tutto il lavoro va fatto **solo** dentro:

```text
Assets/_Project/Groups/G_Marius/
```

Le altre aree si possono **leggere** per capire le dipendenze, ma non modificare:

- `Assets/_Project/Shared/` — libreria condivisa, gestita con pull-before-push.
- `Assets/_Project/Home/` — shell comune dell'applicazione.
- `Assets/_Project/Groups/G_<Altro>/` — altri gruppi.
- `Assets/_Project/Docs/` — documentazione comune.
- `Assets/XR/`, `Assets/Oculus/`, `ProjectSettings/`, `Packages/` — configurazioni globali.
- `Assets/_Project/SCN_Placeholder.unity` — scena placeholder globale.
- `.gitignore`, README, documenti originali (PDF/MD) e file `.fbx`/`.obj` originali.

### 1.2 Cosa non fare senza autorizzazione esplicita

1. Non modificare `Shared`, altri gruppi, `ProjectSettings`, `Packages`, configurazioni XR o scene globali.
2. Non aggiungere pacchetti, dipendenze o sistemi complessi.
3. Non creare un XR Origin, un rig, un player, controller, locomozione, input actions o UI: il player condiviso di `SCN_Core` è già integrato e **non va duplicato**.
4. Non duplicare UI, HUD, scoring, difficoltà, timer o salvataggi: sono sistemi comuni.
5. Non creare Canvas/HUD locali. La guida comune `Guida_HUD_comune_SI81.pdf` li vieta.
6. Non modificare i prefab condivisi. È consentito **istanziarli** nelle scene del gruppo.
7. Non rinominare automaticamente asset importati (FBX, OBJ, texture): si rischiano riferimenti rotti.
8. Non modificare le Build Settings senza autorizzazione esplicita. `SCN_ManDown` è stata aggiunta come indice 2 in TASK 01 con autorizzazione; `SCN_Placeholder` non è più l'unica scena in build.
9. Non dichiarare completato o testato ciò che non è stato provato in Unity / sul visore.
10. Non inventare regole di gameplay dove i documenti sono ambigui: segnala e chiedi conferma.
11. **Ogni nuovo oggetto inserito nella scena va messo come figlio di `prefab_scena`** (con `SetParent(scena.transform, true)` per preservare la posizione world). La scena deve avere un solo root, così da poter essere usata interamente come prefab. Non lasciare nuovi root in `SCN_ManDown.unity`. Regola comunicata dall'utente il 2026-11-22 e applicata anche a `MNG_SpawnPoint` e `MNG_Scenario` in TASK 01.

### 1.3 Flusso di lavoro richiesto

1. Leggere questo file e `TASK_PROGRESS.md` verificandone il percorso effettivo; leggere solo le sezioni dei documenti pertinenti al task, dando priorità a `Docs/GUIDA_INTEGRAZIONE_SCENARIO.md` e `Docs/UI_INTEGRATION_FOR_AI.md` per i vincoli UI.
2. Verificare lo stato reale del solo materiale coinvolto nel task (Unity MCP per la scena quando necessario), senza ripetere l'inventario completo del progetto.
3. Eseguire **un solo task circoscritto**, indicando file e prefab coinvolti; non passare autonomamente ai successivi.
4. Attendere conferma se la modifica ha impatto fuori dalla cartella del gruppo o se una scelta non è definita.
5. Implementare in modo piccolo e testabile, riutilizzando ciò che esiste già.
6. Salvare il progetto nell'Editor (vedi §1.4) dopo ogni modifica importante e comunque alla fine del task.
7. Verificare compilazione, riferimenti nell'Inspector e funzionalità quando possibile; distinguere i test eseguiti da quelli ancora manuali.
8. Alla fine di **ogni micro task / fase significativa**, aggiornare questo file nel §10 e `TASK_PROGRESS.md` con modifiche effettuate, verifiche eseguite, problemi e prossimo passaggio, anche in caso di modifiche esclusivamente documentali.
9. In caso di compattazione del contesto o interruzione, rileggere i due documenti e riprendere dalla prima operazione incompleta.

### 1.4 Salvataggio del progetto

- Salvare il progetto nell'Editor con `File > Save Project` (o `AssetDatabase.SaveAssets()` da script) **dopo ogni modifica importante** a materiali, prefab, mesh, scene o script.
- Salvare **comunque alla fine di ogni task**, anche quando le modifiche sembrano minori: un task non si considera chiuso finché il salvataggio non è stato eseguito.
- Dopo il salvataggio, rileggere l'asset o il file modificato per confermare che le modifiche siano state scritte su disco.
- I soli documenti `.md` non richiedono il salvataggio dell'Editor, ma vanno comunque salvati su disco.

---

## 2. Fonti documentali e priorità

| Documento | Ruolo |
|---|---|
| `Docs/Man_Down/Man_Down_GDD.pdf` | Fonte primaria per meccaniche ed esiti dello scenario. |
| `Docs/Man_Down/FlowChart_ManDown.pdf` | Riferimento di flusso, pericolo ambientale e Bad Ending. |
| `Docs/Project Work VR - GDD.pdf` | GDD generale: difficoltà, interazioni VR, punteggi, accessibilità, sistemi comuni. |
| `Docs/Man_Down/Man_Down_Specifiche.md` | Sintesi operativa dei tre documenti. **Non è prova** che una funzionalità esista. |
| `Docs/Man_Down/Man_Down_Scoring_System.md` | Pesi delle cinque dimensioni, ID di fallimento/penalità e soglie 80/50 approvati; la formula finale e l'eventuale passaggio allo SPI comune vanno concordati in integrazione. |
| `Docs/UI_INTEGRATION_FOR_AI.md` (cartella del gruppo) | Istruzioni API per l'integrazione UI finale. Il percorso citato nella guida (`Assets/_Project/Docs/UI_INTEGRAZIONE_FOR_AI.md`) non corrisponde al progetto reale. |
| `Docs/GUIDA_INTEGRAZIONE_SCENARIO.md` | Guida operativa UI comune v0.6; casi 2/3/6 rilevanti per portare la scena esistente nel kit e scrivere lo script finale. |
| `Assets/_Project/Docs/Guida_HUD_comune_SI81.pdf` | Regole HUD comune; vieta Canvas/HUD locali. |
| `README.md` (root) | Convenzioni di progetto, ownership, workflow `Shared`, build e scene. |

**Nota TASK 00:** i due file UI elencati sopra sono nella cartella `Docs/` del gruppo G_Marius, non in `Assets/_Project/Docs/`.

**Regola sulle discrepanze:** non scegliere una versione e non modificare il comportamento in automatico.
Riporta il conflitto e chiedi conferma. Per lo scenario, il GDD specifico è la base funzionale più dettagliata; il flowchart resta riferimento di flusso.

---

## 3. Stato reale verificato del progetto

Verifica effettuata il **2026-11-22** con ispezione dei file e dell'Editor Unity aperto (Unity 6000.3.14f1).

### 3.1 Tecnologia

| Componente | Stato |
|---|---|
| Unity | `6000.3.14f1` |
| Render pipeline | URP `17.3.0`, asset attivo `PC_RPAsset`, color space **Linear**, quality level `PC` |
| OpenXR | `1.16.1` installato |
| XR Management | `4.7.0` installato |
| Meta XR Core SDK | `209.0.0` installato |
| Input System | `1.19.0` installato |
| XR Interaction Toolkit | **NON installato** |
| Input Actions XR | **assenti** |
| Kit `SI81.UI` / `SI81.Input` / `SI81.Interaction` | **disponibili** e verificati in TASK 01 |
| Player / XR Origin condiviso | **presente in `SCN_Core`** (`RigRegistration`, camera abilitata `CenterEyeAnchor`) |
| Locomozione / interazione / grab | **condivisi** (`InputService`, `RayInteractor`, `Interactable`); nessun sistema locale in Man Down |

Nota: esistono file `.csproj` residui di sample XRI nella root del repository, ma **non** sono contenuto del progetto Unity e non implicano che XRI sia installato. Le righe storiche che davano player e interazione come assenti sono superate dall'integrazione del 2026-11-25 (§0.3).

### 3.2 Script

**Aggiornamento TASK 01 (2026-11-25):** dopo il merge con `origin/main` esistono gli script condivisi in `Assets/_Project/Home/` (`SI81.Input.InputService`, `SI81.Rig.SpawnPoint`, `RigRegistration`, `RigLocomotion`, `SI81.Interaction.Interactable`, `RayInteractor`, `SI81.UI.ScenarioController`, `RigUIBridge`, `MissionDefinition`, `MissionCatalog`, `UIScreenManager`). La verifica del 2026-11-22 riportata di seguito è storica e superata.

**Aggiornamento TASK 02 (2026-11-25):** `Assets/_Project/Groups/G_Marius/Code/Scripts/ManDown/` contiene `ManDownScenario.cs` e `ManDownScenarioTypes.cs` (namespace `SI81.ManDown`). Il controller possiede lo stato dello scenario, le condizioni iniziali configurabili/forzabili, il registro azioni con timestamp, la ricezione dei risultati RCP grezzi, il punteggio provvisorio, gli esiti e gli ID approvati. Non implementa input, camera, mani, interazioni o UI specifica: usa sessione, spawn, timer condiviso e chiusura missione della classe base `SI81.UI.ScenarioController`.

- **Aggiornamento TASK 03 (2026-11-25):** `Code/Scripts/ManDown/` contiene anche `ManDownNpc.cs`, collegato su `MESH_RiggedEmployee` in scena. Si limita alla coscienza: GRIP su `MESH_ShoulderGrip` → `ManDownScenario.VerifyConsciousness()`, con azione `CONSCIOUSNESS_CHECK`, timestamp e risultato `NPC incosciente`. Non conserva stato locale: il reset è quello del controller. Non implementa respiro, posa, telefono, RCP, DAE o punteggio.
- **Aggiornamento TASK 05.1 (2026-11-25):** `ManDownScenario` espone `BreathingAssessedAt`, `BreathingResult` e `BreathingBranch` (`ManDownBreathingBranch`) e riemette `BreathingAssessed` senza duplicare `BREATHING_CHECK`; `ManDownNpc` aggiunge `AssessBreathing()` e proprietà delegate, senza stato locale né interazione VR. La randomizzazione e le forzature restano quelle del TASK 02. Nessun asset di scena o condiviso modificato.
- **Aggiornamento TASK 05.2 (2026-11-25):** `Code/Scripts/ManDown/ManDownBreathingZone.cs` gestisce il rilevamento fisico con il BoxCollider trigger e le ancore controller; `ManDownNpc` aggiunge etichetta, popup informativi condivisi e obiettivo NPC. La scena contiene `MESH_MouthZone` sotto l'osso `Head` dell'NPC. Nessun asset condiviso modificato; la posa dell'NPC e la transform di `MESH_ShoulderGrip` sono invariate.
- **Aggiornamento TASK 06.1 (2026-11-26):** `Code/Scripts/ManDown/` contiene anche `ManDownPoseTypes.cs` e `ManDownPoseController.cs`, con l'Editor separato in `Code/Scripts/ManDown/Editor/ManDownPoseControllerEditor.cs`. Il controller è sull'NPC con 73 ossa, `Supine` iniziale catturata e tutte e cinque le pose configurate dall'utente. `ManDownScenario` espone `ChoosePosition()`, il registro delle scelte e i dati Debrief; `ManDownNpc` espone `SelectPosition()`. Nessun Animator, clip, ragdoll, modifica alle scale o asset condiviso toccato.
- **Aggiornamento TASK 06.2 (2026-11-26):** `ManDownNpc` apre il popup di posizionamento con `AskDecision` a quattro opzioni mischiate e gestisce il riarmo del GRIP e l'accodamento dei popup; `ManDownBreathingZone` sospende il conteggio dopo un cambio di posa; `ManDownScenario` conserva lo storico delle valutazioni con `ManDownBreathingAssessmentRecord` e i flag di rivalutazione. Trigger box invariate come gerarchia e configurazione. Nessun nuovo prefab, collider, UI o asset condiviso.
- **Aggiornamento TASK 06.3 (2026-11-26):** `ManDownScenario` gestisce il monitoraggio finale con `MonitoringDurationSeconds=15`, API/eventi dedicati, `PHASE_RESOLUTION` e conclusione tramite il punteggio provvisorio esistente; `ManDownScenarioResult` espone i nuovi dati; `ManDownNpc` pulisce popup/callback alla conclusione. Nessun timer HUD aggiuntivo, scoring nuovo, RCP o DAE. Pose e BUGFIX 01–05 preservati.
- **Aggiornamento TASK 07.1 (2026-11-26):** `Code/Scripts/ManDown/ManDownCpr.cs` implementa il rilevamento fisico delle compressioni con le due ancore del rig condiviso (nessun collider sui controller), zona trigger `MESH_ChestCompressionZone` figlia di `Spine1`, due soglie e ciclo pressione/rilascio con invalidazione, impulsi aptici su entrambi i controller e dati grezzi con timestamp di missione. Scene aggiornata con un solo componente CPR e nessuna intersezione con le zone di spalla/bocca. `ManDownAed` non esiste ancora; `ManDownPhone` esiste dal Task 04.
- **Aggiornamento TASK 07.2 (2026-11-26):** `Code/Scripts/ManDown/ManDownCprRhythm.cs` valuta la qualità ritmica iscrivendosi agli eventi del detector, senza modificarlo: intervalli dalle sole pressioni di compressioni complete, errore e qualità con parametri configurabili (0,55/0,50/0,60/0,40 s), statistiche grezze, snapshot nell'Inspector, `InterruptSequence()` e `ResetStatistics()`. Nessun punteggio, metronomo, UI o progressione RCP/DAE.
- **Aggiornamento TASK 07.3 (2026-11-26):** `Code/Scripts/ManDown/ManDownCprFlow.cs` integra detector e ritmo nella progressione: istruzione iniziale e avviso di ritmo con `Alert` comune, soglie 20/20/30 lette da `ManDownScenario`, avvio del monitoraggio esistente a 30 compressioni senza DAE, interruzione esplicita per il futuro DAE e snapshot grezzo (`ManDownCprQualitySnapshot`) nel risultato. `ManDownCpr.cs` e `ManDownCprRhythm.cs` rimangono invariati; nessun DAE, penalità o scoring applicato.
- Nessun sistema locale duplica input, rig, interazione, HUD o UI comune. Lo scoring è solo provvisorio (Time ancora a 0) e sarà sostituito dal calcolo definitivo delle cinque dimensioni.

### 3.3 Scene

**`Level/Scenes/SCN_ManDown.unity` — snapshot verificato il 2026-11-25 su disco e in Play Mode**

- La scena è stata rinominata da `SCN_Man_Down.unity` preservando il GUID `3b4441978213bad42a806f3c23963e12`. `SceneRoots` contiene **un solo root**: `/prefab_scena`.
- Sotto il root si trovano la struttura `MESH_OfficeStructure` (14×10×3 m con soffitto), l'ufficio arredato, il NPC `MESH_RiggedEmployee`, tre telefoni, il DAE, il resto dei prop e i due marker `MNG_SpawnPoint` e `MNG_Scenario` aggiunti in TASK 01.
- La scena di scenario **non contiene** Camera, AudioListener, Canvas, HUD, EventSystem, XR Origin/rig, input locale, `OVRInput`, XRI, InputActionAsset, script di interazione o raycast duplicati. In Play Mode la camera attiva è `CenterEyeAnchor` di `SCN_Core` e l'EventSystem è quello condiviso.
- `MNG_SpawnPoint` usa `SI81.Rig.SpawnPoint` a `(-17.240, 0.253, 26.600)`, yaw `138`. `MNG_Scenario` usa `SI81.ManDown.ManDownScenario` con `SO_Mission_ManDown`, spawn point collegato, `editorLevel=1` (`Guided`) e `loadCoreInEditor=0`; il componente è stato riletto su disco come `Assembly-CSharp::SI81.ManDown.ManDownScenario` dopo il salvataggio di TASK 02, con un solo componente derivato da `ScenarioController`.
- Collider su `MESH_OfficeStructure`: `Floor` con MeshCollider e pareti/soffitto con BoxCollider. In TASK 03 è stato aggiunto **solo** un `BoxCollider` sul figlio `MESH_ShoulderGrip` dell'NPC, con `Interactable` (`canSelect=false`, `canGrab=true`, `followsHand=false`) e `LabelAnchor` di tipo `Interaction`; modello, materiali e posa dell'NPC restano invariati. Telefoni, DAE, scrivanie, sedie e prop non hanno collider di gameplay.
- Build Settings verificate su disco: `SCN_Core.unity` indice 0, `SCN_MockScenario.unity` indice 1, `SCN_ManDown.unity` indice 2.

Lo snapshot del 2026-11-24 (`SCN_Man_Down.unity`, nessuna scena nelle Build Settings) è storico e superato. La vecchia descrizione di `prefab_scena.prefab` come blocco con `Main Camera`, `Directional Light` e `Cube` è parimenti storica dei primi task.

### 3.4 Prefab esistenti

| Prefab | Contenuto verificato |
|---|---|
| `Level/Prefabs/Man_Down/MESH_Chair.prefab` | Prefab autonomo (non più variante del FBX) con 5 mesh unite e 5 renderer, 93.153 tris; nessuno script. Vedi Task 3. |
| `Level/Prefabs/Man_Down/MESH_Lamp.prefab` | Istanza di `MESH_Lamp.fbx`; nessuno script. |
| `Level/Prefabs/Man_Down/MESH_FireExtinguisher.prefab` | Prefab autonomo da `MESH_FireExtinguisher.fbx`, 3 renderer, 2.834 tris, `MAT_FireExtinguisher`; nessuno script. Vedi Task 5. |
| `Level/Prefabs/Man_Down/MESH_Employee.prefab` | Da `MESH_Employee.fbx`, 12 SkinnedMeshRenderer, 54.285 tris, 12 materiali `MAT_Employee_*`; nessuna Camera/Light, nessuno script. Vedi Task 6d. |
| `Level/Prefabs/Man_Down/MESH_Clock.prefab` | Da `MESH_Clock.fbx`, 1 renderer, 252 tris, `MAT_Clock`; nessuna Camera/Light, nessuno script. Vedi Task 7b. |
| `Level/Prefabs/Man_Down/MESH_TablePrinter.prefab` | Da `MESH_TablePrinter.fbx`, 1 renderer, 295 tris, `MAT_TablePrinter_Black` + `MAT_TablePrinter_White`; nessuna Camera/Light, nessuno script. Vedi Task 8. |
| `Level/Prefabs/Man_Down/MESH_OfficeDoor.prefab` | Da `MESH_OfficeDoor.fbx` (nuovo modello sostituito dall'utente), 2 renderer, **2100 tris**, `MAT_OfficeDoor_Panel` + `MAT_OfficeDoor_Handle`; nessuna Camera/Light, nessuno script. Vedi Task 15. |
| `Level/Prefabs/Man_Down/MESH_FireAlarm_Sprinkler.prefab` | Prefab autonomo da `MESH_fire_alarm_sprinkler.obj`: 2 mesh unite, 2 renderer, **1.222 tris**, `MAT_FireAlarm_Body` + `MAT_FireAlarm_Sprinkler`; 0 Camera/Light, 0 script. Vedi Task 10. |
| `Level/Prefabs/Man_Down/MESH_Printer.prefab` | Prefab autonomo da `MESH_Printer.obj`: 1 renderer, **3.760 tris**, `MAT_Printer`; 0 Camera/Light, 0 script. Vedi Task 11. |
| `Level/Prefabs/Man_Down/MESH_Desk.prefab` | Prefab autonomo da `MESH_Desk.fbx`: 17 renderer, **10.736 tris**, 19 materiali `MAT_Desk_*`; Camera e 5 Light esclusi, 0 script. Vedi Task 12. |
| `Level/Prefabs/Man_Down/prefab_scena.prefab` | Camera, Directional Light e Cube; nessuno script. |

Tutti i prefab usano il prefisso `MESH_`, compresi i nuovi: decisione esplicita dell'utente del 2026-11-22 (vedi §5). I due prefab creati in questa sessione sono già stati rinominati dall'utente da `PFB_*` a `MESH_*`.

**Aggiornamento TASK 00 (2026-11-24):** la tabella sopra è il registro storico dei task di materiali. Oltre a essa esistono e sono verificati `MESH_RiggedEmployee.prefab` (NPC a terra nella scena), `MESH_AED.prefab` (DAE istanziato in scena), `MESH_Phone.prefab`, `MESH_OfficeDeskCovered.prefab` e `MESH_Sofa 1.prefab`. `MESH_Employee.prefab` esiste ma **non** è istanziato in scena. Nessuno di questi prefab contiene script di gameplay.

### 3.5 Modelli disponibili nel gruppo

Percorso: `Art/Models/Man_Down/`

| Modello | Tris | Mesh / renderer | Materiali embedded | Note |
|---|---:|---:|---|---|
| `MESH_Chair.fbx` | 93.153 | 33 mesh / 33 renderer | 5 | FBX originale invariato (Chairback trasparente). Il prefab usa 5 mesh unite e 5 renderer, stessi 93.153 tris (vedi §3.6 e Task 3). |
| `MESH_Clock.fbx` | 252 | 1 mesh / 1 renderer | 1 (`Material.001`) | Disco sottile con quadrante rivolto verso −Z. Il prefab `MESH_Clock` usa `MAT_Clock` con `Clock.jpeg` come albedo (Task 7b). |
| `MESH_Desk.fbx` | 10.736 | 17 mesh / 17 renderer | 19 | Tutti i materiali embedded erano trasparenti (alpha 0,2 / 0); contiene 1 Camera e 5 Light importati. Nel prefab del Task 12 sono opachi e Camera/Light rimossi; FBX invariato. |
| `MESH_Employee.fbx` | 54.285 | 0 MeshFilter / 12 SkinnedMeshRenderer | 12 | NPC; nessuna texture assegnata. |
| `MESH_fire_alarm_sprinkler.obj` | 1.222 | 10 mesh / 10 renderer | 10 | Materiali con nomi tecnici derivati dall'UV. Il prefab usa 2 mesh unite e 2 renderer (Task 10); OBJ originale invariato. |
| `MESH_FireExtinguisher.fbx` | 2.834 | 3 mesh / 3 renderer | 1 | Estintore. Il prefab `MESH_FireExtinguisher` usa `MAT_FireExtinguisher` (Task 5). |
| `MESH_Lamp.fbx` | 2.220 | 3 mesh / 3 renderer | 1 (`Lit`) | Contiene 1 Camera importata. |
| `MESH_OfficeDoor.fbx` | 2100 | 2 mesh / 2 renderer, 5 slot materiale | 4 (`Matériau`, `Matériau.001`, `Matériau.002`, `Matériau.003`) | Nuovo modello sostituito dall'utente (Task 15). Nessuna texture embedded; texture `Blanc.png`/`NoirGris.jpeg` assegnate per mappatura confermata dall'utente. FBX invariato. |
| `MESH_Printer.obj` | 3.760 | 1 mesh / 1 renderer | 1 (`defaultMat`) | Texture `TI24_maq*` e `norm1a`. Mappatura confermata nel Task 11: `TI24_maq` = colore, `TI24_maqRR` = roughness. OBJ invariato. |
| `MESH_TablePrinter.fbx` | 295 | 1 mesh / 1 renderer, 2 submesh | 2 (`black`, `white`) | Tavolino stampante. Il prefab `MESH_TablePrinter` usa i due materiali uniformi (Task 8). |

Problemi trasversali verificati:

- Tutti i materiali embedded hanno shader `Universal Render Pipeline/Lit` ma **zero texture assegnate**.
- I materiali sono incorporati nel modello (`materialImportMode: ImportViaMaterialDescription`, `materialLocation: InPrefab`).
- `MESH_Chair` → materiale embedded `Chairback`: `_Surface = 1` (Transparent), queue 3000. **Risolto nel prefab** il 2026-11-22 tramite override con `MAT_Chair_Fabric` opaco; il FBX originale resta invariato.
- `MESH_Desk` → tutti i 19 materiali: `_Surface = 1` (Transparent), queue 3000. È un problema di rendering reale, non solo estetico.
- `MESH_Desk` importa 1 Camera e 5 Light; `MESH_Lamp` importa 1 Camera. Sono oggetti potenzialmente indesiderati nelle istanze.

### 3.6 Materiali del gruppo

Percorsi: `Art/Materials/Man_Down/Lamp/`, `Art/Materials/Man_Down/Chair/`, `Art/Materials/Man_Down/FireExtinguisher/`, `Art/Materials/Man_Down/Employee/`, `Art/Materials/Man_Down/Clock/`, `Art/Materials/Man_Down/TablePrinter/`, `Art/Materials/Man_Down/OfficeDoor/`, `Art/Materials/Man_Down/FireAlarm/`, `Art/Materials/Man_Down/Printer/` e `Art/Materials/Man_Down/Desk/`

| Materiale | Stato |
|---|---|
| `MAT_Lamp.mat` | **Configurato (2026-11-22)**: `_BaseMap` = `Lamp_DefaultMaterial_BaseColor`, `_BumpMap` = `Lamp_DefaultMaterial_Normal`, `_MetallicGlossMap` = `TEX_Lamp_MetallicSmoothness` (metallic in R, smoothness = 1 - roughness in A, importata lineare); `_Metallic = 1`, `_Smoothness = 1`, `_SmoothnessTextureChannel = 0`; keyword `_METALLICSPECGLOSSMAP`, `_NORMALMAP`. |
| `MAT_LampLight.mat` | **Configurato (2026-11-22)**: `_BaseMap` rimosso, `_BaseColor` bianco caldo; `loo.png` usato come `_EmissionMap` (non più come albedo); `_EmissionColor` bianco caldo, `_EMISSION` attivo; `_Metallic = 0`, `_Smoothness = 0.2`. |
| `MAT_Chair_Fabric.mat` | **Creato e corretto (2026-11-22)**: `_BaseMap` = `chairback.png`, colore bianco, `_Metallic = 0`, `_Smoothness = 0.15`, opaco; `_Cull = 0` (Off, doppia faccia) dal Task 4. Sostituisce il materiale embedded `Chairback` trasparente. |
| `MAT_Chair_Leather.mat` | **Creato (2026-11-22)**: colore uniforme scuro ripreso da `Lether` embedded, `_Metallic = 0`, `_Smoothness = 0.30`, opaco. Nessuna texture disponibile. |
| `MAT_Chair_Plastic.mat` | **Creato (2026-11-22)**: colore uniforme scuro ripreso da `Matteplastic`, `_Metallic = 0`, `_Smoothness = 0.15`, opaco. Nessuna texture disponibile. |
| `MAT_Chair_Metal.mat` | **Creato (2026-11-22)**: colore uniforme scuro ripreso da `BlackMetal`, `_Metallic = 0.85`, `_Smoothness = 0.40`, opaco. |
| `MAT_Chair_Chrome.mat` | **Creato (2026-11-22)**: colore uniforme grigio ripreso da `Chrome`, `_Metallic = 1`, `_Smoothness = 0.85`, opaco. |
| `MAT_FireExtinguisher.mat` | **Creato (2026-11-22)**: `_BaseMap` = `APC_Fire_extinguisher_BaseColor`, `_BumpMap` = `APC_Fire_extinguisher_Normal` (importata `NormalMap`), `_MetallicGlossMap` = `TEX_FireExtinguisher_MetallicSmoothness` (R = metallic, A = 1 - roughness, lineare), `_OcclusionMap` = `APC_Fire_extinguisher_AO`, `_OcclusionStrength = 1`; `_Metallic = 1`, `_Smoothness = 1`, `_SmoothnessTextureChannel = 0`, opaco, `_Cull = 0`. Keyword `_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`. |

#### Materiali Employee (Task 6c)

Tutti creati il 2026-11-22 con shader `Universal Render Pipeline/Lit`, `_Metallic = 0`, `_SmoothnessTextureChannel = 0`, opachi (`_Surface = 0`, queue 2000), `_Cull = 0`. Nessuna maschera metallic/smoothiness: non esistono mappe metallic/roughness per questo modello, quindi i valori di smoothness sono uniformi e motivati. Keyword `_NORMALMAP` solo dove è presente una normal.

| Materiale | BaseMap | BumpMap | Smoothness |
|---|---|---|---:|
| `MAT_Employee_AvatarBody.mat` | `AvatarBodyMale_Color_1K` | `AvatarBodyMale_Normal1_1K` | 0.30 |
| `MAT_Employee_AvatarEyelashes.mat` | nessuna (`_BaseColor` nero) | nessuna | 0.20 |
| `MAT_Employee_AvatarHead.mat` | `AvatarHeadMale_Color_1K` | `AvatarHeadMale_Normal1_1K` | 0.30 |
| `MAT_Employee_AvatarLeftCornea.mat` | `AvatarLeftCornea_Color_512` | nessuna | 0.65 |
| `MAT_Employee_AvatarLeftEyeball.mat` | `AvatarEyes_Color_512` | `AvatarEyes_Normal_512` | 0.65 |
| `MAT_Employee_AvatarRightCornea.mat` | `AvatarRightCornea_Color_512` | nessuna | 0.65 |
| `MAT_Employee_AvatarRightEyeball.mat` | `AvatarEyes_Color_512` | `AvatarEyes_Normal_512` | 0.65 |
| `MAT_Employee_AvatarTeethLower.mat` | `AvatarTeeth_Color_1K` | `AvatarTeeth_Normal1_1K` | 0.40 |
| `MAT_Employee_AvatarTeethUpper.mat` | `AvatarTeeth_Color_1K` | `AvatarTeeth_Normal1_1K` | 0.40 |
| `MAT_Employee_outfit_bottom.mat` | `pants_SEVAN_Color_1K` | `SEVAN_Normal_1K` (confermato "entrambi") | 0.15 |
| `MAT_Employee_outfit_shoes.mat` | `sneakers_SEVAN_Color_1K` | `SEVAN_Normal_1K` (confermato "entrambi") | 0.25 |
| `MAT_Employee_outfit_top.mat` | `shirt_Olympus_Color_1K` | `shirt_Olympus_Normal_1K` | 0.15 |

`MAT_Clock.mat` — **Creato (2026-11-22, Task 7b)**: `_BaseMap` = `Clock.jpeg`, `_BaseColor` bianco, `_Metallic = 0`, `_Smoothness = 0.35`, opaco, `_Cull = 0`, nessuna keyword. Nessuna mappa metallic/roughness disponibile.

`MAT_TablePrinter_Black.mat` e `MAT_TablePrinter_White.mat` — **Creati (2026-11-22, Task 8)**: URP Lit, colori uniformi 0,178 e 0,906 ripresi dagli embedded, `_Metallic = 0`, smoothness 0,25 / 0,30, opachi, `_Cull = 0`, nessuna keyword e nessuna texture.

`MAT_OfficeDoor_Panel.mat` e `MAT_OfficeDoor_Handle.mat` — **Ricreati (2026-11-23, Task 15)** per il nuovo `MESH_OfficeDoor.fbx`: URP Lit opachi, `Metallic = 0`, `_Surface = 0`, `_AlphaClip = 0`, `_Cull = 2`, queue 2000. `Panel` usa `Blanc.png` (smoothness 0,35) su `Matériau`/`Matériau.003`; `Handle` usa `NoirGris.jpeg` (smoothness 0,45) su `Matériau.001`/`Matériau.002`. Nessuna normal/metallic/AO map (non disponibili). I vecchi materiali `Door`/`Doorway` del modello precedente sono stati rimossi.

Non esistono altri materiali nel gruppo.

Verifiche del 2026-11-22:
- `MESH_Lamp.prefab`: `Cube` → `MAT_Lamp`; `Symmetry` e `Symmetry_1` → `MAT_LampLight`. Prima della correzione `Symmetry` usava il materiale embedded `Lit` senza texture.
- La texture `TEX_Lamp_MetallicSmoothness.png` è stata generata da `Lamp_DefaultMaterial_Metallic` (R) e `Lamp_DefaultMaterial_Roughness` (A = 1 - roughness); i file sorgente originali non sono stati modificati.
- Anteprima renderizzata in Editor: corpo in pietra chiara e due strisce diffusori emissive coerenti. Nessuna prova su Quest.
- `MESH_Chair.prefab`: nel Task 2 tutti i 33 renderer avevano materiali del gruppo (`Box02`: `MAT_Chair_Leather` + `MAT_Chair_Fabric`; `Cylinder01`: `MAT_Chair_Plastic` + `MAT_Chair_Metal`). Dopo il Task 3 i renderer sono 5 e i materiali sono assegnati direttamente alle mesh unite. I colori uniformi senza texture riprendono quelli dei materiali embedded.
- Anteprima renderizzata in Editor prima/dopo: lo schienale è ora opaco con la trama a pois visibile; prima risultava traslucido. Nessuna prova su Quest.

### 3.7 Texture disponibili e stato di import

Percorso: `Art/Textures/Man_Down/`

- Tutte le texture sono importate con `Texture Type = Default`, `sRGB = True`, `max size = 2048`, compressione attiva, mipmap attive.
- Eccezioni corrette verificate: `Lamp_DefaultMaterial_Normal.png`, `APC_Fire_extinguisher_Normal.png`, `fire_Normal.tga.png`, `sprinkler_Normal.tga.png`, `norm1a.png`, le 6 normal map dell'Employee (`AvatarBodyMale_Normal1_1K`, `AvatarEyes_Normal_512`, `AvatarHeadMale_Normal1_1K`, `AvatarTeeth_Normal1_1K`, `SEVAN_Normal_1K`, `shirt_Olympus_Normal_1K`) e le 2 di OfficeDoor (`Normal_DirectX`, `Normal_DirectX_4`, entrambe con `flipGreenChannel = True` perché etichettate DirectX) sono `NormalMap` (`sRGB = False`); le normal map FireAlarm e del Printer usano `flipGreenChannel = False` (convenzione OpenGL, da verificare visivamente).
- Maschere combinate `Default` con `sRGB = False` e alpha da input: `TEX_Lamp_MetallicSmoothness.png`, `TEX_FireExtinguisher_MetallicSmoothness.png`, `TEX_FireAlarm_MetallicSmoothness.png`, `TEX_Sprinkler_MetallicSmoothness.png`, `TEX_Printer_MetallicSmoothness.png`. Le maschere MS della vecchia OfficeDoor sono state rimosse insieme al vecchio modello/texture (Task 15).
- Normal map di tutti i modelli sistemati: corrette. Restano eventuali sole texture del Desk.
- Le mappe metallic/roughness/AO sorgente sono importate sRGB: per l'uso come maschere vanno generate versioni lineari dedicate.
- Esistono sette maschere combinate metallic + smoothness: le quattro già elencate, `TEX_FireAlarm_MetallicSmoothness.png`, `TEX_Sprinkler_MetallicSmoothness.png` (Task 10) e `TEX_Printer_MetallicSmoothness.png` (Task 11, R = 0 perché non esiste una mappa metallica). Tutte usano A = smoothness; URP Lit si aspetta metallic in R e smoothness in A di `_MetallicGlossMap`. Per gli altri modelli va creata quando si faranno i materiali.

Dettaglio per cartella:

| Cartella | File | Note |
|---|---|---|
| `TEX_Chair/` | `chairback.png` | Tessuto grigio a pois; non è una normal map. |
| `TEX_Clock/` | `Clock.jpeg` | Da verificare la corrispondenza UV prima dell'uso. |
| `TEX_Desk/` | `GR_PaleteColors.jpg`, `PaperTEX.png` | `GR_PaleteColors` è una **palette colori di riferimento**, non una texture UV: non usata. `PaperTEX` è carta con righe assegnata a `MAT_Desk_Paper` nel Task 12. |
| `TEX_Employee/` | 15 file: `Avatar*`, `pants_SEVAN_Color`, `SEVAN_Normal`, `shirt_Olympus_*`, `sneakers_SEVAN_Color` | Il FBX non referenzia texture (unica dipendenza: `Lit.shader`): vanno assegnate a mano. Mancano roughness e metallic: non inventarli. Il ruolo di `SEVAN_Normal_1K` (pantaloni, scarpe o entrambi) è da confermare. |
| `TEX_FireAlarm/` | `fire_*`, `sprinkler_*` (BaseColor, Normal, Metalness, Roughness, AO) + `TEX_FireAlarm_MetallicSmoothness.png`, `TEX_Sprinkler_MetallicSmoothness.png` | Set completo per allarme e sprinkler. Normal map riconfigurate come `NormalMap`/sRGB off; maschere lineari generate nel Task 10. |
| `TEX_FireExtinguisher/` | `APC_Fire_extinguisher_*` (BaseColor, Normal, Metallic, Roughness, AO), `TEX_FireExtinguisher_MetallicSmoothness.png` | Set completo; la maschera combinata è stata generata da Metallic (R) e Roughness (A = 1 - roughness) senza modificare i sorgenti. |
| `TEX_Lamp/` | `Lamp_DefaultMaterial_*`, `loo.png`, `TEX_Lamp_MetallicSmoothness.png` | `loo.png` è la maschera del paralume, usata come emissione; la maschera combinata è stata generata dai due mappe sorgente. |
| `TEX_OfficeDoor/` | `Blanc.png`, `NoirGris.jpeg` | Texture del nuovo modello porta (Task 15). Mappatura confermata dall'utente: `Blanc` → pannello (`Matériau`, `Matériau.003`), `NoirGris` → maniglia/parti metalliche (`Matériau.001`, `Matériau.002`). Le vecchie texture del modello precedente non sono più presenti. |
| `TEX_Printer/` | `TI24_maq.png`, `TI24_maqRR.png`, `norm1a.png`, `TEX_Printer_MetallicSmoothness.png` | Mappatura confermata dall'utente nel Task 11: `TI24_maq` = BaseColor, `TI24_maqRR` = Roughness, `norm1a` = NormalMap (OpenGL). Maschera lineare generata da `1 − Roughness`. |

**Regola:** non indovinare lo slot di una texture incerta. Se la mappatura non è verificata, fermarsi e chiedere.

### 3.8 Altro contenuto del gruppo (aggiornato 2026-11-24)

- `Audio/Music/` e `Audio/Sound/`: **vuote** (solo `PLACEHOLDER.txt`). Nessun audio per 112, DAE o feedback.
- `Code/Scripts/ManDown/`: contiene `ManDownScenario.cs`, `ManDownScenarioTypes.cs` e `ManDownNpc.cs` (solo coscienza); `Code/Shaders/` resta **vuota**. Nessun input XR, raycast o UI Canvas/TMP propria.
- `UI/`: **vuota**.
- `Art/Models/Man_Down/` contiene i modelli storici elencati sopra **e** il modello del DAE (`MESH_AED.obj`, texture `TEX_AED/AED2.png`) usato dal prefab `MESH_AED.prefab`.
- **Il DAE esiste già** in scena e tra i prefab del gruppo: la vecchia riga "non esistono asset per DAE" è superata.
- Non esistono asset per pozzanghera, panno, finestre dedicate o materiale audio. La pozzanghera è comunque **esclusa** per direttiva TASK 00 (§0 e §7.4).

### 3.9 Sistemi condivisi: cosa c'è e cosa manca

**Disponibile in `Shared/` (sola lettura, da istanziare o copiare secondo README):**

- Modelli d'ufficio: `MESH_Telefono.fbx`, `MESH_Poltrona.fbx`, `MESH_Armadio.fbx`, `MESH_Banco.fbx`, `MESH_Puf.fbx`, `MESH_Tavolino.fbx`, `MESH_RilevatoreDiGas.fbx`.
- Modelli stanza/ufficio: `SCRIVANIA_CRISTALLO_ONEDRIVE.fbx`, `TV_DA_UFFICIO_ONEDRIVE.fbx`.
- Moduli stanza per Chemical Alert (pareti, pavimento, soffitto, porte, finestre).
- Varie casse, barili, camion, ventola, soda caustica.

**Disponibile e verificato nel progetto reale (aggiornamento TASK 01, 2026-11-25):**

- Kit condiviso: `SI81.Input.InputService`, `SI81.Rig.SpawnPoint`, `RigRegistration`, `RigLocomotion`, `SI81.Interaction.Interactable`, `RayInteractor`, `SI81.UI.ScenarioController`, `RigUIBridge`, `MissionDefinition`, `MissionCatalog` e `UIScreenManager` sono presenti e risolvono in compilazione.
- `Assets/_Project/Home/Level/Scenes/SCN_Core.unity` contiene l'unico rig/camera/UI condivisi; `RigDirectPlay` lo carica automaticamente in Play Mode quando la scena di scenario ha uno spawn point. `SO_Mission_ManDown.asset` esiste ed è riutilizzato senza duplicazione.
- Non esiste un rig locale in Man Down: non creare un'altra camera, un altro EventSystem, un altro `InputService` o un altro sistema di raycast/selezione/grab.

**Implementato in TASK 02/TASK 03:**

- Controller logico `ManDownScenario`: condizioni iniziali, fasi non rigide, registro azioni con timestamp, API/eventi per le classi future, ricezione di risultati RCP grezzi, esiti e ID approvati. Non crea UI, input o interazioni.
- **TASK 03:** `ManDownNpc` collega la sola verifica della coscienza al controller tramite `Interactable`/`LabelAnchor` condivisi; non conserva stato locale e non tocca respiro, posa, telefono, RCP o DAE.
- Punteggio e debriefing: il controller produce un risultato provvisorio e congela l'esito una sola volta; il calcolo definitivo delle cinque dimensioni e lo SPI comune restano da integrare.

**Non ancora implementato per Man Down:**

- `ManDownPhone`, `ManDownCpr`, `ManDownAed` e il rilevamento fisico di compressioni/elettrodi: useranno `Interactable` + `RayInteractor` condivisi e le API già esposte dal controller. La respirazione e la posa laterale di `ManDownNpc` restano da implementare nel task dedicato.
- UI specifica della missione e chiamate UI dedicate: il task UI resta separato; la UI comune è già presente ma non è stata personalizzata.
- Pozzanghera e panno: **esclusi** per direttiva TASK 00, non da implementare.
- Audio (musica, ambience, SFX, istruzioni DAE, chiamata 112) e interfaccia telefonica per la chiamata simulata al 112.
- Parametri didattici configurabili (ampiezza/soglie RCP, fase finale, `T_target`/`T_max`): da validare, non da inventare.

Le vecchie righe del 2026-11-24 che indicavano kit `SI81.UI`, rig, interazione e `SCN_Core` come assenti sono superate: il documento `Docs/SharedPlayerRig.md` resta un riferimento storico, ma il sistema reale è ora verificato nel codice condiviso.

---

## 4. Regole per materiali e texture

### 4.1 Pipeline e shader

- Solo `Universal Render Pipeline/Lit`.
- Salvataggio in `Art/Materials/Man_Down/<Oggetto>/`, seguendo la convenzione esistente della cartella `Lamp/`.
- Nomi: `MAT_<Oggetto>_<Materiale>`, es. `MAT_OfficeChair_Fabric`.

### 4.2 Slot URP

- Base color → `_BaseMap` (sRGB).
- Normal → `_BumpMap` (texture importata come `NormalMap`, `sRGB = False`).
- Metallic + Smoothness → `_MetallicGlossMap`: **metallic in R, smoothness in A**.
- Occlusion → `_OcclusionMap` (lineare).
- `_Cull`: in `UnityEngine.Rendering.CullMode` **0 = Off (doppia faccia)**, 1 = Front, 2 = Back. Per mesh a una sola faccia usare 0 e verificare anche il lato opposto (vedi Task 4).
- **Non mettere mai la roughness nello slot Metallic.** In URP si usa la smoothness, non la roughness.
- Se non esiste una maschera combinata, non improvvisare: o si crea una maschera dedicata, o si usano valori uniformi motivati.
- Emissione: assegnare una `_EmissionMap` reale e colori coerenti; non abilitare `_EMISSION` senza mappa.

### 4.3 Casi da verificare prima di creare materiali

1. `MESH_OfficeDoor`: il vecchio modello 948 tris è stato **sostituito dall'utente** il 2026-11-23 con un nuovo `MESH_OfficeDoor.fbx` (2100 tris, 4 materiali Matériau). La vecchia mappatura texture è obsoleta; la nuova mappatura è stata confermata dall'utente nel Task 15 (`Blanc`=pannello, `NoirGris`=maniglia).
2. `MESH_Printer`: ruolo confermato dall'utente nel Task 11 (`TI24_maq` = colore, `TI24_maqRR` = roughness); mappatura completata.
3. `MESH_Clock`: verificare se `Clock.jpeg` è realmente l'albedo UV — **risolto nel Task 7a**: è l'albedo del quadrante (UV circolari 0,096..0,904 e texture con quadrante circolare).
4. `MESH_Desk`: rimuovere/ignorare la trasparenza errata solo dopo decisione esplicita; la correzione riguarda i materiali embedded o nuovi materiali, non il modello originale.
5. `MESH_Employee`: mancano roughness e maschera metallic/smoothness; non inventare gli slot mancanti.

### 4.4 Materiali: stato sintetico

- **Funzionanti:** i due materiali della Lamp, i cinque della Chair, `MAT_FireExtinguisher`, i 12 `MAT_Employee_*`, `MAT_Clock`, i 2 `MAT_TablePrinter_*`, i 2 `MAT_OfficeDoor_*`, i 2 `MAT_FireAlarm_*`, `MAT_Printer` e i 19 `MAT_Desk_*` (creati/sistemati il 2026-11-22; verificati in Editor, non su Quest).
- **Da correggere:** nessun materiale del gruppo noto come problematico dopo gli interventi su Lamp, Chair, FireExtinguisher ed Employee.
- **Da creare:** nessun materiale rimasto; tutti i modelli del gruppo hanno materiali dedicati.
- **Problema di rendering noto residuo:** trasparenza errata su tutti i 19 materiali del Desk. Quella di `Chairback` è stata risolta nel prefab della Chair.

---

## 5. Convenzioni di naming

| Tipo | Convenzione | Esempio |
|---|---|---|
| Materiale | `MAT_NomeOggetto_Materiale` | `MAT_OfficeChair_Fabric` |
| Prefab | `MESH_NomeOggetto` | `MESH_ManDown_DAE` |
| Texture nuova | `TEX_NomeOggetto_Tipo` | `TEX_OfficeDoor_BaseColor` |
| Scena | `SCN_NomeScena` | `SCN_ManDown` (rinominata in TASK 01, GUID preservato) |
| Script / classi C# | `PascalCase` | `ManDownScenarioController` |
| Metodi / proprietà pubbliche | `PascalCase` | `StartScenario()` |
| Campi privati/protetti | `camelCase` | `private float elapsedTime;` |
| Costanti | `UPPERCASE` | `MAX_RCP_INTERACTIONS` |
| Suoni | `SFX_` / `MUS_` secondo README | `SFX_DAE_Shock` |

Regole aggiuntive:

- Prefissi in maiuscolo + underscore, nome descrittivo in PascalCase.
- Nomi tecnici in inglese, senza spazi, accenti o caratteri speciali.
- Non rinominare i file importati se questo può rompere riferimenti; per i nuovi asset usare la convenzione.
- **Decisione esplicita del 2026-11-22:** d'ora in poi **tutti** i prefab, nuovi e derivati da modelli, usano il prefisso `MESH_` (non più `PFB_`). L'utente ha già rinominato `PFB_FireExtinguisher.prefab` → `MESH_FireExtinguisher.prefab` e `PFB_Employee.prefab` → `MESH_Employee.prefab`; il documento è stato allineato a quei nomi verificati su disco.

---

## 6. Stato XR e regole di integrazione

- OpenXR e Meta XR Core SDK sono installati e il **player condiviso è presente in `SCN_Core`**: non crearne uno locale.
- La scena Man Down contiene solo contenuto di scenario, i marker `MNG_SpawnPoint` e `MNG_Scenario` e la zona di presa `MESH_ShoulderGrip` con componenti condivisi; rig, camera XR, locomozione, EventSystem, input, raycast, grab e interazioni arrivano dal sistema comune. Nessun Canvas, TMP o HUD locale.
- Tutta l'input XR futura deve passare da `SI81.Input.InputService` e dalle interazioni condivise: niente `OVRInput`, Input Actions locali, XRI o raycast/selezione/grab duplicati.
- In Play Mode la camera abilitata è `CenterEyeAnchor` di `SCN_Core`; Man Down non ha una `Main Camera` XR propria.
- Il caricamento del core in Editor è gestito da `RigDirectPlay` e `loadCoreInEditor` resta `false`: con `true` si generano due `SCN_Core`, due `RigRegistration` e due `MNG_UI`.
- La validazione fisica su Meta Quest 3 non è stata eseguita: movimento, rotazione, pose controller, haptics e presa/selezione reali restano test manuali (§0.3 e procedura in `TASK_PROGRESS.md`).

---

## 7. Discrepanze documentali verificate

Non risolvere queste ambiguità da soli.

### 7.1 Numerazione Bad Ending

| Condizione | Flowchart | GDD specifico |
|---|---|---|
| Avvicinamento in ambiente pericoloso | Bad Ending 1 — esposizione al pericolo | Pozzanghera opzionale: penalizzazione, nessun Bad Ending dedicato |
| Mancata chiamata al 112 | Bad Ending 2 | Bad Ending 1 |
| Mancata RCP quando necessaria | Bad Ending 3 | Bad Ending 2 |
| DAE presente ma ignorato | Bad Ending 4 | Bad Ending 3 |

**Indicazione operativa:** usare identificatori semantici interni, non numeri; concordare la numerazione mostrata al giocatore.

**Aggiornamento Task 00e:** la numerazione della tabella è storica e superata: usare gli ID del §0.2 (`EMERGENCY_NOT_CALLED`, `CPR_OMITTED`, `AED_OMITTED`, `CPR_POOR_QUALITY`, `AED_INCORRECT_USE`, `TIMEOUT`, `LOW_SCORE`). Le descrizioni italiane restano demandate all'integrazione UI.

**Nota TASK 00:** la riga "avvicinamento in ambiente pericoloso" è **esclusa** da qualsiasi implementazione: non creare pozzanghera, danno o Bad Ending dedicato. Le etichette dei tre errori critici sono ora sostituite dagli ID semantici del §0.2.

### 7.2 Altre discrepanze / lacune

1. `FlowChart_ManDown.pdf` contiene anche un diagramma estraneo (pressa/PPE/estintore) non pertinente al flusso Man Down.
2. Il timer della difficoltà **Guidato** non è definito nel GDD Man Down.
3. Il GDD generale usa `Guidato / Realistico / Stress`; la guida HUD usa `Guided / Operational / Crisis`. Mappatura da concordare.
4. "Attivare il primo soccorso aziendale" è citato nelle specifiche ma non ha una meccanica definita.
5. Non sono definite le probabilità di respirazione anomala e di disponibilità del DAE (la pozzanghera è esclusa in TASK 00).
6. Non è definito quando un'omissione diventa definitiva (timeout, numero di interazioni, passaggio di fase).
7. Non è definito se il pericolo ambientale grave causi fallimento immediato o penalizzazione: domanda **irrilevante** perché il pericolo ambientale è escluso in TASK 00 (§0 e §7.4).
8. Non è definito il criterio di completamento di RCP/DAE né il punteggio per successo parziale.

**Aggiornamento Task 00e (2026-11-25):** i punti 4, 5, 6 e 8 sono risolti dalle decisioni approvate nel §0.2. Il punto 7 è irrilevante perché il pericolo ambientale è escluso. Restano come riferimento storico solo i punti 1–3 (discrepanze tra PDF), da non usare come regole operative.

### 7.3 Domande aperte — stato

**Risolte dalle decisioni approvate:** numerazione/etichette dei Bad Ending (ora ID semantici), condizioni di timeout e arrivo soccorsi, criterio DAE ignorato, regole di successo parziale e penalità recuperabili, meccanica del primo soccorso aziendale (esclusa), probabilità delle variabili randomiche, durata Realistica (180 s), conteggi RCP e limiti alla mancata chiamata.

**Ancora necessari solo per la validazione didattica, non per scrivere la struttura del codice:** valori di ampiezza/soglia di rilascio/tolleranza di frequenza della RCP, durata della breve fase finale di assistenza, `T_target`/`T_max` del punteggio Time, scelta finale tra punteggio ponderato di Man Down e SPI comune durante l'integrazione. I campi saranno configurabili: non inventare valori clinici.

### 7.4 Decisioni già prese dal gruppo

1. **Pozzanghera / pericolo ambientale: escluso del tutto** (aggiornamento TASK 00, 2026-11-24). Niente pozzanghera, panno, rischio di scivolamento, danno, penalità Safety o Bad Ending dedicato. La precedente decisione "solo penalizzante e opzionale" è superata; Safety resta 5/5.
2. **Unica modalità implementata: Realistica, 180 s.** Il precedente punteggio GDD 30/10/0 è superato dalle soglie approvate 80/50. Guidata e Stress non si implementano ora, ma i parametri restano configurabili per future estensioni. **Decisione operativa TASK 03:** in questa migrazione `SO_Mission_ManDown` resta invariato e il timer è solo quello comune; `HUDRoot.BeginFromSession()` usa `mission.GetTimeLimit()` (Operational 600 s, override 0) e `ManDownScenario` non avvia né riavvia timer. I 180 s restano la durata di gameplay approvata, da applicare solo con una decisione esplicita sull'asset missione.
3. **Ulteriori decisioni approvate:** elenco completo nel §0.2 e in `TASK_PROGRESS.md`; in caso di conflitto, prevalgono sul GDD specifico e sul flowchart.

---

## 8. Vincoli tecnici noti

- `MESH_Chair` resta pesante (93.153 tris): il Task 3 ha ridotto i renderer da 33 a 5 senza cambiare la geometria; la decimazione dei triangoli non è stata eseguita.
- `MESH_Desk` importa Camera e Light: verificare se disattivarli nelle istanze o correggere l'import in un task dedicato.
- `MESH_Lamp` importa una Camera.
- La trasparenza errata di `Chairback` è risolta nel prefab; resta quella dei 19 materiali del Desk.
- Le normal map non-Lamp vanno reimportate come `NormalMap` prima di usarle.
- Le maschere metallic/roughness/AO vanno importate lineari.
- Esiste una maschera metallic+smoothness pronta per la Lamp; per gli altri modelli va creata.

---

## 9. Checklist prima di chiudere un task

- [ ] Ho lavorato solo in `Assets/_Project/Groups/G_Marius/`, salvo autorizzazioni esplicite registrate (TASK 01: `ProjectSettings/EditorBuildSettings.asset`).
- [ ] Non ho modificato `Shared`, altri gruppi, `Packages`, XR o scene globali; ogni modifica auto-generata non intenzionale è stata ripristinata.
- [ ] Non ho creato player/XR Origin/input/UI/scoring duplicati: uso solo il sistema condiviso di `SCN_Core`.
- [ ] I nomi rispettano le convenzioni.
- [ ] I materiali usano URP Lit e la smoothness è nello slot corretto.
- [ ] Non ho indovinato slot di texture incerti.
- [ ] Ho salvato il progetto nell'Editor dopo le modifiche importanti e alla fine del task (§1.4).
- [ ] Ho verificato in Play Mode (e su Quest se possibile).
- [ ] Ho aggiornato il §10 con solo modifiche reali e verificate.
- [ ] Ho lasciato invariati i file preesistenti non miei.

---

## 10. Registro dei task

> Aggiornare **solo** con file realmente creati/modificati, comportamento implementato, verifiche eseguite e problemi aperti.
> Non segnare come completato ciò che non è stato testato.
> I record storici descrivono lo stato **al momento di quel task**; per lo stato attuale vedere l'header e il record TASK 03.

### Task 0 — Analisi iniziale e creazione di AGENTS.md

- **Data:** 2026-11-22
- **Obiettivo:** esplorare il progetto, leggere i documenti, analizzare modelli/texture/materiali, creare questo file.
- **File creati:**
  - `Docs/Man_Down/AGENTS.md`
- **File modificati:** nessun altro.
- **Verifiche eseguite:**
  - Struttura reale delle cartelle del gruppo.
  - Contenuto di `SCN_Man_Down`, `prefab_scena`, `MESH_Chair`, `MESH_Lamp`.
  - Conteggio mesh/triangoli e materiali embedded di tutti i 10 modelli.
  - Proprietà di `MAT_Lamp` e `MAT_LampLight`.
  - Impostazioni di import di tutte le texture del gruppo.
  - Presenza di script, package XR e sistemi condivisi.
  - Lettura di `Man_Down_Specifiche.md`, dei tre PDF di scenario e della guida HUD comune.
- **Risultato:** nessuna funzionalità di gameplay implementata; solo documentazione.
- **Problemi aperti:** quelli elencati nel §7.
- **Prossimo passo (eseguito):** intervento sulla Lamp, registrato nel Task 1.

### Task 1 — Materiali e prefab della Lamp

- **Data:** 2026-11-22
- **Obiettivo:** correggere i materiali della Lamp e completare l'assegnazione dei materiali nel prefab.
- **File creati:**
  - `Art/Textures/Man_Down/TEX_Lamp/TEX_Lamp_MetallicSmoothness.png` (R = metallic, A = 1 - roughness; generata dalle mappe sorgente senza modificarle)
- **File modificati:**
  - `Art/Materials/Man_Down/Lamp/MAT_Lamp.mat`
  - `Art/Materials/Man_Down/Lamp/MAT_LampLight.mat`
  - `Level/Prefabs/Man_Down/MESH_Lamp.prefab` (una sola override aggiunta)
- **Comportamento implementato:**
  - `MAT_Lamp`: base color + normal + maschera combinata; `_Metallic = 1`, `_Smoothness = 1`, `_SmoothnessTextureChannel = 0`.
  - `MAT_LampLight`: `loo.png` passato da `_BaseMap` a `_EmissionMap`; base color bianco caldo, emissione bianca calda, superficie opaca.
  - Prefab: `Cube` → `MAT_Lamp`; `Symmetry` → `MAT_LampLight` (prima usava il materiale embedded `Lit`); `Symmetry_1` → `MAT_LampLight`.
- **Verifiche eseguite:**
  - Rilettura in Editor di tutte le proprietà dei due materiali.
  - Confronto dei tre file modificati con copie di backup: solo le righe attese sono cambiate.
  - Anteprime renderizzate in Editor da più angolazioni: corpo chiaro e due strisce diffusori emissive.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Problemi aperti:** invariati (§7.3). Il modello conserva 1 Camera importata, non rimossa.
- **Decisioni registrate (storiche):** pozzanghera solo penalizzante e opzionale; scenario da realizzare partendo da Realistico. **Superato il 2026-11-25:** la pozzanghera è esclusa del tutto e Realistico ha timeout 180 s con soglie 80/50 (§0.2 e §7.4).
- **Prossimo passo (eseguito):** intervento sulla Chair, registrato nel Task 2.

### Task 2 — Materiali e prefab della Chair

- **Data:** 2026-11-22
- **Obiettivo:** sostituire i materiali embedded della sedia, correggere la trasparenza di `Chairback` e completare l'assegnazione nel prefab.
- **File creati:**
  - `Art/Materials/Man_Down/Chair/MAT_Chair_Fabric.mat`
  - `Art/Materials/Man_Down/Chair/MAT_Chair_Leather.mat`
  - `Art/Materials/Man_Down/Chair/MAT_Chair_Plastic.mat`
  - `Art/Materials/Man_Down/Chair/MAT_Chair_Metal.mat`
  - `Art/Materials/Man_Down/Chair/MAT_Chair_Chrome.mat`
- **File modificati:**
  - `Level/Prefabs/Man_Down/MESH_Chair.prefab` (override materiali sui 33 renderer)
- **Comportamento implementato:**
  - Tutti i materiali sono `Universal Render Pipeline/Lit` e opachi.
  - `MAT_Chair_Fabric` usa `chairback.png` come BaseMap; gli altri usano colori uniformi ripresi dai materiali embedded, con metallic/smoothness coerenti al tipo di materiale.
  - Mapping nel prefab: `Lether` → `MAT_Chair_Leather`; `Chairback` → `MAT_Chair_Fabric`; `Matteplastic` → `MAT_Chair_Plastic`; `BlackMetal` → `MAT_Chair_Metal`; `Chrome` → `MAT_Chair_Chrome`.
  - `Box02` e `Cylinder01` hanno due slot: entrambi sono stati remappati.
- **Verifiche eseguite:**
  - Rilettura in Editor di shader, parametri e assegnazioni dei cinque materiali.
  - Conteggio degli override nel prefab: 33 renderer coperti, 5 materiali usati.
  - Confronto del prefab con una copia di backup: solo le override materiali sono state aggiunte.
  - Anteprime Editor prima/dopo: schienale opaco con trama a pois visibile; la versione precedente era traslucida.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Problemi aperti:** il modello resta pesante (93.153 tris); la geometria è stata poi unita nel Task 3. I colori uniformi senza texture sono scelte tecniche provvisorie, non texture mancanti da inventare.
- **Prossimo passo (eseguito):** ottimizzazione dei draw call della Chair, registrata nel Task 3.

### Task 3 — Unione delle mesh della Chair

- **Data:** 2026-11-22
- **Obiettivo:** ridurre i draw call della sedia da 33 a 5 senza alterare la geometria visibile.
- **Decisione:** il FBX originale non è stato modificato. Il prefab `MESH_Chair.prefab`, prima variante del modello importato, è diventato un prefab autonomo con 5 figli `Merged_*`; nessun altro asset o scena referenziava il vecchio prefab (verificato per GUID).
- **File creati:**
  - `Art/Models/Man_Down/MESH_Chair_Optimized/MESH_Chair_Fabric.asset`
  - `Art/Models/Man_Down/MESH_Chair_Optimized/MESH_Chair_Leather.asset`
  - `Art/Models/Man_Down/MESH_Chair_Optimized/MESH_Chair_Metal.asset`
  - `Art/Models/Man_Down/MESH_Chair_Optimized/MESH_Chair_Plastic.asset`
  - `Art/Models/Man_Down/MESH_Chair_Optimized/MESH_Chair_Chrome.asset`
- **File modificati:**
  - `Level/Prefabs/Man_Down/MESH_Chair.prefab` (5 renderer invece di 33)
- **Comportamento implementato:**
  - Le mesh sono state unite per materiale con bake delle trasformazioni; UV, normali e tangenti preservati. `Box02` e `Cylinder01` sono stati separati per submesh prima dell'unione.
  - Vengono inclusi solo i vertici effettivamente usati dai triangoli: 54.818 vertici totali contro 58.818 del FBX.
  - I 5 renderer usano i materiali del Task 2 e conservano `shadowCastingMode` / `receiveShadows`.
  - Triangoli invariati: 93.153. Formato indici `UInt16` per tutte e 5 le mesh.
- **Verifiche eseguite:**
  - Conteggio renderer: 5 (5 submesh totali); triangoli 93.153; bounds identici prima/dopo (centro e dimensione, delta zero).
  - Anteprime Editor a 800x600 confrontate pixel per pixel con la versione pre-ottimizzazione: differenza media 0,0031/255, 12 pixel con differenza >2 su 480.000.
  - Backup del prefab e delle mesh salvati in temp prima delle modifiche.
- **Trade-off noto:** gli asset mesh non compressi occupano ~6,78 MB contro i ~3,10 MB del FBX originale; il guadagno è nei draw call, non nella memoria o nei triangoli.
- **Non verificato:** Frame Debugger/Profiler a runtime, Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Problemi aperti:** decimazione (93.153 tris) e LOD non eseguiti: richiedono uno strumento di semplificazione non disponibile (Blender assente).
- **Prossimo passo (eseguito):** correzione della faccia nascosta di `MAT_Chair_Fabric`, registrata nel Task 4.

### Task 4 — Correzione del lato nascosto di `MAT_Chair_Fabric`

- **Data:** 2026-11-22
- **Segnalazione:** lo schienale in tessuto sembrava trasparente visto da un lato.
- **Causa verificata:** `MAT_Chair_Fabric.mat` aveva `_Cull = 2`, che in `UnityEngine.Rendering.CullMode` significa **Back** (non "Off": `Off = 0`). La mesh `MESH_Chair_Fabric.asset` ha 389 vertici con normale +X e 0 con normale -X: è una superficie a una sola faccia, quindi il lato opposto veniva cullato. Non era trasparenza del materiale: `_Surface = 0`, queue 2000, nessuna keyword di trasparenza, texture `chairback.png` con alpha sempre 255 e formato DXT1 senza alpha.
- **File modificato:** `Art/Materials/Man_Down/Chair/MAT_Chair_Fabric.mat` (`_Cull` da 2 a 0).
- **Verifiche eseguite:**
  - Render close-up dello schienale su sfondo magenta da entrambi i lati: prima 160.000/160.000 pixel di sfondo dal lato -X, dopo 160.000/160.000 pixel solidi da entrambi i lati.
  - Render completo della sedia dall'angolazione standard confrontato con quello pre-correzione: differenza media 0,0031/255, 12 pixel su 480.000 con differenza > 2 (nessuna regressione).
  - Rilettura del materiale dopo il salvataggio: `_Cull = 0`, `_Surface = 0`, queue 2000, nessuna keyword di trasparenza.
- **Nota:** gli altri quattro materiali della Chair hanno normali bilanciate sui due lati (es. Leather 5.427/+X e 5.525/-X), quindi non presentano lo stesso difetto; non sono stati modificati.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Prossimo passo (eseguito):** materiali e prefab del FireExtinguisher, registrati nel Task 5.

### Task 5 — Materiale e prefab del FireExtinguisher

- **Data:** 2026-11-22
- **Obiettivo:** creare il prefab dell'estintore con materiali URP e texture del gruppo.
- **File creati:**
  - `Art/Textures/Man_Down/TEX_FireExtinguisher/TEX_FireExtinguisher_MetallicSmoothness.png` (R = metallic, A = 1 - roughness, 2048x2048, `sRGB = False`)
  - `Art/Materials/Man_Down/FireExtinguisher/MAT_FireExtinguisher.mat`
  - `Level/Prefabs/Man_Down/MESH_FireExtinguisher.prefab`
- **File modificati:**
  - `Art/Textures/Man_Down/TEX_FireExtinguisher/APC_Fire_extinguisher_Normal.png.meta`: `textureType` 0 → 1 (`NormalMap`), `sRGBTexture` 1 → 0; backup del meta in temp.
- **Comportamento implementato:**
  - `MAT_FireExtinguisher`: BaseColor, Normal, maschera combinata e AO (`_OcclusionStrength = 1`); `_Metallic = 1`, `_Smoothness = 1`, `_SmoothnessTextureChannel = 0`, opaco, `_Cull = 0`.
  - Keyword attive: `_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`. Nessuna keyword di trasparenza; queue 2000.
  - Prefab autonomo con 3 renderer (`HandleLeaver_low.001`, `Main_low.001`, `Tube_low.001`), 2.834 tris, tutti con `MAT_FireExtinguisher`; nessuna Camera o Light importata nel modello.
  - FBX originale invariato.
- **Verifiche eseguite:**
  - Maschera generata da Metallic (media 56,2) e Roughness (media 89,6): smoothness media 165,4, range metallic 0..255, smoothness 0..239.
  - Rilettura in Editor di shader, parametri, texture e keyword del materiale; rilettura delle impostazioni di import della normal map e della maschera.
  - Confronto del `.meta` della normal map con la copia di backup: solo `textureType` e `sRGBTexture` sono cambiati.
  - Anteprime Editor da due angolazioni: modello solido e colorato (rosso), nessun buco o faccia mancante.
  - Progetto salvato con `AssetDatabase.SaveAssets()` a fine task.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Problemi aperti:** nessuno noto per questo modello.
- **Prossimo passo (eseguito):** micro-task 1 dell'Employee, registrato qui sotto.

### Task 6a — Employee: inventario e mapping (solo lettura)

- **Data:** 2026-11-22
- **Obiettivo:** verificare texture, slot e dipendenze prima di creare materiali. **Nessun asset modificato.**
- **Risultati verificati:**
  - `TEX_Employee/` contiene **15** texture, non 18 come scritto in precedenza: 9 color (di cui 2 cornea) + 6 normal.
  - `MESH_Employee.fbx` ha **12 SkinnedMeshRenderer**, 1 slot ciascuno, 54.285 tris totali; nessun MeshFilter. Nessuna Camera o Light.
  - `AssetDatabase.GetDependencies(fbx, false)` restituisce solo `Lit.shader`: il modello non referenzia alcuna texture, quindi l'assegnazione va fatta ex novo.
  - Tutte le 15 texture sono `Default` / `sRGB = True` (1024² o 512²).
- **Mapping proposto (12 slot):**
  | Slot FBX | BaseMap | BumpMap |
  |---|---|---|
  | `AvatarBody` | `AvatarBodyMale_Color_1K` | `AvatarBodyMale_Normal1_1K` |
  | `AvatarEyelashes` | nessuna (nero uniforme) | nessuna |
  | `AvatarHead` | `AvatarHeadMale_Color_1K` | `AvatarHeadMale_Normal1_1K` |
  | `AvatarLeftCornea` | `AvatarLeftCornea_Color_512` | nessuna |
  | `AvatarLeftEyeball` | `AvatarEyes_Color_512` | `AvatarEyes_Normal_512` |
  | `AvatarRightCornea` | `AvatarRightCornea_Color_512` | nessuna |
  | `AvatarRightEyeball` | `AvatarEyes_Color_512` | `AvatarEyes_Normal_512` |
  | `AvatarTeethLower` | `AvatarTeeth_Color_1K` | `AvatarTeeth_Normal1_1K` |
  | `AvatarTeethUpper` | `AvatarTeeth_Color_1K` | `AvatarTeeth_Normal1_1K` |
  | `outfit_bottom` | `pants_SEVAN_Color_1K` | `SEVAN_Normal_1K` (da confermare) |
  | `outfit_shoes` | `sneakers_SEVAN_Color_1K` | `SEVAN_Normal_1K` (da confermare) |
  | `outfit_top` | `shirt_Olympus_Color_1K` | `shirt_Olympus_Normal_1K` |
- **Decisione aperta:** `SEVAN_Normal_1K.jpeg` è l'unica normal SEVAN; non è possibile verificare se appartenga ai pantaloni, alle scarpe o a entrambi. Fino a conferma non va assegnata a `outfit_shoes` (né a `outfit_bottom`, se si preferisce attendere).
- **Prossimo micro-task (eseguito):** reimport delle normal map, registrato nel Task 6b.

### Task 6b — Employee: reimport delle normal map

- **Data:** 2026-11-22
- **Obiettivo:** importare correttamente le 6 normal map dell'Employee prima di creare i materiali.
- **File modificati (solo `.meta`, 6 file in `Art/Textures/Man_Down/TEX_Employee/`):**
  - `AvatarBodyMale_Normal1_1K.jpeg.meta`
  - `AvatarEyes_Normal_512.jpeg.meta`
  - `AvatarHeadMale_Normal1_1K.jpeg.meta`
  - `AvatarTeeth_Normal1_1K.jpeg.meta`
  - `SEVAN_Normal_1K.jpeg.meta`
  - `shirt_Olympus_Normal_1K.jpeg.meta`
- **Modifica applicata:** `textureType` 0 → 1 (`NormalMap`), `sRGBTexture` 1 → 0. Backup dei 6 meta originali in temp.
- **Verifiche eseguite:**
  - Rilettura dell'impostazione di import di tutti e 6 i file dopo `SaveAndReimport()`.
  - Confronto dei 6 meta con i backup: in ogni file sono cambiate solo `textureType` e `sRGBTexture`.
  - Nessun file di texture o FBX modificato.
  - Progetto salvato con `AssetDatabase.SaveAssets()` a fine micro-task.
- **Nota:** l'assegnazione di `SEVAN_Normal_1K` ai materiali resta sospesa in attesa di conferma; il reimport è neutrale rispetto a quella scelta.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Prossimo micro-task (eseguito):** creazione dei 12 materiali `MAT_Employee_*`, registrato nel Task 6c.

### Task 6c — Employee: creazione dei 12 materiali URP

- **Data:** 2026-11-22
- **Obiettivo:** creare i materiali del personaggio con le texture del gruppo, senza toccare l'FBX.
- **Decisione applicata:** `SEVAN_Normal_1K` assegnata a entrambi gli slot `outfit_bottom` e `outfit_shoes`, per conferma esplicita dell'utente ("entrambi per ora").
- **Cartella creata:** `Art/Materials/Man_Down/Employee/` con 12 file `MAT_Employee_<Slot>.mat`.
- **Proprietà comuni verificate:** shader `Universal Render Pipeline/Lit`, queue 2000, `_Surface = 0`, `_Cull = 0`, `_Metallic = 0`, `_SmoothnessTextureChannel = 0`, nessuna keyword di trasparenza. `_NORMALMAP` abilitata solo sugli 8 materiali con normal; `_BaseColor` nero solo su `AvatarEyelashes`, bianco sugli altri 11.
- **Valori di smoothness:** 0.30 Body/Head, 0.20 Eyelashes, 0.65 Cornea/Eyeball, 0.40 Teeth, 0.15 bottom/top, 0.25 shoes.
- **Verifiche eseguite:**
  - Rilettura in Editor dei 12 materiali dopo `AssetDatabase.SaveAssets()`: conteggio 12, texture e smoothness corrispondenti al mapping.
  - Nessuna texture, FBX o file fuori dal gruppo modificato in questo micro-task.
- **Non verificato:** Play Mode nella scena di gioco, prova su Meta Quest 3, e resa visiva del personaggio (si vedrà al Task 6d con il prefab e le anteprime).
- **Prossimo micro-task (eseguito):** creazione di `MESH_Employee.prefab`, registrato nel Task 6d.

### Task 6d — Employee: prefab con materiali URP

- **Data:** 2026-11-22
- **Obiettivo:** creare il prefab del personaggio con i 12 materiali creati nel Task 6c, senza toccare l'FBX.
- **File creato:** `Level/Prefabs/Man_Down/MESH_Employee.prefab` (istanza di `MESH_Employee.fbx` con override dei materiali; FBX originale invariato).
- **Override applicati (12/12):** ogni SkinnedMeshRenderer ha ricevuto il materiale `MAT_Employee_<nome renderer>` corrispondente; rilettura dal prefab salvato che conferma tutti i 12 assegnamenti.
- **Verifiche eseguite:**
  - 12 SkinnedMeshRenderer, **54.285 tris** (invariati rispetto al modello sorgente), 0 Camera, 0 Light.
  - Anteprime Editor da 8 angolazioni: il personaggio è visibile da tutte le direzioni; non c'è un problema di culling o facce mancanti.
  - Anteprime frontali (+Z) e posteriori (−Z) a 700×700: 76.753 e 77.494 pixel solidi su 490.000; colori medi (46,37,38) e (72,59,54).
  - Nessun file fuori dal gruppo modificato; progetto salvato con `AssetDatabase.SaveAssets()`.
- **Nota:** il primo tentativo di anteprima con FOV 30 e distanza 1,7× il lato maggiore ha prodotto un'immagine vuota per un parametro di camera; ripetuto con FOV 35 e distanza fissa 3,2 → resa corretta da entrambi i lati. Non era un difetto del modello.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Stato Employee:** completati inventario, reimport normal, 12 materiali e prefab. Nessun elemento di gameplay (animazioni, ragdoll, interazioni) è stato aggiunto.
- **Prossimo passo proposto (eseguito):** Clock, su indicazione esplicita dell'utente; vedi Task 7a.

### Task 7a — Clock: inventario e verifica albedo (sola lettura)

- **Data:** 2026-11-22
- **Obiettivo:** verificare modello e texture prima di creare materiale e prefab. **Nessun asset modificato.**
- **Risultati verificati:**
  - `MESH_Clock.fbx`: 1 renderer (`MESH_Clock`), mesh `Cylinder`, 1 submesh, 1 slot, **252 tris**, 384 vertici, 0 Camera, 0 Light.
  - UV range: u = 0,096..0,904, v = 0,096..0,904: compatibile con una texture circolare applicata su una faccia del cilindro.
  - Materiale embedded `Material.001`: `Universal Render Pipeline/Lit`, queue 2000, `_Color` (0,906, 0,906, 0,906, 1), **nessuna texture assegnata**.
  - `AssetDatabase.GetDependencies(fbx, false)` restituisce solo `Lit.shader`: il FBX non referenzia `Clock.jpeg`.
  - `TEX_Clock/Clock.jpeg`: sorgente 225×225, importata 256×256, `Default` / `sRGB = True` (corretto per un'albedo).
- **Verifica albedo:** analisi dei pixel della sorgente (luminanza media 221, 4.533 pixel scuri, 41.431 chiari) e mappa ASCII 24×12: compare un chiaro quadrante circolare con anello e tacche scure. `Clock.jpeg` è quindi l'albedo del quadrante; il dubbio sollevato in §4.3 punto 3 è risolto.
- **Prossimo micro-task (eseguito):** creazione di `MAT_Clock` e del prefab `MESH_Clock`, registrati nel Task 7b.

### Task 7b — Clock: materiale e prefab

- **Data:** 2026-11-22
- **File creati:**
  - `Art/Materials/Man_Down/Clock/MAT_Clock.mat`
  - `Level/Prefabs/Man_Down/MESH_Clock.prefab` (nome con prefisso `MESH_*` secondo la convenzione aggiornata)
- **Materiale verificato:** shader `Universal Render Pipeline/Lit`, `_BaseMap` = `Clock.jpeg`, `_BaseColor` bianco, `_Metallic = 0`, `_Smoothness = 0.35` (valore uniforme motivato: quadrante stampato, leggermente lucido; non esistono mappe metallic/roughness), `_SmoothnessTextureChannel = 0`, opaco, queue 2000, `_Cull = 0`, nessuna keyword attiva.
- **Prefab verificato:** 1 renderer, mesh `Cylinder`, **252 tris**, 0 Camera, 0 Light, materiale `MAT_Clock`; FBX originale invariato.
- **Verifiche di resa:** anteprime da 6 direzioni assiali. Il quadrante è visibile dal lato **−Z** (67.080/160.000 pixel solidi, colore medio 200,199,201, coerente con l'albedo chiara); il lato +Z mostra il retro del case scuro (colore medio 23,25,31). Nessuna faccia mancante.
- **Nota:** il modello è un disco sottile (bounds 2,00 × 2,00 × 0,13) con il quadrante rivolto verso −Z. Da tenere presente quando verrà posizionato nella scena.
- **Verifiche generali:** nessun file fuori dal gruppo modificato; progetto salvato con `AssetDatabase.SaveAssets()`.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Prossimo passo proposto (eseguito):** rimanenti modelli uno a uno, su indicazione esplicita dell'utente; vedi Task 8a.

### Task 8 — TablePrinter

- **Data:** 2026-11-22
- **Nota di metodo:** su indicazione dell'utente, da questo modello in poi si lavora con **un task per modello** (non più micro-task separati), chiedendo conferma tra un modello e l'altro.
- **Obiettivo:** sistemare materiali e prefab del tavolino stampante senza toccare l'FBX.
- **File creati:**
  - `Art/Materials/Man_Down/TablePrinter/MAT_TablePrinter_Black.mat`
  - `Art/Materials/Man_Down/TablePrinter/MAT_TablePrinter_White.mat`
  - `Level/Prefabs/Man_Down/MESH_TablePrinter.prefab`
- **Stato iniziale verificato (sola lettura):**
  - `MESH_TablePrinter.fbx`: 1 renderer (`MESH_TablePrinter`), mesh `printer`, 2 submesh / 2 slot, **295 tris**, 355 vertici, 0 Camera, 0 Light.
  - UV range u = 0,000..0,688, v = 0,000..0,875.
  - Materiali embedded: `black` (URP Lit, `_Color` 0,178) e `white` (URP Lit, `_Color` 0,906), nessuna texture, nessuna keyword.
  - `GetDependencies(fbx, false)` → solo `Lit.shader`: nessuna texture referenziata. Non esistono texture dedicate: quelle in `TEX_Printer/` appartengono al modello `MESH_Printer.obj`.
- **Materiali creati:** URP Lit, colori uniformi ripresi dagli embedded (0,178 e 0,906), `_Metallic = 0`, smoothness 0,25 (black) e 0,30 (white) come superfici verniciate, opachi, `_Cull = 0`, queue 2000, nessuna keyword.
- **Prefab verificato:** 1 renderer, **295 tris**, 0 Camera, 0 Light; slot = `MAT_TablePrinter_Black`, `MAT_TablePrinter_White` nell'ordine originale. FBX invariato.
- **Verifiche di resa:** anteprime da 3 angolazioni (due diagonali opposte + dall'alto), 49.120 / 52.393 / 63.594 pixel solidi su 160.000, colori medi coerenti con i due materiali. Nessuna faccia mancante.
- **Verifiche generali:** nessun file fuori dal gruppo modificato; progetto salvato con `AssetDatabase.SaveAssets()`.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.
- **Ordine di intervento concordato per i modelli rimasti** (dal più semplice, con conferma tra uno e l'altro): 1. TablePrinter ✅, 2. OfficeDoor ✅ (poi sostituzione cercata dall'utente), 3. FireAlarm/sprinkler ✅, 4. Printer ✅, 5. Desk ✅.
- **Prossimo passo (eseguito):** OfficeDoor, registrato nel Task 9.

### Task 9 — OfficeDoor

- **Data:** 2026-11-22
- **Obiettivo:** risolvere l'ambiguità dei due set di texture e creare materiali e prefab senza toccare l'FBX.
- **Risoluzione dell'ambiguità (§4.3 punto 1):** `MESH_OfficeDoor.fbx` ha 1 renderer, mesh `SM Office Door`, 2 submesh / 2 slot, **948 tris**, 1291 vertici, 0 Camera, 0 Light. Submesh 0 = `Doorway` (110 tris), submesh 1 = `Door` (838 tris); entrambi coprono quasi tutto lo spazio UV 0..1.
  - `TEX_OfficeDoor/` contiene 11 texture: un set senza suffisso (`Base_Color`, `Metallic`, `Mixed_AO`, `Normal_DirectX`, `Roughness`, `Opacity`) e uno con suffisso (`Base_Color_1`, `Metallic_2`, `Mixed_AO_5`, `Normal_DirectX_4`, `Roughness_3`).
  - **Mapping deciso con evidenza:** set **senza suffisso → `Doorway`**, set **con suffisso → `Door`**. Campionando le mappe sugli UV dei due submesh: l'unica mappa `Opacity` è scura nel 26% dei vertici di `Doorway` e nello 0,2% di `Door`; inoltre i pixel scuri di `Base_Color` e `Opacity` coincidono (10,4%), mentre `Base_Color_1` e `Opacity` non coincidono affatto (0%). Le due texture sono quindi allineate e appartengono allo stesso set, usato da `Doorway`.
- **File creati:**
  - `Art/Textures/Man_Down/TEX_OfficeDoor/TEX_OfficeDoor_Doorway_MetallicSmoothness.png` (2048², R = metallic media 146,4, A = 1 − roughness media 141,2)
  - `Art/Textures/Man_Down/TEX_OfficeDoor/TEX_OfficeDoor_Door_MetallicSmoothness.png` (2048², R = metallic media 43,6, A = 1 − roughness media 179,5)
  - `Art/Materials/Man_Down/OfficeDoor/MAT_OfficeDoor_Doorway.mat`
  - `Art/Materials/Man_Down/OfficeDoor/MAT_OfficeDoor_Door.mat`
  - `Level/Prefabs/Man_Down/MESH_OfficeDoor.prefab`
- **File modificati (solo `.meta`):** `Normal_DirectX.png.meta` e `Normal_DirectX_4.png.meta`: `textureType` 0 → 1, `sRGBTexture` 1 → 0, `flipGreenChannel` 0 → 1. Il flip del verde è applicato perché i file sono esplicitamente etichettati `DirectX` (convenzione opposta a OpenGL/Unity). Backup dei meta originali in temp.
- **Materiali verificati:** entrambi URP Lit, `_Metallic = 1`, `_Smoothness = 1`, `_SmoothnessTextureChannel = 0`, `_OcclusionStrength = 1`, opachi, queue 2000, `_Cull = 0`; keyword `_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`.
  - `MAT_OfficeDoor_Doorway`: BaseColor, Normal_DirectX, maschera Doorway, Mixed_AO.
  - `MAT_OfficeDoor_Door`: BaseColor_1, Normal_DirectX_4, maschera Door, Mixed_AO_5.
- **Prefab verificato:** 1 renderer, **948 tris**, 0 Camera, 0 Light; slot `MAT_OfficeDoor_Doorway`, `MAT_OfficeDoor_Door`; FBX invariato.
- **Scelta conservativa:** `Opacity.png` **non è stata usata** per abilitare trasparenza o alpha clip. Il suo ruolo di maschera è plausibile ma il modo di impiegarlo (transparenza vs taglio) non è verificabile: nessun materiale del gruppo usa alpha. Resta disponibile per una decisione esplicita futura.
- **Verifiche di resa:** anteprime da 4 angolazioni, 33.761..49.218 pixel solidi su 160.000, colori medi beige/legno coerenti con le BaseColor; nessuna faccia mancante.
- **Verifiche generali:** nessun file fuori dal gruppo modificato; progetto salvato con `AssetDatabase.SaveAssets()`.
- **Non verificato:** Play Mode nella scena di gioco e prova su Meta Quest 3.

### Task 9b — Indagine sullo stiramento UV di OfficeDoor (correzione tentata e annullata)

- **Data:** 2026-11-22
- **Segnalazione:** i materiali `Door` e `Doorway` appaiono stirati.
- **Diagnosi (sola lettura):**
  - FBX: solo UV0 (UV1/UV2/UV3 assenti); scale di tutte le mappe = 1,1, offset = 0,0; `globalScale = 1,00`; texture 2048² non tileable (diff bordi 87,2 / 57,0 e 29,7 / 57,1 contro baseline casuale 44,4 / 18,2).
  - Bounding box dei submesh: `Doorway` 0,120 × 1,020 × 2,067 m; `Door` 0,609 × 0,723 × 2,000 m. UV0 ≈ 0..1 in entrambi.
  - Jacobiano locale UV→posizione: rapporto medio pesato `|dP/dv| / |dP/du|` = **1,098** (`Doorway`) e **1,027** (`Door`) — non risolutivo, perché gli UV sono un atlas con più isoluine sovrapposte.
  - Bbox UV per faccia: `Doorway` facce X± coprono 1,020 × 2,067 m con dU 0,931 / dV 0,943 → 2:1; `Door` facce Z± coprono 0,609 × 0,723 m con dU 0,976 / dV 0,962 → 1,2:1.
- **Metodo di misura validato:** griglia 8×8 in vista ortografica, conteggio delle transizioni chiare/scure. Controllo: quad 1×1 → V/H = 1,000; quad 1×2 → 2,016; quad 2×1 → 0,496.
- **Risultati (V/H, 1 = isotropo):**
  | Variante | vista +Z | vista +X |
  |---|---|---|
  | Originale | 2,685 | 2,273 |
  | V × 0,50 (correzione tentata) | 3,189 | 2,834 |
  | U × 0,50 | 2,615 | 2,685 |
  | U × 0,40 | 2,380 | 2,130 |
  | U × 0,35 | 2,185 | 2,528 |
  | V × 2,00 (richiede tiling) | 2,065 | 1,898 |
- **Conclusione verificata:** la compressione V scelta inizialmente (fattori 0,4935 `Doorway` / 0,3615 `Door`, ancoraggio in alto) **aumenta** lo stiramento invece di correggerlo. Nessuna trasformazione UV globale riporta V/H a 1: il modello ha isole UV con scale diverse per faccia; serve un ri-unwrap per faccia (non eseguibile senza editor UV/Blender) oppure una texture pre-distorta. `V × 2,00` migliora ma richiede tiling su texture non tileable.
- **Azioni eseguite:** creato e poi **rimosso** `Art/Models/Man_Down/MESH_OfficeDoor_UVFixed/MESH_OfficeDoor_UVFixed.asset` (cartella rimossa); prefab riportato alla mesh FBX originale. Verifica finale: 1 renderer, **948 tris**, slot `MAT_OfficeDoor_Doorway` + `MAT_OfficeDoor_Door`, 0 Camera, 0 Light; render identico al prima (83566 / 56370 / 58498 pixel solidi). Progetto salvato.
- **Immagini di supporto (temp):** `officedoor_checker_0/1.png`, `officedoor_grid_*_*.png`, `officedoor_uvfix_*_before/after.png`, `officedoor_restored_0/1/2.png`.
- **Decisione:** provata l'opzione 2 (Task 9c), poi superata dalla generazione procedurale di texture coerenti (Task 9d).

### Task 9c — OfficeDoor: texture pre-distorte (opzione 2, in prova)

- **Data:** 2026-11-22
- **Scelta utente:** provare l'opzione 2 (texture pre-distorta) dopo il fallimento della correzione UV.
- **Metodo:** per ciascuna delle 4 mappe dei due materiali è stata creata una copia 2048² in cui il contenuto viene stirato orizzontalmente di **k = 2,5** (campionamento `u_sorgente = (u − 0,5)/k + 0,5`, crop centrale al 40% della larghezza); bilinear filtering. Per le normal map la componente X è stata divisa per k e il vettore rinormalizzato; `flipGreenChannel = True` mantenuto in importazione (DirectX).
- **File creati** in `Art/Textures/Man_Down/TEX_OfficeDoor/`: `TEX_OfficeDoor_Doorway_BaseColor_PD.png`, `TEX_OfficeDoor_Doorway_Normal_PD.png`, `TEX_OfficeDoor_Doorway_MS_PD.png`, `TEX_OfficeDoor_Doorway_AO_PD.png` e le 4 equivalenti `_Door_`.
- **Materiali aggiornati:** `MAT_OfficeDoor_Doorway` e `MAT_OfficeDoor_Door` puntano alle nuove mappe; impostazioni URP Lit invariate (metallic 1, smoothness 1, `_Cull = 0`, keyword `_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`). Le texture originali e le maschere generate restano intatte e non referenziate dai materiali.
- **Prefab invariato:** mesh FBX originale, 1 renderer, **948 tris**, 0 Camera, 0 Light.
- **Evidenza quantitativa:** con griglia pre-distorta k = 2,5 il rapporto V/H scende da 2,685 / 2,273 a circa **1,91 / 1,96** (non arriva a 1,0: resta una componente anisotropa dovuta alle isole UV multiple). Il crop centrale preserva la finestra scura di `Doorway` (pixel scuri 10,4% → 14,8%, luminanza media 0,728 → 0,642).
- **Render prima/dopo (stessa camera):** vista 0 solido 83566 → 83558, colore medio 91,78,60 → 103,85,61; vista 1 56370 → 56428, 89,74,53 → 101,81,54; vista 2 58498 → 58501, 183,151,96 → 187,154,99.
- **Immagini (temp):** prima `officedoor_restored_0/1/2.png`, dopo `officedoor_predist_0/1/2.png`.
- **Stato:** **superato** dal Task 9d. Le texture `_PD` restano su disco ma non sono più referenziate dai materiali; da rimuovere dopo conferma visiva del risultato procedurale.

### Task 9d — OfficeDoor: texture procedurali coerenti (approccio A, definitivo in attesa di conferma)

- **Data:** 2026-11-22
- **Scelta utente:** approccio A (atlas dipinti rispettando le UV esistenti), **alpha clip**, **legno chiaro** per il pannello e **acciaio** per telaio/ferraglia, vetro trasparente per l'apertura.
- **Classificazione delle regioni UV** (rasterizzazione dei 948 triangoli su griglia 1024²):
  - `Doorway`: 96,9% acciaio (telaio), 3,1% vetro (pixel con `Opacity.png` < 128).
  - `Door`: 88,2% legno (faccia del pannello), 11,8% acciaio (maniglia/ferraglia, triangoli con baricentro `x > -0,15`).
- **Texture generate** in `Art/Textures/Man_Down/TEX_OfficeDoor/` (1024², 8 file): `TEX_OfficeDoor_{Doorway,Door}_Gen_{BaseColor,Normal,MS,AO}.png`.
  - BaseColor: legno chiaro con venatura (`0,80/0,65/0,45` + grana), acciaio spazzolato grigio (`~0,58`) con micro-graffi, vetro azzurro tenue con **alpha 0**.
  - Normal: derivate da height field con gradiente Sobel, convenzione **OpenGL** (`flipGreenChannel = False`, sRGB disattivato).
  - MS: R = metallic (1 acciaio, 0 legno/vetro), A = smoothness (0,50 acciaio, 0,35 legno, 0,92 vetro); G/B = 0.
  - AO: bianco costante (nessuna occlusione artificiale).
  - Pattern calcolati con coordinate compense `u × 2,5`, `v × 2,5` per mitigare l'anisotropia delle isole UV; wrap `Clamp`, filtering bilineare, mipmap attive.
- **Materiali** `MAT_OfficeDoor_Doorway` / `MAT_OfficeDoor_Door`: URP Lit, slot `_BaseMap`/`_BumpMap`/`_MetallicGlossMap`/`_OcclusionMap` puntanti alle nuove texture, scale 1×1 offset 0. **Alpha clip attivo**: `_AlphaClip = 1`, `_Cutoff = 0,5`, keyword `_ALPHATEST_ON`, render queue **2450**. Invariati `_Metallic = 1`, `_Smoothness = 1`, `_SmoothnessTextureChannel = 0`, `_OcclusionStrength = 1`, `_Cull = 0`, shader keywords `_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`. Nessun uso di Roughness.
- **Verifica canali:** Doorway BaseColor alpha<128 = 3,1% (vetro), MS R medio 247 / A medio 131; Door MS R medio 30 (88,2% legno) / A medio 94. Normal map verificate come `NormalMap`, sRGB=False, flip verde=False.
- **Prefab invariato:** `MESH_OfficeDoor.fbx` originale, 1 renderer, **948 tris**, 2 materiali, 0 Camera, 0 Light.
- **Render (stesse 3 viste del confronto precedente):** `officedoor_gen_0/1/2.png`; pixel solidi 83619 / 56486 / 58448, colore medio 105,88,71 / 103,87,75 / 181,159,134 (prima: 83566 / 56370 / 58498 e 91,78,60 / 89,74,53 / 183,151,96).
- **Stato:** **rifiutato dall'utente** ("è terribile"). L'utente cercherà un altro modello per la porta. Vedi Task 9e per il ripristino.

### Task 9e — OfficeDoor: ripristino e chiusura

- **Data:** 2026-11-22
- **Azione:** su richiesta dell'utente, i due materiali sono stati **riportati alle texture originali del Task 9** (`Base_Color`, `Normal_DirectX`, maschere MS dedicate, `Mixed_AO`; equivalenti `_1`/`_4`/`_5` per `Door`). Alpha clip disattivato (`_AlphaClip = 0`, keyword `_ALPHATEST_ON` rimossa), queue 2000, keyword ripristinate a `_NORMALMAP`, `_METALLICSPECGLOSSMAP`, `_OCCLUSIONMAP`.
- **Pulizia:** eliminate tutte le 16 texture generate dai tentativi 9c/9d (8 `_Gen_*` + 8 `_PD`). Restano solo le texture originali e le maschere MS del Task 9.
- **Prefab:** invariato, mesh FBX originale, 1 renderer, 948 tris, 0 Camera/Light.
- **Decisione:** la porta verrà sostituita con un altro modello fornito dall'utente; per ora non si interviene più su `MESH_OfficeDoor`.
- **Prossimo passo (eseguito):** `MESH_fire_alarm_sprinkler.obj`, registrato nel Task 10.

### Task 10 — FireAlarm / Sprinkler

- **Data:** 2026-11-22
- **Modello sorgente:** `Art/Models/Man_Down/MESH_fire_alarm_sprinkler.obj` (**invariato**), 10 renderer / 1.222 tris, 0 Camera, 0 Light.
- **Raggruppamento:** 2 renderer `fire_*` (corpo allarme, 34 tris) + 8 renderer sprinkler (1.188 tris, incluso `polySurface12_...:pSphere3`, inizialmente escluso e poi recuperato). Mesh unite salvate in `Art/Models/Man_Down/MESH_FireAlarm_Optimized/`: `MESH_FireAlarm_Body.asset` (60 vertici, 34 tris) e `MESH_FireAlarm_Sprinkler.asset` (1.320 vertici, 1.188 tris). Tangenti ricalcolate; trasformazioni sorgente identità.
- **Materiali creati** in `Art/Materials/Man_Down/FireAlarm/`:
  - `MAT_FireAlarm_Body.mat`: `fire_BaseColor.tga.png`, `fire_Normal.tga.png`, `TEX_FireAlarm_MetallicSmoothness.png`, `fire_ao.tga.png`.
  - `MAT_FireAlarm_Sprinkler.mat`: `sprinkler_BaseColor.tga.png`, `sprinkler_Normal.tga.png`, `TEX_Sprinkler_MetallicSmoothness.png`, `sprinkler_ao.tga.png`.
  - Entrambi URP Lit, opachi, `_Metallic = 1`, `_Smoothness = 1`, `_SmoothnessTextureChannel = 0`, `_OcclusionStrength = 1`, scale 1×1 offset 0, keyword `_METALLICSPECGLOSSMAP`, `_NORMALMAP`, `_OCCLUSIONMAP`. `_Cull = 2` (Back) per allinearsi ai materiali embedded originali: i mesh sono corpi chiusi e il rendering risulta identico come silhouette al prima. Nessun uso di Roughness.
- **Maschere generate:** `TEX_FireAlarm_MetallicSmoothness.png` (1024², metallo medio 0,037, smooth 0,235) e `TEX_Sprinkler_MetallicSmoothness.png` (1024², metallo medio 0,448, smooth 0,237), da `Metalness` (R) e `1 − Roughness` (A), sRGB disattivato, mipmap attive.
- **Normal map:** `fire_Normal.tga.png` e `sprinkler_Normal.tga.png` riconfigurate come `NormalMap`, `sRGB = False`, `flipGreenChannel = False` (convenzione OpenGL ipotizzata; da verificare visivamente se l'illuminazione risulta invertita in verticale).
- **Prefab creato:** `Level/Prefabs/Man_Down/MESH_FireAlarm_Sprinkler.prefab`: 2 renderer, **1.222 tris**, 0 Camera, 0 Light, 0 script.
- **Render prima/dopo (500², 3 viste):** `firealarm_before_0/1/2.png` vs `firealarm_after_0/1/2.png` in `C:/Users/GravinaKristian/AppData/Local/Temp/opencode/`; pixel solidi 35016→34727, 51252→51066, 19774→19641 (silhouette preservata); colore medio 125,122,128→79,27,32 / 123,121,126→77,46,52 / 240,235,240→154,68,73.
- **Stato:** in attesa di conferma visiva. Progetto salvato.
- **Prossimo passo (eseguito):** `MESH_Printer.obj`, registrato nel Task 11.

### Task 11 — Printer

- **Data:** 2026-11-22
- **Modello sorgente:** `Art/Models/Man_Down/MESH_Printer.obj` (**invariato**), 1 renderer, 1 mesh, **3.760 tris**, 2.669 vertici, 0 Camera, 0 Light. UV u 0..1, v 0..0,82.
- **Mappatura texture (confermata dall'utente):** `TI24_maq.png` = BaseColor, `TI24_maqRR.png` = Roughness, `norm1a.png` = NormalMap. Le due texture grigie hanno la stessa struttura (correlazione 0,707); l'utente ha scelto questa interpretazione dopo aver visto i render candidati `printer_cand_maq_*` e `printer_cand_maqRR_*`.
- **Normal map:** `norm1a.png` riconfigurata come `NormalMap`, `sRGB = False`, `flipGreenChannel = False` (OpenGL).
- **Maschera generata:** `TEX_Printer_MetallicSmoothness.png` (1024²) con R = 0 (nessuna mappa metallica disponibile: metallic uniforme 0) e A = `1 − Roughness` da `TI24_maqRR.png`; smoothness media 0,632; `sRGB = False`, alpha da input, mipmap attive. Nessuna texture AO associata (non disponibile).
- **Materiale creato:** `Art/Materials/Man_Down/Printer/MAT_Printer.mat`, URP Lit opaco, `_BaseMap = TI24_maq`, `_BumpMap = norm1a`, `_MetallicGlossMap = TEX_Printer_MetallicSmoothness`, `_OcclusionMap = null`; `_Metallic = 0`, `_Smoothness = 1`, `_SmoothnessTextureChannel = 0`, `_Cull = 2` (come l'embedded), scale 1×1 offset 0; keyword `_METALLICSPECGLOSSMAP`, `_NORMALMAP`. Nessun uso di Roughness.
- **Prefab creato:** `Level/Prefabs/Man_Down/MESH_Printer.prefab`: 1 renderer, **3.760 tris**, 0 Camera, 0 Light, 0 script, materiale `MAT_Printer`.
- **Render prima/dopo (500², 2 viste):** `printer_before_0/1.png` vs `printer_after_0/1.png` in `C:/Users/GravinaKristian/AppData/Local/Temp/opencode/`; pixel solidi 43762→43660, 44088→44002 (silhouette preservata); colore medio 125,123,128→68,68,72 e 122,120,124→70,69,74 (la texture è molto scura, come da BaseColor scelto).
- **Stato:** in attesa di conferma visiva. Progetto salvato.
- **Prossimo passo (eseguito):** `MESH_Desk.fbx`, registrato nel Task 12.

### Task 12 — Desk (ultimo modello)

- **Data:** 2026-11-22
- **Modello sorgente:** `Art/Models/Man_Down/MESH_Desk.fbx` (**invariato**), 17 renderer, 19 materiali embedded, **10.736 tris**; conteneva 1 Camera e 5 Light area.
- **Decisione utente:** rendere **opachi tutti i 19 materiali** (trasparenze embedded alpha 0,2 e 0 trattate come bug di import, come già per la Chair).
- **Materiali creati** in `Art/Materials/Man_Down/Desk/`: 19 materiali `MAT_Desk_*` (Books, Boxes, BTranaparency, CellPhone, CellScreen, Desk, Cup, Eraser, Wire, Mousepad, Notebook, Paper, PCKeyboard, KeyboardLight, PCMonitor, PCScreen, PCMouse, PCSpeakers, Pen). Tutti URP Lit opachi: `_Surface = 0`, `_AlphaClip = 0`, `_ZWrite = 1`, `_SrcBlend = 1`, `_DstBlend = 0`, queue 2000, `_Cull = 2`, alpha 1, keyword `_ALPHAPREMULTIPLY_ON` e `_SURFACE_TYPE_TRANSPARENT` rimosse. Smoothness ripresa dai materiali originali (0,33–0,96); metallic 0. Il colore uniforme originale è preservato (0,906 per quasi tutti, 0,076 per `MAT_Desk_Wire`).
- **Texture:** `PaperTEX.png` assegnata solo a `MAT_Desk_Paper` (UV del foglio 0..1). `GR_PaleteColors.jpg` **non usata**: è una palette di riferimento, non una texture UV.
- **Prefab creato:** `Level/Prefabs/Man_Down/MESH_Desk.prefab`, ricostruito come gerarchia pulita (non variante del FBX) per evitare di ereditare Camera/Light: **17 renderer, 10.736 tris, 19 slot materiale, 0 Camera, 0 Light, 0 script**. Trasformazioni locali dei renderer (rotazione 270°, scala 100) copiate dal sorgente.
- **Verifica:** 19/19 materiali conformi (opachi, cull 2, queue 2000); `MAT_Desk_Paper.BaseMap = PaperTEX`.
- **Render prima/dopo (500², 2 viste):** `desk_before_0/1.png` vs `desk_after_0/1.png` in `C:/Users/GravinaKristian/AppData/Local/Temp/opencode/`; pixel solidi 40071→40497 e 45392→46287; colore medio 230,85,234→126,122,132 e 234,85,239→127,122,134 (prima il magenta del background trapelava attraverso le trasparenze).
- **Pulizia scena:** rimosse le istanze accidentali di `MESH_FireAlarm_Sprinkler` e `MESH_Printer` lasciate dalle anteprime; scena `SCN_Man_Down` tornata ai 5 root originali e salvata.
- **Stato:** tutti i modelli del gruppo completati, inclusa la nuova porta sostituita nel Task 15. Progetto salvato.
- **Prossimo passo:** verifica visiva/integrazione in scena della nuova porta, oppure verifica integrale finale del gruppo.

### Task 13 — Struttura dell'ufficio (plane + cubi)

- **Data:** 2026-11-22
- **Richiesta:** creare solo pavimento (`Plane`) e muri (`Cubi`), con dimensioni da ufficio medio. Nessun arredo, nessun XR rig/player/UI.
- **Dimensioni scelte:** interno **4,0 × 5,0 m**, altezza **2,8 m**, spessore muri **0,15 m**. Apertura porta **1,0 × 2,1 m** sul lato +Z (verso la camera esistente), realizzata con 2 cubi laterali + 1 architrave: sono comunque solo cubi.
- **Materiali creati** in `Art/Materials/Man_Down/OfficeStructure/`: `MAT_OfficeStructure_Floor` (grigio caldo 0,72/0,70/0,66, smoothness 0,30) e `MAT_OfficeStructure_Walls` (bianco sporco 0,91/0,91/0,89, smoothness 0,15). Entrambi URP Lit opachi, metallic 0, cull 2, queue 2000, nessuna texture.
- **Prefab creato:** `Level/Prefabs/Man_Down/MESH_OfficeStructure.prefab`: 7 renderer (1 `Floor` + 6 muri), 0 Camera, 0 Light, 0 script. Collider primitivi mantenuti (utili per future interazioni), nessun componente aggiunto.
- **Scena:** istanza connessa del prefab inserita in `SCN_Man_Down.unity` a `(0, 0, -4)`, 6 m davanti alla `Main Camera` di `prefab_scena` (che guarda +Z). Il prefab `prefab_scena` non è stato modificato. L'istanza è stata poi riparentata come **figlia di `prefab_scena`** (world position preservata), secondo la nuova regola: la scena ha un solo root e può essere usata interamente come prefab.
- **Nota:** al momento dell'intervento la scena conteneva solo il root `prefab_scena`; i 5 root dei modelli visti in precedenza non erano più presenti e **non sono stati ripristinati**.
- **Stato:** progetto salvato. Dimensioni poi aumentate nel Task 14; in attesa di feedback su colori/apertura porta.

### Task 14 — Ingrandimento ufficio

- **Data:** 2026-11-22
- **Richiesta:** ingrandire il locale per poter contenere diverse scrivanie.
- **Dimensioni finali:** interno **8,0 × 10,0 m**, altezza **3,0 m**, spessore muri **0,15 m**. Apertura porta allargata a **1,2 × 2,1 m** sul lato +Z. Spazio sufficiente per 6 scrivanie da 1,2 × 0,8 m disposte in 2 file da 3, con corridoi di ~1,2 m.
- **Modifica:** aggiornati i 7 trasform locali nel prefab `MESH_OfficeStructure` e salvato con `PrefabUtility.SavePrefabAsset`; la scena ha recepito automaticamente le modifiche tramite l'istanza connessa. Verificato: size `(8,00; 3,00; 10,00)`, 7 renderer, 0 Camera/Light/script.
- **Scena:** l'istanza resta figlia di `prefab_scena`; spostata a world `(0, 0, -6,50)` per mantenere la porta alla stessa distanza dalla camera.
- **Nota importantissima sui modelli:** `MESH_Desk.prefab` misura **2,31 × 3,24 × 6,02 m** (scala import molto più grande di scrivania reale: 6 m di lunghezza). Le dimensioni dell'ufficio sono state scelte da **ufficio medio reale**, non adattate a quel modello: andrà ridimensionato in futuro se si vorrà usarlo qui.
- **Nota scena:** la scena aveva 4 root: `prefab_scena` (con `MESH_OfficeStructure` figlio), più `MESH_Employee`, `MESH_Chair` e `MESH_FireExtinguisher` aggiunti dall'utente. Su conferma dell'utente sono stati **riparentati sotto `prefab_scena`** con world position preservata. Ora la scena ha **1 solo root** (figli: `Main Camera`, `Directional Light`, `MESH_OfficeStructure`, `MESH_Employee`, `MESH_Chair`, `MESH_FireExtinguisher`). L'utente penserà in seguito a posizionarli come figli della stanza.
- **Stato:** progetto salvato.

### Task 15 — Nuova OfficeDoor (modello sostituito dall'utente)

- **Data:** 2026-11-23
- **Modello sorgente:** `Art/Models/Man_Down/MESH_OfficeDoor.fbx` (**sostituito dall'utente**, FBX invariato da me), 2 renderer, **2100 tris**, 0 Camera, 0 Light. Renderer: `Cube.001` (44 tris, 1 submesh, `Matériau.001`) e `Cylindre.001` (2056 tris, 4 submesh, `Matériau.001`/`Matériau`/`Matériau.002`/`Matériau.003`). Nessuna dipendenza texture embedded.
- **Texture presenti** in `Art/Textures/Man_Down/TEX_OfficeDoor/`: solo `Blanc.png` (177×167, bianco piatto, sRGB) e `NoirGris.jpeg` (4960×3508, grigio scuro piatto, sRGB). Le vecchie texture del modello precedente non sono più presenti.
- **Mappatura confermata dall'utente:** pannello bianco = `Blanc.png` su `Matériau` e `Matériau.003`; maniglia/parti metalliche grigie = `NoirGris.jpeg` su `Matériau.001` e `Matériau.002`.
- **Materiali creati** in `Art/Materials/Man_Down/OfficeDoor/`: `MAT_OfficeDoor_Panel.mat` (BaseMap `Blanc`, BaseColor bianco, Metallic 0, Smoothness 0,35) e `MAT_OfficeDoor_Handle.mat` (BaseMap `NoirGris`, BaseColor bianco, Metallic 0, Smoothness 0,45). Entrambi URP Lit opachi: `_Surface = 0`, `_AlphaClip = 0`, `_Cull = 2`, queue 2000, tiling 1×1, offset 0. Nessuna normal/metallic/occlusion map disponibile; nessuno slot inventato.
- **Pulizia:** rimossi i vecchi materiali `MAT_OfficeDoor_Door.mat` e `MAT_OfficeDoor_Doorway.mat` del modello precedente; ricreato `MESH_OfficeDoor.prefab`.
- **Prefab creato:** `Level/Prefabs/Man_Down/MESH_OfficeDoor.prefab`, 2 renderer, **2100 tris**, 0 Camera, 0 Light, 0 script. Assegnazioni: `Cube.001 -> [MAT_OfficeDoor_Handle]`, `Cylindre.001 -> [MAT_OfficeDoor_Handle, MAT_OfficeDoor_Panel, MAT_OfficeDoor_Handle, MAT_OfficeDoor_Panel]`.
- **Verifica:** anteprime prima/dopo salvate in `C:/Users/GravinaKristian/AppData/Local/Temp/opencode/` (`door2_before_0/1/2.png`, `door2_after_0/1/2.png`); silhouette preservata (solid pixel comparabili). Console: nessun nuovo errore dovuto al task.
- **Stato:** progetto salvato. In attesa di conferma visiva dell'utente prima di passare ad altro.

### Task 16 — Ufficio open space 14×10 con arredi

- **Data:** 2026-11-23
- **Richiesta utente:** usare prefab G_Marius e modelli in `Assets/_Project/Shared/Art/Models` (piantaDaUfficio, Banco, Telefono, Computer, Armadio e altro pertinente), lasciando senza materiale se non ce l'hanno; costruire un ufficio più grande e sensato. Scelte confermate: **Open space 14×10 m**, **MESH_Desk G_Marius non incluso** (scala import 6 m).
- **Vincoli rispettati:** nessuna modifica a Shared/asset originali; modelli Shared solo istanziati in scena; tutto sotto `prefab_scena`; nessun XR rig/UI/gameplay.
- **Struttura:** `MESH_OfficeStructure.prefab` aggiornato a **14×10×3 m**, porta 1,2×2,1 m sul lato +Z. Istanza riposizionata a local `(0,0,0)` sotto `prefab_scena`.
- **Arredi piazzati (istanze in scena, figlie di `prefab_scena`):** 4 postazioni (Tavolino+Computer+Telefono+Chair G_Marius), reception (Banco+Chair+Computer), angolo meeting (Tavolino+2 Poltrona+2 Puf), storage (Armadio, Scaffale, Libreria), area stampa (TablePrinter+Printer G_Marius), 3 piante, FireExtinguisher, AED, RilevatoreDiGas, FireAlarm/Sprinkler, Clock, Lamp, OfficeDoor, Employee. Nomi scena: prefisso `ENV_` per istanze da modelli Shared, `MESH_` per prefab G_Marius.
- **Nota:** posizionamento automatico con normalizzazione bounds bottom-center; elementi a parete/soffitto corretti con passaggio manuale. Il layout è una prima bozza sensata e va verificato visivamente in Editor.
- **Nota scena:** la scena ha 2 root (`prefab_scena` e il root `OfficeDoor` preesistente segnalato nel Task 15, non toccato). L'istanza `OfficeDoor` dentro `prefab_scena` è il nuovo prefab Task 15.
- **Stato:** progetto salvato. In attesa di conferma visiva/regolazioni di posizionamento.

### Task 17 — Correzione scale e rotazioni arredi

- **Data:** 2026-11-23
- **Richiesta utente:** molti modelli hanno grandezze sproporzionate o sono ruotati male (libreria, piante, armadio, ecc.).
- **Causa principale:** gli FBX hanno il root con rotazione base **X ≈ -90° (270°)** e scala **100** (`Banco` e `Poltrona` hanno root scala 1 ma figli a 100). Il posizionamento precedente impostava una `localRotation` assoluta, cancellando la rotazione base: i modelli risultavano "sdraiati" (Y e Z scambiati). Inoltre il rilevatore di gas aveva fattore di scala errato (100 × 45 = 4500) e orologio/allarme/storage erano compenetrati nei muri.
- **Metodo corretto:** `localRotation = Quaternion.Euler(0, yaw, 0) * baseRotationAsset`; `localScale = baseScaleAsset * fattore`; posizionamento con bounds bottom-center o center a seconda dell'oggetto.
- **Correzioni applicate:**
  - Scrivaniette Tavolino: scala 1,5 → **1,41 × 0,72 × 1,41 m** (altezza piano 0,72 m).
  - Computer postazioni: yaw 0 (schermo + tastiera verso la sedia); reception yaw 180 (sedia a -Z).
  - Telefono: appoggiato piatto (rot 90/180), scala 0,7 → 0,13 × 0,02 × 0,25 m.
  - Poltrone: scala 0,68 → 1,03 m di altezza, yaw 90/270 verso il tavolino; Puf 0,50 × 0,42 × 0,50 m.
  - Tavolino meeting: scala 1,15 → 1,08 × 0,55 × 1,08 m; Banco reception 1,20 × 0,87 × 1,20 m.
  - Armadio: 1,52 × 2,14 × 0,80 m, z = **-4,44** (fuori dal muro); Libreria 0,62 × 1,58 × 0,36 m (yaw 90, z = -4,66); Scaffale 2,00 × 0,62 × 0,36 m (yaw 90, z = -4,66).
  - Piante: scala 2 → 0,44 × 1,15 × 0,45 m, in piedi.
  - TablePrinter: scala 0,55, yaw 90 → 1,60 × 0,71 × 1,24 m; Printer sopra a y 0,71.
  - RilevatoreDiGas: scala totale 45 (fattore 0,45 su base 100), yaw 90 → 0,06 × 0,26 × 0,16 m, parete sinistra a y 2,62.
  - Orologio: yaw 180, scala 0,2 → 0,40 × 0,40 × 0,03 m, z = **-4,83** (fuori dal muro): ora visibile.
  - AED: x = 6,78, 0,13 × 0,34 × 0,31 m; Estintore x = 6,72, 0,24 × 0,67 × 0,19 m, appeso a y 0,35.
  - Allarme antincendio: corpo ruotato 180° e portato a x = **-6,78** (era dentro il muro e col fronte verso la parete); sprinkler al soffitto `(0; 2,88; 0)`, top a 2,99.
  - Porta: scala 0,52 → 1,07 × 2,10 × 0,33 m, centrata nell'apertura.
  - Lampade: 2 istanze (aggiunta `MESH_Lamp_2` a x = +3,0), y = 2,94.
  - Employee: spostato a `(2,0; 0; 0,5)`.
- **Verifica:** rendering di controllo in `C:/Users/GravinaKristian/AppData/Local/Temp/opencode/` (`final_*.png`, `fix_*.png`, `check_*.png`, `detail_*.png`, `scene_*.png`). Controllo numerico: tutti gli arredi dentro il volume interno `x[-6,85; 6,85]`, `z[-4,85; 4,85]`, `y[0; 3]`; solo struttura e porta oltre i limiti (atteso).
- **Note:** la struttura non ha soffitto (floor + 6 pareti): il cielo resta nero; i modelli Shared senza texture restano bianchi/grigi di default come richiesto. La scena ha **1 solo root** (`prefab_scena`).
- **Stato:** progetto salvato. In attesa di conferma visiva; eventuali scelte di design da concordare: soffitto, scrivanie rotonde vs rettangolari, seconda lampada.

### Task 18 — Soffitto, postazioni OfficeDeskCovered, divano davanti alla TV

- **Data:** 2026-11-23
- **Richiesta utente:** aggiungere soffitto; usare `OfficeDeskCovered` per le scrivanie/postazioni (ogni postazione uguale); posizionare il divano in modo sensato davanti alla TV; mantenere almeno un tavolo tondo con puf. Nessun XR Origin/rig/player/UI; tutto sotto `prefab_scena`.
- **File modificati:**
  - `Level/Prefabs/Man_Down/MESH_OfficeStructure.prefab` — aggiunto child `Ceiling` (Cube 14×0,15×10 m, local pos `(0; 2,925; 0)`, top a 3,0 m locali / 3,25 m world rispetto alla struttura), materiale `MAT_OfficeStructure_Floor`, BoxCollider/MeshRenderer.
  - `Level/Scenes/SCN_Man_Down.unity` — riorganizzati arredi sotto `prefab_scena`.
  - `Docs/Man_Down/AGENTS.md` — questo aggiornamento.
- **Comportamento implementato:**
  - Rimossi i 4 root stray fuori `prefab_scena`: `TV_DA_UFFICIO_ONEDRIVE`, `Phone` (istanza `MESH_Sofa.prefab`), `OfficeDeskCovered` stray, `MESH_Sofa`/Sofa 1 root. La TV Shared è stata ri-parentata sotto `prefab_scena` preservando la world transform.
  - Sostituiti i 4 desk delle postazioni (`ENV_Desk_0..3`, vecchi `MESH_Tavolino` Shared) con istanze prefab `MESH_OfficeDeskCovered.prefab` (`Assets/_Project/Groups/G_Marius/Level/Prefabs/Man_Down/MESH_OfficeDeskCovered.prefab`), mantenendo posizioni XZ/altezza pavimento delle postazioni; `ENV_Computer_0..3`, `ENV_Telefono_0..3` e `MESH_Chair_0..3` mantenuti e rialzati sopra il nuovo piano scrivania (top desk ~1,225 m world).
  - Posizionato `MESH_Sofa_1` (istanza prefab `MESH_Sofa 1.prefab`) sotto `prefab_scena` davanti a `TV_DA_UFFICIO_ONEDRIVE`, allineato sull'asse X della TV e rivolto verso -Z (verso la TV), a distanza sensata (centro sofa ~`(-16,45; 0,52; 20,15)`, TV ~`(-16,45; 1,23; 18,04)`).
  - Mantenuti `ENV_Tavolino_Meeting`, `ENV_Puf_1`, `ENV_Puf_2` nell'area meeting.
- **Verifiche eseguite:**
  - Gerarchia scena: `SceneRoots` contiene solo `prefab_scena`.
  - Verifica numerica in Editor/script: soffitto presente in `MESH_OfficeStructure`; computer/telefoni delle 4 postazioni appoggiati sopra i nuovi desk (minY ~1,230 m, desk top ~1,225 m); meeting con tavolino tondo e 2 puf presente.
  - `editor_save_all` + `AssetDatabase.SaveAssets()` eseguiti.
- **Non verificato:** conferma visiva in Editor dell'utente, Play Mode, prova su Meta Quest 3.
- **Note:** il divano usato è `MESH_Sofa 1.prefab` (candidato già individuato in precedenza; `MESH_Sofa.prefab` ha bounds anomali piccoli). La TV resta un'istanza del modello Shared read-only. Nessun asset fuori da `Assets/_Project/Groups/G_Marius/` è stato modificato intenzionalmente.

### Task 00 — Analisi del gameplay e architettura (TASK 00)

- **Data:** 2026-11-24
- **Obiettivo:** leggere tutte le fonti, ispezionare la scena reale con Unity MCP, verificare risorse e impedimenti, proporre una architettura semplice e aggiornare la documentazione di gruppo.
- **File creati:**
  - `Docs/Man_Down/TASK_PROGRESS.md`
- **File modificati:**
  - `Docs/Man_Down/AGENTS.md` (nuovo §0 con direttive prioritarie e correzioni degli stati reali nei §2, §3.3, §3.4, §3.8, §3.9, §7.1 e §7.4)
- **Nessuna modifica a:** `Level/Scenes/SCN_Man_Down.unity`, prefab, materiali, texture, script, Build Settings, `Shared`, `Home` o configurazioni globali.
- **Verifiche eseguite:**
  - Lettura di AGENTS, specifiche, scoring, GDD specifico e generale, flowchart, guide UI e guida interazioni; estrazione del testo dei PDF non leggibili direttamente.
  - Ispezione MCP della scena attiva: root unico `/prefab_scena`, `isDirty: false`, inventario di componenti e collider.
  - Verifica del NPC `MESH_RiggedEmployee` (12 SkinnedMeshRenderer, nessun Animator/clip, già a terra), dei tre telefoni `ENV_Telefono_1/2/3`, del DAE `MESH_AED` presente in scena e nei prefab.
  - Verifica dell'assenza del kit `SI81.UI` (`ScenarioController`, `ScenarioTrigger`, `LabelAnchor`, `SCN_Core`, catalogo e template missione) e di XRI nel manifest.
- **Risultato:** architettura proposta in `Docs/Man_Down/TASK_PROGRESS.md`: `ManDownScenario` con API stabile più `ManDownNpc`, `ManDownPhone`, `ManDownCpr` e `ManDownAed`. Nessun gameplay implementato; **in attesa di conferma** prima di creare script o toccare la scena.
- **Problemi aperti (al momento del Task 00):** probabilità, criteri RCP/DAE, timeout, soglie di esito, primo soccorso aziendale, sorgente dei trigger ed etichette dei Bad Ending. **Risolti il 2026-11-25** nel Task 00e: vedere §0.2 e `TASK_PROGRESS.md`.

### Task 00b — Regole di lavoro per i task futuri

- **Data:** 2026-11-24
- **Richiesta utente:** seguire i documenti forniti, con priorità alle guide di integrazione UI disponibili in `Docs/`; leggere `AGENTS.md` prima di ogni task principale; aggiornare `AGENTS.md` e `TASK_PROGRESS.md` alla fine di ogni micro task.
- **File modificati:**
  - `Docs/Man_Down/AGENTS.md` (punti §0 nn. 1 e 8, §1.3 passi 1 e 8, nuovo registro Task 00b)
  - `Docs/Man_Down/TASK_PROGRESS.md` (nuova sezione "Regole di lavoro confermate")
- **Nessuna modifica a:** scena, prefab, script, Build Settings o sistemi comuni.
- **Verifiche:** solo rilettura delle sezioni modificate; nessuna prova in Play Mode prevista per una modifica documentale.
- **Stato:** direttive registrate e operative dal prossimo task; nessun gameplay implementato.

### Task 00c — Direttive operative riviste

- **Data:** 2026-11-24
- **Richiesta utente:** riscrivere le direttive di lavoro in forma più precisa; le decisioni di gameplay restano differite a quando serviranno.
- **File modificati:**
  - `Docs/Man_Down/AGENTS.md` (§0 punto 8, nuovo §0.1 con 17 regole, §1.3 aggiornato, nuovo registro Task 00c)
  - `Docs/Man_Down/TASK_PROGRESS.md` (sezione "Regole di lavoro confermate" riscritta, nuovo registro fasi)
- **Nessuna modifica a:** scena, prefab, script, materiali, Build Settings, sistemi comuni.
- **Verifiche eseguite:** rilettura dei due file e conferma del percorso effettivo via ricerca file; nessuna compilazione o prova in Play Mode, trattandosi di modifica solo documentale.
- **Verifiche ancora da effettuare:** batch/compilazione e controllo Inspector al primo task con codice; prove funzionali manuali a cura dell'utente.
- **Stato:** direttive riviste e operative; nessun gameplay implementato, nessun task successivo avviato.

### Task 00d — Riorganizzazione di `TASK_PROGRESS.md`

- **Data:** 2026-11-24
- **Richiesta utente:** organizzare `TASK_PROGRESS.md` nelle sezioni Roadmap dei task, Task corrente, Operazioni completate, Operazioni rimanenti, File creati o modificati, Decisioni tecniche, Test effettuati, Problemi aperti e Prossimo passaggio; la roadmap deve contenere i 13 task indicati.
- **File modificati:**
  - `Docs/Man_Down/TASK_PROGRESS.md` (riscritto con le nove sezioni richieste e la roadmap a 13 task; contenuto verificato precedentemente conservato)
  - `Docs/Man_Down/AGENTS.md` (nuovo registro Task 00d)
- **Nessuna modifica a:** scena, prefab, script, materiali, Build Settings, sistemi comuni.
- **Verifiche eseguite:** rilettura del documento riscritto e correzione di due refusi; nessuna compilazione o prova in Play Mode per una modifica solo documentale.
- **Verifiche ancora da effettuare:** compilazione, riferimenti Inspector, Play Mode e prova su Quest nei task di gameplay.
- **Stato:** documento riorganizzato; roadmap e decisioni aperte registrate; nessun gameplay implementato e nessun task successivo avviato.

### TASK 01 — Registrazione iniziale (storica, superata il 2026-11-25)

> **Nota:** questo verbale descrive la situazione precedente al merge con `origin/main`. Le verifiche e i file effettivi sono registrati nel record "TASK 01 — Integrazione del player rig condiviso" più sotto, che ha priorità.

- **Data:** 2026-11-24
- **Richiesta utente:** creare un rig temporaneo seguendo `Docs/SharedPlayerRig.md`, con movimento, rotazione, controller e interazione; senza nuovi pacchetti, senza modifiche globali, senza XRI, isolato dalla scena finale e senza dipendenze permanenti.
- **Verifiche eseguite:**
  - Lettura di `AGENTS.md`, `TASK_PROGRESS.md` e `Docs/SharedPlayerRig.md`; il documento cita `Docs/SharedInteraction.md`, assente.
  - `Packages/manifest.json` e Unity MCP: Meta XR Core 209.0.0, Input System 1.19.0, XR Management 4.7.0, OpenXR 1.16.1, URP 17.3.0, Test Framework 1.6.0; XRI non installato.
  - Unity MCP e ricerca su disco: assenti `MNG_CoreRig.prefab`, `InputService`, `RigRegistration`, `Interactable`, `OVRCameraRig`; nessuna assembly definition in `Assets/` e un solo script fuori dal gruppo.
  - Scena attiva `SCN_Man_Down.unity`: un solo root, nessuna Camera/rig, `isDirty: false`; Editor `ready`, nessuna compilazione in corso e `compilationFailed: false`.
- **File modificati:** solo `Docs/Man_Down/TASK_PROGRESS.md` e `Docs/Man_Down/AGENTS.md` (§3.9 e questo registro).
- **Nessuna modifica a:** scena, prefab, script, materiali, Build Settings, `Shared`, `Home`, `Packages`, `ProjectSettings`, `.gitignore`.
- **Risultato:** rig **non creato**. I componenti indispensabili descritti dalla documentazione non esistono nel progetto; non è stato inventato un sistema alternativo né usato XRI.
- **Verifiche non eseguite:** Play Mode, Test Runner, compilazione di script del rig, prova su Meta Quest 3. La procedura di test manuale è documentata in `TASK_PROGRESS.md`.
- **Problemi aperti:** mancano prefab/script/assembly del rig, `SharedInteraction.md`, `SCN_Core` e adattatore `IRigUIBridge`; serve decisione o conferimento dei componenti da parte del team/utente.
- **Stato (storico, superato il 2026-11-25):** TASK 01 era stato rinviato su indicazione dell'utente: il rig temporaneo sarebbe stato inserito dall'utente a breve; OpenCode non lo ha creato. Ora il rig condiviso è integrato; vedere il record finale del TASK 01 e §0.3.

### Task 00e — Decisioni gameplay approvate

- **Data:** 2026-11-25
- **Richiesta utente:** registrare le otto decisioni approvate e le decisioni integrative definitive; aggiornare `TASK_PROGRESS.md`, specifiche, scoring e `AGENTS.md`; aggiornare il piano delle classi; non implementare ancora gli script di gameplay.
- **File modificati:**
  - `Docs/Man_Down/TASK_PROGRESS.md` (decisioni approvate, roadmap, piano di implementazione, problemi aperti aggiornati)
  - `Docs/Man_Down/Man_Down_Specifiche.md` (riscritto per coerenza con le decisioni)
  - `Docs/Man_Down/Man_Down_Scoring_System.md` (soglie 80/50, ID, RCP 20/20/30, DAE, esiti e limiti di integrazione)
  - `Docs/Man_Down/AGENTS.md` (header, nuovo §0.2, §3.9, §7.2/§7.3/§7.4, registro Task 00e)
- **Nessuna modifica a:** scena, prefab, materiali, texture, script, Build Settings, `Shared`, `Home`, altri gruppi, `Packages`, `ProjectSettings`, XR, `.gitignore`.
- **Verifiche eseguite:** rilettura integrale dei quattro file, ricerca delle residue indicazioni incompatibili dopo l'aggiornamento. Nessun Test Runner o Play Mode: nessuno script creato.
- **Risultato:** tutte le decisioni di gameplay risultano approvate e documentate; il piano delle cinque classi è aggiornato con separazione tra rilevamento fisico RCP e validazione. Nessun gameplay implementato.
- **Problemi aperti (aggiornati il 2026-11-25):** rig condiviso integrato in Editor/Play Mode ma non validato su Meta Quest 3; parametri didattici RCP/Time non ancora forniti (campi configurabili, nessun valore inventato); applicazione finale delle soglie al punteggio ponderato o allo SPI comune rinviata all'integrazione.
- **Stato:** documentazione aggiornata; attendere il prossimo task. Nessun commit o push.

### TASK 01 — Integrazione del player rig condiviso (migrazione, 2026-11-25)

- **Data:** 2026-11-25
- **Richiesta utente:** seguire `Docs/!LLM_Migrazione_Rig_UI.md` §B.2, integrare il rig di `SCN_Core` in Man Down senza rig locale, senza gameplay e senza UI specifica; autorizzazione esplicita per merge/branch, Build Settings e rename della scena.
- **File creati o modificati:**
  - `Level/Scenes/SCN_ManDown.unity` (+ `.meta`): rinominata da `SCN_Man_Down.unity` con GUID preservato; aggiunti `MNG_SpawnPoint` e `MNG_Scenario` come figli di `prefab_scena`.
  - `Level/Scenes/SCN_Man_Down.unity` (+ `.meta`): vecchio percorso risultato `D` dopo il rename.
  - `ProjectSettings/EditorBuildSettings.asset`: `SCN_Core` indice 0, `SCN_MockScenario` indice 1, `SCN_ManDown` indice 2.
  - `Docs/Man_Down/TASK_PROGRESS.md` e `Docs/Man_Down/AGENTS.md`: aggiornati con stato, verifiche, limiti e procedura manuale.
  - `Docs/Man_Down/Man_Down_Specifiche.md` e `Docs/Man_Down/Man_Down_Scoring_System.md`: allineate sul rig condiviso, eliminando il riferimento al rig temporaneo dell'utente.
- **Non modificati:** prefab, materiali, texture, script di gameplay, `SO_Mission_ManDown.asset`, altri asset `Shared`/`Home`, altri gruppi, `Packages`, configurazioni XR e `.gitignore`. `MAT_CabinetBody.mat`, `MAT_CoffeeTableBody.mat` e `ProjectSettings/ShaderGraphSettings.asset`, riscritti automaticamente da Unity, sono stati ripristinati.
- **Comportamento verificato:** un solo root `prefab_scena`; un solo `RigRegistration`; una camera abilitata (`CenterEyeAnchor`); un solo `EventSystem`, `RigUIBridge` e `InputService`; due `RayInteractor` condivisi; sessione attiva con `SO_Mission_ManDown`; spawn/tracking origin a `(-17.240, 0.253, 26.600)`, yaw `138`; rig stabile dopo circa 65 s; console runtime con `0 errori`.
- **Verifiche eseguite:** compilazione completata; scena e Build Settings riletti su disco con `editorLevel: 1` e `loadCoreInEditor: 0`; validatore scenario `0 errori, 9 avvisi` di naming preesistente; Play Mode dalla scena salvata; test PlayMode `unity command test run --mode playmode --async-tests true --filter SI81.Rig.Tests --timeout 600` → **23/23 superati, 0 falliti**.
- **Non verificato:** prova fisica su Meta Quest 3 (movimento/rotazione reali, pose controller, haptics, presa/selezione e inquadratura). L'Editor riporta `No XR device is available ... Running without VR`; la procedura manuale è in `TASK_PROGRESS.md`.
- **Stato:** TASK 01 completato in Editor/Play Mode. Nessun gameplay o UI specifica implementata in quel task; nessun commit o push. La frase storica "Attendere approvazione esplicita prima del Task 02" è **superata**: TASK 02 è stato approvato e completato, come registrato sotto.

### TASK 02 — Controller logico `ManDownScenario` (2026-11-25)

- **Richiesta utente:** completare esclusivamente il controller logico compatibile con `ScenarioController`: inizializzazione all'inizio effettivo, condizioni indipendenti configurabili/forzabili, azzeramento stato precedente, init idempotente, fasi non rigide, chiamata al 112 indipendente, DAE prima delle 20 compressioni, registrazione di timestamp e azioni tentate/completate, ricezione di risultati RCP grezzi senza rilettura/validazione delle compressioni, timeout 180 s con esito `TIMEOUT`, punteggio provvisorio con Time a 0, esiti 80/50, ID approvati e conclusione una sola volta. Nessun `ManDownNpc`, `ManDownPhone`, `ManDownCpr`, `ManDownAed`, UI, input XR, interazione o scoring definitivo.
- **File creati:**
  - `Code/Scripts/ManDown/ManDownScenario.cs` (+ `.meta`)
  - `Code/Scripts/ManDown/ManDownScenarioTypes.cs` (+ `.meta`)
- **File modificati:**
  - `Level/Scenes/SCN_ManDown.unity`: su `MNG_Scenario` il componente `SI81.UI.ScenarioController` è stato sostituito da `SI81.ManDown.ManDownScenario` (la base ha `[DisallowMultipleComponent]`); missione `SO_Mission_ManDown` e spawn point preservati.
  - `Docs/Man_Down/TASK_PROGRESS.md` e `Docs/Man_Down/AGENTS.md`: verbale, API reale, verifiche eseguite/non eseguite e limiti.
- **Comportamento implementato:** `ManDownScenario` eredita da `ScenarioController`; `OnScenarioStarted()` richiama un'`InitializeScenario()` idempotente che azzera condizioni, azioni, risultati, cause, penalità e flag e riavvia il timer condiviso a 180 s. Probabilità 70/30 respirazione e 75/25 DAE configurabili e forzabili in modo indipendente; fasi non rigide; duplicati delle azioni uniche ignorati; `ReportCprResult(ManDownCprResult)` registra solo dati grezzi e soglie 20/20/30; penalità e fallimenti multipli registrabili; senza 112 nessun arrivo simulato, nessuna dichiarazione di fuori pericolo ed esito massimo parziale; una sola conclusione per istanza. `ResetScenario()` non azzera più gli eventi: `ClearEvents()` resta solo in `OnDestroy()` per preservare le sottoscrizioni future.
- **API eventi e proprietà pubbliche:** metodi `InitializeScenario()`, `ResetScenario()`, `VerifyConsciousness()`, `VerifyBreathing()`, `Call112()`, `StartCpr()`, `ReportCprResult(ManDownCprResult)`, `ReportCprPoorQuality()`, `ActivateAed()`, `PlaceElectrode(int)`, `PlaceLeftElectrode()`, `PlaceRightElectrode()`, `ConfirmAedInstruction()`, `ReportAedIncorrectUse()`, `ResumeCpr()`, `PlaceSidePosition()`, `Monitor()`, `BeginFinalAssistance()`, `CompleteFinalAssistance()`, `FinishMission()`, `FinishMission(int)`, `GetRequiredCprCompressions(ManDownCprPhase)`; eventi e proprietà di stato elencati in `TASK_PROGRESS.md`, disponibili per i componenti futuri senza dipendere da camera, controller, mani o input.
- **Verifiche eseguite:** ricompilazione Unity `completed`, `failed=false`, `compilationFailed=false`, console 0 errori; harness temporaneo isolato **49/49 verifiche superate, `TOTALE FAIL=0`** (A1–A28, B1–B5, C1–C3, D1–D6, E1–E3, F1–F4); Play Mode con `scenarioCount=1`, `hudRootCount=1`, `initialized/running/accepts/sessionActive=True`, `duration=180`, `hudTimerRunning/countdown=True`, `timerSubscribers=1`; scadenza forzata con `timedOut=True`, `outcome=Negative`, `failureCauses=[TIMEOUT,CPR_OMITTED,LOW_SCORE]`, `penalties=[EMERGENCY_NOT_CALLED]`, timer fermo e conclusione unica; console runtime `cursor:262`, 0 errori; Play Mode fermato e `editor save_all` con scena `isDirty:false`.
- **Non verificato / non eseguito:** prova su Meta Quest 3 (movimento e rotazione reali, pose controller, haptics, presa/selezione); rilevamento fisico RCP/DAE; UI specifica; scoring definitivo e SPI; conferma visiva dell'inquadratura sul visore.
- **Problemi aperti:** dati didattici RCP/Time non ancora forniti (campi configurabili, non inventati); applicazione finale delle soglie al punteggio ponderato o allo SPI comune; `git status` mostra modifiche auto-generate da Unity su `MAT_CabinetBody.mat` e `MAT_CoffeeTableBody.mat`, estranee a TASK 02 e da verificare prima di un commit.
- **Stato (al momento di TASK 02):** TASK 02 completato e verificato in Editor/Play Mode. Nessun commit o push. **Attendere approvazione esplicita prima del task successivo.**
- **Nota TASK 03:** le frasi di questo record su `HUDService.StartTimer(180)` e sul timer attivo a 180 s descrivono lo stato storico di TASK 02 e sono superate: in TASK 03 `ManDownScenario` non avvia né riavvia il timer. Vedi il record TASK 03 e §7.4 punto 2.

### TASK 03 — Sola verifica della coscienza dell'NPC (2026-11-25)

- **Richiesta utente:** completare esclusivamente la verifica della coscienza del NPC a terra con interazione e UI comuni SI81; verificare la compatibilità di TASK 02 con la UI comune senza estendere ad altre meccaniche. Niente respirazione, telefono/112, RCP, DAE, rischi ambientali, primo soccorso aziendale, scoring definitivo o avanzamento HUD. Nessun commit/push/merge.
- **File creati:**
  - `Code/Scripts/ManDown/ManDownNpc.cs` (+ `.meta`): componente `[DisallowMultipleComponent]` su `MESH_RiggedEmployee`, con riferimenti serializzati a `ManDownScenario` e a `Interactable`; sottoscrive `GrabEntered` in `OnEnable()` e la rimuove in `OnDisable()`; `AssessConsciousness()` filtra duplicati, riferimenti nulli e missione non attiva, poi chiama `VerifyConsciousness()`.
  - `C:/Users/GravinaKristian/AppData/Local/Temp/opencode/si81_task03_playmode_test.cs` (harness temporaneo, non in repository): 35 verifiche in Play Mode.
- **File modificati:**
  - `Code/Scripts/ManDown/ManDownScenario.cs`: aggiunti `ACTION_CONSCIOUSNESS_CHECK = "CONSCIOUSNESS_CHECK"` e `CONSCIOUSNESS_RESULT = "NPC incosciente"`; `VerifyConsciousness()` registra una sola azione con timestamp e risultato e passa alla fase interna `BreathingAssessment`. Rimosse le chiamate `HUDService.StartTimer(...)` da `InitializeScenario()` e `HUDService.StopTimer()` da `ResetScenario()`.
  - `Level/Scenes/SCN_ManDown.unity`: aggiunto `MESH_ShoulderGrip` come figlio di `MESH_RiggedEmployee`, con `BoxCollider` non trigger, `Interactable` (`canSelect=false`, `canGrab=true`, `followsHand=false`) e `LabelAnchor` (`kind=Interaction`, titolo `Persona a terra`, dettaglio `Verifica la coscienza`, chiavi vuote → predefiniti comuni `GRIP` / `Afferra`, offset `(0; 0,250; 0)`); aggiunto `ManDownNpc` con riferimenti collegati. Modello, materiali e posa dell'NPC invariati.
  - `Docs/Man_Down/AGENTS.md`, `Docs/Man_Down/TASK_PROGRESS.md`, `Docs/Man_Down/Man_Down_Specifiche.md`: verbale, stato e specifica del gesto aggiornati.
- **Comportamento implementato:** il giocatore avvicina il controller a una spalla e preme GRIP; non c'è spostamento fisico, scuotimento o presa che segua la mano. `VerifyConsciousness()` registra `CONSCIOUSNESS_CHECK` con `Completed=true`, `Success=true` e `Note="NPC incosciente"`; il risultato è costante in ogni partita. Il controller è l'unica fonte autorevole di `IsConsciousnessChecked`: `ManDownNpc` non ha campo `assessed` locale e `ResetScenario()` ripristina verifica, azioni e disponibilità della presa. Non vengono chiamati `CompleteObjective()` né avanzano fase HUD/obiettivo: `PhaseKeyFor(BreathingAssessment)` resta `PHASE_SCAN`. Non vengono creati input, raycast, Canvas, TMP, HUD o timer propri; non sono modificati `SO_Mission_ManDown.asset` né asset condivisi.
- **API e compatibilità:** usati solo `SI81.Interaction.Interactable`, `SI81.UI.LabelAnchor`, `SI81.Input.Hand`, `RayInteractor`/`InputService` comuni e le API già esposte da `ManDownScenario`. L'alias `ManDownNpc.CONSCIOUSNESS_RESULT` punta alla costante del controller. Nessuna penalità numerica, `RecordChoice()` o calcolo definitivo; i soli dati Awareness sono predisposti come registrazione dell'azione.
- **Timer:** TASK 02 è stato compatibilizzato con il timer comune rimuovendo i due avvii/fermi duplicati. `HUDRoot.BeginFromSession()` usa `mission.GetTimeLimit(...)`; `SO_Mission_ManDown` resta invariato (Operational 600 s, override 0) e i 180 s approvati restano una decisione di gameplay da applicare solo con una modifica esplicita all'asset missione. In diagnostica direct Play Mode `GetTimeLimit=0`, `timerRunning=False`, `countdown=False`, `remaining=0`, `elapsed=0`: il test eseguito ha verificato solo l'assenza di riavvio del timer, non l'avvio tramite Home. Codice condiviso non modificato.
- **Verifiche eseguite:** ricompilazione Unity `status=completed`, `failed=false`, `compilationFailed=false`, 0 errori e 4 warning `CS0414` preesistenti; validatore scenario `0 errori, 9 avvisi` di naming preesistente; console dopo validazione `entries=[]`, `cursor=314`, `groundTruth.consoleErrors=0`. Play Mode da `SCN_ManDown` con test `si81_task03_playmode_test.cs`: **35/35 PASS**. Confermati: un solo `ManDownScenario`, `ManDownNpc`, `Interactable`, `LabelAnchor` e `HUDRoot`; zero Canvas; etichetta e flag `Interactable` corretti; Scenario inizializzato e attivo; azione unica `CONSCIOUSNESS_CHECK @ 35,63s` con nota `NPC incosciente`; duplicati ignorati; reset ripristina `IsConsciousnessChecked=false`, svuota azioni e consente una nuova verifica; transform e bounds dell'NPC invariati; `MissionFlow.CurrentPhaseKey=hud.phase.scan`, obiettivo e contatore invariati. Console dopo Play Mode `entries=[]`, `cursor=340`, `groundTruth.consoleErrors=0`; Play Mode fermato, `editor save_all` e `AssetDatabase.SaveAssets()` eseguiti.
- **Non verificato:** prova fisica su Meta Quest 3: avvicinamento reale del controller alla spalla, GRIP, pose/haptics, inquadratura dell'etichetta e presa/selezione con il rig condiviso. L'Editor riporta `No XR device is available ... Running without VR`. Non sono stati avviati respirazione, telefono, RCP, DAE, UI dedicata o scoring definitivo.
- **Problemi aperti:** timer comune senza countdown nella diagnostica direct Play Mode (limite osservato, non corretto in questo task); parametri didattici RCP/Time ancora da validare; possibile deallocazione automatica dei materiali condivisi da parte di Unity, ripristinata prima della chiusura.
- **Stato:** TASK 03 completato e verificato in Editor/Play Mode; nessun avanzamento dell'obiettivo 2, nessun commit o push. La prossima meccanica (respirazione, TASK 05) richiede approvazione esplicita. **Nota TASK 05.1:** la respirazione è stata approvata e la sua logica implementata; vedere il record seguente.

### TASK 05.1 — Logica della valutazione respiratoria (2026-11-25)

- **Richiesta utente:** verificare la randomizzazione già implementata senza duplicarla; mantenere probabilità e forzature; estendere `ManDownNpc` con un metodo pubblico per la futura interazione VR; registrare la valutazione e il timestamp della prima valida; rendere l'esito disponibile a interazione e UI; impedire duplicati ma consentire la ripetizione; integrare il reset; predisporre la distinzione tra ramo normale e ramo assente/anomale. Niente interazione VR, animazioni respiratorie, feedback UI, RCP o assistenza; nessun asset condiviso; nessun commit/push.
- **File modificati:** `Code/Scripts/ManDown/ManDownScenario.cs`, `Code/Scripts/ManDown/ManDownScenarioTypes.cs`, `Code/Scripts/ManDown/ManDownNpc.cs` e i documenti `Docs/Man_Down/TASK_PROGRESS.md` e `Docs/Man_Down/AGENTS.md`.
- **Comportamento implementato:** `ManDownScenario.VerifyBreathing()` registra `BREATHING_CHECK` solo alla prima valutazione valida, conservandone il timestamp in `BreathingAssessedAt` (`-1` prima della verifica) ed esponendo `BreathingResult` e `BreathingBranch` (`NotAssessed` / `Normal` / `AbsentOrAbnormal`); le ripetizioni non duplicano il registro né spostano il timestamp, ma riemettono `BreathingAssessed`. `ClearRuntimeData()` azzera il timestamp, quindi `ResetScenario()` comprende anche la valutazione. `ManDownNpc` non conserva stato locale: `AssessBreathing()` delega al controller ed espone le proprietà corrispondenti. La valutazione resta facoltativa e non blocca le altre azioni.
- **Verifiche eseguite:** ricompilazione `compilationFailed=false`, 0 errori e 4 warning `CS0414` preesistenti; harness Edit Mode temporaneo fuori dal repository **22/22 PASS, `TOTALE FAIL=0`**; console `consoleErrors=0`; `git status` mostra solo i tre script del gruppo modificati da OpenCode, nessun asset di scena/condiviso toccato.
- **Non verificato:** Play Mode, Test Runner e prova su Meta Quest 3 (test runtime delegati all'utente); interazione VR e indicio visivo del respiro non implementati.
- **Problemi aperti:** invariati rispetto a TASK 04, con l'aggiunta che `ManDownNpc` richiede ancora il collegamento VR a `AssessBreathing()` e l'indicio visivo transform-based. Task 04 resta aperto in attesa dei test dell'utente.
- **Stato:** TASK 05.1 completato in compilazione/Edit Mode; il microtask 05.2 ha poi aggiunto l'interazione e i popup. Nessun commit o push.

### TASK 05.2 — Interazione respiratoria e feedback NPC (2026-11-25)

- **Richiesta utente:** estendere l'interazione sulla spalla mantenendo l'etichetta GRIP/Afferra iniziale e nascondendola dopo la prima verifica valida, con popup "La persona è incosciente." senza suggerire la respirazione; creare una piccola BoxCollider trigger davanti alla bocca, rilevare solo la presenza fisica di un controller senza pulsanti né raycast, richiedere 2,5 s di permanenza continua configurabile e azzerare il conteggio all'uscita; mostrare i popup sullo stato respiratorio; aggiornare l'obiettivo NPC; gestire il reset; niente UI personalizzata, asset condivisi, RCP o assistenza.
- **Decisione utente sui popup:** usare `AskDecision` con una sola opzione "OK" senza modificare script, prefab o asset condivisi; il pannello reale è stato verificato, ma l'uso è tecnicamente supportato e non previsto dal contratto documentato (2–4 opzioni), in attesa di conferma del team UI. Fallback: `Alert` condiviso se il pannello manca.
- **Decisione tecnica sul rilevamento:** i controller del rig condiviso non hanno Collider e `TryGetAimPose` richiede dispositivi tracciati; si usano le ancore `RigRegistration.LeftHand` / `RightHand` confrontate con i bounds del BoxCollider trigger: solo i due controller, nessun raycast, nessun pulsante, nessun altro oggetto.
- **File creati:** `Code/Scripts/ManDown/ManDownBreathingZone.cs` (+ `.meta`) con `requiredDwellSeconds` configurabile (default 2,5).
- **File modificati:** `Code/Scripts/ManDown/ManDownNpc.cs`; `Level/Scenes/SCN_ManDown.unity` (nuovo `MESH_MouthZone` figlio dell'osso `Head`, BoxCollider isTrigger, componente collegato, riferimento `shoulderLabel`); `Docs/Man_Down/TASK_PROGRESS.md`, `Docs/Man_Down/AGENTS.md`, `Docs/Man_Down/Man_Down_Specifiche.md`.
- **Comportamento implementato:** permanenza di una sola mano azzerata all'uscita o al cambio mano; completamento che richiama `AssessBreathing()`; popup informativi mostrati una sola volta per missione anche se la valutazione è ripetuta; etichetta della spalla soppressa dopo la verifica e ripristinata a nuova missione; obiettivo 1 completato quando coscienza e respirazione sono registrate, in ordine qualsiasi; nessun avanzamento obbligatorio o blocco delle altre azioni.
- **Verifiche eseguite:** compilazione `compilationFailed=false` e 0 errori (due timeout del solo comando `unity recompile`, con Editor `ready` e tipo compilato verificato via reflection); pannello condiviso con un'opzione: 1 pulsante attivo su 4, testo corretto, timer nascosto, layout 480×139, chiusura via OK e `Close()`; harness Edit Mode **13/13 + 4/4 PASS**; riferimenti e collider riletti in scena, `isDirty=false`, console 0 errori.
- **Non verificato:** Play Mode, Test Runner e prova su Quest 3 (gesto fisico, popup sul visore, conferma con il raggio, comportamento in Operational): test runtime delegati all'utente.
- **Stato:** TASK 05.2 completato in compilazione/Edit Mode; in attesa del microtask 05.3 e della conferma UI sui popup a un pulsante. Nessun commit o push.

### TASK 06.1 — Pose Controller, Pose Editor e logica decisionale (2026-11-26)

- **Richiesta utente:** cinque pose statiche dell'NPC con le trasformazioni locali delle ossa esistenti, applicazione istantanea, ripristino della posa iniziale, persistenza, un solo NPC, nessuna dipendenza da clip/Animator/ragdoll e nessuna modifica alle scale; Custom Inspector con Save/Apply e stato per ogni posa, individuazione delle ossa da `Hips`, ripristino di `Supine`, Undo, protezione da salvataggi incompleti e conferma prima di sovrascrivere; gestione delle pose mancanti senza eccezioni/spam; logica delle quattro scelte con registrazione completa e dati per il futuro scoring, integrata in `ManDownScenario`. Niente popup, secondo grab, trigger, rivalutazione, timer, soccorsi, `EndMission`, RCP o DAE.
- **File creati:** `Code/Scripts/ManDown/ManDownPoseTypes.cs`, `Code/Scripts/ManDown/ManDownPoseController.cs`, `Code/Scripts/ManDown/Editor/ManDownPoseControllerEditor.cs` (con `.meta`).
- **File modificati:** `Code/Scripts/ManDown/ManDownScenarioTypes.cs`, `Code/Scripts/ManDown/ManDownScenario.cs`, `Code/Scripts/ManDown/ManDownNpc.cs`, `Level/Scenes/SCN_ManDown.unity`, `Docs/Man_Down/Man_Down_Specifiche.md`, `Docs/Man_Down/Man_Down_Scoring_System.md`, `Docs/Man_Down/TASK_PROGRESS.md` e questo file.
- **Comportamento implementato:** il controller cattura e applica 73 ossa con la sola posizione/rotazione locali; le pose non configurate non toccano le ossa e generano un solo avviso per partita; l'Editor salva con conferma e Undo intitolato; lo scenario registra scelta, posizione precedente, timestamp, esito, errori, correzioni, prima corretta e fino a 8 voci Debrief, senza calcolare punteggi né chiamare `RecordChoice`; l'NPC fa da ponte tra scelta logica e posa visiva.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **10/10 + 16/16 PASS**; checksum delle ossa invariato; scena salvata con `Supine` e posa iniziale configurate e quattro pose vuote; custom editor riconosciuto; console 0 errori. **Non verificato:** Play Mode, Quest 3, Ctrl+Z interattivo e configurazione manuale delle pose alternative.
- **Problemi aperti:** i due root `[SI81 InputService]` in memoria ma non su disco sono stati rimossi prima del salvataggio; le modifiche auto-generate fuori dal gruppo elencate in `TASK_PROGRESS.md` restano intatte. Nessun commit o push. In attesa del microtask 06.2.

### TASK 06.2 — Interazione VR e posizionamento dell'NPC (2026-11-26)

- **Richiesta utente:** collegare pose e logica decisionale alle interazioni VR: GRIP sulle spalle contestuale (coscienza prima, popup di posizionamento dopo la respirazione), domanda e quattro opzioni con ordine casuale, correzioni con nuovo GRIP, nessun feedback di correttezza, trigger box che seguono `Head`/`outfit_top`, rivalutazione respiratoria con storico e validità dopo il posizionamento, integrazione in `ManDownScenario`, timer/112/obiettivi invariati e reset a Supine. Niente timer di 15 s, soccorsi, conclusione, scoring, RCP o DAE. Le pose configurate manualmente non vanno modificate.
- **File modificati:** `Code/Scripts/ManDown/ManDownNpc.cs`, `ManDownBreathingZone.cs`, `ManDownScenario.cs`, `ManDownScenarioTypes.cs`, `Level/Scenes/SCN_ManDown.unity` (solo salvataggio delle pose dell'utente) e i documenti del gruppo. Nessun file creato.
- **Comportamento implementato:** popup condiviso a quattro opzioni mischiate, mappatura e applicazione istantanea della posa, riarmo del GRIP su `GrabExited`, prevenzione sovrapposizioni e accodamento dei popup informativi, fase `PHASE_DECISION` solo durante il popup, storico delle valutazioni con flag iniziale/rivalutazione e invalidazione dopo cambio posa, sospensione del conteggio della zona finché i controller non escono, reset che ripristina Supine e le interazioni.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **31/31 PASS** con pannello reale; riferimenti e trigger box riletti in scena; pose dell'utente con somme di controllo invariate; scena salvata con `isDirty=false`; console 0 errori. **Non verificato:** Play Mode, Quest 3 e rifinitura visiva delle pose/box.
- **Problemi aperti:** solo la prova VR dell'utente e l'eventuale rifinitura estetica delle pose. Nessun commit o push. In attesa del microtask 06.3.

### TASK 06.3 — Monitoraggio finale e conclusione del ramo normale (2026-11-26)

- **Richiesta utente:** monitoraggio di 15 s configurabile avviato da respirazione normale + posizione laterale corretta, in `PHASE_RESOLUTION` senza secondo timer; 112, rivalutazione, cambio posa e movimento disponibili; nessun azzeramento per 112/rivalutazione, interruzione su posizione errata e riavvio completo al ripristino; conclusione con arrivo simulato solo con 112, altrimenti senza soccorsi ed esito massimo Partial; timeout prioritario, conclusione unica, popup puliti, reset completo, API riutilizzabile per RCP/DAE. Niente scoring definitivo, RCP o DAE; pose e BUGFIX 01–05 da preservare.
- **File modificati:** `Code/Scripts/ManDown/ManDownScenario.cs`, `ManDownScenarioTypes.cs`, `ManDownNpc.cs`, `Level/Scenes/SCN_ManDown.unity` (solo `monitoringDurationSeconds: 15`) e documenti `Man_Down_Specifiche.md`, `TASK_PROGRESS.md`, questo file.
- **Comportamento implementato:** API/eventi di monitoraggio, avvio/interruzione/riavvio, scadenza con caso A/B, conservazione dei dati nel risultato, riga di debrief per l'omissione della rivalutazione, pulizia popup su conclusione e reset.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **21/21 PASS**; regressioni 28/28, 10/10, 17/17, 17/17; scena salvata con `isDirty=false` e pose invariate. **Non verificato:** Play Mode e prova su Quest 3 dei 15 s e del popup di arrivo.
- **Problemi aperti / prossima azione:** prove manuali dell'utente; fix pending di `SceneMissionLauncher` (richiede autorizzazione). Nessun commit o push. In attesa del Task 07.

### BUGFIX 01 — Stato globale del 112 e targhette dei telefoni (2026-11-26)

- **Richiesta utente:** dopo il "Sì" su un qualsiasi telefono nessun telefono deve riaprire il popup; tutti devono mostrare GRIP/Afferra, nasconderla durante il grab, ripristinarla al rilascio e tenerla nascosta su tutti dopo la chiamata fino al riavvio. Usare `ManDownScenario` e `LabelAnchor` condivisi, senza nuove UI.
- **File modificati:** `Code/Scripts/ManDown/ManDownPhone.cs`, `ManDownPhoneUi.cs`, `Level/Scenes/SCN_ManDown.unity` (riferimento `scenario`, offset delle tre ancore, listener `grabEntered`/`grabExited`), `Docs/Man_Down/TASK_PROGRESS.md`, `Man_Down_Specifiche.md` e questo file. Nessun asset condiviso, `ProjectSettings`, NPC, prefab o collider duplicati.
- **Comportamento implementato:** `IsCall112Made` è l'unica fonte autorevole letta da tutti i telefoni; il "No" lascia riprovare, il "Sì" registra una sola volta e sopprime le etichette su tutti; reset a nuova missione. L'offset locale `(0; 0; 0,00357143)` compensa la scala 70 e la rotazione dei modelli, portando il punto etichetta 0,25 m sopra il telefono.
- **Verifiche eseguite:** compilazione `compilationFailed=false`; harness Edit Mode temporaneo fuori dal repository **28/28 PASS**; console 0 errori dopo pulizia; scena `isDirty=false`.
- **Non verificato:** Play Mode e prova su Quest 3 (comparsa reale delle targhette per prossimità, GRIP, popup, presa/rilascio, reset); le sottoscrizioni agli eventi non sono attive in Edit Mode e sono state simulate. Nessun commit o push. In attesa di BUGFIX 02.

### BUGFIX 02 — Duplicazione e collisioni dei telefoni (2026-11-26)

- **Duplicazione (KNOWN ISSUE 02):** causa individuata nello scarico asincrono non atteso di `SceneMissionLauncher` (Home, file condiviso): `UnloadSceneAsync()` viene avviato e `loadedMissionScene` azzerato subito, quindi un rilancio rapido carica una seconda copia della scena in additivo. Nessuna istanziazione, reparenting o duplicazione nel codice di G_Marius. Su indicazione esplicita dell'utente (**"Solo ISSUE 03 ora"**) il file condiviso non è stato toccato: resta da autorizzare la correzione della race.
- **Collisioni (KNOWN ISSUE 03):** `ManDownPhone` ignora selettivamente, con `Physics.IgnoreCollision`, le collisioni tra i propri collider e quelli del rig condiviso (`RigRegistration`, `CharacterController`), anche se il rig si registra dopo il telefono. Nessuna modifica alla collision matrix globale, ai collider di interazione, alla gravità o al GRIP.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **10/10 PASS** e riesecuzione BUGFIX 01 **28/28 PASS**; Console 0 errori.
- **Non verificato / prossima azione:** prova fisica su Quest 3 e correzione del launcher condiviso solo dopo autorizzazione. In attesa del BUGFIX 03.

### BUGFIX 03 — Interazione sulle spalle e targhetta GRIP (2026-11-26)

- **Causa individuata:** il GRIP non è difettoso; è il `DecisionPanel` condiviso a sovrascrivere la callback di un popup già aperto quando un altro componente chiama `AskDecision`. I flag `popupPending`/`displayedPositionChoices` rimanevano attivi e bloccavano il GRIP della spalla. La verifica della coscienza inoltre veniva registrata anche senza popup presentato.
- **Modifiche:** `ManDownPhoneUi` blocca l'apertura se un popup è già aperto; `ManDownNpc` recupera lo stato pendente, verifica la disponibilità del pannello prima di registrare la coscienza e guida tre stati della targhetta con due `LabelAnchor` locali alternate (`Persona a terra` / nascosta / `Cambia posizione`), senza toccare asset condivisi né reflection.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **17/17 PASS**; BUGFIX 01 **28/28** e BUGFIX 02 **10/10** rieseguiti; Console 0 errori; scena salvata.
- **Non verificato / prossima azione:** popup e GRIP reali su Quest 3, BUGFIX 04 e autorizzazione per il launcher condiviso.

### BUGFIX 04 — Valutazione respiratoria inconsistente (2026-11-26)

- **Cause individuate:** la zona non richiedeva la coscienza verificata né controllava i popup condivisi; il flag `completed` impediva la ripetizione finché il controller non usciva e `AwaitingExit` poteva bloccare il rilevamento dopo un cambio di posa. Il rilevamento è basato sulle ancore delle mani e sui bounds del trigger, non sugli eventi `OnTrigger*`, quindi collider multipli/raycast non erano in causa.
- **Modifiche:** `ManDownBreathingZone` accumula solo con coscienza verificata, pannello disponibile e nessun popup aperto; riparte dopo la chiusura del popup senza richiedere l'uscita; azzera su cambio mano, uscita e cambio posa. `ManDownNpc.AssessBreathing()` richiede la coscienza verificata. Durata 2,5 s, box ingrandita e pose invariate. `ManDownScenario` resta autorevole per primo/storico e timestamp.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **17/17 PASS**; regressioni 28/28, 10/10, 17/17; Console 0 errori; scena salvata.
- **Non verificato / prossima azione:** prova fisica su Quest 3, BUGFIX 05 e autorizzazione per il launcher condiviso.

### BUGFIX 05 — Duplicazione visiva dell'NPC (2026-11-26)

- **Esito:** solo documentazione, su indicazione esplicita dell'utente ("documenta e basta, mettendo il fix come pending"). Nessuna modifica a codice, pose, trigger box o asset.
- **Causa verificata:** in Editor/Play Mode puliti c'è un solo NPC, un solo `Hips`, 73 ossa e 12 `SkinnedMeshRenderer` correttamente bindati; il Pose Controller non istanzia nulla. Il doppio NPC compare solo caricando `SCN_ManDown` due volte in additivo: stessa race di `SceneMissionLauncher` del BUGFIX 02 (unload asincrono non atteso). La copia nella vecchia posa è quindi la vecchia istanza della scena, non un duplicato di mesh.
- **Fix pending:** attendere la fine dell'unload in `SceneMissionLauncher` prima di ricaricare la scena, previa autorizzazione a modificare il file condiviso di `Home`.
- **Prossima azione:** BUGFIX 06.

### Aggiornamento opzioni di posizione NPC (2026-11-26)

- **Modifica:** cinque posizioni totali (compresa `None` = ritorno supino), quattro opzioni per popup sempre escludendo la corrente; `None` ora accettato da `ManDownScenario.ChoosePosition()`, mappato a `ManDownPose.Supine` e mostrato come "Posizione iniziale (supina)".
- **Verifiche:** harness Edit Mode **14/14 PASS**; regressioni 28/28, 10/10, 17/17, 17/17; compilazione e Console pulite.
- **Prossima azione:** BUGFIX 06 / RCP.

### Feedback aptico sulla valutazione respiratoria (2026-11-26)

- **Causa:** lo scenario non generava impulsi; la sola vibrazione arrivava dalla conferma del popup condiviso, quindi non era legata al completamento della permanenza e sembrava avvenire solo la prima volta.
- **Modifica:** `ManDownBreathingZone` invia un impulso sul controller che completa i 2,5 s tramite `RigRegistration.Pulse`, con ampiezza/durata configurabili e rispetto del gate UI_HAPTICS; nessun nuovo sistema di haptics. Con respirazione anomala (`ManDownNpc.IsBreathingAbnormal`) l'impulso è più forte e lungo e viene ripetuto due volte: `abnormalDoublePulse`, ampiezza, durata e gap sono tutti serializzati e regolabili dall'Inspector.
- **Verifiche:** harness **13/13 PASS** (incluso doppio impulso anomalo); regressioni 28/28, 10/10, 17/17, 17/17, 14/14; compilazione e Console pulite.
- **Prossima azione:** prova su Quest 3 e BUGFIX 06.

### TASK 07.1 — Rilevazione fisica delle compressioni RCP (2026-11-26)

- **Richiesta utente:** zona trigger toracica unica e configurabile, solidale al corpo; controller destro/sinistro distinti senza nuovi collider condivisi; compressione solo con entrambe le mani in zona, vicine e con movimento giù/su senza pulsanti; punto medio + posizione individuale; due soglie con escursione iniziale 5 cm; ciclo con riarmo al rilascio, nessuna compressione a mani abbassate e invalidazione se un controller esce o i requisiti vengono a mancare; vibrazione breve su entrambi i controller alla pressione; timestamp e conteggi per il futuro scoring; disponibile dopo la valutazione respiratoria in posizione supina, con RCP errata su persona che respira ancora registrata; cambio posa/conclusione/reset interrompono; Gizmos e debug senza log per frame. Niente UI, ritmo, scoring, progressione RCP/DAE o infrastrutture per altre difficoltà.
- **File creati:** `Code/Scripts/ManDown/ManDownCpr.cs` (+ `.meta`). **File modificati:** `Level/Scenes/SCN_ManDown.unity` (nuovo `MESH_ChestCompressionZone` figlio di `Hips/Spine/Spine1`, BoxCollider trigger, componente collegato a scenario e NPC), `Man_Down_Specifiche.md` (§5.4.1, feedback aptico e tabella interazioni), `TASK_PROGRESS.md` e questo file. Nessun asset condiviso, ProjectSettings, pose salvate, collider dei controller o zone spalla/bocca toccati.
- **Comportamento implementato:** rilevamento con le sole ancore `RigRegistration.LeftHand`/`RightHand` e bounds della zona, quindi controller con collider multipli restano una mano ciascuno; asse `transform.up` della zona e soglie in metri dal centro (`0,03` / `0,08`, distanza max `0,25`, tolleranza individuale derivata dall'escursione); stati Idle/Armed/Pressed, pressione e completamento solo con punto medio ed entrambe le mani alle soglie, invalidazione una volta per ciclo e nuovo rilascio richiesto; `StartCpr()` alla prima pressione anche con respirazione normale; `RigRegistration.Pulse` su entrambe le mani con ampiezza/durata configurabili; eventi e liste di timestamp con `MissionElapsedSeconds`; Gizmos delle due soglie e delle mani, `debugLog` default false.
- **Verifiche eseguite:** compilazione `compilationFailed=false` e 0 errori (4 warning `CS0414` preesistenti); harness Edit Mode temporaneo fuori dal repository **18/18 PASS** con rig e bridge aptici finti; 1 solo `ManDownCpr`, bounds senza intersezione con spalla/bocca, scene `isDirty=false`, un solo root, zero oggetti di test residui; Console `consoleErrors=0`.
- **Non verificato:** Play Mode e prova su Quest 3 del gesto reale, della vibrazione e della calibrazione; ritmo, qualità e scoring non implementati. **Prossima azione:** microtask 07.2; fix di `SceneMissionLauncher` ancora pending. Nessun commit o push.

### TASK 07.2 — Valutazione della qualità ritmica RCP (2026-11-26)

- **Richiesta utente:** intervalli tra pressioni di compressioni fisicamente complete (non dai rilasci), prima compressione come riferimento senza valutazione, parametri iniziali 0,55/0,50/0,60/0,40 s configurabili, errore `Max(0, min − intervallo, intervallo − max)` e qualità `Clamp01(1 − errore/tolleranza)`, media delle qualità distinta dall'errore medio, statistiche per il futuro scoring, interruzione esplicita senza cancellare i dati, nessun reset del riferimento all'uscita dei controller, reset completo che azzera tutto, RCP su persona che respira registrata ma non positiva per lo scoring, debug senza log per frame. Niente UI, metronomo, progressione o scoring finale.
- **File creati:** `Code/Scripts/ManDown/ManDownCprRhythm.cs` (+ `.meta`) con `ManDownCprIntervalOutcome`, `ManDownCprIntervalRecord` e `ManDownCprRhythmSnapshot`. **File modificati:** `SCN_ManDown.unity` (nuovo componente su `MESH_ChestCompressionZone` con riferimenti a CPR e scenario), `Man_Down_Specifiche.md` (§5.4.2), `Man_Down_Scoring_System.md` (§4 RCP), `TASK_PROGRESS.md` e questo file. `ManDownCpr.cs` invariato. Nessun asset condiviso, ProjectSettings o altro gruppo toccato.
- **Comportamento implementato:** pairing pressione→compressione completa tramite eventi; pressione invalida che non entra nella sequenza ma non azzera il riferimento; first-reference senza valutazione; contatori corretti/troppo brevi/troppo lunghi, percentuale, errore cumulativo/medio, intervallo medio, qualità complessiva, storico e ultimo esito; `InterruptSequence()` conserva le statistiche, `ResetStatistics()` a nuova missione; epsilon `1e-4` alle soglie e qualità `Clamp01`; `IsApplicableForScoring` false con respirazione normale; snapshot serializzato consultabile in Inspector; `InterruptSequence()` e `ResetStatistics()` richiamabili dal menu contestuale dell'Inspector (`SI81/...`) anche in Play Mode; log solo su eventi, mai per frame.
- **Verifiche eseguite:** compilazione 0 errori (4 warning `CS0414` preesistenti); harness Edit Mode **26/26 PASS** su tutti i casi richiesti e sulla calibrazione dei parametri; regressione TASK 07.1 **18/18 PASS**; scena salvata `isDirty=false`, un solo root e un solo componente per tipo, zero oggetti di test residui.
- **Non verificato / prossima azione:** Play Mode e Quest 3 per il gesto reale e la calibrazione; nessun `frequencyFactor`, penalità o scoring applicato. Microtask 07.3; fix di `SceneMissionLauncher` ancora pending. Nessun commit o push.

### TASK 07.3 — Integrazione RCP nello scenario (2026-11-26)

- **Richiesta utente:** RCP dopo la valutazione respiratoria e in posizione supina, anche con respirazione normale senza progressione positiva né istruzioni; messaggio iniziale non bloccante una sola volta con il parametro configurato; progressione 20/20/30 configurabile sul solo numero di compressioni fisicamente complete; distinzione iniziale/interruzione/dopo DAE senza implementare il DAE; monitoraggio esistente a 30 compressioni senza DAE; avviso di ritmo insufficiente con parametri configurabili; registrazione centrale dei dati; nessuna UI personalizzata, metronomo, QTE, scoring, penalità o timer alternativo; reset e conclusione corretti.
- **File creati:** `Code/Scripts/ManDown/ManDownCprFlow.cs` (+ `.meta`). **File modificati:** `ManDownScenario.cs` e `ManDownScenarioTypes.cs` (`ManDownCprQualitySnapshot`, `CprStartedAt`, snapshot e `CprQuality` nel risultato, `ReportCprQualitySnapshot()`, nota facoltativa in `StartFinalMonitoring()`), `SCN_ManDown.unity` (nuovo componente con riferimenti e parametri 0,60/5/5), `Man_Down_Specifiche.md` (§5.4.3), `Man_Down_Scoring_System.md`, `TASK_PROGRESS.md` e questo file. Detector e valutazione ritmica invariati; nessun asset condiviso, ProjectSettings, rig o altro gruppo toccato.
- **Comportamento implementato:** istruzioni e avvisi solo tramite `Alert` comune; progressione per fase con soglie lette dallo scenario e completamento una sola volta; interruzione per DAE che conserva le statistiche; monitoraggio finale riutilizzato a 30 compressioni senza DAE; snapshot grezzo nel controller e nel risultato; conclusione che disattiva il flusso e reset che azzera tutto.
- **Verifiche eseguite:** compilazione 0 errori (4 warning `CS0414` preesistenti); harness Edit Mode **23/23 PASS**; regressioni 07.1 **18/18** e 07.2 **26/26 PASS**; scena `isDirty=false`, un solo root e un componente per tipo, zero oggetti di test residui.
- **Non verificato / prossima azione:** Play Mode, visore e messaggi UI reali; DAE, penalità e scoring finale demandati. Task 08; fix di `SceneMissionLauncher` ancora pending. Nessun commit o push.

### TASK 08.1 — Scatola DAE e apertura manuale (2026-11-26)

- **Richiesta utente:** sostituire nella scena la scatola a mesh unica con un placeholder funzionale in primitive senza toccare mesh/texture/prefab originali; sportello apribile manualmente con GRIP e movimento del controller, senza HingeJoint né nuovo sistema di grab; LabelAnchor Interaction "Defibrillatore (DAE)"; dispositivo provvisorio separato; riutilizzare la randomizzazione 75/25 già presente; niente recupero, accensione, elettrodi, analisi, scarica, audio, scoring o UI personalizzata.
- **Verifica preliminare (eseguita, esito positivo):** `Interactable.followsHand=false` impedisce a `RayInteractor` di registrare offset, rendere kinematico un Rigidbody e muovere l'oggetto in `LateUpdate`; `GrabEntered`/`GrabExited` consegnano la mano e `RigRegistration.TryGetAimPose` dà la posa mondo. L'apertura manuale era quindi implementabile con le API esistenti; l'alternativa automatica non è stata avviata.
- **File creati:** `Code/Scripts/ManDown/ManDownAedDoor.cs` e `ManDownAedBox.cs` (+ `.meta`); sette materiali URP Lit in `Art/Materials/Man_Down/AEDBox/`; `Level/Prefabs/Man_Down/MESH_AEDBox.prefab` e `MESH_AEDPlaceholder.prefab` (+ `.meta`).
- **File modificati:** `SCN_ManDown.unity` (`MESH_AED` rimosso dall'istanza e sostituito da `MESH_AEDBox` alla stessa posizione, un solo root, scenario collegato); `Man_Down_Specifiche.md` (§7 e §7.1), `TASK_PROGRESS.md` e questo file. `MESH_AED.obj`, `TEX_AED` e `MESH_AED.prefab` intatti; nessun asset condiviso, `ProjectSettings`, rig, UI o altro gruppo toccato.
- **Comportamento implementato:** sportello vincolato all'asse della cerniera con angolo 0–110° e soglia di recupero 70°, più verso di apertura configurabile; scatola fissa, angolo mantenuto al rilascio e nuovo GRIP per richiudere. Disponibilità letta solo da `ManDownScenario.IsAedAvailable`: DAE assente → visivi e dispositivo disattivati e GRIP spento; conclusione → interazioni disabilitate; reset → scatola chiusa e dispositivo nel punto iniziale. Etichetta comune nascosta in presa e ripristinata al rilascio.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **25/25 PASS**; Play Mode senza visore con flag tutti OK e 0 errori; bounds e riferimenti riletti; scena `isDirty=false`; render di controllo in temp. **Non verificato:** gesto reale su Quest 3 e calibrazione del verso/soglia; recupero e procedure DAE restano al Task 08.2. Nessun commit o push.

### TASK 08.2 — Recupero, trasporto e accensione automatica del DAE (2026-11-26)

- **Richiesta utente:** dispositivo stabile nella scatola e non afferrabile a sportello chiuso; primo GRIP valido dopo l'apertura minima → estrazione, trasporto libero e accensione automatica una volta per missione; indicatore grigio/verde configurabile senza toccare materiali condivisi; registrazione centrale di stato e timestamp distinta dall'utilizzo corretto; reset e conclusione corretti; nessun elettrodo, analisi, scarica, audio o scoring.
- **File creati:** `Code/Scripts/ManDown/ManDownAedDevice.cs` (+ `.meta`). **File modificati:** `ManDownScenario.cs`, `ManDownScenarioTypes.cs`, `ManDownAedBox.cs`, i due prefab `MESH_AEDPlaceholder.prefab` e `MESH_AEDBox.prefab`, `SCN_ManDown.unity` (solo riferimento `device.scenario`) e i documenti del gruppo. Nessun asset originale, condiviso o di altro gruppo modificato.
- **Comportamento implementato:** gate della presa basato su `door.IsOpenEnoughForRetrieval` (70°); primo GRIP → `AED_RETRIEVED` e `AED_POWERED_ON` con timestamp, eventi e indicatore verde; presa indipendente dallo sportello dopo il recupero; Rigidbody cinematico/senza gravità in scatola e dinamico/con gravità dopo il rilascio; collisioni con il rig ignore selettivamente; etichetta Interaction del dispositivo nascosta a sportello chiuso e in presa, visibile dopo il rilascio; etichetta della scatola soppressa a porta aperta o dopo il recupero. Reset ripristina posa, rotazione, scala, fisica, stato spento e registro; conclusione disattiva le interazioni.
- **Parametri configurabili:** su `ManDownAedDevice` riferimenti a scenario/Interactable/LabelAnchor/renderer dell'indicatore e colori `offColor`/`onColor`; la soglia di apertura resta `retrievalAngle` su `ManDownAedDoor` (Task 08.1), senza duplicazione.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **28/28 PASS** e regressione 08.1 **25/25 PASS**; Play Mode con 0 errori e ciclo completo verificato (spento→80°→GRIP→due azioni→verde→dinamico al rilascio); scena `isDirty=false`. **Non verificato:** prova fisica su Quest 3 e inerzia del lancio (fuori dal task). Nessun commit o push.

### TASK 08.3 — Elettrodi e aggancio magnetico (2026-11-26)

- **Decisione approvata su chiarimento:** il testo conteneva sia il divieto di cavi (§1 e §13) sia una sezione che lo sostituiva con due `LineRenderer`. L'utente ha scelto di **implementare i cavi**, senza un quarto caso aggiuntivo, con resets alla nuova missione. Il vecchio divieto nelle specifiche è stato aggiornato.
- **File creati:** `Code/Scripts/ManDown/ManDownAedElectrode.cs`, `ManDownAedAttachments.cs`, `ManDownAedLeads.cs` (+ `.meta`), `MESH_AedElectrode.prefab` (+ `.meta`), materiali `MAT_AedElectrode_Pad` e `MAT_AedLead`. **File modificati:** `ManDownAedDevice.cs`, `ManDownScenario.cs`, `ManDownScenarioTypes.cs`, `MESH_AEDPlaceholder.prefab`, `SCN_ManDown.unity`, documenti del gruppo. Pose, RCP, rig e asset condivisi invariati.
- **Comportamento implementato:** elettrodi recuperabili solo dopo DAE acceso; rilascio vicino a una zona libera con NPC supino → aggancio alla zona più vicina, allineamento, fisica disattivata, registro centrale e timestamp storico; rimozione con GRIP che libera la zona e riporta l'elettrodo allo slot; mancato aggancio senza penalità; intercambiabilità dei due elettrodi; cavi visivi che seguono DAE/elettrodi e si resettano; interazioni disattivate a conclusione.
- **Parametri configurabili:** `snapDistance` 0,10 m sul componente delle zone; `width` 0,004 m, `pointCount` 16 e `maxSag` 0,15 m su `ManDownAedLeads`; posizione/rotazione delle zone e materiali dedicati. Zone A/B figlie di `Spine2` e fuori dal volume RCP.
- **Verifiche eseguite:** compilazione 0 errori; harness Edit Mode **23/23 PASS**, regressioni 08.2 **28/28** e 08.1 **25/25**; Play Mode 0 errori con doppio aggancio e quattro azioni corrette; scena `isDirty=false`, un solo root. **Non verificato:** prova fisica su Quest 3 e resa dinamica dei cavi; analisi, scarica, audio e scoring restano ai task successivi. Nessun commit o push.
- **BUGFIX pavimento (2026-11-26):** segnalato che gli elettrodi cadono sotto il pavimento; causa verificata nel `MeshCollider` non convesso a spessore zero del `Floor` (anche un cubo di prova attraversava). Aggiunto un `BoxCollider` sul prefab di gruppo `MESH_OfficeStructure.prefab`, mantenendo il `MeshCollider`; test `Physics.Simulate` in Play Mode: cubo ed elettrodo si arrestano sulla superficie. Nessun altro asset toccato.

### BUGFIX 08.3.1 — Aggancio magnetico degli elettrodi (2026-11-26)

- **Segnalazione:** in VR gli elettrodi si afferrano e rilasciano ma non sembrano agganciarsi. **Causa verificata:** `RayInteractor.ReleaseGrab()` chiama `GrabExit` e subito dopo ripristina `isKinematic`/`useGravity` con i valori del grab, sovrascrivendo la fisica impostata nel callback: l'aggancio riusciva ma l'elettrodo ricadeva dalla zona. Zone, posa supina e tolleranza risultavano corrette.
- **Correzione:** consolidamento della fisica in `LateUpdate` (dopo l'Update del grab condiviso), con riaffermazione di posa/rotazione dalla zona; log mirato solo al rilascio con distanze, tolleranza, posa, disponibilità zone, esito e motivo; Gizmos con raggio di tolleranza. Nessun sistema condiviso, pose, RCP o cavi modificato; tolleranza invariata a 0,10 m.
- **Verifiche:** compilazione 0 errori; harness 08.3 **23/23 PASS**; simulazione dell'ordine reale di rilascio in Play Mode con elettrodo stabile sulla zona dopo 2 s; scena salvata e pulita. **In attesa della conferma del test VR dell'utente: non procedere con il Task 08.4.** Nessun commit o push.

### TASK 08.4 — Analisi cardiaca, scarica e valutazione temporale (2026-11-26)

- **File modificati:** `ManDownAedDevice.cs`, `ManDownCpr.cs`, `ManDownScenario.cs`, `ManDownScenarioTypes.cs`, `MESH_AEDPlaceholder.prefab`, `SCN_ManDown.unity`, documenti del gruppo. Testo del task pervenuto troncato a «Prevedi»: implementazione basata su obiettivi e descrizione del Task 08.5.
- **Comportamento:** analisi automatica a elettrodi applicati/NPC supino/mani lontane; interruzione su contatto, rimozione elettrodo o cambio posa; esito da `IsBreathingAbnormal` (semplificazione concordata); autorizzazione con beep+indicatore+QTE sincronizzati; pulsante con `Select` del sistema condiviso; blocco di sicurezza senza nuovo beep; scarica una sola volta; indicatori configurabili; snapshot grezzo ed eventi per il Task 08.5. Nessuna progressione RCP, scoring, penalità quantificata o altra difficoltà.
- **Parametri:** analisi 3 s, hands-clear 0,15 s, grace 1,5 s, zero 8 s, blink 2 Hz, margine contatto 0,05 m, audio volume/distanze e colori degli stati.
- **Verifiche:** compilazione 0 errori; harness **27/27 PASS** (formula ai valori di esempio e divisione per zero inclusi), regressione 08.3 **23/23 PASS**, zone regolate manualmente preservate, scena pulita. Nessun commit o push.

### TASK 08.5 — Integrazione DAE, RCP e nuova progressione temporale (2026-11-26)

- **Modificati:** `ManDownScenario.cs`, `ManDownScenarioTypes.cs`, `ManDownCprFlow.cs`; scena con riferimento `aedDevice` del flusso. Testo pervenuto parzialmente troncato, implementato dalle sezioni complete.
- **Rimosso:** soglie 20/20/30, completamenti di fase, monitoraggio aggiuntivo nel ramo anomalo e metodi/parametri obsoleti. Conservati contatori, ritmo e monitoraggio del ramo normale.
- **Nuovo:** finestra 45 s dalla prima valutazione anomala, proroga 15 s una sola volta con condizioni rigorose, accumulo RCP come somma delle qualità degli intervalli, sospensione RCP durante analisi/scarica, statistiche separate in proroga, conclusione ordinaria distinta dal timeout.
- **Verifiche:** compilazione 0 errori, harness **11/11 PASS**, regressioni 08.4 **27/27**, 08.B2 **7/7**, 08.3 **23/23**, feedback elettrodi **6/6 PASS**; scena pulita. In attesa conferma VR; non procedere con 08.6. Nessun commit o push.

### TASK 09.1 — Sistema di scoring locale (2026-11-26)

- **Creato:** `ManDownScoring.cs` con `ManDownScoreResult` e componente su `MNG_Scenario`. Calcolo separato dalle interazioni; SPI con `ScoreRules.ScoreFromDimensions` (media delle cinque dimensioni), formula condivisa invariata.
- **Regole:** Safety con penalità una sola volta; Awareness 30/70; Decision 112 + condizioni del ramo; Execution anomalo da valore RCP grezzo/90 con cap 60 e traguardi DAE 5+5+10+20, normale 100/−50; Time con soglie 20/60. Pesi e penalità configurabili da Inspector. Nessun punteggio visibile in missione; nessuna integrazione con EndMission/esiti/debriefing (Task 09.2).
- **Verifiche:** compilazione 0 errori, harness **12/12 PASS** su tutti i casi richiesti e limiti 0-100, regressioni 08.5 **11/11**, 08.4 **27/27**, 08.B2 **7/7**, 08.3 **23/23**, feedback elettrodi **6/6 PASS**; scena pulita. Nessun commit o push.

### TASK 09.2 — Integrazione scoring, esiti e debriefing (2026-11-26)

- **Modificato:** `ManDownScenario.cs`. Conclusione centralizzata con `EndMission(outcome, -1, dimensions, debriefLines)`; SPI interno con arrotondamento UI; esito con condizioni vincolanti (timeout/RCP omessa → Negative; 112 omesso o Execution<50 → al massimo Partial); debrief dinamico italiano con `DebriefLinePrefix`, max 8 righe, senza numeri o giudizi clinici. Soglie 80/50 ed Execution 50 configurabili da Inspector.
- **Verifiche:** compilazione 0 errori, harness **12/12 PASS**, validatore `SI81/Scenari/Valida scenario` **0 errori/9 avvisi di naming**, regressioni 09.1 **12/12**, 08.5 **11/11**, 08.4 **27/27**, 08.B2 **7/7**, 08.3 **23/23 PASS**; scena pulita. Nessun commit o push. Prossima: Task 10.

### TASK 09.3 — Effetti sonori dello scenario (2026-11-26)

- **Creato:** `ManDownScenarioAudio.cs` su `MNG_Scenario`, due AudioSource 2D figli. Notifica `SFX_Notifica_soccorsi` su `EmergencyCalled` (una volta per missione); sirena `SFX_Ambulance` su `MissionSession.Ended` solo con 112, per ogni esito, con fade out 3 s a tempo non scalato e ripristino volumi. Nessun file audio modificato.
- **Verifiche:** compilazione 0 errori, harness **15/15 PASS** su tutti i casi richiesti; scena pulita. Nessun commit o push.

### BUGFIX 08.B1 — Collisioni DAE ed elettrodi con il player (2026-11-26)

- **Causa verificata:** DAE ed elettrodi già ignoravano il `CharacterController` del rig come i telefoni, ma il BoxCollider del pulsante di scarica (`MNG_ShockButton`, figlio del DAE) non era compreso nell'ignore e, muovendosi con il dispositivo afferrato, spingeva il player.
- **Correzione:** `IgnorePlayerBodyCollisions()` del DAE e dell'elettrodo usa `GetComponentsInChildren<Collider>(true)` invece di `GetComponents<Collider>()`. Nessuna modifica a rig, grab, telefoni, matrice globale o layer.
- **Verifiche:** in Play Mode tutti i collider del DAE risultano ignorati con `MNG_CoreRig`; i telefoni invariati; caduta su pavimento confermata; harness 08.4 **27/27** e 08.3 **23/23 PASS**; scena pulita. **In attesa della conferma VR; non procedere con il Task 08.5.** Nessun commit o push.

### BUGFIX 08.B2 — Rilevazione imprecisa del contatto con l'NPC (2026-11-26)

- **Causa verificata:** unico trigger rettangolare troppo grande (0,55×0,40×1,90 m + margine 0,05) con controllo su AABB rispetto al corpo reale (0,26×0,22×1,78 m): spazio vuoto intorno all'NPC rilevato come contatto. Nessun problema di collider delle mani, rig, layer o bugfix 08.B1.
- **Correzione:** nuovo `ManDownAedContactZones` con sette trigger locali su testa, torace, bacino, braccia e gambe, figli delle ossa; test orientato nel sistema locale del BoxCollider dalle sole ancore delle mani; margine 0; log solo al cambio di stato con mano/zona; Gizmos viola (rossi se attivi) e regolazione Inspector.
- **Verifiche:** harness dedicato **7/7 PASS**, harness 08.4 **27/27 PASS**, compilazione e Console pulite, scena salvata, zone elettrodi invariate. **In attesa della conferma VR; non procedere con il Task 08.5.** Nessun commit o push.

### BUGFIX 08.B3 — Interazione con il pulsante di scarica (2026-11-26)

- **Causa verificata:** collider del pulsante interamente dentro quello del corpo del DAE (il ray colpiva il corpo di grab, non il pulsante) e `Selectable` del pulsante mai aggiornato dopo l'accensione. Nessun problema di layer, grab, contatto o collisioni con il player.
- **Correzione:** pulsante sporgente nel prefab, `RefreshInteraction()` nello stato acceso, log mirati su pressione/accettazione/rifiuti con motivo, impulso aptico e movimento visivo della mesh, Gizmo ciano. Pressione diretta non supportata dal sistema condiviso: confermato l'uso di `Select` con grilletto.
- **Verifiche:** raycast colpisce il pulsante per primo; Play Mode con `selectable=True` e scarica completata (qualità 0,923); harness 08.4 **27/27** e 08.B2 **7/7 PASS**; compilazione e Console pulite. **In attesa della conferma VR; non procedere con il Task 08.5.** Nessun commit o push.

### TASK 10.1 — Revisione coerenza scoring/esiti/debriefing (2026-11-26)

- **Modificati:** `ManDownScoring.cs` (snapshot RCP autorevole, `RcpPoints`/`RcpMaxPoints`, `CallFullScoreUntil`), `ManDownScenario.cs` (`ScoringCprSnapshot`, timeout letto dal risultato congelato, `AED_OMITTED` solo se mai recuperato, debrief per respirazione non valutata/normale/anomala e confronto quantità RCP con il cap), `ManDownCprFlow.cs` (flag `Inappropriate` non più forzato a true). Nessuna scena, prefab, UI, label, audio, timer, peso o soglia modificata.
- **Verifiche:** compilazione 0 errori (5 warning `CS0414` preesistenti); harness Edit Mode temporaneo fuori dal repository **20/20 PASS** su 18 casi; regressioni 09.1 **12/12**, 09.2 **12/12**, 08.5 **11/11 PASS**. Nessun test VR eseguito; nessun commit o push.
- **Problemi aperti:** conferma VR dei casi anomalo/DAE, timeout prima della valutazione e posizione errata poi corretta; Task 10 (test completi) e 13 (difficoltà) restano su richiesta esplicita.
