# Implementation Guide — Scenario 08 Machine Lock

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
| Domain glossary & ADRs | [`GLOSSARY.md`](GLOSSARY.md) · [`adr/0001..0006`](adr/) |
| UI team hand-off note | [`UI_TEAM_HANDOFF_AlertPresenter.md`](UI_TEAM_HANDOFF_AlertPresenter.md) |
| Asset audit | [`../ASSET_AUDIT_Scenario08_MachineLock.txt`](../ASSET_AUDIT_Scenario08_MachineLock.txt) |
| Asset credits | [`ASSET_CREDITS.md`](ASSET_CREDITS.md) |

> **Document status.** This is the hand-off technical guide for the scenario.
> P0–P9, Issue #35, Issue #36 (#37–#42: custom low-poly `MESH_WorkshopShell`,
> Blender hero press/sub-stations/REI-120 exit door, uniform-scale safety props,
> 4-quadrant `MESH_OilPuddleSplat`, 14 UNI EN ISO 7010 signs, 6 brushed-metal
> plaques, 18 authentic workshop audio clips, `HUD_DecisionPanel` exit flow,
> Early Exit SPI, and persistent shuffle-bag pattern rotation), and Issue #43
> (full-door exit interaction, edge-clamped decision panel, locked exit glide +
> fade-to-black behind end UI, and unnumbered 6-state randomized electrical
> Sub-Stations) are implemented, verified by automated QA, and **passed on the
> physical Meta Quest 3 headset (`2G0YC5ZG1402NV`)** on branch
> `scenario/machine-lock`. Only syncing with `origin/main` and opening/merging
> the Pull Request remain under Issue #34.

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
point: architecture, wiring, deviations, verification commands, QA evidence,
device gate and PR draft.

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

These come from GDD §6 and from the shared rig/UI guides. Breaking one of them
breaks the whole application, not just this scenario.

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

---

## 4. Architecture and assemblies

### 4.1 Assembly breakdown

| Assembly | Definition | Contents | References |
|---|---|---|---|
| `SI81.MachineLock` | `Code/Scripts/MachineLock/Core/SI81.MachineLock.asmdef` | `MachineLockSpi`, `MachineLockSpiOptions`, `MachineLockConfig`, `MachineLockPatternSelector`, SPI enums (`SpiEvent`, `CriticalEvent`, `DebriefLineId`, `SpiTimeMode`, `ObservationMode`) | `SI81.UI` (for `DifficultyLevel`, `OutcomeType`, `DebriefLinePrefix`) |
| `SI81.MachineLock.Runtime` | `Code/Scripts/MachineLock/Runtime/SI81.MachineLock.Runtime.asmdef` (added in P8/P9) | The 16 runtime components (including `MachineLockExitDoor` and `MachineLockCollisionSafety`) and `MachineLockTexts` | `SI81.MachineLock`, `SI81.UI`, `SI81.Input`, `SI81.Interaction`, `SI81.Rig`, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime` |
| `SI81.MachineLock.Tests` | `Code/Tests/EditMode/SI81.MachineLock.Tests.asmdef` | `MachineLockSpiTests` — 42 NUnit tests (SPI, Early Exit, shuffle-bag rotation, ADR-0004 PPE & hover checks) | `SI81.MachineLock`, `SI81.MachineLock.Runtime`, `SI81.UI`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework` |
| `SI81.MachineLock.PlayModeTests` | `Code/Tests/PlayMode/SI81.MachineLock.PlayModeTests.asmdef` (added in P9) | `MachineLockFlowPlayModeTests` — 33 `[UnityTest]` integration tests (including exit confirmation, locked exit fade-to-black, and randomized Sub-Station slots) | `SI81.MachineLock.Runtime`, `SI81.MachineLock`, `SI81.UI`, `SI81.Input`, `SI81.Interaction`, `SI81.Rig`, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`, test runners, `nunit.framework` |
| `Assembly-CSharp-Editor` | `Code/Scripts/Editor/` (Unity Editor folder) | `MachineLockSceneBuilder` (+ `.Plaques`), `MachineLockValidatorRunner`, `MachineLockDebugMenu` | Every runtime assembly plus `AssetDatabase` |
| `SI81.Rig.PlayModeTests` | `Assets/_Project/Home/Code/Tests/PlayMode/` (shared) | Rig regression seam (`SI81.Rig.Tests`) | Shared rig |

Assembly graph:

```text
SI81.MachineLock              SPI, options, config (references SI81.UI)
    ▲
SI81.MachineLock.Runtime      content components and texts
                              (references SI81.MachineLock, SI81.UI, SI81.Input, SI81.Interaction, SI81.Rig, Core RP, URP)
    ▲
SI81.MachineLock.PlayModeTests
Assembly-CSharp-Editor        scene builder, validator runner, debug menu
```

Why the runtime assembly exists: without an `.asmdef`, the runtime components
land in `Assembly-CSharp`, which test assemblies cannot reference from an
`asmdef`. Adding `SI81.MachineLock.Runtime.asmdef` is what makes the P9 PlayMode
integration tests compile against the real components instead of `InternalsVisibleTo`
tricks or reflection. It is recorded as a P9 deviation in §6.

### 4.2 Runtime flow

```text
ScenarioController base
  OnScenarioStarted
    MachineLockSpi(config.CreateSpiOptions(Level))
    SetPhase(PHASE_SCAN) / SetHint / RefreshObjectives / intro subtitles

Content component (press, alarm, panel, PPE, spill, observation, exit)
  -> MachineLockScenario.Notify*
      -> spi.ApplyEvent / spi.ApplyCritical / spi.Observe
      -> HUD (SetHint, Alert, Feedback, Subtitle)
      -> RefreshObjectives

NotifyExitReached
  apply handover penalties -> spi.EvaluateOutcome -> supervisor line
  -> EndMissionOnce(outcome)
       EndMission(outcome, spi.Spi, spi.Dimensions100(), capped debrief)
```

Every `Notify*` method is null-safe, idempotent and ignores calls after
`HasEnded`. `EndMissionOnce` is the single end path; the timer expiry calls it
directly, and every other ending (exit, forced end) must route through it too.

### 4.3 Repository layout

Only the paths this scenario owns:

```text
Assets/_Project/Groups/G_Marius/
├── Code/Scripts/MachineLock/
│   ├── Core/                     # SI81.MachineLock: SPI, options, config
│   └── Runtime/                  # SI81.MachineLock.Runtime: components, Italian texts
├── Code/Tests/EditMode/          # SI81.MachineLock.Tests: SPI unit tests
├── Code/Tests/PlayMode/          # SI81.MachineLock.PlayModeTests (P9)
├── Code/Scripts/Editor/          # Scene builder, validator runner, debug menu
├── Level/Data/                   # SO_MachineLockConfig, SO_MachineLockDamageProfile (URP VolumeProfile)
├── Level/Prefabs/MachineLock/    # 12 production-named prop prefabs
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

