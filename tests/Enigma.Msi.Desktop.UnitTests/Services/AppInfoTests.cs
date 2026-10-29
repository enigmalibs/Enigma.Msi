using System;
using Enigma.Msi.Desktop.Services;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Services;

/// <summary>
/// Covers the app's identity: the constants the About dialog shows, and the version, revision and
/// copyright read out of the assembly.
/// </summary>
public sealed class AppInfoTests
{
    [Fact]
    public void Name_IsTheDisplayedProductName_NotTheAssemblyName()
    {
        Assert.Equal("Enigma.Msi", AppInfo.Name);
    }

    [Fact]
    public void Version_IsReported()
    {
        Assert.False(string.IsNullOrWhiteSpace(AppInfo.Version));
    }

    [Fact]
    public void Version_CarriesNoBuildMetadata()
    {
        // The SDK appends +<commit-sha> to the informational version. It must never reach the version line.
        Assert.DoesNotContain("+", AppInfo.Version, StringComparison.Ordinal);
    }

    [Fact]
    public void Copyright_IsTheOneDirectoryBuildPropsStamps()
    {
        Assert.Equal("Copyright © 2026 Josué Clément", AppInfo.Copyright);
    }

    [Fact]
    public void ParseVersion_SplitsTheVersionFromTheRevision_AndShortensTheRevision()
    {
        (string version, string? buildSha) = AppInfo.ParseVersion(
            "1.7.0+3f2a9c1d4e5b6a7980f1e2d3c4b5a69788776655",
            "1.7.0");

        Assert.Equal("1.7.0", version);
        Assert.Equal("3f2a9c1", buildSha);
    }

    [Fact]
    public void ParseVersion_KeepsARevisionAlreadyShort()
    {
        (_, string? buildSha) = AppInfo.ParseVersion("1.7.0+abc12", "1.7.0");

        Assert.Equal("abc12", buildSha);
    }

    [Theory]
    [InlineData("1.7.0")]
    [InlineData("1.7.0+")]
    [InlineData("1.7.0+   ")]
    public void ParseVersion_ReportsNoRevision_WhenTheBuildRecordedNone(string informational)
    {
        (string version, string? buildSha) = AppInfo.ParseVersion(informational, "9.9.9");

        Assert.Equal("1.7.0", version);
        Assert.Null(buildSha);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("+3f2a9c1")]
    public void ParseVersion_FallsBack_WhenTheInformationalVersionSaysNothingUsable(string? informational)
    {
        (string version, string? buildSha) = AppInfo.ParseVersion(informational, "1.6.0");

        Assert.Equal("1.6.0", version);
        Assert.Null(buildSha);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ParseVersion_SaysUnknown_WhenThereIsNoVersionAtAll(string? fallback)
    {
        (string version, string? buildSha) = AppInfo.ParseVersion(null, fallback);

        Assert.Equal(AppInfo.UnknownVersion, version);
        Assert.Null(buildSha);
    }
}
