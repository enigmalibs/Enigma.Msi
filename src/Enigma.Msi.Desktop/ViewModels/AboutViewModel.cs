using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.Msi.Desktop.Services;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// What the About dialog says: which product this is, and which build of it is running. The same shape
/// as every Enigma app's About dialog.
/// </summary>
/// <remarks>
/// Read-only: nothing here changes while the dialog is open. It derives from <see cref="ObservableObject"/>
/// anyway, so it is the same shape as every other ViewModel in this folder.
/// </remarks>
public sealed class AboutViewModel : ObservableObject
{
    /// <summary>Creates the ViewModel over the running build.</summary>
    public AboutViewModel()
        : this(AppInfo.Version, AppInfo.BuildSha, AppInfo.Copyright)
    {
    }

    /// <summary>Creates the ViewModel over given values.</summary>
    /// <param name="version">The version to show.</param>
    /// <param name="buildSha">The short revision to show, or <see langword="null"/> to show no build line.</param>
    /// <param name="copyright">The copyright notice, or an empty string to show no copyright line.</param>
    /// <exception cref="ArgumentNullException"><paramref name="version"/> or <paramref name="copyright"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The values are arguments rather than read here because <see cref="AppInfo"/> reports the running
    /// assembly, which a test cannot vary: this is the seam that lets a build with no revision be asserted.
    /// </remarks>
    public AboutViewModel(string version, string? buildSha, string copyright)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        BuildSha = buildSha;
        Copyright = copyright ?? throw new ArgumentNullException(nameof(copyright));
    }

    /// <summary>The product's name.</summary>
    public string DisplayName => AppInfo.Name;

    /// <summary>The version of the running build.</summary>
    public string Version { get; }

    /// <summary>The short commit this build was cut from, or <see langword="null"/> if it recorded none.</summary>
    public string? BuildSha { get; }

    /// <summary>
    /// Whether there is a revision to show: a build with none shows no build line at all, not an empty one.
    /// </summary>
    public bool HasBuildSha => !string.IsNullOrEmpty(BuildSha);

    /// <summary>The copyright notice, or an empty string if the assembly carries none.</summary>
    public string Copyright { get; }

    /// <summary>Whether there is a copyright notice to show.</summary>
    public bool HasCopyright => Copyright.Length > 0;
}