---

## 5. Component and prefab wiring map

**Wiring state.** `SCN_MachineLock.unity` and all 12 prefabs are built and
cross-wired by `MachineLockSceneBuilder.BuildFullScene`, verified by
`MachineLockValidatorRunner.ValidateScene` (`Errors=0 Warnings=0`) and the
PlayMode scene test `RealScene_MachineLock_SpawnNoForbiddenObjectsAndAllComponentsWired`.

### 5.1 Scene roots

| Root | Role | Key contents |
|---|---|---|
| `MNG_Scenario` | Orchestrator | `MachineLockScenario` (mission, config, empty `spawnPoint`, `editorLevel = Guided`, `loadCoreInEditor = true`) |
| `MNG_SpawnPoint` | Player start | `SI81.Rig.SpawnPoint` at `(-7.6, 0, 0)`, yaw `90°` |
| `MNG_Light` | Lighting root | Four baked ceiling fixtures (`MESH_Magazzino_LuceDaSoffitto.fbx`), at most two realtime/mixed lights, at most one shadow caster |
| `MNG_Audio` | Ambient audio | `SFX_AmbientFactory` loop |
| `ENV_Room` | Shell & hall | Blender-authored `MESH_WorkshopShell.fbx` (`28 × 13 × 6.5 m`, 3,516 tris, ADR-0005), `18 × 11 m` `MESH_Floor`, invisible bay bounds, `MESH_Door` (`MachineLockExitDoor` + `MESH_ExitBeacon`), active-bay & background `ENV_IndustrialDressing` (workbenches, tool cabinets, bund drums, bollards, sheet pallets, overhead conduits, forklift, racking), 14 UNI EN ISO 7010 `MESH_Signs`, yellow floor lanes |
| `ENV_Press` | Press area | `MESH_HydraulicPress`, display, thermometer, leak pipe, `MESH_Valve` |
| `ENV_Safety` | Safety equipment | E-stop pedestal, alarm button, electrical panel, PPE cabinet, spill kit, water extinguisher |
| `HAZARD_Oil` | Spill | `VFX_OilJet`, `VFX_OilPuddle` (`MESH_OilPuddleSplat.fbx`, 4 quadrants), `ZONE_PuddleHazard`, `ZONE_Coverage1..4`, `PATTERN_PuddleA/B/C` |
| `ZONE_Delimitation` | Cone targets | `ZONE_ConePoint1..4` |
| `MNG_LayoutPatterns` | Layout variants | `PATTERN_A/B/C` (rotated via persistent shuffle-bag `machinelock_pattern_state.json`) |
| `MNG_ObservationTargets` | Awareness sources | `OBS_Press`, `OBS_Thermometer`, `OBS_Display`, `OBS_Puddle`, `OBS_Panel`, `OBS_PpeCabinet` |
| `MNG_Debug` | QA helpers | Empty container for debug-only objects |

`ENV_`, `ZONE_`, `OBS_` and `PATTERN_` are local grouping prefixes. They do not
conflict with the validator; the SI81 prefixes (`MESH_`, `MNG_`, `VFX_`,
`SFX_`, `MAT_`, `TEX_`, `SCN_`, `SO_`) remain mandatory for assets.

### 5.2 The 12 prefabs

All prefabs keep their production root name so final art can replace the visual
children without breaking references. Prefab-level `Interactable` and scenario
components are added in P3–P5; do not rename the roots.

| # | Prefab | Target root components | Children and colliders |
|---:|---|---|---|
| 1 | `MESH_HydraulicPress` | `MachineLockPress` | Blender `MESH_HydraulicPressBody.fbx` + moving `MESH_HydraulicPressRam.fbx`, `MAT_PressSteel_Green`, HMI fault display, 120 °C gauge, brushed-metal `MAT_Plaque_PressNameplate`, east leak manifold, hazard trigger |
| 2 | `MESH_EStopPedestal` | `Interactable` (E-stop) | Pedestal, yellow backplate, red mushroom (`SFX_MechanicalButton`), `MESH_PULSANTI_TEAM.fbx` test-start button `Interactable`, engraved brushed-metal `MAT_Plaque_EStop` |
| 3 | `MESH_AlarmButton` | `MachineLockAlarm`, `Interactable` | Wall box, red button, beacon dome renderer, `beaconLight` (off at start), `beaconRotor`, spatial looping `SFX_AlarmSiren` |
| 4 | `MESH_ElectricalPanel` | `MachineLockEnergyPanel` | 3 physically separated (≥ 0.88 m) IP65 steel Sub-Stations (`MESH_SubStationDisconnect` + `MESH_DisconnectHandle`, `MESH_SubStationLockout`, `MESH_SubStationTester`), recessed LEDs, unnumbered brushed-metal plaques, 6-state persistent shuffle-bag slot permutation across `Z = -0.88 / 0.00 / +0.88` |
| 5 | `MESH_PpeCabinet` | `MachineLockPpeCabinet`, `Interactable` (door) | Wide shallow (`1.80 × 2.05 × 0.44 m`) sliding double doors (colliders disable on `Open()`), six hero `MachineLockPpeItem` bays (`MESH_HeatGlovesHero`, `MESH_ChemicalApronHero`, `MESH_FaceShieldHero`, `MESH_SafetyBootsHero`, `MESH_LatexGlovesHero`, `MESH_SurgicalMaskHero`) & 6 anti-aliased `EquippedBadge` indicators |
| 6 | `MESH_SpillKit` | Container only | `MESH_SCATOLA_APRIBILE_ROSSA_TEAM.fbx` bin, four nested `MESH_Cone`, two `MESH_AbsorbentMat`, two `MESH_AbsorbentSock` |
| 7 | `MESH_WaterExtinguisher` | `MachineLockExtinguisher`, `Interactable` | Uniform-scale `MESH_WaterExtinguisherHero.fbx`, wall bracket, attached `MESH_IsoSign_F001` (travels across Patterns A/B/C), capsule collider, spray/steam/smoke particle hooks |
| 8 | `MESH_Cone` | `MachineLockCone`, `Interactable` | Uniform-scale `MESH_TrafficConeHero.fbx` with dual reflective collars, 0.5 m high, box collider (`MachineLockCollisionSafety` ignores `CharacterController`) |
| 9 | `MESH_AbsorbentMat` | `MachineLockAbsorbent` (`AbsorbentKind.Mat`), `Interactable` | Quilted `MESH_AbsorbentMatHero.fbx`, `0.95 × 0.034 × 0.75 m`, box collider (`MachineLockCollisionSafety` ignores `CharacterController`) |
| 10 | `MESH_AbsorbentSock` | `MachineLockAbsorbent` (`AbsorbentKind.Sock`), `Interactable` | Curved fabric-sleeved `MESH_AbsorbentSockHero.fbx` (zero sausage texture), capsule collider (`MachineLockCollisionSafety` ignores `CharacterController`) |
| 11 | `MESH_Valve` | `MachineLockValve`, `Interactable` | Pipe section, bright red `MESH_Valve_Wheel` (0.3 m), pulsing cue ring, box collider |
| 12 | `MESH_IndustrialDoor_RollUp` | `MachineLockExitDoor` | Hinged REI-120 steel door (`MESH_SteelExitDoorFrame.fbx`, `MESH_SteelExitDoorLeaf.fbx`, `MESH_PanicBar.fbx`, zero `MESH_DoorRollHousing`), `Interactable` (`Select` + `Grab`, `FollowsHand = false`) on `MESH_SteelExitDoorLeaf`, overhead `MESH_IsoSign_E002`, `MESH_ExitBeacon` |

