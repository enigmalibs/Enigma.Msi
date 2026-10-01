# Release notes

Release notes for both artifacts of this repository: the **Enigma.Msi.Desktop** application, released as an
MSI installer, and the **Enigma.Msi** library it is built on, which is **not published anywhere** — it is
consumed in-repo by `ProjectReference` and stays at 1.0.0. The two version independently — they coincided
at 1.0.0 for the first release and part from 1.1.0 onwards. Newest release first.

| Artifact | Current version |
|---|---|
| Enigma.Msi.Desktop (application, MSI) | **1.4.0** |
| Enigma.Msi (library, in-repo) | **1.0.0** |

The sections below are the record of each release as it was made, and are not rewritten: the earlier ones
describe the library as a NuGet package, which it no longer is — and, as it turned out, never was, since
no version of it was ever pushed to a feed.

## 1.4.0 — Enigma.Msi.Desktop

An **application-only release**, and a small one: the quick start shows the MSI file name it is about to
use, and lets it be changed before Apply rather than after. **The library remains at 1.0.0 and is not
re-released.** The model, the validator, the build client, the `net472` worker and the `.msipkg.json`
format (`schemaVersion` 1) are all untouched, so profiles saved by 1.0.0 to 1.3.0 open in 1.4.0 unchanged
and there is no migration.

### What's new

- **The quick start shows the MSI file name — eight answers, one of them pre-filled.** **MSI file name**
  sits directly under Version, with the `.msi` unit beside it, and **fills itself in as the application
  name and the version are typed**: characters a file name cannot hold are dropped, every run of spaces
  becomes a dot and the version is appended — `Enigma Msi` at `1.4.0` gives **`Enigma.Msi.1.4.0`**. It is
  an ordinary answer otherwise: **type a name of your own and it is left alone** from then on, whatever
  happens to the name and the version afterwards; empty the field and it follows them again from their
  next edit. Apply stays disabled while it is blank, holds an invalid character or ends in `.msi`, and the
  dialog says which. 1.3.0 derived the name silently, from the application name alone, at Apply — and fell
  back to `package` when nothing of the name survived; that fallback is gone, because the field is now in
  plain sight.
- **What a quick start names the installer has changed** — `Widget` at `2.1.0` now gives `Widget.2.1.0`
  where 1.3.0 gave `Widget`. **Saved profiles are unaffected:** `msiFilename` has always been a stored
  field, so whatever a 1.0.0 to 1.3.0 profile carries opens verbatim, and the main form's *MSI file name*
  field is the same plain field it always was.

### Compatibility

- No change to the `.msipkg.json` profile format — still **`schemaVersion` 1**. Profiles are
  interchangeable between 1.0.0, 1.1.0, 1.2.0, 1.3.0 and 1.4.0 in **both** directions.
- **The library's public surface is unchanged** — still 1.0.0, still in-repo by `ProjectReference`, still
  multi-targeting `netstandard2.0`, `net8.0` and `net10.0`.
- No dependency moved: every package holds at the version 1.3.0 shipped with.
- The app still targets **.NET 10.0** (`WinExe`), is published framework-dependent for `win-x64`, and
  still requires **Windows and the WiX CLI** (`dotnet tool install --global wix`) to build an MSI.

## 1.3.0 — Enigma.Msi.Desktop

An **application-only release**, and the one that settles what this repository publishes. The quick start
stops guessing where the `.msi` should go, the splash screen is down to what identifies the app, and the
**Enigma.Msi library stops being a NuGet package** — it was never published to a feed, and now it cannot
be. **The library remains at 1.0.0 and is not re-released.** The model, the validator, the build client,
the `net472` worker and the `.msipkg.json` format (`schemaVersion` 1) are all untouched, so profiles saved
by 1.0.0, 1.1.0 or 1.2.0 open in 1.3.0 unchanged and there is no migration.

### What's new

- **The quick start asks where the `.msi` goes — seven answers, not six.** **Output folder** sits
  immediately after Release folder, and is stated rather than derived: 1.2.0 silently used the release
  folder's *parent*, which was a reasonable guess and still only a guess. It is required — Apply stays
  disabled while it is blank — and otherwise unconstrained: an output folder **inside** the release folder
  is accepted, with a permanent hint and no warning. Its **Browse…** button is the one that is ungated,
  because unlike the icon and executable pickers (which open at the release folder and need one first) the
  output folder depends on nothing. A folder that does not exist yet is reported **once**, by the Problems
  pane on Validate — the dialog applies string and parse rules only and never touches the disk.
  **Saved profiles are unaffected:** `outputPath` has always been a stored field, so whatever a 1.0.0,
  1.1.0 or 1.2.0 profile carries opens verbatim.
