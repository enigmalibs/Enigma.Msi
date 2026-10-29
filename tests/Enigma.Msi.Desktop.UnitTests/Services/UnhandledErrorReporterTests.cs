using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Msi.Desktop.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Services;

/// <summary>
/// Covers what the safety net writes down and what it shows: every real error in full with where it was
/// caught, one dialog at a time with later errors counted into it, an error just seen not shown again, and
/// a dialog that fails turning the net into a log-only one rather than a loop.
/// </summary>
/// <remarks>
/// Posted work runs inline and the dialog is substituted, so each step is observable as it happens; a
/// dialog "stays open" until the test completes the task it returned.
/// </remarks>
public sealed class UnhandledErrorReporterTests
{
    private readonly ManualTimeProvider _time = new();
    private readonly IUnhandledErrorDialogService _dialog = Substitute.For<IUnhandledErrorDialogService>();
    private readonly RecordingLogger<UnhandledErrorReporter> _logger = new();

    public UnhandledErrorReporterTests() => _dialog.ShowAsync(Arg.Any<UnhandledErrorBatch>()).Returns(Task.CompletedTask);

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
    public void AnError_IsShownInTheDialog()
    {
        UnhandledErrorReporter reporter = CreateReporter();
        Exception error = new IOException("The disk is full.");

        reporter.Report(error, UnhandledErrorSource.Dispatcher);

        _ = _dialog.Received(1).ShowAsync(Arg.Is<UnhandledErrorBatch>(batch => batch.First == error));
    }

    [Fact]
    public void ACancellationSomeoneAskedFor_IsLoggedAtDebugLevelOnly_AndNotShown()
    {
        UnhandledErrorReporter reporter = CreateReporter();
        using CancellationTokenSource source = new();
        source.Cancel();

        reporter.Report(new OperationCanceledException(source.Token), UnhandledErrorSource.UnobservedTask);

        Assert.Equal(LogLevel.Debug, Assert.Single(_logger.Entries).Level);
        _ = _dialog.DidNotReceive().ShowAsync(Arg.Any<UnhandledErrorBatch>());
    }

    [Fact]
    public void OnceShutdownHasStarted_AnyCancellation_IsLoggedAtDebugLevelOnly()
    {
        UnhandledErrorReporter reporter = CreateReporter();

        reporter.ShutdownStarted();
        reporter.Report(new OperationCanceledException(), UnhandledErrorSource.UnobservedTask);

        Assert.Equal(LogLevel.Debug, Assert.Single(_logger.Entries).Level);
        _ = _dialog.DidNotReceive().ShowAsync(Arg.Any<UnhandledErrorBatch>());
    }

    [Fact]
    public void ErrorsWhileTheDialogIsOpen_AreCountedIntoIt_NotShownInAnotherOne()
    {
        TaskCompletionSource open = new();
        UnhandledErrorBatch? shown = null;
        _dialog.ShowAsync(Arg.Do<UnhandledErrorBatch>(batch => shown = batch)).Returns(open.Task);
        UnhandledErrorReporter reporter = CreateReporter();

        reporter.Report(new IOException("first"), UnhandledErrorSource.Dispatcher);
        reporter.Report(new IOException("second"), UnhandledErrorSource.Dispatcher);
        reporter.Report(new IOException("third"), UnhandledErrorSource.UnobservedTask);
        open.SetResult();

        _ = _dialog.Received(1).ShowAsync(Arg.Any<UnhandledErrorBatch>());
        Assert.Equal(2, shown!.More);
    }

    [Fact]
    public void TheSameErrorRightAfterTheDialogClosed_IsNotShownAgain_ButIsStillLogged()
    {
        UnhandledErrorReporter reporter = CreateReporter();

        reporter.Report(Thrown("periodic"), UnhandledErrorSource.Dispatcher);
        reporter.Report(Thrown("periodic"), UnhandledErrorSource.Dispatcher);

        _ = _dialog.Received(1).ShowAsync(Arg.Any<UnhandledErrorBatch>());
        Assert.Equal(2, _logger.Entries.FindAll(entry => entry.Level == LogLevel.Error).Count);
    }

    [Fact]
    public void ADifferentErrorAfterTheDialogClosed_IsShownInANewOne()
    {
        UnhandledErrorReporter reporter = CreateReporter();

        reporter.Report(Thrown("one"), UnhandledErrorSource.Dispatcher);
        reporter.Report(Thrown("another"), UnhandledErrorSource.Dispatcher);

        _ = _dialog.Received(2).ShowAsync(Arg.Any<UnhandledErrorBatch>());
    }

    [Fact]
    public void ADialogThatFails_IsLogged_AndLaterErrorsAreOnlyLogged()
    {
        _dialog.ShowAsync(Arg.Any<UnhandledErrorBatch>()).ThrowsAsync(new InvalidOperationException("No host."));
        UnhandledErrorReporter reporter = CreateReporter();

        reporter.Report(Thrown("one"), UnhandledErrorSource.Dispatcher);
        reporter.Report(Thrown("another"), UnhandledErrorSource.Dispatcher);

        _ = _dialog.Received(1).ShowAsync(Arg.Any<UnhandledErrorBatch>());
        Assert.Contains(_logger.Entries, entry => entry.Message.Contains("could not be shown", StringComparison.Ordinal));
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
        InlineUiDispatcher dispatcher = new();

        Assert.Equal("time", Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(null!, dispatcher, _dialog, _logger)).ParamName);
        Assert.Equal("dispatcher", Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(_time, null!, _dialog, _logger)).ParamName);
        Assert.Equal("dialog", Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(_time, dispatcher, null!, _logger)).ParamName);
        Assert.Equal("logger", Assert.Throws<ArgumentNullException>(() => new UnhandledErrorReporter(_time, dispatcher, _dialog, null!)).ParamName);
    }

    private UnhandledErrorReporter CreateReporter() => new(_time, new InlineUiDispatcher(), _dialog, _logger);

    // Thrown and caught in one place, so two calls with one message are the same error in two instances.
    private static InvalidOperationException Thrown(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }
}
