namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// The six answers the quick start collects, validated and ready to be applied to the form.
/// </summary>
/// <param name="AppName">The product's name — also the MSI file name, the shortcut names and the last
/// segment of the install path.</param>
/// <param name="Version">The product version, as text that parses as a <see cref="System.Version"/>.</param>
/// <param name="Manufacturer">The publisher shown by the installer and in Control Panel.</param>
/// <param name="ReleasePath">The folder whose contents are packaged.</param>
/// <param name="IconPath">The <c>.ico</c> file used as the product icon and on both shortcuts.</param>
/// <param name="ExecutableRelativePath">
/// The main executable's path <em>relative to</em> <paramref name="ReleasePath"/> — the relative one and
/// not the absolute one, because that is what a shortcut target needs: the release folder is what
/// <c>[INSTALLDIR]</c> becomes once the MSI is installed.
/// </param>
public sealed record QuickStartSettings(
    string AppName,
    string Version,
    string Manufacturer,
    string ReleasePath,
    string IconPath,
    string ExecutableRelativePath);
