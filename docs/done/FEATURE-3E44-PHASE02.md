# FEATURE-3E44-PHASE02 — The buildmsi script

## Summary

`packaging/windows/buildmsi.ps1`, with its `buildmsi.bat` twin, builds the app's Windows installer in one
command. It checks that `git`, `dotnet`, `wix` and the vendored worker are there before touching
anything. It reads the app's `<Version>` from `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`, runs
`git clean -dxf` and `publish.ps1`, and writes `artifacts/Enigma.Msi.X.Y.Z.msipkg.json`. Then it runs the
vendored `Enigma.Msi.Worker.exe build` on that profile, which writes `artifacts/Enigma.Msi.X.Y.Z.msi`.

The script is Enigma.GitClient's, with only the planned adaptations (a `diff` against it shows nothing
else):
- the product name "Enigma Msi";
- the upgrade code of the installed 1.7.0, `49589519-f722-4733-af70-81e027756344`;
- the assembly `Enigma.Msi.Desktop`;
- the version read from the app's csproj, since this repository's `Directory.Build.props` has none;
- the MSI and profile named `Enigma.Msi.X.Y.Z`.

`WindowsInstallerIdentityTests` (Desktop suite, 4 tests) pins the script's upgrade code, product name and
assembly name, and that the csproj holds exactly one `X.Y.Z` `<Version>`, the one the assembly carries.
The docs now describe the script instead of the retired per-version profile clone rule:
- the README;
- `docs/RELEASE.md` (§2 and *The Windows installer's identity*);
- CLAUDE.md's layout;
- the csproj's `<Version>` comment and the publish scripts' comments.

## Files/modules touched

- Created:
  - `packaging/windows/buildmsi.ps1`, `packaging/windows/buildmsi.bat`
  - `tests/Enigma.Msi.Desktop.UnitTests/WindowsInstallerIdentityTests.cs`
  - `docs/done/FEATURE-3E44-PHASE02.md`
- Modified:
  - `README.md`, `docs/RELEASE.md`, `CLAUDE.md`
  - `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` (comment)
  - `packaging/windows/publish.ps1`, `packaging/windows/publish.bat` (comments)
  - `docs/roadmap.md`, `docs/plan/FEATURE-3E44.md`

## Deviations & follow-ups

- **The MSI is 74.3 MB uncompressed**, against the 27.73 MB the committed 1.7.0 profile's `High` gave.
  That is the plan's `None` ("exactly like GitClient"). The user's own installed
  `C:\Temp\Enigma.Msi.1.7.0.msi` is 74.3 MB as well, so it was built the same way.
- **The test MSI is 1.7.0**, the version this dev's tree still declares, so it is not meant to be
  installed. `FEATURE-5897` builds the 1.8.0 one.
- WixSharp logs "Source base directory" as the *invoking* process's directory. Every profile path is
  absolute, so that changes nothing, and the user's repository gained no file (`git status` unchanged).
- **Follow-up, not in scope:** the app still calls itself `Enigma.Msi` (About, the Linux launcher), while
  the MSI calls it "Enigma Msi". `docs/RELEASE.md` says so.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| A fourth test | The csproj `<Version>` test | The script's XPath fails if `<Version>` moves to `Directory.Build.props` or a second one appears; nothing else would notice |
| `.bat` line endings in the working tree | Written CRLF | Matches `publish.bat` and `.gitattributes` (`*.bat eol=crlf`), so cmd.exe runs it as checked out |
| The publish scripts' comments | Name `buildmsi.ps1` instead of `msiProfiles\` | They said the profiles there package the folder, which is no longer the release path |

## Build/test evidence

Run baseline: see `docs/done/FEATURE-62E1.md` (0 warnings, 417/417).

- `dotnet build Enigma.Msi.slnx -c Release --no-incremental` gave **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.Msi.slnx -c Release --no-build` gave **421 / 421 passed** (417 + the 4 new).
- `buildmsi.ps1` and `buildmsi.bat` are ASCII. The `.bat` is CRLF.
- **Scratch clone** (`git clone` of this branch, with the dev's tree committed there):
  - **`wix` off the `PATH`:** the script printed
    `buildmsi: wix is not on the PATH: dotnet tool install --global wix.` and exited 1. An untracked
    marker file survived, so `git clean` never ran.
  - **`buildmsi.bat` exited 0** in 18 s and ran the three steps (`git clean`, `publish.ps1`, the worker).
    It wrote `artifacts/Enigma.Msi.1.7.0.msi` (74,310,005 bytes) and `Enigma.Msi.1.7.0.msipkg.json`
    (UTF-8, no BOM, absolute paths, `"compression": "None"`). After it, `git status --porcelain` was
    empty: `.wixsharp/` beside the vendored worker is ignored.
  - **MSI tables**, read through the Windows Installer API:
    - Property: `ProductName` Enigma Msi, `Manufacturer` Josué Clément, `ProductVersion` 1.7.0,
      `UpgradeCode` `{49589519-F722-4733-AF70-81E027756344}`, `ALLUSERS` 1, `ARPPRODUCTICON`
      `app_icon.ico`.
    - Upgrade: `WIX_UPGRADE_DETECTED` / `WIX_DOWNGRADE_DETECTED` on that code.
    - Directory: `INSTALLDIR` `jtkr_isn|Enigma Msi` under `ProgramFilesFolder`, `worker` under it.
    - Shortcut: "Enigma Msi" in `DesktopFolder` and in `ProgramMenuFolder`, both on
      `[INSTALLDIR]\Enigma.Msi.Desktop.exe` with icon `IconFile1_app.ico`.
    - Media: one cabinet, 107 files.

    The installed 1.7.0's package has the same tables and the same 107 files.
