using System;
using System.Reflection;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// The app's identity — the strings the About dialog shows, kept in one place rather than spread through
/// its markup and ViewModel.
/// </summary>
/// <remarks>
/// Static rather than a service: none of it is substitutable and none of it depends on anything. The
/// version, the build revision and the copyright are read from this assembly once.
/// </remarks>
public static class AppInfo
{
    /// <summary>The displayed product name — deliberately the library's name, not the assembly's.</summary>
    public const string Name = "Enigma.Msi";

    /// <summary>What <see cref="Version"/> says when the assembly carries no version at all.</summary>
    public const string UnknownVersion = "0.0.0";

    // A short sha is what a bug report quotes and what git accepts; the full forty characters are noise.
    private const int ShaLength = 7;

    // typeof(AppInfo).Assembly, not GetExecutingAssembly(): the latter answers with whoever is on the
    // stack, which under a test host is not this app.
    private static readonly Assembly ThisAssembly = typeof(AppInfo).Assembly;

    private static readonly (string Version, string? BuildSha) Product = ParseVersion(
        ThisAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        FallbackVersion());

    /// <summary>
    /// The app's version, as declared by <c>&lt;Version&gt;</c> in the csproj, with no build metadata.
    /// </summary>
    public static string Version => Product.Version;

    /// <summary>
    /// The short commit this build was cut from, or <see langword="null"/> when it recorded none — a build
    /// from a source drop rather than from the repository.
    /// </summary>
    public static string? BuildSha => Product.BuildSha;

    /// <summary>
    /// The copyright notice <c>Directory.Build.props</c> stamps on the assembly, or an empty string when it
    /// carries none.
    /// </summary>
    public static string Copyright { get; } =
        ThisAssembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright?.Trim() ?? string.Empty;

    /// <summary>
    /// Splits an informational version into the displayable version and the short revision the SDK
    /// appends after a <c>+</c>.
    /// </summary>
    /// <param name="informationalVersion">The assembly's informational version, e.g. <c>1.7.0+3f2a9c1…</c>.</param>
    /// <param name="fallbackVersion">What to show when the informational version says nothing usable.</param>
    /// <returns>The version, never empty, and the revision shortened to seven characters, or <see langword="null"/>.</returns>
    /// <remarks>
    /// Public so the parse can be tested: the running assembly's own attribute is fixed for a test run, so
    /// a build with no revision could never be asserted through <see cref="Version"/> alone.
    /// </remarks>
    public static (string Version, string? BuildSha) ParseVersion(string? informationalVersion, string? fallbackVersion)
    {
        string candidate = informationalVersion?.Trim() ?? string.Empty;

        if (candidate.Length > 0)
        {
            int separator = candidate.IndexOf('+', StringComparison.Ordinal);
            string version = separator < 0 ? candidate : candidate[..separator];

            if (version.Length > 0)
            {
                return (version, Shorten(separator < 0 ? null : candidate[(separator + 1)..]));
            }
        }

        string fallback = fallbackVersion?.Trim() ?? string.Empty;

        return (fallback.Length > 0 ? fallback : UnknownVersion, null);
    }

    private static string? Shorten(string? revision)
    {
        string trimmed = revision?.Trim() ?? string.Empty;

        return trimmed.Length switch
        {
            0 => null,
            <= ShaLength => trimmed,
            _ => trimmed[..ShaLength],
        };
    }

    // The plain assembly version, trimmed to three components: the fourth is a zero nobody wrote.
    private static string? FallbackVersion() => ThisAssembly.GetName().Version?.ToString(3);
}
