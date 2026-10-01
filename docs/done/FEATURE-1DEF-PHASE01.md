# FEATURE-1DEF — PHASE01 — Quick start: derived MSI file name

**Status:** DONE
**Branch:** `feature/feature-1def-phase01-quickstart-msi-filename`
**Plan:** `docs/plan/FEATURE-1DEF.md` (PHASE01)
**Run:** feature/2026-10-01-quickstart-msi-name-release

## Summary

The quick start now shows the **MSI file name**, directly under **Version** — the value that lands in
*Install and output → MSI file name* on Apply. It is **derived live** from the application name and the
version as they are typed: characters a file name cannot hold are dropped, every run of whitespace becomes
one `.`, and `.` + the version is appended, so `Enigma Msi` at `1.4.0` shows **`Enigma.Msi.1.4.0`** with
no other action. The field is an ordinary editable answer: a name typed by hand is never overwritten by a
later name or version edit, and emptying the field hands it back to the derivation from the next edit.
Apply gates on the validator's two string rules for `output.msiFilename` (no invalid file-name characters,
no `.msi` extension) and the dialog says which one a typed name breaks.

1.3.0 derived the name silently, from the application name alone, at Apply — with a `package` fallback
the user never saw. That derivation and its fallback are gone: `ApplyQuickStart` takes the dialog's answer
verbatim.

## Files/modules touched

### Modified

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/ViewModels/QuickStartSettings.cs` | Gains `MsiFilename` after `Version` (eight parameters, mirroring the field order); `AppName`'s doc no longer claims to be the MSI name |
| `src/Enigma.Msi.Desktop/ViewModels/QuickStartViewModel.cs` | The eighth `[ObservableProperty]` `MsiFilename`; `OnAppNameChanged`/`OnVersionChanged` partial hooks driving `FollowDerivedMsiFilename`; the `ToMsiFilename`/`ToFilenameSegment` derivation (moved from the editor and extended); `MsiFilenameError`/`HasMsiFilenameError` with two public message constants; `CanApply` and `ToSettings()` extended; class remarks |
| `src/Enigma.Msi.Desktop/Views/QuickStartCard.axaml` | The `MSI file name` editor (`Unit=".msi"`) with a permanent hint and the warning line, between Version and Manufacturer; the intro sentence counts eight and stops listing the MSI name as derived |
| `src/Enigma.Msi.Desktop/ViewModels/PackageEditorViewModel.cs` | `ApplyQuickStart` sets `MsiFilename = settings.MsiFilename`; **deleted** `ToMsiFilename`, `FallbackMsiFilename`, `MsiExtension` and the two `using`s they alone needed; `<remarks>` corrected |
| `src/Enigma.Msi.Desktop/Views/MainWindow.axaml` | Toolbar tooltip: "Fill the form from eight answers" |
| `src/Enigma.Msi.Desktop/Views/QuickStartCard.axaml.cs`, `Services/IQuickStartDialogService.cs`, `Services/QuickStartDialogService.cs`, `ViewModels/MainWindowViewModel.cs` | "seven" → "eight" in XML docs and comments |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/QuickStartViewModelTests.cs` | 14 new cases (below); the seven-answer cases now cover eight |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/PackageEditorViewModelTests.cs` | The four derivation tests (`NamesTheMsiAfterTheApplication`, `SanitizesTheMsiFileName`, `StripsATrailingMsiExtensionFromTheFileName`, `FallsBackToAFileNameWhenNothingSurvivesSanitization`) replaced by one that asserts the answer is taken verbatim; the derivation they covered now lives — and is tested — in the dialog's ViewModel |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/MainWindowViewModelTests.cs` | Both settings helpers carry the new argument; the stale "six answers" comment corrected |
| `docs/guides/desktop-app.md`, `README.md`, `CLAUDE.md` | Documentation sweep (below) |
| `docs/roadmap.md`, `docs/plan/FEATURE-1DEF.md` | PHASE01 → IN PROGRESS → DONE; the item row → IN PROGRESS |

### Created

| Path | What |
|---|---|
| `docs/done/FEATURE-1DEF-PHASE01.md` | This file |

