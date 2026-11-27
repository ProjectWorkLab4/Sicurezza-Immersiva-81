# AGENTS.md — Group Simone (G_Simone)

This file is the instruction sheet **and the complete record** for AI agents working in this area.
Every request from the user must be recorded below.

## Scope

- Work **only** inside `Assets/_Project/Groups/G_Simone/`.
- Do **not** touch files outside this folder (e.g. `Assets/_Project/Shared/`, the repository root, other groups) unless the user explicitly tells you to.

## Operating rules

1. **Confirm the directory before every change.** Ask the user to confirm the exact target directory each time you are about to modify or create something — every single time, even if it is the same folder as before.
2. **Track everything.** Every time the user asks you to do something, add an entry to the **Action log** with the date, the request, what you did, and the files you changed — **including your thoughts/reasoning and anything you learned** while doing it, not just a bare action list.
3. **Never commit unless explicitly told.** Do not create git commits on your own. Treat other git-changing operations (push, reset, force push) as requiring explicit instruction too.
4. **Ask when unclear.** If an instruction is ambiguous, ask the user instead of guessing.
5. **Recap when asked.** If the user asks for a recap of your understanding, provide it and wait for confirmation before acting.
6. **Approval loop.** For every task, post your thoughts, reasoning, and what you learned in the chat, and wait for the user's approval before writing that text into the Action log.
7. **Follow the repository README.** `README.md` at `C:\Users\PelloniMatelda\Documents\Sicurezza-Immersiva-81\` is the source of truth for project conventions. Follow its indications: the shared-asset pull-before-push workflow, the "Fix All" restrictions, area ownership rules, placeholder rules, naming conventions and Conventional Commits. This governs *how* work is done; the scope rule above still limits which files I may change unless you explicitly say otherwise.
8. **Activation repository check.** At the start of each activation, report whether the repository shows signs of change, using only local information. Do **not** run `git fetch`, `git pull`, `git push`, or any git command that changes state — the user runs all git commands personally and will ask if a fetch is needed. Read-only local inspection (e.g. `git status`, `git log`, `git diff`, reading local refs) is permitted for this report only. If read-only commands are not permitted, say so instead of guessing.

## Project context

- **Project:** Sicurezza Immersiva 81 — a collaborative VR serious game about recognising and managing workplace emergencies.
- **Group:** G_Simone — scenario **Blackout (ID 2)**.
- **Game design document (same folder):** `Scenario_Blackout_ID_2.md`.

Key points from the design document:

- Free exploration while a timer runs; no automatic level progression. Win by leaving through the **emergency exit**.
- GDD mechanics only: 6DoF / teleport movement, grab and drop, direct tool use, door opening, typing numbers on the phone, hearts and vignette feedback.
- A fire alarm and blackout occur after a few seconds; emergency lights and green exit signage guide the player. Smoke increases over time; fire blocks the wrong path.
- Phone call to 112 and the gas detector are time-wasting / neutral actions; restarting the electrical panel causes a spark and damage.
- Outcomes: **full success** (correct exit, no wrong actions), **partial success** (survive but exit through the normal door or make mistakes), **failure** (all hearts lost or time expired).
- Difficulty tiers: **Guided** (highlighted signage, no hearts), **Realistic** (3 hearts, 3 min), **Stress** (2 min, faster smoke).

Earlier this session: `group_simone` was reset to cloud `origin/main` and force-pushed with the two Blackout scenes committed. The previous local state is still available in the local ref `backup/group_simone-pre-cloud` (`14676a7`).

## Action log

### 2026-11-23 — Setup of this file
- **Request:** Create `agents.md` in the group's docs folder, read the game design document in the same folder, restrict all work to `group_simone`, record everything done in it, ask when unclear, and confirm the target directory before every change.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/`
- **Design document confirmed by user:** `Scenario_Blackout_ID_2.md`
- **Actions performed:**
  - Read `Scenario_Blackout_ID_2.md` (Blackout scenario design document).
  - Created this file (originally named `agents.md`, later renamed to `AGENTS.md`) with the scope, operating rules, project context and action log.
  - Created the Unity `.meta` companion file.
- **Files changed:**
  - `Assets/_Project/Groups/G_Simone/Docs/Blackout/agents.md` (new; renamed later)
  - `Assets/_Project/Groups/G_Simone/Docs/Blackout/agents.md.meta` (new; renamed later)

### 2026-11-23 — Rules update and rename to AGENTS.md
- **Request:** "I don't want you to commit anything unless i tell you to do so, write every future task you complete in the AGENTS.md file, including thougths and anything you learned." Follow-up: confirmed the same directory and asked to rename the file to `AGENTS.md` (capital) because it is more conventional.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/`
- **Actions performed:**
  - Asked for directory confirmation before making any change, as required.
  - Backed up the existing file and its `.meta` to `C:\Users\PelloniMatelda\AppData\Local\Temp\opencode\agents-backup\`.
  - Renamed `agents.md` → `AGENTS.md` and `agents.md.meta` → `AGENTS.md.meta`, preserving the original Unity GUID `690bf24e3b83472287dfd1f43cf3cb86`.
  - Added the "never commit unless explicitly told" rule and expanded the logging rule to include thoughts and learnings.
- **Files changed:**
  - `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (renamed from `agents.md`, updated)
  - `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md.meta` (renamed from `agents.md.meta`, GUID preserved)
- **Thoughts / reasoning:** Filename case matters for convention and consistency even on Windows, where paths are case-insensitive. Keeping the same Unity GUID in the `.meta` avoids breaking any references. A backup was taken before deleting the old files so nothing could be lost if something went wrong.
- **What I learned:** A normal write to `AGENTS.md` on Windows would have overwritten the existing lowercase file rather than renaming it, so the old file had to be removed first. Also observed unrelated modified/untracked files (Shared materials, regenerated `.meta` files, `.vscode/`, OpenXR settings, `Assets/Blackout.unity`) that appeared after the earlier hard reset — most likely Unity re-importing assets. I left all of them untouched.

### 2026-11-23 — Approval step for log entries
- **Request:** "Every completed task gets logged in AGENTS.md with the request, actions, files changed, my thoughts/reasoning, and anything I learned. Also send thoughts, reasoning and learning in this chat so I can approve what you write in agents.md."
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/`
- **Actions performed:** Update `AGENTS.md` with a new operating rule requiring chat approval, and append this entry.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md`
- **Thoughts / reasoning:** You want visibility and control over the log content, not only the actions. Writing my reasoning into a persistent file without showing it first would remove your chance to correct a mistake or disagree. The safest workflow is therefore: draft in chat → you approve → I write.
- **What I learned:** "Tracked" here means more than a git record — it means an explicit, reviewable narrative in `AGENTS.md`. You treat that file as a control document, so its content should be proposed rather than assumed. I also noted that rules accumulate: the earlier rules (confirm directory before every change, no commits unless told, stay inside `group_simone`) still apply to changes made to `AGENTS.md` itself.

### 2026-11-23 — Follow repository README
- **Request:** "new rule: follow indications explicited in README in this directory: C:\Users\PelloniMatelda\Documents\Sicurezza-Immersiva-81"
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/`
- **Actions performed:** Read the root `README.md` in full (path explicitly authorized by the user), drafted a new operating rule and this log entry, and waited for approval before changing anything.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (pending approval)
- **Thoughts / reasoning:** The README is the team's shared rulebook; treating it as authoritative prevents me from violating conventions around Shared assets and Unity settings. It is an addition, not a replacement — the existing no-commit and scope rules still apply unless explicitly overridden.
- **What I learned:** Shared folder requires pull-before-push and copying assets instead of editing originals; "Fix All" must not be run casually and stray changes are discarded one file at a time; each area owns its own folder; keep PLACEHOLDER files and commit `.meta` files; `SCN_Placeholder.unity` is the only build scene and must not be touched by a group; use uppercase prefixes with PascalCase names, Conventional Commits, and the `Build/Sicurezza-Immersiva-81_ver<num>.apk` build naming.

### 2026-11-23 — Activation repository check rule
- **Request:** "I do want the activation check as a standing rule, but I want to pull/fetch/give any git command myself. I'll ask if I need you to fetch. I just want you to tell me if there have been any changes in the repository."
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/`
- **Actions performed:** Clarified whether read-only local git inspection is allowed (user answered yes), drafted rule 8 and this entry, and waited for approval before writing.
- **Files changed:** `AGENTS.md` (pending approval)
- **Thoughts / reasoning:** The user wants repository visibility without surrendering control of git operations. A report is only meaningful if I have an authorized way to observe state; therefore the rule explicitly separates permitted read-only inspection from forbidden state-changing/network commands.
- **What I learned:** All git commands are user-owned; I must never fetch, pull, push, or otherwise alter git state on my own, and any activation report must be based only on locally available data.

### 2026-11-24 — Emissive material for MESH_SegnaleticaLuminosa
- **Request:** "can you add a sensible amount of emission on MESH_SegnaleticaLuminosa please. it shouldn't be visible with normal lighting but visible in the dark", clarified to "modify emission on its plane, not the entire asset". User chose to create the material only and assign it in Unity, with moderate green emission, in G_Simone/Art/Materials.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Materials/` for the material; `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for the log.
- **Actions performed:** Inspected `MESH_SegnaleticaLuminosa.fbx` and its `.meta`; found one geometry named `Plane` and no material definition, so Unity uses the default material. Confirmed the only scene instance is in `Assets/Blackout.unity` (outside scope). Copied URP/Lit shader GUID from an existing Shared material. Created `MAT_SegnaleticaLuminosa_G_Simone.mat` with `_EMISSION`, near-black `_BaseColor` (0.01, 0.01, 0.01), `_EmissionColor` green at intensity 2, and `m_LightmapFlags: 1`. Created its `.meta` with a new unique GUID `3f51081bac434f198a59e8c0f47b4a62`. Ran the root AGENTS.md `collab-proxy` check (clean). Left the user's other local changes untouched.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Art/Materials/MAT_SegnaleticaLuminosa_G_Simone.mat` (new), `...mat.meta` (new), `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (log entry).
- **Thoughts / reasoning:** Emission is additive, so a bright emissive would show even in daylight; keeping the base color near-black makes the signage read as unlit in normal light while the moderate green emission becomes obvious in darkness. Because the FBX has no material of its own, the cleanest in-scope deliverable is a reusable material asset; assigning it requires a renderer override in the scene/prefab, which the user chose to do.
- **What I learned:** The FBX contains a single `Plane` geometry and no embedded material. URP/Lit shader GUID in this project is `933532a4fcc9baf4fa0491de14d08ed7`. Root `AGENTS.md` mandates Git only and forbids Unity VCS; `git grep collab-proxy` on the package manifests returned nothing. Numerous unrelated local changes (scene, prefabs renames, OpenXR settings, Shared materials) are present and were not modified.

