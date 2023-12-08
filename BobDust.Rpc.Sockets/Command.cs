using BobDust.Rpc.Sockets.Abstractions;
using System;
using System.Collections.Generic;

namespace BobDust.Rpc.Sockets
{
	abstract class Command : BinarySequence
	{
		public string OperationName
		{
			get;
			protected set;
		}

		public IDictionary<string, object> Parameters
		{
			get;
			private set;
		}

		public Command()
		{
			Parameters = new Dictionary<string, object>();
		}

		public Command(string operationName)
		   : this()
		{
			OperationName = operationName;
		}

		public abstract ICommandResult Return();

		public abstract ICommandResult Return(object value);

		public abstract ICommandResult Throw(Exception exception);

	}
}
