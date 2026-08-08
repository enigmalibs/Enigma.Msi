using System;
using System.Collections.Generic;

namespace Enigma.Msi.Validation;

/// <summary>
/// The outcome of validating a package: <em>every</em> violation found, never just the first one.
/// </summary>
public sealed class MsiValidationResult
{
    /// <summary>A result with no violations.</summary>
    public static MsiValidationResult Valid { get; } = new(new List<MsiValidationError>());

    /// <summary>Creates a result carrying <paramref name="errors"/>.</summary>
    /// <param name="errors">The violations found; empty for a valid package.</param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    public MsiValidationResult(IEnumerable<MsiValidationError> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        Errors = new List<MsiValidationError>(errors);
    }

    /// <summary>Every violation found, in declaration order of the members they belong to.</summary>
    public IReadOnlyList<MsiValidationError> Errors { get; }

    /// <summary>Whether the package passed — that is, <see cref="Errors"/> is empty.</summary>
    public bool IsValid => Errors.Count == 0;
}
