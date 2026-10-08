using System.Runtime.Serialization;

namespace BobDust.Rpc.Sockets.Serialization
{
	[DataContract]
	[KnownType(typeof(DataContractCommand))]
	[KnownType(typeof(DataContractCommandResult))]
	public abstract class DataContractCommandBase : BinarySequence
	{
		private static HashSet<Type> _knownTypes = [];
		public static IEnumerable<Type> KnownTypes 
		{ 
			get
			{
				return _knownTypes;
			}
			set
			{
				_knownTypes = _knownTypes.Union(value).ToHashSet();
			}
		}

		[DataMember]
		public string? ContractType { get; set; }

		[DataMember]
		public string? OperationName { get; set; }

		public DataContractCommandBase() : base() { }

		public DataContractCommandBase(string contractType, string operationName)
		{
			ContractType = contractType;
			OperationName = operationName;
		}

		protected abstract DataContractSerializer GetSerializer();

		protected abstract DataContractCommandBase ReadObject(DataContractSerializer serializer, Stream stream);

		protected abstract void WriteObject(DataContractSerializer serializer, Stream stream);

		protected virtual void CopyFrom(DataContractCommandBase deserialized)
		{
			ContractType = deserialized.ContractType;
			OperationName = deserialized.OperationName;
		}

		public override void Write(Stream stream)
		{
			var serializer = GetSerializer();
			WriteObject(serializer, stream);
		}

		public override void Read(Stream stream)
		{
			var serializer = GetSerializer();
			var deserialized = ReadObject(serializer, stream);
			CopyFrom(deserialized);
		}
	}
}
