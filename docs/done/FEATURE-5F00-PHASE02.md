# FEATURE-5F00-PHASE02 — Guides & index (DONE)

## Summary

Added `docs/guides/` — the per-category guides for the library, plus their index. **Five** guides, not the
plan's suggested four: validation got its own instead of being folded into the model guide (see
*Deviations & follow-ups*). Serialization stayed inside the model guide as planned, since the
`.msipkg.json` format *is* the model's serialization.

The plan's snippet-verification gate was run as a **compile-and-execute harness**, not as a read-through.
Every C# snippet was extracted into a throwaway project outside the repository, referencing the built
`Enigma.Msi.dll` directly, and compiled with `Nullable=enable` and `TreatWarningsAsErrors`-equivalent
scrutiny; on top of that, the guides' factual claims that a compiler cannot check — enum member sets,
model defaults, constant values, quoted validator messages — were asserted at run time against the real
assembly, and the JSON samples and the CLI transcripts were **generated from the library and the worker**
rather than hand-written. That is what makes the numbers in the coverage table below mean something.

The gate found one real mismatch, in prose rather than in code: the desktop guide described the Problems
and Build log panes as sitting *below* the form, when `MainWindow.axaml` puts them in a splitter-resizable
right-hand column. Corrected against the actual layout.

One thing verified beyond the plan's ask, because the toolchain happened to be available: `wix` 7.0.0 is
installed on this machine, so the worker CLI's **success** path was exercised end to end — a real 2.6 MB
MSI was built from a scratchpad profile — confirming the documented `MSI written to <path>` line and exit
code `0`. This is *not* the PHASE04 dogfood acceptance (that builds the desktop app's own installer from a
committed profile); it is snippet verification that happened to reach all the way to a real MSI.

## Files/modules touched

### Created
- `docs/guides/README.md` — the index. Two-line restatement of the core idiom so it stands alone, the
  Windows + `wix` prerequisite up front, guides grouped under three themes (the model / building / the
  application).
- `docs/guides/model.md` — `MsiPackage` field-by-field with defaults and required-ness, the five nested
  types, the four WixSharp-free enums with their full member sets, four usage scenarios, the `MsiPackageJson`
  API, and the profile format shown both fully populated and minimal.
- `docs/guides/validation.md` — the three-method surface, the complete in-memory and environment rule
  tables keyed by profile path, four usage scenarios, DI registration.
- `docs/guides/building.md` — `IMsiBuildService` with its real signatures, `MsiPrerequisites` and
  `MsiBuildResult` member tables, five usage scenarios (build, pre-flight, streamed log, cancellation,
  relocated worker), and a worker-discovery section covering `DefaultWorkerPath`, the three constants and
  the two MSBuild opt-out properties.
- `docs/guides/worker-cli.md` — both command-line forms, the strict-parsing rationale, the `0`/`1`/`2`
  exit-code contract, the real stdout/stderr shapes, where the worker lives in each context, a CI job, and
  exit-code branching in PowerShell and bash.
- `docs/guides/desktop-app.md` — the window and its command bar, the five form sections, what a build does
  step by step, the profile round-trip guarantees, and an end-to-end workflow.
- `docs/done/FEATURE-5F00-PHASE02.md` (this file).

### Modified
- `docs/roadmap.md`, `docs/plan/FEATURE-5F00.md` — PHASE02 status.
- `CLAUDE.md` — documentation freshness sweep: the build-state callout now records PHASE02 as done and
  points at `docs/guides/` (it previously listed the guides under "still to come"), and the project-layout
  block names `docs/guides/` with its never-packed/relative-links-are-fine caveat.

No source file was touched: this phase adds documentation only.

## Snippet-verification gate

**Method.** Snippets were extracted verbatim into `Snippets.csproj` (net10.0, `Nullable=enable`,
`ImplicitUsings=disable`, `LangVersion=14`) in the scratchpad, referencing
`src/Enigma.Msi/bin/Release/net10.0/Enigma.Msi.dll` as a plain assembly reference so **nothing was ever
written into the repository**. Each snippet kept its own `using` block and body unchanged, wrapped only in
a method so it could compile. Result: **0 errors, 0 warnings**, re-confirmed against the assembly produced
by the final Release build of the solution.

"Symbols" below counts distinct public types and members of `Enigma.Msi` named anywhere in the file — in
prose, tables or code — each checked against `src/`.

| File | Code fences | of which C# (compiled) | Symbols | Mismatches | Uncertain |
|---|---|---|---|---|---|
| `README.md` (index) | 0 | 0 | 6 | 0 | 0 |
| `model.md` | 8 | 6 | ~72 | 0 | 0 |
| `validation.md` | 5 | 5 | 18 (+20 rule paths) | 0 | 0 |
| `building.md` | 8 | 7 | 29 (+2 MSBuild properties) | 0 | 0 |
| `worker-cli.md` | 8 | 0 | 0 (CLI surface, not API) | 0 | 2 (see below) |
| `desktop-app.md` | 1 | 0 | 0 (UI surface, not API) | **1 — fixed** | 0 |
| **Total** | **30** | **18** | — | **1 found, 1 fixed, 0 unresolved** | **2** |

**Verified by execution, not by reading:**

