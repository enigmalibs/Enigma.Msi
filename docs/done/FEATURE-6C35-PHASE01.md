# FEATURE-6C35 — PHASE01 — UI improvements (hints, build overlay, shortcuts, icons, warning)

**Status:** DONE
**Branch:** `feature/feature-6c35-phase01-ui-polish`

## Summary

The five user-requested improvements to `Enigma.Msi.Desktop`, all inside `src/Enigma.Msi.Desktop` plus its
test project and the desktop guide. No library, worker, model or schema change; no new package reference.

1. **Variable hints.** A shared `TextBlock.hint` style (11 px, `EnigmaForegroundTertiaryBrush`, wrapping)
   backs a permanent hint under *Install path* and under a shortcut row's *Location* and *Target*, with the
   wording the plan fixed. Each hint is grouped with its field in a tight `StackPanel` so the section's
   12 px spacing separates fields, not a field from its own hint. Placeholders are untouched.
2. **Build-progress overlay.** New app-side seam `IBuildProgressService` (`ShowAsync(ICommand)` ·
   `ReportMessage(string)` · `HideAsync()`) over the control library's `IOverlayService`, with
   `BuildProgressService` owning the `BuildProgressCard` it shows — a 400 px card with a hammer icon,
   *Building MSI…*, an indeterminate bar, the latest streamed line (`CharacterEllipsis`) and Cancel.
   `MainWindowViewModel.BuildAsync` raises it after validation passes and **before** the pre-flight check,
   takes it down before every outcome report, and guarantees it in a `finally`; hiding is idempotent, so the
   `finally` only ever fires for a path that did not get to its own hide. The toolbar's Cancel button is
   gone — unreachable under a modal overlay — and the card's Cancel is the one affordance.
3. **Shortcuts rework.** The `ListBox` is now an `ItemsControl` inside a `MaxHeight="420"` `ScrollViewer`,
   one bordered card per row with the row's name as its header (`ShortcutViewModel.DisplayName`, falling
   back to *Shortcut*) and its own trash button. `SelectedShortcut`/`HasSelectedShortcut` are deleted and
   `RemoveShortcutCommand` is parameterized. Removing the selection removes the root cause of the
   selected/pressed background flash when clicking into an editor inside a row.
4. **Expander icons.** Icon Set A on the five `SettingsCardExpander`s via `{ei:IconGeometry}` —
   Product → `Package`, Install and output → `HardDrives`, Control Panel information → `Info`,
   Shortcuts → `LinkSimple`, Managed UI → `AppWindow`.
5. **Warning visibility.** The *Package incomplete* message renders in `EnigmaWarningBrush` at full opacity
   (`Opacity="0.7"` removed).

## Files/modules touched

**Created**

- `src/Enigma.Msi.Desktop/Services/IBuildProgressService.cs`
- `src/Enigma.Msi.Desktop/Services/BuildProgressService.cs`
- `src/Enigma.Msi.Desktop/Views/BuildProgressCard.axaml` + `.axaml.cs`
- `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/ShortcutViewModelTests.cs`

**Modified**

- `src/Enigma.Msi.Desktop/Views/MainWindow.axaml` — all five UI changes plus the hint style
- `src/Enigma.Msi.Desktop/ViewModels/MainWindowViewModel.cs` — `IBuildProgressService` injected, the
  show/report/hide cycle, one dispatcher post feeding both the log and the card
- `src/Enigma.Msi.Desktop/ViewModels/PackageEditorViewModel.cs` — selection deleted, remove parameterized
- `src/Enigma.Msi.Desktop/ViewModels/ShortcutViewModel.cs` — `DisplayName` + `UnnamedDisplayName`
- `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs` — the new singleton, doc comment updated
- `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/MainWindowViewModelTests.cs`,
  `PackageEditorViewModelTests.cs`
- `docs/guides/desktop-app.md` — command table, hint table, shortcut cards, the overlay
- `docs/roadmap.md`, `docs/plan/FEATURE-6C35.md` — statuses

## Deviations & follow-ups

- **The plan's xmlns fallback was not needed.** `Enigma.Icons.Avalonia` declares
  `XmlnsDefinition` for both `Enigma.Icons.Avalonia` *and* `Enigma.Icons.Avalonia.Markup`, so the existing
  `ei:` prefix reaches `IconGeometry`; no `eim:` alias was added.
- **The per-row remove binding differs from the plan's snippet.** The plan cast the `ItemsControl`'s
  DataContext to `PackageEditorViewModel`, but DataContext is inherited from the window, so the resolved
  path is
  `$parent[ItemsControl].((vm:MainWindowViewModel)DataContext).Package.RemoveShortcutCommand`.
  Same contract, correct source object.
- **`RemoveShortcutCommand` carries no `CanExecute`.** With the selection gone there is nothing to gate on;
  it is always executable and a null parameter is a no-op, as the plan specified.
- **The card carries a hammer icon** next to its title, matching the toolbar's Build button. Not in the
  plan; cosmetic.
- **Line endings:** nothing to report — every touched file is LF, per `.gitattributes`.
- **Follow-up (not a defect):** the shortcut list keeps its pre-existing `MaxHeight="420"`, so with several
  shortcuts the cards scroll inside the left column's own scroll area. Preserved deliberately; worth
  revisiting if the nested scrolling annoys in use.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release` — **0 warnings, 0 errors** (XAML `AVLN` diagnostics included;
  compiled bindings mean the new binding paths are compile-time checked).
- `tests/Enigma.Msi.Desktop.UnitTests` — **70 tests, 0 failed** (57 before this phase). New/changed:
  overlay shown once with the cancel command · not shown when validation fails · taken down *before* the
  report on success, build failure, pre-flight failure and cancellation · every streamed line forwarded to
  `ReportMessage` · ctor null-guard · per-row remove (right row, null no-op, always executable) · add
  appends · `Changed` stops following a removed row · `DisplayName` fallback, value and notification.
- `dotnet test --solution Enigma.Msi.slnx -c Release` — **302 tests, 270 passed, 32 failed**, exactly the
  same 32 failures as on the parent commit before any change: they are `Enigma.Msi.UnitTests` cases that
  need Windows (stub-worker `.exe` spawning, the `wix` CLI, Windows path semantics in
  `MsiFilename_MustNotBeAPath`), and the net472 `Enigma.Msi.Worker.UnitTests` suite cannot start at all
  here (`mono` is not installed). **This dev added no failure** — every one of the 13 new tests passes and
  the desktop suite is fully green. The suite's Windows-only half still needs a Windows run before release;
  PHASE02's dogfood build is on that same machine.
- **Visual acceptance verified by rendering, not by inspection.** A throwaway harness (scratch directory,
  not in the repo) hosted the real `MainWindow` under headless Skia and captured frames in both theme
  variants: the three hints render below their fields in tertiary foreground; the five expander icons show;
  the incomplete-package message is the theme's warning orange at full opacity and legible on both
  backgrounds; the toolbar has no Cancel; shortcut rows are bordered cards with header and trash button.
  The same harness verified behaviour the pixels cannot show: the shortcut host is an `ItemsControl` with
  **zero** selectable containers under it (the flash's root cause is gone), each row's remove button
  resolves its command and carries its own row as the parameter and removing one leaves exactly the other,
  the header follows an edited name live, and the card's `Message`/`CancelCommand` bindings resolve and
  update.
- **Not verified here:** an actual MSI build behind the overlay (needs Windows + the WiX CLI) and
  pointer-interaction feel. The cancel path is covered by the ViewModel tests and the worker-tree kill is
  unchanged from `FEATURE-43A9`.
