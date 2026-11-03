# FEATURE-5897 — Desktop app 1.8.0 release

## Summary

**Enigma.Msi.Desktop 1.8.0** is prepared and verified on Windows. It is an app-only SemVer minor (the
user's rule: 1.7.0 → 1.8.0) carrying this run's two other items, both `DONE`:
- `FEATURE-62E1`: the new icon set without the tile;
- `FEATURE-3E44`: the vendored worker and `buildmsi.ps1` / `buildmsi.bat`.

No item was quarantined. The library stays at 1.0.0.

Release changes:
- **Version:** `<Version>1.8.0</Version>` in the desktop csproj. There is no committed profile:
  `buildmsi.ps1` writes it per build.
- **Docs:**
  - `RELEASENOTES.md` has a 1.8.0 section (*Added*, *Changed*, *Upgrading from 1.7*, *Compatibility*), and
    its version table says 1.8.0;
  - the README callout is about 1.8.0;
  - `SECURITY.md` supports 1.8.x;
  - `CLAUDE.md` has a 1.8.0 build-state paragraph.

## Files/modules touched

- Modified:
  - `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`, `CLAUDE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-5897.md`
- Created: `docs/done/FEATURE-5897.md`

## Deviations & follow-ups

- **`docs/RELEASE.md` needed no version edit.** Its only specific versions are facts: the 1.7.0 whose
  upgrade code the script keeps, and the 1.0.0 to 1.7.0 profiles. Its example `X.Y.Z` (`1.3.0`) is
  generic.
- **The MSI is in this repository's `artifacts/`** (git-ignored): `Enigma.Msi.1.8.0.msi` and its
  `.msipkg.json`, copied from the scratch clone. The profile's absolute paths name that clone, which is
  the record of where it was built. For a distributable build, rerun `buildmsi` on the merged `main`.
- **Still the maintainer's** (`docs/RELEASE.md` §§3–5):
  - install, launch and uninstall;
  - checking that 1.8.0 upgrades the installed Enigma Msi 1.7.0, with one entry in *Installed apps*;
  - the merge into `main` and the `1.8.0` tag.

  Nothing was tagged, pushed, installed or merged outside the run branch.
- **Owed on Linux:** a run of the Linux install with the new icons (`install.sh`). It was not run here.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Avalonia 12.1.5 | Held at 12.1.4 | The coupled set; Enigma.Avalonia.Desktop 1.3.0 (still the latest) is built against 12.1.4 |
| Order of the dogfood build and CLAUDE.md | MSI built first, then the paragraph with its figures | The clone's tree differs from the commit only in CLAUDE.md and this record, neither in the payload |

## Build/test evidence

Run baseline: see `docs/done/FEATURE-62E1.md` (0 warnings, 417/417).

- `git tag --list 1.8.0` was empty.
- `dotnet list Enigma.Msi.slnx package --outdated` reported only Avalonia, Avalonia.Desktop,
  Avalonia.Fonts.Inter, Avalonia.Themes.Fluent and Avalonia.Headless at 12.1.4 → 12.1.5. Every other
  project had no update.
- `dotnet build Enigma.Msi.slnx -c Release --no-incremental` gave **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.Msi.slnx -c Release --no-build` gave **421 / 421 passed**: the worker
  suite and its drift guards, the library suite and the desktop suite.
- `Enigma.Msi.Desktop.dll` is `FileVersion` 1.8.0.0, `ProductVersion` `1.8.0+44ecc9f…`.
- **Dogfood** (`buildmsi.bat` in a scratch clone of this dev's tree): exit 0 in 18 s, with the three
  steps logged. The clone's `git status --porcelain` was empty afterwards.
  - `artifacts\Enigma.Msi.1.8.0.msi`: 74,310,005 bytes, WiX 7.0.0. Its `app.ico` is the new icon
    (`cmp`).
  - Property: `ProductName` Enigma Msi, `ProductVersion` 1.8.0, `UpgradeCode`
    `{49589519-F722-4733-AF70-81E027756344}`, `ALLUSERS` 1, `ARPPRODUCTICON` set.
  - Upgrade: detection below and above 1.8.0 on that code.
  - Directory: `INSTALLDIR` "Enigma Msi" under `ProgramFilesFolder`.
  - Shortcut: two "Enigma Msi" shortcuts, `DesktopFolder` and `ProgramMenuFolder`, on
    `[INSTALLDIR]\Enigma.Msi.Desktop.exe`.
  - File: 107 files, including `app.ico` and `Enigma.Msi.Desktop.exe` 1.8.0.0, plus the six seeded
    `worker\.wixsharp\` files (`Enigma.Msi.Worker.exe`/`.pdb`, `WixSharp.dll`, `WixSharp.UI.dll`,
    `WixToolset.Dtf.WindowsInstaller.dll`, `WixToolset.Mba.Core.dll`).
