# FEATURE-74C4 — PHASE02 — Quick-start dialog & Control Panel default

**Status:** DONE
**Branch:** `feature/feature-74c4-phase02-quick-start`
**Plan:** `docs/plan/FEATURE-74C4.md` (PHASE02)

## Summary

Six answers now produce a buildable package. Two changes, both app-only:

- **Control Panel information is on by default.** `Reset()` sets `HasControlPanelInfo = true`, and nothing
  else moved: `ToPackage()` still writes the section iff the toggle is on, so an untouched new package
  serializes `"controlPanel": {}` — which is exactly what the toggle claims. **`LoadFrom` was deliberately
  not touched.** It still derives the toggle from `package.ControlPanel is not null`, so a 1.0.0/1.1.0
  profile written without the section still opens with the toggle off, and saving it again does not add
  one. "Default on" is about *new* packages, and that boundary is guarded by the pre-existing
  `LoadFrom_WithoutOptionalBlocks_SwitchesThemOff`, left untouched and passing.
- **A one-page quick start.** A `MagicWand` **Quick start** button, first in the toolbar, opens a modal
  `ContentDialog` with six fields. Apply is disabled until all six are answered, the version parses, and
  the executable is inside the release folder. Applying **resets the form then fills it** — fresh
  `productId`/`upgradeCode` — and, when the form already holds work, asks first. The dialog also opens
  **once by itself** when the app starts on an empty form.

What the quick start derives: `InstallPath` = `%ProgramFiles%\<app name>`; `OutputPath` = the release
folder's **parent** (falling back to the folder itself at a drive root, which still validates as an
existing directory); `MsiFilename` = the app name sanitized for the validator's rule (invalid file-name
characters removed, trimmed, a trailing `.msi` stripped `OrdinalIgnoreCase`, `"package"` if nothing
survives); the Control Panel section on with `ProductIcon`; and two shortcuts, `%ProgramMenu%` then
`%Desktop%`, each named after the app, targeting `[INSTALLDIR]\<relative exe>` with the icon.

**The relative executable path is what crosses the seam, not the absolute one** — `QuickStartSettings`
carries `ExecutableRelativePath`, because the release folder is what `[INSTALLDIR]` becomes after
installation, so a nested exe must keep its sub-folder (`[INSTALLDIR]\bin\Widget.exe`).

**`CanApply` is string-and-parse only, never disk.** That mirrors the split the app already lives by —
`IsPackageValid` runs the in-memory rules on every keystroke and leaves the file-system rules to
Validate/Build — so a path that does not exist is reported once, by the Problems pane, rather than
half-reported in two places.

**Two leaks the shared dialog host makes easy, both closed.** `IContentDialogService` owns one
`ContentDialog` and resets `IsPrimaryButtonEnabled` by plain assignment rather than `ClearValue`, so the
`IDisposable` from `Bind(...)` is disposed in a `finally` around the `await` — `ShowAsync` completes
exactly when the dialog closes, which makes that the only correct site. And `ShowAsync` does not reset the
six `Dialog*` size properties, so the quick-start card **and** the Yes/No confirmation each state their
own size instead of inheriting whatever the last dialog left behind.

**The quick-start ViewModel is transient and reached through a `Func<>`.** A singleton would open the
second run on the first run's answers; verified against the running app (a re-opened dialog is blank, with
the version back at `1.0.0`).

## Files touched

**Created**

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/ViewModels/QuickStartSettings.cs` | The six validated answers as a `sealed record` |
| `src/Enigma.Msi.Desktop/ViewModels/QuickStartViewModel.cs` | The dialog's form: `CanApply`, `ExecutableRelativePath`, `ExecutableError`, three browse commands |
| `src/Enigma.Msi.Desktop/Views/QuickStartCard.axaml(.cs)` | The dialog's content; code-behind focuses the first field |
| `src/Enigma.Msi.Desktop/Services/IQuickStartDialogService.cs` | Seam: ask the six questions |
| `src/Enigma.Msi.Desktop/Services/QuickStartDialogService.cs` | Owns the card, wraps `IContentDialogService`, gates Apply |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/QuickStartViewModelTests.cs` | 18 tests |

