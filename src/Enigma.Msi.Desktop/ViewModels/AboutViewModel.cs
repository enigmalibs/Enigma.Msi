using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Msi.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// The About dialog: the app's identity, plus the one thing it can do — open the repository.
/// </summary>
public sealed partial class AboutViewModel : ObservableObject
{
    private readonly IUrlLauncherService _urlLauncher;
    private readonly ILogger<AboutViewModel> _logger;

    /// <summary>Creates the dialog's ViewModel.</summary>
    /// <param name="urlLauncher">Opens the repository URL in the machine's browser.</param>
    /// <param name="logger">Records a launch the shell refused.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public AboutViewModel(IUrlLauncherService urlLauncher, ILogger<AboutViewModel> logger)
    {
        _urlLauncher = urlLauncher ?? throw new ArgumentNullException(nameof(urlLauncher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>The product name.</summary>
    public string Name => AppInfo.Name;

    /// <summary>The one-line description under the name.</summary>
    public string Tagline => AppInfo.Tagline;

    /// <summary>The running version.</summary>
    public string Version => AppInfo.Version;

    /// <summary>The copyright line.</summary>
    public string Copyright => AppInfo.Copyright;

    /// <summary>Who wrote it.</summary>
    public string Author => AppInfo.Author;

    /// <summary>
    /// The repository's URL. Shown as selectable text as well as behind the button, so it can be copied
    /// by hand when there is no browser to open it with.
    /// </summary>
    public string RepositoryUrl => AppInfo.RepositoryUrl;

    /// <summary>Opens the repository in the machine's browser.</summary>
    [RelayCommand]
    private async Task OpenRepositoryAsync()
    {
        // A machine with no http association is not an error to interrupt the user with: the URL is on
        // screen and selectable, which is the fallback.
        if (!await _urlLauncher.LaunchAsync(AppInfo.RepositoryUrl).ConfigureAwait(true))
        {
            _logger.LogWarning("Could not open {Url} — no browser accepted it.", AppInfo.RepositoryUrl);
        }
    }
}
