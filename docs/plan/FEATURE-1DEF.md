# FEATURE-1DEF — Desktop app 1.4.0: the quick start's MSI file name

**Status:** DONE (multi-phase — 2 phases)
**Type:** FEATURE (multi-phase)
**Branch (per phase, at build time):** `feature/feature-1def-phaseNN-<slug>` — one branch per phase, cut from the run-branch tip.
**Run:** feature/2026-10-01-quickstart-msi-name-release
**Depends on:** FEATURE-2D02 fully DONE (app at 1.3.0; this item builds the next app release on top).

## Objective

Ship **Enigma.Msi.Desktop 1.4.0**.

1. **The quick start shows the MSI file name, directly under Version** — the value that lands in
   *Install and output → MSI file name* on Apply. It is **derived live** from the application name and the
   version while the user types: spaces in the application name become dots and the version is appended,
   so `Enigma Msi` + `1.4.0` gives **`Enigma.Msi.1.4.0`**. 1.3.0 derived the name silently, from the
   application name alone, and only at Apply.
2. **Release 1.4.0** — app-only, a SemVer **minor**: one backwards-compatible feature, no breaking change,
   no profile-format change.

**The Enigma.Msi library stays at 1.0.0** and is untouched by both phases: the model, the validator, the
`.msipkg.json` contract (`schemaVersion` 1), the build client and the `net472` worker. Profiles stay
interchangeable between 1.0.0 … 1.4.0 in both directions and there is no migration.

## Context & constraints

Read from the repository at planning time (2026-10-01), and binding on every phase:

- **Zero-warning builds.** `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` are on solution-wide. The
  XAML compiler's `AVLN5001` is part of that — `PlaceholderText`, never the obsolete `Watermark`, on the
  control library's `TextBox`-derived editors. `AVLN*` warnings are printed, not promoted: read the count.
- **MTP-native tests.** `dotnet test --solution Enigma.Msi.slnx -c Release`.
- **The coupled UI set** (Avalonia 12.1.1 + `Enigma.Avalonia.Desktop` 1.0.0 + `Enigma.Icons.Avalonia`
  1.0.0 + the two headless packages) is not bumped by this item.
- **The shared `ContentDialog`'s two traps**, already handled by `QuickStartDialogService` and still
  binding: the `CanApply` binding onto the host is disposed on close, and every `Dialog*` size property is
  stated. `DialogHeight` is `NaN` with an infinite max, so an eighth field grows the dialog rather than
  clipping it.
- **The validation split.** The quick start applies string and parse rules only and never touches the
  disk. The MSI file name's two rules in the validator (`output.msiFilename`: no invalid file-name
  characters, no `.msi` extension) *are* string rules, so the dialog may — and should — enforce them.
- **MVVM style.** The app uses CommunityToolkit's source generators (`[ObservableProperty]`,
  `[NotifyPropertyChangedFor]`, `[RelayCommand]`) throughout — 56 usages. The house rule is explicit
  properties without generators; per that rule's own escape clause, **this codebase stays consistent with
  itself** and the divergence is flagged rather than half-migrated.
- **This run is on Linux.** The library suite's Windows-only cluster (16 tests × 2 TFMs —
  `MsiBuildServiceTests` 10, `MsiPrerequisiteTests` 5, `MsiPackageValidatorTests.MsiFilename_MustNotBeAPath`
  1) fails here by construction, and the `net472` worker suite cannot start without `mono`. Baseline on the
  starting commit `585710a`: build 0 warnings; **374 tests, 342 passed, 32 failed** — exactly that set;
  `Enigma.Msi.Desktop.UnitTests` **142/142**.

### Verified against the code at planning time (2026-10-01)

- `PackageEditorViewModel.ApplyQuickStart` sets `MsiFilename = ToMsiFilename(appName)`; the private
  static `ToMsiFilename` strips invalid file-name characters and a trailing `.msi`, and falls back to
  `FallbackMsiFilename` (`"package"`). It has that single call site; `FallbackMsiFilename` is referenced by
  one test (`ApplyQuickStart_FallsBackToAFileNameWhenNothingSurvivesSanitization`).
- `QuickStartViewModel` holds seven `[ObservableProperty]` answers; `CanApply` is computed;
  `ToSettings()` trims every answer. `QuickStartSettings` is a positional record of seven.
- `QuickStartCard.axaml` lays the fields out as name, version, manufacturer, release folder, output
  folder, icon, main executable; field + hint pairs are wrapped in a `StackPanel` with the card's `.hint`
  style. The main form's MSI field is a `TextEditor` with `Unit=".msi"`.
