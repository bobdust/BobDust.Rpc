using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BobDust.Rpc.Sockets.Serialization;

public class ObjectToNativeTypesConverter : JsonConverter<object>
{
    private const string TypePropertyName = "$type";
    private const string PayloadPropertyName = "$payload";

    private static bool ShouldIndicateType(Type type)
    {
        ObjectIndicatorAttribute? attribute = type.GetCustomAttribute<ObjectIndicatorAttribute>();

        if (attribute != null)
        {
            return true;
        }

        return false;
    }

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.StartObject => ReadObject(ref reader, typeToConvert, options),
            JsonTokenType.StartArray => JsonSerializer.Deserialize<List<object>>(ref reader, options),
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone() // Fallback safety
        };
    }

    private object? ReadObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        // Extract the runtime type discriminator
        if (root.TryGetProperty("$type", out var typeProperty))
        {
            string? typeName = typeProperty.GetString();
            // Resolve the stored text metadata into a concrete .NET Type object
            Type? targetType = Type.GetType(typeName ?? string.Empty);

            if (targetType == null)
            {
                throw new JsonException($"Could not resolve .NET type: '{typeName}'");
            }

            // Get the real data payload block
            if (!root.TryGetProperty(PayloadPropertyName, out var payloadProperty))
            {
                throw new JsonException($"Missing payload property: '{PayloadPropertyName}'");
            }

            // Deserialize using the newly resolved targetType instead of the original 'typeToConvert'
            string rawJson = payloadProperty.GetRawText();
            reader.Skip();
            return JsonSerializer.Deserialize(rawJson, targetType, options);
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (ShouldIndicateType(value.GetType()))
        {
            writer.WriteStartObject();

            // 1. Serialize the explicit .NET type information
            string? typeName = value.GetType().AssemblyQualifiedName;
            writer.WriteString(TypePropertyName, typeName);

            // 2. Serialize the object data as a sub-payload to avoid property collisions
            writer.WritePropertyName(PayloadPropertyName);
            JsonSerializer.Serialize(writer, value, value.GetType(), options);

            writer.WriteEndObject();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
