using System;
using System.IO;
using Avalonia.Controls;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Desktop.Views;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Views;

/// <summary>
/// The safety net's dialog content, through the real markup: what it says, which lines one error leaves
/// out, and that the details start collapsed but complete.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class UnhandledErrorCardTests(HeadlessAvaloniaFixture headless)
{
    [Fact]
    public void TheCard_SaysTheAppIsStillRunning_AndWhatWentWrong()
    {
        headless.Run(() =>
        {
            UnhandledErrorCard card = Show(new IOException("The disk is full."));

            Assert.StartsWith("Something went wrong", card.FindControl<TextBlock>("LeadText")!.Text, StringComparison.Ordinal);
            Assert.Equal("The disk is full.", card.FindControl<TextBlock>("MessageText")!.Text);
        });
    }

    [Fact]
    public void ASingleError_HidesTheMoreLine()
    {
        headless.Run(() =>
        {
            UnhandledErrorCard card = Show(new IOException("The disk is full."));

            Assert.False(card.FindControl<TextBlock>("MoreText")!.IsVisible);
        });
    }

    [Fact]
    public void SeveralErrors_ShowTheMoreLine()
    {
        headless.Run(() =>
        {
            UnhandledErrorCard card = Show(new IOException("first"), new IOException("second"));

            TextBlock more = card.FindControl<TextBlock>("MoreText")!;
            Assert.True(more.IsVisible);
            Assert.Equal("1 more error occurred.", more.Text);
        });
    }

    [Fact]
    public void TheDetails_StartCollapsed_AndHoldTheWholeReport()
    {
        headless.Run(() =>
        {
            UnhandledErrorCard card = Show(new IOException("The disk is full."));

            Assert.False(card.FindControl<Expander>("DetailsExpander")!.IsExpanded);
            Assert.Contains(
                "System.IO.IOException: The disk is full.",
                card.FindControl<TextBox>("DetailsText")!.Text,
                StringComparison.Ordinal);
            Assert.NotNull(card.FindControl<Button>("CopyDetailsButton"));
        });
    }

    private static UnhandledErrorCard Show(params Exception[] errors)
    {
        UnhandledErrorPolicy policy = new(new ManualTimeProvider());

        foreach (Exception error in errors)
        {
            _ = policy.Offer(error);
        }

        UnhandledErrorCard card = new() { DataContext = new UnhandledErrorViewModel(policy.Pending!) };
        Window window = new() { Content = card };

        window.Show();

        return card;
    }
}
