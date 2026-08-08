namespace Enigma.Msi.Model;

/// <summary>
/// Where the package installs to, and which folder's contents it packages.
/// </summary>
public sealed class InstallSettings
{
    /// <summary>
    /// Target installation directory. May contain Windows environment variables, e.g.
    /// <c>%ProgramFiles%\MyApp</c>.
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;

    /// <summary>
    /// Local directory whose contents (recursively) are packaged into the installer — typically an
    /// application's release output folder.
    /// </summary>
    public string ReleasePath { get; set; } = string.Empty;
}
