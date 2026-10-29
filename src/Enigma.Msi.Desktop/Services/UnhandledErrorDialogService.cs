using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Controls;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Desktop.Views;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// <see cref="IUnhandledErrorDialogService"/> over the control library's shared <c>ContentDialog</c>, owning
/// the <see cref="UnhandledErrorCard"/> it shows there.
/// </summary>
/// <remarks>
/// The dialog host is shared with every other question the window asks, and the build overlay is drawn
/// above it. So the dialog waits: until the window is on screen, until no other dialog holds the host —
/// showing would replace that dialog's content — and until no build covers it. An error while a build
/// runs therefore shows when the build ends.
/// </remarks>
public sealed class UnhandledErrorDialogService : IUnhandledErrorDialogService
{
    // The control's own defaults: the card's 480 px minimum is what sizes the dialog.
    private const double DefaultMinWidth = 320d;
    private const double DefaultMaxWidth = 600d;

    // Parsed once: ToGeometry re-parses the path data on every call.
    private static readonly Geometry TitleIcon = PhosphorIconSet.Instance
        .GetGlyph(PhosphorIcon.Warning, PhosphorWeight.Regular)
        .ToGeometry();

    // Every Enigma app's warning amber, as Enigma.GitClient's safety net uses it.
    private static readonly IBrush TitleIconBrush = new SolidColorBrush(Color.Parse("#E8A33D"));

    private readonly IContentDialogService _contentDialogService;
    private readonly MainWindow _window;

    /// <summary>Creates the service over the window whose dialog host it shows the card in.</summary>
    /// <param name="contentDialogService">The control library's dialog, whose host the app registers at startup.</param>
    /// <param name="window">The window that owns that host and the build overlay above it.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UnhandledErrorDialogService(IContentDialogService contentDialogService, MainWindow window)
    {
        _contentDialogService = contentDialogService ?? throw new ArgumentNullException(nameof(contentDialogService));
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    /// <inheritdoc />
    public async Task ShowAsync(UnhandledErrorBatch batch)
    {
        if (batch is null)
        {
            throw new ArgumentNullException(nameof(batch));
        }

        while (IsBusy)
        {
            await WhenStateChangesAsync().ConfigureAwait(true);

            // Background priority: whatever awaited the dialog that just closed runs first, and may ask
            // its next question at once — which this one then waits behind too.
            await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
        }

        // A fresh card per showing, as the other dialogs do: a control that outlives its dialog is a
        // binding that outlives it too.
        var card = new UnhandledErrorCard { DataContext = new UnhandledErrorViewModel(batch) };

        _ = await _contentDialogService
            .ShowAsync(dialog =>
            {
                dialog.Title = "Something went wrong";
                dialog.Content = card;
                dialog.CloseButtonText = "Close";
                dialog.DefaultButton = DefaultButton.Close;
                dialog.IconData = TitleIcon;
                dialog.IconBrush = TitleIconBrush;

                // Every size stated, none inherited: ShowAsync resets the content and the buttons but
                // *not* the six size properties.
                dialog.DialogWidth = double.NaN;
                dialog.DialogHeight = double.NaN;
                dialog.DialogMinWidth = DefaultMinWidth;
                dialog.DialogMaxWidth = DefaultMaxWidth;
                dialog.DialogMinHeight = 0d;
                dialog.DialogMaxHeight = double.PositiveInfinity;
            })
            .ConfigureAwait(true);
    }

    private bool IsBusy => !_window.IsVisible || _window.HostDialog.IsOpen || _window.HostOverlay.IsOpen;

    // Completes the next time any of the three things IsBusy reads changes.
    private Task WhenStateChangesAsync()
    {
        TaskCompletionSource changed = new();

        void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != Visual.IsVisibleProperty
                && e.Property != ContentDialog.IsOpenProperty
                && e.Property != Overlay.IsOpenProperty)
            {
                return;
            }

            _window.PropertyChanged -= OnPropertyChanged;
            _window.HostDialog.PropertyChanged -= OnPropertyChanged;
            _window.HostOverlay.PropertyChanged -= OnPropertyChanged;
            _ = changed.TrySetResult();
        }

        _window.PropertyChanged += OnPropertyChanged;
        _window.HostDialog.PropertyChanged += OnPropertyChanged;
        _window.HostOverlay.PropertyChanged += OnPropertyChanged;

        return changed.Task;
    }
}
