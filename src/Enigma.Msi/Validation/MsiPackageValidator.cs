using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enigma.Msi.Model;

namespace Enigma.Msi.Validation;

/// <summary>
/// The default <see cref="IMsiPackageValidator"/>. Stateless and thread-safe: a single instance can
/// be shared, or registered as a singleton by a consumer that uses dependency injection.
/// </summary>
public sealed class MsiPackageValidator : IMsiPackageValidator
{
    /// <summary>Largest value Windows Installer accepts for a version's major and minor fields.</summary>
    private const int MaxVersionMajorMinor = 255;

    /// <summary>Largest value Windows Installer accepts for a version's build field.</summary>
    private const int MaxVersionBuild = 65535;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="package"/> is <see langword="null"/>.</exception>
    public MsiValidationResult Validate(MsiPackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        var errors = new List<MsiValidationError>();
        CollectModelErrors(package, errors);

        return new MsiValidationResult(errors);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="package"/> is <see langword="null"/>.</exception>
    public MsiValidationResult ValidateEnvironment(MsiPackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        var errors = new List<MsiValidationError>();
        CollectEnvironmentErrors(package, errors);

        return new MsiValidationResult(errors);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="package"/> is <see langword="null"/>.</exception>
    public MsiValidationResult ValidateAll(MsiPackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        var errors = new List<MsiValidationError>();
        CollectModelErrors(package, errors);
        CollectEnvironmentErrors(package, errors);

        return new MsiValidationResult(errors);
    }

    private static void CollectModelErrors(MsiPackage package, ICollection<MsiValidationError> errors)
    {
        if (package.SchemaVersion < 1)
        {
            errors.Add(new("schemaVersion", "Must be 1 or greater."));
        }
        else if (package.SchemaVersion > MsiPackage.CurrentSchemaVersion)
        {
            errors.Add(new(
                "schemaVersion",
                $"Unsupported schema version {package.SchemaVersion}; this version of Enigma.Msi reads up to {MsiPackage.CurrentSchemaVersion}."));
        }

        RequireText(package.AppName, "appName", errors);
        RequireText(package.Manufacturer, "manufacturer", errors);
        ValidateVersion(package.Version, errors);
        RequireGuid(package.ProductId, "productId", errors);
        RequireGuid(package.UpgradeCode, "upgradeCode", errors);
        RequireDefinedEnum(typeof(InstallScope), package.Scope, "scope", "installation scope", errors);
        RequireDefinedEnum(typeof(CompressionLevel), package.Compression, "compression", "compression level", errors);

        // A non-nullable member can still arrive null from JSON ("install": null), so guard before use.
        if (package.Install is null)
        {
            errors.Add(new("install", "Required."));
        }
        else
        {
            RequireText(package.Install.InstallPath, "install.installPath", errors);
            RequireText(package.Install.ReleasePath, "install.releasePath", errors);
        }

        if (package.Output is null)
        {
            errors.Add(new("output", "Required."));
        }
        else
        {
            RequireText(package.Output.OutputPath, "output.outputPath", errors);
            ValidateMsiFilename(package.Output.MsiFilename, errors);
        }

        ValidateShortcuts(package.Shortcuts, errors);

        if (package.Ui is not null)
        {
            RequireDefinedEnum(typeof(Wui), package.Ui.Wui, "ui.wui", "WixUI dialog set", errors);
            ValidateDialogs(package.Ui.InstallDialogs, "ui.installDialogs", errors);
            ValidateDialogs(package.Ui.ModifyDialogs, "ui.modifyDialogs", errors);
        }
    }

    private static void CollectEnvironmentErrors(MsiPackage package, ICollection<MsiValidationError> errors)
    {
        if (package.Install is not null)
        {
            RequireExistingDirectory(package.Install.ReleasePath, "install.releasePath", errors, requireContent: true);
        }

        if (package.Output is not null)
        {
            RequireExistingDirectory(package.Output.OutputPath, "output.outputPath", errors, requireContent: false);
        }

        if (package.ControlPanel?.ProductIcon is { } productIcon)
        {
            RequireExistingFile(productIcon, "controlPanel.productIcon", errors);
        }

        if (package.Shortcuts is null)
        {
            return;
        }

        for (int i = 0; i < package.Shortcuts.Count; i++)
        {
            if (package.Shortcuts[i]?.IconPath is { } iconPath)
            {
                RequireExistingFile(iconPath, $"shortcuts[{i}].iconPath", errors);
            }
        }
    }

    private static void ValidateVersion(Version? version, ICollection<MsiValidationError> errors)
    {
        if (version is null)
        {
            errors.Add(new("version", "Required."));
            return;
        }

        if (version.Major > MaxVersionMajorMinor
            || version.Minor > MaxVersionMajorMinor
            || version.Build > MaxVersionBuild)
        {
            errors.Add(new(
                "version",
                $"Windows Installer only accepts major.minor.build with major and minor up to {MaxVersionMajorMinor} and build up to {MaxVersionBuild}; \"{version}\" exceeds that."));
        }
    }

    private static void ValidateMsiFilename(string filename, ICollection<MsiValidationError> errors)
    {
        const string memberPath = "output.msiFilename";

        if (string.IsNullOrWhiteSpace(filename))
        {
            errors.Add(new(memberPath, "Required."));
            return;
        }

        if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errors.Add(new(
                memberPath,
                "Must be a plain file name: no directory separators, and no characters that are invalid in a file name."));
        }

        if (filename.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new(memberPath, "Must not include the .msi extension — the build appends it."));
        }
    }

