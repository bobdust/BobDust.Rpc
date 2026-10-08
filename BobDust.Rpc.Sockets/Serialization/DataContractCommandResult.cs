using System.Runtime.Serialization;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	public class DataContractCommandResult : DataContractCommandBase, ICommandResult
	{
        public DataContractCommandResult() : base() { }

		public DataContractCommandResult(string ContractType, string operationName) : base(ContractType, operationName)
		{
		}

		public DataContractCommandResult(string ContractType, string operationName, object returnValue) : this(ContractType, operationName)
		{
			ReturnValue = returnValue;
		}

		public DataContractCommandResult(string ContractType, string operationName, Exception exception) : this(ContractType, operationName)
		{
			Exception = exception;
		}

		[DataMember]
		public object? ReturnValue { get; set; }

		[DataMember]
		public Exception? Exception { get; set; }

		protected override void CopyFrom(DataContractCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var commandResult = (DataContractCommandResult)deserialized;
			ReturnValue = commandResult.ReturnValue;
			Exception = commandResult.Exception;
		}

        protected override DataContractSerializer GetSerializer()
        {
            return new DataContractSerializer(GetType(), KnownTypes);
        }

        protected override DataContractCommandBase ReadObject(DataContractSerializer serializer, Stream stream)
        {
            return (DataContractCommandResult)serializer.ReadObject(stream);
        }

        protected override void WriteObject(DataContractSerializer serializer, Stream stream)
        {
            serializer.WriteObject(stream, this);
        }
    }
}
