# Asset Credits — Scenario 08 Machine Lock

| Field | Value |
|---|---|
| Document | `ASSET_CREDITS.md` |
| Scenario | 08 — Machine Lock (`SCN_MachineLock`) |
| Owner | G_Marius |
| Branch | `scenario/machine-lock` |
| Asset source list | [`../ASSET_AUDIT_Scenario08_MachineLock.txt`](../ASSET_AUDIT_Scenario08_MachineLock.txt) |
| Design reference | [`GDD_MachineLock.md`](GDD_MachineLock.md) — §17 Asset plan, ADR-0005, ADR-0006 |
| Status | Complete for Issue #36 (#37–#42): Blender-authored `MESH_WorkshopShell` (~3.5k tris) + 20 Blender hero/dressing/spill FBXs, 7 PolyHaven CC0 PBR texture sets, 14 UNI EN ISO 7010 signs, 6 brushed-metal plaques, organic `MESH_SPLAT` oil puddle, 18 authentic workshop audio clips |

---

## 1. Purpose

Every third-party, shared-library and authored asset used by Scenario 08 gets one row in this file so the project can prove, at review time, where each asset came from and under which license it is used.

The scenario ships only assets that are:

1. copied from or referenced directly in `Assets/_Project/Shared/`, or
2. created inside `Assets/_Project/Groups/G_Marius/`, or
3. downloaded under a permissive license (`CC0-1.0`, `CC-BY-4.0`, `Public Domain`) recorded below.

## 2. Rules

- **Never edit `Assets/_Project/Home/` or pre-existing `Assets/_Project/Shared/` originals.** Copy Shared textures/materials into `Assets/_Project/Groups/G_Marius/` before customizing, or reference Shared FBXs read-only.
- **Root names keep the production convention** (`MESH_HydraulicPress`, `MAT_Oil`, `SFX_AlarmSiren`, …).
- **No new packages.**
- **Android / Quest 3 texture optimization:** 1024 ASTC 6×6 for PBR maps, 512/1024 ASTC 4×4 for ISO 7010 signage and plaques; zero displacement maps on Quest 3 materials.

## 3. Status vocabulary

| Status | Meaning |
|---|---|
| `SHARED` | Supplied in `Assets/_Project/Shared/` (referenced read-only or copied into `G_Marius`). |
| `GROUP` | Authored inside `G_Marius` (Blender 5.0 FBX, shader, material, prefab, ISO 7010 sign, config). |
| `BRANCH` | Copied from another SI81 team branch (`origin/gruppo_ludovica`) into `G_Marius`. |
| `DOWNLOADED` | Third-party asset with a verified reachable URL and redistribution license recorded below. |
| `GENERATED` | Procedurally produced in-repo (`MachineLockSceneBuilder` VFX, lighting, procedural normal maps). |

---

## 4. Credit tables

### 4.1 Shared library assets referenced read-only or copied into `G_Marius`

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `MAT_PaintedSteel` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_PaintedSteel.mat` | Material | `SHARED` | `Assets/_Project/Shared/Art/Materials/MAT_OfficePropWhiteMetallic.mat` | SI81 shared library | Project-owned (educational use) | Copied and renamed, URP Lit | P2 |
| 2 | `MAT_PolishedSteel` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_PolishedSteel.mat` | Material | `SHARED` | `Assets/_Project/Shared/Art/Materials/MAT_OfficePropPolishedMetal.mat` | SI81 shared library | Project-owned (educational use) | Copied and renamed, URP Lit | P2 |
| 3 | `TEX_Glove_Normal` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_Glove_Normal.png` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/NORMAL_GUANTO.png` | SI81 shared library | Project-owned (educational use) | Copied into `G_Marius`; NormalMap; Android 1024 ASTC 6×6 | Issue #35 |

Shared FBX meshes referenced read-only from `Assets/_Project/Shared/` (zero edits to `Shared/`):

