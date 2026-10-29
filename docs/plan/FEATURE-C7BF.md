# FEATURE-C7BF — App icon in the output directory

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-c7bf-icon-in-output-dir`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

The app icon is written to the build output directory — and therefore to the publish directory and the
installed folder — next to `Enigma.Msi.Desktop(.exe)`, as `app.ico`. Shortcuts, launchers and installers
can then point at a file instead of extracting it from the executable. Part of the uniform baseline.

## Context & constraints

- `src/Enigma.Msi.Desktop/Assets/appicon.ico` (ten PNG frames, 16–256) is the `ApplicationIcon`; `Assets/**`
  is an `AvaloniaResource` (embedded), so nothing lands in `bin/` today. The file name was kept on purpose
  by FEATURE-25EF (csproj, XAML, profiles name it) and stays.
- Every other Enigma app's icon is `Assets/app.ico`; Enigma.GitClient's FEATURE-5096 copies it to the
  output root as `app.ico`.
- An item with `CopyToOutputDirectory` is also published, and flows to referencing projects' output (the
  desktop test project), which is where a test can check it.
- The MSI profile's `releasePath` is the publish folder, so `app.ico` is installed with the app from 1.7.0
  on; the profile's `productIcon`/`iconPath` keep pointing at the source asset (WixSharp embeds it at build
  time) — unchanged.

## Steps

1. `Enigma.Msi.Desktop.csproj`: `<None Include="Assets/appicon.ico" CopyToOutputDirectory="PreserveNewest"
   TargetPath="app.ico" />` (or `Link`-equivalent), with a one-line why. It stays an `AvaloniaResource`.
2. Test (desktop suite): `app.ico` sits in `AppContext.BaseDirectory`, byte-identical to the asset
   (located from the source tree, as an existing test locates repo files, or compared to the embedded
   resource through `AssetLoader`).
3. Verify: a Release build puts `app.ico` in `bin/Release/net10.0/`; a `win-x64` publish puts it in
   `publish/` — both byte-identical to `Assets/appicon.ico`.

## Acceptance criteria

- `bin/<Configuration>/net10.0/app.ico` and the publish folder's `app.ico` exist, byte for byte the asset.
- The embedded resource still resolves (`avares://Enigma.Msi.Desktop/Assets/appicon.ico` unchanged).
- Release build warning-free; the whole suite green.

## Out of scope

- PNG copies, the Linux hicolor icons (FEATURE-22F0), changing the MSI profile's icon paths.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Output name | `app.ico` | The name every other Enigma app ships; uniform across apps | `appicon.ico`; `Enigma.Msi.Desktop.ico` |
| Source asset name | Unchanged (`appicon.ico`) | FEATURE-25EF kept it on purpose; renaming touches profiles and XAML for nothing | Renaming to `app.ico` |
| Mechanism | `None` + `CopyToOutputDirectory` + `TargetPath` | Standard MSBuild; flows to publish with no custom target | A custom `Copy` target |
| Profile icon paths | Unchanged | WixSharp embeds the icon at build time from the source path | Pointing them at the installed file |
| Test | One desktop-suite test + a manual build/publish check | Pins the copy; the publish check is the real proof | No test |
