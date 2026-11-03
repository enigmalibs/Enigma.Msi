# Release runbook

Reusable checklist for releasing a new **Enigma.Msi.Desktop** version. The app is the only artifact this
repository releases: it ships as an `.msi` installer, built by `packaging/windows/buildmsi.ps1` with a
released copy of this repo's own `net472` worker — the dogfood loop. The **Enigma.Msi** library underneath
it is not published to NuGet or to any other feed; it is consumed in-repo by `ProjectReference`, holds at
1.0.0, and has no release flow of its own. There is no `dotnet pack` step anywhere in this document.

Replace `X.Y.Z` with the version being released (e.g. `1.3.0`) throughout. The app's version lives in
`src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` (`<Version>`) and moves independently of the library's:
the two coincided at 1.0.0 for the first release and parted at the app's 1.1.0.

**Nothing in this repository runs an outward-facing command for you.** No step here pushes, tags, uploads or
publishes on its own — the merge, the tag and wherever the MSI ends up are all the maintainer's, run by hand.

Run the commands **from the repository root**. `buildmsi.ps1` and `publish.ps1` resolve the repository from
their own location, so they also run from anywhere.

## 1. Pre-release checks

Run from the repository root, on the branch that will be merged:

- [ ] `<Version>X.Y.Z</Version>` set in `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`.
- [ ] `RELEASENOTES.md` has a top `X.Y.Z` section describing the release (newest-first; any `(unreleased)`
      heading renamed to `X.Y.Z`), and its current-version table names the new app version.
- [ ] `README.md`'s "what's new" callout reflects `X.Y.Z`.
- [ ] Clean, warning-free build across all TFMs:
      ```bash
      dotnet build Enigma.Msi.slnx -c Release
      ```
- [ ] Full test suite green:
      ```bash
      dotnet test --solution Enigma.Msi.slnx -c Release
      ```
      The `--solution` flag is **not optional**: this repo runs the Microsoft Testing Platform runner (see
      `global.json`), and on the .NET 10 SDK in MTP mode a bare `dotnet test <solution>` is rejected. The
      run must include the `net472` worker suite and its WixSharp **drift guards** — a drift-guard failure
      is a real finding about a WixSharp upgrade, never a flake.
- [ ] The docs still describe the shipped UI: `docs/guides/desktop-app.md` against the app as it now
      behaves, and `CLAUDE.md`'s build-state block against what this release changed.
- [ ] The guides' snippets under `docs/guides/` verified against the built library, if a guide or the
      README quick-start snippet was touched.

## 2. Build the MSI

One command, on Windows:

```powershell
.\packaging\windows\buildmsi.ps1
# Or, from a cmd prompt, with no execution policy involved:
packaging\windows\buildmsi.bat
```

- **It runs `git clean -dxf` first**, which deletes every file git does not track, ignored ones included.
  Commit or move anything new before running it. Then it publishes with `publish.ps1`, which runs
  `dotnet publish src\Enigma.Msi.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true`. Last, it
  packages the publish folder with Enigma.Msi.