**Modified**

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/ViewModels/PackageEditorViewModel.cs` | `Reset()` leaves the Control Panel section on; `HasData`; `ApplyQuickStart` + three derivation helpers |
| `src/Enigma.Msi.Desktop/ViewModels/MainWindowViewModel.cs` | `IQuickStartDialogService` injected before the logger; `QuickStartCommand`, `ShowQuickStartOnStartupCommand`, the Yes/No confirmation |
| `src/Enigma.Msi.Desktop/Services/IPathPickerService.cs` | Fifth question `PickExecutableAsync`; summary four → five |
| `src/Enigma.Msi.Desktop/Services/PathPickerService.cs` | `PickExecutableAsync` over `ShowOpenFileDialogAsync`, reusing `ExistingDirectoryOrEmpty` |
| `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs` | The dialog service singleton; the ViewModel transient + its `Func<>` factory |
| `src/Enigma.Msi.Desktop/Views/MainWindow.axaml` | The Quick start button, first in the toolbar's left panel |
| `src/Enigma.Msi.Desktop/Views/MainWindow.axaml.cs` | `OnOpened` invokes `ShowQuickStartOnStartupCommand` |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/MainWindowViewModelTests.cs` | Three construction sites updated (arity 10 → 11); 12 tests added |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/PackageEditorViewModelTests.cs` | 22 tests added |
| `docs/guides/desktop-app.md` | A *Quick start* section, the toolbar row, the Control-Panel-default note, the workflow lead-in |
| `docs/roadmap.md`, `docs/plan/FEATURE-74C4.md` | Statuses |

**No csproj change and no `Directory.Packages.props` change** — `QuickStartCard.axaml` is picked up by the
existing `<AvaloniaResource>`/XAML globs, and nothing new was referenced.

## Deviations & follow-ups

- **`HasExecutableError` was added** alongside `ExecutableError`, which the plan's step 6 does not list.
  The card needs a `bool` to bind `IsVisible` to; the alternative was
  `Converter={x:Static ObjectConverters.IsNotNull}` and a fourth xmlns. One documented property is
  cheaper, and it is directly assertable in a test.
- **`ProgramMenuShortcutPath` is a private constant rather than a reuse of
  `ShortcutViewModel.DefaultShortcutPath`.** The two agree today, but "what a new row defaults to" and
  "where the quick start puts the Start-menu shortcut" are separate decisions; borrowing one for the other
  would make a change to either silently move the other.
- **`ApplyQuickStart` writes `\` literally**, never through `Path.Combine`, for the install path and the
  shortcut targets. An MSI path is a Windows path whichever machine builds it, and `Path.Combine` emits
  `/` on a non-Windows host — which would have made this dev's own tests platform-dependent. For the same
  reason the derivation tests build their paths from `Path.GetPathRoot(Path.GetTempPath())` (`C:\` here,
  `/` elsewhere) instead of hard-coded Windows literals, and the sanitization test uses `/` — the one
  character `Path.GetInvalidFileNameChars()` rejects on every platform this suite runs on.
- **The Yes/No confirmation also states its own six size properties.** The plan only requires this of the
  two *new* dialogs; the confirmation is a third one, and without it a plain sentence would inherit the
  620 px width the quick-start card had just set on the shared host.
- **The plan's step 6 mentions `[NotifyCanExecuteChangedFor]` on "the Apply command".** There is no Apply
  command — step 8's chosen design binds the dialog's `IsPrimaryButtonEnabled` to `CanApply` — so only
  `[NotifyPropertyChangedFor(nameof(CanApply))]` is present. `[NotifyCanExecuteChangedFor]` is used where
  it does apply: `ReleasePath` re-gates the icon and executable browse commands.
- **Two tests beyond the plan's list.** `Reset_ThenSaved_WritesAnEmptyControlPanelBlock` pins the
  acceptance criterion's literal `"controlPanel": {}`, and
  `QuickStart_Applied_YieldsAPackageThatValidatesCleanlyAndEnablesBuild` runs the whole apply →
  Validate → Build-enabled path against **real folders**, so the *environment* rules are exercised too.
  The plan treated that end-to-end check as manual; it is now also automated.
- **`msiProfiles/` needed no attention:** both committed profiles carry a `controlPanel` block, so the
  new default cannot change how they open. Verified by inspection.
- **Documentation sweep:** `CLAUDE.md`'s build-state block gained a `FEATURE-74C4` paragraph covering
  `PHASE01` and `PHASE02` and noting that `PHASE03` still owns the outstanding dogfood-MSI debt, plus a
  short paragraph on the two shared-`ContentDialog` traps (the undisposed binding, the un-reset sizes) and
  the UIA title gap, so the next dialog added to this app does not rediscover them. `RELEASENOTES.md` was
  deliberately left alone — `PHASE03` is the release-prep phase and owns the 1.2.0 entry. `README.md`
  needed nothing: it mentions the app in one line and describes no features.
- **Line endings:** the whole diff is LF; no CRLF churn observed, nothing to recommend.
- Nothing in the worker, the library, the model or the `.msipkg.json` contract was touched, so 1.0.0,
  1.1.0 and 1.2.0 profiles stay interchangeable in both directions. The build log's append-only rule is
  untouched — no code in this dev writes to it.

## Build & test evidence

```
dotnet build Enigma.Msi.slnx -c Release      →  Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test --solution Enigma.Msi.slnx -c Release
                                             →  total: 444   failed: 0   succeeded: 444   skipped: 0
