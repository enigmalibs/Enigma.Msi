# FEATURE-5E0B — Remove the splash screen

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-5e0b-remove-splash-screen`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

The app starts right away: the main window is the first and only thing on screen — no splash, no
two-second wait. Part of the uniform baseline for every Enigma app.

## Context & constraints

- The splash came with FEATURE-74C4 `PHASE01` and was restyled by FEATURE-2D02 `PHASE02`:
  - `Views/SplashWindow.axaml(.cs)` — the undecorated 420×260 window, a 2 s `DispatcherTimer`, dismissed
    by click or key, raising `Dismissed` once;
  - `App.OnFrameworkInitializationCompleted` makes it the initial `MainWindow` and hands over to the real
    window on `Dismissed` (`ShowMainWindow`: assign, show, then close the splash — order load-bearing
    under `ShutdownMode.OnLastWindowClose`);
  - `ServiceCollectionExtensions.AddViewsAndViewModels` registers it transient, with a remark about why.
- Without a splash, `desktop.MainWindow = mainWindow` is all the lifetime needs: the initial
  `MainWindow` is shown automatically, and there is no hand-over left to order.
- `tests/…/Views/SplashWindowTests.cs` is the only headless suite. `HeadlessTestApp` and the two headless
  packages stay: FEATURE-394B adds the About card's headless tests next.
- `Assets/logo.png` stays — the About card shows it.
- Mentions to update: `App.axaml.cs` summary, `AppInfo` summary ("the splash screen and the About
  dialog"), the desktop test csproj comment, `HeadlessTestApp` remarks (they name the splash),
  `AppInfoTests` summary, `docs/guides/desktop-app.md` (*The splash screen*), `README.md`
  (*Splash and About* bullet), `docs/RELEASE.md` §5 ("the splash hands over to the main window"),
  `CLAUDE.md` (layout/test lines naming `SplashWindowTests`).

## Steps

1. Delete `Views/SplashWindow.axaml`, `Views/SplashWindow.axaml.cs`, `tests/…/Views/SplashWindowTests.cs`.
2. `App.OnFrameworkInitializationCompleted`: after the hosts and storage providers are registered,
   `desktop.MainWindow = mainWindow`; remove `ShowMainWindow` and the splash comments.
3. `ServiceCollectionExtensions`: drop the `SplashWindow` registration and its remark sentence.
4. Reword every comment/doc listed above that names the splash; the guide's section goes, the README
   bullet becomes About-only.
5. Build Release warning-free; run the whole suite.

## Acceptance criteria

- `git grep -n -i splash -- src tests` finds nothing.
- `App` assigns the main window directly; no timer, no hand-over.
- The Release build is warning-free; the whole suite is green (the 6 splash tests are gone — recorded).
- A Release run of the app opens on the main window (manual check, recorded).

## Out of scope

- The About dialog's content (FEATURE-394B).
- `RELEASENOTES.md` (FEATURE-AB6B).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Hide or delete | Delete every splash type, registration and test | Code that is never shown is dead weight | A setting to turn it off |
| Headless test infra | Kept | FEATURE-394B's About card tests use it next; removing then re-adding is churn | Removing it with its only suite |
| Startup shape | `desktop.MainWindow = mainWindow` | The lifetime shows the initial main window itself — the pre-splash shape | Showing it explicitly |
| Tests | Delete the splash tests; no new test | Behaviour removed; a test asserting a type's absence pins nothing useful | An "app starts on MainWindow" headless test (needs a lifetime the session does not run) |
