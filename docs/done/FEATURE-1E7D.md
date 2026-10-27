# FEATURE-1E7D — Desktop app 1.5.0 release

**Status:** DONE
**Branch:** `feature/feature-1e7d-release-1-5-0`
**Plan:** `docs/plan/FEATURE-1E7D.md`
**Run:** vibe/2026-10-22-release-1-5-0

## Summary

**Enigma.Msi.Desktop 1.5.0** is prepared and **verified on Windows**. It is an application-only SemVer
minor release that adds no new work. It releases what the `(unreleased)` release-notes section held:

- **BUG-7325**: an app installed under Program Files builds MSIs.
- **FEATURE-1399**: a new package defaults to `None` compression.

The app declares `<Version>1.5.0</Version>`, and `msiProfiles/Enigma.Msi.Desktop.1.5.0.msipkg.json`
describes its installer. `RELEASENOTES.md` leads with a 1.5.0 section, the README callout names 1.5.0, and
`SECURITY.md` supports 1.5.x.

Unlike 1.4.0, this release was built on a Windows host with the WiX CLI. The whole suite ran (471/471), the
**dogfood MSI was built** from the committed profile, and its tables were read back. **The Enigma.Msi
library stays at 1.0.0**: `src/Enigma.Msi/Enigma.Msi.csproj` is untouched, and nothing was tagged, pushed,
uploaded or installed.

## Files/modules touched

### Created

| Path | What |
|---|---|
| `msiProfiles/Enigma.Msi.Desktop.1.5.0.msipkg.json` | The 1.5.0 installer profile, cloned from 1.4.0. Only the two fields the clone rule changes for a version bump differ (`version`, `productId`) |
| `docs/done/FEATURE-1E7D.md` | This file |

### Modified

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` | `<Version>` 1.4.0 → **1.5.0** |
| `RELEASENOTES.md` | Current-version table: the app row → **1.5.0**. `(unreleased)` renamed to `## 1.5.0 — Enigma.Msi.Desktop`, with an intro paragraph. Sections reordered to *Fixed → Changed*. "installing the next release" → "installing 1.5.0". A *Compatibility* section added. The bullets under *Changed* are as FEATURE-1399 wrote them; the *Fixed* bullet keeps BUG-7325's content, reworded into shorter sentences |
| `README.md` | What's-new callout → **1.5.0** |
| `SECURITY.md` | Supported-versions table: the app row 1.2.x → **1.5.x** (it went stale at 1.3.0) |
| `docs/RELEASE.md` | §4: the tag-convention sentence no longer lists versions. It said "`git tag` shows `1.0.0` and `1.1.0`" and had been stale since 1.3.0 |
| `CLAUDE.md` | Build-state block. FEATURE-1DEF's "still owed on Windows" is corrected to what the repo can show. The BUG-7325/FEATURE-1399 entry now says "released in 1.5.0". A FEATURE-1E7D entry is added |
| `docs/roadmap.md`, `docs/plan/FEATURE-1E7D.md` | TODO → IN PROGRESS → **DONE** (whole-table reformat at each flip) |

**No source code and no test changed.** The only non-documentation edits are one version string and one
new JSON data file.

## GUID contract

| Field | Value | Rule |
|---|---|---|
| `productId` | `3a277978-1588-49a5-a8dc-f27f06eb2322` | **New for this version.** Generated with PowerShell `[guid]::NewGuid()` and lowercased to match the committed profiles. Not written by hand |
| `upgradeCode` | `3405046f-527a-439e-a22f-866247dc8314` | **Copied unchanged** from 1.0.0 through 1.4.0. It is what makes 1.5.0 *upgrade* an installed earlier version |

