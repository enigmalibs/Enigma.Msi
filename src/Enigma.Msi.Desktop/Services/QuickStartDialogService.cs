using System;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Media;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Desktop.Views;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// <see cref="IQuickStartDialogService"/> over the control library's shared <c>ContentDialog</c>, owning
/// the <see cref="QuickStartCard"/> it shows there.
/// </summary>
public sealed class QuickStartDialogService : IQuickStartDialogService
{
    private const double CardWidth = 620d;
    private const double CardMinWidth = 480d;
    private const double CardMaxWidth = 720d;

    // Parsed once: ToGeometry re-parses the path data on every call.
    private static readonly Geometry TitleIcon = PhosphorIconSet.Instance
        .GetGlyph(PhosphorIcon.MagicWand, PhosphorWeight.Regular)
        .ToGeometry();

    private readonly IContentDialogService _contentDialogService;
    private readonly Func<QuickStartViewModel> _viewModelFactory;

    /// <summary>Creates the service over the dialog host it shows the card in.</summary>
    /// <param name="contentDialogService">The control library's dialog, whose host the app registers at startup.</param>
    /// <param name="viewModelFactory">
    /// Produces a form per showing. A single shared instance would open the second quick start on the
    /// first one's answers.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public QuickStartDialogService(
        IContentDialogService contentDialogService,
        Func<QuickStartViewModel> viewModelFactory)
    {
        _contentDialogService = contentDialogService ?? throw new ArgumentNullException(nameof(contentDialogService));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
    }

    /// <inheritdoc />
    public async Task<QuickStartSettings?> ShowAsync()
    {
        QuickStartViewModel viewModel = _viewModelFactory();
        var card = new QuickStartCard { DataContext = viewModel };
        IDisposable? applyGate = null;

        try
        {
            DialogResult result = await _contentDialogService
                .ShowAsync(dialog =>
                {
                    dialog.Title = "Quick start";
                    dialog.Content = card;
                    dialog.PrimaryButtonText = "Apply";
                    dialog.CloseButtonText = "Cancel";
                    dialog.DefaultButton = DefaultButton.Primary;
                    dialog.IconData = TitleIcon;

                    // Apply stays disabled until the six answers add up. Bound rather than assigned,
                    // because CanApply changes on every keystroke — and disposed below, because the host
                    // is shared and its reset assigns IsPrimaryButtonEnabled instead of clearing it: a
                    // binding left installed would still be gating the *next* dialog's button.
                    applyGate = dialog.Bind(
                        ContentDialog.IsPrimaryButtonEnabledProperty,
                        new Binding(nameof(QuickStartViewModel.CanApply)) { Source = viewModel });

                    // Every size stated, none inherited: ShowAsync resets the content and the buttons
                    // but *not* the six size properties, so whatever the previously shown dialog left
                    // behind would otherwise apply here.
                    dialog.DialogWidth = CardWidth;
                    dialog.DialogHeight = double.NaN;
                    dialog.DialogMinWidth = CardMinWidth;
                    dialog.DialogMaxWidth = CardMaxWidth;
                    dialog.DialogMinHeight = 0d;
                    dialog.DialogMaxHeight = double.PositiveInfinity;
                })
                .ConfigureAwait(true);

            // Primary is the only value that counts as confirmation: Escape and a click on the scrim
            // both close with None, not Close.
            return result == DialogResult.Primary && viewModel.CanApply ? viewModel.ToSettings() : null;
        }
        finally
        {
            // ShowAsync completes exactly when the dialog closes, which makes this the one correct
            // disposal site for the binding above.
            applyGate?.Dispose();
        }
    }
}
