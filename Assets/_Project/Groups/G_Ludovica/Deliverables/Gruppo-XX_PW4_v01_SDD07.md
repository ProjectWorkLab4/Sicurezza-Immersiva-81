# Gruppo-XX_PW4_v01_SDD07 — Scenario Design Document 07 · Forklift Chaos

> Sostituire `XX` con il numero ufficiale del gruppo prima della consegna.
> Copiare questo file nella cartella comune `COMMON_BUILD` insieme agli altri output.
> Scenario 07, complessità 3/5. Ultimo aggiornamento: 24 novembre 2026.

## 1. Mission brief (scheda cliente)

**Ruolo:** operatore di magazzino. Un carrello elevatore (Muletto 1) percorre la corsia 3 con un carico instabile; Marco è a terra e sanguina. Obiettivo: mettere in sicurezza la corsia, allertare il 112 e il capo turno e soccorrere Marco senza agire d'impulso.

**Criteri di completamento:** Muletto 1 spento · corsia delimitata (BL-01 + coni) · accesso libero · Marco assistito secondo le istruzioni del 112 fino all'arrivo dei soccorsi.

## 2. Mappa e oggetti

Coordinate in metri, asse Z del mondo = asse Y della mappa SVG.

| ID | Posizione (x, z) | Riferimento in scena |
|---|---|---|
| ZN-02 Corsia 3 | (30, 17), 4 × 18 | `ZN02_Corsia3` |
| ZN-03 Scaffale alto / angolo cieco | (32, 8), 1,4 × 18 | `MDL_Scaffale_ZN03` (prefab) |
| ZN-04 Accesso unico | (30, 8) | `ZN04_AccessoUnico` |
| P0 Partenza | (30, 10), guarda a nord | `MNG_SpawnPoint`, `MARKER_Partenza_P0` |
| MC-01 Marco | (29,5; 19,7) | `PLACEHOLDER_Marco_MC01` |
| VH-01 Muletto 1 | (29, 15) | `MDL_Muletto1_VH01` |
| OP-01 Operatore sotto shock | (28,4; 16,8) | `PLACEHOLDER_Operatore_OP01` |
| PA-01 Pallet instabile | (27, 22,5) | `MDL_Pallet_PA01` (prefab) |
| LZ-B Zona caduta PA-01 | (27,5; 19,5) | `ZONE_LZB_CadutaPA01` |
| RK-01 Rack (radio, barriera, coni) | (28, 9,5) | `PLACEHOLDER_Rack_RK01`, `MDL_Radio_RK01`, `MDL_Barriera_Stack_RK01` |
| PA-02 Secondo pallet | (31, 13) | `MDL_Pallet_PA02` (prefab) |
| LZ-C Zona caduta PA-02 | (30,4; 12) | `ZONE_LZC_CadutaPA02` |
| SL-01 Pavimento scivoloso | (30,4; 20), r 1,5 | `SL01_PavimentoScivoloso` |
| BL-01 Linea barriera | (28–32, 9) | `BL01_LineaBarriera`, `MDL_Barriera_BL01` |
| MS-01 Spawn Muletto 2 | (36, 5) | `MDL_Muletto2_MS01` |
| CC-S1 Spawn colleghi | (21, 5) | `GRP_Colleghi_CCS1` (2 NPC) |

Rotta Muletto 2: (36,5) → (30,5) → (30,9) → (31,1; 10,5) → (31,1; 18,4). Waypoint `WP_M2_1…5`.

## 3. Cinque momenti decisivi (senza pannelli a scelta multipla)

I cinque decision point **non sono quiz**: il giocatore li risolve camminando, guardando e usando oggetti nella scena. Non esiste più `AskDecision` in questo scenario.