### 2026-11-24 — Emergency light material for G_Simone
- **Request:** "can you make me a material under G_simone/Art/materials for a regular emergency light. it has to be moderately emissive". Follow-up choices: folder `G_Simone/Art/Materials`, name `MAT_LuceEmergenza`, emission colour "Emergency green".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Materials/` for the material; `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for the log.
- **Actions performed:** Ran the read-only activation repository check and reported the existing local changes. Read `MAT_SegnaleticaLuminosa.mat` as a serialization reference. Generated a new unique GUID `8677a78dc2dc4dd5999612f2d04d0ed6` and confirmed it was unused. Created `MAT_LuceEmergenza.mat` using URP/Lit (shader GUID `933532a4fcc9baf4fa0491de14d08ed7`), opaque render type, `_EMISSION` keyword, `m_LightmapFlags: 1`, `_BaseColor`/`_Color` light grey `(0.8, 0.8, 0.8)` and `_EmissionColor` emergency green `(0, 1.5, 0)`. Created the matching `.meta`. Did not assign the material to any renderer and did not modify scenes, scripts, Shared assets or ProjectSettings.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Art/Materials/MAT_LuceEmergenza.mat` (new), `...MAT_LuceEmergenza.mat.meta` (new), `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (log entry).
- **Thoughts / reasoning:** Copying the existing material's serialization keeps the URP/Lit shader GUID and `AssetVersion` consistent with the project. A light-grey base makes the fixture read as a normal emergency light under normal lighting, while moderate green emission at intensity 1.5 makes it visible in the dark without the stronger signage glow. Creating the material only keeps the change inside G_Simone and avoids touching the shared Blackout scene.
- **What I learned:** The existing `MAT_SegnaleticaLuminosa.mat` in the folder uses URP/Lit, `_EMISSION`, near-black base `(0.01, 0.01, 0.01)` and green-yellow emission `(0.22352941, 1, 0)`. The folder is `Materials` with capital M. The working tree contains unrelated user changes (scene, OpenXR settings, Shared materials, prefabs, docs), which were left untouched. No commit, push or Unity VCS tooling was used.

### 2026-11-24 — OnBlackout event and looping fire-alarm placeholder
- **Request:** "use the same GameManager script and make it so: -after 5 seconds an OnBlackout event happens -when the event is triggered the light components in the hierarchy turn off -activate a sound (use a placeholder if you can, I don't have a sound yet. ignore this if you can't use a placeholder)". Follow-ups: "the sound has to be activated with the event", "does the timer start as soon as the game loads? also if you can, generate a fire alarm that loops for the audio clip".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` for the script; `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for the log.
- **Actions performed:** Ran the read-only activation repository check. Inspected `GameManager.cs`: its immediate `Time.realtimeSinceStartup - startTime >= 5f` check could never fire and `OnBlackout()` was empty. Replaced it with `public event Action OnBlackout`, a `WaitForSecondsRealtime(5f)` coroutine started in `Start()` (so the timer begins when the scene/object becomes active), and two explicit subscribers: `DisableSceneLights()` (all `Light` components, including inactive, set `enabled = false`) and `PlayBlackoutSound()`. Added serialized `blackoutSound`, an `AudioSource` reused/created in `Awake()` with `loop = true`, and a runtime-generated 2-second alternating 800/600 Hz fire-alarm placeholder with short fades to avoid clicks; the clip loops until stopped. Added `using System.Collections;` for the coroutine. No scene, prefab, Shared, or ProjectSettings files were modified.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/GameManager.cs` (modified); `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (log entry).
- **Thoughts / reasoning:** Making the sound a separate event subscriber makes it unambiguously activated by `OnBlackout`. Real-time delay matches the earlier timing approach and is unaffected by `timeScale`. Disabling all `Light` components creates the blackout while emissive materials continue to glow for emergency signage. The generated looping alarm is a placeholder that can be replaced later through the Inspector without further code changes.
- **What I learned:** Unity has no `PointLight` class; point lights are `Light` components serialized with `m_Type: 2`. The `---GameManager---` object has no children, so scene-wide `FindObjectsByType<Light>` is necessary. Unity 6 supports `FindObjectsInactive.Include`, `FindObjectsSortMode.None`, `WaitForSecondsRealtime`, and runtime `AudioClip.Create`/`SetData`. No commit, push or Unity VCS tooling was used.

### 2026-11-24 — Darker scene on OnBlackout
- **Request:** "I want you to add what you gave me before to the script, but it's not in any way related to it not working. I already fixed that and it works now, I just want it to be darker once the event is triggered".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` for the script; `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for the log.
- **Actions performed:** Ran the read-only activation repository check. Read `GameManager.cs` and preserved the user's current `Start()`-based `AudioSource` initialization. Added `ApplyBlackoutLook()` as a third `OnBlackout` subscriber, with a matching unsubscribe in `OnDestroy()`. The method sets `RenderSettings.ambientMode` to Flat, `ambientLight` to black, `ambientIntensity` to 0 and `skybox` to null; enables black `FogMode.ExponentialSquared` fog at density 0.02; and sets `Camera.main` clear flags to `SolidColor` with a black background. Light shutdown and alarm logic were not modified.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/GameManager.cs` (modified); `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (log entry).
- **Thoughts / reasoning:** This darkens the structure at the same moment as the blackout event while leaving emissive materials visible for guidance. Runtime `RenderSettings` changes reset when play stops, so no permanent scene settings were altered.
- **What I learned:** `UnityEngine.Rendering.AmbientMode` can be fully qualified to avoid adding another `using`. Fog and camera background changes help darken areas that ambient settings alone do not affect.

### 2026-11-25 — Push rejected (non-fast-forward), resolved by fetch + merge + push
- **Request:** "may you push the commit I made please? I can't understand what the issue is"; follow-up choice "Fetch + merge, then push (Recommended)".
- **Directory confirmed by user:** not applicable (git-only operation). Logging for this request was explicitly declined at the time and re-enabled later.
- **Actions performed:** Read-only inspection (`git status`, `git log`, `git remote -v`) showed local commit `5bdcc1f` ahead 1 / behind 4 vs `origin/group_simone`. An attempted `git push origin group_simone` was rejected as non-fast-forward. With approval, ran `git fetch origin`, `git merge origin/group_simone --no-edit` (clean; merge commit `611c8b2` brought in GasLeak content, Shared materials, `Tools/`, HUD PDF), then `git push origin group_simone` → `b8032b2..611c8b2`. All uncommitted working-tree changes were left untouched.
- **Files changed:** none in the working tree; git history only (merge commit `611c8b2`, push to `origin/group_simone`).
- **Thoughts / reasoning:** The failure was branch divergence, not credentials or permissions. A plain push would have dropped 4 remote commits, so merging preserved everyone's work. Force-push was rejected as too destructive. Uncommitted files can never be pushed.
- **What I learned:** `git status -sb` shows ahead/behind counts; non-fast-forward means the local tip is behind its remote counterpart. When both sides have unique commits, a merge commit is unavoidable; `--no-edit` keeps Git's default message.

### 2026-11-25 — Commit and push remaining local work; logging resumed
- **Request:** "I do want them committed and pushed, now you can go back to logging in agents."
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for the log; the commit scope explicitly includes the listed files outside `G_Simone` (`Assets/Blackout.unity` and `Assets/XR/Settings/OpenXR Package Settings.asset`), both approved by the user.
- **Actions performed:** Ran the root `collab-proxy` check (clean). Reviewed the tracked diffs and untracked file list. The user chose to include the machine-generated OpenXR feature `fileID` churn and to keep the deletion of the temporary camera/light in `Assets/Blackout.unity`. Appended this entry and the earlier push entry to `AGENTS.md`. Staged all approved files, created the Conventional Commit `feat(blackout): add emissive materials, prefabs, scripts and scene setup`, and pushed `group_simone` to `origin/group_simone`.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (two new log entries), plus all approved modified/deleted/untracked files from the working-tree review (emissive materials and `.meta` files, new prefabs and `.meta` files, `MESH_SegnaleticaLuminosa.fbx`/`.meta`, `GameManager.cs`/`.meta`, `Assets/Blackout.unity`, `Assets/XR/Settings/OpenXR Package Settings.asset`).
- **Thoughts / reasoning:** Restoring logging after the earlier opt-out keeps the group's control document complete. The README normally treats OpenXR settings churn as a change to review and discard, but the user explicitly approved committing it here. Keeping the camera/light deletion is consistent with the camera being a temporary stand-in until the real VR player rig exists. The mandatory `collab-proxy` check returned nothing before the commit.
- **What I learned:** Explicit user approval can override the README's default handling of machine-generated XR settings. The temporary camera in `Assets/Blackout.unity` is not the final VR rig, so camera-dependent code must be adapted when the rig arrives. No Unity Version Control tooling was used.

### 2026-11-25 — Finish stuck merge after fetch/pull attempt
- **Request:** "pull what is in origin"; follow-up choice "Finish the merge".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for the log.
- **Actions performed:** `git fetch origin` (only `origin/group_tiziano` moved, `57dbae2`→`339ee72`). `git pull origin group_simone` failed: `You have not concluded your merge (MERGE_HEAD exists)`. Inspection showed `.git/MERGE_HEAD` empty (0 bytes), 942 staged files, `ORIG_HEAD` = `8801308`, and `git status` reporting "All conflicts fixed but you are still merging". With approval, restored `MERGE_HEAD` to `origin/main` (`811ee1983ef9ed6304fe7ab859e05a36c011cf8f`), re-ran the `collab-proxy` guard (clean), and committed the merge as `6083302` (`Merge branch 'origin/main' into group_simone`). Then pushed `group_simone` to `origin/group_simone` and appended this log entry.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this log entry); git history only otherwise — merge commit `6083302` integrating `origin/main` into `group_simone` (942 staged files) and its push. No G_Simone file content was edited by me.
- **Thoughts / reasoning:** Pulling was impossible while a broken merge state existed. Finishing the merge preserved the large staged integration instead of discarding it with an abort. Since `origin/group_simone` had no new commits, completing the in-progress merge was the only integration needed, and pushing then brought the remote back in sync.
- **What I learned:** An empty `.git/MERGE_HEAD` makes Git report an unfinished merge and blocks any pull; `ORIG_HEAD` still identifies the pre-merge commit. `origin/group_tiziano` changed on the remote but belongs to another group, so it was left alone. The `collab-proxy` guard remains clean.

### 2026-11-25 — Restore Struttura.prefab from Git
- **Request:** "do you see or know what happened to the file Struttura that was saved under the folder Art/Prefab in G_simone? also tell me if there's any way to restore it or restore the latest version of it"; follow-up choice "Restore latest (Recommended)".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Prefab/` (named in the selected restore option).
- **Actions performed:** Read-only inspection showed `Struttura.prefab` and `.meta` tracked in `HEAD` and `origin/group_simone` but absent from the working tree (`.D` unstaged deletion). Restored both with `git restore -- <paths>`; verified both files exist and the folder has no Git differences.
- **Files changed:** `Struttura.prefab` and `Struttura.prefab.meta` restored in the working tree; no Git history changed.
- **Thoughts / reasoning:** Restoring from the index/HEAD recovers the exact committed/pushed version without reintroducing the older `MESH_Struttura` name/GUID and needs no new commit.
- **What I learned:** The restored version still references the old GUID `064c3fd498cb4344aa653cf035173c87`, which now belongs to `Puf.prefab.meta`; Unity may treat the "Struttura" prefab as a variant of "Puf" until that is corrected.

### 2026-11-25 — Restore deleted model files under G_Simone/Art/Models
- **Request:** "can you restore the last pull on my local?"; clarified to "I need to restore the deleted files, that's it".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Models/`.
- **Actions performed:** Read-only inspection showed six tracked files deleted in the working tree: `P_W_4_Scena.fbx`, `P_W_4_Scena 1.fbx`, `P_W_4_Scena 1 (1).fbx` and their `.meta` files. Restored all six from `HEAD` with `git restore`; verified the folder has no remaining Git differences. No pull/merge was undone and no history was changed.
- **Files changed:** the six FBX/`.meta` files restored in the working tree; no commit or push.
- **Thoughts / reasoning:** The files were still tracked in `HEAD`, so a simple `git restore` recovered the committed versions without touching the merge, other local changes, or remote state.
- **What I learned:** The earlier "restore the last pull" request meant restoring files deleted locally, not reverting the merge. Unstaged deletions of tracked files are recoverable from the index/HEAD with `git restore`.

