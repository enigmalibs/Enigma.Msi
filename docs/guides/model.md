# The package model

`MsiPackage` is the single input the whole library is built around. You fill one in, and every consumer
of the library takes it from there: `IMsiPackageValidator` reports what is wrong with it,
`IMsiBuildService` sends it to the worker to be built, the desktop app binds a form to it, and a saved
`.msipkg.json` profile is *exactly* its serialization. There is deliberately no mirrored DTO layer and no
fluent builder — one representation, everywhere.

The type is a plain mutable data carrier with permissive defaults rather than a set of `required`
members. That is a deliberate choice in favour of half-filled packages: a profile can be deserialized,
edited field by field and reported on, without the model refusing to exist. Required-ness is therefore a
*validation* concern, not a construction-time one — see [Validating a package](validation.md).

All paths in the model are Windows paths, and the values that Windows Installer interprets — install
directories, shortcut locations, shortcut targets — carry MSI-style tokens (`%ProgramFiles%`,
`%Desktop%`, `[INSTALLDIR]`) rather than resolved paths, so a profile stays portable between machines.

## Package fields

| Member | Type | Default | Required | Notes |
|---|---|---|---|---|
| `SchemaVersion` | `int` | `1` (`MsiPackage.CurrentSchemaVersion`) | yes | Always serialized. A value above the current one is a validation error, never a silent misread. |
| `AppName` | `string` | `""` | yes | Product name, as shown by the installer and in Control Panel. |
| `Version` | `System.Version?` | `null` | yes | Only the first three components are significant to Windows Installer. |
| `ProductId` | `Guid` | `Guid.Empty` | yes | Identifies this exact product version. Regenerate it for every release. |
| `UpgradeCode` | `Guid` | `Guid.Empty` | yes | Identifies the product *line*. Must stay constant across versions, or upgrades stop being detected. |
| `Manufacturer` | `string` | `""` | yes | Publisher shown in Control Panel. |
| `Scope` | `InstallScope` | `PerMachine` | yes | Per-machine (elevated) or per-user. |
| `Install` | `InstallSettings` | `new()` | yes | Where the package installs to, and which folder it packages. |
| `Output` | `OutputSettings` | `new()` | yes | Where the `.msi` is written, and under which name. |
| `Compression` | `CompressionLevel` | `High` | yes | Cabinet compression level. |
| `ControlPanel` | `ControlPanelInfo?` | `null` | no | When `null`, MSI defaults apply. |
| `Shortcuts` | `List<Shortcut>` | empty list | yes (may be empty) | Must not be `null`; an empty list means "no shortcuts". |
| `Ui` | `UiSettings?` | `null` | no | When `null`, the build applies `UiSettings.CreateDefault()` — *not* "no UI". |

### `InstallSettings`

| Member | Type | Notes |
|---|---|---|
| `InstallPath` | `string` | Target install directory. May contain Windows environment variables, e.g. `%ProgramFiles%\Contoso\Widget`. |
| `ReleasePath` | `string` | Local directory whose contents are packaged **recursively** — typically an app's release output folder. Must exist and be non-empty at build time. |

`ReleasePath` is a folder, not a file list: everything under it goes into the MSI, and nothing else does.
Point it at a clean publish/release output rather than at a build tree with intermediates in it.

### `OutputSettings`

| Member | Type | Notes |
|---|---|---|
| `OutputPath` | `string` | Directory the generated MSI is written to. Must exist at build time. |
| `MsiFilename` | `string` | File name **without** directory and **without** the `.msi` extension — the build appends it. |

### `ControlPanelInfo` — all optional

| Member | Type | Notes |
|---|---|---|
| `ProductIcon` | `string?` | Icon shown next to the product entry. Must exist at build time when set. |
| `Comments` | `string?` | Free-text comments shown for the product. |
| `Contact` | `string?` | Support contact. |
| `HelpLink` | `string?` | URL of the support/help page. |
| `UrlInfoAbout` | `string?` | URL of the product's "about" page. |

