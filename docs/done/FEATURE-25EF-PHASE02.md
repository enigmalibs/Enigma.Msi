# FEATURE-25EF — PHASE02 — Release prep: desktop app 1.6.0

**Status:** DONE
**Branch:** `feature/feature-25ef-phase02-release-1-6-0`
**Plan:** `docs/plan/FEATURE-25EF.md` (PHASE02)
**Run:** feature/2026-10-28-new-icons-release

## Summary

**Enigma.Msi.Desktop 1.6.0** is prepared. It is an **application-only** SemVer minor release that ships
PHASE01's new icon and logo. It has no breaking change and no profile-format change.

- The app declares `<Version>1.6.0</Version>`.
- `msiProfiles/Enigma.Msi.Desktop.1.6.0.msipkg.json` describes its installer.
- `RELEASENOTES.md` leads with a 1.6.0 section, the README callout names 1.6.0, and `SECURITY.md` supports
  1.6.x.

**The Enigma.Msi library stays at 1.0.0**: `src/Enigma.Msi/Enigma.Msi.csproj` is untouched. The run
tagged, pushed and uploaded nothing.

**This phase ran on Linux**, like the 1.1.0 and 1.4.0 release preps. It could **not** build the dogfood MSI
or run the Windows-only half of the suite. Both are recorded below as blocked, with the commands, and
handed to the maintainer.

## Files/modules touched

### Created

| Path | What |
|---|---|
| `msiProfiles/Enigma.Msi.Desktop.1.6.0.msipkg.json` | The 1.6.0 installer profile, cloned from 1.5.0. Only `version` and `productId` change |
| `docs/done/FEATURE-25EF-PHASE02.md` | This file |

### Modified

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` | `<Version>` 1.5.0 → **1.6.0** |
| `RELEASENOTES.md` | A new `## 1.6.0 — Enigma.Msi.Desktop` section on top: an intro, *Changed* (the icon, the logo) and *Compatibility*. The current-version table's app row → **1.6.0**. Older sections are untouched |
| `README.md` | The what's-new callout → **1.6.0**. The "build its own installer" example now builds the 1.6.0 profile, not 1.3.0's |
| `SECURITY.md` | The app's supported-versions row 1.5.x → **1.6.x** |
| `CLAUDE.md` | Build-state block: a FEATURE-25EF entry. FEATURE-2D02's and FEATURE-1E7D's "nothing was tagged" now say the run tagged nothing and the maintainer tagged `1.3.0`/`1.5.0` on `main` later |
| `docs/roadmap.md`, `docs/plan/FEATURE-25EF.md` | PHASE02 TODO → IN PROGRESS → **DONE**. The item is **DONE** |

**No source code and no test changed.**

## GUID contract

| Field | Value | Rule |
|---|---|---|
| `productId` | `577b6b13-6f79-4bbc-9abe-bd48944a2569` | **New for this version.** Generated with `uuidgen` and lowercased, like every committed profile |
| `upgradeCode` | `3405046f-527a-439e-a22f-866247dc8314` | **Copied unchanged** from 1.0.0 through 1.5.0. It is what makes 1.6.0 upgrade an installed 1.5.0 instead of installing beside it |

`diff` against 1.5.0 shows exactly the two lines `version` and `productId`. The icon paths
(`src\Enigma.Msi.Desktop\Assets\appicon.ico`) needed no edit, because PHASE01 kept the file name. The MSI
built from this profile therefore carries the new icon on both shortcuts and in the Control Panel.

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| `<Version>` 1.6.0; the cross-published dll stamps 1.6.0 | **Met.** `FileVersion 1.6.0.0` on both the dll and the apphost exe |
| 1.6.0 notes section and table; README callout; `SECURITY.md` 1.6.x | **Met** |
| Profile committed; diff = `version` + `productId`; `upgradeCode` verbatim; deserializes with 0 in-memory errors | **Met** |
| 0 warnings; desktop suite green; library failures equal the baseline | **Met** |
| Dogfood MSI explicitly blocked, with reason and commands | **Met.** See *Deviations* |
| Library at 1.0.0; nothing packed or pushed | **Met** |

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How to verify the profile | A throwaway .NET 10 file-based program (`#:project` on the real `Enigma.Msi`, `PublishAot=false`), deleted afterwards | The same check as 1.4.0: the real deserializer and validator, with no permanent test bound to a file that changes every release | A permanent profile test |
| The release notes' section names | *Changed* and *Compatibility* | An artwork change alters something that exists rather than adding a feature. 1.5.0 already used *Changed* | *What's new* |
| Check the apphost exe as well as the dll | Yes. The exe also carries `FileVersion 1.6.0.0` and the 10 new icon frames | The exe is what Explorer, the taskbar and the shortcuts show. A Linux cross-publish customizing the apphost is not something to assume | Checking the dll only |

