# FEATURE-74C4 — PHASE03 — Release prep: desktop app 1.2.0

**Status:** DONE
**Branch:** `feature/feature-74c4-phase03-release-1-2-0`
**Plan:** `docs/plan/FEATURE-74C4.md` (PHASE03)

## Summary

**Enigma.Msi.Desktop 1.2.0** is prepared **and verified end to end**. This is an **application-only
release**: the app declares `<Version>1.2.0</Version>`, `msiProfiles/Enigma.Msi.Desktop.1.2.0.msipkg.json`
describes its installer, and `RELEASENOTES.md` carries a 1.2.0 section covering the splash screen, the
About dialog, the quick start and the Control Panel default. **The Enigma.Msi library stays at 1.0.0 and
is not re-released** — nothing was packed, tagged or pushed, and `Enigma.Msi.csproj`, its
`<PackageReleaseNotes>` and the README badges were not touched.

Unlike 1.1.0's release prep, this one ran **on Windows with the WiX CLI**, so it also settles the debt
that phase left behind:

- The whole solution builds warning-free and **all 444 tests pass, 0 skipped** — including the
  `Enigma.Msi.UnitTests` classes that carried 1.1.0's 32 Linux failures (24 tests per TFM across
  `MsiPrerequisiteTests`, `MsiBuildServiceTests` and the `msiFilename` path rule) and the entire `net472`
  `Enigma.Msi.Worker.UnitTests` suite (79 tests, of which **16 WixSharp enum drift guards**). **No drift
  fired**, so WixSharp's member sets are where the mappings expect them.
- The **dogfood MSI is built** from the committed profile: `artifacts\Enigma.Msi.Desktop.msi`,
  **17.25 MB**, worker exit code `0`, WiX **7.0.0**. Its Property/Upgrade/Shortcut tables were read back
  and check out (below). The 1.1.0 MSI itself was never built and is now superseded — its profile is kept
  as history, not rebuilt.

## Files/modules touched

### Created

| Path | What |
|---|---|
| `msiProfiles/Enigma.Msi.Desktop.1.2.0.msipkg.json` | The 1.2.0 installer profile, cloned from the 1.1.0 file with exactly the two fields the runbook's clone rule permits (`version`, `productId`) |
| `docs/done/FEATURE-74C4-PHASE03.md` | This file |

### Modified

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` | `<Version>` 1.1.0 → **1.2.0** |
| `RELEASENOTES.md` | New `## 1.2.0 — Enigma.Msi.Desktop` section on top (what's new + Compatibility); the current-version table's app row → **1.2.0**. The 1.1.0 and 1.0.0 sections are untouched history |
| `CLAUDE.md` | Build-state block: FEATURE-74C4 recorded as complete with PHASE03's numbers; the stale **"no git remote and no tags"** claim corrected; the "Outstanding, and Windows-only" paragraph retired (it is no longer outstanding) |
| `docs/RELEASE.md` | Three staleness fixes — see *Runbook review* |
| `SECURITY.md` | Documentation-freshness sweep: the supported-versions table named no artifact while listing only `1.0.x`, which now reads as covering an app that is at 1.2.x. Split into one row per artifact — library **1.0.x**, application **1.2.x** |
| `docs/roadmap.md`, `docs/plan/FEATURE-74C4.md` | PHASE03 status → IN PROGRESS → DONE; the item row → DONE. The Status column was re-padded from 11 to 6 characters across the whole table, since no `IN PROGRESS` cell remains to set the width |

**No source code, no test and no library file was changed.** The only non-documentation edits are one
version string and one new JSON data file.

## GUID contract

| Field | Value | Rule |
|---|---|---|
| `productId` | `16055e02-dcf3-4b91-a906-2e3a22bfb949` | **Fresh for this version.** Generated with `[guid]::NewGuid()` — not hand-fabricated, not derived from the 1.1.0 value |
| `upgradeCode` | `3405046f-527a-439e-a22f-866247dc8314` | **Reused verbatim** from 1.0.0/1.1.0. It is the app's permanent identity, and what makes 1.2.0 *upgrade* an installed 1.1.0 or 1.0.0 instead of accumulating beside it |

Everything else — `appName`, `manufacturer`, `scope`, `install`, `output`, `compression`, `controlPanel`,
`shortcuts` — is carried over unchanged. `releasePath` still points at the RID-specific **publish** output
(`src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish`), which is the difference between a 17 MB
installer and a 156 MB one.