The room shell uses the scenario-owned Blender-authored `MESH_WorkshopShell.fbx` (`28 × 13 × 6.5 m`, 3,516 tris, ADR-0005) inside `SCN_MachineLock` (`ENV_Room`), combined with the `18 × 11 m` `MESH_Floor` (plus solid `BOUND_WestDoorwayFloor` alcove slab), invisible bay boundary colliders, active-bay & background industrial dressing (`MESH_IndustrialWorkbench`, `MESH_ToolCabinet`, `MESH_BundDrums`, `MESH_SteelBollard`, `MESH_SheetPallet`, `MESH_OverheadConduit`, plus read-only Shared `Muletto`, racking, barrels, pallets, vents), yellow safety floor lanes, 14 UNI EN ISO 7010 signs, and four baked `MESH_Magazzino_LuceDaSoffitto.fbx` ceiling fixtures.

### 5.3 Component wiring

Each row is the serialized reference set the component reads at runtime. A
missing reference is a silent no-op in most components and a console warning in
`MachineLockScenario` only for the config, so wire these deliberately.

| Component | Lives on | Required serialized references | Drives |
|---|---|---|---|
| `MachineLockScenario` | `MNG_Scenario` | `mission = SO_Mission_MachineLock`, `config = SO_MachineLockConfig`, `spawnPoint = None`, `editorLevel = Guided`, `loadCoreInEditor = true`, `exitBeaconRenderer/Materials`, `damageFeedback` | SPI, macro-objectives, HUD, timer, Exit Beacon, edge-clamped `HUD_DecisionPanel`, `InputChannel.World` exit lock, single `EndMission` |
| `MachineLockPress` | `MESH_HydraulicPress` | `scenario`, `ramTransform`, `ramTravel = 0.6`, `displayRenderer`, `displayFaultMaterial`, `displayOffMaterial`, `estopInteractable`, `testStartInteractable`, `pressHazardTrigger`, `loopAudio`, `oneShotAudio`, `stopClip`, `clackClip` (`SFX_MechanicalButton`) | Ram motion, E-stop, power, try-start, display state |
| `MachineLockEnergyPanel` | `MESH_ElectricalPanel` | `scenario`, `config`, `press`, `mainSwitchInteractable`, `lockoutInteractable`, `testerInteractable`, `switchHandle`, lockout/tester/main LED renderers and materials, `arcVfx`, `oneShotAudio`, breaker/lockout/tester clips | 3 unnumbered IP65 Sub-Stations (6-state persistent shuffle-bag slot order): main switch, lockout gating, residual charge, LIVE/ZERO tester, gated hover feedback |
| `MachineLockAlarm` | `MESH_AlarmButton` | `scenario`, `alarmInteractable`, `beaconLight`, `beaconRotor`, `beaconDomeRenderer`, `sirenAudio`, `buttonAudio`, `buttonClip` | Alarm press, beacon, siren, subtitles |
| `MachineLockPpeCabinet` | `MESH_PpeCabinet` | `scenario`, `cabinetInteractable`, `slidingDoorLeft/Right`, `doorColliders`, `slideDoorsOpen = true`, `oneShotAudio`, `equipClip`, `openClip`, six `items` | Non-blocking sliding doors, 4-slot replacement (`Gloves`/`Body`/`Face`/`Feet`), 4/4 completion |
| `MachineLockPpeItem` | Each PPE item | `cabinet`, `slot` (`Gloves`/`Body`/`Face`/`Feet`), `isCorrect`, `displayName`, `interactable`, `itemVisualRoot`, `equippedBadgeVisual` | Correct/wrong equipment, gated hover & shelf `"<Nome DPI> indossato"` status |
| `MachineLockSpillSystem` | `HAZARD_Oil` | `scenario`, `config`, `energyPanel`, `ppeCabinet`, `oilJetVfx`, `oilLeakAudio`, `puddleQuadrants[4]`, `puddleHazardTrigger`, `approachRadius = 4`, `snapZones` (4 cone + 4 coverage) | Leak, approach hazard, baseline floor markers, 4-quadrant `MESH_OilPuddleSplat` containment completion |
| `MachineLockSnapZone` | `ZONE_ConePoint1..4`, `ZONE_Coverage1..4` | `kind` (`Cone`/`Absorbent`), `snapPoint`, `ghostVisual` (`VFX_SnapGhost`), `baselineMarkerVisual` (`VFX_BaselineMarker`), `quadrantIndex` 0–3 | Baseline Guided markers, bright snap ghosts, nearest-zone snap within `snapRadius` |
| `MachineLockCone` | `MESH_Cone` instances | `spillSystem`, `config`, `interactable`, `labelAnchor`, `oneShotAudio`, `snapClip` | Grab, release, snap, ghost preview, player-body collision ignore |
| `MachineLockAbsorbent` | Mat/sock instances | `spillSystem`, `config`, `absorbentKind`, `interactable`, `labelAnchor`, `oneShotAudio`, `snapClip` | Grab, release, snap, ghost preview, player-body collision ignore |
| `MachineLockCollisionSafety` | Static helper | None | Calls `Physics.IgnoreCollision` between grabbed/placed props and Rig `CharacterController` while preserving non-trigger raycasts |
| `MachineLockValve` | `MESH_Valve` | `scenario`, `spillSystem`, `interactable`, `wheelTransform`, `pulsingCue`, `rotationAxis`, `closedRotationAngle = 90`, `rotationSeconds`, `oneShotAudio`, `squeakClip` | Close valve, stop jet, stop pulsing cue |
| `MachineLockExtinguisher` | `MESH_WaterExtinguisher` | `scenario`, `extinguisherInteractable`, `waterSprayVfx`, `steamBurstVfx`, `smokeBurstVfx`, `oneShotAudio`, `steamBurstClip` | Water misuse, steam, Safety/Decision penalty |
| `MachineLockExitDoor` | `MESH_IndustrialDoor_RollUp` | `scenario`, `doorInteractable` (on `MESH_SteelExitDoorLeaf`), `doorLeafPivot`, `oneShotAudio`, `clickClip`, `latchClip` | Full-door + panic-bar exit confirmation (`HUD_DecisionPanel`), click-to-recenter, walk-away auto-close (`> 3.5 m`), locked player glide to `(-7.35, 0, 0)`, `+90°` outward door swing & fade-to-black exit sequence |
| `MachineLockObservationTarget` | `OBS_*` | `targetId` (`press`, `thermometer`, `display`, `puddle`, `panel`, `ppe_cabinet`), `scenario`, `config`, `targetOffset` | Gaze or proximity dwell, one score per id |
| `MachineLockDamageFeedback` | `MNG_Scenario` | URP `damageVolume`, `fallbackFlashLight`, `overlayMaterialAsset` (`MAT_DamageOverlay`), `oneShotAudio`, shock (`SFX_ElectricShock`)/burn/steam clips, `fadeIn=0.15`/`hold=0.5`/`fadeOut=1.5` | Camera post-processing check, stereo-safe peripheral red vignette overlay + respawn blink + exit fade-to-black backdrop (`renderQueue 2950` behind UI `3000`) + haptics (`accessibility.reduce_flashes`) |
| `MachineLockTexts` | Static | None | Every Italian subtitle, hint, alert, hover feedback, and debrief string |

