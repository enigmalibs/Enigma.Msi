# FEATURE-43A9-PHASE03 — net472 worker: WixSharp translation & CLI (DONE)

## Summary

Delivered `Enigma.Msi.Worker`, the net472 console exe that is the only process in the solution allowed
to touch WixSharp — and with it the whole model → WixSharp translation, both command-line entry points,
and the drift guards that keep the library's enum mirrors honest.

**This phase produced a real MSI end to end.** `Enigma.Msi.Worker.exe build sample.msipkg.json` compiled
a 2.6 MB `ContosoWidget.msi` on this machine (WiX CLI 7.0.0), so the translation is verified against the
actual toolchain, not only against unit assertions. The plan listed that as a best-effort manual step.

Four points later phases depend on:

- **`MsiPackage` → `ManagedProject` is one pure function.** `MsiProjectFactory.Create(package)` returns a
  fully configured project and builds nothing, so the net472 suite asserts the translated object graph
  (directory tree, shortcuts, cabinets, Control Panel entry, managed-UI dialog sequences) without a WiX
  toolchain. `MsiBuildRunner.Run` is the only place that calls `BuildMsi()`.
- **The mirrors are guarded from both sides.** `WixSharpDriftTests` pins WixSharp's real member sets
  *and* asserts that every WixSharp member the mirrors omit is one we decided to omit — so a member a
  future WixSharp adds fails the suite instead of quietly becoming unreachable.
- **The exit-code contract is honoured exactly**, including the distinction the plan draws: an MSI build
  failure is exit **1** (well-formed input, operation failed), not exit 2. That is why the runner catches
  what `BuildMsi()` throws and turns it into a failed `MsiBuildResult` — PHASE04's client can rely on the
  result file being present and meaningful whenever the exit code is 0 or 1.
- **Both modes funnel through `MsiBuildRunner`**, so the headless CI path and the library-driven path
  cannot drift apart.

## Files/modules touched

### Created — `src/Enigma.Msi.Worker/` (net472, `OutputType=Exe`)
- `Enigma.Msi.Worker.csproj` — net472 (WixSharp is .NET Framework-only), `WixSharp_wix4` +
  `ProjectReference` to `Enigma.Msi` (its netstandard2.0 face), binding redirects on
- `Program.cs` (`Enigma.Msi.Worker`) — entry point, both modes, exit codes `0/1/2`, the top-level
  handler, and the shared package-loading helper
- `WorkerMode.cs` — `Internal` (`--request`/`--result`) | `Cli` (`build <file>`)
- `WorkerArguments.cs` — the strict parser (`Parse(args, out error)` → `null` on rejection) plus the
  `Usage` text printed alongside every usage error
- `MsiBuildRunner.cs` — `ValidateAll` gate → translate → `BuildMsi()` → `MsiBuildResult`
- `Translation/WixEnumMap.cs` — `MapScope`, `MapCompression`, `MapWui`, `MapDialog`; throws on anything
  it does not know
- `Translation/MsiProjectFactory.cs` — `MsiPackage` → `ManagedProject`

### Created — `tests/Enigma.Msi.Worker.UnitTests/` (net472)
- `Enigma.Msi.Worker.UnitTests.csproj` — xunit.v3 only (see *Deviations* on coverlet)
- `TestPackages.cs` — `CreateFull` / `CreateMinimal` fixtures (no disk access needed)
- `Translation/WixEnumMapTests.cs` — per-member theories for all four maps, totality + injectivity
  facts, and out-of-range rejection
- `Translation/WixSharpDriftTests.cs` — the drift guards
- `Translation/MsiProjectFactoryTests.cs` — the translated project graph
- `MsiBuildRunnerTests.cs` — the validation gate (in-memory *and* environment rules)
- `WorkerArgumentsTests.cs` — happy paths plus 15 malformed command lines

