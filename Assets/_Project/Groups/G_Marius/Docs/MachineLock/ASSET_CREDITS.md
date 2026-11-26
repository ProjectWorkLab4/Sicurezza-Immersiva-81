# Asset Credits — Scenario 08 Machine Lock

| Field | Value |
|---|---|
| Document | `ASSET_CREDITS.md` |
| Scenario | 08 — Machine Lock (`SCN_MachineLock`) |
| Owner | G_Marius |
| Branch | `scenario/machine-lock` |
| Asset source list | [`../ASSET_AUDIT_Scenario08_MachineLock.txt`](../ASSET_AUDIT_Scenario08_MachineLock.txt) |
| Design reference | [`GDD_MachineLock.md`](GDD_MachineLock.md) — §17 Asset plan |
| Status | Complete for Issue #35: `PFB_OldIndustrialBuilding` shell + ASTC textures, upgraded Shared FBX props, Polyhaven CC0 PBR textures, 16 Diegetic Plaques, 16 authentic workshop audio clips, 10 VFX systems, damage overlay shader & URP volume profile |

---

## 1. Purpose

Every third-party, shared-library and generated asset used by Scenario 08 gets one
row in this file. The file exists so that the project can prove, at review time,
where each asset came from and under which license it is used.

The scenario may only ship assets that are:

1. copied from or referenced directly in the project's own `Assets/_Project/Shared/` library, or
2. created inside `Assets/_Project/Groups/G_Marius/`, or
3. downloaded under a license that allows redistribution inside a built
   application, with the source, author and license recorded below.

Anything else is not allowed in the build.

## 2. Rules

These rules restate the GDD asset policy (§17) and the repository rules in
[`README.md`](../../../../../../README.md).

- **Never edit pre-existing `Shared/` originals.** Copy textures/materials into
  `Assets/_Project/Groups/G_Marius/` before customizing, or reference Shared FBX
  meshes directly from group prefabs/scene instances without modifying the FBX.
- **Never edit another group's folder.** Assets from `origin/gruppo_ludovica`
  (`SFX_FKC_Siren.wav`, `SFX_FKC_Alarm.wav`) are copied into `G_Marius` and
  recorded below.
- **Root names keep the production convention** (`MESH_HydraulicPress`,
  `MAT_Oil`, `SFX_AlarmSiren`, …) so art swaps never break serialized references.
- **No new packages.** URP decals only if the renderer feature is already enabled.
- **Downscale high-resolution textures** to 1024 or 2048 ASTC 6×6 (or 512/1024 ASTC 4×4
  for signage/plaques) on Android/Quest 3, and omit 8K displacement maps from Quest 3
  materials. Record every override in the *Modifications* column.
- **Player-facing text stays in code, plaques, or localization data**, not in asset names.

## 3. Status vocabulary

| Status | Meaning |
|---|---|
| `SHARED` | Supplied in `Assets/_Project/Shared/` (referenced directly or copied into `G_Marius`). |
| `GROUP` | Authored inside `G_Marius` (shader, material, prefab, texture, config). |
| `BRANCH` | Copied from another SI81 team branch (`origin/gruppo_ludovica`) into `G_Marius`. |
| `DOWNLOADED` | Third-party asset with a verified reachable URL and redistribution license recorded below. |
| `GENERATED` | Procedurally produced in-repo (`MachineLockSceneBuilder` plaques, signs, VFX). |

## 4. Credit tables