| DP | Azione fisica richiesta | Errore possibile | Conseguenza |
|---|---|---|---|
| DP1 | Avvicinarsi e **guardare** Marco, M1, PA-01 e l'angolo cieco (dwell 1,2 s su ciascuno) | Guardare il pallet e urtarlo (`G`) | LG-02 / LG-03 / LG-19 |
| DP2 | Spegnere M1 (`F1`) e posare la barriera presa dal rack (`F5`) sulla linea BL-01 (`F6`), poi i coni (`F7`) | Lasciare M1 acceso / spostare Marco | LG-05, niente Successo / LG-11 |
| DP3 | Prendere la radio (`F2`) e premere i **due tasti fisici** sulla radio: `112` (`F3`) e `CAPO` (`F4`) | Solo capo turno o nessuna chiamata | LG-07, soccorsi non programmati |
| DP4 | Usare gli oggetti del kit accanto a Marco: parla (`F8`), respiro (`F9`), ferita (`F10`), copri (`F11`), RCP (`F14`), DAE (`F15`) | Spostamento senza pericolo (`F13`); apnea senza RCP entro 120 s | LG-11 / LG-16 |
| DP5 | Con il clacson, posare la barriera BL-01 o premere il tasto `CAPO` sulla radio; fermare M2 puntandolo (`F12`) | Correre in corsia / non fermare il mezzo | LG-10 (investimento) |

## 4. Interazioni (≥3 richieste)

| Gruppo | Azioni | VR | Editor |
|---|---|---|---|
| osserva / ispeziona | Marco, M1, pallet, angolo cieco | sguardo prolungato (dwell) | sguardo prolungato |
| grab / use | radio + tasti 112/capo, barriera dal rack → posa su BL-01, coni, comando M1 | sguardo + grilletto sugli oggetti etichettati | F1–F7 |
| comunica | tasto `112`, tasto `CAPO`, parla con Marco | sguardo + grilletto sui pulsanti fisici | F3, F4, F8 |
| soccorso | respiro, compressione, copertura, spostamento d'emergenza, RCP, DAE | sette cubi etichettati accanto a Marco | F9–F15 |

**Locomozione:** script `ForkliftChaosLocomotion` (`MNG_Locomotion`) sposta il `TrackingOrigin` del CoreRig. In Editor: `WASD`/frecce, `Q`/`E` per ruotare, mouse con tasto destro, `Shift` per correre. In VR: stick sinistro = cammina, stick destro = svolta a scatti da 30°. Nessun asset condiviso è stato modificato.

Nota VR: ogni azione ha un oggetto proprio con collider ed etichetta; non ci sono più pannelli a comparsa. Un solo grilletto per frame impedisce attivazioni doppie. La radio e la barriera, una volta prese, restano agganciate alla testa del giocatore.

## 5. Rischi ed eventi

**Visibili:** M1 acceso, PA-01 in bilico, sangue e SL-01, operatore sotto shock.
**Nascosti:** M2 dall'angolo cieco (clacson + cicalino + sottotitolo + aptico + ombra nei livelli bassi), PA-02, colleghi curiosi (voci e passi).

| EV | Innesco | Avvisi | Contromisura | Conseguenza |
|---|---|---|---|---|
| EV-01 Urlo | t = 0 | audio + "Aiuto!" + aptico | osservare | timer avviato |
| EV-02 Muletto 2 | 30–90 s dopo la chiamata; 150 s senza chiamata | clacson, sottotitolo, aptico, ombra | barriera o radio | investimento = Fallimento |
| EV-03 Colleghi | 60–90 s dall'urlo se BL-01 non è chiusa | voci, passi, sottotitolo | barriera o capo turno | entrati = niente Successo, soccorsi +60 s |
| EV-04 PA-01 | urto o sosta in LZ-B oltre 5 s | scricchiolio, sottotitolo, oscillazione, 4 s di preavviso | allontanarsi | su Marco = Fallimento; di lato = avviso |
| EV-07 PA-02 | sosta in LZ-C oltre 5 s | scricchiolio, inclinazione, 4 s di preavviso | camminare e uscire | corsia ostruita = +60 s; giocatore sotto = Fallimento |
| EV-08 SL-01 | locomozione veloce entro 1,5 m da Marco | suono, sottotitolo, aptico | camminare | penalità Safety, nessuno spostamento forzato |
| EV-05 Marco peggiora | spostamento indebito o non coperto a 180 s | respiro, sangue, sottotitolo | passi corretti | spostato = Fallimento; tempo = penalità Execution |
| EV-06 Soccorsi | t_call + 120–210 s (+60 s se corsia bloccata) | sirena, sottotitolo | corsia libera | fine livello |

**Errori critici:** spostare Marco senza pericolo immediato; correre in corsia senza osservare/delimitare; urtare o sostare sotto la pila facendo cadere un pallet su Marco; M2 non fermato; apnea senza RCP entro 120 s.

## 6. Esiti

