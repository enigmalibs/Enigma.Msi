# FEATURE-43A9-PHASE02 — MsiPackage model, validation & serialization (DONE)

## Summary

Delivered the library's whole declarative surface: the `MsiPackage` model graph (plus the enum mirrors
that keep WixSharp off the public API), the `.msipkg.json` serialization contract, and the aggregating
validator. This is the single representation every later phase consumes — PHASE03's worker
deserializes it and maps it to WixSharp, PHASE04's client ships it to the worker and reads
`MsiBuildResult` back, PHASE05's ViewModel materializes it. No mirrored DTO layer exists anywhere.

Three design points are worth stating because later phases depend on them:

- **Required-ness is a validation concern, not a deserialization one.** The model is a mutable data
  carrier with permissive defaults instead of `required` members, so a half-filled package can be
  loaded, edited and reported on — which is exactly what the desktop app needs. `MsiPackageValidator`
  is what says "this is incomplete", returning *every* violation as `{ Path, Message }` with the
  member path in the profile's own JSON casing (`shortcuts[1].targetPath`).
- **The in-memory and environment rule sets are separate methods** (`Validate` /
  `ValidateEnvironment`, with `ValidateAll` for the build path). The in-memory set provably never
  touches the disk — there is a test for it — so a UI can run it on every keystroke.
- **The enum mirrors are deliberate subsets** of WixSharp's member sets, and their member names match
  WixSharp's verbatim (`WixUI_InstallDir`, the British `Licence`) so PHASE03's drift guards can compare
  the two sets by name.

## Files/modules touched

### Created — `src/Enigma.Msi/Model/` (namespace `Enigma.Msi.Model`)
- `MsiPackage.cs` — the root document: `SchemaVersion` (+ `CurrentSchemaVersion = 1`), `AppName`,
  `Version`, `ProductId`/`UpgradeCode` (real `Guid`s), `Manufacturer`, `Scope`, `Install`, `Output`,
  `Compression` (defaults to `High`), `ControlPanel?`, `Shortcuts`, `Ui?`
- `InstallSettings.cs` — `InstallPath`, `ReleasePath`
- `OutputSettings.cs` — `OutputPath`, `MsiFilename` (extension-free)
- `ControlPanelInfo.cs` — `ProductIcon?`, `Comments?`, `Contact?`, `HelpLink?`, `UrlInfoAbout?`
- `Shortcut.cs` — `ShortcutPath`, `ShortcutName`, `TargetPath`, `IconPath?`, `Arguments?`
- `UiSettings.cs` — `Wui`, `InstallDialogs`, `ModifyDialogs`, plus `CreateDefault()`
- `InstallScope.cs`, `CompressionLevel.cs`, `Wui.cs`, `Dialog.cs` — the WixSharp-free enum mirrors

### Created — `src/Enigma.Msi/Build/` (namespace `Enigma.Msi.Build`)
- `MsiBuildResult.cs` — `{ Success, MsiPath?, Errors }` + `Succeeded`/`Failed` factories

### Created — `src/Enigma.Msi/Serialization/` (namespace `Enigma.Msi.Serialization`)
- `MsiPackageJson.cs` — the shared `JsonSerializerOptions` (camelCase, indented, enums as member
  names, nulls omitted, case-insensitive reads) and `Serialize`/`Deserialize` +
  `SerializeResult`/`DeserializeResult`
- `VersionJsonConverter.cs` — `Version` as `"1.2.3"` instead of STJ's object shape
- `GuidJsonConverter.cs` — accepts every `Guid.TryParse` form (incl. `{…}` from Visual Studio),
  always writes the canonical hyphenated form

### Created — `src/Enigma.Msi/Validation/` (namespace `Enigma.Msi.Validation`)
- `IMsiPackageValidator.cs` — `Validate` / `ValidateEnvironment` / `ValidateAll`
- `MsiPackageValidator.cs` — the rule sets (stateless, thread-safe)
- `MsiValidationResult.cs` — `{ IsValid, Errors }`
- `MsiValidationError.cs` — `{ Path, Message }`, `ToString()` → `path: message`

