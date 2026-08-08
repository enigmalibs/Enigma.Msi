# CLAUDE.md

Guidance for Claude Code (and other AI agents) working in this repository.

## What this is

**Enigma.Msi** builds Windows MSI installers from a single declarative model, on top of
[WixSharp](https://github.com/oleg-shilo/wixsharp) (WiX v4). It is the successor of the older
`MsiBuilder` project. A consumer describes the package — app identity, install scope, source folder,
shortcuts, control-panel info, compression, managed-UI dialogs — as an `MsiPackage`, and the library
turns it into an `.msi`. A companion Avalonia desktop app drives the same model through a UI.

> **Build state.** The library's declarative surface (`FEATURE-43A9-PHASE02`) and the net472 worker
> (`FEATURE-43A9-PHASE03`) are in place: the `MsiPackage` model with its WixSharp-free enum mirrors,
> the `.msipkg.json` serialization contract, the aggregating validator, and the worker that translates
> a package to WixSharp and builds the MSI — usable on its own through `Enigma.Msi.Worker.exe build
> <file.msipkg.json>`. Still to come: the build client and packaging plumbing (PHASE04), and the
> Avalonia desktop app (PHASE05) — see `docs/plan/FEATURE-43A9.md`. Types referenced below that those
> phases deliver (`IMsiBuildService`, the worker-copy targets) do not exist yet.

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
src/Enigma.Msi/                      The library — the only public/packable one (model, validation, JSON, build client)
src/Enigma.Msi.Worker/               net472 console exe — WixSharp translation + the actual MSI build
src/Enigma.Msi.Desktop/              Avalonia desktop app                                               (PHASE05)
tests/Enigma.Msi.UnitTests/          xUnit v3 suite for the library
tests/Enigma.Msi.Worker.UnitTests/   net472 suite: mapping + WixSharp enum drift guards
tests/Enigma.Msi.Desktop.UnitTests/  ViewModel suite                                                    (PHASE05)
docs/                                Guides, samples, and the dev-workflow tracking artifacts
```

## Target frameworks & dependencies

- **`src/Enigma.Msi`** multi-targets **`netstandard2.0;net8.0;net10.0`**. `netstandard2.0` is not
  legacy caution — it is what lets the `net472` worker consume the *same* model assembly as modern
  consumers. `net8.0`/`net10.0` are the two currently-supported LTS releases.
- **`src/Enigma.Msi.Worker`** is **`net472`**, forced by WixSharp being .NET Framework-only; its test
  project mirrors that TFM. This is a documented deviation from the house `net10.0` app default.
- **`src/Enigma.Msi.Desktop`** is `net10.0` (Avalonia; plain TFM, no `-windows` suffix).
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
