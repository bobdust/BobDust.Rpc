using BobDust.Rpc.Sockets.HelloWorld.Dummies;

namespace BobDust.Rpc.Sockets.ServerSample
{
	public class Gratitude : IGratitude
	{
		public GreetingResult? Thanks(GreetingOptions? options, string? gratitudeFollowup)
		{
			if (options is null || gratitudeFollowup is null)
			{
				return default;
			}

			return new GreetingResult{Result = $"Thanks, {options?.Name}. {gratitudeFollowup}!"};
		}

		public Task ThanksAwayAsync(GreetingOptions options, string gratitudeFollowup) => ThanksAsync(options, gratitudeFollowup);

		public async Task<GreetingResult?> ThanksAsync(GreetingOptions? options, string? gratitudeFollowup)
		{
			if (options is null || gratitudeFollowup is null)
			{
				return default;
			}

			async IAsyncEnumerable<string> AsyncThanks()
			{
				yield return "Thanks";
				yield return $" {options?.Name ?? "world"}";
				yield return ". ";
				yield return gratitudeFollowup;
			}
			var builder = new System.Text.StringBuilder();
			await foreach (var part in AsyncThanks())
			{
				builder.Append(part);
			}
			return new GreetingResult{ Result = builder.ToString()};
		}

		public void ThanksAway(GreetingOptions options, string gratitudeFollowup)
		{
			Thanks(options, gratitudeFollowup);
		}
	}
}
