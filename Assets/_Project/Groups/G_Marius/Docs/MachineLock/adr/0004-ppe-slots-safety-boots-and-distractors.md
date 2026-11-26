# Change PPE 4th Correct Slot to Safety Boots and Pair Distractors Cleanly

Status: accepted

Original GDD §9.6 defined the four correct PPE slots as Gloves (Heat Gloves), Body (Chemical Apron), Face (Face Shield), and Respiratory (Respirator FFP3), with Latex Gloves and Surgical Mask as distractors. During the post-Quest 3 redesign interview (Q10), the scenario owner aligned the four correct PPE items with `lista_asset.md`: **Heat Gloves** (`Gloves`), **Chemical Apron** (`Body`), **Face Shield** (`Face`), and **Safety Boots** (`Feet`), while keeping **Latex Gloves** and **Surgical Mask** as the two incorrect distractors.

## Consequences

- `PpeSlot` becomes `{ Gloves, Body, Face, Feet }` (four required protection slots).
- `Latex Gloves` occupies `Gloves` (replaced by `Heat Gloves`), and `Surgical Mask` occupies `Face` (replaced by `Face Shield`), guaranteeing that equipping all four correct items always clears `HasWrongPpeEquipped` with zero soft-lock states.
- `MachineLockPpeItem`, `MachineLockPpeCabinet`, `MachineLockSceneBuilder`, `MachineLockFlowPlayModeTests`, and `GDD_MachineLock.md` §9.6 will be updated to match the four new slots.
