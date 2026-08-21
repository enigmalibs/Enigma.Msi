using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Enigma.Msi.Desktop.Views;

/// <summary>
/// The About dialog's content: the app's identity and a link to its repository. Shown inside the control
/// library's shared <c>ContentDialog</c> by <see cref="Services.AboutDialogService"/>, which is where
/// the Close button comes from — the card carries none of its own.
/// </summary>
public partial class AboutCard : UserControl
{
    /// <summary>Creates the card.</summary>
    public AboutCard() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The dialog's Escape handling only fires while focus is inside it, and nothing focuses dialog
        // content on open — so the card takes focus itself, which is why it is Focusable. Posted rather
        // than called inline: at attach time the dialog is not on screen yet, and focusing an invisible
        // element does nothing.
        Dispatcher.UIThread.Post(() => { _ = Focus(); }, DispatcherPriority.Input);
    }
}