Interaction pattern for every selectable object: `Interactable` raises select or
grab; the component calls `MachineLockScenario.Notify*`. No component polls
input and no component raycasts.

### 5.4 World labels, Audio & VFX contracts

Authoritative inventories live in [`GDD_MachineLock.md`](GDD_MachineLock.md) and [`ASSET_CREDITS.md`](ASSET_CREDITS.md) (single source of truth):

- **World labels (`LabelAnchor`, ADR-0002):** 11 sparse anchors, max 3 `LabelKind.Risk` (Press, Thermometer, Puddle), electrical bay collapsed to 1 order-neutral overhead anchor (`"Isolamento elettrico LOTO"`), 14 UNI EN ISO 7010 safety signs (`TEX_IsoSign_*`), 6 engraved brushed-aluminum plaques (`TEX_Plaque_*`), and dwell/cooldown-gated hover Action Feedback (`0.55 s` dwell / `4.0 s` global cooldown on PPE & Sub-Stations). Full table in [`GDD_MachineLock.md`](GDD_MachineLock.md) §8.4.
- **Audio (`SFX_*`):** 18 authentic 44.1 kHz mono WAV clips (`Audio/Sound/MachineLock/` + `SFX_AmbientFactory.wav` in `Audio/Music/MachineLock/`); sound-event subtitles on alarm siren, steam burst, and electric shock (`MachineLockTexts.Sound*`). Full attribution and triggers in [`ASSET_CREDITS.md`](ASSET_CREDITS.md) §4.6 and [`GDD_MachineLock.md`](GDD_MachineLock.md) §13.1.
- **Visual effects (`VFX_*`):** 10 systems (`VFX_OilJet`, `VFX_OilPuddle`, `VFX_Steam`, `VFX_Smoke`, `VFX_ElectricalArc`, `VFX_AlarmBeacon`, `VFX_DisplayFault`, `VFX_WaterSpray`, `VFX_BaselineMarker`/`VFX_SnapGhost`, `VFX_DamageVignette` + `MAT_DamageOverlay`) + overhead `MESH_ExitBeacon`. Budget enforced by validator: ≤ 200 particles/system, ≤ 4 play-on-awake systems. Full list in [`ASSET_CREDITS.md`](ASSET_CREDITS.md) §4.7 and [`GDD_MachineLock.md`](GDD_MachineLock.md) §13.2.

### 5.7 Data assets

#### `SO_MachineLockConfig` — `Level/Data/SO_MachineLockConfig.asset`

Created and owned by the scenario, assigned on `MNG_Scenario.config`.
`MachineLockConfig.CreateSpiOptions(Level)` copies the plain values into the SPI
so `MachineLockSpi` stays free of Unity dependencies.

| Group | Fields (defaults) |
|---|---|
| Time limits | `timeLimitGuided 900`, `timeLimitRealistic 600`, `timeLimitStress 480` |
| Hard end | `hardEndRealistic true`, `hardEndStress true`, `hardEndGuided false` |
| Residual energy | `energizedProbability 0.5`, `energyDecayMinSeconds 15`, `energyDecayMaxSeconds 30` |
| Observation | `observationMode Gaze`, `gazeDistance 3.5`, `gazeAngle 25`, `gazeDwell 1.2`, `proximityDistance 1.5`, `proximityDwell 0.5` |
| Containment | `snapRadius 0.6` |
| Inaction | `inactionSmokeSeconds 90`, `inactionSmokeStage2Seconds 150`, `inactionBadEndingSeconds 240` |
| Critical | `criticalCooldownSeconds 5`, `respawnDelaySeconds 2`, `criticalSafetyPenalty 10`, `waterSafetyPenalty 5`, `tryStartSafetyPenalty 5`, `criticalDecisionPenalty 10` |
| Decision penalties | `missingAlarmPenalty 8`, `missingMainPenalty 2`, `missingLockoutPenalty 2`, `missingConesPenalty 4`, `missingContainmentPenalty 5`, `timeoutPenalty 5` |
| Execution rewards | `executionEStop 2`, `executionAlarm 2`, `executionIsolation 3`, `executionZeroEnergy 2`, `executionPpe 3`, `executionContainment 2`, `executionExit 1` |
| Caps | `awarenessMax 20`, `awarenessTargets 6`, `timeMax 5` |

#### `SO_MachineLockDamageProfile` — `Level/Data/SO_MachineLockDamageProfile.asset`

Created and owned by the scenario (`URP VolumeProfile`) with a red `Vignette`
override (`(0.85, 0.05, 0.05)`, intensity `0.55`, smoothness `0.4`) and a
`ColorAdjustments` override stored as sub-assets. Assigned to
`MNG_Scenario/VFX_DamageVignette` (`Volume.sharedProfile`) and driven at runtime
by `MachineLockDamageFeedback` on `MNG_Scenario` (which also holds
`fadeInSeconds`, `holdSeconds`, `fadeOutSeconds`, `maxVignetteIntensity`,
`fallbackLightIntensity`, `forceFallbackForTesting`, and the shock/burn/steam
clips).

