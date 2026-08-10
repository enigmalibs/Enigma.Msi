# FEATURE-6C35 — PHASE02 — Release prep: desktop app 1.1.0 (DONE)

**Status:** DONE
**Branch:** `feature/feature-6c35-phase02-release-1-1-0`

## Summary

**Enigma.Msi.Desktop 1.1.0** is prepared for release. This is an **application-only release**: the app
declares `<Version>1.1.0</Version>`, `msiProfiles/Enigma.Msi.Desktop.1.1.0.msipkg.json` describes its
installer, and `RELEASENOTES.md` covers the five PHASE01 improvements. **The Enigma.Msi library stays at
1.0.0 and is not re-released** — no `dotnet pack`, no NuGet push, no change to `Enigma.Msi.csproj`, its
`<PackageReleaseNotes>` or the README badges. The model, worker and `.msipkg.json` format
(`schemaVersion` 1) are untouched, so 1.0.0 profiles open in 1.1.0 unchanged.

One thing this dev could **not** do: the dogfood MSI build. This session ran on Linux; that step needs
Windows and the WiX CLI. It is recorded as an explicit blocker below with the exact commands, and
everything that *is* verifiable off-Windows was verified instead — including the publish payload the MSI
would package.

## Files/modules touched

### Created

- `msiProfiles/Enigma.Msi.Desktop.1.1.0.msipkg.json` — the 1.1.0 installer profile, cloned from the
  1.0.0 file with exactly two fields changed (`version`, `productId`) per the runbook's clone rule. The
  clone was verified to be **byte-identical to what `MsiPackageJson.Serialize` produces** for the
  package it deserializes to, so the committed file is still the library's own output rather than
  hand-written JSON that merely looks like it (the `é` escapes in `manufacturer` included — that is
  the default `JavaScriptEncoder`, as PHASE04 recorded).
- `docs/done/FEATURE-6C35-PHASE02.md` (this file).

### Modified

- `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` — `<Version>` 1.0.0 → **1.1.0**; the neighbouring
  comment updated, since the two artifacts' versions have now actually parted rather than merely being
  free to.
- `RELEASENOTES.md` — restructured from a single-release document into a **newest-first, multi-version**
  one, which is the shape `docs/RELEASE.md`'s pre-flight already assumed. New `## 1.1.0 —
  Enigma.Msi.Desktop` section on top (what's new, plus a Compatibility block); the 1.0.0 content is kept
  verbatim as history under `## 1.0.0 — Enigma.Msi and Enigma.Msi.Desktop`, its sub-headings demoted one
  level. A small table at the top states each artifact's current version, so "the library is still
  1.0.0" is answerable without reading prose.
- `docs/RELEASE.md` — see *Runbook review* below.
- `docs/roadmap.md`, `docs/plan/FEATURE-6C35.md` — PHASE02 status.

No source code was changed; no test was added or modified.

## GUID contract

| Field | Value | Rule |
|---|---|---|
| `productId` | `0918d2fa-375f-42ae-a66d-59ee2a895a1a` | **Fresh for this version.** Generated with `uuidgen` — not hand-fabricated, not derived from the 1.0.0 value. |
| `upgradeCode` | `3405046f-527a-439e-a22f-866247dc8314` | **Reused verbatim** from 1.0.0. It is the app's permanent identity; this is what makes 1.1.0 *upgrade* an installed 1.0.0 instead of accumulating beside it. |

Everything else in the profile — `appName`, `manufacturer`, `scope`, `install`, `output`, `compression`,
`controlPanel`, `shortcuts` — is carried over unchanged, as the clone rule requires. `releasePath` still
points at the RID-specific **publish** output (`…\net10.0\win-x64\publish`), which PHASE04 measured as the
difference between a 17 MB installer and a 156 MB one.

## Runbook review (plan step 6)

Reviewing `docs/RELEASE.md` against an app-only release turned up a genuine hazard rather than mere
staleness, so it was fixed rather than only noted:

- **The runbook read as one linear flow, all of it library-shaped.** A maintainer following it for 1.1.0
  would have run §4 *Pack* and §5 *Push to NuGet* for a library that is not being released — publishing a
  duplicate 1.0.0, or a 1.0.0 nupkg rebuilt from a tree whose app has moved. A new **§0 "Which release is
  this?"** now names the two flavours and their sections up front: an app-only release runs §1's build +
  test gate, then §7 and the app pre-flight, and **skips §4 and §5 entirely**.
- **Tagging.** §0 also records that tagging an app-only release is the maintainer's call: §3's bare
  `X.Y.Z` convention names the *library* version, so an app-only release either goes untagged or wants a
  distinct app-scoped tag (e.g. `desktop/X.Y.Z`) to keep the two version streams from colliding in one
  namespace. Nothing here picks, and — as before — **nothing outward-facing was run**: this repository
  still has no git remote and no tags.
- **The GUID generator line said `[guid]::NewGuid()` only.** Now names `uuidgen` as the non-Windows
  equivalent, which is what this dev actually used.
- **The lead-in's "they happened to coincide at 1.0.0; nothing keeps them in step afterwards"** now
  states that they coincided at 1.0.0 and parted at the app's 1.1.0.

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| `<Version>` 1.1.0 | **Met** — and confirmed in the built artifact: the published `Enigma.Msi.Desktop.dll` stamps `1.1.0.0` |
| Release-notes section added | **Met** — `## 1.1.0` on top, 1.0.0 kept as history, library-stays-at-1.0.0 stated explicitly |
| 1.1.0 profile committed | **Met** |
| GUID contract respected (fresh `productId`, verbatim `upgradeCode`) | **Met** — see *GUID contract* |
| Profile deserializes + zero in-memory validation errors | **Met** — and stronger: it round-trips byte-identically through `MsiPackageJson.Serialize` |
| Dogfood MSI build performed and recorded | **Explicitly blocked** — Linux session; see below |
| Build zero warnings | **Met** |
| Full test suite green | **Met for every test that can run here** — 32 pre-existing Windows-only failures remain, unchanged and untouched by this dev; see *Build/test evidence* |