### 2026-11-25 — Animate blackout door Z rotations
- **Request:** "may you change GameManager so that OnBlackout event also changes the Z rotation (exclusively on inspector) of these objects changes like this: porta_tagliafuoco_D3 90 -> 0, porta_tagliafuoco_D3 180 -> 90, porta_tagliafuoco_D12 0 -> -90, porta_tagliafuoco_D5 0 -> 90, porta_tagliafuoco_D6 90 -> 180, porta_tagliafuoco_D7 90 -> 0; even better if you can make it quick but not immediate. animate it a bit".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` for the script; `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for the log.
- **Actions performed:** Added an Inspector-editable `BlackoutDoorRotationRule[]` with the six supplied name/from-Z/to-Z rules, a 0.6 s duration, duplicate-name matching by starting Z (2 degree tolerance), and a smooth `Mathf.SmoothStep` coroutine subscribed to `OnBlackout`. Only each object's local Euler Z is changed; X/Y are preserved. No commit or push.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/GameManager.cs`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this log entry).
- **Thoughts / reasoning:** Rules stay visible/editable in the Inspector, and the two `porta_tagliafuoco_D3` entries are distinguished by their starting Z value. Animating over time avoids an instant snap.
- **What I learned:** `localEulerAngles.z` plus `Mathf.DeltaAngle` lets the animation start from the object's current Inspector angle without wrapping the long way around.

### 2026-11-25 — Debug blackout door rotation matching
- **Request:** "the objects don't seem to move".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` (script) and `Assets/_Project/Groups/G_Simone/Docs/Blackout/` (log).
- **Actions performed:** Verified the script is attached to `---GameManager---` inside `Struttura.prefab` and the six rules are serialized. Made name matching case-insensitive with suffix support, widened Z tolerance to 5 degrees, added an optional direct `Transform` per rule, a 0.1 s start delay, and `[BlackoutDoors]` console diagnostics. No commit or push.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/GameManager.cs`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this log entry).
- **Thoughts / reasoning:** The likely failure was exact name/start-Z matching; diagnostics will reveal actual runtime names/angles if it still fails.
- **What I learned:** The scene contains only the `Struttura` prefab instance, with GameManager nested inside it; the prefab already stores the edited rule fields.

### 2026-11-25 — Add box colliders to mesh objects in SCN_Blackout
- **Request:** "are you able to put collision boxes on every component in scene that's not under Luci or GameManager" (asked twice); follow-up choice "Mesh objects only".
- **Directory confirmed by user:** active scene `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity`; log under `Assets/_Project/Groups/G_Simone/Docs/Blackout/`.
- **Actions performed:** Counted 225 objects outside `Luci` and `---GameManager---`, of which 172 had a MeshRenderer and 1 (`MESH_Telefono`) already had a collider. Added BoxCollider to the remaining 171 mesh objects using Unity tooling, sized from each `MeshFilter.sharedMesh.bounds` with a 0.01 minimum thickness. Skipped empty parents, manager roots, lights, GameManager, and existing colliders. Saved the scene. No commit or push.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this log entry).
- **Thoughts / reasoning:** Mesh-only scope avoids meaningless colliders on empty parent/manager objects and prevents duplicate colliders.
- **What I learned:** Prefab-instance children can receive scene-level component overrides; they persist after saving the scene.

### 2026-11-25 — Tighten colliders, cover doors/windows, clear SegnaleticaLuminosa
- **Request:** "make the colliders tighter on the meshes, also put colliders on doors and windows. take off colliders on components named SegnaleticaLuminosa".
- **Directory confirmed by user:** active scene `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity`; log under `Assets/_Project/Groups/G_Simone/Docs/Blackout/`.
- **Actions performed:** Removed 21 Collider components from all objects whose hierarchy contains a `SegnaleticaLuminosa*` ancestor. Tightened 150 BoxColliders on mesh objects to exact `MeshFilter.sharedMesh.vertices` min/max bounds (minimum 0.001 thickness). Added BoxCollider to 11 door/window objects that lacked one and updated 34 existing door/window colliders, using local vertex bounds for meshes and hierarchy-local bounds for parent empties. Saved the scene. No commit or push.
- **Files changed:** `SCN_Blackout.unity`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this log entry).
- **Thoughts / reasoning:** Exact vertex bounds remove the padding introduced by renderer bounds; parent door/window empties use combined child mesh bounds so there is one tight collider per logical door/window.
- **What I learned:** The `SegnaleticaLuminosa` objects themselves had no colliders; the colliders were on their mesh children, so ancestor-based removal was necessary.

### 2026-11-25 — Add collider to pavement and walls
- **Request:** "may you add a collider to the pavement and the walls".
- **Directory confirmed by user:** active scene `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity`; log under `Assets/_Project/Groups/G_Simone/Docs/Blackout/`.
- **Actions performed:** Inspected the active scene and found the pavement/walls combined in `/Struttura/stanze` (2 submeshes, 3,432 vertices, materials `Material.004`/`Material.005`, world bounds ~82.35 × 6.16 × 97.33). Added a non-convex `MeshCollider` using that mesh. Confirmed `COL_Floor_Blackout` and `/Struttura/soffitto` already had BoxColliders and left them unchanged. Saved the scene. No commit or push.
- **Files changed:** `SCN_Blackout.unity`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this log entry).
- **Thoughts / reasoning:** A BoxCollider around the combined walls/pavement mesh would create a single solid volume filling the whole building, so a MeshCollider was used to preserve room interiors.
- **What I learned:** `stanze` is a scaled, non-readable imported mesh, but assigning it to `MeshCollider.sharedMesh` still works in the Editor.

### 2026-11-25 — Convert scene colliders to exact MeshColliders
- **Request:** "can you make it so even other meshes collide with the player?"; follow-up choice "Use exact MeshColliders everywhere".
- **Directory confirmed by user:** active scene `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity`; log under `Assets/_Project/Groups/G_Simone/Docs/Blackout/`.
- **Actions performed:** Inventoried 163 colliders: 149 BoxColliders on mesh objects, 12 BoxColliders on non-mesh objects, and 2 MeshColliders. Replaced the 149 mesh BoxColliders with non-convex MeshColliders using each `MeshFilter.sharedMesh`. Added MeshColliders to the 21 previously non-colliding `SegnaleticaLuminosa*/Plane` meshes, so those now collide with the player too. Ensured the 2 existing MeshColliders had their meshes assigned. Removed 11 BoxColliders from door/window parent empties because their child meshes now provide exact collision; kept the BoxCollider on `COL_Floor_Blackout` because it has no mesh and is already an exact rectangular floor volume. Saved the scene. No commit or push.
- **Files changed:** `SCN_Blackout.unity`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this log entry).
- **Thoughts / reasoning:** MeshColliders follow the actual mesh geometry instead of box bounds, so the player collides with concave shapes accurately. Removing parent boxes avoids duplicate, approximate blockers.
- **What I learned:** `MeshCollider.sharedMesh` accepts non-readable imported meshes in the Editor; the sign planes were the only meshes that still lacked colliders.

### 2026-11-25 — Reset blackout doors on player interaction
- **Request:** "may you make me a script so that if the player interacts with one of the doors listed in GameManager, it goes back to its original position?" Follow-ups: new script in `G_Simone/Code/Scripts`; trigger via shared `Interactable` select/grab; restore the pre-blackout local Z rotation; log the task in `Docs/Blackout/AGENTS.md`.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` for the script; `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for the log.
- **Actions performed:** Ran the read-only activation repository check (branch `group_simone`, 4 ahead, local modifications in GameManager, scene, docs, OpenXR and Home files, untracked `BlackoutScenario.cs`). Created `DoorResetOnInteract.cs` and its `.meta`, which reads `GameManager.DoorRotationRules`, captures each door's current local Z in `Awake` before the delayed blackout, resolves direct `target` or name+fromZ matches via `GameManager.DoorNameMatches`, subscribes to `SelectEntered`/`GrabEntered` on the door's `Interactable` (adding one with `FollowsHand = false` if absent), and smoothly animates the door back to its captured Z; it keeps re-applying that Z while GameManager's blackout rotation is still active. Added read-only `DoorRotationRules` / `IsDoorRotationActive` accessors and made `DoorNameMatches` public static in `GameManager.cs`. Verified with a Unity script recompile (`completed`, no errors) and a `typeof(DoorResetOnInteract)` eval. No commit or push.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/DoorResetOnInteract.cs` (new), `...DoorResetOnInteract.cs.meta` (new), `Assets/_Project/Groups/G_Simone/Code/Scripts/GameManager.cs` (modified); `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** Keeping the rules in GameManager avoids a second list to maintain, so a read-only property was the smallest change. Capturing the actual angle in `Awake` rather than trusting `fromZ` blindly preserves the exact Inspector value, while `Mathf.DeltaAngle` avoids 0/360 wrapping. Using shared `Interactable` events adds no new input path, and auto-adding a non-hand-following `Interactable` means the existing colliders work without scene edits. The post-animation guard handles interaction during the 0.6 s blackout animation.
- **What I learned:** `SI81.Interaction` and `SI81.Input` are auto-referenced, so Assembly-CSharp scripts in G_Simone can use `Interactable`/`Hand` directly. `Interactable` is sealed and disallows duplicates on one GameObject, but the ray walks up from child colliders to a parent `Interactable`. Unity MCP reached the correct project; recompile completed with no errors and eval confirmed the compiled type. GameManager already had a local D3→D4 edit, which was preserved.

### 2026-11-25 — 3-minute timeout shows the failure screen
- **Request:** "I already have a timer script. how do I make it so the player sees the failed screen (already exists) if it doesn't trigger a success in 3 minutes?" Follow-ups: use the shared HUD timer; treat the existing mission end as success; follow the GDD (Guided no countdown, Realistic 180 s, Stress 120 s), currently developing Realistic; edit only `G_Simone/Code/Scripts`.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` for the code; log under `Assets/_Project/Groups/G_Simone/Docs/Blackout/`.
- **Actions performed:** Ran the read-only activation repository check. Confirmed that `HUDService.TimerExpired` already flows through `ScenarioController.HandleTimerExpired` into `BlackoutScenario.OnTimerExpired()`, which sets `timeout = true` and calls `ChiudiMissione()` → `EndMission(OutcomeType.Negative)` → the common failed/debrief screen, and that `EndMission` already calls `HUDService.StopTimer()` on success. Added `AvviaTimerScenario()` called from `BlackoutScenario.OnScenarioStarted()` to start `HUDService.StartTimer` with 0 s for Guided, 180 s for Operational/Realistic and 120 s for Crisis/Stress. No Home asset, scene or meta edits; no commit or push. Unity recompile completed with no errors.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs` (modified); `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry, pending approval).
- **Thoughts / reasoning:** The failure screen and the timeout→negative path already existed, so no new UI or event plumbing was needed — only making the shared HUD timer run for the GDD duration. Starting it in `OnScenarioStarted()` overrides the level-config limit (600/300 s) without touching Home-owned assets. Success needs no extra code because `EndMission` stops the timer. Guided gets 0 s so the HUD shows elapsed time without a countdown, matching the GDD.
- **What I learned:** `HUDRoot.StartTimer(0)` switches to stopwatch mode and never raises `TimerExpired`; positive values count down with 60/30 s warnings. `MissionDefinition.GetTimeLimit` returns level-config values (Guided 0, Operational 600, Crisis 300) and mission overrides are currently 0 in `SO_Mission_Blackout.asset`. `ScenarioController.HandleTimerExpired` already guards `IsRunning`, so the override only fires during an active mission.

### 2026-11-25 — Success screen from grabbing D1 or D10, with GDD-based scoring
- **Request:** "Implement the success screen by opening door D1 and D10"; clarified "by opening I mean grabbing it". Correction: "the 100% points are only given if the player follows procedimento corretto in the GDD"; final tweak: "both doors can't be grabbed together, so it's an or rather than an and". Follow-ups: use `porta_tagliafuoco_D1` + `porta_D10`, no door rotation, track the other GDD wrong actions too, more scoring context to come later, edit only `G_Simone/Code/Scripts`.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/`.
- **Actions performed:** Ran the read-only activation repository check and inspected the open scene hierarchy. Added to `BlackoutScenario.cs`: serialized `porteSuccesso` names, runtime `Interactable` setup on each door with `FollowsHand = false`, `GrabEntered` tracking with deduplication; grabbing **either** door during an active mission calls `EndMission(Positive)` with safetyScore 100 and five 100 dimensions only if there are no decision errors, no GDD wrong actions and no timeout, otherwise Positive with dimension-derived score and partial-success debrief lines naming the grabbed door. Added GDD wrong-action tracking: grab of `RilevatoreDiGas`, power-on of `QuadroElettricoInterattivo` when present, and a public `SegnalaAzioneErrata(string)` hook for future area triggers such as not following the signage. Updated `CalcolaDimensioni` and `CostruisciDebrief` to include GDD errors; error lists reset at scenario start; `OnDestroy` unsubscribes all handlers. Verified with a Unity recompile (`completed`, no errors). No scene or Home edits; no commit or push.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs` (modified); `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry, pending approval).
- **Thoughts / reasoning:** A fixed 100 would reward wrong actions, so full marks are gated by the GDD's "Procedimento corretto": no wrong decisions, no GDD wrong actions and no timeout. Partial success still shows the success screen but uses the dimension-derived score and flags mistakes in the debrief until the final scoring rules arrive. Because both doors cannot be grabbed together, the trigger was changed from a cumulative AND to a single-grab OR; the first valid grab ends the mission and the second is ignored by `EndMission`'s guard. Wrong actions are deduplicated by description so repeated grabs do not stack, and the public hook covers "non seguire la segnaletica" until a fire/smoke trigger exists.
- **What I learned:** The scene has `RilevatoreDiGas` (mesh + collider, no script or `Interactable`) and no `QuadroElettricoInterattivo` instance, so that subscription stays dormant until one is added. `QuadroElettricoInterattivo.CambioStato` is an `Action<bool>` with `true` = powered. `Interactable.GrabEntered` fires independently of `FollowsHand`. `EndMission` with safetyScore `-1` derives the score from the five dimensions; 100 must be passed explicitly. Unity recompile completed with no errors after switching the trigger to OR.

