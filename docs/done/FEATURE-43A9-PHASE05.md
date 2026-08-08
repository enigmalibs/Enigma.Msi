# FEATURE-43A9-PHASE05 — Avalonia desktop app (DONE)

## Summary

The UI over the model. `src/Enigma.Msi.Desktop` is a net10.0 Avalonia 12.1.1 `WinExe` that edits an
`MsiPackage` as a form, saves and loads it as `.msipkg.json`, reports every violation the validator
finds, and drives a real build through `IMsiBuildService` with the worker deployed beside it — Open /
Save / Save as / Validate / Build / Cancel, a live append-only log, and InfoBar feedback. Bootstrapping
follows the family precedent (`Enigma.LicenseManager.Desktop`): an `IHost` built and started before
`StartWithClassicDesktopLifetime`, C# 14 `extension(IServiceCollection)` registration blocks, NLog with a
file target, and the `SynchronizationContext` reset before `StopAsync` so shutdown cannot deadlock.

With this phase the whole of FEATURE-43A9 is complete: model, worker, build client, and app.

**Verified against the real toolchain, not only against tests.** The built app launched, its window came
up (`MainWindowTitle` = `Enigma.Msi — new package`, `Responding` = True, nothing logged above Info), and
the worker the build placed in `bin/Release/net10.0/worker/` — the exact folder `MsiBuildService`
discovers — built a 2 609 152-byte `SmokeWidget.msi` from a hand-written `.msipkg.json` against
`wix 7.0.0+b8977d6`. What is *not* automated, and stays a manual acceptance step for the maintainer, is
clicking through the form itself; this session could launch the GUI but not drive it.

Five decisions worth knowing about:

- **The form is the only representation.** `PackageEditorViewModel` holds the editable state and
  `LoadFrom`/`ToPackage` are the only two crossings, at the file and build boundaries. No DTO layer.
  The round-trip is pinned by comparing `MsiPackageJson.Serialize` of the original against the
  materialized package — what the form drops, a saved profile drops.
- **Text the model cannot hold is reported by the form, not by the validator.** A half-typed version or
  GUID reaches `MsiPackage` as `null`/`Guid.Empty`, which the validator would call "required" — the wrong
  complaint about a typo. `PackageEditorViewModel.GetInputErrors()` reports those three cases in the
  validator's own `{ Path, Message }` shape, so both sets display as one list, input errors first.
- **Build is gated on the in-memory rules only.** `IMsiPackageValidator`'s split is used as designed:
  `Validate` runs as the user types and drives `BuildCommand.CanExecute`, while `ValidateAll` (which
  touches the disk) runs when a build starts. Typing a path that does not exist yet therefore does not
  disable Build, and the missing folder is reported at build time. A `PackageEditorViewModel.Changed`
  event carries edits from child rows and the managed-UI block up to the gating, which `ObservableObject`
  alone does not do for collections or nested ViewModels.
- **The dialog sequences are reorderable lists, not check boxes.** The model's sequences are ordered and
  the order is not the enum's — the default modify sequence is Welcome → MaintenanceType → Progress →
  Exit while `Dialog` declares `Progress` before `MaintenanceType`. A checklist would silently reorder
  it, so `DialogSequenceViewModel` offers add / remove / move up / move down, used twice.
