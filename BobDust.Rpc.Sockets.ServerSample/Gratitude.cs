using BobDust.Rpc.Sockets.HelloWorld;

namespace BobDust.Rpc.Sockets.ServerSample
{
	public class Gratitude : IGratitude
	{
		public string Thanks(GreetingOptions options, string gratitudeFollowup)
		{
			return $"Thanks, {options?.Name}. {gratitudeFollowup}!";
		}

        public Task ThanksAwayAsync(GreetingOptions options, string gratitudeFollowup) => ThanksAsync(options, gratitudeFollowup);

        public async Task<string> ThanksAsync(GreetingOptions options, string gratitudeFollowup)
        {
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
			return builder.ToString();
        }

        public void ThanksAway(GreetingOptions options, string gratitudeFollowup)
        {
            Thanks(options, gratitudeFollowup);
        }
    }
}
