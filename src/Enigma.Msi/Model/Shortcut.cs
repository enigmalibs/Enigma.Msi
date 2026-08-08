namespace Enigma.Msi.Model;

/// <summary>
/// A single shortcut created by the installer.
/// </summary>
public sealed class Shortcut
{
    /// <summary>
    /// Location the shortcut is created in — a special-folder token such as <c>%Desktop%</c> or
    /// <c>%ProgramMenu%</c>, optionally with a sub-folder (<c>%ProgramMenu%\MyCompany</c>).
    /// </summary>
    public string ShortcutPath { get; set; } = string.Empty;

    /// <summary>Display name of the shortcut.</summary>
    public string ShortcutName { get; set; } = string.Empty;

    /// <summary>
    /// The installed file the shortcut launches, expressed against the install directory, e.g.
    /// <c>[INSTALLDIR]\MyApp.exe</c>.
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>Optional path to the shortcut's icon file.</summary>
    public string? IconPath { get; set; }

    /// <summary>Optional command-line arguments passed to the target.</summary>
    public string? Arguments { get; set; }
}
