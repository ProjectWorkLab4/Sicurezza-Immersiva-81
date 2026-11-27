# EVACUATION — Manuale comandi di test e verifica

Comandi di debug: `EvacDebugCommands` (`Editor/Development`), **ContextMenu sempre presente** per ogni comando oltre al tasto.
⚠ Il Meta XR Simulator ruba il focus della Game View: i tasti funzionano solo con focus sulla Game View. Evitare `0–9` e `W/A/S/D` (usati dal Simulator).
Tutti i comandi scrivono su console con prefisso `[EV]`. Nessun comando modifica Chemical Alert.

## 1. Controlli del giocatore (Meta XR Simulator, Quest 3)

| Azione | Controllo | Risultato atteso |
|---|---|---|
| Afferrare / rilasciare | Grip | `ScenarioInteractable.Grabbed` / `Released` via `EvacInteractableBridge` |
| Attivare porta / pulsante / NPC | Trigger con la mano nell'area (~1 m per NPC) | Porta si apre/chiude istantaneo (`OnOpened`/`OnClosed`), ascensore → esito, NPC entra in fila |
| Ispezionare piano | Trigger sull'area del piano **oppure** sguardo fermo 0.75 s entro 1.2 m | `[EV] Plan inspected: …`, `evac.lg01` una sola volta |
| Muoversi | Locomozione del rig condiviso da `SCN_Core` (nessuna locomozione locale) | Attraversamento porte U1/U2 e porta scale emergenza senza compenetrazioni |
| Contatto fuoco (Crisis) | Entrare nel collider del focolaio, cooldown 2 s | `FireContact(n,intensity)`, `evac.lg07`/`lg08` |

Binding provvisori: `ScenarioInputBindings` (riusato da CA). Nessun altro script legge `OVRInput`.

## 2. Tutti i comandi di debug

| Tasto | Menu `ContextMenu` | Cosa fa / risultato atteso |
|---|---|---|
| `H` | `Evac/Help (H)` | Stampa l'elenco dei comandi registrati |
| `N` | `Evac/Advance Phase (N)` | Briefing → Action → fine run |
| `M` | `Evac/Cycle Difficulty (M)` | Guided → Operational → Crisis, con reset completo |
| `F` | `Evac/Force Timeout (F)` | Timeout: `TimeUp` + `evac.lg10` (ignorato in Guided) |
| `R` | `Evac/Reset (R)` | `[EV] Reset: …`, rig a `SPAWN_Player` |
| `X` | `Evac/Reset x3 (X)` | 3 reset consecutivi, nessun residuo |
| `P` | `Evac/Print State (P)` | `[EV] state: phase=… hearts=… plan=… npc=… exit=… seed=…` |
| `K` | `Evac/Print Result (K)` | `[EV] RESULT … scores S/A/D/E/T …` |
| `T` | `Evac/Teleport Next Marker (T)` | Teletrasporto ciclico ai marker |
| `O` | `Evac/Run SelfCheck (O)` | `[EV] E02 SelfCheck evaluator: N/N PASS` + check configurazioni |
| `Y` | `Evac/Run Smoke Test (Y)` | `[EV] SmokeTest 1/49 … PASS` ×49 → `SmokeTest summary 49/49 ALL PASS` |
| `F8` | `Evac/Fix Seed (F8)` | Fissa il seed al valore dell'Inspector |
| `F9` | `Evac/Randomize Seed (F9)` | Nuovo seed casuale |
| `L` | `Evac/Toggle Alarm (L)` | `[EV] Alarm …`; forza on/off (il cambio fase riprende il controllo) |
| `I` | `Evac/Inspect Next Plan (I)` | Simula hover+trigger sul piano successivo → `evac.lg01` |
| `G` | `Evac/Open All Doors (G)` | Tutte le porte aperte, collider di blocco disattivati |
| `C` | `Evac/Close All Doors (C)` | Tutte le porte chiuse |
| `J` | `Evac/Go To Next Test Spot (J)` | Rig davanti al piano/porta successivi, rivolto verso di essi |
| `E` | `Evac/Simulate Elevator Button (E)` | `evac.lg06`; Failure (Guided: Partial + `DangerLevelChanged(Down)`) |
| `Q` | `Evac/Simulate Emergency Stairs (Q)` | Teletrasporto in `ZONE_StairsEmergency` → esito da valutatore |
| `V` | `Evac/Simulate Internal Stairs (V)` | Teletrasporto in `ZONE_StairsInternal` → Partial + `evac.lg11` |
| `U` | `Evac/Collect Next NPC (U)` | Simula attivazione mano sul successivo NPC Idle |
| `Z` | `Evac/Collect All NPCs (Z)` | Raccoglie tutti i 5 NPC |
| `F7` | `Evac/Print NPC Trail (F7)` | `[EV] NPC trail: crumbs …, line …` |
| `B` | `Evac/Request Hint (B)` | Prossima hint ordinata (solo Guided) → `HintRequested(key)` |
| `F10` | `Evac/Print Level Behavior (F10)` | `[EV] Level …: time=… hearts=… highlights=… hints=… reduced=… fire=…` |
| `F3` | `Evac/Force Fire Contact (F3)` | Un contatto fuoco (solo Crisis) → `FireContact` / `DamageTaken` |
| `F4` | `Evac/Print Fire Slots (F4)` | `[EV] Fire: … active=[…] rejected=… reduced=…` |
| `F5` | `Evac/Validate Fire Lanes (F5)` | `[EV] Fire lane: 200/200 seed validi, 0 scartati.` |
| `F6` | `Evac/Toggle Fire Gizmos (F6)` | Mostra/nascondi i gizmo dei focolai attivi |
| `F11` | `Evac/Run 5 Varied Replay Games (F11)` | `[EV] E10 replay games: 5/5 PASS` |
| `F12` | `Evac/10 Clean Resets (F12)` | `[EV] E10 reset stress: 10/10 PASS` |

