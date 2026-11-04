# FEATURE-F30B — Desktop app 1.9.0 release

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-f30b-release-1-9-0`
**Run:** feature/2026-11-04-final3-icons-release

## Objective

Prepare **Enigma.Msi.Desktop 1.9.0**, an app-only SemVer minor that carries this run's icon change.
Follow `docs/RELEASE.md` and the shape of 1.8.0 (`FEATURE-5897`). Have the installer build,
`packaging/windows/buildmsi.ps1`, produce `artifacts\Enigma.Msi.1.9.0.msi`.

## Context & constraints

- The app declares `<Version>1.8.0</Version>` in `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`. The
  user's rule `x.y.z → x.(y+1).z` gives **1.9.0**, because z is 0. The tags `1.0.0` to `1.8.0` exist. The
  library stays at 1.0.0 and is not re-released.
- **The version is not written in `buildmsi.ps1`.** The script reads it from that csproj's `<Version>`
  (`/Project/PropertyGroup/Version`, required to be `X.Y.Z`) and names the MSI and its profile after it.
  `docs/RELEASE.md` §2 says so: "a release changes nothing for the installer". The csproj comment beside
  `<Version>` says the same. All eight other Enigma apps' `buildmsi.ps1` read their version from a project
  file the same way. Setting `<Version>1.9.0` is therefore what changes the script's version, and the
  dogfood build proves it.
- **The vendored worker** (`packaging/windows/Enigma.Msi.Worker/`, the 1.7.0 worker) is unchanged:
  `git diff 1.7.0..HEAD -- src/Enigma.Msi src/Enigma.Msi.Worker` is empty. Its README refreshes it only
  "when this repository releases a worker that changes".
- **1.9.0 carries** the items of this run that are `DONE` on the run branch when this dev starts. Today
  that is only `FEATURE-1AC4`, the frame-filling icon set (`final3`). If that item ended quarantined, the
  release still goes ahead and its notes record that nothing user-facing changed.
- No committed MSI profile: `buildmsi.ps1` writes it per build (`FEATURE-3E44`).
- `SECURITY.md` supports the latest minor line only. `RELEASENOTES.md` is newest-first with a
  current-version table. The README callout leads with the latest release, and CLAUDE.md's build-state
  block gets a paragraph per release.
- The Avalonia set moves only as a whole, and only together with what `Enigma.Avalonia.Desktop` is built
  against.
- Never tag, push or merge into `develop` or `main` inside the run. `git clean` runs only in a scratch
  clone.

## Steps

1. Check that `git tag --list 1.9.0` is empty.
2. Set `<Version>1.9.0</Version>`.
3. `RELEASENOTES.md`: add a `1.9.0 — Enigma.Msi.Desktop` section on top, in 1.8.0's shape:
   - a summary;
   - *Changed*;
   - *Upgrading from 1.8*: the installed "Enigma Msi" 1.8.0 is upgraded in place;
   - *Compatibility*: profiles, library, dependencies.

   Update the current-version table too, and check every path the notes quote against the files.
4. Point the `README.md` callout at 1.9.0, and make `SECURITY.md` support 1.9.x.
5. Add a 1.9.0 paragraph to CLAUDE.md's build-state block: what the run changed, and what was verified on
   Windows.
6. In `docs/RELEASE.md`, bring any line that names a *current* version to 1.9.0. Facts about older
   versions stay as they are.
7. Check the dependencies (`dotnet list package --outdated`) and log the result. Apply patch and minor
   bumps outside the coupled Avalonia set. Hold the Avalonia set unless `Enigma.Avalonia.Desktop` moved
   with it.
8. Pre-flight on Windows:
   - a clean Release build, warning-free;
   - the whole suite green, worker suite and drift guards included;
   - the built app reports 1.9.0.
9. Dogfood: run `buildmsi.bat` in a scratch clone of the dev's tree. It must build
   `artifacts\Enigma.Msi.1.9.0.msi`. Record its size and read back its tables:
   - Property: `ProductVersion` 1.9.0, `UpgradeCode`, `ProductName`;
   - Upgrade and Shortcut;
   - File: `app.ico`, which must be the `final3` icon, and the seeded `worker\.wixsharp\` files.

   Copy the MSI and its profile into this repository's git-ignored `artifacts/`.

## Acceptance criteria

- The app's assembly carries 1.9.0.
- `buildmsi.ps1` names its output `Enigma.Msi.1.9.0.msi` with no edit to the script, as the dogfood build
  shows.
- `RELEASENOTES.md` covers every `DONE` item of this run, and its quoted paths match the files. README,
  SECURITY and CLAUDE.md name 1.9.0.
- The Release build is warning-free, and the whole suite is green on Windows.
- The 1.9.0 MSI is built by `buildmsi` and its tables are read back, or the failure is recorded with its
  output.
- Nothing in the dev tags, pushes, installs the MSI, or merges outside the run branch.

## Out of scope

- Install, launch and uninstall verification of the MSI (the maintainer's step, `docs/RELEASE.md` §5).
- Tagging, merging into `develop` or `main`, and publishing the MSI anywhere.
- Refreshing the vendored worker, and moving the Avalonia set on its own.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version | 1.9.0 | The user's rule (y+1, z unchanged at 0) | 1.8.1; 2.0.0 |
| "Update the new version number in the buildmsi script" | No edit to `buildmsi.ps1`. The release sets the csproj `<Version>`, which the script reads, and the dogfood build proves that the script produces `Enigma.Msi.1.9.0.msi` | The script has no version literal: it derives `X.Y.Z` from the csproj by design (`docs/RELEASE.md` §2), as all eight other Enigma apps' scripts do | A `$Version = '1.9.0'` literal in the script (a second source of truth that drifts from the assembly, and breaks parity with the other apps); a `-Version` parameter (new surface nobody asked for) |
| Vendored worker | Not refreshed | The worker and the library have not changed since the 1.7.0 copy, and its README's refresh rule does not fire | Re-vendoring a byte-identical build under a new tag |
| Library | Stays 1.0.0 | Not published, and nothing in it changed | Bumping it |
| Order in the run | Last | The notes describe the icon, which is built first | First |
| Committed MSI profile | None | `buildmsi.ps1` generates the profile per build | `msiProfiles/Enigma.Msi.Desktop.1.9.0.msipkg.json` (the retired clone rule) |
| Dogfood MSI | Built with `buildmsi.bat` in a scratch clone | WiX is installed here, and it proves the release path end to end without running `git clean` on the user's tree | Leaving it to the maintainer |
| Dependencies | Non-coupled patch and minor bumps applied; the Avalonia set held together with `Enigma.Avalonia.Desktop` | House rule from earlier releases | Bumping everything |
| Breakdown | One dev | A routine release, as 1.8.0 was | Splitting the docs from the version |
