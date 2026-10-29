# FEATURE-CED6 — Update NuGet packages to latest

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-ced6-update-packages`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Every package pinned in `Directory.Packages.props` at its latest stable version, the solution still
building warning-free and its whole suite still green.

## Context & constraints

`dotnet list Enigma.Msi.slnx package --outdated` on 2026-10-29 (nuget.org):

| Package | Pinned | Latest |
|---|---|---|
| Avalonia, .Desktop, .Fonts.Inter, .Themes.Fluent, .Headless, .Headless.XUnit | 12.1.1 | 12.1.4 |
| Enigma.Avalonia.Desktop | 1.0.0 | 1.3.0 (its nuspec pins Avalonia 12.1.4, CommunityToolkit.Mvvm 8.4.2) |
| Microsoft.Extensions.Hosting | 10.0.10 | 10.0.14 |
| System.Text.Json | 10.0.10 | 10.0.14 |
| NLog, NLog.Extensions.Logging | 6.1.4 | 6.2.2 |
| WixSharp_wix4 | 2.14.1 | 2.14.2 |
| xunit.v3 | 3.2.2 | 4.0.2 (major) |
| coverlet.collector | 10.0.1 | 10.1.0 |
| NSubstitute | 6.1.0 | 6.2.0 |

Already latest: Enigma.Icons.Avalonia 1.0.0, CommunityToolkit.Mvvm 8.4.2, AvaloniaUI.DiagnosticsSupport
2.2.3, PolySharp 1.16.0, System.Buffers 4.6.1 (re-checked at build time).

- **The Avalonia set is coupled** (CLAUDE.md): Avalonia 12.1.x + `.Headless*` + Enigma.Avalonia.Desktop +
  Enigma.Icons.Avalonia move together. Enigma.Avalonia.Desktop 1.3.0 is built against 12.1.4, so the
  whole set moves to 12.1.4 at once.
- **WixSharp moves under guard**: the worker suite's 16 drift tests and `WixSharpBuildDirTests` must stay
  green; the worker's `SeedWixSharpBuildDir` `<Error>` fires if the seeded file set changed — then re-pin
  it per CLAUDE.md (*Project layout*).
- **xunit.v3 4.x** is a major: check that `Avalonia.Headless.XUnit` 12.1.4 accepts it and that the
  `net472` worker suite still restores and runs under MTP.
- Enigma.Avalonia.Desktop 1.0.0 → 1.3.0 may carry API or style changes the app uses (`ContentDialog`,
  `Overlay`, `InfoBar`, the editors, the picker services) — the build and the headless/ViewModel suites
  are the check; the two `ContentDialog` traps in CLAUDE.md are re-verified against 1.3.0's source.

## Steps

1. Re-run `dotnet list package --outdated` (and `--include-prerelease` off) to confirm the table.
2. Bump every row in `Directory.Packages.props`; refresh the coupled-set comment (12.1.4 / 1.3.0) and the
   headless comment (built against xunit.v3 — the version it accepts).
3. Restore, build Release warning-free; fix any API break or new analyzer warning the bumps surface,
   at its cause.
4. Run the whole suite, worker suite and drift guards included.
5. A package that cannot move without a change out of this item's scope is held back at its newest
   compatible version, with the reason recorded in the completion doc and in the props comment.
6. `CLAUDE.md`'s dependency paragraph names the new coupled-set versions.

## Acceptance criteria

- `dotnet list package --outdated` reports nothing, or only rows recorded as held back with a reason.
- Release build warning-free; the whole suite green, including the 16 WixSharp drift guards.
- The `ContentDialog` traps documented in CLAUDE.md are confirmed (or corrected) against 1.3.0.

## Out of scope

- Adopting new Enigma.Avalonia.Desktop 1.3.0 features the app does not need.
- Upgrading the SDK pin in `global.json`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Majors (xunit.v3 4.x) | Included | "Latest versions" is the spec; Enigma.GitClient already runs 4.0.2 | Patch/minor only |
| Coupled Avalonia set | Moved as a whole to 12.1.4 with Enigma.Avalonia.Desktop 1.3.0 | The house coupling rule; 1.3.0 is built against 12.1.4 | Avalonia alone; holding the set back |
| WixSharp 2.14.2 | Moved, drift guards as the gate | A patch; the guards exist exactly for this | Holding it back |
| Incompatible package | Held back, recorded | A senior's call — no unrelated rewrite under a dependency bump | Quarantining the whole item |
| Order in the run | Second, before any UI work | Later devs are written and tested against the libraries that ship | Last, before the release |
