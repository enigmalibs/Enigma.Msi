using System.Collections.Generic;

namespace Enigma.Msi.Build;

/// <summary>
/// What a build needs before it can run: the worker executable, and the WiX CLI the worker drives.
/// Reported as data rather than thrown, so a UI can show the problems next to a disabled Build button
/// instead of failing the user's first build with console noise.
/// </summary>
public sealed class MsiPrerequisites
{
    /// <summary>
    /// Full path of the worker executable that was found, or <see langword="null"/> when discovery
    /// failed — in which case <see cref="Problems"/> says where it was looked for.
    /// </summary>
    public string? WorkerPath { get; init; }

    /// <summary>
    /// What the WiX CLI reported for <c>--version</c> (e.g. <c>7.0.0</c>), or <see langword="null"/>
    /// when it could not be run.
    /// </summary>
    public string? WixToolVersion { get; init; }

    /// <summary>
    /// Every unmet prerequisite, each phrased as something the user can act on. Empty when
    /// <see cref="IsSatisfied"/> is <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<string> Problems { get; init; } = [];

    /// <summary>Whether the worker executable was found.</summary>
    public bool WorkerFound => WorkerPath is not null;

    /// <summary>Whether the WiX CLI ran and reported a version.</summary>
    public bool WixToolAvailable => WixToolVersion is not null;

    /// <summary>Whether a build can be started — that is, <see cref="Problems"/> is empty.</summary>
    public bool IsSatisfied => Problems.Count == 0;
}