### 2026-11-25 — Guided rule 1: custom URP outline instead of tint
- **Request:** Fix the Guided highlighter: outline the object instead of tinting it; use a custom URP shader; revert my earlier BlackoutScenario highlighter edits.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/`, `Assets/_Project/Groups/G_Simone/Art/Shaders/`, `Assets/_Project/Groups/G_Simone/Art/Materials/`.
- **Actions performed:** Removed the `EvidenziatoreScenario[]` field/call from `BlackoutScenario.cs`. Created `SH_ContornoEvidenziazione.shader` (URP SRPDefaultUnlit, Cull Front, normal-expanded vertices), `MAT_ContornoEvidenziazione.mat` (#E69F00, width 0.02) and `EvidenziatoreContorno.cs`, which builds/destroys outline mesh children from assigned renderers. No scene, prefab, GasLeak or Home edits; no commit or push.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Art/Shaders/SH_ContornoEvidenziazione.shader` (new), `...shader.meta` (new), `Assets/_Project/Groups/G_Simone/Art/Materials/MAT_ContornoEvidenziazione.mat` (new), `...mat.meta` (new), `Assets/_Project/Groups/G_Simone/Code/Scripts/EvidenziatoreContorno.cs` (new), `...cs.meta` (new), `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs` (modified), `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** A shader-driven inverted hull gives an actual rim without changing the original materials, unlike `EvidenziatoreScenario`'s MaterialPropertyBlock tint. Keeping the new component separate avoids changing GasLeak, which uses the old highlighter.
- **What I learned:** URP draws untagged passes as SRPDefaultUnlit, but I tagged the outline pass explicitly; world-space normal expansion keeps outline width independent of object scale. Compile/Play was not verifiable in this session (no Unity MCP). User confirmed the result looked right ("all good").

### 2026-11-25 — Pending work for the next session
- **Request:** "note down the things we still have to do in the log, so in a next session we can work on it again". The user supplied the list below and chose to record only those seven items.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for this log.
- **Pending items (user's list):**
  1. Vignette effect (feedback only, no hearts; exclude coughing).
  2. Telephone interaction.
  3. Gas detector interaction.
  4. Electric panel (`QuadroElettricoInterattivo`) — the mesh is missing, to be revisited.
  5. Going out of the wrong door.
  6. Potential VFX (exclude coughing).
  7. The issue with the success doors (assigned in the Inspector, but grip produces no `afferrata` log / success screen).
- **Actions performed:** Appended this checklist to `AGENTS.md` only. No code, scene, prefab or material was changed; no git operations.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** Keeping an explicit pending list in the group's control document lets a future session resume without relying on chat history. The items are recorded verbatim from the user, without adding agent-inferred items, as requested.
- **What I learned:** The user treats `AGENTS.md` as the hand-off point between sessions; scope notes such as "mesh missing" and "exclude coughing" must be preserved with the task.

### 2026-11-25 — Push attempted and stopped
- **Request:** "can you push my edits following the guidelines in the main's readme?"
- **Directory confirmed by user:** G_Simone only for the commit; fetch + merge `origin/main` then commit and push; commit message `feat(blackout): update Blackout scenario and docs`.
- **Actions performed:** Read the root README and AGENTS.md; ran the `collab-proxy` check (clean); ran `git fetch origin` (approved). Merge of `origin/group_simone` was attempted but aborted: local `Assets/_Project/Home/Level/Scenes/SCN_Core.unity` edits would be overwritten. Inspection showed incoming commit `51af7ec` deletes `GameManager.cs`, AGENTS.md lines, model files and more. On user decision, stopped: no stage, commit, merge or push.
- **Files changed:** none. Working tree unchanged; branch ahead 4, behind 1 after fetch.
- **Thoughts / reasoning:** Pushing a destructive remote commit without resolution would lose current work; stashing out-of-scope Home files was not authorised.
- **What I learned:** `origin/group_simone` contains a large destructive commit that must be resolved by the team before integration; local Home edits block merging SCN_Core changes.

### 2026-11-26 — Realistic-development cleanup: highlighter, questions, alerts and success doors
- **Request:** Remove the Blackout highlighter and its related scripts; remove the decision questions (not in the GDD), editing only the Blackout UI script; comment out the serialized phone field for later; change the blackout alert body to "L'ufficio è al buio"; suppress suggestion texts outside Guided; switch the success-door trigger from grab to select; enlarge the success-door colliders. A later attempt to freeze the scene and reposition the outcome panel was reverted at the user's request. G_Simone only; no commits.
- **Directory confirmed by user:** Assets/_Project/Groups/G_Simone/Code/Scripts/ (script edits); Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/ (scene edit); highlighter assets under Art/Shaders/ and Art/Materials/.
- **Actions performed:** Deleted EvidenziatoreContorno.cs/.meta, SH_ContornoEvidenziazione.shader/.meta, the empty Art/Shaders.meta and MAT_ContornoEvidenziazione.mat/.meta; removed the contorni field, call and method from BlackoutScenario.cs and the 7 inactive components plus the contorni array from SCN_Blackout.unity. Removed the 4 AskDecision prompts, phone event handling and question-driven objective completion from BlackoutScenario.cs; kept timer, alert, door success and GDD wrong-action tracking. Left a commented-out serialized TelefonoInterattivo field. Changed the alert body to "L'ufficio è al buio". Gated the 2 Subtitle and 2 SetHint calls to Guided only. Changed the success-door trigger from GrabEntered to SelectEntered and renamed the related method, field, log and tooltip. Replaced the MeshCollider on porta_tagliafuoco_D1 and porta_D10 with non-trigger BoxColliders 1.5× larger in width and height, depth unchanged. An outcome-panel/time-freeze workaround was implemented and then fully reverted. Unity recompiles completed with no errors.
- **Files changed:** Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs; Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity; deleted highlighter script, shader, material and their .meta files; this log entry.
- **Thoughts / reasoning:** The highlighter was Guided-only and inert in the scene (empty renderer lists, attivo: 0), so removing it and its scene references was safe. The questions and the phone-ending path conflicted with the GDD (phone is neutral; the exit ends the level), so they were removed at the scenario call site rather than in shared UI code. Hints and subtitles are aid features reserved for Guided by the difficulty table; the event alert is not an aid and remains at all levels. A trigger collider was rejected because RayInteractor casts with QueryTriggerInteraction.Ignore and CharacterController does not collide with triggers, so a solid enlarged box was used instead. The outcome-screen workaround was reverted because it broke the game and is deferred.
- **What I learned:** AskDecision lives in Home/ScenarioController.cs and is shared with GasLeak and Mock, so only the Blackout script could be edited. The phone component works standalone in VR and raises its own events, so removing the scenario subscription did not disable it. Unity ignores removed serialized fields in a scene until the next save, so the stale telefono line is harmless. Both success doors have a 100× parent scale, which is why their tiny FBX mesh bounds translate to roughly 1–2 m doors.

### 2026-11-26 — Simplify Blackout phone to a two-select 112 call
- **Request:** Make the phone show "Seleziona di nuovo il telefono per chiamare il 112" on the first select, then "Il 112 risponde: dice di evacuare l'edificio e che i soccorsi stanno arrivando" on the second; no further interaction afterwards. Rebuild later.
- **Directory confirmed by user:** Assets/_Project/Groups/G_Simone/Code/Scripts/ and Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/.
- **Actions performed:** Created TelefonoChiamata112.cs/.meta with the two-step one-shot flow via VRMessageDisplay; re-enabled Interactable on MESH_Telefono, attached the new script, left TelefonoInterattivo and VRInteractableButton disabled. Saved the scene. Recompile completed with no errors. No commit.
- **Files changed:** Code/Scripts/TelefonoChiamata112.cs (new) and .meta; Assets/Level/Scenes/Blackout/SCN_Blackout.unity; this log entry.
- **Thoughts / reasoning:** A dedicated G_Simone script keeps the shared phone script and GasLeak unaffected. Durations of 4 s and 8 s were chosen as reasonable defaults.
- **What I learned:** VRMessageDisplay.GetOrCreate() supplies a runtime display with no scene instance, and ScenarioController.Subtitle is the text channel it uses.

### 2026-11-26 — Wrong action for selecting listed doors
- **Request:** Selecting any of 11 specified doors must count as a wrong action and remove points; do not change any existing Z-rotation behaviour, and make all listed doors selectable.
- **Directory confirmed by user:** Assets/_Project/Groups/G_Simone/Code/Scripts/ and Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/.
- **Actions performed:** Extended DoorResetOnInteract.cs with a serialized list of the 11 door names and the message "Non hai seguito la segnaletica". At Awake it resolves each door, adds an Interactable with FollowsHand=false where missing, and subscribes SelectEntered to BlackoutScenario.SegnalaAzioneErrata. No rotation logic was modified. Verified all names resolve uniquely and recompile completed with no errors. No commit.
- **Files changed:** Code/Scripts/DoorResetOnInteract.cs (modified); this log entry.
- **Thoughts / reasoning:** Reusing the existing door script avoided duplicate Interactable setup. Only SelectEntered is used so grab-based reset behaviour stays separate; errors are deduplicated by description, so the generic message counts once.
- **What I learned:** GameManager.DoorNameMatches resolves all 11 names uniquely, and SegnalaAzioneErrata is the public GDD wrong-action hook.

### 2026-11-26 — Gas detector selection is a wrong action
- **Request:** Selecting the gas detector must count as a wrong action and remove points, keeping the existing message wording.
- **Directory confirmed by user:** Assets/_Project/Groups/G_Simone/Code/Scripts/.
- **Actions performed:** In BlackoutScenario.cs, moved the existing gas-detector GDD error subscription from GrabEntered to SelectEntered, and updated the matching unsubscribe in OnDestroy. The user's existing select handler (subtitle message and 10 s disable coroutine) was left intact. Recompile completed with no errors. No commit.
- **Files changed:** Code/Scripts/BlackoutScenario.cs (modified); this log entry.
- **Thoughts / reasoning:** Reusing the existing GDD-specific message kept the penalty distinct from the generic door error; both handlers can coexist on SelectEntered.
- **What I learned:** The detector already had a select handler; only the scoring hook was still bound to grab.

### 2026-11-26 — Red vignette and full-screen tint on wrong actions
- **Request:** Show clear red feedback on every wrong action, including repeated interactions; feedback only, no hearts.
- **Directory confirmed by user:** Assets/_Project/Groups/G_Simone/Code/Scripts/.
- **Actions performed:** Added to BlackoutScenario.cs a lazily created global URP Volume/Vignette (intensity 0.85, 1 s fade) plus a runtime ScreenSpaceCamera red overlay (alpha 0.3, 1 s fade) that does not depend on post-processing. Both flash from RegistraErroreGDD before the IsRunning and deduplication checks, with a Console log for diagnosis. Enabled camera post-processing at runtime and restored it in OnDestroy. Verified in Play mode that both effects peak; recompile clean. No commit.
- **Files changed:** Code/Scripts/BlackoutScenario.cs (modified); this log entry.
- **Thoughts / reasoning:** Central feedback at the GDD error entry point covers doors, gas detector and panel; adding a UI overlay guarantees visibility even if URP post-processing is unavailable in the headset.
- **What I learned:** The shared rig has no post-processing by default, and a ScreenSpaceCamera canvas follows the head camera reliably in VR play mode.

### 2026-11-26 — Three mistakes fail the Realistic scenario
- **Request:** In Realistic only, fail the mission after 3 mistakes.
- **Directory confirmed by user:** Assets/_Project/Groups/G_Simone/Code/Scripts/.
- **Actions performed:** In BlackoutScenario.cs, after registering a new distinct GDD error, call ChiudiMissione() when Level is Operational and erroriGDD.Count >= 3. Guided and Stress are unchanged; repeated duplicate interactions still give visual feedback but do not count. Verified in Play mode that three distinct errors end the mission. Recompile clean. No commit.
- **Files changed:** Code/Scripts/BlackoutScenario.cs (modified); this log entry.
- **Thoughts / reasoning:** Using the existing error list keeps the failure threshold consistent with scoring, and reusing ChiudiMissione() preserves the existing failure screen.
- **What I learned:** The difficulty level is available directly via the scenario's Level property when an error is registered.

### 2026-11-26 — Three wrong interactions fail Realistic, armed at blackout
- **Request:** Repeated wrong actions must count toward the 3-mistake failure; mistakes must not exist before the blackout crisis starts; keep scoring unchanged after blackout.
- **Directory confirmed by user:** Assets/_Project/Groups/G_Simone/Code/Scripts/.
- **Actions performed:** Added interazioniErrate counter and blackoutAttivo flag to BlackoutScenario.cs. RegistraErroreGDD returns while blackout is inactive; once active, every wrong interaction increments the counter, while erroriGDD stays deduplicated for scoring/debrief. Failure at 3 is Realistic-only and uses ChiudiMissione(). Counter and flag reset at scenario start. Verified in Play mode that three identical wrong interactions end the mission. Recompile clean. No commit.
- **Files changed:** Code/Scripts/BlackoutScenario.cs (modified); this log entry.
- **Thoughts / reasoning:** Separating the failure counter from the deduplicated error list gives 3-strike behaviour without changing score penalties; gating on OnBlackout matches the GDD, where correct actions only matter once the crisis begins.
- **What I learned:** Editor logs showed repeated wrong-door selections shared one message, so the earlier distinct-error count never reached 3; HandleBlackout is the central moment to arm the mistake system.

### 2026-11-26 — Empty negative criterion in Blackout mission
- **Request:** Remove the negative-criterion text shown on the outcome/brief screens.
- **Directory confirmed by user:** Assets/_Project/Home/UI/Data/Missions/ (explicitly authorized, outside G_Simone).
- **Actions performed:** Set the Text of the Outcome: 2 criterion in SO_Mission_Blackout.asset to empty. The criterion entry itself was kept so the three-criteria structure remains valid. No commit.
- **Files changed:** Assets/_Project/Home/UI/Data/Missions/SO_Mission_Blackout.asset (modified); this log entry.
- **Thoughts / reasoning:** Deleting the whole entry would break MissionBriefScreen's three rows and the validator's three-criteria requirement.
- **What I learned:** The criterion text is shared by all difficulties of Blackout.

### 2026-11-26 — Success doors rotate on selection
- **Request:** On selection, porta_D10 should rotate Z 0→-90 and porta_tagliafuoco_D1 should rotate Z 90→0, once.
- **Directory confirmed by user:** Assets/_Project/Groups/G_Simone/Code/Scripts/ and Assets/Level/Scenes/Blackout/.
- **Actions performed:** Added a oneShot option to DoorToggleRule and to the toggle animation; added the two success-door rules with oneShot=true, smooth 0.6 s and SelectEntered trigger. Set the four rules explicitly on the DoorResetOnInteract component in SCN_Blackout.unity and saved. D15/D16 rules unchanged. Recompile clean. No commit.
- **Files changed:** Code/Scripts/DoorResetOnInteract.cs (modified); Assets/Level/Scenes/Blackout/SCN_Blackout.unity (modified); this log entry.
- **Thoughts / reasoning:** Reusing the existing rule system keeps animation and name resolution consistent; one-shot prevents further interaction after the mission-ending selection.
- **What I learned:** Unity keeps old serialized arrays and does not apply new initializer defaults, so scene data had to be updated explicitly.

### 2026-11-26 — Hide Blackout objectives lists outside Guided (shared UI)
- **Request:** "I want to work on the 'procedimento corretto' list in the ui code. The list is all wrong, and it only needs to show in the guided level. remove the whole thing for realistic and stress levels, we'll fix it for the guided later." Follow-ups: all four UI locations (debrief "Percorso corretto", wrist objectives, Brief and Pause objectives); Blackout only; use the selected level in Brief. The earlier read-only document request was explicitly not logged.
- **Directory confirmed by user:** `Assets/_Project/Home/Code/Scripts/Runtime/Screens/` (`DebriefScreen.cs`, `MissionBriefScreen.cs`, `PauseScreen.cs`) and `Assets/_Project/Home/Code/Scripts/Runtime/HUD/` (`WristPanel.cs`). Outside `G_Simone` by explicit instruction.
- **Actions performed:** Ran the read-only activation check. Located the "Percorso corretto" list and the generic placeholder objectives in `SO_Mission_Blackout.asset`. Added a local `ID_BLACKOUT = "02"` gate in each file: Debrief hides the whole side panel using `result.Level`; Brief hides objective rows using `UINavigationContext.SelectedLevel` and updates them on level-card click; Pause hides title and rows using `MissionSession.Level`; WristPanel hides objective row and counter in `ApplyLevel` using `MissionSession.Current`/`cfg.Level`. Unity recompile completed with no errors. No git state-changing commands.
- **Files changed:** `Assets/_Project/Home/Code/Scripts/Runtime/Screens/DebriefScreen.cs`; `MissionBriefScreen.cs`; `PauseScreen.cs`; `Assets/_Project/Home/Code/Scripts/Runtime/HUD/WristPanel.cs`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** The incorrect text is scenario data, not UI code, so a temporary level gate is safer than rewriting objectives now. Gating by mission id keeps other groups' scenarios unchanged. Guided keeps the current list until the user fixes it later.
- **What I learned:** Brief reads the chosen level from `UINavigationContext.SelectedLevel` before launch, Pause and HUD read `MissionSession.Level`, Debrief reads `MissionResult.Level`. The Pause objective title is a sibling of `PANEL_Objectives` and is found by name at runtime. `MissionSession` is in the `SI81.UI` namespace.

### 2026-11-26 — Reduce "procedimento corretto" to "Evacua l'edificio"
- **Request:** "take whatever actions the script deems 'percorso corretto' and change them to 'Evacua l'edificio'". Follow-up: apply to all three targets — the Guided objectives list, the success/scoring logic, and the debrief text. The previous log entry was approved as drafted and appended.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/` (log), `Assets/_Project/Groups/G_Simone/Code/Scripts/` (`BlackoutScenario.cs`), `Assets/_Project/Home/UI/Data/Missions/` (`SO_Mission_Blackout.asset`).
- **Actions performed:** Appended the approved "Hide Blackout objectives lists outside Guided" entry. Replaced the four generic objectives in `SO_Mission_Blackout.asset` with one item, `Evacua l'edificio`. In `HandlePortaSelezionata` removed the `erroriGDD.Count == 0` gate and the partial-success branch: selecting a success door now always ends with Positive, score 100 and five 100 dimensions, adding "Evacua l'edificio: successo completo"; wrong actions remain as BAD notes. In `CostruisciDebrief` replaced "→ non hai seguito il procedimento corretto" with "→ Evacua l'edificio". Unity recompile reported up_to_date with no errors. No git state-changing commands.
- **Files changed:** `Assets/_Project/Home/UI/Data/Missions/SO_Mission_Blackout.asset`; `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (previous entry appended; this entry pending approval).
- **Thoughts / reasoning:** The single objective and the success rule now agree: evacuation is the only action that defines the correct path. Keeping the GDD error tracking preserves useful debrief information while removing its effect on scoring, as requested.
- **What I learned:** `BlackoutScenario.cs` had changed since the earlier read (new phone/gas-detector handling), so edits were verified by content. All four UI lists read the same `SO_Mission`, so changing its Objectives updates Brief, Pause, wrist and Debrief together; the level gate prevents that from reappearing outside Guided.

### 2026-11-26 — Segnaletica luminosa: green base, emissive only on blackout
- **Request:** "make it so segnaleticaluminosa is emissive only when blackout event triggers"; follow-up: "I want the material to be the same green before blackout, maybe a little duller, but not emissive".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` (`GameManager.cs`) and `Assets/_Project/Groups/G_Simone/Art/Materials/` (`MAT_SegnaleticaLuminosa.mat`). No scene or prefab edits.
- **Actions performed:** Ran the read-only activation check. Inspected the material (URP/Lit, `_EMISSION`, green `_EmissionColor`) and the prefab override assigning it to the child renderer. In `GameManager` added a cached `_EmissionColor` property id and material-name constant; `Start()` forces signage emission to black and subscribes `AccendiEmissioneSegnaletica` to `OnBlackout`; `OnDestroy()` unsubscribes. `ImpostaEmissioneSegnaletica(bool)` uses a `MaterialPropertyBlock` on matching single-material renderers to switch between black and the material's own green. Changed the material's `_BaseColor`/`_Color` from near-black `(0.01, 0.01, 0.01)` to a duller green `(0.16, 0.7, 0)`, leaving `_EmissionColor` at `(0.2235, 1, 0)`. Unity recompile completed with no errors. No git state-changing commands.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/GameManager.cs`; `Assets/_Project/Groups/G_Simone/Art/Materials/MAT_SegnaleticaLuminosa.mat`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry, pending approval).
- **Thoughts / reasoning:** A runtime property block keeps the shared material asset untouched and gates the glow to the blackout event. A green albedo makes the signage visible in normal light while staying non-emissive; the duller base preserves contrast with the brighter emissive green at blackout.
- **What I learned:** URP/Lit's albedo and emission are independent, so emission-off alone could not fix a near-black base. The prefab overrides the first material slot with this asset, so the base-color change applies to every signage instance. `Material.GetColor(int)` works with `Shader.PropertyToID`.

### 2026-11-26 — Rilevatore CO: sottotitolo su selezione, disattivazione dopo 10 s
- **Request:** "do not write this specific thing in the log, do log what comes after this. I want to work on rilevatore gas. I want it to be interactable. when selected, I want the text to say 'I livelli di CO sono troppo alti' and end the interaction after 10s." Follow-ups: HUD subtitle; one-shot then disable; add to `BlackoutScenario.cs`; do not log the earlier read-only document request. Later: "adapt this task to the new changes" → "check the changes that have been made and redo the task with these new edits"; final choice was to verify and keep the current code rather than rebuild it as a standalone script.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` for the code; `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for this log.
- **Actions performed:** Ran the read-only activation check (`group_simone` ahead 4 / behind 1). Reused the `Interactable` that `PreparaAzioniErrate()` already creates at runtime on `RilevatoreDiGas`; subscribed `SelectEntered`; added title `RILEVATORE CO`, text `I livelli di CO sono troppo alti` and a 10 s duration. On select, called inherited `Subtitle(...)` and started a coroutine that sets `Selectable = false` and `Grabbable = false` after 10 s; repeated selects during the message are ignored. Added `RiattivaRilevatore()` in `OnScenarioStarted()` and coroutine/handler cleanup in `OnDestroy()`. Unity recompile completed with no errors and reflection confirmed the three new methods. Afterwards other local edits appeared (phone script and AGENTS entries, shared UI level gating, `SO_Mission_Blackout.asset`, always-full-score success logic); reviewed them with read-only git diffs and verified the gas-detector code was intact and still compiled (`up_to_date`, no errors). User chose to keep the existing implementation. No scene, prefab, Shared or Home edits by me; no git state-changing commands and no commit.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs` (modified in this task); `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** A select handler on the existing runtime `Interactable` avoids editing `Struttura.prefab` or the scene. Using inherited `Subtitle` keeps the text on the common HUD, matching the phone flow. Disabling both select and grab makes "end the interaction" explicit, and the mission-start reset keeps the object usable on replay. No extra GDD wrong-action tracking was added because it was not requested. When concurrent edits appeared, rebuilding as a standalone component would have duplicated the phone pattern without fixing a problem: the new always-full-score logic makes GDD errors debrief notes only, so disabling the detector after 10 s cannot affect scoring.
- **What I learned:** `Interactable.SelectEntered` is the trigger-select event and `Selectable`/`Grabbable` are writable at runtime; `Subtitle` forwards to `HUDService.ShowSubtitle` without an `IsRunning` guard. The first recompile status was stale (`compilationFailed: true`) and a later poll reported clean. The new phone flow in `TelefonoChiamata112.cs` uses the same `VRMessageDisplay`/`ScenarioController.Subtitle` channel. `SCN_Blackout.unity` gained the phone `Interactable`/script but the detector name and setup are unchanged; the Home and mission-asset changes are outside G_Simone and were left untouched.

### 2026-11-26 — Bug report: no success screen and pause not working in Editor + Quest Link
- **Request:** "I don't have a success screen anymore, nor I can pause the game." Follow-ups: neither the outcome panel nor the Debrief screen appears; pause was tried with the VR controller menu button; testing in Unity Editor with Quest Link; no edits outside `G_Simone`; the user later confirmed the bug still happens and approved logging the diagnosis.
- **Directory confirmed by user:** `G_Simone` only; log under `Assets/_Project/Groups/G_Simone/Docs/Blackout/`.
- **Actions performed:** Ran the read-only activation check. Traced the success flow (`BlackoutScenario.HandlePortaSelezionata` → `EndMission` → `MissionSession.End` → `MissionFlowDriver.EndSequence` → `UIScreenManager.ShowRoot(Debrief)`) and the pause flow (XR left-menu button → `InputService.MenuPerformed` → `MissionFlowDriver` → `MissionFlow.TogglePause`). Entered Play Mode, started a Blackout mission with `MissionSession.Begin`, loaded `SCN_Blackout` additively and confirmed 18 Interactables were created, including D1, D10 and `RilevatoreDiGas`, all enabled. `MissionFlow.TogglePause()` worked (`IsPaused = true`, `timeScale = 0`); calling `EndMission(Positive, 100, …)` directly showed the Debrief screen after ~4 s. Found zero `IOutcomePresenter` components, so the short outcome panel cannot appear. `InputService` in this Editor session had `menuAction = World/Menu[/Keyboard/tab]` and no XR controllers (`leftController`/`rightController` null), so controller select/menu cannot fire here. Stopped Play Mode. No source files changed.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry). No code, scene, prefab or Home file was edited by me.
- **Thoughts / reasoning:** Both symptoms are in the shared input/UI layer, not in the Blackout scenario: the G_Simone trigger side is intact and both flows complete when invoked directly. Because the user restricted changes to `G_Simone`, a local workaround would duplicate Home logic and still could not restore Debrief or the XR input scheme; the useful deliverable is this precise hand-off note.
- **What I learned:** `MissionFlow.CanPause` requires an active mission, no result, no outcome showing and a `UIScreenManager`; `MissionFlowDriver` skips the outcome panel when no `IOutcomePresenter` is found but still shows Debrief after ~4 s. In Play Mode `InputService` used the Editor scheme (Tab for menu) with no XR controllers, which explains why a Quest Link controller cannot select the success doors or pause. The fix requires Home/input or Editor XR setup, outside the allowed scope.

### 2026-11-26 — Restore backed-up "Evacua l'edificio" UI edits after clean-scene test
- **Request:** "the issue has been fixed. I now need to remove the 'procedimento corretto' actions that are inside the ui." Earlier in the same task: the user asked to back up and `git restore` `SCN_Core` plus its modified Home references to test a clean scene; if that did not help, to avoid changing `SCN_Core`. Follow-up: restore option 1 but only those edits, then clarified to include the UI gating; logging approved.
- **Directory confirmed by user:** `Assets/_Project/Home/UI/Data/Missions/` and `Assets/_Project/Home/Code/Scripts/Runtime/{Screens,HUD}/`; earlier `Assets/_Project/Home/` for the backup and restore; `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for this log.
- **Actions performed:** Backed up eight modified Home files to `%LOCALAPPDATA%\Temp\opencode\home-backup-20261126\`, then ran `git restore` on all eight. Tested in Play Mode: the clean `SCN_Core` did not fix the reported symptoms — Debrief still only appeared when `EndMission` was invoked directly, `IOutcomePresenter` count remained zero, and `MissionFlow.TogglePause()` still worked when invoked. Stopped Play Mode. The user then reported the issue fixed and asked to remove the wrong "procedimento corretto" actions from the UI; copied five files back from the backup (`SO_Mission_Blackout.asset`, `DebriefScreen.cs`, `MissionBriefScreen.cs`, `PauseScreen.cs`, `WristPanel.cs`), verified each with a file-hash comparison, and confirmed the asset now has the single objective `Evacua l'edificio`. Unity recompile completed with no errors. `SCN_Core.unity`, `MNG_CoreRig.prefab` and `SCN_MockScenario.unity` were left at the restored HEAD state.
- **Files changed:** `Assets/_Project/Home/UI/Data/Missions/SO_Mission_Blackout.asset`; `Assets/_Project/Home/Code/Scripts/Runtime/Screens/DebriefScreen.cs`; `MissionBriefScreen.cs`; `PauseScreen.cs`; `Assets/_Project/Home/Code/Scripts/Runtime/HUD/WristPanel.cs`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** Restoring the exact backed-up files was safer than re-editing code from memory, and hash verification guaranteed the intended edits came back. Keeping `SCN_Core` at HEAD respected the user's request not to create breaking changes in other worktrees. The level gate hides the temporary objective lists outside Guided while the single correct objective replaces the four wrong actions everywhere.
- **What I learned:** The backup retained both the single objective and the four UI level gates; all four lists (Brief, Pause, wrist, Debrief) read the same `SO_Mission`, so the asset edit propagates to each. The clean-scene test proved the earlier UI edits were not the cause of the success/pause problem.

