using System;
using System.IO;
using Avalonia.Platform;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// The app icon as a file beside the executable, where shortcuts, launchers and installers look for it.
/// </summary>
/// <remarks>
/// The app's copy-to-output item flows into this suite's output as it does into a publish, so the file
/// beside the test assembly is the one the app ships. It is compared with the embedded asset rather than
/// with the source tree, so the test does not depend on where the repository is checked out.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class AppIconTests(HeadlessAvaloniaFixture headless)
{
    private static readonly string IconFile = Path.Combine(AppContext.BaseDirectory, "app.ico");

    [Fact]
    public void TheIcon_IsWrittenBesideTheExecutable_AsAppIco()
    {
        Assert.True(File.Exists(IconFile), $"No app.ico in '{AppContext.BaseDirectory}'.");
    }

    [Fact]
    public void TheIconFile_IsTheEmbeddedAssetByteForByte()
    {
        byte[] embedded = headless.Run(() =>
        {
            using Stream asset = AssetLoader.Open(new Uri("avares://Enigma.Msi.Desktop/Assets/appicon.ico"));
            using MemoryStream bytes = new();
            asset.CopyTo(bytes);
            return bytes.ToArray();
        });

        Assert.Equal(embedded, File.ReadAllBytes(IconFile));
    }
}