No file deleted.

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| Eight fields, MSI file name third, with the `.msi` unit and a permanent hint | **Met** — name, version, **MSI file name**, manufacturer, release folder, output folder, icon, main executable |
| `Enigma Msi` + `1.4.0` shows `Enigma.Msi.1.4.0`; edits update it live | **Met** — `MsiFilename_TurnsSpacesIntoDotsAndAppendsTheVersion`, `MsiFilename_FollowsEveryEditOfTheNameAndTheVersion`, and `MsiFilename_RaisesChangeNotificationWhenDerived` (the notification is what makes the bound editor repaint) |
| A hand-typed value is never overwritten; emptying it makes it follow again | **Met** — `MsiFilename_TypedByHand_IsNeverOverwrittenByLaterEdits`, `MsiFilename_EmptiedByHand_FollowsAgainFromTheNextEdit` |
| Apply disabled while blank, invalid or `.msi`-suffixed, and the dialog says which | **Met** — the blank row in `CanApply_IsFalseWhileAnySingleAnswerIsBlank`, `MsiFilenameError_RejectsAnInvalidCharacter`, `MsiFilenameError_RejectsTheMsiExtension` |
| Applying takes the dialog's name verbatim; `FallbackMsiFilename` and the editor's `ToMsiFilename` gone | **Met** — `ApplyQuickStart_TakesTheMsiFileNameAsAnswered_WithoutDerivingItFromTheApplicationName`; `grep` finds `FallbackMsiFilename` nowhere under `src/` or `tests/`, and `ToMsiFilename` only as `QuickStartViewModel`'s own private helper — none in `PackageEditorViewModel` |
| A quick-start package still passes Validate with zero problems | **Met** — `ApplyQuickStart_ProducesAPackageThatPassesTheInMemoryRules` and the end-to-end `QuickStart_Applied_YieldsAPackageThatValidatesCleanlyAndEnablesBuild` (real folders, environment rules included) both green |
| Warning-free build; desktop suite fully green; library failures = the Windows-only baseline | **Met** — see *Build/test evidence* |

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How "edited by hand" is detected | Compare the field with the **last derived value** (kept in `_derivedMsiFilename`) at the moment the name or version changes; blank counts as "not edited" | Needs no flag that could drift from what the user actually did, and makes "empty it to hand it back" fall out of the same rule | A `_msiFilenameEdited` flag set from the property's setter (the setter cannot tell a programmatic derivation from a keystroke without extra plumbing) |
| Emptying the field: re-derive immediately? | No — it stays empty until the next name/version edit | Refilling the box the user has just cleared, under their cursor, would fight the edit; the plan's wording ("follows again on the next name/version edit") | Re-deriving on clear |
| MVVM style for the new property | `[ObservableProperty]` + `[NotifyPropertyChangedFor]` + the generator's `partial void On…Changed` hooks | The app already uses the generators in 56 places; the house rule's own escape clause says stay consistent and flag the divergence — flagged in the plan, not half-migrated here | Hand-written `field`-keyword properties for one field in a generator-styled class |
| Shape of the segment helper | One pass with a `StringBuilder` and a pending-separator flag | Reads like the helper it replaces, allocates once, and handles `Contoso / Widget` → `Contoso.Widget` (a run of whitespace around a dropped character still yields one dot) | `Split` + per-word filtering + `Join` (three allocations, and harder to read) |
| Whether the version is sanitized too | Yes, by the same segment rule | A version typed with a stray space or separator must not produce an invalid derived name; a derived name is meant never to trigger the dialog's own warning | Appending the raw version |
| Where the derivation's tests live | In `QuickStartViewModelTests`, with the four editor-side derivation tests removed | The derivation moved; testing it where it no longer exists would test nothing | Keeping the editor tests by routing them through the dialog's ViewModel |
| Card-level headless test | None | The card change is markup over bindings; compiled bindings fail the build on a wrong path, and every behaviour sits in the ViewModel (as planned) | A headless `QuickStartCard` test |

## Deviations & follow-ups

- **No deviation from the plan's steps.** One addition: the version is put through the same sanitization as
  the name (see *Decisions*), which the plan implied ("the version treated the same way") rather than spelt
  out.
- **The new derivation changes what a quick start names a package**, deliberately: 1.3.0 would have
  produced `Widget`, 1.4.0 produces `Widget.2.1.0`. Profiles already saved are untouched — `msiFilename` is
  a stored field — so nothing changes for anyone who does not run the quick start again.
