# Agent rules — Sicurezza Immersiva 81

These rules apply to every contributor and to every agent working in this
repository. They are mandatory unless the team explicitly agrees otherwise on
`main`.

## Version control: Git only

**Git is the only version control system used in this project. Nothing else.**

- Use `git` and GitHub for branching, commits, merges, reviews and releases.
- **Never install, enable, initialize or use Unity Version Control** — also
  called Unity VCS, UVCS, Plastic SCM, or `com.unity.collab-proxy`.
  This covers the Unity Hub Version Control tab, the Editor Version Control
  window, the `com.unity.collab-proxy` package, and the `plastic` / `cm` /
  `uvcs` command-line tools.
- Never add `com.unity.collab-proxy` back to `Packages/manifest.json` or
  `Packages/packages-lock.json`. A change that reintroduces it must be rejected
  in review.
- Never create or commit Unity Version Control workspace files such as
  `.plastic/`, `plastic.workspace`, `plastic.workspace.plastic`, `ignore.conf`,
  or any other Plastic/UVCS configuration.
- If Unity, Unity Hub or any tool offers to set up version control, connect a
  workspace or install the Version Control package, **decline**. Close the
  prompt and keep using Git.
- If a Unity asset appears "checked out" or read-only, fix the file permissions
  and use Git normally. Do not "fix" it by installing or re-enabling Unity
  Version Control.

Quick check before committing:

```bash
git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json
```

The command must print nothing.

## Related project rules

The rules for shared assets, the Meta XR Project Setup Tool and naming live in
[README.md](README.md). Read them before touching `Assets/_Project/Shared/` or
`ProjectSettings/`.
