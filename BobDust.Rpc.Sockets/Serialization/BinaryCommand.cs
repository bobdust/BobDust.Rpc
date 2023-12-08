using BobDust.Rpc.Sockets.Abstractions;
using System;
using System.Collections.Generic;

namespace BobDust.Rpc.Sockets.Serialization
{
	[Serializable]
	class BinaryCommand : BinaryCommandBase, ICommand
	{
		public BinaryCommand() : base() { 
			Parameters = new Dictionary<string, object>();
		}

		public BinaryCommand(string operationName)
		   : base(operationName)
		{
			Parameters = new Dictionary<string, object>();
		}

		public BinaryCommand(string operationName, IDictionary<string, object> parameters) : this(operationName)
		{
			Parameters = parameters;
		}

		protected override void CopyFrom(BinaryCommandBase deserialized)
		{
			base.CopyFrom(deserialized);
			var command = (BinaryCommand) deserialized;
			Parameters = command.Parameters;
		}

		public IDictionary<string, object> Parameters { get; private set; }

		public ICommandResult Return()
		{
			return new BinaryCommandResult(OperationName);
		}

		public ICommandResult Return(object value)
		{
			return new BinaryCommandResult(OperationName, value);
		}

		public ICommandResult Throw(Exception exception)
		{
			return new BinaryCommandResult(OperationName, exception);
		}
	}
}
