# Enigma.Msi — Guides & Samples

Per-category guides for **Enigma.Msi**, the library that builds Windows MSI installers from a single
declarative model. It is the engine behind **Enigma.Msi.Desktop**, the app this repository releases — if
you are here to *use* the app rather than to write code against the model, [the desktop
app](desktop-app.md) and the repository [README](../../README.md) are the two pages you want.

You describe the package once as an `MsiPackage` — app identity, install scope, the folder to package,
shortcuts, Control Panel information, compression, managed-UI dialogs — and then hand that same object to
whichever part of the library you need: `IMsiPackageValidator` to find out what is wrong with it,
`IMsiBuildService` to build the `.msi`, `MsiPackageJson` to save it as a `.msipkg.json` profile. There is
deliberately no mirrored DTO layer and no fluent builder chain — the app, the profile format and the
worker all consume the very same type.

The library is **not published to any feed**: it lives in this repository and is consumed by
`ProjectReference`. See the README's *Using it* section.

Nothing here needs a container: `MsiPackageValidator` and `MsiBuildService` are created with `new` and are
stateless once constructed. Both sit behind `I*` interfaces, so they register just as happily as
singletons in a `Microsoft.Extensions.DependencyInjection` container — the library ships no
`AddEnigmaMsi()` of its own, on purpose.

Each guide follows the same shape — **what the category does → tables of the types and their members →
copy-pasteable usage samples → notes** — and every snippet targets the real public API.

**Building an MSI requires Windows and the WiX CLI** (`dotnet tool install --global wix`). The library
loads anywhere; the build does not.

## The model

- [The package model](model.md) — every `MsiPackage` field with its default and its rules, the four
  WixSharp-free enums, and the `.msipkg.json` profile format as the library actually writes it.
- [Validating a package](validation.md) — `IMsiPackageValidator`, the complete rule set, and why the
  in-memory rules are split from the ones that touch the disk.

## Building

- [Building an MSI](building.md) — `IMsiBuildService`, the pre-flight check, streaming the build log,
  cancellation, and how the out-of-process worker is discovered.
- [The worker CLI](worker-cli.md) — `Enigma.Msi.Worker.exe build <file.msipkg.json>`, the exit-code
  contract (`0` / `1` / `2`), and running it from a CI job with no library reference at all.

## The application

- [The desktop app](desktop-app.md) — the Avalonia app over the same model: the form, the validate/build
  cycle, the live log, and what a saved profile looks like coming out of it.
