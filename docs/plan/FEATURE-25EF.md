# FEATURE-25EF — Desktop app 1.6.0: new icons

**Status:** TODO (multi-phase — 2 phases)
**Type:** FEATURE (multi-phase)
**Branch (per phase, at build time):** `feature/feature-25ef-phaseNN-<slug>` — one branch per phase, cut from the run-branch tip.
**Run:** feature/2026-10-28-new-icons-release
**Depends on:** FEATURE-1E7D DONE (app at 1.5.0; this item builds the next app release on top).

## Objective

Ship **Enigma.Msi.Desktop 1.6.0** with the app's new artwork.

1. **New icon and logo.** The maintainer dropped a new icon set into the untracked
   `new-icons-remove-when-done/`: an `.ico`, ten PNG frames and an SVG master. It replaces the app's
   current window/executable/installer icon and its splash/About logo. The temporary folder is then
   deleted.
2. **Release 1.6.0**: app-only, a SemVer **minor**. The prompt's rule is minor + 1, with the patch reset
   to 0. The release has a visible change and no breaking one, and no profile-format change.

**The Enigma.Msi library stays at 1.0.0** and is untouched by both phases: the model, the validator, the
`.msipkg.json` contract (`schemaVersion` 1), the build client and the `net472` worker. Profiles stay
interchangeable between 1.0.0 … 1.6.0 in both directions and there is no migration.

## Context & constraints

Read from the repository at planning time (2026-10-28), on `develop` @ `b6c8b1f`:

- **The app has two image assets**, both under `src/Enigma.Msi.Desktop/Assets/`, embedded as
  `<AvaloniaResource Include="Assets/**" />`:
  - `appicon.ico` has 7 frames: 16, 24, 32, 48 and 64 as BMP, 128 and 256 as PNG. It is the csproj's
    `<ApplicationIcon>` (the Win32 icon of the dll and the apphost) and `MainWindow.axaml`'s
    `Icon="/Assets/appicon.ico"`. It is also `controlPanel.productIcon` and both shortcuts' `iconPath` in
    **all six** committed profiles (`msiProfiles/…1.0.0` to `…1.5.0`), as
    `src\Enigma.Msi.Desktop\Assets\appicon.ico`.
  - `logo.png` is 256×256 RGBA. `AppInfo.LogoUri` names it, and so do `SplashWindow.axaml` (shown at
    96×96) and `AboutCard.axaml` (64×64). `SplashWindowTests.TheLogo_IsAResourceThatResolves` and
    `AppInfoTests.LogoUri_*` cover it.
- **The new set** (`new-icons-remove-when-done/`, untracked):
  - `enigma-msi.ico` has 10 frames, all PNG: 16, 20, 24, 32, 40, 48, 64, 96, 128 and 256.
  - Ten frames `enigma-msi-<n>.png`. `enigma-msi-256.png` is byte-identical to the `.ico`'s 256 frame.
  - `enigma-msi.svg`, whose own comment names its master: `Branding/Apps/export.py`. That is outside this
    repository.
- The app declares `<Version>1.5.0</Version>`. The tags are bare and lightweight, on `main`'s merge
  commits: `1.0.0`, `1.1.0`, `1.3.0`, `1.4.0` and `1.5.0`.
- `msiProfiles/Enigma.Msi.Desktop.1.5.0.msipkg.json` is the newest profile: `upgradeCode`
  `3405046f-527a-439e-a22f-866247dc8314`, `"compression": "High"`.
- **This host is Linux**, with SDK 10.0.103 and no `wix` or `mono`. As at 1.1.0 and 1.4.0:
  - The library suite has a documented **Windows-only cluster** that fails here.
  - The `net472` worker suite cannot start.
  - The dogfood MSI cannot be built.
- **The Linux test rule.** Before any change, record a baseline of failing tests on the run branch. A dev
  is green when the build has 0 warnings and the desktop suite is fully green. The library suite's failing
  list must equal the baseline, compared by `diff`.
