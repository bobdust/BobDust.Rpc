namespace BobDust.Rpc.Sockets.Abstractions
{
	public interface IServer<in T> where T : class
	{
		void Start();
		void Stop();
	}
}