#### `SO_Mission_MachineLock` — edited in place

`Assets/_Project/Home/UI/Data/Missions/SO_Mission_MachineLock.asset` is
updated, never duplicated.

| Field | Value |
|---|---|
| `Id` | `08` |
| `Title` | `Machine Lock` |
| `Context` | `Ruolo: operatore di reparto. Entri nella sala di stampaggio: la pressa da 250 t lavora in modo anomalo, l'olio idraulico a 120 °C perde da un tubo e una pozza calda circonda la macchina. Metti in sicurezza la sala e avvisa il preposto.` |
| `Objectives` | 1. `Ferma la pressa e allarma` 2. `Isola l'energia e blocca` 3. `Indossa i DPI e contiene` 4. `Esci e avvisa il preposto` |
| `Criteria` Positive | `Procedura completa e area sicura` |
| `Criteria` Partial | `Area non delimitata o DPI incompleti` |
| `Criteria` Negative | `Macchina ancora attiva o errore grave` |
| `Levels` Guided | unlocked, override 0 |
| `Levels` Operational | locked, override 0 (uses 600 s) |
| `Levels` Crisis | locked, override 480 |
| `Complexity` | `4` |
| `Icon` / `Gradient` | Keep the existing values |
| `SceneName` | `SCN_MachineLock` |
| `DesignerNote` | `Guided vertical slice — G_Marius. GDD: Docs/MachineLock/GDD_MachineLock.md.` |
| `IsTraining` | `false` |

#### SPI dimensions

| Dimension | Max points | Weight |
|---|---:|---:|
| Safety | 20 | 20 % |
| Awareness | 20 | 20 % |
| Decision | 40 | 40 % |
| Execution | 15 | 15 % |
| Time | 5 | 5 % |
| **SPI** | **100** | **100 %** |

`Dimensions100()` normalizes each dimension to 0–100 for the shared score
screen. The debrief caps at 5 positive rows and 4 error rows, critical rows
first.

---

## 6. Documented deviations from GDD

### 6.1 GDD §6.1 deviations

1. **Weighted SPI (20 / 20 / 40 / 15 / 5).** The shared UI normally computes the
   SPI as an equal average of five dimensions and the guide recommends passing
   `-1`. This scenario passes an explicit weighted score because the brief
   defines those weights; the five dimension bars receive normalized 0–100
   values. Needs Team Ludovica approval. If rejected, the fallback is one method
   change inside `MachineLockSpi`.
2. **Text-only DPI.** The brief asks for wrist icons; the shared HUD exposes no
   DPI slot API. The scenario uses `SetHint`, per-item feedback and alerts. A
   future HUD widget can replace the presentation without changing gameplay
   logic.
3. **No padlock prop.** Lockout stays an abstract interaction on the electrical
   panel with a red LOCK indicator, a feedback line and the try-start proof. The
   procedure step and the score are preserved.

### 6.2 Additional implementation deviation (P8/P9)

4. **Runtime assembly added.** `SI81.MachineLock.Runtime.asmdef` is added so the
   PlayMode integration tests can compile directly against the runtime
   components without reflection or `InternalsVisibleTo`. Before it, the
   scripts lived in `Assembly-CSharp` and were unreachable from an `.asmdef`
   test assembly. This changes no behaviour at runtime; it moves the runtime
   scripts into a named assembly referencing `SI81.MachineLock`, `SI81.UI`,
   `SI81.Input`, `SI81.Interaction`, `SI81.Rig`,
   `Unity.RenderPipelines.Core.Runtime`, and
   `Unity.RenderPipelines.Universal.Runtime`.

---

## 7. Implementation phases

Each phase is one GitHub issue. A phase closes only after its tests pass and its
evidence is attached to the issue. The full work breakdown is in GDD §15.

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

---

## 8. Reproducible verification

Run every command from the repository root. `Unity.exe` below is a placeholder
for the local `6000.3.14f1` editor binary; substitute the absolute path.

### 8.1 CLI — build, validator, tests, previews, and deploy

Use the repo wrapper [`scripts/unity-run.ps1`](../../../../../../scripts/unity-run.ps1) from the repository root (resolves Unity `6000.3.14f1` and ADB automatically, omits `-nographics` on Windows OpenXR PlayMode/preview/APK runs, and restores URP auto-touched Shared materials):

```powershell
.\scripts\unity-run.ps1 hygiene         # Git-only, banned API, .meta, protected path checks
.\scripts\unity-run.ps1 build-scene     # Rebuild textures, materials, plaques, prefabs, SCN_MachineLock
.\scripts\unity-run.ps1 validate        # Validator (Errors=0 Warnings=0, FloorContinuity, 46/46 ASTC, 14/14 ISO 7010)
.\scripts\unity-run.ps1 score-tests     # Editor score checks (42 assertions)
.\scripts\unity-run.ps1 editmode        # EditMode SPI & pattern tests (42 tests)
.\scripts\unity-run.ps1 playmode        # PlayMode integration tests (33 tests, no -nographics)
.\scripts\unity-run.ps1 rig-tests       # Shared rig regression (25 tests, no -nographics)
.\scripts\unity-run.ps1 inspect-scene   # Triangle budget top-25 & workshop shell bounds
.\scripts\unity-run.ps1 capture-views   # Render 9 offscreen 1280x720 preview PNGs to Logs/previews/
.\scripts\unity-run.ps1 build-apk       # Build Builds/SI81_MachineLock_Quest3.apk
.\scripts\unity-run.ps1 deploy-apk      # ADB install to connected Quest 3
```

What the validator log must contain (`[MachineLockValidator]` lines):

| Line | Expected |
|---|---|
| `Errors=… Warnings=…` | `Errors=0 Warnings=0` |
| `WorkshopShell …` | `WorkshopShell Renderers=1 Tris=3516 Budget=12000` |
| `SplatOilPuddle …` | `SplatOilPuddle Quadrants=4 Tris=1536` |
| `Iso7010Signs=…` | `Iso7010Signs=14/14` |
| `AstcTexturesChecked=…` | `AstcTexturesChecked=46/46 BadTextureRefs=0` |
| `Prefabs=… MissingScripts=…` | `Prefabs=12 MissingScripts=0` |
| `LabelAnchors=… RiskLabels=…` | `LabelAnchors=11 RiskLabels=3 RiskCap=3` |
| `Triangles=… Budget=100000` | triangles ≤ 100000 (`52,818` actual) |
| `RealtimeLights=… ShadowLights=…` | ≤ 2 realtime/mixed, ≤ 1 shadow caster |
| `PATTERN_PuddleA/B/C DryMargin …` | every side ≥ 2.50 m |
| `ElectricalSubStations …` | separation ≥ 0.55 m (`0.88 m` actual) |
| `PpeWiring …` | `Correct=4 Distractors=2 HasFeet=True DistractorsOnGlovesAndFace=True` |
| `AudioClips=…` | `AudioClips=18/18 AudioWiringVerified=True` |
| `Runtime …` | `ExitDoors=1 LegacyTriggers=0` |