- **Successo:** BL-01 chiusa (regola cliente: niente Successo senza barriera), M1 spento, accesso libero, passi su Marco completati, soccorsi non ritardati, M2 fermato o mai partito.
- **Parziale:** Marco soccorso correttamente ma corsia bloccata (colleghi o PA-02), soccorsi oltre 4 minuti o un passo mancante senza danni.
- **Fallimento:** errore critico, timeout, investimento, nuovo ferimento da pallet, apnea senza RCP.

## 7. Difficoltà

| | Guided | Operational | Crisis |
|---|---|---|---|
| Sagoma su Marco | sì (LabelAnchor) | no | no |
| Suggerimenti | dopo 20 s di inattività | max 3, dopo 45 s | nessuno |
| Preavviso M2 | ombra + lampeggiante + freccia | ombra + lampeggiante | solo clacson e sottotitolo |
| Timer | sì | sì | sì |
| Apnea | mai | 1 partita su 3 | 1 partita su 2 |
| Arrivo M2 dopo chiamata | 60–120 s | 30–90 s | 30–90 s |
| Distrattori | nessuno | capo turno interrompe | più voci e colleghi che chiamano |

## 8. Replay

Asset `SO_Replay_ForkliftChaos.asset`: seed + contatore tentativi (PlayerPrefs `FKC_Attempt`). Variano orario di M2, posizione di PA-02, numero di colleghi (1–3), stato del respiro di Marco (regolare / rumoroso solo sonoro / apnea con RCP-DAE) e posizione di RK-01 (ovest o est). Il replay vale per i tre livelli e per esiti parziali/falliti.

## 9. Valutazione e debrief

Cinque dimensioni 0–100 (Safety, Awareness, Decision, Execution, Time); SPI = media semplice; stelle da `ScoreRules` comune. Il registro delle scelte usa gli ID LG-01…LG-21 con esito (Corretto, Rischioso, Errore critico, Mancato), minuto e conseguenza. Debrief: righe ordinate per gravità e tempo, prefissi `+ ` / `- ` / `! `, percorso corretto finale e nota sul 112 italiano.

**Integrazione Common Core:** timer, HUD, sottotitoli, icone, debrief, score, salvataggio, grab/use/snap, controller e comfort NON sono duplicati: lo scenario estende `SI81.UI.ScenarioController` e chiama `HUDService`, `AskDecision`, `EndMission`. Componenti da altri gruppi integrati: UX/UI (HUD e debrief) e Game System (Score Manager, 5 dimensioni e rank).

**Fallback dichiarato temporaneo:** il registro timeline e la tabella frasi sono implementati dentro `ForkliftChaosScenario.BuildDebrief()` in attesa del registro comune (brief §9, fallback entro il 25 novembre). Se il registro comune diventa disponibile, spostare la tabella frasi in un asset dati e rimuovere il fallback.

## 10. File implementativi

- `Assets/_Project/Groups/G_Ludovica/Assets/Scenes/SCN_ForkliftChaos.unity`
- `Assets/_Project/Groups/G_Ludovica/Assets/Code/ForkliftChaosScenario.cs`
- `Assets/_Project/Groups/G_Ludovica/Assets/Code/ForkliftChaosInteractable.cs`
- `Assets/_Project/Groups/G_Ludovica/Assets/Code/ForkliftChaosReplayConfig.cs`
- `Assets/_Project/Groups/G_Ludovica/Assets/Level/SO_Replay_ForkliftChaos.asset`
- `Assets/_Project/Groups/G_Ludovica/Assets/Level/Prefabs/MDL_Magazzino_*.prefab` (6 prefab)

## 11. Punti da validare col cliente (ing. Marco Piatti)

1. Spostamento di Marco solo in pericolo immediato, max 3 m, testa sostenuta: formulazione da confermare con un addetto al primo soccorso.
2. Respiro "rumoroso" = solo sonoro e regolare; respiro agonico = da trattare come assente.
3. RCP 30:2 (addestrati) vs sole compressioni guidate dal 112 (non addestrati): quale variante nel debrief.
4. Numero 112 corretto in Italia, ma organizzazione regionale variabile (nota già nel debrief).
5. Radio portatile al posto della postazione fissa: proposta da approvare.
6. Scale dei prefab importati: approvate a occhio dal cliente, da ricalibrare in visore se necessario.
