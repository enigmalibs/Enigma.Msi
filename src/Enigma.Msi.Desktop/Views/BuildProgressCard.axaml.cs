using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Enigma.Msi.Desktop.Views;

/// <summary>
/// The card the build overlay shows: a title, an indeterminate bar, the latest line the build wrote, and
/// Cancel. Owned by <see cref="Services.BuildProgressService"/>, which mutates
/// <see cref="Message"/> while the build runs.
/// </summary>
public partial class BuildProgressCard : UserControl
{
    /// <summary>Defines the <see cref="Message"/> property.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<BuildProgressCard, string?>(nameof(Message));

    /// <summary>Defines the <see cref="CancelCommand"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CancelCommandProperty =
        AvaloniaProperty.Register<BuildProgressCard, ICommand?>(nameof(CancelCommand));

    /// <summary>Creates the card.</summary>
    public BuildProgressCard() => InitializeComponent();

    /// <summary>The latest line the build wrote, shown on one line and elided when it does not fit.</summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>What the Cancel button invokes.</summary>
    public ICommand? CancelCommand
    {
        get => GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }
}
