# FEATURE-1E7D — Desktop app 1.5.0 release

**Status:** DONE
**Type:** FEATURE (single-phase)
**Branch:** `feature/feature-1e7d-release-1-5-0` — cut from the run-branch tip.
**Run:** vibe/2026-10-22-release-1-5-0
**Depends on:** BUG-7325 and FEATURE-1399 DONE (the content of the `(unreleased)` release-notes section).

## Objective

Ship **Enigma.Msi.Desktop 1.5.0**. It is an application-only SemVer **minor** release and adds no new
work: it releases what the `(unreleased)` section of `RELEASENOTES.md` already records.

- **BUG-7325**: the worker's `.wixsharp/` folder is seeded at build time and ships in the MSI. An app
  installed under Program Files can now build MSIs.
- **FEATURE-1399**: a new package defaults to `None` compression instead of `High`.

The release is prepared **and verified on Windows**. This host has the WiX CLI, so the dogfood MSI is
built and read back here. **The Enigma.Msi library stays at 1.0.0**, and nothing is tagged, pushed or
uploaded.

## Context & constraints

Read from the repository at planning time (2026-10-22), on `vibe/2026-10-22-wixsharp-dir-compression` @
`ab5cd09`:

- `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj` declares `<Version>1.4.0</Version>`. Tag `1.4.0`
  exists, on `main`'s `a5f555c`. The tags are bare: `1.0.0`, `1.1.0`, `1.3.0` and `1.4.0`; there is no
  `1.2.0`.
- `msiProfiles/Enigma.Msi.Desktop.1.4.0.msipkg.json` is the newest profile: `productId`
  `e45c0ea9-78b4-477d-bef6-22c914e281a1`, `upgradeCode` `3405046f-527a-439e-a22f-866247dc8314`,
  `"compression": "High"`. The manufacturer is stored escaped (`Josué Clément`).
- `RELEASENOTES.md` has a top `## (unreleased) — Enigma.Msi.Desktop` section with *Changed*
  (FEATURE-1399) and *Fixed* (BUG-7325). Its *Fixed* bullet says "installing the next release". The
  current-version table's app row reads **1.4.0**.
- `README.md`'s what's-new callout names 1.4.0.
- The supported-versions table in `SECURITY.md` lists the app at **1.2.x**. It went stale at 1.3.0.
- `docs/RELEASE.md` §4 says "`git tag` shows `1.0.0` and `1.1.0`". That sentence went stale at 1.3.0.
- The build-state block in `CLAUDE.md` still has two stale statements. It says 1.4.0 is "prepared but not
  yet built as an MSI … Still owed on Windows". BUG-7325's done doc records a Windows **464/464** baseline at
  `a2fd60c`, and tag `1.4.0` exists. It also says BUG-7325 and FEATURE-1399 are "unreleased, no version
  bump".
- The host is Windows 11 with SDK 10.0.402 and **WiX 7.0.0** on `PATH`. The last Windows run (FEATURE-1399)
  was **471/471**. `artifacts/` does not exist yet; it is git-ignored.
- Zero-warning builds (`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`). Tests run as
  `dotnet test --solution Enigma.Msi.slnx -c Release`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Release content | Exactly the `(unreleased)` section: BUG-7325 and FEATURE-1399. No new feature | The prompt asks for a release. The pending changes are already done, tested and noted | Adding unrequested polish to "fill out" the release |
