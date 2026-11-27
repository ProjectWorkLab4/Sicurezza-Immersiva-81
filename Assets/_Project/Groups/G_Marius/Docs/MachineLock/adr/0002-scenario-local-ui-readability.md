# Keep Shared Home UI Untouched and Solve Label Readability Locally

Status: accepted

Quest 3 playtesting showed that World Labels and Event Alerts compenetrate with nearby props and walls, and that electrical panel sub-controls and PPE items were hard to distinguish. Rather than modifying `Assets/_Project/Home/` (which is shared across all scenarios and owned by the UI team), we decided to keep `Assets/_Project/Home/` untouched and solve readability inside `Assets/_Project/Groups/G_Marius/` using hover-driven Action Feedback, Wrist Hints, Diegetic Plaques mounted flush on controls and shelves, and sparse World Labels positioned cleanly in open space.

## Considered Options

- **Modify shared `WorldLabel` / `AlertPresenter` in `Assets/_Project/Home/`** (e.g., overlay shader or depth-clipped placement): rejected for this branch to preserve scenario blast radius and avoid uncoordinated changes to shared UI. The `AlertPresenter` depth-clip observation will be handed off to the UI team separately.
- **Scenario-local combination of hover Action Feedback, Wrist Hints, Diegetic Plaques, and clean anchor offsets** (chosen): works immediately within the shared `HUDService` contract and requires zero shared-UI edits.
