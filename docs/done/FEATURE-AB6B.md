# FEATURE-AB6B — Desktop app 1.7.0 release

## Summary

**Enigma.Msi.Desktop 1.7.0** is prepared and verified on Windows. It is an app-only SemVer minor (the user's
rule: 1.6.0 → 1.7.0) carrying this run's eight `DONE` items:
- `FEATURE-D37E`: no `net8.0`;
- `FEATURE-CED6`: packages updated;
- `FEATURE-5E0B`: no splash;
- `FEATURE-394B`: the uniform About dialog;
- `FEATURE-C7BF`: `app.ico` in the output;
- `FEATURE-2EEB`: the safety net;
- `FEATURE-22F0`: Linux install and uninstall;
- `FEATURE-C19D`: publish scripts.

No item was quarantined. The library stays at 1.0.0.

Release changes:
- **Version:** `<Version>1.7.0</Version>` in the desktop csproj.
- **Profile:** `msiProfiles/Enigma.Msi.Desktop.1.7.0.msipkg.json`, cloned per the runbook's rule. Its `diff`
  against 1.6.0 is exactly `version` and `productId`, the new `productId` being
  `e7853b7b-aa67-44cf-bb45-79aab0c730df` from `[guid]::NewGuid()`. The `upgradeCode` is verbatim and
  `compression` stays `High`.
- **Docs:**
  - `RELEASENOTES.md` has a 1.7.0 section (*Added*, *Changed*, *Compatibility*), and its version table says
    1.7.0;
  - the README callout is about 1.7.0, and its self-build example names the 1.7.0 profile;
  - `SECURITY.md` supports 1.7.x;
  - `CLAUDE.md` has a 1.7.0 build-state paragraph.
- **The WixSharp 2.14.2 seed check** (follow-up from FEATURE-CED6) is closed. The dogfood build from a
  seeded worker left `worker\.wixsharp\` byte- and timestamp-identical, so the worker csproj comment and
  `WixSharpBuildDirTests` now say 2.14.2 too.

## Files/modules touched

- Modified:
  - `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`
  - `src/Enigma.Msi.Worker/Enigma.Msi.Worker.csproj` (comment)
  - `tests/Enigma.Msi.Worker.UnitTests/WixSharpBuildDirTests.cs` (comment)
  - `RELEASENOTES.md`
  - `README.md`
  - `SECURITY.md`
  - `CLAUDE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-AB6B.md`
- Created:
  - `msiProfiles/Enigma.Msi.Desktop.1.7.0.msipkg.json`
  - `docs/done/FEATURE-AB6B.md`

## Deviations & follow-ups

- **The MSI is 27.73 MB, against 1.5.0's 17.29 MB.** The payload is now the ReadyToRun publish
  (FEATURE-C19D), whose app assemblies are about twice their IL size. That is the trade the user's publish
  command makes, and the notes say it.
- **Still the maintainer's** (`docs/RELEASE.md` §§3–5):
  - install, launch and uninstall;
  - checking that it upgrades an installed 1.6.0;
  - the merge to `main` and the `1.7.0` tag.

  Nothing was tagged, pushed or merged outside the run branch.
- **Owed on Linux:** a run of the Linux-published app (FEATURE-22F0).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the MSI tables were read | A scratchpad PowerShell script over `WindowsInstaller.Installer`, not committed | As the earlier releases did. An `[int]` cast was needed for `StringData` |
| Which publish fed the MSI | `packaging\windows\publish.ps1`, as the runbook now says | The release proves the documented path |

## Documentation sweep

- README, RELEASENOTES, SECURITY and CLAUDE.md are listed above. `docs/RELEASE.md` needed nothing more:
  FEATURE-C19D and FEATURE-5E0B already updated it.

## Build/test evidence

- `git tag --list 1.7.0`: empty.
- `dotnet build Enigma.Msi.slnx -c Release --no-incremental`: **0 warnings**, 0 errors.
- `dotnet test --solution Enigma.Msi.slnx -c Release`: **417/417 passed**, exit 0. The worker suite
  (`net472`) passed with its 16 WixSharp drift guards and `WixSharpBuildDirTests`.
- `Enigma.Msi.Desktop.dll` carries FileVersion `1.7.0.0` and ProductVersion `1.7.0+48362b6…`.
- Dogfood:
  1. `mkdir artifacts`.
  2. `packaging\windows\publish.ps1`: exit 0.
  3. `worker\Enigma.Msi.Worker.exe build msiProfiles\Enigma.Msi.Desktop.1.7.0.msipkg.json`: **exit 0** in
     34 s with WiX 7.0.0, producing `artifacts\Enigma.Msi.Desktop.msi` at **27,730,327 bytes**.

  Read back from the MSI:
  - **Property:**
    - `ProductName` Enigma.Msi.Desktop;
    - `ProductVersion` **1.7.0**;
    - `ProductCode` {E7853B7B-…C730DF}, new;
    - `UpgradeCode` **{3405046F-…DC8314}**, unchanged;
    - `Manufacturer` Josué Clément;
    - `ARPPRODUCTICON` set.
  - **Upgrade:** the same UpgradeCode. `VersionMax 1.7.0` with `WIX_UPGRADE_DETECTED` means older versions
    are upgraded, and `VersionMin 1.7.0` with `WIX_DOWNGRADE_DETECTED` means downgrades are detected.
  - **Shortcut:** Desktop and Program Menu, both targeting `[INSTALLDIR]\Enigma.Msi.Desktop.exe`.
  - **File** (107 files):
    - `app.ico`, 24,018 bytes (= the asset);
    - `Enigma.Msi.Desktop.dll`, 606,208 bytes (ReadyToRun);
    - `Enigma.Msi.Worker.exe`;
    - under `.wixsharp`, the six seeded files: the worker exe and `.pdb`, `WixSharp.dll`,
      `WixSharp.UI.dll`, `WixToolset.Dtf.WindowsInstaller.dll`, `WixToolset.Mba.Core.dll`.
