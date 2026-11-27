# CONVENZIONI E REGOLE — EVACUATION (leggere a inizio di ogni sessione)

Cartella di lavoro: `Assets/_Project/Groups/G_Tiziano/` (d'ora in poi `GROUP/`). Docs dell'agente: `GROUP/Docs/Agent_Evacuation/`.

## 🚫 DIVIETI ASSOLUTI
1. **GIT: mai, in nessun modo.** I commit li fa l'utente.
2. **Chemical Alert è CONGELATO**: vietato modificare/spostare/cancellare script, scena `SCN_ChemicalAlert`, prefab in `Level/Prefabs/` (esclusi quelli che crei tu in `Evacuation/`), SO, materiali, `Docs/Agent/`. Puoi LEGGERLO e RIUSARE le sue classi generiche (vedi REUSE_MAP creata in E00). Se serve una modifica a codice CA: duplica la classe in `Evacuation/` con il tuo namespace, oppure annota in HANDOFF "Richieste all'utente".
3. **Non scrivere fuori da `GROUP/`.** Vietato toccare `Shared/` (solo lettura e **duplicazione** in `GROUP/`), `Home/`, `Groups/G_*` altrui, `Packages/`, `ProjectSettings/`, `Assets/` radice, `Library/`.
4. Non installare/aggiornare package, non cambiare Project Settings né build target.
5. **Niente UI** (canvas, pannelli, testi a schermo, HUD, vignette, schermate) **e niente animazioni** (Animator, clip, DOTween). Eventi + stato + log. Eccezione: movimento programmatico degli NPC (transform) e VFX particellari placeholder per fuoco/fumo.
6. Non toccare i `PLACEHOLDER.txt`. Non cancellare file che non hai creato tu.

## Risparmio di contesto
- Leggi solo: EV_00_BRIEF, questo file, "Stato corrente" + ultime 2 entry di EV_HANDOFF, `REUSE_MAP.md` (da E01 in poi), la TUA task in EV_TASKS. L'allegato frasi del BRIEF solo in E12.
- Mai leggere la gerarchia completa della scena: cerca per nome/componente. Mai stampare file >150 righe: usa intervalli.
- Una task = un obiettivo. Lavoro extra → HANDOFF ("Problemi aperti").

## Posizioni (sotto GROUP/)
- Script: `Code/Scripts/Evacuation/{Core,Interaction,Systems,Data,Debug}/` · SO istanze: `Code/Scripts/Evacuation/Data/Assets/`
- Prefab: `Level/Prefabs/Evacuation/` · Scena: `Level/Scenes/SCN_Evacuation.unity` · Materiali: `Art/Materials/Evacuation/` · Modelli duplicati da Shared: `Art/Models/` · Audio: `Audio/Sound/`
- I file vanno creati in modo che Unity generi i `.meta` (via MCP o refresh subito dopo).

## Nomenclatura
- Asset: `SCN_` `SO_` `MAT_` `MESH_` `SFX_` `VFX_` `MNG_`. Esempi: `SO_EvacuationConfig_Guided`, `MNG_EvacuationDirector`. PascalCase, niente spazi/accenti, varianti come suffisso, nomi tecnici in inglese.
- Codice: PascalCase classi/metodi/proprietà/campi pubblici; camelCase campi privati (`[SerializeField] private`); COSTANTI maiuscole; `/// <summary>` su ogni metodo pubblico.
- **Namespace obbligatorio `SI81.Tiziano.Evacuation`** (+ `.Core .Interaction .Systems .Data .Debug`). Stesso assembly di tutti i gruppi e di Chemical Alert: **non definire classi con lo stesso nome di classi CA** (es. non creare un secondo `ActionLog`): riusa l'originale con `using`, oppure usa il prefisso `Evac`. **Niente asmdef.**
- Log prefisso `[EV]`. Chiavi testo prefisso `evac.`.

## Architettura
- **Confine verso il futuro**: `EvacuationDirector` espone stato (`Phase`, `TimeRemaining`/`Elapsed`, `Hearts`, `Difficulty`, `Outcome`, `ActionLog`, `Seed`) ed eventi C# (`PhaseChanged`, `PlanInspected`, `NpcDialogueRequested`, `NpcCollected`, `FireContact`, `DamageTaken`, `Died`, `HintRequested`, `DangerLevelChanged`, `TimeUp`, `ResultReady`…). Nessun altro sistema parla con l'esterno. Gli altri sistemi riportano al Director con metodi `Report…`.
- **Interazioni**: solo `ScenarioInteractable` (di CA, riusato). Nessuno script tranne `ScenarioInputBindings` legge `OVRInput`. Deve funzionare col Meta XR Simulator.
- **Visual separato dalla logica**: ogni prefab = root (logica/collider) + figlio `Visual` (primitive/graybox). Dove servirebbe un'animazione: stato istantaneo + `UnityEvent` (`OnOpened`, `OnFollowStarted`…).
- **Ancoraggio ai marker**: nessuna coordinata nel codice; sostituire l'ambiente (E11) = spostare marker e cambiare `Visual`.
- **Dati**: tuning in ScriptableObject, nessun numero magico. **Eventi**: C# `event`, unsubscribe in `OnDisable`; niente `Find`/`SendMessage` a runtime (lookup una-tantum in Awake/Start ok).
- **Reset**: ogni sistema implementa `IResettable` (di CA); il Director li resetta tutti; nessuno stato statico che sopravviva; i riferimenti vanno re-registrati senza sottoscrizioni duplicate. Test: 3 reset consecutivi puliti.
- **Seed**: ogni casualità passa da un unico `System.Random` seedato dal Director (seed loggato, fissabile da comando).
- Audio: `AudioClip` opzionale per ogni SFX; se vuoto non succede nulla.
- Quest-friendly: niente luci realtime extra, niente post-processing, mesh/materiali condivisi, particelle con tetto (max ~100 per sistema).
- Input: solo `OVRInput` / nuovo Input System; mai `UnityEngine.Input` legacy.

## Ciclo di lavoro con l'MCP Unity
1. Scrivi script → compila → leggi console; correggi PRIMA di proseguire.
2. Dopo ogni modifica di scena/prefab: assegna TUTTI i riferimenti serializzati e salva.
3. Verifica in Play Mode: nessun NullReference/warning nuovo `[EV]`.
4. Una modifica di scena alla volta; niente operazioni batch giganti.

## Test: controlli del giocatore vs comandi di debug
- **Controlli giocatore** = controller Meta Quest 3 nel Meta XR Simulator (grip = afferra, trigger = attiva; binding provvisori in `ScenarioInputBindings`).
- **Comandi di debug** = `EvacDebugCommands` (creato in E03; Editor/Development): **ContextMenu SEMPRE presente** per ogni comando; tasto da tastiera in più. ⚠ Il Simulator ruba il focus della Game View: i tasti funzionano solo con focus sulla Game View e **evitare 0–9 e W/A/S/D** (usati dal Simulator). Il test automatico è `RunSmokeTest` (ContextMenu), da estendere a ogni task.
- A fine task scrivi in HANDOFF e nella risposta: **Comandi di test** (menu/tasto → cosa fa → risultato atteso, ≤10 righe) + **Test col Simulator** (passi) se tocca l'interazione. Il tuo test è via Play Mode/console; l'utente verifica col Simulator.

## HANDOFF (obbligatorio a fine task)
Riscrivi "Stato corrente" (≤15 righe) e aggiungi UNA entry di log (≤8 righe) + "Comandi di test" e "Richieste all'utente". Telegrafico.
