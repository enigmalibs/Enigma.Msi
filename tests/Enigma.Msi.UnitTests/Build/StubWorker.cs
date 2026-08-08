using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Enigma.Msi.UnitTests.Build;

/// <summary>
/// Access to the stub worker executable the build-client tests drive, plus the small helpers they all
/// need in order to work with a real child process.
/// </summary>
/// <remarks>
/// The stub is a project of its own (<c>tests/Enigma.Msi.StubWorker</c>), copied next to the test output
/// by <c>build/CopyWorkerOutput.targets</c> — the same target that will place the real worker beside the
/// Desktop app. Testing the build client against a real process is deliberate: process startup, stream
/// pumping, exit codes and killing a process tree are precisely the parts an abstraction over
/// <see cref="Process"/> would have mocked away.
/// </remarks>
internal static class StubWorker
{
    /// <summary>Full path of the stub worker executable.</summary>
    public static string ExecutablePath { get; } = Path.Combine(
        AppContext.BaseDirectory,
        "stub-worker",
        "Enigma.Msi.StubWorker.exe");

    /// <summary>A path where no executable lives, for the worker-not-found paths.</summary>
    public static string MissingExecutablePath { get; } = Path.Combine(
        Path.GetTempPath(),
        "enigma-msi-no-such-worker-" + Guid.NewGuid().ToString("N"),
        "Enigma.Msi.Worker.exe");

    /// <summary>
    /// Reads the value the stub announced for <paramref name="prefix"/> (e.g. <c>stub: pid=</c>).
    /// </summary>
    public static string ValueOf(IReadOnlyList<string> log, string prefix)
    {
        string line = Assert.Single(log, l => l.StartsWith(prefix, StringComparison.Ordinal));

        return line.Substring(prefix.Length);
    }

    /// <summary>Reads a process id the stub announced.</summary>
    public static int ProcessIdOf(IReadOnlyList<string> log, string prefix)
        => int.Parse(ValueOf(log, prefix), CultureInfo.InvariantCulture);

    /// <summary>
    /// Waits for a process to disappear. Killing is asynchronous — <see cref="Process.Kill(bool)"/>
    /// returns before Windows has finished tearing the tree down — so the assertion polls rather than
    /// sampling once and flaking.
    /// </summary>
    public static async Task AssertExitedAsync(int processId)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (HasExited(processId))
            {
                return;
            }

            await Task.Delay(50, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        Assert.Fail($"Process {processId} was still running 30 seconds after the build was cancelled.");
    }

    /// <summary>Terminates a process if it is still running — cleanup after a test that went wrong.</summary>
    public static void KillIfRunning(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);

            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Already gone, which is the state we wanted it in.
        }
    }

    private static bool HasExited(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);

            return process.HasExited;
        }
        catch (ArgumentException)
        {
            // No process carries that id any more: it exited and was reaped.
            return true;
        }
    }

    /// <summary>
    /// Collects the build log, and optionally signals as each line arrives so a test can act the moment
    /// the worker reaches a known point.
    /// </summary>
    /// <remarks>
    /// Deliberately not <see cref="Progress{T}"/>: that posts through the synchronization context, so a
    /// test would be racing the thread pool to cancel a build at the right moment. This one reports
    /// straight through, on the thread the build client calls it from.
    /// </remarks>
    public sealed class Log : IProgress<string>
    {
        private readonly ConcurrentQueue<string> _lines = new();
        private readonly Action<string>? _onLine;

        public Log(Action<string>? onLine = null) => _onLine = onLine;

        /// <summary>Every line reported so far, in arrival order.</summary>
        public IReadOnlyList<string> Lines => _lines.ToArray();

        public void Report(string value)
        {
            _lines.Enqueue(value);
            _onLine?.Invoke(value);
        }
    }
}
