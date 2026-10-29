using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Enigma.Msi.Desktop.ViewModels;

namespace Enigma.Msi.Desktop.Views;

/// <summary>
/// The content of the safety net's dialog. Shown inside the control library's shared <c>ContentDialog</c>
/// by <see cref="Services.UnhandledErrorDialogService"/>, which is where the Close button comes from.
/// </summary>
public partial class UnhandledErrorCard : UserControl
{
    /// <summary>Creates the card.</summary>
    public UnhandledErrorCard() => InitializeComponent();

    // The clipboard belongs to the window the card is in, which only the view can reach: copying from here
    // keeps a clipboard seam out of a ViewModel that has nothing else to do with the platform.
    private async void OnCopyDetailsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UnhandledErrorViewModel viewModel && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(viewModel.Details).ConfigureAwait(true);
        }
    }
}
