# Chemical Alert — stato del lavoro

**Ultimo aggiornamento:** 2026-11-24
**Stato:** punti 1–2 e documentazione (punto 6) completati; restano verifiche Simulator/headset e punti 4, 5 e 7. Modifiche non committate.
**Scena:** `Level/Scenes/SCN_ChemicalAlert.unity`

---

## Obiettivo concordato

Aggiornare il gameplay **senza imporre un ordine di azioni**. Le regole di riferimento:

- **Tre DPI obbligatori**: guanti chimici, maschera/respiratore, protezione occhi (occhiali/visiera).
- **Due copie interagibili per tipo** nel mobiletto: acquisirne una disabilita l'interazione dell'altra.
- **Ordine libero**: ventilazione, SDS, DPI e contenimento possono essere affrontati in qualsiasi sequenza.
- **Errori non bloccanti** portano a esito `Partial`, non bloccano la partita.
- **Vite**: Guided non perde vite, Operational parte con 3, Crisis con 1.
- **Durata partita**: 180 s. **Escalation ventilazione**: 40 / 60 / 80 s. Nessuna scadenza separata per i DPI.
- **Contenimento**: un solo anello contenitivo; tre fogli assorbenti sulla pozzanghera, assorbimento ~4,5 s, successo automatico dopo altri 2 s.
- **Pannello SDS**: acido formico per tutte le varianti, sfondo bianco al 50%, testo nero, quattro pittogrammi. Chiusura con **B** o seconda interazione con l'SDS.
- **Feedback**: popup errori omogenei ~4–5 s; vignetta tossica senza protezione occhi; cue di tosse avvicinandosi senza maschera.
- **Non previsti**: schermata di vittoria, fase di uscita safe, smaltimento rifiuti.

---

## Completato

### Codice e dati

- Aggiornati `ChemicalAlertDirector`, `PlayerHealth`, `PlayerPPEState`, `SubstanceContactSensor`, `CleanupSystem`, `PerimeterSlots`, `Puddle`, `AbsorbentSheet`, `PPEItemObject`, `ScenarioInteractable`, `ScenarioInputBindings`, `SdsItem`, `SpillSausage`, `VentilationSystem`, `VentilationEscalation`, `ScenarioReplayController` e `OutcomeEvaluator`.
- Tutte le varianti sostanza richiedono gli stessi tre tipi di DPI.
- Acquisire una copia di un DPI nasconde la copia raccolta e disabilita l'interazione del duplicato.
- Tentare un'azione con gli strumenti senza guanti registra un errore e costa una vita per tentativo; in Guided il danno è soppresso.
- Avvicinarsi senza maschera genera avviso e cue di tosse; esposizione ad aria tossica senza protezione occhi emette la vignetta.
- Perimetro ridotto a un singolo anello logico/visivo; la pozzanghera viene contenuta al posizionamento dell'anello.
- Tre fogli avviano l'assorbimento da 4,5 s con macchia progressiva; completamento automatico dopo altri 2 s.
- Rimosse dal codice le classi e i percorsi `PPEEquipZone`, `WasteItem`, `WasteBag`, `ExitSafeZone`; aggiornati gli esiti e il comando `RunSmokeTest`.
- Configurazioni verificate: 180 s, soglie 40/60/80 s, 3 fogli, assorbimento 4,5 s, delay 2 s, vite Guided 0 / Operational 3 / Crisis 1.
- Creato `Interaction/ChemicalAlertPanelUI.cs`: pannello SDS statico acido formico, quattro pittogrammi, popup errori 4,5 s, chiusura con B o seconda interazione SDS, vignetta tossica e cue di tosse (sintetico di fallback se non viene assegnato un clip).

### Scena e asset

- Riparati i riferimenti null di Director e sistemi: ventilazione, pozzanghera, SDS, DPI, salute, cleanup, replay, contatto sostanza, input.
- `PPECabinet` ricostruito con **7 figli**: `Visual` + `PPE_Gloves_A/B`, `PPE_Mask_A/B`, `PPE_EyeProtection_A/B`. Tutti con `PPEItemObject`, `ScenarioInteractable`, `Rigidbody` e `BoxCollider`, `itemData` corretti e dimensioni visive plausibili.
- Rimossa la versione "display" del mobiletto non collegata al gameplay; le copie A/B sono quelle effettivamente interagibili.
- Perimetro scena ridotto a un solo `SpillSausage_0` / `PerimeterSlot_0`; fogli ridotti a tre `AbsorbentSheet`.
- Rimossi `TestCube`, le vecchie zone DPI e gli oggetti scena `WasteBag`, `ZONE_ExitSafe`, marker `POS_WasteBag`.
- Rimossa dal prefab `AbsorbentSheet` la componente di smaltimento mancante; pulito `ScenarioMarkers` da un marker obsoleto.
- Importati TMP Essentials (autorizzazione utente) in `Assets/TextMesh Pro/`; la scena contiene canvas World Space per SDS/errori e canvas per la vignetta.
- `Docs/SDS/` contiene il testo acido formico e i quattro PNG usati dalla UI.

