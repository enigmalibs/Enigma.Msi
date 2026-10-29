using System;
using System.Linq;
using Enigma.Msi.Desktop.Services;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Services;

/// <summary>
/// Covers the hand-over between the process-wide hooks and the safety net: what is reported before it is
/// attached is kept and handed over, within a bound, and nothing is lost across a detach.
/// </summary>
/// <remarks>
/// The hooks are static, so every test starts by draining whatever an earlier one left, and ends detached.
/// <c>Install</c> is never called here: it would hook this test process's own unobserved tasks.
/// </remarks>
[Collection(Name)]
public sealed class UnhandledExceptionHooksTests : IDisposable
{
    private const string Name = "unhandled-exception-hooks";

    public UnhandledExceptionHooksTests()
    {
        UnhandledExceptionHooks.Attach(new RecordingReporter());
        UnhandledExceptionHooks.Detach();
    }

    public void Dispose() => UnhandledExceptionHooks.Detach();

    [Fact]
    public void WhatCameBeforeTheReporter_IsHandedOverOnAttach_InOrder()
    {
        Exception first = new InvalidOperationException("first");
        Exception second = new InvalidOperationException("second");
        RecordingReporter reporter = new();

        UnhandledExceptionHooks.Report(first, UnhandledErrorSource.UnobservedTask);
        UnhandledExceptionHooks.Report(second, UnhandledErrorSource.Dispatcher);
        UnhandledExceptionHooks.Attach(reporter);

        Assert.Equal(
            [(first, UnhandledErrorSource.UnobservedTask), (second, UnhandledErrorSource.Dispatcher)],
            reporter.Reports);
    }

    [Fact]
    public void OnceAttached_ReportsGoStraightToTheReporter()
    {
        RecordingReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);
        Exception error = new InvalidOperationException("now");

        UnhandledExceptionHooks.Report(error, UnhandledErrorSource.Dispatcher);

        Assert.Equal([(error, UnhandledErrorSource.Dispatcher)], reporter.Reports);
    }

    [Fact]
    public void BeforeTheReporter_AtMostThirtyTwoReportsAreKept()
    {
        RecordingReporter reporter = new();

        for (int index = 0; index < 40; index++)
        {
            UnhandledExceptionHooks.Report(new InvalidOperationException($"#{index}"), UnhandledErrorSource.UnobservedTask);
        }

        UnhandledExceptionHooks.Attach(reporter);

        Assert.Equal(32, reporter.Reports.Count);
        Assert.Equal("#0", reporter.Reports.First().Exception.Message);
        Assert.Equal("#31", reporter.Reports.Last().Exception.Message);
    }

    [Fact]
    public void AfterADetach_ReportsAreKeptForTheNextAttach()
    {
        RecordingReporter first = new();
        RecordingReporter second = new();
        Exception error = new InvalidOperationException("late");

        UnhandledExceptionHooks.Attach(first);
        UnhandledExceptionHooks.Detach();
        UnhandledExceptionHooks.Report(error, UnhandledErrorSource.AppDomain);
        UnhandledExceptionHooks.Attach(second);

        Assert.Empty(first.Reports);
        Assert.Equal([(error, UnhandledErrorSource.AppDomain)], second.Reports);
    }

    [Fact]
    public void Attach_RejectsAMissingReporter()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => UnhandledExceptionHooks.Attach(null!));

        Assert.Equal("reporter", exception.ParamName);
    }
}
