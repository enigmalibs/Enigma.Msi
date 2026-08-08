# Validating a package

`IMsiPackageValidator` answers one question about an `MsiPackage`: everything that is wrong with it. Not
the first thing — *every* violation, each paired with the member path that produced it. You create an
`MsiPackageValidator`, hand it a package, and get back an `MsiValidationResult` carrying a list of
`MsiValidationError`.

The aggregating shape is the point. A caller that only learns about one violation at a time forces the
user through one build-fail-fix cycle per mistake, which is exactly what filling in a dozen-field
installer description does not need. Every rule runs on every call, so a package with six problems
reports six problems.

The rules are split in two by *cost*, not by importance: the in-memory rules never touch the file system,
so a UI can run them on every keystroke, while the environment rules stat directories and files and are
therefore run when a build is about to start. `IMsiBuildService.BuildAsync` runs both before it spawns
anything.

## The three methods

| Method | Rules run | Touches disk | Use it for |
|---|---|---|---|
| `Validate` | In-memory only | no | As-you-type feedback; gating a Build button. |
| `ValidateEnvironment` | Environment only | yes | Checking that the paths a package refers to are really there. |
| `ValidateAll` | Both, in-memory first | yes | What a build must pass. This is what `BuildAsync` calls. |

All three take an `MsiPackage` and return an `MsiValidationResult`; all three throw
`ArgumentNullException` when the package is `null`. `MsiPackageValidator` is stateless and thread-safe,
so one instance can be shared or registered as a singleton.

## Key types

| Type | Namespace | Role |
|---|---|---|
| `IMsiPackageValidator` | `Enigma.Msi.Validation` | The interface. DI-friendly. |
| `MsiPackageValidator` | `Enigma.Msi.Validation` | The default implementation. Create with `new`. |
| `MsiValidationResult` | `Enigma.Msi.Validation` | `Errors` (`IReadOnlyList<MsiValidationError>`) and `IsValid`. `MsiValidationResult.Valid` is the shared empty result. |
| `MsiValidationError` | `Enigma.Msi.Validation` | `Path` and `Message`; `ToString()` renders `path: message`. |

`MsiValidationError.Path` is written in the **JSON casing of the profile format**, not in C# casing —
`install.releasePath`, `output.msiFilename`, `shortcuts[0].targetPath`, `ui.installDialogs[2]`. That is
deliberate: it lets a UI map a violation straight back to the field that produced it, and it means an
error read out of a profile and an error reported by the library name the same thing.

## In-memory rules

| Path | Rule |
|---|---|
| `schemaVersion` | At least 1, and no greater than `MsiPackage.CurrentSchemaVersion`. A higher value reports the supported ceiling instead of being misread. |
| `appName` | Non-blank. |
| `manufacturer` | Non-blank. |
| `version` | Present; major and minor at most 255, build at most 65535. |
| `productId` | Not `Guid.Empty`. |
| `upgradeCode` | Not `Guid.Empty`. |
| `scope` | A defined `InstallScope` member. |
| `compression` | A defined `CompressionLevel` member. |
| `install` | Not `null` (JSON can supply `"install": null` even though the member is non-nullable). |
| `install.installPath` | Non-blank. |
| `install.releasePath` | Non-blank. |
| `output` | Not `null`. |
| `output.outputPath` | Non-blank. |
| `output.msiFilename` | Non-blank; a plain file name (no directory separators, no characters invalid in a file name); must not end in `.msi`. |
| `shortcuts` | Not `null` — use an empty list for "no shortcuts". |
| `shortcuts[i]` | Not `null`. |
| `shortcuts[i].shortcutPath`, `.shortcutName`, `.targetPath` | Non-blank. |
| `ui.wui` | A defined `Wui` member — checked only when `Ui` is set. |
| `ui.installDialogs`, `ui.modifyDialogs` | At least one dialog each when `Ui` is set; every entry a defined `Dialog` member. |

A `null` `Ui` is not a violation — it means the build applies `UiSettings.CreateDefault()`.

## Environment rules

