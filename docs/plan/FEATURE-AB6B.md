# FEATURE-AB6B — Desktop app 1.7.0 release

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-ab6b-release-1-7-0`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Prepare **Enigma.Msi.Desktop 1.7.0** — app-only, a SemVer minor — carrying this run's items, following
`docs/RELEASE.md` and the shape of 1.5.0/1.6.0 (FEATURE-1E7D, FEATURE-25EF `PHASE02`).

## Context & constraints

- The app declares `<Version>1.6.0</Version>` in `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`. The
  user's rule: `x.y.z → x.(y+1).0` — **1.7.0**. The library stays at 1.0.0 and is not re-released.
- **1.7.0 carries** this run's items that are `DONE` on the run branch when this dev starts:
  FEATURE-D37E (no `net8.0`), FEATURE-CED6 (packages), FEATURE-5E0B (no splash), FEATURE-394B (uniform
  About), FEATURE-C7BF (`app.ico` in the output), FEATURE-2EEB (safety net), FEATURE-22F0 (Linux
  install/uninstall), FEATURE-C19D (publish scripts). A quarantined item is left out of the notes.
- Profile clone rule (`docs/RELEASE.md`): copy `msiProfiles/Enigma.Msi.Desktop.1.6.0.msipkg.json`, new
  `productId` from a real generator, `upgradeCode` verbatim (`3405046f-527a-439e-a22f-866247dc8314`),
  `version` 1.7.0; everything else unchanged (`compression` stays `High`).
- **1.6.0 left Windows debt** (CLAUDE.md): the whole suite with the worker suite and drift guards, and the
  dogfood MSI, were owed on Windows. This run is on Windows with WiX 7.0.0 installed: the release closes it
  for 1.7.0.
- `SECURITY.md` supports the latest minor line only; `RELEASENOTES.md` is newest-first with a
  current-version table; the README callout leads with the latest release.
- Never tag, push or merge into `develop`/`main` inside the run.

## Steps

1. `git tag --list 1.7.0` is empty.
2. `<Version>1.7.0</Version>` (and its comment if it names a version).
3. `msiProfiles/Enigma.Msi.Desktop.1.7.0.msipkg.json` per the clone rule; it deserializes and validates
   (in-memory) with zero errors.
4. `RELEASENOTES.md`: a `1.7.0` section on top in 1.6.0's shape — summary, one entry per change,
   *Upgrading from 1.6*, the library note (still 1.0.0, now `netstandard2.0` + `net10.0`) — and the
   current-version table.
5. `README.md` callout → 1.7.0; `SECURITY.md` → 1.7.x supported.
6. `CLAUDE.md` build-state block: a 1.7.0 paragraph (what the run changed, what was verified on Windows).
7. Pre-flight on Windows: clean Release build warning-free; the whole suite green (worker suite and drift
   guards included); the built app reports 1.7.0.
8. Dogfood: `mkdir artifacts`; `.\packaging\windows\publish.ps1`; the worker builds
   `artifacts\Enigma.Msi.Desktop.msi` from the 1.7.0 profile; record its size and read back its Property
   (`ProductVersion` 1.7.0, `UpgradeCode`), and check its File table holds `app.ico` and the seeded
   `worker\.wixsharp\` files.

## Acceptance criteria

- The app's assembly carries 1.7.0; the 1.7.0 profile is committed and valid.
- `RELEASENOTES.md` covers every `DONE` item of this run; README, SECURITY and CLAUDE.md name 1.7.0.
- Release build warning-free; the whole suite green on Windows.
- The dogfood MSI is built and its tables read back (or the failure is recorded with its output).
- Nothing in the dev tags, pushes or merges outside the run branch.

## Out of scope

- Install/launch/uninstall verification of the MSI (the maintainer's step, `docs/RELEASE.md` §5).
- Tagging, merging to `main`, publishing the MSI anywhere.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version | 1.7.0 | The user's rule (y+1, z reset) | 1.6.1; 2.0.0 |
| Library | Stays 1.0.0 | Not published; the TFM drop changes no in-repo consumer | Bumping it |
| Profile compression | `High`, as every release profile | Shipped installer size over one slow build | Following the `None` default |
| Dogfood MSI | Built here | WiX 7.0.0 is installed; it closes the Windows debt 1.6.0 left | Leaving it to the maintainer |
| Breakdown | One dev | A routine release, as 1.5.0 | Splitting docs from the version |