```

Zero warnings includes the `AVLN` XAML count on both projects that compile `.axaml` — those print rather
than fail the compile under `TreatWarningsAsErrors`, so the count was read, not inferred. 444 total, up
from 392: **52 new tests** (18 `QuickStartViewModelTests`, 22 in `PackageEditorViewModelTests`, 12 in
`MainWindowViewModelTests`), with the 32 Windows-only library cases and the whole worker suite running on
this pass as well.

**The running app was driven through UI Automation** (Windows 11, Release build), which covers every
manual acceptance criterion in the plan. Two passes, 30 checks, all passing:

| Criterion | Evidence |
|---|---|
| It opens by itself on a freshly launched, empty app | Startup tree contains the card and its six `Edit`s with no input; screenshot inspected |
| Apply stays disabled until the six answers are complete | Disabled at open, still disabled with five of six, enabled on the sixth |
| The version must parse | Setting `one point two` disabled Apply again; `2.1.0` re-enabled it |
| An exe outside the release folder shows the error | Setting an exe under a sibling folder surfaced *"The executable must be inside the release folder…"* and disabled Apply; moving it back cleared both |
| The icon/exe pickers wait for the release folder | At open, only the release-folder `Browse…` is enabled; both others enable the moment the folder is set |
| **Apply yields a form that passes Validate with zero problems and enables Build**, against a real folder | `installPath='%ProgramFiles%\Contoso Widget'`, `outputPath` = the release folder's parent, `msiFilename='Contoso Widget'`, then Validate → **`Problems (0)`**, Build **enabled**, the *Package incomplete* hint gone |
| Escape dismisses it | `{ESC}` closed the startup dialog — which also proves the card takes focus, since Escape only fires while focus is inside the dialog |
| The toolbar button opens it | Invoking **Quick start** re-opened the dialog |
| A re-opened dialog starts fresh | All fields blank, version back at `1.0.0` — the transient ViewModel + `Func<>` factory doing their job |
| Cancelling changes nothing and asks nothing | Typing then Cancel left the form empty and produced no confirmation |
| Applying over a form with data asks first | The confirmation appeared with *"The package in the form will be replaced…"* and a **No** button |
| Declining leaves the form exactly as it was | `Typed by hand` still in the app-name field afterwards |
| Accepting replaces the package | App name, install path and output folder all became the derived values |
| No orphaned processes | `Enigma.Msi.Desktop` and `wix` process counts both zero after each pass |

Shortcut derivation is the one thing UIA could not read — the Shortcuts expander is collapsed, so its rows
are not in the automation tree — and it is covered instead by the unit tests, which assert exactly two
rows in `%ProgramMenu%`-then-`%Desktop%` order with the right names, `[INSTALLDIR]\…` targets (root-level
and nested) and icons.

**Worth knowing for next time:** the control library's `ContentDialog` does **not** expose its `Title` to
UI Automation — only the content and the buttons are in the tree. A dialog assertion has to key off the
message text or a button name, never the title.
