# Security Policy

Enigma.Msi builds Windows Installer packages, and an MSI is code that runs with elevated privileges on
every machine it is installed on — a per-machine install executes as `SYSTEM`. A defect in how a package
is authored, in what ends up embedded in the `.msi`, or in how the out-of-process worker is located and
launched can therefore affect every machine the resulting installer touches, not just the build host.
Vulnerability reports are taken seriously and handled with priority.

## Supported versions

Security fixes are provided for the latest released version of each artifact. The two version
independently — the **Enigma.Msi** library (NuGet) and the **Enigma.Msi.Desktop** application (MSI) — so
they are listed separately. Both follow [Semantic Versioning](https://semver.org/), and users are
encouraged to stay current with the newest release.

| Artifact                              | Version | Supported          |
|---------------------------------------|---------|--------------------|
| Enigma.Msi (library, NuGet)           | 1.0.x   | :white_check_mark: |
| Enigma.Msi.Desktop (application, MSI) | 1.2.x   | :white_check_mark: |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions, or pull
requests.** Public disclosure before a fix is available puts every user at risk.

Instead, use **GitHub's private vulnerability reporting**:

1. Go to the repository's **Security** tab.
2. Select **Report a vulnerability** to open a private advisory.
3. Include as much detail as you can — the affected version, the component involved, a description
   of the issue, and, where possible, a minimal reproduction and its impact.

This keeps the report private between you and the maintainers while it is triaged and fixed.

## What to expect

- Your report will be acknowledged and triaged as promptly as possible.
- The issue will be investigated and, once confirmed, a fix prepared and released.
- Coordinated disclosure is preferred: please allow a reasonable period for a fix to ship before
  any public discussion of the vulnerability.
- Your contribution will be credited in the resulting advisory unless you ask to remain anonymous.

## Scope

Reports concerning the public API surface of the **Enigma.Msi** library, the `.msipkg.json` profile
handling, the bundled **Enigma.Msi.Worker** executable (including how it is discovered, launched and
handed its request file), the packaging plumbing that redistributes it, and the **Enigma.Msi.Desktop**
application are in scope. Because Enigma.Msi builds on **WixSharp** and the **WiX Toolset**, issues
rooted in those underlying projects should also be reported upstream to WixSharp and the WiX Toolset
respectively.
