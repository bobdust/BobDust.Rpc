using System.Text.Json;
using System.Text.Json.Serialization;
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
            return JsonSerializer.Deserialize<JsonCommandResult>(stream, options)!;
        }

        protected override void Serialize(Stream stream, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(stream, this, options);
        }
    }
}
