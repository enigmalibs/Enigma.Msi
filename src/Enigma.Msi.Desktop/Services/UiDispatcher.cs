using System;
using Avalonia.Threading;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// <see cref="IUiDispatcher"/> over Avalonia's <see cref="Dispatcher.UIThread"/>.
/// </summary>
public sealed class UiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }
}
