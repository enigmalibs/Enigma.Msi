using System;
using System.Threading.Tasks;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers the About dialog's ViewModel: the identity it reports, and the one thing it does — opening the
/// repository through a substituted launcher, so no test ever starts a browser.
/// </summary>
public sealed class AboutViewModelTests
{
    private readonly IUrlLauncherService _urlLauncher = Substitute.For<IUrlLauncherService>();

    [Fact]
    public void Properties_ReportTheAppsIdentity()
    {
        AboutViewModel viewModel = CreateViewModel();

        Assert.Equal(AppInfo.Name, viewModel.Name);
        Assert.Equal(AppInfo.Tagline, viewModel.Tagline);
        Assert.Equal(AppInfo.Version, viewModel.Version);
        Assert.Equal(AppInfo.Copyright, viewModel.Copyright);
        Assert.Equal(AppInfo.Author, viewModel.Author);
        Assert.Equal(AppInfo.RepositoryUrl, viewModel.RepositoryUrl);
    }

    [Fact]
    public async Task OpenRepository_LaunchesExactlyTheRepositoryUrl_Once()
    {
        AboutViewModel viewModel = CreateViewModel();
        _urlLauncher.LaunchAsync(Arg.Any<string>()).Returns(true);

        await viewModel.OpenRepositoryCommand.ExecuteAsync(null);

        _ = _urlLauncher.Received(1).LaunchAsync(AppInfo.RepositoryUrl);
        _ = _urlLauncher.DidNotReceive().LaunchAsync(Arg.Is<string>(url => url != AppInfo.RepositoryUrl));
    }

    [Fact]
    public async Task OpenRepository_ARefusedLaunchIsSwallowed()
    {
        AboutViewModel viewModel = CreateViewModel();
        _urlLauncher.LaunchAsync(Arg.Any<string>()).Returns(false);

        // A machine with no browser association is not an error to interrupt the user with: the command
        // logs and completes, and the URL stays on screen to be copied by hand.
        await viewModel.OpenRepositoryCommand.ExecuteAsync(null);

        Assert.Null(viewModel.OpenRepositoryCommand.ExecutionTask?.Exception);
    }

    [Fact]
    public void Constructor_RejectsAMissingUrlLauncher()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new AboutViewModel(null!, NullLogger<AboutViewModel>.Instance));

        Assert.Equal("urlLauncher", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsAMissingLogger()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new AboutViewModel(_urlLauncher, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    private AboutViewModel CreateViewModel()
        => new(_urlLauncher, NullLogger<AboutViewModel>.Instance);
}
