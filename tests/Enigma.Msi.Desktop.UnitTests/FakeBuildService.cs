using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Msi.Build;
using Enigma.Msi.Model;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// A hand-written <see cref="IMsiBuildService"/> rather than a substitute: the interesting cases —
/// streaming lines through the <see cref="IProgress{T}"/> before returning, and blocking until the
/// caller's token is cancelled — need a real method body, which a substitute's synchronous return
/// value cannot provide.
/// </summary>
internal sealed class FakeBuildService : IMsiBuildService
{
    /// <summary>What the pre-flight check reports. Satisfied by default.</summary>
    public MsiPrerequisites Prerequisites { get; set; } = new()
    {
        WorkerPath = @"C:\app\worker\Enigma.Msi.Worker.exe",
        WixToolVersion = "7.0.0"
    };

    /// <summary>What the build returns once it completes.</summary>
    public MsiBuildResult Result { get; set; } = MsiBuildResult.Succeeded(@"C:\out\Widget.msi");

    /// <summary>Lines reported through the progress sink before the build returns.</summary>
    public IReadOnlyList<string> LogLines { get; set; } = [];

    /// <summary>
    /// When set, the build never completes on its own — it waits for the cancellation token, the way a
    /// real worker run does.
    /// </summary>
    public bool WaitForCancellation { get; set; }

    /// <summary>Completes as soon as a build has started and reported its lines.</summary>
    public TaskCompletionSource BuildStarted { get; } = new();

    /// <summary>How many builds were started.</summary>
    public int BuildCount { get; private set; }

    /// <summary>The package handed to the most recent build.</summary>
    public MsiPackage? LastPackage { get; private set; }

    /// <inheritdoc />
    public Task<MsiPrerequisites> CheckPrerequisitesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Prerequisites);

    /// <inheritdoc />
    public async Task<MsiBuildResult> BuildAsync(
        MsiPackage package,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        BuildCount++;
        LastPackage = package;

        foreach (string line in LogLines)
        {
            log?.Report(line);
        }

        BuildStarted.TrySetResult();

        if (WaitForCancellation)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }

        return Result;
    }
}
