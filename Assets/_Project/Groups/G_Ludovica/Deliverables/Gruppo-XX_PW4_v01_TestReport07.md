# Gruppo-XX_PW4_v01_TestReport07 — Forklift Chaos

> Sostituire `XX` con il numero ufficiale del gruppo prima della consegna.
> **Stato: PIANO DI TEST — da compilare dopo i playtest reali.** Nessun risultato è stato inventato: le celle "Esito" e "Note" vanno riempite durante la sessione.

## 1. Ambiente

| Campo | Valore |
|---|---|
| Build / scena | `SCN_ForkliftChaos` (Editor Play standalone con SCN_Core additivo, oppure build `ARVR_PW4_SicurezzaImmersiva81_v01.zip`) |
| Dispositivo | Quest 3 / Quest 3S — da compilare: ____________________ |
| Meta XR Core SDK / OpenXR | 209.0.0 / 1.16.1 (vedi `ValentinoAGENTS.md`) |
| Livelli da provare | Guided, Operational, Crisis |
| Movimento | Editor: `WASD`/frecce, `Q`/`E` ruota, mouse+tasto destro, `Shift` corsa · VR: stick sinistro cammina, stick destro svolta a scatti |
| Comandi Editor | F1 spegni M1 · F2 prendi radio · F3 tasto 112 · F4 tasto CAPO · F5 prendi barriera · F6 posa barriera · F7 coni · `G` urtare pallet · F8 parla · F9 respiro · F10 ferita · F11 copri · F12 ferma M2 · F13 sposta Marco · F14 RCP · F15 DAE |
| Comandi VR | sguardo prolungato per osservare; sguardo + grilletto sugli oggetti etichettati. **Nessun pannello a scelta multipla.** |

## 2. Matrice dei playtest (5 richiesti, ≥2 esterni)

| # | Tipo | Partecipante | Livello | Scenario di prova | Cosa verificare | Esito (compilare) | Note |
|---|---|---|---|---|---|---|---|
| T1 | Interno | ________ | Guided | Percorso corretto completo | osservazione, M1 spento, barriera + coni, radio → 112 + capo turno, parla/respiro/ferita/copri, M2 fermato con barriera, soccorsi in tempo → **Successo**, SPI e 5 dimensioni | DA ESEGUIRE | |
| T2 | Interno | ________ | Operational | M2 non fermato + colleghi entrati | clacson 6 s prima, ombra, decisione M2 via radio, LG-18, soccorsi +60 s → **Parziale**, timeline debrief leggibile | DA ESEGUIRE | |
| T3 | Interno | ________ | Crisis | Errori critici | sposta Marco senza pericolo → Fallimento; riprova: corri in corsia → investimento; riprova: sosta in LZ-B → pallet su Marco | DA ESEGUIRE | |
| T4 | **Esterno** | ________ | Guided | Utente naïf, nessuna spiegazione | capisce cosa fare con soli avvisi/sottotitoli? tempo di reazione al clacson < 6 s? comfort (nessuna nausea)? | DA ESEGUIRE | |
| T5 | **Esterno** | ________ | Operational | Utente naïf, replay variato | secondo play con orario M2 e RK-01 diversi: riconosce l'angolo cieco? trova i pulsanti sulla radio e il kit di soccorso senza aiuto? | DA ESEGUIRE | |

## 3. Checklist di conformità (spuntare durante i test)

- [ ] EV-01 urlo: audio + testo + aptico entro 1 s dall'avvio.
- [ ] EV-02: clacson e sottotitolo almeno 10 s prima che M2 raggiunga l'accesso.
- [ ] EV-03: colleghi compaiono solo se BL-01 non è chiusa e il capo turno non è stato avvisato.
- [ ] EV-04 / EV-07: avviso 4 s prima della caduta; il pallet non cade su Marco se il giocatore si allontana.
- [ ] EV-08: corsa vicino a Marco produce penalità ma nessuno spostamento forzato della vista.
- [ ] EV-05: non coperto a 180 s produce avviso e penalità Execution.
- [ ] EV-06: sirena e fine livello; ritardo di 60 s se la corsia è bloccata.
- [ ] Apnea: mai in Guided; RCP/DAE disponibili come cubi etichettati accanto a Marco; timeout 120 s = Fallimento.
- [ ] Debrief: SPI, stelle, 5 dimensioni, righe con minuto/ID/esito/conseguenza, percorso corretto, nota 112.
- [ ] Niente Successo senza barriera BL-01.
- [ ] Tre livelli giocabili e rigiocabili; variabili di replay effettivamente diverse tra T4 e T5.
- [ ] Nessun errore in Console; nessun pannelloDecisioni (non più usato); HUD leggibile a 1,2 m.
- [ ] Locomozione: WASD/stick sinistro muovono il rig; Q/E o stick destro ruotano; non si attraversano i muri.
- [ ] Nessun quiz a comparsa: per osservare e agire servono sguardo e grilletto sugli oggetti.

## 4. Esiti attesi per configurazione (riferimento per il compilatore)

| Condizione | Esito atteso |
|---|---|
| barriera + M1 spento + accesso libero + Marco assistito + soccorsi in tempo + M2 fermato | Successo |
| Marco assistito ma corsia bloccata da colleghi/PA-02, oppure soccorsi > 4 min | Parziale |
| Marco spostato senza pericolo / investimento da M2 / pallet su Marco / apnea senza RCP / timeout | Fallimento |

## 5. Firma

| Ruolo | Nome | Data | Firma |
|---|---|---|---|
| Esecutore T1–T3 | ________________ | ___/11/2026 | ____________ |
| Esecutore T4 (esterno) | ________________ | ___/11/2026 | ____________ |
| Esecutore T5 (esterno) | ________________ | ___/11/2026 | ____________ |
| Validatore (ing. Marco Piatti) | ________________ | ___/11/2026 | ____________ |
