using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enigma.Msi.Serialization;

/// <summary>
/// Reads GUIDs in any form <see cref="Guid.TryParse(string, out Guid)"/> accepts — including the
/// brace form <c>{2C7…}</c> that Visual Studio and <c>uuidgen</c> emit — and always writes the
/// canonical hyphenated <c>D</c> form. The built-in converter is stricter on input and reports
/// failures less clearly.
/// </summary>
public sealed class GuidJsonConverter : JsonConverter<Guid>
{
    /// <inheritdoc />
    /// <exception cref="JsonException">The JSON value is not a string, or is not a parseable GUID.</exception>
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a GUID string, found {reader.TokenType}.");
        }

        string? text = reader.GetString();

        if (!Guid.TryParse(text, out Guid parsed))
        {
            throw new JsonException(
                $"\"{text}\" is not a valid GUID. Expected a form such as \"9b4a0f2e-1c3d-4b5a-8e6f-7a8b9c0d1e2f\".");
        }

        return parsed;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("D", CultureInfo.InvariantCulture));
}
