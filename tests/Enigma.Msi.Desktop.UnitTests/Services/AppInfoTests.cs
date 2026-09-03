using System;
using Enigma.Msi.Desktop.Services;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Services;

/// <summary>
/// Covers the app's identity: the constants the splash and the About dialog show, and the version read
/// out of the assembly.
/// </summary>
public sealed class AppInfoTests
{
    [Fact]
    public void Identity_IsTheDisplayedProductName_NotTheAssemblyName()
    {
        Assert.Equal("Enigma.Msi", AppInfo.Name);
        Assert.Equal("Declarative Windows MSI builder", AppInfo.Tagline);
        Assert.Equal("Josué Clément", AppInfo.Author);
        Assert.Equal("© 2026 Josué Clément", AppInfo.Copyright);
        Assert.Equal("https://github.com/enigmalibs/Enigma.Msi", AppInfo.RepositoryUrl);
    }

    [Fact]
    public void Version_IsReported()
    {
        Assert.False(string.IsNullOrWhiteSpace(AppInfo.Version));
    }

    [Fact]
    public void Version_CarriesNoBuildMetadata()
    {
        // The SDK appends +<commit-sha> to the informational version. It must never reach the UI.
        Assert.DoesNotContain("+", AppInfo.Version, StringComparison.Ordinal);
    }

    [Fact]
    public void LogoUri_IsAnAvaresUriNamingThisAssembly()
    {
        Assert.True(Uri.TryCreate(AppInfo.LogoUri, UriKind.Absolute, out Uri? uri));
        Assert.Equal("avares", uri!.Scheme);

        // Uri lower-cases the authority, so the assembly name is compared case-insensitively.
        Assert.Equal(
            typeof(AppInfo).Assembly.GetName().Name,
            uri.Host,
            StringComparer.OrdinalIgnoreCase);

        // The Assets/ segment is part of the resource's identity — the folder-less form does not resolve.
        Assert.EndsWith("/Assets/logo.png", AppInfo.LogoUri, StringComparison.Ordinal);
    }
}
