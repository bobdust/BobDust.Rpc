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
		public string? ContractType { get; private set; }

		[DataMember]
		public string? OperationName { get; private set; }

		public DataContractCommandBase() : base() { }

		public DataContractCommandBase(string contractType, string operationName)
		{
			ContractType = contractType;
			OperationName = operationName;
		}

		protected abstract DataContractSerializer GetSerializer();

		protected abstract DataContractCommandBase ReadObject(DataContractSerializer serializer, Stream stream);

		protected abstract void WriteObject(DataContractSerializer serializer, Stream stream);

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
			var serializer = GetSerializer();
			stream.Seek(0, SeekOrigin.Begin);
			var deserialized = ReadObject(serializer, stream);
			CopyFrom(deserialized);
		}

		protected virtual void CopyFrom(DataContractCommandBase deserialized)
		{
			ContractType = deserialized.ContractType;
			OperationName = deserialized.OperationName;
		}

		public override void Write(BinaryWriter writer)
		{
			using var stream = new MemoryStream();
			var serializer = GetSerializer();
			WriteObject(serializer, stream);
			stream.Position = 0;
			writer.Write(stream.ToArray());
		}
	}
}
