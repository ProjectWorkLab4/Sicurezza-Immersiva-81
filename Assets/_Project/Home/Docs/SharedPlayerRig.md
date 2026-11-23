# Shared Player Rig — foundation

Implementation record for issue [#3](https://github.com/ProjectWorkLab4/Sicurezza-Immersiva-81/issues/3).
The rig is authored here; the common UI (`D:\lavoro\PW4_Test`, `SI81.UI`) is the integration target.
`SCN_Core` owns the rig, the UI and the HUD; scenario scenes load additively and contain no camera, rig,
Canvas, EventSystem or input module.

World interaction is documented in [SharedInteraction.md](SharedInteraction.md).

## Assemblies

| Assembly | Owns | References |
|---|---|---|
| `SI81.Input` | actions, bindings, contexts, polling/events, UnityEvent bridge | `Unity.InputSystem` |
| `SI81.Rig` | rig registration/bridge, locomotion, spawn point, aim follower, prefab | `SI81.Input`, `Oculus.VR` |
| `SI81.Interaction` | `Interactable`, per-hand `RayInteractor` (hover/select/grab) | `SI81.Input` |
| `SI81.Rig.Editor` | prefab builder, Editor direct-play bootstrap | `SI81.Input`, `SI81.Rig`, `SI81.Interaction`, `Oculus.VR` |
| `SI81.Rig.PlayModeTests` | one PlayMode integration seam | `SI81.Input`, `SI81.Rig`, `SI81.Interaction`, Input System test fixture |

`SI81.Rig` never references `SI81.UI`. The UI adapter implements `IRigUIBridge` in the UI/app-shell project and
calls `UIScreenManager.SetHead/SetTrackingOrigin`, `HUDRoot.SetRig`, `UIPointer.IsPointingAtUI` and
`HapticsService.Pulse`. `SI81.UI` may reference `SI81.Input` but never `SI81.Rig` or the Meta SDK.

## Input Service

One runtime instance, created before the first scene, `DontDestroyOnLoad`, reachable from
`InputService.Instance`. Static state resets on `SubsystemRegistration`. Duplicates are rejected with one warning;
the first instance wins. `InputService.ResetInstance()` / `EnsureInstance()` are the test/bootstrap seam.

The action asset is created in code (`SI81Input`), with two control schemes: `MetaQuest` (XR controllers) and
`Editor` (keyboard/mouse). No `.inputactions` file.

| Map | Action | MetaQuest | Editor | Channel |
|---|---|---|---|---|
| World | `Select` | both triggers | left mouse | World |
| World | `Grab` | both grips | right mouse | World |
| World | `Menu` | left Menu | Tab | World (pause owner) |
| World | `Move` | left stick | WASD + arrows | World |
| World | `Turn` | right stick | Q / E | World |
| UI | `Point` | — | mouse position | UI |
| UI | `Click` | both triggers | left mouse | UI |
| UI | `Scroll` | — | mouse wheel | UI |
| UI | `TrackedDevicePosition` | both aim positions | — | UI |
| UI | `TrackedDeviceOrientation` | both aim rotations | — | UI |
| UI | `Navigate` | both sticks | WASD + arrows | UI |
| UI | `Back` | B / Y | Esc / Backspace | UI |

A/X are intentionally unbound: the common UI selects with the trigger. This is the documented deviation from the
design guide's "Trigger + X/A" row.

`Back` lives only in the `UI` map. The issue lists it in both maps but also decides it is UI-only in this
increment; keeping one action avoids two owners for B/Y. The World map therefore omits it.

Binding paths and action names are constants in `SI81.Input.InputBindings`; a button change is a one-line edit.

### API

- Polling: `WasSelectPressed()`, `WasSelectPressed(Hand)`, `IsGrabHeld()`, `IsGrabHeld(Hand)`,
  `WasMenuPressed()`, `WasBackPressed()`, `ReadMove()`, `ReadTurn()`.
- Events (hand included): `SelectPerformed`, `SelectCanceled`, `GrabPerformed`, `GrabCanceled`,
  `MenuPerformed`, `BackPerformed`, `MoveChanged`, `TurnChanged`; AnyHand convenience events for Select/Grab.
- Contexts: `PushContext(InputChannel.World, owner)` returns an idempotent handle; `PopContext` is owner-scoped.
  The first World context cancels held Select/Grab through the normal cancel events. World suppression stops
  Select/Grab/Move/Turn; UI/Back/Menu stay available.
- UI gate: `SetUIPointerOverUI(Hand, bool)`. A hand pointing at UI keeps its `Select` out of the world; the other
  hand is unaffected.
- Back: `SetBackEnabled(bool)`, default off. Enable only while the menu is visible.
- Aim pose: `TryGetAimPose(Hand, out Vector3, out Quaternion)` in tracking space. `RigRegistration.TryGetAimPose`
  converts it to world space with the tracking origin.
- UnityEvent bridge: `InputUnityEventBridge` (Select/Grab/Back/Menu and Vector2 Move/Turn), event-driven, no
  per-frame work.

## Player Rig

Prefab: `Assets/_Project/Home/Level/Prefabs/MNG_CoreRig.prefab` (rebuild with
`SI81 > Rig > Rebuild MNG_CoreRig Prefab`).

- Root `MNG_CoreRig`: `CharacterController` (1.8 m, radius 0.3), `RigRegistration`, `RigLocomotion`,
  `RigUIPointerFeed`.
- Child `OVRCameraRig` (Meta XR Core SDK 209), `TrackingSpace` / floor level, `CenterEyeAnchor`,
  `LeftControllerAnchor` and `RightControllerAnchor`.
- `LeftControllerModel` / `RightControllerModel` are visual only; `ControllerAimFollower` aligns them to the
  Input Service aim pose so the drawn ray and the controller agree.
- `LeftControllerAnchor` / `RightControllerAnchor` carry one `RayInteractor` each (`Hand`, rig `TrackingSpace`,
  rig root ignored). Scenario objects only add `Interactable`; see [SharedInteraction.md](SharedInteraction.md).
- Copy the prefab together with its `.cs` and `.meta` files: Unity resolves the prefab's script references by GUID.
- Snap turn is the comfort default. A comfort vignette is a future component on the rig root; this increment does
  not implement or tune it.
- Only the root moves. The camera and tracking origin follow.
- Locomotion: left stick relative to head yaw, 0.2 radial deadzone, 1.8 m/s cap, right stick snap turn 45°,
  re-arm threshold, CharacterController gravity/floor/collision. Movement and turn stop while World is suppressed
  or `Time.timeScale == 0`.
- The `UIPointer` components live in the UI project and are added to the prefab copy there: `SI81.Rig` must not
  reference `SI81.UI`. The adapter exposes their `IsPointingAtUI` state through `IRigUIBridge`.

### Hand-off

`IRigUIBridge.ApplyRig(RigHandoff)` receives head, tracking origin, left hand and right hand. The same interface
carries `IsPointingAtUI(Hand)` and `Pulse(Hand, amplitude, duration)`. The UI adapter keeps the
`controls.ui_haptics` gate inside `HapticsService`.

## Spawn point

Add `SpawnPoint` to `MNG_SpawnPoint` in a scenario. On enable it stores the rig's previous pose and applies the
marker position and yaw to the rig root; on disable (scenario unload) it restores the previous pose.
`RigRegistration.TeleportTo` resets the vertical velocity. The old `ScenarioController` moves the UI tracking
origin instead: when merged, the adapter/scenario loader must call the rig spawn point or `RigRegistration.TeleportTo`,
not `UIScreenManager.SetTrackingOrigin`'s transform, so there is one owner of the player pose.

## Editor direct play

`SI81.Rig.EditorTools.RigDirectPlay` loads `SCN_Core` additively when Play starts in a scenario scene that contains
a `SpawnPoint` and `SCN_Core` is not loaded. The scenario scene stays active. Hiding the menu remains the UI
adapter's job (`ScenarioController`/`UIScreenManager`); the rig does not reference UI.

## Testing

One PlayMode seam: `SI81.Rig.PlayModeTests` (`Assets/_Project/Home/Code/Tests/PlayMode`) with a virtual XR
controller (same control names as OpenXR) and a stub `IRigUIBridge`. It does not instantiate the Meta prefab or
require OpenXR. The rig hierarchy is created in code: head/left/right, `RigRegistration`, `RigLocomotion`,
`RigUIPointerFeed`, bridge.

Covered: hand-off references, Select/Grab/Back/Menu, suppression and held-action cancellation, owner-scoped
contexts, per-hand UI gate, Move/Turn values, locomotion and snap turn, pause/suppression blocking, Pulse,
tracking-space aim conversion, singular/reset Input Service, and the interaction seam (hover, select, grab follow,
Rigidbody restore, occlusion, UI gate, World suppression, two-hand independence).

Run from the Editor Test Runner (PlayMode) or:

```powershell
Unity.exe -batchmode -nographics -projectPath <repo> -runTests -testPlatform PlayMode `
  -testFilter SI81.Rig.Tests.RigIntegrationTests -testResults Logs/rig-tests.xml -logFile Logs/rig-tests.log
```

## Migration for scenario teams

Groups that already have a rig/input code (Tiziano `OVRInput`, Simone Interaction SDK, Ludovica bare camera):

1. Remove the local camera/rig, Canvas, EventSystem, input module, `PauseInput` and `UIPointer` from the
   scenario scene. The scenario validator must pass with only environment, objects, `MNG_Scenario`,
   `MNG_SpawnPoint` and `LabelAnchor`.
2. Put `SpawnPoint` on `MNG_SpawnPoint`; remove the plain-transform spawn assignment from local scripts.
3. Replace direct `OVRInput` reads with `InputService` actions/events. Do not add a local action asset, haptics
   service, EventSystem or pointer.
4. Put `Interactable` on every object that must be pointed at, selected or grabbed; consume its C# events or
   inspector events. Do not write a local raycast, interactor or `OVRInput` read. World interaction arrives with the
   rig prefab, so the scenario adds only content components.
5. The UI team merges the prefab and the `IRigUIBridge` adapter, removes the old `CoreRig` hand-off and binds
   `InputService` to the single `InputSystemUIInputModule`, `UIScreenManager` Back and `MissionFlowDriver` Menu.
6. Run the PlayMode seam and the Quest checklist before merging to `main`.

## Quest 3 device gate

Device `2G0YC5ZG1402NV` (Quest 3) was verified with `metavr device list` on 2026-11-23. Run this checklist on
device after the build is installed:

- Exactly one rig and one camera in `SCN_Core`; no rig in the scenario scene.
- Floor height correct; head/controllers track; controller models follow the hands and the aim ray.
- Left stick moves relative to gaze; right stick snaps 45°; no drift at rest (deadzone) and capped speed.
- Left Menu opens/closes pause; movement stops while pause/menu is open.
- B/Y is inert during a mission and goes Back only while the menu is open.
- Trigger hovers/clicks UI through the existing ray; a UI click does not select a world object behind it.
- Haptics pulses on menu/pause/alerts/interaction and is disabled by `controls.ui_haptics`.
- Console/logcat clean of `[SI81 Input]` duplicate-instance, missing-bridge and rig errors.

Do not claim a device pass without running it. The device serial above is evidence of connection only.

## Out of scope

Poke/direct interaction, two-hand grab, sockets, throwing and grab anchors, teleport/smooth turn/vignette tuning,
HUD wrist panel content, migration of the four group scenes, hand tracking, XR Interaction Toolkit, new packages,
Figma and networking. The world ray visual and the final UI/world priority remain with the UI/world ray decision.
