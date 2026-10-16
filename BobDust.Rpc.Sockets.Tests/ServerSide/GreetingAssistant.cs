using BobDust.Rpc.Sockets.HelloWorld.Dummies;

namespace BobDust.Rpc.Sockets.Tests.ServerSide
{
	public class GreetingAssistant : IGreetingAssistant
	{
        public void Error()
        {
            throw new NotImplementedException("expected impl");
        }

        public Task ErrorAsync()
        {
            throw new Exception(nameof(ErrorAsync), new NotImplementedException("expected async impl"));
        }

        public string Hello()
		{
			return "Hello, world!";
		}

		public string Hello(GreetingOptions options)
		{
			return $"Hello, {options?.Name}";
		}

		public GreetingResult Hello(GreetingOptions options, string greetingFollowup)
		{
			return new GreetingResult{Result = $"Hello, {options?.Name}. {greetingFollowup}!"};
		}

        public Task HelloAndForgetAsync(GreetingOptions options, string greetingFollowup) => HelloAsync(options, greetingFollowup);

        public async Task<GreetingResult> HelloAsync(GreetingOptions options, string greetingFollowup)
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
			return new GreetingResult{Result = builder.ToString()};
        }
    }
}
