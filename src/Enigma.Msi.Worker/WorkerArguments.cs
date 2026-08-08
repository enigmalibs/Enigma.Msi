using System;
using System.Collections.Generic;

namespace Enigma.Msi.Worker;

/// <summary>
/// The worker's command line, parsed strictly: anything the two documented forms do not describe is
/// rejected with a reason instead of ignored.
/// </summary>
/// <remarks>
/// Strictness is the point. The predecessor project's parser scanned for the options it recognized and
/// silently dropped everything else, so a typo (<c>--results</c>) produced a build that looked fine and
/// wrote its outcome nowhere. Here a typo is an exit-code-2 usage error.
/// </remarks>
public sealed class WorkerArguments
{
    /// <summary>The <c>build</c> verb that selects <see cref="WorkerMode.Cli"/>.</summary>
    private const string BuildCommand = "build";

    /// <summary>Option naming the request file in <see cref="WorkerMode.Internal"/>.</summary>
    private const string RequestOption = "--request";

    /// <summary>Option naming the result file in <see cref="WorkerMode.Internal"/>.</summary>
    private const string ResultOption = "--result";

    private WorkerArguments(WorkerMode mode, string requestPath, string resultPath, string profilePath)
    {
        Mode = mode;
        RequestPath = requestPath;
        ResultPath = resultPath;
        ProfilePath = profilePath;
    }

    /// <summary>Both documented command-line forms, ready to print alongside a usage error.</summary>
    public static string Usage { get; } = string.Join(
        Environment.NewLine,
        "Usage:",
        $"  Enigma.Msi.Worker.exe {RequestOption} <request.json> {ResultOption} <result.json>",
        $"  Enigma.Msi.Worker.exe {BuildCommand} <file.msipkg.json>");

    /// <summary>Which entry point the command line selected.</summary>
    public WorkerMode Mode { get; }

    /// <summary>
    /// The request file to read in <see cref="WorkerMode.Internal"/>; empty in
    /// <see cref="WorkerMode.Cli"/>.
    /// </summary>
    public string RequestPath { get; }

    /// <summary>
    /// The result file to write in <see cref="WorkerMode.Internal"/>; empty in
    /// <see cref="WorkerMode.Cli"/>.
    /// </summary>
    public string ResultPath { get; }

    /// <summary>
    /// The <c>.msipkg.json</c> profile to build in <see cref="WorkerMode.Cli"/>; empty in
    /// <see cref="WorkerMode.Internal"/>.
    /// </summary>
    public string ProfilePath { get; }

    /// <summary>
    /// Parses a command line.
    /// </summary>
    /// <param name="args">The raw arguments, as handed to <c>Main</c>.</param>
    /// <param name="error">
    /// On failure, why the command line was rejected — a single line, meant to be printed above
    /// <see cref="Usage"/>. Empty on success.
    /// </param>
    /// <returns>The parsed arguments, or <see langword="null"/> when <paramref name="args"/> was rejected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
    public static WorkerArguments? Parse(IReadOnlyList<string> args, out string error)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (args.Count == 0)
        {
            error = "No arguments were given.";
            return null;
        }

        return args[0] == BuildCommand
            ? ParseCli(args, out error)
            : ParseInternal(args, out error);
    }

    private static WorkerArguments? ParseCli(IReadOnlyList<string> args, out string error)
    {
        if (args.Count != 2)
        {
            error = $"The '{BuildCommand}' command takes exactly one argument, the profile to build; got {args.Count - 1}.";
            return null;
        }

        string profilePath = args[1];

        if (string.IsNullOrWhiteSpace(profilePath))
        {
            error = $"The '{BuildCommand}' command was given an empty profile path.";
            return null;
        }

        if (LooksLikeOption(profilePath))
        {
            error = $"Expected a profile path after '{BuildCommand}', but got the option '{profilePath}'.";
            return null;
        }

        error = string.Empty;
        return new WorkerArguments(WorkerMode.Cli, string.Empty, string.Empty, profilePath);
    }

    private static WorkerArguments? ParseInternal(IReadOnlyList<string> args, out string error)
    {
        string? requestPath = null;
        string? resultPath = null;

        for (int i = 0; i < args.Count; i++)
        {
            string token = args[i];

            switch (token)
            {
                case RequestOption:
                    if (!TryTakeValue(args, ref i, RequestOption, ref requestPath, out error))
                    {
                        return null;
                    }

                    break;

                case ResultOption:
                    if (!TryTakeValue(args, ref i, ResultOption, ref resultPath, out error))
                    {
                        return null;
                    }

                    break;

                default:
                    error = $"Unknown argument '{token}'.";
                    return null;
            }
        }

        if (requestPath is null)
        {
            error = $"Missing required option '{RequestOption}'.";
            return null;
        }

        if (resultPath is null)
        {
            error = $"Missing required option '{ResultOption}'.";
            return null;
        }

        error = string.Empty;
        return new WorkerArguments(WorkerMode.Internal, requestPath, resultPath, string.Empty);
    }

    /// <summary>
    /// Consumes the value that follows <paramref name="option"/>, advancing <paramref name="index"/>
    /// past it.
    /// </summary>
    private static bool TryTakeValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        ref string? destination,
        out string error)
    {
        if (destination is not null)
        {
            error = $"Option '{option}' was given more than once.";
            return false;
        }

        if (index + 1 >= args.Count)
        {
            error = $"Option '{option}' needs a file path.";
            return false;
        }

        string value = args[index + 1];

        // A following option means the value was forgotten, e.g. "--request --result out.json": taking
        // it verbatim would produce a build whose request path is "--result".
        if (string.IsNullOrWhiteSpace(value) || LooksLikeOption(value))
        {
            error = $"Option '{option}' needs a file path, but was followed by '{value}'.";
            return false;
        }

        destination = value;
        index++;
        error = string.Empty;
        return true;
    }

    private static bool LooksLikeOption(string token)
        => token.StartsWith("-", StringComparison.Ordinal);
}
