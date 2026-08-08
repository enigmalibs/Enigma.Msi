# The worker CLI

`Enigma.Msi.Worker.exe` is the `net472` executable that does the actual MSI building — the only process in
the solution that touches WixSharp. The library normally drives it for you, but it is also a supported
entry point on its own: given a saved `.msipkg.json` profile, it builds the MSI with no .NET host, no
project reference and no library involved. That is what makes it usable from a CI job.

Both entry points funnel through the same code path, so the headless CLI and the library's build client
cannot drift apart: the worker re-validates the package with the full validator, translates it to WixSharp,
builds it, and reports the outcome.

The worker is a one-shot process with no configuration, no logging framework and no DI. Everything WixSharp
and the WiX toolchain emit goes to stdout and stderr, which is exactly what lets a caller stream it as a
live build log — and what makes the CLI's output readable in a CI transcript.

## Requirements

| Requirement | Notes |
|---|---|
| Windows | The worker is `net472` and MSI generation is Windows-only. |
| .NET Framework 4.7.2 | Present on every supported Windows version. |
| The WiX CLI | `dotnet tool install --global wix`. The worker drives it; without it, the build fails. |

## Command line

Two forms, and nothing else — this is the worker's own usage text:

```text
Usage:
  Enigma.Msi.Worker.exe --request <request.json> --result <result.json>
  Enigma.Msi.Worker.exe build <file.msipkg.json>
```

| Form | Mode | Who uses it |
|---|---|---|
| `build <file.msipkg.json>` | CLI | You, and CI. Builds a saved profile and reports on the console. |
| `--request <req> --result <res>` | Internal | `IMsiBuildService`. Reads a package from `<req>`, writes an `MsiBuildResult` to `<res>`. |

The internal form is documented because you will see it in a process list, not because you should call it
— use the library for that.

**Parsing is strict.** Anything the two forms do not describe is rejected with a reason and a usage
message, rather than ignored: an unknown argument, a repeated option, an option whose value is missing (or
is another option), a `build` with the wrong number of arguments. This is a deliberate correction of the
predecessor project, whose parser scanned for the options it recognized and silently dropped the rest — so
a typo like `--results out.json` produced a build that looked fine and wrote its outcome nowhere. Here that
typo is an exit-code-2 usage error.

## Exit codes

The exit codes are a contract; CI can branch on them.

| Code | Meaning | Typical cause |
|---|---|---|
| `0` | The MSI was produced. | — |
| `1` | The input was well-formed, but the operation failed. | The package failed validation, or the MSI build itself failed. |
| `2` | Bad arguments, unreadable input, or an unexpected error. | A typo in an option, a missing profile file, a profile that is not valid JSON. |

The `1` / `2` split is what makes the codes useful: `1` means "your package or your toolchain has a
problem the log describes", `2` means "the worker was never given something it could act on". A WiX
authoring failure is a `1`, not a `2` — the input was fine, the build was not.

## Output

On success, the MSI's path goes to **stdout**:

```text
MSI written to C:\src\Widget\artifacts\ContosoWidget-1.0.0.msi
```

On failure, **every** reason goes to **stderr** — never just the first, which is the whole point of the
aggregating validator:

```text
The build failed with 3 error(s):
  appName: Required.
  output.msiFilename: Must not include the .msi extension — the build appends it.
  install.releasePath: Directory does not exist.
```

Validation violations are rendered as `path: message`, with the path in the profile's own JSON casing, so
a message points straight at the member of the `.msipkg.json` file that produced it. A build failure
instead reports one line per exception in the chain (`ExceptionType: message`), because WixSharp likes to
wrap the actionable message — a missing `wix` CLI, a WiX authoring error — inside a generic outer
exception.

A usage error prints the reason and the usage text to stderr and exits `2`.

## Usage

### Building a profile

```powershell
.\worker\Enigma.Msi.Worker.exe build .\Widget.msipkg.json
```

The MSI lands in the profile's `output.outputPath`, named `output.msiFilename` + `.msi`. Paths inside the
profile are resolved relative to the **current working directory**, so run the worker from a predictable
place — or make every path in the profile absolute, which is what the desktop app writes.

### Where the worker is

| Context | Path |
|---|---|
| Inside the `Enigma.Msi` nupkg | `tools/worker/Enigma.Msi.Worker.exe` |
| A project referencing the package | `$(OutDir)worker\Enigma.Msi.Worker.exe` — put there automatically |
| Beside the desktop app | `worker\Enigma.Msi.Worker.exe` |

For a CI job that has no .NET project at all, unpacking the nupkg is enough: a `.nupkg` is a zip, and
`tools/worker/` is self-contained.

```powershell
# Get the worker without building anything that references it.
nuget install Enigma.Msi -Version 1.0.0 -OutputDirectory packages -ExcludeVersion
.\packages\Enigma.Msi\tools\worker\Enigma.Msi.Worker.exe build .\Widget.msipkg.json
```

### In a CI job

```yaml
# GitHub Actions — windows-latest is required; MSI building is Windows-only.
jobs:
  installer:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install the WiX CLI
        run: dotnet tool install --global wix

      - name: Build the application
        run: dotnet build src/Widget/Widget.csproj -c Release

      - name: Build the installer
        run: .\src\Widget\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build .\Widget.msipkg.json

      - uses: actions/upload-artifact@v4
        with:
          name: installer
          path: artifacts/*.msi
```

The application build is a **hard prerequisite** of the installer step, and not only for the obvious
reason: the profile's `install.releasePath` points at the app's release output, and the validator rejects
a release folder that does not exist or is empty before WiX is ever invoked.

### Branching on the exit code

```powershell
.\worker\Enigma.Msi.Worker.exe build .\Widget.msipkg.json

switch ($LASTEXITCODE) {
    0 { Write-Host 'Installer built.' }
    1 { throw 'The package or the WiX build failed — see the output above.' }
    2 { throw 'The worker was invoked wrongly, or the profile could not be read.' }
}
```

```bash
# Git Bash / MSYS on Windows
if ./worker/Enigma.Msi.Worker.exe build ./Widget.msipkg.json; then
  echo "Installer built."
else
  code=$?
  echo "Worker failed with exit code ${code}." >&2
  exit "${code}"
fi
```

## Notes

- **The profile is the whole input.** The CLI takes no overrides — no `--version`, no `--output`. To build
  a variant, write a variant profile; that keeps what was built reproducible from a file in source
  control.
- **The worker validates its input even though the library already did.** It is a supported entry point of
  its own, and must never hand a half-filled package to WixSharp. Expect the same violations from both.
- **Relative paths follow the working directory, not the profile's location.** Absolute paths inside the
  profile remove the ambiguity.
- **Cancellation is the caller's job in CLI mode.** The library kills the worker's whole process tree on
  cancellation; a shell that kills only the worker can leave `wix` processes behind. In CI, cancel the job
  rather than the process.
- **Exit `2` never means "the package is bad".** If you see it on a profile that used to build, suspect
  the invocation or the file, not the model.
