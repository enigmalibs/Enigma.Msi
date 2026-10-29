using System;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// The safety net: every exception nothing else caught ends up here and is logged, in full, with where
/// it was caught.
/// </summary>
/// <remarks>
/// A safety net, not the error handling: a failure the application knows about — a profile that cannot
/// be read, a build that fails — is said where it happens, in that place's own words, and never reaches
/// this. <see cref="UnhandledExceptionHooks"/> is what feeds it.
/// </remarks>
public interface IUnhandledErrorReporter
{
    /// <summary>Logs an exception, from any thread.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="source">Where it was caught.</param>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    void Report(Exception exception, UnhandledErrorSource source);

    /// <summary>Says the application is closing: from here on a cancellation is never worth a word.</summary>
    void ShutdownStarted();
}
