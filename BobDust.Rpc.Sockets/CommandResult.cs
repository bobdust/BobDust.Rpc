using System;

namespace BobDust.Rpc.Sockets
{
	public abstract class CommandResult : BinarySequence
	{
		public object ReturnValue
		{
			get;
			protected set;
		}

		public string OperationName
		{
			get;
			protected set;
		}

		public Exception Exception
		{
			get;
			protected set;
		}

		protected CommandResult()
		{
		}

		protected CommandResult(string operationName)
		{
			OperationName = operationName;
		}

		protected CommandResult(string operationName, object returnValue)
		{
			OperationName = operationName;
			ReturnValue = returnValue;
		}

		protected CommandResult(string operationName, Exception exception)
		{
			OperationName = operationName;
			Exception = exception;
		}

	}
}
