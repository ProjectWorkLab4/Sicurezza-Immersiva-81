# UI Team Hand-Off Note — `AlertPresenter` 1.0 m Gaze Placement Depth Clipping

| Field | Value |
|---|---|
| Target team | Team Ludovica (`Assets/_Project/Home/UI/`) |
| Reporter | G_Marius (`scenario/machine-lock`, Issue #35) |
| Reference file | `Assets/_Project/Home/Code/Scripts/Runtime/HUD/AlertPresenter.cs` (lines 48, 217) |
| Shared files modified by G_Marius | **None** (`Assets/_Project/Home/` untouched) |

## Observation

In `AlertPresenter.cs`:

- Line 48 declares `[SerializeField] private float distance = 1f;`
- Line 217 places both `HUD_EventAlert` and `HUD_ActionFeedback` at that fixed `distance` using `HUDPlacement.PlaceAtGaze(view.CanvasTransform, head, distance, theme.Hud.AlertPitch, theme.Hud.MaxPitch)`.

When a Quest 3 player stands close (`< 0.9 m`) to a wall-mounted prop or open cabinet (for example the electrical panel Sub-Stations, the PPE locker shelves, or the wall alarm button), an `Alert` or `ActionFeedback` card spawned at `1.0 m` penetrates behind the wall/cabinet mesh and is partially or completely occluded by world geometry.

## Scenario-Side Mitigations Applied in `SCN_MachineLock` (No Shared UI Changes)

To keep `Assets/_Project/Home/` untouched while keeping Scenario 08 legible on Quest 3:

1. Room clearance was expanded to an `18 × 11 m` active stamping bay inside `PFB_OldIndustrialBuilding` with a `≥ 2.5 m` Dry Perimeter around the puddle.
2. The PPE cabinet was redesigned as a shallow (`0.44 m` deep), wide (`1.80 m`) sliding-door unit whose door colliders disable immediately on `Open()`, eliminating close-up head crowding against hinged doors.
3. Critical operational identity and state cues are duplicated onto flush diegetic plaques (`TEX_Plaque_*`) mounted directly on each station/bay and mirrored to the wrist panel hint (`SetHint`) so players never rely solely on a 1.0 m floating card when standing close to a wall.

## Recommended Shared UI Improvement (For Team Ludovica)

Consider one of the following in `AlertPresenter` / `HUDPlacement` on a future shared-UI PR:

1. **Depth-aware placement clamp:** Raycast along the gaze direction up to `distance` (against opaque world layers) and clamp alert placement to `Mathf.Clamp(hit.distance - 0.12f, 0.45f, distance)` (scaling the canvas proportionally so angular size stays constant), or
2. **Overlay / depth-bias render option** for `HUD_EventAlert` / `HUD_ActionFeedback` (with comfortable minimum stereo distance `≥ 0.55 m` to avoid eye strain).