Each member is applied only when it carries a non-blank value; the others are left at the MSI default
rather than overwritten with a blank Control Panel field.

### `Shortcut`

| Member | Type | Notes |
|---|---|---|
| `ShortcutPath` | `string` | Where the shortcut is created — a special-folder token such as `%Desktop%` or `%ProgramMenu%`, optionally with a sub-folder (`%ProgramMenu%\Contoso`). |
| `ShortcutName` | `string` | Display name. |
| `TargetPath` | `string` | The installed file it launches, expressed against the install directory: `[INSTALLDIR]\Widget.exe`. |
| `IconPath` | `string?` | Optional icon file. Must exist at build time when set. |
| `Arguments` | `string?` | Optional command-line arguments passed to the target. |

Every shortcut's working directory is set to the install directory by the build, so a shortcut does not
need to state it.

### `UiSettings`

| Member | Type | Default | Notes |
|---|---|---|---|
| `Wui` | `Wui` | `WixUI_InstallDir` | The built-in WixUI dialog set hosting the managed dialogs. |
| `InstallDialogs` | `List<Dialog>` | empty | Ordered dialogs for a fresh install. Must not be empty when `Ui` is set. |
| `ModifyDialogs` | `List<Dialog>` | empty | Ordered dialogs for modify / repair / remove. Must not be empty when `Ui` is set. |

`UiSettings.CreateDefault()` returns `WixUI_InstallDir` with a `Welcome → InstallDir → Progress → Exit`
install sequence and a `Welcome → MaintenanceType → Progress → Exit` modify sequence. It is what the
build applies when `MsiPackage.Ui` is `null`, and it is public precisely so a UI can pre-fill the same
values when the user starts customizing — leaving `Ui` at `null` and calling `CreateDefault()` produce
the same installer.

## Enums

All four are WixSharp-free mirrors: the public surface never exposes a WixSharp type, and the net472
worker maps each value to the real WixSharp one. Reflection drift tests guard the mirrors against a
WixSharp upgrade that renames or adds a member.

| Enum | Members | Notes |
|---|---|---|
| `InstallScope` | `PerMachine`, `PerUser` | A deliberate subset — WixSharp's `perUserOrMachine` (scope chosen at install time) is out of scope at v1. |
| `CompressionLevel` | `None`, `Low`, `Medium`, `High`, `MsZip` | `None` is the fastest build and the largest MSI; `High` the reverse, and the default. |
| `Wui` | `WixUI_Minimal`, `WixUI_InstallDir`, `WixUI_FeatureTree`, `WixUI_Mondo`, `WixUI_Advanced`, `WixUI_ProgressOnly`, `WixUI_Common` | Underscored names mirror WixSharp's `WUI` verbatim, which is why they deviate from house PascalCase. |
| `Dialog` | `Welcome`, `Licence`, `Features`, `InstallDir`, `SetupType`, `Progress`, `MaintenanceType`, `Exit` | Note the British spelling `Licence` — it matches WixSharp's member name verbatim. |

## Usage

### A minimal package

Everything the validator insists on, and nothing else: no Control Panel block, no shortcuts, and the
default managed UI by omission.

```csharp
using System;
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
```

`UpgradeCode` is parsed from a literal on purpose: it is the product line's permanent identity, so it
belongs in source (or in a committed profile) rather than being generated per build. `ProductId`, which
identifies this one version, can be generated freely.

### Shortcuts and Control Panel information

```csharp
using System;
using System.Collections.Generic;
using Enigma.Msi.Model;

var package = new MsiPackage
{
    AppName = "Contoso Widget",
    Version = new Version(1, 2, 3),
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
        MsiFilename = "ContosoWidget-1.2.3"
    },
    ControlPanel = new ControlPanelInfo
    {
        ProductIcon = @"C:\src\Widget\Assets\widget.ico",
        Comments = "Contoso Widget desktop client.",
        HelpLink = "https://contoso.example/widget/support"
    },
    Shortcuts = new List<Shortcut>
    {
        new Shortcut
        {
            ShortcutPath = "%Desktop%",
            ShortcutName = "Contoso Widget",
            TargetPath = @"[INSTALLDIR]\Widget.exe"
        },
        new Shortcut
        {
            ShortcutPath = @"%ProgramMenu%\Contoso",
            ShortcutName = "Contoso Widget",
            TargetPath = @"[INSTALLDIR]\Widget.exe",
            Arguments = "--from-start-menu"
        }
    }
};
```