### 2026-11-26 — Light blue material for windows
- **Request:** "may you add a light blue material to all the windows"
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Materials/` for the material; `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/` for the scene.
- **Actions performed:** Found 6 `finestra_*` groups with 18 mesh renderers in `SCN_Blackout`, all using the read-only default URP "Lit" material from Packages. Created `MAT_Finestra_LightBlue.mat` (URP/Lit, `_BaseColor` #ADD8E6) and assigned it to all 18 renderers. Saved the scene.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Art/Materials/MAT_Finestra_LightBlue.mat` (new) and its `.meta`; `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity`.
- **Thoughts / reasoning:** Created a new asset instead of editing the shared default "Lit" material, which is used by other objects and cannot be edited in place. This broad first pass was narrowed by the next request.
- **What I learned:** The scene's windows are named in Italian (`finestra_*`); the default "Lit" material lives in `Packages/com.unity.render-pipelines.universal/Runtime/Materials/` and is read-only.

### 2026-11-26 — Blue only on glass panes, frames get MAT_CabinetBody
- **Request:** "only give the blue to the glass panes, give MAT_CabinetBody to the rest of the window components"
- **Directory confirmed by user:** same as above; `MAT_CabinetBody` was only referenced from `Assets/_Project/Shared/Art/Materials/` and was not modified.
- **Actions performed:** Measured the local bounds of all 18 window meshes: the two `finestra_anta_*` meshes per window are flat surfaces (~0.001 thick) and are the glass panes; `finestra_telaio_*` is a solid frame. Kept `MAT_Finestra_LightBlue` on the 12 panes and assigned `MAT_CabinetBody` to the 6 frames. Saved the scene.
- **Files changed:** `SCN_Blackout.unity`.
- **Thoughts / reasoning:** Geometry was the only reliable way to distinguish panes from frames — no object is named "glass"/"vetro" and each mesh has a single material slot, so a submesh split was not possible.
- **What I learned:** `MAT_CabinetBody.mat` exists in Shared; referencing a Shared material from a scene does not modify the Shared asset. The initial "all windows blue" assignment was therefore corrected in place.