`diff` against the 1.4.0 profile shows exactly two changed lines: `version` and `productId`.
`"compression": "High"` is carried over (see *Decisions*).

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| `<Version>` is 1.5.0, and the published dll stamps 1.5.0 | **Met.** `FileVersion 1.5.0.0`, `ProductVersion 1.5.0+cc89b36…` |
| `RELEASENOTES.md` leads with 1.5.0, there is no `(unreleased)` heading, and the version table says 1.5.0. README callout 1.5.0, `SECURITY.md` 1.5.x | **Met** |
| 1.5.0 profile committed, `diff` vs 1.4.0 = `version` + `productId`, `upgradeCode` verbatim | **Met** |
| 0 warnings; the whole suite green on Windows | **Met.** 0 warnings, **471/471** |
| Dogfood MSI built (exit 0), with ProductVersion 1.5.0, the new ProductCode, the same UpgradeCode, and the six `.wixsharp` files | **Met.** See *Build/test evidence* |
| `docs/RELEASE.md` §4 and `CLAUDE.md` no longer state anything the release made false | **Met** |
| Library still 1.0.0; nothing tagged, pushed or uploaded | **Met** |

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How to prove the profile deserializes and validates | The worker's own `build` run. It deserializes the committed file, runs the in-memory **and** environment rules, and exits `1` on any violation. It exited `0` | On Windows, with the publish in place and `artifacts/` created, this is the strongest check available. It is the real code path, not a harness. The Linux releases needed a scratchpad harness only because they could not run the worker | A throwaway `#:project` harness (duplicates what the worker proved) |
| How to prove BUG-7325's fix ships | Read the MSI's Directory, Component and File tables with `WindowsInstaller.Installer` and list the files whose component sits under `worker\.wixsharp` | The release notes promise the folder "ships inside its MSI". This checks that promise in the shipped artifact, not only in the publish folder | Trusting the publish folder's contents |
| Manufacturer check | Compared the MSI's `Manufacturer` value code point by code point | The console showed `Josu� Cl�ment` through the Git Bash → PowerShell pipe. The code points (`U+00E9` twice) prove that was console encoding, and the MSI is intact | Accepting the console rendering at face value either way |
| `docs/RELEASE.md` §4 wording | "as every existing tag in `git tag` shows", with no list | A list goes stale with every release. "every release" would be false too: `1.2.0` was never tagged | Listing `1.0.0`…`1.4.0` |
| The `1.8 MB` footprint | Not stated in the release notes. They say "six more files" | The publish confirms six files of 1,784 KB, but sizes differ between WixSharp versions. The file count is the stable fact | Quoting a size |

## Deviations & follow-ups

- **No deviation from the plan.**
- **Maintainer's steps (not run, by design):** merge to `main`, `git tag 1.5.0`, push the tag, attach
  `artifacts\Enigma.Msi.Desktop.msi` wherever it is published, and `docs/RELEASE.md` §5 on a test machine.
  §5 checks install/launch/uninstall and that 1.5.0 **upgrades** an installed 1.4.0 instead of installing
  beside it. A per-machine install building an MSI as a standard user is the end-to-end proof of BUG-7325.
  It needs an installed copy, so it belongs to §5 too.
- **The MSI on disk is this run's build.** `artifacts\Enigma.Msi.Desktop.msi` (git-ignored) was built from
  the dev branch's working tree before the commit. The published dll's `ProductVersion` therefore carries
  the planning commit's hash (`+cc89b36…`), not the release commit's. That is cosmetic: `AppInfo` cuts at
  the first `+`, so the app shows `Version 1.5.0`. Rebuild from the merged `main` per §2 if the
  informational version should name the released commit.
- **Follow-up (maintainer's call):** there is no `1.2.0` tag (`git tag` → `1.0.0`, `1.1.0`, `1.3.0`,
  `1.4.0`).
- **Follow-ups unchanged from earlier items, still out of scope:**
  - The worker csproj's `AppendRuntimeIdentifierToOutputPath` comment still names the deleted
    `PackEnigmaMsiWorkerPayload` target.
  - `MsiPackageJson.Serialize` writes `Environment.NewLine`.
  - The dogfood profile sets no `helpLink`/`urlInfoAbout`.
- **Line endings:** nothing to report. Every touched and created file is LF-only (`grep -c $'\r'` → 0
  each).

