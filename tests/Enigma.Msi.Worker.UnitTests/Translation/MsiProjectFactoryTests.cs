using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.Msi.Model;
using Enigma.Msi.Worker.Translation;
using Xunit;
using Wix = WixSharp;

namespace Enigma.Msi.Worker.UnitTests.Translation;

/// <summary>
/// Covers the model → WixSharp project translation without building an MSI (that needs Windows plus the
/// <c>wix</c> CLI and stays a manual acceptance step).
/// </summary>
/// <remarks>
/// WixSharp expands a multi-segment directory name into a chain of nested <c>Dir</c>s, so the directory
/// assertions walk the tree and rebuild the path rather than reading a single <c>Dir.Name</c>.
/// </remarks>
public sealed class MsiProjectFactoryTests
{
    [Fact]
    public void Create_RejectsNull()
        => Assert.Throws<ArgumentNullException>(() => MsiProjectFactory.Create(null!));

    [Fact]
    public void Create_TranslatesProductIdentity()
    {
        MsiPackage package = TestPackages.CreateFull();

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        Assert.Equal(package.AppName, project.Name);
        Assert.Equal(package.Version, project.Version);
        Assert.Equal(package.ProductId, project.ProductId);
        Assert.Equal(package.UpgradeCode, project.UpgradeCode);
        Assert.Equal(Wix.InstallScope.perUser, project.Scope);
    }

    [Fact]
    public void Create_TranslatesOutputSettings()
    {
        MsiPackage package = TestPackages.CreateFull();

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        Assert.Equal(package.Output.OutputPath, project.OutDir);
        Assert.Equal(package.Output.MsiFilename, project.OutFileName);
    }

    [Fact]
    public void Create_KeepsWixSharpsDefaultVersionWhenTheModelCarriesNone()
    {
        MsiPackage package = TestPackages.CreateMinimal();
        package.Version = null;

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        Assert.NotNull(project.Version);
    }

    [Fact]
    public void Create_PackagesTheReleaseFolderRecursivelyIntoTheInstallDirectory()
    {
        MsiPackage package = TestPackages.CreateFull();

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        // WixSharp marks the root of the expanded chain as the InstallDir; the files land on its leaf.
        Assert.Single(project.Dirs.OfType<Wix.InstallDir>());

        (string path, Wix.Dir dir) = Assert.Single(
            Walk(project.Dirs),
            entry => entry.Dir.FileCollections is { Length: > 0 });

        Assert.Equal(package.Install.InstallPath, path);

        Wix.Files files = Assert.Single(dir.FileCollections);
        Assert.Equal(package.Install.ReleasePath, files.Directory);
        Assert.Equal("*.*", files.IncludeMask);
    }

    [Fact]
    public void Create_TranslatesEveryShortcutIntoItsOwnDirectory()
    {
        MsiPackage package = TestPackages.CreateFull();

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        Dictionary<string, Wix.ExeFileShortcut> byPath = ShortcutsByPath(project);

        Assert.Equal(package.Shortcuts.Count, byPath.Count);

        foreach (Shortcut expected in package.Shortcuts)
        {
            Wix.ExeFileShortcut actual = Assert.Contains(expected.ShortcutPath, byPath);

            Assert.Equal(expected.ShortcutName, actual.Name);
            Assert.Equal(expected.TargetPath, actual.Target);
            Assert.Equal(expected.Arguments, actual.Arguments);
            Assert.Equal(expected.IconPath, actual.IconFile);
            Assert.Equal("[INSTALLDIR]", actual.WorkingDirectory);
        }
    }

    [Fact]
    public void Create_LeavesTheShortcutIconAndArgumentsUnsetWhenTheModelHasNone()
    {
        MsiPackage package = TestPackages.CreateMinimal();
        package.Shortcuts.Add(new Shortcut
        {
            ShortcutPath = "%Desktop%",
            ShortcutName = "Widget",
            TargetPath = @"[INSTALLDIR]\Widget.exe"
        });

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        Wix.ExeFileShortcut shortcut = Assert.Single(ShortcutsByPath(project).Values);

        // WixSharp initialises both to the empty string and omits the corresponding attributes; the
        // translation must leave them there rather than push the model's nulls through.
        Assert.Equal(string.Empty, shortcut.IconFile);
        Assert.Equal(string.Empty, shortcut.Arguments);
    }

