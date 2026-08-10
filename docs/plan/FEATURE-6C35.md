# FEATURE-6C35 — Desktop app 1.1.0: UI polish & release

**Status:** IN PROGRESS (multi-phase — 2 phases)
**Type:** FEATURE (multi-phase)
**Branch (per phase, at build time):** `feature/feature-6c35-phaseNN-<slug>` — one branch per phase, cut from current `HEAD`.
**Depends on:** FEATURE-5F00 fully DONE (1.0.0 prepared; this item builds the app's next release on top).

## Objective

Ship **Enigma.Msi.Desktop 1.1.0**: five user-facing UI improvements to the Avalonia app, then the
release preparation for the app. The library, worker, model and `.msipkg.json` schema are untouched —
**Enigma.Msi (the nupkg) stays at 1.0.0 and is not re-released**; every change lives in
`src/Enigma.Msi.Desktop` plus docs.

The five improvements (all user-requested):

1. **Variable hints** — permanent helper text under the three token fields (Install path, shortcut
   Location, shortcut Target), because the placeholder disappears the moment typing starts.
2. **Build-progress overlay** — a modal overlay (indeterminate bar, latest log line, Cancel) while an
   MSI build runs; today nothing shows that a long build is in progress.
3. **Shortcuts rework** — per-row remove without list selection, killing the selection/pressed
   background flash when clicking into an editor inside a row.
4. **Expander icons** — icons in the five left-hand `SettingsCardExpander` headers.
5. **Warning visibility** — the "Package incomplete — press Validate for details." message in the
   theme's warning orange instead of dimmed default text.

## Context & constraints

- **App-only release.** No library/worker/model/schema change; `Enigma.Msi.csproj` (incl.
  `<PackageReleaseNotes>`) is not touched. Old profiles open unchanged — there is no migration.
- **No new dependencies.** Everything used already ships in the pinned UI stack: verified against the
  real packages that `Enigma.Avalonia.Desktop` 1.0.0 has `SettingsCardExpander.IconData` (`Geometry`),
  `IOverlayService.ShowAsync(Control)`/`HideAsync()` (host already placed in `MainWindow` and
  registered in `App`), and the theme dictionary defines `EnigmaWarningBrush` +
  `EnigmaForegroundTertiaryBrush` in both Dark and Light variants; `Enigma.Icons.Avalonia` 1.0.0 has
  the `IconGeometry` markup extension (`Enigma.Icons.Avalonia.Markup`, `Icon` typed
  `Enigma.Icons.Phosphor.PhosphorIcon`). `Directory.Packages.props` is untouched.
- **Hint tokens are verified, not invented.** The token lists below were extracted from WixSharp
  2.12.0's real special-folder mapping (`%ProgramFiles%`, `%ProgramFiles64%`, `%LocalAppData%`,
  `%CommonAppData%`, `%Desktop%`, `%ProgramMenu%`, `%StartMenu%`, `%Startup%`, … plus `[INSTALLDIR]`).
  The validator does not restrict tokens; WixSharp resolves them — the hints cite a curated common
  subset, they do not claim to be exhaustive.
- **MVVM seams, house pattern.** The ViewModel never constructs Avalonia controls: the overlay is
  driven through a thin app-side service (like the existing `IPathPickerService` / `IUiDispatcher`
  seams) so the desktop test suite can substitute it with NSubstitute.
- **Invariants preserved:** the build log stays append-only (one `Add` per line, never
  re-concatenation); brushes referenced via `{DynamicResource Enigma…Brush}` only; zero-warning build
  (`TreatWarningsAsErrors`, XAML `AVLN` diagnostics included); explicit `using`s; XML docs on new
  public members.
- **User decisions (interview, 2026-08-10):** scope includes release prep · app version **1.1.0**
  (semver minor — user-visible features) · shortcuts = per-row remove, selection concept deleted ·
  overlay card includes the latest streamed log line · toolbar Cancel button removed (unreachable
  under the modal overlay) · hints below the fields · icon Set A (see PHASE01) ·
  `EnigmaWarningBrush` at full opacity, no extra icon · 2-phase split (the five UI changes overlap in
  `MainWindow.axaml`, so they land as one dev).
- **Defaults recorded at planning:** shortcut card header shows the shortcut's **Name** with fallback
  "Shortcut" (no maintained index numbering) · shared hint style (small font, tertiary foreground) ·
  overlay card fixed width ≈ 400 px, message single-line with `CharacterEllipsis` · overlay-message
  updates ride the existing `IUiDispatcher` marshalling · the 1.0.0 MSI profile stays in
  `msiProfiles/` and the 1.1.0 profile is added alongside.

## PHASE01 — UI improvements (hints, build overlay, shortcuts, icons, warning)

