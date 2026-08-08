# FEATURE-5F00 — First release: Enigma.Msi 1.0.0 (NuGet) & Desktop app (MSI)

**Status:** IN PROGRESS (multi-phase)
**Type:** FEATURE (multi-phase)
**Branch (per phase, at build time):** `feature/feature-5f00-phaseNN-<slug>` — one branch per phase, cut from current `HEAD`.
**Depends on:** FEATURE-43A9 fully DONE (all five phases).

## Objective

Cut the first public release of the repo, end to end:

1. **Enigma.Msi 1.0.0** published to NuGet — the client library with the net472 worker bundled inside the nupkg.
2. **Enigma.Msi.Desktop 1.0.0** released as a Windows MSI installer — built with Enigma.Msi itself (dogfooding: the app's own worker CLI builds the app's installer).

Follows the house first-release process (dotnet-release: metadata → guides → README/notes → runbook), extended with one phase for the app release. All outward-facing commands (tag, pack into `./artifacts`, nuget push) are **printed, never run** — the maintainer executes them; the NuGet API key never appears in the repo or output.

## Context & constraints

- Package identity: `PackageId` **Enigma.Msi**, `RepositoryUrl`/`PackageProjectUrl` **https://github.com/enigmalibs/Enigma.Msi** (user decision: same base URL family as Enigma.Core), `RepositoryType` git, MIT license, Authors "Josué Clément".
- Version scheme: SemVer, bare `X.Y.Z` git tags (Enigma.Core precedent — no `v` prefix). First release **1.0.0** for both the library and the app (independent `<Version>` properties; they coincide at 1.0.0).
- The nupkg is unusual for the family: besides `lib/`, it carries **`tools/worker/`** (the net472 worker + its dependency closure, incl. WixSharp/WiX assemblies) and **`build/Enigma.Msi.targets`** (consumer copy to `$(OutDir)worker/`) — the plumbing prepared by FEATURE-43A9 PHASE04. Pack-verify must check this content explicitly.
- **License audit scope is larger than usual**: everything under `tools/worker/` is *redistributed* — WixSharp (MIT) plus the WixToolset assemblies it pulls (WixToolset.Dtf.WindowsInstaller, WixToolset.Mba.Core — verify their licenses, historically MS-RL for WiX components) and System.Text.Json (MIT). Record every verdict in the completion doc; if any license forbids redistribution, stop and surface it before publishing.
- `GeneratePackageOnBuild` stays **off**; packing is explicit (`dotnet pack` in the runbook).
- No symbol package (`IncludeSymbols` off) — family default (Enigma.Core ships none).
- Test pre-flight command (MTP mode): `dotnet test --solution Enigma.Msi.slnx -c Release`.
- The app's MSI profile uses **Enigma.Msi's own `.msipkg.json` format** — a documented deviation from the release skill's legacy `.msiprofile` template (user decision in FEATURE-43A9: clean break). Updating the release skill's template itself stays a follow-up outside this repo.
- The repo has no remote yet at planning time; the runbook's merge/push/tag steps assume `github.com/enigmalibs/Enigma.Msi` exists with `main` as default branch — verify before printing, skip with a note otherwise.

## PHASE01 — Package metadata, packaging layout & license audit

**Status:** DONE — see `docs/done/FEATURE-5F00-PHASE01.md`. Step 2's library→worker `ProjectReference`
proved to be a circular reference (MSB4006 at restore); the payload is assembled by a pack-time target
instead, same intent, confirmed with the maintainer.
**Branch:** `feature/feature-5f00-phase01-metadata`

1. Add the 12 packaging properties to `src/Enigma.Msi/Enigma.Msi.csproj`: `PackageId`, `Version` (1.0.0), `Title`, `Description`, `PackageTags`, `PackageReadmeFile` (README.md), `PackageLicenseFile` (LICENSE.md), `RepositoryUrl`, `RepositoryType`, `PackageProjectUrl`, `PackageReleaseNotes` (placeholder until PHASE03 finalizes it), `GenerateDocumentationFile` (already true) — plus the packing `ItemGroup` for root README.md/LICENSE.md. Confirm `GeneratePackageOnBuild` is absent/off.
2. Finalize the worker-bundling pack layout from FEATURE-43A9 PHASE04: worker output packed under `tools/worker/`, `build/Enigma.Msi.targets` packed under `build/`. Add a build-order-only `ProjectReference` from `Enigma.Msi.csproj` to the worker (`ReferenceOutputAssembly=false`, `SkipGetTargetFrameworkProperties=true`) so `dotnet pack` on the library always builds the worker first — packing from a clean tree must never produce a nupkg with an empty `tools/worker/`. A local throwaway pack may be used to iterate on the layout (deleted afterwards).
3. Third-party license audit of everything that ships: runtime deps of the library per TFM (System.Text.Json on netstandard2.0) **and the full `tools/worker/` closure** (WixSharp_wix4 + WixSharp_wix4.bin, WixToolset.* assemblies). Test-only/compile-only packages (xunit.v3, coverlet, PolySharp) are out of scope. Record findings per package (license, redistribution verdict) for the completion doc; stop and surface any non-redistributable finding.