### Documentation sweep (applied)

| File | Edit |
|---|---|
| `CLAUDE.md` | Build-state block, as listed above (planned scope) |
| `README.md`, `SECURITY.md`, `docs/RELEASE.md` | As listed above (planned scope) |

Also checked:

- `docs/guides/desktop-app.md`: its "for the next release" step is generic; FEATURE-1399 had already
  updated the compression default. Nothing to change.
- Every other `*.md` outside `docs/plan`/`docs/done`: no `(unreleased)`, `1.2.x` or `1.4.x` left that the
  release made false.

## Build/test evidence

**Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`.

**Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release`, first run, no fix cycle:

```
total: 471   failed: 0   succeeded: 471   skipped: 0
  Enigma.Msi.Worker.UnitTests    (net48|x64)    passed   <- includes the 16 WixSharp drift guards
  Enigma.Msi.Desktop.UnitTests   (net10.0|x64)  passed
  Enigma.Msi.UnitTests           (net8.0|x64)   passed
  Enigma.Msi.UnitTests           (net10.0|x64)  passed
```

**Publish:**

```
dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
  →  106 files, 47 MB                        (1.4.0: 100 files, 45 MB — the +6 are .wixsharp\)
     worker/: 29 files; worker/.wixsharp/: Enigma.Msi.Worker.exe, Enigma.Msi.Worker.pdb, WixSharp.dll,
              WixSharp.UI.dll, WixToolset.Dtf.WindowsInstaller.dll, WixToolset.Mba.Core.dll (1,784 KB)
     Enigma.Msi.Desktop.dll  FileVersion 1.5.0.0   ProductVersion 1.5.0+cc89b360…
```

**Dogfood MSI** (`mkdir artifacts` first, run from the repository root):

```
.\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.1.5.0.msipkg.json
  exit 0, 21.1 s, WiX 7.0.0
  artifacts\Enigma.Msi.Desktop.msi   18,129,207 bytes = 17.29 MB      (1.3.0: 17.25 MB)
  orphaned wix processes afterwards: 0
```

**Read back out of the MSI** with `WindowsInstaller.Installer` (scratchpad scripts, not committed):

```
Property   ProductName    = Enigma.Msi.Desktop
           ProductVersion = 1.5.0
           ProductCode    = {3A277978-1588-49A5-A8DC-F27F06EB2322}     = the new productId
           UpgradeCode    = {3405046F-527A-439E-A22F-866247DC8314}     = unchanged
           Manufacturer   = Josué Clément    (U+00E9 ×2, verified by code point)
           ALLUSERS       = 1                (PerMachine)
           ARPPRODUCTICON = app_icon.ico

Upgrade    {3405046F-…DC8314}  VersionMax 1.5.0  Attributes 1  WIX_UPGRADE_DETECTED    → older versions upgraded
           {3405046F-…DC8314}  VersionMin 1.5.0  Attributes 2  WIX_DOWNGRADE_DETECTED  → same-or-newer detected

Shortcut   DesktopFolder      Enigma.Msi.Desktop.lnk → [INSTALLDIR]\Enigma.Msi.Desktop.exe
           ProgramMenuFolder  Enigma.Msi.Desktop.lnk → [INSTALLDIR]\Enigma.Msi.Desktop.exe

Directory  _.wixsharp  parent worker  (worker parent INSTALLDIR)
File       106 rows; under worker\.wixsharp:
           Enigma.Msi.Worker.exe 19,968 · Enigma.Msi.Worker.pdb 6,424 · WixSharp.dll 689,152 ·
           WixSharp.UI.dll 717,312 · WixToolset.Dtf.WindowsInstaller.dll 197,904 · WixToolset.Mba.Core.dll 169,744
```

The six `.wixsharp` sizes match the publish folder byte for byte, and the MSI's 106 `File` rows match the
publish's 106 files.

**Not run, by design:** tag, push, upload, merge to `main`, install/launch/uninstall.
