# Agent rules — Sicurezza Immersiva 81

## Version control: Git only

Use **Git and GitHub exclusively** for branching, commits, merges, reviews, and releases. Decline any Unity or Unity Hub prompt for Unity Version Control / Plastic SCM, and keep `com.unity.collab-proxy` out of `Packages/manifest.json` and `Packages/packages-lock.json` (enforced mechanically by `.githooks/pre-commit` and `scripts/check-hygiene.ps1`).

Enable the repo pre-commit guardrail once per clone:

```bash
git config core.hooksPath .githooks
```

## Navigation

- **Unity builds, validator, EditMode/PlayMode tests, Editor preview PNGs, and Quest 3 APK deploy** → [`scripts/unity-run.ps1`](scripts/unity-run.ps1) (resolves Unity `6000.3.14f1` and ADB, waits synchronously on Windows, omits `-nographics` for Windows OpenXR PlayMode/preview/APK runs, and cleans URP auto-touched Shared materials).
- **Pre-commit & repository hygiene checks** → [`scripts/check-hygiene.ps1`](scripts/check-hygiene.ps1).
- **Code review standards (judgement calls)** → [`CODING_STANDARDS.md`](CODING_STANDARDS.md); asset naming prefixes in [`Assets/_Project/Docs/Nomenclatura.md`](Assets/_Project/Docs/Nomenclatura.md); shared-asset and Project Setup Tool rules in [`README.md`](README.md).
- **Shared Player Rig, HUD, and Interaction runtime contracts** → `Assets/_Project/Home/Code/Scripts/Runtime/` (UI data/prefabs live in `Assets/_Project/Home/UI/`).
- **Scenario 08 (Machine Lock) design, glossary, ADRs, and credits** → `Assets/_Project/Groups/G_Marius/Docs/MachineLock/`.
