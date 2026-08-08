using System;
using System.Linq;
using Enigma.Msi.Build;
using Enigma.Msi.Model;
using Enigma.Msi.Validation;
using Xunit;

namespace Enigma.Msi.Worker.UnitTests;

/// <summary>
/// Covers the runner's validation gate. The build itself is not exercised here: it needs Windows and
/// the <c>wix</c> CLI, so a real MSI build stays a manual acceptance step.
/// </summary>
public sealed class MsiBuildRunnerTests
{
    [Fact]
    public void Run_RejectsNull()
        => Assert.Throws<ArgumentNullException>(() => MsiBuildRunner.Run(null!));

    [Fact]
    public void Run_ReportsEveryValidationViolationWithoutBuilding()
    {
        // An untouched package violates every required-member rule at once.
        var package = new MsiPackage();

        MsiBuildResult result = MsiBuildRunner.Run(package);

        Assert.False(result.Success);
        Assert.Null(result.MsiPath);
        Assert.True(result.Errors.Count > 1, "the runner must aggregate violations, not stop at the first");
    }

    [Fact]
    public void Run_ReportsViolationsInTheValidatorsPathAndMessageForm()
    {
        var package = new MsiPackage();
        MsiValidationResult expected = new MsiPackageValidator().ValidateAll(package);

        MsiBuildResult result = MsiBuildRunner.Run(package);

        Assert.Equal(expected.Errors.Select(error => error.ToString()), result.Errors);
    }

    [Fact]
    public void Run_AppliesTheEnvironmentRulesToo()
    {
        // Everything the in-memory rules check is present; only the release folder is missing from disk,
        // which is exactly what the worker must catch before handing the package to WixSharp.
        MsiPackage package = TestPackages.CreateMinimal();

        MsiBuildResult result = MsiBuildRunner.Run(package);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.StartsWith("install.releasePath:", StringComparison.Ordinal));
    }
}
