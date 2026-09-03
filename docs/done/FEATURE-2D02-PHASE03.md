# FEATURE-2D02-PHASE03 — Drop NuGet packaging & publication

**Status:** DONE
**Branch:** `feature/feature-2d02-phase03-de-nuget`
**Date:** 2026-09-03

## Summary

`Enigma.Msi` will never be published to nuget.org, and the repository now says so everywhere. This phase
removed the packaging machinery *and* every claim of publication, leaving the library as an in-repo
`ProjectReference` consumer at 1.0.0 and the desktop app's MSI as the only artifact this repository
releases.

**No behaviour changed.** The library, the worker, the app and all four test suites build and run exactly
as before — the app and the worker already consumed the library by `ProjectReference`, and the worker
already reached its hosts' output through `build/CopyWorkerOutput.targets`, which is untouched and is now
the *only* worker-deployment mechanism in the repo. 453 tests before, 453 tests after.

What went:

- **`build/Enigma.Msi.targets`** — deleted (`git rm`). It existed only to be imported by NuGet into a
  consuming project.
- **The packaging `PropertyGroup`** in `src/Enigma.Msi/Enigma.Msi.csproj` — `PackageId`, `Title`,
  `Description`, `PackageTags`, `PackageReadmeFile`, `PackageLicenseFile`, `RepositoryUrl`,
  `RepositoryType`, `PackageProjectUrl`, `PackageReleaseNotes` — with its header comment.
- **The README/LICENSE pack `ItemGroup`** and the `build/Enigma.Msi.targets` pack item.
- **The whole `PackEnigmaMsiWorkerPayload` target**, its `_EnigmaMsiWorker*` properties, its
  `Restore`/`Build` `MSBuild` invocations, its glob and its missing-payload guard — some 40 lines of
  comment explaining a workaround that no longer has anything to work around.

What stayed: `<Version>1.0.0</Version>` (moved into the first `PropertyGroup`), `<OutputType>`,
`<TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>`,
`<GenerateDocumentationFile>`, and the three `netstandard2.0`-conditional `PackageReference` groups
(`System.Buffers`, `PolySharp`, `System.Text.Json`).

## Files/modules touched

**Deleted**

- `build/Enigma.Msi.targets`

**Created**

- `docs/done/FEATURE-2D02-PHASE03.md` (this file)

**Modified**

| File | Change |
|---|---|
| `src/Enigma.Msi/Enigma.Msi.csproj` | Packaging metadata, pack items and pack target removed; `<Version>` moved into the first `PropertyGroup`; `IsPackable=false` + a `RefuseToPack` guard target added |
| `build/CopyWorkerOutput.targets` | Header comment rewritten — it no longer introduces itself as the counterpart of a deleted file, and states that it is the only worker-deployment mechanism |
| `src/Enigma.Msi/Build/MsiBuildService.cs` | `DefaultWorkerPath` doc: `CopyWorkerOutput.targets` from the worker project's build output, not `Enigma.Msi.targets` for package consumers |
| `src/Enigma.Msi/Build/MsiBuildServiceOptions.cs` | Same correction on `WorkerFolderName`, **and** on the class-level `<summary>` (which also referred to "the layout the `Enigma.Msi` package produces") |
| `README.md` | Reframed around the desktop app; NuGet badge and `dotnet add package` gone; build-from-source instructions; library described afterwards as the engine, consumed by `ProjectReference`; 1.3.0 what's-new callout |
| `RELEASENOTES.md` | Header paragraph and current-version table: two artifacts, one released as an MSI, one in-repo at 1.0.0, plus a note that the historical sections are deliberately not rewritten. Sections 1.2.0/1.1.0/1.0.0 untouched |
| `SECURITY.md` | "(NuGet)" dropped from the supported-versions table and the sentence above it (app row first); *Scope* now says "the MSBuild plumbing that copies it next to its hosts" |
| `docs/RELEASE.md` | Rewritten as the app release runbook — five sections, no pack/push/nuget.org verification, no release-flavour matrix |
| `docs/guides/building.md` | Four NuGet-wording corrections plus the *Worker discovery* section rewritten around `CopyWorkerOutput.targets` (see *Deviations*) |
| `docs/guides/worker-cli.md` | *Where the worker is* table and the CI-without-a-project paragraph: the worker project's own `bin/Release/net472/`, not a nupkg to unpack |
| `CLAUDE.md` | 11 edits — build-state block (pack claims marked historical, the never-published note settled, `FEATURE-2D02` `PHASE03` recorded as done), the architecture section's nupkg claim, two project-layout rows, the `net472` output-path paragraph, the `PackEnigmaMsiWorkerPayload` rationale, and the *Build & test* pack command |
| `docs/guides/README.md` | Guides-index intro: points a reader after the *app* at the desktop guide and the README, and states the library is on no feed (documentation freshness sweep) |
| `docs/roadmap.md` | `PHASE03` → `IN PROGRESS` → `DONE` |
| `docs/plan/FEATURE-2D02.md` | `PHASE03` status → `IN PROGRESS` → `DONE` |

