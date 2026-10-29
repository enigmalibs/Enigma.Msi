# FEATURE-394B — Uniform About dialog

## Summary

The About dialog is now the one every Enigma app shares. It is a copy of Enigma.GitClient's
(`Views/Dialogs/AboutView.axaml`, itself Enigma.MarkdownEditor's): a 380 px card holding one centred column
with these lines:
- the 96 px logo;
- the name (18 SemiBold);
- `Version X.Y.Z` (secondary brush);
- `Build <7-char sha>` (11 px, tertiary brush, the sha selectable, hidden when the build recorded none);
- the copyright (11 px, secondary brush, hidden when empty).

The dialog itself has no title, no icon and one `Close` button that is the default. The tagline, the author,
the repository URL and the **View on GitHub** button are gone. With them went `IUrlLauncherService` and
`UrlLauncherService`, whose only caller was that button.

`AppInfo` now carries the following:
- `Name` and `Version`;
- `BuildSha`, the `+<sha>` the SDK appends to the informational version, shortened to 7 characters;
- `Copyright`, read from the assembly attribute `Directory.Build.props` stamps;
- `ParseVersion(informational, fallback)`, a public static so a build with no revision can be tested.

`AboutViewModel` is the reference's shape and is built fresh per showing, not through DI.

## Files/modules touched

- Modified:
  - `src/Enigma.Msi.Desktop/Services/AppInfo.cs`
  - `src/Enigma.Msi.Desktop/Services/AboutDialogService.cs`
  - `src/Enigma.Msi.Desktop/Services/IAboutDialogService.cs`
  - `src/Enigma.Msi.Desktop/ViewModels/AboutViewModel.cs`
  - `src/Enigma.Msi.Desktop/Views/AboutCard.axaml`
  - `src/Enigma.Msi.Desktop/Views/AboutCard.axaml.cs`
  - `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/Services/AppInfoTests.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/ViewModels/AboutViewModelTests.cs`
  - `README.md`
  - `docs/guides/desktop-app.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-394B.md`
- Created:
  - `tests/Enigma.Msi.Desktop.UnitTests/Views/AboutCardTests.cs`
  - `docs/done/FEATURE-394B.md`
- Deleted:
  - `src/Enigma.Msi.Desktop/Services/IUrlLauncherService.cs`
  - `src/Enigma.Msi.Desktop/Services/UrlLauncherService.cs`

## Deviations & follow-ups

- **The displayed copyright changed** from the constant `© 2026 Josué Clément` to the assembly's
  `Copyright © 2026 Josué Clément`, as the reference shows it.
- The card keeps one behaviour of its own that the reference lacks: it takes focus on attach, because the
  dialog only handles Escape while focus is inside it. That is behaviour, not style.
- The dialog still states all six `Dialog*` sizes, now at the control's defaults (`NaN`, `NaN`, 320, 600, 0,
  ∞), because of the reset trap. Enigma.GitClient states none. The result is the same as long as no other
  dialog has resized the host, and this way it stays the same when one has.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How `ParseVersion` is exposed | A public static on `AppInfo` returning a tuple | Testable without a new type. The reference's `ProductVersion` record lives in a Core assembly this app does not have |
| Fallback version | `Version.ToString(3)`, then `0.0.0` | As the reference |
| Brush test | Each line's foreground compared with the colour of the key it names, in light and dark | A missing key does not fail the markup, it silently inherits, so only a comparison catches it |

## Documentation sweep

- `README.md`: the *About* bullet lists the new content.
- `docs/guides/desktop-app.md`: the About paragraph describes the shared dialog.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release`: **0 warnings**, 0 errors.
- `dotnet test --solution Enigma.Msi.slnx -c Release`: **368/368 passed**. Before, 348; 9 old About and
  AppInfo tests were replaced by 29 new ones:
  - `AppInfoTests`, 15 (the parse included);
  - `AboutViewModelTests`, 7;
  - the headless `AboutCardTests`, 7.
- Manual check, done with a screenshot through UI Automation:
  1. Launched `bin/Release/net10.0/Enigma.Msi.Desktop.exe` and cancelled the auto-opened quick start.
  2. Invoked the "About Enigma.Msi" button.
  3. Captured the window.

  It showed the centred logo, **Enigma.Msi**, `Version 1.6.0`, a quieter `Build 4a3434d` (the run branch
  tip), the copyright and a lone **Close**, with no title and no icon. That is the reference's layout. The
  markup is the reference's, line for line.
