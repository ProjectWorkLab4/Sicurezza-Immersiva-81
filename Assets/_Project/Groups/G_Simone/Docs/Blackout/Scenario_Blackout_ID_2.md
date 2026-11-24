# Scenario 1: Blackout (ID 2)

## Allineamento col GDD

- **Esplorazione libera:** il giocatore agisce nell'ordine che vuole mentre il timer scorre. Ogni azione ha un effetto indipendente. (nel figjam è più un albero decisionale)
- **Nessun passaggio automatico di livello.** Si vince varcando l'uscita d'emergenza.
- **Solo meccaniche del GDD:** movimento 6DoF (lineare/teleport), grab e drop, uso diretto degli strumenti, apertura di porte, digitazione di numeri sul telefono, cuori e vignettature.
- **Rilevatore di gas:** resta, ma come azione che fa solo perdere tempo. Con l'allarme attivo non serve misurare, si evacua.

## Breve descrizione

Il giocatore lavora alla sua postazione in ufficio quando va via la corrente e scatta l'allarme antincendio. Con le sole luci d'emergenza e la segnaletica luminosa deve raggiungere l'uscita d'emergenza prima che il fumo invada la stanza, senza perdere tempo né compiere azioni pericolose.

**Tempo disponibile livello realistico:** 3 minuti (proposta)

**Tempo disponibile livello stress:** 2 minuti (proposta)

## Oggetti presenti

| Oggetto | Interazione (meccanica GDD) |
| --- | --- |
| Cellulare sulla scrivania | Digitazione numeri |
| Rilevatore di gas portatile, con display | Grab, uso diretto, drop |
| Quadro elettrico | Apertura sportello?, leva azionabile? o solo interazione singola? |
| Porte tagliafuoco con maniglione antipanico | Apertura |
| Uscita d'emergenza | Apertura (fine livello) |
| Porta "normale" (uscita non di emergenza) | Apertura (fine livello) |
| Allarme antincendio (può anche solo essere vfx?) | Automatico |
| Luci d'emergenza e segnaletica luminosa di esodo | Automatico |
| Fuoco e fumo lungo il percorso sbagliato (si possono fare con il particle system, oppure modelli leggeri, il trigger per il danno è l'area non il modello in sé) | Automatico (danno al contatto) |
| Scrivanie, sedie, PC | Prop |

## Come si presenta

Il giocatore è alla sua postazione. Dopo qualche secondo le luci si spengono e parte l'allarme antincendio: un incendio in un'altra zona del piano ha causato il blackout. L'ufficio resta al buio, illuminato solo dalle luci d'emergenza e dalle frecce verdi della segnaletica.

Da definire: pianta dell'ufficio, posizione del quadro elettrico, delle due porte e del focolaio, percorso indicato dalla segnaletica.

## Meccaniche

- **Blackout:** dopo pochi secondi le luci si spengono.
- **Allarme antincendio** attivo per tutto il livello.
- **Porte tagliafuoco** che si chiudono, ma restano apribili con il maniglione antipanico.
- **Luci d'emergenza e segnaletica luminosa** che si accendono.
- **Fumo** che aumenta nel tempo. Se il giocatore resta nella stanza: dopo 40 s tosse, dopo altri 20 s vignettatura grigia, poi perde un cuore a intervalli regolari. Nel livello Guidato non ci sono cuori: compare il feedback "pollice giù".
- **Fuoco** sul percorso sbagliato: al contatto vignettatura rossa e perdita di un cuore.
- **Rilevatore:** quando è in mano, il display mostra un valore alto e il bip accelera.
- **Telefono:** chiamando il 112 parte una risposta registrata dell'operatore.

## Procedimento corretto

- Interrompere subito il lavoro, senza recuperare oggetti.
- Individuare la segnaletica luminosa di esodo.
- Seguirla, aprendo le porte tagliafuoco con il maniglione.
- Uscire dall'uscita d'emergenza: fine livello.

## Procedimento

| Azione | Tipo | Conseguenza |
| --- | --- | --- |
| Rimanere fermi alla postazione | Non corretta | Il fumo aumenta fino alla perdita di tutti i cuori: sconfitta |
| Chiamare il 112 | Neutra | L'operatore dice di uscire subito seguendo le vie di fuga; tempo perso, il livello continua |
| Aspettare i soccorsi dopo la chiamata | Non corretta | Come "rimanere fermi" |
| Usare il rilevatore di gas | Non corretta | Display con valore alto; nessun danno, ma tempo perso. Nel recap: con l'allarme attivo si evacua subito |
| Riaccendere il quadro elettrico | Non corretta | Scintilla: vignettatura rossa e perdita di un cuore. La luce non torna |
| Non seguire la segnaletica | Non corretta | Si arriva al fuoco: vignettatura rossa e perdita di un cuore al contatto |
| Uscire dalla porta "normale" | Non corretta | Successo parziale |
| Seguire la segnaletica e uscire dall'uscita d'emergenza | Corretta | Successo completo |

**Esiti (Lose/Win conditions del GDD):**

- **Successo completo:** esce dall'uscita d'emergenza senza aver compiuto azioni non corrette.
- **Successo parziale:** sopravvive ma esce dalla porta "normale" o ha compiuto azioni non corrette lungo il percorso.
- **Fallimento:** perde tutti i cuori o finisce il tempo prima di uscire.

## Feedback

- **Visivi:** buio; luci d'emergenza; frecce verdi della segnaletica; fumo; vignettatura grigia (soffocamento) e rossa (danno); scintilla del quadro elettrico; display del rilevatore.
- **Uditivi:** allarme antincendio; tosse; crepitio del fuoco; scarica elettrica; porte tagliafuoco che si chiudono.
- **Linguistici:** voce dell'operatore al telefono; suggerimenti nel livello Guidato; recap delle azioni giuste e sbagliate nelle schermate di esito.

## Differenze per livello di difficoltà

| Livello | Aiuti e regole | Punteggio (GDD) |
| --- | --- | --- |
| Guidato | Segnaletica, porte tagliafuoco e uscita d'emergenza evidenziate con contorno #E69F00. Suggerimento: "Segui le frecce verdi verso l'uscita". Feedback pollice su / intermedio / giù. Cronometro, nessun cuore | Non previsto |
| Realistico | Nessuna evidenziazione, 3 cuori, vignettatura rossa al danno, timer 3 min | Completo 30 - Parziale 10 - Fallimento 0 |
| Stress | Nessun aiuto, timer 2 min, fumo che sale più in fretta. Disturbi: fumo più denso? Luci d'emergenza che sfarfallano? | Completo 50 - Parziale 20 - Fallimento 0 |

## Punti da chiarire

- [ ] Disegnare la pianta dell'ufficio e il percorso d'esodo.
- [ ] Tarare i tempi: aumento del fumo, perdita dei cuori.
- [ ] Sovrapposizione con lo Scenario 6 (Evacuation): stesso ufficio, allarme e uscita d'emergenza. Differenziare puntando su buio e orientamento.
- [ ] Rilevatore di gas: tenerlo o toglierlo (in un ufficio è poco credibile).
- [ ] Verificare la procedura corretta effettiva.
