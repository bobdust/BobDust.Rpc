using System.Formats.Nrbf;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

#if !NET48
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif

using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	class NrbfCommand : NrbfCommandBase, ICommand
	{
		public NrbfCommand() : base() { }

		public NrbfCommand(string contractType, string operationName, IEnumerable<(string Name, object? Value, string Type)> parameters) : base(contractType, operationName)
		{
			Parameters = parameters;
		}

		protected override void CopyFrom(BinaryCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var command = (NrbfCommand)deserialized;
			Parameters = command.Parameters;
		}

		public IEnumerable<(string Name, object? Value, string Type)>? Parameters { get; private set; }

		public ICommandResult Return()
		{
			return new NrbfCommandResult(ContractType!, OperationName!);
		}

		public ICommandResult Return(object? value)
		{
			return new NrbfCommandResult(ContractType!, OperationName!, value);
		}

		public ICommandResult Throw(Exception exception)
		{
			return new NrbfCommandResult(ContractType!, OperationName!, exception);
		}

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        protected override void Serialize(BinaryFormatter formatter, Stream stream)
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        {
			(string, object?, string)[] parameters = [..(Parameters ?? []).Select(p => {
				var (name, value, type) = p;
				var valueType = Type.GetType(type)!;
#if !NET48
				return (name, new JsonSerializable(type, JsonSerializer.SerializeToUtf8Bytes(value, valueType)), type);
#else
				return (name, new JsonSerializable(type, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, valueType, null))), type);
#endif
			})];
			formatter.Serialize(stream, new NrbfCommand(ContractType!, OperationName!, parameters));
		}

		protected override NrbfCommandBase Deserialize(string contractType, string operationName, ClassRecord rootRecord)
		{
			var paramsRecord = ((SerializationRecord[]?)rootRecord.GetArrayRecord(BackingField(nameof(Parameters)))?.GetArray(typeof((string, object?, string)[])))?.OfType<ClassRecord>();
			IEnumerable<(string, object?, string)> parameters = [..(paramsRecord ?? []).Select(record => {
				var paramName = record.GetString("Item1");
				var valueRecord = record.GetClassRecord("Item2");
				var typeName = valueRecord!.GetString(BackingField(nameof(JsonSerializable.Type)));
				var type = Type.GetType(typeName!);
				var bytes = ((SZArrayRecord<byte>)valueRecord!.GetArrayRecord(BackingField(nameof(JsonSerializable.Bytes)))!).GetArray();
#if !NET48
				var paramValue = JsonSerializer.Deserialize(bytes, type!);
#else
				var paramValue = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type!);
#endif
				return (paramName!, paramValue!, typeName!);
			})];
			return new NrbfCommand(contractType, operationName, parameters!);
		}
	}
}
