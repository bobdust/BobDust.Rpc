using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Obsolete(".NET 10 no longer supports BinaryFormatter.")]
	[Serializable]
	class BinaryCommandResult : BinaryCommandBase, ICommandResult
	{
		public BinaryCommandResult() : base() { }

		public BinaryCommandResult(string operationName) : base(string.Empty, operationName)
		{
		}

		public BinaryCommandResult(string operationName, object returnValue) : this(operationName)
		{
			ReturnValue = returnValue;
		}

		public object? ReturnValue { get; private set; }

		public Exception? Exception { get; private set; }

		protected override void CopyFrom(BinaryCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var commandResult = (BinaryCommandResult)deserialized;
			ReturnValue = commandResult.ReturnValue;
			Exception = commandResult.Exception;
		}
	}
}