- **PPE & Controls:** `GUANTO.fbx`, `OCCHIALI_PROTETTIVI.fbx`, `MESH_PULSANTI_TEAM.fbx`, `MESH_SCATOLA_APRIBILE_ROSSA_TEAM.fbx`.
- **Background Factory Bay & Lighting:** `Muletto.fbx` (666 tris), `MESH_Folgorazione_Generatore.fbx` (1,322 tris), `MESH_Magazzino_Barriera.fbx`, `MESH_Magazzino_Scaffale.fbx`, `MESH_Folgorazione_Barile.fbx`, `Pallet.fbx`, `MESH_VENTOLA_TEAM.fbx`, `MESH_Magazzino_LuceDaSoffitto.fbx`.

*(Superseded in Issue #36: `PFB_OldIndustrialBuilding.fbx`, `MESH_PORTA_CAMION_TEAM.fbx`, `MESH_RilevatoreDiGas.fbx`, `Estintore.fbx`, `MESH_Magazzino_Cono.fbx`, `MESH_PANNO_ASSORBENTE_TEAM.fbx`, `MESH_SALSICCIOTTO_ONEDRIVE.fbx`, and `TEXTURE_SAUSAGES.png` are no longer referenced by `SCN_MachineLock`.)*

### 4.2 Group-authored Blender 5.0 3D models (`Assets/_Project/Groups/G_Marius/Art/Models/`)

| # | Asset | Triangles | Status | Source / Author | License | Notes | Phase |
|---|---:|---:|---|---|---|---|---|
| 1 | `MESH_WorkshopShell.fbx` | 3,516 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | `28 × 13 × 6.5 m` Italian industrial workshop shell with H-beam columns, Warren roof trusses, clerestory windows, West exit vestibule, East electrical backing wall (ADR-0005) | Issue #37 |
| 2 | `MESH_IndustrialWorkbench.fbx` | 460 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Hardwood butcher-block workbench with red drawers, pegboard, bench vise | Issue #37 |
| 3 | `MESH_ToolCabinet.fbx` | 464 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Lista-style 6-drawer steel workshop tool cabinet | Issue #37 |
| 4 | `MESH_BundDrums.fbx` | 1,016 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Two ribbed 200 L hydraulic oil drums on yellow spill-containment bund | Issue #37 |
| 5 | `MESH_SteelBollard.fbx` | 364 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Bolted yellow/black industrial steel safety bollard | Issue #37 |
| 6 | `MESH_SheetPallet.fbx` | 448 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Euro pallet loaded with strapped stamped metal sheet blanks | Issue #37 |
| 7 | `MESH_OverheadConduit.fbx` | 432 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Overhead armored cable tray & risers connecting Electrical Bay to Press | Issue #37 |
| 8 | `MESH_HydraulicPressBody.fbx` | 3,448 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Chamfered 250 t H-frame hydraulic press with tie rods, bolster, hydraulic cylinders, guard rails, HMI arm, 120 °C gauge, east leak manifold | Issue #38 |
| 9 | `MESH_HydraulicPressRam.fbx` | 444 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Sliding cast-steel hydraulic ram head, hazard pinch edge, piston & guide rods | Issue #38 |
| 10 | `MESH_SubStationDisconnect.fbx` | 680 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | IP65 steel disconnect enclosure with escutcheon & recessed LED bezels | Issue #38 |
| 11 | `MESH_DisconnectHandle.fbx` | 160 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Red/black rotary throw disconnect lever (pivoted on X rotation shaft) | Issue #38 |
| 12 | `MESH_SubStationLockout.fbx` | 836 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | IP65 yellow/slate LOTO enclosure with steel padlock hasp & parked padlock | Issue #38 |
| 13 | `MESH_SubStationTester.fbx` | 1,100 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Authentic industrial voltage tester / multimeter cabinet with probes & dial (replaces cyan gas detector) | Issue #38 |
| 14 | `MESH_SteelExitDoorFrame.fbx` | 344 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Beveled steel exit jambs, 3 North barrel hinges, threshold sill, overhead E002 light box | Issue #38 |
| 15 | `MESH_SteelExitDoorLeaf.fbx` | 360 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Hinged Italian REI-120 steel exit door leaf with wired-glass vision slit, kickplate, closer | Issue #38 |
| 16 | `MESH_PanicBar.fbx` | 300 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Signal-green EN 1125 horizontal panic bar (*maniglione antipanico*) | Issue #38 |
| 17 | `MESH_WaterExtinguisherHero.fbx` | 766 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Uniform-scale Y-up UNI EN 3 water fire extinguisher with Class-A blue band, pressure gauge, hose & nozzle | Issue #39 |
| 18 | `MESH_TrafficConeHero.fbx` | 224 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Uniform-scale Y-up safety traffic cone with weighted rubber base & dual reflective collars | Issue #39 |
| 19 | `MESH_AbsorbentMatHero.fbx` | 248 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Quilted pillowed polypropylene oil-absorbent pad (`0.95 × 0.034 × 0.75 m`) | Issue #39 |
| 20 | `MESH_AbsorbentSockHero.fbx` | 412 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Curved fabric-sleeved industrial oil-absorbent sock/boom with crimped metal end clips | Issue #39 |
| 21 | `MESH_OilPuddleSplat.fbx` | 1,536 | `GROUP` | Derived from team-provided `MESH_SPLAT.glb`, normalized & split into 4 quadrants in Blender 5.0 | Project-owned | Organic 7.0 × 5.0 m glossy hot-oil puddle (`MESH_PuddleQuadrantNE/NW/SE/SW`) bound to `MAT_Oil` | Issue #39 |
| 22 | `MESH_SafetyBootsHero.fbx` | 5,152 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Sculpted S3 steel-toe leather work boots pair with lugged soles, steel toe bumper, eyelets, crisscross laces & padded collar | Issue #36 |
| 23 | `MESH_ChemicalApronHero.fbx` | 1,660 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Draped PVC/neoprene industrial splash bib apron with gravity folds, reinforced safety stripes, grommets, neck/waist straps & peg hook | Issue #36 |
| 24 | `MESH_FaceShieldHero.fbx` | 2,064 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Contoured EN 166 full face shield with safety-yellow HDPE browguard, ratchet suspension harness & curved clear polycarbonate visor | Issue #36 |
| 25 | `MESH_HeatGlovesHero.fbx` | 3,014 | `DOWNLOADED` | Derived from Poly Haven `garden_gloves_01` (`https://polyhaven.com/a/garden_gloves_01`, `CC0-1.0`) + flared EN 407 gauntlet cuffs authored in Blender 5.0 | Poly Haven (`CC0-1.0`) + `G_Marius` | Decimated photogrammetry glove pair enhanced with flared aluminized forearm cuffs & EN 407 heat-leather finish | Issue #36 |
| 26 | `MESH_LatexGlovesHero.fbx` | 3,440 | `SHARED` | Normalized & mirrored L+R pair derived from `Assets/_Project/Shared/Art/Models/GUANTO.fbx` | SI81 shared library + `G_Marius` | Centered disposable nitrile/latex glove pair (Wrong DPI distractor) preserving baked UV map for `TEX_Glove_Normal.png` | Issue #36 |
| 27 | `MESH_SurgicalMaskHero.fbx` | 1,020 | `GROUP` | Authored in Blender 5.0 (`G_Marius`) | Project-owned | Smooth curved 3-pleat non-woven medical surgical mask with pinched nose bridge & 3D elastic ear-loops (Wrong DPI distractor) | Issue #36 |

### 4.3 Scenario prefabs (12 production roots)

All 12 prefabs keep their production root names under `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/` and combine Blender hero meshes, PolyHaven CC0 PBR materials, UNI EN ISO 7010 signage, and engraved brushed-aluminum plaques.

| # | Production prefab | Source & key components | Phase |
|---|---|---|---|
| 1 | `MESH_HydraulicPress` | `MESH_HydraulicPressBody.fbx` + `MESH_HydraulicPressRam.fbx`, `MAT_PressSteel_Green`, HMI fault display, 120 °C gauge, `MAT_Plaque_PressNameplate`, `MachineLockPress` | P2 / Issue #38 |
| 2 | `MESH_EStopPedestal` | Pedestal + `MESH_PULSANTI_TEAM.fbx` + red E-stop mushroom + green Try-Start + `MAT_Plaque_EStop` | P2 / Issue #40 |
| 3 | `MESH_AlarmButton` | Break-glass housing, rotating emissive beacon + realtime point light, looping spatial `SFX_AlarmSiren`, `MachineLockAlarm` | P2 / Issue #40 |
| 4 | `MESH_ElectricalPanel` | 3 physically separated (≥ 0.88 m) IP65 steel Sub-Stations (`MESH_SubStationDisconnect`, `MESH_DisconnectHandle`, `MESH_SubStationLockout`, `MESH_SubStationTester`), recessed LEDs, brushed-aluminum plaques, `MachineLockEnergyPanel` | P2 / Issue #38 |
| 5 | `MESH_PpeCabinet` | Wide non-blocking sliding-door locker (`1.80 × 2.05 × 0.44 m`), 4 correct PPE + 2 paired distractors (`GUANTO.fbx`, `OCCHIALI_PROTETTIVI.fbx`, `MESH_SafetyBoots`, pleated surgical mask), 6 anti-aliased `EquippedBadge` indicators, `MachineLockPpeCabinet` | P2 / Issue #35 |
| 6 | `MESH_SpillKit` | Red spill-kit trolley (`MESH_SCATOLA_APRIBILE_ROSSA_TEAM.fbx`) with 4 nested `MESH_Cone`, 2 `MESH_AbsorbentMat`, 2 `MESH_AbsorbentSock` | P2 / Issue #39 |
| 7 | `MESH_WaterExtinguisher` | Uniform-scale `MESH_WaterExtinguisherHero.fbx` + wall bracket + attached `MESH_IsoSign_F001` (travels across Patterns A/B/C), `VFX_WaterSpray`, `MachineLockExtinguisher` | P2 / Issue #39 |
| 8 | `MESH_Cone` | Uniform-scale `MESH_TrafficConeHero.fbx` (`MAT_TrafficCone_Body`, `MAT_ConeReflective`), player-body no-clip safety, `MachineLockCone` | P2 / Issue #39 |
| 9 | `MESH_AbsorbentMat` | Quilted `MESH_AbsorbentMatHero.fbx` (`MAT_AbsorbentMat` + quilted normal map), `MachineLockAbsorbent` | P2 / Issue #39 |
| 10 | `MESH_AbsorbentSock` | Curved fabric-sleeved `MESH_AbsorbentSockHero.fbx` (`MAT_AbsorbentSock`, zero sausage texture), `MachineLockAbsorbent` | P2 / Issue #39 |
| 11 | `MESH_Valve` | Bright red spoked handwheel + pulsing Guided cue ring on east pipe outside puddle, `MachineLockValve` | P2 / Issue #35 |
| 12 | `MESH_IndustrialDoor_RollUp` | Hinged REI-120 steel door (`MESH_SteelExitDoorFrame.fbx`, `MESH_SteelExitDoorLeaf.fbx`, `MESH_PanicBar.fbx`, `MAT_ExitDoorSteel`, `MAT_PanicBar`, zero `MESH_DoorRollHousing`), overhead `MESH_IsoSign_E002`, `MESH_ExitBeacon`, `MachineLockExitDoor` | P2 / Issue #38, #41 |

### 4.4 Signage, Plaques, Shaders & Procedural PBR Maps (`G_Marius`)

| # | Asset | Unity path | Type | Status | Author | License | Notes | Phase |
|---|---|---|---|---|---|---|---|---|
| 1 | `TEX_IsoSign_*` & `MAT_IsoSign_*` (14 signs) | `Assets/_Project/Groups/G_Marius/Art/{Textures,Materials}/MachineLock/{TEX,MAT}_IsoSign_*.{png,mat}` | Texture + Material | `GROUP` | G_Marius | Project-owned | Crisp 4×-supersampled anti-aliased UNI EN ISO 7010 / D.Lgs. 81/08 signs (`W012`, `W017`, `W024`, `W011`, `P011`, `M008`, `M009`, `M013`, `M026`, `PpeBoard`, `F001`, `F005`, `E001`, `E002`), Android 512/1024 ASTC 4×4 | Issue #40 |
| 2 | `TEX_Plaque_*` & `MAT_Plaque_*` (6 metal plaques + 6 PPE badges) | `Assets/_Project/Groups/G_Marius/Art/{Textures,Materials}/MachineLock/{TEX,MAT}_Plaque_*.{png,mat}` | Texture + Material | `GROUP` | G_Marius | Project-owned | Engraved brushed-aluminum nameplates (`MainSwitch`, `Lockout`, `Tester`, `EStop`, `PressNameplate`, `DeptHeader`) and 6 crisp anti-aliased PPE shelf equipped badges, Android 512/1024 ASTC 4×4 | Issue #40 |
| 3 | `TEX_AbsorbentMat_{Albedo,QuiltedNormal}`, `TEX_AbsorbentSock_{FabricAlbedo,FabricNormal}`, `TEX_OilRipple_Normal` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_*.png` | Texture | `GENERATED` | G_Marius | Project-owned | Procedural quilted pad, woven polypropylene fabric, and subtle wet-oil ripple PBR maps (1024 ASTC 6×6) | Issue #39 |
| 4 | `SH_MachineLockDamageOverlay` & `MAT_DamageOverlay` | `Assets/_Project/Groups/G_Marius/Code/Shaders/SH_MachineLockDamageOverlay.shader` | Shader + Material | `GROUP` | G_Marius | Project-owned | VR-stereo-safe (`Single-Pass Instanced`) peripheral red damage vignette & respawn blink overlay (ADR-0003) | Issue #35 |
| 5 | `SO_MachineLockDamageProfile` | `Assets/_Project/Groups/G_Marius/Level/Data/SO_MachineLockDamageProfile.asset` | VolumeProfile | `GROUP` | G_Marius | Project-owned | URP `VolumeProfile` with red `Vignette` and `ColorAdjustments` sub-assets | P8 |

### 4.5 Downloaded third-party textures (Poly Haven CC0-1.0)

| # | Production asset set | Unity path prefix | Status | Source | Author | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|
| 1 | `TEX_WorkshopFloor_{Albedo,Normal,Rough}` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_WorkshopFloor_*` | `DOWNLOADED` | `https://polyhaven.com/a/concrete_floor_02` | Rob Tuytel (Poly Haven) | `CC0-1.0` | 1K download; Android 1024 ASTC 6×6, no displacement | Issue #37 |
| 2 | `TEX_FactoryWall_{Albedo,Normal,Rough}` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_FactoryWall_*` | `DOWNLOADED` | `https://polyhaven.com/a/factory_wall` | Rob Tuytel (Poly Haven) | `CC0-1.0` | 1K download; Android 1024 ASTC 6×6, no displacement | Issue #37 |
| 3 | `TEX_MetalPlate_{Albedo,Normal,Rough}` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_MetalPlate_*` | `DOWNLOADED` | `https://polyhaven.com/a/metal_plate` | Rob Tuytel (Poly Haven) | `CC0-1.0` | 1K download; Android 1024 ASTC 6×6, no displacement | Issue #37 |
| 4 | `TEX_CorrugatedIron_{Albedo,Normal,Rough}` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_CorrugatedIron_*` | `DOWNLOADED` | `https://polyhaven.com/a/corrugated_iron` | Jenelle van Heerden & Dimitrios Savva (Poly Haven) | `CC0-1.0` | 1K download; Android 1024 ASTC 6×6, no displacement | Issue #37 |
| 5 | `TEX_WorkbenchWood_{Albedo,Normal,Rough}` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_WorkbenchWood_*` | `DOWNLOADED` | `https://polyhaven.com/a/dark_wooden_planks` | Amal Kumar (Poly Haven) | `CC0-1.0` | 1K download; Android 1024 ASTC 6×6, no displacement | Issue #37 |
| 6 | `TEX_GreenMetalRust_{Albedo,Normal,Rough}` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_GreenMetalRust_*` | `DOWNLOADED` | `https://polyhaven.com/a/green_metal_rust` | Rob Tuytel (Poly Haven) | `CC0-1.0` | 1K download; Android 1024 ASTC 6×6, no displacement | Issue #35 |
| 7 | `TEX_ConcreteWall009_{Albedo,Normal,Rough}` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteWall009_*` | `DOWNLOADED` | `https://polyhaven.com/a/concrete_wall_009` | Charlotte Baglioni (Poly Haven) | `CC0-1.0` | 1K download; Android 1024 ASTC 6×6, no displacement | Issue #35 |

### 4.6 Audio (20 clips)

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `SFX_PressLoop` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_PressLoop.wav` | Audio (loop) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/c/cf/WWS_Pressing.ogg` | Work With Sounds / Technical Museum of Slovenia | `CC-BY-4.0` | 44.1 kHz mono WAV, 6.00 s seamless loop | Issue #35 |
| 2 | `SFX_PressStop` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_PressStop.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Layered power-down + low rumble | Issue #35 |
| 3 | `SFX_MechanicalButton` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_MechanicalButton.wav` | Audio (one-shot) | `GROUP` | Team-supplied `G_Marius` audio drop (wired to `MachineLockPress.clackClip`) | G_Marius | Project-owned | Wired to E-stop emergency mushroom press | Issue #40 |
| 4 | `SFX_EStopClack` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_EStopClack.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Mechanical latch for PPE cabinet sliding doors | Issue #35 |
| 5 | `SFX_AlarmButton` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_AlarmButton.wav` | Audio (one-shot) | `BRANCH` | `origin/gruppo_ludovica`: `SFX_FKC_Alarm.wav` | Team Ludovica / Wyzowl | `CC0-1.0` | Renamed to `SFX_AlarmButton.wav` | Issue #35 |
| 6 | `SFX_AlarmSiren` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_AlarmSiren.wav` | Audio (loop) | `GROUP` | Updated `G_Marius` workshop alarm siren WAV | G_Marius | Project-owned | Looping 3D spatial Logarithmic rolloff (`2.5–24 m`, volume `0.65`) | Issue #40 |
| 7 | `SFX_BreakerOff` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_BreakerOff.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Heavy disconnect breaker click | Issue #35 |
| 8 | `SFX_Lockout` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_Lockout.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Heavy mechanical lock (LOTO & Exit Door panic-bar latch) | Issue #35 / #41 |
| 9 | `SFX_OilLeak` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_OilLeak.wav` | Audio (loop) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/b/b1/WWS_Airpumpsteamlocomotive.ogg` | Work With Sounds / Konrad Gutkowski | `CC-BY-4.0` | 4.00 s seamless pressurized hiss loop | Issue #35 |
| 10 | `SFX_ValveTurn` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_ValveTurn.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Heavy mechanical ratchet | Issue #35 |
| 11 | `SFX_TesterBeep` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_TesterBeep.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Clear digital multimeter beep | Issue #35 |
| 12 | `SFX_EquipPpe` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_EquipPpe.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Fabric slide foley | Issue #35 |
| 13 | `SFX_Snap` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_Snap.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Crisp snap placement click | Issue #35 |
| 14 | `SFX_SteamBurst` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_SteamBurst.wav` | Audio (one-shot) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/2/22/WWS_Steamlocomotive.ogg` | Work With Sounds / Konrad Gutkowski | `CC-BY-4.0` | 1.80 s pressurized steam burst | Issue #35 |
| 15 | `SFX_ElectricShock` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_ElectricShock.wav` | Audio (one-shot) | `GROUP` | Team-supplied `G_Marius` audio drop (wired to `MachineLockDamageFeedback.shockClip`) | G_Marius | Project-owned | Replaces `SFX_Shock.wav` on live-oil shock critical event | Issue #40 |
| 16 | `SFX_Shock` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_Shock.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` | Wyzowl | `CC0-1.0` | Retained fallback clip | Issue #35 |
| 17 | `SFX_Burn` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_Burn.wav` | Audio (one-shot) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/9/9d/Zzrrrp_sizzle.ogg` | stephan (Wikimedia Commons) | `Public Domain` | 1.50 s hot-oil sizzle burst | Issue #35 |
| 18 | `SFX_AmbientFactory` | `Assets/_Project/Groups/G_Marius/Audio/Music/MachineLock/SFX_AmbientFactory.wav` | Audio (loop) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/d/d9/WWS_HydropowerStation.ogg` | Work With Sounds / Technical Museum of Slovenia | `CC-BY-4.0` | 8.00 s seamless industrial machine-hall hum | Issue #35 |
| 19 | `SFX_FeedbackPositive` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_FeedbackPositive.wav` | Audio (one-shot) | `GENERATED` | Procedurally synthesized 44.1 kHz mono WAV (`MachineLockActionFeedback.BuildPositiveSamples`) | G_Marius | Project-owned | Ascending D5→A5 chime for valid safety actions (`MachineLockActionFeedback`) | Issue #46 |
| 20 | `SFX_FeedbackNegative` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_FeedbackNegative.wav` | Audio (one-shot) | `GENERATED` | Procedurally synthesized 44.1 kHz mono WAV (`MachineLockActionFeedback.BuildNegativeSamples`) | G_Marius | Project-owned | Low double-buzz for wrong/blocked actions (`MachineLockActionFeedback`) | Issue #46 |

### 4.7 VFX, Lighting & Wayfinding (`SCN_MachineLock`)

| # | Production item | Scene path | Type | Status | Notes | Phase |
|---|---|---|---|---|---|---|
| 1 | `VFX_OilJet` / `VFX_OilParticles` | `HAZARD_Oil/VFX_OilJet` | VFX | `GENERATED` | Pressurized hot-oil stream + 80-particle spray (`playOnAwake = true`), stops when valve closes | P8 |
| 2 | `VFX_OilPuddle` | `HAZARD_Oil/VFX_OilPuddle` | Mesh / VFX | `GROUP` | 4-quadrant organic `MESH_OilPuddleSplat.fbx` (`MAT_Oil`, ripples), each quadrant hides as absorbents snap | Issue #39 |
| 3 | `VFX_Steam` | `HAZARD_Oil/VFX_Steam` | VFX | `GENERATED` | Ambient hot-oil steam wisps (max 60 particles, `playOnAwake = true`), bursts on water contact | P8 |
| 4 | `VFX_Smoke` | `HAZARD_Oil/VFX_Smoke` | VFX | `GENERATED` | Inaction & water-reaction dark smoke plume (max 120 particles, `playOnAwake = false`) | P8 |
| 5 | `VFX_ElectricalArc` | `ENV_Safety/MESH_ElectricalPanel/SUB_VoltageTester/VFX_ElectricalArc` | VFX | `GENERATED` | Electrical arc burst on live-oil shock (max 40 particles, `playOnAwake = false`) | P8 / Issue #38 |
| 6 | `VFX_AlarmBeacon` | `ENV_Safety/MESH_AlarmButton/VFX_AlarmBeacon` | VFX + Light | `GENERATED` | Rotating emissive dome (`MAT_BeaconRed`) + realtime red point light (no shadows, disabled at start) | P8 |
| 7 | `VFX_DisplayFault` | `ENV_Press/MESH_HydraulicPress/MESH_PressDisplay/VFX_DisplayFault` | VFX marker | `GENERATED` | Drives press HMI blinking fault emission (`MAT_HMI_Screen` → `MAT_HMI_Off` when main power is cut) | P8 |
| 8 | `VFX_WaterSpray` | `ENV_Safety/MESH_WaterExtinguisher/VFX_WaterSpray` | VFX | `GENERATED` | Water spray cone (max 100 particles, `playOnAwake = false`), fires on extinguisher misuse | P8 |
| 9 | `VFX_BaselineMarker` & `VFX_SnapGhost` | `ZONE_Coverage1..4` & `ZONE_ConePoint1..4` | Wayfinding | `GENERATED` | Guided baseline floor outlines (`MAT_BaselineZone`) + green snap ghosts (`MAT_GhostSnap`) | P8 / Issue #35 |
| 10 | `VFX_DamageVignette` + `MAT_DamageOverlay` | `MNG_Scenario/VFX_DamageVignette` | Volume + Overlay | `GENERATED` | Global URP `Volume` (`SO_MachineLockDamageProfile`) + stereo-safe peripheral overlay (`SH_MachineLockDamageOverlay`, ADR-0003) | P8 / Issue #35 |
| 11 | `MESH_ExitBeacon` | `ENV_Room/MESH_Door/MESH_IndustrialDoor_RollUp/MESH_ExitBeacon` | Wayfinding | `GENERATED` | Overhead exit status bar (`MAT_ExitBeacon_Idle` → `MAT_ExitBeacon_Ready` when `TaskEStop && TaskContainment`) | Issue #35 / #38 |
| 12 | `MNG_Light` | `SCN_MachineLock/MNG_Light` | Light | `GENERATED` | Single directional realtime fill light (no shadows) + flat industrial ambient | P2 / Issue #35 |
| 13 | `MESH_CeilingLight01..04` | `ENV_Room/MESH_Signs/MESH_CeilingLight01..04` | Light | `GENERATED` | Four baked ceiling industrial fixtures (`MESH_Magazzino_LuceDaSoffitto.fbx`, zero realtime cost) | P2 / Issue #35 |
| 14 | `LIGHT_DamageFlash` | `MNG_Scenario/LIGHT_DamageFlash` | Light | `GENERATED` | Red point flash pulsed alongside damage overlay on Critical Events (disabled while idle) | P8 / Issue #35 |

---

## 5. Attribution text for the build

```text
Scenario 08 (Machine Lock) includes the following Creative Commons Attribution 4.0 (CC BY 4.0) audio recordings from the Work With Sounds project via Wikimedia Commons (https://creativecommons.org/licenses/by/4.0/):
- "WWS Pressing" by Work With Sounds / Technical Museum of Slovenia (https://upload.wikimedia.org/wikipedia/commons/c/cf/WWS_Pressing.ogg) — edited into a seamless mono loop (SFX_PressLoop.wav).
- "WWS HydropowerStation" by Work With Sounds / Technical Museum of Slovenia (https://upload.wikimedia.org/wikipedia/commons/d/d9/WWS_HydropowerStation.ogg) — edited into a seamless mono loop (SFX_AmbientFactory.wav).
- "WWS Steamlocomotive" by Work With Sounds / Konrad Gutkowski (https://upload.wikimedia.org/wikipedia/commons/2/22/WWS_Steamlocomotive.ogg) — trimmed burst (SFX_SteamBurst.wav).
- "WWS Airpumpsteamlocomotive" by Work With Sounds / Konrad Gutkowski (https://upload.wikimedia.org/wikipedia/commons/b/b1/WWS_Airpumpsteamlocomotive.ogg) — edited into a seamless mono loop (SFX_OilLeak.wav).

Additional CC0-1.0 / Public Domain assets:
- PBR textures "Concrete Floor 02", "Factory Wall", "Metal Plate", "Green Metal Rust" (Rob Tuytel), "Concrete Wall 009" (Charlotte Baglioni), "Corrugated Iron" (Jenelle van Heerden & Dimitrios Savva), "Dark Wooden Planks" (Amal Kumar), and 3D model "Garden Gloves 01" from Poly Haven (https://polyhaven.com/, CC0-1.0).
- Mechanical, digital, and foley SFX from Wyzowl SFX Kit (https://github.com/wyzowl/sfx-kit, CC0-1.0) and Team Ludovica (SFX_FKC_Alarm.wav, CC0-1.0).
- "Zzrrrp sizzle" by stephan via Wikimedia Commons (https://upload.wikimedia.org/wikipedia/commons/9/9d/Zzrrrp_sizzle.ogg, Public Domain).
```
