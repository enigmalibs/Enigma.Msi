using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Msi.Desktop.Views;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// <see cref="IBuildProgressService"/> over the control library's overlay, owning the
/// <see cref="BuildProgressCard"/> it shows there.
/// </summary>
public sealed class BuildProgressService : IBuildProgressService
{
    /// <summary>What the card says before the build has written a line.</summary>
    public const string StartingMessage = "Starting…";

    private readonly IOverlayService _overlayService;

    private BuildProgressCard? _card;

    /// <summary>Creates the service over the overlay it shows the card in.</summary>
    /// <param name="overlayService">The control library's overlay, whose host the app registers at startup.</param>
    /// <exception cref="ArgumentNullException"><paramref name="overlayService"/> is <see langword="null"/>.</exception>
    public BuildProgressService(IOverlayService overlayService)
        => _overlayService = overlayService ?? throw new ArgumentNullException(nameof(overlayService));

    /// <inheritdoc />
    public Task ShowAsync(ICommand cancelCommand)
    {
        if (cancelCommand is null)
        {
            throw new ArgumentNullException(nameof(cancelCommand));
        }

        // A fresh card per build: the alternative is one long-lived control whose stale message shows for
        // the instant between showing the overlay and the first streamed line.
        _card = new BuildProgressCard
        {
            Message = StartingMessage,
            CancelCommand = cancelCommand
        };

        return _overlayService.ShowAsync(_card);
    }

    /// <inheritdoc />
    public void ReportMessage(string message)
    {
        if (_card is { } card)
        {
            card.Message = message;
        }
    }

    /// <inheritdoc />
    public Task HideAsync()
    {
        if (_card is null)
        {
            return Task.CompletedTask;
        }

        _card = null;

        return _overlayService.HideAsync();
    }
}
