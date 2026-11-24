# TASK — Evacuation (una sessione nuova per task)

## Prompt da incollare a inizio sessione
> Leggi Assets/_Project/Groups/G_Tiziano/Docs/Agent_Evacuation/EV_00_BRIEF.md e EV_01_CONVENTIONS.md e, in EV_HANDOFF.md (stessa cartella), SOLO "Stato corrente" e le ultime 2 entry del log (da E01 in poi leggi anche REUSE_MAP.md). Poi esegui SOLO la task **Exx** (leggi solo la sua sezione in EV_TASKS.md). NON usare git in nessun modo. NON scrivere fuori da Assets/_Project/Groups/G_Tiziano/. NON modificare nulla di Chemical Alert. NON creare UI né animazioni. Quando i criteri "Done quando" sono soddisfatti, aggiorna EV_HANDOFF.md, rispondimi con l'elenco dei comandi di test e fermati.

## Prompt di emergenza (contesto quasi saturo)
> Sei vicino al limite di contesto. Aggiorna subito EV_HANDOFF.md con lo stato REALE (fatto, mancante, errori aperti, comandi di test attivi) e fermati. Niente git.

## Mappa
- Ordine: **E00 → E01 → E02 → E03 → E04 → E05 → E06 → E07 (🎯 vertical slice) → E08 → E09 → E10 → E11 → E12 → (E13)**.
- Con i modelli a 1M di contesto si possono unire senza rischi: **[E00+E01] · [E02+E03] · [E04+E05] · [E08+E09] · [E10+E12]**. E06 ed E07 meglio da soli.
- **E11 (modello reale)** si può fare appena l'utente carica l'FBX, anche prima di E08; se il modello (costruito sulla pianta del BRIEF) è già pronto prima di E01, E01 lo usa direttamente invece del graybox.
- Scadenza: Evacuation su Unity 24–25 nov → vertical slice (E07) entro il 24.

---

## E00 — Ricognizione del riuso (solo lettura)
**Dipende da:** —
**Fai:** leggi (solo firme pubbliche, non i corpi) le classi CA riusabili in `Code/Scripts/{Core,Interaction,Systems,Data,Debug}/` e `Docs/Agent/ENV_REPORT.md`. Scrivi `Docs/Agent_Evacuation/REUSE_MAP.md` (≤50 righe) con, per ciascuna: namespace, nome, firma pubblica essenziale, verdetto **RIUSA così com'è / COPIA in `Evacuation/` / NON TOCCARE**. Da coprire: `ScenarioInteractable` (+ metodi `Simulate…` se esistono), `ScenarioHandInteractor`, `ScenarioInputBindings`, `ActionLog`, `ScenarioResult`, enum `Dimension`/`Outcome`, `IResettable`, pattern `ChemicalAlertConfig`/`HintSO`, il codice di highlight emissione (MaterialPropertyBlock), il registro tasti di `DebugCommands`, il pattern di `RunSmokeTest`, `DisturbanceController`. Rileva anche cosa è montato sull'`OVRCameraRig` in `SCN_ChemicalAlert` (componenti, ancore, interattori) e quale layer/collider usa l'hover.
**Fuori scope:** qualsiasi modifica.
**Done quando:** REUSE_MAP esiste, nessun asset modificato, HANDOFF compilato.

