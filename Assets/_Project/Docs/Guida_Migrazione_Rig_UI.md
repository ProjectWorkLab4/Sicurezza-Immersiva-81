# Migrazione al Player Rig condiviso e alla UI comune

**Versione 1.0 — 24 novembre 2026**
Per gli owner dei branch di gruppo: `G_Ludovica`, `G_Marius`, `G_Simone`, `G_Tiziano`.
Riferimenti: [SharedPlayerRig.md](../Home/Docs/SharedPlayerRig.md),
[SharedInteraction.md](../Home/Docs/SharedInteraction.md),
[GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md](GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md).

> Questa guida ha due parti:
> **Parte A** è per le persone (in italiano).
> **Parte B** è per l'assistente LLM che esegue la migrazione (in inglese, istruzioni deterministiche).
> La guida V2 resta il riferimento per kit scenario, catalogo missioni e validatore e **non va modificata**.

---

# Parte A — Guida per gli owner dei branch

## 1. Cosa cambia, in breve

| Prima (branch di gruppo) | Dopo (main) |
|---|---|
| Ogni scenario aveva camera, rig, Canvas, EventSystem e input propri | Un solo rig in `SCN_Core`: `MNG_CoreRig` (`SI81.Rig`) |
| Input letto da `OVRInput` o dagli interattori XR Interaction Toolkit | Un solo `InputService` (`SI81.Input`) |
| Interazione con raycast/interattori locali | `Interactable` + `RayInteractor` condivisi (`SI81.Interaction`) |
| UI e HUD duplicati nella scena | `MNG_UI` e `MNG_HUD` vivono in `SCN_Core` |
| Spostamento del giocatore sull'origine del tracciamento della UI | `SpawnPoint` / `RigRegistration.TeleportTo` |
| Build Settings con il solo placeholder | `SCN_Core` indice 0, poi la propria scena |

`SCN_Core` è la scena condivisa che possiede rig, UI e HUD. Le scene di scenario si caricano in
additivo e contengono **solo** ambiente, oggetti, `MNG_Scenario`, `MNG_SpawnPoint` e contenuto.

### Regola d'oro

> **Un solo rig, una sola camera, un solo EventSystem.** Se la scena di scenario contiene uno di
> questi elementi, la migrazione non è finita.

## 2. Prerequisiti

1. Unity **6000.3.14f1**, Meta XR Core SDK **209** e Input System **1.19**, come su `main`.
2. Git è l'unico sistema di versionamento. Non installare o riattivare Unity Version Control /
   Plastic / `com.unity.collab-proxy`.
3. Non eseguire il **Project Setup Tool** per "aggiustare" la scena: riscrive `ProjectSettings/`,
   `Assets/XR/Settings/` e `Assets/Oculus/` in base alla macchina. Se un file di configurazione
   differisce, tieni la versione di `main`.
4. Crea un branch dedicato, per esempio `migration/G_Tiziano`, partendo dal tuo branch di gruppo
   aggiornato con `main`.

```bash
git switch <tuo-branch>
git pull origin <tuo-branch>
git switch -c migration/<gruppo>
git merge origin/main
```

Durante il merge, per `ProjectSettings/`, `Packages/`, `Assets/Oculus/`, `Assets/XR/`,
`Assets/Resources/` e i `.meta` delle cartelle condivise **tieni la versione di `main`**. Se compare
`com.unity.collab-proxy` in `Packages/manifest.json`, il merge è sbagliato: riprendi `Packages/` da
`main`.

Verifica:

```bash
git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json
```

Il comando non deve stampare nulla.

## 3. Passi della migrazione

### 3.1 Togli rig, camera, UI e input locali dalla scena

Apri la scena di scenario e **elimina**:

- `OVRCameraRig`, `XROrigin`, `Camera` e `AudioListener` locali;
- `Canvas`, `EventSystem` e `InputSystemUIInputModule` locali;
- `PauseInput`, `UIPointer`, `XRGrabInteractable`, `XRRayInteractor`, `XRInteractionManager` e
  qualsiasi locomozione locale;
- eventuali asset di azioni locali (`.inputactions` o `UIInputActions`).

