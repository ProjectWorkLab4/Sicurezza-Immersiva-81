# SI81 common UI: instructions for AI assistants

You are helping a team of the **Sicurezza Immersiva 81** project (Unity VR, Meta Quest 3) connect **their scenario** to the **common UI**. Read this whole file before answering. The human-readable guide is `Assets/_Project/Docs/GUIDA_INTEGRAZIONE_SCENARIO.md` (+ `.pdf`): cite its sections (§1–§10) when you explain something, so the team can check.

## 0. How to answer

- **Answer in Italian**, in simple, short sentences. All text the player sees is Italian.
- Before giving steps, find out which case you are in (§3). If something essential is missing, ask: team/group folder (`G_Ludovica`, `G_Marius`, `G_Simone`, `G_Tiziano`), scenario title and number (01–12), whether a scene already exists, what the scenario must do.
- Give **concrete Unity steps** (menu paths, Inspector fields, object names) and complete C# files. Never give partial snippets that need guessing.
- Never invent API names. Use only what is listed in §5. If something is not there, say so and follow §7.
- When you are not sure what the project contains, ask the team to check in Unity (or to paste the validator output) instead of guessing.

## 1. Facts about the project (do not contradict them)

| Item | Value |
| --- | --- |
| Unity | 6000.3.14f1, URP 17.3.0 |
| XR | OpenXR 1.16.1 + Meta XR Core SDK 209.0.0, XR Management 4.7.0 |
| Input | Input System 1.19.0 only. **No XR Interaction Toolkit** (not approved) |
| Device | Meta Quest 3, **controllers only** (no hand tracking) |
| UI tech | uGUI + TextMeshPro, world-space canvases, all inside the common UI |
| Common UI code | `Assets/_Project/Home/` (code in `Code/Scripts/`, prefabs in `UI/Prefabs/`, data in `UI/Data/`, scenes in `Level/Scenes/`; scenario template in `Assets/_Project/Shared/Level/ScenarioTemplate/`), assembly `SI81.UI`, namespace `SI81.UI` |
| Owner of the common UI | **Team Ludovica** (contact: Romario Gjoka) |
| New packages | **Forbidden** unless the whole team approves |

## 2. Architecture in one minute

