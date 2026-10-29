# FEATURE-2EEB-PHASE02 — The error dialog

## Summary

An exception nothing else handles is now shown, as well as logged, in a **Something went wrong** content
dialog. The dialog says the app is still running and gives the error's message. Its collapsed **Details**
hold type, message, stack trace and inner exceptions, with a **Copy details** button. It is Enigma.GitClient's
safety-net dialog: same title, Phosphor *Warning* icon in `#E8A33D`, one default **Close**, 480 px minimum,
and the same text.

- **`UnhandledErrorReporter`** now posts each real error to the UI thread. There, `UnhandledErrorPolicy`
  decides, and batches are shown one after another through the new seam. A dialog that fails switches the
  net to log-only for the session instead of looping.
- **`UnhandledErrorDialogService`** waits before showing anything, until all three of these hold:
  - the window is visible;
  - no other dialog holds the shared host, since showing would replace that dialog's content;
  - no build overlay covers the host.

  It yields at background priority after each change, so whatever awaited a closing dialog can ask its next
  question first. It states all six dialog sizes, because of the reset trap.
- **`UnhandledErrorCard`** + **`UnhandledErrorViewModel`**: the view follows its batch, so an error counted
  in while it is open updates the count and the details. The copy goes through the window's clipboard from
  code-behind.
- **The policy now recognises a fault reported twice:** once by the dispatcher, and again by the finaliser
  wrapped in a fresh `AggregateException`. The manual run below caught this.

## Files/modules touched

- Created:
  - `src/Enigma.Msi.Desktop/Services/IUnhandledErrorDialogService.cs`
  - `src/Enigma.Msi.Desktop/Services/UnhandledErrorDialogService.cs`
  - `src/Enigma.Msi.Desktop/ViewModels/UnhandledErrorViewModel.cs`
  - `src/Enigma.Msi.Desktop/Views/UnhandledErrorCard.axaml`
  - `src/Enigma.Msi.Desktop/Views/UnhandledErrorCard.axaml.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/UnhandledErrorViewModelTests.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Views/UnhandledErrorCardTests.cs`
  - `docs/done/FEATURE-2EEB-PHASE02.md`
- Modified:
  - `src/Enigma.Msi.Desktop/Services/UnhandledErrorReporter.cs`
  - `src/Enigma.Msi.Desktop/Services/IUnhandledErrorReporter.cs`
  - `src/Enigma.Msi.Desktop/Services/UnhandledErrorPolicy.cs`
  - `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Services/UnhandledErrorReporterTests.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Services/UnhandledErrorPolicyTests.cs`
  - `docs/guides/desktop-app.md`
  - `README.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-2EEB.md`

## Deviations & follow-ups

- **The reporter is still attached at startup, not when the window opens** (plan step 4). Errors are logged
  from the first moment, and the *dialog service* waits for the window to be visible. That gets the same
  ordering without a gap in the log.
- **Errors during a build show when it ends.** The plan allowed this only if the overlay exposed its state,
  and it does: `Overlay.IsOpen`, which `OverlayService` drives.
- **A fault reported twice is still logged twice**, once per source (Dispatcher, then UnobservedTask). That is
  deliberate: the log records every hook that fired, and the policy keeps it to one dialog.
- **Follow-up outside this repo:** Enigma.GitClient's policy has the same double-report gap, because its
  same-instance check does not look inside the finaliser's `AggregateException`.
- The `UnhandledErrorDialogService` itself is verified manually, not by a unit test: it needs the real
  `MainWindow` and its hosts. The reporter's sequencing behind it is unit-tested through the seam.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the dialog waits | `PropertyChanged` on the window (`IsVisible`), the dialog host and the overlay (`IsOpen`), then a background-priority yield | Event-driven, no timer; the yield lets a dialog sequence (e.g. confirm → quick start) finish first |
| Seam | `IUnhandledErrorDialogService` | The app's pattern for dialogs (`IAboutDialogService`, `IQuickStartDialogService`); it makes the reporter testable |
| Details font | `Consolas, Menlo, monospace`, as the build log | The app has no `MonospaceFontFamily` key; GitClient's comes from its own `App.axaml` |
| "Copy details" style | The default button | The app has no `toolbar` button class |

## Documentation sweep

- `docs/guides/desktop-app.md`: a new *When something goes wrong* section.
- `README.md`: a *safety net* bullet.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release`: **0 warnings**, 0 errors.
- `dotnet test --solution Enigma.Msi.slnx -c Release`: **417/417 passed**, exit 0, all three assemblies
  `passed`. That is 16 new tests:
  - reporter, now 10 (from 5);
  - policy, 3 for the aggregate duplicate;
  - ViewModel, 4;
  - headless card, 4.
- Manual check, with a temporary throw in `ShowAboutAsync` (after `await Task.Yield()`) and Release build
  screenshots:
  1. About was invoked while the startup quick start was open. No error dialog appeared under it.
  2. Cancelling the quick start brought up **Something went wrong**: amber warning icon, the lead sentence,
     the message.
  3. The first run said "1 more error occurred." for one throw. That was the aggregate duplicate, fixed above.
  4. Re-run after the fix: no "more" line, and the expanded Details showed the monospace stack and **Copy
     details**.
  5. The app stayed alive throughout.

  The temporary code is removed: `MainWindowViewModel.cs` has no diff, and the suite is 417/417.
