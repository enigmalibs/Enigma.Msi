# FEATURE-43A9-PHASE01 — Repository & solution bootstrap (DONE)

## Summary

Stood up the Enigma.Msi repository and solution as infrastructure-only groundwork for PHASE02–PHASE05.
Created the standard root config set (git hygiene, full C# `.editorconfig`, shared build props,
Central Package Management, SDK/test-runner pin, MIT license), scaffolded a convention-compliant
.NET solution — an empty multi-targeted `Enigma.Msi` library plus an MTP-native xUnit v3 test project
with a single smoke test — and wrote the repo `CLAUDE.md`. No library code beyond the empty project.

Everything is byte-consistent with the Enigma.Core family: the `git-repo-hygiene` and
`dotnet-solution-config` templates were verified identical to Enigma.Core's `.gitignore`,
`.gitattributes` and `.editorconfig` before copying, so "copy the template" and "match the family"
were the same action.

## Files/modules touched

### Created — git & config
- `.gitignore` — Visual Studio / dotnet ignore set, plus an `*.msi` rule for built installers **and a
  paired `!*.msi/` negation** (see *Deviations & follow-ups*)
- `.gitattributes` — `* text=auto eol=lf` + binary rules (incl. `*.ico`)
- `.editorconfig` — full C# code style, naming conventions and analyzer severities

### Created — root docs & build config
- `RELEASENOTES.md` — empty (0 bytes); content deferred to FEATURE-5F00
- `global.json` — SDK `10.0.100` / `rollForward: latestFeature`; `test.runner: Microsoft.Testing.Platform`
- `Directory.Build.props` — Authors "Josué Clément", Copyright © 2026, LangVersion 14, Nullable enable,
  ImplicitUsings disable, TreatWarningsAsErrors, EnforceCodeStyleInBuild
- `Directory.Packages.props` — CPM on; central pins for System.Text.Json 10.0.10, System.Buffers 4.6.1,
  PolySharp 1.16.0, xunit.v3 3.2.2, coverlet.collector 10.0.1
- `LICENSE.md` — MIT, 2026 Josué Clément
- `Enigma.Msi.slnx` — `.slnx` solution referencing both projects under `/src/` and `/tests/`
- `CLAUDE.md` — repo guidance mirroring Enigma.Core's shape (what this is, architecture incl. the
  net472 worker protocol and the load-bearing invariants, layout, TFMs, build/test, conventions,
  dev workflow)

### Created — projects
- `src/Enigma.Msi/Enigma.Msi.csproj` — library, `netstandard2.0;net8.0;net10.0`, docs generation on,
  conditional netstandard2.0-only `System.Buffers` + `PolySharp` groups, and a comment stating the
  TFM rationale (netstandard2.0 so the net472 worker consumes the same model assembly; net8.0/net10.0
  = the current LTS pair)
- `tests/Enigma.Msi.UnitTests/Enigma.Msi.UnitTests.csproj` — MTP-native xUnit v3 project
  (`net8.0;net10.0`, `OutputType=Exe`, `xunit.v3` + `coverlet.collector`, ProjectReference to the library)
- `tests/Enigma.Msi.UnitTests/SmokeTest.cs` — single `[Fact]` proving the toolchain builds and runs green

### Modified — workflow tracking
- `docs/roadmap.md` — FEATURE-43A9 `TODO` → `IN PROGRESS`; PHASE01 `TODO` → `IN PROGRESS` → `DONE`
  (table columns realigned for the wider status values)
- `docs/plan/FEATURE-43A9.md` — item status `TODO` → `IN PROGRESS`; PHASE01 `TODO` → `IN PROGRESS` → `DONE`

### Untouched
- `README.md` — already existed with a `# Enigma.Msi` heading (13 bytes) rather than the planned
  0-byte placeholder. Left as-is per the "create only if missing, never clobber" rule; FEATURE-5F00
  writes the real content anyway.

## Deviations & follow-ups

- **`*.msi` gitignore rule needed a negation — the plan's rule alone silently ignored the whole
  source tree.** Gitignore patterns match directories as well as files, and this repo's project
  directory is literally named `Enigma.Msi`, so a bare `*.msi` excluded `src/Enigma.Msi/` wholesale
  (`git check-ignore -v` confirmed: `.gitignore:203:*.msi → src/Enigma.Msi/Enigma.Msi.csproj`). Fixed
  by pairing it with `!*.msi/`, which re-includes directories only. Verified both ways: a real
  `Setup.msi` file is still ignored, and a directory named `*.msi` is traversed normally. The rule and
  the reason are commented in `.gitignore` and flagged under *Conventions* in `CLAUDE.md` so nobody
  "cleans up" the negation later.
- **No NuGet packaging metadata on the library csproj.** Enigma.Core's own bootstrap carried
  `PackageId`/`Version 0.1.0`; the current house template and this plan both defer *all* packaging
  metadata to release time (FEATURE-5F00), so it was omitted. Deliberate divergence from the
  historical family precedent, aligned with the newer convention.
- **Test csproj drops the template's fixture copy-glob `ItemGroup`.** The template says to keep only
  the extensions the suite actually loads; the bootstrap suite loads none. PHASE02 should add a
  `**/*.json` glob if it introduces on-disk profile fixtures.
- **`System.Text.Json` is pinned but not yet referenced.** Per the plan, so PHASE02 can add the
  conditional `netstandard2.0` reference without touching CPM. An unused `<PackageVersion>` is inert.
- **Line endings (CRLF):** the working-tree files are CRLF (Windows), but `.gitattributes` is in place
  from this same commit and `git check-attr` confirms `text=auto eol=lf` applies to them, so git
  normalizes to LF in the index on the first `add` — no churn, no renormalize pass needed.
  Recommendation-only per `dev-workflow`; **no action taken**.
- **`dotnet test` invocation:** on the .NET 10 SDK in MTP mode, `dotnet test <solution>` is rejected —
  the solution must be passed as `dotnet test --solution Enigma.Msi.slnx`. Recorded in `CLAUDE.md`.

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release`, from a cleaned `bin/`/`obj/` →
  `Build succeeded. 0 Warning(s) 0 Error(s)`, producing `Enigma.Msi.dll` for `netstandard2.0`,
  `net8.0` and `net10.0`, and the test assembly for `net8.0` and `net10.0`.
- **Test:** `dotnet test --solution Enigma.Msi.slnx -c Release` (MTP) →
  `Passed! total: 2, failed: 0, succeeded: 2, skipped: 0` — the smoke test green on both test TFMs.
- **CPM:** a repo-wide grep for `PackageReference … Version=` in `**/*.csproj` returns no matches.
- **Ignore rules:** `git status --short --untracked-files=all` lists exactly the three project source
  files under `src/`/`tests/` and no `bin/` or `obj/` entry.
- **Placeholders:** `wc -c RELEASENOTES.md` → `0`.

## Acceptance criteria — all met

1. ✅ `dotnet build Enigma.Msi.slnx -c Release` — zero warnings.
2. ✅ `dotnet test --solution Enigma.Msi.slnx -c Release` — smoke test passes on net8.0 and net10.0.
3. ✅ All root config files present and consistent with Enigma.Core's conventions (templates verified
   byte-identical before copying); no `Version=` on any `PackageReference`.
