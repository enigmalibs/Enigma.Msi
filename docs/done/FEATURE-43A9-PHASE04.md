# FEATURE-43A9-PHASE04 — Build client, interop & packaging plumbing (DONE)

## Summary

Closed the loop between the modern library and the net472 worker. `IMsiBuildService`/`MsiBuildService`
validates a package, writes it as a request file, runs the worker over it, streams the worker's output
line by line, and reads the outcome back — plus a pre-flight check that reports a missing worker or a
missing WiX CLI as sentences the user can act on. Alongside it, the two MSBuild files that put the worker
where the client looks for it: one for package consumers, one for projects inside this repository.

**This phase produced a real MSI end to end through the library.** A scratch net10.0 host referencing
`Enigma.Msi`, with the real worker copied beside it by `build/CopyWorkerOutput.targets`, reported
`workerFound=True wix=7.0.0+b8977d6` from the pre-flight and then built a 2 609 152-byte
`ContosoWidget.msi` through `BuildAsync` — worker discovery, the JSON handshake, log streaming and
result parsing all exercised against the actual toolchain rather than only against the stub.

Four decisions later work depends on:

- **Cancellation throws, and it kills the tree.** `BuildAsync` throws `OperationCanceledException`
  (the .NET convention) rather than returning a "cancelled" result, and the worker's whole process tree
  is terminated first. Measured against the real toolchain: with a genuine `wix.exe` child running at the
  moment of cancellation, both it and the worker were gone afterwards. PHASE05's Cancel command should
  catch `OperationCanceledException`, not inspect the result.
- **Validation comes before the worker.** A package that cannot build costs a millisecond and returns
  *every* violation, never a process launch. `Build_WithAnInvalidPackage_ReportsEveryViolationWithout
  RunningAWorker` pins the ordering by configuring a worker path that could not possibly run.
- **Every way the handshake can fail has its own message.** Worker not found (worded differently for a
  configured path and for the default location), no result file (naming the exit code), and a result file
  that is not a build result are three distinct strings, because the user's next move differs.
- **The worker copy is expressed as items, not as a `Copy` task after `Build`.** That is what makes it
  survive `dotnet publish` — verified for both mechanisms.

## Files/modules touched

### Created — `src/Enigma.Msi/Build/`
- `IMsiBuildService.cs` — `CheckPrerequisitesAsync` + `BuildAsync(package, log, cancellationToken)`
- `MsiBuildService.cs` — the implementation: validation gate, worker discovery, temp request/result
  files, process start, dual-stream pumping into `IProgress<string>`, process-tree kill, result parsing,
  temp cleanup
- `MsiBuildServiceOptions.cs` — `WorkerPath`, `WixToolPath`, and the `worker`/`Enigma.Msi.Worker.exe`
  layout constants shared with the MSBuild files
- `MsiPrerequisites.cs` — `WorkerPath`, `WixToolVersion`, `Problems`, `WorkerFound`, `WixToolAvailable`,
  `IsSatisfied`

### Created — `build/`
- `Enigma.Msi.targets` — shipped in the nupkg under `build/`, auto-imported by consumers; injects the
  package's `tools/worker/` payload as `None` items linked into `worker/`
- `CopyWorkerOutput.targets` — the in-repository counterpart, producing the same layout from a project's
  build output; hooks both `Build` and `Publish`, and documents the three `ProjectReference` attributes a
  host must use

### Created — `tests/Enigma.Msi.StubWorker/` (net8.0 console exe, test asset)
- `Enigma.Msi.StubWorker.csproj`, `Program.cs` — speaks the real `--request`/`--result` protocol and the
  real `MsiPackageJson` contract, scripted per test by a `stub.directive` file in the package's release
  directory; also answers `--version` (standing in for the WiX CLI) and `--sleep <seconds>` (so it can
  give itself a child process for the process-tree test)