### Verifiche eseguite

| Verifica | Esito |
|---|---|
| Compilazione C# Unity | Nessun errore |
| Validazione scena | 0 issue, 0 missing script, 0 prefab rotti |
| `RunSmokeTest` in Play Mode ×3 dopo reset (2026-11-24) | 7/7 PASS ogni volta, esito `Success`, 3 cuori, 3 DPI acquisiti, contenimento/copertura/cleanup completi |
| Operational senza DPI | Contatto 3→2; uso anello senza guanti 2→1, azione comunque permessa |
| Guided senza DPI | Contatto non imposta `IsDead` |
| Validazione mobiletto da log | 3 tipi × 2 copie, nessun decoy |
| Pannello SDS | Visibile in cattura Unity; leggibilità a piena risoluzione da confermare su Simulator/headset |
| Reload scena + audit riferimenti (2026-11-24) | 0 missing script, 0 prefab rotti; Director/PlayerPPEState/PPECabinet tutti assegnati |
| Replay seed fisso `81081` | Due reset identici: `variantB`, seconda perdita a `29.85 s`, composizione armadietto invariata |
| Replay seed casuale | Seed diversi (`1141970398`, `419091233`) e varianti diverse (`variantC`, `variantA`) |
| Seconda perdita T16 + anello singolo | Evento a 30 s, `event.second_spill` registrato, crescita x2, contenimento `1/1` ferma la crescita, assorbimento e `Success` |

**Nota Console:** nessun errore o warning di gameplay durante Play Mode. Alla chiusura restano messaggi OVR/XR di teardown (`xrDestroySession` con spazi non distrutti), non errori `[CA]`.

**Nota audit (2026-11-24):** i soli riferimenti null rimasti sono 15 slot in `guidedHighlightTargets`, 6 in `operationalDistractors` (da rivedere al punto 4) e i campi audio opzionali (`coughAudioClip`, `VentilationSystem.audioLoop`, `DisturbanceController.loopSource`, `ScenarioReplayController.secondSpillAudioSource`, `SubstanceContactSensor.coughSfx`), gestiti con fallback.

---

## Da fare

### 1. Ripresa e verifica riferimenti

- [x] Ricaricare la scena salvata e ricontrollare riferimenti, gerarchia e prefab dopo un reload completo. — **OK** (reload da disco: scena non dirty, 20 root, 566 GameObject, 0 missing script, 0 prefab rotti).
- [x] Confermare che la modifica manuale al `PPECabinet` non abbia invalidato i riferimenti di Director/`PlayerPPEState`. — **OK** (7 figli, 6 copie PPE collegate, tutti i riferimenti assegnati).

### 2. Play Mode pulito e smoke test

- [x] Svuotare la Console prima del test.
- [x] Play Mode pulito post-pulizia prefab.
- [x] Eseguire `RunSmokeTest` **tre volte** dopo reset, verificando 7/7 PASS ed esito `Success`. — **OK** (ogni run: `Success`, 3 cuori, 3 DPI, contenimento/copertura/cleanup completi).
- [x] Verificare replay con seed **fisso** e **casuale**. — **OK** (`81081` riproduce `variantB` / `29.85 s`; due seed casuali e varianti diversi).
- [x] Verificare la seconda perdita T16 con il nuovo anello singolo. — **OK** (evento a 30 s, espansione x2, anello `1/1` ferma la crescita; assorbimento e `Success`).

### 3. Verifiche su Simulator / headset

> **Bloccante attuale:** XR Simulator 2.0 non è in esecuzione (`XR Simulator is not running`).

