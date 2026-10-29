# FEATURE-22F0 — Linux install and uninstall scripts

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-22f0-linux-install-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

`packaging/linux/install.sh` and `uninstall.sh`, as Enigma.GitClient ships them: a per-user, no-root
install of the desktop app into the XDG directories, with a launcher entry and icons, and its exact
removal. Part of the uniform baseline.

## Context & constraints

- **The reference** is Enigma.GitClient's `packaging/linux/` (@ `4bfab63`): `install.sh` (options
  `--from DIR`, `--framework-dependent`, `--rid RID`, `-h`; refuses root; validates XDG paths; publishes
  into a sibling staging dir and swaps it in by rename; symlink in `$XDG_BIN_HOME`; hicolor icons at
  16/24/32/48/64/96/128/256; a `.desktop` file from `<app-id>.desktop.in` with `@EXEC@` substituted in
  bash; `DOTNET_ROOT` pinned when a framework-dependent build's runtime is not where a launcher looks;
  cache refreshes as conveniences), `uninstall.sh` (removes exactly that; the symlink only if it points
  into the install dir; refuses an install dir not ending in the app id), `icons/<app-id>-<N>.png`, mode
  `100755` for both scripts, `.gitattributes` keeping `*.sh` LF.
- This repo has no `packaging/` folder yet. `.gitattributes` already has `*.sh text eol=lf`.
- **The app on Linux:** an Avalonia `net10.0` app, so it runs there (CLAUDE.md records Linux phases with
  the desktop suite green). It edits, validates, opens and saves `.msipkg.json` profiles; **building an
  MSI needs Windows** — the worker is `net472` and drives the WiX toolchain. The publish carries the
  `worker/` folder anyway (it is part of the app's payload); the scripts say plainly what works where.
- App id `enigma-msi`; launcher `Enigma.Msi.Desktop`; display name `Enigma.Msi` (`AppInfo.Name`); the
  tagline "Declarative Windows MSI builder" is the entry's `Comment`.
- `Assets/appicon.ico` holds PNG frames at every size the reference installs (16, 24, 32, 48, 64, 96, 128,
  256): the icons are extracted byte for byte, no re-rendering.
- Settings: this app keeps none on disk (no config folder to mention).

## Steps

1. `packaging/linux/icons/enigma-msi-<N>.png` × 8, extracted from `appicon.ico` (a throwaway script in
   the scratchpad, not committed).
2. `packaging/linux/enigma-msi.desktop.in` — the reference's keys; `Categories=Development;`;
   `Keywords=msi;installer;wix;windows;setup;`; `StartupWMClass=Enigma.Msi.Desktop`.
3. `packaging/linux/install.sh` — the reference adapted: names, help text (MSI builds need Windows), the
   closing notes (no git check; a note that building an MSI needs Windows).
4. `packaging/linux/uninstall.sh` — the reference adapted (no former-launcher name, no config folder).
5. Both staged with mode `100755` (`git update-index --chmod=+x`).
6. Verify on this machine through Git Bash: `bash -n` on both, `--help` on both, `uninstall.sh` on an
   empty `XDG_DATA_HOME` in the scratchpad ("nothing to remove"); `install.sh --from` against a
   `linux-x64` publish into a scratchpad `XDG_DATA_HOME`/`XDG_BIN_HOME`, then `uninstall.sh` removing it
   all (symlink creation on Windows may need `MSYS=winsymlinks:nativestrict`; whatever cannot run here is
   recorded as owed on Linux).
7. README: a *Linux* subsection under the app's build-from-source section.

## Acceptance criteria

- Both scripts parse (`bash -n`), print their help, and are `100755` in the index; LF in the tree.
- Icons byte-identical to the `.ico`'s frames of the same size.
- The install → uninstall round trip leaves the scratchpad XDG tree empty of `enigma-msi` files (or the
  part not runnable on Windows is recorded as owed).
- Release build warning-free; the whole suite green (nothing in it changes).

## Out of scope

- `publish.sh` (FEATURE-C19D); a distro package (deb/rpm/AppImage/Flatpak); making MSI builds work on Linux.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Ship Linux scripts for a Windows-MSI tool | Yes, with the limitation stated in the help and the closing note | The spec asks for them; the app runs and edits profiles on Linux | Skipping as "not applicable" |
| Default build | Self-contained, as the reference | Works on a machine with no .NET runtime | Framework-dependent default |
| Drop the worker from the Linux payload | No | One payload shape per platform from the same project; a Linux-only exclusion is new build logic | A RID-conditioned exclusion in `CopyWorkerOutput.targets` |
| Icons | Extracted from the `.ico`'s PNG frames | Byte-identical to the shipped icon, no tool dependency | Re-rendering from the SVG master (outside the repo) |
| Tests | Script checks + a scratchpad round trip, no new unit test | Shell scripts; the round trip is the real proof | A test project for shell |
