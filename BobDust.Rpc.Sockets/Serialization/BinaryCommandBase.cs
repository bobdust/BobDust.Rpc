using System.Runtime.Serialization.Formatters.Binary;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	abstract class BinaryCommandBase : BinarySequence
	{
        public string? ContractType { get; private set; }
		public string? OperationName { get; private set; }

		public BinaryCommandBase() : base()
		{
		}

		public BinaryCommandBase(string contractType, string operationName) : this()
		{
			ContractType = contractType;
			OperationName = operationName;
		}

		protected virtual void CopyFrom(BinaryCommandBase deserialized)
		{
			ContractType = deserialized.ContractType;
			OperationName = deserialized.OperationName;
		}

		public override void Write(Stream stream)
		{
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            var formatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            formatter.Serialize(stream, this);
		}

		public override void Read(Stream stream)
		{
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            var formatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            var deserialized = (BinaryCommandBase)formatter.Deserialize(stream);
			CopyFrom(deserialized);
		}
	}
}
