using System.Text.Json;
using System.Text.Json.Serialization;

namespace Logos.AI.Engine.Extensions;

/// <summary>
/// Кастомний конвертер для System.Text.Json, який дозволяє читати числа як рядки.
/// Це корисно, коли API може повернути 5.0 замість "5.0" для строкового поля.
/// </summary>
public class FlexibleStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
            case JsonTokenType.True:
            case JsonTokenType.False:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.GetRawText();
                }
            case JsonTokenType.Null:
                return null;
            default:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.GetRawText();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
