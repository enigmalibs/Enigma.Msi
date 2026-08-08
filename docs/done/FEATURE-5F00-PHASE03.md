# FEATURE-5F00-PHASE03 — README, release notes & community files (DONE)

## Summary

Wrote the release documentation the package actually ships with. `README.md` and `RELEASENOTES.md` were
placeholders (13 bytes and 0 bytes respectively) held back deliberately until this phase; both are now
authored, `SECURITY.md` is added, and `<PackageReleaseNotes>` in `src/Enigma.Msi/Enigma.Msi.csproj` no
longer carries its PHASE01 placeholder. With this, the nupkg no longer contains an empty landing page —
the one remaining blocker on publishing that was recorded in `CLAUDE.md`.

Three things shaped the content beyond the templates:

- **The packed README's link rule was applied strictly.** The README is packed as
  `PackageReadmeFile`, so it renders on nuget.org where the repository tree is absent. The only links in
  it are `LICENSE.md`, `RELEASENOTES.md` and the two badges' targets; the guides are named in prose
  (`docs/guides/`, indexed by `docs/guides/README.md`) with no clickable per-guide links and no absolute
  GitHub URLs. PHASE02's follow-up — "the README should link the guides in prose and not duplicate their
  tables, or the two will drift" — is honoured: the README carries no member tables at all.
- **The Windows + `wix` prerequisite got its own subsection** (`### Requirements for building an MSI`)
  rather than a passing mention, because it is the difference between "the package doesn't work" and
  "you need one global tool". It names `CheckPrerequisitesAsync` as the API that reports it.
- **`RELEASENOTES.md` covers both artifacts of the release** — the library and the desktop app — in one
  document, as the plan requires (single notes source, no `CHANGELOG.md`). Its *Dependencies* section is
  transcribed from `Directory.Packages.props`, split by consumer (library per-TFM / bundled worker /
  desktop app), and records that the four Avalonia-coupled packages move as a set.

## Files/modules touched

### Created
- `SECURITY.md` — from the house template. Supported-versions table at `1.0.x`, GitHub private
  vulnerability reporting (no email address, per the template's rationale). The intro and *Scope* were
  written for this package specifically: an MSI runs elevated (`SYSTEM` for a per-machine install), so a
  defect can affect every target machine and not just the build host; scope names the library surface,
  the `.msipkg.json` handling, the bundled worker *including how it is discovered, launched and handed
  its request file*, the redistribution plumbing, and the desktop app, with WixSharp and the WiX Toolset
  called out as upstream.
- `docs/done/FEATURE-5F00-PHASE03.md` (this file).

### Modified
- `README.md` — replaced the one-line placeholder. Title, the two house badges, a one-paragraph intro
  carrying the single load-bearing idea (one `MsiPackage` everywhere; WixSharp confined to the bundled
  `net472` worker), the what's-new callout for 1.0, five *Features* bullets (model / validation /
  building / profiles / worker CLI) plus a `### WixSharp never leaks` subsection for the cross-cutting
  property, *Installation* with the supported-TFM line and the requirements subsection, one compiling
  *Quick start*, the prose *Documentation* pointer, and *License*.
- `RELEASENOTES.md` — first-release variant, `# Enigma.Msi v1.0.0 Release Notes`. Sections: positioning
  paragraph → *Feature overview* (eight bullets: model, validation, profiles, building, worker CLI,
  packaging plumbing, desktop app, and the tested WixSharp isolation) → *Dependencies* → *Compatibility*
  → *Version*. Sub-section order follows the template; empty ones (New Features / Fixes / Breaking
  Changes) are omitted as it prescribes for a first release.
- `src/Enigma.Msi/Enigma.Msi.csproj` — `<PackageReleaseNotes>` finalized: prose mirroring the top of
  `RELEASENOTES.md`, ending `See RELEASENOTES.md for the full details.`, with the PHASE01 placeholder
  comment removed. The `<` and `>` of the CLI form are XML-escaped (`&lt;file.msipkg.json&gt;`). No other
  csproj property was touched — `<Version>` stays 1.0.0 and `GeneratePackageOnBuild` stays absent.
- `docs/roadmap.md`, `docs/plan/FEATURE-5F00.md` — PHASE03 status.
- `CLAUDE.md` — documentation freshness sweep; see *Deviations & follow-ups*.

No source file, test or build script was changed: the only non-prose edit is the one csproj metadata
property.

## Snippet-verification gate (README quick start)

**Method.** Same shape as PHASE02's gate, scaled to this phase's single snippet: the *Quick start* fence
was copied **verbatim** into a throwaway `SnippetGate` console project in the scratchpad (net10.0,
`Nullable=enable`, `ImplicitUsings=disable`, `LangVersion=14`, `TreatWarningsAsErrors=true`) with a
`ProjectReference` to `src/Enigma.Msi/Enigma.Msi.csproj`, then **compiled and executed** — nothing was
ever written into the repository, and the harness directory has been deleted.

| File | Code fences | of which C# (compiled) | Symbols | Mismatches | Uncertain |
|---|---|---|---|---|---|
| `README.md` | 3 | 1 | 25 library (+7 BCL) | 0 | 0 |
| `RELEASENOTES.md` | 0 | 0 | 31 library (+2 MSBuild properties) | **2 — fixed** | 0 |
| `SECURITY.md` | 0 | 0 | 2 (component names only) | 0 | 0 |
| **Total** | **3** | **1** | — | **2 found, 2 fixed, 0 unresolved** | **0** |