### 4.1 Shared library assets copied into `G_Marius` or added in `Shared/`

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `TEX_ConcreteFloorWorn_Albedo` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteFloorWorn_Albedo.jpg` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/concrete_floor_worn_001_diff_4k.jpg` | SI81 shared library | Project-owned (educational use) | Copied and renamed; 2048 default, Android 1024 ASTC 6×6 | P2 |
| 2 | `TEX_ConcreteFloorWorn_Normal` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteFloorWorn_Normal.exr` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/concrete_floor_worn_001_nor_gl_4k.exr` | SI81 shared library | Project-owned (educational use) | Copied and renamed; NormalMap; 2048 default, Android 1024 ASTC 6×6 | P2 |
| 3 | `TEX_ConcreteFloorWorn_Rough` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteFloorWorn_Rough.jpg` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/concrete_floor_worn_001_rough_4k.jpg` | SI81 shared library | Project-owned (educational use) | Copied and renamed; linear; 2048 default, Android 1024 ASTC 6×6 | P2 |
| 4 | `MAT_ConcreteFloorWorn` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_ConcreteFloorWorn.mat` | Material | `SHARED` | `Assets/_Project/Shared/Art/Materials/Material.036.mat` | SI81 shared library | Project-owned (educational use) | Copied from `Material.036`, rewired to group textures, URP Lit | P2 |
| 5 | `MAT_PaintedSteel` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_PaintedSteel.mat` | Material | `SHARED` | `Assets/_Project/Shared/Art/Materials/MAT_OfficePropWhiteMetallic.mat` | SI81 shared library | Project-owned (educational use) | Copied and renamed, URP Lit | P2 |
| 6 | `MAT_PolishedSteel` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_PolishedSteel.mat` | Material | `SHARED` | `Assets/_Project/Shared/Art/Materials/MAT_OfficePropPolishedMetal.mat` | SI81 shared library | Project-owned (educational use) | Copied and renamed, URP Lit | P2 |
| 7 | `PFB_OldIndustrialBuilding` | `Assets/_Project/Shared/Art/Models/PFB_OldIndustrialBuilding.fbx` | Model | `SHARED` | Team Shared asset drop (Issue #35 / ADR-0001) | SI81 shared library | Project-owned (educational use) | Referenced directly in `SCN_MachineLock`; scene instance omits `building top` exterior roof (saving 21,748 tris) and uses scenario URP Lit materials | Issue #35 |
| 8 | `buildingbase.jpg` | `Assets/_Project/Shared/Art/Textures/TEX_OldIndustrialBuilding/buildingbase.jpg` | Texture | `SHARED` | Team Shared asset drop (`TEX_OldIndustrialBuilding`) | SI81 shared library | Project-owned (educational use) | 8192×2148 source; `.meta` clamped to 2048 default, Android 2048 ASTC 6×6 | Issue #35 |
| 9 | `buildingNormalMap.jpg` | `Assets/_Project/Shared/Art/Textures/TEX_OldIndustrialBuilding/buildingNormalMap.jpg` | Texture | `SHARED` | Team Shared asset drop (`TEX_OldIndustrialBuilding`) | SI81 shared library | Project-owned (educational use) | 8192×2148 source; NormalMap, 2048 default, Android 1024 ASTC 6×6 | Issue #35 |
| 10 | `buildingAmbientOcclusionMap.jpg` | `Assets/_Project/Shared/Art/Textures/TEX_OldIndustrialBuilding/buildingAmbientOcclusionMap.jpg` | Texture | `SHARED` | Team Shared asset drop (`TEX_OldIndustrialBuilding`) | SI81 shared library | Project-owned (educational use) | 8192×2148 source; linear, 1024 default, Android 1024 ASTC 6×6 | Issue #35 |
| 11 | `buildingSpecularMap.jpg` | `Assets/_Project/Shared/Art/Textures/TEX_OldIndustrialBuilding/buildingSpecularMap.jpg` | Texture | `SHARED` | Team Shared asset drop (`TEX_OldIndustrialBuilding`) | SI81 shared library | Project-owned (educational use) | 8192×2148 source; linear, 1024 default, Android 1024 ASTC 6×6 | Issue #35 |
| 12 | `TexturesCom_ConcreteBare0348_1_S.jpg` | `Assets/_Project/Shared/Art/Textures/TEX_OldIndustrialBuilding/TexturesCom_ConcreteBare0348_1_S.jpg` | Texture | `SHARED` | Team Shared asset drop (`TEX_OldIndustrialBuilding`) | SI81 shared library | Project-owned (educational use) | 1024×1024; Android 1024 ASTC 6×6 | Issue #35 |
| 13 | `buildingDisplacementMap.jpg` | `Assets/_Project/Shared/Art/Textures/TEX_OldIndustrialBuilding/buildingDisplacementMap.jpg` | Texture | `SHARED` | Team Shared asset drop (`TEX_OldIndustrialBuilding`) | SI81 shared library | Project-owned (educational use) | **Unreferenced** on Quest 3 URP Lit materials (displacement omitted); Android import capped at 256 ASTC 6×6 | Issue #35 |
| 14 | `TEX_Glove_Normal` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_Glove_Normal.png` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/NORMAL_GUANTO.png` | SI81 shared library | Project-owned (educational use) | Copied into `G_Marius`; NormalMap; Android 1024 ASTC 6×6 | Issue #35 |
| 15 | `TEX_AbsorbentMat_Normal` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_AbsorbentMat_Normal.png` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/MAPPA_NORMALE_PANNO.png` | SI81 shared library | Project-owned (educational use) | Copied into `G_Marius`; NormalMap; Android 1024 ASTC 6×6 | Issue #35 |
| 16 | `TEX_AbsorbentSock_Albedo` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_AbsorbentSock_Albedo.png` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/TEXTURE_SAUSAGES.png` | SI81 shared library | Project-owned (educational use) | Copied into `G_Marius`; Android 1024 ASTC 6×6 | Issue #35 |

Shared FBX meshes referenced directly from group prefabs/scene instances without modifying any pre-existing `Shared/` file:

- **Models:** `PFB_OldIndustrialBuilding.fbx`, `GUANTO.fbx`,
  `OCCHIALI_PROTETTIVI.fbx`, `MESH_Magazzino_Cono.fbx`, `MESH_PANNO_ASSORBENTE_TEAM.fbx`,
  `MESH_SALSICCIOTTO_ONEDRIVE.fbx`, `Estintore.fbx`, `MESH_Folgorazione_CartelloEstintore.fbx`,
  `Quadro Elettrico.fbx`, `Manopola.fbx`, `MESH_PULSANTI_TEAM.fbx`,
  `MESH_RilevatoreDiGas.fbx`, `MESH_SCATOLA_APRIBILE_ROSSA_TEAM.fbx`,
  `MESH_Magazzino_Barriera.fbx`, `MESH_Magazzino_Scaffale.fbx`,
  `MESH_Folgorazione_Barile.fbx`, `Pallet.fbx`, `MESH_VENTOLA_TEAM.fbx`,
  `MESH_Magazzino_LuceDaSoffitto.fbx`, and `Chemical_Alert_Room/MESH_PORTA_CAMION_TEAM.fbx`.

### 4.2 Assets authored in `G_Marius`

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `SH_MachineLockDamageOverlay` | `Assets/_Project/Groups/G_Marius/Code/Shaders/SH_MachineLockDamageOverlay.shader` | Shader | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP transparent unlit VR-stereo-safe (`UNITY_VERTEX_INPUT_INSTANCE_ID` / `UNITY_VERTEX_OUTPUT_STEREO`) overlay shader (`ZTest Always`, `Queue=Overlay`) for peripheral red damage vignette & respawn blink (ADR-0003) | Issue #35 |
| 2 | `MAT_DamageOverlay` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_DamageOverlay.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | Uses `SH_MachineLockDamageOverlay`; driven at runtime by `MachineLockDamageFeedback` | Issue #35 |
| 3 | `MAT_IndustrialBuilding_Wall` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_IndustrialBuilding_Wall.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit bound to `buildingbase`, `buildingNormalMap`, `buildingAmbientOcclusionMap` (no displacement map) | Issue #35 |
| 4 | `MAT_IndustrialBuilding_Floor` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_IndustrialBuilding_Floor.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit bound to `TexturesCom_ConcreteBare0348_1_S.jpg` | Issue #35 |
| 5 | `MAT_IndustrialBuilding_Window` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_IndustrialBuilding_Window.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit translucent factory daylight glass | Issue #35 |
| 6 | `MAT_PressSteel_Green` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_PressSteel_Green.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit bound to Polyhaven CC0 `TEX_GreenMetalRust_*` (tiling 2×2) for hydraulic press body | Issue #35 |
| 7 | `MAT_ConcreteWall_PBR` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_ConcreteWall_PBR.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit bound to Polyhaven CC0 `TEX_ConcreteWall009_*` for bay concrete surfaces | Issue #35 |
| 8 | `MAT_WallConcrete`, `MAT_CeilingConcrete`, `MAT_DarkSlate`, `MAT_CyanAccent` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_*.mat` | Materials | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit architectural and electrical Sub-Station accents | P2 / Issue #35 |
| 9 | `MAT_Oil`, `MAT_EmergencyStop_Red`, `MAT_EmergencyStop_Yellow`, `MAT_HMI_Screen`, `MAT_HMI_Off` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_*.mat` | Materials | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit hazard, emergency control and press display materials | P2 / P8 |
| 10 | `MAT_FaceShield_Visor`, `MAT_Apron_Rubber`, `MAT_HeatGlove`, `MAT_SafetyBoot_Leather`, `MAT_SafetyBoot_Sole`, `MAT_Latex`, `MAT_SurgicalMask_Blue` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_*.mat` | Materials | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit PPE materials (ADR-0004: 4 correct + 2 distractors); `MAT_HeatGlove` references `NORMAL_GUANTO.png` | P2 / Issue #35 |
| 11 | `MAT_AbsorbentMat`, `MAT_AbsorbentSock` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_*.mat` | Materials | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit spill containment materials referencing `MAPPA_NORMALE_PANNO.png` and `TEXTURE_SAUSAGES.png` | Issue #35 |
| 12 | `MAT_FloorSafetyMarking_Yellow`, `MAT_BaselineZone`, `MAT_GhostSnap`, `MAT_BeaconRed`, `MAT_ExitBeacon_Idle`, `MAT_ExitBeacon_Ready`, `MAT_LedRed`, `MAT_LedGreen`, `MAT_LedOff`, `MAT_LightFixture`, `MAT_ParticleSoft` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_*.mat` | Materials | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | Guided Wayfinding, LEDs, emissive Exit Beacon, and soft transparent URP Particles/Unlit material | P2 / P8 / Issue #35 |
| 13 | `TEX_Sign_*` & `MAT_Sign_*` (5 wall signs) | `Assets/_Project/Groups/G_Marius/Art/{Textures,Materials}/MachineLock/{TEX,MAT}_Sign_*.{png,mat}` | Texture + Material | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | 1024×512 wall signs (`Stampaggio`, `QuadroGenerale`, `Dpi`, `KitAntispandimento`, `Uscita`), Android 1024 ASTC 4×4 | P2 |
| 14 | `TEX_Plaque_*` & `MAT_Plaque_*` (21 Diegetic Plaques) | `Assets/_Project/Groups/G_Marius/Art/{Textures,Materials}/MachineLock/{TEX,MAT}_Plaque_*.{png,mat}` | Texture + Material | `GENERATED` | created in-repo by `MachineLockSceneBuilder.Plaques.cs` | G_Marius | Project-owned (educational use) | 512×256 multi-line industrial Diegetic Plaques (3 electrical Sub-Stations, E-Stop, Alarm, Valve, Spill Kit, Extinguisher, Exit Header, 6 PPE bays, plus 6 per-item `<Nome DPI> INDOSSATO` shelf badges), Android 512 ASTC 4×4 (ADR-0002) | Issue #35 |
| 15 | `SO_MachineLockDamageProfile` | `Assets/_Project/Groups/G_Marius/Level/Data/SO_MachineLockDamageProfile.asset` | Volume profile | `GROUP` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | URP `VolumeProfile` with red `Vignette` and `ColorAdjustments` sub-assets | P8 |