### Created — `tests/Enigma.Msi.UnitTests/`
- `TestPackages.cs` — the `CreateFull` / `CreateMinimalValid` fixtures
- `Model/FullPackageCoverageTests.cs` — reflection guard: `CreateFull` must populate every model
  member (so the round-trip test cannot silently stop covering a newly added field)
- `Model/MsiPackageDefaultsTests.cs` — the model's default contract, `UiSettings.CreateDefault()`
- `Build/MsiBuildResultTests.cs` — factories, multi-error carrying, snapshot semantics, null guards
- `Serialization/MsiPackageJsonTests.cs` — round-trips, wire shape, unknown-member tolerance, result
  document
- `Serialization/VersionJsonConverterTests.cs`, `Serialization/GuidJsonConverterTests.cs` — converter
  edge cases
- `Validation/MsiPackageValidatorTests.cs` — one test per in-memory rule + the 18-violation
  aggregation test
- `Validation/MsiPackageValidatorEnvironmentTests.cs` — disk rules against real temp directories

### Modified / deleted
- `src/Enigma.Msi/Enigma.Msi.csproj` — added the netstandard2.0-only `System.Text.Json`
  `PackageReference` (framework-provided on net8.0+, where referencing it would raise NU1510)
- `docs/roadmap.md`, `docs/plan/FEATURE-43A9.md` — PHASE02 → DONE
- `CLAUDE.md` — the "Bootstrap state" callout became "Build state": the model/serialization/validation
  now exist, PHASE03–PHASE05 remain, and the types those phases deliver are flagged as not-yet-present
  (documentation freshness sweep)
- **Deleted** `tests/Enigma.Msi.UnitTests/SmokeTest.cs` — its own doc comment scheduled it for
  replacement by this phase's real tests

## Deviations & follow-ups

- **`MsiBuildResult` lives in `Build/`, not with the model.** The plan lists it among PHASE02's model
  types; it is a build *outcome*, and PHASE04's `IMsiBuildService`/options/preflight types belong in
  the same folder. Namespace `Enigma.Msi.Build`. No behavioural difference.
- **The enum mirrors are subsets, verified against the real WixSharp member sets.** Reflected over the
  locally cached `WixSharp_wix4.bin` **2.12.0** assemblies: `InstallScope` = `perMachine`, `perUser`,
  `perUserOrMachine`; `CompressionLevel` = `high`, `medium`, `low`, `mszip`, `none`; `WUI` = the 7
  `WixUI_*` members (all mirrored); `WixSharp.Forms.Dialogs` fields = `Welcome`, `Licence`, `Features`,
  `InstallDir`, `InstallScope`, `Progress`, `SetupType`, `MaintenanceType`, `Exit`.
  - `InstallScope` mirrors only `PerMachine`/`PerUser`, exactly as the plan specifies — dual-purpose
    packages are out of scope at v1.
  - Consequently `Dialog` omits WixSharp's `InstallScope` **dialog** (that dialog only has something to
    switch in a dual-purpose package). This also matches MsiBuilder's mirror, which omitted it.
  - **PHASE03 must re-verify against WixSharp 2.14.1**: only 2.12.0 is in the local NuGet cache, so
    2.14.1's member sets were not inspected here. That is precisely what the phase's drift guards are
    for; if 2.14.1 added or renamed members, they fail loudly there.
