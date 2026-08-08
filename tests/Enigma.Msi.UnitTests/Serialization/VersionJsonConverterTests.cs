using System;
using System.Text.Json;
using Enigma.Msi.Serialization;
using Xunit;

namespace Enigma.Msi.UnitTests.Serialization;

/// <summary>
/// Covers the <see cref="Version"/> converter: the readable string form on the wire, and a clear
/// failure on anything that is not a version.
/// </summary>
public sealed class VersionJsonConverterTests
{
    [Theory]
    [InlineData("1.2")]
    [InlineData("1.2.3")]
    [InlineData("1.2.3.4")]
    public void RoundTrip_PreservesEveryComponent(string text)
    {
        var version = Version.Parse(text);

        string json = JsonSerializer.Serialize(version, MsiPackageJson.Options);

        Assert.Equal($"\"{text}\"", json);
        Assert.Equal(version, JsonSerializer.Deserialize<Version>(json, MsiPackageJson.Options));
    }

    [Theory]
    [InlineData("\"1\"")]
    [InlineData("\"\"")]
    [InlineData("\"1.2.3.4.5\"")]
    [InlineData("\"1.2.x\"")]
    [InlineData("\"v1.2\"")]
    public void Read_RejectsStringsThatAreNotVersions(string json)
    {
        JsonException exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Version>(json, MsiPackageJson.Options));

        Assert.Contains("is not a valid version", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("[]")]
    public void Read_RejectsNonStringTokens(string json)
    {
        JsonException exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Version>(json, MsiPackageJson.Options));

        Assert.Contains("Expected a version string", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ReportsTheOffendingMemberWhenDeserializingAPackage()
    {
        JsonException exception = Assert.Throws<JsonException>(
            () => MsiPackageJson.Deserialize("""{ "appName": "Widget", "version": "one.two" }"""));

        Assert.Contains("is not a valid version", exception.Message, StringComparison.Ordinal);
        Assert.Equal("$.version", exception.Path);
    }
}
