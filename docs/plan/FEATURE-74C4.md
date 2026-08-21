# FEATURE-74C4 — Desktop app 1.2.0: identity, quick start & release

**Status:** IN PROGRESS (multi-phase — 3 phases)
**Type:** FEATURE (multi-phase)
**Branch (per phase, at build time):** `feature/feature-74c4-phaseNN-<slug>` — one branch per phase, cut from current `HEAD`.
**Depends on:** FEATURE-6C35 fully DONE (app at 1.1.0; this item builds the next app release on top).

## Objective

Ship **Enigma.Msi.Desktop 1.2.0**: give the app a visual identity (a splash screen and an About dialog),
remove the tedium from first-time package setup (a one-page quick-start dialog that derives the install
path, the output folder, the MSI file name, the product icon and both shortcuts from six answers), switch
Control Panel information on by default, then prepare **and verify** the release.

**The Enigma.Msi library stays at 1.0.0 and is not re-released** — an app-only release runs no
`dotnet pack` and no NuGet push. Every code change lives in `src/Enigma.Msi.Desktop` and
`tests/Enigma.Msi.Desktop.UnitTests`, plus docs and `msiProfiles/`. The model, the validator, the
`.msipkg.json` contract (`schemaVersion` 1), the build client and the `net472` worker are untouched, so
profiles are interchangeable between 1.0.0, 1.1.0 and 1.2.0 in both directions and there is no migration.

The four user-requested improvements:

1. **Splash screen** — logo, app name, tagline, version and the author's name, ~2 s, dismissable.
2. **About dialog** — the same identity plus copyright and a link to the GitHub repository.
3. **Control Panel information on by default** — a new package no longer starts with that section off.
4. **Quick start** — one dialog, six fields, and the main form comes back filled: product identity,
   install path `%ProgramFiles%\<AppName>`, output folder, MSI file name, the product icon, and two
   icon'd shortcuts (`%ProgramMenu%` and `%Desktop%`) targeting `[INSTALLDIR]\<exe>`.

## Context & constraints

- **App-only release.** No library/worker/model/schema change; `Enigma.Msi.csproj` (including
  `<PackageReleaseNotes>`) is not touched. `docs/RELEASE.md` §0 says which of its sections apply to an
  app-only release flavour.
- **No new dependencies.** Everything needed already ships in the pinned UI stack (Avalonia **12.1.1**,
  `Enigma.Avalonia.Desktop` **1.0.0**, `Enigma.Icons.Avalonia` **1.0.0**). `Directory.Packages.props` is
  untouched. The splash is a plain Avalonia `Window`; the two dialogs are the control library's
  `ContentDialog`; the logo is derived from artwork already in the repo.
- **The app is a single-window form.** `INavigationService` is deliberately unregistered
  (`ServiceCollectionExtensions`), so "About page" means a **modal `ContentDialog`**, not a navigation
  shell. Introducing navigation for one read-only dialog is out of scope.
- **MVVM seams, house pattern.** A ViewModel never constructs Avalonia controls: both new dialogs are
  driven through thin app-side services in the shape of the existing `IPathPickerService` /
  `IUiDispatcher` / `IBuildProgressService` seams, so the desktop test suite can substitute them with
  NSubstitute. `BuildProgressService` is the reference implementation to copy: it owns the control it
  shows and wraps one control-library service.
- **Validator facts that shape the derivations** (read from `MsiPackageValidator`, not assumed):
  `output.outputPath` is required **and** must be an existing directory; `install.releasePath` must exist
  **and be non-empty**; `output.msiFilename` must be a plain file name, with no invalid filename
  characters and no `.msi` extension; `controlPanel` has **no** in-memory rule at all and its environment
  rule only fires when `productIcon` is non-null. Defaulting the Control Panel section on therefore
  cannot introduce a validation error, and a blank output folder certainly would — which is why the
  quick start must derive one.
- **`appicon.ico` already contains a 256×256 PNG frame.** Its ICO directory has 7 entries; entry 6 (the
  256×256 one) is PNG-encoded **RGBA with alpha actually in use** (transparent corners, opaque centre),
  at **offset 36932, length 4491 bytes**. `Assets/logo.png` is therefore a **verbatim byte copy** of that
  entry — no re-encoding, no `System.Drawing`, no external tool, no quality loss. Proven at planning time
  by extracting it to a scratch file: identical SHA-256 on both sides, valid PNG magic, IHDR 256×256.

### Verified against the shipped packages at planning time (2026-08-17)

Everything below was checked against the real sources on disk — `Enigma.Avalonia` / `Enigma.Icons` at the
1.0.0 tag, the Avalonia 12.1.1 assemblies in the NuGet cache, and this repo's own tests. **Do not
re-derive these from memory; they are the exact strings to type.**

- **Icon members exist verbatim** in `PhosphorIcon`: `MagicWand`, `Rocket`, `Sparkle`, `Info`, `Question`,
  `GithubLogo`, `Star`, plus the five already used. The default weight is `Regular` wherever the API or
  the `ei:Icon` control defaults one (even though `default(PhosphorWeight)` is `Thin`).
