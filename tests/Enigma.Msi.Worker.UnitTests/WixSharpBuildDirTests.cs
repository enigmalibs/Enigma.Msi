using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Wix = WixSharp;

namespace Enigma.Msi.Worker.UnitTests;

/// <summary>
/// Guards the <c>.wixsharp</c> folder the worker's build seeds beside <c>Enigma.Msi.Worker.exe</c>. WixSharp
/// packages the managed UI from that folder and creates and fills it on first use — which an installed
/// worker under Program Files has no right to do. The seed only helps while WixSharp still looks for the
/// folder where it is put, and still accepts the copies in it as current, so both rules are checked here
/// through WixSharp's own (internal) members rather than re-implemented.
/// </summary>
/// <remarks>
/// When one of these fails after a WixSharp bump, re-run a real MSI build from a fresh worker output and
/// look at what WixSharp writes into <c>.wixsharp</c>; then update the worker csproj's
/// <c>SeedWixSharpBuildDir</c> target, and only then the expectations here.
/// </remarks>
public sealed class WixSharpBuildDirTests
{
    /// <summary>The assemblies WixSharp_wix4 2.14.1 copies into <c>.wixsharp</c> for a managed-UI project.</summary>
    private static readonly string[] IsolatedFiles =
    [
        "Enigma.Msi.Worker.exe",
        "WixSharp.dll",
        "WixSharp.UI.dll",
        "WixToolset.Dtf.WindowsInstaller.dll",
        "WixToolset.Mba.Core.dll"
    ];

    private static readonly string WorkerOutputDir = ReadWorkerOutputDir();

    private static readonly string SeededDir = Path.Combine(WorkerOutputDir, ".wixsharp");

    [Fact]
    public void WixSharp_ResolvesItsBuildDirBesideTheWorkerExe_WhereTheBuildSeedsIt()
    {
        // Checked first: resolving the folder creates it when it is missing, and an empty folder made by
        // this test must not pass for a seeded one.
        Assert.True(Directory.Exists(SeededDir), $"The worker's build did not seed '{SeededDir}'.");

        string? resolved = ResolveWixSharpBuildDir(Path.Combine(WorkerOutputDir, "Enigma.Msi.Worker.exe"));

        Assert.NotNull(resolved);
        Assert.Equal(Path.GetFullPath(SeededDir), Path.GetFullPath(resolved));
    }

    [Fact]
    public void EveryIsolatedFile_HasACopyWixSharpAcceptsAsUpToDate()
    {
        MethodInfo isUpToDateCopyOf = Member(
            typeof(Wix.Utils).GetMethod(
                "IsUpToDateCopyOf",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                [typeof(string), typeof(string)],
                modifiers: null),
            "Utils.IsUpToDateCopyOf(string, string)");

        string[] stale = IsolatedFiles
            .Where(file => isUpToDateCopyOf.Invoke(
                null,
                [Path.Combine(SeededDir, file), Path.Combine(WorkerOutputDir, file)]) is not true)
            .ToArray();

        Assert.Empty(stale);
    }

    /// <summary>
    /// What WixSharp's <c>Utils.WixsharpBuildDir</c> resolves to for <paramref name="clientAssembly"/>. The
    /// value is cached in a static field the first time it is read, so the cache is cleared for the read
    /// and every piece of global state is put back afterwards.
    /// </summary>
    private static string? ResolveWixSharpBuildDir(string clientAssembly)
    {
        const BindingFlags InternalStatic = BindingFlags.NonPublic | BindingFlags.Static;

        PropertyInfo buildDir = Member(typeof(Wix.Utils).GetProperty("WixsharpBuildDir", InternalStatic), "Utils.WixsharpBuildDir");
        FieldInfo cache = Member(typeof(Wix.Utils).GetField("wixsharpBuildDir", InternalStatic), "Utils.wixsharpBuildDir");

        string previousClientAssembly = Wix.Compiler.ClientAssembly;
        object? previousCache = cache.GetValue(null);

        try
        {
            Wix.Compiler.ClientAssembly = clientAssembly;
            cache.SetValue(null, null);

            return buildDir.GetValue(null) as string;
        }
        finally
        {
            Wix.Compiler.ClientAssembly = previousClientAssembly;
            cache.SetValue(null, previousCache);
        }
    }

    private static TMember Member<TMember>(TMember? member, string name)
        where TMember : MemberInfo
        => member ?? throw new InvalidOperationException(
            $"WixSharp no longer has '{name}', which the .wixsharp seeding relies on. Re-verify what WixSharp writes into .wixsharp, and where.");

    private static string ReadWorkerOutputDir()
        => Path.GetFullPath(typeof(WixSharpBuildDirTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "EnigmaMsiWorkerOutputDir")
            .Value);
}