- Zero-warning builds (`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`). Tests run as
  `dotnet test --solution Enigma.Msi.slnx -c Release`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which files does the new set replace, and under which names? | Overwrite in place, keeping the names: `appicon.ico` ← `enigma-msi.ico`, `logo.png` ← `enigma-msi-256.png` | The six committed profiles, the csproj, `MainWindow.axaml`, `AppInfo.LogoUri`, both views and the tests all name these paths. Keeping them makes the change pure artwork, with every reference unchanged and every profile still valid. The names describe a role, not a design | Renaming to `enigma-msi.*` (touches six references and leaves the 1.0.0–1.5.0 profiles pointing at a deleted file) |
| The other nine PNG frames and the SVG | **Not committed** | Nothing in the app uses them. `Assets/**` is embedded wholesale, so they would ride in the assembly for nothing. The SVG names its master (`Branding/Apps`) outside this repo, so a copy here would be a second, drifting source of truth | Committing the full set under `Assets/`; keeping it under a non-embedded `branding/` folder |
| Which PNG backs the logo? | The **256 px** frame | The views draw it at 96 and 64 DIPs. At 200 % scaling that is 192 and 128 px, which a 256 source covers and a 96 or 64 px frame would blur. It is also what the current `logo.png` is | Matching frames per view (two resources, blurry on HiDPI) |
| Temporary folder in the dirty tree | Proceed. PHASE01 deletes it, and it was never tracked, so nothing is committed for it | The prompt names it as the spec's input and asks for its removal. It is not someone's unknown work | Stopping at the pre-flight's *dirty working tree* |
| New tests | None | The existing headless test proves the logo resource resolves. Roslyn parses the `.ico` to embed it as the Win32 icon, so a malformed file fails the build. A test pinning frame sizes would pin artwork, not behaviour | A frame-count test on `appicon.ico` |
| Version | **1.6.0** | The prompt's rule (minor + 1, patch → 0). SemVer agrees: visible, nothing breaks | — |
| Work-item shape | One FEATURE, two phases: artwork, then release prep | The same shape as FEATURE-1DEF. Each phase is one reviewable commit | Two items; one phase mixing both |
| Library version | Stays **1.0.0** | Nothing in it changes | — |
| Dogfood profile | Clone 1.5.0 per the clone rule: `version`, a fresh `productId` (`uuidgen`, lowercased), `upgradeCode` verbatim, everything else unchanged | The icon paths need no edit, because the names are kept. `compression` stays `High`, as at 1.5.0 | — |
| Verification on Linux | As at 1.4.0: the build, the suite under the Linux test rule, a `win-x64` cross-publish stamping 1.6.0, and a scratchpad harness for the profile. The MSI build and the Windows test run are **explicitly blocked** and handed over | This host can do no more. Skipping them silently would overstate the release | Claiming Windows verification |
| README's "build its own installer" example | Point it at the 1.6.0 profile | It names the 1.3.0 profile, which builds a three-releases-old installer | Leaving it stale |
| `CLAUDE.md`'s "nothing was tagged" for 1.3.0 and 1.5.0 | Correct both to "the run tagged nothing; the maintainer tagged it later" | Tags `1.3.0` and `1.5.0` exist on `main`. FEATURE-1E7D made the same correction for 1.4.0 | Leaving them |
| Merges and tag | **After the run**, at the user's explicit request: the run branch into `develop` and `develop` into `main` (both `--no-ff`, default messages, as the history shows), then a bare lightweight `1.6.0` on `main`'s merge commit. Nothing is pushed | The run itself never writes `develop`/`main` or tags (vibe §5). The user asked for the merges and the tag as a step after the run | Tagging from inside the run; pushing |
| Dependency refresh / TFMs | None | The coupled UI set moves only as a deliberate item. The prompt does not ask for it | — |

---

## PHASE01 — Replace the app icon and logo

**Status:** TODO
**Branch:** `feature/feature-25ef-phase01-new-icons`

1. Copy `new-icons-remove-when-done/enigma-msi.ico` over `src/Enigma.Msi.Desktop/Assets/appicon.ico`.
2. Copy `new-icons-remove-when-done/enigma-msi-256.png` over `src/Enigma.Msi.Desktop/Assets/logo.png`.
3. Delete `new-icons-remove-when-done/` (untracked, so this is a plain `rm`).
4. Verify: a warning-free build, and the suite under the Linux test rule. Check that the built
   `Enigma.Msi.Desktop.dll`'s Win32 resources carry the new icon's 10 frames.

