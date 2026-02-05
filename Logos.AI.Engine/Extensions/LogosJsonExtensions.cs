using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
namespace Logos.AI.Engine.Extensions;

public static class LogosJsonExtensions
{
    // Кешуємо опції, щоб не створювати їх щоразу (Performance boost)
    private static readonly JsonSerializerOptions JsonOptions = CreateDefaultOptions();
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions();
        ConfigureLogosOptions(options);
        return options;
    }
    /// <summary>
    /// Централізоване налаштування JsonSerializerOptions для всього проекту.
    /// </summary>
    public static void ConfigureLogosOptions(JsonSerializerOptions options)
    {
        options.WriteIndented = true;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        
        // Додаємо підтримку Enum як рядків
        options.Converters.Add(new JsonStringEnumConverter());
        
        // Додаємо кастомний конвертер для гнучкого читання рядків (числа -> рядки)
        options.Converters.Add(new FlexibleStringConverter());

        // Дозволяємо читати числа з лапок
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;

        // Налаштування для роботи з коментарями та комами
        options.ReadCommentHandling = JsonCommentHandling.Skip;
        options.AllowTrailingCommas = true;

        // Стратегія іменування camelCase
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Обробка циклічних посилань
        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Враховуємо анотації nullable
        options.RespectNullableAnnotations = true;
    }
    public static string SerializeToJson<T>(this T obj) => JsonSerializer.Serialize(obj, JsonOptions);
    
    public static T? DeserializeFromJson<T>(this string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public static BinaryData GetSchemaFromType<T>(bool indented = true)
    {
        // Налаштування для генерації схеми
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        
        // Отримуємо вузол схеми з типу
        JsonNode schemaNode = options.GetJsonSchemaAsNode(typeof(T), new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true
        });
		
        // OpenAI Strict Mode вимагає явного additionalProperties: false та required для всіх об'єктів
        MakeSchemaStrict(schemaNode);
		
        // Перетворюємо в BinaryData для OpenAI SDK
        return BinaryData.FromString(schemaNode.ToString());
    }

    private static void MakeSchemaStrict(JsonNode node)
    {
        if (node is not JsonObject obj) return;

        // OpenAI Strict Mode: об'єкти повинні мати "additionalProperties": false
        if (obj["type"] is JsonValue typeVal && typeVal.TryGetValue<string>(out var typeStr) && typeStr == "object")
        {
            obj["additionalProperties"] = false;

            // FIX: Всі поля з properties повинні бути в required
            if (obj.ContainsKey("properties") && obj["properties"] is JsonObject props)
            {
                var requiredList = new JsonArray();
                foreach (var prop in props)
                {
                    requiredList.Add(prop.Key);
                }
                obj["required"] = requiredList;
            }
        }

        // Рекурсія по властивостях
        if (obj.ContainsKey("properties") && obj["properties"] is JsonObject properties)
        {
            foreach (var prop in properties)
            {
                if (prop.Value is not null) MakeSchemaStrict(prop.Value);
            }
        }

        // Рекурсія по масивах (якщо є списки об'єктів)
        if (obj.ContainsKey("items") && obj["items"] is {} items)
        {
            MakeSchemaStrict(items);
        }
    }
}