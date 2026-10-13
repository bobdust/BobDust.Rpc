namespace BobDust.Rpc.Sockets.HelloWorld.Dummies
{
	public interface IGreetingAssistant
	{
		GreetingResult Hello(GreetingOptions options, string greetingFollowup);
		void Error();
		Task ErrorAsync();
		Task<GreetingResult> HelloAsync(GreetingOptions options, string greetingFollowup);
		Task HelloAndForgetAsync(GreetingOptions options, string greetingFollowup);
	}

}
