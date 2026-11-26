# Asset Credits — Scenario 08 Machine Lock

| Field | Value |
|---|---|
| Document | `ASSET_CREDITS.md` |
| Scenario | 08 — Machine Lock (`SCN_MachineLock`) |
| Owner | G_Marius |
| Branch | `scenario/machine-lock` |
| Asset source list | [`../ASSET_AUDIT_Scenario08_MachineLock.txt`](../ASSET_AUDIT_Scenario08_MachineLock.txt) |
| Design reference | [`GDD_MachineLock.md`](GDD_MachineLock.md) — §17 Asset plan |
| Status | Populated in P2 (blockout and placeholders); audio and VFX rows follow in P8 |

---

## 1. Purpose

Every third-party, shared-library and generated asset used by Scenario 08 gets one
row in this file. The file exists so that the project can prove, at review time,
where each asset came from and under which license it is used.

The scenario may only ship assets that are:

1. copied from the project's own `Assets/_Project/Shared/` library, or
2. created inside `Assets/_Project/Groups/G_Marius/`, or
3. downloaded under a license that allows redistribution inside a built
   application, with the source, author and license recorded below.

Anything else is not allowed in the build.

## 2. Rules

These rules restate the GDD asset policy (§17) and the repository rules in
[`README.md`](../../../../../../README.md).

- **Never edit `Shared/` originals.** Copy the asset into
  `Assets/_Project/Groups/G_Marius/` and record the copy here.
- **Never edit another group's folder.** Assets from `origin/group_tiziano`,
  `origin/gruppo_ludovica`, `origin/group_simone` and `origin/group_marius` may be
  read and copied, never modified.
- **Placeholder roots keep the production name** (`MESH_HydraulicPress`,
  `MAT_Oil`, `SFX_AlarmSiren`, …) so final art can replace the visual children
  without breaking references. Record the placeholder here as well, so nobody
  mistakes it for final content.
- **No new packages.** URP decals only if the renderer feature is already
  enabled.
- **Downscale 4K textures** to 1024 or 2048 ASTC before they are used on Quest.
  Record the downscale in the *Modifications* column.
- **Player-facing text stays in code or localization data**, not in asset names.

## 3. Status vocabulary

| Status | Meaning |
|---|---|
| `SHARED` | Copied from `Assets/_Project/Shared/`; only the group copy is edited. |
| `GROUP` | Authored inside `G_Marius` (FBX, material, prefab, texture, audio). |
| `PLACEHOLDER` | Graybox or primitive stand-in created for the blockout. |
| `DOWNLOADED` | Third-party asset with an explicit license recorded below. |
| `GENERATED` | Procedurally produced in-repo (VFX graph, gradient texture, synthesized audio). |
| `PENDING` | Asset not yet acquired. Must not appear in a release build. |

## 4. Credit tables

Columns are identical in every table so the rows can be merged into one manifest
later.

- **Production asset** — the name used in the project (`MESH_`, `MAT_`, `TEX_`,
  `SFX_`, `VFX_` prefix per the naming convention).
- **Unity path** — path relative to the repository root.
- **Type** — model, material, texture, prefab, audio, VFX, font.
- **Status** — one of the values in §3.
- **Source** — `Shared` path, `origin/<branch>` path, download URL, or
  `created in-repo`.
- **Author / publisher** — person, studio or `G_Marius` for in-group work.
- **License** — SPDX identifier or the license name exactly as published.
- **Modifications** — renaming, recolor, downscale, material rebuild, etc.
- **Phase** — the implementation phase (P2–P8) that introduced the asset.

