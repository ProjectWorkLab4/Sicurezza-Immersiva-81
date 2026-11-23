# TASK — Chemical Alert (una sessione nuova per task)

## Prompt da incollare a inizio sessione
> Leggi Assets/_Project/Groups/G_Tiziano/Docs/Agent/00_BRIEF.md e 01_CONVENTIONS.md e, in HANDOFF.md (stessa cartella), SOLO "Stato corrente" e le ultime 2 entry del log. Poi esegui SOLO la task **Txx** (leggi solo la sua sezione in TASKS.md). NON usare git in nessun modo. NON scrivere fuori da Assets/_Project/Groups/G_Tiziano/. NON creare UI né animazioni. Quando i criteri "Done quando" sono soddisfatti, aggiorna HANDOFF.md, rispondimi con l'elenco dei comandi di test e fermati.

## Prompt di emergenza (contesto quasi saturo)
> Sei vicino al limite di contesto. Aggiorna subito HANDOFF.md con lo stato REALE (fatto, mancante, errori aperti, comandi di test attivi) e fermati. Niente git.

## Mappa
- **PIANO v2 (si salta la scena di test).** Fatte: T00, T01 (ridotta), T02 (script scritti, NON verificati nel Simulator: si verificano nello scenario reale). Poi PULIZIA (prompt a parte). **Ordine da qui: T05 (ambiente con i modelli esistenti) → T04 → T06 → T07 → T08 → T09 → T10 → T11 → T12 → T13 → T14.** T03 annullata.
- **Vertical slice giocabile (L2 end-to-end): fino a T14.** Priorità massima.
- Poi: T15 (livelli), T16 (replay + evento dinamico).
- T17 solo quando il modellatore consegna gli asset in Shared. T18 documentazione. T19 controllo prestazioni.
- Dopo T07 un altro dev può iniziare Evacuation riusando `Code/Scripts/Core`.

---

## T00 — Ricognizione (solo lettura)
**Dipende da:** —
**Fai:** rileva versione Unity, impostazione Active Input Handling, build target, pipeline attiva (Mobile/PC renderer), prefab `OVRCameraRig` nel package Meta XR Core SDK (percorso), contenuto di `Shared/`, capacità del tuo MCP (leggere console, entrare in Play Mode, creare scene/prefab, assegnare riferimenti). Cerca un budget prestazionale documentato.
**Output:** `Docs/Agent/ENV_REPORT.md` (≤40 righe, solo fatti).
**Fuori scope:** modificare qualsiasi asset.
**Done quando:** ENV_REPORT creato, nessun asset modificato; HANDOFF compilato.

## T01 — Scena, rig e struttura
**Dipende da:** T00
**Fai:** crea le sottocartelle di `Code/Scripts` (Core, Interaction, Systems, Data, Debug), la scena `Level/Scenes/SCN_ChemicalAlert.unity` con un'istanza di `OVRCameraRig` (tracking a livello pavimento), luce direzionale base, un pavimento e un cubo di test. Nessun script ancora, salvo un `Hello` di verifica da rimuovere.
**Fuori scope:** gameplay.
**Done quando:** la scena si apre e va in Play senza errori; il rig è un'istanza (il prefab nel package non è modificato). L'utente verifica nel Meta XR Simulator che si vede la scena e si muovono testa e controller.

