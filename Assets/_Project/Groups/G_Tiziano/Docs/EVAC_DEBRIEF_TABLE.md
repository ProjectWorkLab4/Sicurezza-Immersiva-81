# EVACUATION — Tabella frasi del debrief (LG01–LG11)

Tratta dall'allegato frasi del `EV_00_BRIEF.md`, allineata a `EvacActionIds` (`SI81.Tiziano.Evacuation.Core`).
`{t}` = timestamp dell'azione, passato come tempo di gioco a `ActionLog.Add(double timestamp, …)`.
Nessuna frase italiana è presente nel codice: solo le chiavi qui elencate.

| ID | Costante (`EvacActionIds`) | Quando | Dim. / esito | Frase modello (`{t}`) | Cosa potevi fare | Chiavi |
|---|---|---|---|---|---|---|
| LG01 | `Lg01PlanInspected` = `evac.lg01` | Prima ispezione di una delle 3 piante | Awareness / **Corretto** | "Al {t} hai consultato il piano di emergenza ed evacuazione. Conseguenza: hai individuato il percorso verso le scale." | — (nessun suggerimento previsto) | `evac.log.lg01.phrase` · `evac.log.lg01.hint` |
| LG02 | `Lg02NoPlan` = `evac.lg02` | Uscita senza aver ispezionato alcun piano | Awareness / **Mancato** | "Non hai controllato il piano. Conseguenza: orientamento meno sicuro. Cosa potevi fare: ispezionare le planimetrie all'entrata delle stanze o nel corridoio." | ispezionare le planimetrie all'entrata delle stanze o nel corridoio | `evac.log.lg02.phrase` · `evac.log.lg02.hint` |
| LG03 | `Lg03Room1Done` = `evac.lg03` | Tutti i 3 NPC dell'ufficio 1 raccolti | Execution / **Corretto** | "Al {t} hai avvisato i colleghi della tua stanza. Conseguenza: nessuno lasciato indietro nel primo ufficio." | — | `evac.log.lg03.phrase` · `evac.log.lg03.hint` |
| LG04 | `Lg04Room2Done` = `evac.lg04` | Tutti i 2 NPC dell'ufficio 2 raccolti | Execution / **Corretto** | "Al {t} hai controllato l'altro ufficio e fatto evacuare tutti. Conseguenza: piano sgomberato." | — | `evac.log.lg04.phrase` · `evac.log.lg04.hint` |
| LG05 | `Lg05StairsMissingNpcs` = `evac.lg05` | Scale di emergenza con NPC mancanti | Decision / **Rischioso** | "Al {t} hai raggiunto le scale senza controllare entrambi gli uffici. Conseguenza: colleghi rimasti indietro, vittoria parziale. Cosa potevi fare: controllare entrambe le stanze." | controllare entrambe le stanze | `evac.log.lg05.phrase` · `evac.log.lg05.hint` |
| LG06 | `Lg06Elevator` = `evac.lg06` | Pulsante ascensore premuto | Safety / **Errore critico** | "Al {t} hai usato l'ascensore durante l'allarme. Conseguenza: perdita automatica. Cosa potevi fare: non usarlo mai in emergenza, seguire il cartello verso le scale." | non usarlo mai in emergenza, seguire il cartello verso le scale | `evac.log.lg06.phrase` · `evac.log.lg06.hint` |
| LG07 | `Lg07FireContact` = `evac.lg07` | 1° o 2° contatto fuoco (Crisis) | Safety / **Rischioso** | "Al {t} ti sei avvicinato troppo alle fiamme. Conseguenza: scottatura. Cosa potevi fare: mantenere la distanza dal fuoco." | mantenere la distanza dal fuoco | `evac.log.lg07.phrase` · `evac.log.lg07.hint` |
| LG08 | `Lg08FireDeath` = `evac.lg08` | 3° contatto fuoco (morte) | Safety / **Errore critico** | "Al {t} sei entrato di nuovo in contatto con il fuoco. Conseguenza: perdita di una vita. Cosa potevi fare: aggirare i focolai." | aggirare i focolai | `evac.log.lg08.phrase` · `evac.log.lg08.hint` |
| LG09 | `Lg09StairsAllNpcs` = `evac.lg09` | Scale di emergenza con tutti i 5 NPC | Decision / **Corretto** | "Al {t} hai raggiunto le scale di emergenza con tutti i colleghi. Conseguenza: evacuazione completata." | — | `evac.log.lg09.phrase` · `evac.log.lg09.hint` |
| LG10 | `Lg10Timeout` = `evac.lg10` | Timer scaduto prima delle scale (non Guided) | Time / **Mancato** | "Al {t} il tempo è scaduto prima delle scale. Conseguenza: livello fallito." | — | `evac.log.lg10.phrase` · `evac.log.lg10.hint` |
| LG11 | `Lg11InternalStairs` = `evac.lg11` | Raggiunte le scale interne (sbagliate) | Decision / **Rischioso** | "Al {t} hai preso le scale interne invece di quelle di emergenza. Conseguenza: vittoria parziale. Cosa potevi fare: seguire il cartello illuminato verso le scale di emergenza." | seguire il cartello illuminato verso le scale di emergenza | `evac.log.lg11.phrase` · `evac.log.lg11.hint` |

## Note di coerenza

- **LG11** è l'unica frase scritta in questa task, nello stesso stile delle altre: la consegna richiedeva di scriverla "da scrivere nello stesso stile" nel BRIEF.
- `EvacActionIds.GetSeverity()` restituisce `Correct` per LG01/03/04/09 e `evac.replay.start`, `Missed` per LG02/LG10, `Critical` per LG06/LG08, `Risky` per LG05/LG07/LG11. Le etichette "Mancato/Errore critico/Rischioso" della tabella BRIEF corrispondono a questi valori.
- `PhraseKey(id)` produce `evac.log.lgNN.phrase` ed è il valore memorizzato come `ConsequenceKey` in `ActionLogEntry`; `HintKey(id)` produce `evac.log.lgNN.hint` per il testo "cosa potevi fare" della UI futura.
- `evac.replay.start` (seed + layout NPC/fuoco) è una entry **meta** a inizio run: non è un LG di debrief, non è punteggiata ed è presente nel `ScenarioResult` via `ActionLog`.
- LG10 non viene mai emesso in Guided (nessun timeout); LG07/LG08 solo in Crisis; LG06 in tutti i livelli, ma in Guided dà `Partial` + `DangerLevelChanged(Down)` invece di `Failure`.

## Percorso corretto (chiusura del debrief)

1. Ispeziona il piano; 2. avvisa i colleghi della tua stanza; 3. controlla la seconda stanza; 4. evita l'ascensore e raggiungi le scale di emergenza seguendo il cartello entro il tempo.
