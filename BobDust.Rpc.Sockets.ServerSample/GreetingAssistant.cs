using BobDust.Rpc.Sockets.HelloWorld;

namespace BobDust.Rpc.Sockets.ServerSample
{
	public class GreetingAssistant : IGreetingAssistant
	{
		public string Hello()
		{
			return "Hello, world!";
		}

		public string Hello(GreetingOptions options)
		{
			return $"Hello, {options?.Name}";
		}

		public string Hello(GreetingOptions options, string greetingFollowup)
		{
			return $"Hello, {options?.Name}. {greetingFollowup}!";
		}
	}
}
