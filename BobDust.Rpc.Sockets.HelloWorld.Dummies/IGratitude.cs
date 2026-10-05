namespace BobDust.Rpc.Sockets.HelloWorld
{
	public interface IGratitude
	{
		void ThanksAway(GreetingOptions options, string gratitudeFollowup);
		string Thanks(GreetingOptions options, string greetingFollowup);
		Task<string> ThanksAsync(GreetingOptions options, string greetingFollowup);
		Task ThanksAwayAsync(GreetingOptions options, string greetingFollowup);
	}
}
