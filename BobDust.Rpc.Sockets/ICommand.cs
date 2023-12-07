using System;
using System.Collections.Generic;

namespace BobDust.Rpc.Sockets
{
	public interface ICommand : IBinarySequence
   {
      string OperationName { get; }
      IDictionary<string, object> Parameters { get; }
      ICommandResult Return();
      ICommandResult Return(object value);
      ICommandResult Throw(Exception exception);
   }
}