Solo menu (senza tasto):

| Menu | Risultato atteso |
|---|---|
| `Evac/Collect NPC By Number` | Raccoglie l'NPC con indice 1-based impostato nell'Inspector |
| `Evac/Print Replay Layout` | `[EV] Replay layout: seed=… fixed=… logged=… npc=[…] fire=[…]` |
| `Evac/Set Difficulty Guided` / `Operational` / `Crisis` | Cambia livello con reset completo |
| `Evac/Run Evaluator SelfCheck` (`EvacSelfCheckRunner`) | `[EV] E02 SelfCheck TOTAL: ALL PASS` |

## 3. Procedura di verifica

**A. Verifica rapida (ogni modifica)**
1. Aprire `SCN_Evacuation`, entrare in Play Mode.
2. `O` → SelfCheck e config validi.
3. `Y` / `Evac/Run Smoke Test` → `49/49 ALL PASS`.
4. `F5` → `200/200 seed validi, 0 scartati`.
5. `R`, `X`, `F12` → reset puliti, 0 errori/warning `[EV]` nuovi in console.

**B. Verifica per livello**
1. `M` (o menu `Set Difficulty …`) cicla Guided → Operational → Crisis; `F10` stampa il comportamento atteso: cronometro/180 s/120 s, 0/3/1 cuori, aiuti solo in Guided, fuoco solo in Crisis.
2. Guided: `E` → `Partial` + pericolo Down (non sconfitta); `F` → nessun `TimeUp`; `B` → 5 hint ordinate.
3. Operational: `I` → LG01; `Z` → LG03+LG04; `E` → `Failure` + LG06; `F` → `Failure` + LG10; `Q` con 5/5 → `Success`.
4. Crisis: `F4` → 5 focolai lontani da muri/scrivanie; `F3` ×3 → `Died` + LG07/LG07/LG08; `Q` → esito; `F5` → 0 scartati.

**C. Verifica replay/reset**
1. `F8` con seed 4242 + 2 reset → stessa disposizione NPC/fuoco (`Evac/Print Replay Layout`).
2. `F9` → disposizione diversa; `F11` → 5 finali differenti senza residui; `F12` → 10 reset senza doppie sottoscrizioni.

**D. Verifica col Simulator (utente)**
1. Play → passa dalle porte U1/U2 e dalla porta scale emergenza senza compenetrazioni.
2. Trigger sulle 3 piante e sugli NPC: fila indiana dietro il giocatore, ordine di raccolta, distanza ~0.8–1 m.
3. Provare sguardo fermo 0.75 s su un piano: ispezione senza trigger.
4. Premere pulsante ascensore e avvicinarsi al fuoco in Crisis: conseguenze leggibili via eventi/stato (la UI non è in questo gruppo).
5. `R` ripristina; controllare che VFX fuoco/fumo si spengano.

## 4. Cosa NON è presente qui

Nessun Canvas, pannello, sottotitolo, vignetta, schermata di esito, animazione o modello audio definitivo: questi sono affidati ad altri team.
La UI futura si aggancia a `ResultReady`, `ActionRecorded`, `HintRequested`, `DangerLevelChanged`, `FireContact`, `DamageTaken` e allo stato pubblico del Director.
