using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;
using Enigma.Msi.Validation;

namespace Enigma.Msi.Build;

/// <summary>
/// The default <see cref="IMsiBuildService"/>: writes the package to a request file, runs the worker
/// over it, streams the worker's output as it arrives, and reads the outcome back from the result file.
/// </summary>
/// <remarks>
/// <para>
/// Every temporary file lives in a directory of its own under the system temp folder, which also
/// becomes the worker's working directory — so whatever intermediate files the WiX toolchain leaves
/// behind are swept away with it rather than accumulating next to the worker or the host application.
/// </para>
/// <para>
/// Stateless and thread-safe once constructed; concurrent builds do not share anything but the options.
/// </para>
/// </remarks>
public sealed class MsiBuildService : IMsiBuildService
{
    /// <summary>Name of the request file inside a build's temporary directory.</summary>
    private const string RequestFileName = "request.json";

    /// <summary>Name of the result file inside a build's temporary directory.</summary>
    private const string ResultFileName = "result.json";

    /// <summary>
    /// How long the pre-flight check waits for the WiX CLI to answer <c>--version</c> before giving up
    /// and reporting it as unavailable. A pre-flight that hangs is worse than one that says "no".
    /// </summary>
    private static readonly TimeSpan WixProbeTimeout = TimeSpan.FromSeconds(30);

    private readonly MsiBuildServiceOptions _options;
    private readonly IMsiPackageValidator _validator = new MsiPackageValidator();

    /// <summary>Creates the service.</summary>
    /// <param name="options">
    /// Where to find the worker and the WiX CLI. When <see langword="null"/>, defaults apply: the
    /// worker is discovered beside the host application and the WiX CLI is resolved through
    /// <c>PATH</c>.
    /// </param>
    public MsiBuildService(MsiBuildServiceOptions? options = null)
        => _options = options ?? new MsiBuildServiceOptions();

    /// <summary>
    /// Where the worker is discovered when <see cref="MsiBuildServiceOptions.WorkerPath"/> is not set:
    /// the <c>worker</c> folder beside the host application, which is where
    /// <c>build/CopyWorkerOutput.targets</c> copies it from the worker project's build output.
    /// </summary>
    public static string DefaultWorkerPath { get; } = Path.Combine(
        AppContext.BaseDirectory,
        MsiBuildServiceOptions.WorkerFolderName,
        MsiBuildServiceOptions.WorkerFileName);

    /// <inheritdoc />
    public async Task<MsiPrerequisites> CheckPrerequisitesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var problems = new List<string>();

        string? workerPath = ResolveWorkerPath();
        if (workerPath is null)
        {
            problems.Add(DescribeMissingWorker());
        }

        string? wixVersion = await ProbeWixToolAsync(cancellationToken).ConfigureAwait(false);
        if (wixVersion is null)
        {
            problems.Add(
                $"The WiX CLI ('{_options.WixToolPath}') could not be run, so no MSI can be produced. "
                + "Install it with: dotnet tool install --global wix");
        }

