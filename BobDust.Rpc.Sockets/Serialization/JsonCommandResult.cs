#if !NET48
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
using JsonSerializerOptions = Newtonsoft.Json.JsonSerializerSettings;
#endif
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	class JsonCommandResult : JsonCommandBase, ICommandResult
	{
        public JsonCommandResult() : base() { }

		public JsonCommandResult(string contractType, string operationName) : base(contractType, operationName)
		{
		}

		public JsonCommandResult(string contractType, string operationName, object? returnValue) : this(contractType, operationName)
		{
			ReturnValue = returnValue;
		}

		public JsonCommandResult(string contractType, string operationName, Exception exception) : this(contractType, operationName)
		{
			Exception = exception;
		}

		public object? ReturnValue { get; set; }

		public Exception? Exception { get; set; }

		protected override void CopyFrom(JsonCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var commandResult = (JsonCommandResult)deserialized;
			ReturnValue = commandResult.ReturnValue;
			Exception = commandResult.Exception;
		}

        protected override JsonCommandBase Deserialize(Stream stream, JsonSerializerOptions options)
        {
#if !NET48
            return JsonSerializer.Deserialize<JsonCommandResult>(stream, options)!;
#else
			using var reader = new StreamReader(stream);
			using var jsonReader = new JsonTextReader(reader);
			var serializer = JsonSerializer.Create(options);
			return serializer.Deserialize<JsonCommandResult>(jsonReader)!;
#endif
        }

        protected override void Serialize(Stream stream, JsonSerializerOptions options)
        {
#if !NET48
            JsonSerializer.Serialize(stream, this, options);
#else
			using var writer = new StreamWriter(stream);
			using var jsonWriter = new JsonTextWriter(writer);
			var serializer = JsonSerializer.Create(options);
			serializer.Serialize(jsonWriter, this);
#endif
        }
    }
}