## E01 — Scena, rig, graybox e marker
**Dipende da:** E00
**Fai:** crea `Level/Scenes/SCN_Evacuation.unity` **duplicando** `SCN_ChemicalAlert` via AssetDatabase.CopyAsset, poi nella COPIA rimuovi tutto il contenuto CA (oggetti e componenti CA-specifici sul rig: DebugCommands CA, zone PPE, salute, ecc.) lasciando: `OVRCameraRig` con bindings e interattori mani, luce direzionale. Verifica che la scena CA originale sia intatta (0 diff). Costruisci il graybox 1:1 del BRIEF (pavimento, muri con vani porta, 2 uffici con 4 scrivanie+sedie prop ciascuno, corridoio, blocchi scale emergenza/interne e ascensore, soffitto opzionale/assente) con primitive e root+`Visual`, materiali placeholder in `Art/Materials/Evacuation/`. Crea TUTTI i marker del BRIEF sotto `ScenarioMarkers` (numero `ROUTE_*` ragionevole; 10 `FIRESLOT` distribuiti tra corridoio, davanti alle porte, dentro gli uffici tra le scrivanie e vicino alle scale). Struttura cartelle `Code/Scripts/Evacuation/*`, `Level/Prefabs/Evacuation/`. Posiziona il rig su `SPAWN_Player`.
**Fuori scope:** gameplay, modelli reali.
**Done quando:** scena si apre e va in Play senza errori; CA intatto; si cammina nel graybox a scala 1:1 nel Simulator senza compenetrazioni; tutti i marker esistono con i nomi del BRIEF; niente componenti CA sul rig. (Il comando di teletrasporto ai marker arriva in E03.)

## E02 — Dati e valutatore (logica pura)
**Dipende da:** E01
**Fai:** in `Evacuation/Data`: `EvacuationConfig` SO + 3 istanze (`SO_EvacuationConfig_Guided/Operational/Crisis`): durata (0=infinita), `hasTimeLimit`, `hearts` (0/3/1), `fireEnabled`, numero fuochi attivi (5), contatti letali (3), cooldown contatto (2 s), `highlight`, `hintsEnabled`, `hintIdleSeconds`, `disturbances`, `npcCount` per stanza (3/2), `routeLaneWidth` (1.2), punti 0/30/10/0 e 0/50/20/0, `interactionDwell`. `OnValidate` con avvisi `[EV]`. In `Evacuation/Core`: `EvacuationOutcomeEvaluator` che, dato uno snapshot (piano ispezionato, NPC raccolti per stanza, uscita raggiunta {None, Emergency, Internal}, ascensore usato, contatti fuoco, vivo, timeout, difficoltà, tempo impiegato), produce `ScenarioResult` (riusa tipi CA) con regole di Esiti e Valutazione del BRIEF e le voci di log LG01–LG11 (id/dimensione/esito del BRIEF). Aggiungi `SelfCheck` con ≥10 casi (Success, Partial NPC mancanti, Partial senza piano, Partial scale interne, Fail ascensore, Fail timeout, Fail morte fuoco, Guided ascensore → Partial, Guided senza limite, sotto-punteggi attesi) con PASS/FAIL.
**Fuori scope:** scena, runtime, testi italiani.
**Done quando:** compila; il SelfCheck (ContextMenu su un componente temporaneo `EvacSelfCheckRunner` o, se già esiste, `EvacDebugCommands`) stampa tutti PASS.

## E03 — EvacuationDirector e comandi di debug
**Dipende da:** E02
**Fai:** `EvacuationDirector` (prefab `MNG_EvacuationDirector`, in scena) con fasi Briefing → Action → Resolution → ResultHandoff; `StartScenario()` (+ `autoStartOnPlay` con ritardo configurabile, default attivo solo in Editor); timer countdown o cronometro secondo config; hearts; `Seed` (`System.Random` unico, fissabile); registro `IResettable` e `ResetScenario()` (riporta tutto e teletrasporta il rig a `SPAWN_Player`); API `Report…` che altri sistemi useranno (stub: `ReportPlanInspected`, `ReportNpcCollected`, `ReportExitReached(kind)`, `ReportElevatorUsed`, `ReportFireContact`); eventi del BRIEF; alla fine chiama l'evaluator ed emette `ResultReady` + stampa recap in console. Crea `EvacDebugCommands` (registro tasti + ContextMenu, stampa `H`): avanza fase, cambia difficoltà (reset), forza timeout, stampa stato, reset, 3 reset consecutivi, teletrasporto ciclico ai marker, fissa/randomizza seed, esegui SelfCheck. Crea lo scheletro di `RunSmokeTest`.
**Done quando:** fasi/timer/cronometro funzionano da comando; i 3 livelli caricano il proprio config; 3 reset consecutivi riportano allo stato iniziale senza sottoscrizioni duplicate; `H` elenca i comandi.

