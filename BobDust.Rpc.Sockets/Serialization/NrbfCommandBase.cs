using System.Formats.Nrbf;
using System.Runtime.Serialization.Formatters.Binary;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	abstract class NrbfCommandBase : BinaryCommandBase
	{
		public NrbfCommandBase() : base()
		{
		}

		public NrbfCommandBase(string contractType, string operationName) : base(contractType, operationName)
		{
		}

		public override void Write(Stream stream)
		{
#pragma warning disable SYSLIB0011 // Type or member is obsolete
			var formatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
			Serialize(formatter, stream);
		}

#pragma warning disable SYSLIB0011 // Type or member is obsolete
		protected abstract void Serialize(BinaryFormatter formatter, Stream stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

		public override void Read(Stream stream)
		{
			var rootRecord = NrbfDecoder.DecodeClassRecord(stream);
			var contractType = rootRecord.GetString(BaseBackingField(nameof(BinaryCommandBase), nameof(ContractType)));
			var operationName = rootRecord.GetString(BaseBackingField(nameof(BinaryCommandBase), nameof(OperationName)));
			var deserialized = Deserialize(contractType!, operationName!, rootRecord);
			CopyFrom(deserialized);
		}

		protected abstract NrbfCommandBase Deserialize(string contractType, string operationName, ClassRecord rootRecord);

		protected string BackingField(string nameOf) => $"<{nameOf}>k__BackingField";

		protected string BaseBackingField(string nameOfBase, string nameOf) => $"{nameOfBase}+<{nameOf}>k__BackingField";
	}
}