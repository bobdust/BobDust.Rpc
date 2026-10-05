using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	class BinaryCommand : BinaryCommandBase, ICommand
	{
		public BinaryCommand() : base() { }

		public BinaryCommand(string contractType, string operationName, IEnumerable<(string Name, object Value)> parameters) : base(contractType, operationName)
		{
			Parameters = parameters;
		}

		protected override void CopyFrom(BinaryCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var command = (BinaryCommand)deserialized;
			Parameters = command.Parameters;
		}

		public IEnumerable<(string, object)>? Parameters { get; private set; }

		public ICommandResult Return()
		{
			return new BinaryCommandResult(OperationName);
		}

		public ICommandResult Return(object? value)
		{
			return new BinaryCommandResult(OperationName, value);
		}

		public ICommandResult Throw(Exception exception)
		{
			return new BinaryCommandResult(OperationName, exception);
		}
	}
}
