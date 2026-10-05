namespace BobDust.Rpc.Sockets.Abstractions
{
	public interface IChannel : IDisposable
	{
		IChannel Connect();
		Task<IChannel> ConnectAsync();
		IChannel Mount<T>();
		T As<T>();
	}
}
