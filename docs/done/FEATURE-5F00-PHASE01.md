# FEATURE-5F00-PHASE01 — Package metadata, packaging layout & license audit (DONE)

## Summary

Made `Enigma.Msi` packable. The library csproj now carries the full house set of twelve packaging
properties plus the items that actually pack `README.md` and `LICENSE.md`, and it assembles the
family-unusual part of the package — the `tools/worker/` payload the net472 worker is shipped as, and the
`build/Enigma.Msi.targets` that copies it into a consumer's output. `dotnet pack` from a tree with no
`bin`/`obj` anywhere produces a complete 3.8 MB nupkg with zero warnings.

Three things worth knowing before the later phases touch this:

- **The plan's `ProjectReference` from the library to the worker is impossible, and the payload is
  assembled by a target instead.** The worker already references the library (that is how the model stays
  single-sourced), so a build-order reference back to it closes a cycle; `ReferenceOutputAssembly="false"`
  does not break it. Verified on a throwaway pair of projects carrying exactly the three attributes
  `build/CopyWorkerOutput.targets` documents: `error MSB4006: There is a circular dependency in the target
  dependency graph involving target "_GenerateRestoreProjectPathWalk"`, at restore, before any compile.
  `PackEnigmaMsiWorkerPayload` instead invokes the worker's build through the `MSBuild` task
  (`RemoveProperties="TargetFramework"`, the same NETSDK1005 guard the in-repo hosts use) and globs its
  output into `None`/`Pack="true"` items. Deliberate and confirmed with the maintainer.

- **The payload glob has to live inside a target, and `Restore` has to be its own invocation.** Inside a
  target because on a clean tree the worker's output directory does not exist when the library is
  evaluated, so an evaluation-time glob silently yields nothing — exactly the empty `tools/worker/` the
  plan says must never ship. `Restore` separately (conditional on a missing assets file, carrying
  `ExcludeRestorePackageImports=true`) because a single `Targets="Restore;Build"` call reuses one project
  instance and builds against the evaluation from before the assets file existed: it packed successfully
  but emitted eleven `MSB3277` assembly-conflict warnings from the worker's reference resolution, meaning
  the packed worker had been built from a different reference set than the solution build produces. Split
  into two invocations, the cold-tree pack is warning-free.

- **The package redistributes two MS-RL assemblies.** Everything in the closure is redistributable, so
  nothing blocks the release, but `WixToolset.Dtf.WindowsInstaller` and `WixToolset.Mba.Core` (plus the
  native `mbanative.dll` from the latter) are Microsoft Reciprocal License, not MIT — see the audit below.

`%(RecursiveDir)` is kept on the payload's `PackagePath` even though the worker's output is currently flat
(23 files, no subdirectories): a future WixSharp version that ships a `runtimes/` folder would otherwise
be silently flattened into `tools/worker/`. Verified to preserve nesting.

## Files/modules touched

### Modified
- `src/Enigma.Msi/Enigma.Msi.csproj` — replaced the bootstrap comment that deferred packaging metadata to
  this phase with:
  - the twelve packaging properties: `PackageId` `Enigma.Msi`, `Version` `1.0.0`, `Title`, `Description`,
    `PackageTags`, `PackageReadmeFile`, `PackageLicenseFile`, `RepositoryUrl`, `RepositoryType` `git`,
    `PackageProjectUrl`, `PackageReleaseNotes` (placeholder, PHASE03 finalizes it) and the
    already-present `GenerateDocumentationFile`. `GeneratePackageOnBuild` and `IncludeSymbols` stay
    absent, both deliberate and commented;
  - the `None`/`Pack="true"` items for root `README.md` and `LICENSE.md`;
  - the `PackEnigmaMsiWorkerPayload` target and its three private properties — worker build invocation,
    an `Error` guard on a missing `Enigma.Msi.Worker.exe`, and the `tools/worker/` glob.
- `docs/roadmap.md`, `docs/plan/FEATURE-5F00.md` — item and PHASE01 statuses.

