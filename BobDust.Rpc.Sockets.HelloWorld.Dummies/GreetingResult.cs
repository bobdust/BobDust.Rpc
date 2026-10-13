using System.Runtime.Serialization;

namespace BobDust.Rpc.Sockets.HelloWorld.Dummies;

[DataContract, KnownType(typeof(NotImplementedException))]
[Serializable]
public record GreetingResult
{
    [DataMember]
    public string? Result { get; set; }
}