- Both `.msipkg.json` samples in `model.md` are the library's own `MsiPackageJson.Serialize` output,
  transcribed. This caught something a hand-written sample would have got wrong: member order follows the
  model's declaration order (`scope` before `install`/`output`, `compression` after `output`), and
  `shortcuts` is emitted as `[]` while `controlPanel`/`ui` are omitted entirely — an empty list is not a
  missing one.
- Enum member sets for `InstallScope`, `CompressionLevel`, `Wui` and `Dialog` asserted element-by-element
  against `Enum.GetNames<T>()`.
- Every `MsiPackage` default in the field table asserted against a freshly constructed instance.
- `UiSettings.CreateDefault()`'s dialog set and `Wui` asserted, since three guides describe it.
- `MsiBuildServiceOptions.WorkerFileName`/`WorkerFolderName`/`DefaultWixToolPath` and
  `MsiPackage.CurrentSchemaVersion` asserted to equal the values the guides print.
- The guide's `IMsiBuildService` signature block is proven by a class that implements *both* it and the
  real interface — if the block drifts, the harness stops compiling.
- The three validator messages quoted in `worker-cli.md` asserted to be messages the validator actually
  produces, and its 3-error transcript proven **reproducible exactly** — same three lines, same order,
  same count — from a constructed package.
- `validation.md`'s no-double-reporting claim asserted: a blank `install.releasePath` yields exactly one
  error from `ValidateAll`.
- The worker's usage text, and exit codes `2` (no arguments; the `--results` typo the guide names
  explicitly; a missing profile) and `1` (well-formed profile, failing validation) run against the real
  `net472` binary; output matches the guide verbatim.
- Exit code `0` and `MSI written to <path>` run end to end with `wix` 7.0.0.

**The two uncertain items**, both in `worker-cli.md`, are recorded rather than glossed:

1. The `nuget install Enigma.Msi …` snippet was **not executed** — it needs `nuget.exe` (absent here) and
   a published package (there is none until PHASE05). Syntax and the resulting
   `packages\Enigma.Msi\tools\worker\` path were checked against `-ExcludeVersion` semantics and PHASE01's
   verified pack layout, but not run.
2. The GitHub Actions YAML was **not executed** — the repository has no remote and no workflows yet. It is
   well-formed and its worker path matches the verified `$(OutDir)worker\` layout.

## Deviations & follow-ups

- **Deviation: five guides instead of the plan's suggested four.** The plan states the count "follows the
  library, not a target" and says to "adjust the set to the code as built", so this is within its terms
  rather than against them. `validation.md` was split out because `IMsiPackageValidator` is a distinct
  consumer-facing capability with its own interface, three methods, two rule sets and ~20 documented
  rules — CLAUDE.md calls the aggregating validator a load-bearing invariant, and burying it in the model
  guide would have made that guide the longest and least navigable of the set.
- **Deviation: the plan's suggested `model.md` scope was widened slightly** to carry the `MsiPackageJson`
  API alongside the `.msipkg.json` example, since the plan folds serialization into that guide and the
  format cannot be documented usefully without the type that reads and writes it.
- The `docs/guides/` files use relative links to each other. Correct here — these files are never packed;
  PHASE03's packed root README must point at the guides in prose instead, as its plan already says.
- Under the `net472` console's default codepage the em dash in `output.msiFilename`'s message renders as
  `-`. The message itself carries an em dash (the guide quotes it faithfully); this is a console-encoding
  artifact, not a discrepancy, and needs no change.
- **Follow-up for PHASE03:** the guides deliberately contain no version numbers and no dependency
  versions, so nothing here needs revisiting at each release — but the README written in PHASE03 should
  link the guides in prose and not duplicate their tables, or the two will drift.
- **Follow-up (outside this phase):** `worker-cli.md`'s two unexecuted snippets become verifiable once
  PHASE05 publishes the package and a remote exists. Worth re-checking then.
- Line endings: no CRLF churn; the six new files and the two modified ones are LF-only, and the diff is
  content-only.

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`.
- **Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release` → **368 passed, 0 failed, 0 skipped**
  across all four suites (`Enigma.Msi.UnitTests` on net8.0 and net10.0, `Enigma.Msi.Worker.UnitTests` on
  net472, `Enigma.Msi.Desktop.UnitTests` on net10.0). **No tests were added:** this phase adds prose and
  code samples only, and the samples' correctness is enforced by the compile-and-execute harness described
  above rather than by a suite. Committing the harness as a suite was considered and rejected — it would
  put a doc-snippet project in the solution and in every future build, to guard text that a release-time
  gate already covers.
- **Snippet harness:** `dotnet build` of the extracted snippets → 0 errors, 0 warnings; runtime assertion
  pass → `ALL CHECKS PASSED`; CLI transcript check → `worker-cli.md transcript: REPRODUCED EXACTLY`.
- **Real MSI:** `Enigma.Msi.Worker.exe build <scratchpad profile>` → exit `0`, 2 600 960-byte
  `ContosoWidget-1.0.0.msi`, with the documented success line.
- Everything the harness produced lives outside the repository and has been removed; `git status` shows
  only `docs/guides/`, `docs/done/FEATURE-5F00-PHASE02.md`, `docs/roadmap.md` and
  `docs/plan/FEATURE-5F00.md`.