### 4.1 Shared library assets copied into `G_Marius`

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `TEX_ConcreteFloorWorn_Albedo` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteFloorWorn_Albedo.jpg` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/concrete_floor_worn_001_diff_4k.jpg` | SI81 shared library | Project-owned (educational use) | Copied and renamed; 2048 default, Android override 1024 ASTC 6×6 | P2 |
| 2 | `TEX_ConcreteFloorWorn_Normal` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteFloorWorn_Normal.exr` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/concrete_floor_worn_001_nor_gl_4k.exr` | SI81 shared library | Project-owned (educational use) | Copied and renamed; NormalMap; 2048 default, Android override 1024 ASTC 6×6 | P2 |
| 3 | `TEX_ConcreteFloorWorn_Rough` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_ConcreteFloorWorn_Rough.jpg` | Texture | `SHARED` | `Assets/_Project/Shared/Art/Textures/concrete_floor_worn_001_rough_4k.jpg` | SI81 shared library | Project-owned (educational use) | Copied and renamed; linear; 2048 default, Android override 1024 ASTC 6×6 | P2 |
| 4 | `MAT_ConcreteFloorWorn` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_ConcreteFloorWorn.mat` | Material | `SHARED` | `Assets/_Project/Shared/Art/Materials/Material.036.mat` | SI81 shared library | Project-owned (educational use) | Copied from `Material.036`, rewired to the three group textures, tiling 6×4, URP Lit | P2 |
| 5 | `MAT_PaintedSteel` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_PaintedSteel.mat` | Material | `SHARED` | `Assets/_Project/Shared/Art/Materials/MAT_OfficePropWhiteMetallic.mat` | SI81 shared library | Project-owned (educational use) | Copied and renamed, URP Lit unchanged | P2 |
| 6 | `MAT_PolishedSteel` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_PolishedSteel.mat` | Material | `SHARED` | `Assets/_Project/Shared/Art/Materials/MAT_OfficePropPolishedMetal.mat` | SI81 shared library | Project-owned (educational use) | Copied and renamed, URP Lit unchanged | P2 |

Shared FBX meshes are referenced directly from the prefabs and are **not** copied or edited:
`GUANTO.fbx`, `MASCHERINA.fbx`, `OCCHIALI_PROTETTIVI.fbx`,
`MESH_Magazzino_Cono.fbx`, `MESH_PANNO_ASSORBENTE_TEAM.fbx`, `Estintore.fbx`,
`Quadro Elettrico.fbx`, `Manopola.fbx` and
`Chemical_Alert_Room/MESH_PORTA_CAMION_TEAM.fbx`.

### 4.2 Assets authored in `G_Marius`

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `MAT_WallConcrete` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_WallConcrete.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit gray concrete | P2 |
| 2 | `MAT_CeilingConcrete` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_CeilingConcrete.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit dark concrete | P2 |
| 3 | `MAT_Oil` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Oil.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit (0.05, 0.03, 0.02), metallic 0, smoothness 0.9 | P2 |
| 4 | `MAT_EmergencyStop_Red` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_EmergencyStop_Red.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit red | P2 |
| 5 | `MAT_EmergencyStop_Yellow` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_EmergencyStop_Yellow.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit yellow | P2 |
| 6 | `MAT_HMI_Screen` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_HMI_Screen.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit emissive fault screen | P2 |
| 7 | `MAT_FaceShield_Visor` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_FaceShield_Visor.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit transparent visor (alpha 0.18) | P2 |
| 8 | `MAT_Apron_Rubber` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Apron_Rubber.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit dark rubber | P2 |
| 9 | `MAT_HeatGlove` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_HeatGlove.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit heat-resistant fabric | P2 |
| 10 | `MAT_Latex` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Latex.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit white/cream, smoothness 0.85 | P2 |
| 11 | `MAT_Respirator` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Respirator.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit off-white | P2 |
| 12 | `MAT_FloorSafetyMarking_Yellow` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_FloorSafetyMarking_Yellow.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit yellow with faint emission; used on thin quads at y = 0.01 | P2 |
| 13 | `MAT_BeaconRed` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_BeaconRed.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit emissive red dome | P2 |
| 14 | `MAT_LedRed` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_LedRed.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit emissive red LED | P2 |
| 15 | `MAT_LedGreen` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_LedGreen.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit emissive green LED | P2 |
| 16 | `MAT_GhostSnap` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_GhostSnap.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit transparent green highlight | P2 |
| 17 | `MAT_LightFixture` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_LightFixture.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit emissive ceiling diffuser | P2 |
| 18 | `MAT_Sign_Stampaggio` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Sign_Stampaggio.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit with emission map for the sign texture | P2 |
| 19 | `MAT_Sign_QuadroGenerale` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Sign_QuadroGenerale.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit with emission map for the sign texture | P2 |
| 20 | `MAT_Sign_Dpi` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Sign_Dpi.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit with emission map for the sign texture | P2 |
| 21 | `MAT_Sign_KitAntispandimento` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Sign_KitAntispandimento.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit with emission map for the sign texture | P2 |
| 22 | `MAT_Sign_Uscita` | `Assets/_Project/Groups/G_Marius/Art/Materials/MachineLock/MAT_Sign_Uscita.mat` | Material | `GROUP` | created in-repo | G_Marius | Project-owned (educational use) | URP Lit with emission map for the sign texture | P2 |
| 23 | `TEX_Sign_Stampaggio` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_Sign_Stampaggio.png` | Texture | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | 1024×512 bitmap text, Android 1024 ASTC 4×4 | P2 |
| 24 | `TEX_Sign_QuadroGenerale` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_Sign_QuadroGenerale.png` | Texture | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | 1024×512 bitmap text, Android 1024 ASTC 4×4 | P2 |
| 25 | `TEX_Sign_Dpi` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_Sign_Dpi.png` | Texture | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | 1024×512 bitmap text, Android 1024 ASTC 4×4 | P2 |
| 26 | `TEX_Sign_KitAntispandimento` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_Sign_KitAntispandimento.png` | Texture | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | 1024×512 bitmap text, Android 1024 ASTC 4×4 | P2 |
| 27 | `TEX_Sign_Uscita` | `Assets/_Project/Groups/G_Marius/Art/Textures/MachineLock/TEX_Sign_Uscita.png` | Texture | `GENERATED` | created in-repo by `MachineLockSceneBuilder` | G_Marius | Project-owned (educational use) | 1024×512 bitmap text, Android 1024 ASTC 4×4 | P2 |

