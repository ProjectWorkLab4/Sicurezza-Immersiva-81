# Troubleshoot

Common problems for this repository, why they happen, and the standard fix.
When in doubt, ask on the team channel before committing configuration changes.

## Git shows ProjectSettings / XR files as modified without anyone changing them

**Symptom.** After opening the project (or after a pull), `git status` shows files in
`ProjectSettings/`, `Assets/XR/Settings/` or `Assets/Oculus/` as modified.

**Cause.** Unity and the Meta XR Project Setup Tool (PST) rewrite these files based
on the local machine. These changes are machine noise, not project decisions.

**Fix.**

1. Check whether the change is deliberate: `git diff <file>`.
2. If nobody agreed to the change, discard it **one file at a time**:
   ```bash
   git restore ProjectSettings/EditorBuildSettings.asset
   ```
3. Never discard a whole folder blindly: some changes (for example a new build
   scene) can be real work. Review the diff first.

## Meta PST "Fix All" rewrote the project settings

**Symptom.** After clicking **Fix All** in the Project Setup Tool, many files in
`ProjectSettings/` and `Assets/XR/Settings/` show up in `git status`.

**Cause.** Fix All applies every PST task on your machine, including
machine-specific ones (XR loaders, local SDK paths, editor-only options).

**Fix.** Run Fix All only when the team agrees on it. After running it, review
`git status`, keep only the changes you meant to make, and restore the rest.
See the [Fix All rule](README.md#project-setup-tool-fix-all-rule) in the README.

## `Assets/Oculus/OculusProjectSetupSettings.asset` reappeared or changed

**Symptom.** `Assets/Oculus/OculusProjectSetupSettings.asset` shows as new or
modified, or Unity logs that it was recreated.

**Cause.** This file stores the PST options shared by the whole team. The project
ships it with **Background Checks disabled** so the PST does not re-scan the project
every few seconds on every machine. If the file is missing (for example after Unity
regenerates it), that option falls back to enabled on that machine only.

**Fix.** Open the Project Setup Tool settings (**Meta > Project Setup Tool**, or the
pencil icon on the PST status pill) and disable **Background Checks** again. This
rewrites the shared asset; commit it together with your work. The expected content
is a single entry:

```yaml
boolProperties:
  keys:
  - Meta.XR.SDK.Project Setup Tool.BackgroundChecks
  values:
  - 0
```

## Whole-file diffs that only change line endings

**Symptom.** A `.asset` or `.meta` file shows a huge diff in which every line
appears changed.

**Cause.** The repository stores text files with LF endings and pins this with
`.gitattributes`, so this should not happen on an up-to-date branch. It appears on
branches created before `.gitattributes` existed, or if a tool committed files with
CRLF endings.

**Fix.** Pull `main` (which contains `.gitattributes`), merge it into your branch,
and let Git normalize. If files still show CRLF churn, run once:

```bash
git add --renormalize .
git commit -m "chore: renormalize line endings"
```

## Build Settings reference a scene nobody has

**Symptom.** Builds fail or warn about a missing scene; `EditorBuildSettings.asset`
points to a file that does not exist in the repository.

**Fix.** `main` keeps `Assets/_Project/SCN_Placeholder.unity` as the only build
scene so this never happens. Do not add group scenes to Build Settings from a group
branch: the scene list changes only on `main`, when the real scene flow is ready.
See [Placeholder scene](README.md#placeholder-scene).
