using BobDust.Rpc.Sockets.Builders;
using System;

namespace BobDust.Rpc.Sockets.ServerSample
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var assistant = ServerFactory.Default.Get<GreetingAssistant>(1234);
			assistant.Start();
			Console.WriteLine("Greeting Assistant started.");
			Console.ReadLine();
		}
	}
}