- "seven" appears in: the card's intro sentence, the toolbar tooltip (`MainWindow.axaml`), XML docs in
  `IQuickStartDialogService`, `QuickStartDialogService`, `MainWindowViewModel`, `QuickStartCard.axaml.cs`,
  `QuickStartViewModel`, `QuickStartSettings`, `PackageEditorViewModel`, and in
  `docs/guides/desktop-app.md` and `README.md`. (`MainWindowViewModelTests` still says "six answers" — stale
  since 1.3.0.)
- The 1.3.0 MSI profile's `upgradeCode` is `3405046f-527a-439e-a22f-866247dc8314`; its
  `output.msiFilename` is `Enigma.Msi.Desktop`.
- Tags: `1.0.0`, `1.1.0`, `1.3.0` (bare `X.Y.Z`).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One `FEATURE`, two phases: the quick-start field, then the 1.4.0 release prep | The repository's own release pattern (FEATURE-6C35, -74C4, -2D02): feature phases, then a release phase; one reviewable commit each | One combined dev (mixes a code review with a release review); two separate items (the release only exists because of the feature) |
| Editable or read-only field? | **Editable**, auto-following the application name and version **until the user types a value of their own**; emptying the field makes it follow again on the next name/version edit | The draft asks for automatic updates; editability keeps the dialog's "every field is an answer" shape and lets a user who wants another name state it there, instead of fixing it on the form afterwards. Detaching on a manual edit is the standard pattern for derived fields — it never overwrites what the user typed | Read-only preview (forces a second edit on the form for anyone who wants another name); always overwrite on name/version change (destroys a deliberate edit) |
| Derivation rule | Strip characters a file name cannot hold from the application name, turn each run of whitespace into **one** `.`, append `.` + the version treated the same way; a trailing `.msi` is stripped; **blank while the application name is blank** | Reproduces the draft's example exactly (`Enigma Msi` + `1.4.0` → `Enigma.Msi.1.4.0`) and always satisfies the validator's `output.msiFilename` rule; blank-while-nameless keeps the dialog from opening on `1.0.0` | Version-only output on an empty name; replacing each space individually (`A  B` → `A..B`) |
| What the Apply gate requires of it | Non-blank, and the validator's two string rules — no invalid file-name characters, no `.msi` extension — with an **inline warning** under the field, as the executable already has | The dialog's promise is a package that passes Validate with zero problems; a derived value is always valid, so the gate only bites on a hand-typed one, and then it says why | Silent gate (Apply disabled with no reason); no gate (a typed `Widget.msi` would reach the Problems pane) |
| The `package` fallback | **Deleted**, with `PackageEditorViewModel.ToMsiFilename` | The field is now visible and required: a name that sanitizes to nothing leaves it blank and gates Apply, which is clearer than substituting a name the user never saw | Keeping a fallback inside the dialog |
| `QuickStartSettings` shape | Positional record gains `MsiFilename` **after `Version`**, mirroring the field order | Same rule FEATURE-2D02 applied to `OutputPath` | A named property; a separate derivation call in `ApplyQuickStart` |
| The main form's MSI field | Unchanged — no auto-follow outside the dialog | The draft scopes the behaviour to the content dialog; on the form, the field is an ordinary stored value | Live derivation on the form too |
| Counting | "eight answers" / "eight fields" wherever the UI and the docs count them | 1.3.0 set the pattern (six → seven); the MSI name is a field the user may answer, pre-filled or not | Dropping the count from every text |
| Release version | **1.4.0** | SemVer: a backwards-compatible feature is a minor bump; no profile, API or behaviour of an existing profile changes | 1.3.1 (not a fix); 2.0.0 (nothing breaks) |
| Dependency refresh / TFMs | None — everything holds at its pinned version; TFMs unchanged | Every previous app release did the same; the prompt does not ask for it, and the coupled UI set moves only as a deliberate work item | Bumping non-coupled packages opportunistically |
| The repository's own 1.4.0 profile | Clone of 1.3.0: fresh `productId` (`uuidgen`), `upgradeCode` verbatim, `version` 1.4.0, **`msiFilename` unchanged** (`Enigma.Msi.Desktop`) | The runbook's clone rule changes exactly three fields, and `docs/RELEASE.md` names `artifacts\Enigma.Msi.Desktop.msi` as the artifact | Adopting the new `<name>.<version>` convention for the dogfood profile (changes the documented artifact path; its own decision) |
| Verification on this Linux host | Desktop suite fully green; library suite failures **identical** to the 32-test Windows-only baseline; worker suite recorded as not runnable; dogfood MSI build and the Windows test run handed to the maintainer, as 1.1.0 did | The repository's precedent (FEATURE-6C35-PHASE02) and the only honest reading of DoD criterion 2 on a host that cannot run those tests: no regression, everything runnable is green | Calling the release "verified" without a Windows run |
| Card-level headless test | None added | The card is pure markup over bindings; compiled bindings fail the build on a wrong path, and the ViewModel carries every behaviour | A headless `QuickStartCard` test (gold-plating for a one-field markup change) |

