using System;
using System.Collections.Generic;
using System.IO;
using Enigma.Msi.Model;
using WixSharp.CommonTasks;
using Wix = WixSharp;

namespace Enigma.Msi.Worker.Translation;

/// <summary>
/// Builds the WixSharp <c>ManagedProject</c> that corresponds to an <see cref="MsiPackage"/>. This is
/// the whole model → WixSharp translation, kept separate from the build itself so the net472 suite can
/// assert the translated project graph without a WiX toolchain (an actual
/// <c>ManagedProject.BuildMsi()</c> needs Windows plus the <c>wix</c> CLI).
/// </summary>
public static class MsiProjectFactory
{
    /// <summary>
    /// Recursive source glob applied to the release folder. WixSharp's <c>Files</c> walks
    /// sub-directories, which is exactly the "package one release folder" scope of the model.
    /// </summary>
    private const string ReleaseGlob = "*.*";

    /// <summary>Directory the shortcuts' working directory resolves to at install time.</summary>
    private const string InstallDirProperty = "[INSTALLDIR]";

    /// <summary>
    /// Translates <paramref name="package"/> into a WixSharp project ready to build.
    /// </summary>
    /// <param name="package">
    /// The package to translate. Expected to have passed <c>IMsiPackageValidator.ValidateAll</c>
    /// already — the factory maps what it is given and does not re-report violations.
    /// </param>
    /// <returns>The configured project.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="package"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The package carries an enum value outside the mirrors' member sets (see <see cref="WixEnumMap"/>).
    /// </exception>
    public static Wix.ManagedProject Create(MsiPackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        var project = new Wix.ManagedProject(package.AppName);

        project.AddDir(CreateInstallDir(package.Install));

        foreach (Wix.Dir shortcutDir in CreateShortcutDirs(package.Shortcuts))
        {
            project.AddDir(shortcutDir);
        }

        project.Scope = WixEnumMap.MapScope(package.Scope);
        project.ProductId = package.ProductId;
        project.UpgradeCode = package.UpgradeCode;

        // WixSharp defaults the version to 1.0.0.0; only override it when the model actually carries
        // one, so an unvalidated package cannot null it out.
        if (package.Version is not null)
        {
            project.Version = package.Version;
        }

        project.OutDir = package.Output.OutputPath;
        project.OutFileName = package.Output.MsiFilename;

        ApplyControlPanelInfo(project, package);
        ApplyCompression(project, package.Compression);
        ApplyManagedUi(project, package.Ui);

        return project;
    }

    private static Wix.InstallDir CreateInstallDir(InstallSettings install)
        => new(install.InstallPath, new Wix.Files(Path.Combine(install.ReleasePath, ReleaseGlob)));

    private static IEnumerable<Wix.Dir> CreateShortcutDirs(IEnumerable<Shortcut> shortcuts)
    {
        foreach (Shortcut shortcut in shortcuts)
        {
            var exeShortcut = new Wix.ExeFileShortcut(
                shortcut.ShortcutName,
                shortcut.TargetPath,
                shortcut.Arguments ?? string.Empty)
            {
                WorkingDirectory = InstallDirProperty
            };

            // Leave IconFile at WixSharp's default (the empty string, which omits the attribute) when
            // the model carries no icon — assigning the model's null through would replace that default
            // with a null WixSharp does not expect.
            if (!string.IsNullOrWhiteSpace(shortcut.IconPath))
            {
                exeShortcut.IconFile = shortcut.IconPath;
            }

            yield return new Wix.Dir(shortcut.ShortcutPath, exeShortcut);
        }
    }

    private static void ApplyControlPanelInfo(Wix.ManagedProject project, MsiPackage package)
    {
        Wix.ProductInfo info = project.ControlPanelInfo;
        info.Manufacturer = package.Manufacturer;

        if (package.ControlPanel is not { } controlPanel)
        {
            return;
        }

        // Each member is optional: assigning an empty string would replace the MSI default with a
        // blank Control Panel entry rather than omitting the field.
        if (!string.IsNullOrWhiteSpace(controlPanel.ProductIcon))
        {
            info.ProductIcon = controlPanel.ProductIcon;
        }

        if (!string.IsNullOrWhiteSpace(controlPanel.Comments))
        {
            info.Comments = controlPanel.Comments;
        }

        if (!string.IsNullOrWhiteSpace(controlPanel.Contact))
        {
            info.Contact = controlPanel.Contact;
        }

        if (!string.IsNullOrWhiteSpace(controlPanel.HelpLink))
        {
            info.HelpLink = controlPanel.HelpLink;
        }

        if (!string.IsNullOrWhiteSpace(controlPanel.UrlInfoAbout))
        {
            info.UrlInfoAbout = controlPanel.UrlInfoAbout;
        }
    }

    private static void ApplyCompression(Wix.ManagedProject project, CompressionLevel compression)
    {
        Wix.CompressionLevel level = WixEnumMap.MapCompression(compression);

        // WixSharp seeds Media with a single entry, but the predecessor project reached for it with
        // FirstOrDefault()? — which would have dropped the setting silently had the list ever been
        // empty. Seed it explicitly instead, and cover every cabinet.
        if (project.Media.Count == 0)
        {
            project.Media.Add(new Wix.Media());
        }

        foreach (Wix.Media media in project.Media)
        {
            media.CompressionLevel = level;
        }
    }

    private static void ApplyManagedUi(Wix.ManagedProject project, UiSettings? ui)
    {
        // A null Ui means "the library's default managed UI", not "no UI" — the model exposes the very
        // same defaults through UiSettings.CreateDefault so a UI can pre-fill them.
        UiSettings settings = ui ?? UiSettings.CreateDefault();

        project.UI = WixEnumMap.MapWui(settings.Wui);

        var managedUi = new Wix.ManagedUI();

        foreach (Dialog dialog in settings.InstallDialogs)
        {
            managedUi.InstallDialogs.Add(WixEnumMap.MapDialog(dialog));
        }

        foreach (Dialog dialog in settings.ModifyDialogs)
        {
            managedUi.ModifyDialogs.Add(WixEnumMap.MapDialog(dialog));
        }

        project.ManagedUI = managedUi;
    }
}
