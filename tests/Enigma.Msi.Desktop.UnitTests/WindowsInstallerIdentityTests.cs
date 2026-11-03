using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Enigma.Msi.Desktop.Services;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// The Windows installer's identity, written as constants in <c>packaging/windows/buildmsi.ps1</c>, which
/// nothing in the build reads: the upgrade code that makes an MSI replace the installed Enigma Msi rather
/// than install beside it, the product's name, the executable its shortcuts start, and the version it is
/// stamped with.
/// </summary>
/// <remarks>
/// The script is read from the repository, found by its solution file above the test's output, so the
/// tests do not depend on where the repository is checked out.
/// </remarks>
public sealed class WindowsInstallerIdentityTests
{
    /// <summary>
    /// The upgrade code of the MSI that installed 1.7.0, read from its package: every later MSI must keep
    /// it.
    /// </summary>
    private const string UpgradeCode = "49589519-f722-4733-af70-81e027756344";

    /// <summary>The installer's product name, also its install folder and its shortcuts' name.</summary>
    private const string ProductName = "Enigma Msi";

    [Fact]
    public void UpgradeCode_IsTheOneOfTheInstalledReleases()
    {
        Assert.Contains($"$UpgradeCode = '{UpgradeCode}'", BuildMsiScript());
    }

    [Fact]
    public void ProductName_IsEnigmaMsi()
    {
        Assert.Contains($"$ProductName = '{ProductName}'", BuildMsiScript());
    }

    [Fact]
    public void Shortcuts_StartTheAppsOwnExecutable()
    {
        // The shortcuts' target, the publish folder and the csproj the version is read from all take this one line.
        string? assembly = typeof(AppInfo).Assembly.GetName().Name;

        Assert.Contains($"$Assembly = '{assembly}'", BuildMsiScript());
    }

    [Fact]
    public void Version_IsTheAppsCsprojVersion_WhichTheAssemblyCarries()
    {
        // buildmsi.ps1 reads /Project/PropertyGroup/Version from the app's csproj: it must find exactly one.
        string csproj = Path.Combine(Root(), "src", "Enigma.Msi.Desktop", "Enigma.Msi.Desktop.csproj");

        string[] versions = [.. XDocument.Load(csproj).Root!.Elements("PropertyGroup").Elements("Version").Select(version => version.Value.Trim())];

        Assert.Equal([AppInfo.Version], versions);
        Assert.Matches(@"^\d+\.\d+\.\d+$", AppInfo.Version);
    }

    private static string[] BuildMsiScript() =>
        File.ReadAllLines(Path.Combine(Root(), "packaging", "windows", "buildmsi.ps1"));

    /// <summary>
    /// The repository's root: the first directory above the test's output that holds the solution.
    /// </summary>
    private static string Root()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Enigma.Msi.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root was not found above " + AppContext.BaseDirectory);
    }
}