### Modified
- `Directory.Packages.props` — pinned `WixSharp_wix4` 2.14.1 (with a note that
  `WixSharp_wix4.bin` must never be referenced directly, and why the net472 suite omits
  `coverlet.collector`)
- `Enigma.Msi.slnx` — added both new projects under `/src/` and `/tests/`
- `docs/roadmap.md`, `docs/plan/FEATURE-43A9.md` — PHASE03 → DONE
- `CLAUDE.md` — the "Build state" callout now records the worker as delivered (and names its CLI form),
  and the layout table's `(PHASE03)` tags are gone; the not-yet-present list narrowed to
  `IMsiBuildService` and the worker-copy targets (documentation freshness sweep, minimal scope as
  chosen)

## Deviations & follow-ups

- **WixSharp 2.14.1's member sets are identical to 2.12.0's** — PHASE02's open item is closed. Verified
  by reflecting over the downloaded 2.14.1 assemblies before writing a line of code, then pinned in
  `WixSharpDriftTests`: `InstallScope` = `perMachine`, `perUser`, `perUserOrMachine`;
  `CompressionLevel` = `none`, `low`, `medium`, `high`, `mszip`; `WUI` = the 7 `WixUI_*` members;
  `WixSharp.Forms.Dialogs` = `Welcome`, `Licence`, `Features`, `InstallDir`, `InstallScope`, `Progress`,
  `SetupType`, `MaintenanceType`, `Exit`. The mirrors therefore need no change, and the two documented
  omissions (`perUserOrMachine`, the `InstallScope` dialog) are now asserted rather than merely stated.
- **Folder named `Translation/`, not `Wix/`.** A `Wix` folder would make the namespace
  `Enigma.Msi.Worker.Wix`, which collides with the `using Wix = WixSharp;` alias the translation files
  use (CS0576). The alias is worth keeping: it makes every crossing into WixSharp visible at the call
  site and sidesteps the real name clashes between the mirrors and WixSharp (`InstallScope`,
  `CompressionLevel`, `Shortcut` all exist in both).
- **`MsiBuildRunner` catches `Exception` around `BuildMsi()` deliberately.** WixSharp surfaces toolchain
  and authoring failures as whatever type the failing step threw, and the exit-code contract counts all
  of them as exit 1. The catch is scoped to the build call only; `Program`'s top-level handler still owns
  genuinely unexpected errors (exit 2). Each exception in the chain becomes one line in
  `MsiBuildResult.Errors`, because WixSharp likes to wrap the actionable message.
- **`coverlet.collector` is omitted from the net472 suite.** The package ships build assets for net8.0+
  only, so referencing it from net472 restores against a non-matching framework and raises NU1701 —
  which `TreatWarningsAsErrors` turns into a build error. The library suite still collects coverage.
  Follow-up if worker coverage is ever wanted: run OpenCover/AltCover against the test exe directly.
- **The plan's xunit-on-net472 risk did not materialise.** `dotnet test --solution` drives the net472
  test exe through MTP without any special handling (`xunit.v3` 3.2.2 declares a `net472` dependency
  group). No fallback to `Microsoft.NET.Test.Sdk` was needed.
- **Compression is applied to *every* `Media` entry, and a missing one is seeded.** MsiBuilder reached
  for `Media.FirstOrDefault()?.CompressionLevel`, which would have dropped the setting silently had the
  list been empty. WixSharp does seed one entry today; the worker no longer depends on that.
- **`Dir.IsInstallDir` has no public getter**, so the directory tests identify the install directory as
  the single `Dir` carrying file collections and separately assert that exactly one top-level `Dir` is a
  WixSharp `InstallDir`. Worth knowing: WixSharp expands a multi-segment path into a chain of nested
  `Dir`s at construction time, so `%ProgramFiles%\Contoso\Widget` becomes three objects and the files
  land on the leaf — the tests rebuild the path by walking the chain.
- **Shortcut `IconFile`/`Arguments` are left at WixSharp's empty-string defaults** when the model carries
  none, rather than having the model's `null` pushed through.
