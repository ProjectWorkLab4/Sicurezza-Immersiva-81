# Replace OldIndustrialBuilding with Low-Poly Industrial Workshop Shell

Status: accepted (supersedes ADR-0001)

Using `PFB_OldIndustrialBuilding.fbx` consumed `45,379` of the scenario's `100,000`-triangle Quest 3 budget (pushing the scene to `90,356` tris) while still leaving the oversized hall feeling sparse and preventing detailed upgrades to the hydraulic press, electrical Sub-Stations, and workshop dressing. We decided to replace `PFB_OldIndustrialBuilding` with a Blender-authored, PolyHaven-textured `28 × 13 × 6.5 m` Italian industrial stamping workshop shell (`≤ 12,000` tris) that preserves the `18 × 11 m` playable stamping bay, `≥ 2.5 m` Dry Perimeter, and three Layout Patterns (A/B/C) while keeping a visible background factory bay behind the safety barriers.

## Considered Options

- **Raise the Quest 3 triangle cap (e.g. to 140k–160k)** while keeping `PFB_OldIndustrialBuilding`: rejected to preserve safe Quest 3 GPU headroom and avoid frame pacing risk.
- **Keep an enclosed `18 × 11 m` four-walled box only**: rejected because a visible background factory hall beyond the safety barriers gives much stronger industrial scale and realism.
- **Replace the shell with a low-poly `28 × 13 × 6.5 m` Blender workshop hall (chosen)**: reclaims ~35,000+ triangles for cohesive mid-poly hero machinery, factory props, and UNI EN ISO 7010 Safety Signs while staying strictly within the `100,000`-triangle Quest 3 budget.

## Consequences

- `MachineLockSceneBuilder` and `MachineLockValidatorRunner` validate the new scenario-owned workshop shell under `Assets/_Project/Groups/G_Marius/` instead of `PFB_OldIndustrialBuilding.fbx`.
- All new models, textures, and materials stay inside `Assets/_Project/Groups/G_Marius/`; pre-existing `Assets/_Project/Shared/` originals remain untouched.
