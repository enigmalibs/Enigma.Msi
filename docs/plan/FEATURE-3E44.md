# FEATURE-3E44 — Windows MSI build script

**Status:** DONE
**Type:** FEATURE
**Branch:** one per phase, see below
**Run:** feature/2026-11-03-icons-msi-release

## Objective

One command builds the app's Windows installer, as in Enigma.GitClient (`FEATURE-EAB7` there).
`packaging/windows/buildmsi.ps1`, with a `buildmsi.bat` twin, cleans the repository, publishes the
release and builds `artifacts/Enigma.Msi.X.Y.Z.msi` with a vendored copy of Enigma.Msi's worker CLI. The
product is named **"Enigma Msi"**, keeps the upgrade code of the installed Enigma Msi, and takes its
version from the app's csproj.

## Context & constraints

- **The installed product.** This machine has *Enigma Msi 1.7.0* installed from an MSI. I read its
  cached package `C:\Windows\Installer\132cf.msi` read-only while planning; the original was
  `C:\Temp\Enigma.Msi.1.7.0.msi`:
  - `UpgradeCode` `{49589519-F722-4733-AF70-81E027756344}`;
  - `ProductName` "Enigma Msi", `Manufacturer` "Josué Clément", `ALLUSERS` 1 (per machine);
  - `INSTALLDIR` = `ProgramFilesFolder\Enigma Msi`, with a `worker` subfolder;
  - shortcuts "Enigma Msi" in `DesktopFolder` and `ProgramMenuFolder`, target
    `[INSTALLDIR]\Enigma.Msi.Desktop.exe`, icon `app.ico`; `ARPPRODUCTICON` set.
  The script must keep that upgrade code. Another one would install 1.8.0 beside 1.7.0, into the same
  folder.
- **The committed profiles' upgrade code is not that one.** `msiProfiles/*.msipkg.json` (1.0.0 to 1.7.0)
  carry `3405046f-527a-439e-a22f-866247dc8314`, `appName` "Enigma.Msi.Desktop" and
  `%ProgramFiles%\Enigma.Msi`. No installed product has that code
  (`Installer.RelatedProducts` is empty). Those profiles built the dogfood MSIs that the runs only read
  back and never installed.
- **The worker to vendor.** This repository's `src/Enigma.Msi.Worker/bin/Release/net472/`, built at tag
  `1.7.0` (`a7a1bfb`, stamped `1.0.0+a7a1bfb`), is byte-identical to GitClient's committed
  `packaging/windows/Enigma.Msi.Worker/`, except the `.exe.config` (line endings). It was snapshotted to
  the session scratchpad before the baseline rebuilt it. HEAD's source equals the `1.7.0` tag's
  (`git diff 1.7.0 HEAD` is empty), so this is the current worker.
- **The worker CLI:** `Enigma.Msi.Worker.exe build <file.msipkg.json>`. Exit codes: `0` built, `1`
  invalid package or failed build, `2` bad invocation. It resolves relative paths against the working
  directory. WixSharp writes a `.wixsharp/` folder beside it on first use. The in-tree build seeds that
  folder (`BUG-7325`); the vendored copy leaves it out, as GitClient's does. The worker needs the WiX CLI
  (7.0.0 is installed here).