## E04 — Allarme, piani di evacuazione e porte
**Dipende da:** E03
**Fai:** `AlarmSystem` (IResettable): parte a inizio Action, `AudioClip` opzionale in loop, luce/emissione pulsante ≤3 Hz su `POS_Alarm` (+ cartello uscita illuminato sopra le scale emergenza: emissione fissa), evento `AlarmStateChanged`. `EvacuationPlanInspectable` (3 istanze su `POS_Plan_*`, root+`Visual`+collider): ispezione = Activated in hover oppure sguardo fermo ≤1.2 m per `interactionDwell`; emette `PlanInspected(planId)` al Director (log LG01 solo la prima volta). `DoorInteractable` (Activated apre/chiude istantaneo, `UnityEvent OnOpened/OnClosed`, collider disattivato da aperta, `IResettable`) montata su porte stanza (aperte), scale emergenza (chiusa, sbarrafuoco), scale interne (chiusa). Comandi: forza allarme on/off, simula ispezione piano, apri/chiudi porte. Estendi `RunSmokeTest`.
**Done quando:** allarme parte con Action e si ferma a fine; ispezionare un piano logga LG01 una sola volta; porte funzionano e si resettano; test Simulator: hover+trigger sul piano e sulla porta.

## E05 — Ascensore, scale e uscita
**Dipende da:** E04
**Fai:** `ElevatorButton` (Activated su `POS_ElevatorButton`, entrambi i pulsanti se due) → `ReportElevatorUsed` (LG06; Guided: evento pericolo Down + log, scenario continua). `StairsExitZone` (trigger) su `ZONE_StairsEmergency` e `ZONE_StairsInternal`: rileva il giocatore (testa/rig) → `ReportExitReached(kind)`; il Director chiude la partita, chiama l'evaluator. Uscita da una stanza senza piano ispezionato → LG02 a fine partita. Comandi: simula ascensore, simula uscita emergency/internal (teletrasporto al trigger). Estendi `RunSmokeTest`.
**Done quando:** (senza NPC) ascensore → Failure (Guided: no); scale interne → Partial; scale emergenza con piano → Success; reset pulito; test Simulator: premere pulsante ascensore, entrare nelle zone scale.

## E06 — NPC colleghi (raccolta e fila indiana)
**Dipende da:** E05
**Fai:** `EvacNpc` (root con collider area di interazione ~1 m + figlio `Visual` capsula; stati Idle/Following/Evacuated; `UnityEvent OnFollowStarted/OnStopped/OnEvacuated`), `ScenarioInteractable` su di esso: Activated → Following, evento `NpcDialogueRequested(key)` (follow/already). `NpcTrailFollower`: il giocatore lascia briciole (testa proiettata a terra, ogni ~0.25 m); ogni NPC segue il percorso con distanza 0.8–1 m dal precedente nella fila (ordine di raccolta), velocità ≤ 1.6 m/s, quota Y fissa, si ferma se il giocatore si ferma; nessun NavMesh. `NpcRegistry` (IResettable): spawn di 3+2 NPC sugli `NPCSLOT_R1/R2` scelti col seed, conteggio per stanza; a `ReportExitReached(Emergency)` gli NPC Following diventano Evacuated; LG03/LG04 quando una stanza è completa; LG05/LG09 all'uscita. Comandi: raccogli NPC N, raccogli tutti, stampa stato fila. Estendi `RunSmokeTest`.
**Done quando:** raccolta e fila funzionano da comando e (test Simulator) con la mano; Success richiede 5/5, 3/5 → Partial; reset ripristina NPC e fila senza briciole residue.

