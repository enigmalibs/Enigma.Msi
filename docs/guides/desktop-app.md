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

## The window

A command bar across the top; below it the package form on the left and two panes on the right —
**Problems**, carrying a count and the violations from the last validate or build attempt, and **Build
log** beneath it, with its own clear button. A splitter between the two columns resizes them. A status
line runs along the bottom.

| Command | What it does |
|---|---|
| **New** | Starts an empty package, with freshly generated `productId` and `upgradeCode` and the version at `1.0.0`. Discards what is in the form. |
| **Open** | Loads a `.msipkg.json` profile into the form. |
| **Save** | Saves to the open profile, asking for a path the first time. |
| **Save as…** | Saves under a new name. Suggests a file name from the MSI file name, or the app name. |
| **Validate** | Reports every problem with the package, without touching the worker. |
| **Build** | Validates, checks prerequisites, then builds the MSI. Disabled while the package is incomplete or a build is running. |
| **Cancel** | Cancels the running build, terminating the worker and its children. |

The title bar shows the open profile (`Enigma.Msi — Widget.msipkg.json`) or `Enigma.Msi — new package`. A
status line reports the last thing that happened; results also arrive as an info bar.

**Build stays disabled until the package is complete.** The gate is the validator's in-memory rules plus
the form's own parse errors — deliberately not the environment rules, which touch the disk and would make
typing a path hit the file system on every keystroke. Those run when the build actually starts, which is
why Build can be enabled and still report problems. The hint next to the disabled button says as much:
*Package incomplete — press Validate for details.*

## The form

Each section is a collapsible card.

| Section | Fields |
|---|---|
| **Product** | App name, version, manufacturer, product ID, upgrade code, install scope, compression. |
| **Install and output** | Install path, release path (with a folder browser), output path (with a folder browser), MSI file name. |
| **Control Panel information** | A switch that includes the whole optional block, then product icon (with a file browser), comments, contact, help link, about URL. |
| **Shortcuts** | A list of shortcuts, each with location, name, target, icon and arguments. Add and remove; new shortcuts default to `%ProgramMenu%` and take the app name. |
| **Managed UI** | A switch that pins the package's own UI, then the WixUI dialog set and the two dialog sequences. |

Two details in the form are worth pointing out, because they encode rules from the model.

**The two GUIDs have regenerate buttons, and they mean different things.** `productId` identifies this
exact version and can be regenerated freely — do it for every release. `upgradeCode` identifies the
product *line* and must stay constant forever, or Windows Installer stops recognizing a new build as an
upgrade of the old one. Regenerating it on an existing product is almost always a mistake.

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
