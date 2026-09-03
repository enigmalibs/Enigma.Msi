using System;
using System.Reflection;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// The app's identity — the strings the splash screen and the About dialog show. One source, so the two
/// cannot disagree about what this application is called or who wrote it.
/// </summary>
/// <remarks>
/// Static rather than a service: none of it is substitutable, none of it depends on anything, and every
/// value is a compile-time constant except <see cref="Version"/>, which is read from the assembly once.
/// </remarks>
public static class AppInfo
{
    /// <summary>The displayed product name — deliberately the library's name, not the assembly's.</summary>
    public const string Name = "Enigma.Msi";

    /// <summary>The one-line description shown under <see cref="Name"/>.</summary>
    public const string Tagline = "Declarative Windows MSI builder";

    /// <summary>Who wrote it.</summary>
    public const string Author = "Josué Clément";

    /// <summary>The copyright line shown in the About dialog.</summary>
    public const string Copyright = "© 2026 Josué Clément";

    /// <summary>The project's repository, opened by the About dialog's button.</summary>
    public const string RepositoryUrl = "https://github.com/enigmalibs/Enigma.Msi";

    /// <summary>
    /// The logo, as an Avalonia resource URI. The <c>Assets/</c> segment is part of the resource's
    /// identity — the folder-less form does not resolve.
    /// </summary>
    public const string LogoUri = "avares://Enigma.Msi.Desktop/Assets/logo.png";

    /// <summary>
    /// The app's version, as declared by <c>&lt;Version&gt;</c> in the csproj, with no build metadata.
    /// Computed once.
    /// </summary>
    public static string Version { get; } = ReadVersion();

    // typeof(AppInfo).Assembly, not GetExecutingAssembly(): the latter answers with whoever is on the
    // stack, which under a test host is not this app.
    private static string ReadVersion()
    {
        Assembly assembly = typeof(AppInfo).Assembly;

        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (informational is not null)
        {
            // The SDK appends +<commit-sha> (the source revision id) to the informational version. It
            // belongs in a crash report, never in a window.
            int metadata = informational.IndexOf('+');
            string release = metadata < 0 ? informational : informational[..metadata];

            if (!string.IsNullOrWhiteSpace(release))
            {
                return release;
            }
        }

        return assembly.GetName().Version?.ToString(3) ?? "unknown";
    }
}
