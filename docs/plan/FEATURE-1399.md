# FEATURE-1399 — Default cabinet compression to `None`

**Status:** DONE
**Type:** FEATURE (single-phase)
**Branch:** `feature/feature-1399-compression-none-default` — cut from the run-branch tip.
**Run:** vibe/2026-10-22-wixsharp-dir-compression

## Objective

Building an MSI at the default compression, `High`, takes far too long. **Make `CompressionLevel.None` the
default** — for a package created in code (`new MsiPackage()`), for a new package in the desktop app
(startup and *New*) and for a quick-start package. `High` stays one selection away.

## Context & constraints

Read from the repository at planning time (2026-10-22):

- The default lives in three places: `MsiPackage.Compression` (`= CompressionLevel.High`, with an XML doc
  saying so), and `PackageEditorViewModel` — the `_compression` field initializer and `Reset()`.
  `ApplyQuickStart` goes through `Reset()`. `CompressionLevel`'s XML docs call `High` "the library's
  default".
- `MsiPackageJson` writes with `DefaultIgnoreCondition = WhenWritingNull`; `compression` is a non-nullable
  enum, so **every saved profile carries it explicitly** and opens unchanged. Only JSON that omits the member
  — hand-written, or `{}` — changes meaning: it now reads as `None`.
- Tests pinning `High` as the default: `MsiPackageDefaultsTests.NewPackage_HasEditableSectionsAndFamilyDefaults`,
  `MsiPackageJsonTests.RoundTrip_MinimalPackage_PreservesFieldsAndDefaults` and
  `Deserialize_EmptyObject_LeavesModelDefaults`, `PackageEditorViewModelTests.ApplyQuickStart_ReplacesTheWholePackage_WithFreshIdentifiers`.
- `docs/guides/model.md` states the default twice (the members table, the enums table), shows two JSON
  samples written from packages that leave `Compression` unset, and has a sample section whose premise is
  that `None` is a non-default opt-in.
- The committed `msiProfiles/*.msipkg.json` all state `"compression": "High"` explicitly.
- The library stays at **1.0.0** — it has no release flow (`IsPackable=false`, in-repo consumer only).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where does the default change? | The library model **and** the desktop app's two sites | The app's default mirrors the model's; changing only one leaves a lying default | App only (the library's documented default would still be `High`); model only (the app hard-codes its own) |
| Keep the app's two hard-coded sites? | Yes, set to `None`, plus a test that a fresh form's compression equals `new MsiPackage().Compression` | Matches how the form states every other default (`Scope` likewise); the test stops the two from drifting apart again | Reading `new MsiPackage().Compression` in the ViewModel (a style the form uses nowhere else) |
| Committed `msiProfiles/*`? | Untouched | Each states its compression explicitly, and each is the history of a release | Switching the 1.4.0 dogfood profile to `None` |
| Profiles that omit `compression`? | Read as `None` from now on; said so in the release notes | It is what a default means; saved profiles always carry the member, so only hand-written JSON is affected; no `schemaVersion` bump (the format did not change) | A schema bump; a migration |
| `docs/guides/model.md`'s "per-user, uncompressed package" sample? | Turned into a per-user package that opts back into `High` for a release build | Its premise — `None` as the opt-in — is no longer true, and the opposite trade-off is now the one worth showing | Deleting it; leaving an explicit `None` that now restates the default |
| Release? | None; an entry under the `## (unreleased)` section of `RELEASENOTES.md` | Not asked for | A 1.5.0 release prep |
| Library version? | Stays 1.0.0 | No release flow; RELEASENOTES records the change against the app | A library bump |

## Steps

1. `src/Enigma.Msi/Model/MsiPackage.cs` — `Compression` defaults to `CompressionLevel.None`; XML doc updated.
2. `src/Enigma.Msi/Model/CompressionLevel.cs` — "the library's default" moves from `High` to `None`.
3. `src/Enigma.Msi.Desktop/ViewModels/PackageEditorViewModel.cs` — the field initializer and `Reset()` use
   `CompressionLevel.None`.
4. Tests — the four pins above move to `None`; add `PackageEditorViewModelTests` cases: a fresh form and a
   `Reset()` both match the model's default.
5. Docs — `docs/guides/model.md` (both tables, both JSON samples, the sample section); `RELEASENOTES.md`
   `(unreleased)` entry; `CLAUDE.md` build-state entry.

## Acceptance criteria

- [ ] `new MsiPackage().Compression == CompressionLevel.None`, and `MsiPackageJson.Deserialize("{}")` yields `None`.
- [ ] The desktop app starts, resets (*New*) and applies a quick start at `None`.
- [ ] A profile that states `"compression": "High"` still opens and round-trips as `High`.
- [ ] Every `High`-as-default pin is updated (not deleted), the new form tests pass, the whole suite passes,
      and the build is warning-free.
- [ ] No doc still calls `High` the default.

## Out of scope

- `BUG-7325` (the worker's `.wixsharp` directory).
- Changing the committed dogfood profiles' explicit `"High"`.
- A version bump, a new profile or an MSI release.