"Symbols" counts distinct public types, members and namespaces of `Enigma.Msi` named anywhere in the file
— in prose or code — each checked against `src/`. The 25 in the README's snippet are the four namespaces,
`MsiPackage` with seven initialized members, `InstallSettings`/`OutputSettings` with two members each,
`IMsiBuildService`, `MsiBuildService`'s parameterless constructor, `BuildAsync(package, IProgress<string>)`,
and `MsiBuildResult` with `Success`/`MsiPath`/`Errors`.

**Verified by execution, not by reading:** `dotnet run -c Release` on the harness printed

```
install.releasePath: Directory does not exist.
output.outputPath: Directory does not exist.
[gate] snippet executed
```

— which proves three things at once: the snippet compiles warning-free against the real assembly, its
`result.Success ? … : string.Join(…, result.Errors)` branch runs, and `BuildAsync` reached the end of the
snippet without throwing. The two lines are the expected outcome for the illustrative
`C:\src\Widget\…` paths: `BuildAsync` runs the full validator *before* spawning anything, so the fictional
directories fail validation and no worker or WiX process was ever launched. The paths are deliberately
fictional in the README — a snippet that only works if the reader happens to have a directory is worse
documentation than one that shows the shape.

**The two mismatches, both in `RELEASENOTES.md` prose, both fixed in place:**

1. Validation violations were described as "`MsiValidationError` **records**". `MsiValidationError` is a
   `sealed class` with a two-argument constructor and get-only `Path`/`Message` — not a C# `record`.
   Reworded to "entries". This is exactly the kind of claim a reader would take as an API guarantee
   (positional deconstruction, value equality, `with`) that the type does not offer.
2. `ControlPanelInfo` was summarized as "(icon, comments, contact, help/about/**update** links)". There
   is no update-link member: the type has `ProductIcon`, `Comments`, `Contact`, `HelpLink` and
   `UrlInfoAbout`, and nothing else. Replaced with the five real member names.

Also checked and found correct rather than assumed: the two MSBuild properties the notes cite
(`IncludeEnigmaMsiWorker`, `EnigmaMsiWorkerFolderName`) against `build/Enigma.Msi.targets`;
`UiSettings`' real shape (`Wui` + `InstallDialogs`/`ModifyDialogs`) behind the notes' "managed-UI flavour
and the install/modify dialog sequences"; every dependency version against
`Directory.Packages.props`; and the exit-code contract and `schemaVersion 1` against
`MsiPackage.CurrentSchemaVersion`.

## Deviations & follow-ups

- **No deviation from the plan's five steps** — all were executed as written.
- **`RELEASENOTES.md` is ahead of the desktop app's csproj.** The plan has PHASE03 cover
  "the Desktop app 1.0.0 in the same document", and PHASE04 step 1 set/confirm the app's `<Version>`.
  `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` currently has **no `<Version>` property at all**
  (so it defaults to 1.0.0 implicitly), which makes the notes true by accident rather than by
  declaration. PHASE04 must add the explicit property; until it does, the claim rests on the SDK default.
  Flagged here rather than fixed, because touching that csproj is PHASE04's scope.
- **`SECURITY.md` was created without a prior offer.** The release skill says to *offer* it; the plan's
  step 4 instructs to create it. The plan is the contract, so it was created (and it was missing, so
  nothing was clobbered).
- **No `CHANGELOG.md`** — deliberately, per the skill: `RELEASENOTES.md` is the single release-notes
  source and a second chronology guarantees divergence.
- The README's *Documentation* section names what the guides cover but links none of them. That is the
  packed-README link rule, not an oversight — `docs/guides/README.md` keeps its relative links because
  it is never packed.
- **Follow-up for PHASE05:** pack-verify must confirm the nuspec's `<releaseNotes>` matches the
  finalized `<PackageReleaseNotes>` written here, and that the embedded `README.md` is **non-empty** —
  the check that would previously have failed silently.
- **Follow-up (post-publish):** the NuGet version badge in the README resolves only once 1.0.0 is on
  nuget.org; until then it renders as "not found". Expected, and part of PHASE05's post-publish
  verification.
- Line endings: no CRLF churn — the two new files and the modified ones are LF-only, and `git diff
  --stat` shows content-only changes (no whole-file rewrites).

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`.
  This is the acceptance criterion that matters for a doc phase touching the csproj: the
  `<PackageReleaseNotes>` edit keeps the Release build green.
- **Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release` → **368 passed, 0 failed, 0 skipped**
  across all four suites (`Enigma.Msi.UnitTests` on net8.0 and net10.0, `Enigma.Msi.Worker.UnitTests` on
  net472, `Enigma.Msi.Desktop.UnitTests` on net10.0). **No tests were added** — this phase adds prose
  plus one csproj metadata property; the single code sample is guarded by the compile-and-execute gate
  above, and PHASE05's pack-verify checks the metadata against the produced artifact.
- **Snippet harness:** `dotnet run -c Release` → compiled with `TreatWarningsAsErrors=true`, 0 warnings,
  0 errors; executed to completion with the expected validation output. The harness lived entirely in the
  scratchpad and has been deleted; `git status` shows only `README.md`, `RELEASENOTES.md`, `SECURITY.md`,
  `src/Enigma.Msi/Enigma.Msi.csproj`, `CLAUDE.md`, `docs/roadmap.md`, `docs/plan/FEATURE-5F00.md` and
  `docs/done/FEATURE-5F00-PHASE03.md`.
- **Not verified here (out of scope):** no `dotnet pack` was run this phase — confirming the authored
  README and release notes actually land in the nupkg is PHASE05's pack-verify step.