- **Brush keys exist verbatim in both variants:** `EnigmaBackgroundBrush`, `EnigmaSurfaceBrush`,
  `EnigmaBorderBrush`, `EnigmaBorderSubtleBrush`, `EnigmaForegroundBrush`,
  `EnigmaForegroundSecondaryBrush`, `EnigmaForegroundTertiaryBrush`, `EnigmaWarningBrush`. The **only**
  accent keys are `EnigmaAccentBrush` and `EnigmaAccentHoverBrush` — there is no accent-foreground,
  -pressed or -fill key to reach for.
- **`Window.SystemDecorations` is obsolete in Avalonia 12.1.1** — it raises
  `AVLN5001: 'Window.SystemDecorations' is obsolete: Use WindowDecorations instead.`, which breaks the
  repo's zero-warning rule (an `AVLN` warning, so it prints rather than failing the compile — read the
  count). The splash uses **`WindowDecorations="None"`** (enum `WindowDecorations { None, BorderOnly,
  Full }`).
- **The desktop lifetime auto-shows whatever `MainWindow` holds** when
  `OnFrameworkInitializationCompleted` returns, and `MainWindow` is settable at any time. The default
  `ShutdownMode` is **`OnLastWindowClose`** — a *last window standing* rule, not a MainWindow-identity
  rule. **Do not set `OnMainWindowClose`.**
- **`IContentDialogService` owns ONE shared `ContentDialog` host**, and its reset assigns
  `IsPrimaryButtonEnabled` rather than calling `ClearValue` — a binding installed for one dialog was
  proven still live during the next. Any binding onto the host must be **disposed** when the dialog
  closes. For the same reason **`ShowAsync` does not reset the six `Dialog*` size properties** (a known
  library defect, filed upstream, not a feature): both new dialogs set their size **in their own
  `configure`**, never relying on inheritance or on a value set once on the host. Defaults are
  `DialogMinWidth` 320, `DialogMaxWidth` **600**, `DialogWidth`/`DialogHeight` `NaN`.
- **Escape only fires while focus is inside the dialog**, and nothing focuses the content on open, so a
  dialog whose content must be Escape-dismissable has to give a control initial focus. `DefaultButton` is
  intent-only: there is **no** Enter-to-commit.
- **Test-suite conventions:** xUnit v3, `[Fact]` only (the suite contains zero `[Theory]`), plain
  `Assert.*` (no fluent assertion library), one `public sealed class <Type>Tests` per type with an XML
  summary, test names in `Subject_Condition_Behaviour` form, NSubstitute substitutes held in `readonly`
  fields, **no headless Avalonia harness**, and `TestContext.Current.CancellationToken` for bounded waits.
- **Invariants preserved:** the build log stays append-only; brushes referenced via
  `{DynamicResource Enigma…Brush}` only; `PlaceholderText`, never the obsolete `Watermark`; explicit
  `using`s; file-scoped namespaces; XML docs on new public members; zero-warning build
  (`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`). `AVLN` XAML diagnostics are **not** promoted by
  `TreatWarningsAsErrors`, so the warning count of every project that compiles `.axaml` must be read, not
  just the "Build succeeded" line.
- **Compiled bindings are on** (`AvaloniaUseCompiledBindingsByDefault`), so every new `UserControl` root,
  `Window` root and `DataTemplate` needs an explicit `x:DataType`, or the build fails with `AVLN2100`.

### User decisions (interview, 2026-08-17)

- Quick start is a **one-page dialog** with all six fields visible — not a multi-step wizard, not a
  live-preview panel.
- Applying it **resets the form then fills it**, with a **Yes/No confirmation first when the form already
  holds data**. Fresh `productId`/`upgradeCode` come from that reset.
- **All six fields are required** before Apply enables. Recorded deliberately against the plan author's
  recommendation (four required, Icon and Main exe optional with graceful degradation): the consequence
  is that someone without an `.ico` cannot use the quick start at all. Accepted, because the main form
  remains fully editable — the quick start is assistance, never the only path.
- Entry points: a **"Quick start" toolbar button**, **and** an automatic open **once at startup when the
  form is empty**. **No opt-out setting** in 1.2.0 — the app has no settings store, and introducing one
  (path, format, corrupt-file handling) for a single boolean is out of proportion. Escape dismisses.
- Splash: **~2 seconds**, dismissed early by **any click or key press**; the main window becomes visible
  only **after** the splash closes.
- Logo: a new **`Assets/logo.png`** extracted from `appicon.ico`, bound by both the splash and About.
- About shows: logo, name, tagline, **app version** (from the assembly), **copyright**, the author, and
  the **repository URL** with a button that opens it in the browser. Not the library version, not the
  build-environment status, not a license line.
- About is opened from an **icon-only button at the right end of the toolbar**.
- The quick start's **output folder** default is the **release folder's parent directory**.
- The **main exe** is captured with a **file picker rooted at the release folder**, and the shortcut
  target is the exe's path **relative to the release folder** (so a nested exe yields
  `[INSTALLDIR]\bin\App.exe`); an exe outside the release folder is an error shown in the dialog.
- The empty Control Panel block is **kept**: a new package with nothing filled there saves as
  `"controlPanel": {}`. The toggle alone decides whether the section is written; `ToPackage` is unchanged.
- The release phase **closes the outstanding Windows verification debt** (see PHASE03).
- Displayed name is **"Enigma.Msi"** with the tagline **"Declarative Windows MSI builder"** — not
  "Enigma.Msi Desktop", not the assembly name.
- **3-phase split**, as below.

### Defaults recorded at planning (low-impact, applied without asking)