    private static void ValidateShortcuts(List<Shortcut>? shortcuts, ICollection<MsiValidationError> errors)
    {
        if (shortcuts is null)
        {
            errors.Add(new("shortcuts", "Must not be null; use an empty list when the package creates no shortcuts."));
            return;
        }

        for (int i = 0; i < shortcuts.Count; i++)
        {
            string prefix = $"shortcuts[{i}]";
            Shortcut? shortcut = shortcuts[i];

            if (shortcut is null)
            {
                errors.Add(new(prefix, "Required."));
                continue;
            }

            RequireText(shortcut.ShortcutPath, $"{prefix}.shortcutPath", errors);
            RequireText(shortcut.ShortcutName, $"{prefix}.shortcutName", errors);
            RequireText(shortcut.TargetPath, $"{prefix}.targetPath", errors);
        }
    }

    private static void ValidateDialogs(List<Dialog>? dialogs, string memberPath, ICollection<MsiValidationError> errors)
    {
        if (dialogs is null || dialogs.Count == 0)
        {
            errors.Add(new(memberPath, "Must list at least one dialog when a managed UI is configured."));
            return;
        }

        for (int i = 0; i < dialogs.Count; i++)
        {
            RequireDefinedEnum(typeof(Dialog), dialogs[i], $"{memberPath}[{i}]", "dialog", errors);
        }
    }

    private static void RequireText(string value, string memberPath, ICollection<MsiValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new(memberPath, "Required."));
        }
    }

    private static void RequireGuid(Guid value, string memberPath, ICollection<MsiValidationError> errors)
    {
        if (value == Guid.Empty)
        {
            errors.Add(new(memberPath, "Required — must be a non-empty GUID."));
        }
    }

    private static void RequireDefinedEnum(
        Type enumType,
        object value,
        string memberPath,
        string description,
        ICollection<MsiValidationError> errors)
    {
        if (!Enum.IsDefined(enumType, value))
        {
            errors.Add(new(memberPath, $"\"{value}\" is not a known {description}."));
        }
    }

    private static void RequireExistingDirectory(
        string path,
        string memberPath,
        ICollection<MsiValidationError> errors,
        bool requireContent)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            // Reported by the in-memory rules; do not report the same member twice.
            return;
        }

        if (File.Exists(path))
        {
            errors.Add(new(memberPath, "Expected a directory, but a file exists at this path."));
            return;
        }

        if (!Directory.Exists(path))
        {
            errors.Add(new(memberPath, "Directory does not exist."));
            return;
        }

        if (!requireContent)
        {
            return;
        }

        try
        {
            if (!Directory.EnumerateFileSystemEntries(path).Any())
            {
                errors.Add(new(memberPath, "Directory is empty — there is nothing to package."));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors.Add(new(memberPath, $"Directory cannot be read: {ex.Message}"));
        }
    }

    private static void RequireExistingFile(string path, string memberPath, ICollection<MsiValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            errors.Add(new(memberPath, "File does not exist."));
        }
    }
}
