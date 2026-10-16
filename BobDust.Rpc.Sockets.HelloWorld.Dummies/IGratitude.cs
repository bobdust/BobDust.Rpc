namespace BobDust.Rpc.Sockets.HelloWorld.Dummies
{
	public interface IGratitude
	{
		void ThanksAway(GreetingOptions options, string gratitudeFollowup);
		GreetingResult? Thanks(GreetingOptions? options, string? greetingFollowup);
		Task<GreetingResult?> ThanksAsync(GreetingOptions? options, string? greetingFollowup);
		Task ThanksAwayAsync(GreetingOptions options, string greetingFollowup);
	}
}
