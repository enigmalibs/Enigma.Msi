# The desktop app

`Enigma.Msi.Desktop` is an Avalonia application over the same `MsiPackage` the library builds from. It is a
single form: you fill in the package, press **Validate** to see everything that is wrong with it, press
**Build** to produce the MSI, and watch the WiX toolchain's output stream into a log pane as it happens.
What it saves is a plain `.msipkg.json` profile — the same file the [worker CLI](worker-cli.md) builds, so
anything described in the app can be rebuilt in CI without the app.

There is no separate view model of the installer inside the app: the form binds to the model, `Validate`
calls the same `IMsiPackageValidator` a library consumer would, and `Build` goes through the same
`IMsiBuildService`. The app is a UI over the library, not a reimplementation of it.

## Requirements

| Requirement | Notes |
|---|---|
| Windows | Building an MSI is Windows-only. |
| The WiX CLI | `dotnet tool install --global wix`. The app reports its absence before a build rather than failing during one. |
| The worker | Ships in `worker\` beside the app's executable; the build puts it there. |

## Running it

```bash
dotnet run --project src/Enigma.Msi.Desktop
```

Or, from a release build, `Enigma.Msi.Desktop.exe` in the output folder — with its `worker\` subfolder
next to it. The app finds the worker there by itself; no configuration.

## The splash screen

The app opens with a splash: the logo, **Enigma.Msi** and `Version <x.y.z>` — and nothing else. The
tagline, the copyright and the author line are in the About dialog, which is where there is time to read
them. It is the only window on screen while it is up — the main window appears when it goes.

It stays for about two seconds, and **any click or key press dismisses it immediately**. There is no
setting to turn it off, and it behaves the same in Debug and Release.

## The window

A command bar across the top; below it the package form on the left and two panes on the right —
**Problems**, carrying a count and the violations from the last validate or build attempt, and **Build
log** beneath it, with its own clear button. A splitter between the two columns resizes them. A status
line runs along the bottom.

| Command | What it does |
|---|---|
| **Quick start** | Asks seven questions and fills the whole form from the answers. See [Quick start](#quick-start). |
| **New** | Starts an empty package, with freshly generated `productId` and `upgradeCode` and the version at `1.0.0`. Discards what is in the form. |
| **Open** | Loads a `.msipkg.json` profile into the form. |
| **Save** | Saves to the open profile, asking for a path the first time. |
| **Save as…** | Saves under a new name. Suggests a file name from the MSI file name, or the app name. |
| **Validate** | Reports every problem with the package, without touching the worker. |
| **Build** | Validates, checks prerequisites, then builds the MSI. Disabled while the package is incomplete or a build is running. |

There is no Cancel button in the command bar: a running build covers the window with a modal progress card,
and that card carries the one cancel affordance.

At the far right of the same bar sits an **icon-only About button**. It opens a modal dialog reporting the
logo, the name and tagline, the app version, the copyright, the author and the repository URL — with a
**View on GitHub** button that opens the repository in your browser. The URL is selectable text as well,
so it can be copied by hand on a machine with no browser association. Close or Escape dismisses it.

The title bar shows the open profile (`Enigma.Msi — Widget.msipkg.json`) or `Enigma.Msi — new package`. A
status line reports the last thing that happened; results also arrive as an info bar.

**Build stays disabled until the package is complete.** The gate is the validator's in-memory rules plus
the form's own parse errors — deliberately not the environment rules, which touch the disk and would make
typing a path hit the file system on every keystroke. Those run when the build actually starts, which is
why Build can be enabled and still report problems. The hint next to the disabled button says as much:
*Package incomplete — press Validate for details.*

## Quick start

Seven answers, and the form comes back filled in. It is the fastest way from a published folder to a
buildable package, and nothing it produces is beyond editing afterwards — the quick start is assistance,
never the only path.

| Field | What it is |
|---|---|
| **Application name** | The product's name. Also the MSI file name, both shortcut names, and the last segment of the install path. |
| **Version** | The product version. Must parse as a version number. |
| **Manufacturer** | The publisher. |
| **Release folder** | The folder whose contents are packaged. Browse for it first: the icon and executable questions open their pickers here. |
| **Output folder** | Where the built `.msi` is written. Typically the release folder's parent, so the installer lands beside the payload rather than inside it — but any folder is accepted, the release folder itself included. Its Browse button works from the start; it depends on nothing. |
| **Icon** | An `.ico` file. Used as the product icon *and* on both shortcuts. |
| **Main executable** | The `.exe` both shortcuts point at. It must be **inside the release folder** — that folder is what `[INSTALLDIR]` becomes once the MSI is installed — and the dialog says so if it is not. |

From those, it derives:

| Field | Derived as |
|---|---|
| Install path | `%ProgramFiles%\<application name>` |
| MSI file name | The application name, with any character a file name cannot hold removed and a trailing `.msi` stripped. If nothing survives that, `package`. |
| Product icon | The chosen icon, with the Control Panel section switched on. |
| Shortcuts | Two, in this order: `%ProgramMenu%` then `%Desktop%`. Each named after the application, targeting `[INSTALLDIR]\<executable>` — a nested executable keeps its sub-folder, e.g. `[INSTALLDIR]\bin\Widget.exe` — and carrying the icon. |

**Apply stays disabled until all seven are answered**, the version parses, and the executable is inside the
release folder. Those are string and parse rules only — no path is checked for existing here, which is the
main form's Problems pane's job, in one place. An output folder that does not exist is reported there, once.

**Applying replaces the package**, it does not merge into it: `productId` and `upgradeCode` are
regenerated and everything else goes back to a new package's defaults before the seven answers are applied.
If the form already holds something, you are asked to confirm first — and cancelling the quick-start
dialog itself asks nothing at all.

The dialog **opens by itself, once**, when the app starts on an empty form. Escape or Cancel dismisses it,
and it does not come back in that session. There is no setting to turn it off.

## The form

Each section is a collapsible card.

| Section | Fields |
|---|---|
| **Product** | App name, version, manufacturer, product ID, upgrade code, install scope, compression. |
| **Install and output** | Install path, release path (with a folder browser), output path (with a folder browser), MSI file name. |
| **Control Panel information** | A switch that includes the whole optional block, then product icon (with a file browser), comments, contact, help link, about URL. **On by default for a new package** — see below. |
| **Shortcuts** | One card per shortcut — its name as the header, its own remove button, then location, name, target, icon and arguments. **Add** appends; new shortcuts default to `%ProgramMenu%` and take the app name. |
| **Managed UI** | A switch that pins the package's own UI, then the WixUI dialog set and the two dialog sequences. |

**The three fields that take variables say so, permanently.** A hint under the *Install path* box and under a
shortcut's *Location* and *Target* lists what may be typed there — the placeholder alone would not do, since it
disappears the moment typing starts, which is exactly when the tokens are wanted:

| Field | Hint |
|---|---|
| Install path | `%ProgramFiles%`, `%ProgramFiles64%`, `%LocalAppData%`, `%CommonAppData%` |
| Shortcut location | `%Desktop%`, `%ProgramMenu%`, `%StartMenu%`, `%Startup%` — sub-folders allowed, e.g. `%ProgramMenu%\Contoso` |
| Shortcut target | `[INSTALLDIR]` is the install folder, e.g. `[INSTALLDIR]\Widget.exe` |

The hints are a curated common subset, not the whole list: the validator does not restrict tokens at all —
WixSharp resolves them when the MSI is built.

**Each shortcut is a card, and each card removes itself.** There is no list selection to make first, which is
also why clicking into a field inside a card no longer highlights the whole row. A card with no name yet is
headed *Shortcut*.

Two further details in the form are worth pointing out, because they encode rules from the model.

**The two GUIDs have regenerate buttons, and they mean different things.** `productId` identifies this
exact version and can be regenerated freely — do it for every release. `upgradeCode` identifies the
product *line* and must stay constant forever, or Windows Installer stops recognizing a new build as an
upgrade of the old one. Regenerating it on an existing product is almost always a mistake.

**Control Panel information starts switched on, for a new package only.** The section costs nothing to
carry — it has no validation rule of its own and every field in it is optional — while switching it off is
what produces an installed product with no Control Panel entry at all. A new package with nothing typed
there saves as `"controlPanel": {}`, which is exactly what the switch says. **Opening a profile is
unaffected:** the switch follows what the profile carries, so a 1.0.0 or 1.1.0 package written without the
section still shows it off, and saving it again does not add one.

**The managed-UI switch is a three-state affair in disguise.** Off means the package carries no UI
settings, and the build applies its default sequences — `Welcome → InstallDir → Progress → Exit` for a
fresh install, `Welcome → MaintenanceType → Progress → Exit` for modify/repair/remove. The editor is
pre-filled with exactly those, switched off, so turning it on starts from something valid rather than from
two empty lists. Once on, the sequences are yours to reorder (each has add / remove / move up / move down)
and must not be empty.

## What a build does

1. **Validate.** Both rule sets, plus the form's own parse errors. Anything found is listed in the
   **Problems** pane with the member path that produced it, and the build stops there.
2. **Pre-flight.** The worker and the WiX CLI are checked. Problems go to the build log and an info bar —
   this is where a missing `wix` tool is reported, with the `dotnet tool install --global wix` hint.
3. **Build.** The worker runs out of process. Its output — WixSharp's and the WiX toolchain's — streams
   into the log pane line by line as it arrives, so a long build is visibly alive.
4. **Report.** On success the MSI's path goes to the status line, the log and a success info bar. On
   failure, *every* reason is appended to the log.

**A build runs under a modal card.** From the moment Build is pressed — before the pre-flight check, so even
the fast failures are visible — the window dims behind a card titled *Building MSI…* carrying an
indeterminate bar, the latest line the build wrote, and **Cancel**. The card comes down on every exit path,
and always *before* the outcome is reported, so no result is ever read through the dimming.

**Cancel** terminates the worker together with everything it started, so a cancelled build leaves no
orphaned `wix` processes behind. The log pane has its own clear command; the build log is append-only by
construction — a line costs one append, never a re-concatenation of everything logged so far.

## Profiles

Saved packages are `<name>.msipkg.json`, exactly the format described in
[the package model](model.md) — the app has no format of its own.

- **Absolute paths.** The app writes the paths as they are in the form, and its browsers produce absolute
  ones. That is what makes a saved profile buildable from any working directory by the worker CLI.
- **`schemaVersion` survives a round trip.** It is preserved rather than rewritten on save, so a profile
  from a future schema version is *reported* by the validator instead of being silently downgraded.
- **No importer for the old format.** The predecessor project's `<App>.msiprofile.<version>.json` files
  are a deliberate clean break; re-enter the package once and save it as a `.msipkg.json`.

## Typical workflow

The short version is **Quick start**, then Validate, Save as… and Build — steps 1 to 4 below are what it
does for you. The long version, if you would rather type it or the quick start's seven answers do not fit:

1. **New**, then fill in Product — the app name, manufacturer and version. Keep the generated
   `upgradeCode`; it is this product's identity from now on.
2. Point **release path** at your app's release output folder (its whole contents get packaged,
   recursively) and **output path** at where the `.msi` should be written. Both must exist.
3. Set the **MSI file name** without the `.msi` extension — the build appends it.
4. Add a shortcut or two, targeting `[INSTALLDIR]\YourApp.exe`.
5. **Validate**, and fix what it lists.
6. **Save as…** next to your solution, so the profile is in source control.
7. **Build**, and watch the log.
8. For the next release: open the profile, bump the version, regenerate **product ID** only, rebuild.

## Notes

- **The app is a client, not the builder.** It cannot build an MSI without the worker beside it and the
  WiX CLI installed; the pre-flight check is what tells you which one is missing.
- **Validation errors carry profile paths, not field labels.** `install.releasePath` is the JSON member,
  which is the *Release path* box in the Install and output section. The paths are stable, which is what
  lets the same message make sense in the app, in the CLI and in a library consumer's log.
- **A blank field and an unparseable one are different problems.** Blank is the validator's "required"
  rule; text that no model can hold — a malformed version, a non-GUID identifier — is reported by the form
  itself, and appears in the same list.
- **`Ui` off is not "no UI".** For an installer with no interaction, switch Managed UI *on* and choose
  `WixUI_ProgressOnly`.