La scena deve restare con ambiente, oggetti, `MNG_Scenario`, `MNG_SpawnPoint` e i componenti di
contenuto. Il rig, la UI e la HUD arrivano da `SCN_Core`.

### 3.2 Spawn point

Crea un GameObject `MNG_SpawnPoint` nella scena e aggiungi il componente **`SpawnPoint`**
(`SI81.Rig`). Mettilo dove deve apparire il giocatore e assegna la sua trasformata al campo
`spawnPoint` di `ScenarioController`.

- `SpawnPoint` applica posizione e imbardata quando viene abilitato e ripristina la posa precedente
  quando la scena viene scaricata.
- Non spostare più `UIScreenManager.TrackingOrigin`: la posa del giocatore ha un solo owner.

### 3.3 Input: solo `InputService`

Sostituisci ogni lettura diretta di `OVRInput`, `UnityEngine.Input` o degli interattori XR con
`SI81.Input.InputService`:

| Azione | API |
|---|---|
| Select (grilletto) | `WasSelectPressed()`, `WasSelectPressed(Hand)`, `SelectPerformed`, `SelectCanceled` |
| Grab (impugnatura) | `IsGrabHeld()`, `IsGrabHeld(Hand)`, `GrabPerformed`, `GrabCanceled` |
| Menu / pausa | `WasMenuPressed()`, `MenuPerformed` |
| Back (solo menu) | `WasBackPressed()`, `BackPerformed` |
| Move | `ReadMove()`, `MoveChanged` |
| Turn | `ReadTurn()`, `TurnChanged` |
| Posa aim | `TryGetAimPose(Hand, out Vector3, out Quaternion)` |
| Sospensione mondo | `PushContext(InputChannel.World, owner)` → `InputContextHandle` |

Regole:

- Non creare un asset di azioni locale e non aggiungere un secondo `EventSystem`.
- I tasti sono già mappati: Select = grilletto, Grab = impugnatura, Menu = tasto menu sinistro /
  Tab, Back = B/Y / Esc / Backspace, Move = stick sinistro / WASD, Turn = stick destro / Q-E.
- Durante pausa o menu il canale World è sospeso: non serve un controllo locale di `timeScale`.
- Il gate UI è automatico: se il raggio punta la UI, il Select non arriva al mondo.

### 3.4 Interazione: `Interactable` + `RayInteractor`

Il rig porta un `RayInteractor` per mano. Gli oggetti con cui interagire devono avere il componente
**`Interactable`** (`SI81.Interaction`), sullo stesso GameObject del collider o su un suo figlio.

- `Selectable`, `Grabbable`, `FollowsHand` decidono cosa può fare il raggio.
- Eventi C#: `HoverEntered`, `HoverExited`, `SelectEntered`, `SelectExited`, `GrabEntered`,
  `GrabExited`.
- Non scrivere raycast, hover o grab locali: la presa, il rilascio con velocità e il ripristino del
  `Rigidbody` sono già nel `RayInteractor`.
- Se il tuo scenario ha eventi propri (per esempio `Grabbed`, `Activated`, `Inspected`), crea un
  piccolo adattatore che li inoltra dagli eventi di `Interactable`; non duplicare l'interattore.

### 3.5 Flusso missione e UI

- `MNG_Scenario` ha `ScenarioController` (o una sua sottoclasse) e un `SO_Mission_<Titolo>`.
- La UI comune usa `UIScreenManager`, `HUDService`, `DecisionPanel` e `MissionSession`. Non
  costruire Canvas o HUD nella scena.
- `SceneName` della missione deve coincidere con il nome del file di scena.
- La card in Home diventa disponibile solo quando la scena è **attiva nelle Build Settings** e il
  GUID della scena è quello atteso.

### 3.6 Build Settings

- `Assets/_Project/Home/Level/Scenes/SCN_Core.unity` resta **indice 0** e non va mai rimossa,
  riordinata o sostituita.
- `Assets/_Project/Home/Level/Scenes/Mock/SCN_MockScenario.unity` resta dopo `SCN_Core`.
- Aggiungi **solo la tua scena**, dopo le scene condivise. Non aggiungere scene di altri gruppi.
- Se rinomini o sposti una scena, sposta anche il suo `.meta` per non cambiare il GUID.

