# Enigma.Msi v1.0.0 Release Notes

The first public release of **Enigma.Msi** — a library that builds Windows MSI installers from a single
declarative model. One `MsiPackage` is the input everywhere: the validator reports on it, the build
client builds it, a saved `.msipkg.json` profile is its serialization, and the companion desktop app
binds to it. The MSI authoring itself is done by WixSharp (WiX v4), which is .NET Framework-only; it is
confined to a `net472` worker process shipped inside the package, so it never reaches the public API and
modern .NET consumers take no .NET Framework dependency.

This document covers both artifacts of the release: the **Enigma.Msi** library (NuGet) and the
**Enigma.Msi.Desktop** application (MSI installer), both at **1.0.0**.

## Feature overview

- **The package model** — `MsiPackage` describes product identity (name, version, `ProductId` /
  `UpgradeCode`, manufacturer), install `Scope` (per-machine or per-user), `InstallSettings` (the source
  folder packaged and the install target), `OutputSettings` (where the `.msi` is written and under which
  name), cabinet `Compression`, optional `ControlPanelInfo` (`ProductIcon`, `Comments`, `Contact`,
  `HelpLink`, `UrlInfoAbout`), a list of `Shortcut` entries, and optional `UiSettings` carrying the
  managed-UI flavour (`Wui`) and the install/modify dialog sequences.
  Members are permissive data with defaults rather than `required`, so a half-filled package can be
  deserialized, edited and reported on; required-ness is a validation concern.
- **Validation** — `IMsiPackageValidator` returns every violation at once as `MsiValidationError`
  entries (`{ Path, Message }`), never a first-error bool. In-memory rules (shape, identity, GUIDs,
  version, tokens, dialog sequences) are separated from environment rules that touch the disk, so an
  editor can validate as-you-type and hit the filesystem only when asked.
- **Profiles** — `MsiPackageJson` serializes and deserializes `<name>.msipkg.json`. The file *is* the
  model, carrying a `schemaVersion`; a version above the one the library understands is reported as a
  validation error rather than silently misread. The legacy `<App>.msiprofile.<version>.json` format of
  the predecessor project is a deliberate clean break — there is no importer.
- **Building** — `IMsiBuildService.BuildAsync` validates the package, then drives the WiX toolchain out
  of process and returns an `MsiBuildResult` (`Success`, `MsiPath`, `Errors`). Failures arrive as data,
  not exceptions. `CheckPrerequisitesAsync` reports a missing worker or a missing WiX CLI ahead of time
  as `MsiPrerequisites`. The build log streams line by line through `IProgress<string>`, and
  cancellation terminates the worker together with its child processes, so a cancelled build leaves no
  orphaned `wix` processes.
- **The worker CLI** — the bundled `net472` worker also runs headless:
  `Enigma.Msi.Worker.exe build <file.msipkg.json>`. It needs no library reference, which makes a
  committed profile buildable straight from a CI job. Exit codes are a contract: `0` success, `1` the
  operation failed on well-formed input (validation or MSI build), `2` bad arguments, unparseable input,
  or an unexpected error.
- **Packaging plumbing** — the package ships the worker and its whole dependency closure under
  `tools/worker/`, plus `build/Enigma.Msi.targets`, which NuGet imports into every consuming project and
  which copies the payload to `$(OutDir)worker/` where the build client discovers it. Two MSBuild
  properties adjust it: `IncludeEnigmaMsiWorker` (set `false` for a project that reads profiles but
  never builds an MSI) and `EnigmaMsiWorkerFolderName`.
- **The desktop app** — **Enigma.Msi.Desktop 1.0.0**, an Avalonia application over the same
  `MsiPackage`: fill in the form, validate, build, watch the live log, and save or load a
  `.msipkg.json` profile. It hosts the same worker the library ships.
- **WixSharp is isolated, and that isolation is tested** — the public `InstallScope`,
  `CompressionLevel`, `Wui` and `Dialog` enums are WixSharp-free mirrors mapped inside the worker, and
  reflection-based drift tests assert them against WixSharp's real member sets so an upstream rename or
  addition fails the build instead of silently mis-mapping.

## Dependencies

- **Library** — `System.Text.Json` **10.0.10**, `System.Buffers` **4.6.1** and `PolySharp` **1.16.0**
  (compile-only) on `netstandard2.0` only; on `net8.0`/`net10.0` those are framework-provided, so the
  library has **no** runtime package dependencies there.
- **Worker (bundled, `net472`)** — `WixSharp_wix4` **2.14.1**, which brings `WixSharp_wix4.bin` and the
  WixToolset assemblies it needs.
- **Desktop app** — Avalonia **12.1.1** (`Avalonia`, `.Desktop`, `.Themes.Fluent`, `.Fonts.Inter`),
  `Enigma.Avalonia.Desktop` **1.0.0**, `Enigma.Icons.Avalonia` **1.0.0**, `CommunityToolkit.Mvvm`
  **8.4.2**, `Microsoft.Extensions.Hosting` **10.0.10**, NLog **6.1.4**
  (+ `NLog.Extensions.Logging`), and `AvaloniaUI.DiagnosticsSupport` **2.2.3** in Debug builds only. The
  four Avalonia-coupled packages move as a set.

## Compatibility

- The library targets **.NET Standard 2.0**, **.NET 8.0**, and **.NET 10.0**. `netstandard2.0` is what
  lets the `net472` worker consume the very same model assembly as modern consumers.
- The desktop app targets **.NET 10.0** (`WinExe`).
- **Building an MSI requires Windows and the WiX CLI** (`dotnet tool install --global wix`). The library
  itself loads on any supported runtime; the build does not.
- The `.msipkg.json` profile format is at **`schemaVersion` 1**.

## Version

- Initial release: **1.0.0** (Enigma.Msi library and Enigma.Msi.Desktop application).
