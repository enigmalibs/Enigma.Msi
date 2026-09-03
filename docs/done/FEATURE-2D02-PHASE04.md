# FEATURE-2D02 — PHASE04 — Release prep: desktop app 1.3.0

**Status:** DONE
**Branch:** `feature/feature-2d02-phase04-release-1-3-0`
**Plan:** `docs/plan/FEATURE-2D02.md` (PHASE04)

## Summary

**Enigma.Msi.Desktop 1.3.0** is prepared **and verified end to end on Windows**. This is an
**application-only release**, and the first one made after the packaging was removed — so there is no pack
step to skip any more, only the app flow `docs/RELEASE.md` now describes. The app declares
`<Version>1.3.0</Version>`, `msiProfiles/Enigma.Msi.Desktop.1.3.0.msipkg.json` describes its installer, and
`RELEASENOTES.md` leads with a 1.3.0 section covering the quick start's output folder, the splash restyle
and the de-NuGet. **The Enigma.Msi library stays at 1.0.0** — `src/Enigma.Msi/Enigma.Msi.csproj` was not
touched, and nothing was packed, tagged or pushed.

Verification, all of it run here:

- Clean `-t:Rebuild` of the whole solution: **0 warnings, 0 errors**, no `AVLN` diagnostic.
- **453/453 tests pass, 0 skipped** across the four suites, including the `net472` worker suite and its
  **16 WixSharp drift guards** — **no drift fired**, so WixSharp's member sets still match the mirrors.
- The committed profile deserializes through `MsiPackageJson` and reports **0 in-memory validation
  errors**; after the publish and `mkdir artifacts` it reports **0 environment errors** too.
- The **dogfood MSI is built** from that profile: `artifacts\Enigma.Msi.Desktop.msi`, **17.25 MB**, worker
  exit code `0`, WiX **7.0.0**. Its Property, Upgrade and Shortcut tables were read back out of the MSI
  and check out (below).

## Files/modules touched

### Created

| Path | What |
|---|---|
| `msiProfiles/Enigma.Msi.Desktop.1.3.0.msipkg.json` | The 1.3.0 installer profile, cloned from the 1.2.0 file with exactly the two fields the runbook's clone rule permits (`version`, `productId`) |
| `docs/done/FEATURE-2D02-PHASE04.md` | This file |

### Modified

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` | `<Version>` 1.2.0 → **1.3.0**. The comment above it already framed the app as versioning independently of the library, so it needed no rewrite |
| `RELEASENOTES.md` | New `## 1.3.0 — Enigma.Msi.Desktop` section on top (What's new + Compatibility); the current-version table's app row → **1.3.0**. The 1.2.0/1.1.0/1.0.0 sections are untouched history |
| `CLAUDE.md` | Build-state block: `FEATURE-2D02` recorded as complete through `PHASE04`, with this phase's numbers |
| `docs/RELEASE.md` | Documentation-freshness sweep: §2 gains the `mkdir artifacts` prerequisite — the directory is git-ignored and `output.outputPath` must already exist, which stopped both this release and 1.2.0 at validation |
| `docs/roadmap.md`, `docs/plan/FEATURE-2D02.md` | PHASE04 status → IN PROGRESS → DONE; the item row → DONE |

**No source code and no test was changed.** The only non-documentation edits are one version string and one
new JSON data file — which is what an app-only release prep should look like.

## GUID contract

| Field | Value | Rule |
|---|---|---|
| `productId` | `4a817b41-057a-4f5d-9698-54f3e0103360` | **Fresh for this version.** Generated with `[guid]::NewGuid()` — not hand-fabricated, not derived from the 1.2.0 value |
| `upgradeCode` | `3405046f-527a-439e-a22f-866247dc8314` | **Reused verbatim** from 1.0.0/1.1.0/1.2.0. It is the app's permanent identity, and what makes 1.3.0 *upgrade* an installed earlier version instead of accumulating beside it |

Everything else — `appName`, `manufacturer`, `scope`, `install`, `output`, `compression`, `controlPanel`,
`shortcuts` — is carried over unchanged. The clone diff against 1.2.0 is exactly those two lines.
`releasePath` still points at the RID-specific **publish** output
(`src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish`).

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| `<Version>` is 1.3.0 | **Met** — and confirmed in the built artifact: the published `Enigma.Msi.Desktop.dll` stamps `FileVersion 1.3.0.0` |
| 1.3.0 release-notes section + version table | **Met** — `## 1.3.0` on top, app row **1.3.0**, library row unchanged at **1.0.0** |
| 1.3.0 profile committed, fresh `productId`, verbatim `upgradeCode` | **Met** — see *GUID contract* |
| Profile deserializes and reports zero in-memory validation errors | **Met** — 0 in-memory errors, and 0 environment errors after the publish; it round-trips to `MsiPackageJson.Serialize`'s output character for character once line endings are normalized (see *Deviations*) |
| Warning-free build of the whole solution | **Met** — clean `-t:Rebuild`, `0 Warning(s) 0 Error(s)` |
| Entire test suite passes on Windows | **Met** — 453/453, 0 skipped, drift guards included |
| Dogfood MSI built from the committed profile and recorded | **Met** — `artifacts\Enigma.Msi.Desktop.msi`, 17.25 MB, exit `0` |
| `CLAUDE.md`, the desktop guide and the README describe the repo as it is | **Met** — `CLAUDE.md` updated here; the guide and the README's 1.3.0 callout were written by PHASE01–03 and re-read against the shipped UI (see *Deviations*) |
| Library still at 1.0.0; nothing packed, tagged or pushed | **Met** — `src/Enigma.Msi/Enigma.Msi.csproj` untouched at `<Version>1.0.0</Version>`; no `git tag`, no push, no upload was run. There is no pack step to run any more |

