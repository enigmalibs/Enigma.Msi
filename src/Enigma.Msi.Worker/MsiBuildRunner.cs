using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.Msi.Build;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;
using Enigma.Msi.Worker.Translation;

namespace Enigma.Msi.Worker;

/// <summary>
/// Runs one build end to end: re-validate the package, translate it to WixSharp, produce the MSI, and
/// report the outcome as an <see cref="MsiBuildResult"/>. Both worker modes funnel through here, so the
/// headless CLI and the library's build client cannot drift apart.
/// </summary>
public static class MsiBuildRunner
{
    /// <summary>
    /// Validates and builds <paramref name="package"/>.
    /// </summary>
    /// <param name="package">The package to build.</param>
    /// <returns>
    /// A successful result carrying the MSI path, or a failed one carrying <em>every</em> validation
    /// violation (formatted as <c>path: message</c>) or the reason the MSI build failed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="package"/> is <see langword="null"/>.</exception>
    public static MsiBuildResult Run(MsiPackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        // Defence in depth: the library's build client validates before spawning the worker, but the
        // worker is also a supported entry point of its own (CLI mode, CI) and must never hand a
        // half-filled package to WixSharp.
        MsiValidationResult validation = new MsiPackageValidator().ValidateAll(package);
        if (!validation.IsValid)
        {
            return MsiBuildResult.Failed(validation.Errors.Select(error => error.ToString()));
        }

        return Build(package);
    }

    private static MsiBuildResult Build(MsiPackage package)
    {
        try
        {
            // WixSharp writes the toolchain's own output to stdout/stderr, which the caller streams as
            // a live build log; only the outcome comes back through the return value.
            string? msiPath = MsiProjectFactory.Create(package).BuildMsi();

            return msiPath is null
                ? MsiBuildResult.Failed("The MSI build produced no output file; the WiX toolchain's diagnostics are in the build log.")
                : MsiBuildResult.Succeeded(msiPath);
        }
        catch (Exception ex)
        {
            // Deliberately broad: WixSharp surfaces toolchain and authoring failures as whatever
            // exception type the underlying step threw, and the exit-code contract counts every one of
            // them as "the build failed on well-formed input" (exit 1), not as an unexpected worker
            // error (exit 2). Program's top-level handler still owns the latter.
            return MsiBuildResult.Failed(Describe(ex));
        }
    }

    private static IEnumerable<string> Describe(Exception exception)
    {
        // One line per exception in the chain: WixSharp likes to wrap the actionable message
        // (a missing wix CLI, a WiX authoring error) inside a generic outer exception.
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            yield return $"{current.GetType().Name}: {current.Message}";
        }
    }
}
