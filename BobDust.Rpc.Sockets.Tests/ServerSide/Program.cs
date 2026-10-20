using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld.Dummies;

namespace BobDust.Rpc.Sockets.Tests.ServerSide
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			await Template.Main(args, Main);
			Console.ReadLine();
		}

		private static async Task Main(int port, Settings? settings, object[] inputs)
		{
			await Task.CompletedTask;
			//const int port = 1234;
			var assistant = (settings is null ? ServerFactory.Default : ServerFactory.WithSettings(settings))
				//.WithSettings(Settings.DataContract([typeof(GreetingOptions), typeof(GreetingResult)]))
				//.WithSettings(Settings.Json)
				//.WithSettings(Settings.Xml)
				//.Default
				.Get<GreetingAssistant>(port);
			assistant.Start();
			ServerFactory.Listen(port).Register<IGratitude, Gratitude>(() => new Gratitude()).Start();
			Console.WriteLine($"Greeting Assistant on {port} started.");
		}
	}
}
