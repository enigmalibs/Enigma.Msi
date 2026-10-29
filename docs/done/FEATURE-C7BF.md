# FEATURE-C7BF — App icon in the output directory

## Summary

The app icon is now written beside `Enigma.Msi.Desktop.exe` as **`app.ico`**, the name every other Enigma
app ships. It lands in the build output and in the publish folder, and from 1.7.0 on, through the MSI's
`releasePath`, in the installed folder too. Shortcuts, launchers and installers can point at the file instead
of extracting the icon from the executable.

The mechanism is one item in the desktop csproj:
`<None Include="Assets/appicon.ico" TargetPath="app.ico" CopyToOutputDirectory="PreserveNewest" />`. The
asset keeps its name, `appicon.ico`, which the XAML and the MSI profiles use, and it stays an
`AvaloniaResource`.

## Files/modules touched

- Modified:
  - `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`
  - `tests/Enigma.Msi.Desktop.UnitTests/HeadlessAvaloniaFixture.cs` (`Run<TResult>`)
  - `docs/roadmap.md`, `docs/plan/FEATURE-C7BF.md`
- Created:
  - `tests/Enigma.Msi.Desktop.UnitTests/AppIconTests.cs`
  - `docs/done/FEATURE-C7BF.md`

## Deviations & follow-ups

- **The fixture gained `Run<TResult>(Func<TResult>)`**, which the byte comparison needs. FEATURE-CED6 had left
  it out.
- **The first version of `Run<TResult>` overflowed the stack.** It was copied from Enigma.GitClient as
  `Run(() => result = function())`, and an expression-bodied assignment converts to `Func<TResult>` too, so
  overload resolution picked the method itself. It now uses a block body, and a comment says why.
- **Follow-up outside this repo:** Enigma.GitClient's `HeadlessAvaloniaFixture.Run<TResult>` has the same
  latent recursion. It only fires on the first call.
- **Follow-up about MTP's summary.** While the test host was crashing with that stack overflow, the `dotnet
  test` summary still printed "failed: 0". From here on, each run is checked on its exit code and its three
  per-assembly `passed` lines as well as its totals.
- The MSI profiles' `productIcon` and `iconPath` still point at the source asset. WixSharp embeds the icon at
  build time, so this item changes nothing there.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| What the test compares against | The embedded asset (`AssetLoader`), not the source file | Independent of where the repository is checked out, and it proves the file and the resource are one icon |
| Where the test lives | The suite's root, `AppIconTests` | It pins an output-layout fact. It is neither a service, a ViewModel nor a view |

## Documentation sweep

- Nothing became stale. The guide's "the output folder, with its `worker\` subfolder" is still true, and the
  icon file is announced in the 1.7.0 notes (FEATURE-AB6B).

## Build/test evidence

- `dotnet build Enigma.Msi.slnx -c Release`: **0 warnings**, 0 errors.
  `src/Enigma.Msi.Desktop/bin/Release/net10.0/app.ico` is byte-identical to `Assets/appicon.ico` (`cmp`).
- `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64`:
  `bin/Release/net10.0/win-x64/publish/app.ico` is byte-identical to the asset, and it is the only `.ico`
  there.
- `dotnet test --solution Enigma.Msi.slnx -c Release`: **370/370 passed**, exit 0, all three assemblies
  `passed`. The two new tests are `AppIconTests`: the file is present, and it is the embedded asset byte for
  byte.
