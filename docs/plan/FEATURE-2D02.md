# FEATURE-2D02 — Desktop app 1.3.0: quick-start output folder, splash restyle & de-NuGet

**Status:** DONE (multi-phase — 4 phases)
**Type:** FEATURE (multi-phase)
**Branch (per phase, at build time):** `feature/feature-2d02-phaseNN-<slug>` — one branch per phase, cut from current `HEAD`.
**Depends on:** FEATURE-74C4 fully DONE (app at 1.2.0; this item builds the next app release on top).

## Objective

Ship **Enigma.Msi.Desktop 1.3.0**, and settle what this repository actually publishes.

1. **Quick start gains an Output folder field** — the seventh answer. 1.2.0 derived the output folder
   silently (the release folder's *parent*); from here the user states it.
2. **The splash screen is restyled to match Enigma.MarkdownEditor** — logo, name, version, and nothing
   else. The author's name comes off it.
3. **The library stops being a NuGet package.** `Enigma.Msi` will never be published to nuget.org; the
   packaging machinery and every claim of publication are removed from the repository.
4. **Release 1.3.0** — app-only, prepared *and* verified on Windows, dogfood MSI included.

**The Enigma.Msi library stays at 1.0.0.** It is not re-released, and after PHASE03 there is no such
thing as releasing it: the app's MSI is the only artifact this repository produces. The model, the
validator, the `.msipkg.json` contract (`schemaVersion` 1), the build client and the `net472` worker are
functionally untouched throughout, so profiles stay interchangeable between 1.0.0, 1.1.0, 1.2.0 and
1.3.0 in both directions and there is no migration.

## Context & constraints

Read from the repository at planning time (2026-09-03), and binding on every phase:

- **Zero-warning builds.** `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` are on solution-wide; a
  warning fails the build. The XAML compiler's `AVLN5001` is part of that — `PlaceholderText`, never the
  obsolete `Watermark`, on the control library's `TextBox`-derived editors.
- **MTP-native tests.** `dotnet test --solution Enigma.Msi.slnx -c Release`; the `--solution` flag is not
  optional on the .NET 10 SDK in MTP mode.
- **The coupled UI set.** Avalonia 12.1.1 + `Enigma.Avalonia.Desktop` 1.0.0 + `Enigma.Icons.Avalonia`
  1.0.0 move together or not at all. Nothing in this item bumps any of them.
- **The shared `ContentDialog`'s two traps** (already worked around in `QuickStartDialogService`, and
  still binding here): its host's reset *assigns* `IsPrimaryButtonEnabled` rather than clearing it, so
  the `CanApply` binding must stay disposed on close; and `ShowAsync` does not reset the six `Dialog*`
  size properties, so the service states every one of them explicitly.
- **The validation split.** In-memory rules run on every keystroke; disk rules (including
  `RequireExistingDirectory(package.Output.OutputPath, …)`) run on Validate and Build. The quick start
  applies string/parse rules only and never touches the disk — an output folder that does not exist is
  reported once, by the Problems pane, and not half-reported in the dialog.
- **The `net472` split and `netstandard2.0`.** The worker consumes the *same* model assembly, which is
  what `netstandard2.0` is for. It stays, and so do `net8.0`/`net10.0` — the TFM set is **not** touched by
  this item (see *User decisions*).
- **`build/CopyWorkerOutput.targets` is load-bearing and stays.** It is the in-repo counterpart that puts
  the worker in `$(OutDir)worker/` for the app and the tests; only the *packed* `build/Enigma.Msi.targets`
  goes away in PHASE03.

### Verified against the code at planning time (2026-09-03)

- `PackageEditorViewModel.ApplyQuickStart` sets `OutputPath = ParentDirectoryOrSelf(settings.ReleasePath)`;
  `ParentDirectoryOrSelf` has that single call site.
- `QuickStartViewModel` holds six observable answers; `CanApply` is a computed property with
  `[NotifyPropertyChangedFor]` wiring, and the two file pickers gate on `HasReleasePath`.
- `SplashViewModel` is referenced from `App.axaml.cs` (`DataContext`), `ServiceCollectionExtensions`
  (`AddTransient`) and `SplashWindow.axaml` (`x:DataType`) — and nowhere else. It has no test file.