### Created
- `docs/done/FEATURE-5F00-PHASE01.md` (this file).

Nothing was created under `build/` — `build/Enigma.Msi.targets` and `build/CopyWorkerOutput.targets`
already had the layout this phase only had to finalize on the pack side.

## Third-party license audit

Scope is what the package **redistributes**: the library's runtime dependencies per TFM, and every file in
the `tools/worker/` payload. Licenses read from the restored `.nuspec` in the local NuGet cache; each file
mapped back to the package that owns it by inspecting the package contents.

### Library runtime dependencies (nuspec `<dependencies>`)

| Package | Version | TFMs | License | Redistribution |
|---|---|---|---|---|
| System.Text.Json | 10.0.10 | netstandard2.0 only | MIT | OK |
| System.Buffers | 4.6.1 | netstandard2.0 only | MIT | OK |

`net8.0`/`net10.0` have **no** dependencies — both packages are framework-provided there, which is why
they are referenced conditionally. `PolySharp` is compile-only (`PrivateAssets=all`) and correctly absent
from the nuspec; `xunit.v3`, `coverlet.collector` and `NSubstitute` are test-only. None of the three ships.

### `tools/worker/` payload — 23 files

| File(s) | Owning package | Version | License | Redistribution |
|---|---|---|---|---|
| `Enigma.Msi.Worker.exe`, `.exe.config`, `.pdb` | this repository | 1.0.0 | MIT (`LICENSE.md`) | OK |
| `Enigma.Msi.dll`, `.pdb`, `.xml` | this repository | 1.0.0 | MIT (`LICENSE.md`) | OK |
| `WixSharp.dll`, `WixSharp.Msi.dll`, `WixSharp.UI.dll`, `WixSharp.UI.WPF.dll`, `WixSharp.MsiEventHost.exe` | WixSharp_wix4.bin (via WixSharp_wix4) | 2.14.1 | MIT | OK |
| `WixToolset.Dtf.WindowsInstaller.dll` | WixToolset.Dtf.WindowsInstaller | 4.0.1 | **MS-RL** | OK — see note |
| `WixToolset.Mba.Core.dll`, `mbanative.dll` | WixToolset.Mba.Core | 4.0.1 | **MS-RL** | OK — see note |
| `System.Text.Json.dll` | System.Text.Json | 10.0.10 | MIT | OK |
| `System.Text.Encodings.Web.dll` | System.Text.Encodings.Web | 10.0.10 | MIT | OK |
| `System.IO.Pipelines.dll` | System.IO.Pipelines | 10.0.10 | MIT | OK |
| `Microsoft.Bcl.AsyncInterfaces.dll` | Microsoft.Bcl.AsyncInterfaces | 10.0.10 | MIT | OK |
| `System.Memory.dll` | System.Memory | 4.6.3 | MIT | OK |
| `System.Buffers.dll` | System.Buffers | 4.6.1 | MIT | OK |
| `System.Numerics.Vectors.dll` | System.Numerics.Vectors | 4.6.1 | MIT | OK |
| `System.Runtime.CompilerServices.Unsafe.dll` | System.Runtime.CompilerServices.Unsafe | 6.1.2 | MIT | OK |
| `System.Threading.Tasks.Extensions.dll` | System.Threading.Tasks.Extensions | 4.6.3 | MIT | OK |

`System.ValueTuple` 4.6.2 (MIT) is in the worker's restore graph but does **not** ship — net472 provides
it as a facade, which is what the binding-redirect note in the worker csproj is about.

**No non-redistributable finding — nothing to stop the release for.** The two MS-RL packages are the WiX
Toolset's own assemblies, pulled in by WixSharp; MS-RL permits redistribution in compiled form, and its
reciprocity applies to source files containing MS-RL code, not to a package that bundles the unmodified
binaries next to MIT code. It does mean the nupkg is **not** wholly MIT even though `LICENSE.md` is — see
the follow-up below.

## Deviations & follow-ups