- [ ] Avviare XR Simulator 2.0 e lanciare l'applicazione.
- [ ] Leggibilità SDS a piena risoluzione.
- [ ] Chiusura pannello con **B**.
- [ ] Chiusura con **seconda interazione** sull'SDS.
- [ ] Durata e stile dei popup errore (~4–5 s).
- [ ] Vignetta tossica senza protezione occhi.
- [ ] Cue di tosse avvicinandosi senza maschera.
- [ ] Valutare sostituzione del cue sintetico con un clip dedicato (non presente nel progetto).

### 4. Verifica visiva

- [ ] Scala/aspetto dell'anello quando si espande sulla pozzanghera (`SpillSausage_0` ha `placedVisualScaleFactor = 9`).
- [ ] Layout e spaziatura dei sei DPI nel mobiletto.
- [ ] Rivedere `guidedHighlightTargets` / `operationalDistractors`: contengono voci PPE e slot null da ripulire se non più coerenti.

### 5. Pulizia asset

- [ ] Confermare l'inutilizzo e quindi eliminare `Level/Prefabs/WasteBag.prefab` (+ `.meta`).
- [ ] Confermare l'inutilizzo e quindi eliminare `Level/Prefabs/ZONE_ExitSafe.prefab` (+ `.meta`).
- [ ] Eliminare le **cinque** catture in `Assets/Screenshots/`: `132522`, `132721`, `132759`, `133051`, `150615` (ciascuna con `.meta`).

> I precedenti tentativi di cancellazione via Editor sono stati bloccati dal safety check e non hanno avuto effetto.

### 6. Documentazione

- [x] Aggiornare `00_BRIEF.md` alle nuove regole.
- [x] Aggiornare `Docs/TECHNICAL_DOCS.md` (ordine libero, DPI, anello singolo, esiti, scena e verifiche runtime).
- [x] Aggiornare `Docs/TEST_COMMANDS_MANUAL.md` (comandi attuali e procedure ripetibili).
- [x] Aggiornare il sintetico `HANDOFF.md`.
- Nota: `TASKS.md` contiene la cronologia del brief precedente; non riscriverla come stato attuale.

### 7. ProBuilder

- [ ] Decidere se mantenere o annullare `lightmapping.autoUnwrapLightmapUV: true` in `ProjectSettings/Packages/com.unity.probuilder/Settings.json` (unica differenza rilevata, non modificata deliberatamente in questo task).

---

## File principali toccati

- `Code/Scripts/Systems/`: `ChemicalAlertDirector`, `CleanupSystem`, `PerimeterSlots`, `Puddle`, `PlayerHealth`, `PlayerPPEState`, `ScenarioReplayController`, `SubstanceContactSensor`, `VentilationEscalation`, `VentilationSystem`.
- `Code/Scripts/Interaction/`: `AbsorbentSheet`, `PPEItemObject`, `ScenarioInputBindings`, `ScenarioInteractable`, `SdsItem`, `SpillSausage`; **nuovo** `ChemicalAlertPanelUI`.
- `Code/Scripts/Core/OutcomeEvaluator.cs`, `Code/Scripts/Data/ChemicalAlertConfig.cs`, i tre config (Guided/Operational/Crisis) e le tre varianti sostanza.
- `Code/Scripts/Debug/DebugCommands.cs`.
- `Level/Scenes/SCN_ChemicalAlert.unity`, `Level/Prefabs/AbsorbentSheet.prefab`, `Level/Prefabs/ScenarioMarkers.prefab`.
- `Docs/SDS/` (testo e pittogrammi).
- Eliminati: `Interaction/ExitSafeZone.cs`, `Systems/PPEEquipZone.cs`, `Systems/WasteBag.cs`, `Systems/WasteItem.cs` (+ `.meta`).
- Documentazione aggiornata: `Docs/TECHNICAL_DOCS.md`, `Docs/TEST_COMMANDS_MANUAL.md`, `Docs/Agent/Chemical_Alert/{00_BRIEF.md,HANDOFF.md,TASK_PROGRESS.md}`.

---

## Attenzione alla ripresa

- **Nessun commit presente**: working tree con 49 voci (40 file tracciati modificati/eliminati + 9 voci non tracciate). Preservare tutte le modifiche elencate.
- Sono presenti anche modifiche a `Docs/Agent/Evaquation/EV_*.md` non legate a questa task: non toccarle.
- TMP Essentials è importato in `Assets/TextMesh Pro/` con autorizzazione dell'utente.
- Le stringhe italiane della UI possono essere rifinite in un passaggio successivo.
- L'ultima modifica visiva al `PPECabinet` è manuale dell'utente: verificarla prima di intervenire.