- **A quieter splash screen.** It shows the logo, **Enigma.Msi** and `Version X.Y.Z` — and nothing else.
  The tagline and the author line are off it (both are still in **About**, which is unchanged), and the
  card is 420×260 with a subtler border. **Its behaviour is untouched:** about two seconds, dismissed
  immediately by any click or key press, still the only window on screen until the main one takes over.
- **The Enigma.Msi library is no longer packaged, and cannot be.** It is on no feed, no version of it ever
  was, and this release removes the machinery that suggested otherwise: the packaging metadata, the
  worker-payload pack target and `build/Enigma.Msi.targets` are deleted, and the project declares
  `IsPackable=false` plus a guard that makes an accidental `dotnet pack` **fail loudly** — on its own,
  `IsPackable=false` would make it a silent no-op. **Consume the library in-repo by `ProjectReference`**:
  clone the repository and reference `src/Enigma.Msi/Enigma.Msi.csproj`. Nothing about the build changed —
  `build/CopyWorkerOutput.targets` still copies the `net472` worker into `$(OutDir)worker/` beside each
  host, and is now the only mechanism that does.

### Compatibility

- No change to the `.msipkg.json` profile format — still **`schemaVersion` 1**. Profiles are
  interchangeable between 1.0.0, 1.1.0, 1.2.0 and 1.3.0 in **both** directions.
- **The library's public surface is unchanged** — the `MsiPackage` model, `IMsiPackageValidator`,
  `MsiPackageJson` and `IMsiBuildService` are exactly what 1.0.0 shipped; only its packaging is gone. It
  still multi-targets `netstandard2.0`, `net8.0` and `net10.0`.
- The app still targets **.NET 10.0** (`WinExe`), is published framework-dependent for `win-x64`, and
  still requires **Windows and the WiX CLI** (`dotnet tool install --global wix`) to build an MSI.

## 1.2.0 — Enigma.Msi.Desktop

An **application-only release**: the app gets a visual identity and a fast path from a published folder to
a buildable package. **The Enigma.Msi library remains at 1.0.0 and is not re-released** — no NuGet package
is published for this version. The model, the validator, the build client, the `net472` worker and the
`.msipkg.json` format (`schemaVersion` 1) are all untouched, so profiles saved by 1.0.0 or 1.1.0 open in
1.2.0 unchanged and there is no migration.

### What's new

- **Quick start — six answers, and the form is filled in.** A **Quick start** button at the head of the
  command bar (and, on a freshly launched app with an empty form, one automatic opening) asks for the
  application name, version, manufacturer, release folder, an `.ico` and the main executable. From those
  six it derives the rest of a package that passes Validate with nothing outstanding: the install path
  `%ProgramFiles%\<application name>`, an output folder (the release folder's **parent**, so the `.msi`
  lands beside the payload rather than inside it), the MSI file name sanitized to the validator's rule, the
  product icon with the Control Panel section switched on, and **two shortcuts** — `%ProgramMenu%` then
  `%Desktop%` — each named after the application, carrying the icon and targeting
  `[INSTALLDIR]\<executable>`, where a nested executable keeps its sub-folder (`[INSTALLDIR]\bin\App.exe`).
  Apply stays disabled until all six are answered, the version parses and the executable is inside the
  release folder; applying **replaces** the package — fresh `productId` and `upgradeCode` included — and
  asks first when the form already holds work. Everything it produces stays editable: the quick start is
  assistance, never the only path.
- **Control Panel information is on by default.** A **new** package no longer starts with that section
  switched off — the section carries no validation rule of its own and every field in it is optional, while
  leaving it off is what produces an installed product with no Add/Remove Programs entry at all. An
  untouched new package saves as `"controlPanel": {}`, which is exactly what the switch says.
  **Opening a profile is unaffected:** the switch still follows what the profile carries, so a 1.0.0 or
  1.1.0 profile written without the section opens with it off and saving it again does not add one.
- **A splash screen.** The app opens on an undecorated card showing the logo, **Enigma.Msi**, the tagline,
  the running version and the author. It is the only window on screen while it is up — the main window
  appears when it goes — stays for about two seconds, and **any click or key press dismisses it
  immediately**. It behaves the same in Debug and Release, and there is no setting to turn it off.
- **An About dialog.** An icon-only button at the far right of the command bar opens a modal dialog with
  the logo, the name and tagline, the app version, the copyright, the author and the repository URL, plus
  a **View on GitHub** button that opens it in your browser. The URL is selectable text as well, so it can
  be copied by hand on a machine with no browser association. Close or Escape dismisses it.

### Compatibility

- No change to the `.msipkg.json` profile format — still **`schemaVersion` 1**. Profiles are
  interchangeable between 1.0.0, 1.1.0 and 1.2.0 in **both** directions.
- The app still targets **.NET 10.0** (`WinExe`), is published framework-dependent for `win-x64`, and
  still requires **Windows and the WiX CLI** (`dotnet tool install --global wix`) to build an MSI.
