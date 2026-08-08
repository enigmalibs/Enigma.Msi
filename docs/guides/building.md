# Building an MSI

`IMsiBuildService` turns a validated `MsiPackage` into an `.msi` file. You create an `MsiBuildService`,
optionally check its prerequisites, then call `BuildAsync` and get back an `MsiBuildResult` saying whether
the MSI was produced and where it went.

Underneath, the build runs **out of process**. WixSharp is .NET Framework-only, so the code that touches
it lives in a `net472` worker executable that ships inside the `Enigma.Msi` package and is discovered on
disk at run time. The library writes the package to a request file, runs the worker over it, streams the
worker's output back line by line, and reads the outcome from a result file. None of that is visible on
the API — but two of its consequences are, and they are worth knowing before your first build: the worker
has to actually be *there*, and the WiX CLI it drives has to be installed. That is what
`CheckPrerequisitesAsync` exists for.

Failures come back as data, not exceptions. A package that fails validation, an undiscoverable worker and
a WiX toolchain error all arrive as a failed `MsiBuildResult` carrying every reason. Only two things
throw: `ArgumentNullException` for a `null` package, and `OperationCanceledException` when you cancel.

## Requirements

| Requirement | Notes |
|---|---|
| Windows | MSI generation is Windows-only. The library itself loads anywhere; a build does not. |
| The WiX CLI | `dotnet tool install --global wix`. `CheckPrerequisitesAsync` reports its absence with that exact hint. |
| The worker on disk | Shipped in the package under `tools/worker/` and copied into your output automatically. See [Worker discovery](#worker-discovery). |

The library targets `netstandard2.0`, `net8.0` and `net10.0`.

## Key types

| Type | Namespace | Role |
|---|---|---|
| `IMsiBuildService` | `Enigma.Msi.Build` | The interface. DI-friendly. |
| `MsiBuildService` | `Enigma.Msi.Build` | The default implementation. Create with `new`; stateless and thread-safe once constructed. |
| `MsiBuildServiceOptions` | `Enigma.Msi.Build` | Where to find the worker and the WiX CLI. Every member has a working default. |
| `MsiPrerequisites` | `Enigma.Msi.Build` | What the pre-flight check found, and every unmet prerequisite. |
| `MsiBuildResult` | `Enigma.Msi.Build` | The outcome: `Success`, `MsiPath`, `Errors`. |

The interface is two methods:

```csharp
Task<MsiPrerequisites> CheckPrerequisitesAsync(
    CancellationToken cancellationToken = default);

Task<MsiBuildResult> BuildAsync(
    MsiPackage package,
    IProgress<string>? log = null,
    CancellationToken cancellationToken = default);
```

`MsiBuildService` can be constructed with `new MsiBuildService()` for the common case, or
`new MsiBuildService(options)` when your layout differs from the one the package produces. The library
ships **no** `AddEnigmaMsi()` extension — registration is the consumer's choice:

```csharp
using Enigma.Msi.Build;
using Enigma.Msi.Validation;
using Microsoft.Extensions.DependencyInjection;

services.AddSingleton<IMsiPackageValidator, MsiPackageValidator>();
services.AddSingleton<IMsiBuildService>(_ => new MsiBuildService());
```

## `MsiBuildResult`

| Member | Type | Notes |
|---|---|---|
| `Success` | `bool` | Whether the MSI was produced. |
| `MsiPath` | `string?` | Full path of the generated MSI on success; `null` otherwise. |
| `Errors` | `IReadOnlyList<string>` | Every reason the build did not succeed. Empty on success. |

Validation violations arrive here pre-rendered as `path: message` strings — the same form
[`MsiValidationError.ToString()`](validation.md) produces.

## `MsiPrerequisites`

| Member | Type | Notes |
|---|---|---|
| `WorkerPath` | `string?` | The worker that was found, or `null` when discovery failed. |
| `WixToolVersion` | `string?` | What the WiX CLI reported for `--version`, or `null` when it could not be run. |
| `Problems` | `IReadOnlyList<string>` | Every unmet prerequisite, each phrased as something the user can act on. |
| `WorkerFound` | `bool` | Whether the worker executable was found. |
| `WixToolAvailable` | `bool` | Whether the WiX CLI ran and reported a version. |
| `IsSatisfied` | `bool` | Whether a build can be started — that is, `Problems` is empty. |

The check reports problems as data rather than throwing, so a UI can show them next to a disabled Build
button instead of failing the user's first build with console noise. The WiX probe gives up after 30
seconds and reports the tool as unavailable: a pre-flight that hangs is worse than one that says "no".

## Usage

### The simplest build

```csharp
using System;
using System.Threading.Tasks;
using Enigma.Msi.Build;
using Enigma.Msi.Model;

IMsiBuildService builder = new MsiBuildService();

MsiBuildResult result = await builder.BuildAsync(package);

if (result.Success)
{
    Console.WriteLine($"Built {result.MsiPath}");
}
else
{
    foreach (string error in result.Errors)
    {
        Console.Error.WriteLine(error);
    }
}
```

### Checking prerequisites first

Worth doing before the user's first build — it distinguishes "your package is wrong" from "this machine
cannot build MSIs at all".

```csharp
using System;
using System.Threading.Tasks;
using Enigma.Msi.Build;

IMsiBuildService builder = new MsiBuildService();

MsiPrerequisites prerequisites = await builder.CheckPrerequisitesAsync();

if (!prerequisites.IsSatisfied)
{
    foreach (string problem in prerequisites.Problems)
    {
        Console.Error.WriteLine(problem);
    }

    return;
}

Console.WriteLine($"Worker: {prerequisites.WorkerPath}");
Console.WriteLine($"WiX: {prerequisites.WixToolVersion}");
```

### Streaming the build log

The WiX toolchain is chatty, and its output is the only place a toolchain-level diagnostic appears. Pass
an `IProgress<string>` to receive it line by line as it arrives.

```csharp
using System;
using System.Threading.Tasks;
using Enigma.Msi.Build;
using Enigma.Msi.Model;

IMsiBuildService builder = new MsiBuildService();

var log = new Progress<string>(line => Console.WriteLine(line));

MsiBuildResult result = await builder.BuildAsync(package, log);
```

`log` is called from a **background thread** and must not throw. In a UI application, marshal each line
onto the UI thread — and append it to a collection rather than rebuilding a single string, or a long
build turns the log into quadratic work.

### Cancelling a build

Cancellation terminates the worker **and its children**, so a cancelled build leaves no orphaned `wix`
processes behind. `BuildAsync` throws `OperationCanceledException` once the worker is gone.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Msi.Build;
using Enigma.Msi.Model;

IMsiBuildService builder = new MsiBuildService();

using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    MsiBuildResult result = await builder.BuildAsync(package, log: null, cancellation.Token);
    Console.WriteLine(result.Success ? $"Built {result.MsiPath}" : "Build failed.");
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("The build was cancelled; the worker has been terminated.");
}
```

### Pointing at a worker somewhere else

Set `WorkerPath` when the worker does not sit beside your application — a test host, a tool that ships the
worker in its own layout, or a consumer that renamed the destination folder.

```csharp
using Enigma.Msi.Build;

