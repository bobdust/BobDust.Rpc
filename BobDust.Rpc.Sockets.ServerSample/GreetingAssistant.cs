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

        public Task HelloAndForgetAsync(GreetingOptions options, string greetingFollowup) => HelloAsync(options, greetingFollowup);

        public async Task<string> HelloAsync(GreetingOptions options, string greetingFollowup)
        {
			async IAsyncEnumerable<string> AsyncHello()
			{
				yield return "Hello";
				yield return $" {options?.Name ?? "world"}";
				yield return ". ";
				yield return greetingFollowup;
			}
			var builder = new System.Text.StringBuilder();
            await foreach (var part in AsyncHello())
			{
				builder.Append(part);
			}
			return builder.ToString();
        }
    }
}