**Acceptance criteria**
- `dotnet build Enigma.Msi.slnx -c Release` zero warnings with the metadata in place.
- A local test pack contains: `lib/` for all three TFMs, `tools/worker/Enigma.Msi.Worker.exe` + closure, `build/Enigma.Msi.targets`, README.md, LICENSE.md.
- License audit table complete; no unresolved redistribution question.

## PHASE02 — Guides & index

**Status:** DONE — see `docs/done/FEATURE-5F00-PHASE02.md`. Five guides rather than the suggested four:
validation was split out of `model.md` (the plan's "adjust the set to the code as built"). The snippet
gate was run as a compile-and-execute harness; one prose mismatch found and fixed, two CLI snippets
recorded as unexecuted.
**Branch:** `feature/feature-5f00-phase02-guides`

1. `docs/guides/` — one guide per real capability area (count follows the library, not a target): suggested set — `model.md` (MsiPackage reference: every field, defaults, `.msipkg.json` example), `building.md` (client API: `IMsiBuildService`, preflight, cancellation, worker discovery), `worker-cli.md` (headless `build <file.msipkg.json>`, exit codes 0/1/2, CI usage), `desktop-app.md` (the Avalonia app workflow). Adjust the set to the code as built.
2. `docs/guides/README.md` index (relative links are fine here — this file is never packed).
3. **Snippet-verification gate**: cross-check every API reference in every code fence against the real public surface in `src/`; fix mismatches in place; record the coverage table (per file: snippets · symbols · mismatches · uncertain) for the completion doc.

**Acceptance criteria**
- Every guide follows the house shape (intro → tables → per-scenario usage → notes); index complete.
- Snippet gate run with 0 unresolved mismatches, table recorded.

## PHASE03 — README, release notes & community files

**Status:** DONE — see `docs/done/FEATURE-5F00-PHASE03.md`. `RELEASENOTES.md` covers the Desktop app
1.0.0 in prose as planned, but the app's `<Version>` property itself is still PHASE04's step 1; the notes
are therefore ahead of the csproj until that phase lands. The README quick-start gate was run as a
compile-and-execute harness and found two prose mismatches in `RELEASENOTES.md`, both fixed.
**Branch:** `feature/feature-5f00-phase03-docs`

1. Root `README.md` (packed — nuget.org landing page): title, exactly the two house badges (NuGet version + MIT license), intro, what's-new callout, Features, Installation (`dotnet add package Enigma.Msi` + supported TFMs line), Quick start (one compiling sample: build a minimal `MsiPackage` and `BuildAsync` it), Documentation section pointing at `docs/guides/` **in prose only** (no relative `docs/` links — dead on nuget.org), License. Mention the `wix` global-tool prerequisite and Windows requirement prominently.
2. `RELEASENOTES.md` — first-release variant (`# Enigma.Msi v1.0.0 Release Notes`): Feature overview (library + worker CLI + desktop app), Dependencies (WixSharp_wix4 2.14.1, Avalonia 12.1.1 set, Enigma.Avalonia.Desktop/Enigma.Icons.Avalonia 1.0.0…), Compatibility (TFMs; Windows + wix tool required to build MSIs), Version. Covers the Desktop app 1.0.0 in the same document (single notes source — no CHANGELOG.md).
3. Finalize `<PackageReleaseNotes>` in the csproj — prose mirroring the top of RELEASENOTES.md, ending "See RELEASENOTES.md for the full details."
4. `SECURITY.md` from the house template (supported versions table: 1.0.x, GitHub private vulnerability reporting).
5. Re-run the snippet gate for the README quick-start (record in the completion doc).

**Acceptance criteria**
- README renders correctly standalone (no repo-relative links except LICENSE.md/RELEASENOTES.md); quick-start verified against the real API.
- RELEASENOTES.md + PackageReleaseNotes + SECURITY.md consistent with each other and the csproj version.
- `dotnet build Enigma.Msi.slnx -c Release` green with zero warnings after the `PackageReleaseNotes` csproj edit (a doc phase that touches the csproj still keeps the Release build green).

## PHASE04 — Desktop app release prep (MSI profile & dogfood build)

**Status:** DONE — see `docs/done/FEATURE-5F00-PHASE04.md`. Two confirmed deviations from step 2: the
`releasePath` is the **publish** output (`…\net10.0\win-x64\publish`), not the raw build output the plan
names — the latter carries `runtimes/` for every platform and measured a 156 MB MSI installing 580 MB, so
the release payload is now `dotnet publish -c Release -r win-x64 --self-contained false` (17 MB MSI); and
the icon gate was closed by **generating** the icon rather than sourcing it. The dogfood MSI was built and
inspected, but not installed — install/launch/uninstall stays with the maintainer.
**Branch:** `feature/feature-5f00-phase04-app-msi`

1. Confirm/set `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` `<Version>` 1.0.0.
2. Create `msiProfiles/Enigma.Msi.Desktop.1.0.0.msipkg.json` — the app's own new-format profile (documented deviation from the legacy `.msiprofile` template). **GUID contract**: generate both `productId` and `upgradeCode` now with a local generator (`[guid]::NewGuid()` / `uuidgen` — never hand-fabricated); the `upgradeCode` is the app's permanent identity and must be reused verbatim in every future version's profile; `productId` is regenerated per version. Field set (the full `MsiPackage` shape): `schemaVersion` 1; `version` 1.0.0 (auto); auto-detected — appName, releasePath `src\Enigma.Msi.Desktop\bin\Release\net10.0`, manufacturer from `<Authors>`, productIcon from `Assets\*.ico`; confirmed with defaults — installPath `%ProgramFiles%\Enigma.Msi`, scope PerMachine, compression High, `msiFilename` (auto from app name, confirm), outputPath (default `./artifacts` — an ignored directory, so the built MSI never lands in the tree untracked); `shortcuts[]` with the house default: one `%Desktop%` and one `%ProgramMenu%` entry, both targeting `[INSTALLDIR]\Enigma.Msi.Desktop.exe`.
3. **Icon gate (before the dogfood build):** if `Assets\*.ico` still doesn't exist, ask the user to provide it or to consciously approve releasing 1.0.0 without an icon — never proceed silently with a blank `productIcon`.
4. Dogfood verification (manual, Windows + wix tool): `Enigma.Msi.Worker.exe build msiProfiles/Enigma.Msi.Desktop.1.0.0.msipkg.json` after a Release build of the app — the produced MSI installs, launches, and uninstalls cleanly. Record the outcome (or the blocker) in the completion doc; this is the project's end-to-end acceptance for FEATURE-43A9's manual step as well.

**Acceptance criteria**
- Profile committed; it deserializes cleanly via `MsiPackageJson` and the in-memory rules of `IMsiPackageValidator` report zero errors (no worker/WixSharp invocation — the build itself is the manual step below); GUID contract respected.
- Icon gate resolved (asset provided, or explicit user approval to ship without).
- Dogfood MSI build performed and recorded (or explicitly blocked with reason — never silently skipped).

## PHASE05 — Release runbook, pre-flight & pack-verify

**Status:** TODO
**Branch:** `feature/feature-5f00-phase05-runbook`

1. `docs/RELEASE.md` from the house template, all five placeholders filled (PackageId Enigma.Msi, solution Enigma.Msi.slnx, lib csproj `src/Enigma.Msi/Enigma.Msi.csproj`, lib dir `src/Enigma.Msi`, default branch main) — extended with the app-MSI step (profile clone rule: keep `upgradeCode`, new `productId`, bump version) and an explicit note that the solution Release build is a **hard prerequisite of the pack step** (`tools/worker/` is harvested from the worker's build output). **Carried in from PHASE04:** the app-MSI step is `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false` **then** `…\worker\Enigma.Msi.Worker.exe build msiProfiles\<name>.msipkg.json`, run **from the repository root** — the profile's paths are all repo-root-relative, and the publish is what the profile packages.
2. Pre-flight: `dotnet build Enigma.Msi.slnx -c Release` and `dotnet test --solution Enigma.Msi.slnx -c Release` — both green, zero warnings.
3. **Pack-verify** (local, then deleted): pack into a throwaway dir; confirm nupkg version 1.0.0, README.md embedded non-empty, LICENSE.md embedded, nuspec `<version>/<title>/<license>/<readme>/<releaseNotes>` correct, dependency floors per TFM, **and `tools/worker/` + `build/Enigma.Msi.targets` present** (the family-unusual content). Delete the verify dir.
4. Print the runbook (never run): merge to `main` → `git tag 1.0.0` + push → `dotnet pack src/Enigma.Msi/Enigma.Msi.csproj -c Release -o ./artifacts` → `dotnet nuget push ./artifacts/Enigma.Msi.1.0.0.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json` → post-publish verification (package page, badge, `dotnet add package Enigma.Msi --version 1.0.0` restores, tag matches notes) → app MSI distribution step (where the built MSI goes is the maintainer's choice).

**Acceptance criteria**
- Pre-flight green; pack-verify checklist fully satisfied including the worker bundle; verify dir deleted.
- Runbook printed with all placeholders resolved; no outward command executed; no secret echoed.
