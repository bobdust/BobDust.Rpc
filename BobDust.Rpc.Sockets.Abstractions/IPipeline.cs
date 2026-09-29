using BobDust.Core.ExceptionHandling;

namespace BobDust.Rpc.Sockets.Abstractions
{
	public interface IPipeline : IDisposable, IExceptionHandler
	{
		string Id { get; }
		void Send(IBinarySequence data);
		void Write(byte[] buffer);
		int Read(byte[] buffer);
		void Open();
		void Close();
        Task WriteAsync(byte[] buffer, CancellationToken cancellationToken);
        Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken);

        Func<IPipeline, IBinarySequence, Task>? OnReceived { get; set; }
	}
}
