using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Decides what the safety net shows: which exceptions are not errors at all, which wait for the dialog
/// already there, and what the details say. Nothing here touches a window, so it is tested without a
/// platform.
/// </summary>
/// <remarks>
/// <para>
/// One dialog at a time. An error that comes while one waits for its host, or while one is on screen, is
/// counted into it ("2 more errors occurred") rather than queued behind it: a loop of failures must not
/// open a dialog per failure. An error identical to one the reader closed less than
/// <see cref="RepeatWindow"/> ago is logged and not shown again.
/// </para>
/// <para>
/// Every member is called on the UI thread, except <see cref="IsShuttingDown"/>,
/// <see cref="IsIgnoredCancellation"/> and the static helpers, which any thread may read.
/// </para>
/// <para>
/// Enigma.GitClient's policy, without its credential redaction: nothing this app throws carries a secret.
/// The details still never include <see cref="Exception.Data"/>.
/// </para>
/// </remarks>
public sealed class UnhandledErrorPolicy
{
    /// <summary>How long an error the reader has seen is not shown again.</summary>
    public static readonly TimeSpan RepeatWindow = TimeSpan.FromMinutes(1);

    // How deep the details follow inner exceptions before they stop.
    private const int MaximumDepth = 8;

    private static readonly object Seen = new();

    private readonly TimeProvider _time;
    private readonly ConditionalWeakTable<Exception, object> _offered = new();
    private readonly Dictionary<string, long> _closedAt = new(StringComparer.Ordinal);
    private volatile bool _isShuttingDown;

    /// <summary>Creates the policy.</summary>
    /// <param name="time">Measures how long ago an error was seen.</param>
    /// <exception cref="ArgumentNullException"><paramref name="time"/> is <see langword="null"/>.</exception>
    public UnhandledErrorPolicy(TimeProvider time)
    {
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    /// <summary>Whether the application is closing, when no cancellation is worth a word.</summary>
    public bool IsShuttingDown
    {
        get => _isShuttingDown;
        set => _isShuttingDown = value;
    }

    /// <summary>The batch waiting to be shown, or <see langword="null"/>.</summary>
    public UnhandledErrorBatch? Pending { get; private set; }

    /// <summary>The batch on screen, or <see langword="null"/>.</summary>
    public UnhandledErrorBatch? Showing { get; private set; }

    /// <summary>
    /// Answers whether an exception is a cancellation that is no error: one while the application closes,
    /// or one whose own token was cancelled — a stop someone asked for. A cancellation nobody asked for,
    /// such as a timeout, is an error.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns><see langword="true"/> when it is not worth showing.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public bool IsIgnoredCancellation(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        return exception switch
        {
            OperationCanceledException cancelled => IsShuttingDown || cancelled.CancellationToken.IsCancellationRequested,
            AggregateException { InnerExceptions.Count: > 0 } aggregate => AllIgnored(aggregate),
            _ => false,
        };
    }

    /// <summary>Decides what an exception does.</summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The decision; on <see cref="UnhandledErrorDecision.Queue"/>, <see cref="Pending"/> holds it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public UnhandledErrorDecision Offer(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        // The same exception can reach two hooks: an awaited fault rethrown on the dispatcher whose task
        // the finaliser also finds unobserved.
        if (!_offered.TryAdd(exception, Seen))
        {
            return UnhandledErrorDecision.Ignore;
        }

        if (IsIgnoredCancellation(exception))
        {
            return UnhandledErrorDecision.Ignore;
        }

        if ((Showing ?? Pending) is { } open)
        {
            open.Add(exception);
            return UnhandledErrorDecision.Merge;
        }

        if (_closedAt.TryGetValue(SignatureOf(exception), out long closed) && _time.GetElapsedTime(closed) < RepeatWindow)
        {
            return UnhandledErrorDecision.Suppress;
        }

        Pending = new UnhandledErrorBatch(exception);
        return UnhandledErrorDecision.Queue;
    }

    /// <summary>Takes the pending batch on screen.</summary>
    /// <returns>The batch, or <see langword="null"/> when none was waiting.</returns>
    public UnhandledErrorBatch? BeginShow()
    {
        if (Pending is not { } batch)
        {
            return null;
        }

        Pending = null;
        Showing = batch;

        return batch;
    }

    /// <summary>
    /// The batch on screen was closed: each of its errors is not shown again for <see cref="RepeatWindow"/>.
    /// </summary>
    public void EndShow()
    {
        if (Showing is not { } batch)
        {
            return;
        }

        Showing = null;

        long now = _time.GetTimestamp();

        foreach (Exception error in batch.Errors)
        {
            _closedAt[SignatureOf(error)] = now;
        }
    }

    /// <summary>
    /// Says what went wrong in one line: the message of the exception that carries the meaning.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The sentence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static string Summarise(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        Exception meaningful = exception;

        // Wrappers say "one or more errors occurred" or "an invocation threw": the one inside says what.
        while (meaningful is AggregateException { InnerExceptions.Count: 1 } or TargetInvocationException { InnerException: not null })
        {
            meaningful = meaningful.InnerException!;
        }

        string message = meaningful.Message.Trim();

        return message.Length > 0 ? message : meaningful.GetType().Name;
    }

    /// <summary>
    /// Builds the details of an exception for a bug report: for it and every exception inside it, the type,
    /// the message and the stack trace.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Never <see cref="Exception.Data"/> and never <see cref="Exception.ToString"/>: those are where
    /// whatever a library chose to attach travels.
    /// </remarks>
    public static string BuildDetails(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        StringBuilder details = new();
        Append(details, exception, 0);

        return details.ToString().TrimEnd();
    }

    private bool AllIgnored(AggregateException aggregate)
    {
        foreach (Exception inner in aggregate.InnerExceptions)
        {
            if (!IsIgnoredCancellation(inner))
            {
                return false;
            }
        }

        return true;
    }

    private static string SignatureOf(Exception exception)
        => exception.GetType().FullName + "\n" + exception.Message + "\n" + exception.StackTrace;

    private static void Append(StringBuilder details, Exception exception, int depth)
    {
        _ = details.Append(exception.GetType().FullName).Append(": ").AppendLine(exception.Message);

        if (exception.StackTrace is { Length: > 0 } stack)
        {
            _ = details.AppendLine(stack);
        }

        if (depth >= MaximumDepth)
        {
            return;
        }

        if (exception is AggregateException aggregate)
        {
            for (int index = 0; index < aggregate.InnerExceptions.Count; index++)
            {
                _ = details.Append("--- Inner exception ").Append(index + 1).Append(" of ")
                    .Append(aggregate.InnerExceptions.Count).AppendLine(" ---");
                Append(details, aggregate.InnerExceptions[index], depth + 1);
            }
        }
        else if (exception.InnerException is { } inner)
        {
            _ = details.AppendLine("--- Inner exception ---");
            Append(details, inner, depth + 1);
        }
    }
}