### 4.3 Scenario prefabs (12 production roots)

All 12 prefabs keep their production root name and combine Shared FBX meshes, Polyhaven CC0 PBR materials, and flush Diegetic Plaques.

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `MESH_HydraulicPress` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_HydraulicPress.prefab` | Prefab | `GROUP` | Multi-part press (`MAT_PressSteel_Green`, `MAT_PolishedSteel`) + `MESH_PULSANTI_TEAM.fbx` | G_Marius | Project-owned (educational use) | Painted green rusted steel frame, twin cylinders, moving `MESH_HydraulicPress_Ram`, safety cage, HMI fault display, thermometer, leak manifold | P2 / Issue #35 |
| 2 | `MESH_EStopPedestal` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_EStopPedestal.prefab` | Prefab | `GROUP` | `MESH_PULSANTI_TEAM.fbx` + pedestal + `MAT_Plaque_EStop` | G_Marius | Project-owned (educational use) | Industrial pedestal with yellow backplate, red E-stop mushroom, green try-start button, flush Diegetic Plaque | P2 / Issue #35 |
| 3 | `MESH_AlarmButton` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_AlarmButton.prefab` | Prefab | `GROUP` | Wall housing + beacon + `MAT_Plaque_Alarm` | G_Marius | Project-owned (educational use) | Red break-glass style button, rotating emissive beacon + realtime point light, flush Diegetic Plaque | P2 / Issue #35 |
| 4 | `MESH_ElectricalPanel` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_ElectricalPanel.prefab` | Prefab | `GROUP` | `Quadro Elettrico.fbx`, `Manopola.fbx`, `MESH_PULSANTI_TEAM.fbx`, `MESH_RilevatoreDiGas.fbx` + plaques | G_Marius | Project-owned (educational use) | 3 physically separated (≥ 0.88 m), color-coded Sub-Stations: Main Disconnect (red/slate), Lockout Station (yellow/black), Voltage Tester (cyan/blue) with flush Diegetic Plaques | P2 / Issue #35 |
| 5 | `MESH_PpeCabinet` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_PpeCabinet.prefab` | Prefab | `GROUP` | Wide sliding-door locker + `GUANTO.fbx`, `OCCHIALI_PROTETTIVI.fbx`, `MESH_SafetyBoots`, pleated surgical mask + 7 bay plaques | G_Marius | Project-owned (educational use) | 1.80×2.05×0.44 m non-blocking sliding doors (colliders disable on `Open()`), 6 distinct bays (ADR-0004: Heat Gloves, Apron, Face Shield, Safety Boots S3 + Latex Gloves & low-poly pleated Surgical Mask distractors) + `EquippedBadge` | P2 / Issue #35 |
| 6 | `MESH_SpillKit` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_SpillKit.prefab` | Prefab | `GROUP` | `MESH_SCATOLA_APRIBILE_ROSSA_TEAM.fbx` + nested cones/mats/socks + `MAT_Plaque_SpillKit` | G_Marius | Project-owned (educational use) | Red industrial spill-kit bin with 4 cones, 2 mats, 2 socks and Grip Diegetic Plaque | P2 / Issue #35 |
| 7 | `MESH_WaterExtinguisher` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_WaterExtinguisher.prefab` | Prefab | `GROUP` | `Estintore.fbx` + `MESH_Folgorazione_CartelloEstintore.fbx` + `MAT_Plaque_Extinguisher` | G_Marius | Project-owned (educational use) | Wall-mounted water extinguisher distractor with shared extinguisher sign and Class-A warning plaque | P2 / Issue #35 |
| 8 | `MESH_Cone` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_Cone.prefab` | Prefab | `GROUP` | `MESH_Magazzino_Cono.fbx` | G_Marius | Project-owned (educational use) | 0.5 m cone; ignores `CharacterController` collisions on grab/place without losing raycasts | P2 / Issue #35 |
| 9 | `MESH_AbsorbentMat` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_AbsorbentMat.prefab` | Prefab | `GROUP` | `MESH_PANNO_ASSORBENTE_TEAM.fbx` + `MAT_AbsorbentMat` | G_Marius | Project-owned (educational use) | 1.0 × 0.02 × 0.8 m mat with `MAPPA_NORMALE_PANNO.png` normal map and player-body no-clip safety | P2 / Issue #35 |
| 10 | `MESH_AbsorbentSock` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_AbsorbentSock.prefab` | Prefab | `GROUP` | `MESH_SALSICCIOTTO_ONEDRIVE.fbx` + `MAT_AbsorbentSock` | G_Marius | Project-owned (educational use) | Upgraded from primitive cylinder to Shared FBX absorbent sock with `TEXTURE_SAUSAGES.png` and player-body no-clip safety | P2 / Issue #35 |
| 11 | `MESH_Valve` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_Valve.prefab` | Prefab | `GROUP` | Pipe collar, bright red spoked wheel, pulsing cue ring + `MAT_Plaque_Valve` | G_Marius | Project-owned (educational use) | Unmissable bright red valve wheel on east oil pipe outside puddle with pulsing halo and Diegetic Plaque | P2 / Issue #35 |
| 12 | `MESH_IndustrialDoor_RollUp` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_IndustrialDoor_RollUp.prefab` | Prefab | `GROUP` | `MESH_PORTA_CAMION_TEAM.fbx` + `MAT_Plaque_ExitHeader` | G_Marius | Project-owned (educational use) | Wide 2.0 × 2.5 m unobstructed exit portal with overhead `MESH_ExitBeacon` | P2 / Issue #35 |

