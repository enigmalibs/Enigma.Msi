using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Desktop.Views;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// <see cref="IAboutDialogService"/> over the control library's shared <c>ContentDialog</c>, owning the
/// <see cref="AboutCard"/> it shows there.
/// </summary>
public sealed class AboutDialogService : IAboutDialogService
{
    // The control's own defaults: the card's fixed 380 px width is what sizes the dialog.
    private const double DefaultMinWidth = 320d;
    private const double DefaultMaxWidth = 600d;

    private readonly IContentDialogService _contentDialogService;

    /// <summary>Creates the service over the dialog host it shows the card in.</summary>
    /// <param name="contentDialogService">The control library's dialog, whose host the app registers at startup.</param>
    /// <exception cref="ArgumentNullException"><paramref name="contentDialogService"/> is <see langword="null"/>.</exception>
    public AboutDialogService(IContentDialogService contentDialogService)
    {
        _contentDialogService = contentDialogService ?? throw new ArgumentNullException(nameof(contentDialogService));
    }

    /// <inheritdoc />
    public async Task ShowAsync()
    {
        // A fresh card per showing, as BuildProgressService does: the host is shared, and a control that
        // outlives its dialog is a binding that outlives it too. The ViewModel is immutable and cheap.
        var card = new AboutCard { DataContext = new AboutViewModel() };

        _ = await _contentDialogService
            .ShowAsync(dialog =>
            {
                // No title and no icon: the card draws them as a column beside the content, and the card
                // already shows the application's own icon and name above everything else.
                dialog.Title = null;
                dialog.Content = card;

                // One button, and it only dismisses: the dialog asks nothing.
                dialog.CloseButtonText = "Close";
                dialog.DefaultButton = DefaultButton.Close;

                // Every size stated, none inherited: ShowAsync resets the content and the buttons but
                // *not* the six size properties, so whatever the previously shown dialog left behind
                // would otherwise apply here.
                dialog.DialogWidth = double.NaN;
                dialog.DialogHeight = double.NaN;
                dialog.DialogMinWidth = DefaultMinWidth;
                dialog.DialogMaxWidth = DefaultMaxWidth;
                dialog.DialogMinHeight = 0d;
                dialog.DialogMaxHeight = double.PositiveInfinity;
            })
            .ConfigureAwait(true);
    }
}
