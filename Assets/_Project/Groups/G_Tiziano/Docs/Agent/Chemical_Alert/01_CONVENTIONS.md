# CONVENZIONI E REGOLE (leggere a inizio di ogni sessione)

Cartella di lavoro unica: `Assets/_Project/Groups/G_Tiziano/` (d'ora in poi `GROUP/`).

## 🚫 DIVIETI ASSOLUTI
1. **GIT: mai, in nessun modo.** Niente comandi git, niente commit/branch/stash/checkout, niente modifica di `.git*`. I commit li fa l'utente.
2. **Non scrivere fuori da `GROUP/`.** Vietato modificare/creare/spostare/cancellare in `Shared/`, `Home/`, `Groups/G_*` degli altri, `Packages/`, `ProjectSettings/`, `Assets/` (radice), `Library/`. Puoi LEGGERE `Shared/` e i package (read-only) e **duplicare** da Shared a `GROUP/` quando richiesto. Se ti serve qualcosa fuori scope, annotalo in HANDOFF ("Richieste all'utente") e continua con un placeholder.
3. Non installare/aggiornare package, non cambiare Project Settings, non cambiare build target.
4. **Niente UI** (canvas, pannelli, testi a schermo, HUD, menu, sottotitoli, schermate di esito) **e niente animazioni** (Animator, clip, DOTween, tween complessi). Eventi + stato + log.
5. Non toccare i `PLACEHOLDER.txt`. Non cancellare file che non hai creato tu.

## Risparmio di contesto
- Leggi solo: 00_BRIEF, questo file, "Stato corrente" + ultime 2 entry di HANDOFF, la TUA task. Niente altro finché non serve.
- Mai leggere la gerarchia completa della scena: cerca per nome/componente. Mai stampare file >150 righe: usa intervalli di righe.
- Una task = un obiettivo. Lavoro extra → HANDOFF ("Problemi aperti"), non farlo.

## Posizioni (tutte sotto GROUP/)
- Script: `Code/Scripts/` con sottocartelle `Core/ Interaction/ Systems/ Data/ Debug/`
- Prefab: `Level/Prefabs/` · Scene: `Level/Scenes/` (`SCN_ChemicalAlert.unity`)
- Materiali placeholder: `Art/Materials/` · Audio: `Audio/Sound/` · Docs: `Docs/` (questi file stanno in `Docs/Agent/`)
- I file vanno creati in modo che Unity generi i `.meta` (via Unity MCP o refresh subito dopo la scrittura).

## Nomenclatura (del progetto)
- Asset: `SCN_` scena · `SO_` ScriptableObject · `MAT_` · `MESH_` · `SFX_` · `VFX_` · `ICN_` · `MNG_` manager. Prefab: PascalCase descrittivo con prefisso di tipo se esiste (es. `MNG_ChemicalAlertDirector`, `Barrel`, `PPECabinet`).
- PascalCase descrittivo, niente spazi/accenti, varianti come suffisso (`SO_ChemicalAlertConfig_Guided`). Nomi tecnici in inglese.
- Codice: PascalCase per classi, metodi, proprietà e campi pubblici; camelCase per campi privati/protected (`[SerializeField] private`); COSTANTI maiuscole. Commento `/// <summary>` su ogni metodo pubblico.
- **Namespace obbligatorio: `SI81.Tiziano.ChemicalAlert`** (e `.Core`, `.Interaction`, `.Systems`, `.Data`, `.Debug` se utile). Tutti i gruppi compilano nello stesso assembly: senza namespace i nomi di classe collidono con quelli degli altri. **Niente asmdef.**
- Script/shader già presenti in `Shared/`: riferirli, non duplicarli.

## Architettura
- **Confine verso il futuro** (UI, Game System, Interaction comune): un solo punto, `ChemicalAlertDirector`, che espone: stato leggibile (`Phase`, `TimeRemaining`, `Hearts`, `Difficulty`, `Outcome`, `ActionLog`) ed eventi C# (`PhaseChanged`, `DamageTaken`, `VentilationStageChanged`, `HintRequested`, `ResultReady`, ecc.). Nessun altro sistema parla con l'esterno.
- **Interazioni (provvisorie)**: gli oggetti usano solo `ScenarioInteractable` (eventi Grabbed, Released, Activated, Inspected, HoverEnter/Exit). L'implementazione usa Meta XR Core SDK (`OVRInput`, ancore dei controller dell'`OVRCameraRig`) + Input System; quando arriverà l'Interaction Library comune si sostituirà solo questo strato. Deve funzionare col **Meta XR Simulator**.
- **Visual separato dalla logica**: ogni prefab ha la logica/collider sulla root e un figlio `Visual` con la mesh (primitive/graybox). Il modellatore sostituirà `Visual` senza toccare script. Dove servirebbe un'animazione (anta, pulsante, ventilatore) cambia lo stato in modo istantaneo ed esponi `UnityEvent` (`OnOpened`, `OnPressed`, `OnTurnedOn`…) a cui il modellatore potrà agganciare le animazioni.
- **Dati**: tuning in ScriptableObject, nessun numero magico. **Eventi**: C# `event`, unsubscribe in `OnDisable`; niente `Find`/`SendMessage` a runtime.
- **Reset**: ogni sistema implementa `IResettable`; il Director li resetta tutti; nessuno stato statico che sopravviva al reset.
- **ActionLog**: ogni azione significativa → `ActionLog.Add(id, isCorrect, dimension, consequenceKey)`.
- Log con prefisso `[CA]`. Errori di configurazione: `Debug.LogError` + fallback sicuro.
- Audio: campo `AudioClip` opzionale per ogni SFX; se vuoto non succede nulla (le clip arriveranno dopo).
- Quest-friendly: niente luci realtime extra, niente post-processing, mesh/materiali condivisi, particelle con tetto.
- Input: solo `OVRInput` / nuovo Input System; mai `UnityEngine.Input` legacy.