## Deviations & follow-ups

- **No deviation from the plan.**
- **Blocked on this host: the dogfood MSI build.** It needs Windows and the WiX CLI; this host is Linux
  with no `wix`. Run from the repository root on Windows:
  ```
  mkdir artifacts
  dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
  .\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.1.6.0.msipkg.json
  ```
  1.5.0's MSI was 17.29 MB. 1.6.0 should be within a few KB of it, because the icon shrank and the logo
  grew. Its `Icon` table should hold the new 24,018-byte `.ico`.
- **Blocked on this host: the Windows half of the suite.** It covers the `net472` worker suite with its 16
  WixSharp drift guards (it cannot start without `mono`) and the library's 16-test Windows-only cluster
  × 2 TFMs. The expected Windows total is **471**, unchanged from 1.5.0, since no test was added.
- **Maintainer's steps:** install/launch/uninstall per `docs/RELEASE.md` §5, including that 1.6.0
  upgrades an installed 1.5.0 and that the new icon shows on the shortcuts and in Control Panel.
- **The run's own outward-facing steps: none.** The merges into `develop` and `main` and the `1.6.0` tag
  follow the run, at the user's request (see the plan's *Decisions*).
- **Follow-ups unchanged from earlier releases, still out of scope:**
  - the worker csproj's stale `PackEnigmaMsiWorkerPayload` comment;
  - `Environment.NewLine` in `MsiPackageJson.Serialize`;
  - no `helpLink`/`urlInfoAbout` on the dogfood profile;
  - no `1.2.0` tag.
- **Line endings:** nothing to report. Every touched and created text file is LF-only (`grep -c $'\r'` → 0).

### Documentation sweep (applied)

| File | Edit |
|---|---|
| `README.md`, `SECURITY.md`, `RELEASENOTES.md`, `CLAUDE.md` | As listed above (planned scope) |

Also checked:

- `docs/RELEASE.md`: version-agnostic, nothing to change.
- `docs/guides/desktop-app.md`: describes "the logo" without depicting it, so it is still true.
- Every other `*.md` outside `docs/plan`/`docs/done`: no `1.5.x` left that this release made false.

## Build/test evidence

**Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`.

**Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release`, first run, no fix cycle:

```
total: 390   failed: 32   succeeded: 358   skipped: 0
```

| Suite | TFM | Result |
|---|---|---|
| `Enigma.Msi.Desktop.UnitTests` | net10.0 | **156/156** |
| `Enigma.Msi.UnitTests` | net8.0 + net10.0 | 202/234. A `diff` of the failing list against the run's baseline is empty |
| `Enigma.Msi.Worker.UnitTests` | net472 | not started (no `mono`) |

**Profile verification** (scratchpad harness, deleted afterwards):

```
deserialize: OK
  version 1.6.0 · productId 577b6b13-6f79-4bbc-9abe-bd48944a2569 · upgradeCode 3405046f-527a-439e-a22f-866247dc8314
  manufacturer Josué Clément · PerMachine · compression High · 2 shortcuts
  productIcon src\Enigma.Msi.Desktop\Assets\appicon.ico
in-memory  : 0 error(s)
round-trip : identical content
```

The environment rules were not run. The profile's Windows-relative paths do not resolve on Linux.

**Cross-publish from Linux:**

```
dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
  →  106 files, 47 MB                        (1.5.0: 106 files, 47 MB)
     worker/: 29 files; worker/.wixsharp/: the six seeded files
     native .pdb files: 0
     Enigma.Msi.Desktop.dll  FileVersion 1.6.0.0   ProductVersion 1.6.0+5262699…
     Enigma.Msi.Desktop.exe  FileVersion 1.6.0.0   ProductVersion 1.6.0+5262699…
     both: RT_GROUP_ICON frames [16, 20, 24, 32, 40, 48, 64, 96, 128, 256], all PNG
```

`AppInfo` cuts the version at the first `+`, so the splash and About read `Version 1.6.0`.
