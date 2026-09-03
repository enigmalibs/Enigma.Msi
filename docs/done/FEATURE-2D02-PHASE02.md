# FEATURE-2D02-PHASE02 — Splash screen restyle & headless tests

**Status:** DONE
**Branch:** `feature/feature-2d02-phase02-splash-restyle`

## Summary

The splash screen now shows the logo, **Enigma.Msi** and `Version 1.2.0` — and nothing else — at
420×260 inside a subtle border. The tagline and the author line are gone from it; both stay in the About
dialog, which is where there is time to read them. The window's behaviour is untouched: the same 2 s
timer, the same click-or-key dismissal, the same `App`-owned handover to the main window.

Two structural consequences of "nothing else":

- **`SplashViewModel` is deleted.** It existed to forward four constants off `AppInfo`; with two lines
  left, the markup reads them directly with `{x:Static services:AppInfo.…}`. That removes the
  `x:DataType`, the `vm:` namespace, the DI registration and the `DataContext` assignment in
  `App.axaml.cs` — the splash is now a window with no ViewModel and no DataContext at all.
- **The repository gains its first headless-Avalonia suite.** With nothing to inject there is nothing a
  ViewModel test could assert about the splash, so the assertions run against the *real markup* on
  Avalonia's headless platform: `Avalonia.Headless` + `Avalonia.Headless.XUnit` 12.1.1, pinned inside
  the coupled Avalonia block so they can never drift from the Avalonia they drive.

The headless session builds the app's **real `App`**, not a stand-in. Every colour on the splash is a
`DynamicResource` `Enigma*` key resolved out of the `FluentTheme` + `Enigma.Avalonia.Desktop` dictionary
pair that `App.axaml` merges in that order, so a copy of that resource surface in the test project would
be one more thing to keep in step. Nothing of the app's composition runs: the test session sets the
application up without a lifetime, so `OnFrameworkInitializationCompleted` finds no
`IClassicDesktopStyleApplicationLifetime` and resolves no host, no windows and no services.

## Files/modules touched

**Modified — app**

- `src/Enigma.Msi.Desktop/Views/SplashWindow.axaml` — 480×280 → **420×260** (and the `d:Design*` pair
  with it); `x:DataType` and the `vm:` namespace dropped for `xmlns:services` + `{x:Static}`; the name at
  `FontSize` 18 / `SemiBold` (from 28); the version as a horizontal `StackPanel` with `Spacing="5"`
  holding a literal `Version` label and `AppInfo.Version`, both in `EnigmaForegroundSecondaryBrush`;
  outer `Spacing` 10 → 12; border brush `EnigmaBorderBrush` → **`EnigmaBorderSubtleBrush`**; the tagline
  and author `TextBlock`s **removed**. `Background`, `CanResize`, `ShowInTaskbar`, `Topmost`,
  `WindowDecorations` and `WindowStartupLocation` unchanged. Carries the "no ViewModel, and why" comment.
- `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs` — `AddTransient<SplashViewModel>()` removed
  (`AddTransient<SplashWindow>()` stays); the `<remarks>` now speaks of the splash *window* rather than
  "the splash pair", and says where its two lines come from.
- `src/Enigma.Msi.Desktop/App.axaml.cs` — the `splash.DataContext = …` line removed, with a one-line
  comment in its place. The `Dismissed` subscription, `desktop.MainWindow = splash` and `ShowMainWindow`
  are untouched.

**Deleted**

- `src/Enigma.Msi.Desktop/ViewModels/SplashViewModel.cs` — nothing references it; a repo-wide grep
  outside `docs/done/` and `docs/plan/` returns no hits.

**Added — tests**

- `tests/Enigma.Msi.Desktop.UnitTests/HeadlessTestApp.cs` — the assembly-level
  `[AvaloniaTestApplication]` hook and its `BuildAvaloniaApp()`: `AppBuilder.Configure<App>()` with
  `.UseHeadless(new AvaloniaHeadlessPlatformOptions())` in place of the app's `UsePlatformDetect()`, plus
  `.WithInterFont()`. Headless drawing is left on — these tests read the control tree, not pixels.
- `tests/Enigma.Msi.Desktop.UnitTests/Views/SplashWindowTests.cs` — six cases: the product name, the
  literal `Version` label and `AppInfo.Version` are all on screen; the tagline and the author are
  **not**; the logo `Image` has a non-null `Source` (a missing `AvaloniaResource` fails as the markup
  loads); the window flags and the 420×260 size are the splash's; and `Background` plus the `Border`'s
  `BorderBrush` both resolve in **each** theme variant (an `[AvaloniaTheory]` with a light and a dark
  row).

**Modified — build/test configuration**

- `Directory.Packages.props` — `Avalonia.Headless` and `Avalonia.Headless.XUnit` at **12.1.1**, inside
  the coupled Avalonia `ItemGroup` under their own comment explaining why they live there rather than
  with the test packages.