- No new package references; the pinned Avalonia 12.1.1 / `Enigma.Avalonia.Desktop` 1.0.0 /
  `Enigma.Icons.Avalonia` 1.0.0 UI stack is unchanged.
- **Upgrading in place works as intended**: the installer keeps the app's permanent `UpgradeCode`, so
  1.2.0 upgrades an installed 1.1.0 (or 1.0.0) rather than installing alongside it.

## 1.1.0 — Enigma.Msi.Desktop

An **application-only release**: five user-facing improvements to the desktop app. **The Enigma.Msi
library remains at 1.0.0 and is not re-released** — no NuGet package is published for this version. The
model, the validator, the build client, the `net472` worker and the `.msipkg.json` format
(`schemaVersion` 1) are all untouched, so profiles saved by 1.0.0 open in 1.1.0 unchanged and there is no
migration.

### What's new

- **Variable hints that stay put.** The three fields that accept WixSharp path tokens — *Install path*,
  and a shortcut's *Location* and *Target* — now carry permanent helper text underneath naming the tokens
  they accept: `%ProgramFiles%`, `%ProgramFiles64%`, `%LocalAppData%`, `%CommonAppData%` for the install
  path; `%Desktop%`, `%ProgramMenu%`, `%StartMenu%`, `%Startup%` (sub-folders allowed) for a shortcut's
  location; `[INSTALLDIR]` for its target. Previously the only guidance was the placeholder, which
  disappeared the moment you started typing. The hints cite a common subset — WixSharp resolves the
  tokens and the validator does not restrict them.
- **A build-progress overlay.** Building an MSI now raises a modal card — title, indeterminate progress
  bar, the latest streamed log line, and Cancel — so a long build is visibly in progress instead of
  looking like nothing happened. It appears once validation passes, comes down before the outcome is
  reported so the result is never shown under the dimming, and is guaranteed to be taken down on every
  exit path: success, build failure, missing prerequisites, cancellation, or an unexpected error. The
  toolbar's Cancel button is **gone** — it was unreachable behind the modal overlay, so the card's Cancel
  is now the single cancel affordance. Cancelling still terminates the worker together with its child
  processes, leaving no orphaned `wix` processes.
- **Shortcuts as self-contained cards.** Each shortcut is a bordered card headed by its own name (falling
  back to *Shortcut* while unnamed) with its own remove button, replacing the selected-row-plus-*Remove
  selected*-button arrangement. The list-selection concept is deleted outright, which removes the root
  cause of the selection/pressed background that used to flash across a row every time you clicked into
  one of its editors.
- **Icons on the section headers.** The five left-hand sections — *Product*, *Install and output*,
  *Control Panel information*, *Shortcuts*, *Managed UI* — carry icons, so the form is scannable at a
  glance rather than five identically-shaped headers.
- **A legible incomplete-package warning.** The *Package incomplete — press Validate for details.*
  message renders in the theme's warning colour at full opacity instead of dimmed default text, in both
  the Dark and Light variants.

### Compatibility

- No change to the `.msipkg.json` profile format — still **`schemaVersion` 1**. Profiles are
  interchangeable between 1.0.0 and 1.1.0 in both directions.
- The app still targets **.NET 10.0** (`WinExe`), is published framework-dependent for `win-x64`, and
  still requires **Windows and the WiX CLI** (`dotnet tool install --global wix`) to build an MSI.
- No new package references; the pinned Avalonia 12.1.1 / `Enigma.Avalonia.Desktop` 1.0.0 /
  `Enigma.Icons.Avalonia` 1.0.0 UI stack is unchanged.
- **Upgrading in place works as intended**: the installer keeps the app's permanent `UpgradeCode`, so
  1.1.0 upgrades an installed 1.0.0 rather than installing alongside it.

## 1.0.0 — Enigma.Msi and Enigma.Msi.Desktop

The first public release of **Enigma.Msi** — a library that builds Windows MSI installers from a single
declarative model. One `MsiPackage` is the input everywhere: the validator reports on it, the build
client builds it, a saved `.msipkg.json` profile is its serialization, and the companion desktop app
binds to it. The MSI authoring itself is done by WixSharp (WiX v4), which is .NET Framework-only; it is
confined to a `net472` worker process shipped inside the package, so it never reaches the public API and
modern .NET consumers take no .NET Framework dependency.

This release covers both artifacts: the **Enigma.Msi** library (NuGet) and the **Enigma.Msi.Desktop**
application (MSI installer), both at **1.0.0**.

### Feature overview

