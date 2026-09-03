# FEATURE-2D02-PHASE01 — Quick start: output folder field

**Status:** DONE
**Branch:** `feature/feature-2d02-phase01-quickstart-output-folder`

## Summary

The quick start now asks **seven** questions instead of six: **Output folder** sits immediately after
Release folder, and the form no longer derives where the `.msi` is written.

1.2.0 silently put the output folder at the release folder's *parent* (`ParentDirectoryOrSelf`), with a
fallback to the release folder itself when the release folder was a drive root. That was one of the two
places in the app where a path appeared without anyone having typed it, and the rule had to be learned
from the guide rather than seen. From here the user states it, like every other answer.

- `OutputPath` is a seventh `[ObservableProperty]` on `QuickStartViewModel`, blank by default and
  required: `CanApply` gains `&& !string.IsNullOrWhiteSpace(OutputPath)`, so Apply stays disabled until
  it is answered. `ToSettings()` carries it trimmed.
- Its **Browse…** button is deliberately **ungated**, unlike the icon and executable pickers: those open
  *at* the release folder and compare their answer against it, while the output folder depends on
  nothing. It opens a folder picker at whatever is already in the field.
- No new validation. An output folder **inside** the release folder is accepted with no warning and no
  block — the permanent hint under the field says what is typical ("the release folder's parent, so the
  installer lands beside the payload rather than inside it") and leaves the choice open. An output folder
  that does not exist is still reported exactly once, by the main form's Problems pane, because the
  quick start applies string and parse rules only and never touches the disk.
- `PackageEditorViewModel.ApplyQuickStart` assigns `OutputPath = settings.OutputPath` verbatim, and
  `ParentDirectoryOrSelf` is deleted — it had that single call site.

`QuickStartSettings` keeps its positional-record shape, with `OutputPath` inserted after `ReleasePath` so
the record mirrors the field order.

## Files/modules touched

**Modified — app**

- `src/Enigma.Msi.Desktop/ViewModels/QuickStartSettings.cs` — `OutputPath` parameter after `ReleasePath`,
  with its `<param>` doc; "six answers" → "seven".
- `src/Enigma.Msi.Desktop/ViewModels/QuickStartViewModel.cs` — the `_outputPath` observable property with
  `[NotifyPropertyChangedFor(nameof(CanApply))]`; `CanApply` and `ToSettings()` extended; the ungated
  `BrowseOutputPathAsync` `[RelayCommand]`; class `<remarks>` rewritten — the field order is still a
  dependency order, and the output folder is explicitly *not* derived any more.
- `src/Enigma.Msi.Desktop/Views/QuickStartCard.axaml` — the `Output folder` `TextEditor` between Release
  folder and Icon, wrapped with its `.hint` `TextBlock` in a `StackPanel` (as the executable field
  already is); intro sentence now "Answer these seven…" and no longer promises a derived output folder;
  `d:DesignHeight` 520 → 600; the release-folder comment corrected (it is no longer "first of three
  paths").
- `src/Enigma.Msi.Desktop/ViewModels/PackageEditorViewModel.cs` — `ApplyQuickStart` takes the entered
  output folder; `ParentDirectoryOrSelf` **deleted**; `<remarks>` corrected to stop listing the output
  folder among what it derives.

**Modified — prose the change made factually wrong** (one-word corrections, same feature):
`Views/QuickStartCard.axaml.cs`, `Views/MainWindow.axaml` (the Quick start tooltip),
`Services/IQuickStartDialogService.cs`, `Services/QuickStartDialogService.cs`,
`ViewModels/MainWindowViewModel.cs` — each said "six answers"/"six questions".

**Modified — tests**

- `tests/…/ViewModels/QuickStartViewModelTests.cs` — the complete-form helper answers seven;
  `CanApply_IsTrueOnceAllSevenAnswersAreThere` (renamed); a blank-`OutputPath` case added to
  `CanApply_IsFalseWhileAnySingleAnswerIsBlank`; `BrowseOutputPath_TakesThePickedFolderAndOpensAtWhatIsAlreadyThere`
  and its cancelled counterpart; `BrowseOutputPathCommand.CanExecute(null)` asserted **true** with no
  release folder (the ungated-picker rule); `ToSettings_CarriesTheEnteredOutputFolder` and the trim case;
  `CanApply_AcceptsAnOutputFolderInsideTheReleaseFolder`.
- `tests/…/ViewModels/PackageEditorViewModelTests.cs` — the three derivation tests
  (`PutsTheOutputFolderBesideTheReleaseFolder`, `IgnoresATrailingSeparatorOnTheReleaseFolder`,
  `FallsBackToTheReleaseFolderWhenItHasNoParent`) replaced by
  `TakesTheOutputFolderAsEntered_WithoutDerivingItFromTheReleaseFolder` (an output folder that is neither
  the release folder nor its parent) and `AcceptsAnOutputFolderInsideTheReleaseFolder`;
  `TakesTheFourEnteredFieldsVerbatim` → `TakesTheEnteredFieldsVerbatim`, now asserting `OutputPath`.
- `tests/…/ViewModels/MainWindowViewModelTests.cs` — both `QuickStartSettings` helpers gain an output
  folder; `CreateSettingsForARealFolder` supplies an **existing** one, so the end-to-end quick-start →
  Validate cases still see a package with zero problems.

**Modified — docs**

- `docs/guides/desktop-app.md` — the quick-start section describes seven fields: an **Output folder** row
  in the field table (ungated Browse, an inside-the-release-folder output accepted), the Output folder
  row **removed** from the derived table, "all seven are answered", and the command-bar row and typical
  workflow updated. A non-existent output folder is noted as the Problems pane's business.
- `CLAUDE.md` — a `FEATURE-2D02` paragraph in the build-state block (documentation freshness sweep,
  accepted by the maintainer): PHASE01's outcome, the ungated-picker rule, and what PHASE02–04 still
  hold. The `FEATURE-74C4` paragraph above it is left as the record of what 1.2.0 shipped.
- `docs/roadmap.md`, `docs/plan/FEATURE-2D02.md` — statuses.

**Deleted:** nothing (`ParentDirectoryOrSelf` was a private method, not a file).

## Deviations & follow-ups

- **Plan step 5 (`QuickStartDialogService`) — no change made, as the plan anticipated.** `DialogHeight`
  is `NaN` with `DialogMaxHeight` infinite, so the seventh field grows the dialog rather than clipping
  it. The card's design height went 520 → 600; with dialog chrome that is ≈ 720 px, which fits a 1080p
  screen with room to spare, so no cap was introduced and the six `Dialog*` properties are untouched.
- **Beyond the letter of the plan:** five files carrying "six answers"/"six questions" in prose (the
  tooltip, the dialog service, the card's code-behind, `IQuickStartDialogService`,
  `MainWindowViewModel`) were corrected. The plan listed only the four implementation files plus the
  guide, but this change made those sentences false; leaving them would have left the codebase asserting
  something untrue about itself. No behaviour is affected.
- **The card's layout is not covered by a test.** The repository has no Avalonia headless platform yet —
  PHASE02 adds it. The bindings' targets (`OutputPath`, `BrowseOutputPathCommand`) are exercised by the
  ViewModel tests and the markup compiles under `AVLN`, but the *visual* result of the new field was not
  asserted. PHASE02's headless suite is scoped to the splash window; extending it to the quick-start card
  is a reasonable follow-up, not a gap this phase created.
- **Line endings:** the working tree is consistent; nothing to recommend.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release` — **Build succeeded. 0 Warning(s), 0 Error(s)** (17.6 s), all
  seven projects including the XAML compiler over the edited `QuickStartCard.axaml`.
- `dotnet test --solution Enigma.Msi.slnx -c Release` — **447 passed, 0 failed, 0 skipped** (10.4 s)
  across all four suites (`Enigma.Msi.UnitTests` on net8.0 and net10.0, `Enigma.Msi.Desktop.UnitTests`,
  and the `net472` `Enigma.Msi.Worker.UnitTests` with its WixSharp drift guards). 444 → 447: seven cases
  added, three derivation cases removed, one renamed.

**Acceptance criteria**

| Criterion | Met |
|---|---|
| Seven fields, in the order name → version → manufacturer → release folder → output folder → icon → executable | Yes — `QuickStartCard.axaml` |
| Output field has a working folder picker and a permanent hint | Yes — ungated `BrowseOutputPathCommand`, `.hint` `TextBlock` |
| Apply disabled while the output folder is blank, enabled once all seven are valid | Yes — `CanApply`, covered by tests |
| Applying puts the entered output folder on the form verbatim; nothing derives it; `ParentDirectoryOrSelf` gone | Yes — grep confirms no remaining reference |
| An output folder inside the release folder applies without warning or block | Yes — two tests, one per ViewModel |
| A quick-start package with an existing output folder passes Validate with zero problems | Yes — `MainWindowViewModelTests` real-folder cases, and `ApplyQuickStart_ProducesAPackageThatPassesTheInMemoryRules` |
| Warning-free build; whole suite passes including the new cases | Yes — 0 warnings, 447/447 |
