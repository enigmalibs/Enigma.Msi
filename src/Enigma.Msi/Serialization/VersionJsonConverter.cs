using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enigma.Msi.Serialization;

/// <summary>
/// Serializes <see cref="Version"/> as its plain string form (<c>1.2.3</c>) instead of
/// System.Text.Json's default object shape (<c>{ "major": 1, … }</c>), which keeps hand-edited
/// <c>.msipkg.json</c> profiles readable.
/// </summary>
public sealed class VersionJsonConverter : JsonConverter<Version>
{
    /// <inheritdoc />
    /// <exception cref="JsonException">
    /// The JSON value is not a string, or is not a parseable version.
    /// </exception>
    public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Expected a version string such as \"1.2.3\", found {reader.TokenType}.");
        }

        string? text = reader.GetString();

        // The explicit null check is not redundant: netstandard2.0's Version.TryParse is unannotated,
        // so its [NotNullWhen] flow information is unavailable on that target.
        if (!Version.TryParse(text, out Version? parsed) || parsed is null)
        {
            throw new JsonException(
                $"\"{text}\" is not a valid version. Expected two to four dot-separated numbers, e.g. \"1.2.3\".");
        }

        return parsed;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
