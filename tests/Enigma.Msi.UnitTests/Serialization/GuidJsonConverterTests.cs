using System;
using System.Text.Json;
using Enigma.Msi.Serialization;
using Xunit;

namespace Enigma.Msi.UnitTests.Serialization;

/// <summary>
/// Covers the <see cref="Guid"/> converter: the lenient input the tools people copy GUIDs from
/// produce, the canonical output form, and a clear failure otherwise.
/// </summary>
public sealed class GuidJsonConverterTests
{
    private const string Canonical = "9b4a0f2e-1c3d-4b5a-8e6f-7a8b9c0d1e2f";

    [Theory]
    [InlineData(Canonical)]
    [InlineData("{9b4a0f2e-1c3d-4b5a-8e6f-7a8b9c0d1e2f}")]
    [InlineData("(9b4a0f2e-1c3d-4b5a-8e6f-7a8b9c0d1e2f)")]
    [InlineData("9b4a0f2e1c3d4b5a8e6f7a8b9c0d1e2f")]
    [InlineData("9B4A0F2E-1C3D-4B5A-8E6F-7A8B9C0D1E2F")]
    public void Read_AcceptsEveryCommonGuidForm(string text)
    {
        Guid value = JsonSerializer.Deserialize<Guid>($"\"{text}\"", MsiPackageJson.Options);

        Assert.Equal(Guid.Parse(Canonical), value);
    }

    [Fact]
    public void Write_UsesTheCanonicalHyphenatedForm()
    {
        string json = JsonSerializer.Serialize(Guid.Parse("{9B4A0F2E-1C3D-4B5A-8E6F-7A8B9C0D1E2F}"), MsiPackageJson.Options);

        Assert.Equal($"\"{Canonical}\"", json);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"not-a-guid\"")]
    [InlineData("\"9b4a0f2e-1c3d-4b5a-8e6f\"")]
    public void Read_RejectsStringsThatAreNotGuids(string json)
    {
        JsonException exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Guid>(json, MsiPackageJson.Options));

        Assert.Contains("is not a valid GUID", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("null")]
    public void Read_RejectsNonStringTokens(string json)
    {
        JsonException exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Guid>(json, MsiPackageJson.Options));

        Assert.Contains("Expected a GUID string", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ReportsTheOffendingMemberWhenDeserializingAPackage()
    {
        JsonException exception = Assert.Throws<JsonException>(
            () => MsiPackageJson.Deserialize("""{ "productId": "nope" }"""));

        Assert.Contains("is not a valid GUID", exception.Message, StringComparison.Ordinal);
        Assert.Equal("$.productId", exception.Path);
    }
}
