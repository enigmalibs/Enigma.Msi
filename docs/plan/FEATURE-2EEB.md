# FEATURE-2EEB — Catch unhandled exceptions

**Status:** IN PROGRESS
**Type:** FEATURE (multi-phase)
**Branch (per phase, at build time):** `feature/feature-2eeb-phaseNN-<slug>` — one branch per phase, cut from the run-branch tip.
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Every exception nothing else catches is logged and shown in an error **content dialog** that says the
application is still running — the safety net every Enigma app carries. The reference is Enigma.GitClient's
(`Services/UnhandledExceptionHooks.cs`, `UnhandledErrorPolicy.cs`, `UnhandledErrorReporter.cs`,
`Views/Dialogs/UnhandledErrorView.axaml`), scaled to a single-window app with no credentials.

## Context & constraints

- Today nothing hooks `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` or
  Avalonia's `Dispatcher.UIThread.UnhandledException`: a throwing command ends the process, and the only
  trace is whatever NLog wrote before (file target `${basedir}/${processname}.log`).
- Known failures are already handled where they happen (`MainWindowViewModel`: open/save I/O, build
  cancellation) — the net is a safety net, not the error handling.
- `CommunityToolkit.Mvvm`'s `AsyncRelayCommand` rethrows a faulted execution on the UI
  `SynchronizationContext` — i.e. into `Dispatcher.UIThread.UnhandledException`.
- An exception reaching `AppDomain.UnhandledException` ends the process whatever a handler does: it can
  only be logged.
- One window, one `ContentDialog` host (`MainWindow.HostDialog`), shared with About and Quick start;
  the build `Overlay` sits **above** the dialog host in `MainWindow.axaml`.
- The dialog host's size properties are not reset between dialogs (CLAUDE.md trap): the error dialog
  states all six.
- No secrets travel in this app's exceptions (paths and WiX output), so the details need no redaction —
  but they still never include `Exception.Data`.
- Reference styling: title "Something went wrong", Phosphor `Warning` icon in `#E8A33D`, one `Close`
  button (default); content `MinWidth 480`: lead sentence, the message (SemiBold), "N more errors
  occurred." (secondary), a collapsed **Details** expander holding a read-only monospace `TextBox`
  (max 220 px high) and a **Copy details** button.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Shape | GitClient's: static hooks → reporter → policy → dialog | Uniform across apps; each part testable on its own | A try/catch in `Main` (catches nothing on the UI thread) |
| Scaling down | No multi-window tracking, no token recovery, no redaction | Single window, no credentials here | Copying GitClient verbatim |
| What is shown | Dispatcher and unobserved-task exceptions → dialog; AppDomain → logged (the process ends) | The runtime allows nothing more | Pretending to recover from AppDomain |
| Floods | One dialog at a time; later errors counted into it; an identical error closed < 1 min ago is logged only | A failing loop must not open a dialog per failure | A dialog per exception |
| Cancellations | Ignored when their token was cancelled, or during shutdown | A stop someone asked for is not an error | Showing them |
| Busy host | Wait for the open dialog to close, then show | Showing would replace the other dialog's content | Interrupting it |
| Build overlay | The reporter waits while the overlay is shown if `IOverlayService`/`Overlay` exposes its open state; otherwise recorded as a limitation (the dialog becomes visible when the build ends) | The overlay is above the dialog host | Re-ordering the hosts (changes every dialog) |
| Errors before the window | Buffered (≤ 32) and handed over once the main window has opened | The dialog needs a host on screen | Dropping them |
| Copy details | The card's code-behind writes to `TopLevel.Clipboard` | View-only behaviour; no new service seam for one click | An `IClipboardService` |
| Phasing | PHASE01 hooks + policy + logging, PHASE02 dialog + wiring | Two reviewable units; PHASE01 already stops the crashes | One dev |

## PHASE01 — Hooks, policy and logging

**Status:** DONE

