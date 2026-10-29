# FEATURE-2EEB-PHASE01 — Hooks, policy and logging

## Summary

The app now catches every exception nothing else catches, and logs it with where it came from. It is
Enigma.GitClient's safety net, scaled to one window and to an app that carries no credentials.

- **`UnhandledExceptionHooks`** (static) hooks three places:
  - `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException`, installed first thing in
    `Main`. Unobserved tasks are marked observed.
  - The UI dispatcher's `UnhandledException`, installed by `App` under the desktop lifetime only, and marked
    handled. **A throwing command no longer ends the process.**

  Reports that arrive before the reporter exists are buffered (at most 32) and traced, then handed over on
  `Attach`.
- **`UnhandledErrorReporter`** logs each one at once, on the reporting thread, as `Error` with its source. A
  cancellation someone asked for, or any cancellation after the window has closed, is logged at `Debug`
  only.
- **`UnhandledErrorPolicy` / `UnhandledErrorBatch`** hold the decisions PHASE02's dialog needs:
  - one batch at a time, with later errors merged into it;
  - the same instance offered twice is ignored;
  - an identical error closed less than 1 minute ago is suppressed;
  - the one-line summary looks through wrappers;
  - the details (type, message, stack, inner exceptions to 8 levels) never include `Exception.Data`.

  They are fully tested here and wired into the reporter in PHASE02.
- **`Program.Main`** installs the hooks before anything else and detaches them before the host is disposed.
  It also logs an exception that escapes `Main` itself (a failed start) as `Fatal` through NLog directly,
  then rethrows. Without a window there is no dialog, and a WinExe has no console.

## Files/modules touched

- Created:
  - `src/Enigma.Msi.Desktop/Services/UnhandledErrorSource.cs`
  - `src/Enigma.Msi.Desktop/Services/UnhandledErrorDecision.cs`
  - `src/Enigma.Msi.Desktop/Services/UnhandledErrorBatch.cs`
  - `src/Enigma.Msi.Desktop/Services/UnhandledErrorPolicy.cs`
  - `src/Enigma.Msi.Desktop/Services/IUnhandledErrorReporter.cs`
  - `src/Enigma.Msi.Desktop/Services/UnhandledErrorReporter.cs`
  - `src/Enigma.Msi.Desktop/Services/UnhandledExceptionHooks.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/ManualTimeProvider.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/RecordingLogger.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/RecordingReporter.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Services/UnhandledErrorPolicyTests.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Services/UnhandledErrorBatchTests.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Services/UnhandledExceptionHooksTests.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Services/UnhandledErrorReporterTests.cs`
  - `docs/done/FEATURE-2EEB-PHASE01.md`
- Modified:
  - `src/Enigma.Msi.Desktop/Program.cs`
  - `src/Enigma.Msi.Desktop/App.axaml.cs`
  - `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs`
  - `docs/roadmap.md`, `docs/plan/FEATURE-2EEB.md`

## Deviations & follow-ups

- **The shutdown signal is `MainWindow.Closed`**, not a lifetime event. The app has one window, so once it
  has closed the app is closing. `ShutdownRequested` can be cancelled, and `Exit` comes too late for the
  cancellations a close triggers.
- **`Main` logs a fatal exception through NLog directly.** The plan did not list this. It is the only way an
  error before the safety net exists, or one that escapes the dispatcher, ends up anywhere.
- **The hooks are installed only under a desktop lifetime.** The XAML designer and the headless test fixture
  also run `OnFrameworkInitializationCompleted`, and a handled dispatcher hook there would swallow a test's
  own exceptions.
- **About the manual check below:** through UI Automation, a command that throws *synchronously* surfaces as
  an error of the automation call, not on the dispatcher. A real click goes through the dispatcher. The check
  therefore used a throw after an `await`, which is how this app's async commands fail in practice.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Null guards | `?? throw` in constructors, `if (x is null) throw` in methods | This app's idiom; Enigma.GitClient's `ThrowIfNull` is not used here |
| `IsShuttingDown` read from any thread | A `volatile` backing field | `Report` reads it on whatever thread reported |
| Duplicate detection | `ConditionalWeakTable.TryAdd` | One call instead of GitClient's look-up-then-add |
| Testing `Add` | Batches grown through the policy | `Add` stays internal (only the policy grows a batch), and the suite has no `InternalsVisibleTo` |
| Hooks tests | Never call `Install()`, drain on setup, detach on teardown, own collection | `Install` would hook the test process's own unobserved tasks; the hooks are static |

## Documentation sweep

- Nothing became stale. The user-facing description lands with the dialog in PHASE02.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release`: **0 warnings**, 0 errors.
- `dotnet test --solution Enigma.Msi.slnx -c Release`: **401/401 passed**, exit 0, all three assemblies
  `passed`. There are 31 new tests:
  - policy, 12;
  - batch, 9;
  - hooks, 5;
  - reporter, 5.
- Manual check:
  1. Temporarily changed `ShowAboutAsync` to throw after `await Task.Yield()`, built Release and launched
     the app.
  2. Invoked **About** through UI Automation.
  3. The process stayed alive. `Enigma.Msi.Desktop.log` recorded
     `ERROR | An unhandled exception reached the safety net (Dispatcher) | … System.InvalidOperationException: TEMPORARY safety-net check`.
  4. Removed the temporary code: `MainWindowViewModel.cs` has no diff, and the rebuilt suite is 401/401
     again.
