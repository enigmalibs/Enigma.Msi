# FEATURE-C19D — Publish scripts for Windows and Linux

## Summary

One-command publish scripts, each running the requested command against `src/Enigma.Msi.Desktop` from
wherever it is started:

| Script | Command |
|---|---|
| `packaging/windows/publish.ps1` | `dotnet publish … -c Release -r win-x64 -p:PublishReadyToRun=true` |
| `packaging/windows/publish.bat` | the same, for a shell with no PowerShell execution policy to satisfy |
| `packaging/linux/publish.sh` | `dotnet publish … -c Release -r linux-x64 -p:PublishReadyToRun=true` |

Each script resolves the project from its own location (`$PSScriptRoot`, `%~dp0`, `BASH_SOURCE`), takes no
arguments, and exits with `dotnet`'s exit code: `exit $LASTEXITCODE`, `exit /b %ERRORLEVEL%`, `exec`. Their
header comments name the command and the output folder. **`docs/RELEASE.md` §2 now publishes with
`publish.ps1`**, so the MSI ships what the script produces (ReadyToRun).

## Files/modules touched

- Created:
  - `packaging/windows/publish.ps1`
  - `packaging/windows/publish.bat` (CRLF)
  - `packaging/linux/publish.sh` (100755)
  - `docs/done/FEATURE-C19D.md`
- Modified:
  - `docs/RELEASE.md` (§2)
  - `README.md` (*Building it from source*, *On Linux*)
  - `docs/roadmap.md`, `docs/plan/FEATURE-C19D.md`

## Deviations & follow-ups

- **The runbook's framework-dependent bullet was reworded.** It no longer names a `--self-contained false`
  flag. On the .NET 10 SDK, a RID-specific publish is framework-dependent by default, which the win-x64
  output confirms: no `coreclr.dll`.
- **The MSI payload now carries ReadyToRun images.** `Enigma.Msi.Desktop.dll` is 606 KB instead of 287 KB,
  and the whole win-x64 publish folder is 70 MB. FEATURE-AB6B's dogfood build measures the installer.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How `publish.bat` was written | Written as text, then converted to CRLF bytes | `.gitattributes` keeps `*.bat` CRLF in the tree |
| How R2R was verified | Assembly sizes, IL build against publish | Windows PowerShell 5.1 has no `PEReader`; the doubling of every app assembly is unambiguous |

## Documentation sweep

- `docs/RELEASE.md` §2: the publish step is `.\packaging\windows\publish.ps1`, and the framework-dependent
  and ReadyToRun bullet was rewritten.
- `README.md`: the build-from-source block uses `publish.ps1`, and *On Linux* names `publish.sh`.

## Build/test evidence

- **Each script was run from the scratchpad, not the repo root:**
  - `cmd /c …\publish.bat`, exit 0;
  - `powershell -ExecutionPolicy Bypass -File …\publish.ps1`, exit 0;
  - `bash …/publish.sh`, exit 0 (a linux-x64 ReadyToRun publish cross-compiled from Windows).

  Each left its `publish/` folder with `Enigma.Msi.Desktop(.exe)`, `app.ico` and
  `worker/Enigma.Msi.Worker.exe`, and no bundled runtime.
- **ReadyToRun:**
  - `Enigma.Msi.Desktop.dll` is 287,744 bytes (IL) against 606,208 (win-x64) and 603,136 (linux-x64);
  - `Enigma.Msi.dll` is 36,352 against 90,112.
- **Failure propagates.** With `dotnet` off `PATH`:
  - `publish.bat` exits 9009;
  - `publish.ps1` exits 1;
  - `publish.sh` exits 127.
- **Line endings and modes:** `git ls-files --eol` shows `publish.bat` as `i/lf w/crlf attr/text eol=crlf`,
  and `publish.sh` is `100755`.
- `dotnet build Enigma.Msi.slnx -c Release`: **0 warnings**. `dotnet test --solution …`: **417/417 passed**,
  exit 0.
