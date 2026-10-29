using System;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers the About dialog's ViewModel: what it reports for the running build, and which lines a build
/// with no revision or no copyright leaves out.
/// </summary>
public sealed class AboutViewModelTests
{
    [Fact]
    public void TheParameterlessViewModel_ReportsTheRunningBuild()
    {
        AboutViewModel viewModel = new();

        Assert.Equal(AppInfo.Name, viewModel.DisplayName);
        Assert.Equal(AppInfo.Version, viewModel.Version);
        Assert.Equal(AppInfo.BuildSha, viewModel.BuildSha);
        Assert.Equal(AppInfo.Copyright, viewModel.Copyright);
    }

    [Fact]
    public void AllValues_AreReportedAsGiven()
    {
        AboutViewModel viewModel = new("1.7.0", "3f2a9c1", "Copyright © 2026 Someone");

        Assert.Equal("1.7.0", viewModel.Version);
        Assert.Equal("3f2a9c1", viewModel.BuildSha);
        Assert.True(viewModel.HasBuildSha);
        Assert.Equal("Copyright © 2026 Someone", viewModel.Copyright);
        Assert.True(viewModel.HasCopyright);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ABuildWithNoRevision_HasNoBuildLine(string? buildSha)
    {
        AboutViewModel viewModel = new("1.7.0", buildSha, "Copyright © 2026 Someone");

        Assert.False(viewModel.HasBuildSha);
    }

    [Fact]
    public void AnAssemblyWithNoCopyright_HasNoCopyrightLine()
    {
        AboutViewModel viewModel = new("1.7.0", "3f2a9c1", string.Empty);

        Assert.False(viewModel.HasCopyright);
    }

    [Fact]
    public void Constructor_RejectsAMissingVersion()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new AboutViewModel(null!, "3f2a9c1", string.Empty));

        Assert.Equal("version", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsAMissingCopyright()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new AboutViewModel("1.7.0", "3f2a9c1", null!));

        Assert.Equal("copyright", exception.ParamName);
    }
}
