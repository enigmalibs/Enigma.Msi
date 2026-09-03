# Release runbook

Reusable checklist for releasing a new **Enigma.Msi.Desktop** version. The app is the only artifact this
repository releases: it ships as an `.msi` installer, built by this repo's own `net472` worker from a
committed profile — the dogfood loop. The **Enigma.Msi** library underneath it is not published to NuGet or
to any other feed; it is consumed in-repo by `ProjectReference`, holds at 1.0.0, and has no release flow of
its own. There is no `dotnet pack` step anywhere in this document.

Replace `X.Y.Z` with the version being released (e.g. `1.3.0`) throughout. The app's version lives in
`src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` (`<Version>`) and moves independently of the library's:
the two coincided at 1.0.0 for the first release and parted at the app's 1.1.0.

**Nothing in this repository runs an outward-facing command for you.** No step here pushes, tags, uploads or
publishes on its own — the merge, the tag and wherever the MSI ends up are all the maintainer's, run by hand.

Run every command **from the repository root** — the MSI profile's paths are all repo-root-relative.

## 1. Pre-release checks

Run from the repository root, on the branch that will be merged:

- [ ] `<Version>X.Y.Z</Version>` set in `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`.
- [ ] `RELEASENOTES.md` has a top `X.Y.Z` section describing the release (newest-first; any `(unreleased)`
      heading renamed to `X.Y.Z`), and its current-version table names the new app version.
- [ ] `README.md`'s "what's new" callout reflects `X.Y.Z`.
- [ ] `msiProfiles/Enigma.Msi.Desktop.X.Y.Z.msipkg.json` committed, cloned per [the profile clone
      rule](#the-profile-clone-rule) below.
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

Two commands, both **from the repository root**:

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
- The worker invoked here is the one `build/CopyWorkerOutput.targets` copied next to the app during the
  build — the same executable the app itself drives, so the released installer is built by the code being
  released.
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
`shortcuts` — is carried over unchanged, unless the app's TFM moved (then `releasePath` follows it). Older
profiles are kept as the record of what each release was built from. The profile is the library's own
`.msipkg.json` format, so it can be opened, edited and validated in the desktop app itself.

## 3. Merge to the default branch

Merge the release branch into the default (published) branch — `main` — via a pull request (or
fast-forward), then check it out locally:

```bash
git switch main
git pull
```

## 4. Tag the release

The convention here is a **bare** `X.Y.Z` tag — `git tag` shows `1.0.0` and `1.1.0`, no `v` prefix. The
app-only 1.1.0 was tagged bare, alongside the library's `1.0.0`, so in practice both artifacts share one
bare-version tag namespace rather than the app getting a prefix (e.g. `desktop/X.Y.Z`). Keeping that is the
path of least surprise; splitting the namespaces is still open. Tag the merge commit and push the tag:

```bash
git tag X.Y.Z
git push origin X.Y.Z
```

## 5. Post-release verification

On a test machine, against the MSI from §2:

- [ ] It installs, launches and uninstalls cleanly:
      ```bash
      msiexec /i artifacts\Enigma.Msi.Desktop.msi
      msiexec /x artifacts\Enigma.Msi.Desktop.msi
      ```
- [ ] Installed **over the previous version, it upgrades rather than accumulates** — one Control Panel entry
      at `X.Y.Z`, not two. This is what the reused `upgradeCode` buys, and the only step that actually
      proves it.
- [ ] The installed app starts, the splash hands over to the main window, and About reports `X.Y.Z`.
- [ ] The GitHub release/tag is present and its notes match `RELEASENOTES.md`.
