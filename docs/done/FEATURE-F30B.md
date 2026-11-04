# FEATURE-F30B — Desktop app 1.9.0 release

## Summary

**Enigma.Msi.Desktop 1.9.0** is prepared and verified on Windows. It is an app-only SemVer minor: the
user's rule `x.y+1.z` takes 1.8.0 to 1.9.0. It carries this run's other item, `FEATURE-1AC4` (the
frame-filling `final3` icon set), which is `DONE`. No item was quarantined, and the library stays at 1.0.0.

- **Version:** `<Version>1.9.0</Version>` in the desktop csproj.
- **The buildmsi script's version:** the release did not edit `packaging/windows/buildmsi.ps1`, because
  the script has no version literal. It reads `/Project/PropertyGroup/Version` from that csproj and names
  the MSI and its profile after it. The dogfood build shows it: it produced `Enigma.Msi.1.9.0.msi` with
  `ProductVersion` 1.9.0.
- **Docs:**
  - `RELEASENOTES.md` has a 1.9.0 section (*Changed*, *Upgrading from 1.8*, *Compatibility*), and its
    version table says 1.9.0;
  - the README callout is about 1.9.0;
  - `SECURITY.md` supports 1.9.x;
  - `CLAUDE.md` has a 1.9.0 build-state paragraph.

## Files/modules touched

- Modified:
  - `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`, `CLAUDE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-F30B.md`
- Created: `docs/done/FEATURE-F30B.md`

## Deviations & follow-ups

- **`buildmsi.ps1` is unchanged, and the draft asked for a change.** "Update the new version number in
  the buildmsi script" is met by the csproj bump the script reads (see *Decisions*). If you want a version
  literal or a `-Version` override in the script, that is a separate change, and it would diverge from the
  other eight Enigma apps' scripts.
- **`docs/RELEASE.md` needed no version edit.** It names only historical facts (the 1.7.0 whose upgrade
  code the script keeps, and the 1.0.0 to 1.7.0 profiles) and a generic example `X.Y.Z` (`1.3.0`).
- **Documentation sweep:** the edits above are the whole of it. Nothing else names the current version,
  and no guide describes the icon.
- **The MSI is in this repository's `artifacts/`** (git-ignored): `Enigma.Msi.1.9.0.msi` and its
  `.msipkg.json`, copied from the scratch clone. The profile's absolute paths name that clone. For a
  distributable build, rerun `buildmsi` on the merged `main`.
- **Still the maintainer's** (`docs/RELEASE.md` §§3–5):
  - install, launch and uninstall;
  - checking that 1.9.0 upgrades the installed Enigma Msi 1.8.0, with one entry in *Installed apps* and
    the new icon on its shortcuts;
  - the merge into `main` and the `1.9.0` tag.

  Nothing was tagged, pushed, installed or merged outside the run branch.
- **Owed on Linux:** a run of `install.sh` with the new icons. It was not run here.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Avalonia 12.1.5 | Held at 12.1.4 | It is a coupled set, and Enigma.Avalonia.Desktop 1.3.0 (still the latest, not reported outdated) is built against 12.1.4 |
| Scratch clone contents | A clone of the dev branch (`f02cafd`) with the dev's six modified tracked files copied over | `git clean -dxf` removes untracked files only, so the payload is exactly the dev's tree. The clone differs from this commit only in CLAUDE.md and this record, neither of which is in the payload |
| Notes on upgrading from 1.7.0 | Mentioned with 1.8.0 | Both were installed from MSIs with the same upgrade code, and the Upgrade table detects every version below 1.9.0 |

## Build/test evidence

Run baseline: see `docs/done/FEATURE-1AC4.md` (0 warnings, 421/421).

- `git tag --list 1.9.0` was empty.
- `dotnet list Enigma.Msi.slnx package --outdated` reported only Avalonia, Avalonia.Desktop,
  Avalonia.Fonts.Inter, Avalonia.Themes.Fluent and Avalonia.Headless at 12.1.4 → 12.1.5. Every other
  project had no update.
- `dotnet build Enigma.Msi.slnx -c Release --no-incremental` gave **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.Msi.slnx -c Release --no-build` gave **421 / 421 passed**: the worker
  suite and its drift guards, the library suite and the desktop suite.
- `Enigma.Msi.Desktop.dll` is `FileVersion` 1.9.0.0, `ProductVersion` `1.9.0+f02cafd…`.
- **Dogfood** (`buildmsi.bat` in the scratch clone, WiX 7.0.0): exit 0 in 48 s, with its three steps
  logged. Afterwards, the clone's `git status --porcelain` listed only the six copied files.
  - `artifacts\Enigma.Msi.1.9.0.msi`: 74,338,677 bytes. The publish folder's `app.ico` is the `final3`
    icon (`cmp`), and the File table's `app.ico` is 20,626 bytes, its size.
  - Property: `ProductName` Enigma Msi, `ProductVersion` 1.9.0, `UpgradeCode`
    `{49589519-F722-4733-AF70-81E027756344}`, `ALLUSERS` 1, `ARPPRODUCTICON` `app_icon.ico`.
  - Upgrade: detection below 1.9.0 (`WIX_UPGRADE_DETECTED`) and from 1.9.0 up (`WIX_DOWNGRADE_DETECTED`)
    on that code.
  - Directory: `INSTALLDIR` "Enigma Msi" under `ProgramFilesFolder`.
  - Shortcut: two "Enigma Msi" shortcuts, `DesktopFolder` and `ProgramMenuFolder`, on
    `[INSTALLDIR]\Enigma.Msi.Desktop.exe`, with icon `IconFile1_app.ico`.
  - File: 107 files, including `app.ico` and `Enigma.Msi.Desktop.exe` 1.9.0.0, plus the six seeded
    `worker\.wixsharp\` files (`Enigma.Msi.Worker.exe`/`.pdb`, `WixSharp.dll`, `WixSharp.UI.dll`,
    `WixToolset.Dtf.WindowsInstaller.dll`, `WixToolset.Mba.Core.dll`).