### 8.2 Editor menu items

| Menu item | Use |
|---|---|
| `SI81/MachineLock/Build Full Scene` | Rebuild the whole P2 blockout and scene from `MachineLockSceneBuilder` |
| `SI81/MachineLock/Build Scene Shell` | Rebuild only the P1 shell (mission, config, spawn, Build Settings) |
| `SI81/Scenari/Valida scenario` | Shared scenario validator on the open scene |
| `SI81/MachineLock/Run Score Tests` | Pure C# SPI and time assertions from GDD §11 and §15; logs one PASS/FAIL row per case |
| `SI81/MachineLock/Log State` | Logs task flags, dimensions, SPI, elapsed, state, outcome |
| `SI81/MachineLock/Force Energized` | Play Mode only: oil stays live |
| `SI81/MachineLock/Force Zero Energy` | Play Mode only: oil is immediately safe |
| `SI81/MachineLock/Complete Current Objective` | Play Mode only: QA shortcut |

### 8.3 Git hygiene

```powershell
# Version control hygiene — must print nothing
git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json

# No XRI or OVRInput in the scenario code — must print nothing
git grep -n -E "OVRInput|XRGrabInteractable|XRRayInteractable|XRInteractionManager" `
  -- Assets/_Project/Groups/G_Marius/Code/Scripts/MachineLock