### 2026-11-26 — Read the group AGENTS.md
- **Request:** "read agents.md in g_simone"
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Docs/Blackout/` for this log.
- **Actions performed:** Read the full `AGENTS.md` (385 lines) and the root agent rules supplied by the user; ran the read-only activation repository check (branch `group_simone`, ahead 4 / behind 1, 9 modified and 10 untracked files). Appended the two window-material entries that had not been logged at the time of the work. No git state-changing commands and no Unity VCS tooling.
- **Files changed:** `AGENTS.md` (three entries).
- **Thoughts / reasoning:** The file is the group's control document; entries are proposed in chat and approved before being written, so the two earlier tasks are now recorded retroactively rather than silently.
- **What I learned:** The log ended at the "Restore backed-up UI edits" entry; the window work from this session was missing. All git observations were local and read-only, as required.

### 2026-11-26 — MAT_CabinetBody on all scaffale components
- **Request:** "now add MAT_CabinetBody to all scaffale components"
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/` for the scene edit; `MAT_CabinetBody` only referenced from `Assets/_Project/Shared/Art/Materials/`, not modified.
- **Actions performed:** Found 9 `Scaffale` objects under `/Struttura` (`Scaffale`, `Scaffale (1)` … `Scaffale (8)`), each a single mesh (`Cube.017`) with one material slot, all previously using `Salotto`. Assigned `MAT_CabinetBody` to all 9 mesh renderers, saved the scene and verified 9/9.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** All nine shelves share the same mesh and material, so assigning the material per renderer is a direct, reversible scene override. Nothing in `Shared` was edited.
- **What I learned:** The shelves are separate scene objects, not prefab instances with nested parts; `Salotto` was the current override on all of them.