## Ciclo di lavoro con l'MCP Unity
1. Scrivi/modifica script → compila → leggi la console; correggi PRIMA di proseguire.
2. Dopo ogni modifica di scena/prefab: assegna TUTTI i riferimenti serializzati e salva.
3. Verifica in Play Mode: nessun NullReference/warning nuovo.
4. Una modifica di scena alla volta; niente operazioni batch giganti.

## Controlli del giocatore vs comandi di debug (due cose distinte)
- **Controlli del giocatore** = controller Meta Quest 3 (trigger, grip, thumbstick, A/B/X/Y), usati anche nel Meta XR Simulator. Provvisori: li deciderà il team; stanno tutti in `ScenarioInputBindings` (nessun altro script legge `OVRInput`).
- **Comandi di debug** = tasti da tastiera / ContextMenu per forzare stati e verificare la logica senza visore. Non fanno parte del gioco e non sostituiscono i controlli del giocatore.
- Nei "Comandi di test" a fine task elenca entrambi: (a) azioni da eseguire con i controller nel Simulator, (b) comandi di debug da tastiera.

## Comandi di test (OBBLIGATORI a fine task)
Il sistema `DebugCommands` (creato in T03) mappa tasti + `ContextMenu` su azioni dello scenario (solo Editor/Development build). Ogni task che aggiunge funzioni aggiunge i suoi comandi. A fine task scrivi in HANDOFF e nella risposta all'utente:
- **Comandi di test**: tasto/menu → cosa fa → risultato atteso (≤10 righe).
- **Test col Meta XR Simulator** (se il task tocca l'interazione): passi da eseguire.
Il tuo test è sempre doppio: tu verifichi via Play Mode/console con i comandi; l'utente verifica col Simulator.

## HANDOFF (obbligatorio a fine task)
Riscrivi "Stato corrente" (≤15 righe) e aggiungi UNA entry di log (≤8 righe) + "Comandi di test" e "Richieste all'utente". Telegrafico.
