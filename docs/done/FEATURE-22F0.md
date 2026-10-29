# FEATURE-22F0 — Linux install and uninstall scripts

## Summary

`packaging/linux/` now carries Enigma.GitClient's per-user Linux installer, adapted to this app. There is no
root, and nothing is written outside `$HOME`.

- **`install.sh`** builds the app from the repository (`dotnet publish`, self-contained by default) or takes
  an already-published folder (`--from DIR`). Other options are `--framework-dependent` and `--rid`. It
  installs into the XDG directories:
  - `~/.local/share/enigma-msi`;
  - an `enigma-msi` symlink in `~/.local/bin`;
  - `~/.local/share/applications/enigma-msi.desktop`, with the launcher path quoted and `DOTNET_ROOT`
    pinned when a framework-dependent build's runtime is not where a launcher looks;
  - the icon at 8 hicolor sizes.

  It stages beside the install directory and swaps it in by rename, so a failed build leaves the previous
  install untouched. It refuses root and validates the XDG paths. Both its help and its closing note say
  that building the MSI needs Windows.
- **`uninstall.sh`** removes exactly that. The symlink goes only if it points at this installation, and the
  install directory only if its path ends in `enigma-msi`.
- **`enigma-msi.desktop.in`**: `Name=Enigma.Msi`, `Comment=Declarative Windows MSI builder`,
  `Categories=Development;`, `StartupWMClass=Enigma.Msi.Desktop`.
- **`icons/enigma-msi-{16,24,32,48,64,96,128,256}.png`** are the `.ico`'s own PNG frames, byte for byte.
  They were extracted by a throwaway script in the scratchpad, which is not committed.

## Files/modules touched

- Created:
  - `packaging/linux/install.sh` (100755)
  - `packaging/linux/uninstall.sh` (100755)
  - `packaging/linux/enigma-msi.desktop.in`
  - `packaging/linux/icons/enigma-msi-*.png` (8)
  - `docs/done/FEATURE-22F0.md`
- Modified:
  - `README.md` (*On Linux*)
  - `docs/roadmap.md`, `docs/plan/FEATURE-22F0.md`

## Deviations & follow-ups

- **Owed on Linux:**
  - running the published app itself;
  - the default build-from-source path (no `--from`). It is the same `dotnet publish` that was run by hand
    here, but not through the script.
  - the cache refreshes.

  Everything else ran on this Windows machine through Git Bash.
- **The symlink on Windows.** Git Bash cannot create native symlinks without Developer Mode ("Operation not
  permitted"). The round trip therefore ran with `MSYS=winsymlinks:lnk`, whose links `[[ -L ]]` and
  `readlink` treat as symlinks. On Linux `ln -sfn` is a real symlink.
- **Linux payloads carry the `net472` `worker/` folder too.** Plan decision: one payload shape from one
  project. The app reports a build it cannot run like any other prerequisite problem.
- Size figures in GitClient's help ("37 MB rather than 116 MB") were not carried over. They were not
  measured for this app, so the help says "a much smaller install".

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| `GenericName` | `MSI Installer Builder` | What a launcher search for "installer" should find |
| Closing note | "building the MSI needs Windows" instead of GitClient's git check | The one thing a Linux user must know before relying on the app |
| Settings line in `uninstall.sh` | Replaced by "profiles are left where you saved them" | This app keeps no settings folder; profiles are the user's data |

## Documentation sweep

- `README.md`: a new *On Linux* subsection under *Building it from source*.

## Build/test evidence

- `bash -n` passes for both scripts. `--help` prints for both, and an unknown option exits 1. Both are LF
  (no `\r`), and both are `100755` in the index.
- An empty `uninstall.sh` printed "Nothing to remove" and exited 0.
- The round trip ran through Git Bash with `XDG_DATA_HOME` and `XDG_BIN_HOME` in the scratchpad, against
  `dotnet publish src/Enigma.Msi.Desktop -c Release -r linux-x64 --self-contained true`:
  1. `install.sh --from` exited 0 and produced the app directory, the symlink to `…/Enigma.Msi.Desktop`, the
     8 icons (all byte-identical to `packaging/linux/icons`) and the entry with `Exec` substituted.
  2. A second install printed "Replacing …".
  3. `uninstall.sh` removed all four kinds of item, and **0** `enigma-msi` files were left.
  4. A foreign `enigma-msi` symlink was "left alone".
- `dotnet build Enigma.Msi.slnx -c Release`: **0 warnings**. `dotnet test --solution …`: **417/417 passed**,
  exit 0. Nothing in .NET changed.