### Steps

1. `Services/UnhandledErrorSource.cs` (`Dispatcher`, `UnobservedTask`, `AppDomain`), `UnhandledErrorDecision`
   (`Ignore`, `Suppress`, `Queue`, `Merge`).
2. `Services/UnhandledErrorBatch.cs` — first error + later ones, `Message` (the meaningful message through
   single-inner `AggregateException`/`TargetInvocationException`), `More`, `Details` (type, message,
   stack, inner exceptions to depth 8; "Error i of n" when several), `Changed` event.
3. `Services/UnhandledErrorPolicy.cs` — `TimeProvider`-driven: `IsShuttingDown`, `IsIgnoredCancellation`,
   `Offer` (same instance twice → `Ignore`; cancellation → `Ignore`; open batch → `Merge`; same signature
   closed < `RepeatWindow` (1 min) → `Suppress`; else `Queue`), `BeginShow`, `EndShow`.
4. `Services/IUnhandledErrorReporter.cs` (`Report(exception, source)`, `ShutdownStarted()`) and
   `Services/UnhandledExceptionHooks.cs` (static: `Install()` for AppDomain + unobserved tasks,
   `InstallDispatcher()` with `Handled = true`, `Attach`/`Detach`, `Report`; early reports buffered ≤ 32
   and traced).
5. `Services/UnhandledErrorReporter.cs` — this phase: logs (`LogError` with the source; `LogDebug` for an
   ignored cancellation), and records the policy decision; no dialog yet.
6. Wiring: `Program.Main` calls `Install()` first and `Detach()` before the host is disposed;
   `App.OnFrameworkInitializationCompleted` calls `InstallDispatcher()`, attaches the reporter (resolved
   from DI, `TimeProvider.System` registered) and calls `ShutdownStarted` on `ShutdownRequested`.
7. Tests (plain `[Fact]`): policy (ignore/merge/suppress/repeat-window expiry with a manual
   `TimeProvider`, shutdown), batch (message unwrapping, details shape, depth cap, `Changed`), hooks
   (buffer before attach → flushed on attach, cap at 32, detach) — hooks tests in one non-parallel
   collection since the class is static.

### Acceptance criteria

- A command that throws no longer ends the process; the exception is in the log with its source.
- Release build warning-free; the whole suite green, new tests included.

## PHASE02 — The error dialog

**Status:** TODO

### Steps

1. `ViewModels/UnhandledErrorViewModel.cs` — `Lead`, `Message`, `HasMore`/`MoreText`, `Details`; follows the
   batch's `Changed`.
2. `Views/UnhandledErrorCard.axaml(.cs)` — the reference's markup (named elements, automation names);
   **Copy details** writes `Details` to the clipboard from code-behind.
3. `UnhandledErrorReporter` shows the batch: on the UI thread, on `MainWindow.HostDialog` through
   `IContentDialogService`; waits for a busy host (`Closed`) and, if exposed, for the build overlay;
   the dialog's own failure → logged, net switches to log-only; next pending batch shown after close.
4. Attach the reporter once the main window has opened (`Opened`), so buffered startup errors show.
5. Tests: `UnhandledErrorViewModelTests` (texts, singular/plural, follows `Changed`); headless
   `UnhandledErrorCardTests` (markup loads, texts, more-line hidden for one error, details collapsed).
6. Docs: `docs/guides/desktop-app.md` gains a short *When something goes wrong* section; README bullet.

### Acceptance criteria

- A throwing command shows "Something went wrong" with its message; the app keeps running.
- Two errors in a row → one dialog, "1 more error occurred."; the same error again within a minute → log only.
- Release build warning-free; the whole suite green, new tests included; a manual run with a temporary
  throwing path is recorded and the temporary code removed.

## Out of scope

- Crash reporting to any service; restarting the app after an AppDomain-level failure.
- Changing the existing, specific error handling in `MainWindowViewModel`.