### 3.7 Validazione e test

1. Esegui il validatore degli scenari (`SI81/Scenari/...`): non devono restare camera, rig, Canvas,
   EventSystem o moduli di input locali.
2. Apri `SCN_Core` e la tua scena in Play Mode: un solo rig, nessun avviso
   `Nessun IRigUIBridge`, un solo EventSystem.
3. Esegui i test PlayMode del rig (dalla Test Runner o da batchmode):

```powershell
Unity.exe -batchmode -nographics -projectPath <repo> -runTests -testPlatform PlayMode `
  -testFilter SI81.Rig.Tests.RigIntegrationTests -testResults Logs/rig-tests.xml -logFile Logs/rig-tests.log
```

4. Prova sul Quest con la checklist di [SharedPlayerRig.md](../Home/Docs/SharedPlayerRig.md):
   un rig, floor corretto, controller e raggio allineati, stick sinistro, snap turn, Menu in pausa,
   B/Y solo nel menu, trigger che clicca la UI senza selezionare il mondo, aptica.

## 4. Note per gruppo

| Gruppo | Cosa rimuovere | Nota |
|---|---|---|
| `G_Tiziano` | `OVRCameraRig`, `ScenarioInputBindings`, `ScenarioHandInteractor`, letture `OVRInput` | Il caso pilota: sostituire con `InputService` e `Interactable`. |
| `G_Simone` | Componenti XR Interaction Toolkit (`XRGrabInteractable`, `XRRayInteractor`, `XRInteractionManager`, locomozione), Canvas/EventSystem locali | Il rig condiviso sostituisce l'intera pila XRI. |
| `G_Ludovica` | Camera e Canvas locali nella scena Forklift Chaos; duplicati di `MNG_UI`/`MNG_HUD` | Gli asset UI restano in `Home/UI`; la scena non li duplica. |
| `G_Marius` | Rig/input locali, se presenti | Vale la stessa regola: un solo rig in `SCN_Core`. |

## 5. Errori comuni

| Sintomo | Causa | Correzione |
|---|---|---|
| `Secondo RigRegistration rifiutato` | La scena ha ancora un rig locale | Elimina camera/rig/`OVRManager` dalla scena. |
| `Nessun IRigUIBridge registrato` | `MNG_UI` non ha il bridge o è stato disattivato | Usa il prefab `MNG_UI` aggiornato e non duplicarlo. |
| Il trigger seleziona il mondo dietro la UI | Il raggio UI non sta aggiornando il gate | Verifica un solo `InputSystemUIInputModule` e `UIPointer` sul rig. |
| Menu non si apre | Manca `MissionFlowDriver` o l'input Menu non arriva | Usa `MNG_UI` aggiornato; Menu = tasto menu sinistro / Tab. |
| Card "Non disponibile" | Scena non attiva nelle Build Settings o `SceneName` diverso | Aggiungi la scena dopo `SCN_Core`, controlla il GUID. |
| Script mancante nella scena | Resta un componente `CoreRig` o un vecchio interattore | Rimuovi il componente; il hand-off ora è `SI81.UI.Rig.RigUIBridge`. |
| Unity non apre il progetto: `invalid dependencies` / `file:C:/...` | Il tuo `Packages/manifest.json` contiene un pacchetto locale (per esempio un MCP) | Tieni `Packages/manifest.json` e `packages-lock.json` di `main`; non aggiungere percorsi locali. |
| Console: `uso Camera.main` | La UI è partita prima del rig | Assicurati che `SCN_Core` sia caricata e che il bridge sia assegnato. |

## 6. Checklist finale

- [ ] `git grep collab-proxy` non stampa nulla.
- [ ] La scena non contiene camera, `OVRCameraRig`, Canvas, EventSystem o moduli di input locali.
- [ ] `MNG_SpawnPoint` con `SpawnPoint` è presente e assegnato a `ScenarioController`.
- [ ] Nessuna lettura di `OVRInput`, `UnityEngine.Input` o XRI nel codice di scenario.
- [ ] Oggetti interattivi con `Interactable`; nessun raycast/interattore locale.
- [ ] `SCN_Core` è indice 0 delle Build Settings; la propria scena è dopo le scene condivise.
- [ ] Validatore, Play Mode e test del rig passano.
- [ ] Checklist Quest eseguita sul visore.

## 7. Esito del pilot Tiziano

Il pilot è sul branch `pilot/rig-ui-migration-tiziano`, creato da `origin/group_tiziano` e aggiornato
con `integration/testing-ui`. Il merge ha prodotto solo tre conflitti, tutti su file condivisi
(`ProjectSettings/ProjectSettings.asset`, `Assets/XR/Settings/OpenXR Package Settings.asset`,
`Guida_Design-Interactions.md.meta`), risolti tenendo `main`.

Il pilot ha trovato un blocco reale: il branch Tiziano conteneva un pacchetto locale
`com.coplaydev.unity-mcp` con percorso `file:C:/Unity-Projects/...`, che impediva a Unity di aprire il
progetto. Va rimosso tenendo `Packages/manifest.json` di `main`.

La compilazione del branch pilota è passata (exit 0) e la validazione di `SCN_Core` ha trovato
1 `RigRegistration`, 1 camera attiva, 1 `EventSystem`, 2 `UIPointer` e il `RigUIBridge` con tutti i
riferimenti. La scena `SCN_ChemicalAlert` è stata migrata: 0 rig/camera/EventSystem locali,
radice OVR convertita in `MNG_Scenario`, `ScenarioController` + `MNG_SpawnPoint`, 27
`Interactable`, 3 zone PPE ricreate con `PPEZoneRigAnchor`, missione boilerplate `Id: 05`.

Errori trovati e corretti nel pilot:

- le zone PPE (`Head`, `Torso`, `Hands`) erano figlie della radice `OVRCameraRig` e sono state
  perse eliminando il rig; vanno ricreate e riagganciate al rig condiviso;
- `Id: 08` era già usato da un'altra missione: usare un id libero;
- `PlayerPPEState.zones` e `PPEItemObject.equipZones` puntavano alle zone eliminate e vanno
  riscritti;
- il tasto Menu scelto per l'Hint apre anche la pausa (`MissionFlowDriver`): serve una decisione
  di gameplay.

Dettaglio completo, build APK ed esito: `Assets/_Project/Groups/G_Tiziano/Docs/RIG_UI_PILOT.md`
sul branch del pilot (PR #22).

---

# Parte B — Istruzioni per l'assistente LLM

**Scope:** migrate one group branch to the shared Player Rig and common UI already merged in `main`.
Do not redesign gameplay. Do not modify `GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md` or `SharedPlayerRig.md`
unless explicitly asked.

## B.1 Invariants

1. `SCN_Core` is the only scene that owns the player rig, camera, UI, HUD and EventSystem.
2. A scenario scene contains only environment, content, `MNG_Scenario`, `MNG_SpawnPoint`.
3. `SI81.Input.InputService` is the only input owner. No `OVRInput`, no local action asset.
4. `SI81.Interaction.Interactable` + the rig's `RayInteractor` are the only interaction path.
5. `SI81.Rig` never references `SI81.UI`; the adapter is `SI81.UI.Rig.RigUIBridge`.
6. Keep `ProjectSettings/`, `Packages/`, `Assets/Oculus/`, `Assets/XR/`, `Assets/Resources/` and
   shared folder `.meta` files from `main`.
7. `SCN_Core` remains Build Settings index 0. Never renumber it.

## B.2 Procedure

1. **Merge**
   ```bash
   git switch <group-branch>
   git pull origin <group-branch>
   git switch -c migration/<group>
   git merge origin/main
   ```
   Resolve conflicts by taking `main` for the paths in B.1.6. Then:
   ```bash
   git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json
   grep -n '"file:' Packages/manifest.json
   ```
   Both must print nothing (no Unity VCS, no local `file:` package paths such as a local MCP).
   Do not run Unity Project Setup Tool.

2. **Strip local scene infrastructure.** Open the scenario scene and delete GameObjects/components:
   `OVRCameraRig`, `OVRManager`, `XROrigin`, `Camera`, `AudioListener`, `Canvas`, `EventSystem`,
   `InputSystemUIInputModule`, `TrackedDeviceRaycaster`, `XRInteractionManager`,
   `XRGrabInteractable`, `XRRayInteractor`, locomotion providers, `PauseInput`, `UIPointer`,
   local `ScenarioHandInteractor`-style components, local `.inputactions`.

3. **Spawn point.** Create `MNG_SpawnPoint` with `SI81.Rig.SpawnPoint`. Set its world position and
   yaw. Assign it to `ScenarioController.spawnPoint`. Do not move
   `UIScreenManager.TrackingOrigin` from scenario code.

4. **Input.** Replace direct input reads with `InputService`:
   - polling: `WasSelectPressed(Hand)`, `IsGrabHeld(Hand)`, `WasMenuPressed()`,
     `WasBackPressed()`, `ReadMove()`, `ReadTurn()`;
   - events: `SelectPerformed`, `SelectCanceled`, `GrabPerformed`, `GrabCanceled`,
     `MenuPerformed`, `BackPerformed`, `MoveChanged`, `TurnChanged`;
   - world suppression: `PushContext(InputChannel.World, owner)` and dispose the handle;
   - aim: `TryGetAimPose(Hand, out Vector3, out Quaternion)`.
   Never instantiate `InputAction`, `InputActionAsset` or `InputSystemUIInputModule` in scenario code.

5. **Interaction.** Add `Interactable` to every object that must be hovered, selected or grabbed.
   Set `Selectable`, `Grabbable`, `FollowsHand`. Consume `HoverEntered`, `HoverExited`,
   `SelectEntered`, `SelectExited`, `GrabEntered`, `GrabExited`. Delete local raycast/hover/grab
   code. If scenario-specific events are required, write a small adapter component that forwards
   `Interactable` events; do not duplicate the interactor.

6. **Mission/UI.** Keep `MNG_Scenario` with `ScenarioController` (or a subclass) and one
   `SO_Mission_<Title>`. `SceneName` must equal the scene file name. Use `HUDService`,
   `DecisionPanel` and `MissionSession`; do not create Canvas or HUD.

7. **Build Settings.** Ensure
   `Assets/_Project/Home/Level/Scenes/SCN_Core.unity` is index 0 and
   `Assets/_Project/Home/Level/Scenes/Mock/SCN_MockScenario.unity` follows. Add only the group's
   scene after them, with its `.meta` GUID unchanged.

8. **Validate.** Run the scenario validator, then:
   ```powershell
   Unity.exe -batchmode -nographics -projectPath <repo> -runTests -testPlatform PlayMode `
     -testFilter SI81.Rig.Tests.RigIntegrationTests -testResults Logs/rig-tests.xml -logFile Logs/rig-tests.log
   ```
   Open `SCN_Core` + scenario in Play Mode: exactly one `RigRegistration`, one camera, one
   `EventSystem`, no `Nessun IRigUIBridge` warning.

## B.3 Forbidden

- Editing `ProjectSettings/EditorBuildSettings.asset` to remove/reorder `SCN_Core`.
- Adding `com.unity.collab-proxy` or opening Unity Version Control.
- Running Meta XR Project Setup Tool "Fix All".
- Adding a camera, rig, Canvas, EventSystem, input module, local action asset, local raycast or
  local grab implementation to a scenario scene.
- Editing `GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md`, `SharedPlayerRig.md` or files under
  `Assets/_Project/Shared/` as part of a group migration.

## B.4 Acceptance

- `git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json` → empty.
- `git grep -n -E "OVRInput|XRGrabInteractable|XRRayInteractor|XRInteractionManager" -- <group Code>` → empty.
- Build Settings: `SCN_Core` index 0, group scene after shared scenes.
- Unity batchmode compile: no `error CS`.
- PlayMode rig tests pass; Editor smoke has one rig, one EventSystem, UI hand-off working.
- Quest checklist from `SharedPlayerRig.md` executed by the owner.

## B.5 Scenario-kit details

For scenario creation, validation, mission catalog, `SO_Mission_*`, `SceneName` and the
`ScenarioController` API, follow [GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md](GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md).
That guide is read-only. This document only defines the rig/UI/input migration on top of it.