- The splash behaves identically in Debug and Release.
- All new UI text is English and hard-coded — the app has no localization infrastructure.
- A failed browser launch is logged as a warning and swallowed; the repository URL is rendered as
  selectable text so it can always be copied by hand.
- The "Quick start" button sits **left of New**; the About button sits at the far right of the same row.
- The Control Panel expander keeps its current collapsed-by-default state; only the toggle inside it
  changes.
- `%ProgramMenu%` is created before `%Desktop%` (the order the request lists them in).
- The output folder falls back to the release folder itself when it has no parent (a drive root).

## PHASE01 — Splash screen & About dialog

**Status:** DONE
**Branch:** `feature/feature-74c4-phase01-splash-about`

All in `src/Enigma.Msi.Desktop` (+ its test project + the desktop guide). New files first, then the two
places that wire them in (`App`, `MainWindow`).

1. **`Assets/logo.png`** — extract the ICO's 256×256 entry verbatim and commit it. It is picked up
   automatically by the existing `<AvaloniaResource Include="Assets/**" />` glob, so no csproj change.
   Reproducible extraction (run once, from the repo root; no `System.Drawing`, no external tool):

   ```powershell
   $ico = "src\Enigma.Msi.Desktop\Assets\appicon.ico"
   $b = [System.IO.File]::ReadAllBytes($ico)
   $count = [BitConverter]::ToUInt16($b, 4)
   for ($i = 0; $i -lt $count; $i++) {
     $o = 6 + $i * 16
     $w = if ($b[$o] -eq 0) { 256 } else { $b[$o] }
     if ($w -ne 256) { continue }
     $size = [BitConverter]::ToUInt32($b, $o + 8)
     $off  = [BitConverter]::ToUInt32($b, $o + 12)
     [System.IO.File]::WriteAllBytes("src\Enigma.Msi.Desktop\Assets\logo.png", $b[$off..($off + $size - 1)])
   }
   ```

   Run it **inline** in the PowerShell tool — not through a nested `powershell.exe`, which is blocked.
   The `width == 0` branch is not defensive padding: 256 is encoded as 0 in an ICO directory entry.
   Then verify the written file independently before committing it — PNG magic (`89 50 4E 47`), IHDR
   width/height (bytes 16..23, big-endian) both 256, colour type (byte 25) `6` = RGBA — and check it
   against the values measured at planning time: **4491 bytes**, sourced from **offset 36932**, SHA-256
   identical to that byte range of the ICO. **If the frame turns out not to be PNG-encoded on inspection,
   stop and report** — do not fall back to a re-encode that changes the artwork.
2. **`Services/AppInfo.cs`** — one static, XML-documented source of identity, so the splash, the About
   dialog and any future consumer cannot disagree:
   - `Name` = `"Enigma.Msi"`, `Tagline` = `"Declarative Windows MSI builder"`,
     `Author` = `"Josué Clément"`, `Copyright` = `"© 2026 Josué Clément"`,
     `RepositoryUrl` = `"https://github.com/enigmalibs/Enigma.Msi"`.
   - `LogoUri` = `"avares://Enigma.Msi.Desktop/Assets/logo.png"` — verified to resolve; the folder-less
     `avares://Enigma.Msi.Desktop/logo.png` does **not**.
   - `Version` — read from `typeof(AppInfo).Assembly` (the app's own, not `GetExecutingAssembly()`'s
     caller-dependent answer) via `AssemblyInformationalVersionAttribute`, **truncated
     at the first `+`**: SourceLink is on in this repo, so the raw informational version carries a
     `+<commit-sha>` suffix that must never reach the UI. Fall back to
     `Assembly.GetName().Version?.ToString(3)` when the attribute is absent, and to `"unknown"` when both
     are. Computed once (static readonly), not per access.
3. **`Services/IUrlLauncherService.cs`** + `UrlLauncherService` — `Task<bool> LaunchAsync(string url)`,
   implemented with `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })`. It returns
   `false` (never throws) for the failure modes that are the OS's, not the app's — no browser
   association, a shell refusal — catching `Win32Exception`, `InvalidOperationException` and
   `ObjectDisposedException` only. A seam, not a static helper, so `AboutViewModel` is testable without
   spawning a browser.
4. **`ViewModels/SplashViewModel.cs`** — four read-only properties (`Name`, `Tagline`, `Version`,
   `Author`) sourced from `AppInfo`. No dependencies, no commands: the splash's only interaction is
   dismissal, which is the view's own concern.
5. **`Views/SplashWindow.axaml(.cs)`** — `x:DataType="vm:SplashViewModel"`, and the window header exactly
   as verified warning-free: **`WindowDecorations="None"`** (*not* the obsolete `SystemDecorations`),
   `ShowInTaskbar="False"`, `Topmost="True"`, `CanResize="False"`,
   `WindowStartupLocation="CenterScreen"`, ~480×280, `Background="{DynamicResource EnigmaSurfaceBrush}"`
   inside a 1 px `EnigmaBorderBrush` border. Content: the logo at ~96 px
   (`Source="avares://Enigma.Msi.Desktop/Assets/logo.png"` — the `Assets/` segment is required, the
   folder-less form does not resolve), `Name` (~28 px, SemiBold), `Tagline` (secondary foreground),
   `Version`, then `Author`.
   Code-behind owns dismissal and nothing else: a `DispatcherTimer` of **2 s** started on `Opened`,
   plus `PointerPressed` and `KeyDown` handlers. All three funnel through one `Dismiss()` that is
   **idempotent** (a guard flag, and the timer stopped) and raises `event EventHandler? Dismissed` exactly
   once. The window does **not** close itself — `App` owns the handover, because closing before the main
   window is shown would shut the app down.
