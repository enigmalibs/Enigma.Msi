using System;
using System.Collections.Generic;
using System.IO;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.ViewModels;

/// <summary>
/// Covers what the safety net's dialog says, and that it follows errors counted into its batch while it
/// is open.
/// </summary>
public sealed class UnhandledErrorViewModelTests
{
    [Fact]
    public void ASingleError_SaysWhatWentWrong_AndNothingAboutMore()
    {
        UnhandledErrorPolicy policy = new(new ManualTimeProvider());
        _ = policy.Offer(new IOException("The disk is full."));
        UnhandledErrorViewModel viewModel = new(policy.Pending!);

        Assert.StartsWith("Something went wrong, but the application is still running.", viewModel.Lead, StringComparison.Ordinal);
        Assert.Equal("The disk is full.", viewModel.Message);
        Assert.False(viewModel.HasMore);
        Assert.Contains("System.IO.IOException: The disk is full.", viewModel.Details, StringComparison.Ordinal);
    }

    [Fact]
    public void AnErrorCountedIn_IsFollowed_InTheCountAndTheDetails()
    {
        UnhandledErrorPolicy policy = new(new ManualTimeProvider());
        _ = policy.Offer(new IOException("first"));
        UnhandledErrorViewModel viewModel = new(policy.Pending!);
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _ = policy.Offer(new IOException("second"));

        Assert.True(viewModel.HasMore);
        Assert.Equal("1 more error occurred.", viewModel.MoreText);
        Assert.Contains("Error 2 of 2", viewModel.Details, StringComparison.Ordinal);
        Assert.Equal([nameof(viewModel.HasMore), nameof(viewModel.MoreText), nameof(viewModel.Details)], changed);
    }

    [Fact]
    public void SeveralMoreErrors_AreCountedInThePlural()
    {
        UnhandledErrorPolicy policy = new(new ManualTimeProvider());
        _ = policy.Offer(new IOException("first"));
        _ = policy.Offer(new IOException("second"));
        _ = policy.Offer(new IOException("third"));
        UnhandledErrorViewModel viewModel = new(policy.Pending!);

        Assert.Equal("2 more errors occurred.", viewModel.MoreText);
    }

    [Fact]
    public void Constructor_RejectsAMissingBatch()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new UnhandledErrorViewModel(null!));

        Assert.Equal("batch", exception.ParamName);
    }
}
