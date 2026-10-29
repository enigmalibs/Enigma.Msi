using System;
using System.Collections.Generic;
using Enigma.Msi.Desktop.Services;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>A safety net that only remembers what reached it, in order.</summary>
internal sealed class RecordingReporter : IUnhandledErrorReporter
{
    public List<(Exception Exception, UnhandledErrorSource Source)> Reports { get; } = [];

    public void Report(Exception exception, UnhandledErrorSource source) => Reports.Add((exception, source));

    public void ShutdownStarted()
    {
    }
}