6. **`ViewModels/AboutViewModel.cs`** — the `AppInfo` values as read-only properties, plus
   `OpenRepositoryCommand` (`AsyncRelayCommand`) calling `IUrlLauncherService.LaunchAsync(AppInfo.RepositoryUrl)`
   and logging a **warning** (injected `ILogger<AboutViewModel>`) when it returns `false`. Constructor
   null-guards both dependencies, as every ViewModel in this app does.
7. **`Views/AboutCard.axaml(.cs)`** — `UserControl`, `x:DataType="vm:AboutViewModel"`, sized to the
   dialog's content width: the logo at ~64 px beside the name/tagline, then version, copyright, author,
   then the repository URL in a `SelectableTextBlock` with a "View on GitHub" `Button` bound to
   `OpenRepositoryCommand`. No Close button of its own — the dialog supplies it.
8. **`Services/IAboutDialogService.cs`** + `AboutDialogService` — `Task ShowAsync()`. Wraps
   `IContentDialogService`: constructs an `AboutCard` with the injected `AboutViewModel` as its
   `DataContext`, and configures the dialog with `Title = "About"`, the content, `CloseButtonText =
   "Close"` and the `Info` glyph as `IconData`. Exactly the `BuildProgressService` shape — the service
   owns the control, the ViewModel never sees one. It **also sets the dialog's size explicitly in its own
   `configure`** (`DialogMaxWidth`, and `DialogWidth`/`DialogHeight` if wanted): the host is shared with
   the quick start and `ShowAsync` does not reset those six properties, so a dialog that does not state
   its size inherits whatever the previous one left behind.
