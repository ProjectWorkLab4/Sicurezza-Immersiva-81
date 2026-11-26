# Implementation Overview — Scenario 08 Machine Lock

| Field | Value |
|---|---|
| Document | `README_IMPLEMENTATION.md` |
| Scenario | 08 — Machine Lock (`SCN_MachineLock`) |
| Deliverable | Guided vertical slice, playable end to end |
| Owner | G_Marius |
| Branch | `scenario/machine-lock` |
| Unity | `6000.3.14f1` |
| Meta XR Core SDK | `209.0.0` |
| OpenXR | `1.16.1` |
| Target device | Meta Quest 3, controllers only |
| Game design document | [`GDD_MachineLock.md`](GDD_MachineLock.md) |
| Asset audit | [`../ASSET_AUDIT_Scenario08_MachineLock.txt`](../ASSET_AUDIT_Scenario08_MachineLock.txt) |
| Asset credits | [`ASSET_CREDITS.md`](ASSET_CREDITS.md) |

---

## 1. What this is

Scenario 08 puts the player in a stamping room during an ongoing emergency: a
250-ton hydraulic press is running irregularly, the cooling system has failed,
hydraulic oil at roughly 120 °C is leaking from a cracked pipe, and a hot oil
puddle surrounds the machine. The player has to stop the press, raise the alarm,
isolate and lock out the main power, verify zero residual energy, wear the
correct PPE, delimit and contain the spill, then leave the room and report to the
supervisor.

This branch delivers the **Guided** slice only. Operational and Crisis stay
locked in the mission data, but the SPI and the time system implement all three
modes and are unit-tested, so unlocking them later is a data change.

The authoritative behaviour, numbers, texts and coordinates live in
[`GDD_MachineLock.md`](GDD_MachineLock.md). This document is the technical entry
point: stack, constraints, repository layout, phase plan and verification.

## 2. Target stack

| Component | Version |
|---|---|
| Unity Editor | `6000.3.14f1` |
| Meta XR Core SDK (`com.meta.xr.sdk.core`) | `209.0.0` |
| OpenXR (`com.unity.xr.openxr`) | `1.16.1` |
| XR Management | `4.7.0` |
| Universal Render Pipeline | `17.3.0` |
| Input System | `1.19.0` |

Versions are declared in `Packages/manifest.json` and
`ProjectSettings/ProjectVersion.txt`. This scenario adds **no packages**.

## 3. Hard constraints from the shared application

These come from the GDD (§6) and from the shared rig/UI guides. Breaking one of
them breaks the whole application, not just this scenario.

- `SCN_Core` is always loaded; `SCN_MachineLock` loads additively on top of it.
- The scenario scene must not contain a Camera, AudioListener, OVRCameraRig,
  XR Origin, Canvas, EventSystem, InputModule, `MNG_UI`, `MNG_HUD`, `HUD_*`,
  `PANEL_*`, `CANVAS_*`, XR Interaction Toolkit components or `OVRInput` calls.
- Input goes only through `SI81.Input.InputService`; scenarios receive it through
  the shared `Interactable`.
- Interaction goes only through `SI81.Interaction.Interactable` plus the rig's
  `RayInteractor`. No local raycast, hover or grab code.
- UI goes only through the `ScenarioController` API, `HUDService`, `LabelAnchor`
  and `ScenarioTrigger`.
- Spawning uses `SI81.Rig.SpawnPoint` on `MNG_SpawnPoint`;
  `ScenarioController.spawnPoint` stays empty to avoid a double offset.
- `SO_Mission_MachineLock` is edited in place, never duplicated.
- `EndMission` is called exactly once on every path.
- Git only. No Unity Version Control, no `com.unity.collab-proxy`, no Project
  Setup Tool "Fix All".
- Player-facing text in Italian; code, comments, docs and commit messages in
  English.

## 4. Repository layout

Only the paths this scenario owns:

```text
Assets/_Project/Groups/G_Marius/
├── Code/Scripts/MachineLock/
│   ├── Core/                     # SI81.MachineLock: pure C# SPI and config
│   └── Runtime/                  # Scenario components and Italian texts
├── Code/Tests/EditMode/          # SI81.MachineLock.Tests: SPI unit tests
├── Code/Scripts/Editor/          # Debug menu (Editor only)
├── Level/Prefabs/MachineLock/    # Production-named prop prefabs
├── Level/Scenes/SCN_MachineLock.unity
├── Art/{Materials,Models,Textures}/MachineLock/
├── Audio/{Sound,Music}/MachineLock/
└── Docs/MachineLock/             # This folder
```

Outside the group folder, the only file this scenario edits is the existing
mission asset:

```text
Assets/_Project/Home/UI/Data/Missions/SO_Mission_MachineLock.asset
```

## 5. Implementation phases

Each phase is one GitHub issue. A phase closes only after its tests pass and its
evidence is attached to the issue. The full work breakdown and per-phase test
plan are in the GDD (§15).

