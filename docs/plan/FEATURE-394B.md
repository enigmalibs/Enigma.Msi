# FEATURE-394B — Uniform About dialog

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-394b-uniform-about-dialog`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

The About dialog looks and reads exactly like every other Enigma app's. The reference is Enigma.GitClient's
(`src/Enigma.GitClient.Desktop/Views/Dialogs/AboutView.axaml`, itself Enigma.MarkdownEditor's): same
layout, sizes, type ramp, colours and dialog chrome.

## Context & constraints

**The reference** (Enigma.GitClient @ `4bfab63`):

- Dialog: `Title = null` and no icon (the card draws neither), one `Close` button,
  `DefaultButton = Close`.
- Content: a `UserControl`, `Width="380"`, one centred `StackPanel Spacing="14"`:
  1. the logo, a 256 px PNG shown at 96×96, centred;
  2. a `StackPanel Spacing="3"`: the product name (`FontSize 18`, `SemiBold`, centred,
     `EnigmaForegroundBrush`); a horizontal `Spacing="5"` line `Version` + the version
     (`EnigmaForegroundSecondaryBrush`); a `Build` + short-sha line (`FontSize 11`,
     `EnigmaForegroundTertiaryBrush`, the sha a `SelectableTextBlock`, the line hidden when the build
     recorded no revision);
  3. the copyright (`FontSize 11`, centred, wrapping, `EnigmaForegroundSecondaryBrush`, hidden when empty),
     read from the assembly's `AssemblyCopyrightAttribute` (`Copyright © 2026 Josué Clément`).
- ViewModel: `DisplayName`, `Version`, `BuildSha` + `HasBuildSha`, `Copyright` + `HasCopyright`; a
  `(version, buildSha, copyright)` constructor for tests beside the parameterless one. The version and
  the 7-char sha come from the informational version (`X.Y.Z+<sha>`), the plain assembly version as
  fallback.

**This app today:** `Views/AboutCard.axaml` — 64 px logo beside name and tagline, version, copyright,
author, a separator, the repository URL and a **View on GitHub** button; `AboutDialogService` titles it
"About" with an Info icon at 360–520 px; `AboutViewModel` takes `IUrlLauncherService` + a logger;
`AppInfo` holds `Name`, `Tagline`, `Author`, `Copyright`, `RepositoryUrl`, `LogoUri`, `Version`.
`IUrlLauncherService`/`UrlLauncherService` exist for the GitHub button alone.

- The house `*Card` naming for dialog content (`BuildProgressCard`, `QuickStartCard`) stays: the type
  keeps the name `AboutCard`; its markup becomes the reference's.
- **The `ContentDialog` size trap still holds in Enigma.Avalonia.Desktop 1.3.0** (`ResetDialog` clears
  content, buttons, icon — not the six `Dialog*` sizes): the service keeps stating all six, now the
  control's own defaults (`NaN`, `NaN`, 320, 600, 0, ∞), so the card's 380 px governs.
- The card's focus-on-attach (Escape needs focus inside the dialog) stays — a behaviour, not a style.
- `Assets/logo.png` is the 256 px frame of `appicon.ico` — the reference's `app-256.png` equivalent.

## Steps

1. `AppInfo`: keep `Name`, `Version`; add `BuildSha` (short sha or `null`) and `Copyright` read from
   `AssemblyCopyrightAttribute` (empty when absent); remove `Tagline`, `Author`, `RepositoryUrl`, `LogoUri`
   (unused once the card changes). Parsing in one testable static (`Parse(informational, fallback)`).
2. `AboutViewModel`: the reference's shape, still an `ObservableObject`; no launcher, no logger.
3. `AboutCard.axaml`: the reference's markup (names `BuildLine`, `BuildSha`, `Copyright` kept), logo
   from `avares://Enigma.Msi.Desktop/Assets/logo.png`.
4. `AboutDialogService`: no title/icon, `Close` + `DefaultButton.Close`, the six sizes at the control's
   defaults; builds a fresh `AboutViewModel` per showing (it is immutable); `AboutViewModel`'s DI
   registration goes.
5. Delete `IUrlLauncherService`, `UrlLauncherService` and their registration.
6. Tests: `AppInfoTests` (parse: sha/no sha/blank/fallback, copyright), `AboutViewModelTests` (the shape),
   new headless `Views/AboutCardTests` (texts, logo resolves, build line hidden with no sha, copyright
   hidden when empty, brushes resolve in light and dark).
7. Docs: README bullet, `docs/guides/desktop-app.md` About paragraph.

## Acceptance criteria

- The card matches the reference element for element (order, sizes, font sizes, brushes, spacing).
- The dialog has no title and no icon, one Close button that is the default.
- No `IUrlLauncherService`, `Tagline`, `Author`, `RepositoryUrl` remain in `src/` or `tests/`.
- Release build warning-free; the whole suite green, new tests included.

## Out of scope

- A shared About library across Enigma apps (each app carries its copy, as GitClient does).
- The main window's About button (stays as is).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What "uniform" covers | Content too, not only style: tagline, author and GitHub link removed | The reference shows exactly icon, name, version, build, copyright; extra rows break uniformity | Restyling the current rows |
| Type name | Keep `AboutCard` | Local `*Card` convention wins over the reference's `AboutView` | Renaming to `AboutView` |
| URL launcher seam | Deleted | Its only caller goes; dead code otherwise | Keeping it "for later" |
| Copyright source | Assembly attribute | As the reference; one source with `Directory.Build.props` | Keeping the `© 2026 …` constant |
| Dialog sizes | Stated, at the control's defaults | The reset trap still holds in 1.3.0; defaults give the reference's sizing | Not setting them (would inherit the last dialog's) |
| ViewModel lifetime | New per showing, not DI | Immutable, cheap; the reference does the same | Singleton in DI |
