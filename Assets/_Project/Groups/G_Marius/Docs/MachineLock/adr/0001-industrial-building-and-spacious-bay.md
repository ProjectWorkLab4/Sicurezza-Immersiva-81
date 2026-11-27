# Use OldIndustrialBuilding Shell and Expand Room Footprint

Status: superseded by ADR-0005

The original GDD §7.1 specified a `12 × 8 × 4 m` graybox room, which on Quest 3 proved too cramped once the `3 × 2.5 m` hydraulic press, `7 × 5 m` oil puddle, PPE cabinet, spill kit, and `0.6 m` Player Rig `CharacterController` were placed inside it. We decided to replace the primitive room shell with the repository's shared `PFB_OldIndustrialBuilding.fbx` + `TEX_OldIndustrialBuilding` textures (committed to `scenario/machine-lock` as part of the Shared asset drop) and size the walkable stamping bay generously around all scenario stations and `lista_asset.md` props, updating the GDD and validator budgets accordingly.

## Consequences

- `MachineLockSceneBuilder` and `MachineLockValidatorRunner` will measure room bounds and dry-perimeter margins against the new industrial building interior rather than hardcoded `12 × 8 × 4 m` box walls.
- All three Layout Patterns (A, B, C) must be re-authored against the expanded interior coordinates so walkways stay clear and no prop clips into building pillars or walls.
