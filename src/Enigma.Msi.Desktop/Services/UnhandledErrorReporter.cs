using System;
using Microsoft.Extensions.Logging;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Default <see cref="IUnhandledErrorReporter"/>: logs every exception the hooks catch, in full, with where
/// it was caught.
/// </summary>
/// <remarks>
/// The log is written at once, on whatever thread reported, because that thread may be about to end the
/// process. A cancellation someone asked for — or any cancellation once the application is closing — is
/// not an error, and is logged at debug level only.
/// </remarks>
public sealed class UnhandledErrorReporter : IUnhandledErrorReporter
{
    private readonly UnhandledErrorPolicy _policy;
    private readonly ILogger<UnhandledErrorReporter> _logger;

    /// <summary>Creates the reporter.</summary>
    /// <param name="time">Measures how long ago an error was seen.</param>
    /// <param name="logger">Receives every exception, in full.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UnhandledErrorReporter(TimeProvider time, ILogger<UnhandledErrorReporter> logger)
    {
        _policy = new UnhandledErrorPolicy(time ?? throw new ArgumentNullException(nameof(time)));
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
    }

    /// <inheritdoc />
    public void ShutdownStarted() => _policy.IsShuttingDown = true;
}
