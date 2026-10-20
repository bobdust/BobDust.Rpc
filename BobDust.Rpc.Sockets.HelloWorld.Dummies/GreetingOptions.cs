using System.Runtime.Serialization;
#if NET48
using BobDust.Rpc.Sockets.Serialization;
#endif

namespace BobDust.Rpc.Sockets.HelloWorld.Dummies
{
#if NET48
	[TypeIndicator]
#endif
	[DataContract]
	[Serializable]
	public class GreetingOptions
	{
		[DataMember]
		public string? Name { get; set; }
	}
}