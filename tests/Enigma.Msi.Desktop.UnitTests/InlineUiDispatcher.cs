using System;
using Enigma.Msi.Desktop.Services;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// Runs posted work on the calling thread. What makes the log tests deterministic: the real dispatcher
/// would hand the line to Avalonia's UI thread, which no unit test has.
/// </summary>
internal sealed class InlineUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}