- **Added `UiSettings.CreateDefault()`** (not in the plan's type list) so the default dialog sequences —
  `WixUI_InstallDir`, Welcome/InstallDir/Progress/Exit and Welcome/MaintenanceType/Progress/Exit — are
  defined **once**. PHASE03's worker applies it when `Ui` is null; PHASE05 can pre-fill the editor with
  it. Without it the same list would be hand-copied into both.
- **Validation rules beyond the plan's bullet list.** The plan names "required fields, GUID non-empty,
  version shape, dialog lists non-empty"; the implementation reads those as: `schemaVersion` bounds
  (a profile from a future schema is reported, not silently misread); the Windows Installer version
  range (major/minor ≤ 255, build ≤ 65535 — otherwise the MSI build fails much later with a worse
  message); `msiFilename` must be a plain file name and must not carry `.msi` (WixSharp's
  `OutFileName` is extension-free, so `Widget.msi` would produce `Widget.msi.msi`); every enum value
  must be a defined member (protects PHASE03's mapping from an out-of-range cast); and each
  non-nullable section is null-guarded, because `"install": null` in a hand-edited profile does reach
  the property.
- **One environment rule goes past "paths exist":** an existing but empty `install.releasePath` is
  reported ("nothing to package"), since it otherwise yields a silently contentless MSI. Checked with a
  single `EnumerateFileSystemEntries` probe, and `IOException`/`UnauthorizedAccessException` on it is
  reported as a validation error rather than escaping into a UI command.
- **`JsonSerializerOptions.MakeReadOnly()` was removed after it threw.** It requires a
  `TypeInfoResolver` (`InvalidOperationException` at type-init, which failed 76 test executions on the
  first run); the reflection-populating overload is trim/AOT-annotated, and STJ seals the instance on
  first use anyway. The XML doc now says "shared — never mutate".
- **`PropertyNameCaseInsensitive = true`** was added to the options (not specified in the plan):
  profiles are hand-editable, and a casing slip should not silently drop a field.
- **PHASE01's open question about a test fixture glob is answered: not needed.** All fixtures are
  in-code (`TestPackages`), and the environment tests build their own temp directories, so the test
  csproj needs no `**/*.json` copy item.
- **Line endings (CRLF):** no churn observed — the new files are LF and `.gitattributes` already
  governs them. Recommendation-only per `dev-workflow`; **no action taken**.

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release --no-incremental` →
  `Build succeeded. 0 Warning(s) 0 Error(s)`, producing `Enigma.Msi.dll` for `netstandard2.0`, `net8.0`
  and `net10.0`. The netstandard2.0 target compiles the whole model/serialization/validation surface,
  which is what lets PHASE03's net472 worker consume it.
- **Test:** `dotnet test --solution Enigma.Msi.slnx -c Release` (MTP) →
  `Passed! total: 186, failed: 0, succeeded: 186, skipped: 0` — 93 tests on each of net8.0 and net10.0.
- **Aggregation proof:** `MultiInvalidPackage_ReportsEveryViolationWithItsMemberPath` asserts the exact
  ordered list of **18** member paths returned for a package broken in 18 ways (`schemaVersion`,
  `appName`, `manufacturer`, `version`, `productId`, `upgradeCode`, `scope`, `compression`,
  `install.installPath`, `install.releasePath`, `output.outputPath`, `output.msiFilename`,
  `shortcuts[0].{shortcutPath,shortcutName,targetPath}`, `ui.wui`, `ui.installDialogs`,
  `ui.modifyDialogs[0]`).
- **Round-trip proof:** the full-fixture test asserts that re-serializing the deserialized package
  yields **byte-identical** JSON, so any member lost on the way back changes the document — and
  `FullPackageCoverageTests` proves the fixture populates every member of the graph in the first place.
- **Not verified here (out of PHASE02 scope):** nothing in this phase touches WixSharp or builds an
  MSI, so no end-to-end MSI acceptance was attempted; WixSharp 2.14.1's enum member sets remain
  unverified until PHASE03 (see *Deviations*).

## Acceptance criteria — all met

1. ✅ **Round-trip preserves every field; unknown JSON fields ignored.**
   `RoundTrip_FullPackage_PreservesEveryField` (byte-identical re-serialization + explicit per-member
   assertions), `RoundTrip_MinimalPackage_PreservesFieldsAndDefaults`,
   `Deserialize_IgnoresUnknownMembers`, `Deserialize_EmptyObject_LeavesModelDefaults`.
2. ✅ **The validator returns all violations for a deliberately multi-invalid package, each with a
   member path.** See *Aggregation proof* above; `MsiValidationError.Path` carries the JSON-cased path
   for every violation, including collection indices.
3. ✅ **Build + full test suite green, zero warnings.** See *Build/test evidence*.