| Path | Rule |
|---|---|
| `install.releasePath` | Must be an existing directory, and must not be empty — an empty release folder means there is nothing to package. |
| `output.outputPath` | Must be an existing directory. The build does not create it. |
| `controlPanel.productIcon` | Must be an existing file, when set. |
| `shortcuts[i].iconPath` | Must be an existing file, when set. |

Two details worth knowing. A path that exists but is a *file* where a directory is expected gets its own
message rather than "does not exist", because the fix is different. And blank values are **skipped**
here: they are already reported by the in-memory rules, and reporting the same member twice in one
`ValidateAll` would be noise.

## Usage

### Checking a package before doing anything with it

```csharp
using System;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;

IMsiPackageValidator validator = new MsiPackageValidator();
MsiValidationResult result = validator.ValidateAll(package);

if (result.IsValid)
{
    Console.WriteLine("The package is ready to build.");
}
else
{
    Console.Error.WriteLine($"{result.Errors.Count} problem(s):");

    foreach (MsiValidationError error in result.Errors)
    {
        // "install.releasePath: Directory does not exist."
        Console.Error.WriteLine($"  {error}");
    }
}
```

### As-you-type validation in a UI

Run only the in-memory rules while the user types, and keep the environment rules for the moment a build
starts. Typing a path one character at a time would otherwise hit the file system on every keystroke.

```csharp
using System.Linq;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;

IMsiPackageValidator validator = new MsiPackageValidator();

// Called from a property-changed handler: cheap, no I/O.
bool CanBuild(MsiPackage current) => validator.Validate(current).IsValid;

// Mapping violations back onto the form's fields.
string? ErrorFor(MsiPackage current, string memberPath) => validator
    .Validate(current)
    .Errors
    .FirstOrDefault(error => error.Path == memberPath)
    ?.Message;
```

### Reporting the two rule sets separately

Useful when the two kinds of problem deserve different treatment — a missing field is the user's to fix
in the form, a missing directory is theirs to fix on disk.

```csharp
using System;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;

IMsiPackageValidator validator = new MsiPackageValidator();

MsiValidationResult model = validator.Validate(package);
MsiValidationResult environment = validator.ValidateEnvironment(package);

Console.WriteLine($"{model.Errors.Count} field problem(s), {environment.Errors.Count} on disk.");
```

### Grouping violations by member

`Path` is stable and machine-readable, which makes it a usable grouping key.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;

IMsiPackageValidator validator = new MsiPackageValidator();

IEnumerable<IGrouping<string, MsiValidationError>> byMember = validator
    .ValidateAll(package)
    .Errors
    .GroupBy(error => error.Path);

foreach (IGrouping<string, MsiValidationError> group in byMember)
{
    Console.WriteLine($"{group.Key}: {string.Join("; ", group.Select(error => error.Message))}");
}
```

### Registering the validator for injection

```csharp
using Enigma.Msi.Validation;
using Microsoft.Extensions.DependencyInjection;

services.AddSingleton<IMsiPackageValidator, MsiPackageValidator>();
```

## Notes

- **A validation failure is not an exception.** The only thing the validator throws is
  `ArgumentNullException` for a `null` package. Everything about the package's *content* comes back as
  data, including violations that make the package unbuildable.
- **`BuildAsync` validates for you.** It runs `ValidateAll` before spawning the worker and returns the
  violations as a failed `MsiBuildResult` — you do not have to validate first, though doing so lets you
  report problems without the cost of a process launch.
- **The worker validates again.** It is a supported entry point of its own (see
  [the worker CLI](worker-cli.md)), so it re-runs `ValidateAll` rather than trusting its input. The
  double check is deliberate defence in depth, not redundancy to remove.
- **`Validate` alone is not enough to build.** It cannot see that a release folder is missing. Gate a
  Build *button* on `Validate`, but expect `ValidateAll` to still find problems when the build starts.
- **Environment rules are a snapshot.** A directory that exists when you validate can be gone when you
  build; the worker's own `ValidateAll` is what actually protects the build.
