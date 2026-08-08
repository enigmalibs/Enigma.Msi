# FEATURE-5F00-PHASE05 — Release runbook, pre-flight & pack-verify (DONE)

## Summary

The release is now executable by hand: `docs/RELEASE.md` is the checklist that takes 1.0.0 (and every
version after it) from a green pre-flight to a published package and a built installer, and the package it
describes has been **packed and inspected** rather than assumed correct.

The pack-verify is the substance of this phase. `Enigma.Msi` is an unusual nupkg for this family — beyond
`lib/` it redistributes the whole `net472` worker under `tools/worker/` plus the `build/Enigma.Msi.targets`
that puts it into a consumer's output — and none of that had ever been confirmed to survive a real
`dotnet pack`. It does: 23 worker files, byte-identical to the worker's build output, alongside the three
`lib/` TFMs, an embedded non-empty README and LICENSE, and a nuspec whose every metadata field is what
PHASE01–PHASE03 authored.

No outward-facing command was run. Nothing was tagged, packed into `./artifacts`, or pushed.

## Files/modules touched

### Created

- `docs/RELEASE.md` — the runbook, from the house template with all five placeholders filled
  (`Enigma.Msi`, `Enigma.Msi.slnx`, `src/Enigma.Msi/Enigma.Msi.csproj`, `src/Enigma.Msi/`, `main`) and
  extended with three repo-specific sections; see *Runbook deviations* below.
- `docs/done/FEATURE-5F00-PHASE05.md` (this file).

### Modified

- `docs/roadmap.md`, `docs/plan/FEATURE-5F00.md` — PHASE05 status, and `FEATURE-5F00` itself flipped to
  DONE: this was its last phase.
- `CLAUDE.md` — documentation freshness sweep. The build-state callout said PHASE05 was "still to come"
  and carried a **"Do not push the package before `PHASE05`"** warning that no longer applies; it now
  records the pack-verify result and states plainly that 1.0.0 is *prepared but not published*, with the
  missing remote as the blocker. The project-layout block gained a `docs/RELEASE.md` line.

No source file, csproj or profile was touched. This phase is documentation plus verification.

## Pack-verify

Packed into a throwaway directory outside the repository tree, inspected, then deleted:

```
dotnet pack src/Enigma.Msi/Enigma.Msi.csproj -c Release -o <scratchpad>/packverify
→ Successfully created package 'Enigma.Msi.1.0.0.nupkg'  (1 833 478 bytes)
```

| Check | Result |
|---|---|
| `.nupkg` version matches the release | **1.0.0** — `Enigma.Msi.1.0.0.nupkg` |
| `README.md` embedded and non-empty | 5 928 bytes, **SHA-256 identical to the repo root README** |
| `LICENSE.md` embedded | 1 063 bytes, SHA-256 identical to the repo root LICENSE |
| nuspec `<version>` | `1.0.0` |
| nuspec `<title>` | `Enigma.Msi — declarative Windows MSI builder` |
| nuspec `<license type="file">` | `LICENSE.md` |
| nuspec `<readme>` | `README.md` |
| nuspec `<releaseNotes>` | the finalized PHASE03 prose, ending "See RELEASENOTES.md for the full details." |
| `lib/` for all three TFMs | `netstandard2.0`, `net8.0`, `net10.0` — each with `Enigma.Msi.dll` **and** `Enigma.Msi.xml` |
| `tools/worker/` present | **23 files, 3.53 MB** — name set identical to `src/Enigma.Msi.Worker/bin/Release/net472/` |
| `build/Enigma.Msi.targets` present | SHA-256 identical to `build/Enigma.Msi.targets` in the repo |
| Verify directory deleted | yes — `git status --porcelain` shows only this phase's three doc files |

**Dependency floors, per target framework** — exactly the intended shape, and the reason the netstandard2.0
conditionals in the csproj exist:

| Target framework | Dependencies |
|---|---|
| `net10.0` | *(none)* |
| `net8.0` | *(none)* |
| `.NETStandard2.0` | `System.Buffers` 4.6.1, `System.Text.Json` 10.0.10 — both matching `Directory.Packages.props` |

`PolySharp` is correctly absent (compile-only, `PrivateAssets=all`), and no `.snupkg` was produced —
`IncludeSymbols` is off, the family default.

Worker payload, verbatim: `Enigma.Msi.Worker.exe` + `.exe.config`, `Enigma.Msi.dll`/`.xml`/`.pdb`,
`WixSharp.dll`, `WixSharp.Msi.dll`, `WixSharp.UI.dll`, `WixSharp.UI.WPF.dll`, `WixSharp.MsiEventHost.exe`,
`WixToolset.Dtf.WindowsInstaller.dll`, `WixToolset.Mba.Core.dll`, `mbanative.dll`, `System.Text.Json.dll`
and its `netstandard2.0` support set. The two `.pdb` files are deliberate — the payload is packed
unfiltered so a consumer's `worker/` is byte-for-byte what `build/CopyWorkerOutput.targets` produces
in-repo, and a packaged worker can never behave differently from the local one.