        return new MsiPrerequisites
        {
            WorkerPath = workerPath,
            WixToolVersion = wixVersion,
            Problems = problems
        };
    }

    /// <inheritdoc />
    public async Task<MsiBuildResult> BuildAsync(
        MsiPackage package,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Validate before spawning anything: a package that cannot build should cost the user a
        // millisecond and a full list of violations, not a process launch and a one-line failure. The
        // worker re-validates for its own sake — it is a supported entry point on its own.
        MsiValidationResult validation = _validator.ValidateAll(package);
        if (!validation.IsValid)
        {
            return MsiBuildResult.Failed(validation.Errors.Select(error => error.ToString()));
        }

        string? workerPath = ResolveWorkerPath();
        if (workerPath is null)
        {
            return MsiBuildResult.Failed(DescribeMissingWorker());
        }

        string workingDirectory = Path.Combine(
            Path.GetTempPath(),
            "enigma-msi-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(workingDirectory);

        try
        {
            string requestPath = Path.Combine(workingDirectory, RequestFileName);
            string resultPath = Path.Combine(workingDirectory, ResultFileName);

            File.WriteAllText(requestPath, MsiPackageJson.Serialize(package));

            int exitCode = await RunWorkerAsync(
                workerPath,
                workingDirectory,
                requestPath,
                resultPath,
                log,
                cancellationToken).ConfigureAwait(false);

            return ReadResult(resultPath, exitCode);
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    /// <summary>
    /// Finds the worker: the configured path when there is one, otherwise
    /// <see cref="DefaultWorkerPath"/>.
    /// </summary>
    /// <returns>The worker's path, or <see langword="null"/> when it is not there.</returns>
    private string? ResolveWorkerPath()
    {
        string candidate = string.IsNullOrWhiteSpace(_options.WorkerPath)
            ? DefaultWorkerPath
            : _options.WorkerPath!;

        return File.Exists(candidate) ? candidate : null;
    }

    /// <summary>
    /// Explains a failed discovery. The configured and the discovered case get different wording
    /// because they have different fixes — one is a wrong setting, the other a missing deployment step.
    /// </summary>
    private string DescribeMissingWorker()
        => string.IsNullOrWhiteSpace(_options.WorkerPath)
            ? $"The MSI worker was not found at '{DefaultWorkerPath}'. It ships beside the application "
              + $"in a '{MsiBuildServiceOptions.WorkerFolderName}' folder — the Enigma.Msi package copies it "
              + "there automatically; otherwise set MsiBuildServiceOptions.WorkerPath."
            : $"The MSI worker was not found at the configured path '{_options.WorkerPath}' "
              + "(MsiBuildServiceOptions.WorkerPath).";

    /// <summary>
    /// Runs the worker to completion, forwarding every line it writes to <paramref name="log"/>.
    /// </summary>
    /// <returns>The worker's exit code.</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> was signalled; the worker's process tree has been
    /// terminated.
    /// </exception>
    private static async Task<int> RunWorkerAsync(
        string workerPath,
        string workingDirectory,
        string requestPath,
        string resultPath,
        IProgress<string>? log,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = workerPath,
            // Both paths are ours (a GUID-named temp directory), so plain quoting is enough — neither
            // can contain a quote, and neither ends in a backslash.
            Arguments = $"--request \"{requestPath}\" --result \"{resultPath}\"",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var standardOutputClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var standardErrorClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.Exited += (_, _) => exited.TrySetResult(process.ExitCode);
        process.OutputDataReceived += (_, e) => Forward(e.Data, log, standardOutputClosed);
        process.ErrorDataReceived += (_, e) => Forward(e.Data, log, standardErrorClosed);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using (cancellationToken.Register(() => KillProcessTree(process)))
        {
            int exitCode = await exited.Task.ConfigureAwait(false);

            // Both streams must reach end-of-file before the result is read, otherwise the tail of the
            // build log can arrive after the caller has already been told the build finished.
            await Task.WhenAll(standardOutputClosed.Task, standardErrorClosed.Task).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            return exitCode;
        }
    }

    /// <summary>
    /// Hands one line of worker output to the log. The <see langword="null"/> line the
    /// <see cref="Process"/> stream reader emits at end-of-file is not output — it is the signal that
    /// the stream is drained.
    /// </summary>
    private static void Forward(string? line, IProgress<string>? log, TaskCompletionSource<bool> closed)
    {
        if (line is null)
        {
            closed.TrySetResult(true);
            return;
        }

        log?.Report(line);
    }

    /// <summary>
    /// Turns the worker's result file into an <see cref="MsiBuildResult"/>. Each way the file can let
    /// us down gets its own message: the caller's next move differs between "the worker never wrote
    /// one" and "what it wrote is not a result".
    /// </summary>
    private static MsiBuildResult ReadResult(string resultPath, int exitCode)
    {
        if (!File.Exists(resultPath))
        {
            return MsiBuildResult.Failed(
                $"The worker exited with code {exitCode} without writing a result file; "
                + "its output is in the build log.");
        }

        string json;

        try
        {
            json = File.ReadAllText(resultPath);
        }
        catch (Exception ex) when (IsFileAccessFailure(ex))
        {
            return MsiBuildResult.Failed($"The worker's result file could not be read: {ex.Message}");
        }

        try
        {
            return MsiPackageJson.DeserializeResult(json);
        }
        catch (JsonException ex)
        {
            return MsiBuildResult.Failed(
                $"The worker exited with code {exitCode} but its result file could not be read as a "
                + $"build result: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs <c>--version</c> on the configured WiX CLI.
    /// </summary>
    /// <returns>The version it reported, or <see langword="null"/> when it could not be run.</returns>
    private async Task<string?> ProbeWixToolAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.WixToolPath,
            Arguments = "--version",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => exited.TrySetResult(process.ExitCode);

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            // "Not installed" is the expected answer here, not an exceptional one: the whole point of a
            // pre-flight check is to report it as a problem the user can fix.
            return null;
        }

        // Both streams are drained so the child cannot block on a full pipe while we wait for it.
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource();
        using (cancellationToken.Register(() => KillProcessTree(process)))
        {
            if (await Task.WhenAny(exited.Task, Task.Delay(WixProbeTimeout, timeout.Token)).ConfigureAwait(false) != exited.Task)
            {
                KillProcessTree(process);
                return null;
            }

            timeout.Cancel();

            int exitCode = await exited.Task.ConfigureAwait(false);
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (exitCode != 0)
            {
                return null;
            }

            string version = outputTask.Result.Trim();

            return version.Length == 0 ? null : version;
        }
    }

    /// <summary>
    /// Terminates <paramref name="process"/> together with everything it started. The worker's children
    /// are the WiX toolchain, and leaving those running is exactly the orphaned-build problem this
    /// library exists to avoid.
    /// </summary>
    private static void KillProcessTree(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

#if NETSTANDARD2_0
            // Process.Kill(entireProcessTree:) is .NET Core 3.0+. On .NET Framework the equivalent is
            // taskkill, which walks the tree itself — and MSI building is Windows-only in every case.
            using var taskkill = Process.Start(new ProcessStartInfo("taskkill", $"/T /F /PID {process.Id}")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });

            taskkill?.WaitForExit((int)TimeSpan.FromSeconds(15).TotalMilliseconds);
#else
            process.Kill(entireProcessTree: true);
#endif
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException or AggregateException)
        {
            // The process, or one of its children, exited between the check and the kill. Racing with a
            // process that is dying anyway is the outcome we wanted, so there is nothing to report.
        }
    }

    /// <summary>Removes a build's temporary directory. Best effort — it is under the temp folder.</summary>
    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (IsFileAccessFailure(ex))
        {
        }
    }

    /// <summary>
    /// Whether <paramref name="exception"/> is one of the ways a file path can fail to be read, written
    /// or removed — as opposed to a defect, which must reach the caller.
    /// </summary>
    private static bool IsFileAccessFailure(Exception exception)
        => exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException;
}
