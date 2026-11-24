# EVACUATION — BRIEF PER L'AGENTE
Fonte di verità. NON leggere PDF di consegna, GDD o SDD: tutto ciò che serve è qui.
[PROPOSTA] = scelta di design non nel GDD/SDD, da confermare col team. [DA VALIDARE] = contenuto di sicurezza da validare col referente.

## Il tuo scope
Devi SOLO: (1) montare la scena dello scenario Evacuation, (2) implementare la logica/gameplay.
NON fare: UI/HUD/menu/pannelli/sottotitoli/schermate di esito/vignette (le farà un altro team), animazioni (le fa il modellatore), modelli/texture/audio definitivi, git, Common Core, altri scenari, **modifiche a Chemical Alert**.
Dove il gioco "mostrerebbe" qualcosa (timer, cuori, vignetta rossa, dialogo NPC, hint, esito) tu **emetti eventi C# + stato leggibile** + log `[EV]`. La UI si collegherà dopo.
Ogni task termina con i **comandi di test** (vedi EV_01_CONVENTIONS).

## Progetto
Serious game VR "Sicurezza Immersiva 81" (Unity 6000.3, URP, OpenXR, Meta XR Core SDK, target Quest). Noi: Chemical Alert (FATTO, congelato) + Evacuation (questo). Principio: OSSERVA → VALUTA → DECIDI → AGISCI → CONSEGUENZE → IMPARA.

## Scenario
Ufficio, singolo piano. Suona l'allarme di evacuazione (Guided/Operational: semplice evacuazione; Crisis: incendio attivo). Il giocatore deve: ispezionare il piano di evacuazione, far evacuare i colleghi di ENTRAMBE le stanze (raccogliendoli in fila indiana), raggiungere le scale di EMERGENZA. L'ascensore è letale.
Timer: Guided cronometro senza limite · Operational 180 s · Crisis 120 s.

### Layout (DEFINITIVO, scala 1:1, metri; X = est, Z = nord; Y=0 pavimento, soffitto 3 m)
La pianta è quella dell'SDD; il modello 3D finale è costruito su di essa e sostituirà il graybox (E11) con piccoli aggiustamenti di misura. Le coordinate sono approssimate al metro: conta il NOME dei marker e le relazioni. Tutto il gameplay è ancorato a marker/prefab, mai a coordinate nel codice.
- ZN-01 Ufficio 1: x 2–8, z 8–14. 4 scrivanie+sedie (prop). Porta sul lato sud (z=8) a x≈5.
- ZN-03 Ufficio 2: x 8–14, z 8–14, identico. Porta sul lato sud a x≈10.
- ZN-02 Corridoio: x 2–14, z 5–8.
- Fondo corridoio SINISTRA (x 0–2, z 5–8): **scale INTERNE** (SBAGLIATE) con porta.
- Lato SUD del corridoio (x 6–9, z 2.5–5): **scale di EMERGENZA** (CORRETTE) con porta sbarrafuoco e cartello illuminato sopra.
- Fondo corridoio DESTRA (x 14–16, z 5–8): **ascensore** (LETALE) con pulsanti di chiamata.
- P0 giocatore: (2.7, 13) angolo opposto alla porta della stanza 1. 3 NPC ufficio 1, 2 NPC ufficio 2 (totale **5**).
- Piani di evacuazione (3): all'entrata ufficio 1 (≈4.2, 8), all'entrata ufficio 2 (≈10.1, 8), nel corridoio (≈5, 5.1).
- Allarme sul soffitto del corridoio.

### Marker (empty, sotto un root `ScenarioMarkers`)
`SPAWN_Player`, `NPCSLOT_R1_01..06`, `NPCSLOT_R2_01..06` (posti possibili per NPC), `POS_Plan_R1`, `POS_Plan_R2`, `POS_Plan_Corridor`, `POS_Alarm`, `POS_Elevator`, `POS_ElevatorButton`, `ZONE_StairsEmergency`, `ZONE_StairsInternal`, `ROUTE_01..N` (polilinea del percorso ideale P0→porta stanza→corridoio→porta scale emergenza; una variante per ciascuna stanza: `ROUTE_R1_*`, `ROUTE_R2_*`), `FIRESLOT_01..10`.

### Procedura corretta [DA VALIDARE]
1. Ispezionare il piano di evacuazione. 2. Raccogliere i colleghi della propria stanza. 3. Controllare la seconda stanza e raccogliere i colleghi. 4. Evitare l'ascensore e le scale interne; raggiungere le scale di emergenza (cartello illuminato) entro il tempo.