## Runbook deviations from the house template

Three, all repo-specific rather than stylistic:

1. **`dotnet test` carries `--solution`.** The template prints `dotnet test <solution>`; this repo runs the
   Microsoft Testing Platform runner (`global.json`), and on the .NET 10 SDK in MTP mode a bare
   `dotnet test <solution>` is *rejected*. The runbook prints `dotnet test --solution Enigma.Msi.slnx -c
   Release` and says why, so a future release doesn't rediscover it at the worst moment.
2. **The pack step is preceded by an explicit Release build, and by a content checklist.** `tools/worker/`
   is harvested from the worker's build output, so packing in a configuration you have not built is the
   one route to a nupkg that installs fine and fails on the consumer's first `BuildAsync`. The
   `PackEnigmaMsiWorkerPayload` guard makes it an error rather than a silent shipment, and the checklist
   makes the three unusual entries (`tools/worker/`, `build/Enigma.Msi.targets`, the `.xml` doc files)
   verifiable at a glance.
3. **A whole app-MSI section (§7) the template has no notion of** — the publish-then-worker command pair
   carried in from PHASE04, run from the repository root, with the measured reason `releasePath` must stay
   on the publish output; the profile clone rule as a table (`upgradeCode` verbatim, new `productId`,
   bumped `version`); the exit-code contract; and an app-release pre-flight checklist including the
   install/launch/uninstall step that is still the maintainer's.

## Acceptance criteria

| Criterion | Outcome |
|---|---|
| Pre-flight green | **Met** — Release build 0 warnings / 0 errors (forced full rebuild), 368 tests passed / 0 failed / 0 skipped |
| Pack-verify checklist fully satisfied including the worker bundle | **Met** — every row above verified from the artifact, not from the csproj |
| Verify dir deleted | **Met** — packed outside the tree, then removed; `git status` shows only doc changes |
| Runbook printed with all placeholders resolved | **Met** — printed to the console, and committed as `docs/RELEASE.md` |
| No outward command executed; no secret echoed | **Met** — no `git tag`, no `git push`, no pack into `./artifacts`, no `nuget push`; the API key appears only as the `<NUGET_API_KEY>` placeholder |

## Deviations & follow-ups

- **The repository still has no git remote** (`git remote -v` is empty) and **no tags** (`git tag` is
  empty). The plan anticipated this: the runbook's §2/§3 merge-and-tag steps are written against
  `origin`/`main` as planned but have **not been verified against a real remote**, and the bare `X.Y.Z` tag
  form is the house default for a repo with no prior tags rather than a convention detected here.
  *Before publishing, the maintainer must create `github.com/enigmalibs/Enigma.Msi`, push `main`, and
  confirm the default branch name* — `RepositoryUrl`, `PackageProjectUrl` and the nuspec `<repository>` all
  already point there.
- **The nuspec records the commit `24327f1…`**, i.e. the PHASE04 commit this branch was cut from. That is
  `dotnet pack` reading the current `HEAD`; the published package will carry whatever commit is checked out
  when the real pack runs. Nothing to fix — noted so the value isn't mistaken for a pin.
- **The pre-flight build was re-run with `--no-incremental`.** The first run completed in 3.9 s, i.e. mostly
  from up-to-date projects, which would not re-emit warnings; the recorded evidence is the forced full
  rebuild.
- **`docs/RELEASE.md` is not packed**, so its relative links and repo-relative paths are correct there —
  the packed-README link rule does not apply to it.
- **Follow-up, unchanged from PHASE04:** the desktop MSI has been built but never installed. §7's
  `msiexec /i` / `msiexec /x` pre-flight is where that lands, and it is the last unverified step of the
  1.0.0 app release.
- **Follow-up:** `docs/RELEASE.md` hard-codes the app's `upgradeCode` in the clone-rule table. That is
  deliberate (the value is the point of the rule), but it means the table must be updated if a *second*
  app is ever released from this repo.
- **Line endings:** no CRLF churn observed in this phase; all three touched files are LF.

## Build/test evidence

- **Build:** `dotnet build Enigma.Msi.slnx -c Release --no-incremental` → `Build succeeded. 0 Warning(s)
  0 Error(s)`.
- **Tests:** `dotnet test --solution Enigma.Msi.slnx -c Release` → **368 passed, 0 failed, 0 skipped**
  across all four suites (`Enigma.Msi.UnitTests` on net8.0 and net10.0, `Enigma.Msi.Worker.UnitTests` on
  net472, `Enigma.Msi.Desktop.UnitTests` on net10.0). **No tests were added** — the phase's deliverable is a
  documentation file plus a verification pass over a build artifact; there is no product code to test, and
  a test asserting the shape of a nupkg would have to run `dotnet pack` from inside the unit suite.
- **Pack-verify:** as tabulated above — packed, extracted, every claim checked against file hashes and
  directory listings rather than against the csproj, then deleted.
- **Not verified here:** nothing was published. The remote, the tag, the real `./artifacts` pack and the
  NuGet push are all the maintainer's, per the runbook.