- **Platform note, unchanged from before:** "characters a file name cannot hold" is
  `Path.GetInvalidFileNameChars()` of the machine running the app — on Windows the full set, on Linux only
  `/` and `\0`. That is the same call the validator makes, so the dialog and the Problems pane always agree
  on any one machine; the app itself only builds MSIs on Windows.
- **Edge case left alone:** an application name such as `Widget .msi` with no version derives `Widget.` (the
  space before `.msi` becomes a separator, then the extension is stripped). Valid by the validator's rules
  and absurd as input; not worth a rule of its own.
- **Follow-up (unchanged, not this dev's):** the house MVVM rule (no source generators) and this app
  disagree; migrating would be its own work item.
- **Line endings:** nothing to report — every touched file is LF-only (checked with `grep -l $'\r'` over the
  diff's file list: no hit).

### Documentation sweep (applied)

| File | Edit |
|---|---|
| `docs/guides/desktop-app.md` | Quick start: the command table and the section count **eight**; the field table gains an *MSI file name* row under *Version* (the derivation rule, the example, the follow/detach behaviour, the warning); the derived table loses its *MSI file name* row and the `package` fallback; *Application name* no longer claims to be the MSI name; the Apply-gate paragraph names the MSI rule |
| `README.md` | The *Quick start* bullet lists eight answers, the MSI file name among them, with the derivation example |
| `CLAUDE.md` | Build-state block: `FEATURE-1DEF` recorded as under way, with PHASE01's outcome |

## Build/test evidence

**Baseline** on the starting commit (`585710a`, before this run), on this Linux host: build 0 warnings;
**374 tests, 342 passed, 32 failed** — the documented Windows-only cluster, below; desktop suite **142/142**.

**Build** — `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)` (count
read, including the XAML compiler's pass over `QuickStartCard.axaml`).

**Tests** — `dotnet test --solution Enigma.Msi.slnx -c Release`, first run, no fix cycle needed:

```
total: 385   failed: 32   succeeded: 353   skipped: 0
```

| Suite | TFM | Result |
|---|---|---|
| `Enigma.Msi.Desktop.UnitTests` | net10.0 | **153/153** (142 + 14 new − 3 net from the four removed editor tests and the one added) |
| `Enigma.Msi.UnitTests` | net8.0 + net10.0 | 200/232 — the 32 failures are **exactly the baseline set** and nothing else |
| `Enigma.Msi.Worker.UnitTests` | net472 | not started — needs `mono`, absent here |

The 32 library failures, identical to the baseline (16 tests × 2 TFMs), all host-bound: 10
`MsiBuildServiceTests` (spawn the stub worker as an `.exe`), 5 `MsiPrerequisiteTests` (need the `wix` CLI and a
real worker on disk), and `MsiPackageValidatorTests.MsiFilename_MustNotBeAPath` (Windows path semantics).
This dev touches no library code. On Windows the solution total becomes **464** (453 + 11).

New tests (all in `QuickStartViewModelTests`): `MsiFilename_TurnsSpacesIntoDotsAndAppendsTheVersion`,
`…_IsDerivedAsTheNameIsTyped_AgainstTheDefaultVersion`, `…_FollowsEveryEditOfTheNameAndTheVersion`,
`…_IsBlankWhileTheApplicationNameIs`, `…_CollapsesRunsOfWhitespaceAndDropsTheEnds`,
`…_DropsCharactersAFileNameCannotHold`, `…_StripsATrailingMsiExtensionLeftByAMissingVersion`,
`…_TypedByHand_IsNeverOverwrittenByLaterEdits`, `…_EmptiedByHand_FollowsAgainFromTheNextEdit`,
`…_RaisesChangeNotificationWhenDerived`, `MsiFilenameError_RejectsAnInvalidCharacter`,
`…_RejectsTheMsiExtension`, `…_SaysNothingWhileTheFieldIsBlankOrValid`,
`ToSettings_CarriesTheDerivedMsiFileName`. In `PackageEditorViewModelTests`:
`ApplyQuickStart_TakesTheMsiFileNameAsAnswered_WithoutDerivingItFromTheApplicationName`.
