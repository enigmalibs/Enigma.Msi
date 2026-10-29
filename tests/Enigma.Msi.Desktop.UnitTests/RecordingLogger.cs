using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// A logger that keeps what it is given, so a test can assert the level, the exception and the message of
/// each entry.
/// </summary>
/// <typeparam name="T">The category.</typeparam>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, exception, formatter(state, exception)));
}
