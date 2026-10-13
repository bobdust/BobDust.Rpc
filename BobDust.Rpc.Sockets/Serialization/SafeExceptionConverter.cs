using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BobDust.Rpc.Sockets.Serialization;

public class SafeExceptionConverter : JsonConverter<Exception>
{
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
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            inner = Read(ref reader, typeof(Exception), options);
        }
        // 1. Resolve the target Exception Type
        Type? exceptionType = Type.GetType(typeName);
        
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
        reader.Skip();
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
