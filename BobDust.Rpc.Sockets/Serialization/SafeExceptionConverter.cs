#if !NET48

using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BobDust.Rpc.Sockets.Serialization;

public class SafeExceptionConverter : JsonConverter<Exception>
{
    private readonly bool _exceptionOnly;

    public SafeExceptionConverter(bool exceptionOnly = true)
    {
        _exceptionOnly = exceptionOnly;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Exception).IsAssignableFrom(typeToConvert);
    }

    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        reader.Read();
        var typeName = reader.GetString();
        reader.Read();
        reader.Read();
        var message = reader.GetString();
        reader.Read();
        reader.Read();
        var stackTrace = reader.GetString();
        reader.Read();
        Exception? inner = default;
        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "InnerException")
        {
            reader.Read();
            inner = Read(ref reader, typeof(Exception), options);
            reader.Read();
        }
        // 1. Resolve the target Exception Type
        Type? exceptionType = Type.GetType(typeName!);
        
        // Fallback to standard Exception if the specific type isn't found
        exceptionType ??= typeof(Exception); 

        // 2. Instantiate using the string message constructor
        Exception? exception = Activator.CreateInstance(exceptionType, message, inner) as Exception 
            ?? new Exception(message, inner);

        // 3. Attach the arbitrary text stack trace string cleanly
        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            // SetRemoteStackTrace populates the StackTrace property with a high fidelity raw string
            ExceptionDispatchInfo.SetRemoteStackTrace(exception, stackTrace);
        }
        if (!_exceptionOnly)
        {
            reader.Skip();
        }
        return exception;
    }

    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WriteString("ClassName", value.GetType().FullName);
        writer.WriteString("Message", value.Message);
        writer.WriteString("StackTrace", value.StackTrace);

        if (value.InnerException != null)
        {
            writer.WritePropertyName("InnerException");
            Write(writer, value.InnerException, options);
        }

        writer.WriteEndObject();
    }
}

#else

using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SafeExceptionConverter : JsonConverter
{
    // Target the private field holding the stack trace string in .NET 4.8
    private static readonly FieldInfo StackTraceStringField = typeof(Exception).GetField(
        "_stackTraceString", 
        BindingFlags.Instance | BindingFlags.NonPublic
    );

    public override bool CanConvert(Type objectType)
    {
        return typeof(Exception).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var ex = (Exception)value!;
        var jo = new JObject
        {
            { "ClassName", ex.GetType().AssemblyQualifiedName },
            { "Message", ex.Message },
            { "StackTrace", ex.StackTrace }
        };

        if (ex.InnerException != null)
        {
            jo.Add("InnerException", JToken.FromObject(ex.InnerException, serializer));
        }

        jo.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        var jo = JObject.Load(reader);
        
        var className = jo["ClassName"]?.ToString();
        var message = jo["Message"]?.ToString() ?? string.Empty;
        var stackTrace = jo["StackTrace"]?.ToString();

        // Resolve the exact exception type or fallback to base Exception
        Type exceptionType = className != null ? Type.GetType(className) : typeof(Exception);
        
        // Instantiate the exception (assumes standard message constructor)
        Exception ex;
        try
        {
            ex = (Exception)Activator.CreateInstance(exceptionType, message);
        }
        catch
        {
            ex = new Exception(message);
        }

        // Rehydrate the StackTrace using reflection since SetRemoteStackTrace isn't in .NET 4.8
        if (!string.IsNullOrEmpty(stackTrace) && StackTraceStringField != null)
        {
            StackTraceStringField.SetValue(ex, stackTrace);
        }

        // Handle inner exceptions recursively
        if (jo["InnerException"] != null)
        {
            var innerEx = jo["InnerException"]?.ToObject<Exception>(serializer);
            var innerField = typeof(Exception).GetField("_innerException", BindingFlags.Instance | BindingFlags.NonPublic);
            if (innerField != null)
            {
                innerField.SetValue(ex, innerEx);
            }
        }

        return ex;
    }
}

#endif