### Created — `tests/Enigma.Msi.UnitTests/Build/`
- `StubWorker.cs` — locating the stub, reading values it announced, polling a process id to extinction,
  and the synchronous `IProgress<string>` collector the cancellation tests need
- `MsiBuildServiceTests.cs` — 15 tests: success, request-document round-trip, argument construction, dual
  stream capture, temp cleanup, worker-reported failure, missing result file, unparseable result file,
  both worker-not-found wordings, the validation gate, null argument, pre-cancelled, cancel mid-build,
  cancel kills the tree
- `MsiPrerequisiteTests.cs` — 8 tests: satisfied, both failure modes and their wording, a tool that exists
  but reports failure, both problems at once, pre-cancelled, the default-constructed value

### Modified
- `src/Enigma.Msi/Enigma.Msi.csproj` — packs `build/Enigma.Msi.targets` into the nupkg's `build/` folder
- `tests/Enigma.Msi.UnitTests/Enigma.Msi.UnitTests.csproj` — build-order reference to the stub worker and
  an import of `CopyWorkerOutput.targets` with the source directory and folder name overridden
- `Enigma.Msi.slnx` — added `tests/Enigma.Msi.StubWorker`
- `docs/roadmap.md`, `docs/plan/FEATURE-43A9.md` — PHASE04 → DONE

## Deviations & follow-ups

- **The tests drive a real stub executable, not an abstraction over `Process`.** The plan allowed either.
  A child process was chosen because process startup, stream pumping, exit codes and killing a process
  tree are exactly the parts an injectable `IProcessRunner` would have mocked away — the interesting code
  would then have had no coverage at all. The cost is one extra project under `tests/`
  (`IsTestProject=false`, so the test platform ignores it) and Windows-only tests, which the solution
  already is: it contains a net472 test project and builds MSIs.
- **Behaviour is selected through a file in the package's release directory**, not an environment
  variable, so tests running in parallel cannot influence each other. The release directory has to be
  non-empty to pass validation anyway, so the directive file doubles as its content.
- **`BuildAsync` runs `ValidateAll`, i.e. the environment rules too.** The disk is about to be read by
  the worker regardless, and failing fast with every violation beats a process round trip. The UI's
  as-you-type path is unaffected — that calls `Validate` directly.
- **Cancellation on netstandard2.0 shells out to `taskkill /T /F`.** `Process.Kill(entireProcessTree:)` is
  .NET Core 3.0+, and the library still targets netstandard2.0 so the net472 worker can consume the model.
  The branch compiles and is the standard .NET Framework equivalent, but it is **not exercised by a test**:
  the suite runs on net8.0/net10.0, which take the `Kill(true)` branch. Keeping the type on all three TFMs
  was preferred over a public surface that differs per target framework.
