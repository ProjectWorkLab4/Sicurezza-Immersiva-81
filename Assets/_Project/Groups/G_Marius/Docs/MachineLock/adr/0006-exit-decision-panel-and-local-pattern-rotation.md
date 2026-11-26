# Exit via Panic Bar + Shared DecisionPanel, Non-Blocking Early Exit & Local Pattern State

Status: accepted

Quest 3 testing exposed two defects at the exit: `ZONE_Exit`'s floor-level `ScenarioTrigger` (`y = 0`, `radius = 1.15 m`) was mathematically unreachable for a standing player's head (`y ≈ 1.65 m`), and its `LabelAnchor` showed a misleading `"GRIP — Afferra"` prompt with no `Interactable`. Meanwhile, `MachineLockScenario` hard-blocked exiting before `TaskEStop` and rolled `Random.Range(0, 3)` per run (repeating the same Layout Pattern 33% of the time). We decided to: (1) make the exit door's panic bar an `Interactable` that opens `SCN_Core`'s built-in `HUD_DecisionPanel` via `ScenarioController.AskDecision` (matching the native Home UI without touching `Assets/_Project/Home/` or adding a forbidden `Canvas` to `SCN_MachineLock`); (2) allow Early Exit at any time, swinging the steel door open on confirmation and scoring/penalizing any unfinished safety tasks—especially leaving with the press still running—in the Debrief and outcome matrix; and (3) persist a scenario-local shuffle-bag JSON file in `Application.persistentDataPath` so consecutive runs always rotate through different Layout Patterns without touching `Home/`.

## Considered Options

- **Add a new Canvas/modal layer in `SCN_MachineLock` or `Assets/_Project/Home/`**: rejected because `ScenarioValidator` forbids local `Canvas`/`EventSystem` in scenarios and `Assets/_Project/Home/` is owned by the UI team.
- **Build a custom non-Canvas 3D confirmation plaque at the door**: rejected once `ScenarioController.AskDecision` (`HUD_DecisionPanel`) was confirmed available in `SCN_Core` with native Home styling and Quest 3 raycast support.
- **Use `ScenarioController.AskDecision` + panic-bar `Interactable` + local JSON shuffle-bag (chosen)**: delivers native Home UI aesthetics, intentional VR door interaction, consequence-driven Early Exit, and non-repeating Layout Patterns with zero edits to `Home/`.

## Consequences

- Leaving before `TaskEStop` is no longer blocked; instead it records a critical missing-E-Stop penalty and debrief line, forces a `Negative` outcome (and zero/severe penalty in non-Guided modes), and `GDD_MachineLock.md` plus EditMode/PlayMode tests are updated accordingly.
- `forcedPatternIndex` remains supported for deterministic EditMode/PlayMode tests, bypassing persistent JSON state when set.
