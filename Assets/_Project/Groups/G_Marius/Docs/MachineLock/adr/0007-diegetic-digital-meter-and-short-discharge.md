# Diegetic 7-Segment Digital Meter & 15–30 s Randomized Residual Discharge

Status: accepted

Quest 3 playtesting revealed two usability problems during LOTO zero-energy verification on `SUB_VoltageTester`: (1) the station only had a blank screen box plus red/green pilot LEDs, giving operators no continuous visual proof that Residual Energy was discharging after opening the Main Disconnect Switch; and (2) the fixed 90-second wait felt like a bug, leaving players standing idle at the electrical bay. We decided to: (1) mount a diegetic industrial 7-segment Digital Meter directly on `SUB_VoltageTester` (`MachineLockDigitalMeterDisplay`, rendered procedurally onto a `512 × 432` texture with zero scene `Canvas` or per-frame heap allocations) that starts OFF, powers ON continuously on select, streams simulated `VOLTAGE (V)` / `CURRENT (A)` / `POWER (W)` readings with a curved capacitor-bleed voltage drop (`~222 V × Charge01^1.35`, snapping cleanly to `0 V` at `Charge01 ≤ 0.05`), and automatically confirms `TaskTesterZero` when an active meter reaches safe zero; and (2) replace the fixed 90 s timer in `SO_MachineLockConfig` with a uniform per-run random range `energyDecayMinSeconds = 15`, `energyDecayMaxSeconds = 30` across all difficulty levels (Issue #45).

## Considered Options

- **Add a floating World-Space `Canvas` popup beside the tester**: rejected because `ScenarioValidator` forbids local `Canvas`/`EventSystem` in scenario scenes and a diegetic ~`26 × 22 cm` on-cabinet meter is readable at 1–2 m on Quest 3 without visual clutter.
- **Require a second manual click after an already-ON meter reaches `0 V`**: rejected because watching an active display drop to `0 V` without confirming zero energy creates unnecessary friction.
- **Keep a shorter fixed decay time (e.g. 20 s)**: rejected in favor of a `15–30 s` uniform random range so trainees cannot memorize an exact countdown while the wait stays engaging.

## Consequences

- `MachineLockConfig` replaces `energyDecaySeconds` (`90f`) with `energyDecayMinSeconds = 15f` and `energyDecayMaxSeconds = 30f` plus `RollEnergyDecaySeconds()`.
- `MachineLockEnergyPanel` exposes `ActiveDecaySeconds`, `TesterDisplayOn`, `DisplayedVoltage`, `DisplayedCurrent`, `DisplayedPower`, and `SetDecayDurationForTest(float)` for deterministic automated QA.
- `SUB_VoltageTester` remains parented atomically under its slot root so the new Digital Meter housing and LCD shuffle cleanly across all 6 electrical bay permutations (ADR-0006 / Issue #43).
- `MachineLockValidatorRunner` verifies `MachineLockDigitalMeterDisplay` presence and wiring on `SUB_VoltageTester`, and `MachineLockAuditCli` captures close-up OFF and LIVE preview PNGs for visual readability checks.
