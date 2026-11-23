# CHEMICAL ALERT — BRIEF PER L'AGENTE
Fonte di verità. NON leggere PDF di consegna né GDD: tutto ciò che serve è qui.
[PROPOSTA] = non nel GDD, scelta di design da confermare col team.
[DA VALIDARE] = contenuto di sicurezza da validare col referente.

## Il tuo scope (leggi bene)
Devi SOLO: (1) montare la scena dello scenario, (2) implementare il gameplay/logica di Chemical Alert.
NON devi occuparti di: UI/HUD/menu/pannelli/sottotitoli/schermate di esito (le farà un altro team, dopo), animazioni (le fa il modellatore), modelli/texture/audio definitivi, git, Common Core, altri scenari.
Dove il gioco "mostrerebbe" qualcosa all'utente (cuori, timer, hint, esito, vignette, sottotitoli) tu **emetti eventi C# e stato leggibile** + log in console `[CA]`: la UI si collegherà dopo.
Ogni task termina con l'elenco dei **comandi di test** (vedi 01_CONVENTIONS).

## Progetto
Serious game VR "Sicurezza Immersiva 81" (Unity 6000.3, URP, OpenXR, Meta XR Core SDK, target Quest). Una build con 8 scenari di 4 gruppi; noi: Chemical Alert (poi Evacuation). Principio: OSSERVA → VALUTA → DECIDI → AGISCI → CONSEGUENZE → IMPARA. Le scelte devono modificare lo stato della missione; ogni errore ha una conseguenza osservabile.

## Scenario
Un barile è caduto dal camion nella zona di scarico e perde una sostanza chimica sconosciuta. Il giocatore deve contenere la perdita in tempo. Tempo: 3:00 (Operational e Crisis); Guided: cronometro, nessuna sconfitta per tempo.

### Layout (scala 1:1)
- Centro: barile rovesciato + pozzanghera.
- Nord: porta garage; camion metà dentro/metà fuori, interno accessibile, contiene la SDS (scheda di sicurezza).
- A sinistra della porta garage: pulsante ventilazione industriale.
- Lati: scaffali con barili chiusi (solo prop).
- Sud: armadietto DPI e kit antisversamento affiancati alla parete.
- [PROPOSTA] porta di servizio laterale = zona sicura per allontanarsi.

### Procedura corretta [DA VALIDARE]
1. Attivare la ventilazione.
2. Prendere la SDS dal camion e leggerla.
3. Scegliere ed equipaggiare i DPI corretti dall'armadietto (ci sono DPI errati/decoy).
4. Kit antisversamento: "salsicciotti" attorno alla pozzanghera, poi fogli assorbenti sopra finché sparisce.
5. Raccogliere i fogli usati (con DPI) e buttarli nel sacchetto rifiuti pericolosi.

### Escalation ventilazione (tempi NASCOSTI, in ScriptableObject)
40 s senza ventilazione → evento tosse (+ SFX se assegnato) · +20 s → evento "fase tossica" progressiva (la UI/VFX ci farà la vignetta verde) · +20 s → sconfitta (Operational/Crisis).

### Errori
- Toccare la sostanza senza DPI o con DPI errati → evento scottatura (durata 5 s), 1 danno, SFX se assegnato.
- Ventilazione ignorata → tosse → fase tossica → perdita.
- Allontanarsi senza contenere → esito PARZIALE.

### Esiti
Successo: tutti i passi corretti e vivo · Parziale: sopravvive con procedure sbagliate (danni, perdita non contenuta, rifiuti non smaltiti…) · Fallimento: cuori a 0, tempo scaduto, ventilazione ignorata fino al limite.
Esito = evento `ResultReady(ScenarioResult)` con esito + ActionLog (azioni giuste/sbagliate) + sotto-punteggi. Retry = reset completo dello scenario.

## Difficoltà (codice: Guided / Operational / Crisis; GDD: Guidato / Realistico / Stress)
| | Guided | Operational | Crisis |
|---|---|---|---|
| Oggetti | evidenziati (emissione #E69F00 pulsante) | non evidenziati | non evidenziati |
| Aiuti | richieste di hint come eventi con chiave testo; stato pericolo up/mid/down come evento | nessuno | nessuno |
| Vite | nessun danno/morte | 3 cuori | 3 cuori |
| Disturbi | no | distrattori [PROPOSTA] | luci/audio di disturbo |
| Tempo | cronometro (no sconfitta) | timer 3:00 | timer 3:00 |
| Punti GDD | — | 30 / parz. 10 / 0 | 50 / parz. 20 / 0 |
Tutto in `SO_ChemicalAlertConfig_<Livello>`; mai hardcoded.

## Valutazione: 5 dimensioni (Safety, Awareness, Decision, Execution, Time)
Etichetta ogni azione con una dimensione; calcola sotto-punteggi 0–100. Il Safety Performance Index finale lo calcolerà il Game System comune. Mappatura [PROPOSTA]: Safety = DPI prima del contatto, danni subiti · Awareness = SDS letta, ventilazione prima della tosse · Decision = DPI coerenti con la SDS, contenere invece di andarsene, ordine azioni · Execution = perimetro, copertura, smaltimento · Time = tempo totale, assenza di azioni impulsive.

## Requisiti di design da rispettare
≥3 decision point [PROPOSTA]: ventilazione vs avvicinarsi al barile · quali DPI (letta la SDS) · contenere vs andarsene · ordine perimetro→fogli. ≥3 interazioni (activate pulsante, grab+inspect SDS, grab+equip DPI, grab+place kit). Rischio visibile (pozzanghera) e nascosto (aria viziata). Errore critico con conseguenza leggibile. Evento dinamico [PROPOSTA]: seconda perdita in istante casuale. Replay [PROPOSTA]: sostanza casuale tra 3 varianti (SDS e set DPI diversi), armadietto rimescolato. Stessa categoria di interazione = stesso comportamento ovunque.
Testi per l'utente (SDS, hint, recap): solo **chiavi stabili** (es. `chemical.sds.variantA.title`), mai stringhe italiane nel codice.
