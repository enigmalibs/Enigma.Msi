using System;
using System.IO;
using System.Reflection;
using Enigma.Msi.Desktop.Services;
using Xunit;

namespace Enigma.Msi.Desktop.UnitTests.Services;

/// <summary>
/// Covers what a batch says: the one line that carries the meaning, the details a bug report needs, and
/// how it follows the errors the policy counts into it.
/// </summary>
public sealed class UnhandledErrorBatchTests
{
    [Fact]
    public void Message_IsTheErrorsOwnMessage()
    {
        UnhandledErrorBatch batch = new(new IOException("The disk is full."));

        Assert.Equal("The disk is full.", batch.Message);
    }

    [Fact]
    public void Message_LooksThroughWrappersThatSayNothing()
    {
        Exception inner = new IOException("The disk is full.");

        Assert.Equal("The disk is full.", new UnhandledErrorBatch(new AggregateException(inner)).Message);
        Assert.Equal("The disk is full.", new UnhandledErrorBatch(new TargetInvocationException(inner)).Message);
    }

    [Fact]
    public void Message_FallsBackToTheTypeName_WhenTheErrorSaysNothing()
    {
        UnhandledErrorBatch batch = new(new InvalidOperationException(" "));

        Assert.Equal(nameof(InvalidOperationException), batch.Message);
    }

    [Fact]
    public void Details_NameTheTypeTheMessageAndTheInnerException()
    {
        UnhandledErrorBatch batch = new(new InvalidOperationException("outer", new IOException("inner")));

        string details = batch.Details;

        Assert.Contains("System.InvalidOperationException: outer", details, StringComparison.Ordinal);
        Assert.Contains("--- Inner exception ---", details, StringComparison.Ordinal);
        Assert.Contains("System.IO.IOException: inner", details, StringComparison.Ordinal);
    }

    [Fact]
    public void Details_NumberTheInnerExceptionsOfAnAggregate()
    {
        UnhandledErrorBatch batch = new(new AggregateException(new IOException("one"), new IOException("two")));

        string details = batch.Details;

        Assert.Contains("--- Inner exception 1 of 2 ---", details, StringComparison.Ordinal);
        Assert.Contains("--- Inner exception 2 of 2 ---", details, StringComparison.Ordinal);
    }

    [Fact]
    public void Details_StopFollowingInnerExceptionsAfterEightLevels()
    {
        Exception error = new IOException("level 12");

        for (int level = 11; level >= 0; level--)
        {
            error = new InvalidOperationException($"level {level}", error);
        }

        string details = new UnhandledErrorBatch(error).Details;

        Assert.Equal(8, CountOf(details, "--- Inner exception ---"));
    }

    [Fact]
    public void Details_NumberEachErrorOfABatchOfSeveral()
    {
        UnhandledErrorBatch batch = BatchOf(new IOException("first"), new IOException("second"));

        string details = batch.Details;

        Assert.Contains("Error 1 of 2", details, StringComparison.Ordinal);
        Assert.Contains("Error 2 of 2", details, StringComparison.Ordinal);
        Assert.Contains("second", details, StringComparison.Ordinal);
    }

    [Fact]
    public void AnErrorCountedIn_RaisesChanged_AndCountsAsOneMore()
    {
        UnhandledErrorPolicy policy = new(new ManualTimeProvider());
        _ = policy.Offer(new IOException("first"));
        UnhandledErrorBatch batch = policy.Pending!;
        int changes = 0;
        batch.Changed += (_, _) => changes++;

        _ = policy.Offer(new IOException("second"));

        Assert.Equal(1, changes);
        Assert.Equal(1, batch.More);
        Assert.Equal(2, batch.Errors.Count);
    }

    [Fact]
    public void Constructor_RejectsAMissingError()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new UnhandledErrorBatch(null!));

        Assert.Equal("first", exception.ParamName);
    }

    // Batches only ever grow through the policy, which counts every later error into the waiting one.
    private static UnhandledErrorBatch BatchOf(params Exception[] errors)
    {
        UnhandledErrorPolicy policy = new(new ManualTimeProvider());

        foreach (Exception error in errors)
        {
            _ = policy.Offer(error);
        }

        return policy.Pending!;
    }

    private static int CountOf(string text, string value)
    {
        int count = 0;

        for (int index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