### 2026-11-26 — Black material on all base banco components
- **Request:** "after this add a black material to all basebanco components"
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Materials/` for the new material; `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/` for the scene. Chosen material `MAT_BaseBanco_Black`, pure black `#000000`, applied to all 14 `base banco` objects only (not the `Cube` tops).
- **Actions performed:** Found 14 `base banco` renderers under `/Struttura/MESH_Banco*`, all using the read-only default URP "Lit" material. No project black material existed (only package-internal scene-template assets), so created `MAT_BaseBanco_Black.mat` (URP/Lit, `_BaseColor` `(0,0,0,1)`) with its `.meta`, assigned it to all 14, saved the scene and verified 14/14.
- **Files changed:** `Assets/_Project/Groups/G_Simone/Art/Materials/MAT_BaseBanco_Black.mat` (new) and its `.meta`; `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/SCN_Blackout.unity`; `Assets/_Project/Groups/G_Simone/Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** A dedicated G_Simone material avoids depending on editor-only package assets; a per-renderer scene override is direct and reversible. Scope kept to the `base banco` meshes as confirmed.
- **What I learned:** The `base banco` meshes were all using the default URP "Lit" material; the only black materials in the project are internal to `com.unity.render-pipelines.universal/Editor/SceneTemplates/`.

### 2026-11-26 — Cyan material on all Puf components
- **Request:** "now add a cyan material to all puf components"
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Materials/` for the new material; `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/` for the scene. Chosen material `MAT_Puf_Cyan`, pure cyan `#00FFFF`, applied to all 39 Puf renderers.
- **Actions performed:** Found 39 `Puf` renderers under `/Struttura` (`Puf`, `Puf (1)` … `Puf (39)`), all sharing mesh `Cylinder.005` and the read-only default URP "Lit" material. No cyan material existed, so created `MAT_Puf_Cyan.mat` (URP/Lit, `_BaseColor` `(0,1,1,1)`) with its `.meta`, assigned it to all 39, saved the scene and verified 39/39.
- **Files changed:** `Art/Materials/MAT_Puf_Cyan.mat` (new) and its `.meta`; `SCN_Blackout.unity`; `AGENTS.md` (this entry).
- **Thoughts / reasoning:** A dedicated G_Simone material keeps the change in scope and avoids touching Shared; the per-renderer override remains reversible in the scene.
- **What I learned:** All Puf instances share one imported mesh and were using the default URP "Lit" material; only the earlier `MAT_Finestra_LightBlue` had any blue/cyan tint in the project.

### 2026-11-26 — Light grey material on stanze floor
- **Request:** "can you add a light grey material to stanze"
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Materials/` for the new material; `Assets/_Project/Groups/G_Simone/Assets/Level/Scenes/Blackout/` for the scene. User chose `MAT_Stanze_LightGrey` `#D3D3D3` and **only slot 0 (floor)**, leaving the walls slot unchanged.
- **Actions performed:** `stanze` is one mesh with 2 submeshes / 2 material slots (`Material.004` floor, `Material.005` walls). Created `MAT_Stanze_LightGrey.mat` (URP/Lit, `_BaseColor` `(0.827,0.827,0.827,1)`) with its `.meta`, replaced only `sharedMaterials[0]`, saved the scene and verified slot 0 = `MAT_Stanze_LightGrey`, slot 1 = `Material.005`.
- **Files changed:** `Art/Materials/MAT_Stanze_LightGrey.mat` (new) and its `.meta`; `SCN_Blackout.unity`; `AGENTS.md` (this entry).
- **Thoughts / reasoning:** Assigning the array by index preserved the walls material instead of overwriting both slots; a dedicated in-scope material keeps Shared untouched.
- **What I learned:** `Renderer.sharedMaterials` must be edited as an array to target a single submesh slot; assigning `sharedMaterial` would not make the intended two-slot distinction explicit.

### 2026-11-26 — Fire VFX: moderate in Realistico, intense in Stress
- **Request:** "use FireLocation and FireLocation (1) as location references. I need VFX that look/feel like fire. they're moderate and only for realistic level"; follow-up: "now make intense VFX (enough to make it a bit hard to see) only for stress level"; final tweak: "do not make it a column, make the particles expand a bit". Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` — same script, no scene or Home edits; dense local smoke only, no full-screen veil.
- **Actions performed:** Read-only activation check showed the already-modified `SCN_Blackout.unity` with the two FireLocation objects and untracked audio; left untouched. Confirmed the transforms are children of `Struttura` (scale 100) and that levels map to `MissionSession.Level` (Realistic = Operational, Stress = Crisis). Created and extended `BlackoutFireVFX.cs` (+ `.meta`, GUID `d84c6f1b93a74e2fbd5a0c8e172b6f39`) with two variants: moderate (20/s flame, 5/s smoke, light 1.0) for Operational, intense (55/s flame, 26/s smoke up to 140 particles, light 2.2) for Crisis; Guided creates none. Runtime soft texture and URP Particles/Unlit additive/alpha materials are shared; parent scale is compensated with local scale 0.01; VFX are children of the reference objects. Per the user's correction the intense smoke uses a 55° cone, low speed/gravity, stronger sideways noise and a growing size curve (0.7 → 1.15 → 1.8) so it expands instead of forming a column. Recompile clean. Play Mode: Crisis produced the intense VFX at both points (rates 55/26, cone 55°, size max 2.2, expanding curve, flickering light); Guided produced none. Play Mode stopped, scene not dirty. No commits.
- **Files changed:** `Code/Scripts/BlackoutFireVFX.cs` (new/extended); `Code/Scripts/BlackoutFireVFX.cs.meta` (new); `Docs/Blackout/AGENTS.md` (this entry).
- **Thoughts / reasoning:** A single self-bootstrapping script keeps the whole feature in G_Simone and avoids touching the already-modified scene. Runtime materials/textures avoid new Shared assets; scale compensation keeps sizes in metres. Gating by `MissionSession.Level` at build time keeps the levels visually distinct without duplicate scene objects.
- **What I learned:** `AfterSceneLoad` can precede a standalone Editor session, so the script waits for `MissionSession.IsActive`. URP Particles/Unlit needs explicit `_SrcBlend`/`_DstBlend` plus `_SURFACE_TYPE_TRANSPARENT` for additive vs alpha. A wide cone plus a size-over-lifetime growth curve and low upward force is what makes smoke read as billowing rather than a column.

### 2026-11-26 — SFX_Burning: Realistico a 0.5, Stress a 0.85
- **Request:** "now in realistic scenario insert SFX_Burning at a moderate volume"; follow-up: "now implement that same sound in the same manner but with a higher volume" → "for stress level only". Entry approved by the user.
- **Directory confirmed by user:** `Code/Scripts/GameManager.cs` and `Art/Prefab/Struttura.prefab` for the first task; the follow-up changed only `GameManager.cs`.
- **Actions performed:** Read-only activation check showed the existing user modifications to the scene, prefab, docs, Home mission asset and untracked audio; left them untouched. Added a serialized `burningSound` clip and a dedicated looping 2D `AudioSource` to `GameManager`, subscribed to `OnBlackout` and to `MissionSession.Ended`, with all subscriptions removed in `OnDestroy`. Assigned `SFX_Burning.wav` (GUID `2628dc849ee855f4facc9ebab95b5a5e`) on `---GameManager---` in `Struttura.prefab`. Initially gated to `Operational` at volume 0.5; then extended the same method so `Crisis` uses volume 0.85 while `Guided` stays silent. Recompile clean. Play Mode: Guided not playing, Operational playing at 0.50, Crisis playing at 0.85, both looping; mission end stopped playback. Scene not dirty, no commits.
- **Files changed:** `Code/Scripts/GameManager.cs`; `Art/Prefab/Struttura.prefab`.
- **Thoughts / reasoning:** A separate AudioSource keeps the burning loop independent from the fire-alarm placeholder; reusing one source with a level-dependent volume avoids duplicate audio paths while keeping the two difficulty tiers distinct.
- **What I learned:** AudioClip references in a prefab must be assigned on the prefab asset component (`---GameManager---` child); the scene instance inherits the new field. A single AudioSource can safely switch volume per difficulty because only one level is active per mission.

### 2026-11-26 — Uno sbaglio fallisce lo Stress; errori registrati a tutti i livelli
- **Request:** "now make it so one mistake fails the scenario in stress level"; clarification: "the mistakes are universal for all levels, in case you only registered them for realistic".
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs`.
- **Actions performed:** Read-only activation check showed the existing user modifications to scene, prefab, docs, Home mission asset and untracked audio; left them untouched. Confirmed `RegistraErroreGDD` already registers mistakes for every level after blackout (`erroriGDD`, `interazioniErrate`, red vignette) and only the failure check was gated to Realistic. Extended the condition: Operational still fails at 3 wrong interactions, Crisis fails at 1, Guided has no threshold; the log message now reports the level. Recompile clean. Play Mode with counters reset: Crisis ended after 1 mistake; Operational after 1 and 2 was still running and ended at 3; Guided was still running after 3. Scene not dirty, no commits.
- **Files changed:** `Code/Scripts/BlackoutScenario.cs`.
- **Thoughts / reasoning:** Keeping one shared registration path with per-level thresholds preserves the same feedback and debrief data at every difficulty and avoids duplicate error hooks.
- **What I learned:** `interazioniErrate` is reset only by `OnScenarioStarted`, so repeated manual tests must reset it explicitly; vignette feedback also fires for duplicate descriptions while only distinct ones are added to `erroriGDD`.

### 2026-11-26 — Tosse da immobilità nello Stress, senza danno
- **Request:** "implement a mechanic where every 40s the player is still, SFX_Cough plays non-looped and the red vignette effect happens, without taking actual damage". Entry approved by the user.
- **Directory confirmed by user:** `Code/Scripts/BlackoutScenario.cs` and `SCN_Blackout.unity` (scenario + scene chosen; Stress only; movement of the rig inside the scene).
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. Added a serialized `suonoTosse` clip and a runtime non-looping 2D AudioSource (volume 0.7) to `BlackoutScenario`. `Update()` runs only while the mission is active in Crisis: it reads `RigRegistration.Instance` head and hands, resets the stillness timer when any moves more than 0.25 m, and after 40 s calls `PlayOneShot` on the cough clip plus the existing `MostraVignettaErrore()` — feedback only, no damage or score change — then resets the timer. State is reset in `OnScenarioStarted()` and the source destroyed in `OnDestroy()`. Assigned `SFX_Cough.mp3` (GUID `b66d583cd37ea48418806173977f2af9`) on `MNG_Scenario` in `SCN_Blackout.unity`. Recompile clean. Play Mode with an injected test rig: after 41 s still the cough played (loop false, volume 0.7), vignette and routine were created and the mission stayed running; moving the rig 2 m reset the timer; Operational did not trigger. Scene saved and not dirty, no commits.
- **Files changed:** `Code/Scripts/BlackoutScenario.cs`; `Assets/Level/Scenes/Blackout/SCN_Blackout.unity`.
- **Thoughts / reasoning:** Reusing the existing red-vignette helper gives the required feedback without adding hearts or changing scoring; rig movement as the reset signal matches "only when the player rig moves inside the scene".
- **What I learned:** In a standalone Editor Play of SCN_Blackout there is no registered rig, so a temporary rig was needed for verification; `AudioSource.PlayOneShot` does not set `AudioSource.clip`, which is why playback checks must use `isPlaying`.