- `AppInfo.Author` and `AppInfo.Tagline` are also used by `AboutCard`/`AboutViewModel`; both stay.
- The desktop test suite is ViewModel-only — no Avalonia headless platform anywhere in the repository.
- `build/Enigma.Msi.targets` is referenced by: the library csproj's pack item, two XML doc comments
  (`MsiBuildService.cs:59`, `MsiBuildServiceOptions.cs:15`), `build/CopyWorkerOutput.targets`'s header
  comment, `docs/guides/building.md` and `CLAUDE.md`.
- `<Version>1.0.0</Version>` sits **inside** the packaging `PropertyGroup` of `Enigma.Msi.csproj` — it has
  to survive the removal of that group.
- The 1.2.0 MSI profile's `upgradeCode` is `3405046f-527a-439e-a22f-866247dc8314`.

### User decisions (interview, 2026-09-03)

1. **Output folder: blank and required.** No derivation, no fallback — a seventh answer like the other
   six. Placed **immediately after Release folder**.
2. An output folder **inside** the release folder is accepted: a permanent hint, no warning, no block.
3. **Splash: visual parity only.** MarkdownEditor's markup; the fixed 2 s timer, click/key dismissal and
   `App`'s handover stay exactly as they are. No composition-behind-splash restructure.
4. **Splash content: logo, name, `Version X.Y.Z`.** Tagline and author line removed *from the splash*.
5. **The author name stays in the About dialog** — `AppInfo.Author` is not removed.
6. **Splash tests: headless Avalonia**, as MarkdownEditor does (`Avalonia.Headless.XUnit` 12.1.1).
7. **The packaging is removed entirely** — not merely the publication claims. `Enigma.Msi` becomes an
   in-repo library consumed by `ProjectReference`.
8. **`build/Enigma.Msi.targets` is deleted.**
9. **`README.md` is reframed around the desktop app**, with the library described afterwards as the
   engine behind it. The app is obtained by **building from source** — no Releases-page promise.
10. **`docs/RELEASE.md` is rewritten as the app release runbook.**
11. **The guides are kept**; only their NuGet wording is corrected.
12. **1.3.0 is app-only**; the library stays at 1.0.0.
13. **The dogfood MSI is built in-phase**, on Windows.
14. **No dependency refresh** — everything holds at its pinned version. The single addition is the
    headless test package in PHASE02.
15. **The TFM set is unchanged** (`netstandard2.0;net8.0;net10.0`).
16. **Four phases**, de-NuGet before the release.

### Defaults recorded at planning (low-impact, applied without asking)

- The output field gets a **Browse…** button opening a folder picker at its current value, ungated (it
  depends on nothing).
- The hint uses the card's existing `.hint` style.
- `QuickStartSettings` keeps its positional-record shape; `OutputPath` is inserted **after**
  `ReleasePath`, mirroring the field order.
- `SECURITY.md` keeps both artifact rows; only the "(NuGet)" labelling changes.
- `RELEASENOTES.md` keeps a version row for the library at 1.0.0.
- The reframed README carries no screenshot — the repository has none.
- PHASE03 adds `<IsPackable>false</IsPackable>` to the library so an accidental `dotnet pack` fails
  loudly instead of producing a worker-less package.

---

## PHASE01 — Quick start: output folder field

**Status:** DONE
**Branch:** `feature/feature-2d02-phase01-quickstart-output-folder`

1. `src/Enigma.Msi.Desktop/ViewModels/QuickStartSettings.cs` — add `OutputPath` after `ReleasePath`,
   with its XML `<param>` doc: where the `.msi` is written, stated by the user rather than derived.
2. `src/Enigma.Msi.Desktop/ViewModels/QuickStartViewModel.cs`:
   - a seventh `[ObservableProperty] string _outputPath` with `[NotifyPropertyChangedFor(nameof(CanApply))]`;
   - `CanApply` gains `&& !string.IsNullOrWhiteSpace(OutputPath)`;
   - `ToSettings()` carries `OutputPath.Trim()`;
   - a `BrowseOutputPathAsync` `[RelayCommand]` over `IPathPickerService.PickFolderAsync`, opening at the
     current `OutputPath`, with no `CanExecute` gate — unlike the icon and executable pickers, it does not
     depend on the release folder;
   - update the class-level `<remarks>`: the field order is still a dependency order, and the output
     folder is deliberately *not* derived any more.
