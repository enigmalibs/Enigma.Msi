# CLAUDE.md

Guidance for Claude Code (and other AI agents) working in this repository.

## What this is

**Enigma.Msi** builds Windows MSI installers from a single declarative model, on top of
[WixSharp](https://github.com/oleg-shilo/wixsharp) (WiX v4). It is the successor of the older
`MsiBuilder` project. A consumer describes the package — app identity, install scope, source folder,
shortcuts, control-panel info, compression, managed-UI dialogs — as an `MsiPackage`, and the library
turns it into an `.msi`. A companion Avalonia desktop app drives the same model through a UI.

> **Build state.** `FEATURE-43A9` is complete (`PHASE01`…`PHASE05`) and works end to end: the
> `MsiPackage` model with its WixSharp-free enum mirrors, the `.msipkg.json` serialization contract, the
> aggregating validator, the net472 worker that translates a package to WixSharp and builds the MSI —
> usable on its own through `Enigma.Msi.Worker.exe build <file.msipkg.json>` — the `IMsiBuildService`
> build client that drives it out of process with the MSBuild plumbing that puts the worker where the
> client looks for it, and the Avalonia desktop app over the same model. The first release
> (`FEATURE-5F00`) is under way: `PHASE01` is done — the library is packable, and `dotnet pack` produces a
> complete nupkg (`lib/` for all three TFMs, the `tools/worker/` payload, `build/Enigma.Msi.targets`,
> README, LICENSE) — `PHASE02`: the five per-category guides plus their index live in `docs/guides/`,
> every snippet in them compiled and asserted against the real assembly — and `PHASE03`: the packed
> `README.md`, `RELEASENOTES.md` (covering both the library and the desktop app at 1.0.0) and
> `SECURITY.md` are authored, and `<PackageReleaseNotes>` is finalized — and `PHASE04`: the desktop app
> is releasable (explicit `<Version>` 1.0.0, an application icon, and `msiProfiles/` holding its own
> `.msipkg.json`), and **the dogfood loop is closed** — the worker built the app's 17 MB installer from
> that profile, which is also `FEATURE-43A9`'s manual end-to-end acceptance step, performed — and
> `PHASE05`: `docs/RELEASE.md` is the release runbook, and the pack-verify confirmed from a real
> `dotnet pack` that the nupkg carries what it should (three `lib/` TFMs with their XML docs, the 23-file
> `tools/worker/` payload byte-identical to the worker's build output, `build/Enigma.Msi.targets`, an
> embedded non-empty README and LICENSE, the intended dependency floors).
>
> **`FEATURE-5F00` is complete; 1.0.0 is prepared but not published.** What remains is the maintainer's and
> is deliberately outside the repo: this repository still has **no git remote and no tags**, so
> `github.com/enigmalibs/Enigma.Msi` must be created and `main` pushed before the runbook's tag/pack/push
> steps mean anything. Nothing here ever runs an outward-facing command — follow `docs/RELEASE.md`.
>
> **`FEATURE-6C35` is complete (`PHASE01`, `PHASE02`); the desktop app 1.1.0 is prepared but not built.**
> `PHASE01` was the app-only UI polish: permanent variable hints under the three token fields, a modal
> build-progress card (its Cancel is now the *only* cancel affordance — the toolbar's is gone) driven
> through the `IBuildProgressService` seam over the control library's overlay, shortcuts as one
> self-removing card per row with the selection concept deleted, icons on the five section expanders, and
> the incomplete-package message in the theme's warning brush. `PHASE02` was the release prep: the app
> declares `<Version>1.1.0`, `msiProfiles/Enigma.Msi.Desktop.1.1.0.msipkg.json` is committed (fresh
> `productId`, `upgradeCode` reused verbatim — that reuse is what makes 1.1.0 *upgrade* an installed
> 1.0.0), and `RELEASENOTES.md` is now a newest-first multi-version document.
> **The library stays at 1.0.0 and is not re-released** — an app-only release runs no `dotnet pack` and no
> NuGet push; `docs/RELEASE.md` §0 says which sections apply to which release flavour.
>
> **Outstanding, and Windows-only:** the 1.1.0 dogfood MSI has **not** been built — `PHASE02` ran on Linux,
> where there is no WiX CLI and the `net472` worker cannot start; the commands are in
> `docs/done/FEATURE-6C35-PHASE02.md`. The **test-suite** half of that debt is settled: `FEATURE-74C4`
> `PHASE01` ran the whole solution on Windows — 392 passed, 0 skipped — so the 32 Windows-only
> `Enigma.Msi.UnitTests` cases and the entire `Enigma.Msi.Worker.UnitTests` suite (mapping + the WixSharp
> enum drift guards) are exercised again.

## Architecture

**One declarative model, three consumers.** `MsiPackage` is *the* input everywhere: the worker
consumes it, the saved `.msipkg.json` profile is its serialization, and the desktop app binds to it.
There is deliberately **no** mirror DTO layer and no fluent builder chain — a single representation
is the load-bearing design decision of this repo (its predecessor kept three hand-synced ones).

**The net472 split is not optional.** WixSharp (`WixSharp_wix4`) is .NET Framework-only, so the
process that touches WixSharp must be `net472`. Everything else is modern .NET, and the two halves
talk over a small out-of-process protocol:

```
Enigma.Msi (library)                       Enigma.Msi.Worker (net472 exe)
  IMsiBuildService.BuildAsync(package)
      writes  request.json  ───────────►  --request <req> --result <res>
      streams stdout/stderr ◄───────────  WixSharp ManagedProject → BuildMsi()
      reads   result.json   ◄───────────  writes MsiBuildResult
```

The worker is referenced by nothing at runtime — it is **discovered on disk** (options override,
else `AppContext.BaseDirectory/worker/Enigma.Msi.Worker.exe`) and shipped beside its host: inside
the `Enigma.Msi` nupkg under `tools/worker/`, and next to the desktop app's output.

The worker also has a headless CLI mode (`Enigma.Msi.Worker.exe build <file.msipkg.json>`) so CI can
build an MSI without the library. Exit codes are a contract: `0` success, `1` the operation failed on
well-formed input (validation or MSI build), `2` bad arguments / unparseable input / unexpected error.

**Invariants worth protecting:**

- **WixSharp never leaks onto the public surface.** The library's enums (`InstallScope`,
  `CompressionLevel`, `Wui`, `Dialog`) are WixSharp-free mirrors, mapped inside the worker and
  guarded by reflection **drift tests** against WixSharp's real member sets — a WixSharp upgrade that
  renames or adds a member must fail loudly, not silently mis-map.
- **Validation aggregates.** `IMsiPackageValidator` returns *every* violation as `{ Path, Message }`,
  never a first-error bool. In-memory rules are separated from environment (disk) rules so the UI can
  validate as-you-type.
- **Cancellation kills the process tree.** A cancelled build must not leave orphaned `wix` processes.
- **The build log is append-only.** Never rebuild the whole log by string concatenation per line.

## Project layout

```
Enigma.Msi.slnx                      Solution (SLNX format)
Directory.Build.props                Shared build defaults (Authors, Copyright, LangVersion 14, Nullable, TreatWarningsAsErrors)
Directory.Packages.props             Central Package Management (all package versions pinned here)
.editorconfig                        Code style + analyzer severities
global.json                          SDK 10.0.100 (latestFeature); test runner = Microsoft.Testing.Platform
build/Enigma.Msi.targets             Packed into the nupkg's build/ — copies tools/worker/ to a consumer's $(OutDir)worker/
build/CopyWorkerOutput.targets       In-repo counterpart — same layout, from a project's build output (imported by worker hosts)
src/Enigma.Msi/                      The library — the only public/packable one (model, validation, JSON, build client)
src/Enigma.Msi.Worker/               net472 console exe — WixSharp translation + the actual MSI build
src/Enigma.Msi.Desktop/              Avalonia desktop app — the UI over the same MsiPackage (WinExe, hosts the worker)
tests/Enigma.Msi.UnitTests/          xUnit v3 suite for the library
tests/Enigma.Msi.StubWorker/         Test asset, not a suite: a console exe speaking the worker protocol, spawned by the tests
tests/Enigma.Msi.Worker.UnitTests/   net472 suite: mapping + WixSharp enum drift guards
tests/Enigma.Msi.Desktop.UnitTests/  ViewModel suite (the only one that uses NSubstitute)
msiProfiles/                         Release profiles for MSIs this repo builds of itself (currently the desktop app)
docs/                                Roadmap, plan and completion records (the dev-workflow tracking artifacts)
docs/RELEASE.md                      The release runbook: pre-flight, tag, pack, push, and the app-MSI build
docs/guides/                         Per-category guides + index (repo-only — never packed, so relative links are fine)
```

A project that hosts the worker imports `build/CopyWorkerOutput.targets` **and** declares its own
build-order `ProjectReference` to `src/Enigma.Msi.Worker` with all three of
`ReferenceOutputAssembly="false"`, `SkipGetTargetFrameworkProperties="true"` and
`UndefineProperties="TargetFramework"` — the last one is not optional, without it the host's TFM flows
down as a global property and the build fails with NETSDK1005. The targets file documents the contract.

**`bin/$(Configuration)/net472/` is the other half of that contract**, and the worker guarantees it with
`<AppendRuntimeIdentifierToOutputPath>false</…>`: both `CopyWorkerOutput.targets` and the library's pack
target glob that exact path, and a host that publishes RID-specifically (`dotnet publish -r win-x64`,
which is how the desktop app's release payload is produced) flows its `RuntimeIdentifier` down here.
Without the property the worker lands in a RID-suffixed folder and both globs come up empty — or worse,
copy a stale worker. Do **not** "fix" this by adding `RuntimeIdentifier` to `UndefineProperties`: NuGet's
restore graph walk ignores `UndefineProperties`, so restore resolves the host's RID while the build falls
back to the worker's own inferred `win-x86`, and it fails with NETSDK1047.

**`src/Enigma.Msi` is the one worker host that cannot use that contract.** The worker references the
library, so a `ProjectReference` back to the worker — even build-order-only — closes a cycle and fails
restore with MSB4006; `ReferenceOutputAssembly="false"` does not break it. The `tools/worker/` payload is
therefore assembled by the `PackEnigmaMsiWorkerPayload` target in `Enigma.Msi.csproj`, which builds the
worker through the `MSBuild` task and globs its output into `Pack="true"` items. Two details there are
load-bearing: the glob must live **inside** the target (on a clean tree the worker's output does not exist
when the library is evaluated, so an evaluation-time glob silently packs nothing), and `Restore` must be a
**separate** `MSBuild` invocation from `Build` (sharing one project instance builds against the
pre-assets evaluation and emits MSB3277 reference conflicts). A guard fails the pack if the payload is
missing rather than shipping a nupkg whose every `BuildAsync` call would fail.

## Target frameworks & dependencies

- **`src/Enigma.Msi`** multi-targets **`netstandard2.0;net8.0;net10.0`**. `netstandard2.0` is not
  legacy caution — it is what lets the `net472` worker consume the *same* model assembly as modern
  consumers. `net8.0`/`net10.0` are the two currently-supported LTS releases.
- **`src/Enigma.Msi.Worker`** is **`net472`**, forced by WixSharp being .NET Framework-only; its test
  project mirrors that TFM. This is a documented deviation from the house `net10.0` app default.
- **`src/Enigma.Msi.Desktop`** is `net10.0` `WinExe` (Avalonia; plain TFM, no `-windows` suffix — the
  app's Windows-only nature comes from what it drives, not from its TFM). Its UI stack is a **coupled
  set** pinned together: Avalonia **12.1.1** (+ `.Desktop`, `.Themes.Fluent`, `.Fonts.Inter`),
  `Enigma.Avalonia.Desktop` **1.0.0** and `Enigma.Icons.Avalonia` **1.0.0** — the latter two are built
  against Avalonia 12.1.x, so bump all four or none. `AvaloniaUI.DiagnosticsSupport` is Debug-only via a
  conditional `IncludeAssets`/`PrivateAssets`. Beware two API details: `Enigma.Avalonia.Desktop`'s
  editors derive from `TextBox`, whose `Watermark` Avalonia 12 obsoletes — use `PlaceholderText`, or the
  XAML compiler's `AVLN5001` fails the zero-warnings build; and its picker services' path-returning
  overloads are C# 14 `extension` members, so they cannot be substituted in a test (which is why the app
  puts its own `IPathPickerService` in front of them).
- `System.Buffers` and **PolySharp** (compile-only, `PrivateAssets=all`) are referenced on
  **netstandard2.0 only** — they are framework-provided or unnecessary on net8.0+, and an
  unconditional reference raises NU1510 and fails the zero-warnings build.
- Reference **`WixSharp_wix4`** only — never `WixSharp_wix4.bin`, which is already a dependency of it.
- All versions live in `Directory.Packages.props` (do not put `Version=` on a `<PackageReference>`).

**Building an actual MSI additionally requires Windows and the WiX CLI:**

```bash
dotnet tool install --global wix
```

The library's preflight API reports its absence with that exact hint. End-to-end MSI generation is a
**manual** acceptance step, not an automated test.

## Build & test

Zero-warning builds are enforced (`TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`), so
warnings fail the build.

```bash
# Build the whole solution (Release)
dotnet build Enigma.Msi.slnx -c Release

# Run the full test suite (Microsoft.Testing.Platform runner, per global.json)
dotnet test --solution Enigma.Msi.slnx -c Release

# Produce the NuGet package
dotnet pack src/Enigma.Msi/Enigma.Msi.csproj -c Release -o ./artifacts
```

On the .NET 10 SDK in MTP mode, `dotnet test <solution>` is **rejected** — the solution must be
passed through the explicit `--solution` flag, as above.

Tests are **MTP-native**: `xunit.v3` + `coverlet.collector`, with **no** `Microsoft.NET.Test.Sdk` and
no `xunit.runner.visualstudio`.

## Conventions

- **Language/style.** C# 14, `Nullable=enable`, `ImplicitUsings=disable` (declare `using`s
  explicitly). File-scoped namespaces, namespace == folder, one type per file. Follow
  `.editorconfig`; do not introduce warnings.
- **Documentation.** Public APIs carry XML doc comments (`GenerateDocumentationFile=true`).
- **Profiles.** Saved packages are `<name>.msipkg.json` and carry a `schemaVersion`. The legacy
  `<App>.msiprofile.<version>.json` format is a deliberate clean break — **there is no importer**.
- **`.gitignore` gotcha.** The `*.msi` rule is paired with a `!*.msi/` negation: gitignore patterns
  match directories as well as files, and `src/Enigma.Msi` would otherwise be ignored wholesale.
  Do not drop the negation.
- **Line endings.** `.gitattributes` enforces `* text=auto eol=lf`. Never "fix" line endings as part
  of unrelated work.

## Dev workflow (tracked work)

This repo plans and tracks work through a house workflow:

- `docs/roadmap.md` — the single registry of every work item (`FEATURE-HHHH`, `BUG-HHHH`,
  `CODE-REVIEW-HHHH`; large items are split into `-PHASENN` phases).
- `docs/plan/<ID>.md` — the full plan for each item (the contract a build implements).
- `docs/done/<ID>.md` — a completion record per finished item/phase.

Each unit of work gets its own branch (`feature/…`, `bugfix/…`, `review/…`), cut from the current
`HEAD`. A unit is **done** only when the build is warning-free, the whole test suite passes, the
plan's acceptance criteria are met, the roadmap/plan statuses are updated, and the completion doc is
written. Commits are left to the maintainer.
