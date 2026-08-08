using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Msi.Build;
using Xunit;

namespace Enigma.Msi.UnitTests.Build;

/// <summary>
/// Covers <see cref="MsiBuildService.CheckPrerequisitesAsync"/> — the check whose whole purpose is to
/// turn "the build mysteriously failed" into a sentence the user can act on before starting one.
/// </summary>
/// <remarks>
/// The stub worker doubles as the WiX CLI here: it answers <c>--version</c>, which is exactly what the
/// probe asks of the real tool.
/// </remarks>
public sealed class MsiPrerequisiteTests
{
    /// <summary>An executable that exists but answers <c>--version</c> with a non-zero exit code.</summary>
    private const string FailingTool = "where.exe";

    [Fact]
    public async Task Prerequisites_WhenBothAreInPlace_AreSatisfied()
    {
        MsiPrerequisites prerequisites = await CheckAsync(StubWorker.ExecutablePath, StubWorker.ExecutablePath);

        Assert.True(prerequisites.IsSatisfied);
        Assert.Empty(prerequisites.Problems);
        Assert.True(prerequisites.WorkerFound);
        Assert.Equal(StubWorker.ExecutablePath, prerequisites.WorkerPath);
        Assert.True(prerequisites.WixToolAvailable);
        Assert.Equal("9.9.9-stub", prerequisites.WixToolVersion);
    }

    [Fact]
    public async Task Prerequisites_WhenTheWorkerIsMissing_SayWhereItWasLookedFor()
    {
        MsiPrerequisites prerequisites = await CheckAsync(StubWorker.MissingExecutablePath, StubWorker.ExecutablePath);

        Assert.False(prerequisites.IsSatisfied);
        Assert.False(prerequisites.WorkerFound);
        Assert.Null(prerequisites.WorkerPath);
        Assert.Contains(
            StubWorker.MissingExecutablePath,
            Assert.Single(prerequisites.Problems),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Prerequisites_WhenNoWorkerIsConfigured_NameTheFolderBesideTheApplication()
    {
        MsiPrerequisites prerequisites = await CheckAsync(workerPath: null, StubWorker.ExecutablePath);

        Assert.False(prerequisites.WorkerFound);
        Assert.Contains(
            MsiBuildService.DefaultWorkerPath,
            Assert.Single(prerequisites.Problems),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Prerequisites_WhenTheWixCliIsNotInstalled_GiveTheInstallCommand()
    {
        MsiPrerequisites prerequisites = await CheckAsync(
            StubWorker.ExecutablePath,
            "enigma-msi-no-such-tool-" + Guid.NewGuid().ToString("N"));

        Assert.False(prerequisites.IsSatisfied);
        Assert.False(prerequisites.WixToolAvailable);
        Assert.Null(prerequisites.WixToolVersion);
        Assert.Contains(
            "dotnet tool install --global wix",
            Assert.Single(prerequisites.Problems),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Prerequisites_WhenTheWixCliFailsToReportAVersion_TreatItAsUnavailable()
    {
        MsiPrerequisites prerequisites = await CheckAsync(StubWorker.ExecutablePath, FailingTool);

        Assert.False(prerequisites.WixToolAvailable);
        Assert.Contains(
            "dotnet tool install --global wix",
            Assert.Single(prerequisites.Problems),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Prerequisites_WhenNothingIsInPlace_ReportBothProblemsAtOnce()
    {
        MsiPrerequisites prerequisites = await CheckAsync(
            StubWorker.MissingExecutablePath,
            "enigma-msi-no-such-tool-" + Guid.NewGuid().ToString("N"));

        Assert.False(prerequisites.IsSatisfied);
        Assert.Equal(2, prerequisites.Problems.Count);
    }

    [Fact]
    public async Task Prerequisites_WhenAlreadyCancelled_DoNotProbeAnything()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new MsiBuildService().CheckPrerequisitesAsync(cancellation.Token));
    }

    [Fact]
    public void Prerequisites_WithNoProblems_AreSatisfiedByDefault()
        => Assert.True(new MsiPrerequisites().IsSatisfied);

    private static Task<MsiPrerequisites> CheckAsync(string? workerPath, string wixToolPath)
        => new MsiBuildService(new MsiBuildServiceOptions
        {
            WorkerPath = workerPath,
            WixToolPath = wixToolPath
        }).CheckPrerequisitesAsync(TestContext.Current.CancellationToken);
}
