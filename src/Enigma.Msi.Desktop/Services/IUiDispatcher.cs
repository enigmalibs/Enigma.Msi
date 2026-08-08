using System;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Marshals work onto the UI thread. Exists as an interface because the build log is written from the
/// worker's output-reading thread while it is bound to a control: the ViewModel must hand that work
/// to the dispatcher, and a ViewModel test must be able to run it inline instead of standing up an
/// Avalonia application.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread — immediately when the caller is already on it,
    /// otherwise by posting it.
    /// </summary>
    /// <param name="action">The work to run.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    void Post(Action action);
}
