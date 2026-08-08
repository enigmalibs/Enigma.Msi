using Xunit;

namespace Enigma.Msi.UnitTests;

/// <summary>
/// Bootstrap smoke test: proves the MTP-native xUnit v3 toolchain builds and runs green on both test
/// TFMs. <c>Enigma.Msi</c> has no public types yet — the <c>ProjectReference</c> already forces the
/// library to compile on every target — so a trivial assertion suffices. PHASE02 replaces this with
/// real model, serialization and validation tests.
/// </summary>
public sealed class SmokeTest
{
    [Fact]
    public void Toolchain_BuildsAndRuns() => Assert.True(true);
}