- **`SCN_Core`** is always loaded (Build Settings index 0). It contains the **only** player rig (`MNG_CoreRig` → `OVRCameraRig`, controller models, UI rays), `MNG_UI` (menus, EventSystem, services) and `MNG_HUD` (wrist panel, alerts, subtitles, world labels, decision and outcome panels).
- A **scenario scene** `SCN_<Title>` is loaded **additively** on top of `SCN_Core` from Home → Mission Brief → Avvia. It contains only: environment, objects, `MNG_Scenario` (the team's script, derived from `ScenarioController`), `MNG_SpawnPoint`, `LabelAnchor` components.
- The scenario is described by data: **`SO_Mission_<Title>`** (title, context, objectives, criteria, levels, icon, scene name) listed in **`SO_MissionCatalog`**. Home, Mission Brief, HUD, Pause, Debrief and Score & Replay then work automatically.
- Flow: Home → Mission Brief → scenario → `EndMission(...)` → outcome (3–5 s) → Debrief → Score & Replay → Home.
- Pressing **Play directly in the scenario scene** (Editor only) loads `SCN_Core` automatically and starts at the `editorLevel` (Guided).

## 3. Identify the case, then follow its recipe

### Case A · New scenario from zero
1. Menu `SI81/Scenari/Nuovo scenario...`: Title (English letters, digits, spaces), Number 01–12 (free in the catalog), Group folder. It creates `Assets/_Project/Groups/<group>/Scenari/<Title>/SCN_<Title>.unity` + `SO_Mission_<Title>.asset`, adds the mission to the catalog and the scene to the Build Settings.
2. If the number/title is taken by one of the 8 **sample missions** (Id 01–04, 07–10), first remove that sample from `SO_MissionCatalog` → `Missions` list, then run the wizard (guide §3 step 1, §9).
3. Fill `SO_Mission_<Title>` (rules in §6), build the environment, add `LabelAnchor`s, write the script (§4), call `EndMission` once, validate, test.

### Case B · The team already has a scene
Follow guide §3 "Hai già iniziato una scena? Portala dentro così". In short:
1. Commit the scene as it is; pull the latest project.
2. Remove from it: cameras, `AudioListener`, `OVRCameraRig`, XR Origin, Meta Building Blocks, `Canvas`, `EventSystem`, input modules, XR Interaction Toolkit components. Note where the player stood.
3. **Method A (recommended):** run `Nuovo scenario...`, open old and new scene together, drag the environment roots into the new scene (move objects that reference each other together), delete the template's `MESH_Floor` / `MESH_Example*`, place `MNG_SpawnPoint`, **redo lighting** in the new scene (skybox, ambient, lightmap bake belong to the scene), remove the old scene from Build Settings.
4. **Method B (huge scene / long bake):** rename the scene to `SCN_<Title>` inside Unity and move it to the group's `Scenari/<Title>/`; copy `MNG_Scenario` + `MNG_SpawnPoint` from `SCN_ScenarioTemplate` (close the template without saving); duplicate `SO_Mission_Template` → `SO_Mission_<Title>` and fill it; set `Mission` and `Spawn Point` on `MNG_Scenario`; add the mission to `SO_MissionCatalog`; add the scene to Build Settings after `SCN_Core`.
5. Convert their own UI: labels on objects → `LabelAnchor`; messages → `Alert` / `Feedback` / `Subtitle`; choices → `AskDecision`; end → `EndMission`.
6. If their interactions (grab, teleport) were attached to their own rig: they cannot keep the rig. Tell them to ask the VR Interaction System team how to attach them to the shared rig.

### Case C · Writing or fixing the scenario script
Write one class deriving from `ScenarioController` (template in §4), put it on `MNG_Scenario` **instead of** the base component, re-assign `Mission` and `Spawn Point`. Game logic stays in the team's own scripts; they call the controller.

### Case D · Validator errors
Ask them to run `SI81/Scenari/Valida scenario` (scene open) or `Valida tutti gli scenari` and paste the messages. Every message starts with `[asset/scene name]` and says how to fix it. Errors must be 0; warnings are advice. Sample missions without a scene only give warnings.

### Case E · On the Quest the UI does not respond
1. Check that the APK was built **after** pulling the latest version.
2. During the Android build the Unity Console must show a line starting with `[SI81 Build]` (a build step that keeps the hand-tracking and "Detached" controller OpenXR profiles off; with them on, the Quest 3 controller bindings are rejected and Unity gets no ray and no trigger).
3. Start recording logs in Meta Quest Developer Hub **before** opening the app; point at a button and press the **trigger** with each hand.
4. In the log, the lines `[SI81 Input]` show: UI version, input settings, controllers, and for each trigger press the controller, the ray pose and what it hit (`colpito=...` or `NIENTE`). `XR_ERROR_PATH_UNSUPPORTED` means an OpenXR profile is still on that should be off. Send these findings to Team Ludovica; do not change the common UI.

### Case F · Training scene
`SO_Mission_Training` (Id 00) exists; `SCN_Training` is created by the team that owns the training. Do **not** use `Nuovo scenario...` (it only accepts 01–12): duplicate `SCN_ScenarioTemplate` into that group's `Scenari/Training/`, rename it `SCN_Training`, add it to Build Settings after `SCN_Core`, assign `SO_Mission_Training` to `MNG_Scenario.Mission`.

## 4. Script template (complete, compiles)

```csharp
using System.Collections.Generic;
using SI81.UI;

/// <summary>
/// Logica dello scenario Fire Starter: obiettivi, avvisi, decisione e fine missione.
/// Va su MNG_Scenario al posto del ScenarioController di base.
/// </summary>
public class FireStarterScenario : ScenarioController
{
    /// <summary>Sessione, HUD e primo obiettivo pronti: qui parte la logica.</summary>
    protected override void OnScenarioStarted()
    {
        SetPhase(PHASE_SCAN);
        Subtitle("Caposquadra", "Controlla il quadro elettrico.");
        SetHint("Guarda il quadro a destra");
    }

    /// <summary>Chiamato dalla logica di gioco (o da un UnityEvent) quando il giocatore dà l'allarme.</summary>
    public void OnAlarmGiven()
    {
        Feedback("Allarme dato", FeedbackKind.Positive);
        CompleteObjective();
        SetPhase(PHASE_DECISION);
        AskDecision("Il fumo esce dalla ciabatta. Cosa fai?",
            new[] { "Stacco la spina", "Uso l'acqua" }, OnDecision, 20f);
    }

    /// <summary>Risposta alla decisione: indice dell'opzione, -1 se il tempo scade.</summary>
    private void OnDecision(int choice)
    {
        bool good = choice == 0;
        EndMission(good ? OutcomeType.Positive : OutcomeType.Negative, -1,
            good ? new[] { 90, 85, 90, 80, 85 } : new[] { 40, 50, 20, 30, 40 },
            new List<string>
            {
                good ? DebriefLinePrefix.GOOD + "Hai staccato la spina → nessun rischio elettrico"
                     : DebriefLinePrefix.CRITICAL + "Hai usato l'acqua → scossa elettrica"
            });
    }
}
```

Code rules: PascalCase for classes, methods, public members; camelCase for private; UPPERCASE for constants; a `/// <summary>` **in Italian** on every function; no prefix on class names.

## 5. API reference (only these exist)

### `ScenarioController` (base class of the team's script)
| Member | Use |
| --- | --- |
| `protected virtual void OnScenarioStarted()` | override: start the scenario logic here |
| `protected virtual void OnTimerExpired()` | default ends with Negative, score 20, line "- Tempo scaduto..." |
| `SetObjective(int index)` / `CompleteObjective()` | 0-based objective from `SO_Mission.Objectives`; wrist shows "n/N" |
| `SetPhase(string key)` | `PHASE_SCAN`, `PHASE_DECISION`, `PHASE_ACTION`, `PHASE_RESOLUTION` |
| `SetHint(string text)` | hint, Guided only, ~60 characters |
| `Alert(string text, string iconName = null)` | event alert, 3 s, sound + haptic; "Title\nDetail"; icon e.g. `"ICN_Warning_Filled"` |
| `Feedback(string text, FeedbackKind kind)` | `Positive` / `Neutral` / `Negative`; Guided always, Operational only Negative, Crisis never |
| `Subtitle(string speaker, string text, float seconds = 0f)` | max 32 chars × 2 lines, long text is paged |
| `bool AskDecision(string question, string[] options, Action<int> onChosen, float timeoutSeconds = 0f)` | panel at 1.2 m, 2–4 options (~34 chars); callback index or `-1` on timeout |
| `EndMission(OutcomeType outcome, int safetyScore, int[] dimensions, List<string> debriefLines)` | **once**; pass `-1` as score (average of the 5 dimensions: Safety, Awareness, Decision, Execution, Time, 0–100) |
| Properties | `Mission`, `Level` (`DifficultyLevel.Guided/Operational/Crisis`), `IsRunning`, `CurrentObjectiveIndex`, `ObjectivesCompleted`, `ObjectiveCount`, `ElapsedSeconds` |

Enums: `OutcomeType { Positive, Partial, Negative }`, `FeedbackKind { Positive, Neutral, Negative }`. Debrief prefixes: `DebriefLinePrefix.GOOD` `"+ "`, `BAD` `"- "`, `CRITICAL` `"! "`. Write debrief lines as `action → consequence`.

### `HUDService` (static, only if the script does not derive from `ScenarioController`)
`SetObjective(text)`, `SetObjectiveIndex(i)`, `SetObjectiveProgress(current, total)`, `SetPhase(key)`, `SetHint(text)`, `ShowEventAlert(text, icon)`, `ShowActionFeedback(text, kind)`, `ShowSubtitle(speaker, text, seconds)`, `ShowSoundEvent(text, critical)`, `ClearSubtitle()`, `Pulse(amplitude, duration, nonDominantHand)`. The timer starts by itself: do not call `StartTimer`.

### `LabelAnchor` (component on scene objects)
`Kind` = `Risk` (Guided + Operational, max 3 visible), `Interaction` (all levels), `ContextHelp` (Guided). Fields: `Title`, `Detail` (Italian text) or `Title Key` / `Detail Key` (keys of `SO_UIStrings_It`, must exist), `Offset` (default 0, 0.3, 0), `Icon`, `Button Key` / `Action Key` for Interaction. Shown automatically between 0.75 and 4 m. Set `Suppressed = true` while the object is grabbed.

### `ScenarioTrigger` (component)
Fires its `onEntered` UnityEvent when the player's head comes within `radius` (default 1.5 m); `once` = only the first time. Use it to call public methods of the scenario script without physics.

### Not available yet
`RecordChoice(...)` for the Debrief choice timeline is **planned, not implemented**. Never call it. Prepare by storing `ElapsedSeconds`, action (≤ 60 chars, "Hai ...") and consequence (≤ 40 chars) for at most 8 choices, and keep passing debrief lines to `EndMission` (guide §10).

## 6. `SO_Mission_<Title>` rules

| Field | Rule |
| --- | --- |
| `Id` | 2 digits 01–12, unique (00 = Training, 99 = mock: reserved) |
| `Title` | e.g. `Fire Starter` (shown as "01 · Fire Starter") |
| `Context` | role and situation, ≤ 280 characters |
| `Objectives` | 1–4 lines, ≤ 30 characters each |
| `Criteria` | one short line each for Positive, Partial, Negative |
| `Levels` | unlocked + `TimeLimitSecondsOverride` (0 = level default; keep 0 in Guided) |
| `Complexity` | 1–5 |
| `Icon` | white `ICN_` icon (`Image` optional, `Gradient Top/Bottom` colours) |
| `SceneName` | exactly `SCN_<Title>` in PascalCase |

Levels: Guided (hints, risk labels, context help, elapsed time), Operational (risk labels, countdown 600 s), Crisis (nothing, countdown 300 s). The UI decides what to show; read `Level` only for game logic.

## 7. Adding components

### 7.1 What a team (not Team Ludovica) may add, in its own group folder
Allowed, inside `Assets/_Project/Groups/<group>/`:
- **Scenario logic components** (MonoBehaviours) that drive the existing UI: e.g. a valve that calls `CompleteObjective()` when turned, a sensor that calls `Alert(...)`, a component that toggles `LabelAnchor.Suppressed` when grabbed.
- **3D, non-UI objects**: meshes, materials, VFX, sounds, animated indicators, signs and screens built as 3D models with textures.
- Prefabs and Prefab Variants of their own objects.

Rules for these components: names with SI81 prefixes (`MESH_`, `MAT_`, `VFX_`, `SFX_`, `MNG_`...), Italian `/// <summary>`, Italian player text kept in the team's script or `SO_Mission`, **no `Canvas`**, no camera, no input module, no XR Interaction Toolkit, no new packages, no edits to files of the common UI.

Template for a logic component:

```csharp
using SI81.UI;
using UnityEngine;

/// <summary>
/// Valvola del gas: quando il giocatore la chiude completa l'obiettivo corrente e lo segnala sul polso.
/// </summary>
public class GasValve : MonoBehaviour
{
    [Tooltip("Script dello scenario su MNG_Scenario.")]
    [SerializeField] private ScenarioController scenario;

    // true dopo la prima chiusura (evita doppi completamenti).
    private bool closed;

    /// <summary>Da chiamare quando la valvola è stata chiusa (animazione, interazione o UnityEvent).</summary>
    public void Close()
    {
        if (closed || scenario == null || !scenario.IsRunning) return;
        closed = true;
        scenario.Feedback("Gas chiuso", FeedbackKind.Positive);
        scenario.CompleteObjective();
    }
}
```

### 7.2 When something really needs new UI
If the scenario needs a UI element that does not exist (a new panel, gauge, widget, HUD element, screen, icon or text style), **do not build it in the scenario** (the validator rejects any Canvas there and it would be a local variant of a common system). Write a request for Team Ludovica instead and give it to the team to send:

```
Richiesta componente UI comune · Team <nome> · Scenario <NN · Titolo>
Cosa serve: <descrizione in una riga>
Quando compare: <evento / livello / per quanto tempo>
Contenuto: <testi, numeri, icone>
Dove: <polso / basso nella vista / sull'oggetto / pannello davanti>
Perché quelli esistenti non bastano: <Alert, Feedback, Subtitle, AskDecision, LabelAnchor>
Scadenza: <data>
```

Before writing a request, check whether an existing tool already covers it: most needs are an `Alert`, `Subtitle`, `AskDecision` or a `LabelAnchor`.

### 7.3 Only for Team Ludovica's assistant: adding a common component
The common UI is generated by Editor scripts; it is never edited by hand.
1. Read the project `CLAUDE.md` (rules, naming, Definition of Done) and `Assets/_Project/Home/Docs/FIGMA_SPECS.md` (sizes, states).
2. Colours, sizes, durations go in `Assets/_Project/Home/Code/Scripts/Editor/Source/tokens_figma.json`; Italian texts in `Editor/Source/strings_it.json` (keys like `ui.button.start_mission`, `hud.alert.fire_spreading`). Run `SI81/UI/Build Theme`.
3. Runtime script in `Runtime/` (namespace `SI81.UI`), reading only `SO_UITheme` and `SO_UIStrings_It`; builder code in `Editor/` (namespace `SI81.UI.EditorTools`) that creates or updates the prefab and is safe to re-run. Never edit prefab or scene YAML by hand.
4. Run the matching builder (`SI81/UI/Build Components`, `Build Screens`, `Build HUD`, `Build Base`), then `SI81/Scenari/Crea kit scenario` if `SCN_Core` changes.
5. No `Shadow`/`Outline` components, no Animator on UI, no Layout Groups active at runtime on in-scenario UI; decorative graphics `raycastTarget = false`.
6. Test in `SCN_UISandbox` and in Play Mode from `SCN_Core`, Console clean, then add a `CHANGELOG.md` entry (version, owner, 3-line usage note) and update the integration guide.

## 8. Never do this

- Add a camera, `AudioListener`, `OVRCameraRig`, XR Origin or Building Block to a scenario scene (two rigs: UI fixed in front of the eyes, invisible controllers, no ray).
- Add `Canvas`, `EventSystem`, input modules, `HUD_*`, `PANEL_*`, `CANVAS_*`, `MNG_UI`, `MNG_HUD` to a scenario scene.
- Use XR Interaction Toolkit or `TrackedDeviceRaycaster`; add packages.
- Put panels fixed in front of the eyes or attached to the head; put text in the centre of view or above +10°.
- Call `IProgressService.RecordResult` (already done by `MissionSession.End`); call `EndMission` more than once.
- Show the score with another formula: on screen it is always "SPI N/100", written by the common UI.
- Modify, copy or make local variants of common UI prefabs, scripts, theme or strings (other teams).
- Rename `SCN_Core` or move it from Build Settings index 0.
- Run `SI81/UI/Build...` or `SI81/Scenari/Crea kit scenario` (other teams: they regenerate shared files).
- Move or rename assets from the file explorer, or without their `.meta` files.

## 9. Controller buttons used by the UI

Trigger (index finger) = select on the UI ray, both hands. B (right) / Y (left) = back, only while the menu is open. Left menu button (≡) = Pause during a mission. The grip does nothing in the UI. Do not map scenario actions to these buttons in a way that conflicts.

## 10. Final checklist to give the team

- [ ] Created with `Nuovo scenario...` (or Case B / F); no `[DA COMPILARE]` left.
- [ ] Unique `Id` 01–12, `SceneName` = `SCN_<Title>`, scene in Build Settings after `SCN_Core`.
- [ ] No camera, rig, Canvas or UI in the scene; `MNG_Scenario` with the team's class and the right mission.
- [ ] `LabelAnchor` on key objects, max 3 Risk together.
- [ ] `EndMission` once on every path (timeout included), score `-1`, 5 dimensions, prefixed debrief lines.
- [ ] Italian texts; names with SI81 prefixes; Italian `/// <summary>`.
- [ ] `Valida scenario` = 0 errors; tested in Guided, Operational, Crisis and from `SCN_Core` to Home and back; tested on the Quest.
- [ ] Shared files (the catalog) changed only after pulling the latest version.
