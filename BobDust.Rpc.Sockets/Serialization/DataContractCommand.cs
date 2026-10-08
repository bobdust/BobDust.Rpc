using System.Runtime.Serialization;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	[DataContract]
	public class DataContractCommand : DataContractCommandBase, ICommand
	{
		[DataMember]
		public IEnumerable<(string Name, object Value)>? Parameters { get; set; }

		public DataContractCommand() : base() { }

		public DataContractCommand(string contractType, string operationName, IEnumerable<(string Name, object Value)> parameters) : base(contractType, operationName)
		{
			Parameters = parameters;
		}

		protected override void CopyFrom(DataContractCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var command = (DataContractCommand)deserialized;
			Parameters = command.Parameters;
		}

		public ICommandResult Return()
		{
			return new DataContractCommandResult(ContractType!, OperationName!);
		}

		public ICommandResult Return(object? value)
		{
			return new DataContractCommandResult(ContractType!, OperationName!, value);
		}

		public ICommandResult Throw(Exception exception)
		{
			return new DataContractCommandResult(ContractType!, OperationName!, exception);
		}

        protected override DataContractSerializer GetSerializer()
        {
            return new DataContractSerializer(GetType(), KnownTypes);
        }

        protected override DataContractCommandBase ReadObject(DataContractSerializer serializer, Stream stream)
        {
            return (DataContractCommand)serializer.ReadObject(stream)!;
        }

        protected override void WriteObject(DataContractSerializer serializer, Stream stream)
        {
            serializer.WriteObject(stream, this);
        }
    }
}
