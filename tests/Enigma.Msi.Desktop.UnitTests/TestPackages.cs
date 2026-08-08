using System;
using Enigma.Msi.Model;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// Package fixtures for the ViewModel suite. <see cref="CreateFull"/> populates every member,
/// including the optional blocks, so a load/materialize round-trip through the form has something to
/// lose if it drops a field.
/// </summary>
internal static class TestPackages
{
    /// <summary>A package with every member populated.</summary>
    public static MsiPackage CreateFull() => new()
    {
        AppName = "Contoso Widget",
        Version = new Version(1, 2, 3, 4),
        ProductId = Guid.Parse("9b4a0f2e-1c3d-4b5a-8e6f-7a8b9c0d1e2f"),
        UpgradeCode = Guid.Parse("2f1e0d9c-8b7a-6f5e-4b3c-2d1e0f9a8b7c"),
        Manufacturer = "Contoso AG",
        Scope = InstallScope.PerUser,
        Install = new InstallSettings
        {
            InstallPath = @"%ProgramFiles%\Contoso\Widget",
            ReleasePath = @"C:\build\widget\release"
        },
        Output = new OutputSettings
        {
            OutputPath = @"C:\build\widget\artifacts",
            MsiFilename = "ContosoWidget"
        },
        Compression = CompressionLevel.MsZip,
        ControlPanel = new ControlPanelInfo
        {
            ProductIcon = @"C:\build\widget\app.ico",
            Comments = "The Contoso widget management tool.",
            Contact = "support@contoso.example",
            HelpLink = "https://contoso.example/support",
            UrlInfoAbout = "https://contoso.example/widget"
        },
        Shortcuts =
        [
            new Shortcut
            {
                ShortcutPath = "%Desktop%",
                ShortcutName = "Contoso Widget",
                TargetPath = @"[INSTALLDIR]\Widget.exe",
                IconPath = @"C:\build\widget\app.ico",
                Arguments = "--first-run"
            },
            new Shortcut
            {
                ShortcutPath = @"%ProgramMenu%\Contoso",
                ShortcutName = "Contoso Widget (safe mode)",
                TargetPath = @"[INSTALLDIR]\Widget.exe"
            }
        ],
        Ui = new UiSettings
        {
            Wui = Wui.WixUI_Mondo,
            InstallDialogs = [Dialog.Welcome, Dialog.Licence, Dialog.InstallDir, Dialog.Progress, Dialog.Exit],
            ModifyDialogs = [Dialog.Welcome, Dialog.MaintenanceType, Dialog.Progress, Dialog.Exit]
        }
    };

    /// <summary>The smallest package that passes the in-memory rules.</summary>
    public static MsiPackage CreateMinimalValid() => new()
    {
        AppName = "Widget",
        Version = new Version(1, 0, 0),
        ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        UpgradeCode = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Manufacturer = "Contoso AG",
        Install = new InstallSettings
        {
            InstallPath = @"%ProgramFiles%\Widget",
            ReleasePath = @"C:\build\widget\release"
        },
        Output = new OutputSettings
        {
            OutputPath = @"C:\build\widget\artifacts",
            MsiFilename = "Widget"
        }
    };
}