## Deviations & follow-ups

**1. `IsPackable=false` does not fail loudly — a guard target was added.** The plan's recorded default said
`<IsPackable>false</IsPackable>` would make "an accidental `dotnet pack` fail loudly instead of producing a
worker-less package". Verified empirically: with `IsPackable=false` alone, `dotnet pack
src/Enigma.Msi/Enigma.Msi.csproj -c Release -o …` **exits 0, produces no package and says nothing** — a
silent no-op, not a failure. The substantive guarantee (no worker-less package can be produced) held, but
the stated mechanism did not. Rather than downgrade the intent silently, a three-line `RefuseToPack`
target (`BeforeTargets="Pack"`, one `<Error>`) now makes it real:

```
error : Enigma.Msi is not packable: it is consumed in-repo by ProjectReference and is published to no
feed. The only artifact this repository releases is the desktop app's MSI — see docs/RELEASE.md.
```

Exit code 1. `BeforeTargets` fires even though the SDK's own `Pack` target is conditioned off by
`IsPackable`. It has no effect on `dotnet build` or `dotnet test`, both re-verified after adding it.

**2. `docs/guides/building.md` needed more than the two spots the plan listed.** The plan named the
worker-on-disk table row and the *Worker discovery* paragraph. Three further hits were the same class of
wording and are covered by the phase's acceptance criterion ("the two guides … no longer describe the
library as published or publishable"), so they were corrected too: line 8 ("ships inside the `Enigma.Msi`
package"), line 25 (the requirements table row) and line 52 ("your layout differs from the one the package
produces").

More consequentially, the *Worker discovery* section documented an MSBuild property —
**`IncludeEnigmaMsiWorker`** — that was defined **only** in `build/Enigma.Msi.targets` and therefore
ceased to exist when that file was deleted; `CopyWorkerOutput.targets` has no such opt-out. The property
table and its XML sample were rewritten around what the surviving targets file actually offers
(`EnigmaMsiWorkerSourceDir`, `EnigmaMsiWorkerFolderName`, both set before the `Import`), with the import
contract shown, and the note that a project which never builds an MSI simply does not import the file —
there is nothing to opt out of. **No behaviour was added to `CopyWorkerOutput.targets`** to preserve
`IncludeEnigmaMsiWorker`; documenting reality was the narrower fix, and this phase changes no build
behaviour.

**3. `MsiBuildServiceOptions`'s class-level `<summary>` was corrected as well**, not only the line-15
`WorkerFolderName` doc the plan named — it carried the same "the layout the `Enigma.Msi` package produces"
claim.

**4. `RELEASENOTES.md` historical sections left as-is, per the plan.** The 1.0.0, 1.1.0 and 1.2.0 sections
still describe the library as a NuGet package, including "no NuGet package is published for this version"
and the 1.0.0 section's mention of `build/Enigma.Msi.targets`. They are the record of what each release
said at the time. The rewritten header states this explicitly so a reader is not misled. `PHASE04` adds
the 1.3.0 section that announces the change.

**5. `docs/roadmap.md`, `docs/plan/` and `docs/done/` untouched as claims.** The roadmap row
`FEATURE-5F00 | First release: Enigma.Msi 1.0.0 (NuGet) & Desktop app (MSI)` and every historical plan and
completion record keep their NuGet wording — they are the workflow's own record of what was planned and
done, not statements about current state.

**6. Line endings — recommendation only, no action taken.** Nothing was normalized as part of this phase.

**7. Documentation freshness sweep — two accepted edits, both in this dev's commit.** `CLAUDE.md`'s
project-layout row for `tests/Enigma.Msi.Desktop.UnitTests/` still read "ViewModel suite (the only one
that uses NSubstitute)", which `PHASE02`'s headless `SplashWindowTests` had made incomplete; it now reads
"ViewModel + headless-Avalonia suite". And `docs/guides/README.md`'s intro, out of step with the README's
app-first reframe, now sends a reader who wants the *app* to `desktop-app.md` and the repository README,
and states that the library is on no feed.

**Follow-up for `PHASE04`:** the README's what's-new callout already says 1.3.0 and covers all three
changes (output folder, splash, de-NuGet); `PHASE04` should confirm it matches the final release notes
rather than rewriting it.

## Build/test evidence

All commands run from the repository root on Windows 11, .NET SDK 10.0.100.

**Build — clean and warning-free:**

```
dotnet build Enigma.Msi.slnx -c Release

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Full test suite — all four suites, 453/453:**

