# Shared world interaction — Interactable and RayInteractor

Implementation record for the custom thin interaction layer decided in the wayfinder map
(decision: not Meta XR Interaction SDK). The layer consumes the foundation from
[issue #3](https://github.com/ProjectWorkLab4/Sicurezza-Immersiva-81/issues/3) and never reads raw devices,
`OVRInput` or UI code.

## Assembly and boundary

| Assembly | Owns | References |
|---|---|---|
| `SI81.Interaction` | `Interactable`, `RayInteractor` | `SI81.Input` |

`SI81.Interaction` does not reference `SI81.Rig`, `SI81.UI`, the Meta SDK or the OpenXR loader. The rig prefab carries
the interactors; a scenario object only needs an `Interactable`. The scenario scene stays rig-free.
When the prefab is copied into the common UI project, copy `SI81.Interaction` and its `.meta` files with it; the UI
assembly does not need to reference the interaction assembly, but the prefab's script references must resolve.

## Scene model

- `MNG_CoreRig` already has one `RayInteractor` per hand on `LeftControllerAnchor` / `RightControllerAnchor`. The
  prefab builder wires the hand, the rig `TrackingSpace` and the rig root to ignore.
- A scenario object adds `Interactable` to the GameObject that should be pointed at. The collider may be on the same
  GameObject or on a child; the interactor searches parents from the hit collider.
- The interactor reads the aim pose from `InputService.TryGetAimPose` and converts it to world space with the rig
  `TrackingSpace`. It does not invent a second pose or a second ray.
- No UI ray, `EventSystem`, camera or input module is added by the interaction layer.

## Interactable contract

`Interactable` is a state holder and event source. It does not move by itself and does not read input.

| Member | Meaning |
|---|---|
| `Selectable` / `Grabbable` | Runtime switches; default on. |
| `FollowsHand` | When grabbed, the object follows the hand. Turn off for script-animated grabs. |
| `IsHovered`, `IsSelected`, `IsGrabbed` | Any hand. |
| `IsHoveredBy(Hand)`, `IsSelectedBy(Hand)`, `IsGrabbedBy(Hand)` | Per hand; `Hand.Any` works for hover/select queries. |
| `GrabbedBy` | Hand holding the object, or `Hand.Any`. |
| `CanHover`, `CanSelect`, `CanGrab` | Used by the interactor before dispatching. |

Events, C# and inspector (`UnityEvent<Hand>`), one per transition: `HoverEntered`, `HoverExited`, `SelectEntered`,
`SelectExited`, `GrabEntered`, `GrabExited`. The C# event fires first, then the inspector event. Events carry the hand
that produced the transition, never `Hand.Any`.

During a grab, `Select` remains available; the two channels are independent. A second hand cannot grab an object
already grabbed (`CanGrab` is false). Hover and select from both hands are allowed.

`Interactable.OnDisable` silently clears hover/select/grab state so a re-enabled object does not show stale state. The
interactor validates its references each frame and releases destroyed or disabled targets.

## Ray interactor behavior

- One interactor per hand, one object hovered per hand (nearest non-ignored hit).
- Raycast: `Physics.RaycastNonAlloc`, `QueryTriggerInteraction.Ignore`, default 5 m, all layers, no per-frame
  allocation. Colliders under the rig root are skipped. The first non-ignored collider wins: a wall blocks the ray
  instead of letting it pass through to an object behind it.
- Hover updates every frame from the current aim pose. `Hovered` is exposed for a future pointer visual.
- `Select` (trigger) starts on press and ends on release or cancel. `Grab` (grip) starts on press and ends on release
  or cancel. Actions are queued from the Input Service events and applied after the frame's raycast, so a press uses
  the current hover.
- UI gate: `InputService.IsPointingAtUI(hand)` clears that hand's world hover; the Input Service already keeps that
  hand's `Select` out of the world. The other hand is unaffected. This is the current arbitration — the visual/priority
  ticket may refine the shared ray later.
- World suppression (`InputContextHandle`) or `Time.timeScale == 0` releases hover, select and grab on both hands.
  The Input Service also cancels held Select/Grab, so consumers see the normal exit events.
- The interaction layer draws nothing. The world ray visual is deferred to the UI/world ray priority decision.

## Grab semantics

- The object keeps the offset it had from the hand at grab time and follows position and rotation delta. It is not
  pulled to the hand.
- If the `Interactable` (or a parent) has a `Rigidbody`, the interactor makes it kinematic and disables gravity for the
  duration. On release it restores both, zeroes velocity and leaves the object where it was released.
- No throw velocity, physics joint, grab anchor, two-hand grab, socket, poke or inspect behavior in this increment.
- `LabelAnchor.Suppressed` is not changed by the interaction layer. The label owner subscribes to `GrabEntered` /
  `GrabExited`, the same way it would for any other consumer.

## Haptics

The interaction layer does not pulse. Haptics stay in the existing path (`RigRegistration.Pulse` → `IRigUIBridge` →
`HapticsService`, with the `controls.ui_haptics` gate). Subscribe to the `Interactable` events and call `Pulse` from
the owner that knows the feedback policy.

## Scenario usage

1. Add a collider and `Interactable` to the object.
2. Optionally set `Selectable`, `Grabbable`, `FollowsHand`.
3. Wire `HoverEntered` / `SelectEntered` / `GrabEntered` (and the exit events) in the Inspector or from code.
4. Do not add a camera, rig, Canvas, EventSystem, input action asset, raycast or `OVRInput` read to the scenario.

## Testing

`InteractionTests` lives in the same PlayMode seam (`SI81.Rig.PlayModeTests`). It builds a virtual XR controller, two
`RayInteractor` components, a target cube and an occluding wall in code; it needs no headset, OpenXR loader, Meta
prefab or UI project.

Covered: hover enter/exit, per-hand select, grab follow and release, Rigidbody kinematic restore, occlusion, UI
pointing gate, World suppression release, two-hand independence, `Grabbable` opt-out.

```powershell
Unity.exe -batchmode -nographics -projectPath <repo> -runTests -testPlatform PlayMode `
  -testFilter SI81.Rig.Tests.InteractionTests -testResults Logs/interaction-tests.xml -logFile Logs/interaction-tests.log
```

## Out of scope

World ray visual and final UI/world priority; poke/direct interaction; two-hand grab; sockets; throwing; grab anchors;
inspect; teleport/comfort; HUD prompt content; group scene migration; hand tracking; XR Interaction Toolkit; new
packages.
