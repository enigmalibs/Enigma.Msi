# FEATURE-1AC4 — Frame-filling icon set (final3)

## Summary

The app's artwork is now the set from `C:\Dev\EnigmaLibs\Branding\Apps\final3\msi`. It is
`FEATURE-62E1`'s taped cube on a transparent background, scaled by 1.2727 so that it fills the frame
(16 to 240 of 256 instead of about 40 to 216), with the small frames drawn on the pixel grid. Every file
was overwritten in place under its existing name, so no reference changed. These all read the same paths
as before:
- the csproj's `ApplicationIcon`;
- the window's `Icon`;
- the `app.ico` beside the exe, which `buildmsi.ps1` hands to the MSI for its shortcuts and ARP icon;
- the About card's `logo.png`;
- the Linux installer's `ICON_SIZES`.

The files:
- `Assets/appicon.ico` ← `enigma-msi.ico`: ten 32-bit RGBA PNG frames, 16 to 256 px.
- `Assets/logo.png` ← `png/enigma-msi-256.png`: the `.ico`'s 256 frame byte for byte.
- `packaging/linux/icons/enigma-msi-{16,24,32,48,64,96,128,256}.png` ← the matching PNGs.

## Files/modules touched

- Modified:
  - `src/Enigma.Msi.Desktop/Assets/appicon.ico`
  - `src/Enigma.Msi.Desktop/Assets/logo.png`
  - `packaging/linux/icons/enigma-msi-{16,24,32,48,64,96,128,256}.png`
  - `docs/roadmap.md`, `docs/plan/FEATURE-1AC4.md`
- Created: `docs/done/FEATURE-1AC4.md`

## Deviations & follow-ups

- None from the plan.
- **Documentation sweep:** nothing was stale, so no doc was edited.
  - The only prose about the artwork is 1.8.0's record (CLAUDE.md's 1.8.0 paragraph, `RELEASENOTES.md`'s
    1.8.0 section), and it is still true of 1.8.0.
  - The README's 1.8.0 callout is replaced by the release dev (`FEATURE-F30B`).
  - The About card's comment names the 256 px PNG, which is unchanged.
- The About image still renders at 96×96. The cube now fills more of it, which is what `final3` was made for.
- Owed on Linux: an `install.sh` run to look at the new icons in a launcher. It was not run here.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Copy source | The drop's files, unchanged | They are `export.py final3`'s output, and each `.ico` frame was checked to be its PNG byte for byte |
| Starting point | Checked first that the ten in-repo files were `final2`'s byte for byte | So that "no old artwork left" has a known meaning |

## Build/test evidence

**Run baseline**, taken once on `feature/2026-11-04-final3-icons-release` at `ed942d2` (the planning
commit on top of `develop` @ `b186651`):
- `dotnet build Enigma.Msi.slnx -c Release --no-incremental` gave **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.Msi.slnx -c Release --no-build` gave **421 / 421 passed**: the net472
  worker suite with its drift guards, the library suite and the desktop suite.

The baseline has no pre-existing warning or failure.

This dev:
- Before the copy, `cmp` showed that all ten destination files were `final2`'s. After it, all ten match
  the `final3` drop.
- A script check (`struct` parse of the `.ico`) found ten PNG frames (16, 20, 24, 32, 40, 48, 64, 96,
  128, 256), each 8-bit RGBA (colour type 6) and each byte for byte its `final3` PNG.
  - `logo.png` equals the 256 frame.
  - Each of the eight Linux PNGs equals the frame of its size. 20 and 40 are not installed on Linux, as
    before.
- `dotnet build Enigma.Msi.slnx -c Release --no-incremental` gave **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.Msi.slnx -c Release --no-build` gave **421 / 421 passed**, including
  `AppIconTests`.
- `cmp` confirms that the `app.ico` in the desktop build output and in the desktop test output is the new
  icon.
