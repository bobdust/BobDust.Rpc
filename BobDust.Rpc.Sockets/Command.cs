using BobDust.Rpc.Sockets.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BobDust.Rpc.Sockets
{
	abstract class Command : BinarySequence
	{
		public string OperationName
		{
			get;
			protected set;
		}

		public IEnumerable<(string Name, object Value)> Parameters
		{
			get;
			protected set;
		}

		public Command()
		{
			Parameters = Enumerable.Empty<(string Name, object Value)> ();
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
