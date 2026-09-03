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
> (`FEATURE-5F00`) is under way: `PHASE01` made the library packable, with a `dotnet pack` that produced a
> complete nupkg (`lib/` for all three TFMs, a `tools/worker/` payload, `build/Enigma.Msi.targets`, README,
> LICENSE) — **all of that is gone; see `FEATURE-2D02` `PHASE03`** — `PHASE02`: the five per-category
> guides plus their index live in `docs/guides/`,
> every snippet in them compiled and asserted against the real assembly — and `PHASE03`: the packed
> `README.md`, `RELEASENOTES.md` (covering both the library and the desktop app at 1.0.0) and
> `SECURITY.md` are authored, and `<PackageReleaseNotes>` is finalized — and `PHASE04`: the desktop app
> is releasable (explicit `<Version>` 1.0.0, an application icon, and `msiProfiles/` holding its own
> `.msipkg.json`), and **the dogfood loop is closed** — the worker built the app's 17 MB installer from
> that profile, which is also `FEATURE-43A9`'s manual end-to-end acceptance step, performed — and
> `PHASE05`: `docs/RELEASE.md` became the release runbook, and a pack-verify confirmed from a real
> `dotnet pack` that the nupkg carried what it should (three `lib/` TFMs with their XML docs, a 23-file
> `tools/worker/` payload byte-identical to the worker's build output, `build/Enigma.Msi.targets`, an
> embedded non-empty README and LICENSE, the intended dependency floors). The packaging half of that is
> **history**, not current state.
>
> **`FEATURE-5F00` is complete, and the library was never published — nor will it be.** `origin` is
> `https://github.com/enigmalibs/Enigma.Msi.git` and the tags `1.0.0` and `1.1.0` exist, but no version of
> `Enigma.Msi` was ever packed and pushed to NuGet, and `FEATURE-2D02` `PHASE03` settled the question:
> **the library is an in-repo `ProjectReference` consumer at 1.0.0 with `IsPackable=false`**, and the
> desktop app's MSI is the only artifact this repository releases. Nothing here ever runs an
> outward-facing command — follow `docs/RELEASE.md`.
>
> **`FEATURE-6C35` is complete (`PHASE01`, `PHASE02`) — the desktop app 1.1.0, prepared but never built as
> an MSI.** `PHASE01` was the app-only UI polish: permanent variable hints under the three token fields, a
> modal build-progress card (its Cancel is now the *only* cancel affordance — the toolbar's is gone) driven
> through the `IBuildProgressService` seam over the control library's overlay, shortcuts as one
> self-removing card per row with the selection concept deleted, icons on the five section expanders, and
> the incomplete-package message in the theme's warning brush. `PHASE02` was the release prep: the app
> declared `<Version>1.1.0`, `msiProfiles/Enigma.Msi.Desktop.1.1.0.msipkg.json` is committed (fresh
> `productId`, `upgradeCode` reused verbatim — that reuse is what makes each release *upgrade* the
> installed one), and `RELEASENOTES.md` became a newest-first multi-version document. Its dogfood MSI was
> never built (`PHASE02` ran on Linux) and 1.2.0 has superseded it; the profile is kept as history.
> **The library stays at 1.0.0 and is not re-released** — which at the time meant skipping the runbook's
> pack/push sections; since `FEATURE-2D02` `PHASE03` there are no such sections and `docs/RELEASE.md` is
> the app release runbook outright.
>
> **`FEATURE-74C4` is complete (`PHASE01`…`PHASE03`) — the app-only 1.2.0, prepared *and* verified.**
> `PHASE01` gave the app a visual identity: a ~2 s dismissable `SplashWindow` that hands over to the real
> window, and an About dialog behind an icon-only button at the right of the command bar, both fed by
> `AppInfo` and `Assets/logo.png`. `PHASE02` removed the tedium from first-time setup: **Control Panel
> information is on by default for a new package** (`Reset()` only — `LoadFrom` still derives the toggle
> from the profile, so an existing 1.0.0/1.1.0 profile opens unchanged), and a one-page **quick start** —
> six answers in, a form that passes Validate with zero problems out, deriving the install path, the output
> folder, the MSI name, the product icon and two shortcuts. It opens from the toolbar and once by itself on
> an empty form, and replaces the package after a Yes/No confirmation when there is work to lose.
> `PHASE03` was the release prep, and it **closed the Windows verification debt 1.1.0 left**: the app
> declares `<Version>1.2.0`, `msiProfiles/Enigma.Msi.Desktop.1.2.0.msipkg.json` is committed, the whole
> solution builds warning-free and all **444** tests pass on Windows (the previously Windows-only library
> cases and the entire `net472` worker suite, drift guards included), and the **dogfood MSI is built** —
> `artifacts\Enigma.Msi.Desktop.msi`, 17.25 MB, from the committed profile with WiX 7.0.0.
> Install/launch/uninstall verification stays the maintainer's step.
>
> **`FEATURE-2D02` is under way — the app-only 1.3.0, plus settling what this repository publishes.**
> `PHASE01` is done: the quick start asks **seven** answers, not six. **Output folder** sits after Release
> folder and is stated rather than derived — 1.2.0 silently used the release folder's *parent*, and
> `ParentDirectoryOrSelf` is gone with its single call site. It is required (Apply gates on it) but
> otherwise unconstrained: an output folder *inside* the release folder is accepted with a hint and no
> warning, and one that does not exist is still reported once, by the Problems pane, because the dialog
> applies string and parse rules only. Its Browse button is the one that is **ungated** — the icon and
> executable pickers open *at* the release folder, the output folder depends on nothing. 447 tests pass.
> `PHASE02` is done too: the **splash shows the logo, `Enigma.Msi` and `Version X.Y.Z` and nothing else**
> at 420×260 — the tagline and the author line are off it (both stay in About) — and with two lines left
> `SplashViewModel` is **deleted**: the markup reads `AppInfo` through `{x:Static}`, so the splash has no
> ViewModel, no DI registration and no `DataContext`. Its behaviour is untouched (2 s timer, click or key,
> `App`-owned handover). That is also what brought the repository its **first headless-Avalonia suite** —
> `SplashWindowTests` over the real markup, with an assembly-level `[AvaloniaTestApplication]` hook
> building the app's own `App` on `UseHeadless` (the test session sets up no lifetime, so the app's
> composition never runs). 453 tests pass. `PHASE03` is done: **the NuGet packaging is gone.**
> `build/Enigma.Msi.targets`, the whole `PackEnigmaMsiWorkerPayload` target, the packaging
> `PropertyGroup` and the README/LICENSE pack items are deleted; the library keeps `<Version>1.0.0`, moved
> into its first `PropertyGroup`, and declares `IsPackable=false` **plus a `RefuseToPack` guard target** —
> `IsPackable=false` alone makes `dotnet pack` a *silent* no-op (exit 0, no package, nothing said), and
> the guard is what turns that into an error naming the reason. `README.md` now leads with the app (obtained
> by building from source — no feed, no Releases page), `docs/RELEASE.md` is the app release runbook with
> no pack/push sections, and `RELEASENOTES.md`, `SECURITY.md` and the two affected guides describe the
> library as in-repo. **Nothing about the build changed**: `build/CopyWorkerOutput.targets` is untouched
> and is now the *only* worker-deployment mechanism, and the same 453 tests pass. Only `PHASE04`
> (release 1.3.0) remains.
>
> **Two traps the shared `ContentDialog` sets**, both worked around and worth knowing before adding a
> third dialog: `IContentDialogService` owns **one** host whose reset *assigns* `IsPrimaryButtonEnabled`
> instead of clearing it — so a binding onto the host must be disposed when the dialog closes, or it goes
> on gating the next dialog's button — and `ShowAsync` does **not** reset the six `Dialog*` size
> properties, so every dialog states its own size rather than inheriting the last one's. The host also
> does not expose its `Title` to UI Automation: assert on the content or a button name instead.

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
else `AppContext.BaseDirectory/worker/Enigma.Msi.Worker.exe`) and shipped beside its host:
`build/CopyWorkerOutput.targets` copies the worker project's own build output into each host's
`$(OutDir)worker/` and `$(PublishDir)worker/`.

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
build/CopyWorkerOutput.targets       Copies the net472 worker into a host's $(OutDir)worker/ (imported by every worker host)
src/Enigma.Msi/                      The library — the only public one (model, validation, JSON, build client); not packable
src/Enigma.Msi.Worker/               net472 console exe — WixSharp translation + the actual MSI build
src/Enigma.Msi.Desktop/              Avalonia desktop app — the UI over the same MsiPackage (WinExe, hosts the worker)
tests/Enigma.Msi.UnitTests/          xUnit v3 suite for the library
tests/Enigma.Msi.StubWorker/         Test asset, not a suite: a console exe speaking the worker protocol, spawned by the tests
tests/Enigma.Msi.Worker.UnitTests/   net472 suite: mapping + WixSharp enum drift guards
tests/Enigma.Msi.Desktop.UnitTests/  ViewModel + headless-Avalonia suite (the only one using NSubstitute or a UI platform)
msiProfiles/                         Release profiles for MSIs this repo builds of itself (currently the desktop app)
docs/                                Roadmap, plan and completion records (the dev-workflow tracking artifacts)
docs/RELEASE.md                      The app release runbook: pre-flight, MSI build, merge, tag, verification
docs/guides/                         Per-category guides + index (repo-only — never packed, so relative links are fine)
```

A project that hosts the worker imports `build/CopyWorkerOutput.targets` **and** declares its own
build-order `ProjectReference` to `src/Enigma.Msi.Worker` with all three of
`ReferenceOutputAssembly="false"`, `SkipGetTargetFrameworkProperties="true"` and
`UndefineProperties="TargetFramework"` — the last one is not optional, without it the host's TFM flows
down as a global property and the build fails with NETSDK1005. The targets file documents the contract.

**`bin/$(Configuration)/net472/` is the other half of that contract**, and the worker guarantees it with
`<AppendRuntimeIdentifierToOutputPath>false</…>`: `CopyWorkerOutput.targets` globs that exact path, and a
host that publishes RID-specifically (`dotnet publish -r win-x64`, which is how the desktop app's release
payload is produced) flows its `RuntimeIdentifier` down here. Without the property the worker lands in a
RID-suffixed folder and the glob comes up empty — or worse, copies a stale worker. Do **not** "fix" this
by adding `RuntimeIdentifier` to `UndefineProperties`: NuGet's restore graph walk ignores
`UndefineProperties`, so restore resolves the host's RID while the build falls back to the worker's own
inferred `win-x86`, and it fails with NETSDK1047.

**`src/Enigma.Msi` itself is not a worker host, and cannot become one.** The worker references the
library, so a `ProjectReference` back to the worker — even build-order-only — closes a cycle and fails
restore with MSB4006, and `ReferenceOutputAssembly="false"` does not break it. The library therefore never
copies a worker anywhere: it only *discovers* one at run time, and putting it there is the hosting
project's job. Until `FEATURE-2D02` `PHASE03` the library worked around that cycle for packaging purposes
with a `PackEnigmaMsiWorkerPayload` target that built the worker through the `MSBuild` task to harvest a
`tools/worker/` payload; packaging is gone, and so is the workaround.

## Target frameworks & dependencies

- **`src/Enigma.Msi`** multi-targets **`netstandard2.0;net8.0;net10.0`**. `netstandard2.0` is not
  legacy caution — it is what lets the `net472` worker consume the *same* model assembly as modern
  consumers. `net8.0`/`net10.0` are the two currently-supported LTS releases.
- **`src/Enigma.Msi.Worker`** is **`net472`**, forced by WixSharp being .NET Framework-only; its test
  project mirrors that TFM. This is a documented deviation from the house `net10.0` app default.
- **`src/Enigma.Msi.Desktop`** is `net10.0` `WinExe` (Avalonia; plain TFM, no `-windows` suffix — the
  app's Windows-only nature comes from what it drives, not from its TFM). Its UI stack is a **coupled
  set** pinned together: Avalonia **12.1.1** (+ `.Desktop`, `.Themes.Fluent`, `.Fonts.Inter`, and the
  test-side `.Headless`/`.Headless.XUnit`), `Enigma.Avalonia.Desktop` **1.0.0** and
  `Enigma.Icons.Avalonia` **1.0.0** — the latter two are built against Avalonia 12.1.x, so bump the whole
  set or none of it. The two headless packages are pinned in the *same* `ItemGroup` as the rest for that
  reason, though only the desktop test suite consumes them. `AvaloniaUI.DiagnosticsSupport` is
  Debug-only via a conditional `IncludeAssets`/`PrivateAssets`. Beware two API details:
  `Enigma.Avalonia.Desktop`'s editors derive from `TextBox`, whose `Watermark` Avalonia 12 obsoletes —
  use `PlaceholderText`, or the XAML compiler's `AVLN5001` fails the zero-warnings build; and its picker
  services' path-returning overloads are C# 14 `extension` members, so they cannot be substituted in a
  test (which is why the app puts its own `IPathPickerService` in front of them).
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
```

There is **no pack step**: `src/Enigma.Msi` sets `IsPackable=false` and is consumed in-repo by
`ProjectReference`. The only artifact this repository produces is the desktop app's MSI — see
`docs/RELEASE.md`.

On the .NET 10 SDK in MTP mode, `dotnet test <solution>` is **rejected** — the solution must be
passed through the explicit `--solution` flag, as above.

Tests are **MTP-native**: `xunit.v3` + `coverlet.collector`, with **no** `Microsoft.NET.Test.Sdk` and
no `xunit.runner.visualstudio`.

`tests/Enigma.Msi.Desktop.UnitTests` is the one suite with a UI dependency: `Avalonia.Headless` +
`Avalonia.Headless.XUnit` give it a windowing platform with no screen behind it, so a test can load real
markup. `HeadlessTestApp` declares the assembly-wide session, and only `[AvaloniaFact]`/`[AvaloniaTheory]`
bodies run on it — the plain `[Fact]` ViewModel tests are unaffected. Keep the UI dependency in that
suite alone.

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