## Deviations & follow-ups

- **`MsiPackageJson.Serialize` emits `Environment.NewLine`, so on Windows it writes CRLF.** The committed
  profile round-trips to *identical content*, but the serialized string is 1 117 characters against the
  file's 1 083 — exactly the 34 extra `\r` bytes, plus the file's trailing newline, which `Serialize` does
  not emit. `FEATURE-74C4-PHASE03` recorded the 1.2.0 comparison as byte-identical modulo the trailing
  newline; the CRLF half was missed there. It matters twice: a profile **saved by the app on Windows lands
  CRLF** while every committed profile is LF (git's `* text=auto eol=lf` normalizes it on commit, so the
  tree stays clean), and a test asserting raw byte equality against a committed profile would fail on
  Windows for line endings alone. **Recommendation only — nothing was changed here**, per the workflow's
  line-endings rule. Setting `JsonWriterOptions.NewLine` to a literal LF in `MsiPackageJson.Options` would
  make the format platform-independent; it is a library change and needs its own work item.
- **`artifacts/` had to be created before the build could pass `ValidateEnvironment`** — the directory is
  git-ignored, so it does not exist on a fresh clone, and `output.outputPath` must be an *existing*
  directory (the worker does not create it). Same as 1.2.0, where it was left as a follow-up — **fixed
  here** by the documentation sweep: `docs/RELEASE.md` §2 now opens with the `mkdir artifacts`
  prerequisite.
- **The desktop guide needed no edit.** `docs/guides/desktop-app.md` was brought current by PHASE01 (the
  seven-field quick start) and PHASE02 (the splash section); both were re-read here against the shipped
  1.3.0 UI and every statement holds. It carries no version stamp, so nothing in it goes stale on a bump.
- **The README's what's-new callout needed no edit either.** PHASE03 wrote it for 1.3.0 and it was
  confirmed here against the final release notes — the same three items, in the same order.
- **Install / launch / uninstall verification stays the maintainer's step**, per `docs/RELEASE.md` §5 — it
  mutates the machine, so nothing here ran it:
  ```
  msiexec /i artifacts\Enigma.Msi.Desktop.msi
  msiexec /x artifacts\Enigma.Msi.Desktop.msi
  ```
  The Upgrade table below is evidence that an upgrade is *authored*; only that step proves it happens.
- **Follow-up, still open from 1.2.0 and earlier:** the profile sets only `controlPanel.productIcon` —
  `helpLink` / `urlInfoAbout` pointing at the repository would improve the Add/Remove Programs entry — and
  the `.msipkg.json` non-ASCII escaping still writes the manufacturer as `Josu\u00E9 Cl\u00E9ment` rather
  than `Josué Clément` (`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`). Both are library-level changes
  needing their own work item; neither belongs in an app-only release.
- **Line endings:** nothing to report in the diff. Every touched file is LF-only, the new profile included
  (cloned from an LF file with `sed`, so it stays LF). The CRLF item above is about the library's
  *serializer*, not about this dev's diff.

## Build/test evidence

**Build** — clean rebuild, so the XAML compiler re-ran on both `.axaml`-compiling projects:

```
dotnet build Enigma.Msi.slnx -c Release -t:Rebuild
  →  Build succeeded.   0 Warning(s)   0 Error(s)
```

The count was **read**, not inferred: `AVLN` warnings are printed rather than promoted by
`TreatWarningsAsErrors`, so a nonzero count would have shown while the compile still "succeeded".

**Tests** — the whole solution, all four suites:

```
dotnet test --solution Enigma.Msi.slnx -c Release
  →  total: 453   failed: 0   succeeded: 453   skipped: 0   duration: 7s 036ms
```

| Suite | TFM | Tests |
|---|---|---|
| `Enigma.Msi.UnitTests` | net8.0 + net10.0 | 232 (116 per TFM) |
| `Enigma.Msi.Desktop.UnitTests` | net10.0 | 142 |
| `Enigma.Msi.Worker.UnitTests` | net472 | 79 |

