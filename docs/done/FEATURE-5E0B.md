# FEATURE-5E0B — Remove the splash screen

## Summary

The app starts straight on its main window. The 2-second `SplashWindow` is gone, along with its
registration, its hand-over and its tests. `App.OnFrameworkInitializationCompleted` registers the control
library's hosts and storage providers and then assigns `desktop.MainWindow = mainWindow`. The lifetime shows
that initial window itself, so no hand-over order is left to protect.

## Files/modules touched

- Deleted:
  - `src/Enigma.Msi.Desktop/Views/SplashWindow.axaml`
  - `src/Enigma.Msi.Desktop/Views/SplashWindow.axaml.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Views/SplashWindowTests.cs`
- Modified:
  - `src/Enigma.Msi.Desktop/App.axaml.cs`
  - `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs`
  - `src/Enigma.Msi.Desktop/Services/AppInfo.cs` (summary)
  - `tests/Enigma.Msi.Desktop.UnitTests/Services/AppInfoTests.cs` (summary)
  - `docs/guides/desktop-app.md`
  - `README.md`
  - `docs/RELEASE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-5E0B.md`
- Created: `docs/done/FEATURE-5E0B.md`

## Deviations & follow-ups

- The plan listed `HeadlessTestApp` and the test csproj comment among the splash mentions. FEATURE-CED6
  already replaced both with `HeadlessAvaloniaFixture`, which names no splash.
- The headless infrastructure now has no test using it, for one dev only. FEATURE-394B adds the About
  card's view tests next.
- Splash mentions that remain are history and stay:
  - `CLAUDE.md`'s build-state narrative (FEATURE-74C4, FEATURE-2D02);
  - `RELEASENOTES.md`;
  - the README's 1.6.0 callout, which FEATURE-AB6B rewrites.
- `Assets/logo.png` stays, because the About card shows it.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| README bullet | Becomes *About*, plus one clause saying the app opens on the main window | The bullet described both features. FEATURE-394B rewrites the About half to the new content |
| `AppInfo` summary | No longer names the splash | It is now read by the About dialog alone |

## Documentation sweep

- `docs/guides/desktop-app.md`: the *The splash screen* section is removed.
- `README.md`: the *Splash and About* bullet becomes *About*.
- `docs/RELEASE.md` §5: "starts on the main window" replaces "the splash hands over".

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release`: **0 warnings**, 0 errors.
- `dotnet test --solution Enigma.Msi.slnx -c Release`: **348/348 passed**. That is 354 before, minus the 6
  splash tests deleted with the splash.
- `git grep -n -i splash -- src tests` finds nothing.
- Manual run: `bin/Release/net10.0/Enigma.Msi.Desktop.exe` was started and its process polled. The first
  window to appear, after about 3.2 s on a cold start, was `Enigma.Msi — new package`, the main window, and
  it was still the window 1.5 s later.
