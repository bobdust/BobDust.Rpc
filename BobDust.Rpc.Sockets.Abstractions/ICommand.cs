using System;
using System.Collections.Generic;

namespace BobDust.Rpc.Sockets.Abstractions
{
	public interface ICommand : IBinarySequence
	{
		string OperationName { get; }
		IEnumerable<(string Name, object Value)> Parameters { get; }

		ICommandResult Return();
		ICommandResult Return(object value);
		ICommandResult Throw(Exception exception);
	}
}
