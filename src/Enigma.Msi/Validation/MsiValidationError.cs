using System;

namespace Enigma.Msi.Validation;

/// <summary>
/// A single validation violation: which member is wrong, and why.
/// </summary>
public sealed class MsiValidationError
{
    /// <summary>Creates a violation.</summary>
    /// <param name="path">
    /// The offending member's path, in the JSON casing of the profile format — e.g.
    /// <c>install.releasePath</c> or <c>shortcuts[0].targetPath</c>.
    /// </param>
    /// <param name="message">Why the value is rejected.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    public MsiValidationError(string path, string message)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>
    /// The offending member's path, in the JSON casing of the profile format (e.g.
    /// <c>output.msiFilename</c>), so a UI can map the violation back to the field that produced it.
    /// </summary>
    public string Path { get; }

    /// <summary>Why the value is rejected.</summary>
    public string Message { get; }

    /// <summary>Renders the violation as <c>path: message</c>.</summary>
    /// <returns>The single-line form, as printed by the worker's CLI mode.</returns>
    public override string ToString() => $"{Path}: {Message}";
}
