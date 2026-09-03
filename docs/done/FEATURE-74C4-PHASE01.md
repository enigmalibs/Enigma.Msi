# FEATURE-74C4 — PHASE01 — Splash screen & About dialog

**Status:** DONE
**Branch:** `feature/feature-74c4-phase01-splash-about`
**Plan:** `docs/plan/FEATURE-74C4.md` (PHASE01)

## Summary

The desktop app now has a visual identity. Two additions, both app-only:

- **A splash screen.** `SplashWindow` — undecorated, 480×280, centred, topmost, out of the taskbar —
  shows the logo, `Enigma.Msi`, the tagline, the version and the author for about two seconds. Any click
  or key press dismisses it early. It is the **initial** `MainWindow`, which is what makes it the only
  window on screen: the desktop lifetime auto-shows whatever `MainWindow` holds when
  `OnFrameworkInitializationCompleted` returns, so the real window never flashes behind it.
- **An About dialog.** An icon-only button at the far right of the command bar opens the control
  library's shared `ContentDialog` carrying an `AboutCard`: the logo beside the name and tagline, then
  version, copyright, author, a separator, the repository URL as selectable text, and a **View on GitHub**
  button. Close and Escape both dismiss it.

`AppInfo` is the single source for every identity string, so the two cannot disagree. Its `Version` is
read from the app assembly's `AssemblyInformationalVersionAttribute` **truncated at the first `+`** — the
SDK appends the source revision id, which belongs in a crash report and not in a window.

Two seams were added in the shape of the existing `IPathPickerService` / `IUiDispatcher` /
`IBuildProgressService` trio, so no ViewModel constructs a control and the suite can substitute both:
`IUrlLauncherService` (shell → browser) and `IAboutDialogService` (owns the card, wraps
`IContentDialogService`).

**The handover order in `App` is load-bearing** and is commented as such. The default `ShutdownMode` is
`OnLastWindowClose` — a *last window standing* rule, not a MainWindow-identity rule — so the sequence is
`MainWindow = mainWindow` → `mainWindow.Show()` → `splash.Close()`. Closing the splash first would exit
the process; reassigning `MainWindow` alone would show nothing, because only the *initial* `MainWindow` is
auto-shown. `ShutdownMode` was deliberately **not** changed to `OnMainWindowClose`.

`Assets/logo.png` is a **verbatim byte copy** of the ICO's 256×256 entry — no re-encode, no
`System.Drawing`, no external tool.

## Files touched

**Created**

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/Assets/logo.png` | 4 491-byte PNG, extracted byte-for-byte from `appicon.ico` |
| `src/Enigma.Msi.Desktop/Services/AppInfo.cs` | The identity strings + the assembly version |
| `src/Enigma.Msi.Desktop/Services/IUrlLauncherService.cs` | Seam: open a URL |
| `src/Enigma.Msi.Desktop/Services/UrlLauncherService.cs` | `Process.Start` + `UseShellExecute` |
| `src/Enigma.Msi.Desktop/Services/IAboutDialogService.cs` | Seam: show About |
| `src/Enigma.Msi.Desktop/Services/AboutDialogService.cs` | Owns the card, wraps `IContentDialogService` |
| `src/Enigma.Msi.Desktop/ViewModels/SplashViewModel.cs` | Four read-only properties, no dependencies |
| `src/Enigma.Msi.Desktop/ViewModels/AboutViewModel.cs` | Identity + `OpenRepositoryCommand` |
| `src/Enigma.Msi.Desktop/Views/SplashWindow.axaml(.cs)` | The splash; code-behind owns dismissal only |
| `src/Enigma.Msi.Desktop/Views/AboutCard.axaml(.cs)` | The dialog's content |
| `tests/Enigma.Msi.Desktop.UnitTests/Services/AppInfoTests.cs` | 4 tests |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/AboutViewModelTests.cs` | 5 tests |

**Modified**

| Path | What |
|---|---|
| `src/Enigma.Msi.Desktop/App.axaml.cs` | The splash-first handover, replacing the direct `MainWindow` assignment |
| `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs` | Two services + `AboutViewModel` as singletons; the splash pair transient |
| `src/Enigma.Msi.Desktop/ViewModels/MainWindowViewModel.cs` | `IAboutDialogService` injected second-to-last; `ShowAboutCommand` |
| `src/Enigma.Msi.Desktop/Views/MainWindow.axaml` | Toolbar `StackPanel` → `Grid ColumnDefinitions="*,Auto"` + the About button |
| `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/MainWindowViewModelTests.cs` | Both construction sites updated (arity 9 → 10); 2 tests added |
| `docs/guides/desktop-app.md` | A splash section, and the About button in *The window* |
| `docs/roadmap.md`, `docs/plan/FEATURE-74C4.md` | Statuses |

No csproj change: `logo.png` is picked up by the existing `<AvaloniaResource Include="Assets/**" />`
glob. `Directory.Packages.props` untouched — no new dependencies.

## Deviations & follow-ups

