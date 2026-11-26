# Machine Lock (Scenario 08) — Technical Game Design Document

| Field | Value |
|---|---|
| Document | GDD_MachineLock.md |
| Version | 1.3 (Issue #36 — ADR-0005 & ADR-0006) |
| Date | 2026-11-26 |
| Owner | G_Marius |
| Status | Approved for implementation — Custom low-poly workshop shell, hero asset cohesion, ISO 7010 signage & exit decision flow |
| Source brief | Project Work VR — Scenario 5 Machine Lock (ID 8) |
| Target | Unity 6000.3.14f1, Meta XR Core SDK 209.0.0, OpenXR 1.16.1, Quest 3, controllers only |
| Playable scope | Guided. Operational and Crisis are locked for this PR; their time logic is implemented and unit-tested. |

---

## 1. Summary

The player is a department operator entering a stamping room during an ongoing emergency.

A 250-ton hydraulic press is running irregularly. The cooling system has failed, the hydraulic oil is at approximately 120 °C, and a thin jet from a cracked pipe is feeding a hot oil puddle around the machine. The press display reports a fault. The emergency stop mushroom and the department alarm are next to the machine. The main electrical panel, the PPE cabinet, the spill kit and a water extinguisher are distributed around the room.

The player must:

1. Observe the scene.
2. Stop the press and raise the alarm.
3. Isolate and lock out the main power.
4. Verify zero residual energy.
5. Wear the correct PPE.
6. Delimit and contain the spill.
7. Leave the room and report to the supervisor.

The scenario ships as a **Guided vertical slice**. The SPI and time system support all three time modes: Guided, Realistic (project level `Operational`) and Stress (project level `Crisis`).

---

## 2. Learning objectives

By the end of the scenario the player should be able to:

- Recognise a hydraulic press emergency and the difference between **stopping motion** and **isolating energy**.
- Apply a simplified **Lockout/Tagout** sequence: stop, isolate, lock, try-start, verify zero energy.
- Understand that an emergency stop does **not** remove residual energy.
- Select PPE appropriate to hot oil and fumes, and reject unsuitable PPE.
- Delimit and contain a hot oil spill without using water.
- Escalate the emergency to the supervisor.

This scenario is educational and does not replace mandatory safety training. The procedure and all player-facing texts must be reviewed by a qualified safety representative before release.

---

## 3. Scope

### 3.1 In scope

- **Guided playable** in this PR.
- **All three time modes implemented and unit-tested**: Guided 900 s soft, Realistic 600 s hard, Stress 480 s hard.
- Blockout environment with placeholder props; final models and audio can replace them later without renaming roots.
- Full interaction set: E-stop, alarm, main switch, lockout, voltage tester, try-start, PPE, valve, cones, absorbent mats/socks, water extinguisher, exit.
- Three layout patterns and a random residual-energy state.
- Weighted SPI scoring, debrief lines, outcome, inaction bad ending.
- Italian player-facing text; English code, comments and documentation.
- A pure C# SPI with EditMode tests and an Editor test menu fallback.

### 3.2 Out of scope

- Operational and Crisis **playable** cards in this PR. They stay locked in the mission data; unlocking is a data-only change after QA.
- Level-specific gameplay differences beyond time. The content is identical across modes; only the time rule and the common HUD level settings differ.
- Multiplayer, networking, hand tracking, XR Interaction Toolkit.
- Custom Canvas, EventSystem, camera, rig, HUD or UI in the scenario scene.
- A physical padlock/tag prop; lockout is represented abstractly on the panel.
- DPI wrist icons; DPI feedback is text-only (hint + alert/feedback).
- Final art, final audio, final lighting bake.
- The Debrief choice timeline (`RecordChoice` is not implemented in the common UI).

---

## 4. Glossary

| Term | Meaning in this scenario |
|---|---|
| **Machine Lock** | Scenario title; also the state of a machine isolated and locked out. |
| **LOTO** | Lockout/Tagout: isolate, lock, verify zero energy. |
| **Zero energy** | No residual electrical or hydraulic energy can cause harm. |
| **Try-start** | Attempt to start the machine after isolation; it must not move. |
| **E-stop** | Emergency stop mushroom. Stops motion; does not isolate power. |
| **Main disconnect** | The main switch on the electrical panel; removes machine power. |
| **Lockout point** | Abstract lock interface on the panel; represents the padlock. |
| **Residual energy** | Electrical charge remaining in the oil after the main switch is opened. |
| **Hot oil** | Hydraulic oil at approximately 120 °C. |
| **Puddle** | Hot oil spill around the press. |
| **Containment** | Absorbent mats/socks placed on the puddle. |
| **Delimitation** | Cones placed around the puddle. |
| **DPI / PPE** | Personal protective equipment. |
| **SPI** | Safety Performance Index, 0–100. |
| **Safety** | Physical integrity dimension, max 20. |
| **Awareness** | Scene observation dimension, max 20. |
| **Decision** | Procedural correctness dimension, max 40. |
| **Execution** | Operational skill dimension, max 15. |
| **Time** | Temporal efficiency dimension, max 5. |
| **DP** | Decision point. |
| **Bad Ending** | Critical consequence; in Guided it is a penalty plus feedback, not an immediate end. |
| **Partial** | Safe but incomplete result. |
| **Positive** | Complete and safe result. |
| **Negative** | Unsafe result; Bad Ending 2 or a hard-time expiry with the press running. |
| **HUD** | Common wrist panel, alerts, subtitles and world labels. |
| **SCN_Core** | Shared scene owning rig, UI and HUD. |
| **ScenarioController** | Base class of the scenario script. |
| **Interactable** | Shared select/grab component. |
| **LabelAnchor** | Shared world-label component. |
| **SpawnPoint** | Shared rig spawn marker. |
| **Validator** | `SI81/Scenari/Valida scenario`. |

---

## 5. Narrative and player role

- **Role:** department operator, not a maintenance technician.
- **Situation:** the player enters the stamping room and finds the emergency already in progress.
- **Tone:** urgent, factual, non-judgmental. The system reports; it does not lecture.
- **Opening subtitle:** `Sistema`: “Perdita olio a 120 gradi.” then “Pressa in movimento.”
- **Closing subtitle:** `Preposto`: “Sala in sicurezza. Ottimo lavoro.” or “La sala non è ancora sicura.”
- **Exit confirmation (`AskDecision`):** Activating the west steel exit door panic bar opens the shared `HUD_DecisionPanel` via `ScenarioController.AskDecision` (“Vuoi uscire dal reparto adesso?” → “Sì, esci dal reparto” / “No, resta nella stanza”) before ending the mission (ADR-0006).

---

## 6. Integration constraints

These are mandatory. A violation breaks the shared application.

| Rule | Requirement |
|---|---|
| Scene model | `SCN_Core` is always loaded. `SCN_MachineLock` loads additively. |
| No local infrastructure | No Camera, AudioListener, OVRCameraRig, XR Origin, Canvas, EventSystem, InputModule, `MNG_UI`, `MNG_HUD`, `HUD_*`, `PANEL_*`, `CANVAS_*`, XR Interaction Toolkit, `OVRInput`. |
| Input | Only `SI81.Input.InputService`; scenario actions are exposed through `Interactable`. |
| Interaction | `SI81.Interaction.Interactable` + the rig’s `RayInteractor`. No local raycast, hover or grab code. |
| UI | Only `ScenarioController` API, `HUDService`, `LabelAnchor`, `ScenarioTrigger`. No custom UI. |
| Mission | `SO_Mission_MachineLock` already exists. Edit it in place; do not duplicate it. |
| Scene | `SCN_MachineLock`, added to Build Settings after `SCN_Core` and `SCN_MockScenario`. |
| Spawn | Use `SI81.Rig.SpawnPoint` on `MNG_SpawnPoint`. Leave `ScenarioController.spawnPoint` empty to avoid double offset. |
| Validator | `SI81/Scenari/Valida scenario` must report 0 errors. Warnings must be 0 or explained. |
| Naming | SI81 prefixes; PascalCase after the prefix; no spaces, accents or extra underscores. |
| Language | Player-facing text in Italian. Code, comments, docs and commit messages in English. |
| Git | Git only. No Unity Version Control, no `com.unity.collab-proxy`, no Project Setup Tool “Fix All”. |
| Shared assets | Copy Shared props into `G_Marius` before editing. Never edit `Shared/` originals. |
| End mission | `EndMission` exactly once on every path. |

### 6.1 Documented deviations

1. **Weighted SPI.** The common UI normally computes the SPI as the simple average of five dimensions and the guide recommends passing `-1`. This scenario passes an explicit weighted score because the brief defines weights 20/20/40/15/5. The five dimension bars receive normalized 0–100 values. This deviation must be approved by Team Ludovica; if rejected, switch the SPI to equal weights.
2. **Text-only DPI.** The brief asks for wrist icons; the common HUD has no DPI slot API. This version uses `SetHint` and alerts. A future HUD widget can replace it without changing gameplay logic.
3. **No padlock prop.** Lockout is an abstract interaction on the panel with a red indicator and feedback. The procedure step and the score are preserved.
4. **Low-poly Surgical Mask distractor.** `MASCHERINA.fbx` weighs `15,946` triangles for a 24 cm shelf distractor; `MachineLockSceneBuilder` models a crisp pleated blue disposable surgical mask (~72 tris) so the full `MESH_WorkshopShell` hall stays well under half of the `100,000`-triangle Quest 3 budget (`~41.4k` tris).

---

## 7. Level design

### 7.1 Room & Industrial Hall (ADR-0005, supersedes ADR-0001)

- Building shell: Scenario-owned Blender-authored **28.0 m × 13.0 m × 6.5 m** Italian industrial stamping workshop shell (`≤ 12,000` tris) textured with PolyHaven CC0 PBR concrete, structural steel I-beams, roof trusses, high clerestory windows, and overhead bay fixtures (replaces `PFB_OldIndustrialBuilding.fbx` to reclaim ~35k+ triangles within the `100,000`-triangle Quest 3 budget).
- Active stamping bay: **18.0 m (x) × 11.0 m (z)** (`x ∈ [-9.0, +9.0]`, `z ∈ [-5.5, +5.5]`) in the center of the hall, bounded by safety barriers (`MESH_Magazzino_Barriera.fbx`) with rich workshop dressing inside the bay edges (workbenches, tool cabinets, sheet-stock pallets, spill bund drums, bollards, conduits) and a dressed background factory zone beyond the barriers (forklift, loaded racking, crates, compressor/generator).
- Floor at `y = 0`; the rig uses `CharacterController` and gravity.
- One industrial steel emergency **Exit Door** on the **west** side at `(-9.0, 0, 0)` with an interactable green **panic bar** (*maniglione antipanico*, no `MESH_DoorRollHousing`), topped with an illuminated overhead `E001`/`E002` exit sign and `MESH_ExitBeacon` that turns bright green once `TaskEStop && TaskContainment`.
- The player spawns just inside the west exit at `(-7.6, 0, 0)`, facing east (`yaw 90°`) toward the press and E-stop pedestal.
- Activating the panic bar opens `SCN_Core`'s shared `HUD_DecisionPanel` via `ScenarioController.AskDecision` to confirm leaving; on confirmation, the steel door swings open ~90° and the mission transitions to the Supervisor line and Debrief (ADR-0006).
- Yellow safety floor lanes frame the spill perimeter, the outer Dry Perimeter walkway, and station aprons.
- Authentic **UNI EN ISO 7010 / D.Lgs. 81/08 Safety Signs** (`W012`, `W017`, `W024`, `W011`, `P011`, `M008`, `M009`, `M013`, `M026`, `F001`, `F005`, `E001`/`E002`) are mounted cleanly on the walls in all difficulty modes, paired with compact brushed-metal **Diegetic Plaques** on the electrical Sub-Stations and press controls only (redundant pixel-art prop plaques removed).

### 7.2 Press and puddle

- The press is at the **centre** of the bay, footprint **3.0 m × 2.5 m**, height 4.35 m, built from custom Blender hero meshes (`MESH_HydraulicPressBody.fbx` + `MESH_HydraulicPressRam.fbx`) textured with Polyhaven CC0 `MAT_PressSteel_Green` (`green_metal_rust`), chrome tie rods, twin hydraulic cylinders, cantilevered HMI console, analog 120 °C gauge, and an engraved brushed-aluminum nameplate.
- The puddle is an organic **4-quadrant SPLAT hot-oil spill** (`MESH_OilPuddleSplat.fbx` derived from `MESH_SPLAT.glb`) around the press with dark glossy wet-oil PBR ripples (`MAT_Oil`): outer footprint up to **7.0 m × 5.0 m**.
- The **Dry Perimeter** is at least **2.5 m** wide on every side across all three Layout Patterns (A/B/C), guaranteeing comfortable Quest 3 locomotion around the spill.
- The leak is on the press east manifold at `(2.05, 0.95, 0.55)`; an exposed steel pipe leads directly out to an unmissable bright red pulsing **Valve** at `(4.75, 0.95, 0.55)`, cleanly reachable from the Dry Perimeter without stepping in oil.
- The press display and thermometer are on the **west face**, visible from the E-stop pedestal.
- The E-stop mushroom (wired to `SFX_MechanicalButton.wav`) and green `TEST AVVIO` button sit on a free-standing industrial pedestal (`MESH_PULSANTI_TEAM.fbx` + engraved brushed-aluminum plaque) at `(-5.6, 0, 0)`, facing west directly toward Spawn.
- The main electrical bay on the **east wall** at `(8.72, 1.28, 0)` is expanded into **three physically separated (≥ 0.88 m), cohesive IP65 steel Sub-Stations** with compact recessed pilot LEDs and engraved brushed-aluminum plaques:
  1. `Main Disconnect Switch` (IP65 enclosure + yellow escutcheon + rotary red throw handle, south sub-unit)
  2. `Lockout Station` (safety-yellow/slate IP65 enclosure + steel padlock hasp & parked padlock, center sub-unit)
  3. `Voltage Tester` (authentic industrial voltage tester / multimeter cabinet with probes, dial & LIVE/ZERO LEDs, north sub-unit)
- The alarm button (looping spatial `SFX_AlarmSiren.wav`) is mounted on an industrial post on the west-north side at `(-8.50, 1.45, 2.4)` beneath an ISO 7010 `F005` wall sign.
- The wide sliding-door PPE cabinet, red Spill Kit (with un-squashed `MESH_TrafficConeHero`, quilted `MESH_AbsorbentMatHero`, and fabric-sleeved `MESH_AbsorbentSockHero`), and un-squashed `MESH_WaterExtinguisherHero` are positioned per active Layout Pattern (A/B/C), always facing inward toward the Dry Perimeter.

### 7.3 Top-down reference

```
z = +5.5  +--------------------------------------------------+
          |  [alarm W-N]        [extinguisher (pattern)]     |
          |                     +-----------+                |
          |  [exit W] [spawn]   |   PRESS   |  [valve E]     |
          |     [E-stop ped.]   |  3 x 2.5  |  [3x Elec Bay] |
          |                     +-----------+                |
          |                  ( 7x5 puddle ring )             |
          |  [spill kit (pattern)]      [PPE cabinet (pat.)] |
z = -5.5  +--------------------------------------------------+
        x = -9.0        (>= 2.5 m Dry Perimeter)          x = +9.0
```

### 7.4 Coordinate table

| Element | Position (x, y, z) | Notes |
|---|---|---|
| Door / exit | (-9.0, 0, 0) | Industrial steel exit door with interactable green panic bar (`AskDecision` confirmation → 90° swing-open) + overhead `E001`/`E002` beacon |
| Spawn | (-7.6, 0, 0) | Facing east (`yaw 90°`) |
| E-stop pedestal | (-5.6, 0, 0) | Facing west toward spawn; outside puddle |
| Alarm button | (-8.50, 1.45, 2.4) | West-north post facing east |
| Press | (0, 0, 0) | 3.0 × 2.5 m footprint |
| Puddle centre | (0, 0, 0) | Up to 7.0 × 5.0 m ring (pattern-dependent) |
| Electrical bay (3 Sub-Stations) | (8.72, 1.28, 0) | East wall; Sub-Stations at local Z `-0.88`, `0`, `+0.88` |
| Valve | (4.75, 0.95, 0.55) | East oil pipe, outside puddle, bright red pulsing cue |
| PPE cabinet (Pattern A) | (2.8, 0, -5.12) | Wide sliding-door cabinet, pattern-dependent |
| Spill kit (Pattern A) | (-5.2, 0, -4.35) | Red spill-kit bin, pattern-dependent |
| Water extinguisher (Pattern A) | (3.75, 1.25, 5.22) | North wall, pattern-dependent |
| Display | (-1.8, 1.62, -0.95) | Press west face |
| Thermometer | (-1.6, 1.78, 0.25) | Press west face |

### 7.5 Safe path

1. Enter from the west portal at `(-7.6, 0, 0)`.
2. Walk east to the E-stop pedestal at `(-5.6, 0, 0)` along the yellow entry lane.
3. Press the red E-stop mushroom.
4. Raise the alarm on the west-north post.
5. Walk the spacious (`≥ 2.5 m`) Dry Perimeter to the east electrical bay.
6. Operate Sub-Station 1 (Main Disconnect), Sub-Station 2 (Lockout), and Sub-Station 3 (Voltage Tester until ZERO), then verify Try-Start at the E-stop pedestal.
7. Open the wide sliding-door PPE cabinet and equip all 4 correct PPE items (Heat Gloves, Chemical Apron, Face Shield, Safety Boots S3).
8. Close the bright red Valve on the east oil pipe, then grab 4 cones and 4 absorbents from the Spill Kit and snap them onto the glowing Guided Containment Zones.
9. Walk through the illuminated green west Exit portal to report to the supervisor.

### 7.6 Layout patterns

Fixed: press, E-stop pedestal, alarm post, east electrical bay (3 Sub-Stations), east valve, west exit.

Rotated across consecutive runs via a scenario-local persistent shuffle-bag JSON (`Application.persistentDataPath/machinelock_pattern_state.json`, ADR-0006) so the same Layout Pattern never repeats back-to-back (one of three spacious Layout Patterns in the `18 × 11 m` bay):

| Pattern | PPE cabinet | Spill kit | Extinguisher | Puddle variant |
|---|---|---|---|---|
| A | `(2.8, 0, -5.12)` yaw `0°` | `(-5.2, 0, -4.35)` yaw `0°` | `(3.75, 1.25, 5.22)` yaw `180°` | `(0, 0, 0)`, `7.0 × 5.0 m` |
| B | `(5.8, 0, -5.12)` yaw `0°` | `(-5.2, 0, 4.35)` yaw `180°` | `(0.9, 1.25, 5.22)` yaw `180°` | `(0.5, 0, 0.2)`, `6.6 × 4.8 m` |
| C | `(-2.5, 0, 5.12)` yaw `180°` | `(4.8, 0, -4.35)` yaw `0°` | `(-6.2, 1.25, -5.22)` yaw `0°` | `(-0.4, 0, -0.25)`, `6.8 × 4.7 m` |

Every pattern maintains at least **2.5 m** of Dry Perimeter on all four sides. The E-stop, alarm, and exit are never blocked.

Residual energy: **50%** chance the oil is energised at start. The state is independent of the pattern.

### 7.7 Lighting and atmosphere

- Baked/static lighting: four ceiling industrial fixtures (`MESH_Magazzino_LuceDaSoffitto.fbx` + baked point lights) plus bright industrial ambient light.
- One directional fill light (realtime, shadows off) and one realtime red alarm beacon point light (range 8 m, shadows off, disabled at start) — at most 2 realtime lights total and 0–1 shadow casters.
- Press display: emissive material with a blinking fault pattern (`MAT_HMI_Screen` → `MAT_HMI_Off`).
- Puddle: dark glossy material (`MAT_Oil`, smoothness 0.82, metallic 0.02, ripple normal map; no planar reflection).
- Steam wisps over the puddle; stronger after water contact.
- Performance budget: ≤ 100k triangles, ≤ 2 realtime lights, ≤ 1 shadow caster, ≤ 200 particles/system, ≤ 4 play-on-awake particle systems, textures 1024/2048 ASTC (displacement maps omitted).

---

## 8. Interaction design

### 8.1 Input map

| Action | Input | Used for |
|---|---|---|
| Select | Trigger | Buttons, panel, lockout, tester, PPE items, valve, extinguisher |
| Grab | Grip | Cones, mats, socks |
| Menu / Pause | Left menu button | Common UI only |
| Back | B / Y | Common UI only, menu visible |

No A/X mapping. No `OVRInput`. No XRI.

### 8.2 Object interaction table

| Object | Component | Allowed | Behaviour | Score / consequence |
|---|---|---|---|---|
| Press body | `MachineLockPress` | none | Runs until E-stop; hazard trigger | Contact while running: critical |
| E-stop mushroom | `MachineLockPress` | Select | Stops ram and hydraulic loop; display stays on | Execution +2; cancels inaction timer |
| Alarm button | `MachineLockAlarm` | Select | Red beacon + siren; `ShowSoundEvent` | Execution +2; missing at exit: Decision -8 |
| Main switch | `MachineLockEnergyPanel` | Select | Opens main circuit; display and motor off | With lockout: Execution +3 |
| Lockout point | `MachineLockEnergyPanel` | Select | Abstract lock; red LOCK indicator | With main off: part of +3; missing: Decision -2 |
| Voltage tester | `MachineLockEnergyPanel` | Select | Reads LIVE / ZERO; LED + alert | Zero with try-start: Execution +2 |
| Test start button | `MachineLockPress` | Select | Maintenance jog; must not move after isolation | If powered: critical; if isolated: +2 |
| PPE cabinet | `MachineLockPpeCabinet` | Select | Opens; items become selectable | — |
| PPE item | `MachineLockPpeItem` | Select | Equips/replaces slot; text feedback | Full 4/4: Execution +3 |
| Valve | `MachineLockValve` | Select | Rotates 90°; stops jet and puddle growth | No score; good-practice debrief line |
| Cone | `MachineLockCone` | Grab / release | Snaps to nearest cone marker within 0.6 m | 4/4: containment part |
| Absorbent mat/sock | `MachineLockAbsorbent` | Grab / release | Snaps to coverage zone within 0.6 m | 4/4: containment part |
| Water extinguisher | `MachineLockExtinguisher` | Select | Sprays water; steam/smoke burst | Critical: Safety -5, Decision -10 |
| Exit door panic bar | `MachineLockExitDoor` (`Interactable`) | Select / Grab | Opens `HUD_DecisionPanel` confirmation; on Yes, swings door open and ends mission (Early Exit allowed anytime) | +1 Execution when safe; missing tasks & running press penalized in Debrief |

### 8.3 Snap rules

- A released cone or absorbent checks the nearest free matching `MachineLockSnapZone` within `config.snapRadius` (default 0.6 m).
- If a zone is found, the item snaps to the zone transform, its `Interactable.Grabbable` becomes false, and the zone is occupied.
- If no zone is found, the item stays where it was released and remains grabbable.
- In Guided, subtle baseline floor rings/outlines (`VFX_BaselineMarker`) illuminate on all free Containment Zones as soon as full PPE is worn, brighten into full green snap ghosts (`VFX_SnapGhost`) while holding a matching item, and hide individually once occupied.
- Grabbed and placed cones/absorbents call `Physics.IgnoreCollision` against the Player Rig `CharacterController` (`MachineLockCollisionSafety`) while keeping their colliders non-trigger so `RayInteractor` raycasts keep working without pushing or clipping the player.
- Once containment is complete, the hazard trigger is disabled.

### 8.4 World labels, Diegetic Plaques & Hover Feedback (ADR-0002)

To avoid overlapping 3D cards and depth clipping against walls without modifying `Assets/_Project/Home/`, Scenario 08 combines:

1. **11 sparse World Labels (`LabelAnchor`)** positioned cleanly in open space (max 3 `Risk` anchors, electrical bay collapsed to 1 clean overhead anchor):

| Object | Kind | Title | Detail |
|---|---|---|---|
| Press | Risk | Pressa in moto | Non avvicinarti |
| Thermometer | Risk | Olio a 120 °C | Superficie bollente |
| Puddle | Risk | Pozza d'olio bollente | Non calpestare |
| E-stop | ContextHelp | Fungo di emergenza | Premi per fermare la pressa |
| Alarm | ContextHelp | Allarme reparto | Premi per chiamare i soccorsi |
| Electrical bay (collapsed) | ContextHelp | Quadro elettrico (LOTO) | 1. Stacca · 2. Blocca · 3. Tester |
| PPE cabinet | ContextHelp | Armadio DPI | Apri e indossa i 4 DPI idonei |
| Spill kit | ContextHelp | Kit antispandimento | Afferra con Grip coni, tappeti e sacchi |
| Valve | ContextHelp | Valvola olio | Chiudi per fermare la perdita |
| Extinguisher | ContextHelp | Estintore ad acqua | Non usare su olio o quadro |
| Exit | ContextHelp | Uscita di emergenza | Usa il maniglione (TRIGGER o GRIP) |

2. **Crisp UNI EN ISO 7010 / D.Lgs. 81/08 Safety Signs (`TEX_IsoSign_*`)** visible in all difficulty modes (`W012`, `W017`, `W024`, `W011`, `P011`, `M008`, `M009`, `M013`, `M026`, `PpeBoard`, `F001`, `F005`, `E001`, `E002`), paired with compact engraved brushed-aluminum **Diegetic Plaques** on the 3 electrical Sub-Stations, E-stop pedestal, press nameplate, and department header (redundant pixel-font prop plaques and banners removed; PPE shelf retains anti-aliased `EquippedBadge` indicators).
3. **Dwell-gated hover Action Feedback (`MachineLockHoverFeedback`)** limited strictly to the 6 PPE cabinet bays and 3 electrical Sub-Stations: requires a continuous `0.55 s` ray dwell (cancels immediately on `HoverExited` or selection so grazing/turning never fires haptics or banners) plus a `4.0 s` global cross-object cooldown (`6.0 s` same-text repeat cooldown). All other stations rely on silent 3D World Labels, ISO 7010 signage, and engraved brushed-metal plaques.

Risk labels: press, thermometer and puddle are within 4 m of each other — exactly the maximum of 3. Do not add another Risk anchor near the press.

---

## 9. Gameplay systems

### 9.1 Press

- The ram cycles irregularly: period 6–8 s, travel 0.6 m, with occasional pauses. No Animator required; a small `Update` or coroutine is enough.
- Loop audio: hydraulic clunks and motor hum.
- Display: blinking fault code `ALLARME 04 — PRESSIONE ANOMALA`.
- E-stop: ram stops immediately; motor spins down; display remains on; auxiliary circuit remains live.
- Main switch off: display goes dark; motor silent; alarm beacon remains on.
- Try-start: maintenance jog that bypasses the E-stop. If powered, the ram moves and the scenario triggers a critical. If not powered, nothing moves and the test passes.
- Contact with the press while running: critical.

### 9.2 Alarm

- Wall button; select to raise.
- Red beacon rotates/flashes; siren loop; `HUDService.ShowSoundEvent("Allarme reparto", true)`.
- Alarm can be raised before or after the E-stop. It does not stop hazards.
- Missing at exit: Decision -8, Partial.

### 9.3 Main power and lockout

- Main switch starts ON.
- Switching OFF sets `MainPowerOn = false`; the press display and motor go off.
- After main OFF, the lockout point becomes selectable. Selecting it sets `LockedOut = true`; a red `LOCK` LED turns on; feedback “Lockout applicato”.
- Main OFF alone does not complete the isolation objective; lockout is required.
- Missing lockout at exit: Decision -2.
- Missing main OFF at exit: Decision -2.
- The lockout is abstract; no padlock mesh is added.

### 9.4 Residual energy and voltage tester

- At start, 50% chance the oil is energised.
- If energised, `Charge01 = 1`.
- When main power is OFF, charge decays linearly over `energyDecaySeconds` (default 90 s). When it reaches 0, the oil is no longer energised.
- If main power is ON, charge does not decay.
- The tester is wall-mounted next to the panel. Select it to read:
  - Main ON: `LIVE — quadro inserito`.
  - Main OFF and charge > 0.05: `LIVE — energia residua`.
  - Main OFF and charge ≈ 0: `ZERO — energia assente`.
- The tester shows a red LED for LIVE and a green LED for ZERO; the HUD alert reports the text.
- The tester does not discharge the oil; it only measures.
- Touching live oil causes a shock regardless of PPE.
- Touching hot oil without full PPE causes a burn.

### 9.5 Try-start

- The test button is on the E-stop pedestal.
- It is a maintenance jog that bypasses the E-stop, so it can reveal whether the main circuit is isolated.
- If `MainPowerOn` is true: the ram jogs; the scenario triggers `TryStartUnisolated` (Safety -5, Decision -10, critical).
- If `MainPowerOn` is false: no movement; feedback “Nessun avvio: isolamento ok”; sets `TryStartVerified`.
- Execution +2 only when `TesterZero` and `TryStartVerified` are both true.

### 9.6 PPE (ADR-0004)

- Four required slots: **Gloves**, **Body**, **Face**, **Feet**.
- Correct items:
  - `Guanti anticalore EN 407` — Gloves.
  - `Grembiule antischizzo` — Body.
  - `Visiera facciale EN 166` — Face.
  - `Scarpe antinfortunistiche S3` — Feet.
- Distractor PPE (wrong items, cleanly paired on occupied slots):
  - `Guanti in lattice monouso` — Gloves (replaced by Heat Gloves).
  - `Mascherina chirurgica` — Face (replaced by Face Shield).
- Because both distractors share slots with required correct items, equipping all 4 correct items always clears `HasWrongPpeEquipped` with zero soft-lock states.
- The cabinet is a wide shallow (`1.80 × 2.05 × 0.44 m`) non-blocking sliding-door locker. Selecting it slides both doors open and immediately disables the door colliders so they never block the player's body or raycasts.
- Each of the 6 bays has throttled hover Action Feedback showing the item name and slot, and an anti-aliased `EquippedBadge` shelf status; hovering an already-worn bay emits `"<Nome DPI> indossato"`.
- Selecting an item equips it:
  - If the slot is empty, the item visuals hide (collider stays hoverable, `Selectable = false`), its `EquippedBadge` shows, and the slot is filled.
  - If the slot is occupied, the previous item reappears in the cabinet and the new item equips.
  - Wrong items occupy their slot but do not provide protection.
- `HasFullPpe` is true only with all four correct items (`4/4`).
- No wrist icons: `SetHint("DPI 2/4 — armadio DPI")`; `Feedback` on each equip; a final `Alert` at 4/4.
- Full PPE: Execution +3.
- Touching hot oil without `HasFullPpe`: critical; wrong gloves or mask count as no protection.

### 9.7 Oil leak and valve

- The leak is a thin jet from a cracked pipe on the press east side.
- The valve is on the pipe near the east wall, outside the puddle.
- Selecting the valve rotates it 90°, stops the jet, plays a metal squeak, and stops puddle growth.
- Closing the valve gives no score, but produces a good-practice debrief line if closed before handover.
- The puddle visual is split into four organic SPLAT quadrant meshes (`MESH_PuddleQuadrantNE/NW/SE/SW`); each absorbent hides one quadrant.
- The puddle hazard trigger covers the oil area; it is disabled when all four absorbents are placed.

### 9.8 Containment and delimitation

- Spill kit contains: 4 cones, 2 absorbent mats, 2 absorbent socks.
- 4 cone markers around the puddle; 4 absorbent coverage zones (quadrants).
- All 4 cones and all 4 absorbents are required.
- Snap radius 0.6 m.
- Cones and absorbents can be picked up again until they snap.
- `ContainmentComplete = cones == 4 && absorbents == 4`.
- Containment complete: Execution +2; hazard trigger off; good debrief line.
- Missing cones at exit: Decision -4.
- Missing absorbents at exit: Decision -5.
- Missing both: both penalties apply; the debrief combines them into one line.
- In Guided, free zones show a ghost while holding a matching item.

### 9.9 Water extinguisher

- Wall-mounted on the north wall, selectable.
- Selecting it sprays water; white flash, steam burst, black smoke; critical event.
- Safety -5, Decision -10.
- Damage vignette; haptic pulse; teleport to the entrance after 2 s.
- The label warns “Non usare su olio o quadro” in Guided.
- This is the critical distractor; it must remain visually available and not hidden.

### 9.10 Exit and handover (ADR-0006)

- The west exit is an interactive hinged steel door (`MachineLockExitDoor`) with an EN 1125 green **panic bar** (`Interactable` accepting both Select/Trigger and Grab/Grip with `FollowsHand = false`; legacy `ScenarioTrigger` removed).
- Activating the panic bar opens `SCN_Core`'s shared `HUD_DecisionPanel` via `ScenarioController.AskDecision`:
  - Question: “Vuoi uscire dal reparto adesso?”
  - Option 0: “Sì, esci dal reparto” → unlatches door, disables door leaf collider, smoothly swings door leaf 90° outward into the illuminated west vestibule, plays the Preposto closing line, and calls `EndMission` once.
  - Option 1: “No, resta nella stanza” → closes the popup; the door stays shut and the mission continues uninterrupted.
- **Early Exit is permitted at any time**, including while the press is still running:
  - Leaving with the press running applies `SpiEvent.MissingEStop` (-20 Safety, -40 Decision, Time bonus forfeited, `OutcomeType.Negative`, zero score in Operational/Crisis, and critical debrief line `"! Hai lasciato il reparto con la pressa ancora in funzione → rischio grave"`).
  - Leaving with the press stopped but missing non-critical tasks applies each missing-task penalty at handover and yields `OutcomeType.Partial`.
- The Preposto line depends on the result:
  - Safe (`Positive`): “Sala in sicurezza. Ottimo lavoro.”
  - Incomplete or Unsafe (`Partial` / `Negative`): “La sala non è ancora sicura.”

### 9.11 Observation and Awareness

- Six targets: press body, thermometer, display, puddle, electrical panel, PPE cabinet.
- Primary mode: gaze dwell.
  - Head within 3.5 m.
  - Angle between head forward and target ≤ 25°.
  - Dwell 1.2 s.
- Fallback mode: proximity.
  - Head within 1.5 m for 0.5 s.
  - Selected by `config.observationMode`; default Gaze.
- Each target awards `20 / 6` Awareness points once.
- Awareness is silent except for the Guided hint `Osserva la scena: N/6` before the first action.
- No Awareness debrief line; the Score bar shows it.

### 9.12 Inaction and smoke

- The inaction timer starts at scenario start while the press is running.
- Timings are identical in all modes:
  - 90 s: smoke stage 1; alert “Fumo in sala / La pressa continua”.
  - 150 s: smoke stage 2; alert “Aria irrespirabile / Ferma la pressa”.
  - 240 s: Bad Ending 2; `EndMission(Negative, ...)`.
- Pressing the E-stop cancels the timer and fades the smoke.
- The inaction timer is independent of the level time limit.

### 9.13 Damage feedback (ADR-0003)

- `MachineLockDamageFeedback` owns a URP `Volume` (`SO_MachineLockDamageProfile`) plus a scenario-owned head-aligned VR-stereo-safe overlay (`SH_MachineLockDamageOverlay` / `MAT_DamageOverlay`) and fallback red point light (`LIGHT_DamageFlash`).
- On every Critical Event, it inspects the active camera's `UniversalAdditionalCameraData.renderPostProcessing`:
  - When camera post-processing is enabled and the Volume profile is valid, it drives the URP Vignette (`0.15 s` in → `0.5 s` hold → `1.5 s` out).
  - When camera post-processing is disabled (as in `MNG_CoreRig.prefab`), it drives the scenario-owned peripheral red vignette overlay mesh (`ZTest Always`, `Queue=Overlay`, Single-Pass Instanced stereo macros) + red point-light pulse + dual-hand haptics + one-shot audio.
- On Guided respawn teleport, `PlayRespawnBlink` runs a brief `0.3 s` dark fade-out/fade-in blink synchronized with the teleport.
- Honors `accessibility.reduce_flashes` (suppresses flashing point light and attenuates overlay intensity).
- `Clear()` resets all volume weights, overlay alphas, and light intensity immediately.
- Never modifies `MNG_CoreRig.prefab` and never adds a Camera or Canvas.

---

## 10. State machine

### 10.1 High-level states

| State | Entry condition | Exit condition |
|---|---|---|
| Intro | `OnScenarioStarted` | After intro subtitles |
| Emergency | Press running | E-stop pressed |
| MachineStopped | E-stop pressed | Main power off |
| Isolated | Main off + lockout | Tester zero + try-start |
| ZeroEnergy | Tester zero + try-start | Full PPE + containment |
| Protected | Full PPE | Containment complete |
| Contained | Cones + absorbents complete | Exit |
| HandoverReady | All tasks complete | Exit trigger |
| Ended | `EndMission` | — |

The high-level state is for HUD phase and hints. Task flags are independent and can be completed out of order.

### 10.2 Task flags

```
TaskEStop
TaskAlarm
TaskMainOff
TaskLockout
TaskTesterZero
TaskTryStart
TaskPpeFull
TaskContainment
TaskExit
```

Derived macro-objectives:

| Macro | Text | Complete when |
|---|---|---|
| 0 | Ferma la pressa e allarma | TaskEStop && TaskAlarm |
| 1 | Isola l'energia e blocca | TaskMainOff && TaskLockout && TaskTesterZero && TaskTryStart |
| 2 | Indossa i DPI e contiene | TaskPpeFull && TaskContainment |
| 3 | Esci e avvisa il preposto | TaskExit |

### 10.3 Objective refresh pseudocode

```
void RefreshObjectives()
{
    while (currentMacro < 4 && MacroComplete(currentMacro))
    {
        CompleteObjective();
        currentMacro++;
    }
    if (currentMacro < 4) SetObjective(currentMacro);
}
```

Call `RefreshObjectives()` after every task event. The common controller caps `objectivesCompleted` at the objective count; the loop is safe.

### 10.4 HUD phase

| Phase | When |
|---|---|
| `PHASE_SCAN` | Start, until E-stop |
| `PHASE_ACTION` | E-stop until containment |
| `PHASE_RESOLUTION` | Containment complete until exit |

`PHASE_DECISION` is unused; the scenario is action-based.

### 10.5 Event methods on `MachineLockScenario`

```
NotifyEStopPressed()
NotifyAlarmPressed()
NotifyMainPowerOff()
NotifyLockoutApplied()
NotifyTesterRead(bool zero)
NotifyTryStartVerified()
NotifyPpeChanged(int correctCount, bool full)
NotifyConePlaced(int count)
NotifyAbsorbentPlaced(int count)
NotifyValveClosed()
NotifyObservation(string id)
NotifyCritical(CriticalEvent evt)
NotifyExitReached()
```

All methods must be null-safe and idempotent. They must ignore calls after `ended == true`.

---

## 11. Scoring (SPI)

### 11.1 Formula and ownership

```
SPI = Safety + Awareness + Decision + Execution + Time
```

- Safety: 0–20.
- Awareness: 0–20 (float, rounded at the end).
- Decision: 0–40.
- Execution: 0–15.
- Time: 0–5.
- Final SPI: `Mathf.RoundToInt(sum)`, clamped 0–100.

`EndMission` receives:
- `outcome`
- `safetyScore = SPI`
- `dimensions = [Safety/20, Awareness/20, Decision/40, Execution/15, Time/5] × 100`, rounded
- `debriefLines`

The five bars show normalized performance; the SPI is weighted. This is the documented deviation.

**Ownership:** the authoritative SPI is our own script:
`Assets/_Project/Groups/G_Marius/Code/Scripts/MachineLock/Core/MachineLockSpi.cs`.
It owns the five dimensions, weights, time modes, penalties, outcome and debrief. It has no scene dependencies and is unit-tested. If another team’s SPI exists, do not modify it; you may instantiate it only as a reference. If the team later adopts it, write an adapter and keep our tests.

### 11.2 Safety (max 20, start 20)

| Event | Delta | Notes |
|---|---:|---|
| Hot oil contact without full PPE | -10 | Critical; one event per contact |
| Live oil contact (shock) | -10 | Critical; PPE does not protect |
| Press/steam burn | -10 | Critical |
| Contact with wrong PPE | -10 | Replaces the hot-oil penalty, not cumulative |
| Water extinguisher use | -5 | Critical act |
| Clamp | 0 | — |

### 11.3 Awareness (max 20, start 0)

| Target | Points |
|---|---:|
| Press body | 20/6 |
| Thermometer | 20/6 |
| Display | 20/6 |
| Puddle | 20/6 |
| Electrical panel | 20/6 |
| PPE cabinet | 20/6 |
| Final | `RoundToInt(20 × observed / 6)` |

### 11.4 Decision (max 40, start 40)

| Event | Delta | Notes |
|---|---:|---|
| Approach puddle danger zone before E-stop | -10 | Once per run |
| Approach live puddle danger zone before main off | -10 | Once per run |
| Touch puddle without full PPE | -10 | Critical contact |
| Touch live puddle even with full PPE | -10 | Critical shock |
| Use water extinguisher | -10 | Critical |
| Try-start before isolation | -10 | Critical |
| Exit with press still running (Early Exit) | -20 Safety / -40 Decision (critical) | Permitted anytime via confirmation popup; forces Negative outcome (0 / severe fail in non-Guided) and adds critical debrief row |
| Exit without alarm | -8 | At handover |
| Exit without main off | -2 | At handover |
| Exit without lockout | -2 | At handover |
| Exit without cones | -4 | At handover |
| Exit without absorbents | -5 | At handover |
| Timeout with press stopped | -5 | Guided: at hard exit; Realistic/Stress: at limit |
| Timeout with press stopped and contained but no handover | -5 | Same |
| Idle press running at 240 s | -10 | Ends Negative |
| Clamp | 0 | — |

Critical events apply their Safety and Decision penalties once per contact, with a 5 s cooldown. If several hazards overlap, apply the most severe event and do not stack the same contact.

### 11.5 Execution (max 15, start 0)

| Action | Points |
|---|---:|
| Press E-stop | +2 |
| Raise alarm | +2 |
| Main off + lockout | +3 |
| Tester zero + try-start verified | +2 |
| All 4 correct PPE | +3 |
| Containment complete (4 cones + 4 absorbents) | +2 |
| Exit/handover | +1 |
| **Total** | **15** |

Execution points are awarded once per action. Out-of-order actions still count; the Decision penalties handle unsafe order.

### 11.6 Time (max 5, start 0)

| Brief name | Project level | Limit | At/after limit | Mission at limit |
|---|---|---:|---|---|
| Guided | `Guided` | 900 s | Time = 0 | Continues |
| Realistic | `Operational` | 600 s | Time = 0 | Ends |
| Stress | `Crisis` | 480 s | Time = 0 | Ends |

Formula:

```
Time = 5 * clamp((T_limit - elapsed) / T_limit, 0, 1)
Time = 0 if elapsed >= T_limit
```

- The time limit is the expected time for the level. The score is proportional to the unused fraction of that budget.
- Guided has no countdown in the common HUD; the HUD shows elapsed time. If the player passes 900 s, Time = 0 but the level continues.
- Realistic and Stress use the common HUD countdown (600 s / 480 s). When the countdown reaches zero, the mission ends with Time = 0.
- On Realistic/Stress expiry, the outcome is:
  - **Negative** if the press is still running or the area is not contained.
  - **Partial** if the press is stopped but tasks are incomplete.
  - **Positive** only if all tasks were completed and the exit happened before the timer expiry path.
- The inaction Bad Ending 2 remains 240 s of press running in all modes.
- `OnTimerExpired()` is overridden to force the Realistic/Stress end with Time = 0. Guided does not use `OnTimerExpired`.
- There is no 1200 s hard cap. Guided has no hard cap.

### 11.7 Outcome matrix

| Condition | Outcome |
|---|---|
| All 7 logical tasks complete, no critical, containment + handover | Positive |
| Any critical error in Guided | Partial at best |
| Missing alarm/main/lockout/cones/absorbents | Partial |
| Guided: player exits after 900 s with all tasks complete | Positive, Time = 0 |
| Realistic/Stress: timer expires with press stopped but tasks incomplete | Partial, Time = 0 |
| Realistic/Stress: timer expires with press running or area not contained | Negative, Time = 0 |
| Timeout with press stopped (any mode) | Partial |
| Timeout with press running | Negative — Bad Ending 2 |
| Early Exit with press running | Negative (critical debrief row: left room with press still running; zero / severe penalty in Operational & Crisis; ADR-0006) |
| Early Exit with press stopped but missing tasks | Partial (penalized per missed task in Debrief; in Operational/Crisis heavy penalties may yield low score) |
| Exit with all tasks | Positive |

In Guided, critical errors never end the mission immediately. They apply penalties, damage feedback and a respawn, and the player can continue. The final outcome can still be Partial; it cannot be Positive after a critical error.

### 11.8 Debrief budget

The common Debrief screen shows at most **5 decision rows** and **4 error rows**. The SPI must cap and prioritise:

- Good (max 5):
  1. `+ Hai fermato la pressa e dato l'allarme → soccorsi chiamati`
  2. `+ Hai isolato e bloccato il quadro → energia zero`
  3. `+ Hai verificato l'assenza di energia → rischio elettrico escluso`
  4. `+ Hai indossato i 4 DPI → protezione completa`
  5. `+ Hai delimitato e contenuto la pozza → area sicura`
- Bad/Critical (max 4, critical first):
  1. `! Contatto con olio bollente → ustione`
  2. `! Olio in tensione → folgorazione`
  3. `! Estintore ad acqua → vapore e schizzi`
  4. `! Prova di riavvio senza isolamento → ripartenza`
  5. `- Allarme non dato → reparto non avvisato`
  6. `- Quadro o lockout incompleti → possibile riarmo`
  7. `- Delimitazione o contenimento incompleti → area non sicura`
  8. `- Tempo massimo superato → chiusura forzata`

The builder adds entries in chronological order, then trims to 5 good and 4 bad/critical, keeping critical lines. The score still includes all penalties.

### 11.9 Score examples

| Run | Safety | Awareness | Decision | Execution | Time | SPI | Outcome |
|---|---:|---:|---:|---:|---:|---:|---|
| Perfect, 5 min Guided | 20 | 20 | 40 | 15 | 3.3 | 98 | Positive |
| Perfect, 12 min Guided | 20 | 20 | 40 | 15 | 1.0 | 96 | Positive |
| Missing alarm, 14 min Guided | 20 | 20 | 32 | 13 | 0.3 | 85 | Partial |
| Missing containment + cones, 16 min | 20 | 20 | 31 | 13 | 0.0 | 84 | Partial |
| One hot-oil contact, 14 min | 10 | 20 | 30 | 15 | 0.0 | 75 | Partial |
| Water extinguisher, 15 min | 15 | 20 | 30 | 15 | 0.0 | 80 | Partial |
| Bad Ending 2, 4 min | 10 | 10 | 20 | 5 | 3.7 | 49 | Negative |

---

## 12. HUD and text

### 12.1 Objectives

`SO_Mission_MachineLock.Objectives`:

1. `Ferma la pressa e allarma` (25)
2. `Isola l'energia e blocca` (24)
3. `Indossa i DPI e contiene` (24)
4. `Esci e avvisa il preposto` (25)

### 12.2 Phases

- `PHASE_SCAN` at start.
- `PHASE_ACTION` after E-stop.
- `PHASE_RESOLUTION` after containment.

### 12.3 Hints

| Situation | Hint |
|---|---|
| Start | `Osserva la scena: 0/6` (count updates) |
| After E-stop | `Suona l'allarme sul muro ovest` |
| After alarm | `Apri il quadro a est e togli tensione` |
| Main off | `Applica il lockout sul quadro` |
| Lockout | `Usa il tester di tensione` |
| Tester zero | `Prova il riavvio: non deve partire` |
| Zero energy | `Indossa i 4 DPI nell'armadio a sud` |
| DPI partial | `DPI 2/4 — armadio a sud` |
| PPE full | `Chiudi la valvola e delimita la pozza` |
| Containment partial | `Coni 2/4 · Assorbenti 1/4` |
| Containment complete | `Esci dalla porta ovest e avvisa il preposto` |

Hints are shown only by the Guided level config; the scenario still calls `SetHint` and the common UI decides.

### 12.4 Alerts, feedback, subtitles

- Use `Alert` for major events: E-stop, alarm, panel, lockout, live oil, full PPE, containment, critical, timeout.
- Use `Feedback` for item-level results: cone placed, mat placed, DPI slot, wrong item, valve.
- Use `Subtitle` for narrative and instructions.
- `HUDService.ShowSoundEvent` for critical non-vocal sounds: alarm siren, steam burst, shock.
- Do not use `AskDecision`.
- Do not add keys to `SO_UIStrings_It`.

### 12.5 Text inventory

All strings live in `MachineLockTexts.cs` as `const string` values. Italian, player-facing.

| Id | Text | Notes |
|---|---|---|
| Intro1 | `Perdita olio a 120 gradi.` | Subtitle, Sistema |
| Intro2 | `Pressa in movimento.` | Subtitle, Sistema |
| EStop | `Pressa fermata.` | Subtitle |
| EStop2 | `Il display resta acceso.` | Subtitle |
| Alarm | `Allarme reparto attivato.` | Subtitle |
| Alarm2 | `Soccorsi in arrivo.` | Subtitle |
| Panel | `Quadro generale aperto.` | Subtitle |
| Panel2 | `Energia di macchina isolata.` | Subtitle |
| Lockout | `Lockout applicato.` | Subtitle |
| Lockout2 | `Nessuno può riarmare.` | Subtitle |
| TesterLive | `Olio ancora in tensione.` | Alert |
| TesterLive2 | `Attendi che si scarichi.` | Alert detail |
| TesterZero | `Energia zero confermata.` | Alert |
| TryOk | `Nessun avvio: isolamento ok.` | Feedback |
| TryFail | `La pressa riparte!` | Alert |
| TryFail2 | `Isolamento non riuscito.` | Alert detail |
| PpeEquip | `{0} indossato.` | Feedback, format |
| PpeWrong | `{0}: non idoneo.` | Feedback Negative |
| PpeFull | `DPI completi: 4 su 4.` | Alert |
| Valve | `Valvola chiusa: perdita fermata.` | Feedback |
| Cone | `Cono posizionato ({0}/4).` | Feedback |
| Mat | `Tappeto assorbente posizionato.` | Feedback |
| Sock | `Sacco assorbente posizionato.` | Feedback |
| Containment | `Area delimitata e contenuta.` | Alert |
| HotOil | `Contatto con olio bollente.` | Alert |
| HotOil2 | `Ustione: allontanati subito.` | Alert detail |
| Shock | `Scossa elettrica!` | Alert |
| Shock2 | `Olio ancora in tensione.` | Alert detail |
| Water | `Acqua sull'olio bollente!` | Alert |
| Water2 | `Vapore e schizzi: allontanati.` | Alert detail |
| ExitBlocked | `Non puoi uscire` | Alert title |
| ExitBlocked2 | `La pressa è ancora in moto.` | Alert detail |
| PrepostoSafe | `Sala in sicurezza. Ottimo lavoro.` | Subtitle, Preposto |
| PrepostoUnsafe | `La sala non è ancora sicura.` | Subtitle, Preposto |
| TimeWarning | `Tempo` | Alert title |
| TimeWarning2 | `Concludi la messa in sicurezza.` | Alert detail |
| BadEnding2 | `La pressa ha continuato a lavorare.` | Debrief critical |

Every string must be checked against the HUD limits: subtitles 32 characters × 2 lines; alerts have title + detail; hints ≤60 characters. The HUD paginates long subtitles automatically.

### 12.6 Accessibility

- All critical sounds have subtitles via `ShowSoundEvent`.
- No colour-only information: LEDs have text alerts.
- Text is high contrast; no text in the centre of the view.
- Haptics respect the common settings gate.
- The player can pause with the left menu button; the common UI handles it.

---

## 13. Audio and VFX

### 13.1 Audio

| Id | Type | Trigger | Notes |
|---|---|---|---|
| `SFX_PressLoop` | loop | Press running | 3D at press, irregular clunks |
| `SFX_PressStop` | one-shot | E-stop | Motor spin-down |
| `SFX_EStopClack` | one-shot | E-stop | Mechanical clack |
| `SFX_AlarmButton` | one-shot | Alarm press | Clack |
| `SFX_AlarmSiren` | loop | Alarm raised | 3D at beacon, audible everywhere |
| `SFX_BreakerOff` | one-shot | Main off | Heavy switch |
| `SFX_Lockout` | one-shot | Lockout | Metal click |
| `SFX_OilLeak` | loop | Jet active | Hiss/sizzle, 3D at leak |
| `SFX_ValveTurn` | one-shot | Valve | Metal squeak |
| `SFX_TesterBeep` | one-shot | Tester | Short beep |
| `SFX_EquipPpe` | one-shot | PPE equip | Cloth/velcro |
| `SFX_Snap` | one-shot | Cone/mat snap | Soft click |
| `SFX_SteamBurst` | one-shot | Water extinguisher | Sharp steam |
| `SFX_Shock` | one-shot | Live oil contact | Electric zap |
| `SFX_Burn` | one-shot | Hot oil contact | Sizzle |
| `SFX_AmbientFactory` | loop | Always | Low industrial room tone |

Placeholders: CC0 or synthesized. Final audio later. Keep the `SFX_` names.

### 13.2 VFX

| Id | Trigger | Notes |
|---|---|---|
| `VFX_OilJet` | Start, until valve closed | Thin dark jet |
| `VFX_OilPuddle` | Start | Four quadrant meshes; glossy |
| `VFX_Steam` | Always over hot oil | Low rate; stronger after water |
| `VFX_Smoke` | Inaction stages | Grey/black, rate increases |
| `VFX_ElectricalArc` | Live oil contact / tester live | Short sparks + light |
| `VFX_AlarmBeacon` | Alarm raised | Rotating red light + emissive dome |
| `VFX_DisplayFault` | Press running | Emissive blink |
| `VFX_WaterSpray` | Water extinguisher | Particle cone |
| `VFX_SnapGhost` | Holding item near free zone | Transparent cone/mat |
| `VFX_DamageVignette` | Critical | URP Volume |

Particle budget: ≤200 particles per system, ≤4 active systems, no per-frame allocations.

---

## 14. Technical architecture

### 14.1 Scene hierarchy

```
SCN_MachineLock
├── MNG_Scenario                  (MachineLockScenario)
├── MNG_SpawnPoint                (SpawnPoint)
├── MNG_Light
├── MNG_Audio
├── ENV_Room
│   ├── MESH_Floor
│   ├── MESH_Walls
│   ├── MESH_Ceiling
│   ├── MESH_Door
│   └── MESH_Signs
├── ENV_Press
│   ├── MESH_HydraulicPress
│   ├── MESH_PressDisplay
│   ├── MESH_Thermometer
│   ├── MESH_LeakPipe
│   └── MESH_Valve
├── ENV_Safety
│   ├── MESH_EStopPedestal
│   ├── MESH_AlarmButton
│   ├── MESH_ElectricalPanel
│   ├── MESH_VoltageTester
│   ├── MESH_LockoutPoint
│   ├── MESH_PpeCabinet
│   ├── MESH_SpillKit
│   └── MESH_WaterExtinguisher
├── HAZARD_Oil
│   ├── VFX_OilJet
│   ├── VFX_OilPuddle
│   ├── ZONE_PuddleHazard
│   ├── ZONE_Coverage1..4
│   └── PATTERN_PuddleA/B/C
├── ZONE_Delimitation
│   └── ZONE_ConePoint1..4
├── MNG_LayoutPatterns
│   ├── PATTERN_A
│   ├── PATTERN_B
│   └── PATTERN_C
├── MNG_ObservationTargets
│   ├── OBS_Press
│   ├── OBS_Thermometer
│   ├── OBS_Display
│   ├── OBS_Puddle
│   ├── OBS_Panel
│   └── OBS_PpeCabinet
├── ZONE_Exit
└── MNG_Debug
```

`ENV_`, `ZONE_`, `OBS_` and `PATTERN_` are local grouping prefixes. They do not conflict with the validator; the SI81 prefixes (`MESH_`, `MNG_`, `VFX_`, `SFX_`, `MAT_`, `TEX_`, `SCN_`, `SO_`) remain mandatory for assets.

### 14.2 Prefab list

| Prefab | Contents |
|---|---|
| `MESH_HydraulicPress` | Base, crown, columns, ram, guard, control box, display, thermometer, leak pipe, colliders |
| `MESH_EStopPedestal` | Pedestal, mushroom, test start button, labels |
| `MESH_AlarmButton` | Wall box, red button, collider |
| `MESH_ElectricalPanel` | Cabinet, main switch, lockout point, tester, LEDs, colliders |
| `MESH_PpeCabinet` | Cabinet, door, shelves, six PPE items |
| `MESH_SpillKit` | Cart/bin, four cones, two mats, two socks |
| `MESH_WaterExtinguisher` | Extinguisher, bracket, collider |
| `MESH_Cone` | Cone mesh, collider, `Interactable`, `MachineLockCone` |
| `MESH_AbsorbentMat` | Flat mat, collider, `Interactable`, `MachineLockAbsorbent` |
| `MESH_AbsorbentSock` | Sock, collider, `Interactable`, `MachineLockAbsorbent` |
| `MESH_Valve` | Pipe section, wheel, collider, `MachineLockValve` |

Placeholder prefabs must keep the final names. Replace the visual children later; do not rename the roots or remove the interaction components.

### 14.3 Scripts

| Script | Assembly | Responsibility | Key API |
|---|---|---|---|
| `MachineLockSpi` | `SI81.MachineLock` | Pure C# SPI: dimensions, weights, time modes, penalties, outcome, debrief | `ApplyEvent`, `SetElapsed`, `Spi`, `Outcome`, `Dimensions100`, `BuildDebriefLines` |
| `MachineLockConfig` | `SI81.MachineLock` | Tunable ScriptableObject | Time limits, penalties, gaze, energy, snap, counts |
| `MachineLockScenario` | `Assembly-CSharp` | Orchestrator; state, tasks, HUD, scoring, end | `Notify*`, `OnScenarioStarted`, `RefreshObjectives`, `EndMissionOnce` |
| `MachineLockPress` | `Assembly-CSharp` | Press motion, E-stop, power, try-start, display | `PressEStop`, `SetPower`, `TryStart`, `IsRunning`, `IsPowered`, `IsEStopPressed` |
| `MachineLockEnergyPanel` | `Assembly-CSharp` | Main switch, lockout, residual charge, tester | `SetMainPower`, `ApplyLockout`, `ReadTester`, `TesterZero`, `OilEnergized` |
| `MachineLockPpeCabinet` | `Assembly-CSharp` | Cabinet, slots, equipment | `Open`, `Equip`, `HasFullPpe`, `CorrectCount` |
| `MachineLockPpeItem` | `Assembly-CSharp` | One PPE item | `Slot`, `IsCorrect`, `DisplayName` |
| `MachineLockSpillSystem` | `Assembly-CSharp` | Valve, cones, absorbents, containment | `CloseValve`, `RegisterCone`, `RegisterAbsorbent`, `ContainmentComplete` |
| `MachineLockSnapZone` | `Assembly-CSharp` | Snap target | `Kind`, `Occupied`, `SnapPoint` |
| `MachineLockCone` | `Assembly-CSharp` | Cone item | `TrySnap`, `OnGrabbed/Released` |
| `MachineLockAbsorbent` | `Assembly-CSharp` | Mat/sock item | `TrySnap`, `OnGrabbed/Released` |
| `MachineLockValve` | `Assembly-CSharp` | Valve interaction | `Close` |
| `MachineLockAlarm` | `Assembly-CSharp` | Alarm button, beacon, siren | `Raise`, `Raised` |
| `MachineLockObservationTarget` | `Assembly-CSharp` | Gaze/proximity target | `Id`, `Observed`, `TryObserve` |
| `MachineLockDamageFeedback` | `Assembly-CSharp` | URP vignette + fallback | `PlayCritical`, `PlayMinor`, `Clear` |
| `MachineLockTexts` | `Assembly-CSharp` | Italian strings | Constants |
| `MachineLockDebugMenu` | `SI81.UI.Editor` or group Editor | Editor-only tests | `SI81/MachineLock/...` |

### 14.4 Config defaults

```csharp
timeLimitGuided = 900f;
timeLimitRealistic = 600f;
timeLimitStress = 480f;
hardEndRealistic = true;
hardEndStress = true;
hardEndGuided = false;
energizedProbability = 0.5f;
energyDecaySeconds = 90f;
gazeDistance = 3.5f;
gazeAngle = 25f;
gazeDwell = 1.2f;
snapRadius = 0.6f;
inactionSmokeSeconds = 90f;
inactionBadEndingSeconds = 240f;
criticalSafetyPenalty = 10;
waterSafetyPenalty = 5;
criticalDecisionPenalty = 10;
missingAlarmPenalty = 8;
missingMainPenalty = 2;
missingLockoutPenalty = 2;
missingConesPenalty = 4;
missingContainmentPenalty = 5;
timeoutPenalty = 5;
executionEStop = 2;
executionAlarm = 2;
executionIsolation = 3;
executionZeroEnergy = 2;
executionPpe = 3;
executionContainment = 2;
executionExit = 1;
awarenessMax = 20;
awarenessTargets = 6;
timeMax = 5;
```

### 14.5 File tree

```
Assets/_Project/Groups/G_Marius/
├── Code/Scripts/MachineLock/
│   ├── Core/
│   │   ├── SI81.MachineLock.asmdef
│   │   ├── MachineLockSpi.cs
│   │   └── MachineLockConfig.cs
│   └── Runtime/
│       ├── MachineLockScenario.cs
│       ├── MachineLockPress.cs
│       ├── MachineLockEnergyPanel.cs
│       ├── MachineLockPpeCabinet.cs
│       ├── MachineLockPpeItem.cs
│       ├── MachineLockSpillSystem.cs
│       ├── MachineLockSnapZone.cs
│       ├── MachineLockCone.cs
│       ├── MachineLockAbsorbent.cs
│       ├── MachineLockValve.cs
│       ├── MachineLockAlarm.cs
│       ├── MachineLockObservationTarget.cs
│       ├── MachineLockDamageFeedback.cs
│       └── MachineLockTexts.cs
├── Code/Tests/EditMode/
│   ├── SI81.MachineLock.Tests.asmdef
│   └── MachineLockSpiTests.cs
├── Code/Scripts/Editor/
│   └── MachineLockDebugMenu.cs
├── Level/Prefabs/MachineLock/
│   └── ...
├── Level/Scenes/
│   └── SCN_MachineLock.unity
├── Art/Materials/MachineLock/
├── Art/Models/MachineLock/
├── Art/Textures/MachineLock/
├── Audio/Sound/MachineLock/
├── Audio/Music/MachineLock/
└── Docs/MachineLock/
    ├── GDD_MachineLock.md
    ├── ASSET_CREDITS.md
    └── README_IMPLEMENTATION.md
```

The mission asset stays at:
`Assets/_Project/Home/UI/Data/Missions/SO_Mission_MachineLock.asset`.

### 14.6 Mission asset values

Edit in place. Do not duplicate.

| Field | Value |
|---|---|
| `Id` | `08` |
| `Title` | `Machine Lock` |
| `Context` | `Ruolo: operatore di reparto. Entri nella sala di stampaggio: la pressa da 250 t lavora in modo anomalo, l'olio idraulico a 120 °C perde da un tubo e una pozza calda circonda la macchina. Metti in sicurezza la sala e avvisa il preposto.` (235 chars) |
| `Objectives` | 1. `Ferma la pressa e allarma` 2. `Isola l'energia e blocca` 3. `Indossa i DPI e contiene` 4. `Esci e avvisa il preposto` |
| `Criteria` Positive | `Procedura completa e area sicura` |
| `Criteria` Partial | `Area non delimitata o DPI incompleti` |
| `Criteria` Negative | `Macchina ancora attiva o errore grave` |
| `Levels` Guided | unlocked, override 0 |
| `Levels` Operational | locked, override 0 (uses 600 s) |
| `Levels` Crisis | locked, override 480 |
| `Complexity` | `4` |
| `Icon` | Keep the existing icon. |
| `Gradient` | Keep the existing colours. |
| `SceneName` | `SCN_MachineLock` |
| `DesignerNote` | `Guided vertical slice — G_Marius. GDD: Docs/MachineLock/GDD_MachineLock.md.` |
| `IsTraining` | `false` |

### 14.7 Scene integration steps

1. Pull `main`; create branch `scenario/machine-lock`.
2. Verify `git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json` prints nothing.
3. Do **not** run `SI81/Scenari/Nuovo scenario...` — the mission already exists.
4. Duplicate `Assets/_Project/Shared/Level/ScenarioTemplate/SCN_ScenarioTemplate.unity` into `Assets/_Project/Groups/G_Marius/Level/Scenes/` and rename it `SCN_MachineLock` from the Unity Project window. Keep the `.meta` with the scene.
5. Open the scene. Delete `MESH_ExampleSign`, `MESH_ExampleTool`, `MESH_ExampleRisk`, `MNG_DecisionTrigger` and the template floor if it is replaced by the room blockout.
6. Keep `MNG_Light`, `MNG_SpawnPoint`, `MNG_Scenario`.
7. Add `MachineLockScenario` to `MNG_Scenario`, assign `SO_Mission_MachineLock`, set `editorLevel = Guided`, `loadCoreInEditor = true`, remove the base `ScenarioController`, and **clear** `spawnPoint` (leave it empty).
8. Add `SpawnPoint` to `MNG_SpawnPoint`; position it at `(-7.6, 0, 0)` with yaw facing east.
9. Build the environment and props. Add `LabelAnchor`, `Interactable`, `ScenarioTrigger` and scenario components.
10. Add `SCN_MachineLock` to Build Settings after `SCN_Core` and `SCN_MockScenario`; keep it active.
11. Run `SI81/Scenari/Valida scenario` until 0 errors.
12. Test from `SCN_Core`: Home → 08 → Mission Brief → Guided → Avvia.

### 14.8 Spawn note

The template’s `ScenarioController.spawnPoint` moves the UI tracking origin. The new `SpawnPoint` component teleports the rig root. Using both produces a double offset. Use **only** `SpawnPoint` and leave `ScenarioController.spawnPoint` empty. If the project later removes `ScenarioController` spawn support, this remains correct.

---

## 15. Implementation plan — GitHub issues and tests

Every phase is one GitHub issue. Create the issue before starting the phase; close it only after its tests pass and evidence is attached.

| Issue | Title | Main work | Tests at the end |
|---|---|---|---|
| P0 | `[MachineLock][P0] Branch, project checks, baseline` | Branch, pull, Git checks, open Unity | `git grep collab-proxy` empty; compile; baseline validator |
| P1 | `[MachineLock][P1] Mission data and scene shell` | Update mission in place; duplicate template; Build Settings; spawn | Mission validation; scene load; spawn exact; no camera/Canvas |
| P2 | `[MachineLock][P2] Blockout and asset placeholders` | Room 12×8×4; press graybox; puddle ring; props; lighting | Walk dry perimeter; colliders; frame time; placeholders named |
| P3 | `[MachineLock][P3] Core interactions` | E-stop, alarm, main switch, lockout, tester, try-start | E-stop stops; display off; lockout requires main off; tester live/zero; try-start powered/isolated |
| P4 | `[MachineLock][P4] PPE` | Cabinet, 4 slots, 6 items, replacement | Slot replacement; 4/4; wrong items do not protect |
| P5 | `[MachineLock][P5] Spill containment` | Valve, 4 quadrants, hazard, 4 cones, 2 mats, 2 socks, snap | Snap radius; 4+4 completion; hazard off; missing-item penalties |
| P6 | `[MachineLock][P6] Observation and Awareness` | 6 targets, gaze, proximity fallback | Gaze dwell; fallback; each target once; Awareness points |
| P7 | `[MachineLock][P7] SPI, time modes, debrief, outcomes` | `MachineLockSpi`, time modes, outcome, debrief cap | Unit tests for Guided/Realistic/Stress at 0, half, limit, after limit; weights; outcomes; debrief cap |
| P8 | `[MachineLock][P8] Damage feedback, audio, VFX, lighting` | URP vignette/fallback, audio, VFX, lighting | Vignette/fallback; audio/VFX; Quest readability/performance |
| P9 | `[MachineLock][P9] QA, Quest, PR` | Full regression, Quest checklist, PR | Validator 0/0; Home → mission → Home; critical paths; Quest checklist |

Labels: `scenario:08`, `machine-lock`, `guided`, `spi`, `assets`, `qa`.

Each issue body must include: goal, scope, files, dependencies, acceptance criteria, test plan, evidence, GDD link, asset audit link.

Unit tests to create in P7:

```
Guided_TimeAtZero_IsFive
Guided_TimeAtHalf_IsTwoPointFive
Guided_TimeAtLimit_IsZero_NoHardEnd
Guided_TimeAfterLimit_IsZero_NoHardEnd
Operational_TimeAtLimit_IsZero_Ends
Crisis_TimeAtLimit_IsZero_Ends
Operational_TimeLimit_Is600
Crisis_TimeLimit_Is480
Guided_TimeLimit_Is900
Spi_Weights_Sum100
Spi_CriticalPenalties
Spi_OutcomeMatrix
Debrief_Cap5Good4Error
```

---

## 16. QA and acceptance

### 16.1 Automated / Editor checks

- `SI81/Scenari/Valida scenario`: 0 errors, 0 warnings.
- `git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json`: empty.
- `git grep -n -E "OVRInput|XRGrabInteractable|XRRayInteractable|XRInteractionManager" -- Assets/_Project/Groups/G_Marius/Code/Scripts/MachineLock`: empty.
- Build Settings: `SCN_Core` index 0, `SCN_MockScenario` next, `SCN_MachineLock` active after them.
- SPI tests: perfect, missing alarm, missing containment, critical contact, water extinguisher, bad ending 2.
- Time tests: Guided 900 soft, Realistic 600 hard, Stress 480 hard; zero at/after limit.
- No `Camera`, `AudioListener`, `Canvas`, `EventSystem`, `MNG_UI`, `MNG_HUD`, `HUD_*`, `PANEL_*` in `SCN_MachineLock`.

### 16.2 Play Mode flow

1. Open `SCN_Core`, press Play.
2. Home → card 08 → Mission Brief → Guided → Avvia.
3. Intro subtitles; objectives visible; timer elapsed.
4. Observe all six targets; hint reaches 6/6.
5. E-stop from the west pedestal; press stops; display stays on.
6. Alarm; beacon and siren.
7. Main off; display dark; lockout; red LED.
8. Tester: LIVE then ZERO after 90 s; try-start no movement.
9. PPE: wrong items, then correct 4; hint 4/4.
10. Valve closed; cones 4/4; mats/socks 4/4; hazard off.
11. Exit; Preposto line; outcome; debrief; score; Home.
12. Replay and choose a different layout pattern.

### 16.3 Time-mode tests

- Guided: at 899 s Time > 0; at 900 s Time = 0 and the mission continues; at 1000 s Time = 0 and the mission continues; exit still ends it.
- Realistic: limit 600 s; at 599 s Time > 0; at 600 s Time = 0 and the mission ends; outcome depends on state.
- Stress: limit 480 s; at 479 s Time > 0; at 480 s Time = 0 and the mission ends; outcome depends on state.
- All modes: Time at 0 s = 5; at half limit = 2.5; at limit = 0; after limit = 0.
- `MachineLockSpi` weights sum to 100; critical penalties; outcome matrix; debrief cap 5 good / 4 error.

### 16.4 Critical-path tests

- Walk into the puddle before E-stop: Decision -10, vignette, respawn, continue.
- Touch live oil: shock, Safety -10, Decision -10, respawn.
- Touch hot oil without PPE: burn, Safety -10, Decision -10, respawn.
- Use water extinguisher: steam/smoke, Safety -5, Decision -10, respawn.
- Try-start before isolation: press restarts, Safety -5, Decision -10, respawn.
- Exit with press running: blocked alert.
- Exit without alarm: Partial, Decision -8.
- Exit without containment: Partial, Decision -5.
- Idle 240 s: Bad Ending 2, Negative.
- Realistic/Stress expiry: mission ends, Time 0, outcome based on state.

### 16.5 Quest 3 checklist

- One rig, one camera, one EventSystem.
- Spawn exactly at the door; no double offset.
- Controller models and rays visible; trigger selects; grip grabs.
- Labels readable at 0.75–4 m; max 3 Risk labels.
- Wrist panel readable; hints update.
- Subtitles readable; sound events subtitled.
- Vignette visible or fallback active.
- No console errors; `[SI81 Input]` lines normal.
- Frame rate stable at 72 Hz; no thermal throttling during a 15-minute run.

### 16.6 Asset tests

- Every missing asset has a named placeholder or a documented permissive replacement.
- No `Shared/` file modified; no other group’s file modified.
- `ASSET_CREDITS.md` exists and lists every downloaded asset with license.
- 4K textures are downscaled for Quest.

### 16.7 Definition of Done

- [ ] P0–P9 GitHub issues exist and are linked to the GDD/audit.
- [ ] Every phase has tests and evidence attached.
- [ ] `SO_Mission_MachineLock` updated in place; no duplicate.
- [ ] `SCN_MachineLock` in Build Settings after shared scenes.
- [ ] `MNG_Scenario` has `MachineLockScenario` and the mission.
- [ ] `SpawnPoint` on `MNG_SpawnPoint`; `ScenarioController.spawnPoint` empty.
- [ ] Guided playable; Operational/Crisis locked but time logic tested.
- [ ] All 7 tasks and 4 objectives work.
- [ ] Full weighted SPI and debrief.
- [ ] Critical errors, respawn, inaction, hard-time expiry.
- [ ] No camera, Canvas, EventSystem, XRI, OVRInput, shared UI edits.
- [ ] Validator 0 errors; Console clean.
- [ ] Full flow from Home to Home.
- [ ] Quest 3 test passed.
- [ ] Code comments and `/// <summary>` in English; player text Italian.
- [ ] Asset credits complete.
- [ ] Git commit conventional; PR description includes evidence.

---

## 17. Asset plan

Source: `ASSET_AUDIT_Scenario08_MachineLock.txt` (2026-11-26). The audit’s narrative mentions the old “maintainer / Man Down” placeholder; the GDD story, objectives and interactions are authoritative. Use the audit only for asset availability.

| Asset | Status | Decision |
|---|---|---|
| Industrial factory modular kit | MISSING | Use `Chemical_Alert_Room` modular shell (`MESH_LATO_*`, `MESH_PAVIMENTO_TEAM`, `MESH_TETTO_TEAM`) copied into `G_Marius`; if it cannot be scaled to 12×8×4 cleanly, build a graybox with the same materials. |
| Industrial concrete floor PBR | OK | Copy `Material.036` → `MAT_ConcreteFloorWorn`; downscale 4K textures to 1024/2048 ASTC for Quest. |
| Floor safety marking decal | MISSING | Create `TEX_FloorSafetyMarking_*` + `MAT_FloorSafetyMarking_*`; use a URP Decal Projector only if the renderer feature is already enabled, otherwise a thin quad 0.01 m above the floor. |
| Industrial door | PARTIAL | Use `MESH_PORTA_CAMION_TEAM` or `PortaCamion.prefab`; rename the copy `MESH_IndustrialDoor_RollUp`. |
| Industrial pipes | MISSING | Create `MESH_IndustrialPipe_Straight/_Elbow/_Flange/_Support` from primitives; `MESH_Folgorazione_TuboDiCemento` is only a fallback. |
| Hydraulic press | MISSING | Create a graybox `MESH_HydraulicPress` with a separate `MESH_HydraulicPress_Ram` child and `MESH_HydraulicPress_ControlPanel`. This is the main blocker; do not wait for final art. |
| Industrial panel / display | PARTIAL | Use `Quadro Elettrico.fbx`, `MESH_PULSANTI_TEAM` and a new `MESH_IndustrialHMI` with `MAT_HMI_Screen` (emissive). |
| Emergency stop button | PARTIAL | Use `MESH_PULSANTI_TEAM`; create `MESH_EmergencyStopButton` with a red mushroom and yellow backplate (`MAT_EmergencyStop_Red/_Yellow`). |
| Disconnect switch / meter | PARTIAL | Use `Quadro Elettrico.fbx` + `Manopola.fbx`; create `MESH_DisconnectSwitch`, `MESH_EnergyMeter`; no padlock mesh. |
| Absorbent mats | OK | Use `MESH_PANNO_ASSORBENTE_TEAM.fbx`; copy to `MESH_OilAbsorbentMat`; add our snap/interaction components. |
| Cones | OK | Use `MESH_Magazzino_Cono.fbx`; copy to `MESH_Cone`; the Ludovica prefab is reference only. |
| Face shield | MISSING | Use `OCCHIALI_PROTETTIVI.fbx` as a temporary stand-in, or create a simple transparent visor `MESH_FaceShield` + `MAT_FaceShield_Visor`. |
| Apron | MISSING | Create a simple `MESH_ChemicalApron` + `MAT_Apron_Rubber`. |
| Safety boots | OK (ADR-0004) | Modeled as `MESH_SafetyBoots` (high-top leather work boot with steel toe cap and safety-yellow sole, `MAT_SafetyBoot_Leather` / `MAT_SafetyBoot_Sole`) occupying the `Feet` PPE slot. |
| Heat-resistant gloves | PARTIAL | Use `GUANTO.fbx`; styled with `MAT_HeatGlove` + `NORMAL_GUANTO.png` (`Gloves` slot). |
| Surgical mask | OK | Modeled as a low-poly pleated hospital-blue disposable mask with white ear loops (`MAT_SurgicalMask_Blue`, `Face` slot distractor; replaces 15,946-tri `MASCHERINA.fbx` to hold the 100k Quest 3 budget). |
| Latex gloves | PARTIAL | Use `GUANTO.fbx`; styled with white/cream `MAT_Latex` (`Gloves` slot distractor). |
| Respirator | SUPERSEDED | Superseded by Safety Boots S3 on `Feet` slot (ADR-0004). |
| Fire extinguisher | OK | Use `Estintore.fbx`; do **not** use `MESH_FireExtinguisher.prefab` (missing GUIDs in this checkout). |
| Painted steel | PARTIAL | Copy `MAT_OfficePropWhiteMetallic` or `MAT_SteelDark` → `MAT_PaintedSteel`. |
| Polished steel | OK | Copy `MAT_OfficePropPolishedMetal`. |
| Concrete | OK | Use `Material.036` / `Material.038`; group copies `MAT_ConcreteFloorWorn` / `MAT_ConcreteLayers`. |
| Oil PBR | MISSING | Create `MAT_Oil` (dark brown/black, smoothness 0.9); optional `TEX_OilStain_*` later. |

Asset rules:

1. `[OK]` → use or copy into `G_Marius`; never edit `Shared/` originals.
2. `[PARTIAL]` → use the closest listed asset, rename the copy to the production name, document the deviation.
3. `[MISSING]` → create a graybox/primitive placeholder with the final name, or download a permissive FBX/audio asset. Record source, author and license in `Docs/MachineLock/ASSET_CREDITS.md`.
4. Do not edit other groups’ branches. Read/copy only.
5. Placeholder roots keep the final names so final assets can replace the visual children without breaking references.
6. Downscale 4K textures to 1024/2048 ASTC for Quest.
7. No new packages. URP decals only if the renderer feature is already enabled.
8. No padlock prop: lockout remains an abstract interaction on the panel.

---

## 18. Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Team Ludovica rejects explicit weighted score | High | SPI isolated in `MachineLockSpi`; fallback to equal average is one method change. Document both. |
| Text-only DPI is hard to track | Medium | Persistent `SetHint("DPI n/4")`, per-item feedback, final alert; request HUD widget later. |
| No padlock prop weakens LOTO legibility | Medium | Red LOCK LED, label, feedback, try-start proof. |
| Gaze observation unreliable | Medium | `ObservationMode.Proximity` fallback; test both. |
| URP post-processing not visible on Quest | Medium | Fallback red light + haptic + feedback; request HUD overlay later. |
| Double spawn offset | High | Use only `SpawnPoint`; leave `ScenarioController.spawnPoint` empty; verify position. |
| Random layout creates unfair path | High | Three patterns validated; dry perimeter ≥1.5 m; E-stop/alarm/exit fixed. |
| Missing models/audio | High | Asset audit fallbacks; graybox/primitive placeholders; permissive downloads with credits. |
| Debrief overflow | Medium | SPI caps 5 good / 4 bad-critical; critical lines first. |
| Performance on Quest | Medium | Budgets, baked lighting, particle caps, one dynamic shadow light. |
| Safety-domain inaccuracy | High | Qualified safety representative reviews procedure and texts before release. |
| Time-mode regression | Medium | Unit tests for Guided/Realistic/Stress at 0, half, limit, after limit. |
| “Manometro” confusion | Low | Use voltage tester; keep pressure gauge only on the press if modeled. |

---

## 19. Open items

- Final press, props and audio from the owner.
- Reference images `image.png` and `screen_numero_1.jpg` are not available; layout follows this GDD.
- Safety representative sign-off.
- Team Ludovica approval for the weighted SPI deviation and a future DPI HUD widget.
- Quest validation of the URP vignette.
- Final lighting bake and performance pass.
- Decision on when to unlock Operational/Crisis after QA.

---

## Appendix A — Exact mission strings

```text
Title: Machine Lock
Id: 08
Context: Ruolo: operatore di reparto. Entri nella sala di stampaggio: la pressa da 250 t lavora in modo anomalo, l'olio idraulico a 120 °C perde da un tubo e una pozza calda circonda la macchina. Metti in sicurezza la sala e avvisa il preposto.
Objectives:
- Ferma la pressa e allarma
- Isola l'energia e blocca
- Indossa i DPI e contiene
- Esci e avvisa il preposto
Criteria:
- Positivo: Procedura completa e area sicura
- Parziale: Area non delimitata o DPI incompleti
- Negativo: Macchina ancora attiva o errore grave
DesignerNote: Guided vertical slice — G_Marius. GDD: Docs/MachineLock/GDD_MachineLock.md.
```

## Appendix B — Placeholder dimensions

| Prop | Size (m) | Notes |
|---|---|---|
| Press base | 3.0 × 1.2 × 2.5 | Plus crown and columns |
| Press crown | 3.0 × 1.2 × 2.5 | At y = 2.8 |
| Ram | 1.2 × 0.4 × 1.2 | Travel 0.6 |
| E-stop pedestal | 0.4 × 1.1 × 0.4 | Mushroom 0.12 |
| Panel | 1.0 × 1.2 × 0.25 | At y = 1.2 |
| PPE cabinet | 1.2 × 2.0 × 0.6 | Door 1.2 × 1.8 |
| Spill kit | 1.0 × 0.4 × 0.6 | Cart/bin |
| Cone | 0.5 high | Base 0.3 |
| Mat | 1.0 × 0.02 × 0.8 | Flat |
| Sock | 1.2 × 0.15 × 0.15 | Cylinder |
| Extinguisher | 0.6 high | 0.15 diameter |
| Valve wheel | 0.3 diameter | 90° rotation |
| Door opening | 1.2 × 2.2 | West wall |

## Appendix C — Naming

- Scene: `SCN_MachineLock`
- Mission: `SO_Mission_MachineLock`
- Scenario: `MNG_Scenario`
- Spawn: `MNG_SpawnPoint`
- Scripts: no prefix, PascalCase.
- SPI: `MachineLockSpi`
- Config: `SO_MachineLockConfig`
- Damage profile: `SO_MachineLockDamageProfile`
- Texts: `MachineLockTexts`
- Prefabs/models: `MESH_...`
- Materials: `MAT_...`
- Textures: `TEX_...`
- Audio: `SFX_...`
- VFX: `VFX_...`
- Assemblies: `SI81.MachineLock`, `SI81.MachineLock.Tests`

## Appendix D — Debug menu

Editor-only, wrapped in `#if UNITY_EDITOR`.

- `SI81/MachineLock/Run Score Tests` — runs the score/time cases and logs pass/fail.
- `SI81/MachineLock/Log State` — logs task flags, dimensions, SPI, outcome.
- `SI81/MachineLock/Force Energized` — Play Mode only.
- `SI81/MachineLock/Force Zero Energy` — Play Mode only.
- `SI81/MachineLock/Complete Current Objective` — Play Mode only, QA.
- Do not use `OVRInput`; use `Keyboard.current` only in Editor.

---

*End of GDD v1.1.*