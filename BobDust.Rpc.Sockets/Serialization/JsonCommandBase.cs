using System.Text.Json;

namespace BobDust.Rpc.Sockets.Serialization
{
	abstract class JsonCommandBase : BinarySequence
	{
		private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
		{
			IncludeFields = true // Required for tuples
		};
		public string? ContractType { get; set; }

		public string? OperationName { get; set; }

		public JsonCommandBase() : base()
		{
			_jsonSerializerOptions = new JsonSerializerOptions
			{
				IncludeFields = true, // Required for tuples
				Converters = { new ObjectToNativeTypesConverter() }
			};

		}

		public JsonCommandBase(string contractType, string operationName) : this()
		{
			ContractType = contractType;
			OperationName = operationName;
		}

		protected abstract JsonCommandBase Deserialize(Stream stream, JsonSerializerOptions options);

		protected abstract void Serialize(Stream stream, JsonSerializerOptions options);

		protected virtual void CopyFrom(JsonCommandBase deserialized)
		{
			ContractType = deserialized.ContractType;
			OperationName = deserialized.OperationName;
		}

		public override void Write(Stream stream)
		{
			Serialize(stream, _jsonSerializerOptions);
		}

		public override void Read(Stream stream)
		{
			var deserialized = Deserialize(stream, _jsonSerializerOptions);
			CopyFrom(deserialized);
		}
	}
}
