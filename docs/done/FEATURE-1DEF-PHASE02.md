# FEATURE-1DEF — PHASE02 — Release prep: desktop app 1.4.0

**Status:** DONE
**Branch:** `feature/feature-1def-phase02-release-1-4-0`
**Plan:** `docs/plan/FEATURE-1DEF.md` (PHASE02)
**Run:** feature/2026-10-01-quickstart-msi-name-release

## Summary

**Enigma.Msi.Desktop 1.4.0** is prepared. It is an **application-only** SemVer minor release: one
backwards-compatible feature (PHASE01's derived MSI file name in the quick start), no breaking change, and
no profile-format change. The app declares `<Version>1.4.0</Version>`,
`msiProfiles/Enigma.Msi.Desktop.1.4.0.msipkg.json` describes its installer, `RELEASENOTES.md` leads with a
1.4.0 section, and the README's what's-new callout names 1.4.0. **The Enigma.Msi library stays at 1.0.0**:
`src/Enigma.Msi/Enigma.Msi.csproj` was not touched, and nothing was tagged, pushed or uploaded.

**This phase ran on Linux**, like 1.1.0's release prep did, so it could **not** build the dogfood MSI or run
the Windows-only half of the suite. Both are recorded below as blocked, with the exact commands, and handed
to the maintainer. They were not skipped quietly.

## Files/modules touched

### Created

| Path | What |
|---|---|
| `msiProfiles/Enigma.Msi.Desktop.1.4.0.msipkg.json` | The 1.4.0 installer profile, cloned from 1.3.0 with exactly the two fields the clone rule changes for a version bump (`version`, `productId`) |
| `docs/done/FEATURE-1DEF-PHASE02.md` | This file |

### Modified

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` | `<Version>` 1.3.0 → **1.4.0**. The comment above it already frames the app as versioning independently of the library, so it needed no rewrite |
| `RELEASENOTES.md` | New `## 1.4.0 — Enigma.Msi.Desktop` section on top (What's new and Compatibility); the current-version table's app row is now **1.4.0**, and the library row stays at **1.0.0**. The older sections are history and were not touched |
| `README.md` | What's-new callout → **1.4.0** |
| `CLAUDE.md` | Build-state block: `FEATURE-1DEF` recorded as complete, including what is still owed on Windows |
| `docs/roadmap.md`, `docs/plan/FEATURE-1DEF.md` | PHASE02 went IN PROGRESS → DONE; the item row is now DONE |

**No source code and no test changed.** The only non-documentation edits are one version string and one
new JSON data file.

## GUID contract

| Field | Value | Rule |
|---|---|---|
| `productId` | `e45c0ea9-78b4-477d-bef6-22c914e281a1` | **New for this version.** Generated with `uuidgen` (lowercased to match the committed profiles), not written by hand and not derived from the 1.3.0 value |
| `upgradeCode` | `3405046f-527a-439e-a22f-866247dc8314` | **Copied unchanged** from 1.0.0 through 1.3.0. It is the app's permanent identity, and it is what makes 1.4.0 *upgrade* an installed earlier version instead of installing beside it |

`diff` against the 1.3.0 profile shows exactly those two lines (`version` and `productId`). Everything else
is carried over unchanged, `output.msiFilename` included (`Enigma.Msi.Desktop`, see *Decisions*), and
`releasePath` still points at the win-x64 **publish** output.

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| `<Version>` is 1.4.0 | **Met.** The built artifact confirms it: the cross-published `Enigma.Msi.Desktop.dll` has `FileVersion 1.4.0.0` |
| 1.4.0 release-notes section, version table, README callout | **Met** |
| 1.4.0 profile committed with a new `productId` and the same `upgradeCode`, deserializes, zero in-memory validation errors | **Met.** See *GUID contract* and *Build/test evidence* |
| Warning-free build; desktop suite fully green; library failures exactly the Windows-only baseline | **Met.** The failure set is the same as the baseline and as PHASE01's run, compared by `diff` of the failing-test lists |
| Cross-published app stamps 1.4.0; dogfood MSI recorded or explicitly blocked | **Met.** It stamps 1.4.0, and the MSI build is **explicitly blocked** (Linux host, no WiX CLI), with the commands handed over |
| Library still at 1.0.0; nothing packed, tagged or pushed | **Met.** `src/Enigma.Msi/Enigma.Msi.csproj` is unchanged at `<Version>1.0.0</Version>`, and no `git tag`, push or upload was run |

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| The dogfood profile's `msiFilename` | Kept at `Enigma.Msi.Desktop` | The runbook's clone rule changes exactly `version` and `productId` (and keeps `upgradeCode`). `docs/RELEASE.md` names `artifacts\Enigma.Msi.Desktop.msi` as the artifact. PHASE01 changed how the *quick start* names a new package, not how this repository names its own installer | Adopting `Enigma.Msi.Desktop.1.4.0` (changes the documented artifact path; a decision of its own) |
| GUID casing | `uuidgen` output lowercased | Every committed profile, and `MsiPackageJson.Serialize`, writes GUIDs lowercase. A mixed-case value would make the round-trip check fail on casing alone | Committing `uuidgen`'s raw output |
| How to verify the profile | A throwaway .NET 10 file-based program (`#:project` on the real `Enigma.Msi`), deleted afterwards, as in earlier releases | Checks the committed file against the real deserializer and validator without adding a test project | A permanent profile test (it would bind the suite to a file that changes every release) |
| Harness settings | `PublishAot=false` on the harness | File-based programs default to AOT, which turns off reflection-based `System.Text.Json`. That is a harness setting, not a product defect: the app and the worker are not AOT | — |
| Cross-publish from Linux | Run it | It is the dogfood build's hard prerequisite, and the only way to confirm from this host that the shipped assembly stamps 1.4.0 and the `worker/` payload still lands | Skipping it because the MSI cannot be built here |

## Deviations & follow-ups

- **Blocked on this host: the dogfood MSI build.** This needs Windows and the WiX CLI. This session ran on
  Linux (`Linux 7.2.8-arch1-1`) with no `wix` on `PATH`, and the profile's paths are Windows-shaped. Run it
  from the repository root on Windows:
  ```
  mkdir artifacts
  dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
  .\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.1.4.0.msipkg.json
  ```
  For comparison, 1.3.0's MSI was 17.25 MB with WiX 7.0.0; 1.4.0 should be the same size to within a few KB.
- **Blocked on this host: the Windows half of the suite.** These need one Windows run before the release
  is called verified: the `net472` `Enigma.Msi.Worker.UnitTests` suite (79 tests, including the 16 WixSharp
  drift guards; it cannot start without `mono`) and the library's Windows-only cluster (16 tests × 2 TFMs).
  The expected Windows total is **464** (453 at 1.3.0, plus PHASE01's net 11).
- **Install / launch / uninstall** stays the maintainer's step, per `docs/RELEASE.md` §5 — including that
  1.4.0 upgrades an installed 1.3.0 instead of installing beside it.
- **Follow-up, outside this item's scope:** `git tag` lists `1.0.0`, `1.1.0` and `1.3.0` but no `1.2.0`, and
  `docs/RELEASE.md` §4 still says "`git tag` shows `1.0.0` and `1.1.0`". The missing tag is the
  maintainer's call (tags are outward-facing). The runbook sentence was already out of date before this dev,
  so the sweep left it alone.
- **Follow-ups unchanged from earlier releases** (library-level, each needing its own work item):
  `MsiPackageJson.Serialize` writes `Environment.NewLine`, so CRLF on Windows; the dogfood profile sets only
  `controlPanel.productIcon` (no `helpLink`/`urlInfoAbout`); the relaxed JSON escaping still writes the
  manufacturer as `Josué Clément`.
- **Line endings:** nothing to report. Every touched file is LF-only, and the new profile was cloned with
  `sed` from an LF file (`grep -c $'\r'` → 0).

### Documentation sweep (applied)

| File | Edit |
|---|---|
| `CLAUDE.md` | Build-state block: `FEATURE-1DEF` complete, including what this host verified and what is still owed on Windows |
| `README.md` | What's-new callout → 1.4.0 (the release's callout, written here) |

`docs/guides/desktop-app.md` was re-read against the shipped 1.4.0 UI. PHASE01 had already brought it up to
date, and it has no version stamp, so nothing in it needed changing. `docs/RELEASE.md` is version-agnostic
apart from the stale tag sentence above, which this dev did not cause.

## Build/test evidence

**Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`. The count
was read from the output, not assumed.

**Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release`, first run, no fix cycle:

```
total: 385   failed: 32   succeeded: 353   skipped: 0
```

| Suite | TFM | Result |
|---|---|---|
| `Enigma.Msi.Desktop.UnitTests` | net10.0 | **153/153** |
| `Enigma.Msi.UnitTests` | net8.0 + net10.0 | 200/232. The 32 failures are the documented Windows-only cluster. A `diff` of the failing-test list against PHASE01's run is empty |
| `Enigma.Msi.Worker.UnitTests` | net472 | not started (needs `mono`) |

**Profile verification:** scratchpad harness, deleted afterwards. It reads the committed file back from disk:

```
deserialize: OK
  appName      : Enigma.Msi.Desktop
  version      : 1.4.0
  productId    : e45c0ea9-78b4-477d-bef6-22c914e281a1
  upgradeCode  : 3405046f-527a-439e-a22f-866247dc8314
  manufacturer : Josué Clément
  scope        : PerMachine   compression: High
  installPath  : %ProgramFiles%\Enigma.Msi
  releasePath  : src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish
  outputPath   : artifacts   msiFilename: Enigma.Msi.Desktop
  productIcon  : src\Enigma.Msi.Desktop\Assets\appicon.ico
  shortcuts    : 2
in-memory  : 0 error(s)
round-trip : identical content          (modulo line endings and the trailing newline)
```

Environment rules were **not** run, because the profile's Windows-relative paths do not resolve on a Linux
filesystem. On Windows they pass once the publish has run and `artifacts/` exists, as they did for 1.3.0.

**Cross-publish from Linux** (the dogfood build's hard prerequisite):

```
dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
  →  100 files, 45 MB                        (1.3.0: 100 files, 45 MB)
     worker/Enigma.Msi.Worker.exe present, 23 files in worker/
     native .pdb files: 0   managed .pdb files: 4
     Enigma.Msi.Desktop.dll  FileVersion    1.4.0.0
                             ProductVersion 1.4.0+b55b910bcdf5b0666b221a507090f47d10166281
```

`AppInfo` cuts the version at the first `+`, so the splash and About will read `Version 1.4.0`.

**Not run here, by design:** the tag, any push or upload, and install/launch/uninstall. **Not run here
because this host cannot:** the MSI build and the Windows half of the suite (see *Deviations*).