- **`AboutCard` is `Focusable="True"` and takes focus on attach.** Not spelled out in the plan's step 7,
  but required by two of its own statements: the plan records that Escape only fires while focus is inside
  the dialog and that nothing focuses the content on open, and the acceptance criteria require Escape to
  dismiss. The focus call is posted at `DispatcherPriority.Input` rather than made inline, because at
  attach time the dialog is not on screen yet. Verified: Escape dismisses (see evidence).
- **Two brushes raised from tertiary to secondary** after looking at the rendered result: the splash's
  version and author lines, and the About card's repository URL. The plan specifies a brush only for the
  tagline, leaving these open. At tertiary the URL was the faintest thing on the card, which defeats its
  stated purpose — it is the fallback to copy by hand when no browser opens. Still `{DynamicResource}`,
  still subordinate (12 px).
- **`SplashWindow.Duration` is public** (`static readonly TimeSpan`, 2 s) rather than a private constant,
  so the value is discoverable next to the window it governs.
- **No test for `SplashWindow` or the handover**, per the plan — both are view/lifetime concerns. They
  were instead verified against the running app (see below), which goes beyond what the plan asked.
- **Documentation sweep:** `CLAUDE.md`'s "Outstanding, and Windows-only" paragraph claimed the 32
  Windows-only library cases and the whole worker suite were unexercised. This dev ran them (392 passed,
  0 skipped), so that half was corrected here. Adding `FEATURE-74C4` to the build-state block was left to
  `PHASE03`, which the plan assigns it to. `README.md` needed nothing — it mentions the app in one line
  and does not describe it at feature level.
- **Line endings:** no CRLF churn observed in this dev; nothing to recommend.
- Nothing in the worker, the library, the model or the `.msipkg.json` contract was touched, so 1.0.0 and
  1.1.0 profiles remain interchangeable in both directions.

## Build & test evidence

```
dotnet build Enigma.Msi.slnx -c Release      →  Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test --solution Enigma.Msi.slnx -c Release
                                             →  total: 392   failed: 0   succeeded: 392   skipped: 0
```

Zero warnings includes the `AVLN` XAML count on both projects that compile `.axaml` — those are printed
rather than promoted by `TreatWarningsAsErrors`, so the count was read, not inferred. `WindowDecorations`
is used, never the obsolete `SystemDecorations`. 392 total, up from 381: **11 new tests** (4 `AppInfoTests`,
5 `AboutViewModelTests`, 2 in `MainWindowViewModelTests`), with all previously Windows-only cases and the
whole worker suite running on this pass.

**`Assets/logo.png` verified independently of the extraction** — PNG magic `89 50 4E 47 0D 0A 1A 0A`,
`IHDR` 256×256, bit depth 8, colour type **6 (RGBA)**, non-interlaced, `IEND` terminator, 4 491 bytes, and
SHA-256 `7DDFFF22…F40D9E55` identical to bytes `[36932, 41423)` of `appicon.ico`. Byte-identical to the
ICO's 256×256 entry, as the plan requires.

**The running app was driven and observed** (Windows 11, Release build), which covers the plan's manual
acceptance criteria:

| Criterion | Evidence |
|---|---|
| Only the splash is on screen while it is up | Top-level window enumeration sampled every 300 ms: `t=1.3s…3.5s` → exactly **1** visible window, `'Enigma.Msi' 480x280` |
| The main window then appears | `t=3.8s` → exactly **1** visible window, `'Enigma.Msi — new package' 1216x839`; no lingering splash |
| A click or key press dismisses it early | `WM_KEYDOWN`/`VK_SPACE` posted at `t=1.47s`; main window up by `t=1.97s`, well before the 2 s timer |
| Closing the app exits the process | `WM_CLOSE` on the main window → process exited, **0** leftover `Enigma.Msi.Desktop` processes, stderr empty |
| The version carries no commit-hash suffix | The dialog reports **`Version 1.1.0`**, matching `<Version>` in the csproj exactly |
| About shows logo/name/tagline/version/copyright/author/URL | UI Automation over the open dialog found `Declarative Windows MSI builder`, `Version 1.1.0`, `© 2026 Josué Clément`, `Josué Clément`, `https://github.com/enigmalibs/Enigma.Msi`, `View on GitHub` |
| Close **and** Escape dismiss it | Open → Escape → gone; reopen → Close (invoked via UI Automation) → gone |
| Both render legibly, every brush a `DynamicResource` | Screenshots of the splash and the dialog inspected; every glyph legible, no unset or transparent text, logo alpha correct. No hard-coded colour in either file (`grep` for `#`, `Color=`, `Brushes.` → none) |

**Not verified here, and left to the maintainer:** the light theme. The app follows the OS variant and this
machine is in dark mode; switching the OS theme was outside this dev. The keys used
(`EnigmaSurfaceBrush`, `EnigmaBorderBrush`, `EnigmaBorderSubtleBrush`, `EnigmaForegroundSecondaryBrush`)
are recorded in the plan as existing in both variants, and every one of them resolved in dark. Worth
knowing for next time: a static grep of the shipped `Enigma.Avalonia.Desktop.dll` **cannot** confirm a
theme key — the avares blob is deflate-compressed, so key names are not findable as strings, and
rendering is the only check.