3. `src/Enigma.Msi.Desktop/Views/QuickStartCard.axaml`:
   - insert the `Output folder` `TextEditor` **between** `Release folder` and `Icon`, with its Browse
     button and a `.hint` `TextBlock` beneath it — "Where the `.msi` is written. Typically the release
     folder's parent, so the installer lands beside the payload rather than inside it."; wrap the editor
     and its hint in a `StackPanel`, as the executable field already is;
   - the intro sentence becomes seven answers, and stops promising a derived output folder.
4. `src/Enigma.Msi.Desktop/ViewModels/PackageEditorViewModel.cs`:
   - `ApplyQuickStart` sets `OutputPath = settings.OutputPath`;
   - delete `ParentDirectoryOrSelf` (its only call site is gone) and correct the `<remarks>` list of what
     the quick start derives.
5. `QuickStartDialogService` — no change expected; confirm the card still fits (`DialogHeight` is `NaN`
   with `DialogMaxHeight` infinite, so the extra field grows the dialog rather than clipping it). If the
   card exceeds a sensible height on a 1080p screen, cap it here and state every `Dialog*` property, per
   the trap above.
6. Tests, `tests/Enigma.Msi.Desktop.UnitTests`:
   - `QuickStartViewModelTests` — rename/extend the six-answer cases to seven; `CanApply` false while
     `OutputPath` is blank (theory row added); `ToSettings` carries and trims it;
     `BrowseOutputPath_TakesThePickedFolder` and its cancelled counterpart; assert the browse command is
     executable with **no** release folder set (the ungated-picker rule).
   - `PackageEditorViewModelTests` — `ApplyQuickStart` puts the *entered* output folder on the form, and
     no longer derives the release folder's parent (a case with an output folder that is neither the
     release folder nor its parent).
   - `docs/guides/desktop-app.md` — the quick-start section must describe seven fields.

**Acceptance criteria**

- The quick start shows seven fields in the order name, version, manufacturer, release folder, output
  folder, icon, main executable; the output field has a working folder picker and a permanent hint.
- Apply stays disabled while the output folder is blank, and enabled once all seven answers are valid.
- Applying puts the entered output folder on the form verbatim; nothing derives it any more, and
  `ParentDirectoryOrSelf` no longer exists.
- An output folder inside the release folder applies without warning or block.
- A package produced by the quick start (with an existing output folder) still passes Validate with zero
  problems.
- Solution builds warning-free; the whole suite passes, including the new cases.

---

## PHASE02 — Splash screen restyle & headless tests

**Status:** DONE
**Branch:** `feature/feature-2d02-phase02-splash-restyle`

Target: Enigma.MarkdownEditor's `SplashWindow` — read at
`C:\Dev\EnigmaLibs\Enigma.MarkdownEditor\src\Enigma.MarkdownEditor.Desktop\Views\SplashWindow.axaml`.
**Markup only. The window's behaviour and the start-up sequence are out of scope.**

1. `src/Enigma.Msi.Desktop/Views/SplashWindow.axaml`:
   - `Width` 420, `Height` 260 (from 480×280); `d:DesignWidth`/`d:DesignHeight` to match;
   - drop `x:DataType` and the `vm:` namespace; read the two values through
     `{x:Static services:AppInfo.Name}` and `{x:Static services:AppInfo.Version}`;
   - content: the 96 px logo, the name at `FontSize` 18 / `SemiBold` (from 28), then a horizontal
     `StackPanel` with `Spacing="5"` holding a literal `Version` label and the version, both in
     `EnigmaForegroundSecondaryBrush`; `Spacing="12"` on the outer stack;
   - **remove the tagline and the author `TextBlock`s**;
   - border brush `EnigmaBorderSubtleBrush` (from `EnigmaBorderBrush`); `Background` stays
     `EnigmaSurfaceBrush`; `CanResize`, `ShowInTaskbar`, `Topmost`, `WindowDecorations`,
     `WindowStartupLocation` all unchanged;
   - carry over MarkdownEditor's comment on why there is no ViewModel, in this repository's voice.
2. `src/Enigma.Msi.Desktop/ViewModels/SplashViewModel.cs` — **delete**.
3. `src/Enigma.Msi.Desktop/ServiceCollectionExtensions.cs` — remove the `AddTransient<SplashViewModel>()`
   registration (`AddTransient<SplashWindow>()` stays).
