# FEATURE-CED6 — Update NuGet packages to latest

## Summary

Every package pinned in `Directory.Packages.props` is at its latest stable version on nuget.org
(2026-10-29), and a re-run of `dotnet list package --outdated` reports no updates for any project:

| Package | From | To |
|---|---|---|
| Avalonia, .Desktop, .Fonts.Inter, .Themes.Fluent, .Headless | 12.1.1 | 12.1.4 |
| Enigma.Avalonia.Desktop | 1.0.0 | 1.3.0 |
| Microsoft.Extensions.Hosting, System.Text.Json | 10.0.10 | 10.0.14 |
| NLog, NLog.Extensions.Logging | 6.1.4 | 6.2.2 |
| WixSharp_wix4 | 2.14.1 | 2.14.2 |
| xunit.v3 | 3.2.2 | 4.0.2 |
| coverlet.collector | 10.0.1 | 10.1.0 |
| NSubstitute | 6.1.0 | 6.2.0 |

The coupled Avalonia set moved as a whole: Enigma.Avalonia.Desktop 1.3.0's nuspec pins Avalonia 12.1.4.

**`Avalonia.Headless.XUnit` was dropped.** Its 12.1.4 build is compiled against xunit.v3 3.2.2. Under 4.0.2
every `[AvaloniaFact]` failed discovery with
`MissingMethodException: … TestIntrospectionHelper.GetTestCaseDetails(…)`. The desktop suite now drives the
headless platform through its own `HeadlessAvaloniaFixture`, the fixture Enigma.GitClient uses. It owns a
dedicated UI thread, sets the real `App` up with `SetupWithoutStarting`, and runs test bodies through
`Run(...)`. Tests join `[Collection(HeadlessCollection.Name)]`. The 6 splash tests were moved onto it,
unchanged in what they assert.

## Files/modules touched

- Modified:
  - `Directory.Packages.props`
  - `tests/Enigma.Msi.Desktop.UnitTests/Enigma.Msi.Desktop.UnitTests.csproj`
  - `tests/Enigma.Msi.Desktop.UnitTests/Views/SplashWindowTests.cs`
  - `tests/Enigma.Msi.Worker.UnitTests/Translation/WixSharpDriftTests.cs` (comments)
  - `README.md`
  - `CLAUDE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-CED6.md`
- Created:
  - `tests/Enigma.Msi.Desktop.UnitTests/HeadlessAvaloniaFixture.cs`
  - `tests/Enigma.Msi.Desktop.UnitTests/HeadlessCollection.cs`
  - `docs/done/FEATURE-CED6.md`
- Deleted: `tests/Enigma.Msi.Desktop.UnitTests/HeadlessTestApp.cs`

## Deviations & follow-ups

- **Plan step 5 said to hold an incompatible package back.** I did not do that. The blocker was the xunit
  adapter, not xunit itself, and replacing the adapter with a 100-line fixture is the change Enigma.GitClient
  had already made. That put every package at latest with no version override. Holding xunit.v3 at 3.2.2, or
  giving the desktop suite its own `VersionOverride`, were the alternatives.
- **The `ContentDialog` traps were re-checked against 1.3.0's source** (`ContentDialogService.ResetDialog`).
  Both still hold: the reset *assigns* `IsPrimaryButtonEnabled = true`, and it does not touch the six
  `Dialog*` size properties.
- **Follow-up for FEATURE-AB6B — the seeded `.wixsharp` set is not yet confirmed against 2.14.2.** It is
  checked for location and up-to-dateness by `WixSharpBuildDirTests`, which pass. Whether WixSharp 2.14.2
  writes *additional* files there is only observable from a real MSI build. The release's dogfood build has
  to look, and until then the worker csproj and that test keep their "2.14.1" wording. The four drift-test
  comments now say 2.14.2, because those member sets are exactly what the passing tests prove.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| xunit.v3 4.x vs Avalonia.Headless.XUnit | Drop the adapter, use a GitClient-style fixture | Latest everywhere, the same infrastructure as GitClient, no per-project override |
| Fixture drawing | Headless drawing, no `Avalonia.Skia` | The tests read the control tree; GitClient's Skia is for pixel snapshots this suite does not take |
| Splash tests | Converted, not deleted | They are deleted with the splash by FEATURE-5E0B. Here they prove the fixture works |

## Documentation sweep

- `README.md`: WixSharp_wix4 is now 2.14.2.
- `CLAUDE.md`: the coupled-set versions (12.1.4 / 1.3.0), why `Avalonia.Headless.XUnit` is absent, and the
  desktop suite's headless paragraph now describes the fixture.

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release`: **0 warnings**, 0 errors. The worker output carries WixSharp
  2.14.2.0, and the seeded `.wixsharp/` holds the same six files as before.
- `dotnet test --solution Enigma.Msi.slnx -c Release`: **354/354 passed**, including the 16 WixSharp drift
  guards and the `WixSharpBuildDirTests`.
- Before the fixture, the same run had **6 failed** (the splash tests, at discovery) and 348 passed.
- `dotnet list Enigma.Msi.slnx package --outdated`: "no updates given the current sources" for all seven
  projects.
