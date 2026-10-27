# FEATURE-1399 — Default cabinet compression to `None`

**Status:** DONE
**Branch:** `feature/feature-1399-compression-none-default`
**Plan:** `docs/plan/FEATURE-1399.md`
**Run:** vibe/2026-10-22-wixsharp-dir-compression

## Summary

The default cabinet compression is now `CompressionLevel.None` instead of `High`. That is the fastest MSI
build: on this machine the desktop app's own installer built in 5 s at `None` (49.5 MB), against a 17.25 MB
MSI at `High`.

The new default applies everywhere a package starts fresh:
- `new MsiPackage()` in the library;
- the desktop form's field initializer and `Reset()`, which covers startup, *New* and the quick start.

Saved profiles are unaffected. `MsiPackageJson` always writes the non-nullable `compression`, so every
profile states its level. Only JSON that omits the member now reads as `None`.

## Files/modules touched

### Modified

| File | Change |
|---|---|
| `src/Enigma.Msi/Model/MsiPackage.cs` | `Compression` defaults to `CompressionLevel.None`; its XML doc says so and when to choose `High`. |
| `src/Enigma.Msi/Model/CompressionLevel.cs` | "The library's default." moved from `High` to `None`. |
| `src/Enigma.Msi.Desktop/ViewModels/PackageEditorViewModel.cs` | `_compression` initializer and `Reset()` → `CompressionLevel.None`. |
| `tests/Enigma.Msi.UnitTests/Model/MsiPackageDefaultsTests.cs` | Default pin → `None`; the class summary no longer claims every default is a family-profile choice. |
| `tests/Enigma.Msi.UnitTests/Serialization/MsiPackageJsonTests.cs` | Two default pins → `None`. New test: `Deserialize_AProfileStatingHighCompression_KeepsIt`. |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/PackageEditorViewModelTests.cs` | Quick-start pin → `None`. New *compression default* section with three tests: a new form and a `Reset()` both match `new MsiPackage().Compression`, and `LoadFrom` keeps a stated `High` through `ToPackage()`. |
| `docs/guides/model.md` | Members table and enums table name `None` as the default. Both JSON samples (written from packages that leave `Compression` unset) now show `"None"`. The "per-user, uncompressed package" sample became "per-user, fully compressed" and opts into `High`. |
| `RELEASENOTES.md` | `(unreleased)` section: a *Changed* block covering the new default and profile compatibility. |
| `CLAUDE.md` | The run's build-state entry now covers `FEATURE-1399`. |
| `docs/roadmap.md`, `docs/plan/FEATURE-1399.md` | Status → `DONE` (whole-table reformat). |

### Created

| File | Content |
|---|---|
| `docs/done/FEATURE-1399.md` | This record. |

## Acceptance criteria

- [x] `new MsiPackage().Compression == CompressionLevel.None` (`MsiPackageDefaultsTests`), and
      `MsiPackageJson.Deserialize("{}")` yields `None` (`Deserialize_EmptyObject_LeavesModelDefaults`).
- [x] The desktop form starts, resets and applies a quick start at `None`. Tests:
      `NewForm_StartsAtTheModelsCompressionDefault`, `Reset_ReturnsCompressionToTheModelsDefault`,
      `ApplyQuickStart_ReplacesTheWholePackage_WithFreshIdentifiers`.
- [x] A profile stating `"compression": "High"` opens and round-trips as `High`. Tests:
      `Deserialize_AProfileStatingHighCompression_KeepsIt`, `LoadFrom_KeepsTheProfilesStatedCompression`.
- [x] All four `High`-as-default pins were moved to `None`, none deleted. The new tests pass, the whole
      suite passes (471/471), and the build is warning-free.
- [x] No doc still calls `High` the default. `grep` over `README.md`, `CLAUDE.md`, `docs/guides`,
      `docs/RELEASE.md` finds only the new wording. `RELEASENOTES.md`'s 1.0.0–1.4.0 sections are history
      and left as written.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| The defaults test's summary ("picks the choices the family's profiles use") | Amended to say compression is the fast-build default and that a release profile states its own level | The diff made the old sentence untrue (the committed profiles state `High`), and the summary is what explains the pin | Leaving a comment that contradicts the assertion under it |
| A JSON test for an explicit `High` | Added | It is the compatibility promise the release notes make: a stated level is never replaced by the new default. The existing `Low`/`MsZip` cases prove the mechanism, not that promise | Relying on the `Low`/`MsZip` cases |
| The guide's sample section | Flipped to show opting into `High` | Its premise (`None` as the opt-in) no longer holds; showing the opposite trade-off keeps the section useful | Deleting it |

## Deviations & follow-ups

- **No deviation from the plan.**
- **The dogfood profiles keep `"compression": "High"`.** `docs/RELEASE.md`'s clone rule copies
  `compression` forward, so the next app release's MSI stays at 17 MB-class size and keeps the slow build.
  Switching the release profile is the maintainer's call at release time.
- **The modified guide snippet** (`Compression = CompressionLevel.High` in an object initializer) was not
  compiled separately. It has the same shape as the snippet it replaces, and every property and member it
  uses is exercised by the suite.
- **CRLF:** no line-ending inconsistency observed in the touched files.

### Documentation sweep (applied)

| File | Edit |
|---|---|
| `tests/…/MsiPackageDefaultsTests.cs` (comment) | See *Decisions taken at build time*. |
| `README.md`, `docs/guides/desktop-app.md`, `docs/guides/validation.md`, `docs/RELEASE.md` | Checked. None states the compression default; nothing to change. |

The `model.md`, `RELEASENOTES.md` and `CLAUDE.md` edits are planned scope (plan step 5).

## Build/test evidence

**Starting point:** the run-branch tip after `BUG-7325` (`e4268cd`), 466/466 green and 0 warnings.

**Build:** `dotnet build Enigma.Msi.slnx -c Release` → `0 Warning(s) 0 Error(s)`.

**Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release`, first run, no fix cycle needed:

```
total: 471   failed: 0   succeeded: 471   skipped: 0
  Enigma.Msi.Worker.UnitTests    (net48|x64)    passed
  Enigma.Msi.Desktop.UnitTests   (net10.0|x64)  passed   <- +3: compression default
  Enigma.Msi.UnitTests           (net8.0|x64)   passed   <- +1: explicit High kept
  Enigma.Msi.UnitTests           (net10.0|x64)  passed   <- +1: explicit High kept
```

**End to end at `None`:** `BUG-7325`'s Windows check already built the desktop app's MSI from a
`compression: None` profile through the real worker and WiX 7.0.0 (exit 0, 5 s, 49.5 MB). `WixEnumMap`
maps `None` → WixSharp `none` unchanged, and its drift guards pass.
