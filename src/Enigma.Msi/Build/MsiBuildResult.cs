using System;
using System.Collections.Generic;

namespace Enigma.Msi.Build;

/// <summary>
/// Outcome of a build attempt. Structured on purpose (rather than a tuple or a single message
/// string) so a failure can carry <em>every</em> reason at once — the worker writes this type to its
/// result file and the build client reads it back.
/// </summary>
public sealed class MsiBuildResult
{
    /// <summary>Whether the MSI was produced.</summary>
    public bool Success { get; set; }

    /// <summary>
    /// Full path of the generated MSI when <see cref="Success"/> is <see langword="true"/>;
    /// otherwise <see langword="null"/>.
    /// </summary>
    public string? MsiPath { get; set; }

    /// <summary>
    /// Every reason the build did not succeed — validation violations, or messages from the MSI build
    /// itself. Empty when <see cref="Success"/> is <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<string> Errors { get; set; } = [];

    /// <summary>Creates a successful result for the MSI at <paramref name="msiPath"/>.</summary>
    /// <param name="msiPath">Full path of the generated MSI.</param>
    /// <returns>A successful <see cref="MsiBuildResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="msiPath"/> is <see langword="null"/>.</exception>
    public static MsiBuildResult Succeeded(string msiPath) => new()
    {
        Success = true,
        MsiPath = msiPath ?? throw new ArgumentNullException(nameof(msiPath))
    };

    /// <summary>Creates a failed result carrying a single reason.</summary>
    /// <param name="error">Why the build failed.</param>
    /// <returns>A failed <see cref="MsiBuildResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static MsiBuildResult Failed(string error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new MsiBuildResult { Success = false, Errors = new List<string> { error } };
    }

    /// <summary>Creates a failed result carrying every reason.</summary>
    /// <param name="errors">Why the build failed.</param>
    /// <returns>A failed <see cref="MsiBuildResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    public static MsiBuildResult Failed(IEnumerable<string> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        return new MsiBuildResult { Success = false, Errors = new List<string>(errors) };
    }
}
