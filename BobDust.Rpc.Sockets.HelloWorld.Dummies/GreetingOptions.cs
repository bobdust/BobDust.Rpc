using System.Runtime.Serialization;

namespace BobDust.Rpc.Sockets.HelloWorld.Dummies
{
	[DataContract]
	[Serializable]
	public class GreetingOptions
	{
		[DataMember]
		public string? Name { get; set; }
	}
}