### 4.3 Placeholders and grayboxes

All prefabs below keep the final production name; their visual children can be replaced
later without renaming the roots. They are built by
`MachineLockSceneBuilder.BuildFullScene` from primitives and the Shared FBX meshes.

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `MESH_HydraulicPress` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_HydraulicPress.prefab` | Prefab | `PLACEHOLDER` | primitives + Shared materials | G_Marius | Project-owned (educational use) | Graybox frame, crown, columns, moving `MESH_HydraulicPress_Ram`, guard, control panel, display, thermometer, leak pipe | P2 |
| 2 | `MESH_EStopPedestal` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_EStopPedestal.prefab` | Prefab | `PLACEHOLDER` | primitives | G_Marius | Project-owned (educational use) | Pedestal, yellow backplate, red mushroom, test-start button | P2 |
| 3 | `MESH_AlarmButton` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_AlarmButton.prefab` | Prefab | `PLACEHOLDER` | primitives | G_Marius | Project-owned (educational use) | Wall box, red button, red beacon dome with a realtime point light (disabled at start) | P2 |
| 4 | `MESH_ElectricalPanel` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_ElectricalPanel.prefab` | Prefab | `PLACEHOLDER` | `Quadro Elettrico.fbx`, `Manopola.fbx` + primitives | G_Marius | Project-owned (educational use) | Cabinet, rotary disconnect, lockout point with red LED, voltage tester with red/green LEDs | P2 |
| 5 | `MESH_PpeCabinet` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_PpeCabinet.prefab` | Prefab | `PLACEHOLDER` | primitives + `GUANTO.fbx`, `MASCHERINA.fbx`, `OCCHIALI_PROTETTIVI.fbx` | G_Marius | Project-owned (educational use) | Cabinet, hinged door, shelves and the six PPE items | P2 |
| 6 | `MESH_SpillKit` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_SpillKit.prefab` | Prefab | `PLACEHOLDER` | primitives + nested `MESH_Cone`, `MESH_AbsorbentMat`, `MESH_AbsorbentSock` | G_Marius | Project-owned (educational use) | Cart/bin with four cones, two mats and two socks | P2 |
| 7 | `MESH_WaterExtinguisher` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_WaterExtinguisher.prefab` | Prefab | `PLACEHOLDER` | `Estintore.fbx` + primitives | G_Marius | Project-owned (educational use) | 0.6 m body, wall bracket, capsule collider | P2 |
| 8 | `MESH_Cone` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_Cone.prefab` | Prefab | `PLACEHOLDER` | `MESH_Magazzino_Cono.fbx` | G_Marius | Project-owned (educational use) | 0.5 m high, 0.3 m base, box collider | P2 |
| 9 | `MESH_AbsorbentMat` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_AbsorbentMat.prefab` | Prefab | `PLACEHOLDER` | `MESH_PANNO_ASSORBENTE_TEAM.fbx` | G_Marius | Project-owned (educational use) | 1.0 × 0.02 × 0.8 m, box collider | P2 |
| 10 | `MESH_AbsorbentSock` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_AbsorbentSock.prefab` | Prefab | `PLACEHOLDER` | primitive cylinder | G_Marius | Project-owned (educational use) | 1.2 × 0.15 × 0.15 m, capsule collider | P2 |
| 11 | `MESH_Valve` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_Valve.prefab` | Prefab | `PLACEHOLDER` | primitives | G_Marius | Project-owned (educational use) | Pipe section, 0.3 m `MESH_Valve_Wheel`, box collider | P2 |
| 12 | `MESH_IndustrialDoor_RollUp` | `Assets/_Project/Groups/G_Marius/Level/Prefabs/MachineLock/MESH_IndustrialDoor_RollUp.prefab` | Prefab | `PLACEHOLDER` | `MESH_PORTA_CAMION_TEAM.fbx` + primitives | G_Marius | Project-owned (educational use) | Open frame around the 1.2 × 2.2 m doorway; no collider blocks the walk-through | P2 |

The room itself is a graybox built directly in `SCN_MachineLock`: `MESH_Floor`,
`MESH_Walls`, `MESH_Ceiling`, the sign quads, the yellow safety markings and the
three baked ceiling fixtures are scene objects, not prefabs.

### 4.4 Downloaded third-party assets

Every row here needs a reachable URL and a license that permits redistribution in
a built application.

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| — | *(to be populated in P2 / P8 — currently empty)* | — | — | — | — | — | — | — | — |

### 4.5 Audio

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| — | *(to be populated in P8)* | — | — | — | — | — | — | — | — |

### 4.6 Visual effects and lighting

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| — | *(to be populated in P8)* | — | — | — | — | — | — | — | — |

## 5. Audit coverage summary

The asset audit classifies 22 entries for this scenario. This summary is the
tracking sheet; the credit tables above are the evidence.

| Audit class | Entries | Handled by |
|---|---:|---|
| `[OK]` — real file present in the checkout | 7 | Copy into `G_Marius`, record in §4.1 |
| `[PARTIAL]` — related asset only | 7 | Copy and rename, document the deviation |
| `[MISSING]` — nothing usable | 8 | Placeholder in P2, replacement in P2/P8 |

Blockers carried forward from the audit:

1. Hydraulic press with a moving ram — greybox `MESH_HydraulicPress` in P2.
2. Lockout/tagout props: e-stop, disconnect switch, energy meter, lockout point.
3. Correct/wrong PPE meshes: face shield, apron, heat gloves, respirator.
4. Factory shell and floor markings — `Chemical_Alert_Room` shell or greybox.
5. Oil material — cosmetic, needed for the spill beat.

## 6. Attribution text for the build

If any `DOWNLOADED` row is added, list the required attribution string here
verbatim so the build manifest and the store listing can reuse it.

```text
(no downloaded assets yet)
```

## 7. Open items

- §4.1–§4.3 are populated for P2. Re-check them whenever a blockout asset is replaced.
- Populate §4.4–§4.6 during P8, together with audio, VFX and lighting.
- Confirm the license of every `[BRANCH]` asset copied from another group's
  branch before it is merged into the shared build.
- Re-check the texture downscale for every concrete and metal texture imported
  into `G_Marius`.
