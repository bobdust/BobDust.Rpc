using System.Text.Json;

namespace BobDust.Rpc.Sockets.Serialization
{
	public abstract class JsonCommandBase : BinarySequence
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

		protected abstract JsonCommandBase Deserialize(byte[] bytes, JsonSerializerOptions options);

		protected abstract byte[] Serialize(JsonSerializerOptions options);

		public override void Read(BinaryReader reader)
		{
			const int bufferSize = 4096;
			using var stream = new MemoryStream();
			var buffer = new byte[bufferSize];
			var count = 0;
			while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
			{
				stream.Write(buffer, 0, count);
			}
			stream.Seek(0, SeekOrigin.Begin);
			var jsonBytes = stream.ToArray();
			var deserialized = Deserialize(jsonBytes, _jsonSerializerOptions);
			CopyFrom(deserialized);
		}

		protected virtual void CopyFrom(JsonCommandBase deserialized)
		{
			ContractType = deserialized.ContractType;
			OperationName = deserialized.OperationName;
		}

		public override void Write(BinaryWriter writer)
		{
			var bytes = Serialize(_jsonSerializerOptions);
			writer.Write(bytes);
		}
	}
}
