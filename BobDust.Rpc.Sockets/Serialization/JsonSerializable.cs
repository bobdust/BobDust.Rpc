namespace BobDust.Rpc.Sockets.Serialization;

#if !NET48
[Serializable]
record JsonSerializable(string Type, byte[] Bytes);
#else
[Serializable]
class JsonSerializable
{
    public string Type {get;}
    public byte[] Bytes {get;}
    public JsonSerializable(string type, byte[] bytes)
    {
        Type = type;
        Bytes = bytes;
    }
}
#endif