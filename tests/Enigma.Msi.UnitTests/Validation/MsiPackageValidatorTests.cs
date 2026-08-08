using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;
using Xunit;

namespace Enigma.Msi.UnitTests.Validation;

/// <summary>
/// Covers the in-memory rule set: one test per rule, plus the aggregation guarantee — a package that
/// is wrong in many ways reports <em>every</em> violation, each with its member path.
/// </summary>
public sealed class MsiPackageValidatorTests
{
    private readonly IMsiPackageValidator _validator = new MsiPackageValidator();

    [Fact]
    public void MinimalPackage_IsValid()
    {
        MsiValidationResult result = _validator.Validate(TestPackages.CreateMinimalValid());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FullPackage_IsValid()
        => Assert.True(_validator.Validate(TestPackages.CreateFull()).IsValid);

    [Fact]
    public void Validate_IgnoresTheFileSystem()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install.ReleasePath = @"Z:\definitely\does\not\exist";
        package.Output.OutputPath = @"Z:\definitely\does\not\exist\either";

        Assert.True(_validator.Validate(package).IsValid);
    }

    [Fact]
    public void SchemaVersion_BelowOne_IsReported()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.SchemaVersion = 0;

        AssertSingleError(package, "schemaVersion");
    }

    [Fact]
    public void SchemaVersion_FromTheFuture_IsReported()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.SchemaVersion = MsiPackage.CurrentSchemaVersion + 1;

        MsiValidationError error = AssertSingleError(package, "schemaVersion");
        Assert.Contains("Unsupported schema version", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AppName_IsRequired()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.AppName = "   ";

        AssertSingleError(package, "appName");
    }

    [Fact]
    public void Manufacturer_IsRequired()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Manufacturer = string.Empty;

        AssertSingleError(package, "manufacturer");
    }

    [Fact]
    public void Version_IsRequired()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Version = null;

        AssertSingleError(package, "version");
    }

    [Theory]
    [InlineData(256, 0, 0)]
    [InlineData(1, 256, 0)]
    [InlineData(1, 0, 65536)]
    public void Version_OutsideTheWindowsInstallerRange_IsReported(int major, int minor, int build)
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Version = new Version(major, minor, build);

        MsiValidationError error = AssertSingleError(package, "version");
        Assert.Contains("Windows Installer", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(255, 255, 65535)]
    [InlineData(0, 0, 0)]
    public void Version_AtTheWindowsInstallerLimits_IsAccepted(int major, int minor, int build)
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Version = new Version(major, minor, build);

        Assert.True(_validator.Validate(package).IsValid);
    }

    [Fact]
    public void ProductId_MustNotBeEmpty()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.ProductId = Guid.Empty;

        AssertSingleError(package, "productId");
    }

    [Fact]
    public void UpgradeCode_MustNotBeEmpty()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.UpgradeCode = Guid.Empty;

        AssertSingleError(package, "upgradeCode");
    }

    [Fact]
    public void Scope_MustBeAKnownValue()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Scope = (InstallScope)42;

        AssertSingleError(package, "scope");
    }

    [Fact]
    public void Compression_MustBeAKnownValue()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Compression = (CompressionLevel)42;

        AssertSingleError(package, "compression");
    }

    [Fact]
    public void Install_MustNotBeNull()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install = null!;

        AssertSingleError(package, "install");
    }

    [Fact]
    public void InstallPath_IsRequired()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install.InstallPath = string.Empty;

        AssertSingleError(package, "install.installPath");
    }

    [Fact]
    public void ReleasePath_IsRequired()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install.ReleasePath = string.Empty;

        AssertSingleError(package, "install.releasePath");
    }

    [Fact]
    public void Output_MustNotBeNull()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Output = null!;

        AssertSingleError(package, "output");
    }

    [Fact]
    public void OutputPath_IsRequired()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Output.OutputPath = "  ";

        AssertSingleError(package, "output.outputPath");
    }

    [Fact]
    public void MsiFilename_IsRequired()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Output.MsiFilename = string.Empty;

        AssertSingleError(package, "output.msiFilename");
    }

    [Fact]
    public void MsiFilename_MustNotCarryTheMsiExtension()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Output.MsiFilename = "Widget.MSI";

        MsiValidationError error = AssertSingleError(package, "output.msiFilename");
        Assert.Contains(".msi extension", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MsiFilename_MustNotBeAPath()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Output.MsiFilename = @"sub\Widget";

        MsiValidationError error = AssertSingleError(package, "output.msiFilename");
        Assert.Contains("plain file name", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Shortcuts_MustNotBeNull()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Shortcuts = null!;

        AssertSingleError(package, "shortcuts");
    }

    [Fact]
    public void Shortcut_ReportsEveryMissingMemberWithItsIndex()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Shortcuts.Add(new Shortcut { ShortcutPath = "%Desktop%", ShortcutName = "Widget", TargetPath = "[INSTALLDIR]\\Widget.exe" });
        package.Shortcuts.Add(new Shortcut());

        AssertErrorPaths(
            _validator.Validate(package),
            "shortcuts[1].shortcutPath",
            "shortcuts[1].shortcutName",
            "shortcuts[1].targetPath");
    }

    [Fact]
    public void Ui_RequiresBothDialogSequences()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Ui = new UiSettings();

        AssertErrorPaths(_validator.Validate(package), "ui.installDialogs", "ui.modifyDialogs");
    }

    [Fact]
    public void Ui_RejectsUnknownDialogsAndDialogSets()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Ui = UiSettings.CreateDefault();
        package.Ui.Wui = (Wui)77;
        package.Ui.ModifyDialogs[1] = (Dialog)55;

        AssertErrorPaths(_validator.Validate(package), "ui.wui", "ui.modifyDialogs[1]");
    }

    [Fact]
    public void Ui_FromCreateDefault_IsValid()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Ui = UiSettings.CreateDefault();

        Assert.True(_validator.Validate(package).IsValid);
    }

    [Fact]
    public void MultiInvalidPackage_ReportsEveryViolationWithItsMemberPath()
    {
        var package = new MsiPackage
        {
            SchemaVersion = MsiPackage.CurrentSchemaVersion + 6,
            AppName = "   ",
            Manufacturer = string.Empty,
            Version = new Version(300, 1),
            Scope = (InstallScope)42,
            Compression = (CompressionLevel)99,
            Install = new InstallSettings(),
            Output = new OutputSettings { MsiFilename = "Widget.msi" },
            Shortcuts = [new Shortcut()],
            Ui = new UiSettings { Wui = (Wui)77, ModifyDialogs = [(Dialog)55] }
        };

        AssertErrorPaths(
            _validator.Validate(package),
            "schemaVersion",
            "appName",
            "manufacturer",
            "version",
            "productId",
            "upgradeCode",
            "scope",
            "compression",
            "install.installPath",
            "install.releasePath",
            "output.outputPath",
            "output.msiFilename",
            "shortcuts[0].shortcutPath",
            "shortcuts[0].shortcutName",
            "shortcuts[0].targetPath",
            "ui.wui",
            "ui.installDialogs",
            "ui.modifyDialogs[0]");
    }

    [Fact]
    public void ErrorToString_RendersPathAndMessage()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.AppName = string.Empty;

        Assert.Equal("appName: Required.", AssertSingleError(package, "appName").ToString());
    }

    [Fact]
    public void ValidatorMethods_RejectANullPackage()
    {
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(null!));
        Assert.Throws<ArgumentNullException>(() => _validator.ValidateEnvironment(null!));
        Assert.Throws<ArgumentNullException>(() => _validator.ValidateAll(null!));
    }

    private MsiValidationError AssertSingleError(MsiPackage package, string expectedPath)
    {
        MsiValidationResult result = _validator.Validate(package);

        Assert.False(result.IsValid);
        MsiValidationError error = Assert.Single(result.Errors);
        Assert.Equal(expectedPath, error.Path);

        return error;
    }

    private static void AssertErrorPaths(MsiValidationResult result, params string[] expectedPaths)
    {
        IEnumerable<string> actual = result.Errors.Select(error => error.Path);

        Assert.Equal(expectedPaths, actual);
    }
}
