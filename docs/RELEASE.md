# Release runbook

Reusable checklist for publishing a new **Enigma.Msi** version to NuGet, and for building the companion
**Enigma.Msi.Desktop** MSI installer. Only the packable library project (`src/Enigma.Msi/`) is published to
NuGet; the worker ships *inside* that package under `tools/worker/`, and the desktop app is released as an
`.msi` rather than as a package.

Replace `X.Y.Z` with the version being released (e.g. `1.2.0`) throughout. The library version lives in
`src/Enigma.Msi/Enigma.Msi.csproj` (`<Version>`); the desktop app carries its own independent `<Version>` in
`src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` and is not published to NuGet. They coincided at 1.0.0
and parted at the app's 1.1.0; nothing keeps them in step.

Run every command **from the repository root** — the MSI profile's paths are all repo-root-relative.

## 0. Which release is this?

Because the two artifacts version independently, not every release runs every section. Decide this
first — the sections below are *not* a single linear flow.

| Release flavour | Sections to run |
|---|---|
| **Library** (a new `Enigma.Msi` on NuGet) | §1–§6; also §7 if the app ships in the same release |
| **App only** (a new `Enigma.Msi.Desktop` MSI, library unchanged) | §1's build + test gate, then §7 and *Pre-flight for the app release* — **skip §4 and §5 entirely** |

For an **app-only** release there is no `dotnet pack` and no `dotnet nuget push`: the library's
`<Version>`, `<PackageReleaseNotes>` and the README badges are not touched, and re-packing an unchanged
library would only publish a duplicate. §1's library-specific checkboxes (library `<Version>`,
`<PackageReleaseNotes>`, README badges, `<TargetFrameworks>`) do not apply either — but the warning-free
build and the full test suite still gate the release, and `RELEASENOTES.md` is the single notes source
for both artifacts, so it still gets a section.

**Tagging an app-only release stays the maintainer's call, but there is now a precedent.** The app-only
1.1.0 was tagged bare `1.1.0` on its merge commit, alongside the library's `1.0.0` — so in practice the
two artifacts share one bare-version tag namespace rather than getting an app-scoped prefix (e.g.
`desktop/X.Y.Z`). Keeping that is the path of least surprise; splitting the namespaces is still open, and
nothing in this repository tags anything for you.

## 1. Pre-release checks

Run from the repository root, on the branch that will be merged:

- [ ] `<Version>X.Y.Z</Version>` set in `src/Enigma.Msi/Enigma.Msi.csproj`.
- [ ] `RELEASENOTES.md` has a top `X.Y.Z` section describing the release (newest-first; any `(unreleased)`
      heading renamed to `X.Y.Z`).
- [ ] `<PackageReleaseNotes>` in the library csproj summarizes the release and points to `RELEASENOTES.md`.
- [ ] README badges and the "what's new" callout reflect `X.Y.Z`.
- [ ] `<TargetFrameworks>` reflect the `net8.0` + `net10.0` policy (`netstandard2.0` preserved — it is what
      lets the `net472` worker consume the same model assembly); any change was proposed/confirmed and
      logged in `RELEASENOTES.md` *Compatibility*.
- [ ] Clean, warning-free build across all TFMs:
      ```bash
      dotnet build Enigma.Msi.slnx -c Release
      ```
- [ ] Full test suite green:
      ```bash
      dotnet test --solution Enigma.Msi.slnx -c Release
      ```
      The `--solution` flag is **not optional**: this repo runs the Microsoft Testing Platform runner (see
      `global.json`), and on the .NET 10 SDK in MTP mode a bare `dotnet test <solution>` is rejected.
- [ ] README samples and the guides under `docs/guides/` verified against the built version (the
      snippet-verification gate — re-run it whenever a guide or the README quick-start was touched).

## 2. Merge to the default branch

Merge the release branch into the default (published) branch — `main` — via a pull request (or
fast-forward), then check it out locally:

```bash
git switch main
git pull
```

## 3. Tag the release

The convention here is a **bare** `X.Y.Z` tag — `git tag` shows `1.0.0` and `1.1.0`, no `v` prefix. Tag the
merge commit and push the tag:

```bash
git tag X.Y.Z
git push origin X.Y.Z
```

## 4. Pack

`GeneratePackageOnBuild` is **off** for this library, so no `.nupkg` is produced on an ordinary build — pack
explicitly in Release to get the artifact you publish:

```bash
dotnet build Enigma.Msi.slnx -c Release      # hard prerequisite — see below
dotnet pack src/Enigma.Msi/Enigma.Msi.csproj -c Release -o ./artifacts
```

**The Release build is a prerequisite of the pack, not a nicety.** This package is unusual: besides `lib/`
it carries `tools/worker/` — the `net472` worker and its entire dependency closure (WixSharp, the WixToolset
assemblies, System.Text.Json) — which the `PackEnigmaMsiWorkerPayload` target *harvests from the worker's
build output* at `src/Enigma.Msi.Worker/bin/Release/net472/`. The target builds the worker itself if needed,
and fails the pack outright when the payload is missing rather than shipping a nupkg whose every
`BuildAsync` call would fail; packing in a configuration you have not built is still the shortest route to
a surprise. Never pack in `Debug` for a release.

