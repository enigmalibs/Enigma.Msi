using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;
using Xunit;

namespace Enigma.Msi.UnitTests.Validation;

/// <summary>
/// Covers the environment rule set — the directories and files a package refers to must exist. These
/// are the rules a UI must <em>not</em> run on every keystroke, which is why they live behind their
/// own method.
/// </summary>
public sealed class MsiPackageValidatorEnvironmentTests : IDisposable
{
    private readonly IMsiPackageValidator _validator = new MsiPackageValidator();
    private readonly string _root;
    private readonly string _releaseDirectory;
    private readonly string _outputDirectory;
    private readonly string _iconFile;

    public MsiPackageValidatorEnvironmentTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "enigma-msi-" + Guid.NewGuid().ToString("N"));
        _releaseDirectory = Path.Combine(_root, "release");
        _outputDirectory = Path.Combine(_root, "artifacts");
        _iconFile = Path.Combine(_root, "app.ico");

        Directory.CreateDirectory(_releaseDirectory);
        Directory.CreateDirectory(_outputDirectory);
        File.WriteAllText(Path.Combine(_releaseDirectory, "Widget.exe"), "not really an executable");
        File.WriteAllText(_iconFile, "not really an icon");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void ExistingPaths_Pass()
        => Assert.True(_validator.ValidateEnvironment(CreatePackage()).IsValid);

    [Fact]
    public void MissingReleaseDirectory_IsReported()
    {
        MsiPackage package = CreatePackage();
        package.Install.ReleasePath = Path.Combine(_root, "no-such-folder");

        MsiValidationError error = AssertSingleError(package, "install.releasePath");
        Assert.Equal("Directory does not exist.", error.Message);
    }

    [Fact]
    public void ReleasePathPointingAtAFile_IsReported()
    {
        MsiPackage package = CreatePackage();
        package.Install.ReleasePath = _iconFile;

        MsiValidationError error = AssertSingleError(package, "install.releasePath");
        Assert.Contains("Expected a directory", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyReleaseDirectory_IsReported()
    {
        MsiPackage package = CreatePackage();
        package.Install.ReleasePath = Directory.CreateDirectory(Path.Combine(_root, "empty")).FullName;

        MsiValidationError error = AssertSingleError(package, "install.releasePath");
        Assert.Contains("nothing to package", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingOutputDirectory_IsReported()
    {
        MsiPackage package = CreatePackage();
        package.Output.OutputPath = Path.Combine(_root, "no-such-folder");

        AssertSingleError(package, "output.outputPath");
    }

    [Fact]
    public void EmptyOutputDirectory_IsAccepted()
    {
        MsiPackage package = CreatePackage();
        package.Output.OutputPath = Directory.CreateDirectory(Path.Combine(_root, "empty-output")).FullName;

        Assert.True(_validator.ValidateEnvironment(package).IsValid);
    }

    [Fact]
    public void MissingProductIcon_IsReported()
    {
        MsiPackage package = CreatePackage();
        package.ControlPanel = new ControlPanelInfo { ProductIcon = Path.Combine(_root, "missing.ico") };

        MsiValidationError error = AssertSingleError(package, "controlPanel.productIcon");
        Assert.Equal("File does not exist.", error.Message);
    }

    [Fact]
    public void ExistingProductIcon_IsAccepted()
    {
        MsiPackage package = CreatePackage();
        package.ControlPanel = new ControlPanelInfo { ProductIcon = _iconFile };

        Assert.True(_validator.ValidateEnvironment(package).IsValid);
    }

    [Fact]
    public void MissingShortcutIcon_IsReportedWithItsIndex()
    {
        MsiPackage package = CreatePackage();
        package.Shortcuts.Add(new Shortcut { IconPath = _iconFile });
        package.Shortcuts.Add(new Shortcut { IconPath = Path.Combine(_root, "missing.ico") });

        AssertSingleError(package, "shortcuts[1].iconPath");
    }

    [Fact]
    public void BlankPaths_AreLeftToTheInMemoryRules()
    {
        MsiPackage package = CreatePackage();
        package.Install.ReleasePath = string.Empty;
        package.Output.OutputPath = "   ";

        Assert.True(_validator.ValidateEnvironment(package).IsValid);
    }

    [Fact]
    public void ValidateAll_ReportsInMemoryAndEnvironmentViolationsTogether()
    {
        MsiPackage package = CreatePackage();
        package.AppName = string.Empty;
        package.Install.ReleasePath = Path.Combine(_root, "no-such-folder");

        IEnumerable<string> paths = _validator.ValidateAll(package).Errors.Select(error => error.Path);

        Assert.Equal(new[] { "appName", "install.releasePath" }, paths);
    }

    [Fact]
    public void ValidateAll_OnAFullyValidPackage_Passes()
        => Assert.True(_validator.ValidateAll(CreatePackage()).IsValid);

    private MsiPackage CreatePackage()
    {
        MsiPackage package = TestPackages.CreateMinimalValid();
        package.Install.ReleasePath = _releaseDirectory;
        package.Output.OutputPath = _outputDirectory;

        return package;
    }

    private MsiValidationError AssertSingleError(MsiPackage package, string expectedPath)
    {
        MsiValidationResult result = _validator.ValidateEnvironment(package);

        Assert.False(result.IsValid);
        MsiValidationError error = Assert.Single(result.Errors);
        Assert.Equal(expectedPath, error.Path);

        return error;
    }
}
