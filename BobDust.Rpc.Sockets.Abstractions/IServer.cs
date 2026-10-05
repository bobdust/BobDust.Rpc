namespace BobDust.Rpc.Sockets.Abstractions
{
	public interface IServer
	{
		void Start();
		void Stop();
		IServer Register<TContract, TImplementation>(Func<TImplementation> factory) where TImplementation : class, TContract;
	}

	public interface IServer<in T> : IServer where T : class
	{
	}
}