This writes `./artifacts/Enigma.Msi.X.Y.Z.nupkg`. Confirm the version in the filename matches the tag, and
inspect the package contents — beyond `README.md` and `LICENSE.md` and the expected dependency floors, it
must contain:

- [ ] `lib/netstandard2.0/`, `lib/net8.0/`, `lib/net10.0/` — each with `Enigma.Msi.dll` **and**
      `Enigma.Msi.xml` (the doc file consumers get IntelliSense from).
- [ ] `tools/worker/Enigma.Msi.Worker.exe` + `Enigma.Msi.Worker.exe.config`, `WixSharp.dll`,
      `WixToolset.Dtf.WindowsInstaller.dll`, `WixToolset.Mba.Core.dll` and the rest of the closure.
- [ ] `build/Enigma.Msi.targets` — what copies `tools/worker/` into a consumer's `$(OutDir)worker/`.

An empty `tools/worker/` is the one packaging failure that is invisible until a consumer tries to build an
MSI, so check it every time.

## 5. Push to NuGet

Publish with a NuGet API key that has push rights for the `Enigma.Msi` package:

```bash
dotnet nuget push ./artifacts/Enigma.Msi.X.Y.Z.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

`dotnet pack` produces **only** the `.nupkg` — that single file is what gets pushed. A `.snupkg` symbols
package appears only when the library opts in to symbols (`IncludeSymbols` /
`SymbolPackageFormat=snupkg`); this library does not. The API key is a secret — never commit or echo it.

## 6. Post-publish verification

- [ ] The package page shows the new version: <https://www.nuget.org/packages/Enigma.Msi> (indexing can
      take a few minutes).
- [ ] The README NuGet badge resolves to `X.Y.Z` (shields.io caches briefly).
- [ ] A scratch project can restore the new version:
      ```bash
      dotnet add package Enigma.Msi --version X.Y.Z
      ```
- [ ] The GitHub release/tag is present and its notes match `RELEASENOTES.md`.

## 7. Build the desktop app's MSI

The app is released as an installer built by this repo's own worker — the dogfood loop. Two commands, both
**from the repository root**:

```bash
dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false
.\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.X.Y.Z.msipkg.json
```

- **The publish is a hard prerequisite**, not an alternative to the build. The profile's `releasePath` points
  at `src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish`, so whatever sits in that directory is
  what ships. A stale publish silently packages the previous version; a missing one fails validation. Do not
  re-point it at `bin\Release\net10.0`: the raw build output carries `runtimes/` for Linux, macOS and
  browser (552 MB of Skia/HarfBuzz natives), which measured a 156 MB MSI installing 580 MB against 17 MB
  installing 44 MB for the publish output.
- **Framework-dependent by design** (`--self-contained false`): the target machine needs the .NET 10
  runtime. Flip the flag if that prerequisite ever proves to be friction — it roughly triples the installer.
- **Requires Windows and the WiX CLI** (`dotnet tool install --global wix`), like any MSI this library
  builds.
- The MSI lands in `artifacts\Enigma.Msi.Desktop.msi` (`artifacts/` is git-ignored, so it never pollutes the
  tree). Worker exit codes are a contract: `0` success, `1` validation or build failure on well-formed
  input, `2` bad arguments or an unexpected error.
- **Where the MSI goes afterwards is a per-release decision** — a GitHub release asset alongside the tag is
  the obvious home; nothing in the repo automates it.

### The profile clone rule

Each version gets its **own committed profile**, `msiProfiles/Enigma.Msi.Desktop.X.Y.Z.msipkg.json`. To make
the next one, copy the most recent profile and change exactly three things:

| Field | Rule |
|---|---|
| `upgradeCode` | **Keep verbatim.** It is the app's permanent identity — `3405046f-527a-439e-a22f-866247dc8314`. Change it and Windows Installer treats the new release as an unrelated product: the old version is never upgraded, only accumulated alongside. |
| `productId` | **Generate a new GUID for every version**, from a real generator — `[guid]::NewGuid()` on Windows, `uuidgen` elsewhere. Never hand-fabricated. |
| `version` | Set to the app's release `X.Y.Z`, matching `<Version>` in `Enigma.Msi.Desktop.csproj`. |

Everything else — `installPath`, `releasePath`, `scope`, `compression`, `output`, `controlPanel`,
`shortcuts` — is carried over unchanged, unless the app's TFM moved (then `releasePath` follows it). The
profile is the library's own `.msipkg.json` format, so it can be opened, edited and validated in the desktop
app itself.

## Pre-flight for the app release

- [ ] `<Version>X.Y.Z</Version>` in `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`.
- [ ] `msiProfiles/Enigma.Msi.Desktop.X.Y.Z.msipkg.json` committed, cloned per the rule above.
- [ ] `RELEASENOTES.md` covers the app release (it is the single notes source for both artifacts).
- [ ] The built MSI installs, launches and uninstalls cleanly on a test machine:
      ```bash
      msiexec /i artifacts\Enigma.Msi.Desktop.msi
      msiexec /x artifacts\Enigma.Msi.Desktop.msi
      ```
