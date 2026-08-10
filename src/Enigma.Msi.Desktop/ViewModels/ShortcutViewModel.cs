using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.Msi.Model;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// One row of the shortcuts editor. Edits the five members of <see cref="Shortcut"/> as text, mapping
/// a blank optional field to <see langword="null"/> in both directions so a shortcut the user left
/// alone does not serialize an empty string.
/// </summary>
public sealed partial class ShortcutViewModel : ObservableObject
{
    /// <summary>The special-folder token a new shortcut starts at.</summary>
    public const string DefaultShortcutPath = "%ProgramMenu%";

    /// <summary>What <see cref="DisplayName"/> falls back to while the row has no name yet.</summary>
    public const string UnnamedDisplayName = "Shortcut";

    [ObservableProperty]
    private string _shortcutPath = DefaultShortcutPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string _shortcutName = string.Empty;

    [ObservableProperty]
    private string _targetPath = string.Empty;

    [ObservableProperty]
    private string _iconPath = string.Empty;

    [ObservableProperty]
    private string _arguments = string.Empty;

    /// <summary>
    /// The row's card header — its <see cref="ShortcutName"/>, or <see cref="UnnamedDisplayName"/> while
    /// that is blank, so a row the user has not named yet still has a title to sit under.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(ShortcutName) ? UnnamedDisplayName : ShortcutName;

    /// <summary>Creates a row holding <paramref name="shortcut"/>'s values.</summary>
    /// <param name="shortcut">The shortcut to edit.</param>
    /// <returns>The populated row.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="shortcut"/> is <see langword="null"/>.</exception>
    public static ShortcutViewModel FromModel(Shortcut shortcut)
    {
        if (shortcut is null)
        {
            throw new ArgumentNullException(nameof(shortcut));
        }

        return new ShortcutViewModel
        {
            ShortcutPath = shortcut.ShortcutPath,
            ShortcutName = shortcut.ShortcutName,
            TargetPath = shortcut.TargetPath,
            IconPath = shortcut.IconPath ?? string.Empty,
            Arguments = shortcut.Arguments ?? string.Empty
        };
    }

    /// <summary>Materializes the edited values as a model shortcut.</summary>
    /// <returns>The shortcut this row describes.</returns>
    public Shortcut ToModel() => new()
    {
        ShortcutPath = ShortcutPath,
        ShortcutName = ShortcutName,
        TargetPath = TargetPath,
        IconPath = NullIfBlank(IconPath),
        Arguments = NullIfBlank(Arguments)
    };

    private static string? NullIfBlank(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