| Version | **1.5.0**, as given | SemVer minor fits. A changed default for new packages is a behaviour change, not a fix, so a patch would understate it. No profile, API or format change, so not a major | 1.4.1 (hides the default change); 2.0.0 (nothing breaks) |
| Work-item shape | One single-phase FEATURE | A routine release is one dev, one reviewable commit (dotnet-release *routine release*). The two changes it ships are already merged | A multi-phase item; folding the release into FEATURE-1399 |
| The dogfood profile's `compression` | **Keep `High`**, per the clone rule | The release MSI is downloaded and installed by others, so its size (~17 MB at `High` against ~50 MB at `None`) matters more than a build that runs once per release. FEATURE-1399 deliberately left committed profiles at `High` | Switching the release profile to `None` (triples the shipped installer to save one slow build per release) |
| Library version | Stays **1.0.0** | It is in-repo only and has no release flow (`docs/RELEASE.md`). FEATURE-1399's notes already state "The library remains at 1.0.0" | Bumping it to 1.1.0 for the changed `new MsiPackage()` default (no consumer outside the repo to signal) |
| Dependency refresh / TFMs | None | Every app release so far held dependencies. The coupled UI set (Avalonia 12.1.1 + Enigma.* 1.0.0) moves only as a deliberate work item, and the prompt does not ask for it | Opportunistic non-coupled bumps |
| Verification depth | Full Windows verification here: warning-free build, the whole suite (expected **471/471**), the `win-x64` publish, the **dogfood MSI build**, and a read-back of the MSI's Property, Upgrade, Shortcut and File tables, including the six `.wixsharp` files | This host can run all of it. The File-table check proves in the shipped artifact what BUG-7325 claims ships | Stopping at the build and tests (leaves the release's headline fix unproven in the real MSI) |
| `SECURITY.md` supported version | App row → **1.5.x** | The policy promises fixes for "the latest released version". 1.2.x is wrong today and would stay wrong | Leaving it (a factual error in the security policy) |
| `docs/RELEASE.md` §4's tag sentence | Reword it so it states the bare-tag convention without listing tags | It went stale at 1.3.0 because it enumerated versions. A version-agnostic sentence cannot go stale again | Appending `1.3.0`, `1.4.0`, `1.5.0` (stales again at 1.6.0) |
| `CLAUDE.md` build-state | Replace the `BUG-7325`/`FEATURE-1399` "unreleased" entry with this release's entry, and correct FEATURE-1DEF's "still owed on Windows" sentence to what the repo can show (Windows 464/464 at `a2fd60c`; tag `1.4.0` on `main`) | Both statements are false once 1.5.0 is cut. The correction states only what the repo can show, and does not claim a 1.4.0 MSI build nobody recorded | Leaving the 1.4.0 sentence (CLAUDE.md is read by every agent) |
| Release-notes layout | Rename `(unreleased)` → `## 1.5.0 — Enigma.Msi.Desktop`, add the usual intro paragraph, order *Fixed → Changed → Compatibility*, and change "the next release" → "1.5.0" | `docs/RELEASE.md` §1 renames rather than duplicates. *Fixed* is the headline. The house section order puts fixes before behaviour changes, and every earlier section ends with *Compatibility* | A fresh section above the `(unreleased)` one |
| New tests | None | The release changes one version string, one JSON data file and docs. The profile is proven by the worker build itself | A permanent profile test (binds the suite to a file that changes every release) |
| Outward-facing steps | Merge, tag and push are printed only; install/launch/uninstall stays the maintainer's | `docs/RELEASE.md` and the run's no-push rule | — |

---

## Steps

Follows `docs/RELEASE.md` §1–§2.

1. `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`: `<Version>` **1.5.0**.
2. `msiProfiles/Enigma.Msi.Desktop.1.5.0.msipkg.json`: a clone of the 1.4.0 profile. `version` 1.5.0, a
   **fresh `productId`** from `[guid]::NewGuid()` (lowercased to match the committed profiles),
   **`upgradeCode` verbatim** (`3405046f-527a-439e-a22f-866247dc8314`), and everything else unchanged,
   `"compression": "High"` included. The older profiles are kept.