9. **`ViewModels/MainWindowViewModel.cs`** — inject `IAboutDialogService` (null-guarded, documented in
   the constructor's XML docs like its eight siblings) and add
   `[RelayCommand] private Task ShowAboutAsync() => _aboutDialog.ShowAsync();`.
10. **`Views/MainWindow.axaml`** — the toolbar `Border`'s single horizontal `StackPanel` becomes a
    `Grid ColumnDefinitions="*,Auto"`: the existing `StackPanel` (unchanged, including the
    incomplete-package warning) in column 0, and the new icon-only About `Button` in column 1 so it sits
    hard right. `ToolTip.Tip="About Enigma.Msi"`, `<ei:Icon Kind="Info" />` (the same member the Control
    Panel expander already uses), bound to `ShowAboutCommand`.
11. **`App.axaml.cs`** — the splash-first handover, replacing the direct `desktop.MainWindow = mainWindow`
    assignment. Order is load-bearing:
    1. Resolve `MainWindow` + its ViewModel and register the three hosts and two storage providers
       **exactly as today** — a service whose host is unregistered throws the moment a ViewModel asks it
       for anything, and the quick start's startup open will ask early.
    2. Resolve `SplashWindow` (+ its ViewModel), assign `desktop.MainWindow = splash`. The lifetime
       auto-shows whatever `MainWindow` holds when this method returns, so the splash needs no `Show()`
       call of its own and is the first visible window.
    3. On `splash.Dismissed`, in **exactly this order**:
       1. `desktop.MainWindow = mainWindow;`
       2. `mainWindow.Show();`  ← required: only the *initial* `MainWindow` is auto-shown
       3. `splash.Close();`

       The order is load-bearing, but not for the reason one would guess: the default `ShutdownMode` is
       **`OnLastWindowClose`**, a *last window standing* rule rather than a MainWindow-identity rule. If
       the splash closes before another window is open, the app exits. **Do not set
       `ShutdownMode = OnMainWindowClose`** — reassigning `MainWindow` and showing the real window before
       closing the splash is the whole fix, and it leaves the app's exit behaviour untouched.
    4. `BuildDesignerServices()` is untouched; the designer never runs `Main` and never shows a splash.
12. **`ServiceCollectionExtensions.cs`** — register `IUrlLauncherService`, `IAboutDialogService` and
    `AboutViewModel` as **singletons** beside the existing UI seams, and `SplashWindow` + `SplashViewModel`
    as **transient** (shown once at startup; a singleton would keep a closed window alive for the
    process's life). Update the XML doc comments that enumerate what each extension registers.
13. **Tests** (`tests/Enigma.Msi.Desktop.UnitTests`, following the suite's existing naming and NSubstitute
    style):
    - `AppInfoTests`: the five constants have the exact expected values; `Version` is non-empty and
      contains no `+` (the SourceLink suffix is stripped); `LogoUri` is a well-formed `avares://` URI
      naming the app assembly.
    - `AboutViewModelTests`: constructor null-guards; `OpenRepositoryCommand` calls the launcher **once**
      with exactly `AppInfo.RepositoryUrl`; a `false` return is swallowed (no throw) and does not fault
      the command.
    - `MainWindowViewModelTests`: `ShowAboutCommand` delegates to the substituted
      `IAboutDialogService`; the new constructor argument is null-guarded, in the shape of the existing
      null-guard test. **Blast radius, measured:** the ViewModel is constructed in exactly two places in
      that file — the null-guard test and the private `CreateViewModel()` helper that 30 tests call — and
      its current constructor arity is 9. Both places must be updated in the same edit, or all 31 tests
      fail to compile. Follow the `IBuildProgressService` precedent: app-owned interface in
      `Desktop/Services`, injected second-to-last, `readonly` NSubstitute field. **No test enumerates the
      command surface**, so adding toolbar buttons breaks nothing — but one test asserts
      `_buildProgress.DidNotReceive().ShowAsync(...)`, i.e. the toolbar must still not reintroduce a
      cancel affordance.
    - No test for `SplashWindow` or the handover — both are view/lifetime concerns verified manually
      (see acceptance criteria).
14. **Guide** (`docs/guides/desktop-app.md`): a short section on the splash (what it shows, how to skip
    it) and on About (what it reports, where the button is).

**Acceptance criteria**

- `Assets/logo.png` is committed, is a 256×256 PNG with an alpha channel, and is byte-identical to the
  ICO's 256×256 entry.
- Launching the app shows **only** the splash — the main window does not appear behind or beside it —
  with logo, "Enigma.Msi", the tagline, the version and "Josué Clément"; it disappears after ~2 s, or
  immediately on a click or key press, and the main window then appears. Closing the app from the main
  window still exits the process (no orphaned splash, no lingering process).
- The version shown matches `<Version>` in the csproj and carries no commit-hash suffix.
- The toolbar's right end carries an icon-only About button; it opens a modal dialog showing logo, name,
  tagline, version, copyright, author and the repository URL; "View on GitHub" opens
  `https://github.com/enigmalibs/Enigma.Msi` in the default browser; Close and Escape both dismiss it.
- Both themes render the splash and the dialog legibly (no hard-coded colours; every brush a
  `{DynamicResource Enigma…Brush}`). The brush keys were confirmed from the control library's source, not
  from the packed avares blob, so **check the first run's Avalonia trace output for unresolved
  `DynamicResource` keys** rather than trusting that they resolved.
- `dotnet build Enigma.Msi.slnx -c Release` zero warnings (**including** the `AVLN` count on every
  project that compiles XAML); `dotnet test --solution Enigma.Msi.slnx -c Release` fully green including
  the new tests.

## PHASE02 — Quick-start dialog & Control Panel default

**Status:** TODO
**Branch:** `feature/feature-74c4-phase02-quick-start`

The one-page quick start, its apply logic, and the Control Panel default. Ordered so the model-facing
logic and its tests exist before the dialog that drives them.

1. **Control Panel on by default** (`ViewModels/PackageEditorViewModel.cs`): `Reset()` sets
   `HasControlPanelInfo = true`. Nothing else changes — `ToPackage()` keeps writing the section iff the
   toggle is on, so an untouched new package now serializes `"controlPanel": {}`, which is what the
   toggle honestly reports.
   **`LoadFrom` must NOT be touched.** It derives the toggle from `package.ControlPanel is not null`, so
   opening a 1.0.0/1.1.0 profile without the section still shows the toggle off — which is correct, and
   guarded: "default on" is about *new* packages, not about rewriting what a profile said. Changing that
   line would break `LoadFrom_WithoutOptionalBlocks_SwitchesThemOff`, and the breakage would be telling
   the truth.
   **Verified: no existing test fails from this change** — no test asserts the property on a fresh or
   reset ViewModel today. So this is not a test-fixing step but a test-*adding* one: pin the new default
   with a fresh assertion in the existing reset test.
2. **`PackageEditorViewModel.HasData`** — a computed property answering "would applying the quick start
   throw work away?". True when any of `AppName`, `Manufacturer`, `InstallPath`, `ReleasePath`,
   `OutputPath`, `MsiFilename`, or any Control Panel field is non-blank, or `Shortcuts.Count > 0`, or
   `Ui.IsCustomized`. Deliberately **ignores** `Version`, `ProductId` and `UpgradeCode`: `Reset()` fills
   all three, so counting them would make an untouched new package look like unsaved work.
3. **`ViewModels/QuickStartSettings.cs`** — a public sealed record carrying the six validated answers:
   `AppName`, `Version`, `Manufacturer`, `ReleasePath`, `IconPath`, `ExecutableRelativePath`. It is the
   contract between the dialog and the form; the relative path (not the absolute one) is what crosses,
   because that is what the shortcut target needs.
4. **`PackageEditorViewModel.ApplyQuickStart(QuickStartSettings settings)`** — null-guarded, and the only
   new public method on the form. It calls `Reset()` (fresh identifiers, everything back to defaults),
   then:
   - `AppName`, `Version`, `Manufacturer`, `ReleasePath` from the settings;
   - `InstallPath` = `%ProgramFiles%\` + the trimmed `AppName`;
   - `OutputPath` = the release folder's parent (`Path.GetDirectoryName` of the full path with trailing
     separators trimmed), falling back to the release folder itself when there is no parent;
   - `MsiFilename` = `AppName` sanitized for the validator's rule: every
     `Path.GetInvalidFileNameChars()` removed, trimmed, a trailing `.msi` stripped
     (`OrdinalIgnoreCase`), and `"package"` if nothing survives;
   - `HasControlPanelInfo` = `true`, `ProductIcon` = `IconPath`;
   - two shortcuts appended in order — `%ProgramMenu%` then `%Desktop%` — each with
     `ShortcutName` = `AppName`, `TargetPath` = `[INSTALLDIR]\` + `ExecutableRelativePath`,
     `IconPath` = `IconPath`, and no arguments. Added through the existing `Shortcuts` collection so the
     established `OnShortcutsChanged` subscription bookkeeping wires them like any other row.
5. **`IPathPickerService.PickExecutableAsync(string? startLocation)`** (+ `PathPickerService`) — the fifth
   path question, implemented exactly like `PickIconAsync`, verified signature and argument order:

   ```csharp
   private static readonly IReadOnlyList<FilePickerFileType> ExecutableFileTypes =
       [new FilePickerFileType("Executable") { Patterns = ["*.exe"] }];

   IEnumerable<string> paths = await _fileDialogService
       .ShowOpenFileDialogAsync(
           "Select executable", false, ExistingDirectoryOrEmpty(startLocation), string.Empty, ExecutableFileTypes)
       .ConfigureAwait(true);

   return paths.FirstOrDefault();
   ```

   Positional order is `(title, allowMultiple, suggestedStartLocation, suggestedFileName, fileTypeFilter)`.
   Reuse the existing private `ExistingDirectoryOrEmpty` helper rather than adding a second guard. Claim
   nothing stronger about a blank or missing start location than "unchanged from the existing pickers" —
   they already pass `string.Empty` unconditionally, and the underlying storage-provider behaviour for an
   empty path was not provable at planning time. XML docs in the interface's established style; the
   interface's own summary (which counts the questions) updated from four to five.
6. **`ViewModels/QuickStartViewModel.cs`** — the dialog's form:
   - `[ObservableProperty]` `AppName`, `Version` (initialized to `PackageEditorViewModel.DefaultVersion`),
     `Manufacturer`, `ReleasePath`, `IconPath`, `ExecutablePath` (the absolute path as picked).
   - Three browse commands over `IPathPickerService`: the release folder (`PickFolderAsync`), the icon
     (`PickIconAsync(ReleasePath)`), the executable (`PickExecutableAsync(ReleasePath)`). The icon and
     executable commands are gated on a non-blank `ReleasePath` (`CanExecute`), so the picker always opens
     where the payload is — and the field order in the dialog follows that dependency.
   - `ExecutableRelativePath` — computed: `ExecutablePath` made relative to `ReleasePath` when it is under
     it (ordinal, case-insensitive, separator-normalized), otherwise `null`.
   - `ExecutableError` — the message shown in the dialog when `ExecutablePath` is set but not under the
     release folder ("The executable must be inside the release folder — it is what `[INSTALLDIR]`
     becomes after installation."), rendered in `{DynamicResource EnigmaWarningBrush}`.
   - `CanApply` — all six fields non-blank, `Version` parses as a `System.Version`, and
     `ExecutableRelativePath` is non-null. **String and parse rules only — no disk access**, mirroring
     the split the app already lives by: `IsPackageValid` runs the in-memory rules on every keystroke and
     leaves the file-system rules to Validate/Build. Existence is therefore reported by the main form's
     Problems pane, in one place, rather than half-reported here.
   - `ToSettings()` → `QuickStartSettings` (call only when `CanApply`).
   - Property changes re-raise `CanApply`/the Apply command's `CanExecuteChanged` through
     `[NotifyPropertyChangedFor]`/`[NotifyCanExecuteChangedFor]`.
7. **`Views/QuickStartCard.axaml(.cs)`** — `UserControl`, `x:DataType="vm:QuickStartViewModel"`, one
   column of `editors:TextEditor`s in the main form's idiom (title, `PlaceholderText`, and a `Browse…`
   `Button` in `ActionContent` where a picker applies): App name, Version, Manufacturer, Release folder,
   Icon, Main exe — plus the `ExecutableError` text and a one-line hint under the exe field explaining
   that its path becomes `[INSTALLDIR]\…`.
   **Initial focus is required, not cosmetic**: nothing focuses a dialog's content on open, and Escape
   only fires while focus is inside the dialog — so the card focuses its App-name editor on load, or
   Escape silently does nothing. There is also no Enter-to-commit (`DefaultButton` is intent-only), so
   Apply must be reachable by mouse/Tab.
   **Size**: set `DialogMaxWidth` (the default is 600, and the card wants more) **in the service's own
   `configure`** — the host is shared with the About dialog and its size properties are not reset between
   shows.
8. **`Services/IQuickStartDialogService.cs`** + `QuickStartDialogService` —
   `Task<QuickStartSettings?> ShowAsync()`. Builds a **fresh** `QuickStartViewModel` per call (injected
   `Func<QuickStartViewModel>` or a transient resolved per call — never a singleton, or the second run
   opens with the first run's answers), hosts it in a `QuickStartCard`, shows it through
   `IContentDialogService` with `Title = "Quick start"`, `PrimaryButtonText = "Apply"`,
   `CloseButtonText = "Cancel"`, and returns the settings only when the result is
   **`DialogResult.Primary`** *and* `CanApply` — `null` otherwise. `Primary` is the only value that counts
   as confirmation: Escape and a scrim click both yield `None`, not `Close`.
   **Apply gating, and the leak it must avoid.** `ContentDialog.IsPrimaryButtonEnabled` *is* a
   `StyledProperty<bool>` and therefore bindable — bind it to `CanApply` inside `configure`. But the
   service owns **one shared host** and resets that property by plain assignment, never `ClearValue`, so a
   binding left installed stays live under the *next* dialog and would gate the About dialog's button on a
   dead ViewModel. Keep the `IDisposable` that `Bind(...)` returns and **dispose it in a `finally` around
   the `await`** — `ShowAsync` completes exactly when the dialog closes, which makes that the correct and
   only disposal site. (The alternative, an Apply button inside the card, avoids the binding but gives up
   `DialogResult` and needs an out-of-band result channel; it is the fallback, not the plan.)
9. **`ViewModels/MainWindowViewModel.cs`**:
   - Inject `IQuickStartDialogService` (null-guarded, XML-documented).
   - `[RelayCommand] private async Task QuickStartAsync()`: show the dialog; if it returns `null`, do
     nothing at all. Otherwise, **if `Package.HasData`**, ask for confirmation through
     `IContentDialogService` ("Replace the current package?" / "The package in the form will be replaced.
     Unsaved changes are lost." / Yes+No) and abort unless the result is `DialogResult.Primary`. Then
     `Package.ApplyQuickStart(settings)`, clear `ValidationErrors`, reset `CurrentFilePath` to `null`,
     set a status message, and log the apply at Information. **The confirmation deliberately comes after
     the dialog, not before**: a quick start the user cancels must never have cost them a prompt.
   - `[RelayCommand] private async Task ShowQuickStartOnStartupAsync()`: a once-only guard (a private
     bool), and a no-op unless `!Package.HasData && CurrentFilePath is null`. Delegates to the same apply
     path as the command above (which, on an empty form, will never prompt).
10. **`Views/MainWindow.axaml(.cs)`** — a "Quick start" `Button` (`<ei:Icon Kind="MagicWand" />` + text;
    `Sparkle` and `Rocket` are the verified alternatives) as the
    **first** item of the toolbar's left `StackPanel`, before New, with
    `ToolTip.Tip="Fill the form from six answers"`. Code-behind adds one `Opened` handler that invokes
    `ShowQuickStartOnStartupCommand` — the window's own lifecycle event is the only honest trigger, since
    the ViewModel has no notion of being shown.
11. **Tests** (`tests/Enigma.Msi.Desktop.UnitTests`):
    - `PackageEditorViewModelTests`: a **new** assertion pinning `Reset()` leaving `HasControlPanelInfo`
      **true** (no existing assertion contradicts it — verified), and the existing
      `LoadFrom_WithoutOptionalBlocks_SwitchesThemOff` left untouched and passing, which is what proves
      the default did not leak into the load path; `ApplyQuickStart` sets the four entered fields,
      derives `InstallPath`/`OutputPath`/`MsiFilename` exactly as specified (including the drive-root
      fallback and a name needing sanitization), switches the Control Panel section on with the icon, and
      appends **exactly two** shortcuts in `%ProgramMenu%`-then-`%Desktop%` order with the right names,
      `[INSTALLDIR]\…` targets (including a nested exe) and icons; it regenerates `ProductId`/`UpgradeCode`
      and clears anything previously in the form; null argument throws `ArgumentNullException`.
      `HasData` is false on a fresh reset and true for each contributing field/collection.
    - `QuickStartViewModelTests`: `CanApply` false while any field is blank, false for an unparseable
      version, false when the exe is outside the release folder (with `ExecutableError` set), true for a
      complete valid set; `ExecutableRelativePath` for a root-level and a nested exe, and for differing
      separator/case; the three browse commands assign from the substituted `IPathPickerService` and pass
      `ReleasePath` as the start location; the icon/exe commands cannot execute without a release folder;
      `ToSettings()` carries the relative path, not the absolute one.
    - `MainWindowViewModelTests`: the quick-start command shows the dialog exactly once; a `null` result
      changes nothing (no confirmation asked, form untouched); a result on an **empty** form applies
      **without** a confirmation dialog; a result on a form **with data** asks for confirmation and
      applies only on `Primary` (a `None`/`Close` result leaves the form untouched); the startup command
      opens the dialog once and never again, and not at all when the form has data or a file is open;
      the new constructor argument is null-guarded.
    - `PathPickerService` gains no test (it is the untestable edge over the storage provider — the reason
      the seam exists); the new interface member is exercised through the substituted seam.
12. **Guide** (`docs/guides/desktop-app.md`): a "Quick start" section — the six fields, what it derives,
    that it replaces the form (with confirmation), and that it opens by itself on an empty form; plus a
    note that Control Panel information is now on by default for new packages.

**Acceptance criteria**

- A new package starts with the Control Panel toggle **on**; saving it produces `"controlPanel": {}`;
  opening a 1.0.0/1.1.0 profile with no such section still shows the toggle off. Existing profiles in
  `msiProfiles/` open unchanged.
- The quick start opens from the toolbar, and once by itself on a freshly launched (empty) app.
- Apply stays disabled until all six fields are filled, the version parses, and the exe is inside the
  release folder; an exe outside it shows the error message.
- Given six valid answers, Apply yields a form that **passes Validate with zero problems and enables
  Build** with no further typing — verified end to end against a real published folder: install path
  `%ProgramFiles%\<AppName>`, output folder = the release folder's parent, MSI name = the app name, the
  product icon set, and two shortcuts (`%ProgramMenu%`, `%Desktop%`) named after the app, targeting
  `[INSTALLDIR]\<exe>` with the icon.
- Cancelling the dialog changes nothing and asks nothing. Applying over a form with data asks first, and
  declining leaves the form exactly as it was.
- The build log invariant, the append-only rule and the no-new-dependency rule all hold.
- `dotnet build Enigma.Msi.slnx -c Release` zero warnings (`AVLN` included);
  `dotnet test --solution Enigma.Msi.slnx -c Release` fully green including the new tests.

## PHASE03 — Release prep: desktop app 1.2.0

**Status:** TODO
**Branch:** `feature/feature-74c4-phase03-release-1-2-0`

Mirrors FEATURE-6C35 PHASE02 for the next version, **plus** the Windows verification that 1.1.0 could not
run. **App-only release:** no `dotnet pack`, no NuGet push, no library version change.

1. `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`: `<Version>` **1.2.0**.
2. `msiProfiles/Enigma.Msi.Desktop.1.2.0.msipkg.json` — clone of the 1.1.0 profile with: `version`
   1.2.0; a **fresh `productId`** from a real generator (`[guid]::NewGuid()` — never hand-fabricated);
   **`upgradeCode` reused verbatim** (`3405046f-527a-439e-a22f-866247dc8314` — the app's permanent
   identity, and what makes 1.2.0 *upgrade* an installed 1.1.0 instead of installing beside it);
   everything else unchanged (`releasePath` stays the win-x64 **publish** output). The 1.0.0 and 1.1.0
   profiles are kept.
3. Verify the new profile deserializes through `MsiPackageJson` and passes `IMsiPackageValidator`'s
   in-memory rules with zero errors (no worker/WixSharp invocation needed for this step).
4. `RELEASENOTES.md`: a **1.2.0** section on top (newest-first, as the document already is) covering the
   splash, About, the quick start and the Control Panel default, restating that the **Enigma.Msi library
   remains 1.0.0** and that the profile format is unchanged (`schemaVersion` 1, interchangeable in both
   directions). Update the current-version table. The 1.1.0 and 1.0.0 sections stay as history.
5. **Windows verification pass — the debt 1.1.0 left.** PHASE02 of FEATURE-6C35 ran on Linux, where the
   `net472` worker cannot start and there is no WiX CLI, leaving 32 Windows-only `Enigma.Msi.UnitTests`
   cases and the whole `Enigma.Msi.Worker.UnitTests` suite (mapping + WixSharp enum **drift guards**)
   unexercised since before FEATURE-6C35 PHASE01. Run, from the repo root, and record the real numbers:
   - `dotnet build Enigma.Msi.slnx -c Release` — zero warnings, `AVLN` counts read per project.
   - `dotnet test --solution Enigma.Msi.slnx -c Release` — the whole solution, all four suites. Report
     the total and confirm the previously-skipped Windows-only cases and the worker suite actually ran.
     **A drift-guard failure is a real finding, not a flake**: if one fires, stop and report it — it means
     a WixSharp member set moved, and it must be resolved before the release closes.
6. **Dogfood MSI build** (manual acceptance — Windows + WiX CLI; this machine has WiX **7.0.0**, the same
   version that built the 1.0.0 installer), per `docs/RELEASE.md` §7 from the repo root:
   `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false`,
   then the worker CLI build of `msiProfiles/Enigma.Msi.Desktop.1.2.0.msipkg.json`. Record the MSI path
   and size in the completion doc — never silently skipped. Install/launch/uninstall verification stays
   with the maintainer.
7. **Documentation freshness.** Review and update as needed:
   - `docs/guides/desktop-app.md` — must match the shipped 1.2.0 UI (splash, About, quick start, CP
     default) after PHASE01/02 already touched it.
   - `CLAUDE.md`'s build-state block — add FEATURE-74C4, and **correct the stale claim that the
     repository has "no git remote and no tags"**: `origin` is
     `https://github.com/enigmalibs/Enigma.Msi.git` and tags `1.0.0` and `1.1.0` exist. Also retire the
     "Outstanding, and Windows-only" paragraph once step 5 and 6 have actually run.
   - `docs/RELEASE.md` — check for 1.0.0/1.1.0-specific staleness; note in the completion doc that an
     app-only release runs no library tag/pack/push, and that whether and how to tag an app-only release
     (e.g. an app-scoped tag) is the maintainer's choice. **Nothing outward-facing is ever run from
     here** — no tag, no push, no publish.
8. `README.md` — check whether the app's feature list needs the two new user-facing features; update only
   if it already describes the app at that level of detail.

**Acceptance criteria**

- `<Version>` is 1.2.0; the 1.2.0 release-notes section and the version table are in place; the 1.2.0
  profile is committed with a fresh `productId` and the verbatim `upgradeCode`, deserializes, and reports
  zero in-memory validation errors.
- The full solution builds warning-free and the **entire** test suite passes **on Windows**, with the
  previously unexercised Windows-only library cases and the whole worker suite (drift guards included)
  confirmed as run.
- The 1.2.0 dogfood MSI is built from the committed profile and recorded (path + size), or explicitly
  blocked with the reason.
- `CLAUDE.md`, the desktop guide and `docs/RELEASE.md` describe the repository as it actually is after
  this phase — including the corrected remote/tags statement.
- The library is still at 1.0.0 and nothing was packed, tagged or pushed.