4. `src/Enigma.Msi.Desktop/App.axaml.cs` — remove the `splash.DataContext = …` line and the now-unused
   `using`; the `Dismissed` subscription, the `desktop.MainWindow = splash` assignment and
   `ShowMainWindow` are untouched.
5. `src/Enigma.Msi.Desktop/Views/SplashWindow.axaml.cs` — **no change**: the 2 s `Duration`, the timer,
   `OnPointerPressed`/`OnKeyDown` and the single-fire `Dismiss` all stay.
6. Test infrastructure:
   - `Directory.Packages.props` — add `Avalonia.Headless.XUnit` at **12.1.1**, inside the coupled-set
     block and under its comment, so it moves with the rest;
   - `tests/Enigma.Msi.Desktop.UnitTests/Enigma.Msi.Desktop.UnitTests.csproj` — reference it, with a
     comment saying why the suite now needs a headless platform;
   - add `tests/Enigma.Msi.Desktop.UnitTests/Views/SplashWindowTests.cs`, ported from MarkdownEditor's:
     the markup loads and shows `AppInfo.Name` and `AppInfo.Version`; the logo `Image` has a non-null
     `Source` (a missing `AvaloniaResource` fails as the markup loads); the window flags are the splash
     ones (`WindowDecorations.None`, `CenterScreen`, not in taskbar, not resizable, topmost); and the
     background and border brushes resolve in **both** theme variants;
   - whatever headless bootstrapping the runner needs (an `[AvaloniaTestApplication]`-style hook over the
     app's `BuildAvaloniaApp`) lands in this suite only — no other suite gains a UI dependency.
7. Documentation: `docs/guides/desktop-app.md`'s splash paragraph, if it enumerates the lines shown.

**Acceptance criteria**

- The splash shows the logo, `Enigma.Msi` and `Version 1.2.0` (the running version) — no tagline, no
  author — at 420×260 with the subtle border, and is still dismissed by the 2 s timer, a click or a key.
- `SplashViewModel` is gone from the codebase and from DI; nothing references it.
- The About dialog is unchanged and still shows the tagline, the copyright and the author.
- The new headless tests pass, and the rest of the suite is unaffected.
- Solution builds warning-free (including the XAML compiler) and the whole suite passes.

---

## PHASE03 — Drop NuGet packaging & publication

**Status:** DONE
**Branch:** `feature/feature-2d02-phase03-de-nuget`

`Enigma.Msi` will never be published to nuget.org. This phase removes the machinery *and* the claims. No
behaviour changes: the library, the worker, the app and every test keep working exactly as they do — the
app and the worker already consume the library by `ProjectReference`, and the worker already reaches the
app's output through `build/CopyWorkerOutput.targets`.

1. `src/Enigma.Msi/Enigma.Msi.csproj`:
   - **keep** `<Version>1.0.0</Version>` (move it into the first `PropertyGroup`), `<OutputType>`,
     `<TargetFrameworks>` and `<GenerateDocumentationFile>`;
   - delete the packaging `PropertyGroup` (`PackageId`, `Title`, `Description`, `PackageTags`,
     `PackageReadmeFile`, `PackageLicenseFile`, `RepositoryUrl`, `RepositoryType`, `PackageProjectUrl`,
     `PackageReleaseNotes`) and its header comment;
   - delete the README/LICENSE `Pack` `ItemGroup`, the `build/Enigma.Msi.targets` pack item, and the
     whole **`PackEnigmaMsiWorkerPayload`** target with its `Restore`/`Build` `MSBuild` invocations, its
     glob and its missing-payload guard;
   - add `<IsPackable>false</IsPackable>` with a one-line comment: this library is consumed in-repo by
     `ProjectReference` and is not published anywhere.
2. **Delete `build/Enigma.Msi.targets`.** `build/CopyWorkerOutput.targets` stays; correct its header
   comment, which introduces itself as the in-repo counterpart of the deleted file.
3. Correct the two XML doc comments pointing at it: `src/Enigma.Msi/Build/MsiBuildService.cs:59` and
   `src/Enigma.Msi/Build/MsiBuildServiceOptions.cs:15` — worker discovery is unchanged (options override,
   else `AppContext.BaseDirectory/worker/`), only the sentence about who puts it there.
4. **`README.md` — reframed around the desktop app:**
   - badges: the NuGet badge goes, the MIT badge stays;
   - the app leads — what Enigma.Msi.Desktop is and does (quick start, the five sections, validation
     problems, the build progress card, splash/About), and how to get it: **build from source**, with the
     `dotnet publish … -r win-x64 --self-contained false` + worker-CLI pair, plus the Windows and
     `dotnet tool install --global wix` requirements. No Releases-page link, no `dotnet add package`;
   - the library follows as the engine: the Features list, "WixSharp never leaks", the `MsiPackage`
     quick-start snippet, and how it is consumed (clone + `ProjectReference` — it is not on any feed);
   - the *Documentation* section keeps pointing at `docs/guides/` in prose;
   - a what's-new callout for **1.3.0** (this is the release's callout — PHASE04 need not rewrite it);
   - the supported-TFM sentence stays factually true (`netstandard2.0`, `net8.0`, `net10.0`) but stops
     reading as consumer install guidance.