**Acceptance criteria**

- `Assets/appicon.ico` and `Assets/logo.png` are byte-identical to `enigma-msi.ico` and
  `enigma-msi-256.png`, compared before the folder is deleted. No other file under `Assets/` changes.
- `new-icons-remove-when-done/` no longer exists, and no file from it is committed besides the two
  replacements.
- No reference changes: the csproj, the XAML, `AppInfo` and the profiles are untouched.
- The solution builds with 0 warnings. The desktop suite is fully green, the headless logo test included.
  The library suite's failures equal the baseline.
- The built dll's icon group holds the 10 new frames.

## PHASE02 — Release prep: desktop app 1.6.0

**Status:** TODO
**Branch:** `feature/feature-25ef-phase02-release-1-6-0`

Follows `docs/RELEASE.md` §1–§2. **App-only:** no library version change, and there is no pack step.

1. `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`: `<Version>` **1.6.0**.
2. `msiProfiles/Enigma.Msi.Desktop.1.6.0.msipkg.json`: a clone of 1.5.0. It gets `version` 1.6.0, a
   **fresh `productId`** from `uuidgen` (lowercased) and the **`upgradeCode` verbatim**; everything else is
   unchanged. The older profiles are kept.
3. Verify the new profile deserializes through `MsiPackageJson` and passes the in-memory rules with zero
   errors. Use a scratchpad harness against the real `Enigma.Msi` and delete it afterwards.
4. `RELEASENOTES.md`:
   - A `## 1.6.0 — Enigma.Msi.Desktop` section on top: an intro paragraph, *Changed* (the new icon
     everywhere it appears) and *Compatibility*.
   - The current-version table's app row → **1.6.0**.
5. `README.md`: the what's-new callout → 1.6.0. The "build its own installer" example → the 1.6.0 profile.
6. `SECURITY.md`: the app's supported-versions row → `1.6.x`.
7. `CLAUDE.md`, the build-state block: a FEATURE-25EF entry, and the 1.3.0/1.5.0 tag corrections (see
   *Decisions*).
8. Verification on this host:
   - A warning-free build, and the suite under the Linux test rule.
   - A cross-publish, `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r
     win-x64 --self-contained false`. Confirm the assembly stamps **1.6.0** and that `worker/` and
     `worker/.wixsharp/` are present.
   - The dogfood MSI build and the Windows test run are recorded as **blocked on this host**, with the
     commands.
9. **The run itself runs nothing outward-facing.** The merges and the tag follow the run (see
   *Decisions*); nothing is pushed.

**Acceptance criteria**

- `<Version>` is 1.6.0, and the cross-published dll stamps 1.6.0.
- `RELEASENOTES.md` leads with a 1.6.0 section, and its table names 1.6.0. The README callout names 1.6.0,
  and `SECURITY.md` supports 1.6.x.
- The 1.6.0 profile is committed. A `diff` against 1.5.0 shows only `version` and `productId`, and the
  `upgradeCode` is verbatim. It deserializes, with 0 in-memory errors.
- The solution builds with 0 warnings. The desktop suite is fully green, and the library failures equal
  the baseline.
- The dogfood MSI is explicitly blocked, with the reason and the commands.
- The library is still at 1.0.0. Nothing was packed or pushed.

## Out of scope

- Regenerating the artwork. `Branding/Apps/export.py` lives outside this repository.
- An SVG-rendered logo, which would need a new dependency.
- Install/launch/uninstall on a test machine (`docs/RELEASE.md` §5). Building the MSI on Windows.
- Any library, worker, validator or profile-format change, and the open follow-ups earlier releases left:
  - the stale `PackEnigmaMsiWorkerPayload` comment;
  - `Environment.NewLine` in the serializer;
  - `helpLink`/`urlInfoAbout` on the dogfood profile;
  - the missing `1.2.0` tag.
- Any dependency or TFM change.
