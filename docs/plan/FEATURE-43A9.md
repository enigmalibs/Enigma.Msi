# FEATURE-43A9 — MSI builder library, worker & Avalonia app

**Status:** DONE (multi-phase)
**Type:** FEATURE (multi-phase)
**Branch (per phase, at build time):** `feature/feature-43a9-phaseNN-<slug>` — one branch per phase, cut from current `HEAD`.

## Objective

Create **Enigma.Msi**, the successor of MsiBuilder (`C:\Dev\JCL\MsiBuilder`): a library that lets a user build an MSI installer via WixSharp (WiX v4), plus an Avalonia desktop app to do it through a UI. The repo joins the enigmalibs family (`https://github.com/enigmalibs/Enigma.Msi`) and follows Enigma.Core's structure and conventions.

Feature scope at v1 is **parity with MsiBuilder**: files from one release folder, shortcuts, install scope, version, product/upgrade GUIDs, control-panel info, compression, output path/name, managed-UI dialog selection — with a model designed for extension.

## Context & constraints

- **WixSharp_wix4 2.14.1** (latest stable, 2026-07; MsiBuilder used 2.12.0) is .NET Framework-only, so the process that runs WixSharp must be **net472**. Actual MSI generation additionally requires the WiX CLI (`dotnet tool install --global wix`) on Windows.
- Do **not** reference `WixSharp_wix4.bin` explicitly — it is a dependency of `WixSharp_wix4` (MsiBuilder's duplicate reference was redundant).
- The interop is inspired by MsiBuilder: a modern host spawns the net472 worker exe and exchanges JSON files. Improvements required over MsiBuilder (its known pain points):
  - **Single declarative model** instead of three hand-synced representations (fluent chain + DTOs + ViewModel).
  - **Aggregate validation** (all errors with member paths), not first-error-only bool flags.
  - **Cancellation** actually kills the worker process tree (MsiBuilder leaves orphaned wix builds).
  - **WiX tool preflight** with an actionable message instead of raw console noise.
  - **Headless CI path** (`build <file>` CLI mode on the worker).
  - Strict argument parsing in the worker (reject unknown args; MsiBuilder's parser silently ignored them).
  - Memory-safe build log (no O(n²) string concatenation like MsiBuilderUI's `BuildLog`).
  - Enum mirrors guarded by **drift tests** against WixSharp's real enum members (MsiBuilder pinned them to 2.12 by hand).
- **Profile format is a clean break** (user decision): new schema, **no import** of legacy `<App>.msiprofile.<version>.json` files. New files are `<name>.msipkg.json` with a `schemaVersion` field.
- **Windows-only** official support for the app (MSI building requires Windows in every case). Projects stay plain TFMs (no `-windows` suffix needed for Avalonia).
- Reference codebases: **Enigma.Core** (`C:\Dev\JCL\Enigma.Core`) for repo layout/conventions/release docs; **Enigma.LicenseManager** (`C:\Dev\JCL\Enigma.LicenseManager`) for the Avalonia app precedent (hosting, DI extension blocks, NLog); **MsiBuilder** for the interop mechanism and WixSharp usage (`WixMsiBuilder.cs` shows the exact `ManagedProject` calls).
- UI stack (user decision): **Enigma.Avalonia.Desktop 1.0.0** (`github.com/enigmalibs/Enigma.Avalonia`; targets Avalonia 12.1.1, net8/net10 — NavigationView, SettingsCard, ContentDialog/Overlay/InfoBar, editors, and DI services `IFileDialogService`, `IFolderDialogService`, `INavigationService`, `IContentDialogService`, `IInfoBarService`, `IOverlayService`, registered individually as singletons) and **Enigma.Icons.Avalonia 1.0.0** (`github.com/enigmalibs/Enigma.Icons`; Phosphor set — `<ei:Icon Kind="..."/>`, `{ei:IconGeometry}`/`{ei:IconImage}`). Pin **Avalonia 12.1.1** to match. Bump these as a coupled set only.
- House conventions apply throughout: `.slnx`, `src/tests/docs`, Directory.Build.props (LangVersion 14, Nullable enable, ImplicitUsings disable, TreatWarningsAsErrors, EnforceCodeStyleInBuild), Directory.Packages.props (CPM — never `Version=` on a `PackageReference`), global.json (SDK 10.0.100, `"test": { "runner": "Microsoft.Testing.Platform" }`), full C# `.editorconfig`, `.gitattributes` LF normalization, MIT `LICENSE.md`, xunit.v3 MTP-native (**no** Microsoft.NET.Test.Sdk, no xunit.runner.visualstudio), file-scoped namespaces, namespace == folder, one type per file, XML docs on all public APIs.
- NuGet packaging metadata (the 12 properties, packed README, guides, SECURITY.md) is **added at release time** by FEATURE-5F00 — not in this feature. This plan only prepares the structural packaging plumbing (worker bundling layout).

## Solution shape

| Project | TFM | Role |
|---|---|---|
| `src/Enigma.Msi` | `netstandard2.0;net8.0;net10.0` | The only public library (future NuGet package `Enigma.Msi`): declarative model, validation, JSON serialization, build client (spawns the worker). netstandard2.0 is required so the net472 worker can consume the same model. |
| `src/Enigma.Msi.Worker` | `net472` | Internal console exe: model → WixSharp `ManagedProject` translation and the actual MSI build. Referenced by nothing at runtime — discovered on disk. Ships inside the `Enigma.Msi` nupkg (release-time) and beside the Desktop app (build-time copy). *Documented deviation:* the house default for apps is `net10.0` (and app tests `net10.0` alone) — net472 is forced by WixSharp_wix4 being .NET Framework-only; the test project mirrors the TFM of the code under test. |
| `src/Enigma.Msi.Desktop` | `net10.0` | Avalonia 12.1.1 `WinExe` app (Enigma.Avalonia.Desktop + Enigma.Icons.Avalonia, CommunityToolkit.Mvvm, IHost, NLog). |
| `tests/Enigma.Msi.UnitTests` | `net8.0;net10.0` | Model, validation, serialization, build-client tests. |
| `tests/Enigma.Msi.Worker.UnitTests` | `net472` | Mapping/translation tests + WixSharp enum drift guards. |
| `tests/Enigma.Msi.Desktop.UnitTests` | `net10.0` | ViewModel tests (NSubstitute for service mocks). |

Project/test naming follows Enigma.Core (`<Name>.UnitTests` under `tests/`); root namespace == project name == folder name.

## Design

### The declarative model (`Enigma.Msi`)

One serializable document is *the* input everywhere — the worker consumes it, the saved `.msipkg.json` profile is its serialization, the UI binds to it. No mirror DTO layer.

Root type `MsiPackage` (names indicative; refine at build time without losing fields):

- `SchemaVersion` (int, = 1)
- `AppName` (string, required)
- `Version` (`System.Version`, required — JSON converter, serialized as string)
- `ProductId` (`Guid`, required) / `UpgradeCode` (`Guid`, required) — real GUID types, not strings (fixes MsiBuilder's stringly-typed contract)
- `Manufacturer` (string, required)
- `Scope` (`InstallScope` enum: `PerMachine`/`PerUser`; default `PerMachine`)
- `Install` (required): `InstallPath` (e.g. `%ProgramFiles%\MyApp`), `ReleasePath` (source directory whose contents are packaged — single `*.*` recursive glob at v1, same as MsiBuilder)
- `Output` (required): `OutputPath`, `MsiFilename` (no extension)
- `Compression` (`CompressionLevel` enum; **optional with default `High`** — MsiBuilder made it mandatory with default None; profiles in the family use High)
- `ControlPanel` (optional): `ProductIcon`, `Comments`, `Contact`, `HelpLink`, `UrlInfoAbout`
- `Shortcuts` (list, may be empty): `Shortcut` { `ShortcutPath` (`%Desktop%`/`%ProgramMenu%`/…), `ShortcutName`, `TargetPath` (`[INSTALLDIR]\App.exe`), `IconPath?`, `Arguments?` }
- `Ui` (optional): { `Wui` enum, `InstallDialogs` list, `ModifyDialogs` list } — when null the worker applies MsiBuilder's default (`WixUI_InstallDir`, Welcome/InstallDir/Progress/Exit; modify: Welcome/MaintenanceType/Progress/Exit)

Enums are WixSharp-free mirrors (`InstallScope`, `CompressionLevel`, `Wui`, `Dialog`) — mapped in the worker, guarded by drift tests (see PHASE03).

**Serialization:** System.Text.Json, camelCase, indented, enums as strings, nulls omitted; converters for `Guid`/`Version`. A single options/serializer helper type in the library (e.g. `MsiPackageJson`) used by client, worker and app. The netstandard2.0 target carries the `System.Text.Json` package reference (framework-provided on net8/net10 — condition it to avoid NU1510).

**Validation:** `IMsiPackageValidator`/`MsiPackageValidator` returns a result carrying **all** errors as `{ Path, Message }` (e.g. `install.releasePath`, "Directory does not exist"). Purely in-memory checks (required fields, GUID non-empty, version shape, dialog lists non-empty when `Ui` set) are separated from environment checks (paths exist) so the UI can validate as-you-type without touching the disk. Per Enigma.Core precedent the library ships **no** `AddEnigmaMsi()` DI extension — registration is the consumer's choice.

**Build client:** `IMsiBuildService` with
`Task<MsiBuildResult> BuildAsync(MsiPackage package, IProgress<string>? log = null, CancellationToken cancellationToken = default)`
plus a preflight API (e.g. `Task<MsiPrerequisites> CheckPrerequisitesAsync(...)`) that verifies the worker exe is found and the `wix` CLI is available (actionable message: "run: dotnet tool install --global wix"). `MsiBuildResult` = `{ bool Success, string? MsiPath, IReadOnlyList<string> Errors }` (structured, not a tuple). Worker discovery order: explicit option (e.g. `MsiBuildServiceOptions.WorkerPath`) → `AppContext.BaseDirectory/worker/Enigma.Msi.Worker.exe`. Cancellation kills the **whole worker process tree** (net472 child processes include wix invocations).

### Worker protocol (`Enigma.Msi.Worker`)

Two modes, strict argument parsing (unknown arguments → usage + exit 2):

1. **Internal mode** (used by the client): `Enigma.Msi.Worker.exe --request <request.json> --result <result.json>` — request is the serialized `MsiPackage`; result is the serialized `MsiBuildResult`. Worker re-validates the model (defense in depth), translates to a WixSharp `ManagedProject` (same calls as MsiBuilder's `WixMsiBuilder`: `AddDir`/`InstallDir`+`Files`, `ExeFileShortcut`, `Scope`, `Version`, `ProductId`/`UpgradeCode`, `ControlPanelInfo`, `Media` compression, `OutDir`/`OutFileName`, `ManagedUI` dialogs) and calls `BuildMsi()`. All WixSharp/console output flows to stdout/stderr for live streaming. Exit codes: 0 = success; 1 = the operation did not succeed on well-formed input (validation failure or MSI build failure — matches the MsiBuilder precedent); 2 = bad arguments / unparseable input file / unexpected error.
2. **CLI mode** (headless/CI): `Enigma.Msi.Worker.exe build <file.msipkg.json>` — loads the profile, validates (printing all errors), builds, prints the log and the resulting MSI path; same exit codes.

The translation layer lives in the worker project as testable public-in-assembly classes (à la MsiBuilder's `BuilderConfigurator` with pure mapping helpers), so the net472 test project can cover every mapping without running a real build.

### Packaging plumbing

- **Desktop app:** build-order-only `ProjectReference` to the worker (`ReferenceOutputAssembly=false`, `SkipGetTargetFrameworkProperties=true`) + a copy target placing the worker output in `$(OutDir)worker/`. Known caveat (observed in MsiBuilder's `CopyWorkerOutput` mechanism): the `AfterTargets="Build"` copy lands in `$(OutDir)`, which `dotnet publish` does not include when assembling `$(PublishDir)` — also hook the publish pipeline (e.g. `AfterTargets="Publish"` copying to `$(PublishDir)worker/`, or add the files to the publish item groups), otherwise document the limitation in the repo `CLAUDE.md`.
- **NuGet bundling (prepared here, finalized at release):** the `Enigma.Msi` nupkg will carry the worker + its dependency closure under `tools/worker/` and a `build/Enigma.Msi.targets` copying it to the consumer's `$(OutDir)worker/`. PHASE04 delivers the layout and the `.targets` file; the pack metadata and pack-verify belong to the future release item.

## Out of scope (this feature)

- The first NuGet release and the Desktop app's MSI release — planned as **FEATURE-5F00** (docs/plan/FEATURE-5F00.md): metadata, packed README, guides, SECURITY.md, app MSI profile, runbook, license audit.
- Importing legacy `.msiprofile.json` files (explicit user decision: clean break).
- Leveled/parsed build log (deliberately not selected; log stays plain streamed lines).
- MSI features beyond MsiBuilder parity (multiple source dirs, registry keys, custom actions, …) — later work items.
- Localization/accessibility work beyond Avalonia defaults (family precedent: English-only).
- CI pipelines (none in the family repos).

**Follow-up (outside this repo):** the house release skill's MSI-profile template still emits the legacy `.msiprofile` format; update it to `.msipkg.json`/`MsiPackage` once Enigma.Msi replaces MsiBuilder.

## Risks / notes

- **xunit.v3 + MTP on net472:** xunit.v3 supports net472 and is MTP-native; if `dotnet test` (MTP runner) proves unable to drive the net472 test exe, run it directly (v3 test projects are executables) and record the deviation in the phase's completion doc. Do not fall back to Microsoft.NET.Test.Sdk without flagging it.
- **End-to-end MSI build** requires Windows + the `wix` global tool; it is a **manual acceptance step** (build a sample MSI via CLI mode and via the app), not an automated test.
- **App icon:** the Desktop app needs `Assets/*.ico` (user-owned art). PHASE05 wires `ApplicationIcon` + window `Icon`; if no `.ico` is provided by then, leave the wiring commented with a TODO note in the completion doc.
- WixSharp pulls large binaries; keep them out of git (standard `.gitignore` covers `bin/`/`obj/`).

---

## PHASE01 — Repository & solution bootstrap

**Status:** DONE
**Branch:** `feature/feature-43a9-phase01-bootstrap`

Follow the house bootstrap checklist in order (git init already done during planning):

1. `.gitignore`, `.gitattributes` (git-repo-hygiene; LF normalization, binary rules incl. `*.ico`). Add a `*.msi` ignore rule alongside the template's `artifacts/`/`*.nupkg` entries — dogfood-built installers (FEATURE-5F00) must never sit in the tree as committable binaries.
2. Full C# `.editorconfig` (dotnet-solution-config).
3. `Directory.Build.props` (Authors "Josué Clément", Copyright © 2026, LangVersion 14, Nullable, ImplicitUsings disable, TreatWarningsAsErrors, EnforceCodeStyleInBuild) and `Directory.Packages.props` (CPM on; initial pins: xunit.v3 3.2.2, coverlet.collector 10.0.1, System.Text.Json latest stable, PolySharp, System.Buffers — every package the library template's conditional groups reference must be pinned (CPM fails on unpinned references); versions re-checked at build time).
4. `global.json` — SDK 10.0.100, rollForward latestFeature, MTP test runner.
5. `LICENSE.md` (MIT, house template), empty `README.md` + `RELEASENOTES.md` placeholders.
6. `Enigma.Msi.slnx` with `/src/` + `/tests/` solution folders.
7. `src/Enigma.Msi` (library template: multi-target `netstandard2.0;net8.0;net10.0`, conditional System.Buffers/PolySharp groups) + `tests/Enigma.Msi.UnitTests` (MTP-native xunit.v3 template) with one smoke test. Include a `.csproj` comment stating why the library multi-targets (netstandard2.0 so the net472 worker can consume the model; net8.0/net10.0 = the current LTS pair), per the house TFM-deviation rule.
8. Repo `CLAUDE.md` (mirroring Enigma.Core's shape: what this is, architecture, layout, build/test commands, conventions, dev workflow).
9. `docs/` structure already exists from planning (roadmap, plan, done).

**Acceptance criteria**
- `dotnet build Enigma.Msi.slnx -c Release` — zero warnings.
- `dotnet test --solution Enigma.Msi.slnx -c Release` — smoke test passes on net8.0 and net10.0. (MTP mode on the .NET 10 SDK rejects `dotnet test <sln>` without the `--solution` flag.)
- All root config files present and consistent with Enigma.Core's conventions; no `Version=` on any `PackageReference`.

## PHASE02 — MsiPackage model, validation & serialization

**Status:** DONE
**Branch:** `feature/feature-43a9-phase02-model`

1. Model types in `src/Enigma.Msi` (folder `Model/` or similar; one type per file): `MsiPackage`, `InstallSettings`, `OutputSettings`, `ControlPanelInfo`, `Shortcut`, `UiSettings`, enums `InstallScope`, `CompressionLevel`, `Wui`, `Dialog` — plus **`MsiBuildResult`** (`{ bool Success, string? MsiPath, IReadOnlyList<string> Errors }`), which PHASE03's worker already needs to serialize to the result file. XML docs on every public member.
2. JSON serialization helper (`MsiPackageJson`): camelCase, indented, enums as strings, nulls omitted, `Guid`/`Version` converters; `SchemaVersion` always written. Covers `MsiBuildResult` as well as `MsiPackage`.
3. `IMsiPackageValidator`/`MsiPackageValidator`: aggregate `{ Path, Message }` errors; in-memory rules separated from environment (disk) rules.
4. Tests (`Enigma.Msi.UnitTests`): serialization round-trips (full model, minimal model, null-optional omission, enum-as-string, `MsiBuildResult`), converter edge cases (bad GUID/version input → clear failure), validator theories (each rule, aggregation of multiple errors, member paths).

**Acceptance criteria**
- Round-trip serialize→deserialize preserves every field; unknown JSON fields are ignored (forward compatibility).
- Validator returns **all** violations for a deliberately multi-invalid package, each with a member path.
- Build + full test suite green, zero warnings.

## PHASE03 — net472 worker: WixSharp translation & CLI

**Status:** DONE
**Branch:** `feature/feature-43a9-phase03-worker`

1. `src/Enigma.Msi.Worker` (net472, `OutputType=Exe`, references `Enigma.Msi` + `WixSharp_wix4` 2.14.1 only — no `.bin`).
2. Translation layer (testable, MsiBuilder's `BuilderConfigurator` as reference): pure mapping helpers model-enum → WixSharp (`MapScope`, `MapCompression`, `MapWui`, `MapDialog`) + a configurator that builds the `ManagedProject` from an `MsiPackage`, applying the default managed-UI dialog set when `Ui` is null.
3. Strict argument parsing; internal mode (`--request`/`--result`) and CLI mode (`build <file.msipkg.json>`); exit codes 0/1/2; top-level exception handler writes a failed `MsiBuildResult` where possible.
4. Tests (`Enigma.Msi.Worker.UnitTests`, net472): mapping theories covering **every** model enum member; **drift guards** — reflection tests asserting WixSharp's `InstallScope`/`CompressionLevel`/`WUI`/`Dialogs` member sets against the pinned expectations (a WixSharp upgrade that adds/renames members fails loudly, incl. the British `Licence` spelling); argument-parser tests (unknown arg rejected, missing value rejected); validation-failure path returns aggregated errors.

**Acceptance criteria**
- Every model enum member maps; drift guards pass against WixSharp 2.14.1.
- `Enigma.Msi.Worker.exe build <validation-failing.msipkg.json>` exits 1 with **all** validation errors printed; unparseable JSON and unknown arguments exit 2 (with usage for the latter) — per the exit-code contract in *Worker protocol*.
- Build + full suite green, zero warnings. (Manual, recorded in the completion doc when possible: a real `build sample.msipkg.json` produces an MSI on Windows with the wix tool.)

## PHASE04 — Build client, interop & packaging plumbing

**Status:** DONE
**Branch:** `feature/feature-43a9-phase04-client`

1. In `src/Enigma.Msi`: `IMsiBuildService`/`MsiBuildService` (+ options type) — write request JSON to temp, spawn worker, stream stdout/stderr via `IProgress<string>`, read result file, clean up temp files; cancellation kills the process tree; distinct failure messages for worker-not-found / no-result-file / result-parse-failure.
2. Preflight API: worker discovery check + `wix` CLI availability check with the actionable install hint.
3. Worker discovery: options override → `AppContext.BaseDirectory/worker/Enigma.Msi.Worker.exe`.
4. Packaging plumbing: `build/Enigma.Msi.targets` (consumer copy of `tools/worker/` → `$(OutDir)worker/`) committed and documented; worker-output copy target design shared with the Desktop app (PHASE05 consumes it).
5. Tests (`Enigma.Msi.UnitTests`): process-free coverage via a fake worker executable or an abstraction over process start — request-file content, argument construction, result parsing, missing-worker/missing-result paths, cancellation behavior (where testable without flakiness).

**Acceptance criteria**
- `BuildAsync` round-trip works against a stub worker (test double) including failure and cancellation paths.
- Preflight reports missing worker and missing wix tool with actionable messages.
- Build + full suite green, zero warnings.

## PHASE05 — Avalonia desktop app

**Status:** DONE
**Branch:** `feature/feature-43a9-phase05-desktop`

1. `src/Enigma.Msi.Desktop` (net10.0, `WinExe`, compiled bindings default): Avalonia 12.1.1 set (Avalonia, Desktop, Themes.Fluent, Fonts.Inter, AvaloniaUI.DiagnosticsSupport Debug-only), Enigma.Avalonia.Desktop 1.0.0, Enigma.Icons.Avalonia 1.0.0, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, NLog + NLog.Extensions.Logging.
2. Bootstrapping per the family precedent (Enigma.LicenseManager.Desktop): host built and started before `StartWithClassicDesktopLifetime`; `ServiceCollectionExtensions` with C# 14 `extension(IServiceCollection)` blocks; Enigma.Avalonia services registered as singletons (`IFileDialogService`, `IContentDialogService`, `IInfoBarService`, `IOverlayService`, plus navigation if pages are used); NLog wired (`ClearProviders` + `AddNLog`, `NLog.config` copied) **with a file target** (per the LicenseManager precedent) so app-level errors are diagnosable after the fact; clean shutdown (`StopAsync` on exit, `SynchronizationContext` reset like the precedent).
3. UI: form sections mirroring the `MsiPackage` model (SettingsCard-based), shortcuts collection editor, optional managed-UI dialog selection, GUID generate buttons, live build log pane, InfoBar/status feedback. The log pane must accumulate lines memory-safely (append-only collection or document buffer — **no** per-line string concatenation of the whole log, MsiBuilderUI's O(n²) mistake).
4. Commands: New / Open / Save / Save As (`.msipkg.json` via `IFileDialogService`), Validate (aggregate error display), Build (gated on validity; runs the PHASE04 preflight first and surfaces its actionable failure via `IInfoBarService`; streams log; disabled while running), **Cancel** (kills the running build).
5. ViewModel binds to/edits the model (single representation: VM state materializes an `MsiPackage`, `LoadFrom`/`ToPackage` only at the file/build boundary — no parallel DTO layer).
6. Worker copy target from PHASE04 applied; `ApplicationIcon` + window `Icon` wired (TODO note if no `.ico` asset provided).
7. Tests (`Enigma.Msi.Desktop.UnitTests`, NSubstitute): VM round-trip (ToPackage/LoadFrom), command gating (build disabled when invalid/running), build delegation to a mocked `IMsiBuildService` incl. cancellation and the mocked preflight-failure path, log accumulation behavior, save/load through mocked dialog services.

**Acceptance criteria**
- App starts, edits, validates, saves/loads `.msipkg.json`, and drives a build through the client library (manual end-to-end on Windows recorded in the completion doc).
- Cancel terminates the worker process tree; preflight failure (no wix tool) surfaces the actionable message in the UI.
- Build + full suite green, zero warnings.