- **The publish** (`FEATURE-C19D`): `packaging/windows/publish.ps1` runs `dotnet publish
  src\Enigma.Msi.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true` and exits with dotnet's code.
  The result is `src\Enigma.Msi.Desktop\bin\Release\net10.0\win-x64\publish\`, with `app.ico`
  (`FEATURE-C7BF`) and `worker\` beside `Enigma.Msi.Desktop.exe`.
- **The version** lives in `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` (`<Version>`), not in
  `Directory.Build.props`: the library keeps its own 1.0.0.
- `.gitignore` ignores `artifacts/`, `*.pdb`, `[Bb]in/`, `x64/`, `[Rr]elease/`, `publish/` and `*.msi`
  (directories excepted by `!*.msi/`). The vendored folder must match none of them, because
  `git clean -dxf` deletes ignored files. `.gitattributes` marks `*.dll` / `*.exe` binary, stores `*.bat`
  CRLF and everything else LF.
- Windows PowerShell 5.1 reads a BOM-less script as ANSI, and `.editorconfig` says UTF-8 without BOM. So
  the `.ps1` stays ASCII.
- **The run may not run `git clean` in this repository.** The script is verified end to end in a scratch
  clone instead.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Upgrade code ("its own UpgradeCode") | `49589519-f722-4733-af70-81e027756344`, the installed 1.7.0's | Only that code makes 1.8.0 replace the Enigma Msi actually installed. GitClient kept its installed code the same way | `3405046f-…` from `msiProfiles/` (no installed product has it, so 1.8.0 would install beside 1.7.0 in the same folder); a new GUID |
| Product name, folder, shortcuts | "Enigma Msi"; `%ProgramFiles%\Enigma Msi`; "Enigma Msi" on the desktop and in the program menu | The user's name, and identical to the installed 1.7.0 | `Enigma.Msi.Desktop` / `Enigma.Msi` (the committed profiles') |
| MSI file name | `Enigma.Msi.X.Y.Z.msi`, profile `Enigma.Msi.X.Y.Z.msipkg.json` | The installed 1.7.0's package name, and GitClient's `<Repo>.X.Y.Z` pattern | `Enigma.Msi.Desktop.msi` (no version; the old runbook's name) |
| How to use Enigma.Msi | A copy of the 1.7.0 worker CLI committed in `packaging/windows/Enigma.Msi.Worker/` | The user's "add enigma.msi.worker", exactly as GitClient. A released worker builds the release, independent of the worker under development in `src/` | Running the worker the publish just built (`publish\worker\`), which breaks the "exactly like GitClient" ask |
| Which files are vendored | The net472 output without `*.pdb`, `Enigma.Msi.xml` and `.wixsharp/` | As GitClient: PDBs are git-ignored, the XML is API docs, WixSharp rewrites `.wixsharp/` | Everything |
| `.wixsharp/` | Git-ignored (`packaging/windows/Enigma.Msi.Worker/.wixsharp/`); WixSharp creates it on first use | Keeps `git status` clean after a build; `git clean -dxf` resets it | Committing it |
| Profile | Generated by the script into `artifacts/Enigma.Msi.X.Y.Z.msipkg.json`, with absolute paths and a new `productId` per build | Nothing to edit per release; the file stays as the build's record | A committed `msiProfiles/` profile per version (the old clone rule, now retired; the existing profiles stay as history) |
| Compression | `None` | "Exactly like GitClient". The user listed only name, upgrade code and version as what to adapt | `High`, the committed profiles' release setting |
| Version source | `<Version>` in `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`, checked `^\d+\.\d+\.\d+$` | That is where the app's version lives here ("its right version") | `Directory.Build.props` (it has none) |
| Control Panel / shortcut icon | `app.ico` from the publish folder | As the installed 1.7.0 and GitClient | `Assets\appicon.ico` from the source tree |
| Managed UI, scope | Enigma.Msi's default (`WixUI_InstallDir`); `PerMachine` | Same as the installed 1.7.0 | No UI; per user |
| The publish step | `buildmsi.ps1` calls `publish.ps1` | One definition of the publish | Repeating the `dotnet publish` line |
| `.bat` twin | `buildmsi.bat` runs `buildmsi.ps1` with `-ExecutionPolicy Bypass` | As `publish.bat` and GitClient | No `.bat` |
| Prerequisites | `git`, `dotnet`, `wix` and the vendored worker checked before anything is cleaned | Fails fast with the install command | Letting the worker fail after a full publish |
| Tests | A new Desktop-suite `WindowsInstallerIdentityTests` that pins, in `buildmsi.ps1`, the upgrade code, the product name "Enigma Msi", and the assembly name (from `typeof(AppInfo).Assembly`) | The identity facts a typo would break silently; GitClient's three equivalents | Testing every literal; a library-suite test (the script belongs to the app) |
| Verification | The whole script in a scratch clone of the dev's tree, then the MSI's tables read through the Windows Installer API. Nothing is installed | The run may not `git clean` the user's repository, and installing is outward-facing | Running it in the repository |
| Phases | Two: the vendored worker, then the script | Where a binary comes from and whether a script is right are separate reviews | One phase |

## PHASE01 — Vendor the Enigma.Msi worker CLI

**Branch:** `feature/feature-3e44-phase01-vendor-msi-worker`
**Status:** DONE

### Steps

1. Copy the `1.7.0` worker build (`src/Enigma.Msi.Worker/bin/Release/net472/` as built at `a7a1bfb`)
   into `packaging/windows/Enigma.Msi.Worker/`, without `*.pdb`, `Enigma.Msi.xml` and `.wixsharp/`.
2. `packaging/windows/Enigma.Msi.Worker/README.md`:
   - what it is, and why a repository that builds the worker also carries a copy of it;
   - its source (this repository, tag `1.7.0`, `a7a1bfb`, `WixSharp_wix4` 2.14.2);
   - its requirements (the WiX CLI, and the WiX 7 EULA);
   - the components' licences;
   - how to refresh it.
3. `.gitignore`: `packaging/windows/Enigma.Msi.Worker/.wixsharp/`.
4. Check that `git status --porcelain --ignored` lists no vendored file as ignored, and that the worker
   runs: with no arguments it prints its usage and exits 2.

### Acceptance criteria

- Every vendored file is identical to the 1.7.0 build output and to GitClient's copy (`cmp`; the
  `.exe.config` may differ only in line endings). `Enigma.Msi.dll` carries `1.0.0+a7a1bfb…`.
- No vendored file is git-ignored; `.wixsharp/` is.
- `Enigma.Msi.Worker.exe` with no arguments prints its usage and exits 2.
- The Release build has zero warnings. The whole suite shows no new failure against the run's baseline.

## PHASE02 — The buildmsi script

**Branch:** `feature/feature-3e44-phase02-buildmsi-script`
**Status:** DONE

### Steps

1. `packaging/windows/buildmsi.ps1` (ASCII, `Set-StrictMode`, `$ErrorActionPreference = 'Stop'`,
   comment-based help like `publish.ps1`), resolved from its own location. It is GitClient's script with
   these adaptations:
   1. Constants: `$UpgradeCode = '49589519-f722-4733-af70-81e027756344'`, `$ProductName = 'Enigma Msi'`,
      the manufacturer, `$Assembly = 'Enigma.Msi.Desktop'`.
   2. Check that `git`, `dotnet` and `wix` are on the `PATH` and that the vendored worker exists.
   3. Read `<Version>` from `src\Enigma.Msi.Desktop\Enigma.Msi.Desktop.csproj` and check it.
   4. Run `git -C <root> clean -dxf`; stop on a non-zero exit code.
   5. Run `publish.ps1`; stop on a non-zero exit code. Check that the exe and `app.ico` are in the publish
      folder.
   6. Write `artifacts/Enigma.Msi.X.Y.Z.msipkg.json` (UTF-8 without BOM, absolute paths, a new
      `productId`):
      - `PerMachine`, installed to `%ProgramFiles%\Enigma Msi`, `compression` `None`;
      - `controlPanel.productIcon` and both shortcuts' icons are the publish folder's `app.ico`;
      - shortcuts `%Desktop%` and `%ProgramMenu%`, named "Enigma Msi", targeting
        `[INSTALLDIR]\Enigma.Msi.Desktop.exe`.
   7. Run `Enigma.Msi.Worker.exe build <profile>` and exit with its code. On success, print the MSI's path.
2. `packaging/windows/buildmsi.bat` runs `buildmsi.ps1` through `powershell -NoProfile -ExecutionPolicy
   Bypass -File` and passes its exit code on.
3. Tests: `tests/Enigma.Msi.Desktop.UnitTests/WindowsInstallerIdentityTests.cs` finds the repository root
   by `Enigma.Msi.slnx` and pins `$UpgradeCode`, `$ProductName` and `$Assembly` in `buildmsi.ps1`.
4. Docs:
   - `README.md` *Building it from source*: the installer is built by `buildmsi`.
   - `docs/RELEASE.md`:
     - §1: drop the committed-profile check;
     - §2: becomes `buildmsi` (its `git clean`, its output, its identity);
     - *The profile clone rule* becomes *The Windows installer's identity*;
     - §5: the MSI's new path, and the upgrade over the installed 1.7.0.
   - The csproj's `<Version>` comment, `publish.ps1` / `publish.bat` (which name `msiProfiles\`), and
     CLAUDE.md's project layout.
5. Verify in a scratch clone: commit the dev's tree there, run `buildmsi.bat`, then read the MSI's
   `Property`, `Directory`, `Shortcut`, `Upgrade`, `Icon` and `Media` tables. Also check that, with
   `wix` off the `PATH`, the script stops before `git clean`.

### Acceptance criteria

- In the scratch clone, `buildmsi.bat` exits 0 and leaves `artifacts/Enigma.Msi.<version>.msi` and its
  `.msipkg.json`. After it, `git status --porcelain` is empty.
- The MSI's tables show:
  - `ProductName` "Enigma Msi", `Manufacturer` "Josué Clément", and `ProductVersion` equal to the csproj
    version;
  - `UpgradeCode` `{49589519-F722-4733-AF70-81E027756344}`, `ALLUSERS` 1, and `ARPPRODUCTICON` set;
  - `INSTALLDIR` "Enigma Msi" under `ProgramFilesFolder`;
  - two "Enigma Msi" shortcuts (`DesktopFolder`, `ProgramMenuFolder`) on
    `[INSTALLDIR]\Enigma.Msi.Desktop.exe`, each with an icon.
- The profile says `"compression": "None"`.
- A missing `wix` stops the script before `git clean`, with the install command in the message.
- The Release build has zero warnings. The whole suite, with the new tests, shows no new failure.

## Out of scope

- Installing, launching or uninstalling the MSI (outward-facing; the user's step).
- Signing the MSI; a 64-bit package (Enigma.Msi has no platform setting).
- Changing the library or the in-tree worker.
- Renaming the app inside the app (the About dialog and the Linux launcher still say "Enigma.Msi").
- Deleting `msiProfiles/` (kept as the record of 1.0.0 to 1.7.0).
- A `-NoClean` switch or other options the user did not ask for.
