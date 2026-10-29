using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Default <see cref="IUnhandledErrorReporter"/>: logs every exception the hooks catch, in full, with where
/// it was caught, and shows it in a dialog that says the application is still running.
/// </summary>
/// <remarks>
/// <para>
/// The log is written at once, on whatever thread reported, because that thread may be about to end the
/// process. A cancellation someone asked for — or any cancellation once the application is closing — is
/// not an error, and is logged at debug level only.
/// </para>
/// <para>
/// What is shown is decided on the UI thread by <see cref="UnhandledErrorPolicy"/>: one dialog at a time,
/// later errors counted into it, an error just seen not shown again. If the dialog itself fails, that is
/// logged and no dialog is tried again in this session: the net must not loop on itself.
/// </para>
/// </remarks>
public sealed class UnhandledErrorReporter : IUnhandledErrorReporter
{
    private readonly UnhandledErrorPolicy _policy;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUnhandledErrorDialogService _dialog;
    private readonly ILogger<UnhandledErrorReporter> _logger;

    private bool _presenting;
    private bool _broken;

    /// <summary>Creates the reporter.</summary>
    /// <param name="time">Measures how long ago an error was seen.</param>
    /// <param name="dispatcher">Takes the decision to show onto the UI thread.</param>
    /// <param name="dialog">Shows a batch of errors.</param>
    /// <param name="logger">Receives every exception, in full.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UnhandledErrorReporter(
        TimeProvider time,
        IUiDispatcher dispatcher,
        IUnhandledErrorDialogService dialog,
        ILogger<UnhandledErrorReporter> logger)
    {
        _policy = new UnhandledErrorPolicy(time ?? throw new ArgumentNullException(nameof(time)));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Report(Exception exception, UnhandledErrorSource source)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (_policy.IsIgnoredCancellation(exception))
        {
            _logger.LogDebug(exception, "A cancellation reached the safety net ({Source}); not an error", source);
            return;
        }

        _logger.LogError(exception, "An unhandled exception reached the safety net ({Source})", source);

        _dispatcher.Post(() => Offer(exception));
    }

    /// <inheritdoc />
    public void ShutdownStarted() => _policy.IsShuttingDown = true;

    private void Offer(Exception exception)
    {
        switch (_policy.Offer(exception))
        {
            case UnhandledErrorDecision.Queue:
                _ = PresentAsync();
                break;
            case UnhandledErrorDecision.Suppress:
                _logger.LogDebug("The same error was shown less than {Window} ago; it is not shown again", UnhandledErrorPolicy.RepeatWindow);
                break;
            default:
                break;
        }
    }

    // Shows batches one after another until none waits. Never throws: nothing awaits it.
    private async Task PresentAsync()
    {
        if (_presenting || _broken)
        {
            return;
        }

        _presenting = true;

        try
        {
            while (_policy.BeginShow() is { } batch)
            {
                try
                {
                    await _dialog.ShowAsync(batch).ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    // Logged, never reported: a dialog that cannot be shown would otherwise report itself
                    // again and again.
                    _broken = true;
                    _logger.LogError(exception, "The safety net's dialog could not be shown; later errors are only logged");
                    return;
                }
                finally
                {
                    _policy.EndShow();
                }
            }
        }
        finally
        {
            _presenting = false;
        }
    }
}