- **Deviation (confirmed with the maintainer): PHASE01 step 2's build-order `ProjectReference` from
  `Enigma.Msi.csproj` to the worker is not implementable** — it is a circular project reference (MSB4006
  at restore). Replaced by the `PackEnigmaMsiWorkerPayload` target, which meets the same stated intent:
  packing from a clean tree produces a populated `tools/worker/`, verified. The plan text for step 2 was
  left as written; this record is the deviation.
- `BeforeTargets="_GetPackageFiles"` hooks a NuGet-internal target. It is the only place a `Pack="true"`
  item can be contributed once per pack (the public `TargetsForTfmSpecificContentInPackage` runs per TFM
  and would add the payload three times). Verified on SDK 10.0.301; worth re-checking on a major SDK bump.
- **Follow-up for PHASE03:** the package ships MS-RL components, so a third-party attribution note is
  warranted — either a short "Third-party components" section in the packed README/`RELEASENOTES.md` or a
  `THIRD-PARTY-NOTICES.md`. Out of scope here (PHASE01 only audits and records); listing it so the
  community-files phase decides deliberately rather than by omission.
- **Follow-up for PHASE05:** pack-verify can be shorter than planned — the nuspec and payload checks it
  describes were all run here (results in the evidence below) and only need re-running against the final
  `PackageReleaseNotes` and README from PHASE03.
- The packed `README.md` is still the 13-byte placeholder. Harmless for now (NuGet only errors on an
  *empty* readme), and PHASE03 replaces it — but the package must not be pushed before it does.
- Line endings: no CRLF churn; the three touched files are LF-only and the diff is content-only.

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`, both
  incrementally and from a fully clean tree (every `bin`/`obj` under `src/` and `tests/` deleted first).
- **Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release` → **368 passed, 0 failed, 0 skipped**
  across all four suites (`Enigma.Msi.UnitTests` on net8.0 and net10.0, `Enigma.Msi.Worker.UnitTests` on
  net472, `Enigma.Msi.Desktop.UnitTests` on net10.0). No tests were added: this phase changes packaging
  metadata and a pack-time target, neither of which is reachable from a unit test — the pack itself is the
  verification.
- **Cold-tree pack** (`dotnet pack src/Enigma.Msi/Enigma.Msi.csproj -c Release` into a throwaway dir
  outside the repository, after deleting every `bin`/`obj`): succeeded with **no warnings**. Contents —
  36 entries, 3.8 MB:
  - `lib/netstandard2.0/`, `lib/net8.0/`, `lib/net10.0/` — each `Enigma.Msi.dll` + `Enigma.Msi.xml`;
  - `tools/worker/` — 23 files, the full worker closure listed in the audit above;
  - `build/Enigma.Msi.targets`;
  - `README.md` (13 bytes, non-empty), `LICENSE.md` (1 063 bytes).
- **Nuspec** (extracted and read): `<version>1.0.0`, `<title>` and `<description>` present,
  `<license type="file">LICENSE.md`, `<readme>README.md`, `<releaseNotes>` the placeholder,
  `<repository type="git" url="https://github.com/enigmalibs/Enigma.Msi" commit="…">`, `<tags>`,
  `<copyright>` from `Directory.Build.props`. Dependency groups: `net10.0` and `net8.0` empty,
  `.NETStandard2.0` = `System.Buffers 4.6.1` + `System.Text.Json 10.0.10`.
- **`tools/worker/` guard:** packing with the payload directory overridden to a nonexistent path fails as
  intended — `error : Cannot pack Enigma.Msi: no worker payload under 'C:/nope/'. Build the solution in
  this configuration first (dotnet build Enigma.Msi.slnx -c Release).`
- **`%(RecursiveDir)` preservation:** proven on the throwaway project pair (`sub/nested.txt` landed at
  `tools/worker/sub/nested.txt`), since the real worker output happens to be flat.
- All throwaway pack directories and probe projects were created outside the repository and deleted;
  `git status` shows only the three intended files.
