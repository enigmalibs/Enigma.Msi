using Enigma.Msi.Model;

namespace Enigma.Msi.Validation;

/// <summary>
/// Validates an <see cref="MsiPackage"/>, reporting <em>every</em> violation with the member path
/// that produced it.
/// </summary>
/// <remarks>
/// The split between <see cref="Validate"/> (pure, in-memory) and
/// <see cref="ValidateEnvironment"/> (touches the file system) is deliberate: a UI can run the
/// in-memory rules on every keystroke, while the disk rules run only when a build is about to start.
/// </remarks>
public interface IMsiPackageValidator
{
    /// <summary>
    /// Runs the in-memory rules only — required members, GUIDs, version range, file-name shape,
    /// dialog lists. Never touches the file system, so it is safe to call as the user types.
    /// </summary>
    /// <param name="package">The package to check.</param>
    /// <returns>Every in-memory violation found.</returns>
    MsiValidationResult Validate(MsiPackage package);

    /// <summary>
    /// Runs the environment rules only — the directories and files the package refers to must exist.
    /// Assumes the in-memory rules have been (or will be) run separately: members that are empty are
    /// skipped here rather than reported twice.
    /// </summary>
    /// <param name="package">The package to check.</param>
    /// <returns>Every environment violation found.</returns>
    MsiValidationResult ValidateEnvironment(MsiPackage package);

    /// <summary>
    /// Runs both rule sets — what a build must pass. In-memory violations come first.
    /// </summary>
    /// <param name="package">The package to check.</param>
    /// <returns>Every violation found.</returns>
    MsiValidationResult ValidateAll(MsiPackage package);
}