- **The nupkg's `tools/worker/` payload is not populated here** — deliberately, per the plan's split
  ("PHASE04 delivers the layout and the `.targets` file; the pack metadata and pack-verify belong to the
  future release item"). `dotnet pack` today emits `build/Enigma.Msi.targets` at the right path, and the
  targets file is guarded by `Exists(...)`, so it is inert until FEATURE-5F00 adds the payload.
- **`--version` is how the WiX CLI is probed**, from the host process rather than from the worker. It is a
  proxy — the worker is what actually needs the tool — but both run on the same machine and the global
  tools directory is on the user's `PATH`. A non-zero exit, an unstartable path, and a probe that does not
  answer within 30 seconds are all reported as "unavailable".
- **`UndefineProperties="TargetFramework"` on the worker `ProjectReference` is load-bearing.** Without it
  the host's `TargetFramework` flows down as a global property and the build fails with NETSDK1005. Found
  the hard way here; documented in `CopyWorkerOutput.targets` so PHASE05 does not rediscover it.
- **Both copy targets are guarded with `Condition="'$(TargetFramework)' != ''"`.** The outer pass of a
  multi-targeting host imports `Microsoft.Common.CrossTargeting.targets`, which does not define
  `$(OutDir)`/`$(PublishDir)` — so an unguarded `AfterTargets="Build"` copy resolves to a *relative*
  destination and writes the worker into the project directory rather than into `bin/`. Caught here by an
  untracked `tests/Enigma.Msi.UnitTests/stub-worker/` folder appearing in `git status`; a single-TFM host
  such as the Desktop app would never have shown it, so the guard matters most for whoever adds the next
  multi-targeting worker host.
- **`IProgress<string>` is invoked on a background thread and must not throw** — documented on the
  interface rather than defended with a swallowing `try`/`catch`, which is the standard `IProgress<T>`
  contract. PHASE05's log pane will need to marshal to the UI thread itself.
- **Line endings (CRLF):** no churn observed; all new files are LF and `.gitattributes` governs them.
  Recommendation-only per `dev-workflow`; **no action taken**.

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`
  across all five projects (library × 3 TFMs, worker, stub worker, both test suites).
- **Test:** `dotnet test --solution Enigma.Msi.slnx -c Release` (MTP) →
  `total: 311, failed: 0, succeeded: 311, skipped: 0` — 265 before this phase plus **46 new**
  (23 × net8.0 and net10.0). The stub-worker console project is correctly ignored by the test platform.
- **End-to-end MSI through the library (manual):** scratch net10.0 host, real worker copied by
  `CopyWorkerOutput.targets`, WiX CLI 7.0.0.

  | Step | Observed |
  |---|---|
  | `CheckPrerequisitesAsync` | `satisfied=True workerFound=True wix=7.0.0+b8977d6`, worker resolved to `…/worker/Enigma.Msi.Worker.exe` |
  | `BuildAsync` | `success=True`, 18 log lines streamed, `ContosoWidget.msi` = 2 609 152 bytes |
  | temp directories afterwards | none left under `%TEMP%` |

- **Cancellation against the real toolchain (manual):** the same host cancelled 3 s into a build. Peak
  child processes sampled *during* the build: `wix=1, worker=1`. After cancellation:
  `OperationCanceledException` thrown, `wix=0, worker=0`, and no temporary directory left behind.
- **`build/Enigma.Msi.targets` (manual):** a scratch consumer importing it from a faked package layout
  (`pkg/tools/worker/Enigma.Msi.Worker.exe` plus a nested `sub/extra.txt`) received both files under
  `bin/Release/net10.0/worker/` **and** under `publish/worker/`, preserving the subdirectory.
- **`build/CopyWorkerOutput.targets` with its defaults (manual):** a scratch net10.0 host with only the
  documented `ProjectReference` received the whole net472 worker output — `Enigma.Msi.Worker.exe`, its
  `.exe.config`, `mbanative.dll`, the `.wixsharp` payload — under both `bin/Release/net10.0/worker/` and
  `publish/worker/`. Its overridden form is exercised by every build of the test suite.
- **Pack layout (manual):** `dotnet pack src/Enigma.Msi/Enigma.Msi.csproj -c Release` produces a nupkg
  containing `build/Enigma.Msi.targets` alongside `lib/netstandard2.0`, `lib/net8.0`, `lib/net10.0`.

## Acceptance criteria — all met

1. ✅ **`BuildAsync` round-trip works against a stub worker (test double) including failure and
   cancellation paths.** 15 tests cover success, the request document, argument construction, log
   streaming, temp cleanup, worker-reported failure, a missing result file, an unparseable result file,
   both discovery failures, the validation gate, pre-cancellation, cancellation mid-build, and
   cancellation taking the process tree with it — and the same paths were re-verified against the real
   worker by hand.
2. ✅ **Pre-flight reports missing worker and missing wix tool with actionable messages.** 8 tests, and
   the wording is asserted, not just the boolean: the discovery message names the path that was searched
   (or `MsiBuildServiceOptions.WorkerPath`), and the WiX message carries
   `dotnet tool install --global wix`.
3. ✅ **Build + full suite green, zero warnings** — 311 tests passing, 0 warnings across the solution.
