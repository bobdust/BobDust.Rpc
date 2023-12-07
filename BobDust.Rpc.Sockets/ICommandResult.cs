using System;

namespace BobDust.Rpc.Sockets
{
	public interface ICommandResult : IBinarySequence
   {
      string OperationName { get; }
      object ReturnValue { get; }
      Exception Exception { get; }
   }
}
