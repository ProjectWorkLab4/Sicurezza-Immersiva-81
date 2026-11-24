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