## Runbook review (`docs/RELEASE.md`)

Reviewing the runbook against the repository as it now stands turned up three statements that were true
when written and are not any more:

- **§0's tagging paragraph said "nothing in this repository picks for you."** It does now: the app-only
  1.1.0 was tagged **bare `1.1.0`** on its merge commit (`508fa05`), alongside the library's `1.0.0`
  (`7863a40`) — so the two artifacts already share one bare-version tag namespace instead of taking an
  app-scoped prefix. Recorded as a precedent, with the choice still the maintainer's.
- **§3 said "default to a bare `X.Y.Z` tag when the repo has none."** The repo has two; §3 now states the
  established convention outright.
- **The lead-in's `e.g. 1.1.0`** is now `e.g. 1.2.0`.

Everything else in the runbook held, including §0's central rule: an app-only release runs **§1's build +
test gate, then §7 and the app pre-flight, and skips §4 and §5 entirely** — no `dotnet pack`, no
`dotnet nuget push`. **Nothing outward-facing was run from here**: no tag was created, nothing was pushed,
nothing was published. Whether 1.2.0 gets a tag (and whether it is bare or app-scoped) is the maintainer's
call per §0.

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| `<Version>` is 1.2.0 | **Met** — and confirmed in the built artifact: the published `Enigma.Msi.Desktop.dll` stamps `1.2.0.0` |
| 1.2.0 release-notes section + version table | **Met** — `## 1.2.0` on top, app row now **1.2.0**, library row unchanged at **1.0.0** |
| 1.2.0 profile committed, fresh `productId`, verbatim `upgradeCode` | **Met** — see *GUID contract*; the clone diff against 1.1.0 is exactly those two lines |
| Profile deserializes and reports zero in-memory validation errors | **Met** — and stronger: it round-trips to `MsiPackageJson.Serialize`'s own output character for character, and after the publish `ValidateEnvironment` reports **0** errors too |
| Warning-free build of the whole solution | **Met** — clean `-t:Rebuild`, `0 Warning(s) 0 Error(s)`, no `AVLN` diagnostic |
| Entire suite passes **on Windows**, previously unexercised cases confirmed run | **Met** — 444/444, 0 skipped; the Windows-only cluster and the whole worker suite (drift guards included) each re-run in isolation to prove they were not merely absent |
| Dogfood MSI built from the committed profile and recorded | **Met** — `artifacts\Enigma.Msi.Desktop.msi`, 17.25 MB, exit `0` |
| `CLAUDE.md`, the desktop guide, `docs/RELEASE.md` describe the repo as it is | **Met** — including the corrected remote/tags statement |
| Library still 1.0.0; nothing packed, tagged or pushed | **Met** — `src/Enigma.Msi/Enigma.Msi.csproj` still `<Version>1.0.0</Version>`; no `dotnet pack`, no `git tag`, no push, no publish was run |

## Deviations & follow-ups

- **"Round-trip byte-identical" is exact modulo the file's trailing newline.** `MsiPackageJson.Serialize`
  emits no terminating newline, while both committed profiles (1.1.0 and this one) end with one — the
  ordinary text-file convention, and what `.gitattributes` wants. The comparison was therefore made after
  trimming a single trailing `\n`; both sides are then 1 083 characters and identical. Worth knowing before
  someone writes a test asserting raw byte equality against a committed profile: it would fail on the
  newline alone. (The 1.1.0 completion doc called this "byte-identical" without the caveat.)
- **`artifacts/` had to be created before the profile could pass `ValidateEnvironment`.** The directory is
  git-ignored, so it does not exist on a fresh clone, and `output.outputPath` is required to be an
  *existing* directory — the worker does not create it. `mkdir artifacts` before the dogfood build, or the
  build fails validation rather than the MSI landing somewhere unexpected. This is a pre-existing property
  of the runbook's §7, not something this release introduced; §7 could say so.
- **The desktop guide needed no edit.** `docs/guides/desktop-app.md` was brought current by PHASE01 and
  PHASE02 (splash section, the About paragraph, the whole *Quick start* section, the Control-Panel-default
  note); it was re-read against the shipped 1.2.0 UI here and every statement holds. It carries no version
  stamp of its own, so nothing in it goes stale on a version bump.
