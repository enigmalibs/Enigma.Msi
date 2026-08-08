using System;
using System.IO;
using System.Text.Json;
using Enigma.Msi.Build;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;

namespace Enigma.Msi.Worker;

/// <summary>
/// Entry point of the net472 worker — the only process in the solution that touches WixSharp. It is
/// spawned per build, either by the library's build client (<see cref="WorkerMode.Internal"/>) or
/// straight from a shell or CI job (<see cref="WorkerMode.Cli"/>).
/// </summary>
/// <remarks>
/// Deliberately not bootstrapped with <c>IHost</c>: a stateless one-shot process has no configuration,
/// logging, lifetime or DI needs. All WixSharp and WiX toolchain output goes to stdout/stderr, which is
/// what lets the caller stream it as a live build log.
/// </remarks>
internal static class Program
{
    /// <summary>The MSI was produced.</summary>
    private const int ExitSuccess = 0;

    /// <summary>The input was well-formed but the operation failed: validation, or the MSI build.</summary>
    private const int ExitOperationFailed = 1;

    /// <summary>Bad arguments, unreadable input, or an unexpected error.</summary>
    private const int ExitBadInput = 2;

    public static int Main(string[] args)
    {
        WorkerArguments? arguments = WorkerArguments.Parse(args, out string error);
        if (arguments is null)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(WorkerArguments.Usage);
            return ExitBadInput;
        }

        try
        {
            return arguments.Mode == WorkerMode.Internal
                ? RunInternal(arguments)
                : RunCli(arguments);
        }
        catch (Exception ex)
        {
            // Last resort: MsiBuildRunner already turns build failures into a failed result, so
            // anything arriving here is genuinely unexpected worker machinery. Report it on stderr and,
            // in internal mode, best-effort into the result file so the client is not left guessing.
            Console.Error.WriteLine(ex);

            if (arguments.Mode == WorkerMode.Internal)
            {
                TryWriteResult(arguments.ResultPath, MsiBuildResult.Failed(ex.ToString()));
            }

            return ExitBadInput;
        }
    }

    private static int RunInternal(WorkerArguments arguments)
    {
        MsiPackage? package = LoadPackage(arguments.RequestPath, "request file", out string error);
        if (package is null)
        {
            Console.Error.WriteLine(error);

            // Still write a result: an unreadable request is exit 2, but the client should be able to
            // show the reason rather than "the worker died".
            TryWriteResult(arguments.ResultPath, MsiBuildResult.Failed(error));
            return ExitBadInput;
        }

        MsiBuildResult result = MsiBuildRunner.Run(package);

        try
        {
            File.WriteAllText(arguments.ResultPath, MsiPackageJson.SerializeResult(result));
        }
        catch (Exception ex) when (IsFileAccessFailure(ex))
        {
            Console.Error.WriteLine($"The result file '{arguments.ResultPath}' could not be written: {ex.Message}");
            return ExitBadInput;
        }

        return result.Success ? ExitSuccess : ExitOperationFailed;
    }

    private static int RunCli(WorkerArguments arguments)
    {
        MsiPackage? package = LoadPackage(arguments.ProfilePath, "profile", out string error);
        if (package is null)
        {
            Console.Error.WriteLine(error);
            return ExitBadInput;
        }

        MsiBuildResult result = MsiBuildRunner.Run(package);

        if (result.Success)
        {
            Console.Out.WriteLine($"MSI written to {result.MsiPath}");
            return ExitSuccess;
        }

        // Every reason, never just the first — that is the whole point of the aggregating validator.
        Console.Error.WriteLine($"The build failed with {result.Errors.Count} error(s):");

        foreach (string reason in result.Errors)
        {
            Console.Error.WriteLine($"  {reason}");
        }

        return ExitOperationFailed;
    }

    /// <summary>
    /// Reads and deserializes a package file.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="description">What the file is, for the error message ("request file", "profile").</param>
    /// <param name="error">On failure, why the file could not be turned into a package.</param>
    /// <returns>The package, or <see langword="null"/> on failure.</returns>
    private static MsiPackage? LoadPackage(string path, string description, out string error)
    {
        try
        {
            error = string.Empty;
            return MsiPackageJson.Deserialize(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            error = $"The {description} '{path}' is not a valid .msipkg.json document: {ex.Message}";
            return null;
        }
        catch (Exception ex) when (IsFileAccessFailure(ex))
        {
            error = $"The {description} '{path}' could not be read: {ex.Message}";
            return null;
        }
    }

    private static void TryWriteResult(string path, MsiBuildResult result)
    {
        // Best effort by design: the reason is already on stderr, so failing to also persist it changes
        // nothing the caller can act on.
        try
        {
            File.WriteAllText(path, MsiPackageJson.SerializeResult(result));
        }
        catch (Exception ex) when (IsFileAccessFailure(ex))
        {
        }
    }

    /// <summary>
    /// Whether <paramref name="exception"/> is one of the ways a file path can fail to be read or
    /// written — as opposed to a defect, which must reach the top-level handler.
    /// </summary>
    private static bool IsFileAccessFailure(Exception exception)
        => exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException;
}
