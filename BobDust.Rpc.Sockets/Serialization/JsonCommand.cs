using System.Text.Json;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	public class JsonCommand : JsonCommandBase, ICommand
	{
		public IEnumerable<(string Name, object Value)>? Parameters { get; set; }

		public JsonCommand() : base() { }

		public JsonCommand(string contractType, string operationName, IEnumerable<(string Name, object Value)> parameters) : base(contractType, operationName)
		{
			Parameters = parameters;
		}

		protected override void CopyFrom(JsonCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var command = (JsonCommand)deserialized;
			Parameters = command.Parameters;
		}

		public ICommandResult Return()
		{
			return new JsonCommandResult(ContractType, OperationName);
		}

		public ICommandResult Return(object? value)
		{
			return new JsonCommandResult(ContractType, OperationName, value);
		}

		public ICommandResult Throw(Exception exception)
		{
			return new JsonCommandResult(ContractType, OperationName, exception);
		}

        protected override JsonCommandBase Deserialize(byte[] bytes, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<JsonCommand>(bytes, options);
        }

        protected override byte[] Serialize(JsonSerializerOptions options)
        {
            return JsonSerializer.SerializeToUtf8Bytes(this, options);
        }
    }
}
