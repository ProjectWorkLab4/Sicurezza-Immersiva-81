# LLM instructions — Player Rig and common UI migration

**Agent-facing document.** The human guide is
[Guides/Guida_Migrazione_Rig_UI.md](../../../Guides/Guida_Migrazione_Rig_UI.md).
**Scope:** migrate one group branch to the shared Player Rig and common UI already merged in `main`.
Do not redesign gameplay. Do not modify `GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md` or
[SharedPlayerRig.md](../Home/Docs/SharedPlayerRig.md) unless explicitly asked.

## B.1 Invariants

1. `SCN_Core` is the only scene that owns the player rig, camera, UI, HUD and EventSystem.
2. A scenario scene contains only environment, content, `MNG_Scenario`, `MNG_SpawnPoint`.
3. `SI81.Input.InputService` is the only input owner. No `OVRInput`, no local action asset.
4. `SI81.Interaction.Interactable` + the rig's `RayInteractor` are the only interaction path.
5. `SI81.Rig` never references `SI81.UI`; the adapter is `SI81.UI.Rig.RigUIBridge`.
6. Keep `ProjectSettings/`, `Packages/`, `Assets/Oculus/`, `Assets/XR/`, `Assets/Resources/` and
   shared folder `.meta` files from `main`.
7. `SCN_Core` remains Build Settings index 0. Never renumber it.
8. `SCN_Core` must have a floor collider under the rig (`COL_Floor_Core`, top surface at `y = 0`):
   `MNG_CoreRig` has `CharacterController` and gravity. Without it the rig falls forever.
9. One `SO_Mission_*` per scene. If the catalog already has a mission for the scene, reuse it.

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
   Before deleting the rig root, list its children: scene content parented to it (equip zones,
   hand triggers, markers) must be recreated and anchored to `RigRegistration` at runtime.

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

6. **Mission/UI.** Keep `MNG_Scenario` with `ScenarioController` (or a subclass) and one existing
   `SO_Mission_<Title>`. `SceneName` must equal the scene file name. Use `HUDService`,
   `DecisionPanel` and `MissionSession`; do not create Canvas or HUD. Do not duplicate a mission
   that already exists for the scene; check `SO_MissionCatalog` first.

7. **Build Settings and core scene.** Ensure
   `Assets/_Project/Home/Level/Scenes/SCN_Core.unity` is index 0, has `COL_Floor_Core` with its top
   at `y = 0`, and is followed by
   `Assets/_Project/Home/Level/Scenes/Mock/SCN_MockScenario.unity`. Add only the group's scene
   after them, with its `.meta` GUID unchanged.

8. **Validate.** Run the scenario validator, then:
   ```powershell
   Unity.exe -batchmode -nographics -projectPath <repo> -runTests -testPlatform PlayMode `
     -testFilter SI81.Rig.Tests.RigIntegrationTests -testResults Logs/rig-tests.xml -logFile Logs/rig-tests.log
   ```
   Open `SCN_Core` + scenario in Play Mode: exactly one `RigRegistration`, one camera, one
   `EventSystem`, no `Nessun IRigUIBridge` warning, and the rig stays at eye level.

## B.3 Forbidden

- Editing `ProjectSettings/EditorBuildSettings.asset` to remove/reorder `SCN_Core`.
- Adding `com.unity.collab-proxy` or opening Unity Version Control.
- Running Meta XR Project Setup Tool "Fix All".
- Adding a camera, rig, Canvas, EventSystem, input module, local action asset, local raycast or
  local grab implementation to a scenario scene.
- Creating a second `SO_Mission_*` for a scene already in `SO_MissionCatalog`.
- Editing `GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md`, `SharedPlayerRig.md` or files under
  `Assets/_Project/Shared/` as part of a group migration.

## B.4 Acceptance

- `git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json` → empty.
- `grep -n '"file:' Packages/manifest.json` → empty.
- `git grep -n -E "OVRInput|XRGrabInteractable|XRRayInteractor|XRInteractionManager" -- <group Code>` → empty.
- Build Settings: `SCN_Core` index 0, group scene after shared scenes.
- `SCN_Core` contains `COL_Floor_Core` with its top surface at `y = 0`.
- Unity batchmode compile: no `error CS`.
- PlayMode rig tests pass; Editor smoke has one rig, one EventSystem, UI hand-off working, and the
  rig does not fall.
- Quest checklist from `SharedPlayerRig.md` executed by the owner.

## B.5 Scenario-kit details

For scenario creation, validation, mission catalog, `SO_Mission_*`, `SceneName` and the
`ScenarioController` API, follow
[GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md](GUIDA_INTEGRAZIONE_SCENARIO_V2.0.md).
That guide is read-only. This document only defines the rig/UI/input migration on top of it.