- `tests/Enigma.Msi.Desktop.UnitTests/Enigma.Msi.Desktop.UnitTests.csproj` — both referenced, with a
  comment saying this is the one suite with a UI dependency and why it needs a windowing platform.

**Modified — docs**

- `docs/guides/desktop-app.md` — the splash paragraph: logo, name and `Version <x.y.z>` and nothing
  else, with the tagline/copyright/author pointed at the About dialog.
- `CLAUDE.md` — three spots (documentation freshness sweep, accepted by the maintainer): the
  `FEATURE-2D02` paragraph in the build-state block now records PHASE02's outcome (splash content, the
  deleted `SplashViewModel`, the first headless suite, 453 tests) with PHASE03–04 as what remains; the
  coupled-UI-set bullet in *Target frameworks & dependencies* lists the two headless packages and says
  why they are pinned in the same `ItemGroup` though only the test suite consumes them; and *Build &
  test* gains a paragraph on the headless suite — what it is for, that `HeadlessTestApp` declares the
  assembly-wide session, that plain `[Fact]` tests are unaffected, and that the UI dependency stays in
  that one suite.
- `docs/roadmap.md`, `docs/plan/FEATURE-2D02.md` — statuses.

## Deviations & follow-ups

- **Both headless packages are referenced, not just `Avalonia.Headless.XUnit`.** The plan named the
  XUnit one; the suite uses types from `Avalonia.Headless` itself (`AvaloniaTestApplicationAttribute`,
  `UseHeadless`, `AvaloniaHeadlessPlatformOptions`), so relying on it transitively would have been a
  hidden dependency. Enigma.MarkdownEditor — the model the plan cites — declares both for the same
  reason.
- **The plan's "remove the now-unused `using`" in `App.axaml.cs` does not apply.**
  `using Enigma.Msi.Desktop.ViewModels;` is still needed for `MainWindowViewModel`, so it stays; a
  zero-warning build would have caught it either way.
- **Plan step 6's "a hook over the app's `BuildAvaloniaApp`" is a hook over the app's `App`, not over
  `Program.BuildAvaloniaApp()`.** `Program` is `internal` and there is no `InternalsVisibleTo`, and its
  builder calls `UsePlatformDetect()` — which a headless session must replace.
  `AppBuilder.Configure<App>()` is the reachable, faithful half: the real application object and
  therefore the real resource surface.
- **The visual reading was performed, not deferred.** The Release build of the app was launched on
  Windows and the splash captured mid-display: logo, `Enigma.Msi`, `Version 1.2.0` in the secondary
  brush, on the dark surface inside the subtle border, at 420×260 — no tagline, no author. The process
  was still alive 2 s later, confirming the handover survived the loss of the `DataContext`.
- **The headless suite is scoped to the splash**, as the plan scoped it. Now that the platform is in the
  project, the quick-start card's layout (PHASE01's follow-up note) and the main window's panes are
  reachable the same way — a follow-up, not a gap.
- **Line endings:** the working tree is consistent; nothing to recommend.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release` — **Build succeeded. 0 Warning(s), 0 Error(s)** (16.7 s),
  all seven projects, including the XAML compiler over the rewritten `SplashWindow.axaml`.
- `dotnet test --solution Enigma.Msi.slnx -c Release` — **453 passed, 0 failed, 0 skipped** (10.2 s)
  across all four suites (`Enigma.Msi.UnitTests` on net8.0 and net10.0, `Enigma.Msi.Desktop.UnitTests`,
  and the `net472` `Enigma.Msi.Worker.UnitTests` with its WixSharp drift guards). 447 → 453: the six new
  splash cases (four facts and a two-row theory). The pre-existing `[Fact]` ViewModel tests are
  unaffected by the assembly-level headless session — they continue to run beside it.
- **Manual:** `src/Enigma.Msi.Desktop/bin/Release/net10.0/Enigma.Msi.Desktop.exe` launched, splash
  captured at ~2.1 s, main window shown after dismissal.

**Acceptance criteria**

| Criterion | Met |
|---|---|
| Splash shows logo, `Enigma.Msi`, `Version 1.2.0` — no tagline, no author — at 420×260 with the subtle border | Yes — markup + the captured screenshot |
| Still dismissed by the 2 s timer, a click or a key | Yes — `SplashWindow.axaml.cs` untouched; handover verified by launching the app |
| `SplashViewModel` gone from the codebase and from DI; nothing references it | Yes — file deleted, registration removed, grep clean |
| The About dialog is unchanged and still shows the tagline, the copyright and the author | Yes — `AboutCard`/`AboutViewModel`/`AppInfo` untouched; `AppInfoTests` still asserts all five constants |
| The new headless tests pass and the rest of the suite is unaffected | Yes — 453/453, the 447 prior cases included |
| Warning-free build (XAML compiler included) and the whole suite passes | Yes — 0 warnings, 453/453 |
