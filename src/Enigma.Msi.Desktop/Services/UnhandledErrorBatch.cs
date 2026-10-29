using System;
using System.Collections.Generic;
using System.Text;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// The errors one dialog shows: the first, and how many more came while it waited or was open.
/// </summary>
public sealed class UnhandledErrorBatch
{
    private readonly List<Exception> _errors = [];

    /// <summary>Creates a batch opened by <paramref name="first"/>.</summary>
    /// <param name="first">The error that opens it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="first"/> is <see langword="null"/>.</exception>
    public UnhandledErrorBatch(Exception first)
    {
        _errors.Add(first ?? throw new ArgumentNullException(nameof(first)));
    }

    /// <summary>Raised when another error is counted into the batch, so a dialog on screen can follow.</summary>
    public event EventHandler? Changed;

    /// <summary>The error that opened the batch.</summary>
    public Exception First => _errors[0];

    /// <summary>Every error of the batch, the first one first.</summary>
    public IReadOnlyList<Exception> Errors => _errors;

    /// <summary>What the dialog says went wrong: the first error's message.</summary>
    public string Message => UnhandledErrorPolicy.Summarise(First);

    /// <summary>How many errors came after the first.</summary>
    public int More => _errors.Count - 1;

    /// <summary>
    /// The details of every error, for a bug report: type, message, inner exceptions and stack trace.
    /// </summary>
    public string Details
    {
        get
        {
            if (_errors.Count == 1)
            {
                return UnhandledErrorPolicy.BuildDetails(First);
            }

            StringBuilder details = new();

            for (int index = 0; index < _errors.Count; index++)
            {
                if (index > 0)
                {
                    _ = details.AppendLine().AppendLine();
                }

                _ = details.Append("Error ").Append(index + 1).Append(" of ").Append(_errors.Count).AppendLine();
                _ = details.Append(UnhandledErrorPolicy.BuildDetails(_errors[index]));
            }

            return details.ToString();
        }
    }

    /// <summary>Counts another error into the batch.</summary>
    /// <param name="error">The error.</param>
    internal void Add(Exception error)
    {
        _errors.Add(error);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