## E07 — 🎯 Esiti completi e vertical slice (Operational end-to-end)
**Dipende da:** E06
**Fai:** integrazione e rifinitura: `RunSmokeTest` completo che esegue in sequenza (con reset tra una e l'altra) Success, Partial (NPC mancanti), Partial (scale interne), Failure (ascensore), Failure (timeout con soglia ridotta) in Operational e stampa `n/n PASS`; verifica recap console e `ResultReady` in ogni caso; verifica che gli eventi non vengano emessi doppi; sistema ciò che non torna. Nessuna nuova feature.
**Done quando:** smoke test tutto PASS ×3 consecutivi con reset; 0 errori console; scena valida. **🎯 MILESTONE: l'utente la prova nel Simulator (Operational completo).**

## E08 — Livelli di difficoltà (Guided e Operational/Crisis non-fuoco)
**Dipende da:** E07
**Fai:** Guided: highlight #E69F00 pulsante (MaterialPropertyBlock, riusa il codice CA se generico) su piani, NPC non ancora raccolti, porta+cartello scale emergenza; `HintSO` ×5 con chiavi `evac.hint.*` ordinati sui passi → `HintRequested(key)` dopo `hintIdleSeconds` senza progressi e a richiesta (comando + binding controller se già previsto in `ScenarioInputBindings`); `DangerLevelChanged` (Up = sul percorso giusto, Mid = fermo oltre soglia, Down = errore come ascensore/scale interne); cronometro senza sconfitta. Operational: nessun aiuto, timer 180. Crisis: timer 120, nessun aiuto, flag per ridurre gli effetti (preparazione di E09). Comandi: cambia livello, richiedi hint, stampa comportamento livello.
**Done quando:** i 3 livelli cambiano comportamento da comando; Guided non può perdere per tempo né per ascensore; nessun highlight in Operational/Crisis.

## E09 — Crisis: incendio, fumo, contatti e corsia libera
**Dipende da:** E08
**Fai:** `FireSlot` (prefab: root con trigger collider + figlio `Visual` con VFX placeholder fuoco e fumo a particelle con tetto; cerca prima in `Shared/` un VFX riusabile, mai modificarlo) sui `FIRESLOT_01..10`; `FireManager` (IResettable): solo in Crisis attiva 5 slot scelti col seed. `PlayerFireSensor`: collider trigger a capsula che segue la testa (XZ) + Rigidbody kinematic; ingresso in una zona fuoco = contatto, cooldown 2 s; `ReportFireContact` → contatore 1/2/3; eventi `FireContact(index, intensity)`; 3° contatto = Hearts 0 → `Died` → Failure (LG07 per 1–2, LG08 per 3). **Corsia libera**: `FireLaneValidator` verifica, per ogni segmento delle polilinee `ROUTE_R1_*`/`ROUTE_R2_*`, che esista uno spostamento laterale (−1.5…+1.5 m, passo 0.25) in cui una box larga `routeLaneWidth` non tocchi né fuochi attivi né muri; `FireManager` ripete l'estrazione (max 50 tentativi) finché la combinazione è valida; comando che valida N seed (es. 200) e stampa quante combinazioni sono state scartate (se >50% scartate, segnala slot da spostare). Fumo che non offusca oltre il tetto; flag "riduci effetti". Comandi: forza contatto, stampa fuochi attivi, valida seed, mostra gizmo. Estendi `RunSmokeTest` (caso Crisis: 3 contatti → Failure).
**Done quando:** in Crisis 5 fuochi attivi, mai bloccanti su 200 seed; contatti 1/2/3 come da BRIEF; in Guided/Operational nessun fuoco; reset ripristina; test Simulator: camminare nel fuoco 3 volte.

## E10 — Replay, seed e robustezza del reset
**Dipende da:** E09
**Fai:** replay: seed nuovo a ogni `ResetScenario()` (fissabile da comando), posti NPC e fuochi variano, seed e scelte loggati in `ResultReady`/ActionLog (`ReplayStart`). 5 partite consecutive con esiti diversi senza errori né oggetti residui (NPC, briciole, porte, fuoco, allarme, sottoscrizioni). Verifica che lo stesso seed riproduca stessa disposizione. Controlla rischi di riferimenti nulli dopo reset multipli e 10 reset di fila.
**Done quando:** 5 partite variate OK; seed fisso ripetibile; 10 reset puliti; smoke test ancora tutto PASS.

## E11 — Sostituzione del graybox con il modello 3D (richiede upload dell'utente)
**Dipende da:** E07 (idealmente E10)
**Contesto:** il modello è costruito dal modellatore SULLA STESSA PIANTA del graybox (layout del BRIEF, definitivo). Quindi i marker restano quasi dove sono: serve solo riallinearli alle misure reali del modello. Nessuna rimappatura di stanze.
**Input dell'utente (compila prima di lanciare):** percorso FBX/prefab in `Shared/`: `<…>`; prop già inclusi (scrivanie, sedie, porte, cartelli, scale, ascensore, piani di evacuazione): `<…>`; note su differenze di misure rispetto al BRIEF: `<…>`.
**Fai:** **duplica** da `Shared/` a `GROUP/Art/Models/` (mai modificare l'originale), istanzia il modello e allinealo all'origine del graybox (stessa origine e orientamento: X est, Z nord). Aggiungi collider semplici (box) a muri/pavimento, niente mesh collider complessi. Sostituisci i figli `Visual` del graybox (porte, scrivanie, scale, ascensore, cartello, piani) con i prop del modello, mantenendo root, collider e logica. Verifica ogni marker (`SPAWN_Player`, `NPCSLOT_*` ≥6 per stanza, `POS_*`, `ZONE_*`, `ROUTE_*`, `FIRESLOT_*`) contro la geometria reale: sposta solo quelli che risultano dentro muri/arredi o fuori posto, nomi invariati. Disattiva/rimuovi il graybox. Poi esegui validatore corsia (E09), smoke test, e controlla scala 1:1 e assenza di compenetrazioni nei passaggi (porte, corridoio, vani scale).
**Fuori scope:** creare/modificare modelli o animazioni; cambiare il layout.
**Done quando:** scena giocabile col modello, smoke test PASS, validatore corsia OK su 200 seed, 0 errori; elenco dei marker spostati in HANDOFF.

## E12 — Documentazione e tabella frasi del debrief
**Dipende da:** E07 (aggiornare dopo E08–E11)
**Fai (da HANDOFF + codice, leggi ora l'allegato frasi del BRIEF):** in `Docs/`: `EVAC_TECHNICAL_DOCS.md` (script, prefab, trigger, SO, asset, dipendenze da CA, eventi/stato del Director per la UI futura, chiavi di localizzazione), flowchart Mermaid (decisioni, conseguenze, eventi, esiti, 3 livelli), `EVAC_DEBRIEF_TABLE.md` (ID log LG01–LG11, frase modello con `{t}`, "cosa potevi fare", dimensione, chiavi), `EVAC_TEST_COMMANDS_MANUAL.md` (controlli Simulator + tutti i comandi di debug + procedura di verifica).
**Done quando:** documenti coerenti con nomi di classi, eventi, chiavi e tasti reali.

## E13 — Controllo prestazioni (Quest, opzionale)
**Dipende da:** E11
**Fai:** conteggio tris, draw call, materiali, trasparenze, particelle (Crisis), luci; correzioni nel solo scope (static batching, tetto particelle). Report breve in `Docs/EVAC_PERF_REPORT.md`.
**Done quando:** report con numeri e correzioni; nessun errore.