Two clusters were re-run in isolation so their counts are read rather than assumed:

```
Enigma.Msi.Worker.UnitTests  -class "*WixSharpDriftTests"   Total: 16, Failed: 0
Enigma.Msi.Desktop.UnitTests -class "*SplashWindowTests"    Total:  6, Failed: 0
```

**No drift guard fired** — WixSharp's `InstallScope`, `CompressionLevel`, `Wui` and `Dialog` member sets
still match the library's mirrors. PHASE02's headless-Avalonia suite runs green under the full-solution run
as well as on its own.

**Profile verification** (scratchpad harness — a .NET 10 file-based program with a `#:project` reference to
the real `Enigma.Msi` project, deleted afterwards), reading the committed file back from disk, run from the
repository root:

```
deserialize: OK
  appName      : Enigma.Msi.Desktop
  version      : 1.3.0
  productId    : 4a817b41-057a-4f5d-9698-54f3e0103360
  upgradeCode  : 3405046f-527a-439e-a22f-866247dc8314
  manufacturer : Josué Clément
  scope        : PerMachine   compression: High
  installPath  : %ProgramFiles%\Enigma.Msi
  releasePath  : src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish
  outputPath   : artifacts   msiFilename: Enigma.Msi.Desktop
  productIcon  : src\Enigma.Msi.Desktop\Assets\appicon.ico
  shortcuts    : 2
in-memory  : 0 error(s)
environment: 0 error(s)          (after the publish + mkdir artifacts)
round-trip : identical content; differs only by CRLF vs LF (34 bytes) and the file's trailing newline
```

**Publish** — the dogfood build's hard prerequisite, and the payload the MSI packages:

```
dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
  →  100 files, 45 MB
     worker/Enigma.Msi.Worker.exe present, 23 files in worker/   (AppendRuntimeIdentifierToOutputPath still holding)
     native .pdb files: 0   managed .pdb files: 4                (TrimNativeSymbolsFromPublish doing its job)
     Enigma.Msi.Desktop.dll  FileVersion    1.3.0.0
                             ProductVersion 1.3.0+5bc5979d9f60097a4352abb9e12464c9ddf3a26e
```

The `+<sha>` suffix is the SDK's source-revision id; `AppInfo` truncates at the first `+`, which is why the
splash and About read `Version 1.3.0`.

**Dogfood MSI build:**

```
.\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.1.3.0.msipkg.json
  →  Wix version: 7.0.0
     MSI file has been built: C:\Dev\EnigmaLibs\Enigma.Msi\artifacts\Enigma.Msi.Desktop.msi
      ProductName: Enigma.Msi.Desktop   Version: 1.3.0
      ProductId  : {4a817b41-057a-4f5d-9698-54f3e0103360}
      UpgradeCode: {3405046f-527a-439e-a22f-866247dc8314}
     exit code 0
```

| MSI fact | Value |
|---|---|
| Path | `C:\Dev\EnigmaLibs\Enigma.Msi\artifacts\Enigma.Msi.Desktop.msi` |
| Size | **17.25 MB** (18 087 375 bytes) |
| SHA-256 | `B98B6D66D0FBC2ADCF51A2B5BFA776A8990D9D57CFBBE5A5797B1C5E20E266CF` |
| Files inside | 101 (the 100 publish files + the product icon) |
| Orphaned `wix` processes afterwards | 0 |

Read back out of the MSI's own tables with `WindowsInstaller.Installer`, so the identity is verified in the
shipped artifact and not only in the profile that produced it:

```
Property table   ProductName    = Enigma.Msi.Desktop
                 ProductVersion = 1.3.0
                 ProductCode    = {4A817B41-057A-4F5D-9698-54F3E0103360}
                 UpgradeCode    = {3405046F-527A-439E-A22F-866247DC8314}
                 Manufacturer   = Josué Clément          (non-ASCII intact)
                 ALLUSERS       = 1                      (PerMachine)
                 ARPPRODUCTICON = app_icon.ico

Upgrade table    {3405046F-…DC8314}  VersionMax 1.3.0  Attributes 1   → anything older is upgraded
                 {3405046F-…DC8314}  VersionMin 1.3.0  Attributes 2   → same-or-newer detected, not replaced

Shortcut table   DesktopFolder      Enigma.Msi.Desktop.lnk  →  [INSTALLDIR]\Enigma.Msi.Desktop.exe
                 ProgramMenuFolder  Enigma.Msi.Desktop.lnk  →  [INSTALLDIR]\Enigma.Msi.Desktop.exe
```

The Upgrade table's pair is what makes 1.3.0 a **major upgrade** of an installed 1.0.0/1.1.0/1.2.0 rather
than a second product beside it — bought by reusing the `upgradeCode` verbatim.

**Not run here, by design:** the tag, any push or upload, and install/launch/uninstall on a machine.