# Branch and tree state
git branch --show-current
git status
```

### 8.4 Play Mode flow (manual)

1. Open `SCN_Core`, press Play.
2. Home → card 08 → Mission Brief → Guided → Avvia.
3. Intro subtitles; four macro-objectives visible; timer elapsed.
4. Observe all six targets; hint reaches 6/6.
5. E-stop on the west pedestal; press stops; display stays on.
6. Alarm; beacon and siren.
7. Main off; display dark; lockout; red LED.
8. Tester: LIVE then ZERO after the configured decay; try-start does not move.
9. PPE: wrong items first, then the correct four; hint 4/4.
10. Valve closed; cones 4/4; absorbents 4/4; hazard off.
11. Aim at the West exit door leaf or panic bar and press Trigger/Grip; confirm `"Sì, esci dal reparto"` on `HUD_DecisionPanel`; player locks and glides to the door, door swings `+90°` outward, room fades to black behind end UI; supervisor line → outcome → debrief → score → Home.

---

## 9. Automated QA evidence summary

| Phase | Scope | Commit | Status |
|---|---|---|---|
| P0 | Branch, Git hygiene, baseline, docs | `140b9cd` | **Verified** — `collab-proxy` grep empty; batchmode compile clean |
| P1 | Mission data in place, core assembly, scene shell | `1276dfc` | **Verified** — mission validates; spawn `(-5.2, 0, 0)`; no camera/Canvas |
| P2 | Room blockout, 12 prefabs, materials, 13 labels, credits | `0ffeb12` | **Verified** — validator P2 checks pass; 13 anchors / 3 Risk |
| P3 | Core interactions | `9d07050` | **Verified** — PlayMode tests 1–7 passed; validator `Press=1 Alarm=1 Panel=1 Extinguisher=1` |
| P4 | PPE | `ce82f87` | **Verified** — PlayMode tests 8–9 passed; validator `Cabinet=1 PpeItems=6` |
| P5 | Spill containment | `8afca7f` | **Verified** — PlayMode tests 10–13 passed; validator `Valve=1 Spill=1 Cones=4 Absorbents=4 SnapZones=8` |
| P6 | Observation and Awareness | `bd70447` | **Verified** — PlayMode tests 14–15 passed; validator `ObservationTargets=6` |
| P7 | SPI, time modes, debrief, outcomes | `61671d3` | **Verified** — EditMode `SI81.MachineLock.Tests` 34/34 passed; `RunScoreTests` 40/40 passed |
| P8 | Damage feedback, audio, VFX, lighting | `54b2915` | **Verified** — PlayMode test 16 passed; validator `AudioClips=16/16`, `ParticleSystems=5 PlayOnAwake=2`, `DamageProfile Vignette=True ColorAdjustments=True` |
| P9 | QA, Quest gate, PR | `23bb11b` | **Automated QA verified** — Initial P0–P9 baseline |
| Issue #35 | Scene redesign, asset upgrade, collision fixes, Quest 3 polish | `a6a513f` | **Verified** — Initial redesign baseline (`90,356` tris) |
| Issue #36 (#37–#42) | Workshop shell, hero assets, PPE models, ISO 7010 signage, audio & exit flow overhaul | `59324ba` | **Verified & Closed** — Validator `0/0` (`52,818` tris); Score `42/42`; EditMode `41/41`; PlayMode `30/30`; Rig `25/25`; 9 preview PNGs |
| Issue #43 | Quest 3 exit door interaction/panel anti-clipping, locked fade-to-black exit & randomized unnumbered Sub-Stations | `88ab85f`, `85eb1b0` | **Verified & Closed** — Validator `0/0`; Score `42/42`; EditMode `42/42`; PlayMode `33/33`; Rig `25/25`; Quest 3 headset verified |
| Cleanup | Remove superseded `OldIndustrialBuilding` shared assets | `596208d` | **Verified** — Zero references outside `G_Marius` |

Latest verified test evidence:

| Check | Result |
|---|---|
| `MachineLockSceneBuilder.BuildFullScene` | **PASS** (`ExitCode=0`: 12 prefabs, 14 ISO 7010 signs, 6 brushed-metal plaques, 46 ASTC textures, 18 audio clips, `SO_MachineLockDamageProfile`) |
| `MachineLockValidatorRunner.ValidateScene` | **PASS** (`ExitCode=0`, `Errors=0 Warnings=0`, `Shared scenario validator returned 0 issue(s)`, `WorkshopShell Renderers=1 Tris=3516 Budget=12000`, `SplatOilPuddle Quadrants=4 Tris=1536`, `Iso7010Signs=14/14`, `AstcTexturesChecked=46/46 BadTextureRefs=0`, `Prefabs=12 MissingScripts=0`, `SceneMissingScripts=0`, `Triangles=52818 Budget=100000`, `RealtimeLights=1 ShadowLights=0`, `LabelAnchors=11 RiskLabels=3 RiskCap=3`, `FloorContinuity Probes=7/7 FloorColliders=2`, `ElectricalSubStations Separation Main-Lock=0.88m Lock-Tester=0.88m Main-Tester=1.76m`, `PpeWiring Correct=4 Distractors=2 HasFeet=True DistractorsOnGlovesAndFace=True`, `Runtime Press=1 Alarm=1 Panel=1 Extinguisher=1 Cabinet=1 PpeItems=6 Valve=1 Spill=1 Cones=4 Absorbents=4 SnapZones=8 ObservationTargets=6 DamageFeedback=1 ExitDoors=1 LegacyTriggers=0`, `ParticleSystems=5 PlayOnAwake=2 MaxParticlesBudget=200 PlayOnAwakeBudget=4`, `AudioClips=18/18 AudioWiringVerified=True`, `DamageProfile Vignette=True ColorAdjustments=True`, dry margins A `5.50/5.50/3.00/3.00`, B `6.20/5.20/3.30/2.90`, C `5.20/6.00/2.90/3.40`, min required `2.50 m`) |
| `MachineLockDebugMenu.RunScoreTests` | **PASS** (`ExitCode=0`, `42/42 passed, 0 failed`) |
| EditMode `SI81.MachineLock.Tests` | **PASS** (`ExitCode=0`, `42/42 passed, 0 failed, 0 skipped`) |
| PlayMode `SI81.MachineLock.PlayModeTests` | **PASS** (`ExitCode=0`, `33/33 passed, 0 failed, 0 skipped`; run in `-batchmode` without `-nographics`) |
| PlayMode `SI81.Rig.Tests` regression | **PASS** (`ExitCode=0`, `25/25 passed, 0 failed, 0 skipped`; run in `-batchmode` without `-nographics`) |
| `MachineLockDebugMenu.BuildQuest3Apk` + ADB | **PASS** (`ExitCode=0`, `[SI81 Build]` controllers-only guard logged, `Builds/SI81_MachineLock_Quest3.apk` `95.29 MB`, `adb -s 2G0YC5ZG1402NV install -r` `Success`) |
| Hygiene greps (`collab-proxy`, `OVRInput\|XRI`) | **PASS** (`scripts/check-hygiene.ps1` `0 issues`; `Assets/_Project/Home/` untouched beyond `SO_Mission_MachineLock.asset`) |
| Home → mission → Home manual interactive check | **PASS** (verified on physical Meta Quest 3) |
| Physical Quest 3 gate | **PASS** (verified on physical Meta Quest 3 `2G0YC5ZG1402NV` on 2026-11-27) |

---

## 10. Quest 3 device gate checklist

Device `2G0YC5ZG1402NV` (Meta Quest 3) was verified on 2026-11-27 with the final
`Builds/SI81_MachineLock_Quest3.apk` build (`85eb1b0`). All on-device checks
passed.

Legend: `[ ]` not run · `[x]` passed on device · `[!]` failed, with a note.

### 10.1 GDD §16.5 — scenario on-device

- [x] One rig, one camera, one EventSystem; the scenario scene adds none.
- [x] Spawn exactly at the door; no double offset.
- [x] Controller models and rays visible; trigger selects; grip grabs.
- [x] Labels readable at 0.75–4 m; at most 3 Risk labels at once.
- [x] Wrist panel readable; hints update through the flow.
- [x] Subtitles readable; alarm, steam and shock sound events are subtitled.
- [x] Vignette visible, or the fallback light fires.
- [x] No console errors; `[SI81 Input]` lines normal.
- [x] Stable 72 Hz for a 15-minute run; no thermal throttling.

### 10.2 `SharedPlayerRig.md` — shared rig gate

- [x] Exactly one rig and one camera in `SCN_Core`; no rig in the scenario scene.
- [x] Floor height correct; head and controllers track.
- [x] Controller models follow the hands and the aim ray.
- [x] Left stick moves relative to gaze; right stick snaps 45°.
- [x] No drift at rest (deadzone) and capped speed.
- [x] Left Menu opens and closes pause; movement stops while pause/menu is open.
- [x] B/Y is inert during a mission and goes Back only while the menu is open.
- [x] Trigger hovers/clicks UI through the existing ray; a UI click does not
      select a world object behind it.
- [x] Haptics pulse on menu/pause/alerts/interaction and respect
      `controls.ui_haptics`.
- [x] Logcat clean of `[SI81 Input]` duplicate-instance, missing-bridge and rig
      errors.

### 10.3 Scenario-specific device checks

- [x] E-stop stops the ram, keeps the display on and does not isolate energy.
- [x] Alarm raises the beacon and the siren.
- [x] Main switch darkens the display; lockout is refused while power is on.
- [x] Tester reads LIVE, then ZERO after the decay; try-start stays still.
- [x] Wrong PPE (Latex Gloves on `Gloves`, Surgical Mask on `Face`) does not protect; equipping all 4 correct items (Heat Gloves, Chemical Apron, Face Shield, Safety Boots S3) clears `HasWrongPpeEquipped` and reaches 4/4.
- [x] Wide sliding-door PPE cabinet opens without blocking the player; hero PPE meshes and `"<Nome DPI> indossato"` hover/badge feedback are readable.
- [x] Three unnumbered IP65 electrical Sub-Stations (Main Disconnect, Lockout Station, Voltage Tester) are physically separated (`0.88 m`), shuffle across slots without back-to-back repeats across runs, and show crisp brushed-metal plaques with zero overlapping World Labels.
- [x] Holding a cone or absorbent near the player body never clips or pushes the player back; baseline floor rings appear after 4/4 PPE, brighten into green snap ghosts on hold, and hide when placed over the 4-quadrant `MESH_OilPuddleSplat`.
- [x] Bright red pulsing Valve on the east oil pipe is immediately visible and stops the oil jet on trigger.
- [x] Critical Event triggers a clear stereo-safe peripheral red vignette overlay + red flash + dual-hand haptics + brief dark respawn blink on teleport (without modifying `MNG_CoreRig.prefab`).
- [x] Overhead Exit Beacon turns bright green once `TaskEStop && TaskContainment`; aiming and clicking Trigger/Grip anywhere on the West exit door leaf or panic bar opens the edge-clamped `HUD_DecisionPanel` cleanly inside the bay; cancelling resumes play; confirming locks world input, glides the player to the door, swings the door `+90°` outward, fades to pure black behind end UI, and opens Outcome → Debrief (including Early Exit with `OutcomeType.Negative` when `!TaskEStop`).
- [x] Authentic workshop audio plays cleanly across all 18 clips (`SFX_MechanicalButton`, `SFX_AlarmSiren`, `SFX_ElectricShock`, industrial press, LOTO switches, fabric foley, pressurized steam/leak, burn, machine-hall ambience) alongside crisp UNI EN ISO 7010 safety signage.
- [x] Debrief shows at most 5 positive and 4 error rows, critical first.
- [x] Operational and Crisis cards remain locked in the mission list.

---

## 11. Out of scope for this branch

- Operational and Crisis playable cards (logic implemented and tested, cards
  stay locked).
- Multiplayer, networking, hand tracking, XR Interaction Toolkit.
- Camera, Canvas, EventSystem, rig or HUD authored inside the scenario scene.
- A physical padlock/tag prop; lockout is an abstract panel interaction.
- DPI wrist icons; DPI feedback is text-only.
- Final art, final audio, final lighting bake, decision-timeline debrief.

## 12. Open items

- Final press, prop and audio assets from the owner.
- Safety representative sign-off on the procedure and every Italian string.
- Team Ludovica approval for the weighted SPI and a future DPI HUD widget.
- Quest 3 validation of the URP damage vignette vs. fallback path.
- Final lighting bake and performance pass.
- Decision on when to unlock Operational and Crisis after QA.

---

## 13. Pull request draft

Copy the block below into the GitHub PR once `scenario/machine-lock` is synced
with `origin/main`.

````markdown
## Summary

Scenario 08 Machine Lock — Guided vertical slice, end to end, on the shared rig.

```text
SCN_Core (always loaded)
└── SCN_MachineLock (additive)
    ├── MNG_Scenario           MachineLockScenario + SO_MachineLockConfig + DamageFeedback
    ├── MNG_SpawnPoint         SI81.Rig.SpawnPoint (-7.6, 0, 0)
    ├── ENV_Room / ENV_Press / ENV_Safety  (MESH_WorkshopShell 28×13×6.5 m + 18×11 m bay + 14 ISO 7010 signs + MachineLockExitDoor)
    ├── HAZARD_Oil             4-quadrant MESH_OilPuddleSplat, hazard trigger, 4 coverage zones + baseline markers
    ├── ZONE_Delimitation      4 cone zones + baseline markers
    ├── MNG_LayoutPatterns     Patterns A/B/C (persistent shuffle-bag rotation)
    └── MNG_ObservationTargets 6 Awareness targets