```
dotnet test --solution Enigma.Msi.slnx -c Release

  Enigma.Msi.Desktop.UnitTests   (net10.0|x64)  passed
  Enigma.Msi.Worker.UnitTests    (net48|x64)    passed   <- WixSharp drift guards
  Enigma.Msi.UnitTests           (net8.0|x64)   passed
  Enigma.Msi.UnitTests           (net10.0|x64)  passed

  total: 453   failed: 0   succeeded: 453   skipped: 0
```

Identical to `PHASE02`'s 453, as expected — no behaviour was changed, no test was added or removed.
The `net472` worker suite and its WixSharp enum **drift guards** ran and passed.

**Pack is refused:**

```
dotnet pack src/Enigma.Msi/Enigma.Msi.csproj -c Release -o <tmp>
src\Enigma.Msi\Enigma.Msi.csproj(42,5): error : Enigma.Msi is not packable: …
exit code 1
```

Before the guard target was added, the same command exited 0 and produced nothing (see *Deviations* 1).

**The worker payload still lands** — `build/CopyWorkerOutput.targets` verified intact by inspecting the
app's build output:

```
src/Enigma.Msi.Desktop/bin/Release/net10.0/worker/   23 files
  Enigma.Msi.Worker.exe, Enigma.Msi.Worker.exe.config, WixSharp.dll,
  WixToolset.Dtf.WindowsInstaller.dll, WixToolset.Mba.Core.dll, mbanative.dll, …
```

Same 23-file payload the pack-verify recorded in `FEATURE-5F00-PHASE05`, now reaching the app the only
way that remains.

**Publication-claim sweep** (acceptance criterion): repo-wide, case-insensitive, for
`nuget|nupkg|dotnet add package|dotnet pack|Enigma.Msi.targets|tools/worker`, excluding `docs/done/` and
`docs/plan/`. Every surviving hit is one of:

- an explicit denial (`README.md`, `docs/RELEASE.md`, `CLAUDE.md`, `RELEASENOTES.md`'s new header);
- clearly-labelled history (`CLAUDE.md`'s `FEATURE-5F00` narrative, `RELEASENOTES.md`'s pre-1.3.0
  sections, `docs/roadmap.md`'s item titles);
- unrelated plumbing (`.gitignore`'s stock `*.nupkg` rules, `Enigma.Msi.Worker.csproj`'s comment about
  NuGet's *restore* graph walk ignoring `UndefineProperties` — consuming packages is not publishing one).

## Acceptance criteria

| Criterion | Status |
|---|---|
| Warning-free Release build; entire suite passes; library, worker and app behave as before | Met — 0 warnings, 453/453 |
| No packaging metadata, no pack items, no pack target; `IsPackable=false`; `<Version>1.0.0</Version>` survives | Met — plus a `RefuseToPack` guard (see *Deviations* 1) |
| `build/Enigma.Msi.targets` deleted, nothing references it; `CopyWorkerOutput.targets` intact and the payload still lands | Met — 23 files in the app's `$(OutDir)worker/` |
| README leads with the app, no NuGet badge, no `dotnet add package`, tells the reader to build from source | Met |
| `RELEASENOTES.md`, `SECURITY.md`, `docs/RELEASE.md`, the two guides and `CLAUDE.md` no longer describe the library as published or publishable; historical records untouched | Met |
| `docs/RELEASE.md` reads as the app release runbook, no pack/push sections | Met — §1 pre-release, §2 MSI build + profile clone rule, §3 merge, §4 tag, §5 post-release verification |