## Deviations & follow-ups

- **The dogfood MSI build did not run — platform blocker, not an omission.** It needs Windows and the
  WiX CLI; this session was Linux (`uname`: `Linux archlinux 7.1.6-arch1-1 x86_64`), with **no `wix` on
  `PATH`** and **no `mono`**, so the `net472` worker cannot even start. The publish half *was* run and
  verified (below), which is the step's hard prerequisite. To complete it on Windows, from the repository
  root:
  ```
  dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
  .\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.1.1.0.msipkg.json
  ```
  Expect exit code `0` and `artifacts\Enigma.Msi.Desktop.msi`. **Until that runs, 1.1.0 is prepared but
  unproven end to end** — the profile's environment rules (do `releasePath`, `productIcon` and the
  shortcut icons resolve?) are deliberately *not* covered by this dev's in-memory validation, and the
  backslash paths cannot be resolved off Windows at all.
- **`ValidateEnvironment` was deliberately not run.** The plan scopes step 4 to the in-memory rules, and
  the profile's paths are Windows-shaped (`src\Enigma.Msi.Desktop\...`), which a Linux filesystem cannot
  resolve — a run here would report failures that say nothing about the profile. It is part of the
  Windows dogfood step above.
- **The plan's step 6 said "review and note"; the runbook was also edited.** The review found a real
  footgun (packing/pushing an unreleased library), not cosmetic staleness, so leaving it recorded only in
  a completion doc nobody reads mid-release would have been the wrong call. Scope stayed minimal: one new
  §0 section and three line-level corrections.
- **`RELEASENOTES.md` was restructured, not just prepended to.** The 1.0.0 file was titled
  `# Enigma.Msi v1.0.0 Release Notes` — a single-release document with no room for a second version.
  Adding 1.1.0 on top required a document title that is not a version and one heading level of demotion
  throughout. The 1.0.0 prose itself is unchanged.
- **Follow-up, unchanged from PHASE04 and still open:** the profile sets only `controlPanel.productIcon`;
  `helpLink`/`urlInfoAbout` pointing at the project URL would improve the Add/Remove Programs entry, and
  the `.msipkg.json` non-ASCII escaping (`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`) remains a
  library-level improvement needing its own work item. Neither was touched here — both would change more
  than an app-only release should.
- **Follow-up:** the Windows-only test cluster (16 tests × 2 TFMs) and the `net472`
  `Enigma.Msi.Worker.UnitTests` suite have not run since before PHASE01. They need one Windows run before
  the release is called good — the same machine as the dogfood build.
- **Line endings:** nothing to report. Every touched file is LF; the new profile was cloned from an
  LF-only file and stays LF.

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`
  across all seven projects and all three library TFMs.
- **Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release` → **302 total, 270 passed, 32 failed,
  0 skipped**. The failures are **exactly** the set PHASE01 recorded on the parent commit — 16 distinct
  tests, each failing on both `net8.0` and `net10.0`:
  - `MsiBuildServiceTests` (10) — spawn the stub worker as an `.exe`;
  - `MsiPrerequisiteTests` (5) — require the `wix` CLI / a real worker on disk;
  - `MsiPackageValidatorTests.MsiFilename_MustNotBeAPath` (1) — Windows path semantics.

  `Enigma.Msi.Desktop.UnitTests` is **fully green**. The `net472` `Enigma.Msi.Worker.UnitTests` suite
  does not appear in the run at all — it cannot start without `mono`. **This dev added no failure**: it
  changed one version string, one JSON data file and four markdown files, none of which any failing test
  loads.
- **Profile verification** (scratchpad harness against the real `Enigma.Msi` assembly, deleted
  afterwards) — read the committed file **back from disk**:
  ```
  deserialize: OK
    version      : 1.1.0
    productId    : 0918d2fa-375f-42ae-a66d-59ee2a895a1a
    upgradeCode  : 3405046f-527a-439e-a22f-866247dc8314
    manufacturer : Josué Clément
    scope        : PerMachine     compression: High
    shortcuts    : 2
  in-memory  : 0 error(s)
  round-trip stable: True
  ```
  `round-trip stable` is the check that the hand-cloned file is still exactly the library's own output.
- **Publish (the dogfood build's hard prerequisite, cross-published from Linux):**
  `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64
  --self-contained false` → **100 files, 45 MB**, matching PHASE04's shape exactly:
  - `worker/Enigma.Msi.Worker.exe` present, 23 files in `worker/` — so the `AppendRuntimeIdentifierToOutputPath`
    fix still holds under a RID-specific publish at 1.1.0;
  - **zero** native `.pdb` files (`TrimNativeSymbolsFromPublish` doing its job), all **four** managed
    `.pdb` files retained;
  - `Enigma.Msi.Desktop.dll` stamps assembly version **1.1.0.0**.
- **Not verified here:** the MSI itself (not built — see the blocker above), and its
  install/launch/uninstall, which stays the maintainer's step per `docs/RELEASE.md`:
  ```
  msiexec /i artifacts\Enigma.Msi.Desktop.msi
  msiexec /x artifacts\Enigma.Msi.Desktop.msi
  ```
