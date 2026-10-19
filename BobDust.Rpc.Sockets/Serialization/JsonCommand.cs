#if !NET48
using System.Text.Json;
#else
using Newtonsoft.Json;
using JsonSerializerOptions = Newtonsoft.Json.JsonSerializerSettings;
#endif
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	class JsonCommand : JsonCommandBase, ICommand
	{
		public IEnumerable<(string Name, object? Value, string Type)>? Parameters { get; set; }

		public JsonCommand() : base() { }

		public JsonCommand(string contractType, string operationName, IEnumerable<(string Name, object? Value, string Type)> parameters) : base(contractType, operationName)
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
			return new JsonCommandResult(ContractType!, OperationName!);
		}

		public ICommandResult Return(object? value)
		{
			return new JsonCommandResult(ContractType!, OperationName!, value);
		}

		public ICommandResult Throw(Exception exception)
		{
			return new JsonCommandResult(ContractType!, OperationName!, exception);
		}

		protected override JsonCommandBase Deserialize(Stream stream, JsonSerializerOptions options)
		{
#if !NET48
            return JsonSerializer.Deserialize<JsonCommand>(stream, options)!;
#else
			using var reader = new StreamReader(stream);
			using var jsonReader = new JsonTextReader(reader);
			var serializer = JsonSerializer.Create(options);
			return serializer.Deserialize<JsonCommand>(jsonReader)!;
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
