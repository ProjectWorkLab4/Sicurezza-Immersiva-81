# Sicurezza Immersiva 81

**Sicurezza Immersiva 81** is a collaborative VR serious game project about recognizing and managing workplace emergencies. The project brings multiple teams’ scenarios together as one coherent application, with shared interactions, feedback, and learning goals.

## Shared assets rule: pull before you push

> [!IMPORTANT]
> **`Assets/_Project/Shared/` is shared by every group. Pull before you push.**
>
> Before adding, editing or deleting anything in `Shared/`, pull the latest
> changes from `origin` and make sure your branch contains the newest `main`.
> Resolve conflicts and test locally **before** you commit and push. Never push
> a stale copy of `Shared/`: it can overwrite another group's work.
>
> Minimum workflow:
>
> 1. `git switch main`
> 2. `git pull origin main`
> 3. `git switch <your-branch>`
> 4. `git merge main` (or rebase your branch onto `main`)
> 5. Make, test and commit your change.
> 6. `git fetch origin` and merge `origin/main` again; resolve anything that
>    appeared, retest, then push.

## Contents

- [Shared assets rule: pull before you push](#shared-assets-rule-pull-before-you-push)
- [Version control rule: Git only](#version-control-rule-git-only)
- [Project Setup Tool (Fix All) rule](#project-setup-tool-fix-all-rule)
- [Project objectives](#project-objectives)
- [Tools](#tools)
- [Technology stack](#technology-stack)
- [Project structure](#project-structure)
- [Placeholder scene](#placeholder-scene)
- [Naming conventions](#naming-conventions)
- [Documentation](#documentation)
- [Troubleshooting](Troubleshoot.md)

## Documentation

- [Shared Player Rig](Assets/_Project/Home/Docs/SharedPlayerRig.md) — rig, unified input, locomotion and the UI hand-off.
- [Shared world interaction](Assets/_Project/Home/Docs/SharedInteraction.md) — `Interactable` / `RayInteractor` contract for scenario objects.
- [Interaction button map](Assets/_Project/Docs/Guida_Design-Interactions.md) — targeting, select, grab, back and menu.
- [Common HUD guide](Assets/_Project/Docs/Guida_HUD_comune_SI81.pdf) — shared HUD layout and content.
- [Meta VR CLI guide](Guides/MetaVRCLI_Guide.md) — `metavr` setup and device workflows.
- [Troubleshooting](Troubleshoot.md) — repository, Git and Project Setup Tool problems.

## Version control rule: Git only

> [!IMPORTANT]
> **Git is the only version control system for this project. Nothing else.**
>
> Do **not** install, enable or use **Unity Version Control** (also called Unity
> VCS, UVCS, Plastic SCM, or `com.unity.collab-proxy`). Do not add the package
> back to `Packages/manifest.json` or `Packages/packages-lock.json`, do not open
> the Unity Hub **Version Control** tab, and do not commit `.plastic/`,
> `plastic.workspace` or any other Unity VCS workspace file.
>
> If Unity or Unity Hub offers to set up or connect version control, decline the
> prompt and keep using Git. Pull requests that reintroduce Unity Version Control
> are rejected.
>
> The same rule, with more detail, is in [AGENTS.md](AGENTS.md).

## Project Setup Tool (Fix All) rule

> [!IMPORTANT]
> **Never run "Fix All" casually.** The Meta XR Project Setup Tool rewrites files in
> `ProjectSettings/`, `Assets/XR/Settings/` and `Assets/Oculus/` based on the machine
> it runs on. If those files change without an explicit, team-agreed decision, the
> result is merge conflicts and broken settings for every group.
>
> Before running it, agree with the team. After running it, review `git status` and
> commit **only** the changes you deliberately wanted. If the tool changed other
> files on its own, discard them one file at a time:
>
> ```bash
> git restore <path-to-file>
> ```
>
> Never discard whole folders blindly: a few of those changes may be real work.
> For the full procedure see [Troubleshoot.md](Troubleshoot.md).

## Project objectives

The Project Work turns the concepts developed in the earlier lab into one modular VR application for immersive safety learning. Players practice observing a situation, assessing risks, making decisions, taking action, and learning from the consequences. Mistakes should have understandable consequences and support learning rather than reduce the experience to a quiz.

The class project is organized around **four groups, two scenarios per group, eight scenarios integrated into the shared build, and a catalogue of twelve possible scenarios**. The application also provides shared areas such as Home, Training, Mission Selection, profile and results, settings, and debriefing.

> This project is for educational purposes and does not replace mandatory safety training. Safety procedures and scenario outcomes should be reviewed by a qualified safety representative.

## Tools

| Tool | Use in this project |
|---|---|
| Unity Editor | Develop and integrate the VR application and its content. |
| Meta VR CLI (`metavr`) | Look up current Meta VR documentation and support device workflows such as installing and launching builds, collecting logs and screenshots, and profiling. |
| Meta Quest headset | Test the application on target hardware. |
| Git and GitHub | Collaborate on the shared project and its group-owned content. |

For installation, agent integration, and project-specific `metavr` workflows, see the [Meta VR CLI guide](Guides/MetaVRCLI_Guide.md). For common repository and Meta Project Setup Tool problems, see [Troubleshoot.md](Troubleshoot.md).

## Technology stack

| Component | Version / target |
|---|---|
| Unity | `6000.3.14f1` |
| Meta XR Core SDK | `209.0.0` |
| XR Management | `4.7.0` |
| OpenXR | `1.16.1` |
| Universal Render Pipeline (URP) | `17.3.0` |
| Input System | `1.19.0` |
| Target platform | Meta Quest (see the project guide for the current device workflow) |

Package versions are recorded in `Packages/manifest.json`; the Unity Editor version is in `ProjectSettings/ProjectVersion.txt`.

Builds are stored in the Build/ directory at the root of the repository. Call each build by `Sicurezza-Immersiva-81_ver<num>.apk`

## Project structure

All shared Unity content lives under `Assets/_Project/`. The layout separates **who owns the content** (each group, the shared app shell, or the shared asset library) from **what kind of asset it is** (art, audio, code, level, UI, docs).

```text
Assets/_Project/
├── Docs/                         # Project-wide documentation
├── Groups/
│   ├── G_Ludovica/               # Scenario content owned by group Ludovica
│   ├── G_Marius/                 # Scenario content owned by group Marius
│   ├── G_Simone/                 # Scenario content owned by group Simone
│   └── G_Tiziano/                # Scenario content owned by group Tiziano
├── Home/                         # Shared application shell (not a scenario)
└── Shared/                       # Master library of reusable assets
```

Every content area (`Groups/G_*`, `Home/`, `Shared/`) uses the same skeleton:

```text
<Area>/
├── Art/
│   ├── Materials/                # Materials and material variants
│   ├── Models/                   # 3D models and their embedded textures
│   └── Textures/                 # Standalone textures, sprites, skyboxes
├── Audio/
│   ├── Music/                    # Music and ambience
│   └── Sound/                    # Short sound effects and UI sounds
├── Code/
│   ├── Scripts/                  # Runtime C# scripts
│   └── Shaders/                  # Shaders and Shader Graph assets
├── Docs/                         # Documentation owned by this area
├── Level/
│   └── Prefabs/                  # Prefabs and scene-building blocks
└── UI/                           # Canvases, panels, buttons and UI assets
```

### Ownership rules

- **`Groups/G_<Name>/`** contains the scenarios and assets owned by one group. Only that group edits its folder. Content used by a single scenario stays here.
- **`Home/`** contains the shared application experience: Home, Training, Mission Selection, profile/results, settings and debriefing. It is not a scenario and should not contain group-owned content.
- **`Shared/`** is the master library for assets that are useful to more than one group. Do not modify the Shared original from a group; copy it into your group folder first (see the guide below).
- **`Docs/`** contains project-wide documentation, such as the common HUD guide. Use an area's own `Docs/` folder for documentation that belongs only to that area.

The rule of thumb: if an asset belongs to one scenario, put it in that group's folder; if it belongs to the application shell, put it in `Home/`; if several groups could reuse the same source asset, put the original in `Shared/`.

### How to use an asset from Shared/

`Shared/` is the master library. A group does **not** edit the Shared original. Instead, make a working copy inside the group's own directory:

1. Pull the latest changes before you start (see the Shared assets rule above).
2. In Unity's Project window, open the matching folder under `Assets/_Project/Shared/`.
3. Select the asset and duplicate it with `Ctrl+D` (`Cmd+D` on macOS).
4. Move the duplicate into the matching folder in your group, for example `Assets/_Project/Groups/G_Marius/Art/Materials/`.
5. Rename the copy with the project naming convention, for example `MAT_MetalPanel_G_Marius`.
6. Use and modify the copy freely; the original in `Shared/` stays untouched.
7. Commit the copy together with its `.meta` file. Only change the Shared original when the change is meant for every group.

| Asset type | Recommended action |
|---|---|
| Material | Duplicate, move to your group's `Art/Materials/`, edit the copy. |
| Texture or sprite | Duplicate, move to `Art/Textures/`, assign it to your material or UI. |
| 3D model | Duplicate the model, or create a prefab from it in `Art/Models/` or `Level/Prefabs/`. |
| Audio clip | Duplicate, move to `Audio/Music/` or `Audio/Sound/`. |
| Prefab | Duplicate the prefab or create a Prefab Variant in `Level/Prefabs/`. |
| Script or shader | Reference the Shared version; do not duplicate code files. |

Dragging a Shared prefab into a scene creates a scene instance; its overrides do not modify the original prefab, but the instance remains tied to the Shared prefab. For reusable changes, duplicate the prefab or create a Prefab Variant inside your group folder.

If an asset is useful only to your group, keep it in your group's folder and do not add it to `Shared/`. If you create or download something several groups could use, add the original to `Shared/` using the pull-before-push workflow, then copy it into your group area.

### Placeholder files and empty folders

Git does not track empty directories, so every folder in the shared content skeleton contains a `PLACEHOLDER.txt` file with this content:

```text
PLACEHOLDER DON'T DELETE
```

Keep the placeholder until the folder has real content, then delete it. Without at least one tracked file, the folder disappears from GitHub and from fresh clones. When Unity imports the project, it creates a `.meta` file for every folder and placeholder; commit those `.meta` files so the project imports consistently for everyone.

### Placeholder scene

`Assets/_Project/SCN_Placeholder.unity` is the only scene listed in **Build Settings**.

- It is a **placeholder**: nothing in it is supposed to work and it is not connected
  to any group's content, scene flow or script. Its only purpose is to give the build
  a scene that exists for everyone, so `EditorBuildSettings.asset` never references
  missing files and never has to change while the groups work.
- **Do not edit, rename, move or delete it**, and do not add other scenes to Build
  Settings from a group branch.
- When the real scene flow (Home, scenario selection, etc.) is ready, swapping the
  placeholder for the real scene list is a deliberate change made once on `main`.

## Naming conventions

Use the conventions summarized from the project’s `Nomenclatura.md` reference to keep assets and code recognizable across groups.

### Asset prefixes

| Prefix | Use |
|---|---|
| `CANVAS_` | UI canvas |
| `BTN_` | Button |
| `IMG_` | UI image |
| `PANEL_` | UI panel |
| `LBL_` | UI text or label |
| `ICN_` | Icon |
| `HUD_` | In-scenario UI element |
| `TEX_` | Texture |
| `MAT_` | Material |
| `MESH_` | 3D model |
| `SFX_` | Short sound effect |
| `VFX_` | Visual effect |
| `SCN_` | Unity scene |
| `SO_` | ScriptableObject configuration data |
| `FNT_` | Font |
| `ANIM_` | Animation or clip |
| `MNG_` | Manager |

### General rules

- Write the prefix in uppercase followed by an underscore; use PascalCase for the descriptive name, for example `BTN_StartMission`.
- Avoid spaces, accents, and special characters in technical names. Add variants as suffixes, such as `BTN_StartMission_Hover` or `TEX_BrickWall_Albedo`.
- Name an asset for its purpose, not its appearance, and use the same term for the same concept throughout the project.
- Use English for technical names. Put user-facing text in localization data with stable keys, such as `ui.button.start_mission`.
- Use PascalCase for classes, functions, and public variables; camelCase for private and protected variables; and UPPERCASE for constants. Choose descriptive names and document functions with summaries.
- Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) for writing commit title and descriptions. It is a lightweight convention for creating an explicit commit history.  
  
### Trello

[Trello bello](https://trello.com/invite/b/6afc491e791da3ce73455544/ATTI449dcb37ca55de1be79691aaaa0d1f30CC681F3E/🗂%EF%B8%8F-bacheca-attivita-e-scadenze)