    [Fact]
    public void Create_AddsNoShortcutDirectoriesWhenThePackageHasNone()
    {
        Wix.ManagedProject project = MsiProjectFactory.Create(TestPackages.CreateMinimal());

        Assert.Empty(ShortcutsByPath(project));
    }

    [Fact]
    public void Create_TranslatesControlPanelInfo()
    {
        MsiPackage package = TestPackages.CreateFull();
        ControlPanelInfo expected = package.ControlPanel!;

        Wix.ProductInfo info = MsiProjectFactory.Create(package).ControlPanelInfo;

        Assert.Equal(package.Manufacturer, info.Manufacturer);
        Assert.Equal(expected.ProductIcon, info.ProductIcon);
        Assert.Equal(expected.Comments, info.Comments);
        Assert.Equal(expected.Contact, info.Contact);
        Assert.Equal(expected.HelpLink, info.HelpLink);
        Assert.Equal(expected.UrlInfoAbout, info.UrlInfoAbout);
    }

    [Fact]
    public void Create_SetsTheManufacturerEvenWithoutAControlPanelEntry()
    {
        MsiPackage package = TestPackages.CreateMinimal();

        Wix.ProductInfo info = MsiProjectFactory.Create(package).ControlPanelInfo;

        Assert.Equal(package.Manufacturer, info.Manufacturer);
        Assert.Null(info.Comments);
    }

    [Fact]
    public void Create_AppliesTheCompressionLevelToEveryCabinet()
    {
        MsiPackage package = TestPackages.CreateFull();

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        Assert.NotEmpty(project.Media);
        Assert.All(project.Media, media => Assert.Equal(Wix.CompressionLevel.mszip, media.CompressionLevel));
    }

    [Fact]
    public void Create_TranslatesTheConfiguredManagedUi()
    {
        MsiPackage package = TestPackages.CreateFull();
        UiSettings expected = package.Ui!;

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        Assert.Equal(Wix.WUI.WixUI_Mondo, project.UI);
        Assert.Equal(expected.InstallDialogs.Select(WixEnumMap.MapDialog), project.ManagedUI.InstallDialogs);
        Assert.Equal(expected.ModifyDialogs.Select(WixEnumMap.MapDialog), project.ManagedUI.ModifyDialogs);
    }

    [Fact]
    public void Create_AppliesTheLibraryDefaultManagedUiWhenNoneIsConfigured()
    {
        MsiPackage package = TestPackages.CreateMinimal();
        Assert.Null(package.Ui);
        UiSettings expected = UiSettings.CreateDefault();

        Wix.ManagedProject project = MsiProjectFactory.Create(package);

        Assert.Equal(WixEnumMap.MapWui(expected.Wui), project.UI);
        Assert.Equal(expected.InstallDialogs.Select(WixEnumMap.MapDialog), project.ManagedUI.InstallDialogs);
        Assert.Equal(expected.ModifyDialogs.Select(WixEnumMap.MapDialog), project.ManagedUI.ModifyDialogs);
    }

    /// <summary>
    /// Every shortcut in the project, keyed by the directory path it will be created in — rebuilt from
    /// the nested <c>Dir</c> chain WixSharp expands a path like <c>%ProgramMenu%\Contoso</c> into.
    /// </summary>
    private static Dictionary<string, Wix.ExeFileShortcut> ShortcutsByPath(Wix.ManagedProject project)
        => Walk(project.Dirs)
            .Where(entry => entry.Dir.Shortcuts is { Length: > 0 })
            .ToDictionary(entry => entry.Path, entry => Assert.Single(entry.Dir.Shortcuts));

    private static IEnumerable<(string Path, Wix.Dir Dir)> Walk(IEnumerable<Wix.Dir>? dirs, string prefix = "")
    {
        if (dirs is null)
        {
            yield break;
        }

        foreach (Wix.Dir dir in dirs)
        {
            string path = prefix.Length == 0 ? dir.Name : $@"{prefix}\{dir.Name}";

            yield return (path, dir);

            foreach ((string Path, Wix.Dir Dir) nested in Walk(dir.Dirs, path))
            {
                yield return nested;
            }
        }
    }
}