**Status:** DONE (see `docs/done/FEATURE-6C35-PHASE01.md`)
**Branch:** `feature/feature-6c35-phase01-ui-polish`

All in `src/Enigma.Msi.Desktop` (+ its test project + the desktop guide). Steps ordered smallest to
largest so the XAML-only changes are in place before `MainWindow.axaml`'s bigger surgery.

1. **Warning message** (`Views/MainWindow.axaml`): the "Package incomplete…" `TextBlock` gets
   `Foreground="{DynamicResource EnigmaWarningBrush}"`; the `Opacity="0.7"` is removed. Text, binding
   and position unchanged.
2. **Expander icons** (`Views/MainWindow.axaml`): `IconData` on the five `SettingsCardExpander`s —
   Product → `Package`, Install and output → `HardDrives`, Control Panel information → `Info`,
   Shortcuts → `LinkSimple`, Managed UI → `AppWindow` (all verified members of `PhosphorIcon`), via
   `{ei:IconGeometry Icon=…}`. If the `https://github.com/josueclement/Enigma.Icons` xmlns does not
   cover the `Markup` CLR namespace, add `xmlns:eim="using:Enigma.Icons.Avalonia.Markup"` instead —
   whichever compiles clean.
3. **Variable hints** (`Views/MainWindow.axaml`): one shared hint style — a `TextBlock` class (e.g.
   `Classes="hint"`) in the window's styles: `FontSize` ~11, `Foreground`
   `{DynamicResource EnigmaForegroundTertiaryBrush}`, `TextWrapping="Wrap"`, a small top margin —
   placed directly **below** each concerned editor. Exact wording:
   - Install path: `Variables: %ProgramFiles%, %ProgramFiles64%, %LocalAppData%, %CommonAppData%`
   - Location (in the shortcut row template):
     `Variables: %Desktop%, %ProgramMenu%, %StartMenu%, %Startup% — sub-folders allowed: %ProgramMenu%\Contoso`
   - Target (in the shortcut row template):
     `[INSTALLDIR] = the install folder, e.g. [INSTALLDIR]\Widget.exe`
   Placeholders stay as they are; the hint is what survives once typing hides them.
4. **Shortcuts rework** — delete the selection concept, root cause of the background flash:
   - `ViewModels/ShortcutViewModel.cs`: add a read-only `DisplayName` (`ShortcutName`, or `"Shortcut"`
     when blank) refreshed via `[NotifyPropertyChangedFor]` on `ShortcutName` — the card header.
   - `ViewModels/PackageEditorViewModel.cs`: **remove `SelectedShortcut`** (and
     `HasSelectedShortcut`); `RemoveShortcutCommand` becomes parameterized —
     `[RelayCommand] private void RemoveShortcut(ShortcutViewModel? shortcut)` removing the given row
     (null-safe no-op); `AddShortcut` only appends. The existing `OnShortcutsChanged` subscription
     bookkeeping is unchanged.
   - `Views/MainWindow.axaml`: the `ListBox` becomes an **`ItemsControl`** (keep the `MaxHeight` via a
     wrapping `ScrollViewer`); each item renders as a bordered card
     (`BorderBrush="{DynamicResource EnigmaBorderBrush}"`, `CornerRadius="4"`, padding, vertical
     spacing) with a header row — `DisplayName` left, a trash-icon remove `Button` right, bound
     `Command="{Binding $parent[ItemsControl].((vm:PackageEditorViewModel)DataContext).RemoveShortcutCommand}"`
     with `CommandParameter="{Binding}"` — above the five editors (+ the two hints from step 3). The
     toolbar's "Remove selected" button is deleted; "Add" stays. The empty-state hint stays.
5. **Build-progress overlay**:
   - New seam `Services/IBuildProgressService` (+ implementation): `Task ShowAsync(ICommand
     cancelCommand)` · `void ReportMessage(string message)` (no-op while not shown) ·
     `Task HideAsync()`. The implementation wraps the injected `IOverlayService` and owns the card —
     a small control (e.g. `Views/BuildProgressCard`) with `Message` + `CancelCommand`: title
     "Building MSI…", indeterminate `ProgressBar`, single-line message
     (`TextTrimming="CharacterEllipsis"`, fixed card width ≈ 400 px), Cancel `Button`. Registered as a
     singleton beside the other UI seams in `ServiceCollectionExtensions` (doc comment updated).
   - `ViewModels/MainWindowViewModel.cs`: inject `IBuildProgressService`. In `BuildAsync`, once
     validation passes: `ShowAsync(CancelBuildCommand)` **before** the pre-flight check; every
     streamed line (the existing `AppendLog` dispatcher post) also feeds `ReportMessage`; the overlay
     is hidden **before** the result InfoBar/dialog reporting so the outcome is never shown under the
     dimming — with a `finally` guaranteeing `HideAsync()` on every exit path (success, build failure,
     prerequisite failure, cancellation, exception). `CancelBuildCommand` itself is unchanged.
   - `Views/MainWindow.axaml`: the toolbar **Cancel button is removed** (it is unreachable while the
     modal overlay is up); the overlay's Cancel is the one cancel affordance.
