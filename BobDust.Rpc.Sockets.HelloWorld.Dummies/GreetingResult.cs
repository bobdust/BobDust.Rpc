using System.Runtime.Serialization;
#if NET48
using BobDust.Rpc.Sockets.Serialization;
#endif

namespace BobDust.Rpc.Sockets.HelloWorld.Dummies;

#if NET48
	[TypeIndicator]
#endif
[DataContract, KnownType(typeof(NotImplementedException))]
[Serializable]
public record GreetingResult
{
    [DataMember]
    public string? Result { get; set; }
}
