using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Enigma.Msi.Build;
using Enigma.Msi.Model;
using Enigma.Msi.Serialization;

namespace Enigma.Msi.StubWorker;

/// <summary>
/// A stand-in for <c>Enigma.Msi.Worker.exe</c>: it speaks the same <c>--request</c>/<c>--result</c>
/// protocol and the same JSON contract, but produces its outcome from a script instead of from WixSharp.
/// That lets the library suite drive <c>MsiBuildService</c> against a real child process — real streams,
/// real exit codes, a real process tree to cancel — without WiX, without net472, and without mocking
/// away the part of the build client most likely to be wrong.
/// </summary>
/// <remarks>
/// <para>
/// The script is a one-word directive in <c>stub.directive</c>, inside the package's release directory.
/// That directory is the one thing the test creates, the request necessarily points at, and no other
/// test shares — so behaviour is selected per test with no environment variables and no global state.
/// </para>
/// <para>
/// The stub also answers <c>--version</c>, so it can stand in for the WiX CLI in pre-flight tests, and
/// <c>--sleep &lt;seconds&gt;</c>, which is how it gives itself a grandchild process for the
/// cancellation-kills-the-tree test.
/// </para>
/// </remarks>
internal static class Program
{
    /// <summary>Name of the file, inside the release directory, that selects the behaviour.</summary>
    private const string DirectiveFileName = "stub.directive";

    /// <summary>Where the stub drops a verbatim copy of the request it was given, for the test to assert on.</summary>
    private const string RequestCopyFileName = "stub.request.json";

    /// <summary>Exit code used by the <c>no-result</c> directive — distinctive, so a test can recognise it.</summary>
    private const int NoResultExitCode = 7;

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--version")
        {
            Console.Out.WriteLine("9.9.9-stub");
            return 0;
        }

        if (args.Length == 2 && args[0] == "--sleep")
        {
            Thread.Sleep(TimeSpan.FromSeconds(int.Parse(args[1], CultureInfo.InvariantCulture)));
            return 0;
        }

        if (args.Length != 4 || args[0] != "--request" || args[2] != "--result")
        {
            Console.Error.WriteLine("stub: expected --request <file> --result <file>, --version, or --sleep <seconds>.");
            return 2;
        }

        return Run(requestPath: args[1], resultPath: args[3]);
    }

    private static int Run(string requestPath, string resultPath)
    {
        MsiPackage package = MsiPackageJson.Deserialize(File.ReadAllText(requestPath));
        string releaseDirectory = package.Install.ReleasePath;

        // A verbatim copy, not a re-serialization: the test asserts on what the build client actually
        // wrote, byte for byte.
        File.Copy(requestPath, Path.Combine(releaseDirectory, RequestCopyFileName), overwrite: true);

        // Written to both streams so the tests can prove each is pumped into the build log. The process
        // id is how a cancellation test identifies the process it expects to be dead afterwards.
        Console.Out.WriteLine($"stub: pid={Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}");
        Console.Out.WriteLine($"stub: request={requestPath}");
        Console.Out.WriteLine($"stub: result={resultPath}");
        Console.Error.WriteLine("stub: this line came from stderr");

        string directivePath = Path.Combine(releaseDirectory, DirectiveFileName);
        string directive = File.Exists(directivePath)
            ? File.ReadAllText(directivePath).Trim()
            : "succeed";

        switch (directive)
        {
            case "succeed":
                WriteResult(resultPath, MsiBuildResult.Succeeded(
                    Path.Combine(package.Output.OutputPath, package.Output.MsiFilename + ".msi")));
                return 0;

            case "fail":
                WriteResult(resultPath, MsiBuildResult.Failed(
                    ["stub: the first reason it failed", "stub: the second reason it failed"]));
                return 1;

            case "no-result":
                Console.Error.WriteLine("stub: leaving without writing a result file");
                return NoResultExitCode;

            case "invalid-result":
                File.WriteAllText(resultPath, "this is not a build result");
                return 0;

            case "hang":
                return Hang(spawnChild: false);

            case "hang-with-child":
                return Hang(spawnChild: true);

            default:
                Console.Error.WriteLine($"stub: unknown directive '{directive}'");
                return 2;
        }
    }

    /// <summary>
    /// Blocks until something kills this process — the cancellation tests' whole point. With
    /// <paramref name="spawnChild"/> the stub first starts a sleeping copy of itself and announces its
    /// process id, so a test can check that cancelling the build took the <em>tree</em> down and not
    /// just the process the build client started.
    /// </summary>
    private static int Hang(bool spawnChild)
    {
        if (spawnChild)
        {
            using Process child = Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? throw new InvalidOperationException("The stub has no executable path."),
                Arguments = "--sleep 300",
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("The stub could not start its child.");

            Console.Out.WriteLine($"stub: child={child.Id.ToString(CultureInfo.InvariantCulture)}");
        }

        Console.Out.WriteLine("stub: hanging");
        Thread.Sleep(TimeSpan.FromMinutes(5));

        return 0;
    }

    private static void WriteResult(string resultPath, MsiBuildResult result)
        => File.WriteAllText(resultPath, MsiPackageJson.SerializeResult(result));
}