- **It writes** `artifacts\Enigma.Msi.X.Y.Z.msi`, beside the `Enigma.Msi.X.Y.Z.msipkg.json` profile it was
  built from. `X.Y.Z` is `<Version>` in the csproj. It creates `artifacts\` itself, and `artifacts/` is
  git-ignored.
- **Prerequisites**, checked before anything is cleaned: `git`, the .NET 10 SDK and the WiX CLI
  (`dotnet tool install --global wix`) on the `PATH`. WiX 7 also needs its licence accepted once:
  `wix eula accept wix7`.
- **Its exit code** is `0` when the MSI is written. A failing step stops it with that step's exit code
  and a `buildmsi:` line on stderr. The worker's own codes are `1` for an invalid profile or a failed WiX
  build, and `2` for bad arguments.
- **The payload is the publish output**, never the raw `bin\Release\net10.0` build. The raw build carries
  `runtimes/` for Linux, macOS and browser: 552 MB of Skia/HarfBuzz natives. Measured, that gave a 156 MB
  MSI installing 580 MB, against 17 MB installing 44 MB for the publish output.
- **Framework-dependent by design**: a RID-specific publish is framework-dependent unless told otherwise, so
  the target machine needs the .NET 10 runtime. Adding `--self-contained` would remove that prerequisite —
  it roughly triples the installer. **ReadyToRun** precompiles the app's own assemblies so it starts
  faster, at the cost of larger files.
- **Two workers, on purpose.** The worker that *builds* the MSI is the released copy in
  `packaging\windows\Enigma.Msi.Worker\`: a change to `src\Enigma.Msi.Worker` cannot break the build of
  the release that ships it. Its README says where it came from and how to refresh it. The worker that
  the MSI *installs* is the one the publish builds from `src\`, which `build/CopyWorkerOutput.targets`
  puts in `worker\` beside the app.
- **Where the MSI goes afterwards is a per-release decision** — a GitHub release asset alongside the tag is
  the obvious home; nothing in the repo automates it.

What the MSI installs:

| What | Where |
|---|---|
| the application, for all users | `%ProgramFiles%\Enigma Msi` (a 32-bit package: `Program Files (x86)` on 64-bit Windows), with the worker in `worker\` |
| the shortcuts, "Enigma Msi" | the desktop and the program menu |
| the entry in *Installed apps* | "Enigma Msi", publisher "Josué Clément" |

### The Windows installer's identity

There is no committed MSI profile per version. `buildmsi.ps1` writes the profile on every build from the
csproj's `<Version>` and its own constants, so a release changes nothing for the installer.

- **`upgradeCode`** is `49589519-f722-4733-af70-81e027756344`, written once in `buildmsi.ps1`. It is the
  code the MSI that installed 1.7.0 carries. It must never change: an MSI with another code installs
  beside the old version instead of replacing it. `WindowsInstallerIdentityTests` pins it, together with
  the product name and the executable the shortcuts start.
- **`productId`** is new for every build.
- **The product name is "Enigma Msi"**, which is also the install folder's and the shortcuts' name. The
  app itself still calls itself `Enigma.Msi`.
- **`compression` is `None`**, so the build is fast.

`msiProfiles/` holds the profiles this repository's dogfood MSIs of 1.0.0 to 1.7.0 were built from,
under the clone rule this section replaces. Their upgrade code, `3405046f-527a-439e-a22f-866247dc8314`, is
not the installed 1.7.0's, no installed product carries it, and it is not used again. The profiles stay
as the record of those builds. They are ordinary `.msipkg.json` files and open in the app.

## 3. Merge to the default branch

Merge the release branch into the default (published) branch — `main` — via a pull request (or
fast-forward), then check it out locally:

```bash
git switch main
git pull
```

## 4. Tag the release

The convention here is a **bare** `X.Y.Z` tag with no `v` prefix, as every existing tag in `git tag`
shows. The app-only 1.1.0 was tagged bare, alongside the library's `1.0.0`, so in practice both artifacts
share one bare-version tag namespace rather than the app getting a prefix (e.g. `desktop/X.Y.Z`). Keeping that is the
path of least surprise; splitting the namespaces is still open. Tag the merge commit and push the tag:

```bash
git tag X.Y.Z
git push origin X.Y.Z
```

## 5. Post-release verification

On a test machine, against the MSI from §2:

- [ ] It installs, launches and uninstalls cleanly:
      ```bash
      msiexec /i artifacts\Enigma.Msi.X.Y.Z.msi
      msiexec /x artifacts\Enigma.Msi.X.Y.Z.msi
      ```
- [ ] Installed **over the previous version, it upgrades rather than accumulates**. *Installed apps* lists one
      **Enigma Msi**, at `X.Y.Z`, not two. This is what the fixed upgrade code buys, and this is the only
      step that actually proves it.
- [ ] The desktop and program-menu shortcuts, "Enigma Msi", start it on the main window, and About reports
      `X.Y.Z`.
- [ ] The GitHub release/tag is present and its notes match `RELEASENOTES.md`.
