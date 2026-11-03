# FEATURE-3E44-PHASE01 — Vendor the Enigma.Msi worker CLI

## Summary

`packaging/windows/Enigma.Msi.Worker/` now holds a copy of the worker CLI as released with the app's
1.7.0: the `net472` build output of `src/Enigma.Msi.Worker` at tag `1.7.0` (`a7a1bfb`), without
`*.pdb`, `Enigma.Msi.xml` and `.wixsharp/`. That is 20 files, and a README is added beside them.
`buildmsi.ps1` (PHASE02) runs it, as every other Enigma app runs its own copy. The README says what the
copy is and why a repository that builds the worker also carries one: a released worker builds the
release. It also gives the copy's source and version stamp, its requirements, the licences, what
`.wixsharp/` is, and how to refresh it.

`.gitignore` ignores `packaging/windows/Enigma.Msi.Worker/.wixsharp/`, which WixSharp writes beside the
worker on first use. The comment is the same as GitClient's.

## Files/modules touched

- Created:
  - `packaging/windows/Enigma.Msi.Worker/` (20 binaries/config + `README.md`)
  - `docs/done/FEATURE-3E44-PHASE01.md`
- Modified: `.gitignore`, `docs/roadmap.md`, `docs/plan/FEATURE-3E44.md`

## Deviations & follow-ups

- **Where the files came from.** They are copied from a snapshot of
  `src/Enigma.Msi.Worker/bin/Release/net472/` taken while planning, before the run's baseline rebuilt
  that folder at the run's commit (which would have stamped another sha). The snapshot was the `1.7.0`
  build: its `Enigma.Msi.dll` and `Enigma.Msi.Worker.exe` read
  `1.0.0+a7a1bfb0cfae951132b2738ebddfb076683f2113`, and `git rev-parse 1.7.0^{commit}` is that sha.
- `.gitignore` had no newline at its end. Appending the rule added one; no other line changed.
- The working-tree `.exe.config` keeps the build's CRLF. Git stores it LF (`* text=auto eol=lf`), which
  .NET Framework reads the same. Recommendation only: none needed.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Source of the bits | The 1.7.0 build output snapshot | The plan's source; also proven identical to GitClient's committed copy and to the six other Enigma apps' copies |
| README's "every Enigma app" claim | Checked: Backup, FileExplorer, GitClient, HardCopy, Launchpad, MarkdownEditor and TextEditor carry byte-identical binaries | So the README states a fact, not an intention |

## Build/test evidence

Run baseline: see `docs/done/FEATURE-62E1.md` (0 warnings, 417/417).

- `cmp` of each of the 20 vendored files against the 1.7.0 build snapshot: all identical.
- Against `Enigma.GitClient/packaging/windows/Enigma.Msi.Worker/`: 19 identical. The `.exe.config`
  equals it once CRs are stripped (`diff <(tr -d '\r' …)`).
- The same 19 binaries are identical to the other six Enigma apps' copies (0 differing per repository).
- `WixSharp.dll` is 2.14.2.0, and `WixToolset.Dtf.WindowsInstaller.dll` is 4.0.1, as the licence table
  says.
- `git status --porcelain --ignored packaging/` lists the folder as untracked (`??`), and
  `git check-ignore` matches none of its 21 files. A probe file under `.wixsharp/` is ignored by
  `.gitignore:375`.
- `Enigma.Msi.Worker.exe` with no arguments printed "No arguments were given." and the usage, then
  exited **2**.
- `dotnet build Enigma.Msi.slnx -c Release --no-incremental` gave **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.Msi.slnx -c Release --no-build` gave **417 / 417 passed**.