### 4.4 Downloaded third-party textures

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `TEX_GreenMetalRust_Albedo` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_GreenMetalRust_Albedo.jpg` | Texture | `DOWNLOADED` | `https://polyhaven.com/a/green_metal_rust` | Rob Tuytel (Poly Haven) | `CC0-1.0` | 1K download; Android override 1024 ASTC 6×6 | Issue #35 |
| 2 | `TEX_GreenMetalRust_Normal` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_GreenMetalRust_Normal.exr` | Texture | `DOWNLOADED` | `https://polyhaven.com/a/green_metal_rust` | Rob Tuytel (Poly Haven) | `CC0-1.0` | 1K OpenGL normal map; NormalMap import, Android 1024 ASTC 6×6 | Issue #35 |
| 3 | `TEX_GreenMetalRust_Rough` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_GreenMetalRust_Rough.jpg` | Texture | `DOWNLOADED` | `https://polyhaven.com/a/green_metal_rust` | Rob Tuytel (Poly Haven) | `CC0-1.0` | 1K roughness map; linear, Android 1024 ASTC 6×6 | Issue #35 |
| 4 | `TEX_ConcreteWall009_Albedo` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteWall009_Albedo.jpg` | Texture | `DOWNLOADED` | `https://polyhaven.com/a/concrete_wall_009` | Charlotte Baglioni (Poly Haven) | `CC0-1.0` | 1K download; Android override 1024 ASTC 6×6 | Issue #35 |
| 5 | `TEX_ConcreteWall009_Normal` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteWall009_Normal.exr` | Texture | `DOWNLOADED` | `https://polyhaven.com/a/concrete_wall_009` | Charlotte Baglioni (Poly Haven) | `CC0-1.0` | 1K OpenGL normal map; NormalMap import, Android 1024 ASTC 6×6 | Issue #35 |
| 6 | `TEX_ConcreteWall009_Rough` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteWall009_Rough.jpg` | Texture | `DOWNLOADED` | `https://polyhaven.com/a/concrete_wall_009` | Charlotte Baglioni (Poly Haven) | `CC0-1.0` | 1K roughness map; linear, Android 1024 ASTC 6×6 | Issue #35 |

