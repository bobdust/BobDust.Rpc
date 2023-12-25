namespace BobDust.Rpc.Sockets.HelloWorld
{
	public interface IGreetingAssistant
	{
		string Hello(GreetingOptions options, string greetingFollowup);
	}
}
