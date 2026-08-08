# Enigma.Msi

[![NuGet](https://img.shields.io/nuget/v/Enigma.Msi.svg)](https://www.nuget.org/packages/Enigma.Msi)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

Enigma.Msi builds Windows MSI installers from a single declarative model. You describe the package once
as an `MsiPackage` — app identity, install scope, the folder to package, shortcuts, Control Panel
information, compression, managed-UI dialogs — and then hand that same object to whichever part of the
library you need: `IMsiPackageValidator` to find out what is wrong with it, `IMsiBuildService` to build
the `.msi`, `MsiPackageJson` to save it as a `.msipkg.json` profile. There is deliberately no mirrored
DTO layer and no fluent builder chain. The actual MSI authoring is done by
[WixSharp](https://github.com/oleg-shilo/wixsharp) (WiX v4), which is .NET Framework-only — so it runs
inside a `net472` worker process bundled in this package, and never appears on the public API. Modern
.NET consumers take no .NET Framework dependency.

> **What's new in 1.0** — the first public release: the declarative `MsiPackage` model with validation
> and `.msipkg.json` profiles, the out-of-process build client, and a headless worker CLI for CI. See
> [RELEASENOTES.md](RELEASENOTES.md).

## Features

- **The package model** — `MsiPackage` carries product identity (name, version, product and upgrade
  GUIDs, manufacturer), per-machine or per-user `InstallScope`, the source folder to package and the
  install target, cabinet `CompressionLevel`, optional `ControlPanelInfo`, any number of `Shortcut`
  entries, and the managed-UI dialog sequence. Permissive defaults throughout: a half-filled package can
  still be deserialized, edited and reported on.
- **Validation that aggregates** — `IMsiPackageValidator` returns *every* violation as a
  `{ Path, Message }` pair rather than a first-error bool, and keeps the in-memory rules separate from
  the ones that touch the disk, so an editor can validate as you type and check the filesystem only on
  demand.
- **Building** — `IMsiBuildService.BuildAsync` runs validation, then the WiX toolchain, and returns an
  `MsiBuildResult`. A pre-flight check reports a missing worker or a missing WiX CLI as data; the build
  log streams line by line through `IProgress<string>`; cancellation terminates the worker *and its
  children*, so no orphaned `wix` processes are left behind.
- **Profiles** — `MsiPackageJson` reads and writes `<name>.msipkg.json`, which is exactly the model's
  serialization (no separate on-disk schema), versioned by a `schemaVersion` field.
- **A headless worker CLI** — the bundled worker also runs on its own:
  `Enigma.Msi.Worker.exe build <file.msipkg.json>` builds an MSI from a committed profile with no
  library reference at all. Its exit codes are a contract: `0` success, `1` the operation failed on
  well-formed input, `2` bad arguments or an unexpected error.

### WixSharp never leaks

The library's `InstallScope`, `CompressionLevel`, `Wui` and `Dialog` enums are WixSharp-free mirrors,
mapped inside the worker. Reflection-based drift tests assert them against WixSharp's real member sets,
so a WixSharp upgrade that renames or adds a member fails the build loudly instead of silently
mis-mapping. The worker itself is discovered on disk at run time and referenced by nothing at compile
time — the package ships it under `tools/worker/` and copies it into your output automatically.

## Installation

```bash
dotnet add package Enigma.Msi
```

Targets **.NET Standard 2.0**, **.NET 8.0**, and **.NET 10.0**; built on **WixSharp_wix4 2.14.1**
(WiX v4).

### Requirements for building an MSI

The library loads anywhere. **Generating an `.msi` requires Windows and the WiX CLI**, installed as a
global tool:

```bash
dotnet tool install --global wix
```

`IMsiBuildService.CheckPrerequisitesAsync` reports the absence of either the worker or the WiX CLI with
that exact hint, so a missing toolchain is a message rather than a failed first build.

## Quick start

Describe the package, then build it — the pre-flight check and the streamed log are optional:

```csharp
using System;
using System.Threading.Tasks;
using Enigma.Msi.Build;
using Enigma.Msi.Model;

var package = new MsiPackage
{
    AppName = "Contoso Widget",
    Version = new Version(1, 0, 0),
    ProductId = Guid.NewGuid(),
    UpgradeCode = Guid.Parse("2f1e0d9c-8b7a-6f5e-4b3c-2d1e0f9a8b7c"),
    Manufacturer = "Contoso AG",
    Install = new InstallSettings
    {
        InstallPath = @"%ProgramFiles%\Contoso\Widget",
        ReleasePath = @"C:\src\Widget\bin\Release\net10.0"
    },
    Output = new OutputSettings
    {
        OutputPath = @"C:\src\Widget\artifacts",
        MsiFilename = "ContosoWidget-1.0.0"
    }
};

IMsiBuildService builder = new MsiBuildService();

MsiBuildResult result = await builder.BuildAsync(package, new Progress<string>(Console.WriteLine));

Console.WriteLine(result.Success
    ? $"Built {result.MsiPath}"
    : string.Join(Environment.NewLine, result.Errors));
```

`UpgradeCode` is parsed from a literal on purpose: it is the product line's permanent identity, so it
belongs in source (or in a committed profile) rather than being generated per build. `ProductId`, which
identifies this one version, can be generated freely.

## Documentation

Per-category guides — each with the types and their members in tables, plus copy-pasteable C# samples
verified against the public API — live under `docs/guides/` in the repository, indexed by
`docs/guides/README.md`. They cover the package model and the `.msipkg.json` format, validation and its
complete rule set, building an MSI (pre-flight, log streaming, cancellation, worker discovery), the
headless worker CLI, and the companion Avalonia desktop app that drives the very same model.

## License

Enigma.Msi is released under the [MIT License](LICENSE.md).
