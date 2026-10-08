using System.Text.Json;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	class JsonCommandResult : JsonCommandBase, ICommandResult
	{
        public JsonCommandResult() : base() { }

		public JsonCommandResult(string ContractType, string operationName) : base(ContractType, operationName)
		{
		}

		public JsonCommandResult(string ContractType, string operationName, object? returnValue) : this(ContractType, operationName)
		{
			ReturnValue = returnValue;
		}

		public JsonCommandResult(string ContractType, string operationName, Exception exception) : this(ContractType, operationName)
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
