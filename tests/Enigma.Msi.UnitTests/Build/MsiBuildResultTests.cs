using System;
using System.Collections.Generic;
using Enigma.Msi.Build;
using Xunit;

namespace Enigma.Msi.UnitTests.Build;

/// <summary>
/// Covers the result the worker writes and the build client reads back — in particular that a failure
/// can carry every reason at once.
/// </summary>
public sealed class MsiBuildResultTests
{
    [Fact]
    public void NewResult_HasNoErrors()
    {
        var result = new MsiBuildResult();

        Assert.False(result.Success);
        Assert.Null(result.MsiPath);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Succeeded_CarriesThePathAndNoErrors()
    {
        MsiBuildResult result = MsiBuildResult.Succeeded(@"C:\artifacts\Widget.msi");

        Assert.True(result.Success);
        Assert.Equal(@"C:\artifacts\Widget.msi", result.MsiPath);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failed_WithOneReason_CarriesIt()
    {
        MsiBuildResult result = MsiBuildResult.Failed("The wix CLI is not installed.");

        Assert.False(result.Success);
        Assert.Null(result.MsiPath);
        Assert.Equal("The wix CLI is not installed.", Assert.Single(result.Errors));
    }

    [Fact]
    public void Failed_WithManyReasons_CarriesAllOfThemInOrder()
    {
        var reasons = new List<string> { "appName: Required.", "version: Required.", "productId: Required." };

        MsiBuildResult result = MsiBuildResult.Failed(reasons);

        Assert.Equal(reasons, result.Errors);
    }

    [Fact]
    public void Failed_TakesASnapshotOfTheReasons()
    {
        var reasons = new List<string> { "appName: Required." };

        MsiBuildResult result = MsiBuildResult.Failed(reasons);
        reasons.Add("added afterwards");

        Assert.Single(result.Errors);
    }

    [Fact]
    public void Factories_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => MsiBuildResult.Succeeded(null!));
        Assert.Throws<ArgumentNullException>(() => MsiBuildResult.Failed((string)null!));
        Assert.Throws<ArgumentNullException>(() => MsiBuildResult.Failed((IEnumerable<string>)null!));
    }
}
