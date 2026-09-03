# Enigma.Msi

[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

Enigma.Msi builds Windows MSI installers from a single declarative model. This repository ships that as an
application — **Enigma.Msi.Desktop**, a desktop app where you fill in the installer, see everything that is
wrong with it, and build it — on top of a library that does the actual work. The app saves what you typed
as a plain `.msipkg.json` profile, and a headless worker CLI rebuilds the same profile in CI without the
app or the library.

The MSI authoring itself is done by [WixSharp](https://github.com/oleg-shilo/wixsharp) (WiX v4), which is
.NET Framework-only — so it runs inside a `net472` worker process, discovered on disk at run time, and
never appears on the library's public API.

> **What's new in 1.3.0** — the quick start's **output folder is now a question**, the seventh, instead of
> being derived from the release folder's parent; the **splash screen** is down to the logo, the name and
> the version; and **the library is no longer a NuGet package** — `Enigma.Msi` is not published to any
> feed, and the packaging that used to redistribute the worker is gone. See
> [RELEASENOTES.md](RELEASENOTES.md).

## The desktop app

One form, four buttons that matter. You describe the package, press **Validate** to get every problem at
once, press **Build**, and watch the WiX toolchain's output stream into a log pane while it runs.

- **Quick start** — seven answers (application name, version, manufacturer, release folder, output
  folder, an `.ico`, the main executable) and the whole form comes back filled in: install path, MSI file
  name, product icon with the Control Panel section on, and two shortcuts (`%ProgramMenu%` and
  `%Desktop%`). It produces a package that passes Validate with nothing outstanding, and everything it
  writes stays editable. It opens from the command bar, and once by itself on an empty form.
- **The form** — five collapsible sections: *Product* (identity, install scope, compression, the two
  GUIDs with their regenerate buttons), *Install and output*, *Control Panel information*, *Shortcuts*
  (one self-removing card per shortcut) and *Managed UI* (the WixUI dialog set and both dialog
  sequences). The three fields that take variables carry permanent hints listing what may be typed.
- **Problems** — the validator's output, every violation at once, each with the profile member path that
  produced it. Build stays disabled while the package is incomplete, and says so.
- **The build** — a modal card covers the window from the moment Build is pressed, showing an
  indeterminate bar, the latest log line and **Cancel**; cancelling terminates the worker *and its
  children*, so no orphaned `wix` processes are left behind.
- **Profiles** — Open, Save and Save as… over `.msipkg.json`, written with absolute paths so the worker
  CLI can build them from any working directory.
- **Splash and About** — an undecorated splash (logo, `Enigma.Msi`, the running version) dismissed by a
  click, a key or two seconds; and an About dialog behind the icon-only button at the right of the command
  bar, with the tagline, the copyright, the author and a **View on GitHub** button.

### Requirements

| Requirement | Notes |
|---|---|
| Windows | Building an MSI is Windows-only. |
| The .NET 10 SDK | To build the app from source (`global.json` pins 10.0.100). |
| The .NET 10 runtime | To run it — the app is published framework-dependent. |
| The WiX CLI | `dotnet tool install --global wix`. The app reports its absence *before* a build rather than failing during one. |

### Building it from source

There is no download: clone the repository and build it. Both commands run **from the repository root**.

```bash
git clone https://github.com/enigmalibs/Enigma.Msi.git
cd Enigma.Msi
dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
```

That produces `src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish\Enigma.Msi.Desktop.exe` with its
`worker\` subfolder beside it — the app finds the worker there by itself, with no configuration. For a
development run, `dotnet run --project src/Enigma.Msi.Desktop` is enough.

The app can also build **its own installer**, which is how this repository releases it:

```bash
.\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.1.3.0.msipkg.json
```

The profiles under `msiProfiles/` are ordinary `.msipkg.json` files, so they open in the app itself. See
[docs/RELEASE.md](docs/RELEASE.md) for the full release runbook.

## The library underneath

`src/Enigma.Msi/` is the engine. You describe the package once as an `MsiPackage` — app identity, install
scope, the folder to package, shortcuts, Control Panel information, compression, managed-UI dialogs — and
hand that same object to whichever part of the library you need: `IMsiPackageValidator` to find out what
is wrong with it, `IMsiBuildService` to build the `.msi`, `MsiPackageJson` to save it as a `.msipkg.json`
profile. There is deliberately no mirrored DTO layer and no fluent builder chain — the app, the profile
format and the worker all consume the very same type.

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
  children*.
- **Profiles** — `MsiPackageJson` reads and writes `<name>.msipkg.json`, which is exactly the model's
  serialization (no separate on-disk schema), versioned by a `schemaVersion` field.
- **A headless worker CLI** — the worker also runs on its own:
  `Enigma.Msi.Worker.exe build <file.msipkg.json>` builds an MSI from a committed profile with no library
  reference at all. Its exit codes are a contract: `0` success, `1` the operation failed on well-formed
  input, `2` bad arguments or an unexpected error.

### WixSharp never leaks

The library's `InstallScope`, `CompressionLevel`, `Wui` and `Dialog` enums are WixSharp-free mirrors,
mapped inside the worker. Reflection-based drift tests assert them against WixSharp's real member sets,
so a WixSharp upgrade that renames or adds a member fails the build loudly instead of silently
mis-mapping. The worker itself is referenced by nothing at compile time and discovered on disk at run
time; `build/CopyWorkerOutput.targets` puts it in `worker/` beside each host that needs it.

### Using it

**Enigma.Msi is not published to NuGet, or to any other feed.** It is consumed the way the app and the
worker consume it: clone this repository, add the project to your solution, and reference it directly.

```xml
<ProjectReference Include="..\Enigma.Msi\src\Enigma.Msi\Enigma.Msi.csproj" />
```

A project that then wants to *build* an MSI also needs the worker in its output — import
`build/CopyWorkerOutput.targets` and declare the build-order reference it documents. The library
multi-targets `netstandard2.0`, `net8.0` and `net10.0` (`netstandard2.0` is what lets the `net472` worker
consume the same model assembly) and builds on **WixSharp_wix4 2.14.1** (WiX v4).

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
headless worker CLI, and the desktop app in full.

## License

Enigma.Msi is released under the [MIT License](LICENSE.md).