### 4.5 Audio (16 authentic clips)

All 16 clips keep the exact `SFX_` production names and are formatted as 44.1 kHz mono 16-bit PCM WAV (imported with Android Vorbis/ADPCM compression). Replaced the P8 synthesized placeholders in Issue #35 with Team Ludovica CC0 assets, Wyzowl SFX Kit CC0 foley/mechanical recordings, and Wikimedia Commons industrial recordings.

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `SFX_PressLoop` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_PressLoop.wav` | Audio (loop) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/c/cf/WWS_Pressing.ogg` | Work With Sounds / Technical Museum of Slovenia | `CC-BY-4.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, trimmed to 6.00 s seamless crossfaded loop, normalized | Issue #35 |
| 2 | `SFX_PressStop` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_PressStop.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/digital/digital-power-down.wav` + `sounds/booms-and-bass/bass-rumble-medium.wav`) | Wyzowl | `CC0-1.0` | Layered power-down and low mechanical rumble into 44.1 kHz mono WAV, normalized | Issue #35 |
| 3 | `SFX_EStopClack` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_EStopClack.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/mechanical/mechanical-latch-heavy.wav`) | Wyzowl | `CC0-1.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, trimmed/normalized | Issue #35 |
| 4 | `SFX_AlarmButton` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_AlarmButton.wav` | Audio (one-shot) | `BRANCH` | `origin/gruppo_ludovica`: `Assets/_Project/Groups/G_Ludovica/Audio/Sound/SFX_FKC_Alarm.wav` (origin: Wyzowl SFX Kit `sounds/time/time-alarm.wav`) | Team Ludovica / Wyzowl | `CC0-1.0` | Copied verbatim (2.31 s) and renamed to `SFX_AlarmButton.wav` | Issue #35 |
| 5 | `SFX_AlarmSiren` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_AlarmSiren.wav` | Audio (loop) | `BRANCH` | `origin/gruppo_ludovica`: `Assets/_Project/Groups/G_Ludovica/Audio/Sound/SFX_FKC_Siren.wav` | Team Ludovica | `CC0-1.0` | Copied verbatim (3.00 s loop) and renamed to `SFX_AlarmSiren.wav` | Issue #35 |
| 6 | `SFX_BreakerOff` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_BreakerOff.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/mechanical/mechanical-switch-heavy.wav`) | Wyzowl | `CC0-1.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 7 | `SFX_Lockout` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_Lockout.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/mechanical/mechanical-lock-heavy.wav`) | Wyzowl | `CC0-1.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 8 | `SFX_OilLeak` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_OilLeak.wav` | Audio (loop) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/b/b1/WWS_Airpumpsteamlocomotive.ogg` | Work With Sounds / Konrad Gutkowski | `CC-BY-4.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, trimmed to 4.00 s seamless crossfaded pressurized hiss loop, normalized | Issue #35 |
| 9 | `SFX_ValveTurn` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_ValveTurn.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/mechanical/mechanical-ratchet-heavy.wav`) | Wyzowl | `CC0-1.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 10 | `SFX_TesterBeep` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_TesterBeep.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/digital/digital-blip-clear.wav`) | Wyzowl | `CC0-1.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 11 | `SFX_EquipPpe` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_EquipPpe.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/foley/foley-fabric-slide.wav`) | Wyzowl | `CC0-1.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 12 | `SFX_Snap` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_Snap.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/interface/click-snap-medium.wav`) | Wyzowl | `CC0-1.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 13 | `SFX_SteamBurst` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_SteamBurst.wav` | Audio (one-shot) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/2/22/WWS_Steamlocomotive.ogg` | Work With Sounds / Konrad Gutkowski | `CC-BY-4.0` | Extracted 1.80 s pressurized steam burst to 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 14 | `SFX_Shock` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_Shock.wav` | Audio (one-shot) | `DOWNLOADED` | `https://github.com/wyzowl/sfx-kit` (`sounds/digital/digital-static.wav` + `sounds/digital/digital-glitch-clear.wav` + `sounds/impacts/impact-heavy-hard.wav`) | Wyzowl | `CC0-1.0` | Layered electrical arc burst + impact into 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 15 | `SFX_Burn` | `Assets/_Project/Groups/G_Marius/Audio/Sound/MachineLock/SFX_Burn.wav` | Audio (one-shot) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/9/9d/Zzrrrp_sizzle.ogg` | stephan (Wikimedia Commons) | `Public Domain` | Trimmed to 1.50 s hot-oil sizzle burst, converted to 44.1 kHz mono 16-bit PCM WAV, normalized | Issue #35 |
| 16 | `SFX_AmbientFactory` | `Assets/_Project/Groups/G_Marius/Audio/Music/MachineLock/SFX_AmbientFactory.wav` | Audio (loop) | `DOWNLOADED` | `https://upload.wikimedia.org/wikipedia/commons/d/d9/WWS_HydropowerStation.ogg` | Work With Sounds / Technical Museum of Slovenia | `CC-BY-4.0` | Converted to 44.1 kHz mono 16-bit PCM WAV, trimmed to 8.00 s seamless crossfaded industrial machine-hall hum, normalized | Issue #35 |

