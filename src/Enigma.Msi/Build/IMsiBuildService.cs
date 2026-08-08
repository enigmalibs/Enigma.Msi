using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Msi.Model;

namespace Enigma.Msi.Build;

/// <summary>
/// Builds an MSI from an <see cref="MsiPackage"/> by driving the out-of-process worker.
/// </summary>
/// <remarks>
/// The split exists because WixSharp is .NET Framework-only: the worker is a <c>net472</c> executable
/// discovered on disk, handed the package as a JSON request file and answered with a JSON result file.
/// Consumers never see any of that — but they do see its consequences, which is why the interface
/// exposes <see cref="CheckPrerequisitesAsync"/>: a missing worker or a missing WiX CLI is a
/// configuration problem worth reporting before a build, not a mysterious build failure after one.
/// </remarks>
public interface IMsiBuildService
{
    /// <summary>
    /// Checks everything a build depends on outside the model itself — the worker executable and the
    /// WiX CLI — without building anything.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>What was found, and every unmet prerequisite phrased as an actionable message.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was signalled.</exception>
    Task<MsiPrerequisites> CheckPrerequisitesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates <paramref name="package"/>, then builds it through the worker.
    /// </summary>
    /// <param name="package">The package to build.</param>
    /// <param name="log">
    /// Receives the worker's output line by line as it arrives — the live build log. Called from a
    /// background thread, and must not throw.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels the build. Cancellation terminates the worker <em>and its children</em>, so a cancelled
    /// build leaves no orphaned WiX processes behind.
    /// </param>
    /// <returns>
    /// The outcome. A package that fails validation, an undiscoverable worker, and a failed MSI build
    /// all come back as a failed <see cref="MsiBuildResult"/> carrying every reason — only genuinely
    /// exceptional conditions throw.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="package"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> was signalled. The worker has been terminated by the time
    /// this is thrown.
    /// </exception>
    Task<MsiBuildResult> BuildAsync(
        MsiPackage package,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default);
}
