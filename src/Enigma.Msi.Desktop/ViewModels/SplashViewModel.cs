using Enigma.Msi.Desktop.Services;

namespace Enigma.Msi.Desktop.ViewModels;

/// <summary>
/// What the splash screen shows while the app starts: the identity, and nothing else.
/// </summary>
/// <remarks>
/// No commands and no dependencies. The splash's only interaction is being dismissed, and dismissal is
/// the window's own concern — a ViewModel cannot close a window it does not know about.
/// </remarks>
public sealed class SplashViewModel
{
    /// <summary>The product name.</summary>
    public string Name => AppInfo.Name;

    /// <summary>The one-line description under the name.</summary>
    public string Tagline => AppInfo.Tagline;

    /// <summary>The running version.</summary>
    public string Version => AppInfo.Version;

    /// <summary>Who wrote it.</summary>
    public string Author => AppInfo.Author;
}