5. **`RELEASENOTES.md`** — the header paragraph and the current-version table stop calling the library a
   NuGet package (line 3 and the table around line 9): two artifacts, one released as an MSI, one an
   in-repo library at 1.0.0. **Historical 1.0.0/1.1.0/1.2.0 sections are not rewritten** — they are the
   record of what was true then. PHASE04 adds the 1.3.0 section on top and states the change there.
6. **`SECURITY.md`** — drop "(NuGet)" from the supported-versions table and the sentence above it; the
   scope, reporting and expectations sections stay as they are.
7. **`docs/RELEASE.md` — rewritten as the app release runbook.** One artifact, one flow:
   pre-flight (`dotnet build`/`dotnet test --solution`, both `-c Release`) → app `<Version>` bump →
   `RELEASENOTES.md` section + README callout → MSI profile clone (keep the profile-clone rule and the
   GUID contract verbatim: new `productId`, `upgradeCode` reused) → publish + worker MSI build → tag →
   post-release verification (install/launch/uninstall, and that the MSI upgrades the installed version).
   Sections 0 (release flavours), 4 (Pack), 5 (Push to NuGet), 6 (post-publish nuget.org verification)
   and the pack-verify prose go. Keep the *"nothing here ever runs an outward-facing command"* rule and
   the bare-`X.Y.Z` tag convention.
8. **`docs/guides/`** — correct only the NuGet wording:
   - `building.md`: the worker-on-disk table row (line ~25) and the *Worker discovery* paragraph
     (~226–227) — the worker is copied into the output by `build/CopyWorkerOutput.targets` from the
     worker project's build output, not by a package;
   - `worker-cli.md`: the "inside the `Enigma.Msi` nupkg" row and the sentence after it (~105–110) — the
     self-contained worker payload is the `worker/` folder beside the host, or the worker project's own
     `bin/Release/net472/`.
   Do not restructure the guides; they stay a library reference.
