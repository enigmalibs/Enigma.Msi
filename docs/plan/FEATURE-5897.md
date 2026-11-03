# FEATURE-5897 — Desktop app 1.8.0 release

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-5897-release-1-8-0`
**Run:** feature/2026-11-03-icons-msi-release

## Objective

Prepare **Enigma.Msi.Desktop 1.8.0**, an app-only SemVer minor that carries this run's items. Follow
`docs/RELEASE.md` (as `FEATURE-3E44` leaves it) and the shape of 1.7.0 (`FEATURE-AB6B`).

## Context & constraints

- The app declares `<Version>1.7.0</Version>` in `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`. The
  user's rule `x.y.z → x.(y+1).0` gives **1.8.0** (z is already 0). The library stays at 1.0.0 and is
  not re-released.
- **1.8.0 carries** this run's items that are `DONE` on the run branch when this dev starts:
  - `FEATURE-62E1`: the new icon set without the tile;
  - `FEATURE-3E44`: `buildmsi.ps1` / `buildmsi.bat` and the vendored worker. Its MSI is named
    "Enigma Msi" and carries the installed 1.7.0's upgrade code.
  An item that ended quarantined is left out of the notes.
- **No committed profile.** From `FEATURE-3E44` on, `buildmsi.ps1` writes the profile on every build, so
  there is no `msiProfiles/Enigma.Msi.Desktop.1.8.0.msipkg.json`.
- `SECURITY.md` supports the latest minor line only. `RELEASENOTES.md` is newest-first with a
  current-version table. The README callout leads with the latest release, and CLAUDE.md's build-state
  block gets a paragraph per release.
- The Avalonia set moves only as a whole, and only together with what `Enigma.Avalonia.Desktop` is built
  against.
- Never tag, push or merge into `develop` or `main` inside the run. `git clean` runs only in a scratch
  clone.

## Steps

1. Check that `git tag --list 1.8.0` is empty.
2. `<Version>1.8.0</Version>`.
3. `RELEASENOTES.md`: a `1.8.0 — Enigma.Msi.Desktop` section on top in 1.7.0's shape:
   - a summary;
   - *Added* / *Changed*;
   - *Upgrading from 1.7*: what the installed "Enigma Msi" 1.7.0 does when 1.8.0 is installed;
   - *Compatibility*: profiles, library, dependencies.
   Update the current-version table too. Every path the notes quote is checked against the files.
4. `README.md` callout → 1.8.0; `SECURITY.md` → 1.8.x supported.
5. `CLAUDE.md` build-state block: a 1.8.0 paragraph (what the run changed, what was verified on Windows).
6. `docs/RELEASE.md`: any line that names a specific version is brought to 1.8.0.
7. Dependency check (`dotnet list package --outdated`), logged. Patch and minor bumps outside the coupled
   Avalonia set are applied. The Avalonia set is held unless `Enigma.Avalonia.Desktop` moved with it.
8. Pre-flight on Windows:
   - a clean Release build, warning-free;
   - the whole suite green, worker suite and drift guards included;
   - the built app reports 1.8.0.
9. Dogfood: run `buildmsi.bat` in a scratch clone of the dev's tree. It builds
   `artifacts\Enigma.Msi.1.8.0.msi`. Record its size, then read back its tables:
   - Property: `ProductVersion` 1.8.0, `UpgradeCode`, `ProductName`;
   - Upgrade and Shortcut;
   - File: `app.ico` and the seeded `worker\.wixsharp\` files.
   Copy the MSI and its profile into this repository's git-ignored `artifacts/`.

## Acceptance criteria

- The app's assembly carries 1.8.0.
- `RELEASENOTES.md` covers every `DONE` item of this run, and its quoted paths match the files. README,
  SECURITY and CLAUDE.md name 1.8.0.
- The Release build is warning-free, and the whole suite is green on Windows.
- The 1.8.0 MSI is built by `buildmsi` and its tables are read back (or the failure is recorded with its
  output).
- Nothing in the dev tags, pushes, installs the MSI, or merges outside the run branch.

## Out of scope

- Install, launch and uninstall verification of the MSI (the maintainer's step, `docs/RELEASE.md` §5).
- Tagging, merging into `develop`/`main`, publishing the MSI anywhere.
- Moving the Avalonia set on its own.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version | 1.8.0 | The user's rule (y+1, z reset to 0) | 1.7.1; 2.0.0 |
| Library | Stays 1.0.0 | Not published, and nothing in it changed | Bumping it |
| Order in the run | Last | The notes describe the icon and the installer, built first | First |
| A committed MSI profile for 1.8.0 | None | `FEATURE-3E44`'s script generates the profile per build from the version | `msiProfiles/Enigma.Msi.Desktop.1.8.0.msipkg.json` (the retired clone rule) |
| Dogfood MSI | Built with `buildmsi.bat` in a scratch clone | WiX 7.0.0 is installed here. It proves the new release path end to end without `git clean` on the user's tree | Leaving it to the maintainer; running the old worker-plus-profile command |
| Dependencies | Non-coupled patch/minor bumps applied; the Avalonia set held with `Enigma.Avalonia.Desktop` | House rule from earlier releases | Bumping everything |
| Breakdown | One dev | A routine release, as 1.7.0 | Splitting docs from the version |
