# FEATURE-1AC4 — Frame-filling icon set (final3)

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-1ac4-final3-icon-set`
**Run:** feature/2026-11-04-final3-icons-release

## Objective

Replace the application's artwork with the new set in `C:\Dev\EnigmaLibs\Branding\Apps\final3\msi`
(`enigma-msi.ico`, `enigma-msi.svg`, `png/enigma-msi-{16,20,24,32,40,48,64,96,128,256}.png`), wherever
the app shows an icon: the executable, the window and the taskbar, the `app.ico` beside the executable
(and through it the installer's shortcuts and *Installed apps* entry), the About dialog and the Linux
installer.

## Context & constraints

- **The new artwork** is `FEATURE-62E1`'s drawing, the taped cube in the *Soft* palette on a transparent
  background, **scaled by 1.2727 and centred** so that its longest side spans 16 to 240 of the 256-unit
  view box. Before, it spanned about 40 to 216, so the cube now fills the frame. The small frames were
  drawn on the pixel grid (Branding `FEATURE-BF15`). The SVG's header says it is regenerated with
  `python3 Branding/Apps/export.py final3`.
- **The new `.ico`** has the same shape as today's: ten 32-bit RGBA PNG frames (16, 20, 24, 32, 40, 48,
  64, 96, 128, 256). Each frame is byte for byte the matching `png/enigma-msi-N.png`, which was checked
  while planning.
- **Where the artwork lives** is unchanged since `FEATURE-62E1` and was re-checked while planning. The
  file names were kept on purpose:
  - `src/Enigma.Msi.Desktop/Assets/appicon.ico` is used as:
    - the `ApplicationIcon`;
    - the main window's `Icon`;
    - an `AvaloniaResource`;
    - the `app.ico` copied beside the executable. `buildmsi.ps1` uses that copy as the MSI's
      `productIcon` and its shortcuts' `iconPath`.
  - `src/Enigma.Msi.Desktop/Assets/logo.png` is the About card's image, the `.ico`'s 256 px frame.
  - `packaging/linux/icons/enigma-msi-{16,24,32,48,64,96,128,256}.png` are what `install.sh` installs
    (`ICON_SIZES`, eight sizes: no 20 or 40).
- `AppIconTests` (Desktop suite) checks that `app.ico` is beside the test assembly and identical to the
  embedded `appicon.ico`. It stays valid unchanged.
- The committed `msiProfiles/` name `Assets/appicon.ico` as history and are not touched.
- The source directory is outside the repository and is left in place.

## Steps

1. Check that the current assets are `final2`'s, so the swap has a known starting point.
2. Copy, byte for byte:
   - `enigma-msi.ico` → `src/Enigma.Msi.Desktop/Assets/appicon.ico`;
   - `png/enigma-msi-256.png` → `src/Enigma.Msi.Desktop/Assets/logo.png`;
   - `png/enigma-msi-{16,24,32,48,64,96,128,256}.png` → `packaging/linux/icons/`.
3. Check that no prose or comment describes the icon's old margins or size (README, guides, XAML
   comments, the Linux scripts). Fix any that does.
4. Release build and the whole suite. A script check confirms the frame layout.

## Acceptance criteria

- The three destinations hold the new files byte for byte (`cmp`), and no `final2` file is left.
- A script check shows ten 32-bit RGBA PNG frames (16 to 256) in `appicon.ico`. `logo.png` is its 256
  frame byte for byte, and each Linux PNG is the `.ico` frame of its size byte for byte.
- The `app.ico` in the build output is the new icon (`cmp`).
- The Release build has zero warnings, and the whole suite shows no new failure against the run's
  baseline (`AppIconTests` included).

## Out of scope

- The application's accent colours and any UI change, including the About image's display size.
- Committing the SVG master (it stays in `Branding/Apps`).
- Release notes and the version (`FEATURE-F30B`).
- Changing which sizes the Linux installer installs.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which files | `appicon.ico`, `logo.png` and the eight Linux PNGs, overwritten in place from the delivered set | They are every place that shows the icon. "Installers" are covered through `app.ico`, which `buildmsi.ps1` hands to the MSI, and through the Linux PNGs. The kept names mean no reference changes | Renaming to the drop's `enigma-msi.*` names, which would touch the csproj, the XAML, the tests and `buildmsi.ps1` for nothing; re-packing the `.ico` |
| The SVG | Not added | Nothing here reads it. CLAUDE.md records that the master lives in `Branding/Apps` | `packaging/icon/enigma-msi.svg`, a new artifact that nothing reads |
| Tests | No test change; the frame layout is checked by script | `AppIconTests` already pins `app.ico` to the embedded asset. The artwork's pixels are not a behaviour | A test pinning frame sizes or file hashes, which would break on every artwork drop |
| About image size | Unchanged | The PNG is still 256 px. The cube filling more of it is the intended effect of `final3` | Shrinking the `Image` to keep the old visual size |
| The source directory | Left in place | It is the Branding repository's output, outside this repository | Deleting it |
| Breakdown | One dev | A pure asset swap | Splitting the Windows assets from the Linux ones |