| Phase | Issue title | Main work | Exit check |
|---|---|---|---|
| **P0** | `[MachineLock][P0] Branch, project checks, baseline` | Branch from `origin/main`, Git hygiene, documents, open Unity | `collab-proxy` grep empty; batchmode compile clean; baseline recorded |
| **P1** | `[MachineLock][P1] Mission data and scene shell` | Update the mission in place, duplicate the scenario template, Build Settings, spawn point | Mission validates; scene loads; spawn lands at `(-5.2, 0, 0)`; no camera or Canvas |
| **P2** | `[MachineLock][P2] Blockout and asset placeholders` | 12 × 8 × 4 room, press greybox, puddle ring, props, lighting, first credit entries | Dry perimeter walkable; colliders correct; placeholders carry production names |
| **P3** | `[MachineLock][P3] Core interactions` | E-stop, alarm, main switch, lockout, voltage tester, try-start | E-stop stops the ram; display goes dark on main off; lockout requires main off; tester reads LIVE/ZERO; try-start behaves by power state |
| **P4** | `[MachineLock][P4] PPE` | Cabinet, four slots, six items, replacement rules | Slot replacement works; 4/4 full PPE; wrong items do not protect |
| **P5** | `[MachineLock][P5] Spill containment` | Valve, four puddle quadrants, hazard trigger, four cones, four absorbents, snap zones | Snap radius 0.6 m; 4 + 4 completion; hazard off when contained; missing-item penalties |
| **P6** | `[MachineLock][P6] Observation and Awareness` | Six observation targets, gaze dwell, proximity fallback | Gaze dwell registers; fallback registers; each target scores once |
| **P7** | `[MachineLock][P7] SPI, time modes, debrief, outcomes` | `MachineLockSpi`, Guided/Realistic/Stress time rules, outcome matrix, debrief cap | Unit tests for 0 / half / limit / after limit; weights sum to 100; debrief capped at 5 good and 4 error lines |
| **P8** | `[MachineLock][P8] Damage feedback, audio, VFX, lighting` | URP vignette plus fallback, audio set, VFX set, lighting and asset credits | Vignette or fallback fires; audio and VFX match the GDD tables; Quest readability and frame budget hold |
| **P9** | `[MachineLock][P9] QA, Quest, PR` | Full regression, Quest 3 checklist, validator, PR | Validator 0 errors; Home → mission → Home; critical paths; Quest checklist signed off |

## 6. P0 baseline

Recorded when the branch was created from `origin/main`:

- Branch `scenario/machine-lock` created from `origin/main`.
- Six unrelated Unity auto-modified tracked files restored, leaving only
  intentional changes in the working tree.
- `git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json`
  prints nothing.
- Unity batchmode compile check:
  `Unity.exe -batchmode -nographics -quit -projectPath <repo> -logFile Logs/p0-baseline.log`
  exits `0` with no `error CS` or `CompilationFailed` lines.

## 7. Verification commands

Run these before every phase review.

```powershell
# Version control hygiene — must print nothing
git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json

# No XRI or OVRInput in the scenario code — must print nothing
git grep -n -E "OVRInput|XRGrabInteractable|XRRayInteractable|XRInteractionManager" -- Assets/_Project/Groups/G_Marius/Code/Scripts/MachineLock

# Branch and tree state
git branch --show-current
git status
```

In the Editor:

- `SI81/Scenari/Valida scenario` — 0 errors, 0 warnings (or explained).
- EditMode tests for `SI81.MachineLock.Tests`.
- Play Mode flow: `SCN_Core` → Home → card 08 → Mission Brief → Guided → Avvia.

## 8. Out of scope for this branch

- Operational and Crisis playable cards (logic implemented and tested, cards
  stay locked).
- Multiplayer, networking, hand tracking, XR Interaction Toolkit.
- Camera, Canvas, EventSystem, rig or HUD authored inside the scenario scene.
- A physical padlock/tag prop; lockout is an abstract panel interaction.
- DPI wrist icons; DPI feedback is text-only.
- Final art, final audio, final lighting bake, decision-timeline debrief.

## 9. Documented deviations

1. **Weighted SPI.** The scenario passes an explicit weighted score (20 / 20 /
   40 / 15 / 5) instead of the common equal average. Needs Team Ludovica
   approval; the fallback is one method change inside `MachineLockSpi`.
2. **Text-only DPI.** The brief asks for wrist icons; the common HUD exposes no
   DPI slot, so the scenario uses hints, feedback and alerts.
3. **No padlock prop.** Lockout stays an abstract interaction with a red LOCK
   indicator, a feedback line and the try-start proof.

## 10. Open items

- Final press, prop and audio assets from the owner.
- Safety representative sign-off on the procedure and every Italian string.
- Team Ludovica approval for the weighted SPI and a future DPI HUD widget.
- Quest validation of the URP vignette.
- Final lighting bake and performance pass.
- Decision on when to unlock Operational and Crisis after QA.
