using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld;

namespace BobDust.Rpc.Sockets.ServerSample
{
	internal class Program
	{
		static void Main(string[] args)
		{
			const int port = 1234;
			var assistant = ServerFactory.Default.Get<GreetingAssistant>(port);
			assistant.Start();
			ServerFactory.Listen(port).Register<IGratitude, Gratitude>(() => new Gratitude()).Start();
			Console.WriteLine("Greeting Assistant started.");
			Console.ReadLine();
		}
	}
}
