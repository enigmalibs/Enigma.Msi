# FEATURE-C19D — Publish scripts for Windows and Linux

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-c19d-publish-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

One-command publish scripts on both platforms, part of the uniform baseline:

- Windows — `packaging/windows/publish.ps1` and `packaging/windows/publish.bat`, both running
  `dotnet publish <src/Enigma.Msi.Desktop> -c Release -r win-x64 -p:PublishReadyToRun=true`;
- Linux — `packaging/linux/publish.sh`, next to `install.sh`/`uninstall.sh`, running
  `dotnet publish <src/Enigma.Msi.Desktop> -c Release -r linux-x64 -p:PublishReadyToRun=true`.

## Context & constraints

- The reference is Enigma.GitClient's `packaging/windows/publish.ps1` (`#Requires -Version 5.1`,
  comment-based help naming the command and the output folder, `Set-StrictMode`, project resolved from
  `$PSScriptRoot`, `exit $LASTEXITCODE`) and its planned `publish.bat`/`publish.sh` (FEATURE-0D4E: `%~dp0`
  / script dir, no arguments, `exit /b %ERRORLEVEL%` / `exec`).
- `.gitattributes`: `*.bat` CRLF, `*.sh` LF. `publish.sh` is staged `100755`.
- On the .NET 10 SDK a RID-specific publish is framework-dependent unless told otherwise; the output lands
  in `src/Enigma.Msi.Desktop/bin/Release/net10.0/<rid>/publish/` — for `win-x64`, exactly the MSI
  profile's `releasePath`.
- `docs/RELEASE.md` §2 publishes today with `dotnet publish … -r win-x64 --self-contained false` (no
  ReadyToRun). The worker (`net472`) is copied into `publish/worker/` by `CopyWorkerOutput.targets` either
  way; ReadyToRun applies to the app's managed assemblies only.

## Steps

1. `packaging/windows/publish.ps1` — the reference adapted to `src\Enigma.Msi.Desktop`.
2. `packaging/windows/publish.bat` — `@echo off`, `setlocal`, project from `%~dp0..\..\src\Enigma.Msi.Desktop`,
   the command, `exit /b %ERRORLEVEL%`; header comment in the `.ps1`'s spirit; CRLF.
3. `packaging/linux/publish.sh` — `#!/usr/bin/env bash`, `set -euo pipefail`, project from the script dir,
   `exec dotnet publish …`; header like `install.sh`'s; `100755`.
4. `docs/RELEASE.md` §2: the publish step becomes `.\packaging\windows\publish.ps1` (same output folder,
   now ReadyToRun); the paragraph explaining framework-dependent stays true.
5. README: the build-from-source section names the three scripts and where their output lands.
6. Verify: run `publish.bat`, `publish.ps1` (win-x64) and `publish.sh` through Git Bash (linux-x64) from a
   directory other than the repo root; each exits 0 and leaves its `publish/` folder with the app, `app.ico`
   and `worker/`.

## Acceptance criteria

- The three scripts run from any directory and exit with `dotnet`'s exit code.
- `publish.sh` is `100755` in the index; `publish.bat` is CRLF in the working tree.
- Release build warning-free; the whole suite green (nothing in it changes).

## Out of scope

- Self-contained publishing, other RIDs, arguments to the scripts, MSI building from a script.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where | `packaging/windows/`, `packaging/linux/` | GitClient's layout; next to the Linux installer | A root `publish/` folder |
| `publish.bat` | Calls `dotnet` itself | No dependency on PowerShell's execution policy | Wrapping `publish.ps1` |
| Arguments | None | The spec fixes the command | Passing `%*`/`"$@"` through |
| Self-contained | Not set (framework-dependent, the SDK default for a RID publish) | The spec's command verbatim; matches the MSI's documented prerequisite | `--self-contained` |
| Release runbook | §2 publishes with `publish.ps1` | One publish command, written once; the MSI ships what the script produces (ReadyToRun) | Keeping a second, slightly different command in the runbook |
