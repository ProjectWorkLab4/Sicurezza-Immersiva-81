# Asset Credits — Scenario 08 Machine Lock

| Field | Value |
|---|---|
| Document | `ASSET_CREDITS.md` |
| Scenario | 08 — Machine Lock (`SCN_MachineLock`) |
| Owner | G_Marius |
| Branch | `scenario/machine-lock` |
| Asset source list | [`../ASSET_AUDIT_Scenario08_MachineLock.txt`](../ASSET_AUDIT_Scenario08_MachineLock.txt) |
| Design reference | [`GDD_MachineLock.md`](GDD_MachineLock.md) — §17 Asset plan |
| Status | Template — schema ready, tables populated in P2 (blockout and placeholders) and P8 (audio, VFX, lighting) |

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
| — | *(to be populated in P2)* | — | — | — | — | — | — | — | — |

### 4.2 Assets authored in `G_Marius`

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| — | *(to be populated in P2)* | — | — | — | — | — | — | — | — |

### 4.3 Placeholders and grayboxes

| # | Production asset | Unity path | Type | Status | Source | Author / publisher | License | Modifications | Phase |
|---|---|---|---|---|---|---|---|---|---|
| — | *(to be populated in P2)* | — | — | — | — | — | — | — | — |

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

- Populate §4.1–§4.3 during P2, together with the blockout and placeholder
  prefabs.
- Populate §4.4–§4.6 during P8, together with audio, VFX and lighting.
- Confirm the license of every `[BRANCH]` asset copied from another group's
  branch before it is merged into the shared build.
- Re-check the texture downscale for every concrete and metal texture imported
  into `G_Marius`.
