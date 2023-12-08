using BobDust.Rpc.Sockets.Abstractions;
using System;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	class BinaryCommandResult : BinaryCommandBase, ICommandResult
	{
		public BinaryCommandResult() : base() { }

		public BinaryCommandResult(string operationName) : base(operationName)
		{
		}

		public BinaryCommandResult(string operationName, object returnValue) : this(operationName)
		{
			ReturnValue = returnValue;
		}

		public BinaryCommandResult(string operationName, Exception exception) : this(operationName)
		{
			Exception = exception;
		}

		public object ReturnValue { get; private set; }

		public Exception Exception { get; private set; }

		protected override void CopyFrom(BinaryCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var commandResult = (BinaryCommandResult)deserialized;
			ReturnValue = commandResult.ReturnValue;
			Exception = commandResult.Exception;
		}
	}
}
