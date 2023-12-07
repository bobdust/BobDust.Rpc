using System;

namespace BobDust.Rpc.Sockets.HelloWorld
{
	internal class Program
	{
		static void Main(string[] args)
		{
			const string host = "127.0.0.1";
			const int port = 1234;
			var assistant = ClientFactory.Default.Get<IGreetingAssistant>(host, port);
			var words = assistant.Hello();
			Console.WriteLine(words);
			Console.ReadLine();
		}
	}
}