### Customizing the managed UI

Start from the defaults and change what you need, rather than assembling a dialog list from nothing —
that way a sequence stays valid as the default set evolves.

```csharp
using System.Collections.Generic;
using Enigma.Msi.Model;

UiSettings ui = UiSettings.CreateDefault();
ui.Wui = Wui.WixUI_Mondo;
ui.InstallDialogs = new List<Dialog>
{
    Dialog.Welcome,
    Dialog.Licence,
    Dialog.InstallDir,
    Dialog.Progress,
    Dialog.Exit
};

var package = new MsiPackage
{
    AppName = "Contoso Widget",
    Ui = ui
};
```

### A per-user, uncompressed package

Per-user installs need no elevation, and `CompressionLevel.None` trades MSI size for build speed —
useful while iterating on a package locally.

```csharp
using Enigma.Msi.Model;

var package = new MsiPackage
{
    AppName = "Contoso Widget",
    Scope = InstallScope.PerUser,
    Compression = CompressionLevel.None
};
```

## Saving and loading profiles

A saved package is a `<name>.msipkg.json` file, and `MsiPackageJson` is the only thing that reads or
writes it. Centralizing the serializer options there is what guarantees the modern host and the net472
worker exchange byte-compatible JSON.

| Member | Signature | Role |
|---|---|---|
| `Options` | `JsonSerializerOptions` | The shared options. Never mutate them — copy them if you need different settings. |
| `Serialize` | `string Serialize(MsiPackage package)` | Writes the `.msipkg.json` form. |
| `Deserialize` | `MsiPackage Deserialize(string json)` | Reads it back. Throws `JsonException` on malformed input. |
| `SerializeResult` | `string SerializeResult(MsiBuildResult result)` | The worker's result-file format. |
| `DeserializeResult` | `MsiBuildResult DeserializeResult(string json)` | Reads a result written by the worker. |

The wire format is camelCase, indented, `null` members omitted, enums written as their member names,
`Version` as `"1.2.3"` and `Guid` as the canonical hyphenated form. Reads are deliberately more lenient
than writes: member names are matched case-insensitively, unknown members are ignored for forward
compatibility, and GUIDs are accepted in any form `Guid.TryParse` handles — including the brace form
`{9b4a0f2e-…}` that Visual Studio and `uuidgen` emit.

```csharp
using System.IO;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;

// Save
File.WriteAllText(@"C:\src\Widget\Widget.msipkg.json", MsiPackageJson.Serialize(package));

// Load
MsiPackage loaded = MsiPackageJson.Deserialize(
    File.ReadAllText(@"C:\src\Widget\Widget.msipkg.json"));
```

Handle the two failure modes separately — an unreadable file and an unparseable one have different
fixes:

```csharp
using System;
using System.IO;
using System.Text.Json;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;

try
{
    MsiPackage loaded = MsiPackageJson.Deserialize(File.ReadAllText(path));
    Console.WriteLine($"Loaded {loaded.AppName}.");
}
catch (JsonException exception)
{
    Console.Error.WriteLine($"'{path}' is not a valid .msipkg.json document: {exception.Message}");
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"'{path}' could not be read: {exception.Message}");
}
```

### The profile format

This is the fully-populated package above as the library actually writes it — note the member order,
which follows the model's declaration order rather than alphabetical:

```json
{
  "schemaVersion": 1,
  "appName": "Contoso Widget",
  "version": "1.2.3",
  "productId": "9b4a0f2e-1c3d-4b5a-8e6f-7a8b9c0d1e2f",
  "upgradeCode": "2f1e0d9c-8b7a-6f5e-4b3c-2d1e0f9a8b7c",
  "manufacturer": "Contoso AG",
  "scope": "PerMachine",
  "install": {
    "installPath": "%ProgramFiles%\\Contoso\\Widget",
    "releasePath": "C:\\src\\Widget\\bin\\Release\\net10.0"
  },
  "output": {
    "outputPath": "C:\\src\\Widget\\artifacts",
    "msiFilename": "ContosoWidget-1.2.3"
  },
  "compression": "High",
  "controlPanel": {
    "productIcon": "C:\\src\\Widget\\Assets\\widget.ico",
    "comments": "Contoso Widget desktop client.",
    "contact": "support@contoso.example",
    "helpLink": "https://contoso.example/widget/support",
    "urlInfoAbout": "https://contoso.example/widget"
  },
  "shortcuts": [
    {
      "shortcutPath": "%Desktop%",
      "shortcutName": "Contoso Widget",
      "targetPath": "[INSTALLDIR]\\Widget.exe"
    },
    {
      "shortcutPath": "%ProgramMenu%\\Contoso",
      "shortcutName": "Contoso Widget",
      "targetPath": "[INSTALLDIR]\\Widget.exe",
      "iconPath": "C:\\src\\Widget\\Assets\\widget.ico",
      "arguments": "--from-start-menu"
    }
  ],
  "ui": {
    "wui": "WixUI_InstallDir",
    "installDialogs": [
      "Welcome",
      "InstallDir",
      "Progress",
      "Exit"
    ],
    "modifyDialogs": [
      "Welcome",
      "MaintenanceType",
      "Progress",
      "Exit"
    ]
  }
}
```

The minimal package from the first sample writes as this — `controlPanel` and `ui` are absent because
they are `null`, while `shortcuts` is present but empty, because an empty list is not a missing one:

```json
{
  "schemaVersion": 1,
  "appName": "Contoso Widget",
  "version": "1.0.0",
  "productId": "9b4a0f2e-1c3d-4b5a-8e6f-7a8b9c0d1e2f",
  "upgradeCode": "2f1e0d9c-8b7a-6f5e-4b3c-2d1e0f9a8b7c",
  "manufacturer": "Contoso AG",
  "scope": "PerMachine",
  "install": {
    "installPath": "%ProgramFiles%\\Contoso\\Widget",
    "releasePath": "C:\\src\\Widget\\bin\\Release\\net10.0"
  },
  "output": {
    "outputPath": "C:\\src\\Widget\\artifacts",
    "msiFilename": "ContosoWidget-1.0.0"
  },
  "compression": "High",
  "shortcuts": []
}
```

## Notes

- **`UpgradeCode` is forever.** Change it and Windows Installer stops recognizing the new package as an
  upgrade of the old one, so both versions install side by side. Generate it once per product, keep it in
  the committed profile, and only ever change `ProductId` and `Version` between releases.
- **`MsiFilename` carries no extension and no directory.** `"ContosoWidget-1.0.0"` is right;
  `"ContosoWidget.msi"` and `@"artifacts\ContosoWidget"` are both validation errors.
- **`Version` is narrower than `System.Version`.** Windows Installer only reads `major.minor.build`, with
  major and minor up to 255 and build up to 65535. A fourth component is accepted by the model and
  ignored by the installer, so never encode anything meaningful in it.
- **`Ui = null` means "the default UI", not "no UI".** For an installer with no interaction, set
  `Ui` explicitly to `Wui.WixUI_ProgressOnly`.
- **The profile format is versioned.** `schemaVersion` is always written, and reading a profile from a
  future schema is reported as a validation error rather than silently misinterpreted. There is no
  importer for the predecessor project's `<App>.msiprofile.<version>.json` format — the break is
  deliberate.
- **`MsiPackageJson.Options` is shared and must not be mutated.** System.Text.Json seals a
  `JsonSerializerOptions` instance on first use anyway; copy it if you need different settings.
