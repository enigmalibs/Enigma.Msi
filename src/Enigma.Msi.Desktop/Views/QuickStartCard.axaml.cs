using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Enigma.Msi.Desktop.Views;

/// <summary>
/// The quick start's content: the eight fields, shown inside the control library's shared
/// <c>ContentDialog</c> by <see cref="Services.QuickStartDialogService"/> — which is where Apply and
/// Cancel come from, so the card carries no buttons of its own.
/// </summary>
public partial class QuickStartCard : UserControl
{
    /// <summary>Creates the card.</summary>
    public QuickStartCard() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Not cosmetic: the dialog's Escape handling only fires while focus is inside it, and nothing
        // focuses dialog content on open — so without this, Escape would silently do nothing. Posted
        // rather than called inline, because at attach time the dialog is not on screen yet and focusing
        // an invisible element does nothing. The first field is the one to land on: it is also where
        // typing starts.
        Dispatcher.UIThread.Post(() => { _ = AppNameEditor.Focus(); }, DispatcherPriority.Input);
    }
}