- **The sweep's one accepted edit was `SECURITY.md`.** Its supported-versions table predates the two
  artifacts parting, so a single `1.0.x` row silently spoke for an application now at 1.2.x. It is one row
  per artifact now. Nothing else in it changed — the private-reporting flow and the threat framing are
  version-independent.
- **`README.md` needed no edit either.** It is the *packed* README for the library and describes the app
  only at one-paragraph altitude, under *Documentation* — not at the feature-list level the plan's step 8
  made the condition. Its "What's new in 1.0" callout is library-scoped, and the library is still 1.0.0, so
  it stays. The NuGet badge tracks the published library version and is likewise untouched.
- **1.1.0's MSI stays unbuilt, by design.** Building it now would produce an installer for a superseded
  version whose `productId` was never released; 1.2.0's MSI, built from the same permanent `upgradeCode`,
  upgrades an installed 1.0.0 *or* 1.1.0 directly. The 1.1.0 profile is kept for history.
- **Install / launch / uninstall verification stays the maintainer's step**, per `docs/RELEASE.md`'s app
  pre-flight — it mutates the machine, so nothing here runs it:
  ```
  msiexec /i artifacts\Enigma.Msi.Desktop.msi
  msiexec /x artifacts\Enigma.Msi.Desktop.msi
  ```