- **The log is append-only and marshalled through a seam.** Each streamed line costs one
  `ObservableCollection.Add`, never a re-concatenation (the predecessor UI's quadratic mistake), and the
  hand-off to the UI thread goes through `IUiDispatcher` rather than `Progress<T>` — see *Deviations*.

## Files/modules touched

### Created — `src/Enigma.Msi.Desktop/`
- `Enigma.Msi.Desktop.csproj` — net10.0 `WinExe`, compiled bindings by default, the Avalonia 12.1.1 set
  + `Enigma.Avalonia.Desktop` + `Enigma.Icons.Avalonia` + CommunityToolkit.Mvvm + Hosting + NLog,
  `AvaloniaUI.DiagnosticsSupport` Debug-only, the build-order `ProjectReference` to the worker with all
  three required attributes, and the `build/CopyWorkerOutput.targets` import
- `Program.cs` — host built and started before Avalonia; `SynchronizationContext` cleared before
  `StopAsync`
- `App.axaml` / `App.axaml.cs` — `FluentTheme` + the `Enigma.Avalonia.Desktop` theme dictionary as a
  `ResourceInclude`; resolves the window, registers the three overlay hosts and the storage provider,
  and falls back to a throwaway provider under the XAML designer
- `ServiceCollectionExtensions.cs` — `AddEnigmaAvaloniaServices()`, `AddMsiServices()`,
  `AddViewsAndViewModels()` as `extension(IServiceCollection)` blocks
- `NLog.config` — file target only (a `WinExe` has no console), Info and above
- `Services/IUiDispatcher.cs`, `Services/UiDispatcher.cs` — the UI-thread hand-off
- `Services/IPathPickerService.cs`, `Services/PathPickerService.cs` — the four path questions the app
  asks, over `IFileDialogService`/`IFolderDialogService`
- `ViewModels/MainWindowViewModel.cs` — commands, validation display, pre-flight + build + cancel, log
- `ViewModels/PackageEditorViewModel.cs` — the form, `LoadFrom`/`ToPackage`/`Reset`/`GetInputErrors`,
  GUID regeneration, shortcut add/remove, folder and icon browse
- `ViewModels/ShortcutViewModel.cs`, `ViewModels/UiSettingsViewModel.cs`,
  `ViewModels/DialogSequenceViewModel.cs`
- `Views/MainWindow.axaml` / `.axaml.cs` — toolbar, `SettingsCardExpander` form sections, problems pane,
  log pane, status bar, the three overlay hosts; code-behind keeps the log scrolled to its newest line

### Created — `tests/Enigma.Msi.Desktop.UnitTests/`
- `Enigma.Msi.Desktop.UnitTests.csproj` — net10.0, MTP-native xunit.v3 + coverlet, NSubstitute
- `TestPackages.cs`, `InlineUiDispatcher.cs`, `FakeBuildService.cs` — fixtures and doubles
- `ViewModels/MainWindowViewModelTests.cs` (23), `ViewModels/PackageEditorViewModelTests.cs` (17),
  `ViewModels/DialogSequenceViewModelTests.cs` (11), `ViewModels/UiSettingsViewModelTests.cs` (6)

### Modified
- `Directory.Packages.props` — a Desktop group (Avalonia 12.1.1 ×4, AvaloniaUI.DiagnosticsSupport 2.2.3,
  CommunityToolkit.Mvvm 8.4.2, Enigma.Avalonia.Desktop 1.0.0, Enigma.Icons.Avalonia 1.0.0,
  Microsoft.Extensions.Hosting 10.0.10, NLog 6.1.4 ×2) and NSubstitute 6.1.0 in the test group
- `Enigma.Msi.slnx` — the two new projects
- `docs/roadmap.md`, `docs/plan/FEATURE-43A9.md` — PHASE05 and the item itself to DONE

## Deviations & follow-ups

- **`IPathPickerService` is an addition the plan did not name.** The plan says the ViewModel tests cover
  "save/load through mocked dialog services", which the library's own services cannot support:
  `Enigma.Avalonia.Desktop`'s path-returning overloads are C# 14 `extension` members — static, so not
  substitutable — and the interface methods traffic in `IStorageFile`/`IStorageFolder`, which a ViewModel
  test has no business faking. The four-method app-level interface is what the ViewModels depend on;
  `PathPickerService` is the thin implementation over both library services. Consequence: that
  implementation itself is not unit-tested (it is pure delegation to a storage provider) — it is covered
  by the manual acceptance step.
- **`IUiDispatcher` likewise.** `Progress<T>` posts to the synchronization context captured at
  construction, which makes the hand-off asynchronous even on the UI thread and untestable without a
  dispatcher. The ViewModel hands lines to `IUiDispatcher`, which the suite replaces with an inline
  implementation; `MainWindowViewModel.LogSink` is the trivial `IProgress<string>` that feeds it.
- **`PackageEditorViewModel.GetInputErrors()` is an addition** for the same reason: unrepresentable text
  has to be reported somewhere, and the validator by definition cannot see it.
- **`INavigationService` is deliberately not registered.** The plan makes navigation conditional on pages
  being used ("plus navigation if pages are used"); this app is a single form, so registering it would be
  a dead singleton.
- **No app icon.** The plan's contingency applies: no `.ico` exists in the repository (the artwork is
  user-owned), so `<ApplicationIcon>` in the `.csproj` and `Icon="/Assets/appicon.ico"` on the window are
  present as commented-out lines with a TODO naming its owner. Dropping `src/Enigma.Msi.Desktop/Assets/appicon.ico`
  in and uncommenting both is the whole change; FEATURE-5F00-PHASE04 wants it.