### 4.6 Visual effects and lighting

Every VFX below is a `ParticleSystem` or scene component created in-repo by
`MachineLockSceneBuilder`, rendered with `MAT_ParticleSoft` so no system falls back to a
pink shader on Quest 3. Particle budget: ≤ 200 particles per system and ≤ 4 play-on-awake systems.

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `VFX_OilJet` | `SCN_MachineLock` — `HAZARD_Oil/VFX_OilJet` | VFX | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Dark oil cone, max 80 particles, plays on awake, plus jet stream & `SFX_OilLeak`; deactivated when Valve closes | P8 |
| 2 | `VFX_OilPuddle` | `SCN_MachineLock` — `HAZARD_Oil/VFX_OilPuddle` | VFX / mesh | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Four glossy `MAT_Oil` quadrant meshes scaled by `PATTERN_PuddleA/B/C`; each absorbent hides one quadrant | P8 |
| 3 | `VFX_Steam` | `SCN_MachineLock` — `HAZARD_Oil/VFX_Steam` | VFX | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Soft white steam wisps over hot oil, max 60 particles, plays on awake, burst on water extinguisher | P8 |
| 4 | `VFX_Smoke` | `SCN_MachineLock` — `HAZARD_Oil/VFX_Smoke` | VFX | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Dark grey smoke above press, max 120 particles; ramps at inaction stage 1 (90 s) & stage 2 (150 s), cleared on E-stop | P8 |
| 5 | `VFX_ElectricalArc` | `SCN_MachineLock` — `MESH_ElectricalPanel/VFX_ElectricalArc` | VFX | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Sparks at Voltage Tester Sub-Station, max 40 particles, not auto-playing; pulsed 0.35 s on LIVE reading | P8 |
| 6 | `VFX_AlarmBeacon` | `SCN_MachineLock` — `MESH_AlarmButton/VFX_AlarmBeacon` | VFX / light | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Rotating emissive dome (`MAT_BeaconRed`) + realtime red point light (range 8 m, no shadows, disabled at start) | P8 |
| 7 | `VFX_DisplayFault` | `SCN_MachineLock` — `MESH_HydraulicPress/MESH_PressDisplay/VFX_DisplayFault` | VFX marker | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Drives press HMI blinking fault emission (`MAT_HMI_Screen` → `MAT_HMI_Off` when main power is cut) | P8 |
| 8 | `VFX_WaterSpray` | `SCN_MachineLock` — `MESH_WaterExtinguisher/VFX_WaterSpray` | VFX | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Water cone, max 100 particles, not auto-playing, one-shot burst on water extinguisher misuse | P8 |
| 9 | `VFX_BaselineMarker` & `VFX_SnapGhost` | `SCN_MachineLock` — `ZONE_Coverage1..4` and `ZONE_ConePoint1..4` | VFX / mesh | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Proactive Guided baseline outline rings (`MAT_BaselineZone`, shown once full PPE is worn) + bright green snap ghosts (`MAT_GhostSnap`, shown while holding matching item); both hide on occupy | P8 / Issue #35 |
| 10 | `VFX_DamageVignette` + `MAT_DamageOverlay` | `SCN_MachineLock` — `MNG_Scenario/VFX_DamageVignette` | Volume + Overlay | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Global URP `Volume` (`SO_MachineLockDamageProfile`) plus scenario-owned head-aligned VR-stereo-safe peripheral red vignette & respawn fade overlay (`SH_MachineLockDamageOverlay`) when camera post-processing is disabled (ADR-0003) | P8 / Issue #35 |
| 11 | `MESH_ExitBeacon` | `SCN_MachineLock` — `ENV_Room/MESH_Door/MESH_ExitBeacon` | Wayfinding | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Overhead exit status light bar switching from dim (`MAT_ExitBeacon_Idle`) to bright glowing green (`MAT_ExitBeacon_Ready`) when `TaskEStop && TaskContainment` | Issue #35 |
| 12 | `MNG_Light` | `SCN_MachineLock` — root `MNG_Light` | Light | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Single directional realtime fill (no shadows) plus bright industrial ambient | P2 / Issue #35 |
| 13 | `MESH_CeilingLight01..04` | `SCN_MachineLock` — `ENV_Room/MESH_Signs` | Light | `GENERATED` | `MESH_Magazzino_LuceDaSoffitto.fbx` + baked point lights | G_Marius | Project-owned (educational use) | Four baked ceiling industrial light fixtures (no realtime cost) | P2 / Issue #35 |
| 14 | `LIGHT_DamageFlash` | `SCN_MachineLock` — `MNG_Scenario/LIGHT_DamageFlash` | Light | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | Red point flash pulsed alongside the damage overlay on Critical Events; intensity 0, no shadows, disabled while idle | P8 / Issue #35 |