- **`--help` is not special-cased.** Per the plan's contract, anything outside the two documented forms
  is a usage error: `--help` prints the reason plus `Usage` and exits 2.
- **Not delivered here, by design:** the worker-output copy target and the `build/Enigma.Msi.targets`
  bundling layout belong to PHASE04, and nothing references the worker at build time yet — PHASE04's
  `MsiBuildService` is what discovers it on disk.
- **Line endings (CRLF):** no churn observed; all new files are LF and `.gitattributes` governs them.
  Recommendation-only per `dev-workflow`; **no action taken**.

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`,
  producing `Enigma.Msi.Worker.exe` (net472) alongside the library's three targets.
- **Test:** `dotnet test --solution Enigma.Msi.slnx -c Release` (MTP) →
  `Passed! total: 265, failed: 0, succeeded: 265, skipped: 0` — 186 in the library suite (93 × net8.0,
  net10.0) plus **79 new tests** in `Enigma.Msi.Worker.UnitTests` (net472).
- **Exit-code contract, exercised against the built exe:**

  | Command | Expected | Observed |
  |---|---|---|
  | `build invalid.msipkg.json` (9 violations) | 1, all errors printed | 1, all 9 printed |
  | `build broken.msipkg.json` (malformed JSON) | 2 | 2, with the STJ position in the message |
  | `build does-not-exist.msipkg.json` | 2 | 2 |
  | `--request … --result … --verbose` | 2 + usage | 2, `Unknown argument '--verbose'.` + usage |
  | *(no arguments)* | 2 + usage | 2, `No arguments were given.` + usage |
  | `--request sample --result out` (valid) | 0 | 0, `{"success": true, "msiPath": …}` written |
  | `--request invalid --result out` | 1 | 1, result file carries all 9 violations |
  | `--request broken --result out` | 2 | 2, result file still written with the reason |

- **End-to-end MSI build (the plan's manual acceptance step): performed and successful.** WiX CLI
  `7.0.0`; `build sample.msipkg.json` on a package with a recursive release folder (`Widget.exe` +
  `sub/notes.txt`), a nested Start-menu shortcut, `%ProgramFiles%\Contoso\Widget` install path, `High`
  compression and a partial Control Panel entry produced
  `out\ContosoWidget.msi` (2 609 152 bytes), exit code 0, with WixSharp reporting
  `ProductName: Contoso Widget / Version: 1.2.3` and the expected product/upgrade GUIDs. All fixtures
  lived outside the repository.
- **Drift-guard proof:** `WixSharpInstallScope/CompressionLevel/Wui/Dialogs_StillHasThePinnedMembers`
  compare the pinned name sets against reflection over the referenced assemblies;
  `*Mirror_NamesAWixSharpMember*` and `*Mirror_Omits*` close both directions;
  `MapDialog_ResolvesToTheSameNamedWixSharpDialogsField` resolves each mirror member by name through
  reflection and asserts the map returns that exact `Type`.

## Acceptance criteria — all met

1. ✅ **Every model enum member maps; drift guards pass against WixSharp 2.14.1.** Per-member theories
   cover all 2 + 5 + 7 + 8 mirror members; `Map*_MapsEveryMirrorMemberToADistinctValue` proves each map
   is total (it throws on anything it forgot) and injective; the drift tests pin WixSharp 2.14.1's four
   member sets and both documented omissions.
2. ✅ **`build <validation-failing.msipkg.json>` exits 1 with all validation errors printed; unparseable
   JSON and unknown arguments exit 2 (with usage for the latter).** See the exit-code table — observed
   against the built exe, and covered by `MsiBuildRunnerTests` + `WorkerArgumentsTests`.
3. ✅ **Build + full suite green, zero warnings** — and the manual MSI build the criterion asks for
   "when possible" was possible: a real `ContosoWidget.msi` was produced on Windows with the wix tool.
