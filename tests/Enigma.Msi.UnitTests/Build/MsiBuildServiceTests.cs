using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Msi.Build;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;
using Xunit;

namespace Enigma.Msi.UnitTests.Build;

/// <summary>
/// Covers <see cref="MsiBuildService.BuildAsync"/> against a real worker process — the stub worker,
/// which speaks the same protocol and is scripted per test through a <c>stub.directive</c> file in the
/// package's release directory.
/// </summary>
public sealed class MsiBuildServiceTests : IDisposable
{
    private readonly string _root;
    private readonly string _releaseDirectory;
    private readonly string _outputDirectory;
    private readonly List<int> _spawned = [];

    public MsiBuildServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "enigma-msi-client-" + Guid.NewGuid().ToString("N"));
        _releaseDirectory = Path.Combine(_root, "release");
        _outputDirectory = Path.Combine(_root, "artifacts");

        Directory.CreateDirectory(_releaseDirectory);
        Directory.CreateDirectory(_outputDirectory);
    }

    public void Dispose()
    {
        // A test that fails mid-build must not leave a five-minute sleeper behind.
        foreach (int processId in _spawned)
        {
            StubWorker.KillIfRunning(processId);
        }

        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Build_WhenTheWorkerSucceeds_ReturnsItsResult()
    {
        MsiBuildResult result = await BuildAsync(CreatePackage("succeed"));

        Assert.True(result.Success);
        Assert.Equal(Path.Combine(_outputDirectory, "Widget.msi"), result.MsiPath);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Build_SendsThePackageToTheWorkerAsAMsipkgJsonDocument()
    {
        MsiPackage sent = CreatePackage("succeed");
        sent.AppName = "Round Trip";
        sent.Ui = UiSettings.CreateDefault();

        await BuildAsync(sent);

        string json = File.ReadAllText(Path.Combine(_releaseDirectory, "stub.request.json"));
        MsiPackage received = MsiPackageJson.Deserialize(json);

        Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
        Assert.Equal(sent.AppName, received.AppName);
        Assert.Equal(sent.Version, received.Version);
        Assert.Equal(sent.ProductId, received.ProductId);
        Assert.Equal(sent.UpgradeCode, received.UpgradeCode);
        Assert.Equal(sent.Install.ReleasePath, received.Install.ReleasePath);
        Assert.Equal(sent.Output.MsiFilename, received.Output.MsiFilename);
        Assert.Equal(sent.Ui!.InstallDialogs, received.Ui!.InstallDialogs);
    }

    [Fact]
    public async Task Build_PassesTheRequestAndResultPathsAsTheWorkerProtocolExpects()
    {
        StubWorker.Log log = new();

        await BuildAsync(CreatePackage("succeed"), log);

        string requestPath = StubWorker.ValueOf(log.Lines, "stub: request=");
        string resultPath = StubWorker.ValueOf(log.Lines, "stub: result=");

        Assert.Equal("request.json", Path.GetFileName(requestPath));
        Assert.Equal("result.json", Path.GetFileName(resultPath));
        // Both in a directory of their own, so concurrent builds cannot read each other's files.
        Assert.Equal(Path.GetDirectoryName(requestPath), Path.GetDirectoryName(resultPath));
    }

    [Fact]
    public async Task Build_StreamsBothOfTheWorkersStreamsIntoTheLog()
    {
        StubWorker.Log log = new();

        await BuildAsync(CreatePackage("succeed"), log);

        Assert.Contains(log.Lines, line => line.StartsWith("stub: request=", StringComparison.Ordinal));
        Assert.Contains("stub: this line came from stderr", log.Lines);
    }

    [Fact]
    public async Task Build_RemovesItsTemporaryDirectory()
    {
        StubWorker.Log log = new();

        await BuildAsync(CreatePackage("succeed"), log);

        string? workingDirectory = Path.GetDirectoryName(StubWorker.ValueOf(log.Lines, "stub: request="));

        Assert.NotNull(workingDirectory);
        Assert.False(Directory.Exists(workingDirectory), "The build's temporary directory was left behind.");
    }

    [Fact]
    public async Task Build_WhenTheWorkerReportsFailure_ReturnsEveryReason()
    {
        MsiBuildResult result = await BuildAsync(CreatePackage("fail"));

        Assert.False(result.Success);
        Assert.Null(result.MsiPath);
        Assert.Equal(
            new[] { "stub: the first reason it failed", "stub: the second reason it failed" },
            result.Errors);
    }

    [Fact]
    public async Task Build_WhenTheWorkerWritesNoResultFile_ReportsItsExitCode()
    {
        MsiBuildResult result = await BuildAsync(CreatePackage("no-result"));

        Assert.False(result.Success);
        string error = Assert.Single(result.Errors);
        Assert.Contains("exited with code 7", error, StringComparison.Ordinal);
        Assert.Contains("build log", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Build_WhenTheResultFileIsNotABuildResult_SaysSoRatherThanThrowing()
    {
        MsiBuildResult result = await BuildAsync(CreatePackage("invalid-result"));

        Assert.False(result.Success);
        Assert.Contains(
            "could not be read as a build result",
            Assert.Single(result.Errors),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Build_WhenTheConfiguredWorkerIsMissing_PointsAtTheSetting()
    {
        MsiBuildResult result = await BuildAsync(CreatePackage("succeed"), service: CreateServiceWithoutAWorker());

        Assert.False(result.Success);
        string error = Assert.Single(result.Errors);
        Assert.Contains(StubWorker.MissingExecutablePath, error, StringComparison.Ordinal);
        Assert.Contains("MsiBuildServiceOptions.WorkerPath", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Build_WhenNoWorkerIsConfigured_LooksInTheFolderBesideTheApplication()
    {
        // No WorkerPath: discovery falls back to AppContext.BaseDirectory/worker, where the test output
        // deliberately has nothing (the stub lives in stub-worker/).
        MsiBuildResult result = await BuildAsync(CreatePackage("succeed"), service: new MsiBuildService());

        Assert.False(result.Success);
        Assert.Contains(
            MsiBuildService.DefaultWorkerPath,
            Assert.Single(result.Errors),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Build_WithAnInvalidPackage_ReportsEveryViolationWithoutRunningAWorker()
    {
        MsiPackage package = CreatePackage("succeed");
        package.AppName = string.Empty;
        package.Install.ReleasePath = Path.Combine(_root, "no-such-folder");

        // A worker that could never run: the validation gate has to come first, or this would report a
        // missing worker instead of the two real problems.
        MsiBuildResult result = await BuildAsync(package, service: CreateServiceWithoutAWorker());

        Assert.False(result.Success);
        Assert.Equal(
            new[] { "appName: Required.", "install.releasePath: Directory does not exist." },
            result.Errors);
    }

    [Fact]
    public async Task Build_WithANullPackage_Throws()
        => await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateService().BuildAsync(null!, null, TestContext.Current.CancellationToken));

    [Fact]
    public async Task Build_WhenAlreadyCancelled_DoesNotStartAnything()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateService().BuildAsync(CreatePackage("succeed"), null, cancellation.Token));

        Assert.False(File.Exists(Path.Combine(_releaseDirectory, "stub.request.json")));
    }

    [Fact]
    public async Task Build_WhenCancelled_ThrowsAndTerminatesTheWorker()
    {
        (Task<MsiBuildResult> build, StubWorker.Log log, CancellationTokenSource cancellation) =
            await StartHangingBuildAsync("hang");

        using (cancellation)
        {
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => build);
            await StubWorker.AssertExitedAsync(StubWorker.ProcessIdOf(log.Lines, "stub: pid="));
        }
    }

    [Fact]
    public async Task Build_WhenCancelled_TerminatesTheWholeWorkerProcessTree()
    {
        (Task<MsiBuildResult> build, StubWorker.Log log, CancellationTokenSource cancellation) =
            await StartHangingBuildAsync("hang-with-child");

        using (cancellation)
        {
            int childProcessId = StubWorker.ProcessIdOf(log.Lines, "stub: child=");
            _spawned.Add(childProcessId);

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => build);
            await StubWorker.AssertExitedAsync(StubWorker.ProcessIdOf(log.Lines, "stub: pid="));
            // The point of the test: the WiX toolchain runs as the worker's children, and a cancelled
            // build that leaves those running is the orphaned-build problem this library exists to fix.
            await StubWorker.AssertExitedAsync(childProcessId);
        }
    }

    /// <summary>
    /// Starts a build the stub will never finish, and comes back once the worker has announced it is
    /// hanging — so the cancellation under test happens mid-build rather than racing process startup.
    /// </summary>
    private async Task<(Task<MsiBuildResult> Build, StubWorker.Log Log, CancellationTokenSource Cancellation)>
        StartHangingBuildAsync(string directive)
    {
        var hanging = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        StubWorker.Log log = new(line =>
        {
            if (line == "stub: hanging")
            {
                hanging.TrySetResult(true);
            }
        });

        var cancellation = new CancellationTokenSource();
        Task<MsiBuildResult> build = CreateService().BuildAsync(CreatePackage(directive), log, cancellation.Token);

        await hanging.Task.WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);
        _spawned.Add(StubWorker.ProcessIdOf(log.Lines, "stub: pid="));

        return (build, log, cancellation);
    }

    private static Task<MsiBuildResult> BuildAsync(
        MsiPackage package,
        IProgress<string>? log = null,
        MsiBuildService? service = null)
        => (service ?? CreateService()).BuildAsync(package, log, TestContext.Current.CancellationToken);

    private static MsiBuildService CreateService()
        => new(new MsiBuildServiceOptions { WorkerPath = StubWorker.ExecutablePath });

    private static MsiBuildService CreateServiceWithoutAWorker()
        => new(new MsiBuildServiceOptions { WorkerPath = StubWorker.MissingExecutablePath });

    /// <summary>
    /// A package whose paths point at this test's temporary directories, scripted with
    /// <paramref name="directive"/> — which doubles as the release directory's content, since an empty
    /// one would not pass validation.
    /// </summary>
    private MsiPackage CreatePackage(string directive)
    {
        File.WriteAllText(Path.Combine(_releaseDirectory, "stub.directive"), directive);

        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install.ReleasePath = _releaseDirectory;
        package.Output.OutputPath = _outputDirectory;

        return package;
    }
}