## Regole di gioco (CONFERMATE dall'utente salvo [PROPOSTA])
- **Allarme**: attivo da t=0 (inizio fase Action) fino alla fine. Segnale acustico (AudioClip opzionale) + luce pulsante (emissione materiale, NIENTE luci realtime extra, ≤3 Hz).
- **Piano di evacuazione**: 3 istanze ispezionabili; basta ispezionarne UNA per `PlanInspected`. Ispezione = hover + trigger (Activated) OPPURE sguardo fermo ≤1.2 m per 0.75 s [PROPOSTA]. Testo: chiave `evac.plan.route`.
- **NPC (5)**: restano fermi alle scrivanie e NON evacuano da soli. Interazione (Activated con la mano nell'area di interazione ~1 m) → l'NPC diventa **Following** e si mette in **fila indiana** dietro il giocatore (ordine di raccolta, distanza 0.8–1 m, segue il PERCORSO del giocatore a "briciole di pane", niente NavMesh) [PROPOSTA tecnica]. Emette `NpcDialogueRequested(key)` (`evac.npc.dialogue.follow`, `evac.npc.dialogue.already`). Un NPC è **evacuato** quando il giocatore evacua se stesso con lui al seguito.
- **Ascensore**: interagire con un pulsante di chiamata = fallimento immediato (non serve entrare). In **Guided** NON è sconfitta: evento di pericolo Down + ActionLog errore, scenario continua, esito massimo Partial [PROPOSTA].
- **Scale interne** (sbagliate): raggiungere `ZONE_StairsInternal` termina lo scenario con **Partial** [PROPOSTA]; log `evac.stairs.internal`.
- **Scale di emergenza**: raggiungere `ZONE_StairsEmergency` (dopo aver aperto la porta) termina lo scenario; esito da valutatore.
- **Porte**: `DoorInteractable` provvisoria (Activated = apre/chiude istantaneo, `UnityEvent OnOpened/OnClosed`, collider si disattiva da aperta). Le porte stanza possono partire aperte; quelle delle scale chiuse.
- **Fuoco (SOLO Crisis)**: 10 `FIRESLOT`, **5 attivi** per partita (seed). "Contatto" = il giocatore ENTRA nel collider della zona fuoco (cooldown 2 s sullo stesso ingresso) [PROPOSTA]. Contatto 1 = scottatura (`FireContact(1, intensity)`), 2 = vignetta aumentata (intensity più alta), **3 = morte**. Crisis ha **1 cuore** → il 3° contatto = cuori 0 → `Died` → Failure. I contatti sono un contatore separato dai cuori. Posizioni "strategiche" (danno fastidio, tolgono il passaggio comodo) ma **mai bloccanti**: deve restare sempre una corsia libera ≥1.2 m sui percorsi ROUTE (validazione automatica, vedi E09). Fumo: particelle con tetto, flag "riduci effetti".
- **Cuori**: Guided nessun danno/morte; Operational 3 (nessuna fonte di danno oltre l'ascensore, che è fallimento immediato); Crisis 1.

## Esiti
- **Success**: piano ispezionato + TUTTI i 5 NPC al seguito all'uscita + scale di emergenza + entro il tempo + ascensore mai usato + vivo.
- **Partial**: scale di emergenza raggiunte vivo ma con qualcosa di mancante (NPC lasciati, piano non ispezionato [PROPOSTA]) · scale interne · (Guided) ascensore usato.
- **Failure**: ascensore (non Guided) · cuori a 0 (Crisis) · tempo scaduto (non Guided).
Esito = evento `ResultReady(ScenarioResult)` con esito + ActionLog + sotto-punteggi. Retry = reset completo (`ResetScenario()`, include il teletrasporto del rig a `SPAWN_Player`).

## Difficoltà (codice: Guided / Operational / Crisis; il testo "Stress" dei documenti = Crisis)
| | Guided | Operational | Crisis |
|---|---|---|---|
| Ambiente | evacuazione semplice | evacuazione semplice | incendio: fuoco+fumo |
| Tempo | cronometro, niente sconfitta | 180 s | 120 s |
| Evidenziazione | piani, NPC, porta/uscita emergenza (emissione #E69F00 pulsante, MaterialPropertyBlock) | no | no |
| Aiuti | `HintRequested(key)` (dopo X s senza progressi e a richiesta) + pericolo up/mid/down | nessuno | nessuno |
| Cuori | nessun danno | 3 | 1 |
| Punti GDD | — | 30 / parz. 10 / 0 | 50 / parz. 20 / 0 |
Tutto in `SO_EvacuationConfig_<Livello>`; mai hardcoded. Chiavi hint: `evac.hint.plan`, `evac.hint.room1`, `evac.hint.room2`, `evac.hint.stairs`, `evac.hint.noelevator`.

## Valutazione: 5 dimensioni, sotto-punteggi 0–100 (SPI lo calcola il Game System comune) [PROPOSTA]
- **Safety**: 100; ascensore → 0; contatto fuoco 1 e 2: −25 ciascuno; 3° contatto → 0.
- **Awareness**: 100 se piano ispezionato, altrimenti 30.
- **Decision**: 100; −30 se scale di emergenza raggiunte con NPC mancanti; −40 scale interne; −60 ascensore (min 0).
- **Execution**: 100 × (NPC raccolti / 5).
- **Time**: 0 se timeout; altrimenti 100 se finito entro il 60% del tempo, scende linearmente a 40 al limite (Guided: 100 entro 90 s, scende a 40 a 240 s). Uscire dalla stanza senza aver fatto nulla nei primi 5 s (impulsivo) → −20.

## Registro azioni (ActionLog.Add(id, isCorrect, dimension, consequenceKey))
| ID | Quando | Esito | Dim |
|---|---|---|---|
| evac.lg01 | prima ispezione piano | Corretto | Awareness |
| evac.lg02 | uscita senza aver ispezionato alcun piano | Mancato | Awareness |
| evac.lg03 | tutti gli NPC dell'ufficio 1 raccolti | Corretto | Execution |
| evac.lg04 | tutti gli NPC dell'ufficio 2 raccolti | Corretto | Execution |
| evac.lg05 | scale emergenza con NPC mancanti | Rischioso | Decision |
| evac.lg06 | uso ascensore | Errore critico | Safety |
| evac.lg07 | contatto fuoco 1°/2° | Rischioso | Safety |
| evac.lg08 | 3° contatto (morte) | Errore critico | Safety |
| evac.lg09 | scale emergenza con tutti gli NPC | Corretto | Decision |
| evac.lg10 | timer scaduto | Mancato | Time |
| evac.lg11 | scale interne | Rischioso | Decision |
Chiavi conseguenza/frasi: `evac.log.lgNN.phrase`, `evac.log.lgNN.hint` (solo chiavi nel codice).

## Requisiti di design da rispettare
≥3 decision point: DP1 ispezionare il piano · DP2 controllare entrambe le stanze · DP3 scale emergenza vs interne vs ascensore · DP4 (Crisis) aggirare il fuoco. ≥3 interazioni (inspect piano, communicate NPC, activate porte/pulsante). Rischio visibile (allarme, fuoco) e nascosto (NPC della seconda stanza che non evacuano da soli, ascensore). Errore critico con conseguenza leggibile (ascensore, 3° contatto). Evento dinamico: allarme, accensione fuoco in Crisis, NPC che seguono. Replay [PROPOSTA]: posti NPC casuali tra gli slot, fuoco 5 su 10 casuali, seed loggato. Stessa categoria di interazione = stesso comportamento (porte come in CA).
Testi per l'utente: solo **chiavi stabili**, mai stringhe italiane nel codice.

## Allegato frasi debrief (leggi SOLO in E12)
lg01 "Al {t} hai consultato il piano di emergenza ed evacuazione. Conseguenza: hai individuato il percorso verso le scale." · lg02 "Non hai controllato il piano. Conseguenza: orientamento meno sicuro. Cosa potevi fare: ispezionare le planimetrie all'entrata delle stanze o nel corridoio." · lg03 "Al {t} hai avvisato i colleghi della tua stanza. Conseguenza: nessuno lasciato indietro nel primo ufficio." · lg04 "Al {t} hai controllato l'altro ufficio e fatto evacuare tutti. Conseguenza: piano sgomberato." · lg05 "Al {t} hai raggiunto le scale senza controllare entrambi gli uffici. Conseguenza: colleghi rimasti indietro, vittoria parziale. Cosa potevi fare: controllare entrambe le stanze." · lg06 "Al {t} hai usato l'ascensore durante l'allarme. Conseguenza: perdita automatica. Cosa potevi fare: non usarlo mai in emergenza, seguire il cartello verso le scale." · lg07 "Al {t} ti sei avvicinato troppo alle fiamme. Conseguenza: scottatura. Cosa potevi fare: mantenere la distanza dal fuoco." · lg08 "Al {t} sei entrato di nuovo in contatto con il fuoco. Conseguenza: perdita di una vita. Cosa potevi fare: aggirare i focolai." · lg09 "Al {t} hai raggiunto le scale di emergenza con tutti i colleghi. Conseguenza: evacuazione completata." · lg10 "Al {t} il tempo è scaduto prima delle scale. Conseguenza: livello fallito." · lg11 (da scrivere nello stesso stile: scale interne non di emergenza → vittoria parziale).
Percorso corretto (fine debrief): 1) ispeziona il piano; 2) avvisa i colleghi della tua stanza; 3) controlla la seconda stanza; 4) evita l'ascensore, raggiungi le scale di emergenza seguendo il cartello entro il tempo.