## T02 — Interazione provvisoria (grab/activate/inspect)
**Dipende da:** T01
**Fai:** `ScenarioInteractable` (eventi Grabbed, Released, Activated, Inspected, HoverEnter/Exit; flag `grabbable`, `activatable`, `inspectable`) e `ScenarioHandInteractor` su ciascuna ancora controller del rig: overlap sfera per l'hover, **grip = afferra/rilascia** (parenting o joint semplice, rilascio con velocità), **trigger = attiva** l'oggetto in hover. Controller di riferimento: Meta Quest 3 (Touch Plus: trigger indice, grip laterale, thumbstick, A/B destro, X/Y sinistro). **Tutte le assegnazioni dei pulsanti stanno in UN SOLO file `ScenarioInputBindings`** (azione logica → `OVRInput` button/controller); nessun altro script legge `OVRInput` direttamente. Le assegnazioni sono PROVVISORIE: verranno decise dai dev e sostituite cambiando solo quel file. Locomozione NON inclusa (si usa il teletrasporto di debug di T03). `Inspected` = oggetto tenuto in mano e avvicinato alla testa (<0,35 m e guardato). Usa `OVRInput`. Cubo di test con log eventi.
**Fuori scope:** logica di scenario, highlight, UI.
**Done quando:** compila; il cubo si afferra, rilascia, attiva e ispeziona nel Simulator (l'utente) e gli eventi compaiono in console. Comandi di test indicati.

## T03 — ANNULLATA (il registro `DebugCommands` minimo si crea dentro T07; non fare questa task)
## (vecchio testo, ignorare) Sistema di comandi di test
**Dipende da:** T01
**Fai:** `DebugCommands` (MonoBehaviour solo Editor/Development): registro `Register(key, description, action)`, tasti via nuovo Input System, `ContextMenu` equivalenti, tasto `H` stampa l'elenco in console. Comandi base: teletrasporto del rig a marker, reset scena (placeholder), stampa stato. Gli altri task registrano i propri comandi qui. Nessuna UI su schermo (solo console).
**Done quando:** `H` elenca i comandi; il teletrasporto funziona; HANDOFF riporta come aggiungere un comando.

## T04 — Dati (ScriptableObject)
**Dipende da:** T01
**Fai:** in `Code/Scripts/Data` + istanze in `Code/Scripts/Data/Assets` (o cartella sensata sotto GROUP/): `SO_ChemicalAlertConfig_<Livello>` ×3 (durata totale, soglie ventilazione 40/20/20 s, jitter, vite 0/3/3, durata dolore 5 s, flag aiuti/disturbi/sconfitta per tempo, punti 30/10/0 e 50/20/0); `PPEItem` SO (id, chiave testo, slot Head/Torso/Hands, isDecoy); `SubstanceVariant` SO (id, chiavi testo SDS, set DPI richiesti, growthRate, maxRadius, colore). 3 varianti segnaposto e 8–10 PPE plausibili (guanti chimici, guanti da lavoro [decoy], occhiali a tenuta, occhiali semplici [decoy], grembiule chimico, respiratore, mascherina semplice [decoy]…). `OnValidate` con avvisi.
**Fuori scope:** testi finali (chiavi e contenuti [DA VALIDARE]), logica runtime.
**Done quando:** asset creati, nessun avviso; comando di test che stampa i 3 config.

## T05 — Ambiente provvisorio con i modelli disponibili e marker (PROSSIMA TASK)
**Dipende da:** T01 (ridotta) + pulizia
**Modelli disponibili (l'utente compila prima di lanciare la task):** `<elenco nome → percorso in Shared/ dei modelli già pronti: barile, camion, armadietto, scaffali, kit, ecc.>`
**Prima di tutto:** per ogni modello elencato **duplica** da `Shared/` a `GROUP/Art/Models/` (mai modificare l'originale) e usalo nel figlio `Visual`. Per ciò che manca usa primitive/graybox. Sostituzioni future = solo `Visual`.
**Fai:** nella scena: stanza ~12×10 m h 5 m; porta garage a nord ~4×4 m; camion graybox con interno praticabile; armadietto; piano per kit; punto del pulsante ventilazione a sinistra della porta; scaffali con barili prop; barile rovesciato al centro; porta di servizio laterale. Ogni oggetto = root (logica/collider) + figlio `Visual`. Marker (empty): `SPAWN_Player`, `POS_Barrel`, `POS_Puddle`, `POS_VentButton`, `POS_Truck`, `POS_SDS`, `POS_PPECabinet`, `POS_SpillKit`, `POS_WasteBag`, `ZONE_ExitSafe` (trigger), `ZONE_SpillArea` (trigger). Oggetti statici marcati static, materiali semplici in `Art/Materials/`.
**Fuori scope:** asset definitivi, animazioni.
**Done quando:** nel Simulator si cammina a scala 1:1 senza compenetrazioni; tutti i marker esistono con questi nomi; comando di test che teletrasporta a ogni marker.

## T06 — ActionLog e OutcomeEvaluator (logica pura)
**Dipende da:** T01
**Fai:** classi C# semplici in `Core`: enum `Dimension`, `Outcome`; `ActionLog` (timestamp, actionId, isCorrect, dimension, consequenceKey); `ScenarioResult` (outcome, sotto-punteggi per dimensione, log); `OutcomeEvaluator` con le regole del brief. Niente asmdef/NUnit: aggiungi un `SelfCheck` richiamabile da comando di test che esegue ≥8 casi (successo, parziale per perdita non contenuta, parziale per danni, fail per cuori, per tempo, per ventilazione, ecc.) e stampa PASS/FAIL.
**Done quando:** il comando mostra tutti PASS.

## T07 — ChemicalAlertDirector (fasi, timer, reset, API verso l'esterno)
**Dipende da:** T04, T06
**Fai:** `ChemicalAlertDirector` (prefab `MNG_ChemicalAlertDirector`): fasi Briefing → Scan → Action → Resolution → ResultHandoff, timer in pausa, config scelto per difficoltà. Espone **stato leggibile** (`Phase`, `TimeRemaining`, `Hearts`, `Difficulty`, `Outcome`, `ActionLog`) ed **eventi** (`PhaseChanged`, `TimeUp`, `ResultReady`) — è l'unico confine verso la UI futura. Registro `IResettable` + `ResetScenario()`. Comandi di test: avanza fase, cambia difficoltà, forza fine tempo, reset, stampa stato. **Crea qui anche il registro `DebugCommands` minimo** (tasti + ContextMenu, solo Editor/Development, output in console, tasto `H` = elenco; teletrasporto del rig ai marker) e comandi `SimulateGrab/Release/Activate/Inspect <oggetto>` che richiamano gli stessi eventi di `ScenarioInteractable`: così la logica si verifica senza Simulator. Gli altri task aggiungono i propri comandi.
**Fuori scope:** briefing a schermo, qualsiasi UI.
**Done quando:** fasi e timer funzionano da comando; 3 reset consecutivi riportano allo stato iniziale senza sottoscrizioni duplicate.

## T08 — Ventilazione ed escalation nascosta
**Dipende da:** T02, T05, T07
**Fai:** `VentTerminal` (root con `ScenarioInteractable`, figlio `Visual`, `UnityEvent OnTurnedOn`), `VentilationSystem` (Off/On, audio loop opzionale), `VentilationEscalation` dall'inizio fase Action: evento tosse a 40 s, `VentilationStageChanged(progress 0–1)` a +20 s, perdita a +20 s (valori da config con jitter). Attivata → ferma l'escalation, log (tempo, prima/dopo la tosse). `IResettable`. Comandi di test: attiva ventilazione, accelera il tempo ×N, stampa stadio.
**Done quando:** ignorando, tosse→fase tossica→perdita nei tempi (con soglie ridotte); attivando si ferma; Reset ok. Test Simulator: premere il pulsante col trigger.

## T09 — Camion e SDS
**Dipende da:** T02, T05, T07
**Fai:** `SdsItem` afferrabile e ispezionabile a `POS_SDS` dentro il camion; leggerla = tenerla in mano e portarla alla testa (`Inspected`) → evento `SdsRead` e log (Awareness). Espone la `SubstanceVariant` corrente (chiavi testo). Nessun pannello di lettura: lo farà la UI.
**Done quando:** nel Simulator si afferra e ispeziona, l'evento compare in console; Reset la rimette a posto.

## T10 — Armadietto DPI ed equipaggiamento
**Dipende da:** T04, T05, T07
**Fai:** `PPECabinet` (stato Open/Closed istantaneo via interazione, `UnityEvent OnOpened/OnClosed`), DPI afferrabili (corretti + decoy). `PPEEquipZone` sul rig (Head, Torso, Hands): rilasciare un DPI vicino alla zona dello slot lo equipaggia (l'oggetto si aggancia). `PlayerPPEState.HasRequiredSet(variant)`. Si può togliere. Equipaggiare un DPI sbagliato è permesso (conseguenza al contatto, T11). Log Decision/Safety. Comando di test: equipaggia set corretto/errato, stampa stato DPI.
**Done quando:** equip/unequip e `HasRequiredSet` corretti per le 3 varianti; Reset ok.

## T11 — Pozzanghera, contatto e salute
**Dipende da:** T07, T10
**Fai:** `Puddle` (mesh piana con scala controllata, crescita da `growthRate` a `maxRadius` finché non contenuta), `SubstanceContactSensor` (mani/corpo del giocatore che entrano nella sostanza senza set DPI completo → evento `DamageTaken(kind: burn, 5 s)`, SFX opzionale, 1 danno, finestra di invulnerabilità). `PlayerHealth`: cuori da config; in Guided nessun danno ma evento di pericolo. Cuori 0 → evento `Died`. Log Safety. Comandi: forza contatto, stampa cuori.
**Fuori scope:** vignetta rossa, indicatori cuori, haptics UI (la UI/feedback li collegherà agli eventi).
**Done quando:** contatto con/senza DPI giusti/errati si comporta come da brief; 3 danni → `Died`; Reset ripristina.

## T12 — Kit antisversamento: salsicciotti e perimetro
**Dipende da:** T11
**Fai:** `SpillKit` (cassetta, N salsicciotti afferrabili), `PerimeterSlots` (anello di slot attorno alla pozzanghera a raggio > maxRadius, aggancio per prossimità); a perimetro completo la crescita si ferma (`Contained = true`). Usare il kit prima di SDS/DPI è un errore d'ordine loggato (Decision), non bloccante. Comandi: riempi perimetro, stampa stato.
**Done quando:** perimetro completo ferma la crescita, parziale no; Reset ripristina.

## T13 — Fogli assorbenti, copertura e smaltimento
**Dipende da:** T12
**Fai:** fogli posabili sulla pozzanghera; contatore di copertura (fogli necessari proporzionali alla dimensione corrente) fa sparire la pozzanghera; i fogli posati diventano `WasteItem` (toccarli senza DPI = scottatura); `WasteBag` con trigger di smaltimento; stato `CleanupComplete`. Log Execution. Fogli prima del perimetro: permesso ma pozzanghera più grande, loggato.
**Done quando:** sequenza perimetro→fogli→smaltimento porta a `CleanupComplete`; Reset ok.

## T14 — Esiti, uscita e retry (vertical slice)
**Dipende da:** T08, T11, T13
**Fai:** `ZONE_ExitSafe`: uscire con perdita non contenuta → Partial. Cuori 0, tempo scaduto, perdita da ventilazione → Fail. `CleanupComplete` → Success/Partial via `OutcomeEvaluator`. Emissione `ResultReady(ScenarioResult)` dal Director + stampa in console di esito e recap ActionLog. Retry = comando/metodo `ResetScenario()`. Nessun pannello.
**Done quando:** 3 partite in Play: Success, Partial (uscita senza contenere), Fail (timeout) con esito/recap corretti in console e retry pulito.
**🎯 MILESTONE: vertical slice giocabile. L'utente la prova nel Simulator.**

## T15 — Livelli di difficoltà (solo gameplay)
**Dipende da:** T14
**Fai:** Guided: highlight degli interagibili (emissione #E69F00 pulsante via MaterialPropertyBlock, solo in Guided), hint data-driven (`HintSO` ordinati sui passi → evento `HintRequested(key)` dopo X s senza progressi e a richiesta con un tasto controller), evento di pericolo up/mid/down, nessun danno/morte. Operational: 2–3 distrattori interagibili. Crisis: `DisturbanceController` (luci che sfarfallano ≤3 Hz, loop audio opzionale, flag per ridurre gli effetti). Nessun aiuto in Operational/Crisis.
**Done quando:** i tre livelli cambiano comportamento da comando di test; nessun sfarfallio >3 Hz.

## T16 — Replay ed evento dinamico
**Dipende da:** T14
**Fai:** a ogni avvio scelta casuale di `SubstanceVariant` (SDS, set DPI, colore, crescita); rimescolo dell'armadietto (decoy inclusi); jitter soglie ventilazione; evento dinamico "seconda perdita" in un istante casuale entro una finestra da config (segnale audio opzionale, pozzanghera che si espande se non contenuta, evento + log). Seed loggato; comando per fissare il seed.
**Done quando:** 5 partite consecutive mostrano varianti diverse con SDS/DPI coerenti; l'evento dinamico avviene e compare nel recap.

## T17 — Integrazione asset del modellatore (su richiesta)
**Dipende da:** T14 + asset in `Shared/` (consegnati dall'utente)
**Fai:** per ogni asset indicato dall'utente: **duplica** da `Shared/` a `GROUP/` (Art/Models, Art/Materials, Audio/Sound…, mai modificare l'originale), rinomina secondo convenzione, sostituisci il contenuto del figlio `Visual` (e le clip audio nei campi opzionali) mantenendo collider e logica; aggancia gli `UnityEvent` alle animazioni solo se il modellatore le ha già fornite. Registra fonte/licenza degli asset in `Docs/ASSET_LICENSES.md` (se l'utente le fornisce).
**Fuori scope:** modelli o animazioni creati da te.
**Done quando:** gli oggetti indicati sostituiti, scena senza errori, scenario ancora completabile.

## T18 — Documentazione tecnica e manuale dei comandi
**Dipende da:** T14 (aggiornare dopo T15–T17)
**Fai, partendo da HANDOFF.md:** in `Docs/`: documentazione tecnica (script, prefab, trigger, asset, dipendenze); manuale dei comandi di test; flowchart Mermaid con decisioni, conseguenze, eventi, esiti; elenco di eventi/stato esposti dal Director per la UI futura.
**Done quando:** documenti coerenti col codice.

## T19 — Controllo prestazioni (Quest)
**Dipende da:** T17
**Fai:** controllo di tris, draw call, luci, trasparenze, particelle, materiali (static batching, baking); suggerimenti e correzioni nel solo scope. Report breve in `Docs/PERF_REPORT.md`.
**Done quando:** report con numeri e correzioni; nessun errore.
