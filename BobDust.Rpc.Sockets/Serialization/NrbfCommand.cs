using System.Formats.Nrbf;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	class NrbfCommand : NrbfCommandBase, ICommand
	{
		public NrbfCommand() : base() { }

		public NrbfCommand(string contractType, string operationName, IEnumerable<(string Name, object Value)> parameters) : base(contractType, operationName)
		{
			Parameters = parameters;
		}

		protected override void CopyFrom(BinaryCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var command = (NrbfCommand)deserialized;
			Parameters = command.Parameters;
		}

		public IEnumerable<(string, object)>? Parameters { get; private set; }

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
			(string, object)[] parameters = [..(Parameters ?? []).Select(p => {
				var (name, value) = p;
				var valueType = value.GetType();
				return (name, new JsonSerializable(valueType.AssemblyQualifiedName!, JsonSerializer.SerializeToUtf8Bytes(value, valueType)));
			})];
			formatter.Serialize(stream, new NrbfCommand(ContractType!, OperationName!, parameters));
		}

		protected override NrbfCommandBase Deserialize(string contractType, string operationName, ClassRecord rootRecord)
		{
			var paramsRecord = ((SerializationRecord[]?)rootRecord.GetArrayRecord(BackingField(nameof(Parameters)))?.GetArray(typeof((string, object)[])))?.OfType<ClassRecord>();
			IEnumerable<(string, object)> parameters = [..(paramsRecord ?? []).Select(record => {
				var paramName = record.GetString("Item1");
				var valueRecord = record.GetClassRecord("Item2");
				var typeName = valueRecord!.GetString(BackingField(nameof(JsonSerializable.Type)));
				var type = Type.GetType(typeName!);
				var bytes = ((SZArrayRecord<byte>)valueRecord!.GetArrayRecord(BackingField(nameof(JsonSerializable.Bytes)))!).GetArray();
				var paramValue = JsonSerializer.Deserialize(bytes, type!);
				return (paramName!, paramValue!);
			})];
			return new NrbfCommand(contractType, operationName, parameters);
		}
	}
}
