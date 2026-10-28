# FEATURE-25EF — PHASE01 — Replace the app icon and logo

**Status:** DONE
**Branch:** `feature/feature-25ef-phase01-new-icons`
**Plan:** `docs/plan/FEATURE-25EF.md` (PHASE01)
**Run:** feature/2026-10-28-new-icons-release

## Summary

The desktop app now uses its new artwork. The maintainer dropped it into the untracked
`new-icons-remove-when-done/`:

- **`Assets/appicon.ico`** is now the new `enigma-msi.ico`. It has 10 frames, all PNG-compressed: 16, 20,
  24, 32, 40, 48, 64, 96, 128 and 256. The old icon had 7 frames, at 16, 24, 32, 48, 64, 128 and 256.
  This file is the exe/dll Win32 icon (`<ApplicationIcon>`) and the main window's icon. It is also the
  installer's Control Panel and shortcut icon, through the dogfood profile's unchanged path.
- **`Assets/logo.png`** is now the new `enigma-msi-256.png` (256×256 RGBA). The splash and About show it.

Both files keep their names, so no reference changed: the csproj, `MainWindow.axaml`, `AppInfo.LogoUri`,
`SplashWindow.axaml`, `AboutCard.axaml`, the tests and all six committed profiles. The temporary folder is
deleted. It was never tracked, so the deletion leaves no trace in git.

## Files/modules touched

### Modified

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/Assets/appicon.ico` | Replaced with `enigma-msi.ico`. Byte-identical: sha256 `d6969692…5e458c`. 41,423 → 24,018 bytes |
| `src/Enigma.Msi.Desktop/Assets/logo.png` | Replaced with `enigma-msi-256.png`. Byte-identical: sha256 `24b60603…00e3c4`. 4,491 → 8,421 bytes |
| `docs/roadmap.md`, `docs/plan/FEATURE-25EF.md` | PHASE01 TODO → IN PROGRESS → **DONE**. The item is IN PROGRESS |

### Created

| Path | What |
|---|---|
| `docs/done/FEATURE-25EF-PHASE01.md` | This file |

### Deleted (untracked, never committed)

- `new-icons-remove-when-done/`. It held the `.ico`, ten PNG frames and the SVG master.

**No source code and no test changed.**

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| Both assets byte-identical to their source, and nothing else under `Assets/` changes | **Met.** `cmp` passed on both before the folder was deleted, and the sha256 values match. `Assets/` still holds exactly these two files |
| The temp folder is gone, and nothing else from it is committed | **Met** |
| No reference changes | **Met.** The diff touches the two binaries and the workflow docs only |
| 0 warnings; desktop suite green; library failures equal the baseline | **Met.** See *Build/test evidence* |
| The built dll's icon group holds the 10 new frames | **Met** |

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How to prove the new `.ico` made it into the binary | Parse the built dll's `.rsrc` section (scratchpad Python, not committed). Then check that the `.ico` and the PNG appear whole in the dll as Avalonia resources | It checks what ships, not just what is on disk. It also proves Roslyn accepted an all-PNG `.ico` as the Win32 icon, which the old, mostly-BMP icon never exercised below 128 px | Trusting the incremental build |

## Deviations & follow-ups

- **No deviation from the plan.**
- **Not verifiable on this host:**
  - How Windows renders the icon in the taskbar, Explorer, the apphost and the installed shortcuts. The
    all-PNG frames need Windows Vista or later, which every supported Windows has.
  - The MSI's `Icon` table.
  
  Both belong to PHASE02's Windows hand-over and `docs/RELEASE.md` §5.
- **The other nine PNG frames and the SVG were not committed**, per the plan. The SVG names its master
  `Branding/Apps/export.py`, outside this repository; that is where the artwork is regenerated.
- **Line endings:** nothing to report. The two changed assets are binary, and `.gitattributes`'
  `text=auto` leaves them alone.

### Documentation sweep (applied)

Nothing to edit. The README, `docs/guides/desktop-app.md` and `CLAUDE.md` describe the splash and About as
showing "the logo" and `Assets/logo.png`, which stays true. No doc describes what the artwork looks like.
PHASE02 records the change in `RELEASENOTES.md` and in `CLAUDE.md`'s build-state block.

## Build/test evidence

**Baseline** (run branch @ `2f7daa0`, before any change): 0 warnings. The total was 390: 32 failed, 358
succeeded. The failures are 16 distinct library tests × 2 TFMs: `MsiBuildServiceTests` ×10,
`MsiPrerequisiteTests` ×5 and `MsiPackageValidatorTests.MsiFilename_MustNotBeAPath`. That is the
documented Windows-only cluster.

**Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`.

**Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release`, first run, no fix cycle:

```
total: 390   failed: 32   succeeded: 358   skipped: 0
```

| Suite | TFM | Result |
|---|---|---|
| `Enigma.Msi.Desktop.UnitTests` | net10.0 | **156/156**, including `SplashWindowTests.TheLogo_IsAResourceThatResolves` over the new PNG |
| `Enigma.Msi.UnitTests` | net8.0 + net10.0 | 202/234. A `diff` of the failing list against the baseline is empty |
| `Enigma.Msi.Worker.UnitTests` | net472 | not started (no `mono` on this host) |

**The binary**, `src/Enigma.Msi.Desktop/bin/Release/net10.0/Enigma.Msi.Desktop.dll`:

```
RT_ICON entries: 10   (all PNG)
RT_GROUP_ICON 32512 frames: [16, 20, 24, 32, 40, 48, 64, 96, 128, 256]
appicon.ico embedded whole as an Avalonia resource: True
logo.png    embedded whole as an Avalonia resource: True
```