- **Follow-up, still open from PHASE04 and 1.1.0:** the profile sets only `controlPanel.productIcon`;
  `helpLink` / `urlInfoAbout` pointing at the repository would improve the Add/Remove Programs entry, and
  the `.msipkg.json` non-ASCII escaping (`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, which would write
  `Josué Clément` instead of `Josu\u00E9 Cl\u00E9ment`) remains a library-level improvement needing its own
  work item. Neither was touched — both change more than an app-only release should.
- **Follow-up:** the library's 1.0.0 has still never been packed and pushed to NuGet, and the app's MSI has
  never been attached to a GitHub release. Both are maintainer steps outside this repository, and
  `docs/RELEASE.md` is the runbook for them.
- **Line endings:** nothing to report. Every touched file is LF-only, the new profile included (it was
  cloned from an LF file and stays LF).

## Build/test evidence

**Build** — clean rebuild, so the XAML compiler actually re-ran on both `.axaml`-compiling projects:

```
dotnet build Enigma.Msi.slnx -c Release -t:Rebuild
  →  Build succeeded.   0 Warning(s)   0 Error(s)
```

No `AVLN` diagnostic appeared. That count was **read**, not inferred: `AVLN` warnings are printed rather
than promoted by `TreatWarningsAsErrors`, so a nonzero count would have shown in the output while the
compile still "succeeded".

**Tests** — the whole solution, all four suites:

```
dotnet test --solution Enigma.Msi.slnx -c Release
  →  total: 444   failed: 0   succeeded: 444   skipped: 0   duration: 4s 241ms
```

| Suite | TFM | Tests |
|---|---|---|
| `Enigma.Msi.UnitTests` | net8.0 | 116 |
| `Enigma.Msi.UnitTests` | net10.0 | 116 |
| `Enigma.Msi.Desktop.UnitTests` | net10.0 | 133 |
| `Enigma.Msi.Worker.UnitTests` | net472 | 79 |

**The debt 1.1.0 left, discharged.** "0 skipped" only proves nothing was *marked* skipped, so the classes
that carried 1.1.0's 32 Linux failures (16 distinct tests × 2 TFMs, out of the 24 tests those classes hold
per TFM) and the worker suite were each re-run in isolation and their counts read:

```
Enigma.Msi.UnitTests  -class "*.MsiPrerequisiteTests"                       Total: 8,  Failed: 0
Enigma.Msi.UnitTests  -class "*.MsiBuildServiceTests"                       Total: 15, Failed: 0
Enigma.Msi.UnitTests  -method "*MsiPackageValidatorTests.MsiFilename_MustNotBeAPath*"
                                                                            Total: 1,  Failed: 0
Enigma.Msi.Worker.UnitTests.exe (whole suite)                               Total: 79, Failed: 0
Enigma.Msi.Worker.UnitTests.exe -class "*Drift*"  (WixSharpDriftTests)      Total: 16, Failed: 0
```

**No drift guard fired** — WixSharp's `InstallScope`, `CompressionLevel`, `Wui` and `Dialog` member sets
still match the library's mirrors, so nothing is silently mis-mapped.

**Profile verification** (scratchpad harness against the real `Enigma.Msi` assembly, deleted afterwards),
reading the committed file back from disk, run from the repository root:

```
deserialize: OK
  appName      : Enigma.Msi.Desktop
  version      : 1.2.0
  productId    : 16055e02-dcf3-4b91-a906-2e3a22bfb949
  upgradeCode  : 3405046f-527a-439e-a22f-866247dc8314
  manufacturer : Josué Clément
  scope        : PerMachine     compression: High
  installPath  : %ProgramFiles%\Enigma.Msi
  releasePath  : src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish
  outputPath   : artifacts   msiFilename: Enigma.Msi.Desktop
  productIcon  : src\Enigma.Msi.Desktop\Assets\appicon.ico
  shortcuts    : 2
in-memory  : 0 error(s)
environment: 0 error(s)          (after the publish + mkdir artifacts)
round-trip stable (ignoring the file's trailing newline): True
  serialized length: 1083   file length (LF, trimmed): 1083
```

**Publish** — the dogfood build's hard prerequisite, and the payload the MSI packages:

```
dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
  →  100 files, 44.6 MB
     worker/Enigma.Msi.Worker.exe present, 23 files in worker/   (AppendRuntimeIdentifierToOutputPath still holding)
     native .pdb files: 0   managed .pdb files: 4               (TrimNativeSymbolsFromPublish doing its job)
     Enigma.Msi.Desktop.dll  FileVersion 1.2.0.0
                             ProductVersion 1.2.0+cca1d5345ddcb06ec58c3f0d12672f86a20df245
```

The `+<sha>` suffix is the SDK's source-revision id; `AppInfo` truncates at the first `+`, which is why
the About dialog reads `Version 1.2.0`.

**Dogfood MSI build:**

```
.\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.1.2.0.msipkg.json
  →  Wix version: 7.0.0
     MSI file has been built: C:\Dev\EnigmaLibs\Enigma.Msi\artifacts\Enigma.Msi.Desktop.msi
      ProductName: Enigma.Msi.Desktop   Version: 1.2.0
      ProductId  : {16055E02-DCF3-4B91-A906-2E3A22BFB949}
      UpgradeCode: {3405046F-527A-439E-A22F-866247DC8314}
     exit code 0
```

| MSI fact | Value |
|---|---|
| Path | `C:\Dev\EnigmaLibs\Enigma.Msi\artifacts\Enigma.Msi.Desktop.msi` |
| Size | **17.25 MB** (18 083 279 bytes) |
| SHA-256 | `9539F66E852302105AB76BC2479BE80A492D6F6838CE8F927CB6F6B4E67C8AC7` |
| Files inside | 101 (the 100 publish files + the product icon) |
| Orphaned `wix` processes afterwards | 0 |

Read back out of the MSI's own tables with `WindowsInstaller.Installer`, so the identity is verified in
the shipped artifact rather than only in the profile that produced it:

```
Property table   ProductName    = Enigma.Msi.Desktop
                 ProductVersion = 1.2.0
                 ProductCode    = {16055E02-DCF3-4B91-A906-2E3A22BFB949}
                 UpgradeCode    = {3405046F-527A-439E-A22F-866247DC8314}
                 Manufacturer   = Josué Clément          (non-ASCII intact)
                 ALLUSERS       = 1                      (PerMachine)
                 ARPPRODUCTICON = app_icon.ico

Upgrade table    {3405046F-…DC8314}  VersionMax 1.2.0  Attributes 1   → anything older is upgraded
                 {3405046F-…DC8314}  VersionMin 1.2.0  Attributes 2   → same-or-newer detected, not replaced

Shortcut table   DesktopFolder      Enigma.Msi.Desktop.lnk  →  [INSTALLDIR]\Enigma.Msi.Desktop.exe
                 ProgramMenuFolder  Enigma.Msi.Desktop.lnk  →  [INSTALLDIR]\Enigma.Msi.Desktop.exe
```

The Upgrade table's pair is the proof the release is a **major upgrade** of an installed 1.0.0/1.1.0 and
not a second product beside it — which is exactly what reusing the `upgradeCode` verbatim buys.

**Not verified here:** install / launch / uninstall on a machine (see *Deviations*), and the light theme
of the two PHASE01 dialogs (recorded there, unchanged by this dev).
