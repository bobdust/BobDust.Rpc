using System;
using BobDust.Core.ExceptionHandling;

namespace BobDust.Rpc.Sockets
{
	public interface IPipeline : IDisposable, IExceptionHandler
   {
      string Id { get; }
      void Send(IBinarySequence data);
      void Write(byte[] buffer);
      int Read(byte[] buffer);
      void Open();
      void Close();
      Action<IPipeline, IBinarySequence> OnReceived { get; set; }
   }
}