---

## PHASE01 — Quick start: derived MSI file name

**Status:** DONE
**Branch:** `feature/feature-1def-phase01-quickstart-msi-filename`

1. `src/Enigma.Msi.Desktop/ViewModels/QuickStartSettings.cs` — add `MsiFilename` **after `Version`**, with
   its `<param>` doc: the `.msi` base name, without extension, as shown (and possibly edited) in the
   dialog. Correct the `AppName` param ("also the MSI file name" is no longer true) and the summary's count.
2. `src/Enigma.Msi.Desktop/ViewModels/QuickStartViewModel.cs`:
   - an eighth `[ObservableProperty] string _msiFilename` notifying `CanApply`, `MsiFilenameError` and
     `HasMsiFilenameError`;
   - `OnAppNameChanged` / `OnVersionChanged` partial hooks that recompute the derived name and, **while the
     field still holds the previously derived value or is blank**, write the new one into it; a
     private `_derivedMsiFilename` remembers the last derived value;
   - the derivation as a private static helper (moved from `PackageEditorViewModel`, extended with the
     whitespace → `.` rule and the version suffix);
   - `MsiFilenameError` (`null` while blank or valid; otherwise one of two public message constants — invalid
     characters, `.msi` extension) and `HasMsiFilenameError`;
   - `CanApply` gains `!string.IsNullOrWhiteSpace(MsiFilename) && MsiFilenameError is null`;
   - `ToSettings()` carries `MsiFilename.Trim()`;
   - the class `<remarks>`: eight answers, the MSI name derived from two of them until edited.
