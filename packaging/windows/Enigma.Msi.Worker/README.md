# Enigma.Msi worker CLI (vendored)

A copy of this repository's own headless worker, released with the app's 1.7.0.
`packaging/windows/buildmsi.ps1` runs it to build the app's installer,
`artifacts/Enigma.Msi.X.Y.Z.msi`:

```text
Enigma.Msi.Worker.exe build <file.msipkg.json>
```

**Why a copy, when `src/Enigma.Msi.Worker/` builds the same program?** The installer is built by a
released worker, not by the one under development. A change to the worker in `src/` can never break the
build of the release that ships it. Every Enigma app also carries the same copy in the same place, so
`buildmsi.ps1` is the same script everywhere. The worker that goes **into** the MSI, beside the app in
`worker\`, is still the one built from `src/` by the publish.

The copy is committed rather than built, so the installer build has nothing extra to compile.

Exit codes:
- `0`: the MSI was written, and its path is printed.
- `1`: the profile is invalid or the WiX build failed. Every reason is printed.
- `2`: bad arguments or an unreadable profile.

[`docs/guides/worker-cli.md`](../../../docs/guides/worker-cli.md) documents the CLI, and
[`docs/guides/model.md`](../../../docs/guides/model.md) documents the profile.

## Source

| | |
|---|---|
| Repository | Enigma.Msi (this one), tag `1.7.0` (commit `a7a1bfb`) |
| Folder | `src/Enigma.Msi.Worker/bin/Release/net472/` |
| Version stamp | `Enigma.Msi.dll` and `Enigma.Msi.Worker.exe`: `1.0.0+a7a1bfb0cfae951132b2738ebddfb076683f2113` |
| WixSharp | `WixSharp_wix4` 2.14.2 |
| Left out | `*.pdb`, `Enigma.Msi.xml`, `.wixsharp/` |

The files are the build output unchanged, except `Enigma.Msi.Worker.exe.config`. Git stores that file
with LF line endings (`.gitattributes`), while the build writes CRLF. The other Enigma apps (Backup,
FileExplorer, GitClient, HardCopy, Launchpad, MarkdownEditor, TextEditor) carry the same binaries in
their own `packaging/windows/Enigma.Msi.Worker/`.

## Requirements

- Windows with .NET Framework 4.7.2 or later.
- The WiX CLI on the `PATH`: `dotnet tool install --global wix`. WiX 7 also needs its licence accepted
  once (`wix eula accept wix7`).

## `.wixsharp/`

On its first build, WixSharp writes a `.wixsharp/` folder beside the worker, holding the files it
packages for the installer's managed UI. That folder is git-ignored. `git clean -dxf` removes it, and the
next build writes it again. Unlike the in-tree build (`BUG-7325`), the copy is not seeded: this folder is
writable, and the copy is never installed.

## Licences

| Files | Project | Licence |
|---|---|---|
| `Enigma.Msi.Worker.exe` (+ `.config`), `Enigma.Msi.dll` | Enigma.Msi | MIT |
| `WixSharp.dll`, `WixSharp.Msi.dll`, `WixSharp.UI.dll`, `WixSharp.UI.WPF.dll`, `WixSharp.MsiEventHost.exe` | WixSharp_wix4 2.14.2 | MIT |
| `WixToolset.Dtf.WindowsInstaller.dll`, `WixToolset.Mba.Core.dll`, `mbanative.dll` | WiX Toolset 4.0.1 | MS-RL |
| `System.*.dll`, `Microsoft.Bcl.AsyncInterfaces.dll` | .NET | MIT |

None of these files goes into the MSI; they only build it.

## Refreshing it

When this repository releases a worker that changes:

1. On the release tag: `dotnet build src/Enigma.Msi.Worker/Enigma.Msi.Worker.csproj -c Release`.
2. Replace this folder's files with `src/Enigma.Msi.Worker/bin/Release/net472/*`, leaving out `*.pdb`,
   `Enigma.Msi.xml` and `.wixsharp/`. Keep this README.
3. Update the *Source* table, then build an MSI with `packaging/windows/buildmsi.ps1`.
4. Refresh the copy in the other Enigma apps the same way.
