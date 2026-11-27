# Scenario-Owned Damage Feedback Fallback When Camera Post-Processing Is Off

Status: accepted

Quest 3 playtesting showed no visible screen effect during Critical Events because `MNG_CoreRig.prefab` does not enable URP post-processing (`renderPostProcessing = false`) on its camera, causing `MachineLockDamageFeedback` to animate an invisible `Volume` and skip its fallback path. We decided to keep `MNG_CoreRig.prefab` untouched in this branch and make `MachineLockDamageFeedback` inspect whether active camera post-processing is enabled, automatically driving a scenario-owned visual damage flash/vignette, brief respawn fade, and strong haptics (while honoring `accessibility.reduce_flashes`) whenever URP post-processing is unavailable.

## Consequences

- If the scenario-owned visual feedback is still insufficient on Quest 3 after testing, enabling URP post-processing on `MNG_CoreRig.prefab` will be submitted in a separate, dedicated PR so shared-rig changes stay isolated from Scenario 08.