6. **Tests** (`tests/Enigma.Msi.Desktop.UnitTests`):
   - `MainWindowViewModelTests` (+ substituted `IBuildProgressService`): shown exactly once when a
     build starts; hidden on success, build failure, prerequisite failure and cancellation; streamed
     lines forwarded to `ReportMessage` while building; ctor null-guard for the new dependency;
     existing gating tests updated for the changed ctor.
   - `PackageEditorViewModelTests`: `RemoveShortcut(row)` removes that row and unsubscribes it
     (`Changed` no longer raised by the removed row); null parameter is a no-op; tests referencing
     `SelectedShortcut` reworked; add still appends and wires change notifications.
   - `ShortcutViewModel`: `DisplayName` fallback and change notification.
7. **Guide** (`docs/guides/desktop-app.md`): command table loses the Cancel row (cancel now lives in
   the build overlay), the build section describes the overlay, the shortcuts section describes
   per-row remove, and the fields section mentions the variable hints.

**Acceptance criteria**
- The three hints render below their fields with the exact wording above, in tertiary foreground, in
  both themes; placeholders unchanged.
- Building shows the overlay card (title, indeterminate bar, live latest log line, working Cancel that
  cancels the build and kills the worker tree); the overlay is hidden on **every** exit path and never
  shown for a package that fails validation; the toolbar has no Cancel button.
- Shortcut rows are bordered cards with header (name, fallback "Shortcut") + working per-row remove;
  clicking into any editor inside a row triggers **no** selection/pressed background anywhere;
  `SelectedShortcut` no longer exists.
- The five expanders show the Set A icons; the incomplete-package message renders in
  `EnigmaWarningBrush` at full opacity in both themes.
- The build log remains append-only; no new package references.
- `dotnet build Enigma.Msi.slnx -c Release` zero warnings;
  `dotnet test --solution Enigma.Msi.slnx -c Release` fully green including the new tests;
  `docs/guides/desktop-app.md` matches the shipped UI.

## PHASE02 — Release prep: desktop app 1.1.0

**Status:** TODO
**Branch:** `feature/feature-6c35-phase02-release-1-1-0`

Mirrors FEATURE-5F00 PHASE04 for the next version. **App-only release:** no `dotnet pack`, no NuGet
push, no library version change.

1. `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`: `<Version>` **1.1.0**.
2. `RELEASENOTES.md`: a 1.1.0 section on top covering the five desktop-app improvements, stating
   explicitly that the **Enigma.Msi library remains 1.0.0** (nupkg not re-released). The 1.0.0 section
   stays as history.
3. `msiProfiles/Enigma.Msi.Desktop.1.1.0.msipkg.json` — clone of the 1.0.0 profile with: `version`
   1.1.0; **fresh `productId`** from a real generator (`uuidgen` / `[guid]::NewGuid()` — never
   hand-fabricated); **`upgradeCode` reused verbatim** (`3405046f-527a-439e-a22f-866247dc8314` — the
   app's permanent identity); everything else unchanged (releasePath stays the win-x64 **publish**
   output). The 1.0.0 profile file is kept.
4. Verify the new profile deserializes via `MsiPackageJson` and passes `IMsiPackageValidator`'s
   in-memory rules with zero errors (no worker/WixSharp invocation).
5. **Dogfood MSI build** (manual acceptance — Windows + wix CLI), per `docs/RELEASE.md` §7 from the
   repo root: `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64
   --self-contained false`, then the worker CLI build of
   `msiProfiles/Enigma.Msi.Desktop.1.1.0.msipkg.json`. Record the outcome (or the explicit blocker)
   in the completion doc — never silently skipped. Install/launch/uninstall verification stays with
   the maintainer.
6. Review `docs/RELEASE.md` for 1.0.0-specific staleness; note in the completion doc that an app-only
   release has no library tag/pack/push, and that whether/how to tag an app-only release (e.g. an
   app-scoped tag) is the maintainer's choice — nothing outward-facing is run.

**Acceptance criteria**
- `<Version>` 1.1.0; release-notes section added; 1.1.0 profile committed, GUID contract respected
  (fresh `productId`, verbatim `upgradeCode`), deserializes + zero in-memory validation errors.
- Dogfood MSI build performed and recorded (or explicitly blocked with reason).
- Build zero warnings, full test suite green (no code changes expected — the gate still runs).