3. `RELEASENOTES.md`:
   - The current-version table's app row → **1.5.0**.
   - `(unreleased)` → `## 1.5.0 — Enigma.Msi.Desktop`, with an intro paragraph: an app-only release, the
     library at 1.0.0, no format change.
   - Sections in the order *Fixed → Changed → Compatibility*. "Installs of 1.0.0 to 1.4.0 are fixed by
     installing the next release" → "… by installing 1.5.0".
   - *Compatibility* says: `schemaVersion` 1, profiles interchangeable 1.0.0 … 1.5.0 (with the one caveat
     already stated: a profile that omits `compression` now builds at `None`), the library surface
     unchanged except the `MsiPackage.Compression` default, no dependency moved, and the TFM/runtime/WiX
     prerequisites as before.
4. `README.md`: the what's-new callout → 1.5.0. Both headlines, one line each.
5. `SECURITY.md`: the app's supported-versions row → `1.5.x`, keeping the table aligned.
6. `docs/RELEASE.md` §4: reword the tag sentence so it does not list versions.
7. `CLAUDE.md` build-state block: the 1.4.0 correction and the 1.5.0 entry, per *Decisions*.
8. Verification on this host:
   - `dotnet build Enigma.Msi.slnx -c Release`: 0 warnings, read from the output.
   - `dotnet test --solution Enigma.Msi.slnx -c Release`: whole suite green, worker suite and drift guards
     included.
   - `mkdir artifacts`, then `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r
     win-x64 --self-contained false`. Confirm `Enigma.Msi.Desktop.dll` stamps **1.5.0** and that
     `publish/worker/.wixsharp/` holds the six seeded files.
   - Run `.\src\Enigma.Msi.Desktop\bin\Release\net10.0\worker\Enigma.Msi.Worker.exe build
     msiProfiles\Enigma.Msi.Desktop.1.5.0.msipkg.json`. Expect exit 0 and
     `artifacts\Enigma.Msi.Desktop.msi`. Record its size, its duration and that no `wix` process is
     orphaned.
   - Read the MSI back with `WindowsInstaller.Installer`:
     - **Property:** ProductVersion 1.5.0, ProductCode = the new `productId`, UpgradeCode, Manufacturer
       with the non-ASCII intact, ALLUSERS.
     - **Upgrade:** VersionMax/VersionMin 1.5.0.
     - **Shortcut:** two rows.
     - **File/Directory:** the six `.wixsharp` files under `worker\.wixsharp`.
   - `artifacts/` is a git-ignored scratch output. It is never committed.
9. **Nothing outward-facing is run.** No tag, no push, no upload, no install.

## Acceptance criteria

- `<Version>` is 1.5.0, and the published `Enigma.Msi.Desktop.dll` stamps 1.5.0.
- `RELEASENOTES.md` leads with a `1.5.0` section (no `(unreleased)` heading remains), and its version
  table names 1.5.0. The README callout names 1.5.0, and `SECURITY.md` supports 1.5.x.
- `msiProfiles/Enigma.Msi.Desktop.1.5.0.msipkg.json` is committed. A `diff` against 1.4.0 shows only
  `version` and `productId`, and the `upgradeCode` is verbatim.
- The solution builds with 0 warnings, and the whole suite passes on Windows.
- The dogfood MSI is built from the committed profile (worker exit 0). Its tables carry ProductVersion
  1.5.0, the new ProductCode, the unchanged UpgradeCode, and the six `.wixsharp` files.
- `docs/RELEASE.md` §4 and `CLAUDE.md` no longer state anything the release made false.
- The library is still at 1.0.0, and nothing was tagged, pushed or uploaded.

## Out of scope

- Tagging `1.5.0`, merging to `main`, pushing, attaching the MSI anywhere, and the missing `1.2.0` tag.
  These are the maintainer's.
- Install/launch/uninstall on a test machine (`docs/RELEASE.md` §5).
- Switching the release profile to `None` compression.
- Any code, library, worker or profile-format change. That includes the follow-ups earlier releases left
  open: the worker csproj's stale `PackEnigmaMsiWorkerPayload` comment, LF-only serializer output,
  `helpLink`/`urlInfoAbout` on the dogfood profile.
- Any dependency or TFM change.
