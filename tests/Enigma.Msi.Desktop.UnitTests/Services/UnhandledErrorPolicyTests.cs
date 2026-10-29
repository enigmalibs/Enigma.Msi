using System;
using System.Threading;
using Enigma.Msi.Desktop.Services;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Services;

/// <summary>
/// Covers what the safety net decides to show: one dialog at a time, later errors counted into it,
/// cancellations nobody needs to hear about, and an error just seen not shown again within the window.
/// </summary>
public sealed class UnhandledErrorPolicyTests
{
    private readonly ManualTimeProvider _time = new();

    [Fact]
    public void TheFirstError_QueuesANewBatch()
    {
        UnhandledErrorPolicy policy = new(_time);
        Exception error = Thrown("boom");

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(error));
        Assert.Same(error, policy.Pending!.First);
    }

    [Fact]
    public void TheSameExceptionTwice_IsIgnoredTheSecondTime()
    {
        // An awaited fault rethrown on the dispatcher can also be found unobserved by the finaliser.
        UnhandledErrorPolicy policy = new(_time);
        Exception error = Thrown("boom");

        _ = policy.Offer(error);

        Assert.Equal(UnhandledErrorDecision.Ignore, policy.Offer(error));
        Assert.Equal(0, policy.Pending!.More);
    }

    [Fact]
    public void AnErrorWhileOneWaits_IsCountedIntoIt()
    {
        UnhandledErrorPolicy policy = new(_time);
        _ = policy.Offer(Thrown("first"));

        Assert.Equal(UnhandledErrorDecision.Merge, policy.Offer(Thrown("second")));
        Assert.Equal(1, policy.Pending!.More);
    }

    [Fact]
    public void AnErrorWhileOneIsOnScreen_IsCountedIntoIt()
    {
        UnhandledErrorPolicy policy = new(_time);
        _ = policy.Offer(Thrown("first"));
        UnhandledErrorBatch shown = policy.BeginShow()!;

        Assert.Equal(UnhandledErrorDecision.Merge, policy.Offer(Thrown("second")));
        Assert.Equal(1, shown.More);
        Assert.Null(policy.Pending);
    }

    [Fact]
    public void ACancellationSomeoneAskedFor_IsIgnored()
    {
        UnhandledErrorPolicy policy = new(_time);
        using CancellationTokenSource source = new();
        source.Cancel();

        Assert.Equal(UnhandledErrorDecision.Ignore, policy.Offer(new OperationCanceledException(source.Token)));
        Assert.Null(policy.Pending);
    }

    [Fact]
    public void ACancellationNobodyAskedFor_IsAnError()
    {
        // A timeout surfaces as a cancellation whose token was never cancelled.
        UnhandledErrorPolicy policy = new(_time);

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(new OperationCanceledException()));
    }

    [Fact]
    public void AnyCancellation_IsIgnored_OnceTheApplicationIsClosing()
    {
        UnhandledErrorPolicy policy = new(_time) { IsShuttingDown = true };

        Assert.Equal(UnhandledErrorDecision.Ignore, policy.Offer(new OperationCanceledException()));
    }

    [Fact]
    public void AnAggregateOfIgnoredCancellations_IsIgnored_ButNotOneHoldingARealError()
    {
        UnhandledErrorPolicy policy = new(_time);
        using CancellationTokenSource source = new();
        source.Cancel();

        Assert.True(policy.IsIgnoredCancellation(new AggregateException(
            new OperationCanceledException(source.Token),
            new OperationCanceledException(source.Token))));
        Assert.False(policy.IsIgnoredCancellation(new AggregateException(
            new OperationCanceledException(source.Token),
            Thrown("real"))));
    }

    [Fact]
    public void AnErrorJustClosed_IsSuppressed_UntilTheRepeatWindowHasPassed()
    {
        UnhandledErrorPolicy policy = new(_time);
        _ = policy.Offer(Thrown("periodic"));
        _ = policy.BeginShow();
        policy.EndShow();

        _time.Advance(UnhandledErrorPolicy.RepeatWindow - TimeSpan.FromSeconds(1));
        Assert.Equal(UnhandledErrorDecision.Suppress, policy.Offer(Thrown("periodic")));

        _time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(Thrown("periodic")));
    }

    [Fact]
    public void ADifferentError_IsShown_EvenRightAfterOneWasClosed()
    {
        UnhandledErrorPolicy policy = new(_time);
        _ = policy.Offer(Thrown("one"));
        _ = policy.BeginShow();
        policy.EndShow();

        Assert.Equal(UnhandledErrorDecision.Queue, policy.Offer(Thrown("another")));
    }

    [Fact]
    public void BeginShow_MovesThePendingBatchOnScreen_AndEndShowClearsIt()
    {
        UnhandledErrorPolicy policy = new(_time);

        Assert.Null(policy.BeginShow());

        _ = policy.Offer(Thrown("boom"));
        UnhandledErrorBatch? batch = policy.BeginShow();

        Assert.NotNull(batch);
        Assert.Same(batch, policy.Showing);
        Assert.Null(policy.Pending);

        policy.EndShow();

        Assert.Null(policy.Showing);
    }

    [Fact]
    public void Constructor_RejectsAMissingTimeProvider()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new UnhandledErrorPolicy(null!));

        Assert.Equal("time", exception.ParamName);
    }

    // Thrown and caught in one place, so two calls with one message carry the same stack trace — the same
    // error, as a periodic failure would raise it, in two different instances.
    private static InvalidOperationException Thrown(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }
}
