# FEATURE-D37E — Drop the net8.0 targets

## Summary

.NET 8 leaves support on 2026-11-10, so every `net8.0` target is gone. The library now targets
`netstandard2.0;net10.0`, and its test suite and the stub worker it spawns target `net10.0` only. Every other
framework is untouched: `net472` for the worker and its suite, and `net10.0` for the desktop app and its
suite. The library stays at 1.0.0, and all of its consumers are in-repo and none of them used `net8.0`.

## Files/modules touched

- Modified:
  - `src/Enigma.Msi/Enigma.Msi.csproj`
  - `tests/Enigma.Msi.UnitTests/Enigma.Msi.UnitTests.csproj`
  - `tests/Enigma.Msi.StubWorker/Enigma.Msi.StubWorker.csproj`
  - `Directory.Packages.props` (comment only)
  - `README.md`
  - `docs/guides/building.md`
  - `CLAUDE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-D37E.md`
- Created: `docs/done/FEATURE-D37E.md`

## Deviations & follow-ups

- **Two `net8.0` mentions stay on purpose.** They are the comments explaining why the `net472` worker suite
  omits `coverlet.collector` (in `Directory.Packages.props` and the worker suite's csproj). Both describe the
  package's own `build/net8.0` assets, which is still true, and not one of this repo's targets. The plan's
  "`git grep net8` finds nothing" criterion is therefore met with these two exceptions, plus
  `RELEASENOTES.md`'s past sections, which are history.
- The total test count drops from 471 to 354. The 117 tests that went are the library suite's `net8.0` run
  of the same cases.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The test suite's comment | It now says the `netstandard2.0` build is exercised through the `net472` worker suite | The old wording claimed `net8.0` covered the polyfill surface, which a `net8.0` consumer never reaches (it binds the `net8.0` build) |
| The CLAUDE.md wording | States that `net8.0` was dropped and names this item | A future reader of the TFM list learns why it is shorter |

## Documentation sweep

- `README.md`, `docs/guides/building.md` and `CLAUDE.md` now name `netstandard2.0` and `net10.0` only.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release` after deleting the three projects' `bin/Release`: succeeded,
  **0 warnings**, 0 errors. `src/Enigma.Msi/bin/Release/` holds `net10.0/` and `netstandard2.0/` only.
- `dotnet test --solution Enigma.Msi.slnx -c Release`: **354/354 passed**, 0 failed, 0 skipped, broken down
  as:
  - worker suite (`net472`, drift guards included);
  - library suite (`net10.0`);
  - desktop suite (`net10.0`).
- No tests were added: no behaviour changed, only the target matrix.
