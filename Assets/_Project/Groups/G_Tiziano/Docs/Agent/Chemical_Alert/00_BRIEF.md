# CHEMICAL ALERT — BRIEF PER L'AGENTE
Fonte di verità. NON leggere PDF di consegna né GDD: tutto ciò che serve è qui.
[PROPOSTA] = non nel GDD, scelta di design da confermare col team.
[DA VALIDARE] = contenuto di sicurezza da validare col referente.

## Il tuo scope (leggi bene)
Devi SOLO: (1) montare la scena dello scenario, (2) implementare il gameplay/logica di Chemical Alert.
NON devi occuparti di: HUD/menu/schermate di esito/animazioni/modelli/texture/audio definitivi, git, Common Core, altri scenari.
Eccezione concordata: il pannello SDS statico, i popup errore, la vignetta tossica e il cue di tosse sono implementati in `ChemicalAlertPanelUI` perché servono alla verifica del gameplay; restano fuori HUD, menu, recap e schermata di vittoria.
Dove il gioco "mostrerebbe" qualcosa all'utente tu **emetti eventi C# e stato leggibile** + log in console `[CA]`: il resto della UI si collegherà dopo.
Ogni task termina con l'elenco dei **comandi di test** (vedi 01_CONVENTIONS).

## Progetto
Serious game VR "Sicurezza Immersiva 81" (Unity 6000.3, URP, OpenXR, Meta XR Core SDK, target Quest). Una build con 8 scenari di 4 gruppi; noi: Chemical Alert (poi Evacuation). Principio: OSSERVA → VALUTA → DECIDI → AGISCI → CONSEGUENZE → IMPARA. Le scelte devono modificare lo stato della missione; ogni errore ha una conseguenza osservabile.

## Scenario
Un barile è caduto dal camion nella zona di scarico e perde una sostanza chimica sconosciuta. Il giocatore deve contenere la perdita in tempo. Durata partita: **180 s** (Operational e Crisis); Guided: cronometro, nessuna sconfitta per tempo.

### Layout (scala 1:1)
- Centro: barile rovesciato + pozzanghera.
- Nord: porta garage; camion metà dentro/mità fuori, interno accessibile, contiene la SDS (scheda di sicurezza).
- A sinistra della porta garage: pulsante ventilazione industriale.
- Lati: scaffali con barili chiusi (solo prop).
- Sud: armadietto DPI e kit antisversamento affiancati alla parete.

### Procedura corretta [DA VALIDARE]
Le quattro macro-azioni (ventilazione, SDS, DPI, contenimento) possono essere affrontate in **qualsiasi ordine**. Non esiste un ordine obbligatorio; l'ordine consigliato genera solo un errore non bloccante (`chemical.error.procedure_order`).
1. Attivare la ventilazione.
2. Prendere la SDS dal camion e leggerla (pannello a schermo).
3. Acquisire i **tre DPI obbligatori** dall'armadietto: guanti chimici, maschera/respiratore, protezione occhi (occhiali a tenuta o visiera).
4. Kit antisversamento: posizionare l'**unico anello contenitivo** attorno alla pozzanghera, poi posare **tre fogli assorbenti** sopra la perdita.
5. Il successo si risolve automaticamente dopo l'assorbimento (4,5 s) e un delay di 2 s.

### Armadietto DPI
- **Due copie interagibili per tipo** (6 oggetti totali): `PPE_Gloves_A/B`, `PPE_Mask_A/B`, `PPE_EyeProtection_A/B`.
- Acquisire una copia la nasconde e disabilita l'interazione dell'altra copia dello stesso tipo.
- Nessun DPI decoy: tutte le copie sono corrette.

### Escalation ventilazione (tempi NASCOSTI, in ScriptableObject)
40 s senza ventilazione → evento tosse (+ SFX se assegnato) · +20 s → evento "fase tossica" progressiva (vignetta) · +20 s → sconfitta (Operational/Crisis).

### Errori
- Usare gli strumenti (anello, fogli) **senza guanti** → popup errore, 1 danno per tentativo; in Guided il danno è soppresso.
- Avvicinarsi alla perdita **senza maschera** (e senza ventilazione) → avviso + cue di tosse, nessun danno.
- Esposizione ad aria tossica **senza protezione occhi** → vignetta tossica + errore Safety non bloccante.
- Ordine non consigliato → errore Decision non bloccante; la partita continua.
- Errori non bloccanti portano a esito `Partial`, non bloccano la partita.

### Esiti
Successo: tutte le azioni corrette, perdita contenuta, bonifica completata e vivo · Parziale: sopravvive con procedure sbagliate (danni, perdita non contenuta, bonifica incompleta, errori d'ordine) · Fallimento: cuori a 0, tempo scaduto, ventilazione ignorata fino al limite.
Esito = evento `ResultReady(ScenarioResult)` con esito + ActionLog (azioni giuste/sbagliate) + sotto-punteggi. Retry = reset completo dello scenario.
**Non previsti**: schermata di vittoria, fase di uscita safe, smaltimento rifiuti.

## Difficoltà (codice: Guided / Operational / Crisis; GDD: Guidato / Realistico / Stress)
| | Guided | Operational | Crisis |
|---|---|---|---|
| Oggetti | evidenziati (emissione #E69F00 pulsante) | non evidenziati | non evidenziati |
| Aiuti | richieste di hint come eventi con chiave testo; stato pericolo up/mid/down come evento | nessuno | nessuno |
| Vite | nessun danno/morte | 3 cuori | 1 cuore |
| Disturbi | no | nessuno | luci/audio di disturbo |
| Tempo | cronometro (no sconfitta) | timer 3:00 | timer 3:00 |
| Punti GDD | — | 30 / parz. 10 / 0 | 50 / parz. 20 / 0 |
Tutto in `SO_ChemicalAlertConfig_<Livello>`; mai hardcoded.

## Valutazione: 5 dimensioni (Safety, Awareness, Decision, Execution, Time)
Etichetta ogni azione con una dimensione; calcola sotto-punteggi 0–100. Il Safety Performance Index finale lo calcolerà il Game System comune. Mappatura [PROPOSTA]: Safety = DPI prima del contatto, danni subiti · Awareness = SDS letta, ventilazione prima della tosse · Decision = DPI coerenti con la SDS, contenere invece di andarsene, ordine azioni · Execution = anello, copertura, bonifica · Time = tempo totale, assenza di azioni impulsive.

## Requisiti di design da rispettare
≥3 decision point [PROPOSTA]: ventilazione vs avvicinarsi al barile · quali DPI · contenere vs andarsene · ordine libero delle azioni. ≥3 interazioni (activate pulsante, grab+inspect SDS, grab DPI, grab+place kit). Rischio visibile (pozzanghera) e nascosto (aria viziata). Errore critico con conseguenza leggibile. Evento dinamico [PROPOSTA]: seconda perdita in istante casuale (finestra 20–40 s). Replay [PROPOSTA]: sostanza casuale tra 3 varianti (SDS e titolo diversi, stessi 3 DPI richiesti); armadietto sempre con 3 tipi × 2 copie. Stessa categoria di interazione = stesso comportamento ovunque.
Testi per l'utente (SDS, hint, recap): solo **chiavi stabili** (es. `chemical.sds.variantA.title`), mai stringhe italiane nel codice; il pannello SDS mostra il testo statico acido formico da `Docs/SDS/`.
