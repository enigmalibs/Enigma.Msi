using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Desktop.Views;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Views;

/// <summary>
/// The About card, through the real markup: what it shows, which lines a build leaves out, and that its
/// brushes resolve in both theme variants.
/// </summary>
/// <remarks>
/// The card is hosted in a plain window rather than the control library's dialog: the dialog's chrome is
/// the library's to test, and these assert the card every Enigma app shares. Whether it looks right next
/// to Enigma.GitClient's is a manual reading, recorded in the completion doc.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class AboutCardTests(HeadlessAvaloniaFixture headless)
{
    [Fact]
    public void TheCard_ShowsTheNameTheVersionTheBuildAndTheCopyright()
    {
        headless.Run(() =>
        {
            AboutCard card = Show(new AboutViewModel("1.7.0", "3f2a9c1", "Copyright © 2026 Someone"));

            string[] texts = Texts(card);

            Assert.Contains("Enigma.Msi", texts);
            Assert.Contains("Version", texts);
            Assert.Contains("1.7.0", texts);
            Assert.Contains("Build", texts);
            Assert.Contains("3f2a9c1", texts);
            Assert.Contains("Copyright © 2026 Someone", texts);
            Assert.True(card.FindControl<StackPanel>("BuildLine")!.IsVisible);
            Assert.True(card.FindControl<TextBlock>("Copyright")!.IsVisible);
        });
    }

    [Fact]
    public void TheCard_HasTheReferenceSize()
    {
        headless.Run(() =>
        {
            AboutCard card = Show(new AboutViewModel("1.7.0", "3f2a9c1", "Copyright © 2026 Someone"));

            Image logo = Assert.Single(card.GetLogicalDescendants().OfType<Image>());

            Assert.Equal(380d, card.Width);
            Assert.Equal(96d, logo.Width);
            Assert.Equal(96d, logo.Height);
        });
    }

    [Fact]
    public void TheLogo_IsAResourceThatResolves()
    {
        headless.Run(() =>
        {
            AboutCard card = Show(new AboutViewModel("1.7.0", null, string.Empty));

            // A missing or unreadable AvaloniaResource fails here rather than silently drawing nothing:
            // the Source converter opens the asset as the markup is loaded.
            Image logo = Assert.Single(card.GetLogicalDescendants().OfType<Image>());

            Assert.NotNull(logo.Source);
        });
    }

    [Fact]
    public void ABuildWithNoRevision_HidesTheBuildLine()
    {
        headless.Run(() =>
        {
            AboutCard card = Show(new AboutViewModel("1.7.0", null, "Copyright © 2026 Someone"));

            Assert.False(card.FindControl<StackPanel>("BuildLine")!.IsVisible);
        });
    }

    [Fact]
    public void AnEmptyCopyright_HidesTheCopyrightLine()
    {
        headless.Run(() =>
        {
            AboutCard card = Show(new AboutViewModel("1.7.0", "3f2a9c1", string.Empty));

            Assert.False(card.FindControl<TextBlock>("Copyright")!.IsVisible);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EachLine_TakesItsBrushFromThePalette_InEitherVariant(bool dark)
    {
        headless.Run(() =>
        {
            AboutCard card = Show(
                new AboutViewModel("1.7.0", "3f2a9c1", "Copyright © 2026 Someone"),
                dark ? ThemeVariant.Dark : ThemeVariant.Light);

            // Each colour is a DynamicResource Enigma* key. A renamed or missing key does not fail the
            // markup: the line silently inherits the window's foreground. Comparing every line with the
            // key it names is what catches that.
            AssertForeground(card, "productName", "EnigmaForegroundBrush");
            AssertForeground(card, "version", "EnigmaForegroundSecondaryBrush");
            AssertForeground(card, "build", "EnigmaForegroundTertiaryBrush");
            AssertForeground(card, "copyright", "EnigmaForegroundSecondaryBrush");
        });
    }

    private static void AssertForeground(AboutCard card, string className, string key)
    {
        Assert.True(card.TryFindResource(key, card.ActualThemeVariant, out object? resource), $"{key} does not resolve.");
        Color expected = Assert.IsAssignableFrom<ISolidColorBrush>(resource).Color;

        TextBlock[] blocks = [.. card.GetLogicalDescendants().OfType<TextBlock>().Where(block => block.Classes.Contains(className))];

        Assert.NotEmpty(blocks);

        foreach (TextBlock block in blocks)
        {
            Assert.Equal(expected, Assert.IsAssignableFrom<ISolidColorBrush>(block.Foreground).Color);
        }
    }

    private static string[] Texts(AboutCard card)
        => [.. card.GetLogicalDescendants().OfType<TextBlock>().Select(block => block.Text ?? string.Empty)];

    private static AboutCard Show(AboutViewModel viewModel, ThemeVariant? variant = null)
    {
        AboutCard card = new() { DataContext = viewModel };

        Window window = new()
        {
            Content = card,
            RequestedThemeVariant = variant ?? ThemeVariant.Light,
        };

        window.Show();

        return card;
    }
}