## 5. Attribution text for the build

The following attribution notice covers all `CC-BY-4.0` assets shipped in Scenario 08 (CC0-1.0 and Public Domain assets require no attribution legally, but are credited for completeness):

```text
Scenario 08 (Machine Lock) includes the following Creative Commons Attribution 4.0 (CC BY 4.0) audio recordings from the Work With Sounds project via Wikimedia Commons (https://creativecommons.org/licenses/by/4.0/):
- "WWS Pressing" by Work With Sounds / Technical Museum of Slovenia (https://upload.wikimedia.org/wikipedia/commons/c/cf/WWS_Pressing.ogg) — edited into a seamless mono loop (SFX_PressLoop.wav).
- "WWS HydropowerStation" by Work With Sounds / Technical Museum of Slovenia (https://upload.wikimedia.org/wikipedia/commons/d/d9/WWS_HydropowerStation.ogg) — edited into a seamless mono loop (SFX_AmbientFactory.wav).
- "WWS Steamlocomotive" by Work With Sounds / Konrad Gutkowski (https://upload.wikimedia.org/wikipedia/commons/2/22/WWS_Steamlocomotive.ogg) — trimmed burst (SFX_SteamBurst.wav).
- "WWS Airpumpsteamlocomotive" by Work With Sounds / Konrad Gutkowski (https://upload.wikimedia.org/wikipedia/commons/b/b1/WWS_Airpumpsteamlocomotive.ogg) — edited into a seamless mono loop (SFX_OilLeak.wav).

Additional CC0-1.0 / Public Domain assets:
- PBR textures "Green Metal Rust" (Rob Tuytel) and "Concrete Wall 009" (Charlotte Baglioni) from Poly Haven (https://polyhaven.com/, CC0-1.0).
- Mechanical, digital, and foley SFX from Wyzowl SFX Kit (https://github.com/wyzowl/sfx-kit, CC0-1.0) and Team Ludovica (SFX_FKC_Siren.wav, SFX_FKC_Alarm.wav, CC0-1.0).
- "Zzrrrp sizzle" by stephan via Wikimedia Commons (https://upload.wikimedia.org/wikipedia/commons/9/9d/Zzrrrp_sizzle.ogg, Public Domain).
```
