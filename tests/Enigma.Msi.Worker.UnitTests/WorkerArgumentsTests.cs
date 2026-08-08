using System;
using Xunit;

namespace Enigma.Msi.Worker.UnitTests;

/// <summary>
/// Covers the strict parser. Every rejection case matters as much as the happy path: the predecessor
/// project's parser ignored what it did not recognize, so a mistyped option produced a build whose
/// result went nowhere.
/// </summary>
public sealed class WorkerArgumentsTests
{
    [Fact]
    public void Parse_RejectsNull()
        => Assert.Throws<ArgumentNullException>(() => WorkerArguments.Parse(null!, out _));

    [Fact]
    public void Parse_ReadsTheInternalHandshake()
    {
        WorkerArguments? arguments = WorkerArguments.Parse(
            ["--request", @"C:\temp\req.json", "--result", @"C:\temp\res.json"],
            out string error);

        Assert.NotNull(arguments);
        Assert.Equal(string.Empty, error);
        Assert.Equal(WorkerMode.Internal, arguments.Mode);
        Assert.Equal(@"C:\temp\req.json", arguments.RequestPath);
        Assert.Equal(@"C:\temp\res.json", arguments.ResultPath);
        Assert.Equal(string.Empty, arguments.ProfilePath);
    }

    [Fact]
    public void Parse_AcceptsTheHandshakeOptionsInEitherOrder()
    {
        WorkerArguments? arguments = WorkerArguments.Parse(
            ["--result", "res.json", "--request", "req.json"],
            out _);

        Assert.NotNull(arguments);
        Assert.Equal("req.json", arguments.RequestPath);
        Assert.Equal("res.json", arguments.ResultPath);
    }

    [Fact]
    public void Parse_ReadsTheCliBuildCommand()
    {
        WorkerArguments? arguments = WorkerArguments.Parse(["build", "widget.msipkg.json"], out string error);

        Assert.NotNull(arguments);
        Assert.Equal(string.Empty, error);
        Assert.Equal(WorkerMode.Cli, arguments.Mode);
        Assert.Equal("widget.msipkg.json", arguments.ProfilePath);
        Assert.Equal(string.Empty, arguments.RequestPath);
        Assert.Equal(string.Empty, arguments.ResultPath);
    }

    public static TheoryData<string[]> MalformedCommandLines() => new()
    {
        // No arguments at all.
        new string[0],
        // An unknown argument is rejected rather than skipped — the whole point of parsing strictly.
        new[] { "--request", "req.json", "--result", "res.json", "--verbose" },
        new[] { "--results", "res.json", "--request", "req.json" },
        new[] { "req.json", "res.json" },
        // Missing options.
        new[] { "--request", "req.json" },
        new[] { "--result", "res.json" },
        // Missing values.
        new[] { "--request" },
        new[] { "--request", "req.json", "--result" },
        new[] { "--request", "--result", "res.json" },
        new[] { "--request", " ", "--result", "res.json" },
        // Repeated options: last-wins would silently discard the first path.
        new[] { "--request", "a.json", "--request", "b.json", "--result", "res.json" },
        // The build command takes exactly one profile path.
        new[] { "build" },
        new[] { "build", "a.msipkg.json", "b.msipkg.json" },
        new[] { "build", "--help" },
        new[] { "build", "" }
    };

    [Theory]
    [MemberData(nameof(MalformedCommandLines))]
    public void Parse_RejectsMalformedCommandLines(string[] args)
    {
        WorkerArguments? arguments = WorkerArguments.Parse(args, out string error);

        Assert.Null(arguments);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void Parse_NamesTheUnknownArgumentItRejected()
    {
        WorkerArguments.Parse(["--request", "req.json", "--result", "res.json", "--verbose"], out string error);

        Assert.Contains("--verbose", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_NamesTheMissingOption()
    {
        WorkerArguments.Parse(["--request", "req.json"], out string error);

        Assert.Contains("--result", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Usage_DocumentsBothCommandLineForms()
    {
        Assert.Contains("--request", WorkerArguments.Usage, StringComparison.Ordinal);
        Assert.Contains("--result", WorkerArguments.Usage, StringComparison.Ordinal);
        Assert.Contains("build", WorkerArguments.Usage, StringComparison.Ordinal);
    }
}
