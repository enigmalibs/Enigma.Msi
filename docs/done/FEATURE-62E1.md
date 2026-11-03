# FEATURE-62E1 — New icon set without the tile

## Summary

The app's artwork is now the new set from `C:\Dev\EnigmaLibs\Branding\Apps\final2\msi`. It is the same
taped cube as `FEATURE-25EF`'s, without the dark rounded tile, so it sits on a transparent background.
Every file was overwritten in place under its existing name, so no reference changed. The csproj's
`ApplicationIcon`, the `app.ico` copy beside the exe, the window's `Icon`, the About card's `logo.png` and
the Linux installer's `ICON_SIZES` all read the same paths as before.

- `Assets/appicon.ico` ← `enigma-msi.ico`: ten 32-bit RGBA PNG frames, 16 to 256 px.
- `Assets/logo.png` ← `png/enigma-msi-256.png`: the `.ico`'s 256 frame byte for byte.
- `packaging/linux/icons/enigma-msi-{16,24,32,48,64,96,128,256}.png` ← the matching PNGs.

## Files/modules touched

- Modified:
  - `src/Enigma.Msi.Desktop/Assets/appicon.ico`
  - `src/Enigma.Msi.Desktop/Assets/logo.png`
  - `packaging/linux/icons/enigma-msi-{16,24,32,48,64,96,128,256}.png`
  - `docs/roadmap.md`, `docs/plan/FEATURE-62E1.md`
- Created: `docs/done/FEATURE-62E1.md`

## Deviations & follow-ups

- None from the plan.
- The SVG master (`enigma-msi.svg`) is not committed. It is not used here, and the master stays in
  `Branding/Apps`, which `export.py` regenerates.
- No prose described the old tile, so the documentation needed no edit. The scan covered the README,
  guides, XAML comments and the Linux scripts.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Copy source | The drop's files, unchanged | They are `export.py`'s output; each `.ico` frame was checked to be its PNG byte for byte |

## Build/test evidence

**Run baseline** (taken once, on `feature/2026-11-03-icons-msi-release` at `a7ab894`, the planning
commit on top of `develop` @ `35e3456`):

- `dotnet build Enigma.Msi.slnx -c Release --no-incremental` gave **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.Msi.slnx -c Release --no-build` gave **417 / 417 passed**: the net472
  worker suite with its drift guards, the library suite and the desktop suite.

The baseline has no pre-existing warning or failure.

This dev:

- `cmp` against the drop: all ten destination files are identical.
- Script check (`struct` parse of the `.ico`): ten PNG frames (16, 20, 24, 32, 40, 48, 64, 96, 128, 256),
  each 8-bit RGBA (colour type 6). The 256 frame equals `logo.png`. Each of the eight Linux PNGs equals the
  frame of its size. 20 and 40 are not installed on Linux, as before.
- `dotnet build Enigma.Msi.slnx -c Release --no-incremental` gave **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.Msi.slnx -c Release --no-build` gave **417 / 417 passed**.
  `AppIconTests` passed, and the `app.ico` in the test output is the new icon (`cmp`).