3. `src/Enigma.Msi.Desktop/Views/QuickStartCard.axaml` — insert, **between Version and Manufacturer**, a
   `StackPanel` holding an `MSI file name` `TextEditor` (`Unit=".msi"`, a placeholder such as
   `Contoso.Widget.1.0.0`), a `.hint` line ("Follows the application name and the version — spaces become
   dots — until you type your own. The build appends .msi.") and the warning `TextBlock` bound to
   `MsiFilenameError` in `EnigmaWarningBrush`, as the executable field does. The intro sentence counts
   eight answers and stops listing the MSI name among what is derived.
4. `src/Enigma.Msi.Desktop/ViewModels/PackageEditorViewModel.cs` — `ApplyQuickStart` sets
   `MsiFilename = settings.MsiFilename`; delete `ToMsiFilename`, `FallbackMsiFilename`, the `MsiExtension`
   constant and any `using` left unused; correct the `<remarks>` list of what the quick start derives.
5. The remaining "seven" wording: the toolbar tooltip in `MainWindow.axaml`; the XML docs of
   `IQuickStartDialogService`, `QuickStartDialogService`, `MainWindowViewModel`, `QuickStartCard.axaml.cs`.
6. Tests, `tests/Enigma.Msi.Desktop.UnitTests`:
   - `QuickStartViewModelTests` — the draft's own example (`Enigma Msi` + `1.4.0` → `Enigma.Msi.1.4.0`);
     follows version edits; collapses whitespace runs and trims; strips invalid characters; blank while
     the name is blank; stops following after a manual edit and survives later name/version edits;
     follows again once emptied; `CanApply` false while blank, with invalid characters, or with a `.msi`
     extension (and the matching `MsiFilenameError`/`HasMsiFilenameError`); `ToSettings` carries and trims
     it; the seven-answer cases become eight.
   - `PackageEditorViewModelTests` — `ApplyQuickStart` takes the MSI file name verbatim from the settings;
     the three derivation tests (sanitize, `.msi` strip, `package` fallback) move to the ViewModel suite or
     are deleted with the code they covered; `CreateSettings` gains the new argument.
   - `MainWindowViewModelTests` — the two settings helpers gain the argument; the end-to-end
     "validates cleanly" case still passes; the stale "six answers" comment is corrected.
7. Documentation: `docs/guides/desktop-app.md` (the field table gains *MSI file name* under *Version*, the
   derived table loses it, the counts become eight), `README.md`'s quick-start bullet, and `CLAUDE.md`'s
   build-state block (FEATURE-1DEF recorded).

**Acceptance criteria**

- The quick start shows eight fields in the order name, version, **MSI file name**, manufacturer, release
  folder, output folder, icon, main executable; the MSI field carries the `.msi` unit and a permanent hint.
- Typing `Enigma Msi` as the name with `1.4.0` as the version shows `Enigma.Msi.1.4.0` without any other
  action; editing either updates it live.
- A value typed into the MSI field by hand is never overwritten by later name/version edits; emptying the
  field makes it follow again.
- Apply stays disabled while the MSI name is blank, holds an invalid file-name character or ends in
  `.msi`, and the dialog says which.
- Applying puts the dialog's MSI file name on the form verbatim; `FallbackMsiFilename` and
  `PackageEditorViewModel.ToMsiFilename` no longer exist.
- A package produced by the quick start still passes Validate with zero problems.
- Solution builds warning-free; the desktop suite passes in full including the new cases, and the library
  suite fails on exactly the Windows-only baseline set and nothing else.

---

## PHASE02 — Release prep: desktop app 1.4.0

**Status:** DONE
**Branch:** `feature/feature-1def-phase02-release-1-4-0`

Follows `docs/RELEASE.md`. **App-only:** no library version change, no pack (there is no pack step).

1. `src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj`: `<Version>` **1.4.0**.
2. `msiProfiles/Enigma.Msi.Desktop.1.4.0.msipkg.json` — clone of the 1.3.0 profile with `version` 1.4.0, a
   **fresh `productId`** from `uuidgen` (never hand-fabricated), **`upgradeCode` reused verbatim**
   (`3405046f-527a-439e-a22f-866247dc8314`); everything else unchanged. The older profiles are kept.
3. Verify the new profile deserializes through `MsiPackageJson` and passes the validator's in-memory rules
   with zero errors (scratchpad harness against the real `Enigma.Msi` project, deleted afterwards).
4. `RELEASENOTES.md`: a **1.4.0** section on top — the quick start's MSI file name (shown under Version,
   derived live with spaces → dots and the version appended, editable and then left alone, gated on the
   validator's string rules; the silent `package` fallback is gone; existing profiles unaffected) — plus
   *Compatibility*; the current-version table's app row → 1.4.0.
5. `README.md`'s what's-new callout → 1.4.0.
6. Verification on this host: warning-free build; full suite with the Linux baseline rule above; a
   cross-publish `dotnet publish src/Enigma.Msi.Desktop/Enigma.Msi.Desktop.csproj -c Release -r win-x64
   --self-contained false` confirming the assembly stamps **1.4.0** and the `worker/` payload is present.
   The **dogfood MSI build** and the Windows test run (worker suite, drift guards, the Windows-only library
   cluster) are recorded as **blocked on this host** and handed to the maintainer — never silently skipped.
7. Documentation freshness: `CLAUDE.md`'s build-state block gains the 1.4.0 outcome; the desktop guide is
   re-read against the shipped UI.
8. **Nothing outward-facing is run** — no tag, no push, no upload. The printed runbook is the deliverable.

**Acceptance criteria**

- `<Version>` is 1.4.0; the 1.4.0 release-notes section, the version table and the README callout are in
  place; the 1.4.0 profile is committed with a fresh `productId` and the verbatim `upgradeCode`,
  deserializes, and reports zero in-memory validation errors.
- The solution builds warning-free; the desktop suite is fully green and the library suite's failures are
  exactly the Windows-only baseline.
- The cross-published app stamps 1.4.0; the dogfood MSI build is either recorded or explicitly blocked with
  the reason.
- The library is still at 1.0.0, and nothing was packed, tagged or pushed.

## Out of scope

- Live derivation of the MSI name on the main form, outside the quick start.
- Changing the dogfood profile's `msiFilename` or the documented artifact path.
- Any library, worker, validator or profile-format change — including the open follow-ups recorded by
  earlier releases (LF-only serializer output, `helpLink`/`urlInfoAbout` on the dogfood profile, the
  relaxed JSON escaping).
- Migrating the app off CommunityToolkit's source generators.
- Any dependency or TFM change.
