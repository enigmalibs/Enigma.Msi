using System.Text.Json;
using System.Text.Json.Serialization;
using Enigma.Msi.Build;
using Enigma.Msi.Model;

namespace Enigma.Msi.Serialization;

/// <summary>
/// The single JSON contract of the library: the on-disk <c>.msipkg.json</c> profile format, the
/// request the build client hands to the worker, and the result the worker hands back. Centralizing
/// the options here guarantees the modern host and the net472 worker read and write byte-compatible
/// JSON.
/// </summary>
public static class MsiPackageJson
{
    /// <summary>
    /// The serializer options used on both sides: camelCase property names, indented output, enums as
    /// their member names, <see langword="null"/> members omitted, and the
    /// <see cref="GuidJsonConverter"/>/<see cref="VersionJsonConverter"/> pair. Shared — never mutate
    /// it (System.Text.Json seals it on first use anyway); copy it if different settings are needed.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Serializes <paramref name="package"/> to its <c>.msipkg.json</c> form.</summary>
    /// <param name="package">The package to serialize.</param>
    /// <returns>The indented JSON document.</returns>
    public static string Serialize(MsiPackage package)
        => JsonSerializer.Serialize(package, Options);

    /// <summary>Deserializes a <c>.msipkg.json</c> document.</summary>
    /// <param name="json">The JSON document.</param>
    /// <returns>The deserialized package. Unknown members are ignored, for forward compatibility.</returns>
    /// <exception cref="JsonException">
    /// <paramref name="json"/> is not valid JSON, is the literal <c>null</c>, or contains a value the
    /// model cannot represent.
    /// </exception>
    public static MsiPackage Deserialize(string json)
        => JsonSerializer.Deserialize<MsiPackage>(json, Options)
           ?? throw new JsonException("The document does not contain an MSI package (it is JSON null).");

    /// <summary>Serializes a build result — the worker's result-file format.</summary>
    /// <param name="result">The result to serialize.</param>
    /// <returns>The indented JSON document.</returns>
    public static string SerializeResult(MsiBuildResult result)
        => JsonSerializer.Serialize(result, Options);

    /// <summary>Deserializes a build result written by the worker.</summary>
    /// <param name="json">The JSON document.</param>
    /// <returns>The deserialized result.</returns>
    /// <exception cref="JsonException">
    /// <paramref name="json"/> is not valid JSON or is the literal <c>null</c>.
    /// </exception>
    public static MsiBuildResult DeserializeResult(string json)
        => JsonSerializer.Deserialize<MsiBuildResult>(json, Options)
           ?? throw new JsonException("The document does not contain a build result (it is JSON null).");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Profiles are hand-editable; a casing slip should not cost the user a field.
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new GuidJsonConverter());
        options.Converters.Add(new VersionJsonConverter());

        return options;
    }
}
