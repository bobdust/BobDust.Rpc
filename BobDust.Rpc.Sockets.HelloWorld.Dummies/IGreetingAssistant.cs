namespace BobDust.Rpc.Sockets.HelloWorld
{
	public interface IGreetingAssistant
	{
		string Hello(GreetingOptions options, string greetingFollowup);
		Task<string> HelloAsync(GreetingOptions options, string greetingFollowup);
		Task HelloAndForgetAsync(GreetingOptions options, string greetingFollowup);
	}
}
