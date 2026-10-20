#if !NET48

using System.Text.Json;
using System.Text.Json.Serialization;

namespace BobDust.Rpc.Sockets.Serialization;

public class ObjectToNativeTypesConverter : JsonConverter<object>
{
    private const string TypePropertyName = "$type";
    private const string PayloadPropertyName = "$payload";

#pragma warning disable SYSLIB0050 // Type or member is obsolete
    private static bool ShouldIndicateType(Type type) => type.IsSerializable;
#pragma warning restore SYSLIB0050 // Type or member is obsolete

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
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BobDust.Rpc.Sockets.Serialization;

public class ObjectToNativeTypesConverter : JsonConverter
{
    private const string TypePropertyName = "$type";
    private const string PayloadPropertyName = "$payload";

#pragma warning disable SYSLIB0050 // Type or member is obsolete
    private static bool ShouldIndicateType(Type type) => type.GetCustomAttributes(typeof(TypeIndicatorAttribute), false).Any();
#pragma warning restore SYSLIB0050 // Type or member is obsolete

    public override bool CanConvert(Type objectType) => ShouldIndicateType(objectType) || objectType == typeof(object);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jToken = JToken.Load(reader);
        return jToken.Type switch
        {
            JTokenType.Boolean => jToken.Value<bool>(),
            JTokenType.Integer => jToken.Value<int>(),
            JTokenType.Float => jToken.Value<double>(),
            JTokenType.String => jToken.Value<string>(),
            JTokenType.Date => jToken.Value<DateTime>(),
            JTokenType.Object => ReadObject(jToken, objectType, serializer),
            JTokenType.Array => serializer.Deserialize<List<object>>(reader),
            _ => Fallback(jToken, objectType, serializer)
        };
    }

    private object? Fallback(JToken jToken, Type objectType, JsonSerializer serializer)
    {
        // Create a new JsonSerializerSettings copy that excludes this converter
        var settings = new JsonSerializerSettings();
        foreach (var conv in serializer.Converters)
        {
            if (!(conv is ObjectToNativeTypesConverter))
            {
                settings.Converters.Add(conv);
            }
        }

        return jToken.ToObject(objectType, JsonSerializer.Create(settings));
    }

    private object? ReadObject(JToken jToken, Type objectType, JsonSerializer serializer)
    {
        // Extract the runtime type discriminator
        var typeName = jToken.Value<string>(TypePropertyName);
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            // Resolve the stored text metadata into a concrete .NET Type object
            Type? targetType = Type.GetType(typeName);

            if (targetType == null)
            {
                throw new JsonException($"Could not resolve .NET type: '{typeName}'");
            }

            // Get the real data payload block
            return JsonConvert.DeserializeObject(jToken.Value<string>(PayloadPropertyName), targetType);
        }
        return Fallback(jToken, objectType, serializer);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var valueType = value?.GetType();
        if (valueType is not null && ShouldIndicateType(valueType))
        {
            writer.WriteStartObject();

            // 1. Serialize the explicit .NET type information
            string? typeName = valueType.AssemblyQualifiedName;
            writer.WritePropertyName(TypePropertyName);
            writer.WriteValue(typeName);

            // 2. Serialize the object data as a sub-payload to avoid property collisions
            writer.WritePropertyName(PayloadPropertyName);
            writer.WriteValue(JsonConvert.SerializeObject(value, valueType, null));

            writer.WriteEndObject();
        }
        else
        {
            serializer.Serialize(writer, value);
        }
    }
}

#endif