var options = new MsiBuildServiceOptions
{
    WorkerPath = @"C:\tools\enigma-msi\Enigma.Msi.Worker.exe",
    WixToolPath = @"C:\Users\me\.dotnet\tools\wix.exe"
};

IMsiBuildService builder = new MsiBuildService(options);
```

`WixToolPath` defaults to `"wix"`, which resolves through `PATH` — where `dotnet tool install --global
wix` puts it. Set it to a full path only when the tool is installed somewhere `PATH` does not cover.

## Worker discovery

When `MsiBuildServiceOptions.WorkerPath` is `null` or blank, the worker is looked for at
`AppContext.BaseDirectory/worker/Enigma.Msi.Worker.exe` — exposed as the static
`MsiBuildService.DefaultWorkerPath`. The three parts of that path are also available as constants:

| Constant | Value |
|---|---|
| `MsiBuildServiceOptions.WorkerFileName` | `Enigma.Msi.Worker.exe` |
| `MsiBuildServiceOptions.WorkerFolderName` | `worker` |
| `MsiBuildServiceOptions.DefaultWixToolPath` | `wix` |

Getting the worker there is automatic. The `Enigma.Msi` package ships it under `tools/worker/` together
with `build/Enigma.Msi.targets`, which NuGet imports into every consuming project; the targets file copies
the payload to `$(OutDir)worker/`. It is expressed as `None` items carrying `CopyToOutputDirectory` rather
than a post-build copy task, so `dotnet publish` picks it up too.

Two MSBuild properties adjust that:

| Property | Effect |
|---|---|
| `IncludeEnigmaMsiWorker` | Set to `false` to skip the copy entirely — for a project that references the model but never builds an MSI. |
| `EnigmaMsiWorkerFolderName` | Renames the destination folder. If you set it, you must also set `MsiBuildServiceOptions.WorkerPath` to match, because discovery looks in `worker/`. |

```xml
<PropertyGroup>
  <!-- This project only reads and writes .msipkg.json profiles; it never builds an MSI. -->
  <IncludeEnigmaMsiWorker>false</IncludeEnigmaMsiWorker>
</PropertyGroup>
```

When discovery fails, the failure message says where it looked, and it distinguishes the two cases: a
configured path that is wrong is a wrong setting, an empty default location is a missing deployment step.

## Notes

- **Validate-then-build is already built in.** `BuildAsync` runs the full validator before spawning
  anything, so an invalid package costs you a millisecond and a complete list of violations rather than a
  process launch and a one-line failure. Validating first is still useful — see
  [Validating a package](validation.md) — but never required.
- **Each build gets its own temporary directory.** It lives under the system temp folder, doubles as the
  worker's working directory, and is deleted afterwards, so whatever intermediates the WiX toolchain
  leaves behind go with it instead of accumulating next to your application.
- **`OutputSettings.OutputPath` must already exist.** The build writes into it; it does not create it.
- **Concurrent builds are fine.** `MsiBuildService` is stateless once constructed and two builds share
  nothing but the options — but they will contend for CPU, since each spawns a full WiX toolchain.
- **A worker that exits without writing a result** is reported as such, with its exit code, and its output
  is in the log you streamed. That is the shape of a crashed toolchain rather than a rejected package.
- **The build log is the only place toolchain diagnostics appear.** `MsiBuildResult.Errors` carries the
  *outcome*; when WiX fails on authoring or environment grounds, the actionable detail is in the streamed
  output. Capture it even when you do not display it.