```

New assemblies: `SI81.MachineLock.Runtime` (P8/P9) and
`SI81.MachineLock.PlayModeTests` (P9). EditMode `SI81.MachineLock.Tests` keeps
the SPI and pattern selector pure C#.

Documented deviations from GDD §6.1: weighted SPI 20/20/40/15/5, text-only DPI,
abstract lockout without a padlock prop, low-poly hero PPE meshes holding the
100k Quest 3 budget (`52,818` tris actual), plus the runtime assembly added for
compile-time PlayMode tests.

## Evidence

- **Before:** P9 graybox room (`23bb11b`, `12×8×4 m`, primitive stand-ins, cone
  no-clip bug, swinging PPE door, overlapping labels, invisible damage feedback
  when camera post-processing is off, synthesized SFX) and initial `#35`
  `OldIndustrialBuilding` shell (`90,356` tris).
  **After (`#36` + `#43`):** `MachineLockValidatorRunner.ValidateScene` → `Errors=0 Warnings=0`;
  `Shared scenario validator returned 0 issue(s)`; `WorkshopShell Renderers=1 Tris=3516 Budget=12000`;
  `SplatOilPuddle Quadrants=4 Tris=1536`; `Iso7010Signs=14/14`;
  `AstcTexturesChecked=46/46 BadTextureRefs=0`; `LabelAnchors=11 RiskLabels=3`;
  `Prefabs=12 MissingScripts=0`; `SceneMissingScripts=0`; `Triangles=52818`
  (budget `100000`); `RealtimeLights=1 ShadowLights=0`; `AudioClips=18/18 AudioWiringVerified=True`;
  `ParticleSystems=5 PlayOnAwake=2`; `FloorContinuity Probes=7/7 FloorColliders=2`;
  `ElectricalSubStations Separation Main-Lock=0.88m Lock-Tester=0.88m Main-Tester=1.76m`;
  `PpeWiring Correct=4 Distractors=2 HasFeet=True DistractorsOnGlovesAndFace=True`;
  `ExitDoors=1 LegacyTriggers=0`; `DamageProfile Vignette=True ColorAdjustments=True`.
- `MachineLockDebugMenu.RunScoreTests`: `42/42 passed, 0 failed`.
- EditMode `SI81.MachineLock.Tests`: `42/42 passed, 0 failed, 0 skipped`.
- PlayMode `SI81.MachineLock.PlayModeTests`: `33/33 passed, 0 failed, 0 skipped`
  (executed in `-batchmode` without `-nographics`).
- PlayMode `SI81.Rig.Tests` regression: `25/25 passed, 0 failed, 0 skipped`
  (executed in `-batchmode` without `-nographics`).
- `scripts/check-hygiene.ps1`: `PASS (0 issues)` (`collab-proxy` empty, `OVRInput|XRI` empty).
- Manual flow (Home → 08 → Mission Brief → Guided → Avvia → … → exit confirmation → debrief → Home):
  **PASS** on physical Meta Quest 3.
- Quest 3 device gate (§10 checklist): **PASS** on physical Meta Quest 3
  (serial `2G0YC5ZG1402NV`, APK `Builds/SI81_MachineLock_Quest3.apk`, `95.29 MB`).

## Merge Danger

**Door:** two-way.

Scenario-local code, assets and one edited mission asset (plus removal of the
unreferenced `PFB_OldIndustrialBuilding` shared assets in `596208d`); no package
changes and no shared-scene edits. Reverting the merge removes the scenario
without touching the rig or the Home UI.

**Blast Radius:** scenario.

Adds `SI81.MachineLock.Runtime` and `SI81.MachineLock.PlayModeTests`, edits
`SO_Mission_MachineLock` in place, and adds `SCN_MachineLock` to Build Settings
index 2. The main shared risk is a bad mission asset or a scene that adds rig/UI
infrastructure; both are covered by the validator and the rig regression.
````

### PR hygiene

- [ ] Branch `scenario/machine-lock` is up to date with `origin/main`.
- [x] `git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json` prints nothing.
- [x] `git grep -n -E "OVRInput|XRGrabInteractable|XRRayInteractable|XRInteractionManager" -- Assets/_Project/Groups/G_Marius/Code/Scripts/MachineLock` prints nothing.
- [x] Validator log attached with `Errors=0 Warnings=0`.
- [x] EditMode and PlayMode XML results attached.
- [x] Quest 3 checklist completed on device with notes for every failure.
- [x] `ASSET_CREDITS.md` covers every asset added since P2, including audio and VFX.
- [x] Every commit message follows Conventional Commits.