### 2026-11-26 — Timer dello Stress a 90 secondi
- **Request:** "now change the timer in stress level to 1.5 minutes max". Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs`.
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. In `AvviaTimerScenario()` changed the Crisis branch from 120 to 90 seconds and updated the doc comment; Guided (0, stopwatch) and Operational (180) unchanged. Recompile clean. A Play Mode attempt to read `HUDService.RemainingSeconds` was inconclusive because the standalone scene has no HUD root (`IsReady = false`), so the value was verified by code inspection. Scene not dirty, no commits.
- **Files changed:** `Code/Scripts/BlackoutScenario.cs`.
- **Thoughts / reasoning:** A single switch value keeps the difficulty timers explicit and leaves the other levels untouched.
- **What I learned:** In a standalone Play of SCN_Blackout, HUDService has no root, so timer verification requires either launching from SCN_Core or inspecting the code path.

### 2026-11-26 — Fire VFX ora appaiono dal lancio via SCN_Core
- **Request:** "vfx doesn't appear" — fire particles not showing when launching Blackout from the UI.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutFireVFX.cs`.
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. Reproduced the exact SCN_Core → launcher → additively loaded SCN_Blackout path: no VFX were created, while manually invoking the bootstrap immediately created both. Confirmed `inAttesa` was false and identified the root cause: `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` runs only for the first scene load (SCN_Core), not for the additive Blackout load. Replaced it with a `SceneManager.sceneLoaded` subscription registered at `SubsystemRegistration`, with statics reset there. Recompile clean. Retested: SCN_Core → launch Operational created moderate VFX at both points; ReturnToHome → launch Crisis created the intense variant at both points. Restored SCN_Blackout as the open scene, not dirty. Opening SCN_Core in Play regenerated 19 OpenXR feature fileIDs in `Assets/XR/Settings/OpenXR Package Settings.asset`; left untouched pending the user's decision. No commits.
- **Files changed:** `Code/Scripts/BlackoutFireVFX.cs`.
- **Thoughts / reasoning:** A per-scene callback is required because the scenario scene is loaded additively after the session begins; the bootstrap still avoids duplicates and reads the level from the session first, UI fallback second.
- **What I learned:** `RuntimeInitializeLoadType.AfterSceneLoad` fires only for the first scene load in this project; `SceneManager.sceneLoaded` is the reliable hook for additive scenario loads.

### 2026-11-26 — Shader del contorno: espansione in spazio mondo
- **Request:** "the highlighter shader is broken. the way it was made before was good, can you fix it so it looks like how it was originally please?"
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Art/Shaders/SH_ContornoEvidenziazione.shader` only.
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. Inspected the shader: it compiled and was supported, but the vertex offset was applied in object space, so the 0.02 width was scaled by the ×100 hierarchy of the outlined meshes. Restored the original behaviour: world-space position/normal expansion (`TransformObjectToWorld`, `TransformObjectToWorldNormal`, `TransformWorldToHClip`) and an explicit `LightMode = SRPDefaultUnlit` tag; colour and width unchanged. Forced a reimport; shader supported, one pass, material still bound. The OpenXR feature-fileID churn reappeared during import and was restored again under the user's earlier approval. No commits.
- **Files changed:** `Art/Shaders/SH_ContornoEvidenziazione.shader`.
- **Thoughts / reasoning:** World-space expansion keeps the outline width in metres regardless of parent scale, matching the original implementation described in the earlier log entry.
- **What I learned:** A shader can be reported as supported yet render incorrectly; with scaled meshes, object-space normal expansion multiplies the intended width.

### 2026-11-26 — Sottotitoli Caposquadra rimossi dal Guidato
- **Request:** "now remove the caposquadra ui in guided mode"; clarified: remove only the Caposquadra subtitles and keep the hints. Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs`.
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. Removed the `Subtitle("Caposquadra", ...)` calls from the Guided branches of `OnScenarioStarted()` and `HandleBlackout()`, keeping the two `SetHint(...)` lines. Recompile clean; no `Caposquadra` references remain in the script. No commits.
- **Files changed:** `Code/Scripts/BlackoutScenario.cs`.
- **Thoughts / reasoning:** Removing only the subtitles preserves the Guided hint feature while eliminating the Caposquadra voice UI the user asked to drop.
- **What I learned:** The Caposquadra text was emitted only by `BlackoutScenario`; the shared HUD `SetHint` channel is separate and unaffected.

### 2026-11-26 — Obiettivo segnaletica avanza selezionando D3
- **Request:** "in mechanics where triggers change at what stage you are, the change between 'Segui la segnaletica luminosa' and 'Evacua l'edificio' happens when porta_tagliafuoco_D3 gets selected". Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs`.
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. Added a runtime `SelectEntered` subscription on `porta_tagliafuoco_D3` (resolved with `GameManager.DoorNameMatches`, adding an `Interactable` if absent). The one-shot handler calls `CompleteObjective()`, is reset in `OnScenarioStarted()` and unsubscribed in `OnDestroy()`. Recompile clean. Play Mode: 2 objectives and 1 handler registered; selecting D3 advanced current index 0→1 and completed count 0→1; a second selection was ignored. OpenXR fileID churn reappeared and was restored under the earlier approval. No commits.
- **Files changed:** `Code/Scripts/BlackoutScenario.cs`.
- **Thoughts / reasoning:** Using the base `CompleteObjective()` keeps the wrist/debrief objective counter consistent with the stage change; a one-shot guard prevents repeated selections from skipping further steps.
- **What I learned:** Several handlers can coexist on the same `Interactable.SelectEntered`, so the new objective trigger does not disturb the door-reset or wrong-action subscriptions.

### 2026-11-26 — Volumi del fuoco e dell'allarme nello Stress
- **Request:** "now raise the volume of SFX_Fire and slightly raise the fire alarm volume in stress mode"; clarified: SFX_Fire = SFX_Burning, raise only Stress to 1.0, alarm 0.8 normally and 1.0 in Stress. Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/GameManager.cs`.
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. Raised `BurningVolumeStress` from 0.85 to 1.0 (Realistic unchanged at 0.5), added `AlarmVolume = 0.8` and `AlarmVolumeStress = 1f`, and set the alarm AudioSource volume by `MissionSession.Level` inside `PlayBlackoutSound()`. Recompile clean. Play Mode: Guided alarm 0.80 with burning off; Operational alarm 0.80 / burning 0.50; Crisis alarm 1.00 / burning 1.00. OpenXR fileID churn reappeared and was restored under the earlier approval. No commits.
- **Files changed:** `Code/Scripts/GameManager.cs`.
- **Thoughts / reasoning:** The alarm source previously used Unity's default volume 1.0, so a 0.8 baseline was needed to make Stress a slight raise rather than no change.
- **What I learned:** Both sounds share the blackout event but use separate AudioSources, so their volumes can be tuned independently per difficulty.

### 2026-11-26 — Obiettivi nascosti nel Brief anche in Guidato (solo Blackout)
- **Request:** "hide the objective rows even in guided only for blackout scenario". Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Home/Code/Scripts/Runtime/Screens/MissionBriefScreen.cs` (Brief screen only; other UI locations unchanged).
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. In `ApplyObjectiveVisibility()` removed the Guided exception, so the `ID_BLACKOUT` gate now hides all objective rows at every level; updated the comment. Recompile clean. Play Mode: Blackout + Guided showed 0 of 4 objective rows; FireStarter still showed 4 of 4. Unity restarted during verification with no project-file changes; SCN_Blackout restored as the open scene. No commits.
- **Files changed:** `Assets/_Project/Home/Code/Scripts/Runtime/Screens/MissionBriefScreen.cs`.
- **Thoughts / reasoning:** Removing only the level exception keeps the change scoped to Blackout and leaves other scenarios' Brief behaviour intact.
- **What I learned:** Calling `ApplyObjectiveVisibility()` directly requires the private `mission` field to be set (normally done by `Refresh()`); this was only a verification-harness detail.

### 2026-11-26 — Testo dell'esito positivo cambiato
- **Request:** "what text comes up when the 'esito positivo' ui comes up?" → "subtext"; then "change 'Tutti al sicuro e guasto segnalato' to 'Hai evacuato l'edificio in sicurezza'". Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Home/UI/Data/Missions/SO_Mission_Blackout.asset` only.
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. Located the outcome subtext: `OutcomePanel.ResolveReason()` reads `MissionSession.Current.GetCriterionText(result.Outcome)`, i.e. the Positive criterion (`Outcome: 0`) in `SO_Mission_Blackout.asset`. Changed that Text from "Tutti al sicuro e guasto segnalato" to "Hai evacuato l'edificio in sicurezza"; the partial and negative criteria were left unchanged. No code, scene or G_Simone changes; no commits.
- **Files changed:** `Assets/_Project/Home/UI/Data/Missions/SO_Mission_Blackout.asset`.
- **Thoughts / reasoning:** Editing the mission data keeps the outcome reason consistent across the short outcome panel and any UI that reads the criteria, without touching shared code.
- **What I learned:** The short "Esito positivo" subtext is scenario data (the Positive criterion), not a hardcoded string; the title comes from `ui.outcome.positive` in `SO_UIStrings_It`.

### 2026-11-26 — Tosse da immobilità ogni 20 secondi nello Stress
- **Request:** "change the cough mechanic to every 20s in stress". Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/BlackoutScenario.cs`.
- **Actions performed:** Read-only activation check showed the existing user modifications; left them untouched. Changed `TOSSA_INTERVALLO` from 40f to 20f. Recompile clean. Play Mode with a test rig: at 19.99 s no cough, at 21 s the cough played and the counter reset; level was Crisis. A new untracked `BlackoutCameraSmoke.cs` (+ `.meta`) appeared in the same folder and was left untouched. No commits.
- **Files changed:** `Code/Scripts/BlackoutScenario.cs`.
- **Thoughts / reasoning:** A single constant controls the interval, so the stress-only, no-damage feedback behaviour stays identical with a shorter cadence.
- **What I learned:** The threshold check uses accumulated `Time.deltaTime` with a one-shot reset, so verifying just below and just above 20 s is enough to confirm the new cadence.

### 2026-11-26 — Camera smoke in Stress mode (and visibility fix)
- **Request:** "add a smoke effect on the camera in stress mode", with "do not intensify it so much that it makes it impossible to see, but it does need to make it harder", then "I can't see the vfx" and "you can make the particles black" → "Black, but denser/more opaque". Entry approved by the user.
- **Directory confirmed by user:** `Assets/_Project/Groups/G_Simone/Code/Scripts/` (new script; fixes applied to the same file).
- **Actions performed:** Created `BlackoutCameraSmoke.cs` + `.meta`. Runtime self-bootstrap via `sceneLoaded`; only `DifficultyLevel.Crisis`; subscribes to `GameManager.OnBlackout`; attaches a runtime particle system ~0.45 m in front of the camera with local simulation space and rig-scale compensation. Ramps over 85 s: emission 3→26/s, size 0.25–0.45→0.45–0.95 m, material alpha 0.65→0.95, black gradient with alpha peak 0.65, max 110 particles. Debugged invisible VFX in Play mode: particles existed and rendered but were too dark; switched to black with higher opacity/density. Fixed replay by destroying/recreating the host per scene load and on mission end; switched to `sharedMaterial` so the alpha ramp affects the drawn material. Recompile clean; Play Mode verification via Game View captures; Play stopped; no git state-changing commands; no commit.
- **Files changed:** `BlackoutCameraSmoke.cs` (new, then fixed); `BlackoutCameraSmoke.cs.meta` (new); `AGENTS.md` (this entry).
- **Thoughts / reasoning:** Runtime bootstrap avoids editing the already-modified scene and covers additive loads; black smoke needs opacity and density rather than a light tone to read against a dark office; caps preserve "harder but not impossible".
- **What I learned:** `ParticleSystemRenderer.material` can return an instanced copy, so `sharedMaterial` is required for runtime material tweaks; a `sceneLoaded` host must be recreated between missions or it keeps a stale event subscription.