9. **`CLAUDE.md`** — the build-state block (the "packable / `dotnet pack` produces a complete nupkg"
   claims, the "never been packed and pushed to NuGet" note, the runbook's pack/push steps), the project
   layout line for `build/Enigma.Msi.targets`, the *"`src/Enigma.Msi` is the one worker host that cannot
   use that contract"* paragraph (the `PackEnigmaMsiWorkerPayload` rationale — now historical), and the
   `dotnet pack` command under *Build & test*. Add FEATURE-2D02 to the state block per the house style.
10. Verify nothing else claims publication: re-run a repo-wide search for `nuget`/`nupkg`/`dotnet add
    package` and confirm every remaining hit is either a historical `docs/done/`/`docs/plan/` record
    (left alone — they are the record of what was done) or `Directory.Packages.props`/`global.json`
    plumbing (unrelated: consuming packages is not publishing one).

**Acceptance criteria**

- `dotnet build Enigma.Msi.slnx -c Release` is warning-free and the **entire** suite passes — the library,
  the worker and the app all build and run exactly as before.
- `src/Enigma.Msi/Enigma.Msi.csproj` carries no packaging metadata, no pack items and no pack target, and
  declares `IsPackable=false`; `<Version>1.0.0</Version>` survives.
- `build/Enigma.Msi.targets` is deleted and nothing references it — code comments, guides and `CLAUDE.md`
  included. `build/CopyWorkerOutput.targets` is intact and the app's `$(OutDir)worker/` payload still
  lands (confirmed by the app building and the worker being found).
- `README.md` leads with the desktop app, carries no NuGet badge and no `dotnet add package`, and tells a
  reader to build the app from source.
- `RELEASENOTES.md`, `SECURITY.md`, `docs/RELEASE.md`, the two guides and `CLAUDE.md` no longer describe
  the library as published or publishable; historical `docs/done/` and `docs/plan/` records are untouched.
- `docs/RELEASE.md` reads top-to-bottom as the app release runbook, with no pack/push sections.

---

## PHASE04 — Release prep: desktop app 1.3.0

**Status:** DONE
**Branch:** `feature/feature-2d02-phase04-release-1-3-0`

Follows the runbook as PHASE03 rewrote it. **App-only:** no pack, no push, no library version change —
and after PHASE03 there is no pack step to skip.

1. `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`: `<Version>` **1.3.0**; update the comment above it
   if it still frames the library as the thing being published.
2. `msiProfiles/Enigma.Msi.Desktop.1.3.0.msipkg.json` — clone of the 1.2.0 profile with: `version` 1.3.0;
   a **fresh `productId`** from a real generator (`[guid]::NewGuid()` — never hand-fabricated);
   **`upgradeCode` reused verbatim** (`3405046f-527a-439e-a22f-866247dc8314`, the app's permanent
   identity, and what makes 1.3.0 *upgrade* an installed 1.2.0 rather than install beside it); everything
   else unchanged, `releasePath` still the win-x64 **publish** output. The older profiles are kept.
3. Verify the new profile deserializes through `MsiPackageJson` and passes the validator's in-memory rules
   with zero errors (no worker or WixSharp invocation needed for this step).
4. `RELEASENOTES.md`: a **1.3.0** section on top covering the quick-start output folder (a stated seventh
   answer, replacing the derived parent folder — note that existing profiles are unaffected), the splash
   restyle (logo, name, version; no tagline or author; behaviour unchanged), and **the packaging change**
   — the library is no longer packaged or publishable, `build/Enigma.Msi.targets` is gone, and consumers
   in-repo use `ProjectReference`. Restate that the library stays at 1.0.0 and that the profile format is
   unchanged (`schemaVersion` 1, interchangeable in both directions). Update the current-version table.
5. Windows verification, from the repository root, with the real numbers recorded:
   - `dotnet build Enigma.Msi.slnx -c Release` — zero warnings;
   - `dotnet test --solution Enigma.Msi.slnx -c Release` — all four suites, including the `net472` worker
     suite and its WixSharp **drift guards**. A drift-guard failure is a real finding, not a flake: stop
     and report it.
6. **Dogfood MSI build** (Windows + WiX CLI — this machine has WiX 7.0.0):
   `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64 --self-contained false`,
   then the worker CLI over `msiProfiles/Enigma.Msi.Desktop.1.3.0.msipkg.json`. Record the MSI path and
   size in the completion doc (1.2.0's was 17.25 MB) — never silently skipped. Install/launch/uninstall
   verification stays with the maintainer.
7. Documentation freshness: `docs/guides/desktop-app.md` must match the shipped 1.3.0 UI (seven-field
   quick start, restyled splash); `CLAUDE.md`'s build-state block gains the 1.3.0 outcome; `README.md`'s
   what's-new callout says 1.3.0 (PHASE03 already wrote it — confirm it matches the final notes).
8. **Nothing outward-facing is run from here** — no tag, no push, no upload. The printed runbook is the
   deliverable.

**Acceptance criteria**

- `<Version>` is 1.3.0; the 1.3.0 release-notes section and the version table are in place; the 1.3.0
  profile is committed with a fresh `productId` and the verbatim `upgradeCode`, deserializes, and reports
  zero in-memory validation errors.
- The full solution builds warning-free and the **entire** test suite passes on Windows.
- The 1.3.0 dogfood MSI is built from the committed profile and recorded (path + size), or explicitly
  blocked with the reason.
- `CLAUDE.md`, the desktop guide and the README describe the repository as it actually is after this
  phase.
- The library is still at 1.0.0, and nothing was packed, tagged or pushed.
