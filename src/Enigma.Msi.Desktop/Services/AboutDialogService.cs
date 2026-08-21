using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Desktop.Views;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// <see cref="IAboutDialogService"/> over the control library's shared <c>ContentDialog</c>, owning the
/// <see cref="AboutCard"/> it shows there.
/// </summary>
public sealed class AboutDialogService : IAboutDialogService
{
    private const double CardWidth = 460d;
    private const double CardMinWidth = 360d;
    private const double CardMaxWidth = 520d;

    // Parsed once: ToGeometry re-parses the path data on every call.
    private static readonly Geometry TitleIcon = PhosphorIconSet.Instance
        .GetGlyph(PhosphorIcon.Info, PhosphorWeight.Regular)
        .ToGeometry();

    private readonly IContentDialogService _contentDialogService;
    private readonly AboutViewModel _viewModel;

    /// <summary>Creates the service over the dialog host it shows the card in.</summary>
    /// <param name="contentDialogService">The control library's dialog, whose host the app registers at startup.</param>
    /// <param name="viewModel">What the card binds to.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public AboutDialogService(IContentDialogService contentDialogService, AboutViewModel viewModel)
    {
        _contentDialogService = contentDialogService ?? throw new ArgumentNullException(nameof(contentDialogService));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    /// <inheritdoc />
    public async Task ShowAsync()
    {
        // A fresh card per showing, as BuildProgressService does: the host is shared, and a control that
        // outlives its dialog is a binding that outlives it too.
        var card = new AboutCard { DataContext = _viewModel };

        _ = await _contentDialogService
            .ShowAsync(dialog =>
            {
                dialog.Title = "About";
                dialog.Content = card;
                dialog.CloseButtonText = "Close";
                dialog.IconData = TitleIcon;

                // Every size stated, none inherited: ShowAsync resets the content and the buttons but
                // *not* the six size properties, so whatever the previously shown dialog left behind
                // would otherwise apply here.
                dialog.DialogWidth = CardWidth;
                dialog.DialogHeight = double.NaN;
                dialog.DialogMinWidth = CardMinWidth;
                dialog.DialogMaxWidth = CardMaxWidth;
                dialog.DialogMinHeight = 0d;
                dialog.DialogMaxHeight = double.PositiveInfinity;
            })
            .ConfigureAwait(true);
    }
}
