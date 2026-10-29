# FEATURE-D37E — Drop the net8.0 targets

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-d37e-drop-net8`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Remove every `net8.0` target from the solution — .NET 8 leaves support on 2026-11-10 — and keep every
other framework as it is: `netstandard2.0` and `net10.0` for the library, `net472` for the worker and its
suite, `net10.0` for the desktop app.

## Context & constraints

Read on `develop` @ `fed392e` (2026-10-29):

- `src/Enigma.Msi/Enigma.Msi.csproj` — `<TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>`;
  its comments name `net8.0/net10.0` as "the two currently-supported LTS releases" and say the three
  `netstandard2.0`-only packages are framework-provided "on net8.0+".
- `tests/Enigma.Msi.UnitTests/Enigma.Msi.UnitTests.csproj` — `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`,
  and `EnigmaMsiWorkerSourceDir` points at the stub worker's `bin/$(Configuration)/net8.0/`.
- `tests/Enigma.Msi.StubWorker/Enigma.Msi.StubWorker.csproj` — `net8.0`, "the lower of the two the suite
  targets".
- Comments in `Directory.Packages.props` say "net8.0+" about framework-provided packages and coverlet's
  build assets — still true statements about package support, but phrased against the old floor.
- Docs naming the TFM list: `README.md` (§ library), `docs/guides/building.md`, `CLAUDE.md`
  (*Target frameworks & dependencies*). `RELEASENOTES.md`'s past sections are history and stay.
- The library holds at 1.0.0 and is not published (`IsPackable=false`); every consumer is in-repo
  (`net472` worker, `net10.0` app and suites), so no consumer loses a target.

## Steps

1. Library: `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>`; reword the TFM comment
   (`net10.0` is the current LTS; `netstandard2.0` still serves the `net472` worker) and the "net8.0+"
   comments to "net10.0".
2. Library suite: single `<TargetFramework>net10.0</TargetFramework>`; comment updated; the stub worker
   source dir becomes `…/bin/$(Configuration)/net10.0/`.
3. Stub worker: `net10.0`, comment updated.
4. `Directory.Packages.props`: the two "net8.0+" comments reworded (no version change here).
5. Docs: README, `docs/guides/building.md`, `CLAUDE.md` name `netstandard2.0` and `net10.0` only.
6. Build (Release, warning-free) and run the whole suite.

## Acceptance criteria

- `git grep -n "net8"` finds nothing outside `RELEASENOTES.md`'s past sections and `docs/plan|done/`.
- The Release build is warning-free; the whole suite is green (the library suite now runs once, on
  `net10.0`, so the total test count drops by the library suite's former `net8.0` share — recorded).
- `src/Enigma.Msi/bin/Release/` holds `netstandard2.0/` and `net10.0/` outputs only after a clean build.

## Out of scope

- Any package version change (FEATURE-CED6).
- Re-releasing the library or bumping its 1.0.0.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which projects | Library, library suite, stub worker | They are every `net8.0` occurrence; the spec says keep the others | Library only (would leave a suite testing a target that no longer exists) |
| Suite's TFM element | `TargetFramework` (singular) | One target left; the plural form with one value is noise | Keeping `TargetFrameworks` |
| Library version | Stays 1.0.0 | Not published, consumed in-repo; the repo's rule is that the library is not re-released | Bumping to 1.1.0/2.0.0 |
| Release notes | Mentioned in the 1.7.0 section (FEATURE-AB6B), past sections untouched | Notes are history | Rewriting older sections |