- **The package model** — `MsiPackage` describes product identity (name, version, `ProductId` /
  `UpgradeCode`, manufacturer), install `Scope` (per-machine or per-user), `InstallSettings` (the source
  folder packaged and the install target), `OutputSettings` (where the `.msi` is written and under which
  name), cabinet `Compression`, optional `ControlPanelInfo` (`ProductIcon`, `Comments`, `Contact`,
  `HelpLink`, `UrlInfoAbout`), a list of `Shortcut` entries, and optional `UiSettings` carrying the
  managed-UI flavour (`Wui`) and the install/modify dialog sequences.
  Members are permissive data with defaults rather than `required`, so a half-filled package can be
  deserialized, edited and reported on; required-ness is a validation concern.
- **Validation** — `IMsiPackageValidator` returns every violation at once as `MsiValidationError`
  entries (`{ Path, Message }`), never a first-error bool. In-memory rules (shape, identity, GUIDs,
  version, tokens, dialog sequences) are separated from environment rules that touch the disk, so an
  editor can validate as-you-type and hit the filesystem only when asked.
- **Profiles** — `MsiPackageJson` serializes and deserializes `<name>.msipkg.json`. The file *is* the
  model, carrying a `schemaVersion`; a version above the one the library understands is reported as a
  validation error rather than silently misread. The legacy `<App>.msiprofile.<version>.json` format of
  the predecessor project is a deliberate clean break — there is no importer.
- **Building** — `IMsiBuildService.BuildAsync` validates the package, then drives the WiX toolchain out
  of process and returns an `MsiBuildResult` (`Success`, `MsiPath`, `Errors`). Failures arrive as data,
  not exceptions. `CheckPrerequisitesAsync` reports a missing worker or a missing WiX CLI ahead of time
  as `MsiPrerequisites`. The build log streams line by line through `IProgress<string>`, and
  cancellation terminates the worker together with its child processes, so a cancelled build leaves no
  orphaned `wix` processes.
- **The worker CLI** — the bundled `net472` worker also runs headless:
  `Enigma.Msi.Worker.exe build <file.msipkg.json>`. It needs no library reference, which makes a
  committed profile buildable straight from a CI job. Exit codes are a contract: `0` success, `1` the
  operation failed on well-formed input (validation or MSI build), `2` bad arguments, unparseable input,
  or an unexpected error.
- **Packaging plumbing** — the package ships the worker and its whole dependency closure under
  `tools/worker/`, plus `build/Enigma.Msi.targets`, which NuGet imports into every consuming project and
  which copies the payload to `$(OutDir)worker/` where the build client discovers it. Two MSBuild
  properties adjust it: `IncludeEnigmaMsiWorker` (set `false` for a project that reads profiles but
  never builds an MSI) and `EnigmaMsiWorkerFolderName`.
- **The desktop app** — **Enigma.Msi.Desktop 1.0.0**, an Avalonia application over the same
  `MsiPackage`: fill in the form, validate, build, watch the live log, and save or load a
  `.msipkg.json` profile. It hosts the same worker the library ships.
- **WixSharp is isolated, and that isolation is tested** — the public `InstallScope`,
  `CompressionLevel`, `Wui` and `Dialog` enums are WixSharp-free mirrors mapped inside the worker, and
  reflection-based drift tests assert them against WixSharp's real member sets so an upstream rename or
  addition fails the build instead of silently mis-mapping.

### Dependencies

- **Library** — `System.Text.Json` **10.0.10**, `System.Buffers` **4.6.1** and `PolySharp` **1.16.0**
  (compile-only) on `netstandard2.0` only; on `net8.0`/`net10.0` those are framework-provided, so the
  library has **no** runtime package dependencies there.
- **Worker (bundled, `net472`)** — `WixSharp_wix4` **2.14.1**, which brings `WixSharp_wix4.bin` and the
  WixToolset assemblies it needs.
- **Desktop app** — Avalonia **12.1.1** (`Avalonia`, `.Desktop`, `.Themes.Fluent`, `.Fonts.Inter`),
  `Enigma.Avalonia.Desktop` **1.0.0**, `Enigma.Icons.Avalonia` **1.0.0**, `CommunityToolkit.Mvvm`
  **8.4.2**, `Microsoft.Extensions.Hosting` **10.0.10**, NLog **6.1.4**
  (+ `NLog.Extensions.Logging`), and `AvaloniaUI.DiagnosticsSupport` **2.2.3** in Debug builds only. The
  four Avalonia-coupled packages move as a set.

### Compatibility

- The library targets **.NET Standard 2.0**, **.NET 8.0**, and **.NET 10.0**. `netstandard2.0` is what
  lets the `net472` worker consume the very same model assembly as modern consumers.
- The desktop app targets **.NET 10.0** (`WinExe`).
- **Building an MSI requires Windows and the WiX CLI** (`dotnet tool install --global wix`). The library
  itself loads on any supported runtime; the build does not.
- The `.msipkg.json` profile format is at **`schemaVersion` 1**.

### Version

- Initial release: **1.0.0** (Enigma.Msi library and Enigma.Msi.Desktop application).
