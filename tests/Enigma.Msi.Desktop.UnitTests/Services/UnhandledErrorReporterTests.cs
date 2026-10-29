using System;
using System.IO;
using System.Threading;
using Enigma.Msi.Desktop.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Services;

/// <summary>
/// Covers what the safety net writes down: every real error in full with where it was caught, and a
/// cancellation that is no error at debug level only.
/// </summary>
public sealed class UnhandledErrorReporterTests
{
    private readonly ManualTimeProvider _time = new();
    private readonly RecordingLogger<UnhandledErrorReporter> _logger = new();

    [Fact]
    public void AnError_IsLoggedAsAnError_WithTheExceptionAndItsSource()
    {
        UnhandledErrorReporter reporter = CreateReporter();
        Exception error = new IOException("The disk is full.");

        reporter.Report(error, UnhandledErrorSource.Dispatcher);

        (LogLevel level, Exception? logged, string message) = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, level);
        Assert.Same(error, logged);
        Assert.Contains("Dispatcher", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACancellationSomeoneAskedFor_IsLoggedAtDebugLevelOnly()
    {
        UnhandledErrorReporter reporter = CreateReporter();
        using CancellationTokenSource source = new();
        source.Cancel();

        reporter.Report(new OperationCanceledException(source.Token), UnhandledErrorSource.UnobservedTask);

        Assert.Equal(LogLevel.Debug, Assert.Single(_logger.Entries).Level);
    }

    [Fact]
    public void OnceShutdownHasStarted_AnyCancellation_IsLoggedAtDebugLevelOnly()
    {
        UnhandledErrorReporter reporter = CreateReporter();

        reporter.ShutdownStarted();
        reporter.Report(new OperationCanceledException(), UnhandledErrorSource.UnobservedTask);

        Assert.Equal(LogLevel.Debug, Assert.Single(_logger.Entries).Level);
    }

    [Fact]
    public void Report_RejectsAMissingException()
    {
        UnhandledErrorReporter reporter = CreateReporter();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => reporter.Report(null!, UnhandledErrorSource.Dispatcher));

        Assert.Equal("exception", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsMissingArguments()
    {
        Assert.Equal("time", Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(null!, _logger)).ParamName);
        Assert.Equal("logger", Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(_time, null!)).ParamName);
    }

    private UnhandledErrorReporter CreateReporter() => new(_time, _logger);
}