- **`Watermark` → `PlaceholderText`.** Avalonia 12 obsoletes `TextBox.Watermark`; the editors derive from
  `TextBox`, so the form uses `PlaceholderText`. Worth knowing when reading the older
  `Enigma.LicenseManager.Desktop` views, which still say `PlaceholderText` against Carbon's own property.
- **`Enigma.Avalonia.Desktop` brings `Enigma.Core` and, through it, `BouncyCastle.Cryptography`
  (~4.7 MB)** — for two editor controls this app does not place. There is no opt-out. It is dead weight
  in the app's output and will be dead weight in the MSI that FEATURE-5F00-PHASE04 builds.
- **The end-to-end MSI was produced through the deployed worker's CLI mode, not by clicking Build.** The
  app's own `MsiBuildService` path is covered by the library suite against the stub worker and by
  PHASE04's real-toolchain run; what this phase adds evidence for is that the *app's deployment layout*
  (`worker/` beside the executable) carries a functional worker. Clicking through the form remains the
  maintainer's manual acceptance step.
- **Line endings:** no CRLF/LF churn observed in this phase's diff; `.gitattributes` (`* text=auto
  eol=lf`) is doing its job. No action taken — recommendation-only per the workflow.
- **Follow-up:** `README.md` is still the one-line placeholder PHASE01 left and `RELEASENOTES.md` is
  empty. Both are FEATURE-5F00's work (PHASE03), not a gap in this phase.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release` — **Build succeeded, 0 Warning(s), 0 Error(s)** (with
  `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild`; the Avalonia XAML compiler's `AVLN5001`
  obsolete-property warnings were fixed, not suppressed).
- `dotnet test --solution Enigma.Msi.slnx -c Release` — **Passed! total: 368, failed: 0, skipped: 0**
  across four suites: `Enigma.Msi.UnitTests` (net8.0 and net10.0), `Enigma.Msi.Worker.UnitTests` (net472),
  and the new `Enigma.Msi.Desktop.UnitTests` (net10.0, **57 tests**).
- Worker deployment: `src/Enigma.Msi.Desktop/bin/Release/net10.0/worker/` holds 23 files including
  `Enigma.Msi.Worker.exe` and its WixSharp closure — the layout `MsiBuildService` discovers by default.
- App launch: the process stayed up with `MainWindowTitle = "Enigma.Msi — new package"` and
  `Responding = True`; `Enigma.Msi.Desktop.log` recorded only the three host-startup Info lines.
- Real MSI: `worker/Enigma.Msi.Worker.exe build sample.msipkg.json` → exit 0,
  `SmokeWidget.msi` (2 609 152 bytes), `wix 7.0.0+b8977d6`, product/upgrade GUIDs echoed back as written.

## Acceptance criteria

| Criterion | Status |
|---|---|
| App starts, edits, validates, saves/loads `.msipkg.json`, drives a build through the client library | **Met** — starts and shows its window (verified); edit/validate/save/load/build covered by 57 ViewModel tests; the deployed worker built a real MSI. Clicking through the form is the maintainer's manual step. |
| Cancel terminates the worker process tree | **Met** — the ViewModel cancels the token and reports it (test); killing the tree is `MsiBuildService`'s behaviour, measured against a real `wix.exe` child in PHASE04. |
| Pre-flight failure (no wix tool) surfaces the actionable message in the UI | **Met** — the build stops before the worker, logs every problem verbatim and raises an error InfoBar (test). |
| Build + full suite green, zero warnings | **Met** — 0 warnings, 368/368 tests. |
