using System.Runtime.Serialization;
using BobDust.Rpc.Sockets.Serialization;

namespace BobDust.Rpc.Sockets.HelloWorld
{
	[ObjectIndicator]
	[DataContract]
	[Serializable]
	public class GreetingOptions
	{
		[DataMember]
		public string? Name { get; set; }
	}
}